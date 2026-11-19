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
* **Editorial records are relations**: nodes, the two ends and the kind of every relation, memberships of a
  collection, media, sources and reviews have columns and foreign keys. What is asked for often has a typed
  column; only what is rare and belongs to one kind is checked JSON (`jsonb`) with a version. No table of
  "a record and its body".
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

## Consequences

Backup is `pg_dump` of the database together with the volume of the files, and bringing both back is tried
and written into the operations guide before the step is done. A second site can run beside the first. A
host has to offer PostgreSQL or room for its container.
