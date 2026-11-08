@echo off
setlocal
for %%N in (ClassicShotPatOracle ClassicCombatSequenceOracle ClassicPhaseOracle) do (
 "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x86 /out:"%~dp0%%N.exe" "%~dp0%%N.cs"
 if errorlevel 1 exit /b 1
)
exit /b 0
