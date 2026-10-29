# Vafadar Apps

[![CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/ci.yml/badge.svg)](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/ci.yml)
[![CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/codeql.yml/badge.svg)](https://github.com/shahramvafadar/Vafadar.Apps/actions/workflows/codeql.yml)

**Application and data**

![C# 14](https://img.shields.io/badge/C%23%2014-239120?logo=csharp&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?logo=dotnet&logoColor=white)
![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-512BD4?logo=dotnet&logoColor=white)
![XAML](https://img.shields.io/badge/XAML-0C54C2)
![MVVM](https://img.shields.io/badge/MVVM-6554C0)
![CommunityToolkit.Mvvm](https://img.shields.io/badge/CommunityToolkit.Mvvm-512BD4)
![Syncfusion](https://img.shields.io/badge/Syncfusion-FF6D00)
![Fluent Icons](https://img.shields.io/badge/Fluent%20Icons-0078D4)
![EF Core](https://img.shields.io/badge/EF%20Core-512BD4?logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-003B57?logo=sqlite&logoColor=white)
![SQLCipher](https://img.shields.io/badge/SQLCipher-003B57)
![Brotli](https://img.shields.io/badge/Brotli-455A64)

**Platforms, documents and integrations**

![Android](https://img.shields.io/badge/Android-3DDC84?logo=android&logoColor=white)
![iOS target](https://img.shields.io/badge/iOS%20target-555555?logo=apple&logoColor=white)
![Windows development](https://img.shields.io/badge/Windows%20development-0078D4)
![ML Kit OCR](https://img.shields.io/badge/ML%20Kit%20OCR-4285F4?logo=google&logoColor=white)
![PDF](https://img.shields.io/badge/PDF-B30B1B)
![CSV](https://img.shields.io/badge/CSV-217346)
![Local notifications](https://img.shields.io/badge/Local%20notifications-00695C)
![Google Drive](https://img.shields.io/badge/Google%20Drive-4285F4?logo=googledrive&logoColor=white)
![OneDrive](https://img.shields.io/badge/OneDrive-0078D4)
![MSAL and OAuth PKCE](https://img.shields.io/badge/MSAL%20and%20OAuth%20PKCE-0078D4)
![ECB reference rates](https://img.shields.io/badge/ECB%20reference%20rates-003399)

**Optional service and operations**

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Identity](https://img.shields.io/badge/ASP.NET%20Identity-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)
![Docker Compose](https://img.shields.io/badge/Docker%20Compose-2496ED?logo=docker&logoColor=white)
![Caddy HTTPS](https://img.shields.io/badge/Caddy%20HTTPS-1F88C0?logo=caddy&logoColor=white)
![Cloudflare Tunnel path](https://img.shields.io/badge/Cloudflare%20Tunnel%20path-F38020?logo=cloudflare&logoColor=white)
![Google Play Billing](https://img.shields.io/badge/Google%20Play%20Billing-414141?logo=googleplay&logoColor=white)

**Engineering methods and checks**

![Local first](https://img.shields.io/badge/Local%20first-00695C)
![Offline capable](https://img.shields.io/badge/Offline%20capable-00695C)
![Layered architecture](https://img.shields.io/badge/Layered%20architecture-455A64)
![Dependency injection](https://img.shields.io/badge/Dependency%20injection-455A64)
![Transactional SQLite](https://img.shields.io/badge/Transactional%20SQLite-003B57)
![Additive migrations](https://img.shields.io/badge/Additive%20migrations-003B57)
![AES GCM](https://img.shields.io/badge/AES%20GCM-455A64)
![Six languages and RTL](https://img.shields.io/badge/Six%20languages%20and%20RTL-00796B)
![xUnit v3](https://img.shields.io/badge/xUnit%20v3-512BD4)
![Microsoft.Testing.Platform](https://img.shields.io/badge/Microsoft.Testing.Platform-512BD4)
![GitHub Actions](https://img.shields.io/badge/GitHub%20Actions-2088FF?logo=githubactions&logoColor=white)
![CodeQL](https://img.shields.io/badge/CodeQL-2088FF?logo=github&logoColor=white)
![Dependabot](https://img.shields.io/badge/Dependabot-025E8C?logo=dependabot&logoColor=white)
![Android AOT](https://img.shields.io/badge/Android%20AOT-3DDC84?logo=android&logoColor=white)
![PowerShell](https://img.shields.io/badge/PowerShell-5391FE)
![Python](https://img.shields.io/badge/Python-3776AB?logo=python&logoColor=white)
![Node.js tooling](https://img.shields.io/badge/Node.js%20tooling-5FA04E?logo=nodedotjs&logoColor=white)

These badges identify technologies, methods and available integration paths in the repository; they do not
certify platform qualification, configured provider accounts, public hosting or store availability.

A collection of personal productivity apps by **Shahram Vafadar**, built with .NET 10 and .NET MAUI for Android,
iOS and Windows (and the web where it makes sense). The apps are local-first, private by design, available in
English, Persian (فارسی), German, Spanish, French and Italian, and keep the user's data on the device, with encrypted backup files the user
controls.

> **Source available, not open source.** The code is public for transparency and reference. All rights are
> reserved; see [LICENSE](LICENSE).

## Apps

| App | Description | Platforms | Status |
|---|---|---|---|
| [Zanance](src/Apps/Zanance/README.md) | Personal finance manager: income, expenses, plans, budgets and reports | Android first · iOS target · Windows development | Active implementation and qualification; not released |

## Current delivery status

Documentation refreshed on **29 October 2026**. Android is the current delivery priority; iOS qualification
requires the separate Mac environment, and Windows remains a development target. Local engineering evidence,
physical-device acceptance, genuine OAuth/store integration and public release are separate gates.

Zanance includes accounts, transactions, plans, goals, holdings, budget methods, reports, forecast scenarios,
reconciliation, receipt/PDF reading, CSV review, local profiles, app access controls and backup workflows.
The optional ASP.NET Core service implements identity, protected household synchronization and billing-related
infrastructure; its existence does not imply a configured public service or live commercial activation.

Recent completed source batches cover original account details, Plans, templates, Transactions, period review
and categorization-rule/category-management ownership. Independent category verification is complete; physical
adoption remains separate.
The protected physical reboot/reminder qualification D-607 remains pending its original deadlines.
Performance, general stability, remaining accessibility, provider/store and platform release gates remain open.

Use the [canonical commercial backlog](src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/04-backlog.md)
and [specification status](src/Apps/Zanance/docs/spec/Zanance-Product-Specification.md)
for detailed evidence and remaining work; historical test totals are not current completion percentages.

## Shared libraries

| Library | Purpose |
|---|---|
| [Vafadar.Core](src/Libraries/Vafadar.Core/README.md) | Entity base, app environment and settings abstractions |
| [Vafadar.Localization](src/Libraries/Vafadar.Localization/README.md) | Runtime language switching, right-to-left, Gregorian, Persian and lunar Hijri calendars, shared strings |
| [Vafadar.Cryptography](src/Libraries/Vafadar.Cryptography) | Shared cryptographic primitives for protected app/service data |
| [Vafadar.Data](src/Libraries/Vafadar.Data/README.md) | EF Core SQLite databases, audit timestamps, database backups |
| [Vafadar.Backup](src/Libraries/Vafadar.Backup/README.md) | Encrypted backup packages, retention, validated restore |
| [Vafadar.Backup.GoogleDrive](src/Libraries/Vafadar.Backup.GoogleDrive/README.md) | Backups in the user's Google Drive |
| [Vafadar.Backup.OneDrive](src/Libraries/Vafadar.Backup.OneDrive/README.md) | Backups in the user's OneDrive |
| [Vafadar.Documents](src/Libraries/Vafadar.Documents/README.md) | Text rows from word boxes (LTR and RTL), PDF text layer |
| [Vafadar.Documents.Maui](src/Libraries/Vafadar.Documents.Maui/README.md) | Reading photos and PDFs on the device with the system engines |
| [Vafadar.Authentication](src/Libraries/Vafadar.Authentication/README.md) | Google / Microsoft sign-in abstractions |
| [Vafadar.Authentication.Maui](src/Libraries/Vafadar.Authentication.Maui/README.md) | Microsoft (MSAL) and Google (Play services on Android, browser + PKCE on iOS and Windows) sign-in for cloud backups |
| [Vafadar.Maui](src/Libraries/Vafadar.Maui/README.md) | MAUI bootstrap, XAML localization, RTL, Syncfusion setup, MVVM base |

## Repository layout

```text
├── branding/       Brand masters and the scripts that generate app icons
├── docs/           Architecture, decisions (ADRs), guides, privacy
├── eng/            Build, native dependencies, notifications, diagnostics and optional hosting
├── src/
│   ├── Libraries/  Shared libraries (Vafadar.*)
│   └── Apps/       One folder per app (Vafadar.<App>.Core / .Data / .App, Zanance also .Reports)
├── test/           Tests, mirroring src/
└── Vafadar.Apps.slnx + solution filters (*.slnf)
```

Details: [repository layout and naming](docs/architecture/repository-layout.md).

## Getting started

Requirements: .NET SDK 10.0.401+, .NET MAUI workloads, Android SDK (see [getting started](docs/guides/getting-started.md)).

```powershell
dotnet tool restore
copy Directory.Secrets.props.example Directory.Secrets.props   # optional: add your Syncfusion license key
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-android -p:ContinuousIntegrationBuild=true
dotnet test --solution Vafadar.Tests.slnf
dotnet clean Vafadar.Tests.slnf
dotnet build src/Apps/Zanance/Vafadar.Zanance.App -t:Run -f net10.0-android
```

## Technology and implementation map

| Area | Repository implementation / authority |
|---|---|
| Build and versions | [SDK selection](global.json), [central packages](Directory.Packages.props), [MAUI targets](eng/Maui.props); C# 14, .NET 10, MAUI 10.0.120 |
| Native UI | Compiled XAML bindings, MVVM, dependency injection, semantic theme resources, Syncfusion controls and Fluent icons |
| Financial model | Integer minor units plus ISO currency, DateOnly, injected TimeProvider; domain code in [Zanance.Core](src/Apps/Zanance/Vafadar.Zanance.Core) |
| Persistence | EF Core, real SQLite transactions, additive migrations/compiled models, protected SQLCipher hosts and validated derived ledger blocks; [data architecture](docs/architecture/data-and-backup.md) |
| Privacy and recovery | Local profiles, device-bound access controls, AES-GCM backup envelopes, optional portable password protection, automatic device-key copies; [privacy matrix](docs/privacy/privacy-matrix.md) |
| Languages and regional settings | Six translated languages, independent formatting/digits/calendars, RTL, Gregorian/Persian/Umm al-Qura; [localization](docs/architecture/localization.md) |
| Documents and exchange | On-device Android ML Kit OCR, platform-specific readers, selectable PDF text, Syncfusion PDF reports, reviewed CSV import/export and dated ECB reference rates; source/language limitations remain explicit |
| Connected backup | Google Drive and OneDrive adapters, Google authorization, MSAL and browser PKCE; genuine client/account acceptance remains separate |
| Optional backend | [ASP.NET Core API](src/Apps/Zanance/Vafadar.Zanance.Api), Identity, PostgreSQL/Npgsql, protected household data and provider verification |
| Service operations | [Owned Docker Compose stack](eng/hosting/README.md), Caddy HTTPS, snapshot/restore tools and the approved Cloudflare Tunnel hosting path; public/provider acceptance is separate |
| Android delivery | Signed offline APK preparation, profiled AOT, native dependency/source guards and static ELF/ZIP alignment; runtime/device qualification is recorded independently |
| Verification | xUnit v3 on Microsoft.Testing.Platform, real SQLite/filesystem cases, focused native workflows, source-preservation checks, TRX/coverage; [testing guide](docs/guides/testing.md) |
| Automation and maintenance | GitHub Actions CI, CodeQL, Dependabot, Android release, backend identity, native-cipher and self-hosted-service workflows; PowerShell/Python and branding Node.js scripts |

Only implemented repository components are listed as present. A platform target, integration adapter or static
package check does not establish end-to-end acceptance. No aggregate passing-test count is claimed here.

## Documentation

| | |
|---|---|
| [Architecture overview](docs/architecture/overview.md) | Goals, building blocks, dependency rules |
| [Decisions (ADRs)](docs/adr/README.md) | Why things are the way they are |
| [Guides](docs/README.md#guides) | Building, testing, conventions, secrets, adding an app, releasing |
| [Privacy](docs/privacy/README.md) | Privacy principles, policy and per-app privacy matrix |
| [Canonical backlog](src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/04-backlog.md) | Current delivery work and acceptance boundaries |
| [Roadmap](docs/roadmap.md) | High-level phases and historical milestones |
| [Zanance documentation](src/Apps/Zanance/docs/README.md) | Product specification, decisions, quality evidence and release checklist |
| [Self-hosted service](eng/hosting/README.md) | Optional service operations and protected recovery |

## Feedback and support

Bug reports and ideas are welcome as [issues](https://github.com/shahramvafadar/Vafadar.Apps/issues). Security problems: see [SECURITY.md](SECURITY.md).
See [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request, and [AGENTS.md](AGENTS.md) for the complete
working rules of this repository.

## License

Copyright © 2026 Shahram Vafadar. All rights reserved. See [LICENSE](LICENSE).
Third-party components are used under their own licenses.
