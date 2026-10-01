# 03 – UX design

## 1. Principles

Simple by default, power on demand (PR-04, UX-01); numbers are always explainable (REP-01); never shame the user
(UX-07); every screen works in English, German and Persian (RTL) with runtime switching (LOC-01..03).

## 2. Navigation (D-11, D-27)

```
TabBar:   Home | Transactions | Plans | Insights | More
          └── "+" add button on Home, Transactions and Plans (bottom end corner, mirrored in RTL)
Insights: top tabs Budget · Reports · Forecast · Goals
More:     My money (Accounts, Owed to me) · Organize (Categories, Quick templates, Categorization rules)
          · Currency (Exchange rates, Display units) · Data and security (Backup, Profiles, Import/Export, Settings)
```

Home (D-37) starts with the recorded balance and a quick add card – Expense, Income and Transfer, each opening the
editor with that kind, plus up to six quick templates – so the first screen answers "where do I stand?" and "add an
expense" without scrolling. Then what needs attention, income and expenses of the period (with the period chips this
month / last month), the forecast (Advanced), the budget with what is left per day, and the next due items. The
category chart and the account list are Home sections that start hidden (§21.5 layout), because Insights and Accounts
show them in full.

## 3. Screen inventory

| ID | Screen | Content | Simple / Advanced |
|---|---|---|---|
| UI-01 | Onboarding (3 steps) | 1 language · 2 report currency + calendar suggestion · 3 first account (name, type, opening balance, date) | same |
| UI-02 | Home | Recorded balance card (with "includes N unreviewed"); quick add (Expense · Income · Transfer, quick templates); needs attention; income/expense of the period with period chips and scope (period, accounts, currency); budget remaining and per day; next due items; expense donut below (hidden by default) | Advanced adds the forecast card (end balance, lowest point) |
| UI-03 | Entry editor | Kind segmented control (Expense · Income · Transfer); amount with currency; category grid; title; date (today); account (hidden if only one); "More details": note, payee, icon, foreign amount | Advanced shows account, date, payee, currency directly |
| UI-04 | Transactions | Search, filter chips (period, kind, account, category, review state), list grouped by date with day totals; entry detail with refund, duplicate, make recurring, delete (undo) | same |
| UI-05 | Accounts | Balance per account in its currency, type icon, archived section, total per currency | same |
| UI-06 | Account detail / reconcile | Movement list (in, out, transfer, adjustment), reconcile: enter observed balance → difference → review suggestions → optional adjustment with reason | Advanced |
| UI-07 | Plans | Segments: Due & overdue · Upcoming · Needs review · All plans; each row shows status badge + amount (fixed/≈/?) | same |
| UI-08 | Plan editor | Kind, name, amount mode, account(s), category, start date, repeat (common presets / custom N days-weeks-months-years), calendar, month-end rule, end, reminder, auto-post (with "records only, pays nothing" notice), **preview of next 6 dates** | presets in Simple |
| UI-09 | Occurrence review | Confirm with actual amount/date, link to existing entry (suggestions), move due date, skip, note | same |
| UI-10 | Budget | Month selector, total card (limit, spent net, remaining, %), category limits, copy to next month | category limits Advanced |
| UI-11 | Reports | Expense by category (gross donut + refunds card + net table), income vs expense, monthly trend (6/12), account movement, budget, plan vs actual; tap → drill-down list | same data, Advanced filters |
| UI-12 | Import / Export | Export CSV (period, accounts, include notes?), sensitive-data warning, share; Import: pick file → mapping → preview (valid/invalid/duplicates) → apply → result with "undo this import" | same |
| UI-13 | Backup & restore | Last successful backup, create encrypted backup file (password + confirmation, "cannot be recovered" warning), restore: pick file → password → preview (date, counts) → safety copy → confirm | same |
| UI-14 | Settings & privacy | Language, calendar, report currency, week start, mode Simple/Advanced, reminder defaults, notification privacy, app lock, privacy information, about | same |

## 4. States every screen defines (UX-05)

| State | Pattern |
|---|---|
| Empty | Illustration-free icon + one sentence + primary action (e.g. "No transactions yet · Add expense") |
| Loading | Syncfusion busy indicator only if > 300 ms |
| Error | Inline message with retry; user input is kept (TX-06, AT-04) |
| Offline | Nothing changes (all local); cloud backup, when offered (D-35), shows "could not be reached" and keeps the local ledger usable |
| Permission denied | Notification: banner in Plans "Reminders are off – enable", ledger unaffected (REM-02, AT-34) |
| Invalid input | Field-level message, save disabled until valid; negative amount → explains kind selection (FIN-01) |
| Leaving unsaved | Confirmation "Discard changes?" |
| Incomplete data | Amber "incomplete" label with reason (missing rate, unknown amount, entries before opening date) |

## 5. Key flows

* **Quick expense (UX-04, target ≈ 10 s):** Home "Expense" (or +) → amount (numeric keypad focused) → category tap →
  Save. Date today, default account. A quick template on Home fills account, category, title and, if kept, the amount:
  template → Save. The editor title names the kind ("New expense"), so the choice made on Home is visible.
* **Salary and bill (§15.2):** Plans → + → Income "Salary", fixed, monthly → reminder 3 days before 09:00 → save;
  bill with estimated amount → at due date: confirm actual amount → choose whether future estimates change.
* **Refund:** entry detail → "Refund" → amount (≤ refundable), receiving account, date → linked refund.
* **Month end (§15.4):** Home "needs review" → Plans review → Budget → copy to next month → Backup reminder.

## 6. Visual system (VIS-01..04, UX-06, UX-08, D-27)

The ground is neutral; colour appears only where it carries a meaning, and each colour always means the same thing.
Approved design canvas: "Zanance Design" (foundations, eight screens, dark and English variants). Tokens live in
`Presentation/Palette.cs` (light / dark) and are used as `DynamicResource`.

| Meaning | Token (light / dark text) | Used for |
|---|---|---|
| Action | `Primary` `#1D56C9` / `#79A6FF`, soft `PrimarySoft` | Primary button, selected chip or tab, links. Never an amount |
| Money in | `IncomeText` `#16794A` / `#5FCB95` | Income, positive results, "paid" |
| Problem, debt | `ExpenseText` `#C0392B` / `#FF8E82` (the name is historic) | Negative results and balances, loans, overdue items, exceeded limits, destructive actions |
| Near a limit | `WarningText` `#945700`, bar `NearLimit` `#E3A21A` | Budget ≥ 80 %, estimates, incomplete data |
| The future | `PlanText` `#6A4FC4` / `#B6A5FF` | Plans, due dates (date tiles), contracts, the forecast |
| Savings | `SavingText` `#0E7880` / `#5ED0D7` | Savings accounts, goals, earmarked money |
| Transfer | `TransferText` `#4A5878` / `#AAB7CF` | Transfers between own accounts, ended plans |
| Back to me | `RefundText` `#1B72A2` / `#70C2EE` | Refunds, money owed to the user, unreviewed entries |
| Expenses | `AmountText` (neutral ink) | An expense is shown with "−" and its category icon, not in red, so real warnings stand out |

Each meaning colour has a text, a background and a line variant (`…Text`, `…Background`, `…Line`): icon tiles and
badges use all three. Budget bars follow the same meanings (D-37): green within the limit, amber near it, red over it;
goal bars are teal; the forecast path is violet. Blue never shows the state of money – on Home it marks the everyday
action (Expense in quick add) next to income in green and transfer in slate. Category colours only colour category icons; in the dark theme dark category colours are
lightened (`CategoryLookup.DisplayColor`).

| Token | Value |
|---|---|
| Surfaces | Page `#E8ECF2` / `#0D1219`, card `#FFFFFF` / `#151B24`, card outline and line `#D5DCE6` / `#2A3442`, strong outline `#B9C3D1` / `#3A4556`. The light page is a clear step darker than the cards, so cards stay visible on any screen brightness (owner feedback 2026-10-01) |
| Text | Ink `#0F1B2D`, secondary `#4A5568`, muted `#697586` (dark: `#E9EEF5`, `#AEB8C6`, `#8C97A8`) |
| Shape | Cards radius 18, buttons and inputs 14, icon tiles 40 × 40 radius 12, chips fully rounded |
| Spacing | 4-pt grid; screen padding 16; card padding 16; list row min height 60 |
| Touch targets | ≥ 44 × 44 (buttons 48) |
| Typography | Persian: Vazirmatn (OFL). English and German: Figtree for text, Urbanist Bold (the wordmark's face) for page titles and large amounts (both OFL). Page title 24, amount large 32, row title 15, body 14, secondary 13 |

* Persian digits (۱۲۳) are shown in the Persian interface by default (Settings → "Persian digits"); the separators
  become `٬` and `٫`. Only the displayed text changes (`NativeDigits`, applied to labels); stored values, CSV, PDF,
  backups and input stay Latin, and input accepts Latin, Persian and Arabic digits.* Amounts use tabular figures and are isolated with Unicode directional marks so `−12.50 EUR` never breaks in RTL
  (VIS-02, LOC-03). Currency codes stay Latin.
* Colour is never the only carrier: sign, icon and text label accompany it.
* Direction-dependent icons (back, chevrons) mirror in RTL; charts, logos and money icons do not.
* Every icon has an accessible name (`SemanticProperties.Description`); charts have a table alternative (UX-06).
* Light and dark theme (D-22): the semantic color tokens have a light and a dark variant (`Presentation/Palette.cs`) and are used as `DynamicResource`, so switching recolors open pages; code reads colors from the palette, never as literals. Settings offers "like the device", light and dark.

* **Help** (D-36): a setting whose effect is not obvious has a round "?" (`HelpButton`) next to its label or switch.
  It opens the full explanation and, where it helps, an example; short hints under the control stay for the common case.
  Covered: account type, opening balance, include in totals, report currency, financial month, Simple/Advanced, app
  lock, notification details, plan amount, weekend rule, automatic recording, "apply from", reimbursable and foreign
  amounts, spending type, budget method, rollover, budget period and two-week start, backup password, goal priority.
* **Back on Windows** (D-36): a page opened from another one shows a round back button before its title
  (`PageHeader`); the window's own arrow is small and easy to miss. Android keeps its toolbar arrow.
* **Onboarding** (D-36): a centred column of at most 560 px with a progress bar, the symbol in a halo on the welcome
  step, an icon above every step title, fields in cards and full-width buttons; "Add your first account".
* Rows that open a page are real buttons (keyboard, screen readers, touch feedback); icons are not in the
  accessibility tree, their meaning is in the text next to them.
* Short single-choice lists with long labels (account type) use wrapping chips (`ChoiceChips`) instead of a
  segmented control, so German and Persian labels never scroll or get cut off.
* Categories in the entry editor are wrapping pills (icon + full name) rather than a fixed grid, so no name is
  truncated in any language. Filter bars use compact single-line chips that scroll horizontally.
* Deleting an entry needs no confirmation: the list offers "Undo" for 8 seconds (TX-05).
* Modal pages get the text direction explicitly before they are shown (they are not part of the window's tree).
* Amounts are isolated with LRI…PDI plus inner LRM marks, because some renderers (Windows) ignore isolates.
* Changing language or calendar rebuilds the main shell and returns to the current page, so every cached number,
  date and icon is re-rendered in the new direction.

## 7. Syncfusion controls used (D-13)

| Control | Where |
|---|---|
| `SfCircularChart` / `SfCartesianChart` | Home donut, reports, forecast path |
| `SfSegmentedControl` | Entry kind, Simple/Advanced, report period |
| `SfNumericEntry` or custom keypad | Amount entry (custom parsing for Persian digits) |
| `SfCalendar` (dialog mode) | Date input via the shared `DateField`; Persian calendar confirmed (`CalendarIdentifier.Persian`) |
| `SfChip` / `SfChipGroup` | Filters |
| `SfBusyIndicator`, `SfPopup` | Long operations, confirmations |
