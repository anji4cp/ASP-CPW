$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$problems = [Collections.Generic.List[string]]::new()

$forbiddenNames = @(
  "keys.json", "known_hosts", "db.cnf", "publisher-settings.json",
  "Launcher.exe", "patcher.exe", "elementclient.exe"
)
$gameExtensions = @(".data", ".pck", ".sev", ".sw")
$archiveExtensions = @(".zip", ".rar", ".7z", ".gz")

foreach ($file in Get-ChildItem -LiteralPath $Root -File -Recurse -Force) {
  $relative = $file.FullName.Substring($Root.Length + 1).Replace("\", "/")
  if ($relative.StartsWith(".git/")) { continue }

  if ($forbiddenNames -contains $file.Name) {
    $problems.Add("Forbidden private/machine/client file: $relative")
  }
  if ($gameExtensions -contains $file.Extension.ToLowerInvariant()) {
    $problems.Add("Perfect World payload must not be committed: $relative")
  }
  if ($archiveExtensions -contains $file.Extension.ToLowerInvariant()) {
    $problems.Add("Generated/archive file must not be committed: $relative")
  }
  if ($file.Length -gt 50MB) {
    $problems.Add("Unexpected file larger than 50 MB: $relative")
  }
  if ($relative -match '(^|/)(backups|PUBLISHED|releases|staging|work)(/|$)') {
    $problems.Add("Runtime or backup file must not be committed: $relative")
  }
}

if ($problems.Count -gt 0) {
  $problems | Sort-Object -Unique | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
  exit 1
}

Write-Host "No private keys, machine state, game payloads, backups, or oversized files were found." -ForegroundColor Green
Write-Host "Root: $Root"
exit 0
