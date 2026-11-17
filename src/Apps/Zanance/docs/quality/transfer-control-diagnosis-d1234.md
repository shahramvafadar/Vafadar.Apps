# Transfer-control diagnosis and independent Android D2D evidence (D-1234)

## What was wrong with the earlier control route

The owner correctly reported that the S24 Ultra's Main Zanance installation is older. Retained D1233 public APK hashes confirm different Main binaries: A17 `5699adea9718e5f6a3cac3701e6e05667ceba4de6500a95f672835f5f32cfad6`, S24 `f2c1835f349a01856f36bc9b4d8f08828fb26ffaf50887d948cb995d64a06643`. Both display versionCode1/versionName0.1.0, so display labels alone cannot establish freshness. Neither Main was upgraded for this follow-up.

However, Main was not the source or reader of the D1233 synthetic payloads. The two independent packages had separate package/provider/storage identities, no shared UID declaration and no reference to a Main/app project. Their exact signed APK hashes matched before and after Smart Switch. The S24's new helper providers successfully returned all nine known fields and the compiled fixture identity. The older Main therefore does not explain unsupported fields or missing data in these independent helpers. Its private state was not queried to reach this conclusion.

The earlier route did not establish private app-data transfer capability. Samsung explicitly lists private-storage app data as content Smart Switch cannot back up; its supported-content list also varies by connected device. The observed arrival of two APKs with no synthetic payloads is consistent with that documented limitation. The earlier Custom/Apps selection was unsuitable as an assumed positive control for private data. See [Samsung supported content and exclusions](https://www.samsung.com/us/support/answer/ANS10001412/).

This identifies the practical limitation of the selected workflow, **not** the exact internal Samsung branch that ran. D1233 did not record an actual backup-agent callback/transport flag. Its control excluded cloud data and included D2D data, so the saved outcome also cannot distinguish OEM omission of private payloads from use of a different extraction path. No unsupported-format, Android17 regression or exclusive implementation cause is asserted.

## Necessary independent route

A new finite, predeclared framework comparison was run only on a fresh owned API36/x64 emulator, actual4096byte pages. No phone command, Main operation, Smart Switch transfer or successful financial/test replay was issued. No new account was used.

Android documents backup-manager testing separately from OEM UI transfer. The official local transport provides an `is_device_transfer` setting which sets the actual device-transfer bit. That bit was observed by the original full-backup agent callback, not inferred solely from a requested setting. See [Android test guide](https://developer.android.com/identity/data/testingbackup), [AOSP local transport parameters](https://raw.githubusercontent.com/aosp-mirror/platform_frameworks_base/android16-release/packages/LocalTransport/src/com/android/localtransport/LocalTransportParameters.java) and [transport implementation](https://raw.githubusercontent.com/aosp-mirror/platform_frameworks_base/android16-release/packages/LocalTransport/src/com/android/localtransport/LocalTransport.java).

Two new offline/non-debuggable/x64 SDK-only probes contain17AOT/27ELF/98static16KiBLOAD each, with v3/CRC/alignment verified. The observer agent delegates `OnFullBackup` to Android's base implementation and records only transport flags in excluded no-backup storage. FullBackupOnly is true. The positive control includes the nine D2D domains; the policy probe links the unchanged production XML files, with the original D1233 SHA256 values verified again.

Both private probes deliberately use allowBackuptrue to isolate the **XML** rule behavior. This is a disclosed difference from shipping allowBackupfalse, not a production policy change or a replacement for prior D603 eligibility evidence. The original helpers/retired cohorts were not reinstalled. No shipping or whole-OEM identity is claimed.

## Actual first-attempt results

| Observation | Actual result |
| --- | --- |
|New control UID10217, initial fixture|All nine exact synthetic payloads, prepared once|
|Control full backup|Success; actual transport flags `[2,2]` for framework preflight/full callbacks|
|Control data cleared before restore|All nine missing; no source-preparation marker|
|Control framework restore token1|restoreFinished0; all nine expected payloads restored|
|Control no-backup state after restore|Preparation marker absent and observer flags absent, as required|
|New policy UID10216, initial fixture|All nine exact synthetic payloads, prepared once|
|Policy preflight callback|Actual device-transfer flags `[2]`|
|Policy package backup result|Native `-1002` / package rejected; overall batch success is not treated as package success|
|Policy restore|Not issued after the package rejection; no successful operation was replayed|

The positive transfer control is now genuinely established for all nine domains on the standard Android mechanism. This also demonstrates that the fixture, storage-domain readers and known payloads can survive a real framework backup/restore when the transport supports them.

The first runner incorrectly required package Success even for a policy that excludes every data domain, so it stopped on the preserved package rejection. AOSP LocalTransport's backup-size check rejects a nonpositive measured size with TRANSPORT_PACKAGE_REJECTED (`-1002`). The policy's original XML and actual D2D callback are consistent with that empty-data branch. **The size was not directly recorded**, and the source-based explanation is labeled as inference. It is not a fabricated successful candidate restore or proof of Samsung's earlier internal behavior. Initial broad log selection also matched unrelated system uses of words such as size/empty; those lines are not causal evidence.

No second backup, repeated control, rebuilt rescue variant, candidate restore, fresh Samsung transfer or weakening of the production XML was used to turn the first result green. All original outputs and the initial runner refusal remain in the local lineage.

## Preservation and retirement

This follow-up issued no commands to either real phone. Their existing Main installations and data were neither read privately nor changed; D1233's before/after public preservation receipts remain the applicable actual observations. No fresh Main liveness/private-byte equality is asserted.

On the emulator, the original backup-disabled state, selected transport, settings and absent-key presence, locale/geometry/boot/reverse were restored exactly. Only the two fresh probe UIDs were removed. The host was normally stopped and actual handles disappeared. The SDK's first delete batch masked a missing System32/findstr/JDK preflight with exit0 while removing no files. That refusal was preserved; correcting PATH without skipping the JDK check allowed official deletion. Actual AVD and INI absence was then verified:25files/10,551,637,460logical bytes removed. Two standard cleans removed280files/57,359,841logical bytes. Used APKs, source, DLL/PDB, fixed results and receipts remain local.

Removal of the final fresh SDK-user residue was rejected **before process execution** with blocked by policy. The remaining root is `D:\_Projects\Vafadar.Apps\artifacts\d1234-framework-d2d-owned-avd`, six files/7,732logical bytes from the preceding actual inventory. It is frozen against retry/subset/alternate/move/escalation/reuse. No private key content was read. Shared ADB remains the original instance; earlier frozen scopes were not touched.

## What is and is not complete

The old-Main hypothesis for these independent helper results is resolved, the documented Smart Switch control-route limitation is identified, and the missing **positive framework transfer control** has been established without reusing the phones. The actual D1233 OEM trial remains terminal and inconclusive for rule-enforced exclusion; D1234 does not silently close the broader OEM/private-source/iOS/release gate or claim a complete candidate restore.

Production XML explicitly directs new-device financial recovery to a Zanance portable backup rather than automatic device-key/storage migration. An actual financial-data migration into Main on the personal S24 would be a separate operation with version/schema/backup preservation requirements. It was not substituted for this exclusion review, and upgrading the older Main was unnecessary for interpreting the independent probes.

Evidence root: local `artifacts/d1234-transfer-control-diagnosis`. See [the unchanged OEM result](samsung-oem-transfer-result-d1233.md), [original preparation](prepared-samsung-oem-transfer-d1233.md) and [prior framework eligibility](android-live-backup-eligibility.md). Canonical parent states, retained crash cohorts, one-shot interpretations and financial checkpoints remain unchanged.

## Subsequent remaining interpretation and S24 delivery

[D1235](empty-policy-and-current-s24-d1235.md) correlates the original empty-extraction rejection with bounded public transport bytecode without repeating a backup or restore, and adopts the current complete APK once on S24 without reset. The earlier Main-version facts above describe D1233/D1234; the S24 is current after D1235. This does not relabel the original OEM result or invent a successful candidate restore.
