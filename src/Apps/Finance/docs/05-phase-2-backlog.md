# 05 – Phase 2 backlog

Nothing here was built in phase 1, not even behind a disabled flag (HAND-05). Phase 2A started on 2026-09-26; finished items are marked below. The phase-1 model keeps each item
possible (SC-01).

## Phase 2A – local, in suggested order

| Item | Requirements | Model hook already present |
|---|---|---|
| Savings goals, sinking funds, rollover, envelope/flex budgets | F2-GOAL-01..05, BUD-11/12, §10.3 | **P2-1 done:** goals, earmarks per account, coverage and shortfall by priority, suggested contribution (Goals page in More). **P2-2 done:** budget rollover (surplus, or surplus and deficit; consecutive months, max 24). Envelope/flex budgets still open |
| Split transactions, partial payments of an occurrence | F2-TX-01/02 | **P2-3 done:** splits as grouped entries that add up exactly (split editor from the entry details; join again). **P2-4 done:** partial payments of an occurrence (open with the outstanding rest; the final payment settles; overpayment shows as a larger actual amount) |
| Reimbursable expenses, tags, attachments, bulk operations, rules | F2-TX-03/04 | **P2-6 done:** reimbursable expenses (F2-TX-03). **P2-7 done:** tags with search and a tag report. **P2-8 done:** categorization rules for new entries and generic imports. **P2-11 done:** bulk operations in the transaction list. **P2-17 done:** receipt photos and PDF files attached to entries (system file picker, no permission; photos scaled to 1600 px JPEG; 5 MB limit; in the database and backups, never in CSV) |
| Contracts, price changes, cancellation deadlines, business-day rules | F2-CON-01..05 | **P2-5 done:** contract details, reminders for the last day to cancel and the review date, Home attention (F2-CON-01/03); price changes via "this and future" (F2-CON-02). Weekend rule for due dates (F2-CON-05, weekends only). Final settlement of advance payments (F2-CON-04). Second day per month for monthly plans. Holiday calendars still open |
| Debts, loans, assets | F2-DEBT-01/02, F2-ASSET-01/02 | **P2-9 done:** loan and money-lent accounts with counterparty, separate from the liquid total, repayment shortcut (F2-DEBT-01). Asset accounts with manual valuation (F2-ASSET-01). **P2-18 done:** optional rate and installment, estimated principal/interest split, payoff and schedule, installment posted as principal transfer plus interest entry (F2-DEBT-02); labelled as an estimate, never as the contract |
| Several days per month, weekday rules, holidays | REC-05, REC-12 | **Done:** a second day per month (REC-05). **P2-19 done:** the n-th or last weekday of the month for monthly and yearly plans (REC-12). Public holidays still open (needs a data source) |
| Dark theme, widgets, saved reports, PDF, OCR, scenarios | §21.5, FOR-10, REP-07/08 | **P2-10 done:** dark theme (D-22). PDF report of a period with the Vazirmatn font (REP-07); widgets, saved reports, OCR, scenarios still open |
| Unofficial currency units (e.g. Toman) | FX-07 | **P2-20 done:** user-defined display units (currency, name, factor 10^1..10^6) for display and input; stored amounts, CSV and rates stay in the ISO currency; a one-tap toman choice, never applied automatically |
| Multiple local profiles | §3 | One database per profile possible |

## Phase 2B – online, each with its own decision gate

| Item | Gate before any work |
|---|---|
| Real sync | Hosting, identity, encryption, conflict rules approved (F2-SYNC-01..04) |
| Household / sharing | Server-side authorization model (F2-SHARE-01..05) |
| Bank connection | Provider, cost, markets, read-only (F2-BANK-01..04) |
| AI | Official provider access without user API keys; data minimisation (F2-AI-01..09) |
| Online exchange rates | Source, cost, caching (FX-08) |
| Pro purchase, support link, ads | Store policy review, restore of purchases (MON-01..08) |
