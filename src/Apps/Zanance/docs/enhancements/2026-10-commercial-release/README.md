# Enhancement ZCR – commercial release (Free, Plus, Pro)

**Status reconciled 2026-10-27 (D-595):** personal sync D-292 is implemented and locally qualified,
not publicly accepted or released. Do not rebuild the delivered local framework to close external gates. Consolidated owner
approvals and scope exceptions are recorded in [06-open-decisions.md](06-open-decisions.md). Subsequent
maintenance and ready local sections have been delivered; their current states and remaining gates live in
[04-backlog.md](04-backlog.md). D-69 authorizes continuous delivery of ready planned work; open product/licence,
provider, spending and publication decisions remain owner gates ([05-delivery-plan.md](05-delivery-plan.md)).

Decision: D-61 in [`../../01-assessment-and-decisions.md`](../../01-assessment-and-decisions.md). It supersedes the
earlier monetization model of the specification (MON-02 one-time Pro purchase, MON-03 boundary after usage) and the
"Pro purchase" row of the Phase 2B gates; MON-01, MON-04 … MON-08 stay valid. Earlier ids are kept, never reused.

| Document | Content |
|---|---|
| [01-current-state.md](01-current-state.md) | Gap map: every capability and security item with evidence, tests, UI and status |
| [02-plans-and-pricing.md](02-plans-and-pricing.md) | Free/Plus/Pro matrix, counting rules, prices, Plus Lifetime, campaigns, trial, downgrade |
| [03-architecture.md](03-architecture.md) | Entitlements and billing lifecycle, local encryption and keys, sync, shared space, add-ons, operations, costs |
| [04-backlog.md](04-backlog.md) | **The canonical backlog** of all remaining work (ids `ZCR-*`) |
| [05-delivery-plan.md](05-delivery-plan.md) | Waves, release gates, mandatory acceptance tests, section template, proposed first section, handoff |
| [06-open-decisions.md](06-open-decisions.md) | Consolidated owner decisions, current authority and remaining external gates |
| [07-backend-cost-and-decision.md](07-backend-cost-and-decision.md) | Costed OD-02 proposal, complete backend delivery boundary and explicit owner gates (D-269) |
| [08-backend-cost-model.json](08-backend-cost-model.json) | Historical provider cost inputs; no spending approval |
| [09-current-machine-hosting.md](09-current-machine-hosting.md) | Approved current-machine Docker scope and remaining deployment checks |
| [10-bank-import-research.md](10-bank-import-research.md) | Initial German bank priorities and primary-source export research |
| [18-native-storage-profile-delivery.md](18-native-storage-profile-delivery.md) | Current reduced native source/package/main-app qualification and explicit remaining security/platform gates |

Ids: `ZCR-<area>-nn` for work items (areas GOV, SEC, LOC, IMP, QA, A11Y, ENT, BIL, ID, SYNC, SHR, TAX, AI, BANK, FX,
LANG, REL) and `OD-nn` for open decisions. Open stories of enhancement ZEX continue under the ZCR ids named in the
backlog.


## D-388 - Decision-ready ECB reference FX candidate

[Source/terms/coverage and complete integration proposal](25-ecb-reference-source-decision.md) with
[machine-readable cost/scope model](26-ecb-reference-source-model.json). This does not activate a
provider or change current app/service behavior; owner OD-14 selection remains open.

D-389 records the owner-selected complete daily ECB integration. The source choice and all proposed
usage conditions are approved; the default service still needs explicit source configuration and
public activation remains separate. [Qualification](../../quality/ecb-reference-integration.md).
