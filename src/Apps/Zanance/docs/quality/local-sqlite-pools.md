# Independent local SQLite pools (D-137 / AT-139)

Profile switching and inactive-profile deletion retire only the named file's known pools; restore clears the
exact captured destination connection before migrating that same context. Other SQLite files and active profile
sessions stay independent. Fixture cleanup uses the same production helper only for existing databases under its
unique owned directory, after disposing its providers. Default/factory/read-write/read-only strings are covered;
arbitrary connection-string variants remain their owner's exact-pool responsibility. No process-wide clearing,
collection serialization, schema, permission, backup format, commercial activation or visible layout change.
Nine added AT-139 cases use real TEMP-table session markers and actual profile/backup services. The original
MoveTo/restore behavior lost another database's marker; negative baseline retained. Main 2,210 / App.Tests 348
pass with the normal parallel runner and explicit writer-race tests unchanged (58.337 s; no output runner config).
Strict Windows and Android builds: zero warnings/errors. Complete signed Release APK verified. Normal Release
English/light emulator profile creation/switch, first-run restore from an actual production-service package, return
and inactive-profile deletion succeed. All 24 restored tables are compared: financial rows and other metadata match the actual snapshot; only the existing
onboarding-completion Settings.UpdatedAt audit write differs. Original profile
files/sidecars, complete rows and English/original System theme are restored. Temporary helpers remain ignored.
Release correctly denied the initial inspection-only run-as inventory; the helper was corrected to inspect only an
unlaunched Debug package. One stale prompt-coordinate attempt cancelled before Save; later input used the actual
focused field. Transient hierarchy misses resumed the same created profile without replaying creation/restore.
The observed native document provider was checked before selecting the exact owned file. Negative helper/capture
logs are retained; unsuccessful attempts are not acceptance. One early nested fixture probe used a noncanonical mixed-separator connection string and failed cleanup; it was
corrected to the application's full-path convention before the successful full run. No Windows native, physical
phone, iOS, provider, 200-percent layout or publication acceptance claim. Planned entitlement bindings/automation,
choice persistence/UI and existing external gates remain open.

## APK

D:\_Projects\Vafadar.Apps\artifacts\android\zanance-d137-release.apk

Bytes: 81069995

SHA256: d0a232ddb0aa82ae78e1b7636d6a367fb0cb0cfc6584161d15fd9a6c4b30523b

Package pro.vafadar.zanance, minSdk 24, targetSdk 36; non-debuggable, full arm64-v8a/x86_64 stores and application AOT. ZIP and v2/v3 signatures verified with the existing local test certificate. This is a sideload artifact, not a Store release.

## Native evidence

```json
{
  "EnglishLightNormalSignedRelease": true,
  "ActualProfileCreationSwitchAndFirstRunRestore": true,
  "RestoreFromActualProductionBackupService": true,
  "All24RestoredRowsCompared": true,
  "AllFinancialRowsAndOtherMetadataMatchSnapshot": true,
  "OnlyExistingOnboardingAuditTimestampDiffers": true,
  "ActualReturnToOriginalAndInactiveProfileDeletion": true,
  "OnlyOwnedProfileRemoved": true,
  "OriginalProfileFileSetPreserved": true,
  "ExactOriginalFilesAndSidecarsRestored": true,
  "All24OriginalTablesAfterDisplayRestore": true,
  "EnglishOriginalSystemThemeRestored": true,
  "FinalSignedReleaseInstalledStopped": true,
  "OnlyQemu5570Tcp5571": true,
  "NoPhysicalDeviceOrSystemSettingChange": true
}
```

## Retained evidence

- pool-isolation-baseline.log: original global move/restore cleanup loses the unrelated native session; early empty/nested fixture defect is negative evidence.
- pool-isolation-focused-data-final.log: focused cases after canonical-path correction; the full suite includes the additional captured-destination case.
- pool-isolation-all-tests.log: normal parallel full run, 2,210 passed, zero failed/skipped, actual exit zero.
- pool-isolation-windows-build.log / pool-isolation-android-build.log / pool-isolation-release.log: strict builds and canonical APK.
- pool-isolation-native/final-proof.json and pool-isolation-apk-proof.json: native restoration and artifact checks.
