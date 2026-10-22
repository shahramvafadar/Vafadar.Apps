# 03 – UX design

## 1. Principles

Simple by default, power on demand (PR-04, UX-01); numbers are always explainable (REP-01); never shame the user
(UX-07); every screen works in English, German and Persian (RTL) with runtime switching (LOC-01..03).

### Privacy settings (D-63)

Device authentication and the independent four-digit app PIN have separate controls. PIN status is explicit; a full
form sets/changes/removes it with current/new/confirmation fields, all validation errors next to their fields, a
visible Back action, and masked numeric inputs accepting native digits. PIN takes precedence if both are enabled.
The unlock cover exposes no financial data; a forgotten-PIN button requires successful device authentication and
explicit confirmation before removal. No-device-auth and cancellation leave access locked. The first startup frame
is blank until protected state is read. A separate Android screenshot switch is on by default, applies immediately,
and explains platform limits and persistent recents protection. Both topics have full help and examples in six
languages. About states proprietary ownership separately from its compact third-party notices button.

### First run (D-62)

Three steps remain: language; theme, Simple/Advanced, reporting currency and calendar; first account. Simple is the
default suggestion; the theme is applied immediately. The first and last steps offer "I already have a backup"
instead of account creation. Restore opens a page with a visible Back button and only restoration actions; a return
preserves the draft. A restored profile with accounts opens directly, retaining its currency, default account and
experience; the device theme/language remain local. An empty backup returns to onboarding. All controls are real
buttons; narrow screens scroll while Back/Next remain visible. The progress and action areas have an opaque theme
background above the scrolling form, keeping Windows content from drawing behind them.

## 2. Navigation (D-11, D-27)

```
TabBar:   Home | Transactions | Plans | Insights | More
          └── "+" add button on Home, Transactions and Plans (bottom end corner, mirrored in RTL)
Insights: top tabs Budget · Reports · Forecast · Goals
More:     My money (Accounts, Owed to me) · Organize (Categories, Quick templates, Categorization rules)
          · Currency (Exchange rates, Display units) · Data and security (Backup, Import/Export, Profiles)
          · App (Settings, About Zanance); every row has a one-line description (D-39)
```

Home (D-37) starts with the recorded balance and a quick add card – Expense, Income and Transfer, each opening the
editor with that kind, Receipt (a photo or PDF read on the device into a new expense, attached when it is saved), plus
up to six quick templates – so the first screen answers "where do I stand?" and "add an expense" without scrolling.
Then what needs attention, income and expenses of the period (with the period chips this month / last month), the
forecast (Advanced), the budget with what is left per day, the next due items and the three latest entries. The
category chart and the account list are Home sections that start hidden (§21.5 layout), because Insights and Accounts
show them in full.

## 3. Screen inventory

| ID | Screen | Content | Simple / Advanced |
|---|---|---|---|
| UI-01 | Onboarding (3 steps) | 1 language · 2 report currency + calendar suggestion · 3 first account (name, type, opening balance, date) | same |
| UI-02 | Home | Recorded balance card (with "includes N unreviewed"); quick add (Expense · Income · Transfer · Receipt, quick templates); needs attention; income/expense of the period with period chips and scope (period, accounts, currency); budget remaining and per day; next due items; recent entries; expense donut below (hidden by default) | Advanced adds the forecast card (end balance, lowest point) |
| UI-03 | Entry editor | Kind segmented control (Expense · Income · Transfer); amount with currency; category grid; title; date (today); account (hidden if only one); "More details": note, payee, icon, foreign amount | Advanced shows account, date, payee, currency directly |
| UI-04 | Transactions | Search, filter chips (period, kind, account, category, review state), list grouped by date with day totals; entry detail with refund, duplicate, make recurring, delete (undo) | same |
| UI-05 | Accounts | Balance per account in its currency, type icon, archived section, total per currency | same |
| UI-06 | Account detail / reconcile | Movement list (in, out, transfer, adjustment), reconcile: enter observed balance → difference → review suggestions → optional adjustment with reason | same (correcting a balance is never hidden, ZEX-SA25) |
| UI-07 | Plans | Segments: Due & overdue · Upcoming · Needs review · All plans; each row shows status badge + amount (fixed/≈/?) | same |
| UI-08 | Plan editor | Kind, name, amount mode, account(s), category, start date, repeat (common presets / custom N days-weeks-months-years), calendar, month-end rule, end, reminder, auto-post (with "records only, pays nothing" notice), **preview of next 6 dates** | presets in Simple |
| UI-09 | Occurrence review | Confirm with actual amount/date, link to existing entry (suggestions), move due date, skip, note | same |
| UI-10 | Budget | Month selector, total card (limit, spent net, remaining, %), category limits, copy to next month | limits, method and periods edited in Advanced; existing ones shown in Simple (ZEX-S0502) |
| UI-11 | Reports | Expense by category (gross donut + refunds card + net table), income vs expense, monthly trend (6/12), account movement, budget, plan vs actual; tap → drill-down list | same data, Advanced filters |
| UI-12 | Import / Export | Export CSV (period, accounts, include notes?), sensitive-data warning, share; Import: pick file → mapping → preview (valid/invalid/duplicates) → apply → result with "undo this import" | same |
| UI-13 | Backup & restore | Last successful backup, create local or cloud backup with optional password protection (on by default, choice remembered on device; password + confirmation when on, unreadable-without-password / readable-file warnings), restore: pick file → password if encrypted → preview (date, counts) → safety copy → confirm | same |
| UI-14 | Settings & privacy | Language and region (language, calendar, independent regional format, digits, holiday region, week start) · Money and months (report currency, month start) · Appearance (theme) · Experience (Simple/Advanced) · Privacy and security (app lock) · Notifications (turn on, names and amounts, reminder defaults) · Delete data | same |
| UI-15 | About Zanance (D-39) | Symbol, name, tagline, version; what happens with the data; what a backup, a CSV export and a PDF report contain; the platforms each release is checked on; a problem report the user sends themselves (nothing is sent automatically, ZEX-S0905); a compact button to the required full third-party notices on a page of their own; no inline component list (D-62) (`Resources/Raw/ThirdPartyNotices.txt`) | same |

What each mode shows is decided by one table, `FeaturePolicy` (`Vafadar.Zanance.Core/Settings/FeaturePolicy.cs`);
pages ask it instead of reading the mode. Simple keeps every existing item visible, at least as a summary
([enhancement 05](enhancements/2026-10-multi-unit-goals-insights/05-simple-advanced-matrix.md)).

## 4. States every screen defines (UX-05)

| State | Pattern |
|---|---|
| Empty | Illustration-free icon + one sentence + primary action (e.g. "No transactions yet · Add expense") |
| Loading | Syncfusion busy indicator only if > 300 ms |
| Error | Inline message with retry; user input is kept (TX-06, AT-04) |
| Offline | Nothing changes (all local); cloud backup, when offered (D-35), shows "could not be reached" and keeps the local ledger usable |
| Permission denied | Notification: banner in Plans "Reminders are off – enable", ledger unaffected (REM-02, AT-34). Once the system no longer shows its dialog, "Turn on" explains why and opens the app's notification settings (D-38) |
| Invalid input | Field-level message, save disabled until valid; negative amount → explains kind selection (FIN-01) |
| Leaving unsaved | Confirmation "Discard changes?" |
| Incomplete data | Amber "incomplete" label with reason (missing rate, unknown amount, entries before opening date) |

## 5. Key flows

* **Quick expense (UX-04, target ≈ 10 s):** Home "Expense" (or +) → amount (numeric keypad focused) → category tap →
  Save. Date today, default account. A quick template on Home fills account, category, title and, if kept, the amount:
  template → Save. The editor title names the kind ("New expense"), so the choice made on Home is visible.
* **Expense from a receipt (D-31, D-33, D-37, D-38):** Home "Receipt" → "Take a photo" (phones) or "Choose a photo or
  PDF" (system picker, no storage permission) → read on the device → the editor opens with amount, date and shop to check → category → Save; the file
  becomes the entry's attachment only then. If nothing can be read, the editor still opens with the file.
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
| Surfaces | Page `#E8ECF2` / `#111827`, card `#FFFFFF` / `#1C2536`, muted surface `#F1F4F8` / `#232D40`, card outline and line `#D5DCE6` / `#2B364B`, strong outline `#B9C3D1` / `#3D4A62`. The light page is a clear step darker than the cards, so cards stay visible on any screen brightness (owner feedback 2026-10-01). The dark ground is a deep navy built on `#111827` (D-38): blue clearly leads, so it never reads green-grey, and it matches the blue of the symbol; the Android splash follows it in dark mode |
| Text | Ink `#0F1B2D`, secondary `#4A5568`, muted `#697586` (dark: `#E9EEF5`, `#AEB8C6`, `#8C97A8`) |
| Shape | Cards radius 18, buttons and inputs 14, icon tiles 40 × 40 radius 12, chips fully rounded |
| Spacing | 4-pt grid; screen padding 16; card padding 16; list row min height 60 |
| Touch targets | ≥ 44 × 44 (buttons 48) |
| Typography | Persian: Vazirmatn (OFL). English and German: Figtree for text, Urbanist Bold (the wordmark's face) for page titles and large amounts (both OFL). Page title 24, amount large 32, row title 15, body 14, secondary 13 |

* Persian digits (۱۲۳) are shown in the Persian interface by default (Settings → digit shapes; independent options since D-67); the legacy automatic format uses `٬` and `٫`, while explicit regional formats/digit-shape choices preserve their selected separators. Only the displayed text changes (`NativeDigits`, applied to labels); stored values, CSV, PDF,
  backups and input stay Latin, and input accepts Latin, Persian and Arabic digits.
* Amounts use tabular figures and are isolated with Unicode directional marks so `−12.50 EUR` never breaks in RTL
  (VIS-02, LOC-03). Currency codes stay Latin.
* Colour is never the only carrier: sign, icon and text label accompany it.
* Direction-dependent icons (back, chevrons) mirror in RTL; charts, logos and money icons do not.
* Every icon has an accessible name (`SemanticProperties.Description`); charts have a table alternative (UX-06).
* Light and dark theme (D-22): the semantic color tokens have a light and a dark variant (`Presentation/Palette.cs`) and are used as `DynamicResource`, so switching recolors open pages; code reads colors from the palette, never as literals. Settings offers "like the device", light and dark.
* A theme change never closes what is open: with "like the device" the phone may turn dark on its own while an expense is being typed. On a tab's first page the shell is rebuilt (nothing can be lost there); with a detail page or an editor open, the open pages recompute their code colors in place (`IThemeAware`) and the rebuild waits until the user is back on a tab. Disabled states use fixed colors that read in both themes: a visual-state setter with a `DynamicResource` takes over the property's resource link, so a button that was disabled once kept the old theme's color.

* **Help** (D-36): a setting whose effect is not obvious has a round "?" (`HelpButton`) next to its label or switch.
  It opens the full explanation and, where it helps, an example; short hints under the control stay for the common case.
  Covered: account type, opening balance, include in totals, report currency, financial month, Simple/Advanced, app
  lock, notification details, plan amount, weekend rule, automatic recording, "apply from", reimbursable and foreign
  amounts, spending type, budget method, rollover, budget period and two-week start, backup password, goal priority.
* **Back on Windows** (D-36): a page opened from another one shows a round back button before its title
  (`PageHeader`); the window's own arrow is small and easy to miss. Android keeps its toolbar arrow.
* **Permissions** (D-38): asked in context and in the platform's own dialogs. Notifications: once per device after the
  first start an explanation ("Reminders for due payments" – what is reminded, that nothing leaves the device, that the
  lock screen shows no amounts by default) leads to the system dialog (Android 13+, iOS); "Not now" is respected, and
  Plans and Settings offer "Turn on" later, which opens the system settings once the system no longer asks. Camera: only
  when the user chooses "Take a photo"; Android uses the camera app and needs no permission, iOS shows its dialog with
  our usage text and, after a refusal, offers the settings.
* **Keyboard focus on Windows** (D-37, D-41, D-42): the focus ring is drawn in the action blue of the current theme,
  set on each element as it receives focus (`MauiProgram`; WinUI 3 does not take it from app resources), so the tabs
  of the shell get it too; it follows the rounded corners of each control.
* **Insights on Windows** (D-43): the four Insights pages show Budget, Reports, Forecast and Goals as tabs in their
  header, because the Windows shell only offers them in a drop-down of the Insights tab. Phones keep their top tabs.
* **Wide windows** (D-41): wider than 720 px every page is a centred column of 720 px, so cards and lists read like
  on a large phone instead of stretching across a desktop window or a tablet in landscape.
* **Getting started** (D-41): a new user sees three steps on Home (first expense, a regular payment, a monthly budget),
  ticked off as they are done; the card disappears when all are done or on *Hide*.
* **Onboarding** (D-36): a centred column of at most 560 px with a progress bar, the symbol in a halo on the welcome
  step, an icon above every step title, fields in cards and full-width buttons; "Add your first account".
* Everything that reacts to a tap is a real button (keyboard, screen readers, touch feedback), never a tap gesture
  alone (D-42): list rows and cards that open a page carry a transparent `OverlayButton`; chips, swatches and the
  previous/next period arrows (round `MoveButton`) are buttons too. Icons are not in the accessibility tree, their
  meaning is in the text next to them.
* Form errors appear next to their field, all at once, so one pass fixes the form (entry and plan editors).
* Touch targets are at least 44 px where possible; the 30 px "?" circle sits in a 44 px button (D-48).
* Short single-choice lists with long labels (account type) use wrapping chips (`ChoiceChips`) instead of a
  segmented control, so German and Persian labels never scroll or get cut off.
* Categories in the entry editor are wrapping pills (icon + full name) rather than a fixed grid, so no name is
  truncated in any language. Filter bars use compact single-line chips that scroll horizontally.
* Deleting an entry needs no confirmation: the list offers "Undo" for 8 seconds (TX-05).
* Modal pages get the text direction explicitly before they are shown (they are not part of the window's tree).
* Amounts are isolated with LRI…PDI plus inner LRM marks, because some renderers (Windows) ignore isolates.
* English copy (D-52): plain US English; a financial record is a "transaction" (code and resource keys keep
  `Entry`); a text says what Zanance records, never more ("No planned items are due or overdue"); destructive actions
  and restore name their scope ("Delete this profile's data"); a label quoted in another text matches the label exactly;
  counts that can be 1 use a singular key (`Occurrence_OverdueOneDay`, `Plan_ReminderOneDay`, `Report_OneDay`; 0 days
  before is `Plan_ReminderOnDueDay`) or a count-neutral form ("Category limits kept: {0}"). A yearly plan's day and
  month are written in the plan's own calendar (`IDateFormatter.Format(date, style, calendar)`), "on the last day of
  February" names the month. Restore errors depend on the stage: before the data is replaced the text may say that
  nothing changed (`Backup_Error_SafetyCopyFailed`); from then on it may not (`Backup_Error_RestoreFailed`).
* Persian copy (D-53): formal "شما" with plural verbs; Persian ی and ک and correct half-spaces; «تراکنش» for a
  transaction ("ثبت" is the verb), «برگشت وجه» for a refund, «پس‌دادن درآمد» for an income repayment, «نسخهٔ
  احتیاطی» for the safety copy, «برنامهٔ مالی» where "برنامه" could mean the app, «ارزش خالص دارایی» for net worth,
  «دارایی‌های مقداری» for holdings; the product name stays «Zanance» (quoted in a Persian sentence, never
  transliterated); a missing record is not a fact about the world («طلبی ثبت نشده است», not «کسی به شما بدهکار
  نیست»); a label quoted in another text matches the label.
* German copy (D-54): formal "Sie"; Buchung for a transaction, Umbuchung for a transfer between own accounts,
  Erstattung / Kostenerstattung, "Rückzahlung auf ein Kreditkartenkonto" ("Kartenzahlung" reads as a card purchase),
  Sicherung / Sicherheitskopie, Prognose, Nettovermögen, Bewertungswährung, Bestände; Median is not Durchschnitt;
  counts as "Buchungen: {0}" so that no "1 Buchungen" appears; quoted option names use „…“ and match the label;
  German texts must fit the narrowest layout (360 px).
* Spanish copy (D-55): one general Spanish with the informal "tú"; Transacción (tab "Movimientos"), Transferencia
  ("Traspaso" in quick add), Reembolso, Devolución de ingresos, Presupuesto, Previsión, Copia de seguridad / copia
  preventiva, Patrimonio neto; "el primer lunes", not "primero". The translation comes finished from the owner and is
  not rewritten; a label that does not fit may only use the owner's approved short form after the problem was seen.
* French copy (D-56): one general French with the polite "vous"; Opération (transaction), Virement, Remboursement,
  Revenu restitué, Planification / Échéance, Prévision, Sauvegarde / copie de précaution, Patrimoine net; typographic
  spaces before ":", ";", "?", "!" and "%"; "le dernier jour du mois (août)" avoids "de août". The translation comes
  finished from the owner and is not rewritten; a label that does not fit may only use the owner's approved short
  form after the problem was seen.
* Italian copy (D-57): one general Italian with the informal "tu" and sentence case; Movimenti (transactions),
  Trasferimento, Rimborso, Entrata restituita, Piani / Scadenza (feminine: Scaduta, Completata, Saltata), Previsione,
  Margine disponibile, Backup / Copia di sicurezza, Patrimonio netto; no French spaces before ":", "?" and "!".
  Weekday phrases follow the weekday's gender ("la prima domenica") through `WeekdayGrammar`. As with French, the
  translation is not rewritten, and a short form is used only after a label was seen not to fit.
* Help dialogs (D-60): the text explains what the setting changes and what it does not, then one example with neutral,
  fictitious amounts; the "Example:" label is added by `HelpButton`, never written into `Help_*_Example`. Review all of
  them with `Run-Snapshots.ps1 -Help` after a change.
* Two buttons side by side (holding actions, change/skip an occurrence) use 8 px side padding instead of 16, so that
  longer translated labels fit at 360 px without a smaller font.
* A button whose label had to be short keeps that label as its accessible name (voice control users say what they
  see) and explains the action in `SemanticProperties.Hint` (`{Key}_A11yHint`, one or two sentences, about 100
  characters at most), e.g. that nothing is bought in the app (D-58).
* Translation follow-up of D-52 (closed): the English meaning changed for these keys; Persian follows since D-53 and
  German since D-54: `Holdings_Note`, `Settings_DeleteAll*`,
  `Lock_ConfirmDeleteAll`, `Backup_RestoreIntro`, `Backup_Replace*`, `Backup_RestoredMessage`,
  `Backup_Error_RestoreFailed`, `Onb_WelcomeText`, `Backup_Intro`, `About_DataText`, `About_FilesBackup`,
  `About_FilesCsv`, `About_ProblemText`, `About_PlatformsText`, `Settings_ShowDetailsHint`,
  `Help_NotificationDetails_Text`, `Help_BackupPassword_Text`, `Help_SafetyCopy_Text`, `Backup_PasswordWarning`,
  `Backup_NoPasswordWarning`, `Settings_RegionHint`, `Onb_ZeroHint`, `Onb_CurrencyTitle`/`Text`, `Rates_Intro`,
  `Kpi_K10_*`, `Kpi_K02_Excluded`, `Kpi_K03_Included` (third item), `Kpi_K05_*`, `Kpi_K07_Definition`,
  `Help_SpendingType_Example`, `Help_HoldingPrices_Text`, `Help_AutoPost_Text`, `Help_WeekendRule_Text`,
  `Weekend_Before`/`After`, `Budget_ScopeHint`, `Forecast_Disclaimer`, `Plan_PauseMessage`, `Plan_EndMessage`,
  `Snapshot_ActualPath`, the iOS camera prompt and `widget_description` (Android).
* Changing language or calendar rebuilds the main shell and returns to the current tab, so every cached number,
  date and icon is re-rendered in the new direction. These are deliberate choices in Settings; a theme change, which
  can come from the device, does not rebuild while a page is open (see the theme above).

## 7. Syncfusion controls used (D-13)

| Control | Where |
|---|---|
| `SfCircularChart` / `SfCartesianChart` | Home donut, reports, forecast path |
| `SfSegmentedControl` | Entry kind, Simple/Advanced, report period |
| `SfNumericEntry` or custom keypad | Amount entry (custom parsing for Persian digits) |
| Own `DateField` with `SfCalendar` in dialog mode (D-59) | Date input by three number boxes (day, month, year) in the display calendar, a calendar button that opens the month view (Persian via `CalendarIdentifier.Persian`, lunar Hijri via `CalendarIdentifier.UmAlQura` for 1900–2077, otherwise Gregorian) and the full date below; a box with a number that cannot be part of a date shows red, a day past the month's end becomes its last day when the field is left |
| `SfChip` / `SfChipGroup` | Filters |
| `SfBusyIndicator`, `SfPopup` | Long operations, confirmations |

## Receipt review (D-64)

Home scan still creates an Expense draft. Reading an attachment opens the existing entry draft. Both carry the same
Core evidence model: Found, Review or NotFound, independent of OCR character confidence. A clear total fills the
amount only with a selected, compatible account. Conflicting/damaged totals do not fill it automatically; show the
relevant source rows (at most 400 characters) and up to four complete choices as real 44 px buttons. Source text keeps
its receipt script and decimal punctuation; formatted choice labels follow native-digit preferences and isolate the
number/unit in RTL. A damaged fragment or signed total is not a selectable positive expense.

Date and merchant suggestions remain usable without a total. No new amount preserves an existing amount; no scan,
choice or reread saves anything. The user checks and saves explicitly. An explicit foreign currency, unclear symbol
or excessive precision does not silently become the account currency. Toman is explicitly ten rials, without foreign
exchange. An unlabelled total is labelled as the account ISO unit, independent of UI language and display preferences.
The existing currency-change notice remains; if a display unit changes while a receipt value is still applied, Save
stops and asks the user to choose or enter the amount again. This is receipt review, not an image editor.

## Plan and debt forms (D-65)

Place basic identity/account/amount and an optional category picker before the schedule. Keep first date, named
recurrence calendar, Repeat, plain-language summary, inline rule error and next dates together. Once is the blank
default; Monthly anchors to the selected start day. A count includes the first occurrence. Show ending choices in
both modes; put calendar overrides, short-month policy, weekday/second-day and business-day rules behind an explicit
button, automatically open for existing custom rules. Short months describe last-valid-day versus skipping.
Changing to Custom preserves the unit/interval. The DateField override applies only to this rule's inputs/preview.

Offer Add debt or money owed in Accounts. Use I owe / Owed to me, contextual counterparty labels and a positive
amount at a reference date; no manual minus checkbox. Explain that existing balance does not record cash movement.
Keep interest/installment estimates and other account options in optional sections; existing terms remain visible.
After creation show details with actual repayment and Set repayment reminder as separate actions. The reminder is an
unsaved transfer draft with unknown principal and automatic posting off. Interest remains a separate expense when
an actual repayment is recorded. Choice groups expose individual button names to UI Automation and screen readers.

## Independent regional display and cloud restore (D-67)

Onboarding and Settings share regional format, digit shapes, holiday calendar and first-day-of-week controls,
separate from language, display calendar and currency. Include a live numeric date/number example and a HelpButton
explaining English labels with German formats. A chosen format survives language changes. Month/day words stay in
the UI language. A holiday choice changes defaults for new plan drafts only; Germany is nationwide-only and Iran's
lunar dates are approximate. Preserve legacy regions even when no holiday data is available.

Each connected cloud destination owns its loading/empty/error/completed feedback and file list. Automatically
refresh on entry and after connection, creation or deletion. An empty list explains that connection does not create
a backup. Keep a visible refresh action. List backups across profile sets for restore while retaining set-specific
cleanup. Use stacked date/details rows at narrow widths. Device file selection is a fallback for saved/downloaded
.vbak files. Protection stays optional and its shared local/cloud choice is explained above the destinations.

Changing regional display keeps the open form and live preview; cached tab pages rebuild after returning. New
backup packages include only allowlisted portable display choices, never device credentials or security controls.

## Goal contribution reminders (D-70)

Place the opt-in directly after the first contribution date, in both experience modes. Keep the label, 44 px help
and native switch together at 360 px, with a full explanation of 09:00 device-local time, saved-calendar dates,
generic privacy, permission refusal, inexact system delivery and no automatic financial action. Defaults are off;
loading an existing choice never asks for permission. Include the toggle in unsaved-change detection. Reminder-only
edits preserve the saved rule; changing display preferences does not reinterpret its dates. Tapping a delivered
notification opens goal details after the app lock; a grouped reminder opens the goals list.

## Period review reminders (D-71)

Settings > Notifications offers "Remind me to review each period" in both experience modes, off by default, with
help and a pay-cycle example. The fixed 09:00 device-local time is stated separately from new-plan reminder defaults.
Follow the selected display calendar and financial month start day. Keep the existing unsupported/permission-denied
feedback visible. No account before a closed period means no reminder; finished periods and missed times are skipped.
Generic notifications reveal no period label unless details are allowed. A tap opens the currently due review via
app lock; finishing and step completion remain explicit actions. Changing the choice never records money.

## Aggregate import linking (D-72)

Every affected aggregate gets its own preview card with title/category, account, inclusive date range, original
amount, detail count/total and visible resulting remainder. Decisions start unset: Link and replace or Keep both.
Use short picker labels; explain double counting in the adjacent outcome rather than a clipped option. A zero
remainder explicitly states the aggregate will be removed. Financial relationships that need separate review only
offer Keep both with an explanation. The help topic includes the 412/395/17 example and durable Undo behavior.

Disable import until every card is decided; do not show a red error for an untouched choice. A detail assigned to
two selected aggregates shows a real conflict. The duplicate switch recomputes the accepted rows and resets choices.
Refresh preview is available for own and mapped CSV files. Stale data requires a fresh preview; no reduction happens
silently. Inline Undo conflicts belong next to import history and preserve later edits; successful Undo restores
previous aggregates and discards the now-stale preview. Nothing is saved while opening or previewing a file.

## Application flow safety (D-73 / QA-03)

Dialog cancellation preserves the current draft and selected rows. Bulk writes use independent snapshots; a rejected
save must not optimistically change the visible review, tags or category. Keep selection stable while a bulk dialog
is pending and refuse other bulk commands until it finishes. Undo accepts a tap only during its displayed eight-second
window; a delayed tap cannot restore deleted money. A backwards clock never extends Undo or grants lock grace.

Startup links wait until secure state is read. A missing/rebuilt window or failed cover removal keeps them gated.
Supported widget entry links open drafts only; reminder identity and original date remain typed navigation arguments.
Neither a repeated tap nor a malformed link posts money or completes a review. Native theme callbacks must not recurse
into palette rebuilding; changing theme keeps an open editor and its values. Existing translated labels and controls
are unchanged; explicit native ports allow the actual flow to be tested separately from rendered/platform behavior.

## Valued-asset consent (D-74 / QA-04)

A legacy valued asset represents an estimate, rather than cash. Keep the existing explicit Record anyway/Cancel
explanation for manual Income/Expense, in both Simple and Advanced. Save is busy while consent is pending; a second
save cannot open another dialog or overlap a write. Cancel keeps account, kind, amount and note in the editor and
leaves the asset value/ledger unchanged. Each retry asks again. Consent does not bypass validation. No new copy or
layout; transfers and balance adjustments do not acquire the income/expense warning.

## Large-ledger responsiveness (D-75)

Large transaction snapshot reads must not monopolize the UI thread. Capture the requesting profile before queuing
work; resume presentation changes on the UI thread. Totals and forecast opening balances use unchanged original
account slices and the shared ledger formula. Search counts alone are not proof that result rows are rendered or
accessible. Performance observations distinguish system first frame from loaded financial content and include
measurement overhead; native ANR/row-observation failures remain explicit acceptance findings.

Q-02 uses the named reference and tenfold shapes in quality/performance-q02.md. Preserve dates, transfer identities,
refund meanings, review scope, currency and checked sums through every optimization; never replace a financial
result with an estimate to meet a timing objective.

## Transaction loading and retry (D-76)

The first transaction frame shows loading, rather than an empty-ledger or no-match claim. Keep search, period/kind,
extended filters, Add and bulk actions disabled while reading or after failure. Publish the complete grouped snapshot
before enabling input and exposing its native rows. Loading text and the activity indicator use semantic palette
resources. On failure show an explicit translated Load transactions again action; repeat taps share one read.
Reload preserves existing query/filter choices; only completed snapshots can be searched. This interaction boundary
is separate from bulk-write dialogs and does not imply a measured startup or native-rendering performance claim.

## Large-text first slice (D-77)

Readable text retains native automatic font scaling. Home quick actions wrap as whole items with unshortened names;
plan title/subtitle and amount/status occupy separate rows, so neither side consumes the other. Date tiles and
quick-template pills use minimum sizes and padding, not fixed text heights; section links have a 44 px minimum.
Decorative symbol spans and the named floating plus keep explicit glyph sizes while surrounding text scales. This
must never become a blanket font-scaling opt-out for labels or inputs. Review actual target geometry and rendered
RTL/languages/themes, not only source styles. Full A11Y-03 and screen-reader/device acceptance remain open.

## Persistent actions and financial identity (D-78)

Reserve an Auto grid row below the star-sized scroll viewport for persistent Add/bulk actions. Put transient Undo
in a further row and its button below the message. Retain touch targets, accessible names and existing visibility
rules; scroll padding alone cannot prevent an overlay covering content before the user scrolls. Financial identity
and amount/status have separate growing rows, with an icon spanning both and the real overlay button spanning the
whole row. Titles/subtitles wrap. Group dates/names and totals also use separate rows. Check actual native geometry
and own-window rendering at 100%/200%, both themes, en/fa/de and 360/412/wide widths. Remaining fixed controls and
platform/screen-reader acceptance are tracked separately; viewport clipping at its scroll boundary is not overlap.

## Settings readiness and live choice captions (D-79)

Cover and disable the entire editable Settings scroll content during initial/reload reads. Loading and error text
are translated; retry is a real button with growing height and a 44 px minimum. Publish complete preferences,
suggestions and availability synchronously before uncovering the form. Never expose old fields after failed reload.
When display changes, refresh choice labels/previews in place while keeping selected values and unsaved estimate/
reminder inputs. Native ItemsSource replacement can clear selection: restore it under the synchronous save guard.
Keep language, culture, calendar, digits and theme independent. Shell navigation rebuild remains deferred per D-67.

## Growing action captions (D-80)

Use WrappingAction where a native button's long caption clips at large text. Its semantic surface grows around a
WordWrap Label; only the last transparent Button owns the command, CanExecute, focus and full spoken name. The face
is excluded from the accessibility tree. Follow dynamic palette colors and dim the face with native enablement.
Account/debt actions get full width. Settings suggestion/permission actions follow their message. Bulk actions use
two rows with selection count separate from Select all/Cancel. Preserve the existing compact 12-point bulk captions
with native scaling and useful side padding; do not shrink readable text or squeeze it into four narrow columns.
Review actual caption and target geometry, disabled/selected states and real native Invoke behavior, without Save/
delete/Undo or security changes. Persistent action rows stay outside the viewport per D-78.

## Growing dates and complete large amounts (D-81)

Reserve the full digit count and native entry chrome for each date part; let whole parts move to another row on
narrow large-text forms. Keep Latin editable digits, existing localized order/calendar preview, validation and
partial input. The calendar target stays 44 px. Do not replace real native inputs with a painted date.

Large monetary readouts keep the original signed/direction-marked amount and unit, existing scalable display font
and complete spoken packet. Oversized values use a real horizontal viewport with a visible translated scrolling
hint; never split a decimal number or silently remove its sign/unit. Give account balances and monthly movements
their own rows. Home's account navigation is on the visible heading, leaving the value viewport interactive.
Runtime review must reach both ends through the actual native Scroll pattern and inspect realized glyph bounds;
a detached text probe can resolve a bundled font differently. Financial calculations and persistence are unchanged.


## Reopening Settings after display changes (D-82)

Each language choice must update the actual form captions, including after returning and reopening Settings.
Keep the current form and unsaved estimate input during the change; replace cached tab content only after Back.
Retired navigation titles stop receiving translations before their root is replaced. Current titles remain live.
Opening a form publishes defaults without saving them; do not update stored settings or notification defaults.
The settings-reopened Debug route checks real native selection/back behavior without financial Save or PIN changes.

## Growing child-page headers (D-83)

On Windows, child pages put their scalable complete title and real 44 px Back button in an Auto row above the same
body. Long words can wrap; the body viewport starts below the entire header. Center header and body together in
wide windows. Attach once and preserve inherited form bindings; modal editors retain their own Cancel/header.
The Back tooltip and spoken name follow the current app language. Android retains its native arrow and back
handling while translating only the accessible description, including on retained toolbars after language changes.

## Settings estimate reload (D-84)

Returning to an open Settings form refreshes saved preferences and device/account availability without losing an
unsaved essential-spending estimate. Preserve exact text, selected period and its original currency until explicit
Save or leaving the form. Clean inputs refresh normally; drafts never cross profile/settings contexts. Hide any
suggestion computed for a different currency. Successful Save marks only submitted input as saved, so later typing
remains a draft. This does not persist unfinished input across app restart.

### Visible Insights destinations (D-85)

On Windows show Budget, Reports, Forecast and Goals above the actual root-page body in a growing row. Use two
columns on narrow windows and four at 600 px of navigation width; never hide a destination behind clipped text.
Wrap scaled captions, preserve the current underline, and bind spoken selected/name feedback live. Each whole
caption is a native button target of at least 44 px. Retain body/bindings when attaching or resizing the surface.

### Complete budget figures (D-86)

Show the complete wrapped budget/category name first, spending on its own full-width row and its localized limit
on the next. Envelope identity and amount also get their own rows. Keep existing 14 px body/bold and 13 px secondary
sizes scalable; AmountReadout.CaptionStyle selects the existing typography while null retains the large default.
Preserve the original sign, decimals, approximation, native digits and currency/unit as one packet. An oversized
packet scrolls horizontally with the existing translated hint and complete spoken name. Do not shrink digits or
truncate units to fit. See quality/budget-readouts.md for actual realized glyph and native scroll checks.

### Visible budget period decisions (D-87)

Month, Week and Every 2 weeks are three stable form choices. Show all of them using the existing growing wrapping
ChoiceChips mode, with full scaled captions, selected native spoken state and at least 44 px targets. Rows grow
when space is narrow; the group can share a row in a wide readable column. Keep the adjacent period help action.
Do not require horizontal discovery of the last choice. Period selection changes the existing view only, with no
financial Save. Compact horizontal filter strips elsewhere remain independent. Evidence: quality/budget-periods.md.

### Tag suggestions (D-89)

In transaction details, each suggested tag is a bounded growing row with a real native action. Keep the scalable
13 px font and blue action/page-surface colors; wrap the complete direction-safe tag caption and keep a useful
44 px minimum target. Preserve the raw normalized tag separately from the display marker and pass it unchanged
to AddTag. Choosing a suggestion edits only the open draft; explicit Save controls persistence. Limits stay the same.

### Transaction detail disclosure (D-90)

Name the action by the current panel: "Hide details" while expanded, established "More details" while collapsed.
Exactly one real growing native action is visible; full translated captions/spoken names and 44 px targets follow
native text scale inside the form width. Hiding preserves all unsaved fields. Simple/Advanced and receipt/edit
initial visibility follow the existing policy; no automatic Save or data clearing.

### Onboarding restore alternatives (D-91)

The welcome and first-account restore choices retain full wrapping captions at native large text, a real command/
spoken target and busy disablement. Opening restore and returning keeps the same step and every draft choice;
it must not force account creation. Preserve the existing footer and secondary action meaning.

### Complete Home account refresh (D-92)

Build all account presentation values before publication. One Reset rebinds existing native rows to fresh contexts,
even with equal record values, so semantic theme colors and translated amounts update. Keep every row, order, command
and money packet; do not cap visible accounts or discard current values when snapshot enumeration fails.

### Complete Home customization (D-94)

Section identity occupies a full-width wrapping Auto row; reorder buttons and the visibility switch use a separate
row below it. Preserve native scaling/RTL and local 44 px targets. Reset uses the existing growing semantic action
with its complete translated native name and unchanged command. Keep all eight sections and the profile's layout
preference semantics. Verify actual glyph boundaries and native move/visibility/Reset persistence on fictitious data;
do not treat truncated intermediate renders or process-local scaling as platform accessibility acceptance.

### Hidden Home account views (D-95)

Keep the complete financial/presentation snapshot regardless of section visibility. Hidden Accounts has no attached
BindableLayout source/native rows; show attaches the current complete collection, hide detaches it. Visible reloads
retain native rows and refresh contexts. Keep account detail and the complete Accounts list reachable with unchanged
commands. Review real native visibility switches, navigation, warm rebinds and stored data; wait for arrangement
before geometry checks rather than weakening checks for unarranged rows.

### Complete account descriptions (D-96)

Type/default/excluded/incomplete captions retain complete scalable text and compact flex grouping. Bound each
description to the actual group width before measuring wrapped height; do not shrink an already measured label
into a taller native text block. The whole identity stack must grow above the amount. Check actual glyph bounds
against both native and MAUI allocation; IsTextTrimmed and a native slot alone do not prove no clipping. Review
all existing flag combinations with presentation-only fictitious values, retained rows and exact no-write data.

### Complete debt entry action (D-97)

Accounts uses Secondary WrappingAction for its complete debt/receivable caption, retaining native scaling, semantic
colors, spoken name and the existing new-debt command. Check realized glyphs within both MAUI label and native
target. A newly opened animated modal needs actual arranged dimensions before checking date inputs; bring the
reference date into view through native Scroll without focus or Save. Opening/cancelling a draft never posts principal.

### Single growing modal header (D-98)

Explicit Shell modal presentation owns its existing Cancel/Close/title row; do not attach an ordinary child
PageHeader based on the underlying stack depth. Own title/Cancel groups wrap: the real Close target does not
shrink, the complete scalable title grows within the available group width and moves below when needed.
Keep asset-event title/subtitle together and retain existing command/footer behavior. Verify realized glyphs,
actual native peer name and no title/button overlap; cancellation retains the ordinary parent's single header.

### Complete plan-field feedback (D-99)

Collect independent applicable problems before a write; do not postpone destination feedback behind name/amount
errors. Place each caption beside its source/destination input and keep transfer-safe wording. After invalid Save,
reveal the first affected input without keyboard focus or text replacement. Clearing prior captions changes layout;
use current native positions, scoped latest/visible requests and bounded removed Android layout observers.
Unknown/hidden amounts and unchanged recurrence/money rules remain. Corrections clear old errors on the next Save.

### Complete transaction-field feedback (D-100)

Show all independent applicable amount/account/fee/original-currency/reimbursement problems together beside
their fields before entity mutation. Reopen collapsed invalid detail fields, keep draft text and reveal the first
affected input without changing focus. Resolve native request ordering after dynamic layout changes; preserve
ordinary keyboard/scroll policy, latest/visible request guards and bounded removed Android observers. Transfer
positivity wording does not suggest expense/income. Clear corrected captions on the next Save.

### Existing destination fees in Simple (D-101)

Simple is a presentation level over the same ledger. An existing destination fee must remain visible/editable
so an unrelated transfer edit cannot silently delete it. Keep creation Advanced-only when no fee exists;
explicit blank/zero means removal. Retain the fee id, group, currency and transfer/source-fee financial fields.
Refresh the net-effect summary when fee visibility changes; opening, editing and clearing must show the actual
amount received after fees. Check successful edits with real native Save, independently from invalid-Save evidence.

### Advance-settlement validation and action (D-102)

Show reversed period and invalid/missing/nonnegative bill requirements independently beside their fields. Explain
periods with no advances, and clear corrected field problems without resetting typed input. An idle Record the
difference remains available for field explanations; reveal the first affected field without taking desktop
focus. Keep the full scalable caption/spoken name and minimum target in its persistent dock. Initialize DateFields
before binding. A zero bill is explicit input, not a placeholder value to post; matching bills require no entry.

### Due-item actions and amount feedback (D-103)

Use full growing native action captions/names/targets for completion, partial payment, this-occurrence change,
skip, undo and plan navigation. Keep payment and optional override errors beside their own inputs and independent
during corrections. Invalid action reveals its own field without moving desktop focus or resetting the draft.
An omitted override retains the plan amount; only a positive payment may continue. Initialize DateFields before
binding and keep the existing explicit metadata/partial/completion continuations.

### Complete transaction rows during repeated filters (D-104)

Keep the complete ordered result and all filter/drill-down scopes. Reuse presentation objects only within the same
source/display snapshot; fresh reads and formatting/translation/theme/unit changes must invalidate them. Refresh
selection from the bulk flow. Do not hide rows or change totals to improve a performance counter. Retain the existing
loading/failure/retry cover, actual row button commands, growing captions and persistent bulk/Undo actions.

### Complete selection during transaction reload (D-105)

Refresh retains every selected id still present in the complete source; removed rows lose selection and new rows are
not implicitly selected. Select all applies to visible known rows and preserves hidden selection. Cancel clears the
whole selection. Busy dialog guards and explicit financial confirmation/validation/Undo remain unchanged.

### Exact history with complete Settings publication (D-106)

The optional spending suggestion uses three complete financial months in the selected calendar/pay cycle. Read that
complete date window; keep the form covered until preferences/accounts/suggestion/device availability all finish.
Preserve raw unsaved estimates on reload. Using a suggestion changes only the draft; saving remains explicit.

### Growing native picker choices (D-107)

Windows selected choice names and popup items wrap within the existing native picker rather than losing calendar,
region or other descriptive text. Keep the native selection/keyboard/semantic behavior and inherited fonts, direction
and colors. Popup rows grow from a minimum 44-unit target. A long selected name grows its control, not an extra label.

### Complete transaction report scope (D-108)

Scope, category and custom-period captions wrap inside the available width with independent 44-unit Clear targets.
The original complete filter form scrolls vertically when it exceeds its actual available space. Its height accounts
for current Add/bulk/Undo docks and reserves up to 144 logical units for results; native typography is never shrunk.
Keep the existing horizontal filter choices and complete result/order/totals. Clearing changes the current view only.

### Independent named filters (D-109)

Applying a saved filter replaces the persisted combination independently of the report opened before it. Remove
the old report's scope caption and transient currency/account-set/confirmed-only restrictions; preserve all named
filter choices. Never show an old report label over a new filter's result. Applying a view does not save money.

## Complete unchanged transaction sources (D-110)

An equivalent filter retains the complete bound source only when captions, boundaries, order and row objects all
match. Fresh reads or display contexts publish new sources. Selection remains observable; no result is capped.

## Repeat from a new transaction (D-111 / OD-12)

Repeat is a growing real action after complete basic fields in both modes, with a visible no-save hint and full
translated help. Validate applicable base amounts/accounts beside their fields; never record money to navigate.
Prefill Monthly anchored to the selected first date/calendar, keep posting/reminders off and show actual dates.
Returning preserves the original unsaved transaction, including details not supported by a recurring plan.
Do not reinterpret parsed minor units if an account/currency has changed. Existing-entry recurrence stays separate.

## Complete transaction category choices (D-113)

Category chips wrap complete scalable names within their current group. Reserve outer spacing inside the finite
row and use independent heights: a long neighbor must not inflate short choices. Retain original choice identity,
icons/semantic colors, real selection commands and full spoken names. Selection does not save the transaction.

## Single savings-goal warning packet (D-116)

Display the complete existing warning packet once per goal card, after progress and contribution suggestions.
Keep every distinct funding/date/account warning, scalable 13 pt text, semantic amber and the existing full-card
button/name. Never repeat the same packet above and below progress or discard a warning to shorten a card.

## Commercial policy remains independent of presentation (D-117)

The owner-approved final Core policy never reads Simple/Advanced. Free retains history, corrections, essential
warnings, protection, backup/restore, own basic export and regional/accessibility choices. Financial month start
and the basic forecast through month end are Free. Guest Plus belongs only to the exact authorized shared space,
not personal profiles. Core policy delivery adds no paywall or quota message; test builds remain unrestricted.

## Account write policy boundary (D-118 / ENT-02 in progress)

The Data store prepares structured permission/quota failures before writes; final translated limit presentation
belongs to ENT-04. Current test-build enforcement is inactive, so this step adds no visible restriction/message.
When enabled after approval, account creation and unarchive use the same active quota; correction/archive retain
existing data above quota. No automatic archive, deletion or active-account selection is introduced.

## Template/filter write policy boundary (D-119 / ENT-02 in progress)

Retain the existing entry-detail Save as template and Transactions saved-filter flows. A confirmed same-name
filter replacement reuses its existing slot; failed replacement preserves the complete old query. Existing-id
edits remain possible above quota. Structured rejection is prepared before writes; translated limit UI remains
ENT-04. Current test builds stay unrestricted without a new visible control or message.

## Goal/plan write boundaries (D-120 / ENT-02 in progress)

Keep the existing Goal and Plan forms, detached drafts and explicit Save. Pause retains capacity; history edits do
not enable paid new work. Split/resume continues one plan rather than consuming an extra slot. Rejection precedes
Home pin/protection changes or partial batch storage; failed later history moves roll back completely. Current
test builds remain unrestricted. Final translated quota feedback and retained-form purchase navigation are ENT-04.

## Current-period budget write boundary (D-121 / ENT-02 in progress)

Keep the existing budget form, financial dates, copy actions and confirmed replacement. Retained limits remain
editable above quota; a rejected replacement must preserve the complete original budget. Current test builds
remain unrestricted. Final translated limit feedback, explicit active/read-only selection and future-period
activation remain unfinished ENT-02/03/04 work; no automatic selection or new UI is introduced here.

## Atomic holding editor Save (D-122 / ENT-02 in progress)

Keep the same purchase form, amounts, quantity/date/location, account and fee fields. Its explicit Save commits
quantity, payment, fee and marked purchase-derived price together; failure preserves the previous complete group.
Retained quantity/metadata corrections and delete/Undo stay available after expiry. Reasoned quantity corrections
do not create money or prices. New standalone prices remain a separate management operation. Current test builds
remain unrestricted; translated limit feedback and explicit read-only selection remain ENT-03/04 work.

## Complete explicit goal editor Save (D-123 / ENT-02 in progress)

Keep the existing goal form and unsaved name/target/date/method/reminder choices. Explicit Save commits the goal,
Home pin changes and contribution draft together. A failed contribution Save cannot leave a partial goal or an
unrelated pin removed. Contribution estimates and earmarks never become ledger entries. Retained correction,
release and deletion rights remain available; empty dates support a basic goal. Test builds remain unrestricted.
Selected read-only choices and translated commercial-limit feedback are still ENT-03/04 work.

## Stable recovery destination and explicit import Undo (D-124)

Keep the current file picker, preview, explicit link/keep-both choice and Undo confirmation. The restore database
is the one selected when its source starts reading input; a later profile selection cannot redirect replacement or
migration. Import and Undo preserve complete aggregate/detail data and metadata with no partial writes. Owned data
above quota is retained, without granting paid rights. Current test builds remain unrestricted; explicit selected
read-only resources and commercial feedback remain ENT-03/04 work.

## Retained categorization rule patterns (D-125)

Keep the rule form's existing explicit Add and confirmed Delete, and the transaction detail's save-rule action.
New text/kind configures new automation; correcting the category of an existing pattern preserves data rights.
Same-pattern new-id replacement remains one rule. No stored transaction is reclassified, no draft is trimmed on
commercial rejection, and no limit UI is activated in test builds. Suggestion-delivery policy and translated
commercial feedback remain unfinished.

## Ledger corrections and final refund validation (D-126)

Retain explicit Save, split editing and combining. Corrections of existing parts remain available after downgrade;
new parts configure new split work. Refund Save feedback describes the final batch: an edited purchase cannot
become smaller than its retained refunds or change away from Expense. Missing targets and exceeded totals use
existing translated validation errors. Test deployment remains unrestricted; commercial feedback stays ENT-04.

## Retained short Undo after failed saves (D-127)

Keep the existing eight-second offer and caption. One Undo runs at a time; a failed action may be retried only
within the original remaining time. Never extend the deadline, restore a dismissed offer or clear a newer offer
when an older action completes. Returned deletion data belongs to the original profile. Existing receipt rows
remain available through Undo. No test-build commercial limit is activated. D-126's bottom-of-form SaveError
visibility remains a separate observed UX follow-up, not resolved by this slice.

## Native Undo failure feedback (D-128)

An unsuccessful Undo shows the existing translated generic error dialog and preserves its original short offer.
Keep the actual command disabled through native feedback and list refresh; repeated invocation cannot execute a
replacement offer while a dialog is open. Refresh only after successful Undo. A failed list reload after the
write never reoffers an already committed Undo. D-126's visible ledger Save feedback remains open.

## Undo after later financial changes (D-129)

The short Undo offer restores its committed deletion, not caller-mutated objects. If retained refunds, partially
restored siblings or account currencies changed, report failure through the existing guarded native command and
preserve every newer edit. Keep the original deadline; do not silently move relationships or relabel money.
Renaming/archiving an account still permits unchanged-currency recovery. A completed Undo remains completed.
The native editor timing inside the short window is separate from installed conflict/error-delivery checks.

## Transaction Save feedback remains with Save (D-130)

General entry Save failures belong to the fixed growing footer, before the effect and original Save action.
A rejected purchase correction, a storage failure or receipt-unit review must be visible at either body scroll
position, with complete wrapping/scalable text and a complete accessible name. Keep field errors beside inputs
and their existing reveal behavior. Preserve every unsaved value and allow only explicit retry or discard.

## Explicit downgrade choice and retained bills (D-131)

No resources are silently selected, archived or removed after a downgrade. Selection is independent from original
paused/ended state and from feature/membership rights. The model and ledger boundary are implemented; the user
choice/persistence screen and remaining bindings are unfinished, and current test builds remain unrestricted.
Bill settlement keeps its actual period/amount preview and explicit Save. If reviewed plan/advances change or the
writer fails, retain every unsaved field and show the existing translated generic error; never silently post a
different reviewed basis. Native successful extra/refund/zero-bill navigation is checked independently of unit tests.

## Retained debt closing stays explicit (D-132)

The existing Record repayment/Record return action opens the ordinary unsaved transfer form. Only explicit Save
records actual money. Closing an existing principal remains available after downgrade; estimates and partial
payments are not silently converted into closure. Keep the transfer and actual fee rows, account identity and
draft/error behavior. Do not archive a zero-balance account automatically or infer relationships.

## Retained occurrence corrections and explicit payment links (D-133)

Linking is the existing explicit confirmation of a named actual transaction and amount. It never records a second
payment, silently moves another occurrence's payment or changes actual money/metadata. Retained Skip/Unskip/change
and repair do not need a selected account or paid host. Normal installed confirmations are verified independently
of enabled commercial SQLite tests; the current app remains unrestricted. No caption/layout change is introduced.
Existing occurrence-command failure feedback and other settlement/reopening paths remain separate follow-ups.

## Complete planned payment saves (D-134)

Existing Confirm and Pay part actions keep explicit actual amount/date and established validation. A payment and
its matching status commit together. Pay part uses the current stored amount and earlier actual payments, not an
old form balance; reaching the remaining amount settles once. No caption/layout change. Native normal delivery
is separate from SQLite failure rollback; existing command exception feedback remains an unfinished follow-up.

## Reopening the reviewed payment (D-135)

Keep the existing explicit Reopen confirmation and financial effect: generated payment deletion, manual/imported
payment unlinking. Recheck the reviewed identity under the writer; never target a different payment from an old
form. Preserve earlier partial amounts and edited occurrence details. No caption/layout change. Normal native
success is verified separately from SQL/access rollback; command exception feedback remains an unfinished follow-up.

## Occurrence action failures (D-136)

Full/partial payment, explicit linking, Skip/Unskip, date/amount/note changes and reopening use actual
OccurrenceCommands with the existing PlanStore writers. One gate spans native consent, writer failure feedback
and successful display publication. Rejection/cancellation never reloads, navigates or replaces the draft; ledger
validation stays inline. Unexpected failure uses the native interaction port without raw user-facing exceptions.
Retire the writable reviewed snapshot before post-commit navigation/refresh and publish its replacement only after
all load reads finish. A refresh failure explicitly says the change was saved and requires closing/reopening the
screen before another change. Explicit Reopen/Cancel captions exist in all six languages; French/Italian no longer
present two cancellation captions. No commercial activation, schema, SDK, permission, backup or device-setting change.
29 added actual application-flow cases (AT-138), App.Tests 347. Main 2,201 pass with an independently owned,
ignored Data.Tests output configuration limiting collection concurrency to one. Explicit writer-race tests retain
their concurrent operations. Two normal parallel full-suite attempts failed in unrelated Data.Tests SQLite cleanup/
migration (locked file and disposed native handle). Preserve negative logs; D-137 later completes file-scoped pool cleanup isolation.
An unsupported argument ran zero tests and a help invocation failed; neither is acceptance. Output-only test
configuration was removed; no product workaround or test execution policy was committed.
Strict Windows/Android builds and complete signed Release APK verified. Native normal Release emulator checks cover
en/fa/de, light/dark, actual error dialogs, retained drafts and all 24 complete stored tables. Six additional normal
Release cases commit an actual partial payment, deliberately break only the owned fixture read, display the saved-
but-not-refreshed message and reject replay from enabled native actions. No Windows native
command, real phone, iOS, provider or publication acceptance claim. Selected-plan generation/automation, choice
persistence/UI, commercial activation and existing external gates remain open.
[Evidence](quality/occurrence-command-feedback.md).

## Independent profile lifecycle (D-137)

Profile switching, inactive deletion and first-run restore retain the existing actions, confirmations, busy states and onboarding/account policy. Pool retirement affects only the owned database. No new caption/control/layout; normal Release native profile and restore checks are recorded in [evidence](quality/local-sqlite-pools.md).

## Current automatic payment values (D-138)

Automatic posting rechecks the actual plan and occurrence at Save, without asking for a second confirmation or creating a new form. Partial manual money and suppressed/blocked candidates remain for explicit review; settled or no-longer-due candidates are not reported as pending payments. Current overrides and accounts supply the recorded money/date. Existing layout and inactive commercial registration remain. [Evidence](quality/automatic-occurrence-writer.md).

## Selected future plan occurrences (D-139)

Home action counts/rows, Plans Due/Upcoming and detail Upcoming use the actual scoped future-work projection.
The All list retains every original plan identity; completed/skipped history and open effective past-due payments
remain accessible. Forecast, budget and KPI calculations keep complete original schedules/states. No automatic
choice, hidden pause/end mutation or current test-build restriction. Choice UI/read-only feedback and actual
reminder/snooze delivery remain separate unfinished ENT-03 work. [Evidence](quality/selected-plan-occurrences.md).

D-139 also applies D-82 navigation retirement to profile/onboarding root replacement, before old native handlers
are detached. Keep current forms, deferred display rebuilds and the startup lock cover unchanged.

## Scoped reminders and fresh snoozes (D-140)

Summary notifications retain their original members and the active profile file's opaque local identity. Snoozing
rechecks current open rule/slice/reminder state and selected work; it never adds a new guessed member, posts money
or moves a due date. Rebuild text from current privacy and display choices; generic summaries reveal no names or
item count. Keep one shared pending queue and stable repeat id. Taps wait for the app lock and resolve the current
file, without silently switching profiles. Unscoped legacy caches cannot prove membership and are retired when
normal notifications are rebuilt. This is the final routing format, not a temporary commercial policy.

## Durable reviewed choices (D-141)

Choice loading is read-only. Present the complete original identities and states, never a guessed first subset
above capacity. An explicit empty choice differs from no choice. Saving a reviewed bounded choice must reject a
changed profile, revision, original list or rights; retain the form for review instead of overwriting a newer choice.
All financial history remains visible/correctable. Unlimited and inactive test builds need no choice. This slice
provides the store and actual writer/reminder bindings; the visible chooser is the next delivery step.

## Reviewed active items (D-142)

Show Accounts/Plans as two wrapping native choice chips. Give identity, details and original state their own growing
rows above the separate selection caption. Do not confuse a paused plan with an unselected plan or resume it on tap.
One real row button owns the complete live description; historical states remain readable but cannot be selected.
Keep counters/capacity errors beside the choices and scalable Save/reload outside the scroll viewport. Cover all
loading/failure publication and offer translated retry. Resolve dirty switch/reload/Back explicitly, preserve live
translation/help drafts and retire a saved review before navigation. Unrestricted test builds explain that no subset
is needed. Keep rejected Save feedback beside the pinned action, outside the scrolling list; do not make a
user scroll back to discover a failure. [Native matrix and actual registered navigation](quality/active-item-chooser.md).

## Selected goal semantics (D-143)

Retain original goal state and measurement separately from future availability. Pause counts without resuming on
selection. Explicit Save uses reviewed choice; no guessed subset, financial posting or historical rewrite. Goal
chooser UI and effective contribution/reminder presentation remain open. Current Accounts/Plans form and
unrestricted release behavior are unchanged. [Writer evidence](quality/goal-resource-choices.md).

## Goal choice in the growing availability form (D-144)

Use three wrapping native choices with complete translated names and 44-pixel targets. Display original goal
identity, measurement and state separately from choice. Currency belongs to money-goal metadata, not quantity units.
Completed is a goal state; do not call it an ended Plan. Pause counts without resuming. Keep the same covered loading,
explicit scoped Save, growing pinned error/action, exact live draft and dirty-transition consent. Explain one-goal
capacity with a concrete translated example and keep paid-tool rights separate. [Native evidence](quality/goal-chooser.md).

## Original goal work and reminder destinations (D-145)

Keep original progress/history separate from selected new work; selection does not resume a pause or unlock a paid tool. Reminder taps open the original detail or existing Goals root through app lock, with actual-file validation and no silent profile switch or financial Save. Preserve generic/detail privacy, opt-in and 09:00 local rule dates. Goal-view contribution affordances follow separately. No layout/copy change; current inactive test builds stay unrestricted.

[Native delivery evidence](quality/goal-work-reminders.md).

## Complete original goal views and retained releases (D-146)

Keep lifecycle, financial tone/progress and new-work availability separate. Preserve original Home pins and let long goal identities wrap. Newly opened release-only goals show Release and an original earmarked account; reload retains explicit input. The neutral hint explains both earmarking and releasing without moving money. Keep price/currency feedback beside its input and failed-publication feedback/retry outside the disabled body viewport. One native busy guard spans consent/write/publication.

[Running-app evidence](quality/goal-work-views.md).

## Reviewed goal editor and retained contribution drafts (D-147)

Keep the original goal identity/state, archived references, hidden contribution method, complete saved recurrence
rule/end/calendar/holiday choices, all unchanged spending-cut categories and the retained price assumption. Metadata-only
corrections never create a contribution plan for a legacy goal without one. A new draft saves its explicit named monthly
anchor. Show the actual rule/end/next dates and original method even in Simple mode; multiple saved categories remain
explicit rather than silently becoming the first category.

Construction performs no database read. Cover complete initial/retry publication; preserve explicit dirty fields and their
numeric parse culture/unit. Live captions never replace selections or reinterpret raw input. Changed currency/measurement
facts require actual reentry. Explicit reload merges only dirty fields into fresh original values, displays the current
saved target/rule/note and requires the review action when original inputs changed. It never silently accepts an overwrite.
Shell's empty native modal-attachment query retains an already opened identity; another explicit identity is rejected.

Separate original corrections from new paid contribution/reminder configuration. Paused selected paid configuration stays
paused and never prompts or delivers a reminder. Retained corrections/disabling remain available. Production commercial
registration is still inactive and unrestricted. No choice, display change, opening, permission or navigation posts money.

The paired writer rechecks the captured actual file/rights/choice and complete goal/rule/allocation/reference stamps before
mutation. Clone the complete original goal; never mutate it before a successful write. A successful save retires the draft
before returning, so navigation failure cannot resubmit it. One busy guard spans consent, permission, write and navigation.
An explicit holding/quote-reference change confirms clearing only the previous assumed price; cancellation keeps the draft.

Apply to budget remains a separate explicit confirmation. Its original monthly category limit, full budget metadata/all
limits, financial period/day and category wording are rechecked inside the actual SQLite writer. Change only the confirmed
category limit, never a goal, contribution, allocation, account or ledger entry. Concurrent equal-time edits and duplicate
commands are rejected; unrelated name/display changes do not reenable the same already applied cut.

40 new AT-149 actual-source/real SQLite cases pass. Final normal parallel main 2,560 / App.Tests 537 pass,
zero failures/skips; test output cleaned. Strict Windows and Android builds and canonical Release APK build have zero
warnings/errors. Eighteen English/Persian/German, light/dark, 360/412/wide contexts check the actual Advanced and Simple
modal at process-local 1.5 font stress: complete changed glyphs, real 44-pixel Invoke targets, stale Save rejection,
retained reviewed reload/current saved summary and native Close. All 25 complete fixture tables and original development
data/sidecar bytes are restored. The review uses independent fictitious data and explicit native-effect answers, not
replacement MAUI controls or a production entitlement replacement.

Six normal signed Release owned-emulator cases check the original archived account, uncommon rule, multiple-category
warning, pinned Save and native Close/Back. One actual note-only Save succeeds; complete original rule/categories,
accounts, ledger, allocations and other metadata stay equal. Only that goal note/audit and the previously delivered
D-141 migration differ. Exact owned original bytes/sidecars and Main/English/System are restored; the normal Release
remains installed/stopped. No physical-device, iOS, OS accessibility, provider, performance, Store signing/publication
or commercial-activation acceptance is implied.

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

Evidence: `docs/quality/goal-editor-work.md`.

## Selected budget definitions (D-148)

The foundation supplies all original periods and an independent work projection. The later chooser groups canonical
currency/period/account-set definitions; copied months are not separate choices. Preserve visible history and paid
method numbers; selection never grants a method. Reviewed choice Save must retain drafts on stale input and never
write limits or money. No visible layout or commercial enforcement is changed by this foundation.

## Reviewed budget definition rows (D-149)

Use the existing fourth growing choice alongside accounts/plans/goals. Group copied periods once and show original
currency/period, explicit account names or the all-included meaning, current saved methods and saved-period count.
Retain history but disable historical-only choice. Format count digits without rewriting user account names.
Explain that choosing a definition grants no paid tool and changes no financial limit. Shared covered publication,
discard guards and pinned explicit Save/retry feedback apply. Inactive release remains unrestricted/read-only here.

## Current budget alert publication (D-150)

Default new-budget calendar never filters saved current alerts. Retain original method/currency/account scope
and generic text unless details were explicitly allowed. A failed or retired native batch must not consume
threshold levels. Refunds permit later re-alerting. Existing current-budget destination stays behind app lock;
scoped returning-data and view/editor work affordances remain open.

## Original budget notification navigation (D-151)

Open the exact original saved period through app lock, even if it is now historical. Never reinterpret a USD,
weekly or other-calendar alert through current Home/new-budget defaults. Notification navigation does not
change the saved Home currency; period/theme reloads retain the in-memory visit. Weekly rows prepare valid
month coordinates in their original calendar. Deleted/foreign/error destinations never guess a replacement.
General later visits keep current-default behavior. Commercial view/editor affordances remain open.

## Reviewed original budget editor (D-152)

Cover the whole form until complete original references/preferences and work rights publish synchronously. Keep original archived/missing limits and explicit account scopes in Simple and Advanced. Separate current new-tool eligibility from retained corrections; future periods do not use today's capacity affordance. Show every invalid amount beside its field in one attempt. Category identity/suggestion occupies its own growing row above the amount. Raw amounts retain their input culture until retyped. Stale Save keeps input, pins retry feedback and requires explicit reload/review. Merge only edited fields; show current complete saved values before Keep reviewed edits. Save retires the form before return; Close remains available if return fails. Native Back shares the busy/discard guard. Opening never posts ledger money or creates Settings.

D-153: the original budget page publishes independent edit/copy/delete availability after complete review. Keep
all original figures visible when new work is unavailable. Growing action captions retain native names/targets;
failed writes require explicit reload, while a saved operation with failed refresh offers only read-only retry.


D-154: show six growing resource destinations. Keep all original quick templates visible with separate draft
activity enablement; retained saved queries remain history-only, independently of saved-definition slots. Pin
the complete submitted query before prompts, map duplicate names to original ids and confirm deletion. Cover
template publication, wrap complete template names and offer explicit Reload beside pinned failure feedback.
Never repeat a committed operation after failed native feedback/refresh.

## Original Home quick-template destinations (D-155)

Keep the existing six shortcuts, with explicitly available later templates first. Preserve complete user identity and spoken name, separate selected enablement and parent publication guards. Show translated unavailable/reload feedback in Home; a changed original destination leaves the draft intact and pins feedback beside Save. Initial selection navigation opens only the template kind, without guessing choices. Reading a legacy Home layout never writes Settings.

## Reviewed original Profiles form (D-156)

Original profile cards have independent real navigation and activity-choice targets with complete live spoken
names. Original saved availability and the unsaved choice caption are separate. Explicit Save changes only the
device subset; opening never silently selects an original. Creation requires a free chosen slot when verified
bounded policy applies. Unchosen history remains reachable; current/main profiles cannot be deleted. Full loading
and failed-read covers publish all originals before input. Save/Add/Reload reserve their own rows outside the
scroll viewport, while long names wrap. Busy prompts and Back share the same discard guard; live captions retain
raw names and unsaved identities. Test-build registration remains inactive. See [evidence](quality/local-profile-work.md).

## Original profile work across retained forms (D-157)

The same existing read-only work caption applies to a retained resource in an unchosen original profile.
Keep all original identities and financial progress visible. An explicit profile choice never clears raw drafts,
changes currency, creates money or grants a paid goal type. Retained query viewing and financial correction have
separate eligibility from creating a definition or starting positive work. Return to a chosen original file
restores its separately authorized basic work. Current test builds remain unrestricted; translated dedicated
profile-limit messages and remaining operation forms are a later ENT-04 slice.
See [evidence](quality/profile-file-work.md).

## Reminder refresh after original profile choices (D-158)

Saving a device activity choice refreshes the current original profile's reminder queue; no additional financial
Save or profile switch is required. All original names and history remain visible. Review reminders keep the
established opt-in and fixed 09:00/calendar/month-start behaviour, with independently authorized delivery.
Privacy/display/period changes retire stale publication. Existing forms, captions and layout stay unchanged.
Test builds remain unrestricted. See [evidence](quality/profile-reminder-work.md).

## Reminder consistency with original Plan/Goal data (D-159)

Changing original reminder rules, state or effective financial progress retires stale pending work. A Goal
that reaches its target does not keep an old contribution request. Complete history and explicit drafts remain
independent; publication never saves money or a review. Existing forms/layout/translations stay intact.
See [evidence](quality/reminder-input-work.md).

## Expected write reasons and retained filter retry (D-160)

Known quotas/permission/selection failures name the real reason in the current language, without enum ids
or purchase claims. Keep every unsaved field and separate accepted-save/failed-return feedback. A rejected
filter preserves its exact raw name and complete query; the next explicit tap rereads rights without clearing
the page. Process-local names never cross original files. Counter text uses native digit choice and RTL marks.
The shared reporter does not promise that nothing saved after a committed action.
See [evidence](quality/commercial-feedback.md).

## Explain readiness before Save (D-161)

New Goal/Budget forms name their actual captured capacity, missing feature right or required explicit choice.
Keep complete raw drafts and native disabled Save semantics. Use the same original reviewed policy that
controls the affordance; never calculate a second quota or guess a purchase/provider state. Retain original
correction/history behavior and distinguish field validation from commercial readiness.
See [evidence](quality/pre-save-readiness.md).

## Compare without changing current access (D-162)

Keep the current access card separate from Free/Plus/Pro comparison choices. Use growing native chip targets
and separate full-width identity/value rows; pinned Continue stays outside the scroll viewport. Every selection
is presentation only. Shared/local/profile-choice facts stay distinct. Unavailable online services are an explicit
roadmap; never sell unbuilt features or infer prices, renewals or purchase restoration. Explain no-change return
and active-item navigation in complete translated help.
See [evidence](quality/product-plan-comparison.md).

## Retained editor rejection and committed Close (D-163)

Place complete actual write reasons above pinned Save, preserving every raw field and original validation.
Separate accepted write from default preference, subscriber and navigation failures. Accepted forms become
read-only with Close and cannot resubmit. Keep loaded input enabled in its original binding context; no
child-context lookup may accidentally grant editing. Native query/effect adapters stay separate from flows.
See [evidence](quality/editor-write-feedback.md).

## Explicit original archive consent (D-164)

Archive never silently saves a dirty financial draft. Combine original affected-plan information with explicit
draft-discard consent; declining keeps the same form. Block competing commands during consent/write. A stale
review never toggles the inverse of a changed state. Accepted state/follow-up failure remains saved, read-only
with Close; exact default cleanup is reported without guessing another account.
See [evidence](quality/account-archive.md).

## Original Account/Plan pre-attempt explanation (D-165)

Keep complete translated access/selection/capacity feedback above pinned Save. Explicit Refresh access
reads current writer inputs without clearing, normalizing or saving any draft. Refresh failure disables
Save with retry feedback; input stays editable. Current review is an explanation, never write authority.
Actual rejection retires it until explicit refresh; historical correction/split behavior remains original.

## Visible original import outcome and review (D-166)

Pin complete import/Undo feedback and original review/import actions in their own growing row outside
the body viewport. Keep full history identity separate from a growing native Undo action. A failed
pre-write attempt retains the original reviewed file/choices; a completed attempt retires that payload
before fallible publication. Picker cancellation preserves the form; busy guards cover consent and
authentication. Successful notification/history failure states completion without offering a repeat.

## Restore review and completion (D-167)

Keep complete review/result/error and growing actions outside the body viewport. Freeze accepted input;
show completed restore separately from failed follow-through, with explicit Open restored data retry.
Busy and unfinished accepted completion block Back/tab departure. New stream read failure retains old
review; uncertain source replacement requires fresh review/consent. See [evidence](quality/restore-outcomes.md).

## Forecast publication and retained analytical drafts (D-169)

Cover original reads/projection until all native choices/cards/history are ready. Pinned growing feedback
and Refresh stay outside the viewport. Refusal preserves horizon/scenario/name; only explicit Reset
removes a scenario. Simple/Advanced is presentation, not payment authority. Free saved details show the
entire original path and Delete; hide newly computed comparison parts. Accepted Save stays completed.

## Original receipt source and review lifetime (D-170)

Keep original Home and attachment actions and explicit confirmation-before-Save form. One busy guard
covers picking/unlock/recognition/dialog/navigation. Refusal precedes native source work; translated
feedback gives no exception content. Empty existing rereading never erases an amount. A typed source
is checked again before applying to the editor. Native review waits for the newly revealed detail
action's actual caption/command/name geometry; no font or device setting is changed.

## Original loan review and payment lifetime (D-171)

Cover complete account/schedule reads; retain observed balance/reason/date on explicit refresh. Real
terms do not imply paid estimate availability. Keep original manual repayment and essential underpayment
warning; captions describe the actual manual or calculated action. Full feedback/result and Refresh
own a growing footer. Schedule identity/payment/split/remaining own rows with original scalable caption
typography and complete MoneyText packet. Busy native consent/write never silently repeats accepted payment.

## Original report scope and detached export (D-172)

Cover initial/refreshed original report publication; pin complete feedback and growing Refresh outside
the viewport. Keep the selected unavailable package, rather than silently switching its content. UI
Simple/Advanced controls detail density, not paid access. Original financial facts/warnings and basic
account movement remain available. Native export busy starts before authentication; a detached PDF
must preserve every live choice and status. Account tables state their original currency. Accepted
contribution feedback stays distinct from refresh failure and does not imply a ledger payment.

## Native quick widget actions (D-174)

Android launcher actions use native Button/ImageButton roles, named in the app language. The symbol
keeps its original 40 dp artwork within a 44 dp target. Caption/direction updates must not rely on a
host's cached resource locale or unpublished startup defaults. Original immutable intents open only
unsaved work through the existing app gate. Native trees are not a spoken screen-reader pass.

## Original date purpose names (D-175)

A date's three native inputs and calendar button include their own existing translated purpose.
Do not rely on encountering a preceding visual label to distinguish several dates. Name updates
retain raw typing, date value, calendar, part order and visible placeholders; no container grouping
replaces the actual editable children. Native naming evidence is separate from spoken navigation.

## Original Home layout reuse (D-177)

Default source order matches the existing saved-layout default. Keep correctly placed native sections
attached on appearance; move only displaced containers. Visibility and complete financial bindings
remain authoritative. Native tree reuse is engineering evidence, not a startup-duration or device claim.

## Original holdings captions during Home projection (D-178)

Keep quantity units separate by asset type and preserve original type order, date and formatting.
Routing events once must not replace the original checked quantity calculation or alter stored
names. Native digit shaping applies to the final caption, including digits within a stored name.

## Retained icon accessibility names (D-179)

An unchanged icon key supplies its current translated accessible name, including the selected suffix.
Retain the same native buttons and complete draft through language updates; unload removes the
translation subscription and reattachment catches up without replacing the original selection.

### D-222 startup surface

The first page contains only the untranslated product name, native progress indicator and translated
preparation status. A failed preparation shows generic wrapping feedback and a growing native retry
button. Financial pages stay unpublished through storage/access preparation. No exception details,
key values or filesystem instructions appear in this product flow. Native text scaling is retained.

## Android protected recovery surfaces

Covered startup offers recorded-code recovery and explicit Zanance backup recovery after independent
app-access authorization. A backup form shows the selected file, optional password and an explicit
confirmation; Cancel retains old data. Failures use translated generic feedback without filesystem,
cryptographic or credential details. Never offer a new profile as implicit recovery for missing keys.

Settings exposes optional recovery setup with a full help explanation. The user acknowledges recording
the ephemeral code before Save. A completed enrollment shows only its original-code explanation,
help and Back; it must not show an empty code field, acknowledgment or disabled Save button.
Keep native text scaling, growing captions, direction-safe code inputs and at least 44 px native actions.
The corrected Android Release hierarchy passed en/fa/de in light/dark at 360/412/wide widths.
Growing startup/Settings/file-selection actions retain original commands and complete captions;
spoken/pixel/device acceptance remains separately tracked in quality/android-protected-storage.md.

## Windows protected recovery surfaces (D-264)

Windows uses the same explicit optional recovery setup and covered startup backup/key-recovery flows as Android. Existing app lock remains independent and is never bypassed by key recovery. Native titlebar foreground and background both follow semantic palette resources. Eighteen native recovery views cover fa/en/de, light/dark and narrow/wide layouts; scoped titlebar captures do not constitute spoken accessibility acceptance.

## Holding actions and original page lifetimes (D-265)

Holding commands use growing native surfaces with complete semantic names and hints; history identities/date and quantity occupy separate rows with the same full-row overlay command. Descriptive valuation sentences wrap. An old Home cannot publish awaited reads after its Shell retires; register actual appearing instances rather than assuming lazy ShellContent.Content exposes them. Review off-viewport pickers only after native scrolling realizes their captions.


## D-266 device-only authentication requires success

The owner approved keeping the original lock cover when system authentication is unavailable on 2026-10-14. Only AuthenticationOutcome.Success authorizes device-only entry. Failed, unavailable, unknown and throwing responses preserve the original cover and queued destinations. The existing translated unavailable-device guidance explains restoring a device screen lock. PIN verification/recovery and the previously authenticated return grace remain separate and unchanged. No new stored data, permission, SDK or portable preference is introduced.

Actual Windows LockPage/native Invoke checks covered 18 language/theme/width contexts with an explicit authentication response port; a separate feedback pass checked the translated guidance. The independent Android package used the actual native adapter: NotAvailable after automatic prompting and Unlock, with Back unable to dismiss the cover and zero queued destinations opened. Neither review changes system credentials or native PIN storage. These checks do not accept configured biometrics, Windows Hello, physical devices or iOS.

An unrelated Home collection rendering COM exception remains observed during language/theme review. The lock assertions and unchanged financial fingerprint completed, but this is not a clean whole-application rendering result. SEC-04 remains in progress for its remaining platform and biometric transitions.


## D-267 complete Home balance publication

Observed Windows Home refresh failed in BindableLayout's ContentView handler disconnection while Balances.Clear published an empty intermediate list. Currency totals now use the existing SnapshotCollection complete replacement, preserving existing amount rows during ordinary and covered reloads. Currency calculations, inclusion policy, formatting and current-page retirement guards remain unchanged. The native regression checks original Home and original amount-handler identities, including a real modal cover; financial storage is not edited.


## D-268 sale-result help and truthful connection/account copy

The planned LOC-05 help sits beside an available realised holding result and explains recorded proceeds minus recorded average cost, unknown inputs and the absence of tax rules or official tax documents. The six existing languages include a concrete example. No tax calculation, financial posting, schema or new language is added.

LOC-10 corrects the browser-return message: it describes completion of the browser step and directs the user back to Zanance for the connection outcome, without claiming token exchange succeeded. English loan pay/receive confirmations identify the selected account explicitly, matching the original session argument and the other five translations.

Store drafts now name all six existing languages and describe optional password protection for local/connected backups, readable unprotected files, Android device-protected database keys and final system-backup/device-transfer exclusion. Real OAuth, physical-device, iOS and publication acceptance are explicitly separate; no store text was published.

## Source-qualified regional holiday choices (D-273)

Country remains separate from language, number formatting, calendar, currency and week start.
The existing Settings/onboarding regional view labels federal/shared-date choices directly and
shows coverage, source and reviewed Gregorian years. Unsupported years show an explicit warning;
never imply an empty unreviewed year has no holidays. Plan uses the start year and retains a
saved country/weekend mask after device preference changes. New countries extend the registry,
not the money model; no implicit conversion or payroll/bank-day assertion is allowed.

## Complete Home publication (D-275)

Cover the original dashboard and its actions until its complete first read or reload finishes.
Do not show No accounts from default values while an existing account is still being read.
Loading and failed-read states use growing translated captions and a real wrapping Home retry
button; successful retry runs the original complete read, never account creation or money posting.
An actually empty completed snapshot still offers the existing account setup. Preserve original
rows/handlers during the cover and distinguish this publication gate from app lock and performance.

## D-281 automatic local recovery points

Keep automatic device protection and portable password controls separate. The automatic card offers
daily/weekly/off, a growing translated status and complete question-mark help explaining device/key
loss and missed intervals. Portable status explicitly names portable export. Current-profile local
automatic rows use separate title/detail rows and a last-child real semantic button; they open the
original explicit preview/replace/cancel flow without a share action. Preserve the original growing
restore action row. Opening or translating the form never writes the interval. Subscribe only while
visible; retired pages stop receiving status events. All six existing translations are supplied.

## D-283 coherent Home source

Every Home section uses the same captured original financial reader. Keep the original account/budget/goal/rate
scopes, complete balances, history and action availability. Reading and key derivation stay off the interface thread;
only complete validated inputs uncover the original dashboard. A concurrent source/profile/rights change never
authorizes action from an unrelated file. Loading, failure, retry and formatting retain the existing UI patterns.

## D-285 captured layout and responsive Home

Apply the original saved section order from the validated financial snapshot before removing the
loading cover. Keep legacy device fallback only for null, and current default for explicit empty.
Preserve already placed section handlers. Background financial/recurrence work must not mutate UI,
translate on a worker or publish into a retired page; complete original results still appear together.


### Personal sync at first run (unpublished)

The explicit personal-sync edition offers receiving an existing service profile beside portable
backup restore. Keep the original wizard and its unsaved draft attached under a modal. Explain
that received profile preferences replace empty setup defaults; never manufacture another
financial account. Existing financial data is not an empty destination. Offer Back independently
of readiness, and explicit Use these accounts only after a complete applied round with accounts
and no unresolved review. Confirmation changes only local first-run state, preserves received
preferences and applies them through the original current-profile transition. New-space creation
belongs to the normal sync page, not this receive path. Native runtime acceptance of these new
controls remains pending in the complete personal-sync batch.

### Unpublished personal-sync linked financial groups

An original entry/holding GroupId is one financial version choice. Show one group card with each
member's complete local and service versions, including payment and fees. The action caption and
confirmation must explicitly select the complete group; never imply independent row decisions.
Keep original actual currencies/quantity units and translated member descriptions; do not expose
raw internal identifiers. A remote-only member is displayed and prevents selecting a silent local
hybrid. Stale edits invalidate the entire group and require fresh explicit review. Recording a
choice does not write money; the complete checked round applies it atomically.

### Unpublished portable record review values

Personal-sync choices retain typed values and original names instead of raw stored configuration
strings. Show a template's actual account currency and destination, category hierarchy/rules, saved
filter scope, exact source/target exchange rate, effective Home order/visibility, display-unit factors,
review steps and estimate period/currency. Saved forecast comparisons list each original day/balance,
including negative boundary amounts; empty scope/path stays empty. Malformed or unresolved changed
values disable consent. Native review checks must apply the same display-digit presentation as the
original labels and distinguish text presence from actual multiline/layout geometry. A displayed
account relationship changing after review requires a new decision.

### Unpublished optional attachment version previews

Personal-sync attachment choices describe the complete exact file version, including a displayed
deletion. Optional inline preview uses locally decoded images or one actual PDF page at a time,
with real page counts, previous/next, zoom/pan and Close through native semantic buttons. File name,
media type and byte length remain visible; no file path, opaque JSON or invented financial value is
shown. Locked/unsupported files remain explicitly selectable; renderer support is not consent.
Preview does not select a conflict, replace an attachment or run receipt recognition. Closing or
retiring the page clears its transient image, and changed data/lifetime refuses late publication.

### Personal sync native first-run and comparison qualification

The original first-run flow retains its fixed growing Back/Finish footer while connection content scrolls. Finish
requires actual received accounts; returning to setup preserves the original wizard. Comparison versions keep every
original typed field, complete direction-safe money packet and exact reviewed command parameter. Native actions
retain complete semantic names and at least 44-pixel targets. The independent Windows review passes en/fa/de,
light/dark and 360/412/wide contexts, including glyph bounds and actual first-run Back invocation, with complete
financial fingerprints unchanged. Prior independent Android native evidence remains separate from phone/screen-reader
acceptance. The Debug route uses its own compiled installation identity and never owner credentials or desktop input.

### D-293 - Local and hosted personal-service trust

The approved hosted default uses normal system chain/name validation and an empty fingerprint. Local development
addresses retain explicit trusted fingerprint verification. Explain both in all six languages and state that the
personal service can read connected records. No automatic opt-in or network work follows merely opening the form.
Original Windows36 and Android12 contexts plus native18/6 help dialogs preserve complete text, Back and finances.

### Household flow under development (D-294, native rendering not yet accepted)

Household setup stays in a separate empty profile and preserves the original profile/Add/open and first-run wizard.
The complete local initial read owns its cover; account/session defaults never imply verified membership. Keep
private invitation text in the explicit form, clear visible capability/password on retirement, and explain that it
alone grants no data access. Compare complete owner/device fingerprints independently before admission. Show the
last verified role and separate financial-write state; local permission hints cannot authorize a later write.

Use original growing semantic buttons, native scaling and palette resources. Pending claims and signed members
retain their exact original command parameters. A new member action cannot replace retained original administration;
Resume is explicit. First-run Cancel/Finish have fixed growing rows outside the scroll area; Finish changes only
local setup state after actual connection, without creating another account. All six existing languages include
full privacy/role/grace help. Original app-flow and native backend evidence do not qualify the new rendered page;
complete en/fa/de, themes and phone/minimum/wide native checks remain part of the same unfinished batch.

### Actual household cost form (D-294 in progress)

Cost shares allocate actual expenses/refunds; they never imply joint-account ownership or create another cash
posting. Show cost attribution, private advances and shared/private payment totals separately per actual unit.
Enter exact shares with field-local sum/reference/amount errors. Linking cash is an explicit bounded date/title
search with the real selected movement retained; never pick a first result automatically. Refunds select an
original cost, repayments select actual sender/recipient and an optional exact transfer side. Contact archival
retains history and does not revoke service membership. Currency changes clear numeric drafts; live translation
preserves original input culture and selection identities. Save/Cancel/Delete reserve growing rows outside the
viewport; a new draft has no Delete permission. Long Windows titles use the production growing PageHeader.

The original cost form has54 new fully translated keys, no new language. Eighteen corrected own-window en/fa/de,
light/dark,360/412/wide read-only-history cases preserve every stored row; original writable app-source Save is
qualified on actual protected household storage. Android/native household setup and complete batch UX remain open.

Household source selection (D-294 in progress) uses original unchecked account controls, independent history/file
choices, complete wrapping account/currency/count disclosures and explicit separate-destination consent. A fresh
destination visit confirms the original detached copy; pending copies block publishing. Explain returning to the
same destination after process loss and never imply cancellation deletes profiles/keys/money. Native Windows
selection/review passed18 language/theme/width cases with full fixture restoration; destination and mobile flows
remain separate gates. Recovery pickers retain only bounded encrypted files/public Save receipts across a lock
modal; passwords and foreground grants are retired. The common household conflict page reuses whole-group and
inline-file review while hiding/refusing readable personal connection actions; authenticated native review remains
open. These controls do not manufacture member authority for layout verification.

Original source/profile creation now has functional main Windows acceptance at412/light with actual native
consent/selection/menu/prompt and real encrypted publication. A user-edited connection label pre-fills the original
new-profile prompt; an unchanged personal label does not silently become its new default. The prompt stays
editable and requires explicit Save. Existing target choices use quoted names plus stable reviewed ordinals,
without exposing internal ids. Native flyout and ContentDialog are different surfaces; qualification must invoke
their original named providers without desktop input/focus, not assume a single dialog class. This functional case
does not replace the existing18-case source-control layout matrix or final household/mobile dialog acceptance.


Household device administration (D-294 in progress): complete wrapping public device fingerprints retain real growing approval/removal actions. Current/sole-device removal is unavailable in this flow. Show the current verified owner root after rotation. A visible retained-invitation hint and explicit confirmation finish only the original protected intent, even without clipboard text. Eighteen actual Windows language/theme/width cases and the native current-root/Finish action passed; main mobile remains open.


D-294 device access: label public existing-member device access separately from new-member invitations and recovery kits. Explain separate empty profile, same identity, independent full root/device comparisons and original-request continuation. Disable a new request on a bound ledger. Main Windows export/request/Cancel/admission/cold and18 language/theme/width checks of the changed hint/request/export controls passed; actual owner text entry is not qualified on hidden Windows and remains an Android acceptance gate.


D-294 ownership: choose one actual accepted identity/device with an independently compared complete fingerprint and
an explicit former-owner Leave/Viewer/Editor disposition. Show original stages as waiting/explicit next-device
approval, never auto-finish another person's step. Separate Refresh, consent, Cancel and terminal Finish. Explain the
publication pause, new independent ownership keys, old kit retirement and irrevocable downloaded history in all six
languages. Keep growing native actions and exact retained lost-response continuation. The actual no-pending status
card/Refresh passed18 new Windows language/theme/width cases; transfer prompts/other stages/Android remain open.


D-294 ownership continuation: the target's explicit Offered-stage approval first adopts the exact frozen incoming financial horizon, then prepares ownership keys. Downloaded ciphertext alone is not applied finance. Unsent local rows/conflicts remain for explicit resolution; no automatic financial overwrite or outgoing rotation grant is inferred. A retained candidate and an already applied clean horizon resume without replacement keys or another empty round.

D-294 remote lifecycle UI keeps service UTC deadlines separate from financial dates/calendars, explains 30-day read/export plus90-day retained time and distinguishes permanent access revocation from current physical cleanup. Owner deletion requires explicit irreversible confirmation and warns that downloaded local copies/portable exports survive while unsent work cannot be published. Original uncertain consent stays visible and blocks fresh writes; only authenticated unaccepted expiry permits explicit finishing. Activity pages verify signed membership/publication evidence against trusted local history; actor/time and unsigned operational events are labeled service-reported. Eighteen new native Windows Active/no-deletion status layouts passed; no destructive prompt, other-state or mobile acceptance is claimed.
The subsequent original main Windows lifecycle/audit status review completed18 new language/theme/width cases (fa/en/de, light/dark,360/412/1280),54 own-window captures and7992 native observations. Actual native Discover, lifecycle and audit reads preserved every financial column. Initial hidden-page publication/disabled-peer failures were retained; no protected file/key was reset. A bounded publication check and explicit independent ApplicationId build preceded completion; the broader lifetime cause is not proven. This is actual Active/no-deletion status acceptance only, not destructive confirmation, other lifecycle states, audit history completeness or Android acceptance. Strict current Windows and Android builds passed with zero warnings/errors;23 resource completeness/literal-usage cases passed. Successful earlier service/native/suite cases were not replayed. The whole household batch is still unfinished and uncommitted.
D-294 retained-time privacy: the Delete action reviews only owner-scoped opaque checkpoints before confirmation and rechecks the exact checkpoint afterward. It must not fetch financial data merely to permit owner deletion during retention. Same-device session possession can reattach for status/privacy metadata without restoring financial/key access. Existing trusted owner root availability remains mandatory; changed/missing roots are not adopted from server metadata. Full main destructive-confirmation and mobile acceptance remain pending; prior18 status-only layouts are not broader evidence.

### D-294 privacy observations and retired service data

A negative authenticated service observation takes priority over an earlier positive grant snapshot. Disable sync and remote audit data actions after explicit refusal or original deletion consent; owner lifecycle and privacy metadata remain independent. A read-only grace period still permits its authorized read/export path. Do not turn a successful lifecycle-status request into renewed financial or key authority.

Once permanent deletion is observed, hide future automatic-retention deadlines that no longer describe the room's outcome. Keep the original pending/accepted/retired receipt visible and preserve local copies. Native en/fa/de confirmation semantics, real button targets and cold original receipt were checked on the independent Debug installation; final changed-state layout/control verification remains separately recorded in the canonical household delivery plan.

D-294 final changed-state follow-up: both strict builds passed. The changed Windows deleted-state layout passed18 cases with2,844 native observations and unchanged local financial columns. The new signed complete Android Debug binary then reopened the original retired receipt/local ledger, rendered no obsolete automatic deadlines, disabled the actual audit button and kept lifecycle metadata enabled. Original operation/hash/time and local money were unchanged, without another deletion or receipt request. This verifies the changed display/control gate only; six earlier native confirmation cases and unchanged active layouts were not repeated. Whole D-294, final Release/AOT, physical-device and iOS gates remain open. Canonical details and immutable evidence identities are in enhancements/2026-10-commercial-release/11-household-delivery.md.

A refused household reconnect retains the validated selected local context before contacting the service. Existing
financial rows must never acquire empty-profile setup/recovery actions merely because404/403 prevented rebind.
Retained context alone grants no owner role, key or synchronization. Native local Reload can publish the cached
read-only state after that failed explicit action; display-language/theme review must not repeat the refused rebind.
Main ownership confirmations compare the exact independent recipient device and state the chosen former role and
old-kit invalidation. Never capture a private invitation editor beneath a confirmation (D-294 in progress).

A warm shared page must publish a transport-recorded local refusal before enabling input again. Do not keep
Sync/admin/audit active from an older positive hint, retry HTTP to close the page, erase the selected household
context or renew access through privacy metadata. Keep downloaded rows/keys and original pending work; show the
translated original failure and readonly status. The last verified owning identity is distinct from live device
administration. Native competing-cost decisions choose the complete cost/allocation group, never individual shares.

### D-294 native household confirmations

Retain original native dialogs and explicit consent. Windows action-sheet/prompts/confirmation buttons grow to
at least44 units through an operation-scoped popup observer, removed on dismissal/failure. Copy the original
root page direction into native ContentDialog because MAUI prompts do not inherit it consistently. Do not replace
native commands, simulate confirmations, activate the desktop window or manufacture a protocol role for layout.
The18 complete dialog cases and six subsequent affected RTL prompts preserve the original financial dataset;
see11-household-delivery.md for exact geometry, images and retained negative evidence.


## D-295 original store choices and identity

The original plan comparison remains readable while a separate store surface loads. Display only actual provider
price packets and every introductory/renewal phase. Before native launch, show the original selected terms and
subscription replacement timing/date in the current display calendar. Recheck those exact terms after consent;
a changed offer returns for review without a payment. Keep every native action growing, semantic and at least44px.

Pending/uncertain original work offers Continue, never another Buy. A failed read offers a read-only retry, not a
fabricated pending purchase. Missing private state exposes explicit Restore; Cancel cannot create a key, associate
an identity or start payment. Anonymous Plus remains account-free. Optional identity registration/sign-in/recovery
is separate from financial accounts; clear typed credentials on submit/retirement and show adjacent validation.
An explicit restore explains optional association; Lifetime warns that another subscription is not canceled.
Native own-window and changed main Android evidence is in enhancements/2026-10-commercial-release/12-billing-delivery.md.


## D-296 explicit accountant disclosure

Keep Germany preparation separate from display/holiday country and later tax calculations. Default the explicit
Gregorian year, initially select no financial accounts, keep notes/document copies off, and explain complete
transfer/counterparty disclosure before review. Field errors appear together. Changing any choice retires the
original review; native confirmations never save money. Keep complete scalable summary/warnings, a short native
page title and a pinned growing Review/Share row. State that the ZIP is readable, instructions are English and
stored copies may not be legal originals. An actual Cancel must precede cache/authentication/share effects.


## D-297 Explicit bank-software CSV review

Keep the StarMoney preset separate from generic mapping. Encoding applies to the next picked file and is never
guessed from language/currency. Show selected local account/currency and only masked source ending, every
proposed row (25 per page), original remittance/payee/value-date/category references, duplicate/error states and
before-opening warning. Acknowledge posted ordinary income/expenses explicitly; transfers/refunds/reversals,
pending and splits are excluded. Invalid rows block the whole preset. Real native final count/account consent,
Cancel and durable batch Undo retain their original meaning. Departure/account change invalidates consent;
source/history changes require fresh review. Growing captions/native44-unit actions preserve all six languages.

## D-298 Explicit optional aggregate disclosure

Select no accounts initially; confirmed-only starts on and previous-period comparison off. Initialize both dates,
show all field errors together, and invalidate an old review on every scope edit. The complete exact aggregate
JSON has its own native read-only scrolling viewport and readable currency-separated summary. Manual sharing
states readable-recipient retention, no guaranteed answer and no built-in credit use. Native Cancel precedes
sharing/authentication effects. Clearly show when a provider is unavailable; no automatic network or sign-in.

Metadata-only provider check and original offer precede per-request native recipient/model/max-unit consent.
Fresh available/reserved purchased/gifted balances are distinct from cached historical result balances. Retained
historical consent includes the original destination, provider/model/max units and exact packet; continuation
cannot silently use current data or new terms. Explain status-first identical absent-id replay and unaccepted-id
closure; accepted uncertain work remains pending, without a guessed refund. Keep full model output read-only
plain text with a visible fallibility warning and no generated financial commands. Navigation/cover/profile
retirement refuses late publication. Identity selection uses the existing optional identity page.

## Original market reference review (D-299)

Reference lookup is separate from manual rate drafts. First appearance reads only the original file's retained
cache, local asset definitions and saved history. Require explicit source metadata/fetch, symbol/date and asset
choice; no default asset, silent form fill or sign-in. Show unavailable/error/manual fallback, full original
source/licence/revision/quote identifiers, exact source unit and amount, observation/retrieval/freshness/retention
and effective target date. Native confirmation keeps the complete original converted value, purity/item consent,
same-date replacement and non-payment limitation. Aged references need their own explicit acknowledgment.
Growing footer actions stay outside the scroll viewport; complete asset/history captions have separate labels.
Saved history pages remain read-only, distinguish exact-current from changed/deleted targets, and never supply
new financial consent. Invalidate review across scope edits/cover/retirement; recheck actual original membership
and target at commit. Native Cancel and source identifiers are qualified without a live provider.

## D-300 Covered startup semantics retained

Measured Android AOT compilation changes no layout, captions, lock, retry, protected preparation or Home publication semantics. A ready Activity is not a loaded overview. Retain the cover through storage/access checks and report actual Home readiness separately.

Current exact evidence: enhancements/2026-10-commercial-release/17-startup-responsiveness-delivery.md.

## Growing searchable currency inputs (D-302)

Currency code, editable search and list affordance remain complete at native text size. Give the list a real
44-unit button and original live translated field name. Currency rows retain a real action, original code
identity and translated selected state. Keep the vendor's original filter/selection rather than another search
algorithm. Reserve a whole row for each amount/currency input in constrained transaction, goal and rate groups;
never fix currency width to 110, shrink fonts, hide errors or replace the original draft/control.


## Original reviewed goal lifecycle (D-306)

Completion/reopening, pause/resume, archive and restore change a detached complete goal draft. The original
reviewed packet stays unchanged through native confirmation, writer validation and rejected Save. Keep the
busy guard across confirmation/reload, preserve the unsaved amount and retain existing state-specific actions.
An altered reviewed plan/goal packet requires reload; it cannot become a valid new displayed source or writer
review merely because the database is unchanged. Existing captions, layout and disclosure remain unchanged.
Native en/fa/de light/dark command/confirmation evidence is in enhancements/2026-10-commercial-release/23-reviewed-input-integrity-delivery.md.

## Complete named filter actions (D-308)

Saved query names are user text and may consume their full allowed length. They wrap inside growing rows
of the existing bounded filter viewport, preserving the result/action docks. A decorative caption surface
keeps the original scalable typography/colors; its last real native button owns the unchanged query, argument
and full spoken name. Horizontal overflow is not a substitute for a readable saved-query action. Native
selection remains read-only. [Verification](quality/saved-filter-actions-readable.md).

## Verified purchase history alongside offers (D-310)

Show owned purchase lifecycle in growing full-width title/detail rows on the original plan screen, separately
from offers and current effective access. Preserve native scale, wrapping and existing pinned Continue.
Explain saved verification and its refresh requirement. Label the original paid-through/grace-through date;
never infer a next charge from it. Auto-renew reflects the last verified provider fact, and Lifetime has no
invented renewal date. Keep each device/identity history horizon intact; never merge conflicting historical
fields or expose raw account/purchase/receipt identifiers. Current regional calendar formatting stays live.

## Complete report/snapshot table values (D-311)

Alternative-to-chart financial tables use a wrapping identity/date row and a separately labeled complete
monetary packet per row. Reuse AmountReadout for oversized signed values and its translated horizontal-scroll
hint; never split digits/decimals, shrink the old body size or omit currency/sign. Keep total emphasis and
existing semantic colors. Trend income/expense/result and snapshot saved/actual/difference captions repeat
with each original row rather than assuming hidden or clipped shared column headings. Native scroll ends
must reach the original sign and unit; this is presentation only, not a new report calculation.

### Selectable device-only pre-restore safety copies (D-312)

Backup displays previous-state safety candidates separately from automatic copies and portable exports.
Use growing translated title/date/size rows and real complete named targets. Explain device binding and
verification before replacement; never imply a matching listing header is authenticated content or offer
sharing. Selection uses the original protected restore and explicit replacement confirmation.

## Stable first-plan publication (D-318)

Plans keeps the actual collection/row template and persistent bottom Add target. Empty feedback has its own growing scroll viewport controlled by the existing empty state, avoiding Android empty-adapter exchange. The optional first-plan Add caption uses WrappingAction: preserve full translation/native scaling and the original real command-owning target. No segment toggle or extra Save is required to reveal a newly saved first plan.

## Existing loan terms after repayment (D-319)

A zero outstanding balance stops the repayment estimate, not the existence of stored terms. Show the existing Add-terms instruction only when installment configuration is actually absent. Keep original balance/history/manual transfer/reminder access and genuine missing-data guidance; never infer a missing rate/installment from unavailable estimates or paid entitlement state.


## Receipt OCR accent evidence (D-325)

Latin accent differences may match a complete known total/exclusion phrase, but the existing review card keeps the actual OCR source. Never silently rewrite digits, currencies or spelling to populate a receipt amount. Missing/conflicting evidence keeps manual entry and explicit Save/Discard.


## Filtered transaction summaries (D-326)

A nonempty filtered list shows its count. Add the total caption only when the original income/spending result has a currency packet. Transfer/adjustment/asset movements never acquire a synthetic zero result; a real currency sum of zero remains visible. Reuse the existing six-language count text and MoneyText packets. Keep the original rows, filters and navigation unchanged. See [native and application evidence](quality/transaction-summary-result.md).


## Automatic-copy readiness (D-327)

A protected safety-file list may briefly own native maintenance. Automatic readiness waits asynchronously and preserves cancellation/profile checks; it never blocks the page or treats that expected overlap as a failed creation. After a successful validated not-due check, show the existing last-success time rather than stale transient failure feedback. Do not manufacture a new copy or change its timestamp to clear the message. See [physical and focused evidence](quality/automatic-copy-readiness.md).


## Continued PDF tables (D-329)

Keep section/account identity, exact period/currency/account scope and column names in the repeated table header. A continued balance must remain identifiable without turning back a page. Retain RTL mapping and numeric packets. Fixed A4 rendering does not depend on app viewport/theme; qualify actual native exported pages, not just the chooser or extracted text.

## Interpreted import rows (D-331)

Counts alone do not satisfy IO-09. Keep every source row reachable in bounded read-only pages, including
invalid and known rows. Give interpreted date/kind, financial identities and each complete currency packet
their own growing rows. Source and received amounts are distinct. Show inclusion/skip status explicitly;
mapping changes retain the old review visibly but block import until refresh. Paging never saves money.
See [qualification](quality/interpreted-csv-preview.md).

Next/Previous source-page navigation must bring the selected page's review heading and first row into
the actual viewport (D-332). Do not retain the previous bottom offset or move the viewport for ordinary
status/duplicate updates. A retired page must ignore a queued arrival callback. Qualify native arrival
before an inspection scroll can hide a misplaced position; [evidence](quality/android-import-preview-arrival.md).

## D-333 notification action continuity

A snooze can start the process while the application stays closed. Preparing device storage must not
open a page, remove an app-lock cover or change a plan's due date. An unavailable protected store must
not erase other valid queued reminders. A snooze disposed during preparation stops before reading or scheduling. Existing
notification-detail consent and lock-protected navigation remain. See [evidence](quality/cold-protected-reminder-startup.md).


## D-335 complete menu explanations

More menu titles and explanations wrap completely in growing rows, including sync/household entries.
Do not inherit compact financial-row ellipsis, cap the explanation at two lines or shrink its font.
Keep the original whole-row semantic button, full spoken title/hint, route and readable wide column.
Native Windows200% stress is separate from OS/device/reader acceptance; [evidence](quality/more-menu-readable.md).

## D-338 screenshot fidelity

Render actual UI with fictitious data, preserving original values, warnings and scroll viewports.
Await nested budget publication and shell replacement. No retouching, invented mobile frames or
device acceptance claims. No shipping layout, theme, binding, command or financial algorithm changed.

## D-339 independently growing report legend rows

Category/composition report names, shares and monetary packets occupy independent growing rows.
Names retain native scaling and complete wrapping; oversized amounts use the existing horizontal
readout, outside the original category navigation button. The 18 final Windows contexts (en/fa/de,
Light/Dark, 360/412/wide at process-local 200%) and six original Release/AOT Android contexts pass
native glyph/viewport/scroll/target checks. Fictitious presentation inputs are explicitly separate
from report calculations; no financial commands execute. Strict builds and the full offline signed
APK pass. The Android caller's initial startup ANR is retained: Wait continued the same original PID,
without restarting its scenario. QA-06/ANR, broader A11Y/OS readers, physical Android, iOS and release
gates remain open. Managed test counts are unchanged; unrelated passed suites were not repeated.

[Evidence and limits](quality/report-legends-readable.md).

## D-340 complete inherited row identities

The remaining inherited RowTitle ellipsis truncates the original Non-monthly bills caption at
200% Windows process-local scale/360 width. The shared default now wraps with its original 15-unit
font and semantic color. All 26 other current consumers already explicitly wrap, so their effective
behavior is unchanged. Eighteen final Windows contexts/36 native captions and six Release/AOT
Android contexts/12 captions pass; original account/entry/budget inputs stay unchanged and no method
is saved. Strict builds and a complete signed offline APK pass. The independent Android startup
ANR remains an actual failure; Wait continued the same PID 1753 to its completed caption result.
Actual OS text scale/TalkBack, broader device/iOS/performance and release gates remain open. No
system setting, financial algorithm, schema, translation or managed unit count changes.

[Evidence and limits](quality/budget-group-titles-readable.md).

## D-342 growing optional Home category legends

Category identity and percentage use independent wrapping rows. The unchanged amount packet uses
AmountReadout outside the real category navigation target; the total receives the same full-width
viewport below the chart. Preserve original scalable sizes and complete spoken identity. Detach
native category rows while their optional section is hidden without filtering the original collection.

[Native evidence and remaining acceptance](quality/home-category-legends-readable.md).

## D-343 independent plan-row value viewport

Plan identity/subtitle and status stay in growing rows beside the original date/icon. Reserve a
full-width AmountReadout below them using the unchanged MoneyText packet, scalable15-unit bold font
and semantic amount color. The real original navigation button covers only identity/status, retains
its complete description and44-unit target, and never covers the amount scroll surface.

[Evidence and limits](quality/plan-row-amounts-readable.md).

## D-344 complete ledger/account monetary rows

Ledger and account identity/status keep growing rows beside their icon; original real navigation
or selection targets cover only identity/status and retain44-unit targets. A full-width AmountReadout
below them preserves complete sign/digits/currency and semantic colors. Day-net and account-group
headers preserve all original currency packets at existing secondary typography. Selection highlight,
checkmark, review dot and native account-details handler stay intact.

[Evidence and limitations](quality/ledger-account-amounts-readable.md).

## D-345 - Real OS scaling remains distinct from Debug stress

At actual emulator font_scale2.0/412dp, six original ledger/account amount surfaces pass complete
native allocation/scroll/target/color/selection checks across en/fa/de and both themes. This is not
all-controls,360px or phone acceptance. TalkBack binding and low-confidence captured audio do not
prove spoken navigation. Restore captured emulator settings after the authorized test; keep the
failed reader attempt open. [Evidence](quality/android-os-accessibility-qualification.md).

## D-346 actual OS qualification boundary

Original scalable financial surfaces gain real emulator OS200%/412dp evidence with unchanged
stored inputs. Retain completed cohorts and actual native/ANR failures separately. No serial-GC
workaround is promoted after an ineffective trial; speech controls do not establish TalkBack app
acceptance. App/APK remain D-344; broader A11Y-01/03, QA-06/07 and release gates stay open.
[Evidence and limits](quality/android-os-accessibility-qualification.md).

## D-347 actual foreground review boundary

A native layout under Android's ANR cover is provisional geometry, not unobscured-window acceptance.
Require actual Activity focus and reject retained receipts lacking that proof. Preserve completed
groups and actual failures separately; the ineffective precise-Tarjan trial is removed. Main/APK
remain D-344 and broader accessibility/runtime/platform gates stay open.
[Evidence and limits](quality/android-os-accessibility-qualification.md).

## D-352 reimbursement results and empty flow

Large reimbursement lists have one bounded CollectionView with a scrolling introduction/total header.
Identity/date grow above the independent AmountReadout; the real original button covers identity only.
Use existing scalable row typography and refund semantics. Prepare the entire snapshot and currency
totals before one Reset, keeping loading/failure covered and offering the real translated retry.
Reject stale native parameters after reload. Windows Header/EmptyView can overlap: keep the empty
introduction/card in one separate sibling ScrollView, mutually exclusive with results under the same
ready gate. Qualify actual visible vertical edges, complete horizontal endpoints and intro/empty
separation; allocated bounds alone do not prove readability. No system setting changes are needed.
[Evidence and limits](quality/reimbursement-page.md).

## D-353 Home template publication

Reload the complete template strip atomically with fresh presentation records. Preserve unchanged native
controls and complete current spoken captions/commands; support real reduction to zero and restoration.
No visual redesign or financial activation accompanies this lifecycle correction.

## D-355 Android native picker captions

The closed native Picker must show the complete selected value at inherited OS text scale. Allow
wrapped growing captions, preserve native dialog/selection and editing restrictions, and do not
replace accessibility scaling with smaller type. [Native evidence](quality/android-settings-os-text.md).

## D-356 intrinsic choice captions and debt disclosures

A decorative Border must not subtract its stroke from the chip caption's measured width. Keep
padded text as a sibling over the outline and the real button last, preserving selected semantics.
Both optional debt disclosures grow with complete scalable captions through WrappingAction/Text.
The default-icon dash is a decorative glyph; its real button retains the translated default/selected
name. [Original-form evidence](quality/android-editor-os-text.md).

## D-358 complete measured choice surfaces

Original choice chips do not shrink after intrinsic measurement: the complete surface wraps
as one unit. Preserve scalable captions, original padding, selected names/colors and real
buttons. A fractional Flex reduction can otherwise create an unmeasured second native line.
[Actual OS and Windows evidence](quality/work-editor-os-text.md).

## D-360 retired Android navigation trees

Display-driven root replacement releases the retired root's native fragment ownership while
preserving active forms, native Back commands and translated descriptions. Keep the queued initial
add/removal in reordered transactions; never dispose active widgets or another root's fragments.
[Scoped original-navigation evidence](quality/android-retired-navigation.md).

## D-364 minimum-width editor and wizard actions

Icon and rule disclosures use the existing scalable WrappingAction surface with the unchanged
command and complete translated native name. Center decorative tiles beside growing text.
The wizard reserves its footer outside the scroll body; whole Back/Next/Start surfaces wrap
instead of splitting words in fixed half-width buttons. Retain first-step visibility and busy
disablement. A single-line editable value may scroll; verify its actual caret and glyph endpoints
before treating horizontal overflow as a static-caption failure.
[Native evidence and limits](quality/narrow-editor-actions.md).

Opening the wizard copies its active theme without publishing a new theme change. A genuine
choice still previews immediately; constructor publication must not retire the pending modal owner.

## Decorative reader subtrees (D-367)

Exclude decorative SymbolIcon descendants as well as their parent. Native glyph children can
otherwise expose private characters to TalkBack. Keep the complete meaningful label and real
button outside the excluded subtree. Require actual reader focus and original page transitions,
not just gesture callbacks or node metadata. Six scoped original navigation contexts pass;
correct audible speech and remaining platform/reader acceptance stay open.
[Evidence](quality/android-reader-glyphs.md).

## Owned Android currency-button lifetime (D-369)

A retiring currency input releases only its own chevron child on Android Unloaded, before its
retained native group can be disposed. Loaded restores that same child only while the original
DropDownButtonSettings ownership matches. Preserve the real button/name/44-unit target, filtering,
selection and scalable typography. Never suppress vendor finalizers, keep retired pages alive or
remove another owner's children. Actual popup windows can be non-focusable: an active-window
accessibility dump alone cannot prove their absence. Require owned visible native windows and
unchanged selections; row-selection/speech/device acceptance are separate.
[Native lifecycle evidence](quality/destination-fee-android-lifecycle.md).

## D-374 - Truthful global review notices

The attention item and confirmed-balance explanation show one transaction for one FX transfer,
even when both currencies affect included accounts. Currency-specific counts/amounts stay separate.
Preserve original action, scope, scalable captions and language/region/digit choices. Native review
checks compare the actual display-digit mapping while the bound count stays Latin; do not change
product formatting to satisfy an incorrect observer.
[Original page qualification](quality/home-review-count.md).

## D-375 - Home Add shares the title row

Place Home's real 48 px plus at the physical right of its scalable growing title, including RTL. This
single header replaces the native page title bar; do not add another title band. The content viewport
fills the remaining height down to the tab bar, without bottom Add row, overlay or reserved padding.
Keep the original command, translated description and loading/failure guards. Other page docks and
financial typography remain unchanged. [Native qualification](quality/home-add-header.md).

## D-376 - Truthful getting-started completion

A step is completed only by its named saved fact: first Expense, recurring payment plan and Month
budget. Income, transfers, refunds, Once plans and weekly/fortnightly budgets leave their unrelated
steps visible and actionable. Hide the card automatically only when all three match; preserve explicit
dismissal and account guards. Do not alter the user's creation defaults or existing financial data.
[Original application/native evidence](quality/home-getting-started.md).

## D-377 - Preserve complete Home while reducing materialization work

An empty-tag read optimization must leave all original sections, amounts, order, commands, loading
cover, title action and native text/layout unchanged. Compare the complete original financial/native
presentation, not a reduced sample page. Bounded original navigation timings do not establish cold
startup, responsiveness or general native stability.
[Qualification](quality/home-empty-tag-materialization.md).

## D-383 - Preserve original recent-entry presentation

Use the existing newest-first snapshot and worker to select three nonfuture original records. Keep
original row identities, ties, presenter, amounts, review state and dates. Do not filter financial
history or change loading cover/layout to improve timing. [Qualification](quality/home-recent-selection.md).


## D-385 - Complete bounded CSV review

Keep the last complete selected filename and review when a replacement is too large, malformed,
empty or fails analysis. Explicit feedback says the prior source remains; an already accepted result
must stay accepted and cannot become importable again. Keep picker interaction native, prepare reads
and matching off-window and publish complete values only for current profile/mapping. Never silently
truncate financial inputs or change a stale generic interpretation without explicit Refresh.
[Original running-page scope](quality/csv-input-safety.md).


## D-387 - Preserve CSV financial meaning at quote boundaries

Incomplete or misplaced CSV quoting is an error, not an opportunity to guess an amount or repair a
source. Accurate translated feedback preserves the previous file/review. Valid escaped quotes,
whitespace, multiline and Unicode text retain their original meaning. [Evidence](quality/csv-quotation-integrity.md).

## D-389 daily source precision and free-original access

Daily ECB references show the market date separately from feed retrieval and HTTP document modification.
An unavailable fixing time is explicit; never render a fabricated intraday timestamp. Reverse/cross
references are labelled calculated with original legs/formula. Cache/history/review keep the source,
free-original disclosure and exact consent; opening these views cannot fetch or update finance.
The plan page and native pre-payment terms disclose that the original ECB data is free. The source
button uses the existing growing caption and a full external browser, with no framed source/logo.


## D-392 retained aggregate editing and conservative Undo

Simple mode hides new advanced setup, not the complete existing value: an existing aggregate opens
its original checked option and covered dates even without another detail field. Publish capability
after loading the whole range and retain the switch during unsaved off/on changes. Ordinary Simple
entries remain unchanged. Undo requires immutable original imported metadata even without aggregate
linking; edited/attached/extra rows or missing legacy evidence stay for review instead of deletion.
Original native confirmations and six-language conflict feedback remain.

## D-394 - Account values after tab navigation

Reactivating a retained account after a transaction edit must refresh its covered snapshot even
when the platform omits another OnAppearing callback. Share pending reads, retain the same page,
and preserve unsaved reconciliation amount/reason/date. Never require a manual refresh for newly
saved money or issue another financial write. Native digit conversion remains display-only.

## D-395 - Consistent Plans title action

Plans mirrors Home: a growing title and real48px Add button on its physical right replace the native
title and full-width lower action band. Preserve translated spoken name and the same command. The
optional44px Back target retains original navigation when pushed. Segment chips/warnings remain
above the full remaining list/empty viewport; the empty-state invitation remains inside its content.

## D-396 - Reconciliation belongs to the current review

Changing observed amount or date removes the old comparison/hints/adjust action immediately, while
retaining typed drafts. Compare again before adjustment. Require a reason and original native consent;
disable form input during the operation. Cancel retains the draft without posting. Changed original
account/history/access requires a fresh comparison with six-language feedback. A known committed
outcome remains successful even if refresh notification fails; never invite a second financial write.

## D-397 - Accepted advance bills offer only Back

Keep the settlement form covered while its original plan/account/history snapshot loads, with
explicit translated failure/retry. Pre-write rejection preserves the draft and field explanations.
After acceptance, preserve a complete fixed-footer success notice independently of follow-through
errors, make the original inputs read-only and relabel both original exit actions Back. A return
retry never invokes the writer. Keep complete scalable result/error captions and44px native targets.

## D-398 - Keep normal Home refresh races covered

If saved sources change during preparation, rebuild the complete same-profile Home while its
existing loading cover remains. Publish shortcuts/layout only with the current complete result.
Bound rereads to three attempts; repeated edits or genuine failures retain the existing translated
failure/Retry surface. Never show a stale intermediate packet as ready or silently switch profiles.

## D-399 - Truthful saved occurrence feedback

A successful payment/link/skip/change/reopen remains saved when its notification fails. Retire the
old occurrence before refresh, Back or feedback; a partial payment reloads the actual remaining
amount before another deliberate action. Keep the native saved warning separate from rejected-write
feedback and hold the command gate until it closes. New warning copy is complete in the six languages.

## D-400 - Retained occurrence corrections

A retained form does not grant authority to replace a newer accepted settlement or skip. The original
failure adapter reports the refusal while the same draft remains; Busy spans its native message.
Return and reopen to review current state. No layout, caption or automatic draft reload changed.

## D-401 - Refresh retained occurrence suggestions without replacing input

Returning through an actual Shell tab refreshes the original occurrence's suggestions under its
loading cover. Retain raw payment amount/date, occurrence amount/due date/note and expanded state;
never briefly assign defaults to native date controls. Hide writable fields until the complete read
is ready. A failed read keeps the draft covered with translated Retry; a changed account/destination/
kind/currency requires closing and reviewing again. Do not interrupt pending financial consent or
its busy gate. Only a known accepted command's canonical reload resets payment defaults.

## D-402 - Original refund workflow qualified on the phone

Retain the existing purchase link, same-currency destination choice and visible remaining amount.
Refunds reduce spending rather than becoming income or transfers; prior-period purchases stay in
complete history. The original D-130 refusal stays beside Save with its draft intact; correcting
only the amount can complete the exact remaining refund. No new design or caption is introduced.
Bounded physical/cold public evidence is in quality/physical-refund-lifecycle.md.

## D-403 - First-run portable restoration observed on the phone

The original Welcome restore action precedes first-account creation. Its native file choice, complete
package preview, cancelled/accepted replacement and restored Home retain the original account rather
than forcing a duplicate. The independently reinstalled original Release preserves its skipped-plan
history and portable Monday choice after one cold read. Password protection defaults locally again;
portable display preferences do not carry that device-only choice. No layout/text/command changes or
unchanged visual matrix reruns. See quality/physical-portable-fresh-install.md for the bounded evidence.

## D-404 - Original archive and positive card credit observed on Samsung

A card payment remains a transfer even when it creates positive credit. Original archive consent names
both source/destination-dependent plans, warns about dirty draft discard and lets Keep editing retain
that draft. Accepted archive uses stored identity, retains history/report spending, ends both plans
and explains default clearing. Original unarchive returns the active card without resuming ended plans
or choosing a default; the same state survives one cold read. No new UI/source change or layout matrix
is needed. See quality/physical-credit-card-archive.md for bounded public evidence and restoration.

## D-405 - Retained source with explicit fresh financial review

The existing changed-preview feedback now also explains changes to ordinary CSV account/history facts.
Retain raw source, mapping and overlap choices on refusal; disable stale Import and keep explicit
Refresh available. Leaving the form retires its original token. A rejected financial review does not
show an unexpected-error dialog or guess a new opening baseline. Existing wrapping/error/action dock,
translations and native scaling remain; only this original command/refusal/refresh path is qualified.
See quality/original-csv-financial-review.md for en/fa/de/theme native scopes and observer limits.

## D-406 - Retained Shell import departure

A routed More page can leave the visible Android tab without Page.OnDisappearing. Retire the original
CSV financial review on uncancelled Shell tab navigation after existing leave guards; keep raw source
and choices. Import stays disabled until explicit Refresh. Never interpret a retained native page as
a current financial review. Physical final Release qualification writes no money for this hook.


## D-407 - Reviewed split shares and retained acceptance

The original split modal publishes only its coherent financial source and choices before editable
shares. A changed source keeps raw amounts/categories with translated reopen feedback and no Save
permission. A known accepted split/join, including failed postcommit notification/native return,
keeps fields and financial commands retired with Close-only navigation. Preserve the exact total,
original metadata and attachment owner; do not turn a retained accepted form into another operation.
Native matrices reuse the same refused/accepted forms read-only after original financial qualification.


## D-408 - Original holding editor outcome

Publish the coherent original event/type/history and displayed choices before enabling input. Preserve
raw quantity/amount/fee/reason/note when a changed source requires reopening. An accepted event keeps
all input cards and financial Save disabled with Close, including a failed postcommit notification
or return. Explicit location addition retains financial drafts and cannot refresh away changed facts.
Ignore empty native query redelivery after consuming the original parameters. Presentation matrices
reuse original outcomes read-only and qualify translated feedback without replaying purchases/sales.


## D-409 - Refunded fee correction feedback

Keep source/refund currency errors directly below the account choice and refunded-amount errors below
the fee input, showing both when both are invalid. Retain typed fee/account/note and the same original
review after refusal. Editing a field clears only that field's old error; Save rechecks all financial
constraints. Accepted forms remain disabled/Close-only. The en/fa/de both-theme feedback grows without
clipping at360/412/wide Windows and native412dp Android. Read-only caption qualification after original
acceptance has retired authority; it is separate from the original live enabled retry.


## D-410 - Invalid optional holding fee

Keep invalid nonblank fee input visible with the existing Amount_Invalid feedback below that field.
Never make a negative/malformed fee look like an accepted zero or silently remove the original fee.
Preserve the same editable source review for explicit correction; blank/zero keeps its documented
removal meaning. Show no guessed money prediction while fee input is invalid or its change exceeds
the money range. Native examples follow existing digit/separator choices. Retained caption models
after acceptance have retired authority; they are separate from original live enabled retry.

## D-411 - Refund action independent of managed-source actions

Use the existing CanRefund-controlled card for ordinary expenses and eligible holding fees. The
managed-money explanation and Open the holding remain; editing/deleting purchase/sale principal and
its fee remains grouped in holding history. Show one refund action, preserve its translated semantic
name/native target and existing command, and never expose a principal refund. The original Samsung
fee action now opens/saves its linked refund;26 Windows and24 final Android native contexts qualify
the changed surface. See quality/physical-holding-fee-lifecycle.md for scope and retained failures.

## D-412 - Quantity range feedback beside its field

A too-large raw or resulting holding quantity uses the existing Error value routed through QuantityError
immediately below QuantityInput. FormError suppresses that same range message on the general surface;
other original form feedback retains its existing behavior. Keep raw input and enabled explicit
correction after refusal, omit wrapped/guessed effects, and retire Save after known acceptance.
Final18 Windows and6 Android field-caption contexts read original retired drafts after the original
native same-form corrections; no accepted quantity or money is replayed. This is normal-scale input
qualification, not general live locale-switch, OS200%, spoken-reader or D-386 stability acceptance.
See quality/holding-quantity-input.md.

## D-413 - Original manual holding-price Save/delete and accepted result

Bind manual prices and confirmed deletion to a coherent original type/history/price/location packet,
revalidated in the existing actual-file writer with fresh rights and current draft/confirmation.
Keep raw price/date/mode on changed facts, retire departed authority and offer explicit Refresh.
Known accepted Save/delete survives notification/readback failure without another write; an old-row
deletion preserves a separate new-price draft. An ordinary empty return may refresh history; raw or
failed drafts require explicit review. Keep field-local feedback, scalable native actions and six
translations. Fifteen new selective actual app/SQLite cases,74 affected policy checks and21 resource
checks pass; original native receipts and read-only continuation keep successful financial operations
from being replayed. Full runtime evidence and limits: [original manual price review](quality/holding-price-review.md).

## D-414 - Original holding history deletion and file-bound short Undo

Complete the original holding delete/Undo cycle: coherent displayed-row/group confirmation, fresh
actual-file writer rights/source checks, exact committed deletion receipt and accepted restoration.
Preserve explicit fee-refund links and orphan receipt ownership/content; reject changed/foreign/
mutated/consumed Undo before money and recheck the restored original packet. Keep uncommitted retry,
retire departed actions and distinguish known acceptance from failed notification/readback. Twenty
new selective actual app/SQLite cases, four affected prior cases and21 resource checks pass. Original
native commands and retained read-only captions are qualified separately, without replaying accepted
work. Short Undo remains process-local; no schema/model, new permission/provider or release claim.
See [original evidence and remaining gates](quality/holding-history-undo.md).


## D-415 - Original holding archive confirmation and accepted result

The detail archive no longer saves a retained whole-type copy. Revalidate the original coherent
actual-file type/history/prices/locations packet with fresh Corrections rights and current confirmation,
then change only the tracked type's archive flag. Refuse stale/foreign/mutated/consumed or departed
review, preserve financial history/refund/receipt ownership and raw price drafts, and retain known
acceptance before fallible notification/return. Explicit Refresh reads only; absent/failed reads never
rearm a stale or accepted archive. Thirteen new actual app/SQLite cases pass in selective retained
cohorts, without repeating accepted flows;23 resource checks also pass. Original Windows/Android
Cancel/refusal/archive and read-only captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass. No schema/model, new permission/provider or release claim. Evidence and remaining platform gates: [original archive review](quality/holding-archive.md).


## D-416 - Original type editor creation, edit and accepted identity

The actual type form now owns one coherent actual-file original type/history/price review, or one
stable absent new identity. Recheck fresh rights, immutable source and exact pending raw draft in
the existing writer; copy before mutation and preserve noneditable metadata and dimension rules.
A missing original row never becomes a new form on empty query redelivery. Keep invalid/raw input
for correction, retire stale/departed authority, and require close/reopen after changed source.
Known creation/edit acceptance consumes the original form before notification or navigation; a failed
next step cannot create a duplicate or repeat Save. Nineteen new actual app/SQLite cases pass in two
selective cohorts, without replaying accepted cases;23 resource checks also pass. Original Windows/
Android refusal/edit/create and retired captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass; no schema/model, new permission/provider or release claim. See
[original editor evidence and remaining gates](quality/holding-type-editor.md).

## D-417 - Original physical holding lifecycle

The existing D-416 signed offline candidate passes one original Samsung type creation/edit,
quantity refusal/correction, manual price/delete, immediate purchase-group Undo and archive/cold
lifecycle. Cash849.50, original refund link, type name/note and history persist; Main979/five public
packets/exact three profiles/OS are restored and the phone turn released. Only the new inactive
fixture is retired. No new app/APK/tests/builds or wider D-386/iOS/external/release acceptance is
claimed. The observed refund category is Uncategorized, without a Fees-selection claim. See
[original evidence, observer continuations and remaining gates](quality/physical-holding-type-price-history.md).

## Coherent holding overview publication (D-418)

Keep loading and explicit error/Retry separate from the ready/empty overview. Publish the complete
original file's quantities, known per-currency values, missing values and fine-metal totals together
under the cover; do not initialize settings to display an empty list. Retire pending/retained row
actions on departure and refuse a later opened file. Preserve the existing action dock and row layout.
Use dedicated six-language Holdings loading/Retry captions, not transaction wording. Native semantic
names are compared with their actual bound descriptions; never blindly rewrite user identifiers.
See [original runtime evidence and limits](quality/holding-overview.md).

## Original holding detail cover and type ownership (D-419)

Keep the original quantity, locations, values, basis estimates, history and price rows covered until
all are prepared and published. Show explicit Retry after failure and a separate missing-type state;
neither state may expose old native financial actions. Same-type raw price/date/mode remain for
explicit Refresh; theme recoloring alone does not reread finance. A different routed type owns new
editors, without an old price draft, archive outcome or short Undo. Retained old native targets stay
disabled. A known saved result never automatically reactivates a departed view to read back its
value. See [original actual controls and remaining gates](quality/holding-detail-publication.md).

## Original new holding-place outcome (D-420)

Own the original form while its name prompt is pending. A named storage place is metadata, not a
financial event or geographical position. Preserve its trimmed name/identity on an uncommitted
retry. A known saved place has explicit feedback beside its picker and a read-only Refresh saved
place action; close Add and financial Save until adoption succeeds. Preserve raw date/quantity/
amount/note and the original source on a transfer. Retired captions cannot recover authority.
Keep wrapping scalable captions and semantic colors. Native verification and wider gates:
[original place evidence](quality/holding-location-creation.md).

## Complete original holding-event form (D-421)

Cover every field and Save during the original read and complete publication. Failed loading keeps
raw input covered with translated Retry/Cancel; Retry reads only the originally opened file. A new
routed identity receives a fresh draft, while same-original Retry preserves dirty raw values and
valid selection identities. Never substitute another account/place/kind for an unavailable original
choice. Historical archived payment/place identity remains visible; a transfer destination rebinds
by id. Required missing-default scaffolding is metadata under the original owner and Corrections
policy, not new financial permission. Retire old query/accepted-save callbacks before a new owner.
[Evidence and wider gates](quality/holding-event-publication.md).

## D-422 - Original physical holding-place move and dirty discard

The existing D-421 signed offline APK passes one original Samsung opening3/basis30EUR and move1:
Home2 plus D422_Storage1, total3 and average basis10 remain, with Cash900 and an empty current-month
ledger. One new place is adopted by the original move draft without losing quantity/date/note or
changing its source. Keep editing retains raw2; Discard keeps the saved move1. One terminal cold
retains original history and quantities. Restore Main979/exact five public packets/three profiles/
OS/hash, retire only the new inactive fixture and release the coordinated phone turn.
Twenty-four retained-observation assertions are not new unit tests. No new app/APK/build or successful
suite/accepted financial sequence replay. A separate unsaved USD-type draft exposes unexpected
native currency-search text and is discarded; USD selection/new event and its root cause are not
qualified. Next ready work diagnoses only this original input/list boundary. D-386 general runtime/
A11Y, broader QA-01, Mac/iOS, external/expert/store and release gates remain separate.
See [original sequence, failed observations and limits](quality/physical-holding-location-move.md).

## D-423 - Original native currency selection, without a product workaround

The independent Release/AOT Android caller opens the actual unchanged AccountFormView currency
control once. After actual native realization, the original visible 44-unit GBP button selects GBP
in both the model and control and closes the popup. Stored accounts and entries compare unchanged;
there is no Save surface. The early active-window dump and raw ContentDescription were incomplete
observers, not proof of a blank product list. Preserve failed observations; remove all speculative
production viewport changes. Main code/APK remain D-421, Main resumes with its original PID/Home0,
and no phone input or system setting changes occur. The final independent strict build has zero
warnings/errors. No unchanged Main build/full suite or accepted financial sequence is repeated;
no new unit-test count, physical USD-input, spoken accessibility or general runtime acceptance is
claimed. Continue remaining ready feature/device work with D-386 and external gates separate.

## D-424 - Original entry-detail attachment owner and complete file names

Bind detail attachment reads, native picking and confirmed removal to the original displayed entry,
actual file, metadata and view owner; recheck cached rights and original bytes under the writer.
A changed/departed picker cannot target another query/profile. Consume known commits before
notification/readback; show the saved/reopen outcome without resubmission. Give the complete file
name its own row, separate from reading/removal actions.13 focused actual-source cases pass; no full
suite replay or schema/model/permission/provider change. Retain54 original Windows caption/Cancel
observations and18 changed-row continuations over the same original file. The latter completes its
report and then exits with native0xC0000005: general runtime acceptance is not claimed. Final strict
builds and complete signed offline APK pass. The normal installed Android emulator exercises one
original PDF pick/add, removal Cancel, original ledger Delete/Undo within8s, cold without the input,
and document-only removal with unchanged expense/balance. Restore Main0/original profile set/OS and
retire only the fixture. These are not physical-device, OCR/viewer, general ANR/A11Y, iOS or release
acceptance. Continue remaining ready original entry-editor/detail work under D-386.

## Original editor Save outcome (D-425)

Keep the original raw draft and view owner through native financial confirmation. Save becomes
consumed as soon as its writer commits, including when change notification or later navigation
fails. A receipt write has a separate outcome: its failure must never say that the money was not
saved or invite another Save. A new query owns its own form; an old callback cannot close it or
publish feedback into it. Existing receipt review, file-size limits and manual confirmation remain.

## D-426 - Complete original transaction detail publication and actions

Prepare original entry/reference/group/refund/holding/document/rule facts through one captured
actual-file worker transaction, without creating settings for display. Validate the complete packet
before covered synchronous publication. Keep loading, failed read-only Retry and missing states
separate. A routed identity owns its own session and command instances; old commands, confirmations,
template prompts, OCR/viewer work and departed reads cannot adopt a different publication.

Review updates only the original flag; explicit rule creation uses original category/match and
refuses an already equivalent rule. Delete retains the original exact group/refund/document Undo
contract and deadline. Consume known commits before notification; recovery after saved work only
rereads. A refused viewer request removes only its newly created cache file, while a possibly accepted
native request retains the existing startup cleanup policy.

35 distinct new actual-source behaviors and selective affected/resource checks pass; no unchanged
full suite is replayed. Original Windows evidence includes18 language/theme/width ready contexts,
covered failure/native Retry, query/missing/retired-owner cases and two one-time native metadata
commands through deliberate postcommit notification failures. Preserve the first pre-action report
serialization refusal and continue only its exact original file without reseeding. Final Android,
APK and delivery evidence is recorded in [the detailed qualification record](quality/entry-detail-publication.md). No schema/model, portable format,
permission/provider or broader ANR/crash/A11Y/physical-device/Mac/iOS/store/release acceptance is
inferred. Continue ready original financial workflow work under D-386 and the existing external gates.

## D-427 - Complete original transaction editor publication

Keep the original entry form fully covered until reference and financial source publication completes.
Show translated read-only Retry after initial failure; a missing explicit source cannot offer a guessed
blank transaction. Keep Cancel reachable. A new query owns fresh input, icon and commands; a retained
read-only refresh preserves raw values. Complete both currency/fee flags before uncovering. Keep
historical account identities, and leave a new multi-account choice empty when there is no explicit
or configured default. Native matrices and scope are in [the original evidence](quality/entry-editor-publication.md).

## D-428 - Retained draft and explicit aggregate decisions

Creating an account from a transaction is a nested visit: keep the complete raw money, note/tags,
receipt evidence and pending document; only its accepted account packet refreshes choices. Cancel
returns without writing. Keep saved forms read-only with Close after a postcommit observer failure.
Present each new aggregate overlap separately. Full amounts, source and retained-detail meaning
belong in a growing native dialog body, followed by short Link/Keep both actions. A blocked link
explains the document/relationship boundary and offers explicit Keep both or Cancel.
Home and Transactions display a wrapped truthful aggregate-adjustment Undo notice. Home reserves
only a transient row; its compact header Add action is unchanged. Reappearing never renews the
original eight-second window. [Native evidence and limits](quality/entry-workflow-ownership.md).

## D-429 - Complete original plan form

Keep the whole plan form covered until original fields and derived currency/date/readiness facts are
ready. Show translated loading, explicit read-only Retry, missing-source feedback and a separate
no-account action. A readiness-only error keeps the raw form visible and editable, with Save disabled
and explicit Review. New queries reset optional fields and own fresh command instances. Cancel and
native Back share the original discard lifetime. Nested account creation returns to the same draft;
debt/lent direction, unknown principal, date and currency stay explicit. Known saved work shows Close
and cannot be submitted again after notification failure. See the D-429 quality record for actual
normal-scale platform observations; general runtime/accessibility completion stays deferred.

## Original plan monetary input (D-430)

Keep a raw amount beside its original currency/display-unit label. A formatting preference change
keeps the original parser and shows a complete direction-safe example. Changing currency or unit
requires clearing before fresh input; appending a digit does not acknowledge another meaning.
Transfer destination amounts have their own label, culture and error. Feedback grows with native
text; unknown/hidden amounts do not show irrelevant examples. Never convert or rewrite typed money
to make a changed display preference appear valid. Original Save remains disabled until required
monetary reentry is complete. See [native evidence](quality/plan-money-context.md).

## Original goal/budget monetary input (D-431)

Keep the original unit and direction-safe format example beside each raw amount. Target/contribution,
total and each category limit are independent. Unit changes require clearing before fresh input;
appending a digit cannot adopt another meaning. Hidden stale budget limits expose review controls
and explicit reload. Untouched saved values may renew format through that reload; dirty raw/context
values remain. Quantity rules stay independent of currency. Feedback grows without clipping;
accepted forms offer Close and cannot resubmit after notification failure. [Normal-scale evidence](quality/goal-budget-money-context.md)
does not establish OS/TalkBack or general runtime acceptance; D-386 still applies.

## Original transaction monetary input (D-432)

Each original transaction amount, destination amount, source/destination fee, ISO purchase amount and
reimbursable amount retains its own currency, applicable display unit and parse culture. Unit/currency
changes require complete clearing/reentry; partial edits never renew meaning. Original labels and
format examples remain visible. Receipt/template adoption starts from authoritative minor units;
account Undo and read-only/nested return retain original context. Repeating-plan handoff validates only
its supported primary/destination amounts, without borrowing unused fee meaning. Split shares retain
independent contexts; Join uses the original stored total. Raw/context revisions are rechecked under
original writers; known acceptance remains final. 27 new focused source cases and 51 related prior cases
pass across selective executions, with no full-suite or unrelated successful-check replay.
Keep complete field-local reentry feedback, original unit labels and numeric examples at native scale. Foreign purchase amounts use their ISO currency independently of display units. No automatic conversion is implied.

## Original account money (D-434)

Opening balance, optional repayment and observed balance keep independent original unit labels and
format examples beside raw input. Display changes never relabel an existing amount. Changed units or
currencies show field feedback and require full clearing/reentry; hidden unknown principal does not
hide a stale repayment. Explicit account Refresh preserves the observed draft and requires currency
review. A known Save with a failed default follow-up stays Close-only with the existing review message.

## Original holding monetary input (D-435)

Payment, fee, second-currency amount, optional basis and manual price each show their original unit,
applicable factor and numeric-format example. Changed units/currencies expose field-local feedback;
partial edits cannot adopt them. Keep context refusal separate from refund and malformed-input
errors. An optional price-currency amount is unknown only when blank. Quantity format examples are
numeric rather than currency-labelled. Preserve native scaling, growing rows and existing actions.

## Original secondary monetary editors (D-436)

Each goal/estimate/occurrence/settlement/shared amount has its own immutable original parser and
nearby format/unit explanation. Display-only updates preserve dirty raw text. A changed currency or
unit requires complete clearing and reentry; partial editing never reinterprets it. Invalid nonblank
optional prices are refused rather than treated as a request to remove an assumption. Shared main
amount and share errors publish beside their respective fields even without a language event.

A native forecast prompt captures its visible caption and parser together before awaiting input;
its scenario remains an estimate. Known financial acceptance clears/retires only that submitted draft
before notification/readback. Follow-up failure is truthful and cannot offer another financial Save.
Display-unit publication waits for durable original Settings acceptance. Long unit choices use the
existing wrapping real button with native text scaling and target size preserved.

## Physical and plain-rate original input (D-437)

Do not call physical weight/purity or a plain rate a money amount. Each numeric input retains its
own original format through partial typing; the guide and example explain complete clearing to
adopt the current format. Show all invalid fields beside their controls. Manual-rate feedback keeps
its translated category, and explicit Refresh reads without repeating a write. Display every stored
decimal digit, including delete consent; the estimate badge grows on a separate row.

## Raw Android numeric drafts (D-440)

An input keyboard may suggest numeric keys but must not turn a comma decimal into a different integer
before the form sees it. Preserve separators, signs, grouping, native digits and invalid partial drafts
for original application validation. Retain numeric keyboard hints, secure masking and length limits;
do not normalize a financial value silently. Actual InputConnection evidence is separate from layout
or model-to-native publication evidence; see quality/android-numeric-input.md.

## Secondary monetary system-scale evidence (D-441)

Goal allocation/price, occurrence/bill, Settings estimate/native forecast prompts and unit-definition
captions pass targeted actual OS200% geometry at360/412dp in three languages/both themes. Preserve
native scaling, complete final-line height and raw input; native prompt answers retain44dp targets.
This does not qualify every control or audible TalkBack. See [evidence](quality/secondary-money-os-qualification.md).

## Current title actions at system scale (D-442)

Home and Plans retain their right-side44-50dp Add, complete title and uninterrupted ordinary
viewport at actual OS200%,360/412dp,en/fa/de and both themes. Empty and original filled Plans pass;
Home's valid Undo reservation remains.36 observations include actual Add/Cancel without Save.
This does not qualify active Undo or spoken reader clarity. See [evidence](quality/header-os-qualification.md).

A further36 read-only native-coordinate observations verify the action is physically right of the
complete title (minimum gap8px), without replaying any Add/Cancel. Total D-442 evidence is72
observations/96 header states/192 native text readings; accepted writes remain zero.

## Retained rate outcome geometry (D-443)

Actual OS200%,360/412dp,en/fa/de and both themes retain complete outcome, historical precise ratio,
long date and estimate badge in36 observations/144 readings. The original actual template is used
with disabled native writers; deleted data remains absent. This is geometry of source-proven
historical presentation, not a new current-rate lifecycle. See [evidence](quality/rate-outcomes-os-qualification.md).

## Protected household system-scale geometry (D-444)

36 original protected cost/share/unsaved-repayment observations and312 native readings pass actual
OS200%,360/412dp,en/fa/de and both themes. Full field-local captions and native raw values retain
original meaning without accepted writes. Actual cached signed authority must remain valid; do not
renew it to make a view pass. See [evidence](quality/household-os-qualification.md).

## D-445 - Observe the actual currency search owner

A dropdown arrow and editable search are distinct native targets. Diagnostic text input verifies
the actual owned modal-root focus before typing; opening a list is not proof of search focus.
Observe the real filtered row and selected currency independently. Matching emulator reproduction
does not establish the historical Samsung cause. No shipping interaction is changed in this batch.
See [evidence](quality/currency-input-focus-boundary.md).

## D-446 - Keep Home goal computation away from input/rendering

Compute complete original goal progress on a worker, then format/publish on the original window
context under the existing cover and owner check. The scheduling change does not change goal rows,
colors, scaling, source meaning or any financial result. It is not general ANR acceptance.

## D-447 - Publish the complete original goal list under a local cover

The goal list now reads one captured original file/transaction, keeps its existing financial
calculation on a worker and publishes active/history/funding rows under one translated loading/error
cover. Exact row/command/page lifetime guards refuse stale input; explicit retry and detail return
preserve the original source. No financial algorithm, schema, permission or runtime policy changes.
Fourteen new actual-source cases, three affected prior cases and23 actual resource checks pass
selectively. Eighteen Windows contexts produce84 native observations, six Android contexts produce
36 observations (34new/2retained); original source/base rows remain identical with zero new writes.
Complete signed Offline Main upgrade and actual empty-list Add/Cancel pass without reset; Home139
nodes match the old upgrade except focus, and final return differs only in one drawing-order value.
Strict builds and test-output cleanup pass without warnings. Failed native target/capture-path
observers are preserved and corrected against original receipts without repeating completed scenes.
No general ANR/crash, two-second, low-memory, spoken, physical-phone, iOS or release acceptance is inferred.

See [original goal-list evidence](quality/goal-list-publication.md).

## D-448 - Keep complete original goal details and actions in their page lifetime

Original goal details now use one complete captured file/transaction and worker financial
calculation under a local loading/error/retry cover. Page/identity revisions retire old commands
and pending reads while retaining original dirty money inputs. State/delete writers check the
original action inside their transaction before changes and commit, and preserve known accepted
results across observer failure. Three original regressions reproduce identity, pending-delete and
file-adoption defects;23 new focused cases and58 affected prior goal cases pass in selective runs,
plus19 original monetary-context and23 resource cases. Native Windows146 and Android50 observations
(31new/19retained Android) preserve retained fixture rows with no new fixture writes or replayed fixture
actions. Strict builds, signed Offline Main upgrade/Home139-node/OS checks and test cleanup pass.
Observer/compile failures are retained and repaired without replaying completed native actions.
General stability/performance, spoken/device/iOS, all protected-Main goal variants, native
destructive confirmations and genuine external gates are not accepted by this finite batch.

See [original goal-detail evidence](quality/goal-detail-publication.md).

Final source regressions also cover failed/busy/cancelled Edit validation and retained-stack theme
refresh. Only2 extra native observations per platform qualify the new Edit refusal/retry path;
completed cohorts remain. One new production-app test profile/account-opening/goal Save and one
cold read qualify positive protected-Main account-balance details100/150/50EUR and Edit/Cancel,
with All-history empty. Only that new profile is removed via full two-question UI cleanup. Main's
profile-list/Home139 nodes and0.00EUR return unchanged; English/Dark/Simple and OS are read back.
These are controlled new metadata saves, not repeated fixture payments or full protected-goal,
cryptographic, stability, spoken, physical/iOS or release acceptance.

### D-449: retain complete scaled native toolbar titles

Android keeps its native title and Back control. Grow the owned toolbar when the actual title's last
line needs more height at OS text scale, including Persian script metrics; do not shrink the font.
Retire geometry observers with that toolbar and avoid repeated/shrinking requests inside layout.
The finite360/412dp goal-page evidence is recorded in [goal-page OS text](quality/goal-page-os-text.md);
it does not establish general Android stability or spoken accessibility acceptance.

### D-450: own the native title observer through attachment and retirement

A toolbar observer obtained before attachment can become dead while a different live tree owns its
callbacks. Register a separate explicit native listener; remove that same identity from the current
live tree and detach its weak owner at retirement. Do not share the Shell tracker's Java disposal
boundary with that callback. Preserve scaled title metrics and native Back. Finite lifetime/title
checks do not establish general ANR/crash or spoken-accessibility acceptance.

### Original budget command lifetime (D-461)

Budget Edit, previous/next copy, Delete and Retry belong to the original published period and native
page lifetime. Replacing the displayed review or leaving the page retires retained command objects
and pending consent. A hidden page cannot rearm itself through Retry or a late theme/read callback.
Keep editor-return period semantics and current-month reset on a new tab visit. Known committed
notification failures use the existing saved/refresh message; only a read-only Retry is offered.
No new amount, method, calendar, permission or confirmation design is introduced.

### Complete budget financial publication (D-462)

Budget loading, failure and explicit Retry cover the complete body. Do not reveal old/partially
replaced total, category, cash, goal or plan rows while later sources are still being read. Publish
all financial rows together, retaining the original period and separate action ownership. Workers
prepare amounts/status; native colors, localized rows and collection publication stay with the
window. No financial calculation or saved display meaning changes merely to speed a refresh.

### D-463 - Period-neutral budget language

Shared budget actions, errors and editor labels refer to the period rather than assuming a month.
Keep the exact reviewed start/end dates visible in a growing editor header; financial-month start
and the selected date display stay authoritative. Weekly/two-week views show currency without a
monthly-calendar annotation. Native text scaling and the original raw money/currency are preserved.
Caption qualification does not close full accessibility, stability or short-period acceptance.

### D-465 - Explain hidden budget account choices

Simple budget editing keeps an existing account restriction and names its complete retained accounts
in the growing hidden-choice note. Explain that changing them requires Advanced in Settings.
Use current dirty draft choices, preserve archived/missing identities and leave unrestricted
budgets without a restriction note. Never broaden a saved scope merely because its controls are
hidden. Keep currency, exact displayed dates, raw amount and existing category/method notes intact.
The changed note passes18 Windows and six independent Android language/theme contexts; the original
physical Simple editor confirms it after upgrade/cold read. This is not full spoken acceptance.
