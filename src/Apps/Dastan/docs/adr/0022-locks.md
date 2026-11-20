# 0022. Every act that looks before it writes holds the locks of what it looks at

* Status: Accepted; adds to [0015](0015-postgresql.md) and [0018](0018-relationships.md)
* Date: 2026-11-20
* Step: 20.2 of the [delivery plan](../delivery-plan.md)

## Context

ADR 0015 gave the acts that count before they add a lock of the database, taken in a transaction: at most so
many trees, one request between two accounts, one claim in a tree. A review of the source in November 2026
found acts that look and then write without one:

* `End`, `Cancel` and `Answer` changed a relationship without the lock of its two accounts. Two last
  relationships of two accounts, ended at the same moment, each saw the other still standing and left the
  memberships of circles and trees in place – somebody stayed in a tree of an account they showed nothing with
  any more. Agreeing to another name for a relationship while it ended failed with an error of the database.
* `SetCircle` and `SetTreeMember` read "the two show an accepted relationship" and wrote the membership
  without a lock: shared with somebody at the moment the relationship ended or one blocked the other, the
  membership was written after everything else had been taken away.
* `Link` looked for a loop and wrote without the lock of the tree: two links at the same moment that close a
  loop – directly, or over several steps – were both written.
* An act that ran inside a transaction took no lock at all: "a transaction is open" stood for "the right locks
  are held", and nothing checked it.

Each of these was reproduced by a test that failed before the change (`TogetherTests`): two stores with a
connection each, held at the place where both have decided and neither has written.

## Decision

**One rule.** An act that decides on what it read takes, in its transaction and before it reads, the lock of
everything the decision hangs on, and reads only then.

**The locks** are advisory locks of the transaction (`pg_advisory_xact_lock`), named by what never changes:

| Lock | Named by | Stands for |
|---|---|---|
| `account:<id>` | the account | what the account owns and how much of it: its circles, trees, photographs, albums, invitations, open requests |
| `pair:<a>:<b>` | two accounts, the smaller identifier first | everything between the two: requests, relationships, a block, and whether one is in a circle or a tree of the other or tagged on a photograph of the other |
| `photo:<id>` | the photograph | its tags, objections and audience |
| `restrictions` | – | the numbers of the restrictions |
| `tree:<id>` | the tree | its records, links, claims, members and publication |

**The order.** A transaction takes its locks in the order of their names – the order of the table above. It
may take another lock later, but only one that comes after every lock it holds; a lock that would come before
one that is held is refused with an error. Two acts can therefore never wait for each other. An act inside
another one asks for its locks like any other: what is held is held, what comes later is taken, what would
come earlier is refused. That a transaction is open proves nothing.

Later locks are needed in one place: when the last relationship of two accounts goes, the trees they share
are known only after the lock of the two is held (nothing between them is shared anew from then on), and the
locks of those trees come after it in the order.

Nothing is one lock for everybody: accounts, pairs and trees that have nothing to do with each other do not
wait for each other (`Links_in_two_trees_do_not_wait_for_each_other`), and nothing assumes that the site runs
once.

**Which rule, which acts, which locks.**

| Rule | Acts that could break it | Locks |
|---|---|---|
| One request or relationship of a kind between two accounts; a request counts against the open requests of who asks | `Ask`, `AskChange` | `account` of who asks, `pair` |
| A relationship changes its state once; the one it replaces is still there when it takes its place | `Answer`, `Cancel`, `End` | `pair` |
| Nobody is in a circle or a tree of an account they show no accepted relationship with | `End` (the last one), `Block`, `SetCircle`, `SetTreeMember`, `RepairMemberships` | `pair` of the owner with everybody named; `End` and `Block` then the `tree` of every tree the two share; `SetCircle` also the `account` of the owner; `SetTreeMember` also the `tree` |
| A tag names the owner or somebody the owner shows an accepted relationship with; at most so many tags | `TagPhoto`, `Block` (takes the tags away) | `pair`, `photo` |
| A relationship is shown to a circle of the account that exists | `Show`, `RemoveCircle`, `AddCircle` | `account`; `Show` also the `pair` |
| Nobody is their own ancestor; two records are linked once | `Link`, and with it everything else that changes a tree: `Unlink`, `AddPerson`, `ChangePerson`, `DescribePerson`, `RemovePerson`, `RenameTree`, `RemoveTree` | `tree` |
| A tree is made public once it was asked for and while nobody it names stands in the way (the page of a public tree asks that again each time it is shown) | `AskPublication`, `Consent`, `Publish`, `Unpublish` | `tree` |
| One claim of an account in a tree, only by who may see it; a claim is reviewed once | `Claim`, `ReviewClaim`, `Unclaim`, `SetTreeMember`, the end of the last relationship | `tree` |
| The counts of an account (trees, circles, albums, photographs, invitations, kept pages, affiliations) | the acts that add | `account` |

An account that is closed is not taken out of anything: every page asks whether the accounts it shows are
open, and a closed account is deleted with everything of it after 30 days. Closing takes the lock of the
account for its own steps and needs none of the others.

**What the database keeps by itself**, where a rule can be said to it (migration `KinshipRules`): a link never
joins a record with itself, and two records of a tree are linked once, whichever is named first. That nobody
is their own ancestor over several steps no constraint can say; the lock of the tree keeps it. The membership
of a circle and of a tree already had the owner's pair as its key.

**What was written before.** The migration removes links of a record to itself and the later of two links
between the same two records – the store would have refused both. The command `people repair` counts who is
in a circle or a tree without an accepted relationship with its owner and, with `--change`, takes them out as
the end of the last relationship would have; it names the trees with a loop and changes nothing there, because
which link is the wrong one only who knows the family can say. It can be run as often as wanted. No store of a
site that people use exists yet; the command is run once after the version that brings it.

**Afterwards.** Nothing has to be told to the pages: every page, every JSON, every picture, the export and
the counts read memberships and relationships when they are asked for, so the next request after the end of
a relationship is refused.

## Consequences

* An act of this kind costs one or a few locks more. Acts on the same two accounts or the same tree wait for
  each other; nothing else does.
* A new act that looks before it writes has to say its locks, and to say them in the order. An act that asks
  for an earlier lock inside another one fails loudly – in the first test that runs it, not on a busy day.
* The store has a place for tests to hold an act between deciding and writing (`Reached`). Nothing else sets
  it.

## Not decided here

* Whether somebody is told when they are taken out of a circle or a tree by `people repair`. Nobody is: there
  is nobody yet whom it could concern.
