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
Decided the same day: SkiaSharp (D-056, step 17.3).

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
| ~~Copies without a connection (Q-13); children and guardians (PRIV-003); a mail service; a library for images; a map~~ | **Decided on 2026-11-18** (D-056 to D-069, "everything as proposed"): phase 17 below |
| ~~The form of a statement that connects reading stages between the stories of one collection~~ – decided as D-058 (step 17.7): a collection per story | A reading position for each story of one collection (step 14.11). **What works today**: each story as a collection of its own in the same world, with `spoilerPolicy.correspondences` between them – each then has its own stages and the position is carried where the editors state it. Proposed to the owner on 2026-11-18: keep that, or give one collection a ladder per story – a change of the model of what a reader may see, not built unasked |
| How long reports of visitors that were looked at are kept | **Proposed and built as a default** (step 16.11): 90 days, which `reports forget <folder>` uses when no other number is named. The owner may name another; nothing runs the command by itself |
| The owner's part of a public pilot (section 11): the review of the content, the rights of the images, the host, the devices, the people | A public site |
| The site registered with Google and with Microsoft, and the secrets entered by the owner (D-048) – **the owner does this last, before the site is deployed** (said on 2026-11-18) | The first real sign-in through Google or Microsoft: step 16.8 is built and waits for nothing else. Until it was tried once with each, it is not known to work with them |
| ~~Whether a photograph its owner shows to everybody needs the agreement of the people in it~~ | **Built as D-053 says it** (step 16.9): a photograph that names somebody is shown to everybody only when they agreed. For people a photograph shows and does not name, its owner says when choosing "everybody" that they agree (step 16.11, a proposal the owner may change). A way for such a person to object came with step 17.6. Left for the owner: who looks at objections that are made to do harm – the site has nobody for that |
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
| 16.4 | **Records of people and family trees**: records without an account, a tree with its owner and those who may change or see it, the history of every change; the claim of an account on a record, confirmed by who made the record | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), third addendum. **Done**: trees with an owner, records without an account, parents and partners, who may see and who may change, every change with who made it; the pages `/account/trees` and `/account/trees/{id}` (looked at in a browser, en and fa, 375 px, dark). **Done** too: the claim of an account on a record, confirmed or refused by who made the record (D-052; fourth addendum; looked at in a browser on 2026-11-18: "This is me", the claim that waits and its review, fa 375 px, dark). **Done** too: the drawing of a tree, with the layout and the drawing of the family of a collection ([ADR 0008](adr/0008-charts.md), ninth addendum; looked at in a browser, en 800 px and fa 375 px, dark) |
| 16.5 | **Personal photographs**: brought in like the files of the editors, with their audience, tags that the tagged person approves, the life of a person, recent shares | **Done** 2026-11-18 for still images – [ADR 0014](adr/0014-visitor-accounts.md), seventh addendum. The setting `Dastan:Photos`, the page `/account/photos`, the address `/photo/{id}` that asks the audience, photographs on the page of a person. Looked at in a browser with two made-up pictures (en and fa, 375 px, dark). Albums and a gallery by the year a picture was taken came with step 16.10. **Not built**: videos of visitors (a video is kept as it is, with what it says about where it was made – taking that out needs a library) and smaller renditions of a picture (the same library, which the owner decides on) |
| 16.6 | **Making something public**, with the consent of everybody it names and the check before publication | **Done** 2026-11-18 for family trees – [ADR 0014](adr/0014-visitor-accounts.md), eighth addendum. The owner asks, the check says who stands in the way, every living person the tree names agrees with their own account, the owner makes it public at `/trees/{id}`; agreement taken back hides it at once. The request and the list of who stands in the way were looked at in a browser (en, 375 px, dark); the agreement of a person and the public page were not. **Open for the owner**: a photograph its owner shows to everybody is not held back by the people in it (they can take their tag away, not the picture); whether a public tree may be found by search engines (it asks them to stay away); the legal review |
| 16.7 | **Withdrawing and closing**: hidden at once; a closed account deleted after 30 days; a copy of one's own data | **Done** 2026-11-18 for what exists – [ADR 0014](adr/0014-visitor-accounts.md), fifth addendum. Closing and deleting came with step 16.1, and everything built since is hidden with a closed account and deleted with it; an audience, a claim, a place in a tree are each taken back by the account and hold at once. New here: the copy of one's own data as a file. Photographs (step 16.5) joined both: the copy names them, a closed account shows none, and the files of a deleted account are deleted. The form was looked at in a browser (fa, 375 px, dark); the file it gives was not opened there |
| 16.8 | **Signing in with Google or Microsoft** | **Built** 2026-11-18, **not tried against Google or Microsoft** – [ADR 0014](adr/0014-visitor-accounts.md), ninth addendum. A provider is offered only where the owner entered a client of it (`Dastan:SignIn:Google`, `Dastan:SignIn:Microsoft`); the visitor chooses the name of the account; of what the provider says only a hash of its identifier is kept; an account adds and removes providers and keeps a way in. Checked with a made-up provider and up to the address the visitor is sent to; the page looked at in a browser (fa, dark, with made-up clients). The first real sign-in needs the site registered with each of them and the secrets, which the owner enters |
| 16.9 | **A photograph that names people is shown to everybody only with their agreement** (D-053) | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), tenth addendum. Its owner chooses "everybody"; until every person it names agreed with their own account, its owner and they alone see it, and the owner is told who is waited for. An agreement taken back hides it at once; one given to a showing is asked again for the next. Looked at in a browser with two accounts (the owner in en, the person asked in fa; 375 px, dark) |
| 16.10 | **Albums and a gallery by time** (R-MED-07): photographs of one's own under a name; the years photographs were taken in | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), eleventh addendum. An album has no audience: each photograph keeps its own, and an album in which a viewer may see nothing is not there for them. Albums stand on the page of their owner; the years stand above the photographs of an account and of a person. Looked at in a browser (fa and en, 375 px, dark); the years not, the preview had photographs of one year |
| 16.11 | **Two proposals to the owner, built so that they can be changed**: who shows a photograph to everybody says that the people in it agree; reports that were looked at are kept 90 days unless another number is named | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), twelfth addendum; [ADR 0012](adr/0012-visitor-reports.md), addendum. The owner was asked on 2026-11-18 and had not answered when this was built: both are the recommended option, and both are one place in the code to change. The refusal of a photograph looked at in a browser (fa, 375 px, dark) |

What phase 16 built and what it left, in one place (2026-11-18): steps 16.1 to 16.7 are done and merged; step
16.8 is built and waits for the owner's registration to be tried. Each step says in its row what was looked at in a browser and what was not – the
agreement of a person to a public tree, the public page of a tree and the form that chooses a name after a
sign-in through a provider were not. Nothing of the phase was
run in a container, with a screen reader, in the light appearance or with real photographs of a camera.

Not in this phase: sending mail (no mail service is decided), copies for use without a connection (Q-13 is
open), children and guardians (PRIV-003 needs a rule of the owner's).

## 18. What the owner decided after phase 16 (phase 17)

On 2026-11-18 the owner answered the list of what waited for a decision with "everything as proposed"
(D-056 to D-069). What that opens, in the order it is built:

| Step | What | Status |
|---|---|---|
| 17.1 | **Small rules**: "other" among the kinds of relationship (D-063); an account is for people of 16 and older (D-061); a site that takes new accounts by invitation only, for the closed trial (D-068) | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), thirteenth addendum. The setting `Dastan:InvitationOnly`; the tick "I am 16 or older" on both forms that make an account. Not looked at in a browser |
| 17.2 | **Coasts on the drawing of the places** from Natural Earth, shipped with the site (D-057) | **Done** 2026-11-18 – [ADR 0008](adr/0008-charts.md), tenth addendum. Coasts, and with them rivers and the shores of lakes: the places of the sample lie far from any sea, and coasts alone showed nothing there. No borders. Looked at in a browser (en, 800 px, dark, with a made-up place on the Caspian shore) |
| 17.3 | **Smaller sizes of a photograph** with SkiaSharp (D-056) | **Done** 2026-11-18 for the photographs of visitors – [ADR 0006](adr/0006-media-delivery.md), addendum on reading and writing images. A photograph longer than 800 pixels gets a smaller size beside it, and the pages name both; a picture a camera took turned is taken in upright – for the editors' files too. Smaller sizes of the editors' images came with step 17.8. Not looked at in a browser; the library was not run in a container (the tests of the pipeline run it on Linux) |
| 17.4 | **A picture for the preview of a link** (D-056, step 14.4) | **Done** 2026-11-18 – [ADR 0003](adr/0003-server-rendered-pages.md), addendum on the picture of a link. One picture for every page: the mark of the site on its ground, drawn by the site at `/preview.png`; a page names it with its size. **Not built**: a picture with the words of the page on it – the program has no font for Latin letters and none it could draw Persian with, and fonts in the program are not decided. Looked at in a browser |
| 17.5 | **Public content without a connection** (D-064) | **Done** 2026-11-18 – [ADR 0007](adr/0007-installable-app.md), addendum on what a device keeps. A public page a visitor who is not signed in has read is kept by their device and shown when the site cannot be reached; nothing of an account or a private space is. Tried in a browser with the site stopped: a page that was read came back, the account and a page that was not read gave "no connection" |
| 17.6 | **A way to object to a photograph** for somebody it shows and does not name (D-059) | **Done** 2026-11-18 – [ADR 0014](adr/0014-visitor-accounts.md), fourteenth addendum. Who is signed in says under a photograph everybody sees that it shows them; it is held back at once, its owner sees who said so and cannot take the word back, and only who objected lets it go. Nobody looks at objections: an account may have twenty standing at a time, and more than that is left to the owner of the site. Not looked at in a browser |
| 17.8 | **Smaller sizes of the editors' images** (D-056, step 13.7) | **Done** 2026-11-18 – [ADR 0006](adr/0006-media-delivery.md), second addendum on reading and writing images. The tool makes two smaller sizes of an image that is brought in (480 and 1200 pixels), the record names them (`sizes`), and the page of the image and the gallery tell the browser all sizes. Images that are in a collection already have none until their file is brought in again. Not looked at in a browser |
| 17.7 | **A line of reading stages for each story**, with correspondences the editors state (D-058) | **Done with what the model has; no ladder per story inside one collection** – [ADR 0010](adr/0010-running-the-site.md), addendum on a line for each story. Each story is a collection of its own in the same world: its own stages, its own reading position, a way to the other stories, and a position carried only where the editors state a correspondence. That is what was proposed to the owner ("it uses the model that is there") and decided. **Not built**: several ladders inside one collection – one record would have to say for each story when it may be seen, which changes what every page asks before it shows anything |

Already as decided and nothing to build: no search engines for public trees (D-062), no mail (D-065), no videos
of visitors (D-066), 90 days for reports (D-060, step 16.11), the owner's statement about the people in a
photograph (D-059, step 16.11). The owner's own: the host (D-067), the legal review (D-068), the registration
with Google and Microsoft (D-069), the review of the content and the rights of the images.

## 19. What is left (phase 18)

Written on 2026-11-18, after phase 17, as the list a goal can be set on: every row is either done – built,
checked, written down and merged – or says here why it was not. The groups are in the order they are worked
through.

### 18.1 – Run in a container (nothing has been so far)

| Row | What | Status |
|---|---|---|
| 18.1.1 | The tests of Dastan on Linux in a container (the Compose project `test`), as the pipeline runs them | **Done** 2026-11-18 – `dotnet test --solution Vafadar.Dastan.slnf --configuration Release` in `mcr.microsoft.com/dotnet/sdk:10.0`, on a copy of the repository made with `git archive`: 2250 passed, 328 skipped (the browser tests), none failed – the library for images among them. Done because the pipeline failed in its step "Test" for the commit of steps 17.6 to 17.8 and its log cannot be read without signing in; the tests of Dastan are not what failed there |
| 18.1.2 | The site and the tool of the editors built as images and run, with volumes for the store of the visitors, their photographs and the keys | **Done** 2026-11-18 – `compose.trial.yaml` (new): the image of the site with one volume at `/data`; the Dockerfile makes that folder the container user's. The site refused the forms of an account over plain http from outside the container, as it is meant to; asked as a proxy would, it took them ([operations](operations.md), "A closed trial in a container"). The tool of the editors was built as its image and started with `compose.studio.yaml`, and `eng/Check-Studio.ps1` passed every check against it; bringing an image in through the tool was not tried in the container – it needs an account of an editor, whose password a person types |
| 18.1.3 | In that container: a photograph brought in, its smaller size made, `/preview.png` drawn (the library for images) | **Done** 2026-11-18 – a picture of 1200 by 630 brought in, given whole and as 800 by 420; `/preview.png` drawn; a stranger given nothing of the photograph |
| 18.1.4 | The store of the visitors and their photographs backed up and brought back, and the way written into `operations.md` | **Done** 2026-11-18 – backed up with the site stopped, the volume removed, brought back into a new one: both accounts signed in, the photograph and its smaller size were there ([operations](operations.md), "Backing up what the site keeps") |
| 18.1.5 | A closed trial from the first account to an invited one, in the container (`Dastan__InvitationOnly`) | **Done** 2026-11-18 – the first account made with the site open; started again with invitations only: the page says so and has no form, a form sent all the same makes nothing, an invited visitor gets an account, the same invitation makes no second |

A machine that is short of memory or of room on its system drive stops Docker and the containers of other
projects with it: both are looked at before each run, and a group that cannot run is written down as blocked.

### 18.2 – Look in a browser

| Row | What | Status |
|---|---|---|
| 18.2.1 | Pages that were built and not looked at: objecting to a photograph, the years above photographs, a photograph with two sizes, an image of a collection with sizes in the gallery, the site that takes accounts by invitation only, agreeing to a public family tree and its public page, the sizes of an image in the tool | **Done for what a browser could reach** 2026-11-18, with made-up accounts on this machine. How it was looked at: in the browser of the development tool, whose window did not draw during this work – so what the page holds, where each part stands and how large it is were read from the page itself, and **no picture of a page was seen**. Seen to work: an objection made on the page of the owner and taken back on one's own page, the photograph held back from a stranger meanwhile; the years above the photographs, in Persian digits on the Persian page, and one year chosen; a picture of 1600 by 1000 brought in and given as 800 by 500 and whole, with `srcset` and `sizes`; an album whose photographs change places, the first without "earlier" and the last without "later"; a tree whose people stand in the way, then made public, its public page with `noindex` and without who it is shared with; the legal pages and their links; the site by invitation only, without and with an invitation. **Two defects found and mended**: who arrived through a provider on a site by invitation only was offered a form that would be refused – the page now says why instead; the heading that opens "Change the album" was 26 pixels high – every such heading is now as high as a button. **Not looked at**: an image of a collection with sizes in a gallery – the made-up content of the preview has none, `ImageSizesPagesTests` cover it; the form of sizes in the tool – an editor signs in with a password typed at a prompt, which is not typed for a look (the rule for passwords), `StudioMediaIntakeTests` cover it |
| 18.2.2 | The form that chooses a name after a sign-in through a provider, with a made-up provider that exists in development only | **Done** 2026-11-18 – the setting `Dastan:SignIn:MadeUp` names who the made-up provider says the visitor is; in the environment `Development` the site offers it beside the others, anywhere else the setting does nothing and the form that names it is refused ([ADR 0014](adr/0014-visitor-accounts.md), addendum of 2026-11-18). Seen in a browser in both languages. **A defect found and mended**: the form said what the site keeps of a password, of an account that has none – it now says what is kept of an account that opens through a provider. Checked: `MadeUpProviderTests` (4 cases) |
| 18.2.3 | The pages of accounts, people, family trees and photographs in the light appearance and at 360 pixels | **Done by measuring** 2026-11-18 – the page of the account, of people, of the trees and of one tree, of the photographs, the public page of a person and of a tree, in English and Persian, light, 360 pixels wide: nothing wider than the window, no button, field or folding heading lower than 44 pixels after the one mended in 18.2.1. How it was looked at: in the browser of the development tool, whose window did not draw during this work – so what the page holds, where each part stands and how large it is were read from the page itself, and **no picture of a page was seen**: colours against each other and how a page looks were not judged |
| 18.2.4 | The browser tests that are skipped: why, and run where that is possible | **Done** 2026-11-18 – they are skipped wherever no browser is named: they connect to the browser of the container of `compose.yaml` (the Compose project `test`) through `DASTAN_BROWSER`, and neither a plain `dotnet test` nor the pipeline starts that container. Run on this machine with the container, after everything of phase 18: Chromium 328 passed; Firefox 325 passed, 3 skipped; WebKit 327 passed, 1 skipped – the skipped ones are those the tests themselves leave out for an engine ([acceptance](acceptance.md)). None failed. The container was removed afterwards. In the pipeline they stay skipped: a step that starts the browser there is a change to the shared workflow of the repository, which this plan does not touch |

### 18.3 – What was built in part

| Row | What | Status |
|---|---|---|
| 18.3.1 | A command that makes smaller sizes for the photographs and images that are there already | **Done** 2026-11-18 – for the photographs of the visitors the command `photos sizes <folder>` ([operations](operations.md)); for an image of a collection a form on its page in the tool, "Make the smaller sizes" – an act of an editor with a name and a reason, like every change of a record ([ADR 0006](adr/0006-media-delivery.md)). Checked: three more cases of `ImageSizesTests`, one of `StudioMediaIntakeTests`. Not looked at in a browser |
| 18.3.2 | Without a connection: a mark on a page that is a kept copy, and a control that empties what the device keeps | **Done** 2026-11-18 – a page that the device shows from what it kept says so at its top, in the language of its address: there is no connection, this is the page as it was read before, the site may have changed. The page of the visitor's list says how many pages the device keeps and has a button that forgets them; the section is hidden until the script of the site finds something kept ([ADR 0007](adr/0007-installable-app.md), addendum of 2026-11-18). Checked: `InstallableAppTests`, three more cases of `KeptOfflineTests`; in a browser with the site stopped the Persian page of a story showed the note above its text, and the list said five pages were kept. **Not tried in a browser**: pressing the button that forgets, and the English note |
| 18.3.3 | An album whose order its owner changes | **Done** 2026-11-18 – a photograph changes places with its neighbour, one place at a time ("Earlier in the album", "Later in the album"); only the owner of the album can. Checked: one more case of `AlbumsTests`, more of `PhotoPagesTests`. Not looked at in a browser |
| 18.3.4 | Places for the legal pages – terms, privacy, imprint – with links at the foot of every page; the words are the owner's | **Done** 2026-11-18 – the setting `Dastan:Legal` names a folder with a plain file for each page and language (`terms.en.txt`, `privacy.fa.txt`, `imprint.en.txt`); the pages `/terms`, `/privacy`, `/imprint` show them, and every page names them at its foot. A text that is not there is said to be missing on its page; without the setting the site has no such pages. **The words are not written**: they are the owner's, with the legal review (D-068). Checked: `LegalPagesTests` (6 cases). Not looked at in a browser |
| 18.3.5 | The closed trial on the owner's machine behind a tunnel: a Compose file and the way, written down (D-067) | **Done as far as it goes without the owner's account** 2026-11-18 – `compose.trial.yaml` has a tunnel of Cloudflare as a service that starts only when asked for (`--profile tunnel`) and takes its token from the environment; the way is in [operations](operations.md). **Not tried**: the tunnel needs the owner's Cloudflare account and its token, which the owner enters |
| 18.3.6 | From earlier phases, each looked at again for what the model has now: a gallery of a period (step 14.5), an earlier revision shown (step 14.7) | **Not built, with the reasons** 2026-11-18. *A gallery of a period*: a media item still says when it was made and for which entity it stands, not which time it shows; the periods the model has now are the stretches of a comparison (step 12.6), which are not entities and carry no images. A gallery "of a period" would need a new field of an item – the time it depicts – that the editors fill for every image, and the rule for what counts (a painting of 1850 of an event of 1020). That is a decision about the editorial model for the owner, not something to guess; what there is: a gallery in the order of the making and the search by a year. *An earlier revision shown*: the site is given one published revision and the list of what changed between revisions; showing the records of an earlier one means keeping every published revision whole where the site runs and reading two at once. Nothing in the trial needs it – a citation names its revision and the page says when the shown one is another – and it changes what a deployment stores, so it stays out until the public deployment is planned (D-067) |

### 18.4 – Decided by the owner's silence or word

Proposed to the owner on 2026-11-18, each with what is recommended; without another word they stand as
recommended and nothing is built.

| Question | Recommended |
|---|---|
| Several ladders of reading stages inside one collection (step 17.7) | Not now: a collection per story |
| The words of a page on the picture of a link (step 17.4) – two fonts in the program and a library that joins Persian letters | Not now: the mark alone |
| Somebody who looks at objections to photographs that are made to do harm (step 17.6) | Not now: the limit of twenty; before registration is open |
| Rivers and lakes on the drawing of the places – added beside the coasts that were decided (step 17.2) | They stay |

### 18.5 – The owner's own

The registration with Google and Microsoft (D-069), a host for a public site (D-067), the legal review (D-068),
the review of the Persian text of the sample, the rights of the images, devices and people (section 11).

### 18.6 – A review of phases 17 and 18 (2026-11-18)

The code of both phases was read once more after everything was merged. Nothing was found that works wrongly
for a visitor; four things were made firmer.

| # | What | State |
| --- | --- | --- |
| 18.6.1 | `compose.trial.yaml` asked for the token of the tunnel even when no tunnel was started, so the trial alone did not start without it | **Mended** – the token is asked for by the tunnel only. Tried: the trial built and started without a token |
| 18.6.2 | A picture is read whole into memory; a file of a few hundred kilobytes can say that it is tens of thousands of pixels wide | **Mended** – no picture of more than fifty million pixels is read ([ADR 0006](adr/0006-media-delivery.md), addendum); such a photograph of a visitor is refused with a message. Tried in the container with 1 GB: a PNG of 640 KB and 8000 by 6000 was taken and got its smaller size, one of 8000 by 6300 was refused; the container was not stopped for memory |
| 18.6.3 | A smaller size was written under its own name, so that a request in that moment could get half a file | **Mended** – written beside and renamed when whole |
| 18.6.4 | The trial in the container had no folder for reports of visitors and no way to name the legal texts | **Mended** – `Dastan__Reports` on the volume, `DASTAN_LEGAL` for the folder of the texts. Tried: the legal pages answer in the container |

Not changed, and said here: the image of the tunnel is named without a version (`cloudflare/cloudflared:latest`)
– which version to hold to is chosen when the tunnel is set up with the owner's account.

### 18.7 – A review of everything built so far (2026-11-19)

The owner asked for all the work on Dastan to be looked at once more, not only the last steps. Every source
file of the seven projects was read by a reviewer that only reads – one each for the core, for the readers and
the store of the editors with the command, for the store of the visitors, for the endpoints of the site, for
its pages and texts, and for the tool of the editors – and each finding was looked up in the code before
anything was changed. One finding was wrong and is not changed: a period that begins and ends in the same year
has no length to draw on purpose (`ComparisonTests`). The two files of texts hold the same 878 keys, with the
same places for values, and no page writes markup from what somebody typed.

| # | What was found | State |
| --- | --- | --- |
| 18.7.1 | **A photograph kept more than its picture.** Of a JPEG only EXIF, XMP and the notes of an agency were left out; a comment, the list of further pictures, the credentials of a maker stayed, and so did everything after the end of the picture – a second picture with its own place, the short film a telephone hangs on. Of a PNG only five kinds of chunk were left out | **Mended** – of both only what the picture is drawn from is kept, up to its end ([ADR 0006](adr/0006-media-delivery.md), addendum). A JPEG or PNG that is cut off is refused. `ImageSizesTests`, three more cases |
| 18.7.2 | **One invitation made any number of accounts** on a site by invitation only: the question "is it usable" and the account were two steps | **Mended** – one step of the store; `FirmerAccountsTests` |
| 18.7.3 | **A password could be guessed without being counted** on the forms of somebody who is signed in (another password, new codes, a copy of the data, closing) and for a closed account; tries at the same moment were counted as one | **Mended** – one proof of a password for every act, counted by the store in one sentence ([ADR 0014](adr/0014-visitor-accounts.md), addendum). After five wrong ones every such form answers "wrong" for a quarter of an hour, the right password too |
| 18.7.4 | The name of a deleted account stayed in the histories of trees it had been shown; a record ticked "has died" was named in a public tree although it is an open account that took its agreement back; an objection kept its place when its photograph was shown to fewer people; who had answered "that is not me" could not object later; an account could get two sign-ins of one provider | **Mended** – the same addendum; a unique index (migration `OneLoginOfAProvider`) |
| 18.7.5 | **A writer could record a review.** The form "change as text" of the tool takes any record, with the status of a review and who reviewed (GOV-002) | **Mended** in the store, so that no form goes around it ([ADR 0011](adr/0011-editors-store.md), addendum); `FirmerStoreTests` |
| 18.7.6 | **Two acts made close together put each other's records back** – an act keeps the whole collection as it read it – and two who publish could meet on one number | **Mended** – an act and a publication are one step of the store, one at a time |
| 18.7.7 | In the tool: a page whose address ends like a file (`….png`) was given without a sign-in; anybody could send 250 MB to the form that signs in; "take out" took the first record of an identifier in any group; an identifier could hold `/`, `?`, `#`; Persian digits were refused in numbers and years; a new row named the first record of a list without the editor choosing; a file stayed when the store failed | **Mended** |
| 18.7.8 | On the site: the search of a collection and addresses with a question were marked "may be kept by the device", and so were pages of people when the address was written with a capital; several large photographs at once could take all the memory; an address without its language and with a Persian letter failed (500); a kept page lost the headers of the site; the address a form names carried more than an invitation | **Mended** – `FirmerSiteTests` (23 cases) |
| 18.7.9 | On the pages: a sentence about two people of a tree turned around when their names were of the other script (the child became the parent); the degrees of a parallel ran into the map on a Persian page; the font was fetched twice; days in Latin digits on Persian pages; marks of charts too small to press; buttons that stand once per photograph all called the same; the rows of the reading control drawn as chips; albums without their margin | **Mended** |
| 18.7.10 | In the core: a series whose largest value is 3 (or 0.3, 30) failed its page; a view of a thread carried the stops that are not reached; a story named by the collection and not there was no problem; a tree with a link to nobody failed to be drawn; a draft of a relationship was visible to the other side | **Mended** – `QuantitySeriesTests`, `TreeShapeTests` |
| 18.7.11 | In the readers and the command: a `null` in a comparison failed the program instead of being refused; a HEIC picture or a QuickTime film was taken as an MP4 video; a path with a control character failed the check; the command wrote days in the calendar of the machine; names of kinds accepted lists; a revision made from a file refused translations of withdrawn statements | **Mended** |

**Found and not changed**, each with its reason:

* *Limits can be passed by forms sent at the same moment* (photographs, tags, trees, circles, albums, open
  invitations and requests per account; two claims of one account in a tree; two requests between the same two
  accounts). Each is "count, then add" without a lock. They bound misuse, not money or privacy, and the trial
  is among invited people; closing them means unique indexes and one more migration per rule.
* *The limits per hour count by the exact address* – a visitor with many addresses (IPv6) has many budgets. To
  count by network needs a decision about how wide a network is.
* *The version of a record begins at 1 again* when it is taken out and put back.
* *Two files brought in for the same item and part at the same moment*: the second request fails (500), nothing
  is left behind.
* *An event with a calendar date or a place "in the world" passes the check and is shown nowhere* – only
  events with a place in the order of the telling become steps. Whether that is meant is a question of the
  model.
* *Videos keep what they say about their making* (the docs promise the removal for images).
* *In the core, not reachable today*: a tag of shared media is not tied to the account that may answer it; an
  affiliation does not check whose permissions it carries; a comment about callouts that leave a timeline; the
  label of a meridian west of −180.
* `compose.trial.yaml` believes the networks Docker gives its containers about how a visitor came – with a
  tunnel in the same Compose file there is no narrower name for it; the port is open to this machine only.

Checked: 2,653 tests of Dastan, 328 of them the browser tests that are skipped without a browser; none failed.
Not looked at in a browser: the changes to the pages (the browser of the development tool did not draw).

**Decided by the owner on 2026-11-19 and built** – two things the review had left as questions:

| # | What | State |
| --- | --- | --- |
| 18.7.12 | The history of a tree told everybody who may see the tree with whom it is shared and who claimed a record, while the list of members is the owner's and a claim is shown to two people | **Done** (D-070) – those lines are the owner's; somebody else reads the ones they wrote. `FirmerAccountsTests` |
| 18.7.13 | A copy of one's data held the codes of invitations that can still be used – a key in a file that gets passed on | **Done** (D-071) – such an invitation is named without its code; the text of the page says so. `FirmerAccountsTests`, `OwnDataTests` |

### 18.8 – The legal pages and what the law asks of the site (2026-11-19)

The owner asked for the legal pages the site needs, complete, after research into the present law of the
European Union and of Germany, with a notice about cookies; a lawyer reads them before the site is public.
The site is personal: one private person runs it, no company. What was read is in
[legal/research-eu-2026-11.md](legal/research-eu-2026-11.md) and
[legal/research-de-2026-11.md](legal/research-de-2026-11.md); what follows from it, the records of processing,
the measures, the procedures of the operator and the assessment of risk in [legal/README.md](legal/README.md);
what the owner still has to say and sign in [legal/before-publication.md](legal/before-publication.md).

| # | What | State |
| --- | --- | --- |
| 18.8.1 | **The texts**: terms of use, privacy notice (Articles 13 and 14 GDPR, with a section for people others name or show), a page of everything the site stores on a device, and the legal notice (§ 5 DDG, § 18 MStV, the contact points of the Digital Services Act) – in English, Persian and German, the German one authoritative. They are part of the program; what only the operator knows comes from `Dastan:Operator`, and a text with a gap says that it is not complete. A folder (`Dastan:Legal`) can put a text of the owner's own in the place of one | **Done** (D-072). `LegalPagesTests` – among them that the three languages of a text have the same structure |
| 18.8.2 | **What is stored on a device** (§ 25 TDDDG): only what a function a visitor used needs. The language is remembered when the visitor changes it, not when a page is opened; a form answers in the language of the page it stood on without a cookie. A note said once leads to the page of cookies; there is no banner, since nothing needs consent – except keeping pages for reading without a connection, which is now done only after a yes and is turned off as easily (this changes D-064) | **Done** (D-073). `LawfulSiteTests`, `KeptOfflineTests` |
| 18.8.3 | **Registration** asks for the words "I have read the terms and the privacy notice"; the account notes the version and the moment. An account that never said so, or said so for older terms, is asked on its page – nothing is agreed to by silence | **Done**. Migration `OperatorRestrictions` |
| 18.8.4 | **Notice and action** (Article 16 DSA): the page `notice`, open to everybody, with a form that tells a reference at once; a link "Report this page" on the pages with content of visitors and "Report content" in the foot of every page. The operator reads, settles and forgets notices with the command | **Done** (D-074). `LawfulSiteTests`, `OperatorCommandsTests` |
| 18.8.5 | **Restrictions with a statement of reasons** (Article 17 DSA): the operator hides a photograph, takes a tree back from the public or closes an account, by a command, with the facts and the ground; the person concerned reads them on the account page, the owner of a closed account after name and password; a restriction is lifted the same way. The record of a person is taken out of a tree for a person who objects (Articles 17, 21 GDPR); a closed account is deleted at once on request | **Done** (D-074). `OperatorRestrictionsTests` |
| 18.8.6 | **A word to everybody**: `Dastan:Announcement` – how the operator of a site without mail addresses announces changed terms, the end of a function or a breach | **Done** |
| 18.8.7 | **The record**: research with sources and what could not be verified, records of processing (Article 30), technical and organisational measures, procedures, a short assessment of risk, what the owner and the lawyer still have to do | **Done** |

Checked: 2,689 tests of Dastan, none failed – 329 of them the browser tests, run in Chromium in the container
of `compose.yaml` (skipped without a browser). The new pages were read in the browser of the development tool by
their structure only; it did not draw, so nobody has looked at them. **Not done by the program and open**: the postal address and the other values
of `Dastan:Operator`, the contracts with the host and the network provider, the mailbox, and the reading by a
lawyer ([legal/before-publication.md](legal/before-publication.md)). No text makes a site free of every legal
risk; the files say for each point what was verified and what was not.

## 20. Correcting the foundations (phase 19)

On 2026-11-19 the owner read a review of the architecture, made at revision 1cd363ab and kept by the owner
outside the repository, and decided that
several choices made for a small demonstration must not carry the product: accounts without a mail address,
SQLite, pages without a script, nodes without a profile, one relationship between two people. No feature is
added before this phase is done. Nothing is left for later: each step ends when its exit holds, and a step
that cannot meet its exit is said so, not passed.

**Decided** (D-076 to D-080): PostgreSQL; a mail address that is confirmed for every account, on ASP.NET
Core Identity; every node has a profile with a picture and the facts its kind needs; several relationships
between the same two people; pages still come from the server, and a layer of TypeScript in the browser moves,
zooms, selects and opens; the graph is built on **Syncfusion EJ2 Diagram** behind a contract of Dastan's own;
the timeline is a control of Dastan's own; SkiaSharp stays for pictures on the server and moves to a
supported line. There are no real accounts yet: the stores of the trial are not carried over, the new schema
begins clean.

**What stays**: `Vafadar.Temporal`, sources and reviews, immutable revisions, stable identifiers, the
projection that gives a visitor only what they may see and have read to, consent of both sides, audiences of
media, the account apart from the person, both languages, the pages a reader without a script can read, and
the tests that hold these.

| # | Step | What is done | Exit |
| --- | --- | --- | --- |
| 19.1 | **The contract** | ADRs that take the place of the decisions that fall (D-035, D-045, D-050, D-064 as far as it speaks of scripts, D-065): persistence, identity and mail, the profile of a node, relationships, the interactive layer, the graph, the timeline. `AGENTS.md` of Dastan follows. A table from each requirement of the product documents to the journey that shows it | Every later step names the ADR it builds; no rule of `AGENTS.md` contradicts the plan |
| 19.2 | **Persian in Syncfusion Diagram** – first, because the choice rests on it | A page outside the product with EJ2 Diagram: cards with a picture and mixed Persian and Latin text, long names, punctuation, digits, connectors with labels, the layouts for a hierarchy with several parents, zoom and pan, keyboard, export to SVG and PNG, at 360 px and on a wide window, in Chromium, Firefox and WebKit. Looked at in a real browser by somebody, not read from the markup | Persian text is shown, wrapped and exported right in every case of the list, or the defect is written down with what it costs to go around it, and the owner decides. The licence key is entered by the owner |
| 19.3 | **PostgreSQL** | One database, a schema for each part: identity, editorial content, published read models, personal content, delivery. Npgsql and EF Core; migrations of their own; `compose.yaml` starts a PostgreSQL for development and tests. Editorial records become relations (nodes, relation ends and kinds, memberships, media, sources) with typed columns for what is asked for often and checked JSON only for what is rare; a published revision stays the exact bytes it was. Every rule that was "look, then write" becomes a constraint or a lock of the database: one account per name and per mail address, one claim per person and account, the use of an invitation, the number of a revision, the limits per account. An act on a collection carries the version it read and is refused when another came first | No SQLite in Dastan. Tests run against a real PostgreSQL in CI; for each rule a test sends requests at the same moment and the rule holds. Backup and restore are tried and written into the operations guide |
| 19.4 | **Accounts with a mail address** | ASP.NET Core Identity for visitors and for editors – one way of keeping passwords, tokens and lock-outs, two kinds of account that are never merged by a name or an address. A mail address is required, unique and confirmed before an account is fully open; confirm, send again, forgotten password, another address, sign out everywhere. Google and Microsoft give the address they confirmed; linking is an act of the signed-in visitor. Mail leaves through an outbox in the database with retries and without sending twice, by SMTP – the provider is a setting; in development a mail catcher in Compose. Invitations go by mail and still as a link. Names in any script stay possible. Recovery codes stay as a second way | Every journey – register, confirm, sign in, forget, change, invite, close, be closed by the operator and read why, also for an account with a provider only – passes in a browser test with the mail read from the catcher. A statement of reasons is also sent by mail |
| 19.5 | **The profile of a node** | One contract for every node, public or private: stable identity, kind, names, summary, a profile picture that the audience may see, and the facts its kind needs – a person: born, died, places; an organisation: founded, closed, field, seat; a team: founded, home; and so on from the ontology of the product documents. Dates of private people use `Vafadar.Temporal`, not a year from 1 to 9999. A picture chosen as profile picture is shown only to who may see it; without one a drawing of the kind. A page of the profile for every node; a card of it for graphs. An account stays apart from the person it says it is | Every kind of the ontology has its facts written down and shown; a node without a picture or a fact says so; tests for the audience of the picture |
| 19.6 | **Relationships** | Each relationship is its own record: kind, direction, context (an employer, a school), time, state of consent, audience. Colleague and close friend between the same two exist side by side; which kinds exclude each other is written down. Consent of both and the narrower audience stay. Affiliations and the choice to be found by them (school and year) are kept and searched | The tests that held "one relationship per pair" are replaced; a browser test asks, accepts and shows two kinds between the same two |
| 19.7 | **The interactive layer** | TypeScript, built with the site (one bundler, pinned, in the image), served by the site itself. The first answer of a page is still complete HTML: a reader without a script reads everything and follows links. Data for the browser comes from addresses that give exactly the projection the page would – stage read to, audience, consent – and nothing is hidden only in the browser. The content security policy stays strict | A test per address: what it gives is what the page of the same visitor shows; nothing of the layer is kept by the service worker for an account |
| 19.8 | **DastanGraph** | A contract of Dastan's own (nodes with profile cards, edges with identity, label, direction and source, focus, expand, collapse, select, open the profile, export) with EJ2 Diagram behind it; no type of the library in the domain or in what is stored. The rules of a family tree are Dastan's: several partners, birth and adoptive parents, an ancestor who appears twice, branches that do not meet – one person stays one person however often drawn. Connections between any nodes keep every edge apart. Keyboard, touch, a narrow screen with the profile as a panel, and the list beside the drawing as the way for a screen reader | Profile, genealogy, relationships, Persian and accessibility, export and privacy each have a browser test; made-up sets of 100, 1,000 and 10,000 nodes with what is loaded and what is drawn said, measured on named devices against budgets set before measuring |
| 19.9 | **DastanTimeline** | A control of Dastan's own with three axes: a calendar, a number line for deep time (thousands to billions of years) and the order of a telling. A window in which something happened is drawn as a window, a duration as a duration, an open beginning or end as open. Zoom and pan in the browser. Pictures get the time they show apart from the time they were made, so that a gallery of a period exists. Events with a date and a place and no place in a telling are shown | 500 BCE to today, 10–20 thousand years, 50–60 million years and a telling on one page each, in both languages; the fault of `ChartState.Year` at the lowest number has a test |
| 19.10 | **What follows from the new foundations** | The legal texts and the records of processing are written again for accounts with a mail address and the mail provider (row 18.8); the statement of reasons goes by mail too; SkiaSharp on a supported line with the image of the container tried; the README says where the product stands | The three languages of every legal text agree again; the documents name no rule that fell |
| 19.11 | **Checked as the product is used** | Browser tests of Dastan run in CI; every journey of 19.1's table is walked once more on the final revision in Chromium, Firefox and WebKit; the app is installed on a real phone | The table of 19.1 has evidence in every row, and "built", "looked at" and "ready" are said apart |

**Done on 2026-11-19: 19.1 and 19.2.** The contracts are ADRs 0015 to 0021 and the rules at the head of section 3 of `AGENTS.md`; the table is [foundations-trace.md](foundations-trace.md) – 101 requirements, rules and scenarios of documents 02, 06 and 14, of which 10 are contradicted by the code today, each with the step that ends it (the finer rules of the other product documents refine these and have no rows of their own). The trial of the control is `eng/spikes/diagram-persian`: Persian is shown, wrapped, laid out and exported right in Chromium, Firefox and WebKit at 360 and 1,280 pixels **with five things the product does itself**, written down in [ADR 0020](adr/0020-graph.md); the keyboard of the control is not usable as it comes and is ours to build; touch, a screen reader and thousands of nodes were not looked at and belong to 19.8. Seen in pictures of real browsers, of which the main ones were looked at by eye; the owner has not looked yet.

**Step 19.3, first part, done on 2026-11-19: the store of the visitors is on PostgreSQL.** `Vafadar.Dastan.People` uses Npgsql in the schema `personal` with a first migration of its own; the SQLite migrations are gone. The site is told `Dastan:Database`, does not change tables and does not start on a database that is behind; `database migrate` of the command does, and the command comes with the image of the site. `compose.yaml` starts a PostgreSQL for development and tests that keeps nothing; a test gets a database of its own, copied from a template, and it is dropped when the test is over; the job of CI starts the same image and fails without it. Checked: 2,689 tests of Dastan, none failed, the 195 of the store against a real PostgreSQL. **Second part, the same day: the rules that count before they add are kept by the database.** Each such act of the store of the visitors – a tree, a record of a tree, a circle, an album, a photograph, a tag, an objection, an invitation, a page of the list, a request between two accounts, a claim – runs in a transaction that first takes a lock of PostgreSQL for what it counts (`PeopleStore.Guarded.cs`). `AtTheSameMomentTests` sends twelve requests at the same moment for the last place of each rule and finds one that got it; with the lock taken out, four of its six tests fail, so they do test it. 2,696 tests, none failed. **Still open in 19.3**: the store of the editors as relations and the published revisions in the database; reports and notices as tables; limits per hour by network; backup and bringing back, tried. The accounts themselves move in 19.4.

**Step 19.3, third part, done on 2026-11-19: the store of the editors is on PostgreSQL.** `Vafadar.Dastan.Store` uses Npgsql in the schema `editorial` with one first migration; the SQLite package and `store copy` are gone (`pg_dump` takes its place, operations-studio.md). The tool of the editors is told the connection in `Studio:Store`, does not change tables and does not start on a database that is behind; `database migrate` makes the tables of both stores. Wherever a command asks for a store, the word `env` stands for the connection in `DASTAN_DATABASE`. Acts of several editors at the same moment are done one after the other by a lock of the database per collection (a test with eight acts at once; without the lock it fails). `compose.studio.yaml` has a database of its own. **Not done in this part and still owed by step 19.3:** the records as relations with typed columns (the tables are as they were – a record with its body as JSON), published revisions in the schema `published`, reports and notices as tables, limits per hour by network, an earlier revision shown, and backup and restore tried. **Not tried:** the image of the tool was not built again and `Check-Studio.ps1` was not run since the move; nothing was looked at in a browser.

**Step 19.3, fourth part, done on 2026-11-19: what the database knows about a record (D-081).** The owner chose between full normalisation and the written form with derived relations; the second was taken. Bodies are `json` (checked, and kept as written); `Records` has columns the database computes from the body (kind, name, reading level, review); `RecordLinks` says which record names which, with foreign keys, and follows every act in its step. Publishing takes the lock of its collection (two who publish at once get two numbers; found after the third part, fixed with a test). The test projects start with a wide thread pool – without it, tests that wait on purpose starved the driver and failed for no reason. **Still owed by step 19.3:** published revisions in the schema `published`, reports and notices as tables, the version an act read, limits per hour by network, an earlier revision shown, and the image of the tool built and checked again.

**Step 19.3, fifth part, done on 2026-11-19: the published revisions in a schema of their own, read by the site.** `published.Revisions` keeps a revision with the hash of its text; `published.Releases` says which revision the site shows – a release or a withdrawal, each kept with who and when. Publishing releases nothing; `store release` and `store withdraw` do, and `store revisions` says which is shown. A site told `Dastan:Published` reads the released revisions from the database when it starts, checks each hash, and does not start without tables, without a release or on a changed revision; without the setting it reads files as before. An earlier revision is shown again by releasing it (R-PUB-07). **Still owed by step 19.3:** a page of the tool for releasing, the comparisons from the database, a database role for the site that reads `published` only, reports and notices as tables, the version an act read, limits per hour by network, the images built and checked again. **Not tried:** a site in a container reading from the database; nothing was looked at in a browser – no page changed.

**Step 19.3, sixth part, done on 2026-11-19: what the parts before left owed.** An act says which version of a record it was decided on and is refused under the lock when the record moved on (the pages of the tool hand it over; a new record is not put over one made in between); the versions of a record are counted through a time it was not there. The page of the revisions says which revision the site shows and a publisher releases or withdraws there; a comparison is released the same way and a site that reads from the database reads its comparisons there. Reports and notices are tables of the schema `delivery` (`Dastan:Reports`, `Dastan:Notices`, `Studio:Reports` now say whether, not where). Limits per hour count by network; the trial believes its tunnel only. `database grant site|tool` gives the site and the tool a role each. **Tried:** both images built; the tool with its database and the trial with its database were started in containers, their tables made by the command there, and `Check-Studio.ps1` and `Check-Site.ps1` passed; the new page of the tool was looked at in a browser (en at 800 px dark, fa at 375 px light: released a revision there, and the page of a comparison) – a date inside a Persian option stood turned around and is mended. **Not tried:** a site in a container reading its collections from the database; the roles on a server; a proxy with a certificate. **Step 19.3 is done** but for what its ADR hands to later steps and names there: the schema `identity` and mail in `delivery` (19.4), the indexed years of a time in the telling (19.9), and synchronous stores under load (19.11).

**Step 19.4 is built in these parts, in this order** – each ends with its tests, and the step is done when the last is: (1) *the mail*: outbox, sender, catcher – **done on 2026-11-19**; (2) *the accounts of visitors on ASP.NET Core Identity* in the schema `identity`: register with a name, a mail address and a password, confirm, send again, sign in, sign out, the cookie and its stamp, lock-out – the pages and endpoints of today moved onto it, the store of the visitors keeping what is Dastan's (trees, photographs, circles, consent) under the identifier of the account; (3) *the other journeys*: forgotten password, another address (confirmed at the new one, told to the old one), sign out everywhere, recovery codes as a second way, close and think it over, closed by the operator with the reasons on the page and by mail; (4) *Google and Microsoft*: the confirmed address they give, linking as an act of the signed-in visitor, an account with a provider only; (5) *the texts of the mail* in both languages as plain text and HTML, and invitations by mail and as a link; (6) *the editors on Identity*: a second kind of user with its own tables and cookie, made by the operator; (7) *the journeys in a browser* with the mail read from the catcher. The first part: `delivery."Mail"` with a key per message, written in the transaction of what it tells of, sent by the site every five seconds with retries that wait longer and a stop at the eighth, by SMTP (MailKit 4.18.2) to the server the settings name; Mailpit 1.33.0 catches the mail of a development machine, of the tests and of CI; the command `mail` lists, retries and forgets. Tried against the catcher with a message in Persian. Nothing of the site writes mail yet.

**Step 19.4, second part, the store – done on 2026-11-19; the site not yet.** The schema `identity` with Identity's tables for visitors, in the transaction of the visitors' store; `store.Accounts`: register with name, address and password, confirm by the mailed link (once, 24 hours), send again (not within two minutes), sign in by name or address, five wrong passwords pause for 15 minutes; a taken address is told only to who has it; the two messages in English and Persian. Tried with twelve who ask for the same name, the same address and the same invitation at the same moment. The schema `delivery` got its own owner and migration. **The pages of the site still use the accounts without an address**: they move in one piece when parts 3 and 4 exist in the store, so that the site never keeps two kinds of account. Until then nothing a visitor sees has changed.

**Step 19.4, parts two to four – done on 2026-11-19, in the store and on the site.** The store has every journey of an account (another password, a forgotten one by a mailed link, another address confirmed at the new address, signing out everywhere, recovery codes, closing and opening again, Google and Microsoft as Identity's logins with the address the provider confirmed). The site moved onto these accounts in one piece and the accounts with a name and a password alone are gone, with their code and their tables. A form that makes an account answers "look at your mail"; a mailed link opens a page that changes nothing until its button is pressed, and that page hands its address to no other site; asking for a link answers the same whether there is an account or not. Tried: 2405 tests, among them the journeys through the pages of the site with the mail read from the outbox (`AccountMailPagesTests`), and once by hand in a browser at 375 px, Persian and English, with the real sender and the catcher – making an account, the mail, the link, the recovery codes, a forgotten password. **Not tried:** a real client of Google or Microsoft; the page of the new address by hand; a mail in a real mailbox (the look of the HTML part in a mail program). **Owed to step 19.10 and a condition of going public:** the legal texts of the program still say that an account needs no mail address. Parts five to seven of the step are open.

**Step 19.4, part five – done on 2026-11-19.** The site mails an account what somebody else asks of it – a relationship, a tag, an agreement to a photograph or a family tree being shown to everybody (R-PER-03) – in the language of the account, saying who asked and where to answer and nothing of what it is about; an account turns these messages off (PRIV-007). A decision of who runs the site is mailed with its statement of reasons, and its end too; the commands `people restrict` and `people lift` take the address of the site. An invitation can be mailed to an address that is kept for nothing else. Tried: the store (`NoticeMailTests`), the pages (`AccountMailPagesTests`), 2415 tests in all; the two new parts of the page of the account seen in a browser at 375 px in Persian. **Not tried:** the look of these messages in a mail program; the page in English and in the light theme. Parts six and seven are open.

**Step 19.4, part six – done on 2026-11-19.** The accounts of the editors are kept by ASP.NET Core Identity in a table of their own (`identity."EditorAccounts"`); the store's own code for passwords is removed. Nothing an editor sees has changed: the same command makes an account, the same page signs in, roles and closing are as before. The role of the database the site runs as is given the tables of the visitors and the role of the tool the table of the editors, table by table; a test asks each for the other's and is refused. What is kept of a password is derived with 210,000 rounds for both kinds of account. Tried: 2415 tests, among them the accounts and roles of the editors, the pages of the tool, and the two roles of the database. **Not tried:** the tool by hand in a browser after this change (its pages are tried by the tests of the tool, which sign in through them). Part seven is open.

**Step 19.4, part seven – done on 2026-11-19; the step is built.** The journeys of an account run in a real browser with the mail read from the catcher (`AccountJourneyBrowserTests`, `AccountAccessibilityBrowserTests`): the site sends by SMTP, the test opens the link as it was written. Making an account in English, in Persian and without scripts; a link that is used up; a forgotten password; another address; the page of a link telling no other page where its visitor came from; the pages of an account against the rules of accessibility a tool can decide, in both languages, light and dark. Nine tests, in Chromium, Firefox and WebKit; all 338 browser tests in Chromium; 2415 tests without a browser. The check of accessibility found the list of what was wrong with a form announced as an alert itself, on every page with a form (accounts, people, photographs, trees, notices): corrected, and seen in a browser. **Not tried in step 19.4 as a whole:** a real client of Google or Microsoft; a message in a mail program and in a real mailbox; the tool of the editors by hand after part six; the whole browser suite in Firefox and WebKit after the change of the lists (the nine tests of the account ran there). **Open and the owner's:** the mail provider and its password, and a sender with a domain that receivers trust. **A condition of going public, owed to step 19.10:** the legal texts still say that an account needs no mail address.

**Step 19.5 is built in four parts**: (1) the contract and the table of facts in the domain, read from a record and checked; (2) the page of a profile and the card on the site; (3) the tool of the editors – the facts of a kind asked for, what is missing said, notes before a revision; (4) the people of a private tree as nodes: times instead of years, places, a picture among the owner's photographs, and what may be kept about a living person who was not asked (PRIV-003). **Parts one to three – done on 2026-11-20.** Every one of the 31 kinds of the vocabulary has its facts in code and in DOM-018 of the product documents, held together by a test. A profile says every essential fact, stated or "not known", and tells nothing that lies ahead: a fact of a later stage reads like one nobody stated. The picture is shown to exactly those who may see its media item, by stage and by audience; everybody else sees a drawing of the kind and the words that there is none. Tried: 2461 tests without a browser; 343 browser tests in Chromium, the five of the profile also in Firefox and WebKit (narrow and wide, both languages, light and dark, the rules of accessibility a tool can decide); the page and the cards seen at 375 px in Persian and English. **Not tried:** the profile at desktop width by eye (measured in a browser, not seen); a picture on the real sample – it has none that may be shown. **Not done:** a full run of CI – every run on `main` since 2026-11-19 was cancelled by a newer push before the tests finished.

**Part four – done on 2026-11-20.** A person of a private tree is a node with the same profile: the times of birth and death are expressions of `Vafadar.Temporal` – a year, a month or a day of the Gregorian or the Solar Hijri calendar, about, between two years, or stated as not known – and no number from 1 to 9999 any more; where somebody was born and died, a few words and a picture among the photographs of who chooses it are kept **of somebody who died** (by who may change the tree), **of somebody whose account the record is** (by that account alone) and **of a child** (by an account that is its parent in the tree) – and of a living person nobody asked nothing but the name, the years and the family (PRIV-003); the store refuses, and the page says why. The picture is shown to exactly those who may see that photograph; a public tree shows it only when the photograph is public. Every person has a page, private and – for a public tree – public. Tried: 2483 tests without a browser; 347 browser tests in Chromium, the four of the page of a person also in Firefox and WebKit (375 px and 1280 px, both languages, light and dark, a real picture drawn, the rules of accessibility a tool can decide); the page of a person and the tree seen at 375 px (Persian dark, English light) and at 1280 px. **Not tried:** a real photograph of a camera as a picture of a person (the tests use a made-up image); the page with a screen reader. **Not done:** a full run of CI, as above.

**Step 19.6 – done on 2026-11-20**, in five parts ([ADR 0018](adr/0018-relationships.md), "What is built"): (1) each relationship a record with kind, direction, place and time, several between the same two, the groups that exclude each other, and a change that holds only when the other side agrees; (2) blocking an account (PRIV-009); (3) affiliations and being found through a place, by the rules of step 15.2 that existed only as a library (PRIV-010 to PRIV-012); (4) seeing one's own page and tree as the public, a circle or a named account (PRIV-015); (5) the links of a tree as parent by birth, adoptive parent, guardian with its time, and partner (R-PER-02). The tests that held "one relationship per pair" are replaced. Tried: 2546 tests without a browser; 349 browser tests in Chromium, of which the two of the relationships – two accounts ask, accept and show two kinds side by side, state a place, find each other through it and look at a page as everybody – also in Firefox and WebKit (375 px English light, 1280 px Persian dark, the rules of accessibility a tool can decide); the pages of the relationships, of the places and of a look seen at those two sizes. **Found by the browser and not by the other tests:** the form of the search wrote the language into its address twice; a test of the page holds it now. **Not tried:** the pages with a screen reader; blocking between two real people. **Unsteady once:** in one full run of the browser tests one test of step 19.5 ran out of time waiting for a page while everything ran at once; alone it passed twice. **Not done:** a full run of CI – every run on `main` since 2026-11-19 was cancelled by a newer push before the tests finished.

**Step 19.7 – done on 2026-11-20** ([ADR 0019](adr/0019-interactive-layer.md), "What is built"). The script of the site is TypeScript, checked and bundled by the build with pinned tools, in the image in a stage of its own; what is made lies among the files of the build and is in no repository. The profile of a node has an address for the browser – for an entity, for a person of a tree of an account and of a public tree – answered by the code of the page from the same projection; a test per address holds its answer against the page of the same visitor, at an early and at a late stage, in both languages, for the public and for an account. The first piece of the layer is the panel of a profile on the cards of a list. Tried: 2557 tests without a browser; 354 browser tests in Chromium, the five of the panel – opened, compared with the page, closed by the keyboard, without a script, with a failed answer, and nothing of the data kept on the device – also in Firefox and WebKit; the panel seen at 360 px (English, light) and 1280 px (Persian, dark); the image of the container built once, with the script made in it and no sources in it. **Not tried:** the image started as a site with this script; a device without Node at build time (the build says what is missing). **Not done:** a full run of CI, as above – so that the build with Node passes there is not seen yet.

**Step 19.8 – done on 2026-11-20** ([ADR 0020](adr/0020-graph.md), "What is built"). `DastanGraph` is a contract of the site (`GraphData`) with an address for the family and the connections of an entity and for a tree of an account and a public tree; the control (Syncfusion EJ2 Diagram) is behind it in one file of TypeScript that only a page with a graph asks for. Where a card of a family stands is decided by the layout of Dastan, on the server; the control draws, moves, zooms and saves. The keyboard is ours, the profile opens in the panel of the page, the still drawing stays as the first answer and as the choice of the reader. Found in a browser and ended: the control's own count of use, sent to a host of its maker, is switched off and refused by the rules of the site; pictures alone got one more rule of the content security policy. Tried: 2940 tests, of them 370 in Chromium; the 13 tests of the drawing – what it shows against the answer of its address, no request to another host, Persian lines, two relations between the same two, the keyboard, the tools, the saved pictures, what a reader has not read to, and the still drawing without the script – and the three of the measures also in Firefox and WebKit; the connections of a name looked at by eye in Chromium (Persian, dark, 1100 pixels), which found a line running through a card that no test had found (ended: one round in the place of two). Measured with made-up families of 100, 1,000 and 10,000 people against budgets written before measuring – all far inside (10,000 people drawn in 2.7 to 4.1 seconds, a step with the keyboard below a quarter of a second). **Not tried:** a real phone and touch with two fingers, a screen reader, a saved file opened in another program, a family and a tree of an account looked at by eye, the image of the container built with the second script, the page with a key of the licence (the owner enters it as `Dastan__Syncfusion__Key`). **Not done:** carrying what is chosen on to the timeline and the gallery (R-EXP-05) comes with step 19.9; a full run of CI, as above.

**Step 19.9, first part – done on 2026-11-20** ([ADR 0021](adr/0021-timeline.md), "What is built"). There is one reader of a time a visitor typed – a year before or of the common era, a stretch of years, a depth of time, in English and Persian – and one axis of years that the calendar and deep time share; the search of a collection and "what held at this time" ask by it, a collection may say a depth of time, and the site writes one with its number, its unit and what it is counted from. The fault of `ChartState.Year` at the lowest number is ended and has a test. Tried: 3065 tests, of them 372 in Chromium; among them the reader with 60 typed times and 22 that are none, the axis with ages counted from 1950, from 2000 and from "ago", and the pages with records of 55 million, 5.3 million and 10 to 20 thousand years. **Not tried:** in a browser by eye (the pages are forms and lists that existed; their tests are of the markup). **Not done yet:** the drawing with three axes, zoom and pan, the time a picture shows, calendars side by side, carrying what was chosen – the next parts of this step.

**Step 19.9, second part – done on 2026-11-20** ([ADR 0021](adr/0021-timeline.md), "What is built", part 2). `DastanTimeline` is drawn by our own script in SVG: one view for each axis – the calendar, a depth of time on a line of powers of ten, the order of a telling – with a window as a dashed frame, what lasted as a bar with what is known of its two ends, an open end as an arrow and an end nobody states as a fading line. It is moved and made larger and smaller in the browser, the address keeps the view, every mark leads to its row, and the still chart stays without a script. Tried: 3081 tests, of them 375 in Chromium; the five of the timeline – a view for each axis with every mark leading to its row, what is known drawn as it is known (measured against the axis), moving and the address, Persian on a phone width, and the still chart without the script – and the tests of the still chart also in Firefox and WebKit. Looked at by eye in Chromium, in English and Persian. **Not tried:** a phone, touch with two fingers, a screen reader. **Not done yet:** the page of a story on the control, the time a picture shows and the gallery of a period, calendars side by side, carrying what was chosen.

**Step 19.9, third part – done on 2026-11-20** ([ADR 0021](adr/0021-timeline.md), "What is built", part 3). A picture has the time it shows apart from the time it was made (`depictedTime` in a collection); the gallery of a collection is asked for a time and shows what shows that time whenever it was made, with what was only made then named apart; the timeline of a collection has a lane for what the pictures show; "what held at this time" and the gallery of a time lead to each other with the time. Tried: 3099 tests, of them 375 in Chromium; among them 13 of the pages – both times on the page of an item, the gallery of 1020, of 600 to 500 BCE, of a depth of time and typed in Persian, what was made in a time named apart, what is no time, and a reader who has not read to a picture – and 5 of the reader of a collection. **Not tried:** in a browser by eye. **Not built, and said so in the ADR:** themes of a gallery (the vocabulary has no kind for one), a picture in several places with its own caption (R-MED-07), calendars side by side (R-TIME-05), carrying a chosen node from the graph to the timeline, the page of a story on the control.

**Step 19.9, fourth part – done on 2026-11-20** ([ADR 0021](adr/0021-timeline.md), "What is built", part 4). A date exact to the day is shown as written and, beside it, converted into the calendar of the page (Gregorian, Solar Hijri, Lunar Hijri), said to be converted; what is not exact enough, or names no calendar, is left as written. A card chosen in a graph offers its timeline and its pictures, and a name has a timeline of its own. Tried: 3126 tests, of them 375 in Chromium and the 13 of the graph also in Firefox and WebKit; among them 13 of the conversion (known days of the three calendars, there and back, and what is not converted), 10 of the pages in both languages and 3 of the timeline of a name. **Not tried:** in a browser by eye. **Not built, and said so in the ADR:** themes of a gallery, a picture in several places with its own caption (R-MED-07), the page of a story on the control, a choice of the calendar by the reader, carrying filters from the search to the graph. With these named, step 19.9 is done as far as it is built; the rows of the table of 19.1 they belong to stay open and say so.

**Step 19.10 – done on 2026-11-20.** *The legal texts*: the privacy notice and the terms are written again in English, German and Persian for accounts with a mail address ([ADR 0016](adr/0016-identity-and-mail.md)) – what is kept of an account (the address and whether it is confirmed, the hashes of the secrets of mailed links, the language), which mail the site sends and that it sends nothing else, for how long a message and its record are kept, what Google and Microsoft are asked for now (the confirmed address too), how somebody shows that an account is theirs (through its address), and that a statement of reasons is mailed as well as shown. The three languages of each text have the same lines; no text names a rule that fell ("no e-mail address" is in none). The company that sends the mail is a setting (`Dastan__Operator__Mailer`), named as a processor where it is set; the record of processing ([docs/legal](legal/README.md)) has the mail as an activity of its own, and its risks name a mailbox that is read by somebody else. *SkiaSharp* moved to 4.155.2 and not further: 4.156.2 and 4.157.0 end the process on a file that is no image – found by the test of exactly that, on Windows – ([ADR 0006](adr/0006-media-delivery.md)). *The image of the container* was built with it and with both new scripts and started: a family page, `graph.js`, `timeline.js`, the answer of a graph, a time asked for, and the picture for the preview of a link (drawn by the library on Linux) all answer; with the audience of the public the image shows nothing of the draft collection, as it must. *The README* says where the product stands and what it is not ready for. Tried: 3126 tests, of them 375 in Chromium, among them the 12 of the legal pages and the 34 of the sizes of images; the image started twice on this machine, as the public sees it and as a demonstration. **Not done – the owner's:** the name and the address of the company that sends the mail, a contract with it, a mailbox and a sender with a domain; a lawyer's reading of the three texts (they are written with care and are no legal advice); the key of the licence of the graph.

**Step 19.11 – done on 2026-11-20, as far as it is not the owner's.** *In CI*: the workflow has a job for each of the three engines (`dastan-browser`) that starts the PostgreSQL, the mail catcher and the Playwright container of Dastan on the runner and runs the tests in a browser. **It has not been seen to run**: every run on `main` since 2026-11-19 was cancelled by a later push of the other app, because a run of a branch is cancelled when a newer one begins – on 2026-11-20 the owner decided that every push to `main` keeps a run of its own, and until a run is seen the job is written, not proven. *The last walk*: on the last revision the tests in a browser ran whole in Chromium (375), in Firefox (372, 3 not asked of it) and in WebKit (374, 1 not asked of it), none failed; with them 3126 tests in all. *The table of step 19.1* ([foundations-trace.md](foundations-trace.md), its last section) says for every row that was open where it stands, with its tests, whether it was looked at by eye, and what is still open – and that **no row is ready**: "built", "looked at" and "ready" are three words there. **The owner's, and not done:** installing the app on a real phone and touch with two fingers; a screen reader; Safari itself; everything a real mailbox, a client at Google or Microsoft and the key of the licence of the graph would show.

**Phase 19 is built.** What it leaves open is named where it belongs: in ADR 0021 (themes of a gallery, a picture in several places, the page of a story on the new control, a choice of the calendar by the reader, carrying filters of the search), in ADR 0020 (a phone, a screen reader, a saved file in another program) and in the last section of the table of step 19.1.

**Added from the table of step 19.1** ([foundations-trace.md](foundations-trace.md)), which found what the
product documents ask for and no step named. Nothing of it is left for later; each belongs to the step that
builds its foundation, and that step is not done without it:

* 19.3 also: an earlier revision can be shown (R-PUB-07) – revisions are kept, so the site can open one.
* 19.4 also: mail about a relationship request, a tag and a request to agree, which an account can turn off
  (R-PER-03, PRIV-007).
* 19.5 also: what may be kept about a living person who was not asked, and about children (PRIV-003).
* 19.6 also: guardianship and birth or adoptive parents as kinds (R-PER-02), blocking an account (PRIV-009),
  and "see as somebody else" (PRIV-015).
* 19.8 and 19.9 also: what was chosen is carried from the graph to the timeline to the gallery (R-EXP-05).
* 19.9 also: search and "what held at this time" by a year before the common era, a period and deep time
  (R-PUB-04); dates beside their conversion into the reader's calendar (R-TIME-05); a caption and a source
  per place of a picture (R-MED-07); a gallery of a theme (R-MED-02); a memory that comes up again, said to
  be one (SC-17).
* 19.11 also: the app installed on Windows and on a phone, Safari itself and not only WebKit, somebody who
  uses a screen reader, and somebody who sees the site for the first time (R-PLAT-01, R-PLAT-05).
* 19.3 also closes what review 18.7 found and left: limits per hour counted by network and not by the exact
  address, the version of a record that began at 1 again, and what the trial believes about its proxy.

**Asked for by the product documents and not a foundation** – the owner decides whether they are built in
this phase: posts (R-PER-08), and videos of visitors (R-MED-01, R-MED-03), which D-066 decided against.

**Order.** 19.1 and 19.2 first, at the same time; 19.2 may change 19.8 and nothing else. Then 19.3 and 19.4
together – accounts are written once, on the new database. Then 19.5 and 19.6, which give the graph what it
shows. 19.7 before 19.8 and 19.9. 19.10 and 19.11 close the phase.

**The owner's part**: the Syncfusion licence key for the scripts (entered by the owner; a key of scripts is
readable in every browser, as Syncfusion's model has it); the mail provider and its credentials before the
site is public; the look at 19.2.

## 21. Safe foundations and one visual experience (phase 20)

Planned on 2026-11-20, after a review of the source by the owner and three sketches of the owner's (Discover, the explorer of relationships, the profile of a node; each for a wide window and a phone). **The twelve steps were done by 2026-11-21**; each says below what it built, what it did not build and what it did not show, and [acceptance.md](acceptance.md) says how the whole is shown. What is left to the owner is named there and in step 20.12: people who see the site for the first time ([user-test.md](user-test.md)), a real phone, Safari itself and a screen reader. (As planned, this paragraph said: "Nothing of this phase is built yet.") The sketches are a direction, not a final design: a light, warm ground, readable text, one accent colour, room for pictures and drawings, details only when the reader asks for them. Their pictures are made up for the sketch and are not content of the site; the wordmark stays the plain name. (It did until 2026-11-21 – section 22.)

The review read the code and did not run it. What it found was read again in the code on 2026-11-20 and holds; none of it has been reproduced by a test yet. Each finding is reproduced first, by a test that fails, and corrected after.

**Kept as they are:** .NET 10 and the Blazor Web App, PostgreSQL, ASP.NET Core Identity, the outbox of the mail, `Vafadar.Temporal`, the TypeScript layer, `DastanGraph` behind its contract, the timeline of Dastan's own, and pages that are whole without a script. **Not in this phase:** posts of visitors, videos of visitors, a page of every earlier revision, a logo. (The logo came after the phase, on 2026-11-21, by a decision of the owner – D-082, [ADR 0026](adr/0026-logo.md), section 22.)

The order is the order of the work. Steps 20.5 to 20.11 are looked at by eye in the **published** build (the image of the `Dockerfile`), in Persian and English, at 360, 412 and 1280 pixels, light and dark – not in the server of development.

### 20.1 – Where it stands, with evidence

* Pictures of Discover, a profile, the family and the connections, a timeline, a story, the gallery, and the trees and photographs of an account, from the published build: both languages, three widths, with what the console and the network said. They are the "before" of this phase.
* A test for each finding of 20.2 to 20.4 that fails on the code as it is.

**The pictures – done on 2026-11-20.** The image of the `Dockerfile` was run with a folder of content and the audience of a local demonstration, and a browser took a picture of fourteen pages (Discover, the collection, the search, a profile, the family down and sideways, the connections, the timeline, the story, the gallery, the places, the comparison, "what held", Saved) in both languages at 360, 412 and 1280 pixels: 84 pictures, kept outside the repository (`artifacts/dastan/phase-20/before`). No page was wider than its window, no request failed, and the console said only that a header is not honoured without https. Seen in them: the family that grows sideways is the old still drawing in a mostly empty frame, and Discover is text. **Not taken:** the pages of an account (trees, photographs, requests); they are taken at the beginning of step 20.11, before they change. The tests that fail first are written with each step.

### 20.2 – Relationships and trees when two requests arrive together

*Found:* `End`, `Cancel` and `Answer` change a relationship without the lock of the two accounts that `Ask` and `Block` take; two last relationships ended at the same moment can each see the other and leave the memberships of circles and trees in place. `SetCircle` and `SetTreeMember` read "is there an accepted relationship" and write without a lock. `Link` checks for a loop and writes without the lock of the tree. `Guarded` takes no lock when a transaction is open already, and nothing checks that the open one holds the right locks.

* A table in the ADR: every rule ("no membership without an accepted relationship", "no loop of parents", "one claim", the counts) against the acts that can break it and the locks each act takes.
* Every act of the table takes its whole set of locks, in one order that is written down, from identifiers that never change; reads again what it decides on after it holds them; and writes in the same transaction.
* An act inside another one says which locks it needs; the outer one holds them or the act refuses – an open transaction is no proof.
* A constraint of the database wherever a rule can be said as one.
* After an end or a block, the pages, the JSON, the pictures, the export and the counts refuse from the next request on. A command that repairs memberships left behind by the old code: it can be run twice, removes only what no accepted relationship carries, and reports what it cannot decide.
* *Tests*, with a real PostgreSQL, two connections and gates instead of pauses: two last relationships ended together; one ended while another stays; an end or a block against sharing with a circle or a tree; accepting, changing and ending together; two links that would close a loop, directly and over several steps; an act inside another; a failure that leaves nothing half done.

**Done on 2026-11-20** ([ADR 0022](adr/0022-locks.md)). *Reproduced first:* nine tests failed on the code as it was – somebody stayed in a circle after two last relationships ended together; a membership was written after the relationship had gone, with an end and with a block, for a circle and for a tree; two links closed a loop, directly and over four records; agreeing to another name while the relationship ended failed with an error of the database; a lock asked for inside another act was not taken. *Built:* the locks have one order (account, the two accounts, photograph, restrictions, tree); an act inside another takes what comes later and is refused what would come earlier; every act on a relationship holds the lock of its two accounts, sharing holds it too, and every act that changes a tree – links, records, claims, members, making public – holds the lock of the tree; the end of the last relationship and a block take the locks of the shared trees after the lock of the two. The database refuses a link of a record to itself and a second link between the same two (migration `KinshipRules`, which first removes such links). `people repair` counts and, with `--change`, takes out who is in a circle or a tree without an accepted relationship, and names trees with a loop. *Shown by tests:* the eleven of `TogetherTests` (two of them the controls: one relationship of two ends and the sharing stays; links in two trees do not wait for each other), three of `MembershipRepairTests`, the command in `OperatorCommandsTests`. **Not shown:** a page, a picture or the export asked for by a browser after the end – the store refuses, and the pages ask the store each time, but no test walks it in a browser yet (it is one of the journeys of step 20.12); more than two requests at once on one pair.

### 20.3 – A mail address a provider did not prove

*Found:* for Microsoft the address is the field `mail` or, failing that, a principal name that looks like an address; the new account is made with the address confirmed. Neither field proves that the mailbox exists or is the visitor's. Signing in through a provider does not look at whether the address is confirmed. The answer `address-has-account` tells somebody who proved nothing that an address has an account.

* Who somebody is at a provider (provider and subject) and the confirmed address of the account are two things. An account is found by the first, never by an address.
* An account made through Microsoft confirms its address by a mail of Dastan's own; the visitor can write another address first. A state "waits for confirmation" with its pages: confirm, send again, correct. Through the outbox, with the limits, the one use and the end of the link as they are.
* Google stays as it is where the provider says the address is verified.
* No session of an account, by any way in and for no cookie that exists, before the address is confirmed.
* Two accounts are never joined because their addresses are alike, and before an address is proven nothing says whether it has an account.
* From now on an account records how its address was confirmed. A command that lists accounts without such a record and asks them to confirm again, without touching their data; no evidence is invented. (No account was ever made through Microsoft: there is no client.)
* *Tests:* a principal name only, no address, an address the provider did not verify; coming back twice before confirming; correcting and sending again; an address that has an account; a link that is old or used; a private address asked directly and an old cookie; an account that was confirmed before. The mail really sent to the catcher. A real provider and a real mailbox are the owner's.

**Done on 2026-11-20** ([ADR 0016](adr/0016-identity-and-mail.md), its last section). *Reproduced first:* an account made with an address Microsoft only named was signed in at once. *Built:* a provider carries "proven" only for an address it says it verified (Google); every other address is written by the visitor into the form – what the provider named is a proposal there – and confirmed by a mail of the site through the outbox; until then the account is none: signing in through the provider answers "this account waits", and a sign-in of the account does not hold on any request. Who waits has the mail once more or corrects the address; a new link ends the one before. An address that has an account makes none and is answered in the words of a mail that was sent, at the form and as a correction, and its holder is told. How an address was confirmed is written down; the migration writes "by mail" for accounts that never had a provider and nothing for the others; `people addresses` lists those and `people addresses ask <provider>` asks them anew, once. The privacy notice says it in its three languages. *Shown by tests:* six of `UnprovenAddressTests`, five of `UnprovenAddressPagesTests` (one of them reads the mail from the catcher), the commands in `OperatorCommandsTests`. **Not shown:** a real provider or mailbox; the two new forms were looked at in a browser on the machine of development with the made-up provider (`Dastan__SignIn__MadeUpProves=false`), not in the published build.

### 20.4 – An institution is an entity, and a time there is a stretch of its own

*Found:* an affiliation names its place in words; people are found through a place by the key of those words and an identifier made up on the spot; the context of a relationship is words too. An account cannot state the same place with the same role twice, whatever the dates.

* A reference to an entity that cannot be mistaken: the collection and the entity, never a name or an address. Where one real institution is in several collections, a reviewed "these are the same" – nothing is the same because its name is.
* The store of the people asks a small directory of what is published (name, kind, place, other names, earlier names) and nothing of the editors' work.
* An affiliation and the context of a relationship can point at an entity; the words stay and stay allowed when nothing is chosen. Words never make an institution and never attach to one by themselves.
* A chooser: name, kind, place, what tells it from another of the same name, other and earlier names. Bringing Persian and Arabic letters together helps finding and decides nothing.
* Several stretches at one place in one role; a gap stays a gap; a question about a year inside the gap finds nobody. What counts as "the same statement twice" is said anew, without "account, place, role".
* A migration that keeps numbers, words, dates, roles, audiences and consent; only what is certain is pointed at an entity.
* The profile of an institution can lead to "who was here" – only those who allowed being found, never across a block, with their dates only as they show them. That a place is public makes nobody's time there public.
* Changing the context of an accepted relationship asks the other side again, as ADR 0018 says.
* *Tests:* other and earlier names; two places of one name; a new name for the same place; the same identifier in two collections; a place of a story and a real one; words without a place; the migration; a place taken back; hidden dates; not found by choice, blocked, closed; two stretches and the gap between.
* **The collection that is published has no university, company or club.** The parts are built and tested with made-up places that are labelled so; a reader has something to choose only when such content exists and is approved.

**Done on 2026-11-20** ([ADR 0023](adr/0023-institutions.md)). *Reproduced first:* a second stretch at the same place in the same role was refused. *Built:* a reference of two parts, collection and entity (`EntityReference`); a directory of the places of the collections as a visitor who has read nothing sees them (`Institutions`: universities, companies, organisations, clubs, teams, governments), which is all the personal side knows of the collections; an affiliation and the context of a relationship can point at an entry, and the words stay beside it. The forms of `account/affiliations` and `account/people` look for a place by a name – other and earlier names, every language, Persian and Arabic letters brought together – and offer what they find with kind, seat, collection and other names; the visitor picks one or "none of these", and a form that names anything the page did not offer is refused. "Who was here" asks by entry and by every entry that shares an exact identifier of an outside catalogue with it, under the unchanged rules of ADR 0018; the page of such an entity leads to the question and names nobody; the search by words stays and names the entries of those words. Several stretches at one place are several statements; the same place, role and times twice is one. The migration `InstitutionReferences` adds two empty columns and points nothing at anything. The copy of one's own data names the references. *Shown by tests:* seven of `InstitutionsTests` (the gap, found by entry and not by name, the same place in two collections and under a new name, two stretches, hidden, blocked and closed, the context of a relationship, what the migration keeps), five of `EntityReferenceTests`, nine of `InstitutionPagesTests` on a site with two collections of made-up places (three entries of one name, former and Persian names, Arabic letters, a place of a later stage, a character, forms that make an entry up, a close identifier that joins nothing, pointing later and words again, a withdrawn entry on a second site over the same accounts, colleagues at two employers of one name, Persian). **Not shown, and not built:** a way for a visitor to propose a place to the editors – who finds none writes words; nothing looks through the statements in words that exist; the name of a relationship's place is written without a link to its page; in a browser, only the form of `account/affiliations` with its choice was looked at – on the machine of development, in Persian, dark, in a column of 360 px, with made-up places – and not the form of `account/people`, not the search by entry, not English or light, and nothing in the published build, whose collection holds no such place.

### 20.5 – The parts every page is made of

* The tokens of ADR 0004 grown, not replaced: the colours by meaning, steps of space and of size, the ring of the focus, what is chosen, corners, shadows; light, dark and more contrast as today.
* Parts, each made once: the head of an identity (picture, name, kind, a line, two or three facts), a card with a picture, a row of a fact, the bar of where one is with the switch of the views, the inspector, a chip of a filter, the chooser of an audience, a request for a relationship, and the states "loading", "nothing here", "went wrong".
* The frame of the site as the sketches show it: the name, Discover / My World / Saved, the search and the account above; language and appearance together, away from the main ways; on a phone a bar of four at the foot. Saved is there when it is empty.
* A few icons as drawings in the page, from one source whose licence is recorded.
* Where no picture is: a drawing in its place that says what kind of thing this is – never a picture that pretends.

**Done on 2026-11-21** ([ADR 0024](adr/0024-parts.md)). *Built:* the head of every page – the name; Discover, My World (where the site has accounts), Saved and Search as one list, the way a page lies on marked; the account; the language and the appearance together behind one control that needs no script and, with the script, closes when the visitor presses elsewhere or Escape. On a phone the list of the ways is a bar at the foot of the window, the page ends above it, and the stage of a chart is as high as what is left. Saved has its place while it is empty. The tokens of ADR 0004 keep their values; added are a lower ground, the ground of what is chosen, danger and in-order, two shadows, corners, a smallest size of text, a wide measure, the height of a target and of the bar, and more contrast for who asks the device for it. Parts made once: `IdentityHead`, `NodeCard` with its `Tall` form, `ViewBar`, `Chip`, `AudienceChoice` (taken out of the two pages that each wrote it), `RelationshipRequest`, `StateNote`, `Icon` – twelve drawings of Dastan's own, no set of somebody else. A page `/parts` shows them side by side on a machine of development and does not exist elsewhere. *Shown by tests:* seven of `SiteFrameTests`, six of `FrameBrowserTests` (the bar at the foot at 360 and 412 px with ways of at least 44 px and the foot of the page above it, the row of a wide window, the control with a pointer, with the keyboard and without script, no rule of accessibility broken); thirteen earlier tests were brought to the new head – two of them now say that Saved is offered while it is empty; the whole suite, 3195 tests, passes. *Looked at:* in the published build, the start, a profile, the connections, Saved and the search in Persian and English at 360, 412 and 1280 px, light and dark – 68 pictures, of which three were looked at by eye and all were checked for a page wider than its window (none) and for failed requests (none); the page of the parts on the machine of development, Persian and English, light and dark, 360 and 1280 px. **Not done:** the inspector is the panel of ADR 0019 as it was – it is restyled with the explorer (20.8); the pages that exist are not yet put together from the new parts, except the two that chose an audience and the request for a relationship – that is the work of 20.7 to 20.11; "My World" has no page of its own yet (20.11) and leads to the people of the account; no page was looked at in Firefox or Safari by eye, on a real phone, or with a screen reader.

### 20.6 – One state of exploring

* A contract no renderer knows: what is explored and in which context, the view (connections, timeline, pictures), the filters, the direction of a tree, what is chosen, and how far the reader has read.
* The switch of the views keeps what fits; a filter that does not fit the new view is named as set aside, not dropped in silence.
* In the address: what is public and stable. Not every movement of the drawing; nothing private; an address grants nothing.
* Choosing a node fills the inspector; making it the centre is an act of its own. Opening more, going back and coming back from a picture keep what was chosen and where the drawing stood.

**Done on 2026-11-21** ([ADR 0025](adr/0025-exploring.md)). *Built:* `Exploration` in the core – what is explored, the view, what is chosen, what each view was told, how far the reader has read; read from the query of an address and written to one, knowing no renderer. Every view keeps the names its page always had; what a view was told is carried under a name of its own while another is shown (`tree` and `dir` for the two that both say `view`), and each page passes its links through `Carry`. The same bar (`ExploreBar`) on the page of a name, its connections, its family, its chart of time and its pictures: the views this visitor has, the one shown marked, what is chosen with "put in the middle", "the whole profile" and "choose nothing", and what was left out elsewhere and is shown here, named. Every statement of the list of connections offers "look closer". The drawing that moves chooses what the address says, writes what is chosen into the address and the links of the views and takes it out again; the timeline keeps the links in step with its stretch of years. The drawing of connections carries as open only what the projection says is open. *Shown by tests:* eleven of `ExplorationTests` (thirty cases: the round through the views, the two meanings of `view`, a step inside a view, what is set aside, choosing and centring, twenty-one values that are no choice, a name twice, the count of rows, the reading position in no address), eleven of `ExploringPagesTests` (the bar on four views, a view a name does not have, pages of no single name, choosing and the three acts, something not yet read chosen is the page of a name nobody has, the round family – pictures – family, the note of what is set aside, Persian), two in `GraphBrowserTests` (a card chosen with the keyboard is in the address and in every link of a view, the page asked with that address shows it chosen, Escape takes it out). *Looked at:* the connections with something chosen and years set aside, in English, light, 1280 px, and the family with the same in Persian, dark, 360 px, on the machine of development. **Not built, and said in the ADR:** the connections and the pictures do not leave years out – they name the wish as set aside; where a drawing stood (its movement and size) is not kept across views; what is chosen is a block of the page, not yet an inspector beside the drawing (20.8); exploring a story or a thread as such. **Not shown:** the timeline keeping the links in step was not walked in a browser – no name of the published collection has a chart of time of its own at a stage a test reads to; nothing was looked at in the published build, and the pictures and the page of a name were not looked at by eye at all.

### 20.7 – Discover (first sketch)

* A large search; one story, era or subject with its picture; "follow the next thread" with cards; a real small piece of a drawing of connections or of a comparison; nothing needs an account.
* Only what is approved and published is shown. The cards of the sketch (a country through time, a club, universities) are subjects the collection does not have.

**Done on 2026-11-21.** *Built:* the start of the site as a page wider than a page of text: the name and a search that is the largest thing after it – of the collection where the site has one, of all of them where it has several –; what the site has to begin with as a feature with the picture of its subject or the drawing of its kind; the names the editors say to start with as cards with the picture above the words (at most six); the threads of the collection where it has any; a real piece of a drawing – the connections of the first of those names that is connected with something, drawn as its own page draws them, in a frame that scrolls and that the keyboard reaches, with the way to the whole drawing; and what is laid side by side. Everything is asked of the projection of the visitor: nothing of a later stage is on the start, and nothing needs an account. On a phone a drawing that stands for a picture is a band, so that the words have the room. *Shown by tests:* five of `DiscoverPageTests` (the parts in both languages, a reader at the first stage is shown nothing of Sohrab, no link to an account and every link of the start opens, several collections), five of `DiscoverBrowserTests` (the page fits at 360, 412 and 1280 px in both directions, light and dark, the cards beside or under each other, the frame of the drawing, no rule of accessibility broken, the search in the first screen finds a name). *Looked at:* in the published build with the collection of the repository, the start, the connections with something chosen, the family and a profile in Persian and English at 360, 412 and 1280 px, light and dark – 56 pictures, of which the start in Persian at 1280 px light and the connections in English at 360 px dark were looked at by eye, and all were checked for failed requests (none); on the machine of development the start in English at 1280 px light and in Persian at 360 px dark. **Not built, because the collection does not hold it:** an era or a subject with a picture of its own, cards of several subjects (a country through time, a club, universities) and a picture for any name – the collection that is published has one story and no picture that may be shown at the first stage, so the start shows drawings of kinds; "follow the next thread" is on the page only where a collection lays a thread, and the published one lays none; a piece of a comparison is its card, not a piece of its chart.

### 20.8 – The explorer (second sketch)

* The head of the context and the switch Connections / Timeline / Pictures; "add to the comparison"; the tools of the drawing small and in sight (smaller, larger, all of it), the rest (saving a picture, how it is drawn) behind "more", with names and keys.
* Cards of the graph with a picture, in sizes made for them.
* Wide: the drawing and beside it an inspector that does not cover it – picture, name, kind, a line, the nearest facts, "make this the centre", "the whole profile". A phone: the drawing, and a sheet from the foot that takes the focus, closes with Escape and with Back, and gives the focus back.
* The order of the telling under the drawing where the context is a story, never past what the reader has read.
* **A tree that grows sideways** goes through `GraphView`: the state and the address, the JSON, the placing, the lines, the keys, the window and the saved picture – both directions, both languages, with several parents, partners and relations side by side. The old drawing stays as what a page without a script shows.
* What comes next: follow a connection, compare the times, see the pictures of this context – only from what is published and sourced, inside what was read.

**Done on 2026-11-21** ([ADR 0020](adr/0020-graph.md), its last section; [ADR 0025](adr/0025-exploring.md), the comparison). *Built:* the inspector of a drawing – beside the drawing in a wide window, where it takes room and covers nothing; a sheet at the foot of a phone that opens when asked, takes the focus, and closes with Escape and with the Back button –, with what is chosen, its picture, kind, a line and facts, "put in the middle", "the whole profile", "add to the comparison" and the ways to its timeline and pictures; the tools of the drawing in sight with their keys and the rest behind "More"; the drawing told when its room changes; **the family that grows across the page drawn by the graph** – `flow: across` in the address of the graph, columns of generations with the oldest where the page begins, in both directions of writing, with keys, window and saved picture, the old drawing left as what a page without script shows; under the drawings of the connections and of the family the steps of the telling the name takes part in, as far as they were read, and what comes next (threads, its times, its pictures); the comparison of a visitor – up to five names whose times stand beside the first in the chart of time, carried in the address on every view. *Found on the way and corrected:* the bar of the ways on a phone was 48 px high while the page left 60 px for it – a later rule about the links was stronger; an action that was hidden in the inspector was shown, because the look of an action set its display. *Shown by tests:* six of `ExplorerPagesTests` (tools and inspector in the page, the family across as a graph with the rows and places of the tree that grows down, the address that puts a card in the middle, the steps of the telling at two stages, the comparison on a site with dated statements, what is compared and not yet read), six of `ExplorerBrowserTests` (the inspector beside the drawing in both directions with the chosen card in sight and the room given back; the sheet of a phone with Enter, Escape and Back; the columns of generations in English and Persian with no two cards on each other, the arrow towards the young and a saved picture wider than high; no script), one more of `ExplorationTests`, and the tests of the graph brought to the inspector. *Looked at:* on the machine of development, the family across with something chosen in Persian, dark, 1280 px, and the sheet in Persian at 360 px. **Not built:** cards of the graph in sizes of their own – all cards of a drawing have one width; a stretch of years that the connections honour; where a drawing stood kept across views; exploring a story as such – the order of the telling is that of one name. **Not shown:** nothing was looked at in the published build; the connections with the inspector were not looked at by eye in a light theme; Firefox and WebKit run the tests on the build server only; two fingers and a real phone.

### 20.9 – The profile of a node (third sketch)

* One head for every kind: the picture, the kind, the name, where, a line, three facts, "explore the connections" and "keep".
* Overview / Connections / Timeline / Pictures – the same switch as in the explorer, the same state.
* "In story and picture" as cards; details, other names and sources folded away until asked for.
* An account, the record of a person in a tree, somebody of history and somebody of a story look different at the first glance.
* The one contract of a profile (ADR 0017) feeds the card, the inspector and the page.

**Done on 2026-11-21** ([ADR 0017](adr/0017-node-profile.md), its section "The profile as a page"). *Built:* the page of an entity begins with `IdentityHead` – a mark of what it is (`IdentityMark`: of a story, of the world, decided by the type), the picture or the drawing of the kind (under an arch for what is of a story), the kind, the name as the one title, the position of a place and the collection, the line, the facts of a card, and "Explore the connections" and keeping; then the bar of step 20.6, unchanged; then "In story and picture" – up to three media items and every step of the telling the name has to do with, as cards, each step with the way to what supports it; then what is stated, the connections and the threads as before; and last a fold that needs no script with every fact of the profile, which picture the head shows, the other names, the sources and what other catalogues call it. The pages of an account and of a record in a tree, the private and the public one, carry the mark of what they are above the name. Two drawings were added (an open book, a globe). *Shown by tests:* nine cases of `ProfilePageTests` (the head and its mark for a figure of a story, a person and a place of the world in both languages; the cards at two stages with nothing of a later stage; the fold closed, complete and after everything else; the head saying what the profile for the inspector says), the mark of a record and of an account in `TreePersonPagesTests` and `PeoplePagesTests`; seven earlier tests of the pages and two classes of tests in a browser were brought to the picture standing in the head – `NodeProfileBrowserTests` now also opens the fold and asks the rules of accessibility with it open, at 360 and 1280 px in both languages. The whole suite was run once: 3261 of 3269 passed, the eight that failed were those of the two classes in a browser, which pass since. *Looked at:* in the published build a figure of a story, a place and a work, in Persian and English at 360, 412 and 1280 px, light and dark – 36 pictures, of which two were looked at by eye and all were checked for a page wider than its window (none) and for failed requests (none); on the machine of development the page of Rostam in English, dark, 1280 px with the fold opened, and in Persian, light, 360 px. **Not built:** an account and a record in a tree do not use `IdentityHead` – they have no profile of ADR 0017; the mark stands above their names (kept so in 20.11, with the reason in ADR 0024). The switch of the views has no "Timeline" for a name without dated statements – it is offered where there is one, as since 20.6. **Not shown:** the pages of an account and of a record in a tree were not looked at in a browser after the change; the suite was not run a second time as a whole after the eight were corrected; Firefox and WebKit on the build server only; a real phone and a screen reader.

### 20.10 – Stories, times and pictures

* The page of a story on the timeline of Dastan's own: the order of the telling, the order inside the world, the calendar and deep time stay four things; nothing is converted between axes that have no common measure.
* The still chart of the server says the same as the live one: a window as a window, deep time on its axis – or an honest table where a chart cannot say it.
* A gallery that begins with the pictures, with the context for who asks, and a way back that returns where one was. One picture in several places with a caption for each (R-MED-07), by reference, not by copy.

**Done on 2026-11-21** ([ADR 0021](adr/0021-timeline.md), its last section; [the format of a collection](collection-format.md), `placements`). *Built:* the page of a story gives its steps to the timeline of the site – one view on the axis of the order of the telling, a lane for a name, a mark at every step it has to do with, leading to the cell of the table that says the same; the chart of the server is the still drawing and one press away. A view says whether it is the order of the telling or the order inside the world (`TimeView.InWorld`) – before, an English word was written into the name of the telling. The still chart draws a window in which something happened once as that window; what is counted in a depth of time or in the order of a telling stands under it as a table of its own axis, named as the moving view, and is no longer listed as "not drawn". A gallery begins with its pictures: why they are there, the form of a time and the tools of order and kind are one fold above them, open when something was chosen in it or nothing is shown. The page of a picture is opened with the place, the time, the order and the kind of its gallery, believes of them what the projection confirms, leads back to that gallery at that picture and has its neighbours there. A picture may have `placements`: a caption and sources for an entity or a step it shows; galleries, the cards of a profile and the page of the picture show the caption of the place, and the picture stays one. *Found on the way and corrected:* on a phone the name of a mark of the timeline lay over the mark where the view ended – a name now stands before its mark where it has no room after it, is shortened to whole words with three drawn dots where it has room on neither side, and its length is measured instead of guessed from its letters. *Shown by tests:* twelve cases of `StoriesTimesPicturesTests` (the story as data of the timeline at two stages in both languages, the window in the still chart, the table of the other axes, the captions and the source of a place in both languages and at a stage before the picture, the fold of a gallery, the way back of four galleries with every neighbour, an address that claims what the gallery does not hold), four of `MediaPlacesTests` (the reader and what it refuses), two more of `TimelineViewsTests`, two cases in a browser in `StoryChartBrowserTests` (the view drawn in English at 1280 px and in Persian at 390 px, one mark for every cell, no name over its mark, no rule of accessibility broken, the chart of the server back when asked); six earlier tests were brought to the cells that carry a name. *Looked at:* in the published build the story, two galleries, a picture opened from a gallery and the timeline of the collection, in Persian and English at 360, 412 and 1280 px, light and dark – 60 pictures, of which two were looked at by eye and all were checked for a page wider than its window (none), failed requests (none) and messages of the browser (none but the known one about an address without TLS); on the machine of development the story in English at 1280 px and in Persian, dark, at 360 px before and after the names were corrected. **Not built:** the order inside the world of a story of a collection – the reader of a collection accepts the order of the telling only; the moving view of a story does not tell "takes part" from "is only mentioned" (the still chart, its legend and the table do); an axis of depth in the still chart – a table says it; themes of a gallery (R-MED-02); the Studio has no inputs of its own for `placements` and none for the translation of a caption of a place. **Not shown:** a placed picture was not looked at in a browser – the published collection places none; the table of the other axes was not looked at by eye; the published build was photographed before the names of the marks were corrected; Firefox and WebKit on the build server only; a real phone and a screen reader.

### 20.11 – What is an account's own

* The parts of 20.5 on trees (seeing, changing and sharing as three tasks), requests, the chooser of 20.4 and photographs.
* Accepting a relationship, being found, showing dates, agreeing to a tag and making public stay five separate answers. An invitation accepts nothing and claims no record.
* Nothing to press for what does not exist (posts, videos).

**Done on 2026-11-21** ([ADR 0024](adr/0024-parts.md), "What is an account's own"). *Built:* My World as a page (`/account/world`) – what waits for an answer first, the four spaces as cards with how much each holds or that it holds nothing, no form; the way "My World" of the head leads there. `AccountBar`, the one list of the spaces, on My World, the people, where one was, the family trees, the photographs and the account. The page of a family tree as three tasks – see, change, share – each a part under its own heading with a way to it, shown only to who may use it; what waits for the visitor in a tree said at its top. *Found on the way:* the first version of the bar offered the photographs on a site that keeps none – an earlier test said so, and the bar and My World now offer them only where there is a folder for them. *Shown by tests:* `My_world_says_what_waits_and_what_is_there_and_decides_nothing` in `PeoplePagesTests` (empty, asked, accepted, the bar marking itself, no form, nothing about posts or videos, who is not signed in), `The_page_of_a_tree_is_three_tasks_and_shows_each_only_to_who_may_do_it` in `FamilyTreePagesTests` (owner, who may change, who may see; where each thing stands; the order of the headings), and in a browser, inside the four runs of `TreePersonBrowserTests`, the tree and My World at 375 and 1280 px in both languages and both appearances: three tasks, a way that leads to its part, four cards, no page wider than its window, no rule of accessibility broken. Two earlier tests were brought to the new way of the head. The whole suite ran once: 3290 of 3291 passed; the one that failed measures a time (a step among 100 people of a drawing, 376 ms against a budget of 300 ms) while every project ran at once, touches nothing of this step and passed when its class ran alone – budgets and where they are judged are the work of 20.12. *Looked at:* before the change, on the machine of development, the account, the people, the family trees and the photographs in English at 1280 px; after it, My World and a tree in English, light, 1280 px and in Persian, dark, 360 px, with every way of the bars at least 44 px high. **Not built:** `IdentityHead` for an account and for a record in a tree (they have no profile of ADR 0017; the mark stands above the name); the pages of the people, of where one was and of the photographs were given the bar and otherwise left as they were; My World does not name an agreement asked in a tree – the tree says it at its top. The five separate answers and what an invitation does were not changed and are shown by the tests of ADR 0014, not by new ones – the journeys of 20.12 walk them. **Not shown:** nothing was looked at in the published build – it has no accounts; a tree with people, with members and with something waiting was not looked at in a browser by eye; Firefox and WebKit on the build server only; a real phone and a screen reader.

### 20.12 – Proven, and written down

* **The whole way, measured:** a real PostgreSQL, a family that branches as families do with several relations between the same people, long names in both languages, pictures with rights; the time of the question, of the projection and of the placing, the size of the answer and of the scripts, the first drawing one can use, the answer to a step, memory – with the machine, the network and cold or warm written beside. Budgets are set before the last measurement, on the machine of development; CI writes its times down and judges what does not depend on the machine.
* The first answer holds a bounded neighbourhood; more comes when asked. Drawing only what is in sight does not bound the work of the server.
* Nothing protected is sent for the browser to hide: rights, publication and the boundary of reading are applied before HTML, JSON, counts, suggestions, small pictures and exports are made. The worker keeps nothing private; signing out and changing the account are tried.
* WCAG 2.2 AA for what changed; 44 pixels for the main things to press as a target of the product; contrast, focus, zoom, less motion, and a way without dragging. No page that scrolls sideways at 360 and 412 pixels; moving a drawing is something else.
* The seven journeys of the review as tests in a browser: an unknown subject found and followed and back; times compared and their pictures found; a story read without anything past the boundary in any answer; several relationships asked, accepted and ended; a university with two stretches and being found apart from dates; a tree and photographs seen and, after the end, refused; all of it with the keyboard and on a narrow screen.
* A script for watching five people do four tasks, with what counts as done without help. Watching them is the owner's.
* The ADRs, `acceptance.md`, `operations.md`, the table of step 19.1 and this plan say what is; what is history is labelled as history.

**Done on 2026-11-21** ([ADR 0020](adr/0020-graph.md), "The whole way, measured"; [acceptance.md](acceptance.md), "Phase 20"; [user-test.md](user-test.md)). *Built:* a measurement of the whole way (`WholeWayBrowserTests`): a family tree of 500 people with 734 links and long names in both languages, kept in PostgreSQL, asked of the store, placed, answered and drawn, each part timed, with the size of the answer and of the scripts and the memory of the page, written down where the environment names a file. The seven journeys of the review as tests in a browser: three of looking (`SevenJourneysBrowserTests`: found, followed and left with the keyboard alone at 360 px in both languages; times compared and pictures found; a story read with every answer of the site read for what lies ahead) and one of two people at 375 px (`TwoPeopleJourneyBrowserTests`: two relationships asked, each accepted by itself, one ended; a place with two stretches found without its dates; a tree and a photograph seen and, taken back, refused). A script for watching five people do four tasks. *Found by the measurement and corrected:* the view of a tree asked the database some two thousand times for a tree of 500 people and took 3.5 s; while one view is read each question is now asked once – 54 ms with the whole suite running beside it, and a drawing one can use after 2.8 s instead of 9.6 s. *Measured:* the table in ADR 0020, with the machine, the network and cold and warm beside every number. The budgets were written into the test after the first measurement and before the last; the whole suite, 3297 tests, passed with them judged. *What bounds an answer:* the rows that were opened, the generations that were asked for, and at most 500 people in a family tree, which the store refuses to exceed – the test asks for it. *Protection:* the walk of a story at the second stage reads every answer of the site – pages, the answers of the graphs, a profile, two searches for the name that lies ahead – and finds no record, no name of the site and no step of a later stage; an address that is asked all the same is answered exactly like one nobody has. That a device keeps nothing private is shown by the tests of step 14 as before. *Accessibility:* every page phase 20 changed is asked the rules of WCAG 2.1 A and AA by a tool in a browser, at a narrow and a wide window, in both languages; the ways of the head, the bars and the tasks are measured at 44 px. **Not built:** a first answer smaller than a whole family tree of a visitor – a tree is bounded at 500 people by rule and goes in one answer; an address that hands out a part of a tree was not made, because the measurement did not ask for it. **Not shown:** WCAG 2.2 by a person – a tool decides neither whether the focus is obscured nor whether dragging has another way (every drawing has keys and links); a screen reader; Safari itself and a real phone; the measurement in Firefox and WebKit on the machine of development, over a real network, with photographs as the pictures of the people, or with more than one visitor; signing out and changing the account against the worker were not walked anew – the tests of step 14 stand; the five people and four tasks, which are the owner's.

### The owner's, before or during this phase

1. **Pictures.** The sketches live from pictures; the collection has drawings in the place of most. Where pictures come from and under which rights is the owner's decision; until then the pages are built for pictures and show honest drawings in their place.
2. **Content beyond the Shahnameh** (a country through time, a club, universities): without it Discover has one story and the chooser of institutions has nothing to choose.
3. A real phone, Safari, a screen reader, five people; a mailbox, a sender and the clients at the providers; the key of the licence of the graph – as at the end of phase 19.

## 22. The logo (2026-11-21, D-082)

After phase 20 the owner chose a logo and handed over a picture of it with a written brief: reconstruct it
faithfully as vectors, make every asset from one source, and put it on the site.

**Done on 2026-11-21** ([ADR 0026](adr/0026-logo.md); `branding/dastan/README.md`). *Built:* the geometry of the logo, traced from the large logo of the approved picture and fitted with curves – the open D, the dot as a circle, the page, the six letters of the wordmark as outlines, 172 segments –, kept as `branding/dastan/source/dastan-geometry.json`; one script that writes from it two masters, ten variants as SVG, eighteen PNGs of the logo and the symbol without a ground, eight icons, `favicon.ico` with three sizes, the code the site draws the logo with, and a sheet of comparison; a small-size variant for 16 and 32 pixels with a heavier page and dot, named as such. On the site: the logo in the head of every page as a drawing in the page (indigo and ink on a light ground, white on a dark one, named once, never mirrored), the icons of the tab, the manifest with icons for "any" and separate ones for "maskable", the page for "no connection", the picture of a shared link; the store of the worker named anew so that a device drops the earlier icon and keeps the pages a visitor read. *Shown by tests:* eleven cases of `LogoTests` (the head in both languages; the site draws the geometry of the brand and no other; no asset holds pixels, a script, a font or anything fetched; the icons of the site are byte for byte those the brand made; the three sizes of the ICO; size, ground and the whole symbol of five icons, the maskable ones inside the circle of 40 %), four cases in a browser in `FrameBrowserTests` (the proportions and the colours of the logo on a light and a dark ground, not turned on a Persian page, every icon the page and the manifest name is there), and seven earlier tests brought to the new names. The whole suite ran once: 3308 of 3311 passed; the three that failed were the new cases in a browser, which asked for the icons from inside the page and were refused there – they ask from the test now and pass, with the other tests of the head, the installed app and the worker (70 cases). *Looked at:* the sheet (`branding/dastan/previews/comparison.png`) – the reference, the master, one over the other with 2.6 % of the pixels with ink differing at the soft edges of the picture, the variants, the symbol at 16 to 64 pixels as it is and enlarged, the logo at 24 to 40 pixels, the maskable icon under a circle and a rounded square; the head of the site in English, light, 1280 px and in Persian, dark, 360 px, on the machine of development. **Not built:** formats for print; a guide of the brand beyond the README; the logo in the tool of the editors (the Studio keeps its plain name). **Not shown:** the app installed on a phone or a computer and what a launcher makes of the maskable icon; the icon of a tab in Safari and Firefox by eye; a screen reader; the published build was not photographed. The original vector file and the font of the wordmark were not available: the geometry is a reconstruction of a picture.

## 23. What phase 20 and the logo left that needed nobody's decision (2026-11-21 and 22)

The steps of phase 20 and section 22 each say what they did not build. Six of those things needed no decision
of the owner, no content and no device; they were built in one piece of work. Everything else that is open is
listed at the end of this section with whose it is.

**Done on 2026-11-21.** *Built:*

1. **The symbol in the tool of the editors** ([ADR 0026](adr/0026-logo.md), point 8): before the name of the
   tool in the head of every page, the icon of a tab; the same geometry, written by the same script.
2. **My World names what a family tree asks** ([ADR 0024](adr/0024-parts.md)): an agreement to be named in
   public and a claim to confirm or refuse, each leading to where it is answered. *Found on the way:* the top
   of a tree said "something here waits for your answer" to somebody who had agreed already; it says so only
   while the answer is missing.
3. **The place of a relationship leads to its page** ([ADR 0023](adr/0023-institutions.md)) where the two
   named an entry of a collection.
4. **Inputs for the places of a picture in the Studio** ([ADR 0021](adr/0021-timeline.md)): rows that offer an
   entity and an event, the row that begins the list, the caption of a place in the other language. *Found on
   the way:* a form that was refused showed again only the fields the record had before – a list or a time the
   form had added was gone; it is shown again as it was filled in.
5. **The moving view of a story tells who takes part from who is only mentioned** (ADR 0021): a hollow mark,
   and the words of the table in the name of every mark.
6. **A drawing is where it was left** ([ADR 0025](adr/0025-exploring.md)): size and movement of the moving
   drawing of a graph, kept with the place of the history, put back by Back, Forward and loading anew.

Also: two lines that stood twice in the style sheet of the site (the colours of the logo for a dark device)
stand once.

*Shown by tests:* `StudioSymbolTests` (4 cases: the head in both languages, the geometry of the brand, the icon
for who is signed in and who is not); `My_world_names_what_a_tree_asks_of_an_account_until_it_is_answered` in
`FamilyTreePagesTests` (a claim for who made the record and nobody else; an agreement for who is named and
nobody else; gone when answered, back when taken back, nothing of a tree one may not see); the link of a place
in `Colleagues_can_say_at_which_entry_and_two_employers_of_one_name_are_two` of `InstitutionPagesTests`, for
both sides; `StudioPlacementTests` (3 cases: the row that begins the list; an entity and an event added with
their Persian captions, taken out again down to no list; a place the item does not show refused by the readers
with the form as it was filled in); the presence of every mark in `TimelineViewsTests`, and in a browser, in
`StoryChartBrowserTests`, every mark against its cell of the table in English at 1280 px and in Persian, dark,
at 390 px; `A_visitor_who_comes_back_finds_the_drawing_as_large_and_as_far_moved_as_it_was_left` in
`GraphBrowserTests` (loaded anew, Back, forgotten by "the whole drawing", whole for who comes first) at 1280
and 360 px. The whole suite ran once after the last change: 3321 tests, none failed. *Looked at:* the head of
the tool of the editors in English, light, 1280 px and in Persian, dark, 360 px, on the machine of development, over an empty store; after the work was merged, the moving view of
the story in English, dark, in a wide window, read to the end – thirty marks, three of them hollow, among them
Sohrab at "The remedy does not arrive" and at "Mourning in Zabulistan", each saying so in its name.

*Found by the build server, and corrected:* the run of the first version failed in WebKit and in nothing else –
a drawing came back moved to the other side on a Persian page of 360 px. The control keeps its movement with
one sign and writes it into its settings with another after some movements, and it turns a movement no finger
makes around; where the drawing stands is now read from the control itself and the step is chosen for what the
control does (workaround 6 of [ADR 0020](adr/0020-graph.md)). The three engines of the browser container run
on the machine of development too (`DASTAN_BROWSER_ENGINE`) – what earlier steps said of "the build server
only" was a habit, not a limit. Run there, the test found two more things: a movement was written a moment
after it ended and was lost to somebody who left within that moment – it is written when it ends –, and WebKit
let the drawing stand two pixels beside where it had stood – the rest of the way is gone once the control has
settled. The tests of the graph then passed twice in each of the three engines before this was pushed again.
After the corrections the whole suite ran again: 3321 tests, none failed. (The run before that one had one
failure, of `StudioSymbolTests`, which compares the icon of the tool byte for byte with the one the brand made
and met a working copy in which Git had written the same file once with CRLF and once with LF. What the script
of the brand writes is LF in every working copy now – `.gitattributes`.) The tests in a browser ran whole in
WebKit (404 of 407 passed, 1 not asked of it) and in Firefox (403 of 407 passed, 3 not asked of it). What failed
there are budgets of time and nothing else: the whole way of the largest tree could be used after 30 to 40 s
against 5 s in both, and a step among 100 people took 312 ms against 300 in WebKit. The budgets were set on this
machine for Chromium, which meets them; the build server writes the times down and does not judge them.

*Looked at afterwards, on 2026-11-22, on the machine of development* (English, dark, the pane of a browser):
the form of a picture in the Studio over the collection of the repository – the row that begins the places,
with an entity, an event, a caption, the Persian caption and the sources; with two made-up accounts, the page
of the people after one asked and the other accepted "colleague" at an entry – "colleague, Made-up University
of the East, since 2010" with the name as a link to its page –, and My World of the owner of a tree after the
other said a record is them – "A made-up family – somebody says a record is them, for you to confirm or
refuse: 1", leading to the claims of the tree; and a drawing made larger, left and come back to with the Back
button, by its numbers – the pane could not be made to show the drawing itself in a picture.

*Found by that look, and corrected:* a drawing that was put back was whole again, and what was kept was
forgotten, as soon as the page gave the drawing another room a second time while it arrived – the tests had a
window that did not change. What the visitor moved now stays where it was put when the room changes; only a
drawing nobody touched is shown whole again, and a drawing that was put back is put back again until the
visitor does something. The test gives the drawing another room twice and failed before the correction. It ran
twice in each of the three engines with the tests of exploring and of a record of a tree; then the whole suite
ran: 3321 tests, none failed.

*Looked at once more the same day:* in Persian, light, at the width of a phone (375 px), with the second of
the two made-up accounts – My World after the owner of the tree had confirmed the record and asked to make the
tree public: "A made-up family – می‌پرسد آیا موافقید نامتان همگانی دیده شود", leading to the agreement of the
tree; and the page of the people: "همکار، دانشگاه ساختگی شرق، از ۲۰۱۰" with the place as a link. Neither page
scrolled sideways, and the logo of the head was not turned. *The published build:* the image of the Dockerfile
was built from this revision and started with a folder of content that is not a published revision; in
Persian, dark, it showed the head with the logo in white, answered the icon of a tab, the icon as a drawing,
the icon of a home screen and the manifest, and said that nothing is published here yet – no story, so none of
the six changes that need a story or an account can be seen in it. It shows content once a revision is
published, which needs every statement reviewed, and that is the owner's as the editor; the image has no
accounts unless it is given a database.

**Not shown:** anything of this section in the published build beyond its head and its icons, for the reason
above.

**Read again as a whole on 2026-11-22**, on the owner's word to review the work and correct what is found.
*Corrected:* the place of a relationship is a link also while the relationship waits – for who is asked, on
the page of the people and on My World, and for who asked; that is where "which university" is the question,
and it was words there. *Made better:* who is there in a step and who is only spoken of was decided twice, in
the still chart and for the moving view – it is one rule of the core now (`PresenceRule.IsThere`), with a test
that names its four and three; the hollow mark of the moving view stands before the rules of a disputed and a
loose statement, so that it cannot take their line away; in the script of the graph a tool and "the whole
drawing" end "put back where it was left" themselves and do not depend on a finger having been seen, what
`seen` says is read from one place, and the condition under which a drawing is put back is said in two plain
steps. *Looked for and not found:* a way in which a place of a picture or its Persian caption is lost or
written twice by the form of the Studio; a way in which My World says something of a tree the account may not
see. *Shown by tests:* the link while waiting in `InstitutionPagesTests` (three places), the rule in
`TimelineViewsTests`; the tests of the graph, of exploring, of a record of a tree, of the chart of a story and
of the timeline ran in Chromium, WebKit and Firefox (61 each, none failed); then the whole suite ran: 3321 tests, none failed.

**What is still open, and whose it is.** Nothing below is forgotten; none of it can be built without the one
named.

| Open | Whose | Why not built here |
|---|---|---|
| Themes of a gallery (R-MED-02) | – | **Decided and built on 2026-11-23** (D-083, section 24): a theme is a record of its collection. Which themes a collection has stays the decision of its editors. |
| A stretch of years that the connections and the pictures of a name honour | – | **Decided and built on 2026-11-23** (D-084, section 24.2). |
| A way for a visitor to propose a place to the editors | – | **Decided and built on 2026-11-23** (D-085, section 24.3). Somebody has to look at the page of the proposals in the tool of the editors: that stays with the owner. |
| Pictures, and content beyond the Shahnameh | the owner | Rights and sources; without them Discover has one story and drawings in the place of pictures. |
| The order inside the world of a story of a collection | the owner, with content | No collection states one; a comparison file can. |
| Cards of a graph in sizes of their own; a first answer smaller than a whole family tree | nobody yet | The measurement of step 20.12 did not ask for either. |
| `IdentityHead` for an account and a record in a tree | – | **Decided and built on 2026-11-23** (D-086, section 24.4). |
| Formats of the logo for print; a guide of the brand | – | **Decided and built on 2026-11-23** (D-087, section 24.5). A proof on paper stays with the owner. |
| A real phone, the app installed, Safari itself, a screen reader, five people who see the site for the first time | the owner | They need a device, eyes and ears. |
| A mailbox and a sender, the clients at Google and Microsoft, the key of the licence of the graph, a host and an address | the owner | Accounts, contracts and secrets are entered by the owner alone. |
| The legal pages read by a lawyer, the postal address | the owner | Section 18.8. |

## 24. Five decisions of the owner (2026-11-23, D-083 to D-087)

Section 23 ended with a table of what was open and whose it was. On 2026-11-23 the owner was given, for each of
the five rows that were a decision about the product, the ways it could go with what each costs and a
recommendation, and answered: themes as records of a collection; a stretch of years honoured by all three
views of a name; a way to propose a place to the editors; a full identity head for an account and for a record
of a tree – "not a short one: what we build is built whole"; a guide of the brand with the formats for print.
The order of work the owner set for what follows: everything is tried in the browser on this machine first,
then content is written, then the site goes online. Each decision is built whole, with its tests and its
documents, before the next.

**24.1 Themes of a gallery – done on 2026-11-23** (D-083, [ADR 0027](adr/0027-gallery-themes.md)). *Built:* the
type `Theme` of an entity; `themeIds` of an image or a video; the rules of a collection for them (a theme is
named as a theme and nothing else is, none twice, none of a later stage, nothing depicts a theme, a theme may
rest on no source); the themes a reader is offered, each with the number of what stands under it and only with
something under it; the gallery of a theme, which says "gathered under" and is arranged like every gallery; the
themes above the pictures of the gallery of a collection and of a theme; the themes of an item on its page; the
page of a theme; in the tool of the editors the themes to tick in the form of every image and video, kept apart
from who and what the item shows. *Tried:* `MediaThemesTests` (15), `GalleryThemesTests` (6, English and
Persian), `StudioThemesTests` (2).

**24.2 The years of a name – done on 2026-11-23** (D-084, [ADR 0025](adr/0025-exploring.md), "The years of a
name"). *Built:* the stretch of years of the state of exploring kept to by the connections and the pictures of
a name as by its chart of time; the neighbourhood of a name in a stretch of years – what held in it, what
states no time told apart, what states another time counted and left out, a node reached and opened only by
what is part of it; the gallery of a name in a stretch of years, in the same three parts; the same
neighbourhood for the drawing that moves; on the bar of exploring the years to ask for, to change and to give
up, as a form that sends the rest of the state along and is kept in step by the script; years read as they are
typed, also in Persian; what is no stretch of years said to be none; a picture that keeps the years of the
gallery it was opened from, with its way back, the one before and the next. *Tried:* `ExplorationTests` (the 9
new ones among 41), `YearsOfANameTests` (5), `YearsOfANamePagesTests` (9, English and Persian).

**24.3 A place proposed to the editors – done on 2026-11-23** (D-085, [ADR 0023](adr/0023-institutions.md), "A
place proposed to the editors"). *Built:* the table of the proposals and its box – what is a proposal and what
is none, a box that is full, the decision of an editor, forgetting what was decided long ago –; on the page of
the places somebody was at the form that proposes one, with the kinds people can have been at, offered also
where looking for a place found nothing, with the name carried along; the limit of five in an hour; the word
a statement in words gets when an entry goes by its words now, with the way to choose it; in the tool of the
editors the page of the proposed places – the same place proposed several times as one, the way to begin its
name with what was proposed, the decision with the entry the place has –; a new name that begins with a type
and a name a link of the tool asks for; the command that forgets reports forgets decided proposals too; the
privacy notice in its three languages and the record of processing name what is kept. *Tried:*
`ProposalBoxTests` (19, among them that the tables are what the history of their changes of form makes them),
`PlaceProposalPagesTests` (11, English and Persian), `StudioProposalsTests` (6). *To do by who runs a site:*
`database migrate` before the new version starts.

**24.4 The head of an account and of a record in a tree – done on 2026-11-23** (D-086,
[ADR 0024](adr/0024-parts.md), "The head of an account and of a record in a tree"). *Built:* what an account
says about itself – a few words with an audience of their own and a picture that is one of its photographs –,
kept, read by who the scopes allow, looked at as somebody else, given back in the copy of one's data, taken
back when a circle or a photograph goes; the form for it on the account's own page; the part `PersonHead` and
with it the head of the page of an account – picture or drawing, name, words, the first places – and of the
page of a person of a tree, private and public – picture or drawing, name, tree, the life in two lines –; the
profile of a person of a tree without a second picture; the privacy notice in its three languages and the
record of processing. *Tried:* `AccountDescriptionTests` (13, among them that the tables are what the history
of their changes of form makes them), `AccountProfilePagesTests` (6, English and Persian),
`TreePersonPagesTests` (13, brought to the head). *To do by who runs a site:* `database migrate`.

**24.5 Files for print and the guide of the brand – done on 2026-11-23** (D-087,
[ADR 0026](adr/0026-logo.md), addendum; `branding/dastan/README.md`). *Built:* the script that writes, from
the geometry and by Node alone, the logo and the symbol for print – colour, white on indigo, black – as a PDF
and an EPS each, outlines only, in CMYK, cut to the clear space; the guide of the brand as one page of HTML
and as a PDF of nine pages. *Tried:* `BrandPrintTests`; the colour file read as a PDF and looked at; the guide
looked at in a browser. *Not tried:* a proof on paper – the CMYK values are a beginning and are said to be.

**What section 24 leaves open, and whose it is.** A proof of the colours on paper, a Latin font of the brand,
the registration of the mark: the owner. Somebody who looks at the proposed places and at the reports: the
owner, as editor. Years on a family tree and on the page of a name; themes across collections; facts of an
account beyond a few words and a picture: asked for by nothing. Everything else that was open before stands in
the table of section 23.

**Section 24 as a whole, tried and looked at on 2026-11-23.** The whole suite ran after the last change: 3435
tests, none failed. *Looked at in a browser, with made-up content:* the gallery of a collection with its themes
in Persian and the gallery of a theme in English, dark, 1100 px; the connections of a name in a stretch of
years with the form of the years, English; the pictures of a name in a stretch of years in Persian at 375 px,
where nothing reaches beyond the screen; the form that proposes a place in Persian at 375 px; the page of an
account with its head, the form for it filled in and sent, Persian, 375 and 1100 px; the page of the proposed
places of the tool of the editors in Persian, with the same place proposed twice standing together; the guide
of the brand; the colour file for print read as a PDF. *Found by looking and corrected:* two sentences about
what was left out of the years stood without a blank between them; the example of a year before the common
era was turned around in a Persian sentence; the field for the address of a page had the look of the browser
and not of the form. *Not looked at:* a light appearance of these pages, a phone, a screen reader, the files
for print on paper.

## 25. What the owner asked for after section 24 – the plan (2026-11-23, D-088 and D-089)

*Planned, not begun.* Section 24 ended with four things "nobody asked for". On 2026-11-23 the owner asked for
all four (D-088), and for real content to try the site with: the Shahnameh fuller and more exact, a stretch of
the history of Iran, of Rome and of China, a family tree in the owner's own account, and fitting data wherever
else a part needs it (D-089). The owner then set the order: the development that is left is done in this line
of work; the content is written in a line of work of its own, from
[the work order for content](content-work-order.md); then everything is tried in a browser on the owner's
machine; then the site goes online. [The backlog](backlog.md) is the list; this section is the plan of the
development – for each step what is built, in which files, by which rules, how it is tried and what is
written down. Nothing of it is begun before the owner says so.

The order is 25.1 and 25.2 first, because the content needs them; then 25.3 to 25.6 in the order in which a
later one leans on an earlier one; then 25.7. Each step is built whole – code, tests, documents, a look in the
browser in English and Persian, light and dark, 375 and 1100 px – merged and green before the next begins.

**25.1 A folder for the real collections (backlog A6).** *Why first:* content is written into it from the
first day. *Exists already:* the folder `library/` with `samples`, `comparisons`, `media`, its README, and its
exclusion from both images. *To build:*

* A test project reads `library/` the way the site does: `LibraryTests` in `Vafadar.Dastan.Data.Tests` calls the
  check of the content (`ContentCheck`) on the folder and fails with the list of problems the command prints.
  A library that holds no file yet is in order; a notice (a `REPLACE` text, an address at `example.org`) fails
  as a problem does, because a collection with a notice is not finished. The folder is found from the root of
  the repository as the tests find `content/`.
* A second test starts the site on `library/` (`WebApplicationFactory` with `Dastan:Content`) and asks for the
  entry page of every collection of it, in English and Persian – so that a file that passes the check and
  breaks a page is found. With an empty library the test asks for nothing and passes.
* A third test reads both `.dockerignore` files and fails if `library` is missing from one.
* `Vafadar.Dastan.Web/Properties/launchSettings.json` gets a profile `library` (port 5220, `Development`,
  `Dastan:Content` relative to the project). The work order names it in place of the long command.
* The check of an empty folder says today "the folder holds no content file" as a problem. For `library/` that
  is no problem: the command gets the answer "in order, and empty" for a folder that has the three folders of
  a content root and no file. `content/` is not affected.

*Tried by:* the three tests; the command on the empty folder; a made-up file with a fault put into `library/`
on this machine and taken out again. *Written down:* README, guide for writers ("Where files go"), work
order, operations ("What is in no image"). *Not part of it:* a second database, a second site – the library
is a folder.

*Done on 2026-11-23.* *Built:* `ContentCheck.IsContentRoot`; the command calls a content root without a file
"in order, and empty" and a folder that is no content root still a problem; a site that is told no collection
to begin with (`Dastan:Sample` empty) begins with the first file of the folder and says what is missing where
there is none; the launch profile `library` (port 5220). *Tried:* `LibraryTests` (7 – every file of the library
usable and finished, nothing released by who wrote it, in no image, the command), `LibrarySiteTests` (5 – the
pages of every collection of the library in English and Persian, a site told no collection, the profile).

**25.2 A family tree written from a file (backlog A5).** *To build:*

* `Vafadar.Dastan.People/TreeFile.cs`: the format `dastan-tree-0.1` of the work order, part 7 – `name`,
  `people` (`key`, `name`, optional `born`, `died` as years, `deceased`, `self`), `parents` (`parent`, `child`,
  optional `kind` of `parent`, `adoptive-parent`, `guardian`), `partners` (pairs, optional `since`, `until`).
  Reading is strict: an unknown field, a key used twice, a key that is not there, a person who is their own
  ancestor, more than one `self`, a year that is no year, a `died` without `deceased` being true or left out,
  more people than a tree may hold – each is a problem with its place in the file, and with one problem
  nothing is written.
* `PeopleStore.ImportTree(account, TreeFile)`: one transaction. It writes through the same guarded calls as
  the page does (`AddTree`, `AddPerson`, `Link`), so every rule of the store holds – PRIV-003 above all: of
  somebody who lives, the name, the years and the family and nothing else; the file has no field for more.
  The record marked `self` is tied to the account the way the page ties it. The tree is private.
* Twice the same file: a tree is recognised by its name within the account. If a tree of that name exists the
  command stops and says so; with `--add` it adds what the file has and the tree has not – a person is the
  same person when name and years are the same and it stands in the same place of the family – and changes
  nothing that is there. Nothing is ever taken away by an import.
* The command: `people import-tree <account name> --from <file> --people <database>` in `OperatorCommands`,
  with `--add`; it prints what it wrote in numbers, never a name, and writes a line into the log of who runs
  the site as the other commands of `people` do.

*Rules that are decided here:* the account is named by its name and must exist – the command makes no
account; a file is never kept by the software; an import shares nothing and asks nobody to agree to anything.
*Tried by:* `TreeFileTests` (every problem of the list, a good file of three generations),
`TreeImportTests` (written and read back; a second run; `--add`; a file with a loop writes nothing; PRIV-003
holds), a test of the command, and the page of the tree with the imported tree in the browser.
*Written down:* operations ("A tree from a file"), ADR 0024 addendum, the record of processing (no new data,
a new way in), work order part 7 brought to what was built. *Then, on the owner's machine only:* the owner
makes the account; the tree of `.local/dastan-owner-tree.json` is written into it (backlog B9).

*Done on 2026-11-23.* *Built:* `TreeFile` (the format, read strictly, every problem with its place and all at
once); `PeopleStore.ImportTree` (one transaction through the acts of the pages; the account is named and must be
there and open); the command `people import-tree <account name> --from <file> --people <database>`, which
prints numbers and no name. *Decided while building, other than planned:* there is no `--add` – a tree of that
name that is there stops the command, because "the same person" cannot be told safely where two people of a
family have one name; and the file has no `self` – that a record is an account is said by a claim on the site,
which somebody else reviews (PRIV-004), and an import does not go around it. A year of the file is a year of
the common era. *Tried:* `TreeImportTests` (27), a test of the command in `OperatorCommandsTests`.

**25.3 The years of a name on its page and on its family tree (backlog A1).** D-084 made the chart of time,
the connections and the pictures keep to the years; the page of a name and the drawing of a family say "not
left out in this view". *To build:*

* `Exploration.HonoursYears` answers yes for every view of a name; the sentence "Not left out in this view:
  the years" and its text in both languages go, with the tests that ask for it.
* Core, `Projection`: `Profile(focus, years, present)` and `Family(around, up, down, beside, years, present)`
  beside the ones without years, built on `Holding.Of(time, span, present)` as `Connections` is – one rule of
  "held in these years" for all five views, in one place.
* The page of a name in years: every fact and every relation is in one of three parts – *in these years*
  (its time meets them), *without a stated time* (shown apart, under its own heading, never hidden), *at
  another time* (left out and counted: "12 at another time", with a way to see all). The head of the page –
  the name, the kind, born and died – is of the name and is never left out. Statements that "may have held"
  (a window that only touches the years) stand in the first part and say so, as on the connections.
* The family in years: a parent is a parent at every time – descent states no time and is always drawn. What
  can be left out is what states a time: a partnership, an adoption, a guardianship, a membership of a house.
  A person is drawn when a line that is drawn reaches them. Under the drawing: how many lines were left out,
  and how many state no time. The moving drawing gets the same graph from the server as the still one.
* `ExploreBar` offers the form of the years on the two views; `explore.ts` keeps it in step as on the others.
* The public page of a shared tree and the page of a record of a tree of an account are not content of a
  collection and have no state of exploring: not part of this step.

*Tried by:* `ExplorationTests`, `YearsOfANameTests` (profile and family: inside, outside, no time, a window
that touches), `YearsOfANamePagesTests` (the two pages, English and Persian, years typed in Persian),
`ExplorationBrowserTests` for the form on the two views. *Written down:* ADR 0025 ("The years of a name",
brought to five views), D-084 addendum, CHANGELOG.

**25.4 A theme across collections (backlog A2).** D-083 made a theme a record of one collection. *To build:*

* The statement is the editors' and is the one a place already has (ADR 0023): two entities of the type
  `Theme` in two collections are the same theme when both carry the same identifier of the same issuer, marked
  `exact`. A name joins nothing. The rules of a collection learn: a theme may carry identifiers; two themes of
  one collection do not carry the same one.
* Core, `Themes.cs` beside `Institutions.cs`: from the projections of all collections a visitor may read – each
  at the stage the visitor has reached in it – the themes that are shared, each with its collections. A theme
  of a stage the reader has not reached is not there, and neither its name nor its count.
* The site: a page `/themes` – every theme that has something under it, shared ones once – and `/themes/{id}`
  – the pictures of every collection that has the theme, each under the name of its collection, each
  arranged as the gallery of that collection is, each with its own "read further to see more" where the
  reader's stage holds pictures back. From the page of a theme in a collection a line leads there when the
  theme is shared ("also in: …"), and from there back.
* The label of a shared theme on the shared page: the label of the collection of the address the visitor came
  from, else of the first collection in the order of the site. Different labels are not unified – each
  collection keeps its own under its own heading.
* The tool of the editors: the form of a theme has the identifiers (as a place has); the list of themes says
  which are shared and with which collection.
* The search of the site finds a theme once.

*Tried by:* `SharedThemesTests` (shared and not, a later stage, an identifier that is `close` joins nothing,
the same name joins nothing), `ThemesPagesTests` (English and Persian), `StudioThemesTests` extended; two
made-up collections with one shared theme in the fixtures of the tests – not in `content/samples`, which the
other tests count. *Written down:* ADR 0027 addendum, the format page ("Themes"), the guide for writers, work
order part 6. *Not part of it:* a hierarchy of themes; a theme of the whole site that belongs to no
collection.

**25.5 More that an account says about itself (backlog A3).** D-086 gave an account words about itself and a
picture. *To build, each as a field with an audience of its own that begins at "nobody else":*

| Field | Form | Limits |
|---|---|---|
| Year of birth | a year of the common era | a year, never a day; between 1900 and the year before this one; an account says nothing about being of age by it |
| Where it lives | a town and a country, in words | 80 characters each; no address, no coordinates; tied to no entry of a collection |
| Languages | up to eight, chosen from a list of language tags with their own names | no level, no "mother tongue" |
| A page of its own | one address | `https` only; shown as text with `rel="nofollow noopener ugc"`; never fetched by the site |

* People: `StoredVisitor` gets the fields and, for each, a scope and a circle; `PeopleStore.Description.cs`
  gets `DescribeAccount` widened and one method that answers "what may this viewer see of that account",
  used by every page – so that no page asks for a field by itself. A migration by hand with the test for
  pending changes of the model. A circle that is given up takes its fields back to "nobody else", as it does
  for the words about itself.
* The site: the form on the page of the account – one group for each field, its audience beside it, in the
  shape of the form of D-086; the head (`PersonHead`) shows what the viewer may see, in a line of facts under
  the name; "see as" shows it as another sees it.
* Everything a visitor's own does, these fields do: the copy of one's data holds them; closing the account
  removes them; an account that is restricted shows none; the search for people never searches them.
* Privacy: the notice in English, German and Persian names the four and says they are given freely, to whom
  they are shown and that they can be taken back; the record of processing gets the row; nothing of it is
  used to suggest, sort or find people.

*Rules that are decided here:* none of the four is ever required; none is shown to somebody without an
account unless its audience is "everybody" and the account is findable; the year of birth is never used to
compute an age that is shown. *Tried by:* `AccountFactsTests` (kept, shown by scope, with a circle, taken back,
exported, removed, pending changes), `AccountProfilePagesTests` extended (English and Persian, "see as"), the
head in the browser. *Written down:* ADR 0024 addendum, D-086 addendum, privacy texts, record of processing,
operations (`database migrate` before the version starts).

**25.6 Cards of a graph in sizes of their own (backlog A4).** Every card is 220 by 72 today, in the still
drawing of the server and in the moving one. *To build:*

* Three sizes, decided by the server and sent with the graph, so that both drawings agree by construction:
  *main* (the name in the middle: wider and higher, the picture larger, two lines of words), *usual* (as
  today), *compact* (name and kind on one line, no picture). The rule: the focus is main; with more than a
  number of cards on the drawing that is set in one place, cards further than one step from the focus are
  compact; a card somebody chose is at least usual.
* Core: the graph a view gives carries the size of each node; the layout of the still drawing takes the steps
  between columns and rows from the largest card of the column and the row, not from a constant.
* `graph.ts`: `cardWidth`, the height 72, `columnStep`, `rowStep`, `generationStep`, `besideStep` become
  functions of the size; the ports of the lines, the words that are cut, the picture and the frame of the
  focus follow. A compact card that is chosen grows to usual in place and the lines follow.
* The family drawing keeps one size in a generation, so that a row stays a row.

*Tried by:* the tests of the still drawing (no two cards overlap, in the three sizes, from left to right and
from right to left), `GraphBrowserTests` in Chromium, Firefox and WebKit on this machine before pushing (the
rule of the three engines), the budgets of time of step 20.12 – which must hold as they are; if a budget
fails because of the sizes the sizes are made cheaper, the budget is not raised. A look at a drawing of sixty
cards and of three. *Written down:* ADR 0020 addendum, CHANGELOG.

**25.7 Trying it with the owner (backlog C).** After 25.1 to 25.6 and after the content of the backlog, part
B, as far as it is there: the site and the tool of the editors are started on `library/` on the owner's
machine – databases brought up with `database migrate` first –, the addresses are given to the owner, a second
account with a made-up name is made so that circles and "who was here" have two sides, and the light
appearance of the pages of section 24 and of this section is looked at. What the owner finds is written into
the backlog as rows of its own and built before the site goes online.

**What is open, with what is built if the owner says nothing.** The owner was given these on 2026-11-23:

| Question | Built if nothing is said |
|---|---|
| The Shahnameh: how far | the work as a whole, Rostam and Sohrab whole, Siavash |
| The histories: which span | Iran from Elam (about 2700 BCE), China from the Shang (about 1600 BCE, the Xia as tradition), Rome from 753 BCE; all to 100 CE |
| Languages of the content | English and Persian |
| The names of the family tree | in Latin letters, as the owner wrote them |
| Places people can have been at | ten to fifteen well-known universities and organisations, unless the owner names their own |
| 25.5: which facts of an account | the four of the table |
| 25.6: from how many cards a drawing is compact | set after looking at real collections, in one constant |
