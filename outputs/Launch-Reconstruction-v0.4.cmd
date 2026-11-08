@echo off
setlocal
if not exist "%~dp0Battleship-Lab-v0.4\BattleshipLab.exe" (
    echo Cannot find the Fleet Lab build. Run Battleship-Lab-v0.4\Build.cmd.
    pause
    exit /b 1
)
start "" "%~dp0Battleship-Lab-v0.4\BattleshipLab.exe"
exit /b 0
