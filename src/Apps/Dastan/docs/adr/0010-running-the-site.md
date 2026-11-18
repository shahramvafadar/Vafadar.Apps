# 0010. Running the site: one container, a mounted revision, a proxy it is told to believe

* Status: Accepted
* Date: 2026-11-17
* Step: 10.5 of the [delivery plan](../delivery-plan.md)

## Context

Until now the site ran on a developer's machine only. Whichever host the owner chooses, the site has to be a thing
that can be started there, asked whether it is alive, checked from outside, updated and taken back. None of that
depends on the host; all of it can be built and tried before one exists.

## Decision

1. **One container image, without content.** The image is built from the repository and holds the program. The
   collection is a folder mounted read-only when the container starts. Content and program are published
   separately, a new revision needs no new image, and the working files of the editors cannot get into an image:
   the build is not sent them.
2. **What is mounted is a published revision, and a revision is its content folder.** The `revision` command
   leaves out the pointers of media items to the working documents of reviewers, which lie outside the content
   folder; the readers never showed them, and a site must not need them to start. Found by starting the image:
   with the pointers the container stopped at once.
3. **The site says that it is alive at `/health`**, and the program can ask its own site (`--probe`), because the
   image has no tool to ask with. The answer is `ok` and nothing about the collection.
4. **The site listens without encryption and believes only the proxies it is told to believe.** A proxy holds the
   certificate and passes on how the visitor came. The site takes that from the addresses and networks named in
   `Dastan:TrustedProxies` and from nobody else; told nothing, it believes nobody. A general switch that
   believes every sender is not used: with it, any visitor could claim to be the proxy.
5. **Every cookie follows how the visitor came**, the cookie of the forms too: `Secure` when the visitor came by
   https.
6. **The keys of the forms can outlive a container** (`Dastan:Keys`, a folder on a volume). Without it they live
   as long as the process, which is right for a development machine and a pause for visitors at every update.
7. **The checks a host needs are a script that asks from outside** (`eng/Check-Site.ps1`), not a test inside the
   process: what a proxy and a container do to an answer is exactly what a test inside cannot see.
8. **Update and taking back are an exchange of containers**: start the new one beside the old, check it, switch.
   A collection that cannot be used stops the new container before it answers; the old one keeps running.
   Revisions are folders that are never changed, so taking one back is mounting the one before.

How it is done is written in [operations](../operations.md).

## Considered and not chosen

* **Content in the image.** One thing to deploy instead of two, but every correction of a text would be a build of
  the program, and an image would have to be kept from containing working files by care instead of by construction.
* **`ASPNETCORE_FORWARDEDHEADERS_ENABLED`.** Tried first. The framework no longer believes unknown senders with
  it, and where it did, it believed everybody.
* **Checking the headers in the tests of the repository only.** They are checked there too; but the first run
  from outside found two things the tests inside could not: the proxy was not believed, and the cookie of the
  forms was not `Secure`.

## Consequences

* The repository has a `Dockerfile` for Dastan, a Compose file to try the image on a development machine (in the
  Compose project `test`), a script of checks and a page of operations.
* A host has four things to give the container: the content folder, the proxies to believe, optionally a volume
  for the keys, and a proxy with a certificate.
* The privacy matrix records what the site keeps about visitors when it runs this way: nothing, and that the
  logs of a proxy and a host are theirs.

## Not decided

A host, an address, a certificate, monitoring, backups, a registry for the image, several containers at once,
load. See "What is not decided or not tried" in [operations](../operations.md).

## Verification

2026-11-17, Docker Desktop on Windows, linux/amd64, .NET 10 images: the image was built from the repository
(345 MB); a revision of the first collection, made with the `revision` command from a copy reviewed for the test,
was mounted; the container ran as user 1654 on a read-only file system with 512 MB and reported "healthy";
`Check-Site.ps1` passed all its checks against it, after it had failed on the proxy and on the cookie of the
forms and both were corrected; a form opened before a restart was refused after it without `Dastan:Keys` and
accepted with it. `SiteHealthTests` (Web) and `ContentRevisionTests` (Data: a revision of the collection starts
a site that has the content folder and no document of the editors). The revision of the trial is test data;
nothing is published and nothing was deployed.

## Addendum of 2026-11-18 – several collections on one site (step 14.10)

The site showed one collection: the file `Dastan:Sample` names. It now shows every collection of the folder
`samples`.

* **`DastanLibrary`** reads, when the app starts, the file `Dastan:Sample` names – the collection a visitor
  enters first – and then every other file of the folder that says it is a collection (`schemaVersion`), by
  the names of the files. A translation and a file that is something else are passed over. A collection that
  cannot be used stops the app, as before; so do two collections about the same thing, and a collection about
  something whose name the site uses for an address of its own (`media`, `compare`, `report` …).
* **`DastanSite` is the collection of one request**: the one the address names first
  (`/<language>/<collection>/…`), or the first collection for an address that names none – the start page, a
  comparison. Pages are written as before: they ask for "the collection" and get the one of their address.
  An address under the name of one collection never reaches a record of another: the page asks its own
  projection.
* **Each collection has its own reading position** – the cookie was named after the collection from the
  beginning – its own search, and its own files under `/media/<collection>/…`. The forms that carry the name
  of a collection (the reading position, a report) are answered for that collection.
* **The start page** lists the collections a visitor may see, each with what it is about as far as this
  visitor has read it. With one collection it is as it was, with the search of that collection; with several
  its search looks for a name in all of them (`<language>/search`): every collection is asked through its own
  projection at the visitor's reading position for it, and the hits stand under their collection. The choices
  of kind, place and year stay with the search of one collection – they are choices among its records.
* **The list for search engines** (`sitemap.xml`) names the pages of every collection.
* The comparisons belong to the site, not to a collection, as before.

For whoever runs the site nothing changes with one collection. To offer another, its file – and its
translations and files – is put into the content folder beside the first and the site is started again.

Not done: the same entity in two collections shown as one (each collection has
its own records; the identifiers in other catalogues of step 12.3 are what could connect them); a start page
the editors arrange.

Verification: `SeveralCollectionsTests` (13 cases) – the order of the collections and a file that is none;
the start page in both languages; the pages of each under its name; four addresses that mix two collections;
a search in its own collection; reading positions that do not touch each other and files under the right
name; a report kept under its collection; a collection the site could not reach. The second collection is
written from the template of the editors' tools. The 460 tests of the site with one collection pass
unchanged. Not looked at in a browser.

## Second addendum of 2026-11-18 – the same world in another collection (step 14.11)

Collections that name the same `worldId` are about the same world: another edition of the same work, another
telling of the same story. `DastanLibrary.SameWorld` answers the others for a collection, and the page of a
collection lists them – each with the text it follows, when it follows one – and leads to them.

**The reading position is not carried.** Each collection has its own stages and its own cookie; the page says
so. Carrying a position would need a statement of which stage of one telling corresponds to which stage of
the other – a prose retelling may tell in its second part what a poem tells in its fourth. Nobody has stated
that, and a guess would show a reader what they have not read. The same holds for a reading position for
each story of one collection: the model has one line of stages per collection, and whether reading one story
reveals something of another is the editors' statement to make, in a form the owner has to decide on first.

Verification: `SameWorldTests` (4 cases) – a collection names the other telling of its world in both
languages, and is named by it; a collection of another world is not named; what was read in one telling is
not read in the other until it is read there.

## Third addendum of 2026-11-18 – a reading position carried where the editors state it (step 14.11)

The second addendum said that a reading position is not carried because nobody states which stage of one
telling corresponds to which of another. The editors can now state it.

* **`spoilerPolicy.correspondences`** of a collection: for another collection – named by what it is about –
  pairs of stages, `here` and `there`: who has read this collection to `here` has read what the other tells
  up to `there`. A stage that is not named takes the answer of the nearest stage before it; before the first
  named stage nothing is stated. The readers refuse stages this collection does not have, a later stage here
  that corresponds to an earlier one there, a correspondence with the collection itself and one stated twice;
  the site refuses to start when a stage of the other collection does not exist.
* **The statement is one-sided**: what the poem says about the retelling says nothing about the way back. A
  collection that states nothing carries nothing.
* **The visitor carries it, not the site.** The page of a collection lists the other tellings of its world;
  where a correspondence is stated and leads further than the visitor has read there, a button offers to go on
  there from that stage. It is the form of the reading position of the other collection – the same explicit act
  as choosing a stage – so a link somebody sends still cannot move anybody's reading position. Nothing is
  carried backwards.

Verification: `StageCorrespondenceTests` (3 tests, readers) – nothing in the collection of the repository; the
nearest stage at or before; seven things that are no correspondence. `CarriedReadingTests` (3 tests, site) –
nothing to carry at the opening; the stated stage offered and carried by the visitor's act; never backwards
and nothing from a telling that states nothing. Not looked at in a browser.

## Addendum of 2026-11-18 – a line of reading stages for each story (step 17.7, D-058)

The second addendum left a reading position for each story of one collection open: the model has one ladder
per collection, and how reading one story reveals something of another was a statement of a form nobody had
decided. The owner decided (D-058): each story has a line of its own; the editors may state which stage of
one corresponds to which of another; nothing is carried without such a statement. The proposal named the way:
the model that is there.

* **A collection per story.** Stories of one world that are to be read each at its own pace are collections
  of their own with the same `worldId`. Each has its stages, its cookie and – for an account – its position;
  the page of each lists the others; `spoilerPolicy.correspondences` (third addendum) carry a position from
  one to another where the editors state it, forwards only and by the visitor's own act. Nothing new was
  built for this: the second and third addenda built it for "another telling", and another story of the same
  world is the same thing to the site. The format says so now.
* **One collection with several stories** keeps one ladder for all of them, as before: there, reading one
  story is reading the collection.

**Not built, and not decided beyond D-058**: several ladders inside one collection. A character who appears in
two stories is one record there; with a ladder per story the record would have to say for each story from
which stage on it may be seen, the reading position would be one stage per story, and every page, the search,
the list of a visitor and the sitemap would ask a different question before showing anything. That is a
change of the rule that keeps a reader from seeing what they have not read – not one to make on a guess of
what the editors will write.

Verification: nothing new to run – `SameWorldTests`, `StageCorrespondenceTests` and `SeveralCollectionsTests`
check that collections of one world have positions of their own, lead to each other and carry a position
only where it is stated.
