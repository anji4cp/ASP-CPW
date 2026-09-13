@echo off
setlocal
title Build ASP Launcher
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo Microsoft .NET Framework C# compiler was not found.
  pause
  exit /b 1
)
if not exist "%~dp0bin" mkdir "%~dp0bin"
set /p "APP_VERSION="<"%~dp0VERSION"
set "OUTPUT=%~dp0bin\ASP-Launcher-%APP_VERSION%.exe"
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32manifest:"%~dp0src\app.manifest" /out:"%OUTPUT%" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "%~dp0src\*.cs"
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)
copy /y "%OUTPUT%" "%~dp0bin\ASP-Launcher.exe" >nul
echo Built: %OUTPUT%
exit /b 0
