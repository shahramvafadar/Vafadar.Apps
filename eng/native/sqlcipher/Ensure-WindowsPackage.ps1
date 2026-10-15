<#
.SYNOPSIS Restores only the source-built Windows package matching reviewed entry contents.
.DESCRIPTION Reuses exact qualified native bytes; fresh hosts build once. Never downloads a native binary substitute.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'application-windows')
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$contents = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows.package.contents.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($contents.schemaVersion -ne 1 -or $contents.packageId -ne 'Vafadar.Native.SqlCipher.Windows') { throw 'Unsupported native package contents.' }
$feed = Join-Path (Get-NativeInputRoot) 'feed'
New-Item -ItemType Directory -Force $feed | Out-Null
$package = Join-Path $feed ($contents.packageId + '.' + $contents.version + '.nupkg')
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Test-PackageContents([string]$path) {
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $expectedNames = @($contents.entries.PSObject.Properties.Name | Sort-Object)
        $actualNames = @($archive.Entries.FullName | Sort-Object)
        if (($actualNames -join '|') -cne ($expectedNames -join '|')) { throw 'Native package contains missing, duplicate or unreviewed entries.' }
        foreach ($entry in $archive.Entries) {
            $stream = $entry.Open(); $hash = [Security.Cryptography.SHA256]::Create()
            try { $actual = ([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
            finally { $hash.Dispose(); $stream.Dispose() }
            if ($actual -ne $contents.entries.($entry.FullName)) { throw ('Native package entry differs from reviewed contents: {0}; expected={1}; actual={2}' -f $entry.FullName, $contents.entries.($entry.FullName), $actual) }
        }
    } finally { $archive.Dispose() }
}
if (Test-Path -LiteralPath $package) { Test-PackageContents $package; Write-Output $package; return }
$lockPath = Join-Path $feed 'windows-package.restore.lock'
$held = $null
$timer = [Diagnostics.Stopwatch]::StartNew()
while (-not $held) {
    try { $held = [IO.FileStream]::new($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
    catch [IO.IOException] {
        if ($timer.Elapsed.TotalMinutes -gt 20) { throw 'Another native dependency restore still owns the feed; preserve its active work.' }
        Start-Sleep -Milliseconds 250
    }
}
try {
    if (Test-Path -LiteralPath $package) { Test-PackageContents $package; Write-Output $package; return }
    $root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
    if (-not (Test-Path -LiteralPath (Join-Path $root 'candidate/manifest.json'))) {
        if (Test-Path -LiteralPath $root) { throw 'Incomplete native output is retained; inspect its original process/log or select a fresh run.' }
        & (Join-Path $PSScriptRoot 'Build-Windows.ps1') -RunName $RunName | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Source-built Windows dependency failed.' }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'compatibility-proof.json'))) {
        & (Join-Path $PSScriptRoot 'Test-Windows.ps1') -RunName $RunName | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Original Windows native compatibility failed.' }
    }
    $packed = & (Join-Path $PSScriptRoot 'Pack-Windows.ps1') -RunName $RunName -PackageName ('application-' + [Guid]::NewGuid().ToString('N'))
    Test-PackageContents $packed
    $staged = $package + '.' + [Guid]::NewGuid().ToString('N') + '.creating'
    Copy-Item -LiteralPath $packed -Destination $staged
    Test-PackageContents $staged
    [IO.File]::Move($staged, $package, $false)
    Test-PackageContents $package
    Write-Output $package
} finally { $held.Dispose() }
