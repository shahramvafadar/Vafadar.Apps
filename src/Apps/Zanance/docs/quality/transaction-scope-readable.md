# Complete report scope and reachable transaction results (D-108 / AT-113)

## Observed defects and change

A real German 360/light/normal-text report query clips the scope and category captions. Native Clear buttons are
36 units wide; the first two right edges are 376.67 and 411.33 against a 345.33-unit root. They cannot be reached
inside the visible page. Actual own-window captures and native target evidence are retained, with original developer
files restored with matching hashes. Normal and 200% expanded saved-filter targets independently already pass
44-unit checks; they are not changed merely because their XAML requests a smaller minimum.

Scope/category/date captions now wrap within a star column; the original Clear command has an independent 44-unit
column. The first German 360/dark/200% candidate leaves the result viewport at zero height. This failed capture is
retained and not accepted. The complete existing filter form now scrolls vertically within the actual page and action-
dock space, reserving up to 144 logical units for result rows. Native scale/typography, horizontal period/kind choices,
query interpretation, complete result/order/totals and saved-filter behavior remain. No schema, SDK, permission,
security or financial change.

## Verified checks and retained negative evidence

The first full matrix stopped in Persian because its caption identity check compared native Persian display digits
with the unchanged Latin bound date. Actual glyphs and targets passed. The review now uses the existing native
label mapper's NativeDigits conversion for its expected caption and retains raw/native/expected text separately;
no production digit or date formatting changed. Negative captures remain and the matrix is repeated in a fresh folder.

The initial page helper compile used Margin on VisualElement; the corrected View parameter compiles. No failed
build counts as native acceptance. An early Clear restoration check compared presentation record/selection identity
and did not retain the route query before delivery. Its failure remains; the review now retains a separate complete
query and compares complete serialized result captions instead of object lifetime. It records actual restoration
state on failure rather than weakening the stored-data or result requirement.

The first serialized continuation rejected an unavailable external-process ExitCode. The actual matrix terminal
returned zero and restored original files; no tests/builds had started through that rejected continuation. Final
builds start directly from the confirmed successful terminal, without repeating the matrix. Native installation
likewise uses the completed build's actual terminal result, not an external process object's exit-code availability.

Focused German 360/dark/200% native caption/target/header-scroll checks pass; actual result height is 144.67 units.
Native Clear and original query/result restoration pass with complete Settings/Accounts/Entries/SavedFilters unchanged.
Every original developer database/WAL/SHM file is restored with matching hashes. Actual native result scrolling
also reveals an original complete monetary row and restores the original list position.

Final checks pass: 1,512 main tests (App.Tests 276), strict Windows and canonical complete Android Debug/Release;
21 Windows contexts/1615 own-window renders, 63 complete native scope captions/Clear targets, 21 native
result-scroll/Clear/query-restoration checks and 84 existing expanded-filter targets. Original developer
files return with exact hashes. Normal signed Release follows the actual Reports -> Transactions -> Clear route on
the owned emulator without preference, security or financial writes; all 24 tables in each of three fictitious
profiles are exactly unchanged. Complete signed D-108 APK verified.

APK: `artifacts/android/zanance-d108-release.apk`, 80,922,539 bytes, SHA-256
`a50eadb996d27a476e9b77e613cd848923bf5c54ac70170cac5b5afa90df8700`. Package `pro.vafadar.zanance`, 0.1.0/code 1, min 24/target 36, non-debuggable,
complete ARM64/x86_64 assembly stores and app AOT, ZIP integrity and v2/v3 signatures pass. Certificate SHA-256
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` is the local test certificate, not production signing approval.
The native emulator retains density 420/font scale 1.0, SECURE foreground protection, QA03 Native, English/System/
Advanced and a Home-stopped handoff. Windows process-local 200% stress and normal Android hierarchy checks are distinct.

Retained final evidence: `artifacts/transaction-scope-delivery-proof.json`, `transaction-scope-apk-proof.json`,
`transaction-scope-release-native/proof.json` and `transaction-scope-all-financial-proof/unchanged-proof.json`.
Successful matrix folders start `transaction-scope-final-matrix-`; earlier failed folders are not included.

D-107 counts and APK remain their historical baseline; this step independently reruns required final checks.

Windows process-local stress does not prove actual OS font scale, keyboard/readers, physical ARM64, iOS, cold-start/
ANR or release/provider/owner acceptance. A11Y-03 remains open. CI is one separate delayed check after push.
