# Complete Flex bill-group titles (D-340 / A11Y-03)

## Observed failure and change

At 200% process-local Windows text scale and 360-unit width, the original English
"Non-monthly bills" caption is natively trimmed. Independent run
`3847789464cb4b7a83c582ed1d3acb74`, PID 90636, retains the actual own-window frame and
TextBlock IsTextTrimmed=true at FontSize 30. The title comes from the existing translated resource,
not an invented long test name. No budget method or financial input is saved during presentation.

The shared RowTitle default now uses WordWrap instead of TailTruncation. Font size15, semantic
AmountText color, original bindings and growing Flex group template remain intact. A source inventory
finds 27 current XAML consumers: 26 already explicitly set WordWrap; only the Flex title inherits the
changed default. Their effective setting stays identical, so unchanged successful menu/list reviews
are not repeated. A historical More comment is updated; its controls and explicit settings are unchanged.

This fixes the actual remaining inherited truncation and makes complete row identities the default
for future consumers. It does not change bill estimation, financial posting, budget method, currency,
reminder behavior, schema, SDK or translation resources.

## Focused qualification

`Run-StoreDrafts.ps1 -BudgetTitlesReview` reuses fresh independent package/storage and non-input
desktop safeguards. It presents the original BudgetPage/FlexLine template with both original
translated captions and explicitly fictitious details. These are presentation fixtures, not tests
of the Flex calculation. It never invokes a financial command or saves the budget method.

Final run `5605e9c938fe41f39dc8955093ec50bf`, PID 44160, completes 18 en/fa/de x Light/Dark x
360/412/1280 contexts. All 36 captions retain native scaling at FontSize 30, no trimming and complete
realized glyph allocations. The original account/entry/budget rows compare unchanged. Input desktop
remains Default before/after; only the independent window is rendered. Ordinary Windows identity
is restored after review.

The independent Release/AOT AndroidHome caller adds only a `budget-titles` phase over its existing
separate fictitious table fixture. Run `6d7425c5777045b4a6f2806a9cb69dbe`, comparison
`07a61e5d6b5642b28ccc8ce964ccdd94`, original PID 1753 completes six en/fa/de x Light/Dark cases,
12 captions, using actual native TextView allocations/ellipsis. Settings/accounts/entries/budgets
compare unchanged. Neither Home workload preparation nor passed report/financial suites are rerun.

An actual emulator startup ANR is retained. Activity reports a 31696ms cold launch; startup-ready
and the final six-case report come from the same original PID. Wait is selected only on its verified
current ANR dialog, without restarting the process or reseeding. This is not ANR or speed acceptance.
The completed independent process is stopped; its private fixtures remain. Only the hash-matched owned
temporary dialog XML is removed. No compiler runs during the native launch or caption qualification.

Windows scale 2 is process-local Debug stress. Android OS font_scale remains 1.0, accessibility remains
disabled, and no system setting is changed. Actual OS200%/TalkBack remain pending the separately
requested limited owner exception to AGENTS section9; no approval is inferred from elapsed time.

## Builds and complete candidate

Strict independent Windows (14.35s), independent native Android (84.57s), ordinary Windows (35.72s)
and complete offline Android publish succeed without warnings/errors. Both changed PowerShell scripts
parse. No managed unit count is added and no unchanged domain, OCR, crypto, report or backend suite is repeated.

APK `artifacts/android-budget-titles-d340/pro.vafadar.zanance-Signed.apk`, 94,789,365 bytes,
SHA256 `d81e76561da9779b955b9f548ac07314c65da2db7e7c5a85c433601a4d0b1a43`.
It contains ARM64/x64 and 384 AOT libraries, min API24/target36, without INTERNET or debuggable.
The development certificate SHA256 remains
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`.
Actual SQLCipher/Mono engine bytes in both architectures match D-339. This is not Store signing.

Main upgrades without reset. Its existing PID 2209 remains the same through warm Activity launch
(5634ms), the Preparing cover and later Home showing My account / 0.00 EUR. No financial Save or
preference write is invoked. Activity time is not loaded-Home time or cold acceptance. Broader A11Y-03,
actual OS scaling/readers, physical Android, iOS, full cold/ANR/low-memory performance, OAuth/provider,
Store and release gates remain open. Windows commercial delivery stays deferred.
