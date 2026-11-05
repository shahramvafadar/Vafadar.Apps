# Exact completed forecast currency/day nets and transfer ratios (D-890)

## Actual original failures

ZEX-S0104 and FOR-01/03/10 require correct completed balances and nearest-minor
transfer effects. A completed currency balance or daily path point must fit long;
an arbitrary account/entry enumeration prefix is not a separate published result.
The previous [daily overflow protection](monetary-read-overflow.md) remains necessary:
a genuinely invalid day cannot be rescued by a later recovery.

On baseline10e93e5f an independent original-Core probe reproduces three actual
OverflowExceptions, with complete synthetic input unchanged: Max+Max-Max starting
account balances, same-day IncomeMax+IncomeMax-ExpenseMax, and transfer destination
Max multiplied by source amount Max before dividing by original Max. Each completed
expected result is Max. These are managed arithmetic failures, not native crashes.

35new selected cases then run against unchanged product code:13fail/22pass. They
cover six source permutations of positive/negative starting nets and day effects,
wide unpublished day deltas offset by the starting balance, true invalid starting
totals, large transfer ratios, half-away rounding and true invalid final conversion.
Complete input serialization/order stays exact. No database/financial Save is needed
for these pure reproductions.

## Limited correction

ForecastCalculator uses checked Int128 accumulation for the completed starting
currency and grouped day effects. Starting balance narrows once; each daily balance
narrows after adding its full day delta. Every actual published point still throws
when out of range. The same original account scope, opening/date/review rules,
currency grouping, item order, plan/occurrence/scenario and unknown-rate semantics
remain. No writer, model, schema, cache, migration, KDF or runtime/GC policy changes.

Transfer proportional amounts use the exact Int128 product/division and remainder,
then the original nearest-minor midpoint-away rule and checked long conversion. A
positive divisor is still supplied only by the original Amount>0 branch. A missing
destination rate remains unknown; no one-to-one rate or financial amount is guessed.
The two-long product fits Int128 without decimal's intermediate product limitation.

All35new cases pass on .NET10.0.14/X64 after correction.13distinct affected prior pure
conditions also pass:8missing-rate cases,4actual-day-overflow/later-recovery guards,
and1internal/out-of-scope transfer case. This is35new+13selected, not a full suite.
The final test-source serialization is source-generated for Android; native35execute
that version. Six final XML summaries change no executable/assertion text, with the
initial LF/CRLF proof refusal retained and corrected on the same archived bytes.
Accepted host cases are not rerun for documentation-only changes.

## Actual original Android calculations and ForecastPage

One fresh independent package on retained emulator5606/API36/x64/kernel4096 uses
UID10261/PID19422, the original boot and existing ADB5038. The old packages/data/
native records and retained Q02 workload are not launched/reset/adopted/reseeded.
An early StartupPage observation is not accepted as Onboarding; the same process
continues without relaunch. All35current original calculations then execute once,
with complete20typed source components unchanged and account/entry/schedule0.

One distinct protected fixture transaction subsequently creates4accounts,3future
ledger rows and1unpaid manual cross-currency transfer. This is actual fictitious
database setup, not zero writes or a replay of an application financial Save:
EUR openings Max/Max/-Max and USD0; tomorrow EUR ExpenseMax/ExpenseMax/IncomeMax;
one EUR→USD planned transfer Max/Max. No occurrence is settled or automatically posted.

The actual original ForecastPage/worker/review pipeline publishes two Ready,
interactive cards. EUR starts Max and ends -Max; USD starts0 and ends Max. All26daily
points are exact: first point is the starting balance, each later point the expected
end balance. Four EUR effects and one USD effect remain listed. Thus the original
caller exercises all three corrected boundaries together, without a what-if edit,
SaveSnapshot, financial command, authentication, camera, import or Undo.

Before/after the original read and remaining native observations, complete20typed
source components including all32ledger fields match SHA256
`F9D1C4CAA3EBCBAD1AD1DD950F10646C5220103446738E60C6434110C57F9E13`.
This does not claim the whole private database or Q02 retained-source revalidation.

The initial large AmountReadout child extends past its horizontal viewport by design;
it is not accepted as a simultaneously fully visible caption or reported as a new
layout defect. On the same page, one original horizontal gesture reaches the EUR
caption end, one vertical gesture reveals USD, and one horizontal gesture reaches
its end. Each managed/native full signed caption matches, ellipsis0 and native right
edge equals visible viewport right edge. No renderer/layout workaround, successful
old36-context matrix or fresh frame chosen to hide a raw difference is introduced.
This is finite original scrolling/text/bounds evidence, not glyph/pixel/whole A11Y.
The original Onboarding root is restored after exact source comparison.

## Builds, compiler binding and preservation

Strict Windows, focused host, private native and complete offline shipping builds
finish successfully without warnings/errors. Actual portable-PDB document SHA256
for Forecast.cs matches working source in both native and shipping builds:
`6cf01d37425779a47d47f116a58e8303c09a665b88228a972bda22801d3fbfc6`.
Three actual Core AOT members bind their native/x64 and shipping/x64/ARM64 build
objects. Original MauiProgram reverses exactly after its single private service-
container capture; original ForecastPage/Workspace/ViewModel/presentation are unchanged.

| Package | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
| --- | --- | ---: | --- |
| Used private x64 | cdf5d53efb9e8d28f84cfbe9d3e5a149351f25d894fa9e144ff858c826027ee8 | 56860444 | 192/204/804 |
| Complete shipping | 2d420ef9c424f071a1af8ff46c63a3d4187206d06215b7a938274b0f9fb60cb7 | 99286809 | 384/408/1608 |

Both pass offline/nondebuggable/v2/v3/ZIPCRC/alignment/static16KiB and original Mono
hashes. Shipping contains no diagnostic authority. Static ARM64 compilation is not
current ARM64 runtime evidence; that changed-platform condition and no-reset Main
adoption remain separate next work. Shipping is prepared, not installed on Samsung.

Only fresh UID10261 retires. All original public package records/settings/key presence/
locale/geometry/boot/reverse restore exactly, original5606 remains running. Six
standard scoped Cleans pass; no XML capture, supplemental delete or frozen action.
Used signed packages, source archive, original failures, current compiler/AOT bindings,
native text/bounds/source packets and restoration receipts remain local. No phone,
R13 reader, actual fault interpretation or accepted financial/auth/photo replay occurs.
D888single CI is consumed with CI cancelled/CodeQL success; D889single CI is consumed
while CI in progress/CodeQL success, no second GET. Original fault/D838 pending/
D841unexecuted, whole performance/security/platform/provider/release and goal stay open.
