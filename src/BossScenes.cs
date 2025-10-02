namespace HollowKnightNoAreaTransitions;

// Globals needing patching:
// SceneAdditiveLoadConditional.LoadInSequence
// SceneAdditiveLoadConditional._additiveSceneLoads
// GameManager.instance.OnLoadedBoss

// PLAN:
// - SceneAdditiveLoadConditional._additiveSceneLoads -> have per-scene versions of this
//   - SceneAdditiveLoadConditional.LoadAll
//     - SceneLoad -> only used when transitioning to a new scene so we don't need to patch it?
//   - SceneAdditiveLoadConditional.IsAnyLoaded -> have anything that calls this use per-scene additiveSceneLoads
//     - SpawnPreloader.SpawnPreloader -> use scene name to activate per-scene additiveSceneLoads
//   - SceneAdditiveLoadConditional.ShouldLoadBoss -> have anything that calls this use per-scene additiveSceneLoads
//     - SceneLoad -> only used when transitioning to a new scene so we don't need to patch it?
//     - WaitForBossLoad.OnEnter -> use Owner.scene to activate per-scene additiveSceneLoads
//   - SceneAdditiveLoadConditional.OnEnable -> use gameObject.scene to activate per-scene additiveSceneLoads
//   - SceneAdditiveLoadConditional.Start -> use gameObject.scene to activate per-scene additiveSceneLoads
//   - SceneAdditiveLoadConditional.OnDisable -> use gameObject.scene to activate per-scene additiveSceneLoads
//     - SceneLoad -> only used when transitioning to a new scene so we don't need to patch it?

public class BossScenes(HollowKnightNoAreaTransitionsMod mod)
{
    public class Globals
    {
        public List<SceneAdditiveLoadConditional> AdditiveSceneLoads = [];
        public event Action OnLoadedBoss;
        public bool LoadInSequence = true;

        public void BossLoaded() => OnLoadedBoss?.Invoke();
    }

    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;

    public Dictionary<string, Globals> GlobalsByChunk = [];

    private FieldInfo _additiveSceneLoadsField;
    private PropertyInfo _sceneNameToLoadProperty;
    private FieldInfo _loadOpField;
    private FieldInfo _sceneLoadedField;
    private FieldInfo _repositionSceneField;
    private MethodInfo _applySettingsMethod;
    private MethodInfo _onWasLoadedMethod;
    private MethodInfo _loadRoutineMethod;

    public void Initialize()
    {
        var sceneAdditiveLoadType = typeof(SceneAdditiveLoadConditional);
        var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        var staticBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        _additiveSceneLoadsField = sceneAdditiveLoadType.GetField(
            "_additiveSceneLoads",
            staticBindingFlags
        );
        _sceneNameToLoadProperty = sceneAdditiveLoadType.GetProperty(
            "SceneNameToLoad",
            bindingFlags
        );
        _loadOpField = sceneAdditiveLoadType.GetField("loadOp", bindingFlags);
        _sceneLoadedField = sceneAdditiveLoadType.GetField("sceneLoaded", bindingFlags);
        _applySettingsMethod = sceneAdditiveLoadType.GetMethod("ApplySettings", bindingFlags);
        _repositionSceneField = sceneAdditiveLoadType.GetField("repositionScene", bindingFlags);
        _onWasLoadedMethod = sceneAdditiveLoadType.GetMethod("OnWasLoaded", bindingFlags);
        _loadRoutineMethod = sceneAdditiveLoadType.GetMethod("LoadRoutine", bindingFlags);

        _mod.ChunkManager.OnChunkLoaded += OnChunkLoaded;
        _mod.ChunkManager.OnChunkUnloaded += OnChunkUnloaded;

        On.ScenePreloader.SpawnPreloader += OnSpawnPreloader;
        On.WaitForBossLoad.OnEnter += OnWaitForBossLoadEnter;
        On.SceneAdditiveLoadConditional.LoadRoutine += OnLoadRoutine;
        On.SceneAdditiveLoadConditional.OnEnable += OnSceneAdditiveLoadConditionalEnable;
        On.SceneAdditiveLoadConditional.Start += OnSceneAdditiveLoadConditionalStart;
        // TODO: SceneAdditiveLoadConditional.TryTestLoad uses GameManager.instance.entryGateName
    }

    public void Deinitialize()
    {
        _mod.ChunkManager.OnChunkLoaded -= OnChunkLoaded;
        _mod.ChunkManager.OnChunkUnloaded -= OnChunkUnloaded;

        On.ScenePreloader.SpawnPreloader -= OnSpawnPreloader;
        On.WaitForBossLoad.OnEnter -= OnWaitForBossLoadEnter;
        On.SceneAdditiveLoadConditional.LoadRoutine -= OnLoadRoutine;
        On.SceneAdditiveLoadConditional.OnEnable -= OnSceneAdditiveLoadConditionalEnable;
        On.SceneAdditiveLoadConditional.Start -= OnSceneAdditiveLoadConditionalStart;
    }

    private void OnChunkUnloaded(ChunkState chunkState)
    {
        var globals = GetGlobalsForScene(chunkState.Chunk.SceneName);
        if (globals == null)
            return;

        foreach (var sceneAdditiveLoad in globals.AdditiveSceneLoads)
        {
            if (sceneAdditiveLoad.gameObject.scene.isLoaded)
            {
                USceneManager.UnloadSceneAsync(sceneAdditiveLoad.gameObject.scene);
            }
        }

        GlobalsByChunk.Remove(chunkState.Chunk.SceneName);
    }

    private Globals GetGlobalsForScene(string sceneName)
    {
        if (
            _mod.ChunkManager.CurrentMap == null
            || _mod.ChunkManager.StartingChunk?.SceneName == sceneName
        )
            return null;

        if (!GlobalsByChunk.TryGetValue(sceneName, out var globals))
        {
            globals = new Globals();
            GlobalsByChunk[sceneName] = globals;
        }
        return globals;
    }

    private void OnChunkLoaded(ChunkState chunkState)
    {
        var sceneName = chunkState.Chunk.SceneName;
        var globals = GetGlobalsForScene(sceneName);
        if (globals == null)
            return;

        if (globals.AdditiveSceneLoads.Count > 0)
        {
            MelonCoroutines.Start(LoadAll(globals));
            globals.BossLoaded();
        }
    }

    private IEnumerator LoadAll(Globals globals)
    {
        if (globals.AdditiveSceneLoads != null)
        {
            foreach (
                SceneAdditiveLoadConditional sceneAdditiveLoadConditional in globals.AdditiveSceneLoads
            )
            {
                if (sceneAdditiveLoadConditional)
                {
                    var loadRoutine = (IEnumerator)
                        _loadRoutineMethod.Invoke(
                            sceneAdditiveLoadConditional,
                            [false, sceneAdditiveLoadConditional]
                        );
                    yield return sceneAdditiveLoadConditional.StartCoroutine(loadRoutine);
                }
            }
        }
        globals.LoadInSequence = false;
    }

    private static void OnSceneAdditiveLoadConditionalEnable(
        On.SceneAdditiveLoadConditional.orig_OnEnable orig,
        SceneAdditiveLoadConditional self
    ) =>
        HollowKnightNoAreaTransitionsMod.Instance.BossScenes._OnSceneAdditiveLoadConditionalEnable(
            orig,
            self
        );

    private void _OnSceneAdditiveLoadConditionalEnable(
        On.SceneAdditiveLoadConditional.orig_OnEnable orig,
        SceneAdditiveLoadConditional self
    )
    {
        WithPerSceneGlobals(self.gameObject.scene.name, () => orig(self));
    }

    private static void OnSceneAdditiveLoadConditionalStart(
        On.SceneAdditiveLoadConditional.orig_Start orig,
        SceneAdditiveLoadConditional self
    ) =>
        HollowKnightNoAreaTransitionsMod.Instance.BossScenes._OnSceneAdditiveLoadConditionalStart(
            orig,
            self
        );

    private void _OnSceneAdditiveLoadConditionalStart(
        On.SceneAdditiveLoadConditional.orig_Start orig,
        SceneAdditiveLoadConditional self
    ) => WithPerSceneGlobals(self.gameObject.scene.name, () => orig(self));

    private static void OnSpawnPreloader(
        On.ScenePreloader.orig_SpawnPreloader orig,
        string sceneName,
        LoadSceneMode mode
    ) =>
        HollowKnightNoAreaTransitionsMod.Instance.BossScenes._OnSpawnPreloader(
            orig,
            sceneName,
            mode
        );

    private void _OnSpawnPreloader(
        On.ScenePreloader.orig_SpawnPreloader orig,
        string sceneName,
        LoadSceneMode mode
    )
    {
        WithPerSceneGlobals(sceneName, () => orig(sceneName, mode));
    }

    private void WithPerSceneGlobals(string sceneName, Action action)
    {
        if (
            _mod.ChunkManager.CurrentMap == null
            || _mod.ChunkManager.StartingChunk?.SceneName == sceneName
        )
        {
            action();
            return;
        }

        List<SceneAdditiveLoadConditional> actualAdditiveSceneLoads = null;
        bool actualLoadInSequence = false;
        Utils.Try(() =>
        {
            if (_mod.ChunkManager.CurrentMap == null)
                return;

            var globals = GetGlobalsForScene(sceneName);
            if (globals == null)
                return;

            actualAdditiveSceneLoads =
                (List<SceneAdditiveLoadConditional>)_additiveSceneLoadsField.GetValue(null);
            _additiveSceneLoadsField.SetValue(null, globals.AdditiveSceneLoads);
            actualLoadInSequence = SceneAdditiveLoadConditional.LoadInSequence;
        });

        try
        {
            action();
        }
        finally
        {
            if (actualAdditiveSceneLoads == null)
            {
                _additiveSceneLoadsField.SetValue(null, actualAdditiveSceneLoads);
                SceneAdditiveLoadConditional.LoadInSequence = actualLoadInSequence;
            }
        }
    }

    private static void OnWaitForBossLoadEnter(
        On.WaitForBossLoad.orig_OnEnter orig,
        WaitForBossLoad self
    ) => HollowKnightNoAreaTransitionsMod.Instance.BossScenes._OnWaitForBossLoadEnter(orig, self);

    private void _OnWaitForBossLoadEnter(On.WaitForBossLoad.orig_OnEnter orig, WaitForBossLoad self)
    {
        // Reimplementation of original method but using per-scene additiveSceneLoads (easier than patching IL)
        var globals = Utils.Try(() => GetGlobalsForScene(self.Owner.scene.name));
        if (globals == null)
        {
            orig(self);
            return;
        }

        if (
            !WorldInfo.NameLooksLikeAdditiveLoadScene(self.Owner.scene.name)
            && GameManager.instance
            && SceneAdditiveLoadConditional.ShouldLoadBoss
        )
        {
            void temp()
            {
                // self.Fsm.Event(self.sendEvent);
                // globals.OnLoadedBoss -= temp;
                // self.Finish();
            }
            globals.OnLoadedBoss += temp;
            return;
        }
        self.Finish();
    }

    private static IEnumerator OnLoadRoutine(
        On.SceneAdditiveLoadConditional.orig_LoadRoutine orig,
        SceneAdditiveLoadConditional self,
        bool callEvent,
        SceneAdditiveLoadConditional sceneLoader
    ) =>
        HollowKnightNoAreaTransitionsMod.Instance.BossScenes.LoadRoutine(
            orig,
            self,
            callEvent,
            sceneLoader
        );

    public IEnumerator LoadRoutine(
        On.SceneAdditiveLoadConditional.orig_LoadRoutine orig,
        SceneAdditiveLoadConditional self,
        bool callEvent,
        SceneAdditiveLoadConditional sceneLoader
    )
    {
        // Reimplementation of original method but using per-scene additiveSceneLoads (easier than patching IL)
        _applySettingsMethod?.Invoke(self, null);
        yield return null;
        string sceneNameToLoad = (string)_sceneNameToLoadProperty?.GetValue(self);
        string text = "Scenes/" + sceneNameToLoad;
        var loadOp = new AsyncOperationHandle<SceneInstance>?(
            ScenePreloader.TakeSceneLoadOperation(text, LoadSceneMode.Additive)
                ?? Addressables.LoadSceneAsync(
                    text,
                    LoadSceneMode.Additive,
                    true,
                    100,
                    SceneReleaseMode.ReleaseSceneWhenSceneUnloaded
                )
        );
        _loadOpField?.SetValue(self, loadOp);
        yield return loadOp;
        if (loadOp.Value.OperationException != null)
        {
            UDebug.LogError(
                "Additive scene load for " + sceneNameToLoad + " failed with exception:"
            );
            UDebug.LogException(loadOp.Value.OperationException, self);
        }
        else
        {
            _sceneLoadedField?.SetValue(self, true);
            bool repositionScene = (bool)(_repositionSceneField?.GetValue(self) ?? false);
            // === Difference is here ===
            GameObject[] rootGameObjects = USceneManager
                .GetSceneByName(sceneNameToLoad)
                .GetRootGameObjects();
            Vector3 position = self.transform.position;
            GameObject[] array = rootGameObjects;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].transform.position +=
                    SceneLoader.WORLD_OFFSET + (repositionScene ? position : Vector3.zero);
            }
            // ===
        }
        if (callEvent && GameManager.instance)
        {
            // === Difference is here ===
            var globals = GetGlobalsForScene(self.gameObject.scene.name);
            if (globals != null)
            {
                globals.BossLoaded();
            }
            else if (SceneAdditiveLoadConditional.ShouldLoadBoss)
            {
                GameManager.instance.LoadedBoss();
            }
            // ===
        }
        _onWasLoadedMethod?.Invoke(sceneLoader, null);
        yield break;
    }
}
