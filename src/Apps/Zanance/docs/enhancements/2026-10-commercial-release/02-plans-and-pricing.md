# 02 – Plans, pricing and the rules around them

Status: **owner decision (D-61, 2026-10-07) for the plan model; prices and offers are design and sandbox values, not
yet store prices.** The owner approved OD-03 and Section 2 on 2026-10-10 (D-117). ENT-01 implements only the
pure Core policy and counts; store integration, enforcement and release activation remain separate work in
[04-backlog.md](04-backlog.md). Test builds remain unrestricted.

## 1. Plan model

**Free → Plus → Pro.** No fourth plan for now.

| Plan | Meaning |
|---|---|
| Free | Everyday money management on one device, complete for a person with a few accounts. No online account. |
| Plus | Every local capability without limits: advanced budgets, analysis, holdings, automation, several profiles. |
| Pro | Plus, plus the online services: automatic sync between one's own devices and one shared space (owner + up to 5 members). |
| Plus Lifetime | A one-time purchase of the **local Plus capabilities**. Not a plan of its own and not "forever everything": no Pro services, no AI credit, no Tax add-on, no bank connection, no renewal. |

* Plan (what is unlocked) and purchase kind (monthly, yearly, lifetime) are separate: Plus monthly, Plus yearly and Plus
  Lifetime unlock the same capabilities with different lifetimes of the right.
* **Never behind a plan:** security (app lock, database encryption once built, backup encryption), languages,
  calendars, currencies, holidays, accessibility, themes, Simple/Advanced, viewing all history, correcting mistakes,
  warnings about shortfalls or incomplete data, backup, restore, deletion and the user's own export.
* **Simple/Advanced is not Free/Plus.** `FeaturePolicy` decides what a screen shows; it is never the payment check
  (MON-05 stays).
* Capabilities that are not built and tested are never sold. "Coming soon" appears only on a transparent roadmap that
  follows the store rules, never on the purchase screen.

## 2. Capability matrix

"Active" means usable for new work (new entries, new automation). Archived, paused-by-limit or read-only items are
always visible, counted in history and exportable. The owner confirmed the rules and tool table below on
2026-10-10 (OD-03 / D-117); this approves the final model, not enforcement in test builds.

| Area | Free | Plus (incl. Lifetime) | Pro |
|---|---|---|---|
| Languages, supported currencies, three calendars, existing holidays | full | full | full |
| App lock, biometrics, database encryption (when built), protection of data | full | full | full |
| Light/dark, accessibility, Simple/Advanced | full | full | full |
| Recording transactions, history | unlimited | unlimited | unlimited |
| Active personal financial accounts | 3 | unlimited | unlimited |
| Active budgets | 1 simple budget (limits method) | unlimited, all methods | as Plus |
| Active goals | 1 simple goal (account balance) | unlimited, all kinds (earmark, quantity, contribution plans) | as Plus |
| Active recurring plans | 5, with basic reminders | unlimited; nth weekday, second day, weekend/holiday rules, contracts, settlements, auto-post | as Plus |
| Basic report: income, expenses, balance | full | full | full |
| Explanations of errors/incomplete data, essential financial warnings | full | full | full |
| Wealth analysis, advanced reports (KPIs, commitments, history), advanced PDF | — | full | full |
| Detailed forecast, scenarios, saved forecasts and comparison | basic forecast to month end | full | full |
| Envelopes, flex, rollover, weekly/two-week budgets, financial month start | — (financial month start: free) | full | full |
| Holdings (weight/count, purity, prices, purchases/sales) | view existing data | create and manage | as Plus |
| Loan analysis (rate, schedule), split transactions, rules, receipt reading (on device) | — | full | full |
| Local profiles | 1 | several | several |
| Encrypted backup, restore, basic CSV | full | full | full |
| Backup to the user's own Google Drive / OneDrive | full (optional connection) | full | full |
| Automatic sync between one's own devices | — | — | full |
| Joining a Pro owner's shared space | by invitation | by invitation | yes |
| Creating and hosting a shared space | — | — | one space, up to 6 members incl. owner |
| AI | separate credit, after launch | separate credit | separate credit; never unlimited |
| Tax | separate add-on per country/year, after launch | add-on; basic export for an accountant included | as Plus |
| Bank connection | add-on decision after cost/market review | same | not assumed in Pro |

### Confirmed tool allocation (OD-03)

| Tool | Approved allocation | Reason |
|---|---|---|
| Quick templates | Free: up to 3; Plus: unlimited | Everyday speed is Free; many templates are power use. |
| Tags, manual attachments, notes, search | Free | Recording and documenting is core. |
| Bulk operations | Free for archive/delete/categorize; Plus none extra | Correcting data must never be paid. |
| Categorization rules | Plus | Automation. |
| Saved filters | Free: 1; Plus: unlimited | |
| Home customization | Free | Personalization of the overview is not a selling point. |
| Display units (e.g. Toman) | Free | Part of correct currency display. |
| Reimbursable expenses, refunds, transfers across currencies, fees | Free | Correct calculation, never weakened by plan. |
| Aggregated entries | Free | Recording. |
| Generic CSV import with mapping, migration presets | Free | Moving in must be easy; export is never paid. |
| Android quick-add widget | Free | |
| Receipt reading (OCR on device) | Plus | Owner's table. |
| Reminders | Free for plans within the quota; contribution and review reminders Plus | |

### Counting rules (precise, before any enforcement)

* **Financial account** (cash, checking, savings, credit card, loan, money lent, asset account) – a quota of the
  personal profile. **Online identity**, **local profile** and **shared space** are different things; no quota mixes
  them. Accounts a user sees in someone else's shared space never count against the user's 3.
* **Active** = not archived. Archived accounts, ended goals, ended plans are free and unlimited.
* **Budget:** one budget = one budget definition (scope, currency, period kind) that is used for the current period.
  Past periods and copied months of the same definition are history, not extra budgets. Free = one definition with
  the limits method.
* **Pause** is not a way around a quota: a paused goal or plan still counts as active; pausing cannot bypass the quota.
* **Created from template, import, restore, duplicate:** every creation path checks the same quota in the service
  layer, not in the button. Restore and import are never refused: data above the quota arrives read-only for new
  work (see §5).
* **Shared space members:** six identities including the owner. A pending invitation holds a seat until it expires
  (7 days) or is withdrawn; a member who leaves frees the seat at once. Devices per identity and server
  storage are separate, cost-based decisions; no new limits are announced without one.

## 3. Prices (design and sandbox; euro market, consumer prices incl. VAT)

| Product | Monthly | Yearly | Lifetime |
|---|---:|---:|---:|
| Free | €0 | €0 | – |
| Plus | €2.99 | €24.99 | **€69.99 once (Plus Lifetime)** |
| Pro | €4.99 | €39.99 | **none** |

* These are not net revenue: store fees, VAT, refunds and regional prices are modelled separately
  ([03-architecture.md §6](03-architecture.md#6-service-economics)).
* Prices and purchase texts shown in the app always come from the store catalog of the user's store country, never
  from a string or from the user's exchange rates. The UI language does not decide the store country.
* The yearly amount is the one shown prominently; a monthly equivalent is only a help text. "Yearly vs 12 × monthly"
  and "campaign discount on yearly" are separate statements with their own reference prices.

### Plus Lifetime rules

* Store product: **non-consumable** one-time purchase, not a subscription with an invented end date.
* The capabilities sold as Plus are never taken away by renaming or moving them to Pro. The terms version and the
  entitlement set at purchase are recorded.
* Lifetime while having Pro: the Plus base right stays; when Pro ends, the user returns to Plus Lifetime, not Free.
* Lifetime owners get **25 % off the standard Pro price** as the business goal, never combined with another discount.
  Duration, renewals and the mapping to each store's offer types must be confirmed before it is promised publicly
  ([06-open-decisions.md](06-open-decisions.md), OD-07).
* A Lifetime owner is never invited to buy Plus again. Someone with a Plus subscription who buys Lifetime is told
  plainly that the subscription renews until they cancel it in the store; no automatic cancellation or refund is
  claimed without store confirmation.

### Campaigns

Three to four limited campaigns a year, never a permanent discount.

| Product | Campaign price | Afterwards |
|---|---:|---|
| Plus yearly | €19.99 for the first eligible period | renews at the standard price shown at purchase |
| Pro yearly | €29.99 for the first eligible period | same rule |
| Plus Lifetime | €49.99 once | no renewal |

Every offer has an id, store and country, start and end, eligible group, discounted period, the price afterwards and a
no-stacking rule. The Lifetime owners' 25 % is never applied on a campaign price; when two offers compete, the best
eligible one is shown with its renewal terms. No invented strike-through prices, no resetting countdowns, no
pre-selected purchases.

### Trial

Goal: **14 days of Pro** for eligible people, never started silently, with the renewal price, the end of the trial
and the cancel path shown before confirming. Two possible flows ([06-open-decisions.md](06-open-decisions.md),
OD-06): (a) a **store introductory free trial** on the Pro yearly/monthly subscription (auto-renews; eligibility
handled by the store per account), or (b) an **in-app trial without renewal**. Proposal: (a), because eligibility is
enforced by the store and no own trial server state is needed; changing product, device or profile must not create
endless trials.

## 4. Shared space and sync rules

* Only the owner of the shared space buys Pro. A guest has, inside that space and according to the role, the
  capabilities of Plus; this does not spread to the guest's own profiles or private data.
* A guest syncs the data of that shared space only; this is not free sync of the guest's personal data. A personal
  Pro purchase never allows reading other members' private data.
* Backup to the user's own cloud (Free), automatic sync (Pro), joining a shared space (any plan, by invitation) and
  creating a shared space (Pro) are explained as four different things in every text.

## 5. End of a plan, downgrade and quotas

Data the user is entitled to is never deleted, truncated or locked: viewing, history, backup, restore, deletion and
basic export stay. Calculations on existing data give the same results in every plan; only creating new items and
running advanced automation follow the plan.

| Situation | Behaviour |
|---|---|
| 10 active accounts, back to Free | The user picks 3 accounts for new entries; the others stay visible, in totals as before, reconcilable and editable for corrections, but take no new entries until chosen or upgraded. Nothing is archived or deleted automatically. |
| Several goals | All stay visible with their history and earmarks; one stays active for new contributions; earmarked money is not released silently. |
| More than 5 plans | Choose 5 to keep generating occurrences and reminders; the others stop producing new occurrences (open past occurrences can still be settled or skipped). |
| Envelope/flex budget | Shown read-only with its numbers; the user can keep one limits budget active. |
| Several profiles | All profiles stay openable for viewing, backup, export and deletion; new work only in the chosen one. |
| Holdings | Values and history visible; no new holdings or price entries. |
| Open item needs a fix (wrong amount, reconcile, settle an overdue occurrence, close a loan) | Always allowed. |
| Restore or import above the quota | Always succeeds; data above the quota is read-only for new work. A backup never revives a paid entitlement or a revoked membership. |
| Pro owner's subscription ends | The shared space is not deleted at once: read-only grace period, export for members, then a retention rule announced before sale (OD-05). Local history on each device stays. |

The final counting rules are confirmed (OD-03 / D-117), but activation remains separate; **no commercial limit is applied to test builds
before the owner's approval**, and test access is never a permanent customer entitlement.
