# 0004. Core / Data / App projects per app

- Status: Accepted
- Date: 2026-09-25

## Context

A full Clean Architecture split (Domain, Application, Infrastructure, Presentation) is heavy for small personal
apps, while a single MAUI project makes business logic hard to test and impossible to reuse in a web version.

## Decision

Every app has three projects:

* `Vafadar.<App>.Core` – domain model and use cases, `net10.0`, no infrastructure dependencies.
* `Vafadar.<App>.Data` – EF Core SQLite persistence and migrations, `net10.0`.
* `Vafadar.<App>.App` – the MAUI app (UI, platform code, composition root).

Web (`.Web`), API (`.Api`) and shared DTO (`.Contracts`) projects are added only when needed.

## Consequences

* Business rules and persistence are tested with plain unit tests on any OS, without emulators.
* A web version or API can reuse `Core` directly.
* The split can be refined later (e.g. separate `Application` project) if an app grows large.
