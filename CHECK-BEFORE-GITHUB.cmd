@echo off
setlocal
title ASP CPW GitHub Safety Check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\check-before-github.ps1"
set "ASP_EXIT=%ERRORLEVEL%"
echo.
if not "%ASP_EXIT%"=="0" (
  echo Safety check failed. Remove the reported files before uploading.
  pause
  exit /b %ASP_EXIT%
)
echo Repository safety check passed.
pause
exit /b 0
