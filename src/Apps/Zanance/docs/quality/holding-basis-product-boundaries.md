# Exact weighted holding basis at supported limits (D-867)

## Actual product failure and finite repair

Baseline is73b766ba. A valid opening with quantity/basis9223372036854775807 followed
by an outflow9223372036854775806 leaves quantity1 and basis1. Original RoundedShare
multiplies before dividing in decimal and fails even though that final answer fits.
The exact intermediate product is85070591730234615838173535747377725442, above the
decimal range. Six actual baseline failures cover partial/full Outflow, decreasing
Correction and Sale; original FindConflict accepts each history. Full removal should
leave0/0, partial removal1/1. Independent unbounded-integer calculations confirm both.

Only HoldingsLedger.RoundedShare changes: signed Int128 product, quotient/remainder,
nearest-minor-unit rounding with exact half-away comparison, then checked long
narrowing. The caller still supplies positive current quantity. Products of two
signed64-bit operands fit Int128; remainder magnitude is smaller than that quantity.
No partial decimal approximation or new financial rounding policy is introduced.
Unknown-basis behavior, chronology, sale references/results, price/quantity policy,
writers/models/schema/runtime/GC and original source ownership remain unchanged.
This is a correctness repair, not a latency/whole-performance claim.

## Focused host and original Android evidence

Host .NET10.0.14/X64 baseline12cases has6failures. Corrected product passes12/12.
The test's source-preservation assertion is then made reflection-free for native
linking: tuples preserve all15 event fields. Only the six changed-source-assertion
cases rerun, retaining the other six accepted cases. Therefore12host cases are in
lineage, not all claimed on one final host test assembly. Two existing affected
average-cost/unknown-opening and transfer/basis cases pass; no whole successful
suite or accepted financial write is replayed.

Final original native UID10252/PID1127 runs all12current test bodies once.
Full20-component empty source compares exactly, account/ledger/schedule0;
SHA256 BADE53883C8FA6931060AD4992A8BFBF5D929FC3C7533BEAC50BC29BABEA60C9.

Then one typed fixture transaction adds only one type/location, one valued noncash
OpeningMax and one noncash OutflowMax-1. Account/ledger/schedule/valuation stay0;
all other source collections remain exactly the initial image. The actual original
HoldingDetailPage/VM reader publishes remaining quantity0.001tokens and visible
average cost10.00EUR per token. The native TextView matches its bound caption, has
no ellipsis and full visible bounds; market value remains unknown, not zero. All20
prepared source components stay equal, SHA256 0877ED9A0BE2B75BD344468A875A5FA6E669184ECD481598F6ABFEE303338F4D.
Actual caption: `Average cost: 10.00 EUR per tokens`.
This is direct original-page hosting, not normal Shell navigation or financial Save.
No old D866 language/width matrix, authentication, photo, import or Undo is repeated.

## Build lineage, restoration and remaining requirements

Strict Windows/full offline Android and final private builds have no warnings/errors.
The first private observer build fails with CS8785 from split JsonSerializable
attributes producing duplicate StepJson hints. Those attributes are consolidated
only in the private helper before any deployment/case. Product/full shipping is not
rebuilt to rescue that observer failure. Host wildcard/source-lookup mistakes remain
lookup failures, not product defects or runtime evidence. The immediate PID lookup
initially returns no process; the same issued launch is later bound to PID1127 without
install/launch replay, then all12 cases and the new detail continue in that PID.

Complete shipping SHA256 060a3cb2fd7ea36b00dacd3d33224f8ee276f4a2fd5f5059aa689f5dbaa40f88,
99487647bytes/384AOT/408ELF/1608staticLOAD. Used private SHA256
ab8aaef91afeca2fa9ab1eaabc293dd00a6034e185337993fb14ca379ac07437,
56913826bytes/192AOT/204ELF/804staticLOAD. Both are offline/nondebuggable,
v2v3/ZIPCRC/static16KiB aligned with original Mono. Nine own App/Core/Data AOT bindings
and exact reversal of the sole private MauiProgram DI-container capture pass; no
whole-PE or hardware16KiB claim is made.

Only the fresh UID retires. Original5606 boot/package-UID list/five OS values/locale-key
presence/geometry/reverse/shell UID match exactly; no OS setting write/reset/reboot,
server start/reconnect/pair, original fault record/R13 or phone command occurs.
Four standard Cleans pass (4090 deletion lines), no supplementary/frozen action;
used signed/source/local packets remain. Shipping060a3cb2 is prepared, not installed
on Main;15014/a214 is the dated D864 checkpoint. Original fault/D838/statistical
performance/security/accessibility/external/platform/release gates remain separate,
and the overall goal stays active. D866 sole due response records both CI/CodeQL
in_progress; no second request is allowed. One marker-bound check is due after this push;
no polling or successful-job log/test replay is needed.
