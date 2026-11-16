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
