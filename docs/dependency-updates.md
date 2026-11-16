# Open dependency updates

The automatic update proposals (Dependabot) that are open, what each one changes and why it is not merged yet. The
list is a snapshot; the pull requests themselves are the current state.

**State on 2026-11-16.** Of eight proposals, two were taken over on the owner's decision; six are open. Each of the
six changes what Zanance is built with, and Zanance's verification – its package list, its signed packages – is
dated against the versions in `Directory.Packages.props` and `global.json` as they are. A change there belongs to
the work on Zanance, which decides when its checks are repeated. Dastan needs none of the updates.

## Taken over

| # | Update | Changes | How |
|---|---|---|---|
| 6 | `actions/cache` 4 → 6 | `ci.yml`, `release-android.yml` | The same lines as the proposal, as a commit of the owner |
| 17 | `xunit.v3` 4.0.1 → 4.0.3 | package version (all test projects) | The same line as the proposal, as a commit of the owner; every test of `Vafadar.Tests.slnf` was run with it before |

The branches of the proposals were not merged: their commits are not the owner's, and the repository takes commits
of the owner only (`eng/git-hooks/pre-push`). The two pull requests stay open until Dependabot finds its update
already in `main` and closes them, or until they are closed by hand.

## Open

| # | Update | Changes | Checks of the pull request | Assessment |
|---|---|---|---|---|
| 14 | `FluentIcons.Common` 2.1.341 → 2.1.345 | package version (icons of the MAUI apps) | all passed | Patch. Changes the packages of the Zanance app |
| 15 | `Google.Apis.Auth` 1.77.0 → 1.78.0 | package version (`Vafadar.Zanance.Api`) | all passed | Minor. Changes the packages of the Zanance service |
| 20 | Syncfusion 35.1.37 → 35.1.46 (`Licensing`, `Pdf.Net.Core`) | package versions | all passed except "Isolated purchase authority" | Patch inside the licensed version 35. Changes the packages of the Zanance app and of `Vafadar.Documents` |
| 8 | `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 → 3.0.5 | package version | not looked at | **Major version** of the SQLite binding; the storage of Zanance is verified on the current one. Not without a decision of its own |
| 18 | .NET SDK 10.0.404 → 11.0.100 | `global.json` | not looked at | **Major version** of the SDK for the whole repository. Not without a decision of its own |
| 19 | "dotnet" group, 14 packages | package versions, `dotnet-tools.json` | not looked at | Mixes patches (EF Core, ASP.NET Core 10.0.15) with **major versions** (`Microsoft.Extensions.*`, `Microsoft.Maui.Graphics`, `System.Security.Cryptography.*` 11.0) on projects that target .NET 10. Not as it is |

## The job "Isolated purchase authority"

A job of the workflow `backend-identity.yml`, which runs when the package versions change. It failed on the proposals
17 and 20 and on a push to `main` that only added a package for the browser tests of Dastan, and it passed on the
older proposals 14 and 15. So it fails for a reason that none of these changes brought. What GitHub shows without
signing in is only that a step ended with exit code 1; the log was not read. It is a matter of the work on Zanance.

How the checks were read: the check runs of the head commit of each pull request, through the public interface of
GitHub. Logs were not read.
