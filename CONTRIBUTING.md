# Contributing

Thank you for your interest! This is a personal, **source-available** project (see [LICENSE](LICENSE)), so the
contribution model is a bit different from an open-source project.

## Welcome

* **Bug reports** and **feature ideas** as [issues](../../issues) (use the templates).
* **Translation improvements** for Persian and German (and suggestions for new languages).
* Small, focused **pull requests** for bugs – please open an issue first so we can agree on the approach.

By submitting a contribution you agree to the contribution terms in the [LICENSE](LICENSE) (point 3).

## Please do not

* Include personal or financial data, backup files or screenshots with real data in issues.
* Report security vulnerabilities publicly – see [SECURITY.md](SECURITY.md).
* Open large pull requests without prior discussion.

## Development workflow

* Setup: [getting started](docs/guides/getting-started.md).
* Conventions: [coding conventions](docs/guides/coding-conventions.md) – English only in code and docs, all UI
  strings in every language, tests for new behavior, no warnings.
* Branches: `feature/<app>-<topic>`, `fix/<app>-<topic>`, `docs/<topic>`, `chore/<topic>`.
* Commits: [Conventional Commits](https://www.conventionalcommits.org/), e.g. `fix(finance): correct monthly total`.
* Before opening a pull request:

  ```powershell
  dotnet build Vafadar.Apps.slnx
  dotnet test --solution Vafadar.Apps.slnx
  ```

* Fill in the pull request checklist.
