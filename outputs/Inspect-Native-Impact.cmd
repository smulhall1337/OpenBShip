@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\work\analysis-tools\Show-DamageOrderEvidence.ps1" -Evidence "%~dp0Battleship-Lab-v0.31\Evidence"
set "result=%errorlevel%"
pause
exit /b %result%
