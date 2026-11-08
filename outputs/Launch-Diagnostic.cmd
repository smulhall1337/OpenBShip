@echo off
call "%~dp0Launch-Original.cmd" --diagnostic
exit /b %errorlevel%
