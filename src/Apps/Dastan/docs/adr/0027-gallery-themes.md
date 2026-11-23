# 0027. A theme of a gallery is a record of its collection

* Status: Accepted
* Date: 2026-11-23
* Decision of the owner: D-083

## Context

R-MED-02 asks for galleries that are reached from entities, periods, places, events, stories – and themes. A
gallery of an entity, of a step, of the whole collection and of a time exist ([0021](0021-timeline.md)). A
gallery of a theme did not: an item said whom and what it shows and when, and nothing about a motif, a genre or
a school of painting. ADR 0021 left it open with the words "themes are kinds of the vocabulary, not free words",
and the delivery plan named it as a decision of the owner.

Three ways were laid out: a fixed list of themes in the code, themes as records of a collection, and nothing for
now. The owner chose the second: the collections that are planned – a story, a country, a club, a university –
have nothing in common in their themes, so a fixed list is either too general to be of use or changes with every
collection.

## Decision

1. **A theme is an entity of the type `Theme`** (`EntityTypes.Theme`, a subject of knowledge). It is a record of
   its collection like every entity: an identifier, a name with its translations, a summary, a reading stage,
   other names, a page, a place in the search. Nothing new was built for any of these.
2. **A media item names its themes** in `themeIds` (`MediaItem.Themes`), in the order the editors want them. A
   theme is a grouping, not something an item depicts: `depictedEntityIds` may not name a theme, `themeIds` may
   name nothing else, and no theme is named twice. A theme of a later reading stage than the item is refused –
   its name would be given away.
3. **A theme may rest on no source.** Every other entity names at least one. A theme is the editors' own
   grouping; it names a source where it is a term of scholarship. The page of a theme without a source says
   what a theme is instead of leading to an empty list of sources.
4. **The gallery of a theme is the gallery of that entity** (`/<collection>/entity/<theme>/gallery`,
   `GalleryRule.OfTheme`): what the editors gathered under it and a visitor may see, with what is held elsewhere
   named as a reference, in an order and of a kind like every gallery. It says "gathered under the theme", not
   "shows".
5. **A theme is offered only with something under it** for this visitor (`Projection.Themes`): the gallery of
   the collection and the gallery of a theme name the themes with the number of their items, above the
   pictures. The page of an item names its themes; each leads to everything else under it.
6. **The tool of the editors** offers the themes of the collection in the form of every image and video
   (`themeIds`, to tick) once the collection has a theme, and offers what an item shows without the themes. A
   theme is added like every entity, with the type `Theme`.

## Consequences

* A collection decides its themes without a change of the code; two collections do not share themes, as they
  share nothing else.
* A theme can take part in relations and in threads like every entity. Nothing asks for it and nothing forbids it.
* The reading boundary of a theme is that of an entity. A theme whose name gives something away belongs to the
  stage from which it may be told, and its items to that stage or a later one.
* Not built: themes across collections (a theme "battle" of the whole site), and a gallery of a theme within a
  time. Neither is asked for by a requirement.

## Tests

`MediaThemesTests` (the reader, the rules of a collection, what a reader is offered at each stage, nothing of a
later stage disclosed), `GalleryThemesTests` (the pages in English and Persian), `StudioThemesTests` (the form).

## Addendum of 2026-11-23: a theme across collections (step 25.4, D-088)

The decision left a theme a record of one collection, and named "a theme across collections" as what nobody
had asked for. The owner asked for it.

* **Who says that two themes are one.** The editors, in the one way the collections have for it
  (ADR 0023, DOM-003): both themes carry the same identifier of the same outside catalogue, marked `exact` –
  the Art & Architecture Thesaurus, Iconclass, a list the editors keep. The issuer is compared without regard
  to case, the value as it is written. A name joins nothing, a `close` identifier joins nothing, and what
  stands under two themes joins nothing.
* **A collection has a theme once.** Two themes of one collection that carry the same exact identifier are a
  problem of the collection.
* **What is joined does not depend on any order.** Themes that share an identifier are of one set, and so are
  themes that each share one with a third; the set is the same whatever the order of the collections, of the
  themes and of the identifiers of a theme. A set is one theme of the site – unless it holds two themes of one
  collection: then one collection tells two themes apart that another calls one, the statements contradict
  each other, and **nothing of that set is joined** until the editors say which belongs to which. (An earlier
  form joined such a theme to "the first" of the two; which one that was depended on the order of the files.)
* **What a reader is told.** Each collection is asked through its own projection, at the stage this reader has
  read it to. A theme of a later stage of one collection is not named, not counted and not joined because
  another collection has it too; asked for by its address it is a page that does not exist.
* **The pages.** `/themes` lists every theme with something under it, a shared one once, with its collections.
  `/themes/<collection>/<theme>` is the page of that theme: every collection that has it under its own name,
  with the name that collection gives the theme, its pictures and its references to works, each leading into
  its own collection. A shared theme has one page under the address of each of its collections. The gallery
  of a theme in a collection says where else the theme is.
* **The search of the site** finds a shared theme once and leads to that page.
* **The tool of the editors** shows, on the record of a theme, the themes of other collections that carry one
  of its identifiers as exact.

Not built, and not asked for: themes above themes; a theme of the site that belongs to no collection; a page
that orders the pictures of several collections into one list – each collection keeps its own order.
