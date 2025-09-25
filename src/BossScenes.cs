namespace HollowKnightNoAreaTransitions;

public class BossScenes(HollowKnightNoAreaTransitionsMod mod)
{
    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private FieldInfo _sceneNameToLoadField;

    public void Initialize()
    {
        _sceneNameToLoadField = typeof(SceneAdditiveLoadConditional).GetField(
            "sceneNameToLoad",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        // On.SceneAdditiveLoadConditional.LoadRoutine += OnLoadRoutine;
        // On.WaitForBossLoad.OnEnter += OnWaitForBossLoadEnter;
    }

    public void Deinitialize()
    {
        // On.SceneAdditiveLoadConditional.LoadRoutine -= OnLoadRoutine;
        // On.WaitForBossLoad.OnEnter -= OnWaitForBossLoadEnter;
    }

    // This is called when chunk map loading finishes
    // private IEnumerator OnLoadRoutine(
    //     On.SceneAdditiveLoadConditional.orig_LoadRoutine orig,
    //     SceneAdditiveLoadConditional self,
    //     bool callEvent,
    //     SceneAdditiveLoadConditional sceneLoader
    // )
    // {
    //     var enumerator = orig(self, callEvent, sceneLoader);
    //     while (enumerator.MoveNext())
    //     {
    //         var current = enumerator.Current;
    //         yield return current;

    //         Utils.Try(() =>
    //         {
    //             var parentScene = self.gameObject.scene.name;
    //             if (
    //                 current is AsyncOperation
    //                 && _mod.SceneLoader.LoadedChunks.ContainsKey(parentScene)
    //             )
    //             {
    //                 (current as AsyncOperation).completed += op =>
    //                     Utils.Try(
    //                         "BossSceneCompleted",
    //                         () =>
    //                         {
    //                             var sceneNameToLoad = (string)_sceneNameToLoadField.GetValue(self);
    //                             var scene = USceneManager.GetSceneByName(sceneNameToLoad);
    //                             var cs = _mod.SceneLoader.LoadedChunks[parentScene];
    //                             cs.Scenes.Add(scene);
    //                             _mod.SceneLoader.InitializeScene(cs, scene);
    //                         }
    //                     );
    //             }
    //         });
    //     }
    // }

    // TODO: Check where this FSM action is used
    // private void OnWaitForBossLoadEnter(On.WaitForBossLoad.orig_OnEnter orig,
    //                                     WaitForBossLoad self) {
    //   if (!GameManager.instance ||
    //   !SceneAdditiveLoadConditional.ShouldLoadBoss) {
    //     self.Finish();
    //     return;
    //   }

    //   void OnLoadedBoss() {
    //     self.Fsm.Event(self.sendEvent);
    //     GameManager.instance.OnLoadedBoss -= OnLoadedBoss;
    //     self.Finish();
    //   }

    //   GameManager.instance.OnLoadedBoss += OnLoadedBoss;
    // }
}
