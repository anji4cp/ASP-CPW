@echo off
setlocal
title Restore Original Perfect World Patcher
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0launcher\restore-original-patcher.ps1"
if errorlevel 1 (
  echo.
  echo Restore failed. Read the message above.
  pause
  exit /b 1
)
echo.
echo Original patcher restored.
pause
