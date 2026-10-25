<#
.SYNOPSIS Observes full current Home publication boundaries on completed independent protected workloads.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$RunId,
    [Parameter(Mandatory)][string]$BuildRoot,
    [Parameter(Mandatory)][string]$WorkloadRoot,
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
$stageOutput = Join-Path $output 'home-workload-stages'
$WorkloadRoot = [IO.Path]::GetFullPath($WorkloadRoot)
if (!$WorkloadRoot.StartsWith((Join-Path $root 'artifacts') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    !(Test-Path -LiteralPath (Join-Path $WorkloadRoot 'fictitious-performance.marker'))) { throw 'Existing owned protected workload required.' }
$processReceipt = Join-Path $output 'home-workload-stages-process.json'
if ((Test-Path -LiteralPath $stageOutput) -or (Test-Path -LiteralPath $processReceipt)) {
    throw 'Preserve the original Home stage process and observations; never relaunch after observation expiry.'
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
    'VAFADAR_STORE_DRAFT_REPAIR', 'VAFADAR_SNAPSHOT_FONT_SCALE', 'VAFADAR_WINDOW_SIZE', 'VAFADAR_START_ROUTE',
    'VAFADAR_HOME_PERFORMANCE_ROOT', 'VAFADAR_HOME_PERFORMANCE_LANGUAGES')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $env:VAFADAR_SNAPSHOT_ONLY = 'store-draft'
    $env:VAFADAR_SNAPSHOTS = $output
    $env:VAFADAR_STORE_DRAFT_RUN = $RunId
    $env:VAFADAR_STORE_DRAFT_REPAIR = 'home-workload-stages'
    $env:VAFADAR_HOME_PERFORMANCE_ROOT = $WorkloadRoot
    $env:VAFADAR_HOME_PERFORMANCE_LANGUAGES = 'en'
    $env:VAFADAR_SNAPSHOT_FONT_SCALE = ''
    $env:VAFADAR_WINDOW_SIZE = '412x800'
    $env:VAFADAR_START_ROUTE = ''
    Add-Type -Path (Join-Path $PSScriptRoot '../modules/StoreDraftProcess.cs')
    $inputBefore = [Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName()
    $lease = [Vafadar.Build.StoreDraftProcess]::Start($executables[0].FullName, $root, $RunId)
    $child = $lease.Process
    @{ ProcessId = $child.Id; Desktop = $lease.DesktopName; SourceRun = $RunId; Purpose = 'full-current-Home-timeline' } |
        ConvertTo-Json | Set-Content -LiteralPath $processReceipt -Encoding UTF8
    if (!$child.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Observation expired; preserve the original Home stage PID $($child.Id) and source."
    }
    if ($child.ExitCode -ne 0 -or (Test-Path -LiteralPath (Join-Path $stageOutput 'error.txt'))) {
        throw 'Home stage review failed; preserve the original process outcome and partial observations.'
    }
    $proof = Get-Content -LiteralPath (Join-Path $stageOutput 'home-workload-proof.json') -Raw | ConvertFrom-Json
    $installation = Get-Content -LiteralPath (Join-Path $stageOutput 'installation-preservation.json') -Raw | ConvertFrom-Json
    if ($proof.ProcessId -ne $child.Id -or !$proof.ObservationTimeline -or !$proof.RetainedSourceFilesUnchanged -or
        !$proof.CompletePortableSchemaAndTypedRowsUnchanged -or !$proof.NoFinancialSave -or $proof.Results.Count -ne 2 -or
        $installation.ProcessId -ne $child.Id -or $installation.Package -ne $identity -or
        !$installation.OriginalInputsUnchanged -or !$installation.NoSeedOrFinancialCommands) {
        throw 'Actual retained-source terminal receipt differs.'
    }
    $images = @(Get-ChildItem -LiteralPath $stageOutput -File -Filter '*.png')
    $timelines = @(Get-ChildItem -LiteralPath $stageOutput -File -Filter '*-timeline.json')
    if ($images.Count -ne 2 -or $timelines.Count -ne 4) { throw 'Four new native timelines and two own-window images required.' }
    foreach ($file in $timelines) {
        $observed = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        if (!$observed.IncludesConstructors -or !$observed.ExistingCompleteFinancialControlsMatched -or
            $observed.SourceMode -ne 'Advanced' -or $observed.Timeline[-1].Boundary -ne 'native-layout-complete') {
            throw 'Complete actual Home boundaries required.'
        }
    }
    foreach ($name in $originalReceipts.Keys) {
        if ((Get-FileHash -LiteralPath (Join-Path $output $name) -Algorithm SHA256).Hash -ne $originalReceipts[$name]) {
            throw 'An original normal-size source receipt changed.'
        }
    }
    $inputAfter = [Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName()
    if ($inputAfter -ne $inputBefore) { throw 'Original owner input desktop changed.' }
    @{ InputDesktopBefore = $inputBefore; InputDesktopAfter = $inputAfter; OriginalReceiptsUnchanged = $true;
        ProcessId = $child.Id; TimelineCount = $timelines.Count; OwnWindowImages = $images.Count; NoSeedOrFinancialCommands = $true } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stageOutput 'desktop-isolation.json') -Encoding UTF8
    'PASS: four current native Home timelines; original complete source and financial controls preserved.'
}
finally {
    if ($lease) { $lease.Dispose() }
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
