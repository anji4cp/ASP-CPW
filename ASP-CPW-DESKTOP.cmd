@echo off
setlocal
set "APP=%~dp0desktop\bin\ASP-CPW-Desktop.exe"
if not exist "%APP%" (
  call "%~dp0desktop\BUILD-DESKTOP.cmd"
  if errorlevel 1 exit /b 1
)
start "" "%APP%"
