# 0005. The renderer of the connections graph

- Status: **Proposed – needs the owner's decision.** It recommends a third candidate next to the two the delivery
  plan named (decision 7). Nothing is built on it yet; the graph is part of phase 5.
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
