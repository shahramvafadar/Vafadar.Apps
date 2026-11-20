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

**Not built yet:** the people of a private tree as nodes with this contract – times instead of years, places,
a picture among the owner's photographs, and what may be kept about a living person who was not asked
(PRIV-003) – which is part four of the step; the card in a graph, which the graph of step 19.8 draws.

