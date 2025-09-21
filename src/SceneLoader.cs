namespace HollowKnightNoAreaTransitions;

public class SceneLoader
{
    public static readonly Vector3 WORLD_OFFSET = new(200f, 200f, 0f);
    private static readonly MethodInfo _unloadSceneMethod = typeof(USceneManager).GetMethod(
        nameof(USceneManager.UnloadScene),
        new Type[] { typeof(string) }
    );

    private static int LAYER_TERRAIN;
    private static int LAYER_PLAYER;
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

    public readonly Dictionary<string, ChunkState> LoadedChunks = new();
    public event Action<Scene> OnSceneInit;

    private readonly HollowKnightNoAreaTransitionsMod _mod;
    private Hook _unloadSceneHook;

    public SceneLoader(HollowKnightNoAreaTransitionsMod mod)
    {
        _mod = mod;
    }

    public void Initialize()
    {
        LAYER_TERRAIN = LayerMask.NameToLayer("Terrain");
        LAYER_PLAYER = LayerMask.NameToLayer("Player");
        PHYSICS_MATERIAL_TERRAIN = Resources
            .FindObjectsOfTypeAll<PhysicsMaterial2D>()
            .First(m => m.name == "Terrain");
    }

    public void Deinitialize()
    {
        _unloadSceneHook?.Dispose();
    }

    // Called when the main scene of a chunk has just finished loading and
    // initializes the chunk and scene
    public void OnChunkSceneLoaded(Chunk chunk)
    {
        var scene = USceneManager.GetSceneByName(chunk.SceneName);
        if (!scene.isLoaded)
        {
            throw new Exception($"Chunk scene was not loaded: {chunk.SceneName}");
        }
        if (!(_mod.CurrentMap?.ChunkBySceneName.ContainsKey(chunk.SceneName) ?? false))
        {
            Logger.Warning("Scene finished loading but its chunk map is not active anymore");
            return;
        }

        var cs = new ChunkState(chunk, scene);
        LoadedChunks.Add(chunk.SceneName, cs);
        InitializeScene(cs, scene);
    }

    // Moves the scene to the correct position in the game world, creates
    // transition passageway colliders, etc.
    public void InitializeScene(ChunkState cs, Scene scene)
    {
        MoveScene(scene, cs.Chunk.Position + WORLD_OFFSET);
        CreateColliders(cs);
        OnSceneInit?.Invoke(scene);
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

    // Creates extra colliders for passageways between rooms for when neighboring
    // rooms are not touching, should be called after moving the scene
    private void CreateColliders(ChunkState cs)
    {
        if (cs.Chunk.Colliders == null)
            return;
        Logger.Debug($"CreateColliders: {cs.Chunk.SceneName}");

        var parent = new GameObject("OneLevel_TransitionColliders").transform;
        USceneManager.MoveGameObjectToScene(parent.gameObject, cs.MainScene);
        parent.localPosition = cs.Chunk.Position + WORLD_OFFSET;

        foreach (var rect in cs.Chunk.Colliders)
        {
            var go = new GameObject("OneLevel_TransitionCollider");
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
                vertices = new Vector3[]
                {
                    new(0f, 0f),
                    new(rect.width, 0f),
                    new(0f, rect.height),
                    new(rect.width, rect.height),
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
            meshFilter.mesh.RecalculateNormals();

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Sprites/Lit"))
            {
                color = new Color(0.1f, 0.1f, 0.2f),
            };
        }
    }
}
