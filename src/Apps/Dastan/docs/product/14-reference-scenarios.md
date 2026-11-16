# 14 — Reference Scenarios and Acceptance Criteria

**Version:** 0.4  
**Date:** 2026-11-14  
**Status:** Scenarios for future design and verification. None is a completed software test.

All example people, organizations, dates, and events below are synthetic unless a work title such as Shahnameh is used solely to identify the intended use case. The examples are not publication-ready historical data. Calendar conventions and time-model rules are addressed in the dedicated time document.

The principal reader is a curious member of the general public. They can begin with a topic, chart, media item, or story thread, understand the essentials, and open more detail when interested. Detailed source inspection remains possible without becoming a prerequisite for enjoying the first view. The desired directions are accepted requirements; the detailed outcomes below are proposed acceptance criteria unless a referenced requirement states otherwise.

Identifiers `SC-01` through `SC-14` retain their previous meanings. `SC-15` through `SC-19` cover the latest discovery and media requirements.

## SC-01 — Company change and continuing identity

**Input:** The fictional company Azar was founded in 2000, renamed AzarNov in 2009, and acquired through a share purchase by the fictional company Bahar in 2012. A sample source says AzarNov continued as a separate entity after acquisition. Person A held a management role from 2004 to 2010.

**Reader task:** Search the former name and compare the company in 2008 and 2014.

**Expected:** Dated names lead to the appropriate identity. Acquisition does not automatically mean merger or termination. The former role remains visible in its period; the 2008 view does not show the later owner or manager. The acquisition event is distinguishable from the ownership relationship that follows it. The first explanation is brief, with the sample evidence available on demand.

**Related requirements:** `R-PUB-02`, `R-PUB-03`, `R-DOM-03` through `R-DOM-06`.

## SC-02 — Club, team, season, and role

**Input:** The fictional Sepid Club has senior and youth teams. Player B belonged to the senior team in the 2020–2021 season. Person C was a player and became a coach years later. A sample trophy belongs to a specific team and competition.

**Reader task:** Explore a season's players and coaches, then compare another season.

**Expected:** Club, team, and season are distinct. Youth-team membership is not transferred to the senior team. The person's two roles retain their separate dates. A championship is not an unqualified property of every club member. A name on a single page does not prove membership for an entire season; the evidence's limits remain visible.

**Related requirements:** `R-DOM-04`, `R-DOM-05`, `R-DOM-07`, `R-GOV-01`.

## SC-03 — Story order and spoiler protection

**Input:** A sample collection uses a specified edition of a work such as Shahnameh. Event A is narrated in section five but happened before Event B in section three within the story world. A family relationship is revealed to the reader only in section eight.

**Reader task:** Having reached section four, explore a character's connections.

**Expected:** Reading order is not substituted for event order. The later revelation is hidden from titles, text, graph labels, suggestions, thumbnails, gallery captions, and video posters while the spoiler limit applies. Removing the limit reveals the statement with its edition and evidence. A missing calendar date is not filled with an invented BCE year. A proposed historical mapping remains a separately labelled, sourced interpretation.

**Related requirements:** `R-STORY-01` through `R-STORY-05`, `R-TIME-06`; media presentation also follows `R-MED-05` and `R-MED-08`.

## SC-04 — Comparing three historical tracks

**Input:** Three synthetic collections describe governments in different regions using comparable calendar frames. Some periods overlap, and one shared event involves several collections.

**Reader task:** Put three tracks on one axis, narrow the period, and open an event.

**Expected:** Bar lengths have consistent meaning. A person's lifespan, reign, and dynasty duration are not conflated. A shared event is one event with multiple links. Zoom remains synchronized. A narrative track without a valid calendar mapping cannot imply historical simultaneity: it needs a separate axis or an explicit, justified mapping.

**Related requirements:** `R-PUB-05`, `R-DOM-07`, `R-TIME-05`, `R-TIME-06`.

## SC-05 — An event at an uncertain time

**Input:** A sample source places an event somewhere between 20,000 and 10,000 years before a specified fixed reference point.

**Reader task:** View the event and search a smaller interval inside that range.

**Expected:** The event is not represented as lasting 10,000 years. Its presence within the smaller interval is possible rather than certain. If a midpoint is used for layout, it is not presented as the actual date or the source's best estimate. The source expression and meaning of the range can be opened.

**Related requirements:** `R-TIME-01`, `R-TIME-02`, `R-TIME-08`.

## SC-06 — A duration with uncertain boundaries

**Input:** A synthetic continuous period began between 20,000 and 18,000 years ago and ended between 12,000 and 10,000 years ago, using one fixed reference and an explicit continuity assumption.

**Reader task:** Compare it with another period.

**Expected:** Each uncertain boundary is represented independently. The portion included in every admissible continuous interval is distinguished from the possibly included portion. The result remains conditional on the continuity assumption and source data. If evidence later introduces a break, the display does not retain a falsely certain continuous bar. An unknown end is distinct from a period known to be ongoing.

**Related requirements:** `R-TIME-02`, `R-TIME-03`, `R-TIME-08`.

## SC-07 — Fifty to sixty million years ago

**Input:** Synthetic data describes a natural phenomenon with an age or period between 50 and 60 million years, using a specified unit and source convention.

**Contributor task:** Indicate whether this is one event somewhere in that range or a period continuing from 60 to 50 million years ago.

**Expected:** A simple form captures the semantic distinction without requiring a day or month. A large value is not forced into an out-of-range everyday date type. The data does not move each year because “today” silently changes. A compressed or logarithmic display, if offered, is clearly labelled and uses compatible scales in comparisons.

**Related requirements:** `R-TIME-02`, `R-TIME-04`, `R-TIME-05`.

## SC-08 — BCE, year precision, and an unknown end

**Input:** Synthetic events are recorded in 500 BCE, 1 BCE, and 1 CE. Only a birth year is known for one person. An organization has a known beginning and an unknown end.

**Reader task:** Inspect ordering and elapsed time, then search for organizations active in a selected year.

**Expected:** The conventional historical display does not invent a year zero; internal and displayed conventions convert consistently. A birth year does not become a certain first day of the first month. An unknown end does not establish that the organization remains active today. Converting a Solar Hijri or other calendar expression preserves its original form and precision.

**Related requirements:** `R-TIME-01`, `R-TIME-03` through `R-TIME-05`.

## SC-09 — Invitation, registration, and identity

**Input:** Account A creates a private record for a person without an account and generates an invitation link. The link reaches another person as well. A separate same-name record exists.

**Recipient task:** Register, then decide about the identity link and proposed relationship.

**Expected:** Possessing the link does not prove identity. A shared name or email does not trigger automatic merging. Registration can succeed even when the relationship is rejected. An invitation does not expose private family information before authorization. Invitation state, identity claim, and relationship request are independently traceable.

**Related requirements:** `R-DOM-01`, `R-DOM-06`, `R-PER-03`, `R-PER-04`.

## SC-10 — Mutual friendship and withdrawal of consent

**Input:** Two accounts exist. A requests “close friend”; B has not accepted. After acceptance, each selects a different permitted audience.

**Viewer task:** Look for the relationship on either profile or a shared graph.

**Expected:** No shared relationship is displayed to third parties before acceptance. Afterwards, a viewer needs permission from both sides. Changing the relationship to “best friend” needs fresh acceptance. Either party can stop its display. The relationship never automatically grants access to every photo or friend. This rule does not grant unilateral deletion of a sourced public statement about an organizational role.

**Related requirements:** `R-PER-03`, `R-PER-05`, `R-PER-09`.

## SC-11 — An old photograph and an approved tag

**Input:** A synthetic photograph taken in Solar Hijri year 1380 is uploaded in 1405. Its audience is the owner's selected circle. One depicted person approves their tag.

**Viewer task:** Open the image from the tagged person's profile.

**Expected:** The memory belongs to 1380 while publication time remains distinguishable. A viewer outside the permitted photo audience receives neither the original, thumbnail, nor revealing metadata. Tag approval does not expand that audience. Removing the tag, requesting media removal, and deleting the file are distinct actions with different consequences.

**Related requirements:** `R-TIME-07`, `R-PER-08`, `R-PLAT-03`, `R-MED-04`, `R-MED-05`.

## SC-12 — University discovery with consent

**Input:** A attended a fictional university in Solar Hijri years 1380–1384, B in 1382–1386, and C from 1380 to an unknown end. Only A and B allow personal-account discovery through that affiliation. A's graduation is recorded separately.

**User task:** Find attendees in 1383, entrants in 1380, and graduates in a selected year.

**Expected:** These questions have different meanings and can return different results. C appears in neither personal-account results nor counts. Shared attendance does not create friendship or classmate status. Ambiguous dates produce an appropriately labelled possible match. Publicly documented roles remain subject to public-content rules without exposing C's private account.

**Related requirements:** `R-PER-06`, `R-PER-07`, `R-TIME-08`.

## SC-13 — Preventing indirect disclosure of private relationships

**Input:** Two public entities connect through a private person or relationship. The viewer lacks access to the private portion. A public view and comparison export can be shared.

**Viewer task:** Explore paths, neighbor counts, search, and exports.

**Expected:** Hidden paths, names, identifiers, revealing counts, previews, and suggestions are not disclosed. Client-side hiding does not substitute for authorization. Revocation updates subsequent online responses and controlled caches according to the defined policy; previously downloaded files and offline devices are not claimed to be instantly recoverable. Publishing a collection does not publish private items within it.

**Related requirements:** `R-PLAT-03`, `R-PLAT-06`, `R-MED-05`; [06 — Privacy](06-private-spaces-consent-and-privacy.md).

## SC-14 — Correction, conflicting sources, and referenceable versions

**Input:** A statement supported by sample Source A appears in several stories and timelines. Source B gives a different date. Later, one value is found to have been extracted incorrectly.

**Contributor and reader task:** Propose a correction; optionally open the detail behind a version someone previously shared.

**Expected:** An unreviewed proposal does not silently replace the public version. A genuine source disagreement differs from extraction error. Corrections reach live dependent views. The reference version has a clear current status or correction notice, unless publication restrictions prevent keeping it public. History is not used to restore deleted private material. Popularity does not imply greater evidential confidence.

**Related requirements:** `R-DOM-08`, `R-GOV-01` through `R-GOV-04`, `R-PUB-07`.

## SC-15 — Casual discovery through a connected story thread

**Input:** A synthetic topic card introduces a character, an event, and a connected place. Each has a short overview and optional detail. The connections have defined meanings and a selected story edition where needed.

**Visitor task:** Begin without knowing a name to search, understand the topic, follow an interesting connection, and return to the original thread.

**Expected:** The first glance gives essential information without a long mandatory article or source checklist. A visual relationship or story path offers understandable next steps with a reason for each connection. Opening more detail reveals context, uncertainty, and sources without changing the meaning of the initial overview. Returning restores the previous focus and position. Engagement does not depend on inventing dramatic connections or overstating certainty.

**Related requirements:** `R-EXP-01`, `R-EXP-02`, `R-EXP-04`, `R-EXP-06`.

## SC-16 — Switch between graph, timeline, and gallery

**Input:** The visitor has selected two entities, a period, a story context, and filters in a graph. Authorized images and videos relate to those same selections.

**Visitor task:** Switch to a parallel timeline, open the related gallery, inspect a media item, follow its entity link, and return.

**Expected:** Relevant entity focus, time frame, narrative context, filters, and back-navigation state survive the changes. The gallery states which time basis it uses. Switching views neither resets the task nor grants additional access. If a view cannot represent the current frame, it explains the limitation and offers a suitable alternative without silently altering the data. The exact set of launch views remains proposed.

**Related requirements:** `R-EXP-03`, `R-EXP-05`, `R-MED-02`, `R-MED-06`.

## SC-17 — Recent authorized family or friend media

**Input:** A family member newly shares an old photograph and a recent video with a permitted circle. Another friend shares a private image with a different audience. One source asset is also placed in a family album; an anniversary later resurfaces it.

**User task:** Open recent media shared by friends or family, then inspect the old photograph in its life-timeline context.

**Expected:** The gallery contains only shares the viewer may access. Recent ordering uses the qualifying share time, while capture, creation, and depicted dates remain visible where relevant. Friendship alone does not grant access. The same share is not duplicated because it has several tags or gallery routes. A resurfaced memory is labelled as resurfaced, not newly captured or newly shared. A separately authorized new share remains a distinct social action without duplicating the underlying asset. Hidden shares do not affect visible counts or recommendations.

**Related requirements:** `R-MED-01`, `R-MED-03`, `R-MED-04`, `R-MED-05`.

## SC-18 — A historical gallery with modern depictions

**Input:** A synthetic period gallery includes a photograph of a historical object, a modern illustration depicting an ancient event, and a modern documentary clip about that event. Their capture, creation, depicted time, sources, and permissions differ.

**Visitor task:** Browse media about the ancient period, then change the filter to media created during a selected modern period.

**Expected:** “About this period” uses depicted or contextual time, while “created during this period” uses creation time. The modern illustration and documentary are labelled as modern depictions, not images captured during the ancient event. A photograph's date is distinct from the age of the object it depicts. Uncertain and fictional contexts retain their labels. Multiple contextual placements use the same source asset without losing per-placement captions or source information. Credits and accessibility descriptions remain available.

**Related requirements:** `R-MED-02`, `R-MED-04`, `R-MED-07`, `R-MED-08`.

## SC-19 — Video deep link, context, and revoked access

**Input:** An authorized viewer opens a video's timestamped link from an event gallery. The video has a poster, subtitle track, contextual entity links, and a saved origin. Access is later revoked, or the link is opened by another person without permission.

**Viewer task:** Play the referenced segment, inspect a connected entity, return to the video, and reopen the link after the permission change.

**Expected:** The first authorized visit preserves the timestamp and event context. Returning restores the meaningful viewer state. Every route checks current permission before exposing the poster, subtitle, transcript, metadata, recommendation, download, or video content. A deep link is not an access grant. Unauthorized responses do not reveal a private title or relationship. Subsequent online access stops after revocation according to the defined policy, while no claim is made that exported, previously buffered, or offline copies can always be recalled. Public credits and private captions are not mixed across placements.

**Related requirements:** `R-MED-05`, `R-MED-06`, `R-MED-08`.

## How to use these scenarios

During design, identify the path and screens for selected scenarios. For technology decisions, test only the scenarios relevant to the uncertainty being investigated. During implementation, turn rules with a material semantic or access risk into suitable tests. Record a pass or failure only after executing an identifiable version of a prototype or product.

The scenarios do not silently choose an MVP, require every feature at launch, or claim a completed build. The roadmap and decision register own those choices. The public, private, historical, fictional, company, team, and media cases exercise one reusable conceptual core rather than separate incompatible products.
