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
database encryption is still absent; Windows and isolated Android API 36 x86_64 runtime proof pass, including
Keystore-wrapped fictitious profile recovery after process restart. Architecture acceptance awaits owner review.


D-69 / SEC-09 reviews the existing SDK and Android Release permission baseline with separate Offline/Cloud artifacts.
No Phase 1 finance or OS-backup behaviour changed; see the canonical backlog and SDK review for remaining gates.

D-70 / LOC-02 is a Phase 2 goal contribution reminder using the existing Phase 1 platform scheduler. Generic privacy,
contextual permission and app-lock navigation stay the same; there is no new ledger, schema or permission.

D-71 / LOC-03 adds a profile-level optional financial-period review reminder using the existing notification
permission, privacy and scheduling infrastructure. No Phase 1 ledger invariants or cloud/network capabilities change.

D-72 / LOC-04 completes the remaining aggregate-import overlap choices above the Phase 1 CSV pipeline. Explicit
choices, atomic stale-preview checks and durable Undo use an additive ImportLinks table; no automatic merge or
ledger invariant change. AT-79 local/emulator evidence and the phone/iOS gates live in the acceptance plan.

D-73 / QA-03 adds regression coverage around implemented Phase 1 onboarding, profile switching, application access,
widget/reminder navigation and theme policy. Explicit native ports keep actual flow sources testable without a MAUI
UI dependency. AT-80 is local engineering/runtime evidence; owner phone, real OAuth and iOS acceptance stay open.

D-74 / QA-04 adds 37 AT-81 cases for the existing entry-editor valued-asset consent, including cancellation before
mutation, busy command exclusion, translated native requests and real SQLite validation. No Phase 1 scope or data
model change. Main suite 1,300; native/phone acceptance remains separate.

D-75 / Q-02 hardening records explicit reference and tenfold performance workloads and keeps large entry reads
outside the UI thread. Account indexes call the original balance formula. AT-82 adds 23 cases (main suite 1,323);
actual native timing/ANR/search-row observations and remaining physical-device objectives are in
quality/performance-q02.md. No schema, commercial limit or release acceptance change.

D-76 / AT-83 covers the transaction page until snapshot publication, with disabled input and failure/retry feedback.
Twelve new application cases bring the main suite to 1,335. This follows the D-75 early-input finding without changing
financial semantics or closing native duration, ANR, physical-device or iOS acceptance.

D-83 / AT-90 maintains existing child-page headers and native Back descriptions without changing Phase 1 scope.
Actual running Windows/emulator evidence and the complete signed APK are in quality/font-scaling-a11y03.md;
main suite remains 1,345 passing. OS/screen-reader/device/iOS and remaining layout/draft findings stay open.

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
