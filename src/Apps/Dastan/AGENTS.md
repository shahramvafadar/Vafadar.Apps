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
* Visitors have accounts in a third store ([ADR 0014](docs/adr/0014-visitor-accounts.md),
  `Vafadar.Dastan.People`, the setting `Dastan:People`). It references neither the collections' readers nor the
  store of the editors, and nothing of it reaches a collection. An account is a name and what is kept of a
  password – never add a field about the person (a mail address, a real name, an address of a machine) without a
  decision of the owner and a line in the privacy matrix. A site without the setting must behave as it did
  before accounts. Answers never say whether a name exists. A password and a recovery code are never logged,
  never put into an address and never kept as they are. A change of the schema is a migration; the file of the
  store is never committed. A page asks `ReadingPreference.Read` and `SavedList.Read` for the reading position
  and the list – never a cookie itself: signed in they are the account's, and the cookies of the device are
  neither read nor changed. Nothing is taken from a device into an account without an act of the visitor. An
  invitation makes an account and nothing else (PRIV-006). What an act on a relationship may do and who may
  see it is asked of the rules in `Vafadar.Dastan.Core.Personal` – the store brings a rule object to where
  things stand; never write such a rule into a query or a page. Ask the scope before producing anything from
  a private item, a count included (PRIV-012), and never let a missing circle or a removed account widen an
  audience. A family tree does not exist for an account it is not shared with: answer "not found", never
  "refused". A record of a person says a name and years – add nothing to it without a decision of the owner
  (PRIV-003). Every change of a tree is recorded with who made it, in the same save as the change. A family is laid out
  by `TreeShapeLayout` and drawn by `TreeDrawing`, whatever its members are – never write a second layout or
  a second drawing for another kind of family. The file of a photograph is given by one address that asks
  its audience (`/photo/{id}`); never serve the folder of the photographs, never link a file by its name, and
  bring a file in through `MediaIntake` only – it reads what the file is from its content and takes out what
  an image says about its making. Answer "not found" for a photograph a viewer may not see. Whether something that was made public is shown
  is asked of the store with every request (`PublicTree`) – never keep "is public" as a fact of its own, and
  never let anybody agree for somebody else (D-053).
* The editors work in a store of their own ([ADR 0011](docs/adr/0011-editors-store.md), `Vafadar.Dastan.Store`):
  records as they are written in the file of a collection, each change kept with who, when, why, before and
  after. The website never references the store. What a record may say is decided by the readers – never add a
  rule to the store that the readers do not have, and never write to the store without reading the resulting
  collection with them. A change of the schema is a migration (`dotnet ef migrations add`); the file of a store
  is never committed.
* The tool of the editors (`Vafadar.Dastan.Studio`) is built by the rules of the site – server-rendered pages, no
  script, no style in a page, both languages – and shows a record as it is written, never an interpretation of
  it. Its forms are ordinary forms without a script: done leads back to the page, refused answers
  with the page, the reasons of the readers and the form as it was filled in; the tool checks only what a form
  needs to be understood and leaves every rule to the store. The form of the fields of a record (`RecordForm`) is
  made from the record as it is written and from `ContentVocabulary`; do not give the tool a description of a
  record of its own – a new field or value belongs to the readers and their vocabulary, and the form follows.
  A new entity or relation type is added in `Vocabulary.cs` with what it means; a relation type of the world
  outside a story also says between what kinds of thing it holds, and the readers refuse it elsewhere.
  **Accounts of visitors and private spaces are decided** (D-048 to D-055, 2026-11-18): a name and a password
  kept by the site, and Google or Microsoft as well; anybody may register and may invite others; a store of its
  own beside the site; all private spaces; a claim on a record is confirmed by who made the record; private
  things about living people become public only with the consent of everybody named; a tree has an owner who
  names who may change it; withdrawn consent hides at once and a closed account is deleted after 30 days. The
  rules are the library of ADR 0013 and are asked before anything is read for a viewer.
  **Do not wait for real content to show a gap** (D-047): what the product documents name as possible – kinds
  of things, relations, time, media, audiences – is supported before content needs it, each with its meaning
  and its tests; "it follows the content" is no longer a reason to leave a capability out.
  What a translation may translate is listed by the readers (`ContentVocabulary.Translated`); the texts of a record
  in the other languages are kept with the record in one act (`Put` with texts), never in an act of their own
  that could leave a record and its translation disagreeing. Without accounts it answers the machine it runs on only. With accounts (`Studio:Accounts`, step 11.7) it
  answers other machines: then every address but the page that signs in needs a sign-in, a request from another
  machine has to come encrypted, and an act is kept under the account – never under a name a form gives. A new
  page or address is behind the sign-in unless it is added to the short list in `StudioAccess`; do not add what
  shows anything of the store. A password is typed where a command asks for it and is never a word of a command,
  a setting or a file. Do not make the public site reference the tool or the store. On a server the tool is one container with
  the store on a volume ([operations of the tool](docs/operations-studio.md)); a change of a header, a cookie, the
  sign-in or what is open without one is checked with `eng/Check-Studio.ps1` against the running container.
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
