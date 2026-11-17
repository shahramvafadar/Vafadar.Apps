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

All paths are below `src/Apps/Dastan/`. The files are input of the site and never served as they are. A file is
UTF-8 JSON; a field the format does not know is a problem, not a comment – remarks go into the fields the format
has for them (`note`, `reviewNotes`).

## The command

One command, three things it does. Run it from the root of the repository (.NET 10 SDK):

```powershell
$content = "src/Apps/Dastan/Vafadar.Dastan.Content"

# 1. The values a field may have – ask instead of guessing. Add --json to get them as data.
dotnet run --project $content -- vocabulary
dotnet run --project $content -- vocabulary collection --json

# 2. The smallest file the site accepts, to start a new collection or comparison from. Never over a file.
dotnet run --project $content -- template collection my-story --out src/Apps/Dastan/content/samples/my-story.json
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

## How to work

1. **Read** this page and the page of the format you write.
2. **Ask for the vocabulary.** Types of entities and relations, kinds of sources, what an entity has to do with a
   step: all are fixed sets. A value that is not listed does not exist for the site – see "What the formats cannot
   say yet" before you bend a meaning to fit.
3. **Start** from a template for something new, or from the existing file for an addition or a correction.
4. **Write**, sources first: a record can only cite a source that is in the file.
5. **Check** after every larger change, and repeat until the answer is "in order" with no notice.
6. **Hand over** the files with a short note: what you added or changed, which sources you used, what you were
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
* A label of a relation is read from its first entity: `Zal —Father of→ Rostam`.
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

## Images and videos

* An item is shown only when its rights allow it and the file says so. Do not add a file whose rights you have
  not established; an item with `permission_not_verified` has no file and is listed, not shown.
* A shown image needs a checked text alternative, a shown video its playing time, captions and a transcript.
* The date an artwork was made is not a date of the story it shows.

## Adding to a file and correcting it

* Keep identifiers; change `documentVersion` and `preparedAt` of a collection when you hand a new state over.
* When you change an English text, look at its translations: the translation file is checked for what it names,
  not for whether it still says the same.
* Remove a record only together with everything that points to it; the check names what is left over.
* Do not change `reviewStatus`, `review`, `status` or `publication` of what exists: they record decisions of the
  editors.

## What the formats cannot say yet

The formats are those of the first collection and the first comparison. They are strict on purpose, and some
things have no place in them yet. Do not press content into a field that means something else – name the gap in
your hand-over note, so that the format is extended with its meaning.

* A collection tells **one story** of one work (`NarrativeArc`) and knows the kinds of things a narrative has:
  works, a story, characters, animals, places, regions, objects. Historical persons, organisations and real
  places have no types yet.
* The **types of relations** are a fixed list (`vocabulary` shows it). A type has one meaning; "sibling of",
  "teacher of" or "born in" are not in it.
* What a collection states with a date (`dated`) belongs to one entity. Time across several subjects that is
  no part of a collection is written as a comparison.
* A statement of a collection has one review state (`source_backed_editorial_review_pending`); a comparison has
  the full list.
* The language a file is written in is English.

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
* The command does not review. It cannot know whether a statement was read against its source; it only refuses a
  file in which nobody says so. A writer or an assistant never runs it to make something visible.

## What the check does not check

That a source says what a record claims; that an address still answers; that a summary is fair, a translation
right or a picture what its caption says; that a text of an early stage keeps the story's secrets. These stay
the work of a reader – which is why every file is handed over for review, however clean its check.
