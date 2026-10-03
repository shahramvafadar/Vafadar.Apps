# 07 – Acceptance and validation

Status: **Approved by the owner (2026-10-03).** This is the test plan and the expected results; tests are written with each story, none was written
in this stage. All rates and prices are fictitious. Existing scenario ids `AT-01 … AT-68` of the specification stay
as they are; the scenarios of this package are `ZEX-AT01 … ZEX-AT40` and `ZEX-X01 …`.

## 1. ZEX-AT01–AT40

| Id | Scenario | Expected result (exact) | Definition | Stories | Test type |
|---|---|---|---|---|---|
| ZEX-AT01 | Accounts 1,250 and 750 EUR, one USD account | EUR total 2,000.00; USD separate; no sum of different units | [02 §5.2](02-product-and-domain-design.md#52-native-totals-first) | S0201 | Unit (G01) |
| ZEX-AT02 | Default currency EUR → USD | History and every account currency unchanged | [02 §3](02-product-and-domain-design.md#3-defaults-and-settings) | S0102 | Unit + migration |
| ZEX-AT03 | Home expense 25 EUR, default account EUR | −25.00 on that account only after Save | [02 §4](02-product-and-domain-design.md#4-account-selection-contract) | S0103 | Unit (contract) + view model |
| ZEX-AT04 | Same form, USD account chosen | Saved 25.00 USD; final account and currency visible; notice shown | [02 §4.3](02-product-and-domain-design.md#43-changing-the-account-when-the-currency-differs-zex-p04) | S0103 | View model |
| ZEX-AT05 | Default account archived | Editor without account, Save disabled; no hidden fallback | [02 §4.4](02-product-and-domain-design.md#44-invalid-defaults-and-context) | S0103 | View model |
| ZEX-AT06 | Transfer 100 EUR → 110 USD, fee 2 EUR | EUR −102.00, USD +110.00, expense 2.00 EUR, income 0 | [02 §6.1](02-product-and-domain-design.md#61-cross-currency-transfer-zex-mc10) | S0204 | Unit + integration |
| ZEX-AT07 | No USD/EUR rate | Native totals correct; converted total *incomplete (USD)*, not 0 or complete | [02 §5.3](02-product-and-domain-design.md#53-rate-selection) | S0106 | Unit |
| ZEX-AT08 | Toman on / off | Only display and input change; stored IRR equal | [02 §6.3](02-product-and-domain-design.md#63-display-units-toman-zex-mc09) | S0205 | Unit |
| ZEX-AT09 | 20 g + 0.03 kg same type | 50.000 g | [02 §7.3](02-product-and-domain-design.md#73-what-can-be-added-up) | S0401, S0407 | Unit |
| ZEX-AT10 | 20 g gold + 30 g silver | Two lines, never 50 g of gold | [02 §7.3](02-product-and-domain-design.md#73-what-can-be-added-up) | S0407 | Unit |
| ZEX-AT11 | 20 g 18 k + 20 g fine | Separate; fine equivalent 35.00 g (purity 10000) / 34.998 g (9999) | [02 §7.3](02-product-and-domain-design.md#73-what-can-be-added-up) | S0407 | Unit |
| ZEX-AT12 | Three 10 g coins | 3 coins and 30.000 g derived; counted once | [02 §7.2](02-product-and-domain-design.md#72-units-precision-and-purity-zex-p10) | S0401, S0407 | Unit |
| ZEX-AT13 | Holding without price | Quantity visible; value unknown, total *incomplete* with the holding listed | [02 §7.7](02-product-and-domain-design.md#77-valuation) | S0406, S0605 | Unit |
| ZEX-AT14 | Buy 10 g at 100 EUR/g, fee 20 EUR from 2,000 EUR | Cash 980.00; holding 10 g; consumption excluding fee 0; fee 20.00 | [02 §7.4](02-product-and-domain-design.md#74-events-and-their-effects-zex-as08) | S0403 | Integration |
| ZEX-AT15 | Move 5 g between locations | Total unchanged; no income, sale or result | [02 §7.4](02-product-and-domain-design.md#74-events-and-their-effects-zex-as08) | S0405 | Unit |
| ZEX-AT16 | Sell more than held | Refused with date and available quantity | [02 §7.5](02-product-and-domain-design.md#75-validation-over-history-zex-as15-at16) | S0402, S0404 | Unit |
| ZEX-AT17 | Edit / delete / undo a purchase | Quantity and money side change together | [02 §14](02-product-and-domain-design.md#14-atomicity-undo-archive-and-delete) | S0403 | Integration |
| ZEX-AT18 | Balance goal 5,000 EUR, balance 2,000 | 40 %, remaining 3,000.00 | [02 §8.4](02-product-and-domain-design.md#84-progress-formulas) | S0302 | Unit |
| ZEX-AT19 | Withdraw 500 from that account | 30 %, remaining 3,500.00 | [02 §8.4](02-product-and-domain-design.md#84-progress-formulas) | S0302 | Unit |
| ZEX-AT20 | Transfer 250 from another own account | Goal progresses; total income unchanged | [02 §8.4](02-product-and-domain-design.md#84-progress-formulas) | S0302 | Unit |
| ZEX-AT21 | Remaining 3,000, 250 monthly | 12 dates, labelled as the user's plan | [02 §9.1](02-product-and-domain-design.md#91-scenario--the-users-plan) | S0305 | Unit |
| ZEX-AT22 | Trend zero/negative or < 3 periods | No date; reason shown; manual scenario available | [02 §9.2](02-product-and-domain-design.md#92-observed-trend-zex-p15) | S0702 | Unit |
| ZEX-AT23 | Three goals, capacity 300 EUR | Σ suggestions ≤ 300.00 | [02 §9.3](02-product-and-domain-design.md#93-capacity) | S0604 | Unit |
| ZEX-AT24 | *Money set aside* goal with 3,000 covered, marked *Protect this money* (the same money is goal and protected reserve) | 3,000.00 subtracted once in headroom | [04 §3](04-kpi-and-report-catalog.md#3-liquidity-headroom-contract) | S0307, S0606 | Unit |
| ZEX-AT25 | Goal 50 g, held 20 g, buy 2 g/month | Remaining 30 g; 15 dates | [02 §9.1](02-product-and-domain-design.md#91-scenario--the-users-plan) | S0701 | Unit |
| ZEX-AT26 | Only the gold price rises | Weight goal unchanged; money goal changes with "price effect" label | [02 §8.1](02-product-and-domain-design.md#81-types-zex-d07) | S0701 | Unit |
| ZEX-AT27 | Target date before today, not reached | Overdue; "needed now"; no division by zero, no negative time | [02 §9.1](02-product-and-domain-design.md#91-scenario--the-users-plan) | S0303, S0305 | Unit |
| ZEX-AT28 | Advanced → Simple with goal, holding, weekly budget | Data and numbers unchanged; summaries reachable | [05](05-simple-advanced-matrix.md) | S0502, S0503 | View model + manual |
| ZEX-AT29 | Purchase with two tags | Total counts it once; tag overlap explained | [04 §1](04-kpi-and-report-catalog.md#1-shared-rules-for-every-number) | S0602 | Unit |
| ZEX-AT30 | Partial current month vs last month | Same-length comparison or explicit "so far" | [04 K11](04-kpi-and-report-catalog.md#zex-k11--spending-pattern-change) | S0602 | Unit |
| ZEX-AT31 | Card purchase, later paid from checking | Consumption once; checking outflow on the payment date | [04 §3](04-kpi-and-report-catalog.md#3-liquidity-headroom-contract) | S0606 | Unit |
| ZEX-AT32 | Income 0, expenses positive | Deficit amount; rate "not available", not 0 or ∞ | [04 K06](04-kpi-and-report-catalog.md#zex-k06--surplus-rate) | S0602 | Unit |
| ZEX-AT33 | Monthly aggregate, then details of that range | Overlap notice; replace/link; no double counting | [02 §6.4](02-product-and-domain-design.md#64-aggregated-entries-zex-p21) | S0611 | Integration |
| ZEX-AT34 | Backup, uninstall, reinstall, restore | Accounts, quantities, goals, rates, schedules, units equal | [02 §10](02-product-and-domain-design.md#10-backup-export-and-import) | S0902 | Integration + device |
| ZEX-AT35 | fa/de/en, both calendars | Amounts identical; units, dates and minus sign correct and readable | [03 §5](03-ui-ux-design.md#5-microcopy-en--de--fa) | S0904 | Display tests + snapshots |
| ZEX-AT36 | Same filter on Home, report and export | Equal numbers or an explicit scope difference | [04 §5](04-kpi-and-report-catalog.md#5-parity-of-home-reports-pdf-and-exports) | S0104, S0601 | Parity tests |
| ZEX-AT37 | A commitment with unknown amount | Forecast and headroom *incomplete*; unknown never 0 | [04 §3](04-kpi-and-report-catalog.md#3-liquidity-headroom-contract) | S0606 | Unit |
| ZEX-AT38 | A new price or rate | Quantity unchanged; current value changes; history kept | [02 §7.7](02-product-and-domain-design.md#77-valuation) | S0406 | Unit |
| ZEX-AT39 | Opening holding of an old asset | No payment from a money account | [02 §7.4](02-product-and-domain-design.md#74-events-and-their-effects-zex-as08) | S0402 | Unit |
| ZEX-AT40 | Save a forecast snapshot, data change next month | Snapshot unchanged; comparison with reality separate | [02 §9.5](02-product-and-domain-design.md#95-forecast-snapshots-zex-p22) | S0803, S0804 | Unit |

## 2. Additional cases (ZEX-X)

| Id | Case | Expected | Stories |
|---|---|---|---|
| ZEX-X01 | Edit the date of a linked purchase back before an existing sale | Refused if any day's quantity would turn negative | S0402 |
| ZEX-X02 | Delete a transfer with a destination fee | Transfer and both fees removed; Undo restores all | S0204 |
| ZEX-X03 | Partial sale with known basis | 10 g for 1,000 + 10 g opening at 900; sell 5 g for 600 → basis 95.00/g, result +125.00 | S0404 |
| ZEX-X04 | Partial sale with unknown basis | Result "basis unknown"; no number | S0404 |
| ZEX-X05 | Negative amount or quantity typed | Field error; explains the kind or event to choose | S0103, S0402 |
| ZEX-X06 | Fee counted once | Fee in spending; not in basis; not subtracted again in the result | S0403, S0404 |
| ZEX-X07 | Tag overlap plus aggregated entry | Tag table per tag; aggregated entry counted once in totals | S0602, S0611 |
| ZEX-X08 | Loan installment 450 = 380 principal + 70 interest | K05 −70; K09 15 % of 3,000; liquidity −450 | S0607, S0606 |
| ZEX-X09 | Two-week contribution from a pay day | Dates every 14 days from the anchor, Persian calendar display correct | S0305 |
| ZEX-X10 | Two profiles with different display units and defaults | No leak between profiles | S0205, S0102 |
| ZEX-X11 | Export CSV of a period and compare with R1 | Sum of exported expenses = R1 spending for the same scope | S0601 |
| ZEX-X12 | Restore a backup from the base version | All migrations run; numbers equal | S0901 |
| ZEX-X13 | Account currency with only a plan | Currency change refused with the reason | S0105 |
| ZEX-X14 | Receipt in IRR on a toman account | Amount equals the receipt in rial units | S0103 |
| ZEX-X15 | Weekly budget in Simple | Shown with its week and the same remaining | S0502 |
| ZEX-X16 | RTL Persian Home with holdings and goals | Mirrored layout; digits Persian; codes Latin; minus U+2212 | S0904 |
| ZEX-X17 | Overflow | Sum beyond `long` shows an error, never a wrapped number | S0104 |
| ZEX-X18 | Archived account in totals | Home and Accounts agree | S0201 |
| ZEX-X19 | Goal funded from an Asset account (old data) | Shown "not covered" with a fix action; nothing deleted | S0307 |
| ZEX-X20 | Real device | Quick add, holdings purchase, goal card and restore on an Android device (release build) | S0903 |

## 3. Test layers

| Layer | Scope | Where |
|---|---|---|
| Unit (Core) | Every formula and rule of 02/04, golden examples G01–G18, KPI examples | `test/Apps/Zanance/Vafadar.Zanance.Core.Tests` |
| Integration (SQLite) | Linked operations, undo, migrations, CSV, backup round trip | `Vafadar.Zanance.Data.Tests` |
| View model | Account contract, Home/report parity, Simple/Advanced parity | new App test project or Core services (ZEX-S0101) |
| Rendering | PDF content, display formatting in fa/de/en | `Vafadar.Zanance.Reports.Tests`, Core display tests |
| Snapshot review | Every new screen in the Debug walk-through, en/fa/de, light/dark, 360/412/wide | `eng/scripts/Run-Snapshots.ps1` |
| Device | Android release build on the emulator and a device; Windows Debug | manual per wave |
| iOS | **Blocked: no Mac or iOS device available** | – |

## 4. Regression

* All existing 499 tests stay green; none is removed, skipped or weakened to pass.
* Existing acceptance scenarios AT-01 … AT-68 keep their status; the ones touched by this package (multi-currency
  AT-45…, goals AT-65, forecast AT-42…AT-44, budget AT-39…AT-41, backup AT-57) are re-run in every wave.
* The migration upgrade test is extended with first-schema rows of every new table.

## 5. Release validation per wave (ZEX-E09)

| Gate | Evidence required |
|---|---|
| Build | Windows and Android build with no warnings (`-p:ContinuousIntegrationBuild=true`); CI green |
| Tests | Full suite green; new tests of the wave listed in the wave report |
| Data | Migration test; restore of a base-version backup; backup round trip of the wave's data |
| Platforms | Windows Debug walk-through; Android release on emulator; device run when available; iOS recorded as blocked |
| Languages | Snapshots en/fa/de light/dark at 360/412/wide for every changed screen |
| Documents | Decision log, changelog (only for implemented items), UX design, specification Section 31, privacy matrix, acceptance test plan, this backlog's status |

## 6. What was actually run

In this design stage only the existing test suite was run, to record the baseline:

| Date | Command | Result |
|---|---|---|
| 2026-10-02 | `dotnet test --solution Vafadar.Tests.slnf` at `67410b4` with this package as the only (untracked) change | Passed: 499 total, 0 failed, 0 skipped (46 s); followed by `dotnet clean Vafadar.Tests.slnf` |

No new test, build of new code, migration or device run was performed for this package.
