# Zanance — Product Requirements and Business Design Specification

**Version:** 1.1  
**Baseline date:** `2026-09-25` (v1.0); revised `2026-09-26` (v1.1: aligned with the implemented Phase 1, see Section 31)  
**Product name:** Zanance (decided 2026-09-26; used in this document and in the code since 2026-09-28, D-24)  
**Application ID:** `pro.vafadar.zanance` (Android package and iOS bundle ID)  
**Primary audience:** The product owner and the development team, for detailed design, planning, and incremental implementation  
**Status:** Product baseline. Section 31 summarizes the implementation status; it is not a confirmation of release readiness  
**Document language:** English, with stable identifiers for references in code, tests, and documentation

> This document defines what the product must do, for whom, and with what acceptable behavior. The development team may choose the specific class structures, implementation patterns, packages, and implementation approach, provided that the existing architecture and product rules are respected. All Phase 2 capabilities are future requirements, not instructions to implement them alongside Phase 1.

---

## 0. How to Use This Document and Its Limitations

### 0.1. Sources of Decisions

This document is based on decisions approved by the product owner. Version 1.0 was written before the repository was inspected; the verified state of the repository and all implementation decisions are recorded in the design documents in `src/Apps/Zanance/docs` (starting with `01-assessment-and-decisions.md`). Version 1.1 updates the sections that described the assumed state (Sections 2, 14.2, 28 and 29) and adds the implementation status (Section 31). Requirements themselves are unchanged unless marked.

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

The design documents derived from this specification live in `src/Apps/Zanance/docs` in the repository: assessment and decision log, domain design, UX design, Phase 1 plan, Phase 2 backlog, privacy matrix, acceptance test plan, and release checklist. They record what is actually implemented and verified. Section 29 lists them.

---

## 1. Product Definition, Problem, and Value Proposition

### 1.1. Definition

Zanance is a **local-first, international, and extensible** personal finance manager. Users must be able to record their income and expenses, keep track of due dates, review their budgets, and plan ahead based on the information they have recorded, without creating an online account.

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
| PR-07 | No shared capability, SDK, or online service may be introduced into Zanance merely because another app uses it. |
| PR-08 | “Recording a payment” must never be presented as “executing a bank transfer.” |
| PR-09 | Owner-written Zanance source is proprietary, all rights reserved (D-63). Bundled third-party notices apply only to those components; comply with their licenses. |
| PR-10 | Zanance is the approved untranslated product name (D-21); use the approved master symbol and generated assets (D-26). Free/Plus/Pro is the approved plan design (D-61); prices and offers remain design/sandbox values until separately approved for sale. |

### 1.3. Target Users and Baseline Scenarios

An everyday user with one account and simple expenses; someone with multiple bank and savings accounts; a user with weekly or fortnightly income or rent; an immigrant whose language differs from their country of residence; a user who makes foreign-currency purchases; and someone who does not want to connect their bank account or financial information to our service.

Expenses for a child, car, or trip are initially managed through categories and descriptions. This need does not, by itself, imply creating an online account for a child or building a product targeted at children.

### 1.4. Outside the Product's Identity

Phase 1 is not a payment tool, bank, custodial wallet, lending platform, formal business accounting system, tax return application, or investment adviser. An “account” displayed in the app is simply a record of the user's finances, not an actual account opened or administered by the app.

---

## 2. Current State and Alignment with Shared Libraries

Repository alignment refreshed on `2026-10-09` (D-88). Earlier v1.1 review dates remain historical; Section 31 and the canonical ZCR backlog record current implementation and acceptance boundaries:

| Component | Verified state | Requirement for further work |
|---|---|---|
| `src/Libraries/Vafadar.Core` | Entity base (GUID v7), audit interface, settings and app-environment abstractions, digit normalization (Persian/Arabic/Latin). | Preserve common rules; do not introduce finance concepts into the generic core without a reason. |
| `src/Libraries/Vafadar.Localization` | English, Persian, German, Spanish, French, Italian with runtime switching, RTL, Gregorian, Persian and lunar Hijri (Umm al-Qura) display calendars independent of language, date formatting. | Preserve this independence and test it across all screens and reports. |
| `src/Libraries/Vafadar.Data` | SQLite with EF Core, UTC timestamps, audit interceptor, startup migrations, SQLite snapshot backup source. | Migrations must only add to the schema; sample data must not replace the user's database. |
| `src/Libraries/Vafadar.Backup` | Backup package with manifest and SHA-256 checksums, optional AES-256-GCM password protection (D-62), validation of app id, format and app version before restore, inspection without restore, retention of 10 versions, optional content summary. | Keep encryption, integrity and restore behavior covered by tests. |
| `src/Libraries/Vafadar.Backup.GoogleDrive`, `…OneDrive` | Provider storage implementations, covered by fake-HTTP tests, with configured platform sign-in (D-35/D-50). | Offered only when the provider client is configured; real sign-in/upload/restore for each released platform and signing certificate remains AT-59. |
| `src/Libraries/Vafadar.Authentication`, `…Authentication.Maui` | Shared identity/token contracts and platform implementations: MSAL for Microsoft; Play services on Android and browser PKCE on iOS/Windows for Google (D-35/D-50). | Configuration, provider-console registration and real end-to-end acceptance are independent of source implementation; no fabricated sign-in evidence. |
| `src/Libraries/Vafadar.Maui` | Startup (`UseVafadar`), Syncfusion licensing, `{v:Translate}`, RTL helpers, date field (Gregorian/Persian), choice chips, converters, device authentication for app locks. | Use the existing approach; verify asset and license rights for a release. |
| `src/Apps/Zanance` | Phase 1 implemented; cloud backup implemented but not yet verified on devices (Section 31). | Extend this app; do not create a parallel app or repository without justification. |
**AR-01 — Sharing boundary:** Localization, backup, app lock, reminder infrastructure, and the mechanism for optional capabilities may be reusable. Balance calculations, financial recurrence, budgeting, refunds, and forecasting belong to the Zanance domain; generalizing them into a shared library requires a real need.

**AR-02 — No unjustified redesign:** Implementation must first understand existing decisions. This document does not authorize automatic changes to the framework, architecture, or technology versions.

**AR-03 — App data isolation:** Each app must have its own data, sensitive settings, backups, and connection scope. Sharing a library must not make Zanance data visible in another app.

**AR-04 — Platform priority:** Android is the first release platform. Preserve other platforms that actually exist in the project, but do not claim equivalent support or readiness without testing them.

---

## 3. Confirmed Product Decisions

| Topic | Decision |
|---|---|
| Online app account | Optional; not part of core usage in Phase 1. |
| Local data ownership | Independent local profiles are implemented, each with its own database and backup set (D-34). Profiles share the device-wide access gate and do not provide family sharing or per-person authorization. |
| Source of data | Local data; optional cloud backup, not synchronization. |
| Financial accounts | Multiple accounts, including cash, current/checking, savings, and a simple credit card account. |
| Financial events | Income, expense, and transfer; refunds and adjustments have distinct meanings. |
| Recurrence | Every N days/weeks/months/years, with explicit dates and edge-case behavior. |
| Recurring posting | Manual confirmation by default; optional automatic posting for fixed amounts, marked “Unreviewed.” |
| Budget | Overall monthly limit and category limits in Phase 1; advanced approaches in Phase 2. |
| Currency | A currency per account; foreign-currency purchases with the actual debited amount and a manual exchange rate; online rates later. |
| User experience | Simple and Advanced modes, with shared calculations and data rather than two separate products. |
| Pricing | No active payments in Phase 1. Since D-61 (2026-10-07): Free, Plus (monthly, yearly or Plus Lifetime) and Pro (monthly or yearly) – see enhancement ZCR; services with ongoing costs are assessed separately. |
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

**REC-11:** Specify each rule's calendar when it is created. Changing the app's display calendar must not convert a Gregorian rent schedule into Persian months. Phase 1 supports monthly/yearly rules based on Gregorian and Persian calendars; additional calendars require independent testing before release. The lunar Hijri calendar (Umm al-Qura, 29- or 30-day months) was added and tested this way (2026-10-05): monthly and yearly rules, month-end rules and budget months in lunar months.

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
| Budget-period calendar | Independently selected when setting up a budget. | Defaults to Gregorian, with explicit Persian and lunar Hijri options; preserve history. |
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

**ONB-02:** Offer language, theme, Simple/Advanced experience, reporting currency and calendar before the first account (owner decision D-62); other settings can be skipped and changed later. Introduce display/financial calendars with clear suggestions and a way to change them.

**ONB-03:** D-62: offer an existing-backup restore before first-account creation. Validate and preview it, retain a safety copy and obtain confirmation. Existing accounts complete onboarding; no extra account is created and restored profile preferences are preserved. Cancellation returns to the draft; an empty backup still needs onboarding.

Users can start with a zero balance, but explain that this is a chosen value, not a discovered bank balance. Make entering the actual opening balance and baseline date possible at the earliest opportunity.

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

**BAK-03:** A package covers the information needed to restore the ledger: accounts, transactions, categories, schedules and occurrence states, budgets, effective settings, and attachments. Identify the format version and app ID.

**BAK-04:** Do not include login secrets, service tokens, installation keys, PIN/biometric data, or untrusted purchase status in a portable package without review. Restore must not become a way to bypass future Pro entitlements.

**BAK-05:** Test the existing AES-256 claim alongside package integrity and authenticity, recovery-secret management, and the ability to open the package on a fresh installation. An algorithm name alone does not establish security.

**BAK-06:** D-62: local and connected cloud backups offer optional password encryption, on by default. Remember only the protection choice on the device, never the password; the same password may be reused. Anyone obtaining an unprotected package can read it.

Explain that an encrypted backup may be unrecoverable without the appropriate password/recovery secret. Do not assume the app-lock PIN and backup recovery secret are the same. Do not promise developer-assisted recovery unless such a mechanism actually exists.

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

**SEC-01:** Optional device authentication and a separate four-digit app PIN (D-63). The PIN applies to all profiles on this device, takes precedence when both are enabled, and is held only as a salted verifier in protected device storage. Current PIN is required for change/removal. Five wrong attempts cause a one-minute delay; further failures increase it up to fifteen minutes, persisted across restart. Recovery requires successful device authentication plus confirmation; unavailable or cancelled authentication never opens a PIN-protected app. This is a UI access gate, not database encryption.

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

**PRI-06:** Signing in to a backup provider must not unnecessarily create a Zanance cloud identity. Record the scope of identity information actually received, stored, and requiring deletion in the Privacy Matrix.

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

**MAT-04:** In Zanance's initial profile, record the local ledger core, export, local backup, cloud connections, notifications, lock, and diagnostics separately. Resolve any “Unknown” status affecting sensitive flows or network behavior before release.

**MAT-05:** Changes to an SDK, permission, backup destination, rate retrieval, login, billing, or AI trigger a Matrix and policy review. A disabled UI setting does not prove that an SDK performs no processing.

**MAT-06:** Shared libraries must support genuine feature selection; an app without ads/analytics/AI must not initialize or ship them merely because of a shared reference.

A starter template is provided separately in `Zanance-Privacy-Matrix-Starter.md`. It is not a completed legal declaration.

---

## 20. Monetization and Free / Pro

> **Amended 2026-10-07 (D-61):** the plan model is now **Free → Plus → Pro**, with Plus Lifetime as the only one-time
> purchase (local Plus capabilities only). MON-02 and MON-03 are superseded; MON-01 and MON-04 … MON-08 stay valid.
> Plans, prices and rules: [enhancement ZCR](../enhancements/2026-10-commercial-release/README.md).

**MON-01:** First make the product personally useful and dependable. Payment integration and final feature segmentation are not required in Phase 1; all implemented Phase 1 capabilities must be available for product testing.

**MON-02 (superseded by D-61):** Keep the future design compatible with a one-time Pro purchase for local capabilities. Assess ongoing AI, Bank Sync, or synchronization-service costs separately and transparently; a one-time purchase must not inadvertently create an unlimited commitment to an expensive service.

**MON-03 (superseded by D-61):** Finalize the Free/Pro boundary after real usage. Candidates to assess for Pro include advanced reports, advanced budgets/scenarios, personalization, and additional automation. This document sets no final price or limits for any of them.

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

The canonical ZCR delivery plan governs Free/Plus/Pro entitlements and billing as well as synchronization, household sharing, banking and AI. Each needs its recorded product, cost, privacy and acceptance decisions. D-69 authorizes ready planned work; it does not settle open decisions or permit selling unbuilt services. Building one does not require building the others.

### 27.6. Unacceptable Delivery Claims

“It compiles,” “The screen opens,” “Existing tests are green,” or “HTTP 200 was received” alone do not establish product acceptance. Delivery must include evidence of financial behavior, error paths, and data protection. Report untested items as untested.

---

## 28. Risks, Open Implementation Decisions, and Defaults

The owner has approved the main product decisions. The following were resolved through project inspection; the decisions are recorded in `01-assessment-and-decisions.md`.

| Topic | Resolution approach | Resolution (v1.1) |
|---|---|---|
| Exact domain/project/class structure | Review the existing architecture; make the smallest changes consistent with the product rules. | `Vafadar.Zanance.Core` (domain, calculations), `.Data` (EF Core, stores), `.App` (MAUI). Money in minor units with an ISO 4217 table; a transfer is one entry; occurrences are computed, only changed ones are stored. |
| Chart, date, and notification libraries | Prefer existing capabilities; review licensing, quality, and actual need before adding dependencies. | Syncfusion Charts and Calendar (licensed), Fluent UI System Icons (MIT), Plugin.LocalNotification (MIT) on Android/iOS, AndroidX Biometric (Apache 2.0). |
| Database-file protection and recovery secrets | A short threat model, testable method, and recorded real limitations. | Database in app-private storage, not encrypted; app lock hides the UI only; backups optionally encrypted with a user password that cannot be recovered (D-62). OS backups (Android Auto Backup, iCloud) enabled and disclosed. |
| Non-Android platform support | Inspect actual targets; preserve compatibility without claiming untested readiness. | Windows and Android builds are verified; targeted Android emulator flows are documented. The iOS app target exists, but its build/device acceptance requires a Mac and Apple signing. Windows has no device notifications (the due-date center works). |
| Completing backup OAuth | Review required configuration and actual sign-in; missing configuration blocks that capability, not a reason to require login for the app. | Platform implementations exist and providers are shown only when configured. AT-59 requires real sign-in/upload/restore for the released certificate and platform; configuration alone is insufficient. Local optionally protected files can be shared independently. |
| Import approach and relationship identity | Design from sample files and round-trip tests; do not merge by guessing. | Own export recognized by its header and imported by stable id; other files by explicit mapping; one undoable batch per import. |
| Reviewing automatic entries | Preserve this document's posted/confirmed model and choose appropriate UX. | Automatic and imported entries are Unreviewed; "Needs review" chips and filters on Home, Plans and Transactions; "Mark as reviewed" in the entry details. |
| Missing-rate display and rounding rules | Consistent, explainable behavior; do not change original amounts. | Latest manual rate on or before the date, inverse rates allowed, half away from zero to the minor unit; combined totals only when all rates exist, otherwise "incomplete". |
| Documentation location | Follow repository conventions; Section 29 paths/deliverables are suggestions. | `src/Apps/Zanance/docs`; this specification is kept in `src/Apps/Zanance/docs/spec`. |
| Free/Plus/Pro, pricing and online services | Follow D-61 and the canonical ZCR plan; unresolved quota, provider, cost and sale choices stay gated. | Plan design approved; billing, quotas, sync and shared services are not implemented. No ads/application analytics; configured Cloud builds contain authentication/storage and native SDKs documented in the SDK review. |

**RISK-01:** The largest risk is turning Phase 1 into an endless project. Mitigation: explicit completion criteria and delivery of usable vertical slices.

**RISK-02:** The second risk is prematurely implementing a positive/negative-only model and facing expensive corrections later. Mitigation: distinguish transfers, opening balances, refunds, and schedules from the start.

**RISK-03:** The third risk is promising more security/synchronization/AI than actually exists. Mitigation: record capability status and release evidence independently of the existence of interfaces or libraries.

---

## 29. Design Documents and Working Rules

### 29.1. Design Documents

The deliverables requested in v1.0 exist in `src/Apps/Zanance/docs`:

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

**HAND-04:** Do not turn the plan into a rewrite of shared infrastructure or a new generic platform. Preserve the boundary between Zanance and the libraries.

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

## 31. Implementation Status (v1.1, updated 2026-10-11)

This section records what the repository actually contains. "Verified" is qualified by each scenario's evidence:
automated tests, running Windows layouts or targeted isolated Android emulator checks. Complete signed Release
artifacts and specific emulator runs are recorded below and in the acceptance plan; they do not establish physical
Android/iOS acceptance or production release (Section 27.6). The maintained status lives in
`src/Apps/Zanance/docs/04-phase-1-plan.md` and `07-acceptance-test-plan.md`.

### 31.1. Summary by Phase

| Scope | Status |
|---|---|
| Phase 1A (ledger, accounts, entries, categories, plans, Home, local backup) | Implemented – verified. |
| Phase 1B (budget, reports, forecast, reminders, multi-currency, CSV import/export, Simple/Advanced, app lock, hardening) | Implemented – verified, except cloud backup. |
| Cloud backup to Google Drive / OneDrive (BAK-13, BAK-14, AT-59) | Implemented – unverified (D-35, D-50): OneDrive on Android, iOS and Windows (MSAL), Google Drive on Android (Play services authorization API), iOS and Windows (system browser with PKCE, no client secret); offered only in builds with OAuth client ids, as an explicit choice (connect, back up now, list, restore, delete a cloud backup, disconnect), optionally encrypted with a password (D-62); a build without client ids has no cloud section and no INTERNET permission. BAK-14 is met only after the real sign-in and upload/download test on a device; until then release the offline variant. |
| Phase 2A | Started after Phase 1: savings goals with earmarked funding, coverage by account balances, priority-based shortfalls and suggested contributions (F2-GOAL-01..05, BUD-11/12, AT-65); budget rollover of surplus or surplus and deficit across consecutive months, used identically on the budget page, Home and alerts (§10.3); splitting an income or expense across categories with parts that add up exactly, editable and joinable again (F2-TX-01); partial payments of a plan occurrence that keep it open with the outstanding rest until the final payment settles it, with forecast, lists and reminders following the rest and automatic posting never paying twice (F2-TX-02); contract details on plans (provider, number, term end, renewal, last day to cancel, review date) with reminders 14 days before and on the last day to cancel and on the review date, shown on Home; the app only reminds and never marks a contract as cancelled (F2-CON-01, F2-CON-03); price changes from a date use the existing "this and future" plan slices (F2-CON-02); reimbursable expenses with the amount and who pays back, the repayment recorded as a linked refund (never income), open amounts on Home and in an "Owed to me" list (F2-TX-03); tags on entries with suggestions, search by #tag and a spending-by-tag report with drill-down (F2-TX-04, REP-08); local categorization rules that suggest a category for new entries and generic CSV imports, always visible and undoable, created from an entry or on a rules page (F2-TX-04); loans and money lent as their own account types with lender or borrower, outside the liquid total by default and listed separately, principal and repayments as transfers and interest as expenses (F2-DEBT-01; a principal/interest engine per F2-DEBT-02 is not built); manually valued assets whose revaluations are balance adjustments, never income (F2-ASSET-01); a dark theme with a light / dark / like-the-device choice (UX-08); bulk operations in the transaction list (mark reviewed, change category, add a tag, delete with undo; F2-TX-04); a weekend rule that moves due dates on weekend days to the working day before or after, with the weekend taken from the region and public holidays explicitly not considered (F2-CON-05); the final settlement of advance payments, where only the extra payment becomes an expense and money back becomes refunds of the advances (F2-CON-04); monthly plans on a second day of the month, each day its own occurrence (REC-05 extension); a PDF of the report period in English, German and Persian with an embedded font, clearly marked as not official (REP-07); receipt photos and PDF files attached to entries, photos scaled down to 1600 px, at most 5 MB each, stored in the database and therefore in backups, never in CSV exports, removed at the next start after their entry is deleted so that undo still works (F2-TX-04); an optional interest rate and monthly installment on loans and money lent give a labelled estimate of the next split, the payoff month, the remaining interest and a full schedule (annuity with monthly interest on the balance, never presented as the contract), and "Record installment" posts the principal as a transfer and the interest as a separate expense or income (F2-DEBT-02); monthly and yearly plans on a weekday of the month, the same week as the start date (e.g. the second Monday) or the last one (e.g. the last Friday), in the Gregorian and Persian calendars (REC-12); display units such as the toman, defined explicitly by the user with currency, name and a power-of-ten factor and never inferred from language or region, change how amounts of that currency are shown and entered while storage, CSV, backups and exchange rates keep the ISO currency (FX-07); forecast scenarios that also assume another amount for a plan occurrence, next to leaving it out or moving it, each labelled as an assumption and never saved (FOR-10); saved transaction filters (period or custom range, type, account, category or drill-down, search, review status) applied with one tap, stored in the ledger database and therefore in backups (REP-08, §21.5); an optional envelope method per budget, where category limits are money assigned from today's cash balances and the budget shows what is not assigned yet after goal earmarks and the unspent rest of each envelope, counting every amount once and moving nothing (§10.3, BUD-11/12); a customizable Home where six sections can be hidden and reordered while the balance and the items that need attention always stay on top (§21.5, REP-08, DASH-03) – implemented, verified by unit and data tests. Flex budgets (D-28): fixed, non-monthly and flexible categories, bills from the plans and one flexible limit. Public holidays of Germany and Iran for the weekend rule of plans (D-29). An Android quick add widget without amounts (D-30). On-device receipt reading with review on iOS, Windows and Android (D-31), including PDF invoices – the text layer read directly, scanned pages rendered by the system (D-33). The Android 13+ per-app language setting (D-32). A financial month with its own start day (pay cycle), weekly and two-week budgets next to the month, and limit suggestions from the average of past periods (§10.3). Local profiles, each with its own database, settings, app lock and backups (§3, D-34). |
| Phase 2B | Not started; each online service needs its own decision (Section 27.5). Since D-61 planned in waves 3–8 of enhancement ZCR (canonical backlog `enhancements/2026-10-commercial-release/04-backlog.md`). |
| Automated tests | 1345 passing tests as of 2026-10-09 (domain, data, libraries, documents, localization resources, license check; D-64 adds receipt evidence; D-65 adds 21 plan/debt UX regressions; D-70 adds 19 goal reminder cases; D-71 adds 15 review reminder cases; D-72 adds 27 aggregate/import cases; D-73 adds 68 application flow cases; D-74 adds 37 valued-asset consent cases; D-75 adds 23 performance safety, ledger-index and queued-read cases; D-76 adds 12 snapshot publication/retry cases; D-79 adds 10 Settings read/caption cases), run in CI on every push. |

### 31.2. Status by Requirement Area

| Area | Status and notes |
|---|---|
| FIN, ACC | Implemented – verified. Transfers are single entries; opening balance with a baseline date; balance adjustment with reason; manual reconciliation shows the difference, unreviewed entries and possible duplicates before offering an adjustment (ACC-08); archiving ends active plans (ACC-06); account currency cannot be relabelled once used (ACC-07); an unknown opening balance marks the account and the Home total as incomplete until a matching reconciliation (ACC-09). |
| TX | Implemented – verified: short form with optional details, title/payee/note separated, search and filters (period, type, account, category, review status; amounts through search), edit, delete with undo, duplicate without the occurrence link, "Make recurring". Quick templates, saved from an entry with or without its amount, fill new entries and never save by themselves (TX-04). |
| CAT | Implemented – verified: starter set, custom categories with one sub-level, colors and icons, ordering, archiving, merging into another category of the same kind (entries, plans and budget limits move; nothing is deleted). |
| VIS | Implemented – verified: the approved design (D-27) – neutral surfaces with one meaning per colour (money in, problem or debt, near a limit, plans, savings, transfers, refunds), icon and date tiles, Vazirmatn for Persian and Persian digits by default; light and dark theme (D-22), semantic colors plus signs, labels and icons, offline Fluent UI icon set, icons per category, account and entry. Final brand (PR-10, D-26): the approved Zanance symbol as launcher icon on Android, iOS and Windows, splash (Android symbol, iOS symbol and wordmark), Android notification icon, onboarding and More page; all generated from one vector master in `branding/zanance`. |
| REF | Implemented – verified: full and partial refunds linked to the purchase, refund to another account, refunds larger than the purchase are prevented, refunds of earlier months shown separately. |
| REC | Implemented – verified: recurrence engine for Gregorian, Persian and lunar Hijri (Umm al-Qura) calendars, month-end and leap-day rules, "this and future" changes as a new plan slice, skip/move/link/confirm, pause/resume/end, idempotent automatic posting. |
| REM | Implemented – verified at unit level: local reminders on Android/iOS, rebuilt on start, resume, every change, restore and language change; generic lock-screen text unless details are allowed; permission requested only when reminders are enabled; one summary when several reminders share a time (REM-10); optional second reminder on the due date in Advanced mode (REM-01); "In 1 hour" and "Tomorrow" snooze actions that work without opening the app, never change the due date and end when the occurrence is settled or skipped (REM-04). Deviations in 31.3. |
| BUD | Implemented – verified: monthly budget per calendar (Gregorian, Persian or lunar Hijri, chosen at onboarding) and currency, overall and category limits, optional account scope (Advanced) shown in Simple mode as a note, alerts at 80/100 %, copy to next month, plans of the month and monthly equivalents (BUD-09). Phase 2A (§10.3): months with their own start day, weekly and two-week budgets (limits only), suggestions from past spending. |
| FOR | Implemented – verified: end of month / 30 / 90 days, open occurrences only, overdue items assumed at the base date and labelled, unknown amounts make the result incomplete, daily path with lowest balance and shortfall warning; items can be left out or assumed on another date for the current view only (FOR-04). |
| DASH, REP | Implemented – verified: one filter set per screen with visible scope, actionable review/due items, gross spending chart with refunds card and net table, income and expense, 6/12-month trend with partial months marked, account movement, planned versus posted with open occurrences, variance of settled occurrences and monthly equivalents; every number drills down to its entries. |
| LOC | Implemented – verified: English, German, Persian, Spanish, French and Italian complete (resource tests; Spanish, French and Italian since 2026-10-06, one general translation each for every region of the language; weekday phrases agree with the weekday's gender, D-57); RTL layout; Persian, Arabic and Latin digits accepted; Gregorian, Persian and lunar Hijri (Umm al-Qura) display calendars, the lunar one preparing languages such as Arabic. Optional country/region (the device region is only suggested) and first day of the week (automatic from region or language, or chosen); independent regional date/number formats and digit shapes in onboarding/settings, optional supported holiday region, portable display choices in new backups (D-67); no location access (PR-05, LOC-05). |
| FX | Implemented – verified: original amount and currency on entries, manual dated rates with inverse use, report currency, combined totals only when every rate exists, otherwise marked incomplete. |
| UX | Implemented – verified: Simple and Advanced are views over the same data; hidden settings that affect a number are summarised; empty, incomplete and error states are distinct. |
| ONB | Implemented – verified: no login, language/theme/experience/currency/calendar choice; create a first account or restore existing accounts without duplication (D-62); default categories; dismissible guidance for plans and budget after the first entries (ONB-04). |
| IO | Implemented – verified: CSV export with formula neutralisation and sensitivity warning, re-import of own files without duplicates, generic import with explicit mapping and preview, atomic batch with undo. |
| BAK | Implemented – verified for local optionally encrypted backup files (D-62: password optional, original password needed for encrypted files; preview, safety copy before restore, last 10 kept, restore on a fresh install, migrations of older backups). Cloud destinations implemented but unverified on devices (31.1, D-35); disconnecting, deleting a cloud backup and deleting the data on the device are separate actions (BAK-15). "Delete all data on this device" is a separate, confirmed action (BAK-15, SEC-05). |
| SEC, PRI | Implemented: optional independent four-digit app PIN with protected verifier, durable attempt limiting, current-PIN change/removal and device-authenticated recovery; optional device authentication; covered startup/return, pending notification taps and sensitive operations after unlock (D-63). Android screenshots blocked by default with a user toggle; recents always protected (API 33+ exclusion and secure background flags), iOS cover unchanged; iOS/Windows do not prevent screenshots. No analytics/ads; optional cloud backup only in configured builds. 20 PIN tests and Windows UI checks; real-device authentication/recents and iOS acceptance pending. |
| REL, MON | Release checklist in `08-release-checklist.md`; no billing implemented (Section 28). |

### 31.3. Deviations from This Specification

D-65: blank plans default to Once; selecting Monthly anchors to the first date in the named rule calendar.
Summary/next dates and ending count/date are common in both experience modes; uncommon rules expand locally.
Debt/receivable creation uses positive reference-date amounts with direction-derived signs, optional estimates and
a separate unsaved repayment-reminder draft (unknown principal, automatic posting off). Existing recurrence and
ledger invariants remain unchanged; AT-72 physical-device acceptance is still required.

D-64 receipt maintenance: complete purchase-total evidence replaces the largest-price/last-number fallback. Found,
Review and NotFound states carry source rows and at most four complete choices into both Home and attachment rereads.
No automatic save, no clearing an existing amount when no new total is found, no foreign exchange or UI-locale guess.
Explicit rial/toman units remain associated with the amount; conflicting/unclear units require review or manual entry.
OCR keeps line/block/page/full bounds and angle/confidence when provided; character confidence is not total confidence.
Recognition uses independent bounded upright pixels up to 3200 px; stored photos remain metadata-free 1600 px JPEGs.
Windows actual synthetic-image selection (inclusive tax, skew, EXIF, ambiguity, currency conflict and missing total),
stored-image rereading, manual choice and cancellation/error invariants are tested; physical Android/iOS camera/file acceptance
and a representative receipt corpus remain pending. The Android bundled engine reads Latin script: Persian parser
cases do not demonstrate Persian camera OCR. There is no automatic crop/perspective correction or invented item sum.

| Requirement | Deviation | Reason / next step |
|---|---|---|
| REM-01 (multiple reminders) | Advanced mode offers one reminder plus an optional second one on the due date, not an arbitrary number. | Covers the common cases with a simple UI; can be extended without data loss. |
| REM on Windows | Windows builds show no system notifications; the in-app due-date center works. | Windows is a development and personal-use target in Phase 1. |
| BAK-13/14 | Cloud backup implemented for both providers on Android, iOS and Windows (D-35, D-50); not yet verified with real OAuth clients, and the iOS app not yet built on a Mac. | Register the OAuth clients (Entra iOS platform, Google Android/iOS/Desktop clients), test on each platform; iOS with the iOS release. |
| VIS-01 (expense colour) | Expenses are shown in the neutral text colour with "−" and their category icon, not in dark red; red marks problems only (negative results and balances, debt, overdue, exceeded limits). | Owner-approved design D-27: when every expense is red, real warnings no longer stand out. |
| D-11 (navigation) | A fifth tab "Insights" holds Budget, Reports, Forecast and Goals as top tabs; More keeps the rest in four groups. | D-27: fewer items in More, one place for analysis. |

### 31.4. Build and Licensing Notes

The Syncfusion license key is a build secret: every MAUI app receives it from `eng/AppSecrets.targets` (local git-ignored `Directory.Secrets.props`, GitHub secret `SYNCFUSION_LICENSE_KEY` in CI) and `Vafadar.Maui` registers it once at startup, offline. A test validates the key against the referenced Syncfusion version; the release workflow requires it. The key is never stored in tracked files or documentation.

### 31.5. Required Before Release

1. Run the manual acceptance scenarios (AT-01, 04, 34, 37, 38, 49, 61 and the device parts of the others) on a physical Android device with the release build.
2. Add the `SYNCFUSION_LICENSE_KEY` repository secret and the Android signing secrets in GitHub.
3. If cloud backup is wanted in the first release, register the OAuth clients, set `MICROSOFT_ENTRA_CLIENT_ID` and the Google client of each released platform (`GOOGLE_OAUTH_CLIENT_ID_ANDROID`, `…_IOS`, `…_WINDOWS`) and run AT-59 on a device (D-35); otherwise release the offline variant.
4. Confirm that the privacy policy, Data safety form and Financial features declaration match that build (`06-privacy-matrix.md`, `08-release-checklist.md`).

---

### 31.6. Transaction detail disclosure (D-90 / AT-96)

D-90 continues A11Y-03 with state-matched transaction detail actions and retained unsaved fields. AT-96 checks
48 actual native Invoke operations and 24 complete draft/stored-row restorations, including Simple/Advanced initial
visibility. Main suite remains 1,369 (App.Tests 140); strict Windows and canonical Android Release have zero warnings/
errors. Normal signed Release passes hide/show, retained payee/tag/note values and Keep editing/Discard in en/fa/de;
complete fictitious financial rows remain unchanged. Canonical APK/privacy scripts and full package/signature pass.
Evidence and independent OS/screen-reader/phone/iOS/provider/owner gates:
[quality/entry-details-disclosure.md](../quality/entry-details-disclosure.md).

### 31.7. Onboarding restore actions (D-91 / AT-97)

D-91 / AT-97 keeps both first-run restore alternatives fully readable and retains their existing navigation/busy
bindings. The final Windows matrix and signed Android Release pass; 1,369 tests (App.Tests 140), zero-warning strict
builds and full installable APK/privacy checks. See [quality/onboarding-restore-actions.md](../quality/onboarding-restore-actions.md)
for native draft/stored-data preservation and independent platform/physical-device/provider/owner gates.

### 31.8. Measured Home reload optimization (D-92 / AT-98)

D-92 / AT-98 removes measured redundant Home row/goal work while retaining complete values and financial rules.
All 1,384 tests pass (App.Tests 148), strict builds and complete signed Release APK/privacy/native checks pass.
Controlled tenfold Windows warm reload median: 2,727.70 -> 1,228.28 ms. QA-06 remains partial: entry materialization,
cold start/ANR and physical-device Q-02 are open. See [quality/performance-home-snapshots.md](../quality/performance-home-snapshots.md).

### 31.9. Android Debug boundary and native performance evidence (D-93)

D-93 fixes the Windows-only Home diagnostic call that broke Android Debug, with no Release/UI/financial behavior
change. Final Windows/Android Debug/Release builds and all 1,384 tests (App.Tests 148) pass. Temporary native stage
measurements identify entry materialization/initial account creation; QA-06 and physical/platform acceptance remain
open. Evidence: [quality/home-debug-platform-and-native-stages.md](../quality/home-debug-platform-and-native-stages.md).

### 31.10. Complete Home customization (D-94 / AT-99)

D-94 completes the local Customize Home caption/target follow-up: all eight section identities grow at full
width above the original controls, with 44 px targets and a complete Reset action. AT-99 final en/fa/de theme/width/
200% review and 84 native operations, 1,384 main tests, strict Windows/Android Debug/Release builds and signed Release
owned-sample readbacks pass. A11Y-03, QA-06 and real device/platform acceptance remain partial/open.
Evidence: [quality/home-customization-readable.md](../quality/home-customization-readable.md).

### 31.11. Hidden Home account views (D-95 / AT-100)

D-95 / AT-100 removes measured eager native row construction for hidden Home Accounts while retaining the
complete snapshot and every shown account/action. Native tenfold publication median: 5,339.89 -> 10.69 ms, with no
total cold-start/Q-02 acceptance claim. Final language/theme/width/200% native visibility/detail/list checks, 1,384
main tests, strict Windows/Android Debug/Release builds, signed Release and complete owned-sample readbacks pass.
QA-06 full entry materialization, visible creation, cold duration/ANR and real device/platform acceptance remain open.
Evidence: [quality/home-hidden-account-views.md](../quality/home-hidden-account-views.md).

### 31.12. Complete account descriptions (D-96 / AT-101)

D-96 / AT-101 completes account type/default/excluded/incomplete captions in the shared account row at large
native text, retaining complete text, flags, balances and row actions. Final en/fa/de themes/widths/200% and normal
text checks cover all eight flag combinations; 1,384 main tests, strict Windows/Android Debug/Release builds, signed
Release navigation and exact 24-table original-sample readbacks pass. A11Y-03 other controls/modals and actual OS,
screen-reader, phone/iOS and release acceptance remain open.
Evidence: [quality/account-descriptions-readable.md](../quality/account-descriptions-readable.md).

### 31.13. Complete debt entry action (D-97 / AT-102)

D-97 / AT-102 completes the existing Accounts debt/receivable action caption with a growing real button,
unchanged text/scaling/command and native open/cancel verification of the same unsaved Loan form. Complete stored
accounts/entries/settings/budgets/schedules remain unchanged. Final en/fa/de themes/widths/200% and normal text,
1,384 tests, strict Windows/Android Debug/Release, signed Release navigation and exact 24-table readbacks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/debt-entry-action-readable.md](../quality/debt-entry-action-readable.md).

### 31.14. Single readable modal headers (D-98 / AT-103)

D-98 / AT-103 removes duplicated Windows modal titles and reflows all eleven existing modal title/Cancel
rows without changing forms or financial behavior. Actual glyph/name/target/no-overlap and native Cancel/parent
Back pass across all Windows language/theme/width/text contexts; all nine complete stored data sources remain.
Normal Android debt/expense/plan forms pass in three languages and both themes; original three-profile tables
remain identical. All 1,384 tests, strict Windows/Android Debug/Release and complete signed APK checks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/modal-headers-readable.md](../quality/modal-headers-readable.md).

### 31.15. Visible complete plan validation (D-99 / AT-104)

D-99 / AT-104 collects all applicable plan-field problems in one invalid Save attempt, displays destination
feedback beside its input and reveals the first problem using fresh native layout. Drafts and complete stored
Accounts/Entries/Settings/Schedules remain; existing money/recurrence/unknown-amount rules are unchanged.
Twenty-four new behaviour cases bring the main suite to 1,408 (App.Tests 172). Full Windows language/theme/width
review, strict Windows/Android Debug/Release, normal Android invalid Save/cancel, signed APK and original 24-table
readbacks pass. Other A11Y-03 controls and OS/readers/phone/iOS/owner/release gates remain open.
Evidence: [quality/plan-validation-visible.md](../quality/plan-validation-visible.md).

### 31.16. Complete transaction monetary feedback (D-100 / AT-105)

D-100 / AT-105 collects independent transaction monetary problems before mutation, shows each beside its
input, reveals the next affected field and reopens invalid collapsed details without replacing entered values.
Existing parsers, consent/receipt protections and successful ledger/fee/overlap/attachment ordering remain.
Forty-eight new cases bring the main suite to 1,456 (App.Tests 220). Complete Windows matrix, strict builds,
normal Android invalid Save/cancel, signed APK and original 24-table readbacks pass. Simple destination-fee
retention is a separate pending runtime concern; other controls/platform/owner/release gates remain open.
Evidence: [quality/entry-validation-visible.md](../quality/entry-validation-visible.md).

### 31.17. Existing destination fee retention in Simple (D-101 / AT-106)

D-101 / AT-106 closes the reproduced destination-fee loss in successful Simple edits. Existing fees stay
visible/editable in both modes; new destination-fee creation remains Advanced-only. Native retention, explicit
edit/removal and reopened/new Cancel/Discard preserve financial fields/ids and unrelated rows. The final
Windows matrix has 24 contexts/72 valid Saves; main tests remain 1,456 (App.Tests 220), strict builds and signed
APK pass. Normal Android creation-policy checks and original financial/preference values and expected settings audit pass; successful stored-fee
editing on Android/physical/iOS and other owner/platform/release gates remain open.
Evidence: [quality/destination-fee-retention.md](../quality/destination-fee-retention.md).

### 31.18. Visible advance-settlement feedback (D-102 / AT-107)

D-102 / AT-107 delivers complete settlement period/bill feedback and a growing Record the difference
action. Invalid Save reveals the first affected input without changing the draft; original advance/refund logic
retains zero bills and no-op exact bills. Fourteen new cases bring the main suite to 1,470 (App.Tests 234).
The final Windows matrix has 24 contexts/144 invalid and 72 valid native Saves; six normal-scale Release Android
contexts/18 invalid Saves, strict builds, signed APK and original financial/preference-value checks pass.
Normal Settings audit updates are separate; physical/iOS/readers, valid Android settlement acceptance and other
owner/platform/release gates remain open. Evidence: [quality/settlement-feedback.md](../quality/settlement-feedback.md).

### 31.19. Complete occurrence actions and input feedback (D-103 / AT-108)

D-103 / AT-108 completes due-item action captions and correctly placed payment/override feedback, with independent
corrections and native revelation of the actual affected field. Fourteen new cases bring the main suite to 1,484
(App.Tests 248). Windows: 24 contexts/96 invalid and 96 valid native Saves, plus a separate 24-context/96-invalid
final contextual-message review with no additional financial Saves; normal-scale Release Android:
six contexts/18 invalid Saves. Actual metadata changes never post entries; partial payment and completion retain
the intended entries and unique settlement. Original financial rows/states are preserved/restored; strict builds
and the complete signed phone-test APK pass. Phone/iOS/readers, valid Android occurrence acceptance and other
owner/platform/release gates remain open. Evidence: [quality/occurrence-feedback.md](../quality/occurrence-feedback.md).

### 31.20. Complete transaction presentation reuse (D-104 / AT-109)

Repeated filters retain the complete ordered source while reusing formatted rows within the same data/display
snapshot. Fresh data, culture/language, theme and unit changes invalidate the cache; bulk selection is reapplied.
Four cases bring the main suite to 1,488 (App.Tests 252). Actual bound tenfold Windows warm refresh median:
583.14 -> 358.90 ms; native source publication remains a bottleneck and first materialization is unchanged.
Final running-app/build/APK evidence and QA-06/device/platform limits are recorded in
[quality/transaction-row-reuse.md](../quality/transaction-row-reuse.md). No data/schema/permission change.

### 31.21. Complete bulk-selection reload (D-105 / AT-110)

One complete fresh-snapshot id index replaces repeated full-ledger membership scans. Known selection survives fresh
objects/reordering; removed ids are pruned, new rows are not implicitly selected, and filtered Select all retains
hidden selected rows. Financial validation/copies/Undo and dialog guards remain. Three new cases bring the main
suite to 1,491 (App.Tests 255). Actual tenfold bound Windows LoadAsync median: 25,304.83 -> 1,421.56 ms, excluding
later native arrangement/painting. No data/schema/permission change. Final runtime/build/APK evidence and open
cold/ANR/platform/device gates: [quality/bulk-selection-reload.md](../quality/bulk-selection-reload.md).

### 31.22. Exact Settings suggestion history (D-106 / AT-111)

Read every entry in the last three complete financial months rather than unrelated complete-ledger history. The
existing suggestion calculator, calendar/pay-cycle/currency/account/refund/plan rules and covered complete Settings
publication remain; no auto-save, money, schema or security-policy change. Twenty-one new cases bring the main suite
to 1,512 (App.Tests 276). Actual tenfold bound Windows warm LoadAsync median: 883.78 -> 157.74 ms, excluding later
arrangement/painting. Final runtime/build/APK evidence and open cold/ANR/platform/device gates:
[quality/settings-suggestion-history.md](../quality/settings-suggestion-history.md).

### 31.23. Growing native picker captions (D-107 / AT-112)

Windows selections and popup rows wrap inside the existing native Picker, retaining complete choice identity,
inherited typography/scaling/direction/colors and native selection. Popup targets have a 44-unit minimum height.
The main suite remains 1,512 (App.Tests 276); actual glyph/target/choice/draft/data review spans Settings and other
picker pages. Final 21-context native matrix, strict builds, exact original-data readbacks and signed APK pass;
evidence and platform limits are recorded in
[quality/native-picker-captions.md](../quality/native-picker-captions.md). A11Y-03 and OS/readers/device/iOS acceptance
remain open; no saved preference, money, schema or security-policy changes.

### 31.24. Complete transaction report scope (D-108 / AT-113, locally verified)

Repair reproduced clipped scope/category text, off-page 36-unit Clear targets and a zero-height result viewport at
large text. Growing captions retain independent 44-unit Clear controls; the complete filter form can scroll within
actual page/action-dock space. Keep existing queries, financial results and Clear behavior. Main suite 1,512
(App.Tests 276), final 21 native Windows contexts, strict Windows/complete Android Debug+Release, normal Release
report navigation/Clear, exact original-data readbacks and signed D-108 APK pass;
[quality/transaction-scope-readable.md](../quality/transaction-scope-readable.md).
A11Y-03 and independent platform/OS/readers/device/owner/release gates remain open.

### 31.25. Independent saved transaction filters (D-109 / AT-114, locally verified)

Applying a named filter after report navigation clears unrelated transient report currency, account-set,
confirmed-only and scope-note restrictions. Preserve every existing saved choice and ledger value; no schema or
portable model change. Final six native contexts/18 independent restrictions, main 1,512 tests (App.Tests 276),
strict builds, actual Release named-filter/report application, exact original-data readbacks and signed D-109 APK pass:
[quality/saved-filter-report-scope.md](../quality/saved-filter-report-scope.md). Platform/owner gates remain open.

### 31.26. Complete unchanged transaction source reuse (D-110 / AT-115)

Equivalent searches retain the already bound native source only when all captions, boundaries, order and row
objects match. Fresh data/display reads still publish; complete results and financial rules remain. Nine new cases:
main 1,521/App.Tests 285. Six native Windows contexts/186 own renders, strict builds, normal Release,
exact original-data readbacks and complete signed APK pass:
[quality/unchanged-transaction-source.md](../quality/unchanged-transaction-source.md). No cold/device/ANR acceptance.

### 31.27. Repeat from a new transaction draft (D-111 / OD-12 / AT-116)

The owner approved an in-memory prefilled Plan from a new transaction, without posting it. Monthly anchors to the
selected first date/calendar; disabled posting/reminders, explicit Save and complete original draft retention remain.
Currency/account changes require explicit re-entry. Unsupported transaction details are disclosed and retained.
Fourteen new cases pass: main 1,535/App.Tests 299. Initial native expense/income/transfer and invalid amount checks
pass; final financial/presentation matrices, strict builds, normal signed Release explicit Plan Save/cleanup,
exact original-data readbacks and signed APK pass: [quality/entry-repeat-draft.md](../quality/entry-repeat-draft.md).
Phone/iOS/OS-reader/provider and release acceptance remains separate.

QA-06 follow-up D-112 (2026-10-10): a bounded full-entry enumeration comparison preserves complete ordered
packets and stored fictitious data, but the synchronous candidate is slower in four of seven tenfold pairs and
does not reduce allocation. No production change is adopted. D-111's 1,535-test/App.Tests 299, native, strict-build
and signed APK evidence remains the app baseline. See
[quality/entry-read-enumeration-comparison.md](../quality/entry-read-enumeration-comparison.md); QA-06 remains partial.

### 31.28. Complete transaction category choices (D-113 / AT-117)

Actual clipped category captions now wrap inside finite independently growing rows, preserving native scaling,
raw choice identity, colors/icons, names and real selection buttons without Save. Windows 33 contexts/99 native
selections pass with full private draft/stored-row/developer-byte restoration. Main 1,535/App.Tests 299 and strict
Windows/complete Android builds, normal Release selection/discard, exact original-data readbacks and signed APK pass. A11Y-03 and physical/iOS/OS-reader/owner/release
gates remain open: [quality evidence](../quality/entry-category-captions.md).

D-115 resolves the specific empty-form native gesture hypothesis without production changes: the actual IME
window covers the failed origin; after Back removes it, the identical gesture exposes complete Title/date in the
same blank draft without typing or Save. D-113's 1,535-test/build/signed APK baseline is retained, not rerun.
Last-position fixture and physical-device/iOS/screen-reader acceptance remain open. See
[quality/entry-category-captions.md](../quality/entry-category-captions.md#empty-form-gesture-investigation-d-115).

### 31.29. Single complete savings-goal warning packet (D-116 / AT-118)

Goal cards show the complete existing funding/date warning packet once, with unchanged amounts, scalable text,
semantic color, visibility, raw row/command and full spoken name. Both actual native templates pass the 27-context
Windows review with 54 packets and original presentation/store/developer-byte restoration. Full delivery evidence:
[quality/goal-warning-packets.md](../quality/goal-warning-packets.md). A11Y-03 and broader platform gates stay open.

### 31.30. Final Core commercial policy (D-117 / AT-119 / ENT-01)

OD-03 and the Section 2 tool/capability matrix are owner-approved. Pure Core policy models tiers and purchases
separately, exact scopes and membership, permissions, quotas and counting; Simple/Advanced does not change a right.
Plus Lifetime survives Pro expiry. Free history/corrections/security/backup/restore/basic export remain available;
paused plans/goals count and budget history does not consume another definition. All 76 new policy cases pass:
main suite 1,611, App.Tests 299 unchanged. Platform builds/APK: [evidence](../quality/entitlement-policy.md).
No app enforcement, UI, store, schema or SDK change. Test-build activation and billing/server/owner gates stay open.

### 31.31. Account service quota boundary (D-118 / AT-120 / ENT-02 in progress)

The account store checks enabled permission/capacity under the actual opened file's SQLite writer, including
unarchive; independent providers cannot consume the final slot twice. Corrections/archive preserve over-quota data,
failed writes roll back and current deployment remains explicitly inactive without a paid grant. 15 new real SQLite
cases pass; main suite 1,626, App.Tests 299 unchanged. No schema, SDK or visible change. Other resource, import/restore,
read-only/native operation and activation paths remain open: [evidence](../quality/account-write-policy.md).

### 31.32. Template/filter service quota boundaries (D-119 / AT-121 / ENT-02 in progress)

Actual-file writer transactions guard enabled new-template/filter capacity before changes. Existing ids and
confirmed same-name replacement reuse slots; failed replacement restores complete query data. Two independent
providers cannot consume the same final slot twice. 24 new real SQLite cases pass; main suite 1,650, App.Tests
299 unchanged. Current test builds remain unrestricted. No schema, permission, SDK or visible change; remaining
ENT-02/03 resources/import/restore/native paths and activation stay open: [evidence](../quality/template-filter-write-policy.md).

### 31.33. Goal and plan batch/continuation write boundaries (D-120 / AT-122 / ENT-02 in progress)

Actual-file writer transactions guard counted Goal creation/reopening and net whole Plan batch capacity.
Split/resume uses stored predecessor state, retains one slot and moves actual history links atomically. New paid
kinds/tools are checked independently of presentation; retained/history corrections remain available after expiry.
45 new real SQLite cases pass; main suite 1,695, App.Tests 299 unchanged. Current deployment stays inactive with no
schema, SDK, permission or visible change. Other ENT-02/03 operations/resources/import/restore/read-only/native paths
and activation remain open: [evidence](../quality/goal-plan-write-policy.md).

## Product Outcome

Phase 1 must be a genuinely usable personal finance manager, not merely a collection of charts: a correct ledger, precise due dates, explainable budgets, conditional forecasts, and recoverable data. Phase 2 adds depth and services; it must not be needed to repair fundamental mistakes in Phase 1.

D-67 maintenance status (2026-10-08): connected destinations automatically list app backups across profile sets for
restore, with destination loading/empty/error/file feedback; set-specific retention and explicit preview/confirmation
are unchanged. Password protection remains optional in both local/cloud paths. Independent format culture, digit
shapes and holiday/week choices are available before first-account creation and later, without changing UI language,
currency, stored dates/amounts or existing plan rules. New packages carry allowlisted display preferences; old packages
remain compatible. AT-73/74 local checks do not prove real-provider or physical-device acceptance.


D-68 / SEC-01 research status (2026-10-08): proposed ADR 0010 compares database encryption/provider/key choices and
safe migration. Four Windows feasibility tests pass outside the main 1,134-test suite; the signed Android Release
AOT/trimmed fixture passes on an isolated API 36 x86_64 emulator, including recovery of the same Keystore-wrapped
fictitious profile in a second process and tamper rejection. Technical delivery is complete for owner review. App
data remains plaintext, the production connection and permissions are unchanged, and ADR acceptance/library
procurement/recovery are owner-review gates. ARM64 phone, iOS and real migration acceptance remain later work.


D-69 / SEC-09 current baseline: SDK and endpoint inventory, separate Offline/Cloud APK permission checks and eleven
PowerShell AT-76 cases supplement the 1,134 .NET tests. Offline packaging preserves owner provider configuration and
removes INTERNET from the shipped Android Release artifact; Cloud grants process-wide access and may permit ML Kit
SDK diagnostics before account connection. No new SDK/UI/schema or OS-backup policy. Signed iOS manifests, physical
traffic, real OAuth and later billing/sync SDKs remain review gates. Continuous ready-section delivery is authorized;
unresolved ADR/library, OS-backup, commercial and provider decisions remain owner choices.

D-70 / LOC-02: optional goal contribution reminders use the saved schedule/calendar at 09:00 device-local time,
generic by default, grouped at a common minute and cancelled for reached/inactive/unavailable goals. They never
create financial records. Opt-in is saved in the existing contribution-plan field; permission is device-local.
Reminder-only edits retain saved calendar/anchor/ending. AT-77 contributes 19 cases to the 1,153-test main suite.
Windows system notification delivery is unsupported; physical Android/iOS acceptance remains separate.

D-71 / LOC-03: the remaining optional ZEX-S0610 period review reminder is implemented. Settings offers a portable
profile opt-in, off by default, with translated help. Schedule local 09:00 on the first day after the selected-calendar
financial month closes. Skip empty/finished periods and missed times; share notification privacy/permission/queue.
Tap opens the currently due review through app lock without automatic steps or ledger writes. Additive migration and
compiled model updated; AT-78 adds 15 cases, main suite 1,168. Running-app en/fa/de light/dark 360/412/wide and isolated
API 36 x86_64 native scheduling/delivery/tap/opt-out checks are separate from phone/Doze/reboot/iOS acceptance.

D-72 / LOC-04: import offers explicit per-aggregate link/keep-both choices, inclusive dates and amount previews.
Existing aggregates subtract only new accepted details; shared or previously linked details cannot reduce another
aggregate. Stale decisions fail atomically. Additive ImportLinks metadata journals and the compiled model preserve
original identities/attachments and safe Undo across restart/backup; later edits and dependent imports/refunds require
review. AT-79 adds 27 cases; main suite 1,195. Running en/fa/de light/dark 360/412/wide and isolated native import,
restart skipping and Release Undo are verified. The 412 EUR example remains 395 + 17; Undo restores the original 412
row. No automatic merge, SDK or permission. Physical ARM64/iOS and production acceptance remain open.

D-73 / QA-03: add 68 application flow cases (AT-80), compiling actual non-UI production sources with explicit native
ports, real SQLite/localization and isolated PIN storage; main suite 1,263 passed. Cover onboarding, profiles, bulk,
lock, widget/reminder links and theme. Bulk validation preserves loaded snapshots and selection; pending dialogs
exclude other commands. Delayed Undo and backwards-clock grace fail closed. Missing windows retain the lock gate;
profile paths require known GUID identities. Native behavior remains a separate acceptance gate. No UI translation,
schema, permission, commercial enforcement or production encryption changes. QA-04 is the next independent section.

D-74 / QA-04 / ZEX-S0408: existing manual valued-asset income/expense consent is covered by 37 AT-81 application
cases and running-app evidence. The real editor calls the tested gate before mutation/write; cancellation preserves
the draft and ledger, pending saves exclude another command, and retries require fresh consent. Accepted input still
passes the financial validator; transfers/adjustments remain outside income/spending and this warning. Main suite
1,300 passed. No schema, persisted confirmation flag, import/restore restriction, translation or permission change.
Phone/iOS acceptance remains open; QA-06 performance measurement is the next independent ready section.

D-75 / QA-06 / Q-02: explicit reference 10,000/20/100 and tenfold 100,000/200/1,000 workloads use fictitious data,
real migrations/stores/search/financial calculations. Preserve retained legacy fields and save cleanup through
fingerprints. Large entry materialization runs on a worker after capturing its context/profile; account routing
indexes preserve original ordering/identity and call the established balance formula. AT-82 adds 23 cases; main
suite 1,323 passed. There is no schema, permission, SDK or portable-preference change. See
[the performance evidence report](../quality/performance-q02.md) for process-cold native observations, failures,
raw timing boundaries and physical-reference-device acceptance. The two-second Home objective is not an achieved
or published claim; emulator/desktop results do not close phone or iOS acceptance.

D-76 / QA-06: transaction input and grouped results remain covered until snapshot publication; translated loading
and explicit retry replace premature empty/result claims. Repeated pending loads share one read and preserve
query/filter choices. AT-83 adds 12 actual application cases; main suite 1,335 passed, zero failed/skipped. No schema,
financial formula, portable data, security setting, SDK or permission change. Q-02 retains original negative native
observations and documents the follow-up separately; native duration/ANR and physical/iOS acceptance remain open.

D-77 / ZCR-A11Y-03 first slice: wrapping Home quick actions, two-row plan identity/amount, growing date/template
containers, 44 px section links and fixed decorative glyphs retain native readable-text scaling and all financial
semantics. Windows layout stress is separate from native Android SP conversion and real OS/device acceptance.
See [the large-text evidence](../quality/font-scaling-a11y03.md). Main suite remains 1,335; A11Y-03 is in progress,
including transaction/account rows, bottom tabs, custom-drawn controls and the remaining screens/dialogs.

D-78 / ZCR-A11Y-03 follow-up: persistent actions reserve space below seven page viewports, financial identity wraps
above its amount/status, and transaction Undo text/action use separate rows. AT-85 records runtime measurements,
1,260 captures and normal-scale native emulator review; main suite remains 1,335. No financial/data/security change.
A11Y-03 is still in progress; OS large text, fixed/custom controls, currency grouping and device/screen readers remain.
See [the current evidence and limits](../quality/font-scaling-a11y03.md).

D-79 / Settings maintenance: read preferences/suggestions/device availability completely before enabling the form;
failure offers retry. Refresh translated choices in place without resetting selection or unsaved text. AT-86 adds
ten real-source cases (main suite 1,345; App.Tests 127), with actual binding/native review separate from unit cases.
The large-text report records rendered/native/APK evidence. No new data, schema, permission, SDK, financial formula,
commercial restriction or security policy. Full A11Y-03 and physical-device/iOS/provider acceptance remain open.

D-80 / A11Y-03: long Settings/account/debt captions wrap around a real unchanged command button; debt actions get
full width and bulk actions two rows. Existing compact bulk font size retains native scaling. AT-87 records native
geometry/selection/no-write evidence separately from the unchanged 1,345-case suite. See the large-text report for
the final matrix and signed APK. No finance/model/security/permission/SDK change; full platform acceptance stays open.

D-81 continues A11Y-03 under D-69: date parts reserve all digits at native text scale and reflow; large amount
readouts retain the existing signed decimal/currency packet and expose horizontal overflow with translated feedback.
Account balances/movements get separate rows and Home navigation stays on its heading. AT-88 records actual native
date/amount geometry, both scroll ends and unchanged fictitious entries; main suite remains 1,345 (App.Tests 127).
Final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Keep headers, other controls, real OS,
keyboard/screen-reader/phone/iOS and unresolved owner gates open. No model, financial or security policy change.


D-82 resolves D-81's reopened native language failure under D-69: retired navigation Title bindings are detached
before replacing their Shell, preserving active forms and the lock/deferred rebuild boundary. Settings constructor
defaults no longer invoke a save. AT-89 checks actual native selection/back, strongly retained retired titles,
live captions/drafts and complete unchanged stored settings/accounts/entries. Main suite remains 1,345 (App.Tests
127); final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Header truncation and other controls,
real OS/keyboard/screen-reader/phone/iOS and unresolved owner gates remain independent work.

D-83 continues A11Y-03 under D-69: complete Windows child-page titles grow above the same body; header/body sizing
uses the current root and Back names/tooltips translate live. Android keeps the native arrow/commands with a scoped
live Back description. AT-90 records native glyph/name/target geometry, actual retained bodies/drafts during resize,
single headers after nested return and unchanged stored data. Final evidence/APK is in quality/font-scaling-a11y03.md.
Modal/Insights/custom controls, the existing nested-return Settings draft reload, real OS/keyboard/screen readers,
physical ARM64/iOS, QA-06 durations/ANR and unresolved owner decisions remain independent work.

D-84 resolves the independently observed Settings nested-return estimate reset. Full covered refresh remains;
raw unsaved text/period/currency stay scoped to the current profile/settings row. Save accepts only successfully
submitted input; no autosave, financial rule or schema change. AT-91 adds 13 cases; main suite 1,358 passed (App.Tests 140). The real retained-body
runtime route now checks drafts after nested return. See quality/settings-estimate-draft.md for final evidence/APK.
Continue modal/Insights/custom-control and other ready work under D-69; unresolved owner/platform gates remain.

D-85 continues A11Y-03 with all four visible Windows Insights destinations in a growing navigation row above
the retained body. Narrow windows reflow into two columns; wide readable content shows four. AT-92 invokes the
actual native routes and checks complete caption/selection/target geometry and unchanged stored data. Final
evidence/APK and independent platform/other-control limits are in quality/insights-navigation.md.

D-86 continues A11Y-03 with complete wrapped budget identities and separate full-width spending/limit and
envelope readouts, retaining compact native typography. A separately reproduced MoneyText signed-boundary crash
is repaired by taking the magnitude after decimal conversion; input range and financial calculations stay unchanged.
AT-93 adds 11 independent Core cases and actual native scroll/geometry/data-equality checks. Final evidence and
remaining physical OS/screen-reader/device/iOS gates are in quality/budget-readouts.md. No schema or new strings.

D-86 current strict main suite: 1,369 passed, zero failed/skipped; App.Tests remains 140. Historical slice counts
above remain historical; local matrix/build/package evidence and external acceptance are kept separate.

D-87 continues A11Y-03 by keeping all three budget period decisions visible in the existing wrapping choice group.
An actual pre-change native check fails for the hidden third option. AT-94 measures full native captions/targets,
invokes the existing three choices, restores selection and compares complete stored data without Save. No shared
control, financial calculation, schema or new string change. Final evidence and independent OS/screen-reader/
physical-device/iOS gates are in quality/budget-periods.md.

D-87 current strict main suite remains 1,369 passing (App.Tests 140). AT-94 adds actual native runtime checks;
physical OS/screen-reader/device/iOS acceptance remains independent.

D-88 completes documentation alignment ZCR-GOV-02: current approved brand/proprietary ownership, local profiles,
platform sign-in source, optional backup protection/portable display preferences and D-61 plan design replace old
assertions. Historical review/slice counts stay dated. No app code or build changed; D-87's 1,369-test/App.Tests 140
and signed APK baseline is retained, not a new run. Provider/certificate, Mac/iOS, physical-device, encryption and
open product/commercial choices remain separate gates. See ZCR 01-current-state.md Section 5 for audited corrections.

D-89 continues A11Y-03 with complete growing tag suggestions and original-value native selection. AT-95 checks
84 actual native invocations and 21 exact draft/stored-data restorations across the three-language/theme/width
matrix; normal signed Android Release confirms selection, Keep editing and Discard without financial writes.
Main suite remains 1,369 (App.Tests 140); strict Windows and equivalent-command Android Release have no warnings/errors.
PowerShell startup blocks the canonical APK/privacy scripts locally; the complete signed package and matching binary
policy are independently verified. Evidence and remaining tooling/OS/screen-reader/phone/iOS/owner gates:
[quality/tag-suggestions.md](../quality/tag-suggestions.md).

D-114 owner delivery decision (2026-10-10): no interim OS-backup exclusions or temporary product substitutes.
The app will not be used before all planned sections/phases are complete. Keep existing runtime policy until
permanent encrypted-data/key/recovery handling is decided; SEC-02..08 and release acceptance remain open.

### 31.34. Current-period budget write boundaries (D-121 / AT-123 / ENT-02 in progress)

Budget Save and confirmed Replace check canonical current-period definitions under the actual-file writer before
any limits/removal. Financial month start and each rule calendar determine current rows; ignored incoming identity
changes cannot bypass the actual stored period/currency. Retained corrections/net-neutral replacement remain
possible above quota; triggered replacement failure restores original limits atomically. 26 new real SQLite cases
pass; main suite 1,721, App.Tests 299 unchanged. Current registration remains inactive. No schema/SDK/permission/UI
change. Explicit active/read-only selection, future-period activation and other ENT-02/03 paths remain unfinished:
[evidence](../quality/budget-write-policy.md).

### 31.35. Holding write rights and atomic purchase price (D-122 / AT-124 / ENT-02 in progress)

Holding direct new work and retained corrections/delete/Undo use exact-file cached-access checks. Existing atomic
operations validate quantity history/import ids inside the SQLite writer. The purchase editor saves its derived
price in the same transaction as event/payment/fee; failures and retired access preserve complete stored data.
Concurrent sales stay nonnegative and identical concurrent imports create rows once. 46 new real SQLite cases
pass; main suite 1,767, App.Tests 299 unchanged. Current registration stays inactive. No schema/SDK/permission or
visible layout change. Explicit selected read-only/new-work classification and remaining ENT-02/03 boundaries
stay unfinished: [evidence](../quality/holding-write-policy.md).

### 31.36. Earmark/contribution rights and complete goal Save (D-123 / AT-125 / ENT-02 in progress)

Actual-file goal allocation/contribution writes now check new advanced work separately from retained releases,
corrections and deletion. Reopening history checks stored paid work/reminders; empty weekly/monthly dates retain
basic goal behavior. The editor commits goal/pins/contribution in one actual SQLite writer transaction. Failures
and retired access preserve complete data; independent enabled writers recheck the last slot and existing plan.
39 new actual SQLite cases/main 1,806 pass; App.Tests 299 unchanged. Current registration stays inactive without
schema/SDK/permission/visible-layout changes. Explicit selected read-only resources, automatic contribution
delivery and the remaining ENT-02/03/04 paths are unfinished: [evidence](../quality/goal-contribution-write-policy.md).

### 31.37. Bound recovery and retained import operations (D-124 / AT-126 / ENT-02 in progress)

Database restore captures its actual context before input and uses the same destination for native copy and
migrations; a reproduced profile-redirection regression is corrected, including first-schema migration. Cached
actual-file rights protect database backup/recovery and owned CSV imports/Undo while preserving over-quota data
and expired-host data rights, never portable paid facts. SQL failures/retirement roll back complete import journals.
28 new SQLite cases/main 1,834 pass; App.Tests 299 unchanged. Current registration stays inactive without schema,
SDK, permission, layout or backup-format changes. Selected read-only classification, profiles, automation and
other ENT-02/03 paths remain unfinished: [evidence](../quality/recovery-write-policy.md).

### 31.38. Categorization rule service rights and replacement (D-125 / AT-127 / ENT-02 in progress)

New categorization text/kind checks the approved automation right. Stored pattern-category corrections and deletion
retain owned-data rights with exact membership, including the same-pattern new-id replacement path. Matching and
replacement share the actual SQLite writer, including inactive builds; independent writers retain one pattern.
Failures/retired facts roll back complete stored rules; existing-id updates retain CreatedAt; Changed follows commit.
28 added cases/main 1,862 pass; App.Tests 299 unchanged. No schema, permission, SDK, visible-layout/string or
portable paid-fact changes. Deployment remains inactive. Suggestion delivery, selected read-only classification,
profiles, automation and other ENT-02/03/04 paths remain unfinished:
[evidence](../quality/category-rule-write-policy.md).

### 31.39. Complete ledger Save rights and final refund amounts (D-126 / AT-128 / ENT-02 in progress)

Actual-file writer precedes group classification and validation, including unrestricted test builds. New category
splits/parts require their feature; retained corrections/join preserve data rights. Holding groups and transfer
fees keep distinct semantics. Ordinary new work checks Transactions. Final refund batches include new originals
and peers, omit replaced/deleted old rows and protect edited purchases; widened exact minor units avoid overflow.
Entry, attachment and derived occurrence changes commit together after cached recheck; failures emit no Changed.
45 added cases/main 1,907 pass; App.Tests 299 unchanged. No schema, SDK, permission, visible layout/string or paid
portable facts change. Deployment inactive; ledger delete/Undo, selected read-only data, automation and remaining
ENT-02/03/04 paths stay open: [evidence](../quality/ledger-write-policy.md).

### 31.40. File-bound deletion and retry-safe ledger Undo (D-127 / AT-129 / ENT-02 in progress)

Delete/Restore bind the actual writer before group, existing-id and occurrence reads. Owned DeleteData/Corrections
keep Free/expired-host rights with exact membership. Committed deletion batches retain their original file and
explicit refund relationships, without global mutable links; failed Undo preserves retry data and cannot target
another profile. Restore only missing ids; repeated Undo is a no-op. Receipts retain their established orphan
policy. App offers keep the original eight-second deadline, prevent duplicate taps and respect replacement/dismissal.
38 added cases/main 1,945 pass; App.Tests 308. No schema, SDK, permission, caption/layout, paid portable facts or
activation changes. Selected read-only data, automation and remaining ENT-02/03/04 stay unfinished:
[evidence](../quality/ledger-undo-write-policy.md).

### 31.41. Native transaction Undo failure feedback (D-128 / AT-130)

The unchanged notice binds the actual command through IAppInteraction. Non-fatal Undo/refresh exceptions are
reported by the existing translated dialog instead of escaping native command dispatch. The command includes
feedback in its execution lifetime. Failed Undo keeps the original deadline without refreshing; successful write
followed by failed refresh never revives the consumed offer. Nine added cases/main 1,954 pass, App.Tests 317.
Strict builds/full signed APK and six installed Release language/theme failure flows:
[evidence](../quality/transaction-undo-feedback.md). No schema, SDK, permission, caption/layout or activation changes.
Later refund edits during Undo, visible ledger Save feedback and remaining commercial/external gates remain open.

### 31.42. Immutable ledger Undo and changed financial dependencies (D-129 / AT-131)

The original-file deletion batch owns independent row/tag copies and explicit expected refund metadata, creation
identity and account currencies. Restore checks under the actual writer before row/occurrence writes; later edits,
deletion/recreation, relinking, changed partial siblings or missing/changed-currency accounts reject the whole old
operation with no Changed. Naming/archival retains recovery; complete restoration stays a no-op after later edits.
18 added cases/main 1,972 pass; App.Tests 318. No schema, SDK, permission, portable entitlement or activation change.
Strict builds/full signed APK and normal/conflicted installed flows: [evidence](../quality/ledger-undo-conflicts.md).
Native edit timing inside the offer, visible ledger Save feedback and remaining commercial/external gates stay open.

### 31.43. Immediate transaction Save feedback (D-130 / AT-132)

Existing general entry Save failures now occupy the fixed growing footer beside the original Save action,
with complete scalable wrapping text and a spoken description. D-126's manual-scroll feedback limitation is
resolved by native geometry/draft/data checks, independently of its earlier ledger-policy evidence. No financial
algorithm, schema, SDK, permission, export or commercial activation changes. Main suite remains 1,972 passing
cases (App.Tests 318); native acceptance is separate: [evidence](../quality/entry-save-feedback-visible.md).
Remaining ENT-02/03/04 and owner/provider/security/device/release gates remain open.

### 31.44. Explicit scoped resources and retained settlements (D-131 / AT-133)

Core models explicit downgrade choices for six resource kinds, retaining original pause/archive/end state and
exact scope; no automatic first-N selection or membership/paid grant. The actual ledger writer checks new money's
source/destination accounts and preserves retained correction, verified overdue-payment and advance-bill rights.
Bill Save rechecks reviewed metadata/net paid basis against current stored rows before recording only the difference;
failure keeps the native form. 79 new cases/main 2,051 pass, App.Tests 318. No schema, SDK, permission, portable
preference or activation change. Native platforms/full signed APK: [evidence](../quality/selected-account-write-policy.md).
ENT-02/03 remain in progress: choice UI/persistence, other resource bindings, budgets, automation, commercial messages
and existing owner/security/provider/device/release gates are unfinished.

### 31.45. Retained manual debt closing (D-132 / AT-134)

The actual-file writer permits closing known existing Loan/Lent principal after downgrade/host expiry. Actual
payments and Fees expenses must exactly close the stored baseline and complete resulting ledger; new card debt
or other new counterpart work still needs its separate right. No automatic archive/guessed relationship/FX or
estimate posting. 51 new cases/main 2,102 pass, App.Tests 318. Current test limits remain inactive. Native/package
evidence and remaining boundaries: [retained debt closing](../quality/retained-debt-closing.md).

### 31.46. Retained occurrence corrections and atomic linking (D-133 / AT-135)

Bind retained Skip/Unskip/change, repair and history-free deletion to exact-file membership and the same writer.
Explicit existing-entry Link rereads actual rule/slice/payment relationships and commits markers/state together,
preserving actual money and metadata without a second ledger entry. SQL/access failure rolls back all rows before
Changed; concurrent writers cannot steal a payment. 39 new cases/main 2,141 pass, App.Tests 318. Current commercial
registration remains inactive. [Native/package evidence](../quality/occurrence-correction-writer.md). Other settlement/reopening, selected-plan
generation, automation, choice persistence/UI, command feedback and existing external release gates remain open.

### 31.47. Atomic new planned payments (D-134 / AT-136)

Settle/PayPart reuse complete ledger validation in the same actual-file writer as stored occurrence checks and
state/paid-total publication. Actual linked partial money and Core effective amount/date determine the remaining
payment even from a stale form. SQL/access failure rolls back money and state before notifications; independent
writers keep one final settlement. 16 new SQLite cases/main 2,157 pass, App.Tests 318; [evidence](../quality/atomic-occurrence-payments.md).
Normal commercial registration remains inactive. Reopening generated money, selected-plan automation,
choice persistence/UI, command exception feedback and external gates remain open.

### 31.48. Exact reviewed occurrence reopening (D-135 / AT-137)

Reopen exactly the reviewed settlement under the actual-file writer. Check stored state EntryId/status and
actual payment plan/date/partial markers; reject stale identities without deleting or releasing another payment.
Generated entries reuse complete ledger deletion, groups, refund links, receipt ownership, paid totals and the
file-bound Undo journal. Manual/imported money is kept; unlink it with the opened state atomically. No second
writer after committed Changed. Preserve earlier partial money and complete occurrence overrides; retain automatic
posting suppression through Undo. SQL/access failure rolls back all rows before store/plan notifications.
15 added SQLite cases/main 2,172 pass; App.Tests 318. Current commercial registration stays inactive.
No schema, migration, SDK, permission, backup field, caption or layout change. Selected-plan generation/automation,
choice persistence/UI, command exception feedback and existing external acceptance gates remain unfinished.
[Native/package evidence](../quality/atomic-occurrence-reopening.md).

### 31.49. Occurrence command failure feedback (D-136)

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
[Evidence](../quality/occurrence-command-feedback.md).

### 31.50. Independent profile and fixture SQLite pools (D-137)

Profile switching and inactive-profile deletion retire only the named file's known pools; restore clears the
exact captured destination connection before migrating that same context. Other SQLite files and active profile
sessions stay independent. Fixture cleanup uses the same production helper only for existing databases under its
unique owned directory, after disposing its providers. Default/factory/read-write/read-only strings are covered;
arbitrary connection-string variants remain their owner's exact-pool responsibility. No process-wide clearing,
collection serialization, schema, permission, backup format, commercial activation or visible layout change.
Nine added AT-139 cases use real TEMP-table session markers and actual profile/backup services. The original
MoveTo/restore behavior lost another database's marker; negative baseline retained. Main 2,210 / App.Tests 348
pass with the normal parallel runner and explicit writer-race tests unchanged (58.337 s; no output runner config).
Strict Windows and Android builds: zero warnings/errors. Complete signed Release APK verified. Normal Release
English/light emulator profile creation/switch, first-run restore from an actual production-service package, return
and inactive-profile deletion succeed. All 24 restored tables are compared: financial rows and other metadata match the actual snapshot; only the existing
onboarding-completion Settings.UpdatedAt audit write differs. Original profile
files/sidecars, complete rows and English/original System theme are restored. Temporary helpers remain ignored.
Release correctly denied the initial inspection-only run-as inventory; the helper was corrected to inspect only an
unlaunched Debug package. One stale prompt-coordinate attempt cancelled before Save; later input used the actual
focused field. Transient hierarchy misses resumed the same created profile without replaying creation/restore.
The observed native document provider was checked before selecting the exact owned file. Negative helper/capture
logs are retained; unsuccessful attempts are not acceptance. One early nested fixture probe used a noncanonical mixed-separator connection string and failed cleanup; it was
corrected to the application's full-path convention before the successful full run. No Windows native, physical
phone, iOS, provider, 200-percent layout or publication acceptance claim. Planned entitlement bindings/automation,
choice persistence/UI and existing external gates remain open.
[Evidence](../quality/local-sqlite-pools.md).

### 31.51. Actual automatic payment writer (D-138 / AT-140)

Automatic payment discovery is only a candidate list. Recheck the stored active rule/slice, actual open state,
linked partial/full money, current amount/due date and accounts inside the same SQLite writer as ledger Save.
Manual partial money, including a legacy incorrect paid cache, prevents another automatic full payment. Removed,
paused, skipped, moved-future or already settled candidates post nothing. Use current overrides/account routing;
transfers remain one entry and automatic money stays unreviewed. Exact cached membership, selected plan and
AdvancedPlans rights apply when enforcement is enabled; choices grant no paid rights. Current registration stays
inactive. Snapshot plan choices are immutable, scoped and in memory only; no selection persistence/UI, schema,
backup format, SDK, permission or visible layout change.
31 added AT-140 actual SQLite cases; main 2,241 / App.Tests 348 pass with normal parallel execution (67.124 s).
Strict Windows/Android builds: zero warnings/errors. Complete signed Release APK verified. Six normal Release
emulator foreground flows cover en/fa/de, light/dark, actual posting/override/transfer and partial/paused/disabled
blocking. All 24 unrelated complete tables match; exact original sample files/sidecars and English/System theme
are restored, then the original Main profile is reopened. The first native assertion inspected an inactive sample
while Main was open; its fixture runs through actual services successfully. Selecting the exact owned sample
through native UI resolves the review setup. Transient hierarchy misses resume the same fixture; a local helper
syntax error executes no native action and is corrected. Preserve these negative attempts; they are not acceptance.
The original source reproduces automatic full money after independent manual partial payment; the final writer
prevents it. One expanded test initially expected a result instead of the existing missing-membership exception;
the corrected test retains the rejection. No Windows native, physical-phone, iOS, provider, performance or release
acceptance claim. Other resource bindings, generation/reminders, choice UI/persistence and external gates remain open.
[Evidence](../quality/automatic-occurrence-writer.md).

### 31.52. Selected future plan occurrences (D-139 / AT-141)

Project actionable future occurrences through exact-file scoped plan choices while keeping complete original
schedules/states for history and every forecast/budget/KPI calculation. Preserve open effective past-due payments;
today/future new money checks actual rule/slice/kind/accounts and selected/paid recurrence rights inside the same
ledger writer. Dated ended slices inherit only their actual unique non-overlapping continuation. No invented choices
or stored pause/end changes. Preserve account rejection feedback. Registration remains inactive; no layout or new resource key,
schema, backup, permission or SDK change. Core reminder eligibility is available; actual reminder/snooze delivery,
choice persistence/UI, remaining selected bindings and external acceptance gates remain open.

Profile/onboarding root replacement also reuses D-82 navigation retirement before detaching old Shell handlers;
initial native hot/cold language failures are negative evidence. Final Main/QA03 switching and hot language changes
are checked in the normal signed Release, without a locale override.

41 added AT-141 cases (19 Core / 22 real SQLite); normal parallel main suite 2,282 / App.Tests 348 pass, no failures
or skips (59.741 s). Strict Windows and Android builds: zero warnings/errors. Complete signed Release APK verified.
Six actual normal Release emulator flows cover en/fa/de and light/dark: Home due count 11, six plans in Due/Upcoming/All
and detail future dates/settled history. Current inactive build remains unrestricted above Free's five-plan allowance.
No financial Save during review; all 24 complete prepared tables match. Exact original sample files/sidecars and
English/System plus the original Main profile are restored. Physical-phone, iOS, Windows native, provider,
performance and publication acceptance remain open. Old generic full/partial entry bypass reproduces two failures.
Initial invalid enum/runner attempts and six account-feedback ordering failures remain negative evidence; final
implementation retains established rejection order and the complete canonical suite passes.
[Evidence](../quality/selected-plan-occurrences.md).

### 31.53. Actual scoped Plan reminder delivery (D-140 / AT-142)

D-140 binds actual Plan/contract notification delivery and snooze rebuilding to the opened file and current
explicit plan choice. Original group members, recurrence identities and effective dates survive snooze; current
privacy/language/text is rebuilt from stored rows. Unprovable legacy snooze caches are discarded while normal
reminders are rebuilt; legacy navigation remains readable. Notification taps resolve the current file after
unlocking, without profile guessing or money writes. Reserve the snooze id marker so a hashed occurrence id cannot
keep the original deadline. Registration remains inactive. Choice UI/persistence, other selected resources and
external acceptance remain open. [Evidence](../quality/selected-plan-reminders.md).

D-140 final adapter includes the observed stale Android action-caption correction and database-free receiver
caption initialization. Final baseline: 48 new AT-142 cases, normal parallel main 2,330 / App.Tests 380 pass,
zero failures/skips; strict Windows/Android zero warnings/errors. See the same quality record for final native/APK proof.

### 31.54. Durable scoped account and Plan choices (D-141 / AT-143)

Persist explicit account and plan choices in one additive profile-local ResourceSelections table, keyed by
resource kind and the verified financial scope kind/id. A fresh revision rejects stale and ABA drafts. Reviewed Save
rechecks actual file, membership/rights, original identities/names/states, row revision and capacity under the same
SQLite writer; no-op saves preserve revision and publish no Changed. Never archive/end or erase financial data to
represent read-only availability. Stored choices override cached fallback selections, never verified capabilities.

New account/unarchive and Plan creation join an existing bounded choice atomically when capacity remains. Explicit
archive/ending releases only its own chosen identity; a validated split transfers only the actual selected
predecessor slot. Pauses still count. Current work/reminder snapshots capture the stored revision and retire pending
publication after a change; choices trigger the existing reminder refresh. Retained corrections remain possible even
with malformed choice input, which grants no bounded new work until explicit reviewed repair.

Commercial registration remains inactive: no guessed main-profile identity, fake paid facts, new limits or purchase
activation. Inactive/unlimited loading writes nothing and requires no bounded choice. Choices are financial-profile
preference data in the normal database backup, not security, entitlement or credential facts; restore does not retag
a foreign financial scope. The chooser UI, remaining resource bindings and external/release gates remain open.

44 new AT-143 cases (42 real SQLite / 2 actual application flows); normal parallel main 2,374 / App.Tests 382 pass,
zero failures/skips (67.349 s). AddResourceSelections migration and regenerated compiled model are exercised by
the complete suite. No visible screen or string change. [Evidence](../quality/durable-resource-choices.md).

D-141 final strict Windows/Android builds have zero warnings/errors. Normal installed Release applies the additive
migration with all 23 complete preexisting non-migration tables unchanged, an empty choice table and six original
plans still available above Free capacity. Complete signed local-test APK/package verified; enabled chooser UI,
physical-device, iOS, provider and publication acceptance remain open. See the D-141 quality record.

### 31.55. Reviewed account and Plan chooser (D-142 / AT-144)

Settings now opens a covered account/Plan availability form. One growing real target per complete original
identity keeps type/currency or entry kind, original state and unsaved choice separate. Paused plans remain paused
and count; archived/ended items remain visible but cannot occupy a slot. Counters and inline capacity feedback
follow current display digits. Save and reload reserve their own growing rows outside the scroll viewport. Rejected Save feedback stays
visible beside those actions, including when the original list is scrolled to its end.

Loading/failure hides the complete form; translated retry publishes only a complete current snapshot. Explicit
Save uses the D-141 actual-file/scoped writer and preserves the exact draft after storage, rights or stale-review
failure. Retire a committed review before navigating, so a failed return cannot submit again. Dirty reload, kind
switch and Back require one native discard decision with one shared busy guard. Live captions and help returns
retain the original unsaved identities/index; no autosave, financial posting or state rewriting.

Current commercial registration remains inactive: all eligible items are available, no subset or Save is required,
and the form explains this. There is no fake paid entitlement or new test-build limit. The snapshot review's
fictitious Free context belongs only to its separate artifact SQLite database/page; it never replaces release DI.
The account/Plan chooser is delivered; other resource choices/bindings, activation and owner/external gates stay open.

25 new actual application cases; normal parallel main 2,399 / App.Tests 407 pass, zero failures/skips (97.432 s). Strict builds, 18 form/18 registered-entry Windows combinations, six normal Release own-emulator cases and complete signed APK are recorded in [evidence](../quality/active-item-chooser.md). Physical-phone, native OS accessibility, iOS, provider, performance and publication acceptance remain open.

### 31.56. Durable goal choices and actual writes (D-143 / AT-145)

Extend the existing ResourceSelections boundary to Goals without a schema/entity change. Reviewed original names,
measurement kinds, currency and active/paused/completed/archived states are rechecked with actual file, financial
scope, verified rights, stored revision and capacity. Free's one goal counts a pause; explicit empty is retained.
Invalid/stale choices need review, and choosing an advanced retained goal never grants its paid tools.

The goal writer reads stored choice under the actual SQLite writer; it overrides only cached choice, never
entitlement/membership. Explicit new or reopened goals join a free chosen slot atomically, rather than counting
unselected retained history again. Completion/archive/delete releases only that identity; pause keeps it.
Unselected paused goals cannot resume. Retained notes/state corrections, negative earmark releases and deletion
remain available. Positive earmarks independently require AdvancedGoals and usable goal/account choices; no ledger
or balance is written. Full editor/contribution failures roll back goal, pins, plan and choice together.

Current release registration remains inactive and unrestricted. Settings still exposes Accounts/Plans. Goal
chooser UI, effective goal/contribution projection and native reminder delivery follow in subsequent ENT-02/03
slices. Other selected resources, activation and owner/provider/device/iOS/publication gates remain open.

31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](../quality/goal-resource-choices.md).

### 31.57. Reviewed goal chooser (D-144 / AT-146)

The existing Settings availability form now has Accounts, Plans and Savings goals as three growing native choices.
Read the original goal name, measurement kind and state from the actual D-143 scoped snapshot; money goals also show
their original currency, while holding-quantity goals do not display a currency as their unit. Completed goals say
Completed rather than the Plan-specific Ended. Paused goals count toward Free's one-goal allowance without resuming.
Completed/archived identities remain readable and unselectable. No goal, allocation, contribution or ledger write
occurs while selecting, navigating, refreshing captions or discarding a draft.

Explicit Save uses the reviewed actual-file writer, including empty choices and malformed-choice repair. Preserve
the exact goal draft/index/identity on stale name/kind/currency/rights or storage failure and on live language change.
Keep existing consent/busy guard, covered publication/retry, pinned Save/error and committed-review retirement.
Current unrestricted registration requires no subset or Save and grants no fake paid rights. Choosing an advanced
goal never grants its paid tools; translated explanation/example make this explicit in all six existing languages.

Goal chooser UI is delivered. Effective goal/contribution projection and native reminder filtering/routing remain
the next ENT-02/03 binding slice; other resources/operations, activation and owner/external gates remain open.

18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](../quality/goal-chooser.md).

### 31.58. Actual selected goal work and scoped reminders (D-145 / AT-147)

Read complete original goals, saved contribution rules and allocations from the actual opened database, with
new-work eligibility kept separate from original financial progress. Selection never changes funding/protection,
resumes a paused goal or grants a paid tool. Free balance-goal activity, advanced goal work and contribution reminders
are independent rights. Missing, empty, malformed, foreign and historical choices keep their original data.

New paid contribution work checks the actual chosen goal under the same SQLite writer. A paired new goal can join
its slot before that check; reject failed activity before pin/protection normalization. Retained amount corrections,
reminder disabling/removal and negative earmark releases stay available. These operations never post ledger money.

Prepare goal reminders only for eligible original progress before the pending cap. Keep saved opt-in, generic/details
privacy, fixed 09:00 device-local rule dates, existing permission and the shared bounded queue. Native returning data
contains bounded original goal/date members and an opaque actual-file identity. Taps resolve that file after unlocking,
open the existing single detail or Goals root, and never switch profiles or grant new work. Retained history remains
readable after a choice change. Existing legacy historical links remain supported.

Validate actual file, cached rights, persisted choice revision and original goal/rule/allocation input stamp before
and after native publication; clear a retired queue, including failed snapshot preparation. The stamp is transient,
invariant and reflection-free, not a new schema, portable preference or credential. Current commercial registration
remains inactive and unrestricted. Goal-view contribution affordances and other remaining bindings follow separately.

36 new AT-147 actual application/real SQLite cases; focused 36 pass (9.147 s). Normal parallel main 2,484 /
App.Tests 461 pass (71.478 s), no failures/skips; test output cleaned. Strict Windows 15.49 s / Android 35.35 s,
zero warnings/errors. Six normal signed Release owned-emulator cases cover English/Persian/German, light/dark,
actual native stored single/group Intent routing, original destinations/Back, and rejection in another real profile.
The real pending queue retains all three original goals, bounded members, generic privacy and saved dates at 09:00.
No financial Save; complete original rows and exact owned bytes/sidecars plus Main/English/System restored.
This verifies stored native requests and actual Android Intent delivery, not future alarm firing at 09:00.
Complete signed installable APK verified. No layout/copy change; D-144 geometry evidence remains revision-specific.
Physical-phone, iOS, provider, OS accessibility, Store signing/publication and commercial activation remain unaccepted.

[Evidence](../quality/goal-work-reminders.md).

### 31.59. Original goal views and reviewed detail actions (D-146 / AT-148)

Keep all original goals, saved contribution rules, financial progress, funding/protection, history and original
Home pins. Show availability for new work separately from lifecycle state and financial tone; only new contribution
suggestions require current work rights. Current commercial registration remains inactive and unrestricted.

Historical and unavailable earmarks retain negative releases on their original funding account, including an archived
account. Newly opened release-only forms start with Release and an account holding the original earmark; display/choice
reloads retain explicit direction/account/amount. New positive earmarks remain independently gated. Reopening checks
free chosen capacity and the complete retained paid contribution/reminder; paused goals consume capacity without resuming.

Every detail write validates the loaded actual file, cached rights, stored choice revision, complete original goal/rule/
allocation metadata and account/holding units/availability inside the same SQLite writer. Use the actual factory-bound
connection setting for stable file identity across auto-closed reads and open native SQLite writers. Do not depend on audit time
alone: concurrent note or Home pin edits with identical timestamps must survive. One busy guard spans consent, write,
feedback and publication. Rejected writes retain drafts/lifecycle; committed writes with failed reload disable further
writes and expose growing translated feedback/retry outside the body viewport. Retry never resubmits the action.

Keep dirty explicit price/amount drafts with their original numeric culture and currency. Currency changes preserve
the draft but require explicit reentry before Save. Retained assumed-price corrections preserve the original contribution
rule and never write a holding valuation or ledger entry. Native colors/query effects remain in actual MAUI partials;
tests compile the actual non-UI presenter, view models and Home projection, never replacement UI controls or algorithms.

36 new AT-148 actual-source/real SQLite cases pass; final normal parallel main 2,520 / App.Tests 497 pass,
zero failures/skips, test output cleaned. Strict Windows and Android builds have zero warnings/errors. Actual list,
detail and Home pass 18 English/Persian/German, light/dark, 360/412/wide cases with process-local 1.5 font stress;
18 additional focused cases check the corrected neutral earmark/release explanation. Native glyphs, original tones,
44-pixel action targets, original historical Release/account, dirty price/currency feedback and actual retry/Back are
checked. All 25 complete fixture tables remain equal; original development files/sidecars and display are restored.

Six normal signed Release owned-emulator cases verify original Home/list/detail, completed goal and archived funding,
initially selected Release, enabled Save and native Back. One explicit negative earmark release is saved; complete
original goals, accounts, ledger and other rows remain equal. Only the pre-existing D-141 migration and that one
expected owned allocation differ. Original owned bytes/sidecars plus Main/English/System are restored; the final
normal signed Release is installed/stopped. No physical-device, iOS, OS accessibility, provider, startup performance,
Store signing/publication or commercial-activation acceptance is implied.

[Evidence](../quality/goal-work-views.md). Remaining ENT-02/03 and external gates are open; goal editor/contribution draft binding follows next.

### 31.60. Reviewed goal editor and explicit budget cuts (D-147 / AT-149)

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

### 31.61 Selected budget definitions (D-148)

Canonical scoped budget definition choice and separate current-work projection are implemented with
reviewed actual-file persistence. Save/Replace join or transfer chosen capacity atomically; Delete retains exact
membership checks and releases only the last current definition. Original history, paid-method corrections,
limits and financial rows remain intact. 34 new AT-150 cases; normal parallel main 2,594 / App.Tests 537 pass;
strict builds, normal Release native Save/Delete and six unchanged-editor language/theme checks, complete signed
APK and exact owned data/display restoration verified. Registration remains inactive. Budget chooser/view/alert
binding and other ENT-02/03 paths remain open. See [evidence](../quality/budget-definition-choice.md).

### 31.62 Reviewed budget definition chooser (D-149)

The actual active-items form includes canonical budget definitions, grouped across copied periods with
original scope names/current methods and unselectable history. Covered review captures full account references;
explicit Save rechecks original metadata and writes only chosen definitions. Original account names stay exact
when display digits change. 13 new AT-151 cases; main 2,607 / App.Tests 550 pass; strict builds, 18 Windows native
chooser cases plus 18 registered Settings/help cases, six normal Release emulator cases, exact data/display restoration and complete signed
APK verified. Registration remains inactive. Effective budget/view/alert bindings and other ENT-02/03 paths
remain open. See [evidence](../quality/budget-definition-chooser.md).

### 31.63 Actual budget alert activity (D-150)

Actual alert orchestration checks every eligible original current budget across calendars/currencies/scopes,
with separate chosen-work rights and complete original net spending/carry. Read-only actual-file financial/privacy
reviews guard native publication; failed/retired batches retract attempted alerts and retain retryable device levels.
Carry no longer mixes another account definition. 25 new AT-152 cases; main 2,632 / App.Tests 575, strict builds,
six hidden Windows budget renders, six normal Release Android delivery cases (five with individual taps), a final rebuilt SDK-acceptance case and complete signed APK pass;
owned data/display restored. Enforcement remains inactive. Budget view/editor/scoped returning-data and other
ENT-02/03 paths remain open. See [evidence](../quality/budget-alert-work.md).

### 31.64 Original budget notification destinations (D-151)

Budget alerts now carry the opaque actual-file identity and original saved period id. Resolve after unlock and
again in the real budget page; reject another file, deleted identities and failed reads without guessing a
replacement. Retain original currency/calendar/period and monthly navigation coordinates without changing
Home currency or financial rows. Historical/unselected paid methods remain readable without granting new work.
14 new AT-153 cases; main 2,646 / App.Tests 589 pass. Strict Windows and complete strict Android Release pass;
six hidden Windows language/theme cases plus targeted native month navigation, and six normal signed Release
Android cases each tap four exact original currency/calendar/period destinations; owned data/display restored.
Commercial activation stays inactive. Budget view/editor commercial affordances and other ENT-02/03 paths remain
open. See [evidence](../quality/budget-reminder-routing.md).

### 31.65 Reviewed original budget editor drafts (D-152)

The original budget editor publishes complete references and defaults behind a loading cover. Exact original
id/file handoff retains archived/missing account/category limits and explicit scopes in both experience modes.
Raw amounts keep their input culture through live display changes. Save rechecks the complete review under the
actual SQLite writer; stale failures retain input and require explicit reload/review. Dirty fields alone survive
reload, unchanged fields adopt current saved values, and a committed form retires before native return.
Current new-tool rights are separate from retained corrections and future-period capacity. Registration remains
inactive. Budget view copy/delete/other work affordances and the remaining ENT-02/03 paths are still open.

35 new AT-154 actual application/isolated SQLite cases pass. Final ordinary main 2,681 / App.Tests 624 pass,
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass
without warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, native
validation/stale Save/Reload/Keep edits/Close and complete 25-table restoration. Six normal Release Android
language/theme cases verify original corrections/archived references; one explicit original-total Save preserves
all other metadata and ledger rows. Final normal Release Create/Close and a targeted weekly Create/Save verify
explicit absent identity and canonical zero stored month coordinates; complete owned data/bytes/display restored.
Physical device, iOS, paid activation and product publication are not claimed.

See [evidence](../quality/budget-editor-drafts.md).

### 31.66 Reviewed original budget view actions (D-153)

Original budget view actions now use the complete displayed BudgetEditorReview for copy, confirmed replacement
and deletion, rechecked before/after confirmation and under the existing actual-file writer. Period lookup uses
original calendar/currency/explicit weekly dates and rejects ambiguity. Viewing, original correction and deletion
remain separate from paid new tools and net current-definition capacity; future periods do not consume today's
slot. Busy spans native confirmation through refresh. A committed operation whose refresh fails requires a
read-only Retry, without repeating Copy/Delete. Read-only opening no longer creates Settings; financial history
and all original calculation inputs remain visible. Current commercial registration remains inactive.

26 new AT-155 actual application/isolated SQLite cases pass. Final ordinary main 2,707 / App.Tests 650 pass
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass with
no warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, full native action
names/targets, retained paid history/correction, changed-confirmation rejection and read-only Retry. Six normal
Release Android language/theme cases verify actual original copy/delete confirmations; exactly one owned copy
and deletion of that copy preserve all original 25-table data. Original owned bytes/sidecars/Main/English/System
are restored, final signed Release installed/stopped. ENT-02/03 and remaining resource/operation/import/restore/
native paths stay open. Physical-device, iOS, provider, paid activation and publication acceptance are not claimed.

See [evidence](../quality/budget-view-actions.md).


### 31.67 Reviewed quick templates and saved queries (D-154)

Durable explicit quick-template/saved-filter choices retain every original packet, recheck revision/value and
same-timestamp changes, and use the existing actual-file scoped writer. Chosen new slots and explicit filter
replacement are atomic; retained correction/delete/history stay separate. Actual template create/apply/delete
and saved-query save/delete commands use complete reviews and one busy dialog/write/refresh guard. Queries are
captured before prompts; committed refresh failure never resubmits. Template names wrap, publication is covered
and explicit Reload is available. Six growing chooser destinations and help explain retained query history.
Current Release registration stays inactive; no schema, network, SDK, permission or portable grant is added.

29 new AT-156 cases/main 2,736 behaviours (App.Tests 679) pass after repairing one stale four-kind expectation;
only that failed case reran. Strict Windows/complete Android Release have zero warnings/errors. Eighteen hidden
Windows locale/theme/width cases and six normal Release emulator cases restore complete owned rows/bytes/display.
Physical-device, iOS, OAuth, commercial activation and publication gates remain open. ENT-02/03 are still in progress.

See [evidence](../quality/quick-resources.md).

### 31.68 Original Home template destinations (D-155)

Home quick templates publish separate original activity availability after the complete parent read. Explicitly
available later definitions remain reachable within the existing six shortcuts; retained history and all original
financial calculations remain independent. A process-local TemplateVisit carries the reviewed actual-file/full
packet to the existing entry route. A changed/deleted original, reference, choice or right, or another actual file,
is rejected without guessing a newer same-id row or saving money. Failed navigation pins translated feedback and
requires explicit read-only Reload. The actual initial chooser receives only its typed resource kind, never a
selection. Destination feedback is visible beside Save; applying a template remains a detached draft.

Opening Home no longer creates or updates Settings for its layout fallback. Existing saved layouts remain exact;
the legacy device layout is read until an explicit profile-layout Save. Commercial registration stays inactive.
No schema, SDK, permission, network request, portable preference or entitlement grant is added.

21 new AT-157 actual-source/real SQLite cases pass. The ordinary main run passes 2,754 / App.Tests 697;
three focused name-order cases bring the verified total to 2,757 behaviours / App.Tests 700. The changed flow
class passes all 21 cases; the unchanged full suite is not repeated. Pending navigation awaits its actual event.
Outputs are cleaned. Strict Windows and complete strict Android Release have zero warnings/errors. Eighteen
hidden Windows Home/chooser contexts and eighteen focused destination continuations cover en/fa/de, light/dark,
360/412/wide. Six normal signed Release emulator cases open original Home templates, fill original amount/title
and cancel/discard without financial Save. Complete owned rows/bytes/display are restored. Physical-device, iOS,
OAuth, commercial activation and publication gates remain open; ENT-02/03 remain in progress.

See [evidence](../quality/home-template-destinations.md).

### 31.69 Reviewed original device profile catalogue (D-156)

Original device profile choices now use the existing empty main id and exact original profile ids/names.
The actual Profiles form separates open/rename/delete from an explicit saved activity subset and unsaved choice.
One device preference publication contains the complete catalogue and scoped choice, with separate revisions;
legacy keys remain untouched and every catalogue/startup/backup-set reader uses the new packet after first mutation.
Opening reads only. Complete file/current-profile/provider/catalogue reviews guard publication, both delete
confirmations and creation; authentication is rechecked before committing a switch, with previous-path rollback
when that review retires. Unselected originals remain openable for retained history and correction.

Production device registration remains inactive. Per-file financial/new-money/automation bindings to the chosen
profile remain unfinished; this slice does not complete ENT-02/03 or activate commercial limits. No EF model,
schema, migration, permission, SDK, network request, credential or portable grant is added.

33 new AT-158 actual-source/real isolated SQLite behaviours pass. The ordinary main run passes all 2,790
tests (App.Tests 733), with no failures/skips; outputs are cleaned. Strict Windows and canonical complete Android
Release builds have zero warnings/errors. Eighteen hidden Windows en/fa/de, light/dark, 360/412/wide contexts
check actual native targets, complete growing captions, explicit choice Save, stale Save, retained drafts,
Cancel/Discard/Reload and unchanged complete fixture data. All review processes exit normally and exact owned
development bytes/sidecars are restored. Normal signed Release emulator checks and APK details are recorded in
the evidence document. Physical-device, iOS, OAuth, paid activation and publication gates remain open.

See [evidence](../quality/local-profile-work.md).

### 31.70 Actual-file original profile work (D-157)

The original device profile choice now composes with the existing verified actual-file commercial source.
The legacy empty main id remains separate from the independently verified financial scope; changing the current
profile preference cannot transfer rights to another file. The immutable context retains the original plan,
resource choices and scope, plus complete catalogue/legacy/provider review facts. Original writer/snapshot
validation therefore rejects retired choices, including change-and-return revisions and changes while waiting
for SQLite ownership. Device choices grant neither paid tools nor shared-space membership.

New money, definitions, selected Plan/Goal/Budget work, template activity and future Plan/Goal reminders consult the same
profile condition. Complete original financial inputs, history, query viewing, corrections, refunds, overdue
settlements, deletion, backup/restore and owned export remain independent. A malformed/unlisted catalogue never
guesses main: new work stops while separately verified historical rights remain. Both current registrations
stay inactive; this slice does not activate limits or complete every operation/import/native binding.
No EF model, schema, migration, SDK, permission, network request, credential or portable grant is added.

31 new AT-159 actual-source/real isolated SQLite cases pass. The ordinary main suite passes all 2,821
behaviours (App.Tests 764), without failures/skips; test output is cleaned. Strict Windows and canonical strict
Android Release builds have zero warnings/errors. Focused original-page Windows and normal Release owned-emulator
results, exact data restoration, retained negative evidence and the complete signed APK are recorded in
[the evidence document](../quality/profile-file-work.md). Commercial activation, physical-device, iOS, OAuth,
Store signing/publication and the remaining original-operation bindings remain open.

### 31.71 Original catalogue reminder refresh and guarded period reviews (D-158)

Original catalogue changes now reach the actual reminder watcher, so explicit activity choices rebuild the
queue without another financial action. The shared event port introduces no construction-time database read.
Subscriptions are lifetime-scoped and idempotent; a different catalogue cannot be substituted. Delayed work uses
the injected clock and exposes its actual full batch task for owned-lifetime checks. Disposal cancels future work,
detaches subscriptions and asynchronously drains current work without deleting valid native reminders.

Period-review delivery uses the same captured actual-file paid/profile facts. Complete original preferences,
first-recorded account date, language/formatting/calendar/digits and local day validate before/after native
publication. Revalidation never creates Settings. SDK request refusal is consumed for scheduled requests as well
as immediate alerts; partial pending work is withdrawn and Snooze cache commits only after successful complete
publication. Original financial rows and recovery remain independent. Current registrations stay inactive.
No EF/model/schema/migration, permission, SDK, network, credential or portable format change.

25 new AT-160 actual-source/real SQLite cases pass. The final ordinary main suite passes 2,846 tests
(App.Tests 789), with zero failures/skips, and output is cleaned. Strict Windows and canonical complete strict
Android Release build have zero warnings/errors. Original native Profiles Save drives the real watcher in an
independent fixture; complete rows/catalogue and owned development bytes are restored. Final normal Release
SDK delivery, exact owned restoration, APK details and negative evidence are recorded in
[evidence](../quality/profile-reminder-work.md). Commercial activation, physical-device, iOS, OAuth and publication
remain separate. Original Plan/Goal financial-input retirement and other operation/native bindings remain open.

### 31.72 Original reminder input retirement (D-159)

Original Plan rules/states, Goal/contribution/allocation packets and raw resource choices now retire a
reminder batch when any captured value changes, including changes at unchanged audit times or choice revisions.
Complete original accounts and the date-bounded ledger/quantity inputs used by the existing GoalProgress
calculator are captured in one deferred SQLite reader. Financial data is never filtered by activity selection.
The same coherent reader validates all original packets, preferences and first-data date before/after native
publication; a changed or failed preparation withdraws pending work and preserves the retryable Snooze cache.
Only needed ledger/quantity inputs are read. Future movements remain outside today's calculation.

Snapshot self-mutation rejection belongs to the reminder boundary: existing Goal editors can still mutate their
reviewed draft before the established explicit writer. Full raw selection values distinguish absent/empty
choices. No schema/model/migration, permission, SDK, network, credential or portable preference is added.
Commercial registrations remain inactive and test builds stay unrestricted.

24 new AT-161 actual-source/real SQLite behaviours pass; 100 related reminder/Goal editor/view cases pass.
Final ordinary main: 2,870 passed, zero failures/skips, App.Tests 813, 2m 03.711s; output cleaned.
Strict Windows and canonical complete Android Release have zero warnings/errors. Original running Goals pages
in en/fa/de at 412 pixels/dark verify stale Plan/financial retirement, reached-goal suppression, fresh retry and
complete 25-table restoration. Unchanged layout matrices and successful suites are not repeated.
APK signature/structure and all nine changed production-source PDB checksums match.
Native Release details and retained negative evidence are in the evidence document. Physical device, iOS,
OAuth, paid activation and publication are separate; ENT-02/03 and the commercial release are not complete.

See [evidence](../quality/reminder-input-work.md).

### 31.73 Structured write feedback and retained filter retry (D-160)

Original structured write rejections now explain quota counts, required Plus/Pro/add-on access, exact-space
membership and original profile/resource selection in all six languages. The shared native reporter keeps
unrelated failures on their original path and never guesses purchase, renewal, provider or activation state.
Pre-write Goal/Budget/quick-resource feedback retains the form; committed-return/reload feedback remains
distinct and never falsely says that the accepted write was not saved. Counts use safe Int64 addition,
current formatting/digit choices and direction marks; stored identities and exception details are not shown.

Saving a new filter above capacity now reaches the established reviewed atomic writer for its structured
reason. A rejected operation retires its review; the next explicit menu invocation rereads it without clearing
the current query. Exact raw filter names return to the name prompt. Name drafts are process-local and bounded
by original file identity, independently of rights; another file never inherits them. No automatic retry or
financial save, schema/model/migration, SDK, permission, network, credential or portable preference is added.
Production financial/device registrations remain inactive; test builds stay unrestricted.

19 new AT-162 cases pass with actual sources/resources and real isolated SQLite. Final ordinary main passes
2,889 / App.Tests 832, no failures/skips, 2m 49.493s, followed by cleanup. A preceding main run passed 2,885
before the real second-invocation fix; only the changed final behavior justified another run.
Strict Windows and canonical complete Android Release have zero warnings/errors. Eighteen actual Windows
reason/Close/second-Invoke cases cover en/fa/de, light/dark, 360/412/wide, with unchanged complete 25-table rows
and exact owned development bytes restored. Fictitious menu/name effects are explicit ports; reason presentation
uses the actual native adapter and buttons. Android evidence, negative artifacts and full signed APK are in
the evidence document. Platform/provider/device/commercial activation and publication gates remain separate.

See [evidence](../quality/commercial-feedback.md).

Original configured SQLite targets now stay stable across open/closed Android reporting aliases.
The shared Data helper extends the existing D-146 approach to resource reviews, writers, Plan/reminder
packets, loan/backup boundaries and their unchanged foreign-file guards. Four added real SQLite cases
verify open readers/writers, coherent financial validation and rejection of another actual file.

### 31.74 Readiness before a blocked Save (D-161)

New Goal and Budget editors explain their captured capacity, missing feature right or required explicit
selection before Save is disabled. The reason comes from the existing reviewed policy, uses all six real
translations and preserves complete raw drafts. WorkRestriction is an explanation, never a grant or a
replacement for the atomic writer. Existing budget net-definition/correction rules remain authoritative;
malformed bounded Goal choices require explicit repair, while unlimited access remains unaffected.
Production registrations stay inactive and test builds remain unrestricted.

Nineteen AT-163 actual-source/real SQLite cases pass. The final ordinary suite passes 2,908 tests,
App.Tests 851, zero failures/skips, in 2m 05.449s, followed by successful cleanup. Strict Windows and the
canonical complete signed Android Release build have zero warnings/errors. Thirty-six actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, retaining complete drafts and all
25 tables; exact owned development bytes restore. Native Android regression and negative evidence are
reported separately in the evidence document. No schema/model/migration, permission, SDK, credential,
network or portable preference is added. Physical-device, iOS, provider and activation gates stay separate.

See [evidence](../quality/pre-save-readiness.md).

### 31.75 Read-only plan comparison and actual access (D-162)

More now opens Plans and access: a read-only comparison of the owner-approved Free/Plus/Pro model and
original current local access for this exact file. Comparing tiers changes only in-memory presentation;
counts and local tools come from existing Core QuotaPolicy/PlanPolicy. Current inactive test access never
becomes a fake Free/Plus purchase. Personal rights, exact shared membership, the host's right and original
profile choice remain distinct. All history, correction, backup/restore and owned export rules stay intact.

Only built local tools are compared. Unavailable Pro online services are an explicitly separate roadmap;
optional cloud backup belongs to every tier. Plus Lifetime is the same local Plus set, without invented
renewal. No price, purchase, trial, renewal, cancellation, restore-purchase or provider status is inferred.
Continue returns without changing a plan or buying anything; actual Free has the Continue with Free caption.
The existing explicit active-item form opens without saving. Production registrations stay inactive.

Seventeen AT-164 actual-source/real SQLite cases pass. Final ordinary main: 2,925 passed, App.Tests 868,
zero failures/skips, 2m 05.555s, followed by successful cleanup. Strict Windows and canonical full signed
Android Release have zero warnings/errors. Eighteen actual Windows contexts cover en/fa/de, light/dark,
360/412/wide, all three native tier choices, complete realized row captions/values and native help/return.
Actual Release Android More/chooser/comparison/help/Continue and exact owned restoration are verified.
All eight changed production-source PDB checksums match. No schema/model/migration, permission, SDK,
network, credential or portable paid data is added. Device, provider, billing and activation gates remain
separate. ENT-04 and the commercial release are not complete.

See [evidence](../quality/product-plan-comparison.md).

### 31.76 Original editor write feedback and committed return (D-163)

Account and Plan Save now explain actual structured quota/permission rejection in all six languages,
retain complete raw drafts and place complete feedback beside the pinned Save. Successful writes cannot
be resubmitted after a failed return or follow-up; inputs become read-only and Cancel becomes Close.
Account/default-preference boundaries stay distinct, including a default update that committed before
its subscriber failed. Original change-notification failures after commit remain visible through a typed
committed outcome, never a fake pre-write rejection. Exceptions are not silently swallowed.

Both editors use original native-effect ports and actual platform-independent application sources.
Native query interfaces stay on pages. Plan picker rows retain only their actual plain-text identity/name/
selection, without unused native color/icon allocation. Account and Schedule edit copies preserve all
original fields/identities; Schedule also clones its mutable rule. A refused historical split cannot mutate
the original in-memory state, stop boundary or automatic-post anchor before an explicit corrected retry.
Original validation, signs, calendars, currency locks, category selection, split rules and atomic writers
remain authoritative. No EF property/schema, migration, permission, SDK, credential or portable-format change.

21 AT-165 cases pass; final main passes 2,946 / App.Tests 889, zero failures/skips, 2m 07.788s, followed by
cleanup. Strict Windows and canonical complete signed Android Release have zero warnings/errors.
36 actual Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Actual Save invokes
original atomic rejection/accepted writers over independent fictitious SQLite; raw drafts, pinned captions,
real input/Save enablement and Close are checked. Explicit fictitious return/consent effects are identified;
all 25 original tables and exact owned development bytes restore. Actual normal Android new-form/query
regression and exact owned restoration are separate from typed fixture/physical-device acceptance.
Production registrations remain inactive. Initial pre-attempt readiness and other operation/activation
coverage remain open; this does not complete ENT-04 or the commercial release.

See [evidence](../quality/editor-write-feedback.md).

### 31.77 Reviewed atomic account archive (D-164)

Account archive is now one reviewed SQLite transaction: original archive state, affected active/paused
plans in either account direction, exact default cleanup and original persisted activity choices commit
or roll back together. The reader captures complete original account/plan values and the local day without
creating settings. The writer reacquires/rechecks complete inputs under the actual file's writer and rejects
retired rows, day, cached access, another file or a different original notification owner.

Original Account/Plan feature/capacity decisions are shared, not duplicated. Archiving ends related plans
with established PlanActions; unarchive consumes the original account slot but never resumes ended plans.
Another concurrently chosen default remains untouched. Both original stores publish only after commit;
publication failures retain the accepted result and cannot cause a repeat. The UI blocks concurrent actions,
requires explicit consent before discarding a dirty draft, never silently saves those financial inputs,
and freezes an accepted result with Close after follow-up/return failure.

17 AT-166 cases pass. Final main: 2,963 passed, App.Tests 906, zero failures/skips, 2m 10.406s, followed by
cleanup. Strict Windows and complete signed Android Release have zero warnings/errors. 36 actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced with original native Archive/Close,
real SQL rollback and actual accepted account/plan/default writes over independent fictitious rows.
Explicit fictitious consent/alert/return effects are identified. Actual normal Android consent-decline
and exact owned restoration remain separate from typed fixture/physical-device acceptance. All four
changed production-source PDB checksums match. No EF property/schema, migration, permission, SDK,
credential, network or portable-format change. Production registration stays inactive; initial readiness
and other commercial paths remain open, not release completion.

See [evidence](../quality/account-archive.md).

### 31.78 Original Account/Plan pre-attempt readiness (D-165)

Account and Plan forms now explain original feature/capacity/selection decisions before an attempted
Save. The read-only review uses each original writer's actual configured file and cached access. Active
reads capture complete original rows and persisted choices in a deferred SQLite reader; inactive test
access performs no financial-row/choice read or paid activation. A missing persisted choice counts all
original eligible rows, matching the writer instead of a cached fallback subset. Original correction,
retained advanced-rule and historical net-slot behavior stays available.

The original advanced Plan classification is shared through Core PlanEditPolicy. Save and readiness build
the same detached candidate; readiness does not parse money, validate financial input, mutate original
state or grant a write. Explicit Refresh access keeps every raw amount/date/note/reminder/selection.
A failed or retired read disables Save with translated feedback; an actual writer rejection still retains
the draft and requires explicit fresh review. Atomic writers remain the final authority. No EF property,
schema/migration, permission, SDK/network, credential or portable-format change. Test limits stay inactive.

Fifteen AT-167 cases pass with actual application sources and isolated SQLite. The original 21 AT-165
cases also pass: known full quotas now disable Save before attempting it, so original writer-rejection
cases capture a free slot and then exercise a real later addition/access change. They still prove original
atomic rejection and an explicit corrected retry. Final main: 2,978 passed / App.Tests 921, zero failures
or skips, 2m 12.110s, followed by cleanup. Strict Windows has zero warnings/errors.

The full commercial release and external acceptance remain incomplete; detailed evidence is in
[editor-readiness](../quality/editor-readiness.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) prove native disabled Save, complete reasons/refresh, retained input/read-refusal/retry
and all 25 unchanged original tables. Normal final signed Release Android verifies actual refresh and
Cancel with retained native fields, unchanged original rows, existing D-141 migration only and exact
owned byte/sidecar restoration. Final Release is installed/stopped. Canonical full Android build has
zero warnings/errors; all six changed production-source PDB checksums match. The installable APK is
artifacts/android/zanance-d165-release.apk (82589781 bytes); signature/structure evidence is recorded
in the focused quality document. No physical-phone, iOS, OAuth, billing or release acceptance is inferred.

### 31.79 Original import/Undo accepted outcomes (D-166)

Original ledger import, holdings import and durable Undo now retain the original accepted result across
post-commit change-subscriber failure. A typed completed notification carries the exact counts/batch or
Undo result; original atomic writers, metadata, overlap rechecks, rights and rollback stay unchanged.
The application retires the accepted reviewed payload before any history/failure follow-through and
removes an accepted Undo row before fallible reload. A retained old command cannot repeat that batch.
A complete history read publishes only after success, preserving other original rows on failure.

Structured pre-write rejection is shown beside the retained preview in all six languages. Successful
follow-through failures state the completed outcome instead of pretending it rolled back. Picker
cancellation keeps the existing file/preview/choices. One busy lifetime spans Undo consent/write and
export authentication; explicit fresh review retires failed original evidence without silently saving.
Picker, existing app-lock confirmation and UTF-8 BOM cache/share effects are native adapters. Actual
non-UI application sources are compiled and tested through explicit ports, never stand-in MAUI controls.
Pinned complete feedback/review/import actions reserve a growing row outside the viewport. Original
history identity and growing Undo action have separate rows. No new feature policy, paid activation,
schema/model/migration, SDK/permission/network, credentials or portable-format change.

32 AT-168 cases pass with actual application sources, real isolated SQLite and all six resource languages.
They cover original ledger/holdings permission refusal and retry, post-commit publication outcomes,
nonrepeatable accepted import/Undo, real history-read failure with other batch retention, picker/consent
cancellation and busy guards, explicit read-refusal/retry, security/export ports and neutral complete CSV,
real SQL-trigger whole-transaction rollback, plus explicit aggregate links and complete restored metadata.
Final ordinary main: 3,010 passed / App.Tests 953, zero failures/skips, 2m 42.936s, followed by cleanup.
Strict Windows and canonical complete Android Release have zero warnings/errors. All seven changed
production-source PDB checksums match. Device/platform/provider acceptance remains separate.

The commercial release remains incomplete; see [evidence](../quality/import-outcomes.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) verify original native review/import/Undo, retained refusal and cancellation, complete
pinned reasons/results/targets, accepted subscriber-failure semantics and all 25 original tables restored.
Normal final Release Android verifies DocumentsUI picking, actual preview refresh, picker cancellation,
Import and consented Undo, unchanged original rows, existing D-141 migration only, exact owned bytes/
sidecars and Main/English/System restored. The new owned CSV is removed; final Release is installed/
stopped. APK: artifacts/android/zanance-d166-release.apk (82175915 bytes), signature/structure and all
seven production-source checksums verified. Physical-phone/iOS/OAuth/billing/activation stay separate.

### 31.80 Accepted restore and explicit completion (D-167)

Actual all-source restore is recorded before display/onboarding/lock/work/root completion; accepted
input cannot resubmit. Explicit completion retry never repeats safety/primary restore. A possibly partial
source failure requires new review/consent; interrupted new reads retain the preceding review.
35 AT-169 cases/main 3,045 (App.Tests 988), zero failures/skips; strict Windows/Android zero warnings/errors.
36 Windows contexts plus normal signed Release Android actual picker/consent/source/root and complete
25-table readback pass. Owned bytes/sidecars and Main/English/System restored; final Release stopped.
APK: artifacts/android/zanance-d167-release.apk. No schema/SDK/permission/portable-policy or paid activation.
Commercial and external acceptance remain incomplete: [evidence](../quality/restore-outcomes.md).

### 31.81 Current remaining commercial scope (D-168)

Source inspection confirms three ready operation groups: forecast/saved snapshots, receipt recognition and
loan/report/PDF analysis. Resource writers/choices/reminders/import/recovery are delivered; their historical
partial notes are reconciled in the [current inventory](../enhancements/2026-10-commercial-release/07-current-entry-point-inventory.md).
Documentation-only, no new runtime acceptance: D-167's 3,045 passing tests and complete signed APK remain
the baseline. Provider/activation/encryption/owner-text/device/iOS/release dependencies remain open.

### 31.82 Original forecast work and currency-safe saved comparison (D-169)

Approved original forecast/scenario/snapshot boundaries are implemented. Preserve Free month-end,
retained history/deletion and raw refused drafts; commit currency snapshots together without repeat.
Legacy mixed scopes never sum foreign money and remain stored unchanged.
29 AT-170 cases and the ordinary 3,074-test suite pass (zero failures/skips); cleanup and strict
Windows/complete Android Release builds pass without warnings/errors. Thirty-six existing Windows
layout contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Three further native
two-currency contexts verify per-currency saves and immutable legacy comparison with complete original
25-table restoration. Normal Release Android name/Save/details/Delete consent was exercised once;
exact owned bytes/sidecars and Main/English/System were restored. Final updated Release is installed/stopped.
Commercial registration stays inactive. Phone, iOS, real OAuth, billing and release acceptance remain open.
APK: artifacts/android/zanance-d169-release.apk. [Evidence](../quality/forecast-work.md).
Next: receipt recognition, then loan/report/PDF; the full commercial release remains incomplete.

### 31.83 Shared original receipt work (D-170)

Actual-file source/access checks, shared Home/attachment reading and typed editor destinations are
implemented. Preserve original parser, compact pending attachment, existing amount and explicit Save.
30 AT-171 cases and the ordinary 3,104-test suite pass (App.Tests 1,047, no failures/skips,
2m 30.954s), followed by cleanup. Strict Windows and canonical complete signed Android Release have no
warnings/errors. Thirty-six Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced.
Normal Release Android exercises actual Home source choice, DocumentsUI, original digital-PDF reading,
typed new editor, retained attachment rereading, existing editor and explicit discard without financial
Save. Complete prepared rows and exact owned baseline bytes/sidecars are restored; final Release stopped.
Commercial registration remains inactive; phone/iOS/camera/OCR corpus/provider/Store acceptance is separate.
APK: artifacts/android/zanance-d170-release.apk. [Evidence](../quality/receipt-work.md).
Next: original loan/report/PDF computation; external and full commercial release acceptance stay open.

### 31.84 Original loan work and confirmed installments (D-171)

Coherent actual-file loan analysis and whole original ledger payment boundary are implemented. Keep
Free balances/manual repayment/reconciliation/essential warnings; recheck original terms/source/rights
after consent and under writer. Accepted outcomes are retained without replay.
37 AT-172 cases and ordinary main 3,141 / App.Tests 1,084 pass, zero failures/skips, 2m 54.955s;
cleanup and strict Windows/complete Android Release builds succeed without warnings/errors. Thirty-six
Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, with actual native payment
consent and original ledger readback. Normal signed Release Android verifies original account/schedule,
native payment consent and exact principal/interest entries. Complete owned fixtures and baseline bytes/
sidecars/Main/English/System are restored; final Release installed/stopped. Commercial registration
stays inactive; physical phone/iOS/provider/billing/Store and full-release acceptance remain separate.
APK: artifacts/android/zanance-d171-release.apk. [Evidence](../quality/loan-work.md).
Original report packages/advanced PDF and external/full commercial release acceptance remain open.

### 31.85 Original report projection and detached PDF (D-172)

28 AT-173 application/real SQLite cases; ordinary main 3,169 / App.Tests 1,112 pass with zero
failures/skips (2m 42.749s), followed by test-output cleanup. Final strict Windows and canonical complete
Android Release builds have zero warnings/errors. Thirty-six en/fa/de, light/dark, 360/412/wide and
Simple/Advanced Windows contexts exercise real package targets, original calculations, Free refusal
without changing selection, detached actual PDF and unchanged original inputs. Final targeted PDFs
also retain Free account opening/movement/closing tables and per-account currency scopes; native goal
suggestion acceptance is checked in en/fa/de with its named-calendar rule and no ledger posting.
Normal signed Release Android exercises original report/history, actual PDF cache/platform share,
share-sheet cancellation and complete retained choices. Owned tables, cache, baseline bytes/sidecars
and Main/English/System are restored; final Release installed/stopped. Commercial registration remains
inactive. Physical phone/iOS/OAuth/billing/Store/performance/full-product acceptance remain separate.
Original report inputs and same-file Refresh, base/advanced separation, original budget/goal calculations,
detached scoped PDF, basic balances/per-account currency and reviewed original contribution Save are
implemented. Named recurrence calendars and accepted post-commit outcomes are preserved. The finite
remaining entry-point inventory now moves to bounded ENT-02/03 review. No enforcement activation,
provider facts, encryption approval or full commercial release is implied. [Evidence](../quality/report-work.md).

### 31.86 Bounded local commercial completion review (D-173)

The confirmed local ENT-02 boundaries and ENT-03 resource/history/automation integration are implemented;
the finite post-D-172 review is closed. Enforcement is inactive. Real shared-server grace/retention,
ENT-04/05, billing and native release acceptance remain open. No new runtime/build/test or APK is claimed:
the ordinary main evidence remains 3,169 passing tests and the complete D-172 package. Next ready work
is A11Y-01 Android accessibility. [Evidence](../quality/commercial-completion-review.md).

### 31.87 Native quick widget accessibility and language (D-174)

Real button roles, the 44 dp logo target and original-language RemoteViews captions/direction are
implemented. Main remains 3,169 passing tests; strict builds, twelve actual widget cases across three
languages/two app themes/two widths, actual unsaved intents and a cold Persian receiver are verified.
Complete original owned rows/bytes/sidecars, display and launcher test widgets are restored. Signed
complete D-174 APK available. TalkBack is installed but inactive; spoken/full-screen/platform/device
acceptance and A11Y-01 remain open. [Evidence](../quality/android-widget-accessibility.md).

### 31.88 Contextual original date names (D-175)

DateField and 26 original date usages publish translated purpose on actual part/calendar names,
including dynamic debt reference and Plan start/end. Main 3,169; strict builds, 18 Windows contexts
with retained partial input/three calendars, six Release Plan cases and three debt cases verified.
Original Windows data and owned Android rows/bytes/sidecars/display restored; complete signed APK.
No new data/schema/permission or date/financial policy. Spoken/full platform acceptance remains open.
[Evidence](../quality/date-context-accessibility.md).

### 31.89 Current startup baseline (D-176)

Three bounded normal Release process-cold/warm-cache samples record ready-Home upper bounds
18,670.44 / 14,981.63 / 13,107.65 ms including native dump overhead. Synchronous initial UI preparation
is identified for the next implementation, not causally quantified. QA-06 remains open; main test
count/package unchanged at D-175. No build/device/optimization claim. [Evidence](../quality/startup-current-baseline.md).

### 31.90 Original Home layout reuse (D-177)

Original sections stay attached when already in the saved order; only displaced containers move.
Default XAML order matches the existing Core default. Keep every binding, visibility choice and
financial calculation. Main 3,169, strict builds, 18 native Windows contexts and signed full APK
verified; six normal Release Android Home/return cases and exact owned-data/display restoration verified. Earlier asynchronous startup candidates were rejected
by actual Release timing and removed. QA-06 remains open; no two-second/device/release claim.
[Evidence](../quality/home-layout-reuse.md).

### 31.91 Original asset-event slices (D-178)

Home quantities retain original calculator, type/unit boundaries, event order, date and overflow.
A projection-local index avoids complete-history scans for every type. Ten new cases; ordinary main
3,179 passes, strict builds and signed APK verified. Bounded Core-only measurements preserve exact
results; six Windows caption contexts pass. Six normal Release Android caption/return cases and original Main/fixture/display restoration verified.
Whole startup, two-second/device/ANR/iOS/provider/release acceptance remains open.
[Evidence](../quality/asset-event-index.md).

### 31.92 Live original icon accessibility names (D-179)

Retained icon controls resolve original keys in the current language and refresh selected names on
attached translation changes. Lifetime-scoped subscriptions catch up after reattachment without
financial writes, control reconstruction or draft loss. Native checks and complete signed
APK verified locally; A11Y-01/02/03 and device/spoken acceptance remain open.
[Evidence](../quality/icon-language-accessibility.md).

### 31.93 Official Community direction and compatibility prerequisite (D-180 / AT-179)

Owner selected maintained official Community source and reproducible native build/package/update/
security delivery. Official 4.19.0/OpenSSL 3.5.9 LTS pinned; independent Windows native and current
Microsoft.Data.Sqlite.Core/named provider money/date/key/tamper and encrypted-backup/memory-export
checks pass. Main app and D-179 APK remain unchanged, main 3,179 baseline; databases are not yet
encrypted. Production native matrix/EF/security/reproducibility and keys/recovery/migration remain
open. See [compatibility](../../../../../docs/architecture/sqlcipher-community-build.md).

### 31.94 Independent Windows native producer (D-181 / AT-180)

Official source/tool restoration, pinned assembly-enabled Windows recipe, guarded candidate
packaging and actual original-source EF harness added outside the app. Eleven input validation
cases and first-schema-to-current 32 migrations/key/tamper/Unicode/money/date/FK checks pass.
Changed recipe resumption is rejected to prevent stale upstream objects. See build guide for
clean comparison/package evidence. Main suite remains 3,179 and D-179 APK is unchanged; original
databases are still plaintext. Native mobile/security suites, final keys/recovery/crash migration
and CI matrix remain in progress; this is not SEC-01 completion or release acceptance.

Fresh Windows DLL/package hashes match across two clean builds; both final original-schema
harness runs, native export/system-only dependency inventory and package CRC/native checks pass.
Upstream logs retain 749 warning occurrences each; warning/security review and upstream suites
remain open. These are local candidates, not warning-free production binaries or app activation.

### 31.95 Official upstream crypto suite (D-182 / AT-181)

Matching Windows OpenSSL candidate: stock suite PASS, 347 files/3,818 reported tests; 59 whole
recipe skips and 114 skip markers retained. Candidate native hash unchanged. Bounded source-warning
inspection is documented; no comprehensive warning/security acceptance is inferred. These are
separate from unchanged 3,179 main tests and D-179 APK. SQLCipher/native mobile/key/recovery/crash
migration and final app encryption remain open. See the security-validation guide.

### 31.96 Independent Android native candidates (D-183 / AT-182)

Official locked r30 producer builds API24 arm64/x64 from canonical Git archives. Two fresh native
outputs match; ELF 16 KiB alignment/hardening/exports/system dependencies and twelve hostile ELF
cases pass. X64 API36/4 KiB emulator key/tamper/minor-unit/currency/date/FK probe, deployed-byte check
and owned cleanup pass. Identical candidate packaging/CRC/native checks pass. Main 3,179/D-179 APK
unchanged/plaintext; no managed whole-app integration, arm64/API24/16 KiB device, iOS/key/crash
migration or release acceptance. See the Android build guide; SEC-01 remains in progress.

### 31.97 Stock cipher suite and initial Native CI (D-184 / AT-183)

Maintained Linux stock SQLCipher suite verifies source-built cipher/provider identity, no system
crypto fallback, 226 tests/zero errors and reported allocation cleanup. Failed initial unkeyed
identity probe retained. Independent Linux Native CI is configured for two clean Android outputs,
ELF negative tests and stock suite; hosted execution is not yet verified. This is not a full
SQLite/mobile/managed/key/crash-migration matrix. Main 3,179/D-179 app/APK unchanged/plaintext.

### 31.98 Managed Android original schema (D-185 / AT-184)

Separate complete signed Release AOT/trimmed probe passes 32 original migrations/compiled model,
original financial/settings packets and key/tamper/FK checks on x64 API36/4 KiB emulator. Shared
Windows implementation passes. Exact native slices, no default bundle/INTERNET, APK CRC/signature
and cleanup verified. Initial missing D-44 registration deadlock remains negative evidence; actual
data registration resolves it without app/generated-model change. Main 3,179/D-179 app/APK remain
unchanged/plaintext; final keys/recovery/crash migration, other platform/device gates remain open.

### 31.99 Memory-only portable snapshot primitives (D-186 / AT-185)

Official consistent memory export and bounded read-only import preserve original schema/history/
header metadata; staged encryption uses official export rather than unsupported generic format
conversion. Six tests added, full 3,185 pass; strict main builds and real Windows/Release Android
probe pass. No live backup/provider activation or temporary plaintext file; final keys/authorization/
crash commit/recovery/platform gates remain. Live application databases are still plaintext.

### 31.100 Complete typed staging verification (D-187 / AT-186)

Read-only logical schema/header/all-row typed comparison and relational/cipher integrity are
implemented before future generation acceptance. Twelve tests added; full 3,197/strict builds and
real independent Windows/Release Android proof pass. Native metadata-read crash preserved and
avoided through direct statement spans. Pointer commit, keys/recovery/crash protocol and full
platform/device activation remain open; main databases are not encrypted yet.

### 31.101 Generation protocol checkpoints (D-188 / AT-187)

Eight behavior cases and full 3,205 pass for immutable profile/generation-bound ordered transitions.
Original is selected until explicit selection. No storage/key/recovery effects are certified by
this model; durable coordinator and restart/fault tests remain open. No UI/schema/provider or
backup activation; main databases are still plaintext.

### 31.102 Resumable migration coordination (D-189 / AT-188)

Actual checkpoint/readback and ordered native-effect coordinator implemented, with missing-key,
identity/regression, retired context and cancellation refusal. Fifteen added cases/full 3,220 pass.
Native key/selector/cleanup adapters and real restart/fault/power-loss acceptance remain open.
This infrastructure does not activate migration or encryption in the app; OD-04/OD-10 still gate
final policy. Existing UI, provider and backup composition remain unchanged.

### 31.103 Native CI ownership correction (D-190 / AT-189)

Hosted failure is an actual Linux bind UID mismatch, not a cipher test failure. Shared non-root
host identity/fresh work mounts fix that boundary without relaxing source/version/network guards.
Six focused Docker permission/mount checks, parser and YAML validation pass. Application baseline
3,220/strict builds and D-179 APK unchanged; no redundant app test/UI/APK run. Corrected hosted
native build/security suite success is still pending; encryption integration remains incomplete.

### 31.104 Native parent traversal correction (D-191 / AT-190)

Actual old-image failure reproduced under a foreign non-root UID; immutable home traversal now
0755 while foreign parent writes stay refused. Seven workspace cases and exact reviewed host
identity pass. Hosted [38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) completed successfully: matching clean ABI outputs, twelve
negative ELF cases and 226 official cipher tests/zero errors. App/UI/APK and main 3,220-test
baseline are unchanged and not rerun. Other encryption/platform/recovery acceptance remains open.

### 31.105 Approved policy and Windows protected DEK (D-192 / AT-191)

OD-04/OD-10 approved: automatic local key, optional recovery key and Zanance backup recovery;
permanent sensitive Android/iOS OS-backup/transfer exclusion with completed encryption, no interim
policy or mandatory portable password. Windows CurrentUser DPAPI/context/bounded key primitives
implemented. Twelve added cases/full 3,232 pass, actual encrypted reopen across independent
Windows processes passes. Main database/provider/storage/backup still unchanged; remaining native
adapters/durable migration/recovery/platform checks remain incomplete, not owner policy questions.

### 31.106 Android native key protector (D-193 / AT-192)

Application-scoped non-exportable Android Keystore AES-256/GCM and authenticated profile/device
installation/generation envelope implemented, with explicit preparation/no unwrap replacement.
Independent signed Release AOT/trimmed emulator create/reopen and fifteen native behavior checks
pass; software level 0, not hardware acceptance. Strict main Windows/Android builds zero errors
and warnings. Managed 3,232 D-192 baseline unchanged, not rerun. Main database remains plaintext:
durable storage/recovery/crash-safe migration, iOS and final coupled OS exclusion remain open.

### 31.107 Android immutable envelope storage (D-194 / AT-193)

Original exclusive-create private-file storage with file/directory flush and bounded no-follow reads
implemented. Independent Release stored-key encrypted reopen and eight file behaviors checked.
A fictitious staged orphan is not actual kill/power-loss acceptance. Managed baseline unchanged;
main provider remains plaintext. Native selector/journal, all-profile migration/recovery, other
platform file adapters, final OS exclusion and runtime/device release acceptance remain open.

### 31.108 Transactional generation selection (D-195 / AT-194)

Actual private versioned SQLite protocol journal implemented: non-secret identity/phase only,
explicit no-replace creation, format/context checks, rollback DELETE/EXTRA and immediate
compare-and-commit. Thirteen cases/focused 82 pass. Real native hot-journal process kill recovers
uncommitted selection and committed selection persists after another kill in Windows and separate
Release Android. This is selector proof, not complete crash-safe key/export/cleanup/recovery or
all-profile encryption. Main database still plaintext; iOS/physical/power-loss gates remain.

D-195 final managed evidence: all 3,245 tests pass with zero failures/skips; test output cleaned.
Main strict Windows/Android builds pass with zero warnings/errors. Records under
artifacts/native-sqlcipher: d195-{journal-tests,full-tests,test-clean,main-windows,main-android}.log.
Independent native strict builds also pass; no visible main screen changes or phone-test handoff.

### 31.109 Concrete native migration session (D-196 / AT-195)

Actual captured session joins protected Android keys, bounded coherent memory export, complete
candidate/encrypted rollback verification, transactional selection, keyed reopen and exact plaintext
cleanup. Native independent original model/32 migrations/financial packets pass real interrupted
export and Selected restart, with missing Selected envelope refusal. Ten new guards/focused 92
pass. No ledger/schema/model/UI change or main encryption adoption. Main is still plaintext;
all-profile/startup/provider/backup/recovery/OS-exclusion integration and broader platform/crash
acceptance remain. Native software x64/API36/4 KiB evidence is not phone/arm64/iOS/power acceptance.

D-196 final managed results: all 3,255 tests pass with zero failures/skips and output cleaned.
Main strict Windows/Android builds have zero errors/warnings. Evidence under artifacts/native-sqlcipher:
d196-{session-tests,full-tests,test-clean,main-windows,main-android}.log. Independent signed Release
native build/three actual migration scenarios pass; unrelated upstream/Windows key/UI suites not rerun.

### 31.110 Protected EF factory publication (D-197 / AT-196)

Original location/factory now publishes path/credential creator atomically, avoids path-only keyed
options cache, uses exact-path noncreating/unpooled connection, validates ciphertext/provider and
retires old EF commands. Legacy backup remains plaintext-only and refuses keyed work before data
consumption; generation-aware portable backup/restore is pending composition. Ten guards/focused
102 and actual signed Release Android real-model factory reads/writes/retirement pass. No financial
schema/UI change or main activation: main is plaintext, lifecycle/provider/all-profile/recovery/
backup/OS policy and remaining platform/runtime gates are still open.

D-197 final results: all 3,265 tests pass with zero failures/skips; output cleaned. Main strict
Windows/Android builds have zero errors/warnings. Evidence: artifacts/native-sqlcipher/d197-
{factory-tests,full-tests,test-clean,main-windows,main-android}.log and native factory proof above.
No unchanged migration/crypto/Windows-key/UI matrices or main phone APK were repeated.

### 31.111 Protected portable backup export (D-198 / AT-197)

Original backup/commercial source now writes coherent protected live database as bounded portable
memory output, with no plaintext temp file and captured access checks before/after chunks. Actual
signed Release native complete typed comparison and retirement stop pass; fixed chunk bound does
not depend on pooled buffer size. Portable password remains optional. Keyed restore still requires
new encrypted generation receiver; main profiles/provider remain plaintext and current backups
unchanged. Final lifecycle/catalogue/all-profile/recovery/OS policy and external acceptance remain.

D-198 final managed evidence: all 3,265 tests pass with zero failures/skips and output cleaned.
Main strict Windows/Android builds have zero warnings/errors. Logs under artifacts/native-sqlcipher:
d198-{backup-tests,full-tests,test-clean,main-windows,main-android}.log. Changed native backup route
passes; unchanged migration/crypto/Windows-key/UI matrices and main phone APK were not repeated.

### 31.112 Encrypted restored candidate (D-199 / AT-198)

Bounded portable memory import, fresh local binding, complete pre-upgrade typed verification,
actual migration callback, keyed integrity/reopen and synchronization implemented. Six guards/
focused 108 and real first-schema-to-32-migration financial/profile preservation pass signed
Release Android. No plaintext temporary staging or live selection. Main is still plaintext;
receiver dispatch/durable selection/safety/package rollback, provider/lifecycle/all-profile/
recovery/OS policy and broader platform/runtime acceptance remain.

D-199 final managed evidence: all 3,271 tests pass with zero failures/skips; output cleaned.
Main strict Windows/Android builds have zero errors/warnings. Logs under artifacts/native-sqlcipher:
d199-{restore-tests,full-tests,test-clean,main-windows,main-android}.log. Changed native old-schema
restore route passes; unchanged migration/export/crypto/UI matrices and main phone APK not repeated.

### 31.113 Restored selection with encrypted rollback (D-200 / AT-199)

Actual existing-key selection creates/verifies/synchronizes encrypted current-data rollback before
ordered journal commit/readback and keyed reopen. Same selected dataset survives actual process
termination; current/restored financial packets and original remain, no old connection on Selected
resume. Six guards/focused 114 pass. Observation timeout reconciled live, Mono resume registration
fixed without re-creating candidate. Returns Selected, not package completion. Main stays plaintext;
outer package/source transaction/catalogue/provider/startup/all-profile/recovery/OS policy and
broader platform/retention/crash acceptance remain.

D-200 final managed evidence: all 3,277 tests pass, zero failures/skips; output cleaned. Main
strict Windows/Android builds zero warnings/errors. Logs under artifacts/native-sqlcipher:
d200-{selection-tests,full-tests,test-clean,main-windows,main-android}.log. Native same-run selected
resume proof passes after diagnosed fixes; existing candidate/selection was not re-created.

### 31.114 Validated whole-package restore dispatch (D-201)

The actual BackupService supports an optional receiver for all validated package entries, instead
of individual source writes. Failure does not fall back; borrowed decoded buffers are cleared.
The receiver is not registered in the main app. Full protected package transaction, startup/profile
integration and final encryption acceptance remain open. No UI/native/provider/schema changes.

D-201 verification: 42 backup tests and the complete 3,282-test suite pass with zero failures/skips.
Strict Windows and Android builds pass with zero warnings/errors. Logs: artifacts/native-sqlcipher/
d201-{backup-tests,full-tests,main-windows,main-android,test-clean}.log. No changed screens or native
code: no UI/unchanged native matrix rerun and no phone acceptance claim.

### 31.115 Protected package receiver (D-202)

Whole-package receiver composes actual encrypted database staging and selection with a retained
SQLCipher portable-preference capsule. Selected restart can replay partial preference writes before
protected endpoint publication without old database/package access. Main registration/encryption,
early-stage reconciliation, global lease, recovery/retention and platform acceptance remain open.

D-202 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB fixture passed
actual BackupService package receiver, first-schema/current model migrations, external kill after
first native SharedPreferences committed value, reopened original/restored capsule, replay before
actual protected factory publication and restored money/device-state checks. Encrypted preference
and database rollback retained. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-
proof-96d1bcfdefd34c75b456534548dc4fa1.json, run 8cc8558c1a494c9f944d987e0e6b5445.
The fixture uses its own named SharedPreferences with synchronous Commit; it does not certify the
main MAUI adapter's asynchronous durability. Initial native compile lacked the MAUI preferences
reference; corrected fixture to native isolated adapter without adding a production package/bundle.
Native build zero warnings/errors; complete APK signature, ZIP and both reviewed ABI hashes pass;
independent package uninstalled. Driver can use an explicitly existing built APK and checks packaged
ABI identity before installation; no unchanged native suite or UI matrix rerun.

D-202 final managed evidence: 119 infrastructure tests pass. The complete run executes 3,287 tests:
3,286 pass and one pre-existing budget input-culture assertion fails because unordered child rows
were compared by enumeration order. Correct the assertion to compare category/value pairs sorted
by category identity (same established convention as the adjacent budget test). Focused rerun of
both input-culture variants passes; do not describe the original full run as green or repeat the
complete suite without another concern. Strict Windows/Android builds zero warnings/errors;
test outputs cleaned. Logs under artifacts/native-sqlcipher/d202-{data-tests,full-tests,
budget-order-tests,main-windows,main-android,test-clean}.log. Script parser and focused packaged
ABI hash helper pass after driver-only edits; unchanged native/runtime matrices not repeated.

### 31.116 Factory maintenance ownership (D-203)

All original local-factory contexts now hold shared lifetime ownership; exclusive maintenance waits
for disposal and stops new admission. Same-factory owned contexts require current ownership and
retired owners cannot issue commands/publish. Application startup/profile integration, provider
replacement and full encryption/recovery acceptance remain open; no schema/UI change.

D-203 final evidence: 125 infrastructure tests and all 3,293 tests pass (zero failures/skips).
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Actual Android x64 API36/4 KiB protected factory additionally creates an owned maintenance context,
checks unchanged money, blocks ordinary admission, closes owner while its context is live, refuses
its next native command, then admits the waiter only after native/context disposal. Complete APK
signature and driver packaged native-ABI identity verify; owned package uninstalled and tests cleaned.
Native proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-a146cb94724947fb84be6f20740f02aa.json,
run 6c8e6cac24eb463e955482ba8ccd3d6c. The initial original-model observation expired; live PID 21141
subsequently reported OriginalModelCreated/32. Continued the same fixture with explicit completed-model
continuation, without recreating it. Preserve d203-native-factory.log and d203-native-factory-resume.log;
no timeout restart. Other logs: d203-{data-tests,full-tests,main-windows,main-android,native-build,test-clean}.log.
No UI/native crypto matrix rerun, main encryption activation or physical/iOS/performance acceptance.

### 31.117 Actual profile switch maintenance (D-204)

The original profile flow drains existing contexts before changing paths, uses owned destination
migration/settings and retains exclusion through authentication/commit or previous-path restoration.
Queued work opens only the committed/restored profile. Main encryption/provider/catalogue/startup
adoption and device authentication acceptance remain open; no new model/UI layout.

D-204 final evidence: 99 profile-related tests and all 3,295 tests pass with zero failures/skips.
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Original ProfileService/store/32 migrations/autopost/factory run in an independent Android x64
API36/4 KiB fixture: old context drains before path move, owned destination migration/settings
finish, unrelated work waits through refused authentication and reopens original main money/state,
with no ledger posting. Explicit native authentication decision port is simulated, not a real device
prompt. Full signature and reviewed packaged native-ABI checks pass; package uninstalled/tests cleaned.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-0e375f15d51e42fb955d1901d95dc132.json,
run 43b655df1e514b16a6a09209cde0ae87. Logs: d204-{profile-tests,full-tests,main-windows,
main-android,native-build,native-profile,test-clean}.log. No visible layout or unchanged crypto matrix
rerun. Remaining destructive inactive-profile file operations still need maintenance coordination,
alongside protected catalogue/provider/startup integration; main databases remain plaintext.

### 31.118 Inactive deletion maintenance (D-205)

The existing confirmed profile deletion action awaits original context drain and rechecks its review
before mutation. Cancelled/outdated requests preserve the original; synchronous compatibility calls
refuse active maintenance instead of blocking. Protected generation/key cleanup and main encryption
activation remain open; no new model/layout or policy decision.

D-205 verification: 103 profile tests and all 3,299 tests pass, zero failures/skips. Strict Windows/
Android and independent Release AOT/trim builds zero warnings/errors. Original Android x64 API36/
4 KiB profile fixture confirms inactive deletion waits for an actual retained context, then removes
only inactive database/sidecars and retains original main/catalogue/money; original drain/authentication
refusal cases also pass. APK signature and driver packaged ABI identity verify; package uninstalled,
tests cleaned. The first observation expired while PID 22737 remained live; subsequent actual result
contained all six successful flags. Record same completed run without restart/recreation:
artifacts/native-sqlcipher/d205-native-profile-proof.json, run f3f5098787bd414da33b20f85a6cfe55.
Preserve d205-native-profile.log expiration evidence. Other logs: d205-{profile-tests,full-tests,
main-windows,main-android,native-build,test-clean}.log. Authentication is the decision port, not native
biometric acceptance; no new UI layout, unchanged crypto matrix, main encryption or protected deletion
lifecycle acceptance is claimed.

### 31.119 Durable initial database key identities (D-206)

Private explicit identity bootstrap maps the legacy main/other profile ids to persistent installation
and nonempty key-binding profile/initial generation ids. Idempotent registration retains existing
identities; missing/corrupt state cannot regenerate on read. Main startup/live selector/encryption
activation and final lifecycle acceptance remain incomplete; separate operational schema, no EF change.

D-206 final evidence: 131 infrastructure tests and all 3,305 tests pass with zero failures/skips.
Strict main Windows/Android and independent Release AOT/trim builds have zero warnings/errors.
Independent Android x64 API36/4 KiB creates/commits actual two-profile identity catalogue and main
initial generation journal, externally terminates the completed process and reopens in a new one.
Exact identity fingerprint, nonempty main binding, existing registration and new-profile installation
preservation pass. No DEK/key preparation or main activation occurs. Signature and packaged reviewed
ABI identity verify; independent package uninstalled and test outputs cleaned. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b540cdeaaef44a2d82592e745cd8a3c3.json,
run ded8acc1a61547d19614cc82a3f3e58a. Logs: d206-{data-tests,full-tests,main-windows,main-android,
native-build,native-identity,test-clean}.log. Typed blob rejection was added during review before the
native fixture execution. No unchanged cipher/crypto suite, UI matrix, physical power-loss or main
whole-lifecycle acceptance is claimed.

D-206 negative build evidence: test-output cleanup was mistakenly started before Android build
finished, removing required copied assemblies/resources (MSB3030, 27 errors). Preserve
d206-main-android-clean-race.log; retry only the build after cleanup completes. No test/native
fixture restart or success claim for that failed attempt; final sequential Android build is separate.

### 31.120 Initial protected generation composition (D-207)

Actual identity and existing journal now compose through native migration/keyed cleanup into a
verified retained-key factory publication. Missing state does not regenerate; disposed key owners
refuse construction. Main provider/startup/live restored catalogue and full security lifecycle
acceptance remain incomplete; no new financial model or UI layout.

D-207 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB joins actual
stable identity, initial journal, original 32-migration model, native Keystore/envelope/fs effects,
full migration/cleanup and retained-key original factory. Saved account-name write/money survive
external process termination and cold reopen with an ExistingOnlyKeys port that forbids preparation.
Original is absent, encrypted rollback retained and disposed key owner rejects new factory access.
Initial observer expiry occurred while PID 23665 remained live; subsequent completed first-phase
result passed. Continue the same run via bootstrap-reopen only, without recreating data/key/journal.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-88e6c4bdb0fe4c3aaab969def6fcabee.json,
run a480c4301bbc4109bface2e6ab1530dc. Preserve d207-native-bootstrap.log expiration and
 d207-native-bootstrap-resume.log. Signature and reviewed packaged ABI identity pass; owned package
uninstalled. This is initial composition evidence, not main activation/live restored references,
hardware power-loss, physical arm64/iOS, final recovery/retention/OS policy or product release.

D-207 final managed evidence: 135 infrastructure tests and all 3,309 tests pass, zero failures/skips.
Strict main Windows/Android and independent Release builds zero warnings/errors. Tests cleaned
only after both main builds completed. Logs: d207-{data-tests,full-tests,main-windows,main-android,
native-build,test-clean}.log. Driver parser passes; no unchanged UI/cipher matrices repeated.

### 31.121 Durable restore generation references (D-208)

Protected package receiver can require a retained installation/profile-bound ordered restore intent
and mirror actual selected journal after verified key/files/replay. Unresolved/foreign/stale/corrupt
state cannot replace intent or choose initial fallback. Main live resolver/provider/startup and
complete replay/retention/recovery acceptance remain open; operational metadata, no EF model change.

D-208 evidence: 140 infrastructure tests and all 3,314 tests pass, zero failures/skips. Strict
main Windows/Android and independent Release AOT/trim builds zero warnings/errors. Actual Android
x64 API36/4 KiB package fixture reserves durable references before staging, externally terminates
at partial platform preference write, reopens same pending parent/target, verifies actual selected
journal/keys/files/replay and mirrors selection before protected factory publication. Original and
rollback/capsule references retained; money/device-state checks pass. The final observation expired
while PID 25465 remained live; actual result later contained all six successful flags. Record the
same completed fixture without restart: artifacts/native-sqlcipher/d208-native-restore-proof.json,
run ff097ff6eeb1401197918e20f2e57cff. Preserve d208-native-restore.log timeout evidence. APK signature
and reviewed packaged ABI identity pass, independent package uninstalled, test outputs cleaned only
after both main builds completed. Logs: d208-{data-tests,full-tests,main-windows,main-android,
native-build,test-clean}.log. No unchanged crypto/UI matrix or main startup/reference-retention/
physical/iOS/preference-durability acceptance is claimed.

### 31.122 One-time durable preference replay (D-209)

Protocol v2 records Applied only after actual host preference persistence, native marker commit and
readback/synchronization. Future opens preserve later user preferences; failure leaves replay pending.
Main platform adapters/live resolver/provider/startup and complete recovery/retention acceptance remain
open. No main model/UI layout change or version-1 reset/guessing.

D-209 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB actual package
receiver resumes externally interrupted partial SharedPreferences writes. Injected persistence failure
occurs before Applied/reference/publication; native guard confirms marker/reference remain pending.
Real synchronous SharedPreferences Commit then succeeds, encrypted marker readback passes and
protected publication/money/device-state checks pass. Later user language/culture edits are committed;
a third cold process reopens Applied state, preserves those edits and uses a persistence callback
that throws if old values are persisted again. No unchanged fixture/matrix repeat. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b3ab782cc57a4d8eb520356be0d66168.json,
run 53b6bb4b9a1a44a3ab0dfb4ec6481bda. Complete signature and driver reviewed ABI identity verify;
independent package uninstalled. This validates the independent synchronous adapter/ordering, not
main MAUI platform durability, device/iOS, main encryption/startup or recovery/retention acceptance.

D-209 final managed evidence: 142 infrastructure tests and all 3,316 tests pass with zero failures/
skips. Strict main Windows/Android and independent Release builds have zero warnings/errors.
Test outputs cleaned after both main builds completed. Logs: d209-{data-tests,full-tests,
main-windows,main-android,native-build,native-replay,test-clean}.log. Driver parser passes.

### D-210 — Persist original Android MAUI preferences before recovery completion

The Android host now registers ISettingsPersistence against the exact Preferences.Default backend
used by MAUI 10.0.110. Native PreferenceManager selects the same default SharedPreferences; a custom
IPreferences backend is refused rather than writing a guessed file. Persistence runs off the UI thread.
A fresh device-local operational marker forces a real snapshot write, rather than an empty editor
that may skip writing. Commit failure or cancellation throws; the protected receiver must not mark
its capsule Applied or publish until this completes under exclusive preference-writer ownership.
The marker is neither a secret nor a portable preference. Windows/iOS persistence, live generation
resolution, final provider/startup/recovery and sensitive-file OS exclusion remain open. The main
application database remains plaintext; registration alone does not activate encryption.

Focused acceptance compiles the original MauiSettingsStore and native persistence adapter with
actual MAUI Essentials 10.0.110 in the independent Android package. It writes only fictitious values,
checks cancellation, terminates the process, verifies a retained value and removed key in a cold
process, then cleans its own values. This is Android x64 API36/4 KiB evidence, not phone, iOS,
hardware-key, whole-application encryption or product-release acceptance.
D-210 native evidence: original MAUI 10.0.110 Set/Remove and Android native persistence pass,
including cancellation refusal and cold restart value/removal checks. Run
098f4a594a9d4cd5b79703db501727f5; artifacts/native-sqlcipher/d210-native-cold-result.json.
The Windows PowerShell 5 observer stopped on expected temporary missing result stderr; the same
live process later produced both creation success flags. Only its cold-reopen phase was launched,
without recreating the fixture. Independent signed APK verified; package uninstalled afterward.
Strict Windows/Android/independent Release builds report zero warnings and errors.
D-210 final evidence: all 3,316 managed tests passed, zero failures or skips (3m32s).
Strict Windows (21.17s), Android (75.44s) and independent Release (77.46s) builds
passed with zero warnings/errors. Logs: artifacts/native-sqlcipher/d210-*.log.

### D-211 — Retain the restored generation through cold startup composition

SqlCipherProfileGeneration.OpenRestoredAsync resumes the actual latest package under the current
location's exclusive maintenance owner. It requires the receiver's exact private restore-history
store and installation/profile key namespace before native effects. Pending preference replay and
actual candidate/rollback/journal verification finish before retaining the exact existing key.
Readback requires the latest selected target; missing or mismatched history fails closed. No original
or initial-generation fallback, key preparation, directory guessing or early publication is added.
The retained key owner verifies the provider/integrity again and publishes through the same original
protected factory lifetime used by initial migration. Its disposal still refuses new connections.

The receiver has an internal no-publication resume path for this composition. A stale publication
callback from another process is never invoked; the current host publishes only after obtaining the
retained key owner. The public restore entry retains its existing behavior. Automatic all-profile
startup routing, main provider adoption, early staging reconciliation, optional recovery and retained
file/key lifecycle remain open. Main application storage remains plaintext. This change is not a
claim of complete encryption, device/iOS/OAuth or release acceptance.
D-211 evidence: two managed guards reject foreign history/installation before key/file effects;
all 3,318 tests pass (zero failures/skips, 3m07s). Strict Windows 30.69s, Android 56.77s and
independent Release 82.66s builds have zero warnings/errors. Signed Release AOT/trim Android
x64 API36/4 KiB runs the modified actual package restore: external partial-write interruption,
pending resume into the retained owner, real protected factory money 730 and device-state checks,
then cold Applied reopen preserving later choices. The obsolete publish callback throws if called;
both opens succeed without calling it. Exact restored binding/path are asserted before publication.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-e008a03fbb624134aa6ce18f65354aeb.json,
run bcd5d26ed6ad4da48fe20a7476f0854d. Signature/reviewed ABI identities verified; independent
package uninstalled. Test outputs cleaned after terminal builds. No unchanged native producer suite,
UI matrix or main phone APK was repeated. Main automatic startup routing/provider activation and
recovery/retention remain open; this is not complete application encryption or physical-device acceptance.
### D-212 — Resolve the current protected generation from stable ancestry

OpenCurrentAsync reads the actual existing installation/profile catalogue and requires history's
complete initial key binding. Missing/corrupt/foreign history never implies initial selection and
cannot manufacture migration state. Row zero routes through actual initial migration/reopen;
any recorded restore intent routes through the captured latest receiver and retained restored owner.
Receiver target/parent/private directory and exact history are validated, including unresolved intents.
No enumeration, latest-file guess, key replacement or initial/plaintext fallback is introduced.

The host explicitly initializes history only after verified initial migration; interrupted bootstrap
still fails closed for reconciliation. This automatic composition is not yet registered in the main
startup/provider/profile lifecycle. Native producer acceptance, all-profile live adoption, recovery
and retained-file/key policy, early staging reconciliation and final sensitive OS exclusion remain.
No additional owner decision or commercial activation follows; main storage remains plaintext.
D-212 managed/build evidence: two new guards reject missing and foreign-initial history without
creating a migration journal or invoking native key work. All 3,320 tests pass (zero failures/skips,
3m02s). Strict Windows 29.55s, Android 25.64s and independent Release 77.38s builds have no
warnings/errors. Test outputs cleaned only after all builds/tests completed. Initial native routing
uses actual retained catalogue/history and an existing-only key port whose Prepare throws; cold
reopen preserves the saved account and financial packets and refuses access after owner disposal.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-60a8738158fb4efcb3de7a38de9ee1b6.json,
run 847826db8df04477b49f168da0e073b4. Creation observation expired while PID 28905 remained live;
its same completed three-success result was read, then only recorded cold continuation ran.
No initial fixture, identity or key was recreated because of observer expiry.
D-212 restored-routing evidence: actual stable identity bootstrap supplies the initial key context,
then the current resolver reads retained history after partial-write interruption and after Applied
cold reopen. Captured parent/target assertions and actual retained factory money/device-state checks
pass; later preference edits are retained and no obsolete publication/persistence callback executes.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-26a85b00255a4e55b4bf81f4acb93939.json,
run 13141ec1f63e4c078687bea65746dbc1. Signed APK and reviewed packaged ABI identities verified;
independent package uninstalled. Both current-generation branches now have actual original-model
cold process evidence; main startup/provider/profile registration and recovery/retention are still open.
No physical-device, iOS or product-release acceptance is inferred.
### D-213 — Preserve protected publication when a profile switch is refused

The original profile switch captures its complete publication inside the actual maintenance owner,
then asks the native runtime to open/migrate the destination. On authentication refusal, retired
catalogue or failed opening, rollback restores the original path and bound connection creator.
A path-only MoveTo cannot restore encrypted storage and is no longer used for this rollback.
Rollback is one-use, requires its exact live owner, never revives retired protected context identity,
and refuses a plaintext snapshot after protection is required. The host retains underlying key
owners until the transition finishes; no credentials are exposed by the publication token.

IProfileRuntime.OpenDatabaseAsync is the explicit native destination effect. The existing default
opens/migrates plaintext storage; a protected location rejects it. Final protected hosts supply
verified keyed publication, never strip protection or recreate missing plaintext. The actual MAUI
host's protected generation resolver/lifetime registration and final startup/all-profile/recovery/
retention/provider/OS-policy adoption still remain open. Main database encryption is not activated.

Concurrent initial Android/test builds hit CS2012/MSB3248 locked shared DLLs; those negative logs
are retained. Successful Windows and independent Release builds were not repeated. Only failed
Android and full test executions were retried once, sequentially after competing builds completed.
D-213 native evidence: the original ProfileService/Store/factory executes plaintext regression plus
actual encrypted main/destination switching under source-built SQLCipher and Android-bound keys.
Active context drain, owned migration/settings, refused authentication, queued keyed original reopen
(balance 1,250 with no ledger entries) and successful protected destination/return all pass.
Authentication is an explicit fixture decision port, not owner/device biometric acceptance.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-8ad234ba8cf8479c8b10a9d36869ba85.json,
run 63d29cca400447179541651a003d2527; ProtectedSwitchAndKeyedRollback=true. Original inactive
plaintext deletion remains tested separately; encrypted file/key deletion policy is not claimed.
Signed Release AOT/trim APK, reviewed ABI identities and signature verified; package uninstalled.
Strict Windows 65.03s, final Android 29.46s and independent Release 95.28s builds report zero
warnings/errors. Successful builds and the stock native producer suites were not repeated.
D-213 final managed evidence: all 3,322 tests passed with zero failures/skips (3m00s).
Two actual-factory tests retain the original bound key creator after rollback and reject repeated,
foreign-owner or protected-to-plaintext restoration. Final builds are warning/error-free; the
initial shared-DLL lock failures remain in d213-*-lock-failure.log. Test outputs cleaned only
after all builds/tests completed. No main encryption, physical/iOS authentication or release claim.

### D-214 — Own active and tentative profile keys through the real transition

SqlCipherProfileSession retains the active verified generation while a destination is tentative.
The actual ProfileService invokes finalization after committing its reviewed catalogue or restoring
its complete previous publication. The MAUI runtime forwards opening/finalization to the registered
protected session; existing plaintext hosts retain their original path and never bypass protection.
Accepted switching clears the previous mutable key owner; refused switching clears only the
candidate after the original publication is verified. No envelope, database or rollback is deleted.

Start, switch, acceptance, refusal and close require the actual live maintenance owner. Key retirement
also requires all owned contexts to be disposed. Concurrent resolution is serialized and synchronous
finalization refuses active resolution. Publication identity must belong to the exact retained owner;
keys cannot be retired based on a path string alone. Main registration/covered async startup, early
bootstrap/replay reconciliation, recovery/retained-file lifecycle, native provider and final OS policy
remain open. The main database is still plaintext; this is final lifecycle composition, not activation.

A protected host may resolve another catalogue-bound profile in the same private directory while
retaining the previous publication. It never passes through an unkeyed endpoint. Plain/foreign-root
initialization guards remain enforced, and actual journals, history, keys and files must still verify.
D-214 final evidence: all 3,324 tests pass (zero failures/skips, 3m23s). Two new session guards
reject unresolved/unstarted state and key retirement while actual owned contexts remain. Strict
Windows 24.08s, Android 19.02s and independent signed Release AOT/trim 57.60s builds have zero
warnings/errors; builds/tests were sequential where outputs overlap, with no DLL-lock retry.

Android x64 API36/4 KiB actual original-profile flow uses stable identities, actual initial migration
and history for both profiles. Session resolves through OpenCurrentAsync with an existing-only key
port (Prepare throws), reads/retains original money, rejects authentication, verifies rejected-owner
disposal while the original remains usable, then accepts destination and return and verifies all
previous owners disposed. Session close refuses an owned context and succeeds after disposal.
Native decision port is fictitious authentication, not device biometric acceptance. Independent
proof: artifacts/native-sqlcipher/d214-native-profile-proof.json; run
9434f1b35a1d438cb3cf9f76a1d60574. Observer expired; same PID 31527 completed all seven flags,
read directly without reinstall/restart/recreating data or keys. Profile observation bound increased
to 300 attempts for its now-longer two-profile migration; parser checked without repeating the run.
Reviewed ABI identities/signature verified; package uninstalled and test output cleaned after terminal
builds/tests. Main startup/provider/registration, recovery, files and OS policy remain unactivated/open.
### D-215 — Record initial ancestry before native key migration

Initial ancestry can now be captured as pending against the exact existing Original journal,
before keys/candidates are prepared. Pending row zero survives restart but cannot reserve a
restore transition. OpenCurrentAsync resumes the actual initial migration, retains/reopens its
existing native key, then mirrors Completed journal state into selected initial ancestry before
returning a publication owner. Missing/foreign history still never falls back or regenerates.
Existing verified initial history remains supported; schema/version is unchanged and typed guards
still apply. New pending state is not evidence of a completed migration or permission to open data.

The old ordering left a crash interval after initial migration but before history creation. The
new pre-key intent closes that interval through actual captured resume. Interrupted metadata
creation itself, all-profile host bootstrap, recovery UX, provider/live registration and OS policy
remain separate unfinished work; missing or corrupt records still fail closed. Main application
storage remains plaintext. No owner decision, platform key policy or financial schema changes.
D-215 native evidence: independent signed Release AOT/trim Android x64 API36/4 KiB records the
pending initial intent before native key work, then an external process stop/start completes the
same captured identity through OpenCurrentAsync and actual migration/factory. A third cold process
uses an existing-only key port (Prepare throws), retains the saved account and money, verifies
selected initial ancestry, and refuses new construction after key-owner disposal. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-7562abeb4ea24d4b9a90fbb7011b1916.json,
run 4467935ba0f242168d5dce1f3cb9d315. InitialIntent/Created/Result guards all pass. No scenario
restart/recreation or unchanged native suite/UI matrix repeat. Signature/reviewed packaged ABI
identities verified; package uninstalled. Strict Windows 19.00s, Android 18.10s and independent
Release 55.61s builds report no warnings/errors. Actual initialization is verified only after the
pending record commits; torn/failed metadata creation itself and final main activation remain open.
D-215 final managed evidence: all 3,327 tests pass with zero failures/skips (2m55s).
Three new cases verify pending initial persistence, refusal of restore before completion and
rejection of foreign/advanced journals before history creation. Outputs cleaned after terminal
builds/tests. Main activation, torn metadata creation, recovery/retention and platform gates remain.

### D-216 — Publish complete identity metadata from captured staging

Identity bootstrap now commits and fully validates a private .creating catalogue before publishing
its final path with a no-replace same-directory move. A valid staged catalogue is resumed with its
original installation/profile/generation identities and exact known profile set; no new random ids
are generated on that path. Missing/corrupt/foreign incomplete staging fails closed rather than
being overwritten. Existing final metadata or sidecars still reject new bootstrap. Read never creates.

An explicit native durability overload synchronizes staged file/directory before rename and the
parent after publication. Managed-only callers retain transactional/typed/no-replace checks but do
not claim platform power-loss qualification. Failure after final rename requires actual existing
read/reconciliation, not another CreateNew or replacement. Other journal/history creation protocols
and main lifecycle/provider/OS policy adoption remain unfinished; main storage remains plaintext.

Independent Android x64 API36/4 KiB source-built SQLCipher probe pauses after actual fsync and typed
staged read, before final publication. The external runner terminates that process. Cold resume
requires the final path absent, publishes the same staged identity fingerprint, verifies actual
initial journal binding and idempotent/new profile registration preserving installation. No keys
are prepared, app encryption activated or scenario repeated. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-58220434415e4174bc471b9760cd95e6.json,
run f93ba289610e4355aee1b0446d65bc9a. Reviewed APK ABI identities/signature verified; independent
package uninstalled. Strict Windows 26.63s, Android 17.07s and independent Release 71.92s builds
have zero warnings/errors. Script parser checked; test cleanup waits for terminal builds/tests.
D-216 final managed evidence: all 3,329 tests pass with zero failures/skips (2m47s).
Two new cases preserve staged identities after interrupted synchronization and reject a changed
profile set without rewriting staging or creating the final catalogue. Native interruption proof
is separate from these injected managed effects. Outputs cleaned after terminal builds/tests.

### D-217 — Publish complete migration and history metadata

Generation journal creation and initial restore-history creation now use the same verified staged
publication as identities. Existing/corrupt/foreign final files and sidecars are never replaced;
read never creates. Captured Original journal state and exact initial pending/selected history
must match on staged resume. Advanced journal staging cannot become a fresh Original, and a pending
history cannot be republished as verified initial selection. Typed schemas/versions remain unchanged.

Native overloads synchronize file and directory around no-replace publication. Managed-only effects
do not certify native power loss. Incomplete/corrupt staging still fails closed for reconciliation;
this does not regenerate missing keys or certify complete startup/recovery. Main provider/all-profile
bootstrap/covered async startup, recovery/retention and final OS exclusion remain open/unactivated.

Independent signed Release AOT/trim Android x64 API36/4 KiB interrupts journal publication after
actual staged fsync, resumes that exact journal, then interrupts pending-history publication after
its actual fsync. Third process resumes both bindings, retains pending initial ancestry and confirms
staging aliases gone, without preparing keys or writing financial data. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-442b5d696a9a4a8699baf81c8b31218d.json,
run 18d182139b834d9b84bd34178fd54ef1. Runner clears only its own prior metadata barrier before
its second phase; no scenario repeat/reinstallation for failure or unrelated native suite/UI matrix.
ABI identities/signature verified and package uninstalled. Strict Windows 18.74s, Android 17.54s
and independent Release 59.16s builds report zero warnings/errors. Parser checked.
D-217 final managed evidence: all 3,333 tests pass with zero failures/skips (2m46s).
Four new cases cover captured journal/history resume and rejection of advanced or differently
selected staging without publishing or rewriting it. Outputs cleaned after terminal builds/tests.
Main startup/provider/recovery and physical/platform release acceptance remain unfinished.

### D-218 — Compose preparation of all captured existing profiles

SqlCipherProfileBootstrap joins actual identities, staged journal/history creation, pre-key initial
intent, concrete migration/current-generation resolution and temporary verified key ownership for
every captured existing profile. The actual host maintenance owner excludes ordinary contexts;
private unpublished locations supply local ownership without moving the application's factory.
Only after this pass may the host's protected session publish the selected profile.

The original map must be complete, distinct, absolute and in the real location's private directory.
Existing metadata must match the captured profile catalogue. Missing installation is not inferred
as fresh: explicit initialization still refuses missing/redirected/non-plaintext originals or retained
key/migration/history/cipher state, unless resuming captured valid catalogue staging. Read-only
startup never creates identities. Missing financial databases are never manufactured here.

Native sync qualifies metadata creation; actual journals/history/current files and keys are reopened.
All encrypted rollback/envelopes remain retained, and temporary key owners are disposed before the
next profile. No intermediate application publication or fallback is added. New empty profile
creation, full restore callback/lifecycle registration, covered main async startup, provider adoption,
recovery/retention and OS policy remain open. Main application storage is still plaintext.
D-218 final evidence: all 3,337 managed tests pass with zero failures/skips (2m58s). Four new
cases reject unauthorized missing installation, duplicate original paths, changed profile capture
and missing installation with retained migration state before native key/metadata effects. Final
strict Windows 10.58s, Android 16.48s and independent Release 58.51s builds are warning/error-free;
build iterations before final guard edits are not used as final verification. Tests run after builds.

Independent signed Release AOT/trim Android x64 API36/4 KiB prepares two actual original 32-migration
profiles through the complete new bootstrap. Original main/work entries 250/880 and opening balance
1,000 remain distinct. No application endpoint changes during the pass; retained candidate and encrypted
rollback verify for both. A cold pass with a key port whose Prepare throws preserves all stable identity
fingerprints, financial packets and rollback. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-0796e72e6cdc4fe98e3783c3ce11b8d5.json,
run 6f788670f1204d3d8e32072835e6a0a1. Created/Result all eight guards pass, with no scenario restart
or unchanged native suite/UI matrix. ABI identities/signature verified; independent package uninstalled.
Outputs cleaned after terminal tests/builds. This verifies captured existing profiles, not automatic
new-profile creation, inactive restore policy/UI, main activation, physical/iOS or release acceptance.

### D-219 — Initialize new profiles without plaintext financial files

SqlCipherNewProfile creates the actual current schema in an unpooled in-memory database and exports
bounded memory into verified encrypted candidate and rollback files. A dedicated new-profile intent
captures explicit creation separately from existing-profile migration. Missing existing state never
authorizes an empty replacement. Existing originals/sidecars, unknown ciphertext and foreign or
selected ancestry fail closed before schema/key work. Native synchronization qualifies checkpoints
and the no-replace rename into the existing generation journal. The normal current-generation owner
then verifies the retained key before application publication.

An interrupted unpublished empty-schema attempt may rebuild only under its captured KeyPrepared
intent, using the existing key. The original financial path never exists on disk. Both encrypted
packets are fully compared before selection; later ordinary writes are retained on cold open.
This infrastructure is not yet registered in the main product's profile creation/startup. Main
storage remains plaintext; provider adoption, lifecycle/catalogue registration, optional recovery,
retention and coupled final OS backup policy remain unfinished. No new owner decision is required
for this step; OD-04 and OD-10 remain the approved final policy.

D-219 managed verification covers 3,340 cases: the full solution run passed all other assemblies
and reported only the two new fixture-path failures (3m02s). After the fixture-only correction,
all 166 Data cases pass with zero skips/failures. No unresolved failure remains; the unchanged
assemblies were not rerun. Test output cleaned only after all builds/tests became terminal.

### D-220 — Prepare new protected profiles before catalogue publication

The real Profiles Add command now awaits ProfileService.CreateReviewedAsync. Automatic posting is
serialized, existing contexts drain, native preparation finishes without moving the active endpoint,
and the original catalogue/rights are rechecked before publishing the new profile and selected slot.
No await holds the synchronous catalogue gate. Failed preparation, cancellation or a retired review
cannot publish the proposed profile. The synchronous legacy creation API refuses protected storage.

SqlCipherProfileSession retains its current key while an explicit native new-profile callback prepares
an unpublished destination; pending transitions or unavailable callbacks fail closed. MAUI forwards
to this path only when the protected session is registered. Existing plaintext hosts preserve lazy
creation. LocalDatabaseLocation exposes protection status without exposing credentials or creators.
No page layout, strings or financial algorithm changed.

The final native host must retain/reconcile uncommitted identity/creation artifacts after a rejected
publication, rather than deleting referenced keys or treating missing history as a fresh profile.
This step does not register that host, activate main encryption, implement orphan reconciliation,
optional recovery, retention or coupled OS policy. These remain in the full SEC scope.

D-220 verification: all 3,343 solution tests pass, zero failures/skips (3m03s), including three new
actual-flow cases for context drain/unpublished preparation, failure and catalogue retirement.
Strict final Windows 24.34s, Android 18.90s and independent Release 57.27s builds have zero warnings/errors.
The first Windows attempt exposed use of an internal ownership helper across assembly boundaries;
the final adapter uses the public captured-publication ownership check. Intermediate builds are not
final evidence. The session itself still enforces the real drained owner and retained publication.

Independent signed Release AOT/trim Android x64 API36/4 KiB runs actual ProfileService creation and
switching, native key preparation, identity registration, in-memory 32-migration schema and protected
session publication. New profiles are absent from product preferences until their encrypted files
verify; creation leaves the main endpoint unchanged. Distinct opening balances 250/880 survive a
cold process and a real product switch. Native MAUI preferences are explicitly durably persisted.
Proof: artifacts/native-sqlcipher/build/android-d/d220-profile-creation-proof.json,
run f036ded6af784345bf6047ebc3f8b39e. The runner advanced from successful creation to cold reopening,
then its bounded observation expired. PID 6074 remained live and its exact later cold-result JSON
passed all three checks. No process/scenario restart or fixture recreation was performed. The
observation failure is retained as negative harness evidence, not described as an application failure.
Both APK ABI hashes/signature verified; independent package uninstalled. No layout/strings changed;
this runtime proof does not certify main UI rendering, phone, arm64/API24/16KiB, iOS or main encryption.

Main Release APK built once with Build-AndroidApk.ps1: artifacts/android-d220/pro.vafadar.zanance-Signed.apk
(83,294,522 bytes). Signature, exact pro.vafadar.zanance package, API24/36 and both ABI embedded
libassembly-store.so plus application AOT libraries verified. This uses the local test certificate,
not Store signing; no main/owner database was read or installed over for this step. It still contains
the unactivated plaintext main provider. Test cleanup follows terminal builds/tests only.

### D-221 — Review Windows upstream atomic-width warnings

The two C4133 diagnostics are in unchanged OpenSSL tsan_add calls for 32-bit new_nid and
global_seed. The MSVC macro type-checks both arms of a sizeof-based conditional; its unselected
64-bit arm causes the pointer diagnostic. Exact original obj_dat.obj disassembly uses lock xadd
dword ptr at new_nid. Exact property.obj uses lock add dword ptr at global_seed because its fetch
result is unused. Both original operations are 32-bit; no upstream source patch or warning suppression
was added. This is compiler/source evidence, not a claim that incompatible pointer warnings are
universally harmless. See the official [intrinsic signatures](https://learn.microsoft.com/en-us/cpp/intrinsics/interlockedexchangeadd-intrinsic-functions?view=msvc-170).

Test-WindowsAtomicWidth.ps1 verifies the exact header against the pinned official Git object, the
unchanged candidate DLL hash and pinned MSVC/SDK. A compiled upstream-macro probe exercises -1 + 1
with an adjacent sentinel (an incorrect 64-bit addition would carry into it), 64-bit values above
2^32 and 800,000 atomic additions across four threads. Probe disassembly separately verifies DWORD
and QWORD operations. The first verifier expected xadd for the unused global_seed result and failed;
inspection showed the valid lock add optimization. Corrected only that matching rule, then the related
probe passed. No unrelated native suite, main build/tests/UI or APK was repeated.

Static range review also bounds two C4334 diagnostics: the QUIC first-byte shift is 0..3 and its
result 1..8 fits 32 bits before widening. The two C4319 diagnostics complement a shifted unsigned
32-bit bit value, then intersect it with a final bit-0 mask; upper extension cannot affect that
condition. These are expression-level reviews, not independent acceptance of BN/QUIC algorithms.
The remaining 743 C4244/C4267 narrowing diagnostics still need review; the Windows native candidate
is not yet adopted or declared warning-free. Main encryption and the full recovery/platform scope
remain unfinished. Main tests (3,343) and signed APK are unchanged from D-220.

Proof: artifacts/native-sqlcipher/build/windows-d/atomic-width-4c3a49b4d702474aab6fc39a35172841/proof.json;
first verifier evidence retained in atomic-width-77cc4e16c9904d2ca91fda28fa00f259.
Full 749-occurrence inventory: artifacts/native-sqlcipher/d221-warning-inventory.json.
Native hash remains 3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
Review records source-header and both original-object hashes; the harness parser passes.

### D-222 — Cover the first window while preparing storage off its thread

CreateWindow now constructs only a non-financial StartupPage. OnStart runs the actual migration and
initial settings/display-unit persistence through a worker before creating financial content and
reading secure access state. Foreground posting, reminder refresh and theme/language rebuilds are
suppressed until preparation and the existing app-lock/router setup finish. Existing PIN protection
and deferred links remain separate; link navigation failures use the established generic feedback
and do not misclassify an already prepared database as failed startup.

The real loading/failure/retry surface uses dynamic theme colors, wrapping scalable text and a native
button. StartupViewModel retains the cover on preparation failure, exposes no exception payload,
rejects overlapping attempts and never reruns a completed preparation. Three new translated messages
exist in all six languages. The initial prepared settings row also supplies display units; no extra
startup settings read is added by display-unit publication.

IAppDatabaseStartup is the final initialization port. The existing plaintext host migrates only the
current legacy database and explicitly refuses a configured protected session/publication. The final
native host must replace this registration with complete captured-profile bootstrap. Main SQLCipher
provider/key/recovery/retention/OS integration remains unactivated. This is actual startup integration,
not a claim of completed encryption, the two-second performance target or platform/device acceptance.

Diagnostic routes now await actual startup before accessing data. A Debug-only thread observation
records actual storage work on a different thread from window creation. The startup review renders
real controls with fictitious preparation outcomes, invokes the native retry target and compares all
stored table rows. Run-Snapshots.ps1 -UseExistingBuild reuses only a caller-owned verified Windows
Debug executable for distinct layout cases; default behavior still builds and fails on build errors.
Callers must not infer success from a stale LASTEXITCODE when no native build command was run.
D-222 managed verification: all 3,346 cases pass, zero failures/skips (2m55.9s), including actual
StartupViewModel overlap/completion, explicit failure retry and refusal of plaintext startup with
a configured protected session. Eighteen native Windows cases cover en/fa/de, light/dark and
360x800/412x892/1280x820 at process-local FontScale 2. Thirty-six own-window captures show complete
loading/failure captions. Real native retry Invoke, at least 44px targets, actual storage/window
thread separation and complete stored table-row equality all pass. Records: artifacts/d222-startup.
The original app-data directory was restored without reading its contents; final fictitious data
was parked separately. No desktop focus/input, whole-desktop capture or OS setting change was used.

The first diagnostic compilation passed a language string where AppLanguage was required; corrected
the exact language lookup. A later matrix caller mistook stale LASTEXITCODE for a failed skipped build;
the already successful light-360 case was retained, not repeated. Remaining cases used terminating
script failures and actual proof validation. Do not erase these negative harness observations.

Final ordering review found the early reminder watcher could schedule a read after the initial
settings write. WatchChanges now attaches only after storage/access preparation. This changes only
the MAUI host subscription order, not the tested non-UI sources or reviewed layout; their passing
checks are retained. Final strict builds and cold application QA cover the host change.

An isolated functional startup package overrides ApplicationId to
pro.vafadar.zanance.startupverification; it never installs over or reads the existing Android package.
The Debug variant failed OS process attach/start timeout before Activity startup, with no managed
startup error reported. Its negative observation remains; Release QA uses the phone build settings.
This is application startup QA, not an encryption research probe or acceptance of OAuth/device/PIN,
performance, iOS, complete encryption or release. No native FLAG_SECURE was removed.
D-222 Android completion: isolated signed Release AOT/trim QA visibly displayed the actual preparing
surface, then reached the real onboarding wizard with Welcome/Language/Next and no forced account.
The process remained live. A slow observed migration was initially treated as a possible dispatcher
problem; adding the real Window.Created dispatcher hook did not establish that diagnosis. Temporary
QA-only fixed milestone/thread records showed window/start/prepare/worker/storage/settings/UI
continuation/access/ready all completed. No deadlock or dispatcher failure is claimed. Slow emulator
observations do not meet the performance target. The per-window start hook is retained with the
existing duplicate-attempt guard; its normal startup and compatibility callback are verified together.

All temporary milestone source, compile flag and project override were removed before final delivery.
The isolated QA package and exactly owned dump files were removed. Existing main Android data/package
was never installed over or read. Records: artifacts/native-sqlcipher/d222-qa-{milestones.txt,
release-ui.xml,proof-ready.xml}. Final normal Windows 20.96s and Android 69.98s builds have no warnings
or errors; earlier intermediate build timings are not final delivery evidence. No non-UI tested source
or layout changed during these host/diagnostic corrections, so the passing 3,346 cases and 18 geometry
cases were retained. The normal phone APK is rebuilt after diagnostic removal.

Final normal phone artifact: artifacts/android-d222-delivery/pro.vafadar.zanance-Signed.apk.
Package pro.vafadar.zanance, local test signature, API24/36, arm64/x64 assembly stores verified.
It is a complete Release package; main storage is still plaintext. All temporary proof instrumentation
and build flags are absent from final source. Test outputs were cleaned after all builds/tests ended.

### D-223 — Device-key protection for internal restore safety packages

BackupService now accepts an explicit IBackupSafetyCopyProtection port. Safety-only creation builds
its ZIP in bounded memory, protects it before the first storage upload and clears its owned package
buffers. Portable backups retain the independent optional password choice. DeviceKeySafetyCopyProtection
uses a separate durable platform key per safety copy, bound to the captured profile/installation and
fresh generation; it never borrows the live database key. Opening reads only the existing key and
rejects foreign contexts before key access. No missing-key preparation or plaintext ZIP fallback occurs.
The envelope uses standard AES-256-GCM with a fresh nonce and authenticated non-secret binding/header.
Creation and safety extraction bound cumulative source bytes and package bytes to 256 MiB. Failed
manifest/checksum validation clears materialized entries. This is a format/storage integration, not
a custom cryptographic algorithm or a replacement for SQLCipher.

Two real safety uploads in one second exposed the existing timestamp filename overwrite. Protected
copies now have unique opaque .vbs names outside portable discovery/retention. Generic portable pruning
cannot delete these referenced recovery artifacts. Final native registration still needs private
storage, reference-aware retention and orphan reconciliation; skipping pruning is not that final policy.

Focused Backup tests pass 54/54 and Data tests 169/169. The independent signed Release AOT Android
package uses the actual BackupService, DeviceKeySafetyCopyProtection and AndroidGenerationKeyStore.
Two protected copies restore exact fictitious content, tamper fails authentication, and a cold process
reopens both using a port that forbids key preparation. The cold observation deadline expired; the
same process subsequently wrote its successful result, read without restart. Negative timeout evidence
is retained. Native build has zero warnings/errors. Proof: artifacts/native-sqlcipher/d223-safety-native-proof.json,
run 26da5f287da548f09acdf6abd6d37bf4. The runner now gives these cold phases its established longer bound.

Main protection registration remains inactive. Main database/provider/session/bootstrap, final
Windows/mobile matrix, optional recovery, key loss, reconciliation, retention and coupled OS exclusion
remain unfinished. SEC-08 is in progress; this does not close it or authorize product release.
No visible UI changed; unchanged layout matrices and upstream native producer suites were not repeated.
D-223 final managed verification: full suite passes 3,360 cases, zero failures/skips (3m06.078s).
The additional oversized-source-before-key/write case subsequently passes with all 54 Backup cases;
combined verified behaviours total 3,361. Strict Windows (25.42s) and Android (51.67s) builds have
zero warnings/errors. No unrelated UI matrix or native source suite was repeated.

D-223 phone handoff: artifacts/android-d223-delivery/pro.vafadar.zanance-Signed.apk is a complete
79.1 MiB Release package. apksigner verification succeeds; package pro.vafadar.zanance, API 24/36,
arm64-v8a/x86_64 assembly stores are present. Signed with the existing local test certificate.
Main storage remains plaintext; this APK is not encrypted-main, phone, iOS or release acceptance.

### D-224 — Compose all-profile preparation with covered application startup

ProtectedDatabaseStartup now implements the real IAppDatabaseStartup contract. It waits for drained
application ownership, captures every original profile, invokes the complete native bootstrap and
publishes only the selected original through SqlCipherProfileSession after all profiles verify.
Repeated completed callbacks do not resolve or replace keys again. Explicit first-install authority,
provider adoption and actual native key/file/restore effects are supplied by the final host; missing
metadata never grants that authority. No registration or main-app encryption activation occurs yet.

ProfileService.CaptureStorage reads only device catalogue preferences and retains an immutable path
map plus an actual catalogue recheck. It includes every retained profile regardless of commercial
availability. Financial access and commercial quota evaluation are inappropriate before storage is
prepared. A changed catalogue invalidates the captured preparation before session publication.
The native-independent tests compile these actual app sources; 19 ProfileTests pass including both
new capture scenarios. Native provider acceptance and successful composed startup are recorded
separately below, rather than inferred from these capture tests.

Main native/provider/startup registration, fresh installation, profile lifecycle reconciliation,
restore routing, optional recovery, safety retention and coupled OS exclusion remain in progress.
No visible layout changed and unchanged layout matrices/native producer suites are not repeated.
D-224 actual native verification: independent signed Release AOT Android x64/API36/4 KiB
initial and cold runs pass. The actual startup composes two existing originals before a single
selected-work publication; amount 880 and opening balance 1000 remain correct. Repeated startup
does not resolve or publish again; cold key preparation is forbidden. Both original plaintext
files are absent. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-0745a8874d4c489eb2e71304eb953135.json
(run 830eb2f81d5c4424a6447793485c02cd). Native build: zero warnings/errors, 69.24s.

D-224 final managed verification: all 3,363 tests pass with zero failures/skips (3m26.055s).
Strict Windows and Android builds have zero warnings/errors (21.11s and 83.05s). Main storage
remains plaintext because final native/recovery/retention/OS registrations are not activated.

D-224 signed phone artifact: artifacts/android-d224-delivery/pro.vafadar.zanance-Signed.apk
(79.5 MiB), existing local test certificate, apksigner verification succeeds, package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; independent protected startup proof is not activation, device
acceptance, recovery completion or product release. No new screen layout was introduced.

### D-225 — Explicit encrypted fresh-install startup

ProtectedDatabaseStartup now distinguishes ExistingInstallation, LegacyPlaintextProfiles and
NewInstallation. Absence of an identity file never selects a mode. NewInstallation requires
one captured main profile and the real schema builder; identity metadata must stay in that
private directory. Retained database/journal/key artifacts refuse creation of replacement
identities. Its own staged identity publication may resume under the explicit new-install authority.

The actual SqlCipherNewProfile path creates all 32 application migrations in memory, writes
only verified encrypted candidate/rollback files and returns to the same all-profile bootstrap
and single session publication. No account or financial movement is manufactured. Existing
installation startup never runs the schema builder or prepares replacement keys on cold read.
Main host registration remains pending final recovery, retention and lifecycle adoption.

Three actual app-source authority tests pass. The first byte-preservation fixture check failed
because its completed Windows SQLite migration retained a pooled file handle; the fixture now
closes only its own scoped pool before comparing bytes. No production pooling change was made.
Independent signed Release AOT Android initial/cold startup preserved a fictitious opening balance
of 321 and refused missing identity metadata beside retained encrypted/key artifacts. The first
cold observation deadline expired; its same-process successful result was read before any restart
and retained in artifacts/native-sqlcipher/d225-native-first-observation.json (run
31fdee67b3ac43978d8033c16166c54f). Final code limits artifact matching to file names independently
of parent names and letter case; only the cold phase of that recorded fixture is rechecked.
The runner supports explicit recorded fresh-fixture continuation and a longer bounded observation.
Final proof and managed/build results are recorded below when terminal; no main encryption or
physical-device/iOS/recovery acceptance is inferred from an independent probe.
D-225 final evidence: cold-only verification on the same recorded fixture passes with the final
signed Release AOT build; no original/profile data was recreated. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-96d18899dce344978fd693ea0b53cc69.json.
Its Created=null explicitly denotes continuation, not a repeated initial run; the first observation
is retained separately. Full managed suite: 3,366 pass, zero failures/skips (3m14.247s). Strict
Windows 26.09s and Android 44.32s: zero warnings/errors. Final independent native build 83.58s:
zero warnings/errors. Main runtime registration, recovery/lifecycle/retention/OS adoption, physical
phone, remaining native matrix and iOS acceptance remain unfinished.

D-225 phone artifact: artifacts/android-d225-delivery/pro.vafadar.zanance-Signed.apk (79.1 MiB),
existing local test certificate, apksigner verification succeeds. Actual package pro.vafadar.zanance,
API 24/36 and complete arm64-v8a/x86_64 assembly stores verified. Main storage remains plaintext;
this artifact does not establish encrypted-main, phone, iOS, recovery or release acceptance.

### D-226 — Native Windows generation keys and file synchronization

WindowsGenerationKeyStore now implements the actual generation-key effects with CurrentUser
DPAPI, canonical captured bindings and immutable final file creation. The file receives a protected
owner-only ACL at creation, is flushed through the native handle and its directory, and is reopened
and authenticated before preparation returns. A missing read never prepares a key; existing damaged,
foreign or hard-linked envelopes fail without replacement. Caller-owned DEK buffers are cleared.
A torn exclusive final write is retained for reconciliation, never repaired by fresh random key material.

WindowsPrivateFiles uses exact no-follow native handles, regular-file/link-count checks, redirected
ancestry rejection and final handle/path identity checks. It holds a non-deletable root handle during
operations. WindowsGenerationFileDurability calls real FlushFileBuffers for captured regular files
and the directory; native failure propagates. A direct owned-folder probe first observed successful
directory flush on this development filesystem. This is observed API completion, not a universal
filesystem guarantee or physical power-loss acceptance. Same-user/privileged code is outside DPAPI's
protection boundary. Official API references:
https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-flushfilebuffers
https://learn.microsoft.com/en-us/windows/win32/fileio/obtaining-a-handle-to-a-directory

Nine Windows-native storage tests pass, including repeat preparation retaining exact envelope bytes,
missing/foreign/truncated/oversized state, retired access, unrelated-file refusal and hard-link refusal.
The independent Windows native caller now uses these actual effects. Two separate processes create
and reopen the exact encrypted SQLCipher packet (Int64.MinValue), using existing keys only in the
second process. It does not replace the original complete schema/migration algorithm or rerun an
unchanged producer/upstream suite. Proof:
artifacts/native-sqlcipher/build/windows-d/key-proof-563590e555544bed8b7802c7204859df/key-reopen-proof.json.
Actual ACL readback: protected DACL, one non-inherited Allow rule for current user only;
artifacts/native-sqlcipher/d226-acl-proof.json. No credential/key bytes are logged.

Main native/provider/startup registration, profile lifecycle reconciliation, optional recovery,
retention, coupled OS policy, remaining native matrix and Windows narrowing-warning review remain
unfinished. Main storage is plaintext; this does not establish final native adoption or release.
D-226 terminal verification: 3,375 tests pass, zero failures/skips (3m04.599s). Strict Windows
56.85s and Android 49.48s builds have zero warnings/errors. Nine native Windows storage behaviours
pass; independent two-process key/database reopen and actual current-user-only protected ACL
readback succeed. Test-WindowsKeys.ps1 now enforces that ACL and records its own acl-proof.json;
the new check was applied to the same existing fictitious file without regenerating keys/data.
No visible layout changed or unchanged Android runtime/upstream native suite was repeated.

D-226 phone artifact: artifacts/android-d226-delivery/pro.vafadar.zanance-Signed.apk (79.5 MiB),
existing local test certificate; apksigner verification succeeds. Package pro.vafadar.zanance,
API 24/36 and complete arm64-v8a/x86_64 assembly stores verified. Main storage remains plaintext;
this is not encrypted-main, phone, iOS, recovery or release acceptance.

### D-227 — Complete Windows native startup and scoped original-pool retirement

Independent Windows startup now uses the actual ProtectedDatabaseStartup, profile parser,
complete migrations/bootstrap/session and WindowsGenerationKeyStore/FileDurability. Shared
FreshStartupCompatibility, ProtectedStartupCompatibility and OriginalProfileFixture remove
platform-specific copies of the fixture flow; Android injects its actual native effects and
Windows injects its actual DPAPI/file effects. These callers are not production registrations.
The initial Windows caller build reported CA1416 at deferred platform factories; explicit Windows
checks inside those factories resolve the actual reachability issue without warning suppression.

Windows fresh initial/cold startup passes: all 32 real migrations build in memory, no plaintext
original or automatic account appears, explicit fictitious opening balance 321 survives, and
missing identity beside retained state refuses replacement. First legacy two-profile startup
then exposed a real cleanup failure: the disposed work factory left an idle pooled handle open,
so Windows refused deleting its original after durable encrypted selection. Retained state and
the original failure log were preserved; no fixture or key was regenerated to hide the failure.

SqlCipherProfileBootstrap now clears only each captured original's known pools after its actual
application owner drains contexts, before native preparation/cleanup. This fulfills the migration
session's existing caller obligation; it does not clear all pools or touch unrelated profiles.
The same interrupted Windows fixture resumes with an intentionally re-primed product idle pool
and existing-key-only effects, then a separate cold process reopens both prepared profiles.
Selected work amount 880 and opening balance 1000 remain correct; one session publication/resolution
occurs despite repeated startup. Managed regression uses actual SQLite TEMP session markers to
prove target retirement while the unrelated pool survives. Its first fixture placed identity
metadata outside the captured data directory and was correctly rejected; only that fixture path
was corrected, without weakening the production directory guard. All 16 bootstrap tests pass.

Proof: artifacts/native-sqlcipher/d227-windows-startup-proof.json; retained interrupted fixture
migration-startup-proof-8b789542ac4a468e8799d3b44b2978ce, original error d227-legacy-create.log,
actual pooled-handle resume d227-legacy-resume.log and later process d227-legacy-cold.log.
Test-WindowsStartup.ps1 supplies reproducible initial/cold wrapper phases and candidate hash
checks. Its parser is checked; this turn ran the actual CLI phases separately to retain/resume
the observed failure instead of regenerating test data through the wrapper.

Main registration, profile lifecycle reconciliation, recovery, retained-file policy, final OS
exclusion, remaining native matrix and Windows narrowing-warning review remain open. These
proofs do not activate main encryption or establish physical-device, iOS, power-loss or release
acceptance. Android/shared caller runtime and terminal build/test evidence are recorded below.
D-227 terminal evidence: Android signed Release AOT x64/API36/4 KiB initial and cold runs pass
with shared fixtures and actual Android effects; selected work amount 880 is retained. The harness
initially reported a missing process after a single failed adb observation; direct revalidation
found the same live process (10399) and completed initial result. Only its cold phase then ran,
without fixture/key recreation. The harness now revalidates the same package and completion target
before classifying missing-process termination; parser checked, no simulated branch acceptance
claimed. Proof: artifacts/native-sqlcipher/d227-android-final-proof.json (run
45123c35833f4c94a5d50c2c07eb2d9b). Shared Android probe build: 71.83s, zero warnings/errors.
Full managed suite: 3,376 pass, zero failures/skips (3m13.882s). Strict Windows 19.12s and Android
49.50s: zero warnings/errors. Main adoption and full security/platform/release gates remain open.

D-227 complete signed phone artifact: artifacts/android-d227-delivery/pro.vafadar.zanance-Signed.apk
(79.1 MiB), existing local test certificate; apksigner verification succeeds. Actual package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this is not encrypted-main, device, iOS or release acceptance.

### D-228 — Retain and retry an initial protected publication after observer failure

The shared native caller injects a real first Changed-observer IOException after the protected
endpoint is installed. Before the fix, that original call disposed the already published key owner;
covered startup then rejected retry with 'requires the original unpublished endpoint'. The actual
Windows failure is retained in artifacts/native-sqlcipher/d228-reproduction.log; the existing
fictitious D-227 dataset was reused, not regenerated.

SqlCipherProfileSession retains a generation only when the actual endpoint/path/creator/version
still identify that generation after a failed initial publication. A failed-original marker admits
one startup recovery for that exact original under drained ownership. Ordinary duplicate starts
still fail. Close clears both the owned key and recovery marker. Unpublished generations still
clear on failure; no plaintext publication or replacement key is introduced.

ProtectedDatabaseStartup retains the complete prepared profile capture immediately before initial
publication. Retry requires that same actual catalogue and session owner, never merely an arbitrary
protected endpoint. It skips already completed bootstrap/schema/native key reads. The failed
notification is repeated for the exact retained endpoint without changing its identity or pools,
so later observers that did not run receive it; readiness is accepted only after notification and
publication validation succeed. Notification failures keep recovery available and startup covered.

Native Windows verification reuses the original encrypted account/balance 321 and forbids key
preparation. It proves the published key still opens actual data after failure, changed device
catalogue retry is rejected without key reads, valid retry preserves the exact endpoint and key-read
count, late observers run, and ordinary duplicate session start remains rejected. The same shared
case is available in the Android caller with StartupNotification mode. Managed tests separately
check retained-endpoint notification delivery/foreign-owner refusal and refusal of an unowned
already-protected startup. They do not substitute plaintext fixtures for native encryption proof.
Windows proof: artifacts/native-sqlcipher/d228-native-complete-proof.json. Actual final native
build/runtime and full managed results are recorded below when terminal. Main native/provider,
recovery/lifecycle/retention/OS adoption and remaining platform/warning gates remain open.
D-228 terminal verification: actual Windows and Android observer-failure cases pass. Android
proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-6b204a3f4b4c4c37bf842904a534ab33.json;
shared signed Release AOT x64/API36/4 KiB case confirms retained key/endpoint, unchanged balance
321, changed-catalogue refusal, observer delivery and no repeated key reads. Native Android
build: 66.96s, zero warnings/errors. All 3,378 managed tests pass, zero failures/skips
(3m13.563s); focused app authority cases 4/4 and publication contracts 15/15 pass. Strict
Windows 27.65s and Android 46.90s builds have zero warnings/errors. No visible layout changed,
no unchanged producer/upstream suite was repeated, and main storage remains plaintext.

D-228 complete signed phone artifact: artifacts/android-d228-delivery/pro.vafadar.zanance-Signed.apk
(79.5 MiB), existing local test certificate; apksigner verification succeeds. Actual package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this is not encrypted-main, device, iOS or release acceptance.

### D-229 — Adopt a selected whole-package restore into the active protected session

SqlCipherProfileSession.AdoptRestoredAsync resolves/rechecks the exact selected same-profile
binding/path/version through the existing native generation resolver. It retains previous and
restored key owners until the actual publication notification and maintenance drain succeed.
A post-publication observer failure leaves the restored owner installed and alive; only that
exact pending restore may resume notification/adoption. It never rolls financial data back beneath
already applied portable preferences. Ordinary profile-switch completion cannot accept a pending
restore. Successful adoption retires only the previous in-memory owner, not its retained file/key
references; repeating the accepted exact adoption performs no new resolution or publication.

SqlCipherPackageRestoreReceiver now explicitly disposes the old registered connection after
selection/replay/history verification and before its publication callback. That callback can then
release its owning EF context and satisfy the actual session's drained-owner requirement. The
receiver's existing await-using remains an idempotent failure cleanup. No ledger algorithm or
portable password policy changes; the final native host still owns registration and UI covering.

The shared native RestoreSessionCompatibility runs the actual BackupService/source/package
receiver/history/session with actual platform keys/files. A real package captures the encrypted
profile's balance 321 and allowed locale en in memory. The fixture deliberately changes balance
to 777/locale de, restores the package, injects an actual observer IOException and reads restored
finance through the still-live pending key. Exact resume then adopts without repeated preference
persistence; device-only fake security remains unchanged. It verifies the old registered connection
was closed before adoption and rejects using profile-switch completion for a pending restore.
A separate cold process resolves the recorded restore with existing keys only and refuses old
connection/upgrade/publication/persistence effects. An explicit later fictitious locale choice fr
is preserved. Independent preference effects are native-flushed fictitious file storage, not
claims of final MAUI platform preference registration.

Windows retained D-227 dataset was used. Proofs: artifacts/native-sqlcipher/d229-native-runtime.log
and d229-native-cold.log. The first run preceded the later-choice fixture step; the later fr choice
was explicitly written/flushed into its owned fictitious preference file before cold verification,
without rebuilding its finance/key state. Two managed contract scenarios prove no adoption
resolution before a started session or with a foreign maintenance owner; all 17 publication/factory
contract tests pass. Shared Android runtime and final managed/build evidence are recorded below.
Main native/restore registration, profile reconciliation, optional recovery, safety retention,
final OS policy, remaining native/warning/device/platform and product release gates remain open.
D-229 terminal verification: shared signed Release AOT Android x64/API36/4 KiB whole-package
restore, notification-failure continuation and separate cold restored-session read pass.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-4f192702b8794260919da1951279e423.json
(run 0b6bd586086d46a793e0f4e4ee0b6039). Activity-launch observation timed out while native PID
13018 was confirmed live; its same restore completed and only the next cold phase followed,
without fixture/key reset. Native Android build: 73.14s, zero warnings/errors. All 3,380 managed
cases pass, zero failures/skips (4m00.879s); strict Windows 29.73s and Android 51.99s have zero
warnings/errors. Main native/restore registration and full security/platform acceptance remain
unfinished. Fictitious file preferences qualify that effect, not final MAUI preference integration.

D-229 signed complete phone artifact: artifacts/android-d229-delivery/pro.vafadar.zanance-Signed.apk
(79.1 MiB), existing local test certificate; apksigner verification succeeds. Package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this does not establish encrypted-main, device/iOS or release.

### D-230 — Refuse legacy deletion of protected profile storage

ProfileService's legacy DeleteCore previously removed the device catalogue row before deleting
only its old original filename and portable backup prefix. Protected generations instead retain
ciphertext, journals/history, bound key envelopes and opaque safety references. Using that legacy
path would strand stable encryption identities and make the next complete bootstrap inconsistent.
That path now refuses deletion before any catalogue publication, file effect or Changed event if
the active endpoint is protected or the target has actual generation/new-profile/history/staged
metadata or ciphertext artifacts. The legacy path remains for ordinary plaintext profiles.

This is a permanent storage-authority boundary, not an implementation of protected retirement.
The final native deletion transaction must explicitly capture authorization, persist retirement
intent/catalogue outcome and reconcile its complete referenced artifact/key set across crashes.
It must also handle uncommitted creation identities. Main native activation remains gated on that
work; no deletion, reconciliation, retention or overall SEC completion is claimed here.

Four actual app-source regression scenarios preserve complete device catalogue packet, original
profile row, notification count and exact artifact bytes for protected/current and partially
prepared targets. All 23 ProfileTests pass. Native Windows uses the retained D-227 two-profile
fixture and actual encrypted session switch to main, then invokes actual deletion of inactive
work. It is refused, both profile rows/active endpoint remain and hashes of every owned retained
artifact are unchanged. Existing keys are read only. Proof:
artifacts/native-sqlcipher/d230-native-guard.log. Shared Android caller has the same focused guard
scenario. Main application still uses plaintext registration; no visible layout changes or
unchanged producer/upstream suites are repeated. Terminal platform/build evidence follows below.
D-230 terminal verification: all 3,384 tests pass, zero failures/skips (3m11.386s); strict
Windows 28.01s and Android 45.92s have zero warnings/errors. Actual signed Release AOT Android
x64/API36/4 KiB guard refuses inactive protected deletion after actual switch to main, leaving
profile catalogue/endpoint and hashes of all retained artifacts unchanged. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-3577a018f28e42409c0e679b11872c8f.json
(run f784a71ef4714eb7b293053e46de2304). Native probe build 66.88s, zero warnings/errors.
Activity observation timed out with PID 14037 confirmed live; the same run completed and cold
verification followed without reset. A later adb cleanup connection failed after the proof was
saved; the same local port was reconnected and only the owned independent package uninstalled.
This does not implement protected retirement or activate main encryption; those remain open.

D-230 signed complete phone artifact: artifacts/android-d230-delivery/pro.vafadar.zanance-Signed.apk
(79.5 MiB), existing local test certificate; apksigner verification succeeds. Actual package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this is not encrypted-main, device, iOS or release acceptance.

### D-231 — Durable retirement barriers and ordered continuation

A reviewed non-main retirement now has an exact private SQLite request binding (legacy profile,
stable profile, installation and operation), with ordered Authorized / CatalogueRemoved /
ArtifactsRemoved / Completed barriers. Metadata is staged, verified, published without replacement
and synchronized through the native durability port. Missing, foreign, incorrectly typed or extra
schema state never creates deletion authority. Main retirement is refused.

ProfileRetirementCoordinator resumes only an existing request under the host's exclusive
maintenance ownership. Host operations must be idempotent and verify actual durable catalogue,
complete captured artifacts and key-reference reconciliation before the next barrier is committed.
No filename enumeration, key deletion, inferred identity or main-app registration is supplied here.
A committed SQLite phase may remain observable after native synchronization fails. Resume therefore
synchronizes the actual journal and directory and checks unchanged state before every native effect;
reading the phase alone cannot authorize cleanup. Completed resumes validate the host without
repeating deletion. Cancellation and failed ownership/phase validation preserve the retained intent.

Nine focused real-SQLite tests pass. They include observable commit followed by native flush failure,
repeated failed requalification with no cleanup, exact successful continuation, stale/foreign/missing
requests, extra schema and retired ownership. Test durability/effect ports prove ordering only.
The independent Windows caller uses the reviewed native SQLCipher DLL and actual private Windows
file/directory flushes in two processes. The first durably clears a fictitious catalogue file, commits
CatalogueRemoved, injects an explicit flush failure and leaves both captured fictitious files intact.
A separate cold process requalifies that exact journal, removes only the captured fixture files,
synchronizes each removal and completes; repeated completion does not repeat effects. Evidence:
artifacts/native-sqlcipher/d231-native-create.log and d231-native-resume.log; the retained independently
owned directory is recorded in d231-owned-retirement-path.txt. Native build has zero warnings/errors.
These fictitious file effects are not final MAUI preference publication, a complete profile/key
inventory, forced process termination, physical power-loss acceptance or Android/iOS retirement proof.

Main registration remains plaintext. Actual protected profile retirement, uncommitted creation
reconciliation, optional recovery, reference-aware safety retention, coupled OS exclusion and remaining
native/platform/device gates are still open. No visible layout changed; upstream native producer suites
and unchanged UI matrices were not repeated. Terminal managed/main-build evidence follows below.

D-231 managed verification: the complete suite passed 3,393 cases with zero failures/skips
(3m32.653s). Final review found that LIKE treats the underscore in sqlite_% as a wildcard,
which could conceal an unrelated sqliteAlias table. Exact GLOB filtering now keeps the underscore
literal; the extra-schema regression uses that name and all nine affected cases pass again
(1.048s). Unchanged application/UI scenarios were not repeated for this isolated validation fix.
The final native Windows caller was rebuilt without warnings/errors (1.91s); both actual
Windows fixture processes pass again. Final main Windows strict build has zero warnings/errors
(25.51s). Android/package verification is recorded below. Final Android/iOS retirement effects
and main encryption activation remain open.

D-231 final main Android strict build has zero warnings/errors (42.29s). No native producer,
unchanged UI matrix or unrelated acceptance suite was rerun for the final schema-filter correction.

D-231 final signed complete Release APK: artifacts/android-d231-delivery/pro.vafadar.zanance-Signed.apk
(83,417,818 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores were checked.
Test outputs were cleaned after terminal builds/tests. This is not phone installation, native
retirement integration, encrypted main registration, store signing or product release acceptance.

### D-232 — Exact stable identity retirement after verified references

SqliteDatabaseIdentityCatalogue now removes only the reviewed non-main binding after the existing
retirement journal has reached ArtifactsRemoved and been natively requalified. It verifies the
installation/profile bindings before the host inspects financial/key references, then rechecks the
complete captured identity catalogue inside the write transaction. Foreign installations, replaced
same-legacy identities, stale journal state, changed catalogue and retained references fail closed.
The deletion compares both legacy and stable profile identifiers. Main and every other complete
binding must survive exact readback after native file/directory synchronization. A prior deletion
visible after failed flush may resume with the same journal/reference verification and synchronization;
absence alone never authorizes retirement or creates replacement keys. The journal is left at its
artifact barrier until the coordinator accepts completion. No EF model/schema change is introduced.

The independent Windows retirement caller now uses an actual two-profile identity SQLite catalogue,
not a placeholder identity file. It records the main binding, preserves work identity and its captured
fictitious artifact through the injected catalogue-barrier flush failure, then a cold process uses the
actual RetireAsync and coordinator to remove exactly work identity and verify main unchanged. Its
preference/artifact files remain fictitious; this is not a complete final MAUI/native key inventory.
Proofs: artifacts/native-sqlcipher/d232-native-create.log and d232-native-resume.log; its fresh retained
owned folder is recorded in d232-owned-retirement-path.txt. Final managed/native evidence follows below.

This closes the missing exact identity-removal operation, not the whole protected retirement feature.
Main remains plaintext. Final catalogue preference persistence, durable complete artifact/key inventory,
uncommitted creation reconciliation, optional recovery, reference-aware retention, coupled OS exclusion
and native/platform/device acceptance remain open. Unchanged UI and upstream producer suites are not
repeated. No app source algorithm is duplicated by the independent native caller.

D-232 terminal verification: the full solution passed 3,400 cases, zero failures/skips (3m11.526s).
Final pre-inspection binding validation and changed-catalogue rejection were then tightened with
one additional regression; all 199 Data-library cases pass (1.955s), including all eight AT-232
retirement scenarios. Other unchanged application/UI scenarios were not repeated for that boundary.
The final independent Windows build has zero warnings/errors (1.76s), and both actual fixture
processes pass again: target identity is retained through failed synchronization, then removed on
cold continuation with exact main binding preserved. Final strict main Windows build has zero
warnings/errors (17.41s). Android/package evidence follows below. This is not final MAUI preference,
complete native key-reference inventory, Android/iOS retirement or main encryption acceptance.

D-232 final strict Android main build has zero warnings/errors (39.36s). The repository now
contains 3,401 test cases; the final changed library's 199 cases were actually rerun. No physical
phone, iOS, complete protected retirement, recovery or product release acceptance is implied.

D-232 complete signed Release artifact: artifacts/android-d232-delivery/pro.vafadar.zanance-Signed.apk
(82,979,376 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores were verified.
The native Windows candidate retains its reviewed SHA-256 3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
Test outputs were cleaned after terminal builds/tests. Main encryption registration, complete
profile/key-reference lifecycle, physical devices/iOS/store signing and product release remain open.

### D-233 — Immutable native retirement file inventory and handle-bound Windows deletion

The retirement journal now captures a bounded immutable artifact table before catalogue removal:
named private root, direct filename, native volume/file identity, byte length and SHA-256. Missing
inventory is not an empty set. Identical Authorized capture may requalify a failed native flush;
different/later files cannot replace it. Ordered continuation and identity retirement require the
existing table. Existing metadata-only requests remain readable, but cannot perform native cleanup
without the explicitly captured inventory. The operational SQLite schema is validated exactly,
including typed bounded rows, canonical fingerprints and case-insensitive duplicate refusal.

WindowsProfileRetirementFiles captures actual no-follow regular-file handles and preflights the
complete captured set before cleanup. Each deletion reopens and verifies that same native file object,
length and bytes under the host's maintenance ownership, then marks that very handle for deletion on
close. It never resolves another unchecked filename after verification. Redirected/hard-linked or
busy files fail through the existing native handle checks. Same-content replacement is refused by
volume/file identity; changed content is refused by length/hash. Already absent entries may resume
only under the exact durably requalified CatalogueRemoved request. Actual directory synchronization
and absence checks follow. The journal and its retained authority sidecars cannot be deleted.
Native DELETE access and FILE_DISPOSITION_INFO are used as documented by the Windows API:
https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle
This is namespace cleanup, not a physical sector-erasure or power-loss claim.

All 208 Data-library tests pass, including nine new AT-233 cases. Actual Windows tests delete a
synthetic DPAPI generation-key envelope, refuse reopening its missing key without replacement,
preserve a foreign file and journal, refuse changed bytes and same-content replaced file identity
before deleting any captured entry, and reject capturing its own authority for cleanup. Metadata
cases prove immutable reopen, no later additions, explicit missing-list refusal and safe filenames.
The independent Windows two-process caller now stores the actual captured artifact table and invokes
the production native cleanup class, then the real identity retirement operation. Initial injected
flush failure retains the target file/identity; cold continuation deletes only the captured object
and target identity, preserving main. Evidence: artifacts/native-sqlcipher/d233-native-create.log
and d233-native-resume.log; owned fixture location in d233-owned-retirement-path.txt.

The native host still must discover/authenticate the complete profile database, migration/restore,
portable/safety-copy and per-generation key reference set before capture. This class intentionally
never guesses ownership, scans filename patterns or deletes a shared installation wrapping key.
Final app catalogue persistence, complete reference discovery, Android/iOS native deletion,
uncommitted creation reconciliation, optional recovery/retention/OS policy and main registration
remain unfinished. Main remains plaintext. No visible layout changed and unchanged producer/UI
matrices are not repeated. Terminal main/test/package evidence follows below.

D-233 terminal managed verification: all 3,410 solution cases pass, zero failures/skips
(2m53.233s); focused Data suite passes 208/208. The independent final native Windows caller
builds without warnings/errors (2.16s) and its two real retained-state processes pass.
Strict main Windows build has zero warnings/errors (22.38s). No unchanged UI/native producer
suite was repeated. Main registration remains plaintext and final complete reference discovery,
app catalogue/native mobile effects, recovery/retention/OS integration remain open.

D-233 final strict main Android build has zero warnings/errors (40.20s). Native Windows cleanup
is verified in the actual host tests and independent caller; Android/iOS deletion adapters are
not supplied or claimed by this step. The complete-reference discovery requirement is unchanged.

D-233 complete signed Release APK: artifacts/android-d233-delivery/pro.vafadar.zanance-Signed.apk
(83,450,586 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores are verified.
Test outputs were cleaned after terminal checks. Main encryption, full reference discovery,
app/mobile retirement, recovery/retention/OS policy, device/store and product release acceptance
remain unfinished.

### D-234 — Descriptor-relative Android retirement and captured directory identity

AndroidProfileRetirementFiles now captures UID-private native directory/file identities, lengths
and SHA-256 through no-follow directory-relative file descriptors. It preflights the recorded set,
rechecks the anchored namespace against the inspected inode, calls Bionic unlinkat through the
same directory descriptor, fsyncs that directory and verifies absence. Native missing-file errors
are distinguished from access/ownership/type errors; linked, non-regular and foreign-owned files
are refused. Descriptor duplicates are explicitly closed. The operating system's app-data alias
is canonicalized within the app's actual UID-private data scope; redirected final roots are refused.
Native ABI reference: https://android.googlesource.com/platform/bionic/+/main/libc/include/unistd.h
This is not physical erasure or a defence against privileged/same-UID code.

Review identified another namespace issue: an empty replacement directory could otherwise make
all captured files appear absent while the original directory still retained them. The artifact
record now includes the native directory identity, verified before accepting absence on Windows
and Android. The exact inventory table shape changes for this unreleased infrastructure; earlier
independent inventory shapes are refused, never silently rebound to the currently found directory.
Main remains plaintext and has no production retirement metadata to convert. An actual Windows
regression retains the moved original file and rejects cleanup/absence in an empty replacement root.

The ordered requalification/preflight/removal algorithm is shared in ProfileRetirementFileCleanup;
the native adapters supply actual object effects without duplicating it. The independent runtime
fixture is also shared, with platform key/durability/file ports and trim-safe identity serialization.
It creates real main/work identity rows and native generation envelopes, records only the work
file/envelope, injects synchronization failure after catalogue publication, then cold continuation
removes work files/identity while the main native key remains readable. Catalogue publication in
this fixture is still native-flushed fictitious file storage, not final MAUI preference integration.

The signed Release AOT/trimmed Android x64/API36/4 KiB two-process scenario passes. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-21b20f76f70f4030acf58c25a46e5688.json
(run 9d934b3c3c2e4ec7a7af130b72ef9e8a). After adding focused negative scenarios, only that new
single-phase runtime was executed: changed bytes, same-content replacement and emptied replacement
root are refused before unrelated cleanup. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-0ac16a2050d8408289b1d45d71b8ff8b.json.
The existing native lifecycle was not repeated merely to exercise the added negative cases.
Initial and final independent Android builds have zero warnings/errors (69.96s / 55.93s).
Terminal Windows/test/main-build/package evidence follows below.

This provides real native cleanup effects, not complete profile-reference discovery or final
application activation. Actual complete authenticated reference inventory, durable app catalogue
publication, uncommitted creation reconciliation, optional recovery, reference-aware safety retention,
coupled OS policy and final main/native platform gates remain open. Main remains plaintext. No
unchanged UI or upstream producer suite was repeated. Physical arm64/API24/16 KiB OS, iOS, device
and product release acceptance are not supplied by this x64 emulator result.

D-234 terminal checks: all 3,411 solution tests pass, zero failures/skips (2m51.932s), including
the new actual empty-replacement-root regression. Strict main Windows build has zero warnings/
errors (24.83s); the Android main source was already built strictly without warnings/errors
(66.37s) and production code did not change afterward. Final signed packaging follows below.
The final independent Windows build is clean (1.81s); shared changed-object refusals and actual
main/work key-envelope retirement/cold main-key read pass. Evidence: d234-native-windows-create.log
and d234-native-windows-resume.log. The owned fixture path is recorded in d234-owned-retirement-path.txt.
The Android negative runtime run is e51949403ed6465c8b585ab2b03cdb54; its three new scenarios passed
without repeating the already observed Android lifecycle. No unrelated UI/native producer suite
was repeated; final app catalogue/reference discovery and main registration remain open.

D-234 final complete signed Release APK: artifacts/android-d234-delivery/pro.vafadar.zanance-Signed.apk
(83,044,912 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores are verified.
Test outputs were cleaned after terminal checks. Main encryption activation, complete reference
inventory/app catalogue integration, recovery/retention/OS policy and external platform/device/
store acceptance remain unfinished.

### D-235 — Native profile reference discovery before retirement

ProfileRetirementReferenceDiscovery now reads the actual stable identity catalogue and profile-bound
restore ancestry before capturing files. Known initial/new-profile/restore metadata, ciphertext,
rollback/preference capsules and their sidecars are classified from that ancestry. Unknown artifacts
in the target database/key/portable namespace fail closed rather than being guessed or silently left.
All native generation envelopes in the target stable profile namespace are authenticated against
that installation/profile/generation, including orphaned safety keys. Exact native fingerprints are
checked before/after authentication and again before publication; complete directory names and all
identity rows are rechecked under the host's exclusive maintenance validation.

Local portable/legacy safety backups use the actual BackupFileName parser and exact app/profile set.
Opaque VBS safety copies require bounded native reads and full AES-GCM authentication before target
ownership is accepted. Unverified foreign headers never cause foreign key access/deletion; a matching
header alone is insufficient. Owned plaintext package/key buffers are cleared. Retained backup files
and their separate generation envelopes join the immutable inventory. Shared installation wrapping
keys and cloud backups are excluded. This capture never advances retirement or deletes financial files.

The actual native reference-file ports provide bounded direct listings, regular-file fingerprints
and bounded complete/prefix reads. Windows uses existing no-follow native handles; Android uses
its UID-private descriptor-relative file effects. Four AT-235 actual SQLite/Windows tests cover a
pending restore with retained rollback/preferences, owned/foreign safety and portable separation,
unknown generation/malformed backup refusal and matching-header GCM tamper rejection. Initial test
compilation used an incorrect history method name and exposed native-platform warnings in existing
assertion lambdas; corrected to the actual ReserveAsync and explicit callback platform guards,
without suppression. Strict focused tests pass 4/4 with no warnings.

The independent Windows caller uses the retained D-227 real two-profile encrypted model and native
keys, without repeating migration/startup. It captures actual work ciphertext/history/key references
and a fully authenticated owned safety copy, excludes main ciphertext/keys/foreign safety, preserves
the target ciphertext hash and all identity rows, and remains Authorized. Observed seven objects:
artifacts/native-sqlcipher/d235-native-windows-runtime.log. Native build is clean (17.28s at that check).
The Android fixture uses actual original compiled EF model/migrations inside native SQLCipher and
actual native keys/safety/authentication/read effects, not fake controls or a duplicate app algorithm.
No main application registration or actual catalogue preference publication is introduced here.
Terminal platform/runtime/main/test/package evidence follows below.

Final exact app catalogue transaction and startup retirement reconciliation remain required, with
uncommitted creation recovery, optional recovery-key handling, reference-aware safety retention,
coupled OS policy and remaining native/device/platform acceptance. Main remains plaintext. Unknown
or unauthenticated ownership is refused, not repaired by generating replacement keys. No unchanged
UI/upstream producer suite is repeated; complete release/device acceptance is not claimed.

D-235 observed Android failure: first native fixture stopped before EF schema construction with
its encrypted candidate still zero length. The observation deadline expired, but native PID 15286
remained live and idle; no success was inferred and no restart was made solely for timeout. Source
comparison found the missing original AddZananceData registration before compiled-model Instance,
which sets the documented synchronous initialization switch for Mono (EF issue 31751). The probe
was stopped only after that concrete deadlock diagnosis. The fixture now uses the original data
registration and supports explicit recorded discovery continuation; no replacement profile IDs or
existing generation key preparation occurs in that continuation.

Signed Release AOT/trimmed Android x64/API36/4 KiB continuation of the same run
04eeecb451b84485a6e87b792a6daf3b succeeds. Actual encrypted original model/local portable/owned
safety references are captured, main safety is excluded and finance is not deleted. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-006dbb7b9c374e4188a69417743ac94e.json.
The existing target generation envelope SHA-256 is unchanged before/after continuation; retained
identity metadata was reused. Initial/native resume builds are clean (66.14s / 54.30s). Negative
observation evidence remains in d235-native-android-runtime.log; successful continuation in
 d235-native-android-resume-runtime.log. No unrelated migration/UI/producer acceptance was repeated.

All 3,415 solution cases pass with ContinuousIntegrationBuild, zero failures/skips (3m07.120s).
Strict main Windows (21.22s) and Android (50.57s) builds have zero warnings/errors. Corrected native
Windows scope validation build is clean; actual retained encrypted fixture capture was observed.
Final APK/signature/package evidence follows below. Main encryption/captured app-catalogue retirement
and remaining recovery/retention/OS/platform/device acceptance are still open.

D-235 final complete signed Release APK: artifacts/android-d235-delivery/pro.vafadar.zanance-Signed.apk
(83,507,930 bytes), local test certificate. apksigner verification succeeds; package
pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64 embedded assembly stores are verified.
Test outputs were cleaned after terminal checks. The original native model-init deadlock and
failed observation are retained as negative evidence. Final app retirement/startup/recovery,
retention/OS and main encryption registration remain unfinished; phone/iOS/store acceptance is not implied.

### D-236 — Real catalogue retirement composition and startup continuation

Protected profile DeleteReviewedAsync now enters the application's actual runtime exclusive boundary
and drained database owner before invoking an explicitly supplied native retirement host. Without
that host, the permanent legacy boundary still refuses protected deletion. Plaintext deletion keeps
its existing behavior. The host captures the actual stable identity, reviewed inactive target and
exact before/removed device catalogue state, discovers authenticated native references, publishes
the actual atomic profile packet, requires ISettingsPersistence and unchanged readback, then removes
captured files and retires only the exact identity through the existing coordinator.

Catalogue authority stores SHA-256 fingerprints of complete preference inputs, not profile labels
or plaintext replacement packets. The target removal packet is deterministic for its retained
operation ID, preserves every other profile/name and current selection, and rechecks device choice
projection before Set. Request identity and catalogue digests are committed atomically in the staged
initial metadata transaction. A failed native preference persist retains the exact removal for
idempotent persistence retry; no file/identity cleanup follows until its actual barrier is recorded.
An unrelated catalogue change refuses continuation instead of overwriting that state.

Before complete startup profile capture, an explicitly supplied retirement host resumes pending
requests under the same drained owner. Missing inventory can be completed only while the exact
original catalogue still matches an Authorized request; it cannot be inferred as empty after
publication. Repeated Delete before publication finds the same pending request rather than creating
conflicting operations. Completed records are synchronized/read but do not replay historical
catalogue changes. Final completion notifies once for an actual resumed/deleted operation. Native
file/identity/reference checks remain exact; main/current profiles are refused.

Four actual non-UI app-source scenarios use real isolated SQLite catalogue/native Windows key/file
effects: normal inactive retirement, failed preference persistence followed by exact pending resume,
unrelated catalogue mutation refusal and missing-inventory retry without a second operation.
Settings persistence in those contract tests is an explicit effect, not native platform acceptance.
A real SQLite staged-publication test proves initial identity+digest atomicity and immutable retry.
Existing 23 ProfileTests pass. Public RequireDrainedOwner exposes the actual context-disposal guard
instead of weakening host checks. App tests explicitly compile the new production profile sources.
Terminal native/main/test/package evidence follows below.

The independent Android caller uses the actual ProfileService, ProtectedProfileRetirementHost,
protected session, original compiled model, native generation keys/files, MauiSettingsStore and
AndroidSettingsPersistence. A synthetic failure follows actual durable catalogue persistence so a
cold process must reconcile before session publication. It verifies main balance remains 250 and
only main identity remains; no fake MAUI control or copied product algorithm supplies this result.
Main registration remains plaintext until final key/recovery/retention/OS/provider adoption is ready.
Uncommitted creation reconciliation and other recovery/platform/device/release boundaries remain
open; this does not declare SEC or the whole planned scope complete. Unchanged UI/upstream producer
matrices are not repeated.

D-236 terminal evidence: all 3420 tests pass, zero failures/skips (3m12.507s). The strict
Windows build has zero warnings/errors (21.79s). The strict independent Android build
has zero warnings/errors (52.70s). Actual native Android create/cold continuation completed
on the same fictitious run 60623842398d490ba6f67fe015ffcb23: durable catalogue persistence
followed by injected failure retained target files; cold startup completed retirement,
kept main balance 250 and left only main identity. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-850fb67ec6bf4eca98e834b6aee50a25.json.
No unchanged UI or upstream native producer suite was repeated. Main encryption remains inactive.

The strict main Android build also completed with zero warnings/errors (39.69s).

D-236 complete signed Release APK:
artifacts/android-d236-delivery/pro.vafadar.zanance-Signed.apk (83,114,544 bytes).
Signature verification succeeds; package pro.vafadar.zanance, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores are verified. The script requires a warning-free strict Release publish.
Test outputs were cleaned after all terminal builds/checks. Local test signing and independent
runtime evidence do not imply main encryption, physical phone/iOS, store or product acceptance.

### D-237 — Retain explicit profile creation before native preparation

An explicitly supplied ProtectedProfileCreationHost records a bounded device-only Add request and
requires actual native preference persistence before any identity/key/database preparation. The
request retains the proposed canonical profile id, name, deterministic atomic catalogue packet,
complete before/after preference fingerprints and original private catalogue path. Profile labels
follow the existing device preference privacy, not an unencrypted financial SQLite journal; this
key is not portable and contains no credentials, key bytes, balances or transaction data.

Preparation resumes the same target through the actual native callback. Missing/corrupt intent,
unrelated catalogue changes and changed choice projection refuse preparation/publication rather
than replacing history or generating a second original. Catalogue publication follows verified
preparation and is durably persisted before a completion marker. A retained completed marker only
requalifies/cleans that device request, so later legitimate catalogue changes are never replayed.
Covered startup resumes creation before capturing all profiles and before session publication.
The current financial endpoint and main profile remain unchanged throughout Add.

Cancellation or a native failure after durable Add intent retains that explicit request for exact
startup continuation. The host does not delete unknown old orphan artifacts, infer Add from missing
state or activate main encryption. Historical pre-intent orphans still require separate verified
reconciliation; final recovery, retention, coupled OS exclusion and platform/provider adoption remain
in the full scope. Existing hosts without this composition retain their current creation behavior.

Six application-source contract cases cover persistence interruption before preparation, after
catalogue publication, after completion, unrelated catalogue mutation, corrupt intent, and a native write that restores the original catalogue. Completion requires exact
published-state readback, not merely either allowed pre/post state. Native
settings effects are explicit ports in those cases, not platform acceptance. Actual Android native
continuation and terminal build/test/APK evidence are recorded separately below. Unchanged UI,
upstream native producer suites and unrelated device checks are not repeated.

D-237 verification to date: the complete solution run passes 3425 tests, zero failures/skips
(2m59.021s). After the final publication-readback guard, all six affected creation cases pass
(3.053s), adding one scenario for 3426 covered behaviors. The final Windows build has zero
warnings/errors (15.94s), main Android zero warnings/errors (42.98s). Independent Android
Release native builds were repeated only after relevant source/test-caller changes, not producer
or UI changes. The final caller uses actual ProtectedDatabaseStartup rather than manually duplicating
its ordering. Terminal actual startup/runtime/package evidence follows below.

D-237 final native startup evidence: independent strict Android Release AOT/trim build has zero
warnings/errors (53.31s). The actual ProfileService/native prepare path retained durable Add intent
when interrupted after encrypted schema/key preparation. Cold actual ProtectedDatabaseStartup
resumed that exact request before all-profile bootstrap and session publication; its completed
callback did not repeat resolution. Exactly two identities remain, new profile has no accounts,
main balance remains 250 and a read-only key port forbids replacement preparation on cold resume.
Same fictitious run: 943a0e349933421c8ed9693d3e0610fb. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-b60a776f761a4fabab8c6c2e93aa6bba.json.
Earlier direct-host proofs are retained but do not substitute for this actual startup caller.

Complete signed Release APK: artifacts/android-d237-delivery/pro.vafadar.zanance-Signed.apk
(83,577,562 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. Native
runtime proof uses actual device preferences/persistence and fictitious encrypted finance in an
independent package; main application storage is still plaintext. This does not establish physical
phone/iOS, optional key recovery, historical orphan reconciliation, retention/OS or release acceptance.

### D-238 — Standard optional recovery wrapping and encrypted private vault

DatabaseRecoveryMaterial uses platform RSA-3072 with standard OAEP-SHA256. Each payload contains
its recovery id and the existing canonical DatabaseKeyBinding context plus the exact 32-byte DEK.
Public identity can wrap new generations after private material is disposed; private material is
needed only for explicit recovery. No application cipher, KDF or source fork is introduced.
Stored ciphertext has standard DEK-keyed HMAC-SHA256 confirmation over recovery id/context/ciphertext,
so the current native DEK can requalify an immutable existing mapping without the recovery secret.
Unwrap verifies both context and confirmation before returning an owned key. Inputs/exports/returned
keys have explicit ownership; owned private/key payload buffers are cleared and private RSA owners
are disposed. Managed connection-password strings cannot be completely erased.

SqlCipherRecoveryVault stores private PKCS#8 only inside official native SQLCipher keyed by a
caller-owned random 256-bit recovery key. It requires the reviewed cipher version, exact schema/
installation/recovery/public identity, bounded typed rows, keyed schema access, cipher integrity
and SQLite integrity. A staged no-replace metadata publication synchronizes and reopens exact
private material before final acceptance. Wrong/missing keys, plaintext, foreign or damaged vaults
fail without fallback or replacement. The recovery key itself is never persisted or logged.

SqliteRecoveryEnvelopeCatalogue contains only public identity, RSA ciphertext and HMAC confirmation,
with exact typed schema/identity and bounded immutable per-profile/generation mappings. Existing
capture authenticates ciphertext using the actual native DEK and requalifies native synchronization
after a visible commit/flush error. It refuses changed keys/ciphertext rather than overwriting them.
RecoveryGenerationKeyStore wraps every explicitly prepared native key before caller readiness
advances, while ordinary reads still require device keys. It never silently invokes recovery.
This applies to future database/safety generations when the final host supplies the decorator.

Thirteen focused cases verify standard encryption randomness, wrong profile/install/generation/
private identity, corruption/truncation/disposal, weak/noncanonical public keys, immutable SQLite
mapping/ciphertext/HMAC, failed synchronization without a replacement native key, absent authority
and missing/invalid vault keys. The full solution run passes 3438 behaviors before the final added
confirmation-tamper case; final affected suite passes 13 for 3439 covered behaviors. No financial
EF schema changed; operational recovery metadata does not alter the compiled model/migrations.

Independent native callers use the actual 32-migration model, native device envelope store and
SQLCipher current-generation owner. They preserve a negative fictitious opening amount through
cold recovery after deleting only the independently owned device envelope. A fixed synthetic key
exists only in these independent callers, never the product, a file or output. This tests recovery
material access, not a finished product recovery UI or durable device-key re-enrollment.

Native Windows actual recovery succeeds with unchanged financial ciphertext; wrong recovery key
and damaged private-vault copy fail authentication. Expected native HMAC failure diagnostics contain
page metadata, not keys. Android initially returned ENOENT after the intended fake-envelope removal;
the shared check failed to recognize that platform exception. Its explicit Android callback now
accepts only native ENOENT, not arbitrary exceptions. The original fixture is retained and resumed;
no replacement creation/key/schema is used to hide the failure. Terminal Android/build/APK evidence
follows below. An intermediate namespace qualification build error is retained separately.

Main storage remains plaintext. Final optional recovery enable/confirm UI, durable public/private
registration, native re-enrollment after actual wrapping-key loss, complete retained-reference/retirement
integration, key rotation, coupled OS exclusion and platform/device acceptance remain unfinished.
The vault/protocol is not registered by default; no PIN/device-authentication recovery is bypassed.
Zanance backup recovery and optional portable passwords remain unchanged. Owner OD-04/OD-10 stay
approved and do not require another decision. Unchanged UI and native producer suites are not repeated.

Primary API references: https://www.zetetic.net/sqlcipher/sqlcipher-api/ and
https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsaencryptionpadding.oaepsha256?view=net-10.0.

D-238 terminal evidence: independent strict Windows/Android callers have zero warnings/errors.
Main strict Windows completes in 19.94s, Android in 65.11s, both zero warnings/errors. Full
3438-test run has zero failures/skips (3m31.146s); final 13 affected tests pass (3.295s), including
the added confirmation-tamper scenario, for 3439 covered behaviors. The Android corrected caller
build completes in 62.11s with zero warnings/errors; the original namespace/error recognition
failures are retained, not counted as acceptance.

Windows cold reopen reuses the originally prepared fixture with its missing device envelope and
verifies both wrong recovery key and damaged private-vault copy refusal, actual signed money and
unchanged financial ciphertext. Android resumes the exact failed ENOENT fixture
b8c6121b68604d0692ba13a6a75afb5c without creation or key preparation. Its proof intentionally records
Created=null, not an invented successful initial phase. Cold existing-vault recovery succeeds,
wrong key/damaged vault fail, actual 32-migration finance and -321 opening balance remain, full
financial ciphertext hash is unchanged and no replacement device envelope is created. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-cfd01fd1aab141d9a6b3668efe90ed75.json.
Original expected-native-missing negative stack: d238-android-missing-native-error.log. Windows
final negative/positive log: d238-native-windows-final-reopen.log. Main encryption/recovery UX,
native re-enrollment after wrapping-key loss and full retention/retirement/OS adoption remain open.

D-238 complete signed Release APK: artifacts/android-d238-delivery/pro.vafadar.zanance-Signed.apk
(83,167,792 bytes). Signature verification passes; pro.vafadar.zanance package, API 24/36 and both
arm64-v8a/x86_64 embedded assembly stores are verified. Test output was cleaned after all terminal
checks. This APK does not activate the recovery host or main database encryption and uses local
test signing, not store release acceptance.

### D-239 — Retire captured per-profile recovery mappings before native key removal

ProtectedProfileRetirementHost accepts an explicitly configured recovery catalogue. While Authorized,
it authenticates each target recovery mapping with the actual retained native DEK and records a bounded
immutable recovery-id/count/SHA-256 reference fingerprint in the existing retirement journal. This
contains no private material or plaintext key. Strict journal validation permits only the exact new
optional schema; unknown tables, malformed/missing captures and another recovery identity fail.
Retries requalify an existing Authorized capture before publishing catalogue removal. Missing recovery
capture cannot be inferred as empty after publication, and a host lacking recorded recovery effects
cannot continue a recovery-bearing request.

After actual catalogue persistence, the native host removes only the captured target profile mappings
before deleting its captured device envelopes/files. The recovery transaction rechecks target bytes,
refuses new/changed target generations, preserves every other profile's complete ciphertext/confirmation
rows, synchronizes and reopens actual removal. A delete visible after failed flush can requalify under
the same retained request without replacing keys or touching other profiles. Empty captures remain
explicit authority. Artifact and identity retirement both require actual recovery target absence and
the original recovery identity; the shared encrypted private vault is retained for other profiles.

Five actual SQLite/recovery cases cover exact target removal with other-profile recovery, failure
after delete followed by native requalification, added target generation refusal, wrong native key
capture refusal, and missing/wrong-phase/wrong-request cleanup refusal. One actual ProfileService
flow uses native Windows DPAPI/files and proves target recovery removal before identity retirement.
Existing four profile-retirement application cases still pass. Full/native/build/APK evidence follows.
No financial model or EF migration changed. Main registration remains plaintext; final recovery UX,
native re-enrollment/rotation, broader retained-reference/retention policy and coupled OS/platform
adoption remain in the full scope. Unchanged upstream native suites/UI are not repeated.

D-239 terminal test/native evidence: all 3445 solution tests pass, zero failures/skips
(2m48.472s). The separate strict Android Release AOT/trim build has zero warnings/errors
(55.62s). Actual ProfileService/native settings interruption followed by cold retirement succeeds
on the same fictitious run d367dc82b37c4c1299e987a33e379cfb. Target recovery mappings and identity
are retired, main balance remains 250, and the retained shared encrypted vault still recovers
main's exact key. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-5316e6f94cdc45a8b1af58468e1dd032.json.
This confirms actual Android catalogue/key/file/recovery composition, not product recovery UI,
physical phone/iOS or main encryption activation. Final main build/package evidence follows below.

D-239 main strict builds: Windows zero warnings/errors in 18.87s, Android zero warnings/errors
in 39.16s. No changed screen or upstream native producer required another layout/producer run.

D-239 complete signed Release APK: artifacts/android-d239-delivery/pro.vafadar.zanance-Signed.apk
(83,639,002 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. Local test
signing and independent recovery retirement proof do not activate main encryption or constitute
physical phone/iOS, recovery UX or store/release acceptance.

### D-240 — Explicit native re-enrollment of verified recovered keys

IDatabaseGenerationKeyRecovery requires an owned 32-byte recovered DEK and an explicit host callback
that verifies the same existing financial generation before any key entry is published. The host
remains responsible for app/device authorization and drained access. Native callers exercise actual
SqlCipherNewProfile/current-generation verification with the exact owned candidate key, not an
invented schema or a permissive financial check.

Windows stores CurrentUser DPAPI recovery material in a separate immutable .key.recovered entry;
Android stores a version-2 recovery envelope under a fresh non-exportable Keystore alias, with the
version/purpose/wrapping identity and existing DatabaseKeyBinding context authenticated by native
AES-GCM. Old aliases and original envelopes remain untouched, including unavailable originals.
The separate entry is staged, authenticated against the verified DEK, synchronized, renamed without
replacement and reopened before acceptance. An existing entry must match, with no replacement; a
staged entry can only resume through explicit recovery. Owned key copies are cleared. Reads choose
an existing recovery entry, fail on its corruption and never silently fall back or invoke recovery.
Ordinary preparation cannot regenerate a binding with a pending recovered stage.

Four native Windows contract cases verify original-envelope preservation and cold read, no writes
before failed finance verification, refusal of a different replacement key, and damaged recovery
entry refusal without fallback. The actual independent original-model caller runs create/key-loss,
verified import and a separate native cold read that does not use the recovery secret, preserving
signed finance and complete ciphertext. Android uses the same financial verification with actual
Keystore/fsync effects; terminal evidence follows below.

This is native re-enrollment, not a complete product recovery UI or authorization controller. Final
all-profile recovery orchestration, interrupted malformed-stage reconciliation, wrapping-alias
lifecycle for future new generations after invalidation, recovered-entry reference/retirement and
retention integration remain open. Strict reference discovery still refuses unqualified recovered
namespace artifacts before catalogue removal, rather than guessing ownership of an unavailable
original. Main registration stays plaintext; no temporary OS-backup policy, new permission or SDK.
Unchanged UI/native producer suites are not repeated. OD-04/OD-10 remain approved.

D-240 terminal native/test evidence: all 3449 solution tests pass, zero failures/skips
(2m54.750s). Final independent Android strict Release AOT/trim build has zero warnings/errors
(62.01s). Three native Windows processes create, import and reopen without using the recovery
secret; signed money and complete ciphertext remain unchanged. Three actual Android processes
pass the same flow using the fresh version-2 Keystore recovery envelope on fictitious run
7c24641d9fac425381bce1af3621ce25. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-2817166d0a484a5bab73f02ef924cca1.json.
Four Windows native contract cases also pass. The initial strict test compile exposed platform
annotations across assertion lambdas; the assertions now call the platform-neutral ports after
native Windows construction is explicitly guarded, without disabling diagnostics. Main Windows
strict build is zero warnings/errors (17.70s); final main Android/package evidence follows.
No full app recovery UI, alias invalidation matrix, hardware power-loss/device/iOS or main
registration acceptance is claimed by these native controlled tests.

D-240 main Android strict build completes with zero warnings/errors in 39.62s.

D-240 complete signed Release APK: artifacts/android-d240-delivery/pro.vafadar.zanance-Signed.apk
(83,216,944 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. The APK
uses local test signing and does not activate main encryption/recovery UI or establish phone/iOS/
hardware power-loss/store acceptance. Recovered-entry reference/lifecycle integration remains open.

### D-241 — Authenticated native recovered-key references and retirement

KeyRecoveryReceipt records exact original/recovered native file identity, root identity, length and
SHA-256 under standard DEK-keyed HMAC-SHA256 and the existing profile/install/generation context.
It contains no plaintext key. The original object may explicitly be absent; missing receipt is never
inferred as empty authority. Same-content file/root replacement and changed bytes invalidate the
confirmation. A staged recovered object is normalized only to its final bound filename, retaining
its actual inode/file identity through no-replace rename.

Native import now publishes/resumes an immutable bounded receipt after actual finance verification
and recovered-envelope authentication, before final recovered-key publication. Native private file
creation and file/directory synchronization qualify the receipt. Existing and cold recovered reads
require exact authenticated receipt/current objects, without fallback or manufactured confirmation.
Explicit re-import can requalify the existing exact verified objects; ordinary reads never create
authority. Windows uses real owner-only native files; Android uses actual app-private no-follow
capture/fsync and the existing fresh wrapping alias. Original unavailable envelopes remain immutable.

ProfileRetirementReferenceDiscovery recognizes only exact recovered entry/receipt names, verifies
receipt HMAC and exact native originals/recovered objects with the actual DEK, then captures each
file for the existing native removal algorithm. Unknown or uncommitted staged recovery namespaces
still fail before catalogue publication. After catalogue removal the existing host removes captured
public recovery mappings, all original/recovered/receipt objects and finally the exact identity.
Other profiles and their shared private vault remain. RecoveryGenerationKeyStore forwards explicit
native import only after the same finance verification, then requalifies recovery mapping before
native publication; no public recovery write follows rejected finance verification.

Two new native Windows cases prove same-content original replacement refusal and missing receipt
refusal without fallback. Actual ProfileService source additionally discovers/removes original,
recovered entry and receipt alongside target recovery mappings. Existing native recovery/profile
cases pass. Independent actual Android finance/import/cold retirement evidence follows below.
No financial model/migration or visible UI changed. Main registration stays plaintext; final recovery
UX/all-profile orchestration, alias invalidation/future generation lifecycle, malformed-stage repair,
retention/OS/provider/platform acceptance remain in the full scope. No temporary OS policy or new SDK.

D-241 terminal verification: all 3452 solution tests pass, zero failures/skips (2m54.985s).
Six native Windows key/receipt cases and six actual app retirement cases pass. Final independent
Android strict Release AOT/trim build has zero warnings/errors (70.21s). The actual original
ProfileService/native recovery decorator imports the work profile's DEK after existing encrypted
finance verification, retains original/recovered/receipt objects during injected native preference
failure, and completes their captured cold retirement on the same fictitious run
40526ade78094cf3bcda86a7983ad21e. Main balance 250, its native identity and actual recovery through
the shared private vault remain. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-e36f0b4cd43349f4b243acfeca472b6a.json.
The first compile exposed a local name collision; final source uses a distinct original-key reference.
Unchanged producer suites/UI were not repeated. Main/recovery UX/all-profile/alias lifecycle/
retention/OS/platform gates remain open. Final main build/package evidence follows below.

D-241 strict main builds complete with zero warnings/errors: Windows 17.76s, Android 39.04s.

D-241 complete signed Release APK: artifacts/android-d241-delivery/pro.vafadar.zanance-Signed.apk
(83,671,770 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs cleaned after all terminal checks. The local
signed APK does not activate main encryption/recovery UI or establish phone/iOS/store acceptance.

### D-242 — Preflight complete captured-profile recovery before native writes

ProtectedProfileRecovery captures the actual original profile catalogue independently of commercial
selection and requires the unpublished covered startup endpoint plus its real drained database owner.
Stable native identities must match every captured legacy profile and the existing recovery
installation. All canonical recovery bindings are read from strict bounded metadata; unknown profiles
and a missing original-profile mapping fail without inferring a fresh database or empty authority.

An explicit native unlock port opens the existing private vault with the caller-owned recovery key.
Each DEK is unwrapped/confirmed and passed to the host's complete actual reference verifier before
the first native import. A failed profile preflight therefore writes no native recovered entry.
All retained key buffers are bounded by the existing 4096-binding inventory and cleared in finally;
private material is disposed. Before/within each import and after the batch, catalogue, identities,
bindings and unpublished/drained access are rechecked. Later legitimate profile edits are preserved,
not overwritten. Native imports remain immutable and verify their owned copies against existing
references. Recovery does not publish a session, modify profiles/preferences or complete financial
operations; only normal protected startup runs after the verified native batch.

Four actual application-source cases with real isolated SQLite/RSA metadata prove full-profile
preflight before first write, second-profile failure with zero imports, missing mapping refusal before
unlock, and catalogue mutation refusal without name overwrite. Native unlock/reference/import effects
are explicit ports in these cases, not fake MAUI controls or copied product algorithms.

The independent Android caller uses actual native preferences/persistence, two encrypted original
32-migration profiles, automatic/public recovery capture and actual ProtectedProfileRecovery. It
removes only its fictitious native envelope files, verifies/imports all original profile keys and then
runs actual ProtectedDatabaseStartup before real switching/financial reads. Terminal evidence follows.
Initial strict fixture compilation exposed startup-call ordering before local declarations; the
final caller invokes startup only after the owned recovery lease is released.

Main registration remains plaintext. This composes captured recovery and startup prerequisites but
does not finish product recovery/authentication UI, real alias invalidation/hardware acceptance,
complete concrete retained safety/preference/abandoned-reference verification, retained-file policy,
OS exclusion or platform/iOS/provider acceptance. The final host must verify every supplied referenced
binding; the fixture has current initial generations only and does not prove a full retention corpus.
No schema/model/permission/export/new SDK changes; unchanged UI/native producer suites are not repeated.

D-242 terminal evidence: all 3456 solution tests pass, zero failures/skips (2m55.483s), including
four actual application recovery cases. Final independent Android strict Release AOT/trim build
has zero warnings/errors (81.99s). The same fictitious two-profile fixture
4535123ccf8f4f2c8db30f79f4ea1f87 removes both native envelope files and, in a cold process, runs
actual ProtectedProfileRecovery/native vault/import plus actual ProtectedDatabaseStartup and
real switching. Original main/work balances remain 250/880 and no extra profile is created.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-b977a43500c74666b1e93a0e3afe068b.json.
The fixture proves current initial native generations; full retained safety/preferences/abandoned
reference and real alias-loss corpus remain separate unfinished host acceptance. Final Windows
strict build has zero warnings/errors (19.42s). Main encryption/recovery UI remain inactive;
final Android/package evidence follows below. Unchanged native producer/UI checks were not repeated.

D-242 main strict Android build has zero warnings/errors (38.31s).

D-242 delivery APK: artifacts/android-d242-delivery/pro.vafadar.zanance-Signed.apk, 83,257,904 bytes. Signature verification passed; package pro.vafadar.zanance, API 24/36, arm64-v8a/x86_64 and both complete assembly stores were checked. Local test signing is not store or physical-device acceptance. Test output cleanup passed with zero warnings/errors. Main protection remains inactive.

### D-243 — Recover after actual independent Android wrapping-alias loss

The independent Android native caller now has a WrappingKeyLoss scenario. It removes only the
Keystore alias for its freshly created fictitious installation in the distinct compatibility
package. It preserves both original encrypted key envelopes and requires actual native reads
of both bound DEKs to fail. It changes no global/device settings or product security data.

A cold process then runs the actual ProtectedProfileRecovery and protected startup compositions
from D-242, including native recovered-envelope/receipt publication and real profile switching.
Both original envelope SHA-256 values remain unchanged, the unavailable installation alias is
not recreated, no extra profile appears, and main/work balances remain 250/880. This proves actual
alias absence rather than merely deleted envelope files. It does not establish hardware-backed
invalidation, biometric change behavior, future-generation alias lifecycle or physical-device acceptance.

Verified independent strict Release AOT/trim build: zero warnings/errors, 55.90s. Actual two-phase
runtime proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-679ead1e4b7f46e699024d045d8a8a6d.json,
run c12bdc1cb2d04abf88b7f9cf93593c31. Script parsing and diff checks passed. No application source,
provider registration, UI, permission, financial schema or APK changed. Therefore unrelated solution
suites, main builds, UI matrices and native producer suites were not repeated. The complete signed
D-242 delivery APK remains the current product package; main storage remains plaintext. Final
recovery/authentication UI, complete retained-reference verification, lifecycle/retention, coupled
OS backup exclusion and platform/provider/release acceptance remain open.

### D-244 — Independent Android wrapping aliases for new generations

New native generation preparation no longer recreates the installation wrapping alias. Every new
DEK is wrapped by a fresh non-exportable Android Keystore AES-256-GCM alias. The existing version-2
header authenticates purpose 0 for ordinary generations and purpose 1 for explicit receipt-bound
recovery, along with the complete profile/installation/generation context. Reads accept immutable
legacy version-1 entries and new purpose-0 entries; recovered reads still require purpose 1 and their
native receipt. Ordinary and recovered entries cannot substitute for one another. Bounded native
reads accept only exact authenticated formats up to 82 bytes, not a truncated legacy packet.

The independent caller deliberately prepares legacy installation-bound envelopes for both fictitious
profiles, removes their actual installation alias, verifies refused original reads, recovers through
the actual application/native startup, then creates a new independent generation without recreating
the old alias. A third cold process reads that generation using existing native storage only and
reopens a SQLCipher packet containing Int64.MinValue; recovery-purpose confusion is refused.
This is lifecycle creation/reopen evidence, not final reference-aware alias retirement or hardware acceptance.

The initial fixture compile exposed local-name collisions; final strict native Release AOT/trim build
passed with zero warnings/errors (64.51s). The first runtime observation of the third process expired.
The same process subsequently returned successful completion, read without restart or regeneration:
artifacts/native-sqlcipher/d244-same-process-future-proof.json. Negative observation evidence remains
in d244-alias-runtime.log. Earlier phases passed the runner's actual assertions before its third
launch; no synthesized Created/Reopened report is supplied. The runner now makes one final read of
the same completion target before a non-barrier deadline failure; it never restarts to hide expiry.
Script parsing passed. All 3456 solution tests passed, zero failures/skips (2m53.266s).

Main protection registration remains inactive. No UI/schema/account/portable-password/permission
change. Complete retained-reference validation, recovery/authentication UI, malformed staging,
reference-aware key/alias retention, final provider/OS adoption and physical platform/iOS/provider
acceptance remain open. Unchanged UI and upstream native producer suites were not repeated.

D-244 strict main builds passed with zero warnings/errors: Windows 17.10s and Android 38.62s.

D-244 complete signed Release APK: artifacts/android-d244-delivery/pro.vafadar.zanance-Signed.apk, 83,696,346 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both embedded assembly stores verified. Solution test outputs cleaned successfully after terminal checks. Local test signing is not phone/iOS/store acceptance; main protection registration stays inactive.

### D-245 — Authenticate retained native references before recovery import

ProfileRecoveryReferenceVerifier supplies the concrete read-only reference check for captured native
recovery. It reads actual stable identities and strict restore ancestry, rejects unknown database
namespace entries and missing selected candidates, and requires selected restored journals plus
retained before-restore/preference references. Existing candidate, initial rollback, before-restore
and preference databases are reopened with the owned recovered DEK, reviewed cipher version,
cipher_integrity_check and full integrity_check. Preference capsules also pass their actual typed,
bound allowlisted ReadAsync validation; no settings replay occurs during key verification.

Matching device safety references require complete AES-GCM authentication, not only an untrusted
header. Foreign headers never cause foreign native key access. Safety-only generations can prove
authenticated retained reference without pretending to be financial database generations. Keys
with no authenticated reference fail. All decoded package/key buffers are cleared; native file/root
identities, lengths, fingerprints, SQLite sidecars, namespace listings, identities and ancestry are
rechecked after validation. Caller-owned drained access and actual private-root effects remain mandatory.

Five native Windows/private-file cases pass: genuine safety-only reference, tampered content refusal,
foreign reference refusal, unknown namespace refusal and missing selected generation refusal. These
cases use actual SQLite metadata and native file capture, not a copied verification algorithm.

The independent signed Release Android caller now uses this verifier in actual ProtectedProfileRecovery.
It creates two original encrypted profiles, two initial encrypted rollbacks, two real typed encrypted
preference capsules and two authenticated safety envelopes with separate captured recovery keys. All
four fixture device envelopes are removed. A cold process verifies every binding before native
imports and then runs actual covered startup/switching. Main/work balances remain 250/880; no extra
profile is created. Native file inventory confirms both capsules/rollbacks and safety references.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-6196395354e94738857ce12275c2ac91.json,
run 66f9c0a865d041b68b450403a753785b; inventory: artifacts/native-sqlcipher/d245-native-reference-inventory.log.
The safety fixture proves authenticated codec references, not ZIP manifest/restore acceptance. Its
preference capsules exercise the existing capsule format on initial candidates; a full multi-restore
history/pending-restoration corpus and staged/orphan reconciliation remain separate acceptance work.

All 3461 solution tests pass with zero failures/skips (2m55.599s). Final independent Android strict
Release AOT/trim build has zero warnings/errors (67.48s); Windows strict main build is clean (17.17s).
No UI/schema/permission or portable backup behavior changed. Main registration remains plaintext.
Final recovery UI/authentication, complete restore/crash corpus, reference-aware generation/alias
retention, final provider/adoption/OS policy and platform/provider/device gates remain open. Unchanged
UI matrices and upstream native producer suites were not repeated.

D-245 strict main Android build passed with zero warnings/errors (39.69s).

D-245 complete signed Release APK: artifacts/android-d245-delivery/pro.vafadar.zanance-Signed.apk, 83,262,000 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned successfully after terminal checks. Local signing is not store/phone/iOS acceptance; main protection remains inactive.

### D-246 — Require committed recovery candidates before history mirroring

Review of the real journal/history interruption boundary found a concrete omission in D-245:
required candidate presence followed only the history Selected flag. Verification/selection can
already have committed in the restore journal when history mirroring has not run. The verifier now
requires an actual candidate whenever the journal reached CandidateVerified or Selected/Completed,
independently of that history flag. An authenticated safety reference cannot replace the required
financial candidate. Existing selected rollback/preference requirements remain unchanged.

Two additional actual SQLite/native-file cases exercise CandidateVerified and Selected while history
remains unselected. Each requires the exact missing-candidate refusal before other reference reads,
without inventing data or changing the journal/history. The Selected fixture's intentionally unread
placeholder files isolate this ordering gate; they are not evidence of ciphertext/financial validation.
Seven focused cases pass. All 3463 solution tests pass, zero failures/skips (2m56.718s).

The independent signed Release AOT/trim Android caller executes the same original verifier with
real native private roots and real strict metadata for both interrupted phases. Both refuse missing
candidates, preserve journal/history and create no replacement data/key. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-363f2a58a6bb4a6a99ee8e25041a3305.json,
run 61aa8a42f371446692208249ebb47aa9. Final strict native build has zero warnings/errors (58.82s).
The independent entry point explicitly selects the reviewed native provider before metadata access.
Script parsing and diff checks pass. Unchanged positive financial/UI/producer matrices were not repeated.

This fixes a real missing-reference boundary; it does not complete the multi-restored financial corpus,
malformed/staged/orphan reconciliation, final recovery UI, reference-aware retention or main provider/
OS adoption. No schema/model/permission/UI change. Main storage registration remains plaintext.
Physical devices, iOS, external providers and release acceptance remain separate gates.

D-246 strict main builds have zero warnings/errors: Windows 17.06s and Android 38.94s.

D-246 complete signed Release APK: artifacts/android-d246-delivery/pro.vafadar.zanance-Signed.apk, 83,704,538 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not store/phone/iOS acceptance; main protection remains inactive.

### D-247 — Cold recovery of a real interrupted two-restore chain

The independent Android caller now creates the actual 32-migration model, writes initial account
balance 250, and restores two complete portable packages through the actual BackupService and
SqlCipherPackageRestoreReceiver. The source databases are in memory; the original exporter creates
bounded portable snapshots without plaintext disk staging. The first package restores balance 730.
The second restores balance 990 and deliberately fails after actual native preference persistence,
while its journal is Selected and history remains pending. All three captured device envelopes are
removed; original/candidate/rollback/preference files and private/public recovery material remain.

A cold process uses actual ProtectedProfileRecovery and ProfileRecoveryReferenceVerifier to preflight
and re-enroll every retained key before opening current storage. Actual OpenCurrentAsync resumes the
selected second restore without the portable package or old current connection, persists/replays the
native preference value and completes the same three-row ancestry. Actual compiled-model reads
confirm candidate balances 250/730/990 and before-restore balances 250/730. All retained ciphertext,
rollback and typed preference capsules pass native authentication/integrity/reference verification.
The source-only backup and unused profile-transition ports do not duplicate application algorithms.
This probe does not implement or bypass production UI/device authentication.

Initial cold recovery correctly refused stale independent-package profile metadata: fixture setup
removed profiles.catalogue rather than the real profiles.work.catalogue. Setup now clears the actual
fictitious catalogue. An explicit recorded-fixture continuation verifies the one-profile identity and
three-row history, refuses non-fictitious extra profiles, and repairs only that independent package's
stale catalogue before actual recovery. Existing generations/keys were not rebuilt. Negative runtime
and continuation evidence remain in d247-native-runtime.log and d247-native-continuation.log.
Continuation proof has Created=null, not invented creation evidence:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-39cbf89ea73f4a21b71d998352fecd12.json,
run 5e99e9f6a10944f0a286301b37d86733.

After the fixture correction, one fresh complete two-process scenario passes. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-89a5cc941e1842debb53350fb37f12d9.json.
Final independent strict Release AOT/trim build has zero warnings/errors (54.69s). Script parsing and
diff checks pass. Product source, financial model, UI, permissions, native producer binaries and main
APK are unchanged from D-246. Therefore unchanged 3463-test baseline, main builds/UI matrices and
producer suites were not repeated. The complete verified signed D-246 product APK remains current.

This verifies the real multi-restore/pending-replay recovery corpus. It does not complete malformed
staging/orphan reconciliation, generation/key/alias retention, final recovery/authentication UI,
main provider/OS adoption, physical-device/iOS/provider acceptance or publication. Main remains plaintext.

### D-248 — Match Android key reads to native file ownership requirements

AndroidDatabaseKeyEnvelopeStore now rejects non-regular, multiply linked and foreign-UID file objects
through the actual opened descriptor's fstat before reading/unwrapping. This matches the existing
native retirement/reference boundary instead of accepting a key object that cannot safely qualify for
later cleanup. No key format, crypto algorithm, profile data, native producer binary or UI changed.

The independent original key-store caller verifies creation, cold encrypted reopen and malformed/
missing/directory/symlink/no-overwrite/staged-file cases. Its additional hard-link case reports whether
the read-refusal branch was actually exercised. On this emulator, native link returned EACCES; the
initial negative phase stopped. The fixture now recognizes only EPERM/EACCES as explicit OS link
creation restrictions, not successful coverage of the link-count guard. Its first pattern form used
nonconstant Android errno properties and failed compilation; ordinary equality fixed that harness
compile issue. Both negative runtime/compile logs are retained.

The same fictitious key run fcf6548964f64553a8a8ba41af5a4979 resumes only the negative phase with the
existing key/encrypted fixture; no fresh DEK or creation/reopen replay. Proof:
artifacts/native-sqlcipher/build/android-d/android-key-proof-fcf6548964f64553a8a8ba41af5a4979.json.
HardLinkReadRefusalExercised=false is explicit. The original positive phases passed before the
OS restriction; resumed proof contains the negative phase only and does not invent those results.
Foreign-UID and hard-link read rejection still need an environment capable of creating such objects.
Final independent strict Release AOT/trim build is clean (57.56s). Harness parsing and diff checks pass.
All 3463 solution tests pass, zero failures/skips (2m54.568s); strict Windows main build is clean (23.24s).

Main registration remains plaintext. This read-boundary alignment does not finish malformed-stage/
orphan cleanup, reference-aware file/key/alias retention, final recovery UI/provider/OS adoption or
physical-device/iOS/provider/release acceptance. No unchanged UI or upstream producer suite repeated.

D-248 strict main Android build has zero warnings/errors (40.62s).

D-248 complete signed Release APK: artifacts/android-d248-delivery/pro.vafadar.zanance-Signed.apk, 83,266,096 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-249 — Continue verified recovery past an interrupted receipt stage

Explicit native recovery verifies the owned DEK against retained finance before receipt preparation.
When an existing uncommitted receipt stage has different bounded bytes, KeyRecoveryReceiptStore now
preserves it and creates a separate unique no-replace receipt stage. The new stage is read back,
synchronized and renamed without replacing final authority. Published receipts still require the
same exact native object/MAC and are never overwritten. Correct legacy stages still resume normally.
This does not infer ownership of or delete malformed objects, repair oversized/unsafe files or replace
a published receipt. Cipher-key staging itself remains under its existing stricter continuation.

One new actual Windows DPAPI/private-file case preserves torn stage bytes, publishes final authority,
cold-reads the same DEK and leaves no new unique receipt stage. Seven focused cases pass. The first
assertion used a Windows wildcard that also matched the retained legacy stage; an exact ordinal
predicate fixed the test, then the required collection assertion was aligned with the strict analyzer.
Original negative test/compile logs are retained; no cryptographic/provider code was changed for them.
All 3464 solution tests pass, zero failures/skips (2m48.082s).

The independent signed Release AOT/trim Android caller puts a three-byte interrupted receipt stage
beside each of its four removed fixture envelopes. Actual all-profile recovery authenticates native
references, imports the same keys, publishes valid receipts and opens original main/work balances
250/880 through covered startup and real switching. All four old stages remain with the SHA-256 of
bytes 1/2/3, verified from the same native private root after success. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-d60c0cd29ac9426d9008a60df6307368.json,
run cf79bce63f8546939f2686cebd92a45a; retained stages: artifacts/native-sqlcipher/d249-retained-stage-proof.log.
Independent strict native build is clean (59.44s); strict Windows main build is clean (21.99s).

Preserved rejected staging still needs reference-aware reconciliation before retirement; this is
functional explicit recovery continuation, not finished cleanup/retention. No schema/model/UI/permission
change. Main protection remains inactive; final recovery UI/provider/OS adoption and platform/device/
provider/release acceptance stay open. Unchanged UI and upstream producer suites were not repeated.

D-249 strict main Android build is clean (39.64s), zero warnings/errors.

D-249 complete signed Release APK: artifacts/android-d249-delivery/pro.vafadar.zanance-Signed.apk, 83,704,538 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-250 — Retire bounded temporary receipts under authenticated whole-profile authority

Native profile reference discovery recognizes only the exact legacy receipt-stage name and the
canonical lowercase nonempty GUID attempt suffix introduced in D-249. These are bounded temporary
receipt metadata, never key or selector authority. A reviewed whole-profile retirement may capture
them only after the actual final recovered key and its original/recovered native-object receipt pass
authentication. Oversized stages, invalid names, missing final recovered authority and staged key
envelopes remain refused. No age-based guess or directory-wide deletion is introduced.

Each permitted stage enters the existing immutable native-object inventory with its root/file ids,
length and full fingerprint. The existing durable catalogue-removal barrier and all-object preflight
still precede exact native deletion. Replaced/changed stages cannot be silently removed. This allows
receipt remnants preserved by successful explicit recovery to leave with their reviewed profile,
without giving those remnants permission to open financial data or authorizing automatic key cleanup.

Three actual application-source/native Windows cases cover removal of bounded legacy/unique stages,
refusal without a published recovered entry and refusal of oversized metadata. Nine focused cases
pass; all 3467 solution tests pass, zero failures/skips (2m53.771s).
The independent signed Release AOT/trim Android caller creates both receipt-stage forms, performs
actual finance-verified key recovery, captures real profile retirement and fails after actual native
catalogue persistence. Cold actual host continuation removes only the captured target artifacts and
recovery/identity mappings. Main balance 250 and its recovery material remain. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-150ac54a41354b7093e18671608572ad.json,
run 21bea656d24e4c618f8fc38f0dc53772. Strict native build is clean (66.92s).

This closes reviewed profile retirement of bounded receipt-only remnants. It does not classify or
remove malformed key stages, unknown artifacts, historic pre-intent orphans or files still referenced
by recovery/safety/history. Final automatic safety/generation/key/alias retention and main recovery/UI/
provider/OS adoption remain open, as do physical-device/iOS/provider/release gates. No schema/model/
permission/UI change; main registration remains plaintext. Unchanged UI/producer suites not repeated.

D-250 strict main builds have zero warnings/errors: Windows 17.78s and Android 91.82s.

D-250 complete signed Release APK: artifacts/android-d250-delivery/pro.vafadar.zanance-Signed.apk, 83,266,096 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-251 — Let persistence hosts select their native SQLite engine

Shared Vafadar.Data now references Microsoft.EntityFrameworkCore.Sqlite.Core instead of the package
that transitively selects a default native engine. The current standard SQLite bundle is explicit in
the Zanance app and the ordinary isolated test host. The Data project's design-time bundle has
PrivateAssets=all, so EF context tooling still runs without leaking its engine into application hosts.
The existing resolved bundle version 2.1.12 is pinned centrally; this is dependency ownership
separation, not an upgrade, new cipher bundle or approval of candidate/native production adoption.

Actual restored dependency graphs show only SQLitePCLRaw.core in shared Data. The app's referenced
Zanance.Data node depends only on Vafadar.Data/Core, not the design-time native bundle. The current
app explicitly retains its original standard engine. This makes final reviewed source-built provider
selection possible at the app boundary instead of importing two default/native choices from shared
persistence. Managed bridge/core compatibility still needs qualification when final cipher adoption
changes the host's packages; isolated source-compilation evidence is not that library binary proof.

All 3467 solution tests pass, zero failures/skips (2m48.982s), including actual isolated SQLite flows.
Actual dotnet ef dbcontext info with the Data project as startup succeeds without MAUI; it reports
the original context/provider and design-time datasource, without schema update or model generation.
Windows/Android strict main builds pass, zero warnings/errors (27.46s/60.67s). Dependency graph,
EF execution and test evidence are retained under artifacts/native-sqlcipher/d251-*.
No schema/model, UI, financial behavior, permission or key lifecycle change. Unchanged native producer
and layout matrices were not repeated. Main still uses standard plaintext SQLite; final host/recovery/
retention/OS adoption and native/platform/provider/device gates remain open.

D-251 complete signed Release APK: artifacts/android-d251-delivery/pro.vafadar.zanance-Signed.apk, 83,692,250 bytes. Signature, pro.vafadar.zanance package, API 24/36 and both complete arm64-v8a/x86_64 assembly stores verified. Both embedded standard libe_sqlite3.so hashes equal D-250 exactly. Test outputs cleaned after terminal checks. No UI change/layout replay or native candidate adoption; local signing is not phone/iOS/store acceptance.

### D-252 — Qualify actual persistence assemblies with the reviewed cipher bridge

Two independent binary hosts now reference the actual Vafadar.Zanance.Data project (and its
Vafadar.Data assembly) instead of compiling infrastructure/model/migration source copies. They
explicitly select SQLitePCLRaw.provider.sqlcipher 3.0.5 with its core 3.0.5 and reviewed native slices.
Restored graphs contain no default bundle/e_sqlite3. Runtime checks require the actual production
assembly names, not merged caller-source types. No friend-assembly grant or production probe reference.

Through public original APIs, each host creates encrypted storage with actual native keys and all
32 migrations, publishes the protected factory, writes a fictitious negative EUR account, exports
through the actual binary Zanance backup source and reopens the bounded portable snapshot in memory.
A fresh process opens the same native storage with a key port forbidding creation. The actual model/
negative money and binary raw snapshot APIs remain correct; a wrong DEK is refused. This qualifies
managed binary compatibility, rather than extrapolating prior same-assembly source proofs.

Windows strict host builds are clean (final shared-entry build 1.09s). Initial refactoring exposed
CA1416 in deferred native callbacks; explicit platform checks fixed the entry point without analyzer
suppression. The old output invoked after that failed build is retained as negative evidence, not
final acceptance. The corrected build and same-fixture reopen passed; the repeatable Windows harness
also passes creation/cold reopen under artifacts/native-sqlcipher/binary-host/40f965990678413cbb09246fe6581cd9.

Android is a distinct pro.vafadar.zanance.binarycompatibility package using the actual .NET assemblies,
original native effects, complete signed Release AOT/trim and two exact manifest-qualified native slices.
The first build is clean (55.90s). The application behavior completed successfully but the harness
could not read its sandbox because the independent manifest lacked debuggable=true. The process
was confirmed live and its exact result observed in package PID output. Only this independent package
was made inspectable/no-OS-backup; no product/device setting changed. Its existing creation result was
read after install-r preserved data, then only the same fixture's cold phase continued. No database
or key regeneration was used to hide this observation failure. Final manifest build is clean (9.99s).
Creation evidence: artifacts/native-sqlcipher/d252-preserved-create-result.json. Cold proof:
artifacts/native-sqlcipher/build/android-d/binary-runtime-proof-39a41a98321941e5aab2cf2016f86afa.json,
run 5f3e7ed4ac08435090c499fd3bfec805; negative observation log remains d252-android-binary-runtime.log.
The driver now refuses an uninspectable sandbox before launch and requires stopped ownership before
cold launch. It validates embedded native hashes and refuses default engine leakage; script parsers pass.

Product source/behavior, schema, UI, permissions and APK remain D-251. The 3467-test baseline, main
builds/UI matrices and unchanged upstream producer suites were not repeated. Current signed product
APK remains artifacts/android-d251-delivery/pro.vafadar.zanance-Signed.apk. This closes this binary
bridge prerequisite on tested Windows x64 and Android x64 API36/4KiB; it does not validate arm64,
API24/16KiB OS, iOS, every key/recovery/crash route or whole-app adoption. Final host/recovery UI,
reference-aware retention/reconciliation, native warning review and coupled OS/platform/device/provider
acceptance remain open. Main registration is still plaintext.

### D-253 — Refuse plaintext contexts before protected startup publication

Protected data registration now accepts protectionRequired from location construction through
AddLocalDatabase and AddZananceData. The immutable original path remains available to maintenance
bootstrap, but ordinary/owned synchronous/asynchronous factory calls refuse before the first verified
protected publication. No directory, plaintext database or backup snapshot is created. Failed calls
release their access leases; plaintext MoveTo and captured plaintext rollback are also refused.
The flag is host authority, never inferred from missing keys/files or offered as a user setting.
Existing standard hosts keep their explicit current registration; main encryption is not activated.

Five new factory/location cases cover early refusal, no-write behavior and subsequent drained ownership.
The actual production-assembly cipher hosts now register protectionRequired and check pre-publication
refusal before their existing encrypted creation/cold reopen, typed money and portable snapshot checks.
Windows native runtime passes at artifacts/native-sqlcipher/binary-host/66f872a9b3bf4a8a83766e8810ee008c.
An initial test expectation used a mixed-separator path; canonical comparison corrected that fixture
assertion. No producer suite, unchanged UI matrix or platform security setting was repeated.

Final host/provider activation, optional recovery UI, reference-aware retention/reconciliation and
coupled OS exclusion remain open. OD-04 and OD-10 are already approved; this slice needs no new owner
decision. Native platform/device/provider/release acceptance remain separate from local verification.

D-253 runtime: strict actual-binary Android Release build has zero warnings/errors (1m1.63s).
Both fresh creation and cold same-key reopen pass with pre-publication refusal enforced:
artifacts/native-sqlcipher/build/android-d/binary-runtime-proof-a97d94f80a684d3f85d7117741ecf846.json,
run 0eb40c84818342eaa7492b409fe889b8. All 3472 solution tests pass with zero failures/skips
(2m56.792s), including the five added boundary cases. Existing main registration remains plaintext.

D-253 delivery: strict main Windows build 26.89s and Android build 40.34s have zero warnings/errors.
Complete signed Release APK: artifacts/android-d253-delivery/pro.vafadar.zanance-Signed.apk,
83,249,712 bytes; signature, pro.vafadar.zanance package, API24/36 and both embedded assembly stores
verified. Test output cleanup completed successfully after terminal verification. Main provider remains
standard SQLite; this APK is not a whole-app encryption, physical-device, iOS or release acceptance.

### D-254 — Compose the protected application profile lifecycle

ProtectedProfileStorage now owns one SqlCipherProfileSession for covered all-profile startup,
current-generation resolution, switching and reviewed new-profile preparation. Construction is lazy
and requires the D-253 protected registration boundary; it performs no database/key/profile work.
The host supplies anchored native directory/key/durability effects and an owner-bound restore receiver.
ProtectedDatabaseStartup passes its actual drained lease to that receiver during cold bootstrap.
No obsolete process callback or missing metadata can choose an installation/restore generation.

The resolver reads actual identities/history and validates the retained profile-to-original mapping.
Its key owner's validation outlives the maintenance lease and permits legitimate later catalogue
Add/rename while refusing removal/redirection of that profile. New preparation requires the actual
bounded reviewed Add intent and canonical matching id/path before any identity/key effect. Existing
published-but-unfinished creation can resume its same identity. An independent unpublished protected
location runs the real compiled model/migrations in memory without moving the active endpoint.

Four original application-source cases cover lazy construction/plaintext-registration refusal,
missing reviewed intent and mismatched id/path with unchanged intent/metadata. The native Android
creation-continuation case now uses this actual host for preparation, cold startup and switching,
instead of a fixture-owned resolver/schema implementation. Native production activation and the
final receiver/recovery UI/retention/OS/platform acceptance remain unfinished; main remains plaintext.
No unchanged producer suite or visible UI matrix was repeated.

D-254 terminal baseline: all 3476 solution tests pass, zero failures/skips (3m4.576s). Strict main
Windows 23.21s and Android 41.19s have zero warnings/errors. Signed Release AOT Android composition
passes interrupted preparation, cold reviewed creation completion, exactly two identities, empty
new profile, main balance 250 and switching back through the shared host. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-76db958872af45289ad147a189e575c4.json,
run 865c036f665b4fdcae503257b800c050. Final creation host native build 65.54s is clean.

The cold restored-session qualification now uses the original ProtectedProfileStorage/owned startup
receiver rather than a fixture resolver. The old D-229 Android run 0b6bd586086d46a793e0f4e4ee0b6039
was absent from the current sandbox: DirectoryNotFoundException before host entry is retained in
artifacts/native-sqlcipher/d254-cold-restored-runtime.log. It is not a successful host test or a key
replacement. A fresh independently owned restore fixture is required for this current qualification;
unchanged general tests/main builds are not repeated. The driver permits an explicit cold-only
recorded restore run; it never chooses missing-key generation or financial fallback. Script parser passes.

Complete signed Release product APK: artifacts/android-d254-delivery/pro.vafadar.zanance-Signed.apk,
83,716,826 bytes; signature, pro.vafadar.zanance package, API24/36 and both complete assembly stores
verified. Test outputs cleaned successfully after terminal managed/build checks. Main remains
plaintext: native library adoption/receiver registration/recovery UI/retention/OS/platform/device and
release acceptance remain separate and unfinished. No new owner decision is required for this host.

The fresh retained restore case exposed a real host callback mismatch: SqlCipherProfileBootstrap
passes legacy profile ids, whereas the composed native receiver takes the exact original path.
ProtectedDatabaseStartup now maps the id through its unchanged complete ProfileStorageCapture before
passing the drained host owner. It never uses current selection or a filename guess. The failing
fresh dataset/run 70edbddd953f4a8ebca26e50c60fafb1 and its negative log d254-restored-runtime.log
are retained; only its cold restored-session phase resumes after this fix, without regenerating
backup, finance, identities or keys. Earlier baseline build/test results preceded this production
fix; final affected verification is recorded below rather than treating those old results as final.

D-254 final post-fix verification: the same retained run 70edbddd953f4a8ebca26e50c60fafb1 cold-opens
through actual composed startup with only existing keys, balance 321 and later fr preference preserved.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-512dc37bb32c48b096c1f8db7abc2a7b.json;
Created/Restored are explicitly null because those earlier effects were not repeated. Final Native
Release AOT build 84.50s and strict main Windows 35.02s/Android 60.31s have zero warnings/errors.
All 3476 solution tests pass, zero failures/skips (3m12.025s). The current original receiver path was
verified only with already selected/applied restore state; pending pre-selection and unapplied native
preferences remain receiver-integration work, not acceptance inferred from this cold read.

D-254 final signed product APK replaces the earlier baseline at the same delivery path:
artifacts/android-d254-delivery/pro.vafadar.zanance-Signed.apk, 83,278,384 bytes. Signature, package,
API24/36 and both embedded assembly stores were reverified after the callback fix. Final test-output
cleanup completed successfully. Main encryption is still inactive; this is the final APK for this
host-composition slice, not device/iOS/provider/retention/OS/release acceptance.

### D-255 — Compose retained restore rollback and native preference replay

SqlCipherRetainedRestore constructs an existing-record receiver from the captured stable identity,
original namespace and latest restore reference. Before selection it verifies the actual prior
selected ancestry, opens only its existing keyed ReadWrite/unpooled ciphertext, checks native version,
full cipher authentication and SQLite integrity, and registers captured access for bounded rollback
export. Missing/redirected prior storage, missing/wrong keys or foreign history refuse; no plaintext
connection, history initialization or replacement-key preparation is a fallback. Owned key buffers
are cleared. Already selected journals do not open the previous financial generation.

ProtectedProfileStorage can compose this receiver with the actual settings store/persistence port,
portable allowlist, real compiled-model migration and same-session restore adoption. Bootstrap now
passes its actual DatabaseProfileIdentity as well as original path/owner, without synchronous metadata
reads in the receiver builder. Explicit PrepareRestoreAsync requires a settled active session and
its exact selected history endpoint before recording/synchronizing the new intent/journal. Missing
package/capsule before staging remains a recovery UI/retry requirement, never inferred completion.
RequireStable refuses a new request during tentative switching or failed publication.

Resume checks recorded history before reading the preference capsule/key. Five added real metadata
cases refuse invalid/unrecorded references before key effects. All 3481 solution tests pass with zero
failures/skips (3m3.151s). Strict Windows 22.21s and Android 39.08s have zero warnings/errors. No schema,
permission or visible layout changed; unchanged layout/producer matrices were not repeated.

Two signed Release AOT Android x64/API36/4KiB scenarios use the actual composed application host,
actual BackupService and Android keys/durability/preferences/persistence. One interrupts before prior
key read/selection; the other after actual native preference persistence but before replay marking.
Cold processes continue the same recorded generations with existing-only key ports, restore balance
321, persist portable preferences once, retain a scoped device choice and make a completed startup
callback idempotent. Each hot fixture prepared exactly two keys; neither cold process prepared one.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-a348d8ec40184eaeba0b66a548f635b9.json,
runs a3fe7db634254e1c98b205d9a75f9e76 and 04388ff64c29463c9e9f5b41d20a8e58.
Final native build 72.83s is clean. This is real fault-port/cold-process evidence, not arbitrary
power-loss, whole UI/provider or physical-device/iOS acceptance. Main registration stays plaintext;
final native adoption/restore UI/recovery-key UI/retention/reconciliation/coupled OS policy remain open.

D-255 signed delivery: artifacts/android-d255-delivery/pro.vafadar.zanance-Signed.apk, 83,737,306 bytes.
Signature, pro.vafadar.zanance package, API24/36 and both complete assembly stores verified. Test outputs
cleaned after all managed/native/build processes were terminal. Main still uses standard SQLite;
this APK does not claim main encryption, phone/iOS/provider, full retention/OS or product release acceptance.

### D-256 — Deliver validated backup packages without premature restore intent

ProtectedBackupRestoreReceiver now connects BackupService's validated whole-package callback to the
actual protected host under its drained application lease. Construction/preview/password/ZIP/checksum
failure cannot reserve a financial restore intent. The host first checks supported sources/bounds,
opens the complete portable database read-only in bounded memory, requires a known ordered application
migration prefix and parses the allowlisted portable preferences without writing a capsule. Only after
that preflight does it reserve the existing-generation restore and invoke the original receiver.
Owned preflight buffers are cleared. No fallback to legacy sequential sources is introduced.

This is integrity/history/JSON preflight, not a claim that a forged matching migration history proves
every table's complete application semantics. Actual candidate migrations/full integrity checks remain
required. A later staging/persistence failure still needs the recorded recovery route; this change does
not abandon a committed intent or permit navigation past an unresolved selected generation.

The added actual app-source case verifies refusal before startup and release of the adapter's exclusive
lease with no metadata. All 3482 solution tests pass, zero failures/skips (3m0.101s); strict Windows
46.07s and Android40.48s builds are warning/error-free. No visible UI change or unchanged producer/layout
matrix replay. Native validation also exercises the actual whole-package receiver, not a fake MAUI UI.

Initial independent native compile failed with CS0136 in three fixture locals; explicit names corrected
only the fixture. The first runtime correctly rejected its invalid SQLite source, but the fixture caught
IOException without InvalidDataException. Its negative log d256-native-runtime.log and unchanged existing
run 18916586e9e9424f97020fd261bff73f are retained. The catch was corrected; that run's original database/
keys were not reset. Its earlier valid portable package existed only in memory and was cleared on failure,
so a distinct fresh fictitious dataset is required to complete positive delivery/cold continuation.
Main native registration and full restore/recovery UI/retention/reconciliation/OS adoption remain open.

D-256 terminal native qualification: original BackupService plus ProtectedBackupRestoreReceiver/host
reject damaged ZIP, valid-checksum invalid SQLite and valid-database malformed preferences before
adding a restore-history row or preparing another database generation key. Existing balance777 and
de preference remain. A valid package then records one intent, retains interrupted pre-selection
state and cold-restores balance321/en exactly once with existing keys and preserved scoped device
choice. This exercises actual native Android x64/API36/4KiB Release AOT/trim and ordinary app source.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-ee31c9650b864f1fbe7566b4d916ecba.json,
run73b022e4bc534359a84d8c8f4ade4c6b. The corrected native build is clean (57.23s). Old failure evidence
and its independent fixture remain. No main source changed after the passed 3482-test/strict-build
baseline; general tests/builds were not repeated for this fixture-only catch correction. Safety-copy
creation/keys belong to the separate UI consent path; this proof concerns financial-generation intent
and key effects, not a claim that every UI safety effect is absent. Main native registration remains
inactive; final UI/recovery/retention/OS and platform/device/release acceptance are still unfinished.

D-256 signed delivery: artifacts/android-d256-delivery/pro.vafadar.zanance-Signed.apk,83,315,248 bytes.
Signature, pro.vafadar.zanance package, API24/36 and both complete assembly stores verified. All managed,
native and build processes completed before successful test-output cleanup. Main remains plaintext;
this APK does not claim whole-app encryption, phone/iOS/provider/retention/OS or product release acceptance.

### D-257 — Qualify source-built Android package consumption

Android candidate packaging now includes framework-specific buildTransitive assets for bundled
component notices and source provenance. Native slices remain standard runtimes/android-arm64/native
and runtimes/android-x64/native assets; the Android SDK imports these itself. The actual production-
assembly caller now references Vafadar.Native.SqlCipher.Android rather than loose build-directory
AndroidNativeLibrary items. Package version4.19.0-candidate.3 is centrally pinned; managed bridge3.0.5
and official source-built native bytes remain unchanged. Candidate status is not changed to release
acceptance. No package is published to a remote feed.

The repeatable caller packs into a fresh owned output, populates an immutable local feed only when
its version/hash matches, and restores with explicitly named native-package.nuget.config. Exact
source mapping confines this package name to the local feed; other dependencies use the established
public source. The config's explicit name avoids changing implicit restore configuration for other
native/repository projects. Binary builds additionally require TreatWarningsAsErrors; APK inspection
requires both original ABI hashes, packaged notices/provenance and no default e_sqlite3 leakage.

The first edit damaged a LF project's closing XML while removing CRLF-counted text; restore refused
before native execution. The exact project was repaired, retaining d257-package-consumer.log. Candidate.2
then restored and ran correctly but explicit AndroidNativeLibrary items duplicated SDK runtime assets,
producing two XA4301 warnings. Original runtime/build evidence is retained, not clean-build acceptance.
Those duplicate items were removed from package targets and immutable version bumped to candidate.3;
no warning was suppressed or package cache replaced.

Final strict signed Release AOT/trim caller build has zero warnings/errors (50.72s). Only the cold
phase of the same fictitious run331ce8fb80634664ab4e32ae4e716d84 continues through the new package;
creation/database/native keys were not reset. Actual production persistence assemblies, 32 migrations,
negative money, binary portable snapshot and wrong-key refusal pass. Proof:
artifacts/native-sqlcipher/build/android-d/binary-runtime-proof-b1c72b1eee9241c7bc79453ed129afae.json.
Earlier creation/runtime proof remains binary-runtime-proof-8999f5611cc04175a5e8ef4d3892ab5f.json.

Two packages of the final version are byte-identical, hash
a31d841fe87e14ed93d678b8d0b94baa72dffabfd5dae17949192416427aa1d8; both native slices exactly match
reviewed candidate files. Explicit renamed-config restore and script parsers pass. The actual main
asset graph contains the standard bundle and no Vafadar native cipher package. Main source/UI/engine/
APK remain D-256, with its3482-test baseline; no unrelated tests/main builds/APK or unchanged producer
suite was repeated. This qualifies package consumption on observed Android x64/API36/4KiB only;
arm64/API24/16KiB OS/iOS, native warning review, main registration/recovery/retention/OS/device/release
acceptance remain open. The full goal and SEC scope are not complete.

### D-258 — Bound Windows secure-heap list-index narrowing

The current unsuppressed Windows inventory contains749 OpenSSL occurrences, not direct SQLCipher
warnings.20 C4244/C4267 sites in official crypto/mem_sec.c narrow only list/slist indices into int
(or the sh_getlist return into int). Under valid allocator state their interval is0..59 on the
reviewed Windows x64 ABI; this is expression/range review, not whole allocator security acceptance.

SH_LIST has two x64 pointers, so effective power-of-two minsize is at least16. Accepted arena_size
is a representable positive power of two, at most2^63; minsize cannot exceed it, and sh_init rejects
bittables smaller than one byte. For arena=2^a and min=2^m, ratio=2^(a-m), bittable=2*ratio and
freelist_size=(a-m)+1. Thus k=a-m<=59. sh_malloc starts list at k, only decreases it, checks negative
list/slist before helper calls and splits back toward the bounded requested list. sh_getlist starts
at k and shifts an initial ratio<=bit<2*ratio until a valid allocated ancestor, returning0..k.
sh_free coalesces toward the root; its root buddy is bit0, never set by valid positive-node setbit
calls, so no valid coalescing step decrements below0. sh_actual_size consumes the same bounded return.
This reasoning assumes intact allocator metadata and valid pointers; it does not establish safety
for arbitrary corrupted pointers, pointer arithmetic generally or all OpenSSL entry points.

Review-WindowsSecureHeapRanges.py pins the exact official source SHA256, Windows/OpenSSL commit,
matching native DLL and all20 original occurrence lines/codes, then checks1711 admitted power-of-two
parameter classes. It refuses incomplete coverage rather than writing acceptance. It modifies no
source/binary/compiler flag or suppression and never opens product storage. Positive proof:
artifacts/native-sqlcipher/d258-accepted-secure-heap-ranges.json. Incomplete inventory is refused with
no output acceptance file. The earlier conservative-class artifact is retained; the final calculation
excludes tiny rejected bittables. Source hash:
2406f86ebc633aa7cdf27ab9617a8a49b6ec238e6d9dd3792b749cf7257e5d5e.
Native hash remains3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.

This bounds20 narrowing occurrences only:723 C4244/C4267 remain unreviewed in addition to the6
previously reviewed atomic/shift occurrences. All original749 compiler diagnostics remain present;
no warning-free native build, full Windows adoption or main encryption is claimed. No code/UI/APK,
main suite or unchanged native producer suite was repeated. Main remains D-256/plaintext with its
3482-test baseline. Full security/OS/platform/device/release and original goal scope remain open.

### D-259 — Triage legacy AES conversion sites without blanket approval

All16 C4267 occurrences in official OpenSSL crypto/evp/e_aes.c belong to GCM/TLS, CCM/TLS,
AES Wrap or OCB handlers (lines2960,3007,3164,3565,3573,3606,3612,3631,3641,3721,3731,3738,
4025,4041,4086,4093). The exact SQLCipher OpenSSL source chooses EVP_aes_256_cbc and calls
EVP_CipherInit_ex/Update/Final; these warning lines are not its CBC handler. That direct binding
observation is not proof that all compiled OpenSSL entry points are unreachable or safe.

Do not approve these conversions merely because EVP update inputs use int. The legacy padded-Wrap
output-length expression at3731 rounds input then adds8. At input2147483640 (itself within signed
int range), ((inlen+7)/8)*8+8 is2147483648, outside Windows int. This is a source-expression
counterexample to a blanket range proof, not a reproduced public-API vulnerability or exploit:
actual provider dispatch, output-query rules, error propagation and contextual bounds still matter.
EVP's ordinary block-update overflow guard is after the custom-cipher branch, so that later guard
cannot by itself qualify all custom handlers. OCB partial-buffer additions have local block-size
bounds, but this slice does not separately close them or infer whole handler safety.

Immutable source hashes and all16 original occurrences are captured in
artifacts/native-sqlcipher/d259-aes-warning-triage.json. e_aes.c hash:
02b2fdbd8b105c55ac78805806c027f60fe5d42603a706fc5741d177a8f3ae40;
SQLCipher provider hash:a429d5466fa3fe7bb7608c3f79e8e19ff5706b78f6251b44530171dc30e53f69.
No upstream patch, warning suppression, binary rebuild, large-buffer allocation or user-data access
was made. All16 remain unapproved inside the723 remaining narrowing occurrences. Further adoption
must qualify actual reachable/provider input constraints or an officially supported reduced build
profile, not equate unused-at-the-direct-call-site with full native acceptance. Windows/main encryption
remain unactivated. Main code/APK and3482-test baseline remain D-256; unchanged producer, UI, main
build/test/APK checks were not repeated. Full goal/security/platform/release scope remains open.

### D-260 — Reduce the Windows crypto build with supported upstream options

The Windows recipe now reads windows.crypto.profile.json and records its SHA256 with the actual
Configure options. Only the explicitly reviewed no-dsa/no-dh/no-cms/no-cmp/no-ocsp/no-ts/no-quic
options are allowed; arbitrary no-* removals and duplicate options are refused. The required SQLCipher
AES-CBC, SHA/HMAC, PBKDF2 and random primitives remain enabled. No source fork, patch or additional
warning suppression is introduced. Recovery RSA remains the platform implementation; native RSA
is not removed. An initial no-rsa Configure option was rejected by upstream before any native output;
that negative run is preserved and the supported profile used a fresh independent tree.

The initial supported run windows-storage-d260-supported produced SQLCipher DLL
SHA256 e9541bf30523f0c65126525c816d58863ffb0c8811d3509fed08f353af705ed9,4940800 bytes.
Actual configdata confirms all eight removals and no required primitive disabled. The original
production assemblies passed create/cold reopen, all32 migrations, original negative money,
portable memory snapshot and wrong-key refusal. Binary proof directory:
artifacts/native-sqlcipher/binary-host/d1f6b3bca79e4295af7037342df26008.
The new native build emits646 warnings (570 C4267,72 C4244,2 C4319,2 C4133), versus749 in the prior
profile. These are a new inventory, not transferred warning acceptance or a warning-free build.
Profile/configuration evidence: artifacts/native-sqlcipher/d260-profile-proof.json.

The first supported profile failed the official suite:347 files/2219 reported tests, failing
80-test_ssl_new.t compressed-certificate configuration and90-test_sslapi.t group configuration.
Official test source requires positive default supported groups; compressed-certificate fixtures
require TLS1.3 success. Removing both EC and DH left that protocol without usable groups.
No failed recipe is excluded or source-patched. The revised profile retains EC and removes only
DSA,DH,CMS,CMP,OCSP,TS,QUIC. Fresh run windows-storage-d260-ec keeps old objects/results separate.
Revised native output is5641216 bytes, SHA256
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21. Actual configdata confirms EC
retained and all seven requested removals. All704 diagnostics are OpenSSL:628 C4267,72 C4244,
2 C4319,2 C4133; they are not suppressed or blanket-approved. The actual production-assembly
create/cold proof is artifacts/native-sqlcipher/binary-host/3a4d29d9ace8421d80b22a20dfe91bce,
with zero managed build warnings/errors. The source-based schema/tamper/integrity suite also
passes32 migrations; its expected negative HMAC errors are retained.

Windows packaging now requires the current crypto-profile hash as well as source/native identity,
and uses immutable4.19.0-candidate.2. The old profile is refused before packaging. Package native
bytes, complete manifest and bundled component notices match; package SHA256:
7cfac1cc7ed8bb5b187a75049ceee51fce95ce9541413ffe723cd3e81950a5c6.
The revised official OpenSSL suite passes:347 files/3473 reported tests; 114 whole recipes
are skipped and 775 explicit skip markers remain. Both previously failing TLS recipes now pass,
including test_configuration_of_groups. Native DLL hash remains unchanged by the suite.
Full log:artifacts/native-sqlcipher/build/windows-storage-d260-ec/upstream-f17bbb9c64f2407a9ffaff830f24107a.log.
This qualifies the tested configuration; it is not full vulnerability, main encryption or release acceptance.

Main registration remains inactive/plaintext. Main code,3482-test baseline and signed APK remain
D-256; unrelated main builds/tests/UI/APK and unchanged Android producers were not repeated.
Final provider/bootstrap/recovery/retention/reconciliation and coupled OS exclusion, remaining native
platform/device acceptance and the original full goal remain unfinished.

D-261 completes local Windows candidate package consumption for the existing production-assembly host:
immutable native package/provider, win-x64 runtime resolution, exact notices/provenance and clean
strict build. Original32 migrations/keyed financial/portable/cold cases pass. Main storage is still
plaintext/D-256 with3482-test baseline; app host/recovery/retention/OS/platform acceptance remains open.

D-262 removes database-file mutation from DI path resolution. Actual covered startup drains its
factory, guards protection-required state and preflights legacy naming conflicts before migration.
Five new cases and3487 tests pass; strict Windows/Android builds and existing native startup retry
checks pass. Main encryption remains unactivated; no crash/power-loss/device/iOS acceptance is claimed.

D-262 complete signed Release APK is artifacts/android-d262-delivery/pro.vafadar.zanance-Signed.apk
(83765978 bytes); signature/package/API24-36 and both assembly stores verified. No device release acceptance.

D-263 Android main storage is implemented and locally verified: official SQLCipher, automatic native
keys, optional recorded recovery, retained portable recovery, all-profile lifecycle, encrypted safety
and authenticated reference-aware retirement. Final Android OS backup/transfer exclusion is coupled
to adoption. PIN remains independent and portable passwords optional. Windows/iOS main registration
is unchanged and their adoption/device/provider/Store gates remain open.

Corrected 3509 tests passed with zero failures/skips, outputs cleaned. Strict Windows(17.50s) and
complete signed Android Release builds passed. Final APK 88213830 bytes, SHA256 1f58420b304788fec0dbca7982128de3221b55a76d22fa9aa9d20a85f18c03b5;
v2/v3, permanent package, API24/36, not debuggable, runtime13 and both cipher/assembly payloads verified.
Actual corrected 18 native language/theme/width contexts check original navigation, key-loss cover,
growing captions and actual picker cancellation while preserving exact envelopes/financial hashes.
No power-loss/all-crash-points, phone/arm64/API24/16KiB, spoken, OEM, hosted-CI, iOS/Windows, OAuth or
publication acceptance is inferred. Complete evidence: docs/quality/android-protected-storage.md.
The full canonical backlog and continuous-development objective remain open.

## D-264 Windows protected storage status

Windows main protected startup, DPAPI recovery enrollment/key-loss/cold reopen, isolated profile creation/switch/retirement, encrypted safety retention and explicit portable replacement have native application evidence on an independent fictional installation. Shared Android effects remain platform adapters to the same application algorithms. Final local verification: 3521 tests passed with zero failures/skips, strict Windows/Android builds without warnings/errors, complete signed APK and scoped emulator upgrade/cold reopening passed; see [Windows quality record](../quality/windows-protected-storage.md). No iOS, physical-device, OAuth, Store or hosted fresh compiler acceptance is implied. The continuous-development objective remains open.

## D-265 holding accessibility and retired Home reads

Eighteen native contexts for three changed pages and eighteen distinct bottom contexts cover fa/en/de, both themes and 360/412/wide at process-local 200% typography; Cancel and original command/row identities retain financial data. The actual original Home read waits behind SQLite maintenance, retires with its Shell and completes without publishing into the old collection; replacement Home and complete stored rows verify. 3521 tests pass (zero failures/skips); native rendering is separately verified. [Bounded evidence](../quality/holdings-readable.md). Final artifact/build details accompany delivery; physical-device, actual OS scaling/spoken screen readers and iOS remain open.


## D-266 device-only authentication requires success

The owner approved keeping the original lock cover when system authentication is unavailable on 2026-10-14. Only AuthenticationOutcome.Success authorizes device-only entry. Failed, unavailable, unknown and throwing responses preserve the original cover and queued destinations. The existing translated unavailable-device guidance explains restoring a device screen lock. PIN verification/recovery and the previously authenticated return grace remain separate and unchanged. No new stored data, permission, SDK or portable preference is introduced.

Actual Windows LockPage/native Invoke checks covered 18 language/theme/width contexts with an explicit authentication response port; a separate feedback pass checked the translated guidance. The independent Android package used the actual native adapter: NotAvailable after automatic prompting and Unlock, with Back unable to dismiss the cover and zero queued destinations opened. Neither review changes system credentials or native PIN storage. These checks do not accept configured biometrics, Windows Hello, physical devices or iOS.

An unrelated Home collection rendering COM exception remains observed during language/theme review. The lock assertions and unchanged financial fingerprint completed, but this is not a clean whole-application rendering result. SEC-04 remains in progress for its remaining platform and biometric transitions.

D-266 final domain/application suite: 3,526 passed, zero failures/skips, 3m24.408s. Permanent-identity strict Windows build: zero warnings/errors, 23.49s. No unchanged cipher producer or upstream corpus was repeated.

Final D-266 signed Release upgrade/cold Home passed on the owned emulator; test outputs cleaned. APK signature/payload and the open Home renderer finding are recorded in docs/quality/device-unlock-policy.md. This does not complete SEC-04 platform acceptance.


## D-267 complete Home balance publication

Observed Windows Home refresh failed in BindableLayout's ContentView handler disconnection while Balances.Clear published an empty intermediate list. Currency totals now use the existing SnapshotCollection complete replacement, preserving existing amount rows during ordinary and covered reloads. Currency calculations, inclusion policy, formatting and current-page retirement guards remain unchanged. The native regression checks original Home and original amount-handler identities, including a real modal cover; financial storage is not edited.

D-267 final native review passed all 18 contexts and 36 original Home reloads without the observed COM failure; amount handlers and complete financial rows remain unchanged. Canonical signed Release upgrade/cold/More-to-Home return passed. The unchanged 3,526-test D-266 baseline was retained; native publication is directly verified. Exact evidence and external limits: docs/quality/home-balance-reload.md.


## D-268 sale-result help and truthful connection/account copy

The planned LOC-05 help sits beside an available realised holding result and explains recorded proceeds minus recorded average cost, unknown inputs and the absence of tax rules or official tax documents. The six existing languages include a concrete example. No tax calculation, financial posting, schema or new language is added.

LOC-10 corrects the browser-return message: it describes completion of the browser step and directs the user back to Zanance for the connection outcome, without claiming token exchange succeeded. English loan pay/receive confirmations identify the selected account explicitly, matching the original session argument and the other five translations.

Store drafts now name all six existing languages and describe optional password protection for local/connected backups, readable unprotected files, Android device-protected database keys and final system-backup/device-transfer exclusion. Real OAuth, physical-device, iOS and publication acceptance are explicitly separate; no store text was published.

D-268 final tests: 3,526 passed with zero failures/skips (3m21.454s); 18 original sale-help contexts and 12 English confirmation dialogs completed without financial writes. Independent Android help and final signed Release cold Home passed. Exact scope and retained observer failure: docs/quality/sale-and-connection-copy.md.

## D-269 costed OD-02 backend proposal

Official provider prices and the 2026-10-15 PostgreSQL availability change inform three bounded USD scenarios and a complete first backend delivery/restore boundary. Arithmetic/overage checks pass; see docs/enhancements/2026-10-commercial-release/07-backend-cost-and-decision.md. Provider, region and test-environment budget remain owner choices. No backend, account, mail, hosted resource, SDK, app code or publication changes; ZCR-ID-01 and the full development objective remain incomplete. Application tests/build/native/APK evidence is unchanged D-268 and was not repeated.


## D-270 shared Windows native toolchain qualification

Build, upstream validation and atomic-width validation now use one reviewed toolchain resolver.
Previously the build accepted both reviewed installer patch versions while the checks accepted
only the original installer and did not qualify compiler bytes. All three consumers now require
the same pinned MSVC, SDK, installer allowlist and exact compiler executable hash before execution.
Windows CI runs the qualification check before native preparation; its dependency cache includes
the shared resolver. No compiler installation, source fork, cryptographic flag or binary changed.

Eleven qualification cases passed, including both allowed installer metadata versions and refused
unreviewed versions, paths, SDK/tools, policy and compiler bytes. Actual installation remains
18.10.12201.205: the other patch version was tested as metadata only. The current candidate's
original upstream atomic operations passed the sentinel, 64-bit carry, four-thread update and
disassembly checks; native SHA256 remains
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21.
The 704 upstream diagnostics remain recorded and broader warning/security qualification remains
open. This tooling delivery does not imply hosted Windows acceptance or device/iOS/publication
acceptance. App code, the 3,526-test baseline and signed APK remain D-268; unrelated checks were
not repeated. Evidence: eng/native/sqlcipher/Test-WindowsToolchain.ps1 and
artifacts/native-sqlcipher/build/windows-storage-d260-ec/atomic-width-51c7a432ac70474da3e3206c04cd5e5a/proof.json.


## D-272 complete local identity and private service foundation

Implemented the approved optional ASP.NET Core/PostgreSQL/private-storage backend as an independent
Zanance API project. Maintained Identity password/token providers, protected bearer tickets,
transactional one-use challenges, absolute device grants, atomic refresh generation, immediate
stamp/session revocation, shared durable request limits and private encrypted immutable objects
are connected end to end. Capacity is serialized by real owner in PostgreSQL. No local profile id
or opaque object id establishes ownership. No financial sync/sharing/billing route is implemented.

The separate migration operator and runtime role, persisted wrapped Data Protection key ring,
leased encrypted fictional outbox, resumable deletion/cleanup, content-free readiness and hidden
local start/drain/snapshot/fresh restore operations are implemented. Missing service keys keep the
original installation closed; no replacement key authority is accepted. No real email, account,
paid hosting, app connection, entitlement enforcement or publication was enabled.

Final server Release build has zero warnings/errors. Four actual-host/PostgreSQL scenarios pass
with zero errors/failures/skips on the private .NET/ASP.NET 10.0.13 runtime (6.355 seconds); they cover concurrency,
expiry/replay, ownership, quota, protected delivery, corruption, revocation, cold key loss/recovery
and real pg_dump/pg_restore. Fictional restore was about 0.7 seconds with its committed object and
revocation state preserved. Windows Kestrel and the operator's independently restored destination
returned readiness 200; exact scopes and observed failures are in quality/local-identity-backend.md.

The existing 3,526 app-test baseline and complete signed mobile APK remain D-268; no unchanged app
build, native visual matrix or cipher corpus was replayed. Local ID-01 foundation is delivered;
external sender, hosted certificate/key/backup operations, provider/legal acceptance and mobile
service adoption remain separate gates. The whole development objective remains active.

## D-273 target currencies and source-qualified holiday calendars

Deliver LOC-07/08 as one app batch. The working Western Europe scope is DE, AT, BE, FR, LI,
LU, MC, NL, CH, GB and IE, plus US, CA and AU; the broader geographic boundary question
remains open. Existing EUR, CHF, GBP, USD, CAD and AUD already have ISO minor digits of two.
No stored amount, account currency or language is changed by selecting a country. Country
registration is additive, independent of the interface language. Existing Iran dates remain
compatible; an Iran release variant is future work.

Offline holiday sources and regular dates are reviewed for Gregorian 2026-2027. Settings,
onboarding and Plan display scope, authority, reviewed years and an unverified-year warning.
Switzerland has only August 1 federal coverage; Canada uses federal nominal statutory dates;
the US uses standard Monday-Friday observed federal dates. UK and Australia contain only dates
shared by all constituent regions. Regional, workplace, extraordinary announced closures and
bank-business-day guarantees are excluded explicitly. Legacy Germany/Iran calculations remain
compatible outside the reviewed window; new country datasets do not invent future dates.
Saved Plan country/weekend masks are retained when device regional preferences change.

All 3,569 app tests pass, including 43 new cases; final Windows and signed Android Release builds
have zero warnings/errors. The original regional view passed 18 hidden own-window layout checks
(en/fa/de, light/dark, 360/412/1280 px, Debug text scale 2), complete captions and 44 px picker
height, with unchanged financial rows. Eighteen actual native selections passed in the three
English light contexts. Hidden RTL popup automation failed and is not counted as selection
acceptance. Actual signed emulator Settings selections for Canada, US and Australia, live
Persian/German/English captions, light/dark app choices and return to no holiday calendar were
checked separately through native XML. No financial entry was posted. Emulator Home remains
0.00 EUR; English, Gregorian and automatic digits/week start were restored; theme ends at
Use device setting. This is not physical-device, spoken screen-reader or iOS acceptance.

Detailed official sources, exact scope and observed verification failures are recorded in
quality/target-holiday-calendars.md. No new SDK, network request, permission, schema, paid resource
or commercial enforcement was introduced. The full development objective remains active.

## D-274 operation-scoped encrypted quick-resource reads

Measured protected Release startup after D-273: system first frames 1664/1401/1462 ms,
while loaded Home is observed later (accessibility polling upper bounds, not exact paint).
The original quick-resource operation reopened native keyed connections between EF queries.
A single DbContext alone did not improve its measured warm median (~989 versus ~999 ms).
The complete operation now captures one original context and explicitly keeps its non-pooled
connection open through both resource choices, complete input read and every original validation.
Standalone resource-choice reads also keep their own operation's connection open. No key format,
KDF, schema, guard, commercial activation, global pool or cross-operation credential cache changes.
Later explicit actions still read and validate fresh original data through a new context.

The same independently encrypted fictional fixture and actual production Data assemblies show
warm complete-read median 999.38 ms before, 82.42 ms after. First read 1220.60/565.23 ms includes
process/JIT effects and is kept separate. Wrong-key refusal, 32 migrations, negative balance and
portable read-back pass. Two actual-SQLite cases prove one captured context, unchanged rows/no
default settings or resource-choice writes, fresh action validation and drained ownership.

A Windows review launch exposed stale ApplicationId metadata despite an independent build property.
Its exact process was stopped; that attempt is not independent-installation acceptance. Touch/rebuild
and compiled identity checks precede the corrected review. Debug Home/holiday review routes now
reject a main identity before covered storage startup, without changing Release behavior. Do not
inspect or restore an unrelated installation to turn that failed attempt into evidence.

Final affected tests/build/native results and artifact identity are recorded in
quality/quick-resource-read-performance.md. Earlier full 3,571-test pass precedes the explicit-open
refinement; final Data/App tests are repeated because their production dependency changed, while
unchanged successful test projects retain that baseline. QA-06, full loaded-Home timing, large-data
native publication, physical devices and iOS remain open. The development goal stays active.

## D-275 complete Home loading and retry

Original Home now uses the existing snapshot load state from its first frame through complete
read/publication. Empty initial defaults and dashboard actions remain unavailable until ready;
an actually empty completed snapshot retains its existing next step. Six-language loading,
Home-specific failure/retry, original serialized reloads and retirement guards preserve data.
No schema, permission, SDK, key, stored preference, commercial activation or money formula changes.

All 3,572 app tests pass (zero failures/skips), with the new AT-275 final-retirement publication
case and existing initial/reload failure/retry/cancellation coverage. The final caption refinement
passes 242 localization tests without repeating unchanged successful projects. Strict Windows and
Android builds have zero warnings/errors. Eighteen native language/theme/width contexts, 36 focused
loading/failure captures, 18 actual native retries and unchanged complete financial fingerprint
pass in an independent protected installation. The first partial review exposed a wrong reused
transaction retry caption and was superseded by the corrected complete matrix.

Complete signed Android Release cold launch on the owned emulator shows preparation, actual Home
loading, then ready balance/actions in the same process; no false empty-account card appears.
APK signature/package/payload and exact artifact are recorded in quality/home-loading-publication.md.
This closes the observed publication defect, not the two-second performance objective, Android
fault injection, physical device, spoken screen-reader, OS scaling, iOS or release acceptance.
The development goal remains active; independent owner/provider gates remain unchosen.

## D-276 complete protected work-read performance batch

Complete Plan/Goal reads and fresh validation, budget choice/form reads and validation,
quick-resource validation and bounded budget-alert inputs keep their own non-pooled connection
open through the original operation. Budget form choices and inputs capture the same original
context/file. Every existing rights/stale/current-file check and writer/financial formula stays;
there is no global key-bearing pool, KDF reduction, schema or commercial activation change.

Actual production-assembly native comparison on the same encrypted fixture measures complete
read plus fresh validation: Plan 251.36 to 140.24 ms, Goal 624.61 to 180.53 ms, budget form
1298.93 to 155.51 ms, budget alerts 2281.44 to 653.78 ms, quick resources 788.64 to 146.29 ms
(three warmed medians; first use separate). Negative balance, 32 migrations, portable read-back
and wrong-key refusal pass. These are operation timings, not full loaded Home or device acceptance.
All 3,573 app tests pass with one new actual-SQLite budget context/lifetime case; strict builds,
original native Home and signed Release evidence are in quality/work-snapshot-read-performance.md.
The whole development objective and physical/iOS/reference-workload gates remain open.

## D-277 existing-user upgrade and current test-build policy

ENT-05's current policy is already implemented: the default commercial source is inactive,
with no fake Plus/Pro right or permanent/portable test entitlement. The actual first-schema
upgrade scenario writes four accounts above the approved Free quota and an expense, upgrades
through original migrations, retains identities/balance, accepts an explicit fifth test account,
and restores the real portable source into a separate fresh provider. Both original and restored
providers remain unenforced with null paid context; no resource selections are invented.

One focused AT-277 real-SQLite upgrade/restore scenario passes under strict compilation. It adds
one behavior to D-276's 3,573-test baseline (3,574 covered behaviors, not a new full-suite run).
No app/runtime/schema/dependency/UI change; no unrelated build/native matrix or new APK. The
complete signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.
Exact evidence and scope: quality/test-build-upgrade-policy.md.

Customer activation and announcement remain explicit owner/BIL gates. Do not turn disabled test
enforcement into permanent paid facts or announce future limits as already enabled. LOC-09's
final unattended protection choice and separate local personal-sync approval are pending;
none follows from backend foundation approval. The full development goal remains active.


## D-278 protected reference and tenfold workload evidence

An independent Windows executable now measures the actual protected generation/bootstrap and
production store/validation/calculation paths on deterministic 10,000-entry / 20-account / 100-plan
and tenfold workloads. Complete original schema/header/integrity/typed rows match the authenticated
pre-migration encrypted rollback after measurements, and both plaintext originals are removed.
Warm medians: all-entry reads 158.617 / 1191.737 ms; Plan read/validation 131.983 / 138.041 ms;
quick resources 127.675 / 126.937 ms; original balances/totals 1.487 / 19.066 ms.

Fresh tenfold protected preparation is 27,561.853 ms. Reference 439.797 ms is explicitly a resumed
revalidation, not fresh migration time: the original run terminated on an invalid raw export-byte
comparison, and the corrected complete logical verifier resumed the same generation/keys. The
terminal corrected run passed both workloads; strict Release compilation has no errors/warnings.
No app code/UI/schema change, unrelated test replay or new APK. Application coverage remains
3,574 behaviors (D-276 full 3,573 plus D-277 focused one), not a new complete suite run.

See quality/protected-workload-performance.md and the executable README for samples, failures,
resume ownership and exact scope. QA-06 remains partial: actual loaded Home/search/save, cold/ANR,
reference phone, Android/iOS and the two-second objective are not established by store timings.
The current signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.


## D-279 hosted Windows native qualification

Native cipher candidates now includes a Windows lane using the existing exact compiler qualification,
two fresh native builds with matching DLL bytes, original schema/tamper checks, immutable package
entries, actual production-assembly create/cold reopening and protected reference/tenfold workloads.
Only allowlisted public logs/proofs/measurements are uploaded; databases and DPAPI/key material stay
private. No candidate cache, weaker package/compiler policy, new native dependency or app change.

Actual YAML and all five Windows PowerShell bodies parse. Hosted success is not inferred from syntax
or the existing local D-278 workloads; check the final pushed commit once after the normal delay.
Existing native warnings and the separate unprivileged Windows OpenSSL-suite scope remain explicit.
No unrelated successful app/UI/full-suite/APK replay: application coverage remains 3,574 behaviors
(3,573 full plus one focused), signed APK remains D-276. See quality/windows-native-ci.md for the
exact path, artifact policy and remaining platform/security/device/release gates. Full goal remains open.


## D-280 current protected search and validated-save workloads

The independent protected executable now exercises original complete search/group and validated entry
Save on fresh reference/tenfold data. Focused ledger mode omits unchanged D-278 read/calculation cohorts;
default hosted runs include all operations. Four searches per shape check unique transfer/missing/numeric/
complete results; four actual saves per shape verify stored fields and delete exactly their own row.
Complete schema/header/integrity/typed rows still match the pre-migration encrypted rollback afterward.

Actual warmed unique search/group medians are 9.084 / 17.202 ms; validated saves 68.810 / 69.033 ms.
Search timers include result checks and exclude initial read/native rendering. Both workloads finish,
all eight temporary rows are read back/removed and both original plaintext files are absent. Strict
Release builds have zero warnings/errors. See quality/protected-ledger-performance.md for all samples,
result counts, preservation and timing limits. No unrelated app/UI/APK/full-test replay or new app
change: 3,574 covered behaviors retain their qualified baseline. Native phone/Home/search/Save, cold/
ANR, iOS, the approximate two-second objective and full goal acceptance remain open.

## D-281 automatic device-encrypted local copies

LOC-09 is implemented locally under the owner's final device-key policy. Daily/weekly/off foreground
coordination, independent per-profile timestamps and automatic/pre-restore retention, six-language
status/help/discovery and explicit original protected restore are complete. No password storage,
readable automatic file, cloud fallback, sharing action, new crypto/provider/schema or permission.

Full strict suite: 3,590 passed; final focused Data publication case: one passed (3,591 covered
behaviors, no second full-suite claim). Strict Windows Debug and Android Release builds, 18 final Windows
contexts, native encryption/reference-aware retention, six Android changed-surface/help contexts,
original explicit restore and complete signed APK are recorded in quality/automatic-local-backup.md.
The initial Android live-picker publication loop and native validation lock issue were corrected;
only related checks were repeated. Local success is not hosted CI, physical-device/iOS/OEM,
power-loss, OAuth, store-signing or product-publication acceptance. The full goal remains open.
