# Work order: the first real content of Dastan

This page is the whole brief for whoever writes the first real content of Dastan – a person, or an assistant
that is given this page in a conversation of its own. It was written on 2026-11-23 on the owner's word. It says
what is to be written, where, how, in which order and when it is finished. Where this page and
[the guide for writers](content-guide.md) differ, the guide and the check of the site are right; tell the owner.

Read first, in this order, and do not skip one:

1. This page, whole.
2. [The guide for writers](content-guide.md) – the four rules, the command, the reading stages.
3. [The file of a collection](collection-format.md) and [the file of a comparison](comparison-format.md).
4. `AGENTS.md` at the root of the repository and `src/Apps/Dastan/AGENTS.md` – how work is committed here.

## 1. What you are asked for, and what not

**Asked for:** the collections and the comparison of part 4, in English with Persian beside each, as files in
`src/Apps/Dastan/library/`; and, on the owner's machine only, the owner's family tree (part 7).

**Not asked for, and not to be done:**

* No change of the software, of a test or of a format. If the format cannot say something, leave it out and
  write it into your hand-over note (the guide, "What the formats cannot say yet").
* **No change of anything in `src/Apps/Dastan/content/`.** That folder is what several thousand tests are
  written against; one more entity in `shahnameh-pilot.json` breaks them. Real content lives in `library/`.
* **No review and no release.** Every statement you write is not yet reviewed
  (`source_backed_editorial_review_pending` for a relation and an event, `proposed` for a dated statement and a
  period or event of a comparison, `draft` for a translation). `publication.state` stays `editorial_sample`,
  `publicReleaseApproved` stays `false`. You never run `revision`, `store review`, `store publish` or
  `store release`. Reading a statement against its source and publishing are the owner's (ADR 0009).
* No picture whose rights are not established (part 6).
* Nothing personal about the owner or the owner's family in the repository – in no file, no commit message, no
  test (part 7).

## 2. The rules of the content

The four rules of the guide hold without exception. In the words of this task:

1. **Every statement has a source you opened and read for that statement.** A name, a year, a parent, a
   capital: each cites the page it comes from. Do not write a year from memory and look for a source that fits.
   Do not cite a book you cannot open. What you cannot support is left out and named in the hand-over note.
2. **Never more exact than the source.** "About 550 BCE" is `approximate: true`. "Between 1046 and 1027 BCE"
   is a window with both years. A date that is tradition and not history is said to be tradition – see
   "Tradition and dispute" below.
3. **Write in your own words.** A summary is one or two sentences of yours about what the source says. Do not
   copy a sentence of a source, of a translation of the poem or of an encyclopaedia.
4. **A story has an order and no dates.** A step of the Shahnameh gets `order` and `narrative_position`, never a
   year. The poem, its poet and its manuscripts are of the world and have dates.

### Which sources

Use sources a reader can open, in this order of preference:

| For | Prefer | Kind in the file |
|---|---|---|
| The text of the Shahnameh | Ganjoor (`ganjoor.net`), the text the pilot follows; name the section | `primary_literary_text_transcription` |
| Iran, and the Shahnameh as a work | Encyclopaedia Iranica (`iranicaonline.org`) | `scholarly_reference` |
| Rome, Greece, the Near East | Livius (`livius.org`); the Oxford Classical Dictionary where it can be read; Encyclopaedia Britannica | `scholarly_reference` |
| China | Britannica; the Metropolitan Museum's Heilbrunn Timeline of Art History; the Chinese Text Project for a passage of a classic | `scholarly_reference`, `primary_literary_text_transcription` |
| An object, a manuscript, a painting | The page of the museum or library that holds it | `museum_object_record` |
| The rights of a picture | The page of the holder that states the terms | `institutional_rights_policy` |

Wikipedia and Wikidata are for finding things, not for citing: follow them to a source of the table and cite
that. Where two sources of the table disagree, cite both and treat the statement as tradition or dispute.
A source is one page with one address; cite the article, not the site. `accessedAt` is the day you read it.

### How a time is written

A time is written in one of these shapes ([the file of a comparison](comparison-format.md), "Time"). Years are
years of the common era (`commonEra`) or before it (`beforeCommonEra`), in the Gregorian calendar; there is no
year zero.

```json
{ "kind": "occurrence", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 331 } }

{ "kind": "occurrence", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 550, "approximate": true } }

{ "kind": "occurrenceWindow",
  "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 1046 },
  "latest":   { "frame": "calendar", "era": "beforeCommonEra", "year": 1027 } }

{ "kind": "period",
  "start": { "state": "known", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 559, "approximate": true } },
  "end":   { "state": "known", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 530 } },
  "continuity": "notAsserted" }

{ "kind": "unknown" }

{ "kind": "inapplicable" }
```

The first is something that happened in a year; the second "about" a year; the third between two years; the
fourth something that lasted – a reign, a dynasty –, whose start and end may each be `known`, `between`
(with `earliest` and `latest`) or `unknown`; `unknown` says that nobody knows, and `inapplicable` that there
is none – somebody who lives has no time of death, a part of a poem no years.

* `approximate` is written only as `true`; an exact year leaves it out (`"approximate": false` is refused).
* A time of the calendar is carried by a fact that says when, a statement in `dated`, another name of an
  entity, `depictedTime` of a media item, and a relation with the scope `outside_the_story`. A relation of a
  story and a step of a story never carry one.

### Tradition and dispute

* A year that tradition gives and history does not confirm – the founding of Rome in 753 BCE, the Xia – is
  written as the tradition gives it, and said to be tradition in words: "traditionally" in the `summary` of
  the entity, or in the `label` of the relation or of the dated statement that carries the year (a fact has
  no words of its own for it). Its source is one that says it is tradition. `approximate: true` only where
  the source says "about".
* Where scholars give different years – the fall of the Shang, the beginning of the Arsacids – write the
  window between the earliest and the latest year the sources give, and say in the `summary` that the sources
  differ. Do **not** set `reviewStatus` to `disputed`: that is a decision of a review.
* Where nobody knows, say so: `{ "state": "unknown" }` for an end of a period, `{ "kind": "unknown" }` for a
  fact of a profile.

## 3. Where things go and how they are named

```
src/Apps/Dastan/library/
  samples/        one file for each collection, and its Persian beside it
  comparisons/    the comparison, and its Persian beside it
  media/          files of pictures whose rights are established – nothing else
```

| File | `datasetId` | `worldId` | What it is about (`subjectId`) |
|---|---|---|---|
| `samples/shahnameh.json` | `shahnameh` | `world-shahnameh` | the work: `work-shahnameh` |
| `samples/rostam-and-sohrab.json` | `rostam-and-sohrab` | `world-shahnameh` | its one story: `story-rostam-sohrab` |
| `samples/siavash.json` | `siavash` | `world-shahnameh` | its one story: `story-siavash` |
| `samples/ancient-iran.json` | `ancient-iran` | `world-ancient-iran` | `subject-ancient-iran` (a `Region`) |
| `samples/ancient-china.json` | `ancient-china` | `world-ancient-china` | `subject-ancient-china` (a `Region`) |
| `samples/ancient-rome.json` | `ancient-rome` | `world-ancient-rome` | `subject-ancient-rome` (a `Region`) |
| `samples/places.json` | `places` | `world-places` | `subject-places` (a `Concept`: places of learning and work) |
| `comparisons/iran-china-rome.json` | – | – | the three over time |

Each has its Persian beside it: `shahnameh.fa.json`, and so on.

**`worldId` is not a label.** The site offers the collections of one world to each other as "the same world,
told elsewhere". That is right for the three collections of the Shahnameh and wrong for three histories: each
history and the places have a world of their own, as the table says. The media items of a collection name its
`worldId` (`depictedContext.worldId`).

**No two collections are about the same thing.** `subjectId` is the name of a collection in addresses, and a
site does not start with two collections of one `subjectId`.

**Identifiers** are lower case with hyphens, begin with what the thing is, and never change once a file is
handed over. They are unique in a file across all kinds of record.

| Kind | Form | Examples |
|---|---|---|
| Source | `src-<site>-<article>` | `src-iranica-achaemenid-dynasty`, `src-ganjoor-sohrab-12` |
| Person, character | `person-<name>`, `character-<name>` | `person-cyrus-ii`, `character-gordafarid` |
| Dynasty, government | `dynasty-<name>`, `government-<name>` | `dynasty-achaemenid`, `government-roman-republic` |
| Office | `office-<title>-<of what>` | `office-king-of-kings`, `office-roman-emperor` |
| Period | `period-<name>` | `period-warring-states` |
| Place, region | `place-<name>`, `region-<name>` | `place-persepolis`, `region-parthia` |
| Event of the world | `event-<what>-<year or name>` | `event-battle-of-gaugamela` |
| Step of a story | `event-01`, `event-02`, … in the order of the telling | |
| Relation | `relation-<from>-<type>-<to>`, with `-2` for a second one between the same two | `relation-cyrus-ii-holder-of-king-of-kings` |
| Dated statement | `<entity>-<what>` | `work-shahnameh-completed` |
| Theme | `theme-<name>` | `theme-combat` |

A ruler who is one of several of a name carries the number: `person-darius-i`, `person-darius-iii`. The same
person in two collections has the same identifier in both (`person-alexander-iii` in Iran and, if he is
named there, elsewhere).

## 4. What is to be written

For each collection: what it holds, how each kind of thing is written, and about how much. The numbers are
there to say how deep to go, not to be met exactly. **What is documented comes before what fills a list**: a
ruler of whom only a name survives gets the name, the office and what the source says, and nothing more.

### 4.1 How a history is written (Iran, China, Rome)

A collection that tells no story: `"events": []`, no `editionContext`, one reading stage, every relation with
`"scope": { "kind": "outside_the_story", "eventIds": [] }`. Begin from
`template subject <name> --out src/Apps/Dastan/library/samples/<name>.json` and replace every `REPLACE`.

| What | Written as | With |
|---|---|---|
| A dynasty or ruling house | entity `Dynasty` | facts `founded`, `seat`; optional `dissolved`, `founder` |
| A state as an institution (the Roman Republic) | entity `Government` | facts `founded`, `seat`; optional `dissolved` |
| A ruler, a general, a writer | entity `Person` | facts `born`, `died`, `knownFor`; optional `birthPlace`, `deathPlace`. A time nobody knows is `{ "kind": "unknown" }` – every `Person` states all three essential facts, known or not |
| A span of history (the Warring States) | entity `Period` | facts `began`, `ended` |
| A battle, a founding, a reform | entity `Event` | facts `happened`, `where`; optional `participant` |
| A capital, a city | entity `Place` | fact `partOf` (its region), and `position` with `precision` |
| A land | entity `Region` | fact `partOf` – naming the larger land as an entity of the file – where it is part of one; left out where it is not |
| The office of the ruler | entity `Office` | fact `partOf` (the dynasty or government) |
| **A reign** | relation `holderOf`, ruler → office, with `time` a `period` from the first to the last year of the reign | the heart of each collection: every ruler has exactly one for each time they ruled |
| Who belongs to which house | relation `memberOf`, person → dynasty | no `time` unless the source gives one |
| Who followed whom | relation `successorOf`, the later → the earlier | between rulers, and between dynasties |
| Parents and partners | `parentOf`, `spouseOf`, `siblingOf` | only where the source says so; never from "son of" in a name |
| Who founded a house or a city | `founderOf` | |
| What a house ruled | relation `governs`, dynasty → region, with `time` | |
| A capital | relation `capitalOf`, place → dynasty or region, with `time` where the capital moved | |
| Who took part in what, and where | `participantIn` (person or dynasty → event), `tookPlaceIn` (event → place), `during` (event → period) | |
| A written work of the time (the Behistun inscription, the Records of the Grand Historian) | entity `Work` – then `authorOf` and `commissionerOf` say who wrote and who ordered it – or `Object`, a thing: `authorOf`, `commissionerOf`, `illustratorOf`, `patronOf` and `dedicatedTo` read to a `Work` only; who made an `Object` is its fact `creator`, where it is kept its relation `keptAt` | |

A fact is written in `facts` of its entity and says exactly one thing: a time (`"time"`), an entity of the same
file (`"entityId"`) or words (`"text"`) – which one is fixed by the fact: `born`, `died`, `founded`, `dissolved`,
`began`, `ended`, `happened`, `created` say a time; `seat`, `where`, `partOf`, `birthPlace`, `deathPlace`,
`keptAt`, `founder`, `creator`, `participant` name an entity (a place where the fact is about a place);
`knownFor`, `role`, `field` are words. The check says so when a fact is given the wrong one.

**A fact is stated once** – only `founder`, `creator` and `participant` may be stated several times. `seat`
names one place: the chief capital. Every capital, with its years, is a `capitalOf` relation.

```json
{ "id": "dynasty-achaemenid", "type": "Dynasty", "label": "…", "summary": "…", "sourceIds": ["src-…"], "spoilerLevel": 0,
  "facts": [
    { "fact": "founded", "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 550, "approximate": true } } },
    { "fact": "seat", "entityId": "place-persepolis" },
    { "fact": "founder", "entityId": "person-cyrus-ii" } ] }

{ "id": "relation-cyrus-ii-holder-of-king-of-kings", "from": "person-cyrus-ii", "to": "office-king-of-kings",
  "type": "holderOf", "directionality": "directed", "label": "King of kings", "sourceIds": ["src-…"], "spoilerLevel": 0,
  "reviewStatus": "source_backed_editorial_review_pending",
  "time": { "kind": "period", "start": { "state": "known", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 559, "approximate": true } },
            "end": { "state": "known", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 530 } }, "continuity": "notAsserted" },
  "scope": { "kind": "outside_the_story", "eventIds": [] } }
```

These shapes are kept as made-up files the tests of the software check with every build, in
`test/Apps/Dastan/Fixtures/work-order-shapes/`: `a-history.json` with its Persian `a-history.fa.json`,
`a-work-without-a-story.json` and `places-people-were-at.json`. Look at them for a whole file in each shape. They were tried on the site on 2026-11-23 with made-up records: a `Region` as what
a collection is about, a `Dynasty`, its `Office`, rulers with `holderOf` and a time, `memberOf`, `successorOf`,
`parentOf`, `founderOf`, `governs`, `capitalOf`, a `Period`, an `Event` with `participantIn`, `tookPlaceIn` and
`during`.

Other names – a throne name, a name in the original script, a Latin or Greek form – go into `names` of the
entity, never into a second entity. Identifiers of other catalogues go into `identifiers`.

The page of the collection shows a timeline of everything that has a time, and the page of a ruler a timeline
of the reign and of what happened in it. So: every reign, every dynasty, every period and every event carries
its time, as exact as the source and not more.

| Collection | Span | What it holds | About |
|---|---|---|---|
| **Ancient Iran** | from the first Elamite kingdoms (about 2700 BCE) to 100 CE | Elam as periods with the rulers that are documented; the Medes and their kings as the sources name them, said to rest largely on Herodotus; the Achaemenids – every king, the capitals, the chief wars and works; Alexander and the Seleucids as far as they ruled Iran; the Arsacids to 100 CE with the kings that are established | 6 dynasties and governments, 45–60 persons, 8–12 periods, 25–35 events, 12–18 places |
| **Ancient China** | from the Shang (about 1600 BCE) to 100 CE; the Xia as tradition | Xia as one period said to be tradition; the Shang with the kings the oracle bones confirm; Western and Eastern Zhou, Spring and Autumn, Warring States with the seven states as entities; Qin and its unification; Western Han – every emperor; Xin; Eastern Han to Emperor He | 6–8 dynasties, 45–60 persons, 8–10 periods, 25–35 events, 12–18 places |
| **Ancient Rome** | from the traditional founding (753 BCE) to 100 CE | the seven kings, said to be tradition; the Republic – its chief offices as entities, the events that shaped it and the people of them (not every consul); the Julio-Claudians, the year of the four emperors, the Flavians, Nerva, Trajan; the provinces as regions only where an event needs them | 3 governments and 3–4 dynasties, 45–60 persons, 6–8 periods, 30–40 events, 12–18 places |

Where the three touch – Alexander in Iran, Rome and Parthia at Carrhae, the Han envoy who reached Parthia –
each collection names its own side of it, with the same identifier for the same person and event.

### 4.2 The Shahnameh as a work

`samples/shahnameh.json`. A collection about a work that tells no story of its own here: `subjectId` is
`work-shahnameh`, there are no events (`"events": []`), one reading stage. It gives the reader the map the
stories lie on.

Because it names stories (`NarrativeArc` entities), the reader takes it for a collection of a work and asks
for an **`editionContext`** – `id`, `workId` `work-shahnameh`, `baseText` and `baseUrl` (https) of the text it
follows, `criticalPrintEdition`, `summaryLanguage`, and in `scope` that it is the work as a whole and tells no
story (the block of the made-up `a-work-without-a-story.json` shows all of them) – and then `language` is
left out (`editionContext.summaryLanguage` says it). Do not begin this file from `template subject` without adding it; the check says "editionContext is
missing" otherwise. This shape – a `Work` as what the collection is about, stories as entities, no events –
was tried with the check and on the site on 2026-11-23.

* **The poet and the poem:** Ferdowsi (`Person`, with `born`, `died`, `knownFor`, `birthPlace`), his patron and
  the court he wrote for; when the poem was begun and finished (`dated` on the work); its length as a quantity
  (`verses`, `approximate`); its sources as the scholarship names them – the prose Shahnameh of Abu Mansur,
  Daqiqi's verses.
* **Its three parts** as `Period` entities of the poem's own world – the mythical, the heroic, the historical –
  each tied to the work by a **relation** `partOf` (a `Period` has no fact `partOf`), with the facts `began`
  and `ended` as `{ "kind": "inapplicable" }` – a part of a poem has no years –, and said in its summary to be
  a division of scholarship, not of the poem.
* **Its dynasties and kings in order:** the Pishdadians and the Kayanians with every king the poem names in its
  order; the Ashkanians as the poem has them; the Sasanians as the poem names them. A king of the poem is a
  `Character`, not a `Person`, also where a historical king stands behind the name. Where the scholarship says
  who that is, write that `Person` into **this** file – a relation never leads into another file – with the
  identifier the history collection gives the same person, and a `portrayalOf` relation from the character to
  it; only where a source says so.
* **How kings and houses of the poem are written.** A line of kings of the poem (Pishdadians, Kayanians) is a
  `Dynasty`, a house of heroes (the house of Sam) a `Family`; both with the fact `founded` as
  `{ "kind": "inapplicable" }` – they are of the poem and have no year – and no `seat` unless the poem names
  one (a place of the poem is a `NarrativePlace` or `NarrativeRegion`, never a `Place`). Kingship is an
  `Office` ("King of Iran, in the poem") with `holderOf` from each king – a `Character` – and `successorOf`
  from each king to the one before; none of these carries a time.
* **The scope of a relation in this file.** What is of the real world – who wrote the poem, where a manuscript
  is kept, which edition is an edition of the work – has the scope `outside_the_story`. A story being part of
  the work has `work_structure`, with `storyId` that story. Everything the poem tells – who is whose parent,
  who is of which house, who was king after whom – has `background_lineage`, with `storyId` the story (a
  `NarrativeArc` of this file) in which the poem tells it; a relation of a story always names its story. The
  made-up `a-work-without-a-story.json` shows each of the three.
* **Its heroes and their houses:** the house of Sam and Zal and Rostam; the houses of Gudarz, of Tus, of
  Afrasiab and Turan; with `parentOf` and `memberOf` as the poem tells them.
* **Its stories** as `NarrativeArc` entities, `partOf` the work, in the order of the poem – the ones that have a
  collection of their own, and the chief others by name with a summary of two sentences that gives no ending
  away.
* **Its manuscripts and editions:** the Florence manuscript, the London manuscript of 1276, the Baysonghori
  and the Shah Tahmasp Shahnameh (`Object`, with `created`, `keptAt`, `editionOf`); the editions of Moscow and
  of Khaleghi-Motlagh (`Work`, `editionOf`; the editor is the fact `creator` of the edition – there is no
  relation for an editor, and `authorOf` is not one). The fact `keptAt` of an object names a
  **place** – the town or the building; the library or museum that holds it is said by the relation `keptAt`,
  from the object to that institution.
* The facts these kinds need: a `Work` `creator` and `created`; a `NarrativeArc` `partOf` (the work); a
  `Character` `role`, in words that give nothing away; an `Object` `created` and `keptAt`.

About: 100–140 entities, 150–220 relations, 15–25 dated statements.

This collection has **one reading stage**, and it stands before every story: it must give no ending of a story
away. The summary of a character says who they are, never how they end. The summary of a story says what it
is about, never how it ends.

### 4.3 Rostam and Sohrab, whole

`samples/rostam-and-sohrab.json`. Begin from a copy of `content/samples/shahnameh-pilot.json` – copy it, do not
edit it – and keep its identifiers and its five reading stages. Of its `editionContext` keep `id`, `workId`
and the text it follows, and rewrite `scope` to what the file now covers; set `documentVersion` and
`preparedAt`, and bring `status` and the notes in `inputContract` to what the file now is.

The copy is three things, and the check refuses it until all three are there (tried on 2026-11-23):

1. the file itself, with its `datasetId` changed;
2. its Persian, `content/samples/shahnameh-pilot.fa.json`, copied to `rostam-and-sohrab.fa.json` with the same new
   `datasetId` – a translation names the collection it belongs to;
3. all eight files of `content/media/`, copied into `library/media/` – the video and the image that are the
   project's own, and the files they name: the poster, the captions, the transcript, the other rendition and
   the document of the reviewers. The items `media-dastan-parentage-video` and
   `media-dastan-family-illustration` name them by `../media/…`. These are the only such files in the library.

The new `datasetId` is `rostam-and-sohrab`. Its `worldId` is `world-shahnameh` already, and its media name it.
Take over its statements **with their state of review as they are**.

The fourteen entities of the pilot carry their Persian name in the file itself (`labelFa`), and the pilot's
Persian gives only their summaries. Leave that as it is: a Persian name given twice – in `labelFa` and as
`label` in the translation – is refused. Every **new** entity, in every file, gets its Persian name in the
translation and no `labelFa`.

Then complete it against the text on Ganjoor, section by section:

* Every step of the telling from Rostam's hunt to the mourning, each with `order`, `orderBasis` `narrative`,
  `temporalExpression` `{ "kind": "narrative_position" }`, its sources, and for everybody and everything in it
  an `entityAssociations` row with the role in words and the `presence` – ask `vocabulary` for the values. A
  step has **no** `passageLocator`: the section of the text is named in the `passageLocator` of its source –
  one source for each section of Ganjoor.
* Everybody the telling names and the pilot leaves out – among them Gordafarid, Hojir, Human, Barman,
  Zhende Razm, Kay Kavus, Gudarz, Giv, Tus, Gostaham – with what they do, each at the stage in which the
  telling first names them.
* The places: Samangan, the White Castle, the camp of Iran, the field of the combat.
* The relations as the telling tells or reveals them, each with the `scope` that says how – a relation that a
  later step reveals belongs to that later stage.
* The reading stages may become more than five if the telling needs it; a stage is a point at which a reader
  stops and has been told everything before it and nothing after.

The rule that matters most here: **a record of an early stage gives nothing of a later stage away** – not in
its summary, not in a relation, not in a source that tells the ending. The check finds a pointer to a later
record; it cannot read a summary. Read every text of stages 0 to 3 once more at the end, as a reader who does
not know the story.

About: 25–35 entities, 40–60 relations, 20–30 steps.

### 4.4 Siavash

`samples/siavash.json`. A second story of the same world (`worldId` `world-shahnameh`), built the same way:
its own reading stages, its own `editionContext` with the sections of the text it follows. From Siavash's birth
to his death and what follows at once; the story of Kay Khosrow is not part of it. Characters that are also in
Rostam and Sohrab – Rostam, Kay Kavus, Tus, Giv – keep the same identifiers. Where you are sure which stage of
one story a reader of the other has read, state it in `spoilerPolicy.correspondences`; where you are not, state
nothing.

About: 30–40 entities, 50–70 relations, 25–35 steps.

### 4.5 The comparison of Iran, China and Rome

`comparisons/iran-china-rome.json`. Begin from a copy of `content/comparisons/iran-china-rome.json`. Bring its
three lanes to the span of the three collections and to the same statements: a period or an event of the
comparison says the same years, with the same uncertainty and from the same source, as the collection does.
Lanes: one for each land; periods are the dynasties and governments; events are the few that a reader should
see side by side (the unification under Qin, Cyrus taking Babylon, the founding of the Republic, Carrhae,
Actium). Every period and event is `proposed`. The kinds of a source are written differently in a comparison than in
a collection: `primaryText`, `scholarlyReference`, `museumRecord`, `rightsPolicy` – ask `vocabulary comparison`.
Its `publication` stays `localDemoAllowed` true and `publicReleaseApproved` false, and its `status` `draft`.

* Set `show.from` and `show.to` to the span of the collections; the copy shows 600 BCE to 700 CE.
* The copy holds periods and events after 100 CE (the Sasanians, the caliphates, Sui, Tang, the later Rome).
  Take them out, and take their entries out of the copied Persian file as well: a translation that names
  something the comparison does not have is refused.
* The time of a period is `kind` `period`; of an event usually `occurrence`.
* The translation of a comparison names it by `id`, not by `datasetId`.

### 4.6 Places people can have been at

`samples/places.json`. A small collection so that a visitor who says where they were finds an entry to choose
(ADR 0023). Ten to fifteen real universities and organisations of several countries – among them the ones the
owner names, if the owner names any – each a `University`, `Company` or `Organisation` with `founded`, `seat`,
its seat as a `Place` with a `position`, its other and earlier names in `names`, and one identifier of a
catalogue (`identifiers`, `match` `exact`) – a ROR identifier for a university is the usual one. Source: the
institution's own page for its founding and seat, as a source of the kind `scholarly_reference`. A `Company` also states `field` – what it does, in words –,
an `Organisation` `founded` and `field`, and what the collection is about (`subject-places`, a `Concept`) its
`field`. The made-up `places-people-were-at.json` of the tests shows the whole shape.

### 4.7 Persian

For every file, a translation beside it (`dastan-translation-0.1` and `dastan-comparison-translation-0.1`),
`"review": "draft"`. Translate every label and summary, the roles in the events, the names of the reading
stages, the facts said in words – but for the entities taken over from the pilot, whose Persian name is in
`labelFa` (4.3). Names of people and places in the Persian form that is usual – for the
Shahnameh the form of the poem, for China and Rome the form Persian books of history use; where there is no
usual form, transliterate and keep it the same everywhere. Do not translate what you did not write in English
first, and do not let the Persian say more than the English.

## 5. How to work

### The loop

```powershell
$content = "src/Apps/Dastan/Vafadar.Dastan.Content"
$library = "src/Apps/Dastan/library"

dotnet build $content                                             # once, and after a change of the software
dotnet run --project $content --no-build -- vocabulary collection # the values a field may have – ask, never guess
dotnet run --project $content --no-build -- check $library        # every file of the library, as the site reads it
dotnet run --project $content --no-build -- check $library/samples/ancient-iran.json --json
```

The exit code is the answer: `0` in order, `1` a file cannot be used. A **notice** – a `REPLACE` text, an
address at `example.org` – means the file is not finished. A file is done when the check says "in order" with
no notice **and the site starts on the library**: the check reads file by file, and only the site sees two
collections about the same thing, a collection named like an address of the site, or a correspondence to a
stage another collection does not have. You do not start the site for that – the tests do:

```powershell
dotnet test --project test/Apps/Dastan/Vafadar.Dastan.Web.Tests -- --filter-class "*LibrarySiteTests"
```

starts the site on `library/` inside the test, as the site starts for a visitor, and asks for the pages of
every collection and comparison of it, in English and Persian. It passes when the site can start on the
library and every one of those pages answers.

Every file in `library/` is read by the tests of the software with every build, and a problem or a notice
fails them for everybody. Never commit a file with a `REPLACE` text or a problem – also not on a branch.

Work **one collection at a time, in batches**: sources first, then the entities that cite them, then the
relations between them, then the Persian. Check after every batch of twenty to thirty records, not once at the
end – the check names every problem, and two hundred at once cannot be read.

### The order

1. `ancient-iran`, then `ancient-rome`, then `ancient-china` – each whole, with its Persian.
2. The comparison.
3. `shahnameh`, then `rostam-and-sohrab`, then `siavash`.
4. `places`.
5. The owner's family tree (part 7), when the owner has made the account (backlog, B9).

### Looking at it

The site is started for looking **only when the owner says so** – not by you on your own, also not to check a
file (the tests above do that). When the owner asks, it is started on the library with its launch profile of
that name and answers at `http://localhost:5220`:

```powershell
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Web --launch-profile library
```

It begins with the first collection of `library/samples` in the order of the names, and refuses to start while
the folder holds none. `Development` shows what is not reviewed, as a local demonstration; a public site would
show nothing of it. Stop it when the owner is done, and before you build again. What is worth looking at, for
each collection: its entry page, its timeline, the page and the timeline of one ruler, the connections of one
ruler, the family of one house, the search, and the same in Persian. Until the owner asks, say in your
hand-over note that the collection was not looked at in a browser.

### Committing

* One commit for each collection that is finished and checked, on a branch `content/dastan-<name>`, merged
  into `main` and pushed, as `AGENTS.md` says. Message: `content(dastan): <what>`, then what is in it, which
  sources it rests on and what was left out for want of a source.
* Commit as the owner and name no tool, assistant or vendor anywhere – in no file, message or comment. The
  hooks of the repository refuse it; do not go around them.
* Set the status of its row in [the backlog](backlog.md) (B1 to B10) to done, with the day.
* After a push, look at the checks of the repository once, after about twenty minutes.

### On this machine

* Write JSON with a tool that writes files, not through a shell here-document: a here-document of the shell of
  this machine loses backslashes and quotes.
* There is no Python. Node is there, and .NET 10.
* Fetch a source with the tool for reading a page; if a page cannot be read, it cannot be cited.

## 6. Pictures

A collection is complete without a picture, and none is asked for in the first pass. **You bring no file of a
picture into the repository** – but for the project's own files that Rostam and Sohrab takes over from the
pilot (4.3). The site shows a picture only when it is the project's own
(`rights.status` `original_project_content`) – and making pictures is not part of this task. A work of a
museum or a library is written as a **reference**: its record, without a file, with the way to the page of its
holder. That is how the pilot writes its paintings; look at `media-met-452419` in
`content/samples/shahnameh-pilot.json` for a whole item.

If you add references:

* The item has no file of its own here. With `permission_not_verified` it has no `url` at all – any is
  refused; only with `verified_open_access_metadata` may `url` be the `https` address of the holder's file, and
  never a path. It has `sourceUrl` – the public page of the
  work at its holder, `https` – and the record of the holder: `catalogTitle`, `accessionNumber`, `creditLine`,
  `creator`, `creationDate` as the catalogue words it.
* `rights.status` is `permission_not_verified`, with a `statement` that says what the holder's page says and
  `checkedAt`. Where the holder states open access on a page you cite as an `institutional_rights_policy`
  source, `verified_open_access_metadata` may be written, with `license` and the address of that policy.
  Establishing the rights of a picture so that it may be shown is the owner's (the backlog, D2), not yours.
* `demoRenderEligibility.allowed` is `false` and `publicRenderEligibility` is left out – it is not yours to
  set.
* A file in a free media collection is not proof of anybody's rights; the page of the holder is what you cite.
* What an item shows is said in `depictedEntityIds` and `depictedEventIds`, the time it shows in `depictedTime`
  – never guessed from when it was made –, and its themes in `themeIds`. A theme is an entity of the type
  `Theme` of the same collection. A theme that another collection has too carries the same identifier of the
  same catalogue as that one, marked `exact` – look at the themes the library has before you write a new one.
  Give a theme the identifiers of one theme only: where a theme carries the identifiers of two themes that
  another collection tells apart, the site joins none of them.
* `depictedContext.worldId` is the `worldId` of the collection.
* References are for the story collections in this pass: `depictedContext.kind` knows a later artwork of a
  literary narrative and a diagram of today, and neither says what a relief or a bronze of a history is. For a
  history, write the thing as an `Object` entity, and name the gap in your hand-over note.
* An item of a story belongs to the stage of what it shows: a painting of the ending is of the last stage.

## 7. The owner's family tree

The owner asked for a family tree in the owner's own account, to complete it on the site afterwards. It is
personal data of living people:

* **It is never in the repository** – not in a file, a test, a fixture, a message of a commit or this page.
  The names are in a file on the owner's machine only: `.local/dastan-owner-tree.json` at the root of the
  working copy, which Git is told to leave alone (`.git/info/exclude`). If that file is not there, ask the
  owner for the names; do not look for them elsewhere.
* It is written into the database of the owner's machine, into the account the owner made there, and into no
  other database.
* The owner makes the account – on the site that runs locally, with a password only the owner knows. Never
  make it for the owner and never sign in as the owner.

Then one command writes the tree, as a new tree of that account:

```powershell
$env:DASTAN_DATABASE = "<the connection of the local database>"
dotnet run --project src/Apps/Dastan/Vafadar.Dastan.Content --no-build -- people import-tree "<the name of the owner's account>" --from .local/dastan-owner-tree.json --people env
```

The file:

```json
{
  "format": "dastan-tree-0.1",
  "name": "The name of the tree",
  "people": [
    { "key": "a", "name": "A name", "born": 1990 },
    { "key": "b", "name": "Another name", "born": 1960, "deceased": false },
    { "key": "c", "name": "A third name", "died": 2001 }
  ],
  "parents": [ { "parent": "b", "child": "a" }, { "parent": "c", "child": "a", "kind": "adoptive-parent" } ],
  "partners": [ ["b", "c"] ]
}
```

`key` is a name of the record inside the file only. `born` and `died` are years of the common era, given
where the owner gives them; nothing is guessed. `kind` of a line of descent is `parent` (the default),
`adoptive-parent` or `guardian`. The file is read strictly: an unknown field, a key used twice or missing,
somebody who would be their own ancestor – each is said with its place, and nothing is written then. Of somebody who lives,
the site keeps the name, the years and the family and nothing else (PRIV-003) – the file has no field for
more. Brothers and sisters are not stated: they are the children of the same parents. The order of people of
the same parents is the order of the file until years of birth say otherwise.

The command writes the tree once: if the account has a tree of that name already it stops and writes
nothing – what is there is changed on the site. No record is said to be the account itself; the owner says
that on the site, where somebody else confirms it. The tree stays private: do not share it, do not make it
public, do not ask anybody to agree to anything.

## 8. When it is finished, and what you hand over

A collection is finished when:

* the check says "in order" with no notice, for the file and its Persian, and `LibrarySiteTests` pass – the site
  starts on the library and answers every page of it;
* every statement cites a source you read for it, and every time is as exact as its source and not more;
* nothing of an early stage gives a later one away (stories), and the work collection gives no ending away;
* where the owner asked for it, it was looked at in a browser, in English and in Persian, and nothing reads as a
  fault – and where not, the hand-over note says it was not looked at;
* it is committed, merged, pushed, and the checks of the repository are green.

Hand over, for each collection, a note of a few lines in the message of its commit and to the owner in the
conversation: what is in it and how much; the sources; **what you left out because you found no source**;
where sources disagree and what you wrote; what the format could not say. Say what you did not check. The owner
reviews from there – a clean check is not a review.

## 9. If something does not fit

Stop and ask the owner – in Persian, with the choices and what you would choose – when: a thing has no type or
relation that says what it is; two sources of the table contradict each other on something that changes the
shape of a collection; a collection would have to change `content/` or the software; or you are asked for
something this page forbids. Do not bend a field to a meaning it does not have, and do not make a statement
visible to see how it looks.
