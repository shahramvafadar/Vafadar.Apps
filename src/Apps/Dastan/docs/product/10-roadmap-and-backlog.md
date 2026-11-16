# 10 — Roadmap, Priorities and Backlog

**Version:** 0.6  
**Updated:** 2026-11-15  
**Status:** Implementation handoff with a bounded local M0 task and proposed R0 public-release plan. Release dates and budgets have not been set.

## Planning principle

The first audience is the ordinary curious visitor. The product should make a subject understandable and interesting quickly, then support a voluntary next step through a relationship, story, timeline or media item. A visitor should not need a research question or knowledge of the data model to start.

Ideas can continue to evolve while a bounded release is designed. The proposed approach is to clarify the common core, choose one compelling public collection, and design a complete discovery journey around it. Small examples from other domains check that the core remains reusable. Personal and family capabilities remain part of the product direction; their launch sequence is a separate delivery decision.

The v0.3 framing around study and research has been superseded as the primary experience. Sources, accuracy and uncertainty remain essential foundations. Their detail is progressively available instead of dominating every first view. Public galleries are now part of discovery planning, not a feature deferred entirely to a later family-photo release.

## Stages and reviewable outputs

| Stage | Work | Reviewable output | Exit condition |
|---|---|---|---|
| 0 — Written foundation | Reconcile requirements, meanings, decisions and examples | The maintained English foundation and v0.6 implementation package | Important requirements are traceable; contradictions are resolved or explicit |
| 1 — Bound the release | Choose a flagship collection, initial visitor journeys and participation model | A short release PRD and representative content list | A newcomer can understand the intended value and what the release covers |
| 2 — Exercise the common model | Model examples for history, story, company, team and family/media | Semantic examples and representative source records | Core meanings survive the reference scenarios without domain-specific contradictions |
| 3 — Design the experience | Explore concise overviews, visual presets, story threads and galleries | Reviewable flow diagrams and a clickable prototype | General users can orient themselves, follow a connection and understand its context |
| 4 — Resolve technology choices | Inspect the solution and run focused technical spikes | Architecture decisions with evidence | Time handling, rendering, media delivery and permissions have viable approaches |
| 5 — Build a limited release | Implement an end-to-end journey with real reviewed content | A usable website/PWA on the target device matrix | Main journeys, content quality and access behavior meet selected acceptance criteria |
| 6 — Pilot with ordinary visitors | Observe use and correct concrete problems | Findings and prioritized improvements | Major orientation, comprehension, mobile and access issues are addressed |
| 7 — Extend deliberately | Add collections, personal features and deeper tools | Further releases with an explicit purpose | Evidence of use justifies the content and operational cost |

Stages 2, 3 and 4 overlap in the bounded M0 implementation task. The implementing developer should inspect the live repository, make the minimum reversible decisions, and produce a runnable local discovery slice rather than stop after another planning report. A semantic example is not a production schema, and a rendering spike is not a commitment to a framework. Do not wait to model every possible entity before designing a useful first journey.

## Proposed first public journey

1. A visitor arrives through an appealing topic card, image, shared public link or simple search.
2. A concise overview explains the subject and offers a suitable visual default.
3. The visitor follows a clearly labelled relationship or story thread.
4. They open a connected image or video and understand why it belongs in the current context.
5. They switch to a timeline, focused relationship view or gallery while retaining compatible context.
6. Optional detail explains sources, uncertain dates or differing accounts when needed.
7. They keep a useful public link, save a view if supported, report an issue, or leave with a useful understanding.

A public pilot needs a small content-authoring and review workflow behind it. For M0, reviewed versioned local seed data and a recorded content-selection step are sufficient; an admin application, moderation queue and general importer are not prerequisites for the local discovery experience. A beautiful graph with thin, ambiguous or unlicensed content does not establish the intended experience. Sources and essential media rights must be known before publication.

The M0 view and media selection is now bounded in [document 20](20-implementation-plan.md): a focused relationship graph with list equivalent, a narrative event sequence with role-aware parallel lanes, contextual galleries, actual image/video viewing and optional evidence. R0 completes the publication and supported-device gates in [document 16](16-discovery-pilot-prd.md). Image and video both remain part of that public slice.

## Choosing a flagship collection

| Candidate | Why it can engage a general visitor | Main difficulty | Useful bounded experiment |
|---|---|---|---|
| A selected Shahnameh story arc | Familiar characters, family links, turning points and illustrations | Edition context, spoilers and narrative time | One defined arc and edition with permitted media |
| A short historical comparison | Parallel lives, places and events make simultaneity visible | Uncertain dates, disputed accounts and misleading period boundaries | A limited period with a few well-supported lanes |
| Histories of a few companies | Products, founders, acquisitions and changes connect familiar entities | Identity changes and source quality | A stable historical slice rather than current financial data |
| A completed football season or selected clubs | Players, teams, roles, matches and images form accessible connections | Club/team/season distinction and media supply | A completed season with sourced roles and usable media |

Assess candidates on immediate interest, understandable connections, visual variety, content depth, source quality, media permissions and production effort. Do not choose solely because a dataset is large or technically easy to import.

The v0.6 handoff continues to use **Rostam and Sohrab** as its working flagship, following the instruction to proceed. It remains a provisional launch choice; the bounded PRD and sample now make it reviewable. Company and team examples should still exercise the common model even if they are not part of the initial public collection. Family/media examples exercise permissions before that capability is launched.

## Prioritized backlog

Priorities describe dependencies, not calendar deadlines or person-day estimates. Existing backlog identifiers are retained so earlier discussions remain traceable.

| ID | Work | Priority | Dependency or expected result |
|---|---|---|---|
| B-001 | Choose the flagship and a few general-visitor journeys | Before detailed design | Q-01 and Q-12; a newcomer need not have a research question |
| B-002 | Define initial entity types, roles and relationships | Before detailed design | Documents 03 and 14; bounded vocabulary |
| B-003 | Stabilize time meanings and edge examples | Before detailed design | Documents 04 and 14 |
| B-004 | Define public/private boundaries and publication paths | Before detailed design | Documents 06 and 07 |
| B-005 | Identify initial sources, permitted media and content ownership | Before real content publication | Source and media inventory for the flagship |
| B-006 | Inspect Vafadar.Apps and its integration constraints | Before technology selection | Repository structure, dependencies, identity and deployment evidence |
| B-007 | Design discovery entry, entity overview, search and optional source detail | First experience design | Concise first glance; SC-01 through SC-04 and SC-15 |
| B-008 | Design comparison, uncertain time and narrative progression | First experience design | SC-03 through SC-08; no false precision or chronology |
| B-009 | Provide mobile, keyboard and non-graphical paths | Alongside design | Useful list/text alternatives and accessible controls |
| B-010 | Evaluate graph and timeline rendering with realistic examples | Before architecture is fixed | ADR-P08 and the time contract |
| B-011 | Decide storage, search and interface approaches | During the bounded M0 start | Record necessary reversible choices; local curated data may precede a production persistence decision |
| B-012 | Provide authoring, source attribution and editorial review | Before public release | Publish and correct one real collection |
| B-013 | Provide public pages, stable links and discoverability | Limited public release | Useful text and linked context outside a graph |
| B-014 | Verify PWA installation and update behavior | Limited public release | Recorded target device/browser matrix |
| B-015 | Report and correct errors across dependent views | Before public release | Revision history and updated projections |
| B-016 | Save collections, useful views and comparisons | After the main discovery journey | Explicit private/public scope and revision expectations |
| B-017 | Implement invitation, identity claim and mutual social consent | Before personal-space release | SC-09 and SC-10; distinct permission steps |
| B-018 | Add family trees, life timelines and audience circles | Personal capability release | Account/person separation and controlled visibility |
| B-019 | Add personal photos, memories, approved tags and recent shares | Personal media release | SC-11 and SC-17; common media foundations from B-023 |
| B-020 | Add opt-in university or workplace account discovery | After consent and time rules | SC-12; distinct from public role records |
| B-021 | Explore time-aware maps, advanced outputs and edition comparisons | Later expansion | Evidence of visitor or specialist demand |
| B-022 | Develop Dastan pronunciation guidance, wordmark, logo and distinctive release presentation | Alongside experience design | Name selected by user; document 11 records naming overlaps and unfinished identity work |
| B-023 | Design public image/video galleries and contextual media viewing | First experience design | Document 15; SC-16, SC-18 and SC-19 |
| B-024 | Define coordinated visual presets and retained exploration context | First experience design | R-EXP-03 and R-EXP-05; SC-16 |
| B-025 | Author short explainable discovery threads | First content/design slice | SC-15; interesting links without invented causation |
| B-026 | Evaluate media delivery, renditions and revoked-access behavior | Before media implementation choice | R-MED-05 and SC-19; private derivatives must remain controlled |
| B-027 | Test first-glance understanding and optional depth with general users | Prototype and pilot | Comprehension and useful exploration, not time spent alone |

## Outside the initial commitment

A public social newsfeed, algorithmic friend suggestions, unreviewed anonymous editing, source-free bulk imports, live sports coverage, live financial information, a complete historical-border atlas and separate native applications are not initial release commitments. Neither is a specialist research workbench. These can be considered later when an actual use case and its cost are clear.

The authorized recent-share gallery is part of the accepted personal-media direction. Its presence does not imply a public follower feed, engagement ranking or unsolicited notifications.

## Ready for design

The next bounded design slice needs:

- A representative visitor and collection, with a provisional choice allowed while alternatives are assessed.
- Realistic examples of the main entities, connections, stories and media.
- Clear time/world meanings, including uncertainty and unknown values.
- A concise first-glance content contract and a few selected visual presets.
- Public/private rules for every journey, including media derivatives and counts.
- A route to optional sources and alternative accounts.
- A small set of main tasks rather than every capability on one screen.

Much of the conceptual foundation is now documented. The remaining decisions are bounded choices, not a reason to pause all useful work. Content sampling and low-cost flow exploration can proceed with explicit provisional assumptions.

## Ready for development

At implementation start, review the principal flows, exercise the semantic examples, inspect the actual repository, record the decisions needed for M0, and identify content and media responsibilities. Do not require a final platform-wide database, universal ontology, cloud topology or editorial administration system before the local slice can run. Selected acceptance criteria must cover the ordinary-user journey as well as temporal meaning and access. This does not prevent a small disposable prototype or technical spike used to answer a specific question.

## Measures for the prototype and pilot

Assess whether visitors understand the first view, identify the meaning of a connection, choose a useful next thread, retain orientation across views, distinguish different time/media meanings and use the product comfortably on a phone. Ask whether they found something interesting or useful. Sources should be findable when requested, without being mandatory reading for every interaction.

For personal spaces, assess whether people understand the audience, distinguish a tag from permission and can withdraw visibility. Report content problems and confusing empty states as quality issues. Longer sessions or more clicks alone do not demonstrate value. No success-rate targets, participant count, performance budget or monthly cost estimate have been validated yet.

## v0.6 progress and executable handoff

The user's next-step instruction has produced the [Discovery Pilot PRD](16-discovery-pilot-prd.md), [source-backed content and sourcing plan](17-flagship-content-and-source-plan.md), [screen and flow blueprint](18-discovery-flows-and-design-brief.md), and [repository reconnaissance with technical spike briefs](19-repository-reconnaissance-and-spike-plan.md). Dastan is now the accepted name. The sample includes actual source-linked story records and clearly synthetic examples from other domains.

These artifacts complete the bounded definition/content/flow step. They do not establish a functioning interface, completed user study, production content approval, deployed PWA or executed technical spike. Media references with pending reuse permission stay out of the proposed public publication path.

The next coding deliverable is **M0: a runnable local public discovery slice**, including the complete connected journey, phone/RTL handling, source detail and an accurate return path. [Document 20](20-implementation-plan.md) owns the work order, selected technical defaults, finite checks and handoff instructions. Run only the focused experiments needed to settle a concrete risk, record the choice, and continue implementation. The implemented slice can also serve as the reviewable interface prototype.

M0 is not a declaration that R0 publication is ready. Editorial operations, final media approval, content correction, observation with general visitors and the supported-device matrix retain their own release gates. Private-family fixtures are development checks and must not enter public demo data or client bundles. Identity work uses Dastan; temporary text branding and a clearly provisional application icon can support M0 while the final logo is developed.
