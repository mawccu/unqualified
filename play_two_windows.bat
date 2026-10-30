@echo off
rem Opens two copies of the game side by side so you can test multiplayer on one PC.
set GODOT=%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_win64.exe
start "" "%GODOT%" --path "%~dp0." --windowed --resolution 940x600 --position 0,80
start "" "%GODOT%" --path "%~dp0." --windowed --resolution 940x600 --position 960,80
