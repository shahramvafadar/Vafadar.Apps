# D-487 original saved forecast difference categories

## Observed defect and complete correction

SnapshotComparer originally divided unplanned cash effects by sign. This made a transfer into
the saved scope, an asset sale or an increasing adjustment look like income; the opposite kinds
looked like spending. A refund was called income and an income reversal spending. The initial
ten-case reproduction has eight failures and two controls passing on the old implementation.

Classify all eight EntryKind values together after the existing original timestamp, date,
ScheduleId and account/currency-scope predicates. Expense/Refund form net unplanned spending;
Income/IncomeReversal form net unplanned income. Transfer, Adjustment, AssetPurchase and AssetSale
form OtherMovements, including both adjustment directions. Same-currency internal transfers net
to zero. Preserve actual daily balances, original saved scope/path and recorded-later separation.
OtherMovements is computed, not a database model change or new stored financial relationship.
The residual subtracts all known categories and remains exact. Its six-language caption is now
Other differences: changed plans and earlier data edits cannot be truthfully attributed only to
plans. Show a nonzero other-movement row neutrally; negative net income uses the original danger
color. Existing zero other-movement comparisons gain no empty extra row. All five affected/new
resource entries have actual en/fa/de/es/fr/it translations, added with Add-Strings.ps1.

## Selected behavior evidence

Ten original new cases pass after the correction. Four additional controls execute only once:
mixed original kinds/planned residual/backdated changes reconcile exactly; an internal transfer
cancels; a backdated transfer is recorded-later only; a future transfer after the compared horizon
is excluded. The mixed control gives actual7500, difference-2500, recorded-later-400, spending-1500,
income+700, other-400 and residual-900 without changing input rows or the saved path. Two affected
previous forecast regressions pass. Fourteen distinct new Core cases plus two regressions and23
resource completeness/usage cases give39 selected successes, not a full-suite count. AT-170 tags
identify the saved-forecast requirement. Successful cohorts are not replayed for later metadata,
comment or report-width corrections.

The first resource run from E: cannot discover the repository above its assembly and fails at
five discovery/test entries. Copy only its compiled output under owned artifacts/review-tests/
d487-localization and run the unchanged23 cases there; all pass. This is a test-location issue,
not failed translations or a production workaround. Preserve the initial missing MoneyText
namespace native compile failure before scenario execution. Correct only the missing import.

## Original UI and deliberately historical source

The independent snapshot-difference phase uses the actual ForecastCalculator, ZananceStore,
ForecastWorkStore, SnapshotViewModel and SnapshotPage. Windows Debug requires its own storepreview
identity; Android requires the exact independent review package. The shared fixture has the exact
(DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW outer gate, never ordinary Android Debug or
shipping code. No main private database/key is read.

Set only this fixture's injected audit clock two days earlier, create two fictitious accounts and
one original immutable flat100EUR snapshot, then advance that fixture clock to actual time and
seed eleven historical/imported-style ledger facts and one new plan once per platform. These raw
facts deliberately cover every kind; refund/trade facts do not claim valid new form submissions.
The actual page uses the normal real TimeProvider. Never change the OS clock or system settings.
At the final day, actual86 versus saved100 yields difference-14EUR: recorded-later-4, net spending
+5, net income-2, other movements-4 and residual-9. The six parts sum exactly to-14. FinancialSaves0
means during presentation, not that fixture setup writes nothing.

Windows run e9cd9fda5faa432eabc06fcd8bec8cd9 uses its independent non-input desktop. Eighteen
contexts (360/412/1280px, en/fa/de, Light/Dark) give36 inspected original-window PNGs, parts and
footer separately. All13 labels/amount packets per context are complete; long text wraps, RTL
amounts retain sign/order, net income-2 is danger-colored, other-4 remains neutral. Original typed
source/base rows match. Desktop isolation records Default as input desktop before and after;
no desktop mouse/keys/focus capture is used. The script's generic36 count means images, not36
language/theme/width contexts.

Original Release/AOT x64 process7685, comparison c75e380d2d9f47efb9d0cbb94c795ab8, completes six
412dp en/fa/de Light/Dark contexts. Physical ARM64 process11941, comparison
18ecb847e9024338a380fc051e3c5b9c, completes six contexts at the actual384dp (1080px/density450).
Each checks13 real native captions through original scrolling, full visible geometry, no ellipsis
and the actual negative-income color. Combined156 caption observations are not managed tests.
All typed source/base rows remain equal; retain successful per-context receipts for continuation.
Correct observer width reporting after x64 success without replaying those scenes.

Preserve the transient adb transport output and empty-event-file observer refusal; the original
x64 PID remains alive and its completed report is read without restarting/replaying. Its COLD
Activity20322ms is a real slow independent start, not a whole-Home benchmark or improvement claim.
The physical independent COLD Activity1928ms likewise is not whole-Home acceptance. The finite x64
PID event query has no matching ANR/crash record; that does not prove general stability.

## Final package, preservation and delivery

Strict Windows and full Build-AndroidApk.ps1 -Offline complete without warnings/errors. Final
shipping package: E:/Zanance.ReviewBuilds/D487-shipping-package/pro.vafadar.zanance-Signed.apk,
96,526,069 bytes,384 AOT libraries, valid ZIP and v2/v3 signatures, ARM64/x64 permanent identity,
no INTERNET/ACCESS_NETWORK_STATE. SHA256:
122e33f63af8024de1561021e466ebe625f67c00efb9056ff8d794827fe3c2ca.
All four packaged Core/App native components hash-match current build outputs. The first
packaging observer looked for a libaot- prefix in intermediate filenames; correct that observer
to the actual Vafadar.*.dll.so names. No application rebuild or UI replay is needed.

Restore the exact prior independent APKs and stopped states before returning original Main.
Emulator28047 preserves all139 public Home tuples, original APK and exact OS. Samsung initially
preserves8764/all141 tuples; final no-reset shipping upgrade starts13827 with the expected new
APK hash, Home979/all141 public tuples and unchanged OS. No Main financial Save or new Main
profile is created. Hash-delete only two owned emulator XMLs and three owned phone XMLs. Preserve
private receipts/source fixture files. Clean managed test outputs; unrelated backup archive stays
untouched. No schema, permission, SDK, financial writer or commercial activation changes.

Prior e310eadb has four successful workflows: local identity backend39935493326, CodeQL39935493383,
CI39935493355 and self-hosted qualification39935493406. Its one delayed GET is consumed and
ci-forecast-missing-rate-d486 deleted; do not query it again. The new commit gets its own single
15-18 minute check. Exact new-head hosted completion is separate from these local results.

## Remaining scope

Real earlier-save/next-day physical persistence has not been accepted: deliberately historical
fixture timestamps are explicitly not actual elapsed days. Remaining date-moving/multi-currency
physical comparison variants, financial form validation, all device/data/key/OS variants,
ANR/crash/GC, whole-Home/two-second performance, OS200%/spoken accessibility, iOS, provider/Store
and publication acceptance remain open. The broad development goal remains active.
