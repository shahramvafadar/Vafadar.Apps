param(
    [ValidateRange(0, 2147483647)][int]$DeviceTransportId = 0,
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$ProcessId,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$Comparison,
    [Parameter(Mandatory)][string]$OutputPath,
    [ValidateRange(1, 60)][int]$ObserveSeconds = 45,
    [switch]$OwnedEmulator
)
$ErrorActionPreference = 'Stop'
$adb = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
if ($OwnedEmulator) {
    if ($DeviceTransportId -ne 0) { throw 'Owned emulator uses only its exact emulator-5570 serial.' }
    $deviceArguments = @('-P', '5038', '-s', 'emulator-5570')
} else {
    if ($DeviceTransportId -le 0) { throw 'Explicit approved physical transport required.' }
    $deviceArguments = @('-P', '5038', '-t', [string]$DeviceTransportId)
}
$run = if ($OwnedEmulator) { '25b9dab64bf849fcb24e39246d773215' } else { 'd53542fe57d6446ab52b82a757ee7f6e' }
$package = 'pro.vafadar.zanance.androidhomereview'
$target = [IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $target) { throw 'Fresh report output required; preserve existing receipts.' }
$emulated = (& $adb @deviceArguments shell getprop ro.boot.qemu) -join ''
if ($LASTEXITCODE -ne 0) { throw 'Actual runtime admission failed.' }
if ($OwnedEmulator) {
    if ($emulated.Trim() -ne '1') { throw 'Owned emulator requires an actual emulated runtime.' }
    $avd = & $adb @deviceArguments emu avd name
    if ($LASTEXITCODE -ne 0 -or $avd[0].Trim() -ne 'Zanance_Storage_Review') { throw 'Exact retained Zanance emulator required.' }
} else {
    $model = (& $adb @deviceArguments shell getprop ro.product.model) -join ''
    if ($LASTEXITCODE -ne 0 -or $model.Trim() -ne 'SM-A176B' -or $emulated.Trim() -eq '1') { throw 'Physical Samsung required.' }
}
$deadline = [DateTime]::UtcNow.AddSeconds($ObserveSeconds)
do {
    # Observe only an already started original process. A timeout never authorizes a relaunch.
    $actual = (& $adb @deviceArguments shell pidof $package) -join ''
    if ($LASTEXITCODE -ne 0 -or $actual.Trim() -ne [string]$ProcessId) {
        throw 'Original process not confirmed; preserve evidence and inspect authoritative state before further action.'
    }
    $lines = & $adb @deviceArguments logcat -d -v raw "--pid=$ProcessId" -s 'ZananceRetainedReader:I' '*:S'
    if ($LASTEXITCODE -ne 0) { throw 'Observation failed; preserve the original process.' }
    $chunks = @{}; $expected = $null
    foreach ($line in $lines) {
        if ($line -notmatch ('^' + $run + '\|' + $Comparison + '\|(\d+)/(\d+)\|([A-Za-z0-9+/=]+)$')) { continue }
        $index = [int]$Matches[1]; $count = [int]$Matches[2]; $payload = $Matches[3]
        if ($count -lt 1 -or $count -gt 1024 -or $index -ge $count -or
            ($null -ne $expected -and $expected -ne $count)) { throw 'Inconsistent original report chunks.' }
        $expected = $count
        if ($chunks.ContainsKey($index) -and $chunks[$index] -ne $payload) { throw 'Conflicting original report chunk.' }
        $chunks[$index] = $payload
    }
    if ($null -ne $expected -and $chunks.Count -eq $expected) {
        $encoded = -join (0..($expected - 1) | ForEach-Object { $chunks[$_] })
        $json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($encoded))
        $report = $json | ConvertFrom-Json
        if ($report.Comparison -ne $Comparison -or $report.ProcessId -ne $ProcessId) {
            throw 'Original terminal report identity differs.'
        }
        if (!$report.Failed -and ($report.Run -ne $run -or $report.Results.Count -ne 2)) {
            throw 'Original successful report source or shape set is incomplete.'
        }
        # Preserve failures as terminal receipts too. Saving a report does not assert its acceptance.
        $file = [IO.File]::Open($target, 'CreateNew', 'Write', 'Read')
        try { $bytes = [Text.Encoding]::UTF8.GetBytes($json); $file.Write($bytes, 0, $bytes.Length) }
        finally { $file.Dispose() }
        [pscustomobject]@{ Output = $target; Failed = [bool]$report.Failed; ProcessId = $ProcessId; Chunks = $expected }
        return
    }
    Start-Sleep -Seconds 2
} while ([DateTime]::UtcNow -lt $deadline)
Write-Output 'No terminal report yet. Preserve and observe the same process; no restart or source preparation is authorized.'
