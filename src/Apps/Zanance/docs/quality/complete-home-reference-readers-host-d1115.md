# Complete Home reference readers: private host qualification - D1115

## Fixed candidate and original contracts

[D1114](current-home-query-precompilation-admission-d1114.md) does not admit
experimental query interceptors for shipping. This different candidate uses the
existing EF relational command boundary to materialize complete Account and
Category rows with typed provider getters, as existing original row readers do.
It is private source under `artifacts/d1115-complete-reference-readers`, not a
product change, migration, runtime/GC policy change or accepted Android speedup.

The preregistered plan requires all 20 Account and 13 Category persisted fields,
original null/scalar/enum/decimal/date/UTC-tick semantics and model-owned converters.
Neither account flags nor archived/missing-reference categories are filtered.
Legacy Guid.Empty is restored exactly through the original mapped Id setter,
without calling the rejecting identity constructor or substituting a new ID.
Both readers retain original EF command diagnostics/interceptors, caller connection,
transaction and cancellation. Immutable model-only contracts retain no context,
connection, key or data and refuse schema/filter/converter drift before SQL.
Final source/rights validation is not removed; integration is still outstanding.

## New private correctness evidence

The host references actual current Zanance.Data/Core. It builds two disposable
in-memory metadata tables through raw synthetic setup, with no financial Save,
ledger, plan, original retained-source or phone operation. Original EF no-tracking
queries and the candidate are compared after the same original managed Guid sort.
Every mapped field and ordered row is serialized and compared with the original
model's provider roundtrip. The fixture includes nulls, decimal values, Unicode,
enum values, date boundaries, UTC timestamp conversion and a legacy empty ID.

Twelve new comparisons cover counts0/1/20/200 in en-US/de-DE/fa-IR. Full packets
and raw source before/after are exact, no row objects are shared with the baseline,
no tracking appears, pre-cancel issues no SQL and injected command refusal propagates.
Both original and candidate command observers see the same actual transaction.
Eight further new contract conditions reject missing/extra fields, renamed Id and
query filters for both types without SQL. These are private checks, not new product
unit tests or proof of every converter/interceptor/reader-failure combination.

The initial strict host build fails IDE0040 on two helper type declarations;
explicit accessibility fixes only that failed build. An expected-value setup is
also clarified to use the original model's provider roundtrip, including UTC ticks,
before any correctness execution. The twelve successful comparisons are not
replayed after adding the eight drift conditions; candidate reader source remains
unchanged. Evidence therefore has explicit host-binary lineage rather than a claim
that all20 conditions ran on one final binary. Both subsequent strict builds pass
without warnings. No successful old suite or product build runs.

## First-use component measurements

The source/plan digests are frozen before timing. Each role uses a fresh process
on .NET10.0.15/X64, an identical deterministic metadata fixture and initialized
original model/connection outside the timed boundary. Seven pairs alternate role
order for each shape. Each measures the first complete two-table read plus the
unchanged managed Guid ordering; full packets and source audits follow the timing
and allocation boundary. There is no warm-query benchmark disguised as cold use.

| Accounts / categories | Baseline median | Candidate median | Median paired latency ratio | Median paired allocation ratio |
| --- | ---: | ---: | ---: | ---: |
|20 /20|179.2105ms|25.0532ms|0.1407601|0.0213927|
|200 /20|177.8118ms|26.1812ms|0.1430408|0.0447158|

Both fixed gates pass: at least5% median paired latency gain, allocation ratio
at most1.01 and additional process high-water at most64MiB. Maximum candidate-minus-
baseline high-water is -14946304/-14905344 bytes for these shapes. These are whole
private process high-water observations, not exclusive retained-heap or leak proof.
All28 fresh role processes preserve exact full packets, raw source and transaction.
No selective shape, rescue or retuning follows.

This isolates first-use materialization cost after model/fixture setup. It is not
a whole process-cold launch, the original 100k encrypted workload, Android/ARM64,
shipping package, private Main equality or two-second Home acceptance. Other
original consumers can still incur their EF compilation cost; only an actual
integrated original Home comparison can establish a useful end-to-end improvement.

## Preservation and next required work

Standard Clean succeeds and retires172 files/87763792 logical bytes;64 build
files/1275381 bytes remain. Used private Host.dll/PDB, exact candidate inputs,
initial failure, verification packets and all paired outputs remain. There is no
supplemental deletion, protected benchmark read, frozen-scope action, new AVD,
phone input, authentication or accepted financial/test replay.

Next integrate this unchanged candidate into a separate private original Home
review with all original snapshot/rights/publication checks. Bind actual compiler
inputs and package policy before using the same retained source without reset.
Preregister original Android read and whole-ready boundaries, source equality and
fixed non-regression gates; do not adopt a host-only gain. Shipping remains unchanged
and the full goal remains active with original fault/performance/platform gates open.
