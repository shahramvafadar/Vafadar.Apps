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
- Every statement can be asked for what supports it: the page of an entity, each relation, each step of the story,
  each part somebody has in a step and each image or video lead to their own sources, with the state of the
  editors' review, and back to where the visitor was. A source of another site is opened from a page that says
  first that it tells the whole story.
- Names can be searched inside the story, in English and Persian, as far as the story has been read.
- A page that fails says so and offers the same address again; an address that leads nowhere offers the start, the
  story and the reading position; a family drawing without members points to the drawing of all connections.
- Dastan can be installed from the browser: it has a name, icons and a start of its own. Without a connection a
  page says so, in English and Persian, and offers the same address again; nothing of a story is kept in the
  browser. A form that is sent from a page that was open too long leads to a page that explains it.
- A local demonstration asks search engines not to index it.
- Comparisons: several subjects side by side over one axis of years. The first one shows what ruled in Iran, in
  China and in Rome between 600 BCE and 700 CE as three rows of bars, with single events marked above them. On a
  wide screen the time runs across the page; on a phone it runs down the page. The chart can be opened further and
  drawn closer together and turned – every such choice is a link, so a chart can be sent as it is seen. Below it the same periods and events stand as a table, with the
  state of the editors' review. This first comparison is a **draft**: its years are not checked against sources
  yet, and it is shown in the local demonstration only.
- A chart has the whole page: it is as wide as the window and as high as the window, and scrolls inside. At each of
  its four sides stand a "+" and a "−": the "+" opens the chart a step further in that direction, the "−" closes it
  again. For the comparison these are the earlier years, the later years, the single events and the subjects; for a
  family the ancestors, the descendants and the relatives beside the line.
- The family of somebody is a tree now. It opens with two generations above and below and can be opened further a
  generation at a time, to brothers, sisters, uncles and aunts and their children, or to the whole family; its
  generations run down the page or across it. Partners stand beside each other, children below their parents, and
  the lines show whose child somebody is.
- The story is a chart as well: every name has a row and every step of the telling a column, and a mark shows what
  part a name has in a step. It can be closed from the first and from the last step, to the numbers of the steps
  and to fewer names, and opened again. After a "+" or a "−" the chart stays on the screen – for the comparison and
  the family tree too.
