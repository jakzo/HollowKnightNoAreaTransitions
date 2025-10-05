namespace HollowKnightNoAreaTransitions;

public class ChunkManager(HollowKnightNoAreaTransitionsMod mod)
{
    public ChunkMap CurrentMap;
    public Chunk StartingChunk;
    public readonly Dictionary<string, ChunkState> LoadedChunkStates = []; // key = scene name
    public event Action<ChunkState> OnChunkLoaded; // when chunk loaded and initialized by us
    public event Action<ChunkState> OnChunkUnloaded; // when chunk finishes unloading by us
    public event Action<Scene> OnAnySceneInit; // when any area scene initialized by us or game
    public event Action<Scene> OnChunkSceneInit; // when chunk loaded and initialized by us or game

    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private readonly HashSet<Chunk> _loadedChunks = [];
    private readonly Dictionary<Chunk, ChunkLoadOperation> _pendingLoad = [];
    private readonly Dictionary<Chunk, ChunkUnloadOperation> _pendingUnload = [];
    private ChunkOperation _currentOperation = null;

    public void Initialize() { }

    public void Deinitialize()
    {
        ImmediatelyUnloadAllChunks(true);
    }

    public void ImmediatelyUnloadAllChunks(bool keepStartingChunkLoaded = false)
    {
        _currentOperation?.Abort();
        _currentOperation = null;

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
        _pendingLoad.Clear();
        _pendingUnload.Clear();
        _currentOperation?.Abort();
        _currentOperation = null;
        CurrentMap = null;
        StartingChunk = null;
    }

    // Allowed states for a chunk in the operations queue are:
    // - Not loaded -> Pending Load
    // - Loaded -> Pending Unload
    // - Loaded -> Pending Unload (uncancellable) -> Pending Load
    // Any other combination, ordering or duplicates are not allowed
    public void LoadChunk(Chunk chunk, Action<Scene> onComplete = null)
    {
        if (_pendingLoad.ContainsKey(chunk))
        {
            // Logger.Debug($"Chunk load already queued: {chunk.SceneName}");
            return;
        }

        if (_loadedChunks.Contains(chunk))
        {
            if (!_pendingUnload.TryGetValue(chunk, out var unloadOp))
            {
                // Logger.Debug($"Chunk already loaded: {chunk.SceneName}");
                return;
            }

            if (unloadOp.CanAbort())
            {
                Logger.Debug($"Aborting pending unload instead of loading: {chunk.SceneName}");
                unloadOp.Abort();
                _pendingUnload.Remove(chunk);
                return;
            }
        }

        Logger.Debug($"Queueing load of chunk: {chunk.SceneName}");
        _pendingLoad.Add(
            chunk,
            new ChunkLoadOperation(
                chunk,
                scene =>
                {
                    AfterWaitingForSceneInit(scene);
                    onComplete?.Invoke(scene);
                    OnChunkLoaded?.Invoke(LoadedChunkStates[chunk.SceneName]);
                }
            )
        );
    }

    public void HandleSceneLoadedByGame(AsyncOperationHandle<SceneInstance> handle)
    {
        var scene = handle.Result.Scene;
        SceneLoader.RunAfterSceneHasInitialized(scene, AfterWaitingForSceneInit);
    }

    private void AfterWaitingForSceneInit(Scene scene)
    {
        Logger.Debug($"Scene '{scene.name}' is now initialized");

        if (CurrentMap?.ChunkBySceneName.TryGetValue(scene.name, out var chunk) ?? false)
        {
            if (
                _pendingLoad.TryGetValue(chunk, out var op)
                && op is StartingChunkLoadOperation startingOp
            )
            {
                startingOp.MarkAsComplete();
            }

            InitializeChunkScene(chunk, scene);
        }

        // Logger.Time(scene.name, "Finished chunk init");
        OnAnySceneInit?.Invoke(scene);
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
        OnChunkSceneInit?.Invoke(scene);
    }

    public void UnloadChunk(string sceneName, bool isCancellable)
    {
        if (!(CurrentMap?.ChunkBySceneName.TryGetValue(sceneName, out var chunkState) ?? false))
        {
            // Logger.Debug($"No chunk with scene name in current map: {sceneName}");
            return;
        }

        UnloadChunk(chunkState, isCancellable);
    }

    public void UnloadChunk(Chunk chunk, bool isCancellable)
    {
        if (_pendingUnload.ContainsKey(chunk))
        {
            // Logger.Debug($"Chunk unload already queued: {chunk.SceneName}");
            return;
        }

        if (_pendingLoad.TryGetValue(chunk, out var loadOp) && loadOp.CanAbort())
        {
            Logger.Debug($"Cancelling pending chunk load: {chunk.SceneName}");
            loadOp.Abort();
            _pendingLoad.Remove(chunk);
            if (!_loadedChunks.Contains(chunk))
                return;
        }
        else if (!_loadedChunks.Contains(chunk))
        {
            // Logger.Debug($"Chunk not loaded, cannot unload: {chunk.SceneName}");
            return;
        }

        if (!LoadedChunkStates.TryGetValue(chunk.SceneName, out var chunkState))
        {
            // Logger.Debug($"No loaded chunk with scene name: {chunk.SceneName}");
            return;
        }

        Logger.Debug($"Queueing unload of chunk: {chunk.SceneName}");
        _pendingUnload.Add(
            chunk,
            new ChunkUnloadOperation(
                chunkState,
                isCancellable,
                () =>
                {
                    LoadedChunkStates.Remove(chunk.SceneName);
                    _loadedChunks.Remove(chunk);
                    _pendingUnload.Remove(chunk);
                    Logger.Debug($"Chunk unloaded: {chunk.SceneName}");
                    OnChunkUnloaded?.Invoke(chunkState);
                }
            )
        );
    }

    public void OnUpdate()
    {
        KeepOnlyVisibleChunksLoaded();
        HandleOperations();
    }

    private ChunkOperation PeekNextOperation()
    {
        if (_currentOperation != null)
            return _currentOperation;
        if (_pendingUnload.Count > 0)
            return _pendingUnload.Values.First();
        if (_pendingLoad.Count > 0)
            return _pendingLoad.Values.First();
        return null;
    }

    // TODO: Load chunks in order of distance to player
    private void HandleOperations()
    {
        if (_currentOperation == null)
        {
            var nextOp = PeekNextOperation();
            if (nextOp == null)
                return;

            _currentOperation = nextOp;
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
            || _pendingLoad.ContainsKey(StartingChunk)
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
                UnloadChunk(chunk.SceneName, true);
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
        _pendingLoad.Add(StartingChunk, new StartingChunkLoadOperation(StartingChunk));
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
    public abstract bool CanAbort();
    public abstract void Abort();
}

class ChunkLoadOperation(Chunk chunk, Action<Scene> onComplete = null) : ChunkOperation(chunk)
{
    public AsyncOperationHandle<SceneInstance> LoadOperation;

    public override bool OnUpdate()
    {
        if (!LoadOperation.IsValid())
            LoadOperation = SceneLoader.LoadSceneAsync(Chunk.SceneName, onComplete);
        return LoadOperation.IsDone;
    }

    public override bool CanAbort() => !LoadOperation.IsValid() || !LoadOperation.IsDone;

    public override void Abort()
    {
        if (!LoadOperation.IsValid() || LoadOperation.IsDone)
            return;

        Logger.Debug($"Aborting load operation for chunk: {Chunk.SceneName}");
        LoadOperation.Completed += _ => USceneManager.UnloadSceneAsync(LoadOperation.Result.Scene);
    }
}

class StartingChunkLoadOperation(Chunk chunk) : ChunkLoadOperation(chunk)
{
    private bool _isComplete = false;

    public override bool OnUpdate()
    {
        return _isComplete;
    }

    public override bool CanAbort() => false;

    public override void Abort()
    {
        // Cannot abort starting chunk load since it was initiated by the game
    }

    public void MarkAsComplete()
    {
        _isComplete = true;
    }
}

class ChunkUnloadOperation(ChunkState chunkState, bool isCancellable, Action onStart)
    : ChunkOperation(chunkState.Chunk)
{
    public ChunkState ChunkState = chunkState;
    public int SceneIndex = 0;
    public AsyncOperation UnloadOperation;
    public bool HasStarted = false;
    public bool isCancellable = isCancellable;

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

    public override bool CanAbort() => isCancellable && !HasStarted;

    public override void Abort()
    {
        // There is nothing we can do to abort an unload operation
    }
}
