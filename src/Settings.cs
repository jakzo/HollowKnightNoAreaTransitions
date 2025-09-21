namespace HollowKnightNoAreaTransitions;

public class Settings
{
    public float ZoomSpeed => _zoomSpeed.Value;
    public bool DisableTransitions => _disableTransitions.Value;
    public bool DebugLogs => _debugLogs.Value;

    private MelonPreferences_Category _category;
    private MelonPreferences_Entry<float> _zoomSpeed;
    private MelonPreferences_Entry<bool> _disableTransitions;
    private MelonPreferences_Entry<bool> _debugLogs;

    public void Initialize()
    {
        _category = MelonPreferences.CreateCategory("HollowKnightNoAreaTransitions");
        _zoomSpeed = _category.CreateEntry("ZoomSpeed", 4f, "Camera Zoom Speed");
        _disableTransitions = _category.CreateEntry(
            "Beta_DisableTransitions",
            false,
            "Disable Transitions Between Areas"
        );
        _debugLogs = _category.CreateEntry(
            "DebugLogs",
            false,
            "Enable Debug Logs",
            null,
            true,
            true
        );
    }
}
