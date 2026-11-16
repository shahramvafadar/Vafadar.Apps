# Dastan

A visual discovery site for connected stories: people, characters, places, works and events, their relationships,
their order in a story or in time, and the images and videos that belong to them. Public, for ordinary curious
visitors, delivered as a website that can be installed (PWA). The first collection is a bounded arc of the
Shahnameh, Rostam and Sohrab.

| | |
|---|---|
| Status | **Phases 1 to 4 of the [delivery plan](docs/delivery-plan.md) are done**: the foundations, the time library, the read model with the projection, and the first pages of the website (start, story, entity) in English and Persian. **Phase 5 is in progress**: the connections page with its drawing exists; the family view, the story sequence, gallery, evidence and search follow. |
| First milestone | M0 – a runnable local discovery slice ([acceptance](docs/acceptance.md)) |
| Platform | ASP.NET Core Blazor Web App on .NET 10; evergreen browsers; installable |
| Interface languages | English and Persian first (right to left); German and others later |
| App id | `pro.vafadar.dastan` (web address and final identity are open) |

## Documentation

| Document | Content |
|---|---|
| [Product specification](docs/product/README.md) | What Dastan is and how it must behave: 21 documents, with the decision register in document 12 |
| [Delivery plan](docs/delivery-plan.md) | Projects, accepted decisions, the phases to M0, risks |
| [Acceptance](docs/acceptance.md) | The twelve conditions of M0 and their current status |
| [Changelog](CHANGELOG.md) | What changed, in plain words |

Technical decisions of the app are recorded as ADRs in [`docs/adr/`](docs/adr/README.md) when they are made and verified; decisions that
concern the whole repository are in [`docs/adr`](../../../docs/adr/README.md).

## Projects

All three exist.

| Project | Purpose | Phase |
|---|---|---|
| `Vafadar.Dastan.Core` | Domain model, catalogue, the projection that decides what a visitor may see, views, search | 3 |
| `Vafadar.Dastan.Data` | Reads and validates the editorial sample; later the relational store | 3 |
| `Vafadar.Dastan.Web` | The website: pages, components, texts in English and Persian, media delivery | 4 |

Shared libraries written for Dastan and usable by other apps:
[`Vafadar.Temporal`](../../Libraries/Vafadar.Temporal/README.md) (done: stated time, compared without invented
precision) and [`Vafadar.Web`](../../Libraries/Vafadar.Web/README.md) (done: language in the address, direction,
theme, safe headers). [`Vafadar.Core`](../../Libraries/Vafadar.Core/README.md) supplies the search
key for names (`SearchText.Fold`).

## Run

```powershell
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Web      # then open http://localhost:5210
```

This starts the local demonstration (the Development settings): the site reads the sample from `content/` and
shows it as a demonstration. Started with any other settings the site is for the public and shows that nothing
is published, because the sample is not approved for publication. The pages need no script; the reading position
is chosen at the bottom of the page of the story.

## Build and test

.NET SDK 10.0.404 or a newer 10.0 feature band (`global.json`). From the repository root:

```powershell
dotnet build Vafadar.Dastan.slnf -p:ContinuousIntegrationBuild=true    # warnings are errors, as in CI
dotnet test --solution Vafadar.Dastan.slnf
dotnet clean Vafadar.Dastan.slnf                                       # afterwards: free the disk
```

`Vafadar.Dastan.slnf` holds Dastan and the libraries it uses, with their tests. It needs none of the private
package feeds of the MAUI projects.

## Content

[`content/`](content) holds the editorial sample and the original media. It is input for the app, not a public
folder: the sample contains the whole story including its ending, and the app serves only what its projection allows
for the visitor's reading position. Nothing in `content/` may be copied under a web root.

The sample is read by `PilotSample` (`Vafadar.Dastan.Data`), which refuses a file it does not fully understand. A
translation of the sample is a file next to it, `<sample>.<language>.json`; `shahnameh-pilot.fa.json` is the Persian
text, a **draft that nobody has reviewed yet** (`"review": "draft"`). Pages will mark such text as unreviewed (phase 4).

## Services

A database and every other service that can run in a container runs in Docker Desktop on the development machine,
in one Compose project named `test`. M0 needs no database.
