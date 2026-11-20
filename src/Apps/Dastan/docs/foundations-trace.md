# From what the product asks to what can be walked – the table of step 19.1

Written on 2026-11-19 for step 19.1 of the [delivery plan](delivery-plan.md) (section 20, phase 19).

## What this table is for

Phase 19 corrects the foundations and adds no feature. This table says, for every requirement of the product
documents, where it stands today, which step of phase 19 builds or changes it, and which journey a tester
walks in a browser to see it when the phase is done. Step 19.11 ends the phase when every row has evidence.

**How a row is read.** "State today" is the state of the code at the start of phase 19, taken from
[acceptance.md](acceptance.md), the delivery plan (its lists "found and not changed" and "what is left") and
the ADRs [0015](adr/0015-postgresql.md) to [0021](adr/0021-timeline.md). Nothing in it was tried again for
this table. The states:

* `built` – the documents record it as implemented and checked.
* `built in part` – with the part that is missing.
* `not built`.
* `contradicted` – the code does the opposite of what is asked, or cannot do it by its construction.
* `not recorded` – the documents do not say; see the last list.

"Step of phase 19" is the step that builds it or changes what a visitor sees of it; `unchanged` means that
no step changes its behaviour. Step 19.3 moves the storage under almost every row and step 19.11 walks every
journey once more – neither is repeated in each row.

"The journey" is written as the site will be **after** phase 19. ADR 0020 (the graph) does not exist yet;
journeys through a graph follow step 19.8 of the plan: Syncfusion EJ2 Diagram behind the contract
`DastanGraph`.

**Evidence.** The column names test classes that exist today and hold the requirement. It is filled in
completely by step 19.11. Three things are kept apart:

* A test of markup or of a library says what a page holds. It does not say that somebody looked at the page
  in a browser. Most rows below have the first and not the second – acceptance.md says so row by row.
* A class marked **†** runs against one of the SQLite stores or against accounts with a name and a password.
  Steps 19.3, 19.4 and 19.6 rewrite these; a † class is evidence again only when it passes on PostgreSQL.
* A class marked **(T)** is a test of the library `Vafadar.Temporal` (`test/Libraries/Vafadar.Temporal.Tests`):
  it holds the meaning of a time, not a page.

**What the table covers.** The 65 requirements of [document 02](product/02-requirements-and-use-cases.md),
the 17 rules of [document 06](product/06-private-spaces-consent-and-privacy.md) and the 19 scenarios of
[document 14](product/14-reference-scenarios.md): 101 rows, in the order and the groups of those documents.
The identifiers are those of [document 13](product/13-glossary-and-traceability.md); none is new. The detailed
rules of the other product documents (families DOM-, TIME-, MED-, GOV-, PUB-, VIS-, FLOW-, SCREEN-, TASK-,
PILOT-; about 150 identifiers in documents 03, 04, 05, 07, 15, 16 and 18) have **no rows of their own here** –
see the last list.

## Public access and connected knowledge

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-PUB-01 | Most published knowledge opens without an account. | built – no collection is published yet (the owner's review) | unchanged | Open the address of a published collection in a private window, without signing in, and read the page of a name. | `PublishedSiteTests`, `SiteTests` |
| R-PUB-02 | Several kinds of entity, each with the information and connections that fit it. | built in part – 31 kinds and their relations exist; a kind has no profile with its own facts and picture | 19.5 | Open the profile of a company, of a team and of a person, and see on each the facts of its kind and a picture or the drawing of the kind. | `WorldVocabularyTests`, `RealWorldTypesTests`, `OtherSubjectsTests` |
| R-PUB-03 | Relationships, events and changes over time; a past period shows the roles of that period. | built – with records of the tests; a year is asked for, not a day | 19.9 | On a collection choose a year, see which offices and memberships held then, choose a later year and see them change. | `YearCoverTests`, `MomentPageTests`, `RelationTimeTests` |
| R-PUB-04 | Find by subject, time and place. | built in part – by name, kind, place and a year of the common era; no year before it; no period | none | On the search of a collection choose a kind, a place and a year, and open a name from the result. | `FindingTests`, `EvidenceAndSearchTests` |
| R-PUB-05 | Compare several entities or timelines; only compatible frames share an axis. | built – as a still chart opened by links | 19.9 | Open the comparison of Iran, China and Rome, zoom and pan in the browser, and see three lanes over one axis of years. | `ComparisonTests`, `TimelineLayoutTests`, `ComparisonPageTests`, `ChartTests` |
| R-PUB-06 | Help visitors go on to further information; a suggestion says why it follows. | built – threads the editors lay; the site works out none itself (decided, 18.4) | unchanged | On the page of a name follow "where a thread leads on", read the reason of the next stop and open it. | `ThreadsTests`, `ThreadPagesTests` |
| R-PUB-07 | Stable links for entities, statements and saved views; live and version-specific links told apart. | built in part – a statement is cited with its revision; an earlier revision cannot be shown | none | Open the sources of a statement, copy its citation with `?revision=n`, open it after a new revision and read that another one is shown. | `MomentAndCitationTests`, `RevisionsPageTests`, `SavedListTests` |
| R-PUB-08 | A readable alternative to every visual view. | built – a list or table beside each drawing | 19.8, 19.9 | Open the connections of a name with scripts turned off and read every relation of the drawing in the list below it. | `ConnectionsPageTests`, `ComparisonPageTests`, `FamilyAndStoryPageTests` |
| R-PUB-09 | Say where coverage ends; an empty result is not shown as proof of absence. | not recorded – empty views say "nothing to show here"; whether they say what that does not prove is not written down | unchanged | Search for a name the collection does not hold and read what the empty result says about what it means. | `StatesTests` (empty views only) |

## Entities, identity and relationships

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-DOM-01 | A record of a person is independent of an account. | built | 19.5 | In a family tree add a grandparent who has died and has no account, and open that person's profile. | `PrivateSpaceRulesTests`, `FamilyTreesTests`† |
| R-DOM-02 | Names, nicknames or titles, birth information, photos and further attributes; unknown stays unknown. | contradicted – a person of a tree has a name and two years: no day, place, note or picture (ADR 0014); no node has a profile picture | 19.5 | Open a person of a tree, see the picture, born, died and places, and read "unknown" where a fact is not known. | – |
| R-DOM-03 | Aliases, names in other languages and earlier names lead to the same identity. | built – with test data | unchanged | Search for the former name of a company and arrive at the page of the company as it is called now. | `EntityNamesTests`, `RealWorldPagesTests` |
| R-DOM-04 | Several relationships and roles between the same two entities stay apart. | contradicted – between two accounts a second relationship is refused (ADR 0014); public entities can have several | 19.6 | With two accounts ask and accept "colleague" with an employer and "close friend", and see both on the page of each. | `WorldVocabularyTests` (public entities only) |
| R-DOM-05 | Membership, office and affiliation over time; a present holder does not overwrite a former one. | built in part – for public entities; affiliations of accounts are rules in a library, with no store and no page | 19.6 | Open a team, choose two seasons and see two coaches; on an account add a school with its years. | `RelationTimeTests`, `OtherSubjectsTests` |
| R-DOM-06 | No automatic merge because names or contact details match. | built | 19.4, 19.5 | Register with the mail address of a record in a tree and see that nothing is merged; claim the record and have its maker confirm. | `EntityMergeTests`†, `IdentityClaimsTests`†, `PrivateSpaceRulesTests` |
| R-DOM-07 | One shared event links several participants and places. | built in part – steps of a story with typed parts; an event with a date or a place and no place in a telling is shown nowhere | 19.9 | Open an event with a date and a place from the timeline and from two of its participants, and arrive at the same event. | `SampleProjectionTests`, `FamilyAndStoryPageTests` |
| R-DOM-08 | Claims keep their evidence, context and competing accounts. | built | unchanged | Open a statement marked "disputed", read what is disputed and the sources of each side. | `DisputedStatementTests`, `DisputedAndWithdrawnTests`, `EvidenceAndSearchTests` |
| R-DOM-09 | Governed definitions for kinds and relationships. | built – every kind has a meaning and a name in both languages; wrong ends are refused | 19.6 | Run `vocabulary` of the content command and read the meaning of "acquired", "merged with" and "renamed". | `WorldVocabularyTests`, `RealWorldTypesTests`, `ContentToolTests` |

## Time and chronology

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-TIME-01 | Exact, partial, approximate and unknown dates. | built in part – in the library and for public statements; a person of a tree has only a year from 1 to 9999 | 19.5 | Give a person of a tree "born about 1890" and see it written so, not as a day and not as an exact year. | `ExpressionTests` (T), `JsonTests` (T), `ComparisonPageTests` |
| R-TIME-02 | An uncertain window of an occurrence differs from a real duration, in meaning and in drawing. | built in part – library and tables keep them apart; the chart draws a window as a point with a mark (ADR 0021) | 19.9 | On a timeline see an event "between two years" drawn as a window and a reign drawn as a duration, each worded so. | `ComparerTests` (T), `ComparisonTests` |
| R-TIME-03 | Uncertain, open and unknown beginnings and ends; an unknown end is not "still going on". | built in part – in the library, in the forms of the editors and in "may have held"; not drawn as open | 19.9 | On a timeline see an organisation with an unknown end drawn open, and read "end unknown", not "until today". | `ExpressionTests` (T), `ComparerTests` (T), `YearCoverTests`, `StudioTimeFormTests`† |
| R-TIME-04 | Years before the common era and very distant times keep their meaning. | contradicted – a chart knows 9999 BCE to 9999 CE; what lies outside goes into a list; `ChartState.Year` fails at the lowest number | 19.9 | Open one page each for 500 BCE to today, 10–20 thousand years ago and 50–60 million years ago, in both languages. | `ComparerTests` (T), `PositionTests` (T), `YearAxisTests` (T) |
| R-TIME-05 | Calendar, epoch, unit and the expression of the source are kept. | built in part – kept and worded as stated; calendars are not converted (dates are stored Gregorian, D-043) | 19.9 – the axis; conversion is in no step | Open a statement dated in the Solar Hijri calendar and read it in that calendar with its precision. | `JsonTests` (T), `PositionTests` (T), `GalleryAndMediaTests` |
| R-TIME-06 | Time of a story, relative and ordinal time, without invented years. | built | 19.9 | Open the story of Rostam and Sohrab as a timeline and see the order of the telling with no year on it. | `ComparerTests` (T), `StoryChartTests`, `StatesTests` |
| R-TIME-07 | The time something happened, the time it was published and the time it was recorded stay apart. | built – a photograph has the day it was taken and the day it was brought in; a statement has its revision | unchanged | Bring in a photograph taken decades ago, find it among recent shares and read the year it was taken. | `PhotographsTests`†, `PhotoPagesTests`†, `RevisionHistoryTests` |
| R-TIME-08 | Certain overlap, possible overlap and "not enough known" are told apart. | built – "held" against "may have held" | unchanged | Choose a year inside an uncertain period and read that the relation "may have held" then. | `ComparerTests` (T), `YearCoverTests`, `MomentPageTests` |

## Stories and narrative context

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-STORY-01 | Works, worlds, characters, their relations and the events of a story. | built | 19.5, 19.8 | Open Rostam, see his profile, his family and the steps of the story he is part of. | `PilotSampleTests`, `SampleProjectionTests`, `JourneyTests` |
| R-STORY-02 | Editions, adaptations and source anchors are named and not silently combined. | built – each telling is a collection of the same world | unchanged | From a collection open another telling of the same world and read that it keeps its own reading position. | `StageCorrespondenceTests`, `SeveralCollectionsTests` |
| R-STORY-03 | The order of reading is apart from the order of events inside the world. | not recorded – the order of the telling is shown; an order inside the world is nowhere said to be shown | 19.9 | Open a story with a flashback and see the step late in the telling and early in the order of the world. | `StoryChartTests` (order of the telling only) |
| R-STORY-04 | Exploring without spoilers: the reading position governs texts, edges, suggestions and previews. | built – one line of stages per collection; several lines in one collection decided against (18.4) | 19.7 | Set the reading position to stage 1, open the graph of a name and its data address, and find nothing of a later stage in either. | `SampleProjectionTests`, `ProjectionTests`, `SiteTests`, `JourneyTests` |
| R-STORY-05 | Guided narratives of text, visuals and linked events. | built – as threads; the collection of the repository has none | unchanged | Open a thread, go from stop to stop through a name, a step and a picture, then leave it for free exploring. | `ThreadsTests`, `ThreadPagesTests` |

## Personal, family and social purposes

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-PER-01 | Ancestors and descendants are recorded. | built | 19.8 | Open a family tree, focus on a person and expand three generations of ancestors and two of descendants. | `FamilyTreeTests`, `TreeShapeTests`, `FamilyTreesTests`† |
| R-PER-02 | Complex kinship, adoption, guardianship and several marriages are told apart. | built in part – links are "parent of" and "partner of" only | 19.8 – several partners, birth and adoptive parents; guardianship is in no step | In a tree give a child birth parents and adoptive parents and a person two partners, and see each drawn apart. | – |
| R-PER-03 | A shared social relationship needs a request, a notification and acceptance. | built in part – asked and accepted; the other side is told only by "1 waiting" on the site | 19.6; a mail about a request is in no step | Account A asks B to be shown as a friend; a third account sees nothing until B accepts. | `PrivateSpaceRulesTests`, `RelationshipsAndCirclesTests`†, `PeoplePagesTests`† |
| R-PER-04 | Somebody without an account is invited by mail or by a link. | contradicted – an account has no mail address and the site sends nothing; a link exists | 19.4 | Invite a mail address, read the mail in the catcher, open its link, register, and see that no relationship was accepted. | `PrivateSpaceRulesTests`, `BelongingsAndInvitationsTests`† |
| R-PER-05 | Audience circles are apart from kinds of relationship and from filters of a view. | built | 19.6, 19.8 | Hide a kind of relationship in the graph and see, as another account, that what that account may see has not changed. | `PrivateSpaceRulesTests`, `RelationshipsAndCirclesTests`†, `PeoplePagesTests`† |
| R-PER-06 | Education, work and residence in the timeline of a life. | not built – "the life of a person" is photographs; a record holds no place and no affiliation | 19.5, 19.6 | On one's own profile add a school with its years and a place of residence, and see both in the timeline of the life. | – |
| R-PER-07 | Being found through an organisation is something an account opts in to. | built in part – the rule is a library; no store, no page, no search | 19.6 | Three accounts were at one school; two allow being found; a search for the school and a year names two and counts two. | `PrivateSpaceRulesTests` |
| R-PER-08 | Posts, photographs and tags the tagged person approves. | built in part – photographs and tags; no posts | none for posts | Tag a friend in a photograph, approve as the friend, and see as a friend of theirs outside the audience that nothing is shown. | `PrivateSpaceRulesTests`, `PhotographsTests`†, `PhotoPagesTests`† |
| R-PER-09 | A user withdraws consent to showing a relationship or a tag, alone. | built – it holds with the next request | 19.6 | End a shown relationship as one side and see, as a third account, that it is gone at once. | `PrivateSpaceRulesTests`, `PhotoAgreementTests`†, `MakingPublicTests`† |

## Content, platform and quality

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-GOV-01 | Sources and a meaningful history of revisions for public information. | built | 19.3 | From a statement open its sources, then the page "what changed" between two revisions. | `SampleProjectionTests`, `RevisionHistoryTests`, `RevisionsPageTests`, `EditorialStoreTests`† |
| R-GOV-02 | Drafts are apart from published content. | built | 19.3 | In the tool of the editors change a record and see the public site unchanged until a revision is published. | `PublishedRevisionTests`, `PublishedSiteTests`, `EditorialPublishingTests`† |
| R-GOV-03 | Reporting an error, disputed claims and correction. | built | 19.3 | Report an error from a statement, find it in the tool, correct the record, publish, and see the public page changed. | `ReportBoxTests`, `VisitorReportTests`, `StudioReportsTests`†, `DisputedStatementTests` |
| R-GOV-04 | Origin, attribution and conditions of use of media. | built – a video still keeps what it says about its making (18.7) | unchanged | Open an image, read its maker and rights; open a museum work and see its record and a link, nothing of the image. | `GalleryAndMediaTests`, `MediaReferenceTests`, `MediaIntakeTests` |
| R-PLAT-01 | Built in Vafadar.Apps and delivered as an installable app. | built in part – the manifest is read without an error; no installation was observed | 19.11 – a phone; Windows is in no step | Install the app on a real phone from the browser, start it from its icon and open a deep link. | `InstallableAppTests`, `InstalledAppTests`, `WorkerUpdateTests` |
| R-PLAT-02 | Persian, right to left, and a way to more languages. | built – no German (D-031) | 19.2, 19.8 | Open the graph of a name on the Persian site and read mixed Persian and Latin labels, digits and long names without a fault. | `TranslationTests`, `FontTests`, `JourneyTests`, `ChartTests` |
| R-PLAT-03 | No private data leaks through queries, graphs, caches or outputs. | built – for pages and files | 19.7, 19.8 | As a stranger ask the page and the data address of a private relationship and get "not found" from both, with no count. | `SiteTests`, `SampleProjectionTests`, `PrivateSpaceRulesTests`, `PeoplePagesTests`† |
| R-PLAT-04 | Public pages can be referred to and found. | built – no picture with the words of a page (decided, 18.4) | 19.7 | Load a public page with scripts turned off and read its text and the descriptions of its media. | `FindablePagesTests`, `JourneyTests` |
| R-PLAT-05 | Accessibility, keyboard and use on a phone. | built in part – rules a tool decides and the keyboard pass; no phone, no screen reader, no visitor watched | 19.8, 19.11 – a person with a screen reader is in no step | With the keyboard alone at 360 px focus a node of the graph, open its profile panel and return. | `InstalledAppTests`, `JourneyTests`, `ChartTests` |
| R-PLAT-06 | Use without a connection is decided apart from installing; users see what is kept. | built – public pages after a yes (D-073); nothing of an account | 19.7 | Read a page, stop the connection, see the page marked as a kept copy, and have the device forget it on "My list". | `KeptOfflineTests`, `InstallableAppTests` |
| R-PLAT-07 | All project documentation is in English. | built | 19.1, 19.10 | Read `docs/` of Dastan: every document, ADR and legal record is English; the texts of the interface are a separate choice. | – |

## General-user visual discovery

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-EXP-01 | Curious visitors come first and get a useful first glimpse. | built – no first-time visitor was watched | unchanged | Open the start page without a question in mind and understand from it what the collection is about. | `SiteTests`, `JourneyTests`, `CollectionSubjectPagesTests` |
| R-EXP-02 | The essential first, optional depth step by step. | built in part – a page of names, statements and sources; no profile with a picture and the few facts first | 19.5 | Open a name, see its card with a picture and three facts, open the full profile, then the sources of one fact. | `EvidenceAndSearchTests`, `RealWorldPagesTests` |
| R-EXP-03 | Several chart views are normal ways in: relationship map, comparative timeline, story path. | contradicted – the views exist as still pictures from the server; nothing is moved, zoomed or opened in the browser (D-035) | 19.7, 19.8, 19.9 | Open the graph of a name, drag it, zoom, expand a node and open its profile without a new page being loaded. | `ConnectionsLayoutTests`, `StoryChartTests`, `TimelineLayoutTests` (the still views) |
| R-EXP-04 | Visitors follow threads that explain themselves; nearness is not called friendship or cause. | built | 19.8 | Select an edge of the graph, read its label, direction and source, and follow it to the other name. | `ThreadsTests`, `ThreadPagesTests`, `ConnectionsPageTests` |
| R-EXP-05 | Focus and context survive a change of view and the way back. | built in part – Back returns with what was open; a period has no gallery to go to; carrying filters is not recorded | 19.8, 19.9 – carrying a selection between views is in no step | Zoom a timeline to a period, copy the address, open it in another window and see the same view. | `EvidenceAndSearchTests`, `JourneyTests`, `StageBrowserTests` |
| R-EXP-06 | Ways in beyond searching for a known name. | built | unchanged | From the start page begin with a thread, a picture of a gallery or a kind of thing, without typing a name. | `ThreadPagesTests`, `SeveralCollectionsTests`, `FindingTests` |

## Images, videos and galleries

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| R-MED-01 | Images and videos are connected media of the first rank. | built in part – for collections; visitors have no videos (decided, D-066) | none | Open an image and a video of a collection, each in its viewer, and go from each to a name it shows. | `GalleryAndMediaTests`, `MomentAndCitationTests`, `JourneyTests` |
| R-MED-02 | Galleries are reached from entities, periods, places, events and stories. | contradicted – an item says when it was made, not what time it shows, so no gallery of a period exists; no themes | 19.9 – a period; a theme is in no step | Select a period on the timeline, open its gallery and find a painting of 1850 of an event of 1020 at 1020 as a later picture. | `GalleryAndMediaTests` (entity, step and collection only) |
| R-MED-03 | A gallery of recently shared media of family and friends, for the permitted audience only. | built in part – still images of the last thirty days; no videos | unchanged | As a friend inside the audience open recent shares and see an old photograph shared today; outside it see nothing. | `PhotographsTests`†, `PhotoPagesTests`† |
| R-MED-04 | The times of capture or creation, of what is shown, of upload, publication and sharing stay apart. | built in part – the time made, the day taken and the day brought in; no "time it shows" | 19.9 | Open a modern painting of an old event and read both: made in one year, shows another. | `GalleryAndMediaTests`, `PhotoPagesTests`† |
| R-MED-05 | One access rule for media on every way to it and for every derivative. | built | 19.5, 19.7 | Choose a private photograph as profile picture; a stranger sees the drawing of the kind on the card and gets no file. | `GalleryAndMediaTests`, `PrivateSpaceRulesTests`, `PhotoPagesTests`†, `AlbumsTests`† |
| R-MED-06 | The viewer leads back to where it was opened and on to its subjects. | built – the place in a gallery after the way back is not recorded | unchanged | From a gallery open a video, follow a name it shows, and return with Back to the video and to the gallery. | `GalleryAndMediaTests`, `JourneyTests` |
| R-MED-07 | Curated albums and galleries by a rule over the same items, without copies. | built – albums of visitors and galleries of a collection | 19.9 | Put one photograph into two albums, change its caption once and see the same photograph and caption in both. | `AlbumsTests`†, `PhotoPagesTests`†, `GalleryAndMediaTests` |
| R-MED-08 | Accessible media with visible attribution, rights and origin. | built | unchanged | Open a video, turn on its captions and read its maker, its rights and where it comes from. | `GalleryAndMediaTests`, `MediaReferenceTests`, `InstalledAppTests` |

## Private spaces, consent and privacy (document 06)

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| PRIV-001 | Every item and relationship has an explicit scope; a draft is not published. | built | 19.5, 19.6 | Bring in a photograph, leave its audience as it begins, and see as another account that it does not exist. | `PrivateSpaceRulesTests`, `PhotoPagesTests`† |
| PRIV-002 | A record of a person can exist without an account. | built | 19.5 | Add to a tree a person who has died and one who was never invited, and open both profiles. | `PrivateSpaceRulesTests`, `FamilyTreesTests`† |
| PRIV-003 | An unclaimed record of a living person holds little; records of children have their own rules. | built in part – a record holds little; no rule for children and guardians (accounts are for 16 and older, D-061) | 19.5 changes what a record holds; children are in no step | Add a living relative without an account and see which facts the form asks for and who may see them. | `FamilyTreesTests`†, `MakingPublicTests`† |
| PRIV-004 | Linking an account to a record needs a review by somebody else; a matching mail or name is a clue. | built | 19.3, 19.5 | Say "This is me" on a record; its maker confirms; nothing else of the tree opens and no record is merged. | `PrivateSpaceRulesTests`, `IdentityClaimsTests`†, `FamilyTreePagesTests`† |
| PRIV-005 | A family curator works inside what was delegated and accepts nothing for another account. | built | unchanged | As somebody who may change a tree add a person, then try to accept a relationship for another account and be refused. | `PrivateSpaceRulesTests`, `FamilyTreePagesTests`†, `PeoplePagesTests`† |
| PRIV-006 | An invitation, a claim of identity and acceptance of a relationship are three acts. | built | 19.4 | Open an invitation from a mail, register, and see neither a claim nor a relationship until each is done by itself. | `PrivateSpaceRulesTests`, `BelongingsAndInvitationsTests`†, `AccountFollowsTests`† |
| PRIV-007 | States of a relationship; no duplicates on retry; a stale acceptance does nothing; a changed kind is accepted anew. | built in part – two requests sent at the same moment can both be kept (18.7); a changed kind is not recorded | 19.3, 19.6 | Send the same request twice at once and find one; take it back, accept the old page as the other side and see nothing accepted. | `PrivateSpaceRulesTests`, `RelationshipsAndCirclesTests`† |
| PRIV-008 | A relationship is seen only where both sides' audiences allow it. | built | 19.6 | A shows it to everybody, B to one circle; somebody outside that circle does not see it on either page or in the graph. | `PrivateSpaceRulesTests`, `PeoplePagesTests`† |
| PRIV-009 | Either side cancels or withdraws; blocking and controls against repeated unwanted requests. | built in part – cancel, end and a pause of thirty days after "no"; no blocking of an account | 19.6 – blocking is in no step | Decline a request, try to ask again as the sender and be told to wait; end a relationship and see it gone. | `PrivateSpaceRulesTests`, `RelationshipsAndCirclesTests`† |
| PRIV-010 | Being found through a university or organisation needs permission that covers the match revealed. | built in part – a library only | 19.6 | Allow one's name to be shown but not one's school; a search for the school and a year neither names nor counts the account. | `PrivateSpaceRulesTests` |
| PRIV-011 | "Attended during", "entered in", "graduated in" and "class of" mean different things. | built in part – a library only | 19.6 | Search one school for "there in 1383" and for "entered in 1380" and get two different, labelled results. | `PrivateSpaceRulesTests` |
| PRIV-012 | Authorisation comes before results, counts, rankings, previews and suggestions. | built – for pages and files | 19.7 | Ask the data address of a graph as somebody outside an audience; the hidden edge is not there and no count hints at it. | `PrivateSpaceRulesTests`, `PeoplePagesTests`†, `PhotoPagesTests`†, `AlbumsTests`† |
| PRIV-013 | An approved tag connects media with a person and widens no audience. | built | unchanged | Approve a tag; a friend of the tagged person who is outside the audience sees neither the photograph nor a preview. | `PrivateSpaceRulesTests`, `PhotographsTests`†, `PhotoPagesTests`† |
| PRIV-014 | Making something private public is an act of its own, with a check of everybody it names. | built – trees and photographs; a tree's agreement and public page were not looked at in a browser | 19.4 | Ask to make a tree public, read who stands in the way, have each agree with their own account, and open `/trees/{id}`. | `PrivateSpaceRulesTests`, `MakingPublicTests`†, `PhotoAgreementTests`† |
| PRIV-015 | "View as another audience" for profiles, relationships, events and media. | not built (ADR 0013) | none | On one's own profile choose "as a member of this circle sees it" and see exactly what such a visitor gets. | – |
| PRIV-016 | Exports hold only what is permitted when they are made and say their scope, date and sources. | built in part – the copy of one's own data; a drawing cannot be exported | 19.8 | Export a graph that holds a private edge as somebody who may not see the edge; the file does not contain it. | `OwnDataTests`†, `VisitorAccountTests`† |
| PRIV-017 | Private copies without a connection: validity, sign-out; no promise to recall what was downloaded. | built – a device keeps nothing of an account or a private space (D-064, D-073) | 19.7 | Signed in, open one's photographs, stop the connection and reload: the page says "no connection", nothing is shown. | `KeptOfflineTests`, `InstallableAppTests` |

## Reference scenarios (document 14)

| Requirement | What it asks | State today | Step of phase 19 | The journey that shows it | Evidence |
|---|---|---|---|---|---|
| SC-01 | A company is renamed and acquired; its identity, dated names and a former role stay right. | built – with made-up data, never walked in a browser | 19.5, 19.9 | Search "Azar", arrive at AzarNov, compare 2008 with 2014 and see the manager of 2008 and no later owner then. | `EntityNamesTests`, `YearCoverTests`, `FixtureTimeTests`, `OtherSubjectsTests` |
| SC-02 | Club, team and season are apart; two roles of one person keep their dates. | built – with made-up data, never walked in a browser | 19.5, 19.8 | Open Sepid Club, its senior team in 2020–2021, and a person who was a player then and a coach years later. | `OtherSubjectsTests`, `RelationTimeTests`, `RealWorldPagesTests` |
| SC-03 | The order of the story and protection from spoilers on every surface. | built | 19.7, 19.8 | At stage 4 open a character's graph: a relation revealed at stage 8 is in no label, thumbnail, suggestion or data answer. | `SampleProjectionTests`, `SiteTests`, `JourneyTests`, `GalleryAndMediaTests` |
| SC-04 | Three historical tracks on one axis, narrowed and zoomed together; a story is not placed on a calendar. | built in part – three lanes and seven scales by links; no zoom in the browser | 19.9 | Put three lanes on one axis, zoom with the wheel, see all lanes move together, and open a shared event. | `ComparisonTests`, `ComparisonPageTests`, `ChartTests`, `StatesTests` |
| SC-05 | An event somewhere between 20,000 and 10,000 years ago is not a period of 10,000 years. | contradicted – the range lies outside what a chart can show and a window is drawn as a point | 19.9 | Open the event, see a window (not a bar of a duration), and search 15,000–14,000 years ago: "possibly within". | `ComparerTests` (T) |
| SC-06 | A period whose beginning and end are each uncertain; the certain core apart from the possible part. | contradicted – as SC-05 | 19.9 | Open the period and see two soft ends and a firm middle, each explained in words. | `ExpressionTests` (T), `ComparerTests` (T) |
| SC-07 | Fifty to sixty million years ago, as one event or as a period, without a day or a "today" that moves. | contradicted – no axis for deep time; a chart ends at 9999 BCE | 19.9 | In the tool state "between 60 and 50 million years ago" as an event, then open the page with the line of deep time. | `ComparerTests` (T), `PositionTests` (T) |
| SC-08 | 500 BCE, 1 BCE and 1 CE without a year zero; a year stays a year; an unknown end is not "today". | built in part – all of it but converting a date of another calendar | 19.9; conversion is in no step | See 1 BCE next to 1 CE on the axis, a person born "in a year", and an organisation not listed as active after an unknown end. | `PositionTests` (T), `YearAxisTests` (T), `YearCoverTests` |
| SC-09 | An invitation reaches a second person too; registering proves no identity and accepts nothing. | built – by a link | 19.4 | Open an invitation as a second person: an account is made, the claim waits for the maker of the record, no relationship exists. | `PrivateSpaceRulesTests`, `BelongingsAndInvitationsTests`†, `IdentityClaimsTests`† |
| SC-10 | "Close friend" is asked, accepted and shown by both sides' audiences; either side stops it. | built – one relationship between two accounts | 19.6 | A asks "close friend"; before B accepts a third account sees nothing; after, only who both allow; either ends it. | `PrivateSpaceRulesTests`, `RelationshipsAndCirclesTests`†, `PeoplePagesTests`† |
| SC-11 | An old photograph with an approved tag: its own year, its own audience, three different removals. | built – still images; a year of the Solar Hijri calendar on a photograph is not recorded | unchanged | Open the photograph from the tagged person's page as somebody outside its audience: no picture, no small size, no caption. | `PrivateSpaceRulesTests`, `PhotographsTests`†, `PhotoPagesTests`† |
| SC-12 | Finding people by a university and years, with consent; three different questions. | built in part – a library only | 19.6 | Search attendees of 1383, entrants of 1380 and graduates of a year; C, who did not allow it, is in no result and no count. | `PrivateSpaceRulesTests` |
| SC-13 | No indirect disclosure of something private through paths, counts, search or exports. | built in part – pages and files; no graph across accounts and no export exists yet | 19.7, 19.8 | As a stranger explore a graph whose path runs through a private person: no path, no count, and none in the exported file. | `PrivateSpaceRulesTests`, `PeoplePagesTests`†, `SiteTests` |
| SC-14 | Correction, sources that disagree, and versions that can be referred to. | built – an earlier revision is named, not shown | 19.3 | Report an error, see the statement unchanged until a revision is published, then open the old citation and read the notice. | `VisitorReportTests`, `DisputedStatementTests`, `MomentAndCitationTests`, `RevisionHistoryTests` |
| SC-15 | Casual discovery along a connected thread, and the way back. | built – with a thread of the tests | 19.5, 19.8 | From a topic on the start page open a character, follow a connection for its stated reason, and return to the thread. | `ThreadPagesTests`, `JourneyTests` |
| SC-16 | Change between graph, timeline and gallery and keep entities, period, context and filters. | built in part – each view has an address and Back works; a period has no gallery; a selection is not carried | 19.8, 19.9 – carrying a selection is in no step | In a graph select a name, change to its timeline, open the gallery of a period, open an item, and return step by step. | `JourneyTests`, `StageBrowserTests` |
| SC-17 | Recent media of family and friends; an old photograph in its place in a life; a memory that resurfaces. | built in part – still images; no videos; nothing resurfaces | none | Open recent shares, see only what one may see in the order it was shared, and the old photograph with its own year. | `PhotographsTests`†, `PhotoPagesTests`†, `AlbumsTests`† |
| SC-18 | A historical gallery with modern depictions: "about this period" against "made in this period". | contradicted – no time an item shows, so no gallery of a period | 19.9 | Open the gallery of an old period, see a modern drawing marked as a later picture, then filter by when items were made. | `GalleryAndMediaTests` (the time made only) |
| SC-19 | A link to a moment of a video keeps its context and stops when access ends. | built in part – for videos of a collection and the reading position; visitors have no videos, so no access to take back | unchanged | Open a video at `?t=1:15` from a gallery, follow a name, return; lower the reading position and get "not found" for page and file. | `MomentAndCitationTests`, `GalleryAndMediaTests`, `JourneyTests` |

## Asked for and not in any step of phase 19

Each line is a requirement, or a part of one, whose state is `not built` or `built in part` and that no step
of 19.3 to 19.11 names. Where the owner already decided against it, the decision is named; it stays here so
that it is known.

1. **R-PUB-04** – searching by a year before the common era, and by a period. The search asks for a year of the
   common era; step 19.9 gives the timeline its axes and says nothing about the search.
2. **R-PUB-07 (and SC-14)** – showing an earlier revision. Left out by row 18.3.6 "until the public deployment
   is planned"; ADR 0015 keeps every published revision in the database, but no step shows one.
3. **R-TIME-05 (and SC-08)** – converting a date between calendars while keeping its original form. Calendars
   are not converted today; ADR 0021 speaks of "the calendars of the site" and names no conversion.
4. **R-PER-02** – guardianship as a link of its own. Step 19.8 names several partners and birth and adoptive
   parents, not guardians.
5. **R-PER-03 (and PRIV-007)** – telling somebody by mail that they were asked for a relationship, tagged or
   asked to agree. Step 19.4 builds the outbox and names the mails of an account, invitations and the
   statement of reasons; no step names these.
6. **R-PER-08** – posts. Photographs and tags exist; a post is in no step and in no ADR.
7. **R-PLAT-01** – an installation on Windows observed. Step 19.11 installs on a real phone only.
8. **R-PLAT-05** – somebody using the site with a screen reader, and a first-time visitor watched. Step 19.8
   tests what a tool can decide and keeps the list beside the drawing; no step puts a person in front of it.
9. **R-MED-01, R-MED-03, SC-17, SC-19** – videos of visitors (decided against, D-066), and with them a video
   whose access is taken back.
10. **R-MED-02** – a gallery of a theme. The model has no themes; step 19.9 builds the gallery of a period only.
11. **R-EXP-05 and SC-16** – carrying a selection (two names, a period, filters) from the graph to the timeline
    to a gallery. Steps 19.8 and 19.9 each keep their own view in the address; no step joins them.
12. **SC-17** – a memory that resurfaces on an anniversary, labelled as resurfaced.
13. **SC-18 (R-MED-07)** – a caption and a source for each placement of the same item in several galleries.
14. **PRIV-003** – rules for records of children and for guardians. Accounts are for people of 16 and older
    (D-061); the rule itself is still the owner's. ADR 0017 widens what a record of a living person holds.
15. **PRIV-009** – blocking an account. "Not built" since step 16.3; step 19.6 does not name it.
16. **PRIV-015** – "view as another audience". Not built (ADR 0013); in no step.

Counted as `built` above because the owner decided against the rest (row 18.4): several lines of reading
stages inside one collection (R-STORY-04), suggestions the site works out by itself (R-PUB-06), the words of
a page on the picture of a link (R-PLAT-04).

Not requirements, but found by the review of 18.7, left unchanged there and named by no step of phase 19:
limits per hour that count by the exact address (many budgets with IPv6); a video that keeps what it says
about its making; the version of a record that begins at 1 again after it was taken out; `compose.trial.yaml`
believing the networks of Docker. Step 19.3 does cover the other finding of that list – limits and rules that
forms sent at the same moment could pass.

## Contradicted today

| Row | What the code does | Ended by |
|---|---|---|
| R-DOM-02 | A person of a tree is a name and two years; no node has a profile picture. | 19.5 |
| R-DOM-04 | A second relationship between the same two accounts is refused. | 19.6 |
| R-PER-04 | An account has no mail address and the site sends no mail, so nobody is invited by mail. | 19.4 |
| R-TIME-04 | A chart knows years from 9999 BCE to 9999 CE; `ChartState.Year` fails at the lowest number. | 19.9 |
| R-EXP-03 | Graph, timeline and story are still pictures from the server; nothing is moved or zoomed in the browser. | 19.7, 19.8, 19.9 |
| R-MED-02 | An item has no time it shows, so no gallery of a period exists. | 19.9 |
| SC-05 | A window of 20,000 to 10,000 years ago lies outside the chart and would be drawn as a point. | 19.9 |
| SC-06 | The same for a period with two uncertain ends. | 19.9 |
| SC-07 | No axis for millions of years. | 19.9 |
| SC-18 | No gallery "about this period" apart from "made in this period". | 19.9 |

## Could not be decided from the documents

| Row | What is not said | What would have to be looked at |
|---|---|---|
| R-PUB-09 | Whether an empty result says that it proves no absence. | The words of the empty search, gallery and "at a year" pages in both languages; `StatesTests`. |
| R-STORY-03 | Whether an order of events inside the world is shown apart from the order of the telling. | The read model of a story (ADR 0001) and the story page with a collection that has a flashback. |
| PRIV-007 (SC-10) | Whether changing the kind of a relationship asks the other side again. | `SocialRelationship` in `Vafadar.Dastan.Core.Personal` and `RelationshipsAndCirclesTests`; ADR 0018 says each is agreed to by itself. |
| R-EXP-05 | Whether filters of a search or a gallery survive a change of view. | The addresses of search and gallery pages; `FindingTests`. |
| R-MED-06 | Whether the way back restores the place in a gallery. | `JourneyTests` and a gallery longer than one screen, in a browser. |
| SC-11 | Whether the year of a photograph can be given in the Solar Hijri calendar. | The form of `/account/photos` and `PhotographsTests`. |
| SC-04 | Whether a shared event of a comparison is one record linked from several lanes. | [The format of a comparison](comparison-format.md) and `ComparisonTests`. |
| acceptance.md against ADR 0021 | acceptance.md says "about" and "between two years" are "drawn as such"; ADR 0021 says the chart "drew a window as a point with a mark". R-TIME-02 follows the ADR here. | The comparison page with a period "between two years", in a browser. |
| The detailed rules | About 150 identifiers of the families DOM-, TIME-, MED-, GOV-, PUB-, VIS-, FLOW-, SCREEN-, TASK- and PILOT- (documents 03, 04, 05, 07, 15, 16, 18) refine the rows above and have no rows here. | Each of those documents against this table, before step 19.11 calls the table complete. |

## Where the rows stand at the end of phase 19 (step 19.11, 2026-11-20)

The table above says where each row stood when the phase began and is left as it was written. This section
says where the rows that were open then stand now. Three words are kept apart:

* **built** – the code is there and its tests pass (3126 tests on the last revision, of them 375 in Chromium;
  the tests in a browser also in Firefox and WebKit, see the end of this section);
* **looked at** – somebody looked at the page in a real browser by eye. This was done by the builder for a few
  pages, named below; the owner has not looked at any of it yet;
* **ready** – fit for the public. **No row is ready**: nothing was tried on a real phone, with a screen reader,
  with a real mailbox, with a client at Google or Microsoft or by a visitor who was watched, and the texts of
  the collection are drafts no editor has approved.

A row that is not named here stands as the table above says.

| Row | Now | Evidence | Looked at | What is still open |
|---|---|---|---|---|
| R-PUB-02, R-DOM-02, R-EXP-02 | built – every node has a profile with a picture or a drawing in its place and the facts of its kind; a person of a tree has days, places, a note and a picture (19.5, ADR 0017) | `NodeProfileBrowserTests`, `PersonProfilesTests`, `TreePersonPagesTests`, `TreePersonBrowserTests`, `ProfileDataTests` | the profile of a person of a tree and the panel of a profile | – |
| R-PUB-03 | built – "what held" answers for a year, a stretch of years and a depth of time (19.9) | `MomentPageTests`, `DeepTimeTests`, `AskedTimeTests` | no | – |
| R-PUB-04 | built – the search takes a year before the common era, a period and a depth of time, by the one reader of a time (19.9) | `AskedTimeTests`, `FindingTests`, `DeepTimeTests` | no | – |
| R-PUB-05, SC-04 | built – a comparison is moved and made larger and smaller in the browser; what is counted on another axis has a view of its own (19.9) | `TimelineBrowserTests`, `TimelineViewsTests`, `ComparisonPageTests` | the comparison of Iran, China and Rome, in English and Persian | whether a shared event is one record linked from several lanes is still not said |
| R-PUB-07 | unchanged – an earlier revision cannot be shown | – | – | in no step |
| R-PUB-08 | built – the list or the table stays beside every drawing, and the still drawing stays without a script (19.8, 19.9) | `GraphBrowserTests`, `TimelineBrowserTests` (without a script) | yes, both | a person with a screen reader |
| R-PUB-09 | not recorded, as before | – | – | the words of the empty pages were not read for this |
| R-DOM-04, PRIV-007 | built – two accounts can have several relationships, each agreed to by itself; a changed kind asks the other side again (19.6, ADR 0018) | `SeveralRelationshipsTests`, `SeveralRelationshipsPagesTests`, `RelationshipsBrowserTests`, `AtTheSameMomentTests` | no | – |
| R-DOM-05, R-PER-06, R-PER-07, PRIV-010, PRIV-011, SC-12 | built – affiliations of an account have a store, a page and a search with what each side shows (19.6) | `AffiliationsTests`, `AffiliationPagesTests`, `SeenAsTests`, `SeenAsPagesTests` | no | between real people |
| R-DOM-07 | built in part – a statement with a time is on the timeline of its name and of the collection whether or not a telling names it (19.9); an event of a telling with a date of its own is still refused by the reader of a collection | `TimelineOfANameTests`, `TimelinePageTests` | no | an event that is both a step and dated |
| R-TIME-01 | built – a person of a tree has a time as the library says it: a day, a year, "about", a window (19.5) | `PersonProfilesTests` | yes | – |
| R-TIME-02, R-TIME-03, SC-05, SC-06 | built – a window is drawn as a window, what lasted with what is known of its ends, an open end as open and an end nobody states as not stated (19.9) | `TimelineViewsTests`, `TimelineBrowserTests` (measured against the axis) | yes, a made-up comparison with every kind of end | the still chart of the server still draws a window as a point |
| R-TIME-04, SC-07 | built – deep time has an axis of its own and is asked for and written with its meaning; the fault at the lowest number is ended (19.9) | `AskedTimeTests`, `DeepTimeTests`, `TimelineBrowserTests`, `ComparisonPageTests` | yes | the still chart draws the calendar only |
| R-TIME-05, SC-08 | built – a date exact to the day is shown as written and converted beside it; what is not exact enough is not converted (19.9) | `CalendarConversionTests`, `CalendarPagesTests` | no | a choice of the calendar by the reader |
| R-STORY-03 | built in part – the order inside the world can be said (a position of its own) and has a view of its own on a timeline; the page of a story shows the order of the telling as before | `TimelineViewsTests` | no | the page of a story on the new control |
| R-PER-02 | built – several partners, parents by birth, adoptive parents and guardians, a guardianship with its time (19.6, 19.8) | `FamilyTreesTests`, `GraphDataTests`, `TreePersonPagesTests` | no | – |
| R-PER-03, R-PER-04 | built – an account has a confirmed mail address; a request, a tag and an invitation are told by mail (19.4, ADR 0016) | `AccountsWithMailTests`, `NoticeMailTests`, `AccountJourneyBrowserTests` (the mail really sent to a catcher) | the pages of an account | a real mailbox, a sender with a domain |
| R-PER-08 | unchanged – no posts | – | – | in no step |
| R-PLAT-01 | built in part, as before – no installation was observed | `InstalledAppTests` | no | a real phone: the owner's |
| R-PLAT-05 | built in part – the rules a tool decides pass on the new pages too, and the graph and the timeline have their own keyboard | axe in `GraphBrowserTests`, `TimelineBrowserTests`, `AccountJourneyBrowserTests` | – | a phone, a screen reader, a visitor who is watched |
| R-EXP-03 | built – the graph and the timeline are moved, zoomed, selected and opened in the browser (19.7, 19.8, 19.9); the page of a story keeps its chart of links | `GraphBrowserTests`, `GraphSizeBrowserTests`, `TimelineBrowserTests`, `ProfilePanelBrowserTests` | yes | the page of a story |
| R-EXP-05, SC-16 | built in part – a chosen card of a graph leads to its timeline and its pictures, a time leads from "what held" to its gallery and back, the view of a timeline is kept in the address (19.9) | `GraphBrowserTests`, `TimelineOfANameTests`, `GalleryOfATimeTests`, `TimelineBrowserTests` | no | filters of the search are not carried to another view |
| R-MED-02, R-MED-04, SC-18 | built – an item has the time it shows apart from the time it was made, and the gallery of a time shows what shows that time (19.9) | `MediaShownTimeTests`, `GalleryOfATimeTests` | no | themes; a picture in several places (R-MED-07) |
| R-MED-01, R-MED-03, SC-17, SC-19 | unchanged – visitors have no videos (D-066) | – | – | in no step |
| PRIV-003 | built in part – a record of a person holds more now, and a child (under 18) is described only by who may (19.5); no rule for guardians of an account | `PersonProfilesTests` | no | children with accounts are in no step |
| PRIV-009 | built – an account can block another (19.6) | `BlockingTests` | no | between real people |
| PRIV-015 | not built, as before (ADR 0013) | – | – | – |
| PRIV-016, SC-13 | built in part – a drawing of a graph is saved as SVG and PNG; there is no graph across accounts (19.8) | `GraphBrowserTests` (the saved pictures) | no | a saved file opened in another program |

**Accounts, the stores and the tool of the editors** (the rows of steps 19.3 and 19.4 that were not open above) stand as their steps
say in the delivery plan, with their tests.

**The last walk, in three browsers.** On the last revision the tests in a browser were run whole in Chromium
(375 of 375), in Firefox (372, and 3 that are not asked of it: installing, and the page for "no connection") and in WebKit (374, and 1 that is not asked of it: installing) – none failed. They walk every journey that has a row with a test in a browser
above. What a test cannot see – whether a page looks right – was looked at by eye for the pages named in the
column "Looked at", in Chromium only.

**In CI.** The workflow has a job for each of the three engines that runs these tests (`dastan-browser`). It
has not been seen to run: every run on `main` since 2026-11-19 was cancelled by a later push of the other app
of the repository, because a run of a branch was cancelled when a newer one began. On 2026-11-20 the owner
decided that every push to `main` keeps a run of its own; the first run after that is the first that can show
whether the job passes.

**The owner's, before anything is called ready:** a real phone (installing, touch with two fingers on the graph
and the timeline), a screen reader, Safari itself, a mailbox and a sender with a domain, the company that sends
the mail and a contract with it, clients at Google and Microsoft, the key of the licence of the graph, a
lawyer's reading of the legal texts, and the approval of the texts of the collection by an editor.
