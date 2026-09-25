# 0009. Public, source-available repository; build-time secrets

- Status: Accepted
- Date: 2026-09-25

## Context

The code is published on GitHub for visibility, but it is not open source. The apps need values that must not be
in a public repository: the Syncfusion license key, OAuth client configuration, Android signing keys.

## Decision

* The repository is public with an **"All rights reserved"** [LICENSE](../../LICENSE): viewing (and forking on GitHub,
  as the GitHub terms allow) is permitted; any other use needs permission.
* Secrets are **injected at build time**: an app declares `<AppSecret Include="Name" Value="$(Name)" />`, and
  `eng/AppSecrets.targets` generates an internal `AppSecrets` class. Values come from environment variables
  (GitHub Actions secrets) or the git-ignored `Directory.Secrets.props` locally.
* Signing keystores and certificates are never committed (`.gitignore`), and CI decodes them from secrets only in
  the release workflow's protected `production` environment.
* GitHub secret scanning and push protection are enabled on the repository.

## Consequences

* Builds without secrets (e.g. pull requests from forks) still succeed; Syncfusion then shows its license banner.
* Values compiled into an app can be extracted from the app package; this keeps them out of the source, not out of
  the binary. Nothing that must stay truly secret (e.g. a server key) may ever be compiled into an app.
* A leaked secret must be rotated (new Syncfusion key, new OAuth client), not just deleted from history.
