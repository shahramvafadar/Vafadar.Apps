# 0015. One PostgreSQL database, and rules the database itself keeps

* Status: Accepted (D-076); takes the place of the SQLite files of [0011](0011-editors-store.md) and [0014](0014-visitor-accounts.md)
* Date: 2026-11-19
* Step: 19.3 of the [delivery plan](../delivery-plan.md)

## Context

Dastan kept two SQLite files – the store of the editors and the store of the visitors – and read published
content from files. That fitted a demonstration on one machine. It does not carry the product: rules such as
"one account per name" or "one claim per person" were kept by looking and then writing, which holds only
while one writer is let in at a time; the records of the editors were bodies of JSON that cannot be asked for
"every relation that ends at this node"; and a file cannot be shared by more than one running site.

## Decision

* **PostgreSQL**, through Npgsql and EF Core. One database, and **a schema for each part**, each with its own
  `DbContext`, its own migrations and its own history table, so that the parts stay apart as the programs do:
  `identity` (accounts of visitors and of editors, [0016](0016-identity-and-mail.md)), `editorial` (the
  working records of collections), `published` (revisions as they were published), `personal` (what belongs
  to accounts: trees, people, relationships, photographs, choices) and `delivery` (mail that waits to be
  sent, notices, reports).
* **An editorial record keeps its written form, and the database knows what it says** (D-081, which changed
  this point on 2026-11-19; it first read "no table of a record and its body"). The body of a record is the
  record as the file of a collection writes it, in a column of the type `json` – checked by the database and
  kept character for character, so a collection is written out as it came in (`jsonb` would reorder its
  fields). What is asked for often – the kind, the name, the reading level, the state of the review – is a
  column **the database computes from the body**, so a column can never say something else than its record.
  Which record names which – the ends of a relation, the sources of a statement, the entities of an event,
  what a media item shows, the stops of a thread – is a table of rows with foreign keys: a row cannot name a
  record the collection does not have, and a record cannot go while another names it. Full normalisation was
  the other choice; it would have made every new field of the content a change of tables and two descriptions
  of one record to keep the same.
* **A published revision stays the exact bytes it was**, with its hash, in `published` – the site reads the
  revision from there, and a revision can still be written out as the file of a collection and brought in
  from one. The site never reads `editorial`.
* **The database keeps the rules.** Each rule that must hold is a constraint or a lock, written down beside
  its table: unique names and mail addresses (compared as they are normalised), one claim per person and
  account, one open relationship of a kind and context between two accounts, the number of a revision, one
  use of an invitation. A limit per account ("at most 200 photographs") is kept by a row of counters that the
  act locks. An act on a collection carries the version it read; the collection has a version that is a
  concurrency token, and an act that finds another is refused and may be tried again – a bounded number of
  times, and only where trying again is harmless.
* **Time.** When something happened in the telling is not a `date` (PostgreSQL begins at 4713 BC): the
  expression of `Vafadar.Temporal` is kept as it is written, with two numbers – the earliest and the latest
  year on one scale – that are indexed. When a row was written is `timestamptz` in UTC.
* **Files stay files**: photographs and media lie on a volume; the database keeps what is known about them.
* **Running it.** The site and the tool of the editors are told one connection (`Dastan:Database`); its
  password is a secret the owner enters and is never in the repository. `compose.yaml` starts a PostgreSQL
  for development and for the tests, with a password that is good for this machine only. Migrations are
  applied by a command of the operator, not by a site that starts.
* **Tests** run against a real PostgreSQL – in the Compose project `test` on a developer's machine and as a
  service of the job in CI; a class of tests has a database of its own. For every rule above a test sends
  requests at the same moment.
* **Nothing is carried over** from the SQLite files (D-080): nobody real has an account, and collections come
  in from their files. The SQLite packages leave Dastan.

## What is built (2026-11-19)

The store of the visitors (`personal`). A database is made with `--encoding=UTF8 --locale=C`, so that text is
compared and ordered by its code points, as the store always did; where names are shown in order, the site
orders them for the language of the page. The rules of that store that count before they add are kept by locks of the transaction, one per account, tree,
photograph or pair of accounts, taken in one order, with a test of requests at the same moment for each.

The store of the editors (`editorial`), in the same way: Npgsql, one first migration, the tool told the
connection in `Studio:Store`, tables made by `database migrate` and never by the tool, and `store copy` gone
in favour of `pg_dump`. An act on a collection, a change of a comparison and a change of the list of the
editors each take a lock of the transaction first, so that acts of the same moment are done one after the
other; a test lets eight editors put a record at once.

The records as D-081 decides them: bodies, headers, translations, revisions, documents and the "before" and
"after" of every change are `json`; `Records` has the computed columns `Kind`, `Label`, `SpoilerLevel` and
`ReviewStatus` and a check that a body is the record of its own identifier; `RecordLinks` holds which record
names which, brought up to date in the same step as every act, publishing included. Tests ask the database
directly: a link to a record that is not there, the removal of a record that is named, a body that is no JSON
or is another record's, and a write to a computed column are each refused.

The published revisions (`published`): `Revisions` – the file and the translations as `json`, with the
SHA-256 of both – and `Releases`, to which a word is only ever added: a release names the revision the site
shows from now on, a withdrawal names none, and the last word about a collection counts. Publishing releases
nothing. A site told `Dastan:Published` reads the released revisions when it starts, with a plain question to
that schema – it has no model of the editors' tables and reads no other schema of theirs – compares each hash
and does not start on a revision that was changed, on a database without the tables, or without anything
released. So an earlier revision can be shown again (R-PUB-07) by releasing it.

What was owed after that part and is built: the page of the revisions in the tool says which revision the
site shows and a publisher releases or withdraws there; a comparison is released the same way
(`published.Comparisons`: the text as it was at that moment, with its hash) and a site that reads from the
database reads its comparisons there; an act says which version of a record it was decided on and is refused,
under the lock of the collection, when the record is at another by then; the versions of a record are counted
through a time it was not there. Reports and notices are the tables `delivery."Reports"` and
`delivery."Notices"`, reached by the site, the tool and the command with plain questions (`Vafadar.Dastan.Data`);
`Dastan:Reports`, `Dastan:Notices` and `Studio:Reports` say whether, no longer where.

Who may do what: `database grant site|tool <role>` gives a role of the database what its program needs and
takes the rest – the site reads `published`, writes `personal` and adds to `delivery`, and cannot read
`editorial`; the tool works in `editorial`, adds to `published`, reads and settles in `delivery`, and cannot
reach `personal`; neither changes a published revision. The limits per hour of the site count by the network
of a connection (the first 64 bits of an IPv6 address) and no longer by the exact address.

**Two points of the decision above were built otherwise than they were first written**, and are said here so
that the text and the program agree. A limit per account is kept by a lock of the transaction for that
account, taken before the count – not by a row of counters: the lock needs no second place that could
disagree with the rows it counts. And an act carries the version of the *record* it read, checked under the
lock of the collection – not a version of the whole collection: two editors who change different records of
one collection at the same moment are both kept, which a version of the collection would refuse.

**What of this decision belongs to later steps**, each named where it is built: the schema `identity` and
the mail that waits in `delivery` (step 19.4, with the accounts – one account per name and per mail address
and the single use of an invitation are written once, there); the two indexed numbers of a time in the
telling (step 19.9, the first that asks "what held at this time").

**Seen on the way and not solved here.** The stores speak to the database synchronously. Under a burst of
requests that all wait – as the tests of this step produce on purpose – the threads of the process ran out and
commands stood half sent for half a minute; the test projects now start with a wide pool. A site under real
load would meet the same limit later. It belongs to step 19.11 (load), and is written here so that it is not
found twice.

## Consequences

Backup is `pg_dump` of the database together with the volume of the files, and bringing both back is tried
and written into the operations guide before the step is done. A second site can run beside the first. A
host has to offer PostgreSQL or room for its container.
