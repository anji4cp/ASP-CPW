@echo off
setlocal
set /p "APP_VERSION="<"%~dp0desktop\VERSION"
set "APP=%~dp0desktop\bin\ASP-CPW-Desktop-%APP_VERSION%.exe"
if not exist "%APP%" (
  call "%~dp0desktop\BUILD-DESKTOP.cmd"
  if errorlevel 1 exit /b 1
)
start "" "%APP%"
