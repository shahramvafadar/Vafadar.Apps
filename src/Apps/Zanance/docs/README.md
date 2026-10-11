# Zanance – design documentation

The accepted product baseline is the owner's [*Zanance Product Specification*](spec/Zanance-Product-Specification.md)
(v1.1, 2026-09-26) and its [privacy matrix starter](spec/Zanance-Privacy-Matrix-Starter.md). Requirement identifiers used here and in tests (`FIN-01`, `TX-01`, `REC-13`, `AT-17`, …) are the
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
| [07 – Acceptance test plan](07-acceptance-test-plan.md) | Specification scenarios and subsequent feature/runtime verification |
| [08 – Release checklist](08-release-checklist.md) | Gates for the first public release |
| [Large-text review](quality/font-scaling-a11y03.md) | D-77/D-78 layouts, D-79 Settings publication/captions, D-80 growing actions, runtime evidence and remaining gates |
| [Enhancement ZEX](enhancements/2026-10-multi-unit-goals-insights/README.md) | Design package for multi-unit holdings, trackable goals and explainable insights – approved 2026-10-03, implementation in phases |
| [Specification](spec/Zanance-Product-Specification.md) | The owner's requirements; Section 31 = implementation status and deviations |

Documentation alignment D-88 removes superseded brand, cloud-sign-in, profile and pricing statements; original
review dates remain historical. Current remaining work lives in the canonical
[ZCR backlog](enhancements/2026-10-commercial-release/04-backlog.md), with physical/provider/iOS gates kept open.

Status words used everywhere: **Implemented – verified** (behaviour tested), **Implemented – unverified**,
**Planned**, **Not included**, **Unknown – needs verification**. Nothing is reported as done without evidence.

Complete growing transaction category choices: [runtime evidence](quality/entry-category-captions.md).

Retained occurrence correction and atomic payment linking: [engineering evidence](quality/occurrence-correction-writer.md) (D-133 / AT-135).

Atomic new planned payments: [engineering evidence](quality/atomic-occurrence-payments.md) (D-134 / AT-136).

Exact reviewed occurrence reopening: [engineering evidence](quality/atomic-occurrence-reopening.md) (D-135 / AT-137).

Occurrence command failures: [engineering evidence](quality/occurrence-command-feedback.md) (D-136 / AT-138).
