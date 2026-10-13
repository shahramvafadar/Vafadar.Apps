<# .SYNOPSIS Exercises the original complete migration with independent real-model profiles, native keys and process interruption. #>
[CmdletBinding()]
param([ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')][string]$RunName = 'android-x64',
    [ValidatePattern('^[a-zA-Z0-9.:-]+$')][string]$DeviceSerial = '127.0.0.1:5571',
    [ValidateRange(1,65535)][int]$AdbPort = 5038, [switch]$Factory, [switch]$Restore, [switch]$RestoreSelection, [switch]$PackageRestore, [switch]$UseExistingBuild, [switch]$ProfileMaintenance, [switch]$IdentityCatalogue, [switch]$InitialBootstrap, [switch]$SettingsPersistence, [switch]$MetadataPublication, [switch]$AllProfiles, [switch]$NewProfile,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$ResumeRestoreSelectionRun = '',
    [ValidatePattern('^[0-9a-f]{32}$')][string]$ResumeFactoryRun = '',
    [ValidatePattern('^[0-9a-f]{32}$')][string]$ResumeBootstrapRun = '')
$ErrorActionPreference = 'Stop'
if ($ResumeRestoreSelectionRun -and -not $RestoreSelection) { throw 'Recorded selection resume requires the restore-selection mode.' }
if ($ResumeFactoryRun -and (-not $Factory -or $ResumeRestoreSelectionRun)) { throw 'Recorded factory continuation requires only the factory mode.' }
if ($ResumeBootstrapRun -and (-not $InitialBootstrap -or $ResumeFactoryRun -or $ResumeRestoreSelectionRun)) { throw 'Recorded bootstrap continuation requires only initial-bootstrap mode.' }
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
$adb = Join-Path ${env:ProgramFiles(x86)} 'Android/android-sdk/platform-tools/adb.exe'
$package = 'pro.vafadar.zanance.nativecompatibility'
if (-not $ResumeRestoreSelectionRun -and -not $ResumeFactoryRun -and -not $ResumeBootstrapRun) {
    if (-not $UseExistingBuild) {
        & dotnet build (Join-Path $projectRoot 'Vafadar.SqlCipher.Compatibility.Android.csproj') -c Release -p:ContinuousIntegrationBuild=true "-p:NativeCandidateDirectory=$candidate" *> (Join-Path $root 'android-migration-build.log')
        if ($LASTEXITCODE -ne 0) { throw 'Independent complete migration build failed.' }
    }
    # Existing builds are explicitly caller-owned; verify both reviewed native slices inside the actual APK.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $apkPath = Join-Path $projectRoot 'bin/Release/net10.0-android/pro.vafadar.zanance.nativecompatibility-Signed.apk'
    $archive = [IO.Compression.ZipFile]::OpenRead($apkPath)
    try {
        foreach ($file in $manifest.files) {
            $entry = $archive.GetEntry('lib/' + $file.abi + '/libsqlcipher.so')
            if (-not $entry) { throw 'Complete APK is missing a reviewed native slice.' }
            $stream = $entry.Open(); $digest = [Security.Cryptography.SHA256]::Create()
            try { $actual = [BitConverter]::ToString($digest.ComputeHash($stream)).Replace('-', '').ToLowerInvariant() }
            finally { $stream.Dispose(); $digest.Dispose() }
            if ($actual -ne $file.sha256) { throw 'Complete APK differs from reviewed native slices.' }
        }
    } finally { $archive.Dispose() }
    & $adb -P $AdbPort -s $DeviceSerial install -r (Join-Path $projectRoot 'bin/Release/net10.0-android/pro.vafadar.zanance.nativecompatibility-Signed.apk') | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Independent migration package installation failed.' }
}
$activity = (& $adb -P $AdbPort -s $DeviceSerial shell cmd package resolve-activity --brief $package | Select-Object -Last 1).Trim()
if ($activity -notmatch '^pro\.vafadar\.zanance\.nativecompatibility/[A-Za-z0-9_.]+$') { throw 'Unexpected independent migration activity.' }
function Invoke-OwnedMigration([string]$Phase, [string]$Identity, [string]$Barrier = '') {
    & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
    if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Owned migration process did not stop.' }
    if ($Barrier -eq 'metadata-staged') {
        & $adb -P $AdbPort -s $DeviceSerial shell run-as $package rm -f ('files/native-compatibility/migration-proofs/' + $Identity + '/metadata-staged.json')
        if ($LASTEXITCODE -ne 0) { throw 'Owned metadata barrier cleanup failed.' }
    }
    & $adb -P $AdbPort -s $DeviceSerial shell am start -W -n $activity --es migration_phase $Phase --es migration_run $Identity | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Owned migration launch failed.' }
    $folder = 'files/native-compatibility/migration-proofs/' + $Identity
    $report = $null
    $maximumAttempts = if ($Barrier -in @('restore-selected','package-partial','identity-staged','metadata-staged','new-profile-key') -or $Phase -in @('profile-maintenance','bootstrap-intent','bootstrap-create','all-profiles-create','all-profiles-reopen','new-profile-create','new-profile-resume','new-profile-reopen')) { 300 } else { 100 }
    for ($attempt=0; $attempt -lt $maximumAttempts; $attempt++) {
        if ($Barrier -eq 'export') {
            $name = (& $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat ($folder + '/candidate-path.txt') | Out-String).Trim()
            if ($name -notmatch '^profile\.db\.[0-9a-f]{32}\.cipher\.db$') { throw 'Unexpected owned candidate filename.' }
            & $adb -P $AdbPort -s $DeviceSerial shell run-as $package test -e ($folder + '/' + $name)
            if ($LASTEXITCODE -eq 0) { $report = [pscustomobject]@{ ActualCandidateCreationObserved = $true }; break }
        } else {
            $path = if ($Barrier -in @('selected','restore-selected','package-partial','identity-staged','metadata-staged','new-profile-key')) { $folder + $(if ($Barrier -eq 'new-profile-key') { '/new-profile-key.json' } elseif ($Barrier -eq 'metadata-staged') { '/metadata-staged.json' } elseif ($Barrier -eq 'identity-staged') { '/identity-staged.json' } elseif ($Barrier -eq 'restore-selected') { '/restore-selected.json' } elseif ($Barrier -eq 'package-partial') { '/package-partial.json' } else { '/ready-selected.json' }) } else { 'files/result.json' }
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
$scenarios = if ($NewProfile) { @('new-profile') } elseif ($AllProfiles) { @('all-profiles') } elseif ($MetadataPublication) { @('metadata-publication') } elseif ($SettingsPersistence) { @('settings-persistence') } elseif ($InitialBootstrap) { @('initial-bootstrap') } elseif ($IdentityCatalogue) { @('identity-catalogue') } elseif ($ProfileMaintenance) { @('profile-maintenance') } elseif ($PackageRestore) { @('package-restore') } elseif ($RestoreSelection) { @('restore-selection') } elseif ($Restore) { @('restore-staging') } elseif ($Factory) { @('factory-layer') } else { @('interrupted-export','selected-restart','missing-selected-key') }
foreach ($scenario in $scenarios) {
    $identity = if ($ResumeBootstrapRun) { $ResumeBootstrapRun } elseif ($ResumeFactoryRun) { $ResumeFactoryRun } elseif ($ResumeRestoreSelectionRun) { $ResumeRestoreSelectionRun } else { [Guid]::NewGuid().ToString('N') }
    if ($scenario -eq 'new-profile') {
        $created = Invoke-OwnedMigration 'new-profile-create' $identity 'new-profile-key'
        if (-not $created.ActualKeyPreparedBeforeSchema) { throw 'Durable new-profile key barrier missing.' }
        $resumed = Invoke-OwnedMigration 'new-profile-resume' $identity
        $finished = Invoke-OwnedMigration 'new-profile-reopen' $identity
        if (-not $resumed.EncryptedNewProfileWithoutPlaintextOriginal -or -not $finished.Actual32Migrations -or -not $finished.ColdSavedDataPreserved) { throw 'New-profile restart evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Created = $created; Resumed = $resumed; Result = $finished }
        continue
    }
    if ($scenario -eq 'all-profiles') {
        $created = Invoke-OwnedMigration 'all-profiles-create' $identity
        if (-not $created.AllCapturedProfilesPrepared -or -not $created.DistinctFinancialPacketsPreserved) { throw 'All-profile creation evidence missing.' }
        $finished = Invoke-OwnedMigration 'all-profiles-reopen' $identity
        if (-not $finished.AllCapturedProfilesPrepared -or -not $finished.DistinctFinancialPacketsPreserved -or -not $finished.NoIntermediatePublication -or -not $finished.StableBindingsAndEncryptedRollback) { throw 'Cold all-profile evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Created = $created; Result = $finished }
        continue
    }
    if ($scenario -eq 'metadata-publication') {
        $journal = Invoke-OwnedMigration 'metadata-journal' $identity 'metadata-staged'
        if (-not $journal.CommittedMetadataStaged) { throw 'Committed journal staging missing.' }
        $history = Invoke-OwnedMigration 'metadata-history' $identity 'metadata-staged'
        if (-not $history.CommittedMetadataStaged) { throw 'Committed history staging missing.' }
        $finished = Invoke-OwnedMigration 'metadata-reopen' $identity
        if (-not $finished.JournalAndHistoryStagingResumed -or -not $finished.CapturedBindingsUnchanged -or -not $finished.InitialIntentStillPending -or -not $finished.NoKeyPreparationOrFinancialData) { throw 'Actual metadata publication restart failed.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Journal = $journal; History = $history; Result = $finished }
        continue
    }
    if ($scenario -eq 'settings-persistence') {
        $created = Invoke-OwnedMigration 'settings-create' $identity
        if (-not $created.OriginalMauiWritesPersisted -or -not $created.CanceledPersistenceRejected) { throw 'Original MAUI persistence evidence missing.' }
        $finished = Invoke-OwnedMigration 'settings-reopen' $identity
        if (-not $finished.OriginalMauiValueRetainedAfterRestart -or -not $finished.RemovalRetainedAfterRestart) { throw 'Original MAUI cold persistence evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Created = $created; Result = $finished }
        continue
    }
    if ($scenario -eq 'initial-bootstrap') {
        $intent = $null
        if ($ResumeBootstrapRun) {
            $pidText = (& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()
            if ($pidText -notmatch '^\d+$') { throw 'Recorded bootstrap process is no longer live.' }
            & $adb -P $AdbPort -s $DeviceSerial shell run-as $package test -f ('files/native-compatibility/migration-proofs/' + $identity + '/profile.db.generation.sqlite')
            if ($LASTEXITCODE -ne 0) { throw 'Recorded bootstrap journal is missing.' }
            $created = (& $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat files/result.json | Out-String) | ConvertFrom-Json
        } else {
            $intent = Invoke-OwnedMigration 'bootstrap-intent' $identity
            if (-not $intent.InitialIntentBeforeKeyWork) { throw 'Initial pending intent evidence missing.' }
            $created = Invoke-OwnedMigration 'bootstrap-create' $identity
        }
        if (-not $created.ActualIdentityMigrationFactoryJoined -or -not $created.SelectedCiphertextAndRollback -or -not $created.RetainedKeyDisposalRefusesAccess) { throw 'Initial protected generation evidence missing.' }
        $finished = Invoke-OwnedMigration 'bootstrap-reopen' $identity
        if (-not $finished.InitialGenerationColdReopened -or -not $finished.SavedWriteAndMoneyCorrect -or -not $finished.NoKeyPreparationOnRestart -or -not $finished.NoOriginalOrPlaintextFallback -or -not $finished.RetainedKeyDisposalRefusesAccess) { throw 'Cold protected generation evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; InitialIntent = $intent; Created = $created; Result = $finished }
        continue
    }
    if ($scenario -eq 'identity-catalogue') {
        $created = Invoke-OwnedMigration 'identity-create' $identity 'identity-staged'
        if (-not $created.IdentityStagedBeforePublication) { throw 'Original identity bootstrap evidence missing.' }
        $finished = Invoke-OwnedMigration 'identity-reopen' $identity
        if (-not $finished.IdentityStableAfterProcessRestart -or -not $finished.NonemptyMainKeyAndActualJournalBinding -or -not $finished.ExistingRegistrationUnchanged -or -not $finished.NewProfileRetainsInstallation -or -not $finished.NoKeyPreparationOrAppActivation) { throw 'Original native identity restart evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Result = $finished }
        continue
    }
    if ($scenario -eq 'profile-maintenance') {
        $finished = Invoke-OwnedMigration 'profile-maintenance' $identity
        if (-not $finished.OriginalProfileFlow -or -not $finished.DrainBeforePathChange -or -not $finished.OwnedMigrationAndSettings -or -not $finished.RefusedAuthenticationRestoresQueuedWork -or -not $finished.NoFinancialPosting -or -not $finished.InactiveDeletionDrainsAndRetainsMain -or -not $finished.ProtectedSwitchAndKeyedRollback) { throw 'Original native profile maintenance evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Result = $finished }
        continue
    }
    if ($scenario -eq 'package-restore') {
        $partial = Invoke-OwnedMigration 'package-restore' $identity 'package-partial'
        if (-not $partial.ActualPartialPreferenceWrite) { throw 'Actual partial platform preference barrier missing.' }
        $finished = Invoke-OwnedMigration 'package-resume' $identity
        if (-not $finished.ActualPackageReceiver -or -not $finished.PartialPlatformPreferencesSurvivedKill -or -not $finished.DurableReplayBeforeProtectedPublication -or -not $finished.FinancialAndDeviceStateCorrect -or -not $finished.EncryptedPreferenceAndDatabaseRollbackRetained -or -not $finished.RestoreReferencesSurviveKillAndSelection) { throw 'Actual package restore restart evidence missing.' }
        $reopened = Invoke-OwnedMigration 'package-applied-reopen' $identity
        if (-not $reopened.AppliedReplaySkippedAfterColdRestart -or -not $reopened.LaterUserPreferencesRetained -or -not $reopened.NoRepeatedPreferencePersistence) { throw 'One-time durable preference replay evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Interrupted = $partial; Result = $finished; AppliedRestart = $reopened }
        continue
    }
    if ($scenario -eq 'restore-selection') {
        if ($ResumeRestoreSelectionRun) {
            $pidText = (& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()
            if ($pidText -notmatch '^\d+$') { throw 'Recorded restore writer is no longer live; do not restart it.' }
            $selected = (& $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat ('files/native-compatibility/migration-proofs/' + $identity + '/restore-selected.json') | Out-String) | ConvertFrom-Json
            if (-not $selected.ActualRestoreSelected) { throw 'Recorded live writer has no actual selection barrier.' }
            & $adb -P $AdbPort -s $DeviceSerial shell am force-stop $package
            if ((& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()) { throw 'Recorded selected writer did not terminate.' }
        } else { $selected = Invoke-OwnedMigration 'restore-select' $identity 'restore-selected' }
        if (-not $selected.ActualRestoreSelected) { throw 'Actual restore selection kill barrier missing.' }
        $finished = Invoke-OwnedMigration 'restore-selected-resume' $identity
        if (-not $finished.RestoreSelectedAfterProcessKill -or -not $finished.RestoredAndPreviousFinancialPacketsCorrect -or -not $finished.EncryptedRollbackAndOriginalRetained -or -not $finished.NoOldConnectionNeededOnSelectedRestart) { throw 'Actual restore selection restart evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Interrupted = $selected; Result = $finished }
        continue
    } elseif ($scenario -eq 'restore-staging') {
        $restored = Invoke-OwnedMigration 'restore-staging' $identity
        if (-not $restored.EncryptedRestoreStaging -or -not $restored.FirstSchemaTo32Migrations -or -not $restored.OriginalFinancialPacketsPreserved -or -not $restored.OriginalSourceUnchanged -or -not $restored.NoLiveSelection) { throw 'Actual native encrypted restore staging evidence missing.' }
        $proofs += [pscustomobject]@{ Scenario = $scenario; Run = $identity; Result = $restored }
        continue
    }
    if ($ResumeFactoryRun) {
        $pidText = (& $adb -P $AdbPort -s $DeviceSerial shell pidof $package | Out-String).Trim()
        if ($pidText -notmatch '^\d+$') { throw 'Recorded original-model process is no longer live.' }
        & $adb -P $AdbPort -s $DeviceSerial shell run-as $package test -f ('files/native-compatibility/migration-proofs/' + $identity + '/migration-context.txt')
        if ($LASTEXITCODE -ne 0) { throw 'Recorded original-model context is missing.' }
        $created = (& $adb -P $AdbPort -s $DeviceSerial shell run-as $package cat files/result.json | Out-String) | ConvertFrom-Json
    } else { $created = Invoke-OwnedMigration 'migration-create' $identity }
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
