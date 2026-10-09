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
