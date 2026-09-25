# 05 – Phase 2 backlog

Nothing here is built in phase 1, not even behind a disabled flag (HAND-05). The phase-1 model keeps each item
possible (SC-01).

## Phase 2A – local, in suggested order

| Item | Requirements | Model hook already present |
|---|---|---|
| Savings goals, sinking funds, rollover, envelope/flex budgets | F2-GOAL-01..05, BUD-11/12, §10.3 | Budgets are per period with explicit currency |
| Split transactions, partial payments of an occurrence | F2-TX-01/02 | Entries have ids; occurrence ↔ entry link can become 1:n |
| Reimbursable expenses, tags, attachments, bulk operations, rules | F2-TX-03/04 | Payee and note already separate |
| Contracts, price changes, cancellation deadlines, business-day rules | F2-CON-01..05 | Series split (D-08) keeps history |
| Debts, loans, assets | F2-DEBT-01/02, F2-ASSET-01/02 | Account types enumerated |
| Several days per month, weekday rules, holidays | REC-05, REC-12 | Rule is a value object |
| Dark theme, widgets, saved reports, PDF, OCR, scenarios | §21.5, FOR-10, REP-07/08 | Colour tokens (03 §6) |
| Unofficial currency units (e.g. Toman) | FX-07 | Currency table |
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
