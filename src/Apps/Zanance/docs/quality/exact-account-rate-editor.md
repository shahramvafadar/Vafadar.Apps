# Exact account-rate editor (D-843)

5 November 2026. Preserve an existing decimal loan/lent rate through the original
account form, including its native two-way Entry binding. No financial posting,
accepted authentication, camera or old successful workflow is replayed.

## Confirmed defects and correction

The old Load format `0.##########` rounds stored rates to ten fractional digits.
For example, `0.9999999999999999999999999999` becomes `1`, and `1e-28` becomes `0`.
TryApply then writes the parsed displayed rate even when only the name was edited.
The native Entry also had MaxLength 8, so changing the formatter alone is insufficient.

A distinct parsing defect occurs before the range check: Decimal.TryParse can round
excess significant fractional digits or a tiny positive value. Correction from D845:
`100.00000000000000000000000001` is still exactly representable and was already
refused by the existing maximum-100 check; it is a range control, not proof of parser
rounding. With one more fractional zero before its final 1 (100 + 1e-27), the framework
really rounds down to 100. D845 records that separate original-goal failure/control.
The actual D843 Android above-100 refusal is consequently a range control; its new
loaded native text/name-only memory precision checks and host rounding failures remain.

Load now uses the culture's lossless default decimal fixed-point representation,
retaining the stored scale. The native Entry permits 32 characters, sufficient for a
canonical stored decimal rate and optional percentage suffix; this is an input bound,
not a new business precision rule. Parsing still normalizes the original culture's
separator, supported digit scripts and percentage suffix. After the same unsigned
plain-decimal parse and existing maximum 100 check, canonical fixed-point meanings
must match exactly. Only redundant leading integer/trailing fractional zeros are
removed for that comparison. No floating-point epsilon, truncation or guessed rate.

The account type/range, money parsers, historical principal sign, rate culture/revision,
writer/consent/ownership and financial policies remain. No Core/Data/schema/migration,
compiled model, runtime/GC or third-party dependency changes are required.

## Selected host evidence

One initial test-build refusal for required Account initializer members is preserved;
it is not a product test failure. The corrected strict baseline builds without warnings.
On unchanged original form code, 29 new cases expose 22 failures: 18 loaded-format
cases and four inexact-input cases. The fifth inexact control already refuses.
All six exact-input syntax controls already pass. These baseline format assertions
stop before TryApply; no old native name Save is claimed to have been executed.

After correction all 29 pass on actual .NET 10.0.14/X64: six cultures times three stored
rates, five inexact-input refusals and six exact syntax controls. Four directly affected
existing loaded-debt sign/installment/display-change cases also pass. This is 29 new
plus four affected selected cases, not 33 new tests or a full suite. The new original-form
cases mutate only memory; related host metadata writes create no ledger posting.

## Original native editor, no Save

Fresh API36/x64/4096 PID21943/UID10239 directly includes the original App/Core/Data,
AccountEditorPage/XAML/ViewModel/Form. The only private MauiProgram difference is one
reversible DI capture, compared with explicit line-ending normalization. Prepare one
new fictitious loan metadata account with opening 0, rate just below 1 and installment
600; no cash account, entry, schedule, payment or accepted original Save is created.

The original reader reaches Ready. Initially the rate lies below the viewport, so it
is not accepted as visible. One original ScrollView scroll brings its native EditText
into view at [87,971,530,1087]; actual text and Form.RateText both retain the complete
30-character rate and native MaxLength is 32. This proves bound native text retention,
not simultaneous visibility of every glyph in a horizontally scrolling input.

A name-only TryApply on a detached memory account preserves every decimal bit. It is
not a native Save/DB-writer acceptance. One different raw value, just above 100, then
enters through the actual EditText two-way binding and is retained exactly; original
TryApply refuses it with TermsError and no target mutation. All 20 typed source
components remain equal, SHA256
`B2FA5FAC11DE774E913A9D68B082B197CBF1336FE9152463389CECE4BBF78C56`.
This is the declared set, not whole schema/cipher/WAL/key equality or every native locale.
Return to original Onboarding, retire only fresh UID10239, and preserve the original
AVD/packages/boot/settings/locale-key presence/geometry/reverse/shell exactly.
No restart, reset, reboot, ADB server/reconnect/pair, phone or actual-exit read occurs.

## Artifacts and continuation

Strict Windows, complete offline Android and private native publish have no warnings
or errors. Nine actual App/Core/Data AOT ZIP members match their current build artifacts;
no cross-build managed PE identity is inferred. The private provider is absent from the
shipping manifest. Both APKs pass original Mono identity, v2/v3, ZIP CRC/alignment,
offline/nondebuggable and static 16KiB LOAD checks; these are not 16KiB hardware proof.

| Package | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Private native |0c12647eff13b7513518f93d5d2ef52d92ce3b3164aba1509b01f6cd3083ebb3|56876962|192/204/804|
| Complete shipping |1f2509dddeea90c50bb3be0dec66164140d4a3adc1a2684e2a370f9352629847|99471263|384/408/1608|

Four standard Clean profiles pass: tests384, Windows1172, native1282 and shipping1289
deletion lines. Used APKs/source/native observations and all initial failures remain;
no supplemental deletion or frozen scope is touched. No new XML capture is needed.
The current candidate is prepared, not installed on physical Main. The last dated Main
remains D842's5939/22a3f00a; no new liveness or private Main-source proof is claimed.

D842's sole CI observation records main CI41967026851 and CodeQL41967026840 in_progress;
no second request follows. D838's exact new-PC R13 authority remains pending, D841's
actual SDK candidate unexecuted, and old D809 terminal unknown/D833 data stay protected.
Original native-fault cause/remedy, overall performance/security/accessibility/platform/
provider/release gates and the overall goal remain open. Independent work continues.

Related: [monthly interest](exact-monthly-loan-interest.md),
[current physical Main](current-interest-arm64-main-adoption.md),
[SDK word boundary](sdk-word-category-memory-boundary.md),
[canonical backlog](../enhancements/2026-10-commercial-release/04-backlog.md).
