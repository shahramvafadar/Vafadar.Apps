# 20 — Implementation Plan and First Runnable Milestone

**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Actionable implementation recommendation; no Dastan application has been built or tested by this document review

> **In the repository (2026-11-15):** the outcome, the screens and the twelve acceptance conditions of this document stand. Its order of work, its two-project layout and its six interface languages are replaced by the [delivery plan](../delivery-plan.md) (decisions D-029 to D-034): foundations and reusable libraries come first. The status of each condition is in [acceptance](../acceptance.md).

## 1. Outcome and scope

Build **M0: a runnable local public discovery slice** inside the actual `Vafadar.Apps` checkout. A visitor should open the provisional Rostam and Sohrab collection, understand a concise introduction, follow a meaningful connection, change among visual views, open real local image/video content and inspect optional evidence while preserving context.

This is the immediate implementation objective. It is smaller than **R0: the public-release discovery pilot** in [document 16](16-discovery-pilot-prd.md). R0 adds the remaining publication, editorial, operations and actual deployment/device evidence. The broader product still includes family trees, social consent, invitations, institutional discovery and private galleries; those capabilities are not required to make M0 usable.

Use [document 18](18-discovery-flows-and-design-brief.md) as the behavior contract, [document 17](17-flagship-content-and-source-plan.md) as the sample/evidence contract and [document 19](19-repository-reconnaissance-and-spike-plan.md) as a dated starting point for repository integration. The user has requested implementation to start. Make routine reversible choices, record them and continue into working code; do not end after producing another plan or a set of empty projects.

## 2. Repository preflight and document import

1. Read the applicable current `AGENTS.md`, nested instructions and architecture/build/testing guidance. Inspect `git status`, branch, remotes and changed files before editing. Preserve existing uncommitted work and the owner's intended branch. Use isolation when needed rather than overwriting someone else's changes.
2. Confirm the active SDK, central package versions, solution filters, current CI conventions and available runtime/browser tools. The reviewed snapshot uses .NET 10 and Microsoft.Testing.Platform, but the live checkout is authoritative.
3. Import the reviewed product specifications into an app-owned documentation location such as `src/Apps/Dastan/docs/product/`, retaining stable requirement/decision IDs and source links. Reconcile an existing Dastan folder rather than replacing it. Keep this packet's execution prompt outside tracked source by default, consistent with the repository's attribution/configuration rules.
4. Keep original sample IDs or an explicit tested import mapping. Store editorial seed data outside `wwwroot` and other public static directories. Keep synthetic private fixtures in test-only paths; do not ship the whole ZIP or reviewer reader as product content.
5. Write a short actual execution plan/checkpoint in the app docs: starting commit, selected approach, scope, next concrete step and any real environmental blocker. Record the provisional flagship and remaining release gates explicitly.

Follow current repository rules for owner identity, hooks, verified commits and pushes, while respecting the scope of the user's authorization. A coding task does not itself authorize a public deployment, domain purchase or messages/invitations to other people. An unavailable device or hosting credential should leave its own gate pending while local implementation continues.

## 3. Recommended minimum architecture

The recommendation fits the existing web-only guidance and is deliberately small. Adopt it with an ADR after the bounded checks; depart from it only for an observed requirement or repository constraint, with the reason recorded.

| Part | M0 default | Why / boundary |
|---|---|---|
| Host and UI | ASP.NET Core Blazor Web App; prerender essential public text, use Interactive Server for interactive components as needed | Matches the repository's web direction and reuses C# skills. Check reconnect and touch behavior. This online-first mode does not promise offline interactive reading. |
| Domain | `Vafadar.Dastan.Core`, plain .NET | Typed entities, relationships, event associations, temporal expressions, source references, media associations and projection rules; no HTTP, EF or UI dependencies. |
| Web project | `Vafadar.Dastan.Web` | Composition, routes, web-localization adapter, sample catalogue adapter, filtered view models and presentation. Split infrastructure later when it has an actual independent responsibility. |
| Content store | Validated, immutable local JSON catalogue behind a small read-only query interface | Enough for the sourced sample; no database setup, migration or account needed merely to read it. Preserve provenance and publication state. |
| Graph | One browser renderer through a narrow adapter, initially evaluate Cytoscape.js; equivalent semantic relationship list | Pan/zoom and selection are browser interactions, not a reason to duplicate the domain or filter inaccessible data only in JavaScript. |
| Story/time views | One shared projected event sequence rendered as a vertical route and parallel participant lanes | Event order and participation remain consistent; no separate fake dataset per chart. Begin with simple accessible HTML/SVG where sufficient. |
| Media | Locally bundled original still/video, native image/video elements, poster, captions and transcript | Provides a real gallery flow without third-party availability or credentials. External references remain clearly identified. |
| Search | Bounded in-memory search over allowed names/aliases in the available collection | Return explainable matches; preserve homonyms/world context. No dedicated search service for this sample. |
| Tests | Matching Core/Web test projects using repository conventions; a small browser journey suite | Test actual projection/state behavior and running routes, not copies of the implementation. |

Microsoft distinguishes static and interactive render modes, and interactive components can be prerendered. The exact mode must be named in the ADR and tested under its real connection lifecycle. [Official render-mode documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0). Cytoscape.js documents a graph renderer with browser interaction and an MIT core license; record the exact selected version and notice during implementation. No claim of Dastan suitability or benchmark performance has yet been established. [Official Cytoscape.js documentation](https://js.cytoscape.org/).

Investigate at most one alternate frontend/renderer if a concrete M0 criterion fails. A TypeScript interface with an ASP.NET Core boundary remains an option, but no parallel production UI stacks should be built merely to compare possibilities. Avoid microservices, a graph database, external search, queues, distributed caching, native MAUI clients or a new generic plugin platform without a demonstrated need.

Suggested locations follow existing naming:

- `src/Apps/Dastan/Vafadar.Dastan.Core/`
- `src/Apps/Dastan/Vafadar.Dastan.Web/`
- `test/Apps/Dastan/Vafadar.Dastan.Core.Tests/`
- `test/Apps/Dastan/Vafadar.Dastan.Web.Tests/`
- `src/Apps/Dastan/docs/`, `README.md` and `CHANGELOG.md`
- A Dastan solution filter and narrowly scoped build/review output under the repository's approved `artifacts` directory.

Add new projects to the existing solution and the applicable non-MAUI test filter. A separate API deployment, Data project or shared `Vafadar.Web` library can wait until actual dependencies justify it. Do not copy Zanance's financial model, native bootstrap, account stores or private deployment into Dastan.

## 4. Resolve the concrete risks, then implement

### Step A — Minimal model and temporal contract

Read the sample format and run its supplied validator before adaptation. Define the smallest types and projections needed by the flagship, preserving source records, semantic relationship scope, event-association role/presence, edition and spoiler level. The sample JSON is an editorial interchange fixture, not a complete production database schema.

Implement representative temporal values from document 04: an exact day, year-only precision, an uncertain point, a period with separate uncertain endpoint bounds, an unpositioned duration quantity, an open-ended interval, a BCE/CE boundary, an anchored deep-time range and narrative order without a calendar mapping. Round-trip each without inventing missing precision. Use explicit numeric limits and units; do not store millions of years or narrative indices in `DateTime`, `DateOnly` or JavaScript `Date`. Convert compatible values to display coordinates only after semantic validation. Unknown, approximate, ongoing and incompatible are different results.

Keep ordinary operational timestamps and injected current time separate. Document the scoped historical-domain exception to the existing financial-date convention rather than changing shared financial behavior. Synthetic company/team/family fixtures check model reuse; their presence does not authorize exposing them in public discovery or claiming that private permissions are implemented.

**Exit:** The sample is valid, the core can produce a small allowed view, and meaningful temporal tests pass or identify a precise defect to fix. Continue to the host and screens immediately.

### Step B — Host, culture isolation and first visual route

Create the Core/Web boundary and a real local start command. Render the collection and entity overview with useful initial HTML, then connect one real focused graph and its relationship-list alternative. Reuse graph nodes, links and context from the same projection.

Do not register the existing mutable `AddVafadarLocalization` singleton stack directly in the server. Use request/circuit-appropriate state and explicit culture; one visitor's language, reading position, selected entity or filters must not change another visitor's state. Browser preference persistence is optional and contains only non-sensitive UI preferences. Test separate browser contexts, not only two sequential settings in one shared session.

The repository scans UI `.resx` resources and requires en/fa/de/es/fr/it key/placeholder parity. Keep the initial controls small and translate them properly. Use `lang`/`dir`, readable mixed-script names and logical CSS properties. The supplied story summaries may remain English with a visible content-language/fallback policy. Do not imply complete translated editions from localized navigation.

**Exit:** A visitor can enter, understand a subject, follow a labeled relation and return at a phone width. Language switching is isolated and the actual renderer has been inspected.

### Step C — Complete the small discovery journey

Implement all eight screen contracts using shared routes/components; they need not be eight separate full-page layouts. A source panel or media viewer can be an accessible contextual destination.

- Curated collection entry and bounded search.
- Concise entity overview with a few meaningful next connections.
- Focused relation graph, relation list and family projection where applicable.
- Vertical narrative sequence and parallel character lanes based on the same events. A mentioned person is not necessarily physically present; places and objects are not people. Do not draw continuous lifespan/presence bars from first and last mentions.
- Gallery entry from collection, entity and event, with an explicit inclusion rule and time basis.
- Actual local still and video viewing, credit, caption/transcript and relevant entity links.
- Optional evidence for the selected fact, relation, event or media association, with external-source behavior clearly identified.
- Reading-boundary controls and deep links/back navigation that preserve compatible context and reapply current visibility.

The original family illustration and parentage video have **spoiler level 2**. At default level 0, do not use their revealing poster, caption or relations as a hero image. Offer an honest permitted empty gallery or neutral entry treatment. After the visitor explicitly advances to level 2, both should be genuinely viewable. A URL to an external museum page is not an implemented image-viewer test. No third-party download is necessary to complete the bundled-media route.

Use curated concise copy and purposeful visual hierarchy. A text wordmark and original provisional install icon are sufficient to start. Final logo design must not block the runnable slice. Do not present empty categories, fake account controls or unsupported actions as working features.

### Step D — PWA shell, failures and public-page structure

Supply a manifest with an appropriate stable app identity, owned icon sizes, scope, start URL and display mode. Test localhost/HTTPS behavior in the available supporting browser. Installation criteria and UI vary by platform; a service worker and full offline reading are separate decisions. [MDN installability guide](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable).

M0 remains online-first. Implement an intelligible disconnected/reconnect state and safe update/reload behavior. A service worker is optional; if added, use an explicit allowlist of safe static shell assets and a neutral offline page. Do not precache the raw editorial dataset, unrevealed story previews, private fixtures, arbitrary API responses or all media. Do not convert an API failure into a false empty-result claim.

Use stable routes with real headings, concise titles/descriptions, safe default previews and usable initial text. Unknown, malformed or unavailable routes should produce honest errors/status behavior. Keep the local development catalogue unindexed and local-only; publication preparation for R0 later enables reviewed public pages, canonical URLs and sitemap entries. `noindex` is not an access-control boundary.

Do not host raw arbitrary uploaded HTML/SVG or accept remote media URLs without an intake design. M0 can serve its own reviewed static assets and known source links, with normal output encoding and a bounded content/link allowlist. Uploading, transcoding and mail delivery remain later capabilities.

### Step E — Verify, document and deliver the runnable result

Run the targeted Core/Web build and tests under the repository's CI warning policy, then exercise the actual browser routes. Record exact commands, runtime/browser/OS, selected test counts, outcomes and limitations. Update the app README with reproducible restore/build/run/test instructions and current completion status.

Check the existing shared tests or platform gates required by applicable repository instructions when solution/resource/build changes affect them. Distinguish a Dastan pass from a whole-repository pass. Diagnose unrelated failures without rewriting their assertions or claiming they passed. Keep unavailable physical devices as explicit R0 gates while finishing the available local work.

## 5. M0 acceptance matrix

These are future acceptance conditions, not outcomes of this documentation review.

| ID | Observable condition | Evidence required |
|---|---|---|
| **M0-01** | Fresh checkout setup can start the app locally from documented commands with the validated sample | Exact restore/build/run command, SDK and successful local route; no hidden credentials or production database dependency |
| **M0-02** | Discover → collection → entity → labeled relation → another entity → back works | Actual browser journey and visible correct identity/context, not just HTTP 200 responses |
| **M0-03** | Focused graph and list expose the same allowed relationships; keyboard selection and mobile expansion work | Renderer inspection plus projection assertions; unsupported type/direction is not silently drawn |
| **M0-04** | Story and participant lanes agree on narrative order and association meaning | Tests and browser review for mentioned vs present roles, with no invented calendar date or continuous presence |
| **M0-05** | At explicit level 2, a real bundled image and video open with useful labels/captions and context links | Actual image rendering, video play/pause, caption/transcript route and gallery-to-entity/back check |
| **M0-06** | Default level 0 and lowering the boundary remove future details from current/back/deep-linked views | Projection and browser checks over summaries, edges, event labels, media/posters, source snippets, suggestions and counts |
| **M0-07** | Selected evidence supports the specific visible statement and closes back to the origin | Fact/source locator match and return/focus state; external full-story source identified before leaving |
| **M0-08** | Time examples round-trip with uncertainty, duration, unit, epoch, era/calendar and frame preserved | Focused domain tests; incompatible comparisons return an explicit result rather than fabricated coordinates |
| **M0-09** | Independent visitors retain independent language and reading state | Two browser contexts with different cultures and boundaries; no process-global mutable culture/settings reuse |
| **M0-10** | Core routes work at 360px, 412px and a wide viewport; keyboard/focus, light/dark and Persian RTL are usable | Running browser checks; en/fa/de visual matrix and required six-language resource parity; image alternative text and video captions |
| **M0-11** | Manifest, scope, deep-link startup and online-first failure/update behavior work in the available environment | Browser/OS and actual observed installation or exact reason it could not be tested; distinguish manifest validity from installation evidence |
| **M0-12** | Only the explicit demo projection is served; private fixtures and raw review corpus are not web assets | Published-output/static-route inspection and negative requests; sample-publication mode cannot silently become a production content approval |

A complete M0 report must state any unmet item. A blocked physical installation check can coexist with a usable local implementation; report it as pending, not as a passed PWA or complete public release. Visual review and sample validation are not security assurance for a later private system.

## 6. Publication and data boundaries

M0 uses the reviewed dataset as **explicit local demonstration content** under its publication contract. Do not blanket-change source status to published because a sample validator succeeds. Rights metadata, editorial review, edition context and availability must remain intact. The import adapter should reject malformed/unsupported records and report a useful diagnostic; silently skipping a parent/source can corrupt the graph.

Project a view before rendering or serializing it. Apply publication/demo eligibility, current viewer access where relevant, reading boundary and association-level restrictions before counts, search suggestions, graph layout, gallery ordering and previews are computed. A public-story spoiler preference is a presentation constraint; it is not a substitute for server-side protection of real personal data. The private fixture remains a test/model exercise.

Persist only non-sensitive UI preferences if useful. A saved view, query string or browser history item carries selection, not authorization. Stable external source links may leave Dastan and reveal more than the selected story position; label that behavior. Use safe default previews for shared URLs without exposing later reveals in slugs or page metadata.

## 7. What comes after M0

R0 requires the remaining gates in document 16, including reviewed publication status, actually acquired/permitted media and accurate captions, content correction/revision behavior, release-quality language/accessibility coverage, the chosen phone/Windows browser matrix and a concrete hosting/operations plan. Domain and logo decisions can proceed on the runnable experience instead of delaying it.

Introduce relational persistence and minimal editorial tooling when public content can be reviewed, corrected and withdrawn. Decide providers from actual needs and operating cost. Later personal capabilities require a separately exercised authorization/consent design: account association, mutually accepted social requests, invitations, profile discoverability, time-bound institution queries, tag approval, media audiences and revocation across derivatives/caches. None of these should be simulated as real security by a client-only switch.

## 8. Completion report and continuation checkpoint

Deliver the runnable local app, a short summary of implemented behavior, relevant screenshots or browser evidence where permitted, exact verification commands/results, actual changed files/commit status, known limitations and the next small implementation step. Preserve a concise English checkpoint so another developer can continue without reconstructing the conversation.

Separate four states explicitly: **implemented and checked**, **implemented but unverified**, **planned**, and **blocked with a concrete cause**. Do not describe this entire product as complete after M0, and do not stop an authorized implementation merely because later release gates remain open.
