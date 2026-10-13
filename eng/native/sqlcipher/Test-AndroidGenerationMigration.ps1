<# .SYNOPSIS Exercises the original complete migration with independent real-model profiles, native keys and process interruption. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64',
    [ValidatePattern('^[a-zA-Z0-9.:-]+$')][string]$DeviceSerial = '127.0.0.1:5571',
    [ValidateRange(1,65535)][int]$AdbPort = 5038, [switch]$Factory, [switch]$Restore)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'NativeBuildInputs.psm1') -Force
$root = Join-Path (Get-NativeInputRoot) ('build/' + $RunName)
$candidate = Join-Path $root 'candidate'
$manifest = Get-Content -LiteralPath (Join-Path $candidate 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($file in $manifest.files) {
    if ((Get-FileHash -LiteralPath (Join-Path $candidate ($file.abi + '/libsqlcipher.so')) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) {
        throw 'Complete migration input differs from reviewed native slice.'
    }
}
$projectRoot = Join-Path $PSScriptRoot 'Compatibility.Android'
& dotnet build (Join-Path $projectRoot 'Vafadar.SqlCipher.Compatibility.Android.csproj') -c Release -p:ContinuousIntegrationBuild=true "-p:NativeCandidateDirectory=$candidate" *> (Join-Path $root 'android-migration-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Independent complete migration build failed.' }
$adb = Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk/platform-tools/adb.exe'
$package = 'pro.vafadar.zanance.nativecompatibility'
& $adb -P $AdbPort -s $DeviceSerial install -r (Join-Path $projectRoot 'bin/Release/net10.0-android/pro.vafadar.zanance.nativecompatibility-Signed.apk') | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Independent migration package installation failed.' }
$activity = (& $adb -P $AdbPort -s $DeviceSerial shell cmd package resolve-activity --brief $package | Select-Object -Last 1).Trim()
if ($activity -notmatch '^pro\.vafadar\.zanance\.nativecompatibility/[A-Za-z0-9_.]+$') { throw 'Unexpected independent migration activity.' }
function Invoke-OwnedMigration([string]$Phase, [string]$Identity, [string]$Barrier = '') {
    & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
    if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Owned migration process did not stop.' }
    & $adb -P $AdbPort -s $DeviceSerial shell am start -W -n $activity --es migration_phase $Phase --es migration_run $Identity | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Owned migration launch failed.' }
    $folder = 'files/native-compatibility/migration-proofs/' + $Identity
    $report = $null
    for ($attempt=0; $attempt -lt 100; $attempt++) {
        if ($Barrier -eq 'export') {
            $name = (& $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat ($folder + '/candidate-path.txt') | Out-String).Trim()
            if ($name -notmatch '^profile\.db\.[0-9a-f]{32}\.cipher\.db$') { throw 'Unexpected owned candidate filename.' }
            & $adb -P $AdbPort -s $DeviceSerial shell run-as $package test -e ($folder + '/' + $name)
            if ($LASTEXITCODE -eq 0) { $report = [pscustomobject]@{ ActualCandidateCreationObserved = $true }; break }
        } else {
            $path = if ($Barrier -eq 'selected') { $folder + '/ready-selected.json' } else { 'files/result.json' }
            $value = & $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat $path 2>$null
            if ($LASTEXITCODE -eq 0) { $report = ($value | Out-String) | ConvertFrom-Json; break }
        }
        if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim() -notmatch '^\d+$') { throw 'Owned migration ended without evidence.' }
        if ($Barrier) {
            $failed = & $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat files/result.json 2>$null
            if ($LASTEXITCODE -eq 0 -and (($failed | Out-String) | ConvertFrom-Json).Failed) { throw 'Live migration reported failure before interruption; inspect its owned error.' }
        }
        Start-Sleep -Milliseconds 200
    }
    if (-not $report -or $report.Failed) { throw 'Original migration phase failed or evidence expired; do not restart.' }
    if ($Barrier) {
        & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
        if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Owned migration writer survived termination.' }
    }
    return $report
}
$proofs = @()
$scenarios = if ($Restore) { @('restore-staging') } elseif ($Factory) { @('factory-layer') } else { @('interrupted-export','selected-restart','missing-selected-key') }
foreach ($scenario in $scenarios) {
    $identity = [Guid]::NewGuid().ToString('N')
    if ($scenario -eq 'restore-staging') {
        $restored = Invoke-OwnedMigration 'restore-staging' $identity
        if (-not $restored.EncryptedRestoreStaging -or -not $restored.FirstSchemaTo32Migrations -or -not $restored.OriginalFinancialPacketsPreserved -or -not $restored.OriginalSourceUnchanged -or -not $restored.NoLiveSelection) { throw 'Actual native encrypted restore staging evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Result = $restored }
        continue
    }
    $created = Invoke-OwnedMigration 'migration-create' $identity
    if (-not $created.OriginalModelCreated -or $created.Migrations -ne 32) { throw 'Actual original model fixture missing.' }
    $prepared = Invoke-OwnedMigration 'migration-key-prepare' $identity
    if (-not $prepared.KeyDurablyPrepared) { throw 'Actual native protected key preparation missing.' }
    if ($scenario -eq 'factory-layer') {
        $finished = Invoke-OwnedMigration 'migration-factory' $identity
        if (-not $finished.ProtectedFactoryReadWriteAndRetirement -or -not $finished.ProtectedPortableBackupVerified -or -not $finished.RetiredExportStopped -or -not $finished.LegacyRestoreRefusedBeforeData) { throw 'Actual protected factory evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Result = $finished }
        continue
    } elseif ($scenario -eq 'interrupted-export') {
        $killed = Invoke-OwnedMigration 'migration-export-kill' $identity 'export'
        $retained = Invoke-OwnedMigration 'migration-check-interruption' $identity
        if (-not $retained.InterruptedExportBeforeSelection -or -not $retained.OriginalAndKeyPreserved) { throw 'Actual export interruption did not preserve source/key.' }
    } else {
        $killed = Invoke-OwnedMigration 'migration-selected-kill' $identity 'selected'
        if (-not $killed.ActualSelectedCheckpoint) { throw 'Actual committed selection barrier missing.' }
    }
    $finished = Invoke-OwnedMigration $(if ($scenario -eq 'missing-selected-key') { 'migration-missing-key' } else { 'migration-resume' }) $identity
    if ($scenario -eq 'missing-selected-key') {
        if (-not $finished.MissingSelectedKeyRefused -or -not $finished.NoReplacementOrPlaintextFallback) { throw 'Missing-key recovery refused incorrectly.' }
    } elseif (-not $finished.CompleteMigration -or -not $finished.OriginalModelAndMoneyPreserved -or -not $finished.EncryptedRollbackRetained -or -not $finished.ExactPlaintextCleanup) {
        throw 'Complete original native migration evidence missing.'
    }
    $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Interrupted = $killed; Result = $finished }
}
$proof = Join-Path $root ('android-migration-proof-' + [Guid]::NewGuid().ToString('N') + '.json')
$proofs | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $proof -Encoding UTF8
& $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
Write-Output $proof
