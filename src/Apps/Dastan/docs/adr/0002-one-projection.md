# 0002. One projection decides what a visitor sees

- Status: Proposed (implemented and tested in `Vafadar.Dastan.Core`; becomes Accepted when the pages of phases 4 and 5
  are built on it and the journey tests confirm it in the browser)
- Date: 2026-11-16
- Deciders: Shahram Vafadar
- Governing documents: 05 (guided stories and spoiler control), 16 (section 7, gate G-04), 17 (section 7),
  18 (FLOW-06, SCREEN-01 to 08, TASK-06), 20 (section 6); acceptance conditions M0-03, M0-04, M0-06, M0-07, M0-12

## Context

The sample holds the whole story, including its ending, and media nobody has cleared. A visitor has read up to a
stage of the story and must not learn anything of a later stage – not from a name, a count, an edge, a suggestion, a
thumbnail, a source title, an address or the Back button. The specification asks for the filter to run before
anything is rendered or counted.

If every view filtered for itself, each new view would be a new chance to forget one field. The catalogue
([ADR 0001](0001-domain-read-model.md)) already guarantees that no record refers to a record of a later reading
level. That guarantee makes a simpler rule possible.

## Decision

1. **`Projection` is the only way to a view.** `Projection.Of(catalogue, boundary, audience)` selects the records one
   visitor may see. The entry of the collection, an entity, its connections, the family, the story, the lanes, the
   galleries, a media item, the evidence of a statement and the search are methods of the projection and are computed
   from the selected records only. No page, component or route reads the catalogue.

2. **The order of the filters is fixed:** publication for the audience, then the eligibility of media, then the
   reading boundary. Everything derived – order, numbering, neighbourhoods, generations, hits – comes after them.
   * *Publication.* A collection that is not released to an audience has no projection for it (`null`). The sample
     may be demonstrated locally and is not approved for the public, so the public audience gets nothing.
   * *Media.* An item is part of a projection only when its eligibility allows showing it. The model knows the
     eligibility for a demonstration only; until it knows one for a public release, the public sees no media. An
     item that is not eligible is absent – with its identifier, its caption, its catalogue page and the sources only
     it cites. It is not listed as a reference: the editors marked these items as excluded.
   * *Reading boundary.* A record is visible when its level is at or below the visitor's boundary. A boundary beyond
     the last stage is the last stage.

3. **Hidden and unknown are the same answer.** Asking for an entity, a relation, an event or a media item that is
   hidden returns nothing, exactly like asking for one that does not exist. A page therefore cannot say "this exists
   but you may not see it yet", and a deep link into later content shows the same state as a mistyped link.

4. **A visitor learns that the story continues and nothing about how.** `StoryContinues` is derived from the list of
   stages, not from hidden records. The steps of the story are numbered among the visible events; the ordinal of an
   event in the full story is not part of any view, because a gap would tell that something is hidden in between.
   The labels of the stages themselves are written for readers and are all shown in the reading control.

5. **The graph and the list are one answer.** `Connections` returns nodes and edges once; the graph and the list
   next to it render the same edges. Expanding a node adds its relations; a node that is not connected to what is
   open is ignored, so an address cannot open an unrelated part. Whether a node can be expanded is a yes or no, never
   a number of hidden neighbours.

6. **The story and the lanes share their steps.** A cell of a lane is the association the event states, with its
   presence. Somebody who is only mentioned is in the lane as mentioned.

7. **A family view uses what the relation types say.** A relation type states whether it means parentage or
   partnership (`Kinship`); the view never guesses from a name or a label. Generations follow from the parentage
   relations; relations that contradict each other do not stop the view.

8. **Evidence belongs to one statement.** The evidence view returns the sources of the entity, relation, event,
   association or media item that was asked for, with its locator and review state. It carries a flag that a source
   page outside the app can tell more of the story than the visitor has read; it is set whenever a later stage
   exists.

9. **Search finds names.** It searches the names of the visible entities in every language with
   `SearchText.Fold`, ranks an exact name before a name that begins with the text, before a word that begins with
   it, before a name that contains it, and keeps the order of the collection otherwise. Matching and ranking stay in
   the app until a second app needs them (LIB-TEXT-07).

10. **The reading boundary and the language are the visitor's, not the link's.** A projection is built per request
    from the visitor's own boundary. An address carries a selection, never a boundary (decision 5 of the delivery plan, D-033; built in
    phase 4).

## How it is verified

* `Disclosure` (test helper) asks a projection every question about every record of the collection, visible or
  not, collects every text reachable from the answers by reflection and compares it with the texts that belong only
  to hidden records – identifiers, names in both languages, summaries, roles, file names, addresses, locators. It
  does not know the views, so a view added later is covered when it is asked there. It runs for every stage of a
  small made-up story and of the pilot sample with its Persian text.
* A text is compared as a whole. What the editors wrote into a visible text is theirs: the title of the arc names
  Sohrab at the opening, and the projection does not hide words inside a text.
* The tests of the sample state what each stage holds, that graph and list agree for every entity at every stage
  (M0-03), that story and lanes agree (M0-04), that lowering the boundary removes the later stages again (M0-06) and
  that every statement has its own evidence (M0-07).

## Consequences

* A new view is a new method of `Projection` and a new line in `Disclosure`.
* The web project needs no rule of its own about spoilers. It needs one rule about files: a media file is served
  only for an item the projection of the requesting visitor contains (phase 5).
* Building a projection is cheap and its input never changes; the web project may keep one per stage and audience.
* Events that are dated instead of told (a company's history) are not steps of a story. A timeline for them is a
  later view and not part of M0.
* The eligibility of media for a public release is not modelled yet. It has to be before R0.


## Addendum of 2026-11-16 – where a visitor came from

Pages that can be reached from several places – the evidence of a statement, the page before a source, the page of
a media item – return to where they were opened. The address says where that was (`from`, `entity`, `event`). It
is a claim of the visitor, not a permission: a page follows it only when it names a page of the collection, and a
gallery only when the projection has that gallery and it holds the item. A page that says where it stands writes
the address it has checked itself (`EvidenceAddress`), never what its own address carried.

## Addendum of 2026-11-16 – what has no reading stages

A comparison ([ADR 0008](0008-charts.md)) is not part of a story: it has no stages a visitor reads through, so
there is nothing a projection could bound. The rule of this decision still holds for it in its simpler form: what
may be shown is decided once, on the server, before anything is laid out or linked – a comparison that is not
released to the audience of the site is not kept by it (`DastanSite.Comparisons`), and no page gets it another way.
The day a comparison draws on the records of a collection, it gets them from that collection's projection.
