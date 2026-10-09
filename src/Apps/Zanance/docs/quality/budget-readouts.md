# Complete budget figures and signed boundary formatting (D-86 / AT-93)

Actual narrow Windows large-text budget rendering clips the spending and limit currency when both packets share
one horizontal row. Wrapped identity, full-width spending and full-width localized limit now occupy separate rows.
Envelope labels and figures also occupy separate rows. AmountReadout.CaptionStyle retains the original scalable
14 px bold/body and 13 px secondary styles, while existing large callers retain AmountLarge. Original formatted
packets, spoken description, horizontal viewport and translated overflow hint remain. No budget calculation,
progress, carry, method, schema, financial Save, security or policy change.

The actual signed-boundary presentation fixture separately reproduces MoneyText's Math.Abs(long.MinValue)
OverflowException. Eleven independent Core cases fail before correction, then pass: decimal magnitude conversion
supports EUR/en/de, JPY zero digits, KWD three digits, approximation/bidi and IRR/Toman. IRR has two minor digits;
Toman uses the existing display-unit exponent. Positive parsing still rejects an unsigned magnitude beyond the
signed range. This is formatting evidence, not permission to post out-of-range ledger values.

## Runtime evidence and limits

The budget-readouts route changes actual presentation collections/flags only, without SetMethod or Save. It
restores them and compares complete accounts/entries/settings/budgets/plans. Actual native glyph geometry checks
require every model figure row and both compact/default fonts. An icon Grid also inherits BudgetLine, so figure
rows require actual AmountReadout children and an independent count equal to the model. The original icon-row
false selection and the real formatter crash remain negative evidence, excluded from final counts.

During the first full matrix, one 412/light English capture finds Home while an old budget scroll review is still
in progress; the unmeasured Home financial row fails rather than passing as budget evidence. The interrupted
cohorts (including later incomplete wide/baseline runs) are retained and excluded. UI Automation verifies their
review windows are visible: PowerShell WindowStyle.Hidden does not hide a WinUI AppWindow. Requested snapshot
runs now hide only their own native AppWindow and render that root directly, avoiding desktop input/interruption.
Retired onboarding/budget pages explicitly fail rather than awaiting a stale native scroll during cleanup. Normal
Debug without a snapshot request and all Release behavior are unchanged. No assertion is weakened to accept failures.

Final suite, complete successful native matrix, normal Android Release fixture/restoration and signed package
proof are recorded below after verification. Process-local Windows stress does not prove physical OS 200%,
keyboard/screen-reader behavior, physical ARM64/iOS, real provider authentication or Store/product acceptance.

## Completed Windows matrix and suites

Seven successful cohorts cover en/fa/de, light/dark at 360x800, 412x892 and 1280x820 under process-local 200%,
plus 100% light/360. There are 660 own-window/content renders (including prerequisite wizard captures), 492
native budget-figure checks, 1,716 native amount checks, 81 pairs of actual native Scroll endpoints and 21 complete
stored data-equality proofs. Every expected model figure row is required independently of icon templates.
Compact 14/13 px and default large fonts remain scalable; native caption/spoken packets retain signs, digits and
currency. Presentation collections are restored. Original development database/sidecar/marker files are restored
with matching hashes. Failed/interrupted cohorts are excluded, and only incomplete cohorts are rerun. The Debug
window-hide/retired-page guards do not change production UI or the successful earlier geometry checks.

Strict main suite: 1,369 passed, zero failed/skipped; App.Tests' existing 140 cases remain unchanged. Test output
cleanup succeeds. Eleven AT-93 boundary cases independently fail before the fix and pass after it. Native runtime
geometry/scroll/data checks are additional evidence, not invented unit-case counts. Final strict builds and native
Android/package evidence follow below.

## Final builds, normal Android Release and package

Final strict Windows Debug build: zero warnings/errors. The official complete Android Release APK build passes
its zero-warning gate. ZIP integrity, complete ARM64/x86_64 assembly stores and app AOT libraries pass;
independent research/measurement assemblies are absent. Package pro.vafadar.zanance, 0.1.0/code 1, minimum SDK
24/target 36. v2/v3 signatures verify with the local Android Debug certificate SHA-256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. Shipped Cloud permission boundary passes.

Installable file: artifacts/android/zanance-d86-release.apk, 81,172,565 bytes; SHA-256
19b286e1469976f9d969704446edc2c7add0dc906c4173acbe24c38c0164ef55.
This is a complete local phone-test handoff, not Store signing/publishing or physical ARM64 acceptance.

The full signed Release installs on the existing isolated API 36 x86_64 emulator. The exact independently owned
three-entry sample database is first compared completely with the prior baseline. A copied fictitious October
Envelopes budget (alerts off) is prepared with unchanged accounts/entries/settings/schedules, one huge overall
limit and one category limit. The known full Debug package is used without launching it solely for run-as access
to that exact sample file. Initial stream-transfer attempts fail before app launch; the final native file copy is
verified byte-for-byte and by SQLite integrity/full rows before installing and starting Release.

Actual native Insights/Budget shows October 2026, complete separate envelope-label/amount rows, a full huge EUR
limit on its own row after spending, and a category identity/spending/limit on separate rows. Complete native
spoken packets match the actual labels; full-width spending and limit viewports have equal bounds and no overlap.
No financial Save, PIN/security or OS setting change. After stopping Release, complete Accounts, Entries,
Schedules, Settings, Budgets and BudgetCategoryLimits equal the prepared fixture. Restore the pristine sample
file, verify complete equality/integrity and three entries, with no fixture budget remaining. No other profile,
private preferences, SecureStorage or owner archive is read. Final Release reinstall/cold checks follow below.

Android native hierarchy/bounds are not pixel-level glyph or physical font-scale evidence. Keep physical OS 200%,
keyboard/screen readers, physical ARM64/iOS, real OAuth, Store signing and owner product acceptance separate.

The final full signed Release is reinstalled and cold-started in English. Home loads and the actual Transactions
list retains three rows. Native selected captions retain System theme and Advanced mode; initial native Back
reads English. Return Home and stop the app. The exact owned main window keeps SECURE, physical density 420
and font_scale 1.0. No system setting change. CI remains one separate delayed check after push.
