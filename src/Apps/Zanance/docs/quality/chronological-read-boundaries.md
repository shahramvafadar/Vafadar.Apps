# Representable chronological read boundaries (D-826)

## Scope and original failures

Six original chronological read paths fail on supported dates or requested history
near the calendar origin. Add 29 new pure cases before editing production: 25 fail and
4 controls pass. Add three separate unchanged-original controls for an unrepresentable
current financial period; all three correctly refuse. The final batch therefore has
32 new product cases, not 25 or 29 plus another full suite. No financial writer, model,
schema, amount/rounding, payment, source/access guard or runtime/GC policy is changed.

| Original boundary | Genuine failing cases | Required result |
|---|---:|---|
|Inclusive last forecast day, including a two-currency transfer|4|Publish the final valid day once, without constructing its successor|
|Account movement beginning at DateOnly.MinValue|4|Opening before the first day is zero; opening money belongs inside its period|
|Rate age whose nominal cutoff precedes DateOnly.MinValue|3|Compare elapsed days, preserving the strict age threshold and financial-month exemption|
|Same-day wealth explanation on DateOnly.MaxValue|2|No intervening flow or wealth change; keep original valuation/cause handling|
|Monthly report history in the first supported calendar month|6|Return available representable periods, not invented earlier months|
|Complete-month wealth history in that first month|6|Keep today's point, with no nonexistent predecessor month|

First-month history cases cover Gregorian, Persian and Hijri calendars, each with
start day1 and28. They start within a valid selected financial period. The separate
three controls select the first calendar day with financial start28, so the selected
current financial period itself predates that calendar: it still refuses for requested
nonzero history. Clipping unavailable lookback must not silently accept an invalid
current financial period or relax synchronized complete-budget identity.

## Correction and selected host evidence

Forecast traversal uses bounded day numbers, converting only numbers inside the
inclusive input range. DateOnly.MaxValue.DayNumber+1 is still an ordinary int and is
never converted. Recorded future/opening effects, currency routing, scenario/source
selection, order and existing arithmetic rules remain unchanged; no timing claim is
made from this loop change.

Account movements use zero before DateOnly.MinValue because no account/entry can
predate it; original opening-added, period effects and closing use their existing
rules. RateFreshness compares valuationDayNumber-rateDayNumber against the positive
age, rather than constructing a possibly unavailable cutoff. Equality at the threshold
is still not old; future rates and the existing current-financial-month exemption keep
their original meaning. Rate selection, precision, quotes, retrieval and conversions
are not modified or fetched.

Same-day wealth explanations have an empty intervening-flow dictionary instead of
asking for from+1. Original start/end worth, currencies, valuation/correction causes
and remainder calculation remain intact. Requested histories stop before an unsupported
previous year using the existing calendar support predicate. A selected invalid current
financial period still reaches its original range refusal. Monthly trend documentation
now says up to the requested representable months; actual month lengths, earliest
supported dates, period naming and source-first-history filtering are not changed.

All32 new cases pass, together with26 directly affected original Forecast/Report/Rate
cases and three original wealth/price/FX cases. These61 distinct selected cases run on
.NET10.0.14/X64; this is not a whole-suite or all current-platform acceptance. The first
58-case pass is not added again when the remaining three affected cases finish. Retain
source nonmutation checks and the 25 genuine original failures. No old recurrence,
receipt, authentication, photo, financial write or unrelated performance suite repeats.

## Actual Android original calculations

Use only fresh package pro.vafadar.zanance.chronologicalreview, UID10227/PID14705, on
the existing owned API36/x64/4096 emulator. One install/launch and one original
Onboarding-root action execute exactly32 new native calculations. The private caller
links current original App/Core/Data sources, with a scoped original-services handoff;
it is not a shipping diagnostic interface.

The new product cases' bodies and input constants are retained in the private caller,
with only local assertion adapters, namespace/attribute removal and explicit AOT
source-generated input serialization. Its before/after source relation is equivalent;
no cross-platform anonymous-versus-record JSON byte identity is asserted. No unit-test
framework is added to shipping. The initial private compile rejects ignored tuple names
under strict warnings; remove only those unused names and retain the source/log before
the first install. The corrected build passes without warnings/errors, without a
successful case replay, widened guard or changed case data.

All32 native cases pass, including Gregorian/Persian/Hijri first-month histories and
all three preserved invalid-current-period refusals. Inputs are fresh fictitious Core
objects in memory. The system clock, injected time, permissions and settings are not
changed. No account, entry, plan, allocation or financial-command preparation occurs.
The original Onboarding root remains in place; accounts/entries/schedules are zero.

Before and after the action, all20 typed components (19 lists plus Settings) are equal,
SHA256 `45ab2157ac886cef67f556bdb1d471120fe6ae76288729de97a1f32fbcfc3bb0`.
This is finite original-calculation evidence, not screenshot/glyph/layout, full database
or UI date-picker/period-navigation acceptance. The process stays alive; its own exit
summary has no native-exit record. This does not reproduce or repair historical SGen.

## Build, artifacts and restoration

Strict host/test, Windows and corrected private Android builds have zero warnings/errors;
complete offline Android publish exits0 without a warning/error. Private APK
`1ddc45f8f8b398a21c481cd0656dd48455e41f711273932ef029bba53edf1e91`,
56729372bytes, has192AOT/204ELF/804 static16KiB LOAD. Complete shipping APK
`2a0a63bae25232a08818568676faa8d60a5164ebe9f4937f8fc5df344e5c342d`,
99467167bytes, has384AOT/408ELF/1608 static16KiB LOAD. Both pass v2/v3, ZIPCRC/alignment,
offline/nondebuggable and original Mono hashes. The new actual Core AOT component in
each shipping ABI differs from D825 and matches its current build artifact byte-for-byte;
equal total package length is not identity. The private authority/provider is absent
from the shipping manifest. No whole cross-build PE, hardware16KiB or phone acceptance
is inferred. Actual used sources and signed packages remain retained.

Only fresh UID10227 retires after full-source/root verification. Original OS getter
values, absent locale-key presence, geometry/density, boot, preexisting package/UID list
and reverse are exact afterward. The original emulator stays available and shared5038/
PID50972 remains. Four standard Clean profiles complete: tests347, native1187, Windows1172
and shipping1289 file-deletion log lines. Counts are not physical-byte/free-space gains;
no supplemental deletion or frozen/refused scope is attempted. No screenshot/XML capture
or physical-phone command occurs. Main is only the last D81432063/ac3a82f6 observation;
this complete package is prepared, not installed there.

D809 data/system record and terminal unknown R13 result stay preserved unread; D607/
D752 terminal and accepted successor checkpoints remain. Original native-fault remedy,
whole performance/two-second/security/DB/key/A11Y/platform/provider/Store/release gates
and the continuous delivery goal remain open. No completed cohort is a reason to stop
independent work, repeat accepted financial operations or poll a prior CI response.

Related: [earlier final-calendar ranges](calendar-range-endpoints.md),
[goal projection limits](goal-contribution-boundaries.md),
[current frontier](remaining-delivery-evidence.md).
