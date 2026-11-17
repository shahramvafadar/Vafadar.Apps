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
