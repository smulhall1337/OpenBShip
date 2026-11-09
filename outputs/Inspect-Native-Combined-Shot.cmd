@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\work\analysis-tools\Show-CombinedShotProbe.ps1" -RunDirectory "%~dp0." -Probe 8
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\work\analysis-tools\Show-CombinedShotProbe.ps1" -RunDirectory "%~dp0." -Probe 14
set "result=%errorlevel%"
pause
exit /b %result%
