# D-777 - First short-text reuse rejected at host admission

## Distinct bounded premise and fixed gates

2 November2026. Examine a private block-local text-allocation hypothesis while the owner
is absent for D752. Each decoded block owns six first-value-only caches for Icon, Note,
OriginalCurrencyCode, Payee, ReimbursedBy and Title. At most128 UTF-8 bytes per slot are
retained; compare subsequent complete bytes against only that first value. A mismatch
keeps original decoding without replacing the cache. Long/null/empty values and tags
keep their original paths. Immutable strings may be shared only within that decode;
every domain row and mutable tag list remains independent.

This is neither a global string pool nor the rejected raw expansion-array scratch pool.
It does not change compression, codec-v1 envelope/32fields, physical bucket/order,
SQLite/schema/revision/writer/financial rules, source filters, runtime or GC. The private
host compiles the actual unchanged original codec/buffer files and current Core beside
a separate candidate; the production codec is never edited.

Before build/run require, on both10000/100000 repetitive inputs, at least5% median
latency and10% allocation reduction. Unique-text inputs permit at most5% latency
regression and1% allocation growth. Seven fixed alternating pairs follow fixed warmup;
the aggregate pair peak-working-set growth ceiling is64MiB. A second independent
process requires every first-process gate, with no retune/rescue on failure.

## Actual source and host sequence

Private preparation initially refuses before writing candidate files because its LF
multiline needle does not match the original CRLF source. Preserve that failure and
normalize only the private candidate text for the transformation; the original bytes
remain exact. The corrected isolated Release build passes with zero warnings/errors.
Actual first process44804 runs the already installed official.NET10.0.14/X64 host.
No global runtime/tool/SDK installation or configuration changes.

New fictitious in-memory rows cover the full32-field codec packet, ordered physical
identities,200 account identities, mixed kinds/review/source, optional fields, Unicode,
embedded nulls and duplicate ordered tags. The unique shape deliberately varies all
six text fields, including fictitious currency text; it is a codec workload rather
than a valid financial database. Original Encode produces every block once. Both
actual decoders are compared against complete row JSON outside measurement, including
physical order, before/after source digest and independent rows/tags across decodes.
Mutating one decoded tag list affects neither the source nor another decode.

These are the new hypothesis's finite host controls, not a replay of the old product
suite, financial seed/write or a claim of exhaustive malformed/mutation/cancellation
coverage. No permanent product tests are added.

| Rows / shape | Original ms | Candidate ms | Original allocated bytes | Candidate allocated bytes | Aggregate peak growth bytes | Gate |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
|10000 repeated-short-text|11.1845875|7.3770500|10839127|8849908|11976704|Pass|
|10000 unique-short-text|12.3262500|11.1190375|12279112|12286924|0|Pass|
|100000 repeated-short-text|102.0799500|88.8939500|108389776|88496176|112123904|**Reject**|
|100000 unique-short-text|103.8429000|110.6636000|122789888|122866720|146161664|**Reject**|

The100000 unique shape regresses **6.568287%**, exceeding the fixed5% maximum.
Both100000 shapes also exceed the64MiB aggregate peak-growth ceiling. That counter
covers both roles, retained results and post-timing full-JSON equality audits; it is
not exclusive candidate memory, a peak comparison between roles or evidence of a
leak. Keep this limitation rather than attributing all memory growth to the cache.
The unique latency gate independently rejects the premise. Repetitive-text gains and
lower allocated bytes do not override either failure. No second process, Android
package/run, product edit, smaller-slot retune, shape restriction or rescue follows.

## Terminal preservation and exact cleanup refusal

Original production HomeLedgerBlockCodec.cs retains SHA256
`ad6573f80ef5434540ed07b8aec49647797e93641248bb25532444fb32949e6c`.
Used original/candidate source, actual host output and all receipts are archived outside
cleanup scopes. Standard project Clean succeeds with0 warnings/errors. One supplementary
two-scope deletion is rejected before process creation with blocked by policy:

| Frozen exact scope | Remaining files | Logical bytes |
| --- | ---: | ---: |
| D:\_Projects\Vafadar.Apps\artifacts\d777-first-text-reuse |3|21841|
| E:\Zanance.ReviewBuilds\D777-first-text-build |28|406836|

Freeze both without retry, subset, alternate method, move, escalation or reuse. All
older frozen scopes and protected owner files remain untouched. No physical/virtual
device, owner authentication, private Main/key/database, financial command, shipping
build or old successful test is repeated. D752 waits for the owner; D749/D751/D607
remain terminal. Original native-fault origin, two-second/performance, security,
accessibility and later platform/provider/Store/release gates remain open; the overall
Android/crash/performance goal stays active.
