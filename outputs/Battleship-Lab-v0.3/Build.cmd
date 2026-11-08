@echo off
setlocal
set "compiler=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%compiler%" (
    echo The Windows .NET Framework C sharp compiler could not be found.
    exit /b 1
)
"%compiler%" /nologo /target:winexe /platform:x64 /optimize+ /debug:pdbonly /main:FleetLab.Program /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:"%~dp0BattleshipLab.exe" "%~dp0Source\Assets.cs" "%~dp0Source\Game.cs" "%~dp0Source\ClassicGeometry.cs" "%~dp0Source\ClassicShips.cs" "%~dp0Source\Presentation.cs" "%~dp0Source\Audio.cs" "%~dp0Source\MainForm.cs" "%~dp0Source\Validation.cs" "%~dp0Source\Program.cs"
if errorlevel 1 exit /b 1
echo Built BattleshipLab.exe for Windows x64.
exit /b 0
