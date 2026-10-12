<#
.SYNOPSIS Runs a candidate's independent key/integrity probe in a fresh owned directory on Android.
.DESCRIPTION Never opens app storage or changes system settings. Removes only its UUID-scoped fictitious files.
#>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64',
    [ValidatePattern('^[a-zA-Z0-9.:-]+$')][string]$DeviceSerial = '127.0.0.1:5571', [ValidateRange(1,65535)][int]$AdbPort = 5038)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$manifest = Get-Content -LiteralPath (Join-Path $root 'candidate/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$adb = Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk/platform-tools/adb.exe'
$abi = (& $adb -P $AdbPort -s $DeviceSerial shell getprop ro.product.cpu.abi | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $abi -notin @('arm64-v8a','x86_64')) { throw 'Required native Android device ABI unavailable.' }
$file = $manifest.files | Where-Object abi -eq $abi
$library = Join-Path $root ('candidate/' + $abi + '/libsqlcipher.so')
if (-not $file -or $file.sha256 -ne (Get-FileHash -LiteralPath $library).Hash.ToLowerInvariant()) { throw 'Native probe input differs from its manifest.' }
$probe = Join-Path $root ('candidate/' + $abi + '/native-probe')
$owned = '/data/local/tmp/vafadar-sqlcipher-' + [Guid]::NewGuid().ToString('N')
if ($owned -notmatch '^/data/local/tmp/vafadar-sqlcipher-[0-9a-f]{32}$') { throw 'Unsafe probe directory.' }
& $adb -P $AdbPort -s $DeviceSerial shell "test ! -e $owned && mkdir $owned"
if ($LASTEXITCODE -ne 0) { throw 'Independent probe directory is not fresh.' }
try {
    & $adb -P $AdbPort -s $DeviceSerial push $library ($owned + '/libsqlcipher.so') | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Native candidate transfer failed.' }
    & $adb -P $AdbPort -s $DeviceSerial push $probe ($owned + '/native-probe') | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Native probe transfer failed.' }
    $hash = & $adb -P $AdbPort -s $DeviceSerial shell sha256sum ($owned + '/libsqlcipher.so')
    if ($LASTEXITCODE -ne 0 -or $hash -notlike ($file.sha256 + ' *')) { throw 'Transferred native bytes differ.' }
    $log = Join-Path $root ('runtime-' + [Guid]::NewGuid().ToString('N') + '.log')
    & $adb -P $AdbPort -s $DeviceSerial shell "cd $owned && chmod 700 native-probe && LD_LIBRARY_PATH=$owned ./native-probe" *> $log
    if ($LASTEXITCODE -ne 0) { throw ('Independent native runtime failed; retain ' + $log) }
    $proof = Get-Content -LiteralPath $log | Where-Object { $_ -like '{*' } | Select-Object -Last 1 | ConvertFrom-Json
    if (-not $proof.nativeCipherAndProviderMatched -or -not $proof.wrongMissingKeyTamperRefused -or
        -not $proof.correctKeyReopenMinorUnitsCurrencyDate -or -not $proof.foreignKeysEnabled) {
        throw 'Required native observations missing.'
    }
    [ordered]@{ abi=$abi; nativeSha256=$file.sha256; nativeProof=$proof; fictitiousOnly=$true;
        notAppKeyMigrationArm64OrReleaseAcceptance=$true } | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $root 'runtime-proof.json') -Encoding UTF8
}
finally {
    # This absolute path was validated and created fresh above; no app/device-owned directory is removed.
    & $adb -P $AdbPort -s $DeviceSerial shell rm -r $owned
    if ($LASTEXITCODE -ne 0) { throw ('Remove only the owned probe directory after investigation: ' + $owned) }
}
Write-Output (Join-Path $root 'runtime-proof.json')
