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

    public event Action<Scene> OnChunkSceneInit;
    public event Action<Scene> OnAnySceneInit;

    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;

    public void Initialize()
    {
        LAYER_TERRAIN = LayerMask.NameToLayer("Terrain");
    }

    public void Deinitialize() { }

    // Does things which only need to be done once per chunk load, like creating
    // transition passageway colliders, etc.
    public void InitializeMainChunkScene(ChunkState cs, Scene scene)
    {
        CreateColliders(cs);
        GeneratePlayableArea(cs, scene);
        InitializeScene(cs, scene);
    }

    // Moves the scene to the correct position in the game world, etc.
    public void InitializeScene(ChunkState cs, Scene scene)
    {
        MoveScene(scene, cs.Chunk.Position + WORLD_OFFSET);
        RemoveTransitionHaze(scene);
        RemoveRemaskers(scene);
        RemoveSceneBorders(scene);
        // TODO: Automatically adjust colliders so they don't intersect other scenes based on chunk maps?
        // TODO: Boss scene loads (freezes player while waiting for them)
        cs.Chunk.OnLoad?.Invoke(scene);
        OnChunkSceneInit?.Invoke(scene);
    }

    public void HandleSceneLoadedByGame(AsyncOperationHandle<SceneInstance> handle)
    {
        var scene = handle.Result.Scene;
        MelonCoroutines.Start(WaitForSceneInitialization(scene, AfterWaitingForSceneInit));
    }

    public AsyncOperationHandle<SceneInstance> LoadSceneAsync(
        string sceneName,
        Action<Scene> onComplete = null
    )
    {
        Logger.Time(sceneName, "Starting load...", true);
        var op = Addressables.LoadSceneAsync("Scenes/" + sceneName, LoadSceneMode.Additive);
        op.Completed += _ => HandleSceneLoadedByUs(op, onComplete);
        return op;
    }

    private void HandleSceneLoadedByUs(
        AsyncOperationHandle<SceneInstance> handle,
        Action<Scene> onComplete
    )
    {
        Logger.Time(handle.Result.Scene.name, "Finished load");
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var scene = handle.Result.Scene;
            MelonCoroutines.Start(
                WaitForSceneInitialization(
                    scene,
                    scene =>
                    {
                        Logger.Time(handle.Result.Scene.name, "Finished init");
                        AfterWaitingForSceneInit(scene);
                        onComplete?.Invoke(scene);
                        Logger.Time(scene.name, "Finished post init");
                    }
                )
            );
        }
        else
        {
            Logger.Error("Failed to load scene: " + handle.OperationException);
        }
    }

    private IEnumerator WaitForSceneInitialization(Scene scene, Action<Scene> onComplete)
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

        Utils.Try(() => onComplete?.Invoke(scene));
    }

    private void AfterWaitingForSceneInit(Scene scene)
    {
        Logger.Debug($"Scene '{scene.name}' is now initialized");

        if (
            _mod.ChunkManager.CurrentMap?.ChunkBySceneName.TryGetValue(scene.name, out var chunk)
            ?? false
        )
        {
            _mod.ChunkManager.InitializeChunkScene(chunk, scene);
        }

        Logger.Time(scene.name, "Finished chunk init");
        OnAnySceneInit?.Invoke(scene);
    }

    // Moves all scenes in the chunk by a certain amount
    public void MoveChunk(ChunkState cs, Vector3 offset)
    {
        foreach (var scene in cs.Scenes)
        {
            MoveScene(scene, offset);
        }
    }

    // Moves all objects in the scene by a certain amount
    public void MoveScene(Scene scene, Vector3 offset)
    {
        foreach (var obj in scene.GetRootGameObjects())
        {
            obj.transform.localPosition += offset;
        }
    }

    // Transition haze is the yellowish light coming from transition doorways
    public void RemoveTransitionHaze(Scene scene)
    {
        var transitionPoints = UObject.FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None);
        foreach (var point in transitionPoints)
        {
            if (
                point.gameObject.scene != scene
                || !_mod.TransitionHooks.IsTransitionDisabled(point)
            )
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

    public void RemoveSceneBorders(Scene scene)
    {
        foreach (var obj in scene.GetRootGameObjects())
        {
            if (obj.name.Contains("SceneBorder"))
                obj.SetActive(false);

            // TODO: Some are used within the scene for art, need to handle manually instead of this
            var spriteRenderers = obj.GetComponents<SpriteRenderer>()
                .Concat(obj.GetComponentsInChildren<SpriteRenderer>());
            foreach (var sr in spriteRenderers)
            {
                var name = sr.sprite?.name;
                if (name == "black_fader_moon" || name == "msk_generic")
                    sr.enabled = false;
            }
        }
    }

    public void GeneratePlayableArea(ChunkState cs, Scene scene)
    {
        var rootGameObjects = scene.GetRootGameObjects();
        var tilemap =
            rootGameObjects
                .Select(go => go.GetComponent<tk2dTileMap>())
                .FirstOrDefault(tm => tm != null)
            ?? rootGameObjects
                .Select(go => go.GetComponentsInChildren<tk2dTileMap>().FirstOrDefault())
                .FirstOrDefault(tm => tm != null);
        if (tilemap == null)
        {
            Logger.Error($"Could not find tilemap in scene '{scene.name}'");
            return;
        }
        var playableArea = TilemapUtils.CalculatePlayableArea(tilemap);
        cs.PlayableLookupTable = playableArea.lookupTable;
        cs.PlayableAreas = playableArea.perimeters;
        cs.Chunk.CalculatedPlayableBounds = playableArea.bounds;
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
}
