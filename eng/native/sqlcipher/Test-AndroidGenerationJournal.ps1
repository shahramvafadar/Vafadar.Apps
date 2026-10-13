<# .SYNOPSIS Checks original journal recovery after forced termination of the independent Release Android process. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64',
    [ValidatePattern('^[a-zA-Z0-9.:-]+$')][string]$DeviceSerial = '127.0.0.1:5571',
    [ValidateRange(1,65535)][int]$AdbPort = 5038)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$candidate = Join-Path $root 'candidate'
$manifest = Get-Content -LiteralPath (Join-Path $candidate 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($file in $manifest.files) {
    if ((Get-FileHash -LiteralPath (Join-Path $candidate ($file.abi + '/libsqlcipher.so')) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) {
        throw 'Journal input differs from reviewed Android slice.'
    }
}
$projectRoot = Join-Path $PSScriptRoot 'Compatibility.Android'
& dotnet build (Join-Path $projectRoot 'Vafadar.SqlCipher.Compatibility.Android.csproj') -c Release -p:ContinuousIntegrationBuild=true "-p:NativeCandidateDirectory=$candidate" *> (Join-Path $root 'android-journal-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Independent Android journal build failed.' }
$adb = Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk/platform-tools/adb.exe'
$package = 'pro.vafadar.zanance.nativecompatibility'
$proofRun = [Guid]::NewGuid().ToString('N')
& $adb -P $AdbPort -s $DeviceSerial install -r (Join-Path $projectRoot 'bin/Release/net10.0-android/pro.vafadar.zanance.nativecompatibility-Signed.apk') | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Independent journal installation failed.' }
$activity = (& $adb -P $AdbPort -s $DeviceSerial shell cmd package resolve-activity --brief $package | Select-Object -Last 1).Trim()
if ($activity -notmatch '^pro\.vafadar\.zanance\.nativecompatibility/[A-Za-z0-9_.]+$') { throw 'Unexpected independent journal activity.' }
$reports = @()
foreach ($phase in @('journal-create','journal-uncommitted','journal-reopen-rollback','journal-committed','journal-reopen-selected')) {
    & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
    if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Owned journal process did not stop.' }
    & $adb -P $AdbPort -s $DeviceSerial shell am start -W -n $activity --es journal_phase $phase --es journal_run $proofRun | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Independent journal launch failed.' }
    $barrier = $phase -in @('journal-uncommitted','journal-committed')
    $reportPath = if ($barrier) {
        'files/native-compatibility/journal-proofs/' + $proofRun + '/' + $(if ($phase -eq 'journal-uncommitted') { 'ready-uncommitted.json' } else { 'ready-committed.json' })
    } else { 'files/result.json' }
    $report = $null
    for ($attempt=0; $attempt -lt 30; $attempt++) {
        $value = & $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat $reportPath 2>$null
        if ($LASTEXITCODE -eq 0) { $report = ($value | Out-String) | ConvertFrom-Json; break }
        if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim() -notmatch '^\d+$') { throw 'Journal process ended before evidence.' }
        if ($barrier) {
            $failed = & $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat files/result.json 2>$null
            if ($LASTEXITCODE -eq 0 -and (($failed | Out-String) | ConvertFrom-Json).Failed) { throw 'Native journal writer failed; inspect owned error.txt.' }
        }
        Start-Sleep -Seconds 2
    }
    if (-not $report -or $report.Failed) { throw 'Native journal evidence missing or failed; do not restart.' }
    if ($barrier) {
        if ($phase -eq 'journal-uncommitted' -and -not $report.HotJournalFlushed) { throw 'Native hot journal missing.' }
        if ($phase -eq 'journal-committed' -and -not $report.SelectionCommitted) { throw 'Native commit missing.' }
        & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
        if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Native writer remained alive after termination.' }
    } elseif ($phase -eq 'journal-create') {
        if (-not $report.JournalCreated) { throw 'Original journal initialization missing.' }
    } elseif (-not $report.JournalReopened -or $report.CandidateSelected -ne ($phase -eq 'journal-reopen-selected')) {
        throw 'Native restart selected the wrong generation.'
    }
    $reports += $report
}
$reports | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root ('android-journal-proof-' + $proofRun + '.json')) -Encoding UTF8
& $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
Write-Output (Join-Path $root ('android-journal-proof-' + $proofRun + '.json'))
