# 04 – Phase 1 implementation plan

Each slice is a usable vertical increment: domain + persistence + UI + tests, committed separately. A slice is done
only when its acceptance scenarios pass; status is tracked in the table at the end.

D-62 maintenance (2026-10-08): S2 offers theme/experience choices and existing-backup restore without a new
account; S6 allows optional password protection for local and cloud destinations. Tests cover protected and
unprotected packages, preserved preferences, no duplicate account and an empty profile. Cloud sign-in and
physical-device gates remain open; current tracking is ZCR-LOC-11.

## Phase 1A – everyday ledger

| Slice | Goal | Scope | Acceptance | Migration / recovery risk |
|---|---|---|---|---|
| S1 Ledger core | Correct numbers before any UI | Money/Currency, entities, entry kinds, balance/report/budget calculators, first EF migration, repositories | AT-02, 05–08, 11–15, 39–40, 45, 47, **62 golden** (unit tests) | First migration defines the schema; later changes additive only |
| S2 Shell, onboarding, accounts | Start without login | New navigation (D-11), light theme (dark added by D-22), branding (final: D-26), onboarding, accounts list/editor/archive, finance settings | AT-01, 09, 10 | Settings in DB (part of backups) |
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
| S15 Cloud backup (conditional) | Implemented (D-35); released only when the OAuth clients are configured and the device test passes | AT-59 |

**Gate 1B:** every phase-1 scenario passes on the release build (except explicitly unshipped cloud destinations);
privacy policy, Data safety and Financial features declaration match that build.

## Status

| Slice | Status |
|---|---|
| S1 | Implemented – verified (domain + persistence; 59 new tests; migration InitialLedger) |
| S2 | Implemented – verified (Android/Windows builds; en/de/fa screens reviewed via Debug snapshots; AT-10 unit-tested: history and ending the active plans) |
| S3 | Implemented – verified (entry editor, list with search/filters, detail, refund, duplicate, delete with undo, categories; 27 new tests; en/de/fa snapshots reviewed) |
| S4 | Implemented – verified (recurrence engine Gregorian/Persian, plans, occurrences, review/confirm/link/skip/move, "this and future" split, pause/resume/end, auto-post processor; 37 new tests; en/de/fa snapshots) |
| S5 | Implemented – verified (scope header, recorded balance, attention card, period income/expense/result, next due, expense donut with table, drill-down to entries with the same filter; AT-50 unit-tested) |
| S6 | Implemented – verified (encrypted backup file shared to any destination, last 10 kept on device, restore from file or device with password check, preview with counts, safety copy, auto-post after restore; AT-56/57 integration-tested) |
| S7 | Implemented – verified (monthly budget per calendar and currency, overall and category limits, zero/over/no-budget states, confirmed-only switch, plans of the month with exact occurrence counts and monthly equivalents, copy to next month, Home card; alerts delivered with S10) |
| S8 | Implemented – verified (expenses by category with gross chart, refunds card and net table; income and expense with savings share; 6/12-month trend with partial months marked; account movement from opening to closing balance; plan vs actual; drill-down with the same period and scope) |
| S9 | Implemented – verified (end of month / 30 / 90 days, start from recorded balance, open occurrences only, overdue assumed at base date, future-dated entries, unknown amounts mark the result incomplete, daily path with minimum and shortfall warning; AT-42–44 unit-tested) |
| S10 | Implemented – verified (reminder planner: future reminders only, capped, stable ids; rebuilt on start, resume, every change, restore and language change; generic lock-screen text unless details are allowed; permission on demand; tap opens the occurrence; budget alerts once per level; Windows keeps the in-app due centre) |
| S11 | Implemented – verified (manual rates with date and estimate flag, latest rate on or before a date, inverse rates, combined balance on Home only when every rate exists with the rate date shown, missing rates listed; recorded amounts, budgets and history never converted) |
| S12 | Implemented – verified (CSV export with ISO dates, invariant amounts, transfers as one row, optional notes, formula neutralisation and a sensitivity warning; import of own files by id without duplicates and of other files with explicit column, date, calendar, decimal and sign choices; preview with invalid lines, possible duplicates and pre-opening rows; atomic batch with undo; AT-51–55 tested) |
| S13 | Implemented – verified on Windows/Android builds (Simple/Advanced as views of the same data: advanced plan, budget and entry options, Home forecast card, summaries of hidden settings; app lock with device biometrics or credential via a shared library authenticator, cover on leaving, 30 s grace, notification taps after unlock, confirmation for export/backup/restore, Android FLAG_SECURE); device checks pending |
| S14 | Implemented – verified (resource completeness, keys used in code exist, no empty translations; 10,000-entry calculation budget; migration upgrade test; licence list; release checklist updated). Device accessibility and performance runs remain for the release (08) |
| S16 Spec review | Implemented – verified (gaps found by comparing with the English specification v1.1: account detail and reconciliation, archive ends plans, icons for accounts and entries, income reversal and "make recurring", category order and merge, budget account scope, category filter, delete all data, forecast what-if, reminder summary and due-date reminder, plan report variance, Home guidance, Q-02 data set, quick templates, unknown opening balance with data-quality indicator, region and first day of the week in the shared localization library, notification snooze; remaining deviations in spec Section 31.3) |
| S15 | Implemented – unverified (D-35, D-50): OneDrive and Google Drive on Android, iOS and Windows in builds with OAuth client ids; the device test with real clients is still open, and builds without clients keep the offline behaviour (encrypted backup files cover BAK-01..12) |

D-63 privacy maintenance: device-wide four-digit PIN, durable attempt limiting, authenticated recovery and Android
screenshot choice are implemented in ZCR-SEC-10. Broader database/key encryption and device acceptance stay open.

## D-65 maintenance

S2 account setup and S4 plan setup have clearer debt direction, optional terms and date-anchored recurrence controls.
The data/settlement model is unchanged. Local validation is recorded under AT-72 / ZCR-LOC-13; physical-device
acceptance remains open. This maintenance does not reopen or approve another commercial section.

D-67 maintenance: automatic connected-backup discovery with destination feedback and cross-profile restore lists;
independent regional formats/digits/holidays in first run and settings, with portable display choices. Local validation
uses AT-73/74; real-provider and physical-device acceptance remain open in S15/AT-59.


SEC-01 research (D-68, 2026-10-08) prepares ADR 0010 and an independent fictitious encryption harness. Production
database encryption is still absent; Windows proof passes, Android runtime and architecture acceptance are pending.
