@echo off
setlocal
title ASP CPW Manager Installer
echo ASP CPW Manager - One Click Installer
echo.
set /p ASP_SERVER=Ubuntu address [127.0.0.1]: 
if "%ASP_SERVER%"=="" set ASP_SERVER=127.0.0.1
set /p ASP_PORT=SSH port [2223]: 
if "%ASP_PORT%"=="" set ASP_PORT=2223
echo.
echo SSH username is the Ubuntu login name, NOT the password.
echo For the PWKU VM, normally use: pwadmin
set /p ASP_USER=SSH username [pwadmin]: 
if "%ASP_USER%"=="" set ASP_USER=pwadmin
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\install-from-windows.ps1" -Server "%ASP_SERVER%" -Port %ASP_PORT% -User "%ASP_USER%"
set "ASP_EXIT=%ERRORLEVEL%"
if not "%ASP_EXIT%"=="0" (
  echo.
  echo Installation failed. Read the message above.
  pause
  exit /b 1
)
echo.
echo Installation completed.
pause
