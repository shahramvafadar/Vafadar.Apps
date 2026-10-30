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

Actual goal work and scoped reminders: [engineering and native delivery evidence](quality/goal-work-reminders.md) (D-145 / AT-147). Goal-view contribution affordances and remaining ENT-02/03 bindings follow separately; current enforcement remains inactive.

Original goal views and reviewed actions: [source/native/APK evidence](quality/goal-work-views.md) (D-146 / AT-148). Remaining goal editor/contribution draft and other ENT-02/03 bindings remain open; registration stays inactive.

Current Android encrypted application delivery:
[protected storage evidence](quality/android-protected-storage.md). This distinguishes original main
Release evidence from the earlier independent probes and retains pending platform/device/CI gates.

Optional personal aggregate analysis and local credit prerequisites:
[original consent, private continuation and service qualification](enhancements/2026-10-commercial-release/15-ai-local-delivery.md).
This separates local prerequisites/manual sharing from unconfigured live models, credit sales and release gates.

Incremental Android manifest variants and retained binding/GC investigation:
[packaged on/off correction and scoped Samsung continuation](quality/android-manifest-overlay-incremental.md) (D-359).

Original Plans center source, publication and native command ownership (D-633):
[focused data/native evidence and exact qualification limits](quality/complete-original-plans-center.md).
Protected D-607 and broader release/platform acceptance remain separate.

Original template-list source, native command and deletion ownership (D-634):
[nine focused cases,72native states, exact expected source delta and cleanup limits](quality/complete-original-template-center.md).
Protected D-607 and broad release/platform acceptance remain separate.

Original category overview/editor source and accepted metadata ownership (D-638):
[focused and original native evidence](quality/complete-original-category-management.md), with physical adoption and broader release gates separate.

Original account editor source, default/receipt and native presentation ownership (D-639):
[focused failures, actual continuations and terminal limits](quality/complete-original-account-editor-source.md).
Physical adoption awaits the protected D-607 terminal gate.


Original asset-type editor source, Retry and accepted metadata ownership (D-642):
[Focused and original native evidence](quality/complete-original-asset-type-editor-source.md); physical adoption and broader gates remain separate.


Original asset-event native frame, pending Retry and accepted draft lifetime (D-643):
[Focused and original native evidence](quality/original-asset-event-frame-and-receipts.md), preserving D-607 and separate physical/platform gates.


Original holding-detail inline price drafts, retained tabs and accepted receipts (D-644):
[Focused and original native evidence](quality/original-holding-price-draft-ownership.md), preserving D-607 and separate physical/platform gates.


Original saved forecast comparison source, covered Retry and native delete receipts (D-645):
[Focused and original native evidence](quality/original-saved-comparison-source-and-delete.md), preserving D-607 and separate physical/platform gates.


Original forecast workspace, native prompts, tab display retention and Save receipts (D-646):
[Focused and original native evidence](quality/original-forecast-workspace-prompt-and-receipts.md), preserving D-607 and separate physical/platform gates.

Original report and KPI source/command handoff: [D-647 runtime evidence](quality/original-report-workspace-and-kpi-handoff.md). Physical adoption and release gates remain separate.

Original Settings source and current-profile reset: [D-648 runtime evidence](quality/original-settings-source-and-profile-reset.md). Physical adoption and release gates remain separate.

Original manual-rate workspace and reference navigation: [D-649 runtime evidence](quality/original-manual-rate-workspace.md). Physical adoption and release gates remain separate.

Original display-unit workspace and publication authority: [D-650 runtime evidence](quality/original-display-unit-workspace.md). Physical adoption and release gates remain separate.

Original reference workspace and offline access lifetime: [D-651 runtime evidence](quality/original-market-reference-workspace.md). Physical adoption and release gates remain separate.

Original accountant disclosure workspace: [D-652 runtime evidence](quality/original-accountant-disclosure-workspace.md). Physical adoption and release gates remain separate.

Original CSV workspace: [D-653 actual runtime evidence](quality/original-csv-workspace.md). Physical adoption and release gates remain separate.

Original restore lifecycle: [D-654 focused and actual runtime evidence](quality/original-backup-restore-lifecycle.md). General backup/provider and physical release gates remain separate.

Original portable backup creation: [D-655 evidence](quality/original-portable-backup-creation.md), separate from general discovery and real provider acceptance.

Original CI readback and account recovery: [D-656 evidence](quality/original-ci-readback-correction.md); historical failures, focused fixes and native receipt/source proof remain distinct from pushed-head CI.

Original backup workspace/destination ownership: [D-657 evidence](quality/original-backup-workspace.md), including fake-provider/native boundaries, exact restoration and remaining physical/provider gates.

Original profile catalogue and native command lifetime: [D-658 evidence](quality/original-profile-catalogue-lifetime.md), including real owned-emulator root retirement, one protected create/delete cycle and remaining acceptance gates.

Original six-kind resource workspace and one-use native parent return: [D-659 evidence](quality/original-active-resource-workspace.md), including accepted-write preservation across no-reset adoption and precise remaining gates.

Original personal synchronization workspace, current native consent/session boundaries and accepted-result feedback: [D-660 evidence](quality/original-personal-sync-workspace.md).


### Current D-661 household evidence

Original household connection/status and verified local metadata now use one captured
original-file worker transaction. Page/root/disclosure/literal-input/session/row/native
and file continuations retain independent ownership; returned primary acknowledgements
precede fallible follow-through, while unknown outcomes never receive a fabricated
receipt. Covered Retry reads the current original source without replaying accepted work.
Protected first-run completion changes only the oldest settings completion flag and its
UpdatedAt; a committed flag makes later Finish root-only, with no second Save or consent.

26 distinct selected cases (20 new, six affected existing), current strict Windows and
full signed offline Android384AOT/408ELF/1608staticLOAD pass. Actual original API/protected
Android owner setup and one accepted first-run commit with a subsequent Changed failure
passed without financial preparation. Same PID3873 retained 18 en/fa/de/Light/Dark/
360/412.190476/680dp error contexts, full source/metadata and one failure; XML text/native
bounds qualify these contexts, not the disqualified Activity-decor probe or glyph metrics.
Original Reload and root-only Finish reached actual Home with ledger/accounts empty;
20 source collections remained exact, normal foreground sync changed only LastSuccessAt.
OS/display/boot restored; fresh UID/41hash-matchedXML/AVD/API host/database/volume/two
networks retired, five original containers exact. Five standard Cleans passed; eight
supplementary scopes were rejected before process creation and remain frozen without
retry/subset/alternate/move/escalation/reuse. Used packages/evidence remain. No phone or
D607 command; three deadlines/consumed reboot/unread source protected. D660 sole CI result
is unknown after a preserved host marker/persistence failure, consumed and never queried
again. D661 receives one delayed exact-head check. WholeDB/key/security/stability/speed/
A11Y/iOS/genuine identity/provider/Store/release remain separate; overall goal active.

[Full evidence](quality/original-household-workspace.md)
