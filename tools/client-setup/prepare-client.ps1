param(
  [string]$ClientPath = "",
  [string]$Server = "",
  [string]$Port = "",
  [string]$User = "",
  [string]$PatchUrl = "",
  [string]$GameAddress = "",
  [string]$GamePort = "",
  [string]$NewsUrl = "",
  [string]$RegisterUrl = "",
  [string]$HomeUrl = "",
  [string]$SupportUrl = "",
  [string]$ForumUrl = ""
)

$ErrorActionPreference = "Stop"
$SetupRoot = $PSScriptRoot
$KnownHosts = Join-Path $SetupRoot "known_hosts"
$KnownHostsSsh = $KnownHosts.Replace("\", "/")
$Expected = @(
  "launcher\Launcher.exe",
  "patcher\patcher.exe",
  "patcher\skin\mainuni.xml",
  "patcher\server\pid.ini",
  "patcher\server\updateserver.txt",
  "patcher\server\serverlist.txt"
)

function Read-Default([string]$Prompt, [string]$Default) {
  $value = Read-Host "$Prompt [$Default]"
  if ([string]::IsNullOrWhiteSpace($value)) { return $Default }
  return $value.Trim()
}

function Get-EmbeddedPem([string]$Path) {
  $text = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($Path))
  $match = [regex]::Match($text, '-----BEGIN PUBLIC KEY-----[\s\S]{1,400}?-----END PUBLIC KEY-----')
  if (-not $match.Success) { throw "Public key marker was not found in $Path" }
  return $match.Value
}

function Test-ServerSignature([string]$Executable, [string]$ManifestUrl) {
  $pem = Get-EmbeddedPem $Executable
  $response = Invoke-WebRequest -UseBasicParsing -Uri $ManifestUrl -TimeoutSec 10
  # Windows PowerShell returns binary HTTP content as Byte[]. Casting that array
  # to [string] produces a list such as "35 32 49 ...", which hides the marker.
  $data = if ($response.Content -is [byte[]]) {
    [byte[]]$response.Content
  } else {
    [Text.Encoding]::UTF8.GetBytes([string]$response.Content)
  }
  $marker = [Text.Encoding]::ASCII.GetBytes("-----BEGIN ELEMENT SIGNATURE-----")
  $start = -1
  for ($i = 0; $i -le $data.Length - $marker.Length; $i++) {
    $matches = $true
    for ($j = 0; $j -lt $marker.Length; $j++) {
      if ($data[$i + $j] -ne $marker[$j]) { $matches = $false; break }
    }
    if ($matches) { $start = $i; break }
  }
  if ($start -lt 0) { throw "Server manifest has no RSA signature." }

  $signatureStart = $start + $marker.Length
  if ($signatureStart -lt $data.Length -and $data[$signatureStart] -eq 13) { $signatureStart++ }
  if ($signatureStart -lt $data.Length -and $data[$signatureStart] -eq 10) { $signatureStart++ }
  $body = New-Object byte[] $start
  [Array]::Copy($data, 0, $body, 0, $start)
  $signatureText = [Text.Encoding]::ASCII.GetString(
    $data, $signatureStart, $data.Length - $signatureStart)
  $signature = [Convert]::FromBase64String(($signatureText -replace '\s', ''))
  $openSslCommand = Get-Command openssl.exe -ErrorAction SilentlyContinue
  $openSsl = if ($openSslCommand) { $openSslCommand.Source } else { "C:\Program Files\Git\usr\bin\openssl.exe" }
  if (-not (Test-Path -LiteralPath $openSsl -PathType Leaf)) {
    throw "OpenSSL was not found. Install Git for Windows before preparing the client."
  }
  $verifyDir = Join-Path ([IO.Path]::GetTempPath()) ("asp-rsa-" + [Guid]::NewGuid().ToString("N"))
  New-Item -ItemType Directory -Force -Path $verifyDir | Out-Null
  try {
    [IO.File]::WriteAllBytes((Join-Path $verifyDir "manifest-body"), $body)
    [IO.File]::WriteAllBytes((Join-Path $verifyDir "signature.bin"), $signature)
    [IO.File]::WriteAllText((Join-Path $verifyDir "public.pem"), $pem + "`n", [Text.Encoding]::ASCII)
    & $openSsl dgst -md5 -verify (Join-Path $verifyDir "public.pem") `
      -signature (Join-Path $verifyDir "signature.bin") (Join-Path $verifyDir "manifest-body") | Out-Null
    return $LASTEXITCODE -eq 0
  } finally {
    Remove-Item -LiteralPath $verifyDir -Recurse -Force -ErrorAction SilentlyContinue
  }
}

if ([string]::IsNullOrWhiteSpace($ClientPath)) {
  $ClientPath = Read-Host "Full path to the Perfect World client folder"
}
if ([string]::IsNullOrWhiteSpace($ClientPath) -or
    -not (Test-Path -LiteralPath $ClientPath -PathType Container)) {
  throw "Client folder was not found: $ClientPath"
}
$ClientPath = (Resolve-Path -LiteralPath $ClientPath).Path
foreach ($relative in $Expected) {
  $path = Join-Path $ClientPath $relative
  if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required client file is missing: $path" }
}

Write-Host "Client: $ClientPath" -ForegroundColor Cyan
Write-Host "This operation patches two executables and resets updater cache/version files."
$answer = Read-Host "Type PREPARE to continue"
if ($answer -cne "PREPARE") {
  Write-Host "Cancelled. No files were changed." -ForegroundColor Yellow
  exit 2
}

$running = @(Get-Process -Name "Launcher", "Launcher-core", "patcher", "patcher-core", "ASP-Launcher", "elementclient" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
  throw "Close Launcher.exe, patcher.exe, and elementclient.exe before continuing."
}

# ASP Launcher keeps the original CPW executables beside its wrappers.  When
# integration is already installed, prepare those originals in place instead
# of trying to apply an RSA key to the small ASP wrapper executables.
$LauncherRelative = "launcher\Launcher.exe"
$PatcherRelative = "patcher\patcher.exe"
$LauncherCurrent = Join-Path $ClientPath $LauncherRelative
$PatcherCurrent = Join-Path $ClientPath $PatcherRelative
if ((Get-Item -LiteralPath $LauncherCurrent).VersionInfo.ProductName -eq "ASP Launcher") {
  $LauncherRelative = "launcher\Launcher-core.exe"
  if (-not (Test-Path -LiteralPath (Join-Path $ClientPath $LauncherRelative) -PathType Leaf)) {
    throw "ASP Launcher is installed, but launcher\Launcher-core.exe is missing."
  }
}
if ((Get-Item -LiteralPath $PatcherCurrent).VersionInfo.ProductName -eq "ASP Launcher") {
  $PatcherRelative = "patcher\patcher-core.exe"
  if (-not (Test-Path -LiteralPath (Join-Path $ClientPath $PatcherRelative) -PathType Leaf)) {
    throw "ASP Launcher is installed, but patcher\patcher-core.exe is missing."
  }
}

$Server = if ([string]::IsNullOrWhiteSpace($Server)) { Read-Default "Ubuntu address" "127.0.0.1" } else { $Server.Trim() }
$PortText = if ([string]::IsNullOrWhiteSpace($Port)) { Read-Default "SSH port" "2223" } else { $Port.Trim() }
$User = if ([string]::IsNullOrWhiteSpace($User)) { Read-Default "SSH username (not password)" "pwadmin" } else { $User.Trim() }
$PatchUrl = if ([string]::IsNullOrWhiteSpace($PatchUrl)) { Read-Default "Public patch URL" "http://127.0.0.1:8082/patch/" } else { $PatchUrl.Trim() }
$GameAddress = if ([string]::IsNullOrWhiteSpace($GameAddress)) { Read-Default "Game address" "127.0.0.1" } else { $GameAddress.Trim() }
$GamePortText = if ([string]::IsNullOrWhiteSpace($GamePort)) { Read-Default "Game port" "29001" } else { $GamePort.Trim() }
$NewsUrl = if ([string]::IsNullOrWhiteSpace($NewsUrl)) { Read-Default "Launcher news URL" "http://127.0.0.1:8081/launcher-news" } else { $NewsUrl.Trim() }
$RegisterUrl = if ([string]::IsNullOrWhiteSpace($RegisterUrl)) { Read-Default "Register URL" "http://127.0.0.1:8081/#register" } else { $RegisterUrl.Trim() }
$HomeUrl = if ([string]::IsNullOrWhiteSpace($HomeUrl)) { Read-Default "Arc / website URL" "http://127.0.0.1:8081/" } else { $HomeUrl.Trim() }
$SupportUrl = if ([string]::IsNullOrWhiteSpace($SupportUrl)) { Read-Default "Support URL" "http://127.0.0.1:8081/guide" } else { $SupportUrl.Trim() }
$ForumUrl = if ([string]::IsNullOrWhiteSpace($ForumUrl)) { Read-Default "Forum URL" "https://www.arcgames.com/en/forums/pwi/" } else { $ForumUrl.Trim() }

if ($PortText -notmatch '^[0-9]{1,5}$' -or [int]$PortText -lt 1 -or [int]$PortText -gt 65535) { throw "Invalid SSH port." }
if ($GamePortText -notmatch '^[0-9]{1,5}$' -or [int]$GamePortText -lt 1 -or [int]$GamePortText -gt 65535) { throw "Invalid game port." }
if ($User -notmatch '^[a-z_][a-z0-9_-]{0,31}$') { throw "Invalid SSH username." }
if ($Server -notmatch '^[A-Za-z0-9.-]+$' -or $GameAddress -notmatch '^[A-Za-z0-9.-]+$') { throw "Invalid server address." }
if ($PatchUrl -notmatch '^https?://[A-Za-z0-9.:-]+(?:/[A-Za-z0-9._~!$&''()*+,;=:@%-]*)*/$') { throw "Invalid patch URL; it must end with /." }

$Port = [int]$PortText
$GamePort = [int]$GamePortText
$Id = [Guid]::NewGuid().ToString("N")
$Work = Join-Path ([IO.Path]::GetTempPath()) "asp-client-$Id"
$RemoteLauncher = "/tmp/asp-launcher-$Id.exe"
$RemotePatcher = "/tmp/asp-patcher-$Id.exe"
$LocalLauncher = Join-Path $Work "Launcher.exe"
$LocalPatcher = Join-Path $Work "patcher.exe"
$LocalMainUni = Join-Path $Work "mainuni.xml"
$SshOptions = @(
  "-o", "ConnectTimeout=10",
  "-o", "ConnectionAttempts=1",
  "-o", "ServerAliveInterval=10",
  "-o", "ServerAliveCountMax=2",
  "-o", "StrictHostKeyChecking=accept-new",
  "-o", "UserKnownHostsFile=$KnownHostsSsh"
)

New-Item -ItemType Directory -Force -Path $Work | Out-Null
Copy-Item -LiteralPath (Join-Path $ClientPath $LauncherRelative) -Destination $LocalLauncher
Copy-Item -LiteralPath (Join-Path $ClientPath $PatcherRelative) -Destination $LocalPatcher
Copy-Item -LiteralPath (Join-Path $ClientPath "patcher\skin\mainuni.xml") -Destination $LocalMainUni

& (Join-Path $SetupRoot "set-launcher-links.ps1") -MainUniPath $LocalMainUni `
  -NewsUrl $NewsUrl -RegisterUrl $RegisterUrl -HomeUrl $HomeUrl `
  -SupportUrl $SupportUrl -ForumUrl $ForumUrl -NoBackup

try {
  Write-Host "Uploading executable copies..." -ForegroundColor Cyan
  & scp.exe @SshOptions -P $Port $LocalLauncher "$User@$Server`:$RemoteLauncher"
  if ($LASTEXITCODE -ne 0) { throw "Launcher upload failed." }
  & scp.exe @SshOptions -P $Port $LocalPatcher "$User@$Server`:$RemotePatcher"
  if ($LASTEXITCODE -ne 0) { throw "Patcher upload failed." }

  $remoteCommand = "set -e; sudo /opt/asp-cpw/cpw x '$RemoteLauncher'; sudo /opt/asp-cpw/cpw x '$RemotePatcher'; sudo chown '$User':'$User' '$RemoteLauncher' '$RemotePatcher'"
  Write-Host "Applying the active server RSA key..." -ForegroundColor Cyan
  Write-Host "Enter the Ubuntu password when SSH or sudo asks for it. Input is hidden."
  & ssh.exe @SshOptions -t -p $Port "$User@$Server" $remoteCommand
  if ($LASTEXITCODE -ne 0) { throw "Server-side RSA patching failed." }

  & scp.exe @SshOptions -P $Port "$User@$Server`:$RemoteLauncher" $LocalLauncher
  if ($LASTEXITCODE -ne 0) { throw "Unable to download the patched Launcher." }
  & scp.exe @SshOptions -P $Port "$User@$Server`:$RemotePatcher" $LocalPatcher
  if ($LASTEXITCODE -ne 0) { throw "Unable to download the patched patcher." }

  $launcherPem = Get-EmbeddedPem $LocalLauncher
  $patcherPem = Get-EmbeddedPem $LocalPatcher
  if ($launcherPem -cne $patcherPem) { throw "Launcher and patcher received different RSA keys." }
  if (-not (Test-ServerSignature $LocalPatcher ($PatchUrl + "element/files.md5"))) {
    throw "Patched executables do not verify the active ASP CPW manifest. Original client was not changed."
  }

  $BackupRoot = Join-Path $SetupRoot ("backups\" + (Get-Date -Format "yyyyMMdd-HHmmss"))
  New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
  $backupFiles = @(
    "launcher\Launcher.exe", "launcher\Launcher-core.exe",
    "patcher\patcher.exe", "patcher\patcher-core.exe", "patcher\skin\mainuni.xml", "patcher\server\pid.ini",
    "patcher\server\updateserver.txt", "patcher\server\serverlist.txt",
    "config\element\version.sw", "config\element\Listver.sw", "config\element\FullList.sw",
    "config\patcher\version.sw", "config\patcher\newver.sw"
  )
  foreach ($relative in $backupFiles) {
    $source = Join-Path $ClientPath $relative
    if (Test-Path -LiteralPath $source -PathType Leaf) {
      $target = Join-Path $BackupRoot $relative
      New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
      Copy-Item -LiteralPath $source -Destination $target
    }
  }

  Copy-Item -LiteralPath $LocalLauncher -Destination (Join-Path $ClientPath $LauncherRelative) -Force
  Copy-Item -LiteralPath $LocalPatcher -Destination (Join-Path $ClientPath $PatcherRelative) -Force
  Copy-Item -LiteralPath $LocalMainUni -Destination (Join-Path $ClientPath "patcher\skin\mainuni.xml") -Force
  [IO.File]::WriteAllText((Join-Path $ClientPath "patcher\server\pid.ini"), "[Version]`r`npid=101`r`n", [Text.Encoding]::ASCII)
  # The stock PW launcher expects these two lists as UTF-16 LE with a BOM.
  # ASCII/UTF-8 content looks correct in a text editor but produces empty lists.
  [IO.File]::WriteAllText((Join-Path $ClientPath "patcher\server\updateserver.txt"), "`"ASP-Patch`"`t`t`"$PatchUrl`"`r`n", [Text.Encoding]::Unicode)
  [IO.File]::WriteAllText((Join-Path $ClientPath "patcher\server\serverlist.txt"), "ASP Server`r`nPWKU`t${GamePort}:$GameAddress`t1`r`n", [Text.Encoding]::Unicode)
  [IO.File]::WriteAllText((Join-Path $ClientPath "config\element\version.sw"), "1 0`r`n", [Text.Encoding]::ASCII)
  [IO.File]::WriteAllText((Join-Path $ClientPath "config\patcher\version.sw"), "1`r`n", [Text.Encoding]::ASCII)
  New-Item -ItemType Directory -Force -Path (Join-Path $ClientPath "config\launcher") | Out-Null
  [IO.File]::WriteAllText((Join-Path $ClientPath "config\launcher\version.sw"), "1`r`n", [Text.Encoding]::ASCII)
  foreach ($relative in @("config\element\Listver.sw", "config\element\FullList.sw", "config\patcher\newver.sw")) {
    $path = Join-Path $ClientPath $relative
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
  }
  Write-Host "Backup: $BackupRoot" -ForegroundColor Green
  Write-Host "RSA signature, update URL, launcher links, PID, game server, and baseline versions verified." -ForegroundColor Green
} finally {
  Remove-Item -LiteralPath $Work -Recurse -Force -ErrorAction SilentlyContinue
}

exit 0
