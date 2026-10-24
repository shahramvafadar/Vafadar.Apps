# D-523 - Workload preservation after Home revision invalidation

## Failure boundaries

D-521 hosted [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40135440296)
and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40135440218) succeed.
[Native cipher candidates](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40135440298)
fails only its Windows protected-reference/tenfold measurement step; the Android producer and
stock suite succeed. The public job annotation contains only exit1. Log download returns403
and both browser views request sign-in. The owner is asked to authenticate personally; no
credentials are read or account sign-in performed. The one-shot D-521 monitor is consumed/deleted.

The unchanged failed-step project builds locally without warnings/errors. Its original executable
then reproduces a terminal Snapshot record content differs exception at the complete after-write
portable comparison. The reference fixture has already completed four original validated Save,
read-back and exact Delete operations. Preserve that fixture and original failure. Tenfold has
not yet been prepared. This is a confirmed local qualifier failure; attributing the hosted
failure to it still requires the actual hosted log. Do not label a guessed hosted cause fixed.

## Correct comparison boundary

D-509's original HomeFinancialRevision row is intentionally invalidated by successful financial
INSERT/UPDATE/DELETE operations. Restoring money through Delete must not restore the old token:
doing so could allow a stale Home snapshot. The old benchmark incorrectly demanded equality
of that opaque concurrency token after its deliberate round-trip writes.

WorkloadSnapshotVerifier belongs only to the independent benchmark. It reads the singleton
integer Id1/text32 lowercase hexadecimal token, requires its change exactly at the accepted-write
boundary and normalizes only that field in a disposable memory copy of the original snapshot.
The unchanged production SqliteSnapshotVerifier then compares complete headers, schema, triggers
and every typed financial/other row. Original snapshots stay immutable. No production restore
guard, financial mutation, source/schema/dependency/permission or Main APK behavior changes.
Measurement fields distinguish complete financial preservation from the deliberately changed
opaque marker. The two original local continuation receipts used the earlier broad field name;
a separate interpretation receipt preserves this distinction without rewriting original evidence.

Eight new focused actual-source cases pass against original migrated SQLite and backup sources:
an accepted Save/Delete round trip and unchanged read-only snapshots pass; unaccepted writes,
claimed-but-missing writes, a changed original financial row, added schema object, header edit
and malformed revision refuse. The round-trip case first proves the old production complete
comparison rejects the changed token. No unrelated test suite or successful Android producer
is rerun locally.

## Continue the same source without another accepted write

New explicit verify-retained mode reopens only existing marked protected fixtures; it never
prepares a missing shape and executes no timing/search/Save/Delete operations. It takes the
exact original portable projection using the actual backup source/policy on a fresh encrypted
copy of the retained original rollback, then compares the current generation. Original rollback
digest equality is required and the exact new copy/sidecars are removed. Keys stay runtime-only;
no plaintext disk snapshot or private fixture artifact is uploaded.

An initial ReadOnly creator is correctly refused by the existing protected factory before backup
execution. Preserve that failed observation. Give the unchanged ReadWrite-only factory only the
fresh encrypted copy, rather than weaken its contract or open the immutable original for writing.
The original reference then verifies successfully with zero replayed financial/timing commands.

Explicit tenfold mode continues only the previously unexecuted100000-entry/200-account/1000-plan
shape. It skips the accepted reference and refuses an already prepared tenfold folder. That new
shape completes original reads, full source validation, search/group assertions, four distinct
validated Save/read-back/exact Delete samples and final complete financial comparison. Its
four known search counts are1/0/2/100000. A subsequent deliberate repeat admission is refused
before preparation or any financial write. These are fictitious store commands, not physical
Main financial operations or MAUI performance acceptance.

Strict benchmark builds and eight focused cases pass; standard test/benchmark cleanup succeeds.
Preserve the owned failure/continuation fixtures privately while hosted diagnosis remains open.
No visible app change requires a duplicate MAUI/phone matrix. Final exact-head hosted success,
general ANR/crash/performance, two-second Home, accessibility, physical/UI, iOS, providers, Store
and release remain separate. The main goal remains active.


## Subsequent hosted observation

The one18-minute exact-head check of fb134949 reports [Native cipher candidates](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40137760234) and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40137760224) success; [general CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40137760222) is cancelled. Its consumed monitor is deleted. This confirms the corrected current native qualifier, not the unobserved historical exception text. Later documentation-only heads preserve the same native source; their own general CI is separate.


## Original hosted log and latest complete CI closure

On25October the owner opens the original failed job in an authenticated browser. The visible
[Windows log](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40135440298/job/120995863846)
shows a zero-warning/error build, then the Snapshot record content differs InvalidDataException
at SqliteSnapshotVerifier.CompareRows line89, VerifyAsync line53 and original Program.cs
line195. This is exactly the terminal after-write comparison reproduced locally; it is not a
compiler/native-producer failure. D-523 changes only that independent comparison boundary and
its continuation/tests; the original production verifier remains unchanged.

The corrected Native cipher candidates run succeeds as recorded above. The single exact-head
check of latest bec2c2d3 reports [CI success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40139143221)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40139143159).
Its monitor is consumed/deleted. No old job, successful native producer, accepted source or
financial command is rerun to obtain the historical log. No credential is read or entered.
This closes the reported current qualifier defect and its engineering CI check; it does not
close the main product backlog, stability/performance/accessibility or release gates.
