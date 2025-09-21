namespace HollowKnightNoAreaTransitions;

public class HollowKnightNoAreaTransitionsMod : MelonMod
{
    public static HollowKnightNoAreaTransitionsMod Instance { get; private set; }

    public Settings Settings = new();
    public Camera Camera { get; private set; }
    public BossScenes BossScenes { get; private set; }
    public Misc Misc { get; private set; }
    public SceneLoader SceneLoader { get; private set; }
    public TransitionHooks TransitionHooks { get; private set; }
    public ChunkMap CurrentMap;
    public Chunk StartingChunk;
    public bool IsInitialized { get; private set; } = false;

    public HollowKnightNoAreaTransitionsMod()
    {
        Instance = this;
        Camera = new(this);
        Misc = new(this);
        BossScenes = new(this);
        SceneLoader = new(this);
        TransitionHooks = new(this);
    }

    public override void OnInitializeMelon()
    {
        Settings.Initialize();
    }

    public override void OnDeinitializeMelon()
    {
        Deinitialize();
    }

    public void Initialize()
    {
        TransitionHooks.Initialize();
        SceneLoader.Initialize();
        Camera.Initialize();
        BossScenes.Initialize();
        Misc.Initialize();

#if DEBUG
        HollowKnightNoAreaTransitionsDebug.Initialize();
#endif

        IsInitialized = true;
    }

    public void Deinitialize()
    {
        Misc.Deinitialize();
        BossScenes.Deinitialize();
        Camera.Deinitialize();
        SceneLoader.Deinitialize();
        TransitionHooks.Deinitialize();

#if DEBUG
        HollowKnightNoAreaTransitionsDebug.Deinitialize();
#endif

        IsInitialized = false;
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        MelonLogger.Msg($"Scene loaded: {sceneName} (Build Index: {buildIndex})");
    }

    public override void OnUpdate()
    {
        if (
            Input.GetKeyDown(KeyCode.O)
            && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        )
        {
            if (IsInitialized)
            {
                Logger.Info("Deinitializing");
                Deinitialize();
            }
            else
            {
                Logger.Info("Initializing");
                Initialize();
            }
        }
    }
}
