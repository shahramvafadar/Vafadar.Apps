# 0019. Pages from the server, and a layer of TypeScript in the browser

* Status: Accepted (D-079); takes the place of "without a script" in [0003](0003-server-rendered-pages.md) and changes [0007](0007-installable-app.md) and [0008](0008-charts.md)
* Date: 2026-11-19
* Step: 19.7 of the [delivery plan](../delivery-plan.md)

## Context

Every page was drawn by the server and worked without a script; opening a branch of a tree or moving along a
timeline asked the server for another page. That is right for reading and wrong for exploring: a graph has
to be moved, zoomed, opened and looked into where the visitor is.

## Decision

* **The first answer stays a complete page.** Text, links, lists and a still drawing come from the server as
  before, so that a search engine, a reader without a script and a screen reader get everything, and an
  address can be passed on.
* **Blazor stays static.** No circuit to the server and no .NET in the browser: moving a graph must not wait
  for a server, and a public site must not keep a connection per visitor.
* **A layer of TypeScript** takes over where a page says so: custom elements (`<dastan-graph>`,
  `<dastan-timeline>`, a panel for a profile) that find the still content of the page and replace it once
  they run. Sources lie in the project of the site; one bundler with a pinned version builds them with the
  site and in its image; the result is served by the site with names that change with the content. Nothing
  comes from another domain, and the content security policy stays as strict as the controls allow – what a
  control needs is written down there, one rule at a time.
* **Data for the browser** comes from addresses of the site that answer with exactly what the page of the
  same visitor would show: the same projection, the same stage read to, the same audiences and consent.
  Nothing is sent and then hidden. Changes go through the same forms and tokens as before.
* **Kept on a device** only after the visitor said yes (D-073), and only public pages – the layer changes
  nothing of that: what belongs to an account, and any answer of these addresses for somebody who is
  signed in, is never kept by the service worker.
* **Controls of Syncfusion** may be used in the browser (D-079). Their licence key is part of the script, as
  the vendor's model has it, and so readable by everybody; it comes from the secrets of the build and is
  never in the repository.

## Consequences

The build needs Node. Browser tests are no longer an extra: what moves is tested where it moves, in CI
(step 19.11). The rule "no script on a page" leaves `AGENTS.md`; "a page is complete without its script"
takes its place.
