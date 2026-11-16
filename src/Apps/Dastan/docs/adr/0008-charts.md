# 0008. Charts: who draws the timeline, the family tree and the graph

- Status: **Proposed** – the decision is taken on the comparison below; it becomes Accepted when the first chart
  built on it, the comparative timeline, is implemented and verified
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

## Verification

Open until the comparative timeline is built: its layout tests, the answers of the site, the browser tests at 360,
412 and 1280 px in both languages and both appearances, and the automatic accessibility check.
