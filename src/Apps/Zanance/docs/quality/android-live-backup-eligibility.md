# Live Android backup eligibility, without a backup request (D-603)

## Previously uncovered assertion

D-263 qualified the packaged allowBackup=false flag and complete legacy/cloud/
device-transfer exclusion XML. Query the real framework's eligibility decision for
the installed original packages; do not repeat package construction or initiate
backup, restore, account setup, transfer, financial setup or a completed UI cohort.
This is a bounded SEC-07 runtime observation, not completion of OEM/iOS acceptance.

The independent shell-only Java observer uses the Android16
[read-only BackupManager interface](https://raw.githubusercontent.com/aosp-mirror/platform_frameworks_base/android16-release/core/java/android/app/backup/IBackupManager.aidl).
Eligibility/status queries are distinct from backup/request/restore/setter methods.
The [framework eligibility rules](https://raw.githubusercontent.com/aosp-mirror/platform_frameworks_base/android16-release/services/backup/java/com/android/server/backup/utils/BackupEligibilityRules.java)
also distinguish backup destinations; this observation does not replace actual
device-transfer exclusion or OEM payload evidence.

## Actual admission and results

Use the already authorized shell UID2000, user0, API36, actual ranchu emulator or
SM-A176B. Bind both fixed owned package names to their actual original UIDs through
PackageManager before resolving BackupManager. Do not guess Binder transaction
numbers, accept arbitrary methods/packages, change hidden-API policy or elevate.
The only backup-service calls are activity/enabled status and package eligibility.

| Device | Service active | Backup enabled | Main UID/result | Independent UID/result | Positive control |
|---|---|---|---|---|---|
| Owned API36/x64 emulator |true|false|10217 /false|10221 /false|Settings provider1000 /true|
| Samsung API36/ARM64 |true|true|10380 /false|10381 /false|Not queried on phone|

The five eligibility replies are actual native results, not manifest inference or
synthetic booleans. The emulator control is eligible even while the backup mechanism
is disabled; keep both status fields rather than treating them as interchangeable.
The phone's existing enabled state remains enabled. No backup/restore payload is
requested, and no global scheduling, transport, account, authentication or permission
is changed. These results do not prove that data was excluded from an OEM/cloud
transfer that never occurred. No full private Main database/key equality is claimed.

A wrong expected Main UID is rejected on the emulator before BackupManager binding:
exit3/IllegalArgumentException/NoEligibilityResultAccepted, with no eligibility
boolean substituted. It neither repeats a successful positive query nor modifies
the package UID. Other exceptions use the same content-free failure channel, not
messages/stacks/credentials. Five replies plus one refusal are not managed tests.

## Construction and preservation

Use existing JDK21 and Android36 SDK/D8. The initial `-source/-target 17` strict
compile refuses the missing module-path warning before any native run. Preserve it;
`--release 17` resolves the toolchain declaration with all warnings still errors.
Corrected compilation and D8 succeed without suppressing diagnostics. The used
JAR is3118bytes; it is not a signed/installable APK and no app installation occurs.
No product code, package version, runtime, schema, SDK dependency or financial rule
changes. No old managed suite or MAUI package build is repeated for this Java-only
diagnostic. Local used class/JAR/source witnesses remain as small bound evidence.

Before/after package APK/UID/CEDE/permission flags/stopped/PIDs match on both devices.
Actual Main is885f6df5 in both; emulator caller isebd9 and physical caller isB1D5.
Main2920 on the booted emulator and physical Main24382 stay the same across their
respective query. Pre-boot emulator PIDs are not claimed equal. Eleven selected
setting values, backup enabled/activated replies, boot ID, size/density and original
shell authority match before/after. No app launch, financial/seed/key command, real
authentication or private shipping Main database/key/account read is requested.

Only the two new remote JARs are hash-matched and removed:6236logical bytes total.
No other download, private data or earlier policy-rejected cleanup scope is touched.
The owned AVD is stopped; its configuration remains
7b98e07e1fce0bacb029e882a0cb763600ec07e84fdf395c8a74f88def3f990d.
Both existing ADB servers and phone context remain available. No timeout, security,
backup setting, transport or root change is needed. No screenshot/audio/SQL/source
value or account token is exported by the observer.

SEC-07 stays open for actual OEM/device-transfer and iOS evidence. Other security,
genuine authentication, performance, stability, accessibility and release gates are
unchanged; TalkBack remains last and Persian speech parked under D-594. The overall
delivery goal stays active. Evidence pointer:`%TEMP%\Zanance-D603-current.txt`.
