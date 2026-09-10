@echo off
setlocal
title ASP CPW One-Click Publisher
echo ============================================================
echo                  ASP CPW PATCH PUBLISHER
echo ============================================================
echo.
echo Put the changed files inside PATCH-FILES first.
echo Read CARA-PAKAI.md when using this tool for the first time.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publisher\publish-patch.ps1"
set "ASP_EXIT=%ERRORLEVEL%"
echo.
if not "%ASP_EXIT%"=="0" (
  echo Patch was NOT published. Read the error above.
  pause
  exit /b %ASP_EXIT%
)
echo Patch published successfully.
pause
exit /b 0

