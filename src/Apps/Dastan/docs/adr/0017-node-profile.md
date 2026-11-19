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
