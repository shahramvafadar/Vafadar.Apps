# Exact KPI money and even-median results (D-859)

## Remaining observed report arithmetic

The original KPI surplus and category spending change subtract without checking;
essential coverage adds the monthly median to the nonmonthly share without checking;
loan installments add to per-currency totals without checking. Unsupported values can
therefore turn into reversed surplus/change percentages, a negative installment total
or a false "not available" essential month. Original even Median adds its two long
endpoints before decimal division: MaxValue/MaxValue returns-1, and MinValue/MinValue
returns0. These are actual failures of the existing checked-money/report definitions,
not new product scope, financial rules or a performance candidate.

Surplus, spending delta, essential-month sum and each installment aggregate now use
checked arithmetic. Percentage properties retain their original dependency on those
checked money properties; no income/nonpositive comparison still returns null without
evaluating an unused unsupported result. Median converts the first endpoint to decimal
before adding, then uses the same half-away midpoint rounding. Two long endpoints and
their half-unit midpoint fit decimal exactly; a supported median never needs to wrap.
Original odd/empty behavior and caller ordering remain. EligibleIncome and Consumption
stay unchanged: each subtracts two nonnegative supported gross buckets and therefore
already fits the signed range under original validated-source invariants.

Keep the original income/refund/reversal/transfer/capital classification, comparison
ranges, installment splitting/next-date and paid-state policy, financial calendar,
coverage history threshold, unknown status and currency partition. No stored type,
schema/model, migration, source/writer guard, UI text/layout, runtime, GC or dependency
changes. Reports' original workspace exception cover remains; source inspection of
overview/commitment/goal consumers is not relabelled as fresh native UI acceptance.

## Actual baseline and selected host cases

Initial test-only xUnit2031 refuses a filtered Assert.Single assertion; use the original
predicate overload before execution. That analyzer refusal is not a product failure.
The corrected original build runs18 new cases and exposes12 actual failures: two
surplus signs, one dependent surplus rate, two spending-delta signs, one dependent
change percentage, four median boundaries, one essential-month sum and one combined
installment total. Six controls already pass and remain preserved.

All18 pass after correction in .NET10.0.14/X64. Three directly related existing K05/K06,
K07 and K09 cases pass: transfer/capital exclusion, essential coverage and the original
installment interest/principal example. Total21 selected host cases, not a full suite.
Original baseline Core bytes/outcomes remain. No D857/D858 successful suite or financial,
authentication or photography operation is replayed.

Inputs use nonnegative supported gross buckets, signed net-period comparisons, exact
median endpoints/half-unit ties, one valid Checking/essential category with three prior
months' expenses and two distinct nonmonthly plans, and distinct loans with valid
payment-source identities. The coverage sum is MaxValue-1 in plan shares plus monthly
consumption2 (refuse) or1 (exact supported MaxValue). The debt case combines MaxValue
and1 only inside one currency; separate EUR/GBP MaxValue totals stay supported with
the original zero-interest principal split and unknown income ratios. All are disposable
in-memory calculation inputs, not persistent financial preparation or accepted writes.

## Original Android and artifacts

On retained5606/API36/x64/kernel4096/boot6663f98e-6546-4809-a0bc-285f11a96674,
install and normally launch only fresh pro.vafadar.zanance.kpimoneyreview once,
UID10249/PID30117. Bounded initial PID/focus observation follows that same launch;
there is no relaunch, reset or test replay. Current original Onboarding remains the
root. One private check invokes all18 exact tracked case methods directly with a small
assertion adapter; it is not a native test-runner or18 additional permanent product
tests. All20 typed source components match before/after with Accounts/Entries/Schedules0.
Source SHA256:
91F71A2FD8280D708B0BF57007DA9ACFF5744285CC37301A7ECF4C80A9ED717E.
This is original calculation execution/empty-source preservation, not an arbitrary
financial-payload or Reports-screen acceptance. Private App source reverses exactly
to original MauiProgram after its sole service-container capture is removed.

Strict host/Windows/full shipping/private builds pass without warnings/errors.
A premature full-package observer gets FileNotFoundError before the live publisher
finishes; preserve that refusal and follow the same session to terminal success.
Do not restart a build, claim a package pass early or repeat the accepted native check.
Complete offline shipping SHA256
e848d7c1a786fcd2f1b8ffa1bac144d843093e1662d494c9b18bcb49229acb43,
99487647bytes,384AOT/408ELF/1608staticLOAD is prepared, not installed on Main.
Actual used private SHA256
3e9ebb0a691d8000286afd10f355f2c08fe5f78ab0dd0d113d5b46b4a9d36c56,
56889250bytes,192AOT/204ELF/804staticLOAD passes alongside shipping v2/v3, CRC,
ZIP alignment, offline/nondebuggable and original Mono checks. Nine actual own-artifact
App/Core/Data AOT objects equal their APK members; no whole-PE identity is inferred.
Current source/private source archive, original baseline bytes, signed used packages
and raw outcomes remain in bounded local evidence.

## Retirement and unchanged gates

Only freshUID10249 retires after source/PID proof. Original emulator package UIDs,
five OS values, locale-key presence, boot, geometry, reverse and shell UID match exact
preflight. Four standard Cleans pass, without XML/supplemental deletion/frozen access.
No original D809/D833 data/provider/history/trace/register/R13, intentional fault,
reset/reboot/server start/reconnect/pair, phone, genuine authentication, camera or
financial Save. Main31707/abf is only the dated D856 checkpoint. The existing saved
D831 phase evidence still lacks a completed large-case UI admission; no20-account-only
UI optimization or old rejected hypothesis is rescued. These correctness changes are
not a timing sample or whole-performance acceptance. D838 decision/D841 unexecuted
reader, original native-fault cause, security/accessibility/external/platform/release
gates and overall active goal remain separate. CI gets one read-only check after push.
