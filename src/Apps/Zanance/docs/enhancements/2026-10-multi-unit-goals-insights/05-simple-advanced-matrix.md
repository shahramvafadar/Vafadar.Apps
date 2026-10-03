# 05 – Simple / Advanced matrix

Status: **Approved by the owner (2026-10-03)**; implemented phase by phase ([06](06-implementation-backlog.md#phases)). *Simple* is a lighter default presentation, not reduced bookkeeping,
deleted data or a cheaper edition; *Advanced* is not a paid tier. Free/Pro is outside this switch.

## 1. Rules

1. **Presentation only.** The mode never changes a calculation, a category, a schedule, an account's inclusion, an
   earmark, a budget period or any active setting. A report with the same scope gives the same numbers in both modes
   (ZEX-AT28, AT36).
2. **Active data stays reachable.** Whatever exists (a weekly budget, a goal, a holding, a rule, a tag, a rollover)
   keeps at least a traceable summary in Simple, with *Open details* to see and correct it.
3. **Never hidden in Simple:** native currency totals, the account of an entry, financial warnings, correcting a
   balance (reconcile/adjust), security (app lock), backup, restore, basic export, delete data, and the reason of
   any incomplete number.
4. **Creating** complex structures (custom recurrence rules, envelope/flex methods, weekly budgets, holdings with
   purity, contribution methods, scenarios, snapshots) may require Advanced; **viewing and correcting** existing ones
   never does.
5. **One policy, not page conditions.** A single `FeaturePolicy` table in the App layer (key → visibility in Simple,
   create level, summary text) is used by every page; pages ask the policy instead of checking the mode themselves.
6. **Switching** shows what changes ("Simple hides 6 advanced options; your data and numbers stay the same").
7. Onboarding sets Simple explicitly (fixes ZEX-F15) and offers Advanced at the end of setup.

Visibility values: **Always** (both modes) · **Summary** (Simple shows a summary when data exists, details one tap
away) · **Advanced** (Simple shows nothing unless data exists, then Summary).

## 2. The 30 areas

| Id | Area | Simple (default) | Path in Simple | Advanced (create / edit) | Active data in Simple | Home | Help | Acceptance |
|---|---|---|---|---|---|---|---|---|
| ZEX-SA01 | Onboarding | Language, default currency, calendar, first account and opening balance | – | More settings after setup | – | Getting started | "?" on currency and calendar | New user finishes in 3 steps; mode = Simple |
| ZEX-SA02 | Accounts | All active accounts, totals per currency, default account marked | Accounts | Groups, include in totals, usable for payments, country, sort | Archived section always reachable | Native balances | "?" include / usable | Same totals in both modes |
| ZEX-SA03 | Income / expense entry | Amount, kind, category, **account**, date today (changeable) | "More details" | Payee, note, icon, foreign amount, tags, reimbursable directly visible | Existing details open expanded | Quick add | – | Account and currency visible before Save in both modes |
| ZEX-SA04 | Transfer | From, to, amount; both amounts when currencies differ | – | Fee in destination currency, implied rate shown | – | Quick add | Effect line | AT06 in both modes |
| ZEX-SA05 | Multi-currency | Native currency always; converted total secondary | Rates from the converted line | Rate history, freshness setting, valuation currency off | – | Native balances | "?" on converted total | Never only a converted total |
| ZEX-SA06 | Toman / display units | Unit name at fields and amounts where active | Display units | Define units, factors | Active unit always shown | – | Unit note | AT08 |
| ZEX-SA07 | Categories | Main categories, quick pick | Categories | Sub-categories, order, merge, spending type, essential flag | Sub-categories shown under the parent | – | – | – |
| ZEX-SA08 | Search and filter | Period, account, category | Filter button | Combinations, saved filters, tags | Saved filters listed | – | – | – |
| ZEX-SA09 | Refund and correction | From entry details | Entry details | Multi-step settlement details | – | – | – | Refund reachable in Simple |
| ZEX-SA10 | Split | "Split" action in details | Entry details | Full editing, bulk | Split parts shown | – | – | – |
| ZEX-SA11 | Receipt | Add/read a receipt, review result | Home Receipt | Several attachments, file details | Attachments listed | Quick add | – | – |
| ZEX-SA12 | Quick templates | Pinned templates | Home | Create, order, manage | All templates usable | Quick add | – | – |
| ZEX-SA13 | Recurring plans | Common presets, preview of the next date | Plans | Every N, month/holiday rules, pause, this-and-future | Custom rules summarised in one line (fixes ZEX-F08) | Commitments group | "?" on rules | Plan with a custom rule shows its summary in Simple |
| ZEX-SA14 | Due dates and alerts | Due/overdue, next payment, valid warnings | Plans | Calendar view, scenarios, reminder settings | Second reminder summarised | Attention | – | – |
| ZEX-SA15 | Contracts | Amount and important dates when they exist | Plan details | Contract data, stages, advance settlement | Contract summarised (fixes ZEX-F08) | Attention (deadlines) | – | – |
| ZEX-SA16 | Budget | Active budget, remaining, common categories | Insights › Budget | Envelope / flex, rollover, category limits, account scope | Method, rollover and limits summarised (method summary is new) | Budget group | "?" method, rollover | Same remaining in both modes |
| ZEX-SA17 | Weekly / two-week periods | If one exists, it is shown with its real period | Budget period chips appear when such a budget exists | Create and configure all periods | **Shown, with its period, never recomputed monthly** (fixes ZEX-F07) | Budget group follows the period | – | Weekly budget visible in Simple with the same numbers |
| ZEX-SA18 | Account goal | Create a simple balance goal, progress, remaining | Insights › Goals | Contribution plan, priority, alternatives | All goals listed | Goals group (pinned) | "?" observational vs earmark | Goal created in Advanced stays visible in Simple |
| ZEX-SA19 | Goal and ETA | ETA only with its short assumption label | Goal details | Periods, sensitivity, alternative scenarios, capacity | ETA keeps its label | Goal card | "?" ETA | No ETA without label |
| ZEX-SA20 | Quantity holdings | If present: quantity, unit, known value | Accounts › Holdings | Create and manage types, units, purity, events, valuations | Always listed with quantity | Native balances (holdings line when present) | "?" quantity vs value | Holdings visible in Simple when they exist |
| ZEX-SA21 | Debts / loans | Debt and next installment when present | Accounts | Principal/interest table, payoff estimate | Listed | Attention (installment due) | – | – |
| ZEX-SA22 | Receivables | Open amount, record payment | Accounts › Owed to me | Aging, filters, counterparties | Listed | Attention | – | – |
| ZEX-SA23 | Reports | Period overview, simple comparison, data status | Insights › Reports | Net worth, ratios, composition, wealth change | Same numbers | – | "?" per KPI | Same numbers in both modes |
| ZEX-SA24 | Forecast | Valid shortfall warning; understandable card when needed | Insights › Forecast | Daily path, scenarios, snapshots | Snapshots listed | Attention (shortfall) | "?" | Warning appears in Simple when Min < 0 |
| ZEX-SA25 | Reconciliation | Correct a balance, see the difference – always | Account details | Full movement list, investigation tools | – | – | – | Reachable in Simple (documents the existing behaviour, ZEX-F16) |
| ZEX-SA26 | Import / export | Basic export, restore, import guide | More › Import/Export | Mapping, advanced options | Import batches listed with Undo | – | "Export is not a backup" | – |
| ZEX-SA27 | Backup and security | Lock, backup, restore, delete data, last success | More | Same protection; cloud options | – | Attention (backup old) | – | Security identical in both modes |
| ZEX-SA28 | Local profiles | Current profile name, switch when several exist | More › Profiles | Create, manage, delete | Listed | Profile name on Home | – | – |
| ZEX-SA29 | Personalisation | Pin/unpin simple cards, theme | Customize Home | Order and filters of all sections | Hidden sections listed | All | – | – |
| ZEX-SA30 | Free / Pro | Not part of this switch | – | – | – | – | – | No feature is gated by "Pro" through this switch |

## 3. Existing features discovered in the code

| Feature | Area | Today | Designed policy |
|---|---|---|---|
| Home forecast card | SA24 | Advanced only, not computed in Simple | Advanced card; in Simple the shortfall warning only (computed in both) |
| Entry "More details" | SA03 | Collapsed in Simple for new entries | Keep |
| Plan presets / custom repeat | SA13 | Presets in Simple, custom kept visible | Keep, add one-line summary |
| Plan calendar, day rule, missing day, second day, weekend rule, holidays | SA13 | Hidden in Simple; partly summarised | Summary of all of them |
| Plan end | SA13 | Summarised | Keep |
| Second reminder on due date | SA14 | Hidden, no summary | Summary |
| Contract details | SA15 | Hidden, no summary | Summary |
| Budget period | SA17 | Forced to month in Simple | Show existing periods |
| Rollover | SA16 | Summarised | Keep |
| Budget method | SA16 | Not summarised in editor | Summary |
| Category limits | SA16 | Count summarised | Keep |
| Budget account scope | SA16 | Note on page | Keep |
| Tags, rules, saved filters, bulk operations | SA08 | Both modes | Always |
| Reimbursable amounts | SA22 | Both modes | Always (from details) |
| Templates on Home, receipt, widget | SA11/12 | Both modes | Always |
| App lock, notification details | SA27 | Both modes | Always |
| Theme, Persian digits, financial month, week start, region | SA29 / SA01 | Both modes | Always |
| Getting started card | SA01 | Both modes | Always (until done or hidden) |
| Help "?" buttons | all | Both modes | Always |

## 4. New features of this package

| Feature | Simple | Advanced |
|---|---|---|
| Default currency / valuation currency settings | Both rows with help | Valuation currency *Off*, freshness days |
| Account contract notices (currency change, invalid default) | Always | Always |
| Usable for payments, country | Not shown; defaults apply | Editable |
| Holdings | Listed when present | Create / edit |
| Goal types, pin to Home | Balance goal create; pin | All types, contribution methods |
| Trend ETA, capacity | Label only when valid | Details |
| Headroom | Warning when negative | Card and parts |
| Essential-spending estimate | – | Editable |
| Aggregated entries | Marker shown; overlap notice always | Create from the editor |
| Period-end review | Always (guided list) | Same |
| Forecast snapshots | Listed when present | Save / compare |
| Wealth history | – | R5 |
| Data status | Always | Always |

## 5. Mode-switch acceptance

* Switch Advanced → Simple with a weekly budget, a balance goal pinned to Home, a 20 g holding, a rollover, a custom
  plan rule and two tags: every number on Home, Budget, Goals, Accounts and Reports stays equal; each item has a
  visible summary or list entry (ZEX-AT28).
* Switch back: nothing was changed or reset.
* Automated: a policy test enumerates all `FeaturePolicy` keys and asserts that none of them changes a calculator
  input; a view-model test compares the numbers of Home and reports in both modes (ZEX-S0503).
