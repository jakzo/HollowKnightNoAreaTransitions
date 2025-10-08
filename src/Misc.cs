namespace HollowKnightNoAreaTransitions;

public class Misc(HollowKnightNoAreaTransitionsMod mod)
{
    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private Hook _hookCustomSceneManagerDrawBlackBorders;
    private Hook _hookSceneParticlesControllerEnableParticles;

    public void Initialize()
    {
        _mod.ChunkManager.OnChunkSceneInit += InitializeScene;

        _hookCustomSceneManagerDrawBlackBorders = new Hook(
            typeof(CustomSceneManager).GetMethod(
                "DrawBlackBorders",
                BindingFlags.NonPublic | BindingFlags.Instance
            ),
            typeof(Misc).GetMethod(
                nameof(OnDrawBlackBorders),
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );
        _hookSceneParticlesControllerEnableParticles = new Hook(
            typeof(SceneParticlesController).GetMethod(
                nameof(SceneParticlesController.EnableParticles),
                BindingFlags.Public | BindingFlags.Instance
            ),
            typeof(Misc).GetMethod(
                nameof(OnEnableParticles),
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );

        // The vignette stops us seeing the rest of the world so remove it
        // TODO: What to do about dark areas?
        HeroController.instance.vignette.gameObject.SetActive(false);

        // The killplane kills NPCs in other chunks so just remove it
        // TODO: Put killplane below lowest chunk and resize to cover all chunks?
        GameManager
            .instance.gameObject.GetComponentInChildren<KillOnContact>()
            ?.gameObject.SetActive(false);
    }

    public void Deinitialize()
    {
        _mod.ChunkManager.OnChunkSceneInit -= InitializeScene;
        _hookCustomSceneManagerDrawBlackBorders?.Dispose();
        _hookCustomSceneManagerDrawBlackBorders = null;
        _hookSceneParticlesControllerEnableParticles?.Dispose();
        _hookSceneParticlesControllerEnableParticles = null;

        HeroController.instance?.vignette?.gameObject?.SetActive(true);

        GameManager
            .instance?.gameObject.GetComponentInChildren<KillOnContact>()
            ?.gameObject.SetActive(true);
    }

    public void InitializeScene(Scene scene)
    {
        // TODO: Hook OnEnter instead
        foreach (var obj in scene.GetRootGameObjects())
        {
            // Camera locks are not good when zoomed out or moving between areas
            foreach (var cla in obj.GetComponentsInChildren<CameraLockArea>())
            {
                cla.gameObject.SetActive(false);
            }
        }
    }

    private static void OnDrawBlackBorders(
        Orig.CustomSceneManager.DrawBlackBorders orig,
        CustomSceneManager self
    ) => HollowKnightNoAreaTransitionsMod.Instance.Misc._OnDrawBlackBorders(orig, self);

    private void _OnDrawBlackBorders(
        Orig.CustomSceneManager.DrawBlackBorders orig,
        CustomSceneManager self
    )
    {
        // Do not draw scene borders because they cover neighboring scenes
        // TODO: Delete existing borders/restore on unload
    }

    private static void OnEnableParticles(
        Orig.SceneParticlesController.EnableParticles orig,
        SceneParticlesController self,
        bool noSceneParticles
    ) =>
        HollowKnightNoAreaTransitionsMod.Instance.Misc._OnEnableParticles(
            orig,
            self,
            noSceneParticles
        );

    private void _OnEnableParticles(
        Orig.SceneParticlesController.EnableParticles orig,
        SceneParticlesController self,
        bool noSceneParticles
    )
    {
        // Particles are next to the camera and obscure the world
        // TODO: Or should I move them to be in the world?
    }
}
