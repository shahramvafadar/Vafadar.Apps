# Original calendar read boundaries - D-932

## Problem and resulting behavior

At the first supported Gregorian/Persian/Hijri month, original Home and several
report packages attempted to construct an unavailable previous date/month/year.
The final partial Persian/Hijri year also required nonexistent month twelve.
Reports could offer navigation outside the selected calendar, either fail or reset
the scope, and retain an obsolete comparison target.

Home now bounds its backward occurrence window and yesterday caption, and offers
only existing monthly periods. Period review uses the existing validated complete
history boundary. Debt summaries use the established zero-before-DateOnly.MinValue
opening convention; existing installment/interest/principal rules remain. Reports
reuse the original clipped year range, guard both command availability and direct
command execution, and clear comparison rows/receipts/drill targets when the named
history is unavailable. No earlier period or zero comparison is invented. The
original trailing twelve Gregorian months and actually representable raw-date
history before a selected calendar's epoch remain available. Date formatting uses
ISO identity for the narrow day-before-Hijri-epoch case instead of throwing.

No writer, schema, migration, settlement, stored amount, calendar arithmetic,
authentication/runtime/GC setting or commercial permission is changed. Existing
six-language generic missing-history text is reused.

## Failure evidence and focused host checks

Seven new Core cases cover three absent prior-month reviews, three valid first
completed-month reviews, and one earliest debt period. Baseline has four genuine
ArgumentOutOfRange failures and three passes. After correction the three failed
review cases pass; the debt case first exposes an incorrect new assertion that
principal should be11 with zero outstanding. Original LoanCalculator.Split returns
zero principal in that situation. Only that new assertion is corrected, with the
original installment11 and zero interest/principal preserved, and the one remaining
case passes. The three already accepted counter-cases are not rerun.

Five new localization outcomes cover the actual pre-Hijri-epoch raw date in all
five date styles. All pass. These are12new managed outcomes in a recorded lineage,
not a full suite or12fresh failures. No unrelated successful suite is replayed.

## Original Android batch

A fresh API36/x64/4096 cohort uses the original linked Home/Reports pages, source
workers and command implementations. Only their private review clocks are injected;
startup, authentication, commercial and system clocks stay original. One finite
fixture is prepared: two checking/loan accounts with opening0, one synthetic expense7
at DateOnly.MinValue, one income9 at the shared Hijri epoch and two unpaid plans.
No financial UI Save, settlement, import or accepted operation is replayed. This is
not an empty ledger: the two fictitious source entries are explicit.

The preregistered34contexts include ten original contexts for each calendar's first
supported day, and four partial-final-year/month contexts. Original4992/UID10216 has
20exceptions and14Ready packets; three of those Ready previous-period commands are
still incorrectly enabled. After one preserving private upgrade to6342, only the
20failures and three command defects are checked: all23Ready, exact original source,
with unavailable navigation disabled and direct execution keeping actual scope.
The other11accepted primary contexts are not replayed. Baseline success and changed
results are not represented as34observations of the final binary.

Fresh-model packets do not prove retained-row clearing. A separate registered
three-context continuation therefore makes one necessary private diagnostic upgrade
to7336, without another fixture preparation or product change. Actual original
native Next/Previous controls establish a real spending change in month two, then
return to month one: comparison rows and old target clear with explicit missing
history. Actual original twelve-month/last-month chips on each Persian/Hijri history
page establish eight real raw-date comparison lines, then clear them when the named
prior month is unavailable. The same model/source is retained within each transition.
Nine original XML captures/353120bytes are hash-matched; local originals remain.
The first XML console rendering encounters a host encoding error after capture and
hash verification; setup/capture is not repeated to hide it.

The complete20component source, including full32field ledger rows, remains exactly
002874752E71011F042C068CDE869CC2A97A882AC405A374E437BE82382F49A7 throughout.
No row-count-only substitute, cookie normalization, financial UI write or source
repair is used. Original source/compiler checks bind seven changed documents and
the four original page documents in both the retained and full shipping compiles.
An initial shipping PDB path assumption includes an absent RID directory; the actual
shared Android compile PDB is then located and verified. No whole-PE identity claim.

## Builds, preservation and limits

Strict Windows succeeds with zero warnings/errors. Complete offline shipping passes
384AOT/408ELF/1608static16KiBLOAD, v2/v3 signatures, ZIP/CRC and nondebuggable checks.
The three used private native packages pass192/204/804 with original Mono. Full APK:

`b14106820bbfb04286e9b7f755224cfcb05431d542d80c5f9e362b501b74ef0d`

It is prepared, not installed over Main and not ARM64 page acceptance. Current
source, used signed packages, compiler evidence and all original failure receipts
remain in ignored local audit storage. No tracking of private local helper paths
or files is added to repository documentation.

Original Gregorian/root and public OS/font/timeout/accessibility/TTS/locale value
and key presence/geometry/boot/reverse restore exactly. Only freshUID10216 retires;
its own console stops, and official scoped AVD retirement removes24files with
4819793147logicalbytes, zero AVD files remain. Six standard Cleans pass; Windows
assets are restored only for Clean, without another Windows build. All new roots
are under repository artifacts. No supplementary/frozen-scope removal, physical
phone input, retained5606/Q02 provider/lifecycle/native/register read or ADB reset/
reconnect occurs; existing5038/PID50972 remains.

This closes the named read-boundary batch, not every pay-cycle/calendar layout,
physical ARM64, original native-fault/ANR remedy, whole performance, security,
accessibility, iOS/provider/Store/release gate. Overall delivery remains active.

D931's sole due CI response reports CI and CodeQL completed/success:
[CI42374861169](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42374861169),
[CodeQL42374861290](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42374861290).
The response and exclusive consumed marker are retained; no second request or job
rerun follows. This is a late observation and is not called timely.
