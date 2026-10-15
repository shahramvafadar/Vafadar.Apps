param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{32}$')][string]$Run,
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$ProcessId,
    [Parameter(Mandatory)][string]$OutputPath,
    [ValidateRange(1, 60)][int]$ObserveSeconds = 45,
    [ValidateSet('pro.vafadar.zanance.androidsyncreview', 'pro.vafadar.zanance.androidsyncgroupreview', 'pro.vafadar.zanance.androidsyncportablereview', 'pro.vafadar.zanance.androidsyncattachmentreview', 'pro.vafadar.zanance.androidsyncnetwork')]
    [string]$Package = 'pro.vafadar.zanance.androidsyncreview'
)
$ErrorActionPreference = 'Stop'
# Observe only the already launched independent process. Never install, restart or regenerate a fixture.
$adb = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$serial = 'emulator-5570'
$avd = & $adb -P 5038 -s $serial emu avd name
if ($LASTEXITCODE -ne 0 -or $avd[0].Trim() -ne 'Zanance_Storage_Review') { throw 'Owned emulator required.' }
$target = [IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $target) { throw 'Fresh public report output required.' }
$deadline = [DateTime]::UtcNow.AddSeconds($ObserveSeconds)
do {
    $actual = (& $adb -P 5038 -s $serial shell pidof $Package) -join ''
    if ($actual.Trim() -ne [string]$ProcessId) { throw 'Original review process is no longer alive; do not restart it.' }
    $lines = & $adb -P 5038 -s $serial logcat -d -v raw "--pid=$ProcessId" -s 'ZananceSyncReview:I' '*:S'
    if ($LASTEXITCODE -ne 0) { throw 'Public report observation failed; preserve the process.' }
    $chunks = @{}; $expected = $null
    foreach ($line in $lines) {
        if ($line -notmatch ('^' + $Run + '\|(\d+)/(\d+)\|([A-Za-z0-9+/=]+)$')) { continue }
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
            $null = $json | ConvertFrom-Json
            $file = [IO.File]::Open($target, 'CreateNew', 'Write', 'Read')
            try { $data = [Text.Encoding]::UTF8.GetBytes($json); $file.Write($data, 0, $data.Length) } finally { $file.Dispose() }
            $json
            return
        } finally { $bytes.Dispose() }
    }
    Start-Sleep -Seconds 2
} while ([DateTime]::UtcNow -lt $deadline)
Write-Output 'No final public report yet. Preserve and observe the same process; this is not a failed run.'
