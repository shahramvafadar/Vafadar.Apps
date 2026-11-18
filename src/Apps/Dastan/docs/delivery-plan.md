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
| Media may be shown | The model knows whether an item may be shown in a demonstration, not whether it may be published. The museum images are references only: rights and delivery are open, and so is how a reference without a picture is shown. *(2026-11-17: whether an item may be published is part of the model (ADR 0009), and a reference is shown as its catalogue record with a link, step 10.3; the rights of the three museum items stay the owner's decision.)* |
| The public is an audience of the projection | **Done** (2026-11-17, D-044): only a published revision is public – numbered, with nothing but reviewed statements, reviewed texts and media allowed for the public ([ADR 0009](adr/0009-published-revision.md)). No collection is published: that needs the review of the content and the decisions on its media |
| Devices | No phone was used. Installing was not observed in any browser. Android, iOS and Windows each need their own check |
| Accessibility with people | A tool found no broken rule. Nobody has used the site with a screen reader, and no visitor has been watched using it |
| Identity | Palette, wordmark, icons and the Latin font are provisional (ADR 0004); the Persian spelling of the name is open |
| Hosting | No host, no domain, no certificate, no monitoring. The headers are set, but nothing was tested from outside this machine; the privacy matrix has to be read again against the host. *(2026-11-17, step 10.5: the site runs as a container and was checked from outside its process on this machine – [operations](operations.md); a host, an address and a certificate are still open.)* |
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
| 10.2 | **Disputed and withdrawn statements for the public** (GOV-004): a disputed statement is shown as disputed wherever it appears – list, chart, evidence page; a withdrawn one is named in the history of revisions with its reason | The rule lets disputed statements be published; the pages do not say so yet | **Done** 2026-11-17: `DisputedMark` in lists, charts, tables and on the page of sources with what is disputed; `withdrawn` for relations and events, left out by the reader and named with its reason in the history ([ADR 0009](adr/0009-published-revision.md), second addendum) |
| 10.3 | **An image that may not be shown, as a reference.** A museum item without established rights appears as a card with its catalogue record and a link to the museum instead of nothing (section 8, "Media may be shown") | Three of the five items of the collection are such references and are invisible today | **Done** 2026-11-17: `MediaReference` and `Projection.References`, shown in the galleries with the catalogue record and the link to the holder, nothing of the image ([ADR 0006](adr/0006-media-delivery.md), addendum) |
| 10.4 | **The connections chart keeps the name in the middle in sight** when much is opened above it; and the side edges of a stage take less of a phone's width | The two limits recorded for the charts (ADR 0008) | **Done** 2026-11-17: the script of the site brings the name into sight once; up to 40 rem the steps of the left and right side stand in a row above the chart ([ADR 0008](adr/0008-charts.md), fifth addendum) |
| 10.5 | **Running outside a developer's machine**: a container image of the site, a page that says the site is alive, the headers checked from outside the process, a written procedure for an update and for taking a revision back; the privacy matrix read against it | Needed for any host, independent of which one the owner chooses | **Done** 2026-11-17: `Dockerfile`, `compose.site.yaml`, `/health` and `--probe`, trusted proxies, keys of the forms, `eng/Check-Site.ps1`, [operations](operations.md), the privacy matrix ([ADR 0010](adr/0010-running-the-site.md)). No host was used |
| 10.6 | **More than narrative things in a collection**: types for historical persons, organisations and real places, and relation types for them, each with its meaning – only as far as the first content asks for it | The guide names it as what the formats cannot say yet; it follows the content, not the other way round | **Done** 2026-11-17, on the owner's word and ahead of content – `Person`, `Organisation`, `Place`, `Region`; eight relation types of the world outside the story, each with its meaning and with the kinds of thing it holds between, which the readers enforce; the scope `outside_the_story` ([ADR 0001](adr/0001-domain-read-model.md), addendum). No collection uses them yet: what real content asks beyond them is added then |
| 10.7 | **A store and the tools of the editors**: relational storage in place of the files, with review, correction and publication as work in an interface (document 20, after R0) | The files are enough for one collection written by hand; they are not enough for editors | **Started** 2026-11-17 on the owner's word – section 12 |

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

Added on 2026-11-18: **a library that reads and writes images** – to make smaller sizes of an image that is
brought in (step 13.7) – is a dependency and needs the owner's decision; until then nothing is converted.

### What is not planned

German texts (the owner asked for English and Persian), accounts and private spaces, public editing, reading
without a connection (ADR 0007), charts of numbers and maps, a script for pinching and dragging (ADR 0008, point 6).
Each needs a decision of its own before it becomes a step.

## 12. The store and the tools of the editors (phase 11)

Step 10.7, started on 2026-11-17 when the owner asked for it. The decision is
[ADR 0011](adr/0011-editors-store.md) (D-045): the store is the working place of the editors – the records of a
collection with the history of their changes, in a SQLite file – and the public site keeps reading a published
revision from a folder. A step is done when it is built, tested, documented and merged; a step with pages is
looked at in a browser in both languages.

| Step | What | Status |
|---|---|---|
| 11.1 | **The store, and the file in and out.** The project `Vafadar.Dastan.Store`: collections, records, translations and the history of every change; `store import`, `store export`, `store history` in the command of the writers. What comes out reads as what went in | **Done** 2026-11-17 – [ADR 0011](adr/0011-editors-store.md) |
| 11.2 | **Changing one record in the store**, without a file in between: a record is replaced, added or taken out with who, when and why, and the whole collection is read by the readers before the change is kept – a change that makes the collection unusable is refused. Reviewing a statement, disputing it and withdrawing it as acts of their own, each with its record | **Done** 2026-11-17 – `Put`, `Remove`, `Review`, `Statements`; `store waiting`, `store review`, `store put`, `store remove` ([ADR 0011](adr/0011-editors-store.md), addendum) |
| 11.3 | **Publishing from the store**: revision *n* made from the store by the rule of ADR 0009, kept in the store as it was published, and written out as the content folder a site is given; the history of revisions comes from the store | **Done** 2026-11-17 – `Publish`, `Revisions`, `Revision`; `store publish`, `store revisions`, `store revision`; one maker of revisions for files and store ([ADR 0011](adr/0011-editors-store.md), second addendum) |
| 11.4 | **The tool of the editors, to read**: a second web app (`Vafadar.Dastan.Studio`) that runs on the editors' machine only – the collections, their records, what waits for a review, the history of a record with before and after | **Done** 2026-11-17 – `Vafadar.Dastan.Studio`: collections, records, a record with its history, what waits, the latest changes, the revisions; this machine only; reads, does not act ([ADR 0011](adr/0011-editors-store.md), third addendum) |
| 11.5 | **The tool of the editors, to work**: reviewing, disputing and withdrawing a statement, changing the texts of a record, publishing a revision – each a form that says what it will do and what it did | **Done** 2026-11-17 – forms for a review, a change of a record as text, taking out, adding and publishing; done leads back, refused says why and keeps the form ([ADR 0011](adr/0011-editors-store.md), fourth addendum) |
| 11.6 | **Forms for the parts of a record** (sources, associations of an event, a dated statement, a media item) in place of writing the record as text | **Done** 2026-11-17 – the page of a record has its fields as a form: a text, a number, a choice of the vocabulary of the readers, a reading stage, the records to point to, rows for a list of parts; made from the record and the vocabulary, with no description of a record of its own ([ADR 0011](adr/0011-editors-store.md), fifth addendum) |
| 11.7 | **Who an editor is**: signing in, and a store that several editors reach | **Done** 2026-11-17 (D-046: on a server, with signing in) – accounts of the store with a password of which only a derived value is kept, made by a command where the store lies; with `Studio:Accounts` the tool answers other machines, over an encrypted connection only, shows nothing to who has not signed in and keeps every act under the account ([ADR 0011](adr/0011-editors-store.md), seventh addendum) |
| 11.8 | **The texts of the other languages in the tool**: the Persian texts of a record beside its fields, so that a part that is translated can be changed or taken out without a file | **Done** 2026-11-17 – the text of a field in another language stands below the field and is kept with the record in one act; the role of a row goes with its row; the readers list what a translation may translate ([ADR 0011](adr/0011-editors-store.md), sixth addendum) |
| 11.9 | **The tool on a server**: its container, the store and the keys on a volume, a copy of the store that can be brought back, and how it is started, checked and updated | **Done** 2026-11-17 – the image of `Dockerfile.studio` with the store and the keys on a volume, `/health`, `store copy`, `eng/Check-Studio.ps1`, [operations of the tool](operations-studio.md); built, run and checked on this machine as a proxy would ask. A server, a proxy with a certificate and where the copies go are the owner's ([ADR 0011](adr/0011-editors-store.md), eighth addendum) |

Not part of the phase: a tool for visitors, uploading and converting media (document 9: needs an intake design),
comparisons in the store (they stay files until the store holds collections well), accounts of visitors.

## 13. Everything the model can say (phase 12)

Asked for by the owner on 2026-11-17 together with D-047: what the product documents name as possible is built
before content needs it, and the work goes on through the phases below without waiting. Phase 12 completes the
model and the formats; phase 13 the tool of the editors; phase 14 the public site; phase 15 accounts of
visitors and private spaces. A step is done when it is built, tested, documented and merged; a step with pages
is looked at in a browser in both languages where that can be done on this machine.

| Step | What | Status |
|---|---|---|
| 12.1 | **The whole vocabulary of things and relations** (DOM-002, DOM-003, DOM-004, DOM-007 to DOM-012): companies, brands and products; universities, governments, dynasties, families and peoples; countries; clubs, teams, competitions and seasons; events and periods of history; offices and honours; real objects; subjects of nature and of knowledge – and what is said between them: offices held, works translated, adapted and kept, identity that is not established, acquisitions, mergers and splits, governments and capitals, sport, events. Every type says what it means, has a name in English and Persian, and a relation type says between what kinds of thing it holds | **Done** 2026-11-17 – 31 entity types, 72 relation types; two more kinds of thing (an office or honour, a subject); the list for writers says what each type is ([ADR 0001](adr/0001-domain-read-model.md), second addendum) |
| 12.2 | **A relation in time** (DOM-007, TIME): a relation may say when it held – a period, a year, a season – as stated; two tenures are two relations and are never drawn as one; shown where the relation is shown and on the timeline | **Done** 2026-11-17 – `time` of a relation with the scope `outside_the_story`, written like the time of a dated statement; in the sentence of the relation, on its page of sources and as a bar or mark in the lane of its first entity on the timeline ([ADR 0001](adr/0001-domain-read-model.md), third addendum). In the tool of the editors the time is filled in with inputs since step 13.1 |
| 12.3 | **Names** (DOM-003): other names, earlier names with the time they were used, names in other scripts, abbreviations, and identifiers of other catalogues with who issued them; the search finds a thing by any of them | **Done** 2026-11-17 – `names` and `identifiers` of an entity; shown on its page, found by the search with the name that matched, a name never before its stage ([ADR 0001](adr/0001-domain-read-model.md), fourth addendum) |
| 12.4 | **What a collection is about: one story, several, or none – and a file written in another language than English** | **Done** 2026-11-17 – `subjectId`; several `NarrativeArc` with `storyId` on their steps and a page each; a collection about a company or a person with no story, no steps and no edition (`template subject`); `language` or `summaryLanguage` as any language tag, and the pages mark a text that exists in the language of the collection only ([ADR 0001](adr/0001-domain-read-model.md), fifth addendum) |
| 12.5 | **The Persian texts of dated statements and reading stages in the tool of the editors** | **Done** 2026-11-17 – the page `stages` of a collection in the tool: the names of the reading stages in every language, a stage added or the last one taken out; the form of an entity shows its statements with a date row by row with their texts in the other languages; a change of what is not a record is kept in the history under "the collection" ([ADR 0011](adr/0011-editors-store.md), ninth addendum) |
| 12.6 | **Comparisons in the store and in the tool** | **Done** 2026-11-17 – a comparison is a document of the store with its translations and the history of every change; `store comparison list|import|export|history|remove`; in the tool the list of comparisons, bringing one in, changing its file and translations in one act, taking it out ([ADR 0011](adr/0011-editors-store.md), tenth addendum) |
| 12.7 | **A quantity with its unit as a statement** (DOM-005): a population, a number of goals, a price – with its precision, its time and its source | **Done** 2026-11-17 – `quantity` of a statement about an entity: a number, one of 22 units with a meaning and words in both languages, "about", a currency for money; with or without a time; the page of an entity lists what is stated about it ([ADR 0001](adr/0001-domain-read-model.md), sixth addendum) |
| 12.8 | **Where a place is**: coordinates of a real place as stated, with how exact they are – the ground for a map (phase 14) | **Done** 2026-11-17 – `position` of a real place: latitude, longitude and `exact`, `approximate` or `centre_of_area`; refused for a place of a story and for what is no place; the page of the place writes it and links it as a `geo:` position a device opens with its own map ([ADR 0001](adr/0001-domain-read-model.md), seventh addendum) |

## 14. The tool of the editors, completed (phase 13)

Phase 12 is done: the model and the formats say what the product documents name. Phase 13 completes the tool
of the editors (`Vafadar.Dastan.Studio`) so that nothing of the daily work needs a file or a command.

| Step | What | Status |
|---|---|---|
| 13.1 | **A time as a form**: the time of a statement with a date and of a relation of the world filled in with inputs – once, between two times, lasting, with "about" and an unknown beginning or end – in place of writing it as it stands in the file | **Done** 2026-11-17 – `TimeForm`; a relation of the world that states no time has the inputs to state one ([ADR 0011](adr/0011-editors-store.md), eleventh addendum) |
| 13.2 | **Two editors at one record**: a change made from a form that was opened before somebody else changed the record is not kept silently; the editor is told and sees what changed | **Done** 2026-11-17 – every form that changes something carries a mark of what it was made from; when the store holds something else by the time the form comes back, nothing is kept, the editor is told and keeps the inputs ([ADR 0011](adr/0011-editors-store.md), twelfth addendum) |
| 13.3 | **Who may do what**: writers, reviewers, publishers and who looks after the accounts – an act is refused to who has not its role | **Done** 2026-11-17 – an account has any of the roles `writer`, `reviewer`, `publisher` (all three unless said otherwise; existing accounts keep all); a form is on the page for who has its role and cannot be sent by who has not; `store editor add … --roles`, `store editor roles` ([ADR 0011](adr/0011-editors-store.md), thirteenth addendum) |
| 13.4 | **An editor changes the own password in the tool; who signed in when** | **Done** 2026-11-17 – the page `account`: what the account may do, another password proved by the present one, and the last times its password was tried with whether it was right ([ADR 0011](adr/0011-editors-store.md), thirteenth addendum) |
| 13.5 | **A new record and a new language without a file**: a record added from a form that starts with the fields of its kind; a language a collection is not translated into yet is added in the tool | **Done** 2026-11-18 – `<collection>/records/<group>/new` for sources, entities, relations and events: the identifier and the fields of the kind, a relation of the world with the inputs of its time; "Add a language" on the page of a collection ([ADR 0011](adr/0011-editors-store.md), fourteenth addendum). An image or a video is brought in with its file – step 13.7 |
| 13.6 | **Two entities that are one** (DOM-003): merging an entity into another as an act with a reason and a trail; the name of the merged one stays findable; a merge the collection cannot bear is refused | **Done** 2026-11-18 – `EditorialStore.MergeEntities` and "The same as another name" on the page of an entity ([ADR 0011](adr/0011-editors-store.md), fifteenth addendum). An entity that turns out to be two is split by adding the second with the form and pointing the relations that are its to it |
| 13.6b | **A comparison as a form**: the title, the sources and, lane by lane, the periods and events of a comparison filled in as rows with the inputs of a time | **Done** 2026-11-18 – the page of a comparison has the form of what it says as a whole and the list of its lanes; a lane has a page with its periods and events as rows; a lane is added and taken out; and a list that has no row yet – the sources of a comparison, the other names, identifiers and statements of an entity – begins with one ([ADR 0011](adr/0011-editors-store.md), sixteenth addendum) |
| 13.7 | **Images and videos brought in through the tool**: a file is taken in, checked, converted to the sizes the site delivers and kept with its record (ADR 0006) | **Done** 2026-11-18, without the converting – a new image or video is brought in with its file and what the editor states about the right to show it; a file for a part of an item (its file, a poster, captions in a language, a transcript, a further rendition) on the page of the item; what a file is is read from its content, its name is made by the tool, an image loses what it says about its making, and a record that is refused leaves no file ([ADR 0006](adr/0006-media-delivery.md), second addendum). **Not built: making smaller sizes of an image.** That needs a library that reads and writes images, which is a dependency the owner decides on; until then an editor brings a rendition in as a file of its own |

## 15. The public site, completed (phase 14)

Phase 13 is done: the daily work of the editors needs no file and no command. Phase 14 gives the public site
(`Vafadar.Dastan.Web`) the views the product documents name and the model can already answer (D-047). The
list comes from comparing the documents `docs/product/02`, `05`, `10`, `14`, `15`, `16` and `18` with the
pages of the site on 2026-11-18.

| Step | What | Status |
|---|---|---|
| 14.1 | **The places of a collection on a map** (DOM-011): the real places drawn where they lie to each other, and listed with what is stated to be there | **Done** 2026-11-18 – the page `<collection>/places`: a grid of degrees with a numbered mark for each place, the list that says the same, and the real places that state no position apart; offered on the page of a collection that names a real place. The site loads no map from anybody, so the drawing has no coasts and no borders ([ADR 0008](adr/0008-charts.md), sixth addendum) |
| 14.2 | **What held at a time** (R-PUB-03): a year is chosen, and the page says which relations of the world held then, what was stated of each entity for that year and where the people were | **Done** 2026-11-18 – the page `<collection>/at?year=…`: a form that asks for a year and its era; the relations that held then, those that end at a place apart ("who and what was where"), and what is stated of an entity for that year; what the records leave open is said to "may have held"; steps to the nearest years in which something begins or ends ([ADR 0001](adr/0001-domain-read-model.md), eighth addendum). A year is asked for, not a day |
| 14.3 | **A series of quantities as a chart**: the numbers stated of one entity in one unit over the years, drawn and listed | **Done** 2026-11-18 – on the page of an entity, under what is stated of it: statements of a quantity that are called the same, counted in the same unit and placed among the years are one series, drawn from zero with time to the right; a mark leads to the row of its statement ([ADR 0008](adr/0008-charts.md), seventh addendum) |
| 14.4 | **Pages that can be found and passed on** (R-PLAT-04, B-013): a description, a canonical address and a preview for each page; a sitemap and `robots.txt` for a public site; pages that print | **Done** 2026-11-18 – every page names its own address and its address in each language; the start page and the pages of a collection, an entity and a comparison say what they are about; `robots.txt` and, for a public site, `sitemap.xml` with what a visitor sees who has read nothing yet; the answer to a search is not kept by an engine; rules for paper in the style sheet ([ADR 0003](adr/0003-server-rendered-pages.md), third addendum). **Not built: a picture for the preview of a link** – it would have to be drawn for each page |
| 14.5 | **Finding by kind, place and time** (R-PUB-04) and **galleries by place and period, sorted and filtered** (R-MED-02, MED-003) | **Done** 2026-11-18 – the page of the search asks, beside a name or without one, for a kind of thing, a place and a year of the common era, each only when the collection has something to choose from; a gallery is put in the order of the making, the oldest or the newest first, and shows images or videos only ([ADR 0002](adr/0002-one-projection.md), addendum of 2026-11-18). A gallery of a place is the gallery of that entity, as before. **Not built: a gallery of a period or of a theme** – an item says when it was made, not which period it shows, and the model has no themes |
| 14.6 | **A moment of a video has an address** (MED-014, SC-19) | **Done** 2026-11-18 – `<collection>/media/<id>?t=1:15` (or seconds): the page hands the moment to the player of the browser with the address of the file, without a script; the page says where the video starts and offers its beginning; a form asks for the page of a moment ([ADR 0006](adr/0006-media-delivery.md), third addendum). A moment is not checked against the length of the video – the record states none |
| 14.7 | **A statement is cited as it was** (R-PUB-07): the address of a statement in the published revision it was read in, with the words to cite it | **Done** 2026-11-18 – the page of the sources of a statement on a published site says how to cite it: what it is, the number and day of the revision, and the address with `?revision=<n>`; an address written for another revision than the one shown says so and leads to what changed ([ADR 0009](adr/0009-published-revision.md), addendum of 2026-11-18). **Not built: showing an earlier revision** – the site has the revision it was given and the list of what changed, not the earlier records |
| 14.8 | **A visitor reports an error** (R-GOV-03, B-015, SC-14): from a statement to a report the editors find in their tool | **Done** 2026-11-18 – a form on the page of the sources of a statement; the site keeps the statement, the words and the day in a folder it was told and nothing about the visitor; the tool lists what waits with the way to its record and notes who looked; a report changes no record ([ADR 0012](adr/0012-visitor-reports.md)). **The owner's:** how long reports that were looked at are kept |
| 14.9 | **Threads** (R-PUB-06, R-EXP-04, B-025, SC-15): where to go on from an entity, each with its reason; threads the editors lay | **Done** 2026-11-18 – a collection may have `threads`: a few stops in an order – an entity, a step of a story, an image or video – each with the reason it follows; the page `<collection>/thread/<id>` shows a thread as far as the visitor has read; the page of a collection lists the threads, and the page of a name says where a thread leads on from it and why. The store and the tool of the editors know the group ([ADR 0001](adr/0001-domain-read-model.md), ninth addendum). The form of a thread in the tool has the reason of each stop in the other languages beside it. **Not built: suggestions the site works out by itself** – a reason has to be an editor's sentence |
| 14.10 | **Several collections on one site**: every collection in the content folder has its address; the start page lists them | **Done** 2026-11-18 – the site reads every collection of the folder `samples`, the one it was told first; each has its pages under its own name, its own reading position, its own search and its own files; the start page lists them; a report is kept under the collection of its statement ([ADR 0010](adr/0010-running-the-site.md), addendum of 2026-11-18). The start page of a site with several collections has a search that looks for a name in all of them (`search`), each in what the visitor has read of it |
| 14.11 | **A reading position for each story** and **switching the edition** (R-STORY-02, FLOW-06) | **Done in part** 2026-11-18 – a collection names the other collections of the site that are about the same world – another edition, another telling – and leads to them; each keeps its own reading position, and the page says so ([ADR 0010](adr/0010-running-the-site.md), second addendum of 2026-11-18). A reading position is carried where the editors state it: a collection may say, stage by stage, which stage of another telling a reader of its own stage has read (`spoilerPolicy.correspondences`); the page then offers to go on there from that stage – only forward, and only by the visitor's own act (third addendum of 2026-11-18). **Not built:** a reading position for each story of one collection – the model has one line of stages per collection, and whether reading one story reveals something of another is a statement of a form nobody has decided |

Phase 14 is done as far as it can be without a decision: the public site has the views the product documents
name for it. What is left of it is named in the rows above as not built, each with what it would need – a
picture for the preview of a link, a gallery of a period or a theme, an earlier revision shown, suggestions
the site works out by itself, a search across collections, and a reading position that is carried between
editions or kept for each story.

## 16. What is a visitor's own (phase 15)

The product documents name capabilities that belong to one person: saved views, accounts, invitations, private
and family spaces, personal photographs ([document 06](product/06-private-spaces-consent-and-privacy.md),
B-016 to B-020). Most of them keep data about people and need decisions that are the owner's – which the
document itself lists as open (Q-05, Q-06, Q-07, Q-13, Q-14). Phase 15 builds what needs none of them, and
names the rest with the decision each waits for.

| Step | What | Status |
|---|---|---|
| 15.1 | **A list of one's own** (B-016): pages a visitor keeps, without an account | **Done** 2026-11-18 – a button on the page of a name, an image or video and a thread keeps the page in "My list"; the list is a cookie on the visitor's device, at most 30 pages, for a year; the site keeps nothing of it; a page that cannot be opened any more is neither shown nor counted ([ADR 0003](adr/0003-server-rendered-pages.md), fourth addendum) |
| 15.2 | **The rules of private spaces as a library**: who may see a relationship, a tag, an affiliation – the accepted rules of document 06, checked by its acceptance examples, before any account exists | **Done** 2026-11-18 – `Vafadar.Dastan.Core.Personal`: scopes, relationships that need the other side's acceptance and both sides' audiences, invitations, claims that somebody else confirms, being found through an organisation and a year, tags that widen no audience, the check before something is made public; the ten acceptance examples of the document are its tests. Nothing reaches the library: no page, no store, no account ([ADR 0013](adr/0013-private-space-rules.md)) |
| – | **Accounts of visitors, invitations, private and family spaces, personal photographs, copies for use without a connection** | **Not built – the owner's decisions first**: who may have an account and how an identity is proved (Q-06); what is published about living people (Q-05); shared family trees (Q-07); what happens when consent ends (Q-14); copies without a connection (Q-13); and, with any of them, where data about people is kept, who is responsible for it, and how an invitation is sent – a mail service is a third party |

### What waits for the owner (after phases 12 to 16)

Everything the product documents name and that can be built without a decision of the owner is built. What is
left waits for one of these – each is a question of the owner's, not of code:

| Decision | What it opens |
|---|---|
| ~~Accounts of visitors, what is published about living people, shared family trees, the end of consent~~ | **Decided on 2026-11-18** (D-048 to D-055): phase 16 below |
| Copies without a connection (Q-13); children and guardians (PRIV-003) | The rules the library leaves out |
| A mail service for invitations | Sending an invitation – a third party |
| A library that reads and writes images | Smaller sizes of an image that is brought in (step 13.7); a picture for the preview of a link (step 14.4) |
| A map: coasts and borders that may be shipped, or tiles from a host of the owner's | A background for the drawing of the places (step 14.1) |
| The form of a statement that connects reading stages – between two editions, between the stories of one collection | A reading position that is carried between editions, and one for each story (step 14.11) |
| How long reports of visitors that were looked at are kept | The day to run `reports forget` with: the command exists and deletes nothing by itself (step 14.8) |
| The owner's part of a public pilot (section 11): the review of the content, the rights of the images, the host, the devices, the people | A public site |
| The site registered with Google and with Microsoft, and the secrets entered by the owner (D-048) | The first real sign-in through Google or Microsoft: step 16.8 is built and waits for nothing else. Until it was tried once with each, it is not known to work with them |
| Whether a photograph its owner shows to everybody needs the agreement of the people in it (D-053 read strictly) | A check before "everybody" for photographs, like the one for family trees (steps 16.5, 16.6) |
| Whether a public family tree may be found by search engines | Taking "stay away" off the public page of a tree (step 16.6) |
| The kinds of relationship (friend, close friend, family, partner, colleague, classmate – a proposal) | Another list, or kinds the editors can change (step 16.3) |
| A legal review of accounts and private spaces: terms, age, the law of the place, records of living people who were not asked | A site with accounts for the public |
| Where the store of the visitors and their photographs are kept, backed up and brought back (a host) | Accounts on a site that is not a development machine: nothing of phase 16 was run in a container |

## 17. Accounts of visitors and private spaces (phase 16)

On 2026-11-18 the owner decided what phase 15 waited for (D-048 to D-055): visitors sign in with a name and a
password or with Google or Microsoft; anybody may register and invite others; accounts and private spaces are
kept in a store of their own beside the site; an account opens all private spaces; the open questions Q-05,
Q-06, Q-07 and Q-14 are answered. The rules exist as a library (step 15.2); phase 16 gives them a store, a
sign-in and pages.

| Step | What | Status |
|---|---|---|
| 16.1 | **The store of the visitors and an account with a password**: a SQLite file of its own; registering, signing in and out, another password, recovery codes, closing the account; limits against misuse | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md). The project `Vafadar.Dastan.People`, the setting `Dastan:People`, the page `/account`. Without the setting the site is as before. Looked at in a browser (en 800 px, fa 375 px, dark); not tried in a container |
| 16.2 | **What follows the visitor**: the list and the reading position kept with the account and brought together with what the device held; **invitations** as a code or link the sender passes on | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), first addendum. Signed in, the reading position and the list are the account's on every device; a device keeps its own for who is signed out; a new account begins with what its device holds, a sign-in takes nothing until its visitor says so. An invitation makes an account for one person and nothing else. Looked at in a browser (en and fa, 375 px, dark) |
| 16.3 | **Relationships two people show together**: request, acceptance, both sides' audiences, ending (ADR 0013), with circles to name an audience | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), second addendum. The pages `/account/people` and `/people/{id}`; what an act may do and who sees a relationship is decided by the rules of step 15.2. Looked at in a browser with two accounts (en and fa, 375 px, dark) |
| 16.4 | **Records of people and family trees**: records without an account, a tree with its owner and those who may change or see it, the history of every change; the claim of an account on a record, confirmed by who made the record | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), third addendum. **Done**: trees with an owner, records without an account, parents and partners, who may see and who may change, every change with who made it; the pages `/account/trees` and `/account/trees/{id}` (looked at in a browser, en and fa, 375 px, dark). **Done** too: the claim of an account on a record, confirmed or refused by who made the record (D-052; fourth addendum; not looked at in a browser). **Done** too: the drawing of a tree, with the layout and the drawing of the family of a collection ([ADR 0008](adr/0008-charts.md), ninth addendum; looked at in a browser, en 800 px and fa 375 px, dark) |
| 16.5 | **Personal photographs**: brought in like the files of the editors, with their audience, tags that the tagged person approves, the life of a person, recent shares | **Done** 2026-11-18 for still images – [ADR 0014](adr/0014-visitor-accounts.md), seventh addendum. The setting `Dastan:Photos`, the page `/account/photos`, the address `/photo/{id}` that asks the audience, photographs on the page of a person. Looked at in a browser with two made-up pictures (en and fa, 375 px, dark). **Not built**: videos of visitors, smaller renditions of a picture, albums, a gallery by the time a picture was taken |
| 16.6 | **Making something public**, with the consent of everybody it names and the check before publication | **Done** 2026-11-18 for family trees – [ADR 0014](adr/0014-visitor-accounts.md), eighth addendum. The owner asks, the check says who stands in the way, every living person the tree names agrees with their own account, the owner makes it public at `/trees/{id}`; agreement taken back hides it at once. **Not looked at in a browser.** **Open for the owner**: a photograph its owner shows to everybody is not held back by the people in it (they can take their tag away, not the picture); whether a public tree may be found by search engines (it asks them to stay away); the legal review |
| 16.7 | **Withdrawing and closing**: hidden at once; a closed account deleted after 30 days; a copy of one's own data | **Done** 2026-11-18 for what exists – [ADR 0014](adr/0014-visitor-accounts.md), fifth addendum. Closing and deleting came with step 16.1, and everything built since is hidden with a closed account and deleted with it; an audience, a claim, a place in a tree are each taken back by the account and hold at once. New here: the copy of one's own data as a file. Photographs (step 16.5) joined both: the copy names them, a closed account shows none, and the files of a deleted account are deleted. The form was not looked at in a browser |
| 16.8 | **Signing in with Google or Microsoft** | **Built** 2026-11-18, **not tried against Google or Microsoft** – [ADR 0014](adr/0014-visitor-accounts.md), ninth addendum. A provider is offered only where the owner entered a client of it (`Dastan:SignIn:Google`, `Dastan:SignIn:Microsoft`); the visitor chooses the name of the account; of what the provider says only a hash of its identifier is kept; an account adds and removes providers and keeps a way in. Checked with a made-up provider and up to the address the visitor is sent to; the page looked at in a browser (fa, dark, with made-up clients). The first real sign-in needs the site registered with each of them and the secrets, which the owner enters |

What phase 16 built and what it left, in one place (2026-11-18): steps 16.1 to 16.7 are done and merged; step
16.8 is built and waits for the owner's registration to be tried. Each step says in its row what was looked at in a browser and what was not – the
forms of step 16.6, the claims of step 16.4 and the copy of one's own data were not. Nothing of the phase was
run in a container, with a screen reader, in the light appearance or with real photographs of a camera.

Not in this phase: sending mail (no mail service is decided), copies for use without a connection (Q-13 is
open), children and guardians (PRIV-003 needs a rule of the owner's).
