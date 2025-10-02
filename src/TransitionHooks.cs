namespace HollowKnightNoAreaTransitions;

// When the game transitions to a new area the GameManager creates a
// SceneLoad which handles scene loading and fires events at particular
// moments of the loading process. If we are loading into a scene which is
// part of a larger chunk map, queue loading the rest of the scenes in the
// chunk map.
public class TransitionHooks(HollowKnightNoAreaTransitionsMod mod)
{
    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;

    public void Initialize()
    {
        On.TransitionPoint.TryDoTransition += OnTryDoTransition;
        On.SceneLoad.Begin += OnSceneLoadBegin;
        USceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    public void Deinitialize()
    {
        On.TransitionPoint.TryDoTransition -= OnTryDoTransition;
        On.SceneLoad.Begin -= OnSceneLoadBegin;
        USceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private static void OnTryDoTransition(
        On.TransitionPoint.orig_TryDoTransition orig,
        TransitionPoint self,
        Collider2D movingObj
    ) =>
        HollowKnightNoAreaTransitionsMod.Instance.TransitionHooks._OnTryDoTransition(
            orig,
            self,
            movingObj
        );

    // When the knight enters a level exit it should do nothing if the next scene
    // they are going to is already part of the current chunk map and loaded
    private void _OnTryDoTransition(
        On.TransitionPoint.orig_TryDoTransition orig,
        TransitionPoint self,
        Collider2D movingObj
    )
    {
        var isBlocked = Utils.Try(() => IsTransitionDisabled(self));

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

    private static void OnSceneLoadBegin(On.SceneLoad.orig_Begin orig, SceneLoad self) =>
        HollowKnightNoAreaTransitionsMod.Instance.TransitionHooks._OnSceneLoadBegin(orig, self);

    // When the game has decided to load a new room, one of the first things it
    // does is start loading the new room's scene, so we add a one-off hook to
    // OnLoadSceneAsync for that call so that we can load all scenes in the chunk
    // map instead if necessary
    private void _OnSceneLoadBegin(On.SceneLoad.orig_Begin orig, SceneLoad self)
    {
        Utils.Try(() =>
        {
            self.FetchComplete += () =>
                self.OperationHandle.Completed += _mod.SceneLoader.HandleSceneLoadedByGame;
            // TODO: Call this after fade out (but before fade in)
            _mod.ChunkManager.InitChunksOnSceneEntering(self.SceneLoadInfo.SceneName);
        });

        orig(self);
    }

    public bool IsTransitionDisabled(TransitionPoint tp) =>
        _mod.ChunkManager.CurrentMap?.ChunkBySceneName.ContainsKey(tp.targetScene) ?? false;
}
