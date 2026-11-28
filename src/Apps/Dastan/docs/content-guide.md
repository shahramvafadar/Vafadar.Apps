# Writing content for Dastan – the guide

This is the official guide for whoever writes, completes or corrects content for Dastan: a person, or an assistant
such as a language model that is given this page (decision D-042). Content is prepared and reviewed apart from the
software; the software provides this guide, the formats it names and one command that says whether a file can be
used. Follow the guide and the command, and a file you hand over is a file the site starts with.

The guide says how to work. What every field means is in the two format pages:

* [The file of a collection](collection-format.md) – a story with its names, relations, steps and media
  (`dastan-pilot-0.2`) and its translations.
* [The file of a comparison](comparison-format.md) – subjects side by side over time (`dastan-comparison-0.1`) and
  its translations.

## Four rules that come before everything else

1. **Nothing without a source.** A name, a relation, a step of a story, a period, an event: each cites the source
   it comes from. Do not invent a source, an address, a passage or a date, and do not cite a source you did not
   read for that statement. What you cannot support, leave out and name it in your hand-over note.
2. **You do not review and you do not release.** New statements are written as *not yet reviewed*
   (`source_backed_editorial_review_pending` in a collection, `proposed` or `sourcedReviewPending` in a comparison;
   a translation is a `draft`). `publicReleaseApproved` stays `false` and the file stays an `editorial_sample`.
   Reviewing a statement, publishing a revision and allowing an image for the public are decisions of the editors
   ([ADR 0009](adr/0009-published-revision.md)); a file that claims them is refused or taken back.
3. **Never more exact than the source.** A year that is "about" stays about, a time between two years stays
   between two years, a story that tells an order gets no dates. The formats have a place for each of these.
4. **Nothing that gives a story away early.** Every record of a collection has a reading stage. A record, its
   sources and everything it points to must be readable at that stage – see "Reading stages" below.

## What a writer works with

| What | Where | Format |
|---|---|---|
| A collection | `content/samples/<name>.json` | `dastan-pilot-0.2` |
| Its translation | `content/samples/<name>.<language>.json`, beside it | `dastan-translation-0.1` |
| A comparison | `content/comparisons/<name>.json` | `dastan-comparison-0.1` |
| Its translation | `content/comparisons/<name>.<language>.json`, beside it | `dastan-comparison-translation-0.1` |
| Images, videos, captions, transcripts | `content/media/` | named by the collection, by a path relative to its file |
| The list of kinds | `content/kinds.json`, in the content root | `dastan-kinds-0.1` |
| Its translation | `content/kinds.<language>.json`, beside it | `dastan-kinds-translation-0.1` |

All paths are below `src/Apps/Dastan/`. The files are input of the site and never served as they are. A file is
UTF-8 JSON; a field the format does not know is a problem, not a comment – remarks go into the fields the format
has for them (`note`, `reviewNotes`).

## Where files go

There are two content roots, of the same shape (`samples`, `comparisons`, `media`). [`content/`](../content) is
the small pilot the tests of the software are written against; it is changed with the software and not by
content work. [`library/`](../library/README.md) is where the collections that are written to be published go;
every file of it is checked with each build, and a file that cannot be used or that a template left unfinished
fails it. Neither folder is served by the site as files, and neither is in an image of it.

## The command

One command, three things it does. Run it from the root of the repository (.NET 10 SDK):

```powershell
$content = "src/Apps/Dastan/Vafadar.Dastan.Content"

# 1. The values a field may have – ask instead of guessing. Add --json to get them as data.
dotnet run --project $content -- vocabulary
dotnet run --project $content -- vocabulary collection --json
dotnet run --project $content -- vocabulary kinds

# 2. The smallest file the site accepts, to start a new collection or comparison from. Never over a file.
dotnet run --project $content -- template collection my-story --out src/Apps/Dastan/content/samples/my-story.json
dotnet run --project $content -- template subject a-company --out src/Apps/Dastan/content/samples/a-company.json
dotnet run --project $content -- template comparison two-lands --out src/Apps/Dastan/content/comparisons/two-lands.json

# 3. The check: reads a file exactly as the site reads it. A folder stands for every content file in it.
dotnet run --project $content -- check src/Apps/Dastan/content
dotnet run --project $content -- check src/Apps/Dastan/content/samples/my-story.json --json
```

* The check uses the readers of the site itself, so it cannot be more lenient or stricter than the site. It
  reports **every** problem, each with the record it belongs to (`character-rostam: …`, `event-03: …`), not only
  the first.
* The exit code is the answer: `0` in order, `1` a file cannot be used, `2` the command was not understood.
* With `--json` the answer is data: `usable`, and per file `format`, `problems`, `notices` and `summary`.
* A **notice** is not a problem of the format: it names places a template left to be filled (`REPLACE: …` texts,
  addresses at `example.org`). A file with notices is not finished.
* A translation is checked together with the file it translates; naming either is enough.

### Comparing quotes with their sources

A statement rests on a passage of its source. Keep the passage while you write, and let the command compare
every passage with the source it is taken from – before a hand-over, and again when a source was looked at
anew:

```powershell
dotnet run --project $content -- quotes src/Apps/Dastan/library/samples/my-story.json --quotes .local/quotes/my-story.quotes.json --sources .local/sources/my-story
```

* **The file of quotes** is yours and is no part of the collection:

  ```json
  {
    "formatVersion": "dastan-quotes-0.1",
    "datasetId": "my-story",
    "quotes": [
      { "record": "relation-08", "sourceId": "src-a-source", "quote": "the words as the source has them" }
    ]
  }
  ```

  `record` is the identifier of an entity, a relation, an event or a media item, or the `key` of a statement
  with a date; `sourceId` is a source that record cites.
* **The folder of sources** holds a copy of each source as plain text, named `<source id>.txt` – the text of
  the page as you saved it. The command asks no address of the network.
* **Neither is committed.** A passage of a source and a copy of it are the text of somebody else; they stay on
  your machine (`.local/` is not part of the repository). The repository holds the collection and its
  citations, not the sources.
* What is compared is the letters, not how a page sets them: blanks, kinds of quotation marks and dashes,
  capitals, the Arabic and the Persian form of a letter and vowel signs make no difference.
* The answer: each quote that is not in its source, each source of which there is no copy (its quotes were
  not compared), and how many relations, events and statements with a date have no quote. Exit code `0` when
  every quote was compared and found, `1` otherwise. The command changes nothing and records no review.

## How to work

1. **Read** this page and the page of the format you write.
2. **Ask for the vocabulary.** Types of entities and relations, kinds of sources, what an entity has to do with a
   step: all are fixed sets. A value that is not listed does not exist for the site – see "What the formats cannot
   say yet" before you bend a meaning to fit.
3. **Give every record its kinds** – a battle, a drought, a poet, a hero – from the list of kinds of the content
   root (`kinds.json`, see "The kinds of a record" in [the format of a collection](collection-format.md)). A kind
   is never guessed: a record you cannot classify from its sources is left without one, and the check counts it
   (`without a kind`). A kind that is missing is added to the list by the editors, with its meaning, its types and
   its drawing; a kind is never deleted, only retired. A figure of myth is a kind of a character, not of a person.
4. **Start** from a template for something new, or from the existing file for an addition or a correction.
5. **Write**, sources first: a record can only cite a source that is in the file.
6. **Check** after every larger change, and repeat until the answer is "in order" with no notice.
7. **Hand over** the files with a short note: what you added or changed, which sources you used, what you were
   not sure about, and what you left out because you found no source. The editors review from there.

## Reading stages

A collection is read in stages (`spoilerPolicy.levels`, numbered from 0 without gaps). Every source, entity,
relation, event and media item has a `spoilerLevel`: the earliest stage at which a reader may see it.

* Put a record at the stage in which the story tells it – not earlier because it "is known anyway".
* A record cannot point to something of a later stage: a relation not to an entity, an event not to a participant,
  a record not to a source. The check refuses it ("would give the story away"). Either the record belongs to the
  later stage as well, or it needs another source.
* The texts of an early stage must not tell what a later stage reveals. The check cannot read texts for you: a
  summary of a character at stage 0 that mentions the ending passes the check and still breaks the rule.

## Texts and languages

* A collection and a comparison are written in **English**. Other languages are separate translation files; a
  translation may leave texts out, and may not name anything its file does not have.
* A summary says what the source says, in your own words. Do not copy a modern translation or a page of a site.
* A label of a relation is read from its first entity: `Zal —Father of→ Rostam`. **It says what the relation
  is and nothing else** (the owner's rule of 2026-11-26, D-102):
  * It does not name either of the two entities – a page writes them beside it. Write `Rides`, not
    `Rides Rakhsh`; for somebody who held an office, `Held the office of`, not the name of the office again.
  * It does not say whose account the statement is. "According to Herodotus", "by tradition", "in the legend",
    "perhaps invented" are written in the `note` of the relation, which is shown with its sources. Where the
    accounts differ, the note says what each says and on what it rests.
  * How long something lasted is the `length` of the relation (`{ "value": 37, "unit": "years" }`), not words
    in its label.
  * A test of the repository reads every label of the library, in every language, and refuses one that
    repeats a name or carries such words. Nothing in the site shortens a label: it is shown as it is written.
* Identifiers are stable names in lower case with hyphens (`character-rostam`). Once a file is handed over, an
  identifier does not change: addresses, translations and other records point to it.

## Dates and time

* **Dates are stored in the Gregorian calendar** (decision D-043): days as `yyyy-MM-dd`, years as years of the
  common era or before it. Showing a date in another calendar is a matter of the site, not of the file. A date
  from a source in another calendar is converted before it is written, and the source text of the date can be kept
  as the `label` of a media date.
* There is no year zero: the year before 1 CE is 1 BCE.
* A **story has an order, not dates.** A step of a telling has `order` and the time `narrative_position`; a year,
  a date or a duration there is refused. Only what really has a date gets one: when an artwork was made, what is
  stated about an entity of a collection in its list `dated` (when a work was written, when a manuscript was
  made), a period of history in a comparison.
* Say how exact a time is: `approximate: true` for "about"; in a comparison an end `between` two years, or
  `unknown`, `ongoing`, `endedAtUnknownTime`.
* **A figure of legend or myth has no date of history.** Where you still give it a place in time, write it as a
  **hypothetical placement** – a dated statement with `placement` ([format](collection-format.md), "A hypothetical
  placement"; [ADR 0028](adr/0028-hypothetical-placement.md)): its `basis` (`narrative`, `synchronism` or
  `editorialHypothesis`), its `confidence` (`veryLow`, `low`, `medium`) and an `explanation` of why that time.
  Keep the year out of the label and the summary of the entity: a year in a sentence reads as a claim. A
  character of a story takes no other dated statement, several placements may stand side by side, and the time
  is written as stated – "about" or a window between two years – never more exact than the hypothesis. Follow
  [the chronology guidelines](content-chronology-guidelines.md).

## Images and videos

* An item is shown only when its rights allow it and the file says so. Do not add a file whose rights you have
  not established; an item with `permission_not_verified` has no file and is listed, not shown.
* A work you may not show is still worth writing down: with its `sourceUrl` (the page of the museum), its
  catalogue title, number, credit line and the date it was made, the site lists it as a reference – a card with
  that record and a link to the museum, without a picture.
* A shown image needs a checked text alternative, a shown video its playing time, captions and a transcript.
* The date an artwork was made is not a date of the story it shows.
* A gallery has **themes** when the collection states them: add an entity of the type `Theme` for each ("Combat",
  "Safavid painting") and name it in `themeIds` of every image and video that belongs under it
  ([format](collection-format.md), "A theme of the gallery"). A theme needs no source; give it a stage when its
  name gives something away. Which themes a collection has is your decision as its editors – keep them few and
  about the subject.
* The picture of a profile (`picture` of an entity) is one of these items – an image that depicts the entity and may be shown. Without one the page shows a drawing of the kind and says so; an image of a museum that is only a reference cannot be a picture that is seen.

**A picture of a history says what kind of picture it is** (`depictedContext.kind`; the format lists the seven
kinds): a photograph of today of a relief is not a picture from the time of the relief, a painting of 1850 of a
battle of 1020 is not a picture of 1020, a reconstruction and a map of today are proposals. Write the time the
item was made or taken as `creationDate` and the time of what it shows as `depictedTime` – never one for the
other, and never a guess.

## Adding to a file and correcting it

* Keep identifiers; change `documentVersion` and `preparedAt` of a collection when you hand a new state over.
* When you change an English text, look at its translations: the translation file is checked for what it names,
  not for whether it still says the same.
* Remove a record only together with everything that points to it; the check names what is left over.
* Do not change `reviewStatus`, `review`, `status` or `publication` of what exists: they record decisions of the
  editors.

## What the formats can say, and what they cannot say yet

The formats are strict on purpose. Do not press content into a field that means something else – name the gap
in your hand-over note, so that the format is extended with its meaning.

What a collection can say ([format](collection-format.md)):

* It is about **one story, several stories, or something that is no story** – a company, a club, a person, a
  period ("What a collection is about"; `-- template subject` starts a collection without a story).
* Beside the things of a story it names **things of the world**: persons, organisations, companies and brands,
  governments and dynasties, countries, clubs, teams and seasons, events and periods, offices and honours,
  objects and subjects of knowledge ("The types of entities"), and says between them what the list of relation
  types holds ("The relations of the world outside the story"; `-- vocabulary` prints every type with its
  meaning). A type has one meaning: do not use one for something it does not say.
* A relation of the world says **when it held** (`time`); somebody who came back is two relations. Any
  relation says **how long it held** where a source states a length and no years (`length`), and **what stands
  behind it** (`note`).
* A statement about an entity states a **date, a quantity or both**: a population in a year, the number of
  verses of a poem ("A quantity").
* An entity has **other names** – earlier names, titles, short forms – and **identifiers** of other catalogues.
* A file is written in **any language** and says which; a translation document adds another language.

What has no place yet:

* What a collection states with a date (`dated`) belongs to one entity. Time across several subjects that is
  no part of a collection is written as a comparison.
* Borders of a land, and borders that change with time (a real place can say where it is – "Where a place
  is" – as a point only).

## For the editors: publishing a revision

A writer hands a file over; the editors review it. For every relation, event and dated statement they accept,
they set `reviewStatus` to `reviewed` (or `disputed`) and add the record of the review –
`"review": { "by": "…", "on": "2026-11-16", "note": "…" }` ([format](collection-format.md)). When every statement
has it, one command makes the revision:

```powershell
# The first revision.
dotnet run --project $content -- revision src/Apps/Dastan/content/samples/my-story.json `
    --number 1 --date 2026-11-17 --out src/Apps/Dastan/content/published/my-story.json --note "The first publication."

# A correction: the next number, and the file of the revision before.
dotnet run --project $content -- revision src/Apps/Dastan/content/samples/my-story.json `
    --number 2 --date 2026-12-01 --previous <the file of revision 1> --out <a new file> --note "What was corrected."
```

* The command checks the working file as the site reads it and by the rule of [ADR 0009](adr/0009-published-revision.md).
  It names **every** statement that is not reviewed or has no record of its review, and writes nothing then
  (exit code `1`).
* It writes a new file – never over a file – that is the working file marked as the revision: state, number, day,
  and the history of the revisions with what this one adds, changes and removes, compared record by record with
  the revision before. The working file is not touched; work goes on in it.
* The revision is written into a folder beside the folder of the working file (`samples` → `published`), so that
  the paths of media and translations stay right. A translation is published by copying it beside the revision.
* **Withdrawing a statement:** do not delete it. Set its `reviewStatus` to `withdrawn` and write the `review`
  with the reason in its `note`. The site stops showing it at once; the next revision leaves it out and says in
  its history that it was withdrawn and why – in words a reader may see at the reading stage of the statement.
  What still points to the statement has to be corrected first; the check names it.
* **A disputed statement** (`disputed`) is published as disputed. The `note` of its review says what the sources
  disagree about and is shown on the page of its sources.
* A revision needs nothing beside its content folder: the command leaves out the pointers of media items to the
  working documents of reviewers (`reviewerProvenancePath`). The site is given the folder of the revision and
  nothing else ([operations](operations.md)).
* The command does not review. It cannot know whether a statement was read against its source; it only refuses a
  file in which nobody says so. A writer or an assistant never runs it to make something visible.

## For the editors: the store

The editors keep a collection in a store of their own ([ADR 0011](adr/0011-editors-store.md)): a PostgreSQL
database that holds every record with the history of its changes ([ADR 0015](adr/0015-postgresql.md)). A writer does not need it – a writer hands over files.

```powershell
$env:DASTAN_DATABASE = "<the connection of the database>"   # typed here, never written into a file of the repository
$store = "env"                                               # the word that stands for that variable

# Bring a working file in, with the translations beside it. Say who and why.
dotnet run --project $content -- store import src/Apps/Dastan/content/samples/my-story.json --store $store --by "<name>" --reason "<why>"

# Write the collection out again, to hand it to a writer or to make a revision from it. Never over a file.
dotnet run --project $content -- store export <datasetId> --store $store --out <folder>/samples/my-story.json

# What was changed, by whom and why – of the collection, or of one record.
dotnet run --project $content -- store history <datasetId> --store $store --record relation-02
```

```powershell
# What still waits for a review.
dotnet run --project $content -- store waiting <datasetId> --store $store

# Record a review: reviewed, disputed (the note says what the sources disagree about) or withdrawn (why).
dotnet run --project $content -- store review <datasetId> relation-02 --store $store --by "<name>" --as reviewed --note "Read against the text." --files src/Apps/Dastan/content/samples

# Put one record – the JSON object of the file – in place of the one with its id, or as a new one.
dotnet run --project $content -- store put <datasetId> entities --from <file with the record> --store $store --by "<name>" --reason "<why>"

# Take a record out, with the texts that translate it.
dotnet run --project $content -- store remove <datasetId> relation-18 --store $store --by "<name>" --reason "<why>"
```

```powershell
# Publish the next revision out of the store, into a content folder of its own beside a copy of media/.
dotnet run --project $content -- store publish <datasetId> --store $store --by "<name>" --date 2026-11-17 --out <revisions>/1/content/samples/my-story.json --note "The first publication." --files src/Apps/Dastan/content/samples

# Which revisions were published, and one of them written again as it was published.
dotnet run --project $content -- store revisions <datasetId> --store $store
dotnet run --project $content -- store revision <datasetId> 1 --store $store --out <folder>/samples/my-story.json
```

* `publish` numbers the revision itself, after the last one the store published, and refuses a collection in
  which a statement is not reviewed – each is named. The store keeps the revision as it was published.
* The revision is written with its translations beside it; copy `media/` beside `samples/` to make the content
  folder a site is given ([operations](operations.md)).
* `review`, `put` and `remove` are tried on the whole collection first. What the site could not use afterwards
  is refused with the reasons – the same words the check uses – and nothing is changed.
* With `--files` the store looks for the files of media from the folder of the collections; without it, a missing
  file is found when the collection is written out and checked.
* What the store holds can be looked at in a browser with the tool of the editors (`Vafadar.Dastan.Studio`, see
  the README of the app): the records, what waits for a review, and the history of every record. It also acts: a review,
  a change of a record, taking one out, adding one and publishing a revision are forms on its pages, with the
  same refusals as the commands.
* The first import brings everything in. Afterwards a record that reads differently is a change with the record
  before and after; a record the file no longer has is taken out, and its history stays.
* A file the site cannot use is refused and changes nothing; so is a published revision – the store holds the
  working state, and a revision is made from it.
* What comes out reads as what went in. Write it into a folder beside `media/`, where its paths are right; the
  command checks it there.
* The file of the store holds unpublished content: keep it outside the repository and outside any folder a site
  is given. The name after `--by` is taken as it is given – the store does not check who you are.

## What the check does not check

That a source says what a record claims; that an address still answers; that a summary is fair, a translation
right or a picture what its caption says; that a text of an early stage keeps the story's secrets. These stay
the work of a reader – which is why every file is handed over for review, however clean its check.
