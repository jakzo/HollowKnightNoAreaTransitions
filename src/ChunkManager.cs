namespace HollowKnightNoAreaTransitions;

public class ChunkManager(HollowKnightNoAreaTransitionsMod mod)
{
    public ChunkMap CurrentMap;
    public Chunk StartingChunk;
    public readonly Dictionary<string, ChunkState> LoadedChunkStates = []; // key = scene name
    public event Action<ChunkState> OnChunkLoaded;
    public event Action<ChunkState> OnChunkUnloaded;

    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private readonly HashSet<Chunk> _loadedChunks = [];
    private readonly HashSet<Chunk> _pendingLoad = [];
    private readonly HashSet<Chunk> _pendingUnload = [];
    private Queue<ChunkOperation> _operations = new();
    private ChunkOperation _currentOperation = null;

    private bool IsCurrentlyLoaded(Chunk chunk) => _loadedChunks.Contains(chunk);

    private bool IsPendingLoad(Chunk chunk) => _pendingLoad.Contains(chunk);

    private bool IsPendingUnload(Chunk chunk) => _pendingUnload.Contains(chunk);

    public void Initialize() { }

    public void Deinitialize()
    {
        ImmediatelyUnloadAllChunks(true);
    }

    public void ImmediatelyUnloadAllChunks(bool keepStartingChunkLoaded = false)
    {
        _currentOperation?.Abort();
        _currentOperation = null;
        _operations.Clear();

        foreach (var chunkState in LoadedChunkStates.Values)
        {
            if (keepStartingChunkLoaded && chunkState.Chunk == StartingChunk)
                continue;

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
    public void LoadChunk(Chunk chunk, Action<Scene> onComplete = null)
    {
        if (IsPendingLoad(chunk))
        {
            // Logger.Debug($"Chunk load already queued: {chunk.SceneName}");
            return;
        }

        if (IsCurrentlyLoaded(chunk) && !IsPendingUnload(chunk))
        {
            // Logger.Debug($"Chunk already loaded: {chunk.SceneName}");
            return;
        }

        Logger.Debug($"Queueing load of chunk: {chunk.SceneName}");
        _operations.Enqueue(
            new ChunkLoadOperation(
                chunk,
                scene =>
                {
                    onComplete?.Invoke(scene);
                    OnChunkLoaded?.Invoke(LoadedChunkStates[chunk.SceneName]);
                }
            )
        );
        _pendingLoad.Add(chunk);
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
        _pendingLoad.Remove(chunk);
        _mod.SceneLoader.InitializeMainChunkScene(chunkState, scene);
    }

    public void UnloadChunk(string sceneName)
    {
        if (!LoadedChunkStates.TryGetValue(sceneName, out var chunkState))
        {
            // Logger.Debug($"No loaded chunk with scene name: {sceneName}");
            return;
        }

        UnloadChunk(chunkState);
    }

    public void UnloadChunk(ChunkState chunkState)
    {
        if (IsPendingUnload(chunkState.Chunk))
        {
            // Logger.Debug($"Chunk unload already queued: {chunkState.Chunk.SceneName}");
            return;
        }

        if (!IsCurrentlyLoaded(chunkState.Chunk))
        {
            // Logger.Debug($"Chunk not loaded, cannot unload: {chunkState.Chunk.SceneName}");
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
                    _pendingUnload.Remove(chunkState.Chunk);
                    OnChunkUnloaded?.Invoke(chunkState);
                }
            )
        );
        _pendingUnload.Add(chunkState.Chunk);
    }

    public void OnUpdate()
    {
        KeepOnlyVisibleChunksLoaded();
        HandleOperations();
    }

    private void HandleOperations()
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

    private void KeepOnlyVisibleChunksLoaded()
    {
        if (
            tk2dCamera.Instance == null
            || CurrentMap == null
            || StartingChunk == null
            || _pendingLoad.Contains(StartingChunk)
        )
            return;

        var cameraBounds = CameraBounds();
        var loadBounds = CameraBoundsWithMargin(cameraBounds, _mod.Settings.LoadDistance);
        var unloadBounds = CameraBoundsWithMargin(cameraBounds, _mod.Settings.UnloadDistance);

        // TODO: More efficient way than iterating over every chunk?
        foreach (var chunk in CurrentMap.Chunks)
        {
            // Never unload the starting chunk
            if (chunk == StartingChunk)
                continue;

            var bounds = chunk.GetPlayableWorldBounds();
            if (loadBounds.Overlaps(bounds))
                LoadChunk(chunk);
            else if (!unloadBounds.Overlaps(bounds))
                UnloadChunk(chunk.SceneName);
        }
    }

    private static Rect CameraBounds()
    {
        // TODO: Cache and move to camera class
        var cam = GameCameras.instance.tk2dCam.GetComponent<UCamera>();
        var z = cam.WorldToScreenPoint(Vector3.zero).z;
        var bottomLeft = cam.ScreenToWorldPoint(new Vector3(0f, 0f, z));
        var topRight = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, z));
        return new Rect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x - bottomLeft.x,
            topRight.y - bottomLeft.y
        );
    }

    private static Rect CameraBoundsWithMargin(Rect cameraBounds, float margin) =>
        new(
            cameraBounds.xMin - margin - SceneLoader.WORLD_OFFSET.x,
            cameraBounds.yMin - margin - SceneLoader.WORLD_OFFSET.y,
            cameraBounds.width + 2 * margin,
            cameraBounds.height + 2 * margin
        );

    // To be called when the player transitions to a new scene and the screen has faded out
    public void InitChunksOnSceneEntering(string sceneName)
    {
        // TODO: Do I need to keep the exiting chunk loaded here?
        ImmediatelyUnloadAllChunks(true);

        if (!ChunkMap.BySceneName.TryGetValue(sceneName, out var chunkMap))
            return;

        CurrentMap = chunkMap;
        StartingChunk = chunkMap.ChunkBySceneName[sceneName];
        Logger.Debug($"Entering chunk map with scene: {sceneName}");
        _pendingLoad.Add(StartingChunk);
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
            Logger.Debug($"Loading chunk {Chunk.SceneName}...");
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
