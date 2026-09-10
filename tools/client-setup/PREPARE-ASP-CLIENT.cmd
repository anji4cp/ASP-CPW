@echo off
setlocal
title ASP Perfect World Client Preparation
echo ============================================================
echo              ASP PERFECT WORLD CLIENT SETUP
echo ============================================================
echo.
echo This prepares Launcher.exe, patcher.exe, update URL, PID,
echo server list, and clean baseline version files.
echo A backup is created before the original client is changed.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0prepare-client.ps1"
set "ASP_EXIT=%ERRORLEVEL%"
echo.
if not "%ASP_EXIT%"=="0" (
  echo Client preparation failed. The original files were not committed unless stated above.
  pause
  exit /b %ASP_EXIT%
)
echo Client preparation completed successfully.
pause
exit /b 0

