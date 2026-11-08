@echo off
call "%~dp0Launch-Original.cmd" --native
exit /b %errorlevel%
