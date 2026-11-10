# Actual own-keystore entry loss and original client refusal — D-1004

This closes a specifically missing Android private-journal condition after
[D973's controlled value removal](private-analysis-native-storage.md) and
[D1003's inaccessible-marker correction](private-analysis-marker-permission-refusal.md).
The actual fresh package's single known keystore entry is deleted once. The observer
never requests key objects, encoded keys or raw ciphertext, and never enumerates
aliases. No credential or Main data is read/exported. Product code, model, schema,
crypto policy and permanent unit-test count
are unchanged.

[D1025 physical ARM64 continuation](private-analysis-keystore-loss-arm64-d1025.md)
subsequently qualifies the named fresh-package alias-loss refusal on Samsung. The
software-emulator scope and limitations below remain the original D1004 evidence.

[D1026 minimum Android and ps16k continuation](private-keystore-loss-api24-ps16k-d1026.md)
adds two new compatibility cohorts using the same exact SDK and five unchanged
compiler-bound source inputs, without repeating the original API36cohort.

## Scope and original implementation

One fresh offline/nondebuggable SDK package links the current SecureAiStorage and
actual Data AiClient/PersonalSyncTransport. It does not create the original App,
profile, account, financial database, ledger, service identity, provider or HTTP
endpoint. A never-created local database filename and fictional identity bind the
client scope. One declared valid pending AiClientRecord contains a zero aggregate;
it is an input intent, not a provider acceptance/result, credit reservation or
financial save. Actual client read-back validates its original id/version/pending
state before the key-loss boundary.

The pinned [MAUI shared storage source](https://github.com/dotnet/maui/blob/10.0.120/src/Essentials/src/SecureStorage/SecureStorage.shared.cs),
[private preference-name construction](https://github.com/dotnet/maui/blob/10.0.120/src/Essentials/src/Preferences/Preferences.shared.cs)
and [Android backend](https://github.com/dotnet/maui/blob/10.0.120/src/Essentials/src/SecureStorage/SecureStorage.android.cs)
identify the package-derived alias used by its MasterKey builder. The fixture
requires its own keystore count0 before setup, then exactly1 with that known alias
present after the actual native write. Only that exact own-UID entry can be deleted.
No alias enumeration, GetKey/GetEntry/GetEncoded or certificate/key extraction API
is present in the observer. Package/run/PID/UID, shell caller, ranchu and x64 guards
bind every phase. The declared access callback is not genuine Main/profile/auth
acceptance.

## One retained journal, two necessary process boundaries

API36/x64/4096/.NET10.0.14, UID10216, boot
`ac344bdc-0aec-41ca-b6fa-52986e003b16`:

1. PID4882 writes the fictitious journal once through original SecureAiStorage.
   Original AiClient reads that exact valid pending id. A bounded public initialized
   marker digest and content-free stage flags are retained. No client write,
   credential, source-recheck or network effect occurs.
2. One scoped force-stop/normal relaunch gives PID5335 without reinstall/reset or
   preparation replay. Original AiClient again reads the same pending id, proving
   process persistence **before** deletion. Own alias count1/presence is rechecked,
   one exclusive deletion marker is created, and only that known entry is deleted.
   Count0/absence is confirmed immediately; the public marker stays exact.
3. A second scoped force-stop/normal relaunch gives PID5608, excluding old in-process
   key handles/caches. Before any SecureStorage read, count0/known-alias absence is
   required. Original client Read, Submit, Continue and Close each refuse with
   IOException. The actual readable initialized marker still has its original digest.

Both process boundaries are prerequisites of this new key-loss condition, not
replays of D973's nine-case suite. The original journal write, deletion and each
phase execute once under retained one-shot markers. No phase is restarted for a pass.

Post-loss client counters are4private reads,0private writes,0credential calls and
0source-recheck callbacks. A write fence and credential/source ports would count
and refuse before an unexpected effect; neither fence is reached. Actual client
source ordering prevents submission/status/close transport after these reader
exceptions. The manifest has no INTERNET permission; no live session or network
server is configured. Native SecureStorage reports the original value unavailable,
while a genuinely unknown scope still reads null. This proves scope-specific refusal,
not a blanket refusal for every new scope or a fabricated financial result.

The raw Outcome alias boolean in the deletion phase denotes **after deletion**;
only the post-loss phase measures it before its first read. The raw check label
mentioning framework rekey denotes the inferred published backend recovery path;
replacement-alias metadata is not measured after reads. A supplemental clarification
preserves both distinctions without rewriting raw packets or repeating execution.
The observed claims are deletion/absence, four original refusals, unavailable old
value, exact public marker and the new-scope null control.

## Build/source evidence and retirement

One strict SDK build passes without warnings/errors. Used signed package is
17888386bytes, SHA256
`54778a2710e17de73d0d74775c32807d5e9528a7c9eefaea00d6030dd15d0c4e`,
104AOT/114ELF/446static16KiBLOAD, original x64Monoedfa, v2/v3 signatures, ZIP alignment
and CRC pass. This is not full shipping or hardware16KiB runtime evidence. Five
actual compiler-document checks bind current storage, observer, Data AiClient and
both base/analysis transport sources. They do not prove whole-PE identity or an
upstream source-to-binary reconstruction of every platform dependency.

No Windows/full-shipping build, old host suite, biometric/PIN/camera/import/share or
financial action repeats. The existing complete D1003shipping remains prepared;
phone/Main has no command in this cohort.

Only the fresh package retires after its terminal result. Five original OS values,
locale/key presence,320x640/density160, boot and reverse mapping remain exact without
public setting changes. One owned emulator console stop leaves all four original
host handles signalled within45seconds; original ADB5038/PID65060 creation identity
stays exact, with no server reset/start/reconnect/pair or alternate kill. Official
retirement removes24AVD files/4154471701logical bytes and leaves no AVD files.

Two own-output standard Cleans retire888registered files/119527476logical bytes;
438remain. The151logged file-delete targets are inside current own bin/obj roots;
the registered-file count also accounts for files removed by standard directory
cleanup. Six scoped userhome files/7732bytes and one992byte host-tmp file remain.
Used signed/source/PDB/stage/result evidence is retained. No XML, supplementary
delete, frozen-scope read/retry/alternate cleanup or retained5606/Q02 action occurs.

## Explicit remaining boundaries

This is actual loss of one own Android keystore alias in a software-emulator
fixture. It is not genuine biometric enrollment invalidation, hardware/StrongBox
failure, power loss, OS/OEM transfer, complete private-journal recovery, original
database-key lifecycle, Main/physical ARM64/iOS or whole security acceptance.
The intentionally deleted fixture key is not reconstructed/exported; its package
is retired. Canonical parent States, original native-fault cause/remedy, whole
performance/accessibility/platform/provider/Store/release gates remain open and
the complete delivery goal stays active.

The single prior D1003response at approximately16minutes50seconds after recorded
push reports [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42781788324)
and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42781788183)
in_progress with no conclusion. The exact response and exclusive consumption marker
remain; there is no second query, polling, successful job-log read or rerun.
