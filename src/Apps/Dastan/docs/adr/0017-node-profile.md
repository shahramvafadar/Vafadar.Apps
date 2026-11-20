# 0017. Every node has a profile

* Status: Accepted (D-078)
* Date: 2026-11-19
* Step: 19.5 of the [delivery plan](../delivery-plan.md)

## Context

A public entity had a page with names, statements and relations; a person of a private tree had a name, two
years and nothing else; a node in a drawing was a box with a name. The product is about nodes – people,
families, places, organisations, works, events – and each must be recognisable where it stands: by a picture
and by the few facts that matter for what it is.

## Decision

* **One contract, `NodeProfile`**, for every node, public or private: the stable identity, the kind, the
  names in the languages of the site, a summary, **a profile picture**, and **the facts its kind needs**.
* **Facts are typed and belong to a kind.** A table in the product documents, and the same table in code,
  says for each kind of the ontology which facts are essential and which may be added – a person: born,
  died, where, what they were; a family or a house: founded, seat; an organisation: founded, closed, field,
  seat; a team: founded, home, colours; a place: where, of what it is part; a work: by whom, when; an event:
  when, where, who took part. A fact is a time (an expression of `Vafadar.Temporal`, never a year from 1 to
  9999), a place, another node or words, with its source where the node is public. A fact nobody knows is
  said to be unknown; it is not left out and not made up.
* **The picture** is a media item of the collection for a public node, and one of the owner's photographs
  for a private one. It is shown to exactly those who may see that picture: choosing a private photograph as
  a profile picture makes nothing public. Who may not see it, and a node without one, see a drawing of the
  kind.
* **A living person who was not asked** (PRIV-003): the record of a living person that no account has said
  is theirs holds only what a tree needs to name them – a name, the years, how they are related – and no
  picture, no places and no words about them, until the person has an account and agrees. The same holds for
  children; what is kept about a child is decided by an account that is their guardian in the tree, and the
  child's own account takes over when it exists.
* **Two views of one profile**: the page (everything, with sources, timeline, gallery and what may be
  changed) and the **card** (identity, name, picture, up to three facts) that a graph, a list and a search
  result show. Both come from the projection that decides what a visitor may see and has read to.
* **A person of a tree is a node** with the same contract, kept in `personal`. An account stays apart from
  the person it says it is; the same person in two trees is two records until somebody with the right says
  they are one, and then their facts, sources and audiences stay apart.

## Consequences

The editors' tool asks for the essential facts of a kind and says which are missing. The check before a
revision is published names nodes without a picture as a note, not as a fault. Drawings no longer invent
their own idea of what a node shows.

## What is built

**The public nodes (2026-11-20, step 19.5, parts one to three).**

* *The contract.* `NodeProfile` in `Vafadar.Dastan.Core`: the entity, its picture as this visitor may see it,
  the essential facts of its kind – every one, stated or not – and the added facts that are stated; `Card` is
  the first three stated ones. `ProfileFacts` is the table: 31 kinds of the vocabulary, 29 kinds of fact,
  each a time, a place, another node or words. The same table is DOM-018 of the product documents, and a test
  holds the two together.
* *In a record.* An entity has `facts` and `picture` ([collection-format.md](../collection-format.md)). The
  readers refuse a fact its kind does not have, a fact that says something else than its kind of fact takes,
  a place that is none, a fact stated twice that is stated once, and a picture that is no image of the
  collection depicting the entity. The words of a fact are translated like every other text.
* *What a visitor is told.* A fact is told from its stage on and – where it names another node – when that
  node may be seen. **An essential fact that is not told reads exactly like one nobody stated**: a profile
  never says that there is more to come. The picture is there only when the media item may be seen – by
  stage and by the audience it was cleared for; a test shows an image that is not cleared for the public
  missing from the public's profile.
* *On the site.* The page of an entity has a section "Profile": the picture with the title of its item, or a
  drawing of the kind and the words that there is no picture yet; the facts as a list, with "not known" for an
  essential one. The list of a collection shows each subject as its card. No script.
* *For the editors.* The form of an entity has a row for every fact its kind has, with the input that fits
  what the fact says and its Persian words below; an essential fact that is not stated is marked. Before a
  revision is made, the page lists what the profiles lack – no picture, an essential fact – as notes that
  stop nothing. A time can be stated as not known and as not applying. Two entities that are one keep the
  facts and the picture of the one that stays and take what it lacks from the other.

**Decided while building, and different from what the decision may be read as:** a fact has sources and a
stage, and **no review state of its own** – like another name of an entity it belongs to the record; a
statement that needs its own review is a dated statement or a relation. **The sample states two facts and no
picture**: its museum images are references that may not be shown, and the diagram of the project is no
portrait of anybody, so every profile of the sample shows the drawing of its kind and says so.

**The people of a private tree (2026-11-20, step 19.5, part four).**

* *Times, not years.* The record of a person states when they were born and died as an expression of
  `Vafadar.Temporal` (`PersonTime`): a year, a month or a day of the Gregorian or the Solar Hijri calendar,
  exact or about; a time known only to lie between two years; or the statement that it is not known. The
  store refuses every other shape of time and a death before the birth – in one calendar as far as both times
  say (year, month, day), across two calendars by the years of the common era the times can lie in. The form
  keeps the one field for the year and folds the rest away below it.
* *What is kept of whom (PRIV-003).* Beyond the name, the times and the family a record can say where
  somebody was born and died, a few words about them, and a picture. Who may say it depends on who the
  person is: **somebody who died** – who may change the tree; **somebody whose account the record is** – that
  account and nobody else, not the owner of the tree either; **a child** (a living person whose record says
  a birth less than 18 years ago) – an account that is a parent of the child in this tree, and nobody else;
  **a living person nobody asked** – nobody: the store keeps nothing, the form is not there, and the page says
  why. The day of birth of a living person is theirs to say as well: a record made or changed by somebody
  else says the year. When an account says that a record is not them after all, what it wrote about itself
  goes with it; when a child is grown, what a parent wrote is not shown any more.
* *The picture* is one of the photographs of who chooses it. It is shown to exactly those who may see that
  photograph – the store asks the rule of the photograph with every view – so the tree widens the audience
  of no photograph. Everybody else sees the drawing of a person. A photograph that is removed is no picture
  any more.
* *On the site.* Every person has a page below the tree (`/account/trees/{tree}/person/{id}`, and
  `/trees/{tree}/person/{id}` for a tree that is public): the picture or the drawing, the times as they were
  stated, the places, "not known" for what is not stated, the words, the family with ways to the others, and
  the form for who may say more. A public tree shows the picture only when that photograph is public. No
  script, and no search engine is asked in.
* *The copy of one's data* writes the times as they are kept, the places, the words and whether there is a
  picture.

**Decided while building:** the age below which a parent decides is **18**, not the 16 from which somebody may
have an account (D-061): from 16 on a person can say the record is theirs and then decides alone; between 16
and 18 without an account a parent in the tree still does. A record of a child stays out of a public tree as
before – a living person without an account stands in the way of making a tree public. The places are words,
not nodes: a private tree has no places of its own to point at, and pointing at a place of a published
collection is part of the relationships of step 19.6.

**Not built yet:** the card in a graph, which the graph of step 19.8 draws.

