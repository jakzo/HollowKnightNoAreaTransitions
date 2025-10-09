namespace HollowKnightNoAreaTransitions;

public class SceneLoader(HollowKnightNoAreaTransitionsMod mod)
{
    public static Vector3 WORLD_OFFSET = new(200f, 200f, 0f);

    private static int LAYER_TERRAIN;
    private static PhysicsMaterial2D PHYSICS_MATERIAL_TERRAIN;

    public static void UnloadAllScenes()
    {
        for (var i = 0; i < USceneManager.sceneCount; i++)
        {
            var scene = USceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            USceneManager.UnloadSceneAsync(scene);
        }
    }

    public void Initialize()
    {
        LAYER_TERRAIN = LayerMask.NameToLayer("Terrain");
    }

    public void Deinitialize() { }

    // Does things which only need to be done once per chunk load, like creating
    // transition passageway colliders, etc.
    public void InitializeMainChunkScene(ChunkState cs, Scene scene, bool doNotMove = false)
    {
        CreateColliders(cs);
        CalculateChunkInfoAndUpdateNeighbors(cs);

        // Move the scene to the correct position in the game world, etc.
        if (!doNotMove)
            MoveScene(scene, cs.Chunk.Position + WORLD_OFFSET);
        RemoveTransitionHaze(scene);
        RemoveRemaskers(scene);
        RemoveSceneBorders(scene);
        // TODO: Automatically adjust colliders so they don't intersect other scenes based on chunk maps?
        cs.Chunk.OnLoad?.Invoke(scene);
    }

    public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(
        string sceneName,
        Action<Scene> onComplete = null
    )
    {
        // Logger.Time(sceneName, "Starting load...", true);
        var op = Addressables.LoadSceneAsync("Scenes/" + sceneName, LoadSceneMode.Additive);
        op.Completed += _ =>
        {
            // Logger.Time(op.Result.Scene.name, "Finished load");
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                var scene = op.Result.Scene;
                MelonCoroutines.Start(
                    WaitForSceneInitialization(
                        scene,
                        scene =>
                        {
                            // Logger.Time(op.Result.Scene.name, "Finished init");
                            onComplete?.Invoke(scene);
                            // Logger.Time(scene.name, "Finished post init");
                        }
                    )
                );
            }
            else
            {
                Logger.Error("Failed to load scene: " + op.OperationException);
            }
        };
        return op;
    }

    public static void RunAfterSceneHasInitialized(Scene scene, Action<Scene> onComplete)
    {
        MelonCoroutines.Start(WaitForSceneInitialization(scene, onComplete));
    }

    private static IEnumerator WaitForSceneInitialization(Scene scene, Action<Scene> onComplete)
    {
        var startTime = Time.realtimeSinceStartup;
        var hasLogged = false;
        yield return new WaitUntil(() =>
        {
            if (!hasLogged && Time.realtimeSinceStartup - startTime > 10f)
            {
                Logger.Error($"Hung while waiting for scene '{scene.name}' to initialize...");
                hasLogged = true;
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return false;
            var rootObjects = scene.GetRootGameObjects();
            return rootObjects.Length > 0 && rootObjects.Any(go => go.activeInHierarchy);
        });

        // Wait a few more frames to let things settle
        // for (int i = 0; i < 3; i++)
        //     yield return null;

        Utils.Hooks.Try(() => onComplete?.Invoke(scene));
    }

    // Moves all scenes in the chunk by a certain amount
    public void MoveChunk(ChunkState cs, Vector3 offset)
    {
        foreach (var scene in cs.Scenes)
            MoveScene(scene, offset);
        CalculateChunkInfoAndUpdateNeighbors(cs);
    }

    // Moves all objects in the scene by a certain amount
    public void MoveScene(Scene scene, Vector3 offset)
    {
        foreach (var obj in scene.GetRootGameObjects())
        {
            obj.transform.localPosition += offset;
        }
    }

    public IEnumerable<Chunk> GetOverlappingChunks(ChunkState cs)
    {
        var bounds = cs.Chunk.GetTilemapBounds().Value;
        return mod.ChunkManager.CurrentMap?.Chunks.Where(c =>
            {
                if (c.SceneName == cs.MainScene.name)
                    return false;
                var otherBounds = c.GetTilemapBounds();
                if (otherBounds == null)
                    return false;
                return bounds.Overlaps(otherBounds.Value);
            }) ?? [];
    }

    public void CalculateChunkInfoAndUpdateNeighbors(ChunkState cs)
    {
        if (cs.Tilemap == null)
            return;

        cs.Chunk.Calculated ??= Utils.Tilemap.CalculateChunkInfo(cs.Tilemap);

        // TODO: Optimize by returning early if we know neighboring chunks have not changed
        var newOverlappingChunks = GetOverlappingChunks(cs).ToHashSet();
        var removedChunks = cs
            .Chunk.Calculated.OverlappingChunks.Where(c => !newOverlappingChunks.Contains(c))
            .ToArray();
        var addedChunks = newOverlappingChunks
            .Where(c => !cs.Chunk.Calculated.OverlappingChunks.Contains(c))
            .ToArray();
        foreach (var other in removedChunks)
        {
            cs.Chunk.Calculated.OverlappingChunks.Remove(other);
            other.Calculated?.OverlappingChunks.Remove(cs.Chunk);
            if (mod.ChunkManager.LoadedChunkStates.TryGetValue(other.SceneName, out var otherCs))
                RedoTilemap(otherCs);
        }
        foreach (var other in addedChunks)
        {
            cs.Chunk.Calculated.OverlappingChunks.Add(other);
            other.Calculated?.OverlappingChunks.Add(cs.Chunk);
            if (mod.ChunkManager.LoadedChunkStates.TryGetValue(other.SceneName, out var otherCs))
                RedoTilemap(otherCs);
        }
        RedoTilemap(cs);
    }

    public void RedoTilemap(ChunkState cs)
    {
        RestoreTilemap(cs);
        RemoveTilesObscuringNeighboringChunksFromTilemap(cs);
    }

    public void RestoreTilemap(ChunkState cs)
    {
        if (cs.Tilemap == null)
            return;

        var layer = cs.Tilemap.Layers[0];
        if (layer == null || cs.Chunk.Calculated.TilesToRemove.Count == 0)
            return;

        foreach (var (x, y) in cs.Chunk.Calculated.TilesToRemove)
            layer.SetTile(x, y, Utils.Tilemap.TILE_OCCUPIED);
        cs.Chunk.Calculated.TilesToRemove.Clear();
    }

    // Make sure tilemaps are not overlapping neighboring chunk playable areas
    // TODO: Should I cache changes for next time the scene is loaded?
    public void RemoveTilesObscuringNeighboringChunksFromTilemap(ChunkState cs)
    {
        var layer = cs.Tilemap.Layers[0];
        if (layer == null)
            return;

        var chunkPosX = (int)cs.Chunk.Position.x;
        var chunkPosY = (int)cs.Chunk.Position.y;
        var neighbors = cs
            .Chunk.Calculated.OverlappingChunks.Select(n =>
                (
                    (int)cs.Chunk.Position.x - (int)n.Position.x,
                    (int)cs.Chunk.Position.y - (int)n.Position.y,
                    n.Calculated
                )
            )
            .ToArray();

        for (int y = 0; y < cs.Tilemap.height; y++)
        {
            for (int x = 0; x < cs.Tilemap.width; x++)
            {
                if (cs.Chunk.Calculated.PlayableLookupTable[x, y])
                    continue;

                foreach (var (neighborOffsetX, neighborOffsetY, neighborCalculated) in neighbors)
                {
                    var nx = x + neighborOffsetX;
                    var ny = y + neighborOffsetY;
                    if (
                        nx < 0
                        || ny < 0
                        || nx >= neighborCalculated.TilemapSize.x
                        || ny >= neighborCalculated.TilemapSize.y
                        || !neighborCalculated.PlayableLookupTable[nx, ny]
                    )
                        continue;

                    layer.SetTile(x, y, Utils.Tilemap.TILE_EMPTY);
                    cs.Chunk.Calculated.TilesToRemove.Add((x, y));
                    break;
                }
            }
        }

        cs.Tilemap.Build();
    }

    // Transition haze is the yellowish light coming from transition doorways
    public void RemoveTransitionHaze(Scene scene)
    {
        var transitionPoints = UObject.FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None);
        foreach (var point in transitionPoints)
        {
            if (point.gameObject.scene != scene || !mod.TransitionHooks.IsTransitionDisabled(point))
                continue;

            foreach (Transform child in point.transform)
            {
                if (child.name.Contains("haze"))
                    child.gameObject.SetActive(false);
            }
        }
    }

    // Remaskers darken areas until you enter them, but we want the whole map to be visible
    public void RemoveRemaskers(Scene scene)
    {
        var remaskers = UObject.FindObjectsByType<Remasker>(FindObjectsSortMode.None);
        foreach (var remasker in remaskers)
        {
            if (remasker.gameObject.scene != scene)
                continue;

            remasker.gameObject.SetActive(false);
        }

        // Hide Abyss darkener too
        foreach (var obj in scene.GetRootGameObjects())
        {
            if (obj.name.Contains("darkener"))
                obj.SetActive(false);
        }
    }

    private static readonly HashSet<string> SPRITE_NAMES_WHICH_OBSCURE =
    [
        "white_solid",
        "black_solid",
        "black_fader_moon",
        "msk_generic",
        "msk_generic_soft",
        "pipe_mask_02",
    ];

    public void RemoveSceneBorders(Scene scene)
    {
        foreach (var obj in scene.GetRootGameObjects())
        {
            if (obj.name.Contains("SceneBorder") || obj.name.Contains("pipe_mask"))
                obj.SetActive(false);

            // TODO: Some are used within the scene for art, need to handle manually instead of this
            var spriteRenderers = obj.GetComponents<SpriteRenderer>()
                .Concat(obj.GetComponentsInChildren<SpriteRenderer>());
            foreach (var sr in spriteRenderers)
            {
                var name = sr.sprite?.name;
                if (name != null && SPRITE_NAMES_WHICH_OBSCURE.Contains(name))
                    sr.enabled = false;
            }
        }
    }

    // Creates extra colliders for passageways between rooms for when neighboring
    // rooms are not touching, should be called after moving the scene
    private void CreateColliders(ChunkState cs)
    {
        if (cs.Chunk.Colliders == null)
            return;
        Logger.Debug($"CreateColliders: {cs.Chunk.SceneName}");

        if (PHYSICS_MATERIAL_TERRAIN == null)
        {
            PHYSICS_MATERIAL_TERRAIN = Resources
                .FindObjectsOfTypeAll<PhysicsMaterial2D>()
                .FirstOrDefault(m => m.name == "Terrain");
            if (PHYSICS_MATERIAL_TERRAIN == null)
            {
                Logger.Error("Could not find Terrain PhysicsMaterial2D");
                return;
            }
        }

        var parent = new GameObject("HKNAT_TransitionColliders").transform;
        USceneManager.MoveGameObjectToScene(parent.gameObject, cs.MainScene);

        foreach (var rect in cs.Chunk.Colliders)
        {
            CreateTransitionCollider(parent, rect);
        }
    }

    public static BoxCollider2D CreateTransitionCollider(Transform parent, Rect rect)
    {
        var go = new GameObject("HKNAT_TransitionCollider");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = rect.position;
        go.layer = LAYER_TERRAIN;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = rect.size;
        collider.sharedMaterial = PHYSICS_MATERIAL_TERRAIN;
        collider.offset = rect.size / 2f;

        // TODO: Use existing/modified art assets
        var meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.mesh = new()
        {
            vertices =
            [
                new(0f, 0f),
                new(rect.width, 0f),
                new(0f, rect.height),
                new(rect.width, rect.height),
            ],
            triangles = [0, 2, 1, 2, 3, 1],
        };
        meshFilter.mesh.RecalculateNormals();

        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Diffuse"))
        {
            color = new Color(0.1f, 0.1f, 0.2f),
        };
        return collider;
    }

    public static void SetChunkFrozen(ChunkState cs, bool frozen)
    {
        if (cs.IsFrozen == frozen)
            return;

        if (frozen)
        {
            cs.FrozenBehaviours = [];
            cs.FrozenRigidbodies = [];

            void Visit(Transform transform)
            {
                for (int i = 0; i < transform.childCount; i++)
                    Visit(transform.GetChild(i));
                var mbs = transform.GetComponents<Behaviour>();
                foreach (var mb in mbs)
                {
                    if (mb != null && mb.enabled)
                    {
                        try
                        {
                            cs.FrozenBehaviours.Add(mb);
                            mb.enabled = !frozen;
                        }
                        catch
                        {
                            // Some components cannot be added to the list, just ignore them
                        }
                    }
                }
                var rbs = transform.GetComponents<Rigidbody2D>();
                foreach (var rb in rbs)
                {
                    if (rb != null && rb.simulated)
                    {
                        rb.simulated = !frozen;
                        cs.FrozenRigidbodies.Add(rb);
                    }
                }
            }

            foreach (var scene in cs.Scenes)
            {
                if (!scene.isLoaded)
                    continue;
                foreach (var obj in scene.GetRootGameObjects())
                    Visit(obj.transform);
            }
        }
        else
        {
            foreach (var mb in cs.FrozenBehaviours)
                if (mb != null)
                    mb.enabled = true;
            cs.FrozenBehaviours = null;

            foreach (var rb in cs.FrozenRigidbodies)
                if (rb != null)
                    rb.simulated = true;
            cs.FrozenRigidbodies = null;
        }
    }
}
