# 0002. .NET 10 LTS and central build configuration

- Status: Accepted
- Date: 2026-09-25

## Context

Projects should use the latest supported .NET, and dozens of projects must stay consistent (target frameworks,
language settings, analyzers, package versions) without copying settings into every project file.

## Decision

* **.NET 10 (LTS)**, SDK pinned in `global.json` (`rollForward: latestFeature`). Moving to the next LTS (.NET 12) is
  a single change in `Directory.Build.props` and `global.json`.
* **`Directory.Build.props`** defines shared properties: `VafadarTargetFramework` (`net10.0`),
  `VafadarMauiTargetFrameworks` (Android, iOS except on Linux, Windows on Windows), nullable, implicit usings,
  analyzers, code style enforcement, warnings as errors in CI.
* **`eng/Maui.props`** holds settings common to all MAUI projects (minimum OS versions, XAML source generation).
* **Central Package Management** (`Directory.Packages.props`): every package version is declared once.
* The **`.slnx`** solution format (default since .NET 10).
* The MAUI target platforms can be narrowed per build with `-p:VafadarMauiTargetFrameworks=net10.0-android`.

## Consequences

* Project files stay short and only contain what is specific to the project.
* Upgrading a package or framework is a one-line change, automated by Dependabot.
* Contributors need the SDK version from `global.json` (or a newer feature band).
