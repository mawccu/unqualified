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

## Play with a friend or on one PC

Run the game twice (or once on each of two PCs on the same network). Press Esc. In one: Host a game. In the other: type the host's address (127.0.0.1 on the same PC) and press Join. Up to 4 players. The host can restart with F5; clients cannot.

## Test

```
godot --headless --path . res://tests/m0_selftest.tscn
powershell -File tests\run_net_tests.ps1
```

The first prints PASS or FAIL per check and ends with a count; it tests the interaction rules. The second starts several real game processes on this PC and checks the network behaviour (races for one prop, disconnects, out-of-range requests, the 4 player lobby). Run a single scenario with `-Only contend3,far`. Neither can tell you whether the game feels right.
