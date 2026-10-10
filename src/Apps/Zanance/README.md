# Zanance

Personal finance manager: record income and expenses, organize them into accounts and categories, and understand
where the money goes with reports. Local-first and offline, multilingual (English, Persian, German, Spanish, French, Italian, with Gregorian, Persian or lunar Hijri calendar) and backed up
with optionally password-protected files the user keeps wherever they like, or – in builds with cloud backup (D-35) – in the user's own Google Drive or OneDrive. Connected destinations discover existing app backups automatically.
Interface language, regional date/number formats, digit shapes and holiday rules can be chosen independently.

| | |
|---|---|
| Product name | **Zanance** – shown untranslated in the app, stores and notifications; code and folders use the same name (D-24) |
| App id | `pro.vafadar.zanance` – Android package and iOS bundle id (permanent once published) |
| Platforms | Android (first), iOS, Windows |
| Status | 🚧 Phase 1 feature-complete; cloud backup (OneDrive and Google Drive on Android, iOS and Windows) implemented and waiting for OAuth clients and a device test (D-35, D-50): accounts, entries, transfers, refunds, categories, plans with Gregorian/Persian recurrence and automatic posting, Home dashboard, budget, reports, forecast, reminders, manual exchange rates, CSV import/export, optionally encrypted local/cloud backup files (D-62), first-run restore without a new account, theme/experience choices, Simple/Advanced, app lock. Phase 2A in progress: goals with optional contribution-date reminders, rollover and envelope budgets, splits, partial payments, contracts, reimbursements, tags, rules, loans with a repayment estimate, assets, receipts, weekday rules, display units such as the toman, forecast scenarios, saved filters, PDF reports, a customizable Home, a dark theme, flex, weekly and two-week budgets with pay-cycle months and limit suggestions, public holidays, a quick add widget, receipt and PDF invoice reading, and local profiles. Status: [phase 1 plan](docs/04-phase-1-plan.md), [phase 2 backlog](docs/05-phase-2-backlog.md). |
| Solution filter | [`Vafadar.Zanance.slnf`](../../../Vafadar.Zanance.slnf) |

## Projects

| Project | Contents |
|---|---|
| [`Vafadar.Zanance.Core`](Vafadar.Zanance.Core) | Domain model and calculations (platform independent): ledger, plans and recurrence, budget, reports, forecast, reminders, rates, CSV |
| [`Vafadar.Zanance.Data`](Vafadar.Zanance.Data) | `ZananceDbContext`, migrations, stores, automatic posting, backup summary |
| [`Vafadar.Zanance.Reports`](Vafadar.Zanance.Reports) | PDF reports with the embedded Vazirmatn font (right-to-left aware) |
| [`Vafadar.Zanance.App`](Vafadar.Zanance.App) | .NET MAUI app: pages, view models, platform code |

Tests: [`test/Apps/Zanance`](../../../test/Apps/Zanance).

## Run

```powershell
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -t:Run -f net10.0-android                 # emulator / device
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -t:Run -f net10.0-windows10.0.19041.0     # Windows
```

## Database migrations

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project src/Apps/Zanance/Vafadar.Zanance.Data --startup-project src/Apps/Zanance/Vafadar.Zanance.Data
```

Migrations are applied automatically when the app starts. They must only add to the schema (AT-60, tested).

## Reviewing screens without a device

Debug builds can walk through every screen in English, German and Persian, in the light or dark theme, and save
screenshots (plus the report PDF) to `artifacts/snapshots`:

```powershell
./eng/scripts/Run-Snapshots.ps1 -Languages fa,en [-Theme dark]
```

The script builds the Windows Debug app, resets only its own development database, seeds fictitious data and closes
the app when done (`Diagnostics/DebugSnapshots.cs`).

## Documentation

* [Design documentation](docs/README.md): assessment, domain, UX, phase-1 plan, backlog, privacy matrix, test plan,
  release checklist
* [Requirements](docs/requirements.md)
* [Changelog](CHANGELOG.md)
* Shared concepts: [architecture](../../../docs/architecture/overview.md), [localization](../../../docs/architecture/localization.md),
  [data and backup](../../../docs/architecture/data-and-backup.md), [privacy matrix](../../../docs/privacy/privacy-matrix.md)

Privacy controls (D-63): optional four-digit app PIN across local profiles, protected verifier and restart-persistent
attempt limiting; separate Android foreground screenshot preference, on by default, with recents protection retained.
Owner-written source is proprietary and all rights reserved; bundled third-party notices apply only to dependencies.

Period review reminders (D-71): optionally enable a reminder in Settings for 09:00 after your financial month
closes, following the selected calendar and month start day. Off by default; no automatic review or money movements.

Aggregate CSV import (D-72): review each overlapping aggregate and choose linking or keeping both. Preview dates,
amounts and the remaining total before saving. Linked imports retain safe Undo across restart and database backup;
later changes or dependent imports require review. No automatic merging. See AT-79 for evidence and device gates.

Application flow coverage (D-73 / QA-03): 68 AT-80 cases compile actual non-UI onboarding, profiles, bulk commands,
access, widget/reminder routing and theme sources with explicit native ports and real isolated SQLite. The full
main suite has 1,263 passing tests. Native runtime/physical-device evidence remains separate; no commercial limits.

D-74 / QA-04: valued-asset consent now has 37 AT-81 application cases; App.Tests has 105 and the main suite 1,300.
The actual editor gate excludes repeated pending saves and preserves cancelled drafts. Local/emulator evidence and
signed Release handoff remain separate from physical phone/iOS acceptance. That slice was followed by QA-06 performance; see the canonical backlog for the current remaining work.

Current local quality baseline (D-111): main suite 1,535 passing, App.Tests 299. Repeat opens a prefilled monthly
Plan from a new transaction draft without posting money; see [runtime evidence](docs/quality/entry-repeat-draft.md). Date parts grow/reflow and large
balances retain their complete signed decimal/currency packet with a real horizontal viewport when needed.
Repeated Settings language choices preserve the form and stored data; retired navigation titles are detached.
Windows child titles grow above the retained body; Windows/Android Back descriptions follow live language choices.
The open Settings estimate retains unsaved text/period/currency across reload; see docs/quality/settings-estimate-draft.md.
See docs/quality/font-scaling-a11y03.md for the actual running-app evidence and complete signed phone-test APK.
Ready accessibility/performance follow-ups continue under D-69; unresolved owner/provider/physical-device/iOS
acceptance stays separate. Earlier test counts above describe their historical slices.

Repeated transaction filters reuse complete snapshot rows; fresh data/display changes invalidate the cache.
See [transaction runtime evidence](docs/quality/transaction-row-reuse.md) for measured warm performance and its limits.

All four Windows Insights destinations remain visible with growing captions; see [runtime evidence](docs/quality/insights-navigation.md).

Budget names and complete compact figures use separate rows; signed boundary formatting no longer overflows.
See [budget runtime evidence](docs/quality/budget-readouts.md) for independent native/suite/package checks.

All three budget period choices stay visible in growing rows; see [period runtime evidence](docs/quality/budget-periods.md).

Tag suggestion captions grow with text and retain raw selection identity; normal Release verifies unsaved selection
and cancellation. See docs/quality/tag-suggestions.md for complete D-89 APK evidence and the local PowerShell-host
startup limitation. Physical-device, iOS and product acceptance remain open.

The detail disclosure names its current action and preserves unsaved values. D-90 canonical scripts, normal Release
and installable APK evidence are in docs/quality/entry-details-disclosure.md; the older D-89 console-host failures
remain dated evidence. Physical-device, iOS and product acceptance remain open.

Both onboarding restore captions grow with large text. D-91 native draft/navigation checks and the installable APK
are recorded in docs/quality/onboarding-restore-actions.md; physical-device/provider acceptance remains independent.

Measured Home reload work and complete native-row/data checks: docs/quality/performance-home-snapshots.md.
Cold-start and physical-device performance remain separate gates.

Android Debug platform guard and native stage evidence: docs/quality/home-debug-platform-and-native-stages.md.

Complete Home customization and native persisted-action evidence: docs/quality/home-customization-readable.md.

Hidden Home account view evidence: docs/quality/home-hidden-account-views.md; full snapshots and shown actions remain.

Complete account description evidence: docs/quality/account-descriptions-readable.md; balances and row actions remain.

Complete debt entry action evidence: docs/quality/debt-entry-action-readable.md; draft and financial behavior remain.

Single readable modal header evidence: docs/quality/modal-headers-readable.md; existing form and financial behavior retained.

Complete visible plan validation evidence: docs/quality/plan-validation-visible.md; drafts and existing financial rules remain.

Complete transaction-field validation evidence: docs/quality/entry-validation-visible.md; drafts and existing financial rules remain.

Existing destination-fee retention in Simple: docs/quality/destination-fee-retention.md; successful native edits preserve financial fields and row identities.

Complete final-bill field feedback and action: docs/quality/settlement-feedback.md; native valid differences/refunds preserve original financial rows.

Due-item actions, input feedback and safe native continuations: docs/quality/occurrence-feedback.md.

Complete bulk selection now uses one fresh-snapshot membership index. Actual bound full-selection reload evidence and timing limits: [bulk-selection-reload.md](docs/quality/bulk-selection-reload.md).

Settings reads the exact complete-month history needed for its unchanged spending suggestion. Actual bound measurements and runtime/acceptance limits: [settings-suggestion-history.md](docs/quality/settings-suggestion-history.md).

Windows selected/popup picker captions grow with inherited native typography and retain complete names/44-unit targets.
Actual matrix, choice/draft/data preservation and complete signed APK evidence: [native-picker-captions.md](docs/quality/native-picker-captions.md).

Complete report-scope captions, reachable Clear targets and transaction results: [runtime evidence](docs/quality/transaction-scope-readable.md).

Independent saved-filter results after report navigation: [runtime evidence](docs/quality/saved-filter-report-scope.md).

Complete growing transaction category choices: [runtime evidence](docs/quality/entry-category-captions.md).
