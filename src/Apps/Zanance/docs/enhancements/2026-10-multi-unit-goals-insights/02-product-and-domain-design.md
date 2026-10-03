# 02 – Product and domain design

Status: **Approved by the owner (2026-10-03)**; implemented phase by phase ([06](06-implementation-backlog.md#phases)). Proposals that change the meaning of numbers or the stored data are marked
`ZEX-Pxx` and listed in [08](08-decisions-and-approval.md). Current behaviour and its evidence are in
[01](01-current-state-and-gaps.md); this document only designs what changes and the rules that must hold.

## 1. Principles

1. **Answer four questions:** what do I have; what is ahead; how much budget is left; how far is my goal and when
   could I reach it, under which assumption.
2. **Never mix units.** Money is a currency amount, a holding is a quantity of an asset, a value is a dated money
   amount for a quantity. Units are added only when they are the same unit.
3. **Unknown is not zero.** A missing rate, price, amount or opening balance makes a result *incomplete* with its
   reason; it is never filled with zero or a guess.
4. **One definition per number.** Every KPI, report, Home card, chart, PDF and export uses the same calculator in
   `Vafadar.Zanance.Core` for the same scope ([04](04-kpi-and-report-catalog.md)). View models only present.
5. **Presentation is not calculation.** Simple/Advanced, theme, language, calendar display and display units
   change how a number looks, never the number ([05](05-simple-advanced-matrix.md)).
6. **Nothing silently changes the meaning of stored data.** No relabelling of amounts, no automatic conversion, no
   history rewritten by today's rate or price.
7. **Local-first stays.** Everything works offline, without an online account, in fa/de/en, Gregorian/Persian, with
   Undo and full backup/restore. No new permission, service or dependency is needed for this package.

## 2. Vocabulary

| Term | Meaning in Zanance |
|---|---|
| Money account | Cash, Checking, Savings, CreditCard (existing types) – holds an amount in one currency |
| Debt / receivable account | Loan (I owe), Lent (owed to me) – existing |
| Valued asset (legacy) | An existing `Asset` account: a money value only, revalued by adjustments – stays as is |
| Holding | **New:** a quantity of an asset type at a location (20 g of 18 k gold in the home safe) |
| Asset type | **New:** the identity of what is held (18 k gold, 1 oz silver bar, Bahar Azadi coin, camera) |
| Valuation | **New:** a dated price per unit or total value of a holding in a currency, entered by the user |
| Native total | Sum of balances that share a currency (2,000 EUR) – the primary total |
| Converted total | A native total converted into the valuation currency with dated rates – secondary, optional |
| Usable money | Balances of accounts marked *Usable for payments* (ZEX-P17) – the liquidity scope |
| Net worth | Money + receivables + valued assets and holdings − debts, per currency first, converted only on request |
| Eligible income | Income − income reversals; excludes transfers, loan principal, asset sales, refunds and reimbursements |
| Consumption expense | Expense − refunds; excludes transfers, card payments, loan principal, asset purchases, adjustments |
| Capital movement | **New entry kinds** *Asset purchase* and *Asset sale* (ZEX-P07): money ↔ holdings, never income/expense |
| Earmark | Existing `GoalAllocation`: money of an account set aside for a goal; no ledger effect |
| Protected money | Covered earmarks of goals marked *Protect this money* (ZEX-P16) – kept out of headroom |

## 3. Defaults and settings

Three concepts, three settings, all **per profile** (stored in the profile's database `Settings` row, therefore in
every backup):

| Setting | Used for | Never used for | Initial value |
|---|---|---|---|
| **Default currency for new items** (new, ZEX-P01) | Preselected currency of a new account, goal, budget, asset price, display-unit picker, rate form | Existing data; the currency of an entry (that is always its account's) | Onboarding: the currency chosen for the first account; migration: today's report currency |
| **Default account for new entries** (existing `DefaultAccountId`) | The account of quick add, widget, receipt, templates without an account | Reports, filters, the valuation currency | Onboarding: the first account; unchanged by the migration |
| **Valuation currency** (existing `ReportCurrencyCode`, renamed in the UI, ZEX-P01) | Converted totals and converted charts, net worth in one currency, composition charts | New items, budgets on Home, quick add | Unchanged |

Rules:

* Changing one setting never changes another, and never changes stored amounts or currencies (ZEX-MC02, AT02).
* Home and the Budget page show the budget in the **currency of the default account**; when budgets exist in several
  currencies a currency switcher appears; the choice is remembered per profile (ZEX-P02). Alerts are evaluated for
  every budget, not only one currency.
* The valuation currency may be *Off* (Advanced): then no converted totals are shown anywhere and composition charts
  ask for a currency.
* Settings → *Money and months* shows the three rows with "?" help and an example each ([03](03-ui-ux-design.md) F1).

## 4. Account selection contract

One contract for every path that opens the entry editor: Home quick add (Expense, Income, Transfer), the "+" button,
quick templates, the Android widget, a receipt, an account's detail page (*Add entry here*, new), a plan
occurrence, a duplicate, a refund and an edit.

### 4.1 Precedence

1. **Explicit user choice** in the form – always wins, even if a later default changes.
2. **Context account** – the template's account, the account page the user came from, the occurrence's plan
   account, the original entry of an edit, duplicate or refund.
3. **Default account** – only if it is *valid*: exists, not archived, a money account (Cash, Checking, Savings,
   CreditCard), and for refunds in the purchase's currency.
4. **Nothing** – the editor opens with no account selected; the account field is highlighted with "Choose the account
   for this entry"; Save stays disabled until an account is chosen (ZEX-MC06, AT05, ZEX-P05). No silent fallback to
   the first account.

A chart filter, a report scope or the last viewed account never changes the account of a new entry (ZEX-MC04).
Holdings and debts are never the default account (ZEX-MC06); an expense on a Loan, Lent or Asset account is possible
only by choosing it explicitly, and the editor explains the effect.

### 4.2 What the form shows

* The account chip (name, currency, icon) sits directly under the amount, also in Simple mode and also when only one
  account exists (read-only then). The currency code next to the amount is the account's currency (ZEX-MC05).
* Save is labelled with the effect for transfers ("Transfer 100 EUR → 110 USD"); for income/expense the account and
  amount are visible above the button without scrolling on a 412 × 892 phone.
* Nothing changes any balance until Save; leaving the form keeps nothing (ZEX-MC03).

### 4.3 Changing the account when the currency differs (ZEX-P04)

* The typed digits stay; their unit becomes the new account's currency; an inline notice appears under the amount:
  "The amount is now in USD (was EUR). Check it before saving." with *Undo account change*.
* No conversion is performed. The optional *foreign amount* (original purchase currency) is kept unchanged.
* Display units: the digits are re-read in the new account's display unit; when only one of the two currencies has a
  display unit the notice names it ("now in rial, was toman").
* Fee and destination amount of a transfer are cleared when their currency changes, with the same notice.

### 4.4 Invalid defaults and context

* Archiving the default account clears `DefaultAccountId` and shows "No default account – choose one in the account
  list" with an action; the next quick add follows §4.1 step 4.
* A template whose account is archived or deleted opens with the template's category and title but **without amount
  and account**, and a notice "The template's account is archived – choose an account and the amount" (fixes the
  amount reinterpretation found in the review).
* A receipt amount is parsed in ISO units of the chosen account (never through a display unit) and shown with the
  account's currency; when the receipt names a different currency the editor puts the receipt amount into *foreign
  amount* and leaves the account amount empty.

### 4.5 Account currency lock (ZEX-P03)

An account's currency is locked as soon as any stored amount is in it: ledger entries (as source or destination),
plans (amount or destination amount), quick templates, goal earmarks, the loan installment, asset purchases/sales or
budgets with this account in scope. The editor explains which items lock it and offers *Create an account in another
currency* followed by a transfer.

## 5. Totals, rates and conversion

### 5.1 Account groups

| Group | Accounts | In the main totals | In usable money (liquidity) | In net worth |
|---|---|---|---|---|
| Money | Cash, Checking, Savings | if *Include in totals* | if *Usable for payments* (default on) | yes |
| Credit cards | CreditCard | if *Include in totals* (as a negative balance) | never (ZEX-MC12) | yes (debt) |
| Debts | Loan | no (default off) | never | yes (negative) |
| Receivables | Lent; open reimbursements are listed with them | no | never (until received) | yes |
| Valued assets | Asset (legacy) | no (default off) | never | yes, at the recorded value |
| Holdings | new holdings (§7) | never in money totals | never | yes, when valued; otherwise *incomplete* |

* *Usable for payments* (ZEX-P17) is a new per-account flag; *Country* (ZEX-P18) is an optional per-account field.
  Neither is inferred from language, currency or location (ZEX-MC11).
* Archived accounts leave the main totals (they stay in history and reports of past periods) – this aligns Home with
  the Accounts page.
* Asset/Loan/Lent accounts with *Include in totals* switched on are shown in their own group even then; they never
  mix into the money total.

### 5.2 Native totals first

Main totals are always per currency: accounts 1,250 EUR + 750 EUR, 4,000 USD, 100,000,000 IRR → **2,000.00 EUR ·
4,000.00 USD · 100,000,000 IRR** (ZEX-MC07, AT01). A converted total is secondary: smaller type, below the native
totals, prefixed "≈", with the valuation currency and the rate date (ZEX-MC08).

### 5.3 Rate selection

Existing `RateTable` stays the only converter: the latest rate on or before the valuation date, inverse used when
newer, no 1:1 assumption, no triangulation. Extensions:

* The **valuation date** is part of every converted result: today for current totals, the period end for reports,
  each day for historical net worth.
* A result carries `RateInfo` per source currency: date, `IsEstimate`, *outdated* flag (ZEX-P06), missing.
* Missing rate → the converted total is *incomplete* and lists the currencies without a rate; the native totals stay
  complete (AT07).
* **Freshness (ZEX-P06):** *may be outdated* when the rate is older than the chosen number of days before the
  valuation date (Advanced setting: 7 / 30 / 90 days / never; default 30) **and** was entered before the start of the
  current financial month – a rate entered for the running month is never flagged (`RateFreshness`, implemented in
  phase 1; this makes every option of the setting meaningful). It is a reminder to update, not a claim about accuracy;
  the label says "rate of 12 Aug – may be outdated".
* Historical valuation never uses a rate dated after the valuation date (ZEX-AS13).

### 5.4 Multi-currency budgets, reports and charts

* Budgets stay per currency (existing). Reports compute per currency; a chart combining currencies is drawn only in
  a chosen currency with explicit conversion and its rate dates, or with a currency selector (ZEX-MC07 note).
* Spending of several currencies is never summed without conversion.

## 6. Transfers, fees, display units and aggregated entries

### 6.1 Cross-currency transfer (ZEX-MC10)

Existing model kept: one entry with `Amount` (source) and `ToAmount` (destination), fee as a linked Expense
(category Fees) in the source currency. Extensions:

* The editor shows the implied rate ("1 EUR = 1.1000 USD, from your amounts") and the effect line
  "−102.00 EUR from Main, +110.00 USD to Dollar account, fee 2.00 EUR" before Save (AT06).
* An optional *fee in the destination currency* (Advanced) is a second linked Expense on the destination account.
* The actual amounts recorded take precedence over any stored rate; Zanance never creates an FX gain or loss.

### 6.2 Credit cards and loans

Unchanged: a card purchase is an Expense on the card, paying the card is a Transfer (AT31). Loan principal is a
Transfer, interest an Expense. These definitions feed [04](04-kpi-and-report-catalog.md).

### 6.3 Display units (toman) (ZEX-MC09)

Unchanged meaning: a display unit is a factor for display and input of an ISO currency; stored amounts, CSV, PDF,
rates and backups stay in ISO units, and CSV/PDF headers name the stored unit ("Amount (IRR)"). Changes:

* Stored per profile in the database settings (ZEX-P20), so they are in backups and do not leak between profiles.
* Every input that may use a display unit shows the unit name at the field ("toman"); the receipt path never applies
  a display unit (§4.4).

### 6.4 Aggregated entries (ZEX-P21)

* An entry may be marked **Aggregated** with a covered date range (for example "Groceries, September, 412 EUR").
  Aggregated entries are normal expenses or income in totals and budgets.
* KPIs that need timing (daily path, average daily spending, weekday patterns) treat the amount as *spread unknown*
  and say so.
* When detailed entries are added or imported for the same account, kind and category inside the range, an overlap
  notice offers **Link and replace** (the detailed entries replace the aggregated amount; the aggregate keeps the
  difference or is removed, after a preview, with Undo) or **Keep both** (with "this counts the money twice"). No
  entry is deleted without confirmation (AT33).

## 7. Quantity holdings

### 7.1 Model

Three independent layers (ZEX-D06):

1. **Asset type** – identity: name, kind (*Precious metal*, *Coin or bar*, *Countable item*, *Other*), dimension
   (*Mass* or *Count*), metal (gold, silver, platinum, palladium, other – for precious metals and coins/bars),
   purity (optional), unit weight (optional, count types only), count unit name (for count: "coin", "bar",
   "piece", user-defined), divisible (count types: whether fractions are allowed), price currency (default:
   *Default currency for new items*), note, archived.
2. **Holding** – the quantity of one asset type at one **location** (new small entity: name such as "Home safe",
   "Bank box", "Dealer account"; a default location "Main" exists). Its quantity is the running sum of its events.
3. **Valuation** – optional dated price for an asset type: price per base unit or total value of a holding, currency,
   source (manual, from a purchase), note.

A holding is not an account and never appears in money totals; it appears in holdings, net worth and its reports.

### 7.2 Units, precision and purity (ZEX-P10)

| Dimension | Stored unit | Display units | Precision | Rules |
|---|---|---|---|---|
| Mass | milligram (`long`) | g, kg | display up to 3 decimals of g; input up to 3 decimals | 1 kg = 1000 g exactly (ZEX-AS01) |
| Count | thousandth of a unit (`long`) | the type's count unit | indivisible types: whole units only; divisible: up to 3 decimals | 3 coins = 3000 milli-units |

* **Purity** in parts per 10,000 (`int`): 24 k / 999.9 = 9999, 22 k = 9167, 21 k = 8750, 18 k = 7500, 14 k = 5833;
  free input as karat or fineness, stored as the exact fraction the user entered. Without purity, gross mass is kept
  and no fine (pure) mass is shown (ZEX-AS03).
* **Unit weight** (count types, optional): mass per unit in mg; derived mass = count × unit weight. The holding still
  has one quantity (the count); weight is a derived view, never a second holding (ZEX-AS05, AT12).
* Limits: `long` mg allows 9.2 · 10⁹ kg; all arithmetic is checked; overflow is an error, never a wrap.

### 7.3 What can be added up

* Quantities add only within the **same asset type** (same identity, same dimension): 20 g + 0.03 kg of the same
  type = 50 g (AT09).
* Different types are listed separately: 20 g gold and 30 g silver are two lines (AT10); 18 k and 24 k gold are two
  types (AT11).
* **Fine-metal equivalent** is a separate, labelled line per metal: Σ (mass × purity) over types with known purity,
  e.g. 20 g × 0.75 + 20 g × 0.9999 = 35.0 g fine gold (shown as "≈ 35.00 g fine gold, from 2 types with known
  purity"); types without purity are listed as *not included* (AT11).
* Count types never add to mass totals unless they have a unit weight, and then only in the fine-metal line.

### 7.4 Events and their effects (ZEX-AS08)

| Event | Quantity | Money side | Income / consumption expense | Cost basis |
|---|---|---|---|---|
| Opening holding | + q at the opening date | none (never a withdrawal, AT39) | none | known only if the user enters a purchase price; otherwise *unknown* |
| Purchase | + q | *Asset purchase* entry: − amount on the paying money account (ZEX-P07) | none (capital movement, AT14) | + amount paid (in the price currency) |
| Purchase fee | – | linked Expense, category Fees | expense (once) | not added (ZEX-P09) |
| Sale (full or partial) | − q (≤ available on that date) | *Asset sale* entry: + proceeds on the receiving account | none – proceeds are not income (ZEX-AS10) | − basis × q / Q (average cost) |
| Sale fee | – | linked Expense, category Fees | expense (once) | – |
| Location transfer | − q at A, + q at B | none | none | unchanged (AT15) |
| Gift / inheritance received | + q | none | none (not income) | user enters a value as basis or leaves it *unknown* |
| Gift given / consumption / other outflow | − q | none | none (no spending) | − basis share |
| Quantity correction | ± q with a reason | none | none | proportional, shown as correction |
| Valuation | – | none | none | none – a price never changes quantity (ZEX-AS11, AT38) |

* Purchase and sale are **linked operations** in one database transaction: the quantity event, the money entry and
  the fee share a `GroupId`; edit, delete and Undo always act on the whole group (ZEX-AS14, AT17).
* When the paying account's currency differs from the asset's price currency, the editor asks for both amounts (paid
  in the account currency, basis in the price currency), like a cross-currency transfer.
* Sale proceeds appear in cash-flow views as *capital inflow*, in the holdings report with the realised result, never
  in income.

### 7.5 Validation over history (ZEX-AS15, AT16)

* For every holding the running quantity must be ≥ 0 at every date. Adding, editing (amount or date) or deleting an
  event is checked against the full history of that holding, not only today: selling 30 g on 1 Sep is refused when
  only 20 g were held on that day, even if 50 g are held today.
* The message names the date and the available quantity: "On 1 Sep only 20.000 g were held at Home safe."
* Short selling is out of scope; negative quantities are impossible.

### 7.6 Cost basis, fees and realised result

* Method **weighted average cost per asset type** (ZEX-P08), in the type's price currency. After each event in date
  order: purchase adds paid amount and quantity; outflow removes `basis × q / Q`; transfers do nothing.
* Unknown basis (an opening holding or a gift without value) makes the type's basis *unknown* from that event on
  until the user enters the missing value; then it is recomputed.
* **Realised result** of a sale = proceeds − removed basis; shown only when the basis is known; fees are not
  subtracted again (they are already expenses, ZEX-P09), the line says "fees recorded separately".
* **Unrealised result** = current valuation − remaining basis, only with a known basis and a valuation; labelled "if
  sold at your price of 2 Oct".
* These numbers are information for the user, not tax accounting; the help text says so.

### 7.7 Valuation

* A valuation is either *price per unit* (per g, per kg, per unit) or *total value of a holding*; the user picks one
  input mode; the other is derived, never both multiplied (ZEX-AS07).
* Value(t) = quantity(t) × the latest price of the type on or before t in its currency; a holding without any price
  has value *unknown* and makes valued totals *incomplete* (ZEX-AS06, AT13).
* Purchases may create a valuation from their price (source *purchase*), clearly marked.
* New prices never rewrite earlier values; history reports use the price valid at each date (ZEX-AS13).

### 7.8 Legacy asset accounts (ZEX-P11)

Existing `Asset` accounts keep working unchanged (value by adjustment). An optional Advanced assistant *Convert to
holding* asks for type, quantity and location, creates the holding with an opening event at the account's opening
date and a valuation from the current account value, and archives the old account after confirmation. It never runs
by itself and never guesses grams or counts (ZEX-AS16).

## 8. Goals

### 8.1 Types (ZEX-D07)

| Type | Progress source | Reserves money? | Currency / unit | Existing? |
|---|---|---|---|---|
| **Account balance** | the recorded balance of one money account | no – observational | the account's currency | new |
| **Earmarked savings** | covered earmarks (`GoalAllocation`) in one or more accounts of the goal currency | yes | goal currency | existing, kept |
| **Holding quantity** | the quantity of one asset type (optionally one location) | no | the type's dimension (g/kg or count) | new |

* Existing goals become *Earmarked savings* goals by the migration; their numbers do not change.
* A goal for the **money value** of a holding is an *Earmarked savings* or *Account balance* goal in money; a
  holding-quantity goal never counts price changes as progress (AT25/AT26).
* Multi-currency funding of one goal stays out of scope; earmarks come only from accounts in the goal currency, and
  only from money accounts (fixes the asset/loan funding issue found in the review).

### 8.2 Fields and validation (ZEX-GO01)

Required: name, type, the account / asset type, target amount or quantity with its unit. Optional: target date,
priority (earmark goals), note, icon, contribution plan (§8.5), *Protect this money* (earmark goals, ZEX-P16), *Show
on Home* (pin position 1 or 2). Validation: target > 0; a target date in the past is allowed but marked overdue; a
balance goal needs an active money account; at most one active balance goal per account (ZEX-P12, ZEX-GO03).

### 8.3 States (ZEX-P13, ZEX-GO12, GO13)

```
           pause                 resume
Active ───────────────► Paused ───────────► Active
  │  complete (user)                         
  ├────────────────────► Completed ── reopen ─► Active
  └─ archive ──────────► Archived ─── restore ─► previous state
Derived (never stored): Reached (progress ≥ target now), Overdue (target date < today and not reached)
```

* *Reached* is shown as long as progress ≥ target; a later withdrawal shows the goal as not reached again with the
  real numbers (temporary reach ≠ completion).
* *Completed* is a user action ("I reached it / I bought it"); an earmark goal releases its earmarks; the history
  (allocations, progress at completion, completion date) stays. Spending the money afterwards is a normal expense.
* *Paused* goals keep their data, are excluded from capacity suggestions and Home, and show "paused".
* An archived account makes its balance goal show "Account archived – choose another account or complete the goal".

### 8.4 Progress formulas

For all types, with target `T` and current `F` in the goal's unit:

```
Remaining = max(0, T − F)
Progress  = clamp(F / T, 0, 1)           (bar)      – the real F and "x above target" are always shown
Overshoot = max(0, F − T)
```

* Balance goal: `F` = ledger balance of the account (as on Home; a note shows the confirmed balance when it differs).
  A negative balance shows progress 0 % and "balance −120 EUR".
* Earmark goal: `F` = covered earmarks (existing `GoalCalculator`).
* Quantity goal: `F` = quantity of the type (all locations or the chosen one), in the goal's unit.
* Deposits, withdrawals and transfers into the account change a balance goal through the balance; a transfer from
  another own account is progress for this goal but never income in any report (AT20).
* One money source never counts twice: earmarks are covered once across goals (existing priority rule) and envelope
  budgets subtract covered earmarks once (existing); balance goals do not reserve and are never subtracted anywhere.

### 8.5 Contribution plans (ZEX-GO08..GO10)

A goal may have one contribution plan – a plan for the user, never a bank transfer:

| Method | Parameters | Effect |
|---|---|---|
| Fixed amount per period | amount (or quantity for holding goals), schedule (§9.1) | reminder on each date (optional); scenario ETA |
| Share of eligible income | percent, schedule = the user's income dates or the financial month | suggestion per period = percent × eligible income of the closed period; preview shows which income counts |
| Spending cut | categories, cut amount per financial month | preview of the budget change; nothing changes until the user confirms "Apply to budget" |

* **Eligible income** (ZEX-GO09) = Income − IncomeReversal of the selected accounts; excluded: transfers (including
  loan principal), refunds and reimbursements, asset sales, adjustments, opening balances. The preview lists the
  included income entries and allows excluding a category ("bonus") for this goal.
* Suggestions never change a budget, an earmark or an entry without explicit confirmation (ZEX-GO10); a confirmed
  earmark is recorded as an allocation dated on the confirmation day.
* Reminders reuse the existing reminder planner with generic text by default.

### 8.6 Home (ZEX-GO06, GO07)

The user pins one or two goals (*Show on Home*). The Home group *Goals* shows each pinned goal: name, current / target,
remaining, percent, and – only when valid – the next contribution and the ETA with its label (scenario or trend). With
no pinned goal and no active goal nothing is shown; with active goals but none pinned, a one-line link "2 goals in
progress" appears inside *Needs attention* only if a goal is overdue. Active goals are always reachable in Simple
mode through Insights › Goals.

## 9. ETA, capacity and liquidity

All outputs are **estimates under stated assumptions**, never guarantees; no scenario range is a statistical
confidence interval.

### 9.1 Scenario – the user's plan

```
R = max(0, T − F)
N = number of contribution dates from today (inclusive) to the target date (inclusive)
RequiredContribution = ceil_to_precision(R / N)            if N > 0
                     = R ("needed now", deadline passed)    if N = 0
PeriodsNeeded        = ceil(R / C)                          if C > 0, else none
ETA                  = date of the PeriodsNeeded-th contribution date
```

* Contribution dates come from a **contribution schedule** expressed with the existing `RecurrenceRule` (ZEX-P14):
  weekly, every two weeks from an anchor (pay day), monthly on a day, a day of the financial month, Gregorian or
  Persian calendar, month-end rules. Existing goals map Monthly → monthly on today's day of month, Weekly → weekly.
* `ceil_to_precision`: money to the currency's minor unit; mass to 1 mg; count to 1 unit (indivisible) or 0.001.
* Example: R = 3,000 EUR, C = 250 EUR monthly → 12 dates; the ETA is the 12th monthly date, labelled "if you set
  aside 250 EUR every month" (AT21).
* Changing target, date or contribution in the editor recomputes and shows the new required amount and ETA before
  Save (ZEX-GO14).

### 9.2 Observed trend (ZEX-P15)

* Net contribution per period: balance goal = change of the account balance excluding opening balance and
  adjustments; earmark goal = net allocations; quantity goal = net quantity change excluding location transfers and
  valuations.
* Periods = the contribution schedule's periods, or financial months when there is none. Only **complete** periods
  count; the current one is shown separately as "so far".
* At least **3 complete periods** with data → median of their net contributions = `C_trend`; up to 6 periods are
  shown in the explanation; a period larger than 3 × the median of the others is labelled "one-off".
* `C_trend > 0` → ETA as in §9.1 with "at your recent pace of 230 EUR/month (median of 4 months)"; `C_trend ≤ 0` →
  "No date at your current pace"; fewer than 3 periods → "Not enough history yet (2 of 3 months)". In every case the
  manual scenario stays available (ZEX-GO15, AT22).

### 9.3 Capacity

Capacity estimates how much could go to goals per financial month without double counting; it is a suggestion
limit, never an allocation:

```
I   = median eligible income of the last 3 complete financial months      (per currency, usable accounts)
S   = median consumption expense of the same months, without entries that settled non-monthly plans
NM  = Σ monthly equivalents of active non-monthly expense plans (existing BUD-09 formula)
P   = Σ monthly loan principal of scheduled installments
G   = Σ contribution plans already accepted for other active goals (same currency)
Capacity = I − S − NM − P − G
```

* Fewer than 3 complete months → no capacity number, only "not enough history".
* Suggested contributions for several goals are distributed by priority and target date and their sum never
  exceeds `Capacity` (AT23); the user accepts each suggestion separately.
* Quantity goals: capacity in money is shown with an *assumed price* the user enters ("at 60 EUR/g this is 2.5 g per
  month"); the quantity ETA from purchases (§9.1/§9.2) and the money-based scenario are shown as two separate lines
  and never mixed.

### 9.4 Liquidity and headroom

Defined in [04 §3](04-kpi-and-report-catalog.md#3-liquidity-headroom-contract) (the forecast path of usable
accounts, minus explicitly protected money, with an explicit estimate of unplanned essential spending). Domain rules
used by it: §5.1 scope, §8.4 single counting of earmarks, ZEX-P16 protection.

### 9.5 Forecast snapshots (ZEX-P22)

* *Save snapshot* stores: name, created at, base date, horizon, scope (accounts), currency, scenario overrides,
  the daily path, minimum and its date, incomplete reasons, the app version. Snapshots are read-only.
* *Compare with reality* (for a date range ≤ today) shows the snapshot path, the actual balance path recomputed from
  today's ledger for the same scope, and the difference per day; entries created after the snapshot but dated before
  it are counted in "recorded later" and shown separately, so the difference is explainable (AT40).

## 10. Backup, export and import

| Data | Backup (encrypted database package) | CSV export | CSV import | PDF |
|---|---|---|---|---|
| Accounts incl. new flags (usable, country) | yes (database) | account name/currency columns of entries; a separate *accounts* file (new) | via entries (existing); accounts file (new) | in reports |
| Entries incl. new kinds, aggregated range | yes | yes (+ kind values *AssetPurchase*, *AssetSale*, *Aggregated from/to*) | yes (new kinds only from own files) | yes |
| Plans, budgets, rates, goals (all types, contribution plans, pin, protect) | yes | not in CSV (unchanged); listed as "backup only" in the export screen | – | goals report |
| Asset types, locations, events, valuations | yes | new *holdings* export (events and valuations, ISO units, quantity in g or units with the unit named) | own holdings files only | holdings report |
| Forecast snapshots | yes | no (backup only) | – | snapshot report |
| Display units, Home layout, dismissed guidance (ZEX-P20) | yes, after moving to database settings | – | – | – |
| Theme, language, Persian digits, device-wide reminders | no (device preferences, unchanged) | – | – | – |

* Backup format: the package manifest gets the new schema version; restore of older backups runs the migrations
  (existing behaviour) and fills new columns with their defaults (§13).
* CSV files get a format version line or header column (`zanance-format: 2`) so older files import as before; new
  columns are optional on import.
* The export screen states plainly: "A CSV export is not a backup. Use Backup to keep everything."

## 11. Golden examples

Fictitious numbers; each becomes a failing test before calculations change (ZEX-S0101).

| # | Input | Expected |
|---|---|---|
| G01 | Accounts 1,250 EUR, 750 EUR, 4,000 USD, 100,000,000 IRR | Native totals 2,000.00 EUR · 4,000.00 USD · 100,000,000 IRR; no raw sum (AT01) |
| G02 | Default account EUR; quick expense 25 EUR, Save | That account −25.00 EUR only after Save; nothing before (AT03) |
| G03 | Same form, account switched to USD | Saved as 25.00 USD on the USD account; the notice was shown (AT04) |
| G04 | Default account archived | Editor without account, Save disabled (AT05) |
| G05 | Transfer 100 EUR → 110 USD, fee 2 EUR | EUR −102.00, USD +110.00, expense 2.00 EUR, income 0 (AT06) |
| G06 | No USD→EUR rate | Native totals complete; converted total *incomplete (USD)* (AT07) |
| G07 | Toman on/off for IRR | Stored IRR unchanged; display 100,000,000 IRR ↔ 10,000,000 toman (AT08) |
| G08 | 20 g + 0.030 kg same type | 50.000 g (AT09) |
| G09 | 20 g 18 k + 20 g 24 k (9999) | Two lines; fine gold ≈ 34.998 g (35.00 g with purity 10000) (AT11) |
| G10 | 3 coins, unit weight 10 g | 3 coins; derived 30.000 g; value counted once (AT12) |
| G11 | Cash 2,000 EUR; buy 10 g at 100 EUR/g, fee 20 EUR | Cash 980.00; holding 10 g; expense 20.00; consumption excl. fee 0; value 1,000; cash + holding 1,980.00 (AT14) |
| G12 | Balance goal 5,000 EUR, balance 2,000 | 40 %, remaining 3,000; after −500 withdrawal: 30 %, remaining 3,500 (AT18/19) |
| G13 | Remaining 3,000 EUR, 250 EUR monthly | 12 contribution dates (AT21) |
| G14 | Quantity goal 50 g, held 20 g, 2 g/month | Remaining 30 g, 15 dates; price change alone changes nothing (AT25/26) |
| G15 | Three goals, capacity 300 EUR | Σ suggestions ≤ 300.00 (AT23) |
| G16 | *Money set aside* goal "Emergency reserve", 3,000 covered, marked *Protect this money* | Headroom subtracts 3,000 once, not again as a goal (AT24) |
| G17 | Income 0, expenses 450 | Result −450; savings rate *not available* (AT32) |
| G18 | Purchase on card 80 EUR, card paid from checking | Consumption 80 once; checking outflow on payment date (AT31) |

## 12. Data model and schema plan

Additive changes only; names are indicative and follow the existing conventions (`Entity` base with `Guid` v7,
audit timestamps, `long` minor units, `DateOnly`). No SQL or migration is produced in this stage.

| Change | Fields | Notes |
|---|---|---|
| `ZananceSettings` | `DefaultCurrencyCode` (TEXT 3), `ValuationCurrencyEnabled` (bool, default true), `RateFreshnessDays` (int, default 30), `DisplayUnits` (TEXT), `HomeLayout` (TEXT), `HomeBudgetCurrency` (TEXT 3, null) | `ReportCurrencyCode` stays and is labelled *Valuation currency* |
| `Account` | `UsableForPayments` (bool; default by type), `CountryCode` (TEXT 2, null) | Defaults computed for existing rows in the migration |
| `EntryKind` | `AssetPurchase = 6`, `AssetSale = 7` | New values only; never renumbered |
| `LedgerEntry` | `IsAggregated` (bool), `AggregatedFrom`, `AggregatedTo` (DateOnly?), `AssetEventId` (Guid?) | `GroupId` reused for linked operations |
| `AssetType` (new) | Name, Kind, Dimension, Metal?, PurityPer10000?, UnitWeightMg?, CountUnitName?, Divisible, PriceCurrencyCode, Note?, IsArchived, SortOrder | |
| `AssetLocation` (new) | Name, IsArchived, SortOrder | One default location |
| `AssetEvent` (new) | AssetTypeId, LocationId, ToLocationId?, Kind, Date, Quantity (long, base unit), BasisAmount (long?, minor), BasisCurrency, ProceedsAmount (long?), GroupId?, Reason?, Note? | Index (AssetTypeId, Date) |
| `AssetValuation` (new) | AssetTypeId, Date, Mode (PerUnit, Total), PriceMinor (long, per base unit × 1000 scale) or TotalMinor, CurrencyCode, Source, LocationId? | Unique (AssetTypeId, Date, LocationId) |
| `Goal` | `Type` (int, default Earmark), `AccountId?`, `AssetTypeId?`, `LocationId?`, `TargetQuantity` (long?), `HomePin` (int?, 1–2), `Protect` (bool), `PausedAt?`, `CompletedAt?` | `State` gains `Paused`; existing rows get Type = Earmark |
| `ContributionPlan` (new) | GoalId, Method, Amount (long?), Percent (decimal?), RuleJson (existing rule format), CategoryIds?, ReminderEnabled | One per goal |
| `ForecastSnapshot` (new) | Name, CreatedAt, BaseDate, HorizonDays, CurrencyCode, AccountIds, ScenarioJson, PathJson, MinimumMinor, MinimumDate, IncompleteJson, AppVersion | Read-only after insert |

Price storage: `PriceMinor` is the price of **one gram or one unit** in minor units with 3 extra decimals (price per
mg would lose precision); value = quantity(mg) × price(per g, ×1000 scale) / 1,000,000, rounded half away from zero
to the minor unit.

## 13. Migration

1. Additive migration(s) per epic; every new column has a default; the migration upgrade test gets first-schema rows
   for the new tables and asserts unchanged balances, goals and budgets.
2. Settings: `DefaultCurrencyCode = ReportCurrencyCode`; `UsableForPayments = type ∈ {Cash, Checking, Savings}`.
3. Goals: `Type = Earmark`; `Frequency` mapped to a contribution schedule only when the user opens the goal (no
   data invented).
4. Preferences → database (ZEX-P20): on first start of the new version the app copies `display_units`, the Home
   layout and the dismissed-guidance flags into the open profile's settings and marks them copied; other profiles
   receive the same values once (they were shared before); preferences are removed only after a successful copy.
5. Restore of an older backup: migrations run as today; the preference copy runs again for that profile only if its
   settings are empty.
6. The compiled model is regenerated for every model change (existing rule, D-44).

## 14. Atomicity, undo, archive and delete

* Linked operations (transfer + fee, asset purchase/sale + money entry + fee, aggregated replacement) are saved,
  edited and deleted in one transaction; Undo restores the whole group (extends the existing group undo).
* Undo is extended to: deleting an asset event, deleting a valuation, completing a goal, applying a capacity or
  spending-cut suggestion, replacing an aggregated entry.
* Asset types and locations with events cannot be deleted, only archived; an archived type keeps its holdings in
  history and net worth until its quantity is zero, then disappears from current totals.
* Accounts still cannot be deleted (existing); archiving clears the default account (§4.4).

## 15. Responsibilities and limits

| Component (Core unless noted) | Responsibility |
|---|---|
| `EntryClassification` | One place that says whether a kind is income, consumption, capital, transfer or correction |
| `MoneyTotals` | Native totals per account group, converted totals with `RateInfo` |
| `RateTable` (existing) | Conversion, freshness flags |
| `HoldingsLedger` | Running quantities, validation over history, average cost, realised results |
| `AssetValuationService` | Values at a date, incompleteness |
| `GoalProgressService` (extends `GoalCalculator`) | Progress per type, states, single counting |
| `ContributionScheduleService` | Contribution dates from `RecurrenceRule`, required contribution |
| `EtaService` | Scenario, trend and capacity ETA with labels |
| `CapacityCalculator` | Capacity and the distribution of suggestions |
| `LiquidityCalculator` (extends `Forecast`) | Usable scope, headroom, protected money, essential spending estimate |
| `KpiCatalog` | K01–K14 definitions used by Home, reports and PDF |
| `WealthHistory` | Net worth over time and its decomposition |
| App view models | Presentation, mode policy, navigation; no financial formulas |

Precision: money `long` minor units with checked sums; quantities `long` (mg / milli-units); prices as rationals
(minor × 1000 per base unit); percentages computed in `decimal` and rounded only for display (0 or 1 decimal).
