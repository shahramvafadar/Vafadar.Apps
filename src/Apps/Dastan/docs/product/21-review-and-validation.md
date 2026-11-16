# 21 — Review Findings and Validation Evidence

**Version:** 0.6  
**Date:** 2026-11-15  
**Scope:** Complete review of the nineteen-document v0.5 foundation and its supplied data/media; a bounded source-code reconnaissance; an actionable implementation handoff.

## 1. Outcome

The foundation is ready to guide **M0 implementation**, with explicit limits on what the current evidence establishes. The review corrected semantic and integration risks, made local-demo eligibility machine-readable, added an original loadable still image, and separated the first runnable milestone from later R0 public-release gates.

No Dastan application was implemented, deployed, benchmarked or tested on a device during this review. A document reader, content diagram, video and sample validator are not the product. The implementation instructions deliberately require the next delivery to go beyond documentation into working routes and verified behavior.

## 2. Material findings and resolutions

| ID | Finding | Resolution and owning material |
|---|---|---|
| REV-01 | A remaining domain paragraph said the product name was undecided | Corrected to Dastan; naming choice, literary rationale and open identity/domain work remain separate. 03, 11, 12, 13 |
| REV-02 | The old next-step sequence could end after another prototype/plan without a runnable slice | Introduced finite M0 work and twelve observable acceptance conditions; retained R0 publication gates. 10, 16, 18, 20 |
| REV-03 | `participantIds` mixed actors, mentioned people, places and objects | Replaced with typed `entityAssociations` and presence semantics; a mention does not establish attendance or continuous presence. 03, 17, sample format 0.2 |
| REV-04 | Some relationship scope existed only in prose | Added machine-readable scope and directionality. A revelation's reading position is not a relationship's factual start date. 17 and sample |
| REV-05 | Editorial, local-demo and production-publication status were too implicit | Added explicit publication contract and per-media render eligibility; raw editorial data and synthetic fixtures cannot be served wholesale. 07, 17, 20 |
| REV-06 | Museum URLs could be mistaken for a functioning image gallery | Kept candidate-image restrictions and added a separate original bundled illustration. Real image/video viewing is required for M0. 15–18, 20 |
| REV-07 | An activity period was labelled like an unpositioned duration; some dates confused era/calendar | Clarified positioned periods, duration quantities, continuity, Gregorian versus CE and separate endpoint uncertainty. 04 and cross-domain fixtures |
| REV-08 | Institutional-search inclusion could disclose a private affiliation even if the detail panel hid it | Require permission for the affiliation/time context before inclusion, counts or suggestions. 06 |
| REV-09 | Tag approval and tag visibility lacked an explicit fixture-level policy distinction | Added tag access policy, independent audience checks and revocation expectations. Clarified replay/cancellation states for later consent workflows. 06 and fixtures |
| REV-10 | The comparison site's edition attribution could be misapplied to Dastan's own transcription | Explicitly separated provider attribution from the unverified critical print edition behind the selected online witness. 08, 17 |
| REV-11 | Shared localization was a plausible but unsafe direct web reuse | Source review found singleton/shared translator state and process-wide culture changes. Require request/circuit-local state and a two-browser-context isolation test. 09, 19, 20 |
| REV-12 | Existing financial date and UI-resource conventions needed explicit applicability decisions | Keep historical/narrative time outside bounded date types; preserve finance behavior. Apply current six-language shell-resource rules while declaring content-language availability separately. 09, 19, 20 |
| REV-13 | RTL presentation could be implemented by indiscriminately mirroring time/graph semantics | Separate text direction from chronology and relationship direction; require labelled axes, readable mixed-script text and accessible alternatives. 05, 18, 20 |
| REV-14 | A correct-looking document pack did not itself establish sample or archive integrity | Added a dependency-free sample validator, negative input mutations, link/ID/language/HTML checks, a content manifest and reproducible reader/archive tools. The checks establish input integrity, not application correctness |
| REV-15 | Local media provenance/transcript links could lead from a permitted image/video to full editorial spoilers | Separated reviewer-only provenance paths from UI source IDs and added a dedicated level-2-safe plain-text transcript; rejected both leak variants in validation. 17 and sample |

## 3. Review coverage

All nineteen prior topic documents were read and assessed. Documents 02 and 14 retain their earlier revision headers because their content and stable identifiers did not need changes. Seventeen earlier topic documents were updated; documents 20 and 21 are new.

| Prior document | Review focus | Result |
|---|---|---|
| 01 | General-public audience, common core, naming and milestone framing | Updated |
| 02 | Requirements, acceptance examples and scope retention | Reviewed; retained 65 requirements |
| 03 | Identity, entities, relationship scope, event associations and typed domains | Updated |
| 04 | Precision, uncertainty, periods, epochs, BCE/CE, deep time and narratives | Updated |
| 05 | Visual discovery, concise essentials, context and RTL semantics | Updated |
| 06 | Social consent, invitations, institutional discovery, tags and revocation | Updated |
| 07 | Claims, sources, review/publication, imported text and media safety | Updated |
| 08 | Comparative lessons and targeted current primary-source checks | Updated; original observation limits preserved |
| 09 | Architecture boundaries, localization reuse, temporal types and finite decisions | Updated |
| 10 | Delivery sequence, bounded milestone and retained long-term backlog | Updated |
| 11 | Selected Dastan name, accurate rationale and provisional identity | Updated |
| 12 | Accepted versus proposed decisions, current questions and revision history | Updated |
| 13 | Shared vocabulary and stable requirement/scenario/implementation mappings | Updated |
| 14 | Nineteen reference scenarios and their expected semantic outcomes | Reviewed; retained |
| 15 | Media dates, galleries, derivatives, access and actual viewing acceptance | Updated |
| 16 | M0/R0 distinction, release scope, delivery order and future gates | Updated |
| 17 | Story records, text witness, sources, spoilers, media and demo contract | Updated |
| 18 | Six flows, eight screens, states, accessibility and review tasks | Updated |
| 19 | Refreshed repository evidence and bounded technical checks | Expanded |

The review also covered both JSON files, the original video/captions/transcript metadata, navigation diagram, new illustration, package index, implementation brief and cross-document links. The two sample formats now have explicit 0.2 schema/format versions; the legacy dataset identity and all previous record IDs remain stable.

## 4. Checks actually performed

The package's machine-readable `validation-results.json` records final assembly results. Its scope is limited to the supplied artifacts.

- Parsed both JSON files and checked IDs, references, relationship scope/directionality, event association meaning, temporal conventions, publication flags, media eligibility and synthetic fixture expectations.
- Executed the sample validator from an unrelated current directory, confirming it resolves its inputs relative to the package.
- Rejected ten deliberately invalid in-memory variants: dangling references, premature parentage disclosure, invented attendance, fabricated narrative dates, uncleared image eligibility, era/calendar confusion, reversed period bounds an incorrect revocation expectation, a reviewer-document source link and a reviewer-document transcript link.
- Checked maintained Markdown links, stable requirement/scenario references, English documentation and integrated HTML anchors/structure. Checked the reader script's syntax and used the packaged renderer to rebuild it.
- Visually inspected the new original family diagram: four labelled entities and the three intended parent relationships, with source/context labels. The existing video retains its prior representative-frame inspection; this revision did not claim a new end-to-end product player test.
- Built the complete archive, checked its entries and hashes, and verified the extracted package using its supplied tools. The generated HTML is documentation, not a browser-verified application interface.

The sample currently contains **14 entities, 20 relations, 8 narrative events, 18 source records and 5 media records**. The three synthetic cross-domain fixtures contain **12 review queries**. Five media records mean three external museum references plus one original local video and one original local image. Candidate references do not count as completed image acquisition or visual verification.

## 5. Source and repository evidence limits

Targeted primary-source rechecks covered relevant Ganjoor passages, two Met object records, temporal conventions and selected comparable-product/documentation pages. These checks support the particular statements cited in their owning documents. They are not an exhaustive new fact-check of every linked page, a specialist review of the entire Shahnameh or proof of an independently established critical edition.

The current `main` reference remained `957a33c05595746464ca80fb91153bb3bdf8dc14` when checked. Repository reconnaissance included a complete returned path inventory and selected relevant instruction, architecture, build, CI, localization and test files. The large historical instruction file was not read as a complete chronological audit of all unrelated finance work. Document 19 records the exact evidence and limits.

No code build, application regression suite, performance measurement, physical installation, private-data security test or general-user study was performed. No source repository files or remote state were changed by this review. Future M0/R0 acceptance conditions remain unperformed until the implementing developer records real results.

## 6. Remaining decisions that do not block M0

The exact final wordmark/logo, domain, public-launch market, final flagship scope, hosting provider, production persistence, editorial organization and monetization remain open. The implementation recommendation is provisional, with authority to make routine reversible choices and record the result. An explicit local content projection, original media and a narrow initial UI allow progress without those future decisions.

Before public R0, complete editorial approval, required media/translation/accessibility review, correction and withdrawal behavior, deployment/operations planning, human task observations and actual selected device/browser evidence. Before personal/social features, exercise real authorization, consent, invitation and revocation rules independently of the public-story demonstration.
