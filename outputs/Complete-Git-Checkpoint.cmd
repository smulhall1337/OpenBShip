@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\Complete-Git-Checkpoint.ps1"
if errorlevel 1 (
    echo Local Git checkpoint did not finish. See the error above.
    pause
    exit /b 1
)
echo Local Git checkpoint finished. Nothing was uploaded.
pause
exit /b 0
