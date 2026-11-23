# The file of a collection (`dastan-pilot-0.2`)

A collection is about one subject. Most often that is one story of a work; it can as well be several stories,
or something that is no story – a company, a club, a person, a period (see "What a collection is about").
A collection of a story is that story with everybody and everything in it: the names (entities), what connects them
(relations), the steps of the telling (events), the images and videos (media) and the sources all of this rests
on. The site shows it as pages, lists and charts, never further than a reader has read. This page describes the
editorial file a collection is read from. How to work with it – the rules, the command that checks it – is in the
[guide for writers](content-guide.md).

* Files: `content/samples/<name>.json`, with translations beside it as `<name>.<language>.json`.
* Reader: `Vafadar.Dastan.Data.PilotSample`; model: `Vafadar.Dastan.Core.Catalogue`.
* The file is input of the app and not a public interface: the format may change with its version, and the reader
  refuses what it does not understand – a field it does not know, a required field that is missing or empty, a
  value outside a fixed set. The fixed sets are listed by `vocabulary` (see the guide), which takes them from the
  reader itself.

Below, a field is required unless it is called optional. An identifier is a text without blanks; identifiers are
unique across the whole file – no two records share one, whatever their kind. A day is written `yyyy-MM-dd` in
the Gregorian calendar (D-043). An address is an `https` address without a user name or password.

## The collection

| Field | Meaning |
|---|---|
| `schemaVersion` | `dastan-pilot-0.2`. Of a file with another value nothing else is read |
| `datasetId` | The identifier of the collection |
| `worldId` | The identifier of the world of the story; a media item that names a world names this one. A collection about the real world names it too, with a word of its own |
| `subjectId` | Optional for a collection with exactly one `NarrativeArc` (that story is its subject); otherwise the entity the collection is about – see "What a collection is about" |
| `language` | Only without `editionContext`: the language the texts of the file are written in, as a language tag; `en` when left out |
| `documentVersion`, `preparedAt`, `status`, `reviewNotes` | Optional notes of the editors about the state of the file; kept in the file, not shown |
| `editionContext` | The text the collection follows – see below. Needed when the collection tells a story; a collection that tells none may leave it out |
| `spoilerPolicy` | The reading stages – see below |
| `publication` | What may be done with the file – see below |
| `inputContract` | `rawDatasetPubliclyServable` and `reviewerFilesPubliclyServable`, both `false`: the file and the documents of reviewers are never served. Optional notes for whoever builds a reader: `datasetIdentityNote`, `narrativeContext`, `eventAssociations`, `publicationAndSpoilers`, `mediaEligibility`, `sourceLinks`, `reviewerProvenance` |
| `sources`, `entities`, `relations`, `events`, `media` | The records, as lists. `media` may be empty |
| `threads` | Optional: the threads the editors laid through the collection (see "A thread") |

### `publication`

| Field | Meaning |
|---|---|
| `state` | `editorial_sample` – the working file of the editors – or `published_revision` |
| `localDemoAllowed` | Whether a local demonstration may show the collection |
| `publicReleaseApproved` | Whether everybody may see the collection. An editorial sample cannot be approved; only a published revision can |
| `revision` | Only in a published revision, and required there: `number` (from 1) and `publishedAt` (a day); optional `note`. A correction is published as the next number |
| `history` | Only in a published revision: what each revision changed – see below. Written by the `revision` command, not by hand |
| `note` | Optional |

A published revision that is approved holds only reviewed statements: every relation, every event and every dated
statement has the review status `reviewed` or `disputed`, and each of them carries the record of its review (see
"The record of a review"). The rule and its reasons are in [ADR 0009](adr/0009-published-revision.md). Reviewing
and publishing are decisions of the editors, not of a writer.

`history` is a list with one entry for every revision from 1 to the revision the file is, in that order and
without a gap: `number`, `publishedAt` (a day, not before the entry it follows), an optional `note`, and
`changes` – empty for the first revision. A change names one record that differs from the revision before:

| Field | Meaning |
|---|---|
| `kind` | `added`, `changed`, `removed` or `withdrawn` |
| `record` | The identifier of the record |
| `recordKind` | `source`, `entity`, `relation`, `event` or `media` |
| `label` | The name of the record at that revision – of a removed record, the name it had |
| `spoilerLevel` | The reading stage of the record. A visitor is told of a change only from that stage on |
| `reason` | Only for `withdrawn`, and required there: why the editors took the statement back. It is shown to the public – from the reading stage of the record on |

The site shows the history on the page "what changed" of the collection (`/<collection>/revisions`).

### The record of a review

A relation, an event and a dated statement may carry `review` – who reviewed the statement and when:

| Field | Meaning |
|---|---|
| `by` | Who reviewed it: a name or a role, as the editors want it on record. It is not shown on a public page |
| `on` | The day of the review – not after the day the revision is published |
| `note` | What the statement was checked against. Required for a `disputed` and a `withdrawn` statement, where it is shown to the public: what the sources disagree about, or why the statement was taken back |

`review` belongs to a statement whose `reviewStatus` is `reviewed`, `disputed` or `withdrawn`; on any other
statement it is a problem. An editorial sample may hold reviewed statements with their record before it is published; a published
revision has to have the record on every statement.

### `editionContext`

| Field | Meaning |
|---|---|
| `id` | The identifier of the edition |
| `workId` | The entity of type `Work` the story belongs to |
| `baseText`, `baseUrl` | The text the collection follows, in words, and its address |
| `criticalPrintEdition` | The printed edition it was checked against, or that none was |
| `scope` | What of the work the collection covers and what it leaves out |
| `summaryLanguage` | The language the texts of the file are written in: `English` (or `en`), `Persian` (or `fa`), or any other language tag such as `de` or `ar`. A translation document then translates into another language; `labelFa` is not used in a file written in Persian |
| `calendarMapping` | `null` or left out: the order of a telling is never turned into dates |
| `upstreamAttributedByHost`, `evidencePolicy`, `translationPolicy`, `editorialReview` | Optional notes |

### `spoilerPolicy` – the reading stages

| Field | Meaning |
|---|---|
| `kind` | `minimum_reader_progress_level` |
| `levels` | The stages: `level` (0, 1, 2, … without a gap) and `label`, the name a reader chooses the stage by |
| `defaultLevel` | The stage of a reader who has not chosen one |
| `rule`, `readerKnowledgeNote`, `distributionNote` | Optional notes |

Every record below has a `spoilerLevel`: the earliest stage at which a reader may see it. It has to be one of the
stages. A record never points to a record of a later stage – its sources included.

### `correspondences` – another telling, or another story, of the same world

Optional, inside `spoilerPolicy`. When the site offers another collection about the same world (`worldId`) –
another edition, a retelling, another story of the same work – the editors may state which of its stages a reader of a stage of this
collection has read. A visitor can then go on there from that stage, by their own choice.

| Field | Meaning |
|---|---|
| `collection` | The other collection, by the identifier of what it is about (its name in addresses) |
| `levels` | Pairs `here` and `there`: who has read this collection to stage `here` has read the other up to stage `there`. A later `here` never has an earlier `there` |
| `note` | Optional: a note of the editors; not shown |

State only what is sure: a stage that is not named takes the answer of the nearest named stage before it, and
before the first named stage nothing is carried.

## What a collection is about

* **One story** – the usual case: one `NarrativeArc`, and nothing more to say. The address of the collection
  is the identifier of that story.
* **Several stories**: several `NarrativeArc` entities. `subjectId` names what a visitor enters – one of the
  stories, or the work they belong to; every event says in `storyId` which story it is a step of. The story
  named by `subjectId` – or else the first in the file – is the one at `story`; the others have a page each.
  The reading stages are those of the collection: one ladder for all its stories. **For a line of reading
  stages for each story** (D-058), give each story a collection of its own with the same `worldId`: each then
  has its own stages and its own reading position, the page of each leads to the others, and
  `correspondences` (above) say – where the editors are sure – which stage of one a reader of the other has
  read. Without such a statement nothing is carried from one story to the other.
* **No story**: a collection about a company, a club, a person, a period. `subjectId` names that entity;
  there are no events (`"events": []`), `editionContext` may be left out, and relations have the scope
  `outside_the_story`. What happened and when is said by statements with a date and by relations with a
  `time`. One reading stage is enough: nothing has to be kept back. The command `template subject` writes
  such a file.

## A source

| Field | Meaning |
|---|---|
| `id` | The identifier, by which records cite it |
| `title`, `url` | What it is called and its address |
| `kind` | `primary_literary_text_transcription`, `scholarly_reference`, `museum_object_record` or `institutional_rights_policy` |
| `accessedAt` | The day it was looked at |
| `spoilerLevel` | The earliest stage at which the source may be named: a source that tells the ending belongs to the last stage |
| `passageLocator` | Optional: where in the source the statements are found |
| `reuseNote`, `authors` (a list), `publishedAt`, `updatedAt` | Optional |

## An entity

| Field | Meaning |
|---|---|
| `id` | The identifier; it is the name of the entity in addresses |
| `type` | What kind of thing it is – see "The types of entities". A `NarrativeArc` is a story the collection tells |
| `label` | The name |
| `labelFa` | Optional: the Persian name, when no translation file gives it. Stating it in both places is a problem |
| `summary` | One or two sentences, as far as the stage of the entity tells |
| `sourceIds` | At least one source |
| `spoilerLevel` | The stage |
| `dated` | Optional: what is stated about the entity with a time of the world: a date of a calendar or a depth of time (so many years, thousand, million or billion years ago: `{ "frame": "age", "value": 55, "unit": "millionYears", "reference": 1950 }`, where `reference` is the year it is counted from and is left out for "years ago", and `"basis": "radiocarbonYears"` says that it is counted in such years) – see below |
| `picture` | Optional: the media item that is the picture of the profile – an image of the collection that says in `depictedEntityIds` that it depicts this entity. It is shown to who may see that item; everybody else sees a drawing of the kind |
| `facts` | Optional: the facts of the profile – see "The facts of a profile" |

### The facts of a profile

Every type of entity has facts its profile needs and facts that may be added ([ADR 0017](adr/0017-node-profile.md));
the table is in [the domain model](product/03-domain-model-and-ontology.md), DOM-018. An essential fact that a
record does not state is shown to the reader as "not known" – so state what the sources say, and state that a
time is not known where they say that.

| Field | Meaning |
|---|---|
| `fact` | The key of the fact – one the type of the entity has. A fact is stated once, except the few that may be stated several times (`founder`, `creator`, `participant`) |
| `text` | For a fact that is said in words: the words, in the language of the collection |
| `entityId` | For a fact that names a place or another entity: its identifier. A place is an entity whose type is a place |
| `time` | For a fact that says when: the time as in `dated`. `{ "kind": "unknown" }` states that nobody knows; `{ "kind": "inapplicable" }` that there is none – somebody who lives has no time of death |
| `sourceIds` | Optional: the sources the fact rests on |
| `spoilerLevel` | Optional: the stage from which the fact is told – not before its entity. Until then it is shown like one nobody stated |

A fact says exactly one of `text`, `entityId` and `time` – the one its kind of fact takes. In a story, write
`role` in words that give nothing of the story away, or give the fact the stage from which it may be told.

### `dated` – what has a date

A story has an order and no dates. What really has a date is stated here: when a work was written, when a
manuscript was made, when a place was founded. The collection shows these statements as its timeline
(`/<collection>/timeline`), beside the times its images and videos were made.

| Field | Meaning |
|---|---|
| `key` | The identifier of the statement, unique in the whole file like every identifier |
| `label` | What lasted or happened, in a few words |
| `summary` | Optional: a sentence about it |
| `quantity` | Optional: a quantity the statement states – see "A quantity". With a quantity, `time` may be left out |
| `time` | The time as it is stated, in the form a comparison uses ([the file of a comparison](comparison-format.md), "Time"): `kind` `period` for what lasted, `occurrence` for what happened; years of the common era or before it, in the Gregorian calendar (D-043); "about" and "between two years" as they are stated. A place in the story is not a time of the calendar and is refused |
| `sourceIds` | At least one source |
| `spoilerLevel` | The stage from which the statement is shown – not before its entity |
| `reviewStatus` | `proposed`, `sourcedReviewPending`, `reviewed`, `disputed` or `withdrawn` |

### Disputed and withdrawn statements

A **disputed** statement is one the sources disagree about. It is part of the collection and of a published
revision, and every page that shows it says so: a mark beside it in a list, a sign in a chart, and on the page of
its sources what the disagreement is – the `note` of its review.

A **withdrawn** statement is one the editors took back. It stays in the working file, with a `review` that says
who withdrew it, when, and in its `note` why – and it is no part of the collection: the reader leaves it out, no
page shows it, and a record that still points to it is a problem (a relation whose `scope` names a withdrawn
event, a media item that depicts it). The `revision` command leaves it out of the revision it writes; when the
statement was in the revision before, the history names it as `withdrawn` with the reason. A published revision
that holds a withdrawn statement is refused.

### Where a place is

A real place – `Place`, `Region`, `Country` – may say where it is:
`"position": { "latitude": 36.3, "longitude": 59.6, "precision": "approximate" }`. `latitude` is degrees north
(negative to the south, −90 to 90), `longitude` degrees east (negative to the west, −180 to 180), written with
as many digits as the source gives. `precision`: `exact` (the place is there), `approximate` (about there),
`centre_of_area` (the middle of a region or a country; it says nothing of its borders). A place of a story
(`NarrativePlace`, `NarrativeRegion`) has no position – it is not a place on a map – and neither has
anything that is no place.

### A quantity

A statement about an entity can state a quantity: `"quantity": { "value": 50000, "unit": "verses", "approximate": true }`.
The number is written as the source states it and is shown with every digit – nothing is rounded or converted.
A quantity at a time (a population in a year) has `quantity` and `time`; a quantity that is not said for a
time (the number of verses of a poem) has `quantity` alone.

| Field | Meaning |
|---|---|
| `value` | The number |
| `unit` | One of the units below |
| `approximate` | Optional: `true` when the source says "about" |
| `currency` | With the unit `money` only: the currency as three capital letters (ISO 4217), such as `EUR` |

| `unit` | What it measures |
|---|---|
| `count` | A bare number: how many, where the label says of what |
| `people` | Persons: a population, an army, an audience |
| `members` | Members of a group |
| `employees` | People a company employs |
| `verses` | Verses or couplets of a poem |
| `pages` | Pages or folios |
| `volumes` | Volumes of a work |
| `copies` | Copies printed or sold |
| `manuscripts` | Manuscripts that are known |
| `goals` | Goals scored |
| `matches` | Matches played |
| `points` | Points in a competition |
| `titles` | Titles won |
| `seats` | Seats of a hall or of a parliament |
| `years` | A length of time in years – not a date |
| `days` | A length of time in days |
| `metres` | A length or a height |
| `kilometres` | A distance |
| `square_kilometres` | An area |
| `kilograms` | A weight |
| `percent` | A share of a hundred |
| `money` | An amount of money; currency names it with three letters (ISO 4217) |

### Other names and identifiers of an entity

`names` – optional – lists what else the entity is called. The search finds the entity by each of them, and
its page lists them. A name that two entities share joins nothing: they stay two entities.

| Field | Meaning |
|---|---|
| `name` | The name as it is written |
| `language` | The language of the name as a language tag: `en`, `fa`, `ar` |
| `kind` | `alternative` (also known as), `former` (a name it had before), `abbreviation`, `title` (a title or an epithet), `original_script` (the name in the script it was first written in) |
| `time` | Optional: when the name was used, written like the `time` of a statement with a date |
| `sourceIds` | Optional: the sources of the name |
| `spoilerLevel` | Optional: the stage from which the name may be shown, when the name itself gives something away; without it the name is shown with its entity |

`identifiers` – optional – lists what the entity is called in the catalogues of others. Naming an identifier
does not take over what that catalogue states. For a university, a company, a club or another organisation an
`exact` identifier does one thing more: an entity of another collection of the site that carries the same
identifier of the same catalogue, also as `exact`, is the same place for people who state that they were there
([ADR 0023](adr/0023-institutions.md)). Nothing else makes two entities one place – not a name.

| Field | Meaning |
|---|---|
| `issuer` | Who issued the identifier: the name of the catalogue |
| `value` | The identifier |
| `url` | Optional: the address of the record there, `https` |
| `match` | `exact` (the record is about this entity), `close` (about nearly the same), `uncertain` (not established) |

A new name of the same thing is a `former` name of one entity, not a second entity and not a relation.

### A theme of the gallery

A theme gathers images and videos of the collection: "Combat", "Mourning", "Safavid painting". It is an entity
of the type `Theme` ([ADR 0027](adr/0027-gallery-themes.md)) – with a name, a summary, a stage and translations
like every entity – and a media item names the themes it stands under in `themeIds`. The gallery of the
collection offers every theme a reader may see something under; the gallery of a theme is
`/<collection>/entity/<theme>/gallery`.

* A theme is the editors' grouping. It may leave `sourceIds` out; it names a source where the theme is a term
  of scholarship. Every other entity names at least one source.
* Nothing depicts a theme: `depictedEntityIds` names whom and what an item shows, `themeIds` what it is
  gathered under. Naming a theme in the first or anything else in the second is a problem.
* The same theme in another collection: give both themes the same identifier of the same catalogue in
  `identifiers`, with `"match": "exact"`. The site then shows what stands under them on one page
  (`/themes/<collection>/<theme>`), each collection under its own name. A name joins nothing; two themes of one
  collection must not carry the same exact identifier – and stay two themes also where a theme of another
  collection carries the identifiers of both.
* A theme whose name gives something of a story away gets the stage from which it may be told; its items belong
  to that stage or a later one.

### The types of entities

| `type` | What it is | Kind of thing |
|---|---|---|
| `Work` | A work, such as a poem | a work |
| `NarrativeArc` | A story the collection tells – one, several or none | a work |
| `Character` | A character of the story | somebody |
| `AnimalCharacter` | An animal that is a character of the story | somebody |
| `NarrativePlace` | A place of the story; not a place on a modern map | a place |
| `NarrativeRegion` | A region of the story; it has no modern border | a place |
| `NarrativeObject` | An object of the story | a thing |
| `Person` | A real person, living or historical: a poet, a ruler, a scholar – not a character | somebody |
| `Organisation` | People who act under one name: a court, a dynasty, a school, an institution | a group |
| `Place` | A real place: a city, a village, a building | a place |
| `Region` | A real region: a province, a historical land | a place |
| `Company` | A company: a legal body that does business; not its brands | a group |
| `Brand` | A brand: a name under which things are sold; not the company that owns it | a thing |
| `Product` | A product or a line of products | a thing |
| `University` | A university or another institution of learning; not the place of its campus | a group |
| `Government` | A government or a state as an institution; not the land it rules | a group |
| `Dynasty` | A dynasty: a ruling house over generations | a group |
| `Family` | A family or a house, as a group with a name | a group |
| `Community` | A people or a community; not the land it lives in | a group |
| `Country` | A country as a land with a name; its borders depend on the time | a place |
| `SportsClub` | A sports club | a group |
| `SportsTeam` | One team of a club: the first team, a youth team, the women's team | a group |
| `Competition` | A competition: a league, a cup, a tournament | an occasion |
| `CompetitionSeason` | One season or edition of a competition | an occasion |
| `Event` | Something that happened in the world: a battle, a founding, a match – not a step of the story | an occasion |
| `Period` | A period of history: an era, a reign as a span of time | an occasion |
| `Office` | An office or a title somebody holds: king, vizier, president | an office or honour |
| `Honour` | An honour or a prize that is given | an office or honour |
| `Object` | A real thing: a manuscript, a sword, a coin | a thing |
| `NaturalSubject` | A subject of nature: a species, a river system, an element | a subject |
| `Concept` | An idea, a field of knowledge, a technique | a subject |
| `Theme` | A theme the images and videos of the collection are gathered under: a motif, a genre, a school of painting. A media item names it in `themeIds`; nothing depicts it. Its `sourceIds` are optional – see "A theme of the gallery" | a subject |

Things that share a name stay apart by their type: a `Brand` is not the `Company` that owns it, a `SportsTeam`
is not its `SportsClub`, a `Government` or `Dynasty` is not the `Country` it rules, a `CompetitionSeason` is
not its `Competition`, and an `Event` – something that happened in the world – is not a step of the telling
(the records of `events`).

A thing of the story and a thing of the world are different entities, also when they share a name: the Turan
of the poem is a `NarrativeRegion`; a land of history is a `Region`. Do not make one stand for the other.
A place – of the story or real – takes part in a step of the story only as its setting.

## A relation

| Field | Meaning |
|---|---|
| `id` | The identifier |
| `from`, `to` | The two entities. A relation with a direction reads from the first to the second |
| `type` | One of the relation types of the story: `partOf`, `parentOf`, `spouseOf`, `rules`, `rides`, `visits`, `givesKeepsake`, `entrustedTo`, `carries`, `supportsExpeditionOf`, `opponentOf`, `summonedBy`, `returnsTo`, `mourns`, `huntsNear`, `appearsAt`, `withholdsRemedyFor` – or of the world outside it, see "The relations of the world outside the story". `spouseOf`, `opponentOf` and `siblingOf` are the same from both sides; the others read from the first entity to the second |
| `directionality` | `directed` or `symmetric`; it has to agree with the type |
| `label` | The relation in words, read from the first entity: "Father of" |
| `sourceIds` | At least one source |
| `spoilerLevel` | The stage at which the relation is told – not earlier than both entities |
| `reviewStatus` | `source_backed_editorial_review_pending`; after the review of the editors `reviewed` or `disputed`; taken back by the editors `withdrawn` – see "Disputed and withdrawn statements" |
| `passageLocator` | Optional |
| `time` | Optional, and only with the scope `outside_the_story`: when the relation held, written like the `time` of a statement with a date – `kind` `period` for what lasted (a membership, a reign, a season), `occurrence` for what happened once. Somebody who left and came back is two relations, each with its time |
| `scope` | What the relation is a statement about – see below |

`scope`: `kind` – `work_structure` (a story is part of a work), `background_lineage` (who descends from whom,
before the story), `relationship_revealed_in_beat`, `observed_narrative_context`, `selected_beat_action` or
`reported_in_selected_beat`, or `outside_the_story` (a statement about the world the story was told in: who
wrote the work, where a poet was born); `storyId` – the story (may be left out with `outside_the_story`); `eventIds` – the events that tell or reveal it, possibly
empty; optional `interpretation`; `calendarPeriod` is `null` or left out. A family tree is drawn from the types
`parentOf` and `spouseOf` only.

### The relations of the world outside the story

These types say what they mean and between what kinds of thing they hold; a relation between other kinds is
refused. "Somebody" is a `Person`, a `Character` or an `AnimalCharacter` – the type says what kind of thing,
not whether it is of the story. "Anything" means the type does not say. What reads the other way round is no
type of its own: "student of" is `teacherOf` from the other entity, and the `label` says it in words. A new
name of the same thing is no relation either.

| `type` | Meaning | From | To |
|---|---|---|---|
| `authorOf` | The first wrote or composed the work | somebody or a group | a work |
| `bornIn` | The place or region of birth | somebody | a place |
| `diedIn` | The place or region of death | somebody | a place |
| `teacherOf` | The first taught the second | somebody | somebody |
| `siblingOf` | Brothers or sisters; the same from both sides. A family tree is drawn from parents, not from this | somebody | somebody |
| `memberOf` | The first belonged to the group | somebody | a group |
| `founderOf` | The first founded the group or the place | somebody | a group or a place |
| `locatedIn` | The first lies in, or has its seat in, the second | a place or a group | a place |
| `adoptiveParentOf` | The first adopted or raised the second; a family tree is drawn from parents by birth | somebody | somebody |
| `patronOf` | The first supported the second with money, protection or a commission | somebody or a group | somebody or a group or a work |
| `successorOf` | The first followed the second, in an office or as an organisation | somebody or a group | somebody or a group |
| `holderOf` | The first held the office, bore the title or received the honour | somebody or a group | an office or honour |
| `employedBy` | The first worked for the group | somebody | a group |
| `leaderOf` | The first led, managed or governed the second | somebody | a group or a place |
| `ownerOf` | The first owned the second, wholly or in part | somebody or a group | a group or a place or a thing or a work |
| `studiedAt` | The first studied at the institution or in the place | somebody | a group or a place |
| `alliedWith` | Allies; the same from both sides | somebody or a group | somebody or a group |
| `supporterOf` | The first is known as a supporter of the group | somebody | a group |
| `livedIn` | The first lived or worked in the place | somebody or a group | a place |
| `translatorOf` | The first translated the work | somebody or a group | a work |
| `illustratorOf` | The first illustrated or painted the work | somebody or a group | a work |
| `publisherOf` | The first published the work | somebody or a group | a work |
| `commissionerOf` | The first ordered the work to be made | somebody or a group | a work |
| `dedicatedTo` | The work is dedicated to the second | a work | somebody or a group |
| `adaptationOf` | The first work retells or adapts the second | a work | a work |
| `translationOf` | The first work is a translation of the second | a work | a work |
| `editionOf` | The first is an edition, a manuscript or a copy of the second work | a work or a thing | a work |
| `about` | The work is about the second | a work | anything |
| `keptAt` | The first is kept in the place or by the group | a work or a thing | a place or a group |
| `portrayalOf` | The first portrays the second – a character drawn from a person, a role in an adaptation; they stay two entities | somebody | somebody |
| `possiblySameAs` | Possibly the same; it is not established, and the two stay two entities; the same from both sides | anything | anything |
| `confusedWith` | Commonly taken for one another; they are not the same; the same from both sides | anything | anything |
| `acquired` | The first took over ownership or control of the second; the second need not cease to exist | somebody or a group | a group or a thing |
| `mergedInto` | The first merged into the second, which followed it | a group | a group |
| `spunOffFrom` | The first was split off from the second | a group | a group |
| `subsidiaryOf` | The first belongs to the second as a part with a name of its own | a group | a group |
| `brandOf` | The first is a brand of the second | a thing | a group |
| `productOf` | The first is made or sold by the second | a thing | somebody or a group |
| `governs` | The government or dynasty held power over the land or the people | a group | a place or a group |
| `capitalOf` | The first is the seat of government of the second | a place | a place or a group |
| `teamOf` | The first is a team of the club | a group | a group |
| `playsFor` | The first played for the team or club | somebody | a group |
| `coachOf` | The first coached the team or club | somebody | a group |
| `captainOf` | The first was captain of the team | somebody | a group |
| `loanedTo` | The first played for the group on loan | somebody | a group |
| `transferredTo` | The first moved to the group | somebody | a group |
| `competesIn` | The first took part in the competition or season | somebody or a group | an occasion |
| `winnerOf` | The first won the competition, the season or the event | somebody or a group | an occasion |
| `organiserOf` | The first organised the competition or the event | somebody or a group | an occasion |
| `seasonOf` | The first is a season or an edition of the second | an occasion | an occasion |
| `participantIn` | The first took part in the event | somebody or a group | an occasion |
| `tookPlaceIn` | The event took place there | an occasion | a place |
| `during` | The first happened in, or is part of, the period | an occasion | an occasion |
| `worksIn` | The first worked in the field or on the subject | somebody or a group | a subject |
| `foundIn` | The subject is found in the place | a subject | a place |

When a relation held – from when to when somebody played for a club, held an office, ruled – is the `time` of
the relation. When something happened to one entity alone – a birth, a death – is a statement with a date of
that entity (`dated`). A relation of the story has no `time`: what a story tells has an order, not dates.

## An event – a step of the telling

| Field | Meaning |
|---|---|
| `id` | The identifier |
| `storyId` | The story the event is a step of. Left out when the collection tells one story; needed when it tells several. Every story counts its steps from 1 |
| `label`, `summary` | The name of the step and what is told in it |
| `order` | The place in the telling: 1, 2, 3, … No two events share a place |
| `orderBasis` | `narrative` |
| `temporalExpression` | An object whose `kind` is `narrative_position` – with `calendarMapping` and `duration` `null` or left out. A year, a date, a start or an end here is refused |
| `entityAssociations` | Who and what has to do with the step, at least one – see below |
| `sourceIds` | At least one source |
| `spoilerLevel` | The stage |
| `reviewStatus` | `source_backed_editorial_review_pending`; after the review of the editors `reviewed` or `disputed`; taken back by the editors `withdrawn` – see "Disputed and withdrawn statements" |
| `grouping` | Optional: `editorial_beat_may_condense_multiple_scenes`, when the step stands for several scenes |

An association has `entityId`, `role` (in words: "traveler searching for Rakhsh") and `presence`:
`acts_in_selected_scenes`, `involved_in_selected_scenes`, `setting` (a place or region is always this),
`object_in_selected_scenes` (an object is always this), `mentioned_only`, `subject_of_report` or
`deceased_subject`. An entity is associated with a step once. `participantIds` is refused: a list of names does
not say what each has to do with the step.

## A media item

| Field | Meaning |
|---|---|
| `id` | The identifier |
| `type` | `image` or `video` |
| `label`, `caption` | The name and the caption |
| `creator` | Who made it |
| `rights` | `status` – `original_project_content`, `verified_open_access_metadata` or `permission_not_verified`; `statement`; `checkedAt` (a day); optional `license` and `sourceUrl` (the page of the policy) |
| `depictedContext` | `kind` – `later_artwork_of_literary_narrative` or `present_day_explanatory_diagram`; `worldId`; optional `scope`. `calendarDate` is refused: an artwork does not date the story |
| `creationDate` | When the item was made – see below |
| `depictedTime` | Optional: the time the item shows, where it shows a time of the world – apart from `creationDate`, and never guessed from it. Written like the `time` of a dated statement (a date of a calendar, a window between two, a depth of time). A step of the telling an item shows is not written here: that is `depictedEventIds`. The gallery of a time finds an item by this time |
| `placements` | Optional: the places the item stands in with words of their own (R-MED-07). Each names one `entityId` or one `eventId` – one the item lists in `depictedEntityIds` or `depictedEventIds`, each at most once – with a `caption` and optional `sourceIds`. The item is referred to, not copied: where it has no placement its own `caption` is shown |
| `demoRenderEligibility` | `allowed` – whether a local demonstration shows the item; optional `reason` |
| `publicRenderEligibility` | Optional: `allowed` – whether the item is shown to everybody in a published revision, a decision of its own; optional `reason`. It needs rights that are `original_project_content` or `verified_open_access_metadata`, and what showing an item needs |
| `sourceIds`, `spoilerLevel` | As for every record |
| `url` | Optional. The file, as a path relative to the collection's file with forward slashes (`../media/x.png`), inside `content/`; or an `https` address of a file that is not shown from here |
| `sourceUrl` | Optional: the public page of the item, an `https` address. An item that may not be shown and has this page stands in the galleries as a **reference**: its catalogue record and a link to the page, without a picture ([ADR 0006](adr/0006-media-delivery.md), addendum) |
| `altText`, `altTextStatus` | Optional: the text alternative and its state – `not_prepared_image_not_cleared`, `draft_pending_visual_review` or `project_authored_for_exact_diagram` |
| `depictedEntityIds`, `depictedEventIds`, `relatedRelationIds` | Optional lists: whom and what the item shows |
| `themeIds` | Optional list: the themes the item is gathered under – entities of the type `Theme`, each once, none of a later stage than the item. See "A theme of the gallery" |
| `posterUrl`, `captions` (`language`, `url`), `transcriptUrl` with `transcriptFormat` `text/plain` (a `.txt` file), `renditions` (`format`, `url` – the same item in another format), `sizes` (`width`, `url` – for an image: the same picture narrower, in pixels; each narrower than the image, no width twice; the tool writes them when an image is brought in), `durationSeconds`, `audioTrack`, `dimensions` (`width`, `height`) | Optional; local paths like `url` |
| `catalogTitle`, `accessionNumber`, `creditLine` | Optional: the record of a museum |
| `reviewerProvenancePath` with `reviewerProvenanceAudience` `reviewer_only` | Optional: a working document for reviewers, outside `content/`. It is checked and never shown; the `revision` command leaves it out of a revision |
| `status`, `deliveryStatus` | Optional notes |

What may be shown: only an item of `original_project_content` that is allowed for the demonstration and has its
file; an image then needs a text alternative with the state `project_authored_for_exact_diagram`, a video its
playing time, captions and a transcript. An item without established permission has no file.

`creationDate`: `label` (the date as the catalogue words it), `precision` (`year` or `day`), `approximate`
(`true` for "about"), `era` (`CE` or `BCE`), and `year` for the precision `year` or `date` (`yyyy-MM-dd`) for
`day`. `calendar` is `Gregorian`; a year whose calendar the catalogue does not name leaves it out, a day always
names it. Optional notes: `refersTo`, `calendarStatus`.

## A thread

A way through the collection that its editors lay (step 14.9): a few stops in an order, each with the reason
it follows. A thread says in which order to read; it states nothing about the world and has no sources – what
its stops state has its sources there.

| Field | Meaning |
|---|---|
| `id` | The identifier, unique among the threads; it is in the address of the page of the thread |
| `label` | The name of the thread |
| `summary` | What it is about, in a sentence. It is read from the stage of the thread on, so it gives nothing away that this stage does not |
| `spoilerLevel` | The reading stage from which the thread is offered – no earlier than the stage of its first stop |
| `stops` | At least two. Each names one thing – `entityId`, `eventId` or `mediaId` – and has a `note`: why the thread leads there. No stop twice |
| `note` | Optional: a note of the editors about the thread; not shown |

A later stop may belong to a later reading stage. A visitor follows the thread as far as they have read; the
page says only that it goes on. What stands behind a stop that is not reached is not shown either, so put the
stops in an order a reader can follow: the later the stage, the later the stop.

## A translation (`dastan-translation-0.1`)

| Field | Meaning |
|---|---|
| `formatVersion` | `dastan-translation-0.1` |
| `datasetId` | The collection it belongs to |
| `language` | The language tag, for example `fa`; not the language of the collection itself |
| `review` | `draft` or `reviewed`; every text of the file carries this state |
| `note` | Optional remark of the translator |
| `readingStages` | Level → name of the stage: `{ "0": "…", "1": "…" }` |
| `entities` | Identifier → `label`, `summary` |
| `relations` | Identifier → `label` |
| `events` | Identifier → `label`, `summary`, and `roles`: entity identifier → the role of that entity in the event |
| `media` | Identifier → `label`, `caption`, `altText` |
| `threads` | Identifier → `label`, `summary`, and `notes`: the identifier of what a stop names → the note of that stop |
| `dated` | Key of a dated statement → `label`, `summary` |
| `media` … `places` | Inside the texts of an item: the identifier of the entity or the event of a placement → its caption (`"places": { "character-rostam": "…" }`) |
| `entities` … `facts` | Inside the texts of an entity: the key of a fact → its words (`"facts": { "role": "…" }`) |

Every part is optional. A text that is left out is shown in the language of the collection and marked as such. A
translation that names something the collection does not have is a problem.

## What the reader refuses

Beyond a single field: an identifier used twice; a record that cites no source, a source that does not exist or
belongs to a later stage; a relation, an association or a media item that points to a record that does not exist
or belongs to a later stage; a stage that is not one of the stages; stages that do not start at 0 or have a gap;
a dated statement before its entity, or with a time that is a place in a telling; none or several stories; two events at the same place of the telling; an event without an association, or an
entity associated twice; a place that is not the setting, an object that is not an object; a media item that is
shown without what showing it needs; a local path that leaves its folder or names a file that does not exist; a
translation into the language of the collection, a second one into the same language, or one for another
collection; a review recorded for a statement that is not reviewed, or dated after the revision; a history in an
editorial sample, or one that leaves out a revision, ends with another revision than the file is or goes back in
time. Every problem is reported with the name of its record; the app does not start with a collection it
cannot read.
