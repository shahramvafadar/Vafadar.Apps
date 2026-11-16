# 12 — Decisions, Open Questions and Change Log

**Version:** 0.6  
**Updated:** 2026-11-15

This register distinguishes accepted product direction, current proposals, unresolved choices and superseded framing. A fresh user instruction takes precedence over an earlier interpretation. Changes must be reflected here and in the relevant specification rather than silently changing terminology in one document.

## Status vocabulary

| Status | Meaning |
|---|---|
| Accepted | An explicit user requirement or a direction accepted in the conversation |
| Proposed | A recommended design or delivery approach that can still change |
| Open | A choice whose options and consequences have not yet been resolved |
| Superseded | An earlier proposal or interpretation replaced by a newer direction |
| Requires verification | Evidence or implementation inspection is still needed |

Acceptance of a product direction is not proof that an implementation exists or that every detailed behavior has been approved. A reference scenario describes expected behavior for future validation.

## Accepted product decisions

| ID | Decision | Origin and qualification | Main references |
|---|---|---|---|
| D-001 | Create a new website in Vafadar.Apps with installable PWA behavior | Original user request; device support must be verified during implementation | 01, 09 |
| D-002 | Most product information is public | Explicit user clarification; v0.4 clarifies general-user discovery as the primary experience | 01, 05 |
| D-003 | Support entities beyond people and countries, including companies, teams and other subjects | Explicit extension of scope | 03 |
| D-004 | Family trees, personal information and private spaces support user-controlled visibility | Original direction and subsequent clarification | 06 |
| D-005 | Reuse one conceptual core for entities, relationships, events, time and place across purposes | Accepted concept, reinforced in the latest request; detailed module boundaries remain proposed | 03, 04, 09 |
| D-006 | A person record is distinct from a user account | Accepted conceptual distinction | 03, 06 |
| D-007 | Mutual social relationships such as close friendship require two accounts, a request and acceptance | Explicit user correction; public documented roles use separate governance | 06 |
| D-008 | Invite a person without an account by email or a shareable invitation link | Explicit requirement | 06 |
| D-009 | Joining, claiming a person record and accepting a relationship are separate actions | Accepted distinction | 06 |
| D-010 | Discovery of a personal account through a university, company or similar node requires the person's permission | Explicit requirement; distinct from public historical or professional facts | 06 |
| D-011 | Support exact, partial, approximate, ranged and very distant time | Explicit user examples, including deep time | 04 |
| D-012 | Distinguish uncertainty about when an occurrence happened from an actual continuous duration | Accepted conceptual distinction | 04 |
| D-013 | Fictional worlds, works and editions provide the context of narrative information | Accepted concept; exact vocabulary remains proposed | 03, 07 |
| D-014 | Separate narrative presentation order from in-world event order | Accepted distinction | 04, 05 |
| D-015 | Support multiple timeline views, parallel comparisons and meaningful time/place filters | Explicit direction, illustrated by the user's comparison image | 05 |
| D-016 | Public knowledge retains sources, differing accounts and revision history | Accepted foundation; evidence depth can be optional in the interface | 07 |
| D-017 | Memories use their relevant original time, and person tags require approval | Original user direction | 06, 15 |
| D-018 | Tag approval does not automatically enlarge the media audience | Accepted privacy distinction | 06, 15 |
| D-019 | Develop the concept, name, identity, design and technology through documented decisions | Requested sequence; this does not require every future idea to be settled before bounded design work | 09, 10, 11 |
| D-020 | Maintain the conversation's requirements in several connected documents | Explicit documentation request | README, 13 |
| D-021 | General curious users are the primary initial audience | Latest explicit correction; researcher-first positioning is superseded | 01, 05, 10 |
| D-022 | Show concise essential information first and offer more detail when wanted | Latest explicit direction | 02, 05 |
| D-023 | Use several meaningful default charts or similar visual tools, with connected entities and story threads that invite exploration | Latest explicit direction; precise chart set and recommendation rules remain proposed | 05, 08 |
| D-024 | Support image and video galleries through multiple contexts, including historical topics and authorized recent friend/family shares | Latest explicit direction; exact ordering, player and release scope remain proposed | 15 |
| D-025 | Write all product documentation in English | Explicit instruction; it does not determine interface languages | README, 13 |
| D-026 | Use Dastan as the product name | Explicit user selection on 2026-11-15; brand rationale is corrected and sourced in document 11 | 01, 11 |
| D-027 | Proceed with the proposed bounded next step | Explicit user instruction on 2026-11-15; release definition, representative content and principal flow design | 16, 17, 18, 19 |
| D-028 | Re-review and improve the complete foundation, then provide a complete ZIP and an actionable implementation brief | Explicit user request on 2026-11-15; prepare a handoff that starts implementation, with honest milestone and verification boundaries | 20, 21 |
| D-029 | Build Dastan inside the Vafadar.Apps repository, reuse its shared libraries where they fit, and write parts that other apps can use as independent shared libraries | Explicit user instruction on 2026-11-15 after the repository was inspected; an existing library is extended only without breaking the apps that use it | [Delivery plan](../delivery-plan.md), 19 |
| D-030 | Avoiding rework and technical debt has priority over an early demonstrable result | Explicit user instruction on 2026-11-15. Changes the order of work in document 20 (foundations and reusable libraries before screens); the scope and the twelve acceptance conditions of M0 are unchanged | [Delivery plan](../delivery-plan.md), 20 |
| D-031 | The first interface languages are English and Persian; German and others follow later. Right-to-left support is general, so Kurdish and Arabic need translations only | Explicit user instruction on 2026-11-15; resolves Q-03 for interface languages. Calendars and translations of source content remain separate choices | 16, 18, [Delivery plan](../delivery-plan.md) |
| D-032 | Dastan's interface texts and its list of languages are its own; they are independent of the texts and languages of other apps in the repository | Explicit user instruction on 2026-11-15 | [Delivery plan](../delivery-plan.md) |
| D-033 | Accept the delivery plan of 2026-11-15 with its twelve decisions: the project set (`Vafadar.Temporal`, `Vafadar.Web`, `Vafadar.Dastan.Core`, `.Data`, `.Web`), ASP.NET Core localization with Dastan's own resources, languages per app, the language in the address, a per-visitor reading boundary that a link never raises, own components by default with Syncfusion where it adds value, a bounded comparison for the graph renderer, Persian sample text drafted for review, feature branches, app-level working rules, browser tests and local build output | Explicit user approval on 2026-11-15. Refines P-015; individual technical choices are recorded as ADRs when they are made and verified | [Delivery plan](../delivery-plan.md) |
| D-034 | Run the database and every other service that can run in a container in the Docker Desktop installation of the development machine; all containers of this project belong to one group (Compose project) named `test` | Explicit user instruction on 2026-11-15. M0 needs no database; the rule applies from the first container, for example the browser of the journey tests | [Delivery plan](../delivery-plan.md) |

## Current proposals

| ID | Proposal | Reason | Revisit when |
|---|---|---|---|
| P-001 | Start delivery with a bounded public discovery experience | Fits the predominantly public scope and general audience | Flagship, content supply and launch plan are chosen |
| P-002 | Build one substantial flagship collection and small cross-domain model examples | Gives visitors depth while checking reuse | Content cost or prototype findings suggest a different scope |
| P-003 | Allow anonymous public reading; initially require accounts and review for contributions | Reduces discovery friction while supporting content quality | Editorial capacity and participation policy are defined |
| P-004 | Use claims and evidence to explain public relationships | Preserves context, time and conflicting sources | Semantic examples or authoring complexity require refinement |
| P-005 | Evaluate a modular architecture within the existing solution | Limits early operational complexity while retaining clear responsibilities | Repository inspection and actual scale requirements provide evidence |
| P-006 | Resolve the few immediate technical risks, record a coherent working choice and continue into M0 implementation | Makes time, culture, projection and visual behavior concrete without an open-ended evaluation phase | A concrete acceptance failure or repository constraint justifies a change |
| P-007 | Plan Persian/RTL seriously from the first interface design and preserve multilingual names | Fits the project context; English documentation is a separate decision | Launch languages and calendars are selected |
| P-008 | Express Dastan as a broad, approachable discovery brand | Covers history, stories, organizations, teams and personal memories | Wordmark, subtitle and identity testing refine presentation |
| P-009 | Coordinate a focused relationship map, parallel timelines and story paths with galleries and accessible lists | Offers meaningful default views with manageable orientation | Prototype findings identify the best first-release set |
| P-010 | Preserve compatible exploration context when moving between visual and media views | Supports the accepted connected experience | Interaction design resolves incompatible time/world frames |
| P-011 | Reuse media assets, contextual placements and share records under common access rules | Supports multiple galleries without conflicting copies or permissions | Semantic and media delivery examples are exercised |
| P-012 | Use a bounded Rostam and Sohrab arc as the design flagship | Concrete narrative, family, place and media relationships exercise the shared core | Content review and prototype findings inform the final launch choice |
| P-013 | Use document 16 as the proposed R0 release boundary | Makes public-release readiness explicit without dropping later personal capabilities | Implementation and release evidence justify scope changes |
| P-014 | Implement M0 as the first runnable local discovery milestone | A real connected journey with the supplied data gives design and technical decisions observable consequences | The M0 acceptance matrix in document 20 is exercised |
| P-015 | Begin with the minimal web architecture recommended in document 20, validated against the current checkout | Fits the inspected solution while keeping content projection, domain and presentation distinct | A concrete current constraint requires a documented alternative |

## Superseded or refined ideas

| Earlier idea or framing | Current position | Reason |
|---|---|---|
| Family and memories as the principal initial launch | Public-first delivery is the current proposal; personal capability remains accepted | The user made public information the dominant scope |
| Researchers or research tasks as the primary audience | General curious users are primary; optional depth supports more demanding use | Explicit latest correction |
| An article or advanced search as the assumed entry to every journey | Topics, images, charts, story threads and public links can also be entry points | The user emphasized attractive discovery and galleries |
| Media planned mainly as a late personal-photo feature | Public images and videos participate in initial discovery planning | Latest gallery requirement |
| A visible mutual close-friend relationship set by one person | A shared social relationship requires the other account's acceptance | Explicit consent requirement |
| Identity centered only on people's stories | Brand and core cover many entity types and purposes | Companies, teams and other subjects are in scope |
| One start/end pair represents every time expression | Occurrence uncertainty, actual duration, uncertain endpoints and narrative time remain distinct | The examples require different meanings |
| Earlier name shortlists, including the v0.4 recommendations | Archived; Dastan is the selected name | Explicit user decision on 2026-11-15 |
| Persian documentation in the v0.3 pack | All maintained documentation is English in v0.4 | Explicit latest instruction |
| Start with a runnable slice immediately and settle structure along the way (documents 19 and 20) | Foundations and reusable libraries first, in the phases of the delivery plan; the M0 scope and acceptance conditions stay | D-030 |
| Six interface languages from the first slice, as the repository's resource checks required for every project | English and Persian first; each app declares its own languages | D-031, D-032 |
| Two projects, `Vafadar.Dastan.Core` and `Vafadar.Dastan.Web`, with the sample adapter inside the web project | Two shared libraries and three app projects, as listed in D-033 | D-029, D-033 |

## Open questions and working defaults

This is a decision register, not a mandatory questionnaire. Resolve a question when the next concrete task depends on it. Use clearly labelled provisional choices for reversible exploratory work.

| ID | Question | Current working default | Needed by |
|---|---|---|---|
| Q-01 | Which collection becomes the launch flagship? | Rostam and Sohrab is the current design/pilot choice under P-012; confirm release fit through content and prototype review | Before public-release commitment |
| Q-02 | Who may suggest, review and publish changes? | A small reviewed contributor group initially | Contribution-flow design |
| Q-03 | Which interface languages and calendars launch first? | Interface languages resolved: English and Persian first (D-031). Launch calendars and translations of source content remain open; Persian text of the sample is drafted for review (D-033) | Search, layout and date design |
| Q-04 | What time bounds and normalization conventions apply? | Preserve source wording and define explicit calculation contracts | Time implementation |
| Q-05 | How are public claims about living people and their objections handled? | Human review with a defined scope and rationale | Publishing this content category |
| Q-06 | How does an account establish a claim to a person record? | No automatic merging by name or email alone | Invitation and identity-claim implementation |
| Q-07 | How are shared trees and child/deceased records administered? | Defined stewardship, permissions and history | Family-capability release |
| Q-08 | What name and language-market presentation will be used? | Name resolved: Dastan (D-026). Persian display spelling, pronunciation guidance, market, domain and final identity remain open | Before final wordmark and public branding commitment |
| Q-09 | Where will the first collection's facts and media come from? | Use the sourced editorial sample and two original bundled media assets for explicit local demonstration; preserve the pending public-release review and external-image restrictions | Real content publication |
| Q-10 | What is the actual Vafadar.Apps integration contract? | Resolved by the inspection of the working copy on 2026-11-15 and D-029/D-033: `Vafadar.Core` is reused, `Vafadar.Localization` is not referenced, the other libraries are not relevant to M0. Details in the [delivery plan](../delivery-plan.md) | Application integration |
| Q-11 | What are the cost and revenue models? | Open; preserve the value of public discovery | Costly commitments and broad release |
| Q-12 | Who participates in the initial pilot, and at what scale? | A small group of ordinary curious visitors with varied topic familiarity | Prototype validation |
| Q-13 | What may be available offline, and for how long? | Start by evaluating selected public content; define private validity separately | Offline implementation, especially private media |
| Q-14 | What remains after a social relationship or consent ends? | Immediate withdrawal from shared visibility; narrowly defined retained history | Social-capability release |
| Q-15 | Who extends the entity and relationship vocabulary? | Managed initial vocabulary with a suggestion path | Broad contribution |
| Q-16 | How are external imports synchronized and corrected? | Versioned ingestion and review without silent overwrites | Recurring imports |
| Q-17 | How is the default account chosen when sources conflict? | Transparent editorial rules with accessible alternatives | Publishing disputed collections |
| Q-18 | Which visual presets are default for each context? | Focused connections, parallel time and connected story paths; gallery/list alternatives | First experience prototype |
| Q-19 | What qualifies media for each gallery, and how is it ordered? | Explicit contextual relation and time basis; recent personal shares use share time | Gallery design |
| Q-20 | Which image/video ingestion, hosting and playback workflows launch first? | Evaluate permitted sources, accessibility, cost and private-access needs before selecting a provider | Media implementation and release PRD |

## Version history

| Version | Stage | Change |
|---|---|---|
| 0.1 | Initial conversation | Family, relationships, life timelines, memories and historical comparisons; early naming ideas |
| 0.2 | Further conversation | Fictional worlds, uncertain and deep time, mutual consent, opt-in cohort discovery and comparable-product exploration |
| 0.3 | First integrated pack; 2026-11-14 | Predominantly public scope, nonhuman entity types, claims and roles, fourteen topic documents and fourteen reference scenarios |
| 0.4 | Previous revision; 2026-11-14 | English throughout; general users made primary; concise overview and optional detail; meaningful default visuals and story threads; image/video galleries and common media semantics; fifteen topic documents, sixty-five requirements and nineteen scenarios; refreshed name shortlist |
| 0.5 | Previous revision; 2026-11-15 | Dastan selected; sourced name rationale; bounded pilot PRD, actual Shahnameh sample, synthetic common-core fixtures, screen/flow blueprint and limited repository reconnaissance; main requirement/scenario IDs retained |
| 0.6 | Reviewed handoff; 2026-11-15 | All nineteen earlier topic documents reviewed; semantic and publication corrections; typed event associations; original local image; expanded repository review; M0 implementation plan and audit report; standalone execution brief; sample and package validators |
| – | Import into the repository; 2026-11-15 | The documents, the sample and the original media moved into `src/Apps/Dastan`; links updated together; decisions D-029 to D-033 recorded. From here the documents are maintained in the repository and carry no pack version ([import notes](README.md#import-notes)) |

Versions 0.1 and 0.2 identify conversation stages, not separately delivered file packs. Version 0.3 was the first integrated documentation artifact. Version 0.4 preserves existing document paths and stable IDs while adding requirements and scenarios. Version 0.5 adds concrete pilot/design inputs and preserves unchanged v0.4 specifications. Version 0.6 reviews the complete foundation and adds documents 20 and 21. Unchanged document headers retain their previous revision dates. M0 and R0 are delivery milestones, not equivalent to these document-pack versions. These document versions do not represent software releases.

## Maintenance rules

1. Record an explicit new user direction as Accepted; record an inferred implementation approach as Proposed.
2. Review its effect on identity, time, privacy, content, media and experience.
3. Update the relevant requirement and scenario; keep earlier identifiers stable unless a correction is explained.
4. Record why an earlier direction was replaced instead of silently deleting the history.
5. Keep one full treatment of a rule in its owning document and use links elsewhere.
6. Rebuild the index and integrated reader from the maintained English Markdown sources.
7. Recheck time-sensitive product and technology facts before a purchase or implementation commitment.

When integrated into the repository, these files can live under a documentation path consistent with the actual solution. The product name Dastan is accepted. Document 20 provides a provisional implementation layout and architecture recommendation; it is not an accepted technology decision or completed build. The current checkout determines the branch and exact integration. Final logo, domain and public-release commitments remain open.
