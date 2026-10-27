<#
.SYNOPSIS Serializes preparation of the pinned project-owned notification source cache.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$cacheRoot = Join-Path $repoRoot 'artifacts/notification-core'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
$lockPath = Join-Path $cacheRoot 'source.prepare.lock'
$held = $null
$timer = [Diagnostics.Stopwatch]::StartNew()
while (-not $held) {
    try {
        $held = [IO.FileStream]::new($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    } catch [IO.IOException] {
        if ($timer.Elapsed.TotalSeconds -gt 90) { throw 'Another source preparation still owns the notification cache; preserve its active work.' }
        Start-Sleep -Milliseconds 100
    }
}
try {
    & python (Join-Path $PSScriptRoot 'Prepare-NotificationCore.py')
    if ($LASTEXITCODE -ne 0) { throw 'Pinned notification source preparation refused its inputs.' }
} finally {
    $held.Dispose()
}
