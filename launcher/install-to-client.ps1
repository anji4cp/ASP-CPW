param([string]$ClientPath = "")

$ErrorActionPreference = "Stop"
$LauncherRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Wrapper = Join-Path $LauncherRoot "bin\ASP-Launcher.exe"

function Read-SavedClientPath {
  $settings = Join-Path $env:LOCALAPPDATA "ASP Editor Studio\CPW Desktop\settings.ini"
  if (-not (Test-Path -LiteralPath $settings)) { return "" }
  $line = Get-Content -LiteralPath $settings | Where-Object { $_ -like "ClientPath=*" } | Select-Object -First 1
  if (-not $line) { return "" }
  try { return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($line.Substring($line.IndexOf('=') + 1))) }
  catch { return "" }
}

if ([string]::IsNullOrWhiteSpace($ClientPath)) { $ClientPath = Read-SavedClientPath }
if ([string]::IsNullOrWhiteSpace($ClientPath)) { $ClientPath = Read-Host "Perfect World client folder" }
$ClientPath = [IO.Path]::GetFullPath($ClientPath.Trim().Trim('"'))
$PatcherDir = Join-Path $ClientPath "patcher"
$LauncherDir = Join-Path $ClientPath "launcher"
$Current = Join-Path $PatcherDir "patcher.exe"
$Core = Join-Path $PatcherDir "patcher-core.exe"
$LauncherCurrent = Join-Path $LauncherDir "Launcher.exe"
$LauncherCore = Join-Path $LauncherDir "Launcher-core.exe"
$NamedCopy = Join-Path $ClientPath "ASP-Launcher.exe"
$Element = Join-Path $ClientPath "element\elementclient.exe"

if (-not (Test-Path -LiteralPath $Wrapper -PathType Leaf)) { throw "Build ASP Launcher first: $Wrapper" }
if (-not (Test-Path -LiteralPath $Current -PathType Leaf) -and -not (Test-Path -LiteralPath $Core -PathType Leaf)) { throw "patcher.exe was not found in $PatcherDir" }
if (-not (Test-Path -LiteralPath $LauncherCurrent -PathType Leaf) -and -not (Test-Path -LiteralPath $LauncherCore -PathType Leaf)) { throw "Launcher.exe was not found in $LauncherDir" }
if (-not (Test-Path -LiteralPath $Element -PathType Leaf)) { throw "element\elementclient.exe was not found." }
$running = @(Get-Process -Name "Launcher", "Launcher-core", "patcher", "patcher-core", "ASP-Launcher" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) { throw "Close Launcher, patcher, and ASP Launcher before installing." }

if (-not (Test-Path -LiteralPath $Core)) {
  $product = (Get-Item -LiteralPath $Current).VersionInfo.ProductName
  if ($product -eq "ASP Launcher") { throw "ASP Launcher is already patcher.exe, but patcher-core.exe is missing. Restore the original patcher first." }
  $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
  $backupDir = Join-Path $PatcherDir "asp-launcher-backup\$stamp"
  New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
  Copy-Item -LiteralPath $Current -Destination (Join-Path $backupDir "patcher-original.exe")
  Move-Item -LiteralPath $Current -Destination $Core
}
elseif (Test-Path -LiteralPath $Current) {
  $product = (Get-Item -LiteralPath $Current).VersionInfo.ProductName
  if ($product -ne "ASP Launcher") { throw "patcher-core.exe already exists, but patcher.exe is not ASP Launcher. Nothing was overwritten." }
}

if (-not (Test-Path -LiteralPath $LauncherCore)) {
  $launcherProduct = (Get-Item -LiteralPath $LauncherCurrent).VersionInfo.ProductName
  if ($launcherProduct -eq "ASP Launcher") { throw "ASP Launcher is already Launcher.exe, but Launcher-core.exe is missing. Restore the original Launcher first." }
  $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
  $backupDir = Join-Path $LauncherDir "asp-launcher-backup\$stamp"
  New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
  Copy-Item -LiteralPath $LauncherCurrent -Destination (Join-Path $backupDir "Launcher-original.exe")
  Move-Item -LiteralPath $LauncherCurrent -Destination $LauncherCore
}
elseif (Test-Path -LiteralPath $LauncherCurrent) {
  $launcherProduct = (Get-Item -LiteralPath $LauncherCurrent).VersionInfo.ProductName
  if ($launcherProduct -ne "ASP Launcher") { throw "Launcher-core.exe already exists, but Launcher.exe is not ASP Launcher. Nothing was overwritten." }
}

function New-IconFromExecutable([string]$Executable, [string]$Destination) {
  Add-Type -AssemblyName System.Drawing
  $icon = [Drawing.Icon]::ExtractAssociatedIcon($Executable)
  if (-not $icon) { return $false }
  $stream = [IO.File]::Create($Destination)
  try { $icon.Save($stream) } finally { $stream.Dispose(); $icon.Dispose() }
  return $true
}

function Build-IconWrapper([string]$IconSource, [string]$Destination) {
  $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
  if (-not (Test-Path -LiteralPath $csc)) { $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe" }
  if (-not (Test-Path -LiteralPath $csc)) { Copy-Item -LiteralPath $Wrapper -Destination $Destination -Force; return }
  $tempIcon = Join-Path ([IO.Path]::GetTempPath()) ("asp-launcher-" + [Guid]::NewGuid().ToString("N") + ".ico")
  try {
    if (-not (New-IconFromExecutable $IconSource $tempIcon)) { Copy-Item -LiteralPath $Wrapper -Destination $Destination -Force; return }
    $sources = @(Get-ChildItem -LiteralPath (Join-Path $LauncherRoot "src") -Filter "*.cs" | ForEach-Object FullName)
    $arguments = @("/nologo", "/target:winexe", "/optimize+", "/platform:anycpu",
      ("/win32manifest:" + (Join-Path $LauncherRoot "src\app.manifest")), ("/win32icon:" + $tempIcon),
      ("/out:" + $Destination), "/reference:System.dll", "/reference:System.Core.dll",
      "/reference:System.Drawing.dll", "/reference:System.Windows.Forms.dll") + $sources
    & $csc $arguments
    if ($LASTEXITCODE -ne 0) { throw "Unable to build the icon-matched ASP wrapper." }
  } finally { if (Test-Path -LiteralPath $tempIcon) { Remove-Item -LiteralPath $tempIcon -Force } }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("asp-launcher-build-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
try {
  $patcherWrapper = Join-Path $tempRoot "patcher.exe"
  $launcherWrapper = Join-Path $tempRoot "Launcher.exe"
  Build-IconWrapper $Core $patcherWrapper
  Build-IconWrapper $LauncherCore $launcherWrapper
  Copy-Item -LiteralPath $patcherWrapper -Destination $Current -Force
  Copy-Item -LiteralPath $patcherWrapper -Destination $NamedCopy -Force
  Copy-Item -LiteralPath $launcherWrapper -Destination $LauncherCurrent -Force
} finally { if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force } }

$hiddenSystem = [IO.FileAttributes]::Hidden -bor [IO.FileAttributes]::System
[IO.File]::SetAttributes($Core, ([IO.File]::GetAttributes($Core) -bor $hiddenSystem))
[IO.File]::SetAttributes($LauncherCore, ([IO.File]::GetAttributes($LauncherCore) -bor $hiddenSystem))
Write-Host "ASP Launcher was installed successfully." -ForegroundColor Green
Write-Host "Original patcher: $Core"
Write-Host "Original launcher: $LauncherCore"
Write-Host "Integrated entry: $Current"
Write-Host "Integrated launcher: $LauncherCurrent"
Write-Host "Administrator/UAC: required automatically"
