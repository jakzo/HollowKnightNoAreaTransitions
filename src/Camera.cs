namespace HollowKnightNoAreaTransitions;

public class Camera(HollowKnightNoAreaTransitionsMod mod)
{
    private const float INITIAL_CAM_OFFSET = 30.8f;

    public float Zoom = 2f;

    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private GameObject _decoupled;
    private FieldInfo _sceneCameraField;
    private Hook _hookCameraControllerLateUpdate;
    private Hook _hookCameraControllerLockToArea;
    private Hook _hookCameraTargetUpdate;
    private Hook _hookLightBlurredBackgroundUpdateCameraClipPlanes;

    public void Initialize()
    {
        _sceneCameraField = typeof(LightBlurredBackground).GetField(
            "sceneCamera",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        // TODO: Update limits and locks instead of just removing them
        _hookCameraControllerLateUpdate = new Hook(
            typeof(CameraController).GetMethod(
                "LateUpdate",
                BindingFlags.NonPublic | BindingFlags.Instance
            ),
            typeof(Camera).GetMethod(
                nameof(OnCameraLateUpdate),
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );
        _hookCameraControllerLockToArea = new Hook(
            typeof(CameraController).GetMethod(
                nameof(CameraController.LockToArea),
                BindingFlags.Public | BindingFlags.Instance
            ),
            typeof(Camera).GetMethod(
                nameof(OnLockToArea),
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );
        _hookCameraTargetUpdate = new Hook(
            typeof(CameraTarget).GetMethod(
                nameof(CameraTarget.Update),
                BindingFlags.Public | BindingFlags.Instance
            ),
            typeof(Camera).GetMethod(
                nameof(OnCameraTargetUpdate),
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );
        UnlockCamera();

        _hookLightBlurredBackgroundUpdateCameraClipPlanes = new Hook(
            typeof(LightBlurredBackground).GetMethod(
                "UpdateCameraClipPlanes",
                BindingFlags.NonPublic | BindingFlags.Instance
            ),
            typeof(Camera).GetMethod(
                nameof(OnUpdateCameraClipPlanes),
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );

        DecoupleFromCamera();

        // TODO: On scene entry set camera position to knight
    }

    public void Deinitialize()
    {
        SetCameraPosition(2f);
        if (GameCameras.instance?.tk2dCam != null)
            GameCameras.instance.tk2dCam.ZoomFactor = 1f;

        // TODO: Restore camera limits
        _hookCameraControllerLateUpdate?.Dispose();
        _hookCameraControllerLateUpdate = null;
        _hookCameraControllerLockToArea?.Dispose();
        _hookCameraControllerLockToArea = null;
        _hookCameraTargetUpdate?.Dispose();
        _hookCameraTargetUpdate = null;

        _hookLightBlurredBackgroundUpdateCameraClipPlanes?.Dispose();
        _hookLightBlurredBackgroundUpdateCameraClipPlanes = null;
    }

    private static void OnCameraLateUpdate(
        Orig.CameraController.LateUpdate orig,
        CameraController self
    ) => HollowKnightNoAreaTransitionsMod.Instance.Camera._OnCameraLateUpdate(orig, self);

    private void _OnCameraLateUpdate(Orig.CameraController.LateUpdate orig, CameraController self)
    {
        orig(self);

        Utils.Hooks.Try(() =>
        {
            DoCameraZoom();
            RemoveLimits();
        });
    }

    public void DoCameraZoom()
    {
        var scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta != 0f)
        {
            Zoom = Mathf.Min(
                Mathf.Pow(Zoom, 1f - scrollDelta * _mod.Settings.ZoomSpeed * 0.01f),
                1000f
            );
            // Logger.Debug($"Zoom = {Zoom}");
        }
        SetCameraPosition(Zoom);
    }

    // TODO: Fix camera flickering
    public void SetCameraPosition(float zoom)
    {
        var cam = GameCameras.instance?.cameraParent;
        if (cam == null)
            return;

        var camPos = cam.localPosition;
        var newCamZ = -((zoom - 2f) * INITIAL_CAM_OFFSET);
        if (newCamZ != cam.localPosition.z)
        {
            cam.localPosition = new Vector3(camPos.x, camPos.y, newCamZ);
        }

        _decoupled.transform.localPosition = new Vector3(
            camPos.x,
            camPos.y,
            _decoupled.transform.localPosition.z
        );
    }

    public void RemoveLimits()
    {
        var cam = GameManager.instance.cameraCtrl;
        cam.xLimit = cam.yLimit = float.PositiveInfinity;
        cam.xLockMin = cam.yLockMin = float.NegativeInfinity;
        cam.xLockMax = cam.yLockMax = float.PositiveInfinity;
        cam.SetAllowExitingSceneBounds(true);
        while (cam.lockZoneList.Count > 0)
        {
            cam.ReleaseLock(cam.lockZoneList[0]);
        }

        var target = cam.camTarget;
        target.xLockMin = target.yLockMin = float.NegativeInfinity;
        target.xLockMax = target.yLockMax = float.PositiveInfinity;
    }

    // TODO: Call this after loading into a new scene?
    public void UnlockCamera()
    {
        var camTarget = GameCameras.instance.cameraTarget;
        camTarget.xLockMin = camTarget.yLockMin = float.NegativeInfinity;
        camTarget.xLockMax = camTarget.yLockMax = float.PositiveInfinity;
    }

    public void DecoupleFromCamera()
    {
        _decoupled = new GameObject("HKNAT_OriginalCameraPosition");
        _decoupled.transform.SetParent(GameCameras.instance.cameraParent, false);
        _decoupled.transform.localPosition = tk2dCamera.Instance.transform.localPosition;

        UObject.Destroy(tk2dCamera.Instance.GetComponent<AudioListener>());
        _decoupled.AddComponent<AudioListener>();
    }

    public void RecoupleToCamera()
    {
        UObject.Destroy(_decoupled);

        tk2dCamera.Instance.gameObject.AddComponent<AudioListener>();
    }

    private static void OnLockToArea(
        Orig.CameraController.LockToArea orig,
        CameraController self,
        CameraLockArea lockArea
    ) => HollowKnightNoAreaTransitionsMod.Instance.Camera._OnLockToArea(orig, self, lockArea);

    private void _OnLockToArea(
        Orig.CameraController.LockToArea orig,
        CameraController self,
        CameraLockArea lockArea
    )
    {
        // Do not lock to area
    }

    private static void OnUpdateCameraClipPlanes(
        Orig.LightBlurredBackground.UpdateCameraClipPlanes orig,
        LightBlurredBackground self
    ) => HollowKnightNoAreaTransitionsMod.Instance.Camera._OnUpdateCameraClipPlanes(orig, self);

    private void _OnUpdateCameraClipPlanes(
        Orig.LightBlurredBackground.UpdateCameraClipPlanes orig,
        LightBlurredBackground self
    )
    {
        orig(self);

        Utils.Hooks.Try(() =>
        {
            // TODO: Fix blurred lights flickering when zooming
            // TODO: Fix blurred background items not showing as they normally appear
            var sceneCamera = (UCamera)_sceneCameraField.GetValue(self);
            sceneCamera.farClipPlane += Zoom * 10f;
        });
    }

    private static void OnCameraTargetUpdate(Orig.CameraTarget.Update orig, CameraTarget self) =>
        HollowKnightNoAreaTransitionsMod.Instance.Camera._OnCameraTargetUpdate(orig, self);

    private void _OnCameraTargetUpdate(Orig.CameraTarget.Update orig, CameraTarget self)
    {
        // Game has hardcoded camera target limits of 0 to 9999 when in FOLLOW_HERO mode.
        // This limit is applied in CameraTarget.Update.
        // LOCK_ZONE mode does not have these limits and behaves the same as FOLLOW_HERO within
        // the Update method so switch to that temporarily.
        var mode = self.mode;
        self.mode = CameraTarget.TargetMode.LOCK_ZONE;
        orig(self);
        self.mode = mode;
    }
}
