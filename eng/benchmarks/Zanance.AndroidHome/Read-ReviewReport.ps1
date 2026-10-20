param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$Run,
    [Parameter(Mandatory)][ValidateSet('prepare', 'prepare-unreviewed', 'home-review-count', 'home-review-count-fixed', 'home-add-header', 'home-add-header-fixed', 'getting-started', 'measure', 'compare', 'guard', 'filters', 'schema', 'billing-owned', 'tables', 'legends', 'budget-titles', 'home-legends', 'plan-rows', 'ledger-rows', 'font-scale', 'ledger', 'read-profile', 'ledger-read', 'plan-publication', 'plan-publication-fixed', 'home-stages', 'reimbursements', 'reimbursement-page', 'reimbursement-visibility', 'reimbursement-page-fixed', 'home-template-reload', 'settings-text', 'settings-text-page', 'editor-text', 'retained-aggregate', 'csv-input', 'csv-input-empty', 'csv-quotation', 'ecb-reference', 'ecb-reference-report', 'home-refresh', 'home-refresh-references', 'holding-type', 'holding-archive', 'holding-history', 'holding-price', 'holding-quantity-input', 'holding-fee-action', 'holding-fee-input', 'holding-refund-review', 'holding-financial-review', 'split-financial-review', 'import-financial-review', 'occurrence-refresh', 'occurrence-state', 'occurrence-commit', 'settlement-commit', 'settlement-commit-fixed', 'settlement-commit-observed', 'root-retirement', 'settings-read', 'narrow-surfaces', 'destination-fee', 'destination-fee-fixed', 'destination-fee-native', 'destination-fee-lifecycle', 'destination-fee-owned', 'destination-fee-resume', 'destination-fee-inspect', 'destination-fee-prepared', 'destination-fee-migrations', 'destination-fee-effect', 'destination-fee-unicode')][string]$Phase,
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$ProcessId,
    [Parameter(Mandatory)][string]$OutputPath,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$Comparison,
    [ValidateRange(1, 60)][int]$ObserveSeconds = 45,
    [ValidateRange(0, 2147483647)][int]$DeviceTransportId = 0,
    [switch]$PhysicalDevice
)
$ErrorActionPreference = 'Stop'
if ($Phase -in @('holding-type', 'holding-archive', 'home-review-count-fixed', 'home-add-header', 'home-add-header-fixed', 'getting-started','home-review-count','measure','compare','guard','filters','schema','billing-owned','tables','legends', 'budget-titles','home-legends', 'plan-rows', 'ledger-rows', 'font-scale','ledger','read-profile','ledger-read','plan-publication', 'plan-publication-fixed', 'home-stages', 'reimbursements', 'reimbursement-page', 'reimbursement-visibility', 'reimbursement-page-fixed', 'home-template-reload', 'settings-text', 'settings-text-page', 'editor-text', 'retained-aggregate', 'csv-input', 'csv-input-empty', 'csv-quotation', 'ecb-reference', 'ecb-reference-report', 'home-refresh', 'home-refresh-references', 'holding-quantity-input', 'holding-fee-action', 'holding-fee-input', 'holding-refund-review', 'holding-financial-review', 'split-financial-review', 'import-financial-review', 'occurrence-refresh', 'occurrence-state', 'occurrence-commit', 'settlement-commit', 'settlement-commit-fixed', 'settlement-commit-observed', 'root-retirement', 'settings-read', 'narrow-surfaces', 'destination-fee', 'destination-fee-fixed', 'destination-fee-native', 'destination-fee-lifecycle', 'destination-fee-owned', 'destination-fee-resume', 'destination-fee-inspect', 'destination-fee-prepared', 'destination-fee-migrations', 'destination-fee-effect', 'destination-fee-unicode') -and [string]::IsNullOrEmpty($Comparison)) { throw 'The original comparison identity is required.' }
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
$deadline = [DateTime]::UtcNow.AddSeconds($ObserveSeconds)
do {
    $actual = (& $adb @deviceArguments shell pidof pro.vafadar.zanance.androidhomereview) -join ''
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
