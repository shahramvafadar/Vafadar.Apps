# Testing

## Running tests

| Task | Command |
|---|---|
| All tests | `dotnet test --solution Vafadar.Apps.slnx` |
| Without building MAUI projects | `dotnet test --solution Vafadar.Tests.slnf` |
| One project | `dotnet test --project test/Libraries/Vafadar.Backup.Tests` |
| Filter by test name | `dotnet test --project test/Libraries/Vafadar.Backup.Tests --filter-method "*Encrypted*"` |
| Code coverage | `dotnet test --solution Vafadar.Tests.slnf --coverage` |
| TRX report | `dotnet test --solution Vafadar.Tests.slnf --report-trx` |
| Debug a test project directly | `dotnet run --project test/Libraries/Vafadar.Backup.Tests` |

Tests use **xUnit v3** on **Microsoft.Testing.Platform** (enabled in `global.json`). Visual Studio's Test Explorer
discovers and runs them as usual.

## Structure

* One test project per production project: `test/` mirrors `src/` (`Vafadar.Backup` → `Vafadar.Backup.Tests`).
* Every production assembly exposes its `internal` members to its test project (`Directory.Build.targets`).
* Shared helpers live in `test/Shared/Vafadar.Testing`:
  `TemporaryDirectory`, `FakeHttpMessageHandler`, `StaticAccessTokenProvider`, `RepositoryPaths`.
* Time-dependent code gets a `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`).

## Conventions

* Test names are sentences with underscores: `Wrong_password_is_rejected`.
* Arrange / act / assert, separated by blank lines; one behavior per test.
* Pass `TestContext.Current.CancellationToken` to async APIs (xUnit analyzers require it).
* Use real SQLite files (in a `TemporaryDirectory`) for data tests rather than mocks – SQLite is fast and behaves
  like production.
* Tests that change process-wide state (cultures) disable parallelization for their assembly
  (see `Vafadar.Localization.Tests/AssemblyInfo.cs`).
* External services (Google Drive, Microsoft Graph) are tested against `FakeHttpMessageHandler`; real accounts are
  never used in automated tests.

## Repository-wide checks

Some tests protect the repository as a whole:

* `ResourceCompletenessTests` – every translated `.resx` in `src/` has the same keys and placeholders as the neutral one.
* `FinanceAppTests` – the Finance app id in code matches `ApplicationId` in the app project (the id is permanent).
* `FinanceDataTests.Startup_migration_creates_the_database` – the app's startup migration works, which also fails
  when the model changed without adding a migration.

## What is not unit-tested

MAUI pages and platform code. Keep logic out of code-behind and view models thin, so that almost everything worth
testing lives in `Core`, `Data` or a library. UI automation can be added later if needed.
