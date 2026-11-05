param(
    [ValidateSet('Debug', 'Release', 'All')]
    [string] $Configuration = 'All'
)

$ErrorActionPreference = 'Stop'
$workspacePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$binPath = [IO.Path]::GetFullPath((Join-Path $workspacePath 'src/OperationMaps.Wpf/bin'))

if (Get-Process -Name 'OperationMaps.Wpf' -ErrorAction SilentlyContinue) {
    throw 'Close OperationMaps before resetting its databases.'
}

$configurations = if ($Configuration -eq 'All') { @('Debug', 'Release') } else { @($Configuration) }
$targets = @(
    foreach ($buildConfiguration in $configurations) {
        $folder = Join-Path $binPath "$buildConfiguration/net10.0-windows"
        foreach ($databaseName in @('catalog.db', 'operationmaps.db')) {
            $databasePath = Join-Path $folder $databaseName
            foreach ($suffix in @('', '-wal', '-shm', '-journal')) {
                $candidatePath = [IO.Path]::GetFullPath($databasePath + $suffix)
                if (-not $candidatePath.StartsWith($binPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Database path is outside the intended bin directory: $candidatePath"
                }
                if (Test-Path -LiteralPath $candidatePath -PathType Leaf) { $candidatePath }
            }
        }
    }
)

if ($targets.Count -eq 0) {
    Write-Output 'No local database files found. Nothing to reset.'
    return
}

# Check every file before moving any of them. An open SQLite database must not be reset.
foreach ($target in $targets) {
    $handle = [IO.File]::Open($target, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $handle.Dispose()
}

$backupPath = Join-Path $workspacePath ('artifacts/database-reset-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6))
foreach ($target in $targets) {
    $relativePath = $target.Substring($binPath.Length + 1)
    $destinationPath = Join-Path $backupPath $relativePath
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destinationPath)) -Force | Out-Null
    Move-Item -LiteralPath $target -Destination $destinationPath
    Write-Output "Backed up and reset: $relativePath"
}

Write-Output "Backup directory: $backupPath"
Write-Output 'Start the application to recreate the schema and default forms. Catalog records and administrator accounts will be empty.'
