# Coding conventions

Most formatting and naming rules are enforced by `.editorconfig` and the build. This page covers what tools cannot
check.

## Language

* **All code, comments, commit messages, issues and documentation are in English.** User-facing text is never
  hard-coded; it comes from resources in all supported languages.
* XML documentation comments on public types and members of shared libraries.
* Comments explain *why*, not *what*.

## C#

* File-scoped namespaces matching the folder; one public type per file (small related records may share a file).
* `_camelCase` private fields, `PascalCase` constants and static readonly fields, `I` prefix for interfaces.
* `sealed` by default for classes not designed for inheritance.
* Nullable reference types are on; do not suppress warnings with `!` without a comment.
* Validate public arguments (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`).
* Prefer records for immutable data, primary constructors for simple services.
* Async all the way: `async`/`await`, `CancellationToken` as the last parameter of async APIs; no `.Result` /
  `.Wait()` (use synchronous APIs where code must be synchronous, e.g. `MigrateLocalDatabase`).
* `ConfigureAwait(false)` is not used (analyzer disabled): app code needs the UI context, and library code is small.

## Time, culture and identifiers

* Never use `DateTime.Now` / `DateTimeOffset.UtcNow` in logic: inject `TimeProvider` (tests use `FakeTimeProvider`).
* Store moments as UTC `DateTimeOffset`, calendar dates as `DateOnly`, money as `long` minor units + currency code.
* Persist and exchange data with `CultureInfo.InvariantCulture`; format for display with `IDateFormatter` / the
  current culture.
* New entities get GUID v7 identifiers from the `Entity` base class.

## Dependency injection

| Kind | Lifetime |
|---|---|
| Stateless services, settings, localization, backup service | Singleton |
| Pages and view models | Transient |
| `DbContext` | Never injected; create short-lived instances with `IDbContextFactory<T>` |
| HTTP clients | `IHttpClientFactory` (named or typed clients) |

Register an app's services in extension methods (`AddFinanceData()`) close to the implementation, and compose them
in `MauiProgram`.

## MAUI and MVVM

* One folder per feature under `Features/`: `XxxPage.xaml`, `XxxPage.xaml.cs`, `XxxViewModel.cs`.
* Every page sets `x:DataType` (compiled bindings). No logic in code-behind beyond wiring (e.g. `OnAppearing`).
* View models derive from `ViewModelBase`, use `[ObservableProperty]` on **partial properties** and `[RelayCommand]`.
* Text via `{v:Translate Key}` or `Translator`; dates via `IDateFormatter`.
* Colors and styles from `Resources/Styles`; no hard-coded colors in pages. Support light and dark themes
  (`AppThemeBinding`).
* Use layouts that work right-to-left (see [localization](../architecture/localization.md#right-to-left-layout-rules)).
* Syncfusion controls: add the specific `Syncfusion.Maui.*` package to the app (version from `Directory.Packages.props`).

## Errors and logging

* Throw specific exceptions (`BackupException` with a `BackupError`, `AuthenticationRequiredException`) that the UI
  can map to translated messages; do not show exception messages to users.
* Catch exceptions at the UI boundary (commands) and show a friendly, translated message; log the details with
  `ILogger<T>`.
* Never log personal data (amounts, notes, account names) or tokens.

## Git workflow

* `main` is always releasable; work on short-lived branches: `feature/<app>-<topic>`, `fix/<app>-<topic>`,
  `chore/<topic>`, `docs/<topic>`.
* Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):
  `feat(finance): add monthly report`, `fix(backup): retry OneDrive upload`, `docs: update release guide`.
  Scope = app or library name.
* Pull requests (even when working alone) so that CI runs before merging; squash-merge into `main`.
* Release tags: `<app>/v<version>`, e.g. `finance/v1.0.0`.
