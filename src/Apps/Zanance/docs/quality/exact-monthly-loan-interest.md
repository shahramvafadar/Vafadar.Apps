# Exact monthly loan-interest rounding (D-840)

5 November2026. Correct a confirmed intermediate-rounding defect in the original
monthly interest calculation, payment split and estimated repayment schedule.

## Original failures and unchanged rule

For600 minor units at annual0.9999999999999999999999999999%, exact monthly interest
is strictly below0.5 minor units. The original decimal multiply/divide first rounds
the intermediate to0.5; Math.Round(AwayFromZero) then incorrectly publishes1. The
same error can assign one extra interest unit to a split and create an unnecessary
second estimated installment for a fully covered600-unit principal.

Eight independent declared principal/rate pairs have an exact product strictly less
than600 and therefore monthly interest strictly below one half. All eight wrongly
round up on the original source. A split case and schedule case fail too. Exact ties
and a just-above-tie control remain valid. The first13 new cases expose10 failures;
a further money-range Fact, containing three checks, passes on the same unchanged
original method. Preserve that13+1 baseline lineage rather than one invented14-case
original run. All inputs are fictitious calculations with no persisted financial write.

The existing policy stays: nonpositive principal/rate yields zero; positive monthly
interest rounds to the nearest minor unit, with exact halves away from zero. Rates
and entered-payment schedules remain estimates rather than reproduction of a contract.
No editor rate limit, currency, stored money type, date,600-installment cap, principal-
transfer/interest-income-or-expense classification, writer or settlement policy changes.

## Exact calculation and selected host checks

Reuse D835's complete decimal coefficient/scale-to-monthly-fraction helper, renamed
MonthlyRateFraction because it now serves both APIs. Its arithmetic and the entire
annuity formula remain text-identical after reversing only that private name; Git LF
versus checkout CRLF initially refuses raw-byte comparison and is preserved separately.
This is a source-text comparison with only line-ending normalization, not whole PE or
raw-byte identity. No product/build retry follows that observer correction.

For positive principal n and reduced monthly rate p/q, divide n*p by q with wide integer
intermediates. Add one exactly when twice the nonnegative remainder is at least q.
Only then narrow the final rounded amount with checked Int64 conversion. A below-half
value cannot become an artificial tie; genuine out-of-range rounded interest refuses.
No decimal epsilon, floating comparison, approximate fallback or alternative money
storage is introduced. Original Split/Schedule/writer implementations call this same
corrected method; no financial replay or migration is required.

All14 new tests pass on actual .NET10.0.14/X64. Five directly affected existing pure
contracts then pass: ordinary annuity schedule, underpayment/600-month refusal,
interest-first payment split, principal-transfer/loan-interest expense and lent-money
interest income. These are14new+5affected=19selected tests, not19new or a full suite.
The old date-only zero-rate case, unrelated suites and accepted financial operations
are not repeated. The mathematical helper is shared; a new third-party dependency,
runtime/GC/configuration or schema/compiled-model change is unnecessary.

## Original Android page and complete source preservation

A fresh distinct original App/Core/Data caller on existing API36/x64/4096 uses the
current LoanSchedulePage/XAML/ViewModel directly. Only one reversible MauiProgram DI
capture is added privately; the production VM/page stays unchanged. New PID20405/
UID10237 executes16 new original scalar outcomes corresponding to the14 tests,
including one genuine overflow refusal. No D834/D835 old scalar catalog repeats.

Prepare only one new fictitious loan metadata input: opening balance-600EUR minor,
rate0.9999999999999999999999999999%, installment600. There is no cash account or
ledger entry/schedule/payment Save. The original read/worker/publication shows Ready,
exactly one row/payment600/principal600/remaining0, summary interest0, and the four
actual native name/summary/split/remaining TextViews are fully visible and unellipsized.
No refresh/retry, financial posting, camera, authentication or accepted workflow replay.

All20 typed protected-source components compare exactly before the original page and
after it, SHA2561AAF9A350E9D8F97F3BF7D241D87BCBD76BD1A164301EBB7D42AAFA99684E568.
This is the declared source set, not every SQLite table/schema/cipher/WAL/key. Return
once to the original onboarding root, then retire only fresh UID10237. Original AVD,
packages including D809 UID10216/D833 UID10234, boot/settings/locale key presence,
size/density/reverse and shellUID2000 are exact. The retained emulator/data/evidence
remain available; no root/restart/reset/reboot/server/reconnect/pair or phone action.

## Strict artifacts and retirement

Windows and complete offline Android plus the private native Release publish pass
without warnings/errors. Both actual shipping Core AOT members change from D835;
all six shipping and three private App/Core/Data AOT members match current artifacts.
The private provider/authority is absent from the shipping manifest; no whole cross-
build PE identity or actual ARM64 execution of this new interest path is inferred.

| Package | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Private native |4cb63934949138c6ff347b534c854e3a3f21a5a1f115580c2e7f3b477e37a973|56741660|192/204/804|
| Complete offline shipping |22a3f00a37f5abdeea42f1ec583b6159864a81572afa3712d492998925e8ebf5|99471263|384/408/1608|

Both pass v2/v3 signature, ZIP CRC/alignment, offline/nondebuggable manifest and original
Mono hashes; static16KiB LOAD alignment is not actual16KiB hardware qualification.
Four standard Clean profiles pass with347/1172/1289/1283 deletion lines for tests/
Windows/shipping/native. Used source/tests/APKs/logs and failures remain. No supplemental
cleanup, frozen scope, unchanged full suite or previous successful build is replayed.

The new complete shipping candidate is prepared, not installed on physical Main.
D837's Main27284/4a72373a is only its last actual public observation, not fresh liveness.
D838's actual new-PC R13 read awaits its separate bounded owner authority; no actual
record/register interpretation or old unknown-result retry occurs. D837's sole CI
response says CodeQL41955880428 completed-success/CI41955880476 completed-cancelled;
neither is queried again. D839 later has sole CI41956442681 and CodeQL41956442734
completed-success; this is the preceding diagnostic commit, not acceptance of D840.
Original fault cause/remedy, overall performance/security/
accessibility/platform/provider/release gates and the delivery goal remain open.

Related: [exact annuity lineage](exact-positive-annuity-ceiling.md),
[loan range guards](loan-estimate-numeric-range.md),
[original full schedule boundaries](complete-original-loan-schedule.md),
[current physical Main](current-physical-main-adoption-d836.md),
[new-PC synthetic controls](new-accounts-pc-r13-synthetic-boundary.md),
[canonical backlog](../enhancements/2026-10-commercial-release/04-backlog.md).


Later [D842 current ARM64/Main adoption](current-interest-arm64-main-adoption.md) qualifies the new monthly-interest
hardware condition and adopts existing22a3f00a without reset as Main5939. This is the
new dated public checkpoint; the original observations above retain their historical scope.
