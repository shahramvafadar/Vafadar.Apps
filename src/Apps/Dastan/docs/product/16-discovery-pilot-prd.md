# 16 — Dastan Discovery Pilot PRD

**Version:** 0.6  
**Updated:** 2026-11-15  
**Product:** Dastan  
**Release label:** Discovery Pilot (R0), a proposed delivery slice  
**Status:** Reviewable product specification and implementation handoff. The product name and instruction to prepare a coding start are accepted; the bounded release choices below remain documented working proposals. No software release or completed usability study is claimed.

## 1. Purpose and decision

Give an ordinary curious visitor a rewarding first experience of Dastan: notice an appealing subject, understand the essentials, follow a meaningful connection, and move into a story or related media without losing orientation.

The working flagship is **Rostam and Sohrab**, a bounded Shahnameh arc with connected characters, family links, narrative events, places and artwork. It is selected provisionally for this design exercise under the user's instruction to proceed. It does not narrow the long-term product to Persian literature or determine the final launch collection.

The name **Dastan** is the user's choice. Its brand explanation and exact-name overlap findings are recorded in [document 11](11-brand-and-positioning.md). The domain, localized wordmark, final logo and public store title are separate unfinished decisions; they do not block this concept work.

## 2. Target visitor and intended value

The primary visitor is a general reader with an interest in an image, character or story. Prior knowledge of Shahnameh is optional. They should not need to register, understand graph terminology, configure a query or read an academic introduction before finding something useful.

The main user job is: **“Help me understand what I am looking at and show me an interesting connection I can follow.”** Optional evidence detail supports trust. Editors and specialist readers are secondary participants, with separate tasks.

A useful outcome may be a short visit: recognizing a relationship, following a scene to its participants, or understanding that an illustration was made much later than the story it depicts. Time spent and click count alone are not success measures.

## 3. Completed inputs and the next executable milestone

| Artifact | Purpose | Completion means |
|---|---|---|
| This release PRD | Bound the proposed first usable slice | Scope, acceptance conditions and dependencies are explicit |
| Source-backed flagship sample | Exercise actual story meanings | Records cite sources and distinguish narrative sequence, evidence and media metadata |
| Cross-domain fixtures | Check common-core reuse | Company, sport and private-family examples preserve their different semantics |
| Screen and flow blueprint | Make the experience concrete before interface implementation | Main content, actions, context and failure states are defined |
| Technical reconnaissance and spike plan | Ground later implementation choices | Observed repository facts and unanswered technical questions are separated |

These are design and implementation inputs completed for the handoff. An included motion-diagram media sample demonstrates a content type; it does not demonstrate a video hosting service, product player or functioning PWA.

**M0** is the next runnable local discovery slice, specified in [document 20](20-implementation-plan.md). It must let a reviewer complete a connected collection/entity/relationship/story/gallery/evidence journey with actual sample data and media. It includes a focused graph and list, one shared narrative model rendered as a story sequence and role-aware parallel lanes, meaningful return state, and spoiler behavior. A static landing page, documentation reader or collection of disconnected screen mockups does not complete M0.

**R0** is the subsequent public-pilot readiness boundary. It includes the M0 journey plus the publication, correction, media, usability and supported-device evidence below. This distinction permits implementation to start now without treating every future workflow as an initial prerequisite.

## 4. R0 public scope

All rows are proposed release commitments, not additional user-approved requirements. The underlying accepted requirements remain in [document 02](02-requirements-and-use-cases.md).

| ID | Capability | Bounded R0 behavior | Traceability |
|---|---|---|---|
| PILOT-01 | Curated discovery entry and simple search | Open the flagship from a short collection introduction or search for names inside the available collection | R-EXP-01, R-EXP-06, R-PUB-04 |
| PILOT-02 | Concise entity overview | Show a short introduction, type/world context, a suitable visual and a small set of labelled next connections; expose optional detail | R-EXP-02, R-EXP-04, R-DOM-02 |
| PILOT-03 | Focused connections and family view | Expand only relevant permitted neighbours; explain each relation; provide a list alternative and a focused family projection | R-EXP-03, R-PUB-08, R-DOM-04 |
| PILOT-04 | Story order and parallel participation | Follow narrative steps; compare selected characters' participation against the same story sequence | R-STORY-01, R-STORY-03, R-STORY-05, R-PUB-05 |
| PILOT-05 | Contextual image and video gallery | Show reviewed media associated with the collection, character or event, using explicit creation/depiction context | R-MED-01, R-MED-02, R-MED-04 |
| PILOT-06 | Media viewing and return | Open a permitted image or video, inspect a contextual link and return to the prior gallery/selection | R-MED-05, R-MED-06, R-MED-08, R-EXP-05 |
| PILOT-07 | Spoiler-aware exploration | Apply the chosen reading boundary to summaries, relations, event labels, media, suggestions and public-link previews | R-STORY-04 |
| PILOT-08 | Sources, correction and stable public links | Reach supporting evidence, identify scope limits, link to an entity or view, and report a content problem | R-GOV-01, R-GOV-03, R-PUB-07, R-PUB-09 |
| PILOT-09 | Reviewed publication | Keep authoring/import separate from publication, record revisions and propagate corrections to dependent views | R-GOV-01, R-GOV-02, R-DOM-08 |
| PILOT-10 | Responsive, accessible and installable delivery | Core journeys work on the chosen phone/Windows matrix, including list/keyboard routes; verify PWA installation separately from offline features | R-PLAT-01, R-PLAT-02, R-PLAT-04, R-PLAT-05, R-PLAT-06 |

The three principal visual families are a focused relationship map, a narrative timeline with optional parallel participation lanes, and a connected story path. Galleries are a primary entry and continuation surface. A family tree is a specialized projection of the relationship information, not another independent data model.

Both images and videos are in M0 and the proposed usable public slice. Initial video can be a short original explanatory diagram with captions and a transcript. M0 requires the actually playable local video and the bundled original family-connection illustration with context. Both are level-2 media in the sample: demonstrate them after explicit reading-boundary advancement, and demonstrate a non-revealing empty state at the default boundary. They are development content samples, not evidence of completed public editorial approval. A public release additionally requires an appropriate delivery policy. A storyboard, source URL or unavailable placeholder alone does not satisfy viewing acceptance.

## 5. Boundaries and later capability

The public pilot does not launch personal account claiming, friend requests, email invitations, private family publication, tagged private photos or university-cohort account discovery. These remain accepted product directions and appear in the model fixtures before implementation. No real invitations or notifications are sent in this stage.

Company and football examples are synthetic model checks, not additional public collections promised for R0. A historical comparison and deep-time examples continue to exercise temporal semantics, but the flagship does not invent historical dates to demonstrate them.

Other later capabilities include a complete atlas, live sport, unrestricted public editing, bulk unreviewed imports, an algorithmic social feed, complex researcher tools, automatic factual publishing and extensive private offline media. Scope reduction must not remove the underlying identity, time, provenance or access contracts.

## 6. Flagship content contract

The source manifest in [document 17](17-flagship-content-and-source-plan.md) and [the sample dataset](../../content/samples/shahnameh-pilot.json) is authoritative for the current inventory. Counts describe this sample, not completeness of Shahnameh or promised catalogue coverage.

Use connected characters, a nonhuman entity such as Rakhsh, narrative places, a work/collection context, typed relationships and a short event sequence. The collection should reward several meaningful steps without filling it with unsupported links. A location in a narrative is not automatically a verified modern boundary or coordinate.

Every publishable factual assertion needs a source and a context. Use the poem for what the poem says; a museum object page can support artwork metadata without overriding a conflicting textual detail. Short summaries are original editorial paraphrases, not copied modern translations. Source spelling variants do not create duplicate characters.

The original world and the digitized text witness remain identifiable. Edition certainty must not be overstated. A modern scholarly explanation, a museum synopsis and a source passage have different evidential roles. A disputed or incomplete item may remain a reviewer note instead of entering the casual default view.

## 7. Time and spoiler contract

The flagship uses **narrative sequence**, labelled as such. Step spacing does not represent years or equal elapsed time. An entity may act in an event, be mentioned, provide the setting or occur as an object. Lane markers retain the recorded role/presence; an association does not prove physical attendance. A character's appearance in a step does not establish lifespan or continuous presence between steps. Parallel lanes compare these qualified associations in the same source sequence; they do not date the story on a historical calendar.

Birth, parentage or identity may become known to a reader before a character learns them. The spoiler boundary follows what the selected account has revealed to the reader, not a universal assumption that every family link is a final twist. The dataset defines the actual levels and safe introductory copy.

A conservative introductory boundary is the default. Increasing it requires a clear action. Direct links into later content must respect the recipient's boundary rather than silently exposing a revealing title or thumbnail. External source pages may contain the full story; warn before opening them when that matters. Source URLs and documentation datasets are reviewer material and are not a spoiler-filtered product API.

Use `SC-03`, `SC-04`, `SC-05` through `SC-08`, and `SC-18` from [document 14](14-reference-scenarios.md) as relevant semantic checks. No sample chronology should be inferred from page or chapter numbers alone beyond the stated ordering purpose.

## 8. Shared exploration context

The proposed view-state contract includes collection, focused entity, world, text witness or edition, selected story position, spoiler boundary, view mode, relevant media/time basis, filters and return location. It is a presentation contract, not a fixed API or database schema.

A view switch preserves compatible context. A gallery entered from a character remains associated with that character until the visitor changes it. A source drawer closes back to the same focus. A story-to-calendar switch explains why a calendar is unavailable rather than inventing dates. New focus is added to a reversible exploration trail.

Proposed public route families are collection, entity, story, connections, gallery and media. Stable internal identifiers underpin readable slugs. Route design must not reveal private identifiers or act as an access token. Final routes, localization prefixes and URL serialization are implementation decisions to test with the actual solution.

## 9. Screen contract

[Document 18](18-discovery-flows-and-design-brief.md) owns the screen inventory and six principal flows. The first design slice should include a discovery entry, entity overview, focused relations, story view, gallery, media viewer and optional source/detail surface.

The first screen should show one clear focus and primary action. Avoid making every feature a navigation item or filling every overview with every relation. Keep short explanations visible where omitting them would mislead. Wider networks and detailed evidence open progressively.

The phone design must make return paths, selected context, reading position and view changes usable without precision dragging or hover. A graph should not be the only route to a parent, event or image. Body copy and controls need readable sizing; reduced motion and predictable video controls are part of the design contract.

The working interface direction includes Persian/RTL and an English path; all specifications remain English. Implementation must also follow the live repository's applicable localization instructions, including required UI resource languages; source content language and interface language stay distinguishable. RTL layout must preserve explicit chronology and relationship direction. Final launch languages and translation coverage remain open. The content sample's English labels are editorial working data, not a decision to launch an English-only product.

## 10. Editorial and media operations

For R0, a small initial editorial group owns the flagship, reviews claims and contextual media links, checks rights and publishes a version. For M0, a reviewed local seed and a documented selection/revision procedure are enough; building an administration UI or general importer is not a prerequisite. A correction records its reason and updates affected descriptions, connections, story steps and gallery placements. Importing a record does not publish it.

Every media item identifies creator/source, reuse basis, representation type, relevant creation and depicted context, accessibility information and spoiler level. A URL may be stored as a research reference while republication remains pending. Provider summaries should not be treated as a substitute for verifying the underlying narrative claim.

A present-day explanatory video must identify itself as an original diagram. It is not an archival recording or evidence of a historical event. Its captions, transcript and poster use the same access and spoiler policy as playback. The first release needs a defined error state if playback or a remote source fails.

Public data and private fixtures must not share a default publishing path. The sample files are documentation/reviewer inputs; no client-side filter over a downloadable private dataset can establish privacy. A later private-media implementation requires server-enforced access and an explicit revocation/offline policy.

## 11. Proposed acceptance gates

| Gate | Required evidence before the relevant release | Failure implication |
|---|---|---|
| G-01 — Content | Published statements have valid sources/context; media reuse and attribution are reviewed | Keep incomplete records in draft |
| G-02 — Meaning | No fabricated calendar dates, lifespan bars or unsupported causal links; model scenarios preserve identity and time | Correct the model or presentation before release |
| G-03 — Connected journey | Main flows reach an overview, labelled connection, story/media and useful return state | The public slice is incomplete |
| G-04 — Spoilers | A boundary applies to every visible and derived route, including media metadata and public previews | Fix exposure before enabling spoiler-sensitive content |
| G-05 — Media | Image and video paths work with credits, captions/transcript and contextual links; unavailable states are usable | Do not call the media capability complete |
| G-06 — Access | Synthetic private cases do not leak through counts, suggestions, posters, metadata or direct URLs | Do not enable the affected private capability |
| G-07 — Everyday usability | Observe first-time general visitors performing the defined tasks on phone and desktop | Revise confusing content and navigation |
| G-08 — Delivery | Record actual target devices/browsers, installation, update and failure behavior | Limit support claims to verified configurations |

For the first formative usability round, propose five to eight participants with varied familiarity. A working directional threshold is at least four of five observed visitors understanding the initial subject and following a labelled connection without coaching, with no critical time/world misunderstanding. This is a small qualitative check, not a statistical market claim. Record findings and revise the target if the task or sample changes.

No user study, security test, device installation, performance benchmark or completed gate is claimed in this documentation stage. The prospective tasks in document 18 must not be reported as passed until they are actually performed.

## 12. Delivery sequence and decisions

1. Inspect the current repository and reconcile the sample and fixtures with the domain/time rules.
2. Record only the technical decisions needed to start M0; use the bounded spikes in [document 19](19-repository-reconnaissance-and-spike-plan.md) when they resolve a concrete uncertainty.
3. Build a runnable application skeleton in the actual solution and connect the source-backed sample through the shared read model.
4. Implement the complete M0 journey from the screen contracts, including mobile/RTL cases and actual image/video viewing.
5. Execute the finite M0 checks, record failures or untested environments accurately, and use the running slice for interface review.
6. Complete the additional publication, correction, content, usability and supported-device gates before calling R0 ready for a public pilot.

The implementing developer should follow [document 20](20-implementation-plan.md): inspect the current repository, record the minimum reversible technical decisions, build M0 and report actual evidence and remaining gaps. The running slice is also the interface prototype for review. Necessary design and technical checks may overlap; do not stop at another plan or require a final logo, universal model or production infrastructure before useful local implementation. R0 gates still determine when a public pilot can be described as ready.
