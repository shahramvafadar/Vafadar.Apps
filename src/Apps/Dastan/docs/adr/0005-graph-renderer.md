# 0005. The renderer of the connections graph

> Replaced on 2026-11-19 (D-079) by [0020](0020-graph.md): the graph is explored in the browser, on Syncfusion EJ2 Diagram behind a contract of Dastan. The still drawing of this decision stays as what a reader without a script sees.

- Status: **Accepted by the owner on 2026-11-16** (decision D-035): the graphs of M0 are drawn by the server as
  SVG. The comparison below is kept as it was written before the decision.
- Date: 2026-11-16
- Deciders: Shahram Vafadar
- Governing documents: 09 (presentation and rendering), 18 (SCREEN-04), 20 (step B, step C); M0-03
- Related: [ADR 0002](0002-one-projection.md) (graph and list are one answer), [ADR 0003](0003-server-rendered-pages.md)

## Context

Decision 7 of the delivery plan: decide the graph renderer by a bounded comparison in phase 4 – Syncfusion Diagram
first, Cytoscape.js as the single alternative. Building the pages of phase 4 showed a third way that the plan did
not name, and it fits the rest of the architecture better than either. The plan was accepted by the owner, so the
change is the owner's to decide.

What the graph of M0 has to do (document 18, M0-03): show the neighbourhood of one entity – in the sample at most
about a dozen nodes – with **labelled** edges; the same relations as the list next to it; select an edge; move the
focus; expand a neighbour and take that back; work with the keyboard and on a phone of 360 px; work in Persian
(right to left, Arabic-script labels) and in both themes; never be the only way to the content.

## The candidates

| | Syncfusion Blazor Diagram | Cytoscape.js | Server-rendered SVG |
|---|---|---|---|
| How it runs | A Blazor component: needs an interactive mode – a live connection per visitor or WebAssembly | A script in the browser (canvas), fed with JSON through an adapter | The server writes an `<svg>` into the page; nodes and edges are links |
| Fits ADR 0003 (no connection, no script) | No | Needs a script; no connection | Yes |
| Reading boundary | The data is filtered by the projection; the component state lives in the connection or the browser | Filtered JSON in the page | The page is the projection; nothing else is sent |
| Select, move focus, expand, undo | Events in the component | Events in the script | Links: the address names focus and expanded nodes; Back undoes |
| Keyboard and screen reader | The component's own support; the canvas-like surface needs a parallel list | A canvas: nothing is reachable, the list is the alternative | Every node and edge is a real link in reading order |
| Right to left, mixed scripts, themes | Component settings and its own theme files | Labels are drawn by the script; fonts and direction by hand | The page's own CSS and tokens |
| Layout | Built-in automatic layouts | Built-in automatic layouts | Written by us: a ring around the focus, rows for generations |
| Pan, zoom, drag, hundreds of nodes | Yes | Yes, its strength | No |
| Cost | Package and license key in the build, a connection or a runtime download per visitor | One script file to host and to allow in the content policy | A layout function and its tests |

**What was tested and what was not.** Neither Syncfusion Diagram nor Cytoscape.js was installed or run for this
record; their columns come from their documentation and from how Blazor render modes work. The server-rendered
column rests on what phase 4 built: pages without a script, the projection per request, statements as links. No
SVG graph exists yet either. This is a comparison of fit, not of measurements.

## Recommendation

**Render the graphs of M0 as SVG on the server.** The neighbourhoods are small, every gesture the specification
names is a link, the result is accessible by construction and identical to the list because both are the same
answer of the projection. It needs no license key at run time, no connection and no script, and it cannot leak:
there is no data in the browser beyond the page.

Keep the boundary that makes a later change cheap: the page gets a `ConnectionsView` (nodes, edges, expandable
nodes) and hands it to one component. If a later collection needs free exploration of large graphs – pan, zoom,
drag, hundreds of nodes – that component is replaced by an interactive one for those views, and the choice between
Syncfusion Diagram and Cytoscape.js is then made by a hands-on comparison with that collection's data.

## What the owner decides

1. **Accept the recommendation** – phase 5 builds the SVG graph and the family view; or
2. **Keep decision 7 as written** – phase 5 starts with a hands-on comparison of Syncfusion Diagram and
   Cytoscape.js (both need a script or a connection, which changes ADR 0003 for the graph pages); or
3. **Both** – SVG for M0 and a hands-on comparison scheduled before the first collection with a large graph.

## Consequences of the recommendation

* No Syncfusion component is used in M0; the license registration for Blazor in `Vafadar.Web` waits for the first
  component that needs it.
* The layout of small graphs is our code and is tested like the rest.
* The graph does not pan or zoom in M0.


## What was built (2026-11-16)

* **Layout.** `ConnectionsLayout` (in `Vafadar.Dastan.Core`, no markup) turns a `ConnectionsView` into a tidy
  tree: the focus on the starting side, one row per neighbour, the neighbours of an expanded row indented below
  it. A ring around the focus was not used: on a phone of 360 px the labels of eight edges do not fit around a
  centre, and a drawing that only works on a wide screen would make the list the real interface. Heights are
  estimated from the length of the texts for the narrowest screen, because the server cannot measure text.
* **Every relation is drawn exactly once.** A relation to an entity that is already in the drawing is a row that
  refers to it instead of a second node; the tests compare the relations of the drawing with those of the list.
* **Direction** is an arrowhead on the connector, from the first entity of the relation to the second; a relation
  that is the same from both sides has none. The label stands at the row it leads to.
* **Markup.** An `<svg>` whose connectors are lines in percent and `em`, and whose nodes are HTML inside
  `foreignObject`, so that names wrap, links are real links and Persian is set by the page's own CSS. Expanding
  and collapsing are links that change the `open` part of the address.
* **Limits.** With several relations of different direction between the same two entities the arrowheads say
  that both directions occur, not which label has which; the list below the graph does. Rows keep the order of
  the collection; nothing minimises crossings because a tree has none.
* **The family** – this is the layout of M0; since step 8.5 the family is a tree laid out by `FamilyTreeLayout`
  and drawn in a stage, see the addendum of [ADR 0008](0008-charts.md). In M0 it was laid out by `FamilyLayout`:
  a row per generation, at most three boxes beside each other on a
  line, a line with an arrow from each parent to the child and a dashed line between partners. The order inside a
  generation is the order of the collection; nothing is rearranged to shorten lines, so they can cross in a large
  family.
* **The connections after M0** – since step 9.3 the neighbourhood is a chart in a stage, with the focus in the
  middle instead of at the top; see the third addendum of [ADR 0008](0008-charts.md). The markup described here –
  lines of an SVG, boxes of HTML inside it, links that change `open` – is the same; the measures are in em.
