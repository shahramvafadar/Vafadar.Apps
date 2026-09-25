# Vafadar Finance

Personal finance manager: record income and expenses, organize them into accounts and categories, and understand
where the money goes with reports. Local-first, multilingual (English, Persian, German) and backed up to the user's
own Google Drive or OneDrive.

| | |
|---|---|
| App id | `pro.vafadar.finance` (permanent) |
| Platforms | Android (first), iOS, Windows |
| Status | 🚧 Skeleton: navigation, settings (language, calendar), database and backup wiring. Features follow the [requirements](docs/requirements.md). |
| Solution filter | [`Vafadar.Finance.slnf`](../../../Vafadar.Finance.slnf) |

## Projects

| Project | Contents |
|---|---|
| [`Vafadar.Finance.Core`](Vafadar.Finance.Core) | Domain model and use cases (platform independent) |
| [`Vafadar.Finance.Data`](Vafadar.Finance.Data) | `FinanceDbContext`, entity configurations, EF Core migrations |
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

Migrations are applied automatically when the app starts.

## Documentation

* [Design documentation](docs/README.md): assessment, domain, UX, phase-1 plan, backlog, privacy matrix, test plan,
  release checklist
* [Requirements](docs/requirements.md)
* [Changelog](CHANGELOG.md)
* Shared concepts: [architecture](../../../docs/architecture/overview.md), [localization](../../../docs/architecture/localization.md),
  [data and backup](../../../docs/architecture/data-and-backup.md), [privacy matrix](../../../docs/privacy/privacy-matrix.md)
