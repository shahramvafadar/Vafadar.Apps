# 04 – KPI and report catalog

Status: **Approved by the owner (2026-10-03)**; implemented phase by phase ([06](06-implementation-backlog.md#phases)). KPIs are for the end user, not product analytics. Every KPI has an in-app
explanation ("?") with its definition and a path to the entries behind it ([03](03-ui-ux-design.md) F7). A KPI that
does not support a decision is not added to make the list longer.

## 1. Shared rules for every number

* **One calculator per definition** (`KpiCatalog` in Core, [02 §15](02-product-and-domain-design.md#15-responsibilities-and-limits)).
  Home, reports, charts, PDF and CSV use it with the same scope and give the same result; any scope difference is
  printed next to the number (ZEX-AT36).
* **Scope bar** on every report: period (financial month / week / two weeks / year / custom), calendar, accounts
  (default: accounts in totals; liquidity KPIs: usable accounts), currency (native; converted only when chosen),
  *confirmed only* or *including unreviewed* (default: including, with the unreviewed count), rate/price date.
* **Classification** of entries ([02 §2](02-product-and-domain-design.md#2-vocabulary)): eligible income,
  consumption expense, capital movement, transfer, correction. Transfers, card payments, loan principal, asset
  purchases/sales and adjustments are never income or consumption. Refunds reduce consumption; reimbursements are
  refunds. Goal earmarks and savings transfers are not spending. Price and FX changes are not income.
* **Unknown is not zero:** unknown amounts, rates, prices or opening balances make a result *incomplete* with a
  reason and a count. A zero denominator gives "not available", never 0 % or ∞.
* **Tags overlap:** tag sums are per tag and never added to a total; the tag report says so (ZEX-AT29).
* **Partial periods** are compared same-length (day 1…d against day 1…d of the comparison period) or carry the
  label "so far" (ZEX-AT30).
* "Your spending rose" is never called "personal inflation"; quantity and price data do not exist for that.
* Rounding (for every KPI unless stated): money to the currency's minor unit (half away from zero); percentages with
  0 decimals on cards, 1 in reports; months with 1 decimal.
* Profile: every KPI is computed inside the open local profile only; profiles are never combined.

## 2. KPI dictionary

Each KPI lists: question · definition and formula · included / excluded · source · currency, scope, period ·
zero / negative / unknown · minimum data · rates and prices · Simple / Advanced · where shown · drill-down ·
example · acceptance.

### ZEX-K01 – Budget remaining

| Attribute | Definition |
|---|---|
| Question | How much is left in this period's budget? |
| Formula | `Remaining = (Limit + Carry) − (Σ Expense − Σ Refund)`; `Usage % = NetSpent / (Limit + Carry) × 100` if `Limit + Carry > 0`; `Per day = floor(Remaining / days left including today)` for the current period when `Remaining > 0`. Flex budgets compare the flexible part only (existing) |
| Included | Expenses and refunds of the budget's accounts and categories (sub-categories with their parent) dated in the period |
| Excluded | Transfers, card payments, loan principal, asset purchases/sales, adjustments, opening balances, income |
| Source | Ledger, budget, rollover chain (existing `BudgetCalculator`, `BudgetRolloverCalculator`, `FlexCalculator`) |
| Currency / scope / period | The budget's currency; its account scope; its period (month, week, two weeks), budget calendar and financial month start. Home shows the budget of the default account's currency (ZEX-P02) |
| Zero / negative / unknown | Limit 0: no percentage, "spent 40 EUR of a 0 EUR limit"; negative remaining: "over by 75 EUR" in red; no budget: no card (never 0) |
| Minimum data | A budget for the period |
| Rates / prices | None (native) |
| Simple / Advanced | Same number. Simple: overall remaining and per day; Advanced adds categories, method details. *Confirmed only* (Advanced toggle) applies to Home, page and alerts alike (fix) |
| Where | Home budget group, Budget page, R1 |
| Drill-down | Budget page › category › Transactions (period, accounts, categories, currency) |
| Example | Limit 1,000, carry 50, expenses 820, refunds 20 → remaining **250.00**; 12 days left → **20.83 per day**; usage 76 % |
| Acceptance | Given the example, when Home and the Budget page show October, then both show 250.00 EUR and 20.83 per day; with *confirmed only* and one unreviewed 30 EUR expense both show 280.00 |

### ZEX-K02 – Lowest projected balance and its date

| Attribute | Definition |
|---|---|
| Question | Will I run short before my next income? |
| Formula | `Min = min over t in (today, horizon] of Liquidity(t)` (§3), `MinDate` = first day it occurs |
| Included | Usable accounts (ZEX-P17); recorded future entries; open plan occurrences; overdue occurrences at today; explicit essential-spending estimate if set |
| Excluded | Budgets, monthly equivalents, earmarks (not money movements), holdings, debts, receivables, credit limits |
| Source | `Forecast` (existing) with the usable scope |
| Currency / scope / period | Per currency; usable accounts; horizon end of financial month / 30 / 90 days |
| Zero / negative / unknown | Negative minimum shown as negative; unknown plan amounts: "incomplete (2 unknown amounts)" |
| Minimum data | At least one usable account |
| Rates / prices | None (native per currency) |
| Simple / Advanced | Simple: only a warning in *Needs attention*: when `Min < 0` "Your balance may fall below zero around 29 Oct" (`Home_BalanceBelowZero`); when `Min ≥ 0` but the headroom (§3) is below 0 "Your plans would use protected money around 24 Oct" (`Home_HeadroomProtected`); Advanced: card with end balance, minimum and date, path |
| Where | Home attention / commitments group, Forecast, R2 |
| Drill-down | Forecast list of the items up to the minimum date |
| Example | Base 2,500 at the end of 30 Sep; rent −950 (3 Oct); card payment −400 (10 Oct); salary +2,800 (25 Oct); essential 20/day → minimum **670.00 on 24 Oct** |
| Acceptance | Given the example, then the minimum is 670.00 on 24 Oct, the card purchase of 30 Sep (on the card account) is not subtracted again (ZEX-AT31) |

### ZEX-K03 – Goal progress

| Attribute | Definition |
|---|---|
| Question | How far is my goal? |
| Formula | [02 §8.4](02-product-and-domain-design.md#84-progress-formulas): `Remaining = max(0, T − F)`, `Progress = clamp(F/T)`, `Overshoot = max(0, F − T)` |
| Included | Balance goal: account balance; earmark goal: covered earmarks; quantity goal: holding quantity |
| Excluded | Price changes for quantity goals; income from transfers |
| Source | `GoalProgressService` |
| Currency / scope / period | The goal's currency or unit; current |
| Zero / negative / unknown | Negative balance: 0 %, "balance −120 EUR"; overshoot shown "+250 EUR above target" |
| Minimum data | An active goal |
| Rates / prices | None |
| Simple / Advanced | Same; Simple shows current/target/remaining/%; Advanced adds contribution history |
| Where | Home goals group, Goals, R3 |
| Drill-down | Goal detail › contributions or account movements |
| Example | Target 5,000, balance 2,000 → **40 %, 3,000 remaining**; withdrawal 500 → **30 %, 3,500** |
| Acceptance | ZEX-AT18/AT19 |

### ZEX-K04 – Open commitments in the next 30 days

| Attribute | Definition |
|---|---|
| Question | Which payments are coming and how much are they? |
| Formula | `Σ outstanding` of open outgoing occurrences (expense plans and transfers to non-usable accounts such as loan installments and card payments) with due date ≤ today + 30, overdue included; split *fixed*, *estimated (≈)*, *unknown (count)* |
| Included | Open and partly paid occurrences (outstanding part only) |
| Excluded | Settled and skipped occurrences; plans inside the usable scope (transfer between usable accounts) |
| Source | Occurrences (existing), forecast items |
| Currency / scope / period | Per currency and account; rolling 30 days |
| Zero / negative / unknown | Nothing due: "No payments in the next 30 days"; unknown amounts listed with "?" and never summed as zero |
| Minimum data | One active plan |
| Rates / prices | None |
| Simple / Advanced | Simple: total and the next 3 items; Advanced: full list, per account |
| Where | Home commitments group, Plans, R2 |
| Drill-down | Plans › Due & overdue / Upcoming |
| Example | Rent 950 (fixed), phone ≈ 30, insurance unknown, loan 450 partly paid 200 (250 outstanding) → **1,200.00 + ≈ 30 + 1 unknown** |
| Acceptance | Given the example, then the total is 1,200.00 fixed, 30 estimated, 1 unknown; the 200 already paid is not counted again |

### ZEX-K05 – Operating income surplus

| Attribute | Definition |
|---|---|
| Question | Did I earn more than I spent? |
| Formula | `Surplus = EligibleIncome − ConsumptionExpense` where `EligibleIncome = Income − IncomeReversal`, `ConsumptionExpense = Expense − Refund` (interest and fees are expenses and count once) |
| Included | Accounts in totals (or the chosen scope), the period |
| Excluded | Transfers, loan principal, asset purchases and sales, adjustments, opening balances, valuation changes |
| Source | `LedgerCalculator.Totals` extended by `EntryClassification` |
| Currency / scope / period | Per currency; scope bar; period |
| Zero / negative / unknown | Negative shown as deficit "−450 EUR" |
| Minimum data | Entries in the period |
| Rates / prices | Native; converted only on request |
| Simple / Advanced | Simple (in the period overview) |
| Where | Home period group (existing "result"), R1 |
| Drill-down | Income list, expense list |
| Example | Income 3,000, expenses 2,400, refunds 100 → **+700.00** |
| Acceptance | Given a 1,000 EUR asset purchase in the period, then the surplus is unchanged (ZEX-AT14) |

### ZEX-K06 – Surplus rate

| Attribute | Definition |
|---|---|
| Question | Which share of my income was left? |
| Formula | `K05 / EligibleIncome × 100`, numerator K05, denominator eligible income, only when `EligibleIncome > 0` |
| Included | Same as K05 |
| Excluded | Same as K05; not a measure of wealth change, price growth or money moved to savings |
| Source | `KpiCatalog` over `LedgerCalculator.Totals` (existing formula in `ReportsViewModel.cs:387-391`, moved to Core) |
| Currency / scope / period | Per currency; scope bar; period; open profile |
| Zero / negative / unknown | Income 0 → "not available" (ZEX-AT32); negative surplus → negative rate |
| Minimum data | Eligible income in the period |
| Rates / prices | Native; never averaged across currencies |
| Simple / Advanced | Advanced (R1 card); the existing label "Share of income kept" stays with a "?" explanation |
| Where | R1 |
| Drill-down | Income and expense lists of the period |
| Example | 700 / 3,000 → **23 %** (23.3 % in reports) |
| Acceptance | Given income 0 and expenses 450, then the rate shows "not available" and the surplus −450.00 |

### ZEX-K07 – Essential expense coverage

| Attribute | Definition |
|---|---|
| Question | How many months could I cover essential costs with the money I can use? |
| Formula | `Months = SelectedUsableMoney / EssentialMonthly`, `EssentialMonthly = median` of the last 3 complete financial months of consumption in categories marked *essential*, plus monthly equivalents of essential non-monthly plans |
| Included | Usable accounts chosen for this KPI (default: all usable); categories marked essential (defaults: housing, groceries, energy, phone & internet, insurance, transport, health) |
| Excluded | Holdings, debts, receivables, credit limits; protected money is shown separately ("of which protected 1,000") |
| Source | Ledger, categories (`IsEssential`, new), plans |
| Currency / scope / period | One currency at a time; usable accounts; the last 3 complete financial months; open profile |
| Zero / negative / unknown | Essential 0 → "not available" (never ∞); negative usable money → 0.0 months with "balance negative" |
| Minimum data | 3 complete months with essential spending, otherwise "not enough history" |
| Rates / prices | Native |
| Simple / Advanced | Advanced; also shown inside an emergency-fund goal |
| Where | R3, emergency goal detail |
| Drill-down | Essential categories of the 3 months |
| Example | Usable 6,000, essential months 1,450 / 1,500 / 1,620 → median 1,500 → **4.0 months** |
| Acceptance | Given the example, then 4.0 months; with essential spending 0 then "not available" |

### ZEX-K08 – Fixed and non-monthly commitments (12 months)

| Attribute | Definition |
|---|---|
| Question | What do my regular obligations cost over a year? |
| Formula | `Σ` expected amounts of active expense plans with due dates in the next 12 months, grouped by spending type (fixed, non-monthly); per plan its monthly share = amount × occurrences per year / 12 (existing BUD-09) |
| Included | Active expense plans (fixed and estimated amounts), contracts |
| Excluded | Unknown amounts (counted separately), ended plans, skipped occurrences, income plans |
| Source | Plans and occurrences (`BudgetPlanning`, `Recurrence.PerYear`), category spending type |
| Currency / scope / period | Per currency; all plan accounts; rolling 12 months; open profile |
| Zero / negative / unknown | No plans → "No regular payments yet"; unknown amounts listed with "?" |
| Minimum data | One active expense plan |
| Rates / prices | Native |
| Simple / Advanced | Simple: two totals; Advanced: per plan and month |
| Where | R2 |
| Drill-down | Plan detail |
| Example | Rent 950 × 12 = 11,400 (fixed); car insurance 600 yearly → 600 (share 50.00); phone ≈ 30 × 12 = ≈ 360 → **fixed 11,400 · non-monthly 600 · ≈ 360** |
| Acceptance | Given the example, then the totals match and the monthly share is labelled "share, not money set aside" |

### ZEX-K09 – Debt service burden

| Attribute | Definition |
|---|---|
| Question | Which part of my income goes to loan installments? |
| Formula | `(Σ scheduled principal + interest of installments in the period) / EligibleIncome × 100`, only when income > 0 |
| Included | Loan accounts with an installment plan (new link loan ↔ plan, ZEX-S0607) |
| Excluded | Card purchases; principal is not consumption (it stays out of K05) |
| Source | Loan accounts (`Installment`, `InterestRate`), `LoanCalculator.Split`, installment plans |
| Currency / scope / period | Per currency; loan accounts of the scope; the period; open profile |
| Zero / negative / unknown | No income → "not available"; installment without schedule → "add the payment day" |
| Minimum data | One loan with an installment plan |
| Rates / prices | Native |
| Simple / Advanced | Simple: "next installment 450 EUR on 5 Nov"; Advanced: ratio |
| Where | R2, loan account detail |
| Drill-down | Installment entries and the schedule |
| Example | Installments 450 (380 principal + 70 interest), income 3,000 → **15 %** |
| Acceptance | Given the example, then 15 % and K05 subtracts only the 70 interest |

### ZEX-K10 – Net worth

| Attribute | Definition |
|---|---|
| Question | What am I worth today, and what is it made of? |
| Formula | Per currency at date t: `money + receivables + valued assets + holdings value − debts` (card balances are debts when negative); converted total only on request with `RateInfo` |
| Included | All non-archived accounts plus archived ones with a non-zero balance; holdings at their latest price ≤ t |
| Excluded | Credit limits, earmarks (they are inside money), budgets |
| Source | Balances (`LedgerCalculator`), `AssetValuationService`, `RateTable` |
| Currency / scope / period | Per currency, or the valuation currency on request; all accounts and holdings of the open profile; a date (default today) |
| Zero / negative / unknown | Negative net worth shown as negative; holdings without price → "incomplete: 2 holdings without a price"; missing rate → converted total incomplete |
| Minimum data | One account |
| Rates / prices | Prices and rates dated on or before t; their dates shown |
| Simple / Advanced | Advanced (R4); Simple users with holdings see quantities and known values in Accounts |
| Where | R4, R5 |
| Drill-down | Group › accounts / holdings |
| Example | Money 2,980 EUR, holding 10 g valued 1,000 EUR, loan −4,000 EUR, lent 300 EUR → **280.00 EUR** |
| Acceptance | Given a 10 g holding without price, then net worth is "incomplete" and the 10 g are listed |

### ZEX-K11 – Spending pattern change

| Attribute | Definition |
|---|---|
| Question | Where did my spending change? |
| Formula | Per category or payee: `Δ = Net(current) − Net(comparison)`; `Δ % = Δ / Net(comparison) × 100` only when comparison > 0; comparison = previous comparable period, same length when the current one is partial |
| Included | Consumption expense (expense − refunds) of the scope |
| Excluded | Same as K05; aggregated entries are compared only with aggregated or complete periods (labelled) |
| Source | `LedgerCalculator.ExpenseByCategory`, payees |
| Currency / scope / period | Per currency; scope bar; current vs previous comparable period in the chosen calendar and financial month; open profile |
| Zero / negative / unknown | Comparison 0 → "new"; both 0 → not shown; net negative (more refunds) shown as negative |
| Minimum data | Two comparable periods with entries |
| Rates / prices | Native |
| Simple / Advanced | Simple: top 3 changes in R1; Advanced: full table, payees |
| Where | R1 |
| Drill-down | Entries of the category in both periods |
| Example | Groceries days 1–12: 180 vs 150 → **+30.00 (+20 %)** |
| Acceptance | Given day 12 of October, then October 1–12 is compared with September 1–12, not with all of September |

### ZEX-K12 – Open receivables and their age

| Attribute | Definition |
|---|---|
| Question | Who owes me what, and since when? |
| Formula | Open reimbursements (`reimbursable − linked refunds`) + Lent balances, per counterparty; age = today − expense date (reimbursement) or the last lending date; *overdue* when an optional due date (new field) has passed |
| Included | Open reimbursable amounts, Lent accounts with a positive balance |
| Excluded | Paid back amounts; never part of usable money until received |
| Source | `EntryActions.OpenReimbursements`, Lent accounts, `Counterparty`, `ReimbursedBy` |
| Currency / scope / period | Per currency; all accounts; as of today; open profile |
| Zero / negative / unknown | Nothing open → "Nobody owes you money"; unknown due date → age only |
| Minimum data | One open receivable |
| Rates / prices | Native |
| Simple / Advanced | Simple: total and count, "record payment"; Advanced: aging (0–30, 31–90, > 90 days), counterparties |
| Where | Accounts › Owed to me, Home attention (existing), R2 |
| Drill-down | The open expense or the Lent account |
| Example | Hotel 180 (12 days), Sara 300 lent (95 days) → **480.00, 1 older than 90 days** |
| Acceptance | Given the example, then 480.00 with one item older than 90 days, and K02 is not raised by it |

### ZEX-K13 – Currency and asset composition

| Attribute | Definition |
|---|---|
| Question | In which currencies and assets is my wealth? |
| Formula | Share of each currency / holding group in the valuation currency at date t, over the **known** part; unknown parts listed separately with their quantity |
| Included | Positive balances of money accounts, receivables, valued assets, valued holdings |
| Excluded | Debts (shown as a separate bar), unvalued holdings and currencies without a rate (listed, not in shares); grams and money never in one chart without valuation |
| Source | K10 parts, `RateTable`, `AssetValuationService` |
| Currency / scope / period | The valuation currency; all accounts and holdings of the open profile; a date |
| Zero / negative / unknown | Nothing valued → "Add rates or prices to see the composition" |
| Minimum data | Two parts with a known value |
| Rates / prices | Dated on or before t, dates shown |
| Simple / Advanced | Advanced |
| Where | R4 |
| Drill-down | Part › accounts / holdings |
| Example | 2,000 EUR, 4,000 USD at 0.92 = 3,680 EUR, gold 1,000 EUR → EUR 30 %, USD 55 %, gold 15 %; "not valued: 5 coins" |
| Acceptance | Without a USD rate the chart shows EUR and gold only and lists USD as "no rate" |

### ZEX-K14 – Data status of a report

| Attribute | Definition |
|---|---|
| Question | Can I rely on this number? |
| Content | A list, never a score: unreviewed entries in scope; entries without category; unknown plan amounts in the horizon; missing, estimated or outdated rates used; holdings without price; accounts with unknown opening balance; last reconciliation per account (new date field) older than 60 days; last backup older than 30 days; possible duplicates; aggregated entries in the period; entries before an opening date |
| Source | Each calculator returns its incompleteness reasons; reconciliation dates; backup history |
| Currency / scope / period | The scope of the report it belongs to; open profile |
| Zero / negative / unknown | No issue → "No open issues for this view" |
| Minimum data | – |
| Rates / prices | Lists their dates |
| Simple / Advanced | Always visible (footer of every report, R6 in full) |
| Where | Every report footer, R6, Home attention (at most the most important item) |
| Drill-down | Each item opens the list or screen that fixes it |
| Example | "3 unreviewed · 1 unknown amount · USD rate from 12 Aug" |
| Acceptance | No "98 % accurate" style number exists anywhere; every listed item opens its fix |

## 3. Liquidity headroom contract

An estimate of money the user could still spend **under the current assumptions** – never "safe to spend".

```text
Liquidity(t) = U(base)                                   usable balance at the end of today
             + Σ receipts with date in (today, t]        recorded future entries + open occurrences
             − Σ payments with date in (today, t]        recorded future entries + open occurrences
             − overdue open payments (counted at today, labelled as assumption)
             + overdue open receipts (counted at today, labelled)
             − E × days(today, t]                         explicit essential-spending estimate, if set
Headroom     = min over t in (today, horizon] of (Liquidity(t) − Protected)
```

Rules:

* **Base:** `U(base)` already contains every entry dated ≤ today; only entries and occurrences dated after today are
  added, so nothing is counted twice. Settled occurrences are in the ledger, not in the plan list.
* **Scope:** usable accounts of one currency (ZEX-P17). A transfer between usable accounts nets out; a transfer to a
  non-usable account (card payment, loan installment, savings account marked not usable) is a payment; the reverse is
  a receipt. Card purchases stay on the card and are never subtracted again; the card payment is (AT31).
* **Essential spending estimate `E`:** set explicitly by the user per day, week or month (Advanced); Zanance may
  *suggest* the median daily consumption of the last 3 months excluding plan-settled entries; without `E` the result
  says "Day-to-day spending not included".
* **Protected:** covered earmarks of goals marked *Protect this money* in usable accounts of the currency, counted
  once (ZEX-P16, AT24). Only *Money set aside* goals can be protected; a balance goal reserves nothing and is never
  subtracted. Budgets reserve nothing. Holdings, receivables, credit limits and non-usable accounts are
  never sources of payment.
* **Unknown amounts** make the result *incomplete* with their count (AT37). Negative headroom is shown as negative.
* **Label:** "Estimate with your current plans and assumptions"; tapping explains the parts.

**Example:** base 2,500 EUR at the end of 30 Sep; rent −950 on 3 Oct; card payment −400 on 10 Oct; salary +2,800 on 25 Oct; `E` = 20/day;
1,000 set aside for the goal *Car repair* (money set aside, marked *Protect this money*). Liquidity on 24 Oct = 2,500 − 950 − 400 − 24 × 20 = **670**; Headroom = 670 − 1,000 =
**−330 EUR around 24 Oct**: "Your plans would use 330 EUR of the money you protected, around 24 Oct."

## 4. Report packages

| Id | Package | Question | Sections | KPIs | Simple / Advanced | Existing basis |
|---|---|---|---|---|---|---|
| ZEX-R1 | Period overview | Where did my money go and what is left? | Income vs consumption (transfers and capital movements as separate lines), result, budget, top changes, data status | K01, K05, K06, K11, K14 | Simple: summary, top 3 changes; Advanced: categories, payees, tags, comparison table | Income & expense, expenses by category, trend (extend) |
| ZEX-R2 | Commitments and cost of living | Which payments must I not forget? | Due calendar 30/90 days, yearly and quarterly bills, cancellation deadlines, debt installments, receivables, shortfall warning | K02, K04, K08, K09, K12 | Simple: next 30 days, warnings; Advanced: 12 months, monthly shares | Plans, contracts, plans vs actual (extend) |
| ZEX-R3 | Goals and capacity | How far are my goals and what is needed? | Progress per goal, contributions actual vs plan, shortfall, ETA with assumptions, capacity and suggested split | K03, K07, capacity | Simple: progress and next step; Advanced: trend, capacity, alternatives | Goals page (extend) |
| ZEX-R4 | Holdings and balances | How much money, gold or other assets do I have? | Groups by currency and quantity, debts, valuations with dates, fine-metal line, realised and unrealised results with basis status | K10, K13 | Simple (when holdings exist): quantities and known values; Advanced: valuation, basis, composition | Accounts report (extend), new holdings |
| ZEX-R5 | Wealth change | Did my wealth grow through saving or through prices and rates? | Net worth over time; decomposition into flows, price effect, FX effect, corrections, unexplained remainder; forecast snapshots vs reality | K10 history | Advanced | New (E08) |
| ZEX-R6 | Review and data quality | Which numbers can I rely on? | Unreviewed, possible duplicates, without category, unknown amounts, outdated rates, holdings without price, reconciliation dates, backup status, aggregated overlaps | K14 | Always available in both modes | Pieces exist (extend) |

Cash flow, consumption and wealth change are three different views that reconcile: **change of money =
surplus (K05) + capital inflows − capital outflows + debt and receivable movements + corrections**; **change of
wealth = surplus + price effect + FX effect + corrections**, with any remainder shown.

### 4.1 Wealth change decomposition (ZEX-P23)

For two dates `a < b`, per currency (or in the valuation currency with dated rates):

```text
ΔNW          = NW(b) − NW(a)
Flows        = surplus (K05) over (a, b]                     income − consumption
PriceEffect  = Σ holdings: Σ over price changes of q(t) × Δprice   (quantity held at each price change)
FXEffect     = Σ foreign balances: balance(t) × Δrate          (only for the converted view)
Corrections  = adjustments and quantity corrections
Remainder    = ΔNW − (Flows + PriceEffect + FXEffect + Corrections)   shown, never hidden
```

A remainder appears for example when an opening balance is unknown or a valuation is missing on one of the dates;
the report names the cause when it can.

### 4.2 Forecast snapshot vs reality (ZEX-P22)

Shows the saved path, the actual path of the same scope recomputed today, their daily difference, and the
contributions to the difference: *recorded later* (entries dated before the snapshot but entered after it),
*plans changed*, *unplanned spending*, *unplanned income*. The snapshot itself never changes (ZEX-AT40).

## 5. Parity of Home, reports, PDF and exports

| Number | Home | Report | PDF | CSV |
|---|---|---|---|---|
| Native balances | yes | R4 | R4 section | – (entries) |
| Budget remaining | yes | R1 | R1 | – |
| Surplus | period group | R1 | R1 | derivable from entries with the same filter |
| Commitments 30 days | commitments group | R2 | R2 | – |
| Goal progress | goals group | R3 | R3 | – |
| Net worth | – | R4/R5 | R4/R5 | holdings export |
| Data status | attention | R6 + footers | footer per section | – |

Each PDF section prints its own scope (fixes ZEX-F10); drill-downs pass the currency (fixes ZEX-F09).
