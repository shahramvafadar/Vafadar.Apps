# 0003. Pages are rendered on the server, without a live connection and without a script

> Changed on 2026-11-19 by [0019](0019-interactive-layer.md): pages still come from the server and are complete without a script, and a layer of TypeScript in the browser takes over where a page is explored. "Without a script" no longer holds.

- Status: Accepted (implemented and verified on 2026-11-16 for the pages of phase 4)
- Date: 2026-11-16
- Deciders: Shahram Vafadar
- Governing documents: 09 (presentation and rendering, web-safe localization), 16, 18, 20 (step B);
  delivery plan, decisions 2, 4, 5 and 6; acceptance conditions M0-01, M0-06, M0-09, M0-10, M0-12
- Related: [ADR 0002](0002-one-projection.md), repository [ADR 0013](../../../../../docs/adr/0013-web-library.md)

## Context

The plan asked for a written result on where interactive components are needed. Blazor can render a component on
the server per request (static), keep a connection open per visitor (interactive server) or run it in the browser
(WebAssembly). The second keeps state of every visitor in the server's memory; the third downloads a runtime. The
pages of M0 show text, links, a few forms, two media files and small graphs.

## Decision

1. **Every page of M0 is rendered on the server for its request** (`AddRazorComponents` without an interactive
   mode). No page keeps a connection open, and no page of phase 4 loads a script – not even the framework's
   navigation script.
2. **A request is all there is of a visitor.** Language comes from the address, the reading position and the
   theme from cookies; `Visitor` is built per request and asks `DastanSite` for the projection of that reading
   position. Nothing of a visitor is stored on the server, so two visitors cannot affect each other and a restart
   loses nothing.
3. **State a visitor can share is in the address; state that is theirs is in a cookie.** The address names the
   language, the collection and the entity. The reading position is never in the address: a link cannot move its
   receiver forward (delivery plan, decision 5).
4. **What a visitor does is a link or a form.** Moving the reading position and choosing the theme are forms that
   post to `/preferences/…`, protected by antiforgery tokens, answered with a redirect to a path of the site.
5. **Hidden and unknown are one page.** A page asks its projection; without an answer it sets "not found". The
   response for an entity of a later stage and for an entity that does not exist is the same page with the same
   status.
6. **Addresses:** `/{language}/` (Discover), `/{language}/{collection}` (the entry of a collection; the name is
   the identifier of its story) and `/{language}/{collection}/entity/{id}`. Views of later phases are added under the
   collection. `/preferences/…` carries no language.
7. **An interactive component is added only for a gesture that cannot be a link or a form**, with a written
   reason, and never as the only way to the content. Known candidates for later phases: the connection-loss notice
   and the install prompt (phase 6). The video uses the browser's own player. The graph is decided in
   [ADR 0005](0005-graph-renderer.md).
8. **The site decides who it shows the collection to.** `Dastan:Audience` is `Public` unless the configuration
   says otherwise; only the Development settings ask for a local demonstration. A site started for the public shows
   that nothing is published.

## How it is verified

* `Vafadar.Dastan.Web.Tests` starts the site in memory and asks it as a browser does, with cookies kept per
  visitor and sent only to the paths they were set for. It follows every link from the start page for reading
  stages 0 to 4 in both languages and looks for any text that belongs only to hidden records; it runs two visitors
  with different languages, reading positions and themes at the same time; it checks that an address cannot raise
  the reading position, that the editorial files cannot be downloaded and that a page contains no script and no
  inline style.
* In a browser on 2026-11-16: Persian and English, light and dark, 360 px, 412 px and 1280 px, the reading form
  and the hidden-then-visible page of Sohrab. Layout was measured in the page (no horizontal overflow, reading
  direction of a relation, size of touch targets); two screenshots were taken, further ones failed because the
  preview pane was not drawing.

## Consequences

* Pages work without JavaScript, are complete in the first response and can be read by a search engine once a
  collection is public.
* Every click is a full page load. If that proves slow, the framework's enhanced navigation can be added without
  changing a page.
* A refused form answers with a bare status 400 today. A friendly page for an expired form belongs to the failure
  states of phase 6.
* Every page carries a form token, so pages are not cached between visitors.


## Addendum of 2026-11-16 – one script since phase 6

Since [ADR 0007](0007-installable-app.md) every page loads one script, a file of the site: it registers the service
worker that shows a page for "no connection". What this record decided stands: a page is complete in the first
answer, needs no script to show its content or to be used, and has no script written into it. The two candidates
of point 7 – a notice for a lost connection and an install prompt – were not built. A refused form now leads to a
page that says so.

## Addendum of 2026-11-17 – the script does a second thing

The one script also brings the name a chart is drawn around into sight
([ADR 0008](0008-charts.md), fifth addendum). The rule is unchanged: a page shows its content and can be used
without the script, and no script is written into a page.

## Third addendum of 2026-11-18 – pages that can be found and passed on (step 14.4, R-PLAT-04)

The pages are text written by the server, so an engine can read them. What was missing is what tells an
engine, or a messenger that previews a link, where a page is and what it is about.

* **Every page names its address** (`PageAddress`, in the layout): `rel="canonical"`, the same page in each
  language of the site (`hreflang`), and `og:url`, `og:type`, `og:site_name`, `og:locale`. The address is
  the one the page was asked by – scheme and host as the site was reached, which behind a proxy is what the
  trusted proxy says (docs/operations.md). The pages "not found", "refused" and "an error" take it back:
  they have no address of their own. It stands in the head through a section, because a page decides that it
  is not found after the head was begun.
* **A page of content says what it is about** (`PageDescription`): the start page, a collection, an entity
  and a comparison carry `description`, `og:title` and `og:description` – the summary the page shows to this
  visitor, cut at a word after 200 characters. A description never says more than the page: it is made from
  what the projection gave for the reading position.
* **`/robots.txt`**: a public site lets every engine in, keeps it out of `/preferences/` and names the list
  of its pages; a demonstration sends every engine away, as its header and its `meta` already do.
* **`/sitemap.xml`** (public sites only; a demonstration answers "not found"): the start page, the collection,
  its stories, gallery, timeline, places, the page of a year, its revisions, every entity and every image or
  video, and the comparisons – each in every language, naming the others. **Only what a visitor sees who has
  read nothing yet**: a page of a later reading stage is in no list, so an engine cannot show what a reader
  has not reached.
* **The answer to a search** carries `noindex, follow`: it is no page of its own.
* **Paper**: the style sheet has rules for printing – no navigation, no forms, no steps of a chart; a chart is
  printed as far as it fits and the list under it says the same; a link that leads out of the site is followed
  by its address.

Not done: a picture for the preview of a link (`og:image`) – the site has no picture that stands for a page,
and one of a collection would have to be allowed for the public first; a feed of what changed; structured
data (schema.org).

Verification: `FindablePagesTests` (10 cases) – the address of a page in both languages on four pages; the
description in the words of the page; no address on a page that is not there; `robots.txt` and the list of a
demonstration and of a public site; nothing of a later reading stage in the list; rules for paper in the
style sheet. Not looked at in a browser and not printed.

## Fourth addendum of 2026-11-18 – a list of one's own, in a cookie (step 15.1, B-016)

A visitor wants to keep a page to come back to. The product documents call that saved views and place them
with the accounts. An account is a decision of the owner; a list is not.

* **The list is a cookie on the visitor's device** (`dastan.saved`): the collection, the kind of page – a
  name, an image or video, a thread – and its identifier, for at most 30 pages. It is written like the other
  cookies of a choice: readable by the site only, not by a script, sent with requests of this site, for a
  year. The site has no account and no store for it: nothing of a list is kept on the server, and another
  browser or device does not have it. The page of the list says so.
* **Keeping and taking out** is one button, sent without a script, with the token every form carries. Keeping
  a page the visitor cannot open is refused: the page is asked of the projection of its collection at the
  visitor's reading position.
* **The list never shows more than its pages do.** The page of the list asks the projection again for every
  entry. A page that was kept and cannot be opened any more – the reading position was moved back, a record
  was withdrawn – is not shown and not counted; it comes back when it can be opened again.
* **A cookie is written by anybody.** What is no entry of a list is passed over; a list that is too long is
  cut; nothing of a cookie is written into a page but through the records it names.
* **A full list lets its oldest page go.** Thirty pages fit a cookie with room to spare; a larger list would
  need a store, and with it an account.
* The link "My list" stands in the head of the pages of a visitor who has a list. The page of the list is not
  for search engines.

Not done: saved views of a chart or a search (an address of the site already is one, and a browser keeps
it); names and notes for kept pages; a list that follows a visitor to another device – that is an account.

Verification: `SavedListTests` (17 cases) – keeping a page and finding it in the list, in both languages;
the cookie as it is written; the page without a list; the last page taken out; a page that cannot be opened;
a page that is not reached any more; four things that are no act; three cookies that are no list; a full
list; pages of two collections. Not looked at in a browser.

## Addendum of 2026-11-18 – the picture a link is shown with (step 17.4, D-056)

The third addendum left the picture for the preview of a link out: it would have to be drawn for each page.
With a library that writes images (D-056) the site draws one.

* **One picture for every page**: `/preview.png`, 1200 by 630 pixels – the mark of the site (the paths of
  `icon.svg`) on its ground, drawn once by `PreviewPicture` and kept. A page that has an address of its own
  names it in its head with its whole address, its kind and its size (`og:image`, `og:image:width`,
  `og:image:height`, `og:image:alt`) and asks for the large form of a preview (`twitter:card`).
* **What a page is about stays text**: the title and the description of the page stand beside the picture
  wherever a link is shown (third addendum). Nothing of a collection is in the picture, so a link to a page
  shows nothing a visitor has not reached.
* **Not built: the words of the page on the picture.** Drawing text needs a font in the program for every
  script the site writes. The site has one font file – Vazirmatn without Latin letters, in a form for browsers
  – and takes the Latin letters from the device of the visitor; a container has no fonts at all. Persian
  needs its letters joined as well, which is a second library. Fonts in the program are a decision that was
  not made.
* The mark was the provisional one, drawn in two places. Since 2026-11-21 the picture draws the symbol of the
  logo from the one geometry of the brand (`BrandGeometry`, [ADR 0026](0026-logo.md)): a new mark is a change
  of that geometry and nowhere else.

Checked: `PreviewPictureTests` (4 cases): the picture is a PNG of that size with the ground at its corner and
the mark in its middle; pages in both languages name it; a page without an address names none. Looked at in a
browser. Not tried: what the places that show links make of it – that needs a site they can reach.
