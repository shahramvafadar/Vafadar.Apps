# Zanance – design documentation

The accepted product baseline is the owner's [*Finance Product Specification*](spec/Finance-Product-Specification.md)
(v1.1, 2026-09-26) and its [privacy matrix starter](spec/Finance-Privacy-Matrix-Starter.md). Requirement identifiers used here and in tests (`FIN-01`, `TX-01`, `REC-13`, `AT-17`, …) are the
identifiers of that specification. These documents translate the specification into a design that fits this
repository and track what is actually implemented.

| Document | Content |
|---|---|
| [01 – Assessment and decision log](01-assessment-and-decisions.md) | Verified state of the repository, differences to the specification, decisions, risks |
| [02 – Domain design](02-domain-design.md) | Financial concepts, entities, occurrence lifecycle, calculation rules |
| [03 – UX design](03-ux-design.md) | Navigation, screen inventory, flows, states, visual system, accessibility |
| [04 – Phase 1 implementation plan](04-phase-1-plan.md) | Vertical slices with order, dependencies, acceptance, tests, migration risk |
| [05 – Phase 2 backlog](05-phase-2-backlog.md) | Future capabilities and their decision gates |
| [06 – Privacy matrix](06-privacy-matrix.md) | Data flows verified against the code |
| [07 – Acceptance test plan](07-acceptance-test-plan.md) | Mapping of the 68 acceptance scenarios to tests |
| [08 – Release checklist](08-release-checklist.md) | Gates for the first public release |
| [Specification](spec/Finance-Product-Specification.md) | The owner's requirements; Section 31 = implementation status and deviations |

Status words used everywhere: **Implemented – verified** (behaviour tested), **Implemented – unverified**,
**Planned**, **Not included**, **Unknown – needs verification**. Nothing is reported as done without evidence.
