# Current holding chronology component cost (D-872)

## Descriptive question, not a reversal candidate

D869 repaired actual order-dependent checked-overflow failures. D870 reused correct
Positions in the original editor; D871 qualified the changed ARM64/public Main.
This step measures the added component cost before proposing any optimization.
Current baseline is bd6e70ea667e1716d2c905cb510e8d89e70e8cb7; product code is unchanged.
The retained pre-D869 source is normalized only into a private namespace and reverses
exactly. It is a descriptive raw-order reference on benign agreeing inputs, never a
production reversal candidate: its known incorrect boundary behavior remains disqualified.

The frozen one-process plan has12shapes:10k/100k events,1/20/200 asset types and two
source orders. Each type has one place; alternating noncash opening10/outflow9 dates
are valid under original FindConflict. No database/writer/account/ledger is involved.
Chronological order is ascending day; `id_order` is deterministic descending Id/source
order, not a claim that current reader uses descending Id. Both roles receive the same
original objects and an original AssetEventIndex built outside timing. No index cost
is included. Transfers, large boundary values, all consumers and production distributions
are not represented by this finite benign corpus.

## One actual process and preserved source

Strict private build succeeds with zero warnings/errors. Actual .NET10.0.14/X64,
PID72816, measures Quantity over every original type slice, Positions over the
whole input, and their combined invocation separately. Each role/mode/shape warms for
one second; seven pairs alternate role order, with three calls per sample. Timing and
current-thread allocated bytes exclude input generation, index construction, validation,
JSON/file output and audit mapping. There is no forced collection/runtime/GC setting change.
All14raw samples per measurement,36measurement packets and12full15-field input JSONs
are retained. Median-of-seven values are descriptive, not significance or whole-app gates.

Original FindConflict accepts every shape. Before/after measurement, complete15-field
source JSON, original reference order/multiplicity and ordered position/type-quantity
outputs agree exactly. These are12private workload audits, not new product unit tests.
Legacy/current ordered outputs are compared independently of timed checksum consumption.
Input JSONs and evidence SHA256 manifest, private source archive and used build remain
retained. Current HoldingsLedger source SHA256 is
`0db03caad08dc2251a6dd0b9fc460bcfa6eb786915ed1c4d3e1af7cd6bd0ff5f`.

## Measured component cost

Combined call medians and allocated bytes per call:

| Rows | Types | Input order | Raw reference ms | Current ms | Raw bytes | Current bytes |
|---:|---:|---|---:|---:|---:|---:|
| 10000 | 1 | chronological | 0.1328 | 1.0762 | 1064 | 2315040 |
| 10000 | 1 | id_order | 0.1273 | 1.2257 | 1064 | 2315040 |
| 10000 | 20 | chronological | 0.1433 | 1.1760 | 9624 | 2331584 |
| 10000 | 20 | id_order | 0.2526 | 1.7361 | 9624 | 2331584 |
| 10000 | 200 | chronological | 0.2670 | 1.6684 | 95360 | 2493320 |
| 10000 | 200 | id_order | 0.2679 | 1.6920 | 95360 | 2493320 |
| 100000 | 1 | chronological | 2.4601 | 19.0308 | 1064 | 19985160 |
| 100000 | 1 | id_order | 2.4334 | 25.9030 | 1064 | 19985160 |
| 100000 | 20 | chronological | 2.4282 | 17.5957 | 9624 | 20001624 |
| 100000 | 20 | id_order | 2.6780 | 21.6708 | 9624 | 20001624 |
| 100000 | 200 | chronological | 2.6126 | 17.8023 | 95360 | 20163040 |
| 100000 | 200 | id_order | 2.6550 | 22.0330 | 95360 | 20163040 |


Across all12shapes, separate modes report:

- quantity: current/raw median latency ratio 3.357–11.169; current per-call allocation 170608–1822400bytes.
- positions: current/raw median latency ratio 6.421–11.020; current per-call allocation 2144432–18340640bytes.
- combined: current/raw median latency ratio 6.249–10.645; current per-call allocation 2315040–20163040bytes.

The observed cost is material in these workloads. Current Positions constructs a
change list and ordered view; Quantity orders each already-indexed type slice. This
source observation identifies work to investigate; it does not isolate a causal share
of timings, establish a leak/retained heap, or qualify Android/whole Home/two-second
speed. Tiny raw-reference allocations make ratios particularly easy to overstate;
absolute per-call values and complete paired data are retained.

## Terminal scope and next independent work

This descriptive component-cost task is complete. No optimization is accepted and no
correctness guard is relaxed. A separate fresh candidate must preserve chronological
checked prefixes, increase-before-decrease same-day behavior, transfer legs, first-seen
position keys, source single-enumeration and independent output ownership, then meet
preregistered timing/allocation guards before another process/native/product work.
No retuning or rescue of a previously rejected performance candidate is authorized here.

No old successful host/native suite, Windows/full APK build, financial Save, photo,
authentication, Main or emulator action is replayed. Main32234/f342113f is only the dated
D871 checkpoint, not fresh liveness. No R13/D838/crash-reader execution or frozen-scope
reuse/cleanup occurs. One standard private Clean follows actual measurement termination;
no supplementary deletion. Initial nonexistent AssetEvent.cs/backlog/index discovery
lookups are lookup failures, not failed product tests or measurement results.
Original-fault/D838, correctness-preserving performance implementation and broader
security/accessibility/platform/external requirements stay open; overall goal is active.
CI is checked once after each original due time with exclusive consumed marker and
saved response, without waiting, polling, job replay or tracking automation.
