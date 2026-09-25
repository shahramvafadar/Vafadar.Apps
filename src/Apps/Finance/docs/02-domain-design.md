# 02 – Domain design

All types live in `Vafadar.Finance.Core` unless stated otherwise. Rules are referenced by specification IDs.

## 1. Money and currency

* `Currency` – ISO 4217 code plus minor digits from the ISO table (EUR 2, JPY 0, KWD 3, IRR 2). Unofficial display
  units such as Toman are phase 2 (FX-07) and are never inferred from language or country.
* `Money(long Minor, Currency)` – arithmetic only between equal currencies; conversion is explicit (`ExchangeRate`).
* Parsing user input accepts Latin, Persian (۰–۹) and Arabic-Indic (٠–٩) digits and the decimal separators of the
  current culture plus `.`/`٫`; ambiguous group separators are rejected, never guessed (LOC-04, FIN-05).
* Rounding: half away from zero to the currency's minor unit; totals are computed from minor units, so displayed
  sums always equal the displayed total.

## 2. Entities

| Entity | Key fields | Notes |
|---|---|---|
| `Account` | Name, Type (Cash, Checking, Savings, CreditCard), Currency, OpeningBalance (minor, signed), OpeningDate, Icon, IncludeInTotals, IsArchived, SortOrder | ACC-01..07. Currency cannot change once entries exist (ACC-07) |
| `Category` | Kind (Income/Expense), Name or SystemKey (translated default), ParentId (one level), Color, Icon, IsArchived, SortOrder | CAT-01..04. Deleting a used category is not possible; archive or merge instead |
| `LedgerEntry` | Kind, Date (DateOnly, effective), AccountId, Amount (minor, > 0), CategoryId?, Title?, Payee?, Note?, Icon?, Review (Confirmed/Unreviewed), Source (Manual, Schedule, Import, Adjustment) | TX-01..07 |
| – transfer part | ToAccountId, ToAmount | Only for Transfer; `ToAmount` in the destination currency (FX-03) |
| – foreign amount | OriginalAmount, OriginalCurrency | Purchase amount in another currency; `Amount` is what the account was charged (FX-01) |
| – links | RefundOfId?, ScheduleId?, OccurrenceDate?, ImportBatchId?, ImportKey? | Unique index on (ScheduleId, OccurrenceDate) (D-07) |
| `Schedule` | Name, Kind (Income, Expense, Transfer), AccountId, ToAccountId?, CategoryId?, AmountMode (Fixed, Estimated, Unknown), Amount?, ToAmount?, Note, Icon, Rule, Reminder, AutoPost, State (Active, Paused, Ended), PausedFrom?, PreviousScheduleId? | REC-01..22 |
| `OccurrenceState` | ScheduleId, OriginalDate, Status (Settled, Skipped, Cancelled, Open-with-changes), DueDate override, Amount override, EntryId?, Note, AutoPostSuppressed | Unique (ScheduleId, OriginalDate) (D-06) |
| `Budget` | Year, Month, Calendar (Gregorian/Persian), Currency, TotalLimit?, AccountScope (all included or explicit list) | BUD-01..08 |
| `BudgetCategoryLimit` | BudgetId, CategoryId, Limit | BUD-02 |
| `ExchangeRate` | Date, From, To, Rate (decimal as string), IsEstimate | FX-02, FX-04 |
| `ImportBatch` | FileName, ImportedAt, RowCount, State (Applied, Reverted) | IO-11 |
| `FinanceSettings` | ReportCurrency, DefaultAccountId, Mode (Simple/Advanced), BudgetCalendar, WeekStart, ReminderDefaults, NotificationShowsAmounts, OnboardingCompleted | Stored in the database so they are part of backups (BAK-03) |

Transfers, opening balances and adjustments never carry an income/expense category.

## 3. Effect of each entry kind

| Kind | Balance of `AccountId` | Balance of `ToAccountId` | Income (report) | Expense (report) | Budget |
|---|---|---|---|---|---|
| Income | + Amount | – | + Amount | – | – |
| IncomeReversal | − Amount | – | − Amount | – | – |
| Expense | − Amount | – | – | + Amount (gross) | + Amount |
| Refund | + Amount | – | – | − Amount (net) | − Amount |
| Transfer | − Amount | + ToAmount | – | – | – |
| Adjustment (±) | ± Amount | – | – | – | – |

* Gross expense = Σ Expense; refunds = Σ Refund; **net expense = gross − refunds** (may be negative, REF-03).
* A card purchase is an Expense on the card account; paying the card is a Transfer (ACC-04, AT-07).
* A transfer fee is a separate Expense entry linked by a shared `GroupId` (FIN-02, AT-08).

## 4. Balances and reports (FIN-12, §24)

```
Balance(account, t)   = OpeningBalance + Σ effect(entries of account with Date ≤ t and Date ≥ OpeningDate)
LedgerBalance         = over all entries;  ConfirmedBalance = over Confirmed entries only
Result(period)        = NetIncome(period) − NetExpense(period)
BudgetRemaining       = Limit − NetEligibleExpense;   Usage% = NetEligibleExpense / Limit × 100 only if Limit > 0
```

* Entries dated before an account's opening date are flagged as *outside the opening basis* (FIN-04, IO-12, AT-09)
  and excluded from the balance until the user moves the opening date or confirms them.
* Every view has one filter set (period, accounts, confirmed-only) applied to all its numbers (DASH-01, FIN-14).
* Multi-currency totals are shown per currency; a combined total is shown only when every needed rate exists,
  otherwise the result is marked *incomplete* (FX-02, FX-05).

## 5. Recurrence rules (REC-03..12)

`RecurrenceRule` = Frequency (Once, Daily, Weekly, Monthly, Yearly) × Interval N ≥ 1, Start date (anchor),
Calendar (Gregorian/Persian, fixed at creation), Monthly/Yearly day rule (SpecificDay or LastDayOfMonth),
MissingDayPolicy (LastValidDay – default – or Skip), End (Never, OnDate, AfterCount).

* The k-th occurrence is computed from the anchor (`anchor + k·N units`), never from the previous occurrence, so a
  31st that moved to the 28th returns to the 31st next month (REC-09, AT-19).
* Persian months use `System.Globalization.PersianCalendar` (month lengths 31/30/29-30, leap Esfand) (AT-22, AT-24).
* `AfterCount` counts generated occurrences; skipping or postponing does not add occurrences (REC-06, AT-27).
* Past start dates generate no entries automatically; the user chooses "from today" or reviews past occurrences (REC-07).

## 6. Occurrence lifecycle (REC-13..18)

```
            ┌── settle (confirm / link existing entry) ──► Settled ──(undo)──► Open (AutoPostSuppressed if it was automatic)
Open ───────┼── skip ───────────────────────────────────► Skipped ──(undo)──► Open
 (Future /  ├── cancel (schedule ended) ─────────────────► Cancelled
  Due /     ├── move due date / change amount ───────────► Open (with override)
  Overdue)  └── auto-post (Fixed + complete data) ───────► Settled + entry Unreviewed
```

Future / Due / Overdue are derived from the (overridden) due date and today; they are never stored.
Early confirmation keeps the real payment date on the entry (FIN-07, AT-28). One active settlement per occurrence in
phase 1 (REC-14).

## 7. Automatic posting (REC-19..22)

A processor runs when the app starts or resumes (and after restore): for each active schedule with `AutoPost`,
Fixed amount, active account and complete data, every open occurrence with due date ≤ today and without
`AutoPostSuppressed` gets an Unreviewed entry. Idempotency is guaranteed by the unique index (D-07); running it twice
or concurrently cannot create a second entry (AT-30). Correctness never depends on background execution (REC-22).

## 8. Forecast (FOR-01..09, §24.3)

```
Forecast(t) = LedgerBalance(scope, base date)
            + Σ open expected receipts ≤ t − Σ open expected payments ≤ t
            + net planned transfers across the scope boundary
```

* Occurrences already settled are not added again (FOR-03, AT-42).
* Open overdue occurrences are counted at the base date and labelled as assumptions (FOR-04).
* Unknown amounts or missing rates make the forecast *incomplete* instead of zero (FOR-05, AT-43).
* The daily path and its minimum within the horizon are computed (FOR-08, AT-44).
* Budgets and monthly equivalents are never subtracted (FOR-07).

## 9. Budget (BUD-01..10)

Eligible expense = Expense − Refund for the budget's accounts and categories within the period (budget calendar),
excluding transfers, opening balances and adjustments. A category limit is an analysis of the same expenses and
never adds to the total (AT-40). Zero limit: no percentage, shows the overspent amount (AT-39). Monthly equivalent of
non-monthly schedules = amount × occurrences per year / 12, shown only for comparison (BUD-09, AT-41).

## 10. Invariants enforced by the domain

* Amount > 0 for Income/Expense/Refund/Transfer (FIN-06); zero-amount entries are rejected.
* Transfer: `AccountId ≠ ToAccountId`; `ToAmount` required when currencies differ.
* Σ linked refunds ≤ refundable amount of the original purchase (REF-04, AT-15).
* Archived accounts accept no new entries; their schedules must be moved, paused or ended first (ACC-06).
