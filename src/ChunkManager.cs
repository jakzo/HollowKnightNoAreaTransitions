using UnityEngine.AddressableAssets;

namespace HollowKnightNoAreaTransitions;

public class ChunkManager(HollowKnightNoAreaTransitionsMod mod)
{
    public ChunkMap CurrentMap;
    public Chunk StartingChunk;
    public readonly Dictionary<string, ChunkState> LoadedChunkStates = []; // key = scene name

    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private readonly HashSet<Chunk> _loadedChunks = [];
    private Queue<ChunkOperation> _operations = new();
    private ChunkOperation _currentOperation = null;

    private bool IsCurrentlyLoaded(Chunk chunk) => _loadedChunks.Contains(chunk);

    private bool IsPendingLoad(Chunk chunk) =>
        _operations.Any(op => op is ChunkLoadOperation && op.Chunk == chunk);

    private bool IsPendingUnload(Chunk chunk) =>
        _operations.Any(op => op is ChunkUnloadOperation && op.Chunk == chunk);

    public void Initialize() { }

    public void Deinitialize()
    {
        ImmediatelyUnloadAllChunks();
    }

    public void ImmediatelyUnloadAllChunks()
    {
        _currentOperation?.Abort();
        _currentOperation = null;
        _operations.Clear();

        foreach (var chunkState in LoadedChunkStates.Values)
        {
            foreach (var scene in chunkState.Scenes)
            {
                if (!scene.isLoaded)
                    continue;
                USceneManager.UnloadSceneAsync(scene);
            }
        }

        LoadedChunkStates.Clear();
        _loadedChunks.Clear();
        CurrentMap = null;
        StartingChunk = null;
    }

    // Allowed states for a chunk in the operations queue are:
    // - Not loaded -> Pending Load
    // - Loaded -> Pending Unload
    // - Loaded -> Pending Unload -> Pending Load
    // Any other combination, ordering or duplicates are not allowed
    public void LoadChunk(Chunk chunk)
    {
        if (IsPendingLoad(chunk))
        {
            Logger.Debug($"Chunk load already queued: {chunk.SceneName}");
            return;
        }

        if (IsCurrentlyLoaded(chunk) && !IsPendingUnload(chunk))
        {
            Logger.Debug($"Chunk already loaded: {chunk.SceneName}");
            return;
        }

        Logger.Debug($"Queueing load of chunk: {chunk.SceneName}");
        _operations.Enqueue(new ChunkLoadOperation(chunk));
    }

    public void InitializeChunkScene(Chunk chunk, Scene scene)
    {
        if (!scene.isLoaded)
            throw new Exception($"Chunk scene was not loaded: {chunk.SceneName}");

        if (!(CurrentMap?.ChunkBySceneName.ContainsKey(chunk.SceneName) ?? false))
        {
            Logger.Warning(
                $"Scene finished loading but its chunk map is not active anymore: {chunk.SceneName}"
            );
            USceneManager.UnloadSceneAsync(scene);
            return;
        }

        var chunkState = new ChunkState(chunk, scene);
        LoadedChunkStates.Add(chunk.SceneName, chunkState);
        _loadedChunks.Add(chunk);
        _mod.SceneLoader.InitializeScene(chunkState, scene);
    }

    public void UnloadChunk(string sceneName)
    {
        if (!LoadedChunkStates.TryGetValue(sceneName, out var chunkState))
        {
            Logger.Debug($"No loaded chunk with scene name: {sceneName}");
            return;
        }

        UnloadChunk(chunkState);
    }

    public void UnloadChunk(ChunkState chunkState)
    {
        if (IsPendingLoad(chunkState.Chunk))
        {
            Logger.Debug($"Removing pending chunk load from queue: {chunkState.Chunk.SceneName}");
            _operations = new Queue<ChunkOperation>(
                _operations.Where(op => !(op is ChunkLoadOperation && op.Chunk == chunkState.Chunk))
            );
            return;
        }

        if (IsPendingUnload(chunkState.Chunk))
        {
            Logger.Debug($"Chunk unload already queued: {chunkState.Chunk.SceneName}");
            return;
        }

        if (!IsCurrentlyLoaded(chunkState.Chunk))
        {
            Logger.Debug($"Chunk not loaded, cannot unload: {chunkState.Chunk.SceneName}");
            return;
        }

        Logger.Debug($"Queueing unload of chunk: {chunkState.Chunk.SceneName}");
        _operations.Enqueue(
            new ChunkUnloadOperation(
                chunkState,
                () =>
                {
                    LoadedChunkStates.Remove(chunkState.Chunk.SceneName);
                    _loadedChunks.Remove(chunkState.Chunk);
                }
            )
        );
    }

    public void OnUpdate()
    {
        if (_currentOperation == null)
        {
            if (_operations.Count == 0)
                return;

            _currentOperation = _operations.Dequeue();
        }

        var isDone = _currentOperation.OnUpdate();
        if (!isDone)
            return;

        _currentOperation = null;
    }

    // To be called when the player transitions to a new scene and the screen has faded out
    public void InitChunksOnSceneEntering(string sceneName)
    {
        var sceneIsInCurrentChunkMap = CurrentMap?.ChunkBySceneName.ContainsKey(sceneName) ?? false;
        if (!sceneIsInCurrentChunkMap)
            ImmediatelyUnloadAllChunks();

        if (CurrentMap == null)
        {
            if (!ChunkMap.BySceneName.TryGetValue(sceneName, out var chunkMap))
                return;

            CurrentMap = chunkMap;
            StartingChunk = chunkMap.ChunkBySceneName[sceneName];
            Logger.Debug($"Entering chunk map with scene: {sceneName}");
            // Let the game load the entered scene normally (then init it ourselves)
            // TODO: Load nearby chunks first, load only visible chunks, etc.
            LoadAllChunksExcept(sceneName);
            return;
        }

        UnloadChunk(sceneName);
        // Let the game load the entered scene normally (then init it ourselves)
    }

    public void LoadAllChunksExcept(string exceptForSceneName = null)
    {
        if (CurrentMap == null)
            throw new Exception("No current chunk map to load chunks from");

        foreach (var chunk in CurrentMap.Chunks)
        {
            if (chunk.SceneName == exceptForSceneName)
                continue;

            LoadChunk(chunk);
        }
    }
}

abstract class ChunkOperation(Chunk chunk)
{
    public Chunk Chunk = chunk;
    public abstract bool OnUpdate();
    public abstract void Abort();
}

class ChunkLoadOperation(Chunk chunk, Action<Scene> onComplete = null) : ChunkOperation(chunk)
{
    public AsyncOperationHandle<SceneInstance> LoadOperation;

    public override bool OnUpdate()
    {
        if (!LoadOperation.IsValid())
        {
            LoadOperation = HollowKnightNoAreaTransitionsMod.Instance.SceneLoader.LoadSceneAsync(
                Chunk.SceneName,
                scene => onComplete?.Invoke(scene)
            );
        }

        if (!LoadOperation.IsDone)
            return false;

        return true;
    }

    public override void Abort()
    {
        if (!LoadOperation.IsValid() || LoadOperation.IsDone)
            return;

        Logger.Debug($"Aborting load operation for chunk: {Chunk.SceneName}");
        LoadOperation.Completed += _ => USceneManager.UnloadSceneAsync(LoadOperation.Result.Scene);
    }
}

class ChunkUnloadOperation(ChunkState chunkState, Action onStart) : ChunkOperation(chunkState.Chunk)
{
    public ChunkState ChunkState = chunkState;
    public int SceneIndex = 0;
    public AsyncOperation UnloadOperation;
    public bool HasStarted = false;

    public override bool OnUpdate()
    {
        if (UnloadOperation != null && !UnloadOperation.isDone)
            return false;

        if (!HasStarted)
        {
            HasStarted = true;
            onStart?.Invoke();
        }

        UnloadOperation = null;
        if (SceneIndex >= ChunkState.Scenes.Count)
            return true;

        var scene = ChunkState.Scenes[SceneIndex++];
        if (scene.IsValid() && scene.isLoaded)
            UnloadOperation = USceneManager.UnloadSceneAsync(scene);
        return false;
    }

    public override void Abort()
    {
        // There is nothing we can do to abort an unload operation
    }
}
