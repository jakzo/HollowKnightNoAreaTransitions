namespace HollowKnightNoAreaTransitions;

public class Settings
{
    public float ZoomSpeed => _zoomSpeed.Value;
    public bool DebugLogs => _debugLogs.Value;

    private MelonPreferences_Category _category;
    private MelonPreferences_Entry<float> _zoomSpeed;
    private MelonPreferences_Entry<bool> _debugLogs;

#if DEBUG
    public bool DebugColliders => _debugColliders.Value;
    private MelonPreferences_Entry<bool> _debugColliders;
#endif

    public void Initialize()
    {
        _category = MelonPreferences.CreateCategory("HollowKnightNoAreaTransitions");
        _zoomSpeed = _category.CreateEntry("ZoomSpeed", 4f, "Camera Zoom Speed");
        _debugLogs = _category.CreateEntry(
            "DebugLogs",
            false,
            "Enable Debug Logs",
            null,
            true,
            true
        );

#if DEBUG
        _debugColliders = _category.CreateEntry(
            "DebugColliders",
            false,
            "Show Chunk Terrain Colliders",
            null,
            true,
            true
        );
#endif
    }
}
