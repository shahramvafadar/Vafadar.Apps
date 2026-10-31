# D-701 - Enforce the original portable image budget before native growth

## Actual defect, separate from native-crash cause

Review of the project's native-copy boundaries finds a real incomplete budget in
SqlCipherPortableSnapshot.WriteAsync. The fresh attached memory database receives
max_page_count only when emptyRowTables is nonempty. Original restore rollback and
other no-policy callers therefore export/materialize the entire image before the
later serialized-length refusal. D700's original SGen fault and the separate
managed resource-cache exception remain unresolved; this defect is not claimed
as either failure's exclusive cause or a general stability repair.

Two actual original Windows SQLCipher checks reproduce the missing/null and explicit
empty-policy cases before changing production: maximum32768bytes, ordinary payload
131072bytes,34exported pages and one preparation callback before refusal. Output
is0bytes, selected source values remain exact and the attached alias is released.
The process returns1 because the preparation boundary is too late. These are new
fictitious in-memory SQLite packets, not financial commands or replayed profiles.

## Complete correction and native error boundary

Give every fresh attached destination the existing native page budget before
sqlcipher_export, independently of whether it omits private rows. Keep the original
source transaction, header copying, full permitted rows, explicit omission authorizer,
access checks, final serialized-length check, zeroization/free and detach ownership.
Do not reject by source page_count: an explicitly omitted large private journal
must still produce a small valid portable image. No field/schema/migration,
financial formula, key/KDF, permission, runtime, collector or UI copy changes.

The [SQLite page limit](https://www.sqlite.org/pragma.html#pragma_max_page_count)
controls destination pages; serialization normally makes another native copy
([original API contract](https://www.sqlite.org/c3ref/serialize.html)). This is a
portable-image page budget, not a cap on total native/process memory or proof of
lower peak PSS. Retain the original whole-page rounding/minimum-one-page behavior
and final exact byte guard.

The first page-bound continuation rejects growth before preparation, but actual
SQLCipher reports its export scalar failure as SQLite error1. Preserve that
intermediate observation. Wrap only SQLITE_ERROR/FULL from the actual export
command in InvalidDataException with the original native exception retained.
The original portable I/O family now reaches callers rather than an unexpected
raw provider error. Cancellation-requested and other native failure codes are
not reclassified; exceptions are not swallowed or labelled an accepted backup.

## Actual Windows and Android cases

The new independent eng/tests/SqlCipherPortableBounds host calls the original
shared exporter and original memory-image reader against the real reviewed native
SQLCipher package. It copies no exporter and installs no fake SQLite scalar function.
The seven finite cases are:

1. Missing omission policy with an oversized ordinary packet.
2. Explicit empty policy with the same named oversized boundary.
3. Missing policy with a fitting ordinary packet.
4. Explicit empty policy with a fitting ordinary packet.
5. Large explicitly omitted private journal with a fitting ordinary packet.
6. Existing nonempty policy with an oversized retained ordinary packet.
7. Original cancellation before native export.

All seven Windows cases pass. Oversized inputs have preparation0/output0 and
portable InvalidDataException preserving inner SqliteException. Fitting output is
12288bytes/3pages with an8-page destination ceiling, full signed/Unicode/payload
packet values and user_version73/application_id4660 retained. Only selected
private-journal rows are omitted. The comparisons cover those explicit packet
values, private-row counts and two header identifiers; they are not a full source
schema/whole-database/key comparison. Source records match before/after and owned
attachment aliases are absent after every outcome. Cancellation remains cancelled.

The same committed check source is linked into one separate original shared-library
Android component, with no app/profile/account/ledger preparation or financial
command. Its actual native button is pressed once in a fresh API36/x64/4096
fixture, UID10216/PID4934, bootfcc53f98-bb1c-41bb-811e-17dad2f07644.
An exclusive native marker prevents replay. The real UI reports seven passed;
all seven actual result JSONs are read from that same completed process. The
original shared Data library/SQLCipher entry points execute on Android, not a
Windows result copied into a fake page. Actual x64 cipher bytes equal shipping.
This is component/native allocation-boundary acceptance, not a repeated Main
backup/restore UI or real authentication/encryption/recovery acceptance.

These are seven native checks per platform, not fourteen new xUnit cases. The
Windows CI job now runs this host after preparing its reviewed native dependency.
Its clean hosted checkout is separate from refused local output scopes. The final
host's serializer-only source-generation refinement compiles strict; successful
Windows case effects are not replayed just to change JSON serialization metadata.

## Builds, packages and restoration

Windows app strict build completes with0warnings/errors in44.59seconds. Complete
offline signed shipping cf3bda4d60ac6d02da81ab1949818f5f15ca13c85d8ea18587da8919f018f189
is99307423bytes with384ownAOT/408ELF/1608static16KiBLOAD, v2/v3, CRC and ZIP16KiB.
The actually used independent component
4d76984a5194c350dbf6018ce05ebc3dba9ba72ec50e6d32bc5569750ccba222
is12591524bytes with74AOT/85ELF/329static16KiBLOAD and the same package guards.
Both are offline/nondebuggable. Static alignment and this4096-byte emulator are
not real16KiB hardware/API24 runtime, whole-PE identity or phone adoption.

No OS setting changes occur. All five original setting values,1080x1920/density420,
locale and boot match before retirement. Preserve used helper-source copies and
result packets outside cleanup roots, hash-match/delete2owned XMLs4841bytes and
remove only the finished fresh package/UID. Shut down only this emulator; official
AVD deletion retires25files/5052642363logicalbytes. Five scoped standard Cleans
complete. The last Windows clean initially fails NETSDK1005 after Android-only
restore; retain that failure, restore only its missing Windows target and continue
only that unfinished clean. No successful app build or prior clean is replayed.

Automatic approval review rejects the sole supplementary nine-target cleanup
before process creation with blocked by policy:343files/141440327logicalbytes
remain frozen, including the two new private source roots, residual AVD user root,
the new Windows check host's local bin/obj and four unused publish files. No retry,
subset, alternate method, move, escalation or reuse occurs. Later local host builds
must use a fresh artifacts directory rather than those refused bin/obj inputs.
Used signed packages/source/receipts remain outside refused scopes; logical bytes
are not measured host free-space reclamation.

Phone/D607's three deadlines, consumed reboot and unread post-reboot source remain
untouched. Main adoption waits for actual D607 completion/cleanup; cf3bda4d is only
a prepared candidate. Old successful financial, PDF/CSV, widget, restore, UI and
unrelated unit/native suites are not replayed. General crash/ANR, security/DB/key,
Home speed, accessibility/iOS/provider/Store/release gates remain independent;
the overall delivery goal stays active.

## Sole delayed CI observation

D701's exact-head response shows [CI in_progress/no conclusion](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41236627405), with [Local identity backend](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41236627438), [Self-hosted service qualification](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41236627477) and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41236627471) completed/success. Its exclusive marker/raw response/receipt are retained and only its schedule is deleted. No secondGET or later main-CI/native-Windows-host pass is inferred.
