# 06 – Implementation backlog (reference)

**This is the single reference backlog of enhancement ZEX.** Every work item of the package has an entry here; the
project's main backlog ([05 – Phase 2 backlog](../../05-phase-2-backlog.md)) only links to it. All stories are
**approved by the owner on 2026-10-03** (all proposals ZEX-P01…P23 as preferred) and are implemented phase by phase (see [Phases](#phases)); each story's state is kept in the [state table](#state).

Sizes are relative (S < M < L < XL) with their main uncertainty, not time estimates. Every story is a vertical slice:
logic, persistence, UI, strings (en/fa/de), tests, backup/export and the documents it affects. A story is done only
when its acceptance criteria pass on the platforms named in ZEX-S0903 and the documents are updated (AGENTS.md §10).
Cross-cutting criteria apply to **every** story: Simple/Advanced policy ([05](05-simple-advanced-matrix.md)),
accessibility (real buttons, ≥ 44 px, announced notices, RTL), localization in fa/de/en (Persian digits, German
length), no new permission, dependency or network use, and backup coverage.

## 1. Epics, order and critical path

| Epic | Title | Priority | Depends on | Stories |
|---|---|---|---|---|
| ZEX-E01 | Calculation contract and defaults | P0 | – | S0101–S0106 |
| ZEX-E02 | Multi-currency and Home | P1 | E01 | S0201–S0206 |
| ZEX-E03 | Account goals and Home | P1 | E01 (S0104), S0201 | S0301–S0307 |
| ZEX-E04 | Quantity holdings | P1 | E01 | S0401–S0409 |
| ZEX-E05 | Simple/Advanced policy | P1, with every epic | E01 | S0501–S0503 |
| ZEX-E06 | Core reports and data quality | P1 | E02, E03 (S0604), E04 (S0605) | S0601–S0612 |
| ZEX-E07 | Quantity goals and trend ETA | P1 | E03, E04 | S0701–S0703 |
| ZEX-E08 | Wealth history and forecast snapshots | P2 (order only) | E04, E06 | S0801–S0804 |
| ZEX-E09 | Validation and release | Gate of every wave | each wave | S0901–S0906 |

Critical path: S0101 → S0104 → S0102 → S0103 → S0201 → S0301 → S0305 → S0401 → S0403 → S0404 → S0601 → S0606 →
S0702 → S0801 → S0802 → S0906. Waves: [09](09-implementation-goal-draft.md#waves-suggested-for-that-goal).
Complementary work is in named stories: headroom S0606, period-end review S0610, aggregated entries S0611, product
transparency S0905, essential coverage S0612.

## Phases

Agreed with the owner on 2026-10-03 (replaces the waves of [09](09-implementation-goal-draft.md)). Every phase ends
with all tests green, Android and Windows builds, documents updated, a commit with CI checked and an APK for the
owner's phone test; the next phase starts after the owner's check.

| Phase | Epics | Estimate (working days) |
|---|---|---|
| 1 | ZEX-E01 calculation contract and defaults, ZEX-E02 multi-currency and Home, ZEX-E05 rules for both | 12–16 |
| 2 | ZEX-E03 account goals and Home | 7–9 |
| 3 | ZEX-E04 quantity holdings | 11–14 |
| 4 | ZEX-E06 core reports and data quality | 15–19 |
| 5 | ZEX-E07 quantity goals and trend ETA, ZEX-E08 wealth history and forecast snapshots | 9–11 |
| 6 | ZEX-E09 validation and release | 6–8 |

## State

States: *Planned*, *In progress*, *Implemented – verified* (acceptance tested), *Implemented – unverified*.

| Story | Title | Phase | State |
|---|---|---|---|
| ZEX-S0101 | Golden examples as tests; view-model test harness | 1 | Implemented – verified (phase 1 examples) |
| ZEX-S0102 | Separate default currency, default account and valuation currency | 1 | Implemented – verified |
| ZEX-S0103 | Account selection contract for every entry path | 1 | Implemented – unverified (contract unit-tested; editor to check on a phone) |
| ZEX-S0104 | Shared definitions: entry classification, totals, checked arithmetic | 1 | Implemented – verified |
| ZEX-S0105 | Account currency lock covers all stored amounts | 1 | Implemented – verified |
| ZEX-S0106 | Rate information and freshness | 1 | Implemented – verified |
| ZEX-S0201 | Home balances: native first, converted secondary, archived excluded | 1 | Implemented – verified |
| ZEX-S0202 | Accounts overview groups and account details | 1 | Implemented – unverified (snapshots only) |
| ZEX-S0203 | Usable for payments and optional country | 1 | Implemented – verified |
| ZEX-S0204 | Cross-currency transfer effect preview and fees | 1 | Implemented – verified (fees unit-tested; form to check on a phone) |
| ZEX-S0205 | Display units and Home layout per profile and in backups | 1 | Implemented – unverified (dismissed-guidance flags stay device-wide) |
| ZEX-S0206 | Home layout: attention, at most three groups | 1 | Implemented – verified |
| ZEX-S0301 | Goal types and states | 2 | Planned |
| ZEX-S0302 | Balance goal: create, edit, preview | 2 | Planned |
| ZEX-S0303 | Goal lifecycle and messages | 2 | Planned |
| ZEX-S0304 | Pin goals to Home; goal card | 2 | Planned |
| ZEX-S0305 | Contribution schedule and scenario ETA | 2 | Planned |
| ZEX-S0306 | Contribution methods: fixed, share of income, spending cut | 2 | Planned |
| ZEX-S0307 | Earmark goals: money accounts only, protect flag, regression | 2 | Planned |
| ZEX-S0401 | Asset types, units, purity and locations | 3 | Planned |
| ZEX-S0402 | Opening holdings and history validation | 3 | Planned |
| ZEX-S0403 | Purchase: linked money and quantity, fee, capital entry kinds | 3 | Planned |
| ZEX-S0404 | Sale, average cost and realised result | 3 | Planned |
| ZEX-S0405 | Location transfer, gifts, outflows, corrections | 3 | Planned |
| ZEX-S0406 | Valuations and valued totals | 3 | Planned |
| ZEX-S0407 | Holdings screens and aggregation rules | 3 | Planned |
| ZEX-S0408 | Legacy asset accounts kept; optional conversion assistant | 3 | Planned |
| ZEX-S0409 | Holdings in backup, CSV export and import | 3 | Planned |
| ZEX-S0501 | Feature policy model and audit | 1 (rules with every phase) | Planned |
| ZEX-S0502 | Summaries for hidden active data | 1 (rules with every phase) | Planned |
| ZEX-S0503 | Mode-switch regression suite | 1 (rules with every phase) | Planned |
| ZEX-S0601 | Report scope bar, drill-down parity, PDF scopes | 4 | Planned |
| ZEX-S0602 | Period overview (R1): surplus, rate, spending changes | 4 | Planned |
| ZEX-S0603 | Commitments and cost of living (R2): K04, K08 | 4 | Planned |
| ZEX-S0604 | Goals and capacity (R3) | 4 | Planned |
| ZEX-S0605 | Holdings and net worth (R4): K10, K13 | 4 | Planned |
| ZEX-S0606 | Liquidity and headroom: K02 with usable scope, protected money, essential estimate | 4 | Planned |
| ZEX-S0607 | Debt burden and receivables (K09, K12) | 4 | Planned |
| ZEX-S0608 | Data quality (R6, K14): reconciliation date, backup status | 4 | Planned |
| ZEX-S0609 | KPI explanations | 4 | Planned |
| ZEX-S0610 | Period-end review | 4 | Planned |
| ZEX-S0611 | Aggregated entries and overlap handling | 4 | Planned |
| ZEX-S0612 | Essential expense coverage (K07) | 4 | Planned |
| ZEX-S0701 | Holding-quantity goal | 5 | Planned |
| ZEX-S0702 | Observed-trend ETA for all goal types | 5 | Planned |
| ZEX-S0703 | Capacity suggestions with an assumed price for quantity goals | 5 | Planned |
| ZEX-S0801 | Net worth history | 5 | Planned |
| ZEX-S0802 | Wealth change decomposition | 5 | Planned |
| ZEX-S0803 | Save forecast snapshot | 5 | Planned |
| ZEX-S0804 | Snapshot vs reality | 5 | Planned |
| ZEX-S0901 | Migration and older-backup restore per wave | 6 | Planned |
| ZEX-S0902 | Backup round trip with all new data | 6 | Planned |
| ZEX-S0903 | Platform verification per wave | 6 | Planned |
| ZEX-S0904 | Localization, RTL, dark theme and accessibility per wave | 6 | Planned |
| ZEX-S0905 | Product transparency | 6 | Planned |
| ZEX-S0906 | Performance with the reference data set | 6 | Planned |

---

## 2. ZEX-E01 – Calculation contract and defaults

#### ZEX-S0101 – Golden examples as tests; view-model test harness
* **Value:** numbers are fixed before code changes; regressions become visible.
* **Sources:** golden examples G01–G18 ([02 §11](02-product-and-domain-design.md#11-golden-examples)); ZEX-AT01–AT40 numbers; finding ZEX-F14.
* **Now:** golden data test for the specification (`GoldenDataTests.cs`); no App/view-model tests.
* **In scope:** each golden example as a failing test in Core/Data tests; a new `Vafadar.Zanance.App.Tests` project (net10.0 target where possible, or contract services moved to Core) for the account contract and Home/report parity. **Out:** UI automation.
* **Screens / mode:** none.
* **Depends on:** –. First story of the package.
* **Code / data:** `test/Apps/Zanance/*`; solution filter `Vafadar.Tests.slnf`.
* **Migration / backup:** none.
* **Acceptance:** Given the golden examples, when the suite runs before any change, then each new example fails for the expected reason (missing feature) and all existing tests pass; after the epic's stories they all pass.
* **Tests:** unit and integration (SQLite) per example.
* **L10n / a11y:** display tests for G01/G07/G09 in en/fa/de.
* **Risk / recovery:** an App test project may not build without MAUI workloads in CI → keep logic in Core services with tests (preferred).
* **Size:** M (test project setup).

#### ZEX-S0102 – Separate default currency, default account and valuation currency
* **Value:** changing one default never changes the others (ZEX-MC02, MC05).
* **Sources:** ZEX-MC02, MC05; ZEX-P01, P02; AT02.
* **Now:** `ReportCurrencyCode` doubles as default currency (`AccountEditorViewModel.cs:71`, `GoalEditorViewModel.cs:98`, `BudgetViewModel.cs:240`).
* **In scope:** new setting `DefaultCurrencyCode`; UI labels *Default currency for new items* and *Valuation currency*; Home/Budget budget currency = default account's currency with a switcher (P02); alerts for every budget; help texts with examples. **Out:** removing the report currency.
* **Screens / mode:** Settings › Money and months (F1), Home budget group, Budget page; both modes, *Off* for valuation currency in Advanced.
* **Depends on:** S0101, S0104.
* **Code / data:** `ZananceSettings`, `SettingsViewModel`, view models that read the report currency for defaults.
* **Migration / backup:** column with value copied from `ReportCurrencyCode`; included in backups (database).
* **Acceptance:** Given EUR as both, when the valuation currency is changed to USD, then new accounts still default to EUR, Home still shows the EUR budget, and no stored amount changes; given budgets in EUR and USD, then both raise their own alerts.
* **Tests:** migration test; settings store test; view-model test of defaults.
* **L10n / a11y:** three help texts with examples in fa/de/en.
* **Risk / recovery:** confusion about the rename → help text and changelog entry.
* **Size:** M.

#### ZEX-S0103 – Account selection contract for every entry path
* **Value:** quick add never takes money from an unexpected account (ZEX-MC03–MC06).
* **Sources:** ZEX-MC03, MC04, MC05, MC06; ZEX-P04, P05; findings ZEX-F02, F03, F04; AT03–AT05.
* **Now:** silent fallback, no notice on currency change, receipt read through the display unit, template amount reinterpretation.
* **In scope:** precedence explicit › context › valid default › none ([02 §4](02-product-and-domain-design.md#4-account-selection-contract)); account chip always visible; currency-change notice with Undo; invalid default state; archived default cleared with notice; templates with archived account; receipts parsed in ISO units; *Add entry here* on account details; effect label on Save. **Out:** "last used" account.
* **Screens / mode:** entry editor (F2), account details, Home quick add line; both modes.
* **Depends on:** S0102.
* **Code / data:** `EntryEditorViewModel`, `AccountDetailViewModel`, `HomeViewModel` (receipt), `AccountEditorViewModel` (archive); contract logic in a Core service.
* **Migration / backup:** none.
* **Acceptance:** Given default account Main (EUR), when Home *Expense* 25 is saved, then Main −25.00 EUR and nothing changed before Save (AT03); given the account switched to USD, then the notice appears and the entry is 25.00 USD (AT04); given Main archived, then the editor has no account and Save is disabled until one is chosen (AT05); given a toman display unit on IRR and a receipt total 1,250,000 IRR, then the amount field shows 1,250,000 rial units, not ×10.
* **Tests:** contract service unit tests for all entry paths; view-model tests for the notice and disabled Save.
* **L10n / a11y:** notice announced; chip button names include the currency in words.
* **Risk / recovery:** one more tap when the default is invalid – intended.
* **Size:** L (many entry paths).

#### ZEX-S0104 – Shared definitions: entry classification, totals, checked arithmetic
* **Value:** Home, reports and PDF compute the same numbers.
* **Sources:** principles [02 §1](02-product-and-domain-design.md#1-principles); ZEX-AT36; findings ZEX-F11, F13.
* **Now:** income/expense rules in `LedgerCalculator`; Home donut net vs report donut gross; unchecked sums.
* **In scope:** `EntryClassification` (income, consumption, capital, transfer, correction) used by every calculator; `MoneyTotals` per account group with `RateInfo`; checked sums with a clear error; one donut definition (gross chart + net table, as in reports). **Out:** new reports.
* **Screens / mode:** Home chart (if shown), reports.
* **Depends on:** S0101.
* **Code / data:** `Core/Ledger/LedgerCalculator.cs`, `Core/Reports/ReportCalculator.cs`, `Core/Rates/RateTable.cs`, Home/Reports view models.
* **Migration / backup:** none.
* **Acceptance:** Given the same scope, when Home and the period report show October, then income, spending and result are identical; given sums beyond `long`, then an error is shown instead of a wrong number.
* **Tests:** parity tests, overflow tests.
* **L10n / a11y:** –.
* **Risk / recovery:** subtle behaviour change of the Home donut → changelog note.
* **Size:** M.

#### ZEX-S0105 – Account currency lock covers all stored amounts
* **Value:** no plan, template or goal amount is ever relabelled into another currency.
* **Sources:** ZEX-MC02; ZEX-P03; finding ZEX-F01.
* **Now:** lock only on entries (`ZananceStore.cs:122-149`).
* **In scope:** lock when entries, plans, templates, earmarks, installment, asset events or budget scopes reference the account; editor lists the reasons and offers *new account in another currency + transfer*. **Out:** currency conversion of accounts.
* **Screens / mode:** account editor; both modes.
* **Depends on:** S0101.
* **Code / data:** `ZananceStore.HasEntriesAsync` → `CurrencyLockReasonsAsync`.
* **Migration / backup:** none.
* **Acceptance:** Given an account without entries but with a plan, when the currency is changed, then Save is refused with "1 plan uses this currency".
* **Tests:** store tests per referencing kind.
* **L10n / a11y:** reason list announced.
* **Risk / recovery:** –.
* **Size:** S.

#### ZEX-S0106 – Rate information and freshness
* **Value:** converted numbers show how old and how certain their rates are.
* **Sources:** ZEX-MC08; ZEX-P06; AT07.
* **Now:** oldest rate date on Home; `IsEstimate` only in the rates list.
* **In scope:** `RateInfo` (date, estimate, outdated, missing) on every converted result; freshness setting (Advanced); amber "may be outdated" / "estimate" labels; link to Rates. **Out:** online rates.
* **Screens / mode:** Home converted line, Accounts converted view, reports in converted view; both modes.
* **Depends on:** S0104.
* **Code / data:** `RateTable`, `ZananceSettings.RateFreshnessDays`.
* **Migration / backup:** setting with default 30.
* **Acceptance:** Given a USD rate dated 40 days ago, when Home shows the converted total, then it reads "rates of <date> – may be outdated"; given no rate, then "incomplete (USD)" and the native totals unchanged.
* **Tests:** RateTable tests for freshness boundaries (financial month start vs 30 days).
* **L10n / a11y:** dates in the chosen calendar.
* **Risk / recovery:** –.
* **Size:** S.

## 3. ZEX-E02 – Multi-currency and Home

#### ZEX-S0201 – Home balances: native first, converted secondary, archived excluded
* **Value:** the first screen answers "what do I have" without mixing units.
* **Sources:** ZEX-MC07, MC08; ZEX-P19; finding ZEX-F05; AT01, AT07.
* **Now:** native totals and a combined line exist; archived accounts included on Home.
* **In scope:** balance card per currency, holdings line (when holdings exist, after S0407), converted line with `RateInfo`, archived accounts excluded, groups (money vs debts) never mixed. **Out:** layout of the lower groups (S0206).
* **Screens / mode:** Home (wireframe home.html); both modes.
* **Depends on:** S0104, S0106.
* **Code / data:** `HomeViewModel`, `HomePage.xaml`.
* **Migration / backup:** none.
* **Acceptance:** Given G01, then Home shows three native lines and no raw sum; given an archived 500 EUR account in totals, then Home and Accounts both show 2,000.00 EUR.
* **Tests:** view-model or Core totals tests.
* **L10n / a11y:** each line read as "two thousand euros".
* **Risk / recovery:** –.
* **Size:** S.

#### ZEX-S0202 – Accounts overview groups and account details
* **Value:** usable money, cards, debts, receivables, valued assets and holdings are visibly separate (ZEX-MC12).
* **Sources:** ZEX-MC07, MC12; F3.
* **Now:** cash and debts sections; no holdings; no last reconciled date.
* **In scope:** groups with per-currency totals, default marker, optional converted view (Advanced), account details with *Add entry here*, converted value, last reconciled date (from S0608). **Out:** account deletion.
* **Screens / mode:** Accounts, account details; both modes (converted view Advanced).
* **Depends on:** S0201.
* **Code / data:** `AccountsViewModel`, `AccountDetailViewModel`.
* **Migration / backup:** none.
* **Acceptance:** Given accounts of all types, then each appears in exactly one group and group totals equal the sum of their rows per currency.
* **Tests:** grouping unit tests.
* **L10n / a11y:** group headings as headings.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0203 – Usable for payments and optional country
* **Value:** the liquidity scope is the user's choice, never guessed (ZEX-MC11, MC12).
* **Sources:** ZEX-MC11, MC12; ZEX-P17, P18.
* **Now:** absent.
* **In scope:** two account fields; defaults by type; liquidity, K02, K07 and headroom use *usable*; country shown in details (Advanced). **Out:** any logic from country.
* **Screens / mode:** account editor; Advanced.
* **Depends on:** S0202.
* **Code / data:** `Account`, migration defaults.
* **Migration / backup:** two columns with defaults; database backup.
* **Acceptance:** Given a savings account marked not usable, then the forecast minimum excludes it while totals still include it.
* **Tests:** migration test; forecast scope test.
* **L10n / a11y:** help "?" with example.
* **Risk / recovery:** –.
* **Size:** S.

#### ZEX-S0204 – Cross-currency transfer effect preview and fees
* **Value:** a transfer shows exactly what leaves and arrives (ZEX-MC10).
* **Sources:** ZEX-MC10; AT06; finding ZEX-F19.
* **Now:** two amounts and a source fee exist.
* **In scope:** effect line, implied rate, optional destination fee (Advanced), destination preselected in the source currency first. **Out:** FX gains.
* **Screens / mode:** entry editor transfer; both modes (destination fee Advanced).
* **Depends on:** S0103.
* **Code / data:** `EntryEditorViewModel`, `EntryActions`.
* **Migration / backup:** none (fee entries already supported).
* **Acceptance:** Given G05, then EUR −102.00, USD +110.00, one fee expense 2.00 EUR, income 0.
* **Tests:** existing transfer tests extended; destination fee test.
* **L10n / a11y:** effect line announced.
* **Risk / recovery:** –.
* **Size:** S.

#### ZEX-S0205 – Display units and Home layout per profile and in backups
* **Value:** toman and layouts follow the profile and survive restore (ZEX-MC09, AS16).
* **Sources:** ZEX-MC09; ZEX-P20; finding ZEX-F12; AT08, AT34.
* **Now:** device preferences.
* **In scope:** move display units, Home layout and dismissed guidance into database settings; one-time copy; CSV/PDF headers name the stored unit. **Out:** theme and language (stay device-wide).
* **Screens / mode:** none visible; Display units, Customize Home unchanged.
* **Depends on:** S0102.
* **Code / data:** `DisplayUnitPreferences`, `HomeLayoutPreferences`, `ZananceSettings`.
* **Migration / backup:** preference copy ([02 §13](02-product-and-domain-design.md#13-migration)); included in backups.
* **Acceptance:** Given toman active in profile A only, then profile B shows rial; given a backup restored on a fresh install, then toman and the Home layout are back.
* **Tests:** settings round-trip in backup test; migration copy test.
* **L10n / a11y:** –.
* **Risk / recovery:** keep preferences until the copy is confirmed.
* **Size:** M.

#### ZEX-S0206 – Home layout: attention, at most three groups
* **Value:** Home shows the situation at a glance without unused features (ZEX-D09).
* **Sources:** ZEX-P19, P02; ZEX-GO06 (goals group placeholder).
* **Now:** sections Period, Forecast, Budget, Upcoming, Recent, Categories, Accounts.
* **In scope:** order balances → quick add → attention → budget → next 30 days → goals; empty groups not shown; default account line under quick add; budget currency switcher; existing sections available in *Customize Home*. **Out:** goal card content (S0304).
* **Screens / mode:** Home; both modes (forecast card Advanced, warning in both).
* **Depends on:** S0201, S0603 (next 30 days numbers).
* **Code / data:** `HomeLayout`, `HomeViewModel`, `HomePage.xaml`.
* **Migration / backup:** layout migration maps old sections to the new order.
* **Acceptance:** Given a single-account user without plans, budget or goals, then Home shows balances, quick add and recent entries only; given all data, then at most three groups below attention.
* **Tests:** HomeLayout tests extended.
* **L10n / a11y:** headings; reading order.
* **Risk / recovery:** users miss a section → *Customize Home* restores it.
* **Size:** M.

## 4. ZEX-E03 – Account goals and Home

#### ZEX-S0301 – Goal types and states
* **Value:** goals can follow a balance, earmarks or a quantity without breaking existing goals (ZEX-D07).
* **Sources:** ZEX-GO01, GO12; ZEX-P13.
* **Now:** earmark goals; states Active/Completed/Archived.
* **In scope:** `Type`, `AccountId`, `AssetTypeId`, `TargetQuantity`, `Paused` state, `CompletedAt`, derived Reached/Overdue. **Out:** quantity UI (S0701).
* **Screens / mode:** none yet.
* **Depends on:** S0101.
* **Code / data:** `Goal`, `GoalCalculator` → `GoalProgressService`, migration.
* **Migration / backup:** existing goals → Earmark; numbers unchanged.
* **Acceptance:** Given existing goals, when migrated, then every goal shows the same funded, remaining and suggestion as before.
* **Tests:** migration test; calculator regression (AT-65).
* **L10n / a11y:** state names.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0302 – Balance goal: create, edit, preview
* **Value:** "Savings account to 5,000 EUR" in a few taps (ZEX-D04).
* **Sources:** ZEX-GO01–GO04, GO14; ZEX-P12; AT18–AT20.
* **Now:** absent.
* **In scope:** type chooser, account picker (money accounts), target, date, preview of required contribution and ETA before Save, one active balance goal per account. **Out:** contribution methods (S0306).
* **Screens / mode:** goal editor (goals.html); Simple and Advanced.
* **Depends on:** S0301, S0305 (preview of dates).
* **Code / data:** `GoalEditorViewModel`, `GoalStore`.
* **Migration / backup:** database.
* **Acceptance:** Given balance 2,000 and target 5,000, then 40 % and 3,000 remaining; after a 500 withdrawal 30 % and 3,500; a transfer of 250 from another own account raises progress and leaves income unchanged.
* **Tests:** progress unit tests; store test for the uniqueness rule.
* **L10n / a11y:** live preview announced politely.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0303 – Goal lifecycle and messages
* **Value:** every goal state has a clear meaning and message (ZEX-GO12, GO13, GO15).
* **Sources:** ZEX-GO12, GO13, GO15; AT27.
* **Now:** overdue, unfunded, suggest-now messages.
* **In scope:** pause/resume, complete/reopen, archive/restore; withdrawal after reaching; spending after completion keeps history; messages: overdue, not enough data, zero/negative pace, invalid unit, archived account. **Out:** –.
* **Screens / mode:** goal details; both modes.
* **Depends on:** S0301.
* **Code / data:** `GoalProgressService`, goal view models.
* **Migration / backup:** `PausedAt`, `CompletedAt`.
* **Acceptance:** Given a goal due yesterday and not reached, then "The date has passed – 3,000 EUR are needed now" and no division by zero.
* **Tests:** state machine tests.
* **L10n / a11y:** state chips with text, not colour only.
* **Risk / recovery:** –.
* **Size:** S.

#### ZEX-S0304 – Pin goals to Home; goal card
* **Value:** progress at a glance (ZEX-GO06, GO07).
* **Sources:** ZEX-GO06, GO07; ZEX-P19.
* **Now:** goals not on Home.
* **In scope:** pin 1–2 goals; Home goals group with current/target/remaining/%/next contribution/ETA when valid; no empty card. **Out:** –.
* **Screens / mode:** Home; both modes.
* **Depends on:** S0206, S0302, S0305.
* **Code / data:** `Goal.HomePin`, `HomeViewModel`.
* **Migration / backup:** database.
* **Acceptance:** Given two pinned goals, then both appear in one group; given none and no overdue goal, then no goals group.
* **Tests:** view-model test.
* **L10n / a11y:** card is one button with the full text as its name.
* **Risk / recovery:** –.
* **Size:** S.

#### ZEX-S0305 – Contribution schedule and scenario ETA
* **Value:** required amounts and ETA follow real pay dates and calendars (ZEX-GO11).
* **Sources:** ZEX-GO11; ZEX-P14; AT21, AT27.
* **Now:** `AddMonths`/weekly counting.
* **In scope:** schedule via `RecurrenceRule` (weekly, two-week, monthly, financial month, Persian); opportunities N; required contribution; scenario ETA; mapping of existing frequencies. **Out:** trend (S0702).
* **Screens / mode:** goal editor, goal details; both modes (custom schedules Advanced).
* **Depends on:** S0301.
* **Code / data:** `ContributionScheduleService`, `EtaService`.
* **Migration / backup:** `ContributionPlan` table.
* **Acceptance:** Given 3,000 remaining and 250 monthly, then 12 dates and the ETA is the 12th date of the schedule; given two-week pay from 3 Oct with 150, then 20 dates.
* **Tests:** schedule tests incl. Persian months and month-end rules.
* **L10n / a11y:** dates in the chosen calendar.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0306 – Contribution methods: fixed, share of income, spending cut
* **Value:** the user chooses how to fund a goal, with previews (ZEX-GO08–GO10).
* **Sources:** ZEX-GO08, GO09, GO10.
* **Now:** even split only.
* **In scope:** three methods; eligible income definition and preview list; spending-cut preview and explicit *Apply to budget*; reminders reuse the planner. **Out:** bank transfers, automatic budget changes.
* **Screens / mode:** goal editor › contribution plan; Advanced (fixed amount also in Simple).
* **Depends on:** S0305, S0104.
* **Code / data:** `ContributionPlan`, budget editor integration.
* **Migration / backup:** database.
* **Acceptance:** Given salary 3,000 and a transfer of 500 from savings in the month, when 10 % of income is chosen, then the suggestion is 300 (the transfer is not income); given a spending cut of 50 on Leisure, then no budget changes until *Apply*.
* **Tests:** eligible income tests incl. loan principal, asset sale, reimbursement exclusions.
* **L10n / a11y:** "This is a plan – Zanance moves no money".
* **Risk / recovery:** –.
* **Size:** L.

#### ZEX-S0307 – Earmark goals: money accounts only, protect flag, regression
* **Value:** earmarks stay correct and can protect money for headroom (ZEX-GO05).
* **Sources:** ZEX-GO05; ZEX-P16; finding ZEX-F06; AT24.
* **Now:** funding from any non-archived account in the currency.
* **In scope:** funding limited to money accounts; *Protect this money*; release capped at the earmarked amount; regression of AT-65 and envelopes. **Out:** account floors.
* **Screens / mode:** goal details; both modes (protect Advanced).
* **Depends on:** S0301.
* **Code / data:** `GoalDetailViewModel`, `GoalCalculator`.
* **Migration / backup:** `Protect` column.
* **Acceptance:** Given an Asset account in EUR, then it is not offered as a funding account; given a *Money set aside* goal marked *Protect this money* with 1,000 covered, then headroom subtracts 1,000 once; a balance goal offers no *Protect this money* option.
* **Tests:** calculator and store tests.
* **L10n / a11y:** –.
* **Risk / recovery:** existing earmarks on non-money accounts → shown as "not covered" with a fix action, never deleted.
* **Size:** S.

## 5. ZEX-E04 – Quantity holdings

#### ZEX-S0401 – Asset types, units, purity and locations
* **Value:** gold, coins and other items can be recorded by weight or count (ZEX-D03).
* **Sources:** ZEX-AS01–AS05; ZEX-P10.
* **Now:** absent.
* **In scope:** `AssetType`, `AssetLocation`, unit conversion, purity, unit weight, divisibility, precision rules, editor. **Out:** online prices.
* **Screens / mode:** asset type editor (holdings.html); Advanced create, Simple view.
* **Depends on:** S0101.
* **Code / data:** new Core types and tables.
* **Migration / backup:** new tables; database backup.
* **Acceptance:** Given 0.030 kg entered, then 30.000 g stored as 30,000 mg; given an indivisible coin type, then 1.5 is refused.
* **Tests:** unit conversion and validation tests.
* **L10n / a11y:** decimal separators per language; units spoken ("grams").
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0402 – Opening holdings and history validation
* **Value:** existing gold is recorded without touching money (ZEX-AS12, AS15).
* **Sources:** ZEX-AS12, AS15; AT16, AT39.
* **Now:** absent.
* **In scope:** opening event, running quantity per holding, validation over full history for add/edit/delete. **Out:** short selling.
* **Screens / mode:** event editor; Advanced create, Simple correct.
* **Depends on:** S0401.
* **Code / data:** `HoldingsLedger`, `AssetEvent`.
* **Migration / backup:** database.
* **Acceptance:** Given an opening holding of 20 g, then no money entry exists; given a back-dated sale of 30 g on a day with 20 g, then it is refused naming the date and 20.000 g.
* **Tests:** history validation tests with back-dated edits.
* **L10n / a11y:** error announced.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0403 – Purchase: linked money and quantity, fee, capital entry kinds
* **Value:** buying gold moves money into a holding without counting as spending (ZEX-AS09).
* **Sources:** ZEX-AS09, AS14; ZEX-P07, P09; AT14, AT17.
* **Now:** linked operations exist for transfers + fee.
* **In scope:** `EntryKind.AssetPurchase` and `AssetSale`; purchase editor with two currencies; fee as expense; one transaction; edit/delete/undo as a group; every calculator excludes capital kinds. **Out:** tax reports.
* **Screens / mode:** purchase editor (holdings.html); Advanced.
* **Depends on:** S0402, S0104.
* **Code / data:** `EntryKind`, `EntryClassification`, store, CSV.
* **Migration / backup:** new enum values; CSV kind values; backup automatic.
* **Acceptance:** Given G11, then cash 980.00, holding 10 g, consumption excluding fees 0, fee expense 20.00; deleting the purchase removes all three and Undo restores all three.
* **Tests:** integration tests for atomicity; every report excludes the capital kinds.
* **L10n / a11y:** summary block announced.
* **Risk / recovery:** a report missing the new kinds → classification tests per report.
* **Size:** L.

#### ZEX-S0404 – Sale, average cost and realised result
* **Value:** partial sales are recorded honestly; results only with a known basis (ZEX-AS10).
* **Sources:** ZEX-AS10, AS14, AS15; ZEX-P08, P09.
* **Now:** absent.
* **In scope:** sale editor, average cost, unknown basis handling, realised and unrealised results with labels. **Out:** FIFO, tax.
* **Screens / mode:** sale editor, holding details; Advanced.
* **Depends on:** S0403.
* **Code / data:** `HoldingsLedger`.
* **Migration / backup:** database.
* **Acceptance:** Given 10 g bought for 1,000 and 10 g opening without price, when 5 g are sold for 600, then the realised result is "basis unknown"; after entering 900 for the opening holding, the basis per g is 95.00 and the realised result 600 − 475 = 125.00; proceeds never appear as income.
* **Tests:** average-cost tests with back-dated edits.
* **L10n / a11y:** "not a tax calculation" help.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0405 – Location transfer, gifts, outflows, corrections
* **Value:** every real-world movement has its own meaning (ZEX-AS08, AS11).
* **Sources:** ZEX-AS08, AS11; AT15.
* **Now:** absent.
* **In scope:** the four event kinds with their effects table ([02 §7.4](02-product-and-domain-design.md#74-events-and-their-effects-zex-as08)). **Out:** –.
* **Screens / mode:** event editor; Advanced.
* **Depends on:** S0402.
* **Code / data:** `AssetEvent.Kind`.
* **Migration / backup:** database.
* **Acceptance:** Given 20.000 g of 18k gold and 5 g moved from Home safe to Bank box, then the total stays 20.000 g and no income, expense or result is created.
* **Tests:** effect tests per kind.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** S.

#### ZEX-S0406 – Valuations and valued totals
* **Value:** values are dated, explicit and never invent zero (ZEX-AS06, AS07, AS13).
* **Sources:** ZEX-AS06, AS07, AS13; AT13, AT38.
* **Now:** absent.
* **In scope:** valuation editor (per unit or total), value at a date, incompleteness, purchase-derived valuations marked. **Out:** online prices.
* **Screens / mode:** valuation editor, holdings list; Advanced create, Simple view.
* **Depends on:** S0401.
* **Code / data:** `AssetValuation`, `AssetValuationService`.
* **Migration / backup:** database.
* **Acceptance:** Given a new price of 110 EUR/g, then the quantity is unchanged, the current value changes and the value on earlier dates stays.
* **Tests:** valuation-at-date tests.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0407 – Holdings screens and aggregation rules
* **Value:** the user sees what they hold, by identity, with a separate fine-metal line (ZEX-AS02, AS03, AS05).
* **Sources:** ZEX-AS02, AS03, AS05; AT09–AT12.
* **Now:** absent.
* **In scope:** holdings list, holding details, Home/Accounts holdings line, fine-metal equivalent. **Out:** composition charts (S0605).
* **Screens / mode:** holdings.html; both modes.
* **Depends on:** S0402, S0406.
* **Code / data:** new pages and view models.
* **Migration / backup:** –.
* **Acceptance:** Given G08–G10, then 50.000 g; two gold lines with "≈ 34.998 g fine gold"; 3 coins with 30.000 g derived and one value.
* **Tests:** aggregation tests; display tests in fa/de/en.
* **L10n / a11y:** Persian digits for quantities; units in words for screen readers.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0408 – Legacy asset accounts kept; optional conversion assistant
* **Value:** existing data keeps working; conversion only on request (ZEX-AS16).
* **Sources:** ZEX-AS16; ZEX-P11.
* **Now:** `Asset` accounts with adjustments.
* **In scope:** unchanged behaviour; assistant *Convert to holding* (Advanced) with preview and confirmation; validator stops income/expense on asset accounts unless confirmed. **Out:** automatic conversion.
* **Screens / mode:** account details (Advanced).
* **Depends on:** S0402, S0406.
* **Code / data:** assistant, validator.
* **Migration / backup:** none automatic.
* **Acceptance:** Given an Asset account "Car 8,000 EUR", when the app is updated, then it is unchanged; converting creates nothing until confirmed.
* **Tests:** assistant integration test.
* **L10n / a11y:** –.
* **Risk / recovery:** archived old account stays restorable.
* **Size:** S.

#### ZEX-S0409 – Holdings in backup, CSV export and import
* **Value:** holdings are portable and restorable (ZEX-AS16).
* **Sources:** ZEX-AS16; AT34; [02 §10](02-product-and-domain-design.md#10-backup-export-and-import).
* **Now:** backup covers the database; CSV covers entries only, no version marker.
* **In scope:** holdings CSV (events, valuations), format version marker for all CSV files, own-file import with dedupe, backup manifest counts for goals/holdings/snapshots, restore preview lists them. **Out:** generic broker imports.
* **Screens / mode:** Import/Export, Backup; both modes (holdings export Advanced).
* **Depends on:** S0403–S0406.
* **Code / data:** `CsvExport`, `CsvImport`, `ZananceBackupSummary`.
* **Migration / backup:** format version 2 for CSV; manifest summary extended.
* **Acceptance:** Given holdings, when exported and re-imported into an empty profile, then quantities, units, locations and valuations are equal; older CSV files import unchanged.
* **Tests:** CSV round-trip; backup restore test.
* **L10n / a11y:** "export is not a backup".
* **Risk / recovery:** –.
* **Size:** M.

## 6. ZEX-E05 – Simple/Advanced policy

#### ZEX-S0501 – Feature policy model and audit
* **Value:** one consistent mode behaviour (ZEX-D05, D10).
* **Sources:** [05](05-simple-advanced-matrix.md); findings ZEX-F15, F16.
* **Now:** page-level checks; no tests; onboarding does not set the mode.
* **In scope:** `FeaturePolicy` table; pages use it; onboarding sets Simple; documentation corrected (reconcile always available). **Out:** –.
* **Screens / mode:** all.
* **Depends on:** S0101.
* **Code / data:** App layer.
* **Migration / backup:** none.
* **Acceptance:** Given the policy table, then no page reads `ExperienceMode` directly.
* **Tests:** policy enumeration test.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0502 – Summaries for hidden active data
* **Value:** nothing disappears when switching to Simple (ZEX-D10).
* **Sources:** findings ZEX-F07, F08; ZEX-SA13–SA17.
* **Now:** weekly budgets invisible in Simple; several plan options without summary.
* **In scope:** summaries for plan rules, second reminder, contract, budget method; budget page shows existing weekly/two-week budgets in Simple. **Out:** –.
* **Screens / mode:** plan editor, budget page and editor; Simple.
* **Depends on:** S0501.
* **Code / data:** view models.
* **Migration / backup:** none.
* **Acceptance:** Given a weekly budget and Simple mode, then the Budget page shows it with its week and the same remaining as in Advanced.
* **Tests:** view-model tests.
* **L10n / a11y:** summary texts in three languages.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0503 – Mode-switch regression suite
* **Value:** proof that the mode only changes presentation (ZEX-AT28).
* **Sources:** ZEX-AT28, AT36; [05 §5](05-simple-advanced-matrix.md#5-mode-switch-acceptance).
* **Now:** no test references `ExperienceMode` (AT-49 is claimed as unit + manual).
* **In scope:** a rich data set (weekly budget, rollover, custom plan rule, tags, balance goal, holding) and tests comparing the numbers of Home, Budget, Goals, Accounts and reports in both modes; every later story adds its cases. **Out:** UI automation.
* **Screens / mode:** all; both modes.
* **Depends on:** S0501; grows with every epic.
* **Code / data:** test projects; `FeaturePolicy`.
* **Migration / backup:** none.
* **Acceptance:** Given the AT28 data set, when switching Advanced → Simple → Advanced, then every compared number is equal and no setting changed.
* **Tests:** parity tests (unit/view model).
* **L10n / a11y:** –.
* **Risk / recovery:** a page computing differently per mode is caught here.
* **Size:** S (grows with each epic).

## 7. ZEX-E06 – Core reports and data quality

#### ZEX-S0601 – Report scope bar, drill-down parity, PDF scopes
* **Value:** every number can be traced to its entries (K14, AT36).
* **Sources:** [04 §1, §5](04-kpi-and-report-catalog.md#1-shared-rules-for-every-number); findings ZEX-F09, F10.
* **Now:** global report currency, drill-down without currency, one PDF scope line, trend and account details missing in the PDF.
* **In scope:** scope bar (period, calendar, accounts, currency native/converted, confirmed-only); drill-down passes currency; PDF prints the scope per section and includes trend and account details. **Out:** a custom report builder.
* **Screens / mode:** reports hub (reports.html); both modes.
* **Depends on:** S0104, S0106.
* **Code / data:** `ReportsViewModel`, `TransactionsViewModel`, `Vafadar.Zanance.Reports/PdfReport.cs`.
* **Migration / backup:** none.
* **Acceptance:** Given EUR and USD expenses, when the EUR category total 820 is tapped, then the list shows only EUR entries summing to 820.00; the PDF section of the same report prints the same scope and number.
* **Tests:** drill-down parity tests; PDF content test (text of the generated document).
* **L10n / a11y:** scope bar as a group of buttons with their current value in the name.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0602 – Period overview (R1): surplus, rate, spending changes
* **Value:** "where did my money go" with fair comparisons (K05, K06, K11).
* **Sources:** ZEX-K05, K06, K11; finding ZEX-F18; AT29, AT30, AT32.
* **Now:** income & expense (with "Share of income kept"), trend with partial months labelled; no same-length comparison; tag count inconsistent.
* **In scope:** R1 sections ([04 §4](04-kpi-and-report-catalog.md#4-report-packages)), same-length comparison, top changes, capital movements and transfers as separate lines, tag count consistent with amounts. **Out:** "personal inflation".
* **Screens / mode:** R1; Simple summary, Advanced tables.
* **Depends on:** S0601.
* **Code / data:** `ReportCalculator`, `KpiCatalog`, `ReportsViewModel`.
* **Migration / backup:** none.
* **Acceptance:** Given day 12 of October, then October 1–12 is compared with September 1–12; given income 0 and expenses 450, then surplus −450.00 and rate "not available"; given an entry with two tags, then the total counts it once and the tag table notes the overlap.
* **Tests:** KPI unit tests with the catalog examples.
* **L10n / a11y:** chart tables as alternatives.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0603 – Commitments and cost of living (R2): K04, K08
* **Value:** no payment is forgotten; yearly costs are visible.
* **Sources:** ZEX-K04, K08.
* **Now:** plans, contracts, monthly equivalents exist; no 30-day or 12-month aggregate.
* **In scope:** 30-day total with fixed/estimated/unknown, overdue included, partly paid outstanding only; 12-month view by spending type with monthly shares; Home *Next 30 days* group. **Out:** contract stages.
* **Screens / mode:** R2, Home; both modes (12 months Advanced).
* **Depends on:** S0601.
* **Code / data:** `Occurrences`, `BudgetPlanning`, `KpiCatalog`.
* **Migration / backup:** none.
* **Acceptance:** catalog K04 example → 1,200.00 + ≈ 30 + 1 unknown; K08 example → fixed 11,400, non-monthly 600, ≈ 360.
* **Tests:** unit tests.
* **L10n / a11y:** due dates in the chosen calendar.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0604 – Goals and capacity (R3)
* **Value:** progress, needed contributions and a realistic split (K03, capacity).
* **Sources:** ZEX-K03; [02 §9.3](02-product-and-domain-design.md#93-capacity); AT23.
* **Now:** goals list with suggestions per goal; no capacity.
* **In scope:** capacity calculator, distribution of suggestions (Σ ≤ capacity), accept per goal, R3 report. **Out:** automatic allocations or transfers.
* **Screens / mode:** R3, goal ETA panel; Advanced.
* **Depends on:** S0305, S0306.
* **Code / data:** `CapacityCalculator`, goal view models.
* **Migration / backup:** accepted plans stored in `ContributionPlan`.
* **Acceptance:** Given capacity 300 and three goals needing 200, 150 and 100, then the suggestions sum to at most 300.00, ordered by priority and target date; accepting one changes no budget or entry.
* **Tests:** capacity tests incl. non-monthly share and loan principal.
* **L10n / a11y:** "estimate, not a promise" label.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0605 – Holdings and net worth (R4): K10, K13
* **Value:** what am I worth and in what (K10, K13).
* **Sources:** ZEX-K10, K13; AT13.
* **Now:** no net worth.
* **In scope:** net worth per currency, converted on request with `RateInfo`, composition of the known part, list of unvalued holdings. **Out:** history (E08).
* **Screens / mode:** R4; Advanced (quantities remain visible in Simple through Accounts).
* **Depends on:** S0406, S0407, S0601.
* **Code / data:** `KpiCatalog`, `AssetValuationService`.
* **Migration / backup:** none.
* **Acceptance:** catalog K10 example → 280.00 EUR; K13 example → EUR 30 %, USD 55 %, gold 15 %, "not valued: 5 coins".
* **Tests:** unit tests.
* **L10n / a11y:** composition chart with table alternative.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0606 – Liquidity and headroom: K02 with usable scope, protected money, essential estimate
* **Value:** an honest "will I run short" answer (K02, headroom).
* **Sources:** ZEX-K02; [04 §3](04-kpi-and-report-catalog.md#3-liquidity-headroom-contract); AT24, AT31, AT37.
* **Now:** forecast on accounts in totals, cards and savings included, no protected money.
* **In scope:** usable scope, protected money, essential-spending estimate with a suggestion, headroom, labels, Home warning. **Out:** statistical prediction of unplanned spending.
* **Screens / mode:** Forecast, Home attention, R2; warning in both modes, details Advanced.
* **Depends on:** S0203, S0307, S0601.
* **Code / data:** `Forecast` → `LiquidityCalculator`, `ForecastViewModel`, `HomeViewModel`.
* **Migration / backup:** essential estimate stored in settings.
* **Acceptance:** catalog headroom example → minimum 670 on 24 Oct, headroom −330 EUR; a plan with unknown amount → "incomplete (1 unknown amount)"; the card purchase is not subtracted again.
* **Tests:** forecast tests extended.
* **L10n / a11y:** "estimate, not a guarantee" in three languages.
* **Risk / recovery:** misread as "safe to spend" → wording and help.
* **Size:** L.

#### ZEX-S0607 – Debt burden and receivables (K09, K12)
* **Value:** installments and money owed to me are clear.
* **Sources:** ZEX-K09, K12.
* **Now:** loan estimate (`LoanCalculator`), reimbursements, Lent accounts; no due dates, no ratio.
* **In scope:** link loan ↔ installment plan, K09 ratio, optional due dates on reimbursable entries and Lent accounts, aging buckets. **Out:** contract-accurate amortisation.
* **Screens / mode:** R2, loan details, Owed to me; Simple next installment / totals, Advanced ratio and aging.
* **Depends on:** S0601.
* **Code / data:** `Account`, `LedgerEntry`, `LoanCalculator`, `EntryActions`.
* **Migration / backup:** `DueDate` columns, plan link column; database.
* **Acceptance:** catalog K09 example → 15 %, K05 subtracts only the 70 interest; K12 example → 480.00, one older than 90 days.
* **Tests:** unit tests.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0608 – Data quality (R6, K14): reconciliation date, backup status
* **Value:** the user knows which numbers to trust (K14).
* **Sources:** ZEX-K14; finding ZEX-F17.
* **Now:** signals exist in places; no last reconciled date; backup age only on the Backup page; `IsAutomaticBackupDue` unused.
* **In scope:** `LastReconciledOn` set on reconcile, backup age in Home attention, R6 list with actions, a footer on every report. **Out:** accuracy scores.
* **Screens / mode:** R6, Home attention, report footers; both modes.
* **Depends on:** S0601.
* **Code / data:** `Reconciliation`, `BackupService`, report view models.
* **Migration / backup:** `LastReconciledOn` column.
* **Acceptance:** Given an account reconciled 70 days ago and a backup 34 days old, then R6 lists both with their actions and Home shows the backup item once.
* **Tests:** unit tests for each signal.
* **L10n / a11y:** list items as buttons.
* **Risk / recovery:** too many attention items → only valid ones, max three on Home.
* **Size:** M.

#### ZEX-S0609 – KPI explanations
* **Value:** every number explains itself (KPI rule).
* **Sources:** [04](04-kpi-and-report-catalog.md).
* **Now:** help "?" exists for settings only.
* **In scope:** KPI sheet (definition, included/excluded, data status, *Show entries*) for K01–K14 in three languages. **Out:** –.
* **Screens / mode:** sheet ZEX-UI14; both modes.
* **Depends on:** S0602–S0608.
* **Code / data:** strings, a reusable sheet view.
* **Migration / backup:** none.
* **Acceptance:** Given any KPI card, then "?" opens its sheet and *Show entries* lists exactly the counted entries.
* **Tests:** a test that every KPI has its texts in all three languages.
* **L10n / a11y:** German lengths checked at 360 px.
* **Risk / recovery:** –.
* **Size:** M (texts).

#### ZEX-S0610 – Period-end review
* **Value:** a short, finishable monthly routine (complementary).
* **Sources:** requirements §10 (complementary work).
* **Now:** the pieces exist separately.
* **In scope:** guided checklist of existing actions (unreviewed, plans, reconcile, goals, backup), progress kept per period, optional reminder. **Out:** a second accounting engine.
* **Screens / mode:** review screen (review-and-snapshots.html); both modes.
* **Depends on:** S0608.
* **Code / data:** new view model; settings key for progress.
* **Migration / backup:** progress in database settings.
* **Acceptance:** Given September ended, then Home attention offers "Review September" and each item opens its existing screen; finishing hides the item until next month.
* **Tests:** view-model test.
* **L10n / a11y:** checklist items as toggles with labels.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0611 – Aggregated entries and overlap handling
* **Value:** summary entries never double count (AT33).
* **Sources:** ZEX-P21; AT33.
* **Now:** no aggregated flag.
* **In scope:** aggregated flag and range in the editor (Advanced), overlap detection on save and import, *Replace* / *Keep both* with preview and Undo, KPI awareness of unknown timing. **Out:** automatic merging.
* **Screens / mode:** editor, overlap sheet; marker in both modes.
* **Depends on:** S0104.
* **Code / data:** `LedgerEntry`, `CsvImport`, store.
* **Migration / backup:** `IsAggregated`, range columns; CSV columns.
* **Acceptance:** Given "Groceries September aggregated 412" and 6 detailed entries totalling 395, when *Replace* is chosen, then September groceries total 412.00 (395 + 17) and Undo restores the 412 aggregated entry.
* **Tests:** integration tests incl. import.
* **L10n / a11y:** overlap sheet announced.
* **Risk / recovery:** Undo; nothing deleted without confirmation.
* **Size:** M.

#### ZEX-S0612 – Essential expense coverage (K07)
* **Value:** emergency-fund sizing.
* **Sources:** ZEX-K07.
* **Now:** absent.
* **In scope:** essential category flag with defaults, K07 card in R3 and the emergency goal detail. **Out:** –.
* **Screens / mode:** R3, goal detail, category editor; Advanced.
* **Depends on:** S0602, S0203.
* **Code / data:** `Category`, `KpiCatalog`.
* **Migration / backup:** `IsEssential` column with defaults by system key.
* **Acceptance:** catalog K07 example → 4.0 months; essential spending 0 → "not available".
* **Tests:** unit tests.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** S.

## 8. ZEX-E07 – Quantity goals and trend ETA

#### ZEX-S0701 – Holding-quantity goal
* **Value:** "50 g of gold" as a goal; prices are not progress (ZEX-AT25, AT26).
* **Sources:** ZEX-GO01, GO04; AT25, AT26.
* **Now:** absent.
* **In scope:** quantity goal type in editor and card, progress from quantity, distinction from money-value goals. **Out:** price-based quantity forecasts.
* **Screens / mode:** goal editor and card; Advanced create, Simple view.
* **Depends on:** S0301, S0407.
* **Code / data:** `GoalProgressService`, goal view models.
* **Migration / backup:** uses `TargetQuantity`, `AssetTypeId`.
* **Acceptance:** Given target 50 g, held 20 g and 2 g per month, then 30 g remaining and 15 dates; a price change alone changes nothing; a money goal for the same holding shows "includes price effect".
* **Tests:** unit tests.
* **L10n / a11y:** units spoken in words.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0702 – Observed-trend ETA for all goal types
* **Value:** "at your pace" estimates with honest data rules (ZEX-P15).
* **Sources:** ZEX-P15; AT22.
* **Now:** absent.
* **In scope:** net contribution per period per type, median, one-off labels, messages. **Out:** statistical models.
* **Screens / mode:** goal ETA panel; label in both modes, details Advanced.
* **Depends on:** S0305, S0701.
* **Code / data:** `EtaService`.
* **Migration / backup:** none.
* **Acceptance:** Given net contributions 200, 250, 230 and 900, then the median is 240 and 900 is labelled one-off; given 2 complete months only, then "Not enough history yet (2 of 3 months)" and the scenario stays available.
* **Tests:** unit tests.
* **L10n / a11y:** –.
* **Risk / recovery:** misread as a promise → labels.
* **Size:** M.

#### ZEX-S0703 – Capacity suggestions with an assumed price for quantity goals
* **Value:** money capacity translated to quantity only with an explicit price.
* **Sources:** [02 §9.3](02-product-and-domain-design.md#93-capacity).
* **Now:** absent.
* **In scope:** assumed price input, separate lines for the quantity and the money scenario. **Out:** online prices.
* **Screens / mode:** goal ETA panel; Advanced.
* **Depends on:** S0604, S0701.
* **Code / data:** `CapacityCalculator`, `EtaService`.
* **Migration / backup:** assumed price stored with the contribution plan.
* **Acceptance:** Given capacity 150 EUR and an assumed 60 EUR per g, then "2.5 g per month at your price" while the quantity ETA from purchases stays a separate line.
* **Tests:** unit tests.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** S.

## 9. ZEX-E08 – Wealth history and forecast snapshots

#### ZEX-S0801 – Net worth history
* **Value:** how wealth developed, with dated prices and rates.
* **Sources:** ZEX-K10, AS13; R5.
* **Now:** absent.
* **In scope:** net worth per month end and per day on demand, using valuations and rates dated on or before each date. **Out:** –.
* **Screens / mode:** R5; Advanced.
* **Depends on:** S0605.
* **Code / data:** `WealthHistory`.
* **Migration / backup:** none (computed).
* **Acceptance:** Given a new price today, then last month's net worth stays unchanged.
* **Tests:** unit tests with back-dated valuations.
* **L10n / a11y:** chart with table alternative.
* **Risk / recovery:** performance → month-end cache in memory only.
* **Size:** M.

#### ZEX-S0802 – Wealth change decomposition
* **Value:** "saving or prices?" answered honestly (ZEX-P23).
* **Sources:** [04 §4.1](04-kpi-and-report-catalog.md#41-wealth-change-decomposition-zex-p23).
* **Now:** absent.
* **In scope:** flows, price effect, FX effect, corrections, remainder with its causes. **Out:** tax.
* **Screens / mode:** R5; Advanced.
* **Depends on:** S0801.
* **Code / data:** `WealthHistory`.
* **Migration / backup:** none.
* **Acceptance:** Given surplus +500, price effect +100 and FX effect −30 with no corrections, then the parts sum to ΔNW = +570 with remainder 0; with a missing valuation on one date, the remainder is shown with that cause.
* **Tests:** unit tests.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** L.

#### ZEX-S0803 – Save forecast snapshot
* **Value:** plans can be kept for later comparison (ZEX-P22).
* **Sources:** ZEX-AT40.
* **Now:** scenarios are never saved.
* **In scope:** save with name, scope, assumptions and path; read-only list; backup coverage. **Out:** editing snapshots.
* **Screens / mode:** Forecast; Advanced (list visible in both when snapshots exist).
* **Depends on:** S0606.
* **Code / data:** `ForecastSnapshot`, `ForecastViewModel`.
* **Migration / backup:** new table; database backup.
* **Acceptance:** Given a saved snapshot, when plans change, then the snapshot path is unchanged.
* **Tests:** store tests.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0804 – Snapshot vs reality
* **Value:** learn from the difference between plan and reality.
* **Sources:** ZEX-AT40; [04 §4.2](04-kpi-and-report-catalog.md#42-forecast-snapshot-vs-reality-zex-p22).
* **Now:** absent.
* **In scope:** comparison chart and parts (recorded later, plans changed, unplanned spending and income). **Out:** –.
* **Screens / mode:** snapshot details; Advanced.
* **Depends on:** S0803.
* **Code / data:** `LiquidityCalculator`, snapshot view model.
* **Migration / backup:** none.
* **Acceptance:** Given an entry dated before the snapshot but recorded after it, then it appears under "recorded later" and the parts add up to the difference.
* **Tests:** unit tests.
* **L10n / a11y:** chart with table alternative.
* **Risk / recovery:** –.
* **Size:** M.

## 10. ZEX-E09 – Validation and release (every wave)

#### ZEX-S0901 – Migration and older-backup restore per wave
* **Value:** existing users keep their data.
* **Sources:** E09; AT34; AT-60.
* **Now:** migration upgrade test with first-schema rows exists.
* **In scope:** rows for every new table and column; restore of backups made by the base version. **Out:** –.
* **Screens / mode:** –.
* **Depends on:** each wave.
* **Code / data:** `MigrationTests`, backup tests.
* **Migration / backup:** the subject of the story.
* **Acceptance:** Given a base-version backup, when restored into the new version, then balances, goals, budgets and settings are equal.
* **Tests:** integration.
* **L10n / a11y:** –.
* **Risk / recovery:** a failing migration blocks the wave.
* **Size:** M.

#### ZEX-S0902 – Backup round trip with all new data
* **Value:** a backup really restores everything (AT34).
* **Sources:** ZEX-AT34; finding ZEX-F20 (disclosed, behaviour kept).
* **Now:** round-trip tests cover the database; settings round trip untested.
* **In scope:** backup → delete → restore with holdings, goals, snapshots, display units and layout; restore preview counts; backup help states that safety copies are not encrypted. **Out:** changing the safety-copy behaviour.
* **Screens / mode:** Backup; both modes.
* **Depends on:** S0205, S0409, S0803.
* **Code / data:** `ZananceBackupSummary`, backup view model.
* **Migration / backup:** manifest summary extended.
* **Acceptance:** AT34 – every listed item equal after restore on a fresh install.
* **Tests:** integration; device check on Android.
* **L10n / a11y:** –.
* **Risk / recovery:** –.
* **Size:** M.

#### ZEX-S0903 – Platform verification per wave
* **Value:** claims are backed by the same platform and build.
* **Sources:** E09.
* **Now:** Windows Debug walk-through and Android emulator used in reviews.
* **In scope:** Windows Debug walk-through, Android release on the emulator and, when available, a device; iOS **Blocked: no Mac/iOS device**. Evidence per wave in [07](07-acceptance-and-validation.md). **Out:** store release.
* **Depends on:** each wave.
* **Code / data:** `DebugSnapshots` routes for new screens.
* **Migration / backup:** –.
* **Acceptance:** Given a wave, then each of its stories has a recorded check per platform or a recorded blocker.
* **Tests:** manual + snapshots.
* **L10n / a11y:** see S0904.
* **Risk / recovery:** –.
* **Size:** M per wave.

#### ZEX-S0904 – Localization, RTL, dark theme and accessibility per wave
* **Value:** every language and theme works.
* **Sources:** AT35; project UI rules.
* **Now:** snapshot tool supports languages, themes and sizes.
* **In scope:** snapshots en/fa/de light/dark at 360/412/wide; screen-reader names; German lengths; Persian digits and signs. **Out:** –.
* **Depends on:** each wave.
* **Code / data:** strings, `DebugSnapshots`.
* **Migration / backup:** –.
* **Acceptance:** Given the wave's screens, then no truncation at 360 px in German, RTL is mirrored correctly and every button has a name.
* **Tests:** snapshot review, string completeness test.
* **L10n / a11y:** the subject of the story.
* **Risk / recovery:** –.
* **Size:** S per wave.

#### ZEX-S0905 – Product transparency
* **Value:** users know what is backed up, exported and supported where.
* **Sources:** requirements §10 (transparency).
* **Now:** About page, privacy matrix.
* **In scope:** in-app *Data coverage* (what backup, CSV and PDF contain), a platform feature matrix (Android/Windows/iOS, "not verified" where true), a problem-report path that sends nothing automatically and asks the user to remove financial details; no telemetry. **Out:** a support backend.
* **Screens / mode:** About; both modes.
* **Depends on:** S0409.
* **Code / data:** About view model, resources.
* **Migration / backup:** –.
* **Acceptance:** Given the About page, then *Data coverage* matches [02 §10](02-product-and-domain-design.md#10-backup-export-and-import).
* **Tests:** –.
* **L10n / a11y:** three languages.
* **Risk / recovery:** claims must match tests.
* **Size:** S.

#### ZEX-S0906 – Performance with the reference data set
* **Value:** the app stays fast with real-size data (Q-02).
* **Sources:** Q-02; release checklist.
* **Now:** calculation budget test with 10,000 entries exists.
* **In scope:** 10,000 entries, 20 accounts, 100 plans plus 30 asset types, 500 events, 200 valuations, 10 goals; Home and reports within the existing budget. **Out:** –.
* **Depends on:** E08.
* **Code / data:** performance tests.
* **Migration / backup:** –.
* **Acceptance:** Given the reference data set, then the calculation budget test passes for Home, R1–R6 and the holdings ledger.
* **Tests:** performance test.
* **L10n / a11y:** –.
* **Risk / recovery:** slow parts get in-memory caches.
* **Size:** M.

---

## 11. Requirement traceability

| Requirement | Stories |
|---|---|
| ZEX-MC01, MC07 | S0201, S0202 |
| ZEX-MC02 | S0102, S0105 |
| ZEX-MC03–MC06 | S0103 |
| ZEX-MC08 | S0106, S0201 |
| ZEX-MC09 | S0205 |
| ZEX-MC10 | S0204 |
| ZEX-MC11, MC12 | S0202, S0203, S0606 |
| ZEX-AS01–AS05 | S0401, S0407 |
| ZEX-AS06, AS07, AS13 | S0406 |
| ZEX-AS08, AS11 | S0405 |
| ZEX-AS09, AS14 | S0403 |
| ZEX-AS10 | S0404 |
| ZEX-AS12, AS15 | S0402 |
| ZEX-AS16 | S0408, S0409, S0902 |
| ZEX-GO01, GO02, GO03, GO04, GO14 | S0301, S0302 |
| ZEX-GO05 | S0307 |
| ZEX-GO06, GO07 | S0304 |
| ZEX-GO08–GO10 | S0306 |
| ZEX-GO11 | S0305 |
| ZEX-GO12, GO13, GO15 | S0303 |
| ZEX-K01 | S0102, S0602 |
| ZEX-K02 | S0606 |
| ZEX-K03 | S0302, S0604 |
| ZEX-K04, K08 | S0603 |
| ZEX-K05, K06, K11 | S0602 |
| ZEX-K07 | S0612 |
| ZEX-K09, K12 | S0607 |
| ZEX-K10, K13 | S0605, S0801 |
| ZEX-K14 | S0608 |
| ZEX-D05, D10, SA01–SA30 | S0501–S0503 |
| Headroom | S0606 |
| Period-end review | S0610 |
| Aggregated entries | S0611 |
| Wealth history, decomposition | S0801, S0802 |
| Forecast snapshots | S0803, S0804 |
| Product transparency | S0905 |
| Findings ZEX-F01–F20 | see [01 §8](01-current-state-and-gaps.md#8-findings-in-existing-code-not-fixed-in-this-stage) |
