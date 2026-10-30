# UNQUALIFIED (working title)

First-person co-op game about friends whose résumé lies become obligations on the job. This repository holds the Godot project. The design pack is in `docs/pack`.

Status: M0 foundation. See `docs/IMPLEMENTATION_STATUS.md`.

## Run it

Engine: Godot 4.7.2 stable (installed with `winget install --id GodotEngine.GodotEngine --version 4.7.2`).

Open the folder in the Godot editor and press F5, or from a terminal:

```
godot --path .
```

If `godot` is not found in an already open terminal, open a new one, or use the full path under `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe`.

## Controls

WASD move, mouse look, Shift sprint, E pick up, left click place on a socket, Q drop, Esc menu (FOV and mouse sensitivity), F5 restart.

## Test

```
godot --headless --path . res://tests/m0_selftest.tscn
```

Prints PASS or FAIL per check and ends with a count. It tests the interaction rules only. It cannot tell you whether the game feels right.
