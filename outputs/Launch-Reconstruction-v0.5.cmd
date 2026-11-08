@echo off
setlocal
if not exist "%~dp0Battleship-Lab-v0.5\BattleshipLab.exe" (
    echo Cannot find the Fleet Lab build. Run Battleship-Lab-v0.5\Build.cmd.
    pause
    exit /b 1
)
start "" "%~dp0Battleship-Lab-v0.5\BattleshipLab.exe"
exit /b 0
