param([string]$ClientPath = "")
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ClientPath)) { $ClientPath = Read-Host "Perfect World client folder" }
$ClientPath = [IO.Path]::GetFullPath($ClientPath.Trim().Trim('"'))
$dir = Join-Path $ClientPath "patcher"
$launcherDir = Join-Path $ClientPath "launcher"
$current = Join-Path $dir "patcher.exe"
$core = Join-Path $dir "patcher-core.exe"
$launcherCurrent = Join-Path $launcherDir "Launcher.exe"
$launcherCore = Join-Path $launcherDir "Launcher-core.exe"
if (-not (Test-Path -LiteralPath $core -PathType Leaf)) { throw "patcher-core.exe was not found." }
if (-not (Test-Path -LiteralPath $launcherCore -PathType Leaf)) { throw "Launcher-core.exe was not found." }
$running = @(Get-Process -Name "Launcher", "Launcher-core", "patcher", "patcher-core", "ASP-Launcher" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) { throw "Close Launcher, patcher, and ASP Launcher before restoring." }
[IO.File]::SetAttributes($core, ([IO.File]::GetAttributes($core) -band (-bnot [IO.FileAttributes]::Hidden) -band (-bnot [IO.FileAttributes]::System)))
[IO.File]::SetAttributes($launcherCore, ([IO.File]::GetAttributes($launcherCore) -band (-bnot [IO.FileAttributes]::Hidden) -band (-bnot [IO.FileAttributes]::System)))
Copy-Item -LiteralPath $core -Destination $current -Force
Copy-Item -LiteralPath $launcherCore -Destination $launcherCurrent -Force
$named = Join-Path $ClientPath "ASP-Launcher.exe"
if (Test-Path -LiteralPath $named) { Remove-Item -LiteralPath $named -Force }
Write-Host "Original Launcher.exe and patcher.exe restored successfully." -ForegroundColor Green
Write-Host "The core files were retained as additional safety copies."
