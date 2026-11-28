# 0029. A kind of record is a key of the library's list of kinds, shown with its drawing and chosen in the address

* Status: Accepted
* Date: 2026-11-28
* Request of the owner: 2026-11-28 – every record a kind (a battle, a natural disaster, a political, religious,
  literary or scientific person, a figure of myth), each kind with an icon of its own, the kinds editable over
  time, and the pages that show many records able to filter by kind. The design is
  [the plan of kinds](../plan-kinds.md).
* Decisions K1–K4 of the plan were taken on its recommendations as working defaults; the owner may still change
  them.

## Context

A record has a type (`EntityType`, `Vocabulary.cs`): `person`, `event`, `character`, `place`. The type decides
the profile of the record – the facts it may have, the relations it may take – and is part of the format; it
changes with a release and is not meant to grow every month. What a visitor asks for is finer: the battles of a
war, the poets of a time, the droughts and frosts, the heroes and demons of a story. A finer type would multiply
the profiles and the relations; a free tag would mean nothing fixed and could not be checked.

## Decision

1. **A list of kinds in the library (K1).** `kinds.json` lies in the content root, beside `samples`,
   `comparisons` and `media` (format `dastan-kinds-0.1`); `kinds.<language>.json` translates its labels and
   meanings (format `dastan-kinds-translation-0.1`, with `review` like every translation). Each kind has a key,
   a label, a meaning, the types it may be given to (keys of `EntityTypes`), the name of a drawing of the site
   (`icon`), and may name a wider kind (`parent`). A kind no longer wanted is marked `retired` and never
   deleted: a record that has it keeps it, and it stays valid. The reader (`KindsFile`) is strict like the
   readers of collections: an unknown field, an unknown type, a parent that is not in the list and a loop of
   parents are problems. A content root without the file has no kinds.
2. **A record names several kinds (K2).** An entity has the optional field `kinds`, a list of keys; a period or
   an event of a comparison has the same field and counts as a `period` or an `event`. The readers refuse a key
   the list does not have, a kind not given to the type of the record and a key named twice.
3. **Figures of myth are kinds of characters (K3).** `hero`, `king`, `demon`, `divineBeing` and `creature` are
   given to `character` (and the two creatures of a story to `animalCharacter`), never to `person`: a figure of
   myth stays a character of a story, as the [chronology guidelines](../content-chronology-guidelines.md) say.
4. **Every record a kind is a rule that is not on yet (K4).** The check counts the records without a kind
   (`without a kind: n of m`, for every collection and comparison) so that the library can be classified; the
   rule that refuses a record without a kind is written and tested, and is turned on by one line:
   `KindsFile.EveryRecordHasAKind` in `Vafadar.Dastan.Data/KindsFile.cs` (the default of
   `PilotSampleOptions.RequireKinds`). It is turned on when the library is classified.
5. **Shown with its drawing.** The page of a record and the card of a name (`IdentityHead`, `NodeCard`, the hits
   of the search) show the kinds beside the type, each with its drawing and its label (`KindNames`); the meaning
   is its hint. A kind that names a drawing the site does not have shows a drawing of the nature of its first
   type. The drawings are Dastan's own, added to `Icon.razor` like those of [ADR 0024](0024-parts.md): `war`,
   `flag`, `scroll`, `building`, `nature`, `river`, `faith`, `candle`, `crown`, `pen`, `learning`, `art`,
   `hero`, `demon`, `divine`, `creature`, `city`, `mountain`, `monument`.
6. **Chosen in the address.** The list of names of a collection, its timeline and a comparison offer a choice of
   kinds (`KindChoice`): a link for every kind that what the page shows has, and for every wider kind above one,
   each toggling the kind in the parameter `kinds` (keys with commas between them). A wider kind takes in the
   kinds below it (`KindFilter`). Every link of a chart keeps the choice. The search asks for kinds with boxes
   that can be ticked, in the same parameter, among the kinds the names of the collection have. A page that has
   no kind shows no choice; a key that is no kind of the page is not asked for. All of it works without a
   script. The timeline narrowed by kind leaves out the pictures: a picture is of no kind.

## Consequences

* Editors add and rename kinds without a release of the code; a new drawing still needs one.
* The list is one for a content root: a site that reads its collections from the database reads `kinds.json`
  from its content root, where its media lie; a released revision does not carry its kinds yet.
* The tool of the editors carries `kinds` through as it is written; its form offers no choice of kinds yet.
* The search of the whole site and the other views of many names (people, places, the graph) offer no choice
  of kinds yet; they show the kinds of a card where they use one.
* The library is not classified yet: no collection has a kind, and the pages show no choice until one has. The
  classification of the library (step 3 of the plan) and turning on K4 are the next work.
