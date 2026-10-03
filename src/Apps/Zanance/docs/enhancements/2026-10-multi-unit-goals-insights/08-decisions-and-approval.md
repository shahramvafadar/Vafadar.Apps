# 08 – Decisions and approval

**Package status: Approved by the owner on 2026-10-03** – the design and every proposal ZEX-P01…P23 with its preferred option; implementation runs in the phases of [06](06-implementation-backlog.md#phases). Text below kept as written for the review. The owner accepted the *direction* of the requirements document; the
detailed design below is not approved yet and no implementation has started.

## 1. Accepted direction (requirements document, section 3)

| Id | Decision | Source | Status |
|---|---|---|---|
| ZEX-D01 | Several accounts at the same time with different currencies; the main totals are per currency | Owner request | Accepted direction |
| ZEX-D02 | A default currency stays from onboarding on; quick add uses the default account | Owner request | Accepted direction |
| ZEX-D03 | Gold is held in grams/kilograms, plus countable holdings | Owner request | Accepted direction |
| ZEX-D04 | A goal for an account, its progress on Home and an estimate of when it is reached | Owner request | Accepted direction |
| ZEX-D05 | Simple/Advanced is defined for every area | Owner request | Accepted direction |
| ZEX-D06 | Quantity is kept apart from monetary value and from liquidity | Proposal of the document | Accepted direction |
| ZEX-D07 | Three goal types: account balance, earmarked savings, holding quantity | Proposal of the document | Accepted direction |
| ZEX-D08 | ETA as an explained scenario; no promise, no hidden price growth | Proposal of the document | Accepted direction |
| ZEX-D09 | Home answers at most three main money questions; the rest lives in reports | UX proposal, to be confirmed by user tests | Accepted direction |
| ZEX-D10 | A feature already used never disappears or stops working when switching to Simple | Proposal of the document | Accepted direction |
| ZEX-D11 | Pricing, bank connection, sync, AI and market services are not decided here; earlier decisions about them stay | Scope boundary | Accepted direction |

## 2. Design proposals that need the owner's approval

Each proposal names the preferred option, the alternative and the consequence. Low-risk choices are decided in the
design documents and listed in §3; the ones below change the meaning of numbers, the stored data or the scope.

| Id | Topic | Preferred option | Alternative | Consequence of the choice | Design |
|---|---|---|---|---|---|
| ZEX-P01 | Default currency for new data | A new per-profile setting *Default currency for new items* (accounts, goals, holdings' price currency), initialised from today's report currency by the migration; the report currency becomes *Valuation currency (optional)* used only for converted totals and converted charts | Keep one setting for both | Preferred: changing the valuation currency no longer changes the currency of new accounts or the budget shown on Home. Alternative keeps the coupling the requirements reject (ZEX-MC02/MC05) | [02 §3](02-product-and-domain-design.md#3-defaults-and-settings) |
| ZEX-P02 | Budget and Home currency | Home and the Budget page show the budget of the *default account's currency*, with a currency switcher when budgets exist in several currencies | Follow the valuation currency (today's behaviour) | Preferred keeps the everyday budget tied to the money the user spends; a valuation change does not move the Home budget | [02 §3](02-product-and-domain-design.md#3-defaults-and-settings), [03 F3](03-ui-ux-design.md) |
| ZEX-P03 | Account currency lock | An account's currency is locked once anything stores amounts in it (entries, plans, templates, goal earmarks, loan installment, holdings' purchases); the editor explains why and offers "new account in another currency + transfer" | Lock only on entries (today) | Preferred prevents silent relabelling of plan, template and goal amounts found in the review | [01](01-current-state-and-gaps.md), [02 §4](02-product-and-domain-design.md#4-account-selection-contract) |
| ZEX-P04 | Changing the account in the entry form when currencies differ | Keep the typed digits, relabel them to the new currency, show an inline notice with *Undo account change*; never convert; Save shows account and currency | Clear the amount | Preferred keeps the quick flow; the notice makes the change visible (ZEX-MC05) | [02 §4](02-product-and-domain-design.md#4-account-selection-contract), [03 F2](03-ui-ux-design.md) |
| ZEX-P05 | Invalid default account | No silent fallback: the editor opens with *no account selected*, the account field highlighted and Save disabled until a choice; the default account is cleared when it is archived, with a notice offering a new default | Fall back to the first account (today) | Preferred meets ZEX-MC06/AT05; costs one extra tap only when the default became invalid | [02 §4](02-product-and-domain-design.md#4-account-selection-contract) |
| ZEX-P06 | Rate freshness | Every converted value shows the date of the oldest rate used; a rate is marked *may be outdated* when it is older than 30 days or older than the start of the current financial month, whichever is later (Advanced setting: 7 / 30 / 90 days / never); estimated rates are marked *estimate*. The threshold is a reminder rule, not an accuracy claim | Show the date only (today) | Preferred makes old and estimated rates visible without inventing precision | [02 §5](02-product-and-domain-design.md#5-totals-rates-and-conversion) |
| ZEX-P07 | Money side of asset purchases and sales | Two new entry kinds *Asset purchase* and *Asset sale*: they change the money account like a transfer and are neither income nor consumption expense; they link to the quantity event in one group | Model the holding as a monetary account and use transfers | Preferred keeps quantity and value apart (ZEX-D06) and lets every report exclude capital movements by kind; costs one additive change to the entry kinds and the report filters | [02 §7](02-product-and-domain-design.md#7-quantity-holdings) |
| ZEX-P08 | Cost basis | Weighted average cost per holding identity (asset type), in the asset's price currency; a location transfer keeps the basis; a sale removes basis × sold share; opening holdings without a purchase price make the basis *unknown* until the user enters one; realised result is shown only with a known basis | FIFO lots | Preferred is easy to explain and to keep correct under edits; FIFO would need lot selection UI. Not a tax method; labelled so | [02 §7.6](02-product-and-domain-design.md#76-cost-basis-fees-and-realised-result) |
| ZEX-P09 | Fees on asset trades | A fee is its own expense entry (category *Fees*), never part of the basis and never subtracted again in the realised result | Capitalise the fee into the basis | Preferred counts the fee exactly once as spending; the result line shows "fees recorded separately" | [02 §7.6](02-product-and-domain-design.md#76-cost-basis-fees-and-realised-result) |
| ZEX-P10 | Quantity storage | Mass in milligrams (`long`, 1 g = 1000 mg, 1 kg = 1 000 000 mg), count in thousandths of a unit (`long`); purity in parts per 10 000 (18 k = 7500); display rounds to 3 decimals of a gram | Decimal columns | Preferred keeps exact integer arithmetic like money and the g/kg factor exact | [02 §7.2](02-product-and-domain-design.md#72-units-precision-and-purity-zex-p10) |
| ZEX-P11 | Legacy *Asset* accounts | Stay valued monetary assets unchanged; an optional, explicit *Convert to holding* assistant (Advanced) creates a holding from user-entered type and quantity and keeps the old value as a dated valuation; nothing is converted automatically | Migrate automatically | Preferred never guesses grams or counts (ZEX-AS16) | [02 §7.8](02-product-and-domain-design.md#78-legacy-asset-accounts-zex-p11) |
| ZEX-P12 | One balance goal per account | At most one *active* account-balance goal per account; earmark goals on the same account are allowed and labelled as a different kind | Several observational goals per account with an overlap notice | Preferred is the requirements' default (ZEX-GO03) and avoids confusing overlaps | [02 §8](02-product-and-domain-design.md#8-goals) |
| ZEX-P13 | Goal states | States *Active, Paused, Completed, Archived* are stored; *Reached* and *Overdue* are derived and never stored, so a withdrawal after reaching shows the current state again | Store *Reached* | Preferred satisfies ZEX-GO12/GO13 without stale states | [02 §8.3](02-product-and-domain-design.md#83-states-zex-p13-zex-go12-go13) |
| ZEX-P14 | Contribution opportunities | Computed from a real contribution schedule (the existing recurrence engine: weekly, every two weeks, monthly, a day of the financial month, Gregorian or Persian calendar) instead of `AddMonths` | Keep monthly/weekly counting | Preferred fixes two-week pay and the Persian calendar (ZEX-GO11); existing goals keep their frequency mapped to an equivalent rule | [02 §9.1](02-product-and-domain-design.md#91-scenario--the-users-plan) |
| ZEX-P15 | Trend ETA policy | At least 3 complete comparable periods, up to 6 shown, median of the net contribution, one-off periods labelled; zero or negative median gives no date. A product rule, not a validated statistical model | Average of all history | Preferred is robust to one-off deposits; stated as a rule in the app | [02 §9.2](02-product-and-domain-design.md#92-observed-trend-zex-p15) |
| ZEX-P16 | Protected resources for headroom | Only goals explicitly marked *Protect this money* (for example the emergency fund) count as protected, through their covered earmarks (*Money set aside* goals only; a balance goal reserves nothing); no separate account floor in this package | Add a per-account minimum balance | Preferred makes "counted once" true by construction (ZEX-AT24); a floor can be added later as another protected source | [04 headroom](04-kpi-and-report-catalog.md#3-liquidity-headroom-contract) |
| ZEX-P17 | Usable money scope | A per-account flag *Usable for payments* (default on for cash, checking and savings; off for credit card, loan, lent, asset, holdings) defines the liquidity scope; `IncludeInTotals` keeps its meaning for the main totals; credit cards stay in totals as debt but never count as usable money | Derive the scope from the account type only | Preferred lets a user exclude a blocked or foreign account (ZEX-MC11/MC12) without changing totals | [02 §5.1](02-product-and-domain-design.md#51-account-groups) |
| ZEX-P18 | Optional account country | An optional *Country* field per account (never inferred), shown only in Advanced and in the account details | Leave out | Preferred satisfies ZEX-MC11 at small cost; no logic uses it except grouping and display | [02 §5.1](02-product-and-domain-design.md#51-account-groups) |
| ZEX-P19 | Home layout | Home = native balances and quick add, *Needs attention*, then at most three groups: budget, upcoming commitments/shortfall, pinned goals (one or two goals in one group); groups without data are not shown | Keep today's sections | Preferred follows ZEX-D09; existing optional sections (category chart, accounts) stay available in *Customize Home* | [03 F1/Home](03-ui-ux-design.md) |
| ZEX-P20 | Display units and Home layout in backups | Move display units, Home layout and the dismissed-guidance flags from app preferences into the per-profile database settings (so they are per profile and in the backup); theme, language and digits stay device-wide | Keep preferences | Preferred fixes the backup and profile gaps found in the review (ZEX-MC09, ZEX-AS16) | [02 §10](02-product-and-domain-design.md#10-backup-export-and-import) |
| ZEX-P21 | Aggregated entries | An entry can be marked *Aggregated* with a covered date range; importing or adding detailed entries in that range for the same account and category raises an overlap notice with *link and replace* or *keep both* (with explicit double-count warning), always with Undo | No marker | Preferred prevents double counting (ZEX-AT33) and tells KPIs that daily timing is unknown | [02 §6.4](02-product-and-domain-design.md#64-aggregated-entries-zex-p21) |
| ZEX-P22 | Forecast snapshots | A saved snapshot stores scope, assumptions, the daily path and incompleteness at the time of saving and is read-only; comparing it with reality uses today's ledger for the same scope and marks entries recorded after the snapshot | Recompute the old forecast | Preferred keeps the snapshot unchanged (ZEX-AT40) | [02 §9.5](02-product-and-domain-design.md#95-forecast-snapshots-zex-p22), [04 R5](04-kpi-and-report-catalog.md) |
| ZEX-P23 | Wealth change decomposition | Net worth change between two dates = flows + price effect + FX effect + corrections + **unexplained remainder** (shown, never hidden); uses only valuations and rates dated on or before each date | Show the total change only | Preferred answers "savings or price/FX?" honestly (E08) | [04 R5](04-kpi-and-report-catalog.md) |

## 3. Decisions taken in the design (low risk, no approval needed beyond the package)

* Converted totals stay secondary everywhere; native totals always come first (ZEX-MC07/MC08).
* An unknown rate, price or amount never becomes zero; it makes the result *incomplete* with the reason.
* Toman stays a display unit of IRR; CSV, PDF, backups and rates stay in ISO units with the unit named.
* The quick-add widget and the receipt path follow the same account contract; a receipt amount is parsed in ISO
  units of the chosen account, never through a display unit (fixes the 10× issue found in the review).
* Archived accounts are excluded from Home totals like on the Accounts page (consistency fix).
* Sums of minor units use checked arithmetic in the shared calculators; an overflow shows an error instead of a
  wrong number.
* Report and KPI numbers come from one set of definitions in `Vafadar.Zanance.Core`; view models only present them.
* Simple/Advanced changes presentation only (see [05](05-simple-advanced-matrix.md)).

## 4. Risks

| Risk | Effect | Mitigation |
|---|---|---|
| New entry kinds (asset purchase/sale) are missed by an existing report filter | Capital movements could appear as spending | One shared classification (`EntryKinds.IsConsumption`, `IsCapital`) used by every calculator; golden tests per report |
| Settings split (P01) confuses existing users | Users look for "report currency" | Migration keeps the same value in both; Settings shows both with help texts and an example |
| Quantity history validation on edits is complex | A past edit could create negative holdings | Validate the running quantity per holding over the full history on every change; tests with back-dated edits |
| Average cost with unknown opening basis | No realised result for some sales | Shown as *basis unknown* with an action to enter the basis; never estimated |
| Trend ETA misunderstood as a promise | Wrong expectations | Every ETA carries its assumption label; no date without enough data |
| Moving preferences into the database (P20) | A migration could lose the user's units/layout | One-time copy on first start with the new version, kept in preferences until the copy is confirmed |
| Performance with holdings, valuations and daily paths | Slower Home | Calculators work on in-memory sets already loaded; reference data set extended in E09 |
| No App-layer test project exists | View model behaviour (account contract) is untested | Add a view-model test project or move the contract into Core services with tests (backlog ZEX-S0101) |
| iOS cannot be verified | iOS evidence missing | Marked *Blocked: no Mac/iOS device*; iOS ships later as before |

## 5. Approval gate

The owner reviews documents 01–09 and the wireframes and answers:

1. Approve, change or reject each of ZEX-P01 … ZEX-P23.
2. Confirm the epic order and waves in [06](06-implementation-backlog.md) and [09](09-implementation-goal-draft.md).
3. Send a separate implementation goal (done 2026-10-03: approval of the phase plan). Until then the package stayed **Awaiting owner approval** and nothing was
   implemented.
