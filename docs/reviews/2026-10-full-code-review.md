# Full code review – October 2026

Status: **In progress** – CR01 to CR04 done (2026-10-04). Owner: Shahram Vafadar.

After enhancement ZEX (phases 1–6), every line of the repository is read once more, in a fixed order, to find bugs,
performance problems and code that can be simpler or clearer, to check that every screen uses the best control for its
task, and to review the UI/UX. The review changes no product scope: it fixes and improves what exists.

## 1. Scope

About 57,000 lines of C# and XAML (generated code excluded):

| Area | Files | Lines |
|---|---|---|
| Shared libraries (`src/Libraries`) | 95 | 6,050 |
| Zanance core (`Vafadar.Zanance.Core`) | 75 | 10,630 |
| Zanance data (`Vafadar.Zanance.Data`, without the compiled model and migrations) | 10 | 2,690 |
| Zanance reports (`Vafadar.Zanance.Reports`) | 1 | 180 |
| Zanance app (`Vafadar.Zanance.App`) | 196 | 27,600 |
| Tests (`test`) | 103 | 10,200 |

Generated code (`CompiledModel`, migration designer files) is not read line by line; the migrations are checked for
data safety. Engineering scripts and the CI workflow are part of the last section.

## 2. What every section checks

1. **Bugs.** Wrong results, unhandled cases (empty data, one account, several currencies, Persian calendar, month start
   day, archived items, deleted references), rounding, time zones and "today", null handling, `async void` without a
   guard, races between loads, events not unsubscribed, resources not disposed, missing cancellation.
2. **Performance.** Repeated or N+1 database reads, loading all entries where a filtered query suffices, work in loops
   that can be done once, missing `AsNoTracking`, allocations in hot paths, layouts nested more than needed,
   `BindableLayout` over lists that can grow (no virtualisation), work on the UI thread, start-up time.
3. **Code quality.** Duplicated logic that belongs in one place, dead code, unclear names, missing or outdated
   comments, magic numbers, consistency with the rules in `AGENTS.md`.
4. **Controls.** For each input and list: is the control the best fit (for example `CollectionView` vs.
   `BindableLayout`, `SfComboBox` vs. `Picker`, chips vs. segmented control, `Switch` vs. `CheckBox`, numeric entry,
   date and time fields, charts vs. tables), on Android and Windows, with keyboard and screen reader.
5. **UI/UX.** Clarity of texts in English, Persian and German; help ("?") with examples where a term needs it; empty,
   loading and error states; right-to-left layout and mirrored icons; dark theme and contrast; touch targets of at
   least 44 px; screen reader names; narrow (360 px) and wide windows; Simple and Advanced mode.
6. **Tests.** Each section reviews its own tests: missing cases, tests that pass for the wrong reason, slow tests.

## 3. Sections, in order

The order goes from the bottom of the dependency graph to the screens, so that a fix below is in place before the code
above it is read.

| # | Section | Contents | Lines |
|---|---|---|---|
| CR01 | Shared libraries | `Vafadar.Core`, `Data`, `Localization`, `Maui`, `Documents(.Maui)`, `Backup(.GoogleDrive/.OneDrive)`, `Authentication(.Maui)` and their tests | 6,050 |
| CR02 | Core – money and ledger | `Money`, `Ledger`, `Accounts`, `Categories`, `Rates`, `Settings`, `Dashboard` | 3,160 |
| CR03 | Core – plans and budgets | `Plans`, `Reminders`, `Budgets`, `Forecasts`, `Receipts` | 3,110 |
| CR04 | Core – goals, holdings, reports, files | `Goals`, `Holdings`, `Reports`, `DataFiles`, and the PDF project `Vafadar.Zanance.Reports` | 4,540 |
| CR05 | Data layer | `ZananceStore`, `PlanStore`, `GoalStore`, `HoldingStore`, `AutoPostProcessor`, configurations, backup summary, migrations (data safety) | 2,690 |
| CR06 | App foundation | `App`, `AppShell`, `MauiProgram`, `Presentation`, styles and resources, `Platforms`, `Security`, `Reminders`, `Profiles`, `Diagnostics` | 4,970 |
| CR07 | Recording | Home, Transactions, entry editor and details, split, templates | 5,450 |
| CR08 | Accounts and holdings | Accounts, account detail and editor, loans, categories, rates, holdings | 4,630 |
| CR09 | Plans and budget | Plans, plan editor and details, occurrences, settlement, budget and budget editor | 3,890 |
| CR10 | Insights | Goals, forecast and snapshots, reports, KPI sheets, period review | 5,390 |
| CR11 | Settings and data | Settings, backup and restore, import/export, onboarding, profiles, More, About | 3,280 |
| CR12 | Cross-cutting UI/UX and release | Walk-through of every screen (en/fa/de, light/dark, 360 px and wide, Simple and Advanced), consistency of controls and texts across screens, accessibility, engineering scripts and CI | – |

## 4. How each section is done

1. Every file of the section is read completely; nothing is sampled.
2. Findings are recorded in §5 with an id (`CR07-03`), the file and line, the kind (bug, performance, quality,
   control, UI/UX, test) and the severity:
   * **High** – a wrong number, data loss or a crash;
   * **Medium** – a visible problem or a clear performance cost;
   * **Low** – clarity, consistency, small improvements.
3. Bugs and clear improvements are fixed in the section. Every fixed bug gets a regression test where it can be tested
   outside the UI. A change of behaviour or design the owner has not decided (for example replacing a control the
   owner knows, or changing a stored format) is listed as a question for the owner and only done after approval.
4. The section ends like a ZEX phase: the full test suite, the Release builds of the CI, a Windows walk-through of the
   changed screens (English and Persian, light and dark), the documents updated, commit and push, one CI check, and
   for sections that change the app (CR06–CR12) a signed APK for the owner's phone. The next section starts after the
   owner's check.

Out of scope: new features, package upgrades (asked separately), changes to the backup or CSV format without a
migration and the owner's approval.

## 5. Findings

Filled in section by section.

| Id | File | Kind | Severity | Finding | Resolution |
|---|---|---|---|---|---|
| CR01-01 | `Vafadar.Backup/BackupService.cs` | performance | Medium | The database snapshot, compression, checksums and PBKDF2 (600,000 iterations, twice for a restore: preview and restore) ran on the caller's thread – the UI thread in the app – and froze the screen on a phone. | Fixed: the package work runs on the thread pool; test `The_heavy_work_runs_off_the_callers_thread`. |
| CR01-02 | `Vafadar.Backup.GoogleDrive`, `Vafadar.Backup.OneDrive` (service registration) | bug | Medium | The HTTP clients kept the default timeout of 100 s; a whole backup in one request on a slow mobile connection failed. | Fixed: 10 minutes per transfer (`TransferTimeout`); one test per storage. |
| CR01-03 | `Vafadar.Maui/Controls/ChoiceChips.cs` | control, accessibility | Medium | Chips were tap gestures on a border: no keyboard focus, not announced as buttons, the chosen chip not announced. | Fixed: a transparent button over each chip (the app's overlay pattern); the chosen one is read as "…, selected" (`Common_ChipSelected`, three languages). |
| CR01-04 | `Vafadar.Maui/Controls/DateField.cs` | control, accessibility | Medium | The date field was a tap gesture on a border: no keyboard focus, not announced as a button. | Fixed: a transparent button over the field carries the date and the hint. |
| CR01-05 | `ChoiceChips.cs`, `DateField.cs` | UI | Low | Outline and text colours were set once; after a theme switch an open page kept the colours of the other theme. | Fixed: both follow `RequestedThemeChanged` while shown. |
| CR01-06 | `Vafadar.Documents/TextLayout.cs` | performance | Low | `Rows` recomputes the average centre of a row for every word (quadratic within one row). | No change: a row holds a few dozen words at most; the cost is not measurable. |
| CR01-07 | `Vafadar.Authentication.Maui/GoogleSignInService.cs` | quality | Low | A new `HttpClient` for the e-mail lookup and the revocation. | No change: called once per sign-in or sign-out. |
| CR01-08 | `Vafadar.Data/LocalDatabaseLocation.cs` | performance | Low | SQLite runs in rollback-journal mode; WAL would let reads run during a write. | No change: no contention measured (one user, short writes); WAL adds files that profile moves and deletion would have to handle. |
| CR01-09 | `Vafadar.Zanance.App/Presentation/AttachmentFiles.cs` | performance | Medium | Found while checking the OCR path: a photo is decoded at full size (about 48 MB for 12 MP) before it is scaled down to 1,600 px. | Moved to CR06 (app code). |
| CR02-01 | `Vafadar.Zanance.Core/Money/Currency.cs` | performance | Low | `MinorFactor` built the power of ten with LINQ on every access – for every amount formatted, parsed or converted. | Fixed: a table lookup. |
| CR02-02 | `Vafadar.Zanance.Core/Categories/CategoryRule.cs` | bug | Medium | Categorization rules ignored the Arabic forms of Yeh and Kaf (the transaction search already handled them): a rule typed on one Persian keyboard missed payees typed on another. | Fixed: rules and search share `Vafadar.Core.Text.SearchText`; test `Persian_letter_forms_and_half_spaces_do_not_hide_a_rule`. |
| CR02-03 | `Vafadar.Zanance.Core/Ledger/EntrySearch.cs` | UI/UX | Low | A search with a space ("میوه فروشی") did not find a payee written with a half-space ("میوه‌فروشی"), which is how most people type it. | Fixed: the half-space counts as a space when matching; tests in `EntryListTests` and `SearchTextTests`. |
| CR02-04 | `EntrySearch.cs`, `Accounts/Account.cs` | quality | Low | A misindented line and a missing blank line between properties. | Fixed. |
| CR02-05 | `Vafadar.Zanance.Core/Rates/RateTable.cs` | performance | Low | The rate of a date is found by scanning the sorted list. | No change: the scan starts at the newest rate and a profile has a few dozen rates. |
| CR02-06 | `Vafadar.Zanance.Core/Ledger/LedgerCalculator.cs` | performance | Low | Total balances read all entries once per account. | No change: within the Q-02 budget with 10,000 entries and 20 accounts (`PerformanceTests`). |
| CR02-07 | `Vafadar.Zanance.Core/Ledger/EntryTags.cs` | UI/UX | Low | Tags typed with Arabic and Persian letter forms ("كافه", "کافه") stay two tags. | No change: tags keep the user's text; search finds both. |
| CR03-01 | `Vafadar.Zanance.Core/Plans/Recurrence.cs` | performance | Medium | Every range of a plan was generated from its first date: a daily or weekly plan that started years ago produced every earlier date on each call (Home asks up to four times per plan for the next due item). | Fixed: daily and weekly rules start at the step of the range and keep their numbers; test `A_range_long_after_the_start_has_the_same_dates_and_numbers_as_counting_from_the_start`. |
| CR03-02 | `Vafadar.Zanance.Core/Forecasts/SnapshotComparison.cs` | performance | Medium | The actual balance of every day of a saved forecast read the whole ledger once per account (90 days × 20 accounts × 10,000 entries ≈ 18 million steps on the snapshot page). | Fixed: the first day from the ledger, later days by adding what happened on them; equality test and a measurement in `ZexPerformanceTests`. |
| CR03-03 | `Vafadar.Zanance.Core/Budgets/BudgetCalculator.cs` | quality | Low | Budget spending was summed without overflow check, unlike every other sum of the ledger (ZEX-S0104). | Fixed: checked; test. |
| CR03-04 | `Vafadar.Zanance.Core/Receipts/ReceiptParser.cs` | bug | Low | Persian total keywords ("جمع کل") were not recognised with the Arabic letter forms that PDFs and OCR often return, so a later line with "مبلغ" could be taken as the total. | Fixed: keywords match ي/ى/ك and words with or without a half-space; test. |
| CR03-05 | `Vafadar.Zanance.Core/Reminders/ReminderSnoozes.cs` | quality | Low | Snoozes are stored with reflection-based JSON. | No change: Release builds trim only assemblies marked trimmable (MAUI default), which the app's own are not. |
| CR03-06 | `Vafadar.Zanance.Core/Budgets/BudgetSuggestions.cs` | performance | Low | Each category suggestion reads all entries once per past period. | No change: about 50 passes over the entries, a few milliseconds with 10,000 entries. |
| CR04-01 | `Vafadar.Zanance.Core/Holdings/Quantities.cs` | bug | High | A typed quantity lost its decimal point when the point was also the language's group separator: in German "1.5" g became 15 g (the group separator was removed first). | Fixed: separators are read like amounts – the last of two kinds is the decimal one, a single one groups thousands only when it is the language's group separator before exactly three digits; badly grouped input is refused. Tests per language. |
| CR04-02 | `Vafadar.Zanance.Core/Holdings/HoldingsLedger.cs` | bug | High | The cost basis per gram multiplied the basis by one million in 64-bit integers and overflowed silently for holdings bought in rials (e.g. 100 g for 500 billion rials), showing a wrong price per gram. | Fixed: computed in decimal with the same truncation; test. |
| CR04-03 | `Vafadar.Zanance.Reports/PdfReport.cs` | bug | Medium | A table that ran down a page printed its rows under the disclaimer at the page foot. | Fixed: tables paginate above the reserved footer on every page; test `Rows_of_a_long_table_end_above_the_disclaimer_on_every_page` (failed on the old code). |
| CR04-04 | `Vafadar.Zanance.Core/DataFiles/CsvImport.cs` | performance | Medium | Every imported row looked its category up by translating the name of every category again (10,000 rows × 30 categories). | Fixed: the names are indexed once per import; same first-match rule. |
| CR04-05 | `Vafadar.Zanance.Core/Reports/KpiCatalog.cs` | performance | Low | The essential coverage rebuilt the same filtered entry list in each of its three months. | Fixed: built once. |
| CR04-06 | `Vafadar.Zanance.Core/DataFiles/CsvImport.cs` | quality | Low | The own CSV format names accounts; two accounts with the same name cannot be told apart and the first one is used. | No change here: the format is shared with spreadsheets and older files. CR08 checks whether the account editor prevents duplicate names. |
