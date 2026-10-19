# D-374 count transactions once in Home review notices

## Defect and correction

FIN-02 defines a transfer as one operation. Home_Unreviewed says "Transactions to review" and
FIN-12 requires a truthful count. Home previously summed LedgerCalculator.Unreviewed's currency
counts: one unreviewed EUR/USD transfer contributed one EUR group and one USD group, showing two
transactions in both the attention notice and confirmed-balance explanation.

Use UnreviewedEntryCount for the global count. Its operation-local account-id set preserves the
existing InScope predicate, explicit/default selection, destination-only and historic destination
references, dates and original entry occurrences. Repeated account records do not multiply an item;
repeated input entries are not silently deduplicated by identity. Counting never adds money.
Keep the per-currency Unreviewed summaries, ordered duplicate handling, checked totals and all
financial algorithms unchanged. No schema, preference, permission, translation, font or layout change.

## Managed evidence

The initial test fixture omitted three required CurrencyCode values; that compile failure is not
behavioural coverage. After correcting only those inputs, four tests against Home's original
expression produce two expected failures: the FX count is two instead of one, and counting valid
items unnecessarily raises the currency-total OverflowException. Replace that expression with
the dedicated counter and add scope/duplicate/source-enumeration coverage.

All 36 related Core cases pass with zero errors/failures/skips: seven new count cases, 14 retained
UnreviewedScopeIndex and 15 ReviewFix cases. The unchanged full suite is not repeated locally.
Release test output is cleaned successfully. Count-only acceptance does not weaken the monetary
overflow refusals or claim that Home can display a financially unrepresentable balance.

Core source SHA256: 77170d479a0daa655f9dd501c0295762d8053506580cbc6a186606e6f60f1cb4.
HomeViewModel source SHA256: adbe74618ebb5d433f1c0741c21bb0371c7806d55771dfe4f4831f4f32e6dd01.

## Original native publication

A distinct SQLite fixture contains two accounts, one EUR/USD transfer and the original settings.
Actual HomeViewModel, HomePage, store reads and native labels are used; no fake MAUI controls or
duplicate financial implementation. Account, entry and settings fields are serialized before/after
read-only publication. This small fixture is separate from the protected performance corpus and
from both main installations; it does not establish full main-profile database equivalence.

Windows completes 18 en/fa/de Light/Dark contexts at 360/412/1280 widths in the guarded fresh
storepreview installation. Original native captions are measured and the application's own windows
are rendered on a separate non-input desktop. Input desktop remains Default. Process88636 exits
normally; all original fixture fields remain unchanged. The resulting images are visually reviewed.

Android's first observer rejects a Persian native caption while its bound count is already one.
Retain process703's terminal managed review failure and its database. A retained-data diagnostic
continuation1541 proves the caption is visible, non-ellipsized and correctly sized, but raw text
comparison still fails in fa-Light: the original native label mapper intentionally renders Persian
digits while the bound string retains Latin digits. This is an observer defect, not a newly found
application geometry fault, timeout or native process crash.

Use the same public NativeDigits.Apply conversion and the original label direction restriction for
the native expectation; retain exact bound-text checks and full geometry checks. Keep both failure
reports. Final process1860 continues the same fixture, retaining the two already passed English
contexts from1541 and completing only fa/de Light/Dark. Six contexts pass at the unchanged actual
412dp emulator width. The completion caller has CoreMvid feff40e3-7339-41ce-9fcb-d061f4563a2f;
retained English packets belong to the preceding caller, not retrospectively to1860.

Coordinate an exclusive Samsung turn, independently verify awake original Zanance foreground and
fictitious Main979, and upgrade the complete main APK without reset. A fresh ARM64 independent
fixture then passes all six contexts in process11928 at the actual unchanged 384dp width. Native
display digit expectations, both review captions, complete fixture fields and original window focus
pass. Stop only the terminal independent caller and restore the same main process11370. Original
meaningful native Home attributes, profile/balance, language/theme and exact OS settings match;
release the phone turn. No actual authentication, system-setting write or main financial Save.

## Protected complete Home measurement

Because Home's worker calculation changed, measure only the new candidate on the retained D-373
unreviewed corpus25b9dab64bf849fcb24e39246d773215. Reuse the successful D-373 candidate measurements
1d2b201782724f24821e35d8b9f9eb8a without replay. Current comparison12fad1ea027e4c498eb0f98be1fbeedb
uses the original presentation baseline38f39add0d4243a2ba9b29361873042d, unchanged data and keys.
Process2571 completes six original Home contexts/twelve publications. Original financial/native
presentation, focus and complete raw schema/typed rows match. Stop after terminal success and
restore main2254 with its exact original Home0 and OS state. No fixture or key is regenerated.

| Corpus | Retained D-373 median | Current median | Current range |
|---|---:|---:|---:|
| Reference, 10000 entries/20 accounts | 849.01ms | 816.58ms | 727.11-1237.41ms |
| Tenfold, 100000 entries/200 accounts | 2613.96ms | 2714.35ms | 2528.49-2993.18ms |

These are descriptive samples against a saved earlier baseline, not a statistically controlled
speedup. Tenfold is not faster; accept no performance improvement or two-second target from this
change. No second successful baseline run or timing search is used to obtain a better number.
Phone acceptance above concerns the count fixture, not a repeated physical performance benchmark.

## Final package and boundaries

Independent and main strict Windows/Android builds have zero warnings/errors. Final ordinary
Windows build takes22.10s. The complete signed offline main APK is
artifacts/android-home-review-count-d374/pro.vafadar.zanance-Signed.apk,94830325 bytes, SHA256
f8c03c9d4658d12344c65797bdbd5fcd81fed6a0198b4a5f5c672551974eb5af.
Certificate SHA256 remains92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
It retains minAPI24/target36, ARM64/x64,384 AOT libraries, no INTERNET/debuggable/profileable flag,
the identical408 native-library entry names and eight unchanged engine libraries relative to D-373.
The independent-only review constant never activates this diagnostic in the main Release package.

Main no-reset upgrades32060->2254 emulator and4166->11370 Samsung preserve exact meaningful native
Home observations, visible fictitious profile/balance and OS state. These observations do not prove
every private main database row. The existing protected corpus receives the full raw-data comparison.
Only four exact-content repository-owned temporary XML exports are removed; fixture databases,
old reports and keys remain. The independent packages are closed and their retained data preserved.

General GC/finalizer/ANR, cold/low-memory/two-second performance, spoken-reader, iOS, provider,
Store/signing and release acceptance remain open. Existing successful financial, OCR, encryption,
backend and unchanged layout suites are not replayed. Evidence is under artifacts/home-review-count-d374
and the guarded Windows run artifacts/store-drafts/5e9513f1d19f4e8583ec70337aa2f2a0.
