# Full code review – October 2026

Status: **Done** – CR01 to CR12 (2026-10-04). Owner: Shahram Vafadar.

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
| CR05-01 | `Vafadar.Zanance.Data/GoalStore.cs` | bug | High | Saving an existing contribution plan dropped its assumed price: a price changed or removed on the goal page was silently lost (the existing test only covered the first save). | Fixed: the price is copied with the other fields; test `A_changed_assumed_price_of_an_existing_plan_is_kept`. |
| CR05-02 | `Vafadar.Zanance.Data/ZananceStore.cs` (`MergeCategoryAsync`) | bug | Medium | Merging categories moved entries, plans, templates, rules and budget limits, but not the categories of a goal's spending cut or of saved filters; they kept pointing to the archived category. | Fixed: both lists move to the target in the same transaction; test. |
| CR05-03 | `ZananceStore.EnsureDefaultCategoriesAsync` | performance | Low | Raised `Changed` even when nothing was added, so the reminders were rebuilt for nothing. | Fixed: only when categories were added; test. |
| CR05-04 | `ZananceStore.MoveCategoryAsync` | quality | Low | The new sort order used a minimum recomputed inside the renumbering loop (correct only by accident). | Fixed: computed once. |
| CR05-05 | `ZananceStore.cs`, `Configurations/EntityConfigurations.cs` | quality | Low | A closing brace on the same line and two misindented lines. | Fixed (no model change; the compiled-model test passes). |
| CR05-06 | `Vafadar.Zanance.Data/PlanStore.cs` (`UnsettleAsync`) | review | – | Reopening deletes the plan's entry and then updates the state in a second transaction; reported as a risk of a "settled" occurrence without an entry. | Corrected after the review: `DeleteEntryAsync` already reopens the occurrence (open, no entry, no automatic posting) in the same transaction as the deletion; the second step only writes the same values again, and the unlink case is one transaction. No issue. |
| CR05-07 | `Migrations/2026100*` | data safety | – | The ZEX migrations only add columns and tables; data updates are scoped (default currency, usable accounts, essential categories). | No issue; covered by `ZexUpgradeTests`. |
| CR06-01 | `Vafadar.Zanance.App/Presentation/AttachmentFiles.cs` | performance | Medium | (CR01-09) On Android a photo was decoded at full size before scaling; a 50-megapixel photo needs about 200 MB and can end the app. | Fixed: the size is read first and the photo decoded at a power-of-two reduction that still leaves at least 1,600 px. To be checked on the phone with the APK (a large camera photo). |
| CR06-02 | `Presentation/IconPicker.cs` | control, accessibility | Medium | Icons were tap gestures on borders: no keyboard focus, read by their English key ("BuildingBank", "default"), selection not announced. | Fixed: the overlay-button pattern; tiles are named like the category editor's ("Icon 12", `Icon_Default`) and the chosen one is read as "…, selected". |
| CR06-03 | `Presentation/IconPicker.cs` | UI | Low | The chosen icon was marked in the income green, against the colour rule (green = money in), and the colours did not follow a theme switch. | Fixed: the soft action blue with a blue outline, as dynamic resources. A first attempt showed no mark at all: in MAUI 10 a dynamic resource set from code does not override a value set from code before, so the old value is cleared first (checked in both themes). |
| CR06-04 | `Security/LockPage.cs` | UI/UX | Medium | The Unlock button had white text on the light blue of the dark theme (low contrast), the cover kept the theme of the moment it was created, and a cancelled or failed unlock showed no reaction. | Fixed: palette colours as dynamic resources (`OnPrimary` text); `Lock_NotUnlocked` is shown and announced after a failed attempt. |
| CR06-05 | `Security/AppLockService.cs` | bug | Medium | Links and rebuilds waiting for the unlock ran one after another without a guard: one failing navigation stopped the rest and its exception reached the lock page's `async void` handler, which ends the app. | Fixed: each waiting action is guarded; failures are logged. |
| CR06-06 | `Presentation/Palette.cs`, `Presentation/CategoryLookup.cs` | performance | Low | Every palette colour and category colour was parsed from text on each use – once per row and chart point. | Fixed: palette colours are parsed once per theme; category display colours are cached per value and theme. |
| CR06-07 | `Reminders/ReminderService.cs` | performance, quality | Low | The budget check read all categories once per budget; the debounce timer was replaced without being released and without guarding against two changes at the same moment. | Fixed: categories read once per update; the timer is swapped atomically and the old one disposed. |
| CR06-08 | `Resources/Styles/Zanance.xaml` | quality | Low | Two comments with a section sign and a dash stored with the wrong encoding, a comment above the wrong style, and a `ContentPage` style repeating the `Page` style of `Styles.xaml`. | Fixed; page backgrounds checked in both themes. |
| CR06-09 | `App.xaml.cs` | quality | Low | `RunForegroundWork` awaits `RefreshAsync` without its own guard. | No change: `RefreshAsync` catches every failure itself; a missing blank line in the file was fixed. |
| CR06-10 | `Platforms`, `Profiles/ProfileService.cs`, `Reminders/LocalNotificationScheduler.cs` | review | – | Secure window flag before the first frame, iOS privacy cover, widget intents (immutable), camera file provider, per-app language, profile switch under the posting lock, inexact reminders. | No issue found. |
| CR06-11 | `Features/Categories/CategoryEditorViewModel.cs` | quality | Low | The category editor keeps its own copy of the icon list (without Wallet and Payment). | Moved to CR11 (categories); descriptive translated icon names, if wanted, go to CR12. |
| CR06-12 | `Vafadar.Localization/Formatting/NativeDigits.cs` | UI/UX | Medium | Seen in the snapshots: with Persian digits, 4,500.00 is shown as "۴٬۵۰۰٫۰۰", and in the app font the Arabic decimal and thousands separators look almost the same – the ambiguity `CultureFactory` avoids for Latin digits. | Moved to CR12 (cross-cutting number display). |
| CR07-01 | `Features/Entries/EntryDetailViewModel.cs` | bug, UI/UX | High | Delete on the entry details asked nothing because the transaction list offers Undo – but the details are also opened from Home, accounts, plans and refunds, where no Undo is shown: the entry was gone without a question or a way back. | Fixed: without the list behind the page, Delete asks first (`Entry_DeleteQuestion`, with title and amount). A shared undo bar for every page goes to CR12. |
| CR07-02 | `Features/Entries/EntryEditorViewModel.cs` | bug | Medium | Unsaved-change detection ignored tags, reimbursement, aggregate range and destination fee: changing only those and pressing Cancel or Back discarded them without asking. | Fixed: every editable field is part of the comparison. |
| CR07-03 | `EntryEditorViewModel.SaveAsync` | bug | Medium | A failure while leaving the page after a successful save was reported as "could not be saved" (inviting a second save), and an invalid destination fee was found only after the entry and its fee had been changed in memory. | Fixed: navigation runs after the save and is guarded on its own; the destination fee is checked with the other input before anything changes. |
| CR07-04 | `EntryEditorViewModel` (aggregated entry) | bug | Medium | A new aggregated entry covered the Gregorian calendar month, not the financial month (Persian calendar, month start day) like every other period. | Fixed: `PeriodMath` with the user's calendar and start day. |
| CR07-05 | `Features/Home/HomeViewModel.cs` | bug, performance | Medium | Appearing and a period change could load at the same time and mix their rows; one load read the plans four times, the states and categories twice and computed the balances twice. | Fixed: loads run one after the other; plans, states and categories are read once and passed on. |
| CR07-06 | `Features/Transactions/TransactionsViewModel.cs` | performance | Medium | Each filter change cleared the day groups and added them one by one, so the list laid out once per day (slow for "All" with thousands of entries). | Fixed: the grouped list is built and set at once. |
| CR07-07 | `Vafadar.Zanance.Data/ZananceStore.cs`, `TransactionsViewModel.BulkDeleteAsync` | performance | Medium | Bulk delete ran one transaction and one change notification per entry (every other screen and the reminders refreshed each time). | Fixed: `DeleteEntriesAsync` deletes the selection with the same rules in one transaction and notifies once; tests `A_bulk_delete_removes_the_selection_with_its_groups_in_one_change_and_can_be_undone`, `A_bulk_delete_of_nothing_changes_nothing`. |
| CR07-08 | `TransactionsViewModel.BulkCategoryAsync` | bug | Low | The category list for a bulk change named sub-categories without their main category; two with the same name ("Other") could not be told apart and the first was taken. | Fixed: "Main › Sub" like the entry editor. |
| CR07-09 | `EntryDetailViewModel`, `SplitEditorViewModel` | performance | Low | Opening the details or the split editor read the whole ledger to find refunds of the entry. | Fixed: only the refunds of the entry or its parts are read; the transfer fee comes from the group already loaded. |
| CR07-10 | `EntryDetailViewModel.OpenAttachmentAsync`, `Presentation/AttachmentFiles.cs` | UI/UX | Low | Opening an attachment without an app for its type did nothing (or could fail the command). | Fixed: `Attachment_OpenFailed` is shown. |
| CR07-11 | `SplitEditorViewModel.JoinAsync` | UI/UX | Low | A refused join left the page without a word. | Fixed: the reasons are shown like for a refused split. |
| CR07-12 | `EntryEditorPage.xaml`, `EntryDetailPage.xaml`, `SplitEditorPage.xaml`, `TransactionsPage.xaml`, `TemplatesPage.xaml`, `HomeLayoutPage.xaml`, `HomePage.xaml` | control, accessibility | Low | Text buttons styled by hand (some below 44 pt), two switches without a name, Delete and Read on attachments and Delete on templates without the item's name for screen readers, a recent-entry row without its amount, a clipped tag suggestion pill. | Fixed: `TextButton` style, names and hints; checked in the snapshots. |
| CR07-13 | `EntryDetailViewModel` (`DetailLine`) | UI | Low | The "From a plan" line had an empty label that still took a line. | Fixed: the label is optional. |
| CR07-14 | Recording screens | review | – | Persian amounts inside sentences (e.g. the save effect) keep the sign in front – checked magnified; receipt reading, templates and the reimbursement list. | No issue found. |
| CR08-01 | `Features/Accounts/AccountFormModel.cs`, new `Vafadar.Zanance.Core/Accounts/AccountNames.cs` | bug | Medium | (CR04-06) Two accounts could have the same name, also in another spelling (case, half-space, Arabic letter forms); pickers and the CSV format could not tell them apart. | Fixed: the editor refuses a name another account (also an archived one) uses, `Account_NameTaken`; tests in `CodeReviewPart8Tests`. |
| CR08-02 | `Features/Accounts/AccountEditorViewModel.cs` | bug | Medium | A loan, money lent or an asset could be made the default account (quick add then refused it), and an account changed into such a type stayed the default. Switching the default and cancelling discarded the change without asking. | Fixed: the switch is offered for money accounts only, the default is cleared when the type no longer allows it, and the switch counts as a change. |
| CR08-03 | `Features/Categories/CategoryEditorPage.xaml` + `ViewModel`, `Features/Entries/EntryEditorPage.xaml` + `ViewModel` | bug, UI | Medium | Seen in the snapshots: two icons looked selected in the category editor. A trigger setter whose value is a binding stays applied after the trigger turns off, so the icon chosen first kept its outline; the category chips of the entry editor use the same pattern. | Fixed: the outline and fill are properties of the swatch or chip; checked in both themes. The plan editor has the same pattern and is fixed in CR09. |
| CR08-04 | `Features/Categories/CategoriesViewModel.cs` | bug | Medium | Archiving a main category leaves its sub-categories active, but the list showed them neither under an active main category nor under archived – they could no longer be opened. | Fixed: such sub-categories are listed on their own. |
| CR08-05 | `Features/Holdings/AssetTypeEditorViewModel.cs` | quality | Low | First reported as a bug (a second tap creating the type twice). Corrected in CR11: a `[RelayCommand]` command refuses a second run while the first runs (the toolkit default), so this could not happen. | A busy guard and editing the saved type afterwards were added; they are kept as defence only. |
| CR08-06 | `Features/Holdings/AssetEventEditorViewModel.cs`, `HoldingDetailViewModel.cs` | bug | Low | A price per gram times a very large quantity, or a very large total price, could overflow while typing or saving (as CR04-02). Editing an event read the whole ledger to find its money entries. | Fixed: such input is invalid; the group's entries are read directly. |
| CR08-07 | `Features/Rates/RatesViewModel.cs`, `RatesPage.xaml` | UI/UX | Low | The suggested "from" currency ignored the missing rates (the code meant to prefer them never ran), the Persian decimal separator "٫" was not accepted in a rate, and a rate row did not say that a tap offers to delete it. | Fixed: a missing currency first; "٫" accepted; the row says "Delete rate?" as its hint. |
| CR08-08 | `Features/Accounts/AccountRow.xaml` | UI | Low | Type, "Default", "Not in totals" and "Incomplete" stood in one line that was cut off on a phone; the comment said the row opens the editor (it opens the details). | Fixed: the labels wrap; comment corrected; the tap handler is guarded. |
| CR08-09 | `Features/Categories/CategoryEditorViewModel.cs` | quality | Low | (CR06-11) Its own copy of the icon list, without Wallet and Payment, so "Icon 43" meant different icons. | Fixed: `IconPicker.Keys`. |
| CR08-10 | `AccountDetailViewModel.cs`, `AccountFormModel.cs`, `RulesViewModel.cs` and the CR08 pages | quality, accessibility | Low | Settings read twice per load, misindented lines, a rule saved with surrounding spaces, unnamed checkboxes and switches, hand-styled text buttons, delete buttons without the item's name. | Fixed. |
| CR08-11 | Accounts, holdings, categories, rates | review | – | Reconciliation, loan estimate and installment, holding conflicts and undo, display units. | No issue found. |
| CR09-01 | `Features/Plans/PlanEditorViewModel.cs` | bug | High | "Also skip public holidays" was read from a plan but never written: `BuildRule` set no `HolidayRegion`, so the switch was lost on every save (and the preview ignored it) since the option exists. | Fixed: the region is part of the rule (a plan keeps its region when the device's region has no calendar); the preview shows the shifted dates. |
| CR09-02 | `Features/Budget/BudgetViewModel.cs`, `Vafadar.Zanance.Data/ZananceStore.cs` | bug | Medium | "Copy to next month" over an existing budget deleted it and saved the copy in a second step: a failing save lost the next month's budget. | Fixed: `ReplaceBudgetAsync` replaces in one transaction; test `Copying_over_an_existing_budget_replaces_it_in_one_step`. |
| CR09-03 | `BudgetViewModel.LoadAsync` | bug, performance | Medium | Appearing, the period, currency and confirmed-only switches could load at the same time and mix their lines (as CR07-05); accounts, plans and states were read twice. A failing currency switch could end the app (`async void`). | Fixed: loads run one after the other; data read once; the handler is guarded. |
| CR09-04 | `Features/Budget/BudgetEditorViewModel.cs` | bug | Medium | With every account unticked, the empty list meant "all accounts". (The double tap reported here as well could not happen – corrected in CR11, see CR08-05.) | Fixed: at least one account is required (`Budget_NoAccountChosen`); the busy guard is kept as defence. |
| CR09-05 | `Features/Plans/PlanEditorPage.xaml` | bug, UI | Medium | (CR08-03) Category chips used a trigger setter with a binding: an earlier choice could keep looking selected. | Fixed with the chip's outline and fill. |
| CR09-06 | `Features/Plans/PlansViewModel.cs`, `ZananceStore.CountUnreviewedAsync` | performance | Low | The plan centre loaded every entry to count the unreviewed ones, and filled its list row by row. | Fixed: counted in the database; the list is set at once. |
| CR09-07 | `Features/Plans/PlanRowView.xaml` | accessibility | Low | Screen readers heard the name and date of a due item but not its amount or "3 days overdue" (Home and the plan centre). | Fixed: the row's full description. |
| CR09-08 | `PlanEditorViewModel.SaveAsync` | bug | Low | A navigation failure after a successful save was reported as "could not be saved" (as CR07-03). | Fixed. |
| CR09-09 | Plan and budget pages | accessibility, control | Low | Ten switches and a checkbox list without names (the account checkboxes now say the account), hand-styled text buttons. | Fixed. |
| CR09-10 | `OccurrenceViewModel`, `PlanDetailViewModel`, `SettlementViewModel` | review | – | Confirm, link, skip, partial payments and their undo; pause, resume and end; final settlement. | No bug found. Commands do not run twice at once (toolkit default), so skip and link need no guard of their own. |
| CR10-01 | `Features/Reports/ReportsViewModel.cs` | bug | Medium | A `_loading` flag kept the page's own scope changes from reloading – and also dropped a change the user made while a load ran (another currency, period or package showed the old numbers); loads from appearing and a query, and the PDF walk through the packages, could run at the same time. | Fixed: the page's own changes are marked separately, a user change during a load is applied by one more load, and loads and the PDF run one after the other. |
| CR10-02 | `Features/Goals/GoalEditorViewModel.cs` | bug | Medium | When the goal was saved but its contribution plan was not, the page still treated the goal as new: saving again added a second goal. A navigation problem after saving showed the account error. | Fixed: after the first save the page edits the saved goal; navigation is separate from saving. |
| CR10-03 | `Features/Forecast/ForecastViewModel.cs` | bug | Low | Appearing and the horizon switch could compute at the same time and fill the cards twice (as CR07-05). | Fixed: loads run one after the other. |
| CR10-04 | `Features/Goals/GoalDetailViewModel.cs` | performance, bug | Low | One load read all accounts and the whole ledger four times (coverage, trend, capacity, progress). The assumed price was multiplied by 1,000 without an overflow check (as CR08-06). | Fixed: read once and passed on; a price too large is not stored. |
| CR10-05 | `ReportsViewModel.Overview.cs` | performance | Low | The overview read the categories again for the budget number. | Fixed: the lookup of the overview is used. |
| CR10-06 | Report, KPI sheet, snapshot and goal pages | control | Low | Hand-styled text buttons. | Fixed: `TextButton` style. |
| CR10-07 | Reports, KPI sheets, period review, goals list, snapshots | review | – | KPIs with their explanation sheets, drill-downs with the same scope, PDF, review steps, snapshot comparison (CR03-02). | No issue found. Editors without a discard question on Cancel (goals, splits, holdings, settlement) go to CR12. |
| CR11-01 | `Vafadar.Zanance.Data/ZananceStore.cs`, `Features/Settings/SettingsViewModel.cs`, `BudgetViewModel.cs`, `PeriodReviewViewModel.cs` | bug | Medium | Every settings change read the row, changed one value and wrote the whole row back. Two changes at the same moment (two switches, or a review step ticked twice quickly) restored each other's old value. Six handlers were `async void` without a guard: a database error ended the app. | Fixed: `UpdateSettingsAsync` runs read, change and save one at a time; the settings page, the budget switches and the review steps use it, guarded. Test `Settings_changed_at_the_same_moment_are_all_kept`. |
| CR11-02 | `Features/Backup/BackupViewModel.cs` | bug | Medium | After a successful restore, a problem showing the message or rebuilding the screens was caught by the general handler and reported as "restore failed". | Fixed: the message and the rebuild follow the restore, guarded on their own (also for cloud backups, which use the same restore). |
| CR11-03 | `Presentation/AttachmentFiles.cs` (cache cleanup) | privacy | Medium | CSV exports and PDF reports were written unencrypted to the cache for sharing and never removed; they stayed until the system cleared the cache. | Fixed: deleted on every start and after "Delete all data", like the attachment copies. |
| CR11-04 | `Features/Onboarding/OnboardingViewModel.cs` | bug | Low | A finish that failed after the first account was saved created a second "first account" on the next try, and an error was not caught. | Fixed: the account of a failed finish is reused (and the name check of CR08-01 applies); errors are shown. |
| CR11-05 | Review document, `CHANGELOG.md` | correction | – | CR08-05 and part of CR09-04 reported duplicates from a double tap on Save. `[RelayCommand]` commands do not run twice at once (CommunityToolkit default; the button is disabled meanwhile), so these could not happen. | Rows and changelog corrected; the added guards stay as defence only. |
| CR11-06 | Settings, backup, import/export and More pages | accessibility, control | Low | Five switches without a name (app lock, notification details, backup password, export notes, skip duplicates); hand-styled text and row buttons. | Fixed. |
| CR11-07 | Import/export, profiles, About, cloud backup | review | – | Import preview and all-or-nothing import, undo of an import batch, holdings file, profile switch and deletion, licences, cloud sign-in errors. | No issue found. |
| CR12-01 | New `Presentation/UnsavedChanges.cs`; goal, budget, holding event, asset type, split and settlement editors | UI/UX | Medium | (CR10-07) Six editors left at once on Cancel or the Android back button, also with typed input; the other editors asked "Discard changes?". | Fixed: one shared helper asks the same question everywhere; each editor compares its input with what it loaded. |
| CR12-02 | 24 labels with a "?" on 11 pages (`HorizontalStackLayout` of `FieldLabel` and `HelpButton`) | UI | Medium | At 360 px a long label (German "Kurse gelten als möglicherweise veraltet nach") pushed its "?" off the screen: a horizontal stack never shrinks its children. | Fixed: a grid that keeps the "?" next to a short label and lets a long one wrap; checked in German and Persian at 360 px. |
| CR12-03 | `HomePage.xaml` (pinned goals), `BudgetPage.xaml` (limit lines) | UI | Medium | At 360 px the amounts next to a name squeezed it letter by letter ("Emer / genc / y fund", "Gesa / mt"). | Fixed: the amounts go under the name, as on the goals page. |
| CR12-04 | `HomePage.xaml` quick add | UI | Low | German "Umbuchung" was cut ("Umbuch…") at 360 px. | Fixed: a short label of its own, `Home_QuickTransfer` ("Transfer"). |
| CR12-05 | `Features/Settings/SettingsPage.xaml` (day-to-day spending) | UI | Low | The amount field shared its line with three chips and shrank to a few letters on a phone. | Fixed: the chips have their own line. |
| CR12-06 | `Features/Holdings/HoldingText.cs` (`SignedQuantity`) | UI, RTL | Low | In Persian the sign of a holding change stood after the number ("۱۰٫۰۰۰+ گرم"), unlike amounts. | Fixed: sign and number form one left-to-right run, the unit follows in the reading direction ("+۱۰٫۰۰۰ گرم"). |
| CR12-07 | `Features/Accounts/AccountFormView.xaml` | control | Low | "Balance is negative" could be toggled by its label, "Opening balance unknown" only by the small box. | Fixed: both labels toggle their box. |
| CR12-08 | `eng/scripts/Run-Snapshots.ps1` | engineering | Low | Every walk-through moved the previous walk-through's sample data aside: 71 `Data-before-snapshots-*` folders (27.6 MB) since 1 Oct. | Fixed: a marker identifies unchanged sample data, which is removed instead; data changed after a walk-through is still moved aside, never deleted. The existing folders are left for the owner to decide. |
| CR12-09 | `Vafadar.Localization/Formatting/NativeDigits.cs` | decision | – | (CR06-12) Rendered large in the app's Persian font, the thousands separator "٬" and the decimal separator "٫" are clearly different (comma and slanted stroke); the ambiguity came from downscaled snapshots. | No change: standard Persian notation. |
| CR12-10 | Undo after deleting (CR07-01) | decision | – | A shared undo bar for every page was considered. Deleting outside the transaction list now asks first; the only other undo (replacing an aggregated entry) follows an explicit choice in a dialog. | No change: no case loses data without a question. |
| CR12-11 | Icon names for screen readers (CR06-02, CR06-11) | accessibility | Low | The icon tiles were named "Icon 12" – the same in every picker, but saying nothing about the icon. | Done after the review: every icon has a name in three languages (`Icon_Home` … `Icon_QuestionCircle`, e.g. "Car", "Savings"), used by the shared icon picker and the category editor; `Category_IconOption` removed. |
| CR12-12 | `.github/workflows`, `eng/scripts` | review | – | CI (tests, Android, Windows, optional iOS), CodeQL, signed release with validated inputs, removed keystore and short artifact retention; string, signing and APK scripts. | No issue found. CI has no NuGet cache (about 8 minutes per run); left as it is. |
| CR12-13 | All screens | walk-through | – | English, Persian and German; light and dark; 360 px and the default phone size; Simple and Advanced (snapshots of every section, CR06–CR12). | Findings fixed above; nothing else found. |
| CR12-14 | All main screens | walk-through | – | Follow-up after the review: wide window (1280 × 820) in English and Persian, German in the dark theme at 360 px, Simple mode in Persian and German at 360 px. | No issue found: content keeps its readable width in wide windows. |
| CR12-15 | `Features/Categories/CategoryEditorViewModel.cs`, `Features/Profiles/ProfilesViewModel.cs` | bug | Low | Seen during CR11, recorded here: two categories with the same name, type and main category (the CSV import takes the first of them), and two profiles with one name, could be created. | Fixed: refused with `Category_NameTaken` and `Profile_NameTaken`; the same name under different main categories stays allowed ("Other"). |
| CR12-16 | iOS | open | – | The iOS targets build only in CI on demand (macOS runner) and were not built during the review. | Owner: run the CI workflow with "ios" ticked. |
| CR12-17 | Device checks | open | – | Only the device can confirm: a large camera photo as attachment (CR06-01), the lock screen in the dark theme, the Android back button in the editors (CR12-01), screen readers (TalkBack) with the names added in CR06–CR11. | Owner: check with the APK. |
| CR12-18 | `eng/scripts/Run-Snapshots.ps1`, `Diagnostics/DebugSnapshots.cs` | engineering | Low | Found while shooting the lunar calendar (2026-10-05): `-Only home,budget` reached the app as "home budget" (PowerShell joins a list passed to a `[string]` parameter with spaces), so no screen matched and the walk-through ended after onboarding without an error; the documented `-Only report,holding` had the same effect. A crash outside the walk-through left no trace. | Fixed: `-Languages` and `-Only` take lists, a filter that matches no screen is an error, unhandled exceptions are written to `error.txt`, and a non-zero exit code is printed. New `-Calendar` option. |

## 6. Outcome

All twelve sections were reviewed in order on 2026-10-04; every section ended with tests, Release builds, snapshots of the
changed screens, a pushed commit, a CI run and an APK for the owner's phone.

- 114 rows: 5 High, 38 Medium, 55 Low, 16 reviews, decisions or open owner checks without a severity (counted after the follow-up corrections).
- The High findings were wrong data or lost input: a German quantity read ten times too large (CR04-01), a rial cost basis
  that overflowed (CR04-02), an assumed price lost on update (CR05-01), an entry deleted without question or undo
  (CR07-01) and the public holiday option of plans never saved (CR09-01).
- Recurring patterns, now fixed everywhere they occurred: loads that could run at the same time and mix their rows,
  read-modify-write of the settings, trigger setters with bindings, success reported as failure after navigation
  problems, hand-styled buttons and unnamed switches, layouts that broke at 360 px.
- Each fixed bug that can be tested outside the UI has a regression test (`CodeReview*Tests`); the suite has
  755 tests at the end of the review.
- Two findings were corrected later in the review itself (CR11-05).
