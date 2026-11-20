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

## What is built

**Step 19.7, 2026-11-20.**

* *The build.* The sources lie in `Vafadar.Dastan.Web/Scripts/src` as TypeScript. The build of the site
  checks them with the compiler of TypeScript (`tsc --noEmit`, strict) and makes them into one file,
  `wwwroot/app.js`, with esbuild; both come from `Scripts/package-lock.json` with exact versions
  (esbuild 0.28.2, MIT; TypeScript 7.0.2, Apache-2.0) and are tools of the build – nothing of them is in the
  file that is served. `npm ci` runs without the install scripts of packages (`Scripts/.npmrc`), and the
  tools are fetched into `obj/` beside the other files of a build. A build needs Node. The image of the
  container builds the file in a stage of its own from a Node image and hands it to the stage that
  publishes the site (`BuildDastanScripts=false` there); the image that runs holds no Node and no sources.
* *The one script* the site had is those sources now, unchanged in what it does: the worker for reading
  without a connection where the visitor said yes, what the device keeps, and the name of a chart brought
  into sight.
* *Data for the browser.* The profile of a node has an address: the address of its page and
  `/profile.json` – for an entity of a collection, for a person of a tree of an account, and for a person
  of a tree that is public. It is answered by the code that draws the page, from the same projection: the
  stage the visitor has read to and the audience of the site for an entity, the account and what it may see
  for a tree. What the page does not show is "not found" there too. The answer is words – a name, the
  facts as the page writes them, the picture – so the browser interprets nothing and hides nothing. It is
  sent with `Cache-Control: no-store`.
* *The first piece of the layer: the panel of a profile.* Where a list shows a node as a card, the card
  offers a look at the profile; a panel (the `dialog` of the browser) is filled from the address of the
  profile and leads on to the page. The offer is in the page hidden and is shown by the script: a reader
  without a script sees the list as before, with the name as the link.
* *Kept on a device.* The service worker keeps pages, and what a page is drawn with; an answer of an address
  of data is neither. A browser test opens the panel for somebody who reads without a connection and finds
  nothing of the data among what the device keeps.
* *The content security policy is unchanged:* everything from the site itself, nothing written into a page.

**Decided while building:**

* **The address of data is below the address of the page it belongs to,** not below one root for all data:
  the collection, the stage and the language are found for it exactly as for the page, by the same code.
* **The words come from the server.** A label like "Born" and "Not known" is in the answer, in the language
  of the page; the script has no texts of its own and so cannot say something the page would not.
* **What is said aloud is said by the script.** The panel lies closed in every page; that it waits, and that
  it failed, is announced by the script that shows it, so a page that never opens it announces nothing.
* **The file of the script is kept in the repository, and held to its sources.** The repository refuses a
  commit while a file of the sources is hidden by an ignore rule, and a file that is made is such a file;
  so `wwwroot/app.js` is kept, as the bytes the bundler wrote. Every build makes it again; on the build
  server a file that differs from what its sources make stops the build, so it cannot go stale unnoticed.

**Not built here:** the addresses of the data of a graph and of a timeline come with their controls (steps
19.8 and 19.9), each with its test against the page; the browser tests in CI are step 19.11.
