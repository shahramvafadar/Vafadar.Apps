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
