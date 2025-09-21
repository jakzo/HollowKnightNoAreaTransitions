# HollowKnightNoAreaTransitions Mod

Hollow Knight: Silksong mod that makes Pharloom one big level rather than a
collection of separate rooms.

## Development

```ps1
dotnet build
```

### MonoMod HookGen

The `On.*` hooks in the code come from the `MMHOOK_Assembly-CSharp.dll`
reference file which can be generated like by
[downloading MonoMod](https://github.com/MonoMod/MonoMod/releases) then running:

```ps1
.\MonoMod.RuntimeDetour.HookGen.exe --private "C:\Program Files (x86)\GOG Galaxy\Games\Hollow Knight Silksong\Hollow Knight Silksong_Data\Managed\Assembly-CSharp.dll"
```
