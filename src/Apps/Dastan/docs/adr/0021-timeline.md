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
