@echo off
setlocal
if not exist "%~dp0build\current\BattleshipLab.exe" (
    echo Run Setup.ps1 first. See README.md for setup instructions.
    pause
    exit /b 1
)
if not exist "%~dp0build\current\Assets\Graphics\ISLAND.COL" (
    echo Original game assets are missing. Run Setup.ps1 with your extracted game folder.
    pause
    exit /b 1
)
start "" "%~dp0build\current\BattleshipLab.exe"
exit /b 0
