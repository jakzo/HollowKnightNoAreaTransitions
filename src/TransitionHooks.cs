namespace HollowKnightNoAreaTransitions;

// When the game transitions to a new area the GameManager creates a
// SceneLoad which handles scene loading and fires events at particular
// moments of the loading process. If we are loading into a scene which is
// part of a larger chunk map, hook the Unity LoadSceneAsync call and call it
// for each of the chunks then proxy their combined progress into the progress
// of the original scene loaded by SceneLoad.BeginRoutine so that all SceneLoad
// events are called at the right time.
public class TransitionHooks
{
    private static readonly MethodInfo _methodAsyncOperationProgress =
        typeof(AsyncOperation).GetMethod("get_progress");
    private static readonly MethodInfo _methodAsyncOperationAllowActivation =
        typeof(AsyncOperation).GetMethod("set_allowSceneActivation");
    private static readonly MethodInfo _methodLoadSceneAsync = typeof(SceneLoad).GetMethod(
        nameof(USceneManager.LoadSceneAsync),
        new Type[] { typeof(string), typeof(LoadSceneMode) }
    );

    private readonly HollowKnightNoAreaTransitionsMod _mod;
    private Hook _hookAsyncOperationProgress;
    private Hook _hookAsyncOperationAllowActivation;
    private Hook _hookLoadSceneAsync;
    private ConditionalWeakTable<AsyncOperation, AsyncOperation[]> _loadOperations = new();

    public TransitionHooks(HollowKnightNoAreaTransitionsMod mod)
    {
        _mod = mod;
    }

    public void Initialize()
    {
        On.TransitionPoint.OnTriggerEnter2D += OnTransitionPointEnter;
        USceneManager.activeSceneChanged += HandleActiveSceneChanged;
        On.SceneLoad.Begin += OnSceneLoadBegin;

        // These are the only properties used in SceneLoad.BeginRoutine
        _hookAsyncOperationProgress = new Hook(
            _methodAsyncOperationProgress,
            OnAsyncOperationProgress
        );
        _hookAsyncOperationAllowActivation = new Hook(
            _methodAsyncOperationAllowActivation,
            OnAsyncOperationAllowActivation
        );
    }

    public void Deinitialize()
    {
        On.TransitionPoint.OnTriggerEnter2D -= OnTransitionPointEnter;
        USceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        On.SceneLoad.Begin -= OnSceneLoadBegin;
        _hookAsyncOperationProgress?.Dispose();
        _hookAsyncOperationAllowActivation?.Dispose();
    }

    // When the knight enters a level exit it should do nothing if the next scene
    // they are going to is already part of the current chunk map and loaded
    private void OnTransitionPointEnter(
        On.TransitionPoint.orig_OnTriggerEnter2D orig,
        TransitionPoint self,
        Collider2D movingObj
    )
    {
        var isBlocked = Utils.Try(
            () =>
                _mod.Settings.DisableTransitions
                && movingObj.gameObject.layer == Utils.Layers.Player.Id
                && (_mod.CurrentMap?.ChunkBySceneName.ContainsKey(self.targetScene) ?? false)
        );

        if (!isBlocked)
            orig(self, movingObj);
    }

    // When transitioning while multiple chunks are loaded, the active scene gets
    // switched to one of the chunk scenes, but we want to keep the active scene
    // set to the chunk the player is in because that's what the game expects
    private void HandleActiveSceneChanged(Scene current, Scene next)
    {
        Utils.Try(() =>
        {
            var targetSceneName =
                GameManager._instance?.nextSceneName ?? GameManager._instance?.sceneName;
            if (targetSceneName != null && targetSceneName != next.name)
            {
                var gameScene = USceneManager.GetSceneByName(targetSceneName);
                if (gameScene.isLoaded)
                {
                    USceneManager.SetActiveScene(gameScene);
                }
            }
        });
    }

    // When the game has decided to load a new room, one of the first things it
    // does is start loading the new room's scene, so we add a one-off hook to
    // OnLoadSceneAsync for that call so that we can load all scenes in the chunk
    // map instead if necessary
    private void OnSceneLoadBegin(On.SceneLoad.orig_Begin orig, SceneLoad self)
    {
        Utils.Try(() => _hookLoadSceneAsync = new Hook(_methodLoadSceneAsync, OnLoadSceneAsync));
        orig(self);
    }

    // Called on room transition when the game starts loading the new room's
    // scene, and we check if the new room is part of a chunk map and start
    // loading all its scenes
    private AsyncOperation OnLoadSceneAsync(
        Func<string, LoadSceneMode, AsyncOperation> orig,
        string sceneName,
        LoadSceneMode mode
    )
    {
        var targetSceneOp = orig(sceneName, LoadSceneMode.Additive);

        Utils.Try(() =>
        {
            _hookLoadSceneAsync.Dispose();

            if (ChunkMap.BySceneName.TryGetValue(sceneName, out var chunkMap))
            {
                _mod.CurrentMap = chunkMap;
                _loadOperations.Add(
                    targetSceneOp,
                    chunkMap
                        .Chunks.Select(chunk =>
                        {
                            var op =
                                chunk.SceneName == sceneName
                                    ? targetSceneOp
                                    : USceneManager.LoadSceneAsync(
                                        chunk.SceneName,
                                        LoadSceneMode.Additive
                                    );
                            op.completed += op =>
                                Utils.Try(
                                    "ChunkSceneLoaded",
                                    () => _mod.SceneLoader.OnChunkSceneLoaded(chunk)
                                );
                            return op;
                        })
                        .ToArray()
                );
            }
        });

        return targetSceneOp;
    }

    // The game code is designed to only load one scene at a time when
    // transitioning to another room so when loading all the scenes from a chunk
    // map we have the game's scene load operation proxy its properties to all
    // scene loads, in this case showing the minimum progress of all the scene
    // loads when .progress is accessed
    private float OnAsyncOperationProgress(Func<AsyncOperation, float> orig, AsyncOperation self)
    {
        return Utils.Try(
            () => _loadOperations.TryGetValue(self, out var ops) ? ops.Min(orig) : orig(self),
            () => orig(self)
        );
    }

    // The game code is designed to only load one scene at a time when
    // transitioning to another room so when loading all the scenes from a chunk
    // map we have the game's scene load operation proxy its properties to all
    // scene loads, in this case setting the value of .allowSceneActivation on all
    // scene loads and unloading existing scenes (since we loaded them in additive
    // mode)
    private void OnAsyncOperationAllowActivation(
        Action<AsyncOperation, bool> orig,
        AsyncOperation self,
        bool value
    )
    {
        orig(self, value);

        Utils.Try(() =>
        {
            if (_loadOperations.TryGetValue(self, out var ops))
            {
                foreach (var op in ops)
                {
                    orig(op, value);
                }

                SceneLoader.UnloadAllScenes();
            }
        });
    }
}
