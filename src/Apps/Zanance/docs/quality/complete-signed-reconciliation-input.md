# Complete signed observed-balance input (D-912)

## Observed failures and correction

The original reconciliation session removed a leading minus and parsed the remaining
magnitude as positive money. A valid observed balance of `long.MinValue` consequently
failed before comparison: its positive magnitude cannot fit in Int64. Six new
original application/session cases reproduce FormatException across en/de/fa,
EUR/JPY/KWD and a captured EUR display unit. Separately, five new Core cases show
that direct ReconciliationResult.Difference could wrap for unrepresentable signed
differences. This is11actual failures in18baseline cases, not34baseline failures.

The existing operational ReconciliationReadReview already rejects an unrepresentable
correction before consent. That safeguard is retained unchanged; no previously
accepted incorrect financial write is claimed. The public Core difference now uses
checked subtraction as well. A valid difference of Int64.MinValue remains readable,
while a positive adjustment magnitude that cannot fit still refuses.

MoneyAmount now shares one original grammar with a separate explicitly signed-balance
entry point. It applies the sign before the checked minor-unit conversion and uses
the asymmetric negative bound. MoneyText shares original explicit-unit scaling and
precision; MoneyInputContext retains the immutable currency/culture/unit parser.
Ordinary positive money APIs still reject signs and the extra negative magnitude.
No guessed separator, precision relaxation, rounding, currency fallback or changed
financial writer is introduced. AccountReconciliationSession uses that captured
signed parser and retains its revision/unit/file/consent ownership checks.

## New and affected host evidence

On.NET10.0.14/X64,12new difference cases,16new signed-parser cases and6new original
session cases pass. Application cases keep the full original raw text and source,
return difference−1 and create no ledger entry/reconciliation metadata. These are
34new product cases, not62with native execution. They do not constitute a full suite.
Directly affected22positive-parser/old input-boundary,9captured-context and1session
unit-refusal cases also pass. Unrelated old suites and financial UI successes do not
replay.

An initial application runner starts before its build produces the DLL and refuses;
it executes no test. The actual baseline run follows the same completed build. An
initial combined class/method filter selects zero cases and is disqualified. Only
that empty selection is corrected; the accepted unit-refusal case is not repeated.
Both observation failures and their outputs remain retained.

Strict test, Windows and full Android builds succeed without compiler warnings/errors.
The full signed offline nondebuggable APK is99491743bytes, SHA256
`b90c2d6de61c3cb8a9f84012e31918f3eb633475a153500e434d879c63e9313d`.
Its384AOT/408ELF/1608staticLOAD/v2v3/CRC/ZIP16KiB checks pass. It is prepared, not
installed on Main. Twelve actual portable-PDB source checks bind four Core files in
native/shipping, original session in both and both Core test sources in native.
This is source-input identity, not whole-PE equivalence. Eight used Core DLL/PDB
files remain archived.

## Native calculation and original form continuation

Fresh API36/x64/4096 UID10270/PID21756 executes the28new Core cases once with complete
20part source unchanged. Independent full-source native APK SHA256
`487919eb14e5c36f3e65ee3d51d45d6df31ea5e4dcf2cfdc53e835642dbde8ef`,56766236bytes,
passes192AOT/204ELF/804staticLOAD, original Mono, offline/nondebuggable/v2v3/CRC/ZIP.
Its only declared bootstrap service-capture line reverses exactly to original text.
The private diagnostic endpoints/test shims are not part of the shipping package.

One new fictitious Checking account has opening−9223372036854775807minorEUR; no
goal, schedule or ledger entry is prepared. Original AccountDetailPage becomes
Ready in the same process. Initial Loading packets are not accepted as Ready.
The controlled root is original page integration, not an account editor/navigation
acceptance or a repeated historical financial fixture.

The first private input driver refuses with InvalidOperationException before any
text or Compare action. Its input/compare flags remain false. Saved hierarchy has
multiple scroll wrappers and an off-viewport field; neither a unique-scroller nor
a successful managed scroll is inferred from that failed driver. A native-selection
attempt also refuses before its gesture because it initially assumes one ScrollView.
Selecting the actual scrollable vertical body from the same XML corrects only that
observation. One ordinary native scroll brings the original field into the viewport.
No package upgrade, case/setup replay or marker deletion occurs.

The actual field has a907x116px screen target. One native tap/text injection enters
`-92233720368547758.08`; the shown owned IME closes with one Back, retaining the page.
The model's original ObservedText is exact. Accessibility XML exposes a combined
caption/value packet, so the initial bare-value XML assertion is disqualified; that
same captured XML is checked against the exact caption plus raw value. No text is
entered again and no bare native Editable.Text identity is claimed. An initial
text-mode XML transfer is retained; raw bytes of the same capture are hash-bound.

One visible original Compare button,907x126px, receives exactly one native tap.
Original CanAdjust becomes true with no error, recorded
−92,233,720,368,547,758.07EUR and difference−0.01EUR. A normal feedback scroll exposes
the exact original result text inside the viewport at[87,896][994,984]. The failed
private driver's flags remain false and its caption array remains empty: independent
native action receipts, original model and exact screen XML prove this continuation.
No new line/ellipsis/glyph or full keyboard/culture/scale matrix proof is invented.
Adjust, reason entry, financial Save and a matching-balance metadata update never run.

Complete20part source remains equal through terminal comparison, SHA256
`962BE61242E47BF5FFF6862A92F76E8773A0B19F11B989BA2FD15AEDA22EB2E7`.
Entries stay0 and LastReconciledOn stays null. Only fresh UID10270 and four bound
remote XMLs125232bytes retire. Local raw/refusal evidence stays. Original public
packages, five OS values, locale-key presence, boot/1080x1920/420geometry/reverse
are exact; retained Q02 returns normally in the same PID15609. No phone or Main
private source/key/auth/photo operation occurs.

## Cleanup and open conditions

Five standard scoped Cleans remove29155files/2278277410logicalbytes. Final unique
roots retain3710files/590158472bytes; the shared test root is not counted twice.
No supplemental deletion or frozen scope is attempted. Used signed packages,
current sources/compiler files and actual outputs remain.

D910's sole response records
[CI42166290469 cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42166290469)
and [CodeQL42166290333 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42166290333).
D911's sole response records
[CI42167085076 in_progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42167085076)
and [CodeQL42167085046 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42167085046).
Markers/raw responses/receipts remain. Neither receives a second request or an
inferred later conclusion.

Current28Core ARM64 and prepared Main adoption remain separate missing conditions.
Main22204/37518cfa is only the last D910 observation. Original fault remedy,
D838pending/D841undeployed, whole performance/security/accessibility/platform/
provider/release gates and overall goal remain open; terminal cohorts stay terminal.
