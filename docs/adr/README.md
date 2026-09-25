# Architecture decision records

An ADR records one significant decision: the context, the decision and its consequences. ADRs are not edited after
they are accepted; a later decision that changes one **supersedes** it with a new ADR.

| # | Decision | Status |
|---|---|---|
| [0001](0001-monorepo-with-shared-libraries.md) | One repository, one solution, shared libraries, solution filters | Accepted |
| [0002](0002-dotnet-10-and-central-build-configuration.md) | .NET 10 LTS and central build configuration | Accepted |
| [0003](0003-ui-technology.md) | .NET MAUI XAML + MVVM by default; Blazor Hybrid when a web version is planned | Accepted |
| [0004](0004-project-structure-per-app.md) | Core / Data / App projects per app | Accepted |
| [0005](0005-local-first-data.md) | Local-first data with SQLite and EF Core | Accepted |
| [0006](0006-backup-to-user-cloud-storage.md) | Encrypted backups to the user's own Google Drive / OneDrive | Accepted |
| [0007](0007-localization.md) | Runtime-switchable localization; calendar independent of language | Accepted |
| [0008](0008-testing-with-xunit-v3-and-mtp.md) | xUnit v3 on Microsoft.Testing.Platform | Accepted |
| [0009](0009-secrets-and-source-available-license.md) | Public, source-available repository; build-time secrets | Accepted |

## Template

```markdown
# NNNN. Title

- Status: Proposed | Accepted | Superseded by NNNN
- Date: YYYY-MM-DD

## Context
What problem or force requires a decision?

## Decision
What we decided.

## Consequences
What becomes easier or harder; follow-up work.
```
