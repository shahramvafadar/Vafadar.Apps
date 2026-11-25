# Dastan – backlog

What is left to do for Dastan, in one place, as of 2026-11-25. The history of what was built is in the
[delivery plan](delivery-plan.md); the decisions are in
[the decisions of the product](product/12-decisions-questions-and-change-log.md). This page says what comes
next, who does it and when it is done.

**The order the owner set** (2026-11-23): first the remaining development (part A), in this line of work; the
content (part B) is written in a line of work of its own, from [the work order for content](content-work-order.md);
then everything is tried in a browser on the owner's machine (part C); then the site goes online (part D).

Status words: **Planned** – agreed, not begun; **Waits** – needs somebody or something named in the row;
**Doing**; **Done** (with the day). An item is done when it is built, tested, documented, merged into `main`
and the checks of the repository are green.

## A. Development – this line of work

Begun and done on 2026-11-23, on the owner's word. Each item was built whole – with its tests and its documents – before the next.
How each is built – the files, the rules, the tests, the documents – is in the
[delivery plan](delivery-plan.md), section 25 (A6 is 25.1, A5 is 25.2, A1 to A4 are 25.3 to 25.6).

| # | Item | What is built | Done when | Status |
|---|---|---|---|---|
| A1 | The years of a name on its page and on its family tree | The page of a name and the drawing of a family keep to the stretch of years of the state of exploring, as the timeline, the connections and the pictures do (D-084). What states no time is shown apart and never hidden; what states another time is counted and said. The bar of exploring asks for the years on these two views too, and "Not left out in this view: the years" is said nowhere any more | A relation and a fact with a time are left out outside the years and shown inside; a relation without a time is shown apart; tests of the projection and of the pages in English and Persian; ADR 0025 | Done (2026-11-23) |
| A2 | A theme across collections | The editors state that a theme of one collection is the same theme as one of another, the way a place is the same place: both carry the same identifier of the same catalogue, marked `exact` (ADR 0023, ADR 0027). A page of the site shows a theme with the pictures of every collection that has it, each under the name of its collection and as far as the reader has read that collection. Nothing is joined by a name | Two collections with one theme show on one page; a theme of a later stage of one collection is not named; tests; ADR 0027; the format page | Done (2026-11-23) |
| A3 | More that an account says about itself | A year of birth (a year, never a day), where the account lives (a town and a country, in words), the languages it speaks, and one address of a page of its own. Each has an audience of its own and begins "nobody else". The head of the page of the account shows what the viewer may see. The copy of one's data, the closing of the account and "see as" hold them | Each field is kept, shown by its scope, exported and taken back with a circle; a change of form of the database with the test that asks for pending changes; the privacy notice in three languages and the record of processing; ADR 0024 | Done (2026-11-23) |
| A4 | Cards of a graph in sizes of their own | The name in the middle of a drawing has a larger card, the others the usual one, and a drawing with many cards a compact one (name and kind, no picture). The still drawing of the server and the moving one agree, as they do today | The three sizes in the still and the moving drawing, in Chromium, Firefox and WebKit; the budgets of time of step 20.12 still hold on this machine; ADR 0020 | Done (2026-11-23) |
| A5 | A family tree written from a file | A command of who runs the site: `people import-tree <account name> --from <file> --people <database>` writes a tree – its people, who is a parent of whom, who are partners – into the account that is named, by the rules of the store (PRIV-003: of somebody who lives only the name, the years and the family). It refuses a file it cannot read whole and writes nothing then. The format of the file is in [the work order for content](content-work-order.md), part 7 | A tree of three generations is written and shown on the page of the tree; a second run writes nothing – nothing is added to a tree that is there; a file with a person who is their own ancestor is refused; tests; operations guide | Done (2026-11-23) |
| A6 | A folder for the real collections | `src/Apps/Dastan/library/` holds the collections that are written to be published, with the same three folders as `content/` (`samples`, `comparisons`, `media`). `content/` stays what the tests are written against and is not changed by content work. A test reads every file of `library/` with the check of the site, so that a file that cannot be used fails the build; a library that holds no file yet is in order. A launch setting starts the site on `library/` | `dotnet test` fails on a broken file in `library/`; the site started on `library/` shows every collection of it; the images of the site hold nothing of it; README and the guide for writers | Done (2026-11-23) |

A5 and A6 come first: the content work needs them. Both are done.

## B. Content – its own line of work

Written from [the work order for content](content-work-order.md), which says for each item what is in it, how
it is written and when it is finished. Nothing here is reviewed or released by who writes it.

| # | Item | Status |
|---|---|---|
| B1 | The Shahnameh as a work: the poet, the making of the poem, its three parts, its dynasties and kings in order, its chief heroes and their houses, its manuscripts and editions | Done 2026-11-23 (`library/samples/shahnameh.json`, with its Persian; the three parts are left out for want of a source; not yet looked at in a browser) |
| B2 | The story of Rostam and Sohrab, whole: every step of the telling, everybody in it, its places | Done 2026-11-23 (`library/samples/rostam-and-sohrab.json`, 30 steps, with its Persian; not yet looked at in a browser) |
| B3 | The story of Siavash, as a second story of the same world | Done 2026-11-24 (`library/samples/siavash.json`, 35 steps, with its Persian; not yet looked at in a browser) |
| B4 | Iran to about 100 CE: Elam, the Medes, the Achaemenids, Alexander and the Seleucids, the Arsacids | Done 2026-11-23 (`library/samples/ancient-iran.json`, with its Persian; not yet looked at in a browser) |
| B5 | China to about 100 CE: Shang, Zhou, Qin, Western Han, Xin, Eastern Han – and Xia as tradition | Done 2026-11-23 (`library/samples/ancient-china.json`, with its Persian; not yet looked at in a browser) |
| B6 | Rome to about 100 CE: the kings, the Republic, the emperors from Augustus to Trajan | Done 2026-11-23 (`library/samples/ancient-rome.json`, with its Persian; not yet looked at in a browser) |
| B7 | The comparison of the three over time, brought to the same span and the same statements | Done 2026-11-23 (`library/comparisons/iran-china-rome.json`, built from the three collections; not yet looked at in a browser) |
| B8 | Places people can have been at: a small collection of real universities and organisations, so that "where you were" has entries to choose | Done 2026-11-24 (`library/samples/places.json`, twelve institutions, with its Persian) |
| B9 | The owner's family tree, written into the owner's account on the owner's machine – never into the repository | Waits for the owner's account – the command is there (A5) |
| B10 | Persian for all of the above | Done 2026-11-24 with each item (drafts, not reviewed) |

The passes after this first one – the further stories of the Shahnameh, the histories into late antiquity, the
neighbours, other works, pictures and comparisons – are planned in [the content roadmap](content-roadmap.md).

## C. Trying it – with the owner

After A and B. The site and the tool of the editors are started on the owner's machine with the real
collections, and the owner looks.

| # | Item | Who | Status |
|---|---|---|---|
| C1 | The site and the tool of the editors, started on `library/`, with the addresses given to the owner | this line of work | Done once on 2026-11-24, on the owner's word; the owner's findings became [the review of the look](reviews/2026-11-24-ui-ux-review.md), whose nine steps are built. Started again only on the owner's word |
| C2 | A second account with a made-up name, so that relationships, circles and "who was here" have two sides | this line of work | Waits for C1 |
| C3 | The light appearance of the pages of sections 24 and 25 of the delivery plan. Looked at on 2026-11-23, light and at the width of a phone, in English and Persian: the themes of the site, the page of a name and a family tree in a stretch of years, the form of the facts of an account. Not yet: the gallery and the page of a picture with their themes, the connections and the pictures in a stretch of years, the form that proposes a place, the head of a record of a tree, the drawing of connections with its card sizes | this line of work | Waits for the owner's word to start the site (AGENTS.md) |
| C4 | A real phone, the site installed as an app, Safari itself, a screen reader | the owner | Waits |
| C5 | Five people who see the site for the first time ([user-test.md](user-test.md)) | the owner | Waits |
| C6 | The owner's own photographs, brought in by the owner | the owner | Waits |

## D. Going online – the owner's

| # | Item | Why it is the owner's | Status |
|---|---|---|---|
| D1 | Reading the statements of every collection against their sources and recording the review; publishing a revision | Reviewing and publishing are decisions of the editors (ADR 0009). Until then a public site shows nothing | Waits |
| D2 | The rights of every picture that is to be shown | Rights are established by the owner; a picture without them is named with its holder and not shown | Waits |
| D3 | A mailbox and a sender; the clients at Google and Microsoft; the licence key of the graph; a host and an address | Accounts, contracts and secrets are entered by the owner alone | Waits |
| D4 | `database migrate` on the database of the host before the site starts | Part of bringing a version up ([operations](operations.md)) | Waits for D3 |
| D5 | The legal pages read by a lawyer; the postal address | Section 18.8 of the delivery plan | Waits |
| D6 | Somebody who looks at the reports of visitors and at the proposed places | An editorial duty (D-085) | Waits |
| D7 | A proof of the colours of the brand on paper; a Latin font of the brand; the registration of the mark | D-087 | Waits |

## E. Found by the review of 2026-11-24

What reading the whole against the documents of the product found ([delivery plan](delivery-plan.md), section
26). What is planned is planned in full – what is built, the tests, the documents, when it is done, what has
to be settled first – in the [delivery plan](delivery-plan.md), section 27 (E2 is 27.2, E6 is 27.3, E5 is 27.4,
E3 and E7 are 27.5, E4 is 27.6, E20 is 27.7, E11 is 27.8, E18 is 27.9). **Two matters wait for the owner's
word** before anything is begun: the order among them (27.10; proposed: lengths of reigns first, pictures
immediately before the pictures of the content, the calendars last) and the way E11 is put right (27.8;
proposed: a rule of the content guide, the labels corrected, a test that refuses a repeat).

| # | Item | Why | Status |
|---|---|---|---|
| E1 | The views of time read the times a record states among its facts (D-098) | The library states its dates as facts; 134 of 212 names with a time were in no view of time | Done (2026-11-24) |
| E2 | A reign known only by its length, without years | The Roman kings; the reigns of the Shahnameh ([content roadmap](content-roadmap.md)) | Planned |
| E3 | A picture of a history as a media reference: a relief, a coin, a bronze – and with it the kinds of picture document 15 names (a later photograph of an object, a reconstruction, a map) | `depictedContext` knows a later artwork of a story and a diagram of today only | Planned |
| E4 | A command of the content tool that reads every source of a collection and compares a stored quote with it | Done by hand for the first pass of content | Planned |
| E5 | Quantities of a work as dated statements in the generator of the writer | The length of the Shahnameh | Planned |
| E6 | A relation of a person to the land they come from, in a story | "Sudabeh of Hamavaran" | Planned |
| E7 | The times of a picture beyond when it was made and what it shows: brought in, published, shared (MED-006) | The reader refuses them today | Planned, with the pictures of pass 5 |
| E8 | `spoilerPolicy.correspondences` between the three collections of the world of the Shahnameh | Content, not development: a reader of one story has read a stage of another | Planned, content |
| E9 | The calendar a source writes in: kept (document 04, TIME-009) or every date Gregorian (D-043) | The two contradicted each other | Decided (2026-11-24, D-100): every date Gregorian; see E18 |
| E10 | The chart of time of a history by kind, a kind put in and taken out by a link, names that do not meet sharing a row (D-099) | The page of Iran was about 36,000 px high; the chart is 410 px to begin with | Done (2026-11-24) |
| E11 | Labels of relations that repeat the name they lead to ("King of the Medes" to an office of the same name) | Seen in the chart of Iran on 2026-11-24 | Planned, content |
| E12 | At the width of a phone the names of bars that stand close run over each other and over their bars – more visible since E10, where many names share a lane | Seen on 2026-11-24 | Done (2026-11-24): a row is kept for a mark as far as its name really reaches |
| E13 | A list of the page of a collection shows its first twelve cards and folds the others; a story without a step leads to its name, and its page of steps leads on | The page of the Shahnameh was about 43,500 px high on a phone; its 70 stories all led to "No step of the story to show" | Done (2026-11-24) |
| E14 | A year asked for leads to its answer (`#answer`); the stage of a map is as high as the map on a narrow screen | Seen on a phone on 2026-11-24: the forms filled the first screen again; a map stood in the middle of an empty stage | Done (2026-11-24) |
| E15 | Persian orders written in words in the text of the Shahnameh (148 places) | "۱مین پادشاهی" is not Persian | Done (2026-11-24), content, still a draft |
| E16 | The title of the page of places reads "{name} on the map", as the link to it does | "Places of Places of learning and work" | Done (2026-11-24) |
| E17 | A map wider than its stage is moved sideways once, so that the place in the middle of its places stands in the middle (`stage.ts`) | On a phone the map of ancient Iran began with Greece | Done (2026-11-24) |
| E18 | A visitor sees a date in another calendar – Solar Hijri, Lunar Hijri – beside or instead of the Gregorian one; what is stored stays Gregorian (D-100) | The owner's word of 2026-11-24 | Planned, the last piece of work |
| E19 | The pages of the story, the chart of time, the places, a year and the gallery say what they are in their head (`PageDescription`); every collection of the start page has the drawing of its kind | A second look on 2026-11-24: 260 pages read for names of links and fields, double identifiers, the order of headings – nothing found there; these pages had no description, and one card of the start page alone had a sign | Done (2026-11-24) |
| E20 | The title of a source in the language of the source (`lang`), so that a screen reader reads an English title in a Persian page as English | Seen on 2026-11-24 | Planned |

## Not planned

Named so that nobody takes them for forgotten: the order inside the world of a story (no collection states
one); borders of a land and borders that change; a spot colour of the brand; videos of visitors (D-066).
