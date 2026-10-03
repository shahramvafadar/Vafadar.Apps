# Zanance enhancement ZEX – multi-unit holdings, trackable goals and explainable insights

**Status: Approved by the owner on 2026-10-03; implementation in phases (see [06](06-implementation-backlog.md#phases)).** Former status: awaiting owner approval. This package is a review, design and planning result only. No source code,
test, migration, dependency, project file, CI or app setting was changed for it. Implementation may start only after
the owner approves this design and gives a separate implementation goal (see [08](08-decisions-and-approval.md) and
[09](09-implementation-goal-draft.md)).

## Scope

The enhancement answers four questions reliably: *what money and holdings do I have; which commitments are ahead; how
much is left in my budget; how far is my goal and – under which assumption – when can I reach it.* It covers all
packages E01–E09 of the owner's requirements document, including the historical wealth report and forecast
snapshots (E08). Priorities P0/P1/P2 only order the work; nothing in scope is dropped.

Out of scope (unchanged product policy, not removed): bank connections, real payments, family sync or sharing, a
backend, cloud AI, online market prices or rates, analytics SDKs, ads, Pro purchases, subscriptions and pricing
changes. Cloud backup is not sync, a local profile is not family access control, the UI lock is not database
encryption, and Simple/Advanced is not Free/Pro.

## Inputs

| Input | Where | How it was used |
|---|---|---|
| Owner's requirements document *Zanance – multi-unit holdings, trackable goals and explainable reports*, v0.1, 2026-10-02 (Persian) | Owner's document set `Vafadar.Apps.Docs/Zanance/Zanance_Product_Requirements_FA_v0.1.md`, kept outside this repository like the other owner copies | Business requirements MC01–MC12, AS01–AS16, GO01–GO15, K01–K14, the Simple/Advanced matrix, E01–E09, AT01–AT40. Its competitor findings (S1–S25, U01–U16) are decision input, not statistical evidence; they were not re-researched |
| Owner's design and backlog instruction for this stage (Persian), 2026-10-02 | Same document set (design and backlog instruction, Persian) | Defines this stage: review, design, backlog, stop for approval |
| The Zanance product specification v1.1 and design documents 01–08 | [`../../spec`](../../spec/Zanance-Product-Specification.md), [`../../`](../../README.md) | Existing rules, identifiers and implementation status |
| The code and tests at the base commit | Repository | Current state and gaps ([01](01-current-state-and-gaps.md)) |

**Base commit:** `67410b447ea7723c825f597f95f7e05a03046af9` on `main` (2026-10-02), clean working tree apart from this
package. The requirements document was written against `b43f69ad2863d0ab934b564427ccbdfcc3427222`; the three commits
in between (`2660b10`, `1290ba4`, `67410b4`) change Windows dialog fonts, the minimum window size, the Insights tab
label, the "?" touch target and `AGENTS.md` – none of them changes a capability assessed here.

## Documents

| Document | Content |
|---|---|
| [01 – Current state and gaps](01-current-state-and-gaps.md) | Every requirement as Existing / Extend / New / Blocked / Needs verification, with code, UI and test evidence |
| [02 – Product and domain design](02-product-and-domain-design.md) | Behaviour, concepts, data model, calculations, lifecycles, migration, backup and golden examples |
| [03 – UI and UX design](03-ui-ux-design.md) | Screen inventory, the ten key flows with wireframes and interaction notes, states, microcopy in fa/de/en, accessibility, platform notes |
| [04 – KPI and report catalog](04-kpi-and-report-catalog.md) | K01–K14 in full, the liquidity headroom contract, the six report packages, numeric examples |
| [05 – Simple / Advanced matrix](05-simple-advanced-matrix.md) | All 30 areas and every existing feature, the single display policy and the mode switch rules |
| [06 – Implementation backlog](06-implementation-backlog.md) | **The single reference backlog of this enhancement**: epics, stories, tasks, dependencies, acceptance criteria |
| [07 – Acceptance and validation](07-acceptance-and-validation.md) | AT01–AT40 mapped to stories, definitions and tests with expected numbers; regression and release validation |
| [08 – Decisions and approval](08-decisions-and-approval.md) | Accepted direction, design proposals needing approval, risks, approval gate |
| [09 – Implementation goal draft](09-implementation-goal-draft.md) | The goal text drafted before approval – superseded by the phase plan in 06 |
| [wireframes/](wireframes/index.html) | Offline HTML wireframes with fictitious data (not screenshots of an implementation) |

The project's main backlog ([05 – Phase 2 backlog](../../05-phase-2-backlog.md)) has one entry that points here; this
package's [06](06-implementation-backlog.md) is the only place where the work items and their state are tracked.

## Identifiers

The requirements document uses short identifiers that would collide with the specification's (`AT-01` …, `FX-*`).
In this repository they always carry the prefix **`ZEX-`**:

| Source identifier | Repository identifier | Meaning |
|---|---|---|
| D01–D11 | ZEX-D01–ZEX-D11 | Direction decisions of the requirements document ([08](08-decisions-and-approval.md)) |
| MC01–MC12 | ZEX-MC01–ZEX-MC12 | Currencies, accounts, quick entry |
| AS01–AS16 | ZEX-AS01–ZEX-AS16 | Quantity holdings |
| GO01–GO15 | ZEX-GO01–ZEX-GO15 | Goals and contribution |
| K01–K14 | ZEX-K01–ZEX-K14 | In-app KPIs |
| E01–E09 | ZEX-E01–ZEX-E09 | Work packages (epics) |
| AT01–AT40 | ZEX-AT01–ZEX-AT40 | Business acceptance scenarios |
| – | ZEX-P01 … | Design proposals of this package that need the owner's approval |
| – | ZEX-S0101 … | Stories in the backlog (`S` + epic number + running number) |
| – | ZEX-SA01–ZEX-SA30 | Areas of the Simple/Advanced matrix |
| – | ZEX-R01 … | Report packages |
| – | ZEX-X01 … | Additional acceptance cases beyond AT01–AT40 |

Existing identifiers (`D-xx` decisions, `AT-xx` scenarios, specification requirement ids) are never reused or
renumbered. Where a story touches an existing requirement, the existing id is named next to the new one.

## Status words

As everywhere in the Zanance documentation: **Implemented – verified**, **Implemented – unverified**, **Planned**,
**Not included**, **Unknown – needs verification**. In this package every new work item is **Designed – awaiting
approval**; nothing here is implemented.
