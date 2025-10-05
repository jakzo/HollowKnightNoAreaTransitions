namespace HollowKnightNoAreaTransitions;

public class Settings
{
    public float ZoomSpeed => _zoomSpeed.Value;
    public float LoadDistance => _loadDistance.Value;
    public float UnloadDistance => _unloadDistance.Value;
    public bool DebugLogs => _debugLogs.Value;

    private MelonPreferences_Category _category;
    private MelonPreferences_Entry<float> _zoomSpeed;
    private MelonPreferences_Entry<float> _loadDistance;
    private MelonPreferences_Entry<float> _unloadDistance;
    private MelonPreferences_Entry<bool> _debugLogs;

#if DEBUG
    public bool DebugColliders => _debugColliders.Value;
    public bool DebugTransitions => _debugTransitions.Value;
    public bool DebugPlayableAreas => _debugPlayableAreas.Value;
    public bool DebugSkipMenu => _debugSkipMenu.Value;
    private MelonPreferences_Entry<bool> _debugColliders;
    private MelonPreferences_Entry<bool> _debugTransitions;
    private MelonPreferences_Entry<bool> _debugPlayableAreas;
    private MelonPreferences_Entry<bool> _debugSkipMenu;
#endif

    public void Initialize()
    {
        _category = MelonPreferences.CreateCategory("HollowKnightNoAreaTransitions");
        _zoomSpeed = _category.CreateEntry("ZoomSpeed", 4f, "Camera Zoom Speed");
        _loadDistance = _category.CreateEntry(
            "LoadDistance",
            200f,
            "Load Distance",
            "For performance the mod will only load areas which are visible or within this many game units of the screen edge"
        );
        _unloadDistance = _category.CreateEntry(
            "UnloadDistance",
            300f,
            "Unload Distance",
            "Areas beyond this many game units of the screen edge will be unloaded (make sure this is greater than Load Distance)"
        );
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
        _debugTransitions = _category.CreateEntry(
            "DebugTransitions",
            false,
            "Show Transition Points",
            null,
            true,
            true
        );
        _debugPlayableAreas = _category.CreateEntry(
            "DebugPlayableAreas",
            false,
            "Show Playable Areas",
            null,
            true,
            true
        );
        _debugSkipMenu = _category.CreateEntry(
            "DebugSkipMenu",
            false,
            "Skip Menu on Load",
            null,
            true,
            true
        );
#endif
    }
}
