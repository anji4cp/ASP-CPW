@echo off
setlocal
title Build ASP CPW Desktop Manager
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo Microsoft .NET Framework C# compiler was not found.
  pause
  exit /b 1
)
if not exist "%~dp0bin" mkdir "%~dp0bin"
set /p "APP_VERSION="<"%~dp0VERSION"
if "%APP_VERSION%"=="" (
  echo desktop\VERSION is empty.
  exit /b 1
)
set "OUTPUT=%~dp0bin\ASP-CPW-Desktop-%APP_VERSION%.exe"
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /out:"%OUTPUT%" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "%~dp0src\*.cs"
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)
echo Built: %OUTPUT%
exit /b 0
