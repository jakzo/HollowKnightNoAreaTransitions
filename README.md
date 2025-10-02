# HollowKnightNoAreaTransitions Mod

Hollow Knight: Silksong mod that makes Pharloom one big level rather than a
collection of separate rooms.

## Mapping

To create your own maps from multiple areas:

- Use a debug build of the mod
- Set `DebugColliders = true` in the mod preferences
  - Green borders around terrain colliders
  - Blue borders around transitions
  - Cyan borders around transitions to other areas within the same map
    (transition disabled)
- Mouse wheel to zoom
- `Ctrl-O` to enable/disable the mod (buggy, enabled by default so probably
  don't need this)
- `Ctrl-P` to fly
- `Ctrl-L` to log the names of nearby `GameObject`s (useful for finding an
  object you want to remove)
  - Flashes the objects so you can see which ones were logged
  - If you also hold `Shift` it will only select colliders
- `Ctrl-K` to load the area connected to the nearest transition
- Hold `Ctrl` while clicking and dragging with the mouse to move areas
  - Hit detection is a bit buggy, click around until you find where you are able
    to drag the area
- `UserData/HKNAT_ChangedChunks.txt` contains code for the map of areas you have
  moved with the mouse
  - Copy this code into your map file
- Use these code snippets in the Unity Explorer console for testing collider
  placement
  ```csharp
  HKNAT.HeroPositionInChunk("Tut_02");
  HKNAT.CreateTestingCollider("Bonetown", new Rect(182f, 0f, 2f, 8f));
  ```

## Development

```ps1
dotnet build
dotnet build -p:StartGame=true
.\BuildAndRun.ps1
```

### MonoMod HookGen

The `On.*` hooks in the code come from the `MMHOOK_Assembly-CSharp.dll`
reference file which can be generated like by
[downloading MonoMod](https://github.com/MonoMod/MonoMod/releases) then running:

```ps1
.\MonoMod.RuntimeDetour.HookGen.exe --private "C:\Program Files (x86)\GOG Galaxy\Games\Hollow Knight Silksong\Hollow Knight Silksong_Data\Managed\Assembly-CSharp.dll"
```
