param(
  [string]$Server = "127.0.0.1",
  [int]$Port = 2223,
  [string]$User = "pwadmin"
)

$ErrorActionPreference = "Stop"
$Package = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$PackageName = Split-Path -Leaf $Package
if ($PackageName -notmatch '^[A-Za-z0-9._-]+$') {
  throw "The package folder name contains unsupported characters: $PackageName"
}
$Remote = "/tmp/$PackageName"

if ($User -notmatch '^[a-z_][a-z0-9_-]{0,31}$') {
  throw "Invalid SSH username '$User'. Enter the Ubuntu login name (normally: pwadmin), not its password."
}

Write-Host "ASP CPW Manager - Windows installer" -ForegroundColor Cyan
Write-Host "Target: $User@$Server`:$Port"
Write-Host "SSH username: $User" -ForegroundColor Yellow
Write-Host "At each 'password:' prompt, type the Ubuntu password. Typed characters will not be displayed."
Write-Host "You may be asked again when sudo starts."

ssh -p $Port "$User@$Server" "rm -rf '$Remote'"
if ($LASTEXITCODE -ne 0) { throw "Unable to prepare the remote folder." }
scp -P $Port -r "$Package" "$User@$Server`:/tmp/"
if ($LASTEXITCODE -ne 0) { throw "Unable to upload the installer." }
ssh -t -p $Port "$User@$Server" "chmod +x '$Remote/installer/install-asp-cpw.sh' && sudo '$Remote/installer/install-asp-cpw.sh' --yes"
if ($LASTEXITCODE -ne 0) { throw "Ubuntu installation failed. Review the displayed error." }

Write-Host "ASP CPW Manager installed successfully." -ForegroundColor Green
exit 0
