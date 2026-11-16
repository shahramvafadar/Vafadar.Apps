# 06 — Private Spaces, Consent, and Privacy

**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Conceptual product rules; accepted requirements, proposed designs, and open decisions are distinguished below.

## 1. Purpose and decision status

The primary experience serves a curious general audience exploring connected people, history, stories, organizations, companies, and teams. Short introductions, visual relationships, timelines, story threads, and galleries make discovery approachable. Private family trees, life stories, and personal relationships are optional uses of the same core. A public entity does not make the private information connected to it public.

**Accepted** identifies an explicit user requirement or a previously accepted direction. **Proposed** identifies a detailed design that implements that direction and remains open to refinement. Existing `PRIV-` identifiers remain stable. This document describes intended behavior; it does not claim that an authorization system or media feature has been implemented.

### Accepted foundations

- Public discovery is the dominant use; users choose personal or family spaces when relevant.
- A displayed social relationship such as “close friend” requires two accounts, a request, notification, and acceptance by the other person.
- A person without an account can be invited by email or a shareable link.
- Discovering personal accounts or profiles through a university, organization, or attendance period requires the person's permission. Publicly documented roles follow a separate content-publication policy.
- An approved person tag can appear in that person's profile and life timeline. Approval does not automatically expand the media audience.
- A person record, a sign-in account, an audience circle, and a visual filter are separate concepts.
- Images and videos can be reached through multiple contexts, including authorized recent shares from friends or family and public historical-period galleries. Each route must respect the content's access restrictions.

The exact audience controls, verification method, default expiry periods, and launch feature set remain proposed designs or open decisions.

## 2. Records, accounts, and management authority

| Rule | Status | Product rule |
|---|---|---|
| **PRIV-001** | Proposed | Every item and relationship has an explicit access scope. A private note about a public entity remains private. Public presentation follows publication; saving a draft is not publication. |
| **PRIV-002** | Accepted | A person record can exist without an account, including a historical person, a deceased relative, or an uninvited person. An account represents sign-in identity, settings, and granted permissions. |
| **PRIV-003** | Proposed | An unclaimed record for a living person contains minimal information within the creator's permitted scope. Creating it gives no unlimited ownership of that person's identity. Child and supervised records have separate, reviewable management rules. |
| **PRIV-004** | Proposed | Linking an account to an existing record requires independent review. Matching email, name, or photograph is a clue, not automatic proof, record merging, access to an entire family, or ownership of a public page. |
| **PRIV-005** | Proposed | A family curator can collaborate within delegated permissions. Content-management authority does not let them accept a social relationship or publish on behalf of an independent account holder. |

When records might describe the same person, the system can suggest a match. Reviewers should see conflicting facts, sources, and audiences. A merge must not combine the private permissions of both records into a larger audience. Self-confirmed information, another contributor's assertion, and the creator's private notes remain distinguishable.

A public knowledge page about a living person is also distinct from their private account. Verified identity can provide a route for corrections, but does not automatically grant power to erase or rewrite all public statements about that person. Such requests follow content governance. Conversely, the existence of a public page does not authorize publication of sensitive family information or disclosure of a private account.

## 3. Invitations and mutual social relationships

**PRIV-006 — Accepted:** An invitation, an identity claim, and acceptance of a relationship are independent actions. Opening an invitation or registering does not approve every fact or relationship in an existing record. When no account exists, the invitation route comes first. A shared social relationship becomes displayable only after the two accounts have completed the required acceptance.

**PRIV-007 — Proposed:** Relationship states include draft, requested, accepted, rejected, cancelled, and ended. A pending request is visible only as necessary to its participants. Retrying a request or accepting it twice must not create duplicate active relationships or duplicate notifications. A stale acceptance after cancellation, rejection or expiry cannot silently reactivate the request. The notification clearly names the requested relationship and accompanying message. A substantive change, such as “acquaintance” to “close friend,” needs fresh acceptance.

**PRIV-008 — Proposed:** Acceptance does not mean visibility to everyone. A viewer needs permission from both parties to see the relationship, together with permission to see the relevant person information in that context. One party's wider audience cannot override the other's restriction. A private audience circle also does not create a confirmed social relationship label.

**PRIV-009 — Proposed:** Either party can cancel a request or withdraw consent to display a relationship. Subsequent presentation and discovery results must reflect that change. Keeping a limited private history or publicly displaying an ended relationship are separate decisions. Blocking and controls against repeated unwanted requests should be part of the relationship experience.

Invitation links should be revocable, expire according to a defined policy, and serve a specific purpose without embedding private information in the URL. One accepted purpose does not authorize a second relationship or repeated account linking. An invitation intended for a named recipient and a shareable invitation with a broader recipient rule must declare which recipient checks apply. Possessing a link does not verify the recipient's identity. Responses must not disclose whether an email has an account unless the relevant disclosure is permitted.

## 4. Discovery through universities, companies, and time

**PRIV-010 — Accepted:** Personal-account or personal-profile discovery through a university or organization is available only for people who have granted the appropriate permission. **Proposed detail:** name discovery, organizational affiliation, and attendance dates can be controlled separately. A visible name is not consent to publish education history. Being included in a university-and-year result itself discloses the affiliation and temporal match; hiding the attendance dates on the result card does not remove that disclosure. The discovery grant must cover the match being revealed. Browsing a sourced public role follows content governance and must not reveal a link to a private account or private data.

**PRIV-011 — Proposed:** Temporal filters have explicit meanings.

| Filter | Meaning of a matching result |
|---|---|
| Attended during a year or period | The recorded attendance interval overlaps the selected time. |
| Entry year | The recorded start of attendance occurred in that year. |
| Graduation year | A separately recorded graduation event occurred in that year. |
| Specific class or cohort | There is evidence of membership in that class or group. |

Overlapping attendance does not establish that people were classmates, acquainted, or friends. Uncertain intervals distinguish possible overlap from overlap supported throughout the permitted range. Unknown dates are not replaced with invented values. Hidden personal information cannot be revealed merely to answer a temporal filter.

**PRIV-012 — Proposed:** Authorization is applied before producing results, counts, rankings, summaries, or recommendations. Counts, suggested names, graph degree, connection paths, comparisons, notifications, thumbnails, video posters, captions, transcripts, and exports must not reveal private people or relationships. Even “three additional hidden members” can disclose information. Client-side visual hiding is insufficient.

## 5. Media, tags, galleries, and deliberate publication

**PRIV-013 — Accepted:** Tag approval permits a connection between the media and the person. Display in their life timeline still requires permission to view the media, the tag, and the relevant person information. Approval does not enlarge the audience. Removing a tag, requesting removal of the media, and correcting a caption are separate actions.

The accepted multi-entry gallery requirement uses this same rule. **Proposed detail:** an item visible in a family album must not become visible to a broader audience through a person page, period gallery, video deep link, search preview, or “recent shares” view. Friendship acceptance by itself grants no access to all of a friend's photos. Video posters, subtitle files, previews, metadata, and download routes follow the same authorization model as the media they disclose. Detailed media behavior is in [15 — Media and Galleries](15-media-and-galleries.md).

**PRIV-014 — Proposed:** Making private material public is a separate action with an audience preview. Names, relationships, places, media, and dependent sources are checked again. Summaries, translations, and AI-derived content do not bypass the source's private scope. Publishing one part of a story does not publish every connected record. Reusing a source asset does not transfer private captions or tags into a public placement.

**PRIV-015 — Proposed:** “View as another audience” is available for profiles, relationships, events, and media. It uses the real authorization rules. The preview itself must not reveal the selected viewer's private groups or other personal information.

## 6. Removal, export, and offline use

**PRIV-016 — Proposed:** Exports contain only information permitted at generation time and identify their scope, date, and sources. Shared or public outputs must not accidentally include private notes, sensitive image metadata, captions, or tags. Removal and access changes also cover search indexes, media links, posters, previews, and derived presentations.

**PRIV-017 — Proposed:** Define private offline-copy validity and sign-out behavior. Revocation applies to subsequent authorized online requests; offline behavior must explain synchronization and expiry. The product must not promise to recall an image, video, screenshot, or file already downloaded by another person. If a limited history is retained for handling a removal, its access remains restricted. Media already buffered or saved on an offline device is not described as instantly recoverable.

Removing an item from one album is distinct from withdrawing the asset or revoking its access. Proposed media rules distinguish those actions so that an independently authorized placement can remain available without restoring a revoked asset or disclosing a private share.

## 7. Acceptance examples

These are future checks of the proposed rules, not completed software tests.

1. **Friendship:** a close-friend request is invisible to third-party viewers until accepted. After acceptance, only viewers permitted by both sides see it.
2. **Invitation:** a recipient registers but rejects the relationship. The account exists; the relationship does not become public.
3. **Identity:** two records share an email. The system suggests review without automatically merging information or permissions.
4. **University:** a person's name is visible but their education affiliation is private. They appear in neither the affiliation results nor their counts.
5. **Time:** two students entered in different years but attended during overlapping years. Attendance finds both; entry-year search finds the appropriate student without creating a classmate relationship.
6. **Photo:** a tag is approved. A friend of the tagged person who lacks media access sees neither the photo nor a revealing preview.
7. **Publication:** a private story is prepared for public publication. Hidden relationships and media without publication permission are identified before publication.
8. **Revocation:** graph paths, search, and online media links stop returning the restricted information after access is removed. Offline behavior follows the disclosed policy.
9. **Recent shares:** an old authorized family photograph can appear because it was newly shared, while the caption retains the original capture date. A newly approved friendship does not grant an audience that the share did not select.
10. **Video route:** a viewer who lacks access cannot obtain a revealing poster, subtitle, transcript, count, recommendation, or video segment through another route.

Cross-reference scenarios `SC-09` through `SC-13` and `SC-17` through `SC-19` in [14 — Reference Scenarios](14-reference-scenarios.md).

## 8. Open decisions

Keep these aligned with the central [decision register](12-decisions-questions-and-change-log.md), especially `Q-05`, `Q-06`, `Q-07`, `Q-13`, and `Q-14`.

- What evidence and assurance level link an account to an existing record?
- Which roles and review route handle conflicts between a living person's request and a public knowledge page?
- Does displaying an ended social relationship need separate consent, and how long is its restricted history kept?
- Which discovery permissions are simple defaults, and which belong in advanced settings?
- What are the offline-validity periods, deletion-retention rules, and limits of guardian authority?
- How are personal-gallery notifications and resurfaced memories controlled without implying new publication?

## 9. First-slice boundary and later verification

The initial public demonstration uses reviewed-purpose sample content and synthetic fixtures; it does not implement personal accounts, real invitation delivery or private family uploads. Its spoiler boundary is a reading preference, not proof of a private-data security boundary. Full editorial datasets and the family review fixture must not be deployed as public static data.

When private use is implemented, authorization must be enforced on the data query and every dependent media route, with actor-specific tests. Include invitations that are replayed, cancelled or expired; a tag whose audience is narrower than its media; a university/time query that would reveal a hidden affiliation; and a viewer whose access changes between opening a page and requesting a derivative. The current fixture explicitly grants tag access separately from the fact of approval, so an expected result cannot rely on an unstated permission.
