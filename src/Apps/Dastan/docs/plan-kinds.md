# Plan – kinds: a classification under every type, with an icon and a filter

Asked by the owner on 2026-11-28: every record the site shows should have a *kind* – a battle, a natural disaster
(a drought, a frost, an earthquake), a political, religious, literary or scientific person, a figure of myth – each
kind with an icon of its own, the kinds editable over time, and the pages that show many records able to filter by
kind. This plan records the design and the decisions; it is carried out as one batch with its ADR.

**State (2026-11-28).** The owner confirmed K1–K4 as recommended (D-103, [ADR 0029](adr/0029-kinds.md)).
Steps 1 to 3 are done: the format, the reader and the check, the vocabulary, the list `library/kinds.json` with its
Persian, the drawings, the choice of kinds on the list of names, the timeline, the comparisons and the search, and
the classification of the whole library – 1,717 distinct entities of the collections and 796 periods and events of
the comparisons – with the rule of K4 on. The list grew from 45 to 96 kinds while classifying (below). Step 4 is done:
the look in a browser on the owner's word (Persian, wide and 360 pixels) found the boxes of kinds in the search
of a collection stretched to the width of a line, one under the other; they now stand in columns with their
icon and name beside them. The choices on the list of names, the timeline and a comparison, wider kinds taking
in narrower ones, and the addresses with the choice in them work as planned.

## What exists

* **Types** – the fixed vocabulary of `EntityType` (Vafadar.Dastan.Core/Vocabulary.cs): `person`, `event`,
  `dynasty`, `character`, `place` and so on, each with a nature. The type decides the profile of a record (which
  facts it may have, which relations it may take) and is part of the format; it is not meant to grow every month.
* **Grouping by type** – the timeline of a collection already draws its chart by type ("by kind" in the page's
  words), and lists can be narrowed by type.

## Design

1. **A list of kinds in the library** – `library/kinds.json` (and `kinds.fa.json` for its Persian, as collections
   have), outside the code so editors can add and rename kinds without a release: each kind has a key, a label, a
   short meaning, the types it may be given to (`event`, `person`, `character`, …), an icon key, and optionally a
   parent kind (`battle` under `war`, `drought` under `naturalEvent`). Kinds are never deleted once used: a kind
   no longer wanted is marked `retired`, so old records keep their meaning.
2. **A record names its kinds** – an entity gets `kinds: ["battle"]`, one or more keys of that list. The check
   refuses an unknown key or a kind not allowed for the record's type. A record without a kind stays valid while the
   collections are filled; a report of the content tool lists the records still without one, so that "every record
   has a kind" can be reached and then made a rule.
3. **Icons** – each kind names an icon of the site's icon set (`Icon` component); a missing icon falls back to the
   icon of its type. Icons appear beside names in lists, cards and the legend of charts.
4. **Filters** – the pages that show many records – the list of a collection, its timeline, the comparisons, the
   search results – get a filter by kind (several may be chosen; a parent kind includes its children). The filter
   is part of the address, like the existing filters, so a filtered view can be shared.
5. **First kinds** (to be refined while classifying):
   * events: war, battle, siege, revolt, conquest, treaty, founding, natural event (drought, flood, earthquake,
     frost, famine, epidemic), religious event, building, discovery, death, accession;
   * persons: political (ruler, statesman, general), religious, literary (poet, historian), scientific,
     artistic, philosopher;
   * characters: hero, king, demon, divine being, creature;
   * places and regions: city, capital, battlefield, sanctuary, river, mountain;
   * works: poem, history, inscription, monument.

   Added while classifying, so that every type the library uses has kinds of its own: events – law and reform,
   journey and embassy, fall of a state, writing of a work, ceremony, disaster; periods – rule of a house, age of
   history, ancient culture; persons and characters – royal house, rebel, and for characters sage, sorcerer,
   noble, of the people, steed; stories – story of a reign, war story, tale of a hero, love story, tragedy, tale of
   the court; works and things – teaching, treatise, edition, manuscript, document, weapon, keepsake, wondrous
   thing, banner; places – fortress, camp, prison, sea or lake, open land, land, state; offices – throne, office
   of state, office of a faith; houses – ruling house, house of legend, house of heroes; bodies – council,
   research, university, library, museum; ideas – idea, topic.
6. **Classifying the library** – every existing record of the library is given its kinds, by a script proposing a
   kind from the type, the relations (a holder of a royal office → ruler; an author of a work → literary) and the
   label (a "battle of …"), and an editor's reading of what the script could not decide. Nothing is guessed: a
   record the script cannot classify is left for reading.

## Decisions for the owner (recommendation first)

| # | Question | Recommendation |
|---|---|---|
| K1 | Where the list of kinds lives | `library/kinds.json`, edited like a collection (other option: in the code, changed with a release) |
| K2 | One kind or several per record | several (a poet who was also a vizier is literary and political) |
| K3 | Kinds of figures of myth | a kind of `character` (hero, demon, divine being), not of `person`: a figure of myth stays a character of a story, as the chronology guidelines say |
| K4 | When "every record has a kind" becomes a rule of the check | after the library is classified, in the same batch |

## Work

1. ADR, format (`kinds.json`, `kinds` on an entity), reader and check, vocabulary output.
2. Icons and the filter on the list of a collection, the timeline, the comparisons and the search.
3. The kinds list and the classification of the whole library, its Persian, and the rule of K4.
4. Tests, the look in a browser on the owner's word, the changelog.
