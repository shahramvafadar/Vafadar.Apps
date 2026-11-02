# Immutable tag-query preparation rejected at its host gate (D-801)

## Distinct observed cost and fixed scope

2 November 2026, published baseline d4a8057e. Original EntrySearch.Apply normalizes the
query once, but MatchesText evaluates text.TrimStart('#') inside every visited tag
predicate. For a hashtag search this creates the same immutable query suffix for
many rows/tags. Investigate preparing only that constant suffix once per Apply.
This is distinct from D777's rejected cache of normalized input values: no value
cache, dictionary, cross-query retention, pooled text or financial formula is added.

A private candidate retains the original complete predicate, short-circuit/callback
order, financial-kind grouping, field normalization and deferred row enumeration.
Only the constant tag query and its argument to the original text predicate change.
Actual current Core supplies the reference model, money and opening-date helpers.
Product EntrySearch/SearchText are never edited; their original file fingerprints
and EntrySearch blob5e507752a02e448ca1acba7c76ca2f7616f3ac9a remain exact.

Before the first build/run, freeze two shapes (10000/100000 entries), hash-miss,
hash-hit, plain-miss and early-title-hit cases. Require at least5% median improvement
for both hashtag cases at both sizes; controls may regress at most5%, allocation
may grow at most1%, and additional pair process working-set high-water may grow at
most64MiB. Fix six alternating pairs/two materializations per sample and one-second
warmup per role/shape. A second independent process requires every first-process
gate; native/product work additionally needs its separate original UI/source/memory
admission. No retuning, resampling, selective hit-only rescue or changed threshold.

Before any execution, correct private account construction to bind actual original
account IDs to their dictionary keys. Record two unique tag values plus one shared
value per timed row, and positive Persian half-space/digit tag contracts. No comparison
or timing had executed before this input admission; the fixed gates/samples stay.
The final private strict build succeeds with zero warnings/errors.

## Actual contracts and measured first process

Actual .NET10.0.14/X64/PID32392 completes298 private comparison contracts:288 combine
12 queries/eight filter scopes/three cultures (en-US/fa-IR/de-DE), four preserve deferred
mutation/reenumeration, and six preserve original nullable-tag outcomes/refusals.
The comparisons retain ordered object references, duplicate multiplicity, category
callback order and original field/tag-list ownership. These are private host controls,
not298 new product tests or native accessibility observations.

Both timed roles use the same full original objects/accounts; every row retains all32
field names of the guarded scalar projection. Before/after complete JSON hashes and
tag-list references match. The10000-row source is7377629bytes with SHA256
8D2AFD54BDF3EBF1FE8A391EC6A6E222AA5DB850E67DDC2AAE85D22FD3B9B6A7;
100000-row source is73967729bytes with SHA256
34FCEB1B0A2C43775A8C3CE9B1441CD3B9DA2950A47BD04EF9BD9B26AF579402.
Full raw source and pair records are retained, rather than only counts/digests.

| Entries / case | Original median ms/query | Candidate median ms/query | Reduction | Fixed gate |
|---|---:|---:|---:|---|
| 10000 / hash-miss | 2.064225 | 2.042875 | 1.0343% | **Reject** |
| 10000 / hash-hit | 1.972750 | 1.743400 | 11.6259% | Pass |
| 10000 / plain-miss | 2.122375 | 2.305700 | -8.6377% | **Reject** |
| 10000 / title-hit | 0.479700 | 0.462375 | 3.6116% | Pass |
| 100000 / hash-miss | 18.843725 | 18.273275 | 3.0273% | **Reject** |
| 100000 / hash-hit | 29.138350 | 21.663450 | 25.6531% | Pass |
| 100000 / plain-miss | 26.000175 | 24.465725 | 5.9017% | Pass |
| 100000 / title-hit | 9.385700 | 7.615150 | 18.8643% | Pass |

Three latency gates fail: hash-miss gains only1.0343%/3.0273%, below5%, and the10000-row
plain control regresses8.6377%, above5%. Hashtag-hit improvements11.6259%/25.6531% do
not rescue the rejected matrix. Allocation does improve for hashtag searches:
100000-row hash-miss98944264to86944312bytes/materialization and hash-hit97344280to87744320.
Plain/title controls add only eight bytes/materialization. All allocation and additional
pair high-water guards pass; maximum additional pair peak is3473408bytes.

Each sample performs two materializations; reported time/allocation divides by two,
then medians use the six fixed pairs. These are component samples, not complete Transactions read,
sort/group/render, Home command-to-ready, absolute whole-app peak or phone results.
High-water growth is additional to each pair's already loaded process high-water;
it is not an absolute64MiB process-memory bound. Streaming source snapshots stay
outside timed intervals. Private pre-sample collections are fixed harness controls,
not a changed product runtime/GC configuration. Exclusive causes of timing differences
(extra argument, code layout or machine variance) are unproven. Do not call this a
production regression, or combine selected gains with unrelated historical workloads.
Exit0 means the comparison completed; gate-proof AllPass=false is the admission result.

## Rejection, preservation and next boundary

Reject before a second process, native integration or product edit. No rescue or new
benchmark begins after the failed admission. The original Core/Data/App, financial
algorithms, schema/migrations, runtime/GC and product test count remain unchanged.
No database, Save/Import/Undo/payment, source/key fixture, UI/authentication, phone/
emulator/ADB action or unrelated successful test/build is repeated. The previous
bounded phone binding stays the D800 record; no new phone liveness claim is made.

Retain used private source, raw original source, complete input/output and all policy,
contract/pair/gate receipts. One standard Clean removes49 registered files/
11970289logicalbytes;28files/405873bytes remain in the new build root. No supplementary
delete or historical frozen scope is attempted. Logical bytes are not a host free-space
measurement. Canonical item states remain open: overall two-second Home, original
native fault, broad security/accessibility, legitimate PDF input, iOS/provider/Store/
release need their own original evidence. The delivery goal remains active.

Related: [rejected input-value text cache](first-block-text-reuse-admission.md),
[current execution frontier](remaining-delivery-evidence.md),
[current product resume correction](plan-resume-date-ownership.md).
