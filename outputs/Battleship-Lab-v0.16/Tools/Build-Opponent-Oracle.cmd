@echo off
setlocal
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x86 /out:"%~dp0ClassicTargetOracle.exe" "%~dp0ClassicTargetOracle.cs"
exit /b %errorlevel%
