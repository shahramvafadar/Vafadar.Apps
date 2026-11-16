# 0008. Charts: who draws the timeline, the family tree and the graph

- Status: **Accepted for the comparative timeline** (points 1, 4 and 5: built and verified on 2026-11-16, see
  "What was built"). The family tree (point 2) still has its first layout only; that part stays Proposed until
  step 8.5 of the delivery plan is built
- Date: 2026-11-16
- Deciders: Shahram Vafadar (requirement D-039: charts with many capabilities that look good; for each chart a
  suitable Syncfusion control, a control of our own or a trustworthy open-source one)
- Governing documents: 04 (TIME-006, the layouts of time), 05 (visual tools, timeline and comparison behaviour),
  09 (ADR-P08, visual rendering), 18; requirements R-PUB-05, R-EXP-03
- Related: [ADR 0003](0003-server-rendered-pages.md), [ADR 0005](0005-graph-renderer.md) (the graphs of M0),
  [ADR 0002](0002-one-projection.md), repository [ADR 0012](../../../../docs/adr/0012-temporal-model.md)

## Context

M0 has three drawings: the connections of an entity, a family, and the lanes of a story as a table. The owner asks
for more, and names the example: **several timelines side by side** – the dynasties of China, Iran and Rome as rows
of coloured bars over one axis of years from 600 before the common era to 700 of it, with single events marked
above the axis – so that one can see what happened in each at the same time. Some charts are to run across the page
and some down it; family trees need drawings of their own; most charts have to be easy to open further and to
scroll. And they have to look good.

What any renderer has to respect is decided already:

* **Time is not a `DateTime`.** Years before the common era, an unspecified calendar, "about", a start that is only
  known to lie between two years – the model keeps them (`Vafadar.Temporal`), and a chart must not flatten them.
  There is no year zero.
* **One projection decides.** What a visitor may see is decided on the server before anything is laid out; a chart
  gets the projection's answer and nothing else.
* **Direction of text is not direction of time.** On a Persian page the labels are right to left and the axis keeps
  its direction (documents 04 and 05).
* **A chart is never the only way.** The same facts stand beside it as a list or a table; colour is never the only
  carrier of a meaning.
* **No page needs a script** to show its content (ADR 0003); the content security policy is `default-src 'self'`.

## The candidates

Looked up on 2026-11-16 in the documentation and repositories of the projects; what could not be verified is said.

### Syncfusion Blazor (35.1)

| Component | For | Finding |
|---|---|---|
| Charts, range column on a numeric axis | timeline | The only one whose axis is a number, so years before the common era are possible. Can be turned by 90°, zooms and scrolls, takes annotations. Not verified: labels inside the bars, rewriting the labels of the axis into years of an era |
| Gantt Chart, Scheduler | timeline | Bound to `DateTime`: no year before 1 |
| Timeline | timeline | A list of items one after the other: no scale, no bars, no lanes |
| Diagram, swimlanes | timeline | Lanes without an axis |
| Diagram, complex hierarchical tree | family, graph | A child may have several parents; opening and closing nodes. No layout for genealogy, partners are not a notion. Accessibility for screen readers stated as "intermediate" |

All of them: the guides require an interactive render mode – a live connection per visitor or the app in
WebAssembly – and the script of the vendor; no page of the documentation says that a chart or a diagram renders in
static server rendering, and an export to SVG without a rendered component is not documented. The Community License
covers Blazor.

### Open source

| Library | For | Finding |
|---|---|---|
| vis-timeline (Apache-2.0 OR MIT, active) | timeline | Groups, ranges, points, zoom, an `rtl` option. No vertical axis (open request), no documented accessibility, about 120 kB compressed plus two libraries, needs a DOM; years before the common era only through `Date` objects |
| TimelineJS3 (MPL-2.0) | timeline | A story of slides, not bars per lane |
| Apache ECharts (Apache-2.0) | timeline | Can render SVG on a server – in Node; no right-to-left support; its tooltips break a strict content security policy |
| d3 scales, axis, zoom (ISC) | building blocks | Scales are plain numbers and free of the DOM; everything else has to be built |
| Topola, family-chart | family | Topola is shaped by GEDCOM and draws in the browser; family-chart has one maintainer and a paid edition for some features |
| Cytoscape.js with dagre or ELK (MIT; ELK is EPL-2.0) | graph | Draws on a canvas: nothing to read without it, no keyboard access |

No library draws lanes, years before the common era, a vertical axis and right-to-left labels together, and none
does it without a script.

### Our own drawing

The server lays the chart out and writes it as SVG, with HTML inside for the texts – the way ADR 0005 already draws
two graphs, at 360 px, in both languages, tested. Known ways to lay out exist for each kind: a linear scale with
packing of rows for a timeline; layers per generation with an ordering that reduces crossings (Sugiyama and others;
Gansner and others 1993; Brandes and Köpf 2001) for a family; a tidy tree (Reingold and Tilford 1981; Buchheim,
Jünger and Leipert 2002) for single parents.

## Decision

1. **The comparative timeline is drawn by Dastan itself**: the layout in `Vafadar.Dastan.Core`, on a scale of years
   that `Vafadar.Temporal` provides, written as SVG by the page. One layout serves both directions – across the page
   and down it.
2. **The family tree is drawn by Dastan itself**, as now, with a layered layout that grows with the tree: ordering
   inside a generation to reduce crossings, partners next to each other, ancestors and descendants opened in steps.
3. **The graph of connections stays as decided** (ADR 0005, D-035). A graph that is explored freely over hundreds of
   nodes is the case in which Syncfusion Diagram and Cytoscape.js are tried hands-on.
4. **Scrolling is the browser's**: a chart that is wider or longer than the page stands in a frame that scrolls – by
   touch, wheel and keyboard, without a script. The names of the rows stay in view.
5. **Opening further is an address**: the stretch of time shown, the scale, the rows that are open and the
   direction are part of the address, changed by links – like the opened rows of the connections graph. Every state
   can be shared, reloaded and left by the Back button, and it never carries more than the visitor may see.
6. **One script may be added later, as an improvement only**: pinching and dragging to change the scale. It would
   change what the links change and nothing else; the chart is complete without it. It is not part of the first
   build and needs its own decision (d3-zoom is the candidate).
7. **Syncfusion is used where a live connection is at home anyway**: the tools of editors – tables, forms, uploads –
   and charts of numbers (counts, shares) if the product gets them. Not for the three drawings above.

## Why

* The worth of these charts is that they are right about time and about what a visitor may see. A library whose
  axis is a `DateTime` or a JavaScript `Date` cannot be right about the first; a component that needs the data in
  the browser or a connection per visitor makes the second harder to guarantee.
* What the owner asks for beyond that – rows side by side, both directions, right to left, good looks in two
  appearances – is exactly what no candidate offers together. Adapting one would mean owning its limits as well as
  the adaptation.
* The cost of drawing ourselves is known from M0: the layout is plain code with tests, the drawing is markup and
  style sheets, and the browser tests already check that nothing clips or overlaps.
* Looks are ours to decide this way: the rounded bars, the colour families of the rows, the callouts of events and
  the chips of the axis come from the design tokens (ADR 0004) and follow both appearances.

## What this costs

* Collisions of labels, the stacking of callouts and the packing of overlapping periods are our own work, and so is
  every refinement of them.
* Without the later script there is no pinching; changing the scale is a tap on a link and a new page.
* A chart with thousands of items is a large page. The first collections are far from that; when one comes near, the
  stretch of time shown bounds what is drawn.

## What was built (2026-11-16)

The comparative timeline, steps 8.1 to 8.4 of the delivery plan.

* **Model and file.** A comparison is lanes of periods and single events over one kind of time, each statement with
  its stated time, its sources and the state of its review (`Comparison`, `ComparisonCharts` in
  `Vafadar.Dastan.Core`). It is read from an editorial file by a strict reader
  ([the format](../comparison-format.md), `ComparisonSample`). The first one – Iran, China and Rome from 600 BCE to
  700 CE – is a draft without sources and is released to the local demonstration only.
* **A comparison is not a story.** It has no reading stages, so it does not pass through a `Projection`
  (ADR 0002). Whether it is shown is decided once, when the site reads it: a comparison that is not released to the
  audience of the site is not kept at all (`DastanSite.Comparisons`), and every page and every link to it starts
  from that list.
* **One layout, two drawings.** `TimelineLayout` places bars and callouts along the time; `TimelineChart` draws
  them across the page, `TimelineChartDown` down it. Without a choice of the visitor both are in the page and the
  style sheet shows the one that fits the screen (down below 40 rem); with a choice only that one is written.
* **Left to right in every language.** The chart is laid out the same on a Persian page – the time runs from left
  to right and from top to bottom – and only its texts follow the language.
* **Nothing is made more exact.** An end stated as "about" fades and its year is written as "c."; an end that is
  only known to lie between two years fades and carries no year on the bar; a period that reaches out of the time
  shown is cut to a point; what cannot be placed on the axis is not drawn and is named under the chart. The table
  below the chart states every time in full.
* **Scrolling and opening further** as decided: the frame of the time scrolls by touch, wheel and keyboard, the
  names of the lanes stand still; direction, scale (seven steps) and the stretch of time are in the address and
  changed by links. A year zero, a stretch that ends before it begins and a scale that does not exist are not
  choices – the chart then shows what its comparison is about.
* **No style of its own.** Places and sizes are attributes of the SVG in em, bars and callouts are HTML inside it,
  colours are the tokens of six lanes for both appearances. The security policy of the site did not change.

What differs from the plan of this ADR, and what is not there:

* Down the page the names of the bars are written along the time (turned by a quarter), because a column of a lane
  is too narrow for a line of text on a phone. The callouts keep their direction and take the room the lanes leave.
* The name of a long bar does not stay in view while the time is scrolled; it stands in the middle of its bar.
* A bar that is too short for its name is not a link: it has a title, and its row is in the table.
* A lane is told by its name and its colour; the names of the lanes are not repeated to a screen reader inside the
  drawing. The table is the alternative that needs no drawing.

## Verification

2026-11-16, .NET SDK 10.0.404, Windows 11 x64, Chromium 1.64.1 in the Playwright container:

* Layout and model: `TimelineLayoutTests`, `ComparisonTests` (`Vafadar.Dastan.Core.Tests`, 169 tests);
  `YearAxisTests` (`Vafadar.Temporal.Tests`).
* File: `ComparisonSampleTests` (`Vafadar.Dastan.Data.Tests`, 201 tests) – the comparison of the repository as it
  is, and one refusal per rule of the reader.
* Site: `ComparisonPageTests` (`Vafadar.Dastan.Web.Tests`, 239 tests) – both drawings, the table, the wording of
  time in English and Persian, every choice of the address and its limits, no script and no style in the page, not
  found for the public.
* Browser: `ChartTests` (`Vafadar.Dastan.Browser.Tests`, 55 cases) – at 360, 412 and 1280 px, English and Persian:
  the page never scrolls sideways, the frame of the time does, by the keyboard too; on a phone the time runs down
  the page and the names of the lanes stay at its top; no name of a bar and no callout is cut, no two bars and no
  two callouts lie on each other, every year of the axis stands at its line and no callout lies on one; a bar leads
  to its row and Back returns; opening further doubles the length and
  Back undoes it; the same without a script; no finding of axe-core 4.14 for WCAG 2.1 A and AA and its best
  practices in both appearances.

Two faults were found on the way and corrected. Without a choice of the visitor both drawings were shown on a
phone, because a later rule of the style sheet outweighed the one that hides the drawing for wide screens – a
browser test found it. And the years of the axis did not stand at their lines: their places are written in em, and
an em of a text with a smaller font is smaller, so a year stood up to a hundred pixels away from its line and under
a callout. No test saw that; it was seen in a picture of the page. The size is now on a span inside the text, and
the browser tests check for every drawing that each year stands at its line and that no callout lies on a year –
the check fails on the faulty version.

Not checked: a phone (narrow windows of a desktop browser were used), a screen reader, another browser than
Chromium, a comparison with hundreds of periods. The years of the first comparison were not checked against
sources – that is editorial work and the reason it is a draft.
