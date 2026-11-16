# 0003. Pages are rendered on the server, without a live connection and without a script

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
