# 0018. Relationships are records of their own, and two people can have several

* Status: Accepted (D-078); changes the relationships of [0014](0014-visitor-accounts.md)
* Date: 2026-11-19
* Step: 19.6 of the [delivery plan](../delivery-plan.md)

## Context

The store refused a second relationship between two accounts whatever its kind: a friend could not also be a
colleague. The product documents say the opposite, and life does.

## Decision

* **Each relationship is a record**: the two accounts, the kind, the direction where a kind has one (parent
  of), the **context** (the employer, the school, the team – words, or a node), the time it holds for, the
  state of each side's consent, and the audience each side chose.
* **Several at once.** Between the same two accounts there is at most one open relationship of the same kind
  and context; relationships of different kinds, or of one kind in two contexts, stand side by side.
* **What excludes what** is written down as groups: between two people only one family role holds (nobody
  is parent and sibling of the same person), and a parent is a parent by birth, by adoption or as a guardian
  – three kinds of the role, of which a child can have each from different people, and a guardianship has a
  time it holds for (R-PER-02); "friend" and "close friend" are steps of one ladder, so one of them;
  everything else is free. The links of a family tree follow the same kinds.
* **Told by mail** (R-PER-03, PRIV-007): being asked for a relationship, being tagged, and being asked to
  agree to something public each send a mail through the outbox of [0016](0016-identity-and-mail.md), which
  an account can turn off kind by kind. A relationship whose kind is changed is agreed to again.
* **Blocking** (PRIV-009): an account can block another; the blocked one cannot ask, tag, invite or find it,
  and is not told.
* **Seen as somebody else** (PRIV-015): an account can look at its own page, a tree and a photograph as the
  public, as a circle or as one named account would see it – drawn by the same projection, not by a guess.
* **Consent stays two-sided** and each relationship is agreed to by itself. Each side chooses its audience;
  a third person sees a relationship only where both audiences let them.
* **Affiliations** – a person and an organisation, with what they were there and when – are kept in the same
  way, and an account chooses whether it can be found through them ("who was at this school in these
  years"). The rules that existed only as a library are now used.
* A drawing keeps every relationship apart: its own line, label and direction, selectable by itself.

## Consequences

The tests that held "one relationship per pair" are replaced. Ending one relationship leaves the others.

## What is built

**Step 19.6, 2026-11-20 – in five parts.**

* *Records of their own.* A relationship has its kind (`RelationshipCatalogue` in `Vafadar.Dastan.Core`:
  thirteen kinds), for a kind with a direction who holds the role, where it holds (words, up to 120
  characters), and from when until when (expressions of `Vafadar.Temporal`). Between the same two accounts
  several stand side by side; the same kind in the same place is there once.
* *What excludes what.* Two groups: **family** – family, parent, adoptive parent, guardian, sibling, partner –
  and **friendship** – friend, close friend. Two people hold one kind of a group at a time; colleague,
  classmate, teammate, neighbour and "another kind" are free. A child has a parent by birth, an adoptive
  parent and a guardian in different people.
* *Called something else.* Changing a relationship is asking the other side: the request names the
  relationship it would replace, the relationship holds as it is until the other side agrees, and then the
  new one takes its place with the audience each side had chosen. Declined or taken back, nothing changed.
* *Each by itself.* Each relationship is agreed to, shown and ended alone. Ending one leaves the others; only
  when the last one between two accounts ends do they leave each other's circles and trees.
* *Blocking.* An account blocks another: what the two showed ends at once – relationships, requests, circles,
  shared trees, tags on each other's photographs. The blocked account asks and reads what a wrong name gets
  ("no such account"), does not find the page of the account, is not found through a place, and an invitation
  by mail to the address of somebody who blocked it reads as sent and is not. It is told nothing. Taking a
  block back restores nothing.
* *Affiliations.* An account states where it was, what it was there, since when, until when and when it
  graduated. Who sees the place and may find the account through it, and who sees when, are two choices; the
  dates never reach more people than the place. A search by a place and – if wanted – a year with one of
  three questions (was there during, began in, graduated in) is answered by `AffiliationSearch` of step 15.2,
  which is thereby used for the first time. Nobody and nobody-you-may-find read alike.
* *Seen as somebody else.* An account looks at its own page and at a tree of its own as the public, as one of
  its circles or as an account it shows a relationship with – drawn by the scopes that draw the real page.
* *The links of a tree* are parent (by birth), adoptive parent, guardian and partner; a guardianship can say
  from when until when; two records are linked once.

**Decided while building, and different from what the decision may be read as:**

* **The context of a relationship and the place of an affiliation are words, not nodes.** Two accounts are
  found through the same place when they write the same name, compared without case and outer spaces.
  Pointing at an entity of a published collection would need the store of the people to know the published
  collections, which it does not; nothing in this step needs it.
* **A year is compared in the calendar it is asked in.** The rules of step 15.2 count years of the common era
  only. An affiliation written in the calendar of the question is compared year against year, exactly; one
  written in another calendar is brought over as the years it can lie in, and the answer may be "possibly" –
  shown as such, never as a plain yes.
* **A look as a circle or as a named account shows of a relationship what the account itself lets them see,**
  not what the other side lets them see: that is the other side's choice and is not told to anybody, and the
  page of the look says so. For the same reason it leaves out photographs of others. A look as the public is
  exact – what everybody sees, the account may see too.
* **A tree is shared with accounts, not with circles:** looked at as a circle, a tree is what the public sees.
* **Blocking does not hide what is public.** A blocked account that signs out sees what everybody sees.
* **Not built here:** the drawing that keeps every relationship apart is the graph of step 19.8.
