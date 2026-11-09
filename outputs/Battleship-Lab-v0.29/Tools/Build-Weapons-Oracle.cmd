@echo off
setlocal
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x86 /out:"%~dp0ClassicWeaponsOracle.exe" "%~dp0ClassicWeaponsOracle.cs"
exit /b %errorlevel%
