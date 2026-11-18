# Dastan – architecture decision records

Technical decisions of the app. A decision is **Proposed** while it is being built and becomes **Accepted** when it is
implemented and verified; a later decision that changes one supersedes it with a new record. Decisions that concern
the whole repository are in [`docs/adr`](../../../../../docs/adr/README.md); the owner's product decisions are in
[document 12](../product/12-decisions-questions-and-change-log.md).

| # | Decision | Status |
|---|---|---|
| [0001](0001-domain-read-model.md) | A typed, immutable read model of one collection | Accepted |
| [0002](0002-one-projection.md) | One projection decides what a visitor sees | Proposed |
| [0003](0003-server-rendered-pages.md) | Pages are rendered on the server, without a live connection and without a script | Accepted |
| [0004](0004-design-tokens-and-fonts.md) | Design tokens, type scale and fonts | Accepted; palette and Latin font provisional |
| [0005](0005-graph-renderer.md) | The renderer of the connections graph: SVG drawn by the server | Accepted (D-035) |
| [0006](0006-media-delivery.md) | A media file reaches a browser through one route that asks the projection | Accepted |
| [0007](0007-installable-app.md) | The installable app, a service worker that only shows "no connection", one script | Accepted; installation not observed |
| [0008](0008-charts.md) | Charts: the timeline and the family tree are drawn by Dastan itself; Syncfusion for the tools of editors; a chart has the whole page and is opened at its four sides | Accepted |
| [0009](0009-published-revision.md) | What everybody may see: only a published revision – numbered, with reviewed statements, reviewed texts and media allowed for the public | Accepted |
| [0010](0010-running-the-site.md) | Running the site: one container without content, a mounted revision, a proxy it is told to believe | Accepted; no host used |
| [0011](0011-editors-store.md) | The store of the editors: the records of a collection with their history, in SQLite, beside the file – the public site never reads it | Accepted (D-045) |
| [0013](0013-private-space-rules.md) | The rules of private spaces as a library, checked by the acceptance examples of document 06, before anything is kept | Accepted for the accepted rules; nothing reaches it |
| [0012](0012-visitor-reports.md) | Reports of visitors: a folder between the site and the tool – the statement, the words and the day, nothing about the visitor | Accepted |

Related repository decisions: [0011](../../../../../docs/adr/0011-languages-per-app.md) (languages per app),
[0012](../../../../../docs/adr/0012-temporal-model.md) (stated time), [0013](../../../../../docs/adr/0013-web-library.md) (the web library).
