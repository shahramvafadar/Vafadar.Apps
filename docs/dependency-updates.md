# Open dependency updates

The automatic update proposals (Dependabot) that are open, what each one changes and why it is not merged yet. The
list is a snapshot; the pull requests themselves are the current state.

**State on 2026-11-16.** None of the eight was merged with the work on Dastan: each changes what Zanance is built
with or built by, and Zanance's verification – its package list, its signed packages, its release workflow – is
dated against the versions in `Directory.Packages.props`, `global.json` and the workflows as they are. A change
there belongs to the work on Zanance, which decides when its checks are repeated. Dastan needs none of the updates.

| # | Update | Changes | Checks of the pull request | Assessment |
|---|---|---|---|---|
| 6 | `actions/cache` 4 → 6 | `ci.yml`, `release-android.yml` | all passed | Small. Touches the release workflow of Zanance; merge with the next change of the workflows |
| 14 | `FluentIcons.Common` 2.1.341 → 2.1.345 | package version (icons of the MAUI apps) | all passed | Patch. Changes the packages of the Zanance app |
| 15 | `Google.Apis.Auth` 1.77.0 → 1.78.0 | package version (`Vafadar.Zanance.Api`) | all passed | Minor. Changes the packages of the Zanance service |
| 17 | `xunit.v3` 4.0.1 → 4.0.3 | package version (all test projects) | all passed except "Isolated purchase authority" | Patch of the test framework. The failed job is a service test of Zanance and fails on #20 as well, so the cause is probably not this update; to be looked at before merging |
| 20 | Syncfusion 35.1.37 → 35.1.46 (`Licensing`, `Pdf.Net.Core`) | package versions | all passed except "Isolated purchase authority" | Patch inside the licensed version 35. Changes the packages of the Zanance app and of `Vafadar.Documents` |
| 8 | `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 → 3.0.5 | package version | not looked at | **Major version** of the SQLite binding; the storage of Zanance is verified on the current one. Not without a decision of its own |
| 18 | .NET SDK 10.0.404 → 11.0.100 | `global.json` | not looked at | **Major version** of the SDK for the whole repository. Not without a decision of its own |
| 19 | "dotnet" group, 14 packages | package versions, `dotnet-tools.json` | not looked at | Mixes patches (EF Core, ASP.NET Core 10.0.15) with **major versions** (`Microsoft.Extensions.*`, `Microsoft.Maui.Graphics`, `System.Security.Cryptography.*` 11.0) on projects that target .NET 10. Not as it is |

How the checks were read: the check runs of the head commit of each pull request, through the public interface of
GitHub, once. Logs of the failed job were not read.
