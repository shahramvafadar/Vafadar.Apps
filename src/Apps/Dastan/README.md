# Dastan

A visual discovery site for connected stories: people, characters, places, works and events, their relationships,
their order in a story or in time, and the images and videos that belong to them. Public, for ordinary curious
visitors, delivered as a website that can be installed (PWA). The first collection is a bounded arc of the
Shahnameh, Rostam and Sohrab.

| | |
|---|---|
| Status | **The first milestone, M0, is built**: all seven phases of the [delivery plan](docs/delivery-plan.md) are done. What each acceptance condition was checked with, and what was not checked, is in the [acceptance record](docs/acceptance.md). It is a local demonstration; nothing is published, and what a public pilot still needs is listed in the delivery plan. |
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

All exist.

| Project | Purpose | Phase |
|---|---|---|
| `Vafadar.Dastan.Core` | Domain model, catalogue, the projection that decides what a visitor may see, views, search | 3 |
| `Vafadar.Dastan.Data` | Reads and validates the editorial sample; later the relational store | 3 |
| `Vafadar.Dastan.Web` | The website: pages, components, texts in English and Persian, media delivery | 4 |
| `Vafadar.Dastan.Content` | The command of the writers and the editors: check, vocabulary, template, revision, store | 9 |
| `Vafadar.Dastan.Studio` | The tool of the editors: pages that show what the store holds. Runs on the editors' machine only | 11 |
| `Vafadar.Dastan.People` | The store of the visitors: their accounts and, with them, what belongs to one person ([ADR 0014](docs/adr/0014-visitor-accounts.md)). Used by the website only where it is told a file for it | 16 |
| `Vafadar.Dastan.Store` | The store of the editors: the records of a collection with the history of their changes ([ADR 0011](docs/adr/0011-editors-store.md)). The website does not use it | 11 |

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
is published, because the sample is not approved for publication. No page needs a script to be read; the reading position
is chosen at the bottom of the page of the story.

The tool of the editors shows what their store holds ([ADR 0011](docs/adr/0011-editors-store.md)). The store is
a PostgreSQL database ([ADR 0015](docs/adr/0015-postgresql.md)); `compose.yaml` starts one for development that
keeps nothing. The tool needs its connection and answers this machine only:

```powershell
docker compose -f src/Apps/Dastan/compose.yaml up -d postgres
$env:DASTAN_DATABASE = "Host=127.0.0.1;Port=53432;Username=postgres;Database=dastan"   # the development database has no password
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Content -- database migrate --database env
$env:Studio__Store = $env:DASTAN_DATABASE
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Studio      # then open http://localhost:5211
```

For several editors the tool has accounts ([ADR 0011](docs/adr/0011-editors-store.md), seventh addendum): an
account is made where the store lies – the password is asked for, never written into the command – and the tool
is started with `Studio:Accounts`. It then answers other machines over https only, and only who signed in:

```powershell
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Content -- store editor add "<name>" --store env
$env:Studio__Accounts = "true"
$env:Studio__Keys = "<a folder for the keys of the sign-in>"       # without it everybody signs in again after a restart
$env:AllowedHosts = "<the name the tool is reached by>"
# behind a proxy that holds the certificate: $env:Studio__TrustedProxies__0 = "<its address or network>"
```

The tool on a server – its container, the accounts, copies of the store – is in
[operations of the tool](docs/operations-studio.md).

Running the site outside a developer's machine – the container image, what it is told, how it is checked, updated and
taken back – is in [operations](docs/operations.md).

## Build and test

.NET SDK 10.0.404 or a newer 10.0 feature band (`global.json`). From the repository root:

```powershell
dotnet restore Vafadar.Dastan.slnf                                     # packages; no private feed is needed
dotnet build Vafadar.Dastan.slnf -p:ContinuousIntegrationBuild=true    # warnings are errors, as in CI
dotnet test --solution Vafadar.Dastan.slnf
dotnet clean Vafadar.Dastan.slnf                                       # afterwards: free the disk
```

`Vafadar.Dastan.slnf` holds Dastan and the libraries it uses, with their tests. It needs none of the private
package feeds of the MAUI projects.

### Tests in a browser

The journey of M0, the installable app, the chart of a comparison and the automatic part of the accessibility check run as tests in a real browser (`test/Apps/Dastan/Vafadar.Dastan.Browser.Tests`). The browser
lives in the official Playwright container of the Compose project `test` – Chromium, or Firefox or WebKit when
asked for; the tests start the site on this machine, on `localhost` only, and tell the browser where it is.

```powershell
docker compose -f src/Apps/Dastan/compose.yaml up -d
$env:DASTAN_BROWSER = "ws://127.0.0.1:53333/"
dotnet test --project test/Apps/Dastan/Vafadar.Dastan.Browser.Tests
docker compose -f src/Apps/Dastan/compose.yaml down
```

Without `DASTAN_BROWSER` the tests are skipped and say so; that is what happens in CI, which has no container. The
version of the container and the version of the package `Microsoft.Playwright` in `Directory.Packages.props` have
to be the same.

The other two engines are named by `DASTAN_BROWSER_ENGINE` and run two classes of tests at a time: a window of
Firefox or WebKit takes far more memory than one of Chromium, and the container may take 6 GB.

```powershell
$env:DASTAN_BROWSER_ENGINE = "firefox"   # or "webkit"; unset or "chromium" for Chromium
dotnet test --project test/Apps/Dastan/Vafadar.Dastan.Browser.Tests --max-threads 2
```

What the other engines are not asked: whether the app can be installed (a question only Chromium answers), and,
in Firefox, the page for "no connection" – what the tests can turn off there is not what a service worker uses.
The WebKit of the container is the engine of Safari, not Safari on a phone or a Mac. Right after the container
has started, the first page of a run can take longer than the tests wait; a second run shows whether that was it.

## Content

Content is written apart from the software, by people and by assistants (D-042). [The guide for
writers](docs/content-guide.md) says how; [the file of a collection](docs/collection-format.md) and [the file of a
comparison](docs/comparison-format.md) say what every field means; and one command checks a file exactly as the
site reads it, lists the values a field may have and writes a file to start from:

```powershell
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Content -- check src/Apps/Dastan/content
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Content -- vocabulary
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Content -- template collection my-story
```

[`content/`](content) holds the editorial sample and the original media. It is input for the app, not a public
folder: the sample contains the whole story including its ending, and the app serves only what its projection allows
for the visitor's reading position. Nothing in `content/` may be copied under a web root.

The sample is read by `PilotSample` (`Vafadar.Dastan.Data`), which refuses a file it does not fully understand. A
translation of the sample is a file next to it, `<sample>.<language>.json`; `shahnameh-pilot.fa.json` is the Persian
text, a **draft that nobody has reviewed yet** (`"review": "draft"`). Pages will mark such text as unreviewed (phase 4).

What a collection states with a date about its entities (`dated`) and the times its images and videos were made
are shown at `/<collection>/timeline`, on the chart of a comparison.

[`content/comparisons`](content/comparisons) holds comparisons: several subjects side by side over one axis of
years, shown at `/compare/<id>` as a timeline and a table ([ADR 0008](docs/adr/0008-charts.md)). They are read by
`ComparisonSample`, as strictly as the sample; [the format](docs/comparison-format.md) is described on a page of its
own. The first one, `iran-china-rome.json`, is a **draft without sources** and is shown in the local demonstration
only.

## Third-party files

| File | What | License | From |
|---|---|---|---|
| `Vafadar.Dastan.Web/wwwroot/fonts/vazirmatn-nl-wght.woff2` | The font Vazirmatn 33.003, variable weight, without Latin letters – unchanged (`Vazirmatn-NL[wght].woff2`, SHA-256 `cdef5578…bbf5`) | SIL Open Font License 1.1, `wwwroot/fonts/vazirmatn-OFL.txt` | Release v33.003 of <https://github.com/rastikerdar/vazirmatn> |
| `Vafadar.Dastan.Web/Data/coasts.txt`, `Data/waters.txt` | The coasts of the earth, and its rivers and the shores of its lakes: Natural Earth 1:50m, written as text with positions rounded to a hundredth of a degree (step 17.2) | Public domain | naturalearthdata.com, from the repository `nvkelso/natural-earth-vector` on 2026-11-18 |
| (package) `SkiaSharp`, `SkiaSharp.NativeAssets.Linux.NoDependencies` 4.154.1 | Reading and writing images (step 17.3); restored by the build, not in the repository | MIT | nuget.org, published by Microsoft |

## Services

A database and every other service that can run in a container runs in Docker Desktop on the development machine,
in one Compose project named `test`. M0 needs no database.
