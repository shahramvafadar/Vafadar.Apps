# 0011. The store of the editors: records with their history, beside the file – not in place of the site's input

* Status: Accepted (D-045)
* Date: 2026-11-17
* Step: 11.1 of the [delivery plan](../delivery-plan.md), section 12

## Context

Until now a collection is one file that the editors change by hand. That is enough for one collection and one
person. It is not enough for editors: a file does not say who changed a statement, when and why, what it said
before, or what is waiting for a review (GOV-005). The plan names "relational storage with editorial tools" as
the phase after the pilot's first steps (document 20), and says to decide the provider "from actual needs and
operating cost".

What exists and must keep working: the readers that decide what a usable collection is (`PilotSample`), the
rule of what everybody may see (ADR 0009), the `revision` command, and a public site that is given the content
folder of a published revision and nothing else (ADR 0010).

## Decision

1. **The store is the working place of the editors, not the input of the public site.** The site keeps reading
   a published revision from a folder. A revision is made from the store, written out and deployed as before.
   The public site never opens the store, so the store adds nothing a visitor can reach and nothing a host has
   to run.
2. **A collection lives in the store as its records.** Each source, entity, relation, event and media item is a
   row: its collection, its group, its identifier, its place in the group, and the record itself as it is written
   in the file of a collection (JSON), with its review state beside it for asking "what is not reviewed". The
   rest of the file – edition, reading stages, publication, notes – is the header of the collection. A
   translation is a row per language.
3. **The file format stays the one description of a record.** The store does not have a second model of an
   entity or a relation in tables of its own. What a record may say is decided by the readers, which read what
   the store writes out; a rule added to a reader holds for the store without a change of the store.
4. **Every change of a record is kept**: who, when, why, the record before and after, and the version it led
   to. Changes are only added. A record that is taken out leaves its history.
5. **In and out is the file of a collection.** `Import` takes a working file with its translations: the first
   time everything comes in, afterwards what differs becomes changes. `Export` writes the file back; it reads
   as what went in – every field, every record, the order of every group. A file the site cannot use and a
   published revision are refused and change nothing.
6. **SQLite, through EF Core, in a file the editors name.** A few editors and one collection need no database
   server; a file is backed up by copying it and costs nothing to run. The schema is made and changed by
   migrations. The model uses nothing that is special to SQLite, so another database can take its place when
   there is a reason – several editors at once over a network would be one.
7. **A project of its own**, `Vafadar.Dastan.Store`, used by the command of the writers and later by the tools of
   the editors – not by the website, which would carry a database it must not use.

## Considered and not chosen

* **Tables for entities, relations, events, media and their parts.** It is what "relational" suggests, and it
  would let a database ask "all relations of type parentOf". But it is a second description of every record
  beside the file format, to be kept equal to the readers field by field, for questions nobody asks yet: the
  site answers them from the read model. If the editors' tools need such a question, the review state shows how
  a field moves beside the record without the record being taken apart.
* **PostgreSQL in a container** (decision 8 of the plan allows a database there). A service to run, to back up
  and to secure for a working place of a few people. Kept as the path when the store has to be shared.
* **The shared library `Vafadar.Data`.** It is written for an app's database on a device – local profiles,
  encryption, backup to a user's cloud. None of that is the store of editors; EF Core is used directly.
* **The store as the input of the public site.** The site would need the database, its credentials and its
  availability to show a page, and what is not published would lie one mistake away from a visitor.

## Consequences

* The file of a collection is still what a writer – a person or an assistant – works with and hands over
  (D-042). Bringing it into the store is how its changes get an author, a time and a reason.
* A review written into a file is a change of its statement like any other; the store does not yet know reviewing
  as an act of its own. That, withdrawing, and publishing from the store are the next steps (delivery plan, 12).
* The store does not check who the editor is: the name is what the command is told. An account of an editor is a
  step of its own and needs the owner's decision on how editors sign in.
* The file of a store holds unpublished content. It belongs where the working files belong – not in a folder a
  site serves, not in the image of the site, and not in the repository.

## Verification

`EditorialStoreTests` (22 tests) with a copy of the collection of the repository: it comes out as it went in,
with its Persian translation; what comes out is read by the site; the store keeps it when closed and opened
again; the same file a second time changes nothing; a changed record, a review, a removed record and a changed
translation each become a change with who, when, why, before and after; a file the site cannot use and a published
revision are refused and change nothing; the commands `store import`, `store export` and `store history`.

## Addendum of 2026-11-17 – the acts of an editor (step 11.2)

The store now knows three acts on one record, without a file in between:

* **Putting a record** – in place of the record with its identifier, or new at the end of its group.
* **Taking a record out** – with the texts that translate it, since a translation names only what the collection
  has.
* **Recording a review** of a relation, an event or a statement with a date: reviewed, disputed or withdrawn, with
  who reviewed, on which day, and a note. The record of the review is written into the statement as the file
  format has it (ADR 0009).

Every act is **tried on the whole collection first**: the collection as it would be is read by the readers of the
site, and only a collection they accept is kept. So the store never holds a collection the site could not use,
and it has no rule of its own: that a disputed statement says what is disputed, that a withdrawn one says why,
that nothing may point to a record that is gone, that nothing gives the story away early – each is what a reader
says, returned to the editor with the name of the record. An act that is refused changes nothing. An act needs
somebody and a reason; both are kept in the history of the record.

The store looks for the files a media item names only when it was told where the files of its collections lie;
otherwise an act cannot be refused for a file that is missing, and the check of a written-out file finds it.

Statements can be asked for with the state of their review – all of them, or those that still wait.

Verification: `EditorialActsTests` (26 tests): a review with and without a note; disputed and withdrawn only
with a note, by the words of the readers; a withdrawn statement stays in the store and is no part of what the site
reads; a statement something points to is not withdrawn; a dated statement by its key; put and remove, and what
they are refused for, with the collection unchanged afterwards; the commands `store waiting`, `store review`,
`store put`, `store remove`.

## Addendum of 2026-11-17 – publishing from the store (step 11.3)

* **The store makes the next revision.** `Publish` takes what the store holds and makes revision *n* by the rule
  of [ADR 0009](0009-published-revision.md) – with the same code that makes a revision from a file; the file
  command and the store share one maker, which now works on a collection in memory. The number follows the last
  revision the store published; the editors do not name it and cannot skip one. The revision before, for the
  history and for what changed, is the one the store kept.
* **A revision is kept as it was published** – its file and the translations published with it – and is never
  changed: a row per collection and number. It can be written out again, which is how a site is taken back to a
  revision ([operations](../operations.md)).
* **Publishing changes no record.** The working state stays the working state; the history of a record is the
  history of its changes, not of what was published.
* **A translation published with a revision leaves out the texts of withdrawn statements**, since a revision does
  not hold a withdrawn statement and a translation names only what its collection has. The working translation
  keeps them.
* **Nothing is kept when a revision cannot be made**: every statement that is not reviewed, or reviewed without a
  record, is named, as by the file command.

Not done here: a first revision of the store that follows revisions made from files before the store existed (the
store starts at 1); copying the media files into the content folder of a revision (the editors copy `media/`);
the revisions of comparisons.

Verification: `EditorialPublishingTests` (14 tests): an unreviewed collection is not published and each
statement is named; revision 1 and its record in the store; a site in the public settings reads a revision of
the store from a content folder with nothing of the editors beside it; revision 2 with a changed record and a
withdrawn statement – its history, its translation, the store's working state, and revision 1 unchanged; a
statement changed after its review stops the next revision until it is reviewed again; the commands
`store publish`, `store revisions`, `store revision`. `ContentRevisionTests` (Data) still pass with the
shared maker.

## Addendum of 2026-11-17 – the tool of the editors, to read (step 11.4)

* **A second web app, `Vafadar.Dastan.Studio`**, shows what the store holds: the collections; a collection with
  its groups, what waits for a review and what was published; the records of a group with the state of their
  review; a record as it is written, with every change of it – who, when, why, and each field that differs before
  and after; the statements that wait; the latest changes of a collection; the revisions. A record that was taken
  out keeps the page of its history.
* **It is built like the site**: pages rendered on the server for every request, no script, no style in a page,
  English and Persian from the address, light and dark, the base style sheet and the headers of `Vafadar.Web`
  (ADR 0003, repository ADR 0013). Its own style sheet repeats the colours of the site (ADR 0004).
* **It answers the machine it runs on and nobody else.** The tool shows what is not published and has no
  accounts; until it knows who an editor is (step 11.7), where it runs is its only guard. A request from another
  address is refused before anything else happens, and the allowed host names are those of the machine itself.
  No page is kept by a browser or indexed.
* **It only reads.** No page has a form that changes the store; acting is step 11.5. The store is opened for a
  request and closed with it, and brought to the form the program knows once, when the tool starts – a tool that
  is not told its store, or cannot open it, does not start.
* **A record is shown as it is written** – fields and values, nested as the record nests them. Nothing is
  interpreted, so the page cannot show a record differently from what the store holds and the readers read.

Known limits: a change of a translation shows the whole group of texts that differs, not the single text; the
tool has no search; the Persian font of the site is not part of the tool, which uses the fonts of the machine.

Verification: `StudioTests` (24 tests) over a store with the collection of the repository after some work:
every page and what it lists; what the store does not hold is not found; Persian, right to left, Persian digits;
no page kept, indexed or with a script or an inline style; no form but the choice of the appearance; a request
from another address is refused; the tool does not start without its store. In a browser, on a published build
at the width of a phone, English and Persian: no page scrolls sideways – a table wider than the page scrolls in
its frame –, every link and button is at least 44 px high. Measured, not seen: no picture could be taken.

## Addendum of 2026-11-17 – the tool of the editors, to work (step 11.5)

The tool now acts as well as shows. Every act of the store has a form on the page of what it acts on:

| Where | Form | Act of the store |
|---|---|---|
| The page of a record that is or holds a statement | Record a review: what it found, the day, a note | `Review` |
| The page of a record | Change the record, as the text it is written as | `Put` |
| The page of a record | Take the record out | `Remove` |
| The page of a group | Add a record – never in place of one that is there | `Put` |
| The page of the revisions | Publish the next revision | `Publish` |

* **A form is an ordinary form** sent to the address of its page, with the token of the tool; there is no script.
  An act that was done leads back to the page (a redirect), which shows the record as it is now and says what was
  done – so loading the page again does not do the act again. An act that was refused answers with the page
  itself: "Not done. Nothing was changed.", every reason in the words of the readers, and the form as it was
  filled in.
* **Every act asks who and – except a review, whose note is the reason – why.** The name is what the editor says
  it is; the tool keeps the last one in a cookie to start the next form with, and says on the form that it does
  not check it. This is a convenience, not an identity (step 11.7).
* **A record is changed as text**, the JSON it is written as. It is the one editor that can change every field of
  every kind of record today; forms for the parts of a record are step 11.6. The identifier of a record cannot be
  changed there: a record with another identifier is a new record.
* **The tool adds nothing to the rules.** It checks only what a form needs to be understood – a name, a reason, a
  day, a text that is a record – and hands the rest to the store, which tries the act on the whole collection.
* **Guards.** The forms are reachable from the machine of the tool only; a form without the token of the tool is
  refused; what a page says was done is one of the things that can be done there, never a text from the address.

Not done: writing a published revision out to a folder from the tool (the command `store revision` does it);
changing a translation in the tool (`store import` does it); undoing an act other than by another act.

Verification: `StudioActsTests` (19 tests), each with a store of its own and a browser with cookies that fills
in a form of a page and sends it with its token: a review recorded and refused; a change kept, unchanged and
refused, with the text staying in the form; taking out and what stops it; adding and never replacing; publishing
refused until every statement is reviewed, then kept; a forged form; a forged "done". `StudioTests`: every page
has the forms of what can be done there and no other. In a browser, on a published build at phone width in
Persian: the page of a record with its three forms does not scroll sideways, every field has a label and every
control is at least 44 px high. A form could not be sent from the browser of this session (its window does not
draw); sending is covered by the tests above.

## Addendum of 2026-11-17 – the fields of a record as a form (step 11.6)

The page of a record has a second way to change it: its fields as a form (`RecordForm`, `FieldInput`). The record
as text stays below it.

* **The form is made from the record, not from a description of records.** Every field the record has gets the
  input that fits what it holds:

  | What the field holds | Input |
  |---|---|
  | A text | One line, or several when the text is long |
  | A number | A number; the reading stage (`spoilerLevel`) is a choice of the stages of the collection |
  | Yes or no | A box to tick |
  | A text with a vocabulary (`ContentVocabulary`, by the place of the field: `relations[].scope.kind`) | A choice of its values |
  | The identifier of a record (`from`, `to`, `entityId`, `storyId`) | A choice of the records of that group |
  | Identifiers of records (`sourceIds`, `eventIds`, `depictedEntityIds`, …) | A box to tick for each record of that group |
  | Texts | A text with one in a line |
  | A part with fields of its own (`scope`, `rights`) | Its fields, set in |
  | A list of parts with the same simple fields (`entityAssociations`) | A row for each, and an empty one |
  | Anything else – nothing, a part within a part | The text it is written as |

  So a new field or a new value needs nothing in the tool: the readers and their vocabulary know it, and the form
  follows. The label of an input is the name of the field in the file – the name the guide to the content and the
  reasons of the readers use.
* **What the form sends is the record again.** The fields are read in the order of the record; what the form does
  not show – the identifier, the state and the record of a review – stays as it is. The records a field points
  to keep the order the record names them in, new ones follow in the order of the collection. A text left empty
  takes its field out of the record. A row whose texts are all empty is no row: that is how the empty row stays
  out, and how a row is taken out. Every record of the sample comes back from its form as it was; a test holds
  that for each of them.
* **The form adds no rule.** It refuses only what it cannot understand – a number that is none, a text of a part
  that is not JSON – and hands the record to `Put`, which tries it on the whole collection. A value the record
  has that the vocabulary does not know stays choosable; the readers say what is wrong with it.
* **The form does not add a field.** It changes the fields a record has. A field the record does not have yet is
  written in the record as text.

Not done in this step, and done in step 11.8 (next addendum): the texts of the other languages. Until then a row
of an event whose role was translated could not be taken out in the tool. The reasons of the form itself ("is not a number") are English, as the reasons of
the readers are.

Verification: `StudioFormTests` (15 tests): every record of the sample through its form and back; the inputs of
an event; the form on the page; sent as it stands it changes nothing, for a name, a relation, an event and a
source; a text, a stage and the sources filled in; a row added; a row taken out; a translated row refused with
the reason; an emptied text; a refusal that keeps the form; a number that is none; Persian. Measured in a browser
on the published tool at 375 px, right to left: no overflow, every input with a label, every target at least
44 px, and one change sent from the browser and kept.

## Addendum of 2026-11-17 – the texts of the other languages in the tool (step 11.8)

A record and the texts that translate it are read together by the readers: a translation of something the
collection does not have is refused. So the tool could not take out a part that was translated, and an editor had
no way to change a Persian text but a file. Now:

* **The readers list what a translation may translate** (`ContentVocabulary.Translated`): for each group the
  fields, and whether the roles of what takes part are translated. The reader reads its translations by this same
  list, so the list cannot differ from what is accepted, and the tool makes its inputs from it.
* **The store keeps the texts with the record, in one act.** `Put` takes the texts of the record by the language
  (`texts`); the record and the translations are changed together and tried together. A language that is not
  named keeps its texts, a language named with no texts loses those of the record, a language the collection does
  not have is refused – a language is added by importing its file. `Texts` reads the texts of a record by the
  language. There is no act that changes a translation alone: it could only do what `Put` with the record as it
  is does.
* **In the form the text of a field in another language stands below the field** – for every text of the record
  the list names, and for the role of every row of what takes part, also in the row that is added. The label
  names the language. A text left empty is a text the language does not have. **The role goes with its row**: a
  row that is taken out takes its role in the other languages with it, and a row that names somebody else keeps
  its role under the new name.
* The record as text does not carry the texts of the other languages; it keeps the rule it had.

Not in the tool: the texts of a statement with a date (`dated`) and of the reading stages, the review state of a
translation, and a new language – `store import` of the file of the translation does all of these. The history
shows a change of a translation as before: as a change of the document of its language, not of the record.

Verification: `EditorialTextsTests` (6 tests): the texts of a record read; a text changed with who and why,
the record and the other records untouched; a part refused alone and taken out together with its text; a language
named with no texts and one not named; an unknown text and an absent language refused; the list of the readers.
`StudioFormTests` (19 tests now): the Persian text below its field, with its language; a text changed and one
emptied while the record stays; a row taken out with its Persian role; a row added with one; a refused form that
keeps the Persian texts; and the form sent as it stands still changes nothing. Measured in a browser on the
published tool at 375 px, right to left: no overflow, every input labelled, every target at least 44 px, and a
row taken out with its Persian role from the browser.

## Addendum of 2026-11-17 – who an editor is (step 11.7, D-046)

The owner decided that the tool runs on a server and that editors sign in. Until now the tool took the name an
editor typed and was guarded by where it ran. Now it can know who an editor is.

**An editor is an account of the store.** A name – compared without case – and a password, of which the store
keeps only a derived value (PBKDF2 with SHA-256, 600,000 rounds, a salt of its own for every account; the rounds
are written into the value, so that they can be raised later). A password has at least 12 characters. Considered
and not chosen: signing in with a provider (Microsoft, Google) – it needs an app registered with the provider
and an account of every editor there, which nobody asked for; it can replace the accounts later, because the
acts take a name and do not care where it comes from. The identity framework of the platform – a second database
model and pages for what the tool does not offer (registering, mail, recovery); the store needs a name, a derived
password and a stamp.

| The store | Does |
|---|---|
| `AddEditor`, `SetPassword`, `CloseEditor` | Make an account, give it another password, close it or open it again |
| `SignIn` | The name and the password of an open account give a sign-in; nothing else does, and why not is not said |
| `Holds` | Whether a sign-in still holds: the account is open and has the password it was signed in with |
| `Editors` | The accounts, without anything of their passwords |

* **Wrong passwords are counted.** After five in a row the account takes no password for fifteen minutes – not
  the right one either. An unknown name takes as long to refuse as a known one.
* **Another password and a closed account end every sign-in at once**: the account has a stamp that a sign-in
  carries, and both change it. What a closed account did stays in the history under its name.
* **An account is made where the store lies**, by the commands `store editor add|password|close|open` and
  `store editors`. The tool has no page that makes an account or changes a password: who can run a command on
  the machine of the store is who administers it. The password is asked for and typed twice without being shown;
  it is never a word of the command, and where nobody can be asked – input from a file or a pipe – no account is
  made.
* An account is not content: it has no history in the store and is in no file the store writes out.

**The tool with accounts** (`Studio:Accounts`). Without the setting the tool is what it was: on its machine
only, with the name an editor types. With it:

* It answers other machines – and a request from another machine **only over an encrypted connection**. The
  certificate is held by a proxy in front of the tool; the tool believes what a proxy says about how an editor
  came only from the proxies it was told (`Studio:TrustedProxies`), as the site does (ADR 0010).
* **Nothing of the store is shown to who has not signed in.** Every address leads to the page that signs in,
  except that page, what a page is drawn with, the choice of the appearance and the pages that say something
  went wrong; a form sent without a sign-in is refused. After signing in the editor arrives where they wanted to
  go – inside the tool only.
* **The sign-in is a cookie** of the tool (HttpOnly, SameSite, `Secure` over https, twelve hours, renewed
  while it is used), for the whole tool in every language. Every request asks the store whether it still holds.
  Its keys outlive the process where a folder for them is named (`Studio:Keys`).
* **Every act is kept under the account.** No form asks for a name, and a name a form gives all the same is not
  heard. Signing out is a form with the token of the tool.

Not done: roles – every editor may do every act, also publish; a page for an editor to change their own
password; a record of who signed in when. Two editors who change the same record at the same moment: the later
act is tried on the collection as the earlier one left it and replaces the record – the history shows both, and
nothing warns the later editor.

Verification: `EditorAccountsTests` (17 tests): signing in without case; a wrong name or password; the password
not in the file of the store; a salt for every account; what an account needs; the count of wrong passwords and
its end; another password; a closed account; the commands, with a password typed twice and never a word; no
account where nobody can be asked; a store from before the accounts. `StudioAccountsTests` (22 tests): every
kind of page sent to the sign-in without saying anything of the store; a form without a sign-in refused; the
page that signs in drawn; arriving where one wanted to go, in both languages; a refusal that does not say what
was wrong; no way out of the tool by the address to arrive at; the lock after wrong passwords; an act under the
account whatever a form says; two editors in one store; signing out with the token only; another password and a
closed account ending a sign-in; another machine answered over an encrypted connection only, and a stranger's
word about it not heard; the tool without accounts as before. In a browser on the published tool at 375 px,
right to left: sent to the sign-in, signed in, arrived at the record, no input for a name, signed out.

## Addendum of 2026-11-17 – the tool on a server (step 11.9)

With accounts the tool can leave the machine of an editor. How it runs there is in
[operations of the tool](../operations-studio.md); what was decided:

* **One container, the store on a volume.** The image (`Dockerfile.studio`) holds the tool and the command of
  the store, nothing of a store. The store and the keys of the sign-in lie in `/data`, the one place the
  container can write to. It stays SQLite (D-045): one program writes to one file, which is what one container
  is. A database server becomes right when the tool has to run more than once – nothing asks for that yet.
* **The proxy holds the certificate, and has to be named.** The tool refuses a request from another machine
  that did not come by https, and believes only a named proxy about that. A tool whose proxy was forgotten
  refuses everything – it fails closed.
* **Accounts are made in the container**, with the command beside the tool, by whoever may run a command on
  the server. There is still no page that makes an account.
* **Alive or not** is asked at `/health`, without a sign-in; it says nothing of the store.
* **A copy of the store** is written by the database itself (`store copy`, `CopyTo`): whole, of one moment,
  while the tool runs, never over a file. Bringing it back is putting it in the place of the file while the tool
  is stopped. The store does not copy itself anywhere: where copies go and how often is the server's question.
* **Checking from outside** (`eng/Check-Studio.ps1`) needs no account and asks as the proxy would. Its first
  run found that the cookie of the forms was not Secure behind a proxy; it is now.

Verification: `StoreCopyTests` (3 tests): a copy holds the collections, the history and the accounts of its
moment and not what came after; never over a file; the command, which makes no store where there is none.
`StudioAccountsTests`: alive without a sign-in; every cookie Secure over https. On this machine, in the Compose
project of Dastan: the image built; the container healthy, as a user without rights on a file system that cannot
be written to; `Check-Studio.ps1` passed all 23 checks; a store with a test account put into the volume;
signed in as a proxy would pass it on, the start and a record answered; the key of the sign-in written to the
volume; `store editors` and `store copy` run in the container, an account refused without a terminal; then the
container and the volume removed. Not done is listed at the end of the operations page.

## Ninth addendum of 2026-11-17 – texts that belong to no record of their own (step 12.5)

Step 11.8 put the texts of the other languages beside the fields of a record. Two kinds of text had no place:
the names of the reading stages (they are part of the collection, not a record) and the texts of a statement
with a date (a translation names them by the key of the statement, not by their entity).

* **`EditorialStore.PutStages`** changes the names of the stages in the language of the file and in the other
  languages in one act; a stage is added at the end, the last one is left away – which the readers refuse
  while a record is told at it. A stage that is gone loses its name in every language.
  `Stages` and `StageTexts` read them.
* **`EditorialStore.Put`** takes, for an entity, the texts of its dated statements by language and key
  (`dated`); a statement that is taken out loses its texts in the same act. `DatedTexts` reads them.
* **What is not a record has a history.** Until now a change of the header of a collection – stages, edition,
  publication note – was kept without a trace. It is now a change of the group `collection`
  (`EditorialStore.CollectionGroup`) with who, when, why, before and after, and counts as a change.
* **The tool**: the page `<collection>/stages` – one form, a row per stage and one more to add a stage, the
  name in every language of the collection. In the form of an entity the list `dated` is shown row by row
  (a list whose rows have parts of their own was written as text before); the texts of a row in the other
  languages stand below its `label` and `summary`. The time of a row is still written as it stands in the
  file – a form for a time is step 13.1.

Verification: `EditorialStagesAndDatedTests` (9 tests, store) and `StudioStagesAndDatedTests` (10 tests,
tool): read by language; kept with who and why; unchanged sent as it stands; a stage added and taken out; a
stage in use, a nameless stage and an unknown language refused with the form kept; the texts of a dated
statement kept with its entity and gone with its row; records with lists of nested parts (media, an entity
with dates) sent as they stand change nothing. Not looked at in a browser.

## Tenth addendum of 2026-11-17 – comparisons in the store (step 12.6)

Comparisons stayed files while the store learned to hold collections. They are now kept in the store too.

* **A comparison is one document**, not a set of records: its lanes, periods and events are parts of it and
  have no identity outside it. The store keeps the file as it is written and one document per language it is
  translated into (`StoredDocument`, kind `comparison`), and every change of either with who, when, why,
  before and after (`DocumentChange`). Two new tables, migration `Documents`; the tables of the collections
  are untouched.
* **The readers of the site decide**, as for a collection: `PutComparison` reads the file with its
  translations through `ComparisonSample.Read` before anything is kept; what the site could not read is
  refused with the reasons and changes nothing.
* **The translations given are all translations**: a language that is not among them is taken out, in the
  same act and with the same reason.
* A comparison has no revisions of its own: its file says whether it may be shown to the public
  (`publication`), and what is written out of the store (`store comparison export`) is the file a site is
  given in its folder `comparisons`.
* **The tool**: `comparisons` lists them and brings a new one in as its file; `comparisons/<id>` changes the
  file and every translation as text in one form – an emptied translation is taken out, a further one names
  its language in its document –, takes the comparison out, and shows what was changed of it. Forms for the
  lanes and periods of a comparison are step 13.6.

Verification: `ComparisonStoreTests` (6 tests) – in and out as it went in, read by the site again; unchanged
twice; a change with who and why; four refusals that change nothing; a translation and a comparison taken
out with the history kept; beside a collection in one store; the commands. `StudioComparisonsTests`
(6 tests) – the list in both languages, bringing in, changing, refusing with what was written kept, taking
out. Not looked at in a browser.

## Eleventh addendum of 2026-11-17 – a time as a form (step 13.1)

The time of a statement was the one part of a record that an editor still had to write as JSON.

* A field named `time` that the form can say with its inputs is a `FieldKind.Time`: the kind (`occurrence`,
  `between`, `period`, or none), up to four positions (`at`, `latest`, `until`, `untilLatest` – each an
  era, a year, optionally a month and a day, and "about"), how a period begins (`known`, `between`,
  `unknown`) and ends (`known`, `between`, `ongoing`, `endedAtUnknownTime`, `unknown`), and whether it
  is stated as continuous. These are the shapes of `Vafadar.Temporal` (document 04); the form adds none.
* **Read through the library, written by the library**: the inputs are filled from
  `TemporalJson.Deserialize` and what was filled in is built as a `TemporalExpression` – so a period that
  ends before it begins, a day without a month or the 31st of February are refused by the same code the site
  reads with – and written with `TemporalJson.Serialize`.
* **A time that was not changed stays as the file wrote it**: what the inputs give is compared with what
  stood there; when it is equal, the field is kept untouched. Sending a form as it stands changes nothing.
* **The calendar of a position stays what the file says** – named or not (D-043). The form does not turn a
  year without a calendar into a Gregorian year.
* A time the inputs cannot say – a place in a story, a time with a "last confirmed", another calendar – stays
  a part written as it stands.
* A relation with the scope `outside_the_story` that states no time shows the inputs with the kind "–";
  filled in, the relation gets its time, set back to "–" it loses it. A relation of a story has no such
  inputs: it cannot have a time.

No script: every input is always there; the hint above them says which the chosen kind uses. That costs
room on the page and is the price of a form that works without scripts (ADR 0003).

Verification: `StudioTimeFormTests` (11 tests) – the inputs of a time and unchanged when sent as it stands;
a corrected year with a day and "about"; once → lasting with an unknown end; between two years before the
common era; four inputs that are no time and a period that ends before it begins, refused with the form
kept; a relation of the world given a time and losing it; no inputs on a relation of the story. Not looked at
in a browser.

## Twelfth addendum of 2026-11-17 – two editors at one record (step 13.2)

With accounts and a server (D-046) two editors can have the same record open. The second one to send a form
overwrote the first without either knowing.

* Every form of the tool that changes what it shows carries a hidden mark (`Seen`): a hash of exactly what
  the form was made from – for a record its body, its texts in the other languages and the texts of its dated
  statements; for the stages their names in every language; for a comparison its file and its translations.
* When a form comes back, the mark is made again from what the store holds now. If it differs, **nothing is
  kept**: the page says that somebody changed this in between, shows the form with the editor's inputs on top
  of what the store holds now, and the history on the page says what was changed. The form that comes back
  carries the new mark – sent again, it is kept, knowingly.
* The mark covers what the form shows, not the whole collection: a change of another record does not make a
  form stale.
* The check is made by the page and the change is kept by the store a moment later; two forms sent within
  that moment can still follow each other unnoticed. A store-wide lock would close that gap and was not built:
  the history keeps both changes with who and when, nothing is lost without a trace.
* Publishing a revision and bringing a new record in show nothing that somebody else could have changed and
  carry no mark.

Verification: `StudioStaleFormTests` (7 tests) – the form of fields, the form of the text and the form for
taking out, each opened before somebody else changed the record: refused, nothing kept; the form that comes
back has the inputs and is kept when sent again; a change of the Persian text makes it stale; a change of
another record does not; the stages and a comparison are guarded in the same way. Not looked at in a browser.

## Thirteenth addendum of 2026-11-17 – who may do what, and the own account (steps 13.3 and 13.4)

Every account could do everything. GOV-002 keeps writing, reviewing and publishing apart.

* **Three roles**, any of which an account has: `writer` (changes, adds and takes out records, texts, stages
  and comparisons), `reviewer` (records the review of a statement), `publisher` (publishes a revision).
  Reading needs no role. An account is made with all three unless roles are given; the accounts that existed
  keep all three (the migration gives them). A closed account may do nothing.
* **Roles are given where the store lies**, by command (`store editor add <name> --roles …`,
  `store editor roles <name> <roles>`), like the accounts themselves: the tool has no page that makes an
  account or changes what one may do, so nobody can give themselves a role through the tool.
* **In the tool** a form is on the page only for who has its role (`Allowed`); who has not reads a sentence
  instead. A form that is not on the page cannot be sent: the request is refused before anything is done.
  The roles are read from the store with every request – a role that is taken away is gone at once.
* A tool without accounts runs on the machine of its one editor, who may do everything, as before.
* The rule that who wrote a statement is not who reviews it is not enforced: with one or two editors it would
  stop the work. The history says who did what.
* **The own account** (`account`): what it may do; another password, for which the present one is proved – a
  wrong one counts like a wrong one at a sign-in, and the new password signs the editor out everywhere, this
  browser included; and the last times the password of the account was tried, with whether it was right.
* **What is kept of a sign-in** is the account, the time and whether the password was right – for the last 50
  tries of an account. No address, no browser, nothing typed. A try with a name the store does not know is
  not kept: what was typed there could be a password. (Privacy matrix: the paragraph of the accounts.)

Verification: `EditorRolesTests` (16 tests, store) and `StudioRolesTests` (10 tests, tool): default and given
roles, changed and listed by command; what is no role; a closed account; a form per role in five kinds of
account; a form sent after the role was taken away does nothing; the page of the account in both languages;
another password with a wrong present one, with two different new ones, and done – after which the old one
signs in nowhere; tries remembered, the last 50 only, none for an unknown name. Not looked at in a browser.

## Fourteenth addendum of 2026-11-18 – a new record and a new language without a file (step 13.5)

A record could be changed with a form but only be added as text; a language came into a collection only with
an imported file.

* **A new record starts as a form.** `NewRecord` gives, for a source, an entity, a relation and an event, the
  record a form starts with: the fields a record of that kind has, empty or with the usual value – a relation
  begins as one of the world outside a story (with the inputs of its time), an event at the place after the
  last step. The form is the form of step 11.6, made from that record; what is filled in is read by the same
  code and tried by the readers before it is kept. The starting records are no content and no templates of
  the format: they exist so that a form has fields.
* **The identifier is asked for** – it names the record for good – and adding never replaces: an identifier
  that any record of the collection has is refused, and the editor is sent to that record's page instead.
* An image or a video has no such form: its record is of use only with its file (step 13.7). The text form
  for adding stays for every group.
* **`EditorialStore.AddLanguage`** adds an empty translation document of a language tag to a collection, as a
  draft. From then on the forms of the records have an input for that language below every text. What is no
  language tag, and the language the file is written in, are refused by the readers.

Verification: `StudioNewRecordTests` (11 tests): the form in both languages; an entity, a relation of the
world with its time and a step of the story added and kept with who and why; no identifier, a taken one and a
record the site cannot read refused with the form kept; no form for media or an unknown group; a language
added and its first text filled in; three languages that are refused. Not looked at in a browser.
