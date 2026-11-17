# 0011. The store of the editors: records with their history, beside the file – not in place of the site's input

* Status: Accepted (D-045)
* Date: 2026-11-17
* Step: 11.1 of the [delivery plan](../delivery-plan.md), section 12

## Context

Until now a collection is one file that the editors change by hand. That is enough for one collection and one
person. It is not enough for editors: a file does not say who changed a statement, when and why, what it said
before, or what is waiting for a review (GOV-005). The plan names "relational storage with editorial tools" as
the phase after the pilot's first steps (document 20), and says to decide the provider "from actual needs and
operating cost".

What exists and must keep working: the readers that decide what a usable collection is (`PilotSample`), the
rule of what everybody may see (ADR 0009), the `revision` command, and a public site that is given the content
folder of a published revision and nothing else (ADR 0010).

## Decision

1. **The store is the working place of the editors, not the input of the public site.** The site keeps reading
   a published revision from a folder. A revision is made from the store, written out and deployed as before.
   The public site never opens the store, so the store adds nothing a visitor can reach and nothing a host has
   to run.
2. **A collection lives in the store as its records.** Each source, entity, relation, event and media item is a
   row: its collection, its group, its identifier, its place in the group, and the record itself as it is written
   in the file of a collection (JSON), with its review state beside it for asking "what is not reviewed". The
   rest of the file – edition, reading stages, publication, notes – is the header of the collection. A
   translation is a row per language.
3. **The file format stays the one description of a record.** The store does not have a second model of an
   entity or a relation in tables of its own. What a record may say is decided by the readers, which read what
   the store writes out; a rule added to a reader holds for the store without a change of the store.
4. **Every change of a record is kept**: who, when, why, the record before and after, and the version it led
   to. Changes are only added. A record that is taken out leaves its history.
5. **In and out is the file of a collection.** `Import` takes a working file with its translations: the first
   time everything comes in, afterwards what differs becomes changes. `Export` writes the file back; it reads
   as what went in – every field, every record, the order of every group. A file the site cannot use and a
   published revision are refused and change nothing.
6. **SQLite, through EF Core, in a file the editors name.** A few editors and one collection need no database
   server; a file is backed up by copying it and costs nothing to run. The schema is made and changed by
   migrations. The model uses nothing that is special to SQLite, so another database can take its place when
   there is a reason – several editors at once over a network would be one.
7. **A project of its own**, `Vafadar.Dastan.Store`, used by the command of the writers and later by the tools of
   the editors – not by the website, which would carry a database it must not use.

## Considered and not chosen

* **Tables for entities, relations, events, media and their parts.** It is what "relational" suggests, and it
  would let a database ask "all relations of type parentOf". But it is a second description of every record
  beside the file format, to be kept equal to the readers field by field, for questions nobody asks yet: the
  site answers them from the read model. If the editors' tools need such a question, the review state shows how
  a field moves beside the record without the record being taken apart.
* **PostgreSQL in a container** (decision 8 of the plan allows a database there). A service to run, to back up
  and to secure for a working place of a few people. Kept as the path when the store has to be shared.
* **The shared library `Vafadar.Data`.** It is written for an app's database on a device – local profiles,
  encryption, backup to a user's cloud. None of that is the store of editors; EF Core is used directly.
* **The store as the input of the public site.** The site would need the database, its credentials and its
  availability to show a page, and what is not published would lie one mistake away from a visitor.

## Consequences

* The file of a collection is still what a writer – a person or an assistant – works with and hands over
  (D-042). Bringing it into the store is how its changes get an author, a time and a reason.
* A review written into a file is a change of its statement like any other; the store does not yet know reviewing
  as an act of its own. That, withdrawing, and publishing from the store are the next steps (delivery plan, 12).
* The store does not check who the editor is: the name is what the command is told. An account of an editor is a
  step of its own and needs the owner's decision on how editors sign in.
* The file of a store holds unpublished content. It belongs where the working files belong – not in a folder a
  site serves, not in the image of the site, and not in the repository.

## Verification

`EditorialStoreTests` (22 tests) with a copy of the collection of the repository: it comes out as it went in,
with its Persian translation; what comes out is read by the site; the store keeps it when closed and opened
again; the same file a second time changes nothing; a changed record, a review, a removed record and a changed
translation each become a change with who, when, why, before and after; a file the site cannot use and a published
revision are refused and change nothing; the commands `store import`, `store export` and `store history`.

## Addendum of 2026-11-17 – the acts of an editor (step 11.2)

The store now knows three acts on one record, without a file in between:

* **Putting a record** – in place of the record with its identifier, or new at the end of its group.
* **Taking a record out** – with the texts that translate it, since a translation names only what the collection
  has.
* **Recording a review** of a relation, an event or a statement with a date: reviewed, disputed or withdrawn, with
  who reviewed, on which day, and a note. The record of the review is written into the statement as the file
  format has it (ADR 0009).

Every act is **tried on the whole collection first**: the collection as it would be is read by the readers of the
site, and only a collection they accept is kept. So the store never holds a collection the site could not use,
and it has no rule of its own: that a disputed statement says what is disputed, that a withdrawn one says why,
that nothing may point to a record that is gone, that nothing gives the story away early – each is what a reader
says, returned to the editor with the name of the record. An act that is refused changes nothing. An act needs
somebody and a reason; both are kept in the history of the record.

The store looks for the files a media item names only when it was told where the files of its collections lie;
otherwise an act cannot be refused for a file that is missing, and the check of a written-out file finds it.

Statements can be asked for with the state of their review – all of them, or those that still wait.

Verification: `EditorialActsTests` (26 tests): a review with and without a note; disputed and withdrawn only
with a note, by the words of the readers; a withdrawn statement stays in the store and is no part of what the site
reads; a statement something points to is not withdrawn; a dated statement by its key; put and remove, and what
they are refused for, with the collection unchanged afterwards; the commands `store waiting`, `store review`,
`store put`, `store remove`.

## Addendum of 2026-11-17 – publishing from the store (step 11.3)

* **The store makes the next revision.** `Publish` takes what the store holds and makes revision *n* by the rule
  of [ADR 0009](0009-published-revision.md) – with the same code that makes a revision from a file; the file
  command and the store share one maker, which now works on a collection in memory. The number follows the last
  revision the store published; the editors do not name it and cannot skip one. The revision before, for the
  history and for what changed, is the one the store kept.
* **A revision is kept as it was published** – its file and the translations published with it – and is never
  changed: a row per collection and number. It can be written out again, which is how a site is taken back to a
  revision ([operations](../operations.md)).
* **Publishing changes no record.** The working state stays the working state; the history of a record is the
  history of its changes, not of what was published.
* **A translation published with a revision leaves out the texts of withdrawn statements**, since a revision does
  not hold a withdrawn statement and a translation names only what its collection has. The working translation
  keeps them.
* **Nothing is kept when a revision cannot be made**: every statement that is not reviewed, or reviewed without a
  record, is named, as by the file command.

Not done here: a first revision of the store that follows revisions made from files before the store existed (the
store starts at 1); copying the media files into the content folder of a revision (the editors copy `media/`);
the revisions of comparisons.

Verification: `EditorialPublishingTests` (14 tests): an unreviewed collection is not published and each
statement is named; revision 1 and its record in the store; a site in the public settings reads a revision of
the store from a content folder with nothing of the editors beside it; revision 2 with a changed record and a
withdrawn statement – its history, its translation, the store's working state, and revision 1 unchanged; a
statement changed after its review stops the next revision until it is reviewed again; the commands
`store publish`, `store revisions`, `store revision`. `ContentRevisionTests` (Data) still pass with the
shared maker.

## Addendum of 2026-11-17 – the tool of the editors, to read (step 11.4)

* **A second web app, `Vafadar.Dastan.Studio`**, shows what the store holds: the collections; a collection with
  its groups, what waits for a review and what was published; the records of a group with the state of their
  review; a record as it is written, with every change of it – who, when, why, and each field that differs before
  and after; the statements that wait; the latest changes of a collection; the revisions. A record that was taken
  out keeps the page of its history.
* **It is built like the site**: pages rendered on the server for every request, no script, no style in a page,
  English and Persian from the address, light and dark, the base style sheet and the headers of `Vafadar.Web`
  (ADR 0003, repository ADR 0013). Its own style sheet repeats the colours of the site (ADR 0004).
* **It answers the machine it runs on and nobody else.** The tool shows what is not published and has no
  accounts; until it knows who an editor is (step 11.7), where it runs is its only guard. A request from another
  address is refused before anything else happens, and the allowed host names are those of the machine itself.
  No page is kept by a browser or indexed.
* **It only reads.** No page has a form that changes the store; acting is step 11.5. The store is opened for a
  request and closed with it, and brought to the form the program knows once, when the tool starts – a tool that
  is not told its store, or cannot open it, does not start.
* **A record is shown as it is written** – fields and values, nested as the record nests them. Nothing is
  interpreted, so the page cannot show a record differently from what the store holds and the readers read.

Known limits: a change of a translation shows the whole group of texts that differs, not the single text; the
tool has no search; the Persian font of the site is not part of the tool, which uses the fonts of the machine.

Verification: `StudioTests` (24 tests) over a store with the collection of the repository after some work:
every page and what it lists; what the store does not hold is not found; Persian, right to left, Persian digits;
no page kept, indexed or with a script or an inline style; no form but the choice of the appearance; a request
from another address is refused; the tool does not start without its store. In a browser, on a published build
at the width of a phone, English and Persian: no page scrolls sideways – a table wider than the page scrolls in
its frame –, every link and button is at least 44 px high. Measured, not seen: no picture could be taken.

## Addendum of 2026-11-17 – the tool of the editors, to work (step 11.5)

The tool now acts as well as shows. Every act of the store has a form on the page of what it acts on:

| Where | Form | Act of the store |
|---|---|---|
| The page of a record that is or holds a statement | Record a review: what it found, the day, a note | `Review` |
| The page of a record | Change the record, as the text it is written as | `Put` |
| The page of a record | Take the record out | `Remove` |
| The page of a group | Add a record – never in place of one that is there | `Put` |
| The page of the revisions | Publish the next revision | `Publish` |

* **A form is an ordinary form** sent to the address of its page, with the token of the tool; there is no script.
  An act that was done leads back to the page (a redirect), which shows the record as it is now and says what was
  done – so loading the page again does not do the act again. An act that was refused answers with the page
  itself: "Not done. Nothing was changed.", every reason in the words of the readers, and the form as it was
  filled in.
* **Every act asks who and – except a review, whose note is the reason – why.** The name is what the editor says
  it is; the tool keeps the last one in a cookie to start the next form with, and says on the form that it does
  not check it. This is a convenience, not an identity (step 11.7).
* **A record is changed as text**, the JSON it is written as. It is the one editor that can change every field of
  every kind of record today; forms for the parts of a record are step 11.6. The identifier of a record cannot be
  changed there: a record with another identifier is a new record.
* **The tool adds nothing to the rules.** It checks only what a form needs to be understood – a name, a reason, a
  day, a text that is a record – and hands the rest to the store, which tries the act on the whole collection.
* **Guards.** The forms are reachable from the machine of the tool only; a form without the token of the tool is
  refused; what a page says was done is one of the things that can be done there, never a text from the address.

Not done: writing a published revision out to a folder from the tool (the command `store revision` does it);
changing a translation in the tool (`store import` does it); undoing an act other than by another act.

Verification: `StudioActsTests` (19 tests), each with a store of its own and a browser with cookies that fills
in a form of a page and sends it with its token: a review recorded and refused; a change kept, unchanged and
refused, with the text staying in the form; taking out and what stops it; adding and never replacing; publishing
refused until every statement is reviewed, then kept; a forged form; a forged "done". `StudioTests`: every page
has the forms of what can be done there and no other. In a browser, on a published build at phone width in
Persian: the page of a record with its three forms does not scroll sideways, every field has a label and every
control is at least 44 px high. A form could not be sent from the browser of this session (its window does not
draw); sending is covered by the tests above.

## Addendum of 2026-11-17 – the fields of a record as a form (step 11.6)

The page of a record has a second way to change it: its fields as a form (`RecordForm`, `FieldInput`). The record
as text stays below it.

* **The form is made from the record, not from a description of records.** Every field the record has gets the
  input that fits what it holds:

  | What the field holds | Input |
  |---|---|
  | A text | One line, or several when the text is long |
  | A number | A number; the reading stage (`spoilerLevel`) is a choice of the stages of the collection |
  | Yes or no | A box to tick |
  | A text with a vocabulary (`ContentVocabulary`, by the place of the field: `relations[].scope.kind`) | A choice of its values |
  | The identifier of a record (`from`, `to`, `entityId`, `storyId`) | A choice of the records of that group |
  | Identifiers of records (`sourceIds`, `eventIds`, `depictedEntityIds`, …) | A box to tick for each record of that group |
  | Texts | A text with one in a line |
  | A part with fields of its own (`scope`, `rights`) | Its fields, set in |
  | A list of parts with the same simple fields (`entityAssociations`) | A row for each, and an empty one |
  | Anything else – nothing, a part within a part | The text it is written as |

  So a new field or a new value needs nothing in the tool: the readers and their vocabulary know it, and the form
  follows. The label of an input is the name of the field in the file – the name the guide to the content and the
  reasons of the readers use.
* **What the form sends is the record again.** The fields are read in the order of the record; what the form does
  not show – the identifier, the state and the record of a review – stays as it is. The records a field points
  to keep the order the record names them in, new ones follow in the order of the collection. A text left empty
  takes its field out of the record. A row whose texts are all empty is no row: that is how the empty row stays
  out, and how a row is taken out. Every record of the sample comes back from its form as it was; a test holds
  that for each of them.
* **The form adds no rule.** It refuses only what it cannot understand – a number that is none, a text of a part
  that is not JSON – and hands the record to `Put`, which tries it on the whole collection. A value the record
  has that the vocabulary does not know stays choosable; the readers say what is wrong with it.
* **The form does not add a field.** It changes the fields a record has. A field the record does not have yet is
  written in the record as text.

Not done, and named as step 11.8: the texts of the other languages. A row of an event whose role is translated
cannot be taken out in the tool – the readers refuse a translation of something that is not there, and the tool
has no form for the translation. The reasons of the form itself ("is not a number") are English, as the reasons of
the readers are.

Verification: `StudioFormTests` (15 tests): every record of the sample through its form and back; the inputs of
an event; the form on the page; sent as it stands it changes nothing, for a name, a relation, an event and a
source; a text, a stage and the sources filled in; a row added; a row taken out; a translated row refused with
the reason; an emptied text; a refusal that keeps the form; a number that is none; Persian. Measured in a browser
on the published tool at 375 px, right to left: no overflow, every input with a label, every target at least
44 px, and one change sent from the browser and kept.
