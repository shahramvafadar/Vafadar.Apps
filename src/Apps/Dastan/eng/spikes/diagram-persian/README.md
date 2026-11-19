# Trial: Syncfusion EJ2 Diagram and Persian (step 19.2)

A technical trial, not part of the product. Dastan wants to draw its interactive graph – family trees and
relationship graphs with profile cards – with Syncfusion EJ2 Diagram (the TypeScript control,
`@syncfusion/ej2-diagrams` 35.2.12). Syncfusion's own accessibility table says that right to left is "not
supported". This trial finds out, in real browsers, whether Persian is shown, wrapped, laid out and exported
correctly. Full RTL support of the control is not needed; correct Persian is.

**Result in one sentence:** Persian cards, labels, the family layout, zoom, pan and export are correct in Chromium,
Firefox and WebKit at 360 px and 1280 px, but only with the workarounds named below – out of the box the control
draws Persian text in the wrong place in a right-to-left page, and it has no notion of text direction at all.

## What is here

| File | What it is |
|---|---|
| `src/main.ts` | The page: two cases, `?case=family` and `?case=relations`. Cards are built from the control's own elements (`setNodeTemplate` → `StackPanel` with an `ImageElement` and two `TextElement`s), not HTML templates and not `Native` shapes, so that the control draws and exports them itself. |
| `index.html` | The page around it, right to left, with the site's Persian font (Vazirmatn) and the CSS half of the direction workaround. |
| `build.mjs` | `node build.mjs` bundles into `dist/` (minified; `DASTAN_PLAIN=1` gives a readable build). |
| `try.mjs` | Opens addresses in Chromium, prints page errors, saves a screenshot. |
| `look.mjs` | The whole walk: both cases, 360 px and 1280 px, Chromium, Firefox and WebKit; screenshots, fit, ctrl+wheel zoom, drag, keyboard, the control's SVG and PNG export, the bare behaviours; writes `report.json`. |

## How to run

The browsers are the ones of the container of `src/Apps/Dastan/compose.yaml`
(`docker compose -f src/Apps/Dastan/compose.yaml up -d browser`, Playwright server at `ws://127.0.0.1:53333/`).

```
node build.mjs
node look.mjs E:/_eBooks/Sample/artifacts/dastan/diagram-persian
DASTAN_SEEN=E:/_eBooks/Sample/artifacts/dastan/diagram-persian node try.mjs "?case=family" "?case=relations"
```

Switches of the address that take one workaround away, to see the bare behaviour of the control:
`fix=none|ltr|css`, `wrap=char`, `same=1`, `curve=1`, `layout=tree`, `plain=1`, `nolabels=1` (explained at the top
of `src/main.ts` and in `index.html`).

## Findings

Screenshots are in `artifacts/dastan/diagram-persian` and are named `{engine}-{case}-{width}[-step].png`; below,
`*` stands for each of `chromium`, `firefox`, `webkit`. Every line of the table was looked at in the pictures named;
where not every one of the pictures was opened, the table says which were.

| | Chromium 1280 | Chromium 360 | Firefox 1280 | Firefox 360 | WebKit 1280 | WebKit 360 | Shown by |
|---|---|---|---|---|---|---|---|
| **A** Cards: picture right, bold name, facts line, right-aligned, long names wrap inside the card, mixed text, digits and punctuation at the correct side | works with workarounds 1, 2, 3 | same | same | same | same (needs also 2b) | same | `*-family-1280-zoomed.png`, `*-family-360.png` |
| **B** Connector labels readable, on their own connector; two opposite connectors told apart | works with workarounds 2, 4 | same | same | same | same | same | `*-relations-1280.png`, `webkit-relations-360-zoomed.png` |
| **C** Family layout: generations as rows, child of two parents under both, no overlap | works (`ComplexHierarchicalTree`) | same layout, see note on 360 | works | same | works | same | `*-family-1280.png`, `*-family-360-fitted.png` |
| **D** Zoom (ctrl+wheel) and pan (drag) | works | works | works | works | works | works | `*-zoomed.png`, numbers in `report.json` |
| **E** Export SVG / PNG by the control | order and pictures right; font and sharpness wrong – works fully only with workaround 5 | same | same | same | same | same | `*-export-svg.png`, `*-export-png.png` against `*-export-svg-repaired.png`, `*-export-png-repaired.png` |
| **G** Keyboard | Tab walks nodes then connectors; arrows push the node; focus never leaves | same | same | same | same | same | `report.json` → `keyboard` |

Pictures actually opened and judged: Chromium, Firefox and WebKit `family-1280` (page and zoomed), Chromium
`family-360`, WebKit `family-360-fitted`, WebKit `relations-1280`, WebKit `relations-360-zoomed`, the family exports
(raw SVG in Chromium and Firefox, raw PNG in Chromium and WebKit, repaired PNG in Chromium and Firefox, repaired
SVG in WebKit), the repaired relations PNG in Firefox, and the bare pictures named below. The other cells
(Firefox 360, the remaining exports) rest on `report.json` – no page errors, the same line widths, the same zoom
and offset numbers – and on the same code path, not on a picture that was opened.

### A – cards and text

* Letters join, Persian digits are shown, and `زال (Zāl)`, `کابل · Kabul`, `زادهٔ ۱۱۵۰ پ.م. – درگذشتهٔ ؟`,
  `… ۶۰۰ سال زیست!` stand in the correct order with the closing punctuation at the left, in all three engines.
* **Bare control in an RTL page: text is drawn outside the cards.** The control places each line by the x of its
  left edge and never sets `text-anchor`; in an RTL page each line lands one line-width too far left.
  → `chromium-family-1280-bare-fix-none.png`.
* **The control has no setting for text direction.** With the diagram kept LTR, every line has an LTR base
  direction: `(Zāl) زال`, `Kabul · کابل`, and `!` / `.` at the right end of Persian sentences.
  → `chromium-family-1280-bare-fix-ltr.png`.
* **Giving the texts `direction: rtl` by CSS is right in Chromium and Firefox but breaks WebKit**: words of the
  lines of a wrapped text are drawn in each other's line and a Latin line is torn apart.
  → `webkit-family-1280-bare-fix-css.png`.
* **`textWrapping: 'Wrap'` breaks inside words** (`رود` / `ابه`), which tears the joined letters apart.
  → `chromium-family-1280-bare-wrap-char.png` (the empty lines in that picture come from workaround 2 being
  combined with `Wrap`, not from the control alone). `'WrapWithOverflow'` breaks at spaces only.
* A line with no Persian letter (`FC Sistan 1925 ✓`) is left LTR on purpose; it is right-aligned like the others.
* The node takes the size of its card: the card with the three-line name is taller, and the layout keeps the rows
  apart. Nothing is cut.
* Firefox reports `downloadable font: STAT: Invalid nameID: 257 … Table discarded` for
  `vazirmatn-nl-wght.woff2`; the font is still used. This is about the font file, not about the control.
* Latin letters are not in this font file ("NL" = non-Latin), so they come from the `sans-serif` of the machine –
  in the Firefox of the container that is a serif face. Not a matter of the control.

### B – connector labels

* A label is a text like the others; with workaround 2 Persian and mixed labels (`هماوردِ (۱۰۵۵ پ.م.)`,
  `مادرِ – mother of`) are correct. Its background (`fill`) hides the line behind it.
* **Two connectors between the same two nodes are drawn on one line** – one line with an arrow at each end, and one
  label hides the other. → `chromium-relations-1280-bare-same-1.png`.
* Bent Bezier curves (`vector1`/`vector2`) tell them apart only until a node moves; then both fall back onto one
  straight line. → `firefox-relations-1280-bare-curve-1.png` and `…-curve-1-after-move.png`.
* Ports at different heights of the card keep them apart for good (workaround 4).

### C – layout

* `ComplexHierarchicalTree` puts generations in rows, the child of two parents (Rostam) under both, the children
  of one parent by two partners (Sohrab with Tahmineh, Faramarz without) under him, the half-brother (Shaghad)
  under the one shared parent, and the partner who has no parents in the graph (Tahmineh) in the row of her
  partner. No cards overlap.
* `HierarchicalTree` does not serve: parents that are roots are all put in the top row whatever their generation
  (Tahmineh beside Sam), and the graph becomes much wider. → `chromium-family-1280-bare-layout-tree.png`.
* The earlier failures of the trial – tiny cards at the top-left corner, `Cannot read properties of undefined
  (reading 'y')` in `getAnnotationPosition`, `Maximum call stack size exceeded` in the layout – had one cause: a
  `margin` that named only `left`. See workaround 1. Labels can be given with the connector; nothing has to be
  added afterwards.
* At 360 px the diagram opens at 100 % showing the top-left part of the tree; the first card is cut by the edge
  (`chromium-family-360.png`). Fitted to the page the whole tree is shown at 41 %, and the text is far too small
  to read (`webkit-family-360-fitted.png`). A phone needs zoom and pan; the layout itself is the same.

### D – zoom and pan

`report.json`, the same in all engines: at 1280 px zoom 1 → 1.2 after ctrl+wheel, and the drag changes the offset
(family: x −122.8 → −62.8, y 64 → 20.4); at 360 px zoom 0.41 → 0.49 and the offset changes likewise. Only the mouse
was tried – **touch (pinch, one-finger pan) was not tried**.

### E – export

* The control's SVG and PNG hold all cards, the pictures (8 `<image>` in the family, 3 in the relations), the
  connectors and labels, and – because the direction marks of workaround 2 are part of the text – the Persian text
  in the correct order with joined letters.
* **Fallback font.** The SVG names `Vazirmatn` but does not carry it, so a file opened by itself is drawn in
  another font, and since the control placed the lines by the widths of Vazirmatn, their right edges are no longer
  flush. The control's PNG is that same SVG drawn onto a canvas: the same fallback font.
  → `chromium-family-1280-export-svg.png`, `webkit-family-1280-export-png.png`.
* **The PNG is 1x**, soft on any modern screen.
* With the font put into the SVG and the PNG drawn from that SVG at 2x (workaround 5) both are as the screen.
  → `chromium-family-1280-export-png-repaired.png`, `webkit-family-1280-export-svg-repaired.png`.
* The export pictures show the exported file as an `<img>`, the way a downloaded file is seen. Not tried: opening
  the files in other programs (an image viewer, Inkscape, an office program), and printing.

### G – keyboard (what the control does by itself)

* The diagram (`#diagramcontent`, `tabindex="0"`) takes the focus. **Tab selects the next node, then the
  connectors, then starts again – the focus never leaves the diagram** (12 further presses in `report.json` →
  `twelveMoreTabs`; in the relations case the walk visibly wraps around). That is a keyboard trap. Shift+Tab was
  not tried.
* The order of Tab is the order of the model, not the order on the screen.
* **Arrow keys do not walk the graph: they push the selected node by one pixel** (`nodeMovedBy: 1`).
* With the tool used here (`ZoomPan | SingleSelect`) ctrl+A does not select all and Escape does not clear the
  selection.
* A click selects a node. What a screen reader announces was not tried; each text carries `role="img"` and an
  `aria-label` with its content (including the invisible direction marks).

### Other

* Minified bundle `dist/main.js`: **2,551,647 bytes** (624,647 gzipped) – the diagram control with data binding,
  the two hierarchical layouts and print-and-export, plus this page.
* The trial licence banner ("This application was built using a trial version of Syncfusion…") appears on every
  page, as expected; no key is used.
* No page errors in any engine in any case (apart from Firefox's remark on the font file).

## Workarounds in this trial, and why

1. **All four sides of every `margin` and `padding`** (`src/main.ts`, `card`). A margin with only `left` leaves the
   others undefined; sizes become NaN and the control fails far away from the cause (see C).
2. **Direction of text** (`index.html` and `persian` / `text` in `src/main.ts`). (a) The diagram's element is set to
   `direction: ltr` inside the RTL page, because the control computes text positions for LTR only. (b) Each line
   that holds Persian is wrapped in U+202B … U+202C (right-to-left embedding, pop) followed by U+200E
   (left-to-right mark). The marks must stand around each *line*, and the control makes the lines, so the control
   wraps the text once (`element.measure`), and the lines it found are handed back with their marks, joined by
   line feeds. (2b) The U+200E is needed for WebKit only: without it WebKit treats two neighbouring Persian lines
   as one run and mixes their words.
3. **`textWrapping: 'WrapWithOverflow'`**, never `'Wrap'`; `horizontalAlignment = 'Right'` on each text and
   `minWidth` on the column of words (a `width` without a `height` is ignored by a container), so that all cards
   are equally wide and the words stand at the right. The picture is simply the last child of a horizontal stack –
   the stack has no right-to-left order.
4. **A port per connector end** for several connectors between the same two nodes.
5. **Export repair** (`dastanExportRepaired` in `src/main.ts`): the font is embedded into the exported SVG as a
   data URL, and the PNG is drawn from that SVG at twice the size.

## What the product must do itself because the control does not

* Keep the diagram LTR inside the RTL page and put direction marks around every line of Persian text
  (workaround 2) – and decide per text which base direction it has.
* Never use `'Wrap'`; keep the text column of a card wider than the longest single word, because an over-long word
  sticks out of the card.
* Spread parallel connectors between the same two nodes itself (ports), and choose where their labels stand.
* Repair the export: embed the font in the SVG and draw the PNG itself at 2x or more.
* Build keyboard access itself: a way out of the diagram for Tab, walking along relationships with the arrow keys,
  a sensible order, and names for screen readers. As delivered the control traps the focus and lets arrow keys
  move cards.
* Decide whether visitors may move cards at all (dragging and arrow keys move them by default) and switch that off
  through node constraints if not.
* Choose the first view on a phone (which part of the tree, at which zoom) – fitting the whole tree makes the text
  unreadable – and try touch gestures, which this trial did not.
* Give the control's sans-serif fallback a Latin font of the site, since Vazirmatn NL has no Latin letters.
* Carry about 2.5 MB of script (0.62 MB gzipped) for the pages that show a graph, and load it only there.
