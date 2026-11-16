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
- An entity with parents, children or a partner has a family drawing: generations as rows, an arrow from each
  parent to the child, a dashed line between partners – only as far as the story has been read.
- The story can be read step by step, with everybody and everything that is part of a step and how: taking part,
  only mentioned, the place, an object. Below the steps a table shows every name against every step.
- Images and videos: a gallery of the collection, of an entity and of a step of the story, and a page for each
  image or video with what kind of picture it is, when and by whom it was made, the rights, and links to whom and
  what it shows. The video has captions and its transcript on the page and starts only when asked. The two original
  items of the sample appear from the third reading stage on; before that the gallery is empty and their files
  cannot be opened by an address either.
- Persian text is set in the font Vazirmatn on every device. The font comes with the site; no page asks another
  site for anything.
