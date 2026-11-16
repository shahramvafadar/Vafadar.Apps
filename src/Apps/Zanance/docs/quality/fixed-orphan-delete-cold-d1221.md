# Fixed orphan DELETE cold-start trial (D1221)

The owner selected a 6,000-ms process-cold Samsung target after the current
original-source measurement. This independent private candidate retains the full
original import/Undo journal validation and replaces only EF expression translation
for orphan deletion with parameterized fixed plain SQLite DELETE statements.
It is rejected on the complete native startup gate and is not promoted.
The overall delivery goal stays paused.

## Preserved boundaries

Original context/key/maintenance ownership, original validated durable receipt
identities, live-entry protection, cancellation, plain DELETE conflict semantics,
connection open/close/disposal and startup ordering remain. No pooling, retained
open connection, deferred cleanup, journal bypass, schema migration or altered
security/license/crypto/financial policy is introduced. This is independent of the
closed D895 open-connection candidate. GUID identities use a typed text parameter
with canonical uppercase serialization; values are never SQL concatenation.

Latest main is fetched before work and before recording. Concurrent Dastan changes
and the unrelated untracked benchmark file remain untouched. Tracked product code,
public package Main and its private source are not changed.

## New host checks and fixed comparison

The actual host is .NET 10.0.15/X64 with SQLite/SQLCipher 4.5.2, not the native
Android 4.19 provider. Ten new negative controls cover pre-cancellation, malformed
version 3 content, invalid JSON, invalid version 2 digest, and cancellation after
journal close, once for each role. Original exception types, full original source,
lease draining and zero/one expected connection opening are preserved.
They are not repeated after unrelated positive-observer corrections.

Three shapes use seven inverse pairs each: no rows; one live, one durable protected
receipt and one orphan; and the same shape with 10,000 durable protected IDs. Each
child executes the first actual purge without a warmup purge. Complete typed
source/DDL/schema/header/sequence checks and all 32 ledger columns prove exactly
the expected orphan delta, original row order/content and no other source change.
All 42 captured before/after packets independently retain 32-column evidence.

| Shape | Original median ms | Candidate median ms | Candidate/original allocation | Additional median peak bytes |
| --- | ---: | ---: | ---: | ---: |
| empty | 787.0489 | 732.2953 | 0.851339 | 0 |
| protected | 823.7651 | 769.3569 | 0.850904 | 0 |
| many | 1356.9880 | 918.8717 | 0.816086 | 0 |

All three host latency/allocation/peak gates pass, admitting only the native trial.
An initial absolute compile glob included a historical nested generated attribute;
explicit tracked compile inputs correct that build refusal before checks. The first
original-only positive case also exposes SQLite's exact creation of a missing
sqlite_sequence SyncOutbox0 row during a zero-delete trigger. Its complete delta
is retained and expected explicitly only when that sequence is absent and outbox
empty. Existing sequence values and every other source/header remain compared;
no sequence normalization or performance threshold relaxation is used. Negative
controls, source-diagnostic and final positive host binaries have separate lineage.

## Fixed current Samsung comparison

The retained independent UID10458/package performanceacceptance keeps the original
10k entries/20 accounts/100 plans and all original keys. One no-reset observer
upgrade contains both roles, original stock Mono 10.0.15 and actual source/PDB
bindings. Every role runs one original purge before Home. There is no reseeding,
financial UI command, successful warm/search replay or Main upgrade.

The fourteen predeclared roles are O,C,C,O,O,C,C,O,O,C,C,O,O,C. Every process-cold
launch follows actual cached/adj>=900, owned am kill without force-stop, two absence
proofs and the normal launcher. Timing uses the immediately preceding device uptime
floor through actual original published Home/native ready draw, not first frame.
All individual samples remain, without outlier removal, selection or rescue.
The first inverse-pair gate passes; the complete fixed median gate below fails.

| Metric | Original median | Candidate median | Candidate/original |
| --- | ---: | ---: | ---: |
| Complete launch to ready ms | 7110.673385 | 7014.439445 | 0.986466 |
| Actual original purge boundary ms | 1007.318423 | 861.072038 | 0.854816 |
| Managed allocated bytes | 58842224.000000 | 58178488.000000 | 0.988720 |

Additional median process peak is 0 bytes. Complete startup improves only 1.3534% against the predeclared 5% gate. Candidate median 7014.439445 ms also misses the owner's 6,000-ms target. The 14.5184% purge-boundary improvement is not an equivalent whole-startup gain.

All 14 complete source/Home checks retain strong source
A9833C5C51A67D5E918C3C36AB88B08124A6C91D28BFCCBAA54520304DAE1708
and the original full Home projection. This source includes all 32 ledger fields,
schema/user/application cookies, sqlite_master/table_xinfo and sqlite_sequence.
Actual original preparation, purge and publication order remain.

One read-only process/cached-state query was issued during role 2's nominal quiet
interval. Local file-time/launch evidence places its completion about 12.906 s after
prelaunch, after that role's 6.889 s ready boundary; it is retained as a transport
qualification and no sample is discarded/repeated. These process-cold launches
have warm OS/file caches and do not establish percentiles, power-cold behavior,
GPU presentation or all-condition latency.

## Terminal decision and preservation

The candidate is closed before further native protected-ID correctness, product
promotion, shipping/Windows builds or old tests. Host equivalence does not become
unperformed Android 4.19 receipt/orphan acceptance. No retune or selective rescue
is made after the complete native failure.

The prior original-source 9b9ec12a observer APK is restored once without reset.
A read-only adoption reports the same complete source and Home, retained successful
cold/warm markers and no measurement/search/financial replay. Original timeout
300000, Samsung Launcher, all five OS values, locale/key presence, geometry, boot,
reverse list and every prior public package UID/APK are exact. Main remains
untouched. Original ADB 5037/PID59860 is not reset or reconfigured.

Twenty-eight used source/PDB/host binary/native AOT/APK objects are hash-retained.
Two standard Cleans remove 13,767 registered files/831,181,417 logical bytes. There is no supplementary/frozen deletion or cleanup retry. Encrypted fictitious host packets and raw paired outputs remain locally.

The [current acceptance](current-performance-acceptance-d1219.md) remains: warm
reference Home and the two scoped reference searches pass; cold 6,000 ms and the
[interrupted stress search/native exit](current-search-native-exit-d1220.md) remain
open. A separate read-only [profile admission](current-startup-profile-admission-d1222.md)
identifies real old Home state-machine type roots that no longer match current
type names. That is a distinct next question, not a relabelled orphan-delete rescue
or proof of uncompiled methods or the whole-startup cause.
