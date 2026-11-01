# Parse-once amount query admission (D-747)

The original EntrySearch.MatchesText repeats decimal.TryParse on the same immutable
normalized query for each eligible row. Privately compare parsing once per Apply,
retaining original predicate/callback order, deferred account/display-unit lookup,
original numeric styles and every money/date/filter/grouping rule. This is not a
D745 stamp, stream sink, storage/index, runtime or collector variant. Product stays exact.

Before measurement, require at least5% median numeric-query improvement for both10k
and100k inputs, no more than5% regression in missing/broad text queries, no per-row
allocation growth, at most128bytes constant extra allocation per query and0.1% total,
and at most64MiB pair peak-working-set growth. Six fixed alternating pairs follow one
warmup per side. A second process requires all first-process gates; no adaptive rescue.

Use official .NET10.0.14/X64 and the retained already-current-schema D740 encrypted
fictitious copies, original keyed factories plus query_only. No migration, new key,
financial write, source seed or device command. Key bytes zero in finally and cipher
bytes remain exact. The complete original32-field packet stamp, row reference/order/
multiplicity, day dates and per-currency net values remain exact after comparisons.
Category-name callback is the original Q02 benchmark's fixed fictitious category,
not native/localized-category acceptance. Timing includes Apply/materialization and
original ByDay, excluding database loading, native input/rendering or cold startup.

| Shape/query | Original median ms | Candidate median ms | Result |
|---|---:|---:|---|
|10k/numeric12.50|8.14845|7.68555|5.680835% improvement|
|10k/missing|6.69655|6.55330|2.139161% improvement|
|10k/broad|3.94795|4.03845|2.292329% regression, within gate|
|100k/numeric12.50|44.34850|42.11595|5.034105% improvement|
|100k/missing|33.70315|32.63200|3.178190% improvement|
|100k/broad|27.59915|29.28315|**6.101637% regression: reject**|

Matched counts are0/0/10000 and2/0/100000; complete day-group/net/row reference equality
passes. Constant allocation grows24bytes per query, inside both predeclared bounds;
largest pair peak-working-set growth18,096,128bytes. These are observed host medians,
not exclusive CPU attribution or a shipped improvement. Preserve the broad-query failure
without a second process, reordering, retuning, relaxed threshold, production patch,
native/phone run, app suite or shipping build. No permanent product-test count changes.

The first helper build fails CS4007 because an implicitly converted span crosses an
await argument boundary. Preserve it, materialize the post-read byte-array hash before
comparison and obtain a strict zero-warning/error build; no measured cohort is replayed.
Four used private source files and their hashes are archived outside cleanup targets.
Standard Clean succeeds. Supplementary removal is blocked by policy before process
creation; the exact scopes and logical residual count recorded below stay frozen,
without retry, subset, alternate method, move, escalation or reuse. They are not reclaimed
host free space. Original Main, owner PIN, terminal D607 and other cohorts remain;
QA06 and the overall goal stay open. Successful D746 native queries are not replayed.

Frozen residuals:3scopes/68files/1,324,733logical bytes:

- `artifacts/d747-parsed-search`
- `E:/Zanance.ReviewBuilds/D747-parsed-search-build/bin`
- `E:/Zanance.ReviewBuilds/D747-parsed-search-build/obj`
