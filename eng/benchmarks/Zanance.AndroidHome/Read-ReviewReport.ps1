param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$Run,
    [Parameter(Mandatory)][ValidateSet('transactions-add-header', 'receipt-source-help', 'scanned-receipts', 'receipt-corpus', 'csv-literal-final', 'forecast-return', 'relocked-destinations', 'validation-layout', 'prepare', 'prepare-unreviewed', 'plan-form', 'entry-workflow', 'home-review-count', 'home-review-count-fixed', 'home-add-header', 'home-add-header-fixed', 'getting-started', 'measure', 'compare', 'guard', 'filters', 'schema', 'billing-owned', 'tables', 'legends', 'original-amount-errors', 'budget-scope-captions', 'budget-period-captions', 'snapshot-difference', 'import-group-undo', 'forecast-missing-rate', 'budget-financial-read', 'budget-titles', 'home-legends', 'plan-rows', 'ledger-rows', 'font-scale', 'ledger', 'read-profile', 'ledger-read', 'plan-publication', 'plan-publication-fixed', 'home-stages', 'reimbursements', 'reimbursement-page', 'reimbursement-visibility', 'reimbursement-page-fixed', 'home-template-reload', 'settings-text', 'settings-text-page', 'editor-text', 'retained-aggregate', 'csv-input', 'csv-input-empty', 'csv-quotation', 'ecb-reference', 'ecb-reference-report', 'home-refresh', 'home-refresh-references', 'holding-detail-retired-callback', 'holding-detail', 'holding-overview-captions', 'holding-overview', 'holding-type', 'holding-archive', 'holding-history', 'holding-price', 'holding-quantity-input', 'holding-fee-action', 'holding-fee-input', 'holding-refund-review', 'holding-financial-review', 'split-financial-review', 'import-financial-review', 'occurrence-refresh', 'occurrence-state', 'occurrence-commit', 'settlement-commit', 'settlement-commit-fixed', 'settlement-commit-observed', 'root-retirement', 'settings-read', 'narrow-surfaces', 'destination-fee', 'destination-fee-fixed', 'destination-fee-native', 'destination-fee-lifecycle', 'destination-fee-owned', 'destination-fee-resume', 'destination-fee-inspect', 'destination-fee-prepared', 'destination-fee-migrations', 'destination-fee-effect', 'destination-fee-unicode')][string]$Phase,
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$ProcessId,
    [Parameter(Mandatory)][string]$OutputPath,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$Comparison,
    [ValidateRange(1, 60)][int]$ObserveSeconds = 45,
    [ValidateRange(0, 2147483647)][int]$DeviceTransportId = 0,
    [switch]$PhysicalDevice,
    [switch]$ReceiptSourceReview,
    [switch]$OriginalBudgetReport
)
$ErrorActionPreference = 'Stop'
if ($OriginalBudgetReport -and ($Phase -ne 'budget-financial-read' -or $Comparison)) {
    throw 'Original budget recovery requires its exact phase/run/PID and no invented comparison binding.'
}
if ($Phase -in @('transactions-add-header', 'receipt-source-help', 'scanned-receipts', 'receipt-corpus', 'csv-literal-final', 'forecast-return', 'relocked-destinations', 'validation-layout', 'plan-form', 'entry-workflow', 'holding-detail-retired-callback', 'holding-detail', 'holding-overview-captions', 'holding-overview', 'holding-type', 'holding-archive', 'home-review-count-fixed', 'home-add-header', 'home-add-header-fixed', 'getting-started','home-review-count','measure','compare','guard','filters','schema','billing-owned','tables','legends', 'original-amount-errors', 'budget-scope-captions', 'budget-period-captions', 'snapshot-difference', 'import-group-undo', 'forecast-missing-rate', 'budget-financial-read', 'budget-titles','home-legends', 'plan-rows', 'ledger-rows', 'font-scale','ledger','read-profile','ledger-read','plan-publication', 'plan-publication-fixed', 'home-stages', 'reimbursements', 'reimbursement-page', 'reimbursement-visibility', 'reimbursement-page-fixed', 'home-template-reload', 'settings-text', 'settings-text-page', 'editor-text', 'retained-aggregate', 'csv-input', 'csv-input-empty', 'csv-quotation', 'ecb-reference', 'ecb-reference-report', 'home-refresh', 'home-refresh-references', 'holding-quantity-input', 'holding-fee-action', 'holding-fee-input', 'holding-refund-review', 'holding-financial-review', 'split-financial-review', 'import-financial-review', 'occurrence-refresh', 'occurrence-state', 'occurrence-commit', 'settlement-commit', 'settlement-commit-fixed', 'settlement-commit-observed', 'root-retirement', 'settings-read', 'narrow-surfaces', 'destination-fee', 'destination-fee-fixed', 'destination-fee-native', 'destination-fee-lifecycle', 'destination-fee-owned', 'destination-fee-resume', 'destination-fee-inspect', 'destination-fee-prepared', 'destination-fee-migrations', 'destination-fee-effect', 'destination-fee-unicode') -and !$OriginalBudgetReport -and [string]::IsNullOrEmpty($Comparison)) { throw 'The original comparison identity is required.' }
# Observe only the already launched independent process. Never install, restart or regenerate a fixture.
$adb = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$deviceArguments = @('-P', '5038', '-s', 'emulator-5570')
if ($PhysicalDevice) {
    if ($DeviceTransportId -le 0) { throw 'Explicit approved phone transport required.' }
    $deviceArguments = @('-P', '5038', '-t', [string]$DeviceTransportId)
    $model = (& $adb @deviceArguments shell getprop ro.product.model) -join ''
    if ($LASTEXITCODE -ne 0 -or $model.Trim() -ne 'SM-A176B') { throw 'Approved Samsung model required.' }
    $emulated = (& $adb @deviceArguments shell getprop ro.boot.qemu) -join ''
    if ($LASTEXITCODE -ne 0 -or $emulated.Trim() -eq '1') { throw 'Actual approved phone required.' }
} else {
    if ($DeviceTransportId -ne 0) { throw 'Physical transport requires the explicit phone opt-in.' }
    $avd = & $adb @deviceArguments emu avd name
    if ($LASTEXITCODE -ne 0 -or $avd[0].Trim() -ne 'Zanance_Storage_Review') { throw 'Owned emulator required.' }
}
$target = [IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $target) { throw 'Fresh public report output required.' }
$observedPackage = if ($Phase -eq 'transactions-add-header') { 'pro.vafadar.zanance.transactionsheaderreview' } elseif ($ReceiptSourceReview -and $Phase -eq 'receipt-source-help') { 'pro.vafadar.zanance.receiptsourcereview' } else { 'pro.vafadar.zanance.androidhomereview' }
if ($ReceiptSourceReview -and $Phase -ne 'receipt-source-help') { throw 'Source-help identity requires its exact phase.' }
$deadline = [DateTime]::UtcNow.AddSeconds($ObserveSeconds)
do {
    $actual = (& $adb @deviceArguments shell pidof $observedPackage) -join ''
    if ($actual.Trim() -ne [string]$ProcessId) { throw 'Original process identity was not confirmed in this observation; preserve it and verify authoritative state before any restart.' }
    $lines = & $adb @deviceArguments logcat -d -v raw "--pid=$ProcessId" -s 'ZananceHomeReview:I' '*:S'
    if ($LASTEXITCODE -ne 0) { throw 'Public report observation failed; preserve the process.' }
    $chunks = @{}; $expected = $null
    foreach ($line in $lines) {
        if ($line -notmatch ('^' + $Phase + '\|' + $Run + '\|(\d+)/(\d+)\|([A-Za-z0-9+/=]+)$')) { continue }
        $index = [int]$Matches[1]; $count = [int]$Matches[2]; $payload = $Matches[3]
        if ($count -lt 1 -or $count -gt 1024 -or $index -ge $count -or ($null -ne $expected -and $expected -ne $count)) { throw 'Inconsistent report chunks.' }
        $expected = $count
        if ($chunks.ContainsKey($index) -and $chunks[$index] -ne $payload) { throw 'Conflicting report chunk.' }
        $chunks[$index] = $payload
    }
    if ($null -ne $expected -and $chunks.Count -eq $expected) {
        $bytes = New-Object IO.MemoryStream
        try {
            for ($index = 0; $index -lt $expected; $index++) {
                $part = [Convert]::FromBase64String($chunks[$index]); $bytes.Write($part, 0, $part.Length)
            }
            $json = [Text.Encoding]::UTF8.GetString($bytes.ToArray())
            $report = $json | ConvertFrom-Json
            if ($OriginalBudgetReport) {
                # The first completed budget frame omitted Comparison. Preserve its bytes and require the
                # exact owned PID/run plus all eighteen original source contexts, without replaying the UI.
                if ($report.PSObject.Properties.Name -contains 'Comparison' -or $report.Scope -ne 'budget-financial-read' -or
                    !$report.OriginalBaseUnchanged -or $report.FinancialSaves -ne 0 -or $report.Observations.Count -ne 18) {
                    throw 'The original unbound budget report is incomplete or belongs to another scope.'
                }
                $contexts = @($report.Observations | ForEach-Object {
                    if ($_.Method -notin @('Limits','Envelopes','Flex') -or $_.Language -notin @('en','fa','de') -or
                        $_.Theme -notin @('Light','Dark') -or !$_.OriginalRowsUnchanged -or !$_.OriginalPageRetired -or $_.FinancialSaves -ne 0) {
                        throw 'An original budget context is missing or changed.'
                    }
                    "$($_.Method)|$($_.Language)|$($_.Theme)"
                } | Select-Object -Unique)
                if ($contexts.Count -ne 18) { throw 'The original budget context set is incomplete.' }
            }
            if ($Comparison -and $report.Comparison -ne $Comparison) { throw 'The original comparison differs.' }
            $file = [IO.File]::Open($target, 'CreateNew', 'Write', 'Read')
            try { $data = [Text.Encoding]::UTF8.GetBytes($json); $file.Write($data, 0, $data.Length) } finally { $file.Dispose() }
            $json
            return
        } finally { $bytes.Dispose() }
    }
    Start-Sleep -Seconds 2
} while ([DateTime]::UtcNow -lt $deadline)
Write-Output 'No final public report yet. Preserve and observe the same process; this is not a failed run.'
