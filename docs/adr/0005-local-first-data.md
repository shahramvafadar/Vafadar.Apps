# 0005. Local-first data with SQLite and EF Core

- Status: Accepted
- Date: 2026-09-25

## Context

Apps must work offline, cost nothing to run and keep personal data private. Most do not need a server.

## Decision

* Each app stores its data in a **SQLite** database in private app storage, accessed with **EF Core 10**.
* `LocalDbContext` (Vafadar.Data) applies SQLite conventions: `DateTimeOffset` as UTC ticks; money as `long` minor
  units by convention; GUID v7 identifiers from the `Entity` base class; automatic `CreatedAt` / `UpdatedAt`.
* Schema changes only through **EF Core migrations**, applied at startup.
* Contexts are created from `IDbContextFactory` (short-lived units of work).

## Consequences

* No backend, no hosting cost, no developer access to user data.
* Data safety depends on backups ([ADR 0006](0006-backup-to-user-cloud-storage.md)).
* EF Core on mobile adds some startup cost and app size; acceptable for these apps, and trimming works in Release
  builds without warnings.
* Identifiers and timestamps are already suitable for a later sync ([web-and-shared-data.md](../architecture/web-and-shared-data.md)).
