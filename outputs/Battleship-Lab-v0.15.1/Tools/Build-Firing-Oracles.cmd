@echo off
setlocal
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x86 /out:"%~dp0ClassicFiringPatOracle.exe" "%~dp0ClassicFiringPatOracle.cs"
if errorlevel 1 exit /b 1
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x86 /out:"%~dp0ClassicSequenceOracle.exe" "%~dp0ClassicSequenceOracle.cs"
exit /b %errorlevel%
