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

D-251 removes implicit native-engine selection from shared persistence. The app/test hosts explicitly
retain the existing standard bundle; the design-time bundle is private and cannot flow to app hosts.
Actual dependency graphs, real EF context tooling, all 3467 tests and strict Windows/Android builds
pass. This is a final-provider integration prerequisite; current main engine/storage remain unchanged
and cipher host/library compatibility, recovery/retention/OS/platform adoption are still unfinished.

D-252 qualifies actual production persistence assemblies, not source copies, with SQLCipher bridge/core
3.0.5 on Windows x64 and signed Release AOT/trim Android x64. Actual 32 migrations, encrypted negative
money, binary portable backup and wrong-key refusal pass across fresh process reopen. Android sandbox
inspection failure was corrected only in its distinct fixture package, preserving creation/result/data
and continuing the same key. See decision/device-key guide for proof/scope. Main/APK remain D-251;
final host/recovery/retention/OS/native platform and device acceptance remain open.

## Android protected storage delivery — D-263

The complete Android main application batch is implemented and locally verified with official
SQLCipher, automatic device keys, optional recorded recovery, all-profile lifecycle, retained portable
recovery, encrypted restore safety and reference-aware retirement. Final Android OS backup/transfer
exclusion is coupled to this runtime; portable passwords remain optional.

All 3509 corrected dependency tests passed without failures/skips and outputs were cleaned. Strict
Windows and complete Android Release builds passed. The final APK is signed, non-debuggable, uses
core runtime 10.0.13 and both reviewed native cipher payloads. Eighteen corrected native contexts cover
en/fa/de, light/dark and 360/412/wide layouts, original enrollment navigation, key-loss cover and actual
file-picker cancellation. Financial hashes and exact fixture envelopes were preserved.

[Complete evidence](quality/android-protected-storage.md) records the exact artifact/hash, observed
failures, native process-loss corpus and limits. Windows/iOS encrypted adoption, fresh hosted CI,
physical/OEM/provider/Store acceptance and the full canonical backlog remain open.

## D-264 Windows protected storage status

Windows main protected startup, DPAPI recovery enrollment/key-loss/cold reopen, isolated profile creation/switch/retirement, encrypted safety retention and explicit portable replacement have native application evidence on an independent fictional installation. Shared Android effects remain platform adapters to the same application algorithms. Final local verification: 3521 tests passed with zero failures/skips, strict Windows/Android builds without warnings/errors, complete signed APK and scoped emulator upgrade/cold reopening passed; see [Windows quality record](quality/windows-protected-storage.md). No iOS, physical-device, OAuth, Store or hosted fresh compiler acceptance is implied. The continuous-development objective remains open.

## D-265 holding accessibility and retired Home reads

Eighteen native contexts for three changed pages and eighteen distinct bottom contexts cover fa/en/de, both themes and 360/412/wide at process-local 200% typography; Cancel and original command/row identities retain financial data. The actual original Home read waits behind SQLite maintenance, retires with its Shell and completes without publishing into the old collection; replacement Home and complete stored rows verify. 3521 tests pass (zero failures/skips); native rendering is separately verified. [Bounded evidence](quality/holdings-readable.md). Final artifact/build details accompany delivery; physical-device, actual OS scaling/spoken screen readers and iOS remain open.


## D-266 device-only authentication requires success

The owner approved keeping the original lock cover when system authentication is unavailable on 2026-10-14. Only AuthenticationOutcome.Success authorizes device-only entry. Failed, unavailable, unknown and throwing responses preserve the original cover and queued destinations. The existing translated unavailable-device guidance explains restoring a device screen lock. PIN verification/recovery and the previously authenticated return grace remain separate and unchanged. No new stored data, permission, SDK or portable preference is introduced.

Actual Windows LockPage/native Invoke checks covered 18 language/theme/width contexts with an explicit authentication response port; a separate feedback pass checked the translated guidance. The independent Android package used the actual native adapter: NotAvailable after automatic prompting and Unlock, with Back unable to dismiss the cover and zero queued destinations opened. Neither review changes system credentials or native PIN storage. These checks do not accept configured biometrics, Windows Hello, physical devices or iOS.

An unrelated Home collection rendering COM exception remains observed during language/theme review. The lock assertions and unchanged financial fingerprint completed, but this is not a clean whole-application rendering result. SEC-04 remains in progress for its remaining platform and biometric transitions.


## D-267 complete Home balance publication

Observed Windows Home refresh failed in BindableLayout's ContentView handler disconnection while Balances.Clear published an empty intermediate list. Currency totals now use the existing SnapshotCollection complete replacement, preserving existing amount rows during ordinary and covered reloads. Currency calculations, inclusion policy, formatting and current-page retirement guards remain unchanged. The native regression checks original Home and original amount-handler identities, including a real modal cover; financial storage is not edited.

D-267 final native review passed all 18 contexts and 36 original Home reloads without the observed COM failure; amount handlers and complete financial rows remain unchanged. Canonical signed Release upgrade/cold/More-to-Home return passed. The unchanged 3,526-test D-266 baseline was retained; native publication is directly verified. Exact evidence and external limits: docs/quality/home-balance-reload.md.


## D-268 sale-result help and truthful connection/account copy

The planned LOC-05 help sits beside an available realised holding result and explains recorded proceeds minus recorded average cost, unknown inputs and the absence of tax rules or official tax documents. The six existing languages include a concrete example. No tax calculation, financial posting, schema or new language is added.

LOC-10 corrects the browser-return message: it describes completion of the browser step and directs the user back to Zanance for the connection outcome, without claiming token exchange succeeded. English loan pay/receive confirmations identify the selected account explicitly, matching the original session argument and the other five translations.

Store drafts now name all six existing languages and describe optional password protection for local/connected backups, readable unprotected files, Android device-protected database keys and final system-backup/device-transfer exclusion. Real OAuth, physical-device, iOS and publication acceptance are explicitly separate; no store text was published.

## D-269 costed OD-02 backend proposal

Official provider prices and the 2026-10-15 PostgreSQL availability change inform three bounded USD scenarios and a complete first backend delivery/restore boundary. Arithmetic/overage checks pass; see docs/enhancements/2026-10-commercial-release/07-backend-cost-and-decision.md. Provider, region and test-environment budget remain owner choices. No backend, account, mail, hosted resource, SDK, app code or publication changes; ZCR-ID-01 and the full development objective remain incomplete. Application tests/build/native/APK evidence is unchanged D-268 and was not repeated.


## D-272 complete local identity and private service foundation

Implemented the approved optional ASP.NET Core/PostgreSQL/private-storage backend as an independent
Zanance API project. Maintained Identity password/token providers, protected bearer tickets,
transactional one-use challenges, absolute device grants, atomic refresh generation, immediate
stamp/session revocation, shared durable request limits and private encrypted immutable objects
are connected end to end. Capacity is serialized by real owner in PostgreSQL. No local profile id
or opaque object id establishes ownership. No financial sync/sharing/billing route is implemented.

The separate migration operator and runtime role, persisted wrapped Data Protection key ring,
leased encrypted fictional outbox, resumable deletion/cleanup, content-free readiness and hidden
local start/drain/snapshot/fresh restore operations are implemented. Missing service keys keep the
original installation closed; no replacement key authority is accepted. No real email, account,
paid hosting, app connection, entitlement enforcement or publication was enabled.

Final server Release build has zero warnings/errors. Four actual-host/PostgreSQL scenarios pass
with zero errors/failures/skips on the private .NET/ASP.NET 10.0.13 runtime (6.355 seconds); they cover concurrency,
expiry/replay, ownership, quota, protected delivery, corruption, revocation, cold key loss/recovery
and real pg_dump/pg_restore. Fictional restore was about 0.7 seconds with its committed object and
revocation state preserved. Windows Kestrel and the operator's independently restored destination
returned readiness 200; exact scopes and observed failures are in quality/local-identity-backend.md.

The existing 3,526 app-test baseline and complete signed mobile APK remain D-268; no unchanged app
build, native visual matrix or cipher corpus was replayed. Local ID-01 foundation is delivered;
external sender, hosted certificate/key/backup operations, provider/legal acceptance and mobile
service adoption remain separate gates. The whole development objective remains active.

## D-273 target currencies and source-qualified holiday calendars

Deliver LOC-07/08 as one app batch. The working Western Europe scope is DE, AT, BE, FR, LI,
LU, MC, NL, CH, GB and IE, plus US, CA and AU; the broader geographic boundary question
remains open. Existing EUR, CHF, GBP, USD, CAD and AUD already have ISO minor digits of two.
No stored amount, account currency or language is changed by selecting a country. Country
registration is additive, independent of the interface language. Existing Iran dates remain
compatible; an Iran release variant is future work.

Offline holiday sources and regular dates are reviewed for Gregorian 2026-2027. Settings,
onboarding and Plan display scope, authority, reviewed years and an unverified-year warning.
Switzerland has only August 1 federal coverage; Canada uses federal nominal statutory dates;
the US uses standard Monday-Friday observed federal dates. UK and Australia contain only dates
shared by all constituent regions. Regional, workplace, extraordinary announced closures and
bank-business-day guarantees are excluded explicitly. Legacy Germany/Iran calculations remain
compatible outside the reviewed window; new country datasets do not invent future dates.
Saved Plan country/weekend masks are retained when device regional preferences change.

All 3,569 app tests pass, including 43 new cases; final Windows and signed Android Release builds
have zero warnings/errors. The original regional view passed 18 hidden own-window layout checks
(en/fa/de, light/dark, 360/412/1280 px, Debug text scale 2), complete captions and 44 px picker
height, with unchanged financial rows. Eighteen actual native selections passed in the three
English light contexts. Hidden RTL popup automation failed and is not counted as selection
acceptance. Actual signed emulator Settings selections for Canada, US and Australia, live
Persian/German/English captions, light/dark app choices and return to no holiday calendar were
checked separately through native XML. No financial entry was posted. Emulator Home remains
0.00 EUR; English, Gregorian and automatic digits/week start were restored; theme ends at
Use device setting. This is not physical-device, spoken screen-reader or iOS acceptance.

Detailed official sources, exact scope and observed verification failures are recorded in
quality/target-holiday-calendars.md. No new SDK, network request, permission, schema, paid resource
or commercial enforcement was introduced. The full development objective remains active.

## D-274 operation-scoped encrypted quick-resource reads

Measured protected Release startup after D-273: system first frames 1664/1401/1462 ms,
while loaded Home is observed later (accessibility polling upper bounds, not exact paint).
The original quick-resource operation reopened native keyed connections between EF queries.
A single DbContext alone did not improve its measured warm median (~989 versus ~999 ms).
The complete operation now captures one original context and explicitly keeps its non-pooled
connection open through both resource choices, complete input read and every original validation.
Standalone resource-choice reads also keep their own operation's connection open. No key format,
KDF, schema, guard, commercial activation, global pool or cross-operation credential cache changes.
Later explicit actions still read and validate fresh original data through a new context.

The same independently encrypted fictional fixture and actual production Data assemblies show
warm complete-read median 999.38 ms before, 82.42 ms after. First read 1220.60/565.23 ms includes
process/JIT effects and is kept separate. Wrong-key refusal, 32 migrations, negative balance and
portable read-back pass. Two actual-SQLite cases prove one captured context, unchanged rows/no
default settings or resource-choice writes, fresh action validation and drained ownership.

A Windows review launch exposed stale ApplicationId metadata despite an independent build property.
Its exact process was stopped; that attempt is not independent-installation acceptance. Touch/rebuild
and compiled identity checks precede the corrected review. Debug Home/holiday review routes now
reject a main identity before covered storage startup, without changing Release behavior. Do not
inspect or restore an unrelated installation to turn that failed attempt into evidence.

Final affected tests/build/native results and artifact identity are recorded in
quality/quick-resource-read-performance.md. Earlier full 3,571-test pass precedes the explicit-open
refinement; final Data/App tests are repeated because their production dependency changed, while
unchanged successful test projects retain that baseline. QA-06, full loaded-Home timing, large-data
native publication, physical devices and iOS remain open. The development goal stays active.

## D-275 complete Home loading and retry

Original Home now uses the existing snapshot load state from its first frame through complete
read/publication. Empty initial defaults and dashboard actions remain unavailable until ready;
an actually empty completed snapshot retains its existing next step. Six-language loading,
Home-specific failure/retry, original serialized reloads and retirement guards preserve data.
No schema, permission, SDK, key, stored preference, commercial activation or money formula changes.

All 3,572 app tests pass (zero failures/skips), with the new AT-275 final-retirement publication
case and existing initial/reload failure/retry/cancellation coverage. The final caption refinement
passes 242 localization tests without repeating unchanged successful projects. Strict Windows and
Android builds have zero warnings/errors. Eighteen native language/theme/width contexts, 36 focused
loading/failure captures, 18 actual native retries and unchanged complete financial fingerprint
pass in an independent protected installation. The first partial review exposed a wrong reused
transaction retry caption and was superseded by the corrected complete matrix.

Complete signed Android Release cold launch on the owned emulator shows preparation, actual Home
loading, then ready balance/actions in the same process; no false empty-account card appears.
APK signature/package/payload and exact artifact are recorded in quality/home-loading-publication.md.
This closes the observed publication defect, not the two-second performance objective, Android
fault injection, physical device, spoken screen-reader, OS scaling, iOS or release acceptance.
The development goal remains active; independent owner/provider gates remain unchosen.

## D-276 complete protected work-read performance batch

Complete Plan/Goal reads and fresh validation, budget choice/form reads and validation,
quick-resource validation and bounded budget-alert inputs keep their own non-pooled connection
open through the original operation. Budget form choices and inputs capture the same original
context/file. Every existing rights/stale/current-file check and writer/financial formula stays;
there is no global key-bearing pool, KDF reduction, schema or commercial activation change.

Actual production-assembly native comparison on the same encrypted fixture measures complete
read plus fresh validation: Plan 251.36 to 140.24 ms, Goal 624.61 to 180.53 ms, budget form
1298.93 to 155.51 ms, budget alerts 2281.44 to 653.78 ms, quick resources 788.64 to 146.29 ms
(three warmed medians; first use separate). Negative balance, 32 migrations, portable read-back
and wrong-key refusal pass. These are operation timings, not full loaded Home or device acceptance.
All 3,573 app tests pass with one new actual-SQLite budget context/lifetime case; strict builds,
original native Home and signed Release evidence are in quality/work-snapshot-read-performance.md.
The whole development objective and physical/iOS/reference-workload gates remain open.

## D-277 existing-user upgrade and current test-build policy

ENT-05's current policy is already implemented: the default commercial source is inactive,
with no fake Plus/Pro right or permanent/portable test entitlement. The actual first-schema
upgrade scenario writes four accounts above the approved Free quota and an expense, upgrades
through original migrations, retains identities/balance, accepts an explicit fifth test account,
and restores the real portable source into a separate fresh provider. Both original and restored
providers remain unenforced with null paid context; no resource selections are invented.

One focused AT-277 real-SQLite upgrade/restore scenario passes under strict compilation. It adds
one behavior to D-276's 3,573-test baseline (3,574 covered behaviors, not a new full-suite run).
No app/runtime/schema/dependency/UI change; no unrelated build/native matrix or new APK. The
complete signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.
Exact evidence and scope: quality/test-build-upgrade-policy.md.

Customer activation and announcement remain explicit owner/BIL gates. Do not turn disabled test
enforcement into permanent paid facts or announce future limits as already enabled. LOC-09's
final unattended protection choice and separate local personal-sync approval are pending;
none follows from backend foundation approval. The full development goal remains active.


## D-278 protected reference and tenfold workload evidence

An independent Windows executable now measures the actual protected generation/bootstrap and
production store/validation/calculation paths on deterministic 10,000-entry / 20-account / 100-plan
and tenfold workloads. Complete original schema/header/integrity/typed rows match the authenticated
pre-migration encrypted rollback after measurements, and both plaintext originals are removed.
Warm medians: all-entry reads 158.617 / 1191.737 ms; Plan read/validation 131.983 / 138.041 ms;
quick resources 127.675 / 126.937 ms; original balances/totals 1.487 / 19.066 ms.

Fresh tenfold protected preparation is 27,561.853 ms. Reference 439.797 ms is explicitly a resumed
revalidation, not fresh migration time: the original run terminated on an invalid raw export-byte
comparison, and the corrected complete logical verifier resumed the same generation/keys. The
terminal corrected run passed both workloads; strict Release compilation has no errors/warnings.
No app code/UI/schema change, unrelated test replay or new APK. Application coverage remains
3,574 behaviors (D-276 full 3,573 plus D-277 focused one), not a new complete suite run.

See quality/protected-workload-performance.md and the executable README for samples, failures,
resume ownership and exact scope. QA-06 remains partial: actual loaded Home/search/save, cold/ANR,
reference phone, Android/iOS and the two-second objective are not established by store timings.
The current signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.


## D-279 hosted Windows native qualification

Native cipher candidates now includes a Windows lane using the existing exact compiler qualification,
two fresh native builds with matching DLL bytes, original schema/tamper checks, immutable package
entries, actual production-assembly create/cold reopening and protected reference/tenfold workloads.
Only allowlisted public logs/proofs/measurements are uploaded; databases and DPAPI/key material stay
private. No candidate cache, weaker package/compiler policy, new native dependency or app change.

Actual YAML and all five Windows PowerShell bodies parse. Hosted success is not inferred from syntax
or the existing local D-278 workloads; check the final pushed commit once after the normal delay.
Existing native warnings and the separate unprivileged Windows OpenSSL-suite scope remain explicit.
No unrelated successful app/UI/full-suite/APK replay: application coverage remains 3,574 behaviors
(3,573 full plus one focused), signed APK remains D-276. See quality/windows-native-ci.md for the
exact path, artifact policy and remaining platform/security/device/release gates. Full goal remains open.


## D-280 current protected search and validated-save workloads

The independent protected executable now exercises original complete search/group and validated entry
Save on fresh reference/tenfold data. Focused ledger mode omits unchanged D-278 read/calculation cohorts;
default hosted runs include all operations. Four searches per shape check unique transfer/missing/numeric/
complete results; four actual saves per shape verify stored fields and delete exactly their own row.
Complete schema/header/integrity/typed rows still match the pre-migration encrypted rollback afterward.

Actual warmed unique search/group medians are 9.084 / 17.202 ms; validated saves 68.810 / 69.033 ms.
Search timers include result checks and exclude initial read/native rendering. Both workloads finish,
all eight temporary rows are read back/removed and both original plaintext files are absent. Strict
Release builds have zero warnings/errors. See quality/protected-ledger-performance.md for all samples,
result counts, preservation and timing limits. No unrelated app/UI/APK/full-test replay or new app
change: 3,574 covered behaviors retain their qualified baseline. Native phone/Home/search/Save, cold/
ANR, iOS, the approximate two-second objective and full goal acceptance remain open.

## D-281 automatic device-encrypted local copies

LOC-09 is implemented locally under the owner's final device-key policy. Daily/weekly/off foreground
coordination, independent per-profile timestamps and automatic/pre-restore retention, six-language
status/help/discovery and explicit original protected restore are complete. No password storage,
readable automatic file, cloud fallback, sharing action, new crypto/provider/schema or permission.

Full strict suite: 3,590 passed; final focused Data publication case: one passed (3,591 covered
behaviors, no second full-suite claim). Strict Windows Debug and Android Release builds, 18 final Windows
contexts, native encryption/reference-aware retention, six Android changed-surface/help contexts,
original explicit restore and complete signed APK are recorded in quality/automatic-local-backup.md.
The initial Android live-picker publication loop and native validation lock issue were corrected;
only related checks were repeated. Local success is not hosted CI, physical-device/iOS/OEM,
power-loss, OAuth, store-signing or product-publication acceptance. The full goal remains open.

D-293: complete local isolated hosting/private restore/TLS renewal qualification follows D-292 personal sync.
Public routing/mail remain external. Next approved complete batch is local household E2EE, not another helper slice.
Canonical state/order remain in enhancements/2026-10-commercial-release/04-backlog.md and 05-delivery-plan.md.

Current household cost status is in canonical ZCR-SHR-02/D-294: original protected persistence, exact complete encrypted contract, two-client/editor/cold facts and corrected Windows layout are qualified; Android/AOT and the remaining full household integration are unfinished. Keep one complete delivery batch and the existing finished baseline.

### D-294 final local delivery

The complete local household batch is implemented and qualified together: protected E2EE membership/device/key
and optional recovery lifecycle; selected separate financial adoption and complete costs/conflicts; two-sided
ownership transfer; audit, permanent privacy intent/retirement and latest-marker-aware full service restoration.
Final service composition, bounded historical row reads, compact pre-allocation portable scrubbing, real main
Android upgrade/export, separate Release AOT/trimmed native allocation and native transfer-dialog geometry pass.
The original full run plus only899 exact failed-case reruns yields3753 passing existing cases;2854 prior successes
were not repeated. Separate focused cases do not change that total. Final product APK:
artifacts/android-household-final-d294/pro.vafadar.zanance-Signed.apk;91356917 bytes;
SHA2566ebe1f0d8f963f1c1f18cd2ecee7d03a4a5cfc5325d25e3883bdeee960ac38f2; signature/permanent id/arm64+x64 verified.
Exact scope, retained failures and runtime evidence are in enhancements/2026-10-commercial-release/11-household-delivery.md.
Earlier in-progress notes are historical. Public/commercial activation, physical/iOS, store/provider/signing,
external security review and publication remain separate gates; identity erasure and ciphertext recall are not
claimed. Continue the already approved complete billing/future-web-prerequisite batch without helper-only slices
or repeated unrelated successful checks.


## D-295 local billing checkpoint

Local Android/service billing and protected signed continuation are qualified; native StoreKit2/Mac and actual
store/provider/public sandbox acceptance remain unfinished. This history does not mark the commercial wave done.
The only current remaining-work state is ZCR-BIL-01..05 in the canonical commercial-release/04-backlog.md;
12-billing-delivery.md records the exact scope, current APK, focused tests and exclusions.


## D-296 Germany accountant preparation

The approved Level 1 batch adds an explicit year/account review and readable ZIP containing exact ledger data,
separate currency/kind recorded totals, blank accountant-review columns and opt-in exact stored document copies.
Transfers remain one row; refunds keep their links; no deductibility/VAT/profit/filing is inferred. Notes/documents
start off, linked transfer identities are explained, original-source changes and retired form/file/rights/lock
refuse sharing, and no money/preferences are written. This is a Plus preparation feature distinct from later
TaxAddOn rules; test commercial enforcement stays inactive. Existing basic CSV and portable backup are retained.
Original invoices/structured e-invoices must be kept separately; this is not an original/GoBD/DATEV/ELSTER archive.
All six translations and the individually named future country queue are included without an ETA.
Implementation, official-source qualifications, bounds, failures and final evidence are recorded in
`enhancements/2026-10-commercial-release/13-accountant-export-delivery.md`. Expert calculations and physical/iOS/
public release acceptance remain separate. No new database schema, migration, SDK or network permission is added.


## D-297 bank CSV execution

One documented StarMoney Giro ordinary-cashflow preset is integrated end-to-end with the original preview,
atomic writer and durable Undo. This advances ZCR-IMP-01, not universal bank coverage. Exact current retail-bank
schemas, mixed relationship imports and other market formats remain open; original-source research continues
Germany, USA, then approved targets. IMP-02 formats/live connections remain separate. See enhancements/2026-10-commercial-release/14-bank-csv-delivery.md.

## D-298 Approved local optional analysis prerequisites

Implement aggregate-only personal scope/consent, existing pinned identity/private original continuation and
service-owned split credit journal together. Separate manual readable-summary sharing from a configured model;
leave actual provider/prices/credit sale unavailable. Shared-data disclosure is separately refused. Exact current
whole-batch qualification and remaining external gates are in the canonical commercial backlog and
`enhancements/2026-10-commercial-release/15-ai-local-delivery.md`; no new full-suite or release claim.

## D-299 approved local rates/prices prerequisites

Explicit source lookup, bounded private offline cache, native original review, immutable saved provenance and
personal/shared source companions advance ZCR-FX-01. They do not select or license a live provider, infer item
premiums or enable automatic financial changes. Keep source/coverage/redistribution/cost/public and actual
device/Mac gates open. Exact current qualification: enhancements/2026-10-commercial-release/16-market-reference-delivery.md.

## D-300 Measured Android startup batch

The observed bootstrap/ANR concern now has a concrete actual-path AOT candidate and retained native process evidence. Keep the general performance/reference-device objective open; do not infer whole-app ANR freedom, iOS or release acceptance.

Current exact evidence: enhancements/2026-10-commercial-release/17-startup-responsiveness-delivery.md.

## D-302 complete original currency fields

The common editable Syncfusion currency input now explicitly retains native text scaling and a visible
44-unit real list button with the original translated field name. Original filtering/selection stays in the
vendor control. Its original currency rows have scalable app typography, real native actions and a translated
selected-state description. Amount/currency groups in transaction, goal and manual-rate forms use complete
rows rather than fixed 110-unit or split currency columns. Drafts, bindings, field errors and explicit Save
remain unchanged; code selection does not save money.

All 18 English/Persian/German light/dark 360/412/wide contexts qualify original native input and list actions
at the requested process-only 200% font stress, including the three actual constrained forms and complete
unchanged isolated account/entry rows. Current strict builds, signed main Android upgrade, native list/selection and typed search
and Cancel evidence are in19-currency-accessibility-delivery.md. No unrelated successful whole-suite, native
crypto, synchronization, billing or service qualification is replayed. This is no new unit-test count or actual
OS-scale, spoken screen-reader, physical-device, iOS or release acceptance; A11Y-03 remains In progress.

## D-303 complete ledger read without discarded identities

Home reads the entire ledger in physical row order, then applies the original stable newest-first date/time
order in memory. This avoids repeated encrypted-page visits through the date index without dropping history,
changing the operation-local cache, using SQLite temporary sort files or weakening encryption. The supported
compiled-model extension binds the existing-id LedgerEntry constructor, so materialization no longer creates
and discards a fresh identifier. New entries keep their original identifier generation. The compiled model is
regenerated; schema, financial rules, keys, backup and privacy policy are unchanged.

All 881 Data tests pass, including two new AT-303 complete-value/order cases. Actual Release/AOT Android
reference/tenfold Home in English/Persian/German retains every canonical financial presentation and complete
raw schema/header/integrity/typed rows on the same ciphertext and keys. Tenfold publication median decreases
from 14,828.37 to 7,418.54 ms; reference median from 3,066.82 to 2,252.00 ms. These are controlled loaded-emulator
observations, not two-second cold/phone/ANR/iOS acceptance. Windows native publication and complete portable
preservation also pass. Full-suite historical counts are not a new full run. Exact evidence and package:
[20-home-read-delivery.md](enhancements/2026-10-commercial-release/20-home-read-delivery.md).

## D-309 protected schema connection follow-through

The actual covered-startup upgrade callback now retains one supplied unpooled keyed connection during
history/pending-migration review, then restores its caller's original state. Closed current/old/unknown
history reproduces two/three/two opens before correction and one afterward; already-open ownership,
cancellation and unknown-prefix refusal remain. Eight new cases plus two existing cases qualify ten
distinct related behaviors across retained runs. The existing old-schema fact also preserves original
account/entry/settings and balance through real migration; its final changed assertions alone are rerun.
No new complete-suite count is claimed.

Original-source Release/AOT Android reference/tenfold measurement on the same retained ciphertext and
keys observes two opens becoming one, with complete raw schema/typed rows unchanged. Strict test and
Windows builds pass. This bounded callback improvement is not whole cold-start, two-second, ANR, phone,
iOS or release acceptance. QA-06 stays In progress. Native/package receipts and exact limits:
[protected schema connection lifetime](quality/protected-schema-connection-lifetime.md).

## D-311 readable report and forecast tables

The original trend/history and forecast comparison/parts templates now separate identity/date from whole
monetary packets, reusing the existing native horizontal AmountReadout and overflow hint. Native original
German month/amount overflow is reproduced at 360 width. Eighteen Windows en/fa/de, light/dark, 360/412/1280
contexts at process-local 200% stress and six original-source Release/AOT Android contexts pass with original
stored data unchanged. Charts/PDF/calculations and schema are unchanged. No new unit-test count or unrelated
successful suite replay is claimed. Exact scope, retained failures and package:
[financial table readouts](quality/financial-table-readouts.md). A11Y-03, spoken readers, actual OS/device/iOS and release gates remain open.

### D-312 original safety-copy recovery on physical Android

The first approved Samsung install/financial/automatic-copy path exposed missing selection of retained
pre-restore safety copies. The original page now lists only current-profile/install candidates through
bounded metadata-only discovery and retains the original complete authenticated restore/consent flow.
Seven new focused cases and original native layout evidence are recorded in
[physical Android safety recovery](quality/physical-android-safety-recovery.md). Broader phone/iOS gates remain.


## D-313 public-path engineering

The Tunnel composition and gateway-preserving operators are locally qualified. No live connector, DNS or real mail is configured; application/APK remain D-312. See [outbound tunnel origin](quality/outbound-tunnel-origin.md).


## D-314 physical loaded-workload evidence

Reference/tenfold original Home, native search and validated write/read-back/cleanup now have actual Samsung ARM64 receipts, with complete raw/portable preservation. Initial publication still misses the performance objective; full cold/low-memory/ANR and broader device/iOS gates remain. App/APK remain D-312. See [physical protected workload](quality/physical-protected-workload.md).


## D-315 delivered ledger-read improvement

34 related tests pass with independent EF expectations, optional inclusive date bounds, complete values/order and cancellation. Strict Windows/Android builds and complete signed main upgrade pass. On the same retained Samsung ciphertext/keys, 100,000-entry public read goes 7,773 -> 4,313 ms and original native load/layout 10,446 -> 6,964 ms, with complete preservation. Exact/empty/wide search and original main 979 EUR history remain correct; successful unchanged financial writes were not replayed. No full-suite rerun/count is claimed. QA-06 remains In progress for remaining latency, cold/ANR/low-memory/device/platform acceptance. See [physical ledger read](quality/physical-ledger-read.md).


## D-316 original physical financial workflow

The unchanged signed D-315 APK now has actual Samsung main-app evidence for original setup, optional budget and balance goal, manual 4/6 partial/final settlement, future-plan/forecast exclusion/reset, process restart and return to the original Main profile. The two actual expense rows total 10, balance is 990, budget left 40; future 20 gives forecast 970 without a third posting. Native displayed-state acceptance is distinct from full database equality. All data is fictitious and retained; no system setting, reinstall or personal data change. No unchanged builds, unit suites or APK qualification were repeated. Broader recurrence/notification/platform/performance/release gates remain open. See [physical planned work](quality/physical-planned-work.md).


## D-317 retained-profile import lifecycle

Actual Samsung original file-picker cancellation/selection, own-format two-row preview/import, zero-new/two-known duplicate preview with disabled action and persisted Undo after process restart pass in the retained D-316 fictitious profile. Exact note/tag/literal formula-like text is retained; financial balance 990 -> 980 -> 990, original two payments, 40 budget left and 970 forecast are verified. Return to Main at 979 and hash-matched exact-file cleanup are complete. Unchanged app/APK and successful builds/tests remain D-315. No producer-export roundtrip, real-bank/aggregate/hostile-file/full raw equality or complete device/platform/release claim. See [physical CSV and Undo](quality/physical-csv-undo.md).

## D-318 observed first-plan defect

The actual first-plan blank-list defect is fixed without persistence/financial changes. Independent original native Save on retained protected data and 18 final en/fa/de light/dark 360/412/1280 large-text layouts pass; standard Windows and complete signed Android builds have zero warnings/errors. Main upgrade without reset displays the retained unpaid plan before any segment toggle, handles empty Upcoming/return and preserves the displayed 979 EUR balance. No successful unrelated unit/device/crypto scenario is replayed or unit count increased. Broader device/platform/performance/release gates remain open. See [first plan publication](quality/first-plan-publication.md).

## D-319 physical debt/lent batch

Both original physical debt/lent lifecycles now have native evidence: positive setup signs, complete 11- and 6-row estimates, cancellation without posting, principal transfers versus 10 expense / 5 income, explicit final repayments, six retained rows and cash 1,495 after signed cold upgrade. The observed zero-balance missing-terms hint is corrected; 38 focused native Windows cases and zero-warning Windows/complete Android builds pass. Main returns 979. Unit counts and successful unrelated checks are retained; broader device/platform/performance/release gates remain open. See [physical loan lifecycle](quality/physical-loan-lifecycle.md).

## D-320 profiled native stability

The independent phone caller now matches production assembly/profile inputs. A captured extra CoreLib
AOT-method selection reproduces native failures under load; removing only those additional roots while
preserving SDK defaults completes all 12 en/fa/de reference/tenfold Home cases and full schema/typed-row
preservation on the same retained ciphertext/keys. Align restored runtime/compiler/task servicing versions,
fix matching-configuration restore, keep only the explicit review window awake and retain scoped failures.
Windows and complete signed Android builds pass without warnings/errors; the main data-preserving upgrade
shows 979 EUR and actual expense/Cancel returns unchanged. No financial C# algorithm, schema, KDF/native
cipher or full-suite count changes. Current loaded observations remain 1.96-2.65s reference/8.83-10.29s tenfold;
this is stability qualification, not a general speed improvement, full cold/ANR matrix or release approval. See [native evidence](quality/physical-home-compilation.md).


## D-321 measured complete-reader follow-up

D-321 reduces the measured complete 100,000-row reader from 4231 to 3344 ms and complete Home input read from 4653 to 3811 ms on retained Samsung ciphertext/keys. All 12 final en/fa/de native Home cases preserve complete financial presentation and raw schema/typed rows; loaded Home still takes 1.83-2.73s reference/ 8.47-9.58s tenfold. Related validation is 42 passing cases plus one final interceptor-reader case (nine added behaviors), not a full-suite rerun. Final strict Windows/complete signed offline Android builds and data-preserving main Home/All-history/return pass. QA-06, cold/ANR statistics and external platform/release gates remain open. See [evidence](quality/physical-home-row-reading.md).


## D-322 measured complete read continuation

D-322 reduces the measured tenfold complete Home input read from 3811 to 3306 ms using exact-query null metadata, retaining all original 32 fields/conversions and safe interceptor fallback. All 12 final en/fa/de phone Home cases retain full financial presentation and raw schema/typed rows; loaded Home remains 1.92-2.83s reference / 7.23-9.51s tenfold. 45 related cases pass (two added behaviors), strict Windows/complete signed offline Android builds and main Home/All-history/return upgrade pass. A recurrence candidate without reliable native gain and temporary worker probes are fully withdrawn. No whole-suite rerun, two-second/cold/ANR/platform or release acceptance is claimed. See [evidence](quality/physical-home-null-bitmap.md).


## D-323 physical original report flow

The unchanged D-322 signed offline main APK passes an original Samsung report/filter/PDF handoff batch:
October income 1,000/spending 21/surplus 979 and five-row drill-down; empty September scope replaced by a
saved October expense filter (three rows/net -21); actual PDF chooser offered and cancelled without a
recipient; filter retained after cold PID 24409 -> 27876 and then explicitly deleted; original unpaid
plan 3/headroom 976 and cash-account zero income/spending retain transfer semantics. Final report/ledger
and Home return 979 without financial Save/reset. Eleven saved-device assertions pass; no new managed
test count, build, APK or unrelated successful checks are repeated. This is English/light displayed-state
acceptance, not raw main-store equality, inspected phone PDF pages, all reports/readers/iOS or release.
See [physical evidence](quality/physical-report-filter-pdf.md).


## D-324 six source-language digital receipts

The unchanged signed D-322 main APK passes six original Samsung digital-PDF receipt flows, one per
en/fa/de/es/fr/it source language: distinct total phrases and Persian digits yield the reviewed 24.50 EUR,
not cash 50/change 25.50/order 999999. Every original editor is explicitly cancelled/discarded without Save;
the same PID 27876 retains five rows/net 979 and final Home 979. The incomplete picker observation alone
continues, and only six exact hash-matched newly owned input files are removed. No managed test count,
app build/install or unrelated successful check is repeated. English/light presentation, digital text and
displayed preservation are the scope; evaluation notices limit merchant accuracy, and real corpus,
image OCR/camera, raw main-store equality, other native/platform and release acceptance remain open.
See [physical evidence](quality/physical-digital-receipts.md).


## D-325 physical image OCR and remaining caption defect

Actual Samsung image OCR exposed French À -> Å in NET À PAYER while the number/unit remained correct.
D-325 folds Latin accents only for whole keyword/exclusion comparisons, preserving original source,
non-Latin marks and all numeric/currency/refusal rules. Ten added behaviors reproduce the failure and
retain safety; all 118 related cases pass, followed by test cleanup and zero-warning Windows/complete
signed offline Android builds. Same-file French recognition now gives 24.50 with original Å source;
seven original raster cases include German EXIF and blank missing-total/currency-conflict drafts.
Three pre-fix successes are retained; four final cases and upgrade preserve five rows/Home 979 without
Save/reset. Exact owned inputs and diagnostic package are removed. Real corpus/camera/scanned/iOS and
release gates remain open; a separate dangling transfer-summary caption is observed and queued next.
See [physical evidence](quality/physical-receipt-ocr-accents.md).


## D-326 transaction-result presentation

D-326 fixes the observed transfer-only transaction summary: show the existing translated count-only
caption when the actual result-currency dictionary is empty. Retain a real zero result, separate currency
packets, original matching rows and unchanged income/spending/transfer semantics. No new string, schema,
financial write, dependency or native cipher change is introduced. All 18 focused application-source cases
pass across six languages, followed by test cleanup; the Windows and complete signed offline Android
builds have zero warnings/errors. The independent actual Windows page passes 18 en/fa/de light/dark
360/412/1280 contexts with complete scaled captions and unchanged stored inputs. Six successful English
contexts are retained; only the remaining contexts continue after correcting the diagnostic's Persian-digit
comparison and hidden-root measurement order. The original Samsung upgrade shows transfer count 1 with
no dangling total, expenses 3/net -21, income 1/net 1000 and final Home 979 without Save/reset. Phone
preservation is displayed-state evidence, not raw main-store equality. Remaining receipt corpus/camera,
performance/cold/ANR, iOS/provider/Store and release acceptance remain separate.
See [qualified evidence](quality/transaction-summary-result.md).


## D-327 automatic-copy physical lifecycle and readiness

D-327 qualifies the original physical automatic-copy lifecycle on SM-A176B and fixes an observed
maintenance overlap. A synchronous onboarding-readiness read raced the legitimate exclusive safety-file
discovery; the native type/source stack identifies DatabaseAccessGate.EnterRead through GetSettings.
Use cancellable GetSettingsAsync readiness, recheck the captured profile set and preserve the original
publication/encryption/interval guards. A successful not-due check clears stale transient feedback without
replacing its file/timestamp. Seven focused cases pass (six together, one final scope case alone; four
added cases), test outputs are cleaned once and strict Windows/final signed offline Android builds pass
without warnings/errors. Twenty-three saved-native observation assertions cover Off/no copy, Daily
creation, one-entry preview, explicit 36-to-37 recovery, separate two-entry safety preview, cold persistence,
final Weekly/Daily without the former error, original-copy preservation and retirement of only the new
fictitious profile. Original successful creation/restore is retained, not repeated after the readiness fix.
Final Main is 979 with its three original profiles/copies and original Daily/password choices; the phone
is released. No native crypto/schema/SDK/permission change or full-suite rerun. Raw main-store/key equality,
power loss, key loss, full hardware retention stress, other devices/iOS/OAuth/Store and release remain open.
See [qualified evidence](quality/automatic-copy-readiness.md).


## D-328 scoped physical accountant delivery

D-328 completes a scoped physical Germany accountant export on SM-A176B using the unchanged signed offline
D-327 app. The original native chooser delivers two readable ZIPs only to a separately installed offline
receiver: both accounts/five rows, then cash only/the same one transfer plus its unselected Main identity.
The host verifier checks actual CSV/manifest bytes, six member digests, default redactions, blank tax fields,
refund/fee references and separate recorded-kind totals. Empty selection, confirmation Cancel and stale-review
invalidation are exercised. Twelve saved-observation/receipt assertions pass; the receiver strict build has
zero warnings/errors. The new receiver is removed, final Home remains 979 and the phone turn is released.
No production code/APK, schema, permission or managed test count changes; successful unrelated tests/builds
are not repeated. Physical documents/notes-enabled/all-layout, iOS, tax specialist/calculation/filing and
release acceptance remain open.


## D-329 physical report PDF correction

D-329 fixes an actual anonymous balance row on page2 of the original Samsung report PDF. Repeat each
table's account/section title, exact scope and column headings as one grid header, retaining all original
values, RTL mapping, row membership and footer exclusion. Three new en/fa/de continuation cases reproduce
the original failure; all eight related Reports cases pass. Strict Windows and the complete signed offline
Android build have no warnings/errors. Data-preserving upgrade and six pages from three original native
English/Persian/German PDF exports are checked; final Home979 and original display choices remain. The
receiver is removed and phone turn released. Seven saved-native/receipt assertions and an original-byte
host verifier retain actual context/figures/visible footer evidence. A suspected footer defect is disproved
by pixels; its experimental source/build are withdrawn and not installed. Initial stale uncompressed
Samsung chooser coordinates opened an unrelated app; no further interaction occurred there, and temporary
fictitious-file grant absence is not proven. Final selection uses a fresh compressed hierarchy and exact
local receiver. No financial/schema/native-crypto change or full-suite/performance replay. Wider device,
reader/iOS/provider/Store and release gates remain open.


## D-330 own-format physical continuation

D-330 qualifies the original Samsung CSV producer/consumer roundtrip on the unchanged D-329 offline APK:
five original rows, all27 exported fields/stable IDs equal after re-export, retained refund/transfer/fee
relationships, disabled five-ID duplicate import, cold original batch and explicit Undo to zero. Only the
new matching-account profile is retired; Main979 and three previous profiles remain. Twenty-three saved
observation assertions pass; only the independent offline receiver is rebuilt with zero warnings/errors.
No maintained unit count, main source/APK or unrelated successful suite/build changes. Bank/aggregate/
malformed-file/full-device/iOS/release gates remain separate.
See [evidence](quality/physical-csv-roundtrip.md).

## D-331 existing import presentation

IO-09's count-only preview is replaced with interpreted read-only pages and a fresh-review gate after
mapping changes. This does not add a bank format or change aggregate/Undo authority. Current scope and
remaining device qualification: [evidence](quality/interpreted-csv-preview.md).

## D-332 scoped Android continuation

D-331 interpretation is exercised in the original Android page without financial writes; the new-page
viewport defect is corrected. No bank preset or financial writer changes. Preserve the observed two
background-receiver startup ANRs as an open QA-06 boundary. [Evidence](quality/android-import-preview-arrival.md).

## D-333 protected notification-only reads

Cold snooze publication and refusal/disposal preservation are locally qualified through original
application sources and an actual x64/API36 action with zero windows. Reuse the existing bootstrap and
source guards; no new financial algorithm/schema or product decision. QA-06 ANRs and broader device/
iOS/interrupted-action/release acceptance remain open. See [evidence](quality/cold-protected-reminder-startup.md).


## D-334 startup packaging decision

Both finite packaging alternatives are rejected: disabling assembly compression fails before
managed startup with a missing compressed_assembly_count native symbol; direct native-library loading
records a real binding ANR and takes24.422s through OnCreate versus the17.755s control. Single samples,
native rebuild differences and public memory counters do not establish the original root cause.
All temporary source/manifest changes are removed; ordinary application/APK remain D-333. No unchanged
successful suite is repeated or new test count claimed. QA-06 and wider device/iOS/release gates remain open.
See [evidence](quality/android-startup-packaging.md).


## D-335 More menu readability

The More menu now grows complete titles and explanations at native font scale instead of
inheriting compact title ellipsis and a two-line explanation limit. The original360/200% native case
reproduces14 trimmed captions. Both offline and personal-sync editions pass18 en/fa/de, Light/Dark,
360/412/1280 contexts each:1224 native caption observations and612 original command targets. Commands,
routes, fonts, semantic names/hints, palette and financial data flow are unchanged. No unrelated successful
suite is repeated or new unit count claimed. Broader A11Y-03, real OS scaling/readers, phone/iOS, startup
ANR and release gates remain open.
See [evidence](quality/more-menu-readable.md).


## D-336 incremental package defect

The observed pre-managed assembly-store abort comes from a cached CoreLib header retaining
index193 while the current descriptor table assigns CoreLib192 and Java.Interop193. Validate the
SDK's current compression-item metadata before its incremental timestamp check and regenerate only
incompatible owned cache entries. Seven focused behavior cases preserve valid cache timestamps,
invalidate stale index/size/magic/truncation, accept missing files and refuse outside destinations.
Keep the original SDK compression, serviced runtime, AOT profile, key/data and permissions. No unrelated
successful financial, OCR, backend or More-layout suite is repeated or new managed count claimed.
QA-06 startup ANR, two-second loaded Home, broader Android, Mac/iOS and owner release gates remain open.
[Evidence, failures and limits](quality/android-compression-cache.md).

## D-338 draft completion boundary

Eight current native Windows draft screenshots in each existing language (en/fa/de/es/fr/it)
are delivered with exact RGB/source hashes and independent fictitious package/storage/non-input
desktop isolation. Five actual native pre-startup refusals, strict Windows/Android Debug builds and
48 visual/pixel checks passed. Only the rejected English budget loading frame was recaptured from
the same fictitious data; 47 successful frames were retained. Shipping UI/APK and managed counts
remain D-336. These are development drafts, not mobile or Store acceptance/publication. QA-06,
broader physical Android, Mac/iOS, provider/accounts and owner release gates remain open.

[Evidence and limits](quality/store-drafts.md).

## D-339 A11Y-03 report legend delivery

Category/composition report names, shares and monetary packets occupy independent growing rows.
Names retain native scaling and complete wrapping; oversized amounts use the existing horizontal
readout, outside the original category navigation button. The 18 final Windows contexts (en/fa/de,
Light/Dark, 360/412/wide at process-local 200%) and six original Release/AOT Android contexts pass
native glyph/viewport/scroll/target checks. Fictitious presentation inputs are explicitly separate
from report calculations; no financial commands execute. Strict builds and the full offline signed
APK pass. The Android caller's initial startup ANR is retained: Wait continued the same original PID,
without restarting its scenario. QA-06/ANR, broader A11Y/OS readers, physical Android, iOS and release
gates remain open. Managed test counts are unchanged; unrelated passed suites were not repeated.

[Evidence and limits](quality/report-legends-readable.md).

## D-340 remaining inherited caption truncation

The remaining inherited RowTitle ellipsis truncates the original Non-monthly bills caption at
200% Windows process-local scale/360 width. The shared default now wraps with its original 15-unit
font and semantic color. All 26 other current consumers already explicitly wrap, so their effective
behavior is unchanged. Eighteen final Windows contexts/36 native captions and six Release/AOT
Android contexts/12 captions pass; original account/entry/budget inputs stay unchanged and no method
is saved. Strict builds and a complete signed offline APK pass. The independent Android startup
ANR remains an actual failure; Wait continued the same PID 1753 to its completed caption result.
Actual OS text scale/TalkBack, broader device/iOS/performance and release gates remain open. No
system setting, financial algorithm, schema, translation or managed unit count changes.

[Evidence and limits](quality/budget-group-titles-readable.md).

## D-341 rejected Home optimization

Whole-page Android timing does not support adopting the scoped-connection experiment despite
correct financial packets and an isolated Windows gain. Restore production sources and keep QA-06
In progress. The only committed code change repairs the original Data-test startup dependency.

[Evidence and limits](quality/home-publication-session.md).

## D-342 complete optional Home categories

Names, percentages and complete monetary viewports now occupy growing independent rows. Hidden
category controls detach from their unchanged source. Eighteen Windows and six Android contexts
pass; no managed count increases. Broad accessibility, performance and release acceptance stay open.

[Evidence](quality/home-category-legends-readable.md).

## D-343 complete original plan-row values

The actual shared plan-row packet splitting is corrected in both current hosts. Focused native
checks pass with original actions/accessible names and unchanged stored inputs. No managed count
increases. Broad accessibility, physical Android, performance, iOS and external gates stay open.

[Evidence and limits](quality/plan-row-amounts-readable.md).

## D-344 ledger/account readability and open renderer limit

Original ledger/account packets now scroll independently of actions in all four current hosts and
two group headers. Focused native checks preserve original inputs, selection and colors. The known
Windows Canvas root-change failure remains open; no SDK/root lifecycle workaround is adopted.
Android, broader accessibility, performance, iOS and external release acceptance stay distinct.

[Evidence and limitations](quality/ledger-account-amounts-readable.md).

## D-345 - Actual OS text-scale evidence

A11Y-03 advances with six language/theme cases and36 original monetary surfaces at emulator OS2.0/412dp;
other controls/widths/devices remain. A11Y-01 stays open after bound TalkBack yields unaccepted
audio/prompt behavior. Restore the three captured settings exactly; do not claim uncaptured flags
or broader acceptance. [Evidence](quality/android-os-accessibility-qualification.md).

## D-346 actual OS qualification boundary

Original scalable financial surfaces gain real emulator OS200%/412dp evidence with unchanged
stored inputs. Retain completed cohorts and actual native/ANR failures separately. No serial-GC
workaround is promoted after an ineffective trial; speech controls do not establish TalkBack app
acceptance. App/APK remain D-344; broader A11Y-01/03, QA-06/07 and release gates stay open.
[Evidence and limits](quality/android-os-accessibility-qualification.md).

## D-347 actual foreground review boundary

A native layout under Android's ANR cover is provisional geometry, not unobscured-window acceptance.
Require actual Activity focus and reject retained receipts lacking that proof. Preserve completed
groups and actual failures separately; the ineffective precise-Tarjan trial is removed. Main/APK
remain D-344 and broader accessibility/runtime/platform gates stay open.
[Evidence and limits](quality/android-os-accessibility-qualification.md).

## D-348 reader observation correction

Installed stock UI automation suppresses other accessibility services. The independent bounded
observer preserves TalkBack and serializes its single native client. Actual retained Main gestures
produce Recorded balance accessibility focus and captured audio; offline transcripts are unreliable,
so spoken-reader acceptance remains open. Settings restore exactly, notification flags and MainPID
are preserved, and the owned remote JAR is removed. Main/APK/count remain D-344. No unrelated
successful suites/builds repeat. A11Y-01/03, native ANR, remaining device/iOS and release gates stay open.

## D-351 shared reimbursement calculation

Replace repeated full-history scans with one relevant-refund index while preserving financial and
overflow semantics. Fifteen new regressions and affected existing report/KPI cases pass in39 selected
cases. No full-suite count or whole-Home speed acceptance is inferred. Current evidence and limits:
[reimbursement batch](quality/reimbursement-batch.md). Canonical QA-06 and native/platform gates stay open.

## D-352 complete reimbursement-page integration

Worker snapshot reads, atomic publication, bounded result rows and original loading/failure/retry
replace per-row native construction. Original amounts/ordering and complete monetary packets stay
under their existing financial authority. A separately scrolling empty flow fixes the actual
Windows header/empty overlap. Native assertions and images qualify the original sources; managed
counts are unchanged and unrelated successful suites are not repeated. Canonical QA-06/A11Y/device/
iOS/release gates remain open. [Evidence](quality/reimbursement-page.md).

## D-353 Home template reload

Remove unnecessary clear/add native teardown using complete snapshot publication. Original-source native
checks cover retained controls and real count changes without posting money. This does not close QA-06
or platform acceptance. [Evidence](quality/home-template-publication.md).

## D-354 rejected complete-row transport

The complete-row JSON candidate passes correctness but regresses tenfold native reads and is withdrawn.
Nine new actual-history boundary tests pass against restored production. QA-06 and current best scalar
reader remain unchanged; no failed experiment is shipped. [Evidence](quality/complete-history-row-packet.md).

## D-359 Android incremental variant correction

Track requested manifest-overlay order/content/removal before the SDK's incremental merger.
Focused build regressions and signed independent on/off checks qualify packaging, without changing
main features or managed suite counts. Actual runtime ANRs remain retained and open in canonical
QA-06; scoped Samsung unsaved-goal cancellation is separate from full physical/A11Y/iOS/release
acceptance. [Complete evidence](quality/android-manifest-overlay-incremental.md).

## D-360 Android root retirement

Release owned native navigation fragments at managed retirement, with reordered pending add/remove.
Eight original root groups and six actual translated Back actions preserve inputs/preferences.
Broader QA-06/physical/reader/iOS/release remain open. [Evidence](quality/android-retired-navigation.md).

## D-361 physical original-navigation continuation

D-360 installed bytes are verified on Samsung. Original three-language/two-theme Settings/Back
paths, three goal types/methods and dirty-draft cancellation preserve Home979 and empty goals.
Native roots remain bounded to one; exact original display/system settings restore. No app code,
APK or managed count changes; broader device/ANR/reader/iOS/release remain open.
[Evidence](quality/physical-navigation-and-goal-drafts.md).

## D-362 finite managed-startup investigation

Separate native/preparation/settings/security phases in original independent Release/AOT startup.
Reject immutable-model overlap after no end-to-end benefit; retain native engine/guards, original
Home/data and exact settings. Next isolate actual protected factory/context creation from the
first EF settings query before another implementation. App/APK and managed counts stay D-360.
[Scoped original evidence](quality/android-managed-startup-phases.md).

## D-375 - Owner-requested Home title action

Completed independently of new Phase2 capabilities: reclaim the bottom Add band through a real 48 px
button beside the growing title. Native layouts, no-reset main upgrades and open/Cancel pass; other
docks and money/security behavior are unchanged. Wider runtime, reader and external acceptance gates
remain open. [Evidence](quality/home-add-header.md).

## D-376 - Existing Home guidance correctness

Completed the observed guidance defect without adding a Phase2 feature: unrelated records cannot
complete or hide the three named tasks. Actual source tests and native publication pass; no new
financial posting, schema, permission, commercial or provider scope.
[Qualification and limits](quality/home-getting-started.md).

## D-377 - Complete Home materialization cost

Improve existing complete Home reads through an exact standard-empty-list path; no new Phase2
feature, financial posting, schema, permission or provider authority. Original native cohorts retain
all data and presentation; the actual Samsung crash remains a wider runtime concern.
[Results and limits](quality/home-empty-tag-materialization.md).

D-389 delivers the owner-approved daily ECB integration under the canonical commercial backlog,
including explicit daily provenance and pre-payment/free-original disclosure. Other providers,
public activation and platform/store acceptance remain separate; see quality/ecb-reference-integration.md.


D-392 completes a bounded financial qualification/correction batch: every new ledger import has immutable
Undo snapshots and existing aggregate ranges remain visible in Simple mode. See the canonical
commercial backlog and quality/import-undo-and-retained-aggregates.md; broader acceptance stays open.

D-396 addresses the ready original reconciliation financial gap with immutable review, edited-consent
retirement and atomic persistence. See the canonical commercial backlog and
quality/reconciliation-review.md; dedicated stability/accessibility stays deferred under D-386.

D-397 keeps final advance-bill acceptance separate from notification/navigation: return retries never
post the difference again. Focused original application/native qualification is in
quality/settlement-commit-lifetime.md. Preserve the observed Home stale-resource error as ready
development; dedicated stability/accessibility remains after development under D-386.

## D-398 - Original Home refresh source changes

Ready feature correctness continues: unchanged original Home packets superseded by saved sources
are reread under the original cover/file, without bypassing reviewed actions or rights. The bounded
gap is locally qualified; see quality/home-refresh-supersession.md. D-386's later runtime/A11Y order remains.

## D-399 - F2-TX-02 / REC original command acceptance

The observed committed-notification gap is locally qualified across foreground full/partial payment,
link/skip/override/undo/reopen. Preserve accepted results and retire the original form before native
follow-through; no money/schema change. See quality/occurrence-commit-lifetime.md. D-386 still defers
dedicated general runtime/accessibility work until ready approved development is finished.

## D-400 - Bounded occurrence state conflict delivery

Original stale correction refusal is locally complete:55 related app/70 SQLite cases, four actual
native commands per Windows/Release AOT Android, six original translated message views per platform,
strict builds, signed offline candidate and Main/OS preservation. No unrelated full-suite replay or
whole-device/iOS/ANR/A11Y/provider/release acceptance. See quality/occurrence-state-conflicts.md.

## D-401 - Original occurrence refresh and physical lifecycle

Complete the observed retained-tab suggestion correction without replacing typed drafts or pending
consent. Independent related native/managed checks and physical override, skip/unskip, manual link/
unlink and generated-delete reopening with one cold read are locally qualified. Main/profile/OS
restoration and phone release are recorded; broader QA-01, D-386 runtime/A11Y and external gates stay
open. See quality/physical-occurrence-corrections.md; no unchanged successful matrix was repeated.

## D-402 - Original refund allocation and remainder on the phone

The bounded QA-01 AT-13/14/15 continuation passes with the retained D-401 candidate: prior purchase,
refunds into two original EUR accounts, explicit excess refusal, same-draft correction, cold links/
periods/balances and exact Main/profile/OS restoration. No source/build/test-count change or financial
replay; broader QA-01, runtime/A11Y, chart/iOS and external gates remain. See quality/physical-refund-lifecycle.md.

## D-403 - Original first-run portable device continuation

The bounded AT-57 same-phone independent Release reinstall path is complete with original native
preview/cancel/accept and cold public/whole-export readback. Permanent Main stays installed, unchanged
and restored; the two owned fixtures and matched external file are removed. Continue ready backlog
work under D-386, retaining existing successful baselines. Protected-password/new-device/iOS, wider
QA-01, runtime/A11Y and external gates remain separate. See quality/physical-portable-fresh-install.md.

## D-404 - Bounded original card payment and archive continuation

AT-07/AT-10 gain original Samsung card expense/payment/positive credit, dirty archive cancel/accept,
both dependent active plans ended, default-cleared notice, historical report preservation and original
unarchive/cold without resuming plans or choosing a default. Main979/five public packets/three profiles/
OS/APK are restored and only the new inactive profile retired. App/APK and managed counts remain D-401;
no unchanged successful suite/build replay. Broader QA-01, D-386 runtime/A11Y and external gates remain.
See quality/physical-credit-card-archive.md.

## D-405 - Stale ordinary CSV review refused before money

Original own/mapped CSV now uses the existing coherent original-file financial packet in its atomic
writer. Changed opening/history facts and departed-form tokens need explicit Refresh; raw source and
choices remain. Eight new cases, retained related checks and12 original native financial contexts per
Windows/Release AOT Android pass, with strict builds/full offline signed APK and no-reset Main/Home0/
OS restoration. No full-suite or accepted-money replay. Broader QA/import, D-386 and external gates
remain separate. See quality/original-csv-financial-review.md.

## D-406 - Original physical opening-boundary CSV and retained-tab review

Samsung's original picker/preview/import stores one older12.34 expense and opening-day20 income in
one new fictitious Cash900 profile. Home920 excludes the older row; explicitly moving only opening
October20 to18 gives907.66. One planned cold preserves both original public details/dates/notes and
the durable two-row batch. Original Undo restores Home900/empty ledger while opening18 remains.
A real retained More-tab departure did not call OnDisappearing and left Import enabled. The original
Shell now retires that review on uncancelled tab navigation after existing leave guards. Final full
signed offline Release upgrades without reset and physically proves disabled Import, retained source
and explicit Refresh without financial replay. Strict Windows and complete Android builds have no
warnings/errors. Managed algorithms/tests are unchanged; retained D-405 successes are not rerun or
counted as new cases. Main979/five public packets/three profiles/OS are restored; only the new inactive profile
and exact hash-matched public file are removed, and the coordinated phone turn is released. No schema/model, resource, permission or provider change.
Broader device/import/report, D-386 runtime/A11Y, iOS/store/release and exact-head CI remain separate.
See [bounded evidence](quality/physical-opening-csv.md) for original receipts, preserved failures and completion status.


## D-407 - Original split/join financial state and physical lifecycle

Two real SQLite regressions first prove that retained split/join could overwrite a newer original
note. The coherent actual-file/group/account/category/refund/attachment review is now rechecked in
the existing atomic writer before EntryActions mutates copies. Changed sources/pending input refuse
before money and retain raw shares; known committed notification failures leave the original form
terminal/Close-only. Original writer rights, exact total and attachment ownership remain preserved.
Sixteen new Data and3 actual app-session cases pass selectively, with retained two-case native
Windows/Release AOT Android finance,36 Windows observations across en/fa/de light/dark360/412/1280
and12 Android observations. Observer failures continue the original stored refusal and pending
acceptance without reseeding or replaying successful money. Strict builds and complete signed offline
Main APK pass. Samsung's original new Cash900 profile records one100 expense, splits60/40, reallocates
70/30, preserves both public metadata packets and Home800 through one cold read, then combines once
to one100 Food row/Home800. Main979/five known public packets/three profiles/OS are restored and only
the new inactive fixture is retired before releasing the phone. No full-suite/unchanged successful
matrix rerun, model/schema/permission/provider change or release claim. Broader QA-01, D-386 runtime/
A11Y, iOS/store/external and exact-head CI remain separate. See [original evidence and limits](quality/original-split-financial-review.md).


## D-408 - Original holding event review and retained acceptance

Two real SQLite regressions first reproduce a stale correction replacing newer source data and an
ordinary exception after a purchase already committed. The original event/type/history/locations/
accounts/group/refund/attachment/valuation packet is now rechecked in the existing atomic quantity/
payment/fee/price writer. Copies preserve raw drafts and immutable reviewed facts. Foreign money or
unreviewed event identity, changed sources/access and pending input refuse before writes. Known
accepted notification/native-return failures retire the original form with Close-only navigation.
One explicit new location can be adopted only while all original financial facts stay unchanged.
An observed native empty-query redelivery now preserves the loaded review instead of losing it.
Twenty new Data and3 actual app-session cases pass selectively, plus46 original related cases once.
Two original financial cases per independent Windows/Release AOT Android preserve exact quantity,
fee/group/derived price, Cash874/929 and income0/expense1.36 Windows observations cover en/fa/de,
both themes and360/412/1280;12 Android observations cover the new native feedback and retained state.
Original Windows pre-command data is continued without reseeding or accepted-money replay. Strict
builds and complete signed offline Main pass; the independent emulator restores Home0/My account,
exact OS values/size/density and final Main hash through one no-reset upgrade. No physical phone,
private installed key/database, schema/model/permission/provider change or full-suite replay. Wider
holding relationships/device/iOS, D-386 runtime/A11Y and external/release/CI gates remain separate.
See [original receipts and limits](quality/original-holding-event-review.md).


## D-409 - Corrected source and retained refund boundaries

Three original real SQLite failures reproduce a corrected holding fee smaller than its retained
refund, a corrected holding source moved to an incompatible account currency and the same ordinary
expense-source currency gap. One shared REF-04 source validator now checks final linked refunds in
the existing atomic ordinary/holding writers before mutation. Explicit valid deletion/unlink and
same-batch source/refund correction remain supported. The actual editor shows amount/account errors
beside their fields, preserves raw input and accepts only an explicit valid correction of that same
original form. Known acceptance remains terminal; no conversion or guessed relinking is introduced.

Thirteen distinct new cases (8 Data, 5 Core) and17 affected existing cases pass in retained selective
cohorts, not a full-suite rerun. Two original native correction cases per independent Windows and
Release/AOT Android preserve exact refund identity/link/amount/currency/metadata, quantity/basis/price,
Cash875/874.75, income0 and net expense0/0.25. Four live observations prove refused Save remains enabled
and accepted Save is disabled. Afterwards36 Windows and12 Android read-only rejected-draft caption
observations cover en/fa/de, both themes and Windows360/412/1280; they rehydrate original refusal
presentation with retired authority and do not replay finance. Strict builds and the complete signed
offline Main pass. One no-reset emulator upgrade restores Home0/My account and exact OS/size/density.
No phone input, Main private database/key read, schema/model/permission/provider change or release
claim. Malformed optional fee text remains a source-inspected next concern, not fixed or reproduced
here. Broader financial/device, D-386 runtime/A11Y, iOS/external and exact-head CI gates remain separate.
See [original evidence and limits](quality/holding-fee-refund-limits.md).


## D-410 - Original optional holding-fee input

Three actual editor/SQLite regressions first prove malformed, negative and out-of-range nonblank
fees committed instead of refusing. The original editor now distinguishes invalid input from an
explicit blank/zero removal before building financial entries. It preserves raw fee/note, original
review and enabled correction with existing field-local feedback. Preview omits guessed zero-fee
or wrapped account changes. The actual non-UI editor is linked through its original native-effect
ports; native query delivery remains in its unchanged MAUI adapter. Seventeen distinct new actual
app-flow cases pass in retained selective cohorts, without replaying successful D-409 cases.

Two original native malformed/negative cases per independent Windows and Release/AOT Android each
refuse with complete rows unchanged, then correct that same original form once to fee1EUR. Both
preserve refund0.75EUR with complete original metadata/link, Cash874.75, quantity/basis/derived price,
income0/net expense0.25 and terminal accepted state. Four original live assertions precede accepted
receipts. Windows retains4 live plus6 English captures, then continues only12 pending fa/de captions;
Android continues only read-only presentation after its original observer failure, with6 final native
caption observations. The expectation now respects native Persian digits rather than raw Latin
resource examples; production text was complete. Original failures and accepted files are retained.

Strict builds, complete signed offline Main and no-reset independent emulator Home/OS restoration
pass. No phone input, private Main data/key read, full-suite rerun, schema/model/resource/permission/
provider change or ANR/crash resolution claim. Wider holding/device/iOS, D-386 runtime/A11Y and
external/release/exact-head CI gates remain separate.
See [original evidence and limits](quality/holding-fee-input.md).

## D-411 - Refund action for holding fees and original physical lifecycle

An original Samsung purchase exposed an eligible fee refund hidden by the managed-source actions
container. The existing CanRefund row now has its own card; purchase/sale principal edit/delete stay
in holding history. Four original accepted financial commands (purchase, linked refund, same-form
fee correction, sale) produce Cash900→848→849.50→850→879.50EUR. Out-of-range and below-refund fees
refuse with raw drafts retained. One terminal cold preserves five known public packets, one unit,
25EUR/unit original basis/price and displayed realized5EUR before fees, without a tax claim.

Twenty-six inspected Windows captures/native observations and24 final read-only Release/AOT Android
action contexts cover en/fa/de and both themes, plus Windows360/412/1280. Original Android semantic
observer and independent modal/route failures are retained; independent Android modal Cancel is not
accepted. Original seeded rows are reused read-only and accepted money is never replayed. Windows
native refund open/Cancel and the actual Samsung refund Save qualify the original action separately.
Strict builds, complete signed offline ARM64/x64 APK and no-reset emulator restoration pass. Samsung
Main979/five known public packets/original three profiles and exact OS values are restored; only the
new inactive fixture is retired and the coordinated phone turn released. No new unit-test count,
full-suite replay, schema/model/resource/permission/provider or Main private-data/key change. QA-01
overall, other holding variants, D-386 runtime/A11Y, iOS/external/release and hosted exact-head CI remain.

See [original evidence and limits](quality/physical-holding-fee-lifecycle.md).

## D-412 - Original holding quantity range input and field feedback

Three original parser and two actual editor/SQLite regressions reproduce Decimal base-unit conversion
and checked quantity-preview overflows. Check the original number against Int64/factor before
multiplication, distinguish range refusal from ordinary invalid quantities, omit an unrepresentable
preview and refuse the original proposed range before financial-entry construction. Keep raw drafts,
original review and explicit same-form correction; preserve the existing checked domain/atomic writer.
The new six-language feedback appears beside Quantity without a duplicate form message.

Sixteen distinct new cases and21 existing resource checks pass in retained selective cohorts; no
unchanged full suite or D-408..411 accepted money/matrix replay. Two original native refusal/correction
cases per independent Windows and Release/AOT Android preserve Cash874.75, source money/refund/price
and basis25 while each valid nonfinancial correction adds2units to the original1. Four original live
observations per platform precede only read-only presentation. After field placement changes, final
Windows18 and Android6 caption contexts reuse the exact original accepted receipts/files with zero
new commands; original Cases retain their original PID and Live is empty. Strict builds, complete
signed offline APK and no-reset emulator Home0/My account, OS and hash restoration pass. No Samsung
input, Main private database/key, schema/model/storage scale or provider change. Broader finance/
holding variants, D-386 general runtime/A11Y, physical/iOS/external/release and exact-head CI remain.

See [original evidence and limits](quality/holding-quantity-input.md).

## D-413 - Original manual holding-price Save/delete and accepted result

Bind manual prices and confirmed deletion to a coherent original type/history/price/location packet,
revalidated in the existing actual-file writer with fresh rights and current draft/confirmation.
Keep raw price/date/mode on changed facts, retire departed authority and offer explicit Refresh.
Known accepted Save/delete survives notification/readback failure without another write; an old-row
deletion preserves a separate new-price draft. An ordinary empty return may refresh history; raw or
failed drafts require explicit review. Keep field-local feedback, scalable native actions and six
translations. Fifteen new selective actual app/SQLite cases,74 affected policy checks and21 resource
checks pass; original native receipts and read-only continuation keep successful financial operations
from being replayed. Full runtime evidence and limits: [original manual price review](quality/holding-price-review.md).

## D-414 - Original holding history deletion and file-bound short Undo

Complete the original holding delete/Undo cycle: coherent displayed-row/group confirmation, fresh
actual-file writer rights/source checks, exact committed deletion receipt and accepted restoration.
Preserve explicit fee-refund links and orphan receipt ownership/content; reject changed/foreign/
mutated/consumed Undo before money and recheck the restored original packet. Keep uncommitted retry,
retire departed actions and distinguish known acceptance from failed notification/readback. Twenty
new selective actual app/SQLite cases, four affected prior cases and21 resource checks pass. Original
native commands and retained read-only captions are qualified separately, without replaying accepted
work. Short Undo remains process-local; no schema/model, new permission/provider or release claim.
See [original evidence and remaining gates](quality/holding-history-undo.md).


## D-415 - Original holding archive confirmation and accepted result

The detail archive no longer saves a retained whole-type copy. Revalidate the original coherent
actual-file type/history/prices/locations packet with fresh Corrections rights and current confirmation,
then change only the tracked type's archive flag. Refuse stale/foreign/mutated/consumed or departed
review, preserve financial history/refund/receipt ownership and raw price drafts, and retain known
acceptance before fallible notification/return. Explicit Refresh reads only; absent/failed reads never
rearm a stale or accepted archive. Thirteen new actual app/SQLite cases pass in selective retained
cohorts, without repeating accepted flows;23 resource checks also pass. Original Windows/Android
Cancel/refusal/archive and read-only captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass. No schema/model, new permission/provider or release claim. Evidence and remaining platform gates: [original archive review](quality/holding-archive.md).


## D-416 - Original type editor creation, edit and accepted identity

The actual type form now owns one coherent actual-file original type/history/price review, or one
stable absent new identity. Recheck fresh rights, immutable source and exact pending raw draft in
the existing writer; copy before mutation and preserve noneditable metadata and dimension rules.
A missing original row never becomes a new form on empty query redelivery. Keep invalid/raw input
for correction, retire stale/departed authority, and require close/reopen after changed source.
Known creation/edit acceptance consumes the original form before notification or navigation; a failed
next step cannot create a duplicate or repeat Save. Nineteen new actual app/SQLite cases pass in two
selective cohorts, without replaying accepted cases;23 resource checks also pass. Original Windows/
Android refusal/edit/create and retired captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass; no schema/model, new permission/provider or release claim. See
[original editor evidence and remaining gates](quality/holding-type-editor.md).

## D-417 - Original physical holding lifecycle

The existing D-416 signed offline candidate passes one original Samsung type creation/edit,
quantity refusal/correction, manual price/delete, immediate purchase-group Undo and archive/cold
lifecycle. Cash849.50, original refund link, type name/note and history persist; Main979/five public
packets/exact three profiles/OS are restored and the phone turn released. Only the new inactive
fixture is retired. No new app/APK/tests/builds or wider D-386/iOS/external/release acceptance is
claimed. The observed refund category is Uncategorized, without a Fees-selection claim. See
[original evidence, observer continuations and remaining gates](quality/physical-holding-type-price-history.md).

## D-418 - One original coherent holding overview

Replace separate initializing/profile-following overview reads with one actual-file worker transaction
and a complete prepared row/value packet. Recheck original source/rights, bound same-file supersession,
cover loading/failure with explicit Retry, and retire departed publication/native targets. Open/Add
check the actual file even under inactive test policy; an empty read creates no settings. Nineteen
distinct new actual app/SQLite cases and23 resource checks pass in selective cohorts, without a full
suite or accepted financial replay. Original Windows and Release/AOT Android loading/error/Retry,
ready/empty, retirement and native targets preserve all typed fixture rows. Only changed six-language
loading/Retry captions are rechecked after visual correction. Strict builds, complete signed offline
APK and no-reset emulator Main/Home0/OS restoration pass; Samsung is untouched. No schema/model or
privacy boundary changes, speed/general ANR/crash/A11Y/device/iOS/store/release acceptance is claimed.
See [original evidence, observer continuation and limits](quality/holding-overview.md).

## D-419 - Complete original holding detail publication and routed identity

Read one actual-file detail/settings packet on a worker without initializing settings, prepare all
quantity/value/history/price fields and publish under an original loading/error/Retry cover. Retire
departed/retained-tab work and recheck original native navigation/file targets. Preserve raw price
input for explicit Refresh, existing accepted editor outcomes and theme-only color rebinding. A new
routed type receives its own editors; old drafts/short Undo/native targets never move to it. Twenty
distinct new actual app/SQLite cases pass in selective cohorts; no full suite or unchanged resource/
financial replay. Retained original fixtures qualify57 Windows views plus only two new query views
and23 Release/AOT Android observations, with no seed/UI financial command. Strict builds, complete
signed offline APK and no-reset emulator Main/Home0/exact OS restoration pass; Samsung is untouched.
A final known-commit/departure test and one retained-source AOT callback control close automatic
readback to a departed view, without replaying completed native matrices. No schema/model/privacy boundary change or general runtime/A11Y/device/iOS/release acceptance.
See [original failures, continuations and limits](quality/holding-detail-publication.md).

## D-420 - Original holding-place creation and known metadata outcome

Create a named place only under the original actual-file holding review and pending form owner,
with fresh rights and a stable absent identity. Prompt ownership closes financial Save before the
name dialog. Recheck source and owner in the same transaction, consume the old review on metadata
acceptance, and retain that known identity before notification/readback. A failed following step
shows saved-place feedback and an explicit read-only choice refresh; it never creates the place
again or resets raw financial inputs. Transfer keeps its original source and selects the new destination.
A departed/replaced form cannot adopt old metadata or recover financial authority.

Eighteen distinct new actual app/SQLite cases and 23 resource checks pass in selective cohorts.
The three original baselines fail before repair; later cohorts run only new boundaries/outcomes.
Original Windows UIA and independent Release/AOT Android native controls each create one place and
recover its choices read-only, with zero financial Save, then retain captions across the required
language/theme/width contexts. All typed rows outside that one location addition remain unchanged.
Strict builds, complete signed offline APK, no-reset Main/Home0 and exact emulator OS restoration
pass. No schema/model, new data/permission/SDK/provider or general ANR/crash/A11Y/phone/iOS/release
acceptance is claimed. D-386 and remaining external gates stand.

See [original failures, focused evidence and limits](quality/holding-location-creation.md).

## D-421 - Complete original holding-event form publication and routed draft ownership

Keep the event form covered until its original coherent read, actual-file revalidation and complete
field publication succeed. Failed reads offer explicit original-file Retry and Cancel; they do not
initialize display settings or leak an exception out of query delivery. Validate the original event/
type before missing-default scaffolding. Required default creation retains Corrections policy and
one known metadata receipt through readback failure, without recreating a deleted accepted default.
A published raw draft gets read-only Retry; unavailable kind/account/place is never replaced silently.

A new routed event/type owns a blank draft and separate financial session. Empty redelivery retains
the existing form. Original pending/departed reads and accepted financial callbacks cannot publish,
close or activate another view. Keep archived historical payment/place identities and rebind a
retained transfer destination by identity. 22 distinct actual app/SQLite cases pass in selective
cohorts, followed only by affected checks. Final strict builds, 56 actual Windows views and 20
independent Release/AOT Android observations pass with original typed rows unchanged. Complete signed
offline APK and no-reset Main/Home0/exact emulator restoration pass. No unchanged full suite or
resource matrix replay; no schema/model, new permission/provider or general ANR/crash/A11Y/phone/iOS/
release acceptance. D-386 and remaining external gates stand.

See [original failures, native observations and limits](quality/holding-event-publication.md).

## D-422 - Original physical holding-place move and dirty discard

The existing D-421 signed offline APK passes one original Samsung opening3/basis30EUR and move1:
Home2 plus D422_Storage1, total3 and average basis10 remain, with Cash900 and an empty current-month
ledger. One new place is adopted by the original move draft without losing quantity/date/note or
changing its source. Keep editing retains raw2; Discard keeps the saved move1. One terminal cold
retains original history and quantities. Restore Main979/exact five public packets/three profiles/
OS/hash, retire only the new inactive fixture and release the coordinated phone turn.
Twenty-four retained-observation assertions are not new unit tests. No new app/APK/build or successful
suite/accepted financial sequence replay. A separate unsaved USD-type draft exposes unexpected
native currency-search text and is discarded; USD selection/new event and its root cause are not
qualified. Next ready work diagnoses only this original input/list boundary. D-386 general runtime/
A11Y, broader QA-01, Mac/iOS, external/expert/store and release gates remain separate.
See [original sequence, failed observations and limits](quality/physical-holding-location-move.md).

## D-423 - Original native currency selection, without a product workaround

The independent Release/AOT Android caller opens the actual unchanged AccountFormView currency
control once. After actual native realization, the original visible 44-unit GBP button selects GBP
in both the model and control and closes the popup. Stored accounts and entries compare unchanged;
there is no Save surface. The early active-window dump and raw ContentDescription were incomplete
observers, not proof of a blank product list. Preserve failed observations; remove all speculative
production viewport changes. Main code/APK remain D-421, Main resumes with its original PID/Home0,
and no phone input or system setting changes occur. The final independent strict build has zero
warnings/errors. No unchanged Main build/full suite or accepted financial sequence is repeated;
no new unit-test count, physical USD-input, spoken accessibility or general runtime acceptance is
claimed. Continue remaining ready feature/device work with D-386 and external gates separate.

## D-424 - Original entry-detail attachment owner and complete file names

Bind detail attachment reads, native picking and confirmed removal to the original displayed entry,
actual file, metadata and view owner; recheck cached rights and original bytes under the writer.
A changed/departed picker cannot target another query/profile. Consume known commits before
notification/readback; show the saved/reopen outcome without resubmission. Give the complete file
name its own row, separate from reading/removal actions.13 focused actual-source cases pass; no full
suite replay or schema/model/permission/provider change. Retain54 original Windows caption/Cancel
observations and18 changed-row continuations over the same original file. The latter completes its
report and then exits with native0xC0000005: general runtime acceptance is not claimed. Final strict
builds and complete signed offline APK pass. The normal installed Android emulator exercises one
original PDF pick/add, removal Cancel, original ledger Delete/Undo within8s, cold without the input,
and document-only removal with unchanged expense/balance. Restore Main0/original profile set/OS and
retire only the fixture. These are not physical-device, OCR/viewer, general ANR/A11Y, iOS or release
acceptance. Continue remaining ready original entry-editor/detail work under D-386.

## D-425 - Original editor Save and pending receipt

The original editor financial writer records acceptance before notification and captures its exact
saved attachment destination. Submitted financial rows/document bytes are detached; late profile,
query, draft or view changes cannot adopt the pending write. Document failure stays separate from
saved money and never rearms Save.12 distinct focused cases pass in cohorts10+2; no full-suite replay.
One real Windows editor/native Save preserves expense1234/document bytes through two deliberate
postcommit notification failures; original fixture/account seed is reused after failed observations.
See [evidence](quality/entry-editor-save.md). Final Android evidence and wider gates are recorded
there. No schema, compiled model, portable format, permission or provider change. Continue ready
original entry/detail work under D-386; general ANR/crash/A11Y and external release gates remain open.

Final installed Android qualification (D-425): upgrade the complete signed offline APK without reset,
then one original Home/PDF receipt and Save in a separate fictitious profile. The stored24.50EUR/
17October/Food/D425_Cash and13KB document survive one cold after the exact input is removed.
Restore Main0/original public profile set/current-month history/OS and retire only the inactive
fixture.12 focused cases, final strict Windows/Android builds and one actual Windows Save pass;
prior negative observers remain recorded. No physical phone, merchant-corpus, general ANR/A11Y,
iOS/provider/store or release acceptance; original edit/overlap/Undo/detail gates remain separate.

## D-426 - Complete original transaction detail publication and actions

Prepare original entry/reference/group/refund/holding/document/rule facts through one captured
actual-file worker transaction, without creating settings for display. Validate the complete packet
before covered synchronous publication. Keep loading, failed read-only Retry and missing states
separate. A routed identity owns its own session and command instances; old commands, confirmations,
template prompts, OCR/viewer work and departed reads cannot adopt a different publication.

Review updates only the original flag; explicit rule creation uses original category/match and
refuses an already equivalent rule. Delete retains the original exact group/refund/document Undo
contract and deadline. Consume known commits before notification; recovery after saved work only
rereads. A refused viewer request removes only its newly created cache file, while a possibly accepted
native request retains the existing startup cleanup policy.

35 distinct new actual-source behaviors and selective affected/resource checks pass; no unchanged
full suite is replayed. Original Windows evidence includes18 language/theme/width ready contexts,
covered failure/native Retry, query/missing/retired-owner cases and two one-time native metadata
commands through deliberate postcommit notification failures. Preserve the first pre-action report
serialization refusal and continue only its exact original file without reseeding. Final Android,
APK and delivery evidence is recorded in [the detailed qualification record](quality/entry-detail-publication.md). No schema/model, portable format,
permission/provider or broader ANR/crash/A11Y/physical-device/Mac/iOS/store/release acceptance is
inferred. Continue ready original financial workflow work under D-386 and the existing external gates.

## D-427 - Complete original transaction editor publication

The editor prepares its original references, group, refunds, tags and rules through one actual-file
worker read transaction. Complete fields stay covered until publication; initial failure offers
Retry and an unavailable explicit source never becomes a new blank entry. A different query owns
fresh raw input and commands. Read-only refresh preserves the original draft; source changes refuse
an unsafe Save. Original detail/template/receipt handoffs and the writer validate exact file/rights
and original financial facts, including later refunds of sibling group entries. Both duplicate fees
keep their values with fresh identities. Pending writer waiting and known acceptance never rearm
saved money. Derived currency flags and optional icon reset finish before the form is uncovered.

33 distinct focused new source behaviors, the related core tag case and23 resource cases pass;
there is no full-suite replay. Original Windows evidence totals96 observations across retained
processes, including18 language/theme/width contexts and two one-time native Save invocations.
The independent Android sequence totals36 original observations, with two native Saves. Final
strict builds, complete signed offline APK, no-reset Main upgrade and protected original form
open/Cancel pass. Native observer failures and general Windows exits remain documented. No schema,
compiled model, portable format, permission or provider change. D-428 remains the next separate
aggregate-overlap/choice/writer/Undo batch. D-386 general runtime/A11Y priorities and external gates
stand.

[Original evidence and acceptance limits](quality/entry-editor-publication.md).

## D-428 - Original transaction account/aggregate workflow

Explicit nested account creation/default publication retains the original raw/receipt draft; cancelled
or retired children cannot write/adopt another file. Newly introduced aggregate coverage is reviewed
individually and rechecked atomically, including document/refund/prior-consumption boundaries.
The existing adjustment journal retains manual source identity; short original Undo restores only
aggregates and keeps details. Acceptance precedes observers; saved forms remain Close-only.
Full native review bodies and truthful transient Home/Transactions Undo notices replace clipped or
misleading captions.22 focused new behaviors and23 final resource cases pass; no full-suite replay.
See [original workflow qualification](quality/entry-workflow-ownership.md) for native actions, preserved
failures, APK and scope. No schema/model/portable-format, permission/provider or external acceptance
change. D-386 still defers general runtime and Android accessibility until after ready features.

## D-429 - Original plan/debt form publication and Save ownership

Complete covered actual-file publication, explicit Retry/missing states, original raw/query/command
lifetime, typed transaction-to-plan handoffs and known nested account return are implemented. Debt
payment requires an active original-currency money account; unknown principal is not guessed.
Temporary readiness errors retain editable raw fields with Save disabled. Original writers validate
new/edit/split sources and full saved semantics atomically, preserving recurrence/history and recorded
money. Known acceptance precedes observers and cannot rearm.27 focused new actual-source behaviors
pass across retained cohorts; affected prior failures are corrected selectively, without a full-suite
replay. [Source/native/build evidence and limits](quality/plan-editor-ownership.md). No schema/model,
compiled-model, portable-format, permission/provider/SDK change. D-386 and external gates remain.

## D-430 - Original plan monetary contexts

Implemented original source/destination culture/currency/display-unit capture, growing native feedback,
clear-and-reenter semantics and original writer revision guards.22 new focused cases and selective
related checks pass; [native/build evidence](quality/plan-money-context.md) retains incomplete observations,
accepted writes and runtime limits. Next: D-431 original goal/budget monetary units, preserving existing
quantity rules and accepted source/native evidence. D-386 and external gates remain separate.

## D-431 - Original goal/budget monetary meaning

Each target/contribution and total/category raw amount retains its independent original currency,
display unit and parse culture. Unit changes require complete clearing/reentry; partial edits cannot
renew meaning. Dirty context meaning survives reload, including identical digits in a new unit.
Hidden limits expose actionable review; share-of-income ignores irrelevant fixed amounts and physical
quantities retain their units. Original raw revision/native owner is checked under paired writers.
Known goal/budget/cut acceptance precedes notification and cannot rearm.25 new and75 affected prior
source cases pass selectively, without a full-suite replay. [Native/build evidence and retained failures](quality/goal-budget-money-context.md).
No schema/model/portable-format, permission/provider/SDK change. D-386 and external gates stand.

## D-432 - Original transaction monetary contexts

Each original transaction amount, destination amount, source/destination fee, ISO purchase amount and
reimbursable amount retains its own currency, applicable display unit and parse culture. Unit/currency
changes require complete clearing/reentry; partial edits never renew meaning. Original labels and
format examples remain visible. Receipt/template adoption starts from authoritative minor units;
account Undo and read-only/nested return retain original context. Repeating-plan handoff validates only
its supported primary/destination amounts, without borrowing unused fee meaning. Split shares retain
independent contexts; Join uses the original stored total. Raw/context revisions are rechecked under
original writers; known acceptance remains final. 27 new focused source cases and 51 related prior cases
pass across selective executions, with no full-suite or unrelated successful-check replay.
[Original native/build evidence and retained failures](quality/entry-money-context.md). Account monetary drafts remain the next separate boundary; dedicated stability/accessibility and all external gates retain their approved order.

## D-433 - Common CI recovery in progress

The D-422 and D-431 hosted test logs identify the same two failures: the mode-policy scanner treats
review-only source fixtures as shipping readers, and a shared-membership Undo fixture supplies a
fabricated group without the original deletion receipt required by D-414. D-431 Windows additionally
refuses installer 18.11.12306.390 before compiling, matching official image 20261018.275.1 inventory.

The scanner now names the 13 exact additional fixtures and validates their complete outer review-only
build gates; it does not exempt the Diagnostics directory or ordinary Release readers. Seven new
positive/negative gate cases prevent unsupported conditions, outer fallbacks and escaped source.
The Undo policy fixture first obtains one real committed deletion receipt, then revokes membership.
A new case proves refusal leaves both deleted rows and that original receipt intact, followed by
successful original restoration when membership returns. No production commercial or provenance
guard changes. Eight focused Core and 11 Data cases pass after the original failures are reproduced.

The official installer metadata is added without changing either approved compiler hash/file-version
pair, pinned MSVC/SDK, signature validation or immutable package contents.17 local toolchain cases
pass on the existing 18.10.12201.205 installation and compiler 19.51.36257.0; other installer versions
are metadata-only checks. Hosted bd6e9484 CI and native Windows pass; its Android package refusal is preserved below, followed by final hosted recovery.
Application sources, runtime behavior and the complete D-432 APK remain unchanged. Successful app,
Android, backend, native financial and device checks are not replayed locally. D-386 and external
gates remain unchanged.

[Qualification and remaining hosted gate](quality/ci-common-recovery.md).


### D-433 hosted follow-up - Original Linux host snapshot

Commit bd6e9484 passes the complete CI workflow, CodeQL and the native Windows reproducibility/package/
persistence job. The Android native job refuses a changed ca-certificates package before compilation.
The Linux recipe restores the original signed Ubuntu snapshot with an isolated exact-release
certificate bootstrap, strict complete-index retrieval and unchanged final tool/NDK/compiler locks.
A fresh local image passes the original host verifier. Final hosted clean candidates and stock-suite
recovery pass on 05dbecf0; no native binary or accepted package is replaced. [Evidence](quality/ci-common-recovery.md).


### D-433 final hosted recovery

Commit `05dbecf089dabc5f433b12dbede82f4e97b10d2b` passes all three workflows:
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906007),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906028) and
[Native cipher candidates](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906072).
The actual Windows job passes exact compiler qualification, two clean candidates, immutable package,
drift refusal and protected persistence/workloads. Android passes host restoration, two clean
candidates, malformed-slice refusals and the official stock suite with reviewed static crypto.
The single scheduled check is complete and deleted; no successful job is rerun manually. The earlier
bd6e9484 Android package refusal remains historical failure evidence. This establishes hosted CI
recovery for that exact commit, not app/device/provider/store/release acceptance.


## D-434 - Original account monetary contexts

Opening balance, optional installment and observed balance retain independent original numeric
culture/currency/display-unit contexts. Display changes preserve raw input, original captions and
format examples; currency/unit changes require complete clearing/reentry. Preserve unknown principal,
historical loan/lent signs and decimal-rate meaning. Original file/presentation/edit revision guards
survive writer waits and reconciliation consent. Reject stored-currency changes before comparison,
including the match path. Postcommit default selection remains bound to the original file and native
owner; a failed follow-up never rearms the accepted account Save.

23 new distinct actual-source cases and 49 prior affected cases pass selectively. Original running
matrices complete 110 Windows/38 independent Release/AOT Android observations; two additional default
scenes and four read-only currency views per platform pass: totals116 Windows/44 Android. Final
strict builds and complete signed offline APK qualification are recorded in the linked evidence. No successful financial action or unrelated full suite is replayed.
See the account-money evidence document for exact identities, failures and limits. D-386 priorities
and physical-device/iOS/provider/store/release gates remain open. Holding monetary inputs follow as
the next separate development batch.

[Account monetary evidence](quality/account-money-context.md).


## D-435 - Original holding monetary contexts

Payment, fee, price-currency amount and opening/gift basis retain independent original currency,
display unit and numeric culture. Currency/unit changes require complete clearing/reentry. Quantity
keeps its original numeric culture because per-unit money depends on it; examples remain numeric.
Hidden fields do not reinterpret another event kind. Native account/unit/mode/kind choice and raw
edit revisions remain bound under original writers, including change-and-return. Manual-price
Refresh retains original raw meaning and known acceptance; old confirmations cannot rearm it.
Only a blank optional second amount requests unknown value; malformed nonblank input is refused.

24 distinct new source cases and 46 prior affected cases pass selectively; eleven original failures
are reproduced. Strict Windows, Android Debug and independent Release/AOT builds and the complete
signed offline APK pass without warnings/errors. 147 distinct Windows and 51 independent Android
observations complete across retained original scenes and final continuations, without accepted
purchase/opening replay. All 50 stored table sets match the original final checkpoint; actual Manual
and automatic Purchase valuations are verified separately. Main upgrades without reset with its
original account/Home0.00EUR and unchanged OS settings. No full-suite baseline is invented. D-386 stability/accessibility order and physical-device/iOS/provider/store/
publication gates remain unchanged.

[Original holding evidence](quality/holding-money-context.md).

## D-436 - Original secondary monetary contexts

Goal contributions/assumed prices, essential estimates, forecast prompts, occurrence overrides/payments,
settlement bills and shared cost/repayment/allocation inputs retain their original numeric culture,
currency and display unit. A later currency/unit change requires complete clearing and reentry;
partial editing cannot silently renew interpretation. Blank optional assumptions remain different
from malformed nonblank input. Estimates and earmarks do not become ledger movements.

Original file, stored facts, raw input revision and native owner are rechecked through asynchronous
consent and the actual atomic writer, including a change and return to the original display value.
Known acceptance retires the submitted input before notification/readback; a follow-up failure never
rearms the financial Save. Display-unit preferences publish globally only after the selective durable
Settings write has committed for the original owner. Long unit actions use the existing wrapping
button. Unit-refusal feedback is republished beside both original shared main amounts and shares.

Selective actual-source evidence is101 distinct new cases and149 affected prior cases, with17 original
source defects reproduced. All367 applicable Windows observations and127 independent Release/AOT
Android observations complete without accepted-action replay. The extra36 superseded household
feedback frames remain historical evidence. Android household review uses an actual new signed owner
admission and protected native source: old recovery/ownership/device revocations remain effective.
Its two contacts and one private KWD1001 cost are explicit independent setup, not cash ledger entries
or UI saves; the18 presentation states preserve all50 table sets and record zero accepted commands.

Strict Windows/Android Debug, independent Release/AOT and complete signed offline APK builds are
warning/error free. No schema/entity model, permission, external provider, commercial activation or
shipping SDK is added. No unrelated full-suite baseline or unit-test count is inferred from native
observations. D-386 dedicated stability/accessibility order and physical-device/iOS/hardware-key/
power-loss/provider/store/publication gates remain unchanged.

[Secondary monetary evidence](quality/secondary-money-context.md).

## D-437 - Original physical and manual-rate numeric meaning

Free holding-type purity and per-item gram weight retain independent original numeric cultures
through display changes and partial typing. Complete clearing renews only that field. Original
format examples and all field-local validation messages remain visible; original stored type and
raw/choice revisions are rechecked under the actual writer.

Manual exchange rates retain the original decimal parser, pair, file and immutable stored-rate
review through loading, Save and delete consent. A later source/pair requires explicit reentry.
Plain ratios never use display-unit factors or change recorded money. Known typed acceptance
consumes the submitted draft before fallible notification/readback; Refresh reads only. Small
positive rates keep complete decimal precision in the list and deletion consent, with a separate
estimate-badge row. Error categories translate without repeating the original command.

Selective evidence is 42 distinct new actual-source cases and 19 affected prior cases; eight actual
source failures were reproduced. Twelve affected resource checks pass. Final native/package
continuation and retained failures are recorded in the linked evidence; native observations do not
increase the unit-test count. No schema/model/portable format, permission, provider or shipping SDK
changes. D-386 stability/accessibility ordering and external release gates remain unchanged.

[Original physical/rate evidence](quality/physical-numeric-context.md).

Final scoped evidence is 203 applicable Windows and 71 independent Release/AOT Android observations,
including read-only recovery of 22 original outcome frames after a report-name collision. Accepted
operations are not replayed. Strict platform/independent and complete signed Offline APK builds pass;
Main upgrades without reset and its actual rates page/return preserve My account/0.00 EUR. Native
Windows 0xC0000005 after completed note captures and Android ANR/Wait remain real unresolved failures,
not stability acceptance. Full source/checkpoint/package details are in the linked evidence.

## D-438 qualification continuation

Original platform failures are classified separately; no stable runtime/GC cure is established.
The new physical/rate forms pass96 actual OS200% native observations at360/412dp, en/fa/de and both
themes, with zero accepted commands and complete original source/base/OS/Main restoration. Shipping
code/APK and the managed baseline remain D-437. QA-06 and remaining/spoken Android accessibility stay
open; successful feature/backend matrices are not repeated. See quality/retained-native-fault-classification.md.

## D-439 original monetary OS continuation complete

Account/holding/entry/goal/budget original contexts complete288 actual OS200%360/412dp observations,
2340 native readings, unchanged full original sources/base and zero accepted command replay. Exact
OS/Main17310/account/Home0.00 EUR restore. This is native geometry evidence, not new managed tests,
shipping behavior or APK. Continue only remaining changed-layout/reader/platform qualification; retain
QA-06 and external gates. See quality/original-money-os-qualification.md.

### D-440 - Native numeric input correction

The remaining native-input boundary exposes three real comma-loss defects. The shared Android adapter
now preserves ordinary numeric raw drafts with numeric keyboard hints and original password/length
behavior.20 native input observations and four adapter guards pass without accepted writes, source
changes or successful old-suite replay. Strict builds and complete Offline signed APK pass. No managed
case-count increase; QA-06, remaining accessibility/device/iOS and external gates remain open.
See quality/android-numeric-input.md.

## D-441 - Secondary monetary OS scopes complete

204 new native observations/900 readings pass on the three original completed sources without a new
accepted write, seed or successful-check replay. Exact OS/Main state restores. Application/APK and
managed baseline stay D-440. Remaining household/header/rate-outcome geometry, general stability,
spoken/device/iOS and external gates remain open. See [evidence](quality/secondary-money-os-qualification.md).

## D-442 - Home/Plans system-scale header scopes complete

36 observations/60 header states/120 native readings and24 original Add/Cancel sequences pass,
with unchanged complete source/base rows and exact OS/Main restoration. Shipping code/APK and
managed count stay D-440. Remaining rate-outcome/household geometry, general stability, audible
reader/device/iOS and external gates stand. See [evidence](quality/header-os-qualification.md).

A further36 read-only native-coordinate observations verify the action is physically right of the
complete title (minimum gap8px), without replaying any Add/Cancel. Total D-442 evidence is72
observations/96 header states/192 native text readings; accepted writes remain zero.

## D-443 - Retained rate outcome OS scopes complete

36 original observations/144 readings pass without command invocation, seed or restoration of the
already deleted rate. Complete source/base rows and exact OS/Main restore. Shipping application/APK
and managed count stay D-440. Continue protected-household geometry and remaining supported
stability/spoken/device/external gates. See [evidence](quality/rate-outcomes-os-qualification.md).

## D-444 - Protected household OS geometry complete

36 observations/312 readings pass with actual protected signed test authority and unchanged complete
source/base data. No enrollment, authority refresh, accepted financial write or new repayment occurs.
Exact original fixture APK, OS and Main restore; shipping app/APK and managed count stay D-440.
Audit remaining stability/performance/spoken/platform/external gates next without geometry replay.
See [evidence](quality/household-os-qualification.md).

## D-445 - Diagnose original currency query input ownership

Two original emulator scopes produce ten native observations: arrow-only list opening retains the
name's modal-root focus; text sent there appends USD to the name. Explicit actual currency focus,
filtered native USD selection and Cancel/Discard succeed with complete source/base equality and
zero accepted writes. Activity.CurrentFocus alone is insufficient for modal ownership. A separate
signed offline arm64 empty-source prerequisite builds without warnings; phone input waits for the
requested exclusive coordination reply. Historical D-422 phone cause remains unproved. Shipping
app/APK and managed baseline remain D-440; no successful financial action or geometry is replayed.

See [currency focus evidence](quality/currency-input-focus-boundary.md).

## D-446 - Move original Home goal progress off the window thread

The unchanged pure goal evaluator now runs on a worker; original metadata/formatting and covered,
current-owner publication remain on the caller. Five new and three affected prior cases pass
selectively, including actual task/thread identity and complete original goal output/source equality.
Three native Home contexts on two retained active goals preserve source/base rows and window-thread
publication; no accepted seed or payment is replayed. Strict Windows/Android/independent builds and
complete signed Offline Main upgrade pass without warnings. Original Main Home139 nodes and OS
remain; no general ANR/crash/two-second/low-memory or phone/iOS/release acceptance is inferred.

See [Home goal worker evidence](quality/home-goal-worker.md).

## D-447 - Publish the complete original goal list under a local cover

The goal list now reads one captured original file/transaction, keeps its existing financial
calculation on a worker and publishes active/history/funding rows under one translated loading/error
cover. Exact row/command/page lifetime guards refuse stale input; explicit retry and detail return
preserve the original source. No financial algorithm, schema, permission or runtime policy changes.
Fourteen new actual-source cases, three affected prior cases and23 actual resource checks pass
selectively. Eighteen Windows contexts produce84 native observations, six Android contexts produce
36 observations (34new/2retained); original source/base rows remain identical with zero new writes.
Complete signed Offline Main upgrade and actual empty-list Add/Cancel pass without reset; Home139
nodes match the old upgrade except focus, and final return differs only in one drawing-order value.
Strict builds and test-output cleanup pass without warnings. Failed native target/capture-path
observers are preserved and corrected against original receipts without repeating completed scenes.
No general ANR/crash, two-second, low-memory, spoken, physical-phone, iOS or release acceptance is inferred.

See [original goal-list evidence](quality/goal-list-publication.md).

## D-448 - Keep complete original goal details and actions in their page lifetime

Original goal details now use one complete captured file/transaction and worker financial
calculation under a local loading/error/retry cover. Page/identity revisions retire old commands
and pending reads while retaining original dirty money inputs. State/delete writers check the
original action inside their transaction before changes and commit, and preserve known accepted
results across observer failure. Three original regressions reproduce identity, pending-delete and
file-adoption defects;23 new focused cases and58 affected prior goal cases pass in selective runs,
plus19 original monetary-context and23 resource cases. Native Windows146 and Android50 observations
(31new/19retained Android) preserve retained fixture rows with no new fixture writes or replayed fixture
actions. Strict builds, signed Offline Main upgrade/Home139-node/OS checks and test cleanup pass.
Observer/compile failures are retained and repaired without replaying completed native actions.
General stability/performance, spoken/device/iOS, all protected-Main goal variants, native
destructive confirmations and genuine external gates are not accepted by this finite batch.

See [original goal-detail evidence](quality/goal-detail-publication.md).

Final source regressions also cover failed/busy/cancelled Edit validation and retained-stack theme
refresh. Only2 extra native observations per platform qualify the new Edit refusal/retry path;
completed cohorts remain. One new production-app test profile/account-opening/goal Save and one
cold read qualify positive protected-Main account-balance details100/150/50EUR and Edit/Cancel,
with All-history empty. Only that new profile is removed via full two-question UI cleanup. Main's
profile-list/Home139 nodes and0.00EUR return unchanged; English/Dark/Simple and OS are read back.
These are controlled new metadata saves, not repeated fixture payments or full protected-goal,
cryptographic, stability, spoken, physical/iOS or release acceptance.

### D-449 - Original goal-page OS text and native toolbar height

The original Persian title needs58px but received48px at actual Android OS200% text. The owned
native toolbar grows for its measured final line without reducing text size or replacing navigation.
Observers retire with the toolbar and growth requests are deduplicated within that lifetime.
Forty-eight immutable contexts contain216 native observations across360/412dp, en/fa/de and both
themes; completed contexts and native Retry are retained rather than replayed. Original fixture/base
rows remain unchanged with zero financial writes. Two unresponsive candidate sequences and a lost
log-chunk report are preserved; compact read-only durable receipt verification completes the report
without replay. Strict Windows/Android builds, complete signed Offline APK384 AOT entries, same-data
Main upgrade, original Goals read and Home139-node return pass. Exact emulator OS settings return.
No managed test baseline increase or general ANR/crash, speed, spoken/physical/iOS, provider/store
or release acceptance is claimed. See [native goal-page evidence](quality/goal-page-os-text.md).

### D-450 - Retire native toolbar listeners on the current Android tree

Three actual native roots reproduce a cached dead observer while the current observer is alive and
different. An exact separate native listener now removes from the current live tree and detaches
its weak owner before releasing the toolbar. Six focused root observations at actual OS200%/360
and412dp retain one active listener and retire four previous owners; two original Persian title
checks retain34px text,58px complete height and66px toolbar. Independent base rows and financial
writes remain unchanged/zero. The direct-tracker candidate's real native activation crash and two
caller compile repairs are preserved. Strict Windows/Android builds and complete signed Offline
APK384 AOT entries, no-reset Main upgrade, Goals/Home139-node return in the same process and exact
OS restoration pass. The transient null tree is continued read-only without replayed input/restart.
MAUI source references are corrected to the actual unchanged10.0.110 pin. No new managed-test count,
replayed D-449216-scene scope or general GC/ANR/speed/spoken/physical/iOS/release acceptance is claimed.
See [native toolbar lifetime evidence](quality/native-toolbar-observer-lifetime.md).

### D-451 - Complete one original read-only navigation pulse sequence

After D-450, one Release/AOT process completes18 original detail/Back visits with en/fa/de and both
themes, one current native root/subscription per cycle, no financial commands and exact source/base
equality. A500ms at-most-one UI probe records maximum callback age524ms and no ages over2000ms;
read/publication/Back/Home intervals are812-1436ms, median942ms on the small original source, not
whole Home/cold/tenfold or two-second acceptance. The exact PID's event buffer retains its start
with zero recorded ANR/crash events. Strict independent Android/Windows builds and signed Offline
caller pass. Main remains D-450: same PID15971, Home139 nodes, display and all original OS values.
No old216-scene matrix, accepted financial action, full suite or unchanged shipping APK is repeated;
no managed-test baseline is added. General stability/performance/spoken/physical/iOS/release gates
remain open. See [original navigation evidence](quality/original-navigation-pulse.md).

### D-465 - Budget scope explanation and physical boundaries

Original Simple account-note defect corrected with six actual-source cases and changed-note-only
native checks. Physical account/currency/pay-cycle/display boundaries pass on one retained source;
accepted financial commands are not replayed. Full QA-01 and external release gates remain open.
See [bounded qualification](quality/budget-account-scope.md).

### D-467 - Original purchase feedback

The actual physical foreign-purchase path now shows a valid ISO-specific error example. Seven
new source cases and bounded native evidence pass; full device/platform/release gates stay open.
See [bounded original purchase qualification](quality/physical-original-purchase-amounts.md).

### D-468 - Native UI servicing

Advance existing MAUI modules to10.0.120 after focused native compatibility checks. Correct the
observed report-count driver defect without replaying its successful run. No new managed cases
or feature/schema change. General stability and external gates remain open.
See [bounded qualification](quality/maui-servicing-qualification.md).

### D-471 - Household caption maintenance

Correct two malformed separators with focused original native rendering and no financial writes.
No managed-test count increase, schema change or reopening of accepted household commands.
Mac is unavailable per owner22 October2026; continue independent Android qualification.
See [scope, retained failures and release limits](quality/household-caption-separators.md).

### D-472 - Physical reconciliation boundary

The original Samsung AT-11 adjustment/reason/cold-read path passes on unchanged D-471, with Main
and all original public profile/history/Home/OS restoration. No new managed tests or rebuild.
[Bounded evidence](quality/physical-account-reconciliation.md) leaves broader release gates open.

### D-473 - Physical saved receipt boundary

Original Samsung Save/attachment/cold/stored-reread/Cancel passes with one accepted expense and
full public Main/profile/OS restoration. No new app code, managed cases or build replay.
See [bounded physical evidence](quality/physical-receipt-save-reread.md); QA-05 remains open.

### D-474 - Physical no-window reminder action

Original Samsung cold native snooze and separate persisted metadata read pass in the existing
independent current-source caller. Main and original data/settings/APKs are unchanged; no old
suite replay. [Bounded evidence](quality/physical-cold-reminder-action.md) leaves general stability,
notification scheduling/restrictions and external platform/release gates open.

### D-475 - Physical scheduled alarm and original due route

Original future AlarmManager delivery, cold ScheduledAlarmReceiver and actual notification tap
pass on the charging/awake Samsung, with source/zero-ledger/Main preservation. Only the independent
caller/status mode changes. [Bounded evidence](quality/physical-scheduled-reminder.md) leaves
precise timing,Doze,reboot,lock/general stability and external acceptance open.

### D-487 - Original saved forecast difference categories

Correct the reproduced cash-sign classification across all eight original ledger kinds. Refunds
reduce unplanned spending, income reversals reduce unplanned income, and transfers/capital trades/
corrections use a separate neutral account-movement row. The residual caption now says other
differences because earlier data edits as well as changed plans can contribute. Original scope,
saved paths, actual daily balances, recorded-later boundaries and source rows remain unchanged.
Fourteen new distinct Core cases and two affected regressions pass;23 resource cases pass,39
selected successes overall. Eighteen Windows contexts yield36 inspected images; six x64/412dp
and six physical ARM64/384dp Release/AOT contexts qualify156 original captions and reversal color
with full typed fixture/base equality. Historical timestamps are seeded once per independent
platform; this is not real elapsed-day comparison or financial form/Save acceptance.
Strict Windows and complete signed Offline384-AOT APK builds pass. Samsung no-reset upgrade
8764-to13827 retains Home979/all141 public tuples/OS; emulator28047/all139 tuples stays unchanged.
Restore both exact independent APKs/stopped states; remove five hash-matched owned XMLs. Prior
e310eadb has four successful workflows and its single-use schedule is consumed/deleted. Final
new-head CI, real elapsed-day/remaining physical variants, security/stability/accessibility/iOS/
provider/Store and release gates remain open. No unrelated successful cohort is replayed.

Evidence: [original saved comparison qualification](quality/snapshot-difference-kinds.md).

### D-488 - Physical original goal-reminder queue lifecycle

Qualify eleven original ReminderService/native scheduler states on one fresh independent Samsung
installation: default off, enable, pause/resume, complete/archive, derived reached/unreached,
detail consent, disable and generic restoration. Pending counts are0/1/0/1/0/0/0/1/1/0/1; every
queued request keeps the original scoped goal/date/stable id and24October2026 local09:00. Account
opening100EUR and contribution25/Once date remain unchanged, with zero entries/allocations/plans.
The reached case explicitly changes only the fictitious target150-to100 and restores150; no
money moves. Actual native AlarmManager/PendingIntent uses ScheduledAlarmReceiver with the
existing inexact one-hour window, not an immediate display. Eleven observations are not new
managed tests or financial-form/timed-delivery/lock acceptance. Strict complete signed Offline
ARM64 caller192 AOT passes; Main identity is refused. Retain the original live15501 after the
first observation timeout; no successful state/seed repeats. Remove only this new fixture and
its pending alarm/notification; Main13827/Home979/all141 public tuples/APK/OS match. Five owned
XMLs are hash-checked/removed. Main shipping code/APK remain D-487. Real09:00 delivery, doze/reboot,
notification tap through lock, remaining calendar/device/security/stability/accessibility/iOS/
provider/Store and release gates remain open.

Evidence: [physical goal queue qualification](quality/physical-goal-reminder-queue.md).

### D-489 - Physical original period-review reminder queues

Qualify22 original protected ReminderService/native queue states on Samsung in one retained20233:
empty/off and empty/enabled sources, source/off, six states in each Gregorian/Persian/Hijri
calendar, and final restored/off. Financial month starts24/2/13 schedule24October2026 local09:00;
the next two native requests follow their actual calendar, retaining09:00 across the stored DST
offset change. Partial review keeps three pending; finishing only the due period removes that
one and retains two future requests. Details, disable and missed-time cases pass. Compare every
settings field before/after publication; no review step is completed by a reminder. Original
account100EUR stays equal, with zero entries/goals/allocations/plans. These22 observations are
not new managed cases, actual timed delivery or financial-form acceptance. Strict signed Offline
ARM64 caller192 AOT/unchanged five runtime engines passes. Preserve initial caller compile error,
two live observation deadlines and premature cleanup refusal; continue only the same live PID.
The temporary owner-authorized cross-project phone handoff is respected; after a short exclusive
turn, remove only the fresh fixture and five hash-matched XMLs, retain Main13827/Home979/all141
public tuples/APK/OS, and explicitly release the phone for the other active project. Main shipping
code/APK remain D-487. Real09:00 delivery, lock/tap/profile-switch/restore/Doze/reboot, broader
security/stability/accessibility/iOS/provider/Store and release gates remain open.

Evidence: [physical period queue qualification](quality/physical-period-reminder-queue.md).

### D-490 - Preserve original imported-group dependencies on Undo

Reproduce unsafe Undo after a later fee is added to an imported transfer. Capture external ledger
and holding group context under the original import writer in internal ImportLinks version2;
refuse later changes before mutation, retaining valid Undo for unchanged pre-existing groups.
Ungrouped journals remain version1; legacy closed groups are supported and unverifiable external
groups require review without invented backfill. No EF model/schema/public CSV/permission change.
Ten new and five selected prior managed cases pass; strict Windows/complete signed Offline384-AOT
APK builds have zero warnings/errors. Six original native Undo cases pass on isolated fictional
Samsung sources with full typed-row/base equality. Preserve the first fixed-dock ScrollToAsync
observer failure and its accepted first outcome; resume only five unexecuted commands and read
that first result without replay. Consent is fictional, not native dialog/picker acceptance.
Restore the old independent APK/stopped state, then no-reset Main upgrade retains979/all141 public
Home tuples/OS in PID31885; remove three owned hash-matched XMLs. Prior ecde9366 CI/CodeQL succeed
once and its monitor is consumed/deleted. New-head CI and broader relationship/device/security/
stability/accessibility/iOS/provider/Store/release gates remain open. No unchanged successful
financial cohort or full suite is repeated.

Evidence: [original grouped Undo](quality/import-group-undo-dependencies.md).

### D-491 - Physical original goal calendars and unavailable sources

Twelve original protected/native queue observations qualify saved Gregorian/Persian/Hijri monthly
rules independently of display calendar, two-goal grouped routes, account/type archive/restore,
missed Once and final disabled state. First24October09:00 and second24November Gregorian /
23November Persian/Hijri stay local09:00 across DST offsets. Full original publication packets
remain equal; opening100/targets150EUR and5units stay, with zero ledger/allocations/holding events.
Preserve two actual coalesced-wait observer cancellations and two accepted states; final caller
follows the replaced handle after yielding and continues only ten missing stages in4354.
Strict complete signed Offline ARM64/192-AOT caller passes. Remove only the new fixture with
pending0 and2 Main/3 scoped owned XMLs; original Main31885/Home979/all141 public tuples/APK/OS
remain. Main code/APK stay D-490, no new managed count or successful unrelated suite/build replay.
Real timed/lock/profile/Doze/reboot, security/stability/accessibility/iOS/provider/Store and release
gates remain open. This is native queue/API evidence, not financial-form or whole-device acceptance.

Evidence: [physical calendar/source queues](quality/physical-goal-calendar-sources.md).

### D-492 - Physical original goal and current-review notification taps

Two actual native notification content taps start8272/9387 after confirmed cached deaths of7065/
8272, without provider-created cold processes. Original protected startup/router open the exact
goal100/150/50 and current September review0of5; complete fictional source remains equal and no
financial/contribution/review-step command occurs. Explicit immediate copies retain original
future request ids/payloads; they are not actual09:00 dispatch or credential-configured acceptance.
One strict complete signed Offline ARM64/192-AOT caller passes; Main/App/APK remain D-490.
Preserve early cache/shade/host guards and the cleanup observer's historical-alarm misclassification;
continue accepted state without repeating seed/Show/tap/uninstall. Active pending/blocked alarms
and posted notifications are absent; historical records remain untouched. Remove only the fresh
fixture and2 Main/10 scoped owned XMLs. Original Main31885/Home979/all141 public tuples/APK/OS
stay equal. Prior f21a5d61 CI/CodeQL succeed once and its monitor is consumed/deleted. No new
managed count or unchanged successful suite/build replay. Timed/lock/profile/grouped/Doze/reboot,
security/stability/accessibility/iOS/provider/Store and release gates remain open.

Evidence: [original content routes](quality/physical-goal-period-notification-routes.md).

### D-493 - Physical grouped goal tap and restored-profile scope

Two native content taps follow confirmed cached deaths12676-to14154-to15038. Original SDK events
retain the same two goal ids/dates and scopeA. The first opens both original GoalsPage rows.
A single original portable export/profile creation/switch/protected restore clones all financial
identities intoB with another file scope; the second actual old-A tap stays onB Home100 despite
both goals existing. Nine original financial arrays compare exactly. Original profile return/
inactive-B retirement restores scopeA/one-profile catalogue. Service restore/cleanup is distinct
from native dialogs/password/credential acceptance. An independent early tap subscription adds no
Application-time database/key work; production routing/security sources remain unchanged.
Final strict signed Offline ARM64/192-AOT caller passes after a preserved initial report-type
compile error. Remove only the fresh fixture and2 Main/8 scoped owned XMLs; pending/posted native
work is absent, historical records untouched, permanent Main31885/Home979/all141 public tuples/
APK/OS equal. Prior7abae129 CI/CodeQL succeed once and monitor is consumed/deleted. Main/APK stay
D-490; no new managed count or successful unrelated build/suite replay. Actual09:00/credentials/
remaining profile-race/security/stability/accessibility/iOS/provider/Store/release gates stay open.

Evidence: [original restored-file scope](quality/physical-goal-profile-notification-scope.md).

### D-495 - Retain pending destinations after a renewed app lock

Two new original-source cases reproduce a queued destination running beneath a renewed cover,
with an available or missing window. One drain owner now rechecks access before each unstarted
action and keeps later input in arrival order. Three new cases and41 existing lock/router cases
pass; the full unrelated suite is not repeated. Two actual Release/AOT Android native-cover cases
with three real button clicks preserve order1/2/3 and complete source/base equality. Authentication
responses are explicit fictional ports, not real credential acceptance. Retire only the new run;
restore the old independent APK/process absence and emulator Main28047/all139 public tuples/OS.
Strict Windows and complete signed Offline384-AOT APK builds pass without warnings/errors. Samsung
upgrades without reset31885-to25043, retaining Home979/all141 public tuples and exact OS; four owned
XMLs across both devices are hash-matched/removed. No financial Save, PIN/key/account/SDK/schema or
permission change. Preserve the initial caller namespace compile refusal and actual timed-fixture
Activity resumption during Main replacement: its interrupted initial D-494 lineage is not accepted
as uninterrupted cold evidence. Same source/run remains equal; current native requests and future
actual delivery are tracked separately. D-493 CI/CodeQL succeed once; its monitor is consumed/deleted.
D-494 timed delivery, real credentials, general security/stability/accessibility/iOS/provider/Store
and release gates remain open. New-head CI follows this completed correction.

Evidence: [original relocked destinations](quality/relocked-pending-destinations.md).

### D-497 - Refresh retained original forecast and saved comparison tabs

Physical D-496 preserves stale0EUR after its single accepted later expense, versus correct-10
on original Back/reopen. AppShell now includes both retained ForecastPage and SnapshotPage in
covered tab activation; appearance/navigation share PendingPublication. Four actual native tab
clicks qualify the same saved page publishing-10 and same forecast publishing97 while retaining
its what-if plan exclusion. The immutable original snapshot and complete original base match;
only the new independent protected profile is retired. Fictitious service writes are not native
financial-form or D-496 acceptance. No new managed cases or unrelated successful-suite replay.
Preserve the initial caller compile errors and live launch-observation guard; both cases finish
in11339. Restore exact old caller APK/process absence and emulator Main28047/all139 public tuples/
OS; two owned XMLs are hash-matched/removed. Strict Windows/Offline384-AOT shipping builds pass.
Samsung remains D-495 to preserve the pending timed cohort; no-reset phone adoption remains a
named integration action. D-494/D-496, phone/platform/security/stability/accessibility/provider/
Store and release gates stay open. Prior f6ad8543 CI/CodeQL succeed once; its monitor is consumed/
deleted. Exact new-head CI follows push.

Evidence: [original retained-tab qualification](quality/forecast-retained-tab-return.md).

### D-499 - Reversible original CSV text

Own CSV contract3 preserves apostrophes/whitespace and named version identity; legacy ambiguous
account aliases refuse rather than selecting another account. Generic human text remains literal.
Forty new/20 existing related cases and bounded original Windows/Samsung native imports pass,
with accepted actions retained through observation-only continuation. Strict Windows/signed
Offline384-AOT shipping builds pass. Main adoption waits for D-494; no schema/permission change
or broader stability/iOS/provider/release acceptance. See [scope and evidence](quality/csv-literal-identity.md).

### D-500 - Shared original Home balance index

One worker-local index serves account rows and both balance totals without changing the financial
formula or source/authority validation. Twelve new and29 related managed cases, retained Windows
and same-day Samsung native sources pass with complete data preservation and zero financial writes.
Isolated allocation falls about two thirds; whole-Home two-second/stability targets remain open.
Main adoption waits for D-494; remaining future-day/external gates are preserved.
See [bounded evidence](quality/home-shared-balance-index.md).

### D-501 - Fixed public receipt-corpus observation

Ten fixed CC-BY-4.0 public CORD v2 images are read once on Samsung through original offline
reader/parser: five correct Found, three Review, two NotFound including the no-total negative.
No financial save, new managed count, shipping source/SDK/permission or broader quality acceptance.
QA-05 remains partial for six-language/camera/scanned/Persian/merchant/currency/device coverage.
Original caller/Main/OS restored; D-494/D-496 preserved. [Evidence](quality/public-receipt-corpus.md).

### D-519 - Independent qualification progression

SUPA Windows scale2 continuation and basic signed-app runtime smoke on actual API24/4096 and
API35/16384 x86_64 complete with zero financial writes. Keep complete platform/security/ARM64,
Android OS scaling/spoken access and stability/performance, iOS and external gates open; the
canonical commercial backlog and D-519 evidence define the bounded result.

### D-520 - New native key runtime cohort complete

Original key-protector create/cold/binding/tamper/authorized retirement completes on actual
API24/4096 and API35/16384 x86_64 without financial writes or Main access. One retained fixture
per new UID and six original phase receipts preserve all previously accepted phone work. Full
SEC/platform/authentication/iOS and release acceptance remains in the canonical commercial backlog.

### D-521 - Actual storage runtime cohort

Original schema51, encrypted snapshots/typed rows, controlled candidate/selection process faults,
missing-key no-fallback and protected recovery qualify on actual x86_64 API24/4096 and API35/16384.
Retain32final-cohort records, old failed polling windows and existing source/financial checkpoints;
no managed test increase. Canonical backlog still owns full multi-profile/hardware/OEM/ARM64/
authentication/iOS and external release gates.


### D-528 - Transactions Add beside the title

Owner-requested Transactions + now matches Home/Plans: original48px accessible command physically
right of a growing title, original pushed Back, no reserved bottom Add band/overlay. Preserve full
filters/results, empty invitations and separate contextual bulk/Undo rows; filter sizing reserves
only the actual header and contextual actions. Eighteen Windows contexts/54 images qualify populated,
filtered-empty and original selection states; six independent Samsung Release/AOT contexts qualify
native Add/Cancel/bulk with unchanged original stored inputs. Retain controller/count and observation
refusals without replaying completed work. Strict builds, complete signed Offline384AOT/408ELF16KiB
static checks and no-reset shipping upgrade24834->32206 pass. One original Main Add/Cancel, Home979,
all141 public tuples/three profiles/OS/timeout300000 remain; no Main financial writes or new managed
cases. Retire the new helper and15 hash-matched XMLs. Full stability/accessibility/iOS/external
release gates remain separate. See [complete runtime and package evidence](quality/transactions-title-add.md).


### D-533 - Current physical Home with original timing-byte evidence

Six actual Samsung current-Home contexts/twelve observations reuse the original encrypted10,000/
100,000-row sources, with zero seed/financial commands or new keys. The original additive callback
and complete schema/header/typed-row/native controls pass. Retain a create-once synchronized
pre-native source/code/day fingerprint and zero-case progress; ten focused actual-policy contracts
pass and the physical timing-interval byte/source guards pass. Preserve the undeclared Activity
controller refusal; start the resolved original launcher once and observe the same32040 after the
first report window expires. Separate clone and failed-adoption provenance. Tenfold4777.22-4992.71ms
publications keep the two-second/general stability gates open; original D-532 historical-byte refusal
and native death remain. Restore original caller UID10381/APK/stopped/grants/no-pending, Main24725/
Home979/all141 tuples and OS; remove two owned hash-matched XMLs and explicitly return the phone.
Three strict builds/standard clean pass;187287221bytes additional host deletion is refused before
execution. No unchanged Windows/full-suite or accepted financial replay; product test count and
shipping D-528 stay unchanged. Runtime/compiler/task10.0.13 identities match; no unsupported mix
or general crash fix is established. Broader performance/accessibility/platform/release gates remain.
[Original runtime evidence and remaining limits](quality/physical-current-home-stages.md).


### D-534 - Bounded current Home CPU sampling

Preserved original API36/x64 sources produce a code-address-only CPU sample with2733 samples,
zero lost and no raw stack/register payload. The actual SDK classifies69 empty AOT objects;
all123 method-bearing objects match the diagnostic signed artifact. Retain initial4010/SGen
SIGSEGV and Android EXIT_SELF/status255 separately; same-source4313 continuation keeps the three
accepted reference contexts and executes only missing tenfold contexts. Complete source/native
and premeasurement-byte controls pass without seed/financial replay. Flat CPU is Mono40.98% and
SQLCipher17.38%, but54.26% broken callchains preclude reliable inclusive/root attribution, KDF-only
claims or a whole-Home/cold/phone speed conclusion. No product/runtime change or new product tests.
Original caller restoration hits storage exhaustion; documented package-specific maintenance and
Android KEEP_DATA code removal/reinstallation restore exact original digest/UID10221/stopped/grants.
Main2878/Home0/all120 public tuples and OS match; three owned hash-matched remote files are removed
and only the dedicated AVD is stopped. Post-restoration private DB/key reopen is not claimed.
General stability, two-second, accessibility/platform/external gates and the broad goal stay open.
[Actual sample, lineage, restoration and limits](quality/current-home-cpu-sampling.md).


### D-535 - Protected-source reopen after data-preserving maintenance

One focused API36/x64 read-only audit verifies existing Android keys/generation bindings after
D-534's KEEP_DATA code maintenance. Reuse the exact signed D-532 audit caller and original
lineage; process3600 reopens four original/adopted sources, matching complete schema/headers/typed
rows and all source files. Zero financial/seed/key creation, native UI or timing replay; historical
D-532 timing-byte refusal remains false/unaccepted. Ordinary signed-r installations return the
original caller UID10221/digest/stopped/grants, Main2845/Home0/all120 public states and exact OS.
Two owned XMLs are hash-cleaned, one redundant57818872-byte host copy removed after equality and
only the dedicated AVD stopped. No rebuild/unchanged suite or phone query; shipping D-528 unchanged.
This bounds independent source retention, not Main-key attestation or general platform/stability/
performance/accessibility/release acceptance. D-533 CI/CodeQL succeed and its follow-up is consumed.
[Actual read-only outcome and scope](quality/post-maintenance-source-retention.md).


### D-536 - Withdraw reduced-allocation ledger ordering

A stable-merge sort reduces host allocation but slows every large shape; a cached UTC/day/ordinal
candidate reduces100000-row host sort allocation4000504 to1600168 bytes and passes thirty original
object-order shapes plus seven affected SQLite regressions. Actual original3844/candidate4548
API36/x64 cohorts retain all schema/typed-row/native/financial/premeasurement-byte controls with
zero seed/key/financial replay. Candidate report observation continues the same live4548 after
its first window expires. Large-pair ratios1.0035/1.0123/0.9659 show no stable material Home benefit;
withdraw all candidate product code and keep shipping D-528 unchanged, without phone repetition.
Preserve an initially missing installed APK after boot; signed restore, sync/orderly shutdown and
one new cold boot then retain exact original UID10221/APK/stopped/grants, Main2880/Home0/all120 states
and OS. Root cause/power-loss safety remain unproved. Retire only two completed inactive emulator
callers' code after exact APK backup; retain their data/keys/UIDs. Fresh returned phone Main27069/
Home979/all141 states remains untouched except read-only preflight/owned XML cleanup. Five emulator
XMLs and one phone XML are hash-cleaned; standard cleans and111079544-byte duplicate removal pass.
Automatic removal of251505548 bytes of three new output trees is refused before execution; no
alternate deletion. Latest D-535 CI/CodeQL succeed. Product test count and wider acceptance stay open.
[Actual comparison, storage recovery and limits](quality/withdrawn-ledger-ordering.md).

### D-537 - Physical Home CPU windows on retained sources

Add an explicit one-publication tenfold/en code-sampling scope with native-monotonic boundaries,
durable scope identity and a released strong full-ledger oracle reference. Ten exact-source scope
contracts, ten unsafe-header refusals, one oversized-input refusal and retained2733/1324/0 native
filter controls pass. Three necessary strict independent ARM64 publishes pass; only the final
signed offline123-packaged-AOT/135ELF artifact is installed. Preserve first recorder exit134 before
any samples and its completed4296.4346ms publication. One necessary CPU-only retry on the same APK
records8634 samples/zero lost; exact native/inspector filters retain2380 inside4140.808074ms,
partitioned2437.831921/1490.250461/212.725692ms. All original/adopted bytes, complete schema/typed
rows and native financial/recent packets match without seed/key/financial replay. Official Mono
BuildId/debuglink-CRC and packaged AOT symbols resolve actual function ranges; balance/index,
SQLCipher and concurrent GC work coexist, unknown samples and an uncollected oracle limit causal
claims. Restore original caller UID10381/APK/stopped/grants and Main27069/Home979/all141 states/OS,
remove eight owned hash-matched remote files and return the phone. Product source/APK remainD-528;
no unchanged Windows/full suite repeats or new product test count. D-536 CI/CodeQL succeed and its
one-shot is consumed/deleted. Two-second/general stability/accessibility/platform/release gates and
the broader goal remain open. [Complete code-window evidence and limits](quality/physical-home-cpu-windows.md).

### D-538 - Withdraw direct endpoint-index construction

A direct original-reference loop lowers host constructor cost but offers no material allocation
relief; its protected read-only-view variant adds8.46% allocation in the many-account shape.
Twenty-six focused actual-Core cases, including two new collection-mutation/captured-slice
contracts, and strict managed/independent ARM64 builds pass. Preserve one unexecuted fixture
compile refusal, the observer240-second parameter refusal and its same10999 continuation,
plus candidate12246's continued observation. Original/candidate single tenfold/en publications
4131.4756/4010.2595ms pass all encrypted-source/adopted-byte/schema/typed-row/native/financial
guards without seed/key/money replay. The2.933966% reduction fails the previously recorded5%
each-pair criterion; omit the unnecessary second pair, restore exact original product source
and pass only the two new tests on that rebuilt original. Restore caller UID10381/APK/stopped/
grants and Main27069/Home979/all141 public states/OS; hash-clean two XMLs and return the phone.
Three scoped cleans and177874001-byte unused-copy removal pass; automatic review refuses deletion
of132837195 residual output bytes before execution, with no alternate attempt. D-537 CI/CodeQL
succeed and its one-shot is consumed/deleted. Product/APK remainD-528; coverage adds two cases,
not a complete-suite pass. Two-second/general stability/accessibility/platform/release gates
and the broader goal stay open. [Complete evidence and limits](quality/withdrawn-account-index-loop.md).

### D-539 - Isolate complete Home controls in another process

The released100000-row oracle remains alive in D-537/D-538; add explicit preparation/measurement
scopes without changing product source, financial formulas or GC policy. Bind compact original
balance/recent/full financial-presentation controls to source-byte-fingerprint/payload hashes,
day, Data/Core/Caller MVIDs and preparation PID/UID. Thirty-eight exact-source identity/scope
contracts and strict initial/full AOT builds pass without warnings/errors; no product test count
or unchanged Windows/full-suite repeat. Complete signed offline/non-debuggable/non-profileable
ARM64 APK has192 packaged AOT/204ELF/804LOAD static16KiB checks and v2/v3/ZIP verification.
Actual15744/e9aac472 prepares original controls with zero target native publications; preserve
its expired observation and recover the same final report. Prove15744 dead, then the same APK,
without reinstall/adoption/seed/key/money replay, measures16357/275fc72d. No full target oracle
read occurs before timing; complete original/adopted bytes/schema/typed rows and independent
financial/native packets match. Elapsed4321.904ms/read2735.4251/model1304.2752/layout281.8074 and
max dispatcher536.1012ms still fail the two-second objective; no product improvement/regression
or causal GC claim. Restore caller UID10381/APK/stopped/grants and Main27069/Home979/all141 states/
OS, hash-clean two XMLs, remove53992832 duplicate bytes, perform scoped cleans and return the phone.
Residual native cache131261554 bytes is inventoried; earlier cleanup refusals remain untouched.
D-538 CI/CodeQL succeed and its one-shot is consumed/deleted. General stability/accessibility/
platform/release gates and the broader goal remain open. [Complete evidence and scope](quality/separate-process-home-oracle.md).

### D-540 - Complete encrypted ledger transport (locally qualified and adopted)

Preserve the complete32-field history and original physical/date/creation tie order while adding
bounded512-row internal encrypted read blocks. The original source/writer/financial formulas stay
authoritative; revision/schema/count/digest/type/pipeline guards fall back to scalar reads without
getter repair. Original transaction commit validates authority before/after affected-block upkeep;
cancellation/IO cannot leave a separately accepted financial edit. No EF entity/model/key/permission
or new SDK change. Focused codec/migration/source/rollback/backup/restore/authority/observer checks
pass in selective continuations; preserve original fixture/parameter/observation failures, not a
full-suite green claim. Strict Windows and full signed offline dual-ABI384AOT builds pass.

Two opposite-order, same-APK/source/day/module Samsung pairs use independent scalar controls in
other proven-dead processes:4710.9157to3323.291ms and4665.0514to3304.8118ms (29.4555%/29.1581%).
All original financial/native/recent/presentation and complete source guards match. Four single
tenfold-English publications do not accept cold/full-device/language coverage or two-second Home.
No accepted financial seed/key/command is repeated. The two-second objective remains open.

The exact old caller APK supports50 migrations ending20261017013925; reverse only M1/M2 internal
bookkeeping on empty independent Main/UID10381 using durable original request, actual memory Down
and full source/schema/header/typed-row proof. Native18045 passes with no shipping/workload touch.
Preserve an earlier undispatched17203 entry-registration omission; correct only its Activity
allowlist and retain original performance APK/Data/Core exact-byte continuity without replay.
Initial old-APK installation observation expires at35seconds with Play Protect foreground; retain
that pending result. After the owner's personal decision, the same install completes with original
b1d5 APK/UID10381; normal21974 startup reaches original onboarding, then stops with proven death.
Main27069/Home979/all141 states/OS remain exact. A local UTF8-reader refusal precedes any shipping
install; correct only encoding and preserve the baseline. After explicit handback, one complete signed no-reset upgrade adopts e0d3 shipping bytes and
Main27069 becomes27774. Preserve the initial16-node Preparing refusal; the same process then
matches Home979/all141 public states and all three original profile actions/selection/OS.
Independent original caller APK/UID/stopped state/storage identities remain exact; remove only17
owned hash-matched phone XMLs. No financial Save or private Main key/database read occurs.
Independent new-candidate bootstrap also reaches
original onboarding on API24/x64/4096 and API35/x64/simulated16384, with no UI/financial input;
remove only new fixtures/two owned XMLs and prove both AVDs stopped with sharedADB76812 retained.
Goal/full stability/accessibility/iOS/provider/Store/release remain open. [Evidence](quality/complete-ledger-read-blocks.md).

### D-541 - Bounded Home publication connection

Same-APK/source Samsung pairs qualify one operation-local connection with fresh original final
validation at5.9096%/5.2783% improvement. Sixteen distinct selected cases and strict builds pass;
complete signed offline384AOT/static-package guards pass with exact qualified Data/Core images.
Original caller retirement/restoration is complete. Main a2d98e/PID29566 preserves Home979/all141 public states, all three original profiles and
exact OS settings after the one signed no-reset upgrade and authorized normal Resume. Original
independent caller bytes/UID/storage/grants/stopped state match; ten new owned hash-matched XMLs
are removed. The earlier foreground refusal and17997 external task removal remain recorded.
Two newly owned independent x64 compatibility fixtures qualify nonempty100000-row block reads
and actual original Home publication on API24/4096-byte pages and API35/simulated16384-byte
userspace pages over a4096-byte kernel. Each fixture is seeded once with110000 reference/tenfold
rows; scalar controls finish in a separate proven-dead PID before the one native publication.
Complete original schema/typed-row/financial/native guards pass with one context, one block read,
zero scalar reads and no post-setup financial command/key/seed replay. Both initially absent
packages are removed and only the two owned AVDs are stopped, preserving their settings.
This is not hardware16KiB, whole-platform, startup, stability or two-second acceptance.
No accepted financial work repeats. Two-second
Home and wider stability/accessibility/platform/release remain open in the canonical backlog.
[Evidence](quality/home-publication-connection.md).

### D-542 - Native Home row publication

Publish the original Home period/upcoming/recent/pinned-goal rows through the existing complete
snapshot collection, staging each original row set before one Reset. Keep all original mappings,
order, fresh theme objects, command identities, full reads, cover and final validation; chart/brush
publication and layout remain unchanged. Eight related existing cases pass after this changed
usage, not new-test/full-suite evidence. Strict Windows/full Android builds are warning-free.

On the retained x64 API36 sources,12 light and12 distinct dark native publications across
reference/tenfold and en/fa/de retain complete original schema/typed rows, financial/native
presentation and recent packets. Each of the four row sets produces one Reset and no per-row Add.
No successful seed, key or financial operation is replayed. These full-control elapsed samples
are not a causal speedup or physical two-second claim. The compatible independent caller stays
stopped on its additive schema because its ordinary Main is nonempty; empty-Main retirement is
inapplicable. Original APK backup/retained sources/Light remain, and shipping emulator Main's
120 original public states match the historical unchanged-APK baseline. Preserve its initial
15-node startup-only observation refusal.26 owned hash-matched XMLs are removed; only that AVD
is stopped.

The complete signed offline package has384AOT/408ELF/1608LOAD static16KiB/ZIP/v2/v3 guards and exact
actual packaged Data/Core LZ4 inputs matching the qualified independent APK. One Samsung no-reset
upgrade tobc1ea2 retains PID4090/Home979/all141 public states, all three profiles and original OS.
The unrelated-app foreground refusal is preserved and the owner's normal return resumes only
readback, never reinstall or money. Wider two-second/stability/accessibility/platform/provider/
Store/release gates and the broader goal remain open.
[Source and qualification](quality/home-row-snapshot-publication.md).


### D-548 - Contextual Transactions maintenance

The observed safe-area omission is fixed and36 new/retained native Undo states plus six focused
source checks pass, with no financial/Undo command or old normal-scene replay. Physical latest-
candidate adoption and publication await the pending original-caller Play Protect restore;
D-547 native admission now passes both matched pairs; broader phase and
external release gates remain open. [Exact scope](quality/transactions-context-safe-area.md).


D-549 qualifies the remaining nonempty original codec/fallback/recovery/native Home path on
API24/4096 and simulated API35/16384 actual native/managed pages. Two fresh once-only synthetic
fixtures, complete source preservation, exact shipping Data/Core and six native outcomes are
recorded separately from previous successful cohorts. Both fresh packages/data are removed and
owned AVDs stopped after exact package/settings/config checks. Hardware16KiB, whole-platform,
performance/stability/A11Y/iOS/provider/Store/release and pending phone adoption remain open.


D-550 full signed896e no-reset upgrade qualifies retained-emulator Main adoption, with original
UID/CEDE/permission flags/all120 public attributes/zero balance/OS/config and stopped compatible
caller retained. Only two owned XMLs removed/AVD stopped; no prior cohort/test/financial replay.
Physical original-caller restore/handback/adoption and broader release gates remain separate.

D-562: repair the actual CI writer defect with an additive duplicate-marker guard migration,
preserving original financial operations and exact source comparison. All83 original failures
pass locally;18 genuinely new managed cases cover conflict policies and narrow restore/workload
oracles. Strict builds and one new Samsung protected-store delete/receipt-Undo cohort pass;
complete signed8fd5 is adopted without reset in Main7028/Home979/all141 with original settings
and caller preserved. Hosted latest-head CI remains pending until its actual result; broader
stability/performance/accessibility/platform/provider/Store gates remain. [D-562 evidence](quality/ci-ledger-invalidation-and-exact-restore.md)


### D-563 - Bounded independent native fault observation

Add an opt-in independent-package observer for reviewed module-relative instruction locations,
with original signal chaining, bounded private records and mandatory independent fatal correlation.
Fifteen new actual-reader cases and fresh refusal/recovery/fatal/concurrency controls on x64 and
ARM64 pass. One changed runtime14/x64/API36/4096 retained-source cohort arms successfully and
preserves all six native contexts/twelve publications, original encrypted sources and complete
financial presentation without seed/key/financial replay. The actual named record is empty and
the process lives: no random crash correction or general stability acceptance is claimed.
The original-code handback guard refuses nonempty independent Main before schema retirement;
keep the compatible new caller stopped with original UID/storage/permissions and old APK backup.
Ordinary emulator Main3035/50f5 and phone Main7028/8fd5 are preserved. Standard owned build cleanup
and exact-head CI are separate recorded gates. Shipping code/APK, permissions, UI and GC settings
are unchanged. QA-06, phone two-second, accessibility/platform/security/iOS/provider/Store/release
remain open. [Full scope and actual failures](quality/bounded-native-fault-code-observation.md).


### D-564 - Ordinary emulator ledger-guard adoption and owned code cleanup

Accept existing signed shipping8fd5 once without reset on ordinary emulator Main3035to6123,
with original all120 public attributes/zero balance, UID/CE/DE/permissions, size/density and five
settings exact. Preserve actual storage refusals. Retire only the stopped finished Billing
recovery caller's unused code after hash/certificate-verified APK backup, retaining its UID/data/
keys/stored grants; do not touch the live historical snapshot caller. Restore compatible D563
ebd9 stopped with original UID/storage/permissions, and hash-clean two new XMLs. The overly broad
uninstalled-permission-declaration observer refusal is preserved and corrected without reinstall.
No phone, private Main database/key, financial command, old successful suite or build is repeated.
No AVD resize, data clear or global cache purge occurs. This completes the distinct ordinary
emulator changed-APK gate; QA-06/A11Y/platform/iOS/provider/Store/release remain open.
[Full boundary and failures](quality/ordinary-emulator-ledger-guard-adoption.md).


### D-565 - Physical ARM64 bounded observer cohort and original handback

Qualify the previously unperformed managed ARM64 observer boundary with one original retained-source
cohort, no new financial/preparation/key commands. Strict complete signed dadba5/192AOT/205ELF and
actual financial/runtime identity pass; all6 native contexts/12 publications and full source/native
presentation guards pass in5178. The named record is empty and that process lives: random SGen
crash/root cause remains unproved. Tenfold median3512.0725ms still exceeds the two-second target.
Only the validated empty independent Main retires M1/M2/M3; original b1d5/UID/storage/permissions
return through the same installer with the owner-authorized scoped Don't send option. Preserve
German-onboarding observer refusal and restore English once in the same7667 without Next/financial
work. Main7028/Home979/all141/both APKs/OS exactly return,5 XMLs hash-clean, and phone is handed back.
One standard clean removes11586 files/637786410 logical bytes;4 unused unsigned copies remove
117540593 bytes, used packages/proofs retained. D563 one-shot CodeQL passes/CI cancels; D564 CI and
CodeQL pass, both schedules consumed/deleted. No old managed/native/financial success is replayed.
QA-06/A11Y/security/platform/iOS/provider/Store/release remain open.
[Full bounded evidence](quality/physical-bounded-fault-observer.md).


### D-566 - Original receipt-source Help at actual OS200% and bounded startup diagnosis

Qualify original native Help/OK/Cancel in en/fa/de, Light/Dark, exact360/412/680dp on API36/x64/4096.
Preserve18 original320/412/640 scenes; continue only12 missing exact widths and skip all6 accepted412.
All18 required exact contexts pass final-line reachability, original44dp actions and Persian RTL,
with zero source/camera/file/financial commands. Correct only the observer's29px coordinate mismatch.
The original covered startup failure is actual insufficient space below the unchanged553648128-byte
protected guard; after backed-up finished-code-only KEEP_DATA retirement, one same-process Retry
passes. Optional content-free independent startup diagnostics are compiler-elided from shipping;
five new managed cases and two affected existing cases pass. Default Windows/Android strict and
complete signed independent f288bc/192AOT/204ELF pass with actual financial/native identity to8fd5.
Restore font/display/five settings, Main2902/all120/UID/storage/grants/8fd5 and stopped ebd9 exactly;
remove only the fresh fixture,7 hash-matched XMLs, shut down only the owned AVD. Standard clean
removes24947 files/2306666041 logical bytes;8 unused copies remove238366227 bytes, used evidence kept.
D565 CI/CodeQL are green and its schedule consumed/deleted. Spoken/physical A11Y, QA-06, speed,
security/platform/iOS/provider/Store/release remain open; no old financial success is replayed.
[Full boundaries and preserved failures](quality/receipt-source-os-text-and-covered-startup.md).


### D-569 - Preserve Java resources during incremental Android APK packaging

Reproduce the installed36.1.69 BuildArchive defect directly: unchanged JAR resources/services
are present on the first call and missing on the second. Refresh only the validated generated
intermediate application APK before packaging; key on AndroidApplication, since the real SDK
changes application OutputType to Library. Preserve the initial complete-app admission refusal.
Three guarded resource/changed-and-removed-input cases, nine authority cases and one redirected
path refusal pass. Strict Windows and corrected complete offline Android builds have zero
warnings/errors;384 own AOT/408 ELF, static16KiB/ZIP/v2/v3 and actual native/financial identity
checks pass. All1422 Java resource bytes match the retained healthy role, including both coroutine
service descriptors. No application unit-test count, financial action, phone/emulator operation,
visible UI or general crash correction is claimed. D568 native timing/adoption and the D567 real
reboot/idle cohort remain unfinished; Loyan retains the physical lease. Preserve the broad goal.
[Full defect, correction, refusals and acceptance boundaries](quality/android-archive-resource-retention.md).


### D-571 - Preserve the complete permitted StarMoney field at CSV syntax boundaries

Correct original preflight counting so separators and CSV quote syntax do not consume the existing
4096-character decoded-field allowance. Preserve all original size/row/column bounds, strict syntax,
source identity and explicit financial review. Five corrected boundary shapes fail before the fix;
all10 boundary cases, two original non-UI application/real-SQLite review flows and five affected
existing structure refusals pass on runtime14. These are12 new cases plus five existing cases;
incorrect initial exception-type assumptions are retained separately and are not product failures.
Strict Windows/complete offline Android builds have zero warnings/errors;384 own AOT/408 ELF,
static16KiB/ZIP/v2/v3 and1422 exact Java resources pass. No Import/Undo, financial/native replay,
phone command or D567 emulator input occurs. The combined review APK contains unfinished D568;
refresh its matched financial inputs before physical comparison after explicit Loyan handback.
Read only the historical D561 failed job: all83 cases/35 families exactly match the already fixed
D562 packet; preserved selected passes and hosted green evidence avoid an unnecessary rerun.
No current-head CI success is inferred. Three original bank sources clarify remaining contract
admission, without a new preset, authentication, provider activation or support claim. The broad
goal and D567/D568, native adoption, stability/speed/A11Y/platform/iOS/Store/release gates stay open.
[Full corrected baseline, checks and source-admission limits](quality/starmoney-decoded-field-boundary.md).


### D-568 - Withdraw the prepared Home publication after failed physical gates

After explicit authorized Loyan handback, preserve the fresh owned Main3130 baseline and original
retained100000-row source. Same-day original/candidate ARM64 processes5215/6816 use separate completed
scalar controls4471/5980 and complete source/native/financial/byte guards. Original3015.6854ms becomes
3284.7178ms (-8.9211% gain); dispatcher441.1000 becomes474.6848ms, failing the1.05-relative gate.
PSS/RSS growth14397/14200KiB stays inside65536, but both main gates fail. No opposite-order/rescue
pair or candidate Main adoption occurs. Restore six exact original files; remove only four new
unpublished source/test files with private copies retained. Thirteen experimental cases are
withdrawn with the API and add0 final shipping tests; successful financial/native suites are not replayed.
Retire only validated empty independent Main bookkeeping M1/M2/M3 with full original source proof,
restore b1d5 in the same owned installer using authorized Don't send, and verify original61-node
English startup before stopping that caller. Strict restored Windows/full Android have0 warnings/
errors; complete signed4a6cb5be/384AOT/408ELF/1422 resources passes offline/ZIP/v2/v3/static16KiB.
Main upgrades once without reset3130to9856, with exact UID/CE/DE/permissions, all141 Home attributes/
Home979 and all three original profile-page states. CSV D571 stays; no clock/system setting/financial
command is changed. Pair26October and final adoption27October are explicit.15 owned XMLs hash-clean;
12769 final-build outputs/1098816790 logical bytes and two unused copies/97644359bytes are removed.
A prior574552517-byte residual deletion remains automatically rejected/unperformed; no bypass.
D567's real idle/reboot cohort is untouched. Whole-Home two seconds, general stability/audible/device/
iOS/provider/Store/release gates remain open. This closes the experiment, not the overall goal.
[Exact failed gates, preserved refusals and restored delivery](quality/home-prepared-publication.md).


### D-572 - Scope the first original physical reader gesture cohort

Use a distinct Samsung/API36/ARM64/current-Main-UID/APK-bound service; preserve the old emulator-only
helper and D567 idle/reboot cohort.24 new actual-policy AT-572 cases and strict complete offline
37AOT/47ELF/v3 packaging pass. With direct contextual owner authority and finite restoration,
English/Persian/German actual accessibility focus opens the original expense form and actual Cancel
returns Home, three opens/three Cancels and zero financial/profile/seed/key commands. Continue only
unfinished Cancels after preserved IME/host-observer refusals; no accepted activation is replayed.
Bounded local playback is retained without upload/transcription or claimed intelligibility.
Restore original system-setting values and absent-key presence on every connection; production
APK/runtime/source, original caller and financial data stay unchanged. Remaining spoken/field/
scroll/theme/width/full-workflow, stability/speed/platform/provider/Store/release gates stay open.
[Complete scope, refusals and actual evidence](quality/physical-reader-scope.md).


### D-573 - Local acoustic evidence without replaying accepted native gestures

Decode the five existing D572 playback WAVs locally with explicit original model paths and no
expected-caption grammar/prompt; verify original hashes and retain full hypotheses privately.
English expense/Cancel and German expense/Cancel/amount terms occur; Persian expected terms do
not. Only the two unresolved Persian clips receive a distinct English diagnostic pass, finding
role words without proving a cause. Three zero-PCM controls are empty. No transcript upload,
phone/emulator input, new voice/engine, application SDK/build/test/financial/gesture replay occurs.
This is machine acoustic evidence, not human intelligibility, pronunciation or complete A11Y.
Owner listening to the existing Persian clip remains pending while independent work continues.
The canonical A11Y-01 row now records D572's actual current-candidate native qualification.
Four redundant archives/155134714 logical bytes remain: automatic review rejects removal before
launch with no detailed reason; no alternate deletion or recovered-space claim follows. Used
runtime/models/notices, WAVs and proofs remain. D567 and the broader goal/gates stay active.
[Exact evidence, unresolved Persian boundary and refusal](quality/local-reader-acoustic-evidence.md).

### D-575 - Preserve omitted optional restore preferences

Correct the reproduced original protected-restore rejection of a package without optional
`display-settings.json`. Explicit nullable memory preserves missing choices; present empty/
malformed JSON still refuses, explicit null affects only allowlisted preferences, and device
security is excluded. Four new AT-575 behaviors pass across selected runs: one genuine failure
is corrected and only that failed case is rerun; the other three successful cases are retained.
Original native restore returns once after proving no earlier restore intent or accepted data;
complete copied rows match. Remaining observations preserve the original accepted restore and
all prepared source/cookie guards. Strict Windows/full offline Android have zero warnings/errors;
384 own AOT/408 ELF,1422 exact resources, ZIP/v2/v3/static16KiB and actual native/managed identity
are checked. Main receives the complete signed no-reset upgrade with all141 public Home
attributes/Home979, UID/CE/DE/permissions and exact system settings preserved. No ledger Save,
Import, Undo or old financial/seed sequence is replayed.

Generated project-relative output is excluded from source discovery after its removal was
rejected before execution;449 residual files/118359992 logical bytes remain, without bypass
or freed-space claim. D574's two SDK-native PID0 cold refusals and original prepared profile
remain retained; no accepted cold timing/two-second result is claimed. D567, spoken acceptance,
general stability/security, iOS/provider/Store/release and the broad goal remain open.
[Full correction, original native outcome and remaining boundaries](quality/optional-restore-preferences.md).


### D-574 - Original loaded cold boundary on retained Samsung sources

Completed the finite reference/tenfold cold cohort without repeating the accepted
source restores or financial/seed commands. The SDK's PID0 records remain refused.
A separately bound one-shot native shell-to-ready interval, including dispatch
overhead, measured8062.511917ms for10,000 entries and17756.488064ms for100,000;
one observation each, not OS-launch metrics, repeated medians or a two-second pass.
All prepared typed rows, financial/read/final cache cookies and original presentation
matched. The tenfold caller's mistaken old-reference presentation was preserved,
then corrected through an independent read-only receipt; no baseline was overwritten.
An ordinary covered startup failure required one native Retry in the same18144;
its unknown cause and the original refusal remain, not a general stability fix.

Five new provenance tests, the prior six lifetime/seven corrected storage cases,
strict caller builds and complete signed offline packages retain their distinct
evidence. Only two fresh profiles were deleted; original independent Main/source
and keys verified, necessary M1/M2/M3 bookkeeping retired before b1d5 restoration.
Main623b/11430/Home979/all141/UID/CEDE/permissions/settings preserved. No D567
emulator action, unrelated test/financial replay or production-schema downgrade.
General QA-06/two-second, ANR/crash, A11Y, platform/provider/Store/release remain open.

[Full source, failure, clock and retirement evidence](quality/loaded-cold-command-boundary.md).


### D-576 / D-577 - Isolated runtime feasibility and current retained Accounts

The original retained Accounts list was reproduced at97/0/97 after the accepted ledger
and Home had81/20/101. Route the actual retained Shell tab callback through the original
shared page read; preserve all calculations, writers, Back behavior and source rules.
Reuse the same four accepted rows: no account, money, seed, key or successful preparation
is repeated for the correction. Six native en/fa/de x Light/Dark contexts reuse one
Accounts instance with complete protected schema/header/typed-row fingerprints unchanged.
Six new AT-576 source-admission cases pass once; native contexts/commands are not unit tests.

The separate experimental .NET10 CoreCLR14/R2R package is never production adoption.
Preserve the observer's JNI readiness failure and other refusals; contain a refused
query without reading source or killing its process. Actual cached/adj900 death and
normal12938 return preserve all original source and101EUR. Only that fresh package is
removed; D567 is untouched. The product remains Mono/SGen, with strict Windows/full
Android zero-warning builds,384 own AOT/408 ELF,1422 exact resources and actual
Core/Data/compression/native engines equal to623b. Complete signed b577 is adopted
once without reset: Main12021/Home979/UID/CEDE/permissions/settings and original caller
are preserved. Relevant native navigation changes only one container drawing-order;
raw refusals/differences remain, without full-tree bitwise equality or private Main
DB/key claims.26,113 owned output files/9,306,774,621 logical bytes and104 owned remote
XML files are cleaned; used packages/proofs and earlier refused scopes remain.

General ANR/crash, speed/two-second, complete A11Y, platform/provider/Store/release and
the overall goal remain open. Category selected-state semantics are an observed
remaining A11Y issue. No unrelated successful suite or financial command is replayed.

[Runtime source, refusals and retirement](quality/coreclr-original-ui-feasibility.md).
[Original Accounts correction and actual Mono adoption](quality/retained-account-overview-navigation.md).

### D-578 - Original category selection caption

The observed missing native selected-state caption is corrected and qualified in
six original language/theme contexts, without financial writes. The canonical
commercial backlog retains overall A11Y/stability/platform/external gates.
[Scoped correction](quality/category-selection-semantics.md).

### D-579 - Original category state through the actual installed reader

The separate helper is freshly bound to signed Main510a3197/97,058,719 bytes and
original UID10380; production source/APK/runtime are unchanged. Eleven new actual
policy/resource cases pass in10+1 selected runs; old successful suites are retained.
Original English reader focus/gestures open one empty expense form, select Food,
select Housing, observe settled exclusivity, deselect Housing and Cancel without
financial Save. Preserve the old-length guard refusal, late IME, intermediate
two-selected native snapshot and failed initial recording; no accepted input is
replayed and no exclusive cache/product cause is asserted. Two complete local
recordings retain native provenance; one unconstrained post-decode observation
finds Housing/selected, without human or general spoken acceptance. All14 original
settings/presence and Main22604/Home979/all141 public attributes/caller are exact.
Only fresh UID10400 is retired;956 registered files and3 owned XMLs are cleaned.
Additional residual/loose deletions are rejected before execution and not retried.
D567, broader accessibility/stability/platform/external release gates and the goal
remain open. Native observations add no managed cases beyond the11 stated here.
[Complete finite reader cohort and limits](quality/original-category-reader-states.md).

### D-580 - Preserve distinct queued Android reminders

Close the diagnosed queued-broadcast cancellation defect: baseline2/4 versus
corrected4/4 actual deliveries after real reboot/controlled idle, exact financial
and SDK Main binaries. Keep five new preparation guards, strict builds and one
no-reset Samsung adoption. D567's original deadline remains failed; its obsolete
fixture is retired only after source acceptance/cause proof, original Main120/
caller/settings restoration and owned AVD stop. Its schedule alone is removed.
Phone now belongs to Dev Loyan1 until explicit return. Further real-time reminder
reliability, accessibility, stability/performance, iOS and external release gates
remain open; no accepted financial command or old successful suite is repeated.
[Complete source, native evidence and limits](quality/reboot-paired-delivery-diagnosis.md).

### D-581 - Mandatory notification source admission under Python optimization

Two actual optimized-subprocess regressions first admit changed replacement/cache
inputs and fail. Replace removable Python assertions with explicit ValueError
rejection for every manifest/license/archive/path/source/cache check. All seven
related source guards pass (two new, five affected existing); one actual strict
Core/MSBuild preparation under PYTHONOPTIMIZE=1 passes without warnings/errors.
All SDK compilation bytes and used signed f7488d1d APK remain exact D580. Reuse
its full Windows/Android/native/adoption results; no app change, new package,
financial/native replay or phone/emulator command. Loyan retains the phone lease;
overall qualification and external gates/goal remain open.
[Complete reproduced defect and focused evidence](quality/notification-source-admission.md).

### D-582 - Retained ordinary API36 adoption of corrected notification Core

Reuse the verified complete signed f7488d1d product without rebuilding. On the
retained API36/x64/4096 ordinary Main, one no-reset upgrade preserves UID10217/
CE352809/DE82676/permission flags, all120 public Home attributes/zero balance,
caller ebd910c2 metadata/process absence, five settings and320x640/density160.
Actual Main3080 becomes3656; no historical PID equality, private database/key
comparison, financial command or new reminder is claimed. Two owned XMLs are
hash-cleaned and only emulator5570 is stopped, with original config hashes exact.
Current emulator Main is now f7488d1d; never downgrade to historical8fd5. Phone
stays with Dev Loyan1 until explicit return. No new managed tests, product builds
or financial/native-success replay; broad qualification/external gates stay open.
[Complete ordinary adoption and limits](quality/retained-notification-core-adoption.md).

### D-583 - Current corrected product bootstrap on isolated compatibility systems

Reuse complete signed f7488d1d without rebuilding for two fresh original-app
bootstrap cohorts. API24/kernel4096 UID10062/PID2745 reaches original Welcome/
Step1 with51 nodes; DE inode is unavailable, not invented. API35/userspace16384/
kernel4096 UID10207/PID3993 reaches original Welcome and exposes all six language
names after one owned body scroll; initial offscreen names are not a proven defect.
No language/Next/restore/profile/financial action or SDK request is sent. Preserve
metadata/tool/focus/label observer refusals without install or launch replay.
Both fresh Main fixtures are retired, original package absence/five settings/size/
density/config exact, four owned XMLs hash-cleaned and both owned AVDs stopped.
No new managed cases or full product build; nonempty data/full onboarding/hardware
16KiB/punctuality/stability/A11Y/iOS/external release gates and goal remain open.
Phone remains exclusive to Dev Loyan1 until explicit return.
[Complete current-product bootstrap and limits](quality/corrected-core-platform-bootstrap.md).

### D-584 - Unused prototype standard cleanup and explicit retirement refusal

Five standard cleans remove all411 registered files/7868802 logical bytes
from exact unused notification prototype project roots, with zero ownership
outliers/registered remnants. Used signed f748 product, live pinned archive and
local native/failure proofs remain exact. Additional guarded retirement of the
remaining E NotificationCore root is rejected before process creation with only
blocked by policy; no deletion or planned backup/manifest occurs from that
command, and no alternate deletion or old refused scope is attempted. Additional
retirement remains incomplete. No build/test/device/financial/Docker replay.
Fresh official-source review finds no newly released MAUI fix beyond current
10.0.120; open upstream investigations do not identify our native cause. Keep
all qualification/external gates and goal open; Loyan retains the phone lease.
[Completed cleanup, refusal and source boundaries](quality/notification-prototype-cleanup.md).

### D-585 - Current-candidate localized reader states and correlated refusals

The scoped physical reader is rebound to the exact current f748 product after
explicit phone return. Three distinct new policy cases pass in selected runs;
preserve the actual candidate regression, wrong runner option and synthetic
request-length mistake separately. Scoped strict helper builds and signed offline
packages pass; Main is not rebuilt/reinstalled and no accepted financial command
is replayed. Fixed refusal codes retain canonical request lineage without arbitrary
exception text, verified natively by a stale-window refusal before any gesture.
German/Light initial/selected/exclusive/deselected/Cancel states pass. Preserve the
Persian intermediate selection loss and generic refusal; the remaining retained
draft and later focused exclusive-transition diagnostic have separate receipts.
The later Cancel opens the expected dirty-draft dialog; preserve the wrong host
Home expectation and use only original owned Discard for cleanup, not reader
confirmation acceptance. The owner's historical recording feedback is English,
not Persian; textual legibility does not accept speech. Original English/Light,
Main/Home/settings identity, helper retirement and cleanup have independent proofs.
General speech/pronunciation and the unexplained earlier loss, stability/speed,
iOS/provider/Store/public release gates and the overall goal remain open.
[Complete native evidence, failures and limits](quality/current-candidate-localized-reader.md).

### D-586 - Distinguish public speech metadata from the actual Samsung reader

An independent no-permission/no-Activity/no-service signed package performs one
metadata request without voice selection, synthesis or financial commands. Public
Google reports473 voices, Persian unsupported/no Persian entry and English default;
actual approved TalkBackPID13984 instead binds private Samsung SMT. Do not confuse
these engine routes. The owner authorizes read-only system speech-language pages:
Samsung3.5.02.16 has14 currently exposed language/region entries, no Persian, and
Use system language selected with actual en-US. No language/voice/engine or download
entry is selected. Preserve API36 deprecation and copied-verifier refusals, plus
an empty-service-list restoration error corrected without reader reactivation.
Opening system settings normalizes an absent default-engine key; restore original
absence rather than claiming no side effect. Main18235/f748/Home979/all141/UIDCEDE/
permissions/EnglishLight, all14 values/presence and original caller are exact.
Only fresh inventoryUID10402 is retired. Standard Clean removes70 registered files/
12845517 logical bytes and nine hash-matched XMLs/262149 bytes. Additional retirement
of the new residual root is rejected before execution;88 files/5006367 bytes remain,
with no alternate deletion. No new managed-test count, shipping rebuild or financial
replay. Persian speech needs a supported engine/voice and separate actual-reader
qualification; current permission is read-only, not installation/selection approval.
General A11Y/stability/speed/iOS/provider/Store/public release and the goal stay open.
[Complete engine-route evidence, cleanup and limits](quality/installed-speech-engine-metadata.md).

### D-587 - Close the owner-cancelled legacy Persian engine candidate

Review the standalone F-Droid1.52.0/code22 APK and source against the pinned signed
repository index: exact digest/size/signer, no requested permissions, ARM64 and
bundled Persian data, minimum21/target33. Initial owner approval permits one
bounded temporary trial, but Android blocks the old target with Unsafe app blocked.
The owner declines Install anyway and requests a newer compatible option. The
original attempt is already terminal with VERIFICATION_FAILURE when cancellation
input is guarded; no stale tap, override, second install, engine/language choice,
synthesis or financial command occurs, and the package remains absent.
Preserve late Samsung default-key normalization rather than rewriting the baseline;
restore original absence with bounded stable observations and exact14-value/presence
checks. Actual Main22461 existed before the install attempt; OS metadata records
old18235 LOW_MEMORY/importance400/status0/trace null. Preserve this selected public
state recovery without claiming PID survival, crash cause or full DB/key equality.
Main f748/UIDCEDE/permissions/Home979/all141/EnglishLight and original caller are
exact. Three hash-matched remote XMLs/68374 logical bytes are removed; used failed
APK/source/provenance and local receipts remain. Host-only recorder review finds
three110-second captures terminated by a12-second final tail before their natural
limit; no audio is repaired, regenerated or replayed to fake acceptance.
Newer compatible review is authorized, but another install/selection needs fresh
approval. Persian speech and broader product/external gates stay open.
[Complete provenance, rejection and preservation](quality/legacy-persian-engine-candidate.md).

### D-588 - Correct the stale current-reader CI expectation

Both final D585/D586 hosted CI runs fail only the older current-category-candidate
method; native Windows/Android jobs and CodeQL pass. Preserve5576 total/one failure/
25 skipped, and delete the two consumed one-shot schedules. Current policy correctly
requires f748 while the old test expects510a. Reproduce only that exact method on
latest ef9d03bc, then update the independent current literal and explicitly reject
both prior510a/4a6c digests. One selected existing case changes from red to green;
no new case, policy relaxation, shipping change, financial test or successful native
build replay. Strict affected test-project build passes. Standard Clean removes393
registered outputs/164750580 logical bytes with no ownership outlier/remnant.
The single delayed exact-head check for a8c5db09 reports CI40538715325 and
CodeQL40538715374 completed/success; preserve the21.20-minute commit-to-observation
delay, consumed receipt and deleted schedule. Initial host DateTime-kind arithmetic
is corrected separately without changing the consumed marker or repeating GET.
The stale-test CI failure is closed; broader product/external gates remain open.
[Complete hosted cause and focused repair](quality/current-reader-ci-regression.md).


### D-589 - Review a genuine newer speech source without retrying the blocked APK

Read the actual Samsung download catalogue to a stable bottom:39 language/region
captions, no Persian/Farsi; no download/voice/engine selection. Return normally to
Main22461/f748/Home979/all141 exact public attributes, original14 values/presence
and caller state; preserve transient-focus/list/encoding/cleanup refusals without
replaying successful input. Ten owned hash-matched XMLs/310990 bytes are removed.
Official stable eSpeak remains1.52.0; pinned upstream ba90c8e9 has1.53.0/code23/
target36 and bundled Persian data, but no completed reviewed binary or runtime
acceptance. Inspect original source, manifest, extractor and official build metadata;
no source execution, build, install, voice selection, reader/financial/test replay.
Isolated SDK terms/scope await the owner; the prior legacy cancellation remains
binding. Persian speech and broader delivery gates stay open.
[Complete newer-source review and remaining approval](quality/newer-persian-speech-source-review.md).


### D-590 - Finish the remaining German capture without replaying accepted actions

Correct the actual host recorder-budget failure using monotonic start, the explicit
40-second natural limit and25-second finite margin under original window/watchdog
guards. One original German Home-title exploration gains actual reader focus;
there is no activation/form/category/financial replay or engine/voice choice.
The recorder exits0 naturally after43.3333 host seconds; original PCM16/stereo48k
WAV7577712 bytes has exact finite RIFF/chunks and39.4667 PCM seconds. The owner explicitly
confirms this exact title sound is German and understandable; this is not inferred
from focus, container validity or visible text and does not accept other utterances.
Main22461/f748/Home979/all141/UIDCEDE/permissions/EnglishLight, original14 settings/
presence and caller are exact; fresh helper10405 is removed once and16 owned XMLs/
675159 logical bytes hash-cleaned. No shipping rebuild or old successful test replay.
Persian and wider product/external gates remain open.
[Complete original recording and restoration](quality/natural-german-reader-recording.md).


### D-591 - Accept only the directly heard German title and retain the Persian gap

The owner explicitly confirms the original D590 sound is German and understandable;
bind acceptance to WAVf061e241/7577712 bytes and only Neue Ausgabe. Preserve the
negative historical Persian listening result and broader speech gates. Host-only
inspection of17 existing Persian packets shows no Housing activation, an
uncorrelated refusal and Food-selected focus records5523/8575ms after Housing focus.
This does not prove immediate clearing or the later managed-state/cached-caption
cause. Original toggle/theme/kind semantics stay unchanged; no speculative fix,
new Android command, gesture, recording, test/build or financial replay occurs.
[Direct acceptance and original packet limits](quality/direct-german-listening-and-fa-lineage.md).


### D-592 - Reject tag-list reuse before native work when the host gate fails

Freeze/link actual original codec/domain sources in two fresh isolated host roles;
only the private candidate reuses the entry's already-owned tag list. Strict builds
and full original-field/order/null/duplicate/mutability guards pass on actual14/X64.
One paired empty/mixed comparison allocates5047904/5383776 bytes for reference and
4785760/5159520 for candidate:combined4.6627% fails the predeclared5% host gate.
Close it without production change, app packaging, native/financial/seed/key input
or successful unrelated test replay. Shipping/model/migration inputs stay exact.
Standard Clean removes28files/593170 logical bytes; further residual-file removal
is rejected before process creation with blocked by policy,20files/422324bytes
remain and no alternate deletion runs. Complete Home speed and broader gates stay
open; no host result is a native/two-second acceptance.
[Complete host eligibility result and cleanup boundary](quality/rejected-tag-list-allocation.md).


### D-593 - Bound the current read-only primary-tab reader cohort

Add dedicated primary-tab operations and copied native Selected metadata only to
the independent helper. Full five-peer/native-role/viewport/unique-selection guards
reject financial-caption aliases; preserve the Italian Cancel/Undo collision and
block ambiguous legacy captions.17 distinct new selected cases plus six affected
existing category-resource cases pass. First native peer-clickability assumption
refuses before a tab gesture; reproduce its failure, allow only the selected peer's
missing redundant action, upgrade the fresh helper once and continue only the four
unperformed Transactions/Plans/Insights/Home steps. Actual focus/selected/title
observations pass; the accepted initial negative control is not replayed or rebound.
Natural original recording exits0; human acceptance of the new clip is not claimed.
Owner English Housing/selected acceptance applies only to historical clip8fed9094.
Main22461/f748/Home979/UIDCEDE/permissions/EnglishLight, original14 settings/presence
and caller match; strict141-node comparison preserves one drawing-order1-to2 delta,
not byte equality. Retire only fresh10406, hash-clean3XML/155778bytes and standard
Clean767files/220472438logical bytes. Extra build-root removal is rejected before
execution;422files/18765916bytes remain without alternate deletion. Product code,
financial writes/keys/voice choices and unrelated successful tests are not changed.
[Complete current cohort, refusals and preservation](quality/read-only-primary-tab-reader.md).


### D-594 - Put further TalkBack last and park Persian speech preparation

The owner considers current TalkBack testing sufficient for now. Finish only the
active cohort's terminal restoration, owned cleanup and documentation. Further
TalkBack tests/recordings/listening requests follow all other available backlog
work; Persian spoken TalkBack has no current priority given no present Iran rollout
plan and limited anticipated Persian-language usage in Europe. Park the proposed
alternate-engine/SDK work without assuming approval or chasing its question. Keep
existing Persian UI/features and completed evidence; this is an execution-order
deferral, not full accessibility/release acceptance or a change to jurisdictions,
financial behavior, accounts or publishing. Other work and the overall goal continue.


### D-596 - Retire camera originals left by an interrupted capture

Two original-filesystem regressions reproduce a gap in startup/data-deletion cleanup:
raw GUID JPEG camera outputs survive when process termination skips `finally`.
The actual shared cleanup now retires only that adapter's top-level opaque JPEGs,
without following links or deleting unrelated/nested/root files. Four new focused
cases pass after the two initial failures. Original Android startup in a fresh UID
removes one staged synthetic original after proven cached-process death; no actual
camera/OCR/financial scenario is replayed. Strict Windows/full signed Android pass.
One no-reset emulator upgrade preserves all120 public tuples; one Samsung upgrade
preserves Home979/141 public captions, geometry and states/UIDCEDE/permissions and
14 settings/presence, with an explicit ScrollView drawing-order2-to1 delta, not full
attribute-byte equality. Used signed885f6df5 and independent7781748a remain;4906 standard
Clean outputs/1523573710bytes and four unused copies/157450351bytes are removed.
Further eight-root retirement is rejected before execution with blocked by policy;
4030files/688445756bytes remain without an alternate attempt. Camera quality, wider
platform/release gates and the full goal remain open; D-594 scheduling is unchanged.
[Original cleanup, native preservation and exact limits](quality/camera-original-cache-retirement.md).


### D-597 - Autonomous emulator work and original camera-result qualification

The owner explicitly authorizes necessary project work in the Zanance-owned emulator
without repeated permission, and necessary low-risk real-phone actions autonomously;
high-risk phone actions still require the owner. Preserve other leases, data and
accepted checkpoints. The original receipt source/camera/provider/preparation/reader
adapters complete one real Camera2 cancellation and one shutter/Done on an owned
synthetic image in the same2222/UID10218. Native results0/-1 and cache-zero outcomes
are preserved. Found14.9/TryMinor(EUR)1490 is qualified without any financial command;
header/footer text loss remains explicit, not whole-camera/OCR acceptance. Reuse the
prepared signedd8b63158 once after approval, without reinstall or old scene replay.
Retire only the fresh helper; original Main3042/885f6df5/all120 public attributes,
UIDCEDE/permissions, five settings/config and caller match; the phone is untouched.
No default app change/build or new managed count: three build-admission checks and
two new native cases are distinct.1263 registered outputs/296045935bytes and two
unused copies/59824542bytes are removed. Further native bin/obj retirement is rejected
before execution;966files/130925594bytes remain with no alternate deletion. Physical
camera/corpus, merchant/text, remaining platform/release gates and the full goal stay
open. D-594 remains in force.
[Complete native boundary and preservation](quality/emulator-camera-acquisition.md).

### D-599 - Bound original command and reader timing before another Home candidate

One new original100k English publication on the owned API36/x64/4096 emulator records
527 fixed typed EF events,109 inside its original publication window, with zero drops.
Whole1664.9301ms, command intervals9.1219ms and reader lifetimes433.7934ms are distinct
instrumented boundaries, not exclusive CPU costs or a phone/cold/two-second pass.
Eleven compilation starts/plans occur outside the window and none inside. Original
full financial/native/schema/typed-row/source/ciphertext controls pass; separate scalar
oracle4086 is proven dead before target4279. There is no financial/seed/key replay.
A refused provisional package is never installed; final53725d08 has exact adopted
Core/Data/Brotli PE/native-engine identity,192AOT/204ELF/static16KiB and strict build
proof. The observer records no SQL/parameters/payload strings/keys/native addresses.
Restore actual ebd9/114 German public dictionaries and stopped flag, Main2942/885f6df5/
all120/settings/config; no phone input. Twelve XMLs/484581bytes,1263 Clean outputs/
296011853bytes and five unused copies/178872682bytes are removed. Further bin/obj
retirement is rejected before execution;967files/130910659bytes remain without retry.
Protected comparison sources stay bound for continuation; no shared alias is retired.
No new managed tests or default app change/build; the full goal and D-594 stay active.[Complete original timing boundary](quality/home-query-reader-timing.md).

### D-601 - Withdraw ledger text reuse and recover the original retirement report

Bounded immutable Note/Payee reuse passes a four-shape host prerequisite and two owned
emulator pairs, but the physical first pair is only 3129.0962 to 3120.7219ms (0.2676%),
below the frozen 5% gate. Withdraw all product changes and five experimental cases;
final new managed count is zero. The 69 selected executions include those five cases,
not a larger retained suite. No phone reverse/rescue, shipping build or Main upgrade.
Reference Core/Data shipping-byte identity is not reproduced; pair checks cannot replace
D-543, and no shipping-equivalent performance acceptance is claimed.

Fix the actual read-only observer's missing retirement phase, require its Comparison,
and select only an already listening explicit ADB server (default 5038). Recover original
PID11081's accepted report without repeating retirement; three bounded observer outcomes
pass. Full empty independent-Main schema proof precedes original B1D5 restoration.
Main24382/885f6df5/Home979/all141, caller actual English onboarding/all61/UIDCEDE/flags/
absence/stopped=false and 14 settings/presence match; emulator/Main/caller/config also
restore exactly. No financial/seed/key replay or private shipping Main DB/key read.
Standard Clean removes 9148 registered files/2253169797 logical bytes; 18 new XMLs/
714460bytes retire. Three further removal scopes are rejected before execution with
`blocked by policy`; 6376 files/1413241028bytes remain without retry or alternate deletion.
Protected evidence/shared aliases remain. The overall goal and D-594 execution order stay
active; two-second, stability, security, remaining accessibility and external gates stay open.

[Complete candidate rejection, input limits and observer repair](quality/withdrawn-ledger-text-reuse.md).


### D-603 - Observe live Android backup eligibility without a payload request

The fixed shell-only observer binds the original owned package UIDs before typed
BackupManager status/eligibility queries. API36 emulator returns Main10217=false,
caller10221=false and Settings-provider1000=true while backup is disabled. Samsung
returns Main10380=false/caller10381=false while its original backup-enabled state
remains true. These are five real framework replies, not actual OEM transfer or
cloud-payload exclusion. A wrong expected UID refuses before service binding; errors
never substitute false. Strict corrected Java/D8 compilation passes; the initial
module-path warning refusal remains. No product code/dependency/permission change,
managed test addition, app rebuild, backup/restore, authentication or financial replay.

Both original APK/UID/CEDE/flags/stopped/PID states, eleven settings, boot/backup state
and display context remain unchanged. Two new hash-bound remote JARs retire with
6236 logical bytes removed; local used build witnesses remain. The owned AVD stops
with exact configuration preserved; both ADB servers and phone context remain.
SEC-07 actual OEM/device-transfer and iOS gates stay open. Overall delivery remains
active; further TalkBack stays last and Persian speech parked under D-594.

[Complete native evidence and limitations](quality/android-live-backup-eligibility.md).

### D-604 - Align current optional-service and backup disclosure inventories

Correct the obsolete absent-feature, active Android OS-backup and cloud-only data-flow
claims against current source. Personal service-readable sync, household payload E2EE,
aggregate-analysis prerequisites, explicitly fetched ECB references and inactive real
purchases retain distinct boundaries. Both app/starter flow rows agree; old SDK/build
versions remain explicitly historical. Submission answers and the public policy stay
drafts requiring the actual selected artifact/processor/platform acceptance. No product
change, new managed test, build, native/financial replay, publication or account action.
[Source-backed corrections and document verification](quality/current-privacy-inventory-alignment.md).

### D-605 - Preserve queue reconstruction across null saved snooze slots

Five fresh regressions reproduce valid-JSON null slots aborting Core/actual SQLite
reminder publication. Filter only null records before the original queue cap; retain
all fields, original source/privacy validation and deadlines. Five new plus one affected
existing managed case pass; no full-suite or successful financial/native replay. Strict
Windows and complete signed Offline384AOT/408ELF product checks pass. One fresh API36/x64
native cohort publishes original plan+snooze after20 nulls, preserves the complete typed
fictitious source and zero ledger, then removes both requests and only its new fixture.

Preserve real startup/storage/AOT/observer refusals; never weaken the staging-space
bound. Restore exact prior stopped portable code with KEEP_DATA ownership/flags unchanged.
Samsung no-reset adoption24382/885f6df5 to17668/1f9816c4 preserves all141 original public
attributes/Home979 and package/settings guards; the first51-node loading observation
continues only in the same process. Original emulated Main/zero/all120 is restored and
its AVD stops. Eight XMLs/207653bytes and3175 registered Clean outputs/1041974987logical
bytes retire; used evidence remains. Delivery, broad stability/security/OEM/other platform
and release gates stay open; TalkBack remains last. [Complete evidence](quality/null-snooze-cache-reconstruction.md).

### D-606 - Specific safe startup storage guidance completed

Classify only the actual unchanged staging-space preflight with a typed IOException;
retain the covered original Retry and block inappropriate key/backup recovery callbacks.
Seven new plus four affected managed cases pass; eighteen new synthetic native layouts
and full package/adoption checks pass. No successful financial/old reminder cohort or
unrelated suite repeats. General low-space recovery and external release gates remain
open. [Scope and receipts](quality/startup-storage-guidance.md).

### D-609 - Retained camera geometry only

The original clipped D-598 frame is geometrically bounded without another camera or
reader run. QA-05 still needs an actually complete supported camera input; no managed
count, product behavior or completed financial result changes. State remains in the
canonical backlog. [Evidence](quality/original-camera-frame-geometry.md).

### D-610 - Direct ledger output not adopted

The new complete-entry output strategy fails its declared host allocation prerequisite;
no native/financial replay or source integration follows. The canonical performance
parent remains open; rejected output retirement is not reported as complete.
[Scope, values and refusal](quality/rejected-direct-ledger-output.md).

### D-611 - Empty derived-identity fallback locally verified

The actual decoder now declines an empty optional identity, retaining complete original
SQLite rows and damaged-cache bytes. Six new/three affected checks and one native Home
pass; no old financial/native cohort repeats. Ready product adoption on Samsung remains
separate behind D-607; the canonical backlog retains broader gates.
[Evidence and exact limits](quality/derived-ledger-empty-identity.md).

### D-612 - Unsupported derived-schema fallback locally verified

Fourteen new cases plus three affected existing cases pass in selected continuations.
Strict builds and one original native fallback pass; physical adoption stays behind
D-607 and broader gates remain separate.
[Evidence and exact limits](quality/unsupported-derived-ledger-schema.md).

### D-613 - Camera source byte boundary locally verified

Six fresh focused cases and one changed original native acquisition pass; no old
cancellation, OCR, Save, seed or financial cohort repeats. D-598 whole-frame/text
limitations and D-607/physical product adoption remain separate.
[Evidence and exact limits](quality/bounded-camera-source-read.md).

### D-614 - Close lower-quality encoding at its first native rejection gate

A distinct encoder-only host candidate passes eligibility but is13.028% slower in the
first original100000-row native Home publication; dispatcher ratio1.13058 also fails.
No reverse, physical test, product promotion or successful financial replay follows.
All tracked sources restore; owned outputs retire with explicit residual refusals.
[Evidence and exact limits](quality/rejected-ledger-brotli-quality.md).

### D-615 - Bound portrait framing and correct the observed merchant suggestion

The finite new camera input still has missing header/footer pixels. Correct the
independent date-as-merchant defect with10 new/45 related cases and one unchanged-JPEG
native reread; no old acquisition/financial success is repeated. Preserve onboarding
navigation failure and keep full-frame, physical/corpus and D-607 gates open.
[Evidence and exact limits](quality/portrait-camera-merchant-boundary.md).

### D-616 - Finish the named complete-camera-source assertion

One new supported virtual-scene frame yields all9 original primary lines, the actual
shop and23.45EUR through original Camera2/preparation/recognition. No old direct/stored
OCR, shutter, Cancel, managed success or financial command is repeated. Preserve the
extra rotated-poster OCR row and historical imagefile failures; no product change.
[Evidence and exact limits](quality/complete-virtual-scene-camera-source.md).

### D-617 - Finish the original receipt editor assertion and observed onboarding defect

Exact original and18 layout contexts pass after independent step scroll ownership.
Two original complete-product receipt drafts retain23.45EUR with correct/empty payee,
then cancel without ledger writes. One fresh zero account is setup, not a replay of
prior financial success. No new unit count or whole-corpus/physical acceptance.
[Evidence and remaining gates](quality/original-receipt-drafts-and-onboarding-scroll.md).

### D-618 - Finish the named changed-onboarding OS200% assertion

The existing complete D-617 product passes18 actual OS200% viewport/direction/footer
contexts without new build/test/source/financial work. Preserve the disqualified
initial observer and exact owned cleanup refusal. Other broader device/accessibility
and external gates remain separate; do not repeat this finite matrix unchanged.
[Evidence and exact limits](quality/onboarding-independent-scroll-os-text.md).

### D-619 - Close the unmet actual16-window premise before native work

Two private encode-only roles show actual22 versus18 even though the candidate asks16.
Original inputs and product remain exact; no decoder/warmup/timing/native/financial
work is repeated. Preserve primary-source qualification and the one cleanup refusal.
QA-06 remains open without another unchanged timing cohort or new product test count.
[Evidence and exact limits](quality/requested-brotli-window-admission.md).

### D-621 - Close the observed complete named-date suggestion gap

The original numeric/calendar policy remains exact.24 new cases and43 selected
existing cases pass; one new ordinary complete-product file/reader/editor route
shows2025-10-27,7.10EUR and BAY MARKET before Discard, with final empty ledger.
Strict builds pass, initial Windows restore and cleanup refusal remain preserved.
No camera/old source/financial success or unrelated suite is repeated; broader gates
and physical adoption after actual D-607 completion remain separate.
[Evidence and exact limits](quality/complete-named-receipt-date-suggestions.md).

### D-622 - Connected/unlaid framework observation, not a general stability pass

Two new native requests establish actual attachment/focus with0x0/unlaid/requested
layout. Original task completion follows native layout release; disconnect leaves a
bounded pending task. Preserve D-455's distinct hidden-root failures and all broader
app correlation gates. No product/financial/test count changes or old successful
cohort repeats. Fresh environment retires; one supplemental cleanup refusal remains.
[Evidence and exact limits](quality/connected-unlaid-scroll-lifetime.md).

### D-623 - Original validation-layout refusal, no speculative product change

The exact linked current helper refuses the held unlaid native target. A different
window-global event and original application fault remain unproved; no candidate
or second helper/scroll/financial operation is admitted. Fresh UID/AVD retire and
product/test counts remain unchanged, with one supplemental cleanup refusal.
[Evidence and exact limits](quality/original-validation-layout-refusal.md).


### D-624 - Six-language Android draft asset batch complete

Eight actual1080x1920 RGB frames per language cover the eight planned roles; historical Windows originals remain. Source/image/pixel/native hashes and scoped data preservation are recorded. One rejected frame is replaced without financial replay; an actual ANR remains unresolved. This closes the Android draft artifact batch only, not final Store composition, release edition, accounts, physical adoption or publication.
[Evidence and exact limits](quality/android-store-drafts.md).

### D-628 - Complete original account-to-holding conversion ownership

Complete original review, file/source/consent guard, atomic writer and typed accepted
follow-through are integrated in the existing account detail. Original formulas and
explicit preview stay.28new focused cases, affected checks, strict builds, one
original native Cancel/conversion and scoped presentation checks pass without replay.
Font-recreation lifetime and physical adoption are separate, two cleanup refusals
stay explicit, and the overall delivery goal remains active.
[Evidence and exact limits](quality/original-account-conversion-ownership.md).

### D-629 – Original complete account-detail batch

Complete original-file source, worker figures, covered loading/error/Retry, current-page
drafts and original route/consent/writer ownership are implemented and qualified by29new
focused cases,6affected existing cases and the original Release/AOT native cohort. The
actual stale installment confirmation produces no ledger delta; accepted notification
failures remain one-shot. Strict Windows/full offline Android pass. See
[complete evidence](quality/complete-original-account-detail.md). Physical adoption after
D-607 and broad security/stability/performance/platform/release gates remain open.

### D-630 – Complete original repayment schedule

Original source/query/lifetime failures are corrected with worker read/validation, private
complete projection, current page/display/file guards and one covered Reset.14new focused
cases and6affected existing free-policy cases pass; strict Windows/full offline Android
and the original native source/cover/error/Retry/tab-return/11-row/last-row/display cohort
pass without a ledger post. See [full evidence](quality/complete-original-loan-schedule.md).
Physical adoption after D-607 and general security/stability/performance/platform/release
gates remain open; no previously accepted financial operation is replayed.

## D-633 - Original Plans center engineering batch

Keep original plan rows/review badge in one validated file snapshot and retire old native
commands. Preserve original formulas/routes and actual permission flow; share appearance/
navigation and retain explicit error/Retry. See
[full current evidence](quality/complete-original-plans-center.md). This history does not
replace the canonical commercial backlog or complete its broad parent release gates.

## D-634 - Original template-center engineering delivery

Keep complete original definitions and independent rights tied to their original file
and current native owner. Cover failures with explicit Reload, refuse old confirmation
responses and retain accepted deletion through its following refresh. Nine focused new
cases and72changed native states pass; no old successful financial operation is replayed.
[Full source, restoration and cleanup record](quality/complete-original-template-center.md).
The canonical commercial backlog and broad release gates remain authoritative.


## D-640 original goal editor

Original goal editor source/lifetime/receipt engineering work is complete in the independent scope. Keep actual physical adoption and broad security/stability/performance/accessibility/platform/provider/release acceptance separate; D-607 and deferred TalkBack priority remain unchanged. [Evidence](quality/complete-original-goal-editor-source.md).


## D-641 original budget editor

Original budget editor coherence, cancelled/retained publication, explicit retry and accepted writer ownership are delivered in the independent scope. Physical adoption and broad security/stability/performance/accessibility/platform/provider/release gates remain separate; D-607 and deferred TalkBack priority are unchanged. [Evidence](quality/complete-original-budget-editor-source.md).


## D-642 implementation continuation

Original type-editor source and ownership are delivered within their documented independent scope. Current remaining state remains in the canonical commercial backlog; this historical plan is not a second executable queue. [Evidence](quality/complete-original-asset-type-editor-source.md).


## D-643 implementation continuation

Original asset-event frame and accepted raw-draft behavior are delivered within the recorded independent scope. Remaining executable state stays in the canonical commercial backlog; this historical plan is not another task queue. [Evidence](quality/original-asset-event-frame-and-receipts.md).


## D-644 implementation continuation

Original holding-detail price draft/frame/receipt behavior is delivered within the recorded independent scope. Remaining executable state stays in the canonical commercial backlog; this historical plan is not another task queue. [Evidence](quality/original-holding-price-draft-ownership.md).


## D-645 Original comparison source ownership

Complete source, current native frame, covered Retry and reviewed delete receipts are implemented. Fourteen new focused cases and31affected existing cases pass; original native pending/Retry/Cancel,12ready and6accepted-feedback contexts preserve the same source. One saved-definition deletion changes no financial rows. Physical adoption after D-607 and broad DB/key/security/performance/stability/accessibility/iOS/provider/Store/release gates remain separate. [Evidence](quality/original-saved-comparison-source-and-delete.md).


## D-646 Original forecast workspace ownership

Complete original source, covered Retry, actual row/native prompt ownership and accepted saved-path receipts are implemented. Twenty new focused and 31 affected existing cases pass; original native pending/Retry/Cancel/scenario, 12 ready and six final live receipt contexts pass with retained source. The real tab-root/caption failure is preserved and corrected. Two DISTINCT saved definitions serve the initial and changed-display branch; no financial posting or preparation is replayed. Physical adoption after D-607 and broad DB/key/security/performance/stability/accessibility/iOS/provider/Store/release gates remain separate. [Evidence](quality/original-forecast-workspace-prompt-and-receipts.md).

### D-647 - Complete original report workspace and KPI handoff (2026-10-29)

Original report reads and native commands now share the actual pinned-file packet, covered
publication, cancellable page/tab/root ownership and exact original row membership. All
native destinations freeze their original route/query before awaits. Source-bound KPI
explanations detach lines, retire replacement queries and validate their own normal-close
handoff without breaking the expected original parent return. Stale source stays covered
with explicit Retry; current captions and retained report choices refresh in place. An
incoming original drill scope survives deferred display rebuild until ordinary departure.
The actual contribution writer validates native ownership before/after SQL and records
commit receipt before Changed; original financial calculations and schema are unchanged.

Two original native failures and two original KPI host failures remain in the evidence.
Twenty-two distinct new focused cases and28 affected existing reader/export/access cases
pass. Actual API36/x64/4096 UID10216 boot1ffc2936-e728-4e88-b441-ba848600ef31 continues
from original PID4749 to corrected5947 by one no-reset update. The single original zero-EUR
unknown-opening account and all19 observed tables remain identical; no financial post,
setup replay, PDF/native share or accepted contribution command is repeated. Pending-read
cancellation, actual old issue refusal, read failure/Retry, valid frozen KPI navigation,
stale display feedback and retained current package captions are verified in original UI.
See [complete scope and limitations](quality/original-report-workspace-and-kpi-handoff.md).
Phone/D-607, all three deadlines and post-reboot unread source remain untouched. Later
physical adoption, broader DB/key/security/performance/stability/accessibility, iOS/provider/
Store/release gates remain separate. Overall delivery remains active; TalkBack stays last.

### D-648 - Complete original Settings source and current-profile reset (2026-10-30)

Original Settings now reads complete preferences/accounts and the unchanged three-month
suggestion history from one actual-file worker transaction, validates after native availability,
and retires page/tab/root reads and sensitive continuations. Independent field edits compose
while original reference/account/history identity stays guarded. Raw estimate/reminder drafts
survive failure/Retry; current calendar/display choices retain their original form meaning.
Original device authentication remains genuine. Rare explicit current-profile reset validates
opaque original typed source/schema and native ownership in the original writer transaction,
records commit before Changed and resumes follow-through without deleting again. The old
reset also removes omitted local contacts/cost graph/reference receipts/resource selections;
shared-file guards, device PIN, other profiles and external backups remain protected.

Twenty distinct new and35 affected existing focused cases pass; published and intermediate
failures remain documented. Strict Windows and full offline384AOT/408ELF/1608staticLOAD pass.
Fresh API36/x64/4096 UID10216 continues4304to6302 by one no-reset update and exact read-only
source adoption. Actual pending Back cancels, departed Delete and old native consent refuse,
raw123.4500 survives failure/Retry/display/calendar, one Day2 metadata choice changes only
that field/UpdatedAt, and one legitimate current fake-profile reset reaches empty onboarding.
No financial post/estimate Save/setup replay or genuine native authentication is claimed.

Original OS/display/boot restore; only fresh UID/data,48hash-matched XMLs1,705,935bytes and
owned AVD retire. Six standard cleans remove43,012files2,791,927,695logicalbytes; official
AVD deletion25files5,336,085,476bytes and one new17-target cleanup4,784files1,352,420,443bytes
succeed. Used signed packages/evidence/source archives remain; older refused scopes are
not retried or reused. Phone/D-607/three deadlines/post-reboot unread source remain untouched.
Physical adoption, broader DB/key/security/performance/stability/accessibility, iOS/provider/
Store/release gates stay separate. Overall delivery remains active; TalkBack stays last.
[Complete evidence](quality/original-settings-source-and-profile-reset.md).

### D-649 - Complete original manual-rate workspace (2026-10-30)

Original rates now pin the first actual settings file before initialization, read complete
original rate facts and consumed currency hints in one worker transaction, validate before
publication/navigation, and retire actual page/tab/root reads and reference commands. Failed
source stays covered with explicit read-only Refresh; raw pair/culture/ratio and original
writer/receipt semantics remain. Original rows require exact current instance identity for
native Delete consent, even after identical visible republication. One complete row replacement
keeps original formatting/order. The page consumes its owned failed cover rather than hiding
Refresh behind another generic modal. No FX algorithm, financial entry, model/migration,
provider, runtime or notification deadline changes.

Thirteen distinct new and26 affected existing cases pass; original and intermediate failures
remain. Strict Windows/full offline384AOT/408ELF/1608staticLOAD pass. Fresh API36/x64/4096
UID10216 continues4387to5908to7117 through two necessary no-reset updates of the same source.
Actual pending Back cancels, closed reference navigation refuses, current reference navigation
works, raw1.2500 survives failure/one read-only Retry, and the remaining corrected inline-error
boundary qualifies actual visible en/System/deDark/faLight captions and read-only Refresh.
Two preliminary hidden-under-dialog caption observations are disqualified. Earlier successful
native cases/raw/setup/rate and financial operations are not replayed for the final UI correction;
all19 observed source tables remain equal and native rate/ledger writes are zero.

Original OS/display/boot restore; only fresh UID/data,39hash-matched XMLs1,311,074bytes and
owned AVD retire. Five standard cleans remove42,642files2,638,963,151logicalbytes; official
AVD deletion25files5,491,864,523bytes and one new18-target cleanup4,694files1,492,565,393bytes
succeed. Actually installed signed packages/final shipping/evidence/source archives remain;
older refused scopes are not retried or reused. Phone/D-607/three deadlines/post-reboot unread
source remain untouched. Physical adoption and broader DB/key/security/performance/stability/
accessibility/iOS/provider/Store/release gates remain separate. Overall delivery stays active.
[Complete evidence](quality/original-manual-rate-workspace.md).

### D-650 - Complete original display-unit workspace (2026-10-30)

Original unit reads now pin the actual first settings file, validate consumed definitions/
default hints and retire page/tab/root readers and native commands. Explicit read-only
Refresh keeps raw name/currency/exponent, full source cover and current captions. Submitted
lists come from original captured definitions, with an ambient publication seal; a different
profile’s later global definitions cannot be copied into the original selective writer.
Remove requires exact original row identity. Unknown changed definitions require explicit
reentry; known own receipts admit only their actual readback, consume only unchanged submitted
input and preserve later typing. Post-commit source failure retains known saved/not-published
feedback without a new authority capture or another write. Legacy-null preference migration,
ISO money/export/FX semantics and unrelated newer preferences remain unchanged.

Four original real-source failures and two native failures remain preserved. Fourteen distinct
new,12 affected existing and21 changed-resource completeness cases pass. Strict Windows/full
offline384AOT/408ELF/1608staticLOAD pass. Fresh API36/x64/4096 UID10216/4870to5782 continues
once no-reset with exact original-source readonly adoption. Actual pending Back cancels,
closed Load refuses, initial read error/Refresh is inline, same raw D650 crates/EUR/exponent3
survives three ready and three failed display contexts and explicit Retry. Only one changed
original unit Add metadata command commits; only DisplayUnits/UpdatedAt change, the other18
observed tables stay equal, and native accounts/financial entries remain zero. Original
baseline is preserved separately from that accepted delta; old D-436 matrices are not replayed.

Original OS/display/boot restore; only fresh UID/data,34hash-matched XMLs1,214,444bytes and
owned AVD retire. Six standard cleans remove42,933files2,737,807,575logicalbytes; official
AVD deletion25files5,480,985,547bytes and one new16-target cleanup4,753files1,239,395,088bytes
succeed. Actually installed signed packages/final shipping/evidence/source archives remain;
older refused scopes are not retried or reused. Phone/D-607/three deadlines/post-reboot unread
source remain untouched. Physical adoption and broader DB/key/security/performance/stability/
accessibility/iOS/provider/Store/release gates remain separate. Overall delivery stays active.
[Complete evidence](quality/original-display-unit-workspace.md).

### D-651 - Complete original reference workspace and offline access lifetime (2026-10-30)

Original market-reference reads now pin the actual captured file, read assets and bounded
saved history in one deferred worker transaction, publish complete collections and retire
page/tab/root cache/network work. Old public Load cannot activate a departed page; equal
copied choices do not gain original review authority. Current native/source checks remain
in the exact selective reference writer and typed committed receipt. Original free ECB
provenance, precision, stale acknowledgment, asset mapping, manual fallback and explicit
provider/financial consent remain unchanged. Offline startup/Sleep/Resume/navigation now
maintain the same native access lifetime needed for local reference/analysis, without
enabling network work or a new permission. No model/schema/runtime/provider policy changes.

Three original actual-source failures, the actual offline native pre-read refusal and
readiness/selector refusals remain preserved.11distinct new and11affected existing cases
pass; host logs identify10.0.12, not a relabelled application servicing runtime. Final strict
Windows and full offline384AOT/408ELF/1608staticLOAD pass. Same fresh API36/x64/4096 UID10216
continues4422to5461 once no-reset with exact original source/consumed-marker adoption.
Actual pending Back cancels, old closed Load refuses, full inline source error/one Retry
recovers, and complete original19-table source stays equal. Accounts/rates/financial posts
are zero; no provider, preparation or earlier positive monetary/ECB matrix is replayed.

Exact original display/OS/boot restore; only fresh UID/data,18hash-matched XMLs442,981bytes
and owned AVD retire. Four Clean operations remove28,955files1,990,429,171logicalbytes and
official AVD deletion25files5,312,295,890bytes. Supplementary13-target cleanup is blocked
before process creation;3,733files997,346,279bytes remain and no retry/subset/alternate/move/
escalation/reuse is attempted. Used signed packages/final shipping/pre-refusal archives and
evidence remain. Phone/D-607/three deadlines/consumed reboot/unread source remain untouched.
Physical adoption and broad DB/key/security/performance/stability/accessibility/iOS/provider/
Store/release gates stay separate; overall delivery stays active. Complete evidence:
`quality/original-market-reference-workspace.md`.

### D-652 - Complete original accountant disclosure workspace (2026-10-30)

Original accountant choices now read minimal immutable active/archived account identities
under one captured-file worker transaction. Original Review validates those actual captions,
currencies/archive identities before scanning another packet. Page/tab/root retirement cancels
source/review/unoffered output; old public Load cannot reactivate disclosure authority and
old completion cannot clear newer busy state. Whole choices publish together. Literal year,
notes/documents opt-ins and unchanged explicit account choices survive failure/Retry/return;
changed identities require new disclosure and retired callbacks cannot erase current review.
Original tax/data-only scope, ZIP contents/bounds/digest/day/rights, redaction, real native
authentication/share and financial behavior remain unchanged. No model/schema/runtime change.

Four actual original-source failures remain preserved.10distinct new and4affected original
native-await refusal cases pass; successful earlier positive output/financial matrices are
not replayed. Strict Windows/full offline384AOT/408ELF/1608staticLOAD pass. Fresh API36/x64/
4096 UID10216/PID4765 has one zero-opening account metadata prepared once and ledger/rates0.
Actual pending Back cancels, closed Load refuses, one raw2025/account/notes input remains
through source failure/Retry with checked native binding and full feedback. One read-only
Review produces only summary; ZIP/share/authentication/financial writes0 and complete original
19-table source equal. Private read-only Entity.Id compile refusal and clipped-target input
refusal remain; only the latter unaccepted checkbox continues after bounded normal scroll.

Exact original OS/display/boot restore; fresh UID/data,25hash-matched XMLs1,001,492bytes and
owned AVD retire. Four Clean operations remove28,955files1,990,843,079logicalbytes; official
AVD deletion25files5,245,121,522bytes and one new10-target cleanup3,726files742,213,210bytes
succeed. Used signed native/final shipping/source archives/evidence remain; earlier refused
scopes are untouched. Phone/D-607/three deadlines/consumed reboot/unread source remain
untouched. Physical and broad DB/key/security/performance/stability/accessibility/iOS/provider/
Store/release gates remain separate; overall delivery stays active. Complete evidence:
`quality/original-accountant-disclosure-workspace.md`.


### D-653 - Complete original CSV workspace

Original CSV import/export now pins one actual file, reads complete workspace/export sources
on the worker, and binds picker/preview/native disclosure/Undo to the actual page/tab/root.
Read-only source Retry preserves raw filename/mapping/calendar and known committed receipts.
Original rules are read in the preview transaction; original financial writer/conflict policy,
all32 fields, journal/CSV formats and explicit native authentication remain. Fresh private CSV
copies preserve BOM/neutral names without overwriting earlier offered copies.16 new and10
related focused host cases, actual fresh native cancellation/retained raw Retry/delayed export
refusal with zero financial operations, strict builds and complete signed offline packages
are recorded in [D-653 evidence](quality/original-csv-workspace.md). Physical adoption waits
for actual D-607 completion; its three deadlines and unread post-reboot source are untouched.
This is not general stability, speed, A11Y/security/DB/key/iOS/provider/Store/release acceptance.


### D-654 - Complete original backup restore lifecycle

Original local/cloud restore selection pins its native frame and actual target publication
before selection. Stream/inspection, consent, authentication, safety, exclusive primary queue
and protected receiver refuse retired/replaced owners. Original accepted primary receipts
remain known before fallible follow-through and cannot replay replacement; actual return
retries only follow-through. Current raw review/error behavior and original restore/encryption/
recovery algorithms remain unchanged. Actual PIN modal authority is exact weak root/page/modal;
its owned cover survives until Cancel while an unrelated route is refused. No credential,
permission, schema, format or financial policy changed.

Twenty distinct new and4 affected host cases, original native root-retirement and actual
LockPage cancellation with full19-table source equality and zero financial/primary/requested
safety operations pass. Strict Windows, complete offline384AOT/408ELF/1608staticLOAD shipping
and independent signed native packages pass. Intermediate host/observer/presentation failures
remain preserved. General backup creation/cloud discovery and other sensitive-page lifetime
work are separate; this is not genuine authentication or full backup release acceptance.
[D-654 evidence](quality/original-backup-restore-lifecycle.md) records the exact boundaries.
Phone/D-607/three deadlines/consumed reboot and unread source are untouched; physical adoption
and broader DB/key/security/stability/speed/A11Y/iOS/provider/Store/release gates stay open.


### D-655 - Complete original portable backup creation

Original local/cloud creation captures actual native/source/input/destination before sensitive
confirmation and keeps an independent portable busy lifetime through the original operation. Exact owned PIN cover
survives Cancel; an unrelated route refuses. One-shot original creation guards every storage
effect, consumes only unchanged submitted input and accepts the actual stored receipt before
fallible listing/share. Original service filename/history/retention pins its first profile;
changed profiles cannot relabel/prune another set, and later timestamp/retention failure or
cancellation cannot erase a known upload receipt. Original format/encryption/defaults/source/
financial/permission/provider policy remain unchanged.

Six original-service failures,12 distinct new and3 affected cases, strict Windows/full offline
384AOT/408ELF/1608staticLOAD and original native LockPageCancel/busyBack/root-retired-confirmation
pass. Full19-table source equal; manual portable/share/financial0. Fresh UID/11hash-matched XMLs/
owned AVD retire with exact original OS. [D-655 evidence](quality/original-portable-backup-creation.md)
keeps failures, receipt and acceptance limits. General discovery/cloud-identity/deletion and
other sensitive-page work remain separate. Phone/D607/three deadlines/consumed reboot/unread
source are untouched; physical/provider/DBkey/security/ANR/speed/A11Y/iOS/Store/release gates
remain open. Overall delivery remains active.

### D-656 - Correct original CI readback and account recovery

The actual failed test job was reproduced on current source: 26 failures in 52
affected original cases. The correction retains the actual original committed CSV
writer's opaque readonly receipt for batch publication, preserves interpretation
feedback, and makes explicit stale-account review read the complete original source
while retaining raw input and account/default/currency guards. Historical assertions
now match original atomic account/default writes, owned route origins, quiet retired
callbacks and inline read covers; no source comparison or writer conflict policy is weakened.
The 53 distinct selected existing cases and eight new negative/receipt cases passed
in focused continuations. Strict Windows and complete offline Android packages passed.
Original native PID 4118 kept the rejected account draft through full explicit review,
then one distinct CSV import changed only Entries/ImportLinks among 20 source sets,
published one actual batch and produced no false follow-through failure. Fresh UID/AVD
and 29 hash-matched XML files retired with exact OS preservation. Supplementary cleanup
of 11 targets was rejected before process creation; 3715 files/947761224 logical bytes
remain and must not be retried, subdivided, moved, escalated, reused or deleted by another method.
Phone/D-607 and all three deadlines remain untouched. Physical adoption, general backup
discovery, sensitive-page work and broader release gates remain separate; pushed-head CI
has one delayed check, not an assumed pass. [D-656 evidence](quality/original-ci-readback-correction.md)

### D-657 - Original backup workspace and destination ownership

Original local/automatic/safety/connected discovery now retains its file/page/root,
cancellation and current row/account identity through native consent and publication.
Sensitive commands have independent ownership; readonly Back cancels pending discovery.
Loading/error covers require explicit Retry, preserve raw input and known actual deletion
receipts, and reject retired source, disclosure and destination effects. Discovery still
spans own-application profile sets independently of retention. Two new library cases,
one related existing case and 32 new application cases passed in focused continuations;
24 targeted resource checks and strict Windows/full offline Android builds passed.
Original native qualification used fictitious identity/local storage, not genuine OAuth:
12 narrow OS200 error contexts and six later receipt contexts, one native connection,
one disconnection and one confirmed fake-file deletion. Full 20-collection source equality,
zero accounts/ledger and exact original OS/preferences remained. Incomplete initial
preparation and necessary no-reset observer/receipt upgrades are separately documented;
successful operations were not replayed. Fresh UID/57 hash-matched XML/owned AVD retired.
Five standard cleans removed 29196 files/2113039306 logical bytes; one bounded supplementary
cleanup removed 3766 files/749948420 logical bytes. Earlier refused scopes were untouched.
Phone/D-607/three deadlines/post-reboot unread source are preserved. Physical adoption,
genuine providers, full DB/key/security/stability/speed/accessibility/iOS/Store/release
remain separate. D-656's sole exact-head CI response was green for all four workflows;
D-657 has its own single delayed pushed-head check. [D-657 evidence](quality/original-backup-workspace.md)

### D-658 - Original profile catalogue and native command lifetime

Original profile commands retain independent busy/page/root/frame/disclosure ownership
through prompts, exclusive waits, destination opening, authentication and publication.
Protected deletion rechecks after identity inventory before accepting its durable request;
accepted request recovery remains unchanged. Retired reads/known mutations cannot publish
into another root. One baseline shared-busy regression failed; 14 distinct new and 73
existing affected cases passed in focused continuations. Final strict Windows and complete
offline Android384AOT/408ELF/1608staticLOAD packages passed. A necessary no-reset private
upgrade preserved UID/full20source before preparation; PID5224 then refused a real old Add
prompt after actual root replacement with exact source/catalogue equality, and accepted
one fresh original profile creation plus both native deletion consents. New profile never
opened, Main/source20/account0/ledger0 preserved; envelope revision changes are expected.
Original OS/boot remained exact; freshUID/24hash-matchedXML/ownedAVD retired, fourCleans
28955files/2015040253logicalbytes and supplementary3711files/750087771logicalbytes removed.
A separate two-file D657 publication cleanup was blocked before process creation; no retry,
subset/alternate/move/escalation/reuse is allowed. Used signed packages/evidence remain.
Phone/D607/three deadlines/post-reboot unread source untouched; physical adoption, genuine
authentication/providers and broader DB/key/security/stability/speed/A11Y/iOS/Store/release
gates remain separate. D657 sole delayed CI response was pending and its schedule deleted;
no second query. D658 gets its own single delayed pushed-head check. [D-658 evidence](quality/original-profile-catalogue-lifetime.md)

### D-659 - Original active-resource workspace and owned parent return

All six original resource kinds retain their existing limits and writers. Original
read/revalidation uses one deferred SQLite transaction on a worker; page/root/frame/
disclosure and independent command ownership retain raw selections through native
consent, Retry and commit. One-use accepted receipts precede fallible Changed handlers.
An exact one-use native parent-return scope keeps the command lock held and corrects
the Shell guard blocking its own Pop; profile Leave uses the same scope, without a
new claim of native profile dirty-return acceptance. 20 new plus86 existing distinct
selected cases passed in focused continuations, with original failures preserved.
Final strict Windows and full offline APK384AOT/408ELF/1608staticLOAD/ZIP/signature pass.
Fresh API36/x64/4096 UID10216/PID4200 first proved old-root kind-consent refusal, one
account-empty commit with controlled Changed failure and six-kind reads. One budget-
empty commit exposed the actual blocked return; neither successful Save was replayed.
One necessary no-reset upgrade to7389 preserved all21 observed source parts; read-only
adoption did not reprepare. One different selected-budget payload changed only its
choice IdentitySet/Revision and reached ProductPlansPage with actual Pop/CanLeavefalse/
Cancelledfalse. A dirty original Back/Discard also returned with exact21source and no
Save. All20 financial/settings collections, account choice and ledger0 remained exact.
Original OS/boot/Home restored; freshUID/46XML2248787bytes hash-clean/AVD retired.
Six standardCleans43012files2831119963logicalbytes and12 supplementary targets4770files
881288496logicalbytes removed; used signed packages/source/evidence remain. The initial
shutdown console subcommand failed under adb shell; only that unfinished step was
corrected, without repeating completed retirement. All earlier refused scopes untouched.
Phone/D607/three deadlines/consumed reboot/post-reboot unread source remain protected.
Physical adoption, real tier/provider/authentication, complete DB/key/security/stability/
speed/A11Y/iOS/Store/release remain separate. D658 sole delayed CI and CodeQL green;
its schedule deleted, no second query. D659 has its own single delayed pushed-head CI.
[D-659 evidence](quality/original-active-resource-workspace.md)

### D-660 - Original personal synchronization workspace

Original connection/status/complete review relationships and first-run eligibility now use
one original-file worker transaction. Independent page/root/disclosure/input/session/row/
preview/Close ownership refuses retired continuations and retains literal nonsecret drafts.
Original writers revalidate around commit; known accepted results precede fallible follow-up,
with six-language explicit feedback and read-only Retry instead of successful-command replay.
52 distinct selected cases (20 new,32 affected existing), strict Windows/full offline Android
384AOT/408ELF/1608staticLOAD, original native consent/root/source/accepted receipt and all18
normal-font en/fa/de/Light/Dark/360/412.190476/680dp feedback contexts passed. Two independent
zero-account/ledger/plan local-binding cohorts each performed one distinct metadata Pause;
the first accepted operation was never replayed. All display contexts shared the second
accepted page/PID4720, exact20financial+9sync source and unchanged1notification/1failure/0remote.
Initial consent/TLS fixture/observer/theme failures are preserved; only unfinished cases
continued. Original OS/display/night/boot/Home restored; both fresh packages/AVDs and
51hash-matchedXML1609941bytes retired. FiveCleans 33175files/2457780345logicalbytes and bounded
supplementary15targets/14177files/1150915509logicalbytes completed; usedsigned/source/evidence kept,
earlier refused scopes untouched. Phone turn explicitly returned, no phone command; D607
three deadlines/consumed reboot/post-reboot unread source preserved. Real identity/provider,
physical adoption, fullDB/key/security/stability/speed/A11Y/iOS/Store/release remain separate.
D659 sole exact-head response was completed/success for all4workflows and its schedule
deleted; no second query. D660 receives one delayed exact-head CI check after push.
[D-660 evidence](quality/original-personal-sync-workspace.md)


### D-661 - Original household workspace

Original household connection/status and verified local metadata now use one captured
original-file worker transaction. Page/root/disclosure/literal-input/session/row/native
and file continuations retain independent ownership; returned primary acknowledgements
precede fallible follow-through, while unknown outcomes never receive a fabricated
receipt. Covered Retry reads the current original source without replaying accepted work.
Protected first-run completion changes only the oldest settings completion flag and its
UpdatedAt; a committed flag makes later Finish root-only, with no second Save or consent.

26 distinct selected cases (20 new, six affected existing), current strict Windows and
full signed offline Android384AOT/408ELF/1608staticLOAD pass. Actual original API/protected
Android owner setup and one accepted first-run commit with a subsequent Changed failure
passed without financial preparation. Same PID3873 retained 18 en/fa/de/Light/Dark/
360/412.190476/680dp error contexts, full source/metadata and one failure; XML text/native
bounds qualify these contexts, not the disqualified Activity-decor probe or glyph metrics.
Original Reload and root-only Finish reached actual Home with ledger/accounts empty;
20 source collections remained exact, normal foreground sync changed only LastSuccessAt.
OS/display/boot restored; fresh UID/41hash-matchedXML/AVD/API host/database/volume/two
networks retired, five original containers exact. Five standard Cleans passed; eight
supplementary scopes were rejected before process creation and remain frozen without
retry/subset/alternate/move/escalation/reuse. Used packages/evidence remain. No phone or
D607 command; three deadlines/consumed reboot/unread source protected. D660 sole CI result
is unknown after a preserved host marker/persistence failure, consumed and never queried
again. D661 receives one delayed exact-head check. WholeDB/key/security/stability/speed/
A11Y/iOS/genuine identity/provider/Store/release remain separate; overall goal active.

[Evidence](quality/original-household-workspace.md)
