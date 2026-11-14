# Guarded complete journal reader component (D-1172)

## Measured premise and independent candidate

[D1164](original-journal-query-compilation-d1164.md) records645.0645ms in the
original journal query-compilation span. [D1166](protected-journal-compiled-reader-d1166.md)
qualifies a different public compiled-delegate candidate; its subsequent
[D1167](compiled-journal-complete-startup-d1167.md) cold cohort is closed without
performance admission. None of those accepted/closed operations is replayed here.

This new private candidate reads all four original ImportLinkBatch fields through
one fixed native command, then invokes the unchanged original JSON/version/digest
validator. It does not narrow the query to StateJson, alter attachment protection,
defer purge, cache a key/connection/context, change a model/migration, use a private
EF API or import the previous background compile-only task. Product code remains
unchanged; the candidate exists only in the independent caller.

Raw ADO commands bypass EF command interception. The original factory registers
its actual SqliteConnection with ProtectedSqliteAccess using the same captured
publication/maintenance-owner validation closure. The candidate explicitly requires
that registered connection before opening, after opening and after reader disposal.
EF OpenConnectionAsync/CloseConnectionAsync retain connection admission and lifetime.
The fixed reader preserves current transaction/command-timeout selection, GetGuid,
string materialization and original UTC-ticks DateTimeOffset conversion. It checks
cancellation and actual reference-identical model admission; it does not pretend
that a replacement model was checked by EF's compiled-query implementation.

Eleven actual PDB/source bindings cover both executed hosts, private reader/branch,
original context/entity/compiled mapping, captured factory, access registry,
connection/command interceptors and access gate. Reversing the sole private reader
choice reproduces the complete original ImportAggregateStore source exactly.

## New protected correctness for the changed reader

One fresh .NET10.0.15/X64 process uses the actual protectionRequired factory and
SQLCipher4.19.0-candidate.7/provider3.0.5. The actual Windows native SHA256 is
`2dc0973b3fb59cd52d83b1b22a48d5e02fcc8c3bc3d2de4de18c756fab7a8d59`.
No account/ledger rows or accepted app financial operation are prepared.

Thirty-one finite roles pass: thirteen original/candidate comparisons, two cross-file
roles, two retired-owner roles and one different-model refusal. Cases include both
journal versions/durable attachment exclusion, complete four-field extrema and order,
null/malformed JSON, unsupported version, invalid digest, malformed GUID/ticks and
cancellation before factory/enumeration. Source schema/schema_version/all typed rows
remain exact, original error categories and purge behavior match, and no entity is
tracked. A metadata-only model binding opens/enumerates nothing and disposes its
original never-open connection. These are necessary comparative contracts for new
reader code, not31new product tests or a replay of an accepted financial cohort.

## Registered first-query host comparisons

Only after correctness passes, register two shapes: zero and32valid journal rows.
Each shape has one new encrypted metadata-only seed. Each child receives its own
byte-identical file copy; synthetic keys travel only through parent/child memory
and stdin, never arguments, files or output. Byte buffers are wiped; complete
erasure of every transient managed string copy is not claimed.

Each fresh child binds the original model and audits its protected source before
one first validated journal call. DbContext construction is outside the interval;
query/read/validation and the same complete review serialization are inside. This
is not cold-file I/O, a whole process-start interval or an Android Home measurement.
The original query is not warmed before timing and there is no forced collection.

Seven inverse-order pairs per shape complete in each of two conditional cohorts,
56unique child processes total, actual .NET10.0.15/X64. Every pair compares the full
four-field/parsed-state output, schema/schema_version and all typed source rows;
every child preserves its source exactly. The second cohort starts only after all
first-cohort gates pass. No retune, selective rescue or successful-cohort replay occurs.

| Cohort / journal rows | Original median ms | Candidate median ms | Reduction | Original / candidate allocated bytes |
| --- | ---: | ---: | ---: | ---: |
|First /0|256.5300|68.9283|73.1305percent|6090824/47456|
|First /32|286.9056|101.9435|64.4679percent|6294712/215560|
|Second /0|251.9275|68.7295|72.7185percent|6098584/47456|
|Second /32|289.4518|101.2353|65.0252percent|6282656/216808|

All four registered gates pass: at least5percent median reduction, all-thread managed
allocation ratio at most1.01, conservative additional process peak at most64MiB.
Peak admission uses maximum candidate minus minimum original peak per shape/cohort,
including source audits; it is neither exclusive candidate memory nor a leak proof.
Native Android/JIT/AOT costs and complete startup cannot be inferred from these hosts.

## Retention, cleanup and remaining adoption boundary

Both private host builds are strict and have zero warnings/errors. Actual used
executables, native library, PDBs and dependencies are hash-copied before cleanup:
76files/54237424logicalbytes. Two standard Clean targets succeed;199previously counted
files/81095166logicalbytes are absent afterward. Encrypted fictitious inputs, source,
complete child packets and fixed gate outputs remain under
artifacts/d1172-guarded-journal-reader. No supplementary/frozen deletion occurs.

There is no app/Windows-product/full-shipping build, emulator/phone/Main command,
original fault/register/SDK trace read, runtime/GC change, old matrix replay,
authentication or financial Save/Import/Undo. This result admits a promising host
component only. A new fixed actual Android complete-pipeline protocol, relevant
remaining source-lifetime/transaction conditions, full-source adoption and the
unchanged latency/allocation/memory gates are still required before any production
change. D1167 remains terminal; its unissued timing roles are not resumed or replaced.
The original native fault, two-second whole-load goal and broader security,
accessibility, platform/provider/Store/release gates remain open. Overall delivery
continues with its full original scope.
