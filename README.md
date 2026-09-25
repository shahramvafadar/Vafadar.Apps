# Vafadar Apps

[![CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/ci.yml/badge.svg)](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/ci.yml)
[![CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/codeql.yml/badge.svg)](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/codeql.yml)

A collection of personal productivity apps by **Shahram Vafadar**, built with .NET 10 and .NET MAUI for Android,
iOS and Windows (and the web where it makes sense). The apps are local-first, private by design, available in
English, Persian (فارسی) and German, and back up their data to the user's own Google Drive or OneDrive.

> **Source available, not open source.** The code is public for transparency and reference. All rights are
> reserved; see [LICENSE](LICENSE).

## Apps

| App | Description | Platforms | Status |
|---|---|---|---|
| [Finance](src/Apps/Finance/README.md) | Personal income and expense tracking with reports | Android · iOS · Windows | 🚧 In development |

## Shared libraries

| Library | Purpose |
|---|---|
| [Vafadar.Core](src/Libraries/Vafadar.Core/README.md) | Entity base, app environment and settings abstractions |
| [Vafadar.Localization](src/Libraries/Vafadar.Localization/README.md) | Runtime language switching, right-to-left, Gregorian/Persian calendar, shared strings |
| [Vafadar.Data](src/Libraries/Vafadar.Data/README.md) | EF Core SQLite databases, audit timestamps, database backups |
| [Vafadar.Backup](src/Libraries/Vafadar.Backup/README.md) | Encrypted backup packages, retention, validated restore |
| [Vafadar.Backup.GoogleDrive](src/Libraries/Vafadar.Backup.GoogleDrive/README.md) | Backups in the user's Google Drive |
| [Vafadar.Backup.OneDrive](src/Libraries/Vafadar.Backup.OneDrive/README.md) | Backups in the user's OneDrive |
| [Vafadar.Authentication](src/Libraries/Vafadar.Authentication/README.md) | Google / Microsoft sign-in abstractions |
| [Vafadar.Maui](src/Libraries/Vafadar.Maui/README.md) | MAUI bootstrap, XAML localization, RTL, Syncfusion setup, MVVM base |

## Repository layout

```text
├── docs/           Architecture, decisions (ADRs), guides, privacy
├── eng/            Build infrastructure
├── src/
│   ├── Libraries/  Shared libraries (Vafadar.*)
│   └── Apps/       One folder per app (Vafadar.<App>.Core / .Data / .App)
├── test/           Tests, mirroring src/
└── Vafadar.Apps.slnx + solution filters (*.slnf)
```

Details: [repository layout and naming](docs/architecture/repository-layout.md).

## Getting started

Requirements: .NET SDK 10.0.401+, .NET MAUI workloads, Android SDK (see [getting started](docs/guides/getting-started.md)).

```powershell
dotnet tool restore
copy Directory.Secrets.props.example Directory.Secrets.props   # optional: add your Syncfusion license key
dotnet build Vafadar.Apps.slnx
dotnet test --solution Vafadar.Apps.slnx
dotnet build src/Apps/Finance/Vafadar.Finance.App -t:Run -f net10.0-android
```

## Technology

.NET 10 (LTS) · C# 14 · .NET MAUI · CommunityToolkit.Mvvm · Syncfusion Essential Studio · EF Core + SQLite ·
xUnit v3 · GitHub Actions

## Documentation

| | |
|---|---|
| [Architecture overview](docs/architecture/overview.md) | Goals, building blocks, dependency rules |
| [Decisions (ADRs)](docs/adr/README.md) | Why things are the way they are |
| [Guides](docs/README.md#guides) | Building, testing, conventions, secrets, adding an app, releasing |
| [Privacy](docs/privacy/README.md) | Privacy principles, policy and per-app privacy matrix |
| [Roadmap](docs/roadmap.md) | What comes next |

## Feedback and support

Bug reports and ideas are welcome as [issues](../../issues). Security problems: see [SECURITY.md](SECURITY.md).
See [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

Copyright © 2026 Shahram Vafadar. All rights reserved. See [LICENSE](LICENSE).
Third-party components are used under their own licenses.
