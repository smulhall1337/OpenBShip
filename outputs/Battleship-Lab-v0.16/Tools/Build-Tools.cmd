@echo off
setlocal
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:System.Drawing.dll /out:"%~dp0InspectAssets.exe" "%~dp0..\Source\Assets.cs" "%~dp0InspectAssets.cs"
exit /b %errorlevel%
