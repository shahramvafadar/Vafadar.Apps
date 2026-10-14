# 05 – Phase 2 backlog

> **Since 2026-10-07 the state of all remaining work is tracked only in the canonical backlog of enhancement ZCR:
> [enhancements/2026-10-commercial-release/04-backlog.md](enhancements/2026-10-commercial-release/04-backlog.md).**
> This file keeps the history of Phase 2A; the accessibility pass and the Phase 2B items below continue there (ZCR-A11Y,
> ZCR-SYNC, ZCR-SHR, ZCR-BANK, ZCR-AI, ZCR-FX, ZCR-BIL).

Nothing here was built in phase 1, not even behind a disabled flag (HAND-05). Phase 2A started on 2026-09-26; finished items are marked below. The phase-1 model keeps each item
possible (SC-01).

D-62 first-run/backup maintenance is recorded as ZCR-LOC-11 in the canonical backlog; it introduces no commercial
limits and does not approve another development wave.

## Phase 2A – local, in suggested order

| Item | Requirements | Model hook already present |
|---|---|---|
| Savings goals, sinking funds, rollover, envelope/flex budgets | F2-GOAL-01..05, BUD-11/12, §10.3 | **P2-1 done:** goals, earmarks per account, coverage and shortfall by priority, suggested contribution (Goals page in More). **P2-2 done:** budget rollover (surplus, or surplus and deficit; consecutive months, max 24). **P2-23 done:** envelope method per budget (money not assigned yet after goal earmarks and unspent envelopes; BUD-11/12). **P2-24 done:** flex method per budget (D-28): expense categories are fixed, non-monthly or flexible; fixed bills expected from the plans, non-monthly bills with their monthly share, one limit for flexible spending, every amount counted once; Home, alerts and rollover use the flexible number. **P2-28 done:** financial month with its own start day (1–28, pay cycle) for budgets, Home, reports, the transaction filters, the forecast month end and rollover; plans keep their dates. **P2-29 done:** limit suggestions: the average net spending of the last three financial months, rounded up, for the overall and the category limits, taken over only on request. **P2-31 done:** weekly and two-week budgets (Advanced) next to the month, with the week start of the settings or a two-week start day (e.g. payday), limits, alerts, rollover between consecutive periods, copying and suggestions from the last four weeks or three periods; envelopes and flex stay monthly |
| Split transactions, partial payments of an occurrence | F2-TX-01/02 | **P2-3 done:** splits as grouped entries that add up exactly (split editor from the entry details; join again). **P2-4 done:** partial payments of an occurrence (open with the outstanding rest; the final payment settles; overpayment shows as a larger actual amount) |
| Reimbursable expenses, tags, attachments, bulk operations, rules | F2-TX-03/04 | **P2-6 done:** reimbursable expenses (F2-TX-03). **P2-7 done:** tags with search and a tag report. **P2-8 done:** categorization rules for new entries and generic imports. **P2-11 done:** bulk operations in the transaction list. **P2-17 done:** receipt photos and PDF files attached to entries (system file picker, no permission; photos scaled to 1600 px JPEG; 5 MB limit; in the database and backups, never in CSV) |
| Contracts, price changes, cancellation deadlines, business-day rules | F2-CON-01..05 | **P2-5 done:** contract details, reminders for the last day to cancel and the review date, Home attention (F2-CON-01/03); price changes via "this and future" (F2-CON-02). Weekend rule for due dates (F2-CON-05, weekends only). Final settlement of advance payments (F2-CON-04). Second day per month for monthly plans. Holiday calendars still open |
| Debts, loans, assets | F2-DEBT-01/02, F2-ASSET-01/02 | **P2-9 done:** loan and money-lent accounts with counterparty, separate from the liquid total, repayment shortcut (F2-DEBT-01). Asset accounts with manual valuation (F2-ASSET-01). **P2-18 done:** optional rate and installment, estimated principal/interest split, payoff and schedule, installment posted as principal transfer plus interest entry (F2-DEBT-02); labelled as an estimate, never as the contract |
| Several days per month, weekday rules, holidays | REC-05, REC-12 | **Done:** a second day per month (REC-05). **P2-19 done:** the n-th or last weekday of the month for monthly and yearly plans (REC-12). **P2-25 done:** public holidays of Germany (nationwide) and Iran (lunar dates approximate) for the weekend rule (D-29) |
| Dark theme, widgets, saved reports, PDF, OCR, scenarios | §21.5, FOR-10, REP-07/08 | **P2-10 done:** dark theme (D-22). PDF report of a period with the Vazirmatn font (REP-07); forecast scenarios with assumed dates and amounts, labelled and never saved (FOR-10, P2-21); saved transaction filters (REP-08, P2-22); customizable Home sections (P2-24); **P2-26 done:** Android quick add widget without amounts (D-30; iOS with the iOS release); **P2-27 done:** on-device receipt reading with review on iOS, Windows and Android (ML Kit, bundled model, no network permission; D-31); **P2-30 done:** PDF invoices and receipts, the text layer read directly and scanned pages rendered by the system (D-33) |
| Unofficial currency units (e.g. Toman) | FX-07 | **P2-20 done:** user-defined display units (currency, name, factor 10^1..10^6) for display and input; stored amounts, CSV and rates stay in the ISO currency; a one-tap toman choice, never applied automatically |
| Multiple local profiles | §3 | **P2-32 done:** local profiles (More › Profiles): one database file per profile with its own data, settings, app lock and backups; switching without a restart (`LocalDatabaseLocation`), a locked profile asks for the device owner first, the main profile is the original database; reminders come from the open profile (D-34) |

## Enhancement ZEX – multi-unit holdings, trackable goals, explainable insights

**Status: design approved by the owner (2026-10-03); implementation in phases 1–6.** Extends the phase 2A capabilities above
without rebuilding them: separate default currency, default account and valuation currency; one account selection
contract for quick entry; quantity holdings (gold in g/kg, coins and items by count) with valuations and linked money
movements; account-balance and quantity goals with Home cards, contribution plans and explained ETAs; the KPIs K01–K14,
liquidity headroom and six report packages; a single Simple/Advanced policy; wealth history and forecast snapshots.
Design, decisions and the **reference backlog with every work item and its state**:
[enhancements/2026-10-multi-unit-goals-insights](enhancements/2026-10-multi-unit-goals-insights/README.md)
([backlog](enhancements/2026-10-multi-unit-goals-insights/06-implementation-backlog.md)). This table does not repeat
those items, so there is only one place for their status.

## Accessibility pass – after the features and the debugging

**Status: planned (owner, 2026-10-06); tracked as ZCR-A11Y-01…04 in wave 2 of the delivery plan.** Once the planned features are finished and debugged, the app is gone through
with the screen readers of every platform and fixed where needed:

| Item | Scope |
|---|---|
| Android – TalkBack | Every screen and dialog reachable and read in a sensible order; every button, chip, switch, field and icon with a spoken name (and a hint where the label is short, as in D-58); errors and changed results announced; custom-drawn controls (Syncfusion charts, calendar, progress bars, the currency box) usable or given a readable text alternative; the quick add widget |
| iOS – VoiceOver | The same checks on an iPhone with the iOS release |
| Windows – Narrator | The same checks; keyboard-only use (Tab order, focus ring, Enter/Space on every action) |
| All platforms | Large text (system font scaling up to 200 %) without cut or overlapping text, colour contrast in both themes, touch targets of at least 44 px, no information given by colour alone, Persian (right to left) read in the right order |

Each finding gets a fix and, where possible, a test; the pass ends with a written check list per platform in
`07-acceptance-test-plan.md`.

## Phase 2B – online, each with its own decision gate

| Item | Gate before any work |
|---|---|
| Real sync | Hosting, identity, encryption, conflict rules approved (F2-SYNC-01..04) |
| Household / sharing | Server-side authorization model (F2-SHARE-01..05) |
| Bank connection | Provider, cost, markets, read-only (F2-BANK-01..04) |
| AI | Official provider access without user API keys; data minimisation (F2-AI-01..09) |
| Online exchange rates | Source, cost, caching (FX-08) |
| Pro purchase, support link, ads | *Superseded by D-61:* Free/Plus/Pro with Plus Lifetime (ZCR-ENT, ZCR-BIL); support links and ads still need their own review (MON-07) |

D-63 privacy maintenance: device-wide four-digit PIN, durable attempt limiting, authenticated recovery and Android
screenshot choice are implemented in ZCR-SEC-10. Broader database/key encryption and device acceptance stay open.

D-64 receipt maintenance supersedes the largest-price fallback in P2-27/P2-30: semantic total evidence, preserved
OCR geometry, independent bounded recognition pixels and review in both entry paths. Finished local implementation
and remaining device/image-quality acceptance are tracked only in canonical ZCR-LOC-12 / ZCR-QA-05.

D-65 debt/plan UX maintenance (ZCR-LOC-13): dedicated debt direction and positive reference-date amount, optional
repayment estimates and separate reminder draft; recurrence calendar, summary/preview and count visible in both
modes. P2-9/P2-18 calculations and principal/interest posting remain unchanged. Device acceptance is AT-72.

D-67 owner-approved maintenance adds connected backup discovery and independent regional display choices. It does
not begin a commercial wave, add holiday countries/states or authorize sync/sharing. Germany uses the existing
nationwide holiday rules. Follow the canonical commercial backlog for the next approved section.


D-68 / SEC-01 is independent security research, not a Phase 2A product feature. Its current evidence and remaining
owner/adoption/device gates are recorded in the canonical commercial backlog and proposed ADR 0010. Windows and
isolated Android API 36 x86_64 runtime proof pass; production encryption is not implemented.


D-69 authorizes continuous implementation of ready canonical sections; unresolved owner decisions remain gates.
SEC-09 reviews the current SDK baseline; repeat the review before future online SDKs are introduced.

D-70 / ZCR-LOC-02 wires the existing contribution-plan reminder flag to the goal editor and shared local scheduler.
Dates follow saved rules; progress/lifecycle/source changes cancel pending requests, generic text is the default,
and a tap only opens the goal. AT-77 covers planner and real-store delivery coordination. Current state and device
evidence live in the canonical backlog and acceptance plan; no ledger/schema or commercial-policy change.

D-71 / ZCR-LOC-03 completes the optional ZEX-S0610 period review reminder locally/on the isolated emulator.
The off-by-default choice is portable; financial boundary/calendar scheduling and cancellation are tested (AT-78).
Physical-device/Doze/reboot and iOS acceptance remain separate from implementation and build proof.

D-72 / ZCR-LOC-04 completes the ZEX-S0611 import follow-up locally/on the isolated emulator: explicit link/keep-both,
new-detail-only reductions, persistent original identity/attachments and conflict-safe Undo across restart/backup.
Main tests: 1,195. AT-79 and the canonical backlog retain physical-device/iOS acceptance separately.

D-73 / ZCR-QA-03 adds actual application flow tests, including Phase 2 bulk commands, validation-safe snapshots and
timed Undo. AT-80: 68 new cases; main suite 1,263. Ready-section delivery continues under D-69 with QA-04 next;
owner product/licence/provider/release gates and physical-device/iOS acceptance remain separate.

D-74 / QA-04 verifies the remaining ZEX-S0408 manual asset-account consent through actual production code, native
runtime and isolated SQLite. Existing conversion-assistant tests remain valid and distinct. No automatic conversion,
new import rule or quota; main suite 1,300. Continuous ready-section delivery proceeds to QA-06 under D-69.

D-75 / QA-06: reference/tenfold measurements and large-ledger read/account-index hardening are tracked in the
canonical commercial-release backlog and quality/performance-q02.md. Main suite 1,323; 23 new AT-82 cases.
Native accessibility/ANR findings must be retained in follow-up; no emulator timing implies physical-device or
publication acceptance. Accessibility work remains the next independent verification area under D-69.

D-76 follows QA-06's early-input finding with an actual snapshot loading/publication gate, retained filters and
translated retry action. AT-83 adds 12 cases; main suite 1,335. See Q-02 for running-app evidence and open findings.
No commercial limits, data model, SDK or permission change; physical-device/iOS and performance objectives remain
separate. Accessibility verification remains the next independent ready area under D-69.

D-77 starts canonical ZCR-A11Y-03 with Home quick actions and plan/date/template layout repairs. Native text remains
scalable, decorative glyphs retain their explicit size and section links have a 44 px minimum. The first verified
slice and its process-local/native evidence do not close the full 200%/screen-reader/device work; see
quality/font-scaling-a11y03.md for the remaining findings.

D-78 continues canonical A11Y-03 with reserved Add/bulk/Undo rows and financial identity/amount layout. AT-85 and
the large-text quality report record running Windows/native evidence and the signed APK. No schema or financial
logic change. Remaining fixed controls, currency grouping and platform/screen-reader acceptance stay in progress.

D-79 repairs the observed Settings publication/caption issues under D-69. Ten AT-86 actual-source tests bring the
main suite to 1,345; running-app checks prove the form cover, real retry and live choice refresh separately. Existing
selection/draft values are preserved. No financial/schema/security policy change. Continue fixed controls and
currency layout findings; A11Y-03 and platform/device acceptance remain in progress.

D-80 continues A11Y-03 under D-69 with growing Settings/account/debt action captions, full-width debt actions and
two-row bulk actions. AT-87 records actual native selection command invocation and geometry without writing money;
main suite remains 1,345. Final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Keep headers,
currency layout, other controls, real OS/screen-reader/phone/iOS and unresolved owner gates open.

D-81 continues A11Y-03 under D-69: date parts reserve all digits at native text scale and reflow; large amount
readouts retain the existing signed decimal/currency packet and expose horizontal overflow with translated feedback.
Account balances/movements get separate rows and Home navigation stays on its heading. AT-88 records actual native
date/amount geometry, both scroll ends and unchanged fictitious entries; main suite remains 1,345 (App.Tests 127).
Final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Keep headers, other controls, real OS,
keyboard/screen-reader/phone/iOS and unresolved owner gates open. No model, financial or security policy change.


D-82 resolves D-81's reopened native language failure under D-69: retired navigation Title bindings are detached
before replacing their Shell, preserving active forms and the lock/deferred rebuild boundary. Settings constructor
defaults no longer invoke a save. AT-89 checks actual native selection/back, strongly retained retired titles,
live captions/drafts and complete unchanged stored settings/accounts/entries. Main suite remains 1,345 (App.Tests
127); final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Header truncation and other controls,
real OS/keyboard/screen-reader/phone/iOS and unresolved owner gates remain independent work.

D-83 continues A11Y-03 under D-69: complete Windows child-page titles grow above the same body; header/body sizing
uses the current root and Back names/tooltips translate live. Android keeps the native arrow/commands with a scoped
live Back description. AT-90 records native glyph/name/target geometry, actual retained bodies/drafts during resize,
single headers after nested return and unchanged stored data. Final evidence/APK is in quality/font-scaling-a11y03.md.
Modal/Insights/custom controls, the existing nested-return Settings draft reload, real OS/keyboard/screen readers,
physical ARM64/iOS, QA-06 durations/ANR and unresolved owner decisions remain independent work.

D-84 resolves the independently observed Settings nested-return estimate reset. Full covered refresh remains;
raw unsaved text/period/currency stay scoped to the current profile/settings row. Save accepts only successfully
submitted input; no autosave, financial rule or schema change. AT-91 adds 13 cases, and the real retained-body
runtime route now checks drafts after nested return. See quality/settings-estimate-draft.md for final evidence/APK.
Continue modal/Insights/custom-control and other ready work under D-69; unresolved owner/platform gates remain.

D-85 continues A11Y-03 with all four visible Windows Insights destinations in a growing navigation row above
the retained body. Narrow windows reflow into two columns; wide readable content shows four. AT-92 invokes the
actual native routes and checks complete caption/selection/target geometry and unchanged stored data. Final
evidence/APK and independent platform/other-control limits are in quality/insights-navigation.md.

D-86 continues A11Y-03 with complete wrapped budget identities and separate full-width spending/limit and
envelope readouts, retaining compact native typography. A separately reproduced MoneyText signed-boundary crash
is repaired by taking the magnitude after decimal conversion; input range and financial calculations stay unchanged.
AT-93 adds 11 independent Core cases and actual native scroll/geometry/data-equality checks. Final evidence and
remaining physical OS/screen-reader/device/iOS gates are in quality/budget-readouts.md. No schema or new strings.

D-87 continues A11Y-03 by keeping all three budget period decisions visible in the existing wrapping choice group.
An actual pre-change native check fails for the hidden third option. AT-94 measures full native captions/targets,
invokes the existing three choices, restores selection and compares complete stored data without Save. No shared
control, financial calculation, schema or new string change. Final evidence and independent OS/screen-reader/
physical-device/iOS gates are in quality/budget-periods.md.

D-89 continues A11Y-03 with complete growing tag suggestions and original-value native selection. AT-95 checks
84 actual native invocations and 21 exact draft/stored-data restorations across the three-language/theme/width
matrix; normal signed Android Release confirms selection, Keep editing and Discard without financial writes.
Main suite remains 1,369 (App.Tests 140); strict Windows and equivalent-command Android Release have no warnings/errors.
PowerShell startup blocks the canonical APK/privacy scripts locally; the complete signed package and matching binary
policy are independently verified. Evidence and remaining tooling/OS/screen-reader/phone/iOS/owner gates:
[quality/tag-suggestions.md](quality/tag-suggestions.md).

D-90 continues A11Y-03 with state-matched transaction detail actions and retained unsaved fields. AT-96 checks
48 actual native Invoke operations and 24 complete draft/stored-row restorations, including Simple/Advanced initial
visibility. Main suite remains 1,369 (App.Tests 140); strict Windows and canonical Android Release have zero warnings/
errors. Normal signed Release passes hide/show, retained payee/tag/note values and Keep editing/Discard in en/fa/de;
complete fictitious financial rows remain unchanged. Canonical APK/privacy scripts and full package/signature pass.
Evidence and independent OS/screen-reader/phone/iOS/provider/owner gates:
[quality/entry-details-disclosure.md](quality/entry-details-disclosure.md).

D-91 / AT-97 keeps both first-run restore alternatives fully readable and retains their existing navigation/busy
bindings. The final Windows matrix and signed Android Release pass; 1,369 tests (App.Tests 140), zero-warning strict
builds and full installable APK/privacy checks. See [quality/onboarding-restore-actions.md](quality/onboarding-restore-actions.md)
for native draft/stored-data preservation and independent platform/physical-device/provider/owner gates.

D-92 / AT-98 removes measured redundant Home row/goal work while retaining complete values and financial rules.
All 1,384 tests pass (App.Tests 148), strict builds and complete signed Release APK/privacy/native checks pass.
Controlled tenfold Windows warm reload median: 2,727.70 -> 1,228.28 ms. QA-06 remains partial: entry materialization,
cold start/ANR and physical-device Q-02 are open. See [quality/performance-home-snapshots.md](quality/performance-home-snapshots.md).

D-93 fixes the Windows-only Home diagnostic call that broke Android Debug, with no Release/UI/financial behavior
change. Final Windows/Android Debug/Release builds and all 1,384 tests (App.Tests 148) pass. Temporary native stage
measurements identify entry materialization/initial account creation; QA-06 and physical/platform acceptance remain
open. Evidence: [quality/home-debug-platform-and-native-stages.md](quality/home-debug-platform-and-native-stages.md).

D-94 completes the local Customize Home caption/target follow-up: all eight section identities grow at full
width above the original controls, with 44 px targets and a complete Reset action. AT-99 final en/fa/de theme/width/
200% review and 84 native operations, 1,384 main tests, strict Windows/Android Debug/Release builds and signed Release
owned-sample readbacks pass. A11Y-03, QA-06 and real device/platform acceptance remain partial/open.
Evidence: [quality/home-customization-readable.md](quality/home-customization-readable.md).

D-95 / AT-100 removes measured eager native row construction for hidden Home Accounts while retaining the
complete snapshot and every shown account/action. Native tenfold publication median: 5,339.89 -> 10.69 ms, with no
total cold-start/Q-02 acceptance claim. Final language/theme/width/200% native visibility/detail/list checks, 1,384
main tests, strict Windows/Android Debug/Release builds, signed Release and complete owned-sample readbacks pass.
QA-06 full entry materialization, visible creation, cold duration/ANR and real device/platform acceptance remain open.
Evidence: [quality/home-hidden-account-views.md](quality/home-hidden-account-views.md).

D-96 / AT-101 completes account type/default/excluded/incomplete captions in the shared account row at large
native text, retaining complete text, flags, balances and row actions. Final en/fa/de themes/widths/200% and normal
text checks cover all eight flag combinations; 1,384 main tests, strict Windows/Android Debug/Release builds, signed
Release navigation and exact 24-table original-sample readbacks pass. A11Y-03 other controls/modals and actual OS,
screen-reader, phone/iOS and release acceptance remain open.
Evidence: [quality/account-descriptions-readable.md](quality/account-descriptions-readable.md).

D-97 / AT-102 completes the existing Accounts debt/receivable action caption with a growing real button,
unchanged text/scaling/command and native open/cancel verification of the same unsaved Loan form. Complete stored
accounts/entries/settings/budgets/schedules remain unchanged. Final en/fa/de themes/widths/200% and normal text,
1,384 tests, strict Windows/Android Debug/Release, signed Release navigation and exact 24-table readbacks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/debt-entry-action-readable.md](quality/debt-entry-action-readable.md).

D-98 / AT-103 removes duplicated Windows modal titles and reflows all eleven existing modal title/Cancel
rows without changing forms or financial behavior. Actual glyph/name/target/no-overlap and native Cancel/parent
Back pass across all Windows language/theme/width/text contexts; all nine complete stored data sources remain.
Normal Android debt/expense/plan forms pass in three languages and both themes; original three-profile tables
remain identical. All 1,384 tests, strict Windows/Android Debug/Release and complete signed APK checks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/modal-headers-readable.md](quality/modal-headers-readable.md).

D-99 / AT-104 collects all applicable plan-field problems in one invalid Save attempt, displays destination
feedback beside its input and reveals the first problem using fresh native layout. Drafts and complete stored
Accounts/Entries/Settings/Schedules remain; existing money/recurrence/unknown-amount rules are unchanged.
Twenty-four new behaviour cases bring the main suite to 1,408 (App.Tests 172). Full Windows language/theme/width
review, strict Windows/Android Debug/Release, normal Android invalid Save/cancel, signed APK and original 24-table
readbacks pass. Other A11Y-03 controls and OS/readers/phone/iOS/owner/release gates remain open.
Evidence: [quality/plan-validation-visible.md](quality/plan-validation-visible.md).

D-100 / AT-105 collects independent transaction monetary problems before mutation, shows each beside its
input, reveals the next affected field and reopens invalid collapsed details without replacing entered values.
Existing parsers, consent/receipt protections and successful ledger/fee/overlap/attachment ordering remain.
Forty-eight new cases bring the main suite to 1,456 (App.Tests 220). Complete Windows matrix, strict builds,
normal Android invalid Save/cancel, signed APK and original 24-table readbacks pass. Simple destination-fee
retention is a separate pending runtime concern; other controls/platform/owner/release gates remain open.
Evidence: [quality/entry-validation-visible.md](quality/entry-validation-visible.md).

D-101 / AT-106 closes the reproduced destination-fee loss in successful Simple edits. Existing fees stay
visible/editable in both modes; new destination-fee creation remains Advanced-only. Native retention, explicit
edit/removal and reopened/new Cancel/Discard preserve financial fields/ids and unrelated rows. The final
Windows matrix has 24 contexts/72 valid Saves; main tests remain 1,456 (App.Tests 220), strict builds and signed
APK pass. Normal Android creation-policy checks and original financial/preference values and expected settings audit pass; successful stored-fee
editing on Android/physical/iOS and other owner/platform/release gates remain open.
Evidence: [quality/destination-fee-retention.md](quality/destination-fee-retention.md).

D-102 / AT-107 delivers complete settlement period/bill feedback and a growing Record the difference
action. Invalid Save reveals the first affected input without changing the draft; original advance/refund logic
retains zero bills and no-op exact bills. Fourteen new cases bring the main suite to 1,470 (App.Tests 234).
The final Windows matrix has 24 contexts/144 invalid and 72 valid native Saves; six normal-scale Release Android
contexts/18 invalid Saves, strict builds, signed APK and original financial/preference-value checks pass.
Normal Settings audit updates are separate; physical/iOS/readers, valid Android settlement acceptance and other
owner/platform/release gates remain open. Evidence: [quality/settlement-feedback.md](quality/settlement-feedback.md).

D-103 / AT-108 completes due-item action captions and correctly placed payment/override feedback, with independent
corrections and native revelation of the actual affected field. Fourteen new cases bring the main suite to 1,484
(App.Tests 248). Windows: 24 contexts/96 invalid and 96 valid native Saves, plus a separate 24-context/96-invalid
final contextual-message review with no additional financial Saves; normal-scale Release Android:
six contexts/18 invalid Saves. Actual metadata changes never post entries; partial payment and completion retain
the intended entries and unique settlement. Original financial rows/states are preserved/restored; strict builds
and the complete signed phone-test APK pass. Phone/iOS/readers, valid Android occurrence acceptance and other
owner/platform/release gates remain open. Evidence: [quality/occurrence-feedback.md](quality/occurrence-feedback.md).

D-104 / AT-109 delivers complete transaction row reuse for repeated filters, scoped to one data/display snapshot.
Four new tests pass (main 1,488). Final running-app/build/APK checks and measured performance limits are recorded in
[quality/transaction-row-reuse.md](quality/transaction-row-reuse.md). QA-06 and real device/platform/release gates remain open.

D-105 / AT-110 removes repeated full-ledger membership scans from complete bulk-selection reload. Final tests/runtime/build/APK evidence: [quality/bulk-selection-reload.md](quality/bulk-selection-reload.md). Cold/device/ANR/native-publication and owner acceptance gates remain open.

D-106 / AT-111 removes unrelated ledger materialization from the unchanged Settings suggestion. Main suite: 1,512 passed, App.Tests 276. Final runtime/build/APK evidence: [quality/settings-suggestion-history.md](quality/settings-suggestion-history.md). Cold/device/ANR/platform and owner acceptance remain independent.

D-107 / AT-112 continues A11Y-03 with complete growing Windows selected/popup picker captions, inherited native
typography and 44-unit popup targets. Native reviews retain choices/drafts/full stored values without Save;
main suite remains 1,512 passed (App.Tests 276). Final native matrix, strict Windows/complete Android Debug+Release, exact original-data readbacks and signed APK pass: [quality/native-picker-captions.md](quality/native-picker-captions.md).
Other controls and actual OS/readers/device/iOS/release acceptance remain open. Continue ready work under D-69.

D-108 / AT-113 addresses actual clipped report-scope captions/off-page Clear actions and a zero-height transaction
result at large text. Final 21 Windows native contexts, main 1,512 tests (App.Tests 276), strict builds, actual
normal Release report/navigation/Clear, exact original-data readbacks and complete signed D-108 APK pass: [transaction-scope-readable.md](quality/transaction-scope-readable.md). A11Y-03 and independent
platform/OS/readers/device/owner/release gates remain open; continue ready work under D-69.

D-109 / AT-114 fixes observed inheritance of unrelated report scope when applying a named transaction filter.
Preserve the saved combination and complete financial data. Final six native contexts/18 independent report
restrictions, main 1,512 tests, strict builds, normal Release, exact original-data readbacks and signed APK pass:
[quality/saved-filter-report-scope.md](quality/saved-filter-report-scope.md). Provider/device/iOS and owner gates remain open.

D-110 / AT-115 avoids native rebuilding of completely unchanged transaction results while retaining every
ordered row, caption and boundary. Fresh data/display snapshots still publish. Main 1,521/App.Tests 285;
six native Windows contexts, strict builds, normal Release, original-data readbacks and signed APK pass:
[quality/unchanged-transaction-source.md](quality/unchanged-transaction-source.md).
Cold/ANR/physical-device/iOS/provider and owner/release gates remain open.

D-111 / approved OD-12 implements Repeat from a new transaction into an unsaved monthly Plan without posting
money. Preserve original complete drafts, named calendar dates, currency identities and safe defaults. Main 1,535/
App.Tests 299, final financial/presentation matrices, strict builds, normal signed Release Plan Save/cleanup,
exact original-data readbacks and complete signed APK pass:
[quality/entry-repeat-draft.md](quality/entry-repeat-draft.md). Provider/physical/iOS/owner/release gates remain open.

D-113 / AT-117 delivers complete growing transaction category choices, retaining original raw selection identity
without Save. Main 1,535/App.Tests 299, Windows 33-context/99-selection review, strict builds, normal signed
Release selection/discard and exact 24-table original-sample readbacks pass. Complete signed D-113 APK is ready:
[quality evidence](quality/entry-category-captions.md). Other A11Y-03/device/iOS/OS-reader/provider/owner gates stay open.

D-117 / ENT-01 implements the owner-approved final Core commercial model (OD-03), with no app enforcement.
76 new policy cases; main suite 1,611, App.Tests 299 unchanged. Phase/runtime/release acceptance stays separate:
[quality evidence](quality/entitlement-policy.md). ENT-02 service checks follow with activation still gated.

D-118 continues ENT-02 with the actual-file account write transaction boundary; test-build limits stay inactive.
15 added SQLite cases/main 1,626 pass. The rest of ENT-02 and runtime/release acceptance remain open:
[quality evidence](quality/account-write-policy.md).

D-119 continues ENT-02 with actual-file template/filter write transactions, slot-preserving edits/replacements
and atomic failure recovery. Current enforcement stays inactive. 24 added SQLite cases/main 1,650 pass; the
remaining resources/import/restore/read-only/native paths and release gates stay open:
[quality evidence](quality/template-filter-write-policy.md).

D-120 continues ENT-02 with Goal Save and whole Plan batch/split transactions, counted pauses and slot-neutral
continuations. 45 added SQLite cases/main 1,695 pass. Current deployment stays inactive; contribution/allocation/
occurrence work, other resources/import/restore/read-only/native paths and activation remain open:
[quality evidence](quality/goal-plan-write-policy.md).

D-121 continues ENT-02 with current-period budget Save/confirmed Replace, canonical financial definitions and
complete replacement rollback. 26 added SQLite cases/main 1,721 pass. Current deployment stays inactive; explicit
active/read-only selection, future-period activation and other ENT-02/03 paths remain unfinished:
[quality evidence](quality/budget-write-policy.md).

D-122 continues ENT-02 with actual-file holding write rights, retained corrections/delete/Undo and atomic
purchase/payment/fee/derived-price Save. 46 added SQLite cases/main 1,767 pass. Current deployment stays inactive;
explicit read-only/import selection, other resources/operations/native paths and activation remain unfinished:
[quality evidence](quality/holding-write-policy.md).

D-123 continues ENT-02 with retained earmark/release rights and contribution operations, plus one atomic
goal/pin/contribution editor Save. 39 added SQLite cases/main 1,806 pass. Current registration remains inactive;
selected read-only items, contribution delivery and remaining ENT-02/03/04 paths stay open:
[quality evidence](quality/goal-contribution-write-policy.md).

D-124 continues ENT-02 with exact-file database backup/recovery and retained import/Undo checks. Restore binds
the destination before input and migrates the same file, correcting an actual reproduced redirection defect.
28 added SQLite cases/main 1,834 pass. Current registration stays inactive; selected read-only import/restore data,
profiles, automation and remaining ENT-02/03 paths stay open: [quality evidence](quality/recovery-write-policy.md).

D-125 continues ENT-02 with actual-file category rule creation/correction/deletion rights and serialized
same-pattern replacement, including current unrestricted builds. 28 added SQLite cases/main 1,862 pass.
No stored transaction is reclassified. Registration stays inactive; suggestion delivery, selected read-only data,
profiles, automation and remaining ENT-02/03/04 paths stay open: [quality evidence](quality/category-rule-write-policy.md).

D-126 continues ENT-02 with actual-file ledger Save rights, complete split/holding/transfer groups and
final-batch refund validation. 45 added SQLite cases/main 1,907 pass. Four reproduced refund defects corrected;
entry/attachment/paid-total rollback retained. Registration inactive; ledger delete/Undo, selected read-only data,
automation and remaining ENT-02/03/04 paths stay open: [quality evidence](quality/ledger-write-policy.md).

D-127 continues ENT-02 with actual-file ledger Delete/Undo, explicit file-bound refund snapshots and
retry-safe application offers. 38 added cases/main 1,945 pass; App.Tests 308. Receipt orphan/purge behavior stays
unchanged, without extra byte copies. Registration inactive; selected read-only data, automation, commercial
feedback and remaining ENT-02/03/04 paths remain open: [quality evidence](quality/ledger-undo-write-policy.md).

D-128 reports actual transaction Undo/refresh failures through the existing translated dialog, preserving the
original retry deadline and blocking repeated invocation through pending feedback. Nine added actual-command cases;
main 1,954 / App.Tests 317 pass. Six normal installed Release failure flows retain committed financial data and
receipt bytes. No activation changes. Later refund edits during Undo and visible ledger Save feedback remain open:
[quality evidence](quality/transaction-undo-feedback.md).

D-129 makes short ledger Undo snapshots independent of returned mutable rows and rejects obsolete refund,
partial-group and account-currency dependencies before writes. 18 added cases/main 1,972 pass; App.Tests 318.
Normal/conflicted installed Release checks preserve complete data/receipts; current enforcement stays inactive.
Native editor timing, visible ledger Save feedback and remaining ENT-02/03/04/external gates remain open:
[quality evidence](quality/ledger-undo-conflicts.md).

D-130 delivers the observed D-126 Save-feedback follow-up: existing general transaction errors stay beside
fixed Save, with native scalable wrapping text and complete spoken names. Explicit rejected Save/retry/storage
failure retain reviewed draft fields and stored rows; field validation clears obsolete general feedback. No data,
permission, algorithm or commercial activation changes. Remaining ENT-02/03/04 and external gates stay open:
[evidence](quality/entry-save-feedback-visible.md).

D-131 continues ENT-02/03 with the explicit Core resource-choice projection and actual ledger account selection,
verified retained overdue payments and serialized reviewed advance bills. 79 added cases/main 2,051 pass;
App.Tests 318. Current test builds remain unrestricted. Selection UI/persistence, other bindings/automation and
external gates remain open: [quality evidence](quality/selected-account-write-policy.md).

D-132 continues ENT-02/03 with actual retained manual Loan/Lent closure and separate new-work rights for
counterpart borrowing. 51 added cases/main 2,102 pass, App.Tests 318. Current app registration stays inactive;
choice UI/persistence, other selected bindings and occurrence-state/automation remain open: [evidence](quality/retained-debt-closing.md).

D-133 continues ENT-02/03 with retained occurrence correction/delete/repair rights and atomic explicit payment
linking under the actual-file writer. 39 added cases/main 2,141 pass, App.Tests 318; native en/fa/de light/dark
normal confirmations preserve actual money/metadata and create no second entry. Registration stays inactive.
New settlements/partial/final posting, generated-entry reopening, selected-plan generation, automation, choice UI/
persistence and occurrence command failure feedback remain unfinished: [evidence](quality/occurrence-correction-writer.md).

D-134 continues ENT-02/03 with atomic new full/partial/final payments under the actual-file writer and current
remaining money. Reuse ledger validation; recheck stored relationships and Core occurrence rules, with complete
SQL/access rollback and notifications after commit. 16 added SQLite cases/main 2,157 pass, App.Tests 318;
six normal Release en/fa/de light/dark flows. Registration stays inactive. Generated-entry reopening,
selected-plan automation, choice persistence/UI and command exception feedback remain open: [evidence](quality/atomic-occurrence-payments.md).

D-135 continues ENT-02/03 with the reviewed settlement identity and actual entry relationship bound to the
same writer as generated deletion or manual unlinking. Reuse complete deletion/Undo; no second committed-state
write. Preserve partial money, overrides, refund/receipt ownership and suppression. 15 new cases/main 2,172 pass,
App.Tests 318; six normal Release native confirmations. Current registration stays inactive. Other selected-plan
generation/automation, choice persistence/UI and command exception feedback remain unfinished.
[Evidence](quality/atomic-occurrence-reopening.md).

D-136 completes exception feedback and shared consent/writer/publication gates for occurrence commands. 29 added application-flow cases; App.Tests 347 and main 2,201 pass with owned output-only Data.Tests serialization. Normal Release native failures preserve drafts and all complete rows. Negative parallel evidence is retained; D-137 later completes file-scoped cleanup isolation. Commercial registration remains inactive; selected-plan generation/automation, choice persistence/UI and external gates remain unfinished.
[Evidence](quality/occurrence-command-feedback.md).

D-137 closes the D-136 global SQLite cleanup follow-up: nine actual session/service cases; normal parallel full suite 2,210 passes, App.Tests 348. Strict Windows/Android, complete signed APK and normal Release native profile/restore/deletion verified. No commercial activation or external acceptance. [Evidence](quality/local-sqlite-pools.md).

D-138 continues ENT-02/03 with fresh automatic payment eligibility/values and explicit cached plan choice under the actual ledger writer. 31 new cases; normal parallel main 2,241 / App.Tests 348 pass. Strict builds, six normal Release native flows and complete signed APK verified. Enforcement remains inactive; generation/reminders, choice persistence/UI and other selected bindings/external gates remain open. [Evidence](quality/automatic-occurrence-writer.md).

D-139 continues ENT-02/03 with selected future occurrences in Home/Plans/detail and actual generic/full/partial
payment writers; history/overdue rights and original calculation inputs remain complete. 41 new cases; normal
parallel main 2,282 / App.Tests 348 pass. Strict builds, six normal Release native flows with all 24 complete rows
unchanged/restored and the complete signed APK are verified. Registration remains inactive. Actual reminder/snooze
delivery, choice persistence/UI, other bindings and external gates remain open. [Evidence](quality/selected-plan-occurrences.md).

D-140 binds actual Plan/contract notification delivery and snooze rebuilding to the opened file and current
explicit plan choice. Original group members, recurrence identities and effective dates survive snooze; current
privacy/language/text is rebuilt from stored rows. Unprovable legacy snooze caches are discarded while normal
reminders are rebuilt; legacy navigation remains readable. Notification taps resolve the current file after
unlocking, without profile guessing or money writes. Reserve the snooze id marker so a hashed occurrence id cannot
keep the original deadline. Registration remains inactive. Choice UI/persistence, other selected resources and
external acceptance remain open. [Evidence](quality/selected-plan-reminders.md).

## D-141 delivery - Durable financial resource choices

ENT-02/03 account/Plan choice persistence and actual writer/reminder binding are delivered without commercial
activation. Additive schema, explicit scoped Save and retained history/correction behavior: 44 new AT-143 cases,
main 2,374 / App.Tests 382 pass. Chooser UI, other resources/operations and owner/external gates remain open.
[Evidence](quality/durable-resource-choices.md).

## D-142 delivered account/Plan chooser

The reviewed Settings account/Plan form is delivered with covered loading, explicit scoped Save, preserved draft
feedback, separate original states and current unrestricted-build explanation. 25 new AT-144 flows; main 2,399 /
App.Tests 407 pass. Strict builds, actual native layout/navigation and complete signed APK are recorded in
[the quality record](quality/active-item-chooser.md). ENT-02/03 remain in progress for other resource choices,
operations/bindings and future activation; owner/external gates remain open.

## D-143 delivered durable goal writer

Extend the existing ResourceSelections boundary to Goals without a schema/entity change. Reviewed original names,
measurement kinds, currency and active/paused/completed/archived states are rechecked with actual file, financial
scope, verified rights, stored revision and capacity. Free's one goal counts a pause; explicit empty is retained.
Invalid/stale choices need review, and choosing an advanced retained goal never grants its paid tools.

The goal writer reads stored choice under the actual SQLite writer; it overrides only cached choice, never
entitlement/membership. Explicit new or reopened goals join a free chosen slot atomically, rather than counting
unselected retained history again. Completion/archive/delete releases only that identity; pause keeps it.
Unselected paused goals cannot resume. Retained notes/state corrections, negative earmark releases and deletion
remain available. Positive earmarks independently require AdvancedGoals and usable goal/account choices; no ledger
or balance is written. Full editor/contribution failures roll back goal, pins, plan and choice together.

Current release registration remains inactive and unrestricted. Settings still exposes Accounts/Plans. Goal
chooser UI, effective goal/contribution projection and native reminder delivery follow in subsequent ENT-02/03
slices. Other selected resources, activation and owner/provider/device/iOS/publication gates remain open.

31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](quality/goal-resource-choices.md).

## D-144 delivered goal chooser

The existing Settings availability form now has Accounts, Plans and Savings goals as three growing native choices.
Read the original goal name, measurement kind and state from the actual D-143 scoped snapshot; money goals also show
their original currency, while holding-quantity goals do not display a currency as their unit. Completed goals say
Completed rather than the Plan-specific Ended. Paused goals count toward Free's one-goal allowance without resuming.
Completed/archived identities remain readable and unselectable. No goal, allocation, contribution or ledger write
occurs while selecting, navigating, refreshing captions or discarding a draft.

Explicit Save uses the reviewed actual-file writer, including empty choices and malformed-choice repair. Preserve
the exact goal draft/index/identity on stale name/kind/currency/rights or storage failure and on live language change.
Keep existing consent/busy guard, covered publication/retry, pinned Save/error and committed-review retirement.
Current unrestricted registration requires no subset or Save and grants no fake paid rights. Choosing an advanced
goal never grants its paid tools; translated explanation/example make this explicit in all six existing languages.

Goal chooser UI is delivered. Effective goal/contribution projection and native reminder filtering/routing remain
the next ENT-02/03 binding slice; other resources/operations, activation and owner/external gates remain open.

18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](quality/goal-chooser.md).

## D-145 delivered actual goal work and scoped reminders

Read complete original goals, saved contribution rules and allocations from the actual opened database, with
new-work eligibility kept separate from original financial progress. Selection never changes funding/protection,
resumes a paused goal or grants a paid tool. Free balance-goal activity, advanced goal work and contribution reminders
are independent rights. Missing, empty, malformed, foreign and historical choices keep their original data.

New paid contribution work checks the actual chosen goal under the same SQLite writer. A paired new goal can join
its slot before that check; reject failed activity before pin/protection normalization. Retained amount corrections,
reminder disabling/removal and negative earmark releases stay available. These operations never post ledger money.

Prepare goal reminders only for eligible original progress before the pending cap. Keep saved opt-in, generic/details
privacy, fixed 09:00 device-local rule dates, existing permission and the shared bounded queue. Native returning data
contains bounded original goal/date members and an opaque actual-file identity. Taps resolve that file after unlocking,
open the existing single detail or Goals root, and never switch profiles or grant new work. Retained history remains
readable after a choice change. Existing legacy historical links remain supported.

Validate actual file, cached rights, persisted choice revision and original goal/rule/allocation input stamp before
and after native publication; clear a retired queue, including failed snapshot preparation. The stamp is transient,
invariant and reflection-free, not a new schema, portable preference or credential. Current commercial registration
remains inactive and unrestricted. Goal-view contribution affordances and other remaining bindings follow separately.

36 new AT-147 actual application/real SQLite cases; focused 36 pass (9.147 s). Normal parallel main 2,484 /
App.Tests 461 pass (71.478 s), no failures/skips; test output cleaned. Strict Windows 15.49 s / Android 35.35 s,
zero warnings/errors. Six normal signed Release owned-emulator cases cover English/Persian/German, light/dark,
actual native stored single/group Intent routing, original destinations/Back, and rejection in another real profile.
The real pending queue retains all three original goals, bounded members, generic privacy and saved dates at 09:00.
No financial Save; complete original rows and exact owned bytes/sidecars plus Main/English/System restored.
This verifies stored native requests and actual Android Intent delivery, not future alarm firing at 09:00.
Complete signed installable APK verified. No layout/copy change; D-144 geometry evidence remains revision-specific.
Physical-phone, iOS, provider, OS accessibility, Store signing/publication and commercial activation remain unaccepted.

[Evidence](quality/goal-work-reminders.md).

## D-146 goal-view delivery

Original goal views, reviewed detail writes and historical negative releases are delivered under continuous authorization. Main 2,520 / App.Tests 497 pass; strict builds, Windows native matrices, six normal Release owned-emulator cases and complete signed APK verified. Phase 1 history remains implemented; commercial ENT-02/03 and external release gates remain open.

[Evidence](quality/goal-work-views.md).

## D-147 goal-editor delivery

40 new AT-149 actual-source/real SQLite cases pass. Final normal parallel main 2,560 / App.Tests 537 pass,
zero failures/skips; test output cleaned. Strict Windows and Android builds and canonical Release APK build have zero
warnings/errors. Eighteen English/Persian/German, light/dark, 360/412/wide contexts check the actual Advanced and Simple
modal at process-local 1.5 font stress: complete changed glyphs, real 44-pixel Invoke targets, stale Save rejection,
retained reviewed reload/current saved summary and native Close. All 25 complete fixture tables and original development
data/sidecar bytes are restored. The review uses independent fictitious data and explicit native-effect answers, not
replacement MAUI controls or a production entitlement replacement.

Six normal signed Release owned-emulator cases check the original archived account, uncommon rule, multiple-category
warning, pinned Save and native Close/Back. One actual note-only Save succeeds; complete original rule/categories,
accounts, ledger, allocations and other metadata stay equal. Only that goal note/audit and the previously delivered
D-141 migration differ. Exact owned original bytes/sidecars and Main/English/System are restored; the normal Release
remains installed/stopped. No physical-device, iOS, OS accessibility, provider, performance, Store signing/publication
or commercial-activation acceptance is implied.

Canonical Build-AndroidApk.ps1 produces artifacts/android/zanance-d147-release.apk, package pro.vafadar.zanance,
nondebuggable Release, intact ZIP, full ARM64/x64 assembly stores and application AOT, verified v2/v3 signature with
the existing local test certificate. Bytes: 81491883. SHA-256: `d3aa44afc6f55aae1ea3b4a0fe82700c5120ad196343afcd7cdcfbdf0d6879aa`.
No schema/model/migration, portable preference, security policy, credential, SDK, paid grant or permission change.

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

Evidence: `docs/quality/goal-editor-work.md`.

## D-148 - Selected budget definition boundary

Canonical scoped budget definition choice and separate current-work projection are implemented with
reviewed actual-file persistence. Save/Replace join or transfer chosen capacity atomically; Delete retains exact
membership checks and releases only the last current definition. Original history, paid-method corrections,
limits and financial rows remain intact. 34 new AT-150 cases; normal parallel main 2,594 / App.Tests 537 pass;
strict builds, normal Release native Save/Delete and six unchanged-editor language/theme checks, complete signed
APK and exact owned data/display restoration verified. Registration remains inactive. Budget chooser/view/alert
binding and other ENT-02/03 paths remain open. See [evidence](quality/budget-definition-choice.md).

## D-149 - Actual reviewed budget definition choices

The actual active-items form includes canonical budget definitions, grouped across copied periods with
original scope names/current methods and unselectable history. Covered review captures full account references;
explicit Save rechecks original metadata and writes only chosen definitions. Original account names stay exact
when display digits change. 13 new AT-151 cases; main 2,607 / App.Tests 550 pass; strict builds, 18 Windows native
chooser cases plus 18 registered Settings/help cases, six normal Release emulator cases, exact data/display restoration and complete signed
APK verified. Registration remains inactive. Effective budget/view/alert bindings and other ENT-02/03 paths
remain open. See [evidence](quality/budget-definition-chooser.md).

## D-150 - Actual current budget alerts

Actual alert orchestration checks every eligible original current budget across calendars/currencies/scopes,
with separate chosen-work rights and complete original net spending/carry. Read-only actual-file financial/privacy
reviews guard native publication; failed/retired batches retract attempted alerts and retain retryable device levels.
Carry no longer mixes another account definition. 25 new AT-152 cases; main 2,632 / App.Tests 575, strict builds,
six hidden Windows budget renders, six normal Release Android delivery cases (five with individual taps), a final rebuilt SDK-acceptance case and complete signed APK pass;
owned data/display restored. Enforcement remains inactive. Budget view/editor/scoped returning-data and other
ENT-02/03 paths remain open. See [evidence](quality/budget-alert-work.md).

## D-151 - Original budget notification destinations

Budget alerts now carry the opaque actual-file identity and original saved period id. Resolve after unlock and
again in the real budget page; reject another file, deleted identities and failed reads without guessing a
replacement. Retain original currency/calendar/period and monthly navigation coordinates without changing
Home currency or financial rows. Historical/unselected paid methods remain readable without granting new work.
14 new AT-153 cases; main 2,646 / App.Tests 589 pass. Strict Windows and complete strict Android Release pass;
six hidden Windows language/theme cases plus targeted native month navigation, and six normal signed Release
Android cases each tap four exact original currency/calendar/period destinations; owned data/display restored.
Commercial activation stays inactive. Budget view/editor commercial affordances and other ENT-02/03 paths remain
open. See [evidence](quality/budget-reminder-routing.md).

## D-152 - Original budget editor drafts

The original budget editor publishes complete references and defaults behind a loading cover. Exact original
id/file handoff retains archived/missing account/category limits and explicit scopes in both experience modes.
Raw amounts keep their input culture through live display changes. Save rechecks the complete review under the
actual SQLite writer; stale failures retain input and require explicit reload/review. Dirty fields alone survive
reload, unchanged fields adopt current saved values, and a committed form retires before native return.
Current new-tool rights are separate from retained corrections and future-period capacity. Registration remains
inactive. Budget view copy/delete/other work affordances and the remaining ENT-02/03 paths are still open.

35 new AT-154 actual application/isolated SQLite cases pass. Final ordinary main 2,681 / App.Tests 624 pass,
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass
without warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, native
validation/stale Save/Reload/Keep edits/Close and complete 25-table restoration. Six normal Release Android
language/theme cases verify original corrections/archived references; one explicit original-total Save preserves
all other metadata and ledger rows. Final normal Release Create/Close and a targeted weekly Create/Save verify
explicit absent identity and canonical zero stored month coordinates; complete owned data/bytes/display restored.
Physical device, iOS, paid activation and product publication are not claimed.

See [evidence](quality/budget-editor-drafts.md).

### D-153 - Original budget actions follow-up

Original budget view actions now use the complete displayed BudgetEditorReview for copy, confirmed replacement
and deletion, rechecked before/after confirmation and under the existing actual-file writer. Period lookup uses
original calendar/currency/explicit weekly dates and rejects ambiguity. Viewing, original correction and deletion
remain separate from paid new tools and net current-definition capacity; future periods do not consume today's
slot. Busy spans native confirmation through refresh. A committed operation whose refresh fails requires a
read-only Retry, without repeating Copy/Delete. Read-only opening no longer creates Settings; financial history
and all original calculation inputs remain visible. Current commercial registration remains inactive.

26 new AT-155 actual application/isolated SQLite cases pass. Final ordinary main 2,707 / App.Tests 650 pass
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass with
no warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, full native action
names/targets, retained paid history/correction, changed-confirmation rejection and read-only Retry. Six normal
Release Android language/theme cases verify actual original copy/delete confirmations; exactly one owned copy
and deletion of that copy preserve all original 25-table data. Original owned bytes/sidecars/Main/English/System
are restored, final signed Release installed/stopped. ENT-02/03 and remaining resource/operation/import/restore/
native paths stay open. Physical-device, iOS, provider, paid activation and publication acceptance are not claimed.

See [evidence](quality/budget-view-actions.md).


### D-154 - Quick-resource follow-up

Durable explicit quick-template/saved-filter choices retain every original packet, recheck revision/value and
same-timestamp changes, and use the existing actual-file scoped writer. Chosen new slots and explicit filter
replacement are atomic; retained correction/delete/history stay separate. Actual template create/apply/delete
and saved-query save/delete commands use complete reviews and one busy dialog/write/refresh guard. Queries are
captured before prompts; committed refresh failure never resubmits. Template names wrap, publication is covered
and explicit Reload is available. Six growing chooser destinations and help explain retained query history.
Current Release registration stays inactive; no schema, network, SDK, permission or portable grant is added.

29 new AT-156 cases/main 2,736 behaviours (App.Tests 679) pass after repairing one stale four-kind expectation;
only that failed case reran. Strict Windows/complete Android Release have zero warnings/errors. Eighteen hidden
Windows locale/theme/width cases and six normal Release emulator cases restore complete owned rows/bytes/display.
Physical-device, iOS, OAuth, commercial activation and publication gates remain open. ENT-02/03 are still in progress.

See [evidence](quality/quick-resources.md).

### D-155 - Home template follow-up

Home quick templates publish separate original activity availability after the complete parent read. Explicitly
available later definitions remain reachable within the existing six shortcuts; retained history and all original
financial calculations remain independent. A process-local TemplateVisit carries the reviewed actual-file/full
packet to the existing entry route. A changed/deleted original, reference, choice or right, or another actual file,
is rejected without guessing a newer same-id row or saving money. Failed navigation pins translated feedback and
requires explicit read-only Reload. The actual initial chooser receives only its typed resource kind, never a
selection. Destination feedback is visible beside Save; applying a template remains a detached draft.

Opening Home no longer creates or updates Settings for its layout fallback. Existing saved layouts remain exact;
the legacy device layout is read until an explicit profile-layout Save. Commercial registration stays inactive.
No schema, SDK, permission, network request, portable preference or entitlement grant is added.

21 new AT-157 actual-source/real SQLite cases pass. The ordinary main run passes 2,754 / App.Tests 697;
three focused name-order cases bring the verified total to 2,757 behaviours / App.Tests 700. The changed flow
class passes all 21 cases; the unchanged full suite is not repeated. Pending navigation awaits its actual event.
Outputs are cleaned. Strict Windows and complete strict Android Release have zero warnings/errors. Eighteen
hidden Windows Home/chooser contexts and eighteen focused destination continuations cover en/fa/de, light/dark,
360/412/wide. Six normal signed Release emulator cases open original Home templates, fill original amount/title
and cancel/discard without financial Save. Complete owned rows/bytes/display are restored. Physical-device, iOS,
OAuth, commercial activation and publication gates remain open; ENT-02/03 remain in progress.

See [evidence](quality/home-template-destinations.md).

## D-156 - Original device profile review

Original device profile choices now use the existing empty main id and exact original profile ids/names.
The actual Profiles form separates open/rename/delete from an explicit saved activity subset and unsaved choice.
One device preference publication contains the complete catalogue and scoped choice, with separate revisions;
legacy keys remain untouched and every catalogue/startup/backup-set reader uses the new packet after first mutation.
Opening reads only. Complete file/current-profile/provider/catalogue reviews guard publication, both delete
confirmations and creation; authentication is rechecked before committing a switch, with previous-path rollback
when that review retires. Unselected originals remain openable for retained history and correction.

Production device registration remains inactive. Per-file financial/new-money/automation bindings to the chosen
profile remain unfinished; this slice does not complete ENT-02/03 or activate commercial limits. No EF model,
schema, migration, permission, SDK, network request, credential or portable grant is added.

33 new AT-158 actual-source/real isolated SQLite behaviours pass. The ordinary main run passes all 2,790
tests (App.Tests 733), with no failures/skips; outputs are cleaned. Strict Windows and canonical complete Android
Release builds have zero warnings/errors. Eighteen hidden Windows en/fa/de, light/dark, 360/412/wide contexts
check actual native targets, complete growing captions, explicit choice Save, stale Save, retained drafts,
Cancel/Discard/Reload and unchanged complete fixture data. All review processes exit normally and exact owned
development bytes/sidecars are restored. Normal signed Release emulator checks and APK details are recorded in
the evidence document. Physical-device, iOS, OAuth, paid activation and publication gates remain open.

See [evidence](quality/local-profile-work.md).

## D-157 - Actual-file original profile activity

The original device profile choice now composes with the existing verified actual-file commercial source.
The legacy empty main id remains separate from the independently verified financial scope; changing the current
profile preference cannot transfer rights to another file. The immutable context retains the original plan,
resource choices and scope, plus complete catalogue/legacy/provider review facts. Original writer/snapshot
validation therefore rejects retired choices, including change-and-return revisions and changes while waiting
for SQLite ownership. Device choices grant neither paid tools nor shared-space membership.

New money, definitions, selected Plan/Goal/Budget work, template activity and future Plan/Goal reminders consult the same
profile condition. Complete original financial inputs, history, query viewing, corrections, refunds, overdue
settlements, deletion, backup/restore and owned export remain independent. A malformed/unlisted catalogue never
guesses main: new work stops while separately verified historical rights remain. Both current registrations
stay inactive; this slice does not activate limits or complete every operation/import/native binding.
No EF model, schema, migration, SDK, permission, network request, credential or portable grant is added.

31 new AT-159 actual-source/real isolated SQLite cases pass. The ordinary main suite passes all 2,821
behaviours (App.Tests 764), without failures/skips; test output is cleaned. Strict Windows and canonical strict
Android Release builds have zero warnings/errors. Focused original-page Windows and normal Release owned-emulator
results, exact data restoration, retained negative evidence and the complete signed APK are recorded in
[the evidence document](quality/profile-file-work.md). Commercial activation, physical-device, iOS, OAuth,
Store signing/publication and the remaining original-operation bindings remain open.

## D-158 - Original catalogue and period-review reminders

Original catalogue changes now reach the actual reminder watcher, so explicit activity choices rebuild the
queue without another financial action. The shared event port introduces no construction-time database read.
Subscriptions are lifetime-scoped and idempotent; a different catalogue cannot be substituted. Delayed work uses
the injected clock and exposes its actual full batch task for owned-lifetime checks. Disposal cancels future work,
detaches subscriptions and asynchronously drains current work without deleting valid native reminders.

Period-review delivery uses the same captured actual-file paid/profile facts. Complete original preferences,
first-recorded account date, language/formatting/calendar/digits and local day validate before/after native
publication. Revalidation never creates Settings. SDK request refusal is consumed for scheduled requests as well
as immediate alerts; partial pending work is withdrawn and Snooze cache commits only after successful complete
publication. Original financial rows and recovery remain independent. Current registrations stay inactive.
No EF/model/schema/migration, permission, SDK, network, credential or portable format change.

25 new AT-160 actual-source/real SQLite cases pass. The final ordinary main suite passes 2,846 tests
(App.Tests 789), with zero failures/skips, and output is cleaned. Strict Windows and canonical complete strict
Android Release build have zero warnings/errors. Original native Profiles Save drives the real watcher in an
independent fixture; complete rows/catalogue and owned development bytes are restored. Final normal Release
SDK delivery, exact owned restoration, APK details and negative evidence are recorded in
[evidence](quality/profile-reminder-work.md). Commercial activation, physical-device, iOS, OAuth and publication
remain separate. Original Plan/Goal financial-input retirement and other operation/native bindings remain open.

## D-159 - Original reminder input retirement

Original Plan rules/states, Goal/contribution/allocation packets and raw resource choices now retire a
reminder batch when any captured value changes, including changes at unchanged audit times or choice revisions.
Complete original accounts and the date-bounded ledger/quantity inputs used by the existing GoalProgress
calculator are captured in one deferred SQLite reader. Financial data is never filtered by activity selection.
The same coherent reader validates all original packets, preferences and first-data date before/after native
publication; a changed or failed preparation withdraws pending work and preserves the retryable Snooze cache.
Only needed ledger/quantity inputs are read. Future movements remain outside today's calculation.

Snapshot self-mutation rejection belongs to the reminder boundary: existing Goal editors can still mutate their
reviewed draft before the established explicit writer. Full raw selection values distinguish absent/empty
choices. No schema/model/migration, permission, SDK, network, credential or portable preference is added.
Commercial registrations remain inactive and test builds stay unrestricted.

24 new AT-161 actual-source/real SQLite behaviours pass; 100 related reminder/Goal editor/view cases pass.
Final ordinary main: 2,870 passed, zero failures/skips, App.Tests 813, 2m 03.711s; output cleaned.
Strict Windows and canonical complete Android Release have zero warnings/errors. Original running Goals pages
in en/fa/de at 412 pixels/dark verify stale Plan/financial retirement, reached-goal suppression, fresh retry and
complete 25-table restoration. Unchanged layout matrices and successful suites are not repeated.
APK signature/structure and all nine changed production-source PDB checksums match.
Native Release details and retained negative evidence are in the evidence document. Physical device, iOS,
OAuth, paid activation and publication are separate; ENT-02/03 and the commercial release are not complete.

See [evidence](quality/reminder-input-work.md).

## D-160 - Structured write feedback and retained filter retry

Original structured write rejections now explain quota counts, required Plus/Pro/add-on access, exact-space
membership and original profile/resource selection in all six languages. The shared native reporter keeps
unrelated failures on their original path and never guesses purchase, renewal, provider or activation state.
Pre-write Goal/Budget/quick-resource feedback retains the form; committed-return/reload feedback remains
distinct and never falsely says that the accepted write was not saved. Counts use safe Int64 addition,
current formatting/digit choices and direction marks; stored identities and exception details are not shown.

Saving a new filter above capacity now reaches the established reviewed atomic writer for its structured
reason. A rejected operation retires its review; the next explicit menu invocation rereads it without clearing
the current query. Exact raw filter names return to the name prompt. Name drafts are process-local and bounded
by original file identity, independently of rights; another file never inherits them. No automatic retry or
financial save, schema/model/migration, SDK, permission, network, credential or portable preference is added.
Production financial/device registrations remain inactive; test builds stay unrestricted.

19 new AT-162 cases pass with actual sources/resources and real isolated SQLite. Final ordinary main passes
2,889 / App.Tests 832, no failures/skips, 2m 49.493s, followed by cleanup. A preceding main run passed 2,885
before the real second-invocation fix; only the changed final behavior justified another run.
Strict Windows and canonical complete Android Release have zero warnings/errors. Eighteen actual Windows
reason/Close/second-Invoke cases cover en/fa/de, light/dark, 360/412/wide, with unchanged complete 25-table rows
and exact owned development bytes restored. Fictitious menu/name effects are explicit ports; reason presentation
uses the actual native adapter and buttons. Android evidence, negative artifacts and full signed APK are in
the evidence document. Platform/provider/device/commercial activation and publication gates remain separate.

See [evidence](quality/commercial-feedback.md).

Original configured SQLite targets now stay stable across open/closed Android reporting aliases.
The shared Data helper extends the existing D-146 approach to resource reviews, writers, Plan/reminder
packets, loan/backup boundaries and their unchanged foreign-file guards. Four added real SQLite cases
verify open readers/writers, coherent financial validation and rejection of another actual file.

## D-161 - Explain readiness before a blocked Save

New Goal and Budget editors explain their captured capacity, missing feature right or required explicit
selection before Save is disabled. The reason comes from the existing reviewed policy, uses all six real
translations and preserves complete raw drafts. WorkRestriction is an explanation, never a grant or a
replacement for the atomic writer. Existing budget net-definition/correction rules remain authoritative;
malformed bounded Goal choices require explicit repair, while unlimited access remains unaffected.
Production registrations stay inactive and test builds remain unrestricted.

Nineteen AT-163 actual-source/real SQLite cases pass. The final ordinary suite passes 2,908 tests,
App.Tests 851, zero failures/skips, in 2m 05.449s, followed by successful cleanup. Strict Windows and the
canonical complete signed Android Release build have zero warnings/errors. Thirty-six actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, retaining complete drafts and all
25 tables; exact owned development bytes restore. Native Android regression and negative evidence are
reported separately in the evidence document. No schema/model/migration, permission, SDK, credential,
network or portable preference is added. Physical-device, iOS, provider and activation gates stay separate.

See [evidence](quality/pre-save-readiness.md).

## D-162 - Read-only plan comparison and actual access

More now opens Plans and access: a read-only comparison of the owner-approved Free/Plus/Pro model and
original current local access for this exact file. Comparing tiers changes only in-memory presentation;
counts and local tools come from existing Core QuotaPolicy/PlanPolicy. Current inactive test access never
becomes a fake Free/Plus purchase. Personal rights, exact shared membership, the host's right and original
profile choice remain distinct. All history, correction, backup/restore and owned export rules stay intact.

Only built local tools are compared. Unavailable Pro online services are an explicitly separate roadmap;
optional cloud backup belongs to every tier. Plus Lifetime is the same local Plus set, without invented
renewal. No price, purchase, trial, renewal, cancellation, restore-purchase or provider status is inferred.
Continue returns without changing a plan or buying anything; actual Free has the Continue with Free caption.
The existing explicit active-item form opens without saving. Production registrations stay inactive.

Seventeen AT-164 actual-source/real SQLite cases pass. Final ordinary main: 2,925 passed, App.Tests 868,
zero failures/skips, 2m 05.555s, followed by successful cleanup. Strict Windows and canonical full signed
Android Release have zero warnings/errors. Eighteen actual Windows contexts cover en/fa/de, light/dark,
360/412/wide, all three native tier choices, complete realized row captions/values and native help/return.
Actual Release Android More/chooser/comparison/help/Continue and exact owned restoration are verified.
All eight changed production-source PDB checksums match. No schema/model/migration, permission, SDK,
network, credential or portable paid data is added. Device, provider, billing and activation gates remain
separate. ENT-04 and the commercial release are not complete.

See [evidence](quality/product-plan-comparison.md).

## D-163 - Original editor write feedback and committed return

Account and Plan Save now explain actual structured quota/permission rejection in all six languages,
retain complete raw drafts and place complete feedback beside the pinned Save. Successful writes cannot
be resubmitted after a failed return or follow-up; inputs become read-only and Cancel becomes Close.
Account/default-preference boundaries stay distinct, including a default update that committed before
its subscriber failed. Original change-notification failures after commit remain visible through a typed
committed outcome, never a fake pre-write rejection. Exceptions are not silently swallowed.

Both editors use original native-effect ports and actual platform-independent application sources.
Native query interfaces stay on pages. Plan picker rows retain only their actual plain-text identity/name/
selection, without unused native color/icon allocation. Account and Schedule edit copies preserve all
original fields/identities; Schedule also clones its mutable rule. A refused historical split cannot mutate
the original in-memory state, stop boundary or automatic-post anchor before an explicit corrected retry.
Original validation, signs, calendars, currency locks, category selection, split rules and atomic writers
remain authoritative. No EF property/schema, migration, permission, SDK, credential or portable-format change.

21 AT-165 cases pass; final main passes 2,946 / App.Tests 889, zero failures/skips, 2m 07.788s, followed by
cleanup. Strict Windows and canonical complete signed Android Release have zero warnings/errors.
36 actual Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Actual Save invokes
original atomic rejection/accepted writers over independent fictitious SQLite; raw drafts, pinned captions,
real input/Save enablement and Close are checked. Explicit fictitious return/consent effects are identified;
all 25 original tables and exact owned development bytes restore. Actual normal Android new-form/query
regression and exact owned restoration are separate from typed fixture/physical-device acceptance.
Production registrations remain inactive. Initial pre-attempt readiness and other operation/activation
coverage remain open; this does not complete ENT-04 or the commercial release.

See [evidence](quality/editor-write-feedback.md).

## D-164 - Reviewed atomic account archive

Account archive is now one reviewed SQLite transaction: original archive state, affected active/paused
plans in either account direction, exact default cleanup and original persisted activity choices commit
or roll back together. The reader captures complete original account/plan values and the local day without
creating settings. The writer reacquires/rechecks complete inputs under the actual file's writer and rejects
retired rows, day, cached access, another file or a different original notification owner.

Original Account/Plan feature/capacity decisions are shared, not duplicated. Archiving ends related plans
with established PlanActions; unarchive consumes the original account slot but never resumes ended plans.
Another concurrently chosen default remains untouched. Both original stores publish only after commit;
publication failures retain the accepted result and cannot cause a repeat. The UI blocks concurrent actions,
requires explicit consent before discarding a dirty draft, never silently saves those financial inputs,
and freezes an accepted result with Close after follow-up/return failure.

17 AT-166 cases pass. Final main: 2,963 passed, App.Tests 906, zero failures/skips, 2m 10.406s, followed by
cleanup. Strict Windows and complete signed Android Release have zero warnings/errors. 36 actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced with original native Archive/Close,
real SQL rollback and actual accepted account/plan/default writes over independent fictitious rows.
Explicit fictitious consent/alert/return effects are identified. Actual normal Android consent-decline
and exact owned restoration remain separate from typed fixture/physical-device acceptance. All four
changed production-source PDB checksums match. No EF property/schema, migration, permission, SDK,
credential, network or portable-format change. Production registration stays inactive; initial readiness
and other commercial paths remain open, not release completion.

See [evidence](quality/account-archive.md).

## D-165 - Account/Plan pre-attempt readiness

Account and Plan forms now explain original feature/capacity/selection decisions before an attempted
Save. The read-only review uses each original writer's actual configured file and cached access. Active
reads capture complete original rows and persisted choices in a deferred SQLite reader; inactive test
access performs no financial-row/choice read or paid activation. A missing persisted choice counts all
original eligible rows, matching the writer instead of a cached fallback subset. Original correction,
retained advanced-rule and historical net-slot behavior stays available.

The original advanced Plan classification is shared through Core PlanEditPolicy. Save and readiness build
the same detached candidate; readiness does not parse money, validate financial input, mutate original
state or grant a write. Explicit Refresh access keeps every raw amount/date/note/reminder/selection.
A failed or retired read disables Save with translated feedback; an actual writer rejection still retains
the draft and requires explicit fresh review. Atomic writers remain the final authority. No EF property,
schema/migration, permission, SDK/network, credential or portable-format change. Test limits stay inactive.

Fifteen AT-167 cases pass with actual application sources and isolated SQLite. The original 21 AT-165
cases also pass: known full quotas now disable Save before attempting it, so original writer-rejection
cases capture a free slot and then exercise a real later addition/access change. They still prove original
atomic rejection and an explicit corrected retry. Final main: 2,978 passed / App.Tests 921, zero failures
or skips, 2m 12.110s, followed by cleanup. Strict Windows has zero warnings/errors.

Remaining commercial bindings, activation and external/device acceptance stay open. See
[focused evidence](quality/editor-readiness.md).

## D-166 - Retained original import and Undo outcomes

Original ledger import, holdings import and durable Undo now retain the original accepted result across
post-commit change-subscriber failure. A typed completed notification carries the exact counts/batch or
Undo result; original atomic writers, metadata, overlap rechecks, rights and rollback stay unchanged.
The application retires the accepted reviewed payload before any history/failure follow-through and
removes an accepted Undo row before fallible reload. A retained old command cannot repeat that batch.
A complete history read publishes only after success, preserving other original rows on failure.

Structured pre-write rejection is shown beside the retained preview in all six languages. Successful
follow-through failures state the completed outcome instead of pretending it rolled back. Picker
cancellation keeps the existing file/preview/choices. One busy lifetime spans Undo consent/write and
export authentication; explicit fresh review retires failed original evidence without silently saving.
Picker, existing app-lock confirmation and UTF-8 BOM cache/share effects are native adapters. Actual
non-UI application sources are compiled and tested through explicit ports, never stand-in MAUI controls.
Pinned complete feedback/review/import actions reserve a growing row outside the viewport. Original
history identity and growing Undo action have separate rows. No new feature policy, paid activation,
schema/model/migration, SDK/permission/network, credentials or portable-format change.

32 AT-168 cases pass with actual application sources, real isolated SQLite and all six resource languages.
They cover original ledger/holdings permission refusal and retry, post-commit publication outcomes,
nonrepeatable accepted import/Undo, real history-read failure with other batch retention, picker/consent
cancellation and busy guards, explicit read-refusal/retry, security/export ports and neutral complete CSV,
real SQL-trigger whole-transaction rollback, plus explicit aggregate links and complete restored metadata.
Final ordinary main: 3,010 passed / App.Tests 953, zero failures/skips, 2m 42.936s, followed by cleanup.
Strict Windows and canonical complete Android Release have zero warnings/errors. All seven changed
production-source PDB checksums match. Device/platform/provider acceptance remains separate.

Other commercial bindings and external acceptance remain open; see [evidence](quality/import-outcomes.md).

D-167 completes original backup restore outcome handling and nonreplacing completion retry.
35 AT-169 cases/main 3,045 pass; strict builds, 36 Windows contexts, actual Release Android recovery and
complete signed APK verified. Current ENT-02/03/04 scope is audited next; external gates stay separate.
See [evidence](quality/restore-outcomes.md).

## Forecast/snapshot follow-through (D-169)

The original computation/atomic Save/history/Delete group is verified locally; retained Free data and
currency-safe legacy comparison are preserved. No commercial activation or external release acceptance.
Main 3,074 tests, strict builds, native Windows/Android evidence and complete signed installable APK:
artifacts/android/zanance-d169-release.apk. See [evidence](quality/forecast-work.md).
Next finite ready group: receipt recognition, followed by loan/report/PDF.

## Shared receipt follow-through (D-170)

Original Home/attachment recognition and typed editor source validation are delivered locally, with
original no-Save review and manual attachment policy intact. Main 3,104 tests, strict builds, 36 native
Windows contexts and both actual digital-PDF Release Android callers pass. Complete signed APK:
artifacts/android/zanance-d170-release.apk. [Evidence](quality/receipt-work.md).
Phone/iOS/camera/OCR corpus/provider/activation/release gates remain open. Next: loan/report/PDF.

## Original loan follow-through (D-171)

Loan analysis/confirmed payment writer are locally verified: main 3,141, strict clean builds, 36 native
Windows contexts and actual signed Release Android schedule/consent/ledger readback. Original balances,
manual repayment and essential warnings stay Free. Full signed APK: artifacts/android/zanance-d171-release.apk.
[Evidence](quality/loan-work.md). Next ready operation: reports/advanced PDF. Full release/external acceptance remains open.

## Original report/PDF continuation (D-172)

28 AT-173 application/real SQLite cases; ordinary main 3,169 / App.Tests 1,112 pass with zero
failures/skips (2m 42.749s), followed by test-output cleanup. Final strict Windows and canonical complete
Android Release builds have zero warnings/errors. Thirty-six en/fa/de, light/dark, 360/412/wide and
Simple/Advanced Windows contexts exercise real package targets, original calculations, Free refusal
without changing selection, detached actual PDF and unchanged original inputs. Final targeted PDFs
also retain Free account opening/movement/closing tables and per-account currency scopes; native goal
suggestion acceptance is checked in en/fa/de with its named-calendar rule and no ledger posting.
Normal signed Release Android exercises original report/history, actual PDF cache/platform share,
share-sheet cancellation and complete retained choices. Owned tables, cache, baseline bytes/sidecars
and Main/English/System are restored; final Release installed/stopped. Commercial registration remains
inactive. Physical phone/iOS/OAuth/billing/Store/performance/full-product acceptance remain separate.
The confirmed D-168 report portion is delivered; finite ENT-02/03 completion review is next.
Owner/external dependencies and full commercial-release acceptance remain open.
[Evidence](quality/report-work.md).

## D-174 native quick widget accessibility

Canonical A11Y-01 is in progress: actual widget button roles, targets, translated names/direction and
cold receiver/original intent checks are delivered locally. No spoken TalkBack or full accessibility
pass is claimed. Main 3,169 and full signed APK; [evidence](quality/android-widget-accessibility.md).

## D-175 date accessibility naming

Existing date purposes now identify actual child inputs/calendar actions in all 26 usages. Native
Windows/Release Android naming and no-write checks plus full signed APK are delivered; spoken
screen-reader and system-scaling/platform acceptance remain open. [Evidence](quality/date-context-accessibility.md).

## D-177 original Home section reuse

Remove redundant native section detachment on repeated appearance; retain existing saved order,
visibility and original financial inputs. Main 3,169, strict builds and 18 Windows contexts verified;
six normal Release Android Home/return cases and exact owned-data/display restoration verified. Rejected startup candidates are removed, not delivered.
Canonical QA-06 and external acceptance remain open. [Evidence](quality/home-layout-reuse.md).

## D-178 original asset-event slices

One projection-local type index preserves original quantity formulas/date/order/units while avoiding
repeated complete-history scans. Ten new cases, main 3,179, strict builds, bounded Core measurements
and six Windows caption contexts verified; Six normal Release Android caption/return cases and original Main/fixture/display restoration verified. Canonical QA-06
and external acceptance remain open. [Evidence](quality/asset-event-index.md).

## D-179 live original icon names

Fix the observed stale constructor-time name on retained icon controls. Native Windows/Release Android checks and complete APK verification pass locally; A11Y-01/02/03 remain open for broader/spoken/platform
acceptance. [Evidence](quality/icon-language-accessibility.md).

## D-180 Community evaluation prerequisite

Owner direction selected; exact official source inputs and native host restoration are recorded.
Independent Windows C/.NET compatibility and original backup/export behavior verified; main app
remains unchanged/plaintext. Continue native production recipe/package/EF/mobile verification before
keys and crash-safe migration. SEC-01 and full release remain open.

## D-188 generation checkpoint foundation

Eight cases/full 3,205 pass for explicit immutable ordered generation transitions. Final durable
key/selector/cleanup/recovery coordinator remains open; live app data stays plaintext. No UI/schema
or commercial activation. See the canonical commercial-release backlog and AT-187.

## D-189 resumable migration coordination

Fifteen new/full 3,220 cases pass for exclusive native-effect coordination, stored checkpoint
readback and fail-closed selected-key behavior. Native adapters/real restart/key/recovery policy
remain open. No UI, live encryption/provider/backup or commercial activation.

## D-190 Native CI ownership correction

Actual hosted Permission denied root cause corrected through Linux non-root host ownership and
fresh work mounts. Six runtime workspace checks pass; corrected full hosted native run pending.
Application/3,220-test/build/APK baseline unchanged. Canonical security/release gates remain open.

## D-191 native parent traversal

Actual hosted/local foreign UID failure corrected by traversal-only permission on immutable build
home. Seven focused checks/host identity pass. Full hosted
[38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) succeeds with matching ABI builds, twelve ELF refusals and
226 cipher tests/zero errors; app baseline unchanged.

## D-192 approved recovery policy and Windows key protection

Automatic local key/optional recovery/Zanance backups and final coupled Android/iOS OS exclusion
approved. Windows CurrentUser/context primitive and real two-process encrypted reopen pass;
full 3,232. Actual native storage, other platforms and final migration/recovery integration remain.

## D-193 Android key protection

Original Android Keystore primitive and separate Release encrypted reopen/fifteen behavior proof
pass; software emulator security level reported explicitly. Main strict builds pass, unchanged
managed baseline retained. Durable files/recovery/coordinator and iOS remain; app plaintext.

## D-194 Android envelope storage

Actual immutable private-file key storage added; independent process reopen/eight file behaviors
checked. Orphan fixture is not an injected publication crash. Selector/recovery/migration and
other platform files remain; live database unchanged/plaintext.

## D-195 transactional migration selector

Concrete non-secret SQLite protocol storage and actual native hot-journal/committed-selection
kill/restart pass on Windows and independent Release Android. Complete native session/migration,
optional recovery and all-profile/physical/iOS acceptance remain. Main remains plaintext.

## D-196 concrete native migration session

Original-model Android session now performs actual protected keys, encrypted candidate/rollback,
verified selection/reopen and exact cleanup, including real process interruption and missing-key
refusal. No main/provider/all-profile adoption; backup/recovery and broader acceptance remain.

## D-197 protected EF factory

Atomic path/credential publication and original factory actual native read/write/retirement pass.
Permanent legacy plaintext backup constraint enforced. Main remains plaintext pending complete
provider/lifecycle/all-profile/portable backup/recovery composition; no temporary OS policy.

## D-198 protected portable backup writer

Actual keyed source/commercial wrapper memory export and typed native comparison/retired-output
stop pass. No plaintext temporary file; optional password unchanged. Main/profile/provider/restore/
recovery/OS policy composition remains; no current user backup restriction or temporary exclusion.

## D-199 encrypted restored staging

Actual bounded portable import/new local key/typed verification/32 schema migrations pass native
old-backup proof. No live selection or original mutation; final receiver dispatch/rollback/catalogue/
provider/all-profile/recovery/OS policy composition and platform acceptance remain.

## D-200 restored selector and encrypted rollback

Actual current/restored selection and existing-key rollback verification/restart pass in independent
Release Android. Preserve original and Selected for outer package transaction. No complete dispatch/
source rollback/retention/provider/main/all-profile/recovery/OS policy acceptance.

D-201 connects an optional complete-package receiver to the actual BackupService after validation,
without per-source fallback. Main registration, durable package staging/publication and SEC-01
acceptance remain incomplete; this is not a new released restore feature.

D-202 adds the protected two-source package receiver and retained encrypted preference replay.
Main lifecycle/provider registration, pre-capsule interrupted staging reconciliation, global lease,
safety/retention and recovery UI remain open; SEC-01 is not complete.

D-203 factory leases/owned maintenance contexts are connected to the actual DI/context disposal path.
Main lifecycle/profile orchestration, provider adoption, key/retention/recovery/OS policy and SEC-01
acceptance remain incomplete. No native warning acceptance or encryption activation follows.

D-204 original profile switching now owns factory maintenance, including actual migrations/settings,
authentication/catalogue commit and rollback. Final protected catalogue/provider/startup/key/retention
activation and SEC-01 acceptance remain open; no encrypted main-profile claim.

D-205 actual confirmed inactive-profile deletion awaits maintenance/context drain and revalidates
reviewed inputs. Protected generation/key/rollback deletion/retention and main encryption lifecycle
remain incomplete; existing file-error cleanup policy is unchanged.

D-206 durable initial installation/profile/key-generation identities are implemented, with main
empty-id mapping and idempotent registration. Main startup/bootstrap/live selector/provider and
protected retention/recovery/deletion/OS-policy activation remain incomplete.

D-207 composes real initial identity/journal/key/migration/cleanup with retained-key factory
publication. Main startup/provider/live restored references and final security lifecycle acceptance
remain incomplete; main databases are still plaintext.

D-208 durable restore intent/ancestry is connected to actual package receiver checks and selected
journal readback. Main live resolver/provider/startup, durable one-time preference replay/later edits,
key/reference retention/deletion/recovery and final OS-policy acceptance remain incomplete.

D-209 adds one-time encrypted replay completion after actual preference persistence; later edits
are preserved. Main durable platform adapters/startup/provider/live selection and final recovery/
retention/OS-policy acceptance remain incomplete.

### D-210 — Persist original Android MAUI preferences before recovery completion

The Android host now registers ISettingsPersistence against the exact Preferences.Default backend
used by MAUI 10.0.110. Native PreferenceManager selects the same default SharedPreferences; a custom
IPreferences backend is refused rather than writing a guessed file. Persistence runs off the UI thread.
A fresh device-local operational marker forces a real snapshot write, rather than an empty editor
that may skip writing. Commit failure or cancellation throws; the protected receiver must not mark
its capsule Applied or publish until this completes under exclusive preference-writer ownership.
The marker is neither a secret nor a portable preference. Windows/iOS persistence, live generation
resolution, final provider/startup/recovery and sensitive-file OS exclusion remain open. The main
application database remains plaintext; registration alone does not activate encryption.

Focused acceptance compiles the original MauiSettingsStore and native persistence adapter with
actual MAUI Essentials 10.0.110 in the independent Android package. It writes only fictitious values,
checks cancellation, terminates the process, verifies a retained value and removed key in a cold
process, then cleans its own values. This is Android x64 API36/4 KiB evidence, not phone, iOS,
hardware-key, whole-application encryption or product-release acceptance.
### D-211 — Retain the restored generation through cold startup composition

SqlCipherProfileGeneration.OpenRestoredAsync resumes the actual latest package under the current
location's exclusive maintenance owner. It requires the receiver's exact private restore-history
store and installation/profile key namespace before native effects. Pending preference replay and
actual candidate/rollback/journal verification finish before retaining the exact existing key.
Readback requires the latest selected target; missing or mismatched history fails closed. No original
or initial-generation fallback, key preparation, directory guessing or early publication is added.
The retained key owner verifies the provider/integrity again and publishes through the same original
protected factory lifetime used by initial migration. Its disposal still refuses new connections.

The receiver has an internal no-publication resume path for this composition. A stale publication
callback from another process is never invoked; the current host publishes only after obtaining the
retained key owner. The public restore entry retains its existing behavior. Automatic all-profile
startup routing, main provider adoption, early staging reconciliation, optional recovery and retained
file/key lifecycle remain open. Main application storage remains plaintext. This change is not a
claim of complete encryption, device/iOS/OAuth or release acceptance.
### D-212 — Resolve the current protected generation from stable ancestry

OpenCurrentAsync reads the actual existing installation/profile catalogue and requires history's
complete initial key binding. Missing/corrupt/foreign history never implies initial selection and
cannot manufacture migration state. Row zero routes through actual initial migration/reopen;
any recorded restore intent routes through the captured latest receiver and retained restored owner.
Receiver target/parent/private directory and exact history are validated, including unresolved intents.
No enumeration, latest-file guess, key replacement or initial/plaintext fallback is introduced.

The host explicitly initializes history only after verified initial migration; interrupted bootstrap
still fails closed for reconciliation. This automatic composition is not yet registered in the main
startup/provider/profile lifecycle. Native producer acceptance, all-profile live adoption, recovery
and retained-file/key policy, early staging reconciliation and final sensitive OS exclusion remain.
No additional owner decision or commercial activation follows; main storage remains plaintext.
### D-213 — Preserve protected publication when a profile switch is refused

The original profile switch captures its complete publication inside the actual maintenance owner,
then asks the native runtime to open/migrate the destination. On authentication refusal, retired
catalogue or failed opening, rollback restores the original path and bound connection creator.
A path-only MoveTo cannot restore encrypted storage and is no longer used for this rollback.
Rollback is one-use, requires its exact live owner, never revives retired protected context identity,
and refuses a plaintext snapshot after protection is required. The host retains underlying key
owners until the transition finishes; no credentials are exposed by the publication token.

IProfileRuntime.OpenDatabaseAsync is the explicit native destination effect. The existing default
opens/migrates plaintext storage; a protected location rejects it. Final protected hosts supply
verified keyed publication, never strip protection or recreate missing plaintext. The actual MAUI
host's protected generation resolver/lifetime registration and final startup/all-profile/recovery/
retention/provider/OS-policy adoption still remain open. Main database encryption is not activated.

Concurrent initial Android/test builds hit CS2012/MSB3248 locked shared DLLs; those negative logs
are retained. Successful Windows and independent Release builds were not repeated. Only failed
Android and full test executions were retried once, sequentially after competing builds completed.
### D-214 — Own active and tentative profile keys through the real transition

SqlCipherProfileSession retains the active verified generation while a destination is tentative.
The actual ProfileService invokes finalization after committing its reviewed catalogue or restoring
its complete previous publication. The MAUI runtime forwards opening/finalization to the registered
protected session; existing plaintext hosts retain their original path and never bypass protection.
Accepted switching clears the previous mutable key owner; refused switching clears only the
candidate after the original publication is verified. No envelope, database or rollback is deleted.

Start, switch, acceptance, refusal and close require the actual live maintenance owner. Key retirement
also requires all owned contexts to be disposed. Concurrent resolution is serialized and synchronous
finalization refuses active resolution. Publication identity must belong to the exact retained owner;
keys cannot be retired based on a path string alone. Main registration/covered async startup, early
bootstrap/replay reconciliation, recovery/retained-file lifecycle, native provider and final OS policy
remain open. The main database is still plaintext; this is final lifecycle composition, not activation.

A protected host may resolve another catalogue-bound profile in the same private directory while
retaining the previous publication. It never passes through an unkeyed endpoint. Plain/foreign-root
initialization guards remain enforced, and actual journals, history, keys and files must still verify.
### D-215 — Record initial ancestry before native key migration

Initial ancestry can now be captured as pending against the exact existing Original journal,
before keys/candidates are prepared. Pending row zero survives restart but cannot reserve a
restore transition. OpenCurrentAsync resumes the actual initial migration, retains/reopens its
existing native key, then mirrors Completed journal state into selected initial ancestry before
returning a publication owner. Missing/foreign history still never falls back or regenerates.
Existing verified initial history remains supported; schema/version is unchanged and typed guards
still apply. New pending state is not evidence of a completed migration or permission to open data.

The old ordering left a crash interval after initial migration but before history creation. The
new pre-key intent closes that interval through actual captured resume. Interrupted metadata
creation itself, all-profile host bootstrap, recovery UX, provider/live registration and OS policy
remain separate unfinished work; missing or corrupt records still fail closed. Main application
storage remains plaintext. No owner decision, platform key policy or financial schema changes.
### D-216 — Publish complete identity metadata from captured staging

Identity bootstrap now commits and fully validates a private .creating catalogue before publishing
its final path with a no-replace same-directory move. A valid staged catalogue is resumed with its
original installation/profile/generation identities and exact known profile set; no new random ids
are generated on that path. Missing/corrupt/foreign incomplete staging fails closed rather than
being overwritten. Existing final metadata or sidecars still reject new bootstrap. Read never creates.

An explicit native durability overload synchronizes staged file/directory before rename and the
parent after publication. Managed-only callers retain transactional/typed/no-replace checks but do
not claim platform power-loss qualification. Failure after final rename requires actual existing
read/reconciliation, not another CreateNew or replacement. Other journal/history creation protocols
and main lifecycle/provider/OS policy adoption remain unfinished; main storage remains plaintext.

Independent Android x64 API36/4 KiB source-built SQLCipher probe pauses after actual fsync and typed
staged read, before final publication. The external runner terminates that process. Cold resume
requires the final path absent, publishes the same staged identity fingerprint, verifies actual
initial journal binding and idempotent/new profile registration preserving installation. No keys
are prepared, app encryption activated or scenario repeated. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-58220434415e4174bc471b9760cd95e6.json,
run f93ba289610e4355aee1b0446d65bc9a. Reviewed APK ABI identities/signature verified; independent
package uninstalled. Strict Windows 26.63s, Android 17.07s and independent Release 71.92s builds
have zero warnings/errors. Script parser checked; test cleanup waits for terminal builds/tests.
### D-217 — Publish complete migration and history metadata

Generation journal creation and initial restore-history creation now use the same verified staged
publication as identities. Existing/corrupt/foreign final files and sidecars are never replaced;
read never creates. Captured Original journal state and exact initial pending/selected history
must match on staged resume. Advanced journal staging cannot become a fresh Original, and a pending
history cannot be republished as verified initial selection. Typed schemas/versions remain unchanged.

Native overloads synchronize file and directory around no-replace publication. Managed-only effects
do not certify native power loss. Incomplete/corrupt staging still fails closed for reconciliation;
this does not regenerate missing keys or certify complete startup/recovery. Main provider/all-profile
bootstrap/covered async startup, recovery/retention and final OS exclusion remain open/unactivated.

Independent signed Release AOT/trim Android x64 API36/4 KiB interrupts journal publication after
actual staged fsync, resumes that exact journal, then interrupts pending-history publication after
its actual fsync. Third process resumes both bindings, retains pending initial ancestry and confirms
staging aliases gone, without preparing keys or writing financial data. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-442b5d696a9a4a8699baf81c8b31218d.json,
run 18d182139b834d9b84bd34178fd54ef1. Runner clears only its own prior metadata barrier before
its second phase; no scenario repeat/reinstallation for failure or unrelated native suite/UI matrix.
ABI identities/signature verified and package uninstalled. Strict Windows 18.74s, Android 17.54s
and independent Release 59.16s builds report zero warnings/errors. Parser checked.
### D-218 — Compose preparation of all captured existing profiles

SqlCipherProfileBootstrap joins actual identities, staged journal/history creation, pre-key initial
intent, concrete migration/current-generation resolution and temporary verified key ownership for
every captured existing profile. The actual host maintenance owner excludes ordinary contexts;
private unpublished locations supply local ownership without moving the application's factory.
Only after this pass may the host's protected session publish the selected profile.

The original map must be complete, distinct, absolute and in the real location's private directory.
Existing metadata must match the captured profile catalogue. Missing installation is not inferred
as fresh: explicit initialization still refuses missing/redirected/non-plaintext originals or retained
key/migration/history/cipher state, unless resuming captured valid catalogue staging. Read-only
startup never creates identities. Missing financial databases are never manufactured here.

Native sync qualifies metadata creation; actual journals/history/current files and keys are reopened.
All encrypted rollback/envelopes remain retained, and temporary key owners are disposed before the
next profile. No intermediate application publication or fallback is added. New empty profile
creation, full restore callback/lifecycle registration, covered main async startup, provider adoption,
recovery/retention and OS policy remain open. Main application storage is still plaintext.

### D-219 — Initialize new profiles without plaintext financial files

SqlCipherNewProfile creates the actual current schema in an unpooled in-memory database and exports
bounded memory into verified encrypted candidate and rollback files. A dedicated new-profile intent
captures explicit creation separately from existing-profile migration. Missing existing state never
authorizes an empty replacement. Existing originals/sidecars, unknown ciphertext and foreign or
selected ancestry fail closed before schema/key work. Native synchronization qualifies checkpoints
and the no-replace rename into the existing generation journal. The normal current-generation owner
then verifies the retained key before application publication.

An interrupted unpublished empty-schema attempt may rebuild only under its captured KeyPrepared
intent, using the existing key. The original financial path never exists on disk. Both encrypted
packets are fully compared before selection; later ordinary writes are retained on cold open.
This infrastructure is not yet registered in the main product's profile creation/startup. Main
storage remains plaintext; provider adoption, lifecycle/catalogue registration, optional recovery,
retention and coupled final OS backup policy remain unfinished. No new owner decision is required
for this step; OD-04 and OD-10 remain the approved final policy.

### D-220 — Prepare new protected profiles before catalogue publication

The real Profiles Add command now awaits ProfileService.CreateReviewedAsync. Automatic posting is
serialized, existing contexts drain, native preparation finishes without moving the active endpoint,
and the original catalogue/rights are rechecked before publishing the new profile and selected slot.
No await holds the synchronous catalogue gate. Failed preparation, cancellation or a retired review
cannot publish the proposed profile. The synchronous legacy creation API refuses protected storage.

SqlCipherProfileSession retains its current key while an explicit native new-profile callback prepares
an unpublished destination; pending transitions or unavailable callbacks fail closed. MAUI forwards
to this path only when the protected session is registered. Existing plaintext hosts preserve lazy
creation. LocalDatabaseLocation exposes protection status without exposing credentials or creators.
No page layout, strings or financial algorithm changed.

The final native host must retain/reconcile uncommitted identity/creation artifacts after a rejected
publication, rather than deleting referenced keys or treating missing history as a fresh profile.
This step does not register that host, activate main encryption, implement orphan reconciliation,
optional recovery, retention or coupled OS policy. These remain in the full SEC scope.

### D-222 — Cover the first window while preparing storage off its thread

CreateWindow now constructs only a non-financial StartupPage. OnStart runs the actual migration and
initial settings/display-unit persistence through a worker before creating financial content and
reading secure access state. Foreground posting, reminder refresh and theme/language rebuilds are
suppressed until preparation and the existing app-lock/router setup finish. Existing PIN protection
and deferred links remain separate; link navigation failures use the established generic feedback
and do not misclassify an already prepared database as failed startup.

The real loading/failure/retry surface uses dynamic theme colors, wrapping scalable text and a native
button. StartupViewModel retains the cover on preparation failure, exposes no exception payload,
rejects overlapping attempts and never reruns a completed preparation. Three new translated messages
exist in all six languages. The initial prepared settings row also supplies display units; no extra
startup settings read is added by display-unit publication.

IAppDatabaseStartup is the final initialization port. The existing plaintext host migrates only the
current legacy database and explicitly refuses a configured protected session/publication. The final
native host must replace this registration with complete captured-profile bootstrap. Main SQLCipher
provider/key/recovery/retention/OS integration remains unactivated. This is actual startup integration,
not a claim of completed encryption, the two-second performance target or platform/device acceptance.

Diagnostic routes now await actual startup before accessing data. A Debug-only thread observation
records actual storage work on a different thread from window creation. The startup review renders
real controls with fictitious preparation outcomes, invokes the native retry target and compares all
stored table rows. Run-Snapshots.ps1 -UseExistingBuild reuses only a caller-owned verified Windows
Debug executable for distinct layout cases; default behavior still builds and fails on build errors.
Callers must not infer success from a stale LASTEXITCODE when no native build command was run.

### D-223 — Device-key protection for internal restore safety packages

BackupService now accepts an explicit IBackupSafetyCopyProtection port. Safety-only creation builds
its ZIP in bounded memory, protects it before the first storage upload and clears its owned package
buffers. Portable backups retain the independent optional password choice. DeviceKeySafetyCopyProtection
uses a separate durable platform key per safety copy, bound to the captured profile/installation and
fresh generation; it never borrows the live database key. Opening reads only the existing key and
rejects foreign contexts before key access. No missing-key preparation or plaintext ZIP fallback occurs.
The envelope uses standard AES-256-GCM with a fresh nonce and authenticated non-secret binding/header.
Creation and safety extraction bound cumulative source bytes and package bytes to 256 MiB. Failed
manifest/checksum validation clears materialized entries. This is a format/storage integration, not
a custom cryptographic algorithm or a replacement for SQLCipher.

Two real safety uploads in one second exposed the existing timestamp filename overwrite. Protected
copies now have unique opaque .vbs names outside portable discovery/retention. Generic portable pruning
cannot delete these referenced recovery artifacts. Final native registration still needs private
storage, reference-aware retention and orphan reconciliation; skipping pruning is not that final policy.

Focused Backup tests pass 54/54 and Data tests 169/169. The independent signed Release AOT Android
package uses the actual BackupService, DeviceKeySafetyCopyProtection and AndroidGenerationKeyStore.
Two protected copies restore exact fictitious content, tamper fails authentication, and a cold process
reopens both using a port that forbids key preparation. The cold observation deadline expired; the
same process subsequently wrote its successful result, read without restart. Negative timeout evidence
is retained. Native build has zero warnings/errors. Proof: artifacts/native-sqlcipher/d223-safety-native-proof.json,
run 26da5f287da548f09acdf6abd6d37bf4. The runner now gives these cold phases its established longer bound.

Main protection registration remains inactive. Main database/provider/session/bootstrap, final
Windows/mobile matrix, optional recovery, key loss, reconciliation, retention and coupled OS exclusion
remain unfinished. SEC-08 is in progress; this does not close it or authorize product release.
No visible UI changed; unchanged layout matrices and upstream native producer suites were not repeated.
### D-224 — Protected startup composition

SEC-01 remains in progress. The actual IAppDatabaseStartup implementation now composes all-profile
bootstrap and selected protected-session publication, independently of commercial availability.
Both initial and cold Android probe runs pass with two original profiles and the selected work
amount preserved. Main registration, new-install handling, reconciliation, recovery and final OS
policy are not yet activated. See D-224 in 01-assessment-and-decisions.md for exact evidence.


### D-225 — Encrypted fresh-install startup composition

SEC-01 remains in progress: the real protected startup contract now supports explicit new
installation with the actual schema created in memory, and distinct existing/legacy authorities.
Missing existing metadata never permits replacement. Three authority tests pass; native fresh
and cold phases use retained fictitious data. Final main provider/lifecycle registration, orphan
reconciliation, restore routing, optional recovery, safety retention and coupled OS policy
remain open. Detailed evidence is in D-225 in the decision log and specification Section 31.


### D-226 — Windows key and synchronization effects

SEC-01 remains in progress. Actual Windows generation-key/file effects now persist immutable
CurrentUser DPAPI envelopes with owner-only ACLs, actual file/directory flush and existing-key
reopen. Nine native Windows tests and the separate-process encrypted native packet pass.
Final main adoption, Windows narrowing-warning review, profile lifecycle reconciliation,
optional recovery, retained-file policy, coupled OS exclusions and external platform/device
gates remain open. Exact proof is in D-226 in the decision log and specification Section 31.


### D-227 — Windows complete native startup and pool cleanup

SEC-01 remains in progress. Windows fresh/cold startup and interrupted two-profile migration
now run actual app startup and final native effects. Observed original deletion failure from
an idle pool is corrected by scoped bootstrap pool retirement; same-state resume and later
reopen succeed without replacement keys. Shared native fixtures remove platform duplication.
Main adoption, reconciliation, recovery/retention/OS policy, native warning review and external
platform/device gates remain open. Exact evidence is recorded in D-227 in the decision log.


### D-228 — Initial publication observer retry

SEC-01 remains in progress. Actual native failure after initial endpoint publication is now
recoverable with the retained key, unchanged prepared catalogue and observer replay. Native
Windows checks prove data/key/endpoint retention and no repeated key reads; shared Android
scenario verifies the same contract. Main registration, reconciliation, recovery/retention/OS
policy and remaining native/platform/device gates remain open. See D-228 in the decision log.


### D-229 — Selected restore adoption by the active session

SEC-01/SEC-08 remain in progress. Actual whole-package receiver now closes the old connection
before final adoption. Protected session validates/retains the exact restored key and rejects
profile-switch completion of its pending restore. Independent Windows restore/observer-error
resume/cold read pass without replacement keys or repeated preference persistence. Main native
and restore registration, reconciliation/recovery/retention/OS policy and remaining external
gates are still open. Detailed evidence is in D-229 in the decision log and specification.


### D-230 — Guard incomplete protected deletion

SEC remains in progress. Actual legacy profile deletion cannot remove a protected/partially
prepared catalogue row while stranding native identities and referenced artifacts. Four new
regressions and retained native Windows two-profile guard checks pass. This does not implement
protected deletion; durable retirement and uncommitted-creation reconciliation remain required
before main registration. Recovery/retention/OS/native platform and external gates remain open.

### D-231 — Persist and resume reviewed retirement barriers

SEC remains in progress. Exact non-main private retirement requests now record ordered catalogue,
artifact and identity barriers. Resume requalifies actual native synchronization before effects,
including a phase visible after a failed flush. Nine focused cases and a two-process actual Windows
file fixture pass. This is not complete native profile retirement: final catalogue/key-reference
inventory and application registration remain open, with recovery/retention/OS/platform gates.
See D-231 in the decision log for the observed evidence and its limits.

### D-232 — Exact identity removal after retirement reference checks

SEC remains in progress. Stable identities can now be retired only for the exact non-main request
at its natively requalified artifact barrier, after reference verification and transactional full
catalogue recheck. Other bindings are preserved; observable deletion/flush failure resumes without
replacement identities. Actual two-profile SQLite removal runs in the independent Windows caller.
Final native preference/artifact/key retirement and main registration remain pending. See D-232
in the decision log for exact evidence and remaining recovery/retention/OS/platform boundaries.

### D-233 — Persist exact file inventory before protected retirement

SEC remains in progress. Retirement now retains immutable direct filenames, native file identities,
lengths and SHA-256 before catalogue removal. Native Windows cleanup refuses changed/replaced files,
preserves foreign files and its own authority, synchronizes deletion, and resumes only the existing
catalogue-removal request. Real DPAPI-envelope deletion and cold independent continuation pass.
Complete native profile/reference discovery and final app/mobile/recovery/retention/OS integration
remain open. See D-233 in the decision log for observed scope and its limits.

### D-234 — Native Android cleanup and bound directory identity

SEC remains in progress. Android now removes only captured no-follow UID-private objects through
an anchored directory descriptor, with shared preflight/requalification and fsync. Root identity
prevents accepting absence in a replacement directory on both native adapters. Actual signed Release
Android lifecycle/cold main-key preservation and three focused changed-object refusals pass.
Final complete reference discovery, app catalogue integration, reconciliation/recovery/retention/OS
and platform/device acceptance remain open. Exact observed scope is recorded in D-234.

### D-235 — Capture complete known native profile references

SEC remains in progress. Actual identity/history metadata, authenticated native generation envelopes,
exact portable backup sets and authenticated opaque safety references now supply retirement inventory.
Unknown target namespace artifacts and changed snapshots fail closed; foreign safety/main references
are preserved. Actual Windows retained encrypted fixture capture succeeds without finance deletion.
Final app catalogue/startup reconciliation, recovery/retention/OS and platform/device integration
remain open. See D-235 in the decision log for exact observed coverage and limits.

### D-236 — Actual profile catalogue retirement and pre-startup continuation

SEC remains in progress. The explicitly configured native host now connects reviewed protected
DeleteAsync to actual catalogue Set/persistence, verified inventory/files/identity retirement and
pending reconciliation before startup capture. Digests protect exact complete catalogue inputs;
no plaintext names/packets enter request metadata. Missing inventory resumes only before publication,
retries reuse one request, and unrelated catalogue changes refuse overwrite. Normal plaintext behavior
and the unconfigured protected guard remain. Final main/recovery/retention/OS/creation/platform gates
are still open. See D-236 in the decision log for exact native and contract evidence.

D-237 adds explicit durable device Add intent before native profile preparation and pending startup
continuation with the same original identity. Catalogue persistence and completion precede request
cleanup; unrelated catalogue changes refuse replay. Main encryption remains inactive; historical
pre-intent orphan reconciliation, recovery/retention/OS and platform adoption remain open. See the
decision log for terminal evidence. This does not close SEC or the full backlog.

D-238 prepares standard RSA-OAEP-SHA256 optional generation recovery, DEK-keyed HMAC confirmation,
a SQLCipher-encrypted private vault and exact durable public recovery metadata. Device key reads
never silently fall back. Actual native recovery uses fictitious original-model finance and keeps
ciphertext unchanged. See decision log for terminal scope/evidence; final UX/re-enrollment/reference/
retirement/OS/provider adoption remain open, and main storage remains plaintext. SEC-06 is in progress.

D-239 connects exact recovery mapping capture/removal to actual protected ProfileService retirement.
Target keys are authenticated before catalogue publication, only captured target mappings are removed
before native key files, and other-profile recovery/private vault remain. Missing/changed references
refuse cleanup. See decision log for terminal evidence; main encryption, final recovery UI/re-enrollment/
rotation/reference-retention/OS/platform gates remain open. No whole-SEC completion is implied.

D-240 adds explicit verified native re-enrollment and cold device-key read without using the recovery
secret, preserving the original envelope and financial ciphertext. Missing/corrupt entries never
regenerate keys. Final recovery UX/all-profile orchestration, recovered-entry lifecycle/reference/
retention/OS/platform adoption remain open; main encryption stays inactive. See decision log evidence.

D-241 authenticates recovered-key native references with a DEK-keyed exact-object receipt and
connects original/recovered/receipt removal to actual protected ProfileService retirement. Missing/
changed/staged references refuse cleanup. See terminal evidence in decision log; main encryption,
final UX/all-profile recovery/alias lifecycle/retention/OS/platform adoption remain open.

D-242 composes complete captured-profile recovery preflight and immutable native imports before
actual covered startup, without quota filtering, session publication or catalogue writes. Unknown/
missing/changed authority fails; original buffers are cleared. See decision log for terminal proof;
main encryption, concrete full retained-reference verification, recovery UX/alias/retention/OS/
platform acceptance remain open. Whole SEC/goal scope is not declared complete.

D-243: actual Android wrapping-alias loss in the independent fictitious package is now verified,
including refused original reads, cold all-profile recovery/startup, unchanged original envelopes,
no alias recreation and balances 250/880. Strict native Release build is clean; see the decision
log and device-key guide for exact runtime proof. Product code/APK remain D-242/plaintext. Future
alias lifecycle, retained-reference validation, final recovery UI/adoption/OS and device/iOS gates
remain open; no unrelated test/build/UI suite was repeated.

D-244: new Android generation keys use independently wrapped purpose-0 envelopes instead of
recreating an installation alias. Legacy reads and receipt-bound recovery remain distinct. Actual
native alias-loss recovery plus a later cold generation read passed; the third observation expired
before the same process's successful result was read without restart. All 3456 tests passed; see
D-244/device-key guide for exact negative and positive evidence. Final protection/adoption and
reference-aware alias retention remain open; main registration remains plaintext.

D-245 adds concrete read-only recovery reference authentication for current/rollback/preference
cipher databases and separately keyed safety references, with exact native object/namespace rechecks.
Five native-file cases and all 3461 tests pass. Actual Android cold all-profile recovery now verifies
two databases/rollbacks/capsules and two safety references after removal of all four fixture envelopes;
see the decision/device-key guide for proof and corpus limits. Main protection remains inactive;
full restore/crash/staged/orphan corpus, final recovery UI/retention/adoption/OS gates remain open.

D-246 fixes required recovery candidate presence at the journal/history interruption boundary.
CandidateVerified or Selected in the real journal requires the actual candidate even before history
Selected is mirrored. Two new cases, all 3463 tests and the actual original verifier on native Android
pass; journal/history remain unchanged and no replacement data/keys appear. See decision/device-key
proof. Multi-restore financial corpus, reconciliation/UI/retention/adoption/OS remain open; main is plaintext.

D-247 verifies an actual two-portable-restore chain and interrupted native preference persistence.
Cold original application recovery verifies/re-enrolls all three generation keys, resumes the selected
second restore without an old connection/package and preserves candidate/rollback balances. A stale
fictitious catalogue setup defect was corrected; its refusal and same-fixture continuation are retained,
and a fresh complete scenario passes. See decision/device-key guide for exact evidence. Product code
and verified signed APK remain D-246; reconciliation/retention/UI/adoption/OS/device gates remain open.

D-248 aligns Android key reads with existing native ownership checks: regular, single-link and
current-UID file descriptors only. Actual normal/cold/negative key-store observations pass; hard-link
creation is denied by the emulator, explicitly not coverage of its read-refusal branch. Same-key
negative continuation and failed harness evidence are retained. All 3463 tests pass. Final recovery/
cleanup/retention/adoption/OS and external platform gates remain open; main remains plaintext.

D-249 lets explicitly verified recovery preserve a mismatching bounded uncommitted receipt stage and
publish a separate read-back/synchronized receipt attempt without replacing final authority. Actual
Windows DPAPI and four Android staged-receipt recoveries pass; all old stages remain unchanged and
main/work balances remain 250/880. All 3464 tests pass. Reconciliation/retention/final adoption remain
open; published/oversized/unsafe receipt authority is not replaced and main registration remains plaintext.

D-250 lets authenticated reviewed profile retirement capture/remove only bounded canonical receipt
stage remnants through its existing immutable native inventory and durable barriers. Missing final
authority, oversized/unknown names and key-envelope staging remain refused. Three added actual app/
Windows cases, all 3467 tests and cold original Android profile retirement pass; main balance/recovery
remain. This closes receipt-only profile cleanup; automatic retention/orphan/key-stage/final adoption
and external gates remain open. Main stays plaintext.
