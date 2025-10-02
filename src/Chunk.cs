namespace HollowKnightNoAreaTransitions;

public class Chunk
{
    public string SceneAssetPath;
    public string SceneName;
    public Vector3 Position;
    public Rect[] Colliders;
    public Action<Scene> OnLoad;
    public Rect PlayableBounds;

    public Rect? CalculatedPlayableBounds; // Computed at load from tilemap

    public Rect GetPlayableWorldBounds()
    {
        var sceneBounds =
            CalculatedPlayableBounds != null ? CalculatedPlayableBounds.Value : PlayableBounds;
        return new Rect(
            Position.x + sceneBounds.x,
            Position.y + sceneBounds.y,
            sceneBounds.width,
            sceneBounds.height
        );
    }
}

public class ChunkState(Chunk chunk, Scene mainScene)
{
    public Chunk Chunk = chunk;
    public Scene MainScene = mainScene;
    public List<Scene> Scenes = [mainScene];

    // Computed at load from tilemap
    public bool[,] PlayableLookupTable; // [x, y]
    public List<Vector3[]> PlayableAreas;
}

public class ChunkMap(List<Chunk> chunks)
{
    public static Dictionary<string, ChunkMap> BySceneName = [];

    public static void Register(ChunkMap chunkMap)
    {
        if (chunkMap.Chunks.Any(c => BySceneName.ContainsKey(c.SceneName)))
        {
            throw new ArgumentException(
                "Chunk map contains scenes already part of another chunk map"
            );
        }
        foreach (var chunk in chunkMap.Chunks)
        {
            BySceneName.Add(chunk.SceneName, chunkMap);
        }
    }

    public static ChunkMap CreateAndRegister(List<Chunk> chunks)
    {
        var chunkMap = new ChunkMap(chunks);
        Register(chunkMap);
        return chunkMap;
    }

    public List<Chunk> Chunks = chunks;
    public Dictionary<string, Chunk> ChunkBySceneName = chunks.ToDictionary(chunk =>
        chunk.SceneName
    );

    public void Add(Chunk chunk)
    {
        if (ChunkBySceneName.ContainsKey(chunk.SceneName))
            throw new ArgumentException("Chunk map already contains a chunk with that scene");
        Chunks.Add(chunk);
        ChunkBySceneName.Add(chunk.SceneName, chunk);
    }
}
