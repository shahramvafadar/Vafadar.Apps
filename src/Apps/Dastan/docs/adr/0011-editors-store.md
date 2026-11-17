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
