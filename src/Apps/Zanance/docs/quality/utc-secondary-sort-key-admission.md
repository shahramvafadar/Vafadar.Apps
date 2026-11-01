# UTC secondary sort-key admission (D-755)

## Changed premise and fixed boundary

D754's actual bounded warm Home trace includes original ledger sorting and DateTime
comparison work. Inspect the actual ReadCompleteHomeLedgerAsync expression: Date
descending, then CreatedAt descending, with stable ties. This private candidate extracts
CreatedAt.UtcTicks as the secondary key once per row rather than comparing DateTimeOffset
keys repeatedly. It is not a reused index, worker, pool, Brotli or normalization candidate.

Before execution, require at least5% median latency reduction at both10000 and100000rows,
no allocation increase and at most65536KiB process working-set growth. Six fixed
alternating pairs follow one warmup per role. A second independent process, production
change or native package is conditional on the first complete host admission passing.
Do not resample or relax this gate after seeing a result.

## Original inputs and equivalence scope

Read only the declared fictitious asset from the kept D743 signed APK outside frozen
scopes. Its37273600byte complete source SHA256 is
aef54ce6b4fd62648a3034db64413291e631dd2269df8a774fb7383d54da513e.
Deserialize a disposable SQLite memory copy and extract only Id/Date/CreatedAt keys in
original descending physical order. The initial extractor's guessed LedgerEntries name
fails before output; correct it to the actual original reader's Entries table. No
successful extraction, financial operation, seed, native preparation or old test repeats.

The actual model converter is UtcTicksDateTimeOffsetConverter: persisted ticks become
DateTimeOffset(ticks,TimeSpan.Zero). The private host uses that exact conversion and the
actual linked Core LedgerEntry/Entity implementation. Only keys are transported for
this host sort contract, not a full original32-field materialization/native acceptance.
Both sorts return the same original object references in the same order, so neither
mutates other fields or replaces mutable Tags. Additional341 boundary objects include
equal instants under every15-minute offset from-14:00 to+14:00, adjacent ticks, stable
ties and timestamp/date extremes. All116281 pairwise timestamp ordering relations and
the complete reference-order equality pass. This is finite equivalence evidence, not
a new product unit-test count or general proof of arbitrary future comparers.

## First host process: rejected

Strict private Release build has zero warnings/errors. Actual isolated runtime10.0.14,
one process and fixed pairs:

| Rows | Original median ms | Candidate median ms | Reduction | Original allocation | Candidate allocation | Working-set growth |
|---|---:|---:|---:|---:|---:|---:|
|10000|2.5944|2.4731|4.6754548%|400576bytes|320576bytes|999424bytes|
|100000|23.93695|18.00365|24.7872014%|4000576bytes|3200576bytes|44392448bytes|

The10000-row latency gate fails. Preserve the actual six-pair spread and tiering/noise
limits; neither a faster tenfold result nor allocation reduction overrides the failed
fixed requirement. No second process, rescue/warmup adjustment, product/test change,
Windows/shipping build, emulator/phone command or native candidate follows. All product
source and ordinary sorting stay exactly as published. No two-second target, release
performance, physical behavior or financial correctness claim is added by this experiment.

## Cleanup and remaining work

Archive the actually used private source and fixed input/results outside cleanup scopes.
One standard Clean succeeds:49files/11856712logicalbytes removed. Automatic review rejects
the one supplementary deletion before process with blocked by policy. Freeze both exact
scopes below,30files/411151logicalbytes: no retry, subset, alternate method, move,
escalation or reuse. Logical bytes do not measure recovered host free space.

| Exact frozen scope | Files | Logical bytes |
|---|---:|---:|
|D:\_Projects\Vafadar.Apps\artifacts\d755-utc-sort-key|2|5173|
|E:\Zanance.ReviewBuilds\D755-host-admission-build|28|405978|

D754 terminal sampling and its frozen scopes remain intact. D752 still waits for actual
owner readiness before its first sensitive-operation SDK prompt; do not conflate the
accepted D751 cold cancellation with that new request. D749/D751/D607 successes are not
repeated. Overall delivery remains active with broader speed/security/stability/platform
gates unresolved.
