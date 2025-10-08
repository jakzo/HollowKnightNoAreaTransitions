namespace HollowKnightNoAreaTransitions;

public static class Orig
{
    public static class CameraController
    {
        public delegate void LateUpdate(global::CameraController self);
        public delegate void LockToArea(global::CameraController self, CameraLockArea lockArea);
    }

    public static class CameraTarget
    {
        public delegate void Update(global::CameraTarget self);
    }

    public static class LightBlurredBackground
    {
        public delegate void UpdateCameraClipPlanes(global::LightBlurredBackground self);
    }

    public static class CustomSceneManager
    {
        public delegate void DrawBlackBorders(global::CustomSceneManager self);
    }

    public static class SceneParticlesController
    {
        public delegate void EnableParticles(
            global::SceneParticlesController self,
            bool noSceneParticles
        );
    }

    public static class TransitionPoint
    {
        public delegate void TryDoTransition(global::TransitionPoint self, Collider2D movingObj);
    }

    public static class SceneLoad
    {
        public delegate void Begin(global::SceneLoad self);
    }

    public static class ScenePreloader
    {
        public delegate void SpawnPreloader(string sceneName, LoadSceneMode mode);
    }

    public static class WaitForBossLoad
    {
        public delegate void OnEnter(global::WaitForBossLoad self);
    }

    public static class SceneAdditiveLoadConditional
    {
        public delegate void OnEnable(global::SceneAdditiveLoadConditional self);
        public delegate void Start(global::SceneAdditiveLoadConditional self);
        public delegate IEnumerator LoadRoutine(
            global::SceneAdditiveLoadConditional self,
            bool callEvent,
            global::SceneAdditiveLoadConditional sceneLoader
        );
    }

    public static class StartManager
    {
        public delegate IEnumerator Start(global::StartManager self);
    }

    public static class SaveSlotButton
    {
        public delegate bool ProcessSaveStats(
            UnityEngine.UI.SaveSlotButton self,
            bool doAnimate,
            string errorInfo,
            SaveStats newSaveStats
        );
        public delegate bool PreloadSave(
            UnityEngine.UI.SaveSlotButton self,
            GameManager gameManager
        );
    }
}
