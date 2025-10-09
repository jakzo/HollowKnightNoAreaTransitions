namespace HollowKnightNoAreaTransitions;

public class Chunk
{
    public string SceneName;
    public Vector3 Position;
    public Rect[] Colliders;
    public Action<Scene> OnLoad;
    public Rect PlayableBounds;
    public ChunkCalculatedInfo Calculated;

    public Rect? GetTilemapBounds()
    {
        if (Calculated == null)
            return null;
        return new Rect(Position.x, Position.y, Calculated.TilemapSize.x, Calculated.TilemapSize.y);
    }

    public Rect GetPlayableChunkMapBounds()
    {
        var sceneBounds = Calculated != null ? Calculated.PlayableBounds : PlayableBounds;
        return new Rect(
            Position.x + sceneBounds.x,
            Position.y + sceneBounds.y,
            sceneBounds.width,
            sceneBounds.height
        );
    }
}

public class ChunkCalculatedInfo
{
    public IntVector2 TilemapSize;
    public Rect PlayableBounds;
    public List<Vector3[]> PlayableAreas;
    public bool[,] PlayableLookupTable; // [x, y]
    public Dictionary<string, Transition> Transitions;
    public HashSet<Chunk> OverlappingChunks = [];
    public HashSet<IntVector2> TilesToRemove = [];
}

public class Transition
{
    public Direction Direction;
    public IntVector2 Position; // bottom or left of transition (depending on direction)
    public int Size;
    public string TargetSceneName;
    public string TargetTransitionName;
}

public class ChunkState(Chunk chunk, Scene mainScene)
{
    public Chunk Chunk = chunk;
    public Scene MainScene = mainScene;
    public List<Scene> Scenes = [mainScene];
    public tk2dTileMap Tilemap = Utils.Tilemap.GetTilemap(mainScene);
    public bool IsFrozen
    {
        get => FrozenBehaviours != null;
    }
    public List<Behaviour> FrozenBehaviours;
    public List<Rigidbody2D> FrozenRigidbodies;
}

public class ChunkMap(List<Chunk> chunks)
{
    public static Dictionary<string, ChunkMap> BySceneName = [];

    public static void Register(ChunkMap chunkMap)
    {
        if (chunkMap.Chunks.Any(c => BySceneName.ContainsKey(c.SceneName)))
            throw new ArgumentException(
                "Chunk map contains scenes already part of another chunk map"
            );

        foreach (var chunk in chunkMap.Chunks)
            BySceneName.Add(chunk.SceneName, chunkMap);
    }

    public static void RegisterChunkToMap(Chunk chunk, ChunkMap chunkMap)
    {
        if (BySceneName.ContainsKey(chunk.SceneName))
            throw new ArgumentException("A chunk map already contains a chunk with that scene");
        BySceneName.Add(chunk.SceneName, chunkMap);
        chunkMap.Add(chunk);
    }

    public static ChunkMap CreateAndRegister(List<Chunk> chunks)
    {
        var chunkMap = new ChunkMap(chunks);
        Register(chunkMap);
        return chunkMap;
    }

    public static void Remove(Chunk chunk)
    {
        if (!BySceneName.TryGetValue(chunk.SceneName, out var chunkMap))
            throw new ArgumentException("Chunk map does not exist with that scene");
        chunkMap.Chunks.Remove(chunk);
        chunkMap.ChunkBySceneName.Remove(chunk.SceneName);
        BySceneName.Remove(chunk.SceneName);
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
