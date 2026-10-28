# Direct complete-ledger output rejected at its host gate (D-610)

## Measured boundary and distinct candidate

D-599's largest inclusive reader interval includes complete block processing;
it does not isolate SQL, codec CPU or GC. Current `HomeLedgerReadBlocks.TryReadAsync`
creates one physical-row list per block, copies its entries into a growing final
list, and then discards the intermediate physical identities. Investigate only
removing those intermediate lists and final-list growth. Do not repeat the rejected
primitive, tag, text-reuse, pool, shared-index, parallel-worker or runtime variants.

Freeze the current five original codec/buffer/domain sources before building.
Both private roles use actual original Encode/decoding and complete32-field rows.
Only the candidate adds an optional fresh final-entry destination to the shared
decoder loop and reserves its exact declared complete workload capacity. Existing
physical-row APIs remain; all header, compression, length, field, UTF-8, cancellation,
bucket/order and trailing-content guards remain in the same parser. No partial
projection, financial interpretation, shared mutable list or persistent format changes.
Production files, EF model and migrations are never modified.

The reference harness performs the original physical-list-to-growing-final-list
copy. The candidate appends the same complete entries to one exact-capacity final
list. Repeated blocks are a bounded host workload, not a real SQLite/Home snapshot.
Both roles retain all entries until each interval ends; native integration/fallback
publication is not inferred from this private prerequisite.

## Gate fixed before either build/run

Require at least5% combined median latency improvement and5% allocation reduction,
worst-shape latency ratio at most1.05 and allocation ratio at most1.01. These are
declared engineering eligibility criteria, not a new owner requirement or complete
Home acceptance. Each role runs once in a separate actual10.0.14/X64 process:
two512-row shapes, minimum2seconds warmup per shape, nine samples and128 complete
blocks/65536 final rows per sample. Both strict isolated builds have zero warnings
and errors. No native package, default app build or unchanged suite is repeated.

| Shape | Reference median ms | Candidate median ms | Reference allocated bytes | Candidate allocated bytes |
|---|---:|---:|---:|---:|
| Empty tags |31.0758|26.9808|41431048|39850712|
| Mixed nullable/duplicate/Unicode tags |31.6578|29.7331|44118024|42537688|

Combined host latency improves9.5956553%, but allocations decrease only3.6945719%,
below the frozen5% allocation criterion. Close the candidate at that gate without
additional samples, reverse/rescue, native qualification, product integration or
shipping/Main installation. Preserve the favorable host result without presenting
it as complete Home, ARM64, cold-start, two-second or general-memory acceptance.

Complete serialized original values/order match before and after timing, including
every repeated block. Source, row, block and separate-read mutable Tags remain
independent; original cancellation propagates. All410 recompressed truncated-row
prefixes refuse, and102 four-byte mutations plus a wrong-bucket case produce identical
complete accepted-output or exception-type/message outcomes in both roles. Their
digest is `5448b235e75d756c3a9229bd5012ba74b1e602ccc025fd05ab3cedd9b6326e00`.
These are private controls for this changed output strategy, not new shipped managed
tests or proof of SQLite fallback/atomic publication on a device.

## Preservation and explicit cleanup refusal

All five production input hashes remain exact. There is no database/key access,
financial/seed replay, emulator/phone command, new native observation or change to
D-607's unfinished fixture. The original D-606 product remains adopted; overall
delivery, performance, stability, security, accessibility and external release gates
remain open. TalkBack stays last under D-594.

Standard Clean removes all28 registered outputs/661242 logical bytes under only the
fresh private D-610 projects, with actual registered-file absence proved. Further
retirement of four new reference/candidate bin/obj roots is rejected before process
creation with `blocked by policy`. All20 residual files/423356 logical bytes remain;
no alternate command, subset, custom cleanup or escalation follows. These sizes are
logical file sizes, not measured host free space. Keep original sources, role diff,
build/run records, gate proof and refusal at `%TEMP%\Zanance-D610-current.txt`.
