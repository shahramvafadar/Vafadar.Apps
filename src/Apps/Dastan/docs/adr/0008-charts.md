# 0008. Charts: who draws the timeline, the family tree and the graph

- Status: **Accepted** – the comparative timeline (points 1, 4 and 5), the family tree (point 2) and the stage of
  a chart (addendum, D-040) are built and verified on 2026-11-16, see "What was built" and the addendum. Point 6
  (a script for pinching) is not built and still needs its own decision
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
* **Scrolling and opening further** as decided: the chart scrolls in its stage by touch, wheel and keyboard, the
  names of the lanes stand still; direction, scale (seven steps), the stretch of time, the single events and the
  number of lanes are in the address and changed by links – since D-040 at the sides of the stage (see the
  addendum). A year zero, a stretch that ends before it begins and a scale that does not exist are not choices –
  the chart then shows what its comparison is about.
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

## Addendum of 2026-11-16 – the stage, and the family tree (D-040, step 8.5)

After the first pictures the owner asked for two things (D-040): a chart is made larger **from each of its four
sides** – a "+" at a side opens it a step further in that direction, a "−" closes it again – and the application
uses **the whole width and height of the page** for a chart.

**The stage.** A chart stands in a stage (`ChartStage`): as wide as the window, as high as the window, with the
chart scrolling inside it in both directions. A page with a stage is no longer a column in the middle; what is read
– heading, notes, the table – keeps the width of a column of text. Each of the four sides of the stage carries two
steps (`EdgeSteps`). A step is a link to the address of the chart as it will be, so the rule of this decision
holds: every state can be sent, reloaded and left with Back, and no script is needed. A step that cannot be taken
is no link and says so. The sides are the sides of the screen in every language.

| Chart | Side where it begins | Side where it ends | The two other sides |
|---|---|---|---|
| Timeline across the page | left: earlier years | right: later years | top: the single events (where their callouts stand); bottom: the lanes, closed from the last one |
| Timeline down the page | top: earlier years | bottom: later years | left: the lanes; right: the single events |
| Family tree, generations down the page | top: ancestors, a generation at a time | bottom: descendants | left and right: the relatives beside the line – brothers and sisters, uncles and aunts, then their children |
| Family tree, generations across the page | the side where reading begins: ancestors | the other side: descendants | top and bottom: the relatives beside the line |

A step of the time is the distance between two years of the axis at the scale shown. What is closed in the chart –
events, lanes – stays in the table.

**The family tree** (point 2 of the decision). The projection answers the tree around somebody – ancestors up to a
number of generations, descendants down to a number, relatives beside the line up to a number of steps, and the
partners of all of them – and says for each of the three directions whether the tree goes on there
(`Projection.Family(around, up, down, beside)`). The whole family is the other question and stays. The layout
(`FamilyTreeLayout`) puts generations into layers, partners beside each other, looks for an order inside a
generation with fewer crossing lines, moves parents above the middle of their children, and gives every line its
way: children of the same parents share one line across the room between two generations, a child of other parents
has its own, and a parent of two such families lets a line out for each. It replaces the layout of M0, which
filled the width of the page (ADR 0005). The tree opens with two generations above and below; on a Persian page it
begins at the right.

What is not there, and what was seen:

* On a narrow screen the two sides beside the frame take about a quarter of the width. The steps keep the size a
  finger needs; the chart has less room and scrolls sooner.
* A line of a parent who stands two generations above the child runs down through the generation between them and
  may run behind a box there. It did not happen in the families tried.
* Two parents who each have a child with the other count as a crossing although their lines are drawn as one; the
  count is the same for every order, so it does not mislead the search.
* A tree larger than the five people of the sample was looked at in a browser with a copy of the sample outside
  the repository that had sixteen people (down and across the page, both languages, a phone width); it is not part
  of the tests of the pages, because the repository has no larger family. The layout itself is tested with larger
  families.

Verification, 2026-11-16, as above; `dotnet test --solution Vafadar.Dastan.slnf` with the browser: 1230 passed,
none failed, none skipped.

* `FamilyTreeTests` (`Vafadar.Dastan.Core.Tests`, 185 tests in the project): the tree around somebody in the three
  directions and what it announces, nothing of a later reading stage, layers, partners, places, crossings, the way
  of a line.
* `FamilyAndStoryPageTests`, `ComparisonPageTests` (`Vafadar.Dastan.Web.Tests`, 257): the sides of the stage and
  their steps for both charts and both directions, in English and Persian, what an address may ask for, drawing and
  list with the same relations for every member.
* `ChartTests` (62 cases) and `FamilyTreeBrowserTests` (51 cases) in a browser at 360, 390, 412 and 1280 px: the
  stage reaches the edges of the page and is as high as the window; each side has its two steps of at least 44 px
  where the chart grows, on a Persian page as well; a "+" opens and a "−" closes, and Back undoes both; the chart
  scrolls inside the stage and the page never sideways; no box of a tree is cut, lies on another or under a line;
  a tree smaller than its stage stands in its middle; no finding of axe-core 4.14 in both appearances.

Three faults were found on the way and corrected. The steps of a side had a class name that the steps of the story
already used, and the pages of the story and of an entity lost their layout – the browser tests of M0 found it. A
test of the page had been changed by the same renaming and failed for that reason. And in the picture of the
larger family the lines of a father with children by two mothers ran together, so that the drawing said that one
mother was the mother of all of them; no test saw that. Every family now has a line of its own, and the tests of
the layout check it.

## Second addendum of 2026-11-16 – the story as a chart (step 9.1)

The lanes of a story were a table. They are a chart in a stage now (`StoryLanesChart`, cut out of the lanes of the
projection by `StoryCharts`); the table stays below it as the same facts without a drawing.

| Chart | Side where it begins | Side where it ends | Above | Below |
|---|---|---|---|---|
| The story | the earlier steps: "−" closes the first step shown, "+" opens the one before it | the later steps, the same way | the names of the steps: closed, a column carries its number alone | the rows, closed from the last one |

* **An order, not a calendar.** The steps stand one beside the other at equal distances, and the page says that the
  distance says nothing about time. There are no bars from a first to a last appearance. A mark keeps the presence:
  who is only spoken of has a hollow mark and another colour, and every mark says in words who has which part in
  which step.
* **It begins where reading begins.** Unlike a chart of years, the order of a telling is read like text: on a
  Persian page the first step stands at the right, beside the names, and the two sides of the stage change places.
* **Never beyond the reading position.** The chart is cut out of what the projection gives; an address that asks
  for a later step gets the last one the visitor has read, and the "+" at that side is no link – without saying
  whether the story goes on, which the page says in general.
* **A step leads back to the stage.** The links of the sides of every stage now end in the name of the stage
  (`#chart`), so that after a step the chart is on the screen again and not the top of the page. This holds for the
  comparison and the family tree as well.

Verification, 2026-11-16: `StoryChartTests` (Core, 196 tests in the project), `StoryChartPageTests` (Web, 272),
`StoryChartBrowserTests` (33 cases at 360, 390, 412 and 1280 px, English and Persian: the stage and its sides, the
names staying in sight while the steps scroll, no name or head cut, every mark, name and head at least 44 px and
every mark in the column of its step, a step at a side and Back, a mark leading to its step, without a script, no
finding of axe-core 4.14 in both appearances). All tests without a browser: 1008 passed. The browser tests: 281
passed in a run of their own. A run of all projects together did not end – Docker Desktop stopped on this machine
while it ran, and the browser of the tests with it; that says nothing about the site, and it is the reason the two
numbers come from two runs.

## Third addendum of 2026-11-16 – the connections of a name as a chart (step 9.3)

The connections of an entity were the drawing of M0 ([ADR 0005](0005-graph-renderer.md)): the focus at the
top, one row per neighbour. They are a chart in a stage now (`ConnectionsLayout` gives a `ConnectionsChart`,
`ConnectionsGraph` writes it). What the projection answers – the view, and what an address may open – is unchanged.

| Chart | Above | Below | Both sides beside |
|---|---|---|---|
| The connections of a name | who or what a relation leads **from** to the name in the middle | who or what a relation of the name leads **to** | names whose relations with it are the same from both sides, the first where reading goes on, the next where it begins |

* **Like the family tree.** A parent stands above a child and a partner beside – for every relation with a
  direction, not only for parentage. A neighbour with relations of both kinds stands where its relation with a
  direction puts it; with relations in both directions it stands below.
* **A step at a side.** "+" opens the boxes of that side that can be opened and are nearest to the middle, "−"
  closes the opened ones that are farthest from it. Both are links that change the `open` part of the address,
  the same part the "+" and "−" of a single box change; a side with nothing to open or close has no link and says
  so. A side cannot be closed below the neighbours of the name: the page shows all of its connections.
* **What hangs on an opened box stays at its side**, a step further from the middle, whatever the direction of
  the relation – the arrow says the direction, the place says the way the visitor went.
* **Every relation once, every name once.** As in M0, a name that already stands in the chart is referred to by
  a box with a dashed border.
* **Measures in em.** A box is 12 em wide and as high as its text is estimated to need; the chart is as large as
  what it shows and scrolls in its stage. Lines run across or down, never at a slant.
* **Limit.** A stage begins at the top of its chart. When much is opened above the name, the name in the middle
  is below what the window shows first and is reached by scrolling the stage; nothing without a script moves a
  frame to a place inside it without moving the page as well. *(Lifted on 2026-11-17 – see the fifth addendum.)*

Verification, 2026-11-16: `ConnectionsLayoutTests` (Core, 197 tests in the project), `ConnectionsPageTests` (Web,
275), `ConnectionsChartBrowserTests` (27 cases at 360, 412 and 1280 px, English and Persian: where the neighbours
stand, a step at a side and Back with the stage staying on the screen, no box cut or on another one with boxes
opened at every side, every control of a box at least 44 px). All tests without a browser: 1012 passed. The
browser tests: 308 passed in Chromium, in a run of their own.

## Fourth addendum of 2026-11-17 – the timeline of a collection (steps 8.6 and 9.4)

A collection can state something with a time of the calendar about its entities (`dated`, in
[the file of a collection](../collection-format.md)), and each of its images and videos was made at some time.
The page `/<collection>/timeline` shows both as the chart and the table of a comparison.

* **One chart, two pages.** What a comparison page did is the component `ComparisonView`: the tools, the chart
  across and down the page on its stage, the table. A page gives it what is compared, the address its choices are
  written to and how the visitor asked to look at it. The page of a collection builds a comparison in memory from
  the answer of the projection – one lane per entity something dated is stated about, one lane for the images and
  videos – and begins with the stretch of time of what it holds. The four sides of its stage mean what they mean
  for a comparison.
* **The story is not on it.** A step of a telling has a place in the story and no date; the reader of the file
  refuses a dated statement whose time is such a place. The page says so.
* **Never beyond the reading position.** A dated statement has a reading stage like every record, never before
  its entity or its source. The projection gives an entity without its later statements, so that no page that
  shows the entity can show them; an address cannot ask for them.
* **Sources stay inside the site.** A source of a collection is reached through the page that says where it
  leads; a comparison links its sources directly, as before.
* **Long names.** The lanes of a collection carry the names of entities: across the page the column of names is
  wider, down the page a name runs down its lane.
* **Dates are stored in the Gregorian calendar** (D-043): the reader knows no other calendar for a day, and years
  are years of the common era or before it.

The collection of the repository states nothing with a date about its entities yet – that is content, and content
is written apart from the software (D-042). Its timeline shows the two diagrams of the project at the day they
were made, from the third reading stage on; the statements the tests use are test data.

Also in this step: every page asks for the font of the Persian text in its head (`preload`), so that text does
not move when the font arrives after the page.

Verification, 2026-11-17: `DatedStatementTests` (Core, 204 tests in the project), `DatedStatementReaderTests`
(Data, 243), `TimelinePageTests` (Web, 284); all tests without a browser: 1070 passed. The page is one of the
views of the browser tests that check that a view fits the window at 360, 412 and 1280 px in both languages and
breaks no rule of accessibility a tool can check: 318 passed in Chromium. With statements made up for looking,
the chart was looked at in English at 1280 px and in Persian at 390 px.

## Fifth addendum of 2026-11-17 – the two limits of the stage (step 10.4)

* **The name a chart is drawn around is in sight.** The charts of connections and of a family are drawn around one
  name. The one script of the site (`app.js`) moves the frame of such a stage once, when the page has arrived,
  so that the box of the name is in the middle – and only when the box is not wholly in sight already. It does
  not follow a visitor who scrolls afterwards and does nothing on a stage that is not shown. Why a script: a
  fragment of the address moves the page together with the frame, and a stage that is aligned to a place inside
  its chart no longer fills the window; scroll snapping would hold the visitor at the name. The rule of
  [ADR 0003](0003-server-rendered-pages.md) stands – no page needs the script: without it the chart is whole and
  the name is reached by scrolling the stage, as before.
* **On a narrow screen the sides beside the chart stand above it.** Up to 40 rem of width, the steps of the left
  and of the right side stand in a row of their own between the upper side and the chart – the left ones at the
  left, the right ones at the right, their words running along the row – and the frame gets the whole width of
  the stage. Before, the two sides took about 106 of 360 px. A side is still a side of the screen (D-040); wider
  screens are unchanged. One row of height is given for a quarter of the width: the family of the first story,
  opened at every side, now stands in the stage of a phone without scrolling.

Verification: `StageBrowserTests` – with much opened above the name, the box of the name is inside the frame at
360 and 1280 px in English and Persian, also in the family drawing; a chart whose name is in sight is not moved;
without the script nothing is moved and the chart is whole; at 360 px the frame is as wide as the stage, the
steps of the sides stand between the upper side and the chart, each at least 44 px, and the page does not scroll
sideways; at 1280 px the sides stand beside the chart. All browser tests in Chromium, Firefox and WebKit.

## Sixth addendum of 2026-11-18 – the places of a collection (step 14.1)

A real place can say where it is (step 12.8); its page shows the degrees and a `geo:` address. The collection
as a whole now has a page of its places, `<collection>/places`.

* **`Projection.Places()`** answers a `PlacesView`: the real places the visitor may see that state a
  position, each with the relations that end there (`PlaceOnMap`), and the real places that state none. A
  place of a story is in neither list – it is no place on a map (ADR 0001).
* **`PlacesMap.Of`** lays the drawing out, without a page and without a browser: the area the places span,
  a tenth more at each side and at least two degrees; a degree of longitude drawn shorter by the cosine of the
  middle latitude; the longer side 40 units and the shorter at least 16; a grid of whole steps of degrees with
  the widest step that still gives three lines. Places on both sides of the line of the date are laid out
  beside each other, not a whole earth apart.
* **`PlacesChart`** writes it as SVG on the stage: the grid with its degrees, and for each place a mark with
  the number it has in the list. A position that is exact is a filled mark; one that is about there or the
  middle of an area is drawn open, the latter larger – it is no point. A mark leads to its row; the row leads
  to the page of the place and to the position in the map of the device.
* **No map is loaded.** The site asks nobody for tiles – that would tell a third party who reads what – and
  ships no coastlines. So the drawing says how the places lie to each other, and the grid where on the earth
  that is; the page says so. A drawn background (coasts from data that may be shipped, or tiles from a host of
  the owner's) is a decision of the owner.
* The page is offered on the page of the collection when it names a real place the visitor may see.

Not done: borders, routes, and places that move with time; marks that lie on each other are not pulled apart
(two places a few kilometres apart in a collection that spans a continent cover each other – the list still
names both).

Verification: `PlacesViewTests` (7 tests) – which places are on the lists, what lies there, a place of a later
reading stage on none; north at the top, east at the right, every place inside, the grid in whole steps; one
place alone; the line of the date; a pole. `PlacesPageTests` (7 tests) – the page in both languages, the mark
and its row, nothing loaded from anybody, a story alone, another collection. Not looked at in a browser.

## Seventh addendum of 2026-11-18 – a series of quantities (step 14.3)

An entity can state a quantity for a time (step 12.7); its page lists every statement with every digit. When
the same quantity is stated for several times, the page now draws it as well.

* **`QuantitySeries.Of(entity, language)`**: the statements of a quantity whose time can be placed among the
  years of the common era, grouped by what they are called in the language of the collection, by their unit
  and – for money – their currency. A group of two or more is a series, in the order of its times. One number
  alone is none, and neither are numbers that state no time. The format has no field that names a series:
  what is called the same and counted the same is the same quantity. Two things of one entity that are called
  the same in the same unit would be drawn as one – the editors name them apart.
* **`SeriesChart.Of`** lays it out without a page: time from the first to the last number with a little room;
  the value from zero – or from below, when a number is below zero – to the next whole step above the
  greatest, in steps of 1, 2 or 5 times a power of ten. Starting at zero is deliberate: a difference then
  looks as large as it is. A time that is a stretch is drawn at its middle.
* **`SeriesFigure`** writes it as SVG in a figure under the list: the lines of the scale with their numbers
  in the digits of the page, the years, a line from number to number and a mark for each. A number the source
  calls "about" is drawn open. A mark leads to the row of its statement; its title says the quantity and the
  time as the list does. Nothing is converted between units, nothing is added up and nothing is put between
  two numbers but a straight line – which says nothing about the years between; the caption says how many
  numbers there are.

Not done: two series in one drawing, a series across entities (the people of two cities), a scale that is not
linear, and numbers that are a range.

Verification: `QuantitySeriesTests` (5 tests) – what is one series and what is not; the scale from zero, with
small numbers, with numbers below zero and with equal numbers; the years of the scale. `SeriesFigureTests`
(3 cases) – the drawing on the page of a city in both languages with its marks and their rows; one drawing
for one series and none for what is stated once. Not looked at in a browser.
