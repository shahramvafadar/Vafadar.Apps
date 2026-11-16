# Vafadar.Temporal

Time as people state it, kept exactly as precise as they state it: a year without a day, "around 1450", "sometime
between 20,000 and 10,000 years ago", "from 2012, end unknown", "55 million years ago", "the second event of the
story". Plain .NET, no dependencies, no texts.

The library never completes what a source leaves open. A year gets no month, a window no middle, a missing end is
not "still going on", and a place in a story is not a date. Decisions and their reasons:
[ADR 0012](../../../docs/adr/0012-temporal-model.md). Not for operational timestamps – when a file was uploaded
is a `DateTimeOffset`.

## Positions

A position is one stated point in time.

| Type | Example | Notes |
|---|---|---|
| `CalendarPosition` | `OfYear(Gregorian, CommonEra, 1980)`, `OfDay(Julian, BeforeCommonEra, 44, 3, 15)` | Year, month or day precision. Era and calendar are separate; the calendar may be `Unspecified`. `Year` is the year as written (500 in "500 BCE"); `ProlepticYear` is for calculating (1 BCE = 0). No year zero. |
| `AgePosition` | `Of(55, MillionYears, AgeReference.Year(2000))` | An age before a reference. Keeps the digits of the source (`55` and `55.0` differ in care, not in value). `AgeBasis.RadiocarbonYears` for uncalibrated ages. |
| `NarrativePosition` | `Of("story-rostam-sohrab", Presentation, 2)` | A place in one ordered sequence of a story. An order, not a time. |

`Approximately()` marks a calendar or age position as approximate. No range is derived from it.

## Expressions

An expression says what is asserted about time.

| Type | Meaning |
|---|---|
| `Occurrence` | Happened at a position. "In 1980" does not mean the whole year, nor that the year is in doubt. |
| `OccurrenceWindow` | Happened once, somewhere between two positions. Nothing lasted that long. |
| `AlternativeOccurrences` | Happened at one of several times. Not averaged, not a range. |
| `Period` | Lasted from a start to an end. Each is known, between two positions, or open. End states: `Ongoing`, `EndedAtUnknownTime`, `Unknown` – three different facts. `Continuity` says whether the source asserts there was no break; `LastConfirmed` is the latest position at which the period is confirmed. |
| `ElapsedTime` | How long, without when: "about thirty years". |
| `EventTime` | The time of another event, by its identifier. The app resolves it. |
| `UnknownTime`, `InapplicableTime` | Not known; does not exist for this thing. |

Every expression can carry the wording of its source in `SourceText`.

Input that contradicts itself is refused with an `ArgumentException`: a window whose end is before its beginning, a
period that ends before it starts, two kinds of time in one statement. Nothing is swapped or repaired.

## Comparing

```csharp
var attended = new Period(PeriodStart.At(solar1380), PeriodEnd.Unknown, PeriodContinuity.Continuous);

TemporalComparer.Overlap(attended, TemporalWindow.Of(solar1383));   // Possible, Basis: OpenBound | PrecisionRange
TemporalComparer.Order(new Occurrence(bce500), new Occurrence(ce1)); // Before
TemporalComparer.Order(new Occurrence(step2), new Occurrence(ce1));  // Incompatible
```

| Answer | Meaning |
|---|---|
| `Definite` / `Before` / `After` | Holds in every reading the records allow – not a claim about what really happened. |
| `Possible` / `Undetermined` | Holds in some readings only. |
| `None` | Holds in no reading. |
| `InsufficientInformation` | A fact is missing: an unknown time, an age without a reference, a period without any stated position. |
| `Incompatible` | Different kinds of time: a date and a story position, two stories, the order of telling and the order of events, two eras, an age and a date, radiocarbon and calendar years. |

`Basis` says what an answer rests on, so the app can explain it: a year read as "somewhere in that year"
(`PrecisionRange`), an approximate value, an asserted continuity, an open bound, or `YearCountOnly`.

Rules worth knowing:

* Two occurrences overlap at most possibly. Sharing a year does not make events simultaneous.
* A period definitely covers a time only when it is asserted to be continuous.
* An open end proves nothing about later times; a `LastConfirmed` position supports the time up to it.
* A `TemporalWindow` is a stretch to search in – the whole of its positions – and asserts nothing.
* Dates in different or unnamed calendars of one era are compared by their year count only, with a margin: less
  than three years apart they are not ordered. Dates of different eras are not compared (no conversion yet).
* Within one statement, what is left unsaid is shared: two ages without a reference have the same one.

## JSON

`TemporalJson.Serialize` / `Deserialize`, or `TemporalJson.Converters` in an app's `JsonSerializerOptions`
(declare members as `TemporalExpression` or `TimePosition`). Reading is strict: unknown kinds and properties,
missing properties and impossible values are a `JsonException`.

```json
{"kind":"occurrence","at":{"frame":"calendar","calendar":"gregorian","era":"commonEra","year":1980}}
{"kind":"occurrence","at":{"frame":"calendar","era":"commonEra","year":1450,"approximate":true},"sourceText":"circa 1450 CE"}
{"kind":"occurrenceWindow","earliest":{"frame":"age","value":60,"unit":"millionYears","reference":2000},
                           "latest":{"frame":"age","value":50,"unit":"millionYears","reference":2000}}
{"kind":"period","start":{"state":"known","at":{…}},"end":{"state":"unknown"},"continuity":"continuous","lastConfirmed":{…}}
{"kind":"occurrence","at":{"frame":"narrative","sequence":"story-rostam-sohrab","order":"presentation","ordinal":2}}
```

| Object | Properties |
|---|---|
| Expression `kind` | `occurrence` (`at`), `occurrenceWindow` (`earliest`, `latest`), `alternatives` (`options`), `period` (`start`, `end`, `continuity`, optional `lastConfirmed`), `elapsed` (`value`, `unit`, optional `approximate`), `eventTime` (`eventId`), `unknown`, `inapplicable`; optional `sourceText` on each |
| Period `start` / `end` `state` | `known` (`at`), `between` (`earliest`, `latest`), `unknown`; for an end also `ongoing`, `endedAtUnknownTime` |
| Position `frame` | `calendar` (optional `calendar`, `era`, `year`, optional `month`, `day`, `approximate`), `age` (`value`, `unit`, optional `reference`, `basis`, `approximate`), `narrative` (`sequence`, `order`, `ordinal`) |

What is not stated is absent: an unnamed calendar, an unstated reference, a missing month. `approximate` is `true`
or absent. Numbers with digits to keep (`value`) are written as they are, without exponents.

## Not here yet

Listed in the [shared libraries backlog](../../../docs/libraries-backlog.md): conversion between calendars and
between ages and dates, decades and centuries as precisions, time of day, formatting for display, and resolving
`EventTime`.
