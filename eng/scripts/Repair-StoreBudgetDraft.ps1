<#
.SYNOPSIS Recaptures only the rejected English budget frame from a completed independent fictitious run.
#>
[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$RunId)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $root
$output = Join-Path $root ('artifacts/store-drafts/' + $RunId)
$original = Get-Content -LiteralPath (Join-Path $output 'complete.json') -Raw | ConvertFrom-Json
if (!$original.FinancialRowsUnchangedAfterSeeding -or $original.Captures.Count -ne 48 -or
    $original.Package -ne ('pro.vafadar.zanance.storepreview.' + $RunId.Substring(0,12))) { throw 'Completed independent run required.' }
if (Get-Process -Id $original.ProcessId -ErrorAction SilentlyContinue) { throw 'Original process still exists; retain it.' }
if (Test-Path -LiteralPath (Join-Path $output 'repair-process.json')) { throw 'Preserve the original repair process and observations.' }
$variables = @('VAFADAR_SNAPSHOT_ONLY','VAFADAR_SNAPSHOTS','VAFADAR_STORE_DRAFT_RUN','VAFADAR_STORE_DRAFT_REPAIR','VAFADAR_WINDOW_SIZE','VAFADAR_START_ROUTE')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
try {
    $env:VAFADAR_SNAPSHOT_ONLY = 'store-draft'; $env:VAFADAR_SNAPSHOTS = $output
    $env:VAFADAR_STORE_DRAFT_RUN = $RunId; $env:VAFADAR_STORE_DRAFT_REPAIR = 'en-budget'
    $env:VAFADAR_WINDOW_SIZE = '412x800'; $env:VAFADAR_START_ROUTE = ''
    Add-Type -Path (Join-Path $PSScriptRoot '../modules/StoreDraftProcess.cs')
    $inputBefore = [Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName()
    $exe = Join-Path $root 'src/Apps/Zanance/Vafadar.Zanance.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Vafadar.Zanance.App.exe'
    $lease = [Vafadar.Build.StoreDraftProcess]::Start($exe,$root,$RunId)
    $child = $lease.Process
    @{ ProcessId=$child.Id; Desktop=$lease.DesktopName } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'repair-process.json') -Encoding UTF8
    if (!$child.WaitForExit(120000)) { throw "Observation expired; retain original repair PID $($child.Id)." }
    if ($child.ExitCode -ne 0 -or (Test-Path -LiteralPath (Join-Path $output 'error.txt'))) { throw 'Repair failed; preserve original observations.' }
    $proof = Get-Content -LiteralPath (Join-Path $output 'repair-en-budget/complete.json') -Raw | ConvertFrom-Json
    if ($proof.ProcessId -ne $child.Id -or $proof.Package -ne $original.Package -or !$proof.FinancialRowsUnchanged) { throw 'Original repair receipt mismatch.' }
    @{ InputDesktopBefore=$inputBefore; InputDesktopAfter=[Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName() } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $output 'repair-en-budget/desktop-isolation.json') -Encoding UTF8
    'PASS: only English budget recaptured, original fictitious rows retained.'
}
finally {
    if ($lease) { $lease.Dispose() }
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name,$previous[$name],'Process') }
}
