# 0025. One state of exploring, read from the address and known to no renderer

* Status: Accepted
* Date: 2026-11-21
* Step: 20.6 of the [delivery plan](../delivery-plan.md)

## Context

A name of a collection has several views: its page, its connections, its family, a chart of what is dated
about it, its pictures. Each was built with a state of its own in its address ([0008](0008-charts.md),
[0020](0020-graph.md), [0021](0021-timeline.md)): which rows of the drawing are open, how many generations
of a family, which stretch of years, which order of pictures. The views did not know of each other. Going
from the drawing of connections to the pictures and back opened the drawing as new; what was chosen in a
drawing that moves lived in the script of that drawing and nowhere else; a link from one view to another
was written by hand on each page.

The sketches of the owner show one explorer with a switch between Connections, Timeline and Pictures and
an inspector for what is chosen. That needs one state behind all views, and the views are drawn by
different things: pages of the server without script, a drawing of Syncfusion, the timeline of Dastan's own.

## Decision

**One contract in the core** (`Exploration`): what is explored, which view is shown, what is chosen, what
each view was told, and how far the reader has read. It holds names of entities and numbers. Nothing in it
is a pixel, a zoom, a place on a canvas or a word of a renderer; the still page, the drawing that moves and
a saved picture are all made from it and the projection of the visitor.

**The address is the state.** The state is read from the query of an address and written to one.

* Every view keeps the names its page always had (`open`; `up`, `down`, `beside`, `family`, `view`;
  `view`, `scale`, `from`, `to`, `events`, `lanes`; `order`, `kind`). An address that was passed on before
  this step means what it meant.
* While another view is shown, what a view was told is **carried** in the address under a name no other
  view uses. Two views call their manner `view`: carried, the direction of a family tree is `tree` and the
  direction of a chart of time is `dir`. Coming back finds the view as it was left.
* What is chosen is `sel`, on every view.
* The page of a view writes its own state as before and passes every link it makes through `Carry`, so a
  step inside a view – one generation more, another order – loses nothing of the others.

**What is in an address, and what is not.** In it: what is public and stable – identifiers of published
entities, small numbers, a few words of a fixed list. Not in it: how far the reader has read (it is part
of the state, and comes from the visitor of the request); where a drawing stands and how large it is shown
(a movement, not a statement); anything of an account. **An address grants nothing:** every name in it is
asked of the projection of the visitor. A value that is not a choice is not carried; an identifier the
visitor may not see is as good as one nobody has – the page of both is the same. The drawing of connections
carries as open only what the projection says is open.

**Choosing is not centring.** Choosing (`sel`) looks at something closer without leaving: the page stays
the page of the first name, and a block names what is chosen with three acts – put it in the middle, its
whole profile, choose nothing. Putting something in the middle is an act of its own: another name is
explored, how far the drawings of the earlier one were opened is not said of the new one, nothing is chosen
in it, and the manner of looking and what was left out stay with the visitor.

**Nothing is dropped in silence.** A visitor can leave things out of a view: a stretch of years, the single
events and some lanes of a chart of time, a kind of picture. A view that does not leave such a thing out
shows more than was asked – and says so ("Not left out in this view: the years you chose"). The wish stays
in the address for the view it belongs to. How far a drawing is opened and in which order pictures stand
leave nothing out and are carried without a word.

**The bar.** The page of every view of a name shows the same bar (`ExploreBar`): the name, the views this
visitor has of it – a view the name does not have is not offered –, the one that is shown marked, what is
chosen, and what was set aside. A view is a page and choosing is a link: all of it works without script.

**Drawings that move** keep the address in step (`explore.ts`): choosing a card writes `sel` into the
address of the page and into the links of the views, and letting go takes it out; the timeline does the
same for its stretch of years. Only where the bar stands: a page that explores no single public name – a
comparison, a tree of an account – gets nothing written into its address.

**The comparison** (step 20.8). A visitor can lay the times of other names beside the times of the name that
is explored: `with`, on every view, at most five names, each once, never the name in the middle. "Add to the
comparison" is offered for what is chosen – on the page and in the inspector of a drawing – when it has
something with a time; the bar names what is compared, each with the way to take it out, and leads to the
chart of time, which then has a lane for each. What is compared is named, and drawn, only as far as the
visitor may see it. Another name in the middle keeps the comparison, without itself. This is the comparison
of a visitor, in an address; the comparisons the editors write (ADR 0008) are another thing and stay.

## Consequences

* A view is added by a line in the table of the contract and a page that passes its links through `Carry`.
* The explorer of step 20.8 and the profile of step 20.9 are new clothes for this state, not new state.
* Addresses grow: a state with several views told something is a longer query. It stays far below what a
  browser or a server refuses, and rows that are open are counted (40).
* The page of a view no longer carries a link "back to the name" of its own: the bar leads there.

## Not decided here, and not built in this step

* **A stretch of years that the connections and the pictures of a name honour.** Today only the chart of
  time leaves years out; the two others name the wish as set aside. Whether they should leave years out too
  belongs to the explorer (20.8).
* **Where a drawing stood** – its movement and its size. *Built on 2026-11-21 (section 23 of the delivery
  plan):* the moving drawing of connections, of a family and of a family tree writes how large it is shown and
  how far it was moved into the place of the history the visitor is at (`history.state`), a moment after the
  visitor – not the page – moved it; Back, Forward and loading the page again put it back, and "the whole
  drawing" forgets it. It is not part of the address: an address is passed on and says what is explored, not
  where somebody's eye was. It is believed only for a drawing of as many cards. The timeline kept its stretch
  in the address already.
* **The inspector.** What is chosen is a block of the page here; the panel beside the drawing and the sheet
  of a phone are 20.8.
* **A context wider than one name** – exploring a story or a thread as such. The contract explores one
  entity of one collection; the chart and the pictures of a whole collection have no bar.
