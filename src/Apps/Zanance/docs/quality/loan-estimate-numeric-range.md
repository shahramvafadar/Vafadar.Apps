# Loan estimate numeric range guards (D-834)

## Actual defects and bounded correction

Original zero/nonpositive-rate installments form principal+months-1 in long before
integer division. A valid positive result can therefore become negative. Divide first
and add one only for a positive remainder. Keep the existing nonpositive-rate rule.
The positive-rate annuity's explicit double-to-long conversion previously permits an
out-of-range/nonfinite estimate to become a negative amount; use checked conversion.
Individual monthly interest rows can each fit while their accumulated public total
wraps. Check that monotonically nonnegative accumulation before returning a schedule.

Keep original nominal-rate formula, monthly interest rounding, interest-first/principal
split, smaller last payment,600-row cap, dates and ledger classification. This is not
new annuity precision/conditioning policy or complete extreme-positive-rate accuracy;
those concerns remain separate. No financial writer, model/schema/migration, compiled
model, permission, dependency, visible string/layout, runtime or GC configuration changes.

## Focused host evidence

Fifteen new pure cases on .NET10.0.14/X64 first report12 genuine failures and3 valid
controls. Five positive month divisors cover principal long.MaxValue, including int.MaxValue;
the negative-rate branch retains its existing no-interest rule. Three schedules expose
unrepresentable cumulative interest and three annuities expose unrepresentable positive
payments. Valid maximum/divisible zero-rate and large smaller-final-payment controls stay
accepted. The zero-rate oracle uses independent wider-integer ceiling arithmetic.

After correction all15 new cases pass. Only three directly affected existing pure cases
run: ordinary annuity, original month-end/zero-rate behavior and underpayment/cap behavior;
each selects one case and passes. This is15 new+3 affected=18 selected cases, not a full
suite or18 new cases. No unrelated successful financial-command test is replayed.

## Actual original Android schedule error publication

Fresh pro.vafadar.zanance.loanrangereview/PID18819/UID10235 first executes the same15
new original Core boundaries once. Then prepare one new fictitious loan account through
the original protected EF factory: opening -(long.MaxValue/2), annual rate12%, installment
46116860184273880 minorEUR, opening date the actual local day. This is a real fictitious
monetary opening/loan-terms metadata write, not zero source writes or an application
payment/financial Save. Entries/schedules0, no seed or accepted-operation replay.

The original LoanSchedulePage/query/read/worker reaches exactly one OverflowException.
A private reversible callback at the original view-model catch records type/count only;
removing it recovers that original file exactly. The original translated Loan_ReadFailed
TextView is actually visible at[84,837,996,925], full visible bounds, ellipsis0. Loading
HasFailed=true/IsReady=false/IsLoading=false, rows0, name/summary null and Retry available.
No partial installment/interest total is published and Retry is not pressed.

The first observer's window-only dump selection does not establish focus. Retain its
failure, then use full window focus/focused-app/ownerUID on the same18819/10235/source.
Continue only the missing observation/proof, without reinstall, prepare, scalar or page
replay. One final all20 typed source comparison matches the post-preparation source,
SHA256 `1AE038C7C89DA765B65A9CA895D59AAA30D4FCD102177B8882DA186F166243C4`.
Restore original Onboarding and retire only fresh UID10235. All original OS values,
locale-key absence, geometry, boot, reverse and packages match, including retained
D833200-account UID10234. No XML/screenshot capture or security bypass is used.

This is one default-context original error-publication boundary, not a new multilingual/
theme/large-text matrix, every loan flow or whole database/key/cipher proof. Existing
caption/layout checks are unchanged. D809 AVD/data/record and terminal R13 unknown remain
protected/unread; no phone, real authentication, photo or native-frame read occurs.

## Strict builds and complete packages

Strict Windows and full offline Android build/publish exit0 with no warnings/errors.
The initial private caller uses a nonexistent OpenedOn property; retain that compile
refusal and correct only to the original OpeningDate before any deployment. The corrected
private native publisher exits0 without warnings/errors. A shell quoting refusal while
adding XML documentation performs no edit; the exact documentation edit is then applied.

| Package | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Private native |eaeb012a0b454322d045b9acdd6726046adef537fb4f9af1774f760d52cee233|56737564|192/204/804|
| Complete offline shipping |fbdd70f2c8e89c0d4335a4441bb077f6d21bcbfff5821ea7aa054fe191d7be54|99467167|384/408/1608|

Both pass v2/v3, ZIP CRC/alignment, offline/nondebuggable and original Mono hashes.
All six shipping App/Core/Data AOT members match the actual current build artifacts;
both Core AOT members differ from retained D830. The private provider/authority is absent
from shipping. These are finite artifact/source checks, not whole cross-build PE identity.
Retain actual used source/APKs/logs and failed observers. Four standard Clean profiles
exit0:347/1172/1289/1283 deletion lines for tests/Windows/shipping/native. No supplementary
or frozen scope is touched; deletion lines are not measured host disk-space gains.

## Remaining execution boundary

The public complete APK is prepared, not installed on physical Main. D81432063/ac3a82f6
is only the last recorded physical observation, not new liveness. D833's retained large
source remains equal; its page timing/native-fault remedy stays unfinished. The preflight
also recognizes D741's already rejected same-JSON IncrementalHash sink and does not run,
retune or rescue it. D831/D832 sole CI responses say CIcancelled/CodeQLsuccess; D833 says
CIin_progress/CodeQLsuccess at its sole observation. None is queried again or proves this
new unpushed correction. Whole performance/security/accessibility/platform/provider/
Store/release gates and the continuous delivery goal remain open.
