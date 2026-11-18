# 0012. Reports of visitors: a folder between the site and the tool

* Status: Accepted
* Date: 2026-11-18
* Step: 14.8 of the [delivery plan](../delivery-plan.md)

## Context

A visitor who sees an error has to be able to say so (R-GOV-03, B-015, scenario SC-14). Until now the site had
no way for it: it is read-only, has no database and knows nothing about its visitors. The editors work in
another program with a store of its own ([ADR 0011](0011-editors-store.md)), which the public site never reads.

A report is words of a stranger. It must not change anything by itself, must not open a way from the public
site into the store, and must not make the site keep anything about who wrote it.

## Decision

* **A report is a small file in a folder** (`ReportBox`, in `Vafadar.Dastan.Data`). The site writes into the
  folder and reads nothing back; the tool of the editors reads it. The two programs share nothing but that
  folder – no connection, no database, no address of each other. On one machine it is one folder of a volume;
  on two, the folder is carried from one to the other like the files of a collection.
* **What is kept**: the collection, the statement (the address of the page of its sources inside the
  collection), the published revision the site shows, the language of the page, the words (at most 1,000
  characters, without control characters) and **the day**. Not the time of the day, no address of the
  visitor's machine, no browser, no name, no way to answer. The form says so, and says not to write anything
  personal.
* **The site takes reports only when it was told a folder** (`Dastan:Reports`). Told none, it offers no form
  and the address of the form answers "not found".
* **The form** stands on the page of the sources of a statement – the page a visitor reaches from any
  statement – and is sent without a script, with the token every form of the site carries.
* **What is refused**: a report about a statement the visitor could not read at their reading position (it
  is asked of the projection, like the page), about another collection, without words or with more than a
  form allows, and a form without its token. Refused like any form that is no form of the site.
* **Against programs**: a field no person sees – whoever fills it in is thanked and nothing is kept; at most
  five reports an hour from one address, counted in the memory of the process and forgotten with the hour
  (the address is never written anywhere); and a box that holds 500 reports nobody has looked at takes no
  more and says so. No picture puzzle and no third party.
* **In the tool** the page `reports` lists what waits, the oldest first, each with the way to the record it
  is about; an editor who reviews notes that a report was looked at, with the name the act is kept under. The
  report then lies in the folder `settled`. **A report changes no record**: when it is right, an editor
  changes the record like any other change, with a reason – the trail of the store stays the one trail.
  The tool has the page only when it was told the folder (`Studio:Reports`).

## Consequences

* The public site still has no database and no knowledge of its visitors; what it now writes is one folder.
  The container of the site needs that folder on a volume it may write to.
* A report cannot be answered. That is deliberate: answering would need an address of the visitor.
* The limit of five an hour is per address as the site sees it: behind a proxy the site has to be told the
  proxy (`Dastan:TrustedProxies`), or every visitor is the proxy and shares one limit.
* Several containers of the site write into one folder without knowing of each other; each counts its own
  five an hour.
* Reports that were looked at stay in `settled` until somebody deletes them; the tool shows the last fifty.
  How long they are kept is the owner's decision ([privacy matrix](../../../../../docs/privacy/privacy-matrix.md)).

## Not done

A report about a page that is no statement (a chart, a gallery, the site itself); a report with a picture or
a file; telling the editors by mail that a report came; a public list of what was reported and corrected –
the list of what changed in a revision ([ADR 0009](0009-published-revision.md)) says what was corrected.

## Verification

`ReportBoxTests` (25 cases) – what a file holds and that it holds nothing else; which addresses name a
statement; what is no report; the note that somebody looked; the order; a file that is no report; a full box;
a folder that is not there. `VisitorReportTests` (16 cases) – the form in both languages; a site without a
folder; a report kept and the visitor thanked; a Persian report about a relation; a statement of a later
reading stage; five things that are no report; words too long and a form without its token; the field nobody
sees; more than five in an hour; an address that says something else; the return address. `StudioReportsTests`
(6 cases) – the page in both languages; a tool without a folder; a report with its words and the way to its
record; the note that it was looked at, which leaves the store untouched; a report somebody else looked at
meanwhile and an act without a name. Not looked at in a browser; not tried with two machines.

## Addendum of 2026-11-18 – deleting reports that were looked at

`ReportBox.Forget(day)` deletes the reports an editor looked at before a day; the command of the editors'
tools does it: `reports forget <folder> --looked-at-before <yyyy-MM-dd>`. A report nobody has looked at is
never deleted this way. **Nothing deletes by itself**: how long such reports are kept is the owner's to say,
and whoever runs the site runs the command with that day. Verification: two tests in `ReportBoxTests`.

## Addendum (2026-11-18): how long a report that was looked at is kept (step 16.11)

The decision left open how long reports that an editor looked at are kept. The owner was given 30 days, 90
days, a year and for good on 2026-11-18, with 90 days recommended, and had not answered when this was built.
`reports forget <folder>` now deletes what was looked at more than 90 days ago
(`ContentCommands.ReportsKeptDays`); `--kept-days <n>` names another number and `--looked-at-before` a day,
as before. A report nobody looked at is never deleted, and nothing runs the command by itself: whoever runs
the site runs it, or gives it to a timer. Another number is one constant.

Checked: one more case of `ReportBoxTests`.
