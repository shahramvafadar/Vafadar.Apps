# 01 – Current state and gap map

Audit of `main` at `5d41d9d` (2026-10-07), working tree clean. Status values: **Implemented and verified** (automated
tests and, where it matters, a run of the app), **Implemented but unverified** (code exists, tests or device runs
missing), **Partial**, **Missing**, **Blocked**, **Deferred by owner**. A class, a TODO or "Done" in a README is not
evidence; each row names the implementation, the tests and the UI path. Backlog ids refer to
[04-backlog.md](04-backlog.md).

Maintenance update 2026-10-08 (D-62/D-63): optional backup protection and first-run restore are implemented. The app now has a device-wide four-digit PIN with 20 verifier/attempt/recovery tests and Windows UI checks, and an Android foreground screenshot choice. Android backup rules exclude only device-bound SecureStorage ciphertext; plaintext database backup and database encryption remain open. Historical rows below describe the 2026-10-07 audit and must not be used as current evidence for those changed items.

Verification refresh 2026-10-09 (D-72/D-73/D-74): aggregate import/Undo, bulk operations, profiles, onboarding,
widget routing, theme and app-access rows below now carry current local/emulator evidence (AT-79/80/81). Manual
valued-asset consent is covered by 37 application cases and actual installed Debug/Release flows. Main suite 1,300.
Other rows retain their dated audit scope; the canonical backlog and build-specific SDK review carry later status.
Physical-device, iOS, provider and release acceptance remain independent gates.

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
| Bulk operations | Implemented and locally/emulator verified (D-73) | `TransactionsViewModel`, `BulkTransactionsViewModel` | BulkTransactionTests, UndoTests, AT-80 | `transactions` | physical-device/iOS acceptance remains |
| Aggregated entries (ZEX-S0611) | Implemented and locally/emulator verified (D-72) | `Ledger/AggregatedEntries.cs`, import overlap/journal | Core/Data ImportAggregateTests, AT-79 | entry editor (Advanced), import preview/history | physical-device/iOS acceptance remains |
| Plans (frequencies, intervals, nth/last weekday, second day, weekend/holiday shift, contracts, reminders, auto-post, partial payments, settlement) | Implemented and verified | `Core/Plans`, `Data/AutoPostProcessor.cs` | RecurrenceTests, OccurrenceTests, WeekdayRuleTests, SecondDayTests, WeekendShiftTests, ContractReminderTests, AdvanceSettlementTests, PlanStoreTests | `plans`, `plan`, `occurrence`, `settlement` | discoverability (owner could not find plans) → ZCR-LOC-06 |
| Budgets (month/week/two weeks; limits, envelopes, flex; rollover; suggestions; financial month) | Implemented and verified | `Core/Budgets` | EnvelopeCalculatorTests, FlexCalculatorTests, BudgetRolloverTests, BudgetPeriodTests, BudgetSuggestionTests, FinancialMonthTests, HijriPeriodTests | `//insights/budget`, `budgeteditor` | — |
| Goals (balance, earmark, quantity; priority; contribution plans; trend/ETA; Home cards) | Implemented and verified (Home cards: snapshots only) | `Core/Goals`, `Data/GoalStore.cs` | GoalCalculatorTests, ZexPhase2GoalTests, ZexPhase5Tests | `//insights/goals`, `goal`, `goaldetail` | contribution reminders delivered locally/emulator (D-70, ZCR-LOC-02); physical-device gate remains |
| Period review | Implemented and verified | `PeriodReview.cs` | ZexPhase4ReportTests | `review` | optional reminder delivered locally/emulator (D-71, ZCR-LOC-03); physical-device gate remains |
| Holdings (mass/count, purity, locations, events, manual prices) | Implemented and locally/emulator verified; manual asset-account consent covered (D-74 / AT-81) | `Core/Holdings`, `Data/HoldingStore.cs` | ZexPhase3HoldingTests, ZexPhase3DataTests, ZexPhase3CsvTests, AssetEntryConfirmationTests (AT-81) | `holdings`, `holdingdetail`, `assetevent` | phone/iOS asset-consent acceptance open; "not a tax calculation" help text (ZEX-S0404) → ZCR-LOC-05 |
| Reports (KPIs, wealth history, commitments, liquidity), PDF | Implemented and verified | `Core/Reports`, `Reports/PdfReport.cs` | ZexPhase4KpiTests, ZexPhase4ReportTests, ReportCalculatorTests, PdfReportTests | `//insights/reports`, `kpi` | — |
| Forecast, scenarios, saved snapshots | Implemented and verified | `Core/Forecasts` | ForecastTests, ZexPhase5Tests, ZexPhase5DataTests | `//insights/forecast`, `snapshot` | — |
| Multi-currency (52 currencies), manual rates, display units, report/valuation currency | Implemented and verified; no online rates | `Core/Money`, `Core/Rates` | RateTableTests, DisplayUnitTests, MoneyAmountTests | `rates`, `displayunits` | more currencies → ZCR-LOC-07 |
| Receipt and PDF reading on device | Implemented and verified (parser); device quality unverified | `Vafadar.Documents.Maui`, `Core/Receipts` | ReceiptParserTests, Spanish/French/Italian receipt tests | Home "Receipt" | ZCR-QA-05 |
| CSV import/export (generic mapping) | Implemented and verified; no presets for other apps or bank files | `Core/DataFiles` | CsvTests, ZexPhase3CsvTests | `importexport` | ZCR-IMP-01..02 |
| Profiles (one database each) | Implemented and locally/emulator verified (D-73) | `App/Profiles/ProfileService.cs` | ProfileTests, LocalDatabaseLocationTests, AT-80 | `profiles` | physical-device/iOS acceptance remains |
| Home customization, Android quick-add widget, onboarding, themes | Implemented and locally/emulator verified (D-73) | `Dashboard/HomeLayout.cs`, `QuickAddWidget.cs`, `AppLinkRouter`, `Features/Onboarding`, `ThemeService.cs` | HomeLayoutTests, AppLinkTests, OnboardingTests, ThemeTests, AT-80 | Home, widget, first run, settings | physical-device/iOS acceptance remains |
| Reminders (due dates, contracts, snooze) | Implemented and verified at unit level; device check pending | `Core/Reminders`, `App/Reminders` | ReminderPlannerTests, ReminderSnoozeTests | — | ZCR-QA-01 |
| Localization (6 languages), 3 calendars, date input (D-59) | Implemented and verified | `Vafadar.Localization`, `DateField` | resource tests, DateFormatterTests, CalendarDatesTests | settings | 20+ languages → ZCR-LANG (wave 9) |
| Public holidays | Partial: Germany (nationwide) and Iran (lunar dates approximate) only | `PublicHolidays.cs` | PublicHolidayTests | plan editor | more regions with source and validity → ZCR-LOC-08 |
| Help "?", screen reader hints | Implemented and verified on Windows (D-58, D-60) | `HelpButton`, `*_A11yHint` | resource tests | everywhere | TalkBack/VoiceOver/200 % font → ZCR-A11Y-01..04 |

## 2. Security and data protection

| Item | Status | Evidence | Gap → backlog |
|---|---|---|---|
| Database encryption at rest | **Missing** | `Vafadar.Data/LocalDatabaseLocation.cs:76` (plain `DataSource`), no SQLCipher package; attachments are `byte[]` in the database (`EntryAttachment.cs`); `AppLockService.cs:9` says so (SEC-03) | ZCR-SEC-01..06 |
| OS device/cloud backup of the plaintext database | **Missing protection** | `AndroidManifest.xml:3` `allowBackup="true"` without `dataExtractionRules`; iOS default iCloud backup; disclosed in decision log §1 | ZCR-SEC-07 (owner decision OD-10) |
| App lock | Implemented and locally/emulator verified (D-63, D-73) | Device authenticator, AppLockService/native cover, optional four-digit PIN and durable attempt delays in SecureStorage | PinLockTests, AppLockTests, AT-69/80; PIN never accepts NotAvailable. Device authentication/physical phone/iOS remain open; encrypted-database password/key flow remains SEC-04 |
| Backup encryption | Implemented and verified | `Vafadar.Backup/Security/BackupEncryption.cs`: AES-256-GCM, PBKDF2-SHA256 600 000, salt, versioned header | BackupEncryptionTests, BackupServiceTests, ZexBackupRoundTripTests, ZexUpgradeTests |
| Safety copy before restore | Partial | `BackupService.CreateSafetyCopyAsync` passes no password → plaintext in `AppData/backups-safety` | ZCR-SEC-08 |
| Automatic backup | Missing | `BackupOptions.AutomaticBackupInterval` exists, nothing calls it | ZCR-LOC-09 |
| Cloud backup (Google Drive appdata, OneDrive app folder) | Implemented but unverified | `Vafadar.Backup.GoogleDrive`, `.OneDrive`, `Vafadar.Authentication*`; only in builds with client ids (`MauiProgram.cs:87-111`) | storage and sign-in tests exist; real OAuth clients and devices missing → ZCR-QA-02 (Blocked on owner's client ids) |
| Key storage | Partial (OAuth tokens and device-local PIN verifier/delays; no database key) | ProtectedValueStores, SecurePinStorage | database-key wrapping remains ZCR-SEC-03 |
| Network and telemetry | Partial | INTERNET removed only in Android Release without client ids; no analytics SDK; ML Kit's own logging in online builds not verified | ZCR-SEC-09 |
| Screen protection | Implemented: Android screenshots blocked by default, optional Settings choice; recents remain hidden. iOS cover; Windows screenshots allowed | MainActivity, ScreenProtection, D-63 | physical-device/iOS acceptance remains |
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

## 5. Documentation alignment audit (D-88)

The original 2026-10-07 audit found the outdated statements below. Corrections on 2026-10-09 are documentation
alignment with existing owner decisions and current source, not new product policy or external acceptance.

| Original contradiction | Current correction and evidence |
|---|---|
| Generic one-time Pro model used for Zanance | D-61 Free/Plus/Pro and local-only Plus Lifetime govern spec Sections 3/20/27.5/28, monetization/overview, roadmap and privacy inventory. Historical MON-02/03 stay explicitly superseded; store prices, quotas and purchases remain unimplemented/decision-gated. |
| PR-10 said brand/logo undecided | D-21/D-26 approved Zanance and its generated master assets; prices remain design/sandbox values. PR-09 retains D-63 proprietary/all-rights-reserved ownership and component-only third-party notices. |
| Spec Section 2 described interfaces-only/hidden cloud sign-in | Actual Authentication.Maui, CloudSignIn platform matrix and Google/Microsoft services exist. Providers require configured clients; real sign-in/upload/restore for released certificates/platforms remains AT-59. |
| Spec Section 3 described local profiles as future work | ProfileService already selects separate databases and backup sets (D-34). Profiles are local, share the device gate and do not imply family authorization. |
| Section 31 had old date/count and no ZEX/emulator evidence | Current Section 31 is dated 2026-10-09 and records ZEX, targeted native flows, 1,369 passing main tests/App.Tests 140, and D-87's complete APK. Earlier slice counts remain historical; physical-device/iOS/release acceptance remains open. |
| Generic blanket missing OAuth/Mac statements | AGENTS Section 12 and spec Section 28 require completed provider/certificate/platform registration, real acceptance and a verified Mac/Xcode/signing environment. OD-11 remains unconfirmed; source presence is not external completion. |
| Camera denied or permission-free on every platform | The maintained matrix already records D-38 capture. The root/starter matrices now agree with PermissionPrompts.TakePhotoAsync and iOS Info.plist: Android camera app without a Zanance camera permission; iOS contextual camera permission. |
| ZEX README said nothing implemented/awaiting implementation approval | Initial review-only state is dated as historical; current implementation points to the retained ZEX story table and canonical ZCR backlog. ZCR README likewise separates the original wave-0 review from later D-69 delivery. |
| Starter matrix denied sign-in/portable preferences or implied every backup encrypted | Current rows match optional configured backup sign-in, D-62 optional protection and MauiProgram's explicit SettingsBackupSource/LocalizationService.PortableKeys allowlist. Device security/credentials remain excluded. Offline/Cloud network and SDK scopes are explicit. |

The unchanged D-87 test/build/package logs support the retained engineering baseline; no new test count, app build
or device claim is introduced by this documentation-only slice. The focused assertion audit checks these edited
requirements, links and actual source/manifest anchors. Owner archive/data/secrets are untouched.

## 6. Commercial delivery update (2026-10-10, D-117..121)

The original 2026-10-07 missing-commerce assertions above are historical. ENT-01 is now implemented with approved
OD-03 Core policy, scope/capabilities/quotas and 76 cases. ENT-02 is in progress: actual-file account, template,
filter, Goal and whole Plan batch/split writes plus current-period budget Save/confirmed Replace have real SQLite
checks. Main suite 1,721/App.Tests 299 passes. Current registration remains inactive with no paid grant/paywall;
test builds are unrestricted. Explicit selected active/read-only definitions, future-period activation,
contribution/allocation/occurrence work, other resources/import/restore/native paths and activation remain open.
Catalog/purchase verification/backend/server roles/release acceptance are not delivered by these boundaries.
See [canonical backlog](04-backlog.md) and [budget evidence](../../quality/budget-write-policy.md).

D-122 extends current ENT-02 progress to holding direct writes and retained corrections/delete/Undo/import rights,
with atomic purchase/payment/fee/derived-price Save. Actual SQLite writer validation protects concurrent sales and
duplicate imports. Main 1,767/App.Tests 299 passes. No current quota activation or paid grant; selected read-only
classification and the remaining resource/operation/native paths are open:
[evidence](../../quality/holding-write-policy.md).

D-123 continues ENT-02 with retained earmark/release rights and contribution operations, plus one atomic
goal/pin/contribution editor Save. 39 added SQLite cases/main 1,806 pass. Current registration remains inactive;
selected read-only items, contribution delivery and remaining ENT-02/03/04 paths stay open:
[quality evidence](../../quality/goal-contribution-write-policy.md).

D-124 continues ENT-02 with exact-file database backup/recovery and retained import/Undo checks. Restore binds
the destination before input and migrates the same file, correcting an actual reproduced redirection defect.
28 added SQLite cases/main 1,834 pass. Current registration stays inactive; selected read-only import/restore data,
profiles, automation and remaining ENT-02/03 paths stay open: [quality evidence](../../quality/recovery-write-policy.md).

D-125 continues ENT-02 with actual-file category rule creation/correction/deletion rights and serialized
same-pattern replacement, including current unrestricted builds. 28 added SQLite cases/main 1,862 pass.
No stored transaction is reclassified. Registration stays inactive; suggestion delivery, selected read-only data,
profiles, automation and remaining ENT-02/03/04 paths stay open: [quality evidence](../../quality/category-rule-write-policy.md).

D-126 continues ENT-02 with actual-file ledger Save rights, complete split/holding/transfer groups and
final-batch refund validation. 45 added SQLite cases/main 1,907 pass. Four reproduced refund defects corrected;
entry/attachment/paid-total rollback retained. Registration inactive; ledger delete/Undo, selected read-only data,
automation and remaining ENT-02/03/04 paths stay open: [quality evidence](../../quality/ledger-write-policy.md).

D-127 continues ENT-02 with actual-file ledger Delete/Undo, explicit file-bound refund snapshots and
retry-safe application offers. 38 added cases/main 1,945 pass; App.Tests 308. Receipt orphan/purge behavior stays
unchanged, without extra byte copies. Registration inactive; selected read-only data, automation, commercial
feedback and remaining ENT-02/03/04 paths remain open: [quality evidence](../../quality/ledger-undo-write-policy.md).

D-128 reports actual transaction Undo/refresh failures through the existing translated dialog, preserving the
original retry deadline and blocking repeated invocation through pending feedback. Nine added actual-command cases;
main 1,954 / App.Tests 317 pass. Six normal installed Release failure flows retain committed financial data and
receipt bytes. No activation changes. Later refund edits during Undo and visible ledger Save feedback remain open:
[quality evidence](../../quality/transaction-undo-feedback.md).
