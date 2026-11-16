# 05 — Visual Discovery, Story Threads and Progressive Detail

**Version:** 0.6  
**Updated:** 2026-11-15  
**Status:** Proposed experience specification based on accepted direction. This is not a selected screen design.

## Experience objective

A curious visitor should find something understandable and interesting at first glance, then follow a meaningful connection at their own pace. A useful session can begin without knowing a name or having a research question.

The initial interface prioritizes a concise overview, attractive visual structure and connected media. Sources, alternative accounts and detailed records remain available as further layers. Plain-language uncertainty and context must stay visible wherever omission could mislead.

## Proposed information architecture

| Area | Main purpose | Typical entry |
|---|---|---|
| Discover | Find themes, interesting entities, short stories and visual collections | Curated card, image or a simple search |
| Connections | Explore relationships around a focus | Entity, highlighted connection or story participant |
| Timelines | Understand periods, change and compatible comparisons | Era, life, company, team season or event |
| Galleries | Browse connected images and videos | Period, entity, place, story, album or authorized recent shares |
| My Space | Manage personal timelines, family, saves and sharing | Signed-in account |
| Contribute | Suggest or review content | Secondary action from an entity or separate editor area |

These are conceptual destinations, not a fixed five-tab or six-tab navigation requirement. On mobile, a smaller persistent navigation set may expose the remaining areas contextually. Public discovery should not require family setup.

## Progressive disclosure

| Layer | User question | Proposed content |
|---|---|---|
| First glance | “What am I looking at, and why is it interesting?” | Recognizable title, suitable image or chart, short summary, essential facts, a few relevant connections |
| Context | “How does this connect, and what happened around it?” | Focused relationships, timeline, related media, a short story and meaningful filters |
| Depth | “Tell me more or help me check this.” | Longer explanation, complete roles and intervals, sources, alternate accounts and revisions |

An initial card might use one short summary and a small selection of facts, but exact counts are design choices to test. “More” must lead to meaningful additional information rather than an expanded duplicate.

A disputed attribution, an approximate date or a modern reconstruction is not hidden solely to keep the first layer clean. Short labels can communicate these conditions without exposing the full editorial interface.

## Default visual-view families

The user has requested several chart or similar visual defaults. The following catalogue proposes where each is useful.

| View | Best starting context | What it helps reveal | Meaning that must remain clear |
|---|---|---|---|
| Focused relationship map | Person, company, team, object or story character | Immediate connections and paths worth following | Edge type, direction where relevant, period and context |
| Family or lineage tree | A person or fictional family | Ancestors, descendants and selected family branches | Adoption, guardianship and different relationship types are distinguishable |
| Parallel timeline lanes | Periods, organizations, lives or teams | What overlaps and how subjects change | Compatible temporal frame and consistent scale |
| Connected story paths | Several entities participating in shared events | Where their paths meet and diverge | Schematic paths show participation, not geographic routes or causality |
| Vertical story timeline | A person, company or narrative on mobile | Highlights and readable event sequence | Reading order, event chronology and visual spacing are not interchangeable |
| Media gallery with contextual groups | Entity, place, period, story or authorized circle | Visual texture and related stories | Why each item belongs and which date is being used |
| Map with time controls | Place-based collections with adequate data | Location and change across a period | Historical boundaries, modern locations and uncertain geography are labelled |

A proposed initial visual set is a focused connection map, a parallel timeline and a connected story-path view, with gallery and list alternatives. Family-tree specialization and the map can be phased according to the selected launch collection. The full catalogue remains proposed. For the bounded first implementation, [documents 16](16-discovery-pilot-prd.md) and [20](20-implementation-plan.md) select the M0 views. A single event-sequence model can supply the vertical story path and parallel role-aware lanes; this does not require independent storytelling engines.

Chart choice must follow the meaning of the information. Decorative pie, chord or flow charts should not imply measured proportions, counts or flows that the dataset does not contain. A large network can be summarized before expansion.

## Choosing a default

The entry point can suggest a useful view: an era may open a timeline, a family a tree, a story a guided path, and an image collection a gallery. A user's saved view can override the suggestion.

The system should explain the current focus and let the user switch. It should not silently change the subject, time window or story edition when changing visualization. Not every view must be available for every dataset; an unavailable view should have a clear reason or a sensible alternative.

## Entity overview

| Element | Purpose |
|---|---|
| Name, type and disambiguation | Identify the subject without demanding ontology knowledge |
| Appropriate visual | Give a recognizable entry; origin and reconstruction status are clear |
| Short contextual introduction | Explain the subject in accessible language |
| Essential facts | Show a small relevant selection for the entity type |
| Highlight connections | Offer labelled next steps, such as a shared event or a past role |
| View controls | Open the applicable relationship, timeline, story or gallery view |
| Further detail | Reveal longer explanation and complete data |
| Source and correction actions | Keep provenance and issue reporting reachable |

The first facts for a team may concern a selected season; a company's overview may highlight its formation and change. A species or a literary work should not inherit person-only fields. Contextual summaries need their own editorial review and must remain traceable to underlying claims.

## Following threads

A thread is a visitor-understandable route through linked entities, events, stories or media. It may be editorially curated or derived from explicit relationships.

Illustrative routes:

- An image of a historic place → the place → an event there → a participating person → a related story.
- A team season → a player → a later team → a match or media collection.
- A company → a product → a designer or founder → another relevant organization.
- A family photograph → an approved person tag → a personal event → other authorized photographs.
- A character → a relationship known at the current reading position → a scene in the selected edition.

Each step must have a reason. Suggested labels include “Appears in this story,” “Worked here during this period,” “Shared this event” and “More images of this place.” Layout proximity, popularity or visual similarity alone must not create factual relationships.

The trail records where the visitor came from. Back navigation should return to the earlier focus, scroll position and filters. An editorial thread can have a clear ending and optional branches.

## Context retained across views

Proposed shared view state includes focus entities, world or edition, selected period or time frame, relationship filters, gallery inclusion rule, sorting, spoiler position, audience context and navigation trail.

A switch from an entity's chart to its gallery should reuse the compatible selections. If a filter cannot carry over, explain the change rather than pretending that the resulting collection is identical. A calendar comparison and a purely ordinal story view cannot share a real-time axis without a valid mapping.

Public share links must contain only publishable state. Private account or relationship identifiers must not leak through URLs, previews or saved public collections.

## Search and discovery

Search remains useful, but it is one entry path. A visitor can also choose a theme, an image, a short narrative, a period or a connected subject.

Search should support names, aliases, past names, languages, types and context. Result cards provide enough explanation to distinguish namesakes. Advanced temporal or source filters belong in optional detail; a first-time visitor should not confront a query builder.

A future natural-language interface could translate a question into visible, editable filters. It is not a launch requirement and must not invent relationships or replace the published information with unsupported generated answers.

## Relationship-map behavior

Begin around a useful focus and expand on request. Label connections, preserve stable identity and provide a readable relationship list. Selecting an edge can first show a short explanation, with deeper time and source details available.

Dense clusters can be summarized with meaningful groups. A group count must reflect visible, authorized data. An indication that private nodes exist may itself disclose information and is not an acceptable shortcut.

The shortest path is not automatically the most important relationship. An inferred path must explain its constituent edges and must not cross restricted information.

## Timeline and comparison behavior

Comparisons distinguish actual calendar time, age since birth or formation, narrative order and other relative frames. A character's first-to-last appearance bar is not automatically a lifespan or continuous presence. An event association can describe an actor, someone only mentioned, a setting or an object. Parallel lanes must preserve these roles and must not portray every referenced character as physically present.

Compatible lanes share a time scale and coordinated navigation. Unknown dates remain unknown; approximate dates retain their uncertainty. Gaps, compressed scales or nonlinear views require clear labels.

On a phone, a vertical sequence or selected-lane view may be more useful than shrinking a desktop chart. Keep the comparison's meaning and offer access to the full selection without requiring precision dragging. Text direction and chronology direction are separate choices: an RTL interface must label the axis and preserve a consistent earlier/later convention rather than mechanically mirroring dates, arrows or relationship meaning. Mixed-script names and source identifiers remain readable.

## Galleries as connected experiences

Galleries are primary visual entry points. They can show images, videos or both and can be reached from multiple contexts. Public historical galleries and restricted family galleries use common media capabilities with different access rules.

The gallery should state its organizing rule: newly shared, captured in a period, depicting a period, associated with an entity, or curated for a story. A modern depiction of an ancient event can belong to a historical gallery with an explicit origin label.

The viewer provides a larger image or appropriate video controls, a concise caption, relevant dates, permitted entity links and optional source detail. It also preserves a route back to the gallery and onward to another thread. The full specification is in [document 15](15-media-and-galleries.md).

## Guided stories and spoiler control

A guided story combines selected highlights, media and context into a coherent route. It is a presentation sequence over shared content. Removing an event from one guided story does not remove the underlying event.

For fiction, spoiler position affects headings, summaries, images, relationships, recommendation text and revealing counts. Not knowing a relationship at a reading position is different from asserting that it never exists.

Historical interpretations and different editions remain distinguishable. The visitor can enter deeper comparison without being forced to understand every scholarly distinction at first glance.

## Maps, saving and sharing

Maps are useful when place information supports them. Modern locations, historical names, political territories and contested boundaries must not be conflated. A complete historical atlas is not an implicit launch obligation.

Saved views can preserve a visitor's focus and selections. A private saved trail, a public collection and a fixed-version citation are different objects. Sharing a collection does not widen the permissions of contained content.

## Accessible, understandable engagement

Use text labels alongside visual encoding, readable contrast, keyboard navigation and reduced-motion behavior. Avoid automatically playing audible video as an entry requirement. Provide a meaningful list or narrative alternative.

Recommended evaluation asks whether visitors understood a connection, found something interesting, could recover their place and could tell what was uncertain. Session length alone is insufficient evidence of success.

## Design checks

Evaluate an unfamiliar visitor entering from an image; a person switching chart and gallery; a fan exploring a season; a reader using spoiler control; and a family member opening a newly shared old photograph. Check the mobile and non-graphical paths alongside the desktop visual experience. See SC-15 through SC-19 in [document 14](14-reference-scenarios.md).
