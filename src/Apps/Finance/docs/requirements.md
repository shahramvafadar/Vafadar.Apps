# Finance – requirements

> Status: **to be defined.** This document collects the functional requirements before implementation starts.
> The questions below are the starting point; answers turn into user stories and the domain model.

## Vision

A simple, private, offline personal finance app that makes it effortless to record income and expenses and shows
clearly where money comes from and goes to.

## Questions to answer

### Accounts and money

- Which kinds of accounts are needed (cash, bank account, card, savings, loans, …)?
- Which currencies? Only one per user, or several (e.g. EUR and IRR/Toman)? Are exchange rates needed, and if so,
  entered manually or fetched?
- For Iranian currency: display in Rial or Toman?
- Transfers between accounts?

### Transactions

- Fields per transaction: date, amount, account, category, payee, note, tags, attachment (receipt photo)?
- Split transactions (one payment, several categories)?
- Recurring transactions (rent, salary, subscriptions) with reminders?
- Quick entry from the home screen / a widget?

### Categories

- Predefined categories (translated) plus user-defined ones? Sub-categories?
- Separate category sets for income and expenses?

### Budgets and goals

- Monthly budgets per category with warnings?
- Savings goals?

### Reports

- Which reports: monthly overview, by category (pie), trend over months (line/bar), income vs. expenses, account
  balances over time, custom date ranges?
- Report periods in the Persian calendar (Persian months) when the Persian calendar is selected?
- Export: CSV / Excel / PDF?

### Other

- App lock with PIN / fingerprint?
- Import from bank statements (CSV) or other apps?
- Multiple users / profiles on one device?
- Which features would be "Pro" (if any)?

## User stories

_To be written from the answers above._

## Out of scope for the first version

_To be decided._
