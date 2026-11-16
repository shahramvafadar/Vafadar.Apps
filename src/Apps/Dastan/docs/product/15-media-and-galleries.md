# 15 — Media and Galleries

**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Feature specification and implementation input. User directions are Accepted; detailed behavior is Proposed. Local sample media exists; product media delivery and provider integration have not been completed.

## 1. Product purpose and accepted direction

Images and videos are part of how ordinary curious visitors discover and enjoy the product. A visitor can enter through an entity, historical period, place, event, story, or a media item itself. An account holder can also browse recent photos and videos that friends or family have authorized them to see. A useful first glimpse leads naturally to related people, objects, organizations, periods, and stories, with detail available when wanted.

The feature uses the same entities, relationships, time expressions, sources, audience rules, and narrative contexts as charts and timelines. It does not create a separate media-only identity or permission system.

### Accepted user requirements

- Images and videos are first-class content, not merely decorations beside an article (`R-MED-01`).
- Contextual galleries can be reached from entities, periods, places, events, and stories (`R-MED-02`).
- Recent authorized friend/family shares provide a personal gallery entry point (`R-MED-03`).
- Meaningful default charts anchor the main visual experience, accompanied by a concise essential overview and optional deeper context. Galleries also provide direct entry points into connected topics (`R-EXP-01` through `R-EXP-04` and `R-EXP-06`; precise defaults remain proposed).
- Existing tag-consent and audience restrictions remain applicable across every presentation.

Retaining navigation context and providing related-entity back links are proposed supporting behavior (`R-EXP-05` and `R-MED-06`). The detailed date fields, gallery ordering, recurrence controls, deduplication rules, player behavior, storage, delivery provider, and exact launch scope below are proposed designs. English is the documentation language; this document does not choose the final interface languages.

## 2. Shared media concepts

**MED-001 — Accepted direction:** The common media experience supports both still images and videos. **Proposed detail:** an “all / images / videos” filter can narrow a mixed gallery without changing its entity, time, narrative, or audience context.

**MED-002 — Proposed:** Separate the underlying asset from where and how it appears.

| Concept | Purpose | Boundary |
|---|---|---|
| Source asset | The identifiable original file or external media reference, with provenance and rights information. | It is not an album, permission grant, or claim that every caption is accurate. |
| Rendition | A thumbnail, video poster, playable version, or other derivative associated with an asset. | Its access and provenance follow the relevant asset and context. |
| Contextual placement | A relationship that places media in an entity, event, period, place, or story context. | Its caption, depicted subject, source, and visibility can be context-specific. |
| Publication or share | A recorded action making an item available to a selected audience at a particular time. | It is distinct from capture, upload, and a social friendship relationship. |
| Dynamic gallery | A current result set determined by context, filters, ordering, and the viewer's permission. | Saving the query does not freeze or enlarge access. |
| Curated album | An intentionally selected sequence of placements with a title and explanation. | Album visibility does not override any item's permission or reuse conditions. |

This permits one asset to appear in several authorized contexts without making duplicate uploads or inconsistent copies of its original metadata. Context-specific annotations remain distinguishable.

## 3. Entry points and intended experience

**MED-003 — Accepted direction; proposed defaults:** Provide multiple meaningful gallery entry points. Each explains why its media is included.

| Entry point | Relevant content | Proposed ordering or context |
|---|---|---|
| Entity | Media depicting, created by, documenting, or otherwise relating to a person, company, team, character, object, or other entity. | Group by relationship or meaningful time; explain the relationship. |
| Historical period | Media about the selected period, or media created during it, depending on the chosen mode. | Explicit time basis; chronological or curated order. |
| Event | Photographs, documents, illustrations, or clips connected to the same event. | Event sequence or an explained editorial order. |
| Place | Media showing the place or connected events across time. | Period groups or a then-and-now comparison where the evidence supports it. |
| Story or chapter | Media connected to a selected work, edition, scene, or reading position. | Narrative order and the current spoiler limit. |
| Recent friend/family shares | Share actions currently visible to the signed-in viewer. | Qualifying share time, with original media dates retained. |
| Discovery theme or media card | A selected topic or appealing media item that opens a connected thread. | Curated relevance with an understandable reason for the next connection. |

A gallery can be useful without a known-name search. Empty or unavailable results explain the permitted context without disclosing how many hidden items or people exist.

## 4. Moving between media and other views

**MED-004 — Proposed support for the accepted connected experience:** Opening media retains the origin: selected entities, time range and time basis, narrative world/edition, spoiler position, filters, ordering, and an appropriate return position. A viewer presents a short caption and a few meaningful related-entity links before optional full context, provenance, or source detail.

A visitor can follow “depicts this character,” “from this event,” or “created by this artist,” inspect the related entity, and return without reconstructing the earlier gallery. The reason for the link is visible; a shared place or period is not presented as proof of personal acquaintance or causation.

**MED-005 — Proposed:** Switching among graph, timeline, and gallery preserves compatible context. When a view cannot represent a selected frame, explain the limitation and offer an appropriate view. Do not silently replace a fictional timeline with a historical one, reset the period, or treat a media upload date as the selected event date. Navigation state is not an access token and is rechecked when opened by another viewer.

## 5. Dates and the meaning of chronology

**MED-006 — Proposed:** Keep the following dates separate when applicable. Unknown values remain unknown; not every field is required.

| Date | Meaning | Example distinction |
|---|---|---|
| Capture/recording time | When a photograph or recording was made. | A photo of an ancient object taken today. |
| Creation time | When an illustration, edited video, artwork, or other asset was produced. | A modern illustration of an ancient battle. |
| Depicted time | The time of the subject or event portrayed. | A modern documentary depicting an older period. |
| Upload time | When the file entered this system. | An old school photograph uploaded years later. |
| Publication time | When an approved item was first made available in a particular publication context. | A draft made public after review. |
| Share time | When an item was shared with a particular audience. | An old public or private item newly shared with an authorized family circle. |
| Last modification | When metadata, captions, or another permitted field changed. | Correcting a spelling without creating a new memory or share. |

Temporal meaning uses the common time model, including partial dates, uncertain ranges, relative or narrative time, and source conventions. Source and extracted metadata can disagree; camera metadata is evidence to review, not an automatic replacement for a contributor's supported date.

**MED-007 — Proposed:** Every time-dependent gallery identifies its basis. “About this period” uses depicted or contextual time. “Created during this period” uses creation time. “Recently shared with me” uses qualifying share actions. An entity gallery or event sequence need not be sorted newest first. Unknown or incompatible dates are grouped or explained rather than assigned invented chronological positions.

**MED-008 — Proposed:** Label the kind of representation. Contemporary documentary media, a later photograph of an old object, a reconstruction, an illustration, a map, and a modern documentary can all be useful, but their evidential roles differ. A gallery about an ancient period must not imply that a modern image was captured then. A fictional depiction keeps its work/edition context and does not become historical evidence through gallery placement.

## 6. Recent shares, recurrence, and deduplication

**MED-009 — Accepted direction; proposed details:** The personal recent-shares entry point contains only qualifying shares the viewer may currently access. It may offer friend/family or selected-circle filters. Confirming a friendship does not automatically grant access to every past share. The visible date identifies the share while capture or depicted dates remain available in the caption or expanded context.

**MED-010 — Proposed:** Avoid duplicate cards for the same share merely because the asset has several tags, matches multiple selected entities, or appears in more than one album. The UI can group equivalent appearances and explain the permitted contexts. A genuinely new authorized share is a distinct action even when it references an existing asset. Deduplication must not expose that another person holds a private copy.

Crops, edits, restorations, and remixes may be separate derivatives with provenance. They must not be silently merged with the original. Suspected duplicates can be suggested for review without replacing captions, rights, or permission boundaries.

**MED-011 — Proposed:** An anniversary or “on this day” resurfacing references the original item and its dates. Label it as resurfaced rather than newly captured, uploaded, or shared. A metadata edit or new tag is not automatically a new share. Recurring occasions, such as yearly birthdays, remain distinct event occurrences. Notification frequency and opt-out controls remain open design decisions.

## 7. Permission and consent across all routes

**MED-012 — Accepted principle; proposed enforcement detail:** Apply permission before delivering or deriving thumbnails, posters, previews, gallery counts, captions, subtitles, transcripts, searchable metadata, recommendations, download links, or playable media. The same restrictions apply on entity pages, historical galleries, albums, recent shares, timelines, search results, embeds, and deep links. An album or gallery is not a route around an asset's access scope.

A public entity does not make its linked family media public. Selecting a historical period does not expose private photographs associated with that period. A private placement's caption, tags, and location are not automatically reused in a public placement. Derived counts and suggestions consider only the viewer's permitted result set.

**MED-013 — Accepted:** Tag approval connects an item to a person without expanding the media audience. Profile visibility, approved tag visibility, and permission to view the underlying media must all be satisfied in the relevant context. Removing a tag, removing one album placement, withdrawing a share, revoking asset access, and deleting an asset have different consequences that the interface must explain.

An independently authorized placement may remain visible after removal from one album. Revoking the asset's own access cannot be bypassed through another placement. A share link does not itself prove identity or confer an audience wider than the granted one. These rules inherit [06 — Privacy](06-private-spaces-consent-and-privacy.md).

## 8. Image and video viewing

**MED-014 — Proposed:** Use a consistent media viewer with an appropriate image view or video player. It should provide a readable essential caption, contextual links, creator/source credit, relevant date labels, and optional detail. Video cards identify video content and expose a permitted poster and duration when known; they do not masquerade as still photographs.

Timestamped video links can return to a relevant segment and retain the event or story context. The timestamp is checked against the available media version. Replacing a video must not silently make an old citation point to unrelated content. Returning from a related entity restores useful context and playback position where appropriate; playback does not unexpectedly restart.

Noisy autoplay is not the proposed default. Captions/subtitles, transcripts when available, alternative image descriptions, clear controls, keyboard operation, and reduced-motion preferences support access. Unavailable captions or uncertain automated transcription are identified rather than presented as complete, verified material. Exact player, subtitle formats, hosting, and embedding choices remain open.

## 9. Provenance, rights, and editorial handling

**MED-015 — Proposed:** Each asset and relevant placement preserve source, creator/provider, edition or version, attribution, permission status, permitted uses, and extraction or transformation history. Captions distinguish documented facts from interpretation. AI-generated or reconstructed depictions are labelled as such when used, and do not create evidence for the event they portray.

The owner or provider of a file, the person depicted, the author of its caption, and a person who shares it are different roles. Publication permission, person-tag consent, and permission to reuse the source are separate decisions. Unresolved reuse status prevents the proposed public republication route until reviewed; a reference link can be considered separately under the content policy.

**MED-016 — Proposed:** Contributors select the asset, its context, dates, essential caption, sources, audience, and any requested tags. Public publication follows the review process in [07 — Governance](07-content-sources-and-governance.md). The form should ask only what is needed for that case and allow progressive detail rather than requiring every metadata field at first entry. Contextual claims made by a poster, caption, or placement receive the same care as prose statements.

## 10. Downloads, revocation, and offline behavior

**MED-017 — Proposed:** Download and export eligibility considers current access, recorded source permissions, and the selected context. Exports preserve permitted credits and exclude hidden captions, tags, and sensitive embedded metadata. A public share or export of a gallery does not include inaccessible items or their counts.

**MED-018 — Proposed:** Subsequent online requests use current permission after a share or asset is revoked. Controlled thumbnails, posters, search entries, and derived views are updated according to the defined policy. Offline validity and cache behavior are explained. Do not promise to recall material already downloaded, screenshotted, buffered, or stored outside the product's control. A revoked deep link must not disclose a private title or relationship in its unavailable response.

External hosting must be evaluated against these requirements. If a provider cannot support the required private behavior, it cannot be assumed to support private galleries merely because its link can be embedded. Public provider links and private uploaded assets may require different supported workflows; no provider has been selected here.

## 11. Representative journeys

- **A curious visitor:** open a topic card, scan the visual overview, view a connected illustration, follow its character link, and return to the same story/gallery position.
- **A period explorer:** select a historical period, browse media about it, distinguish modern reconstructions from original material, and switch to a timeline without losing the selected period.
- **A family member:** see a newly shared old photo, read its original date, open the related family event, and see only media permitted to that viewer.
- **A video viewer:** follow a timestamped event link, read captions if available, inspect a related entity, and return to the same segment and context.
- **A contributor:** reuse one permitted asset in another appropriate context, add a contextual caption, and submit the new public placement for review without duplicating the asset or widening its audience.

## 12. Initial implementation and acceptance coverage

Use [SC-16 through SC-19](14-reference-scenarios.md) for context preservation, recent shares, historical interpretation, and video access. `SC-03`, `SC-11`, and `SC-13` also apply to spoilers, tags, and indirect disclosure. Future verification must cover all routes, not only the full-size media viewer.

The first local M0 implementation must demonstrate at least one actually loadable, permitted still image and the bundled playable explanatory video, together with their captions, origin labels and contextual links. A metadata record, research URL or unavailable placeholder does not satisfy image/video viewing. The v0.6 sample includes an original family-connection illustration for this purpose, explicitly labelled as an explanatory diagram. Both it and the local parentage video require reading boundary level 2 or later; neither belongs in the default introductory hero, preview or gallery. They must not be described as museum artwork or documentary evidence. A museum image requires a usable rendition, recorded reuse basis and visual/description review before it is selected. [Documents 17](17-flagship-content-and-source-plan.md) and [20](20-implementation-plan.md) own the current content selection and implementation steps.

M0 galleries start from the bounded collection, an entity and an event/story context. Use one asset identity across those routes; show a useful empty state when the current reading boundary leaves no eligible media. Test video controls, captions/transcript, return position and failure behavior. Real private uploads, invitations, recent-share delivery and private offline caching remain later capabilities; their synthetic fixtures stay outside the public sample.

The following choices remain for the broader R0 and later media capabilities:

- Which additional gallery contexts and sorts should follow the bounded collection/entity/event M0 routes?
- What distinction between creation and capture is exposed for each media type?
- Which storage, delivery, and external-embedding options meet private-access and reuse requirements?
- What caption, subtitle, transcript, and alternative-description coverage is required at launch?
- How should multiple authorized shares of one asset be grouped without obscuring who shared what?
- Which memories may resurface, who can disable recurrence, and what notifications are appropriate?
- What offline validity, deletion retention, and download policies apply to public and private media?

Keep source supply, offline policy, and external ingestion aligned with the central decision register, especially `Q-09`, `Q-13`, and `Q-16`. Detailed decisions can be added there without changing accepted product directions. The bounded M0 and proposed R0 scope are owned by documents 20 and 16. This general specification does not independently select a media provider, storage limits, or claim a completed product implementation.
