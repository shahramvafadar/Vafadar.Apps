# Vafadar Finance

Personal finance manager: record income and expenses, organize them into accounts and categories, and understand
where the money goes with reports. Local-first and offline, multilingual (English, Persian, German, with Gregorian or Persian calendar) and backed up
with encrypted files the user keeps wherever they like (Google Drive, OneDrive, e-mail, computer).

| | |
|---|---|
| App id | `pro.vafadar.finance` (permanent) |
| Platforms | Android (first), iOS, Windows |
| Status | 🚧 Phase 1 feature-complete except cloud backup (needs Google/Microsoft sign-in): accounts, entries, transfers, refunds, categories, plans with Gregorian/Persian recurrence and automatic posting, Home dashboard, budget, reports, forecast, reminders, manual exchange rates, CSV import/export, encrypted backup files, Simple/Advanced, app lock. Status per slice: [phase 1 plan](docs/04-phase-1-plan.md). |
| Solution filter | [`Vafadar.Finance.slnf`](../../../Vafadar.Finance.slnf) |

## Projects

| Project | Contents |
|---|---|
| [`Vafadar.Finance.Core`](Vafadar.Finance.Core) | Domain model and calculations (platform independent): ledger, plans and recurrence, budget, reports, forecast, reminders, rates, CSV |
| [`Vafadar.Finance.Data`](Vafadar.Finance.Data) | `FinanceDbContext`, migrations, stores, automatic posting, backup summary |
| [`Vafadar.Finance.App`](Vafadar.Finance.App) | .NET MAUI app: pages, view models, platform code |

Tests: [`test/Apps/Finance`](../../../test/Apps/Finance).

## Run

```powershell
dotnet build src/Apps/Finance/Vafadar.Finance.App -t:Run -f net10.0-android                 # emulator / device
dotnet build src/Apps/Finance/Vafadar.Finance.App -t:Run -f net10.0-windows10.0.19041.0     # Windows
```

## Database migrations

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project src/Apps/Finance/Vafadar.Finance.Data
```

Migrations are applied automatically when the app starts. They must only add to the schema (AT-60, tested).

## Reviewing screens without a device

Debug builds can walk through every screen in English, German and Persian and save screenshots:

```powershell
$env:VAFADAR_SNAPSHOTS = "C:\temp\snapshots"; $env:VAFADAR_SNAPSHOT_LANGUAGES = "en,fa"
# start the Windows Debug build with an empty database; the app seeds sample data, saves PNGs and closes
```

## Documentation

* [Design documentation](docs/README.md): assessment, domain, UX, phase-1 plan, backlog, privacy matrix, test plan,
  release checklist
* [Requirements](docs/requirements.md)
* [Changelog](CHANGELOG.md)
* Shared concepts: [architecture](../../../docs/architecture/overview.md), [localization](../../../docs/architecture/localization.md),
  [data and backup](../../../docs/architecture/data-and-backup.md), [privacy matrix](../../../docs/privacy/privacy-matrix.md)
