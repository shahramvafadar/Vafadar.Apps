# Finance — Product Requirements and Business Design Specification

**Version:** 1.1  
**Baseline date:** `2026-09-25` (v1.0); revised `2026-09-26` (v1.1: aligned with the implemented Phase 1, see Section 31)  
**Product name:** Zanance (decided 2026-09-26; "Finance" is the working name used in this document and in the code)  
**Existing application ID:** `pro.vafadar.finance`  
**Primary audience:** The product owner and the development team, for detailed design, planning, and incremental implementation  
**Status:** Product baseline. Section 31 summarizes the implementation status; it is not a confirmation of release readiness  
**Document language:** English, with stable identifiers for references in code, tests, and documentation

> This document defines what the product must do, for whom, and with what acceptable behavior. The development team may choose the specific class structures, implementation patterns, packages, and implementation approach, provided that the existing architecture and product rules are respected. All Phase 2 capabilities are future requirements, not instructions to implement them alongside Phase 1.

---

## 0. How to Use This Document and Its Limitations

### 0.1. Sources of Decisions

This document is based on decisions approved by the product owner. Version 1.0 was written before the repository was inspected; the verified state of the repository and all implementation decisions are recorded in the design documents in `src/Apps/Finance/docs` (starting with `01-assessment-and-decisions.md`). Version 1.1 updates the sections that described the assumed state (Sections 2, 14.2, 28 and 29) and adds the implementation status (Section 31). Requirements themselves are unchanged unless marked.

Requirements with identifiers such as `TX-01` and `REC-01` can be referenced individually. “Must” denotes a product requirement for the specified phase. All numerical examples in this document are hypothetical and are not the product owner's actual financial data.

In the event of a conflict, the order of precedence is: a new explicit decision by the owner, financial correctness and data protection rules, the requirements in this document, and then the proposed implementation. Conflicts with existing documentation must be recorded and resolved; the existing architecture must not be silently replaced.

### 0.2. Meaning of the Phases

| Label | Meaning |
|---|---|
| Phase 1A | The first increment suitable for personal use; establishes calculation correctness and everyday recording. |
| Phase 1B | Completes Phase 1 for limited public testing and then release, subject to quality gates. |
| Phase 2A | Advanced capabilities that are primarily local and do not require an ongoing service operated by us. |
| Phase 2B | Online services or integrations; each requires an independent decision about cost, security, and service terms. |

Phases 1A and 1B together constitute “Phase 1.” The same relationship applies to Phases 2A and 2B. These labels are not delivery dates or commitments to release every future capability.

### 0.3. Design Documents

The design documents derived from this specification live in `src/Apps/Finance/docs` in the repository: assessment and decision log, domain design, UX design, Phase 1 plan, Phase 2 backlog, privacy matrix, acceptance test plan, and release checklist. They record what is actually implemented and verified. Section 29 lists them.

---

## 1. Product Definition, Problem, and Value Proposition

### 1.1. Definition

Finance is a **local-first, international, and extensible** personal finance manager. Users must be able to record their income and expenses, keep track of due dates, review their budgets, and plan ahead based on the information they have recorded, without creating an online account.

The objective is not to build a small business accounting system or to maximize the number of screens. It is to answer a few everyday questions correctly:

- How much income and spending have I recorded so far, and what are my account balances?
- Which payments and receipts are coming up, which are overdue, and which have not yet been confirmed?
- How much have I spent against my budget, and which non-monthly expenses are approaching?
- Based on the information I have recorded, what is my estimated end-of-period balance?
- How can I preserve, restore, or export my data without mandatory dependence on the developer's service?

### 1.2. Non-Negotiable Principles

| ID | Principle |
|---|---|
| PR-01 | Core capabilities must work offline and without registration. |
| PR-02 | Correct amounts, dates, balances, and reports take precedence over the number of features. |
| PR-03 | Posted records, future plans, and forecasts must not be conflated. |
| PR-04 | Simplicity is the default; greater capability is revealed progressively. |
| PR-05 | Language, country/region, currency, and calendar must not dictate one another. |
| PR-06 | Users must be able to export their data; security or data export must not be used to force a purchase. |
| PR-07 | No shared capability, SDK, or online service may be introduced into Finance merely because another app uses it. |
| PR-08 | “Recording a payment” must never be presented as “executing a bank transfer.” |
| PR-09 | Do not apply the MIT License or another open-source license to the product at this stage; comply with third-party dependency licenses. |
| PR-10 | The final brand name, logo, and prices have not been determined; the implementation must not treat them as settled. The app uses a neutral placeholder icon until then. |

### 1.3. Target Users and Baseline Scenarios

An everyday user with one account and simple expenses; someone with multiple bank and savings accounts; a user with weekly or fortnightly income or rent; an immigrant whose language differs from their country of residence; a user who makes foreign-currency purchases; and someone who does not want to connect their bank account or financial information to our service.

Expenses for a child, car, or trip are initially managed through categories and descriptions. This need does not, by itself, imply creating an online account for a child or building a product targeted at children.

### 1.4. Outside the Product's Identity

Phase 1 is not a payment tool, bank, custodial wallet, lending platform, formal business accounting system, tax return application, or investment adviser. An “account” displayed in the app is simply a record of the user's finances, not an actual account opened or administered by the app.

---

## 2. Current State and Alignment with Shared Libraries

Verified state of the repository on `2026-09-26` (v1.0 listed the owner's report; paths carry the `Vafadar.` prefix):

| Component | Verified state | Requirement for further work |
|---|---|---|
| `src/Libraries/Vafadar.Core` | Entity base (GUID v7), audit interface, settings and app-environment abstractions, digit normalization (Persian/Arabic/Latin). | Preserve common rules; do not introduce finance concepts into the generic core without a reason. |
| `src/Libraries/Vafadar.Localization` | English, Persian, German with runtime switching, RTL, Gregorian/Persian display calendars independent of language, date formatting. | Preserve this independence and test it across all screens and reports. |
| `src/Libraries/Vafadar.Data` | SQLite with EF Core, UTC timestamps, audit interceptor, startup migrations, SQLite snapshot backup source. | Migrations must only add to the schema; sample data must not replace the user's database. |
| `src/Libraries/Vafadar.Backup` | Backup package with manifest and SHA-256 checksums, AES-256-GCM encryption with a password, validation of app id, format and app version before restore, inspection without restore, retention of 10 versions, optional content summary. | Keep encryption, integrity and restore behavior covered by tests. |
| `src/Libraries/Vafadar.Backup.GoogleDrive`, `…OneDrive` | Storage classes tested against fake HTTP only. | Not usable until real sign-in exists (Section 17.4); hidden in the app. |
| `src/Libraries/Vafadar.Authentication` | Interfaces only; no Google/Microsoft sign-in. | Fake login flows or assumptions that OAuth is ready are prohibited. |
| `src/Libraries/Vafadar.Maui` | Startup (`UseVafadar`), Syncfusion licensing, `{v:Translate}`, RTL helpers, date field (Gregorian/Persian), choice chips, converters, device authentication for app locks. | Use the existing approach; verify asset and license rights for a release. |
| `src/Apps/Finance` | Phase 1 implemented except cloud backup (Section 31). | Extend this app; do not create a parallel app or repository without justification. |
**AR-01 — Sharing boundary:** Localization, backup, app lock, reminder infrastructure, and the mechanism for optional capabilities may be reusable. Balance calculations, financial recurrence, budgeting, refunds, and forecasting belong to the Finance domain; generalizing them into a shared library requires a real need.

**AR-02 — No unjustified redesign:** Implementation must first understand existing decisions. This document does not authorize automatic changes to the framework, architecture, or technology versions.

**AR-03 — App data isolation:** Each app must have its own data, sensitive settings, backups, and connection scope. Sharing a library must not make Finance data visible in another app.

**AR-04 — Platform priority:** Android is the first release platform. Preserve other platforms that actually exist in the project, but do not claim equivalent support or readiness without testing them.

---

## 3. Confirmed Product Decisions

| Topic | Decision |
|---|---|
| Online app account | Optional; not part of core usage in Phase 1. |
| Local data ownership | One personal workspace per installation; multiple independent in-app profiles and family use are future capabilities. |
| Source of data | Local data; optional cloud backup, not synchronization. |
| Financial accounts | Multiple accounts, including cash, current/checking, savings, and a simple credit card account. |
| Financial events | Income, expense, and transfer; refunds and adjustments have distinct meanings. |
| Recurrence | Every N days/weeks/months/years, with explicit dates and edge-case behavior. |
| Recurring posting | Manual confirmation by default; optional automatic posting for fixed amounts, marked “Unreviewed.” |
| Budget | Overall monthly limit and category limits in Phase 1; advanced approaches in Phase 2. |
| Currency | A currency per account; foreign-currency purchases with the actual debited amount and a manual exchange rate; online rates later. |
| User experience | Simple and Advanced modes, with shared calculations and data rather than two separate products. |
| Pricing | No active payments in Phase 1; prepared for a one-time Pro purchase. Services with ongoing costs are assessed separately later. |
| AI | Not required for the core; conditional Phase 2 capability. Do not plan a product flow that asks users for API keys. |
| Bank connection | Phase 2; manual entry and file import come first. |
| Advertising | No advertising SDKs or advertising tracking in Phase 1. Any future use requires a new decision. |
| Code and ownership | Closed/proprietary for now; do not automatically add a new open-source license. |

---

## 4. Capability Map and Phase Boundaries

| Area | Phase 1: Dependable product | Phase 2: Optional expansion |
|---|---|---|
| Onboarding | No login, independent settings, first account, opening balance. | Multiple independent local profiles and specialized guidance. |
| Recording | Income, expense, transfer, notes, icons, refunds, corrections. | Split transactions, attachments, tags, and advanced bulk operations. |
| Accounts | Cash, current/checking, savings, simple credit card; archiving and manual reconciliation. | Debts/receivables, loans, assets, statement settlement, and specialized installment models. |
| Planning | Flexible one-off and recurring schedules, edit one occurrence/future occurrences, fixed or estimated amount. | Multiple days per month, last business day, complex calendar rules, and contractual changes. |
| Reminders | Local, configurable, due-date center, snoozing, privacy protection. | Advanced summaries, calendar export, and optional channels. |
| Budget | Overall monthly budget, category limits, remaining amounts, and warnings. | Rollover, Envelope/Flex, pay cycles, and setting aside funds for future expenses. |
| Forecast | End-of-month and short-horizon projections based on recorded plans. | Scenarios, longer horizons, and variable-spending estimates with transparent assumptions. |
| Goals | Display periodic equivalents for non-monthly commitments. | Savings goals, actual/notional allocation of funds, priorities, and periodic contributions. |
| Reports | Spending chart, income/expense comparison, trends, and traceable details. | Customizable dashboard, saved reports, PDF, and advanced analysis. |
| Currency | Independent account/reporting currencies; manual conversion and recorded exchange amounts. | Historical/daily rates from an optional service. |
| Data | Safe CSV, import with preview, validated backup and restore. | More bank formats, advanced import, and OCR with user review. |
| Online services | Optional backup in the user's own storage once the integration is complete. | Real synchronization, family sharing, banking, and AI; each with a separate gate. |

**SC-01:** “Phase 2” must not justify an incorrect Phase 1 model. For example, future split transactions must remain possible to add, but Phase 1 does not need all split-transaction screens and code.

**SC-02:** The app must not be filled with unusable buttons and “Coming soon” placeholders. Future capabilities must remain outside normal navigation until ready.

---

## 5. Financial Concepts and Correctness Rules

### 5.1. Glossary

| Concept | Product definition |
|---|---|
| Financial account | A container for recording the balance and movements of a source of funds or a liability; not a user login account. |
| Transaction | A record of an event that occurred, or an automatically posted event explicitly marked as unreviewed. |
| Financial schedule | An expected receipt, payment, or transfer, either one-off or recurring. |
| Occurrence | A specific due event generated by a schedule, independent of the complete series. |
| Original due date | The date produced by the schedule's rule. |
| Overridden due date | A date changed by the user for that occurrence only. |
| Transaction effective date | The date to which the movement of funds is attributed; the basis of cash-basis reporting. |
| Creation/modification time | When data was recorded in the app; not a replacement for the financial date. |
| Posted | Entered in the app's ledger, either manually or automatically. |
| Confirmed | The user has confirmed the event/amount; this does not mean online bank verification. |
| Unreviewed | Entered automatically or through a review workflow and not yet confirmed by the user. |
| Budget | A planned limit or allocation, not a movement of money. |
| Forecast | A future calculation based on posted balances and open schedules, with visible assumptions. |

### 5.2. Core Rules

**FIN-01 — Amount direction:** In the form, users choose the event type and enter the absolute amount. Display income as positive and expenses as negative. Entering a negative number must not cause double negation or a hidden type change; the app must clearly normalize or reject it.

**FIN-02 — Transfers:** Transfers between the user's own accounts are not income or expenses. Both sides must be presented as one linked operation and edited or deleted together. Reject transfers to the same account. A transfer fee is a separate expense.

**FIN-03 — Account scope:** When a balance report includes only one account, a transfer out of that account affects its balance, but must still not become income/expense in an income-and-expense report. An account activity report may show transfers separately.

**FIN-04 — Opening balance:** The opening amount and date must be explicit. The opening amount is the balance at the start of the selected day, before that day's transactions. Importing history before this date requires an explicit decision about the balance baseline, rather than double-counting history.

**FIN-05 — Monetary precision:** Addition, division, conversion, and rounding must follow explicit monetary rules; do not assume every currency has two decimal places. The sum of displayed values must not differ inexplicably from the total.

**FIN-06 — Zero and negative values:** Do not post ordinary income/expense transactions with a zero amount. Zero or negative account balances are valid. A zero budget is a valid “Do not spend in this category” setting and must not cause division by zero.

**FIN-07 — Future dates:** By default, a future-dated transaction is a plan, not income or spending that has already occurred today. Early confirmation of an occurrence must preserve the actual payment date separately.

**FIN-08 — Reporting basis:** Phase 1 uses a cash basis: the effective receipt/payment date determines reporting. The due date, record creation time, or billing period must not replace it. Phase 1 does not provide accrual accounting.

**FIN-09 — Corrections:** Changes to an amount, date, or account must consistently update all views, balances, budgets, and reports. Undo and explanations of important changes must be available. This history is not presented as a statutory audit ledger.

**FIN-10 — Actual income:** Opening balances, transfers, repayment of debt principal, and purchase refunds must not be counted as new income to make income figures appear higher.

### 5.3. Automatic Posting and Trust in the Numbers

**FIN-11:** An ordinary manual entry represents user confirmation. Automatic posting of a fixed schedule creates a “Posted/Unreviewed” transaction, not a claim that the bank account was observed.

**FIN-12:** The main ledger balance includes all transactions posted up to the selected date. When unreviewed entries exist, use “Posted balance — includes unreviewed entries” or an equally clear label. The count and amounts of these entries, and a “Confirmed only” filter, must be accessible.

**FIN-13:** Automatic posting is off by default in Simple Mode. Dashboards and reports must not label a manually maintained ledger balance as a “Live bank balance” or “Guaranteed amount available to spend.”

**FIN-14:** Wherever “Actual” appears in documentation or the UI, it must map explicitly to either “Posted” or “Confirmed only.” These two sets must not be mixed under identical labels across cards and reports on the same screen.

---

## 6. Accounts, Balances, and Reconciliation

**Phase:** 1A; complete the multi-currency experience in 1B.

**ACC-01 — Account creation:** Allow configuration of name, type, currency, opening balance, baseline date, icon, and inclusion in the main total. Opening an actual account or signing in to a bank is not required.

**ACC-02 — Default account:** Users can select a default account for quick entry. When there is only one account, hide account selection in the simple form, but keep the account visible in the details.

**ACC-03 — Basic types:** Cash, current/checking, savings, and simple credit card. Creating a custom name must not depend on a particular country or bank.

**ACC-04 — Credit cards:** A card purchase is an expense and increases debt. A payment from a current/checking account to the card is a transfer, not a second expense. Display a negative balance as debt; a positive card balance is also valid. A credit limit must not count as cash or a spendable asset. Automated interest, fees, and loan statements are Phase 2 capabilities; users can manually record interest/fee expenses in Phase 1.

**ACC-05 — Balances:** Each account's balance must always be available in its own currency. Show combined totals only with an explicit exchange rate and conversion basis. Recorded assets/liabilities and available liquid funds are separate concepts.

**ACC-06 — Archiving:** Archive an account with history instead of destructively deleting it. Its history remains in past reports. Archiving must not silently leave future schedules without an account; move or stop those schedules, or require a replacement account.

**ACC-07 — Changing account currency:** Merely relabeling the currency of an account with activity is prohibited. Changing its actual monetary unit requires a new account or an explicit conversion process. Changing the reporting currency is a different operation and must be possible without deleting history.

**ACC-08 — Manual reconciliation:** Users enter an observed actual balance and its date; the app shows the difference from the ledger. First suggest reviewing missing, duplicate, or unreviewed transactions. Permit an adjustment with confirmation, a reason, and a distinct marker; exclude it from income/expense and budgets by default.

**ACC-09 — Incomplete data:** An unknown opening balance or an account not included in the app must trigger a data-quality indicator. The app must not claim to show a person's complete net worth from incomplete coverage.

---

## 7. Financial Entries, Descriptions, Categories, and Icons

### 7.1. Forms and Operations

**TX-01:** Income and expense entries must include amount, type, date, account, and category. Allow “Uncategorized” for speed. A short title, description, and notes are optional; users must not be forced to write lengthy text for a simple purchase.

**TX-02:** Every item has a display title: the user's title, or the category name when no title is provided. Title, counterparty/merchant, and notes are three different concepts. The simple form may show only the title and a notes button.

**TX-03:** Support multiline descriptions for details such as a bill reference number or the reason for an expense. Do not disclose this text in notifications, support reports, or shared exports without an informed user choice.

**TX-04:** Provide editing, deletion with Undo, duplication, quick-template creation, and conversion into a recurring schedule. Duplicating a record must never inherit the payment link to its original occurrence.

**TX-05:** Group the list by financial date; make the daily total, type, account, icon, notes, and review status clear. Phase 1 includes title/notes search and filters for date, category, account, type, amount, and status.

**TX-06:** Repeated saving caused by multiple taps, returning from a notification, or UI interruptions must not create duplicate transactions. A save failure must preserve the user's input.

**TX-07:** Do not transmit amounts, notes, or actual user data through test screens or error reports. Use demo data only by user choice and in a space that can be cleared.

### 7.2. Categories

**CAT-01:** Provide an editable starter set: salary, side income, gifts received, interest; housing, groceries/food, utilities, communications, transport, health, insurance, education, family, leisure, subscriptions, and other. Do not create sample data as actual transactions.

**CAT-02:** Support custom categories, renaming, ordering, colors and icons, archiving, and informed category merging. Deleting a category in use must not delete its transactions.

**CAT-03:** A category and subcategory level is sufficient for Phase 1. Aggregating parents and children must not count an expense twice. Distinguish translated default-category labels from user-defined names.

**CAT-04:** “Monthly/non-monthly,” “fixed/variable,” and “essential/optional” are schedule attributes or analytical choices, not reasons to require duplicate categories such as “Car-monthly” and “Car-annual.”

### 7.3. Visual Language

**VIS-01:** Use a light, minimal design with adequate spacing. Income uses dark green, expenses use dark red, and transfers use a neutral/distinct color. Color intensity must not reduce text readability.

**VIS-02:** Color must not be the sole carrier of meaning; provide a sign, type label, and icon as well. Positive/negative amounts must remain correctly positioned in RTL layouts.

**VIS-03:** Provide a predefined, appropriately licensed icon set that works offline. Users can choose icons for categories and individual items. An item's default icon comes from its category; a custom item icon must not silently disappear when its category changes.

**VIS-04:** Icons have readable names for accessibility and search. Users must not need to upload an image or install a font package. Category icon colors must not be confused with the semantic colors for income/expense.

### 7.4. Refunds and Corrections

**REF-01:** Phase 1 supports full and partial purchase refunds, preferably linked to the original purchase. Credit the actual receiving account even when it differs from the purchase account.

**REF-02:** By default, a purchase refund reduces spending in the same category; it does not become employment/general income. Increase the account balance on the date the refund is received.

**REF-03:** A refund for an older purchase can make the current month's net spending negative. This is valid; charts and budgets must explain it rather than remove the amount or clamp it to zero.

**REF-04:** Linked refunds must not exceed the refundable amount unless the user explicitly classifies the excess as another event. Also allow refunds for purchases outside the app's history, marked “No original record.”

**REF-05:** Do not collapse a typo correction, an actual refund, and a repayment of income into one operation. Reversing income must reduce the relevant income rather than appear as a purchase expense. This workflow may live under advanced options.

---

## 8. One-Off and Recurring Schedules

### 8.1. Schedule Details

**REC-01:** Income, expenses, and transfers between the user's own accounts can have one-off or recurring schedules. Each schedule includes a name, account/account pair, relevant category, amount or estimate, currency, notes, icon, start date, recurrence calendar, and reminders.

**REC-02:** One-off schedules are required for future payments such as repairs or travel. Forcing users to create a recurrence and delete it afterward is unacceptable.

**REC-03:** An amount is “Fixed,” “Estimated/variable,” or “Not yet known.” Unknown is not the same as zero. Exclude unknown amounts from numerical forecasts and flag the result as incomplete.

### 8.2. Phase 1 Recurrence Options

| Option | Example |
|---|---|
| Every N days | Every 10 days. |
| Every N weeks | Every week, every 2 weeks, every 3 weeks. |
| Every N months | Monthly, every 2 months, every 3 months, every 6 months. |
| Every N years | Annually, every 2 years. |
| One-off | A single due date without recurrence. |

**REC-04:** N must be a positive integer. Before saving, preview at least the next six due dates so that users can understand their selection.

**REC-05:** “Every two weeks” differs from “Twice a month.” Phase 1 directly supports the former. Multiple fixed days per month are added in Phase 2A; until then, users can create two separate schedules, without incorrectly labeling them fortnightly.

**REC-06:** A schedule ends in one of three ways: never, on a date, or after a specified number of occurrences. The count is the number of scheduled occurrences; skipping one must not automatically append another to the end of the contract. Postponing that same occurrence must not increase the count either.

**REC-07:** A start date in the past requires an impact preview. Automatically creating large numbers of past transactions without user choice is prohibited. Default to “From today onward” or a review of past occurrences.

### 8.3. Month Ends, Leap Years, and Calendar Basis

**REC-08:** “Specific day of the month” and “Last day of the month” are independent options. For a day missing from the target month, let users choose “Last valid day” or “Skip that occurrence”; default to the last valid day.

**REC-09:** Preserve the rule's anchor: a schedule for the 31st that falls on the 28th in a short month must return to the 31st in the next applicable month, rather than permanently shifting to the 28th.

**REC-10:** Apply the same principle to leap dates and the Persian calendar. Make invalid-date behavior explicit; do not silently choose another date or month.

**REC-11:** Specify each rule's calendar when it is created. Changing the app's display calendar must not convert a Gregorian rent schedule into Persian months. Phase 1 supports monthly/yearly rules based on Gregorian and Persian calendars; additional calendars require independent testing before release.

**REC-12:** Business-day shifts, public holidays, the last Friday of the month, and complex combinations belong to Phase 2A. Selecting a country in Phase 1 must not, by itself, shift payment dates.

### 8.4. Occurrences and Actions

**REC-13:** Occurrence status is separate from series status: upcoming, due, overdue, completed/linked to a posted entry, skipped, and cancelled. “Overdue” is derived from the date and the occurrence remaining open. “Automatically posted, unreviewed” needs its own indicator.

**REC-14:** Occurrence actions include confirming a payment/receipt, changing the amount, changing the actual date, postponing the due date, skipping, adding notes, and linking an existing transaction. In Phase 1, each occurrence has at most one active settlement entry; partial payments and multiple transactions for one occurrence are added in Phase 2.

**REC-15:** When editing, users choose “This occurrence only” or “This and future occurrences.” A price change from next month must not rewrite previously settled amounts. Editing the entire history is permitted only through an explicit, reviewable correction workflow.

**REC-16:** Pausing and ending a schedule are different operations. Pausing must not delete previously paid occurrences. Resuming requires a preview of the restart date.

**REC-17:** When a transaction already exists from manual entry or import, users can link it to an occurrence instead of creating a second transaction. Matching suggestions require confirmation; similar amounts and names alone do not authorize merging.

**REC-18:** Deleting or undoing confirmation of an occurrence must explain the effect on the linked transaction and schedule. Undoing or deleting an automatically posted entry must not immediately recreate it; return the occurrence to review or the user's selected state.

### 8.5. Automatic Posting

**REC-19:** Automatic posting requires an informed, per-schedule choice. In Phase 1, allow it only for fixed amounts with complete information. An unknown amount, missing exchange rate, or archived account requires user review.

**REC-20:** Automatic posting only changes the app's ledger; it does not execute an actual payment. Users must see this distinction when enabling it. Notifications must say “Recorded; please review,” not “The bank made the payment.”

**REC-21:** Reopening the app, rerunning processing, returning from a notification, or restoring a backup must not create multiple transactions for one occurrence. Conversely, two independent schedules with identical names and amounts must not be mistakenly merged.

**REC-22:** If the app has not run for some time, calculate due occurrences correctly when the user returns and provide a summary of outstanding items. Ledger correctness must not depend on guaranteed background execution.

---

## 9. Reminders and the Due-Date Center

**Phase:** 1B; design the behavior from the start of Phase 1.

**REM-01:** Support reminders for one-off and recurring schedules: on the due date, a specified number of days beforehand, and at a chosen time. Provide one default reminder and multiple reminders in Advanced Mode. Suggested initial default: 3 days beforehand at 09:00; users can change or disable it.

**REM-02:** The in-app due-date center shows upcoming, today, overdue, and unreviewed items even when notification permission has not been granted. Denying permission must not disable recording, budgeting, or reports.

**REM-03:** Request permission when the user enables reminders, not without explanation on first launch. Clearly show whether notifications are enabled and how to change that status. Include Android permission limitations in actual device testing. [S03]

**REM-04:** A notification opens the relevant occurrence. Snoozing a notification does not change the financial due date; rescheduling the occurrence is a separate action. Remove related future notifications after an occurrence is confirmed, skipped, or cancelled.

**REM-05:** Default lock-screen notifications to generic text such as “A financial item needs your attention.” Show amounts, account names, or descriptions only by user choice. Respect app lock when opening a notification.

**REM-06:** Notification action buttons must not record payments or reveal sensitive information without respecting app lock. Repeated taps must not create multiple entries.

**REM-07:** Rebuild valid schedules and remove duplicate notifications after device restart, clock changes, time-zone changes, data restoration, and language changes. If a reminder time does not exist during a daylight-saving transition, move it to the first subsequent valid time; run only once during a repeated hour.

**REM-08:** Preserve the financial due date independently of notification time. By default, notification time uses the device's local time; travel must not automatically shift a payment's calendar date by a day.

**REM-09:** Do not promise guaranteed reminders at an exact second. Scheduling methods and permissions must fit platform limitations; ordinary financial reminders must not unnecessarily depend on restrictive exact-alarm permissions. [S04]

**REM-10:** Provide controllable summaries for multiple due items and limit overdue notifications. After a month of inactivity, do not send dozens of notifications at once. A summary notification should open the review list.

---

## 10. Monthly Budgets and Non-Monthly Expenses

### 10.1. Phase 1 Budgets

**BUD-01:** Allow an overall spending budget for a month; it is optional. No budget must not be represented as zero or as an error.

**BUD-02:** Allow category/subcategory limits in Advanced Mode. Category budgets analyze a subset of the same expenses; an expense must not be counted once in the overall budget and then again in total spending.

**BUD-03:** Users choose which accounts and categories a budget covers. Make the default explicit: posted expenses from selected personal accounts, reduced by refunds, excluding transfers, opening balances, and balance adjustments.

**BUD-04:** Each budget card shows its limit, net posted spending, remaining amount, and percentage used. Make any unreviewed entries included in spending visible, and retain a “Confirmed only” option.

**BUD-05:** Explicitly display usage above 100%; a chart must not conceal the actual value. For a zero budget, percentage usage is undefined; show the overspent amount in text instead. Do not clamp negative net spending caused by refunds to zero.

**BUD-06:** Near-limit and over-limit warnings are optional and can be disabled. Suggested initial thresholds are 80% and 100%; each edit or screen visit must not generate another warning.

**BUD-07:** Allow budget settings to be copied to the following month with a preview. Editing the current month's budget must not automatically rewrite budgets for previous closed months. “From this period onward” must be a separate choice.

**BUD-08:** Specify the budget currency and period calendar. Changing reporting currency must not relabel a EUR 100 budget as USD 100. Changes to the calendar basis require confirmation and apply at future period boundaries; historical budgets remain viewable.

### 10.2. Non-Monthly Commitments

**BUD-09:** Annual, quarterly, and similar schedules have a visible “Approximate/normalized monthly equivalent.” This figure is for comparing spending commitments; it does not automatically create transactions or consume budget.

Example: annual insurance of 720 units has a monthly equivalent of 60 units. However, if only three saving opportunities remain before payment and 180 units have already been set aside, completing the goal requires 180 units at each remaining opportunity, not 60. Actual goal-funding calculations are implemented in Phase 2A.

**BUD-10:** For daily/weekly recurrences, calculate the exact total for a period by counting the occurrences within it. An approximate annual-to-monthly conversion must not replace the actual number of receipts/payments in that month.

### 10.3. Phase 2 Advanced Budgeting

Rollover with choices for carrying forward surpluses and handling deficits; Envelope/Flex with understandable allocations; pay-cycle or custom-start periods; weekly/fortnightly budgets; saving for non-monthly expenses; planned-versus-used comparisons; and suggestions for adjusting limits.

**BUD-11:** Allocating funds to a budget or goal is neither an actual bank transfer nor an expense. Distinguish notional earmarking, actual movement of money, and final spending.

**BUD-12:** Using multiple budgeting methods together must not deduct the same amount twice from “Unallocated money.” Full Envelope or Flex budgeting is optional; simple users must not be forced into allocation-based accounting.

---

## 11. Balance Forecasting and Planned Versus Actual

**Phase:** Schedule-based forecasting in 1B; scenarios and behavioral estimates in 2A.

**FOR-01:** Users can view projected balances and the contributing items for month-end, the next 30 days, and the next 90 days. These are initial example horizons; horizon selection must not be restricted to one calendar month.

**FOR-02:** The baseline formula for the selected account scope is:

```text
Projected balance on the target date
= Ledger balance on the baseline date
+ Open expected receipts through the target date
- Open expected payments through the target date
+ Net scheduled transfers into/out of the selected account scope
```

Transfers between two selected accounts cancel out. Separate fees affect spending. For foreign-currency amounts, make the exchange-rate assumption visible.

**FOR-03:** An occurrence whose transaction is already included in the ledger balance must not be counted again in the future, whether manually posted or automatically posted and unreviewed. The posted amount replaces that occurrence's estimate rather than being added to it.

**FOR-04:** Put open overdue items in an explicit “Overdue commitments/receipts” bucket. By default, include them once from the baseline date and label this as an assumption. Users can change the expected date or exclude an item from a scenario; doing so must not mark it as paid.

**FOR-05:** Distinguish fixed, estimated, and unknown amounts. A missing rate or unknown amount makes the result “Incomplete”; do not treat it as zero or present a definitive total.

**FOR-06:** Phase 1 does not statistically forecast spending that users have not scheduled. State alongside the result that future everyday purchases, income changes, and other unrecorded items may be absent from the calculation.

**FOR-07:** Do not automatically deduct remaining budget or the monthly equivalent of an annual expense from the balance as a second outflow. In Phase 2, variable-spending estimates must add only the portion not already covered and include logic to eliminate overlaps.

**FOR-08:** Allow the projected balance path and minimum projected balance within the selected horizon to be displayed, not just the month-end figure. Important example: salary at month-end can produce a positive final balance even though an earlier payment causes a shortfall.

**FOR-09:** Do not use “Safe to spend” or guarantee the absence of a shortfall. An appropriate label is “Estimated balance after recorded plans.” Make the selected accounts, baseline date, presence of unreviewed items, and last calculation visible.

**FOR-10:** Phase 2A scenario analysis can simulate, for example, a delayed salary or a changed bill amount without changing the main ledger. Scenarios must not replace actual records, and every simulated change must be labeled as an assumption.

---

## 12. Dashboard and Reports

### 12.1. Home

**DASH-01:** Clearly display the period, reporting currency, and account scope at the top of the screen. Cards in the same view must use consistent filters for their calculations.

**DASH-02:** In Simple Mode, Home contains the main information: posted balance, period income and spending, remaining budget when configured, upcoming due items, and an action to add an entry. The spending chart may appear lower down so that charts do not obscure everyday recording.

**DASH-03:** Overdue or unreviewed items must lead to a specific action. A warning must be more than a red number; for example, “2 items need review” should be actionable.

**DASH-04:** Empty data, loading, errors, incomplete data, and no budget are different states. Do not confuse an actual zero with missing data. Do not show sample charts in the user's real workspace.

### 12.2. Phase 1 Reports

| Report | Expectation |
|---|---|
| Spending by category | Pie/donut chart accompanied by a table of amounts and percentages. |
| Income versus expenses | Period totals and a comparison chart; transfers kept separate. |
| Monthly trend | Selectable range, such as 6 or 12 months; shorter histories also display correctly. |
| Account activity | Traceable inflows, outflows, transfers, adjustments, and balances. |
| Budget | Limit, net spending, remaining amount, and overspending. |
| Planned versus posted | Expected amount, actual posted amount, variance, and open occurrences. |
| Recurring commitments | Period occurrence totals and appropriately labeled monthly equivalents. |

**REP-01:** Tapping any figure or chart segment must open the exact contributing transactions with the same filters. Users must be able to reconstruct the number.

**REP-02:** Do not construct a pie chart with negative values. Baseline design: a “Gross spending” chart, a “Refunds” card/column, and a “Net spending” table. This preserves refunds relating to previous months. The chart title must make its gross basis explicit.

**REP-03:** Show income, net spending, and the difference between them. An account's balance change does not necessarily equal that difference; transfers across the selected scope, opening balances, and adjustments must be explained separately in the balance movement breakdown.

**REP-04:** A period with no data, zero income, negative net spending, or percentage change from a zero baseline must have understandable text, not `NaN`, infinity, or fabricated charts.

**REP-05:** Clearly label comparisons between an incomplete current month and a complete previous month. Offer matched-day comparisons or explicitly show complete/incomplete periods; do not present the result as a definitive increase in spending.

**REP-06:** Renaming a category must not break its historical links. Archiving an account/category must not silently remove it from past-month reports. Make any filter exclusions explicit.

**REP-07:** CSV export is sufficient for Phase 1. PDF, print pagination, and formally formatted shareable reports belong to Phase 2 and must not be presented as official tax or bank reports.

**REP-08:** Saved reports, a customizable dashboard, tag/counterparty analysis, and advanced charts are Phase 2A capabilities. Add a chart only when it answers a specific question.

---

## 13. Internationalization: Language, Region, Calendar, and Currency

### 13.1. Independent Settings

| Setting | Suggested default | Rule |
|---|---|---|
| Interface language | Suggested from the device; changeable. | Must not change country or currency. |
| Region/country | Optional; suggested from device settings, not precise location. | Used only to suggest formats; not treated as residence or tax residence. |
| Reporting currency | Suggested according to the user's choice. | Must not alter account currencies or the units of historical amounts. |
| Display calendar | Selectable from calendars actually supported. | Changes presentation only unless the user separately changes the period basis. |
| Budget-period calendar | Independently selected when setting up a budget. | Defaults to Gregorian, with an explicit Persian option; preserve history. |
| Recurrence calendar | Specified when each schedule is created. | Changing the app's appearance must not change it. |
| First day of the week | Suggested by region; changeable. | Controls only relevant displays and periods. |
| Number/date/time formats | Configurable. | Ambiguous input requires clarification. |
| Reminder time | Device-local time with user configuration. | Travel must not change the financial date. |

**LOC-01:** The first release must provide complete, tested Persian, German, and English across all core workflows. Preserve any other languages already in the project and clearly report their completion status. Do not claim support for an incomplete language.

**LOC-02:** Language changes apply without restarting to open forms, error messages, charts, future notification titles, and settings; preserve input in progress. Do not automatically translate or rewrite user-entered names and notes.

**LOC-03:** RTL support covers form order, navigation, directional icons, amount signs, currency codes, and mixed Persian/Latin text. Do not mirror logos, charts, or non-directional icons without a reason.

**LOC-04:** Recognize Persian, Arabic, and Latin digits in input. Do not guess values such as `1,234` without considering the selected format; especially during import, preview the interpreted value.

**LOC-05:** Selecting a country must not trigger location access, GPS collection, or artificial restrictions to that country's banks. Global support is a design goal, not a claim to cover every bank, calendar, and law worldwide.

### 13.2. Currency and Conversion

**FX-01:** Each account has one primary currency. A transaction can retain both the original purchase amount in another currency and the actual amount charged in the account currency; for example, a USD 100 purchase with EUR 92 debited. A EUR 1 fee, if applicable, is separate.

**FX-02:** Users can manually specify the rate or the amounts on both sides of a conversion. Make the date, basis, and estimated/actual nature of the conversion explicit. Without a valid rate, do not present a multi-currency aggregate as definitive; display per-currency totals or an incomplete state.

**FX-03:** For a cross-currency transfer, preserve both the amount deducted from the source and the amount added to the destination. Fees are separate. Exchange-rate or rounding differences must not automatically create fictitious operating income or expenses.

**FX-04:** Historical flow reports use the amount/rate associated with the event. Valuing today's balance is a separate matter and requires a rate date. Changing today's reference rate must not rewrite a historical payment amount.

**FX-05:** Allow reporting currency changes without deleting information, while recognizing that older periods may need additional rates. Label results incomplete until those rates are supplied. One-to-one conversion or merely changing the currency symbol is prohibited.

**FX-06:** Budgets and goals retain their monetary units. Converting a display differs from changing a monetary commitment; permanently converting a goal/budget requires confirmation and preserved history.

**FX-07:** Phase 2 support for informal local units or representations differing from a standard currency requires an explicit unit, factor, and name. Do not infer such a conversion from Persian language or country settings.

**FX-08:** An online rate service is optional in Phase 2. Specify its source, timestamps, offline cache, cost, currency coverage, and the role of reference rates versus actual payment rates.

---

## 14. Simple / Advanced Modes and Experience Design

### 14.1. Differences Between the Modes

**UX-01:** Simple and Advanced are two presentation levels over one dataset and one calculation engine. This choice is unrelated to Free/Pro.

| Area | Simple | Advanced |
|---|---|---|
| Recording | Amount, type, category, title; date defaults to today. | More direct access to account, date, counterparty, currency, status, and additional options. |
| Notes and icons | A short “More details” path. | More direct controls. |
| Accounts | Main account, quick selection of another account. | All accounts, total scope, reconciliation, and currency. |
| Recurrence | Common options and a preview. | Custom intervals, calendar, month-end handling, end conditions, and future changes. |
| Budget | Overall limit and status. | Category limits and scope configuration. |
| Home | A few important cards and a clear action. | Unreviewed details, filters, and additional forecasts. |
| Reports | Simple questions and primary categories. | Precise filters, underlying records, and detailed metrics. |

**UX-02:** Switching from Advanced to Simple must not delete any data, budgets, schedules, or effective settings. If a hidden setting affects a figure, retain a summary and a way to access it; for example, “This report includes only 2 accounts.”

**UX-03:** Added form complexity should follow from the selected type. For example, show a destination account only after Transfer is selected; show conversion details only after a different currency is selected.

**UX-04:** Screen or class counts do not measure completeness. Frequent entry workflows must be short; the usability-testing target is to record an ordinary expense in approximately 10 seconds after familiarization, not a guaranteed promise for every user.

### 14.2. Suggested Navigation

Implemented navigation: four tabs **Home, Transactions, Plans, More**. "Plans" is the user-facing name for schedules. The new-entry action is a floating button on Home, Transactions and Plans. The budget and the forecast also open from their cards on Home. More contains accounts, budget, reports, forecast, categories, exchange rates, import/export, backup and settings.

### 14.3. Screen Inventory and Content Contracts

| ID | Screen/workflow | Expected outcome |
|---|---|---|
| UI-01 | Onboarding | Use without an online account, limited initial choices, financial account selection, and opening balance. |
| UI-02 | Home | Explainable financial status, quick entry, and next due item. |
| UI-03 | Add/edit entry | Short form, preserved input on error, and save feedback. |
| UI-04 | Transaction list/details | Search, filters, schedule links, and refunds. |
| UI-05 | Accounts | Balances, types, currencies, creation, and archiving. |
| UI-06 | Account details/reconciliation | Activity and an explanation of differences from the expected balance. |
| UI-07 | Schedules | Calendar/grouped due-date list, including overdue/unreviewed states. |
| UI-08 | Create/edit schedule | Amount, recurrence, calendar, preview, and reminders. |
| UI-09 | Review an occurrence | Record actual amount, change due date, and link an existing entry. |
| UI-10 | Budget | Limits and usage, categories, and period. |
| UI-11 | Reports | Charts with tables and drill-down. |
| UI-12 | Import/export | File selection, interpretation, preview, result report, and safe export. |
| UI-13 | Backup/restore | Actual status, destination, last successful operation, and restore preview. |
| UI-14 | Settings and privacy | Independent language/calendar/currency, experience mode, lock, data, and support. |

**UX-05:** For every workflow, the design must cover empty, success, error, offline, permission-denied, invalid-input, unsaved-exit, and incomplete-information states, not just the happy path.

**UX-06:** Accessibility includes readability, text scaling, icon descriptions, focus order, screen-reader support, and no reliance on color alone. Provide text alternatives to tables and charts.

**UX-07:** Do not use shaming messages, moral judgments about spending, or opaque personal scores. Warnings must be precise and actionable: amount, period, and a possible action.

**UX-08:** Complete the light theme in Phase 1; a dark theme and extensive personalization may be added in Phase 2A. Preparing the color structure does not mean implementing all themes at once.

---

## 15. Onboarding and Everyday Workflows

### 15.1. First Launch

**ONB-01:** Explain the product's value briefly: local recording, no mandatory account, and optional backup. Do not request notification, Drive, camera, or location permissions before use.

**ONB-02:** Limit essential initial choices to language, reporting currency, and the first account; other settings can be skipped and changed later. Introduce display/financial calendars with clear suggestions and a way to change them.

**ONB-03:** Users can start with a zero balance, but explain that this is a chosen value, not a discovered bank balance. Make entering the actual opening balance and baseline date possible at the earliest opportunity.

**ONB-04:** Offer optional guidance for creating a budget and salary/rent schedules. Never populate users' accounts with the developer/product owner's actual amounts or information.

### 15.2. Salary and Bill Workflow

The user schedules a monthly salary, enters an electricity bill with an estimated amount, and confirms the actual amount when due. The actual amount enters the ledger and budget, the occurrence is no longer counted in the forecast, and the user separately decides whether to update estimates for future occurrences.

### 15.3. Recording from a Due Item

From the due-date center, “Record payment/receipt” opens a form. The actual amount and date can be edited; name, account, and category are suggested from the schedule. Before saving, the app may suggest linking a similar existing transaction; the user makes the decision.

### 15.4. Month-End

The user reviews unreviewed and overdue items, examines the period report, reconciles an important account balance, creates or copies next month's budget, and checks the last successful backup. “Closing a month” in Phase 1 must not create an irreversible accounting lock.

---

## 16. Data Import and Export

### 16.1. Export

**IO-01:** Phase 1 provides readable transaction CSV exports suitable for transfer to another tool. Specify the range, accounts, and selectable columns, or at least the required columns.

**IO-02:** Include a standard financial date, type, original amount and currency, account-currency amount, source/destination accounts, category, title, optional notes, stable identifier, and status. A localized display-date column may be added, but must not replace the standard date.

**IO-03:** Exported transfers must not become two income/expense entries when reimported. Preserve the relationship between both sides and refund links in the app's own exports wherever possible.

**IO-04:** CSV is not a complete restorable copy of the app. Explicitly explain which schedules, settings, budgets, and relationships are not included. Backup is the path for a full restoration.

**IO-05:** Before sharing a file, warn that it contains sensitive financial data; do not imply ordinary exports are encrypted. Users can omit notes and other optional sensitive information.

**IO-06:** User-entered text must not become unintended executable formulas in spreadsheet software. Correctly preserve Unicode, delimiters, quotation marks, and multiline text.

### 16.2. Import

**IO-07:** Phase 1 supports generic CSV import with column mapping and preview. Bank-specific formats and other file types belong to Phase 2.

**IO-08:** Users specify or confirm the destination account, currency unit, date format, calendar, decimal separator, and method for identifying income/expense. Do not import ambiguous data by guessing.

**IO-09:** Before applying an import, show valid-row counts, invalid rows, sample amounts, and potential duplicates. Do not silently discard invalid rows; users choose whether to correct or skip them.

**IO-10:** Reimporting the app's own export must not create duplicates when stable identifiers are available. For files without identifiers, similar dates/amounts/descriptions justify only a warning; do not automatically remove two legitimate identical purchases.

**IO-11:** Each import produces a traceable result and supports undoing that batch without deleting pre-existing data. An interruption or error must not leave an indeterminate, partially imported state.

**IO-12:** Importing older account activity must remain consistent with the opening balance and baseline date. For example, setting the current balance as the opening balance and then importing all historical transactions without adjusting the baseline double-counts history and must be detected.

**IO-13:** Import is not a bank connection, and file contents are not guaranteed to be correct. Provide review and balance reconciliation. Do not request broad access to all device files without an actual need.

---

## 17. Backup, Restore, and Data Ownership

### 17.1. Backup Versus Synchronization

**BAK-01:** Phase 1 has one active local ledger. Google Drive/OneDrive store backup versions; they are not a live shared database. Do not describe two independently edited devices as “Synchronized” merely because a file is automatically replaced.

**BAK-02:** Restore is an informed data-replacement operation, not a merge. If users have worked on two devices, explain the risk of losing one side's changes and first preserve a safety copy.

### 17.2. Backup Package

**BAK-03:** A package covers the information needed to restore the ledger: accounts, transactions, categories, schedules and occurrence states, budgets, effective settings, and eventually attachments. Identify the format version and app ID.

**BAK-04:** Do not include login secrets, service tokens, installation keys, PIN/biometric data, or untrusted purchase status in a portable package without review. Restore must not become a way to bypass future Pro entitlements.

**BAK-05:** Test the existing AES-256 claim alongside package integrity and authenticity, recovery-secret management, and the ability to open the package on a fresh installation. An algorithm name alone does not establish security.

**BAK-06:** Explain that an encrypted backup may be unrecoverable without the appropriate password/recovery secret. Do not assume the app-lock PIN and backup recovery secret are the same. Do not promise developer-assisted recovery unless such a mechanism actually exists.

**BAK-07:** Apply the 10-version retention policy to successful versions belonging to this app/ledger in the specified destination. Delete older versions only after saving and verifying the new one. An incomplete upload must not destroy the last healthy backup.

**BAK-08:** Cloud filenames must not contain account descriptions, salaries, balances, or sensitive notes. Display the destination and selected service account, last success, and last error separately.

### 17.3. Restore

**BAK-09:** Before replacing data, require file selection, app identity and format-version checks, password/integrity validation, a preview of the date and data counts, a safety backup of the current state, and explicit user confirmation.

**BAK-10:** An incorrect password, tampered file, another app's file, insufficient storage, or unsupported version must not change current data. Errors must be understandable, and a failed restore must not leave a partially replaced database.

**BAK-11:** After restoration, verify balances and relationships, identify connections requiring reauthentication, and rebuild notifications. Processing overdue occurrences must not recreate already settled entries.

**BAK-12:** Restoration on a fresh installation is a release criterion; successfully creating a backup file alone is insufficient. Also test app upgrades with data from a previous version and without losing history.

### 17.4. Cloud Connections and Data Deletion

**BAK-13:** Sign in to Google/Microsoft only for the selected service and when it is requested; failed or cancelled sign-in must not lock the local ledger. Changing the cloud account requires explicit display of the new destination.

**BAK-14:** Actual sign-in and tested upload/download are prerequisites for releasing each cloud destination. If an integration is not ready, hide that destination or clearly classify it as unavailable for release; simulated success buttons are unacceptable. Valid local backup remains a release requirement.

**BAK-15:** “Delete data on this device,” “Disconnect the service,” and “Delete cloud backups accessible to the app” are three separate actions. Users must understand their choice and its consequences. Do not guarantee deletion of copies that the app can no longer access or that were previously copied manually.

---

## 18. Security, Privacy, and Release Requirements

### 18.1. Local Protection

**SEC-01:** Provide optional app locking using secure device/biometric capabilities and a considered recovery path. Implemented approach: the operating system confirms the device owner (biometrics with the device credential as fallback); the app stores no PIN of its own. A simple PIN without a guessing-attempt policy or explanation of its limitations must not be treated as complete protection.

**SEC-02:** Returning from the background, opening notifications, reports, exports, and sensitive operations must respect the lock policy. Recent-app previews and notifications must not unintentionally reveal financial information.

**SEC-03:** A UI lock is not database encryption, and backup encryption is not encryption of the live database file. Before release, document and test local data protection, operating-system automatic backups, and risks from temporary copies. Security claims beyond the evidence are prohibited.

**SEC-04:** Do not include financial data, transaction titles, notes, tokens, passwords, or backup contents in logs or crash reports. Sending diagnostics, especially when adding a third-party service, requires a Privacy Matrix review.

**SEC-05:** Full data clearing, restoration, and sensitive exports require clear confirmation. Explain the consequences and available recovery paths for accidental deletion or forgotten passwords before the user acts.

### 18.2. Data Policy

**PRI-01:** The Phase 1 finance core must not require a developer-operated backend, analytics, ads, or cloud AI. Any SDK with network behavior requires a separate decision and review; a shared dependency alone is not authorization.

**PRI-02:** Use accurate marketing language: “Data stays on your device by default; optional transfers such as backup occur by your choice.” When cloud backup, file sharing, or AI exists, “No data ever leaves the device” is inaccurate.

**PRI-03:** A genuine, accessible Privacy Policy in the app and Play Console, including contact details and explanations of retention/deletion, is a release prerequisite. Placeholder text or an empty page does not satisfy it. [S01]

**PRI-04:** Complete Data Safety based on the built version's behavior, SDKs, backups, and actual transfers. Assess collection/sharing definitions and possible exceptions for each specific flow; neither “Encrypted” nor “In the user's own storage” automatically means no disclosure is needed. [S02]

**PRI-05:** If a genuine app account is introduced later, assess and implement deletion of the account and associated data from inside the app and through an external web path, with explanations of permitted retention. Merely disconnecting a service or locking an account does not replace account deletion. [S01]

**PRI-06:** Signing in to a backup provider must not unnecessarily create a Finance cloud identity. Record the scope of identity information actually received, stored, and requiring deletion in the Privacy Matrix.

### 18.3. Play and Release

**REL-01:** Before release, assess the Financial features declaration against the app's actual functionality. This form differs from Data Safety; “The app is local-only” does not replace the assessment. Determine appropriate categories from the current form and guidance, not by guessing from this document. [S05]

**REL-02:** For every release, recheck current Play requirements for the target version, release package, permissions, signing, content forms, and testing track. This document deliberately does not prescribe a fixed API level or SDK version.

**REL-03:** Country, audience, image/icon rights, dependency licenses, support contact, and developer details must be genuine. This document is not legal approval or authorization to conduct financial activities.

**REL-04:** Do not include the owner's actual account numbers, personal information, or statements in demos, Store screenshots, or public test files. Test data must be fictional and identified as such.

---

## 19. Privacy Matrix and Optional-Library Boundaries

**MAT-01:** Maintain a privacy profile for each app and each releasable version. The portfolio table is only a summary; document each data flow in the relevant app's appendix.

**MAT-02:** Minimum fields: app/version, feature status, login account, financial account, local data, outbound data, destination/recipient, purpose, optionality, authentication method, retention/deletion, permissions, SDK, encryption and who can read the data, Policy and Data Safety status, test evidence, date, and reviewer.

**MAT-03:** Distinguish `Implemented / Planned / Not included / Unknown—needs verification`. A future plan must not be reported as current collection, and an existing capability must not be ignored because it was only planned in an earlier version.

**MAT-04:** In Finance's initial profile, record the local ledger core, export, local backup, cloud connections, notifications, lock, and diagnostics separately. Resolve any “Unknown” status affecting sensitive flows or network behavior before release.

**MAT-05:** Changes to an SDK, permission, backup destination, rate retrieval, login, billing, or AI trigger a Matrix and policy review. A disabled UI setting does not prove that an SDK performs no processing.

**MAT-06:** Shared libraries must support genuine feature selection; an app without ads/analytics/AI must not initialize or ship them merely because of a shared reference.

A starter template is provided separately in `Finance-Privacy-Matrix-Starter.md`. It is not a completed legal declaration.

---

## 20. Monetization and Free / Pro

**MON-01:** First make the product personally useful and dependable. Payment integration and final feature segmentation are not required in Phase 1; all implemented Phase 1 capabilities must be available for product testing.

**MON-02:** Keep the future design compatible with a one-time Pro purchase for local capabilities. Assess ongoing AI, Bank Sync, or synchronization-service costs separately and transparently; a one-time purchase must not inadvertently create an unlimited commitment to an expensive service.

**MON-03:** Finalize the Free/Pro boundary after real usage. Candidates to assess for Pro include advanced reports, advanced budgets/scenarios, personalization, and additional automation. This document sets no final price or limits for any of them.

**MON-04:** Access to history, basic export, data deletion, basic protection, and restoration of the user's own data must not be held hostage to a purchase. Entitlement expiry or a store outage must not delete data. Creation of new paid items may be limited, but existing data remains readable and exportable.

**MON-05:** Simple/Advanced is not Free/Pro. Hiding details in Simple Mode is not a mechanism for selling Pro. Financial correctness and privacy principles are the same in both modes.

**MON-06:** Before enabling digital-feature sales, assess and test the permitted payment path for the relevant market and version, purchase restoration, refunds/revocations, and offline behavior. A backup file or local flag alone is not a trusted source of purchase entitlement. [S06]

**MON-07:** Voluntary support links, advertising, and AI sales are outside Phase 1. Each requires an independent review of store policy, data flows, and presentation before release. A contribution that unlocks a feature must not be described as support with nothing received in return.

**MON-08:** Do not promise cross-platform purchases, family purchase transfers, or multi-app subscriptions unless entitlements, user identification, and restoration have actually been designed and tested.

---

## 21. Phase 2A: Advanced Local Capabilities

This section defines the fuller product direction. Do not begin implementing it until Phase 1 is stable. The order below is a proposed priority, not a requirement to build everything simultaneously.

### 21.1. Savings Goals and Funding Upcoming Commitments

**F2-GOAL-01:** A goal has a name, amount, currency, optional target date, icon, priority, and funding method; examples include an emergency fund, an equipment purchase, travel, or a child's future.

**F2-GOAL-02:** Distinguish two states: a purely planned goal and funds actually allocated from selected accounts. Total funded allocations to goals must not exceed attributable funds unless clearly marked “Unfunded.”

**F2-GOAL-03:** A transfer to a savings account is not an expense. Allocating the same money to a goal is not a second transaction. Final spending from that account is an actual expense. Releasing an allocation changes only the allocation, not the account balance.

**F2-GOAL-04:** Calculate the suggested next-period contribution from the amount remaining, funds allocated, and number of saving opportunities before the due date. The monthly average of an annual expense must not replace this calculation.

**F2-GOAL-05:** When an account balance falls, make any goal-funding shortfall visible. Do not show the same money as fully funding two goals at once.

### 21.2. Split Transactions and Reimbursements

**F2-TX-01:** Split a purchase across multiple categories; the parts must sum exactly to the event amount. Change the account balance only once. Each category budget sees its own portion.

**F2-TX-02:** Link partial bill payments or income received in stages to one occurrence through multiple entries. Define explicit rules for the outstanding amount, overpayment, full settlement, and reminders for the remaining balance.

**F2-TX-03:** Distinguish an expense reimbursable by another person/employer from ordinary income. Make the personal-expense portion, open receivable, and reimbursement clear; collecting a receivable must not create fictitious income.

**F2-TX-04:** Add multiple tags, receipt attachments, photos/files, bulk operations, and local categorization rules. Automatic suggestions must always be reviewable and undoable.

### 21.3. Contracts, Subscriptions, and Irregular Items

**F2-CON-01:** A financial schedule can include optional contract details: provider, reference number, start/end dates, renewal, review date, cancellation deadline, and notes. The cancellation deadline is not the payment due date.

**F2-CON-02:** Model price changes from a specified date and the final date of a promotional price without changing history. Phase 2 schedules may have multiple future rates/stages.

**F2-CON-03:** The app only reminds users to cancel unless an authorized integration for actual cancellation is built. Do not infer “Cancelled” merely from interacting with a reminder.

**F2-CON-04:** For recurring advance payments and a final settlement, do not count the full bill as an expense again on top of the advances. The settlement workflow must clearly identify the additional payment or actual refund of the remainder; this capability requires independent testing before release.

**F2-CON-05:** Add multiple days per month, a specified weekday, last business day, and holiday rules only with an explainable calendar/country basis. Do not claim a “Banking business day” rule without a valid calendar source.

### 21.4. Debts, Receivables, Loans, and Assets

**F2-DEBT-01:** Support manually tracked debts/receivables with a counterparty, balance, schedule, and settlement. Receiving loan principal is not employment income; repaying principal is not consumption spending. Interest and fees are separate expenses.

**F2-DEBT-02:** Simple fixed installments can be recorded in Phase 1 using a schedule with N occurrences; a specialized loan engine, principal/interest breakdown, and contractual outstanding-balance calculations belong to Phase 2. Do not infer an actual contract's formula and exceptions from the installment amount.

**F2-ASSET-01:** Non-cash assets and manual valuations may be added. Asset value changes are separate from cash income/expense. An estimated property or investment value is not cash.

**F2-ASSET-02:** Brokerage connections, trade execution, buy/sell advice, and investment tax analysis are not part of this committed roadmap; they require separate proposals.

### 21.5. Experience Quality and Advanced Reporting

Quick-entry widgets, shortcuts, saved filters, a customizable dashboard, a dark theme, PDF export, custom-period reports, an event calendar, and on-device OCR may be assessed. None may impose unnecessary permissions or services on core usage.

---

## 22. Phase 2B: Online Services with Independent Decision Gates

### 22.1. Actual Synchronization

**F2-SYNC-01:** Before implementation, the owner must approve hosting, cost, user identity, encryption, and recovery models. Placing a database in a cloud folder is not a substitute for synchronization design.

**F2-SYNC-02:** Define product rules for offline edits, concurrent changes, deletion, restoration, repeated delivery, and version conflicts. Do not silently resolve differences by overwriting with the latest file.

**F2-SYNC-03:** One recurring schedule running on two devices must not create two entries. Occurrence identity and settlement status must be consistent across the shared ledger. An offline device must converge on a consistent result after reconnecting.

**F2-SYNC-04:** A synchronization outage must not stop local recording, but the last synchronization, pending changes, and conflicts must be clear. User-data isolation tests are a release prerequisite for this capability.

### 22.2. Family and Sharing

**F2-SHARE-01:** Personal and shared ledgers remain separate. Joining a household must not automatically transfer all personal history.

**F2-SHARE-02:** Define at least owner, editor, and viewer roles. Invitations, acceptance, leaving, access revocation, and ownership transfer need explicit rules.

**F2-SHARE-03:** Enforce viewing and editing permissions on the service side; UI filters or a device PIN are not access control. Assess export, deletion, and member-invitation permissions separately as well.

**F2-SHARE-04:** Explain the actual limits of revocation: data already stored on a member's device or in an export cannot necessarily be recalled remotely. Do not promise that all copies will be erased.

**F2-SHARE-05:** A shared transaction identifies individual shares and personal/shared payment. Household reporting must not count the shared balance once per member.

### 22.3. Bank Connections

**F2-BANK-01:** Prioritize reading and reconciliation; payment execution or storing users' banking credentials in the app is not part of this baseline capability. Approve an official provider, costs, and supported markets before building it.

**F2-BANK-02:** Match bank-imported transactions against manual entries, file imports, and scheduled occurrences. Present ambiguous matches for user review and preserve each entry's source. A bank's Pending and Posted states may represent one event, not two expenses.

**F2-BANK-03:** Provide dedicated experiences for disconnection, reauthorization, incomplete data, unsupported accounts, and delayed retrieval. Do not promise compatibility with every bank worldwide.

**F2-BANK-04:** Conflicts between bank data and local edits require an explicit policy. Synchronization must not repeatedly erase personal descriptions or user categorization. Bank data must not bypass validation of the ledger's rules.

### 22.4. Optional AI

**F2-AI-01:** Neither the app core nor any financial calculation depends on AI. Determine totals, budgets, balances, and due dates through testable logic; AI may only interpret or suggest based on permitted results.

**F2-AI-02:** Do not request an API key from an ordinary user. Plan direct use of the user's AI account only when the provider officially supports third-party model execution, authorization scope, and billing/quota behavior. Login alone or a user's subscription does not establish that this is possible.

**F2-AI-03:** A lower-dependency option to assess is generating a summary and prompt on-device, previewing it, and sharing it knowingly with the user's selected AI app; the answer is reviewed there or returned/imported manually. Do not present this as automatic built-in AI or a guaranteed programmatic integration.

**F2-AI-04:** An on-device model may also be assessed, but capability, language support, size, and hardware requirements must actually be tested. Add a backend managed by us only after a new cost and privacy decision, not as a hidden default.

**F2-AI-05:** Appropriate uses include explaining spending trends, monthly summaries, categorization suggestions, explaining planned-versus-posted differences, proposing budgets, and answering questions about selected data. Label output as potentially fallible and provide a path to the underlying data.

**F2-AI-06:** Before sending data, users know which period, accounts, and fields will be sent, and to which destination. Default to aggregate figures and the minimum necessary information; do not send full notes or identifying information by default.

**F2-AI-07:** Model output must never automatically change transactions, budgets, or payments. Every actionable suggestion requires a change preview and separate confirmation. Receipt/note text or model output must not be executed as trusted instructions to access data or take financial action.

**F2-AI-08:** Profit promises, investment decisions, loan-eligibility assessments, and personalized legal/tax advice are outside this baseline capability. Adding such services requires specialist review and a separate proposal.

**F2-AI-09:** AI failure, lack of connectivity, exhausted quota, or revoked access must not damage the financial ledger. Record consent status, transmitted data, and the recipient in the Privacy Matrix.

---

## 23. Competitive Basis and Differentiation Opportunities

This section is a limited review of official documentation, not a claim to cover the entire market, personally test every version, or provide comprehensive user research. Features can change; recheck them before making competitive decisions.

| Official source | Documented observation | Our product decision |
|---|---|---|
| Wallet, Planned Payments guide | Describes recurring schedules, confirmation, and reminders; the same guide warns about skipping nonexistent days of the month and possible duplicates during offline multi-device use. [S07] | Configurable month-end behavior, due-date previews, preserved anchors, and duplicate-prevention tests; synchronization kept separate from backup. |
| Monarch, Flex Budgeting guide | Separates fixed, non-monthly, and flexible spending, and uses goals and budget rollover for non-monthly expenses. [S08] | Monthly equivalents in Phase 1; funded goals and rollover in Phase 2, without turning money set aside into actual spending. |
| Monarch, the same guide | Suggests separate categories when a category contains multiple types of spending. [S08] | Treat recurrence/fixedness as schedule attributes wherever possible so users need not duplicate categories. |

Our proposed differentiation comes from combining these choices: no-account onboarding, quick recording, genuinely independent calendars, precise recurrence, exportable data, progressive simplicity, and transparent estimates. Do not claim that no competitor offers a particular capability.

**COMP-01:** A competitor having a feature is not sufficient reason to add it. Every capability needs a user question, maintenance-cost assessment, and acceptance criteria.

---

## 24. Calculation Rules and Numerical Reference Example

### 24.1. Phase 1 Definitions

```text
Account balance at date t
= Valid opening balance
+ Sum of the effects of transactions posted through t

Net income for the period
= Posted income - Related income reversals in the same cash-basis period

Net spending for the period
= Gross posted expenses - Purchase refunds in the same cash-basis period

Income/expense result for the period
= Net income for the period - Net spending for the period

Remaining budget
= Period budget limit - Eligible net spending

Budget usage percentage, only when the limit > 0
= Eligible net spending / Limit × 100
```

These relationships apply in one currency or after explicit conversion. Transfers, opening balances, and adjustments are excluded from ordinary income/expense. When “Confirmed only” is selected, apply it to all relevant figures in that view.

### 24.2. Golden Test Dataset

All figures are hypothetical and in EUR. The baseline is the beginning of `2026-10-01`. Opening balances: current/checking account 1,000; savings 200; credit card zero.

| Event | Effect |
|---|---|
| Salary of 3,000 into the current account. | Income 3,000; current account +3,000. |
| Rent of 800 from the current account. | Expense 800; current account -800. |
| Electricity of 92.37 from the current account. | Expense 92.37; current account -92.37. |
| Grocery purchase of 100 using the card. | Expense 100; card -100. |
| Payment of 100 from the current account to the card. | Transfer; current account -100; card +100. |
| Transfer of 500 from the current account to savings. | Transfer; current account -500; savings +500. |
| Grocery refund of 20 to the card. | Expense refund 20; card +20. |

**Reference results after these operations:**

| Metric | Value |
|---|---:|
| Current account balance | 2,507.63 |
| Savings balance | 700.00 |
| Card balance | +20.00 |
| Combined net posted balances | 3,227.63 |
| Period income | 3,000.00 |
| Gross spending | 992.37 |
| Purchase refunds | 20.00 |
| Net spending | 972.37 |
| Income less net spending | 2,027.63 |
| Remaining amount in a 1,000-unit budget | 27.63 |
| Budget usage | 97.237%; example display: 97.24%. |

Turn this example into a repeatable test scenario. The gross chart totals 992.37; the net report totals 972.37. Do not leave the difference unlabeled or unexplained.

### 24.3. Forecast for the Same Example

Only the current account is in scope. After the events above, define future income of 200, a gas payment of 90, and insurance of 720. The projected balance afterward is `2,507.63 + 200 - 90 - 720 = 1,897.63`.

Do not automatically add savings of 700 or the card's credit/balance to money available in the current account. Do not deduct previously posted transfers again.

If an automatically posted, unreviewed expense of 10 units is later added to the current account, the ledger balance is 2,497.63 and the “Confirmed only” balance remains 2,507.63. Show the difference and its reason clearly. Do not count the related 10-unit occurrence again in the forecast.

---

## 25. Acceptance Criteria and Test Scenarios

This table is the minimum product test set. It is mapped (in `07-acceptance-test-plan.md`) to appropriate automated tests and manual scenarios on actual devices. Phase 2 entries are test specifications for now, not requirements to implement those capabilities in Phase 1.

| ID | Scenario | Expected result | Phase |
|---|---|---|---|
| AT-01 | Fresh installation without internet or login. | Onboarding, account creation, and financial recording are possible. | 1 |
| AT-02 | Record simple income and expenses. | Balances and reports change exactly once. | 1 |
| AT-03 | Tap Save rapidly several times. | Exactly one transaction is created. | 1 |
| AT-04 | Leave a form or encounter a save error. | Input is not unnecessarily lost; save status is clear. | 1 |
| AT-05 | Transfer between two accounts. | Combined balance remains unchanged; income/expense is unaffected. | 1 |
| AT-06 | Report on only the source account of a transfer. | Its balance decreases, but no fictitious expense is created. | 1 |
| AT-07 | Make a card purchase, then pay the card. | The purchase is one expense; card payment is a transfer. | 1 |
| AT-08 | Charge a transfer fee. | Only the fee is an expense. | 1 |
| AT-09 | Set an opening balance and import older history. | Detect double counting and clarify the baseline. | 1 |
| AT-10 | Archive an account with history/schedules. | Preserve history and explicitly handle future schedules. | 1 |
| AT-11 | Adjust a balance. | Affect the balance, not ordinary income/expense; show the reason. | 1 |
| AT-12 | Partially refund a purchase from the same month. | Reduce net spending and increase the account balance. | 1 |
| AT-13 | Refund a previous month's purchase with no spending this month. | Negative net spending is valid; no invalid negative pie chart is created. | 1 |
| AT-14 | Refund into a different account. | Change the actual receiving account; retain the purchase category. | 1 |
| AT-15 | Refund more than the purchase amount. | Show an error or require an explicit split; do not accept it without explanation. | 1 |
| AT-16 | Create a future one-off schedule. | Do not include it in today's balance until completion is posted. | 1 |
| AT-17 | Recur every 2 weeks from `2027-01-01`. | Due dates are January 1, 15, and 29, not two fixed monthly occurrences. | 1 |
| AT-18 | Total a month containing three fortnightly occurrences. | Sum three actual amounts, not an average of two occurrences. | 1 |
| AT-19 | Schedule the 31st using the last-valid-day policy. | In 2027, use January 31, February 28, and March 31. | 1 |
| AT-20 | Use the same rule with the Skip policy. | Skip the invalid occurrence; preserve the anchor and correct next occurrence. | 1 |
| AT-21 | Schedule the last day of the month. | Adjust correctly for shorter and longer months. | 1 |
| AT-22 | Use Gregorian and Persian leap dates. | Test the selected invalid-date behavior and valid conversions. | 1 |
| AT-23 | Change the display calendar. | Preserve absolute transaction dates and existing recurrence rules. | 1 |
| AT-24 | Use a monthly Persian-calendar rule. | Advance by Persian months, not a fixed equivalent day count. | 1 |
| AT-25 | Change an amount from the next occurrence onward. | Preserve history and create no extra occurrence. | 1 |
| AT-26 | Postpone only one occurrence. | Do not change the future recurrence rule. | 1 |
| AT-27 | Skip an occurrence in a 12-occurrence schedule. | Do not automatically create a thirteenth occurrence. | 1 |
| AT-28 | Confirm next month's occurrence early. | Report on the actual payment date on a cash basis; do not double-count it in the future. | 1 |
| AT-29 | Link an existing transaction to a due occurrence. | Retain one entry and settle the occurrence. | 1 |
| AT-30 | Run automatic posting again. | The same occurrence has only one unreviewed entry. | 1 |
| AT-31 | Delete/undo an automatically posted entry. | The next processing run does not immediately recreate it. | 1 |
| AT-32 | Create two schedules with identical names and amounts. | Do not mistakenly merge independent occurrences. | 1 |
| AT-33 | Leave the app unused for a month. | On return, review due items without a notification storm or duplicate entries. | 1 |
| AT-34 | Deny or disable notifications. | The ledger works and the due-date center remains available. | 1 |
| AT-35 | Snooze a notification. | Do not change the payment date. | 1 |
| AT-36 | Notify about an already paid occurrence. | Cancel it; tapping an old notification must not create a second entry. | 1 |
| AT-37 | Restart the device, travel, or change daylight-saving time. | Rebuild scheduling; preserve financial dates and uniqueness. | 1 |
| AT-38 | Receive a notification while locked. | Do not expose sensitive details without user choice. | 1 |
| AT-39 | Use a zero budget, no budget, or an exceeded budget. | Three clear behaviors; no division by zero or concealed overspending. | 1 |
| AT-40 | Use overall and subcategory limits. | Count each expense once in the main total. | 1 |
| AT-41 | Show an annual expense's monthly equivalent. | Include it in comparison without increasing actual monthly spending. | 1 |
| AT-42 | Forecast an occurrence already posted. | Do not count it again in the future. | 1 |
| AT-43 | Forecast with an unknown amount/rate. | Mark the forecast incomplete; assume neither zero nor a 1:1 rate. | 1 |
| AT-44 | Have a positive month-end balance but a negative interim balance. | Show the minimum and timing of the potential shortfall. | 1 |
| AT-45 | Record a USD transaction on a EUR account. | Preserve the original amount and actual debit; keep the fee separate. | 1 |
| AT-46 | Change reporting currency. | Do not delete/relabel history; expose missing rates. | 1 |
| AT-47 | Use a currency without decimals or with different precision. | Input, calculation, and output respect that monetary unit. | 1 |
| AT-48 | Use Persian/German/English and different digit systems. | Forms, charts, text, and RTL work correctly without changing data. | 1 |
| AT-49 | Switch Simple/Advanced modes. | Preserve data and effective settings; show summaries of hidden filters. | 1 |
| AT-50 | Tap a report total or chart segment. | Underlying records exactly match the reported figure. | 1 |
| AT-51 | Import ambiguous dates/decimal formats. | Require preview and confirmation; avoid destructive guessing. | 1 |
| AT-52 | Reimport the app's own CSV. | Do not create duplicate transactions. | 1 |
| AT-53 | Import two actual purchases with the same date and amount. | Do not remove them solely because they look similar. | 1 |
| AT-54 | Fail or cancel an import. | Produce an atomic result or an explicit rollback; preserve pre-existing data. | 1 |
| AT-55 | Export multiline/formula-like text to CSV. | Produce correct, safe output without unintended formula execution. | 1 |
| AT-56 | Restore using a wrong password, damaged file, or another app's backup. | Reject restoration and preserve current data. | 1 |
| AT-57 | Restore on a fresh installation. | Balances, relationships, schedules, and budgets match the reference results. | 1 |
| AT-58 | Create an eleventh backup and encounter upload failure. | Keep the previous healthy copy until the new one succeeds; correctly apply 10-version retention. | 1 |
| AT-59 | Disconnect or change the Drive/OneDrive account. | Retain local data; clearly show the destination and any reauthentication requirement. | 1, released destinations only |
| AT-60 | Upgrade the app with older data. | Migrate without deleting data and test rollback/recovery. | 1 |
| AT-61 | Open notifications/export while app lock applies. | Do not bypass sensitive-access policies. | 1 |
| AT-62 | Run the golden dataset in Section 24. | All balances, reports, and budgets match exactly. | 1 |
| AT-63 | Use two offline devices and one shared occurrence. | After synchronization, retain only one valid settlement. | 2 |
| AT-64 | A household member leaves or loses access. | Revoke service access; accurately explain offline-copy limitations. | 2 |
| AT-65 | Allocate money to two goals. | Do not double-count actual funding coverage. | 2 |
| AT-66 | Use split transactions, partial payments, and final settlement. | Totals and balances remain exact; an open schedule reflects only the outstanding amount. | 2 |
| AT-67 | Receive incorrect AI output or a proposed data change. | Make no change without confirmation; the ledger continues independently. | 2 |
| AT-68 | Cancel a future subscription or revoke a purchase. | Retain data, export, and access to history. | 2 |

---

## 26. Quality, Performance, and Success Measures

### 26.1. Product Quality

**Q-01:** Do not accept any known critical defect in balances, conversion, double counting, restoration, or disclosure of sensitive information in a public release. An attractive appearance does not replace this condition.

**Q-02:** The development team must identify a reference device and a specific test dataset; an initial suggestion is 10,000 transactions, 20 accounts, and 100 active schedules. Report actual measurements for opening Home, searching, and saving entries. Experience targets such as opening Home in approximately two seconds on the reference device are test objectives, not claims of achieved performance.

**Q-03:** Heavy import, backup, and report-rebuilding operations must not block the UI without feedback. Make cancellation and errors manageable wherever possible. App termination during an operation must not leave data inconsistent.

**Q-04:** Controllable test clocks/dates, fictional data, and month-end, leap-year, and period-boundary scenarios are required. Testing only “Today” is insufficient.

**Q-05:** Calculations must remain equivalent across languages, modes, and workflows. Two screens must not use different formulas for the same concept.

### 26.2. Measuring Success Without Mandatory Tracking

In the first phase, assess these outcomes through owner and volunteer usability testing rather than hidden analytics: starting without assistance, simple-entry time, ability to explain a balance, corrections caused by confusion, successful restoration on a new device, and understanding the difference between a schedule and a posted entry.

**Q-06:** Internal test measurement and manual reporting are compatible with local-first operation. Sending behavioral metrics to a server is a separate decision requiring a Privacy Matrix update; do not add an SDK merely to obtain a KPI chart.

---

## 27. Proposed Implementation Roadmap and Release Gates

### 27.1. Step Zero: Discovery and Baseline Confirmation

Done on `2026-09-26`: the actual structure, documentation, tests, localizations, database state, and backup workflow were reviewed (see `01-assessment-and-decisions.md`). Output: verified state versus the owner's report, conflicts, risks, and a detailed change plan. Do not modify production code merely to complete documentation at this stage.

### 27.2. Phase 1A: A Usable Everyday Ledger

Accounts and opening balances; income/expense/transfers and refunds; categories/notes/icons; lists and basic search; language/calendar/currency settings; core one-off and recurring schedule rules; a basic dashboard; local backup and initial protection.

**Phase 1A gate:** Exact golden-dataset results, dependable saves and corrections, no transfer double counting, a clear distinction between schedules and posted entries, and proven local restoration. Until this gate is met, additional charts and online services are not priorities.

### 27.3. Phase 1B: Completing the First Product

Overall and category budgets; complete Phase 1 reports; forecasting; reminders; the full manual multi-currency experience; import/export; Simple/Advanced modes; cloud backup only for ready destinations; localization, security, migration, and release testing.

**Phase 1B gate:** Pass all Phase 1 criteria except explicitly unreleased cloud capabilities. Review the Privacy Policy, Data Safety, Financial features declaration, and required Store configuration against the same Release package. Claims based on Debug testing are insufficient; test the final experience using the release package.

### 27.4. Phase 2A: Ordered by Value

Suggested order: goals and non-monthly expenses/rollover; then splits and partial payments; then contracts and cancellation deadlines; then reports and advanced rules. Advance loans/assets and extensive personalization only when there is a real need.

### 27.5. Phase 2B: Independent Projects

Synchronization, household sharing, banking, and AI are not small ancillary features. Independently define the problem, cost, data, service authorization, stop criteria, and acceptance criteria for each. Building one does not require building the others.

### 27.6. Unacceptable Delivery Claims

“It compiles,” “The screen opens,” “Existing tests are green,” or “HTTP 200 was received” alone do not establish product acceptance. Delivery must include evidence of financial behavior, error paths, and data protection. Report untested items as untested.

---

## 28. Risks, Open Implementation Decisions, and Defaults

The owner has approved the main product decisions. The following were resolved through project inspection; the decisions are recorded in `01-assessment-and-decisions.md`.

| Topic | Resolution approach | Resolution (v1.1) |
|---|---|---|
| Exact domain/project/class structure | Review the existing architecture; make the smallest changes consistent with the product rules. | `Vafadar.Finance.Core` (domain, calculations), `.Data` (EF Core, stores), `.App` (MAUI). Money in minor units with an ISO 4217 table; a transfer is one entry; occurrences are computed, only changed ones are stored. |
| Chart, date, and notification libraries | Prefer existing capabilities; review licensing, quality, and actual need before adding dependencies. | Syncfusion Charts and Calendar (licensed), Fluent UI System Icons (MIT), Plugin.LocalNotification (MIT) on Android/iOS, AndroidX Biometric (Apache 2.0). |
| Database-file protection and recovery secrets | A short threat model, testable method, and recorded real limitations. | Database in app-private storage, not encrypted; app lock hides the UI only; backups encrypted with a user password that cannot be recovered. OS backups (Android Auto Backup, iCloud) enabled and disclosed. |
| Non-Android platform support | Inspect actual targets; preserve compatibility without claiming untested readiness. | Android, iOS and Windows build; Windows has no device notifications in Phase 1 (the due-date center works). Device tests pending. |
| Completing backup OAuth | Review required configuration and actual sign-in; missing configuration blocks that capability, not a reason to require login for the app. | Blocked: needs OAuth client ids. Until then encrypted backup files are shared to any destination via the system share sheet. |
| Import approach and relationship identity | Design from sample files and round-trip tests; do not merge by guessing. | Own export recognized by its header and imported by stable id; other files by explicit mapping; one undoable batch per import. |
| Reviewing automatic entries | Preserve this document's posted/confirmed model and choose appropriate UX. | Automatic and imported entries are Unreviewed; "Needs review" chips and filters on Home, Plans and Transactions; "Mark as reviewed" in the entry details. |
| Missing-rate display and rounding rules | Consistent, explainable behavior; do not change original amounts. | Latest manual rate on or before the date, inverse rates allowed, half away from zero to the minor unit; combined totals only when all rates exist, otherwise "incomplete". |
| Documentation location | Follow repository conventions; Section 29 paths/deliverables are suggestions. | `src/Apps/Finance/docs`; this specification is kept in `src/Apps/Finance/docs/spec`. |
| Final Free/Pro split, pricing, ads, and online services | Do not implement without a new business decision or assume these matters are already settled. | Nothing implemented; no billing, ads, analytics or network SDKs. |

**RISK-01:** The largest risk is turning Phase 1 into an endless project. Mitigation: explicit completion criteria and delivery of usable vertical slices.

**RISK-02:** The second risk is prematurely implementing a positive/negative-only model and facing expensive corrections later. Mitigation: distinguish transfers, opening balances, refunds, and schedules from the start.

**RISK-03:** The third risk is promising more security/synchronization/AI than actually exists. Mitigation: record capability status and release evidence independently of the existence of interfaces or libraries.

---

## 29. Design Documents and Working Rules

### 29.1. Design Documents

The deliverables requested in v1.0 exist in `src/Apps/Finance/docs`:

| Deliverable | Document |
|---|---|
| Product scope / decision log | `01-assessment-and-decisions.md` |
| Domain/business design | `02-domain-design.md` |
| UX flows / screen inventory | `03-ux-design.md` |
| Phase-1 implementation plan and status | `04-phase-1-plan.md` |
| Phase-2 backlog | `05-phase-2-backlog.md` |
| Privacy matrix / data flows | `06-privacy-matrix.md` |
| Acceptance test plan | `07-acceptance-test-plan.md` |
| Release checklist | `08-release-checklist.md` |
| Architecture decisions | `docs/adr` in the repository root and the decision log above |

### 29.2. Working Rules

**HAND-01:** Do not present an unverified class, method, project, or capability name as a repository fact. Reconcile this document with the actual code.

**HAND-02:** Changes are delivered in vertical slices, each with its own goal, tests and documentation update. Do not describe future work as passing tests or existing capabilities.

**HAND-03:** Make and record low-risk technical decisions through repository inspection. Do not ask again about accepted product decisions such as local-first operation or optional accounts. Record external actions, such as configuring a service account, as specific blockers for the relevant capability.

**HAND-04:** Do not turn the plan into a rewrite of shared infrastructure or a new generic platform. Preserve the boundary between Finance and the libraries.

**HAND-05:** Each implementation stage needs a user goal, scope, acceptance criteria, tests, migration risks, and rollback conditions. Do not implement Phase 2 in Phase 1 merely behind a disabled flag.

**HAND-06:** Each delivery reports what changed, new decisions, requirement status and actual blockers, with evidence and limitations.
### 29.3. Minimum Coverage Matrix

| Cluster | Main requirements | Required output |
|---|---|---|
| Ledger and correctness | FIN, ACC, TX, REF | Domain design and numerical tests. |
| Planning | REC, REM, FOR | State lifecycle, calendar, and due-date/notification tests. |
| Analysis | BUD, DASH, REP | Formulas, filters, and drill-down. |
| International experience | LOC, FX, UX, ONB, VIS, CAT | UI flows and language/currency/calendar tests. |
| Data and trust | IO, BAK, SEC, PRI, MAT, REL | Data-flow, recovery, and release design. |
| Product growth | MON, F2, COMP | Backlog and decision gates. |
| Delivery | Q, RISK, HAND, AT | Acceptance criteria and completion evidence. |

---

## 30. Official Sources and Review Note

The following sources were used for selected platform requirements and limited competitor observations. The rest of the document is proposed/approved product design, not material attributed to competitors. **Review date recorded in the source document: `2026-09-25`.** Recheck store and service policies before release. Listing a source does not replace inspecting the actual implementation.

### S01 — Google Play: User Data

Use: Privacy Policy and account-deletion requirements when app account creation is available.

https://support.google.com/googleplay/android-developer/answer/10144311?hl=en

### S02 — Google Play: Data safety

Use: Assessing actual data flows, SDKs, and disclosure definitions; avoiding automatic conclusions based only on local processing/encryption.

https://support.google.com/googleplay/android-developer/answer/10787469?hl=en

### S03 — Android: Notification runtime permission

Use: Permission-request timing, permission denial, and effects on the notification experience.

https://developer.android.com/develop/ui/compose/notifications/notification-permission

### S04 — Android: Schedule alarms

Use: Limitations and informed choices about scheduling precision relative to actual product needs.

https://developer.android.com/develop/background-work/services/alarms

### S05 — Google Play: Financial features declaration

Use: The separate financial-features declaration and determining appropriate categories from the app's functionality.

https://support.google.com/googleplay/android-developer/answer/13849271?hl=en-GB

### S06 — Google Play: Payments

Use: Reviewing permitted digital-feature sales paths and market-specific conditions/exceptions when payments are introduced.

https://support.google.com/googleplay/android-developer/answer/9858738?hl=en

### S07 — Wallet / BudgetBakers: Setting Up Planned Payments

Use: Recurring schedules and documented limitations involving nonexistent days of the month and duplicates during offline multi-device use. This observation refers to the guide reviewed; future versions may differ.

https://support.budgetbakers.com/hc/de/articles/7149523920786-Geplante-Zahlungen-einrichten

### S08 — Monarch: Using Flex Budgeting

Use: Separating fixed/non-monthly/flexible spending, periodic saving/rollover, and the suggestion to separate categories for different spending types.

https://help.monarch.com/hc/en-us/articles/32125337244052-Using-Flex-Budgeting

---

## 31. Implementation Status (v1.1, 2026-09-26)

This section records what the repository actually contains. It is evidence-based: "verified" means covered by automated tests (unit or SQLite integration) and reviewed in Debug screenshots of every screen in English, German and Persian on Windows. **No scenario has been run on a physical Android device or on a release build yet**, so none of this is a release confirmation (Section 27.6). The detailed, maintained status lives in `src/Apps/Finance/docs/04-phase-1-plan.md` (per slice) and `07-acceptance-test-plan.md` (per acceptance scenario).

### 31.1. Summary by Phase

| Scope | Status |
|---|---|
| Phase 1A (ledger, accounts, entries, categories, plans, Home, local backup) | Implemented – verified. |
| Phase 1B (budget, reports, forecast, reminders, multi-currency, CSV import/export, Simple/Advanced, app lock, hardening) | Implemented – verified, except cloud backup. |
| Cloud backup to Google Drive / OneDrive (BAK-13, BAK-14, AT-59) | Blocked: needs OAuth client ids and a real sign-in test. The destination is hidden; encrypted backup files are shared to any destination with the system share sheet instead. |
| Phase 2 | Not started, as intended. |
| Automated tests | 291 tests (domain, data, libraries, localization resources, license check), run in CI on every push. |

### 31.2. Status by Requirement Area

| Area | Status and notes |
|---|---|
| FIN, ACC | Implemented – verified. Transfers are single entries; opening balance with a baseline date; balance adjustment with reason; manual reconciliation shows the difference, unreviewed entries and possible duplicates before offering an adjustment (ACC-08); archiving ends active plans (ACC-06); account currency cannot be relabelled once used (ACC-07); an unknown opening balance marks the account and the Home total as incomplete until a matching reconciliation (ACC-09). |
| TX | Implemented – verified: short form with optional details, title/payee/note separated, search and filters (period, type, account, category, review status; amounts through search), edit, delete with undo, duplicate without the occurrence link, "Make recurring". Quick templates, saved from an entry with or without its amount, fill new entries and never save by themselves (TX-04). |
| CAT | Implemented – verified: starter set, custom categories with one sub-level, colors and icons, ordering, archiving, merging into another category of the same kind (entries, plans and budget limits move; nothing is deleted). |
| VIS | Implemented – verified: light theme, semantic colors plus signs, labels and icons, offline Fluent UI icon set, icons per category, account and entry. |
| REF | Implemented – verified: full and partial refunds linked to the purchase, refund to another account, refunds larger than the purchase are prevented, refunds of earlier months shown separately. |
| REC | Implemented – verified: recurrence engine for Gregorian and Persian calendars, month-end and leap-day rules, "this and future" changes as a new plan slice, skip/move/link/confirm, pause/resume/end, idempotent automatic posting. |
| REM | Implemented – verified at unit level: local reminders on Android/iOS, rebuilt on start, resume, every change, restore and language change; generic lock-screen text unless details are allowed; permission requested only when reminders are enabled; one summary when several reminders share a time (REM-10); optional second reminder on the due date in Advanced mode (REM-01); "In 1 hour" and "Tomorrow" snooze actions that work without opening the app, never change the due date and end when the occurrence is settled or skipped (REM-04). Deviations in 31.3. |
| BUD | Implemented – verified: monthly budget per calendar and currency, overall and category limits, optional account scope (Advanced) shown in Simple mode as a note, alerts at 80/100 %, copy to next month, plans of the month and monthly equivalents (BUD-09). |
| FOR | Implemented – verified: end of month / 30 / 90 days, open occurrences only, overdue items assumed at the base date and labelled, unknown amounts make the result incomplete, daily path with lowest balance and shortfall warning; items can be left out or assumed on another date for the current view only (FOR-04). |
| DASH, REP | Implemented – verified: one filter set per screen with visible scope, actionable review/due items, gross spending chart with refunds card and net table, income and expense, 6/12-month trend with partial months marked, account movement, planned versus posted with open occurrences, variance of settled occurrences and monthly equivalents; every number drills down to its entries. |
| LOC | Implemented – verified: English, German and Persian complete (resource tests); RTL layout; Persian, Arabic and Latin digits accepted; Gregorian and Persian display calendars. Optional country/region (the device region is only suggested) and first day of the week (automatic from region or language, or chosen); the region changes nothing else and uses no location (PR-05, LOC-05). |
| FX | Implemented – verified: original amount and currency on entries, manual dated rates with inverse use, report currency, combined totals only when every rate exists, otherwise marked incomplete. |
| UX | Implemented – verified: Simple and Advanced are views over the same data; hidden settings that affect a number are summarised; empty, incomplete and error states are distinct. |
| ONB | Implemented – verified: no login, language/currency/calendar choice, first account, default categories; dismissible guidance for plans and budget after the first entries (ONB-04). |
| IO | Implemented – verified: CSV export with formula neutralisation and sensitivity warning, re-import of own files without duplicates, generic import with explicit mapping and preview, atomic batch with undo. |
| BAK | Implemented – verified for local encrypted backup files (password, preview, safety copy before restore, last 10 kept, restore on a fresh install, migrations of older backups). Cloud destinations blocked (31.1). "Delete all data on this device" is a separate, confirmed action (BAK-15, SEC-05). |
| SEC, PRI | Implemented: optional app lock with device biometrics or credential, lock on return, notification taps and exports after unlock, Android `FLAG_SECURE` while the lock is on; no analytics, ads or network SDKs; privacy matrix maintained in `06-privacy-matrix.md`. Device checks pending. |
| REL, MON | Release checklist in `08-release-checklist.md`; no billing implemented (Section 28). |

### 31.3. Deviations from This Specification

| Requirement | Deviation | Reason / next step |
|---|---|---|
| REM-01 (multiple reminders) | Advanced mode offers one reminder plus an optional second one on the due date, not an arbitrary number. | Covers the common cases with a simple UI; can be extended without data loss. |
| REM on Windows | Windows builds show no system notifications; the in-app due-date center works. | Windows is a development and personal-use target in Phase 1. |
| SEC-02 (recent-app preview) | The recent-apps preview is hidden only while the app lock is enabled. | Owner decision pending: hiding it always also blocks screenshots. |
| BAK-13/14 | Cloud backup blocked (31.1). | Needs OAuth client ids. |

### 31.4. Build and Licensing Notes

The Syncfusion license key is a build secret: every MAUI app receives it from `eng/AppSecrets.targets` (local git-ignored `Directory.Secrets.props`, GitHub secret `SYNCFUSION_LICENSE_KEY` in CI) and `Vafadar.Maui` registers it once at startup, offline. A test validates the key against the referenced Syncfusion version; the release workflow requires it. The key is never stored in tracked files or documentation.

### 31.5. Required Before Release

1. Run the manual acceptance scenarios (AT-01, 04, 34, 37, 38, 49, 61 and the device parts of the others) on a physical Android device with the release build.
2. Add the `SYNCFUSION_LICENSE_KEY` repository secret and the Android signing secrets in GitHub.
3. Decide on the recent-apps preview (31.3) and, if cloud backup is wanted in the first release, provide the OAuth client ids.
4. Confirm that the privacy policy, Data safety form and Financial features declaration match that build (`06-privacy-matrix.md`, `08-release-checklist.md`).

---

## Product Outcome

Phase 1 must be a genuinely usable personal finance manager, not merely a collection of charts: a correct ledger, precise due dates, explainable budgets, conditional forecasts, and recoverable data. Phase 2 adds depth and services; it must not be needed to repair fundamental mistakes in Phase 1.
