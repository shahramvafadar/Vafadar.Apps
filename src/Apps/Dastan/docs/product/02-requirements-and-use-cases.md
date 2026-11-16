# 02 — Requirements and Use Cases

**Version:** 0.4  
**Updated:** 2026-11-14

This document preserves the requirements accumulated in the conversation and adds the latest audience, discovery and gallery direction. **Accepted** describes a user requirement or an accepted product principle. **Proposed** describes implementation detail or a release choice. Acceptance examples are proposed checks, not evidence of completed implementation.

Version 0.4 contains **65 requirements**. Existing IDs remain stable. The initial audience is the curious general public; deeper investigation is available as a secondary use. Release priorities are maintained in [document 10](10-roadmap-and-backlog.md).

## Public access and connected knowledge

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-PUB-01 | Most published knowledge is publicly accessible. | Accepted | Proposed access design: a public link opens without creating an account or family. |
| R-PUB-02 | Support multiple entity types, including companies and teams. | Accepted | Each has appropriate information and connections rather than person-only fields. |
| R-PUB-03 | Show relationships, events and changes over time. | Accepted | Selecting a past period shows the roles relevant to that period. |
| R-PUB-04 | Find information by subject, time and place. | Accepted | A visitor can narrow a collection and reach a relevant entity. |
| R-PUB-05 | Compare multiple entities or timelines. | Accepted | Compatible time frames share an axis; unrelated frames do not imply real simultaneity. |
| R-PUB-06 | Help users discover further information and relationships. | Accepted | A next-thread suggestion explains its connection in ordinary language. |
| R-PUB-07 | Provide stable links for entities, statements and saved views. | Proposed | A reference survives a name change; live and version-specific links are distinguishable. |
| R-PUB-08 | Offer readable alternatives to visual views. | Proposed | Essential information is usable through a list without manipulating a graph. |
| R-PUB-09 | Explain material limits in coverage. | Proposed | An empty result is not presented as proof that something never existed. |

## Entities, identity and relationships

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-DOM-01 | Keep a person record independent from an account. | Accepted | Deceased, unregistered and historical people can have records. |
| R-DOM-02 | Support names, nicknames or titles, birth information, photos and further appropriate attributes. | Accepted | Unknown information is not replaced with fabricated required values. |
| R-DOM-03 | Support aliases, languages and historical names. | Proposed | A previous company name resolves to the appropriate identity. |
| R-DOM-04 | Allow multiple relationships and roles between the same entities. | Accepted | Kinship, employment and friendship remain distinguishable. |
| R-DOM-05 | Represent membership, office and organizational affiliation over time. | Proposed detail for an accepted domain | A current coach does not overwrite a former season's coach. |
| R-DOM-06 | Avoid automatic identity merges based on matching names or contact details. | Accepted | A merge requires review and can be corrected. |
| R-DOM-07 | Link a shared event to multiple participants and places. | Accepted | Related timelines refer to the same event without divergent copies. |
| R-DOM-08 | Preserve claims, evidence, context and competing accounts. | Accepted | Conflicting dates do not become an invented average date. |
| R-DOM-09 | Use governed definitions for types and relationships. | Proposed | Acquisition, merger and renaming are separate meanings. |

## Time and chronology

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-TIME-01 | Support exact, partial, approximate and unknown dates. | Accepted | A known birth year remains a year, not a fabricated exact day. |
| R-TIME-02 | Distinguish an uncertain occurrence window from an actual duration. | Accepted | “Occurred within” and “continued throughout” have different meanings and visuals. |
| R-TIME-03 | Allow uncertain, open and unknown boundaries. | Accepted | Unknown end does not automatically mean still ongoing. |
| R-TIME-04 | Support BCE and very distant times. | Accepted | Examples at 500 BCE and 50–60 million years ago preserve their meaning. |
| R-TIME-05 | Preserve calendar, epoch, unit and source expression. | Accepted | Conversion retains the original calendar and precision. |
| R-TIME-06 | Support fictional, relative and ordinal time. | Accepted | A story without calendar dates can be shown without invented years. |
| R-TIME-07 | Distinguish occurrence, publication and recording times. | Accepted | An old memory appears at its relevant time while its new publication is also identifiable. |
| R-TIME-08 | Distinguish definite overlap under the data assumptions, possible overlap and insufficient information. | Accepted | Ambiguous intervals do not create a definite contemporaneity claim. |

## Stories and narrative context

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-STORY-01 | Support works, worlds, characters, relations and story events. | Accepted | A Shahnameh character has connected relationships and story events. |
| R-STORY-02 | Identify editions, adaptations and source anchors. | Accepted | Different versions are not silently combined. |
| R-STORY-03 | Separate reading order from in-world event order. | Accepted | A flashback stays late in the reading sequence and early in chronology. |
| R-STORY-04 | Support spoiler-aware exploration. | Accepted concept; detailed behavior proposed | The reading position affects summaries, edges, suggestions and previews. |
| R-STORY-05 | Support guided narratives using text, visuals and linked events. | Accepted concept; release timing open | A visitor can follow a short story or branch into free exploration. |

## Personal, family and social purposes

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-PER-01 | Record ancestors and descendants. | Accepted | A focused tree supports an appropriate generation range. |
| R-PER-02 | Distinguish complex kinship, adoption, guardianship and multiple marriages. | Accepted | Family information is not forced into a single simple tree. |
| R-PER-03 | Establish shared social relationships through request, notification and acceptance between accounts. | Accepted | An unaccepted request is not a visible shared relationship. |
| R-PER-04 | Invite a person without an account by email or a shareable link. | Accepted | Membership alone accepts neither identity claims nor the proposed relationship. |
| R-PER-05 | Separate audience circles from relationship types and display filters. | Accepted | Hiding a category in a chart does not change its access policy. |
| R-PER-06 | Record education, work and residence in a life timeline. | Accepted | An attendance period links to the correct institution or place. |
| R-PER-07 | Make personal-account discovery through organizations opt-in. | Accepted | Restricted accounts do not appear through counts or suggestions; documented public roles follow public-content policy. |
| R-PER-08 | Support posts, photos and approved person tags. | Accepted; delivery phased | Approving a tag does not broaden the media audience. |
| R-PER-09 | Let a user withdraw display consent for their account's social relationship or tag. | Accepted | Withdrawal needs no counterparty approval; public factual correction has a separate review path. |

## Content, platform and quality

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-GOV-01 | Retain sources and meaningful revision history for public information. | Accepted | A visitor can inspect a claim's origin when needed. |
| R-GOV-02 | Separate drafts from published content. | Proposed | Importing data is not equivalent to publishing it. |
| R-GOV-03 | Support issue reporting, disputed claims and correction. | Proposed | A corrected statement updates dependent live views. |
| R-GOV-04 | Record media origin, attribution and usage conditions. | Proposed | A link to an image is not treated as permission to republish it. |
| R-PLAT-01 | Build within Vafadar.Apps and deliver an installable PWA. | Accepted | Installation is checked on the chosen phone and Windows/browser matrix. |
| R-PLAT-02 | Plan for Persian/RTL and a multilingual path. | Proposed | Mixed-language names, numbers, sources and time axes remain readable. |
| R-PLAT-03 | Prevent private data leakage through queries, graphs, caches and outputs. | Accepted principle; implementation proposed | A restricted relationship cannot be inferred from a path or count returned to an unauthorized visitor. |
| R-PLAT-04 | Make public pages referenceable and discoverable. | Proposed | Useful text and media descriptions do not depend on successfully loading a graph. |
| R-PLAT-05 | Include accessibility, keyboard access and mobile usability. | Proposed | Core actions have a usable alternative to dense visual manipulation. |
| R-PLAT-06 | Decide offline capability separately from PWA installation. | Proposed | Users can tell what is stored and when it was refreshed. |
| R-PLAT-07 | Maintain all project documentation in English. | Accepted in v0.4 | Current Markdown, the reading edition, decisions and new specifications are English; UI language remains a separate choice. |

## General-user visual discovery

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-EXP-01 | Treat curious general users as the primary audience and provide a useful first glimpse. | Accepted in v0.4 | A visitor understands a focus without first defining a research query. |
| R-EXP-02 | Reveal essential information first and optional depth progressively. | Accepted in v0.4 | Overview, context and detailed/source views are reachable without an overwhelming initial panel. |
| R-EXP-03 | Make several meaningful chart or visual-tool views normal entry experiences. | Accepted direction; exact set proposed | A relationship map, comparative timeline and story-path view have defined default use cases. |
| R-EXP-04 | Let visitors follow meaningful, explainable threads across connected content. | Accepted direction; recommendation rules proposed | “Shared event” is explained; mere proximity is not labelled friendship or causation. |
| R-EXP-05 | Preserve relevant focus and context across views and return navigation. | Proposed support for the accepted experience | Entity, period, world, filters and reading position survive a compatible chart-to-gallery switch. |
| R-EXP-06 | Provide discovery entry points beyond searching for a known name. | Accepted direction; presentation proposed | A curated theme, image, short story or gallery starts a useful trail. |

## Images, videos and galleries

| ID | Requirement | Status | Acceptance example |
|---|---|---|---|
| R-MED-01 | Treat images and videos as first-class connected media. | Accepted in v0.4 | Both can open in an appropriate viewer and link to relevant entities and stories. |
| R-MED-02 | Reach contextual galleries from entities, periods, places, events and stories. | Accepted direction; individual entry points proposed | A selected period has a relevant gallery without requiring a separate asset copy. |
| R-MED-03 | Provide a gallery of recently shared, authorized family and friend media. | Accepted in v0.4 | An old photograph shared today can appear among recent shares only for its permitted audience. |
| R-MED-04 | Distinguish capture/creation, depicted time, upload, publication and share time. | Proposed semantic rule | A modern painting of an ancient event is not presented as a photograph captured in that ancient period. |
| R-MED-05 | Enforce the same media access policy across every entry route and derivative. | Accepted privacy principle; details proposed | Posters, thumbnails, captions, counts, recommendations and downloads do not bypass restrictions. |
| R-MED-06 | Connect the media viewer back to its context and onward to relevant subjects. | Proposed support for the accepted experience | A visitor can move from a video to its place and return to the same gallery position. |
| R-MED-07 | Support curated albums and dynamic galleries over shared assets, with clear grouping and deduplication. | Proposed | The same permitted asset can appear in several relevant contexts without duplicated identity or contradictory captions. |
| R-MED-08 | Provide accessible media presentation and visible attribution, rights and provenance. | Proposed | Images have useful descriptions; video captions and origin labels are available where applicable. |

## Example journeys

These are product questions and synthetic scenarios, not assertions about real people or datasets:

1. Start with an interesting historical image, identify what it depicts, and follow a related person.
2. Browse a team's season and discover a player who also appeared in another team.
3. See how a company's name, products, managers and ownership changed.
4. Compare three compatible historical periods and open a shared event.
5. Follow a Shahnameh character without revealing information beyond the reading position.
6. Browse a friend's newly shared album, including photographs taken decades earlier.
7. Switch from a relationship map to a gallery while retaining the selected entity and period.
8. Open additional explanation or evidence when a short summary raises a question.

## Interpretation rules

No result does not prove absence. Nearness in a layout does not prove a strong relationship. Simultaneity does not prove causation. Shared institutional attendance does not establish friendship. A recent share does not imply recent capture, and a work depicting an era was not necessarily created in that era.

[Reference scenarios](14-reference-scenarios.md) define concrete inputs and expected behavior. [Media and galleries](15-media-and-galleries.md) expands the media requirements. These IDs should remain traceable through design and implementation.
