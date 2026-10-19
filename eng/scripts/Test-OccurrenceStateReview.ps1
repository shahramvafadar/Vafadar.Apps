<#
.SYNOPSIS Verifies the retained original occurrence-conflict receipt without launching or replaying commands.
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output = [IO.Path]::GetFullPath($OutputDirectory)
$parent = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts/store-drafts'))
$run = Split-Path -Leaf $output
if ((Split-Path -Parent $output) -ne $parent -or $run -notmatch '^[0-9a-f]{32}$') { throw 'Original owned occurrence output required.' }
$process = Get-Content -LiteralPath (Join-Path $output 'process.json') -Raw | ConvertFrom-Json
$receipt = Get-Content -LiteralPath (Join-Path $output 'complete.json') -Raw | ConvertFrom-Json
$identity = 'pro.vafadar.zanance.storepreview.' + $run.Substring(0, 12)
if ($process.Run -ne $run -or $process.Package -ne $identity -or $receipt.Package -ne $identity -or
    $process.ProcessId -ne $receipt.ProcessId -or !$receipt.OriginalBaseStoreUnchanged -or
    !$receipt.OnlySeparateFictitiousLedgerChanged -or $receipt.Captures.Count -ne 6 -or $receipt.Cases.Count -ne 4) {
    throw 'Original occurrence completion does not match its isolated process.'
}
if ((@($receipt.Cases.Action | Sort-Object) -join ',') -ne 'change,link,skip,unskip') { throw 'All original correction paths are required.' }
foreach ($case in $receipt.Cases) {
    $refusal = Get-Content -LiteralPath (Join-Path $output ($case.Action + '-refused.json')) -Raw | ConvertFrom-Json
    if (!$case.AcceptedRowsUnchanged -or !$case.OriginalDraftRetained -or $case.FailureDialogs -ne 1 -or
        $refusal.Action -ne $case.Action -or $refusal.ProcessId -ne $process.ProcessId -or
        !$refusal.AcceptedRowsUnchanged -or !$refusal.OriginalDraftRetained -or !$refusal.BusyThroughNativeFailure) {
        throw 'Original accepted rows, retained draft and native gate proof are required.'
    }
}
$errorPath = Join-Path $output 'error.txt'
$expectedCount = 0
if (Test-Path -LiteralPath $errorPath) {
    $marker = 'Shown as a failure: System.InvalidOperationException: The occurrence was settled or skipped; reload it before correcting it.'
    foreach ($line in Get-Content -LiteralPath $errorPath) {
        if ($line -ceq $marker) { $expectedCount++; continue }
        if ($line -match '^\s*$|^   at Vafadar\.Zanance\.(Data\.PlanStore\.|App\.Features\.Plans\.OccurrenceCommands\.)|^--- End of stack trace from previous location ---$') { continue }
        throw 'An unexpected original failure must remain rejected.'
    }
    if ($expectedCount -ne 4) { throw 'Exactly four deliberate original conflict refusals are required.' }
}
foreach ($capture in $receipt.Captures) {
    if ($capture.Language -notmatch '^(en|fa|de)-(Light|Dark)-412$' -or $capture.File -ne 'saved-message.png') { throw 'Unexpected native message context.' }
    foreach ($image in @((Join-Path (Join-Path $output $capture.Language) $capture.File),
        (Join-Path $output ($capture.Language + '-saved-message-popup1.png')))) {
        if (!(Test-Path -LiteralPath $image) -or (Get-Item -LiteralPath $image).Length -lt 1024) { throw 'Original root and actual popup layers are required.' }
    }
}
[ordered]@{ Run=$run; ProcessId=$process.ProcessId; OriginalCases=4; OriginalCaptionViews=6;
    PreservedExpectedFailureRecords=$expectedCount; OriginalRowsAndDraftsUnchanged=$true; NoCommandReplayed=$true } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'qualified-result.json') -Encoding UTF8
'Original occurrence-state receipts qualified without launching or invoking a financial command.'
