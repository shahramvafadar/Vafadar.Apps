# One-pass account effects: host admission, native rejection (D-823/D-824)

## Decision

Reject the private candidate before production. Two .NET10.0.14/X64 processes pass all
six component latency/allocation gates, but the first complete original Android
AccountsPage comparison improves only1.713514%, below the fixed5% native minimum.
Do not retune, add an account-count threshold, select a favorable order/size or replay
this cohort. The other three planned native cases are not started. Product financial
code, AccountsViewModel, runtime/GC policy and the product test count remain unchanged.
This is distinct from the closed D821 existing-index-sharing candidate.

## Original work and bounded candidate

The current original account overview scans the complete ledger for each account,
then builds its original index for the included-account totals. The new private
candidate instead captures all requested account states and makes one ledger pass,
routing each original EffectOn result to matching account states. It reuses the
original opening and totals-scope predicates; no AccountEntryIndex or new financial
formula is substituted. Repeated IDs and repeated account/entry references preserve
order and multiplicity; self-transfers route once with the original combined effect.
Future/review/opening filters, checked per-account addition, independent totals and
source/tag ownership are retained. Per-account overflow is deferred to ordered row
consumption, so a later row cannot overtake an earlier observable result/error.
Cancellation is checked during the pass and at completion.

The first host process35176 runs118 distinct private component controls:86 new and32
selected affected controls from D821, justified by this different algorithm. They
include48 kind/review/date/duplicate combinations,27 long-extreme arithmetic cases,
null/source/error ordering, deferred overflow and cancellation. These are not118 new
product tests. The second process84224 skips that completed control block. Both
actual runtimes are.NET10.0.14/X64; all six complete32-field input and ordered output
digests match across the two processes. Full input JSON and all seven alternating
pairs per shape are retained. Warmup is one second per role/shape; iteration counts
are fixed before execution, with no selective rerun.

| Entries/accounts/shape | Host1 original/candidate ms | Host2 original/candidate ms | Allocation ratio in both |
|---|---:|---:|---:|
|10000/20/mixed|0.4127625 / 0.0686250|0.4823625 / 0.0702625|0.0415611291|
|100000/200/mixed|53.5999000 / 2.7590000|43.9778500 / 2.1948000|0.0436816465|
|10000/20/expense|0.4728750 / 0.0702000|0.5402875 / 0.0736250|0.0415611291|
|100000/200/expense|33.0971000 / 1.2925000|41.2060000 / 1.5867000|0.0436816465|
|10000/1/mixed|0.1573500 / 0.0651700|0.2020900 / 0.0693050|0.0055320731|
|100000/1/mixed|2.1002900 / 0.9507650|2.5635400 / 0.9245650|0.0006947376|

Every host shape exceeds the predefined5% latency and5% allocation improvement gates;
additional retained8MiB and timing-pair peak-growth64MiB ceilings also pass. Pair peak
is a shared process observation, not exclusive candidate memory or a leak diagnosis.
This component outcome does not establish original page, database or two-second speed.
The initial readonly-field analyzer failure is retained and corrected before timing.

## New source assets and native caller

Before any native build/observation, freeze10k/20 and100k/200 shapes, both role orders,
fresh independent package per case, one source import per case followed by one no-reset
role upgrade, one untimed original publication and five named samples per role. The
native gate requires at least5% median gain at both sizes/orders and no more than64MiB
additional candidate PSS/RSS. A first failure stops admission without the other cases.
No clock, runtime or GC switch is allowed. The actual native day remains4November2026.

Two fresh fictitious SQLite assets are created once with current original Data schema:
10k/20 is4661248bytes/SHA256
`02ffd734e17c567f2ae9b1345ada97d5de81563e39033ed3662f74f23a5e23e5`;
100k/200 is42389504bytes/SHA256
`e7b5bee82d7f06b27046b7ab116fcf02bf3afcad6b6da3f994849f3ac9e1833d`.
Both integrity and foreign-key checks pass. Every account and all32 original ledger
fields are represented, including future/before-opening/review/transfer cases. The
pre-change original LedgerCalculator produces each account's numeric expected balance
and complete totals at the fixed actual day. There is no financial UI Save replay.
The100k asset is prepared but never imported on Android after the first gate fails.

An initial host provider failure happens before database creation; only the exclusive
issued marker exists. Initialize the already pinned SQLite bundle and resume only
that absent-database case, retaining the failure. The second asset has its own single
creation. Native import uses the original protected factory/interceptors, a transaction
and a bound own-asset attachment. Compare every column's name/type/nullability/key;
explicit named INSERT/SELECT preserves full rows and rowids without requiring identical
physical column order. The initial EF1002 refusal is retained. Only its validated,
fixed-table/regex-checked identifier interpolation has a documented local suppression;
no row value or broad warning/build policy is bypassed. A local line-ending preparation
guard refuses before mutation and is corrected; the same pending native work continues.

The private caller links the current original full app. Baseline uses an archived exact
original AccountsViewModel; candidate changes only local projection to capture once,
read each original row index and reuse captured totals. The private financial component
is byte-bound to the host-used source. No product file is edited. Actual per-version
private sources, signed packages and compiled Core/Data hashes are retained without
assuming that separate output-root PE bytes must be identical.

## Actual first native gate

Only fresh package`pro.vafadar.zanance.streamaccounts10ab`,UID10225 is installed.
Original10k/20 import and full typed input verification occur once. Baseline13504
publishes the actual AccountsPage and all20 native AccountRow handlers with current
original row references. Every balance/negative flag and complete total caption match
the independent pre-change numeric oracle. Each sample covers the original refresh
through complete bound native rows and two frame callbacks, excluding shell transport,
post-timing source hashes and memory observations.

One necessary no-reset upgrade to candidate13818 retains that UID and source. Adoption
is read-only, guarded by the completed first-role marker and equal full20-component
source hash (19 lists plus Settings). There is no reseeding or original application financial-command call.

| Sample | Baseline ms | Candidate ms |
|---|---:|---:|
|0|1304.2058|578.7410|
|1|581.5534|548.3482|
|2|564.5368|523.2945|
|3|593.6688|575.9375|
|4|558.4310|571.5884|
|Median|581.5534|571.5884|

The improvement is1.713514%, so the5% native gate fails. Retain sample0, including its
larger baseline duration; do not discard it, rerun either role, claim exclusive cause
or rescue the result with another size/order. Whole ready-page work includes original
source materialization, validation, formatting and native publication; the component
result is not a guaranteed proportional whole-page gain.

Baseline PSS/RSS before is376494/477856KiB and after398189/499948KiB. Candidate before
is347136/447324KiB and after361976/462708KiB. The fixed extra-memory ceiling passes;
these finite process summaries are not exclusive-allocation or leak proof. Full input,
all20 source components and original numeric results remain equal after every role's
five samples. Both roles return to their original Onboarding root. Neither original
D809 Activity/provider nor its native record is accessed. No new native death occurs;
this is not original-fault repair evidence.

Both strict native builds pass without warnings/errors:192AOT/204ELF/804 static16KiB
LOAD, v2/v3, ZIPCRC/alignment, offline/nondebuggable and exact original Mono hash.
Baseline signed APK is63955020bytes/SHA256
`fbb332f1d1b7475414984c1ad9221d3ec6a15a50a34ac22d452c4ce9af6d38d6`;
candidate63557538bytes/SHA256
`287da943389f1b52eebe42519d3b672fb78e00ea3972bdd40ab17129db96e572`.
These are private x64 callers, not new complete shipping or hardware16KiB acceptance.
Windows, full shipping, unchanged successful suites, financial/auth/photo cohorts and
the original fault reader are not repeated for this rejected private candidate.

## Preservation and continuation

Retire only fresh UID10225 after exact source/root checks. Original OS getter values,
locale-key absence, geometry, density, boot, preexisting package/UID inventory and
reverse match the captured baseline exactly. The existing D809 AVD/data/system record
remain available and unread; its R13 labels stay terminal unknown. No phone command
occurs; Main remains only the last D81432063/ac3a82f6 observation, not fresh liveness.
Four standard Clean profiles complete: D823 host, source builder and the two private
native output profiles (49/172/1192/65 file-deletion log lines respectively). This is
not a measured physical-storage byte recovery. No supplemental deletion/frozen scope,
account/auth/photo/financial replay, runtime/GC change or second CI poll is used.
Retain actual source assets, used sources/packages, pair samples and original failures.

Original fault remedy, whole performance/two-second/security/DB/key/A11Y/platform/
provider/Store/release gates remain open. Overall delivery stays active. The closed
candidate is not a reason to wait for the owner or rerun a prior accepted cohort.

Related: [closed existing-index candidate](account-overview-index-reuse-rejection.md),
[original archived-description correction](archived-account-description-contrast.md),
[current execution frontier](remaining-delivery-evidence.md).
