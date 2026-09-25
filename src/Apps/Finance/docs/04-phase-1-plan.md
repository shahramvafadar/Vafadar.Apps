# 04 – Phase 1 implementation plan

Each slice is a usable vertical increment: domain + persistence + UI + tests, committed separately. A slice is done
only when its acceptance scenarios pass; status is tracked in the table at the end.

## Phase 1A – everyday ledger

| Slice | Goal | Scope | Acceptance | Migration / recovery risk |
|---|---|---|---|---|
| S1 Ledger core | Correct numbers before any UI | Money/Currency, entities, entry kinds, balance/report/budget calculators, first EF migration, repositories | AT-02, 05–08, 11–15, 39–40, 45, 47, **62 golden** (unit tests) | First migration defines the schema; later changes additive only |
| S2 Shell, onboarding, accounts | Start without login | New navigation (D-11), light theme, placeholder branding, onboarding, accounts list/editor/archive, finance settings | AT-01, 09, 10 | Settings in DB (part of backups) |
| S3 Entry editor and list | Daily recording | Categories (defaults + custom), icon font, entry editor (expense/income/transfer, foreign amount, fee), list with search/filters, detail, refund, duplicate, delete with undo | AT-02–08, 12–15, 48 (manual) | Double-save guard (AT-03) |
| S4 Plans core | Plans ≠ ledger | Recurrence engine (Gregorian/Persian), schedules, occurrences, plan editor with preview, review/confirm/skip/move/link, split "this and future", auto-post processor | AT-16–33 | Unique indexes (D-06/D-07) |
| S5 Home dashboard | Explainable overview | Home cards with one filter set, needs-review, next due, donut | AT-49 (partly), 50 | – |
| S6 Local backup UI | Data survives | Settings in package, safety copy before restore, encrypted file export/import, preview, fresh-install restore | AT-56–58, 60 | Restore replaces DB atomically |

**Gate 1A:** AT-62 exact, reliable save/edit, no transfer double counting, plans vs ledger clear, local restore proven.

## Phase 1B – complete first product

| Slice | Scope | Acceptance |
|---|---|---|
| S7 Budget | Monthly total + category limits, scope, alerts 80/100 %, copy to next month, budget calendar | AT-39–41 |
| S8 Reports | All phase-1 reports with drill-down, incomplete labels | AT-13, 46, 50 |
| S9 Forecast | Horizon end of month / 30 / 90 days, path + minimum, assumptions | AT-42–44 |
| S10 Reminders | Local notifications, permission on demand, privacy text, rescheduling, summary | AT-34–38 |
| S11 Multi-currency | Manual rates, report currency, incomplete totals | AT-45–47 |
| S12 Import/Export | CSV export (safe), CSV import with mapping, preview, duplicates, batch undo | AT-51–55 |
| S13 Simple/Advanced + app lock | Mode switch preserving data, device-credential/biometric lock, lock on resume/notification/export | AT-49, 61 |
| S14 Hardening | Localization completeness, accessibility pass, performance with 10,000 entries, release build tests | AT-48, Q-02 |
| S15 Cloud backup (conditional) | Only when real Google/Microsoft sign-in is configured and verified | AT-59 |

**Gate 1B:** every phase-1 scenario passes on the release build (except explicitly unshipped cloud destinations);
privacy policy, Data safety and Financial features declaration match that build.

## Status

| Slice | Status |
|---|---|
| S1 | Implemented – verified (domain + persistence; 59 new tests; migration InitialLedger) |
| S2 | Implemented – verified (Android/Windows builds; en/de/fa screens reviewed via Debug snapshots; AT-10 history part unit-tested, plan part follows S4) |
| S3 | Implemented – verified (entry editor, list with search/filters, detail, refund, duplicate, delete with undo, categories; 27 new tests; en/de/fa snapshots reviewed) |
| S4 | Implemented – verified (recurrence engine Gregorian/Persian, plans, occurrences, review/confirm/link/skip/move, "this and future" split, pause/resume/end, auto-post processor; 37 new tests; en/de/fa snapshots) |
| S5 | Implemented – verified (scope header, recorded balance, attention card, period income/expense/result, next due, expense donut with table, drill-down to entries with the same filter; AT-50 unit-tested) |
| S6 | Planned |
| S7–S15 | Planned |
