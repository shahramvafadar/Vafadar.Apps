# 0021. The timeline is a control of Dastan's own

* Status: Accepted (D-079); takes the place of the timeline of [0008](0008-charts.md)
* Date: 2026-11-19
* Step: 19.9 of the [delivery plan](../delivery-plan.md)

## Context

`Vafadar.Temporal` knows uncertain windows, durations, approximate ages, alternatives, deep time and the
order of a telling. The chart that showed it knew years from 9999 BCE to 9999 CE, drew a window as a point
with a mark, and put what did not fit into a list. No control on the market has an axis for "between 50 and
60 million years ago" beside "in the third night of the telling".

## Decision

* **`DastanTimeline`**, written by us in TypeScript and SVG, behind the layer of [0019](0019-interactive-layer.md).
* **Three axes**: a calendar (BCE and CE, in the calendars of the site), a line of numbers for deep time
  (thousands to billions of years before a named present), and the order of a telling, which has no
  distances. Lanes that share an axis share a view; what belongs to another axis has its own view on the
  page and is not dropped.
* **What is drawn is what is known**: a window in which something happened is a window, a duration is a
  duration, a beginning or an end that is open or uncertain looks open or uncertain, and "about" is a word
  on the mark, not a loss of the window. No date of the browser is used for any of it.
* **Moved in the browser**: zoom and pan without asking the server, with the view kept in the address so
  that it can be passed on; the server still draws the same view still, for a reader without a script.
* **Pictures have two times**: when they were made and what time they show, so that the gallery of a period
  exists and a painting of 1850 of an event of 1020 is found at 1020 as a later picture.
* An event with a date and a place is shown whether or not it has a place in a telling.
* **Asking by time uses the same axes** (R-PUB-04): the search and "what held at this time" take a year
  before the common era, a period and a depth of time, through one reader of what a visitor typed – there is
  no second, narrower idea of a year anywhere on the site.
* **Calendars** (R-TIME-05): a date is shown as it was written and, beside it, in the calendar the visitor
  reads in – Gregorian, Solar Hijri, Lunar Hijri – with "converted" said, and only where the date is exact
  enough to convert.
* **What was chosen is carried** (R-EXP-05): the nodes, the stretch of time and the filters a visitor chose
  go with them from the graph to the timeline to the gallery and back, in the address.
* **A picture in a place** (R-MED-07): the same picture can stand in several places – a profile, a step of a
  telling, a gallery – with a caption and a source of its own in each. A gallery can be of a period and of a
  theme (R-MED-02); themes are kinds of the vocabulary, not free words.

## Consequences

More work than taking a chart off a shelf, and no later rewrite when the first collection reaches before
4713 BC. The check of the year in the address is replaced with the axis's own, which has no lowest number
to stumble on.

## What is built

### Part 1 (2026-11-20): one reader of a time, and one axis of years for the calendar and for deep time

* **One reader** (`AskedTime.Read`, Core): a year of the common era or before it ("1200", "500 BCE", "-500",
  "۵۰۰ پ.م"), a stretch of years ("300 to 400", "600 to 500 BCE") and a depth of time ("12 ka", "55 million
  years ago", "50-60 Ma", "۵۰ تا ۶۰ میلیون سال پیش"), with the digits of Persian and Arabic. What is no time is
  no time, never an error: the lowest number a machine knows, a year 0, a stretch that ends before it begins
  and five thousand digits are all answered. A time is written back in plain letters that the reader takes
  again, so it can stand in an address.
* **One axis of years.** A depth of time that says what it is counted from lies at a year: 12 thousand years
  before 2000 is the year 10001 BCE. One that says only "ago" is counted from the year of the asking. So a
  year, a stretch and a depth are asked of the same records, and a year of the calendar is found among "2 to
  3 thousand years ago" (`TimePlaces`, `YearCover.Of(time, span, present)`). The axis is a decimal number of
  years and has no lowest year; the furthest depth the library knows, a hundred billion years, has a place.
* **What is less than exact only "may have held":** said to be about, counted in radiocarbon years, or "ago"
  without a year it is counted from.
* **One depth is taken as a rounded number:** with half of the place of its last digit that is not a nought to
  each side, and never with less than a hundred years – because "ago" is counted from the year of the
  writing, from 1950 or from 2000 by different sources, and somebody who asks for "55 million years ago" asks
  for what is said to be 55 million years old. A stretch between two depths is taken as it is written.
* **Used by** the search of a collection (the field "When", `?time=`; an address with `?year=` of before is
  still read) and by "what held at this time" (`/at?time=`), which answers for a stretch and a depth what it
  answered for a year. One year typed there is the view of that year, with its steps to the years around it.
* **Deep time is written,** where it was "not stated" before: the number, the unit and what it is counted
  from, as the record says them ("55 million years before the present (counted from 1950)", "5.3 million
  years ago", "…, in radiocarbon years"), a place in a telling, how long something took, and one of several
  times. Nothing is converted.
* **A collection may say a depth of time** of an entity, a relation, a name and a fact of a profile: the
  reader of a collection refused every time that was no date of a calendar; it now refuses only a place in a
  telling, which is what the rule was for.
* **The fault at the lowest number** (R-TIME-04, `ChartState.Year`): `?from=-2147483648` made the page of a
  comparison fail, because the number has no absolute value. It is "no choice" now, with a test.

### Part 2 (2026-11-20): the control – three axes, drawn as what is known, moved in the browser

* **A timeline as views** (`TimelineViews`, Core): every statement goes to the axis its time is counted on – a
  date of the calendar, a depth of time (in years before the year of the visit, which the view names), a place
  in a telling (a view for each telling). A lane appears in the views it has something on. What has no place
  on any axis – a time nobody knows, a year of an era the axis does not count – is named under the views with
  a link to its row, not dropped. A time over two kinds of time cannot be said at all: the library refuses it.
* **What is known is what is drawn.** A window in which something happened once is a dashed frame from its
  first to its last time, never a point at a middle nobody stated and never a bar. What lasted is a bar; where
  its beginning or its end is only known to lie between two times, that part is hatched; "still going on" is
  an arrow; an end nobody states, and "ended, nobody states when", is a fading line with a question mark and
  no arrow; a beginning nobody states comes out of a fading line. What is less than exact is paler; what is
  disputed has the border of a dispute.
* **`timeline.ts`**, our own drawing in SVG, without a library: a line of years for the calendar, a line of
  powers of ten for the depth of time (so that 55 million and ten thousand years stand on one line), a line of
  steps for a telling. Time runs from left to right in every language; the words are the page's. No date or
  clock of the browser is read: every place is a number the server wrote.
* **The data stands in the page**, in an attribute of `<dastan-timeline>`, written by the server with the
  words of the table of the same page (`TimelineData`). There is no second request and no address of data:
  the drawing and the table cannot differ, and the content of a page still holds no script.
* **Moved in the browser:** larger and smaller around the pointer (the key for it with the wheel, two fingers,
  + and −, the buttons), sideways (a finger, the mouse, the arrow keys, the buttons), all of it (0). Nothing
  is asked of the server. The address keeps the view: `from` and `to` of the calendar in the form the still
  chart reads, so the same address without a script shows the same years; a view of another axis under its
  own name.
* **Every mark is a link** to its row of the table, named by what it is and when: Tab walks the marks, Enter
  opens the row, and a reader who hears the page hears the statement and its time.
* **The still chart stays** as the first answer, as the choice of the reader ("Show the still chart") and
  where the script does not run. Its years reach as far as the calendar of the library now (a million years
  to each side); it draws the calendar only – the other two axes are views of the control, and without a
  script their statements are in the table and named under the chart, as before.
* **Where it is:** the comparison of several lanes and the timeline of a collection, which are one component.

**Found by looking, in a browser:** the name of a bar that ran out of the view at the right was out of sight
with it (it now stands on the part of the bar in sight); the words of a mark of the axis at the very side were
cut; a drawing with the role of a picture may not hold links (the frame carries the name of the view instead).

**Looked at by eye** (Chromium, 1100 pixels): the comparison of Iran, China and Rome in English and in Persian,
and a made-up comparison with every kind of beginning and end, deep time and a telling. **Not looked at:** a
phone, touch with two fingers, a screen reader.

**Not built yet (the next parts of the step):** the page of a story on the control (it keeps its own chart of
the telling, opened by links); the time a picture shows and the gallery of a period; calendars shown side by
side; what was chosen carried from view to view.
