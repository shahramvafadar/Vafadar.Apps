# Dastan – delivery plan for M0

- Status: **Accepted by the owner on 2026-11-15** (decisions D-029 to D-034 in the
  [decision register](product/12-decisions-questions-and-change-log.md))
- Date: 2026-11-15
- Scope: the first runnable local discovery slice (M0). The public pilot (R0) and later capabilities are listed in
  section 8 only as boundaries.

Dastan is a new app in this repository: a public, visual discovery site, delivered as an installable web app. This
plan replaces the "start immediately" sequence of the handoff pack with the owner's instruction of 2026-11-15:
**avoid rework and technical debt, even when that delays the first demonstrable screen.** Foundations are therefore
built and tested before screens, and every reusable part is written as an independent library.

The product specification is the handoff pack (21 documents, sample data and media). It was imported into
`src/Apps/Dastan/docs/product/` and `src/Apps/Dastan/content/` in phase 1; the pack itself stays outside the repository.

## 1. Principles

1. **No rework.** A phase starts by reading its governing specification documents in full. A decision that later
   phases build on is recorded as an ADR before code depends on it.
2. **Reuse first, then extract, then extend.** Use an existing shared library where it fits as it is. Write a part
   that is generic from the start as its own library. Extend an existing library only by adding to it; behaviour that
   Zanance relies on does not change without a Zanance decision ([shared libraries backlog](../../../../docs/libraries-backlog.md)).
3. **Libraries carry no translations and no app vocabulary.** Each app owns its texts and its list of languages.
4. **English and Persian first**, with German and other languages later. Direction comes from the language, never from
   a test for Persian, so Kurdish and Arabic need translations only.
5. **One projection decides what a visitor may see.** Publication state, demo eligibility and the reading boundary are
   applied on the server before anything is counted, searched, laid out or serialized.
6. **Repository rules apply unchanged** (`AGENTS.md`): English repository, Persian conversation, no attribution, the
   owner's Git identity, tests with every feature, documentation in the same commit, checks in the running app.
7. **Zanance is developed in parallel.** Dastan work touches shared files (solution, package versions, root
   documentation) as little as possible and only by adding lines, on its own branches.
8. **Services run in containers.** A database and every other service that can run in a container uses the Docker
   Desktop installation of the development machine. All containers of Dastan belong to one Compose project named
   `test`; nothing of another project is started, stopped or pruned. M0 needs no database; the first container is the
   browser of the journey tests.

## 2. What exists and what is new

| Part | Decision | Notes |
|---|---|---|
| `Vafadar.Core` | Reuse | `SearchText.Fold` (added 2026-11-15) for name search, `Digits`, `StaticAppEnvironment` |
| `Vafadar.Localization` | Not referenced | It contains Zanance's shared texts in six languages and a process-wide language service. Dastan uses the localization of ASP.NET Core with its own resources. |
| Data, Backup, Authentication, Cryptography, Documents, Maui libraries | Not relevant to M0 | No database, accounts or MAUI |
| `Vafadar.Temporal` | **New shared library** | Uncertain, partial, ancient, deep and narrative time. Plain .NET, no dependencies, no texts. |
| `Vafadar.Web` | **New shared library** | What every web app of this repository needs: language from the address, `lang`/`dir`, light and dark theme, installable-app shell, connection state, Syncfusion Blazor license registration. No app texts. |
| `Vafadar.Dastan.Core` | New | Domain, catalogue, the publication and reading-boundary projection, view projections, search |
| `Vafadar.Dastan.Data` | New | Reads and validates the editorial sample; the place of the later relational store |
| `Vafadar.Dastan.Web` | New | Blazor Web App: pages, components, resources (en, fa), static assets, media delivery |

```text
src/Libraries/Vafadar.Temporal/            test/Libraries/Vafadar.Temporal.Tests/
src/Libraries/Vafadar.Web/                 test/Libraries/Vafadar.Web.Tests/
src/Apps/Dastan/
├── README.md, CHANGELOG.md
├── docs/                                  product/ (specification), adr/, delivery-plan.md, acceptance.md
├── content/                               editorial sample and original media – never under wwwroot
├── Vafadar.Dastan.Core/                   test/Apps/Dastan/Vafadar.Dastan.Core.Tests/
├── Vafadar.Dastan.Data/                   test/Apps/Dastan/Vafadar.Dastan.Data.Tests/
└── Vafadar.Dastan.Web/                    test/Apps/Dastan/Vafadar.Dastan.Web.Tests/
                                           test/Apps/Dastan/Vafadar.Dastan.Web.Journey.Tests/  (browser)
```

Dependencies: `Dastan.Web → Dastan.Data → Dastan.Core → Vafadar.Core, Vafadar.Temporal`; `Dastan.Web → Vafadar.Web`.
`Dastan.Core` has no infrastructure. Nothing references Zanance or MAUI.

## 3. Decisions

The owner accepted the recommendation of every row on 2026-11-15. Rows marked *reversible* can be changed later at
low cost; the others shape every following phase.

| # | Decision | Recommendation | Alternative | Weight |
|---|---|---|---|---|
| 1 | Projects and names | As in section 2 | Keep temporal types inside `Dastan.Core`; no `Vafadar.Web` until a second web app exists | Shapes everything |
| 2 | Localization | ASP.NET Core localization with Dastan's own `.resx` files; `Vafadar.Localization` untouched | Split `Vafadar.Localization` into an engine and Zanance's texts now (changes Zanance) | Shapes everything |
| 3 | Languages per app | A build property `VafadarLanguages` per project (default: the six languages of today); the completeness test reads it. Dastan: `en;fa`. | Dastan translated into six languages from the start | Shapes phase 1 |
| 4 | Language in the address | `/en/…` and `/fa/…`; the plain address redirects by saved choice, then by browser language | Language only in a cookie (addresses cannot be shared in a language) | Hard to change after publication |
| 5 | Reading boundary (spoilers) | A per-visitor cookie read by the server; a link never raises it; default level 0 | In the address (a shared link would reveal later content) | Shapes the projection |
| 6 | UI components | Own semantic HTML and CSS with design tokens for the public pages; Syncfusion where it gives real value – first candidate the connections graph, later editorial tools | Syncfusion components throughout (larger pages, most of it needs a live connection) | Reversible per component |
| 7 | Graph renderer | Decide by a bounded comparison in phase 4: Syncfusion Diagram first, Cytoscape.js as the single alternative. **Decided on 2026-11-16 (D-035):** SVG drawn by the server for M0; the two are compared hands-on when a collection needs a large, freely explored graph ([ADR 0005](adr/0005-graph-renderer.md)) | Choose one now without a comparison | Reversible behind an adapter |
| 8 | Persian story text | The model carries text per language from the start. Persian names, labels and summaries of the sample are drafted in phase 3 and marked "review pending" for the owner; English stays the fallback. | Persian interface over English story text until after M0 | Affects layout checks |
| 9 | Branches | `feature/dastan-<topic>`, one pull request into `main` per phase | Commit to `main` directly | Reversible |
| 10 | Working rules | A short Dastan section in the root `AGENTS.md` plus `src/Apps/Dastan/AGENTS.md`; Zanance's history in the root file is not moved | Restructure the root file now (conflicts with running Zanance work) | Reversible |
| 11 | Browser tests | Playwright for .NET; the browser runs in the official Playwright container, in the Compose project `test` (principle 8) | Manual browser checks only | Reversible |
| 12 | Build output | The repository's own `artifacts/` and the default `bin`/`obj`; the other machine's `D:\_Projects\…` path does not exist here | – | Reversible |

Settled already: the name Dastan, the Rostam and Sohrab arc as working content, Blazor Web App on .NET 10, English
documentation, English and Persian as first languages, independent translations, no rework before speed, local
tool files outside Git.

## 4. Phases

| Phase | State on 2026-11-16 |
|---|---|
| 0 – Repository ready | **Done.** The working copy is a complete clone on .NET SDK 10.0.404; the work of phases 1 and 2 is on the branch `feature/dastan-foundations` (pushed on 2026-11-16). |
| 1 – Foundations of the repository | **Done:** app folder with README, changelog, working rules, specification, sample and media; decisions D-029 to D-034; ADR 0011 with its tests; the repository tables. |
| 2 – `Vafadar.Temporal` | **Done:** the library, 165 tests, [ADR 0012](../../../../docs/adr/0012-temporal-model.md), its README, the solution filter `Vafadar.Dastan.slnf`. M0-08 is checked ([acceptance](acceptance.md)). Changed against this plan: formatting for display is not built yet – it comes with its first screen (phase 4 or 5), because a formatter without a screen would be designed by guessing. |
| 3 – `Vafadar.Dastan.Core` and `Vafadar.Dastan.Data` | **Done** on the branch `feature/dastan-domain`: the read model with its consistency rules; the strict reader of the sample and of its translations; the Persian text of the sample as a draft ([ADR 0001](adr/0001-domain-read-model.md)); the projection with every view of M0 and the check that no answer discloses a later stage, for every stage of the sample ([ADR 0002](adr/0002-one-projection.md)); a collection of a club, teams and a company through the same model; the time values of the cross-domain fixtures in `Vafadar.Temporal`. 296 tests in the two Dastan test projects; the model part of M0-03, M0-04, M0-06, M0-07 and M0-12 is checked ([acceptance](acceptance.md)). Changed against this plan: of the validator's ten invalid copies, "a revoked viewer keeps media" is not ported – it tests access policies of private spaces, which are not part of M0; a relation has no time of its own in the read model yet (the story needs none, the fixtures' periods are checked in the time library only) – it is added with the first collection that needs it. |
| 4 – `Vafadar.Web` and the first pages | **Done** on the branch `feature/dastan-web`: the shared library `Vafadar.Web` (51 tests, repository [ADR 0013](../../../../docs/adr/0013-web-library.md)); the host `Vafadar.Dastan.Web` with the pages Discover, story and entity, the reading control and the theme choice, in English and Persian ([ADR 0003](adr/0003-server-rendered-pages.md), [ADR 0004](adr/0004-design-tokens-and-fonts.md)); 50 tests that ask the site over HTTP, among them two visitors at the same time (M0-09) and a walk over every page for every reading stage. Checked in a browser at 360, 412 and 1280 px, light and dark, both languages. Changed against this plan: bUnit is not used – the pages are tested through the running site, which also covers routing, cookies and headers; the Syncfusion license registration is not added, because M0 may need no Syncfusion component ([ADR 0005](adr/0005-graph-renderer.md), **open for the owner**); the graph comparison is written as a comparison of fit, not of measurements; the browser container for journey tests (decision 11) is set up with the journeys in phase 5; fonts are the system's, a bundled Persian font is proposed (ADR 0004). |
| 5 – The connected journey | **Done** (in `main`): the connections page – the graph as SVG drawn by the server, expanding and collapsing by links, and the same relations as a list below it (D-035); the family page – generations as rows, parents above their children, partners joined, with the same relations as a list; the story step by step with who and what is part of each step, and the lanes – a table of names by steps built from the same steps and associations. the galleries of the collection, of an entity and of a step, the page of an image or a video, and the route that delivers their files after asking the projection ([ADR 0006](adr/0006-media-delivery.md)). what supports a statement – its own sources, the state of its review, the way back to where it was opened – with a page that says what leaving means before a source of another site is opened; the search for names inside the collection. the states of document 18, each with the page that answers it ([acceptance](acceptance.md), "The states of document 18"). The journey runs as tests in a real browser – Chromium in the Playwright container of the Compose project `test`, 79 tests – for M0-02 to M0-07, M0-09 and the layout of M0-10. Not done here: a phone (a window of 360 px is not one) and a screen reader; they belong to the phases 6 and 7. |
| 6 – Installable app, failures, accessibility | **Done** (in `main`): the manifest with original provisional icons; a service worker that keeps nothing but the page for "no connection" ([ADR 0007](adr/0007-installable-app.md)); a page for a refused form; the demonstration asks not to be indexed; in a real browser: no connection and its return, the journey with scripts turned off, what Chromium says about installing, and the rules of accessibility a tool can decide, in both languages and both appearances and the replacement of the worker by a newer one (135 browser tests). Not done: an installation was not observed, no phone, no screen reader. |
| 7 – Verification and handoff | **Done** on 2026-11-16: the acceptance record states for each condition what it was checked with and what was left; a fresh copy of the repository was restored, built, tested and started with the commands of the README; the gates before a public pilot are listed below. |

Every phase ends with: build without warnings (`-p:ContinuousIntegrationBuild=true`), its tests green, documentation
updated, one commit per finished step, pushed. A phase is not started while the previous one has an open defect.

### Phase 0 – Repository ready (owner action)

* A complete clone of the repository.
* Check: remote, identity `Shahram Vafadar <shahramvafadar@gmail.com>`, hooks path, no other account.
* The handoff pack outside the repository; local tool files in `.git/info/exclude`.
* Baseline: `dotnet test --solution Vafadar.Libraries.slnf` (MAUI projects need their private feeds and are not built
  here).
* First commit: the `SearchText.Fold` work that is already done and tested.

Exit: `git status` works, the baseline is recorded, the branch exists.

### Phase 1 – Foundations of the repository

1. App folder: `README.md`, `CHANGELOG.md`, the specification imported into `docs/product/` with working links, the
   decision register continued with the decisions of 2026-11-15, this plan, `acceptance.md` with M0-01 … M0-12.
2. ADR 0011: languages per app. `VafadarLanguages` in the build properties, `SatelliteResourceLanguages` derived from
   it, `ResourceCompletenessTests` reading it. Zanance's result is unchanged (same six languages).
3. Working rules (decision 10), solution filter `Vafadar.Dastan.slnf`, the repository tables (root README,
   architecture overview, privacy matrix, issue templates).

Exit: Zanance's library tests still pass unchanged; a test proves that a two-language project is accepted and a
missing translation in it is still reported.

### Phase 2 – `Vafadar.Temporal`

Governing documents: 04 (time and uncertainty), 03 section on time, scenarios SC-03 … SC-08, SC-11, SC-12, SC-18.

* Value kinds: exact and partial calendar date, uncertain occurrence, period with independently uncertain ends,
  open end (ongoing, ended unknown, unknown), duration without a position, deep time with unit and fixed reference,
  position in a narrative order, reference to an event.
* Calendar and era are separate; precision and certainty are separate; source wording is kept.
* Comparison returns definite, possible, insufficient information, no overlap or incompatible – never an invented
  coordinate. No `DateTime` for anything that is not an operational timestamp.
* Lossless JSON round trip. Formatting for display follows with its first screen, with the words supplied by the
  app (moved out of this phase on 2026-11-16, see the table above).
* ADR 0012: the model, year numbering across BCE/CE, what is comparable, and that both ends belong to a period.

Exit: the cases of TIME-017 and M0-08 pass as tests; the README documents the contract with examples.

### Phase 3 – `Vafadar.Dastan.Core` and `Vafadar.Dastan.Data`

Governing documents: 03, 06 (what must never be served), 07, 13, 14, 15, 17, both sample files.

* Typed entities, relations (type, direction, scope), events with typed associations (acts, mentioned, setting,
  object), sources with locators, media with rights, representation and dates, world and edition context, text per
  language with explicit fallback.
* The immutable catalogue behind a small read interface.
* **The projection:** publication state and demo eligibility, then the reading boundary, then every derived view –
  entity overview, neighbourhood graph, family view, story sequence, participant lanes, gallery, evidence, search.
* `Dastan.Data`: reader for `dastan-pilot-0.2` with an explicit version check and diagnostics that name the record;
  the checks of the pack's validator, including its ten negative cases, as tests. The cross-domain fixtures are test
  data only.
* Tests that matter most: for every reading level, no projected output refers to a record above it (labels, counts,
  paths, suggestions, media, evidence); a mentioned person is never shown as present; no calendar date is derived
  from a narrative position; a fixture of a company or a team needs no person-only assumption.
* Persian text of the sample (decision 8).

Exit: the tests load the sample and answer every view question for the reading levels 0 to 4, without a web host.

### Phase 4 – `Vafadar.Web` and the first pages

Governing documents: 05, 09, 11, 18 (flows and screen contracts), 16.

* `Vafadar.Web`: language from the address, saved choice, `lang` and `dir`, theme tokens for light and dark,
  Syncfusion license registration, base layout parts, tests with bUnit.
* `Dastan.Web` host: composition root, routes, error and not-found pages, safe response headers, resources in English
  and Persian.
* Pages rendered on the server without a live connection: Discover, arc overview, entity overview.
* Comparisons with a written result (ADRs in the app): where interactive components are needed; graph renderer
  (decision 7); type scale, spacing and colour tokens, fonts for Latin and Arabic script.
* Test: two browser contexts with different languages and reading boundaries do not affect each other (M0-09).

Exit: a visitor can enter, read an entity and follow a labelled connection at 360 px, in English and Persian, in
both themes.

### Phase 5 – The connected journey

* Connections: focused graph, equivalent list, family view; selection by keyboard; bounded expansion.
* Story and time: narrative sequence and participant lanes from the same events; the frame is named.
* Gallery and viewer: the bundled illustration and video after an explicit move to level 2; captions, transcript,
  credit, representation and date labels; media are delivered only through a checked route.
* Evidence for the selected statement, with a notice before leaving to a source that reveals more.
* Search inside the collection; reading-boundary control (advance and lower); deep links that do not raise it.
* Context kept across views, reload, back and forward; the eight states of document 18 (empty, unknown time,
  incompatible frame, unavailable media, spoiler-limited, loading, recoverable failure, invalid link).

Exit: M0-02 … M0-07 pass in the running app and as browser tests.

### Phase 6 – Installable app, failures, accessibility

* Manifest, original provisional icons, scope and start address; ADR on the service worker (recommended: a small
  allow-list for the shell and an offline page, nothing from the catalogue).
* Connection lost and restored, update of an open page.
* Keyboard and focus order, reduced motion, text alternatives, contrast; 360 px, 412 px and a wide window; English
  and Persian; light and dark.
* The local demo is not indexable and serves nothing but the projection (M0-12).

Exit: M0-10 … M0-12 pass; installation is recorded for the browser available here, separately from manifest validity.

### Phase 7 – Verification and handoff

* `acceptance.md`: each of M0-01 … M0-12 as implemented and checked, implemented but unverified, or blocked with the
  cause.
* README with the exact restore, build, run and test commands; CHANGELOG; decision register; R0 gates.
* Report to the owner in Persian.

## 5. Acceptance conditions by phase

| Condition | Phase |
|---|---|
| M0-01 start from documented commands | 4, confirmed in 7 |
| M0-02 entry → entity → relation → entity → back | 5 |
| M0-03 graph and list show the same allowed relations | 3 (projection), 5 (screen) |
| M0-04 story and lanes agree | 3, 5 |
| M0-05 real image and video at level 2 | 5 |
| M0-06 level 0 and lowering remove later details everywhere | 3, 5 |
| M0-07 evidence matches the visible statement | 3, 5 |
| M0-08 time examples round-trip | 2 |
| M0-09 independent visitors | 4 |
| M0-10 widths, keyboard, themes, right to left | 4, 6 |
| M0-11 manifest, scope, failure and update behaviour | 6 |
| M0-12 only the demo projection is served | 3, 6 |

## 6. Risks

| Risk | Handling |
|---|---|
| Conflicts with parallel Zanance work in shared files | Own branches; additions only; rebase before each pull request |
| The private package feeds of the MAUI projects are missing on this machine | Dastan is built through its own solution filter; the full solution is left to CI |
| A graph component that needs a live connection for every gesture | Fixed comparison criteria in phase 4; the list view is always complete without it |
| Persian story text of uncertain quality | Marked "review pending"; shown only in the local demo; English fallback stays |
| The sample has no example of BCE or deep time | Covered by the tests of `Vafadar.Temporal`, not by invented story dates |
| Syncfusion Blazor packages at the pinned version | Checked when the first package is added; the license test is extended to the Blazor platform |

## 7. Not part of M0

Accounts, invitations, private and family spaces, public editing, a database, an editor for content, hosting, a
domain, final branding, more collections, museum images, offline reading. The model keeps room for them; none is
simulated.

## 8. After M0

R0 (public pilot): reviewed publication state, acquired and permitted media, correction and revision, usability
observation with first-time visitors, the phone and Windows browser matrix, hosting and operations, German. Then
relational storage with editorial tools, and the personal capabilities with their own authorization design.

## 8. Before a public pilot (R0)

M0 is a demonstration on a developer's machine. None of the following is done, and each is a decision or a piece of
work of its own; the order is not a plan.

| Gate | What is missing |
|---|---|
| Content is reviewed | The sample is a draft: its statements are "sourced, review pending", the Persian text is an unreviewed draft (D-037), and the collection is not approved for publication – which is why the public site shows nothing. A review, and a way to record a correction and a new revision |
| Media may be shown | The model knows whether an item may be shown in a demonstration, not whether it may be published. The museum images are references only: rights and delivery are open, and so is how a reference without a picture is shown |
| The public is an audience of the projection | **Done** (2026-11-17, D-044): only a published revision is public – numbered, with nothing but reviewed statements, reviewed texts and media allowed for the public ([ADR 0009](adr/0009-published-revision.md)). No collection is published: that needs the review of the content and the decisions on its media |
| Devices | No phone was used. Installing was not observed in any browser. Android, iOS and Windows each need their own check |
| Accessibility with people | A tool found no broken rule. Nobody has used the site with a screen reader, and no visitor has been watched using it |
| Identity | Palette, wordmark, icons and the Latin font are provisional (ADR 0004); the Persian spelling of the name is open |
| Hosting | No host, no domain, no certificate, no monitoring. The headers are set, but nothing was tested from outside this machine; the privacy matrix has to be read again against the host |
| Dependencies | Six automatic update proposals are open for the repository ([note](../../../../docs/dependency-updates.md)); the container of the browser tests and its package move together |
| Reading without a connection | Decided against for M0 ([ADR 0007](adr/0007-installable-app.md)); offering it needs its own decision on what may be kept |

## 9. After M0: charts (phase 8)

The owner's instruction of 2026-11-16 (D-039). How the charts are drawn is decided in
[ADR 0008](adr/0008-charts.md); this is the order of the work. A step is done when it is built, tested and, where
it is visible, checked in a browser in both languages.

| Step | What | Status |
|---|---|---|
| 8.1 | An axis of years in `Vafadar.Temporal`: years before and of the common era as places on one line, without a year zero | Done |
| 8.2 | The layout of a comparative timeline in `Vafadar.Dastan.Core`: lanes, periods packed into rows, single events with callouts, the marks of the axis, a stretch of time and a scale – for both directions | Done |
| 8.3 | Comparisons as content: a read model and a strict reader for a comparison of lanes with periods and events, each with its stated time, its sources and the state of its review; a first one, Iran, China and Rome from 600 BCE to 700 CE, as a draft | Done – [the format](comparison-format.md); the first comparison has no sources yet and is shown in the local demonstration only |
| 8.4 | The page of a comparison: the chart across the page and down it, scrolling in its frame, the stretch of time and the scale changed by links, the same facts as a table; reached from the start page | Done – `/compare/<id>`; evidence in [ADR 0008](adr/0008-charts.md) and in [the acceptance page](acceptance.md) |
| 8.5 | The family tree, second version: ordering inside a generation that reduces crossings, ancestors and descendants opened in steps, scrolling | Done – also the relatives beside the line, and a line of its own for every family; [ADR 0008](adr/0008-charts.md), addendum |
| 8.6 | Periods and dated events of the entities of a collection, so that a collection can offer its own timeline | Done – as step 9.4; [ADR 0008](adr/0008-charts.md), fourth addendum |
| 8.7 | The stage of a chart (D-040): the whole width of the page and the height of the window for a chart, and at each of its four sides a "+" that opens it further there and a "−" that closes it – for the timeline and for the family tree | Done – [ADR 0008](adr/0008-charts.md), addendum |

Not decided here: a script for pinching and dragging (ADR 0008, point 6), charts of numbers, a map.

## 10. The goal after the charts: every drawing is a chart on the stage (phase 9)

The owner asked on 2026-11-16 for one large goal that is worked through to its end (D-041). The goal: **every
drawing of the site is a chart on the stage – the whole page, opened and closed at its four sides (D-040) – and
the charts are tried in three browser engines.** Each step is built, tested, looked at in a browser in both
languages, documented and merged into `main` on its own, so that the work can stop after any step without leaving
something half done.

| Step | What | Status |
|---|---|---|
| 9.1 | The story as a chart: names in rows, the steps of the telling in columns, in a stage; the table stays beside it | Done – [ADR 0008](adr/0008-charts.md), second addendum |
| 9.2 | The browser tests run in Firefox and WebKit as well as in Chromium | Done – README, "Tests in a browser"; the record is in `docs/acceptance.md` |
| 9.3 | The connections of an entity as a chart on the stage, in place of the drawing of M0 (ADR 0005) | Done – [ADR 0008](adr/0008-charts.md), third addendum |
| 9.4 | The timeline of a collection from its own dated records – step 8.6. Dates are stored in the Gregorian calendar (D-043) | Done – `/<collection>/timeline`; [ADR 0008](adr/0008-charts.md), fourth addendum. The collection of the repository has no dated statements yet: they are content (D-042) |
| 9.5 | The official guide and tools for writing content (D-042): one guide to the formats that a person or an assistant can follow, and a command that checks a file against them and says what is wrong | Done – `docs/content-guide.md`, `docs/collection-format.md`, the command `Vafadar.Dastan.Content` (`check`, `vocabulary`, `template`); `ContentToolTests` |

When the goal is reached, the drawings that are left without a stage are none; what is left for a public pilot is
section 8, which needs the owner.

## 11. Towards a public pilot (phase 10)

Asked for by the owner on 2026-11-17: what is left, and a plan for it. Phase 9 is done; the rule of what everybody
may see exists ([ADR 0009](adr/0009-published-revision.md)). What is left before a first collection can be shown
to the public falls into two lists: work on the software, which can be done in the order below without waiting,
and decisions and work that are the owner's. A step is done when it is built, tested, looked at in a browser in
both languages where it is visible, documented and merged.

### The software, in this order

| Step | What | Why now | Status |
|---|---|---|---|
| 10.1 | **The record of a review and the history of revisions.** Who reviewed a statement and when, as part of the file; a command of the writers that makes a published revision out of a sample (`revision`: checks the rule, stamps number and day, refuses what is not ready); a public page "what changed" between two revisions (GOV-005) | ADR 0009 left it open, and without it "reviewed" is a word in a file | **Done** 2026-11-17: `review` on statements, `publication.history`, `ContentRevision` and the command `revision`, the page `/revisions` ([ADR 0009](adr/0009-published-revision.md), addendum) |
| 10.2 | **Disputed and withdrawn statements for the public** (GOV-004): a disputed statement is shown as disputed wherever it appears – list, chart, evidence page; a withdrawn one is named in the history of revisions with its reason | The rule lets disputed statements be published; the pages do not say so yet | Not started |
| 10.3 | **An image that may not be shown, as a reference.** A museum item without established rights appears as a card with its catalogue record and a link to the museum instead of nothing (section 8, "Media may be shown") | Three of the five items of the collection are such references and are invisible today | Not started |
| 10.4 | **The connections chart keeps the name in the middle in sight** when much is opened above it; and the side edges of a stage take less of a phone's width | The two limits recorded for the charts (ADR 0008) | Not started |
| 10.5 | **Running outside a developer's machine**: a container image of the site, a page that says the site is alive, the headers checked from outside the process, a written procedure for an update and for taking a revision back; the privacy matrix read against it | Needed for any host, independent of which one the owner chooses | Not started |
| 10.6 | **More than narrative things in a collection**: types for historical persons, organisations and real places, and relation types for them, each with its meaning – only as far as the first content asks for it | The guide names it as what the formats cannot say yet; it follows the content, not the other way round | Waits for the first content that needs it |
| 10.7 | **A store and the tools of the editors**: relational storage in place of the files, with review, correction and publication as work in an interface (document 20, after R0) | The files are enough for one collection written by hand; they are not enough for editors | Later – a phase of its own |

### The owner's

| What | What it needs | What it unblocks |
|---|---|---|
| **Review of the first collection** – 20 relations, 8 steps of the story, the sources and locators | A reader who knows the text; the result is written into the file as `reviewed` or `disputed` | A published revision at all |
| **The Persian text** (D-037) | A review of the draft translation, or the decision to publish in English first | Persian pages with Persian content for the public |
| **Rights of the media** | A decision per item: original content is clear; the three museum items need their rights established or stay references | Images on public pages |
| **Content with dates** – what is stated about the work with a date; sources for the comparison of Iran, China and Rome | Sources, then writing with the guide and the command (D-042) | A timeline that shows more than two diagrams; a comparison that can be published |
| **Identity** – palette, wordmark, icons, the Persian spelling of the name (ADR 0004) | A decision | The final look; the icons of the installable app |
| **Host, domain, certificate** | A choice and an account; nobody signs in on the owner's behalf | The site reachable from outside, and step 10.5 tried there |
| **Devices and people** – a phone (Android, iOS), installing the app, a screen reader, a first-time visitor | The devices and an hour with each | The claims the acceptance page marks as not checked |
| **The development machine** | Memory and room on drive C: for the browser tests (they stop Docker when both run out) | Reliable runs of the three browser engines |

### What is not planned

German texts (the owner asked for English and Persian), accounts and private spaces, public editing, reading
without a connection (ADR 0007), charts of numbers and maps, a script for pinching and dragging (ADR 0008, point 6).
Each needs a decision of its own before it becomes a step.

