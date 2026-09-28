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

### Not yet included

- Backup to Google Drive or OneDrive (needs the sign-in configuration), public-holiday calendars, widgets and OCR.
