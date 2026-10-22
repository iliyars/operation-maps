<#
.SYNOPSIS
  Builds the OperationMaps package for users: a self-contained folder (no
  .NET install needed) zipped into artifacts\OperationMaps-<version>.zip.

.EXAMPLE
  # Everyone works with the shared catalog in a network folder
  # (see docs\shared-catalog.md). User copies never change its schema.
  .\publish.ps1 -SharedCatalog "\\server\share\OperationMaps\catalog.db"

.EXAMPLE
  # Stand-alone package with your current catalog inside.
  .\publish.ps1 -Catalog "src\OperationMaps.Wpf\bin\Debug\net10.0-windows\catalog.db"

.EXAMPLE
  # The admin's own copy of a shared-catalog setup (migrates + seeds).
  .\publish.ps1 -SharedCatalog "\\server\share\OperationMaps\catalog.db" -Admin
#>
[CmdletBinding()]
param(
  # Shown in "Справка → О программе". Defaults to today's date.
  [string] $Version = (Get-Date -Format 'yyyy.M.d'),

  # UNC/local path of the shared catalog.db written into appsettings.json.
  [string] $SharedCatalog,

  # With -SharedCatalog: build the admin's copy (UpdateSchemaOnStartup = true).
  [switch] $Admin,

  # Without -SharedCatalog: a catalog.db to ship inside the package.
  [string] $Catalog
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if ($SharedCatalog -and $Catalog) {
  throw 'Use either -SharedCatalog (everyone uses one file on the server) or -Catalog (a copy inside the package), not both.'
}
if ($Catalog -and -not (Test-Path $Catalog)) {
  throw "Catalog file not found: $Catalog"
}

$name = if ($Admin) { "OperationMaps-$Version-admin" } else { "OperationMaps-$Version" }
$out = Join-Path 'artifacts' $name
$zip = "$out.zip"

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

Write-Host "Publishing $name ..." -ForegroundColor Cyan
dotnet publish src\OperationMaps.Wpf `
  -c Release -r win-x64 --self-contained true `
  -p:Version=$Version `
  -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# ── Catalog settings ─────────────────────────────────────────────────────────
$settingsPath = Join-Path $out 'appsettings.json'
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json

if ($SharedCatalog) {
  $settings.ConnectionStrings.Catalog = "Data Source=$SharedCatalog"
  # Only ONE copy may change the shared file's schema — see docs\shared-catalog.md.
  $settings.Catalog.UpdateSchemaOnStartup = [bool]$Admin
}
elseif ($Catalog) {
  Copy-Item $Catalog (Join-Path $out 'catalog.db')
}

$settings | ConvertTo-Json -Depth 5 | Set-Content $settingsPath -Encoding utf8

# ── Zip ──────────────────────────────────────────────────────────────────────
Compress-Archive -Path "$out\*" -DestinationPath $zip
$sizeMb = [math]::Round((Get-Item $zip).Length / 1MB, 1)

Write-Host ''
Write-Host "Done: $zip ($sizeMb MB)" -ForegroundColor Green
if ($SharedCatalog) {
  $mode = if ($Admin) { 'admin (updates the schema)' } else { 'user (never changes the schema)' }
  Write-Host "Catalog: $SharedCatalog — $mode"
}
elseif ($Catalog) {
  Write-Host "Catalog: copy of $Catalog inside the package"
}
else {
  Write-Host 'Catalog: none — a new one is created and seeded on first start.'
}
Write-Host 'Unzip to a folder the user can write to (not Program Files) and run OperationMaps.Wpf.exe.'
