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
| [0014](0014-visitor-accounts.md) | Accounts of visitors: a third store beside the site, a name and a password, recovery codes instead of mail, closing with thirty days to think again | Accepted (D-048 to D-050, D-055); not run in a container |
| [0012](0012-visitor-reports.md) | Reports of visitors: a folder between the site and the tool – the statement, the words and the day, nothing about the visitor | Accepted |
| [0015](0015-postgresql.md) | One PostgreSQL database with a schema for each part, and rules the database itself keeps | Accepted (D-076); step 19.3 |
| [0016](0016-identity-and-mail.md) | Accounts on ASP.NET Core Identity with a confirmed mail address; mail through an outbox | Accepted (D-077); step 19.4 |
| [0017](0017-node-profile.md) | Every node has a profile: a picture and the facts its kind needs | Accepted (D-078); step 19.5 |
| [0018](0018-relationships.md) | Relationships are records of their own, and two people can have several | Accepted (D-078); step 19.6 |
| [0019](0019-interactive-layer.md) | Pages from the server, and a layer of TypeScript in the browser | Accepted (D-079); step 19.7 |
| [0020](0020-graph.md) | The graph: a contract of Dastan on Syncfusion EJ2 Diagram | Accepted (D-079); step 19.8 |
| [0021](0021-timeline.md) | The timeline is a control of Dastan's own, with a calendar, deep time and the order of a telling | Accepted (D-079); step 19.9 |
| [0022](0022-locks.md) | Every act that looks before it writes holds the locks of what it looks at, in one order | Accepted; step 20.2 |
| [0023](0023-institutions.md) | A place somebody was at is an entity of a collection when they choose one, and a time there is a stretch of its own | Accepted; step 20.4 |
| [0024](0024-parts.md) | The frame of the site – the main ways, the account, language and appearance behind one control, a bar at the foot of a phone – and the parts every page is made of | Accepted for the structure; step 20.5 |
| [0026](0026-logo.md) | The logo: one geometry reconstructed from the approved picture, every asset made from it by one script, drawn by the site itself | Accepted; D-082 |
| [0027](0027-gallery-themes.md) | A theme of a gallery is a record of its collection: an entity of the type `Theme` that media items name | Accepted; D-083 |
| [0025](0025-exploring.md) | One state of exploring – what is explored, the view, what is chosen, what each view was told – read from the address and known to no renderer | Accepted; step 20.6 |
| [0028](0028-hypothetical-placement.md) | A hypothetical placement in time is a dated statement marked with its basis and confidence, shown apart on the page of its entity and drawn by no view of time | Accepted; F1 of the content plan |

Related repository decisions: [0011](../../../../../docs/adr/0011-languages-per-app.md) (languages per app),
[0012](../../../../../docs/adr/0012-temporal-model.md) (stated time), [0013](../../../../../docs/adr/0013-web-library.md) (the web library).
