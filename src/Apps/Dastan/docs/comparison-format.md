# The file of a comparison (`dastan-comparison-0.1`)

A comparison puts several subjects side by side over one kind of time: what lasted in each of them (periods) and
what happened (single events). The site draws it as a timeline with one lane per subject and shows the same lanes
as a table ([ADR 0008](adr/0008-charts.md)). This page describes the editorial file a comparison is read from. Like
the sample of a collection it is input of the app and not a public interface: the format may change with its
version, and the reader refuses what it does not understand.

* Files: `content/comparisons/<name>.json`, with translations beside it as `<name>.<language>.json`.
* Reader: `Vafadar.Dastan.Data.ComparisonSample`; model: `Vafadar.Dastan.Core.Comparison`.
* The first comparison, `iran-china-rome.json`, is a **draft**: its periods and years are the commonly given ones,
  written down without sources. It is released to the local demonstration only.

## The comparison

| Field | Meaning |
|---|---|
| `formatVersion` | `dastan-comparison-0.1`. Another value is refused |
| `id` | The name of the comparison in addresses (`/compare/<id>`): lower-case letters, digits and hyphens |
| `status` | `draft` or `reviewed` – the state of the file as a whole, for editors |
| `note` | Optional. A remark of the editors; the site does not show it |
| `frame` | The kind of time all lanes share. Only `commonEraYears` exists: years of the common era and before it. Lanes stand side by side only when they are counted in the same way (document 04) |
| `title`, `summary` | English texts |
| `show` | `from` and `to`: the stretch of time the comparison is about, as two positions in time. The chart starts with this stretch; a visitor can move it |
| `publication` | `localDemoAllowed` and `publicReleaseApproved`. A comparison that is not approved for the audience of the site does not exist for it |
| `sources` | The sources the statements cite: `id`, `title`, `url` (https only), `kind` (`primaryText`, `scholarlyReference`, `museumRecord`, `rightsPolicy`), `accessedAt` (`yyyy-MM-dd`), optional `passageLocator` |
| `lanes` | The subjects, in the order they are drawn |

## A lane, a period, an event

A lane has a `key`, a `label`, `periods` and `events`. A period and an event have the same fields:

| Field | Meaning |
|---|---|
| `key` | Unique in the whole comparison – lanes, periods and events share one set of keys. It is the name of the row in the table (`#item-<key>`) |
| `label` | The English name |
| `summary` | Optional. A sentence that the table shows under the name |
| `time` | The time as it is stated, in the JSON form of `Vafadar.Temporal`. A period has `kind: "period"`; an event usually `kind: "occurrence"` |
| `sourceIds` | The sources that support the statement and its time. May be empty; the page then says that no source is recorded |
| `reviewStatus` | `proposed`, `sourcedReviewPending`, `reviewed`, `disputed` or `withdrawn` |

## Time

Time is written as it is stated and never made more exact than that:

```json
{ "kind": "period",
  "start": { "state": "known", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 678, "approximate": true } },
  "end":   { "state": "between",
             "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 247 },
             "latest":   { "frame": "calendar", "era": "beforeCommonEra", "year": 141 } },
  "continuity": "notAsserted" }
```

* There is no year zero, and an era is not a calendar: `calendar` is left out when the source does not name one.
* `approximate: true` is "about". The bar fades at that end, the year on it is written as "c.", the table says
  "about".
* An end `between` two years is drawn to the middle between them and fades; the bar carries no year for it, because
  nobody stated one. The table says "between … and …".
* A period whose start is `unknown`, whose end is `ongoing`, `endedAtUnknownTime` or `unknown`, a period that ends
  in the year it begins in, and any time that is not counted in the years of the frame have no place on the axis.
  They are not drawn; the page names them under the chart and keeps their rows in the table.

## A translation

| Field | Meaning |
|---|---|
| `formatVersion` | `dastan-comparison-translation-0.1` |
| `id` | The identifier of the comparison it belongs to |
| `language` | The language tag, for example `fa`; not the language of the comparison itself |
| `review` | `draft` or `reviewed`; every text of the file carries this state |
| `note` | Optional remark of the translator |
| `title`, `summary` | Optional |
| `lanes` | Lane key → name |
| `items` | Key of a period or an event → `label` and optional `summary` |

A translation may leave texts out – they are shown in the language of the comparison and marked as such – but it
may not name a lane or an item the comparison does not have.

## What the reader refuses

A field it does not know, a required field that is missing, another format version, a name it has no meaning for
(frame, status, review state, kind of a source), a time it cannot read, a period whose time is not a period, a
source without an https address or without a day, a key that is used twice, a cited source that is not listed, a
stretch of time that cannot be placed on the axis or ends before it begins, and a translation for another
comparison. Every problem is reported with the name of the file; the app does not start with a comparison it cannot
read.
