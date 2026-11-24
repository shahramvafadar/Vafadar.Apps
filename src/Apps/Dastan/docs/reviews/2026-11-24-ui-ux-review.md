# UI and UX review – 2026-11-24

The owner looked at the site with the real collections of `library/` for the first time and found it weak and
confusing, "like an old system, not a modern and professional one", and could not find how to see the histories
of Iran, China and Rome side by side. This review walks the main pages as a new visitor would, in Persian, dark,
at 1440 px and at 375 px, with the eight collections of the library. It names what is wrong and proposes a
redesign in steps. Nothing of the redesign is built yet; the owner decides the direction first.

Already corrected on the same day (D-090, D-091): pages as wide as the head, the map filling its stage, a chosen
still chart kept, a black and grey dark appearance.

## What a visitor meets

### The start (`/`)

* The first screen says "Dastan" twice – the logo and a heading – then two sentences and a search field. Nothing
  shows what the site is about: no picture, no story, no example of a connection. A modern start shows its best
  content at once.
* "What is here to read" is a grid of eight cards of very different heights. A card with a picture (China, a pin
  icon as placeholder) squeezes its text into a narrow column beside a large empty icon; the others have none.
  Histories, stories, a work and a list of universities stand mixed, with no grouping.
* "Start with" shows only the dynasties of China – one collection decides the whole section – and each card
  carries a large empty person icon, a long summary and a block of facts. The section is long and looks the same
  everywhere.
* **The comparison of Iran, China and Rome is the last thing on the page**, below everything else. A visitor who
  does not scroll to the end never finds it, and no collection page links to it. This is why the owner could not
  find it.

### A collection (`/subject-ancient-china`)

* The head is a title, a summary and four equal buttons ("pictures", "in time", "in a year", "on a map") and a
  search. What the collection holds – how many people, places, events, its span of years – is not said.
* No link to the comparison that the collection is part of.
* "Start with" repeats the same long cards as the start.

### A name (`/…/entity/person-cyrus-ii`)

* The head is good: picture place, name, kind, summary, facts, actions, and tabs (overview, connections, family,
  timeline). The empty grey person icon is large and says nothing.
* The connections below are one box per relation, one under another, each with the name of the page repeated
  ("Cyrus … → Cyrus …") and a small "sources" link. They are not grouped by kind (family, rule, places, events),
  so a person with thirty relations is a long, uniform list.
* Labels such as "parent" without saying father or mother are correct to the data but read as unfinished.

### Connections (`/…/connections`)

* The drawing works and moves, but it begins below the first screen: a long paragraph of instructions and the
  years bar come first.
* The texts of the cards are very small at the first view; the drawing is fitted to the whole frame, not to a
  readable size.
* Four "+" and "−" at the four sides of the stage, "larger/smaller/all/more" inside it and "towards …" above:
  three sets of controls for related things. A visitor does not know which to use.

### The story (`/story-rostam-sohrab/story`)

* A long vertical list of steps in one column. Each step is text, the names that take part, and a small link.
  There is no sense of progress (step 7 of 30), no way to move step by step, no picture.
* Roles of the people in a step appear in English on the Persian page ("hunter", "his horse", "the tale the poet
  announces"): the Persian of these role notes is missing in the content (a gap of the translation, not of the
  page).

### The comparison (`/compare/iran-china-rome`)

* The chart is the strongest page of the site and the least visible. In the dark appearance the bars of the three
  lanes are grey, so the lanes are told apart only by their names.
* The labels are small; the explanation above the chart is two long sentences.

### Across the site

* **Visual hierarchy is flat.** Headings, body text and buttons are close in size and weight; secondary buttons
  look like the primary ones; many links are plain underlined text. A page has no clear first thing to do.
* **Cards are boxes with a thin line** and the same padding everywhere; no image, no hover, no depth beyond a
  line. Placeholders (empty person and pin icons) take a quarter of a card and add nothing.
* **Too much explaining text** on the pages of drawings – the instructions are long paragraphs before the thing
  itself.
* **Navigation** has three ways at the top (Discover, Saved, Search) and the appearance menu. There is no way to
  the collections, the comparisons or the map of the site from the head; "Discover" is the long start page.
* **Phone**: the bottom bar is good; the start shows the same repeated heading and a card with a huge pin icon in
  the first screen.

## The cause in short

The site was built screen by screen against contracts of accessibility, privacy and correctness, and those hold.
What is missing is a layer of design on top: a clear hierarchy of type, cards that show content instead of
placeholders, grouping, a start that shows the best content, and navigation to the things that make Dastan
special (the comparisons, the maps, the stories).

## Proposed redesign, in steps

Each step keeps what the tests guard – no script needed to read, keyboard, contrast, right to left, no leak of a
later reading stage – and is looked at in a browser in both languages, both appearances and at phone width
before the next one.

| Step | What | Why first |
|---|---|---|
| 1 | **Ways in**: the comparison and the collections reachable from the head (a "Collections" way with histories, stories and comparisons grouped); each collection links to the comparisons it is part of; the start shows the comparison near the top as a featured card with a small preview of its chart | The owner could not find the comparison: the most valuable page is hidden |
| 2 | **Type and buttons**: a clearer scale (larger titles, heavier section headings, smaller muted meta), one primary button style and a quieter secondary one, text links only inside text | Every page gains a first thing to do |
| 3 | **Cards**: no empty placeholders – a coloured initial or a small kind icon instead of a large empty picture; equal heights in a row; summary clamped to three lines with the facts in one compact line; hover and focus states | The start, collections and "start with" stop looking like forms |
| 4 | **The start**: one line of identity (no second "Dastan"), a large featured row (the comparison, the Shahnameh, a history), collections grouped by kind (histories, stories and works, places), "start with" drawn from several collections and shorter | The first impression |
| 5 | **A collection's head**: what it holds in numbers and years, the views as a row of tabs like the page of a name, the comparison it belongs to | Orientation |
| 6 | **Connections of a name** grouped by kind (family, rule and offices, places, events, other) with a count each; the name of the page not repeated in every row | Long lists become readable |
| 7 | **Drawings**: the drawing first and the instructions behind a "how to use" disclosure; one set of controls (zoom and fit in the drawing; the sides' "+ −" kept for opening more, labelled); a readable first zoom | The graph and the timeline become usable at a glance |
| 8 | **The story**: a reading view with progress ("step 7 of 30"), previous and next, the names of a step as chips; the full list stays below | The story reads like a story |
| 9 | **Colour of the lanes** in the comparison in both appearances; labels larger | The chart that answers the owner's question |

Content gap found on the way, not a design step: the Persian of the role notes in the steps of Rostam and Sohrab
(and the other stories) – to be added to the translation files.

## Decisions for the owner

1. **Direction of the look.** (a) Keep the current calm, text-first look and make it clearer (steps 1–9 as
   above) – recommended: it fits a site of sources and reading, and every step is small and reversible; (b) a
   bolder, image-led look like a museum or a streaming site, which needs real pictures for most cards and so waits
   for content with images (pass 5 of the content roadmap).
2. **Order.** Recommended: steps 1 and 9 first (the comparison found and readable), then 2–4 (the general look and
   the start), then 5–8.
3. **Fonts.** The site uses the system font for Latin and Vazirmatn for Persian. A modern Latin font (for example
   Inter, open licence, served by the site like Vazirmatn) would make English pages look more finished. Optional.

## What was done

Steps 1 and 9 on 2026-11-24 (D-092, D-093): "Side by side" as a main way, a comparison a visitor builds of names of
any collections, lanes in colour. Steps 2–7 on the same day (D-094): type and actions, cards, the start, the head
of a collection, groups of connections, the guide of a chart folded. Step 8 on the same day (D-095): the story read one step at a time,
with where the reader is and the previous and the next step. The grouping of the collections of the start by
kind and "start with" drawn from all collections on the same day (D-096). Inter as the Latin font (decision 3, D-097) on the same day.
