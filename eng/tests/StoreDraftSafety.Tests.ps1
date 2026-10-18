# Native refusals use only an already completed fictitious draft run and never enter financial startup.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$RunId, [switch]$MainIdentityOnly)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $root
$output = Join-Path $root ('artifacts/store-drafts/' + $RunId)
$completed = Get-Content -LiteralPath (Join-Path $output 'complete.json') -Raw | ConvertFrom-Json
if (!$completed.FinancialRowsUnchangedAfterSeeding -or $completed.Captures.Count -ne 48) { throw 'Completed fictitious run required.' }
$isolationPath = Join-Path $output 'isolation.json'
$isolationBefore = (Get-FileHash -LiteralPath $isolationPath -Algorithm SHA256).Hash
$exe = Join-Path $root 'src/Apps/Zanance/Vafadar.Zanance.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Vafadar.Zanance.App.exe'
Add-Type -Path (Join-Path $PSScriptRoot '../modules/StoreDraftProcess.cs')
$variables = @('VAFADAR_SNAPSHOT_ONLY','VAFADAR_SNAPSHOTS','VAFADAR_STORE_DRAFT_RUN','VAFADAR_START_ROUTE')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$results = @()
$inputBefore = [Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName()
try {
    $cases = if ($MainIdentityOnly) { @('main-identity') } else { @('identity','output','desktop','storage') }
    foreach ($case in $cases) {
        $selectedRun = if ($case -eq 'identity') { [Guid]::NewGuid().ToString('N') } else { $RunId }
        $selectedRoot = Join-Path $root ('artifacts/store-drafts/' + $selectedRun)
        New-Item -ItemType Directory -Path $selectedRoot -Force | Out-Null
        $env:VAFADAR_SNAPSHOT_ONLY = 'store-draft'
        $env:VAFADAR_STORE_DRAFT_RUN = $selectedRun
        $env:VAFADAR_SNAPSHOTS = if ($case -eq 'output') { Join-Path $root 'artifacts/store-draft-outside-d338' } else { $selectedRoot }
        $env:VAFADAR_START_ROUTE = ''
        $desktopRun = if ($case -eq 'desktop') { [Guid]::NewGuid().ToString('N') } else { $selectedRun }
        $lease = [Vafadar.Build.StoreDraftProcess]::Start($exe, $root, $desktopRun)
        try {
            $child = $lease.Process
            $proofPath = Join-Path $selectedRoot ('refusal-' + $child.Id + '.json')
            $exited = $child.WaitForExit(30000)
            if (!(Test-Path -LiteralPath $proofPath)) { throw "No authoritative refusal; retain original PID $($child.Id)." }
            $proof = Get-Content -LiteralPath $proofPath -Raw | ConvertFrom-Json
            $expected = if ($case -eq 'main-identity') { 'identity' } else { $case }
            if ($proof.ProcessId -ne $child.Id -or $proof.Phase -ne $expected -or !$proof.BeforeMauiBuilder) {
                throw 'Refusal does not identify the original pre-startup guard.'
            }
            # A verified fatal refusal may leave only a native crash dialog on this non-input desktop.
            # Clean up that exact failed child; never kill a process merely because observation expired.
            $cleaned = $false
            if (!$exited) { $child.Kill(); $child.WaitForExit(); $cleaned = $true }
            if ($child.ExitCode -eq 0) { throw 'Refused native process unexpectedly completed successfully.' }
            $results += [ordered]@{ Case=$case; ProcessId=$child.Id; Phase=$proof.Phase; BeforeMauiBuilder=$true; ExitCode=$child.ExitCode; FailedChildCleanup=$cleaned; Desktop=$lease.DesktopName }
        }
        finally { $lease.Dispose() }
    }
    if ((Get-FileHash -LiteralPath $isolationPath -Algorithm SHA256).Hash -ne $isolationBefore) { throw 'Original isolation receipt changed.' }
    $report = if ($MainIdentityOnly) { 'guard-main-identity.json' } else { 'guard-tests.json' }
    $destination = Join-Path $output $report
    if (Test-Path -LiteralPath $destination) { throw 'Preserve prior guard evidence.' }
    [ordered]@{ Cases=$results; OriginalIsolationReceiptUnchanged=$true; InputDesktopBefore=$inputBefore; InputDesktopAfter=[Vafadar.Build.StoreDraftProcess]::ReadInputDesktopName() } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $destination -Encoding UTF8
    "PASS: $($results.Count) original native store-draft refusals before financial startup."
}
finally { foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') } }
