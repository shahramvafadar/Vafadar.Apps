# Candidate namespace refusal before migration key effects — D-1015

Eight actual original failures are corrected in SqlCipherGenerationMigrationSession:
an occupied candidate or rollback namespace now refuses before platform key
preparation/read. No encryption format, financial data, migration, key implementation,
integrity check, KDF, pooling, runtime or GC policy changes.

## Actual original failure

PrepareKeyAsync already requires the captured Original checkpoint and maintenance
owner. Its subsequent File.Exists check covered only candidate/rollback base files.
A directory at either base path is not a file, and orphan transaction sidecars were
not checked. Each of these eight new independent host conditions therefore called
the counting key store's PrepareAsync once and ReadAsync once without refusing:

| Existing namespace entry | Candidate | Rollback |
| --- | --- | --- |
| Directory at the base path |Original failure; corrected refusal|Original failure; corrected refusal|
| Four-byte sentinel at -journal |Original failure; corrected refusal|Original failure; corrected refusal|
| Four-byte sentinel at -wal |Original failure; corrected refusal|Original failure; corrected refusal|
| Four-byte sentinel at -shm |Original failure; corrected refusal|Original failure; corrected refusal|

The fixture creates only a real operational metadata journal and a four-byte
original-path sentinel. It never creates a financial database, actual platform key,
account, plan, entry or ledger. Original failures have selected total8/errors0/
failed8/skipped0/not-run0, with prepared1/reads1/error-none in every failure.
That is an actual call-boundary defect, not evidence that a real key was replaced.

## Correction and focused host result

Before key effects, inspect candidate and rollback with each of the original base,
-journal, -wal and -shm names. File.GetAttributes distinguishes any existing entry
from absence; only FileNotFoundException/DirectoryNotFoundException return absent.
Other metadata failures propagate. An existing object refuses regardless of file
type. The original phase/identity/owner checks, later cancellation, key validation,
buffer wipe and all subsequent export/selection/cleanup behavior stay intact.

All eight new cases pass after correction with zero key prepares/reads, unchanged
Original checkpoint, exact original sentinel and untouched namespace. Five directly
affected existing tests also pass: ordinary candidate-file refusal, retired owner
after the key await, invalid-key wipe, cancellation and foreign identity. These13
selected cases use one final strict host build across two explicit selections;
they are8new tests, not13new tests or full-suite acceptance. No successful financial
or unrelated suite is replayed. Strict host builds have zero warnings/errors.

## Actual Android boundary

A fresh API36/x64/4096 owned environment runs the same eight conditions through the
original compiled Vafadar.Data method and a real SQLite operational journal, with
counting fake keys. PID5011/UID10216/.NET10.0.14returns IOException for every
condition, prepared0/reads0 and exact journal state/bytes, original sentinel and
occupied namespace. Each individual fixture retires after its terminal observation.
One native provider request consumes an exclusive marker; no case is replayed.
This is not genuine Android Keystore, financial migration, ARM64 or16KiB acceptance.

Initial private observer compilation rejects spans across await. Before any install,
read awaited buffers first and compare them afterward. A second private build
rejects unrelated host-test .sooutputs accidentally included by the SDK's default
recursive native-library glob. Inspect the installed SDK declaration and explicitly
exclude only those own host/publish output directories from the same review project.
Both refusals remain; no failed package was installed and no passing host test was
repeated. Product source is unchanged by these observer corrections.

Final strict native package is12575139bytes, SHA256
`b7a511bfdf8a66966573c08ed0b7e04fa378497b522433a332ca31a2a29cb24e`,
74AOT/85ELF/329static16KiBLOAD, offline/nondebuggable, v2/v3signature and ZIPCRC/
alignment pass. The original x64Mono digest remains exact.

Strict Windows and complete offline Android shipping pass with zero warnings/errors.
Shipping is99614623bytes, SHA256
`69e530892ef4121e8be964a6906e7ac892d99fdd31bd0cc8ea8fffe9e7b6ca41`,
384AOT/408ELF/1608staticLOAD, original ARM64/x64Mono members exact, v2/v3/ZIPCRC/
alignment pass. Actual native and shipping compiler documents bind the corrected
session source SHA256
`8143DC829BBBEBFBF86FB2A981CB74E1D867210316F8A8AFFD9D20D2CCD2F6CD`.
This is bounded source binding, not whole-PE or private-source equality. The prepared
shipping package is not installed on Main in this cohort.

## Preservation, retirement and remaining requirements

Only the fresh package is uninstalled after the terminal result. Original five OS
values, locale/key presence, geometry, boot and reverse mapping remain exact without
setting writes. One owned console stop leaves the original four host handles
signalled; ADB5038/PID65060creation identity remains exact without server reset,
start, reconnect or pair. Official retirement removes26AVDfiles/4121704051logical
bytes. No retained5606/Q02, terminal register classification or phone action occurs.

Five standard own-output Cleans retire5583registered files/1061351608logical bytes;
2859registered files remain. Six scoped userhome files7732bytes and ten host-tmp
files10491bytes remain. Logged deletions stay inside current own output roots.
Used signed packages, original failure logs, source and compiler evidence remain;
no supplementary/frozen cleanup, alternate path or old successful operation occurs.

Main10742/9ef33c3a is only the last D1012observation, not current liveness or adoption
of this correction. Wider key/migration/recovery, private database, security,
original native-fault remedy, performance, accessibility, platform/provider/Store
and release requirements remain separate. Canonical parent States and the complete
delivery goal stay open.

The separate SqlCipherNewProfile retained-Original-intent branch still has its own
base-file check before key preparation. This correction does not silently cover
that different entry point; its current call boundary requires separate failure
evidence and remains the next independent source question.
