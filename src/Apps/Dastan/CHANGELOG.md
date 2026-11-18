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
- The connections of a name are a chart on the whole page: the name stands in the middle, who or what leads to it
  above, who or what it leads to below, and a partner or an opponent beside it – as in the family tree. A "+" at a
  side of the chart opens the names there a step further, a "−" closes them again; a single name can still be
  opened and closed by itself.
- Content can be written by anybody who follows the guide – a person or an assistant: one guide for writers, a
  page for each format, and a command that says whether the site can use a file, with every problem and the record
  it belongs to, that lists the values a field may have and that writes the smallest file to start from.
- A collection has a page "in time": what is stated about its names with a year or a day, and when its images and
  videos were made, on the same chart as a comparison – opened and closed at its four sides, with the same facts
  as a table. The story itself is not on it: a story has an order, not dates. Writers state such dates in the
  file of the collection; the first collection has none yet.
- The Persian font is asked for together with the page, so the text of a page does not move when the font arrives.
- The rule for publishing exists: everybody sees a collection only as a published revision – numbered, with every
  statement reviewed – and of it only reviewed texts and the images and videos allowed for the public. A public
  page names the revision it shows. Nothing is published yet; the first collection is still an editorial sample.
- A published revision says who reviewed each statement and when, and what it changed: a page "what changed"
  lists the revisions of a collection, newest first, with the names, relations, steps and images each added,
  changed or removed – only as far as the visitor has read. The editors make a revision with one command that
  refuses a file with a statement nobody reviewed. Nothing is published yet.
- A statement the sources disagree about is marked as disputed wherever it appears – in lists, in the charts of
  connections, family, story and time, and in tables – and the page of its sources says what the disagreement is.
  A statement the editors take back is no longer shown anywhere; the page "what changed" names it with the reason.
- A work whose image may not be shown is no longer missing from the galleries: it stands there as a reference,
  with its title, who made it and when, its catalogue number and credit line, and a link to the museum that holds
  it. The three museum works of the first collection appear this way, each from the part of the story it belongs to.
- The charts of connections and of a family open with the name they are about in sight, however much is opened
  above it. On a phone the chart is as wide as the screen: the steps of its left and right side stand in a row
  above it instead of beside it.
- The site can run outside a developer's machine: as a container that holds the program and is given a published
  revision to show. It says whether it is alive, and a script checks from outside what a host has to be sure of.
  How to update it and how to take a revision back is written down. It is not hosted anywhere yet.
- The editors have a store of their own: a collection is brought into it from its file and written out again
  unchanged, and every change of a name, a relation, a step or an image is kept with who made it, when and why,
  and what it said before. The public site does not use the store. Working in the store without a file, and a
  tool with pages for the editors, are the next steps.
- In the store the editors can record the review of a statement – reviewed, disputed or withdrawn –, change one
  record and take one out, without a file in between. Each change is tried on the whole collection first; what
  the site could not use afterwards is refused with the reasons.
- The editors publish a revision out of their store: it numbers the revision itself, refuses a collection with a
  statement nobody reviewed, keeps every revision as it was published and can write one out again.
- The editors have a tool in the browser, Dastan Studio: it shows the collections of their store, every record
  as it is written, what still waits for a review, and the history of each record with what it said before and
  after. It runs on the editors' machine only and does not change anything yet; the commands do.
- Dastan Studio now does the work as well: the editors record a review, change a record, take one out, add one
  and publish a revision on its pages. What was done is said; what cannot be done is said with the reasons, and
  nothing is changed.
- Dastan Studio shows the fields of a record as a form: texts, numbers, choices of the allowed values, the reading
  stage, the sources and records a field points to as boxes to tick, and rows for a list of parts – a row is added
  by filling in the last one. The record as text stays below it, for a field the record does not have yet.
- In Dastan Studio the Persian text of a field stands below the field and is changed with it. A row that is taken
  out takes its Persian role with it; a row that is added can be given one.
- Dastan Studio can run for several editors: each has an account and signs in, every change is kept under the name
  of the account, and nothing is shown to somebody who has not signed in. Accounts are made by a command where the
  store lies.
- Dastan Studio can run on a server as a container, with its store on a volume. A copy of the whole store can be
  written while the editors work (`store copy`) and put back when the store is lost.
- A collection can name more than the things of its story: real persons, organisations, places and regions, and
  what is said of them – who wrote a work, who taught whom, where somebody was born or died, who belonged to or
  founded what, what lies where. Their pages name their kind in English and Persian. The first collection does
  not use this yet.
- The vocabulary of a collection is complete against the product documents: companies, brands and products,
  universities, governments, dynasties, families, peoples and countries, clubs, teams, competitions and seasons,
  events and periods, offices and honours, real objects and subjects of knowledge – and 47 more things that can
  be said between them, from "held the office" and "translation of" to "merged into", "plays for" and "possibly
  the same". Each has its meaning and its name in English and Persian.
- A relation of the world outside a story can say when it held – from when to when somebody belonged to a court
  or played for a club. The time stands beside the relation and on the timeline; somebody who came back is
  shown with two separate spans, never one.
- An entity can have other names – earlier names with the time they were used, titles, short forms, its name
  in its own script – and the identifiers other catalogues give it. Its page lists them and the search finds
  it by any of them.
- A collection no longer has to be one story. It can tell several stories, each with its own steps and page,
  or none at all: a collection about a company, a club or a person, with what is known about it and when. A
  collection can be written in Persian or any other language instead of English; pages in another language say
  so where a text exists in that language only.
- In the tool of the editors the names of the reading stages can be changed in every language, and a stage
  added; the Persian texts of a statement with a date stand beside it in the form of its entity. A change of
  the stages or of anything else that is not a record now appears in the history with who made it and why.
- A collection can state quantities: how many people lived in a city in a year, how many verses a poem has,
  what something cost. The page of an entity now lists everything that is stated about it – with every digit
  as the source gives it, and "about" where the source says so.
- A real place can say where it is on the earth and how exactly that is meant. Its page shows the position
  and lets a phone or computer open it in its own map; the site itself loads no map from anybody.
- Corrected after looking at the pages in a browser: the degrees and numbers on the drawing of places stand
  where they belong, the drawing of a quantity over time is no longer cut off, and the forms that ask for a
  year, a kind or a place are laid out like the search.
- "My list": a page of a name, an image or a thread can be kept with one button. The list lives in a cookie
  on your own device – there is no account, and the site keeps nothing of it.
- Where the editors state which part of another telling corresponds to what you have read, you can go on
  there from that part with one button – never further than what you have read.
- A collection names the other collections of the site that tell of the same world – another edition, another
  telling – and says that each keeps its own reading position.
- One site can offer several collections: each has its own address, reading position and search, and the
  start page lists them and looks for a name in all of them.
- Editors can lay threads through a collection: a few stops in an order, each with the reason it follows.
  A collection lists its threads, a thread is shown as far as a visitor has read, and the page of a name says
  where a thread leads on from it and why.
- A visitor who sees an error can say so: the page of the sources of a statement has a short form. The site
  keeps the words, the statement and the day – nothing about the visitor – and the editors find the report in
  their tool, with the way to the record it is about.
- A moment of a video has an address – add `?t=1:15` or use the field under the player – and the page of
  the sources of a statement says how to cite it, with the revision it was read in.
- The search also finds without a name: everything of a kind, at a place or of a year. A gallery can be put
  in the order its pictures were made, and show images or videos only.
- Pages can be found and passed on: each says where it is in every language and what it is about, a public
  site lists its pages for search engines – never one a new reader has not reached – and pages print without
  the navigation.
- When the same quantity is stated of something for several years – the people of a city, say – its page
  draws the numbers over time under the list.
- A collection that states years has a page "In a year": choose a year, and it says who was where and what
  held then – and says "may have held" where the sources leave it open.
- A collection that names real places has a page "On the map": the places drawn where they lie to each
  other on a grid of degrees, and listed with what is said to be there. The site still loads no map from
  anybody.
- Images and videos are brought in through the tool of the editors: a new item with its file, or a poster,
  captions and a transcript for an item that is there. The tool recognises a file by its content, names it
  itself and removes from an image what it says about where and with which device it was made.
- A comparison is worked on with forms in the tool: its title and sources, and each of its lanes with its
  periods and events and their times. Other names, identifiers and dated statements of a name can be added
  with the form from the first one on.
- When two records turn out to be the same person, place or thing, an editor merges one into the other:
  everything that named the one names the other, its name stays findable, and the history says what was
  changed.
- In the tool of the editors a new record – a source, a name, a relation, a step of the story – is added with
  a form, and a collection gets a further language with one input.
- An editor's account says what it may do – write, review, publish – and the tool offers each of them only to
  who may. Every editor can change the own password in the tool and see when it was last used or tried.
- When two editors work on the same record, the second change is no longer kept over the first unnoticed:
  the tool says that somebody changed the record meanwhile and keeps what was filled in for another try.
- In the tool of the editors a time is filled in with inputs – a year, a month, a day, "about", from and until,
  an unknown beginning or end – instead of being written as text.
- Comparisons are kept in the store of the editors like collections: brought in, changed with their
  translations in the tool, with every change recorded, and written out for the site.
- A visitor can make an account – on a site that was told where to keep accounts: a name and a password, nothing
  else about the person and no mail address. Signing in and out, another password, recovery codes for a lost
  password (shown once), closing the account with thirty days to open it again. The name of who signed in stands
  in the header. An account opens nothing yet; the list, the reading position and the private spaces follow.
- The links in the header of a page no longer stand without a gap between them.
- Who is signed in has their list and their reading position on every device. A device keeps its own for
  who is signed out; a new account begins with what its device holds, and signing in takes nothing from a
  device unless its visitor says so on the page of the account.
- Who has an account can invite somebody: a link to pass on by any way they like, good for fourteen days and
  for one person. The site sends nothing, and the invitation connects nobody with anybody.
- The small buttons beside an entry of a list ("Take out of my list") look like the rest of the site.
- Two people with accounts can show a relationship together – friend, family, colleague and so on. One asks
  by the other's account name, only the other can accept, and each says who else may see it: nobody, a circle
  of their own, or everybody. It is shown to those both allow. Either can end it. Who is asked finds
  "1 waiting" in the header; the site sends nothing to anybody.
- Circles: a name for some of the people one shows a relationship with, to show something to them only.
- Every account has a page that shows its name and what the viewer may see of its relationships.
- Family trees: who has an account can begin a tree, add people – a name and, when known, the years of birth
  and death; a person needs no account – and link parents and partners. The owner names who may see the tree
  and who may change it, and every change is listed with who made it. Nobody else sees a tree. It is shown as
  a list for now; the drawing follows.
- In a family tree shared with them, somebody can say of a record "this is me". Who made the record confirms
  or refuses – the same name is not proof. Confirmed, the record names the account and nothing else changes;
  the account can take it back at any time.
- The page of the account hands over a copy of one's own data as a file: the list, the reading positions,
  invitations, relationships, circles and one's own family trees.
- A family tree of one's own is drawn: parents above their children, partners side by side, like the family
  of a story. A name in the drawing leads to its record.
- Photographs – on a site that was told where to keep them: who has an account brings a picture in, says who
  may see it (only me, a circle, everybody) and who it shows. Somebody a photograph is said to show is asked
  and can say yes or no; saying yes shows the photograph to nobody who could not see it before. A person's
  page shows their photographs to those who may see them, and one's own page lists what was shared lately.
  What a picture file says about where and with which device it was taken is removed before it is kept.
- A family tree can be made public: its owner asks, sees who stands in the way, and every living person the
  tree names is asked with their own account. A living person without an account cannot agree, so such a tree
  stays private. Anybody who agreed can take that back, and the tree is hidden at once.
- An account can be signed in through Google or Microsoft, on a site whose owner set that up. They only tell the
  site that it is the same person again: the visitor chooses the name of the account, and no name, mail address
  or picture is taken from them. An account can add and remove such a way in, and always keeps one. Not tried
  against Google or Microsoft yet – that needs the owner's registration with each.
- A photograph that is said to show somebody is shown to everybody only when they agreed. Until then its owner
  and the people it names see it, and its owner is told who is waited for. Anybody who agreed can take that
  back, and the photograph is hidden from everybody else at once.
- Photographs can be put into albums: a name and a note for photographs of one's own. An album shows nothing to
  anybody by itself – each photograph is seen by the people chosen for it – and stands on the page of its
  owner for who may see something in it. Photographs can be gone through by the year they were taken.
- Who shows a photograph to everybody ticks that the people in the picture agree; without that it is not shown
  to everybody.
- For whoever runs the site: `reports forget <folder>` deletes reports of visitors that were looked at more
  than 90 days ago; `--kept-days` names another number.
