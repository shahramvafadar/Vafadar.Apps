# 0023. A place somebody was at is an entity of a collection when they choose one, and a time there is a stretch of its own

* Status: Accepted; adds to [0018](0018-relationships.md)
* Date: 2026-11-20
* Step: 20.4 of the [delivery plan](../delivery-plan.md)

## Context

ADR 0018 let an account state where it was – a school, a university, an employer, a team – and let others
find it through the place, as far as it allows. The place was words. A review in November 2026 found what
follows from that:

* People were found through a place by the key of its words. Two universities of one name were one place;
  one university under its old and its new name, or written in two languages, was two.
* The rules that decide who is found were told an identifier that was made up on the spot (`place`).
* The context of a relationship – "colleagues at …" – was words too, and two relationships of one kind were
  told apart by them: colleagues at two employers of one name could not be stated.
* An account could not state the same place with the same role twice, whatever the dates. Somebody who
  studied at a university, left and came back could say one of the two, or one long time that was not true.
  This was reproduced by a test before anything was changed.

The collections, meanwhile, know such places as entities: a type (`university`, `company`, `organisation`,
`sportsClub`, `sportsTeam`, `government`), a name in each language, other and earlier names, a seat, and
identifiers of outside catalogues (DOM-003).

## Decision

**A reference.** A statement of the personal side can point at an entity of a published collection by a
reference of two parts, the collection and the entity (`EntityReference`, written `collection/entity`). It
is never a name and never an address of a page. The same identifier in two collections is two references.

**The words stay.** The words the account wrote stay beside the reference: what the place was called when it
was stated. A statement without a reference is allowed as before – most places are in no collection. Words
never become a reference by themselves, and no reference is derived from words: not at writing, not in a
migration, not by a job. The migration of this step adds two empty columns and points nothing at anything.

**The visitor chooses.** A form looks for a place among the entries by the words the visitor writes –
names, other names, earlier names, in every language, with the letters brought together as the search of
the site does (Persian and Arabic forms of a letter find each other). What it finds is offered with what
tells the entries apart: the kind, the seat where one is stated, the collection, the other names. Nothing
is chosen for the visitor: they pick one entry, or "none of these – keep only my words". A form that names
an entry the page did not offer – no such entity, an entity that is no such place, one only a later stage
of a story shows – is refused.

**What everybody may see, and nothing else.** The personal side learns of the collections through one
directory (`Institutions`), built from each collection as a visitor who has read nothing sees it and as the
audience of the site may see it. A place that a story shows only later is not in it: pointing at it would
tell that it exists. The store of the people does not know the collections at all – it keeps the reference
as a text, and who asks tells it which references are one place.

**The same place in two collections** is what the editors say it is, in the one way the collections have:
both entities carry the same identifier of the same outside catalogue, marked `exact`. A `close` or
`uncertain` identifier joins nothing, and neither does a name, a seat or a kind. The closure is taken over
several collections.

**Finding.** "Who was here" asks the store for the affiliations that point at an entry or at one that is
the same place. The rules are those of ADR 0018, unchanged: only who allows this viewer to find them, never
across a block, never a closed account, with the dates only as each shows them, and the same answer when
there is nobody and when there is nobody this viewer may find. Who wrote only words is not found through an
entry, however alike the words are. The search by words stays, compares words, and names the entries that go
by those words so that the other question can be asked. That an entry is public makes nobody's time there
public: the page of the entry leads to the question and names nobody.

**A stretch is a statement.** An affiliation is one stretch at a place in a role. Several stretches at one
place are several affiliations; what lies between them is no attendance, and a question about a year in the
gap finds nobody. "The same statement twice" – which a form sent twice must not make – is the same place
(the same entry, or the same words where neither names an entry), the same role and the same times.

**A relationship** can carry a reference for its context in the same way. Two relationships of one kind are
compared by the entry where one is chosen and by the words otherwise. Pointing an accepted relationship at
an entry, or taking the entry away, is another statement and asks the other side, as every change does.

**An entry that is not published any more.** The statement keeps its reference; pages show the words, with
no link; the account is told so on its own page and may keep the reference through other changes. No other
statement can be pointed at the entry meanwhile. When the entry is published again the statement is what it
was.

**Added on 2026-11-21** (section 23 of the delivery plan): where a relationship is written with its place – on
the page of one's people and on the page of a person – the name of an entry that is published leads to its
page, as the name of a place somebody was at does (`RelationshipCalled`) – also while the relationship waits
for an answer, on the page of the people and on My World. Words, and an entry that is not
published at the moment, stay words. A request to call a relationship something else is one sentence of two
names and stays words too.

## Consequences

* A place that gets another name stays one place for everybody who chose it; the page shows the name of
  today and, where they differ, the words of the statement.
* The copy of one's own data names the reference (`placeEntity` of an affiliation, `contextPlace` of a
  relationship) beside the words.
* A reference is public information – it names a published entity. Which account points at it is as private
  as the affiliation was before.
* The directory is read once per process from collections that do not change while the site runs.
* The pages work without script: looking for a place asks the page again.

## Not decided here

* **Proposing a place to the editors.** A visitor whose place is in no collection writes words; nothing
  carries those words to an editor. Whether and how visitors propose entries is an editorial process of its
  own.
* **Statements in words that could now point at an entry.** Nothing looks through existing statements. An
  account can point its own statement at an entry at any time.
* **Content.** The published collection holds no university, company or club. Everything here was built and
  tested with made-up places that say so; a visitor has something to choose once such content exists and is
  approved.
