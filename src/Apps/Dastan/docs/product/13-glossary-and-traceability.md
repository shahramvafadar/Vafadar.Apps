# 13 — Glossary and Requirements Traceability

**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Shared terminology and cross-document traceability; identifiers retained from earlier versions

## Purpose and audience correction

The primary launch audience is the general curious public. The product should offer an engaging, concise first look, connected subjects and story threads, several meaningful visual starting points, and progressively available detail. Research remains a secondary possible use. Photos and videos are part of the shared experience across public subjects, historical periods, personal memories, and authorized family or friend sharing.

The terminology below describes the conceptual product. It does not require these terms to appear in the everyday interface or prescribe a production schema. Dastan is the selected product name. All documentation is now English; this is separate from the choice of interface languages and locales.

## Shared glossary

| Term | Meaning in this product | Do not confuse it with |
|---|---|---|
| Entity | A subject with an identity about which information is recorded, such as a person, company, team, work, or place | An account or merely a card on a screen |
| Entity type | A semantic category with suitable properties and relationships | An arbitrary label with no meaning or rules |
| Account | A member's authentication identity, settings, and activities | All public knowledge about that person |
| Person record | Information about a human subject in a specified context | Necessarily a registered user |
| Narrative character | A person or other being in an identified work, world, and account | Automatically the same as a similar historical person |
| Statement or claim | A specific assertion about a property, relationship, or event, with context and evidence | An unquestionable declaration with no room for disagreement |
| Relationship | A meaningful connection between entities, sometimes qualified by direction, role, and time | A decorative line or mere simultaneity |
| Role | A position in a relationship or event, such as coach, buyer, or witness | A permanent intrinsic property of a person |
| Time-qualified membership | Participation in an organization or group during a period and in a role | Automatic friendship or membership of the same class |
| Event | An occurrence with participants, context, and a temporal expression | Necessarily a perfectly known instant with a day and hour |
| Period | Continuation of a state or activity across an interval | The possible date range of one uncertain occurrence |
| Valid time | The time a statement concerns in the real world or narrative | The time the system record was created or edited |
| Record time | When information was entered or changed in the system | When the described event occurred |
| Temporal precision | The known granularity, such as a day, year, or century | Confidence that the claim is correct |
| Temporal uncertainty | Possible values, bounds, or alternatives for a temporal expression | The definite duration of a period |
| Temporal reference | The calendar, narrative, or scientific reference that gives a value meaning | An undefined moving “today” |
| Temporal framework | Rules for reference, units, calendar, and order | The left-to-right or vertical direction of a chart |
| Narrative order | A position in reading or presentation order | In-world event chronology |
| World or context | The real or narrative setting in which a statement has meaning | A public/private access permission |
| Work and edition | A work and a specified edition, translation, retelling, or adaptation | Every similar account merged into one text |
| Source | Material that presents evidence or an assertion | Automatically independent proof of the assertion |
| Acquisition provenance | How information arrived, such as a revision of an external dataset | The original supporting evidence for a relationship |
| Display rank | The selection or ordering of information in a view | Probability of truth or scientific confidence |
| Review status | A stage or outcome of content examination | The popularity or importance of a subject |
| Information space | A public, personal, family, or narrative context with defined boundaries | Necessarily a separate database or service |
| Audience circle | A selected group used for sharing | A family relationship or accepted social connection |
| View filter | A selection of what to display from permitted information | An access-control or security decision |
| Personal discoverability | Permission for an account or personal profile to appear in people-discovery routes | Permission to publish every private history or veto all documented public knowledge |
| Saved exploration or research view | A selection of subjects, time, filters, and presentation for a purpose | An independent copy of every selected fact |
| Guided narrative | A chosen sequence of material that explains a subject | All available information or the only valid account |
| Graph | The structure of entities and their multiple relationships | A requirement to choose a graph database |
| Family tree | A specialized view of genealogy and family relationships | The complete structure of every product relationship |
| PWA | Web delivery with supported installation and application-like behavior | Guaranteed complete offline use or identical behavior on every device |

These definitions are proposed shared product contracts. Source-dependent temporal conventions and technical limitations are documented with references in [document 04](04-time-and-uncertainty.md) and [document 09](09-architecture-and-technology-decisions.md).

## Discovery and media terminology introduced in version 0.4

| Term | Meaning in this product | Boundary to preserve |
|---|---|---|
| Essential overview | The small set of facts, context, and visuals that makes a subject understandable at first glance | It is not an exhaustive article or a claim that omitted details do not matter |
| Progressive disclosure | Revealing deeper explanation, context, and sources when useful | It must not hide a qualification that changes the meaning of the first view |
| Visual preset | A meaningful starting presentation suited to a subject and task | It does not force every entity into the same chart; the exact initial set remains proposed |
| Discovery thread | An explainable route through connected entities, events, media, or stories | Adjacency, recommendation, or editorial sequence does not prove causation |
| Exploration context | Focused subject, applicable time, framework, world or edition, filters, and navigation state | It is not a permission to access all connected content |
| Shared core | Reusable identity, statement, time, relationship, provenance, media, and policy concepts | It is not a commitment to one schema, service, or universal untyped form |
| Typed capability | A set of meaningful behavior for a subject, such as membership, genealogy, or narrative participation | It is not a requirement that every entity support every feature |
| Public or personal view | An authorized presentation of shared concepts for a particular use | It must not duplicate truth or combine audiences implicitly |
| Media item | A logical image or video item with identity, context, and provenance | Merely a filename, thumbnail, or uploaded binary |
| Media rendition | A delivery representation such as a thumbnail, video poster, or processed version | A new unrelated item with unrestricted access |
| Media association | A contextual link such as depicts, records, illustrates, created by, or related to | An ambiguous tag that makes every relationship equivalent |
| Personal tag | A link identifying a person in a personal sharing context, subject to the applicable acceptance rule | Automatic authorization for the media or public historical description |
| Capture time | When a photo or video was recorded | The date of an older event represented by something in the frame |
| Work creation time | When a painting, illustration, animation, or edited work was made | The time of the subject it depicts |
| Depicted or discussed time | The event, period, or narrative time represented in the content | Proof that the media was created during that time |
| Digitization or transformation time | When a scan, restoration, crop, or conversion was made | The creation date of the original work |
| Publication time | A specified release or publication in a known context | Automatically the first-ever publication |
| Upload or import time | When a file or reference entered this system | Capture, event, or publication time |
| Share activity | A particular authorized action sharing content with an audience or destination | A change to the underlying asset's age or a grant to everyone |
| Gallery | A contextual presentation of permitted media and associations | A separate copy of its underlying assets |
| Curated album | An intentional selection, ordering, and description of media | A requirement that every member have the same original audience |
| Dynamic gallery | A view evaluated from context and selection rules | A permanent snapshot or an access-control exception |
| Saved gallery | Stored membership or query rules, presentation choices, and context | A frozen right to access content after permission changes |
| Gallery time basis | Whether a view concerns capture, creation, depiction, or sharing time | An unlabeled generic date field |
| Recent authorized shares | Sharing activities visible to the current viewer, ordered by the chosen sharing rule | All friends' uploads or every asset with a new file timestamp |
| Contextual media viewer | A viewer retaining entry context and offering permitted links and a meaningful back action | An isolated file page that loses the user's place |
| Media reuse rights | Conditions for using or redistributing a work or file | Merely being able to view the file |
| English documentation | English headings, tables, explanatory text, and working specifications | An English-only product interface |

## Traceability of the established conversation requirements

The mappings below retain the identifiers and relationships recorded in version 0.3. Wording reflects the current audience priority without deleting earlier family, historical, narrative, or technical requirements.

| Requirement or point | Requirement or decision | Reference document | Scenario or next work |
|---|---|---|---|
| A new website in the same solution, installable on mobile and Windows | D-001, R-PLAT-01 | 09 | B-006, B-014 |
| Several integrated purposes | D-005 | 01, 03 | Company, team, narrative, and family examples; now also contextual media |
| Ancestors and descendants in a family tree | R-PER-01, R-PER-02 | 03, 06 | B-018 |
| Several friendship levels, colleagues, and classmates | R-DOM-04, R-PER-03 | 03, 06 | SC-10, SC-12 |
| Name, title or nickname, birth date, and photo | R-DOM-02 | 03, 06 | Person model and type-appropriate entry forms |
| Invitation by email or shareable link | R-PER-04 | 06 | SC-09 |
| Bilateral acceptance of an account-based social relationship | D-007, R-PER-03 | 06 | SC-10 |
| Family and other audience-circle visibility settings | R-PER-05 | 06 | SC-13 |
| Education, work, and places in a life timeline | R-PER-06 | 03, 04, 06 | SC-12 |
| Posts and old photos placed in their memory context | R-PER-08, R-TIME-07 | 06 | SC-11 |
| Tagging and the identified person's acceptance | D-017, D-018 | 06 | SC-11 |
| Dynasties, governments, descendants, and important events | R-PUB-03, R-DOM-07 | 03, 05 | SC-04 |
| Filtering by time and place | R-PUB-04 | 05 | SC-04, B-021 |
| Horizontal, vertical, and multi-track comparisons | R-PUB-05 | 04, 05 | SC-04 |
| The supplied Iran, China, and Rome comparison image | R-PUB-05 | 05 | Three compatible tracks on a meaningful common scale |
| Literary works and the Shahnameh world | R-STORY-01, R-STORY-02 | 03, 05, 07 | SC-03 |
| Approximate time around ten or eleven thousand years ago | R-TIME-01, R-TIME-02 | 04 | SC-05 |
| A range between ten and twenty thousand years ago | R-TIME-02, R-TIME-03 | 04 | SC-05, SC-06 |
| Fifty to sixty million years ago | R-TIME-04 | 04, 09 | SC-07 |
| 500 BCE | R-TIME-04, R-TIME-05 | 04 | SC-08 |
| Finding personal profiles by university and year with permission | R-PER-07 | 06 | SC-12 |
| Review of the Shahnameh example and comparable products | Comparative-review evidence and differentiation hypothesis | 08 | Lessons for 04 and 05 |
| Most knowledge being publicly available | D-002, R-PUB-01 | 01, 05, 07 | GOV-001 and the first acceptance example in 07 |
| Companies, teams, and other entity types | D-003, R-PUB-02 | 03 | SC-01, SC-02 |
| A visual reference for learning, discovery, and possible research | R-PUB-06 | 01, 05 | Real user tasks in 10; everyday curiosity now has launch priority |
| Optional restricted personal spaces | D-004 | 06 | SC-09 through SC-13 |
| Documentation for later system design | D-020 | All documents | This package and the change log |
| Name and logo development | D-019 | 11 | B-022 |
| Later tool, technology, and development decisions | D-019 | 09, 10 | ADR-P01 through ADR-P10 |

## Version 0.4 discovery, media, and language traceability

The requirement register in [document 02](02-requirements-and-use-cases.md) is authoritative for requirement wording. [Document 14](14-reference-scenarios.md) owns scenario definitions. The following table links those identifiers; it does not introduce alternative scenario definitions.

| Added or corrected direction | Requirement | Reference documents | Linked scenario or validation focus |
|---|---|---|---|
| General curious users are primary, with a useful first glimpse | R-EXP-01 | 01, 03, 05, 09 | SC-15 |
| Essential overview followed by context, detail, and sources | R-EXP-02 | 03, 05, 07, 09 | SC-15 |
| Several meaningful default visual views; exact set remains proposed | R-EXP-03 | 03, 04, 05, 09 | SC-16 |
| Follow explainable connected subjects and story threads | R-EXP-04 | 03, 05, 09 | SC-15 |
| Retain focus, time, world, filters, and back state across views | R-EXP-05 | 03, 04, 05, 09, 15 | SC-16 |
| Enter through curated themes, cards, and media as well as name search | R-EXP-06 | 01, 05, 15 | SC-15 |
| Images and videos are first-class contextual content | R-MED-01 | 03, 09, 15 | SC-17 through SC-19 |
| Galleries reached through entities, periods, places, events, and stories | R-MED-02 | 03, 04, 05, 15 | SC-16, SC-18 |
| Recent authorized sharing from friends and family | R-MED-03 | 04, 06, 09, 15 | SC-17 |
| Separate capture, creation, depiction, upload, publication, and share dates | R-MED-04 | 04, 09, 15 | SC-17, SC-18 |
| Apply media access to all routes, posters, counts, captions, suggestions, and downloads | R-MED-05 | 06, 09, 15 | SC-19 |
| A contextual media viewer with links and restored back state | R-MED-06 | 03, 05, 09, 15 | SC-16, SC-19 |
| Curated albums and dynamic galleries reuse media rather than duplicating it | R-MED-07 | 03, 09, 15 | SC-18; gallery identity, membership, and policy validation |
| Accessible media presentation with captions, alternative text, attribution, rights, and provenance | R-MED-08 | 05, 07, 09, 15 | SC-18, SC-19; media accessibility and provenance validation |
| Every document is English; interface language is a separate decision | R-PLAT-07 | All documents | Package language review and interface-language decision kept separate |

SC-15 through SC-19 cover the newly added casual exploration, cross-view navigation, recent sharing, historical depiction, and contextual video/access behavior in document 14. Use those canonical definitions when designing or validating work. Do not infer that every new requirement has a separate standalone scenario or that these scenario IDs replace the earlier ones.

## Preserved principles

A person's identity is separate from an account. Invitation, association with a record, and social acceptance are different actions. A visual filter is not access control. Temporal overlap does not establish personal acquaintance. Memory time differs from publication and sharing time. Accepting a personal tag does not enlarge the audience of the photo or video. Uncertain dates do not become invented exact dates. Narrative and historical claims are not merged without context. Content administrators do not own exclusive truth.

The new audience priority preserves these principles. Concise and engaging presentation must still show a qualification when it changes the meaning. Public exploration does not require a family account. Shared media does not combine audiences. A gallery is a contextual view, not a new copy of the facts or a way around revoked permission.

## How designers and developers should continue

Read [the vision](01-product-vision-and-scope.md) and [the decision register](12-decisions-questions-and-change-log.md) first. Locate the relevant accepted or proposed requirement in [document 02](02-requirements-and-use-cases.md). Use documents 03 and 04 for information meaning, 06 and 07 for access and publication, 05 and 15 for visible discovery and media behavior, and 14 for scenario expectations.

Consult [document 09](09-architecture-and-technology-decisions.md) and inspect the actual solution before choosing technology. Dastan is the selected name. Document 20 now recommends a provisional minimal architecture for M0. A recommendation becomes an implementation decision only when checked against the current repository and recorded with its evidence; it does not prescribe a complete production schema or hosting provider. Any change should identify its effect on the requirement, scenario, decision, and dependent documents rather than silently redefining a shared term.

## Version 0.5 pilot and design handoff

The base register still contains 65 requirements and 19 reference scenarios. PILOT, FLOW, SCREEN, TASK and fixture identifiers add a delivery/design layer; they do not replace those requirement or scenario IDs. A prospective task is not a completed test result.

| New decision or artifact | Traceability | Owning document or file |
|---|---|---|
| Dastan selected by the user | D-026; VIS-16 | 11; 01 |
| Proceed with the bounded next step | D-027 | 16–19 |
| Proposed Discovery Pilot R0 scope | P-012, P-013; PILOT-01 through PILOT-10 | [16 — Pilot PRD](16-discovery-pilot-prd.md) |
| Source-linked Rostam and Sohrab sample | R-DOM-04, R-DOM-07, R-STORY-01 through R-STORY-05, R-GOV-01; SC-03 | [17 — Content plan](17-flagship-content-and-source-plan.md); [sample JSON](../../content/samples/shahnameh-pilot.json) |
| Main discovery, relationship, story, gallery and source flows | R-EXP-01 through R-EXP-06; R-MED-02, R-MED-06; SC-15, SC-16, SC-18, SC-19 | [18 — Screen/flow blueprint](18-discovery-flows-and-design-brief.md) |
| Synthetic company, sport and private-family checks | SC-01, SC-02, SC-11, SC-13, SC-17, SC-19 | [Cross-domain fixtures](../../../../../test/Apps/Dastan/Fixtures/cross-domain-fixtures.json) |
| Pinned repository observations and unexecuted spike briefs | B-006, B-010, B-011, B-026; ADR-P01 through ADR-P10 | [19 — Reconnaissance](19-repository-reconnaissance-and-spike-plan.md) |

The selected name does not establish domain or trademark availability. The provisional flagship is a concrete design choice, not an instruction to make every future collection a Shahnameh adaptation. Public factual data, synthetic private fixtures and present-day explanatory media retain distinct publication and evidential status.


## Version 0.6 implementation and audit traceability

The reviewed package retains **65 requirements and 19 reference scenarios**. The following additions connect those contracts to the first runnable milestone; they do not create new accepted user requirements by implication.

| Item | Meaning / traceability | Owner |
|---|---|---|
| D-028 | Complete review and implementation handoff requested by the user | 12; 21 |
| P-014, P-015 | Proposed M0 delivery and concrete technical starting point | 12; 20 |
| M0-01 through M0-12 | Observable local implementation acceptance; current state is future/unperformed | 20 |
| Role-aware event association | Distinguishes physical participants, mentions, settings and objects; R-DOM-07, R-STORY-03; SC-03 | 03; 17; sample schema `dastan-pilot-0.2` |
| Explicit local-demo projection | Sample review is not production publication; R-GOV-01, R-GOV-02, R-STORY-04 | 07; 17; 20 |
| Institutional-query disclosure | Appearing in a university/year result reveals that affiliation; opt-in and permitted membership context are both needed; R-PER-07; SC-12 | 06 |
| Media illustration added | Original source-backed image plus existing local video; R-MED-01, R-MED-04, R-MED-08; SC-18, SC-19 | 17; original media records |
| Web culture isolation | Each request/circuit has independent language state; reuse requires a suitable lifetime | 09; 19; M0-09 |
| Input validation | Sample reference/type/temporal checks and negative mutations; not application test results | 21; supplied sample validator |
| Full review coverage | All nineteen previous documents, both data files, media references and the delivery path reviewed with explicit limits | 21 |

| New working term | Meaning | Boundary |
|---|---|---|
| M0 | First runnable local discovery milestone | Not the public release or the full product |
| R0 | Proposed public discovery pilot | Publication, usability, operational and device evidence remain required |
| Editorial catalogue | Complete source/review input, which may contain withheld content and future story details | Not automatically safe for a client download |
| Local-demo eligibility | Explicit permission within the supplied contract to exercise a record in local demonstration | Not production publication approval or a privacy exemption |
| Projected view | The allowed subset and representation for the current context, computed before serialization/layout/counting | Not a browser filter over a confidential or complete editorial payload |
| Event association | A typed link explaining how an entity relates to an event | Does not automatically imply physical attendance |
| Positioned period | A state extending across an interval in an identified temporal frame | Not merely a quantity such as a duration of five years |

The original dataset identity and record IDs are retained for continuity while the format version changes. Adapters must read the explicit schema version; never infer the format from the dataset ID's historical suffix.
