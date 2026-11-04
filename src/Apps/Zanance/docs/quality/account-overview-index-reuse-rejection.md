# Accounts overview index reuse rejected (D-821)

4 November2026. Status: closed at the first host admission. Current AccountsViewModel
calls original Balance over the full ledger for each account, then original TotalBalances
builds its own AccountEntryIndex. The private candidate captures that same existing
index once for row balances and calls TotalBalancesFromIndex with it. Original financial
functions, index implementation, grouping/currency policy and complete input stay intact.
This is a new Accounts consumer premise, not a reopening of the rejected Home balance
retention or index-constructor candidates. It is not applied to the product.

Before build/run, freeze six shapes:10k/20 and100k/200 accounts with mixed eight-kind
entries or expenses, plus10k/1 and100k/1 controls. Seven alternating pairs use one second
warmup per role/shape;20 iterations for single-account samples,8 for other10k and2 for
other100k samples. Multi-account cases require at least5% latency gain; controls allow
at most5% regression. Every shape caps allocation growth at1%, extra retained managed
output at8MiB and timing-pair process peak growth at64MiB. Every first gate must pass
before a second process; separate original native-page gates would still be necessary.
No threshold branch, timing retune or selective multi-account rescue is permitted.

The first private generator incorrectly names seven properties absent from the actual
LedgerEntry and fails compilation before execution. Preserve that source/log; correct
the names against current source before the successful strict build. No product model
or source guard changes. Actual.NET10.0.14/X64 process54520 passes32 new private
controls, including all eight kinds, empty/single/multiple accounts, future opening,
legacy self-transfers and repeated entry references. Complete source JSON, original
entry/tag-list ownership and ordered balances/totals compare equal. These are not32
new product tests, financial saves or an actual account-page/database acceptance.

| Entries / accounts / shape | Original ms | Candidate ms | Latency change | Fixed gate |
|---|---:|---:|---:|---|
|10000 / 20 / 0|0.3738000|0.1753625|-53.086544%|Pass|
|100000 / 200 / 0|45.5449000|6.4313500|-85.879100%|Pass|
|10000 / 20 / 1|0.4164875|0.2096625|-49.659354%|Pass|
|100000 / 200 / 1|36.3737500|4.3798000|-87.958899%|Pass|
|10000 / 1 / 0|0.1667500|0.2052550|+23.091454%|Reject|
|100000 / 1 / 0|2.1300000|2.4158900|+13.422066%|Reject|

Shape0 uses mixed kinds; shape1 uses expense entries. All allocation ratios are exactly1;
the retained and timing-pair peak ceilings pass. The single-account latency regressions
alone reject admission, despite the larger-account gains. Keep all six full input JSON
files and every raw pair. They contain all32 serialized public ledger fields, not a
truncated speed fixture. Pair high-water includes both roles and is not exclusive
candidate memory, a leak estimate or a prediction of Android performance.

No second process, native performance caller, product algorithm change or permanent test
is started. AccountsViewModel, AccountEntryIndex and LedgerCalculator stay byte-exact
to their current originals; D822 separately changes only archived-row opacity. One
standard host Clean succeeds, with no supplemental/frozen action. Retain used source,
refused preparation, original inputs and measured output. Whole performance/two-second
acceptance, original native-fault remedy and the overall active goal remain open.

Related: [earlier Home-only reuse rejection](home-account-balance-reuse-admission.md),
[original sampling scope](original-home-method-sampling.md),
[separate original archived-row correction](archived-account-description-contrast.md).
