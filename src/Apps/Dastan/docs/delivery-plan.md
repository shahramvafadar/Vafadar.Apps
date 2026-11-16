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
| 7 | Graph renderer | Decide by a bounded comparison in phase 4: Syncfusion Diagram first, Cytoscape.js as the single alternative | Choose one now without a comparison | Reversible behind an adapter |
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
| 0 – Repository ready | **Done.** The working copy is a complete clone on .NET SDK 10.0.404; the work of phases 1 and 2 is on the branch `feature/dastan-foundations`. |
| 1 – Foundations of the repository | **Done:** app folder with README, changelog, working rules, specification, sample and media; decisions D-029 to D-034; ADR 0011 with its tests; the repository tables. |
| 2 – `Vafadar.Temporal` | **Done:** the library, 165 tests, [ADR 0012](../../../../docs/adr/0012-temporal-model.md), its README, the solution filter `Vafadar.Dastan.slnf`. M0-08 is checked ([acceptance](acceptance.md)). Changed against this plan: formatting for display is not built yet – it comes with its first screen (phase 4 or 5), because a formatter without a screen would be designed by guessing. |
| 3 – 7 | Not started |

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
