# 03 – UI and UX design

Status: **Approved by the owner (2026-10-03)**; implemented phase by phase ([06](06-implementation-backlog.md#phases)). The visual system, logo, semantic colours, light/dark themes, Persian RTL
and the navigation (Home · Transactions · Plans · Insights · More) stay as approved ([../../03-ux-design.md](../../03-ux-design.md)).
Nothing here is a screenshot of an implementation: the wireframes below and in [wireframes/](wireframes/index.html)
use fictitious data and show hierarchy, controls and actions only.

Review basis: the current UI was inspected in the Windows Debug build with the snapshot walk-through (fictitious
data; en/fa/de; light/dark; 360, 412 px and wide) and the Android release build on an emulator; no real device and no
user test were used. iOS was not run.

## 1. Screen inventory (affected screens)

| Id | Screen | Change | Entry points | Back / input kept |
|---|---|---|---|---|
| ZEX-UI01 | Home | Native balances + holdings line, quick add, attention, ≤ 3 groups (budget, commitments, goals) | Tab | – |
| ZEX-UI02 | Entry editor | Account chip under the amount, contract of [02 §4](02-product-and-domain-design.md#4-account-selection-contract), effect line, aggregated marker | Home, +, templates, widget, receipt, account page, occurrence, edit | Discard confirmation keeps input on error |
| ZEX-UI03 | Accounts | Groups: money, credit cards, debts, receivables, valued assets, holdings; totals per currency per group; default marker; converted line optional | More › Accounts, Home balance card | – |
| ZEX-UI04 | Account details | *Add entry here*, converted value (optional), last reconciled date, usable flag, country (Advanced), currency lock explanation | Accounts | – |
| ZEX-UI05 | Holdings list | Per asset type: quantity, unit, value or *unknown*, fine-metal line | Accounts › Holdings | – |
| ZEX-UI06 | Asset type editor | Kind, dimension, metal, purity, unit weight, count unit, divisible, price currency | Holdings › + | Discard confirmation |
| ZEX-UI07 | Holding details | Quantity per location, events, valuations, basis status, realised/unrealised | Holdings | – |
| ZEX-UI08 | Asset event editor | Opening, purchase, sale, transfer, gift, outflow, correction; money side and fee; history validation | Holding details, Home (Advanced: quick add *Buy/Sell*) | Discard confirmation |
| ZEX-UI09 | Valuation editor | Price per unit or total, date, currency | Holding details | – |
| ZEX-UI10 | Goals list | Types, states, pin indicator | Insights › Goals | – |
| ZEX-UI11 | Goal editor | Type chooser, account/asset, target, date, contribution plan, live preview of required contribution and ETA | Goals › +, Home card | Discard confirmation |
| ZEX-UI12 | Goal details | Progress, contribution history, ETA panel (scenario / trend / capacity), states, earmarks | Goals, Home | – |
| ZEX-UI13 | Reports hub | Six packages R1–R6 with a scope bar | Insights › Reports | Scope kept per session |
| ZEX-UI14 | KPI explanation sheet | Definition, formula in words, included/excluded, data status, *Show entries* | "?" on every KPI | – |
| ZEX-UI15 | Settings › Money and months | Default currency, default account, valuation currency, rate freshness | More › Settings | – |
| ZEX-UI16 | Period-end review | Guided checklist | Home attention, Reports R6, reminder | Progress kept |
| ZEX-UI17 | Aggregated overlap sheet | Link and replace / keep both, preview, Undo | After saving or importing overlapping entries | – |
| ZEX-UI18 | Forecast snapshots | Save, list, compare with reality | Insights › Forecast (Advanced) | – |
| ZEX-UI19 | Wealth change (R5) | Net worth over time, decomposition | Reports | – |
| ZEX-UI20 | Data coverage (About) | What backup / CSV / PDF contain; platform feature matrix | More › About | – |

## 2. Home (ZEX-UI01, ZEX-P19)

```
┌──────────────────────────────────────────┐
│ Zanance                     [Profile: Me] │
│ ┌──────────────────────────────────────┐ │
│ │ Recorded balance                     │ │
│ │ 2,000.00 EUR                         │ │  ← native totals, largest type, one line each
│ │ 4,000.00 USD                         │ │
│ │ 100,000,000 IRR                      │ │
│ │ Gold 18k 20 g · 24k 20 g · 3 coins   │ │  ← one item per asset type, never summed (02 §7.3)
│ │ ≈ 6,680 EUR · rates of 12 Aug  (?)   │ │  ← converted, secondary, amber "may be outdated"
│ └──────────────────────────────────────┘ │
│ ┌──────────────────────────────────────┐ │
│ │ [Expense] [Income] [Transfer] [Recpt]│ │  ← quick add; default account named below
│ │ to Main · EUR            change ›    │ │
│ │ (Groceries) (Coffee)                 │ │  ← quick templates
│ └──────────────────────────────────────┘ │
│ Needs attention                          │
│ │ Due or overdue: 2                    › │
│ │ Plans use protected money ~24 Oct    › │  ← headroom below 0 (04 §3); the K02 text only when the balance itself falls below 0
│ │ Rates older than 30 days             › │
│ Budget · October (EUR) ▾                 │  ← currency switcher only with several budgets
│ │ 250.00 EUR left · 20.83 per day      › │
│ │ ███████████████░░░░░ 76 %              │
│ Next 30 days                             │
│ │ 1,200.00 EUR + ≈ 30 · 1 unknown      › │  ← K04
│ │ [29] Rent              −950.00 EUR     │
│ Goals                                    │
│ │ Emergency fund   2,000 / 5,000 EUR   › │  ← pinned goal(s), at most two in one group
│ │ ████████░░░░░░░░ 40 % · 3,000 left     │
│ │ ~ Sep 2027 if 250 EUR monthly          │  ← ETA only when valid, with its assumption
│                                  [ + ]   │
└──────────────────────────────────────────┘
```

* **Order:** balances and quick add always first; attention items only when valid; then at most three groups in this
  order: budget, next 30 days, goals. A group without data is not shown (a single-account user without plans, budget
  or goals sees balances, quick add and recent entries).
* Recent entries and the optional category chart and account list stay available in *Customize Home* (existing).
* The default account line under quick add ("to Main · EUR") makes the target visible; *change* opens the account
  picker for this entry only (does not change the default).
* With **no valid default account** the line reads "Choose an account" in amber and quick add opens the editor
  without an account (F2).
* **Wide windows:** the 720 px centred column stays; no second column.

## 3. Key flows

Each flow lists entry points, steps, the wireframe of the key screen, interaction rules, states and the
Simple/Advanced difference. Numbers are fictitious. Each flow takes its numbers from one worked example (02 §11, 04); within a flow they agree, across flows they may differ.

### F1 – Setup and the three defaults; a second account in another currency

Entry: onboarding (new users), Settings › Money and months (existing users).

```
Onboarding step 2 of 3              Settings › Money and months
┌──────────────────────────────┐    ┌───────────────────────────────────┐
│ How should amounts appear?   │    │ Default currency for new items (?) │
│ Currency for your accounts   │    │ EUR ▾                              │
│ EUR ▾                     (?) │    │ Default account for new entries (?)│
│ Calendar                     │    │ Main (EUR) ▾                       │
│ (●) Gregorian ( ) Persian    │    │ Valuation currency (optional)  (?) │
│ Date example: 2 Oct 2026     │    │ EUR ▾          [ ] Off (Advanced)  │
│ [Back]            [Next]     │    │ Rates may be outdated after (Adv.) │
└──────────────────────────────┘    │ 30 days ▾                          │
                                    └───────────────────────────────────┘
```

* Onboarding asks one currency; it initialises all three settings. The help ("?") explains the difference with an
  example ("Your accounts can be in EUR and USD. New accounts start in EUR. Converted totals use EUR.").
* Settings shows the three rows separately; changing one shows "Only new items use this. Existing accounts and
  entries keep their currency."
* **Second account:** Accounts › + → name, type, *currency* (default from the setting, changeable), opening balance,
  date → Save. Home now shows two native lines. If no rate exists, the converted line says "≈ – add a USD rate" (link).
* States: currency picker searchable; an account with stored amounts shows its currency locked with the reason
  (ZEX-P03).

### F2 – Quick add from Home, template and account page; switching account; invalid default

```
New expense                                   (Cancel)
┌──────────────────────────────────────────┐
│ 25.00                               EUR  │ ← amount, keypad focused
│ [■ Main · EUR ▾]                          │ ← account chip, always visible
│ ⓘ The amount is now in USD (was EUR).    │ ← only after an account change, with
│   Check it before saving.  [Undo change] │   Undo; never converts
│ Category  (Groceries)(Coffee)(Housing)…  │
│ Date  Today ▾                            │
│ More details ▸                           │
│ [        Save · −25.00 EUR from Main   ] │ ← effect on the button
└──────────────────────────────────────────┘
```

* Entry points and context: Home *Expense* → default account; template → template account; account page *Add
  entry here* → that account; widget → default account; receipt → default account, amount in ISO units.
* Switching to an account in another currency keeps the digits, relabels them, shows the notice; *Undo change*
  restores the previous account. Fee/destination amounts are cleared with the same notice.
* Invalid default: the chip reads "Choose the account for this entry" (amber outline), Save disabled with the reason
  shown under the button; nothing is preselected.
* Template with an archived account: opens with category/title, amount and account empty, notice "The template's
  account is archived – choose an account and the amount".
* Nothing changes until Save; *Cancel* with input asks "Discard?" (existing).
* Simple: account chip visible; Advanced: details expanded by default (existing).
* Accessibility: the chip is a button named "Account: Main, euro"; the notice is announced (live region).

### F3 – Accounts overview, native totals, details and optional converted value

```
Accounts                                         (+)
Money                                 2,000.00 EUR
  Main ★ default                      1,250.00 EUR
  Savings                               750.00 EUR
  Dollar account                      4,000.00 USD
  Rial account                  100,000,000 IRR
Credit cards                            −80.00 EUR
Owed to me                              480.00 EUR
Debts                                −4,000.00 EUR
Holdings               18k 20 g · 24k 20 g · 3 coins ›
≈ in EUR (rates of 12 Aug)            6,680.00 EUR  (Advanced toggle "Show in EUR")
Archived (2) ›
```

* Totals per currency inside each group; groups without accounts are not shown.
* Account details: balance, *Add entry here*, movements, reconcile (always), "Last reconciled 12 Sep" (new),
  converted value line (only with a rate, labelled), *Usable for payments* and *Country* (Advanced), earmarks.
* Archived accounts are excluded from current totals and listed below.

### F4 – Holdings: create a weight or count asset, opening holding, purchase, partial sale, location transfer

```
New asset type                       Purchase · 18k gold
┌───────────────────────────┐        ┌───────────────────────────────────┐
│ Name  18k gold            │        │ Quantity   10.000 g  ▾ (g/kg)      │
│ Kind  (Precious metal) …  │        │ Location   Home safe ▾             │
│ Measured by (●) weight    │        │ Paid from  Main · EUR ▾            │
│             ( ) count     │        │ Price  (●) per g ( ) total         │
│ Metal  Gold ▾             │        │        50.00 EUR per g             │
│ Purity 18 k ▾ (750/1000)  │        │ Paid       500.00 EUR (computed)   │
│ Price currency EUR ▾      │        │ Fee        20.00 EUR (expense)     │
│ [Save]                    │        │ Date       Today ▾                 │
└───────────────────────────┘        │ ─ After saving ─────────────────── │
                                     │ Main: 1,250.00 → 730.00 EUR        │
                                     │ 18k gold: 20.000 → 30.000 g        │
                                     │ Spending: only the 20.00 fee       │
                                     │ [ Save purchase ]                  │
                                     └───────────────────────────────────┘
```

* *Opening holding* (first event of a new type or "I already own some"): quantity, location, date, optional purchase
  price; no money account field; the summary says "No money is taken from an account."
* Partial sale: quantity (shows "available on that date: 20.000 g"), receiving account, proceeds, fee; summary shows
  cash +, quantity −, realised result (or "basis unknown – enter the purchase price of the opening holding").
* Location transfer: from, to, quantity; summary "Total stays 20.000 g; no income or sale".
* Overselling or a back-dated change that would make a quantity negative: field error with the date and available
  quantity; Save disabled.
* Count type with unit weight: quantity in units ("3 coins"), derived weight shown in secondary text ("30.000 g").
* Simple: holdings are listed and their quantity is visible; creating types and events is in Advanced (an existing
  holding can be corrected in Simple through its details).
* Delete of a purchase: "Delete the purchase? The 10 g, the payment of 500.00 EUR and the 20.00 EUR fee are removed together." + Undo.

### F5 – Goals: create, edit, contribute, withdraw, reach, complete, pin

```
New goal                                    (Cancel)
What do you want to reach?
(●) A balance on one account   ( ) Money set aside   ( ) A quantity of an asset
Name        Emergency fund
Account     Savings · EUR ▾
Target      5,000.00 EUR
By (optional)  Oct 2027 ▾
Contribution plan (optional)
  250.00 EUR  every month on the 25th ▾
[ ] Protect this money (count it as reserved)     ← earmark goals only
[✓] Show on Home
─ Preview ─────────────────────────────────────
Now 2,000.00 · 40 % · 3,000.00 left
With 250 EUR monthly: 12 times → ~ 25 Sep 2027
To be ready by Oct 2027: 231.00 EUR per month (13 dates)
[ Save goal ]
```

* The type chooser explains each in one line; the balance goal says "follows the account balance; reserves nothing".
* The preview updates on every change (ZEX-GO14). A past date shows "The date has passed – 3,000 EUR are needed now".
* Goal details: progress bar teal, real numbers ("2,250 / 2,000 EUR · 250 above target"), state chip (Active, Paused,
  Reached, Overdue, Completed, Archived), actions *Pause*, *Complete*, *Archive*, history.
* Withdrawal from the account after reaching: card returns to "Not reached · 1,800 / 2,000" (no stale "reached").
* Completing: "Mark as completed? The history stays; money set aside is released." (earmark goals).
* Pin: at most two pinned; pinning a third asks which one to replace.
* Simple: create balance goals, see all goals; Advanced: all types, contribution methods, priorities.

### F6 – ETA: assumptions, insufficient data, suggested contribution to confirm

```
When can I reach it?
┌────────────────────────────────────────────┐
│ Your plan      250 EUR monthly → ~ Sep 2027  │  scenario (always available)
│ Recent pace    230 EUR/month (median of 4)   │
│                → ~ Nov 2027                  │  trend (≥ 3 complete months)
│ Capacity       up to 320 EUR/month free      │  capacity (≥ 3 months, shared)
│ (?) These are estimates, not promises.       │
│ [ Use 280 EUR as my plan ]                   │  ← suggestion, applied only on tap
└────────────────────────────────────────────┘
States: "Not enough history yet (2 of 3 months)" · "No date at your current pace" ·
"Target date passed – 3,000 EUR needed now" · "Paused"
```

* Each line has its own label and help; no line without its assumption. A one-off month is marked "includes a
  one-off deposit".
* Accepting a suggestion stores the contribution plan; it changes no budget and no entry.

### F7 – Reports hub, scope, KPI cards and drill-down

```
Reports                                   Scope: October · EUR · Accounts in totals ▾
[Period overview] [Commitments] [Goals] [Holdings] [Wealth] [Data status]
┌──────────────────────────────────┐ ┌──────────────────────────────┐
│ Surplus (?)                      │ │ Spending change (?)          │
│ +700.00 EUR                      │ │ Groceries +30 (+20 %)        │
│ Income 3,000 · Spending 2,300    │ │ Oct 1–12 vs Sep 1–12         │
│ [Show entries]                   │ └──────────────────────────────┘
└──────────────────────────────────┘
Data status: 3 unreviewed · USD rate from 12 Aug ›
```

* Scope bar: period, calendar, accounts, currency (native or converted), confirmed-only; changes apply to every card.
* "?" opens the KPI sheet (ZEX-UI14): definition, included/excluded, the data status relevant to it, *Show entries*.
* Drill-down passes period, accounts, categories **and currency**; the list shows the same total as the card.
* Multi-currency charts ask for a currency or the converted view (with rate dates).

### F8 – Switching Simple/Advanced with advanced data present

* Settings › Experience: switching to Simple shows "Simple hides 6 advanced options. Your data and numbers stay the
  same." with the list (weekly budget, contribution plan, 2 custom plan rules, …).
* In Simple, each existing item stays reachable with a summary (for example Budget: "Weekly budget, week of 29 Sep ·
  120 EUR left" with *Open details*). Nothing is recomputed in another period.

### F9 – Period-end review, aggregated and detailed entries, overlaps

```
Review September                               2 of 5 done
[✓] 4 unreviewed entries confirmed
[✓] Plans: 2 open items handled
[ ] Main: last reconciled 12 Aug – compare balance ›
[ ] Goals: Emergency fund 40 % – contribution of Sept? ›
[ ] Backup: last backup 34 days ago – back up now ›
[Finish review]
```

* Reached from Home attention after the financial month ends, from R6, or from an optional reminder; it only links
  existing actions.
* Overlap sheet after adding detailed groceries in September while "Groceries – September (aggregated) 412 EUR"
  exists: "These 6 entries (395 EUR) fall inside the aggregated amount. *Replace*: keep the 395 EUR detailed and
  reduce the aggregate to 17 EUR · *Keep both*: counts 395 EUR twice". Preview, then Undo for 8 s.

### F10 – Forecast snapshot, comparison with reality, backup and restore of new data

* Forecast (Advanced) › *Save snapshot* → name ("October plan") → saved, read-only.
* Snapshot › *Compare with reality*: two paths (snapshot violet dashed, reality violet solid), the difference per day,
  and the parts: *recorded later*, *plans changed*, *unplanned spending/income*.
* Backup page: "Included: accounts, entries, plans, budgets, goals, holdings and valuations, snapshots, settings
  of this profile. Not included: theme, language, device settings." Restore preview lists counts of accounts,
  entries, plans, goals, holdings and snapshots.

## 4. States

| State | Pattern (existing patterns kept) | New uses |
|---|---|---|
| Empty | Icon + one sentence + primary action | Holdings ("Add gold, coins or other things you own"), goals, snapshots |
| Loading | Busy indicator after 300 ms | Reports with many holdings |
| Error | Inline with retry; input kept | Oversell, overflow ("The number is too large") |
| Incomplete | Amber label with reason | Missing price, rate, unknown amount, unknown basis |
| Archived | Grey row in an *Archived* section | Asset types, locations, goal states |
| Overdue | Red date tile / label | Goals past date, receivables past due |
| Zero | Real zero shown with meaning ("0.00 EUR left") | Budget exactly used |
| Negative | "−" sign, red only for problems | Negative balances, deficit, negative headroom |
| Many items | Lists virtualised, groups collapsible | 50+ asset types, 12-month commitments |
| Not available | "not available" with reason | Ratios with zero denominator |

## 5. Microcopy (en / de / fa)

Persian sentences never start with an amount or a Latin value (project rule); amounts use Persian digits in the
Persian UI, currency codes stay Latin, the minus sign is U+2212 isolated. German uses the formal "Sie" as in the app.

| Key (indicative) | English | German | Persian |
|---|---|---|---|
| `Entry_CurrencyChanged` | The amount is now in USD (was EUR). Check it before saving. | Der Betrag ist jetzt in USD (vorher EUR). Bitte vor dem Speichern prüfen. | مبلغ اکنون به USD است (قبلاً EUR). پیش از ذخیره بررسی کنید. |
| `Entry_ChooseAccount` | Choose the account for this entry. | Wählen Sie das Konto für diese Buchung. | حساب این ثبت را انتخاب کنید. |
| `Home_ConvertedOld` | ≈ 6,680 EUR at rates of 12 Aug – may be outdated | ≈ 6.680 EUR zu Kursen vom 12. Aug. – möglicherweise veraltet | جمع تبدیل‌شده: حدود ۶٬۶۸۰ EUR با نرخ‌های ۱۲ اوت – شاید قدیمی باشد |
| `Settings_DefaultCurrencyHint` | Only new items use this. Existing accounts and entries keep their currency. | Nur neue Einträge verwenden dies. Bestehende Konten und Buchungen behalten ihre Währung. | فقط موارد جدید از این استفاده می‌کنند. حساب‌ها و ثبت‌های فعلی ارز خود را نگه می‌دارند. |
| `Goal_EarmarkNote` | Setting money aside only marks it; it stays in the account. | Zurücklegen markiert das Geld nur; es bleibt auf dem Konto. | کنار گذاشتن پول فقط آن را علامت می‌زند؛ پول در همان حساب می‌ماند. |
| `Goal_BalanceNote` | This goal follows the account balance; it does not reserve money. | Dieses Ziel folgt dem Kontostand; es reserviert kein Geld. | این هدف موجودی حساب را دنبال می‌کند و پولی رزرو نمی‌کند. |
| `Goal_EtaScenario` | If you set aside 250 EUR every month: about 12 months (Sep 2027). Not a guarantee. | Wenn Sie jeden Monat 250 EUR zurücklegen: etwa 12 Monate (Sep. 2027). Keine Garantie. | اگر هر ماه ۲۵۰ EUR کنار بگذارید: حدود ۱۲ ماه (شهریور ۱۴۰۶). تضمینی نیست. |
| `Goal_EtaNoData` | Not enough history yet (2 of 3 months). | Noch zu wenig Verlauf (2 von 3 Monaten). | هنوز سابقهٔ کافی نیست (۲ از ۳ ماه). |
| `Goal_EtaNoPace` | No date at your current pace. | Bei Ihrem aktuellen Tempo kein Datum absehbar. | با روند فعلی، تاریخی قابل‌برآورد نیست. |
| `Goal_PlanNotTransfer` | This is a plan for you – Zanance moves no money. | Das ist Ihr Plan – Zanance überweist kein Geld. | این فقط برنامهٔ شماست؛ Zanance پولی جابه‌جا نمی‌کند. |
| `Asset_ValueUnknown` | Value unknown – add a price to include it in totals. | Wert unbekannt – geben Sie einen Preis ein, um ihn einzurechnen. | ارزش نامعلوم است؛ برای محاسبه در جمع، قیمت وارد کنید. |
| `Asset_FineGold` | ≈ 35.00 g fine gold (2 types with known purity) | ≈ 35,00 g Feingold (2 Sorten mit bekanntem Feingehalt) | طلای خالص: حدود ۳۵٫۰۰ گرم (۲ نوع با عیار معلوم) |
| `Asset_Oversell` | On 1 Sep only 20.000 g were held at Home safe. | Am 1. Sep. lagen nur 20,000 g im Tresor zu Hause. | در ۱ سپتامبر فقط ۲۰٫۰۰۰ گرم در «گاوصندوق خانه» بود. |
| `Asset_DeletePurchase` | Delete the purchase? The 10 g, the payment of 500.00 EUR and the 20.00 EUR fee are removed together. | Kauf löschen? Die 10 g, die Zahlung von 500,00 EUR und die Gebühr von 20,00 EUR werden gemeinsam entfernt. | خرید حذف شود؟ ۱۰ گرم، پرداخت ۵۰۰٫۰۰ EUR و کارمزد ۲۰٫۰۰ EUR با هم حذف می‌شوند. |
| `Asset_OpeningNoCash` | No money is taken from an account. | Es wird kein Geld von einem Konto abgebucht. | پولی از هیچ حسابی کم نمی‌شود. |
| `Home_BalanceBelowZero` | Your balance may fall below zero around 29 Oct. | Ihr Kontostand könnte um den 29. Okt. unter null fallen. | ممکن است موجودی شما حدود ۲۹ اکتبر منفی شود. |
| `Home_HeadroomProtected` | Your plans would use protected money around 24 Oct. | Ihre Pläne würden um den 24. Okt. geschütztes Geld beanspruchen. | برنامه‌ها حدود ۲۴ اکتبر به پول محافظت‌شده می‌رسند. |
| `Headroom_Label` | Estimate with your current plans – not a guarantee. | Schätzung mit Ihren aktuellen Plänen – keine Garantie. | برآورد با برنامه‌های فعلی شما؛ تضمین نیست. |
| `Export_NotBackup` | A CSV export is not a backup. Use Backup to keep everything. | Ein CSV-Export ist keine Sicherung. Nutzen Sie die Sicherung, um alles zu behalten. | خروجی CSV نسخهٔ پشتیبان نیست. برای نگه‌داشتن همه‌چیز از پشتیبان‌گیری استفاده کنید. |
| `Aggregated_Overlap` | These 6 entries fall inside "Groceries – September (aggregated)". | Diese 6 Buchungen liegen in „Lebensmittel – September (zusammengefasst)“. | این ۶ ثبت در بازهٔ «خوراک – سپتامبر (تجمیعی)» قرار می‌گیرند. |

Checked: the longest German labels ("Standardwährung für neue Einträge", "zu Kursen vom 12. Aug. – möglicherweise veraltet") fit two
lines at 360 px; Persian amounts keep `۱٬۰۰۰٫۰۰` with isolates; minus signs are U+2212; codes stay Latin.

## 6. Accessibility, input and platforms

* Touch targets ≥ 44 px; every tap target is a real button (existing rule D-42); focus order follows reading order
  (RTL mirrored); notices are announced; charts keep a table alternative; colour is never the only signal (icons,
  labels, "−", "≈", "?").
* Keyboard (Windows): quick add buttons and account chip reachable by Tab; Enter saves when valid.
* **Android phone** (primary): bottom sheet pickers; numeric keypad for amounts and quantities (decimal separator of
  the language); no new permission.
* **RTL:** amounts and codes isolated; progress bars and paths mirrored as today (`ReadingScaleX`); Persian calendar
  dates in Persian when chosen.
* **Dark theme:** semantic tokens only; teal goal bars, violet forecast paths, amber incomplete labels as defined.
* **Wide windows / tablets:** centred 720 px column (D-41); reports may show two cards per row only above 720 px
  content width – proposed, not required.
* **Windows:** title bar, Insights tabs, focus ring as implemented; holdings and reports work identically.
* **iOS:** same behaviour expected (sheets, numeric keypad, Vision OCR); *not verified* – no Mac or device.
