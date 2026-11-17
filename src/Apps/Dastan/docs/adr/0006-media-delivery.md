# 0006. How a media file reaches a browser

- Status: **Accepted** – implemented and verified on 2026-11-16
- Date: 2026-11-16
- Deciders: Shahram Vafadar
- Governing documents: 15 (media and galleries), 16 (PILOT-05, PILOT-06, section 10), 18 (FLOW-04, SCREEN-06,
  SCREEN-07, the states), 20; M0-05, M0-06, M0-12
- Related: [ADR 0002](0002-one-projection.md) (one projection decides), [ADR 0003](0003-server-rendered-pages.md)
  (no script), the working rules of the app (section 3: `content/` is never a web asset)

## Context

The collection holds images and a video with a poster, captions and a transcript. Each of them gives away what the
item gives away, so each of them is shown under the same two conditions as the item: the editors released it for the
audience of the site, and the visitor has read as far as the item. The files lie in `content/`, beside the editorial
sample with the whole story; that folder is never a web root.

A later phase adds a service worker and, later still, private spaces. What is decided here has to hold for both.

## Decision

**One route delivers files, and it asks the projection.** `GET /media/{collection}/{item}/{part}` –
`MediaFiles.MapMediaFiles` in `Vafadar.Dastan.Web`.

* The address names an **item and one of its files** (`file`, `poster`, `transcript`, `captions-<language>`,
  `rendition-<n>`), never a path. The path is taken from the model after the visitor's projection returned the
  item, and it is checked once more to lie inside the folder of the collection. A name in the address that the
  model does not know is nothing; there is no way to ask for a file by its name on disk.
* The projection is the visitor's own: the reading position of the request's cookie and the audience of the site.
  **Hidden is unknown** here too – an item that was not read to, an item that is not released, an item that does
  not exist and a part the item does not have are the same answer: `404` with no body and the same headers.
* Only files of a **known kind** are served (PNG, JPEG, WebP, SVG, MP4, WebM, WebVTT, plain text), each as its
  type, with `X-Content-Type-Options: nosniff` and the content security policy of every answer of the site.
* The answer is **`Cache-Control: private, no-cache`** with **`Vary: Cookie`**: nothing between the site and the
  browser keeps it, and the browser asks again every time it shows the file. After a visitor moved back in the
  story the file is gone again, not only its page. A video is delivered in ranges, so that a player can start
  before the file has arrived.
* The address carries **no language** (`/media` is one of the unlocalized beginnings of `Vafadar.Web`): a file is
  the same in every language of the site.

**A page decides what it offers before it writes an address.** The gallery and the page of an item ask the
projection for the item and the site whether the file exists. A file that is missing on the machine gives the
state "cannot be shown right now" with everything else that is known about the item – no broken player, no
placeholder picture.

**Consequences for what comes later**

* A service worker (phase 6) must not cache answers of `/media`: a cached file would outlive the reading position
  it was allowed for. The header already says so; the worker has to honour it rather than apply a rule of its own.
* A private space will need the same route to ask an access decision instead of a reading position. The shape –
  an address without a path, an answer per visitor, hidden equals unknown – stays.

## What the pages show, and what they do not

* **What kind of picture it is** stands above the picture and in the list of the gallery: "Explanatory diagram made
  today", "Artwork made long after the story". The time of an item is labelled as the time it was made, with the
  sentence that it is not a time in the story (document 15).
* **A date is written as the record states it** (`TimeText`): the era is always named, "about" stays "about", and a
  Gregorian day on a Persian page is that Gregorian day in Persian digits – it is not moved into another calendar.
* **The gallery states its rule and its order** – everything of the collection, what shows an entity, what shows a
  step; in the order of the editors, which says nothing about dates – and counts only what it shows. An empty
  gallery says the same sentence whether nothing exists or nothing has been reached.
* **A video starts nothing by itself**, loads only what the player needs to show its length, has its captions on
  (the video has no sound; in the language of the page when there are captions in it) and its transcript as text on
  the page. Captions and the transcript exist in English only and are marked as English on a Persian page.
* **The page of an item returns to the gallery it was opened from**, with the item as the place to return to. The
  address says which gallery that was (`?entity=`, `?event=`); the page believes it only when the projection has
  that gallery and the gallery holds the item.
* **Not shown: the editors' note on the scope of an item** (`RepresentationScope`). It is written for reviewers, in
  English, and refers to records by their identifiers. The kind of the item and its caption say for a visitor what
  the note says for a reviewer.
* **Not shown at all: items that are only references to a museum's page.** They are not released for showing in the
  demonstration, so the projection does not return them and no gallery counts them. Showing them as cards without a
  picture is a decision about content and rights for the public release, not for M0. *(Decided on 2026-11-17 – see
  the addendum below.)*

## Verification

`GalleryAndMediaTests`, `MissingFilesTests` and the public site in `SiteTests` (`Vafadar.Dastan.Web.Tests`): the files
are delivered byte for byte with their types at the stage that allows them and are not found one stage earlier, for
every file of both items, with an answer that cannot be told from that of an unknown item; a range of the video is
answered as a range; nothing but the named files of released items is delivered – not the sample, not a file by its
name, not a path that climbs out; after moving back, page and files are gone for that visitor and stay for another;
with files missing, the pages stay and say so. In a browser at 360 px, English and Persian: the image is shown at
the width of the page, the video plays and its captions are shown.

## Addendum of 2026-11-17 – a work that may not be shown, as a reference (step 10.3)

An item that may not be shown to the audience and has a public page at the institution that holds it
(`sourceUrl`) is no longer left out as if it did not exist. It is a **reference**:

* **What a reference is.** `Projection.References` holds a `MediaReference` for each such item within the
  reading boundary: the editors' title and caption, the kind of work, when and by whom it was made, the title,
  number and credit line of the catalogue, the state of the rights, what it depicts, and the address of the page
  of the holder. The record is a type of its own and leaves out everything of the image – the address of the
  image file, the text alternative, the sources, the editors' notes – so a page can show all of a reference and
  shows nothing it may not.
* **Where it is shown.** In a gallery – of the collection, of a name, of a step – below the items that are shown,
  under a heading that says the works are held elsewhere, as a card with a broken border, without a picture, with
  a link that names the host it leads to. A gallery that holds only references is not empty, and the link to a
  gallery is offered for a name or a step that only a reference shows.
* **What it is not.** A reference has no page of its own, no file, no place in the chart of time, and is not found
  by `Projection.Find`; the route of the files answers "not found" for it as before. Nothing is asked of the
  holder's server: the page links to it and loads nothing from it (the content security policy would not allow it).
* **For whom.** For every audience by the same rule: in a demonstration an item that is not released for showing,
  for the public an item that is not allowed for the public. An item that is shown is never also a reference; an
  item that may not be shown and has no page elsewhere – the project's own video in a public revision – is not
  named at all.

The reading boundary holds for a reference as for an item: the work of the ending is not named before the ending.
The page of the holder can tell more than the visitor has read; the link says that it leaves the site.

Verification: `MediaReferenceTests` (Data, on the sample: its three museum works by stage), `Disclosure` (what a
reference leaves out stays a secret at every stage), `GalleryAndMediaTests` (the card and its record, no address
of the image, no page and no file; the galleries of a name and a step; Persian), `PublishedSiteTests` (the public
gallery).

## Second addendum of 2026-11-18 – a file is brought in through the tool (step 13.7)

Until now a media file came into the folder of a collection by hand. With editors on a server (D-046) it comes
through the tool.

* **`MediaIntake.Take`** (`Vafadar.Dastan.Data`) takes a file for one part of an item – its file, the poster
  of a video, captions in a language, a transcript, a further rendition – and keeps it in the folder `media`
  beside the samples.
* **What a file is is read from its content**: PNG, JPEG, WebP; MP4, WebM; WebVTT; plain text. Not from its
  name and not from the type its sender states. An SVG is not taken – it can carry a script, and the files of
  the repository that are SVG were written by its authors, not received. What is none of these is refused.
* **The name on the machine is made by the tool**: the identifier of the item, the part, and a number when
  there already is such a file. The name a file had on the editor's machine is used nowhere, so no path can
  be asked for and no name tells anything. A file is never written over another.
* **Limits**: an image 25 MB, a video 250 MB, a text 2 MB; the tool takes a request of that length and no
  longer.
* **An image loses what it says about its making**: the EXIF and XMP segments of a JPEG, the text, time and
  EXIF chunks of a PNG are left out before it is kept – they can say where a picture was taken and with which
  device. A WebP that carries such notes is refused with the advice to save it without them (rewriting a WebP
  safely needs a library). **A video is kept as it is**: the tool says so, and the editor removes such notes
  before bringing it in.
* **Its size is read from the image** and written into the record (`dimensions`).
* **The record decides**: the record with the new file is put into the store like any change, tried by the
  readers; when it is refused, the file that was taken in for it is deleted again. A file that is replaced
  stays on the machine – the history of the item still names it.
* **A new item states its rights**: the readers refuse an item with a file and without established rights, so
  the form of a new item asks whether it is original project content or openly released by its holder, and
  for the statement. It starts as not shown (`demoRenderEligibility.allowed: false`): an item becomes visible
  by what an editor then says about it, never by being brought in.
* **Not built: converting.** The tool makes no smaller sizes and re-encodes nothing; an editor brings a
  rendition in as a file of its own. Resizing needs a library that reads and writes images – a dependency,
  which the owner decides on.

Verification: `MediaIntakeTests` (11 tests, readers) – each kind recognised by its content, named after its
item, sized, without its metadata; a second file under a name of its own; ten refusals that leave nothing
behind; names and parts that are none refused before anything is written. `StudioMediaIntakeTests` (7 tests,
tool) – the form of a new item in both languages; a new image kept with its file, its rights and nothing of
the name it had; an SVG, a taken identifier, a missing name and missing rights leave no record and no file;
captions in Persian and a poster for the video that is there; a file of another kind refused and not kept;
only the page of an image or video brings files in. Files of the tests are made by the tests. Not looked at
in a browser; no real photograph with location data and no large video was tried.
