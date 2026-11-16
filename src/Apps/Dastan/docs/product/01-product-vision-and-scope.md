# 01 — Product Vision, Audience and Scope

**Version:** 0.6  
**Updated:** 2026-11-15  
**Status:** Product foundation. Accepted direction is separated from proposed release choices.

## Product definition

**A visual discovery platform where curious people explore connected entities, stories, moments and media, with optional personal and family spaces governed by their owners' access choices.**

A visitor might arrive through an intriguing image, a familiar person, a football club, a company, a story or a period in history. The product helps them understand the essentials quickly, see meaningful connections, and choose another thread to follow. A useful first experience must not require a research question, knowledge of graph terminology or familiarity with a complex tool.

The selected product name is **Dastan**, chosen by the user on 2026-11-15. **Visual Discovery Platform** remains a functional descriptor. The intended solution is **Vafadar.Apps**, with a website delivered as an installable PWA for supported phone and Windows environments.

## The audience correction in version 0.4

The primary launch audience is the **curious general public**. Researchers remain a possible secondary audience. Earlier language that made a scholarly reference or research workflow the main launch experience is superseded.

This changes presentation and priorities, while preserving data quality:

- Start with concise, interesting and understandable information.
- Make several visual views part of the normal experience.
- Offer a clear next connection instead of an overwhelming graph.
- Reveal context, detailed records and sources when useful or requested.
- Let image and video galleries serve as starting points and continuation paths.
- Keep uncertainty and story context visible in plain language without filling the first screen with editorial metadata.

A short presentation must still be accurate. Optional detail is a presentation decision; it does not permit misleading captions, invented dates or unlabelled speculation.

## Accepted product direction

| ID | Direction |
|---|---|
| VIS-01 | Most published knowledge content is public. |
| VIS-02 | The domain includes people, organizations, companies, football teams, places, works, fictional characters and other appropriate entity types. |
| VIS-03 | Connections, time, place and stories support visual understanding and discovery. |
| VIS-04 | Genealogy, personal life timelines, social relationships and memories remain supported purposes. |
| VIS-05 | Fictional and mythological worlds, including Shahnameh, use the shared foundation with explicit narrative context. |
| VIS-06 | Precise, partial, approximate, interval, BCE and deep-time expressions are supported. |
| VIS-07 | Shared social relationships such as close friendship require a request and acceptance between two accounts. |
| VIS-08 | Email or link invitations and consent-based discovery of personal accounts are supported. |
| VIS-09 | Multiple timeline and chart views can compare and connect subjects. |
| VIS-10 | Ideas, naming, design and technology choices evolve through maintained documentation. |
| VIS-11 | General users are the primary audience; the first view provides essential information with optional depth. |
| VIS-12 | Connected story threads and multiple visual defaults encourage voluntary exploration. |
| VIS-13 | Images and videos are accessible through contextual public galleries and authorized personal galleries, including recent shares. |
| VIS-14 | One common core supports different purposes rather than separate, disconnected information products. |
| VIS-15 | All maintained project documentation is written in English. This does not select the languages of the future interface. |
| VIS-16 | The selected product name is Dastan. The logo, localized wordmark and release presentation remain design decisions. |

## Audience and intent

| Audience or situation | Typical intent | Product response |
|---|---|---|
| Curious visitor | “That looks interesting; what is it connected to?” | A short overview, an understandable visual and a few useful next threads |
| Fan of a team, company or subject | “How did this develop, and who was involved?” | Time-aware roles, highlights, media and related entities |
| Reader of a story | “Who is this character, and what do I know so far?” | Story-aware relationships and optional spoiler controls |
| Person browsing an era | “Show me what this period was like.” | Contextual images, videos, events, places and clear descriptions |
| Family or friendship participant | “What have people I know shared?” | Authorized recent media, memories and contextual personal timelines |
| Registered explorer | “I want to keep this view and return to it.” | Saved trails, collections and comparisons with explicit visibility |
| Teacher or detailed reader | “I want to explain or check this connection.” | Optional depth, sources and a guided presentation |
| Researcher | “I need evidence, alternatives and a referenceable version.” | Secondary depth and traceability, without dictating the initial interface |
| Contributor or editor | “I can improve this information.” | Structured contribution and review tools separate from casual browsing |

These are usage situations, not a demand to build a separate application for each group.

## The discovery loop

The proposed experience is:

1. **Notice:** encounter an image, highlight, chart, short story or search result.
2. **Understand:** see what it is, why it matters in this context and what is uncertain.
3. **Connect:** choose a labelled relationship, shared event, place, period or media item.
4. **Explore:** move to another entity or view without losing the trail.
5. **Keep or leave:** save something meaningful, share an authorized public link, return, or stop.

Success means an enjoyable, understandable discovery. Endless scrolling, repeated prompts and time spent are not substitutes for user value. The platform should make it easy to return to an earlier step and understand why a recommendation was offered.

## One core, several purposes

| Shared capability | Examples of use |
|---|---|
| Typed entities and stable identity | A person, a company, a team, a place, a work or a natural phenomenon |
| Contextual relationships and roles | Parentage, employment, team membership, ownership or fictional allegiance |
| Events, periods and temporal expressions | A life event, a reign, a season, an acquisition or a very ancient interval |
| Media assets and contextual links | Family photographs, archival material, artwork, film clips and explanatory video |
| Worlds, editions and story sequences | Historical content, Shahnameh, adaptations and family narratives |
| Sources, revisions and publication | Trustworthy public summaries and optional deeper inspection |
| Access and consent | Public discovery, restricted memories, mutual social relationships and opt-in account discovery |
| View state and collections | The same allowed information explored as a chart, timeline, story or gallery |

A chart and a gallery are projections of common information, not independent copies with separate facts. Each domain can have appropriate fields, relation types, validation and presentation.

## Content spaces

### Public discovery

Published information about entities, events, relationships and media. Proposed access: ordinary reading without an account. Entry can be visual, thematic or narrative as well as search-based. Public reading does not automatically imply unrestricted anonymous editing.

### Story worlds

Works, editions, characters and story events. Narrative order, in-world chronology and historical interpretation are distinct. The first screen can remain welcoming while the underlying record preserves the edition and evidence.

### Personal and family spaces

Life timelines, family relationships, memories and selected social connections. Users control audiences and discovery permissions. A public university or company does not expose private people attached to it. An account can contribute publicly and maintain private memories.

## What makes an initial view useful

Proposed first-layer content is a recognizable title, an appropriate image or visual, a short contextual summary, a few essential facts and a limited set of meaningful connections. The exact amount depends on the entity and screen size.

Additional layers can reveal the full timeline, wider relationships, galleries, alternative accounts and sources. This is progressive disclosure, not a requirement to hide every source or warning behind several clicks. A disputed identity or a modern reconstruction must be clear at the point where it could mislead the user.

## Proposed release focus

Start with **one coherent public discovery experience containing enough good content to reward following several threads**. Retain small examples from other domains to verify that the core is reusable.

The bounded **Rostam and Sohrab** arc remains the provisional flagship for the v0.6 implementation handoff. The [Discovery Pilot PRD](16-discovery-pilot-prd.md), [source plan](17-flagship-content-and-source-plan.md), and [screen/flow blueprint](18-discovery-flows-and-design-brief.md) make that next step concrete. This is a working delivery choice, not a claim that the final launch collection has been approved or that the product is implemented. The next coding task is a runnable local **M0 discovery slice** defined in [document 20](20-implementation-plan.md); the complete **R0 public pilot** has separate content, editorial, usability and device gates. The handoff authorizes starting bounded implementation work without requiring every future feature or final visual-identity choice to be settled. Historical periods, teams and companies remain future public collections. Audience appeal, content quality, media rights, scope and connectedness should inform release selection.

A company, team, deep-time and private-family example should help validate the model. They do not all need to become fully published launch sections.

## Scope boundaries

The model can grow without requiring an encyclopedia of everything at launch. A live sports service, financial market feed, unrestricted public social network, universal historical atlas, large-scale unreviewed imports and autonomous factual publishing are not current launch commitments.

A sophisticated analytical workspace is not the main first release. Sources, coherent time, safe access and correct identity remain foundational because later views depend on them.

## Proposed success measures

- A new visitor understands the focus of a view without learning the data model.
- A visitor can explain why two subjects are connected.
- Interesting next steps are available and easy to leave or reverse.
- A view change preserves useful context.
- Concise summaries do not cause confusion about uncertainty, story worlds or media origin.
- Images and videos lead naturally to related content.
- Authorized personal galleries show relevant shares without exposing restricted information.
- Core tasks work on a phone and through a non-graphical alternative.
- Deeper information and sources remain reachable when needed.

Concrete targets and study methods will be set before evaluation. No usability, retention, performance or market-demand result has yet been established.

## Related documents

See [requirements](02-requirements-and-use-cases.md), [the shared model](03-domain-model-and-ontology.md), [visual discovery](05-public-discovery-and-narrative.md), [media and galleries](15-media-and-galleries.md), and [the roadmap](10-roadmap-and-backlog.md).
