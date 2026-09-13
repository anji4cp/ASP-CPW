@echo off
setlocal
title Install ASP Launcher to Perfect World Client
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0launcher\install-to-client.ps1"
if errorlevel 1 (
  echo.
  echo Installation failed. Read the message above.
  pause
  exit /b 1
)
echo.
echo Installation complete. Launcher.exe and patcher.exe can now be used normally.
pause
