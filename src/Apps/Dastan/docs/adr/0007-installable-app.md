# 0007. The installable app, its service worker and the one script of the pages

- Status: **Accepted** – implemented and verified on 2026-11-16; an installation itself was not observed (see below)
- Date: 2026-11-16
- Deciders: Shahram Vafadar
- Governing documents: 09 (PWA installation and offline use), 20 (step D), the delivery plan (phase 6); M0-11, M0-12
- Related: [ADR 0003](0003-server-rendered-pages.md) (pages without a script), [ADR 0006](0006-media-delivery.md)
  (what must never be kept), [ADR 0002](0002-one-projection.md)

## Context

Dastan is delivered as a website that can be installed. M0 is online-first: the story is read with a connection.
What has to be decided is what "installable" needs, what happens without a connection, and how much script that
costs – the pages have none so far, and what a visitor may see is decided per request on the server, so anything a
browser keeps is a risk: a kept page or file would outlive the reading position it was allowed for.

## Decision

1. **One manifest for the whole site** (`/manifest.webmanifest`): name and short name "Dastan", `id`, `start_url`
   and `scope` "/", `display: standalone`, the colours of the light surface and of the icon. The start is the
   address without a language; the site answers it in the visitor's language like any other first visit. There is
   one app, not one per language.
2. **Icons are the provisional drawing** (`icon.svg`) as PNG of 192 and 512 px, a second drawing with the same
   paths inside the middle of a full square for platforms that cut their own shape (`maskable`, and the icon for a
   home screen of 180 px). They are original and provisional like the palette (ADR 0004).
3. **A service worker with one job: the page for "no connection".** It keeps that page and the four files it is
   made of (two style sheets, the font, the icon) and nothing else. A page is always asked for from the network;
   only when that fails the visitor gets the kept page, at the address they asked for. The kept files themselves
   come from the network while there is one. Everything else – above all the files of a collection (`/media`) and
   the forms – is not touched by the worker. It never puts anything into its store while it runs.
4. **The page for "no connection" is the same for everybody**: no name of a story, nothing of a visitor, both
   languages of the site on one page (the worker does not know which one the visitor reads), and one link that asks
   for the same address again.
5. **A new worker takes over at once.** No page depends on what an old worker kept, so nothing has to wait for
   open pages; the name of the store changes with what is kept, and an older store is removed.
6. **The pages get one script** (`app.js`, a file of the site, loaded after the page): it registers the worker.
   No page needs it to show its content or to be used – with scripts turned off the journey is the same, and
   without the worker the browser shows its own page when there is no connection. There is still no script
   written into a page and no handler in the markup; the content security policy stays `default-src 'self'`.
7. **No banner for a lost connection and no install button of our own.** A page that is open stays readable when
   the connection is lost – it does not hold a connection – and the next page says what is wrong. Installing is
   offered by the browser's own menu. Both would need more script for little; ADR 0003 named them as candidates,
   and they are not taken.
8. **A demonstration is not for search engines**: while the site is not the public one, every answer carries
   `X-Robots-Tag: noindex, nofollow` and every page the same as a `meta` element.
9. **A form that is refused leads to a page that says so** (`/refused`, by "see other"), in the visitor's language:
   that the page was open too long or had changed, and that nothing was changed. This closes the point ADR 0003
   left for this phase.

## Consequences

* What a visitor may see stays a decision of the server for every request. The worker cannot answer for it.
* Reading without a connection is not possible, by decision. Offering it later means deciding what may be kept for
  whom and for how long – for a public story whose every part is public that is simple, for a reading position or a
  private space it is not. It needs its own record.
* The store of the worker has to be renamed when one of the kept files changes its meaning; a changed style sheet is
  picked up anyway while there is a connection.

## Verification

* `InstallableAppTests` (answers of the site): the manifest and every icon it lists, with their sizes read from the
  files; the start in the visitor's language; the links in the head of every page; one script per page, a file of
  the site, and no handler in the markup; the files the worker keeps and that it names nothing of a collection; the
  page for "no connection"; the request not to be indexed, and that the public site does not make it.
* `InstalledAppTests` (Chromium 1.64.1 in the container of the Compose project `test`): the worker takes over the
  whole site and its store holds exactly the five files; without a connection the image shown a moment ago is not
  answered, a page asked for is the page for "no connection" in both languages and styled like the site, and after
  the connection is back "Try again" gets the page asked for; with scripts turned off the journey, the reading
  position and the search work; Chromium reads the manifest without an error and names no obstacle to installing
  except that a window of the tests is a private one; thirteen views in both languages and both appearances break
  no rule of WCAG 2.1 A and AA and no best practice that axe-core 4.14 can decide.
* `WorkerUpdateTests` (the same browser): when the site answers with a newer worker, it takes over a page that
  stays open, removes the store of the old one and still shows the page for "no connection" from its own.
* `StatesTests`, `FailureTests`: a refused form, while developing and outside it.

**Not verified.** An installation was not carried out: the browser of the tests is a private window, which cannot
install, and no other browser was driven. No phone, no iOS or Android, no screen reader. What a tool cannot decide about accessibility – whether a text alternative is
a good one, whether the order of reading makes sense when spoken – was not judged by a person with a screen reader.
