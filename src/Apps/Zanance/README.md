# Zanance

Personal finance manager: record income and expenses, organize them into accounts and categories, and understand
where the money goes with reports. Local-first and offline, multilingual (English, Persian, German, with Gregorian or Persian calendar) and backed up
with encrypted files the user keeps wherever they like, or – in builds with cloud backup (D-35) – in the user's own Google Drive or OneDrive.

| | |
|---|---|
| Product name | **Zanance** – shown untranslated in the app, stores and notifications; code and folders use the same name (D-24) |
| App id | `pro.vafadar.zanance` – Android package and iOS bundle id (permanent once published) |
| Platforms | Android (first), iOS, Windows |
| Status | 🚧 Phase 1 feature-complete; cloud backup (OneDrive and Google Drive on Android, iOS and Windows) implemented and waiting for OAuth clients and a device test (D-35, D-50): accounts, entries, transfers, refunds, categories, plans with Gregorian/Persian recurrence and automatic posting, Home dashboard, budget, reports, forecast, reminders, manual exchange rates, CSV import/export, encrypted backup files, Simple/Advanced, app lock. Phase 2A in progress: goals, rollover and envelope budgets, splits, partial payments, contracts, reimbursements, tags, rules, loans with a repayment estimate, assets, receipts, weekday rules, display units such as the toman, forecast scenarios, saved filters, PDF reports, a customizable Home, a dark theme, flex, weekly and two-week budgets with pay-cycle months and limit suggestions, public holidays, a quick add widget, receipt and PDF invoice reading, and local profiles. Status: [phase 1 plan](docs/04-phase-1-plan.md), [phase 2 backlog](docs/05-phase-2-backlog.md). |
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
