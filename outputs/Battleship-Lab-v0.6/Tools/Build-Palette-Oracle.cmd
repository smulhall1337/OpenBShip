@echo off
setlocal
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:exe /platform:x86 /out:"%~dp0ClassicPaletteOracle.exe" "%~dp0ClassicPaletteOracle.cs"
exit /b %errorlevel%
