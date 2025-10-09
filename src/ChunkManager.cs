namespace HollowKnightNoAreaTransitions;

public class ChunkManager(HollowKnightNoAreaTransitionsMod mod)
{
    public ChunkMap CurrentMap;
    public Chunk StartingChunk;
    public ChunkState CurrentChunk;
    public readonly Dictionary<string, ChunkState> LoadedChunkStates = []; // key = scene name
    public event Action<ChunkState> OnChunkLoaded; // when chunk loaded and initialized by us
    public event Action<ChunkState> OnChunkUnloaded; // when chunk finishes unloading by us
    public event Action<Scene> OnAnySceneInit; // when any area scene initialized by us or game
    public event Action<Scene> OnChunkSceneInit; // when chunk loaded and initialized by us or game

    private readonly HashSet<Chunk> _loadedChunks = [];
    private readonly Dictionary<Chunk, ChunkLoadOperation> _pendingLoad = [];
    private readonly Dictionary<Chunk, ChunkUnloadOperation> _pendingUnload = [];
    private ChunkOperation _currentOperation = null;
    private readonly Dictionary<
        Chunk,
        (Chunk fromChunk, Transition fromTransition)
    > _chunksToGenerate = [];

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
    public void LoadChunk(Chunk chunk, Action<Scene> onComplete = null, bool isCancellable = true)
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
                isCancellable,
                scene =>
                {
                    var chunkState = AfterWaitingForSceneInit(scene);
                    onComplete?.Invoke(scene);
                    OnChunkLoaded?.Invoke(chunkState);

#if DEBUG
                    if (mod.Settings.DebugOnlyShowOutlines)
                        UnloadChunk(chunkState, false);
#endif
                }
            )
        );
    }

    public void HandleSceneLoadedByGame(AsyncOperationHandle<SceneInstance> handle)
    {
        var scene = handle.Result.Scene;
        SceneLoader.RunAfterSceneHasInitialized(scene, scene => AfterWaitingForSceneInit(scene));
    }

    private ChunkState AfterWaitingForSceneInit(Scene scene)
    {
        ChunkState chunkState;

        // TODO: What if the scene is not the main scene of a chunk (eg. boss scene)?
        if (
            scene.isLoaded
            && (CurrentMap?.ChunkBySceneName.TryGetValue(scene.name, out var chunk) ?? false)
        )
        {
            Logger.Debug($"Scene '{scene.name}' is now initialized");
            if (
                _pendingLoad.TryGetValue(chunk, out var op)
                && op is StartingChunkLoadOperation startingOp
            )
            {
                startingOp.MarkAsComplete();
            }

            chunkState = InitializeChunkScene(chunk, scene);
        }
        else
        {
            Logger.Warning(
                $"Scene finished loading but its chunk map is not active anymore: {scene.name}"
            );
            USceneManager.UnloadSceneAsync(scene);
            return null;
        }

        // Logger.Time(scene.name, "Finished chunk init");
        OnAnySceneInit?.Invoke(scene);
        return chunkState;
    }

    public ChunkState InitializeChunkScene(Chunk chunk, Scene scene)
    {
        var chunkState = new ChunkState(chunk, scene);
        LoadedChunkStates.Add(chunk.SceneName, chunkState);
        _loadedChunks.Add(chunk);
        _pendingLoad.Remove(chunk);
        mod.SceneLoader.InitializeMainChunkScene(
            chunkState,
            scene,
            doNotMove: _chunksToGenerate.ContainsKey(chunk) // correct position will be set when generating after this
        );
        OnChunkSceneInit?.Invoke(scene);

        if (_chunksToGenerate.TryGetValue(chunk, out var from))
        {
            Logger.Debug($"Generating chunk for scene: {chunk.SceneName}");
            Generate(chunkState, from.fromChunk, from.fromTransition);
            _chunksToGenerate.Remove(chunk);
        }

        if (mod.Settings.GenerateChunkMaps)
        {
            foreach (var transition in chunk.Calculated.Transitions.Values)
            {
                if (
                    transition.Direction == Direction.None
                    || ChunkMap.BySceneName.ContainsKey(transition.TargetSceneName)
                )
                    continue;

                var tpChunk = new Chunk()
                {
                    SceneName = transition.TargetSceneName,
                    // Temporary position for loading in order of distance to player, will be updated when generating
                    Position = new Vector3(
                        chunk.Position.x + transition.Position.x,
                        chunk.Position.y + transition.Position.y,
                        0
                    ),
                };
                ChunkMap.RegisterChunkToMap(tpChunk, CurrentMap);
                _chunksToGenerate.Add(tpChunk, (chunk, transition));
                LoadChunk(tpChunk);
            }
        }

        if (chunkState.Chunk == StartingChunk)
        {
            HeroController.instance.transform.localPosition +=
                StartingChunk.Position + SceneLoader.WORLD_OFFSET;
        }

        if (mod.Settings.FreezeOtherScenes && CurrentChunk != chunkState)
            SceneLoader.SetChunkFrozen(chunkState, true);

        return chunkState;
    }

    public void Generate(ChunkState chunkState, Chunk fromChunk, Transition fromTransition)
    {
        var success = LineUpChunkWithTransition(chunkState.Chunk, fromChunk, fromTransition);
        if (!success)
        {
            Logger.Warning("Removing chunk from map since it could not be aligned");
            ChunkMap.Remove(chunkState.Chunk);
            return;
        }
        mod.SceneLoader.MoveChunk(chunkState, chunkState.Chunk.Position + SceneLoader.WORLD_OFFSET);

#if DEBUG
        if (mod.Settings.DebugOnlyShowOutlines)
        {
            var go = HKNAT.ShowPlayableAreas(chunkState.Chunk);
            go.name = $"HKNAT_PlayableAreas {chunkState.Chunk.SceneName}";
            var parent = GameObject.Find("HKNAT_PlayableAreas_All");
            if (parent == null)
            {
                parent = new GameObject("HKNAT_PlayableAreas_All");
                parent.transform.position = SceneLoader.WORLD_OFFSET;
            }
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = chunkState.Chunk.Position;
        }
#endif
    }

    public static bool LineUpChunkWithTransition(
        Chunk chunk,
        Chunk fromChunk,
        Transition fromTransition
    )
    {
        if (
            !chunk.Calculated.Transitions.TryGetValue(
                fromTransition.TargetTransitionName,
                out var toTransition
            )
        )
        {
            Logger.Error(
                $"Could not find entry point in scene {chunk.SceneName} with name {fromTransition.TargetTransitionName}"
            );
            return false;
        }

        if (toTransition.Direction == Direction.None || fromTransition.Direction == Direction.None)
        {
            Logger.Warning(
                $"Cannot line up chunk {chunk.SceneName} since one of the transitions has no direction"
            );
            return false;
        }

        if (toTransition.Direction != Utils.Game.OppositeDirection(fromTransition.Direction))
        {
            Logger.Warning(
                $"Cannot line up chunk {chunk.SceneName} since the transitions have different directions"
            );
            return false;
        }

        // Line up transitions
        var fromSceneBounds = fromChunk.GetTilemapBounds().Value;
        var toSceneWidth = chunk.Calculated.TilemapSize.x;
        float newSceneX;
        float newSceneY;
        const float MAX_TRANSITION_GAP = 10f;
        // TODO: Both cases have the same logic just with axes flipped
        if (toTransition.Direction == Direction.Left || toTransition.Direction == Direction.Right)
        {
            // Line up bottom of transitions
            var diffY = toTransition.Position.y - fromTransition.Position.y;
            newSceneY = fromChunk.Position.y - diffY;
            // Line up chunk tiles
            newSceneX =
                fromTransition.Direction == Direction.Right
                    ? fromSceneBounds.xMax
                    : fromSceneBounds.xMin - toSceneWidth;
            var toTransitionX = newSceneX + toTransition.Position.x;
            var fromTransitionX = fromSceneBounds.xMin + fromTransition.Position.x;
            var diffX = toTransitionX - fromTransitionX;
            if (Mathf.Abs(diffX) > MAX_TRANSITION_GAP)
                newSceneX -= diffX; // bring transitions together if there is excessive space between them
        }
        else
        {
            // Line up X axis center of transitions
            var diffX = toTransition.Position.x - fromTransition.Position.x;
            newSceneX = fromChunk.Position.x - diffX;
            // Line up chunk tiles
            newSceneY =
                fromTransition.Direction == Direction.Up
                    ? fromSceneBounds.yMax
                    : fromSceneBounds.yMin - chunk.Calculated.TilemapSize.y;
            var toTransitionY = newSceneY + toTransition.Position.y;
            var fromTransitionY = fromSceneBounds.yMin + fromTransition.Position.y;
            var diffY = toTransitionY - fromTransitionY;
            if (Mathf.Abs(diffY) > MAX_TRANSITION_GAP)
                newSceneY -= diffY; // bring transitions together if there is excessive space between them
        }
        chunk.Position = new Vector3(Mathf.Round(newSceneX), Mathf.Round(newSceneY), 0f);
        return true;
    }

    public void UnloadChunk(string sceneName, bool isCancellable)
    {
        if (!LoadedChunkStates.TryGetValue(sceneName, out var chunkState))
        {
            // Logger.Debug($"No chunk with scene name currently loaded: {sceneName}");
            return;
        }

        UnloadChunk(chunkState, isCancellable);
    }

    public void UnloadChunk(ChunkState cs, bool isCancellable)
    {
        if (_chunksToGenerate.ContainsKey(cs.Chunk))
        {
            // Logger.Debug($"Cannot unload because chunk generation is pending: {chunk.SceneName}");
            return;
        }

        if (_pendingUnload.ContainsKey(cs.Chunk))
        {
            // Logger.Debug($"Chunk unload already queued: {chunk.SceneName}");
            return;
        }

        if (_pendingLoad.TryGetValue(cs.Chunk, out var loadOp) && loadOp.CanAbort())
        {
            Logger.Debug($"Cancelling pending chunk load: {cs.Chunk.SceneName}");
            loadOp.Abort();
            _pendingLoad.Remove(cs.Chunk);
            if (!_loadedChunks.Contains(cs.Chunk))
                return;
        }
        else if (!_loadedChunks.Contains(cs.Chunk))
        {
            // Logger.Debug($"Chunk not loaded, cannot unload: {chunk.SceneName}");
            return;
        }

        Logger.Debug($"Queueing unload of chunk: {cs.Chunk.SceneName}");
        _pendingUnload.Add(
            cs.Chunk,
            new ChunkUnloadOperation(
                cs,
                isCancellable,
                () =>
                {
                    LoadedChunkStates.Remove(cs.Chunk.SceneName);
                    _loadedChunks.Remove(cs.Chunk);
                    _pendingUnload.Remove(cs.Chunk);
                    Logger.Debug($"Chunk unloaded: {cs.Chunk.SceneName}");
                    OnChunkUnloaded?.Invoke(cs);
                }
            )
        );
    }

    public void OnUpdate()
    {
        KeepOnlyVisibleChunksLoaded();
        HandleOperations();
        UpdateCurrentScene();
    }

    public void UpdateCurrentScene()
    {
        if (HeroController.instance == null)
            return;
        var pos = HeroController.instance.transform.position;
        if (CurrentChunk != null && IsPositionInChunkPlayableArea(pos, CurrentChunk.Chunk))
            return;
        // TODO: Faster way to get nearby chunks than checking all
        var cs = LoadedChunkStates.Values.FirstOrDefault(c =>
            IsPositionInChunkPlayableArea(pos, c.Chunk)
        );
        if (cs == null)
            return;
        var prevChunkState = CurrentChunk;
        CurrentChunk = cs;
        Logger.Debug($"Player moved to chunk: {cs.Chunk.SceneName}");

        if (mod.Settings.FreezeOtherScenes)
        {
            if (prevChunkState != null)
                SceneLoader.SetChunkFrozen(prevChunkState, true);
            SceneLoader.SetChunkFrozen(CurrentChunk, false);
        }
    }

    private static bool IsPositionInChunkPlayableArea(Vector3 pos, Chunk chunk)
    {
        if (chunk.Calculated == null)
            return false;
        var x = (int)(pos.x - SceneLoader.WORLD_OFFSET.x - chunk.Position.x);
        var y = (int)(pos.y - SceneLoader.WORLD_OFFSET.y - chunk.Position.y);
        if (
            x < 0
            || y < 0
            || x >= chunk.Calculated.TilemapSize.x
            || y >= chunk.Calculated.TilemapSize.y
        )
            return false;
        return chunk.Calculated.PlayableLookupTable[x, y];
    }

    private ChunkOperation PeekNextOperation()
    {
        if (_currentOperation != null)
            return _currentOperation;
        if (_pendingUnload.Count > 0)
            return _pendingUnload.Values.First();
        if (_pendingLoad.Count > 0)
        {
            // Load nearest chunk to player first
            var playerPos = HeroController.instance?.transform.position - SceneLoader.WORLD_OFFSET;
            if (playerPos == null)
                return _pendingLoad.Values.First();
            return _pendingLoad
                .Values.OrderBy(c =>
                    Utils.Unity.PointToRectDistSqr(
                        playerPos.Value,
                        c.Chunk.GetPlayableChunkMapBounds()
                    )
                )
                .First();
        }
        return null;
    }

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
#if DEBUG
            || mod.Settings.DebugOnlyShowOutlines
#endif
        )
            return;

        var cameraBounds = CameraBounds();
        var loadBounds = CameraBoundsWithMargin(cameraBounds, mod.Settings.LoadDistance);
        var unloadBounds = CameraBoundsWithMargin(cameraBounds, mod.Settings.UnloadDistance);

        // TODO: More efficient way than iterating over every chunk?
        foreach (var chunk in CurrentMap.Chunks)
        {
            if (chunk.GetPlayableChunkMapBounds().Overlaps(loadBounds))
                LoadChunk(chunk);
        }

        foreach (var cs in LoadedChunkStates.Values)
        {
            if (
                // Never unload the starting or current chunk
                cs.Chunk != StartingChunk
                && cs != CurrentChunk
                && (
                    !(CurrentMap?.ChunkBySceneName.ContainsKey(cs.Chunk.SceneName) ?? false)
                    || !cs.Chunk.GetPlayableChunkMapBounds().Overlaps(unloadBounds)
                )
            )
                UnloadChunk(cs.Chunk.SceneName, true);
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
        {
            if (!mod.Settings.GenerateChunkMaps)
                return;

            chunkMap = new([new() { SceneName = sceneName, Position = Vector3.zero }]);
            ChunkMap.Register(chunkMap);
        }

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

class ChunkLoadOperation(Chunk chunk, bool isCancellable = true, Action<Scene> onComplete = null)
    : ChunkOperation(chunk)
{
    public AsyncOperationHandle<SceneInstance> LoadOperation;

    public override bool OnUpdate()
    {
        if (!LoadOperation.IsValid())
            LoadOperation = SceneLoader.LoadSceneAsync(Chunk.SceneName, onComplete);
        return LoadOperation.IsDone;
    }

    public override bool CanAbort() =>
        isCancellable && (!LoadOperation.IsValid() || !LoadOperation.IsDone);

    public override void Abort()
    {
        if (!LoadOperation.IsValid() || LoadOperation.IsDone)
            return;

        Logger.Debug($"Aborting load operation for chunk: {Chunk.SceneName}");
        LoadOperation.Completed += _ => USceneManager.UnloadSceneAsync(LoadOperation.Result.Scene);
    }
}

class StartingChunkLoadOperation(Chunk chunk) : ChunkLoadOperation(chunk, isCancellable: false)
{
    private bool _isComplete = false;

    public override bool OnUpdate() => _isComplete;

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
