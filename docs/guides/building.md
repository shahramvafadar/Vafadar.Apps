# Building

## Common commands

| Task | Command |
|---|---|
| Build everything | `dotnet build Vafadar.Apps.slnx` |
| Build one product | `dotnet build Vafadar.Finance.slnf` |
| Build without MAUI (no workloads needed) | `dotnet build Vafadar.Tests.slnf` |
| MAUI projects for one platform only | `dotnet build Vafadar.Apps.slnx -p:VafadarMauiTargetFrameworks=net10.0-android` |
| Release build of an app for Android | `dotnet build src/Apps/Finance/Vafadar.Finance.App -c Release -f net10.0-android` |
| Build as CI does (warnings are errors) | add `-p:ContinuousIntegrationBuild=true` |
| Restore local tools | `dotnet tool restore` |

## Target frameworks

* Plain libraries, tests and (future) web projects: `$(VafadarTargetFramework)` = `net10.0`.
* MAUI projects: `$(VafadarMauiTargetFrameworks)`, which depends on the build machine:

  | Build OS | MAUI targets |
  |---|---|
  | Windows | `net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0` |
  | macOS | `net10.0-android`, `net10.0-ios` |
  | Linux | `net10.0-android` |

  Passing `-p:VafadarMauiTargetFrameworks=...` on the command line overrides this for all projects – useful for
  faster local builds and used by CI.

## Warnings

The code builds without warnings. Locally warnings are shown; in CI (`ContinuousIntegrationBuild=true`, set
automatically on GitHub Actions) they are errors. Code style rules marked `warning` in `.editorconfig` are enforced
during the build.

## Continuous integration

`.github/workflows/ci.yml` runs on every push to `main` and on pull requests:

| Job | Runner | What it does |
|---|---|---|
| Libraries and tests | Ubuntu | Builds `Vafadar.Tests.slnf` (Release) and runs all tests with coverage and TRX results |
| Android build | Ubuntu | Installs the Android workload and builds the whole solution for Android (Release, trimmed) |
| Windows build | Windows | Builds the whole solution for Windows (Release) |
| iOS build | macOS | Only on manual runs with "ios" checked; simulator build, no signing |

Other workflows:

* `codeql.yml` – security analysis of C# and workflow files (weekly and on changes).
* `release-android.yml` – manual: builds a signed `.aab` for Google Play (see [release guide](release-and-publishing.md)).
* Dependabot (`.github/dependabot.yml`) opens weekly update pull requests for NuGet packages, the SDK and actions.
