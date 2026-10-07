# 05 – Phase 2 backlog

> **Since 2026-10-07 the state of all remaining work is tracked only in the canonical backlog of enhancement ZCR:
> [enhancements/2026-10-commercial-release/04-backlog.md](enhancements/2026-10-commercial-release/04-backlog.md).**
> This file keeps the history of Phase 2A; the accessibility pass and the Phase 2B items below continue there (ZCR-A11Y,
> ZCR-SYNC, ZCR-SHR, ZCR-BANK, ZCR-AI, ZCR-FX, ZCR-BIL).

Nothing here was built in phase 1, not even behind a disabled flag (HAND-05). Phase 2A started on 2026-09-26; finished items are marked below. The phase-1 model keeps each item
possible (SC-01).

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
