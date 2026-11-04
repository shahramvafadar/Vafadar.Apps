# Exact budget read aggregation (D-829)

## Scope and original failures

Continue BUD-09/10/11/12 and ZEX-S0104 without changing source/writer, entry classification,
account/category scope, confirmed-only selection, recurrence eligibility, unknown/skipped
amount handling or published rollover choices. Five existing calculators need exact
read intermediates or checked completed results:

|Boundary|Genuine original failures|
|---|---:|
|Suggestion average: wide cross-period numerator or lost integer precision|3|
|Envelope positive/overspent parts and unassigned money|5|
|Flex open clamping, total and eligible flexible-spending difference|5|
|Known plan totals and nonmonthly monthly shares|3|
|Nonmonthly lookahead during the final Gregorian year|2|
|Surplus/deficit carry results and surplus clamping|6|

The first28 new cases have24 genuine failures and4 valid controls. Before running them,
the test helper initially assigns read-only Entity.Id; strict compile refuses at three
locations. Correct only setup to use each real fresh category identity; retain that
compile failure separately, not as a product failure. After production correction,
28 new+24 directly affected original pure cases pass (52selected). Two additional new
fractional-average controls run alone and pass, without rerunning those52 successes.
Final lineage therefore has30 new/24 related =54 distinct selected cases, not a full
suite or all54 on one final test binary. Actual host is .NET10.0.14/X64.

## Correction

Each original period's checked NetExpense remains unchanged. Aggregate nonnegative
period totals in Int128 and compute exact positive ceiling division before narrowing;
a representable average need not have a long-sized numerator. Keep original first-expense
history, zero/nonpositive-period handling, period count, currency factor and RoundUp.
Unknown plans stay unknown; known plan/monthly-share totals refuse overflow.

Envelope component sums and overspent magnitudes are checked; unassigned uses a complete
wide expression. Flex open clamps nonpositive results before subtraction, total uses a
complete wide expression, and eligible spending differences refuse out-of-range results.
The nonmonthly lookahead uses DateOnly.MaxValue inside the final Gregorian year; all
other starts retain AddYears(1). This keeps representable current/future bills without
inventing unavailable future dates or changing rule calendars/payment dates.

Carry finishes limit+received-spent in Int128, applies the original selected surplus-only
clamp there, then narrows once. A huge negative rest may legitimately produce0 in surplus
mode; deficit mode refuses when that actual carried result cannot fit. Complete expressions
preserve representable cancellation, disabled carry still bypasses calculation, and
no money moves. No schema/migration, stored/API monetary type, settlement/funding, amount
rounding policy, dependency, runtime or GC changes occur.

## Original Android and preservation

Fresh pro.vafadar.zanance.budgetaggregatereview/PID15958/UID10230 executes the30
original Core cases once without source preparation on the preserved API36/x64/4096
emulator. Original Onboarding remains; all20 typed source components are equal before/
after, SHA256 `a8a4638a310f40b8c220297facda897cda5102bf6e72a7f93179605ba379d6a7`,
and accounts/entries/schedules stay0. The private caller retains exact case bodies and
input constants, replacing only unit-test attributes/namespace and local assertion
adapters; it is not a shipping diagnostic interface. Only fresh UID10230 retires after
exact original OS/key absence/geometry/boot/packages/reverse restoration. This is original calculation
qualification, not whole budget UI/glyph/database/key/security, speed or native-fault repair.
Keep original D809 AVD/data/crash record unread and bounded R13 terminal unknown. No
physical-phone, prior financial/auth/photo or original-fault observation repeats.

## Build and retirement evidence

Strict host/test, Windows and private native builds pass without warnings/errors;
complete offline shipping publication exits0 with no warning/error diagnostic. Retain
actual used sources and signed packages before four standard Clean profiles:

|Package|SHA256|Bytes|AOT/ELF/static16KiB LOAD|
|---|---|---:|---|
|Private native|c0237a3dac83475e5d0a0c656f3492eb54b9ed2a147fe7002e9a6865842ce38c|56885154|192/204/804|
|Complete shipping|0b37aebbbe37d7acc32ed9552f340055a92cdf6272f5b335f5b5984181dd940e|99467167|384/408/1608|

Both pass v2/v3, ZIPCRC/alignment, offline/nondebuggable and original Mono hashes.
Shipping Core AOT matches its actual current artifact in each ABI and differs from
D828. Equal package length is not identity; the private authority/provider is absent
from shipping. This is not whole cross-build PE identity or hardware16KiB acceptance.
Complete0b37aebb is prepared, not installed on physical Main; last D814 remains the
historical observation. Standard Clean deletion lines: tests347, native1132, windows1172, shipping1289. No physical-byte
gain is inferred. No supplemental deletion or frozen-scope action.
Whole security/stability/performance/accessibility/platform/provider/release gates and the
continuous delivery goal remain active.
