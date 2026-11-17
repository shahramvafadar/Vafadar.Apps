# Empty-extraction classification and current S24 adoption (D-1235)

## Completed work and boundary

The unexplained D1234 package-rejected result has been classified against the public Android transport implementation from the retained official emulator image. The current complete signed Zanance APK was also installed once over the owner's older S24 Main, without reset or uninstall, and reached selected Home. These complete the remaining error interpretation and the specifically authorized S24 update. Actual D1233 Smart Switch private-data transfer remains an unchanged OEM-specific observation; this report does not retroactively turn it into general OEM acceptance.

The rules under review exclude data from backup/automatic device transfer. This work does not delete the owner's financial data or implement financial-data migration through Smart Switch.

## Resolve the package-rejected response without repeating a backup

D1234 already proved an actual nine-domain positive restore control. Its policy probe held all nine expected payloads, entered the original Android full-backup callback with actual transport flags `[2]`, and returned package error `-1002`. The first runner had incorrectly required package Success even for an extraction policy that excludes every data domain. No candidate restore followed that rejected, empty dataset. The global batch Success was never substituted for per-package acceptance.

To resolve the missing classification, a fresh owned API36/x64 emulator was used **only to read the public system LocalTransport APK**. No probe/Main was installed, no source data was prepared, and no backup or restore was requested. The public APK is33,182bytes with SHA256 `4cb25b32a3f4da4dfebda27552fac4b41eacd502cdf1095a480acb79ab21eb33`. Only five named full-backup transport methods were statically decoded.

| Audited compiled branch | Result |
| --- | --- |
|`checkFullBackupSize(J)I`, size at most zero|`-0x3ea` = `-1002` package rejected|
|Same method, positive size within25MiB|`0`|
|Same method, size above25MiB|`-0x3ed` = `-1005` quota exceeded|
|Other audited full-backup I/O/connection error branches|`-1000`; no `-1002` constant|

The additional methods were `performFullBackup`, `sendBackupData`, `finishBackup` and `tearDownFullBackup`. Their bounded output separates the empty-size classification from I/O/socket/quota failures. The [framework preflight source](https://raw.githubusercontent.com/aosp-mirror/platform_frameworks_base/android16-release/services/backup/java/com/android/server/backup/fullbackup/PerformFullTransportBackupTask.java) calls the size check only after receiving a nonnegative measurement. Combined with the inspected size branch, zero is the expected rejected size in this context. See also the [public transport implementation](https://raw.githubusercontent.com/aosp-mirror/platform_frameworks_base/android16-release/packages/LocalTransport/src/com/android/localtransport/LocalTransport.java).

This is a **code-correlated deduction, not a newly captured byte counter**. The public APK was acquired from the same retained official image path; package.xml/system.img metadata predates the D1234 launch and no image update was observed. D1234 did not capture its original LocalTransport APK hash, so historical binary-byte identity is not invented. The evidence closes the empty-export interpretation for the audited backend; it is not a fabricated successful candidate restore or an observation of Samsung's earlier internal extraction branch.

Accordingly, requiring a restored nonempty dataset from an all-domain exclusion policy was the wrong expected endpoint. The supported source-side outcome is no eligible exported data, with the local transport rejecting the empty package. The original nine-domain positive control and the original first rejection remain unchanged. No successful control, financial operation, old managed suite, shipping build, one-shot crash interpretation or Smart Switch transfer was repeated.

The first static decode failed because the launcher looked for build-tools under the image SDK. Ordinary SDK environment variables did not override its hardcoded toolsdir. Inspecting the public launcher and setting its documented JVM discovery option selected the existing build-tools SDK; no tool installation, JDK-version bypass or package change was needed. Both failed tool outputs remain in the lineage. The method was not decoded successfully until the corrected invocation.

## Current complete APK adopted on S24

The prepared package remains `artifacts/d1230-camera-activity-lifetime/shipping-publish/pro.vafadar.zanance-Signed.apk`, SHA256 `5699adea9718e5f6a3cac3701e6e05667ceba4de6500a95f672835f5f32cfad6`. It is the existing complete dual-ABI offline/non-debuggable/v2v3 package with384AOT/408ELF/1608staticLOAD admission, reused without rebuilding or repeating successful product tests. Reviewed incoming changes were confined to Dastan and documents/solution declarations; Zanance/shared product build inputs have not changed since the verified D1230 work.

The older S24 public APK `f2c1835f349a01856f36bc9b4d8f08828fb26ffaf50887d948cb995d64a06643` was retained before installation. Its public certificate matched the prepared current APK. One authorized `install -r --user0` completed successfully, with no uninstall, clear, downgrade or duplicate install. Public UID11035/CE1552612/DE1552123, existing recorded grants, locale/geometry/boot/reverse and all captured OS fields remained equal; only the intended Main APK hash changed.

The owner's first unlock had relocked before preparation. That guard refused before any snapshot, setting write or installation. Public package/signature preparation then continued without authentication or UI input; no lock was bypassed. The owner had explicitly authorized the update. Later visible verification observed the device actually unlocked before app input.

One normal launcher start reached current Main PID7380/UID11035 with154owned native nodes, selected Home and no editable control. Native hierarchy was streamed through the terminal and reduced to bounded presence/counts in memory; no raw XML file or financial text was stored. No credential, financial Save or navigation test sequence was entered. The temporary D517 timeout900000 was restored to its original120000 and the final public snapshot again matched except for the intended APK change. No command was issued to A17 by this update.

Both old and current packages still display versionCode1/versionName0.1.0. The installed SHA256 and current-source admission, rather than that unchanged display label, establish that S24 now has the current prepared binary. No private Main DB/key or complete private-row equality is claimed. Retaining the prior public executable does not promise that a downgrade could safely reverse any application migration.

## Retirement and remaining scope

The public-reader emulator's settings/key presence, locale, geometry, boot and reverse remained exact. It was normally stopped; original owned host handles disappeared. Official SDK deletion verified actual AVD and INI absence:27files/4,092,016,501logical bytes removed. Six scoped SDK-user files/7,732bytes remain. No supplementary deletion or earlier frozen scope was attempted; in particular the separately policy-rejected D1234 residue was neither retried nor reused. Shared ADB remained the original instance.

Local `artifacts/d1235-exclusion-and-s24-adoption` retains the public transport APK, five decoded method outputs, metadata/provenance limits, failed SDK discovery outputs, error-classification receipt, S24 signer/public snapshots, one-install/one-launch results, streamed-UI summary and restoration/retirement receipts. The exact current complete APK remains available at its original prepared path.

The available standard-framework empty-extraction interpretation and S24 delivery work are complete. The [actual OEM result](samsung-oem-transfer-result-d1233.md) still lacks a positive private-data transfer control, and [D1234](transfer-control-diagnosis-d1234.md) retains its original incomplete candidate-restore lineage. No entire OEM/cloud/iOS/security/release gate or canonical parent state is silently closed. Normal Zanance financial-data recovery remains the separately designed portable-backup workflow; it was not executed against either personal Main here.
