@echo off
setlocal
title ASP CPW Patch Preview
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publisher\publish-patch.ps1" -PreviewOnly
set "ASP_EXIT=%ERRORLEVEL%"
echo.
pause
exit /b %ASP_EXIT%

