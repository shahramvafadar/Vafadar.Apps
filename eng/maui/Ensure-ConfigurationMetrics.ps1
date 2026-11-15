<#
.SYNOPSIS Prepares and validates the pinned Android Core source package without changing installed tools.
#>
[CmdletBinding()]
param(
    [string]$WorkRoot = 'artifacts/maui-core',
    [string]$DotNet = '',
    [string]$AndroidSdk = '',
    [string]$InputSeeds = '',
    [string[]]$NuGetFallback = @())
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$ownedRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $WorkRoot))
$comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
if (-not $ownedRoot.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, $comparison)) {
    throw 'Core preparation must remain inside repository-owned artifacts.'
}
if (-not $DotNet) {
    $localSdk = Join-Path $repoRoot ('artifacts/d1071-local-serviced-sdk/sdk/' + $(if ($IsWindows) { 'dotnet.exe' } else { 'dotnet' }))
    $DotNet = if (Test-Path -LiteralPath $localSdk -PathType Leaf) { $localSdk } else { (Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source }
}
if (-not $AndroidSdk) {
    $AndroidSdk = $env:ANDROID_HOME
    if (-not $AndroidSdk) { $AndroidSdk = $env:ANDROID_SDK_ROOT }
    if (-not $AndroidSdk -and $IsWindows) { $AndroidSdk = Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk' }
}
if (-not $AndroidSdk) { throw 'Supply an existing Android36 SDK; this preparation never installs SDKs or accepts terms.' }
$adbName = if ($IsWindows) { 'adb.exe' } else { 'adb' }
foreach ($relative in @('platforms/android-36/android.jar', ('platform-tools/' + $adbName))) {
    if (-not (Test-Path -LiteralPath (Join-Path $AndroidSdk $relative) -PathType Leaf)) {
        throw ('A complete existing Android36 SDK is required; missing ' + $relative + '. No SDK component is installed implicitly.')
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $AndroidSdk 'build-tools/36.0.0') -PathType Container)) {
    throw 'Existing Android build-tools36.0.0 is required; no SDK component is installed implicitly.'
}
$python = (Get-Command python -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
[IO.Directory]::CreateDirectory($ownedRoot) | Out-Null
$lockPath = Join-Path $ownedRoot 'producer.lock'
$held = $null
$timer = [Diagnostics.Stopwatch]::StartNew()
while (-not $held) {
    try { $held = [IO.FileStream]::new($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch [IO.IOException] {
        if ($timer.Elapsed.TotalSeconds -gt 50) { throw 'Core producer ownership is unavailable; do not override or restart it.' }
        Start-Sleep -Milliseconds 100
    }
}
$previousLocation = Get-Location
try {
    Set-Location -LiteralPath $repoRoot
    $arguments = @((Join-Path $PSScriptRoot 'Prepare-ConfigurationMetrics.py'), '--work-root', $WorkRoot,
        '--dotnet', $DotNet, '--android-sdk', $AndroidSdk)
    if ($InputSeeds) { $arguments += @('--input-seeds', $InputSeeds) }
    foreach ($folder in $NuGetFallback) { $arguments += @('--nuget-fallback', $folder) }
    $preparationStarted = [DateTime]::UtcNow
    & $python @arguments
    if ($LASTEXITCODE -ne 0) {
        # The proof reads only the newly built public framework. Preserve its strict refusal,
        # but expose the bounded current diagnostic instead of hiding the actual reason in CI.
        $proofState = Join-Path $ownedRoot 'source-proof.terminal.json'
        if ((Test-Path -LiteralPath $proofState -PathType Leaf) -and
            (Get-Item -LiteralPath $proofState).LastWriteTimeUtc -ge $preparationStarted) {
            $failedProof = Get-Content -LiteralPath $proofState -Raw | ConvertFrom-Json
            if ($failedProof.ExitCode -ne 0) {
                $proofLog = Get-ChildItem -LiteralPath $ownedRoot -Filter 'source-proof*.log' -File |
                    Where-Object LastWriteTimeUtc -ge $preparationStarted |
                    Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
                if ($proofLog -and $proofLog.Length -le 8192) {
                    Write-Host 'Current public Core source-proof refusal (retained original log):'
                    Get-Content -LiteralPath $proofLog.FullName -Tail 16 | ForEach-Object { Write-Host $_ }
                }
            }
        }
        throw 'Private Core source/package preparation refused; retain the original attempt.'
    }
    $profile = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'configuration-metrics.inputs.json') -Raw | ConvertFrom-Json
    $receipt = Get-Content -LiteralPath (Join-Path $ownedRoot ('package-proof.' + $profile.private_version + '.json')) -Raw | ConvertFrom-Json
    if ($receipt.CoreSHA256 -notmatch '^[A-Fa-f0-9]{64}$' -or $receipt.RecipeSHA256 -notmatch '^[A-Fa-f0-9]{64}$') {
        throw 'Prepared Core digest receipt is invalid.'
    }
    # The consumer independently hashes both compiler and pre-link inputs; a cached original must never win silently.
    $text = @"
<Project><PropertyGroup>
  <VafadarMauiCoreExpectedSHA256>$($receipt.CoreSHA256.ToUpperInvariant())</VafadarMauiCoreExpectedSHA256>
  <VafadarMauiCoreRecipeSHA256>$($receipt.RecipeSHA256)</VafadarMauiCoreRecipeSHA256>
  <VafadarMauiCorePreparedVersion>$($profile.private_version)</VafadarMauiCorePreparedVersion>
</PropertyGroup></Project>
"@
    $selection = Join-Path $ownedRoot ('selection.' + $profile.private_version + '.props')
    if ((Test-Path -LiteralPath $selection) -and [IO.File]::ReadAllText($selection) -ne $text) {
        throw 'Existing private Core selection state differs; no implicit repair is permitted.'
    }
    if (-not (Test-Path -LiteralPath $selection)) {
        [IO.File]::WriteAllText($selection, $text, [Text.UTF8Encoding]::new($false))
    }
} finally {
    Set-Location -LiteralPath $previousLocation
    $held.Dispose()
}
