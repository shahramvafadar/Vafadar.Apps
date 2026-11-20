# 0020. The graph: a contract of Dastan on Syncfusion EJ2 Diagram

* Status: Accepted (D-079); takes the place of [0005](0005-graph-renderer.md) and of the family tree of [0008](0008-charts.md)
* Date: 2026-11-19
* Step: 19.8 of the [delivery plan](../delivery-plan.md); the trial of step 19.2 is `eng/spikes/diagram-persian`

## Context

A family tree and the connections of a node were drawn by the server as a still picture; opening a branch
asked for another page. The owner chose Syncfusion EJ2 Diagram (the control for the browser, in TypeScript)
for the graph: it brings layout, zoom and pan, selection, virtualisation and export, and the licence is
there. Its own documentation says right-to-left is not supported, so before anything was built on it we
looked ourselves, in Chromium, Firefox and WebKit, at 360 and 1280 pixels (step 19.2).

## What the trial showed

* **Persian is shown right, with five things the product does itself.** With them, cards with a picture, a
  wrapped Persian name, mixed Persian and Latin text, Persian digits and punctuation are right in all three
  engines; a family with a child of two parents, a parent with children by two partners and a half-brother
  is laid out in rows without overlap (`ComplexHierarchicalTree`; `HierarchicalTree` does not serve); zoom
  and pan work at both widths.
* **What the control does not do, and what we do instead**:
  1. *Direction.* The control has no direction for text. In a right-to-left page it draws every line outside
     its card; kept left-to-right it puts brackets and full stops at the wrong end. So the diagram itself is
     kept left-to-right, and each line of Persian is given its direction with the marks of Unicode (and one
     more mark that WebKit needs). The picture stands at the right because it is the last child of the card.
  2. *Breaking lines.* Its own wrapping breaks inside words and tears joined letters. Lines are broken at
     words, measured once by the control and handed back as lines.
  3. *Two lines between the same two nodes* lie on top of each other, and one label hides the other. Each
     relationship gets its own port on the card.
  4. *Export.* Its SVG and PNG hold the pictures and the Persian text in the right order, but in a fallback
     font, and the PNG is soft. The font is put into the SVG and the PNG is drawn from that at twice the
     size; then both look like the screen.
  5. *A margin or a padding that names one side only* makes sizes "not a number" and the control fail far
     from the cause. Every side is always named.
* **The keyboard is not usable as it comes**: Tab walks nodes and lines in the order they were made, not as
  they stand, and never leaves the diagram; arrow keys move the selected node instead of the selection.
* **Weight**: about 2.5 MB of script, 625 kB compressed, for the diagram alone.
* **Not looked at in the trial**: touch (pinch, one finger), a screen reader, an exported file opened outside
  a browser, and thousands of nodes.

## Decision

* **`DastanGraph` is ours; the control is behind it.** The contract speaks of nodes with the card of their
  profile ([0017](0017-node-profile.md)), of edges with their own identity, label, direction and source
  ([0018](0018-relationships.md)), and of focus, expand, collapse, select, open the profile, fit, and export.
  No type of the control appears in the domain, in what is stored or in what a page sends. The five
  workarounds live in one place behind the contract, each with a test in a real browser, so that a new
  version of the control is tried against them before it is taken.
* **Cards are made of the control's ordinary elements** – a picture and texts – never of HTML or of its
  "native" shapes, because only ordinary elements are laid out, virtualised and exported by the control. The
  full profile opens in a panel of the page, outside the diagram.
* **The rules of a family tree are Dastan's**: several partners, parents by birth, by adoption and as
  guardians, an ancestor who appears twice, branches that do not meet. One person stays one person however
  often drawn; a point where a couple's lines meet is a drawing, not a person.
* **The keyboard and the screen reader are ours**: the diagram is one stop of Tab, arrows move from a card
  to the one that stands next to it in that direction, Enter opens the profile, Escape leaves; and the list
  that stands beside every drawing stays the way for a reader who does not see it.
* **Loaded only where it is used**: the script of the diagram is asked for by the pages that have a graph,
  after the page is shown.
* **If the control fails the measures of step 19.8** – the budgets for 1,000 and 10,000 nodes on the named
  devices, touch, or the keyboard as described – the contract stays and what is behind it is replaced. That
  is what the contract is for; it is decided with numbers, by the owner.

## Consequences

The licence key of Syncfusion is in the script and readable by everybody; it comes from the secrets of the
build. The still drawing of the server stays as the first answer of a page. The trial stays in the
repository as the record of what was seen and as the place where a new version of the control is tried.

## What is built (step 19.8, 2026-11-20)

* **The contract and its addresses.** `GraphData` – nodes with the words of their card and the address of
  their profile, edges with identity, role, words, direction, time, source and whether they are disputed –
  is answered for the family and for the connections of an entity, for a tree of an account and for a public
  tree (`…/family.json`, `…/connections.json`, `…/graph.json`), by the code of the page, asked with what the
  page is asked with, kept by nothing. A test holds every answer against the page of the same visitor.
* **Where a card stands is decided by Dastan.** The answer of a family says the row and the place of every
  node, from the layout that draws the still picture (`FamilyTreeLayout`, `TreeShapeLayout`), where the
  rules of a family live. The control lays nothing out: it draws, moves, zooms and writes pictures. So the
  layout modules of the control are not in the script, and a family looks the same still and moving.
  Connections have no rows; they stand on one round around the node the page is about, which grows
  with their number (with two rounds a line went through a card of the inner one).
* **The control is one file of TypeScript** (`Scripts/src/graph.ts`), made into `graph.js`, which only a
  page with a graph asks for, after it is shown. A page holds `<dastan-graph>` around its still drawing
  (`GraphView.razor`); the script takes the place of the still drawing once it has drawn, and gives it back
  when the reader asks, when the address does not answer and when anything fails.
* **The five workarounds** are in that file, each named where it is done: the diagram is kept left to right
  and each Persian line is wrapped in marks of its direction; lines are broken at spaces by the control and
  handed back as lines; each end of a relation has a point of its own on the side of the card that looks at
  the other card, and a label stands at its own part of its line; the saved SVG carries the font and the
  pictures, and the PNG is drawn from it at twice the size; every padding names four sides.
* **The keyboard**: the drawing is one stop of Tab; the arrow keys go to the nearest card in their direction;
  Enter opens the profile in the panel of the page (ADR 0017) and Escape returns to the drawing; + − 0 make
  it larger, smaller and whole; Tab and Shift+Tab leave. The control gets no key. What is chosen is said in a
  live region, with its relations; the keys are written below the drawing; the list of the page stays.
* **Opening and closing** a generation or a side is what it was: the steps at the sides of the stage, which
  ask for the page again. The drawing is then drawn again from the new answer.

**Found while building, in a real browser:**

* **The control counts its use and sends the count to a host of its maker** (Application Insights) when a
  page runs at an address of development. It is switched off in the script before the control is made
  (`Telemetry.disable()`), and the rules of the site refuse a connection to any other host in any case. A
  browser test listens to everything a page with a graph asks for and fails on an address of another host.
* **The content security policy has one more rule, for pictures alone:** `img-src 'self' blob: data:`. The
  control uses an empty picture written into its script, and a PNG is drawn from a picture held in memory.
  Scripts, styles, fonts and connections stay with the site alone; the control needs nothing else – it sets
  its styles through the object model, which the policy allows.
* **Without a key of its licence the control puts a notice over the page.** The key is given to the site as
  `Dastan__Syncfusion__Key` (operations) and is readable in the page, as said above; the owner enters it.
  The tests run without a key and leave the notice – and nothing else – out of the check of accessibility.
* **The control makes a drawing smaller to fit, never larger,** so "the whole drawing" first shows it at its
  own size; and the words of a relation are kept on one line, because the control breaks a label at every
  space and the marks of the direction must stand around a whole line.
* **In Firefox, where letters are a little larger,** the labels of two relations between the same two cards
  touched; the points of the two lines were moved further apart.

**Measured** (made-up families handed to the drawing in the place of the answer; the browsers of the test
container on the machine of development, a window of 1280 pixels; budgets written into the test before the
first run: drawn within 2 s, 6 s and 30 s, a step with the keyboard within 0.3 s, 0.5 s and 1 s):

| People | Answer | Chromium: drawn, a step | Firefox | WebKit | Texts in the page |
|---|---|---|---|---|---|
| 100 | 25 kB | 0.10 s, 20 ms | 0.13 s, 21 ms | 0.33 s, 172 ms | 300 (all) |
| 1,000 | 277 kB | 0.25 s, 12 ms | 0.54 s, 29 ms | 0.50 s, 246 ms | 75 (what is in sight) |
| 10,000 | 2.9 MB | 2.7 s, 40 ms | 4.1 s, 19 ms | 3.4 s, 131 ms | 75 |

Above 150 cards only what is in sight is in the page. All are far inside the budgets, so the control stays.
The budgets are judged on the machine of development only. On the shared runner of CI, where two tests run
beside each other, the first whole run (2026-11-20) took 593 ms for a step among 100 people in WebKit, twice
the budget, while Chromium and Firefox stayed inside; that measures the runner, so CI writes its times into
the uploaded results (`graph-measures.tsv`) and does not judge them (`DASTAN_BUDGETS=record`). What does not
depend on the machine – every card is there, a step moves the choice, only what is in sight is in the page –
is asserted there too.
What the numbers do not say: the answer of the site for so many nodes (the collection has none so large, and
a page asks for a neighbourhood, not for everything); a phone; a slow connection, on which the 2.5 MB of the
script (625 kB compressed) are felt on the first page with a graph and kept by the browser afterwards.

**Not looked at:** a real phone and touch with two fingers; a screen reader; a saved file opened in another
program; the page with a key of the licence; a family and a tree of an account by eye (their tests pass in
three browsers); the image of the container built with the second script. Looked at by eye: the connections
of a name, in Persian, dark, 1100 pixels wide, in Chromium – cards, a card with a picture, labels and tools.
**Not built:** what is chosen is not yet carried on to the timeline and the gallery (R-EXP-05) – that comes
with the timeline, step 19.9.
