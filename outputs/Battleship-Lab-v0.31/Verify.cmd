@echo off
setlocal
start "" /wait "%~dp0BattleshipLab.exe" --self-check "%~dp0Self-Check.txt"
if errorlevel 1 exit /b 1
start "" /wait "%~dp0BattleshipLab.exe" --ui-check "%~dp0UI-Check.txt"
if errorlevel 1 exit /b 1
type "%~dp0Self-Check.txt"
type "%~dp0UI-Check.txt"
start "" /wait "%~dp0ShutdownCheck.exe" "%~dp0Assets" "%~dp0Shutdown-Check.txt"
if errorlevel 1 exit /b 1
type "%~dp0Shutdown-Check.txt"
"%~dp0DamageCheck.exe" "%~dp0Evidence\Native-Damage-Order-Cases.csv" "%~dp0Evidence\Native-Damage-Order-Events.csv" "%~dp0Damage-Check.txt"
if errorlevel 1 exit /b 1
exit /b 0

