# Changelog – Dastan

Changes a visitor or the owner can notice, newest first. Technical decisions are in the delivery plan and the ADRs.

## Unreleased

- You choose the calendar dates are also shown in – Solar Hijri, Lunar Hijri or Gregorian only – in the
  settings of every page. A year is shown with the two Hijri years it lies in: "1010 CE (converted: 388 or
  389 SH)". A Hijri year can be typed where a year is asked for ("1400 SH").
- A picture of a history says what it is: a photograph of today of an old object, a work of its own time, a
  later picture, a reconstruction or a map – so that none is taken for another.
- For writers of content: a command compares the passages a collection rests on with copies of their sources
  (`quotes`).
- The Shahnameh shows its two counts of couplets side by side, each with its source.
- A collection can say where somebody comes from: Sudabeh comes from Hamavaran.
- A relation says what it is and no more: "Held the office of – King of the Medes" instead of "King of the
  Medes, according to Herodotus – King of the Medes". Whose account a statement is stands with its sources.
- How long something lasted is said where only a length is known: a king of Rome "for 37 years".
- The plan of everything that is left to build is written down (delivery plan, section 27). Nothing of it is
  built yet.
- A link to the story, the timeline, the map, a year or the pictures of a collection shows what the page is
  when it is passed on: these pages have a description now.
- On the start page every collection has the sign of its kind; before, only the first had one.
- On a phone a map begins where its places are: the map of ancient Iran no longer opens on Greece.
- The page of the places of a collection is called "… on the map", like the link that leads to it.
- The page of a large collection is short: a list of stories or of names shows its first twelve and opens the
  others in the same page ("The other 58 stories"). The page of the Shahnameh was about forty screens of a
  phone long.
- A story a collection only names no longer leads to an empty page of steps: its card leads to what is known
  of the story, and the empty page leads there and to a collection that tells the story step by step.
- "In a year": after a year is asked for, the page shows the answer and not the forms again.
- On a phone a map is as high as the map; the empty ground above and below it is gone.
- The Persian text of the Shahnameh writes an order in words ("دوازدهمین") and no longer as "۱۲مین".
- On a phone the names in a timeline no longer lie over each other or over the bars.
- The timeline of a large collection is short: its names stand by kind – periods and dynasties, people, events –,
  names that do not meet in time share a row, and you choose which kinds the chart shows. "A lane for each
  name" shows everything as before.
- The timeline of a collection and "in a year" show everybody and everything whose record says when: a person
  by the years of their life, a battle by its year, a dynasty from its beginning to its end. Before, a name
  stood there only through a relation with years, and most battles and many people were missing.
- A page in English asks for its font with the page: its text and its buttons no longer move a moment after the
  page is shown.
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
- Making an account asks for the tick "I am 16 or older". A site can take new accounts by invitation only
  (for a closed trial). A relationship can be of "another kind".
- The drawing of the places of a collection shows the coasts, rivers and lakes around them (Natural Earth,
  public domain, shipped with the site). It shows no borders and still loads nothing from anybody.
- A photograph taken with the phone upright is no longer shown on its side. Large photographs are shown faster
  where they stand among others: the site makes a smaller size of each and gives the browser the one that fits.
- A link to a page of the site is shown with a picture where somebody passes it on: the mark of the site.
- Without a connection, a page of a story that was opened on the same device before can still be read. Only
  public pages of somebody who is not signed in are kept that way – nothing of an account, a family tree or a
  photograph.
- Somebody who is in a photograph that everybody sees, and was not named by its owner, can say so under the
  photograph. It is then shown to nobody else until they agree or take that back.
- For editors: an image that is brought in through the tool gets two smaller sizes, and the pages of the site
  give a browser the one that fits – the gallery loads small pictures.
- For whoever runs the site: `photos sizes <folder>` makes the smaller sizes of photographs that were brought
  in earlier. For editors: an image that is in a collection already gets its smaller sizes from a button on its
  page in the tool.
- An album's owner changes the order of its photographs.
- For whoever runs the site: a Compose file for a closed trial with accounts (`compose.trial.yaml`), with the
  way to back up what the site keeps and to bring it back.
- The site has pages for its terms of use, its privacy notice and its imprint, named at the foot of every
  page. Their words come from files of whoever runs the site; a text that is not written yet says so.
- Without a connection, a page that is shown from what the device kept says so at its top. The page of your
  list says how many pages the device keeps and lets you have them forgotten.
- After signing in through a provider, the form that makes the account says what is kept of an account
  without a password; on a site that takes accounts by invitation only it says that an invitation is needed.
- What opens a folded part of a page ("Change the album") is as high as a button.
- A photograph of more than 50 million pixels is not taken, and the form says so.
- A photograph you bring in keeps only its picture: comments, further pictures hidden in the same file and
  anything hung on after the picture are left out.
- A wrong password counts on every form that asks for one; after five in a row the account takes none for a
  quarter of an hour.
- On a site that takes accounts by invitation only, one invitation makes one account.
- A sentence about two people of a family tree reads right when their names are in another script; days are
  written in Persian digits on Persian pages; marks of charts are easier to press.
- A chart of numbers no longer fails for some ranges of values.
- In the history of a family tree, with whom the tree is shared and who said a record is them is read by the
  owner of the tree only.
- The copy of your data leaves out the code of an invitation that can still be used.
- What belongs to accounts is kept in PostgreSQL. Nothing a visitor sees has changed.
- The work of the editors is kept in PostgreSQL too. Nothing an editor sees has changed; what two editors do to
  the same collection at the same moment is now done one after the other, so neither loses the other's change.
  A copy of the store is made with the database's own tool.
- A site can show what the editors published straight from the database: the revision that was released, which
  can be the newest or an earlier one again, and none after it was withdrawn.
- In the tool of the editors: the page of the revisions says which revision the site shows, and a publisher
  releases one or withdraws the collection there; a comparison is released on its own page. A change to a record
  that somebody else changed in the meantime is refused and says who came first.
- Reports of visitors and notices about content are kept in the database. For whoever runs the site: the
  settings for them say only whether they are taken, and a role of the database can be given to the site and
  to the tool that lets each do its own work and no more.
- The limits per hour count a whole IPv6 network as one visitor.
- The site has its legal pages: terms of use, privacy notice, a page that lists everything the site stores on
  your device, and the legal notice – in English, Persian and German. They are linked at the foot of every page.
- The site stores on your device only what you asked for. Your language is remembered when you change it, not
  when you merely open a page; a short note, shown once, says so. Pages are kept for reading without a
  connection only after you turned that on – on the page "Cookies", where you also turn it off.
- A new account confirms that the terms and the privacy notice were read. Accounts made before are asked once
  on their page.
- "Report content": anybody can tell who runs the site that something on it is against the law or the terms,
  and is shown a reference at once. Pages with trees, photographs and people have a link "Report this page".
- If who runs the site hides a photograph, takes a family tree back from the public or closes an account, the
  owner reads what was done, why, on which ground, and what to do about it – on the page of the account.
- An account has a mail address now. Making an account asks for a name, an address and a password; the
  account opens when the link that is mailed to the address was opened, and the recovery codes are shown then.
  Nobody else sees the address, and the site writes to it only about the account.
- "Forgot the password?" mails a link that sets a new one. The recovery codes stay, for who cannot read the
  mail of the address any more.
- The page of the account shows its mail address, changes it – the new address confirms the change – and
  signs the account out on every other device.
- Signing in works with the name or with the mail address.
- Signing in through Google or Microsoft takes the mail address they confirmed as the address of the account.
  An address that already has an account makes no second one: sign in to it and add the provider there.
- The accounts with a name and a password alone are gone; nothing of an earlier database is carried over.
- The site tells you by mail when somebody asks something of you: to show a relationship, a tag on a
  photograph, your agreement to a photograph or a family tree being shown to everybody. The message says who
  asked and where to answer. You turn these messages off on the page of your account.
- If who runs the site hides a photograph, takes a family tree back from the public or closes an account, the
  reasons are mailed to the address of the account as well, and so is the end of such a decision.
- An invitation can be mailed by the site to an address you name. The site uses the address for that one
  message and does not keep it.
- The accounts of the editors are kept the same way as those of the visitors, in tables of their own. Nothing
  an editor sees has changed; an account of the tool is made again, like everything else of an earlier
  database.
- What was wrong with a form is read out by a screen reader as a list again, on every page with a form.
- Every entity has a profile on its page: the few facts that matter for what it is – each stated, or said to be
  not known – and its picture, or a drawing of its kind where there is no picture that may be shown. The list
  of a collection shows each subject as a card with the same.
- The tool of the editors asks for the facts the kind of an entity has, marks an essential one that is missing,
  and lists what the profiles lack before a revision is made – as notes that stop nothing.
- A person of a family tree has a page of their own, with a picture or a drawing, when and where they were
  born and died, a few words about them and their family. A date can be a day, a month or a year of the
  Gregorian or the Solar Hijri calendar, "about", between two years, or said to be not known.
- What is kept about a person depends on who they are: of somebody who died, what the family knows; of
  somebody who says a record is them, what they write themselves; of a child, what a parent decides. Of
  somebody who lives and was not asked, a tree keeps a name, the years and the family – nothing else – and
  the page says so. The picture of a person is seen by exactly those who may see that photograph.
- Two people can show several relationships: a friend who is a colleague too, each with where and since when.
  New kinds: parent and child, adoptive parent, guardian, brother or sister, teammate, neighbour. Changing what
  a relationship is called asks the other person; until they agree it stays as it is.
- An account can block another. What the two showed ends at once; the blocked account cannot ask, tag, invite
  or find the other one, and is not told.
- "Where you were": schools, employers and other places, with what you were there and when. You choose who sees
  a place and may find you through it, and – apart from that – who sees the years. A search finds the people
  who chose to be found by you through a place.
- You can look at your own page and at a family tree of yours as everybody, as one of your circles or as one
  person sees it.
- A family tree tells a parent by birth, an adoptive parent and a guardian apart; a guardianship can say from
  when until when.
- In a list, a card offers a look at the profile: it opens beside the list, shows what the page of the node
  shows, and leads on to the page. Without a script the list is as before.
- A family, the connections of a name and a family tree of yours can be moved, made larger and smaller, walked
  with the arrow keys and saved as a picture (SVG or PNG). Choosing a card and pressing Enter opens its profile
  beside the drawing. The still drawing is still there: one press shows it, and it is what you see without a
  script.
- You can ask for a time in your own words: a year ("500 BCE"), a stretch of years ("300 to 400") or a depth of
  time ("55 million years ago", "10 to 20 thousand years ago") – in the search of a collection and in "in a
  year". Times that lie very far back are written with their number, their unit and what they are counted from.
- A timeline can be moved and made larger and smaller: with the mouse, a finger, the keyboard or its buttons.
  It shows what is known as it is known – a window in which something happened as a window, an end that is
  open as open – and times that lie very far back, and the order of a telling, have views of their own.
- A picture can say the time it shows, apart from the time it was made. The gallery of a collection can be asked
  for a time – a year, a stretch or a depth of time – and shows what shows that time, whenever it was made;
  what was only made in that time is listed apart.
- A date that is exact to the day is shown as it was written and, beside it, in the calendar of the page –
  Solar Hijri on a Persian page, Gregorian otherwise – and is said to be converted.
- A card that is chosen in a graph offers its timeline and its pictures; every name with dated statements has a
  timeline of its own.
- The privacy notice and the terms say what an account is now: a name, a mail address that you confirm and
  that nobody else is shown, and a password; which mail the site sends you and how long it keeps it; and that a
  decision about your account is mailed to you with its reasons. In English, German and Persian.
- Ending a relationship, blocking somebody and sharing a circle or a tree with them at the same moment can no
  longer leave somebody in a circle or a tree of an account they show nothing with, and two links made at the
  same moment can no longer make somebody their own ancestor. The command `people repair` puts right what the
  earlier version could have left behind.
- An account made through Microsoft, or through any provider that only names a mail address, is an account
  once you have opened the link the site mails to the address you wrote – not before, whichever way you come.
  You can have the mail again or correct the address. Nothing tells anybody whether an address has an account.
- Where you were: when you state a university, a company, a club or an organisation, you can look for it among
  the entries of the collections and choose one – or none, and keep your own words. A place you chose stays the
  same place when it gets another name and is not taken for another place of the same name; people are found
  through the entry, whatever name each wrote, and never through a name alone. You can state several times at one
  place, and the years between them count as not there. The same choice exists for where two people were
  colleagues, classmates or team-mates. The published collection holds no such place yet.
- Every page has the same head: the name, then Discover, My World, Saved and Search, then your account. The
  language and the appearance (light, dark, like your device) are together behind one control at the end of the
  head. On a phone the four ways are a bar at the foot of the screen. Saved is there before you keep anything.
- The page of a name, its connections, its family, its timeline and its pictures have one bar that leads from
  each to the others. Going to another view and coming back finds the first as you left it: what you opened, how
  far, in which order. You can look closer at somebody in a list or a drawing without leaving – "Chosen" – and
  put them in the middle when you want to. An address you pass on carries all of it, and nothing about you.
- The start page begins with a large search, shows the story with its picture or a drawing of what it is, the
  names to start with as cards, and a piece of a real drawing of connections that you can open in full. It shows
  only what you have read to, and nothing on it asks for an account.
- In a drawing of connections or of a family, what you choose is shown closer beside the drawing – on a phone
  in a sheet from the foot of the screen that you open and close –, with the ways to put it in the middle, to
  its whole profile, its timeline and its pictures. The tools you use all the time are in sight with their
  keys; saving a picture is behind "More". A family tree that grows sideways can now be moved, made larger and
  saved like the one that grows down. Under a drawing stand the steps of the story the name takes part in, as
  far as you have read. You can add names to a comparison and see their times side by side.
- The page of a name begins the same way for every kind of thing: a mark that says at the first glance whether
  it is of a story or of the world, its picture or the drawing of its kind, its name, where it is, a line and
  the nearest facts – and directly below "Explore the connections" and "Keep". Then come "In story and
  picture": up to three pictures and every step of the story the name has to do with, as far as you have
  read, as cards. Every fact of the profile, the other names, the sources and what other catalogues call it
  are under "Details and sources", folded until you open it. The page of an account and of a record in a
  family tree say what they are in the same way.
- The page of a story shows its steps on the timeline of the site: with the script the chart can be moved and made
  larger, on the axis of the order of the telling – it says nothing of dates or lengths. A chart without the
  script now draws a window of time as a window, not as a point in its middle; what is counted in a depth of
  time or in the order of a telling stands under it as a table of its own, and is not put among years.
- A gallery begins with its pictures; why they are there, the time you can ask for and the order are in one
  fold above them. Opening a picture and going back returns to the gallery as you left it – the same time, the
  same order, the same kind. One picture can stand beside somebody and at a step of the story with words for
  each place; the picture itself, its rights and its sources stay one.
- "My World" is a page of its own: what waits for your answer stands first, then your people, where you were, your
  family trees and your photographs, each with how much there is and the way to it. The same list of these
  spaces stands on each of their pages. The page of a family tree is three parts – see the tree, change it,
  share it – with a way to each; you are shown only the parts you may use, and what waits for your answer in a
  tree is said at its top.
- A large family tree opens much faster: the page of a tree of 500 people asked the database some two thousand
  times and took three and a half seconds before its drawing could begin; it now asks each thing once.
- Dastan has its logo: a symbol – an open D with a dot and a page – and the name beside it, in the head of every
  page, as the icon of the tab and of the installed app, and in the picture a link is shown with when it is
  passed on. On a dark appearance the logo is white.
- The tool of the editors shows the symbol of Dastan before its name and as the icon of its tab.
- "My World" also names what a family tree asks of you – whether you agree to be named in public, and whether a
  record somebody says is them is confirmed – and leads to where you answer. A tree no longer says that
  something waits for you once you have answered.
- Where a relationship names a place the site has a page of ("colleague, at …"), the name of the place leads to
  that page.
- In the moving view of a story a name that is only mentioned at a step has a hollow mark, and every mark says
  what the name has to do with the step.
- A drawing you made larger or moved is as you left it when you come back with the Back button or load the page
  again; "the whole drawing" shows all of it again.
- For editors: the form of a picture has inputs for the places it stands in with words of their own – an entity
  or a step, a caption, sources – and for the caption of each place in the other language. A form that is
  refused shows again everything that was filled in, also a list that was begun in it.
- A gallery has themes when a collection states them: the gallery of a collection offers its themes with the
  number of pictures under each, a theme has a gallery of its own, and the page of a picture names its themes.
  For editors: a theme is added as a name of the kind "Theme" – it needs no source – and the form of every image
  and video offers the themes of the collection to tick.
- The years you choose for a name are kept by its connections and its pictures as by its timeline: you see what
  lies in those years, apart from it what states no time, and how much was left out because it states another
  time. The years are asked for, changed and given up on the bar above each of these views.
- A place you were at that the site has no entry for can be proposed to the editors, from the page of the places
  you were at. The proposal is kept without your name and your account. When an entry of that name exists
  later, the page says so beside your statement and lets you choose it. For editors: a page of the proposed
  places, where the same place proposed several times stands together and is decided once.
- Your page can say something about you: a few words, read by who you choose, and one of your photographs as
  your picture, seen by who may see that photograph. The page of an account and the page of a person of a family
  tree begin with the picture or a drawing, the name and a few facts.
- The logo has files for a printer (PDF and EPS, outlines, CMYK) and the brand has a guide
  (`branding/dastan/guide`). The colours for paper are a beginning until a proof is printed.
- For writers: the real collections have a folder of their own (`library/`). Every file in it is checked with each
  build, and the site can be started on it with the launch profile `library`.
- For who runs the site: a family tree can be written into an account from a file (`people import-tree`), whole
  or not at all, for an account that asked for it.
- The years you choose are kept to by the page of a name and by its family tree as well: what states another time
  is left out and counted, what states no time is shown apart, and descent is always drawn.
- A theme that several collections have is one theme: the page of the themes of the site lists it once, and its
  page shows the pictures of every collection that has it, each under its own name and as far as you have read it.
- Your page can say a few facts about you, if you choose: the year you were born, where you live, the languages
  you speak and one address of a page of your own. None is asked for; each is seen by who you choose and begins
  with nobody else.
- In a drawing of connections the name in the middle has a larger card, and a drawing with many cards shows the
  far ones smaller – a name and one line – so that more of it is in sight.
- A limit of the site per hour – of tried passwords, mails, reports – counts the last sixty minutes; the full hour
  of the clock no longer opens it again.
- A family tree in years says that it leaves out a partnership of another time together with the family that
  reaches the name only through it.
- A wide screen is used: a page is as wide as the head of the site, cards and drawings stand side by side, and
  running text keeps a length of line that can be read.
- The map of the places fills its frame instead of a small part of it; on a phone it keeps its size and scrolls.
- When you chose the still drawing of a timeline or of a graph, its own tools – across or down, closer or wider,
  earlier or later – keep the still drawing instead of going back to the one that moves.
- The dark appearance is black and grey instead of navy and blue.
- "Side by side" is one of the main ways at the top of every page (at the foot on a phone). It lists the
  comparisons – Iran, China and Rome for now – and lets you choose what to compare yourself: up to eight names
  from any of the collections, such as the Achaemenids, the Western Han and the Roman Republic, drawn on the same
  page over one line of years, each with its life from founding or birth to its end. A field finds a name in the
  long list. The start shows the comparisons right after the search, every collection leads to them, and the
  page of a name with a time offers "Compare with others".
- Each lane of a moving chart of time has its own colour, as in the still chart.
- A new look across the site: clearer titles and buttons, rounded cards with short summaries, a small round
  mark of the kind where a name has no picture instead of a large empty field. The start begins with what the
  site is for; a collection says how many names it has and which years it covers; the connections of a name
  are grouped by relation in one list of rows; the guide to a chart is folded under it.
- A story can be read step by step: "Read step by step" opens the first step with where you are ("Step 4 of
  19"), the previous and the next step, and the way back to all of them. Only the steps up to where you have
  read are counted.
- The start groups its collections – lands and their history, stories and works, more to explore – and the
  names to start with come from all of them, not only from one.
- English text is set in the font Inter, which comes with the site like the Persian font.
- The roles of the names in the steps of Siavash and of Rostam and Sohrab are in Persian on the Persian pages.
