# 01 – Current state and gap map

Audit of `main` at `5d41d9d` (2026-10-07), working tree clean. Status values: **Implemented and verified** (automated
tests and, where it matters, a run of the app), **Implemented but unverified** (code exists, tests or device runs
missing), **Partial**, **Missing**, **Blocked**, **Deferred by owner**. A class, a TODO or "Done" in a README is not
evidence; each row names the implementation, the tests and the UI path. Backlog ids refer to
[04-backlog.md](04-backlog.md).

Maintenance update 2026-10-08 (D-62/D-63): optional backup protection and first-run restore are implemented. The app now has a device-wide four-digit PIN with 20 verifier/attempt/recovery tests and Windows UI checks, and an Android foreground screenshot choice. Android backup rules exclude only device-bound SecureStorage ciphertext; plaintext database backup and database encryption remain open. Historical rows below describe the 2026-10-07 audit and must not be used as current evidence for those changed items.

## 1. Product capabilities

All capabilities below are open to every user today. There is **no plan, quota, entitlement, purchase or paywall code**
anywhere in `src` (searched for entitlement, purchase, billing, Plus/Pro/Premium, paywall, quota, subscription, IAP);
the only gating is the display mode (`Core/Settings/FeaturePolicy.cs`, tested by `FeaturePolicyTests`,
`ZexModeSwitchTests`). Technical caps only: 10 tags per entry, 5 MB per attachment, 30 pending notifications, 600 loan
instalments, rollover of 24 months, financial month start 1–28.

| Capability | Status | Implementation | Tests | UI | Gap → backlog |
|---|---|---|---|---|---|
| Accounts (cash, checking, savings, credit card, loan, lent, asset; archive, opening balance, reconcile) | Implemented and verified | `Core/Accounts`, `Features/Accounts` | LoanTests, LoanCalculatorTests, ReconciliationTests, LedgerRulesTests, ZananceStoreTests | `accounts`, `accountdetail`, `loanschedule` | quota for Free → ZCR-ENT-04 |
| Entries (income, expense, transfer, refund, income reversal, adjustment), splits, reimbursable, tags, search, attachments, templates, rules, saved filters | Implemented and verified | `Core/Ledger`, `Features/Entries`, `Templates`, `Rules` | GoldenDataTests, SplitTests, ReimbursementTests, EntryTagTests, EntryTemplateTests, CategoryRuleTests | `entry`, `entrydetail`, `transactions`, `templates`, `rules` | — |
| Bulk operations | Implemented but unverified | `TransactionsViewModel` | none | `transactions` | ZCR-QA-03 |
| Aggregated entries (ZEX-S0611) | Partial | `Ledger/AggregatedEntries.cs` | ZexPhase4DataTests, ZexPhase4ReportTests | entry editor (Advanced) | import only warns about overlaps → ZCR-LOC-04 |
| Plans (frequencies, intervals, nth/last weekday, second day, weekend/holiday shift, contracts, reminders, auto-post, partial payments, settlement) | Implemented and verified | `Core/Plans`, `Data/AutoPostProcessor.cs` | RecurrenceTests, OccurrenceTests, WeekdayRuleTests, SecondDayTests, WeekendShiftTests, ContractReminderTests, AdvanceSettlementTests, PlanStoreTests | `plans`, `plan`, `occurrence`, `settlement` | discoverability (owner could not find plans) → ZCR-LOC-06 |
| Budgets (month/week/two weeks; limits, envelopes, flex; rollover; suggestions; financial month) | Implemented and verified | `Core/Budgets` | EnvelopeCalculatorTests, FlexCalculatorTests, BudgetRolloverTests, BudgetPeriodTests, BudgetSuggestionTests, FinancialMonthTests, HijriPeriodTests | `//insights/budget`, `budgeteditor` | — |
| Goals (balance, earmark, quantity; priority; contribution plans; trend/ETA; Home cards) | Implemented and verified (Home cards: snapshots only) | `Core/Goals`, `Data/GoalStore.cs` | GoalCalculatorTests, ZexPhase2GoalTests, ZexPhase5Tests | `//insights/goals`, `goal`, `goaldetail` | contribution reminders delivered locally/emulator (D-70, ZCR-LOC-02); physical-device gate remains |
| Period review | Implemented and verified | `PeriodReview.cs` | ZexPhase4ReportTests | `review` | optional review reminder missing (ZEX-S0610) → ZCR-LOC-03 |
| Holdings (mass/count, purity, locations, events, manual prices) | Implemented and verified; asset-account income/expense confirmation untested (ZEX-S0408) | `Core/Holdings`, `Data/HoldingStore.cs` | ZexPhase3HoldingTests, ZexPhase3DataTests, ZexPhase3CsvTests | `holdings`, `holdingdetail`, `assetevent` | ZCR-QA-04; "not a tax calculation" help text (ZEX-S0404) → ZCR-LOC-05 |
| Reports (KPIs, wealth history, commitments, liquidity), PDF | Implemented and verified | `Core/Reports`, `Reports/PdfReport.cs` | ZexPhase4KpiTests, ZexPhase4ReportTests, ReportCalculatorTests, PdfReportTests | `//insights/reports`, `kpi` | — |
| Forecast, scenarios, saved snapshots | Implemented and verified | `Core/Forecasts` | ForecastTests, ZexPhase5Tests, ZexPhase5DataTests | `//insights/forecast`, `snapshot` | — |
| Multi-currency (52 currencies), manual rates, display units, report/valuation currency | Implemented and verified; no online rates | `Core/Money`, `Core/Rates` | RateTableTests, DisplayUnitTests, MoneyAmountTests | `rates`, `displayunits` | more currencies → ZCR-LOC-07 |
| Receipt and PDF reading on device | Implemented and verified (parser); device quality unverified | `Vafadar.Documents.Maui`, `Core/Receipts` | ReceiptParserTests, Spanish/French/Italian receipt tests | Home "Receipt" | ZCR-QA-05 |
| CSV import/export (generic mapping) | Implemented and verified; no presets for other apps or bank files | `Core/DataFiles` | CsvTests, ZexPhase3CsvTests | `importexport` | ZCR-IMP-01..02 |
| Profiles (one database each) | Implemented but unverified | `App/Profiles/ProfileService.cs` | LocalDatabaseLocationTests only | `profiles` | ZCR-QA-03 |
| Home customization, Android quick-add widget, onboarding, themes | Implemented but unverified (no automated tests; snapshots) | `Dashboard/HomeLayout.cs` (tested), `QuickAddWidget.cs`, `Features/Onboarding`, `ThemeService.cs` | HomeLayoutTests only | Home, widget, first run, settings | ZCR-QA-03 |
| Reminders (due dates, contracts, snooze) | Implemented and verified at unit level; device check pending | `Core/Reminders`, `App/Reminders` | ReminderPlannerTests, ReminderSnoozeTests | — | ZCR-QA-01 |
| Localization (6 languages), 3 calendars, date input (D-59) | Implemented and verified | `Vafadar.Localization`, `DateField` | resource tests, DateFormatterTests, CalendarDatesTests | settings | 20+ languages → ZCR-LANG (wave 9) |
| Public holidays | Partial: Germany (nationwide) and Iran (lunar dates approximate) only | `PublicHolidays.cs` | PublicHolidayTests | plan editor | more regions with source and validity → ZCR-LOC-08 |
| Help "?", screen reader hints | Implemented and verified on Windows (D-58, D-60) | `HelpButton`, `*_A11yHint` | resource tests | everywhere | TalkBack/VoiceOver/200 % font → ZCR-A11Y-01..04 |

## 2. Security and data protection

| Item | Status | Evidence | Gap → backlog |
|---|---|---|---|
| Database encryption at rest | **Missing** | `Vafadar.Data/LocalDatabaseLocation.cs:76` (plain `DataSource`), no SQLCipher package; attachments are `byte[]` in the database (`EntryAttachment.cs`); `AppLockService.cs:9` says so (SEC-03) | ZCR-SEC-01..06 |
| OS device/cloud backup of the plaintext database | **Missing protection** | `AndroidManifest.xml:3` `allowBackup="true"` without `dataExtractionRules`; iOS default iCloud backup; disclosed in decision log §1 | ZCR-SEC-07 (owner decision OD-10) |
| App lock | Implemented but unverified | `Vafadar.Maui/Security/DeviceAuthenticator*` (device credential/biometrics, Class 2 accepted), `App/Security/AppLockService.cs` (fixed 30 s), `LockPage.cs:73` unlocks when no device lock exists (recovery path) | no app password, no tests → ZCR-SEC-04, ZCR-QA-03 |
| Backup encryption | Implemented and verified | `Vafadar.Backup/Security/BackupEncryption.cs`: AES-256-GCM, PBKDF2-SHA256 600 000, salt, versioned header | BackupEncryptionTests, BackupServiceTests, ZexBackupRoundTripTests, ZexUpgradeTests |
| Safety copy before restore | Partial | `BackupService.CreateSafetyCopyAsync` passes no password → plaintext in `AppData/backups-safety` | ZCR-SEC-08 |
| Automatic backup | Missing | `BackupOptions.AutomaticBackupInterval` exists, nothing calls it | ZCR-LOC-09 |
| Cloud backup (Google Drive appdata, OneDrive app folder) | Implemented but unverified | `Vafadar.Backup.GoogleDrive`, `.OneDrive`, `Vafadar.Authentication*`; only in builds with client ids (`MauiProgram.cs:87-111`) | storage and sign-in tests exist; real OAuth clients and devices missing → ZCR-QA-02 (Blocked on owner's client ids) |
| Key storage | Partial (OAuth tokens only) | `Vafadar.Authentication.Maui/ProtectedValueStores.cs` | needed for a database key → ZCR-SEC-03 |
| Network and telemetry | Partial | INTERNET removed only in Android Release without client ids; no analytics SDK; ML Kit's own logging in online builds not verified | ZCR-SEC-09 |
| Screen protection | Implemented (Android FLAG_SECURE, iOS cover); Windows none | `MainActivity.cs:22`, `MauiProgram.cs:76-82` | — |
| Temporary files | Implemented (best-effort deletion) | `SqliteDatabaseBackupSource.cs:110-127`, `AttachmentFiles.cs:62-83` | covered by ZCR-SEC-01 threat model |

## 3. Release, devices and platforms

| Item | Status | Source |
|---|---|---|
| Manual acceptance on a physical Android device (AT-01, 04, 34, 35, 37, 38, 49, 61; Q-02) | Blocked: needs the owner's phone runs | `07-acceptance-test-plan.md`, spec §31.5 |
| Upgrade (AT-60), reinstall/restore (AT-57) on the release build | Missing | `08-release-checklist.md` |
| Cloud backup with real OAuth clients (AT-59) on Android, iOS, Windows | Blocked: client ids | `08-release-checklist.md`, D-50 |
| iOS build, signing, WidgetKit | Blocked: Mac with Xcode and Apple account (to re-check with the owner) | AGENTS.md §12, D-30, CR12-16 |
| Device checks of the code review (large photo, dark lock screen, Android back in editors, TalkBack) | Missing | CR12-17, CR06-01 |
| Android emulator | Available (`pixel_7_-_api_36_0`, used 2026-10-06 for D-59) | — |
| Store listing: English/German/Persian texts still say three languages | Partial | `docs/store/listing.md` |
| Copy follow-ups: "Sign-in complete", "as an expense of {2}" | Missing | D-53, D-54 |
| Narrow no-break space on Android | Missing check | D-56 |
| App/view-model test project (ZEX-S0101) | Missing | no `Vafadar.Zanance.App.Tests` |

## 4. Not started (Phase 2B and commercial)

Entitlements, quotas, catalog, billing, trial, offers, Lifetime, paywall – Missing. Identity, backend, sync, shared
space, roles – Missing. AI, Tax, bank, online rates – Missing (decision gates). Additional 20+ languages – planned as
the last feature wave.

## 5. Outdated statements found (to correct in the touched documents)

* "Monetization: one-time Pro purchase" – spec §3, §20 (MON-02/03), §27.5, §28; `docs/architecture/monetization.md`;
  `docs/roadmap.md` Phase 4; privacy matrices – superseded by D-61 (this enhancement).
* Spec §1.2 PR-10 (brand undecided), §2 (cloud backup "hidden/interfaces only"), §3 (profiles "future") – superseded
  by D-21/D-26, D-35/D-50, D-34.
* Spec §31 (updated 2026-09-29, test count 491, ZEX not mentioned, "no physical device run") – to be refreshed in the
  documentation section ZCR-GOV-02.
* "No Mac" / "OAuth not configured" in AGENTS.md §12, roadmap, D-50, enhancement ZEX docs – kept as the current
  state until the owner confirms otherwise (OD-11).
* `docs/privacy/privacy-matrix.md`: "camera ❌" – camera path exists since D-38.
* ZEX README line 3/79 ("nothing implemented") contradicts its backlog state table.
