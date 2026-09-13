param(
  [Parameter(Mandatory=$true)][string]$MainUniPath,
  [Parameter(Mandatory=$true)][string]$NewsUrl,
  [Parameter(Mandatory=$true)][string]$RegisterUrl,
  [Parameter(Mandatory=$true)][string]$HomeUrl,
  [Parameter(Mandatory=$true)][string]$SupportUrl,
  [Parameter(Mandatory=$true)][string]$ForumUrl,
  [switch]$NoBackup
)

$ErrorActionPreference = "Stop"

function Assert-WebUrl([string]$Name, [string]$Value) {
  $uri = $null
  if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$uri) -or
      ($uri.Scheme -ne "http" -and $uri.Scheme -ne "https")) {
    throw "$Name must be an absolute http:// or https:// URL."
  }
}

function Escape-XmlAttribute([string]$Value) {
  return $Value.Replace("&", "&amp;").Replace('"', "&quot;").Replace("<", "&lt;").Replace(">", "&gt;")
}

function Replace-LauncherUrl([string]$Text, [string]$Pattern, [string]$Value, [string]$Name) {
  $regex = [regex]::new($Pattern, [Text.RegularExpressions.RegexOptions]::Singleline)
  $matches = $regex.Matches($Text)
  if ($matches.Count -ne 1) { throw "Unable to locate exactly one $Name URL in mainuni.xml." }
  $escaped = Escape-XmlAttribute $Value
  return $regex.Replace($Text, { param($match) $match.Groups[1].Value + $escaped + $match.Groups[2].Value }, 1)
}

foreach ($entry in @{
  "News URL"=$NewsUrl; "Register URL"=$RegisterUrl; "Website URL"=$HomeUrl;
  "Support URL"=$SupportUrl; "Forum URL"=$ForumUrl
}.GetEnumerator()) { Assert-WebUrl $entry.Key $entry.Value }

$MainUniPath = [IO.Path]::GetFullPath($MainUniPath)
if (-not (Test-Path -LiteralPath $MainUniPath -PathType Leaf)) { throw "mainuni.xml was not found: $MainUniPath" }
$bytes = [IO.File]::ReadAllBytes($MainUniPath)
if ($bytes.Length -lt 2 -or $bytes[0] -ne 0xFF -or $bytes[1] -ne 0xFE) {
  throw "mainuni.xml must use UTF-16 LE with a BOM. The file was not changed."
}

$text = [IO.File]::ReadAllText($MainUniPath, [Text.Encoding]::Unicode)
function Set-Button([string]$Source, [string]$Button, [string]$Url) {
  $pattern = '(?s)(<SkinButton\s+Name="' + [regex]::Escape($Button) + '"(?:(?!</SkinButton>).)*?<Command[^>]*Name="BrowserLink"[^>]*URL=")[^"]*(")'
  return Replace-LauncherUrl $Source $pattern $Url "$Button button"
}

$text = Set-Button $text "Regist" $RegisterUrl
$text = Set-Button $text "HomePage" $HomeUrl
$text = Set-Button $text "Service" $SupportUrl
$text = Set-Button $text "BBS" $ForumUrl
$text = Replace-LauncherUrl $text '(?s)(<SkinBrowser\s+Name="UpdateBrowser"[^>]*InitURL=")[^"]*(")' $NewsUrl "news panel"

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $PSScriptRoot ("backups\launcher-links\" + $stamp)
$backup = Join-Path $backupRoot "mainuni.xml"
$temp = "$MainUniPath.asp-new"
if (-not $NoBackup) {
  New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
  Copy-Item -LiteralPath $MainUniPath -Destination $backup
}
try {
  [IO.File]::WriteAllText($temp, $text, [Text.Encoding]::Unicode)
  Move-Item -LiteralPath $temp -Destination $MainUniPath -Force
} catch {
  if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force }
  throw
}

Write-Host "Launcher links updated successfully." -ForegroundColor Green
if (-not $NoBackup) { Write-Host "Backup: $backup" }
