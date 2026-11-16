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
| [0004](0004-design-tokens-and-fonts.md) | Design tokens, type scale and fonts | Accepted; palette and fonts provisional |
| [0005](0005-graph-renderer.md) | The renderer of the connections graph: SVG drawn by the server | Accepted (D-035) |
| [0006](0006-media-delivery.md) | A media file reaches a browser through one route that asks the projection | Accepted |

Related repository decisions: [0011](../../../../../docs/adr/0011-languages-per-app.md) (languages per app),
[0012](../../../../../docs/adr/0012-temporal-model.md) (stated time), [0013](../../../../../docs/adr/0013-web-library.md) (the web library).
