# Same-JSON incremental hashing sink admission (D-741)

## Fixed hypothesis and scope

D-740 identified the later original Transactions complete-source stamp boundary.
Compare a private IncrementalHash-backed write-only stream against the actual production
CryptoStream/SHA256 Capture method, using the same original source-generated JSON metadata.
Do not replace serialization, financial fields, order, null/date/tag semantics or SHA256.
No product source or package is changed. The two already current-schema encrypted fictitious
copies are read through original keyed factories with query_only; no migration, new key,
seed, financial command, phone or emulator action occurs.

Before execution, record the admission gates: at least 5% median latency reduction in both
10,000-row and 100,000-row shapes, no increased allocated bytes, and no more than 64 MiB
working-set growth. Six fixed alternating original/candidate pairs follow one warm-up per
side. A second independent process is conditional on admission by the first process;
reject without native/product work or adaptive tuning after a failed gate. Existing native
whole-workload gates remain additional requirements, not replaced by this host comparison.

## Exact original contract admission and harness limits

Actual official .NET 10.0.14/X64 and original Data/Core source are retained. Strict private
builds have zero errors/warnings. The first private reference attempt refuses with IOException
inside the helper before publishing timing results; retain its source, result and consumed
marker. That receipt records only exception type and broad stage, so its exact failing line
is not retrospectively proven. Do not label it a product defect.

The continuation explicitly distinguishes 31 publicly settable properties from the immutable
original Id, which has a private setter. Mutate Id through the original distinct nonempty-ID
constructor, copy the other original fields and retain all 32 required field checks. Add only
bounded static phase diagnostics; no source values or exception payloads are exported.
No financial setup, original migration or successful timed cohort is repeated for that repair.

The final helper compares every emitted byte segment with the actual serializer's RAM output:
7,420,327 bytes for reference and 74,203,438 bytes for tenfold. No financial JSON is written
to disk. Per shape, all 32 separate original-field mutations change the original digest and
match the candidate exactly. Three additional original JSON edge inputs cover null/empty,
unpaired surrogates, Unicode/control text, duplicate ordered tags, extreme signed amounts
and an offset timestamp. Five byte-array/span segmented-write patterns match standard SHA256.
These are finite private admission comparisons, not new app-unit tests or broad corpus proof.
Both original source packet and copied cipher bytes remain exact during query-only work.

## Actual fixed first-process result

| Same original input | Original median | Candidate median | Latency reduction | Allocated bytes original / candidate | Gate |
|---|---:|---:|---:|---:|---|
| 10,000 rows | 25.33445 ms | 21.02495 ms | 17.0104344% | 960 / 744 | Pass |
| 100,000 rows | 106.77565 ms | 103.08040 ms | 3.4607610% | 960 / 744 | **Reject: below 5%** |

Allocation improvement is only 216 bytes per complete call. The measured pair's peak working
set is recorded, without attributing a shared process peak exclusively to the candidate.
The tenfold peak increase is 28,672 bytes. These warm stream-stamp measurements are not
Transactions read/validation/UI latency, original Home, cold startup or phone performance.
Do not combine them with D-740's independently warmed single observations.

The tenfold fixed gate fails. Close this candidate before the second independent process,
reverse/rescue, product patch, app-suite/build or native cohort. Do not lower the gate, tune
another sink variant or claim a shipped performance improvement. Production Transactions
snapshot/project file hashes remain exact.

## Retirement and remaining boundaries

Archive the three actually used final private source files, their hashes, the first refused
source, static admission plan, complete result and rejection receipt. Standard Clean succeeds
with exit 0 and an empty quiet log; do not invent its removed-file count or host space gain.
The current-schema protected copies remain intentionally retained for a genuinely changed
next investigation, without replaying their original setup or migration.

Automatic review rejects supplementary removal before process creation with blocked by policy.
Exactly these new scopes remain frozen: artifacts/d741-json-hash-source and the bin and obj
children of E:/Zanance.ReviewBuilds/D741-json-hash-build. They contain 67 files / 1,325,900
logical bytes. No retry, subset, alternate method, move, escalation or reuse is allowed.
The actually used source archive is outside these exact targets and remains preserved.

This closes one negative performance hypothesis, not QA-06, two-second performance, native
fault origin, general stability/security/database/key, accessibility, hardware 16KiB,
iOS/provider/Store/release. D-737's real owner credential and retired independent cohort,
D-607's terminal fixture and consumed reboot, and the active overall goal are preserved.
