# 09 — Architecture Direction and Technology Decisions

**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Reasoned implementation defaults and bounded decision work; no implementation or final production architecture claimed

> **In the repository (2026-11-15):** the proposals of this document were the starting point. The accepted projects, the localization approach and the technical defaults are in the [delivery plan](../delivery-plan.md) (decisions D-029 to D-034); each technical choice becomes an ADR when it is made and verified.

## Current project knowledge

The accepted delivery direction is a new **Dastan** website in the `Vafadar.Apps` solution, installable as a PWA. The v0.6 read-only review refreshed `main` at commit `957a33c05595746464ca80fb91153bb3bdf8dc14`, examined the complete returned path inventory and inspected selected instructions, build/CI files, architecture guidance and library implementations. The common framework is net10.0 and `global.json` specifies SDK 10.0.404. No Dastan path occurs in that inventory. [Document 19](19-repository-reconnaissance-and-spike-plan.md) records the precise scope, findings and primary source links. No repository implementation, branch or deployment was changed in this review.

The repository's existing architecture guidance identifies a Blazor Web App for web-only products. That makes **ASP.NET Core with a Blazor Web App the starting recommendation for M0**, subject to the bounded interaction checks below, rather than an open-ended comparison of unrelated frameworks. A PWA is the requested mobile/Windows delivery; native MAUI clients are not prerequisites.

All project documentation is English. The current repository-wide resource checks require UI resource keys and placeholders in **English, Persian, German, Spanish, French and Italian**. Honor those checks for Dastan's small shell and controls. Editorial content has its own language and can initially remain the supplied English summaries with explicit language/fallback labels; six UI languages do not imply six translated story editions. Dastan is the selected name. Production storage, hosting, identity providers and a final visual identity remain open.

Re-read applicable instructions and the live checkout before implementation. This packet does not override a newer repository decision, supply credentials or transfer authorization from unrelated Zanance device/financial work.

## Audience-driven architecture goals

The primary launch audience is the general curious public. A successful first view should present a concise essential overview, an immediately useful visual, and understandable next connections. Deeper descriptions and sources remain available through progressive disclosure. Researchers are a secondary possible audience, not the condition for a useful launch experience.

The architecture should support several meaningful visual defaults, contextual photos and videos, connected story threads, and navigation that retains the subject and selected context. It should not force every interaction through an advanced query builder or load an entire graph before showing value.

This experience still depends on rigorous time, identity, source, consent, and privacy semantics. A simple interface is a presentation of correct information, not permission to discard uncertainty or context.

## Proposed shared core and capability boundaries

The conceptual core includes identity, typed entities, statements and evidence, relationships and roles, temporal expressions, places, narrative contexts, media, publication state, access rules, and change provenance.

| Layer or responsibility | Proposed purpose | Boundary to preserve |
|---|---|---|
| Shared core | Common identity, time, statements, associations, media, and policy | A single coherent meaning for shared concepts |
| Typed capabilities | Family relationships, organizational change, team membership, narrative order, or media presentation | Specialized rules and forms without duplicating the core |
| Public views | Entity overviews, useful charts, story routes, contextual galleries, and public search | Published and authorized information only |
| Personal views | Family trees, life timelines, audience circles, saved views, and recent authorized shares | Current access and consent rules; no automatic publication |
| Contribution and governance | Drafts, sources, review, corrections, and disputes | Editing or importing does not equal publication |
| Delivery capabilities | Rendering, search, media processing, notifications, installation, and optional offline access | Technical delivery must preserve domain meaning and policy |

These are responsibility boundaries, not instructions to create separate services, databases, or one generic untyped property store. **The starting proposal remains a modular application in the existing solution.** Separate deployment is justified only by demonstrated workload, team, release, or operational needs.

A proposed information path is: contribute or import; review identity and structure; retain source and context; review and publish; build appropriate discoverable views. Private content has its own access-governed lifecycle and does not enter public publication without a separate authorized action.

## Decisions to investigate

The existing ADR identifiers are preserved. These recommendations guide the first implementation; record adoption, consequences and actual evidence in the repository. M0 does not require every production decision to be settled.

| ID | Topic | Current proposal | Evidence needed |
|---|---|---|---|
| ADR-P01 | Application structure | A modular application with shared core, typed capabilities, and public/personal views | Repository inventory, ownership boundaries, deployment constraints, and a representative cross-capability flow |
| ADR-P02 | Server side | Start with ASP.NET Core using the repository SDK/framework; one Dastan host | Actual build, startup, isolated request state and repository compatibility |
| ADR-P03 | User interface | Start with a Blazor Web App: prerendered essentials and Interactive Server where needed; keep graph gestures in a bounded browser renderer | Mobile interaction, connection-loss behavior, accessible routes and independent cultures; investigate one TypeScript alternative only for a demonstrated blocker |
| ADR-P04 | Primary storage | M0 uses an immutable validated local sample catalogue behind an application query interface; defer persistent authoring to R0 | The same projection rules must work when a relational adapter is introduced; no database service is needed merely to read this sample |
| ADR-P05 | Graph processing | Keep the conceptual graph independent of storage choice | Paths, temporal filtering, publication, permissions, and understandable bounded expansion |
| ADR-P06 | Temporal model | A domain model for real, relative, deep, narrative, and media time | Complete semantic examples, conversions, round trips, and gallery time-basis behavior |
| ADR-P07 | Search and discovery | Begin with a replaceable approach; add a separate engine only when justified | Name variants, relevant connections, contextual media, language quality, and removal of access |
| ADR-P08 | Visual rendering | Compare candidate graph and timeline tools against the same representative information | Useful defaults, progressive detail, touch, accessibility, labels, memory, and context retention |
| ADR-P09 | Offline behavior | Installation and clear online reading first; selected offline capability | Freshness, logout, expiry, revoked access, and media size/cost limits |
| ADR-P10 | Import | Controlled intake retaining source identity, revision, and context | A small correctable data-and-media sample, identity resolution, provenance, and review |

When a decision is made, record alternatives, reasons, consequences, validation evidence, and a condition for revisiting it. The table inherits the live repository toolchain and proposes an M0 frontend; it does not select a production database, media service or cloud provider. Routine reversible implementation choices may be made and recorded without waiting for another planning approval.

## Presentation and rendering

The public interface should offer meaningful visual entry points suited to the subject: a focused relationship view, a timeline, a family view where applicable, a comparison, or a contextual gallery. The exact default set is a design decision. A person, company, team, narrative, and historical period need not open with the same chart.

Short introductory content and usable navigation should remain available while heavier visuals load or if they fail. Text, lists, keyboard navigation, and source access remain important alternatives. This supports a chart-led experience without making a successful GPU rendering operation the only route to understanding.

Use the same context contract across views: focused entity or collection, selected time and time basis, world or edition, relevant filters, spoiler boundary where applicable, and a back-navigation state. A gallery transition must not silently switch a historical-period filter into capture-date filtering. View state selects data; it is not an authorization grant.

Microsoft's documentation distinguishes Blazor render modes and their execution behavior. A comparison must identify the actual mode being tested and its connection, loading, and interaction implications. [Microsoft Learn: Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes). This is a documentation reference, not a selected runtime version.

[Cytoscape.js](https://js.cytoscape.org/) documents graph visualization and analysis. [Sigma.js](https://www.sigmajs.org/) describes WebGL-based interactive graph visualization. They are candidates for evaluation; their suitability for this product's language, accessibility, mobile, and data requirements has not been tested. A rendering engine does not determine truth or access permission.

For timelines, test uncertainty, deep-time values, alternate calendars, narrative ordering, and actual duration before choosing a library. A ready-made engine, an adaptation layer, or a small specialized renderer remain possible. Graph and timeline rendering do not have to use the same library.

## Storage and the role of a graph

Complex relationships do not by themselves select Neo4j or another graph database. Entities, statements, roles, sources, and temporal expressions can be assessed against relational approaches that produce graph views. A specialized graph store becomes a candidate if representative path and network workloads demonstrate a useful advantage.

For M0, parse and validate the curated local catalogue at startup and expose read-only projections. Keep the full seed outside publicly served assets; publication and spoiler rules apply before a view receives data. Add persistence when editing/review workflows require it. PostgreSQL and SQL Server remain production candidates, evaluated against corrections, relationships/time bounds, permissions, search, backup and maintenance. Do not create a graph database, distributed cache or separate search service for the small pilot.

Search indexes, gallery query results, read-oriented graph representations, and caches should be rebuildable from authoritative records. They must not become independent contradictory sources of truth. Corrections, withdrawals, and access changes require an explicit propagation strategy across dependent views and media derivatives.

## Domain time and ordinary programming dates

Domain time includes BCE, millions of years, narrative order, and uncertain bounds. Technical timestamps record actions such as importing a file or sending a notification. They must not share one ambiguous contract.

Microsoft documents a `DateTime.MaxValue` at the end of the year 9999. JavaScript `Date` has a range of approximately one hundred million days in either direction from its epoch; that is insufficient for fifty million years ago. Sources: [Microsoft DateTime.MaxValue](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.maxvalue) and [MDN JavaScript Date](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date).

The proposed model retains original expression, unit, reference, precision, temporal nature, uncertainty bounds, and source. Calendar conversion and screen-coordinate generation are separate responsibilities. The first bounded spike must settle the minimum value types, serialization and comparison rules needed by representative cases, then implement them. Ordinary operational dates can continue to use `DateOnly` and injected `TimeProvider`; historical/deep/narrative values require their own domain types. The user's explicit deep-time requirement must not be narrowed to the existing financial app's date range. [EDTF](https://www.loc.gov/standards/datetime/) informs interchange of some temporal concepts; it does not define all narrative or relationship semantics.

Media capture, original work creation, depicted period, digitization, upload/import, publication, and sharing remain independently meaningful. The selected gallery time basis must be explicit in query and presentation contracts. Playback duration is not historical duration.

## Web-safe localization and shared-library reuse

A concrete incompatibility was found in the existing localization registration: it registers singleton localization/formatting state and `Translator.Instance`; changing language writes process-default cultures and the shared translator. Directly using that registration in a multi-user web host would make user-specific culture changes shared process state. This is a source-derived integration risk, not an observed production incident. [Registration](https://github.com/shahramvafadar/Vafadar.Apps/blob/957a33c05595746464ca80fb91153bb3bdf8dc14/src/Libraries/Vafadar.Localization/LocalizationServiceCollectionExtensions.cs), [implementation](https://github.com/shahramvafadar/Vafadar.Apps/blob/957a33c05595746464ca80fb91153bb3bdf8dc14/src/Libraries/Vafadar.Localization/LocalizationService.cs).

Use request/circuit-appropriate culture and view state, explicit culture when formatting, and browser-local preferences. Reuse verified stateless resources/helpers where useful; do not call `SetLanguage` on a shared singleton from a browser session or change the existing mobile implementation merely to bootstrap Dastan. Test two independent browser contexts with different languages and reading positions. Microsoft documents server-side request localization and resource-based strings: [Blazor globalization and localization](https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0).

The existing `DateFormatter` accepts `DateOnly` and is not a renderer for BCE, deep time or fictional order. Keep those capabilities in Dastan's domain and presentation adapters. A shared library becomes appropriate when its contract and consumers justify extraction; first implementation does not require a new generic `Vafadar.Web` platform.

## Public access, personal access, and discovery

Published public information should support fast ordinary browsing. Private data needs authorization before it enters every result path. Hiding a node in the browser is insufficient if the same information appears in an API response, count, recommendation, path, thumbnail, video poster, caption, transcript, or guessable media URL.

Keep information space, publication status, and personal-access policy distinct and apply them consistently to search and output. Public status of a university or company does not publish connected private histories. Discovery of personal accounts is opt-in; exploration of documented public roles follows content governance without revealing private account links.

Reuse existing identity capabilities where appropriate after inspecting the solution. Associating an account with a person record, accepting a social request, and editing a public profile are different operations. Email verification alone does not establish every asserted relationship.

Public caches and personalized results must be separated according to their effective access context. Saving a view or requesting an earlier content version does not restore revoked permissions. Privacy, publication, source rights, and narrative spoiler context can all affect a result, but should remain distinguishable policies.

## Media identity, processing, and galleries

**Accepted direction:** Images and videos are core contextual content. The same reusable media capabilities must serve public entity and historical-period galleries, personal or family memories, and recent authorized shares.

**Proposed architecture:** Separate media identity and metadata from contextual associations, share activities, file locations, renditions, and gallery definitions. A curated album stores intentional membership and order; a dynamic gallery applies a contextual selection. Neither requires copying the underlying asset into a separate family, history, or company media store merely because it appears there.

Media intake should validate the submitted content, retain provenance and rights, establish the intended audience, and produce appropriate delivery renditions where needed. Thumbnail generation, video posters, optional transcoding, captions, metadata handling, and processing status need explicit contracts. Queue technology, storage provider, supported formats, size limits, codecs, and retention policies remain open choices.

Renditions and derived text must inherit the relevant access restrictions or stricter ones. Public appearance of an allowed asset must not disclose private annotations or associations. Deduplication must preserve identity and rights distinctions and must not reveal that an inaccessible private asset already exists. A public file reference is not, by itself, permission to republish the file.

A media viewer should preserve entry context, show an appropriate caption and attribution, and offer authorized links to the relevant people, event, place, period, or narrative. The back action restores the prior gallery and filters. Missing or failed media processing should have a usable state rather than blocking the rest of the entity page.

A recent-shares view queries authorized share activities, not simply the newest file timestamp. One old photograph shared today can appear as a new share without changing its capture date. Removing access must affect media delivery and derivative routes, not only the gallery card. Detailed behavior is defined in [document 15](15-media-and-galleries.md).

## PWA installation and offline use

MDN describes a web app manifest and appropriate secure hosting as part of installability, and explains that installation behavior varies by browser and platform. Offline capability is a separate concern. An installable website does not imply offline access to all content. [MDN: Making PWAs installable](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable).

The proposed validation matrix records exact browser and operating-system versions for Windows, Android, and iOS targets. Tasks include installation, opening links, sign-in, returning from offline, updating the application, chart interaction, media playback, and the selected UI languages. In-app notifications provide the base social-request flow; push and email are separate channels with their own settings and device capabilities.

M0 is online-first. Include an install manifest, owned icons, correct scope/start URL, an understandable connection-loss state and update/reload behavior. A service worker is optional for that milestone; if used, cache only an explicit safe static shell/offline page. Selected public collections require a later content-version and withdrawal policy. Private offline storage waits for defined logout, expiry, revocation and media-limit rules. Do not promise deletion of files already downloaded outside the application's control.

## Import and operational evidence

Import from Wikidata or other sources enters a review process. Retain external identity, source revision, acquisition time, temporal qualifiers, and actual claim evidence. Imported items must not automatically merge with same-named records or become public. [Document 08](08-comparative-research.md) distinguishes structured data, text, and media reuse.

Select media storage, email delivery, search infrastructure, and other providers only after examining existing infrastructure, cost, privacy requirements, and representative use. There are no measured throughput, capacity, or monthly-cost guarantees in this package.

## Experiments before committing to tools

| Experiment | Representative input | Decision-making evidence |
|---|---|---|
| First glance and progressive detail | A person, company, team, and period with concise essential information | Ordinary users understand the subject and find an interesting next step without specialist terminology |
| Graph and visual presets | Multiple typed relationships, family structure, and a narrative | Readable defaults, controlled expansion, touch behavior, and accessible alternatives |
| Cross-view navigation | The same subject in graph, timeline, gallery, and story views | Focus, temporal meaning, filters, world, and return state survive transitions |
| Time | Partial year, BCE, uncertain boundaries, deep time, narrative order | Correct meaning, ordering, conversion, and round trips without fabricated dates |
| Comparison | Compatible tracks, a shared event, and uncertain periods | Honest scales and clear distinctions between simultaneity and stage comparison |
| Galleries and media | A recent share of an old photo, a modern depiction of an ancient period, and a video | Correct contextual selection, time basis, attribution, viewer links, and processing states |
| Authorization | Public content adjacent to private people, associations, and media | No disclosure through results, counts, posters, captions, playback, downloads, or derived content |
| Search and discovery | Name variants, homonyms, historical names, and connected subjects | Useful results, explainable connections, and no private-data inference |
| Import and correction | A small versioned sample with conflicting evidence and media rights | Reviewability, identity correction, provenance retention, and dependent-view updates |

Record sample size, device, network, and measurement criteria before execution. Add tests to resolve these concrete risks rather than to mirror the implementation mechanically. The new experience/media scenarios SC-15–SC-19 are specified in [document 14](14-reference-scenarios.md); this document does not redefine them.

## Bounded decisions followed by implementation

The next deliverable is **M0, a runnable local public discovery slice**, defined in [document 20](20-implementation-plan.md). Two immediate checks address temporal meaning and browser interaction/localization. Record a viable approach, then build the complete small journey in the same workstream. Do not wait for a final database, cloud provider, identity service, logo or every future capability before starting.

The broader experiments above remain useful as the corresponding capability enters scope. Private-gallery authorization must be verified before private data is introduced; it is not a reason to construct a private social service during M0. Public R0 publication gates remain in [document 16](16-discovery-pilot-prd.md).

## Concrete v0.6 handoff

Use the [Discovery Pilot PRD](16-discovery-pilot-prd.md), [source-backed sample](17-flagship-content-and-source-plan.md), [screen/flow blueprint](18-discovery-flows-and-design-brief.md), [repository evidence](19-repository-reconnaissance-and-spike-plan.md) and [implementation plan](20-implementation-plan.md). The sample validation and document review do not prove the application, PWA, security or device experience. Report those outcomes only after executing the corresponding checks.
