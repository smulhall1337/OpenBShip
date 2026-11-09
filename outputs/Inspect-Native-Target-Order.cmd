@echo off
setlocal
echo Showing a bounded original-code targeting experiment.
echo This is a numerical comparison, not captured native gameplay.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\work\analysis-tools\Show-TargetOrderProbe.ps1" -RunDirectory "%~dp0" -Probe 6
set "result=%errorlevel%"
pause
exit /b %result%
