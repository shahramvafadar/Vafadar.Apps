# 0028. A hypothetical placement in time is a marked dated statement

* Status: Accepted
* Date: 2026-11-28
* Decision of the owner: option (a) of F1 in [the plan of the next content](../content-plan-next.md)

## Context

[The chronology guidelines](../content-chronology-guidelines.md) keep the narrative, literary, historical and
hypothetical chronology of a legend apart (Part I, 5.1) and allow a hypothetical date only when its assumptions,
uncertainty and non-historical status are communicated (Part I, 8): "Never convert speculation into historical
fact." The format could not say this. A `Character` has no `born`, and a dated statement read as a statement of
history: giving Rostam "about 1300 BCE" either turned the hypothesis into a date of history or left it as a
sentence in his summary, where it read as a claim. The guidelines therefore proposed a placement statement, and
the owner approved it.

## Decision

1. **A placement is a dated statement with `placement`**, not a structure of its own. Key, label, summary, time,
   sources, reading stage and review are those of every dated statement (`DatedStatement`); the new optional
   object (`HypotheticalPlacement`) says what makes it a hypothesis:
   * `basis` – `narrative` (the order of the story: the reigns or generations it tells), `synchronism` (the
     reckoning of a named source that fitted the tradition to another history), `editorialHypothesis` (a working
     hypothesis of the editors, not a scholarly estimate);
   * `confidence` – `veryLow`, `low`, `medium`; nothing higher, for nothing of history verifies a placement;
   * `explanation` – optional: why that time, translated like the label and the summary
     (`dated/<key>/explanation` of a translation).
2. **A placement states a time and no quantity.** The time is written in the existing time vocabulary – a year,
   "about", a window between two years – and is never more exact than the hypothesis. The format has no
   "about X within Y–Z": a placement with a centre and a range is written as the window, and the explanation
   names the centre.
3. **Several placements of one entity stand side by side** as competing hypotheses; none is preferred.
4. **A character of a story has no date of history.** A dated statement of a `Character` or an
   `AnimalCharacter` that is no placement is refused by the rules of a collection (`Catalogue`). Before, nothing
   refused it; no collection had one. A placement may stand on an entity of any type.
5. **The page of the entity shows its placements apart**, under the heading "Hypothetical placement in time",
   each with its time, the words of its basis and confidence, its explanation, and the fixed sentence "No
   historically verified date exists." – the wording of Part I, 5.5. The years a visitor chose do not leave a
   placement out: it belongs to the name, not to a year.
6. **No view of time draws a placement.** `Entity.Stated` is what is dated without the placements, and every
   view of time reads it: the chart of time of a collection and of a name, the comparison, a year and a stretch
   of time (`Projection.Timeline`, `Years`, `At`, `Held`, `TimelineLanes`), the series of quantities and the
   link of a name to its chart of time. `Entity.Placements` is read by the page of the entity only.

## Why placements are left out of the charts of time

The proposal allowed either a lane or style of their own in the charts or leaving them out. A style of their
own would have to reach every part a statement passes through – the lanes of a comparison, their layout, the
SVG of the server, the data for the script and the script itself, the moment and the stretch of time – and a
placement drawn as a bar among the bars of history would still be read as one by a visitor who misses the
style. Left out, a placement can never look like a dated fact of history, the charts stay as they were, and a
lane of placements of their own can be added later without changing what is written in the collections.

## Consequences

* The narrative chronology (the reigns a character lived through) stays in words, as before.
* The tool of the editors carries a placement through unchanged: the object stands in the row of its statement
  as it is written in the file, and the explanation of a translation is kept when the form is saved.
* A collection whose summary placed a figure in time in words can now write the placement and take the year
  out of the summary.

## Tests

`DatedStatementReaderTests` (reading a placement, its translation, the vocabulary, the refusals and the rule of a
character), `PlacementPageTests` (the page in English and Persian, and the charts of time without the placement).
