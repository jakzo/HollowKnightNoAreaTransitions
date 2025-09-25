namespace HollowKnightNoAreaTransitions;

// When the game transitions to a new area the GameManager creates a
// SceneLoad which handles scene loading and fires events at particular
// moments of the loading process. If we are loading into a scene which is
// part of a larger chunk map, queue loading the rest of the scenes in the
// chunk map.
public class TransitionHooks(HollowKnightNoAreaTransitionsMod mod)
{
    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private ConditionalWeakTable<AsyncOperation, AsyncOperation[]> _loadOperations = new();

    public void Initialize()
    {
        On.TransitionPoint.OnTriggerEnter2D += OnTransitionPointEnter;
        On.SceneLoad.Begin += OnSceneLoadBegin;
        USceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    public void Deinitialize()
    {
        On.TransitionPoint.OnTriggerEnter2D -= OnTransitionPointEnter;
        On.SceneLoad.Begin -= OnSceneLoadBegin;
        USceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    // When the knight enters a level exit it should do nothing if the next scene
    // they are going to is already part of the current chunk map and loaded
    private void OnTransitionPointEnter(
        On.TransitionPoint.orig_OnTriggerEnter2D orig,
        TransitionPoint self,
        Collider2D movingObj
    )
    {
        var isBlocked = Utils.Try(() =>
            _mod.Settings.DisableTransitions
            && movingObj.gameObject.layer == Utils.Layers.Player.Id
            && (
                _mod.ChunkManager.CurrentMap?.ChunkBySceneName.ContainsKey(self.targetScene)
                ?? false
            )
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
        Utils.Try(() =>
        {
            if (_mod.Settings.DisableTransitions)
            {
                // TODO
                // _hookLoadSceneAsync = new Hook(_methodLoadSceneAsync, OnLoadSceneAsync);
            }
            else
            {
                self.FetchComplete += () =>
                {
                    self.OperationHandle.Completed += op =>
                    {
                        var scene = op.Result.Scene;
                        var chunk =
                            _mod.ChunkManager.CurrentMap?.ChunkBySceneName.GetValueOrDefault(
                                scene.name
                            );
                        if (chunk != null)
                            _mod.ChunkManager.InitializeChunkScene(chunk);
                    };
                };

                _mod.ChunkManager.InitEnteredScene(self.SceneLoadInfo.SceneName);
            }
        });
        orig(self);
    }
}
