# Exact budget warning and suggestion boundaries (D-828)

## Original failures and correction

Keep BUD-06's exact80% near/above100% exceeded rules and ZEX-S0104's checked monetary
results. Original long multiplication can reverse a warning at large limits, subtraction
can wrap remaining money, and RoundUp can overflow its numerator or completed suggestion.
Use Int128 only for threshold products and the exact rounding expression; check the
completed long result and Remaining. Derive two significant digits from integer division,
without floating log/pow. Keep nonpositive amount0, whole-currency minimum step, original
currency factors, suggestions/history/source selection and explicit user Save untouched.
No schema, ledger/writer, amount-storage/API, runtime/GC, dependency or palette change.

The first20-case run reports8 failures: seven genuine original defects and one incorrect
expected threshold at long.MaxValue. Exact80% is7378697629483820645.6, so the first near
integer is646, not645. Retain that oracle failure separately and correct the two adjacent
inputs. Strict host build passes; the first combined selector is malformed and exits3
before tests, not an application failure or acceptance. A corrected class-only29-case run
has28 passes and that one oracle failure. Run only the changed alert method10cases plus
three previously unrun related status cases after correction:13pass. Final lineage has
20 new and12 affected cases passing,32distinct, not29+13 new tests or a full suite.
No successful unrelated/financial/auth/photo cohort repeats.

## Original Android calculations and preservation

Fresh pro.vafadar.zanance.budgetnumericreview/PID15671/UID10229 executes exactly20
new original Core calculations on API36/x64/4096. The private caller retains test bodies
and exact constants with local assertions; no unit-test framework or provider is shipped.
Original Onboarding remains, A/E/S0 and all20 typed components are equal before/after,
SHA256 `3649f2851611f45b4671413264a56134609bfa712a3bf9ad0224efcc4fe86e19`.
No source preparation, account, financial command, auth, photo, Retry or original crash
observation repeats. The copied preflight's fixed4November date refuses after natural
midnight before any app action. These constant-input cases do not depend on today's
date: retain the refusal, observe the natural day and preserve every actual ownership,
OS/key-absence/geometry/boot/package/reverse guard. No clock/settings write occurs.

Only fresh UID10229 retires; exact original system/key absence/geometry/boot/packages/
reverse restore. Original D809 AVD/data/record remain unread, R13 terminal unknown.
No physical-phone command or accepted-operation replay occurs. This is calculation
acceptance, not whole UI/glyph/database/key, native-fault repair or speed acceptance.

## Packages and cleanup

Strict test/Windows/private Android builds have zero warnings/errors; complete offline
publication exits0 with no warning/error diagnostic. Retain actual used sources and signed
packages before standard Clean:

|Package|SHA256|Bytes|AOT/ELF/static16KiB LOAD|
|---|---|---:|---|
|Private native|4034f5a1fc019e8f739588e045b4dd37f540eae762ff39beaed0394956008980|56872866|192/204/804|
|Complete shipping|312b0d3530c072432a500e99f4e7af304a36fa3d67e22051fdf6750d6f3517f4|99467167|384/408/1608|

Both pass v2/v3, ZIPCRC/alignment, offline/nondebuggable and original Mono hashes.
Shipping Core AOT matches its actual current build artifact in each ABI and differs
from D827; equal package length is not identity. The private authority/provider is
absent from shipping. Four standard Clean profiles complete (tests347/native1132/
Windows1172/shipping1289 deletion lines); this is no physical free-space-gain claim.
No Main installation, hardware16KiB acceptance or supplemental/frozen action.
Broader security/stability/performance/accessibility/platform/provider/release gates and
the continuous delivery goal remain open.
