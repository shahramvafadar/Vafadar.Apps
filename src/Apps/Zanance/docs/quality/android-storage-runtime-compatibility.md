# D-521 - Original encrypted storage on minimum-API and16KiB runtimes

## Purpose and actual boundaries

Continue the unqualified storage/migration/recovery part of SEC-02/05/06 after D-519/D-520.
The complete independent Release x86_64 compatibility package compiles the actual Data/Shared
assemblies, compiled model, migrations, native protector/envelope store, migration sessions and
recovery/portable implementations. It never references Main storage or enumerates Main keys.
It is intentionally debuggable solely to read its own public phase receipts through run-as;
it is not a product APK or phone-test handoff. The only declared permission is its own signature
receiver permission. INTERNET is absent. There are103 actual x64 AOT modules.

Both embedded SQLCipher slices match immutable Android package4.19.0-candidate.5. Do not rebuild
or replace a passing native producer: retain arm64 digest48d06bc6 and x64 digest20b009b7. Actual
runtime returns SQLCipher4.19.0 community and OpenSSL3.5.9. Official source/crypto choice and
existing Main adoption remain unchanged.

| Actual owned environment | App UID retained across test-only upgrades | Page size | Final native PID |
| --- | --- | --- | --- |
| Android7 / API24 / x86_64 |10062|4096bytes|10224|
| Android15 / API35 / x86_64 |10207|16384bytes|15791|

The physical phone remains reserved elsewhere and receives no input from this batch. Shared
ADB5038/PID76812 is reused, not restarted. No system settings, clock, account, authentication,
publication or Main installation occurs. Do not infer fresh physical-Main equality from this
absence of input; Main private-key attestation and broader device gates remain separate.

## Whole original storage cohort

Five original cohorts complete on each runtime, with16 final-cohort records per platform32total:

| Cohort | Original behavior verified |
| --- | --- |
| Schema/portable snapshot | First schema through all51 current migrations, actual compiled model, Unicode/IDs/money/dates, enabled foreign keys, no plaintext header/marker, wrong/missing-key and changed-page refusal, complete memory snapshot/header/schema/typed rows and encrypted restore |
| Selected restart | Original current model, one durable key, actual Selected checkpoint and process death, cold completion with original model/money, encrypted rollback retained and exact plaintext cleanup |
| Missing selected key | Same initial original flow, actual selection/process death, missing envelope refusal with no replacement key or plaintext fallback |
| Recovery | Original private vault, device envelope removed in the independent fixture, cold recovery preserving negative opening amount/model/ciphertext; wrong recovery key and damaged private vault refuse without replacement device key |
| Interrupted candidate generation | Adopt exact retained original source bytes without financial seeding, prepare one key, arm on-device observation before Activity launch, kill only the independent package when its candidate appears, prove KeyPrepared/source/envelope preservation and complete original migration on the same source/key |

Every originally needed new-platform fixture is prepared once. These synthetic database rows are
real test writes, not a claim of zero writes inside the probe. No user financial command, accepted
phone fixture or Main ledger write is repeated. The interrupted-candidate continuation copies
only the already preserved original plaintext source within the same UID, validates unchanged
source/copy digests and all51 applied migrations, and creates no new financial rows. Private
keys, envelopes, aliases and private digests never leave the app; comparisons return booleans.
Native observations and controller checks add zero managed-suite test cases.

## Actual failure and targeted controller correction

The initial host poll starts after am start -W. Both candidate files exist by then, but the
following native assertion refuses: API24 is already Selected and API35 CandidateVerified.
Those two failed timing assertions remain failures. A new read-only phase inspects only the
same original journal/file facts: original source and protected envelope digests still match.
No migration or financial preparation is retried to acquire this diagnosis.

Correct the observer, not a healthy storage algorithm. AndroidExportObservation.psm1 validates
one fixed independent package, a strict owned candidate path and fresh output/metadata. It
refuses an already present candidate, arms a device-local existence watcher before launch,
retains actual host PID/target metadata and accepts only the exact observed native PID plus
STOPPED result and terminal state. The caller separately proves package PID absence. Pending
returns the same live handle; it is not a reason to restart. The original migration controller
uses this module, and its ordinary continuation function is also exercised on the retained
initial fixtures. A reattached Windows handle may have unavailable exit metadata; that absence
is explicit, never numeric exit0, and cannot replace the required native report/death proofs.

Active redirected reports need FileShare.ReadWrite. The initial exclusive reader refuses after
starting two observers; retain those handles. API35 reuses its actual live observer. API24's
first observer really expires before any Activity dispatch, with EXPIRED and absent process;
only then is a fresh observer armed for the still-unexecuted same native dispatch. The API35
native STOPPED receipt is recovered after a host exit-metadata refusal, without another launch.
Both original failed fixtures subsequently complete their previously unexecuted migration-resume
phase, preserving model/money and encrypted rollback. These are two additional original
continuations, not a repaired claim that the old polling window passed.

Windows PowerShell5.1 refuses running the local script under its existing policy. Do not change
or override that policy. Actual controller execution uses already available PowerShell7.6.5
with RemoteSigned. Parser-only checks pass under actual5.1.26100.9711 and7.6.5;5.1 native runtime
execution is not claimed. A host UID observer also learns Android15's appId field instead of
assuming the older userId spelling; only the unexecuted API35 diagnostic continues.

Ten focused controller contracts pass: eight parameter/fresh-evidence refusals, refusal of an
actual already completed candidate without changing its live app PID, and real positive arming/
pending retaining the same live handle. That read-only pending check creates no native phase;
after intentional observer-only cleanup, delayed device-shell teardown is actually observed
complete and the original app PID is unchanged. No financial/native source cohort is replayed.

## Package lineage and final cleanup

| Actually executed independent APK | SHA256 | Bytes |
| --- | --- | --- |
| Initial original source |e3894a8f5170192f5752510b9b769e31b1bcf345ea2664d903b5f8378a801677|36098056|
| Read-only failed-cohort observation |a13d4f5de524797d820efd3169b7434abed1118186d2e6310b08642ec829fca5|35825257|
| Retained-source continuation |441faf3856474e19846c09dbc5614b657882a30309bd32702068a05afdcfcaf7|36114440|

All three strict publishes succeed with v2/v3, ZIP CRC, fixed identity/API24 and matching pinned
native slices. Preserve every actually executed signed APK as failure/source lineage; none is
Main. Final installed code matches the last full digest in both UIDs. API24 sync pull reports
that the public APK does not exist although shell stat shows it; a complete public-code stream
has the exact36114440bytes/digest. API35 sync pull matches and its redundant host copy is removed.
These are transport observations, not evidence of a native data/key failure or a guessed
internal adb/SELinux cause.

After the terminal32-record audit, two original continuations and installed-code equality,
uninstall only the two new helpers and verify absence, then stop the two owned AVDs. No remote
XML is created. Standard native-project cleanup passes without warnings/errors. Delete only
nine unused new AAB/resource copies,216029526bytes, retaining all three actual signed APKs and
local public evidence. SDK/AVD definitions remain needed; physical phone, general SDK and existing
ADB server are not modified.

This qualifies only the named independent single-profile x86_64 storage/controlled-process-fault
and recovery cohorts. It does not accept every all-profile lifecycle, natural low-memory or
power loss, hardware authentication/biometric changes, OEM/cloud transfer, ARM64 page-size,
Main private data/keys, all UI/backup/OCR/SDK paths, general ANR/crash/performance/accessibility,
iOS, providers, Store or release. Shipping source/schema/dependencies/permissions/APK are
unchanged. D-520 exact-head [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40131294470)
and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40131294514) succeed and its
one-shot monitor is consumed/deleted; new-head hosted qualification remains separate.
