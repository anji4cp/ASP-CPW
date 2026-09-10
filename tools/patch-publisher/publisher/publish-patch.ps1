param(
  [switch]$PreviewOnly
)

$ErrorActionPreference = "Stop"
$PublisherRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$PatchRoot = Join-Path $PublisherRoot "PATCH-FILES"
$IgnoredName = "PLACE-FILES-HERE.txt"

function Read-Default([string]$Prompt, [string]$Default) {
  $value = Read-Host "$Prompt [$Default]"
  if ([string]::IsNullOrWhiteSpace($value)) { return $Default }
  return $value.Trim()
}

foreach ($channel in @("element", "launcher", "patcher")) {
  $path = Join-Path $PatchRoot $channel
  if (-not (Test-Path -LiteralPath $path -PathType Container)) {
    throw "Required folder is missing: $path"
  }
}

$Files = @(Get-ChildItem -LiteralPath $PatchRoot -File -Recurse -Force |
  Where-Object { $_.Name -ne $IgnoredName })

Write-Host "" 
Write-Host "Files prepared for the patch:" -ForegroundColor Cyan
if ($Files.Count -eq 0) {
  Write-Host "  No patch files found." -ForegroundColor Yellow
  Write-Host "  Read CARA-PAKAI.md and put changed files inside PATCH-FILES."
  exit 2
}

$TotalBytes = 0L
foreach ($file in $Files) {
  $relative = $file.FullName.Substring($PatchRoot.Length + 1).Replace("\", "/")
  if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "Links are not allowed: $relative"
  }
  if ($relative.Split("/") | Where-Object { $_ -eq ".." -or $_.StartsWith(".") }) {
    throw "Hidden or unsafe path is not allowed: $relative"
  }
  $TotalBytes += $file.Length
  Write-Host ("  {0,-10} {1,10:N0} bytes  {2}" -f $relative.Split("/")[0], $file.Length, $relative)
}
Write-Host ""
Write-Host ("Total: {0} file(s), {1:N0} bytes" -f $Files.Count, $TotalBytes) -ForegroundColor Green

if ($PreviewOnly) {
  Write-Host "Preview only. Nothing was uploaded or published."
  exit 0
}

$answer = Read-Host "Type PUBLISH to upload and publish exactly these files"
if ($answer -cne "PUBLISH") {
  Write-Host "Cancelled. Nothing was uploaded." -ForegroundColor Yellow
  exit 3
}

$Server = Read-Default "Ubuntu address" "127.0.0.1"
$PortText = Read-Default "SSH port" "2223"
$User = Read-Default "SSH username (not password)" "pwadmin"
if ($PortText -notmatch '^[0-9]{1,5}$' -or [int]$PortText -lt 1 -or [int]$PortText -gt 65535) {
  throw "Invalid SSH port."
}
$Port = [int]$PortText
if ($User -notmatch '^[a-z_][a-z0-9_-]{0,31}$') {
  throw "Invalid SSH username. Enter the Ubuntu login name, normally pwadmin."
}
if ($Server -notmatch '^[A-Za-z0-9.-]+$') {
  throw "Invalid server address."
}

$UploadId = [Guid]::NewGuid().ToString("N")
$Archive = Join-Path ([IO.Path]::GetTempPath()) "asp-cpw-patch-$UploadId.tar.gz"
$RemoteArchive = "/tmp/asp-cpw-patch-$UploadId.tar.gz"

try {
  Write-Host "Creating upload archive..." -ForegroundColor Cyan
  & tar.exe -czf $Archive -C $PublisherRoot PATCH-FILES publisher/receive-and-publish.sh
  if ($LASTEXITCODE -ne 0) { throw "Unable to create the patch archive." }

  Write-Host "Uploading to $User@$Server`:$Port..." -ForegroundColor Cyan
  & scp.exe -P $Port $Archive "$User@$Server`:$RemoteArchive"
  if ($LASTEXITCODE -ne 0) { throw "Patch upload failed." }

  $RemoteCommand = "set -e; work=`$(mktemp -d /tmp/asp-cpw-publish.XXXXXX); cleanup(){ rm -rf `"`$work`" '$RemoteArchive'; }; trap cleanup EXIT; tar -xzf '$RemoteArchive' -C `"`$work`"; sudo bash `"`$work/publisher/receive-and-publish.sh`" `"`$work/PATCH-FILES`" --yes"
  Write-Host "Publishing on Ubuntu..." -ForegroundColor Cyan
  Write-Host "Enter the Ubuntu password when SSH or sudo asks for it. Input is hidden."
  & ssh.exe -t -p $Port "$User@$Server" $RemoteCommand
  if ($LASTEXITCODE -ne 0) { throw "Ubuntu rejected or failed the publish operation." }
} finally {
  if (Test-Path -LiteralPath $Archive) {
    Remove-Item -LiteralPath $Archive -Force
  }
}

Write-Host "Patch publication completed." -ForegroundColor Green
try {
  $History = Join-Path $PublisherRoot ("PUBLISHED\" + (Get-Date -Format "yyyyMMdd-HHmmss"))
  foreach ($file in $Files) {
    if (-not (Test-Path -LiteralPath $file.FullName)) { continue }
    $relative = $file.FullName.Substring($PatchRoot.Length + 1)
    $target = Join-Path $History $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Move-Item -LiteralPath $file.FullName -Destination $target
  }
  Write-Host "Local source files archived in: $History"
} catch {
  Write-Warning "The patch is already published, but local files could not be archived: $($_.Exception.Message)"
}
exit 0
