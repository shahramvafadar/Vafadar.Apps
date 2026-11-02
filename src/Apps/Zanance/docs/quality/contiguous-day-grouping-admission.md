# Sorted-list day grouping rejected at fallback admission (D-769)

2 November 2026. Continue independent QA06 development while the owner is unavailable
for D752 real cancellation. No phone, SDK authentication, financial write, source seed,
original accepted query, emulator or successful product suite is repeated.

## Distinct premise and frozen gate

Original EntrySearch.ByDay groups all rows with GroupBy, orders days descending, then
copies each group into an independent row array and computes its original DayNet.
The private candidate recognizes only an actual List with descending dates. It checks
that order, copies each contiguous range into an independent array and uses the same
DayNet. Other enumerables and unordered lists keep the original GroupBy body. No query,
stamp, normalization, formula, schema, persisted order, source filter or runtime changes.
This is separate from rejected parse-once search, text normalization and direct codec
output; none of those candidates is reused or rescued.

Before build/run require at least5% median latency and5% allocation reductions at
both10000/100000 sorted rows, no more than5% latency regression and no allocation growth
on late-inversion fallback, and at most64MiB pair peak-working-set growth. Seven fixed
alternating pairs follow one fixed warmup per role; a second independent process
requires every first-process gate. Reject without retuning/rescue when any gate fails.

## Actual private host result

One strict isolated Release build has zero warnings/errors. Actual process78736 uses
the existing official .NET10.0.14/X64 host, with no global installation/configuration.
Fictitious lists contain1000 days,200 accounts, EUR/USD, original row references and
ordered duplicate/Unicode tags. The late-inversion lists change only the private
benchmark ordering/multiplicity before capture. This is not a real source database or
native Transactions measurement. Preserve original and candidate complete row/day/net
equality, source JSON digest before/after, and original fallback for a deferred enumerable.

| Rows / shape | Original median ms | Candidate median ms | Original allocated bytes | Candidate allocated bytes | Gate |
|---|---:|---:|---:|---:|---|
|10000 sorted|1.242445|0.3142925|910616|397584|Pass|
|10000 late inversion|0.213075|0.237065|910616|910616|**11.25895% regression; reject**|
|100000 sorted|2.25058|1.72098|3494952|1117920|Pass|
|100000 late inversion|1.97020|2.20157|3494952|3494952|**11.74348% regression; reject**|

These are finite host observations with tiering/host variation, not an exclusive CPU
attribution or representative native improvement. Favorable sorted results do not
override failed fallback gates. No second process, adaptive threshold, alternative
sorted-only public API, native package or product adoption follows. Original tracked
EntrySearch bytes remain exact. No product unit-test count is increased, and wider
arbitrary-enumerable/mutation/concurrency or platform acceptance is not inferred.

## Preservation and cleanup

Used private source, host output, exact original source and seven-pair receipts remain
locally archived before cleanup. Standard Clean succeeds and removes49 files/11889177
logical bytes. This is not measured host free space. Automatic review rejects one
supplementary two-scope delete before process with `blocked by policy`:

| Frozen exact scope | Files | Logical bytes |
|---|---:|---:|
| `D:\_Projects\Vafadar.Apps\artifacts\d769-contiguous-days` |3|13970|
| `E:\Zanance.ReviewBuilds\D769-contiguous-days-build` |28|406892|

Never retry/subset/alternate/move/escalate/reuse those31 files/420862 bytes. Every older
frozen scope and both protected untracked owner files remain untouched. D752 owner
readiness stays pending; D749/D751/D607 stay terminal. Original fault cause, physical
performance, security/A11Y/iOS/provider/Store and release gates remain open.
