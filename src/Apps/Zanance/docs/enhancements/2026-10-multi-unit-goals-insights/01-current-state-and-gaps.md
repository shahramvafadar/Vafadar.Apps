# 01 – Current state and gaps

Base: commit `67410b4` (see [README](README.md)). Paths are relative to `src/Apps/Zanance/` unless they start with
`src/Libraries` or `test/`. `Core` = `Vafadar.Zanance.Core`, `Data` = `Vafadar.Zanance.Data`, `App` =
`Vafadar.Zanance.App`.

## 1. Method and limits

* Static reading of the code, migrations, tests and documents at the base commit (three parallel read-only
  surveys plus targeted reading). No file outside this package was changed.
* The existing UI was reviewed in the Windows Debug build with fictitious data (the Debug snapshot walk-through
  `Diagnostics/DebugSnapshots.cs`, en/fa/de, light/dark, 360/412 px and wide windows) during the review rounds of
  2026-10-01/02; the Android release build was installed on an emulator. No device test, user test or security audit
  was performed for this package; iOS could not be built or run (no Mac).
* The existing automated tests (499) passed at the base commit (`dotnet test --solution Vafadar.Tests.slnf`); see
  [07 §6](07-acceptance-and-validation.md#6-what-was-actually-run) for the exact run of this stage.
* **There is no test project for `Vafadar.Zanance.App`**: everything in view models (entry editor account choice,
  Home, settings, profiles, undo service, report view models, Simple/Advanced) is untested.

Levels used below: **D** design only · **L** domain logic · **P** persistence · **U** UI reachable · **T** automated
test · **V** verified on a platform build. Status words: **Existing** (no change needed except regression),
**Extend**, **New**, **Blocked**, **Needs verification**.

## 2. Currencies, accounts, quick entry (ZEX-MC)

| Id | Requirement (short) | Status | Levels today | Evidence | Gap |
|---|---|---|---|---|---|
| ZEX-MC01 | One native currency per account; several EUR/USD/IRR accounts | Existing | L P U T V | `Core/Accounts/Account.cs:68`; totals per currency `Core/Ledger/LedgerCalculator.cs:75-128`; test `LedgerRulesTests.cs:206` | – |
| ZEX-MC02 | Default-currency change keeps history; account currency not relabelled | Extend | L P U T | Store refuses currency change with entries `Data/ZananceStore.cs:122-149`; UI disables the picker `App/Features/Accounts/AccountEditorViewModel.cs:63`; test `ZananceStoreTests.cs:77` | No separate default currency (the report currency is used for new accounts, goals, budgets, rates); the lock ignores plans, templates, goal earmarks and the loan installment, whose amounts would be relabelled |
| ZEX-MC03 | Quick add uses the default account; balance changes only on Save | Extend | L U | `DefaultAccountId` `Core/Settings/ZananceSettings.cs:25`; editor preselect `App/Features/Entries/EntryEditorViewModel.cs:302-304` | Falls back silently to the first active account; no "add entry here" on an account page |
| ZEX-MC04 | Precedence explicit › context › default; filters never change it | Extend | U | Query keys `kind`, `from`/`to`, `template` (`EntryEditorViewModel.cs:302-349`) | Not specified as a contract; no account context from account pages; untested |
| ZEX-MC05 | Account's currency wins; amount and account visible before Save | Extend | U | Currency from account `EntryEditorViewModel.cs:571-577`; picker hidden with one account `:544` | Changing the account keeps the digits and re-reads them in the new currency without notice; with a display unit on one side the stored value can shift by 10ⁿ |
| ZEX-MC06 | Invalid default asks; holdings never the default | Extend | U | Archived accounts not in the picker `:457`; archiving keeps `DefaultAccountId` (`AccountEditorViewModel.cs:109-142`) | Silent fallback; template with an archived account keeps its amount but changes account and currency (`EntryEditorViewModel.cs:424-427, 485-486`) |
| ZEX-MC07 | Main totals per currency | Existing | L U T | Home `App/Features/Home/HomeViewModel.cs:242-246` | Archived accounts with *include in totals* still count on Home but not on the Accounts page (`AccountsViewModel.cs:108`) |
| ZEX-MC08 | Converted total secondary, with currency, rate and date; missing ≠ 0 | Extend | L U T | `Core/Rates/RateTable.cs:100-124` (oldest rate date, incomplete); Home text `Home_Combined` | No freshness rule; `IsEstimate` not carried into converted totals; only Home converts |
| ZEX-MC09 | Toman is a display factor of IRR; CSV names the unit | Extend | L U T | `Core/Money/DisplayUnit.cs`, `MoneyText.cs`; CSV in ISO units `Core/DataFiles/CsvExport.cs:81-85` | Stored in device preferences (`App/Presentation/DisplayUnitPreferences.cs:12`): not per profile, not in backups, cleared for all profiles by "Delete all data"; receipt amounts are read through the display unit (10× error for a rial receipt on a toman account, `EntryEditorViewModel.cs:373-378`) |
| ZEX-MC10 | Cross-currency transfer with both amounts and a fee; no fake income | Existing | L P U T | `LedgerEntry.Amount/ToAmount`; fee as linked expense `Core/Ledger/EntryActions.cs:81-98`; tests `LedgerRulesTests.cs:67,167`, `ZananceStoreTests.cs:136,155` | No effect preview, no implied rate shown, no destination-side fee |
| ZEX-MC11 | Country / usability per account only when entered | New | – | `Account` has no such field | – |
| ZEX-MC12 | Usable money, valued assets and debts apart; credit limit is not money | Extend | L U | `IsDebt`/`IsOutsideCash` `Account.cs:40-43`; Accounts page groups `AccountsViewModel.cs:80-129` | Home totals, forecast and period totals filter on *include in totals* only, not on type; no net worth calculation anywhere |

## 3. Quantity holdings (ZEX-AS)

Confirmed absent: any quantity, unit, weight, count, purity or valuation model (search for Quantity, Gram, Weight,
Ounce, Karat, Valuation found none). The existing `Asset` account type is a money value revalued by adjustment
entries (`Core/Accounts/Reconciliation.cs:37-57`); the validator does not stop income or expense entries on it.

| Id | Requirement (short) | Status | Note |
|---|---|---|---|
| ZEX-AS01 | g/kg and counts; g↔kg × 1000 exactly | New | – |
| ZEX-AS02 | Add only same identity and dimension | New | – |
| ZEX-AS03 | Purities apart; fine-metal equivalent separately | New | – |
| ZEX-AS04 | Divisible or whole counts per type | New | – |
| ZEX-AS05 | Coins/bars: count and optional unit weight, never double | New | – |
| ZEX-AS06 | Quantity without price; unknown value, not zero | New | Pattern exists for money: *incomplete* labels (`RateTable`, forecast) |
| ZEX-AS07 | Price per unit or total value, multiplied once | New | – |
| ZEX-AS08 | Purchase, partial sale, location transfer, gift, outflow, correction | New | – |
| ZEX-AS09 | Purchase = one linked operation (cash ↓, quantity ↑), not consumption | New | Linked operations exist for transfer + fee (`GroupId`, `ZananceStore.cs:842-911`) |
| ZEX-AS10 | Sale proceeds not income; result only with known basis | New | – |
| ZEX-AS11 | Price changes never change quantity or income | New | – |
| ZEX-AS12 | Opening holding is not a purchase | New | – |
| ZEX-AS13 | History of quantity, prices and values kept | New | Rates already use "on or before date" (`RateTable.cs:126-127`) |
| ZEX-AS14 | Edit/delete/undo keep both sides consistent | New | Group undo exists for entries (`ZananceStore.cs:917-1023`) |
| ZEX-AS15 | No negative quantity; no overselling | New | – |
| ZEX-AS16 | Everything in backups; no forced conversion of old data | New | Backup is the whole database (`src/Libraries/Vafadar.Data/SqliteDatabaseBackupSource.cs:30-52`), so new tables are included automatically |

## 4. Goals (ZEX-GO)

Existing (P2-1, `Core/Goals/Goal.cs`, `GoalCalculator.cs`, `App/Features/Goals/*`): earmark goals with currency,
target, optional date, priority, monthly/weekly frequency, states Active/Completed/Archived, earmarks per account,
coverage by real balances with shortfall by priority (AT-65), suggested contribution `ceil(remaining /
opportunities)`. Tests: `test/Apps/Zanance/Vafadar.Zanance.Core.Tests/Goals/GoalCalculatorTests.cs` (6),
`ZananceStoreTests.cs:319-333`.

| Id | Requirement (short) | Status | Evidence | Gap |
|---|---|---|---|---|
| ZEX-GO01 | Name, type, account/asset, target and unit required | Extend | `GoalEditorViewModel.cs:123-127` | No goal type, account link, holding link or contribution method |
| ZEX-GO02 | Balance goal follows deposits/withdrawals; own transfer is progress, not income | New | Earmarks are manual; income already excludes transfers (`LedgerCalculator.cs:105-117`) | Balance goal type missing |
| ZEX-GO03 | One active balance goal per account | New | – | – |
| ZEX-GO04 | Remaining = max(0, T − F); bar ≤ 100 %, real amount shown | Extend | `GoalCalculator.cs:68`, `:16` | "Above target" amount not shown |
| ZEX-GO05 | No double counting across goals and envelopes | Extend | `GoalCalculator.cs:136-162`; envelope `App/Features/Budget/BudgetViewModel.cs:407-414` | Funding accounts are not limited to money accounts (`GoalDetailViewModel.cs:145`): an Asset or Loan balance can "fund" a goal |
| ZEX-GO06 | Pin 1–2 goals on Home; reachable in Simple | New | `Core/Dashboard/HomeLayout.cs:5-27` has no goals section | – |
| ZEX-GO07 | Card: name, current, target, remaining, %, next contribution/ETA | Extend | `GoalPresenter.cs:13-73` (lists only) | Not on Home; no ETA |
| ZEX-GO08 | Fixed, percent of eligible income, spending cut | New | Only an even split (`GoalCalculator.cs:124-132`) | – |
| ZEX-GO09 | Eligible income excludes transfers, loan principal, asset sales, reimbursements | New (inputs exist) | Income definition `LedgerCalculator.cs:105-117`; reimbursement = refund (`EntryActions.cs:253-259`) | Asset sales do not exist yet |
| ZEX-GO10 | Preview before changing budgets or entries | New | – | – |
| ZEX-GO11 | Real contribution dates: weekly, two-week, calendar | Extend | `GoalCalculator.cs:99-118` uses Gregorian `AddMonths` | No two-week schedule, Persian calendar or financial month |
| ZEX-GO12 | Active, paused, reached, completed, archived | Extend | `Goal.cs:29-39`; reached computed `GoalCalculator.cs:19` | No Paused |
| ZEX-GO13 | Withdrawal after reaching; spending a completed goal keeps history | Extend | Completing releases earmarks (`GoalCalculator.cs:59`) | No rule documented; releases are not capped at the earmarked amount |
| ZEX-GO14 | Effect of a change shown before Save | New | Allocations save immediately (`GoalDetailViewModel.cs:176-184`) | – |
| ZEX-GO15 | Distinct messages for overdue, no data, zero/negative, invalid unit, archived account | Extend | `Goal_Overdue`, `Goal_Unfunded`, `Goal_SuggestNow` … in `AppResources.resx` | Insufficient data, zero/negative pace, invalid unit, archived account missing |

## 5. KPIs (ZEX-K) – summary

Full definitions and the change needed per KPI are in [04](04-kpi-and-report-catalog.md).

| Id | KPI | Status | What exists |
|---|---|---|---|
| ZEX-K01 | Budget remaining | Extend | `BudgetCalculator.cs:13,45-94`, rollover, flex; confirmed-only only on the Budget page; Home shows only the report-currency budget |
| ZEX-K02 | Lowest forecast balance and date | Extend | `Core/Forecasts/Forecast.cs:241-256`; Home card Advanced only; no account scope; credit cards and savings always included |
| ZEX-K03 | Goal progress | Extend | Earmark goals only |
| ZEX-K04 | Open commitments, 30 days | New (blocks exist) | Forecast items, plans list (60 days), contracts (30 days) |
| ZEX-K05 | Operating income surplus | Extend | `PeriodTotals.Result` = net income − net expense; includes unreviewed; no capital kinds yet |
| ZEX-K06 | Surplus rate | Extend | `Report_SavingsRate` = Result / NetIncome (`ReportsViewModel.cs:387-391`), untested |
| ZEX-K07 | Essential expense coverage | New | – |
| ZEX-K08 | Fixed and non-monthly commitments, 12 months | Extend | `MonthlyEquivalent`, `PlannedInPeriod` (`BudgetPlanning.cs:20-79`), `SpendingType` |
| ZEX-K09 | Debt service burden | New (inputs exist) | `Account.Installment`, `LoanCalculator` |
| ZEX-K10 | Net worth | New | No net-worth calculation anywhere |
| ZEX-K11 | Spending pattern change | New | Only period chips and the trend chart; partial months labelled but not compared same-length |
| ZEX-K12 | Open receivables and their age | Extend | `OpenReimbursements`, Lent accounts; no due date, no age |
| ZEX-K13 | Currency and asset composition | New | Converted totals only on Home |
| ZEX-K14 | Data status of a report | Extend | Unreviewed counts, unknown amounts, missing rates exist in places; no last-reconciled date, no backup status on Home, `IsAutomaticBackupDue` never called |

## 6. Reports, data, backup, modes – summary

| Area | Status | Evidence | Gap |
|---|---|---|---|
| Report types (expenses by category, income & expense, trend, accounts, plans vs actual, tags) | Existing / Extend | `App/Features/Reports/ReportsViewModel.cs`, `Core/Reports/ReportCalculator.cs`, `LedgerCalculator.cs` | Report currency only (no selector); drill-down ignores currency (`ReportsViewModel.cs:528`); Home donut net vs report donut gross; tag entry count mixes kinds/currencies; no same-length comparison; no reports for goals, holdings, net worth, commitments, data quality |
| PDF | Extend | `Vafadar.Zanance.Reports/PdfReport.cs`; same `Build*` path as the screen | Trend and account details missing; scope line claims "accounts in totals" for sections that are not |
| CSV | Extend | `CsvExport.cs`, `CsvImport.cs`; tests `CsvTests.cs` | Entries only; no format-version marker; no aggregated flag; no holdings |
| Backup | Extend | Whole database snapshot, AES-256-GCM, manifest, restore checks (`src/Libraries/Vafadar.Backup/*`) | Device preferences not included: display units, Home layout, dismissed guidance; preview counts only accounts/entries/plans; unencrypted safety copies; local backup can be made without password |
| Undo | Extend | Entry delete (8 s), import batches, occurrence settle/skip | Not for edits, goals, rates, budgets |
| Simple/Advanced | Extend | `ExperienceMode` per profile (`ZananceSettings.cs:6-29`); inventory in [05](05-simple-advanced-matrix.md) | No number changes, but weekly budgets are invisible in Simple without a note while still alerting; several hidden plan options without summary; no tests |
| Profiles | Existing | One database per profile (`App/Profiles/ProfileService.cs`) | Device preferences shared by all profiles; no tests |
| Security | Existing | App lock, `FLAG_SECURE`, iOS cover, encrypted backups | Live database not encrypted (by design, must not be claimed); Windows has no screenshot protection |

## 7. Complementary capabilities

| Capability | Status | Evidence |
|---|---|---|
| Period-end review | Extend | Pieces exist (unreviewed list, plans review, reconcile, backup page); no guided flow |
| Aggregated entries and overlap | New | No flag on `LedgerEntry` |
| Wealth history and its decomposition | New | No net worth; asset values only as adjustments |
| Forecast snapshot and comparison | New | Scenarios are never saved (`Forecast.cs:50-111`) |
| Product transparency (platform matrix, backup coverage, problem report path) | Extend | About page and privacy matrix exist; no in-app coverage page |

## 8. Findings in existing code (not fixed in this stage)

Each finding is assigned to a story; nothing was changed now.

| Id | Finding | Evidence | Story |
|---|---|---|---|
| ZEX-F01 | Currency lock ignores plans, templates, goal earmarks, loan installment | `ZananceStore.cs:122-149` | ZEX-S0105 |
| ZEX-F02 | Receipt amount parsed through the display unit (10× for toman) | `EntryEditorViewModel.cs:373-378` | ZEX-S0103 |
| ZEX-F03 | Template with archived account reinterprets its amount in another currency | `EntryEditorViewModel.cs:424-427, 485-486` | ZEX-S0103 |
| ZEX-F04 | Archived default account: silent fallback; default never cleared | `EntryEditorViewModel.cs:302-304`, `AccountEditorViewModel.cs:109-142` | ZEX-S0103 |
| ZEX-F05 | Home counts archived accounts, the Accounts page does not | `HomeViewModel.cs:242-246`, `AccountsViewModel.cs:108` | ZEX-S0201 |
| ZEX-F06 | Goals can be funded from Asset, Loan, Lent and card balances | `GoalDetailViewModel.cs:145` | ZEX-S0307 |
| ZEX-F07 | Weekly/two-week budgets hidden in Simple without a note, still alerting | `BudgetViewModel.cs:241-245`, `ReminderService.cs:208-209` | ZEX-S0502 |
| ZEX-F08 | Hidden plan options without summary in Simple (calendar, weekend rule, second day, holidays, second reminder, contract) | `PlanEditorPage.xaml:125-229` | ZEX-S0502 |
| ZEX-F09 | Report drill-downs ignore currency | `ReportsViewModel.cs:528`, `TransactionsViewModel.cs:149-177` | ZEX-S0601 |
| ZEX-F10 | PDF scope line does not match all sections | `Report_PdfScope`, `ReportsViewModel.cs:418` | ZEX-S0601 |
| ZEX-F11 | Two donut definitions (Home net, Reports gross) | `HomeViewModel.cs:475-476`, `ReportsViewModel.cs:340-345` | ZEX-S0104 |
| ZEX-F12 | Display units, Home layout, dismissed guidance in device preferences (not per profile, not in backup) | `DisplayUnitPreferences.cs:12`, `HomeLayoutPreferences.cs:8` | ZEX-S0205 |
| ZEX-F13 | Sums of minor units not checked for overflow; `decimal.ToInt64` in conversion may throw | `LedgerCalculator.cs:60-85`, `RateTable.cs` | ZEX-S0104 |
| ZEX-F14 | No App/view-model tests | `test/` | ZEX-S0101 |
| ZEX-F15 | Onboarding does not set the experience mode (the changelog says it does) | `ZananceSettings.cs:29`, onboarding view model | ZEX-S0501 |
| ZEX-F16 | Reconciliation documented as Advanced, available in both modes | `03-ux-design.md:36`, account detail page | ZEX-S0501 (documentation) |
| ZEX-F17 | Automatic backup reminder exists in the library but is never used | `BackupService.cs:70` | ZEX-S0608 |
| ZEX-F18 | Tag report entry count includes kinds and currencies the amount excludes | `ReportsViewModel.cs:450-454` | ZEX-S0602 |
| ZEX-F19 | Transfer destination preselected in any currency | `EntryEditorViewModel.cs:312` | ZEX-S0204 |
| ZEX-F20 | Unencrypted safety copies before restore; local backups possible without password | `BackupService.cs:98-99`, `BackupViewModel.cs:149` | ZEX-S0902 (risk note; behaviour kept, disclosed) |
