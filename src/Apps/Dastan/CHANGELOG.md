# Changelog – Dastan

Changes a visitor or the owner can notice, newest first. Technical decisions are in the delivery plan and the ADRs.

## Unreleased

- The product specification, the editorial sample and the original media are in the repository
  (`docs/product`, `content`). The delivery plan to the first milestone is accepted. No application yet.
- The sample has a Persian text: names, summaries, relation labels, event descriptions and captions
  (`content/samples/shahnameh-pilot.fa.json`). It is a draft and is marked as not reviewed until the owner has
  checked it.
- The website exists and runs locally: a start page, the entry of the story and a page per entity, in English and
  Persian (right to left), light and dark, without any script. The reading position is chosen on the page of the
  story; what belongs to a later part is not shown and cannot be opened by a link. Media, the story sequence, the
  graph and search are not built yet.
- Every entity has a page of its connections: a drawing with the entity at the top and one row per neighbour, with
  the relation that leads to it and an arrow for its direction. A row can be opened to see its own connections and
  closed again; choosing a name puts it at the top. The same connections stand as a list below the drawing.
