@echo off
setlocal
set "compiler=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%compiler%" (
    echo The Windows .NET Framework C sharp compiler could not be found.
    exit /b 1
)
"%compiler%" /nologo /target:winexe /platform:x64 /optimize+ /debug:pdbonly /main:FleetLab.Program /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:"%~dp0BattleshipLab.exe" "%~dp0Source\Assets.cs" "%~dp0Source\DisplaySettings.cs" "%~dp0Source\PreferencesProgram.cs" "%~dp0Source\PreferencesPage.cs" "%~dp0Source\CreditsProgram.cs" "%~dp0Source\CreditsCatalog.cs" "%~dp0Source\CreditsPresentation.cs" "%~dp0Source\CreditsPage.cs" "%~dp0Source\SoundLevel.cs" "%~dp0Source\ClassicPalette.cs" "%~dp0Source\ClassicDamage.cs" "%~dp0Source\ClassicPat.cs" "%~dp0Source\ClipSound.cs" "%~dp0Source\WaveOutput.cs" "%~dp0Source\ClipStudy.cs" "%~dp0Source\ClipFrames.cs" "%~dp0Source\MovieCatalog.cs" "%~dp0Source\EntryProgram.cs" "%~dp0Source\EntryPlayer.cs" "%~dp0Source\FleetProgram.cs" "%~dp0Source\FleetSetup.cs" "%~dp0Source\SoloProgram.cs" "%~dp0Source\SoloSetup.cs" "%~dp0Source\TitleProgram.cs" "%~dp0Source\FrontMenu.cs" "%~dp0Source\QuitProgram.cs" "%~dp0Source\StartupProgram.cs" "%~dp0Source\StartupSound.cs" "%~dp0Source\StartupPage.cs" "%~dp0Source\EndingProgram.cs" "%~dp0Source\EndingPlayer.cs" "%~dp0Source\ClassicWeapons.cs" "%~dp0Source\ClassicSequence.cs" "%~dp0Source\ClassicImpact.cs" "%~dp0Source\ClassicBackdrop.cs" "%~dp0Source\ClassicMessages.cs" "%~dp0Source\ClassicHud.cs" "%~dp0Source\Game.cs" "%~dp0Source\OriginalOpponent.cs" "%~dp0Source\OriginalPlacement.cs" "%~dp0Source\ClassicGeometry.cs" "%~dp0Source\ClassicShips.cs" "%~dp0Source\Presentation.cs" "%~dp0Source\Audio.cs" "%~dp0Source\PauseActions.cs" "%~dp0Source\MainForm.cs" "%~dp0Source\Validation.cs" "%~dp0Source\Program.cs"
if errorlevel 1 exit /b 1
echo Built BattleshipLab.exe for Windows x64.
"%compiler%" /nologo /target:exe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:"%~dp0BattleshipLab.exe" /out:"%~dp0ShutdownCheck.exe" "%~dp0Tools\Check-Shutdown.cs"
if errorlevel 1 exit /b 1
"%compiler%" /nologo /target:exe /platform:x64 /r:System.Drawing.dll /r:"%~dp0BattleshipLab.exe" /out:"%~dp0DamageCheck.exe" "%~dp0Tools\Check-DamageOrderTraces.cs"
if errorlevel 1 exit /b 1
exit /b 0

