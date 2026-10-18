# Complete first settings read (D-363 / AT-363)

## Cause and production boundary

D-362 left the first settings call unsplit. Fixed independent-only phase markers now separate
the original protected context, EF command execution and row materialization. The first split
records context16ms/query682ms. A subsequent command split records context9ms,643ms before the
command,4ms executing it and65ms until the settings result. This does not attribute all pre-command
time to one EF method: connection opening, model/services and query preparation share that interval.
It does exclude a slow settings-row SQL execution as the dominant measured cost.

`SettingsRowReader` replaces only the two no-tracking asynchronous earliest-row queries in
`GetSettingsCoreAsync`. The synchronous getter, tracked writes, serialized creation/update gates,
maintenance factory selection and initial missing-row behavior remain original. The reader uses
EF's existing relational command builder, captured relational connection, diagnostics, timeout,
interceptors, joined transaction and cancellation. It never creates a separate connection or caches
settings, a context, a key or a key-bearing connection string. This is the existing complete-ledger
command pattern, applied to the25-field settings projection.

The complete current model contract checks table/schema, property names/count/types, original
column names, scalar/timestamp converters, absence of inheritance and query filters. Metadata drift
fails before reading. Nullable values, enum/provider coercion, original private-set identifier and
model-owned UTC-tick conversion are retained. Immutable model metadata alone is weakly cached.
There is no schema/entity change, migration or compiled-model regeneration.

No experimental EF query-precompilation feature or dependency upgrade is enabled. Microsoft
[documents that feature's production limitations](https://learn.microsoft.com/en-us/ef/core/performance/nativeaot-and-precompiled-queries).
No resource deferral, model-warming replay, native clock/GC change, weakened KDF, licence reordering,
app-lock fallback or skipped profile validation accompanies this change.

## Whole-path comparison, including shifted EF cost

The first result reduces settings time but increases the first Home read: some shared EF work
moves later. Therefore measure the original complete `Loading.RunAsync` Home publication too,
then confirm once in reverse APK order using existing immutable packages, without rebuilding,
recreating data or resetting either installation.

| Original process | Variant | Settings query interval | Home load/publication | Managed Application entry to Home publication |
| --- | --- | ---: | ---: | ---: |
|13413 | Original EF query |704ms |1537ms |4615ms |
|13619 | Complete typed reader |178ms |1821ms |4318ms |
|13814 | Same reader APK, confirmation |176ms |1808ms |4350ms |
|14009 | Same original APK, reverse confirmation |722ms |1577ms |4572ms |

The paired total reductions are297ms and222ms, substantially smaller than the isolated settings
improvement. These are four instrumented process-cold/warm-file-cache samples on the owned x64
emulator, not a physical-device distribution. The first managed Application callback excludes native
pre-entry work. Home publication is not the GPU/frame-completion boundary or a two-second loaded
Home claim. Existing package-only historical ANR lines are not assigned to these new PIDs.

All four processes finish original native Home with identical compressed hierarchy SHA256
`3131a10ae075af7ad96a5350b4a03f6fc1d34b5ace344f857534507c32a9a223`, My account and0.00EUR.
The first comparison attempt used an uncompressed hierarchy against a compressed baseline and
correctly failed equality; taking the same compressed format resolves that observer mismatch.
No process is restarted after an observation timeout or failed assertion. Each completed process
is stopped only after its actual startup/Home markers and native hierarchy are retained.

Original APK SHA256: `6a6a16d9532fe1be0682d46f860fdb10a1b2b9597692dfea8ec1bb3286b89fbc`.
Reader APK SHA256: `6e41df7517c7f22fb437fd83d945e993a9c019a7dad0c76a5802bdb01c1f94b6`.
Both contain192 AOT libraries; four native engine entries and the signing identity match D-360.
All temporary phase callbacks, interceptors, source symbols and Home markers are removed afterward.

## Focused regression and native preservation

Eight new cases compare every original settings property against independent EF materialization
with populated/null values, preserve exact identity and earliest-created ordering, prove detached
fresh reads and explicit saves, refuse cancellation before creation and after reader completion,
retain a borrowed transaction's uncommitted value/rollback ownership, and refuse a future model
query filter before issuing SQL. The two existing settings creation/update concurrency regressions
also pass:10 distinct focused cases across retained selective runs, not a full-suite rerun.
An initial combined class/method CLI filter selected zero tests; it is not counted as verification.

The independent final Release/AOT `settings-read` phase reuses the marked reference/tenfold root
`ca28fce07b29472a95ff4da3cb66d2f0` and its existing protected generations/keys. PID14159,
comparison`055b474b0c914f0ba99d7870c39feda5`, verifies all25 properties through ordinary and actual
exclusive-maintenance reads, cancellation, and refusal after the generation key owner is disposed.
Full schema and typed rows compare equal in original in-memory portable snapshots before/after.
No financial Save, replacement key, new fixture, main-package data or device setting is involved.

Final independent APK SHA256:
`d16718ec58da91e0810850af11aa90c424900764db0c269aa74c992302ba9bdc`.
Its192 AOT libraries retain all four original engine bytes, original signature and offline,
non-debuggable/non-profileable manifest. Strict build succeeds with zero warnings/errors.
Caller MVID`ee893935-2c5c-4fa9-8573-4c967f1203e3`; Data MVID`5899c77f-665e-4733-8f8a-55622a185dd4`.
The subsequent source comment names D-363; it changes no reader algorithm.

## Handoff and remaining scope

Strict Windows and complete ordinary Android APK builds succeed with zero warnings/errors.
The ordinary offline package is artifacts/android-settings-reader-d363/pro.vafadar.zanance-Signed.apk,
94,830,325bytes, SHA256`b0116347878c0e6f256d8dd893c4fa9c88b6deffb56ceb944d3337ee855b8895`.
Its verified signing certificate remains D-360; minAPI24/target36, ARM64/x64,384 AOT libraries and
all eight native engine entries are unchanged in scope/bytes. The manifest declares no INTERNET,
debuggable application or profileable element.

Install-r upgrades the original emulator main package without clearing data. Old PID11333 ends
at package replacement; the single new process14312 reaches the original My account/0.00EUR Home.
Its51-node compressed hierarchy is byte-identical to D-360, SHA256
`2f53c71a57dffbebfc9954a0df1ca533ece7d5a0794fabb81edb48bfb1f155e4`.
The initial dump produced no remote file before readiness. Continued observation retains14312;
the host's Home assertion is corrected from a TextView caption to the original native tab content
description, using the same captured hierarchy. Neither observer issue triggers reinstall, restart
or an app change. No exact-PID AndroidRuntime error is recorded. Font1.0/accessibility0/absent
enabled-services/touch-exploration0 remain exact; no financial Save or Samsung input occurs.
Owned remote XML is hash-matched before removal, and test-filter cleanup succeeds without warnings.
Local immutable APKs, exact original-source backups, focused test XML, fixed-phase logs, full native
hierarchies and derived paired/native receipts remain under artifacts/settings-stages-d363.
No profiling/diagnostic permission or temporary marker belongs in production.

This closes the measured first-settings query-preparation change, not QA-06 as a whole. Native
pre-entry startup/ANR, large physical workloads, low-memory, spoken reader, Mac/iOS, providers,
store/company/signing and release gates remain separately open. No unrelated successful matrix,
full financial/crypto/backend suite or Samsung test is repeated for this change.
