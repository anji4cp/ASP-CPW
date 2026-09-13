@echo off
setlocal
set /p "APP_VERSION="<"%~dp0launcher\VERSION"
set "APP=%~dp0launcher\bin\ASP-Launcher-%APP_VERSION%.exe"
if not exist "%APP%" (
  call "%~dp0launcher\BUILD-LAUNCHER.cmd"
  if errorlevel 1 exit /b 1
)
start "" "%APP%"
