@echo off
setlocal
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x86 /out:"%~dp0ClassicPatOracle.exe" "%~dp0ClassicPatOracle.cs"
exit /b %errorlevel%
