namespace HollowKnightNoAreaTransitions;

public class Camera(HollowKnightNoAreaTransitionsMod mod)
{
    private const float INITIAL_CAM_OFFSET = 30.8f;

    public float Zoom = 2f;

    private readonly HollowKnightNoAreaTransitionsMod _mod = mod;
    private GameObject _decoupled;
    private FieldInfo _sceneCameraField;

    public void Initialize()
    {
        _sceneCameraField = typeof(LightBlurredBackground).GetField(
            "sceneCamera",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        // TODO: Update limits and locks instead of just removing them
        On.CameraController.LateUpdate += OnCameraLateUpdate;
        On.CameraController.LockToArea += OnLockToArea;
        UnlockCamera();

        On.LightBlurredBackground.UpdateCameraClipPlanes += OnUpdateCameraClipPlanes;

        DecoupleFromCamera();

        // TODO: On scene entry set camera position to knight
    }

    public void Deinitialize()
    {
        SetCameraPosition(2f);
        GameCameras.instance.tk2dCam.ZoomFactor = 1f;

        // TODO: Restore camera limits
        On.CameraController.LateUpdate -= OnCameraLateUpdate;
        On.CameraController.LockToArea -= OnLockToArea;

        On.LightBlurredBackground.UpdateCameraClipPlanes -= OnUpdateCameraClipPlanes;
    }

    private void OnCameraLateUpdate(On.CameraController.orig_LateUpdate orig, CameraController self)
    {
        orig(self);

        Utils.Try(() =>
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
            Zoom = Mathf.Pow(Zoom, 1f - scrollDelta * _mod.Settings.ZoomSpeed * 0.01f);
            Logger.Debug($"Zoom = {Zoom}");
        }
        SetCameraPosition(Zoom);
    }

    // TODO: Fix camera flickering
    public void SetCameraPosition(float zoom)
    {
        var newCamZ = -((zoom - 2f) * INITIAL_CAM_OFFSET);
        var cam = GameCameras.instance.cameraParent;
        var camPos = cam.localPosition;
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
    }

    public void UnlockCamera()
    {
        var camCtrl = GameManager.instance.cameraCtrl;
        while (camCtrl.lockZoneList.Count > 0)
        {
            camCtrl.ReleaseLock(camCtrl.lockZoneList[0]);
        }
        camCtrl.xLimit = camCtrl.yLimit = float.PositiveInfinity;

        var camTarget = GameCameras.instance.cameraTarget;
        camTarget.xLockMin = camTarget.yLockMin = float.NegativeInfinity;
        camTarget.xLockMax = camTarget.yLockMax = float.PositiveInfinity;
    }

    public void DecoupleFromCamera()
    {
        _decoupled = new GameObject("OneLevel_OriginalCameraPosition");
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

    private void OnLockToArea(
        On.CameraController.orig_LockToArea orig,
        CameraController self,
        CameraLockArea lockArea
    )
    {
        // Do not lock to area
    }

    private void OnUpdateCameraClipPlanes(
        On.LightBlurredBackground.orig_UpdateCameraClipPlanes orig,
        LightBlurredBackground self
    )
    {
        orig(self);

        Utils.Try(() =>
        {
            // TODO: Fix blurred lights flickering when zooming
            // TODO: Fix blurred background items not showing as they normally appear
            var sceneCamera = (UCamera)_sceneCameraField.GetValue(self);
            sceneCamera.farClipPlane += Zoom * 10f;
        });
    }
}
