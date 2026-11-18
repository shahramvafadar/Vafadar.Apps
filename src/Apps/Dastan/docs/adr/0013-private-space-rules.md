# 0013. The rules of private spaces, as a library before anything is kept

* Status: Accepted for the rules that document 06 accepts; the rest follows its proposed designs and changes
  when the owner decides otherwise
* Date: 2026-11-18
* Step: 15.2 of the [delivery plan](../delivery-plan.md)

## Context

[Document 06](../product/06-private-spaces-consent-and-privacy.md) describes what belongs to one person:
family trees, relationships two people show together, photographs, being found through a university. It
accepts a few foundations – a relationship needs the other person's acceptance; an invitation, a claim and an
acceptance are separate acts; being found through an organisation needs permission; an approved tag does not
widen an audience; a record, an account, a circle and a filter are different things – and proposes the
designs around them. It leaves open who may have an account and how an identity is proved, what is published
about living people, shared trees, the end of consent and copies without a connection (Q-05, Q-06, Q-07,
Q-13, Q-14).

None of that can be offered to anybody before the owner has decided: it keeps data about people. But the
rules do not depend on where data is kept or how somebody signs in – and they are the part that must not be
wrong.

## Decision

The rules are written as a library, `Vafadar.Dastan.Core.Personal`, with the ten acceptance examples of the
document as its tests. **Nothing reaches it**: no page, no form, no store, no account. It keeps nothing and
knows nobody; a test hands it made-up accounts.

* **`AccessScope`** (PRIV-001): the explicit scope of one item – its owner alone, a circle of accounts, or
  everybody. A viewer without an account sees what is public. A scope names accounts and nothing else.
* **`SocialRelationship`** (PRIV-007, PRIV-008, PRIV-009): draft, requested, accepted, rejected, cancelled,
  ended. Only the side that was asked accepts or declines; nobody does it for somebody else (PRIV-005). An
  act done twice changes nothing and tells nobody again; an acceptance after the request was taken back
  creates nothing. A relationship is seen by its two sides in every state and by anybody else only when it is
  accepted **and both sides allow that viewer** – each side sets its own audience and no other.
* **`Invitation`** (PRIV-006): for one purpose, until a day, taken back by who sent it, used once. Using it
  makes an account and nothing else: no relationship is accepted and no record is claimed by it.
* **`IdentityClaim`** and **`Matching`** (PRIV-004): the same mail address is a suggestion for a reviewer;
  nothing is merged and no scope grows. A claim is confirmed by somebody who is not the claimant, once.
* **`Affiliation`** and **`AffiliationSearch`** (PRIV-010, PRIV-011, PRIV-012): being found through an
  organisation and the dates of it are allowed separately. A search asks the permission first, so neither the
  list nor its count says anything about who may not be found. "Attended during", "entered in" and
  "graduated in" are three questions; an attendance known only between two times is found as possible
  (`YearCover`, step 14.2). People found together are a list, never a relationship.
* **`SharedMedia`** (PRIV-013): an approved tag connects an item with a person and shows it to nobody who
  could not see it before. Every part of an item – a thumbnail, a poster, captions, a transcript, a stretch
  of a video – has the answer of the file. The day an item was taken and the day it was shared are two
  facts.
* **`PublicationCheck`** (PRIV-014): before something private is made public, what would become public with
  it is named with whether it may be. The check publishes nothing.

## Consequences

* When accounts are decided, the rules exist and are checked; what is built then is where things are kept and
  how somebody signs in. The rules are asked **before** anything is read for a viewer, as document 06
  demands: a store has to hand the library what it holds and show only what the library lets through.
* The library says nothing about children and guardians (PRIV-003), blocking and repeated unwanted requests
  (PRIV-009), exports (PRIV-016) and copies without a connection (PRIV-017): these need the owner's decisions
  on what the rule is, not only on where it runs.
* A rule that the owner changes changes a test with it. The tests are named after what document 06 says, so
  a change is found by its sentence.

## Not done

Accounts, sign-in, sending an invitation, a store of private spaces, family trees of private records,
personal photographs, "view as another audience" (PRIV-015), and every page of it.

## Verification

`PrivateSpaceRulesTests` (21 tests): the ten acceptance examples of document 06 – friendship, invitation,
identity, university, time, photo, publication, revocation, recent shares, the routes to a video – and the
rules under them: who accepts, an act done twice, an acceptance that comes too late, an invitation used once,
a claim reviewed by somebody else, dates that are hidden, an attendance that is uncertain, the scope itself,
and who changes it. Everybody in the tests is made up.
