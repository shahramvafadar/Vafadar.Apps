# Web versions and shared data

Most apps are mobile-only and local-first. Some ideas will get a web version, and some may need the same data on
several devices. This document describes how those cases fit into the architecture. Nothing here is implemented
yet; it guides the first app that needs it.

## Options

| Scenario | Approach |
|---|---|
| Mobile app only | Local SQLite + backup to the user's cloud (current default) |
| Web app only | ASP.NET Core Blazor Web App with a server database |
| Same idea on web and mobile, **same UI** | .NET MAUI Blazor Hybrid + Blazor Web App sharing a Razor class library ([ADR 0003](../adr/0003-ui-technology.md)) |
| Same data on several devices / web | Mobile stays local-first and **syncs** with a per-app ASP.NET Core API |

## Target structure for an app with a web version

```text
src/Apps/<App>/
├── Vafadar.<App>.Core        domain + use cases (shared by all of the below)
├── Vafadar.<App>.Data        local SQLite (mobile)
├── Vafadar.<App>.App         MAUI app
├── Vafadar.<App>.Contracts   DTOs shared by API and clients (when an API exists)
├── Vafadar.<App>.Api         ASP.NET Core API + server database (PostgreSQL / Azure SQL)
└── Vafadar.<App>.Web         Blazor Web App
```

Local orchestration of API, database and web front end can use **.NET Aspire** (`Vafadar.<App>.AppHost`).

## Sync-ready data model

The data conventions already make a later sync possible without migrating identifiers:

* **GUID v7 primary keys** – created on any device without coordination, no collisions.
* **`UpdatedAt` timestamps** on auditable entities – basis for "changed since" queries.
* To add when sync starts: **soft deletes** (`DeletedAt`) so deletions propagate, and a per-entity **version**
  (or row version) for conflict detection.
* Conflict policy per entity type (usually "last writer wins" per record, or per field for important data).

## Hosting (when needed)

| Need | Candidate |
|---|---|
| API + web, low traffic | Azure App Service / Azure Container Apps (free or low tiers), or a small VPS with Docker |
| Database | Azure SQL (serverless free offer), PostgreSQL (managed or on the VPS) |
| Identity | Microsoft Entra External ID or ASP.NET Core Identity ([authentication.md](authentication.md)) |
| Domain | Subdomains of `vafadar.pro`, e.g. `finance.vafadar.pro`, `api.finance.vafadar.pro` |

A backend changes the privacy situation (the developer now stores user data): the app's privacy matrix and privacy
policy must be updated before it goes live.
