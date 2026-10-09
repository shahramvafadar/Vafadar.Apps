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
