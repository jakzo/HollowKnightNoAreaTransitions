namespace HollowKnightNoAreaTransitions;

public class Misc(HollowKnightNoAreaTransitionsMod mod)
{
    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;

    public void Initialize()
    {
        _mod.SceneLoader.OnChunkSceneInit += InitializeScene;

        On.CustomSceneManager.DrawBlackBorders += OnDrawBlackBorders;
        On.SceneParticlesController.EnableParticles += OnEnableParticles;

        // The vignette stops us seeing the rest of the world so remove it
        // TODO: What to do about dark areas?
        HeroController.instance.vignette.gameObject.SetActive(false);

        // Move the hero along with the scene it was in
        if (_mod.ChunkManager.StartingChunk != null)
        {
            HeroController.instance.transform.localPosition +=
                _mod.ChunkManager.StartingChunk.Position + SceneLoader.WORLD_OFFSET;
        }

        // The killplane kills NPCs in other chunks so just remove it
        // TODO: Put killplane below lowest chunk and resize to cover all chunks?
        GameManager
            .instance.gameObject.GetComponentInChildren<KillOnContact>()
            ?.gameObject.SetActive(false);
    }

    public void Deinitialize()
    {
        _mod.SceneLoader.OnChunkSceneInit -= InitializeScene;
        On.CustomSceneManager.DrawBlackBorders -= OnDrawBlackBorders;
        On.SceneParticlesController.EnableParticles -= OnEnableParticles;

        HeroController.instance?.vignette?.gameObject?.SetActive(true);

        // TODO: Just move the hero somewhere inside the starting chunk?
        // if (_mod.ChunkManager.StartingChunk != null && HeroController.instance != null)
        // {
        //     HeroController.instance.transform.localPosition -=
        //         _mod.ChunkManager.StartingChunk.Position + SceneLoader.WORLD_OFFSET;
        // }

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
        On.CustomSceneManager.orig_DrawBlackBorders orig,
        CustomSceneManager self
    ) => HollowKnightNoAreaTransitionsMod.Instance.Misc._OnDrawBlackBorders(orig, self);

    private void _OnDrawBlackBorders(
        On.CustomSceneManager.orig_DrawBlackBorders orig,
        CustomSceneManager self
    )
    {
        // Do not draw scene borders because they cover neighboring scenes
        // TODO: Delete existing borders/restore on unload
    }

    private static void OnEnableParticles(
        On.SceneParticlesController.orig_EnableParticles orig,
        SceneParticlesController self,
        bool noSceneParticles
    ) =>
        HollowKnightNoAreaTransitionsMod.Instance.Misc._OnEnableParticles(
            orig,
            self,
            noSceneParticles
        );

    private void _OnEnableParticles(
        On.SceneParticlesController.orig_EnableParticles orig,
        SceneParticlesController self,
        bool noSceneParticles
    )
    {
        // Particles are next to the camera and obscure the world
        // TODO: Or should I move them to be in the world?
    }
}
