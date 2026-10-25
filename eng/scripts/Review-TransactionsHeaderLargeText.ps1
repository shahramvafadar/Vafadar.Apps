<#
.SYNOPSIS Checks only double-size Transactions layouts on a completed independent fictitious source.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$RunId,
    [Parameter(Mandatory)][string]$BuildRoot,
    [ValidateRange(30, 600)][int]$TimeoutSeconds = 300
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $root
$output = Join-Path $root ('artifacts/store-drafts/' + $RunId)
$identity = 'pro.vafadar.zanance.storepreview.' + $RunId.Substring(0, 12)
$original = Get-Content -LiteralPath (Join-Path $output 'complete.json') -Raw | ConvertFrom-Json
$isolation = Get-Content -LiteralPath (Join-Path $output 'isolation.json') -Raw | ConvertFrom-Json
if (!$original.FinancialRowsUnchangedAfterSeeding -or $original.Captures.Count -ne 18 -or
    $original.Package -ne $identity -or $isolation.Package -ne $identity -or !$isolation.EmptyBeforeStartup) {
    throw 'Completed original eighteen-context Transactions source required.'
}
if (Get-Process -Id $original.ProcessId -ErrorAction SilentlyContinue) { throw 'Preserve the original live source process.' }
$scaled = Join-Path $output 'transactions-header-large-text'
$processReceipt = Join-Path $output 'transactions-header-large-text-process.json'
if ((Test-Path -LiteralPath $scaled) -or (Test-Path -LiteralPath $processReceipt)) {
    throw 'Preserve the original scaled process and observations; never relaunch after observation expiry.'
}
$executables = @(Get-ChildItem -LiteralPath (Join-Path $BuildRoot 'bin/Vafadar.Zanance.App') -Recurse -Filter Vafadar.Zanance.App.exe)
$manifests = @(Get-ChildItem -LiteralPath (Join-Path $BuildRoot 'obj/Vafadar.Zanance.App') -Recurse -Filter Package.appxmanifest |
    Where-Object { $_.FullName -match '[\\/]resizetizer[\\/]m[\\/]' })
if ($executables.Count -ne 1 -or $manifests.Count -ne 1) { throw 'One isolated executable and original compiled manifest required.' }
[xml]$metadata = Get-Content -LiteralPath $manifests[0].FullName -Raw
if ($metadata.Package.Identity.Name -ne $identity) { throw 'Actual compiled source identity differs.' }
$originalReceipts = @{}
foreach ($name in @('complete.json', 'isolation.json', 'process.json', 'build.log')) {
    $originalReceipts[$name] = (Get-FileHash -LiteralPath (Join-Path $output $name) -Algorithm SHA256).Hash
}
$variables = @('VAFADAR_SNAPSHOT_ONLY', 'VAFADAR_SNAPSHOTS', 'VAFADAR_STORE_DRAFT_RUN',
    'VAFADAR_STORE_DRAFT_REPAIR', 'VAFADAR_SNAPSHOT_FONT_SCALE', 'VAFADAR_WINDOW_SIZE', 'VAFADAR_START_ROUTE')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $env:VAFADAR_SNAPSHOT_ONLY = 'store-draft'
    $env:VAFADAR_SNAPSHOTS = $output
    $env:VAFADAR_STORE_DRAFT_RUN = $RunId
    $env:VAFADAR_STORE_DRAFT_REPAIR = 'transactions-header-large-text'
    $env:VAFADAR_SNAPSHOT_FONT_SCALE = '2'
    $env:VAFADAR_WINDOW_SIZE = '412x800'
    $env:VAFADAR_START_ROUTE = ''
    Add-Type -Path (Join-Path $PSScriptRoot '../modules/StoreDraftProcess.cs')
    $inputBefore = [Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName()
    $lease = [Vafadar.Build.StoreDraftProcess]::Start($executables[0].FullName, $root, $RunId)
    $child = $lease.Process
    @{ ProcessId = $child.Id; Desktop = $lease.DesktopName; SourceRun = $RunId; RequestedScale = 2 } |
        ConvertTo-Json | Set-Content -LiteralPath $processReceipt -Encoding UTF8
    if (!$child.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Observation expired; preserve the original scaled PID $($child.Id) and source."
    }
    if ($child.ExitCode -ne 0 -or (Test-Path -LiteralPath (Join-Path $scaled 'error.txt'))) {
        throw 'Scaled review failed; preserve the original process outcome and partial observations.'
    }
    $proof = Get-Content -LiteralPath (Join-Path $scaled 'complete.json') -Raw | ConvertFrom-Json
    if ($proof.ProcessId -ne $child.Id -or $proof.Package -ne $identity -or $proof.Captures.Count -ne 18 -or
        !$proof.FinancialRowsUnchangedAfterSeeding -or !$proof.RetainedSourceWithoutSeed -or
        $proof.FinancialCommands -ne 0 -or $proof.RequestedFontScale -ne 2) { throw 'Actual scaled terminal source receipt differs.' }
    $images = @(Get-ChildItem -LiteralPath $scaled -Recurse -File -Filter '*.png')
    if ($images.Count -ne 54) { throw 'Exactly fifty-four new scaled own-window images required.' }
    foreach ($name in $originalReceipts.Keys) {
        if ((Get-FileHash -LiteralPath (Join-Path $output $name) -Algorithm SHA256).Hash -ne $originalReceipts[$name]) {
            throw 'An original normal-size source receipt changed.'
        }
    }
    $inputAfter = [Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName()
    if ($inputAfter -ne $inputBefore) { throw 'Original owner input desktop changed.' }
    @{ InputDesktopBefore = $inputBefore; InputDesktopAfter = $inputAfter; OriginalReceiptsUnchanged = $true;
        ProcessId = $child.Id; ScaledImages = $images.Count; NoSeedOrFinancialCommands = $true } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $scaled 'desktop-isolation.json') -Encoding UTF8
    'PASS: eighteen new scaled contexts on retained original data; no financial preparation or commands.'
}
finally {
    if ($lease) { $lease.Dispose() }
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
