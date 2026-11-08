@echo off
setlocal
set "gameDir=%~dp0..\work\reference-ddraw"
set "graphicsMode=cnc-ddraw 7.1, windowed"
if /i "%~1"=="--native" (
    set "gameDir=%~dp0..\work\reference-rip"
    set "graphicsMode=Windows native DirectDraw baseline"
)
set "logDir=%~dp0..\work\reference-logs"
set "BATTLESHIP_ISO=C:\Users\Sean\Documents\Battleship-The-Classic-Naval-Warfare-Game_Win_EN_ISO-Version\Battleship.iso"
set "BATTLESHIP_LOG=%logDir%\launch-batch-last.txt"
set "BATTLESHIP_GAME=%gameDir%"
set "BATTLESHIP_REG_BACKUP=%logDir%\registry-before-first-setup.json"
set "BATTLESHIP_LAUNCHER=%~f0"
if /i not "%~1"=="--native" if not exist "%gameDir%\ddraw.dll" (
    echo Cannot find the game-local DirectDraw compatibility library.
    pause
    exit /b 1
)
if not exist "%gameDir%\Bs.exe" (
    echo Cannot find the reference game: "%gameDir%\Bs.exe"
    pause
    exit /b 1
)
if not exist "%logDir%" mkdir "%logDir%"
if /i "%~1"=="--configure-only" goto configureOnly
if not exist "%BATTLESHIP_ISO%" (
    echo Cannot find the Battleship ISO: "%BATTLESHIP_ISO%"
    pause
    exit /b 1
)
pushd "%gameDir%"
if errorlevel 1 (
    echo Cannot open the reference game directory.
    pause
    exit /b 1
)
>"%BATTLESHIP_LOG%" echo Launch time: %date% %time%
>>"%BATTLESHIP_LOG%" echo Executable: "%CD%\Bs.exe"
>>"%BATTLESHIP_LOG%" echo Graphics mode: %graphicsMode%
echo Checking Battleship CD image...
powershell.exe -NoProfile -NonInteractive -Command "$ErrorActionPreference='Stop'; try { $image=Get-DiskImage -ImagePath $env:BATTLESHIP_ISO; if ($image.Attached) { $status='Using the already mounted Battleship ISO.' } else { $image=Mount-DiskImage -ImagePath $env:BATTLESHIP_ISO -StorageType ISO -Access ReadOnly -PassThru; $status='Mounted Battleship ISO read-only.' }; Write-Host $status; Add-Content -LiteralPath $env:BATTLESHIP_LOG -Value $status; $volumes=@(); for ($i=0; $i -lt 20; $i++) { $volumes=@($image | Get-Volume | Where-Object DriveLetter); if ($volumes.Count) { break }; Start-Sleep -Milliseconds 250 }; if (!$volumes.Count) { throw 'The ISO is attached but no CD drive letter is available.' }; foreach ($volume in $volumes) { $line='CD drive: '+$volume.DriveLetter+':'; Write-Host $line; Add-Content -LiteralPath $env:BATTLESHIP_LOG -Value $line }; exit 0 } catch { $message='CD mount failed: '+$_.Exception.Message; Write-Host $message; Add-Content -LiteralPath $env:BATTLESHIP_LOG -Value $message; exit 1 }"
if errorlevel 1 (
    popd
    echo The game was not started because its CD image could not be mounted.
    echo Launch log: "%BATTLESHIP_LOG%"
    pause
    exit /b 1
)
echo Configuring Battleship resource paths...
call :ensureConfigured
if errorlevel 1 (
    popd
    echo Resource setup did not complete. The game was not started.
    echo If Windows displayed an administrator prompt, allow the setup helper.
    echo Launch log: "%BATTLESHIP_LOG%"
    pause
    exit /b 1
)
echo Starting Battleship...
echo Graphics mode: %graphicsMode%
if /i "%~1"=="--diagnostic" goto diagnosticLaunch
"%CD%\Bs.exe"
set "result=%errorlevel%"
goto launchFinished

:diagnosticLaunch
echo Recording startup faults for up to 25 seconds...
"%~dp0..\work\NativeTrace.exe" "%CD%\Bs.exe" "%logDir%\desktop-crash"
set "result=%errorlevel%"
echo Diagnostic files: "%logDir%\desktop-crash"

:launchFinished
popd
>>"%BATTLESHIP_LOG%" echo Exit code: %result%
if /i "%~1"=="--diagnostic" (
    echo Diagnostic capture finished with status %result%.
) else (
    echo Battleship exited with code %result%.
)
echo Launch log: "%BATTLESHIP_LOG%"
pause
exit /b %result%

:configureOnly
call :configurePaths
exit /b %errorlevel%

:ensureConfigured
call :configurePaths
set "setupResult=%errorlevel%"
if "%setupResult%"=="0" exit /b 0
if not "%setupResult%"=="5" exit /b %setupResult%
echo Windows will request administrator permission for the resource settings only.
echo The game will start from this original launcher after setup finishes.
powershell.exe -NoProfile -NonInteractive -Command "$ErrorActionPreference='Stop'; try { $q=[char]34; $arguments='/d /s /c '+$q+$q+$env:BATTLESHIP_LAUNCHER+$q+' --configure-only'+$q; $helper=Start-Process -FilePath $env:ComSpec -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru; $helper.WaitForExit(); $result=[int]$helper.ExitCode; Add-Content -LiteralPath $env:BATTLESHIP_LOG -Value ('Elevated resource setup exit code: '+$result); exit $result } catch { $message='Administrator setup was canceled or could not start: '+$_.Exception.Message; Write-Host $message; Add-Content -LiteralPath $env:BATTLESHIP_LOG -Value $message; exit 1 }"
if errorlevel 1 exit /b 1
rem Verify the settings in the original process before launching the game.
call :configurePaths
exit /b %errorlevel%

:configurePaths
powershell.exe -NoProfile -NonInteractive -Command "$ErrorActionPreference='Stop'; $base=$null; $key=$null; try { $root=[IO.Path]::GetFullPath($env:BATTLESHIP_GAME); $paths=@{BSDIR='.'; GRAPATH='.\GRAPHICS'; HWEBPATH='.\HWEBS'; LWEBPATH='.\LWEBS'; PINPPATH='.\PINP'; SFXPATH='.\SFX'}; if (!(Test-Path -LiteralPath (Join-Path $root 'DISK1.ID'))) { throw 'Reference DISK1.ID marker is missing.' }; foreach ($name in $paths.Keys) { $resource=Join-Path $root $paths[$name]; if (!(Test-Path -LiteralPath $resource)) { throw ('Missing resource directory: '+$resource) } }; $base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::Registry32); $subkey='SOFTWARE\Hasbro_Interactive\Battleship'; $key=$base.OpenSubKey($subkey); if (!(Test-Path -LiteralPath $env:BATTLESHIP_REG_BACKUP)) { $values=@(); foreach ($name in $paths.Keys) { $exists=($null -ne $key -and $key.GetValueNames() -contains $name); $entry=@{Name=$name; Existed=$exists}; if ($exists) { $entry.Kind=$key.GetValueKind($name).ToString(); $entry.Value=$key.GetValue($name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) }; $values+=$entry }; @{KeyExisted=($null -ne $key); Values=$values} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $env:BATTLESHIP_REG_BACKUP -Encoding UTF8 }; $correct=($null -ne $key); foreach ($name in $paths.Keys) { if ($null -eq $key -or $key.GetValue($name) -ne $paths[$name]) { $correct=$false } }; if ($key) { $key.Dispose(); $key=$null }; if (!$correct) { $key=$base.CreateSubKey($subkey); foreach ($name in $paths.Keys) { $key.SetValue($name,$paths[$name],[Microsoft.Win32.RegistryValueKind]::String) } }; foreach ($name in $paths.Keys) { $line='Registry32 '+$name+'='+$paths[$name]; Add-Content -LiteralPath $env:BATTLESHIP_LOG -Value $line }; Write-Host '32-bit Battleship resource paths are configured.'; exit 0 } catch { $message='Resource configuration failed: '+$_.Exception.Message; Write-Host $message; Add-Content -LiteralPath $env:BATTLESHIP_LOG -Value $message; $cause=$_.Exception; while ($cause) { if ($cause -is [UnauthorizedAccessException] -or $cause -is [System.Security.SecurityException]) { exit 5 }; $cause=$cause.InnerException }; exit 1 } finally { if ($key) { $key.Dispose() }; if ($base) { $base.Dispose() } }"
exit /b %errorlevel%
