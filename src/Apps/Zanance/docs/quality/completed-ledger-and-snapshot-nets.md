# Completed ledger and saved-comparison currency nets (D-906)

## Batch defect and retained boundaries

A source audit distinguishes mixed-sign independent nets from positive-only
financial sums. Positive-only refund/allocation/planned totals retain their checked
sums; original account balance checks, source/writer/financial validation and original
per-entry duplicate-account review checks remain unchanged. No model, migration,
runtime/GC, date window, filtering, classifier or financial ownership change occurs.

Forty-nine new focused cases initially reproduce23 actual failures. The batch repairs
three production files together:

- Ledger total balances accumulate each original checked account balance in Int128
  by its original ordinal currency key, then check the completed long result.
  Valid MaxValue,+1,-1 and MinValue,-1,+1 nets no longer depend on account order.
- Unreviewed currency summaries retain the original per-entry checked sum, duplicate
  order, once-per-currency count and explicit/archived scope. Only independent
  cross-entry currency accumulation becomes wide; completed results still check
  long bounds. The old duplicate-account destination-last refusal remains intact.
- `PeriodTotals.Result` now checks its final income-minus-spending result rather
  than wrapping a genuinely unsupported derived result.
- Essential coverage sums complete category net expenses in Int128 before checking
  each complete month's long amount and applying the unchanged median/history/share
  policy. Refunds can offset other categories. Positive gross/refund fields retain
  their own original guards; this does not qualify a separate unrepresentable gross
  report merely because a requested net can fit.
- Saved comparisons finish independent opening balances, daily deltas and movement
  categories before checking their published money. Original per-entry effects,
  currency/opening/source scope, dates, metadata and kind-based spending/income/other
  classification stay unchanged. A genuinely unrepresentable compared day's balance
  now refuses instead of silently wrapping.
- Saved-minus-actual difference and the residual explanation now check their final
  money bounds. The residual uses a complete Int128 subtraction, preserving a valid
  final MinValue even when a partial subtraction would cross long bounds.

These rules retain individual-account overflow and genuine completed-currency,
period-result, compared-day, difference and residual refusals. No successful write
is replayed to provide calculation evidence.

## Host and build evidence

Actual.NET10.0.14/X64 passes all49 new cases after the23-failure baseline. Six
specifically affected old cases pass once: three duplicate-account checked-order
cases, two true unreviewed overflow cases and the original account exclusion/
individual-balance refusal. These are selected cases, not a full suite.49Android
executions below are the same49 tests on a new platform condition, not98 product
cases. Sources and complete input JSONs remain unchanged around calculations.

Strict Windows, full offline Android384AOT/408ELF/1608staticLOAD and private original-
app192AOT/204ELF/804staticLOAD pass without warnings/errors. Signed APKs pass v2/v3,
ZIP CRC, ZIP alignment, nondebuggable and offline checks. Both original Mono hashes
remain x64edfa02c3/ARM64f03c6356. Seven actual portable-PDB source checks bind the
three changed Core files in full/private builds and the new test source in private.
This is source/input evidence, not whole-PE equivalence, ARM64 execution or real
hardware16KiB acceptance.

The initial private observer build attempts to assign the read-only Entity.Id and
fails CS0200 before deployment. Preserve that log. Only the private fixture is
corrected: use original generated nonempty identities and order new account amounts
by their original GUID text; the original model is never changed. The corrected
private build is the one installed. There is no failed-runtime case/setup replay.

## Actual original saved comparison

Fresh API36/x64/4096 PID19937/UID10268 passes49 once after original Onboarding startup;
all20 complete source components remain exact. The private bootstrap's only change
is service capture; reversing it gives the original source text exactly. No clock,
security setting, accepted app flow or runtime mode changes.

One new fixture commits three fictitious account openings MaxValue,+1,-1 and one
saved forecast definition with two MaxValue points, zero ledger entries/schedules.
It does not call financial Save/Import/Undo, post a payment or reproduce previous
account/transaction setup. After that necessary new metadata source is captured,
the original DI SnapshotPage receives its original typed id query and root ownership.
Its original worker/read/validation/publication run normally. Initial Loading/false
CanCompare is retained, not called a pass or permission failure.

The same19937 then reaches Ready/no failure/CanCompare/row-found, with two original
comparison rows. Both saved/actual texts are the exact EUR representation of
long.MaxValue (`92,233,720,368,547,758.07 EUR`), both differences zero. The first three
visible native TextViews match their complete source text and have ellipsis0. Their
visible right edge is1pixel shorter than their view bounds; retain that observed
limit rather than claiming full-rectangle/glyph equivalence. Lower offscreen captions
are not accepted or recaptured. Original page/layout is unchanged.

All20 typed source components after read/publication and at terminal observation
retain SHA
`B6D3BCDDAD83CECA9AD8159C52B70B5F6829C46550477FFE5B66DEB0879ACCB8`.
One original owned-window XML35746bytes is SHA-bound and removed remotely, with raw
local evidence retained. This is a controlled original page, not a real owner tap,
physical Main/private DB/key/whole-schema or complete accessibility acceptance.

## Retirement, artifact and CI frontier

Only fresh UID10268 is uninstalled. Original package inventory/five OS values/
locale-key presence/boot/geometry/reverse match exactly. The retained Q02 package
returns normally in the same PID15609 without a source/cold/performance replay or
new qualification; retained large-Budget data/evidence remain untouched. There is
no phone command. Main10611/9f8f480d remains only lastD903 observation, not fresh
liveness. Full shipping SHA
`16b7b88955e555b94bf7175a00ba4c79466f2cc448ef436ac173db8ad3c9c8de`
is prepared and not installed on Main.

Four scoped standard Cleans pass. Current used Core DLL/PDB files are archived
before Clean; actual source-binding receipts, used signed APKs, private sources,
raw XML and host/native output remain. No supplementary/frozen action occurs.
D905's rejected prefix jump is not reused, retuned or rescued.

The single D904 CI response reports
[CI42156056850 cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42156056850)
and [backend42156056739 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42156056739),
[service42156056715 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42156056715),
[CodeQL42156056681 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42156056681).
Cancellation is not a code-failure diagnosis or evidence of its actor/cause.
The single D905 response reports
[CI42157203508 in_progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42157203508)
and [CodeQL42157203427 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42157203427).
Both exclusive markers/responses/receipts remain; never request either again or
read a successful job log/test as another qualification.

Original fault cause/repair, D838 pending authority/D841 undeployed reader, whole
performance/security/platform/provider/release gates and the overall goal remain
active/open. D607/original D752 and accepted successors remain terminal.
