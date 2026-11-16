# The file of a collection (`dastan-pilot-0.2`)

A collection is one story of a work with everybody and everything in it: the names (entities), what connects them
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
| `worldId` | The identifier of the world of the story; a media item that names a world names this one |
| `documentVersion`, `preparedAt`, `status`, `reviewNotes` | Optional notes of the editors about the state of the file; kept in the file, not shown |
| `editionContext` | The text the collection follows – see below |
| `spoilerPolicy` | The reading stages – see below |
| `publication` | `state`: `editorial_sample`. `localDemoAllowed`: whether a local demonstration may show the collection. `publicReleaseApproved`: `false` – an editorial sample cannot be approved for everybody. Optional `note` |
| `inputContract` | `rawDatasetPubliclyServable` and `reviewerFilesPubliclyServable`, both `false`: the file and the documents of reviewers are never served. Optional notes for whoever builds a reader: `datasetIdentityNote`, `narrativeContext`, `eventAssociations`, `publicationAndSpoilers`, `mediaEligibility`, `sourceLinks`, `reviewerProvenance` |
| `sources`, `entities`, `relations`, `events`, `media` | The records, as lists. `media` may be empty |

### `editionContext`

| Field | Meaning |
|---|---|
| `id` | The identifier of the edition |
| `workId` | The entity of type `Work` the story belongs to |
| `baseText`, `baseUrl` | The text the collection follows, in words, and its address |
| `criticalPrintEdition` | The printed edition it was checked against, or that none was |
| `scope` | What of the work the collection covers and what it leaves out |
| `summaryLanguage` | `English` (or `en`) – the language the texts of the file are written in |
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
| `type` | `Work`, `NarrativeArc`, `Character`, `AnimalCharacter`, `NarrativePlace`, `NarrativeRegion` or `NarrativeObject`. A collection has exactly one `NarrativeArc` – its story |
| `label` | The name |
| `labelFa` | Optional: the Persian name, when no translation file gives it. Stating it in both places is a problem |
| `summary` | One or two sentences, as far as the stage of the entity tells |
| `sourceIds` | At least one source |
| `spoilerLevel` | The stage |

## A relation

| Field | Meaning |
|---|---|
| `id` | The identifier |
| `from`, `to` | The two entities. A relation with a direction reads from the first to the second |
| `type` | One of the relation types: `partOf`, `parentOf`, `spouseOf`, `rules`, `rides`, `visits`, `givesKeepsake`, `entrustedTo`, `carries`, `supportsExpeditionOf`, `opponentOf`, `summonedBy`, `returnsTo`, `mourns`, `huntsNear`, `appearsAt`, `withholdsRemedyFor`. `spouseOf` and `opponentOf` are the same from both sides; the others read from the first entity to the second |
| `directionality` | `directed` or `symmetric`; it has to agree with the type |
| `label` | The relation in words, read from the first entity: "Father of" |
| `sourceIds` | At least one source |
| `spoilerLevel` | The stage at which the relation is told – not earlier than both entities |
| `reviewStatus` | `source_backed_editorial_review_pending` |
| `passageLocator` | Optional |
| `scope` | What the relation is a statement about – see below |

`scope`: `kind` – `work_structure` (a story is part of a work), `background_lineage` (who descends from whom,
before the story), `relationship_revealed_in_beat`, `observed_narrative_context`, `selected_beat_action` or
`reported_in_selected_beat`; `storyId` – the story; `eventIds` – the events that tell or reveal it, possibly
empty; optional `interpretation`; `calendarPeriod` is `null` or left out. A family tree is drawn from the types
`parentOf` and `spouseOf` only.

## An event – a step of the telling

| Field | Meaning |
|---|---|
| `id` | The identifier |
| `label`, `summary` | The name of the step and what is told in it |
| `order` | The place in the telling: 1, 2, 3, … No two events share a place |
| `orderBasis` | `narrative` |
| `temporalExpression` | An object whose `kind` is `narrative_position` – with `calendarMapping` and `duration` `null` or left out. A year, a date, a start or an end here is refused |
| `entityAssociations` | Who and what has to do with the step, at least one – see below |
| `sourceIds` | At least one source |
| `spoilerLevel` | The stage |
| `reviewStatus` | `source_backed_editorial_review_pending` |
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
| `demoRenderEligibility` | `allowed` – whether a local demonstration shows the item; optional `reason` |
| `sourceIds`, `spoilerLevel` | As for every record |
| `url` | Optional. The file, as a path relative to the collection's file with forward slashes (`../media/x.png`), inside `content/`; or an `https` address of a file that is not shown from here |
| `sourceUrl` | Optional: the public page of the item, an `https` address |
| `altText`, `altTextStatus` | Optional: the text alternative and its state – `not_prepared_image_not_cleared`, `draft_pending_visual_review` or `project_authored_for_exact_diagram` |
| `depictedEntityIds`, `depictedEventIds`, `relatedRelationIds` | Optional lists: whom and what the item shows |
| `posterUrl`, `captions` (`language`, `url`), `transcriptUrl` with `transcriptFormat` `text/plain` (a `.txt` file), `renditions` (`format`, `url`), `durationSeconds`, `audioTrack`, `dimensions` (`width`, `height`) | Optional; local paths like `url` |
| `catalogTitle`, `accessionNumber`, `creditLine` | Optional: the record of a museum |
| `reviewerProvenancePath` with `reviewerProvenanceAudience` `reviewer_only` | Optional: a working document for reviewers, outside `content/`. It is checked and never shown |
| `status`, `deliveryStatus` | Optional notes |

What may be shown: only an item of `original_project_content` that is allowed for the demonstration and has its
file; an image then needs a text alternative with the state `project_authored_for_exact_diagram`, a video its
playing time, captions and a transcript. An item without established permission has no file.

`creationDate`: `label` (the date as the catalogue words it), `precision` (`year` or `day`), `approximate`
(`true` for "about"), `era` (`CE` or `BCE`), and `year` for the precision `year` or `date` (`yyyy-MM-dd`) for
`day`. `calendar` is `Gregorian`; a year whose calendar the catalogue does not name leaves it out, a day always
names it. Optional notes: `refersTo`, `calendarStatus`.

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

Every part is optional. A text that is left out is shown in the language of the collection and marked as such. A
translation that names something the collection does not have is a problem.

## What the reader refuses

Beyond a single field: an identifier used twice; a record that cites no source, a source that does not exist or
belongs to a later stage; a relation, an association or a media item that points to a record that does not exist
or belongs to a later stage; a stage that is not one of the stages; stages that do not start at 0 or have a gap;
none or several stories; two events at the same place of the telling; an event without an association, or an
entity associated twice; a place that is not the setting, an object that is not an object; a media item that is
shown without what showing it needs; a local path that leaves its folder or names a file that does not exist; a
translation into the language of the collection, a second one into the same language, or one for another
collection. Every problem is reported with the name of its record; the app does not start with a collection it
cannot read.
