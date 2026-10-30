# Documentation

## Current status and entry points

Refreshed 29 October 2026. Start with the [repository README](../README.md) for the technology map and
[Zanance README](../src/Apps/Zanance/README.md) for product scope. The
[canonical commercial backlog](../src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/04-backlog.md)
and [specification](../src/Apps/Zanance/docs/spec/Zanance-Product-Specification.md) own current implementation
and acceptance status. The roadmap describes broader phases; dated quality reports retain historical evidence.
Unfinished work, including protected timed cohorts, is not marked complete by this documentation refresh.

The optional [self-hosted service guide](../eng/hosting/README.md) covers existing operational tooling;
its presence does not establish public hosting, genuine provider acceptance or release readiness.

## Architecture

* [Overview](architecture/overview.md) – goals, building blocks, dependency rules, technology stack
* [Repository layout and naming](architecture/repository-layout.md)
* [Localization, right-to-left and calendars](architecture/localization.md)
* [Data and backup](architecture/data-and-backup.md)
* [Authentication](architecture/authentication.md)
* [Web versions and shared data](architecture/web-and-shared-data.md)
* [Monetization](architecture/monetization.md)
* [Architecture decision records](adr/README.md)

## Guides

* [Getting started](guides/getting-started.md)
* [Git setup and identity](guides/git-setup.md)
* [Building](guides/building.md)
* [Testing](guides/testing.md)
* [Coding conventions](guides/coding-conventions.md)
* [Secrets and configuration](guides/secrets-and-configuration.md)
* [Adding a new app](guides/adding-a-new-app.md)
* [Release and publishing](guides/release-and-publishing.md)

## Privacy

* [Approach](privacy/README.md)
* [Privacy policy (draft)](privacy/privacy-policy.md)
* [Privacy matrix](privacy/privacy-matrix.md)

## Planning

* [Roadmap](roadmap.md)

App-specific documentation lives with each app, e.g. [`src/Apps/Zanance`](../src/Apps/Zanance/README.md).

## D-333 Android notification-only storage

[Cold protected snoozes](../src/Apps/Zanance/docs/quality/cold-protected-reminder-startup.md) records the
source regression, original native receiver action, separate persisted metadata, package and remaining
startup/device boundaries. The independent maintained caller is under eng/benchmarks/Zanance.AndroidReminders.

The original six-kind resource workspace now includes source-bound read/revalidation, accepted receipts and exact native parent returns: [D-659 evidence](../src/Apps/Zanance/docs/quality/original-active-resource-workspace.md). This source/native qualification does not close physical adoption or release gates.
