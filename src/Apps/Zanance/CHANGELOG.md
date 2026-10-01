# Changelog – Zanance

All notable changes to Zanance (project `src/Apps/Zanance`). The format follows [Keep a Changelog](https://keepachangelog.com/),
versions follow [Semantic Versioning](https://semver.org/). Nothing has been released yet; everything below is in
the first release candidate. Requirement ids refer to the [specification](docs/spec/Zanance-Product-Specification.md).

## [Unreleased]

### Added – Phase 1

- App for Android, iOS and Windows in English, German and Persian (right to left), with the Gregorian or Persian
  calendar chosen independently of the language; no sign-in, everything stays on the device.
- Onboarding with language, currency, calendar, first account and starter categories; Simple and Advanced mode.
- Accounts (cash, checking, savings, credit card) with opening balance, archiving, reconciliation and adjustments.
- Income, expenses, transfers with fees, refunds and corrections; review status, search and filters, delete with undo,
  quick templates.
- Categories with one sub-level, icons, colors, ordering, archiving and merging.
- Plans (one-off and recurring, Gregorian or Persian, month-end and leap-day rules), occurrences with skip, move,
  link and confirm, "this and future" changes, pause and end, automatic posting that never posts twice.
- Local reminders with generic lock-screen text, snooze, a summary for reminders at the same time and an optional
  second reminder on the due date.
- Home dashboard, monthly budget with alerts, reports (spending, income and expense, trend, accounts, plans versus
  actual) with drill-down to the entries, and a balance forecast with what-if changes.
- Multiple currencies with manual dated exchange rates and a report currency.
- CSV export and import (own files without duplicates, generic files with a mapping and preview), encrypted backup
  files with restore, "delete all data", app lock, and a recent-apps preview that never shows content.

### Added – Phase 2A

- Savings goals with earmarks per account, priorities, shortfalls and suggested contributions.
- Budget rollover of surplus or of surplus and deficit; an optional envelope method that shows the money not assigned
  yet.
- Split transactions, partial payments of plan occurrences and the final settlement of advance payments.
- Contracts with cancellation and review reminders, reimbursable expenses, tags with a tag report, and
  categorization rules for new entries and imports.
- Loans, money lent and assets outside the cash total; an optional interest rate and installment give a repayment
  estimate, a schedule and installments split into principal and interest.
- Receipt photos and PDF files attached to entries.
- Weekend rules, a second day per month and weekday rules (e.g. the last Friday) for plans.
- Display units such as the toman, defined by the user.
- Forecast scenarios with assumed dates and amounts, saved transaction filters, a customizable Home, a PDF report and
  a dark theme.

### Added – Brand

- The approved Zanance symbol as the app icon on Android (adaptive and themed), iOS and Windows, on the splash screen
  (Android: symbol, iOS: symbol and wordmark), as the Android notification icon and in onboarding and on the More page.

### Added – Per-app language on Android 13+ (D-32)

- The app language can also be chosen in the Android settings; both places stay in step.

### Added – Receipt reading (D-31)

- Read the total, date and shop from a receipt photo on the device (iOS, Windows and Android); the values open the editor
  for review before anything is saved.
- Read PDF invoices and receipts as well (D-33): the text of digital PDFs is taken directly, scanned PDFs are rendered
  by the system and recognised like photos. Invoice totals ("Rechnungsbetrag", "amount payable") are recognised.

### Added – Quick add widget (D-30)

- An Android home-screen widget that opens a new expense, income or transfer in one tap. It shows no amounts.

### Added – Public holidays (D-29)

- Plans with a weekend rule can also move off public holidays of Germany (nationwide) or Iran (lunar holidays are
  calculated and marked as possibly a day off).

### Added – Flex budgets (D-28)

- A flex method for budgets: fixed bills are expected from your plans, non-monthly bills get a monthly share, and one
  limit covers everything flexible. Each expense category can be marked fixed, non-monthly or flexible.

### Changed – Design and help (D-36)

- A "?" next to settings whose effect is not obvious explains them in full, with an example.
- A clearer start: centred welcome with the symbol, a progress bar, an icon for every step, "Add your first account".
- Windows: a visible back button next to the title of every page opened from another page; such a page opens at the
  top with the focus on that button (Settings no longer opens halfway down).
- Light theme: the page background is a step darker and outlines are stronger, so cards stay visible.
- The account icon is chosen with a real button that shows the current icon; "Include in totals" explains itself.
- Rows in More work with the keyboard and screen readers; icons are no longer read aloud as odd characters.
- Android: `eng/scripts/Build-AndroidApk.ps1` builds an APK that installs directly on a phone.
- Android: the status bar shows the page background instead of the brand blue, with dark icons in the light theme,
  and the Insights tabs are no longer written in capitals.
- Spacers and colour dots no longer show a grey rectangle (e.g. below "Customize Home").

### Changed – Home at a glance and quick add (D-37)

- Home starts with your balance and a quick add card: Expense, Income and Transfer open the editor with that kind,
  and your quick templates fill it in one tap – a template with an amount is two taps (template, Save).
- A Receipt button next to them: choose a receipt photo or PDF, it is read on the device and opens a new expense with
  amount, date and shop to check; the receipt is attached when you save.
- Recent entries on Home: the three latest entries, one tap to their details (can be hidden or moved like the other
  sections).
- The editor says what you are adding ("New expense", "New income", "New transfer").
- Windows: the keyboard focus ring is drawn in the app's blue instead of black and white.
- Long subtitles in transaction rows are shortened instead of running under the amount.
- This month / last month moved into the income and expenses section they change.
- The budget card shows what is left per day for the rest of the month.
- Colours where they belong: budget bars are green within the limit, amber near it and red over it; the forecast line
  is violet; the forecast on Home shows the end balance and the lowest point separately, red only below zero; negative
  balances in an account's month summary are red.
- Chart dates follow the app language and calendar (the forecast showed Persian month names in English); the forecast
  list shows dates as "October 28".

### Added – Cloud backup (D-35)

- Optional backups to your own OneDrive (Android, Windows) or Google Drive (Android): connect, back up now, see and restore
  or delete cloud backups, disconnect. Cloud backups are always encrypted with your password; the app sees only its own
  folder. Offered only in builds configured with the OAuth clients; other builds stay fully offline.

### Added – Local profiles (D-34)

- Several independent profiles on one device (More › Profiles), e.g. personal and business, each with its own data,
  settings, app lock and backups. A profile with the app lock asks for your fingerprint, face or PIN before it opens.

### Added – Pay-cycle months and limit suggestions (§10.3)

- The month can start on any day from the 1st to the 28th (Settings), e.g. on payday. Budgets, Home, reports, the
  transaction filters and the forecast month end follow it; plans keep their dates.
- The budget editor suggests limits from the average spending of the last three months, rounded up; they are only
  filled in when you choose so.
- Weekly and two-week budgets (Advanced) next to the monthly one: the week follows the week start in the settings,
  two-week periods start on a day you choose (e.g. payday). Limits, alerts, rollover, copying and suggestions work
  for them too.

### Changed – Design (D-27)

- A calm, neutral look in the brand blue, where every colour has one meaning: green for money coming in, red for
  problems and debt, amber near a limit, violet for plans and due dates, teal for savings, slate for transfers and
  sky blue for refunds and entries to review. Expenses are shown with "−" instead of in red.
- A new Insights tab with Budget, Reports, Forecast and Goals; the More tab is sorted into four groups.
- A quieter Home: balance, what needs attention, income and expenses, budget and the next due items; the category
  chart and the account list can be turned on in the Home layout.
- Date tiles for due items, colour tiles for categories and accounts, and a filter button that keeps the less used
  transaction filters out of the way.
- The Vazirmatn font for Persian and Figtree with Urbanist titles for English and German, and Persian digits in the
  Persian interface (can be turned off in Settings).

### Fixed

- "This and future" changes and resuming a plan are refused when recorded payments or states after the change date
  would no longer match the new dates; a settlement lost in an interrupted posting is repaired on the next start.
- Saving or deleting entries updates the paid amounts of plan occurrences in the same transaction.
- Merging a category into its own sub-category keeps the target as a top-level category.
- Adjustments keep their direction through CSV export and import; CSV amounts with two signs or too many digits are
  rejected instead of being misread.
- A purchase with refunds or an amount to be paid back can no longer be split.
- Plans versus actual counts only fully settled occurrences; a transfer between two accounts counts once in the
  unreviewed summary.
- The safety copy made before a restore no longer counts as the last backup.
- iOS: Face ID usage text in English, German and Persian, and a complete privacy manifest.
- Switching from Persian to another language during onboarding no longer leaves Persian digits in the texts.

### Not yet included

- Cloud backup on iOS and Google Drive on Windows; automatic cloud backups. Cloud backup is not yet verified on a
  device with real OAuth clients.
