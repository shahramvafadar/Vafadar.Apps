# 0009. What everybody may see: the published revision

- Status: **Accepted** – implemented and verified on 2026-11-17; no collection is published yet
- Date: 2026-11-17
- Deciders: Shahram Vafadar (the step was asked for on 2026-11-17; the rule below was chosen conservatively on his
  behalf and is recorded as decision D-044 – every part of it can be changed by him)
- Governing documents: 07 (GOV-003, GOV-004, GOV-021), 16 (PILOT-09, gate G-01), the delivery plan (section 8)
- Related: [ADR 0002](0002-one-projection.md) (one projection), [ADR 0006](0006-media-delivery.md) (media)

## Context

Until now "the public" was an audience that got nothing: the only collection is an editorial sample, and the reader
refuses an editorial sample that calls itself approved. Document 07 asks for more than a switch: an editorial
dataset, a local demonstration and a published public revision are different things (GOV-021), publication follows
a review (GOV-003), and what is published can later be disputed, corrected or withdrawn (GOV-004). The projection
needed the rule by which a collection becomes visible to everybody, before any page can be public.

## Decision

1. **Only a published revision is public.** A file is an `editorial_sample` or a `published_revision`
   (`publication.state`). Only a published revision can be approved for the public (`publicReleaseApproved`), and it
   says which revision it is: a number from 1 and the day it was published (`publication.revision`). An editorial
   sample cannot be approved and states no revision. A published revision that is taken back sets the approval to
   `false` again; nothing else has to change.
2. **A published revision holds only reviewed statements.** Every relation, every step of the story and every
   dated statement is `reviewed` – or `disputed`, which is published as what it is. A statement that is proposed,
   only sourced or withdrawn is not in a published revision: the collection is refused as a whole and every such
   statement is named. There is no public page that leaves a part of a story out without saying so.
3. **A correction is a new revision.** The site reads a collection once and never changes it; what is corrected
   is published as the next number. Every public page says which revision it shows.
4. **The public gets reviewed texts only.** A translation that is a draft is checked like any other and then not
   read into a public site at all; the page shows the reviewed text of the collection instead, marked as another
   language as always. A demonstration keeps showing drafts, marked as drafts.
5. **Showing an item to everybody is a decision of its own.** `publicRenderEligibility.allowed` on an image or a
   video, separate from the permission for a demonstration. It needs rights that allow it – original project
   content or verified open access – and what showing needs anyway: the file, a checked text alternative, for a
   video its playing time, captions and a transcript.
6. **A comparison follows the same rule.** One that is approved for the public has the status `reviewed`, and each
   of its statements is `reviewed` or `disputed` and cites a source.
7. **The reading position stays in force.** Being public changes who may see a collection, not how much of the
   story a visitor has read.

The rule is enforced where a collection becomes one – in the checks of `Catalogue` and in the readers – and where
a visitor gets it: `Projection.Of` and the choice of media for an audience. No page asks who the audience is to
decide what it shows; the footer alone says "local demonstration" or names the revision.

## What this does not decide

* **Who reviews, and how a review is recorded.** `reviewed` is a state the editors write into the file. There is
  no record of who reviewed a statement and when, no history of a correction and no difference between two
  revisions (GOV-005). That needs the store and the tools of the editors, which come after R0's first steps.
* **How a withdrawn statement is explained to the public** (GOV-004): a withdrawn statement is simply not part of
  a revision.
* **Rights of museum images.** The rule gives verified open access a way to be public; whether any item of the
  collection has such rights is the editors' finding, and today none is marked so.
* **Hosting and a public address.** Nothing here makes the site reachable.

## Consequences

* The collection of the repository stays what it is – an editorial sample, shown in a local demonstration only.
  Publishing it means writing a revision: reviewing its 20 relations and 8 steps, deciding on its Persian text and
  on its media.
* The command of the writers checks a published revision by the same rule and says to whom a file is released.
* A public site with an editorial sample shows nothing, as before.

## Verification

2026-11-17: `PublishedRevisionTests` (Core), `PublishedRevisionReaderTests` (Data), `PublishedSiteTests` (Web) – a
site in the Production settings with a copy of the collection made into revision 3: its pages are answered, name
the revision and no demonstration; the reading position holds; of the media only the item allowed for the public is
shown or delivered; the draft Persian text and the draft comparison do not reach a public page; an editorial sample
is still answered with "not found". The revisions of the tests are test data.

## Addendum of 2026-11-17 – the record of a review and the history of revisions (step 10.1)

The first open point above is decided as far as a file can carry it:

* **The record of a review is part of the statement.** A relation, an event and a dated statement carry
  `review`: who reviewed it, on which day, optionally against what. A published revision that is approved has the
  record on every statement, dated not after the revision; a record on a statement that is not reviewed is a
  problem. The name of the reviewer is the editors' record and is not shown on a public page.
* **A revision is made by a command, not by hand** (`revision` of `Vafadar.Dastan.Content`,
  `ContentRevision` in Data): it takes the working file – an editorial sample – checks it by this rule, refuses
  with every statement that is not ready, and writes the revision to a new file. It does not review.
* **A revision carries its history** (`publication.history`): one entry per revision, with the records it added,
  changed and removed compared with the revision before, each with its name and its reading stage.
* **The public page "what changed"** (`/<collection>/revisions`, linked from the note that names the revision)
  lists the revisions, newest first. A change is told to a visitor only from the reading stage of its record on
  (`Projection.Revisions`); a revision of which the visitor may see no change says the same words as one that
  changed nothing – the page gives away neither a later part nor that there is one.

Still open: an account of the reviewer and a store of the editors (the record is a text in a file and is as
trustworthy as whoever may write the file); what exactly changed inside a record (the page names the record, not
the difference of its texts); the history of a comparison.

Verification: `RevisionHistoryTests` (Core), `ContentRevisionTests` (Data: the template of the writers reviewed,
made into revision 1 and 2, read back by the site; what is refused; the command), `RevisionsPageTests` (Web).
The revisions of the tests are test data; nothing is published.

## Addendum of 2026-11-17 – disputed and withdrawn statements for the public (step 10.2, GOV-004)

* **A disputed statement is said to be disputed wherever it is shown.** One component (`DisputedMark`) says it:
  the word beside a relation in every list, beside a step of the telling and in the tables of a chart; a sign
  (≠, with the word for a screen reader) on a relation of the connections chart and on a step of the story chart;
  a dotted line in the family drawing and a dashed outline on a bar or a mark in time, where a chart has only a
  line or a bar – each with a line below the chart that says what the mark means. The mark never relies on colour.
  The page of its sources says what the disagreement is: the note of its review, which a disputed statement of a
  revision has to have. Who reviewed stays the editors' record.
* **A withdrawn statement is no part of a collection.** It stays in the working file with who withdrew it, when
  and why; the reader leaves it out for every audience – also in a local demonstration – and `Catalogue` refuses
  one that reaches it. What points to a withdrawn statement is a problem the editors correct: that is how the
  effect on what depends on it is made visible before anything is published.
* **The history tells a withdrawal.** The `revision` command leaves withdrawn statements out of the revision and
  names each relation and event that the revision before still held as `withdrawn`, with the reason. The page
  "what changed" shows it with the reason, from the reading stage of the statement on.

Still open: a withdrawn dated statement is told as a change of its entity, not as a withdrawal of its own; a
comparison has no history, so a withdrawn statement of a comparison is refused for the public and still listed as
withdrawn in a demonstration; the note of a review is one text in the language of the collection and is not
translated.

Verification: `DisputedAndWithdrawnTests` (Core), `ContentRevisionTests` (Data: withdrawn in the working file,
in a revision and in the history), `DisputedStatementTests` (Web: lists, the three charts, the pages of sources,
the timeline, the history; a page without a disputed statement has no mark).

## Addendum of 2026-11-18 – a statement is cited with its revision (step 14.7, R-PUB-07)

Every statement has the address of its sources (ADR 0002). On a published site that page now says how to cite
the statement.

* **What is cited**: the statement in a few words – the name of an entity, a relation as its sentence, an
  event, the part of an entity in an event, an item – the name of the site, the number and the day of the
  revision the page shows.
* **The address** is the address of the page with `?revision=<n>`. It is written whole, with the host the site
  was reached by, so that it can be copied.
* **An address written for another revision** than the one the site shows says so above the citation – "this
  address was written for revision 2; the site now shows revision 3" – and leads to the list of what changed
  (step 10.2). The statement shown is the one of the present revision: the site has the revision it was given
  and the list of changes, not the earlier records. A revision that is no number is not asked for.
* **A demonstration cites nothing**: it is no revision.

Not done: showing a statement as it stood in an earlier revision; a citation in a named style (APA, Chicago);
copying with one press, which would need a script.

Verification: `MomentAndCitationTests` – the citation of an entity in both languages with the address; an
address for an earlier revision, for the same and for none; a relation cited as its sentence; no citation on
a demonstration.
