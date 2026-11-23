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
  own. *Decided by the owner and built on 2026-11-23 – see "A place proposed to the editors" below.*
* **Statements in words that could now point at an entry.** Nothing looks through existing statements. An
  account can point its own statement at an entry at any time. *Since 2026-11-23 the page of the places says
  so where an entry goes by the words of a statement – see below. Nothing is joined by a name.*
* **Content.** The published collection holds no university, company or club. Everything here was built and
  tested with made-up places that say so; a visitor has something to choose once such content exists and is
  approved.

## A place proposed to the editors (2026-11-23, D-085)

The owner chose a way for a visitor to tell the editors of a place that is missing, with somebody who looks
at what comes in.

1. **A proposal is the place, never the person.** Who signed in can propose a place on the page of the places
   they were at: its name, its kind – one of the types people can have been at (`Institutions.Types`) –, and,
   as far as they say, where it is, a public page about it (an `https` address) and a note. It is kept in the
   table `delivery."Proposals"` (`ProposalBox`) with the day and the language – no account, no name, no
   address. So a proposal cannot be answered; the page says so before it is sent.
2. **Proposing states and publishes nothing.** It is no affiliation of the visitor and no entry of a
   collection. An entry is made by an editor like any other name, reviewed and published like any other.
3. **The site takes proposals when it takes reports** (`Dastan:Reports`): both are words of visitors for the
   editors, kept in the same database and read with the same tool. One sender proposes five places in an
   hour at most; a box of 500 that wait takes no more until an editor has decided.
4. **The editors decide in their tool** (`/proposals`): the same place proposed several times – written
   another way, in another case, with the letters of Arabic for those of Persian – stands together and is
   decided once, as added or as getting no entry. A place that was added can be noted with its entry
   (`collection/name`, which the store has to have); a link begins the form of a new name with the name and
   the kind that were proposed. A decision changes nothing in the store.
5. **The loop closes without the site knowing who proposed.** A statement in words keeps its words. Where an
   entry of a collection goes by those words now, the page of the places says so beside the statement and
   leads to the form that offers the entry; the visitor chooses it or not. That is all the answer a proposal
   gets, and it needs no record of the proposer.
6. **Kept as long as a report.** `reports forget` deletes proposals an editor decided more than 90 days ago –
   or before a day that is named – with the reports; one that waits is never deleted.

The table was written into the history of the changes of form by hand (`20261123120000_Proposals`): the tool
that writes such a change could not be run on the machine of that day. `ProposalBoxTests` asks the database
whether the model has a change no change of form carries, so that a slip of the hand is found.

Tests: `ProposalBoxTests`, `PlaceProposalPagesTests`, `StudioProposalsTests`.
