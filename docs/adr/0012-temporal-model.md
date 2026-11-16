# 0012. A shared model of stated time, compared without invented precision

- Status: Accepted
- Date: 2026-11-16

## Context

Dastan shows stories, history and nature: an event "in 500 BCE", a reign with uncertain ends, a period "50 to 60
million years ago", a company that started in 2012 with an unknown end, and story events that have an order but no
date. Its specification (document 04, "Time, order and uncertainty") requires that these meanings stay apart from
entry to display, that nothing is filled in to make a chart work, and that times of different kinds are not put on
one axis. `DateOnly` and `DateTime` cannot hold any of this, and the date helpers of `Vafadar.Localization` format
ordinary dates only.

None of it is specific to Dastan. The owner decided that parts other apps can use are written as shared libraries
(Dastan decision D-029).

## Decision

1. **A shared library `Vafadar.Temporal`**, plain .NET, without dependencies and without texts. It is for stated
   time – history, story, nature, memory. Operational timestamps stay `DateTimeOffset`, taken from `TimeProvider`.

2. **Positions and expressions are separate.** A position is one stated point: a calendar year, month or day; an
   age before a reference; a place in a story sequence. An expression says what is asserted: an occurrence at a
   position, an occurrence somewhere in a window, alternatives, a period with independently known, uncertain or
   open ends, an elapsed time without a position, the time of another event, unknown, inapplicable. Types refuse
   contradictory input; they never swap or repair it.

3. **Years are stored as people write them:** an era and a year of 1 or more. No era has a year zero. For
   calculating, `ProlepticYear` counts continuously (1 BCE = 0, 500 BCE = −499), so the step from 1 BCE to 1 CE
   is one year. Era and calendar are separate facts; a calendar that the source does not name stays unnamed.

4. **Ages keep their reference, unit, digits and basis.** A fixed reference keeps "12,000 years ago" from moving
   with today's date; an unstated reference is kept as unstated. Uncalibrated radiocarbon years are a basis of
   their own and are never turned into calendar years by subtraction.

5. **A place in a story is an order, not a time.** It names its sequence and whether the sequence is the order of
   telling or the order of events. It has no date and gets none.

6. **Comparing answers in three steps and never guesses.**
   * *Is there a common scale?* Calendar positions of one named calendar; calendar positions of one era by their
     year count; ages of one basis with stated references; positions of one story sequence in one order. Anything
     else is `Incompatible`. A missing fact (an age without a reference, an unknown time, a period without any
     stated position) is `InsufficientInformation`.
   * *What can each side be?* An occurrence is one moment somewhere in its position, window or alternatives. A
     period runs from a start to an end, each somewhere in its position or range; **both ends belong to the
     period** (closed), and a bound stated to the year lies somewhere in that year. An open end is limited only by
     the start and by the last confirmed position.
   * *Does it hold in every reading, in some, in none?* `Definite`, `Possible`, `None` for overlap; `Before`,
     `After`, `Undetermined` for order. Two occurrences overlap at most possibly. A period definitely covers a
     time only when the source asserts that it lasted without a break.

7. **Answers say what they rest on** (`ComparisonBasis`): a coarse position read as its whole unit, an approximate
   value, an asserted continuity, an open bound, or a comparison by year count only.

8. **Calendars are not converted yet.** Dates in different or unnamed calendars of one era are compared by their
   year count with a margin of half a year on each side, because their years do not begin on the same day; they
   are ordered only when at least three years apart. Dates of different eras, and ages against dates, are
   `Incompatible`. Inside one named calendar, positions are ordered on a regular grid of 12 months of 31 places:
   it keeps the order of real dates and puts a day inside its month and a month inside its year, and it is not
   used to measure elapsed days.

9. **One statement shares what it leaves unsaid.** Inside a single expression two ages without a stated reference
   have the same reference and two dates without a named calendar the same calendar; across expressions they do
   not.

10. **JSON is the library's own strict format** with a `kind`, `frame` or `state` in every object. Reading refuses
    unknown and missing properties and impossible values; writing leaves out what is not stated and keeps the
    digits of decimal values.

11. **No formatting for display in this step.** The types expose every part a formatter needs. The formatter is
    added with its first screen, with the words supplied by the app, because the library carries no texts
    ([ADR 0011](0011-languages-per-app.md)).

## Consequences

* The cases the specification asks for are tests of the library: year-only precision, the step across the era,
  an uncertain occurrence against a continuous period, independent uncertain ends, unknown against ongoing ends,
  the direction and the reference of an age, a story without any date, refused comparisons, and a lossless round
  trip of every kind.
* A chart can only place what has a common scale. The other cases come back as `Incompatible` or
  `InsufficientInformation`, and the app has to show them as such – that is intended.
* The year-count rule is cautious: two catalogue dates "1450" and "1451" in unnamed calendars are not ordered.
  A conversion between calendars would remove the margin; it is in the
  [shared libraries backlog](../libraries-backlog.md) together with decades and centuries as precisions, time of
  day, ages against dates and the display formatter.
* The grid of 31 places accepts day numbers a month does not have in calendars whose leap rules are not
  implemented (Solar and lunar Hijri, unnamed). The Gregorian and Julian calendars are checked exactly.
* Zanance is not affected: it does not reference the library.
