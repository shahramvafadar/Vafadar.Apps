# Exact positive-rate nominal annuity ceiling (D-835)

## Original-contract failures

D834 rejects unrepresentable estimates but retains the old floating-point annuity formula.
Twenty-nine new cases first expose19 failures: valid tiny-rate/large-term estimates can
become nonfinite, near-integral results can lose their positive tail, and large amounts
can lose minor units. One additional exact128-point grid is added before correction;
its new Fact fails the unchanged original method. Preserve the29+1 baseline lineage,
not an assertion that all30 ran in one original process or128 separate failing tests.

The independent contract is the ceiling of the same nominal annuity, with exact monthly
fraction p/q and positive principal n over m positive months:

`n * p * (p+q)^m / (q * ((p+q)^m - q^m))`.

Twenty-four finite shapes check the two exact ceiling inequalities using independently
specified rational fractions/powers, not the production interval algorithm. Two maximum-
Int32-month tiny-rate cases use Bernoulli bounds without constructing huge powers. One
integral-asymptote case proves100 < payment <=101; two further cases retain genuine
money overflow. The additional grid covers128 rate/large-money/65..600-month combinations
with rates within the original editor's0..100% rule. Higher-rate Core API controls do
not expand that editor policy. Existing inputs, stored money type and nominal formula
remain; no new rate, principal or month cap is introduced.

## Exact calculation without an enormous month-count power

Keep D834's nonpositive-rate integer ceiling. For positive rates, reconstruct the complete
96-bit decimal coefficient/scale as an exact reduced monthly fraction, without decimal
underflow from dividing a tiny annual rate by1200. All intermediates use wider integers;
only the final checked long result is public. Never store an alternate financial type.

For a positive finite-period annuity, payment is strictly greater than both n/m and
n*p/q. Their floor-plus-one provides a safe integer lower bound and early genuine
money-range refusal. For at most64 months, compute the rational ceiling directly.

The64 cutoff is a mathematical integer-result boundary, not an experimental threshold.
For coprime p,q, an integral annuity requires q|n and
`((p+q)^m-q^m) | ((n/q)*p)`. The left side is at least
`p*(p+q)^(m-1)`, so integrality requires `(p+q)^(m-1) <= n/q`.
Since p+q>=2 and positive Int64 n<2^63, the short exact branch covers every possible
integral result. Longer periods cannot make refinement straddle an exact integer forever.

For longer periods, exponentiate the inverse fraction with outward-rounded fixed-point
lower/upper bounds. Each precision pass uses exponentiation by squaring, at most31 bit
steps for positive Int32 months; no integer power has a digit count proportional to the
requested months. Start at128 bits and double precision only until both monetary ceilings
agree. The original result lies between those bounds; agreement therefore identifies its
exact minor-unit ceiling. A lower bound above long.MaxValue proves real overflow.
This is correctness refinement, not a speed candidate, floating epsilon or result fallback.

Interest/Split/Schedule, monthly interest rounding,600-installment cap, dates, transfer/
interest classification and all writers remain unchanged. The result is still a nominal
estimate, not an actual loan contract or a promise that separately rounded monthly rows
match the requested period exactly. No model/migration/dependency/permission/runtime/GC
or visible control/string/layout changes occur. The existing UI consumes entered payment
terms; this batch qualifies the changed domain entry point, not a new UI calculator flow.

## Selected host and actual Android outcomes

All30 new cases, including the128-point grid, pass on actual .NET10.0.14/X64. Only four
existing directly affected cases then run: three D834 annuity-range refusals and the
ordinary annuity/smaller-last-payment contract. All pass. This is30 new+4 affected=34
selected cases, not34 new tests or a full-suite count. No unrelated successful financial,
source, recurrence, authentication or photo case is repeated.

A fresh SDK-only caller references the actual Core project and base library, without
App/Data/MAUI/SQLite startup, original financial files, keys or a database factory. Its
hash-pinned fictitious catalog contains the corresponding157 scalar outcomes, including
128 grid inputs and exactly two expected overflow refusals. Fresh PID19379/UID10236
executes that catalog once through original LoanCalculator.Installment. All157 outcomes
match, including the two refusals, with ordered outcome SHA256
`A74126529F1F9F2142250CE6DC7241D23AC4015608EE1DF2A16A5FB14F04E0CD`.

The single measured batch is192.0046ms with822304 current-thread allocated bytes. It
includes parsing/result-list work and is not a paired benchmark, exclusive retained
heap, process-cold/UI/database/Home speed or a two-second acceptance result. No forced
collection, collector/runtime/configuration change or intentional crash occurs.

Only fresh UID10236 retires after exact original OS/key absence/geometry/boot/packages/
reverse match. D809 AVD/data/record and terminal R13 unknown, and retained D833 UID10234/
source, stay untouched; no old source rehash or native-record/register read is repeated.
Phone and accepted-operation checkpoints remain untouched. This caller creates no
financial record, performs no payment and opens no financial page or authentication.

## Build, artifacts and retirement

The first line-ending-sensitive source replacement refuses before applying its partial-
class edit; the following compiler reports missing partial. Retain that pre-test build
refusal and apply the exact intended edit before verification. The private SDK's initial
Activity/System.Diagnostics.Activity ambiguity is separately retained; alias only
Stopwatch before deployment. Neither is a runtime test failure or an extra successful run.

Corrected strict test/Windows/full offline Android/SDK publishers have no warnings/errors
and observed exit0. Actual packages:

| Package | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Core-only SDK, min36/v3 |2ff6919b02fa271e02cdcf45607bc0b4c70c7eb4d3cb9c7bd86e1deb042804ee|6012606|27/37/138|
| Full offline shipping, v2/v3 |4a72373a19d788c685b91fd8684ddf851850c12277b1a3527c7237f7d12c8f30|99471263|384/408/1608|

Both pass ZIP CRC/alignment, offline/nondebuggable and original Mono hashes. The SDK's
Core AOT matches its actual build artifact; all six shipping App/Core/Data AOT members
match actual current artifacts and both Core members differ from D834. The private review
identity is absent from shipping. These bindings do not establish whole cross-build PE
identity or execution of every method in the complete shipping app.

Actual used source, catalog, APKs and failure/passing evidence remain retained. Four
standard Clean profiles exit0 with347/1172/1289/109 deletion lines for tests/Windows/
shipping/native. No supplementary/frozen deletion or reuse occurs. D834's sole CI
response reports all four workflows completed-success; it is not this new correction's
CI and is not queried again. The current full APK4a72373a is prepared, not on physical
Main; last D81432063/ac3a82f6 is historical, not new liveness. Native-fault remedy,
whole performance/security/accessibility/platform/provider/Store/release gates and the
continuous delivery goal remain open.
