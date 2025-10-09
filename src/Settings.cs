namespace HollowKnightNoAreaTransitions;

public class Settings
{
    public float ZoomSpeed => _zoomSpeed.Value;
    public float LoadDistance => _loadDistance.Value;
    public float UnloadDistance => _unloadDistance.Value;
    public bool FreezeOtherScenes => _freezeOtherScenes.Value;
    public bool GenerateChunkMaps => _generateChunkMaps.Value;
    public bool DebugLogs => _debugLogs.Value;

    private MelonPreferences_Category _category;
    private MelonPreferences_Entry<float> _zoomSpeed;
    private MelonPreferences_Entry<float> _loadDistance;
    private MelonPreferences_Entry<float> _unloadDistance;
    private MelonPreferences_Entry<bool> _freezeOtherScenes;
    private MelonPreferences_Entry<bool> _generateChunkMaps;
    private MelonPreferences_Entry<bool> _debugLogs;

#if DEBUG
    public bool DebugColliders => _debugColliders.Value;
    public bool DebugTilemaps => _debugTilemaps.Value;
    public bool DebugTransitions => _debugTransitions.Value;
    public bool DebugPlayableAreas => _debugPlayableAreas.Value;
    public bool DebugSkipMenu => _debugSkipMenu.Value;
    public int DebugPreselectSave => _debugPreselectSave.Value;
    public bool DebugOnlyShowOutlines => _debugOnlyShowOutlines.Value;
    private MelonPreferences_Entry<bool> _debugColliders;
    private MelonPreferences_Entry<bool> _debugTilemaps;
    private MelonPreferences_Entry<bool> _debugTransitions;
    private MelonPreferences_Entry<bool> _debugPlayableAreas;
    private MelonPreferences_Entry<bool> _debugSkipMenu;
    private MelonPreferences_Entry<int> _debugPreselectSave;
    private MelonPreferences_Entry<bool> _debugOnlyShowOutlines;
#endif

    public void Initialize()
    {
        _category = MelonPreferences.CreateCategory("HollowKnightNoAreaTransitions");
        _zoomSpeed = _category.CreateEntry("ZoomSpeed", 2f, "Camera Zoom Speed");
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
        _freezeOtherScenes = _category.CreateEntry(
            "FreezeOtherScenes",
            true,
            "Freeze Other Scenes",
            "When enabled, only scripts in the scene the player is in will be running and others will be locked"
        );
        _generateChunkMaps = _category.CreateEntry(
            "GenerateChunkMaps",
            false,
            "Generate Chunk Maps",
            "When enabled, maps will be computed automatically when loading into a new area (may cause stuttering)"
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
        _debugTilemaps = _category.CreateEntry(
            "DebugTilemaps",
            false,
            "Show Chunk Tilemaps",
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
        _debugPreselectSave = _category.CreateEntry(
            "DebugPreselectSave",
            0,
            "Preselect Save Slot",
            "Save slot to automatically load on startup, or 0 to disable",
            true,
            true
        );
        _debugOnlyShowOutlines = _category.CreateEntry(
            "DebugOnlyShowOutlines",
            false,
            "Only Show Outlines of Playable Areas and Transitions",
            null,
            true,
            true
        );
#endif
    }
}
