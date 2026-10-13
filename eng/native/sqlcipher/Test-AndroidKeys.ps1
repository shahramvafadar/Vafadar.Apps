<# .SYNOPSIS Checks original Android Keystore code and encrypted reopen in separate Release processes using only the independent package. #>
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
        throw 'Android key input differs from reviewed native slice.'
    }
}
$projectRoot = Join-Path $PSScriptRoot 'Compatibility.Android'
& dotnet build (Join-Path $projectRoot 'Vafadar.SqlCipher.Compatibility.Android.csproj') -c Release -p:ContinuousIntegrationBuild=true "-p:NativeCandidateDirectory=$candidate" *> (Join-Path $root 'android-key-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Independent Android key build failed.' }
$apk = Join-Path $projectRoot 'bin/Release/net10.0-android/pro.vafadar.zanance.nativecompatibility-Signed.apk'
$adb = Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk/platform-tools/adb.exe'
$package = 'pro.vafadar.zanance.nativecompatibility'
$proofRun = [Guid]::NewGuid().ToString('N')
& $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
& $adb -P $AdbPort -s $DeviceSerial install -r $apk | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Independent key package installation failed.' }
$activity = (& $adb -P $AdbPort -s $DeviceSerial shell cmd package resolve-activity --brief $package | Select-Object -Last 1).Trim()
if ($activity -notmatch '^pro\.vafadar\.zanance\.nativecompatibility/[A-Za-z0-9_.]+$') { throw 'Unexpected independent key activity.' }
$results = @()
foreach ($phase in @('keys-create','keys-reopen','keys-negative')) {
    & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
    if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Independent key process did not stop.' }
    & $adb -P $AdbPort -s $DeviceSerial shell am start -W -n $activity --es key_phase $phase --es key_run $proofRun | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Independent key phase launch failed.' }
    $report = $null
    for ($attempt=0; $attempt -lt 30; $attempt++) {
        $text = & $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat files/result.json 2>$null
        if ($LASTEXITCODE -eq 0) { $report = ($text | Out-String) | ConvertFrom-Json; break }
        if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim() -notmatch '^\d+$') {
            throw 'Independent key process ended without report.'
        }
        Start-Sleep -Seconds 2
    }
    if (-not $report -or $report.Failed) { throw 'Independent key phase failed; retain scoped type/stack evidence.' }
    if ($phase -eq 'keys-negative') {
        if ($report.AndroidKeyBehaviorCases -ne 15 -or -not $report.NoReplacementKey) { throw 'Native refusal cases missing.' }
    } elseif (-not $report.AndroidKeyProtected -or -not $report.EncryptedNativeDatabaseReopened -or $report.Phase -ne $phase) {
        throw 'Native protected key/cipher reopen evidence missing.'
    }
    $results += $report
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root ('android-key-proof-' + $proofRun + '.json')) -Encoding UTF8
& $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
Write-Output (Join-Path $root ('android-key-proof-' + $proofRun + '.json'))
