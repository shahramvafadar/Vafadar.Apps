# Dastan – product specification

Dastan is a predominantly public visual discovery experience for ordinary curious people. Concise essential
information, meaningful relationships, story and time views and contextual images and videos invite visitors to follow
a thread. Optional detail and sources add depth. Later controlled personal and family uses share the core without
inheriting public disclosure.

These documents say **what** Dastan is and how it must behave. How and in which order it is built is in the
[delivery plan](../delivery-plan.md); what has been built and checked is in [acceptance](../acceptance.md).

## Documents

| Number | Document | Main question |
|---|---|---|
| 01 | [Vision and scope](01-product-vision-and-scope.md) | What is Dastan, for whom, and within what boundaries? |
| 02 | [Requirements and use cases](02-requirements-and-use-cases.md) | Which behaviors are required, and what would demonstrate them? |
| 03 | [Domain model and ontology](03-domain-model-and-ontology.md) | What do entity, role, relationship, claim and identity mean? |
| 04 | [Time and uncertainty](04-time-and-uncertainty.md) | How do uncertain, ancient, deep and narrative time work? |
| 05 | [Visual discovery and narrative](05-public-discovery-and-narrative.md) | How does a visitor understand essentials and follow a meaningful thread? |
| 06 | [Private spaces and consent](06-private-spaces-consent-and-privacy.md) | Who may see which information, under which permissions? |
| 07 | [Content, sources and governance](07-content-sources-and-governance.md) | How is information reviewed, published and corrected? |
| 08 | [Comparative research](08-comparative-research.md) | What can existing products teach us about discovery and presentation? |
| 09 | [Architecture and technology decisions](09-architecture-and-technology-decisions.md) | Which boundaries and experiments should guide implementation choices? |
| 10 | [Roadmap and backlog](10-roadmap-and-backlog.md) | How do we move from this foundation to a prototype and product? |
| 11 | [Dastan brand and positioning](11-brand-and-positioning.md) | How should the selected name be explained and presented? |
| 12 | [Decisions, questions and change log](12-decisions-questions-and-change-log.md) | What is accepted, proposed, open or superseded? |
| 13 | [Glossary and traceability](13-glossary-and-traceability.md) | What do the terms mean, and where is each direction covered? |
| 14 | [Reference scenarios](14-reference-scenarios.md) | What should happen in concrete cross-domain examples? |
| 15 | [Media and galleries](15-media-and-galleries.md) | How do images, videos and authorized shares connect to the same core? |
| 16 | [Discovery Pilot PRD](16-discovery-pilot-prd.md) | What is the proposed first usable public slice and its acceptance boundary? |
| 17 | [Flagship content and sources](17-flagship-content-and-source-plan.md) | Which actual records and media can exercise that slice? |
| 18 | [Discovery flows and design brief](18-discovery-flows-and-design-brief.md) | Which screens, transitions, states and review tasks make the experience concrete? |
| 19 | [Repository reconnaissance and spike plan](19-repository-reconnaissance-and-spike-plan.md) | What was observed in the repository before the work started? |
| 20 | [Implementation plan](20-implementation-plan.md) | What should the first runnable milestone implement and verify? |
| 21 | [Review and validation](21-review-and-validation.md) | What was corrected, what was actually checked, and what remains unverified? |

The screen navigation of document 18 as a diagram: [SVG](media/dastan-discovery-flow.svg),
[PNG](media/dastan-discovery-flow.png).

## Data and media

| Material | Location | Boundary |
|---|---|---|
| Editorial sample: 14 entities, 20 relations, 8 narrative events, 18 sources, 5 media records | [`content/samples/shahnameh-pilot.json`](../../content/samples/shahnameh-pilot.json) | Full editorial input including the ending of the story. Never a public payload and never under a web root |
| Original illustration, video, poster, captions and transcripts | [`content/media/`](../../content/media) | Present-day explanatory media, eligible for the local demonstration from reading level 2. The reviewer transcript is not served |
| Synthetic company, team and family fixtures with twelve review queries | [`test/Apps/Dastan/Fixtures/cross-domain-fixtures.json`](../../../../../test/Apps/Dastan/Fixtures/cross-domain-fixtures.json) | Test data only: they check that the model is not built for people alone. Not public subjects, not a permission system |

## Which document decides

1. A decision of the owner recorded in [document 12](12-decisions-questions-and-change-log.md) – the newest one first.
2. The [delivery plan](../delivery-plan.md) for the order of work, the projects and the technical defaults.
3. The topic documents for meaning and behavior: 03 and 04 for the model and time, 06 and 07 for what may be shown, 15
   and 17 for media and content, 16 and 18 for the first slice and its screens.
4. Documents 09, 19 and 20 describe the starting point before the repository work began. Where they differ from 1 or
   2 – the order of work, the number of projects, six interface languages – they are history, marked at their top.

A change of direction is recorded in document 12 and in the owning document in the same commit; identifiers
(`R-…`, `SC-…`, `D-…`, `M0-…`) are never reused.

## Import notes

The documents were written outside the repository as a reviewed handoff (version 0.6, 2026-11-15) and imported on
2026-11-15. What changed on import:

* Links to the sample, the fixtures and the media point to their places in the repository. No other sentence of the
  topic documents was changed, except the entries added to document 12, the notes at the top of 09, 19 and 20, and
  four phrases in documents 10, 16, 17 and 19 that now say "the implementing developer" and "no nested instruction
  file", as the repository's working rules require.
* In the sample, one reviewer-only path (`reviewerProvenancePath` of the family illustration) points to the new place
  of document 17. No record, identifier or value was changed.
* Not imported: the handoff's start guide and execution brief, its generated single-file reader, its archive manifest
  and its Python validation tools. Where a document mentions them (`python tools/validate_samples.py` in document 17,
  the validation results in document 21), it describes how the handoff was checked. In the repository the same
  checks, including the ten negative cases, are tests of `Vafadar.Dastan.Data` (phase 3 of the delivery plan).
* The documents no longer carry a pack version; their history from here is the repository's.
