# 0001. A typed, immutable read model of one collection

- Status: Accepted (implemented and verified on 2026-11-16)
- Date: 2026-11-16

## Context

The first collection is an editorial sample in JSON ([document 17](../product/17-flagship-content-and-source-plan.md)):
14 entities, 20 relations, 8 story events, 18 sources and 5 media records, with the whole story including its ending.
The specification is clear about what this file is and is not ([document 03](../product/03-domain-model-and-ontology.md),
DOM-017): an editorial input, not the app's schema and not something a visitor may receive. The app needs a model
that keeps every meaning the documents distinguish – a relation has a type, a direction, a scope and a source; someone
mentioned in an event is not present in it; a place in the story is not a date – and that later collections of other
kinds (companies, teams, families) can use without becoming "persons".

Phase 3 builds this model and the reader for the sample. The screens come later and must not need a second model.

## Decision

1. **`Vafadar.Dastan.Core` holds an immutable, typed read model of one collection** (`Catalogue`). It is built once,
   validated as a whole and never changed afterwards. Core has no JSON, file or web code.

2. **`Vafadar.Dastan.Data` reads the editorial input into that model.** It accepts exactly the declared format
   version (`dastan-pilot-0.2`), checks everything the handoff's validator checked, and reports every problem with the
   record it belongs to. A problem is never skipped: a relation without its source or endpoint would silently change
   the graph. The cross-domain fixtures have their own format and their own reader, in the tests only.

3. **Identifiers are types.** `EntityId`, `RelationId`, `EventId`, `SourceId` and `MediaId` wrap the stable text of
   the input, so that an event cannot be passed where an entity is expected. The identifiers of the input are kept
   unchanged.

4. **Entity types are a managed list, each with a nature.** The type says what the thing is (character, animal
   character, narrative place, work, company, team …); its nature (being, group, place, work, object, occasion) is
   what views may rely on – a lane is for beings, a setting is a place. A new type is a new entry of the list, not a
   new class. Nothing in the model assumes a person.

5. **A relation is a sourced, scoped statement between two entities**: its type with the direction the type has
   (directed or symmetric), a label that reads in the stated direction, the scope (background, revealed in an event,
   an action in an event, a report), the events of the scope, the sources with a passage locator, the review status
   and the reading level. Nothing derives a relation; nothing turns one into another.

6. **An event is one shared record with typed associations.** Each association names an entity, what it does (free
   text of the editors) and its presence: acts, is involved, is the setting, is an object, is only mentioned, is the
   subject of a report. Views show the presence; they never count associations as "people who were there". The time
   of an event is a `TemporalExpression` of `Vafadar.Temporal` – for this collection a place in the story order – and
   never a date.

7. **Text carries its language.** Labels, summaries and captions are `LocalizedText`: a value per language, each
   marked reviewed or draft. Resolving for a visitor returns the text, the language it is in and whether that is a
   fallback, so a page can say so. The sample's English text is the reviewed base; Persian text is added as a
   separate overlay file next to the sample and is marked as a draft until the owner has reviewed it (decision D-033).

8. **Media are content with rights, not files.** A media record keeps its kind, what it depicts, how it represents it,
   its creation time (a `TemporalExpression`), credit, rights status and demo eligibility. A local file is a path
   relative to the content folder and is opened only by the media route of the web project after the projection has
   allowed it. The reviewer-only provenance path of the input is checked and then dropped: it does not exist in the
   model and can therefore never become a link.

9. **Every record has a reading level; the model itself shows nothing.** The catalogue is the complete editorial
   truth. What a visitor sees is produced by one projection (publication and demo eligibility, then the reading
   boundary, then every derived view). That projection and its tests are the second step of this phase and get their
   own ADR.

## How the reader applies it (2026-11-16)

* **Unknown is refused, not ignored.** `PilotSample` reports a format version it does not know (and then reads
  nothing else), a field it does not know, a value outside the managed lists and a field given twice. Editors' notes
  that the read model does not need are named in the reader, so a new field is noticed the day it appears.
* **Where the checks of the handoff's validator live.** What one record states (versions, known values, https
  addresses without credentials, files inside the content folder, dates) is checked by the reader. Whether the
  records fit together (references, reading levels, places and objects in events, media that may be shown) is checked
  by `Catalogue`, for every source of data. What is true of this particular sample (five stages, the opening as the
  start, Rostam only mentioned in event 3) is asserted by tests on the sample.
* **The validator's ten negative cases.** Six are refusals of the reader or the catalogue with a test each (dangling
  endpoint, parentage moved to the opening, a year on a story event, an uncleared image made showable, a reviewer
  document as the page of an image, a working document as a transcript). "Era as calendar" is refused by the reader
  for the sample and cannot be expressed in `Vafadar.Temporal`. "The mentioned father attends" is a fact of the
  sample and is a test on it. "Reversed period" is refused by `Vafadar.Temporal` and tested on the fixture
  (`FixtureTimeTests`). "A revoked viewer keeps media" tests access policies of private spaces; those are not part
  of M0 and the case is not ported.
* **Told time stays told time.** A calendar mapping, a calendar period, a duration, a year or a date next to a place
  in the story are problems. The creation date of an artwork keeps what the catalogue states: the year, "circa" as
  approximate, an unnamed calendar as unnamed.
* **Translations** are documents of their own (`dastan-translation-0.1`, `<sample>.<language>.json` next to the
  sample) with one review state for the language. The Persian names inside the sample (`labelFa`) follow that state
  and are drafts without it. A translation of something the sample does not have is a problem; a text without a
  translation is a fallback that says so. Evidence metadata (source titles, locators, rights statements, credit
  lines) stays in the language of the source.
* **Files.** A local path is resolved against the sample's folder, has to stay inside the content folder and is kept
  relative to it (`media/…`). A document for reviewers has to stay inside the app folder, has to exist, and is not
  part of the model.

## Consequences

* The reader is strict: the sample either loads completely or the app does not start, with a list of what is wrong.
* Core can be tested without files and without a web host; the reader is tested with the real sample and with
  damaged copies of it (the validator's negative cases).
* Views, search and the media route work on one model. A later relational store replaces the reader, not the model.
* Vocabulary (entity types, relation types, presence kinds) grows by adding entries. Who may add them is an open
  product question (Q-15); until it is answered the list changes with the code.
* The Persian overlay keeps the reviewed English sample byte-for-byte as delivered. A missing Persian text is a
  visible fallback, never an invented translation.

## Addendum of 2026-11-17 – more than narrative things (step 10.6)

The file of a collection could name the things of a story only. The model always allowed more – a nature, not a
"person", is what a view asks an entity for – and the plan held the rest back until content asked for it. The
owner asked for the step on 2026-11-17, before such content exists; so it adds what the guide named as missing
and no more.

* **Four types of entity** in the file: `Person` (a real person; the type the model already had),
  `Organisation`, `Place` and `Region`. Their natures are the ones views know – somebody, a group, a place –
  so no view changes: a real place is the setting of a step like a place of the story, a person stands in a
  drawing of connections like a character.
* **A thing of the story and a thing of the world stay two entities**, also under one name. `NarrativeRegion`
  says "it has no modern border"; a `Region` is a land of history. Nothing maps one to the other.
* **Eight relation types of the world outside a story**: `authorOf`, `bornIn`, `diedIn`, `teacherOf`,
  `siblingOf`, `memberOf`, `founderOf`, `locatedIn`. Unlike the types of the first story, each **says what it
  means and between what kinds of thing it holds** (`RelationType.Meaning`, `From`, `To`), and a collection in
  which one stands between other kinds is refused: "born in" reads from somebody to a place. The kinds are
  natures – a character may be taught as a person may. The types of the first story keep holding between
  whatever they held between; giving them ends would be a judgement about content that exists.
* `siblingOf` is no kinship for the family drawing: the drawing is made from parents and partners (ADR 0005),
  and a sibling without a parent in the collection would have no place in it.
* **A scope for what is not of the story**: `outside_the_story`. A relation still names the story of its
  collection; the scope says the statement is about the world the story was told in.
* **When** something happened is not a relation: it is a statement with a date of the entity (step 9.4).

Not done, on purpose: types nobody asked for (a battle, an office, a title, "patron of", "successor of"); a
check that a relation of the world has the scope `outside_the_story`; pages that set things of the world apart
from things of the story. The first collection that uses the types shows which of these it needs.

Verification: `RealWorldTypesTests` (12 tests): a collection with a poet, a teacher, a brother, a court, a city
and a land and ten relations is read; seven relations between the wrong kinds are refused, each with one reason;
a character may stand where somebody stands; the types of the first story are unbound; a real place that acts in
a step is refused like a place of the story; the list for writers says the meaning and the ends.
`RealWorldPagesTests` (10 tests): the page of each of the four kinds, with its kind in English and Persian; the
connections of a person and of the work; no family from what is no family; found by the search. All with test
data – the collection of the repository uses none of the new types.

## Second addendum of 2026-11-17 – the whole vocabulary (step 12.1, D-047)

Step 10.6 added four types and eight relations and left the rest "until content asks". The owner decided
otherwise (D-047): what the product documents name is supported before content needs it. This step completes
the vocabulary against documents 03 (DOM-002 to DOM-012) and 14.

* **Twenty more types of entity** in the file, beside those of the story and of step 10.6: `Company`, `Brand`,
  `Product`, `University`, `Government`, `Dynasty`, `Family`, `Community`, `Country`, `SportsClub`,
  `SportsTeam`, `Competition`, `CompetitionSeason`, `Event`, `Period`, `Office`, `Honour`, `Object`,
  `NaturalSubject`, `Concept`. What the documents ask to keep apart is apart by type: a brand is a thing, not
  the company; a team is not its club; a government is a group, the country a place; a season is not its
  competition (DOM-009, DOM-010, DOM-011).
* **Two more natures**: `Role` (an office, a title, an honour – what somebody holds) and `Subject` (a species,
  an idea, a field). No view treats them specially; they exist so that a relation can say "to an office".
* **Every type says what it is** (`EntityType.Meaning`), and the list for writers prints it. The name of a type
  in a file is its key with a capital, except the names the first files chose (`NarrativeArc`).
* **Forty-seven more relation types**, each with its meaning and its ends, in the groups of the documents:
  persons and offices (`holderOf`, `successorOf`, `patronOf`, `employedBy`, `leaderOf`, `ownerOf`,
  `studiedAt`, `alliedWith`, `supporterOf`, `livedIn`, `adoptiveParentOf`); works (`translatorOf`,
  `illustratorOf`, `publisherOf`, `commissionerOf`, `dedicatedTo`, `adaptationOf`, `translationOf`,
  `editionOf`, `about`, `keptAt`); identity (`portrayalOf`, `possiblySameAs`, `confusedWith` – DOM-003,
  DOM-004: none of them makes two entities one); organisations (`acquired`, `mergedInto`, `spunOffFrom`,
  `subsidiaryOf`, `brandOf`, `productOf` – DOM-009); governments (`governs`, `capitalOf`); sport (`teamOf`,
  `playsFor`, `coachOf`, `captainOf`, `loanedTo`, `transferredTo`, `competesIn`, `winnerOf`,
  `organiserOf`, `seasonOf` – DOM-010); events (`participantIn`, `tookPlaceIn`, `during`); subjects
  (`worksIn`, `foundIn`).
* **An inverse is not a second type**: "student of" is `teacherOf` read from the other side, "owned by" is
  `ownerOf`. The label of a relation says it in words.
* **A rename is no relation** (DOM-009): a name with the time it was used is a name of the entity – step 12.3.
* `adoptiveParentOf` is no kinship for the family drawing, which is drawn from parents by birth and partners
  (ADR 0005); the relation is shown among the connections.

Not part of the step: when a relation held (step 12.2), names (12.3), views made for companies or sport
(phase 14).

Verification: `WorldVocabularyTests` (8 tests), made from the vocabulary itself so that a type added later is
tested without a new test: a collection with two things of every kind and one relation of every bound type is
read; every bound type is refused at each end for a kind it does not hold between; every type has a meaning and
a name in `Ui.resx` and `Ui.fa.resx`; the list for writers prints meanings and ends. Test data only.

## Third addendum of 2026-11-17 – when a relation held (step 12.2)

DOM-007 asks that a role has its time and that "separate tenures must not become one continuous bar".

* `Relation.Time` is an optional `TemporalExpression` – the same type and the same JSON as the time of a dated
  statement, so "about", "between two years", an open or unknown end are said as they are stated (document 04).
* **Only a relation with the scope `outside_the_story` has a time.** A relation of the story with a `time` is
  refused, as `scope.calendarPeriod` always was: told order is never turned into dates.
* **A tenure is a relation.** Nothing merges two relations of one type between the same two entities; each has
  its identifier, its sources and its time, and the timeline draws each as its own bar.
* The time is shown where the relation is: in its sentence (`Statement`), hence in lists of connections and on
  its page of sources; and on the timeline of the collection, in the lane of the entity the relation reads
  from, labelled with the wording of the relation and the name of the other entity.
* A relation the visitor may not see yet gives nothing of its time (the projection drops the relation).

Not done here: a form for a time in the tool of the editors (step 13.1; until then the record is changed as
text); a "who was where at this date" view over relations (phase 14 – built as step 14.2, eighth addendum).

Verification: `RelationTimeTests` (5 tests) – read as stated; two tenures stay two; a relation of the story, a
place in the story and an unreadable time are refused; hidden before its stage; the collection of the repository
dates no relation. `RealWorldPagesTests` – the time in the sentence in English and Persian, on the page of
sources, and the lane with a bar for every tenure on the timeline. Test data only; not looked at in a browser.

## Fourth addendum of 2026-11-17 – names and identifiers (step 12.3)

DOM-003: an entity may have alternatives, historical names, names in several languages, titles, abbreviations
and external identifiers; search finds these names; similarity does not establish identity.

* `Entity.Names`: each a name with its language tag, a kind (`alternative`, `former`, `abbreviation`, `title`,
  `original_script`), optionally the time it was used, its sources and a reading stage. The label of an entity
  stays the one translated name; other names are stated in their own language and are not translated.
* A name can give a story away ("the grieving father"), so it may carry a stage of its own, never earlier than
  its entity; the projection drops what lies beyond the boundary, and the search cannot find by it.
* `Entity.Identifiers`: issuer, value, optional address, and how sure the editors are (`exact`, `close`,
  `uncertain`). The page names the issuer, links out with `rel="external noopener noreferrer"` and says that
  what the other catalogue states is its own.
* **Nothing merges.** Two entities with one name are both found. "Possibly the same" is a relation
  (`possiblySameAs`, step 12.1), never a merge. Merging and splitting records as acts of the editors with a
  trail belong to the tool (phase 13).
* A rename is a `former` name with its time, of one entity (DOM-009).

Verification: `EntityNamesTests` (9 tests) and `RealWorldPagesTests` (the page in English and Persian, the
search by another name). Test data only; not looked at in a browser.

## Fifth addendum of 2026-11-17 – what a collection is about (step 12.4)

A collection was one story: exactly one `NarrativeArc`, an edition of a text, English texts. With the
vocabulary of step 12.1 a collection can be about a company or a club, which tells no story (DOM-002: "a
collection selects material for a purpose").

* `CatalogueInfo.Subject` (was `Story`): the entity the collection is about and the name of the collection in
  addresses. With exactly one story it is that story, as before – no existing file changes.
* `CatalogueInfo.Stories`: the stories the collection tells, the one a visitor enters first at the beginning –
  the subject when it is a story, else the first in the file. Each story counts its steps from 1; a step says
  its story (`storyId`, derived as `StoryStep.Story`). `Projection.Steps` stays the first story,
  `AllSteps` is every story, `Story(id)` and `Lanes(id)` take a story.
* **One ladder of reading stages for the collection**, also with several stories. Stages per story would make
  "how far have you read" a question per story on every page; no document asks for that.
* **No story**: no events, no edition (`CatalogueInfo.Edition` is optional), relations with the scope
  `outside_the_story` need not name a story. The pages show the subject with its kind, leave out what is of
  a story (work and text, the first step, the link to the story), and `story` is not found.
* **The language of the file** is `CatalogueInfo.Language`: any language tag. A page falls back to it, not to
  English, and marks a text that exists in it only (`Foreign`) with its language and direction.
* Addresses: `story` is the first story, `story/<id>` another one (`StoryAddress`).

Not done: a site that serves several collections at once (it serves one, as before – phase 14); reading
stages per story; a story chart across stories.

Verification: `CollectionSubjectTests` (13 tests) and `CollectionSubjectPagesTests` (6 tests): a collection
about an organisation written in Persian – read, projected, all its pages answer, `story` is not found; two
stories – order, steps from 1, pages, the way from an entity to a step of the second story; the refusals; the
language as a tag, said once, never repeated by a translation; a story written in Persian and translated into
English. The template `subject` is read by the readers in `ContentToolTests`. Test data only; not looked at in
a browser.

## Sixth addendum of 2026-11-17 – a quantity as a statement (step 12.7)

DOM-005 names "a quantity with unit and precision" among the values a statement can have.

* A statement about an entity (`dated`) may state a `Quantity`: a number, a unit and whether the source says
  "about" – and for money its currency. It keeps everything a statement has: sources, review, reading stage,
  translation of its label. So a quantity is reviewed, disputed, withdrawn and published like any statement,
  and no second kind of statement had to be taught to the store, the revisions and the tool.
* **The time became optional**: a statement says when, how much, or both. A population in a year has both;
  the number of verses of a poem has a quantity alone. Only what has a time is on the timeline.
* **The units are a managed list** (`QuantityUnits`, 22 entries) with a meaning for writers and words for the
  pages in both languages. A free text as unit would make "verses", "couplets" and "lines" three units.
* **Nothing is rounded or converted.** A page writes every digit that was stated, grouped by thousands, with
  the digits and marks of its language (`QuantityText`). Precision is what the digits say and "about".
* **The page of an entity lists what is stated about it** – until now such statements were visible on the
  timeline only, and a statement without a time would have been visible nowhere.

Not done: a series of quantities as a chart (phase 14 – built as step 14.3: ADR 0008, seventh addendum); ranges ("between 40,000 and 60,000"); units of
other systems.

Verification: `QuantityTests` (6 tests) and `RealWorldPagesTests` (the page of an entity in English and
Persian with a quantity at a time, a disputed one without a time and an amount of money). Test data only; not
looked at in a browser.

## Seventh addendum of 2026-11-17 – where a place is (step 12.8)

* `Entity.Position` (`GeoPosition`): latitude and longitude as decimal degrees, written with the digits the
  source gives, and a `PositionPrecision` – `Exact`, `Approximate`, `CentreOfArea`. The precision is said
  because a point for a country is no statement about its borders (DOM-011: "names, boundaries and affiliations
  can depend on time and source").
* **Only a real place has a position.** A `NarrativePlace` or `NarrativeRegion` with one is refused, as is
  anything that is no place: the Turan of the poem is not put on a map by accident.
* The page of the place writes the position in the language of the page and links it as a `geo:` address – a
  device opens it with the map it has. The page loads no map and names no map service: nothing about the
  visitor goes to a third party (privacy matrix unchanged).
* Borders, and borders that change with time, are not modelled; a map of the collection is phase 14 (built as step 14.1: ADR 0008, sixth addendum).

Verification: `PlacePositionTests` (9 tests) and `RealWorldPagesTests` (the page in English and Persian).
Test data only; not looked at in a browser.

## Eighth addendum of 2026-11-18 – what held in a year (step 14.2, R-PUB-03)

The relations of the world state when they held (step 12.2) and statements about an entity state their time.
The read model now answers the question the other way round: what does the collection say of one year.

* **`YearCover.Of(time, year)`** says whether a time reaches a year, with three answers (`Holding`):
  * **Yes** – the year lies inside what is stated: the year of an occurrence; a year from the latest possible
    beginning to the earliest possible end of a period; any year after the beginning of what still lasts.
  * **Possibly** – the records leave it open: a year between the two times a beginning or an end is known
    between; any year after a beginning whose end nobody states; any year before an end whose beginning nobody
    knows; the year of a time that is said to be about; one of several alternative times.
  * **No** – the time does not reach the year, or cannot be placed among the years of the common era (another
    era, an order of a story, an unknown time). A period of which neither end is known reaches no year.
* **A year is asked for, not a day.** What is stated to the year holds in that whole year; what ended in a
  year still held in it and what began in it already. The axis is the one of the charts (`YearAxis`): it
  counts years, so a Julian and a Gregorian date of one year are the same year.
* **`Projection.Years()`** names the first and the last year anything the visitor may see is stated for, and
  the years in which something begins, ends or happens; `null` when nothing can be placed.
* **`Projection.At(year)`** answers a `MomentView`: the relations that held or may have held, what is stated
  of entities for the year, and the nearest earlier and later year in which something changes.
* **The page** `<collection>/at` asks for the year with a form that asks for a page (no script). A year is a
  whole number from 1 to 9999 with `era=bce` for the time before the era; anything else is said to be no year
  and is not written back into the page. The relations that end at a place stand apart: who and what was
  where. The page is offered on the page of a collection that states a year.

Not done: a day or a month as the question; a slider; the year on the map of the places (14.1) and in the
family tree; the former names of an entity for the year.

Verification: `YearCoverTests` (31 cases) – occurrences, windows, periods with known, uncertain, unknown and
ongoing ends, times that are about, the change of the era, alternatives, what has no place among the years.
`MomentPageTests` (23 cases) – the years of the projection, the page in both languages, what held, what may
have, what is stated with its quantity, an empty year, a year before the era, six addresses that are no year,
a story alone, another collection. Not looked at in a browser.
