@echo off
setlocal
echo Comparing admitted and unavailable travel in bounded original-code probes.
echo Media, view and impact endpoints are recording stubs.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\work\analysis-tools\Show-TargetOrderProbe.ps1" -RunDirectory "%~dp0." -Stages -Probe 12
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\work\analysis-tools\Show-TargetOrderProbe.ps1" -RunDirectory "%~dp0." -Stages -Probe 13
set "result=%errorlevel%"
pause
exit /b %result%
