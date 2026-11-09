@echo off
setlocal
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:"%~dp0Iced.dll" /out:"%~dp0PeDisasm.exe" "%~dp0PeDisasm.cs"
exit /b %errorlevel%
