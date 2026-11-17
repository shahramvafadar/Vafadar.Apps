# AGENTS.md – working rules for Dastan

The rules of the root [`AGENTS.md`](../../../AGENTS.md) apply here in full: Persian conversation with the owner,
an English repository, no attribution, the owner's Git identity, secrets, honest reporting, tests with every feature
and documentation in the same commit. Its rules about money, the ledger, MAUI, Android devices and Zanance's
documents concern Zanance and do not apply to Dastan. This file adds what the owner decided for Dastan; a new rule
for Dastan is added here in the same commit as the work it came with.

## 1. The app

* **Dastan** is a public visual discovery site, delivered as an installable website (PWA): ASP.NET Core Blazor Web
  App on .NET 10. First collection: a bounded arc of the Shahnameh, Rostam and Sohrab. The product name is shown
  untranslated; its Persian spelling is an open brand question.
* What it is: [`docs/product`](docs/product/README.md) (21 documents). The owner's decisions:
  [document 12](docs/product/12-decisions-questions-and-change-log.md). How and in which order it is built:
  [`docs/delivery-plan.md`](docs/delivery-plan.md). What is done and checked: [`docs/acceptance.md`](docs/acceptance.md).
* The first milestone is M0, a runnable local slice. Nothing is deployed, no account is created and no service is
  bought for M0.

## 2. The owner's rules for the work

* **No rework, no technical debt** – this has priority over an early demonstrable result (D-030). Read the governing
  specification documents of a phase in full before starting it. Record a decision that later work builds on as an
  ADR before code depends on it. Do not leave a shortcut behind with a note to fix it later; do it properly or record
  it as an open item in the plan.
* **Reuse, then extract, then extend** (D-029). Use a shared library where it fits as it is. Write a part that other
  apps can use as its own shared library, without texts and without Dastan's vocabulary. Extend an existing library
  only by adding to it; behaviour another app relies on changes only with that app's decision
  ([shared libraries backlog](../../../docs/libraries-backlog.md)).
* **Zanance is developed at the same time.** Touch shared files – the solution, package versions, root documents,
  shared tests – as little as possible and by adding lines. Work on `feature/dastan-<topic>` branches.
* **Merging does not wait for the owner** (D-038). Finished work whose build and tests pass is brought into `main`
  – by a pull request where one can be opened, otherwise by a merge commit of the branch, pushed after the merged
  state was built and tested; CI on `main` is checked once afterwards. Open pull requests are dealt with in the same
  way: merged when they are verified and change nothing another app was verified on, otherwise left with the
  reason written down. Everything is done under the owner's identity; nothing names a tool or an assistant.
* **Services run in containers** (D-034): a database and every other service that can run in a container uses
  Docker Desktop on the development machine, in one Compose project named `test`. Never start, stop, remove or prune
  anything of another project.

## 3. Architecture

* Projects: `Vafadar.Dastan.Core` (domain, no infrastructure), `Vafadar.Dastan.Data` (reading and validating
  content), `Vafadar.Dastan.Web` (the site). Shared: `Vafadar.Core`, `Vafadar.Temporal`, `Vafadar.Web`. Dastan does
  not reference `Vafadar.Localization`, Zanance or MAUI.
* **One projection decides what a visitor sees.** Publication state and demo eligibility, then the reading boundary,
  are applied on the server before anything is counted, searched, laid out, suggested or serialized. Hiding in the
  browser is never the protection.
* The files in [`content/`](content) are editorial input with the whole story. They are never copied under a web
  root, never served as files and never cached by a service worker. Media reach the browser only through a route
  that checks the projection. The fixtures in `test/Apps/Dastan/Fixtures` are test data and never public.
* Time is never a `DateTime` unless it is an operational timestamp: story and historical time use `Vafadar.Temporal`.
  No date, precision or order is invented; unknown stays unknown.
* Charts are drawn by Dastan itself ([ADR 0008](docs/adr/0008-charts.md)): the layout is plain code in
  `Vafadar.Dastan.Core` with tests, the page writes it as SVG with places and sizes as attributes (no inline style,
  no script). How a chart is looked at – direction, scale, stretch of time – lives in the address and is changed by
  links. Time runs from left to right and from top to bottom in every language; only texts follow the language.
* A chart stands in a stage (`ChartStage`, D-040): the whole width of the page and the height of the window, with a
  "−" and a "+" at each of its four sides (`EdgeSteps`) that close and open the chart in that direction. Every
  step is a link to an address; a new chart says in ADR 0008 what its four sides mean. Class names of the stage
  begin with `stage-` – a short name like `step` belongs to the story already. A chart drawn around a name
  marks its box with `graph-focus`; the one script of the site brings it into sight, and nothing else may depend
  on that script.
* Everybody sees a collection only as a published revision ([ADR 0009](docs/adr/0009-published-revision.md),
  D-044). The rule lives in the checks of `Catalogue`, in the readers and in `Projection`; a page never decides by
  the audience what it shows. Reviewing a statement and publishing a revision are the editors' decisions – never
  mark content as reviewed or approved to make something visible. A revision is made with the `revision` command
  from the working file, carries the record of each review and its history (`publication.history`), and is never
  edited by hand; what a visitor is told of the history goes through `Projection.Revisions`, by reading stage.
  A disputed statement is marked with `DisputedMark` wherever a page or a chart shows a
  statement – a new view of statements uses it too; a withdrawn statement never reaches a `Catalogue`.
* An item that may not be shown is told to a page only as a `MediaReference` (`Projection.References`, the
  galleries): the catalogue record and the link to the holder. Never pass its `MediaItem` to a page, never load
  anything from the holder's server, and add a field to the reference only if a visitor may see it.
* The site runs outside a developer's machine as one container without content
  ([ADR 0010](docs/adr/0010-running-the-site.md), [operations](docs/operations.md)). A change of a header, a
  cookie, a setting or of what the site serves is checked with `eng/Check-Site.ps1` against the container of
  `compose.site.yaml`, and a new setting is written into operations in the same commit. Never put content into
  the image and never widen what the site believes from a proxy.
* To look at the site as the public gets it, publish it (`dotnet publish`) and run the published output with
  `ASPNETCORE_ENVIRONMENT=Production` and `Dastan__Content` pointing to a published revision. `dotnet run` in
  the Production environment serves empty style sheets (static web assets are off there) – a page looked at
  that way was not looked at.
* Dates are stored in the Gregorian calendar (D-043); another calendar is a matter of display. Content is written
  apart from the software, by people and assistants who follow the official guide and tools (D-042): a format
  that changes is changed in its guide and its checks in the same commit. The guide is `docs/content-guide.md`, the
  formats `docs/collection-format.md` and `docs/comparison-format.md`, the command `Vafadar.Dastan.Content`
  (`check`, `vocabulary`, `template`, and `revision` for the editors). The command reads with the readers of the site and lists what they
  accept – a value is added in a reader, never in the command; a template is a file the readers accept.
* A comparison (`content/comparisons`, [format](docs/comparison-format.md)) has no reading stages and does not pass
  through a projection; the site keeps only the comparisons released to its audience. A year is written on a chart
  only when one year is stated – "about" and "between" stay what they are.
* A relationship keeps its type, direction, scope and source. Nothing is derived from proximity in a picture, a
  shared event or a similar name. Someone mentioned in an event is not present in it.

## 4. Languages

* Interface texts exist in **English and Persian** (D-031); the list is `VafadarLanguages` in
  [`Directory.Build.props`](Directory.Build.props) ([ADR 0011](../../../docs/adr/0011-languages-per-app.md)). Every
  text exists in each declared language as a real translation – never the English text in a Persian file.
* Dastan's texts are its own (D-032); they are not taken from another app or a shared library.
* Direction comes from the language, never from a test for Persian: Kurdish and Arabic must need translations only.
  Use logical CSS properties. Do not mirror a time axis, the direction of a relationship, Latin identifiers or
  numbers just because the page is right to left.
* The language of the interface and the language of the content are different things. A text shown in another
  language than the interface says so; a translation that is not reviewed is marked as such.

## 5. Checking the running app

Check every visible change before reporting it: English and Persian; light and dark; 360 px, 412 px and a wide
window; keyboard only. Report what was checked and how, what was not, and what failed. A simulated width is not a
phone, and a valid manifest is not an installation.

The journey is also checked by tests in a browser (`Vafadar.Dastan.Browser.Tests`, see the README): run them after a
change of a page, of the styles or of the headers of an answer. They are skipped without the browser container, so
a run in which they were skipped has checked nothing in a browser – say so. `DASTAN_BROWSER_ENGINE` names `firefox`
or `webkit` instead of Chromium; run those with `--max-threads 2`. A test opens its windows with
`BrowserSite.Visitor` and waits for a page with `BrowserSite.Arrived`: both hold what the three engines need.

## 6. Documentation duties

Update in the same commit as the change:

* [`docs/product/12-…`](docs/product/12-decisions-questions-and-change-log.md) – every decision of the owner as the
  next `D-…`, and the owning topic document when a behaviour changes;
* `docs/adr/` – every technical decision of the app (repository-wide ones in the root `docs/adr`);
* [`docs/acceptance.md`](docs/acceptance.md) – the status and evidence of M0-01 … M0-12;
* [`docs/delivery-plan.md`](docs/delivery-plan.md) – the phase reached and any change of the plan, with its reason;
* [`CHANGELOG.md`](CHANGELOG.md) – what a visitor or the owner can notice, in plain words;
* [`README.md`](README.md) – the status and the exact commands to build, run and test.
