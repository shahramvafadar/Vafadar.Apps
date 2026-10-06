# Changelog – Zanance

All notable changes to Zanance (project `src/Apps/Zanance`). The format follows [Keep a Changelog](https://keepachangelog.com/),
versions follow [Semantic Versioning](https://semver.org/). Nothing has been released yet; everything below is in
the first release candidate. Requirement ids refer to the [specification](docs/spec/Zanance-Product-Specification.md).

## [Unreleased]

### Added – Lunar Hijri calendar

- A third calendar in Settings and at onboarding: lunar Hijri (Umm al-Qura, the official calendar of Saudi Arabia),
  next to Gregorian and Persian. Dates, budget months, monthly and yearly plans, the date picker and the dates of a
  CSV import can use lunar months of 29 or 30 days; a plan on day 30 falls on the 29th in a shorter month.
- Umm al-Qura covers 1900 to 2077; dates outside are shown in the arithmetic Hijri calendar instead of failing.
- A receipt dated 1447/03/12 is read as a lunar date when that is nearer to today than the solar date (which would
  be 2068).
- A plan in another calendar than the one shown names its calendar, e.g. "Every month on day 2 · Gregorian".
- Date tiles and chart axes use distinct short month names ("Rab I", "Rab II") instead of the first three letters.
- This prepares languages such as Arabic; the remaining steps are listed in the localization guide.

### Added – Italian

- Zanance is now available in Italian: the whole interface, help texts, notifications, the widget, the iOS permission
  prompts and the PDF report. A phone set to Italian (Italy, Switzerland …) uses it; the language does not set your
  country, currencies or calendar.
- Italian dates in all three calendars ("venerdì 25 settembre 2026", "venerdì 3 Mehr 1405"), and plans on a Sunday
  read correctly: "la prima domenica", "Ultima domenica" ("il primo lunedì" for the other weekdays).
- Italian receipts are read with their total ("TOTALE", "IMPORTO TOTALE", "NETTO A PAGARE", "TOTALE IVA INCLUSA";
  "(di cui IVA 4,33)" is skipped), never "TOTALE IVA", the taxable amount, a discount, the cash handed over or the
  change.

### Added – Screen reader hints

- Screen readers (TalkBack, VoiceOver, Narrator) now explain six short action buttons after their label: record a
  purchase or sale, add holdings you already own, correct a quantity, change one occurrence and complete a goal –
  for example that nothing is bought or sold in the app and that the plan stays as it is. Written by the owner in
  every language.

### Changed – French

- Four buttons fit the narrowest screens (owner's wording): "Saisir un achat" and "Saisir une vente" (record a
  purchase or sale, nothing is bought or sold), "Corriger" (correct the quantity held) and "Modifier" (changes only
  this occurrence, the plan stays as it is).

### Changed – Spanish

- Two buttons fit the narrowest screens: "Añadir sin compra" (holdings you already own or received as a gift) and
  "Editar vencimiento" (changes only this occurrence, the plan stays as it is).

### Added – French

- Zanance is now available in French: the whole interface, help texts, notifications, the widget, the iOS permission
  prompts and the PDF report. A phone set to any French variant (France, Canada, Belgium, Switzerland …) uses it; the
  language does not set your country, currencies or calendar.
- French dates in all three calendars ("vendredi 25 septembre 2026", "vendredi 3 Mehr 1405").
- French receipts are read with their total ("TOTAL TTC", "NET À PAYER", "Total TTC 24,00 (dont TVA 4,00)" gives
  24,00), never "Total HT", the tax, the cash handed over or the change; amounts like "1 234,56" are read whole.

### Added – Spanish

- Zanance is now available in Spanish: the whole interface, help texts, notifications, the widget, the iOS permission
  prompts and the PDF report. A phone set to any Spanish variant (Spain, Mexico, Argentina …) uses it; the language
  does not change your country, currencies or calendar.
- Dates in Spanish read naturally in all three calendars ("viernes, 25 de septiembre de 2026", "3 de Mehr de 1405").
- Spanish receipts are read with their total ("TOTAL A PAGAR", "IMPORTE TOTAL", "TOTAL IVA INCLUIDO"), never the
  subtotal, the cash handed over or the change.

### Changed – German wording

- The German texts were reviewed throughout: formal "Sie", consistent terms (Buchung, Umbuchung, Sicherung,
  Prognose, Nettovermögen) and the same meaning as the corrected English – deleting and restoring affect the current
  profile, a failed restore says whether data may have changed, and credit card repayments are no longer called
  card payments.

### Changed – Persian wording

- The Persian texts were reviewed throughout: consistent terms («تراکنش»، «برگشت وجه»، «نسخهٔ احتیاطی»، «برنامهٔ
  مالی»), correct half-spaces, and the same meaning as the corrected English – deleting and restoring affect the
  current profile, a failed restore says whether data may have changed, and goals explain all three types.
- After a cloud sign-in in the browser, the page now says that the browser step ended and the result is shown in
  the app, instead of announcing a sign-in that could still fail.

### Changed – English wording

- The English texts were reviewed throughout: plain US English, "transaction" for what you record, and texts that
  say exactly what Zanance does – deleting and restoring affect the current profile only, holdings with a price are
  part of net worth, the region sets weekend days and public holidays for new plans, and the forecast shows only
  what is recorded and planned.
- "1 day overdue", "1 day before" and "On the due date" instead of "1 days" or "0 days before"; a yearly plan on the
  last day names the month ("on the last day of February"), and its day and month are those of the plan's own
  calendar.
- A failed restore says whether your data may have changed: if the safety copy could not be created, nothing was
  touched; otherwise check your accounts. The message no longer points to a safety copy the backup list does not show.

### Fixed – Theme change keeps open pages

- When the phone switched to dark (or light) on its own while an expense, a plan or another form was open, the app
  closed the form and what was typed was lost. Now the open form stays as it is and only changes its colours; the
  screens are renewed once you are back on a tab.
- A Save button that was disabled once kept the colour of the previous theme after a theme change; disabled buttons
  and fields now have a grey that reads in both themes.
- In Persian, opening the expense editor (and other forms with a date) showed "unexpected error": the check for
  unsaved changes wrote dates in the Persian calendar, which cannot show an empty date. It is independent of the
  language now.

### Fixed – Code review (all parts): libraries, core, data, app foundation, screens and cross-cutting UI

- Creating, previewing and restoring a backup no longer freezes the screen: the password protection, compression and
  copying run in the background.
- A large backup to Google Drive or OneDrive no longer fails on a slow connection after 100 seconds; a transfer may now
  take up to 10 minutes.
- Choice chips and date fields are real buttons: they can be reached with the keyboard, screen readers announce them
  as buttons, and the chosen chip is read as "…, selected".
- Chips and date fields take the colours of the new theme at once when the theme is switched.
- Categorization rules recognise payees typed on any Persian keyboard (Arabic or Persian forms of ی and ک), and the
  search and the rules treat a half-space like a space, so "میوه فروشی" finds "میوه‌فروشی".
- Plans that started long ago and saved forecasts load faster: daily and weekly plans no longer go through every past
  date, and the comparison of a saved forecast no longer reads the whole ledger for every day.
- A receipt or invoice whose text uses the Arabic forms of ک and ی (common in PDFs) is read with its total line, not
  with a later line such as a service fee.
- A quantity typed as "1.5" g in German is 1.5 g again (it was read as 15 g); separators follow the language and an
  ambiguous number is refused instead of guessed.
- The price per gram of a holding bought in rials is right also for large amounts (it could overflow).
- Long tables in a PDF report no longer run under the note at the foot of the page.
- Importing a large CSV file is faster.
- A changed assumed price of a quantity goal is kept (a second change was lost).
- Merging categories also moves them in the spending cut of a goal and in saved filters.
- The icon choice (accounts, entries, goals) works with the keyboard and screen readers: each icon is a button with a
  name ("Icon 12", "Default icon") and the chosen one is announced; it is marked in the app's blue selection colour and
  follows a theme change.
- The lock screen says when unlocking was cancelled or failed, keeps its contrast in the dark theme and follows a theme
  change; a link from a notification or the widget that cannot be opened no longer stops the others after unlocking.
- Very large receipt photos no longer risk closing the app on Android: they are decoded at a reduced size first.
- Long lists and the reminder update do less work (colours are prepared once; categories are read once per update).
- Deleting an entry from its details asks first when it was opened from Home, an account or a plan; there is no Undo
  there (from the transaction list, Undo is still offered instead).
- Leaving the entry editor asks before discarding changes also when only tags, the reimbursement, the aggregate range or
  the destination fee changed.
- A saved entry is never reported as "not saved" when only going back afterwards fails.
- A new aggregated entry covers the financial month (in the Persian calendar and from the month's start day).
- Home loads faster and no longer shows a row twice when the period is switched while it loads.
- The transaction list filters faster with many entries; deleting a selection is one step for the other screens.
- In a bulk category change, sub-categories show their main category ("Car › Other").
- Opening an attachment that no app on the device can show says so.
- Buttons and switches in the recording screens have names for screen readers and full-size touch targets.
- Two accounts can no longer have the same name (also not in another spelling), so lists and CSV files tell them
  apart.
- Only money accounts can be the default for new entries; switching the default and cancelling asks first.
- The category editor and the entry editor mark only the chosen icon or category (an earlier choice could still look
  selected).
- Sub-categories of an archived main category stay in the category list.
- Very large prices or quantities of holdings are refused instead of overflowing.
- The exchange-rate form suggests a missing rate first and accepts the Persian decimal separator.
- Account rows wrap their labels on small screens.
- "Also skip public holidays" in a plan is saved again (it was lost on every save) and the preview shows the shifted
  dates.
- Copying a budget to the next month over an existing one can no longer lose it.
- The budget page no longer mixes lines when the period or currency is switched while it loads.
- A budget needs at least one account (with none ticked it covered all accounts).
- The plan centre opens faster with many entries.
- Screen readers hear the amount and status ("3 days overdue") of due items; switches in plans and budgets have names.
- Reports apply a currency, period or package chosen while they are still loading (it was ignored), and the PDF never
  mixes with a reload.
- Saving a goal again after a failed second step no longer creates a second goal.
- The forecast and the goal details load faster and never fill twice.
- Changing several settings quickly keeps all of them (one could restore the old value of another), and a settings
  error no longer closes the app.
- A finished restore is never reported as failed.
- Exported CSV files and PDF reports are removed from the app's cache on the next start and after "Delete all data".
- Finishing onboarding again after an error no longer creates a second account.
- Switches in settings, backup and import/export have names for screen readers.
- Every editor asks before discarding typed input (goals, budgets, holdings, splits and final settlements left at once).
- On small phones, help buttons stay next to long labels, goal and budget names are no longer broken letter by letter,
  and the quick add labels fit in German.
- Holding changes show their sign in front of the number in Persian.
- The "Opening balance unknown" label toggles its checkbox, like the other checkbox labels.
- Two categories with the same name in the same place, or two profiles with one name, can no longer be created.
- Screen readers name each icon in the icon choice ("Car", "Savings") instead of "Icon 12".
### Added – Validation and release, Simple/Advanced policy (enhancement ZEX, phase 6)

- About Zanance says what each file contains: a backup holds everything you entered (device preferences such as theme
  and language excepted), a CSV export holds entries and holdings but is not a backup, and a PDF report is for reading
  only. It also lists the systems every release is checked on (Android, Windows; iOS not yet).
- "Prepare a problem report" in About: the app writes a short text with only its version and the device type and
  opens the share sheet; you choose where it goes and remove financial details first. The app sends nothing on its
  own – no usage data and no crash reports.
- Simple mode no longer hides data that exists: a weekly or two-week budget is shown with its own period, and an
  existing second reminder, contract and budget method stay visible and can be corrected. A new profile starts in
  Simple. What each mode shows is decided in one place, and switching the mode never changes a number or a setting.
- Tests: upgrading a database and restoring a backup of the version before this enhancement, a full backup round trip
  of the new data, keys built from names in all three languages, the Simple/Advanced policy and a mode switch with a
  rich data set, and the speed of the new calculations with 10,000 entries, 500 holding events and 10 goals.
### Added – Quantity goals and wealth history (enhancement ZEX, phase 5)

- A goal can count a quantity of a holding, e.g. 50 g of gold, at all locations or one: progress is the quantity you
  hold, never a price change. The plan is a quantity per date; the editor previews the remaining grams and the date.
- Every goal shows its pace: the median of what reached it in the last complete months (up to six), with the date it
  would reach the target, "Not enough history yet (2 of 3 months)" or "No date at your current pace". A month far above
  the others is marked one-off; your own plan stays a separate line.
- Advanced: for a quantity goal you can type an assumed price; your capacity is then shown as a quantity per month
  ("At your price: 2.5 g per month") – never as the value of the holding.
- Reports › Wealth (Advanced): net worth at the end of each month with the prices and rates of that day, and whether it
  grew through saving or through prices and exchange rates; whatever the parts do not explain is shown with its cause.
- Forecast (Advanced): save the forecast with a name; later compare it with reality – the saved and the actual path,
  and the difference split into entries recorded later, unplanned spending and income, and plans paid differently.
  Saved forecasts never change and are part of backups.

### Added – Reports and data quality (enhancement ZEX, phase 4)

- Reports are now five packages under one scope bar – period overview, commitments, goals, holdings and data status.
  The scope (period, currency, accounts in totals, usable accounts or one account, confirmed only) is named next to
  every number, and "Show entries" lists exactly the entries behind it with the same total.
- Every number has a "?" that explains it: the question it answers, the definition in words, what is included and
  left out, and the data status. There is never an accuracy score.
- Period overview: surplus and the share of income kept ("not available" without income), spending changes against
  the same days of the previous period, transfers and holding purchases as separate lines, categories, and in
  Advanced the trend and tags (an entry with several tags is named, tags never add up).
- Commitments: the headroom – the lowest balance of the usable accounts with your plans, minus money you protected,
  labelled as an estimate and never "safe to spend"; Home warns when plans would use protected money. Payments of
  the next 30 days (known, ≈ estimated and unknown apart), money owed to you with its age, the next loan installment,
  and in Advanced the twelve-month view, the share of income for installments and plans versus actual.
- Goals: progress of every goal; in Advanced the capacity per month (income minus spending, non-monthly shares,
  loan principal and other goal plans) with suggestions that never exceed it – accepting one only stores the goal's
  plan – and how many months your usable money covers essential costs.
- Holdings: net worth per currency (money, money owed to you, valued assets, holdings, debts), holdings without a
  price listed; in Advanced the composition in the valuation currency. Account movements now name holding
  purchases and sales.
- Data status: unreviewed entries, unknown amounts, missing or old rates, holdings without a price, unknown
  opening balances, accounts not compared with the bank for 60 days, an old backup, possible duplicates – each opens
  the screen that fixes it. Comparing a balance now remembers the day; Home shows an old backup once.
- Month review: when a financial month ends, Home offers "Review September" – a short checklist (entries, due
  payments, balances, goals, backup) whose progress is kept.
- Advanced: a day-to-day spending estimate for the headroom (with a suggestion from the last three months),
  essential categories (set by default for housing, food, energy, phone, transport, health and insurance), entries
  that sum up several purchases of a range with a choice to replace or keep both when detailed entries come in (with
  Undo), and due dates for money lent and reimbursements. The PDF prints the scope of every section and includes the
  trend and account details; the CSV export carries the new fields.
### Added – Holdings by weight or count (enhancement ZEX, phase 3)

- New under More > Money: holdings such as gold, coins or other things you own by weight or count. Each type has its
  own unit (g/kg or pieces with an optional weight per piece), metal and purity (karat or fineness), and quantities
  add up only within one type – 18 k and 24 k gold stay apart. A fine-metal line shows the pure metal of the types
  with a known purity.
- Record what you already own, purchases (money from an account, an optional fee as an expense in Fees, a second
  amount when the account's currency differs), sales, moves between locations, gifts, removals and corrections with
  a reason. Nothing can make a holding negative on any date ("On 3 Oct only 20.000 g were held at Home safe."), and
  the form shows the result before saving. Purchases and sales are neither spending nor income.
- Prices per gram or unit, or the total value of what you hold; the latest price gives the value, purchase prices are
  used and marked, and a type without a price shows "Value unknown" instead of zero. Details show the quantity per
  location, the average cost and the unrealised and realised results as estimates. Deleting a change can be undone.
- Home and Accounts show a holdings line per type, never added to your money. Transactions show holding purchases
  and sales as such; their money is changed or deleted together with the holding.
- Import/Export: a holdings file (types, locations, changes, prices) that imports into another profile and skips what
  is already there; every CSV file now carries a format version, and older files import unchanged. Restoring a backup
  lists its goals and holding types.
- Advanced: a valued asset account can be converted into a holding after a preview; the account is archived and can
  be restored. Recording spending or income on such an account asks for confirmation first.
### Added – Account goals (enhancement ZEX, phase 2)

- A goal can follow the balance of one account ("Savings account to 5,000 EUR"): deposits, transfers in and
  withdrawals change its progress, and it reserves nothing. Money set aside works as before; in Advanced you choose
  the kind. One balance goal per account.
- Goals can be paused, completed and reopened, archived and restored. "Reached" and "overdue" follow the real numbers:
  a withdrawal after reaching a goal shows it as not reached again; a passed date says how much is needed now.
- Your plan per goal: contribution dates (monthly, every two weeks from a pay day, weekly; Persian months in the Persian
  calendar) and a fixed amount – in Advanced also a share of last month's income (transfers, refunds and money from
  savings never count) or a spending cut that changes the budget only when you apply it. The editor shows progress,
  the amount needed per date and the estimated date before you save.
- Pin up to two goals to Home: progress, what is left and the estimate of your plan. Without a pinned goal, an overdue
  goal appears under "Needs attention".
- Money is set aside only in money accounts, a release cannot exceed what is set aside, and money set aside can be
  marked as protected (Advanced).

### Changed – Several currencies, defaults and Home (enhancement ZEX, phase 1)

- Three separate settings under Money and months: the default currency for new items, the default account for new
  entries and the valuation currency for converted totals. Changing one never changes another or any stored amount.
- The entry form shows the account directly under the amount. Quick add, the widget and receipts use the default
  account only when it is a usable money account; otherwise the form asks for an account and Save waits for it – no
  hidden fallback to the first account. Switching to an account in another currency keeps the digits and says so,
  with Undo. Save shows what it will do ("−25.00 EUR from Main"); a transfer between currencies shows the rate of
  your amounts, and Advanced adds a fee at the destination.
- A template of an archived account no longer reuses its amount for another account; archiving the default account
  clears the default and says so. Receipt totals are read in the currency itself, never through a display unit.
- An account's currency stays locked while any stored amount is in it (entries, plans, templates, money set aside,
  budgets, a loan installment); the account form names them.
- Accounts are listed in groups – money, credit cards, owed to me, debts, valued assets – each with totals per
  currency; the default account is marked, and its details offer "Add entry here". Advanced: "Usable for payments"
  (the forecast minimum uses only such accounts) and an optional country.
- Home: archived accounts leave the current totals; the converted total says when a rate may be outdated or is an
  estimate; the line under quick add names the account; a balance that may fall below zero this month is shown under
  "Needs attention" in both modes; the budget, forecast and chart follow the default account's currency, with a
  currency switcher on the Budget page and alerts for budgets in every currency. The default Home shows budget, next
  payments and recent entries; the other sections are one tap away in Customize Home. The category chart shows gross
  spending like the reports.
- Display units (such as the toman) and the Home layout belong to the profile and are part of its backups.
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

### Changed – Windows title bar and lists (D-45)

- Windows: the window title shows the Zanance symbol and is readable in every combination of app and Windows theme
  (it was white on the light page when Windows was dark).
- Screen readers read each entry, plan and day of a list by its name and amount instead of a technical type name.
- Windows: transactions and loan installments are aligned with the headers instead of running to the window edge; an
  installment shows principal and interest and what remains on lines of their own.
- Windows: dialogs, help texts, picker lists and the date picker use the app's font (Vazirmatn for Persian) instead of
  the system font (D-47).
- Windows: the window cannot be made narrower than a small phone (360 px), and the Insights tabs fit at that width.
- The "?" help buttons are easier to hit: the circle is the same, the area that reacts to a tap is larger (D-48).

### Changed – Faster start (D-44)

- The app starts faster: the database model is prepared ahead of time instead of at every start, and the database is
  only upgraded when an update brings a change.
- Android: the database is prepared when the window opens, no longer while the system starts the app, which could
  show "Zanance isn't responding" on slow devices, e.g. right after an update or for a notification (D-46).

### Changed – Insights on Windows (D-43)

- Windows: Budget, Reports, Forecast and Goals are visible tabs at the top of these pages instead of a hidden drop-down.
- Screen readers no longer read out dividers and colour dots.
- Countries in Settings are named in the app language (they were shown in their own languages, e.g. "Deutschland" in
  English).
- The currency boxes have the same arrow as the other lists, and the chosen currency is marked in blue instead of green.

### Changed – Keyboard and screen readers (D-42)

- Everything you can tap is a real button: rows, cards, category pills, colour and icon choices and the "All" links can be
  reached with the Tab key on Windows and are announced as buttons by screen readers.
- Budget and Reports: the previous and next month are round buttons instead of bare arrows.
- Windows: the focus ring is blue on the tabs too, and follows the light or dark theme.
- The plan editor shows a missing amount next to the field at the same time as a missing name.
- Home quick add: "Umbuchung" (German) is no longer cut off.

### Changed – Wide windows and the first days (D-41)

- Windows and tablets: in a wide window every page is a centred, readable column instead of cards across the screen.
- Getting started on Home for new users: first expense, a regular payment and a monthly budget, ticked off as you go.
- Reports without entries show one "Add entry" card; the forecast without plans explains how to get one and offers
  "Add plan"; money owed to you has a proper empty state.
- Exchange rates start from the currency of one of your accounts (or the US dollar) instead of the first in the list.
- Windows: the keyboard focus ring is blue now (the earlier change did not take effect).

### Changed – Whole-app review (D-40)

- Reports: a positive result is green, a negative result and negative balances are red; the trend table has column
  titles. Chart grid lines and axes are calm in the dark theme.
- Dark theme: the "?" help buttons and the currency box are no longer white.
- Entry details: the further actions (refund, duplicate, make recurring, rules, template) are one list with icons.
- Quick templates in the editor show their category icon, like on Home; the move buttons of Customize Home are small.
- Dates in refunds and goal history are written out ("Thursday, October 1, 2026").

### Changed – More, Settings and About (D-39)

- More: every row says in one line what it holds; Settings and the new About Zanance are in their own group "App".
- About Zanance: version, what happens with your data, and the open-source licences with their full texts.
- Settings grouped by what they change: language and region, money and months, appearance, experience, privacy and
  security, notifications, delete data. The reminder time has a label.
- The add button has no square around it any more (Android); the Windows time picker shows 24 hours instead of an
  empty AM/PM column; quick templates show their category colour; money owed to you is shown in sky blue.
- Persian digits keep the dots of version numbers (۰.۱.۰.۱) instead of turning them into decimal commas.

### Changed – Dark theme and permissions (D-38)

- The dark theme is a deep navy that matches the blue of the symbol (it looked green-grey before); on Android the
  splash screen is dark too when the device is in dark mode.
- After the first start Zanance offers reminders once, explains what they do and then lets the system ask. "Not now"
  is respected; "Turn on" in Plans and Settings opens the notification settings when the system no longer asks.
- Receipt: on phones you can take a photo right away. Android uses the camera app and needs no camera permission;
  iOS asks for the camera with a short explanation and points to the settings after a refusal.
- With the app lock on, a receipt taken or chosen outside the app opens only after unlocking.

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

### Added – Cloud backup on every platform (D-50)

- OneDrive and Google Drive backups now on Android, iOS and Windows. On iOS both sign in through Safari's secure
  sign-in sheet; on Windows Google opens your browser and the app receives the answer on this computer only. No client
  secret is used, and the access stays on the device (keychain on iOS, protected for your Windows user on Windows).

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

- Automatic cloud backups. Cloud backup is not yet verified on a device with real OAuth clients; the iOS app has not
  been built on a Mac yet.
