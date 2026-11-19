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
