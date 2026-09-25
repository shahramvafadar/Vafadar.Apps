# 03 – UX design

## 1. Principles

Simple by default, power on demand (PR-04, UX-01); numbers are always explainable (REP-01); never shame the user
(UX-07); every screen works in English, German and Persian (RTL) with runtime switching (LOC-01..03).

## 2. Navigation (D-11)

```
TabBar:  Home | Transactions | Plans | More
         └── "+" add button on Home, Transactions and Plans (bottom end corner, mirrored in RTL)
More:    Accounts · Budget · Reports · Categories · Import/Export · Backup & restore · Settings · About
```

## 3. Screen inventory

| ID | Screen | Content | Simple / Advanced |
|---|---|---|---|
| UI-01 | Onboarding (3 steps) | 1 language · 2 report currency + calendar suggestion · 3 first account (name, type, opening balance, date) | same |
| UI-02 | Home | Scope header (period, accounts, currency); recorded balance card (with "includes N unreviewed"); income/expense of period; budget remaining; next due items; "needs review" chip; expense donut below | Advanced adds forecast card and filters |
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
| Offline | Nothing changes in phase 1 (all local); cloud features hidden (D-17) |
| Permission denied | Notification: banner in Plans "Reminders are off – enable", ledger unaffected (REM-02, AT-34) |
| Invalid input | Field-level message, save disabled until valid; negative amount → explains kind selection (FIN-01) |
| Leaving unsaved | Confirmation "Discard changes?" |
| Incomplete data | Amber "incomplete" label with reason (missing rate, unknown amount, entries before opening date) |

## 5. Key flows

* **Quick expense (UX-04, target ≈ 10 s):** + → amount (numeric keypad focused) → category tap → Save. Date today,
  default account.
* **Salary and bill (§15.2):** Plans → + → Income "Salary", fixed, monthly → reminder 3 days before 09:00 → save;
  bill with estimated amount → at due date: confirm actual amount → choose whether future estimates change.
* **Refund:** entry detail → "Refund" → amount (≤ refundable), receiving account, date → linked refund.
* **Month end (§15.4):** Home "needs review" → Plans review → Budget → copy to next month → Backup reminder.

## 6. Visual system (VIS-01..04, UX-06, UX-08)

| Token | Value | Use |
|---|---|---|
| Primary | `#2E7D32` | Actions, selected states |
| Income | `#1B5E20` text on `#E8F5E9` | Always with "+" and an arrow-in icon |
| Expense | `#B71C1C` text on `#FFEBEE` | Always with "−" and an arrow-out icon |
| Transfer | `#37474F` on `#ECEFF1` | Neutral, swap icon |
| Warning / incomplete | `#8D5B00` on `#FFF4E0` | Unreviewed, incomplete |
| Surface / background | `#FFFFFF` / `#F6F7F6` | Cards on light grey |
| Spacing | 4-pt grid; screen padding 16; card padding 16; list row min height 56 | |
| Touch targets | ≥ 48 × 48 dp | |
| Typography | Body 16, secondary 14, amount large 28 semibold; Persian font Vazirmatn (OFL) when added | |

* Amounts use tabular figures and are isolated with Unicode directional marks so `−12.50 EUR` never breaks in RTL
  (VIS-02, LOC-03). Currency codes stay Latin.
* Colour is never the only carrier: sign, icon and text label accompany it.
* Direction-dependent icons (back, chevrons) mirror in RTL; charts, logos and money icons do not.
* Every icon has an accessible name (`SemanticProperties.Description`); charts have a table alternative (UX-06).
* Light theme only in phase 1 (D-12).
* Digits are Latin in every language for now; Persian uses the Latin separators `.` and `,` because the Arabic
  separators (U+066B, U+066C) look like commas next to Latin digits. Input accepts Persian/Arabic digits and `٫`.
* Short single-choice lists with long labels (account type) use wrapping chips (`ChoiceChips`) instead of a
  segmented control, so German and Persian labels never scroll or get cut off.
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
