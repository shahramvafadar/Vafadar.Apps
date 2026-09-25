# 0008. xUnit v3 on Microsoft.Testing.Platform

- Status: Accepted
- Date: 2026-09-25

## Context

The .NET 10 SDK supports the new Microsoft.Testing.Platform (MTP) for `dotnet test`; xUnit v3 (4.x) runs natively on
it. The older VSTest runner is being phased out for these frameworks.

## Decision

* Test framework: **xUnit v3** with its built-in assertions (no commercial assertion libraries).
* Runner: **Microsoft.Testing.Platform**, enabled in `global.json`; test projects are executables.
* Coverage and TRX reports through the Microsoft MTP extensions (`--coverage`, `--report-trx`).
* One test project per production project, `test/` mirrors `src/`; shared helpers in `Vafadar.Testing`.
* MAUI UI is not unit-tested; view-independent logic belongs in `Core`/`Data`/libraries where it can be tested.

## Consequences

* `dotnet test --solution <sln|slnf>` is the command; Visual Studio Test Explorer supports MTP.
* Tests can also be run directly (`dotnet run --project test/...`), which helps debugging.
* UI automation (e.g. Appium) can be added later as a separate test project type if needed.
