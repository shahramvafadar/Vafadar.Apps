# 04 – Canonical backlog (commercial release)

**This file is the only place where the state of remaining Zanance work is tracked from 2026-10-07 on.**
`05-phase-2-backlog.md` keeps the finished Phase 2A history and links here; the ZEX backlog keeps the finished ZEX
stories, and its open stories continue here under the ids given below. Waves and sections:
[05-delivery-plan.md](05-delivery-plan.md); owner decisions: [06-open-decisions.md](06-open-decisions.md).

States: Proposed · Ready (decisions made, may start after owner approval of the section) · In progress · Done
(implemented, tested, documented, pushed) · Blocked (by an owner action) · Deferred by owner. Priority: P1 (blocks
the release), P2 (in the release), P3 (later milestone). Size: S/M/L.

## Overview

**Execution priority override (owner, 2026-10-19, D-386):** remaining approved feature development
precedes dedicated Android ANR/crash investigation (QA-06) and Android accessibility completion
(A11Y-01/A11Y-03). These remain unfinished final qualification requirements, not accepted or removed
from release scope. Preserve failures encountered in necessary feature checks. The connected low-end
Samsung's possible hardware/health limitations remain unverified; do not attribute faults to it without
evidence or repeat unchanged successful checks. Follow the updated delivery plan over historical
priority/order descriptions below.

| Id | Title | Wave | Prio | Size | State | Depends on |
|---|---|---|---|---|---|---|
| ZCR-GOV-01 | Audit, plan matrix, architecture, backlog and delivery plan | 0 | P1 | M | Done | – |
| ZCR-GOV-02 | Refresh spec §31, AGENTS.md §12 and outdated statements | 0/2 | P2 | S | Done (D-88; external confirmation/acceptance still open) | GOV-01 |
| ZCR-SEC-01 | Threat model and encryption decision record + feasibility proof | 1 | P1 | M | In progress (D-264 Android/Windows official Community integration locally verified; iOS/external gates remain) | OD-04 approved |
| ZCR-SEC-02 | Encrypted database (SQLite encryption) behind the existing data layer | 1 | P1 | L | In progress (D-264 Android/Windows main encryption locally verified; iOS adoption and device gates remain) | SEC-01 |
| ZCR-SEC-03 | Data key in Android Keystore / iOS Keychain / Windows DPAPI | 1 | P1 | M | In progress (D-264 Android Keystore/Windows DPAPI per-generation keys locally verified; D-483 actual original-adapter generation/recovered security level1, cold/binding/tamper/authorized retirement on Samsung qualified in a separate UID; Main-key attestation, authentication, iOS/other device gates remain) | SEC-01 |
| ZCR-SEC-04 | Optional app password, biometrics changes, attempt limiting | 1 | P1 | M | In progress (D-266 success-only device unlock; biometric/platform acceptance remains) | SEC-03, OD-04 |
| ZCR-SEC-05 | Crash-safe migration of existing plaintext databases (all profiles) | 1 | P1 | M | In progress (D-263 Android all-profile migration/lifecycle and native interruption cases delivered; broader fault/platform acceptance remains) | SEC-02, SEC-03 |
| ZCR-SEC-06 | Recovery: recovery key / backup path, password change without data loss | 1 | P1 | M | In progress (D-264 Android/Windows optional recovery and explicit portable recovery locally verified; iOS/device acceptance remain) | SEC-04, OD-04 approved |
| ZCR-SEC-07 | Final OS device/cloud backup rules (Android `dataExtractionRules`, iOS exclusion) | 1 | P1 | S | In progress (D-263 final coupled Android policy delivered; iOS exclusion/OEM acceptance remain) | OD-10 approved |
| ZCR-SEC-08 | Encrypted safety copies before restore | 1 | P2 | S | In progress (D-264 local Android/Windows; D-312 actual Samsung safety recovery and profile isolation verified; broader device/iOS gates remain) | SEC-03 |
| ZCR-SEC-10 | Owner-approved PIN, screenshot choice and ownership clarification (D-63) | Maintenance | P1 | M | Done | Owner request 2026-10-08 |
| ZCR-SEC-09 | Network/SDK review of online builds; privacy texts | 1/3 | P2 | S | Done (current baseline; repeat for future SDKs) | – |
| ZCR-LOC-01 | Release bug list from the owner's phone tests | 2 | P1 | M | Proposed | owner reports |
| ZCR-LOC-02 | Goal contribution reminders (ZEX-S0306) | 2 | P2 | M | Done (local/emulator; D-488 physical lifecycle and D-491 saved-calendar/grouped-target/unavailable-source/missed-date queues qualified; actual timed delivery/lock/profile/Doze/reboot and broader platform gates remain) | – |
| ZCR-LOC-03 | Optional period review reminder (ZEX-S0610) | 2 | P2 | S | Done (local/emulator; D-489 physical original three-calendar native queues/progress/privacy qualified; actual timed delivery/lock/profile/Doze/reboot and broader platform gates remain) | – |
| ZCR-LOC-04 | Aggregated entries: import overlap handling (ZEX-S0611) | 2 | P2 | M | Done (local/emulator, D-72; phone/iOS acceptance open) | – |
| ZCR-LOC-05 | "Not a tax calculation" help on sale results (ZEX-S0404) | 2 | P3 | S | Done (D-268 original sale help, six-language copy and store drafts locally verified; publication/device gates remain) | D-69 continuous delivery |
| ZCR-LOC-06 | Recurring payments easier to find (repeat option in the entry form or a clearer entry point) | 2 | P2 | S | Done (local/native, D-111; phone/iOS acceptance open) | – |
| ZCR-LOC-07 | Currencies of the target markets (ISO list, minor digits) | 2/8 | P3 | S | Target coverage delivered (D-273); additional jurisdictions/years remain | official currency sources |
| ZCR-LOC-08 | More holiday regions with source, validity years and "uncertain" state | 8 | P3 | M | Target coverage delivered (D-273); additional jurisdictions/years remain | official jurisdiction/year sources |
| ZCR-LOC-09 | Automatic device-encrypted local backup; separate portable export | 2 | P2 | M | Done (D-281 local/native; D-327 scoped Samsung creation/restore/isolation and readiness fix; broader device/iOS/release gates remain) | SEC-08 |
| ZCR-LOC-13 | Owner-approved plan and debt UX (D-65) | Maintenance | P1 | M | Done (local; AT-72 device gate remains) | owner approval 2026-10-08 |
| ZCR-LOC-15 | Independent saved filters after report navigation (D-109 / AT-114) | Maintenance | P1 | S | Done (local/native D-109; D-323 physical saved-filter/report-scope/cold acceptance; broader device/iOS gates remain) | D-69 ready-defect delivery |
| ZCR-LOC-12 | Owner-requested receipt total detection and review (D-64) | Maintenance | P1 | M | Done (local; device/corpus gate remains QA-05) | owner request 2026-10-08 |
| ZCR-LOC-11 | Owner-requested backup, onboarding and About corrections (D-62) | 2 | P2 | S | Done | owner approval 2026-10-08 |
| ZCR-LOC-10 | Copy follow-ups D-53/D-54 and listing texts naming three languages | 2 | P3 | S | Done (D-268 browser/account copy and six-language drafts; real OAuth/publication remain open) | D-69 continuous delivery |
| ZCR-IMP-01 | CSV presets for other apps and bank exports, preview, duplicates, undo | 2 | P2 | M | In progress: D-297 documented StarMoney ordinary-cashflow preset; D-381 physical interpreted paging/stale-mapping review; D-405 original own/mapped financial snapshot guard; D-406 physical opening-boundary/cold Undo and retained-tab Refresh fix qualified; D-490 original external ledger/holding group Undo dependencies corrected with15 selected managed cases and six native original-page outcomes; other bank/relationship variants require original fixtures | original export specifications/samples |
| ZCR-IMP-02 | OFX/QIF/CAMT only on demand with real samples | 8 | P3 | M | Proposed | IMP-01 |
| ZCR-QA-01 | Device acceptance runs (AT-01…61, Q-02) on the owner's phone | 2 | P1 | M | In progress (D-484 physical original plan zero/correction/dirty draft, debt reference/estimate and cold preservation qualified; D-312 recovery/profile; D-314/315 protected workload and main upgrade; D-316 planned-work lifecycle; D-317 native CSV fixed-ID/duplicate/cold Undo; D-318 first-plan native publication fixed; D-319 debt/lent and six-row cold-upgrade lifecycle; D-323 report/saved-filter/cold/PDF chooser; D-326 transfer summary correction; D-327 automatic-copy lifecycle/readiness; D-328 accountant ZIP receiving/content; D-329 original en/fa/de PDF contents and continued-table fix; D-330 own-format producer/consumer/refund/transfer/cold Undo; D-361 original en/fa/de display/Back and advanced goal choices/dirty discard, exact preference/Home restoration and one native root; D-365 original changed account/debt/monthly disclosures in 18 normal-scale Samsung cases with draft/data/preference preservation; D-401 occurrence override/skip/unskip, manual link/keep and generated delete-on-reopen with cold/Main restoration; D-402 prior-period/cross-account refunds, excess refusal/same-draft remainder and cold public allocation with Main restoration; D-403 original unprotected portable fresh independent reinstall/first-run restore/cold/whole-export comparison with Main restoration; D-404 card purchase/payment/positive credit and dirty archive/unarchive with two ended plans/default/history/cold and Main restoration; D-406 opening-boundary CSV/cold Undo; D-407 guarded split/reallocate/cold/join and original Main restoration; D-411 original holding purchase/fee refund/refused and valid correction/sale/cold and fee-action fix with Main restoration; D-417 original type/price/history Undo/archive/cold; D-422 original place/move/dirty discard/cold with Main restoration; D-423 original independent native selection qualified without a product workaround; D-457 controlled physical currency typing/selection/discard complete; D-458 original balance-goal lifecycle; D-459 original earmark/quantity goals; D-460 original no/zero/exact/exceeded/category-only budgets, refund/cold/native deletion with Main restoration; D-461 protected budget action lifetime; D-463 original rollover/weekly/fortnight source and corrected period wording; D-464 original envelopes/goal earmark/overspending and Flex fixed/nonmonthly/flexible split with cold/public restoration; D-465 original account/currency/pay-cycle scopes and corrected Simple retained-account note; D-466 original incomplete forecast/amount assumption/snapshot/cold/native deletion with Main restoration; D-486 legacy missing-destination forecast correction and six native original pages with ordinary physical forecast preservation; D-472 original reconciliation/input invalidation/reason/Cancel/one adjustment/cold with Main restoration; broader phone acceptance remains) | – |
| ZCR-QA-02 | Cloud backup with real OAuth clients (AT-59), restore on another device | 2 | P1 | M | Blocked (client ids) | – |
| ZCR-QA-03 | App test project: app lock, profiles, bulk operations, onboarding, widget, theme | 2 | P2 | M | Done (local/emulator, D-73; physical bulk D-393; remaining device/iOS acceptance open) | – |
| ZCR-QA-04 | Asset-account income/expense confirmation test (ZEX-S0408) | 2 | P2 | S | Done (local/emulator D-74; bounded physical D-394; broader device/iOS gates open) | QA-03 |
| ZCR-QA-05 | Receipt/PDF reading quality on devices, all six languages | 2 | P2 | M | In progress (D-324 six source-language digital PDFs; D-325 physical Latin image OCR/EXIF/refusal and French accent fix; D-473 one original physical Save/PDF attachment/cold stored reread/Cancel with Main restoration; real corpus/camera/scanned/iOS remain) | sample receipts |
| ZCR-QA-06 | Performance: cold start, search, migration with the reference and a 10× data set | 2 | P2 | M | In progress (D-322 retained physical workloads; D-350 rejects unproven date decoder; D-351 indexes aggregation; D-352 completes worker/atomic virtualized reimbursement-page publication and non-overlapping empty flow; D-353 removes unnecessary template reload teardown; D-354 rejects slower complete-row JSON transport and retains scalar production reader; D-358 retains an independent binding-finalizer fault; D-360 releases owned retired Android root fragments and passes the previously failing 36-group goal/root sequence; D-362 isolates original managed startup and rejects model overlap without benefit; D-363 guards complete first-settings reads with measured whole-managed-publication reductions; D-377 preserves complete original empty-tag semantics; D-383 bounds original recent-row selection on the existing worker with complete native/data/packet equality and no-reset Main upgrades, preserving actual native crashes and making no whole-Home speedup claim; D-384 leaves combined serial-major/64MiB nursery unaccepted after longer dispatcher pauses; D-390 rejects the supported new-bridge candidate after all twelve corresponding dispatcher gaps lengthen and restores ordinary production; D-480 releases merged native validation listeners in four forms and D-481 qualifies their remaining original callers; D-485 repairs reproduced toolbar observation across native roots; D-482 fixes the separately reproduced detached-root callback fault with five new native cases, three affected normal checks and a final Samsung blank-amount rejection/Cancel; actual Main/startup ANRs, two-second objective, cold/low-memory and broader device/iOS gates remain) | – |
| ZCR-QA-07 | Android/iOS parity (notifications, widget, file picker, lock, share, licences); existing Windows development checks | 2 | P2 | M | D-474/D-475 bounded physical notification action/timed-route and D-479 original physical widget actions are qualified; remaining lock/device/platform acceptance stays open. Mac currently unavailable per owner on22 October2026; iOS verification/runs remain pending and independent Android work continues. Windows commercial delivery is outside current scope; D-344 native Canvas root-change AV remains a separate development limitation | platform access/signing |
| ZCR-A11Y-01 | TalkBack pass on Android | 2 | P1 | M | In progress (D-174/175 local names; D-348 service-preserving observer captures audio/focus; D-349 rejects injected navigation as gesture acceptance; D-367 excludes decorative glyph descendants and qualifies24 original scenes/36 actual reader activations in six Settings-bound contexts; D-391 qualifies nine actual en/fa/de speech-request text/locale/role packets and six original open/cancel activations with exact restoration; audible intelligibility/pronunciation and remaining workflows/platform/device acceptance open) | – |
| ZCR-A11Y-02 | Narrator and keyboard pass on Windows | 2 | P2 | S | Proposed | – |
| ZCR-A11Y-03 | Font scaling to 200 %, 360/412 px, both themes, RTL | 2 | P1 | M | In progress (D-77 through D-91 Home/rows/action docks, Settings/reopened languages/drafts, actions/dates/amounts, growing child headers/live Back visible Insights destinations and complete budget figures/signed formatting and visible period choices and complete selectable tags and state-matched detail actions and complete onboarding restore alternatives; D-94 complete Home customization names/targets; D-96 complete account type/status descriptions; D-97 complete debt entry action; D-98 single readable modal headers; D-99 visible complete plan validation; D-100 complete transaction-field feedback; D-101 retained visible existing transfer fees in Simple; D-102 complete settlement fields/action; D-103 complete occurrence actions/field feedback; D-107 complete growing native selected/popup picker captions; D-108 complete report-scope captions/Clear targets and reachable result viewport locally delivered; D-302 complete searchable currency controls and constrained transaction/goal/rate fields; D-308 complete native saved-filter names/actions; D-311 full monetary packets in report/forecast tables; D-335 complete More titles/explanations in offline/sync editions; D-339 complete category/composition report names and amount viewports; D-340 complete default RowTitle and Flex bill-group captions; D-342 complete optional Home category legends and total; D-343 complete shared plan-row values; D-344 complete ledger/account monetary rows and group headers; D-352 growing reimbursement identity, independent amount viewport and separate empty flow; D-345 real emulator OS200%/412dp on six monetary surfaces; D-346 real OS report/forecast/legend geometry and Flex captions, with actual Mono crash/ANR retained; D-347 guarded ordinary foreground Home-category/plan evidence at OS200%/412dp; D-355 actual OS Settings selected captions; D-356 six original editor states across36 real OS200%/412dp contexts, growing debt disclosures/default glyph/chip stroke allocation and targeted Windows matrices; D-358 preserves complete measured choice surfaces and qualifies goal/budget/settlement contexts with exact OS restoration; D-364 qualifies real OS200%/360dp editor actions and original wizard footers, fixes redundant constructor theme publication, preserves native input endpoints and rejects wrong-page images; D-366 seven financial/Settings groups in 42 real OS200%/360dp cases with exact input/source/restoration receipts; D-438 adds96 actual OS200% physical/rate input observations; D-439 adds288 original monetary guide/error observations with zero accepted command replay and exact restoration; remaining changed layouts, other controls and platform acceptance open) | – |
| ZCR-A11Y-04 | VoiceOver pass on iOS | 2 | P2 | S | Blocked (iOS) | QA-07 |
| ZCR-ENT-01 | Plan policy and quota model in Core (no UI, no store) | 3 | P1 | M | Implemented (D-117 / approved OD-03; 76 policy cases; no app enforcement) | OD-03 approved |
| ZCR-ENT-02 | Enforcement in services: create, import, restore, template, deep link, widget, OCR | 3 | P1 | L | Implemented locally (D-173 bounded review closed; enforcement inactive; activation/native release gates remain) | ENT-01 |
| ZCR-ENT-03 | Downgrade flow: choose active items, read-only rest | 3 | P1 | M | Implemented locally (D-173 bounded review closed; actual shared-server lifecycle and native release acceptance remain) | ENT-02 |
| ZCR-ENT-04 | Plan screen, limit messages that keep the form, "continue with Free" | 3/6 | P1 | M | Locally delivered through D-310 (comparison, retained write outcomes and verified purchase validity/renewal/history UI); real store/provider, Mac/iOS and device acceptance remain | ENT-02, translations |
| ZCR-ENT-05 | Existing users and test builds: migration to plans (no limits before approval) | 3 | P1 | S | Current test-build/upgrade policy verified (D-277); customer activation/announcement remain owner gates | OD-03 |
| ZCR-BIL-01 | Play Billing/StoreKit 2 catalog and identity-bound future-web prerequisites | 6 | P1 | L | In progress (D-295 local Android/identity-bound cross-store service delivered; native StoreKit2/Mac and genuine store acceptance remain; web checkout/Windows commercial excluded) | ENT-01, OD-08 approved |
| ZCR-BIL-02 | Verification service, notifications, idempotency, lifecycle states | 6 | P1 | L | In progress (D-295 canonical verification/notifications/durable lifecycle qualified locally; genuine provider/public acceptance remains) | BIL-01, ID-01 |
| ZCR-BIL-03 | Secure entitlement cache, offline window, clock changes | 6 | P1 | M | In progress (D-295 signed private cache/clock/original-request/recovery qualified locally and Android storage checked; iOS/device acceptance remains) | BIL-02 |
| ZCR-BIL-04 | Plus Lifetime, Lifetime-owner Pro discount, campaigns, trial | 6 | P1 | M | In progress (D-295 Lifetime/trial/discount/replacement policy and actual-price review qualified locally; genuine store eligibility/sandbox matrix remains) | BIL-02, OD-06, OD-07 |
| ZCR-BIL-05 | Sandbox test matrix; production activation (owner only) | 6 | P1 | M | In progress (D-295 local fixture/native qualification only; real store sandbox and owner production activation remain unchecked) | BIL-01..04 |
| ZCR-ID-01 | Optional identity, backend foundation, operations | 3 | P1 | L | D-293 local deployment/restore/renewal; D-313 outbound tunnel origin and gateway continuity locally qualified; owner tunnel/DNS, real mail and public acceptance remain | OD-02 approved; public deployment/identity acceptance |
| ZCR-SYNC-01 | Change log, outbox, versions, tombstones | 4 | P1 | L | Implemented and locally qualified D-292: original service/protected Windows/Android HTTP, durable staging, complete graph and native UI pass; hosted CI separate | ID-01 |
| ZCR-SYNC-02 | Conflicts, idempotent posting, restore into a synced profile | 4 | P1 | L | Implemented and locally qualified D-292: explicit complete conflicts/groups/attachments, idempotency and safe restore pass; external platform gates separate | SYNC-01 |
| ZCR-SYNC-03 | Sync status UI and device management | 4 | P2 | M | Implemented and locally qualified D-292: original UI/device revocation and 36 Windows contexts plus retained Android native evidence; public hosting follows | SYNC-01 |
| ZCR-SHR-01 | Shared space, invitations, roles, server-side authorization | 5 | P1 | L | D-294 local delivery qualified: protected E2EE workspace/source adoption, membership/roles/devices and original multi-client finance; public/platform acceptance separate | SYNC-02, SHR-04 |
| ZCR-SHR-02 | Cost sharing between people (F2-SHARE-05), household totals | 5 | P2 | M | D-294 local delivery qualified: mapped complete costs/shares/refunds/repayments, real native two-client conflict decisions and no duplicate cash posting | SHR-01 |
| ZCR-SHR-03 | Audit trail, revocation, ownership transfer, end of subscription | 5 | P1 | M | D-294 local delivery qualified: consent-bound transfer, audit, revocation, expiry/privacy retirement and latest-marker-aware full restoration; no identity-erasure claim | SHR-01, OD-05 approved |
| ZCR-SHR-04 | E2EE from the first shared delivery, including membership/key/recovery lifecycle | 5 | P1 | L | D-294 local delivery qualified: pinned membership/epoch/recovery lifecycle, original native persistence, bounded historical reads and portable scrubbing; external audit/device/iOS separate | OD-09 approved |
| ZCR-TAX-01 | Germany/current-year tax research, expert and official examples; country expansion without ETA | 0→7 | P1 | M | D-296 official-source boundary research; expert/rule acceptance open | OD-01 approved |
| ZCR-TAX-02 | Level 1: Germany data/document export for an accountant (Plus) | 7 | P1 | M | Locally delivered D-296; D-328 default/selected-account and D-380 explicit notes/known-PDF opt-in/redaction qualified physically; wider device/iOS/expert acceptance separate | TAX-01 |
| ZCR-TAX-03 | Level 2: versioned rule set and report for the first country/year | 7 | P2 | L | Proposed | TAX-01, expert |
| ZCR-AI-01 | Local AI credit ledger, provider interface, consent, minimal data | 8 | P3 | L | In progress: D-298 local aggregate/native-consent/private-client/service-credit prerequisites qualified; live provider/pricing/credit sales remain gated; see15-ai-local-delivery.md | OD-13 |
| ZCR-BANK-01 | German-first bank/file/provider evaluation, then USA and other targets | 8 | P3 | M | In progress: D-307 first discovery pass across fourteen targets and provider comparison delivered; exact consumer specimens and later live-provider acceptance remain | OD-14, IMP-01 |
| ZCR-FX-01 | Rates/prices provider boundary, caching, provenance, manual fallback | 8 | P3 | M | In progress: D-299 app/data/service and protected personal/shared source prerequisites; D-389 owner-approved daily ECB integration for all30 target-currency pairs; daily source/native/package evidence in quality/ecb-reference-integration.md; other price sources, public deployment and platform/store acceptance remain gated | OD-14 |
| ZCR-LANG-01 | Additional languages beyond the existing six | Deferred | P2 | L | Outside current delivery by owner decision; no current release dependency or ETA | later scope decision |
| ZCR-REL-01 | Final QA, store metadata, staged rollout | 10 | P1 | M | Proposed | all P1 |

## Item definitions

Each item lists: value · scope / out of scope · files · acceptance · migration · texts · external cost · done when.
Items of later waves are refined into sections before they start.

### ZCR-GOV-01 – Audit and plan (Done 2026-10-07)
Value: one honest picture of the product and one plan. Scope: documents in this folder, D-61, superseded markers.
Out: any code, data model, store or service change. Done: documents pushed; owner chooses the first section.

### ZCR-GOV-02 – Outdated statements (Done 2026-10-09, D-88)
Value: documents match reality. Scope: current brand/proprietary ownership, source-implemented sign-in/profiles,
optional backup protection/portable preferences, D-61 plan design, platform and SDK/privacy boundaries, dated ZEX/ZCR
review statements and current Section 31 evidence. Source/manifest checks and a focused documentation assertion audit
verify the corrections listed in [01-current-state.md §5](01-current-state.md#5-documentation-alignment-audit-d-88).
No app code, build configuration, financial data, policy or entitlement changes; unchanged D-87 tests/APK remain the
local engineering baseline. OD-11 configuration/Mac/provider acceptance remains open, and documents now qualify it
rather than claiming either a missing implementation or a completed external setup.

### ZCR-SEC-01 – Threat model, decision record, feasibility proof
Value: database encryption that fits .NET 10/MAUI on all platforms, decided on evidence. Scope: threat model (lost
unlocked phone, device/cloud OS backup, malware with file access, shared PC); files in scope (database, WAL/SHM,
temporary snapshots, exports, safety copies, caches); library options (SQLCipher community via
`SQLitePCLRaw.bundle_e_sqlcipher`, Zetetic commercial builds, SQLite Encryption Extension) with licence, cost, AOT and
platform support; a throwaway test that creates, re-opens and rejects a wrong key with the chosen provider on Windows
and in an Android emulator run; KDF and key-wrapping design; ADR. Out: wiring into the app, any migration of user
data. Files: `docs/adr/0010-database-encryption.md`, a test project or a branch-local
spike that is not merged into the app. Acceptance: the proof runs on Windows tests and the emulator; the ADR names
the library, licence, key flow and rollback. Cost: possibly a paid licence → owner approval before adoption. Done: ADR
approved by the owner.

### ZCR-SEC-02 – Encrypted database
Value: data unreadable without the key. Scope: `Vafadar.Data` connection creation with a key provider, EF Core
model unchanged, profiles, backups unaffected (portable). Migration: via SEC-05. Tests: open/write/read, wrong key,
WAL/SHM never plaintext, integrity check, performance before/after. Done: new installs encrypted on all supported
platforms, measured start-up time recorded.

### ZCR-SEC-03 – Key storage
Scope: random data key wrapped by Android Keystore, iOS Keychain, Windows DPAPI; per profile. Tests: key
creation/loss/rotation, profile switch. Done: no key or secret in source, keys survive app updates, documented
behaviour on device restore (OS backup does not carry the key → see SEC-06/SEC-07).

### ZCR-SEC-04 – App password and biometrics
Scope: optional app password (Argon2id or PBKDF2 with versioned parameters), biometric unlock as an option, handling
of biometric enrolment changes, attempt limiting with growing delay, lock on return (existing 30 s). Owner decision D-266 removes the unavailable-device unlock fallback: only successful device authentication permits entry. Texts: new strings (owner translations). Done: tests for all transitions;
device check on Android and Windows.

### ZCR-SEC-05 – Migration of existing databases
Scope: per profile, with free-space check, copy-encrypt-verify-switch, recovery after a crash at any step, old
plaintext file removed (no claim of secure erasure). Tests: interrupted migration at each step, low disk, large
database. Done: upgrade test from the current release passes.

### ZCR-SEC-06 – Recovery
Scope: per OD-04 (proposal: recovery key shown once + password-protected backups); password change re-wraps the key
without re-encrypting data. Done: forgotten-password and lost-device scenarios documented and tested where possible.

### ZCR-SEC-07 – OS backup rules
Scope per OD-10: Android `dataExtractionRules`/`fullBackupContent` and iOS `isExcludedFromBackup` for the database,
safety copies and caches, or keep OS backup and rely on SEC-02. Tests: manifest/resource checks; `adb shell bmgr`
backup run on the emulator. Done: behaviour documented in the privacy matrix and the help text.

### ZCR-SEC-08 – Encrypted safety copies
Scope: safety copies encrypted with a device-bound key (they never leave the device) or the backup password when
given; retention unchanged. Done: tests; help text "SafetyCopy" reviewed (texts from the owner).

### ZCR-SEC-09 – Online builds review
Scope: list every SDK and permission of builds with cloud backup, billing or sync (ML Kit logging, MSAL, Google auth);
privacy matrix and store declarations per build. Done: matrix updated, no "nothing is sent" claim on online builds.

### ZCR-LOC-01 – Release bug list
Scope: bugs found on the owner's phone; each bug its own fix with a test. Done when the owner's list is empty or
deferred by the owner.

### ZCR-LOC-15 - Independent saved filters after report navigation

Value: the same named filter returns the same complete results regardless of the previously opened report.
Scope: remove only transient report currency/account-set/confirmed-only/scope-note restrictions during native saved-
filter application, preserving every persisted filter field and existing Clear behavior. No schema, new strings,
cost, provider, security, quota or financial-write change. AT-114 compares the actual saved action/results before
and after three independent fictitious report scopes, complete stored rows and exact original-file restoration.
Final runtime/build/test/APK checks: [saved-filter-report-scope.md](../../quality/saved-filter-report-scope.md).
Physical-device/iOS and product acceptance remain separate.

### ZCR-LOC-02 – Goal contribution reminders (from ZEX-S0306)
Scope delivered in D-70: opt-in on goal contribution dates at 09:00 device-local time using the saved rule/calendar,
generic lock-screen text and existing notification permission/details settings. Twelve planner and seven coordinator/
SQLite cases pass; 1,153 main tests pass. en/fa/de light/dark 360/412/wide editor/help reviewed. Android API 36 x86_64
checks native pending requests across DST, real generic notification delivery and tap to the goal, pause/resume and
saved opt-out after process restart, without financial writes. Complete signed Release APK installs/starts on the
isolated AVD; Debug fixture types are absent from Release. ARM64 phone, Doze/reboot and iOS acceptance remain open.
No schema, SDK or permission added; no real data touched.

### ZCR-LOC-03 – Period review reminder (from ZEX-S0610)
Delivered in D-71: optional profile choice in Settings, off by default, generic by default. Schedule 09:00 device-local
after the financial month closes, following the review calendar and month start day. Skip empty/finished periods and
missed times; taps open the current review without posting money or marking steps. Additive migration, compiled model
and portable-backup/old-schema checks; nine planner and six real-store/coordinator cases, 1,168 main tests pass.
Running-app en/fa/de light/dark 360/412/wide settings/help and API 36 x86_64 native scheduling/delivery/tap/persisted
opt-out are checked. Complete signed Release APK inspected/installed; phone/Doze/reboot/iOS gates remain open.

### ZCR-LOC-04 – Aggregated entries and import (from ZEX-S0611)
Delivered in D-72: explicit per-aggregate link/keep-both choices with dates, amounts and remaining totals; unset by
default. Match account/kind/category and inclusive covered dates. Existing aggregates subtract only new accepted
details; a detail cannot reduce two aggregates. Recheck the preview atomically and require refresh when stale.
Durable profile-local metadata journals preserve original identity, attachment ownership and Undo across restart or
database backup. Later edits, dependent imports/refunds and missing identities reject unsafe Undo without writes.
AT-79 adds eight Core and nineteen SQLite cases; 1,195 main tests pass. Running en/fa/de light/dark 360/412/wide
screens are reviewed. Isolated API 36 x86_64 UI import keeps the 412 EUR sample total at 395 + 17 and restart skips
the same six IDs. Phone/iOS acceptance remains open. No SDK, permission or commercial limits added.

### ZCR-LOC-05 – Sale results help (from ZEX-S0404)
Delivered D-268: original result-row help in six existing languages, explanation/example and native open/close checks. Recorded results are not tax calculations; no tax logic or financial posting was added.

### ZCR-LOC-06 – Recurring payments easier to find
Value: the owner could not find recurring payments. OD-12 was approved on 2026-10-10: Repeat in a new transaction
opens a prefilled Plan without recording a transaction. D-111 keeps the existing Home Add plan target, supplies
six translated action/hint/help/error sets and preserves unchanged plan rules. Final native financial/presentation
matrices, strict builds, normal signed Release Plan Save/cleanup, original-data readbacks and complete signed APK
pass: [entry-repeat-draft.md](../../quality/entry-repeat-draft.md). Phone/iOS and other external gates remain open.

### ZCR-LOC-07/08 – Currencies and holidays
Scope: currencies and holiday regions of the confirmed target markets, each holiday set with source and validity
years, an "uncertain" state for lunar or announced holidays; never presented as bank business days.

### ZCR-LOC-09 – Automatic local backup
Scope: owner approved device-key-encrypted automatic local copies on 2026-10-15, with separate portable exports
using an optional password. Never store a backup password or silently create a readable automatic file. Check
daily/weekly/off on app-start/resume, maintain a separate successful timestamp per profile, coalesce concurrent
requests and keep independent automatic/pre-restore retention. Discover current-profile copies and restore only
after original preview/authentication/confirmation. Explain that device/key loss requires a portable export.
Done: original-source tests, native encryption/restore/retention evidence, six-language UI checks, strict builds,
complete signed APK and documentation in one finished batch. D-281 local/native delivery is complete; physical-device/iOS/hosted acceptance remains separate. See ../../quality/automatic-local-backup.md.

### ZCR-LOC-11 – Backup, first run and About (D-62)

Approved by the owner on 2026-10-08 before further commercial development. Scope: optional cloud backup password,
theme/experience in onboarding, restore before creating the first account, and a compact notices link on About.
No password persistence, new schema or SDK; no commercial enforcement. Local verification covers protected and
unprotected restores, preserved profile settings and no duplicate account, all tests, both builds, and Windows
layouts in en/fa/de, light/dark, 360/412 px and wide. Provider sign-in with real OAuth clients and physical-device
gates remain open. Evidence and policy: D-62, spec §31, acceptance plan and architecture/data-and-backup.

### ZCR-LOC-10 – Copy follow-ups
Delivered D-268: truthful browser-step completion in all six languages, explicit English paying/receiving account wording, and all six store drafts aligned with current backup/encryption policy. Actual OAuth and publication remain separate.

### ZCR-IMP-01 – Moving in from other apps and banks
Scope: CSV presets (mapping, date/number formats) for common apps and bank exports chosen from real samples; preview,
duplicate detection, undo (existing batch undo). Out: OFX/QIF/CAMT (IMP-02), bank connection. Done: tests with the
sample files (fictitious data only in the repository).

### ZCR-QA-01…07 – Verification
QA-01: device runs per `07-acceptance-test-plan.md` with the owner (results recorded there). QA-02: AT-59 with real
clients. QA-03: new `Vafadar.Zanance.App.Tests` (view-model level, no MAUI UI) for app lock, profiles, bulk
operations, onboarding, theme, widget routing. QA-04: test for asset-account confirmations. QA-05: receipts per
language on devices. QA-06: measured times, recorded in the decision log. QA-07: parity table per platform.

### ZCR-A11Y-01…04 – Accessibility pass
As planned in `05-phase-2-backlog.md` (accessibility pass): TalkBack, Narrator/keyboard, 200 % font, VoiceOver; every
finding fixed with a test where possible; checklist per platform in `07-acceptance-test-plan.md`.

### ZCR-ENT-01 – Plan policy and quotas
Value: one testable source of truth for plans. Scope: Core types for plan, purchase kind, entitlement, permission and
quota; counting rules of [02-plans-and-pricing.md §2](02-plans-and-pricing.md#counting-rules-precise-before-any-enforcement);
pure functions with tests for every plan × purchase kind × Simple/Advanced × personal/shared combination. Out: UI,
store, enforcement. Done: policy tests; no behaviour change in the app.

### ZCR-ENT-02 – Enforcement
Scope: checks in the stores/services that create accounts, budgets, goals, plans, profiles, holdings; importers;
restore (never refused, read-only above quota); templates; deep links; widget; OCR. Concurrency: two creations at once
cannot exceed a quota. Done: tests for every entry point; still disabled by a release switch until ENT-05/BIL.

### ZCR-ENT-03 – Downgrade
Scope: choosing active items, read-only rest, corrections always allowed, automation limits. Texts: owner. Done: the
scenarios of [02 §5](02-plans-and-pricing.md#5-end-of-a-plan-downgrade-and-quotas) as tests.

### ZCR-ENT-04 – Plan screen and limit messages
Scope: comparison of three plans, monthly/yearly, separate Plus Lifetime, current plan, renewal date, manage, restore,
errors, "continue with Free"; limit messages that keep the form; no locks on every control; palette roles unchanged.
Texts: owner (all six languages). Done: snapshots 360/412, both themes, RTL, large font.

### ZCR-ENT-05 – Existing users and test builds
Scope: data and recovery kept, changes announced, no limits before owner approval, test access not a permanent
entitlement. Done: decision recorded; upgrade test.

### ZCR-BIL-01…05 – Billing
Scope per [03-architecture.md §1](03-architecture.md#1-entitlements-and-quotas). Cost: store accounts (owner), the
verification service. Done for BIL-05: the full sandbox matrix passes (purchase, renewal, restore, refund, revoke,
pending, error, grace, hold, auto-renew off, expired, recovered, duplicate notifications, clock change, Lifetime with
and after Pro, campaigns, trial). Production activation only by the owner.

### ZCR-ID-01 – Identity and backend
Scope: optional sign-in for Pro services, modular backend, deployment, migrations, backups and restore drills,
secrets, rate limiting, observability without financial content, incident response, support without ledger access.
Cost: hosting (OD-02). Done: service running in a test environment with restore drill passed.

### ZCR-SYNC-01…03, ZCR-SHR-01…04
Scope per [03-architecture.md §3–§4](03-architecture.md#3-personal-sync-pro). Acceptance includes: two devices settle
one occurrence → one entry; conflicting amount edits → conflict shown; Viewer and removed member cannot write; no
report, file, search or API reveals an unshared account; revoked member's offline edits rejected.

### ZCR-TAX-01…03
Approved initial scope: Germany, current tax year, employees and freelancers; accountant export first (Plus).
Official calculations require expert review of the versioned country/year rules; filing is outside this scope.
Add other target countries individually. Show unsupported countries as being added without a promised date,
never as selectable tax rules. Tax availability is independent of existing currency, display and holiday choices.

### ZCR-AI-01, ZCR-BANK-01, ZCR-FX-01
Approved now: local AI provider boundary, explicit consent, minimal data and credit accounting; bank CSV/import
and German-first research; FX/assets provider boundary, cache, source, staleness and manual fallback. Live
providers, licences, costs and bank connections remain separate decisions. Research Germany, then the USA and
the other target countries using primary sources and verified export fixtures; see [10-bank-import-research.md](10-bank-import-research.md).

### ZCR-LANG-01
Deferred outside the current delivery: keep the existing six languages complete and reviewed. Additional
languages have no current release dependency or promised date; plan each with cultural and layout review later.

### ZCR-SEC-10 - Device privacy maintenance (D-63)

Owner-approved on 2026-10-08: optional four-digit device-wide app PIN, protected salted verifier, durable growing
attempt delay, current-PIN change/removal, device-authenticated lost-PIN recovery; optional Android screenshot block
with persistent recents protection; proprietary ownership clarified separately from bundled component notices.
Verified: 1041 tests and strict Windows/Android builds, en/fa/de light/dark 360/412/wide UI and Windows unlock workflow.
Physical-device authentication/recents and iOS are still gates. Only device-bound Android secure-storage ciphertext
is excluded from OS backup/transfer. SEC-01..07 database encryption, key wrapping and broader OS-backup policy stay
Proposed; this access gate does not complete those sections or introduce commercial limits.

### ZCR-LOC-12 - Receipt total maintenance (D-64)

Owner-approved scope: fix purchase-total detection and OCR row reconstruction, preserve date/store, process images
locally with bounded memory and independent recognition/storage copies, and review before saving in Home and rereads.
No ledger/debt/default-kind, schema, commercial, cloud or unrelated design changes. Regressions were run red before
implementation. Evidence: ReceiptEvidenceTests, existing six-language receipt tests, TextLayoutTests and PdfTextTests;
Windows actual file-picker/OCR: inclusive-tax 4.10, 2.39-degree skew 24.90, EXIF-rotated 4.10, two totals
requiring a manual choice, USD/EUR conflict and missing-total abstention; explicit save/stored-JPEG reread 4.10,
including preserving the prior amount when absent. Picker/draft cancellation, unreadable PDF and failed image
preparation left the fictitious ledger and attachments unchanged. Stored JPEGs were bounded and had no EXIF.
Final validation: 1100 C# tests passed, zero warnings/errors in Windows/Android CI-mode builds; en/fa/de, both
themes, 360x800 / 412x892 / 1280x900 running-app snapshots and draft checks passed; test output cleaned.
All source/candidate data is transient. JPEG attachments are metadata-free on Windows as well as phones. Recognition
quality on physical Android/iOS, Persian-script OCR on Android and a representative corpus remain ZCR-QA-05 gates;
no improvement percentage is claimed and no new OCR SDK or automatic receipt crop is introduced.

### ZCR-LOC-13 - Plan and debt setup (D-65)

Owner-approved maintenance: date-anchored monthly repetition, nearby rule summary/actual date preview, explicit rule
calendar, common ending choices in both modes, collapsed uncommon rules and optional category picker. Dedicated
debt/receivable direction with positive reference-date amounts, optional estimates, detail navigation and an unsaved
repayment-reminder transfer draft (unknown principal, no automatic posting). No schema/ledger/backup format change,
SDK or permission. Validation: 1121 tests including 21 new AT-72 cases; strict Windows/Android builds and running-app
en/fa/de both-theme 360/412/wide review. Windows UI Automation verified positive debt/receivable signs, saving
to details, first-date/monthly/custom binding, a saved unknown-principal reminder and cancellation with unchanged
ledger counts. Modal dismissal followed by detail navigation is sequential on Windows. Physical-device AT-72 is still a release gate. LOC-06 and commercial waves
retain their separate scope/approval requirements.

### Owner-approved maintenance on 2026-10-08 (D-67)

ZCR-LOC-14: connected-backup discovery and independent regional display. Implemented: optional protection retained,
automatic listing and destination feedback, restore discovery across profiles with unchanged retention, device-file
fallback labels; shared regional controls in onboarding/settings, portable allowlisted display preferences and
backward-compatible restore. AT-73/74 local tests and rendered checks; real provider and device gates remain QA-02.
This does not authorize commercial enforcement or another wave. Next proposed section remains ZCR-SEC-01.

### SEC-01 evidence update (2026-10-08, D-68)

Research and independent proof authorized by the owner. Proposed ADR 0010 inventories current plaintext data,
compares legacy Community/current self-built/official Commercial SQLCipher and SEE, designs per-profile keys and
safe migration/restart/rollback states. Technical research/proof delivery is complete: four independent Windows
AT-75 tests pass; the separate Release AOT/trimmed Android fixture passes on an isolated API 36 x86_64 AVD using the
existing installed emulator. A second process recovers the Keystore-wrapped key and reopens the unchanged fictitious
profile; modified envelopes are rejected. Main 1,134 tests and strict Windows/Android builds pass. ADR acceptance
and production library/recovery/OS-backup decisions remain owner gates. No real data, production connection, native
provider, main solution or app permission changes. Status remains In progress solely until owner approval of ADR
0010, as required by this section definition; implementation has not begun.


### SEC-09 current-build review (2026-10-08, D-69)

Continuous implementation of ready planned sections is authorized by the owner; unresolved ADR/library and OD-10
choices remain owner gates. Review current sources/resolved wrappers, separate signed Android Offline/Cloud Release
permission boundaries, qualified SDK diagnostics and store drafts in docs/privacy/zanance-sdk-review.md. Eleven
AT-76 PowerShell policy cases reject unexpected permissions and configuration errors; no new SDK, app UI or finance
model. Signed iOS/native traffic/real OAuth and future billing/sync SDK reviews remain explicit gates. Strict
Windows/Android builds have zero errors/warnings; all 1,134 main tests and the eleven policy cases pass.
Both signed APK variants pass their shipped permission checks, signature and assembly/ZIP checks. Offline Release
reaches fresh first-run UI on the isolated emulator. Do not infer production or native-traffic acceptance from these
checks. The baseline is delivered in this commit; future-SDK revalidation is mandatory.

D-73 / QA-03: actual application flow sources are compiled in the new non-MAUI application test project through
explicit native ports. AT-80 adds 68 cases; main suite 1,263 passed. Strict Windows/Android builds, 522 Windows
captures and isolated native onboarding/PIN/widget/bulk/Undo checks are recorded in AT-80. Complete signed Release
APK verified. Native OS authentication, physical ARM64, real OAuth, iOS and product acceptance remain open.

D-74 / QA-04: 37 AT-81 cases exercise the actual manual valued-asset consent and save continuation, including
cancel/edit/accept, pending command exclusion, translated requests, validator and transfer invariants. Main suite
1,300 passed. Strict builds, 288 rendered Windows captures, native Debug/Release cancel and Release accepted
Income/Expense SQL identity/balance proof recorded in AT-81. Complete signed Release APK verified. Phone/iOS gates
remain separate; QA-06 performance is the next independent ready section under D-69.

D-75 / QA-06 delivers reproducible reference/tenfold migration/read/calculation/native measurements with original
financial-row preservation. D-76 covers Transactions until snapshot publication, disables premature input and offers
retry; 12 new AT-83 cases bring the main suite to 1,335. See quality/performance-q02.md for rendered/native evidence
and remaining findings. Native loaded Home, baseline ANR follow-up, physical ARM64 and iOS acceptance remain open;
neither this interaction gate nor emulator observations establish the two-second objective.

D-79 / Settings follow-up resolves the exposed async read/write-suppression boundary and stale live choice captions.
Publish complete Settings before enabling input; read failure stays covered with retry. Ten AT-86 real-source cases
bring the main suite to 1,345 (App.Tests 127). Running bindings, native retry/locale and the signed APK are recorded
in quality/font-scaling-a11y03.md. Keep QA-06/A11Y-03 partial: fixed controls, currency layout, native OS scaling,
screen-reader, baseline ANR/performance and physical/iOS acceptance remain independent. No commercial/security change.

D-80 continues A11Y-03 under D-69 with growing Settings/account/debt action captions, full-width debt actions and
two-row bulk actions. AT-87 records actual native selection command invocation and geometry without writing money;
main suite remains 1,345. Final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Keep headers,
currency layout, other controls, real OS/screen-reader/phone/iOS and unresolved owner gates open.

D-81 continues A11Y-03 under D-69: date parts reserve all digits at native text scale and reflow; large amount
readouts retain the existing signed decimal/currency packet and expose horizontal overflow with translated feedback.
Account balances/movements get separate rows and Home navigation stays on its heading. AT-88 records actual native
date/amount geometry, both scroll ends and unchanged fictitious entries; main suite remains 1,345 (App.Tests 127).
Final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Keep headers, other controls, real OS,
keyboard/screen-reader/phone/iOS and unresolved owner gates open. No model, financial or security policy change.


D-82 resolves D-81's reopened native language failure under D-69: retired navigation Title bindings are detached
before replacing their Shell, preserving active forms and the lock/deferred rebuild boundary. Settings constructor
defaults no longer invoke a save. AT-89 checks actual native selection/back, strongly retained retired titles,
live captions/drafts and complete unchanged stored settings/accounts/entries. Main suite remains 1,345 (App.Tests
127); final rendered/native/APK evidence is in quality/font-scaling-a11y03.md. Header truncation and other controls,
real OS/keyboard/screen-reader/phone/iOS and unresolved owner gates remain independent work.

D-83 continues A11Y-03 under D-69: complete Windows child-page titles grow above the same body; header/body sizing
uses the current root and Back names/tooltips translate live. Android keeps the native arrow/commands with a scoped
live Back description. AT-90 records native glyph/name/target geometry, actual retained bodies/drafts during resize,
single headers after nested return and unchanged stored data. Final evidence/APK is in quality/font-scaling-a11y03.md.
Modal/Insights/custom controls, the existing nested-return Settings draft reload, real OS/keyboard/screen readers,
physical ARM64/iOS, QA-06 durations/ANR and unresolved owner decisions remain independent work.

D-84 resolves the independently observed Settings nested-return estimate reset. Full covered refresh remains;
raw unsaved text/period/currency stay scoped to the current profile/settings row. Save accepts only successfully
submitted input; no autosave, financial rule or schema change. AT-91 adds 13 cases, and the real retained-body
runtime route now checks drafts after nested return. See quality/settings-estimate-draft.md for final evidence/APK.
Continue modal/Insights/custom-control and other ready work under D-69; unresolved owner/platform gates remain.

D-85 continues A11Y-03 with all four visible Windows Insights destinations in a growing navigation row above
the retained body. Narrow windows reflow into two columns; wide readable content shows four. AT-92 invokes the
actual native routes and checks complete caption/selection/target geometry and unchanged stored data. Final
evidence/APK and independent platform/other-control limits are in quality/insights-navigation.md.

D-89 continues A11Y-03 with complete growing tag suggestions and original-value native selection. AT-95 checks
84 actual native invocations and 21 exact draft/stored-data restorations across the three-language/theme/width
matrix; normal signed Android Release confirms selection, Keep editing and Discard without financial writes.
Main suite remains 1,369 (App.Tests 140); strict Windows and equivalent-command Android Release have no warnings/errors.
PowerShell startup blocks the canonical APK/privacy scripts locally; the complete signed package and matching binary
policy are independently verified. Evidence and remaining tooling/OS/screen-reader/phone/iOS/owner gates:
[quality/tag-suggestions.md](../../quality/tag-suggestions.md).

D-90 continues A11Y-03 with state-matched transaction detail actions and retained unsaved fields. AT-96 checks
48 actual native Invoke operations and 24 complete draft/stored-row restorations, including Simple/Advanced initial
visibility. Main suite remains 1,369 (App.Tests 140); strict Windows and canonical Android Release have zero warnings/
errors. Normal signed Release passes hide/show, retained payee/tag/note values and Keep editing/Discard in en/fa/de;
complete fictitious financial rows remain unchanged. Canonical APK/privacy scripts and full package/signature pass.
Evidence and independent OS/screen-reader/phone/iOS/provider/owner gates:
[quality/entry-details-disclosure.md](../../quality/entry-details-disclosure.md).

D-91 / AT-97 keeps both first-run restore alternatives fully readable and retains their existing navigation/busy
bindings. The final Windows matrix and signed Android Release pass; 1,369 tests (App.Tests 140), zero-warning strict
builds and full installable APK/privacy checks. See [quality/onboarding-restore-actions.md](../../quality/onboarding-restore-actions.md)
for native draft/stored-data preservation and independent platform/physical-device/provider/owner gates.

D-92 / AT-98 removes measured redundant Home row/goal work while retaining complete values and financial rules.
All 1,384 tests pass (App.Tests 148), strict builds and complete signed Release APK/privacy/native checks pass.
Controlled tenfold Windows warm reload median: 2,727.70 -> 1,228.28 ms. QA-06 remains partial: entry materialization,
cold start/ANR and physical-device Q-02 are open. See [quality/performance-home-snapshots.md](../../quality/performance-home-snapshots.md).

D-93 fixes the Windows-only Home diagnostic call that broke Android Debug, with no Release/UI/financial behavior
change. Final Windows/Android Debug/Release builds and all 1,384 tests (App.Tests 148) pass. Temporary native stage
measurements identify entry materialization/initial account creation; QA-06 and physical/platform acceptance remain
open. Evidence: [quality/home-debug-platform-and-native-stages.md](../../quality/home-debug-platform-and-native-stages.md).

D-94 completes the local Customize Home caption/target follow-up: all eight section identities grow at full
width above the original controls, with 44 px targets and a complete Reset action. AT-99 final en/fa/de theme/width/
200% review and 84 native operations, 1,384 main tests, strict Windows/Android Debug/Release builds and signed Release
owned-sample readbacks pass. A11Y-03, QA-06 and real device/platform acceptance remain partial/open.
Evidence: [quality/home-customization-readable.md](../../quality/home-customization-readable.md).

D-95 / AT-100 removes measured eager native row construction for hidden Home Accounts while retaining the
complete snapshot and every shown account/action. Native tenfold publication median: 5,339.89 -> 10.69 ms, with no
total cold-start/Q-02 acceptance claim. Final language/theme/width/200% native visibility/detail/list checks, 1,384
main tests, strict Windows/Android Debug/Release builds, signed Release and complete owned-sample readbacks pass.
QA-06 full entry materialization, visible creation, cold duration/ANR and real device/platform acceptance remain open.
Evidence: [quality/home-hidden-account-views.md](../../quality/home-hidden-account-views.md).

D-96 / AT-101 completes account type/default/excluded/incomplete captions in the shared account row at large
native text, retaining complete text, flags, balances and row actions. Final en/fa/de themes/widths/200% and normal
text checks cover all eight flag combinations; 1,384 main tests, strict Windows/Android Debug/Release builds, signed
Release navigation and exact 24-table original-sample readbacks pass. A11Y-03 other controls/modals and actual OS,
screen-reader, phone/iOS and release acceptance remain open.
Evidence: [quality/account-descriptions-readable.md](../../quality/account-descriptions-readable.md).

D-97 / AT-102 completes the existing Accounts debt/receivable action caption with a growing real button,
unchanged text/scaling/command and native open/cancel verification of the same unsaved Loan form. Complete stored
accounts/entries/settings/budgets/schedules remain unchanged. Final en/fa/de themes/widths/200% and normal text,
1,384 tests, strict Windows/Android Debug/Release, signed Release navigation and exact 24-table readbacks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/debt-entry-action-readable.md](../../quality/debt-entry-action-readable.md).

D-98 / AT-103 removes duplicated Windows modal titles and reflows all eleven existing modal title/Cancel
rows without changing forms or financial behavior. Actual glyph/name/target/no-overlap and native Cancel/parent
Back pass across all Windows language/theme/width/text contexts; all nine complete stored data sources remain.
Normal Android debt/expense/plan forms pass in three languages and both themes; original three-profile tables
remain identical. All 1,384 tests, strict Windows/Android Debug/Release and complete signed APK checks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/modal-headers-readable.md](../../quality/modal-headers-readable.md).

D-99 / AT-104 collects all applicable plan-field problems in one invalid Save attempt, displays destination
feedback beside its input and reveals the first problem using fresh native layout. Drafts and complete stored
Accounts/Entries/Settings/Schedules remain; existing money/recurrence/unknown-amount rules are unchanged.
Twenty-four new behaviour cases bring the main suite to 1,408 (App.Tests 172). Full Windows language/theme/width
review, strict Windows/Android Debug/Release, normal Android invalid Save/cancel, signed APK and original 24-table
readbacks pass. Other A11Y-03 controls and OS/readers/phone/iOS/owner/release gates remain open.
Evidence: [quality/plan-validation-visible.md](../../quality/plan-validation-visible.md).

D-100 / AT-105 collects independent transaction monetary problems before mutation, shows each beside its
input, reveals the next affected field and reopens invalid collapsed details without replacing entered values.
Existing parsers, consent/receipt protections and successful ledger/fee/overlap/attachment ordering remain.
Forty-eight new cases bring the main suite to 1,456 (App.Tests 220). Complete Windows matrix, strict builds,
normal Android invalid Save/cancel, signed APK and original 24-table readbacks pass. Simple destination-fee
retention is a separate pending runtime concern; other controls/platform/owner/release gates remain open.
Evidence: [quality/entry-validation-visible.md](../../quality/entry-validation-visible.md).

D-101 / AT-106 closes the reproduced destination-fee loss in successful Simple edits. Existing fees stay
visible/editable in both modes; new destination-fee creation remains Advanced-only. Native retention, explicit
edit/removal and reopened/new Cancel/Discard preserve financial fields/ids and unrelated rows. The final
Windows matrix has 24 contexts/72 valid Saves; main tests remain 1,456 (App.Tests 220), strict builds and signed
APK pass. Normal Android creation-policy checks and original financial/preference values and expected settings audit pass; successful stored-fee
editing remained open at D-101. D-369 later qualifies the independent original Android Release/AOT edits; physical/iOS and other owner/platform/release gates remain open.
Evidence: [quality/destination-fee-retention.md](../../quality/destination-fee-retention.md).

D-102 / AT-107 delivers complete settlement period/bill feedback and a growing Record the difference
action. Invalid Save reveals the first affected input without changing the draft; original advance/refund logic
retains zero bills and no-op exact bills. Fourteen new cases bring the main suite to 1,470 (App.Tests 234).
The final Windows matrix has 24 contexts/144 invalid and 72 valid native Saves; six normal-scale Release Android
contexts/18 invalid Saves, strict builds, signed APK and original financial/preference-value checks pass.
Normal Settings audit updates are separate; physical/iOS/readers, valid Android settlement acceptance and other
owner/platform/release gates remain open. Evidence: [quality/settlement-feedback.md](../../quality/settlement-feedback.md).

D-103 / AT-108 completes due-item action captions and correctly placed payment/override feedback, with independent
corrections and native revelation of the actual affected field. Fourteen new cases bring the main suite to 1,484
(App.Tests 248). Windows: 24 contexts/96 invalid and 96 valid native Saves, plus a separate 24-context/96-invalid
final contextual-message review with no additional financial Saves; normal-scale Release Android:
six contexts/18 invalid Saves. Actual metadata changes never post entries; partial payment and completion retain
the intended entries and unique settlement. Original financial rows/states are preserved/restored; strict builds
and the complete signed phone-test APK pass. Phone/iOS/readers, valid Android occurrence acceptance and other
owner/platform/release gates remain open. Evidence: [quality/occurrence-feedback.md](../../quality/occurrence-feedback.md).

D-104 / AT-109: complete transaction row reuse across repeated filters, with fresh data/display invalidation and
existing bulk selection, is implemented (four cases; main 1,488/App.Tests 252). Actual bound tenfold Windows warm
refresh median 583.14 -> 358.90 ms; native publication remains open. Final verification and limits:
[transaction-row-reuse.md](../../quality/transaction-row-reuse.md). QA-06 remains partial; no capped result list.

D-110 advances ready QA-06 native publication: complete unchanged transaction results retain their actual native
source; changed results and fresh snapshots still publish. Actual tenfold warm publication median 407.51 -> 1.61 ms.
Nine new behavior cases pass (main 1,521/App.Tests 285); six native Windows contexts/186 renders, strict builds,
normal Release, exact original-data readbacks and complete signed APK pass:
[unchanged-transaction-source.md](../../quality/unchanged-transaction-source.md). QA-06 stays partial.

D-112 tests one remaining entry-materialization hypothesis without changing production code: synchronous EF
enumeration inside the existing worker retains complete ordered packets/date bounds, but is slower in four of
seven tenfold pairs and does not reduce allocation. The candidate is not adopted and is not repeated without a
new measured cause. D-111 remains the app/test/native/APK baseline; QA-06 stays partial:
[entry-read-enumeration-comparison.md](../../quality/entry-read-enumeration-comparison.md).

D-113 / AT-117 continues ready A11Y-03 work with complete growing transaction category choices. Actual native
glyph/viewport/command checks pass in 33 contexts/99 selections, with full draft/store/developer-byte restoration.
Main 1,535/App.Tests 299, strict Windows/complete Android builds, normal Release selection/discard, exact original
data readbacks and complete signed APK pass.
See [entry-category-captions.md](../../quality/entry-category-captions.md). Other A11Y-03 and owner/external gates stay open.

D-115 resolves the specific empty-entry gesture hypothesis: the native IME covers the gesture origin despite
the helper's false input-method flag. After native Back removes the actual IME window, the identical gesture
exposes complete Title/date in the same blank draft without typing or Save. No production correction is adopted;
failed candidates and diagnostic ANR evidence remain negative. The first-position category check is accepted;
last-position fixture and broader platform acceptance remain open. Do not repeat completed category/financial
checks or the resolved hypothesis without a new observed cause; see the D-115 section of the same evidence file.
OD-10 interim exclusion was declined by the owner (D-114). Keep current runtime policy and determine final
encrypted-data/key/recovery handling with completed security; no temporary product measures.

D-116 / AT-118 continues ready A11Y-03 work with one complete warning packet per savings-goal card. Native
before evidence shows the duplicated packet; 27 Windows contexts/54 packets check both templates, full text/glyphs,
display digits, original command/name and complete presentation/stored-row restoration. Delivery evidence:
[goal-warning-packets.md](../../quality/goal-warning-packets.md). This does not close broader accessibility gates.

D-117 / approved OD-03 delivers ENT-01: pure Core capabilities, validity/fallback, scope-bound guest rights and
final quota/counting rules. AT-119 tests every valid product/duration x both display modes x personal/shared scope,
invalid products, expiry/revocation, downgrade data access, quotas, history/paused/budget/member identity boundaries.
No app limits, purchase UI, store adapter, schema or financial change. Validation and signed APK:
[entitlement-policy.md](../../quality/entitlement-policy.md). ENT-02 is the next ready service section; its checks
remain disabled until separate test-build/release activation. All unresolved owner/external gates remain open.

D-118 delivers the first ENT-02 write boundary, not the complete section. Account quota checks occur before writes
under the actual opened file's SQLite writer; independent providers cannot both consume the final slot. Inactive
current registration has no permanent paid grant or quota transaction. 15 cases/main 1,626 pass. Goals, plans,
budgets, templates/filters/profiles/holdings, import/restore above-quota classification, automatic work/deep links/
widget/OCR and release activation remain open. [Evidence](../../quality/account-write-policy.md).

D-119 continues ENT-02 with actual-file template/filter write transactions, slot-preserving edits/replacements
and atomic failure recovery. Current enforcement stays inactive. 24 added SQLite cases/main 1,650 pass; the
remaining resources/import/restore/read-only/native paths and release gates stay open:
[quality evidence](../../quality/template-filter-write-policy.md).

D-120 continues ENT-02 with Goal Save and whole Plan batch/split transactions, counted pauses and slot-neutral
continuations. 45 added SQLite cases/main 1,695 pass. Current deployment stays inactive; contribution/allocation/
occurrence work, other resources/import/restore/read-only/native paths and activation remain open:
[quality evidence](../../quality/goal-plan-write-policy.md).

D-121 continues ENT-02 with current-period budget Save/confirmed Replace, canonical financial definitions and
complete replacement rollback. 26 added SQLite cases/main 1,721 pass. Current deployment stays inactive; explicit
active/read-only selection, future-period activation and other ENT-02/03 paths remain unfinished:
[quality evidence](../../quality/budget-write-policy.md).

D-122 continues ENT-02 with actual-file holding write rights, retained corrections/delete/Undo and atomic
purchase/payment/fee/derived-price Save. 46 added SQLite cases/main 1,767 pass. Current deployment stays inactive;
explicit read-only/import selection, other resources/operations/native paths and activation remain unfinished:
[quality evidence](../../quality/holding-write-policy.md).

D-123 continues ENT-02 with retained earmark/release rights and contribution operations, plus one atomic
goal/pin/contribution editor Save. 39 added SQLite cases/main 1,806 pass. Current registration remains inactive;
selected read-only items, contribution delivery and remaining ENT-02/03/04 paths stay open:
[quality evidence](../../quality/goal-contribution-write-policy.md).

D-124 continues ENT-02 with exact-file database backup/recovery and retained import/Undo checks. Restore binds
the destination before input and migrates the same file, correcting an actual reproduced redirection defect.
28 added SQLite cases/main 1,834 pass. Current registration stays inactive; selected read-only import/restore data,
profiles, automation and remaining ENT-02/03 paths stay open: [quality evidence](../../quality/recovery-write-policy.md).

D-125 continues ENT-02 with actual-file category rule creation/correction/deletion rights and serialized
same-pattern replacement, including current unrestricted builds. 28 added SQLite cases/main 1,862 pass.
No stored transaction is reclassified. Registration stays inactive; suggestion delivery, selected read-only data,
profiles, automation and remaining ENT-02/03/04 paths stay open: [quality evidence](../../quality/category-rule-write-policy.md).

D-126 continues ENT-02 with actual-file ledger Save rights, complete split/holding/transfer groups and
final-batch refund validation. 45 added SQLite cases/main 1,907 pass. Four reproduced refund defects corrected;
entry/attachment/paid-total rollback retained. Registration inactive; ledger delete/Undo, selected read-only data,
automation and remaining ENT-02/03/04 paths stay open: [quality evidence](../../quality/ledger-write-policy.md).

D-127 continues ENT-02 with actual-file ledger Delete/Undo, explicit file-bound refund snapshots and
retry-safe application offers. 38 added cases/main 1,945 pass; App.Tests 308. Receipt orphan/purge behavior stays
unchanged, without extra byte copies. Registration inactive; selected read-only data, automation, commercial
feedback and remaining ENT-02/03/04 paths remain open: [quality evidence](../../quality/ledger-undo-write-policy.md).

D-128 reports actual transaction Undo/refresh failures through the existing translated dialog, preserving the
original retry deadline and blocking repeated invocation through pending feedback. Nine added actual-command cases;
main 1,954 / App.Tests 317 pass. Six normal installed Release failure flows retain committed financial data and
receipt bytes. No activation changes. Later refund edits during Undo and visible ledger Save feedback remain open:
[quality evidence](../../quality/transaction-undo-feedback.md).

D-129 makes short ledger Undo snapshots independent of returned mutable rows and rejects obsolete refund,
partial-group and account-currency dependencies before writes. 18 added cases/main 1,972 pass; App.Tests 318.
Normal/conflicted installed Release checks preserve complete data/receipts; current enforcement stays inactive.
Native editor timing, visible ledger Save feedback and remaining ENT-02/03/04/external gates remain open:
[quality evidence](../../quality/ledger-undo-conflicts.md).

D-130 delivers the observed D-126 Save-feedback follow-up: existing general transaction errors stay beside
fixed Save, with native scalable wrapping text and complete spoken names. Explicit rejected Save/retry/storage
failure retain reviewed draft fields and stored rows; field validation clears obsolete general feedback. No data,
permission, algorithm or commercial activation changes. Remaining ENT-02/03/04 and external gates stay open:
[evidence](../../quality/entry-save-feedback-visible.md).

D-131 starts the approved ENT-03 Core choice projection and continues ENT-02 with actual-file selected-account
new ledger work, verified retained overdue payments and reviewed advance bills. 79 added cases/main 2,051 pass;
App.Tests 318. No activation, schema or portable paid facts. Other bindings, budget definitions, choice UI/persistence,
automation and all existing owner/external gates remain open: [evidence](../../quality/selected-account-write-policy.md).

The D-131 manual debt-closing follow-up is delivered by D-132. Occurrence-state/automation boundaries remain
open before enforcement activation. Current test builds are unrestricted; these slices do not accept all
retained operations or complete commercial enforcement.

D-132 continues ENT-02/03 with actual retained manual Loan/Lent closure and separate new-work rights for
counterpart borrowing. 51 added cases/main 2,102 pass, App.Tests 318. Current app registration stays inactive;
choice UI/persistence, other selected bindings and occurrence-state/automation remain open: [evidence](../../quality/retained-debt-closing.md).

D-133 continues ENT-02/03 with retained occurrence correction/delete/repair rights and atomic explicit payment
linking under the actual-file writer. 39 added cases/main 2,141 pass, App.Tests 318; native en/fa/de light/dark
normal confirmations preserve actual money/metadata and create no second entry. Registration stays inactive.
New settlements/partial/final posting, generated-entry reopening, selected-plan generation, automation, choice UI/
persistence and occurrence command failure feedback remain unfinished: [evidence](../../quality/occurrence-correction-writer.md).

D-134 continues ENT-02/03 with atomic new full/partial/final payments under the actual-file writer and current
remaining money. Reuse ledger validation; recheck stored relationships and Core occurrence rules, with complete
SQL/access rollback and notifications after commit. 16 added SQLite cases/main 2,157 pass, App.Tests 318;
six normal Release en/fa/de light/dark flows. Registration stays inactive. Generated-entry reopening,
selected-plan automation, choice persistence/UI and command exception feedback remain open: [evidence](../../quality/atomic-occurrence-payments.md).

D-135 continues ENT-02/03 with the reviewed settlement identity and actual entry relationship bound to the
same writer as generated deletion or manual unlinking. Reuse complete deletion/Undo; no second committed-state
write. Preserve partial money, overrides, refund/receipt ownership and suppression. 15 new cases/main 2,172 pass,
App.Tests 318; six normal Release native confirmations. Current registration stays inactive. Other selected-plan
generation/automation, choice persistence/UI and command exception feedback remain unfinished.
[Evidence](../../quality/atomic-occurrence-reopening.md).

D-136 completes exception feedback and shared consent/writer/publication gates for occurrence commands. 29 added application-flow cases; App.Tests 347 and main 2,201 pass with owned output-only Data.Tests serialization. Normal Release native failures preserve drafts and all complete rows. Negative parallel evidence is retained; D-137 later completes file-scoped cleanup isolation. Commercial registration remains inactive; selected-plan generation/automation, choice persistence/UI and external gates remain unfinished.
[Evidence](../../quality/occurrence-command-feedback.md).

### D-137 - Independent local SQLite pool cleanup

Maintenance follow-up to the D-136 observed parallel cleanup/migration failures is complete: actual-file pool retirement, captured restore destination, 39 isolated fixture cleanup sites; nine regressions, normal parallel full suite 2,210 pass, App.Tests 348, strict builds/native profile lifecycle/full signed APK verified. This does not complete the remaining ENT-02/03 bindings, choice UI/persistence or external gates. [Evidence](../../quality/local-sqlite-pools.md).

D-138 continues ENT-02/03 with fresh automatic payment eligibility/values and explicit cached plan choice under the actual ledger writer. 31 new cases; normal parallel main 2,241 / App.Tests 348 pass. Strict builds, six normal Release native flows and complete signed APK verified. Enforcement remains inactive; generation/reminders, choice persistence/UI and other selected bindings/external gates remain open. [Evidence](../../quality/automatic-occurrence-writer.md).

D-139 continues ENT-02/03 with selected future occurrences in Home/Plans/detail and actual generic/full/partial
payment writers; history/overdue rights and original calculation inputs remain complete. 41 new cases; normal
parallel main 2,282 / App.Tests 348 pass. Strict builds, six normal Release native flows with all 24 complete rows
unchanged/restored and the complete signed APK are verified. Registration remains inactive. Actual reminder/snooze
delivery, choice persistence/UI, other bindings and external gates remain open. [Evidence](../../quality/selected-plan-occurrences.md).

D-140 binds actual Plan/contract notification delivery and snooze rebuilding to the opened file and current
explicit plan choice. Original group members, recurrence identities and effective dates survive snooze; current
privacy/language/text is rebuilt from stored rows. Unprovable legacy snooze caches are discarded while normal
reminders are rebuilt; legacy navigation remains readable. Notification taps resolve the current file after
unlocking, without profile guessing or money writes. Reserve the snooze id marker so a hashed occurrence id cannot
keep the original deadline. Registration remains inactive. Choice UI/persistence, other selected resources and
external acceptance remain open. [Evidence](../../quality/selected-plan-reminders.md).

### D-141 - Durable account and Plan choices

Persist explicit account and plan choices in one additive profile-local ResourceSelections table, keyed by
resource kind and the verified financial scope kind/id. A fresh revision rejects stale and ABA drafts. Reviewed Save
rechecks actual file, membership/rights, original identities/names/states, row revision and capacity under the same
SQLite writer; no-op saves preserve revision and publish no Changed. Never archive/end or erase financial data to
represent read-only availability. Stored choices override cached fallback selections, never verified capabilities.

New account/unarchive and Plan creation join an existing bounded choice atomically when capacity remains. Explicit
archive/ending releases only its own chosen identity; a validated split transfers only the actual selected
predecessor slot. Pauses still count. Current work/reminder snapshots capture the stored revision and retire pending
publication after a change; choices trigger the existing reminder refresh. Retained corrections remain possible even
with malformed choice input, which grants no bounded new work until explicit reviewed repair.

Commercial registration remains inactive: no guessed main-profile identity, fake paid facts, new limits or purchase
activation. Inactive/unlimited loading writes nothing and requires no bounded choice. Choices are financial-profile
preference data in the normal database backup, not security, entitlement or credential facts; restore does not retag
a foreign financial scope. The chooser UI, remaining resource bindings and external/release gates remain open.

44 added AT-143 cases; main 2,374 / App.Tests 382 pass. ENT-02/03 remain in progress, with chooser UI as the next
ready step. [Evidence](../../quality/durable-resource-choices.md).

## D-142 - Reviewed account/Plan chooser delivered

Settings now opens a covered account/Plan availability form. One growing real target per complete original
identity keeps type/currency or entry kind, original state and unsaved choice separate. Paused plans remain paused
and count; archived/ended items remain visible but cannot occupy a slot. Counters and inline capacity feedback
follow current display digits. Save and reload reserve their own growing rows outside the scroll viewport. Rejected Save feedback stays
visible beside those actions, including when the original list is scrolled to its end.

Loading/failure hides the complete form; translated retry publishes only a complete current snapshot. Explicit
Save uses the D-141 actual-file/scoped writer and preserves the exact draft after storage, rights or stale-review
failure. Retire a committed review before navigating, so a failed return cannot submit again. Dirty reload, kind
switch and Back require one native discard decision with one shared busy guard. Live captions and help returns
retain the original unsaved identities/index; no autosave, financial posting or state rewriting.

Current commercial registration remains inactive: all eligible items are available, no subset or Save is required,
and the form explains this. There is no fake paid entitlement or new test-build limit. The snapshot review's
fictitious Free context belongs only to its separate artifact SQLite database/page; it never replaces release DI.
The account/Plan chooser is delivered; other resource choices/bindings, activation and owner/external gates stay open.

25 new AT-144 actual flows, main 2,399 / App.Tests 407 pass. Strict builds, Windows native form/navigation matrix, six normal Release own-emulator cases and complete signed APK: [evidence](../../quality/active-item-chooser.md). Continue remaining selected resources and operations; ENT-03 is not complete and commercial activation remains gated.

## D-143 - Durable selected goal writer delivered

Extend the existing ResourceSelections boundary to Goals without a schema/entity change. Reviewed original names,
measurement kinds, currency and active/paused/completed/archived states are rechecked with actual file, financial
scope, verified rights, stored revision and capacity. Free's one goal counts a pause; explicit empty is retained.
Invalid/stale choices need review, and choosing an advanced retained goal never grants its paid tools.

The goal writer reads stored choice under the actual SQLite writer; it overrides only cached choice, never
entitlement/membership. Explicit new or reopened goals join a free chosen slot atomically, rather than counting
unselected retained history again. Completion/archive/delete releases only that identity; pause keeps it.
Unselected paused goals cannot resume. Retained notes/state corrections, negative earmark releases and deletion
remain available. Positive earmarks independently require AdvancedGoals and usable goal/account choices; no ledger
or balance is written. Full editor/contribution failures roll back goal, pins, plan and choice together.

Current release registration remains inactive and unrestricted. Settings still exposes Accounts/Plans. Goal
chooser UI, effective goal/contribution projection and native reminder delivery follow in subsequent ENT-02/03
slices. Other selected resources, activation and owner/provider/device/iOS/publication gates remain open.

31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](../../quality/goal-resource-choices.md).

Next ready slice: goal chooser and effective contribution/reminder bindings. ENT-02/03 remain in progress.

## D-144 - Reviewed goal chooser delivered

The existing Settings availability form now has Accounts, Plans and Savings goals as three growing native choices.
Read the original goal name, measurement kind and state from the actual D-143 scoped snapshot; money goals also show
their original currency, while holding-quantity goals do not display a currency as their unit. Completed goals say
Completed rather than the Plan-specific Ended. Paused goals count toward Free's one-goal allowance without resuming.
Completed/archived identities remain readable and unselectable. No goal, allocation, contribution or ledger write
occurs while selecting, navigating, refreshing captions or discarding a draft.

Explicit Save uses the reviewed actual-file writer, including empty choices and malformed-choice repair. Preserve
the exact goal draft/index/identity on stale name/kind/currency/rights or storage failure and on live language change.
Keep existing consent/busy guard, covered publication/retry, pinned Save/error and committed-review retirement.
Current unrestricted registration requires no subset or Save and grants no fake paid rights. Choosing an advanced
goal never grants its paid tools; translated explanation/example make this explicit in all six existing languages.

Goal chooser UI is delivered. Effective goal/contribution projection and native reminder filtering/routing remain
the next ENT-02/03 binding slice; other resources/operations, activation and owner/external gates remain open.

18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](../../quality/goal-chooser.md).

Continue actual effective goal/contribution/reminder bindings. ENT-02/03 remain in progress.

## D-145 - Actual goal work and scoped reminder delivery

Read complete original goals, saved contribution rules and allocations from the actual opened database, with
new-work eligibility kept separate from original financial progress. Selection never changes funding/protection,
resumes a paused goal or grants a paid tool. Free balance-goal activity, advanced goal work and contribution reminders
are independent rights. Missing, empty, malformed, foreign and historical choices keep their original data.

New paid contribution work checks the actual chosen goal under the same SQLite writer. A paired new goal can join
its slot before that check; reject failed activity before pin/protection normalization. Retained amount corrections,
reminder disabling/removal and negative earmark releases stay available. These operations never post ledger money.

Prepare goal reminders only for eligible original progress before the pending cap. Keep saved opt-in, generic/details
privacy, fixed 09:00 device-local rule dates, existing permission and the shared bounded queue. Native returning data
contains bounded original goal/date members and an opaque actual-file identity. Taps resolve that file after unlocking,
open the existing single detail or Goals root, and never switch profiles or grant new work. Retained history remains
readable after a choice change. Existing legacy historical links remain supported.

Validate actual file, cached rights, persisted choice revision and original goal/rule/allocation input stamp before
and after native publication; clear a retired queue, including failed snapshot preparation. The stamp is transient,
invariant and reflection-free, not a new schema, portable preference or credential. Current commercial registration
remains inactive and unrestricted. Goal-view contribution affordances and other remaining bindings follow separately.

36 new AT-147 actual application/real SQLite cases; focused 36 pass (9.147 s). Normal parallel main 2,484 /
App.Tests 461 pass (71.478 s), no failures/skips; test output cleaned. Strict Windows 15.49 s / Android 35.35 s,
zero warnings/errors. Six normal signed Release owned-emulator cases cover English/Persian/German, light/dark,
actual native stored single/group Intent routing, original destinations/Back, and rejection in another real profile.
The real pending queue retains all three original goals, bounded members, generic privacy and saved dates at 09:00.
No financial Save; complete original rows and exact owned bytes/sidecars plus Main/English/System restored.
This verifies stored native requests and actual Android Intent delivery, not future alarm firing at 09:00.
Complete signed installable APK verified. No layout/copy change; D-144 geometry evidence remains revision-specific.
Physical-phone, iOS, provider, OS accessibility, Store signing/publication and commercial activation remain unaccepted.

[Evidence](../../quality/goal-work-reminders.md).

Next ready slice: bind goal/Home/detail contribution affordances to the separate work policy while keeping all original progress/history and retained corrections. Other resources/operations, commercial messages/activation and owner/external gates remain open; ENT-02/03 are not complete.

## D-146 - Original goal views and reviewed detail actions

Complete original progress/funding/history/pins stay visible, with separate new-work captions/actions. Historical negative releases, actual-file reviewed detail writes, complete metadata validation, currency/culture drafts and growing failure/retry are delivered. Main 2,520 / App.Tests 497 pass; native Windows matrices, six signed Release emulator cases and complete installable APK verified.

[Evidence](../../quality/goal-work-views.md).

Next ready slice: actual goal editor/contribution draft binding, retaining original saved rules and unsaved input. Other resources/operations, import/restore/native paths, messages/activation and external gates remain open; ENT-02/03 are not complete.

## D-147 - Reviewed goal editor and explicit budget cuts

40 new AT-149 actual-source/real SQLite cases pass. Final normal parallel main 2,560 / App.Tests 537 pass,
zero failures/skips; test output cleaned. Strict Windows and Android builds and canonical Release APK build have zero
warnings/errors. Eighteen English/Persian/German, light/dark, 360/412/wide contexts check the actual Advanced and Simple
modal at process-local 1.5 font stress: complete changed glyphs, real 44-pixel Invoke targets, stale Save rejection,
retained reviewed reload/current saved summary and native Close. All 25 complete fixture tables and original development
data/sidecar bytes are restored. The review uses independent fictitious data and explicit native-effect answers, not
replacement MAUI controls or a production entitlement replacement.

Six normal signed Release owned-emulator cases check the original archived account, uncommon rule, multiple-category
warning, pinned Save and native Close/Back. One actual note-only Save succeeds; complete original rule/categories,
accounts, ledger, allocations and other metadata stay equal. Only that goal note/audit and the previously delivered
D-141 migration differ. Exact owned original bytes/sidecars and Main/English/System are restored; the normal Release
remains installed/stopped. No physical-device, iOS, OS accessibility, provider, performance, Store signing/publication
or commercial-activation acceptance is implied.

[Evidence](../../quality/goal-editor-work.md).

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

## D-148 - Selected budget definition boundary

Canonical scoped budget definition choice and separate current-work projection are implemented with
reviewed actual-file persistence. Save/Replace join or transfer chosen capacity atomically; Delete retains exact
membership checks and releases only the last current definition. Original history, paid-method corrections,
limits and financial rows remain intact. 34 new AT-150 cases; normal parallel main 2,594 / App.Tests 537 pass;
strict builds, normal Release native Save/Delete and six unchanged-editor language/theme checks, complete signed
APK and exact owned data/display restoration verified. Registration remains inactive. Budget chooser/view/alert
binding and other ENT-02/03 paths remain open. See [evidence](../../quality/budget-definition-choice.md).

## D-149 - Actual reviewed budget definition choices

The actual active-items form includes canonical budget definitions, grouped across copied periods with
original scope names/current methods and unselectable history. Covered review captures full account references;
explicit Save rechecks original metadata and writes only chosen definitions. Original account names stay exact
when display digits change. 13 new AT-151 cases; main 2,607 / App.Tests 550 pass; strict builds, 18 Windows native
chooser cases plus 18 registered Settings/help cases, six normal Release emulator cases, exact data/display restoration and complete signed
APK verified. Registration remains inactive. Effective budget/view/alert bindings and other ENT-02/03 paths
remain open. See [evidence](../../quality/budget-definition-chooser.md).

## D-150 - Actual current budget alerts

Actual alert orchestration checks every eligible original current budget across calendars/currencies/scopes,
with separate chosen-work rights and complete original net spending/carry. Read-only actual-file financial/privacy
reviews guard native publication; failed/retired batches retract attempted alerts and retain retryable device levels.
Carry no longer mixes another account definition. 25 new AT-152 cases; main 2,632 / App.Tests 575, strict builds,
six hidden Windows budget renders, six normal Release Android delivery cases (five with individual taps), a final rebuilt SDK-acceptance case and complete signed APK pass;
owned data/display restored. Enforcement remains inactive. Budget view/editor/scoped returning-data and other
ENT-02/03 paths remain open. See [evidence](../../quality/budget-alert-work.md).

## D-151 - Original budget notification destinations

Budget alerts now carry the opaque actual-file identity and original saved period id. Resolve after unlock and
again in the real budget page; reject another file, deleted identities and failed reads without guessing a
replacement. Retain original currency/calendar/period and monthly navigation coordinates without changing
Home currency or financial rows. Historical/unselected paid methods remain readable without granting new work.
14 new AT-153 cases; main 2,646 / App.Tests 589 pass. Strict Windows and complete strict Android Release pass;
six hidden Windows language/theme cases plus targeted native month navigation, and six normal signed Release
Android cases each tap four exact original currency/calendar/period destinations; owned data/display restored.
Commercial activation stays inactive. Budget view/editor commercial affordances and other ENT-02/03 paths remain
open. See [evidence](../../quality/budget-reminder-routing.md).

D-153 continues ENT-02/03 with original budget view rights and paired copy/replacement/delete against complete
displayed reviews, separate future/net capacity and retained correction/delete rights, growing native captions and
refresh-only retry after committed operations. Remaining resources/operations/import/restore/native paths and
activation/external gates stay open. See [evidence](../../quality/budget-view-actions.md).

## D-155 - Original Home template destinations

Home quick templates publish separate original activity availability after the complete parent read. Explicitly
available later definitions remain reachable within the existing six shortcuts; retained history and all original
financial calculations remain independent. A process-local TemplateVisit carries the reviewed actual-file/full
packet to the existing entry route. A changed/deleted original, reference, choice or right, or another actual file,
is rejected without guessing a newer same-id row or saving money. Failed navigation pins translated feedback and
requires explicit read-only Reload. The actual initial chooser receives only its typed resource kind, never a
selection. Destination feedback is visible beside Save; applying a template remains a detached draft.

Opening Home no longer creates or updates Settings for its layout fallback. Existing saved layouts remain exact;
the legacy device layout is read until an explicit profile-layout Save. Commercial registration stays inactive.
No schema, SDK, permission, network request, portable preference or entitlement grant is added.

21 new AT-157 actual-source/real SQLite cases pass. The ordinary main run passes 2,754 / App.Tests 697;
three focused name-order cases bring the verified total to 2,757 behaviours / App.Tests 700. The changed flow
class passes all 21 cases; the unchanged full suite is not repeated. Pending navigation awaits its actual event.
Outputs are cleaned. Strict Windows and complete strict Android Release have zero warnings/errors. Eighteen
hidden Windows Home/chooser contexts and eighteen focused destination continuations cover en/fa/de, light/dark,
360/412/wide. Six normal signed Release emulator cases open original Home templates, fill original amount/title
and cancel/discard without financial Save. Complete owned rows/bytes/display are restored. Physical-device, iOS,
OAuth, commercial activation and publication gates remain open; ENT-02/03 remain in progress.

See [evidence](../../quality/home-template-destinations.md).

## D-156 - Reviewed original device profiles

Original device profile choices now use the existing empty main id and exact original profile ids/names.
The actual Profiles form separates open/rename/delete from an explicit saved activity subset and unsaved choice.
One device preference publication contains the complete catalogue and scoped choice, with separate revisions;
legacy keys remain untouched and every catalogue/startup/backup-set reader uses the new packet after first mutation.
Opening reads only. Complete file/current-profile/provider/catalogue reviews guard publication, both delete
confirmations and creation; authentication is rechecked before committing a switch, with previous-path rollback
when that review retires. Unselected originals remain openable for retained history and correction.

Production device registration remains inactive. Per-file financial/new-money/automation bindings to the chosen
profile remain unfinished; this slice does not complete ENT-02/03 or activate commercial limits. No EF model,
schema, migration, permission, SDK, network request, credential or portable grant is added.

33 new AT-158 actual-source/real isolated SQLite behaviours pass. The ordinary main run passes all 2,790
tests (App.Tests 733), with no failures/skips; outputs are cleaned. Strict Windows and canonical complete Android
Release builds have zero warnings/errors. Eighteen hidden Windows en/fa/de, light/dark, 360/412/wide contexts
check actual native targets, complete growing captions, explicit choice Save, stale Save, retained drafts,
Cancel/Discard/Reload and unchanged complete fixture data. All review processes exit normally and exact owned
development bytes/sidecars are restored. Normal signed Release emulator checks and APK details are recorded in
the evidence document. Physical-device, iOS, OAuth, paid activation and publication gates remain open.

Next ready slice: bind the reviewed original device profile choice to actual-file financial/new-work policy,
retaining historical correction/backup/export and complete calculations. Continue remaining operations,
import/restore/native paths and commercial messages under D-69; activation and owner/external gates stay separate.
See [evidence](../../quality/local-profile-work.md).

## D-157 - Actual-file original profile work

The original device profile choice now composes with the existing verified actual-file commercial source.
The legacy empty main id remains separate from the independently verified financial scope; changing the current
profile preference cannot transfer rights to another file. The immutable context retains the original plan,
resource choices and scope, plus complete catalogue/legacy/provider review facts. Original writer/snapshot
validation therefore rejects retired choices, including change-and-return revisions and changes while waiting
for SQLite ownership. Device choices grant neither paid tools nor shared-space membership.

New money, definitions, selected Plan/Goal/Budget work, template activity and future Plan/Goal reminders consult the same
profile condition. Complete original financial inputs, history, query viewing, corrections, refunds, overdue
settlements, deletion, backup/restore and owned export remain independent. A malformed/unlisted catalogue never
guesses main: new work stops while separately verified historical rights remain. Both current registrations
stay inactive; this slice does not activate limits or complete every operation/import/native binding.
No EF model, schema, migration, SDK, permission, network request, credential or portable grant is added.

31 new AT-159 actual-source/real isolated SQLite cases pass. The ordinary main suite passes all 2,821
behaviours (App.Tests 764), without failures/skips; test output is cleaned. Strict Windows and canonical strict
Android Release builds have zero warnings/errors. Focused original-page Windows and normal Release owned-emulator
results, exact data restoration, retained negative evidence and the complete signed APK are recorded in
[the evidence document](../../quality/profile-file-work.md). Commercial activation, physical-device, iOS, OAuth,
Store signing/publication and the remaining original-operation bindings remain open.

Next ready slice: bind original catalogue changes to reminder refresh and enforce separately authorized period-
review reminders in the same actual-file publication. Then finish remaining operation/import/restore/native
bindings and clear rejection messages that retain each form/draft. Continue ENT-02/03 then ENT-04 under D-69.
ENT-05 activation and
owner/external gates stay separate; this is not completion of the commercial release.

## D-158 - Original catalogue reminder refresh and period reviews

Original catalogue changes now reach the actual reminder watcher, so explicit activity choices rebuild the
queue without another financial action. The shared event port introduces no construction-time database read.
Subscriptions are lifetime-scoped and idempotent; a different catalogue cannot be substituted. Delayed work uses
the injected clock and exposes its actual full batch task for owned-lifetime checks. Disposal cancels future work,
detaches subscriptions and asynchronously drains current work without deleting valid native reminders.

Period-review delivery uses the same captured actual-file paid/profile facts. Complete original preferences,
first-recorded account date, language/formatting/calendar/digits and local day validate before/after native
publication. Revalidation never creates Settings. SDK request refusal is consumed for scheduled requests as well
as immediate alerts; partial pending work is withdrawn and Snooze cache commits only after successful complete
publication. Original financial rows and recovery remain independent. Current registrations stay inactive.
No EF/model/schema/migration, permission, SDK, network, credential or portable format change.

25 new AT-160 actual-source/real SQLite cases pass. The final ordinary main suite passes 2,846 tests
(App.Tests 789), with zero failures/skips, and output is cleaned. Strict Windows and canonical complete strict
Android Release build have zero warnings/errors. Original native Profiles Save drives the real watcher in an
independent fixture; complete rows/catalogue and owned development bytes are restored. Final normal Release
SDK delivery, exact owned restoration, APK details and negative evidence are recorded in
[evidence](../../quality/profile-reminder-work.md). Commercial activation, physical-device, iOS, OAuth and publication
remain separate. Original Plan/Goal financial-input retirement and other operation/native bindings remain open.

Next ready slice: retire original Plan/Goal reminder batches when their actual financial inputs or rule/state
packets change during publication, then complete remaining original operation/import/restore/native bindings and
retained-draft rejection messages. Continue ENT-02/03 then ENT-04 under D-69. Activation and owner/external gates
stay separate; this slice does not complete the commercial release.

## D-159 - Original reminder input retirement

Original Plan rules/states, Goal/contribution/allocation packets and raw resource choices now retire a
reminder batch when any captured value changes, including changes at unchanged audit times or choice revisions.
Complete original accounts and the date-bounded ledger/quantity inputs used by the existing GoalProgress
calculator are captured in one deferred SQLite reader. Financial data is never filtered by activity selection.
The same coherent reader validates all original packets, preferences and first-data date before/after native
publication; a changed or failed preparation withdraws pending work and preserves the retryable Snooze cache.
Only needed ledger/quantity inputs are read. Future movements remain outside today's calculation.

Snapshot self-mutation rejection belongs to the reminder boundary: existing Goal editors can still mutate their
reviewed draft before the established explicit writer. Full raw selection values distinguish absent/empty
choices. No schema/model/migration, permission, SDK, network, credential or portable preference is added.
Commercial registrations remain inactive and test builds stay unrestricted.

24 new AT-161 actual-source/real SQLite behaviours pass; 100 related reminder/Goal editor/view cases pass.
Final ordinary main: 2,870 passed, zero failures/skips, App.Tests 813, 2m 03.711s; output cleaned.
Strict Windows and canonical complete Android Release have zero warnings/errors. Original running Goals pages
in en/fa/de at 412 pixels/dark verify stale Plan/financial retirement, reached-goal suppression, fresh retry and
complete 25-table restoration. Unchanged layout matrices and successful suites are not repeated.
APK signature/structure and all nine changed production-source PDB checksums match.
Native Release details and retained negative evidence are in the evidence document. Physical device, iOS,
OAuth, paid activation and publication are separate; ENT-02/03 and the commercial release are not complete.

Next ready slice: complete remaining original operation/import/restore/native bindings and retained-draft
rejection feedback under D-69; keep commercial activation and owner/external gates separate.
See [evidence](../../quality/reminder-input-work.md).

## D-160 - Structured write feedback and retained filter retry

Original structured write rejections now explain quota counts, required Plus/Pro/add-on access, exact-space
membership and original profile/resource selection in all six languages. The shared native reporter keeps
unrelated failures on their original path and never guesses purchase, renewal, provider or activation state.
Pre-write Goal/Budget/quick-resource feedback retains the form; committed-return/reload feedback remains
distinct and never falsely says that the accepted write was not saved. Counts use safe Int64 addition,
current formatting/digit choices and direction marks; stored identities and exception details are not shown.

Saving a new filter above capacity now reaches the established reviewed atomic writer for its structured
reason. A rejected operation retires its review; the next explicit menu invocation rereads it without clearing
the current query. Exact raw filter names return to the name prompt. Name drafts are process-local and bounded
by original file identity, independently of rights; another file never inherits them. No automatic retry or
financial save, schema/model/migration, SDK, permission, network, credential or portable preference is added.
Production financial/device registrations remain inactive; test builds stay unrestricted.

19 new AT-162 cases pass with actual sources/resources and real isolated SQLite. Final ordinary main passes
2,889 / App.Tests 832, no failures/skips, 2m 49.493s, followed by cleanup. A preceding main run passed 2,885
before the real second-invocation fix; only the changed final behavior justified another run.
Strict Windows and canonical complete Android Release have zero warnings/errors. Eighteen actual Windows
reason/Close/second-Invoke cases cover en/fa/de, light/dark, 360/412/wide, with unchanged complete 25-table rows
and exact owned development bytes restored. Fictitious menu/name effects are explicit ports; reason presentation
uses the actual native adapter and buttons. Android evidence, negative artifacts and full signed APK are in
the evidence document. Platform/provider/device/commercial activation and publication gates remain separate.

Next ready slice: expose the original capacity/permission reason before a blocked Save, keeping complete
drafts and history; continue the remaining ENT-04 screen and original enforcement paths under D-69.
Actual purchase/provider/renewal fields and activation remain independently gated.
See [evidence](../../quality/commercial-feedback.md).

Original configured SQLite targets now stay stable across open/closed Android reporting aliases.
The shared Data helper extends the existing D-146 approach to resource reviews, writers, Plan/reminder
packets, loan/backup boundaries and their unchanged foreign-file guards. Four added real SQLite cases
verify open readers/writers, coherent financial validation and rejection of another actual file.

## D-161 - Explain readiness before a blocked Save

New Goal and Budget editors explain their captured capacity, missing feature right or required explicit
selection before Save is disabled. The reason comes from the existing reviewed policy, uses all six real
translations and preserves complete raw drafts. WorkRestriction is an explanation, never a grant or a
replacement for the atomic writer. Existing budget net-definition/correction rules remain authoritative;
malformed bounded Goal choices require explicit repair, while unlimited access remains unaffected.
Production registrations stay inactive and test builds remain unrestricted.

Nineteen AT-163 actual-source/real SQLite cases pass. The final ordinary suite passes 2,908 tests,
App.Tests 851, zero failures/skips, in 2m 05.449s, followed by successful cleanup. Strict Windows and the
canonical complete signed Android Release build have zero warnings/errors. Thirty-six actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced, retaining complete drafts and all
25 tables; exact owned development bytes restore. Native Android regression and negative evidence are
reported separately in the evidence document. No schema/model/migration, permission, SDK, credential,
network or portable preference is added. Physical-device, iOS, provider and activation gates stay separate.

Next ready slice: complete remaining original blocked-action explanations and ENT-04 plan comparison/current
access presentation under D-69. Keep actual provider/renewal/purchase facts and activation independently gated.
See [evidence](../../quality/pre-save-readiness.md).

## D-162 - Read-only plan comparison and actual access

More now opens Plans and access: a read-only comparison of the owner-approved Free/Plus/Pro model and
original current local access for this exact file. Comparing tiers changes only in-memory presentation;
counts and local tools come from existing Core QuotaPolicy/PlanPolicy. Current inactive test access never
becomes a fake Free/Plus purchase. Personal rights, exact shared membership, the host's right and original
profile choice remain distinct. All history, correction, backup/restore and owned export rules stay intact.

Only built local tools are compared. Unavailable Pro online services are an explicitly separate roadmap;
optional cloud backup belongs to every tier. Plus Lifetime is the same local Plus set, without invented
renewal. No price, purchase, trial, renewal, cancellation, restore-purchase or provider status is inferred.
Continue returns without changing a plan or buying anything; actual Free has the Continue with Free caption.
The existing explicit active-item form opens without saving. Production registrations stay inactive.

Seventeen AT-164 actual-source/real SQLite cases pass. Final ordinary main: 2,925 passed, App.Tests 868,
zero failures/skips, 2m 05.555s, followed by successful cleanup. Strict Windows and canonical full signed
Android Release have zero warnings/errors. Eighteen actual Windows contexts cover en/fa/de, light/dark,
360/412/wide, all three native tier choices, complete realized row captions/values and native help/return.
Actual Release Android More/chooser/comparison/help/Continue and exact owned restoration are verified.
All eight changed production-source PDB checksums match. No schema/model/migration, permission, SDK,
network, credential or portable paid data is added. Device, provider, billing and activation gates remain
separate. ENT-04 and the commercial release are not complete.

Next ready slice: original Account/Plan blocked-action explanations with retained drafts/history under D-69.
Actual provider/renewal/purchase flows and activation remain independently gated.
See [evidence](../../quality/product-plan-comparison.md).

## D-163 - Original editor write feedback and committed return

Account and Plan Save now explain actual structured quota/permission rejection in all six languages,
retain complete raw drafts and place complete feedback beside the pinned Save. Successful writes cannot
be resubmitted after a failed return or follow-up; inputs become read-only and Cancel becomes Close.
Account/default-preference boundaries stay distinct, including a default update that committed before
its subscriber failed. Original change-notification failures after commit remain visible through a typed
committed outcome, never a fake pre-write rejection. Exceptions are not silently swallowed.

Both editors use original native-effect ports and actual platform-independent application sources.
Native query interfaces stay on pages. Plan picker rows retain only their actual plain-text identity/name/
selection, without unused native color/icon allocation. Account and Schedule edit copies preserve all
original fields/identities; Schedule also clones its mutable rule. A refused historical split cannot mutate
the original in-memory state, stop boundary or automatic-post anchor before an explicit corrected retry.
Original validation, signs, calendars, currency locks, category selection, split rules and atomic writers
remain authoritative. No EF property/schema, migration, permission, SDK, credential or portable-format change.

21 AT-165 cases pass; final main passes 2,946 / App.Tests 889, zero failures/skips, 2m 07.788s, followed by
cleanup. Strict Windows and canonical complete signed Android Release have zero warnings/errors.
36 actual Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Actual Save invokes
original atomic rejection/accepted writers over independent fictitious SQLite; raw drafts, pinned captions,
real input/Save enablement and Close are checked. Explicit fictitious return/consent effects are identified;
all 25 original tables and exact owned development bytes restore. Actual normal Android new-form/query
regression and exact owned restoration are separate from typed fixture/physical-device acceptance.
Production registrations remain inactive. Initial pre-attempt readiness and other operation/activation
coverage remain open; this does not complete ENT-04 or the commercial release.

Next ready slice: original Account/Plan pre-attempt readiness and remaining archival sequencing under D-69.
See [evidence](../../quality/editor-write-feedback.md).

## D-164 - Reviewed atomic account archive

Account archive is now one reviewed SQLite transaction: original archive state, affected active/paused
plans in either account direction, exact default cleanup and original persisted activity choices commit
or roll back together. The reader captures complete original account/plan values and the local day without
creating settings. The writer reacquires/rechecks complete inputs under the actual file's writer and rejects
retired rows, day, cached access, another file or a different original notification owner.

Original Account/Plan feature/capacity decisions are shared, not duplicated. Archiving ends related plans
with established PlanActions; unarchive consumes the original account slot but never resumes ended plans.
Another concurrently chosen default remains untouched. Both original stores publish only after commit;
publication failures retain the accepted result and cannot cause a repeat. The UI blocks concurrent actions,
requires explicit consent before discarding a dirty draft, never silently saves those financial inputs,
and freezes an accepted result with Close after follow-up/return failure.

17 AT-166 cases pass. Final main: 2,963 passed, App.Tests 906, zero failures/skips, 2m 10.406s, followed by
cleanup. Strict Windows and complete signed Android Release have zero warnings/errors. 36 actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced with original native Archive/Close,
real SQL rollback and actual accepted account/plan/default writes over independent fictitious rows.
Explicit fictitious consent/alert/return effects are identified. Actual normal Android consent-decline
and exact owned restoration remain separate from typed fixture/physical-device acceptance. All four
changed production-source PDB checksums match. No EF property/schema, migration, permission, SDK,
credential, network or portable-format change. Production registration stays inactive; initial readiness
and other commercial paths remain open, not release completion.

Next ready slice: original Account/Plan initial readiness before an attempted write under D-69.
See [evidence](../../quality/account-archive.md).

## D-165 - Original Account/Plan pre-attempt readiness

Account and Plan forms now explain original feature/capacity/selection decisions before an attempted
Save. The read-only review uses each original writer's actual configured file and cached access. Active
reads capture complete original rows and persisted choices in a deferred SQLite reader; inactive test
access performs no financial-row/choice read or paid activation. A missing persisted choice counts all
original eligible rows, matching the writer instead of a cached fallback subset. Original correction,
retained advanced-rule and historical net-slot behavior stays available.

The original advanced Plan classification is shared through Core PlanEditPolicy. Save and readiness build
the same detached candidate; readiness does not parse money, validate financial input, mutate original
state or grant a write. Explicit Refresh access keeps every raw amount/date/note/reminder/selection.
A failed or retired read disables Save with translated feedback; an actual writer rejection still retains
the draft and requires explicit fresh review. Atomic writers remain the final authority. No EF property,
schema/migration, permission, SDK/network, credential or portable-format change. Test limits stay inactive.

Fifteen AT-167 cases pass with actual application sources and isolated SQLite. The original 21 AT-165
cases also pass: known full quotas now disable Save before attempting it, so original writer-rejection
cases capture a free slot and then exercise a real later addition/access change. They still prove original
atomic rejection and an explicit corrected retry. Final main: 2,978 passed / App.Tests 921, zero failures
or skips, 2m 12.110s, followed by cleanup. Strict Windows has zero warnings/errors.

ENT-02/03/04 remain in progress. Next ready slice: original import/Undo and restore action feedback,
retaining reviewed data and accepted outcomes. Provider/billing activation, encryption/recovery choices,
real OAuth, phone and iOS/Mac/signing remain independent owner/external gates.
See [evidence](../../quality/editor-readiness.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) prove native disabled Save, complete reasons/refresh, retained input/read-refusal/retry
and all 25 unchanged original tables. Normal final signed Release Android verifies actual refresh and
Cancel with retained native fields, unchanged original rows, existing D-141 migration only and exact
owned byte/sidecar restoration. Final Release is installed/stopped. Canonical full Android build has
zero warnings/errors; all six changed production-source PDB checksums match. The installable APK is
artifacts/android/zanance-d165-release.apk (82589781 bytes); signature/structure evidence is recorded
in the focused quality document. No physical-phone, iOS, OAuth, billing or release acceptance is inferred.

## D-166 - Original data-file outcomes and retained review

Original ledger import, holdings import and durable Undo now retain the original accepted result across
post-commit change-subscriber failure. A typed completed notification carries the exact counts/batch or
Undo result; original atomic writers, metadata, overlap rechecks, rights and rollback stay unchanged.
The application retires the accepted reviewed payload before any history/failure follow-through and
removes an accepted Undo row before fallible reload. A retained old command cannot repeat that batch.
A complete history read publishes only after success, preserving other original rows on failure.

Structured pre-write rejection is shown beside the retained preview in all six languages. Successful
follow-through failures state the completed outcome instead of pretending it rolled back. Picker
cancellation keeps the existing file/preview/choices. One busy lifetime spans Undo consent/write and
export authentication; explicit fresh review retires failed original evidence without silently saving.
Picker, existing app-lock confirmation and UTF-8 BOM cache/share effects are native adapters. Actual
non-UI application sources are compiled and tested through explicit ports, never stand-in MAUI controls.
Pinned complete feedback/review/import actions reserve a growing row outside the viewport. Original
history identity and growing Undo action have separate rows. No new feature policy, paid activation,
schema/model/migration, SDK/permission/network, credentials or portable-format change.

32 AT-168 cases pass with actual application sources, real isolated SQLite and all six resource languages.
They cover original ledger/holdings permission refusal and retry, post-commit publication outcomes,
nonrepeatable accepted import/Undo, real history-read failure with other batch retention, picker/consent
cancellation and busy guards, explicit read-refusal/retry, security/export ports and neutral complete CSV,
real SQL-trigger whole-transaction rollback, plus explicit aggregate links and complete restored metadata.
Final ordinary main: 3,010 passed / App.Tests 953, zero failures/skips, 2m 42.936s, followed by cleanup.
Strict Windows and canonical complete Android Release have zero warnings/errors. All seven changed
production-source PDB checksums match. Device/platform/provider acceptance remains separate.

ENT-02/03/04 remain in progress. Next ready slice: original generic mapped-CSV policy binding while
keeping owned-data recovery and atomic import/Undo intact. Restore follow-through, other commercial
paths and verified billing/provider activation remain open. Encryption/recovery decisions, real OAuth,
phone and iOS/Mac/signing are independent owner/external gates.
See [evidence](../../quality/import-outcomes.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) verify original native review/import/Undo, retained refusal and cancellation, complete
pinned reasons/results/targets, accepted subscriber-failure semantics and all 25 original tables restored.
Normal final Release Android verifies DocumentsUI picking, actual preview refresh, picker cancellation,
Import and consented Undo, unchanged original rows, existing D-141 migration only, exact owned bytes/
sidecars and Main/English/System restored. The new owned CSV is removed; final Release is installed/
stopped. APK: artifacts/android/zanance-d166-release.apk (82175915 bytes), signature/structure and all
seven production-source checksums verified. Physical-phone/iOS/OAuth/billing/activation stay separate.

## D-167 - Restore completed before fallible follow-through

Original all-source recovery is recorded before display/onboarding/lock/work/root completion. Retire
accepted input; explicit completion retry never repeats replacement/safety. Preserve old review on new
read failure; uncertain partial-source failure requires fresh inspection/consent. No new commercial,
security, schema, SDK, permission or portable policy. Test enforcement remains inactive.

35 AT-169 cases; main 3,045/App.Tests 988, zero failures/skips; strict builds zero warnings/errors;
36 Windows contexts and normal final Release Android picker/consent/source/root plus complete 25-table
readback verified. Owned bytes/sidecars/Main/English/System restore; final signed Release stopped.
APK: artifacts/android/zanance-d167-release.apk. [Evidence](../../quality/restore-outcomes.md).

Next: finite current ENT-02/03/04 entry-point inventory and confirmed gaps only. The earlier extra generic
mapped-CSV proposal is withdrawn: the approved matrix already includes CSV in Free, Core allows it and
original import/restore deliberately recovers owned data above quota. Historical unfinished slice notes
must be reconciled against current source. LOC-05/10 owner texts and encryption/provider/device/iOS/
billing/release gates remain open; the commercial release and continuous goal are not complete.

## Current completion scope (D-168)

Use [07-current-entry-point-inventory.md](07-current-entry-point-inventory.md) for implemented boundaries,
three confirmed ready operation groups and owner/external dependencies. Next: forecast computation/scenarios
and the original saved-snapshot writer, then receipt recognition, then loan/report/PDF analysis. Historical
partial-slice remaining-work notes are not new gaps. No app code, policy, activation or acceptance change;
D-167 tests/builds/native/APK remain the baseline without repetition.

## Delivered original forecast group (D-169)

Original Basic/Advanced computation, retained draft feedback, atomic multi-currency snapshot Save and
Free saved values/Delete are implemented. Currency-safe legacy comparison is verified without stored
rewrites. Main 3,074 tests, strict builds, native checks and signed APK completed locally.
See [evidence](../../quality/forecast-work.md). Next: receipt recognition, then loan/report/PDF.
ENT-02/03/04 and owner/external gates remain in progress; commercial registration stays inactive.

## Delivered original receipt group (D-170)

Shared actual-file recognition, original source rechecks and typed editor destination are delivered.
Main 3,104 tests, strict builds, 36 Windows contexts, both actual Release Android digital-PDF callers
and full signed APK verified. No commercial activation or external corpus/device acceptance.
[Evidence](../../quality/receipt-work.md). Next: loan/report/PDF, followed by the bounded ENT-02/03 audit.
ENT-04 provider facts and owner/external release dependencies remain open.

## Delivered original loan operation (D-171)

The actual loan calculation/confirmation/writer portion of group 3 is delivered locally with original
Free balances/manual repayment/warnings. Main 3,141, strict builds, 36 Windows contexts, normal Release
Android schedule/consent/payment readback and full signed APK verified. No commercial activation.
[Evidence](../../quality/loan-work.md). Next: original report packages and advanced PDF, then bounded
ENT-02/03 completion audit. Billing/provider/owner/external gates and ENT-04 remain open.

## Delivered original reports/PDF (D-172)

28 AT-173 application/real SQLite cases; ordinary main 3,169 / App.Tests 1,112 pass with zero
failures/skips (2m 42.749s), followed by test-output cleanup. Final strict Windows and canonical complete
Android Release builds have zero warnings/errors. Thirty-six en/fa/de, light/dark, 360/412/wide and
Simple/Advanced Windows contexts exercise real package targets, original calculations, Free refusal
without changing selection, detached actual PDF and unchanged original inputs. Final targeted PDFs
also retain Free account opening/movement/closing tables and per-account currency scopes; native goal
suggestion acceptance is checked in en/fa/de with its named-calendar rule and no ledger posting.
Normal signed Release Android exercises original report/history, actual PDF cache/platform share,
share-sheet cancellation and complete retained choices. Owned tables, cache, baseline bytes/sidecars
and Main/English/System are restored; final Release installed/stopped. Commercial registration remains
inactive. Physical phone/iOS/OAuth/billing/Store/performance/full-product acceptance remain separate.
All three confirmed computation groups in the D-168 inventory are locally delivered. Retain base
balances/status/export, detached original PDF and the same reviewed contribution writer/calendar.
[Evidence](../../quality/report-work.md). Next: bounded current ENT-02/03 completion review; do not
rebuild delivered resource/recovery groups or fabricate ENT-04 provider/purchase facts.

## Bounded current commercial review (D-173)

The current local ENT-02 operation boundaries and ENT-03 choices/history/automation are implemented;
the finite review after D-172 is closed. This does not activate limits or implement server grace/retention.
The ordinary D-172 3,169-test evidence and boundary-specific native records remain unchanged; no new
build/test/device execution is claimed for this source/documentation review. ENT-04/05, billing/shared
server work, QA-01/02/07 and the full release stay open. Next ready engineering: A11Y-01 Android
accessibility review, without claiming physical-device acceptance. [Evidence](../../quality/commercial-completion-review.md).

## Android widget accessibility continuation (D-174)

Actual quick widget roles/targets, original-language captions and RTL, cold persisted locale and
unsaved original intents are locally delivered. Twelve native widget cases; main 3,169; strict builds
and full signed APK. A11Y-01 remains in progress: TalkBack is inactive and no spoken/full-screen/phone
acceptance is claimed. [Evidence](../../quality/android-widget-accessibility.md). Continue actual
remaining accessibility findings; do not repeat delivered commercial operation reviews.

## Contextual original date inputs (D-175)

Fix observed generic start/end date names; bind the existing purpose on all 26 original usages.
Native Windows name/draft/calendar cases and original Release Plan/debt fields verified, no Save.
Main 3,169, strict builds and full signed APK; data/display restored. A11Y-01/02/03 remain open;
this does not accept spoken navigation or physical/iOS/system scaling. [Evidence](../../quality/date-context-accessibility.md).

## Current startup baseline (D-176)

QA-06 records three bounded D-175 Release starts with preserved actual native evidence and no source
change. Next ready implementation is protected original startup preparation outside the initial UI
call, preserving database/display/security/link/foreground order. Duration/ANR/device/iOS acceptance
remain open; do not claim the two-second goal. [Evidence](../../quality/startup-current-baseline.md).

## Original Home section reuse (D-177)

Unchanged native sections remain attached; only displaced original containers move. Default source
order matches Core. Main 3,169, strict builds, 18 Windows contexts and complete signed APK verified;
six normal Release Android Home/return cases and exact owned-data/display restoration verified. Async startup/appearance-gating candidates were rejected and
removed after finite native timing; do not repeat them without new evidence. QA-06 and full release
remain open. [Evidence](../../quality/home-layout-reuse.md).

## Original asset-event slices (D-178)

Home uses one original type index per projection, retaining the original checked quantity calculator.
Ten new cases; main 3,179, strict builds, bounded exact Core computation and six Windows caption
contexts pass. Six normal Release Android caption/return cases and explicit QA03 fixture/Main/display restoration verified. QA-06 and
whole release remain open; do not repeat unrelated completed layout or commercial reviews.
[Evidence](../../quality/asset-event-index.md).

## Retained original icon names (D-179)

Fix the actual stale native icon caption on live language changes. Preserve original keys, selection
and native targets; lifetime-scoped translation refresh does not rebuild forms or save financial data.
Native Windows/Release Android checks and complete APK verified locally. A11Y-01/02/03 remain open; do not repeat
completed commercial or unchanged layout reviews. [Evidence](../../quality/icon-language-accessibility.md).

## Official Community prerequisite (D-180)

Owner selected current official Community source without custom crypto/fork. Source/tool pins and
independent Windows C/.NET key, tamper, original money/date and backup/export checks verified.
Main app remains plaintext/unchanged and retains D-179 test/APK baseline. Next: production native
recipe/package and actual EF compatibility, then Android Release proof; security suites,
reproducibility, iOS/key/recovery/migration remain open. SEC-01 stays in progress; do not repeat
delivered local UI/commercial work or use the deprecated probe bundle.

## Windows Community producer progress (D-181)

Official source/tool module, assembly-enabled pinned Windows build/local package and original EF
compiled-model compatibility harness are implemented independently. Eleven input checks and 32
actual migrations plus original financial packets/key/tamper/FK checks pass. Native reproducibility
and packaging evidence are in the build guide. SEC-01 remains in progress; next ready work is
Android native slices and upstream security suites/CI, then final key/crash migration integration.
Main app and D-179 APK remain unchanged/plaintext. Do not repeat completed UI/commercial work.

Fresh Windows DLL/package hashes match across two clean builds; both final original-schema
harness runs, native export/system-only dependency inventory and package CRC/native checks pass.
Upstream logs retain 749 warning occurrences each; warning/security review and upstream suites
remain open. These are local candidates, not warning-free production binaries or app activation.

## Official crypto-suite progress (D-182)

Windows matching-candidate OpenSSL stock suite PASS: 347 files/3,818 reported tests, explicit
59 whole-recipe skips and unchanged native hash. Bounded warnings review retains unresolved
adoption scope. Main 3,179/D-179 app/APK unchanged and plaintext. Next: official r30 native Android
slices and SQLCipher upstream tests, then platform/key/crash-migration integration. SEC-01 remains
in progress. Do not repeat the completed Windows suite without a change, failure or concern.

## Android native producer progress (D-183)

Locked official r30 API24 arm64/x64 native builds, canonical archives, ELF/hardening/negative
checks and deterministic identical-input packaging verified. Actual x64 emulator independent key/
tamper/financial-packet probe passes with exact owned cleanup. Main app remains D-179/plaintext.
SEC-01 stays in progress: SQLCipher upstream suite, native CI/iOS, managed Android/schema, platform
keys/recovery/crash migration and arm64/16 KiB/minimum-API physical gates remain. Do not repeat
completed native builds without a change/failure/concern or completed UI/commercial reviews.

## Stock cipher suite and Native CI progress (D-184)

Actual official SQLCipher suite: 226 tests/zero errors/reported no leaks, with checked runtime
identity and source-built static crypto. Maintained runner and initial separate Linux Native CI
are implemented; hosted run is pending acceptance, not a green result. Windows/iOS native CI,
managed mobile/schema, runtime/package matrix, keys/recovery/crash migration and device gates
remain. Main app/APK unchanged/plaintext; SEC-01 remains in progress. Do not repeat passed
Windows/OpenSSL/SQLCipher/UI work absent a change, failure or unresolved concern.

## Managed Android compatibility progress (D-185)

Separate signed Release AOT/trimmed managed probe passes actual 32-migration compiled-model
financial/settings/key/tamper behavior on x64 emulator. Windows shared probe and artifact checks
pass. Actual data registration preserves D-44; initial omitted-registration deadlock retained.
Main app/APK unchanged/plaintext. SEC-01 remains in progress: native hosted/iOS matrix, final
platform keys/recovery/crash migration and device acceptance. Do not repeat passed schema/UI
checks unless a change, failure or unresolved concern requires them.

## Memory portable boundary progress (D-186)

Final reusable memory export/read-only import/encrypted staging primitives implemented without
activating them on live data. Six tests/full 3,185, strict builds and actual Windows/Release Android
schema/header/history/restore proof pass. Live databases remain plaintext. SEC-01..08 continue:
key/authorization integration, durable copy/verify/commit/cleanup/recovery and remaining native
platform/CI/device gates are not satisfied by these primitives. No temporary OS policy is added.

## Complete staging verification progress (D-187)

Logical schema/header/all typed row/duplicate/exact byte and relational/cipher integrity verifier
passes twelve new cases, full 3,197, strict builds and actual Windows/Release Android staging proof.
Live databases remain plaintext; durable keys/generation commit/restart/cleanup/recovery and
remaining native platform/hosted/device gates are still open. Do not repeat completed native/UI
checks absent change, failure or unresolved concern; the initial native metadata crash is retained.

## Generation checkpoint progress (D-188)

Immutable non-secret profile/generation transition model and eight behavior cases/full 3,205 pass.
Original remains authoritative until Selected. SEC-05 is not complete: durable storage/key pairing,
selector reopen, exact cleanup and restart fault injection still require the actual coordinator.
No live encryption/provider/backup policy activation; OD-04 and OD-10 remain owner gates.

## Resumable coordination progress (D-189)

Final coordinator checks actual checkpoint/readback, captured context, candidate reopen before
selection/cleanup and refuses plaintext fallback on missing key. Fifteen cases/full 3,220 pass.
SEC-05 remains in progress: native durable key/selector/cleanup adapters, real restart/fault tests
and owner OD-04/OD-10 policy are not accepted by faultable-port tests. No app provider activation.

## Native CI ownership correction (D-190)

Hosted build stopped before compilation: fixed native UID lacked runner output permission. Shared
host non-root identity and fresh work bind correct Android/upstream mounts; six focused Docker
checks/parser/YAML pass. Corrected hosted native suite remains pending. No unchanged app tests,
UI or APK regenerated; final encryption/platform/recovery acceptance remains open.

## Native parent traversal follow-up (D-191)

D-190 corrected output ownership but mode 750 on build home blocks foreign UID traversal. Actual
old image failure reproduced; only parent traversal changes to 0755, with no foreign parent writes.
Seven focused cases/exact reviewed host pass. Full corrected hosted [38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) completed successfully with matching
clean ABI outputs, twelve ELF refusals and 226 cipher tests/zero errors. Linux hosted native
build/suite gate is closed; app/key/recovery/other-platform acceptance remains open. Unrelated
checks were not repeated.

## Accepted policy and Windows key primitive (D-192)

OD-04/OD-10 settled on 2026-10-13; do not ask again or implement an interim OS exclusion. Windows
CurrentUser DPAPI authenticates full binding, refuses malformed/foreign keys and clears owned key
copies. Twelve new/full 3,232 cases and actual two-process encrypted reopen pass. SEC-03 remains
in progress: persistent native storage, Android/iOS adapters, final migration/recovery UX and
OS exclusion coupled with full encryption are not implemented by this primitive.

## Android key primitive (D-193)

SEC-03 now has original Android Keystore adapter as well as Windows DPAPI. Three independent
Release emulator processes prove protected creation/encrypted reopen and fifteen behaviors;
reported software level 0 is not hardware acceptance. No main activation. Durable key storage,
optional recovery, crash migration and iOS remain. Approved OD-04/OD-10 are not decision blockers.
Strict builds pass; unchanged managed 3,232 baseline retained, no unrelated reruns.

## Android envelope files (D-194)

SEC-03 adds immutable Android private-file envelope store with authenticated bounded/no-follow
reads and exclusive-create/file-directory flush. Torn files refuse; whole-file atomicity not claimed. Independent process stored-key encrypted
reopen and eight storage behaviors checked. Actual publication-crash matrix, durable selector,
Windows/iOS files, optional recovery and all-profile activation remain. Main remains plaintext.
No decision required beyond approved OD-04/OD-10; physical platform/release gates remain separate.

## Transactional selection and native kill/restart (D-195)

SEC-05 now has original concrete SQLite selector storage, not only coordinator ports. A versioned
private non-secret protocol file enforces bound identities, ordered CAS and DELETE/EXTRA. Thirteen
behavior cases pass (focused 82). Actual native hot-journal rollback and committed selection after
external process kill pass Windows and independent Release Android. No whole-session activation:
key/candidate/cleanup integration, all-profile mapping, recovery, crash/disk matrix and iOS remain.
Main is still plaintext. Owner policy OD-04/OD-10 remains approved, not a repeated question.

## Concrete native session (D-196)

SEC-05 now joins actual Android key/filesystem, coherent memory snapshot, typed candidate and
retained encrypted rollback, transactional selection and exact cleanup. Independent real-model
Release three-profile process sequences pass export/Selected interruption and missing selected
key refusal. Ten guards/focused 92 pass. Main remains plaintext: lifecycle/catalogue/all-profile,
backup/recovery/OS exclusion, other platform effects and broader crash/disk acceptance remain.
SEC-08 is not closed by an encrypted migration rollback alone; actual restore safety remains.

## Protected EF publication (D-197)

SEC-02 has an original actual-factory protected connection adapter: atomic bound publication,
noncreating exact path, no path-only key cache, provider/ciphertext checks and stale EF retirement.
Actual native original-model factory read/write and ten guards pass (focused 102). Main remains
plaintext; reviewed provider packaging/lifecycle/catalogue/all-profile and generation-aware backup/
restore/recovery composition remain. Plain legacy backup format guard is permanent, not interim
OS policy. All other security/physical/platform release gates remain.

## Protected portable backup writer (D-198)

SEC-02 integration adds actual keyed factory/commercial portable memory export, typed native proof
and mid-output publication retirement. Supersedes keyed-write refusal in D-197; keyed generation
restore remains pending/fail-closed. Main remains plaintext/current backups unchanged; receiver,
provider/catalogue/startup/all-profile/recovery/OS policy and other security gates remain. SEC-08
restore safety is not closed by this export path.

## Restored encrypted staging (D-199)

SEC-02 adds bounded memory portable import into fresh keyed staging, typed validation and actual
old-schema upgrade, verified in independent original-model Release Android. Six guards/focused
108 pass. This does not close SEC-08 safety/package rollback or main restore dispatch/selection;
all main provider/lifecycle/catalogue/recovery/all-profile/OS policy gates remain.

## Restored selection / encrypted previous data (D-200)

Actual selector creates/fully verifies encrypted current-data rollback before ordered commit,
keyed reopen and same-run selected restart. No key replacement/plaintext fallback/original deletion.
Six guards/focused 114 and actual native different old/new packets after external termination pass.
Outer package transaction/source rollback/catalogue publication/safety/key retention remain; main
provider/startup/all-profile/recovery/OS policy not active. SEC-08 is not complete from this primitive.

D-201: actual BackupService now has exclusive optional validated package dispatch with no legacy
fallback and scoped decoded buffers. SEC-01/SEC-07 remain incomplete: register and implement the
protected complete-package receiver, durable all-source recovery and final profile publication.

D-202: protected package receiver composes database staging/selection and encrypted original/restored
portable preferences with replay before endpoint publication. SEC-01/SEC-07 remain open: main provider/
all-profile/startup registration, interrupted early-stage reconciliation, global lease and key/retention/
recovery/OS exclusion acceptance remain.

D-203 connects lifetime ownership to the original local factory and base DbContext, with explicit
maintenance contexts/publications. SEC-01 remains incomplete: main startup/all-profile orchestration,
provider/candidate security acceptance, stable metadata/key lifetime, recovery/retention and final OS
policy need completion. The unchanged default native dependency still requires replacement.

D-204 connects actual reviewed profile switching to D-203 ownership with no self-deadlock and
correct queued-work rollback after refused authentication. SEC-01 still needs native/provider security
acceptance and protected catalogue/all-profile/startup/key/retention/recovery/OS-policy activation.

D-205 coordinates actual inactive profile deletion with D-203/D-204 ownership and stale/cancelled
review refusal. Final protected profile/generation/key/rollback retention/deletion, provider/candidate
acceptance, metadata/startup and recovery/OS policy remain open; main databases are plaintext.

D-206 persists initial installation/key-profile/generation identities without read-time regeneration.
SEC-01 still needs main bootstrap/provider/live catalogue/later-restore references and protected
retention/deletion/recovery/OS policy integration and platform acceptance. Main remains plaintext.

D-207 joins actual initial identities/journal/native keys/migration with retained-key protected
publication. SEC-01 still needs main provider/bootstrap/live restored registry/all-profile/startup,
key lifetime/recovery/retention/deletion/OS policy and platform acceptance; no main activation.

D-208 retains ordered restore intent/ancestry and links actual receiver selection readback. Main
live generation resolution/provider/startup, one-time durable replay/later preference writes,
key/rollback/reference retention/deletion/recovery and OS policy remain; main is plaintext.

D-209 fixes protected replay overriding later preferences, with actual-persistence-before-marker
ordering. Main durable platform effects/live resolver/provider/startup, key/rollback/reference
retention/recovery/deletion and OS policy remain; main databases are plaintext.

### D-210 — Persist original Android MAUI preferences before recovery completion

The Android host now registers ISettingsPersistence against the exact Preferences.Default backend
used by MAUI 10.0.110. Native PreferenceManager selects the same default SharedPreferences; a custom
IPreferences backend is refused rather than writing a guessed file. Persistence runs off the UI thread.
A fresh device-local operational marker forces a real snapshot write, rather than an empty editor
that may skip writing. Commit failure or cancellation throws; the protected receiver must not mark
its capsule Applied or publish until this completes under exclusive preference-writer ownership.
The marker is neither a secret nor a portable preference. Windows/iOS persistence, live generation
resolution, final provider/startup/recovery and sensitive-file OS exclusion remain open. The main
application database remains plaintext; registration alone does not activate encryption.

Focused acceptance compiles the original MauiSettingsStore and native persistence adapter with
actual MAUI Essentials 10.0.110 in the independent Android package. It writes only fictitious values,
checks cancellation, terminates the process, verifies a retained value and removed key in a cold
process, then cleans its own values. This is Android x64 API36/4 KiB evidence, not phone, iOS,
hardware-key, whole-application encryption or product-release acceptance.
### D-211 — Retain the restored generation through cold startup composition

SqlCipherProfileGeneration.OpenRestoredAsync resumes the actual latest package under the current
location's exclusive maintenance owner. It requires the receiver's exact private restore-history
store and installation/profile key namespace before native effects. Pending preference replay and
actual candidate/rollback/journal verification finish before retaining the exact existing key.
Readback requires the latest selected target; missing or mismatched history fails closed. No original
or initial-generation fallback, key preparation, directory guessing or early publication is added.
The retained key owner verifies the provider/integrity again and publishes through the same original
protected factory lifetime used by initial migration. Its disposal still refuses new connections.

The receiver has an internal no-publication resume path for this composition. A stale publication
callback from another process is never invoked; the current host publishes only after obtaining the
retained key owner. The public restore entry retains its existing behavior. Automatic all-profile
startup routing, main provider adoption, early staging reconciliation, optional recovery and retained
file/key lifecycle remain open. Main application storage remains plaintext. This change is not a
claim of complete encryption, device/iOS/OAuth or release acceptance.
### D-212 — Resolve the current protected generation from stable ancestry

OpenCurrentAsync reads the actual existing installation/profile catalogue and requires history's
complete initial key binding. Missing/corrupt/foreign history never implies initial selection and
cannot manufacture migration state. Row zero routes through actual initial migration/reopen;
any recorded restore intent routes through the captured latest receiver and retained restored owner.
Receiver target/parent/private directory and exact history are validated, including unresolved intents.
No enumeration, latest-file guess, key replacement or initial/plaintext fallback is introduced.

The host explicitly initializes history only after verified initial migration; interrupted bootstrap
still fails closed for reconciliation. This automatic composition is not yet registered in the main
startup/provider/profile lifecycle. Native producer acceptance, all-profile live adoption, recovery
and retained-file/key policy, early staging reconciliation and final sensitive OS exclusion remain.
No additional owner decision or commercial activation follows; main storage remains plaintext.
### D-213 — Preserve protected publication when a profile switch is refused

The original profile switch captures its complete publication inside the actual maintenance owner,
then asks the native runtime to open/migrate the destination. On authentication refusal, retired
catalogue or failed opening, rollback restores the original path and bound connection creator.
A path-only MoveTo cannot restore encrypted storage and is no longer used for this rollback.
Rollback is one-use, requires its exact live owner, never revives retired protected context identity,
and refuses a plaintext snapshot after protection is required. The host retains underlying key
owners until the transition finishes; no credentials are exposed by the publication token.

IProfileRuntime.OpenDatabaseAsync is the explicit native destination effect. The existing default
opens/migrates plaintext storage; a protected location rejects it. Final protected hosts supply
verified keyed publication, never strip protection or recreate missing plaintext. The actual MAUI
host's protected generation resolver/lifetime registration and final startup/all-profile/recovery/
retention/provider/OS-policy adoption still remain open. Main database encryption is not activated.

Concurrent initial Android/test builds hit CS2012/MSB3248 locked shared DLLs; those negative logs
are retained. Successful Windows and independent Release builds were not repeated. Only failed
Android and full test executions were retried once, sequentially after competing builds completed.
### D-214 — Own active and tentative profile keys through the real transition

SqlCipherProfileSession retains the active verified generation while a destination is tentative.
The actual ProfileService invokes finalization after committing its reviewed catalogue or restoring
its complete previous publication. The MAUI runtime forwards opening/finalization to the registered
protected session; existing plaintext hosts retain their original path and never bypass protection.
Accepted switching clears the previous mutable key owner; refused switching clears only the
candidate after the original publication is verified. No envelope, database or rollback is deleted.

Start, switch, acceptance, refusal and close require the actual live maintenance owner. Key retirement
also requires all owned contexts to be disposed. Concurrent resolution is serialized and synchronous
finalization refuses active resolution. Publication identity must belong to the exact retained owner;
keys cannot be retired based on a path string alone. Main registration/covered async startup, early
bootstrap/replay reconciliation, recovery/retained-file lifecycle, native provider and final OS policy
remain open. The main database is still plaintext; this is final lifecycle composition, not activation.

A protected host may resolve another catalogue-bound profile in the same private directory while
retaining the previous publication. It never passes through an unkeyed endpoint. Plain/foreign-root
initialization guards remain enforced, and actual journals, history, keys and files must still verify.
### D-215 — Record initial ancestry before native key migration

Initial ancestry can now be captured as pending against the exact existing Original journal,
before keys/candidates are prepared. Pending row zero survives restart but cannot reserve a
restore transition. OpenCurrentAsync resumes the actual initial migration, retains/reopens its
existing native key, then mirrors Completed journal state into selected initial ancestry before
returning a publication owner. Missing/foreign history still never falls back or regenerates.
Existing verified initial history remains supported; schema/version is unchanged and typed guards
still apply. New pending state is not evidence of a completed migration or permission to open data.

The old ordering left a crash interval after initial migration but before history creation. The
new pre-key intent closes that interval through actual captured resume. Interrupted metadata
creation itself, all-profile host bootstrap, recovery UX, provider/live registration and OS policy
remain separate unfinished work; missing or corrupt records still fail closed. Main application
storage remains plaintext. No owner decision, platform key policy or financial schema changes.
### D-216 — Publish complete identity metadata from captured staging

Identity bootstrap now commits and fully validates a private .creating catalogue before publishing
its final path with a no-replace same-directory move. A valid staged catalogue is resumed with its
original installation/profile/generation identities and exact known profile set; no new random ids
are generated on that path. Missing/corrupt/foreign incomplete staging fails closed rather than
being overwritten. Existing final metadata or sidecars still reject new bootstrap. Read never creates.

An explicit native durability overload synchronizes staged file/directory before rename and the
parent after publication. Managed-only callers retain transactional/typed/no-replace checks but do
not claim platform power-loss qualification. Failure after final rename requires actual existing
read/reconciliation, not another CreateNew or replacement. Other journal/history creation protocols
and main lifecycle/provider/OS policy adoption remain unfinished; main storage remains plaintext.

Independent Android x64 API36/4 KiB source-built SQLCipher probe pauses after actual fsync and typed
staged read, before final publication. The external runner terminates that process. Cold resume
requires the final path absent, publishes the same staged identity fingerprint, verifies actual
initial journal binding and idempotent/new profile registration preserving installation. No keys
are prepared, app encryption activated or scenario repeated. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-58220434415e4174bc471b9760cd95e6.json,
run f93ba289610e4355aee1b0446d65bc9a. Reviewed APK ABI identities/signature verified; independent
package uninstalled. Strict Windows 26.63s, Android 17.07s and independent Release 71.92s builds
have zero warnings/errors. Script parser checked; test cleanup waits for terminal builds/tests.
### D-217 — Publish complete migration and history metadata

Generation journal creation and initial restore-history creation now use the same verified staged
publication as identities. Existing/corrupt/foreign final files and sidecars are never replaced;
read never creates. Captured Original journal state and exact initial pending/selected history
must match on staged resume. Advanced journal staging cannot become a fresh Original, and a pending
history cannot be republished as verified initial selection. Typed schemas/versions remain unchanged.

Native overloads synchronize file and directory around no-replace publication. Managed-only effects
do not certify native power loss. Incomplete/corrupt staging still fails closed for reconciliation;
this does not regenerate missing keys or certify complete startup/recovery. Main provider/all-profile
bootstrap/covered async startup, recovery/retention and final OS exclusion remain open/unactivated.

Independent signed Release AOT/trim Android x64 API36/4 KiB interrupts journal publication after
actual staged fsync, resumes that exact journal, then interrupts pending-history publication after
its actual fsync. Third process resumes both bindings, retains pending initial ancestry and confirms
staging aliases gone, without preparing keys or writing financial data. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-442b5d696a9a4a8699baf81c8b31218d.json,
run 18d182139b834d9b84bd34178fd54ef1. Runner clears only its own prior metadata barrier before
its second phase; no scenario repeat/reinstallation for failure or unrelated native suite/UI matrix.
ABI identities/signature verified and package uninstalled. Strict Windows 18.74s, Android 17.54s
and independent Release 59.16s builds report zero warnings/errors. Parser checked.
### D-218 — Compose preparation of all captured existing profiles

SqlCipherProfileBootstrap joins actual identities, staged journal/history creation, pre-key initial
intent, concrete migration/current-generation resolution and temporary verified key ownership for
every captured existing profile. The actual host maintenance owner excludes ordinary contexts;
private unpublished locations supply local ownership without moving the application's factory.
Only after this pass may the host's protected session publish the selected profile.

The original map must be complete, distinct, absolute and in the real location's private directory.
Existing metadata must match the captured profile catalogue. Missing installation is not inferred
as fresh: explicit initialization still refuses missing/redirected/non-plaintext originals or retained
key/migration/history/cipher state, unless resuming captured valid catalogue staging. Read-only
startup never creates identities. Missing financial databases are never manufactured here.

Native sync qualifies metadata creation; actual journals/history/current files and keys are reopened.
All encrypted rollback/envelopes remain retained, and temporary key owners are disposed before the
next profile. No intermediate application publication or fallback is added. New empty profile
creation, full restore callback/lifecycle registration, covered main async startup, provider adoption,
recovery/retention and OS policy remain open. Main application storage is still plaintext.

### D-219 — Initialize new profiles without plaintext financial files

SqlCipherNewProfile creates the actual current schema in an unpooled in-memory database and exports
bounded memory into verified encrypted candidate and rollback files. A dedicated new-profile intent
captures explicit creation separately from existing-profile migration. Missing existing state never
authorizes an empty replacement. Existing originals/sidecars, unknown ciphertext and foreign or
selected ancestry fail closed before schema/key work. Native synchronization qualifies checkpoints
and the no-replace rename into the existing generation journal. The normal current-generation owner
then verifies the retained key before application publication.

An interrupted unpublished empty-schema attempt may rebuild only under its captured KeyPrepared
intent, using the existing key. The original financial path never exists on disk. Both encrypted
packets are fully compared before selection; later ordinary writes are retained on cold open.
This infrastructure is not yet registered in the main product's profile creation/startup. Main
storage remains plaintext; provider adoption, lifecycle/catalogue registration, optional recovery,
retention and coupled final OS backup policy remain unfinished. No new owner decision is required
for this step; OD-04 and OD-10 remain the approved final policy.

### D-220 — Prepare new protected profiles before catalogue publication

The real Profiles Add command now awaits ProfileService.CreateReviewedAsync. Automatic posting is
serialized, existing contexts drain, native preparation finishes without moving the active endpoint,
and the original catalogue/rights are rechecked before publishing the new profile and selected slot.
No await holds the synchronous catalogue gate. Failed preparation, cancellation or a retired review
cannot publish the proposed profile. The synchronous legacy creation API refuses protected storage.

SqlCipherProfileSession retains its current key while an explicit native new-profile callback prepares
an unpublished destination; pending transitions or unavailable callbacks fail closed. MAUI forwards
to this path only when the protected session is registered. Existing plaintext hosts preserve lazy
creation. LocalDatabaseLocation exposes protection status without exposing credentials or creators.
No page layout, strings or financial algorithm changed.

The final native host must retain/reconcile uncommitted identity/creation artifacts after a rejected
publication, rather than deleting referenced keys or treating missing history as a fresh profile.
This step does not register that host, activate main encryption, implement orphan reconciliation,
optional recovery, retention or coupled OS policy. These remain in the full SEC scope.

### D-221 — Review Windows upstream atomic-width warnings

The two C4133 diagnostics are in unchanged OpenSSL tsan_add calls for 32-bit new_nid and
global_seed. The MSVC macro type-checks both arms of a sizeof-based conditional; its unselected
64-bit arm causes the pointer diagnostic. Exact original obj_dat.obj disassembly uses lock xadd
dword ptr at new_nid. Exact property.obj uses lock add dword ptr at global_seed because its fetch
result is unused. Both original operations are 32-bit; no upstream source patch or warning suppression
was added. This is compiler/source evidence, not a claim that incompatible pointer warnings are
universally harmless. See the official [intrinsic signatures](https://learn.microsoft.com/en-us/cpp/intrinsics/interlockedexchangeadd-intrinsic-functions?view=msvc-170).

Test-WindowsAtomicWidth.ps1 verifies the exact header against the pinned official Git object, the
unchanged candidate DLL hash and pinned MSVC/SDK. A compiled upstream-macro probe exercises -1 + 1
with an adjacent sentinel (an incorrect 64-bit addition would carry into it), 64-bit values above
2^32 and 800,000 atomic additions across four threads. Probe disassembly separately verifies DWORD
and QWORD operations. The first verifier expected xadd for the unused global_seed result and failed;
inspection showed the valid lock add optimization. Corrected only that matching rule, then the related
probe passed. No unrelated native suite, main build/tests/UI or APK was repeated.

Static range review also bounds two C4334 diagnostics: the QUIC first-byte shift is 0..3 and its
result 1..8 fits 32 bits before widening. The two C4319 diagnostics complement a shifted unsigned
32-bit bit value, then intersect it with a final bit-0 mask; upper extension cannot affect that
condition. These are expression-level reviews, not independent acceptance of BN/QUIC algorithms.
The remaining 743 C4244/C4267 narrowing diagnostics still need review; the Windows native candidate
is not yet adopted or declared warning-free. Main encryption and the full recovery/platform scope
remain unfinished. Main tests (3,343) and signed APK are unchanged from D-220.

Proof: artifacts/native-sqlcipher/build/windows-d/atomic-width-4c3a49b4d702474aab6fc39a35172841/proof.json;
first verifier evidence retained in atomic-width-77cc4e16c9904d2ca91fda28fa00f259.
Full 749-occurrence inventory: artifacts/native-sqlcipher/d221-warning-inventory.json.
Native hash remains 3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
Review records source-header and both original-object hashes; the harness parser passes.

### D-222 — Cover the first window while preparing storage off its thread

CreateWindow now constructs only a non-financial StartupPage. OnStart runs the actual migration and
initial settings/display-unit persistence through a worker before creating financial content and
reading secure access state. Foreground posting, reminder refresh and theme/language rebuilds are
suppressed until preparation and the existing app-lock/router setup finish. Existing PIN protection
and deferred links remain separate; link navigation failures use the established generic feedback
and do not misclassify an already prepared database as failed startup.

The real loading/failure/retry surface uses dynamic theme colors, wrapping scalable text and a native
button. StartupViewModel retains the cover on preparation failure, exposes no exception payload,
rejects overlapping attempts and never reruns a completed preparation. Three new translated messages
exist in all six languages. The initial prepared settings row also supplies display units; no extra
startup settings read is added by display-unit publication.

IAppDatabaseStartup is the final initialization port. The existing plaintext host migrates only the
current legacy database and explicitly refuses a configured protected session/publication. The final
native host must replace this registration with complete captured-profile bootstrap. Main SQLCipher
provider/key/recovery/retention/OS integration remains unactivated. This is actual startup integration,
not a claim of completed encryption, the two-second performance target or platform/device acceptance.

Diagnostic routes now await actual startup before accessing data. A Debug-only thread observation
records actual storage work on a different thread from window creation. The startup review renders
real controls with fictitious preparation outcomes, invokes the native retry target and compares all
stored table rows. Run-Snapshots.ps1 -UseExistingBuild reuses only a caller-owned verified Windows
Debug executable for distinct layout cases; default behavior still builds and fails on build errors.
Callers must not infer success from a stale LASTEXITCODE when no native build command was run.

### D-223 — Device-key protection for internal restore safety packages

BackupService now accepts an explicit IBackupSafetyCopyProtection port. Safety-only creation builds
its ZIP in bounded memory, protects it before the first storage upload and clears its owned package
buffers. Portable backups retain the independent optional password choice. DeviceKeySafetyCopyProtection
uses a separate durable platform key per safety copy, bound to the captured profile/installation and
fresh generation; it never borrows the live database key. Opening reads only the existing key and
rejects foreign contexts before key access. No missing-key preparation or plaintext ZIP fallback occurs.
The envelope uses standard AES-256-GCM with a fresh nonce and authenticated non-secret binding/header.
Creation and safety extraction bound cumulative source bytes and package bytes to 256 MiB. Failed
manifest/checksum validation clears materialized entries. This is a format/storage integration, not
a custom cryptographic algorithm or a replacement for SQLCipher.

Two real safety uploads in one second exposed the existing timestamp filename overwrite. Protected
copies now have unique opaque .vbs names outside portable discovery/retention. Generic portable pruning
cannot delete these referenced recovery artifacts. Final native registration still needs private
storage, reference-aware retention and orphan reconciliation; skipping pruning is not that final policy.

Focused Backup tests pass 54/54 and Data tests 169/169. The independent signed Release AOT Android
package uses the actual BackupService, DeviceKeySafetyCopyProtection and AndroidGenerationKeyStore.
Two protected copies restore exact fictitious content, tamper fails authentication, and a cold process
reopens both using a port that forbids key preparation. The cold observation deadline expired; the
same process subsequently wrote its successful result, read without restart. Negative timeout evidence
is retained. Native build has zero warnings/errors. Proof: artifacts/native-sqlcipher/d223-safety-native-proof.json,
run 26da5f287da548f09acdf6abd6d37bf4. The runner now gives these cold phases its established longer bound.

Main protection registration remains inactive. Main database/provider/session/bootstrap, final
Windows/mobile matrix, optional recovery, key loss, reconciliation, retention and coupled OS exclusion
remain unfinished. SEC-08 is in progress; this does not close it or authorize product release.
No visible UI changed; unchanged layout matrices and upstream native producer suites were not repeated.
### D-224 — Protected startup composition

SEC-01: actual covered startup now drains application access, captures all original profiles
without commercial reads, invokes native bootstrap and publishes the selected session once.
Changed catalogue snapshots fail before publication. The independent Android initial/cold runs
pass with the work profile selected and both originals encrypted. Main provider/lifecycle
registration, fresh install, reconciliation, recovery, safety retention and coupled OS exclusion
remain open. No completion or main activation is inferred from the probe. Exact evidence is
recorded under D-224 in 01-assessment-and-decisions.md and specification Section 31.


### D-225 — Encrypted fresh-install startup composition

SEC-01 remains in progress: the real protected startup contract now supports explicit new
installation with the actual schema created in memory, and distinct existing/legacy authorities.
Missing existing metadata never permits replacement. Three authority tests pass; native fresh
and cold phases use retained fictitious data. Final main provider/lifecycle registration, orphan
reconciliation, restore routing, optional recovery, safety retention and coupled OS policy
remain open. Detailed evidence is in D-225 in the decision log and specification Section 31.


### D-226 — Windows key and synchronization effects

SEC-01 remains in progress. Actual Windows generation-key/file effects now persist immutable
CurrentUser DPAPI envelopes with owner-only ACLs, actual file/directory flush and existing-key
reopen. Nine native Windows tests and the separate-process encrypted native packet pass.
Final main adoption, Windows narrowing-warning review, profile lifecycle reconciliation,
optional recovery, retained-file policy, coupled OS exclusions and external platform/device
gates remain open. Exact proof is in D-226 in the decision log and specification Section 31.


### D-227 — Windows complete native startup and pool cleanup

SEC-01 remains in progress. Windows fresh/cold startup and interrupted two-profile migration
now run actual app startup and final native effects. Observed original deletion failure from
an idle pool is corrected by scoped bootstrap pool retirement; same-state resume and later
reopen succeed without replacement keys. Shared native fixtures remove platform duplication.
Main adoption, reconciliation, recovery/retention/OS policy, native warning review and external
platform/device gates remain open. Exact evidence is recorded in D-227 in the decision log.


### D-228 — Initial publication observer retry

SEC-01 remains in progress. Actual native failure after initial endpoint publication is now
recoverable with the retained key, unchanged prepared catalogue and observer replay. Native
Windows checks prove data/key/endpoint retention and no repeated key reads; shared Android
scenario verifies the same contract. Main registration, reconciliation, recovery/retention/OS
policy and remaining native/platform/device gates remain open. See D-228 in the decision log.


### D-229 — Selected restore adoption by the active session

SEC-01/SEC-08 remain in progress. Actual whole-package receiver now closes the old connection
before final adoption. Protected session validates/retains the exact restored key and rejects
profile-switch completion of its pending restore. Independent Windows restore/observer-error
resume/cold read pass without replacement keys or repeated preference persistence. Main native
and restore registration, reconciliation/recovery/retention/OS policy and remaining external
gates are still open. Detailed evidence is in D-229 in the decision log and specification.


### D-230 — Guard incomplete protected deletion

SEC remains in progress. Actual legacy profile deletion cannot remove a protected/partially
prepared catalogue row while stranding native identities and referenced artifacts. Four new
regressions and retained native Windows two-profile guard checks pass. This does not implement
protected deletion; durable retirement and uncommitted-creation reconciliation remain required
before main registration. Recovery/retention/OS/native platform and external gates remain open.

### D-231 — Persist and resume reviewed retirement barriers

SEC remains in progress. Exact non-main private retirement requests now record ordered catalogue,
artifact and identity barriers. Resume requalifies actual native synchronization before effects,
including a phase visible after a failed flush. Nine focused cases and a two-process actual Windows
file fixture pass. This is not complete native profile retirement: final catalogue/key-reference
inventory and application registration remain open, with recovery/retention/OS/platform gates.
See D-231 in the decision log for the observed evidence and its limits.

### D-232 — Exact identity removal after retirement reference checks

SEC remains in progress. Stable identities can now be retired only for the exact non-main request
at its natively requalified artifact barrier, after reference verification and transactional full
catalogue recheck. Other bindings are preserved; observable deletion/flush failure resumes without
replacement identities. Actual two-profile SQLite removal runs in the independent Windows caller.
Final native preference/artifact/key retirement and main registration remain pending. See D-232
in the decision log for exact evidence and remaining recovery/retention/OS/platform boundaries.

### D-233 — Persist exact file inventory before protected retirement

SEC remains in progress. Retirement now retains immutable direct filenames, native file identities,
lengths and SHA-256 before catalogue removal. Native Windows cleanup refuses changed/replaced files,
preserves foreign files and its own authority, synchronizes deletion, and resumes only the existing
catalogue-removal request. Real DPAPI-envelope deletion and cold independent continuation pass.
Complete native profile/reference discovery and final app/mobile/recovery/retention/OS integration
remain open. See D-233 in the decision log for observed scope and its limits.

### D-234 — Native Android cleanup and bound directory identity

SEC remains in progress. Android now removes only captured no-follow UID-private objects through
an anchored directory descriptor, with shared preflight/requalification and fsync. Root identity
prevents accepting absence in a replacement directory on both native adapters. Actual signed Release
Android lifecycle/cold main-key preservation and three focused changed-object refusals pass.
Final complete reference discovery, app catalogue integration, reconciliation/recovery/retention/OS
and platform/device acceptance remain open. Exact observed scope is recorded in D-234.

### D-235 — Capture complete known native profile references

SEC remains in progress. Actual identity/history metadata, authenticated native generation envelopes,
exact portable backup sets and authenticated opaque safety references now supply retirement inventory.
Unknown target namespace artifacts and changed snapshots fail closed; foreign safety/main references
are preserved. Actual Windows retained encrypted fixture capture succeeds without finance deletion.
Final app catalogue/startup reconciliation, recovery/retention/OS and platform/device integration
remain open. See D-235 in the decision log for exact observed coverage and limits.

### D-236 — Actual profile catalogue retirement and pre-startup continuation

SEC remains in progress. The explicitly configured native host now connects reviewed protected
DeleteAsync to actual catalogue Set/persistence, verified inventory/files/identity retirement and
pending reconciliation before startup capture. Digests protect exact complete catalogue inputs;
no plaintext names/packets enter request metadata. Missing inventory resumes only before publication,
retries reuse one request, and unrelated catalogue changes refuse overwrite. Normal plaintext behavior
and the unconfigured protected guard remain. Final main/recovery/retention/OS/creation/platform gates
are still open. See D-236 in the decision log for exact native and contract evidence.

D-237 adds explicit durable device Add intent before native profile preparation and pending startup
continuation with the same original identity. Catalogue persistence and completion precede request
cleanup; unrelated catalogue changes refuse replay. Main encryption remains inactive; historical
pre-intent orphan reconciliation, recovery/retention/OS and platform adoption remain open. See the
decision log for terminal evidence. This does not close SEC or the full backlog.

D-238 prepares standard RSA-OAEP-SHA256 optional generation recovery, DEK-keyed HMAC confirmation,
a SQLCipher-encrypted private vault and exact durable public recovery metadata. Device key reads
never silently fall back. Actual native recovery uses fictitious original-model finance and keeps
ciphertext unchanged. See decision log for terminal scope/evidence; final UX/re-enrollment/reference/
retirement/OS/provider adoption remain open, and main storage remains plaintext. SEC-06 is in progress.

D-239 connects exact recovery mapping capture/removal to actual protected ProfileService retirement.
Target keys are authenticated before catalogue publication, only captured target mappings are removed
before native key files, and other-profile recovery/private vault remain. Missing/changed references
refuse cleanup. See decision log for terminal evidence; main encryption, final recovery UI/re-enrollment/
rotation/reference-retention/OS/platform gates remain open. No whole-SEC completion is implied.

D-240 adds explicit verified native re-enrollment and cold device-key read without using the recovery
secret, preserving the original envelope and financial ciphertext. Missing/corrupt entries never
regenerate keys. Final recovery UX/all-profile orchestration, recovered-entry lifecycle/reference/
retention/OS/platform adoption remain open; main encryption stays inactive. See decision log evidence.

D-241 authenticates recovered-key native references with a DEK-keyed exact-object receipt and
connects original/recovered/receipt removal to actual protected ProfileService retirement. Missing/
changed/staged references refuse cleanup. See terminal evidence in decision log; main encryption,
final UX/all-profile recovery/alias lifecycle/retention/OS/platform adoption remain open.

D-242 composes complete captured-profile recovery preflight and immutable native imports before
actual covered startup, without quota filtering, session publication or catalogue writes. Unknown/
missing/changed authority fails; original buffers are cleared. See decision log for terminal proof;
main encryption, concrete full retained-reference verification, recovery UX/alias/retention/OS/
platform acceptance remain open. Whole SEC/goal scope is not declared complete.

D-243: actual Android wrapping-alias loss in the independent fictitious package is now verified,
including refused original reads, cold all-profile recovery/startup, unchanged original envelopes,
no alias recreation and balances 250/880. Strict native Release build is clean; see the decision
log and device-key guide for exact runtime proof. Product code/APK remain D-242/plaintext. Future
alias lifecycle, retained-reference validation, final recovery UI/adoption/OS and device/iOS gates
remain open; no unrelated test/build/UI suite was repeated.

D-244: new Android generation keys use independently wrapped purpose-0 envelopes instead of
recreating an installation alias. Legacy reads and receipt-bound recovery remain distinct. Actual
native alias-loss recovery plus a later cold generation read passed; the third observation expired
before the same process's successful result was read without restart. All 3456 tests passed; see
D-244/device-key guide for exact negative and positive evidence. Final protection/adoption and
reference-aware alias retention remain open; main registration remains plaintext.

D-245 adds concrete read-only recovery reference authentication for current/rollback/preference
cipher databases and separately keyed safety references, with exact native object/namespace rechecks.
Five native-file cases and all 3461 tests pass. Actual Android cold all-profile recovery now verifies
two databases/rollbacks/capsules and two safety references after removal of all four fixture envelopes;
see the decision/device-key guide for proof and corpus limits. Main protection remains inactive;
full restore/crash/staged/orphan corpus, final recovery UI/retention/adoption/OS gates remain open.

D-246 fixes required recovery candidate presence at the journal/history interruption boundary.
CandidateVerified or Selected in the real journal requires the actual candidate even before history
Selected is mirrored. Two new cases, all 3463 tests and the actual original verifier on native Android
pass; journal/history remain unchanged and no replacement data/keys appear. See decision/device-key
proof. Multi-restore financial corpus, reconciliation/UI/retention/adoption/OS remain open; main is plaintext.

D-247 verifies an actual two-portable-restore chain and interrupted native preference persistence.
Cold original application recovery verifies/re-enrolls all three generation keys, resumes the selected
second restore without an old connection/package and preserves candidate/rollback balances. A stale
fictitious catalogue setup defect was corrected; its refusal and same-fixture continuation are retained,
and a fresh complete scenario passes. See decision/device-key guide for exact evidence. Product code
and verified signed APK remain D-246; reconciliation/retention/UI/adoption/OS/device gates remain open.

D-248 aligns Android key reads with existing native ownership checks: regular, single-link and
current-UID file descriptors only. Actual normal/cold/negative key-store observations pass; hard-link
creation is denied by the emulator, explicitly not coverage of its read-refusal branch. Same-key
negative continuation and failed harness evidence are retained. All 3463 tests pass. Final recovery/
cleanup/retention/adoption/OS and external platform gates remain open; main remains plaintext.

D-249 lets explicitly verified recovery preserve a mismatching bounded uncommitted receipt stage and
publish a separate read-back/synchronized receipt attempt without replacing final authority. Actual
Windows DPAPI and four Android staged-receipt recoveries pass; all old stages remain unchanged and
main/work balances remain 250/880. All 3464 tests pass. Reconciliation/retention/final adoption remain
open; published/oversized/unsafe receipt authority is not replaced and main registration remains plaintext.

D-250 lets authenticated reviewed profile retirement capture/remove only bounded canonical receipt
stage remnants through its existing immutable native inventory and durable barriers. Missing final
authority, oversized/unknown names and key-envelope staging remain refused. Three added actual app/
Windows cases, all 3467 tests and cold original Android profile retirement pass; main balance/recovery
remain. This closes receipt-only profile cleanup; automatic retention/orphan/key-stage/final adoption
and external gates remain open. Main stays plaintext.

D-251 removes implicit native-engine selection from shared persistence. The app/test hosts explicitly
retain the existing standard bundle; the design-time bundle is private and cannot flow to app hosts.
Actual dependency graphs, real EF context tooling, all 3467 tests and strict Windows/Android builds
pass. This is a final-provider integration prerequisite; current main engine/storage remain unchanged
and cipher host/library compatibility, recovery/retention/OS/platform adoption are still unfinished.

D-252 qualifies actual production persistence assemblies, not source copies, with SQLCipher bridge/core
3.0.5 on Windows x64 and signed Release AOT/trim Android x64. Actual 32 migrations, encrypted negative
money, binary portable backup and wrong-key refusal pass across fresh process reopen. Android sandbox
inspection failure was corrected only in its distinct fixture package, preserving creation/result/data
and continuing the same key. See decision/device-key guide for proof/scope. Main/APK remain D-251;
final host/recovery/retention/OS/native platform and device acceptance remain open.

D-253 adds host-required protection before initial publication: every factory route refuses without
creating plaintext storage and releases its lease. Actual binary cipher hosts use the setting and
verify refusal before encrypted startup. Final main/recovery/retention/OS adoption remains open;
OD-04/OD-10 are approved and no new owner decision is needed for this implementation prerequisite.

D-254 composes protected startup/current resolution/switching/new-profile preparation in actual app
source, with one session and the actual reviewed durable Add request before identity/key work.
Cold restore receivers receive the current startup owner. Final native registration, receiver,
recovery UI, retention/reconciliation and coupled OS/platform acceptance remain open; main is plaintext.

D-255 composes recorded restore receivers, actual prior-generation rollback access, native preference
replay and settled-session intent capture. Five added metadata cases/all 3481 tests and two actual
Android cold continuations pass. Main remains plaintext; final native registration/restore UI/recovery
UI/retention/reconciliation/OS/device acceptance remain open. Pre-staging package/capsule loss is not
covered by these prepared/selected continuations and must not be completed by guessed fallback.

D-256 defers financial restore intent until whole-package validation and memory-only integrity/known
migration-prefix/portable JSON preflight. The actual app adapter holds the drained owner and never
falls back to sequential restore. All 3482 tests pass. Native fixture exception-catch failure is
preserved without reset; final runtime qualification is recorded in the decision log. Main remains
plaintext; final native registration, restore/recovery UI, retention/reconciliation/OS/device gates remain open.

D-257 qualifies actual production-assembly Android callers restored from the immutable source-built
candidate NuGet package, including original runtime bytes and bundled notices/provenance. Initial
XML and duplicate-asset warning evidence are retained; final cold same-key run is clean. Main remains
D-256/plaintext; no unrelated producer/main/UI/test/APK replay. Native matrix/warning review and final
registration/recovery/retention/OS/device/release acceptance remain open.

D-258 bounds20 secure-heap list-index narrowing occurrences in official Windows OpenSSL source to
0..59 under valid allocator state, with immutable source/binary and complete occurrence qualification.
This does not cover corrupted pointers/full allocator security.723 narrowing occurrences remain
unreviewed; the original749 diagnostics are not suppressed. No main code/APK/native producer replay;
main encryption, complete warning review and full adoption/security/platform acceptance remain open.

D-259 triages16 legacy AES warnings: direct SQLCipher binding is CBC, while warning sites are other
handlers. A padded-Wrap output-expression counterexample disproves blanket int-input range acceptance;
it does not prove public-API exploitation. All16 remain unapproved within723 pending narrowing sites.
No source/binary/app/APK or unchanged producer/main verification changed. Native adoption must qualify
actual dispatch/bounds or a supported reduced profile; the original full goal remains unfinished.

D-260 implements an explicit supported reduced Windows OpenSSL profile with pinned provenance and
required primitives retained. Actual production-assembly migration/persistence/cold checks pass;
initial reduced profile failed TLS tests; the revised EC-retaining native inventory704 is not
blanket approval. Official suite result is recorded in the
D-260 decision. Main remains plaintext/D-256; final integration and full planned work remain open.

D-261 replaces the Windows actual-assembly host's manual DLL path with the immutable source-built
NuGet package, runtime resolution and exact bundled notices/provenance. Strict build and original
32-migration/cold cases pass; no native producer/main/UI/APK replay. This closes package consumption,
not main encryption: final startup/recovery/retention/OS and external acceptance remain unfinished.

D-262 fixes an actual startup ownership gap: legacy filename movement no longer runs during DI.
The covered worker owns a drained lease and refuses protected/conflicting states before changing files.
Five new cases/all3487 tests, strict builds and native startup observations pass. This does not close
SEC-02/05: final cipher registration, recovery/retention/OS and platform acceptance remain open.

## D-264 Windows protected storage status

Windows main protected startup, DPAPI recovery enrollment/key-loss/cold reopen, isolated profile creation/switch/retirement, encrypted safety retention and explicit portable replacement have native application evidence on an independent fictional installation. Shared Android effects remain platform adapters to the same application algorithms. Final local verification: 3521 tests passed with zero failures/skips, strict Windows/Android builds without warnings/errors, complete signed APK and scoped emulator upgrade/cold reopening passed; see [Windows quality record](../../quality/windows-protected-storage.md). No iOS, physical-device, OAuth, Store or hosted fresh compiler acceptance is implied. The continuous-development objective remains open.

## D-265 holding accessibility and retired Home reads

Eighteen native contexts for three changed pages and eighteen distinct bottom contexts cover fa/en/de, both themes and 360/412/wide at process-local 200% typography; Cancel and original command/row identities retain financial data. The actual original Home read waits behind SQLite maintenance, retires with its Shell and completes without publishing into the old collection; replacement Home and complete stored rows verify. 3521 tests pass (zero failures/skips); native rendering is separately verified. [Bounded evidence](../../quality/holdings-readable.md). Final artifact/build details accompany delivery; physical-device, actual OS scaling/spoken screen readers and iOS remain open.


## D-266 device-only authentication requires success

The owner approved keeping the original lock cover when system authentication is unavailable on 2026-10-14. Only AuthenticationOutcome.Success authorizes device-only entry. Failed, unavailable, unknown and throwing responses preserve the original cover and queued destinations. The existing translated unavailable-device guidance explains restoring a device screen lock. PIN verification/recovery and the previously authenticated return grace remain separate and unchanged. No new stored data, permission, SDK or portable preference is introduced.

Actual Windows LockPage/native Invoke checks covered 18 language/theme/width contexts with an explicit authentication response port; a separate feedback pass checked the translated guidance. The independent Android package used the actual native adapter: NotAvailable after automatic prompting and Unlock, with Back unable to dismiss the cover and zero queued destinations opened. Neither review changes system credentials or native PIN storage. These checks do not accept configured biometrics, Windows Hello, physical devices or iOS.

An unrelated Home collection rendering COM exception remains observed during language/theme review. The lock assertions and unchanged financial fingerprint completed, but this is not a clean whole-application rendering result. SEC-04 remains in progress for its remaining platform and biometric transitions.


## D-267 complete Home balance publication

Observed Windows Home refresh failed in BindableLayout's ContentView handler disconnection while Balances.Clear published an empty intermediate list. Currency totals now use the existing SnapshotCollection complete replacement, preserving existing amount rows during ordinary and covered reloads. Currency calculations, inclusion policy, formatting and current-page retirement guards remain unchanged. The native regression checks original Home and original amount-handler identities, including a real modal cover; financial storage is not edited.

D-267 final native review passed all 18 contexts and 36 original Home reloads without the observed COM failure; amount handlers and complete financial rows remain unchanged. Canonical signed Release upgrade/cold/More-to-Home return passed. The unchanged 3,526-test D-266 baseline was retained; native publication is directly verified. Exact evidence and external limits: docs/quality/home-balance-reload.md.

D-268 final affected checks: 3,526 tests passed, zero failures/skips; 18 original sale-help contexts and 12 English pay/receive native dialogs completed with Cancel and complete financial rows unchanged. Independent Android original sale-help open/close and complete signed Release upgrade/cold Home passed. See docs/quality/sale-and-connection-copy.md.

## D-269 decision-ready backend cost model

Completed the prerequisite cost/architecture proposal for OD-02 with official current price references, the dated PostgreSQL plan restriction, explicit bounded usage and independent arithmetic checks. [Proposal](07-backend-cost-and-decision.md) and [structured model](08-backend-cost-model.json). This is not ZCR-ID-01 implementation, hosted restore, provider/legal acceptance or spending authority. Main source/tests/APK remain D-268; no unchanged verification replay.


## D-270 shared Windows native toolchain qualification

Build, upstream validation and atomic-width validation now use one reviewed toolchain resolver.
Previously the build accepted both reviewed installer patch versions while the checks accepted
only the original installer and did not qualify compiler bytes. All three consumers now require
the same pinned MSVC, SDK, installer allowlist and exact compiler executable hash before execution.
Windows CI runs the qualification check before native preparation; its dependency cache includes
the shared resolver. No compiler installation, source fork, cryptographic flag or binary changed.

Eleven qualification cases passed, including both allowed installer metadata versions and refused
unreviewed versions, paths, SDK/tools, policy and compiler bytes. Actual installation remains
18.10.12201.205: the other patch version was tested as metadata only. The current candidate's
original upstream atomic operations passed the sentinel, 64-bit carry, four-thread update and
disassembly checks; native SHA256 remains
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21.
The 704 upstream diagnostics remain recorded and broader warning/security qualification remains
open. This tooling delivery does not imply hosted Windows acceptance or device/iOS/publication
acceptance. App code, the 3,526-test baseline and signed APK remain D-268; unrelated checks were
not repeated. Evidence: eng/native/sqlcipher/Test-WindowsToolchain.ps1 and
artifacts/native-sqlcipher/build/windows-storage-d260-ec/atomic-width-51c7a432ac70474da3e3206c04cd5e5a/proof.json.


## D-272 complete local identity and private service foundation

Implemented the approved optional ASP.NET Core/PostgreSQL/private-storage backend as an independent
Zanance API project. Maintained Identity password/token providers, protected bearer tickets,
transactional one-use challenges, absolute device grants, atomic refresh generation, immediate
stamp/session revocation, shared durable request limits and private encrypted immutable objects
are connected end to end. Capacity is serialized by real owner in PostgreSQL. No local profile id
or opaque object id establishes ownership. No financial sync/sharing/billing route is implemented.

The separate migration operator and runtime role, persisted wrapped Data Protection key ring,
leased encrypted fictional outbox, resumable deletion/cleanup, content-free readiness and hidden
local start/drain/snapshot/fresh restore operations are implemented. Missing service keys keep the
original installation closed; no replacement key authority is accepted. No real email, account,
paid hosting, app connection, entitlement enforcement or publication was enabled.

Final server Release build has zero warnings/errors. Four actual-host/PostgreSQL scenarios pass
with zero errors/failures/skips on the private .NET/ASP.NET 10.0.13 runtime (6.355 seconds); they cover concurrency,
expiry/replay, ownership, quota, protected delivery, corruption, revocation, cold key loss/recovery
and real pg_dump/pg_restore. Fictional restore was about 0.7 seconds with its committed object and
revocation state preserved. Windows Kestrel and the operator's independently restored destination
returned readiness 200; exact scopes and observed failures are in quality/local-identity-backend.md.

The existing 3,526 app-test baseline and complete signed mobile APK remain D-268; no unchanged app
build, native visual matrix or cipher corpus was replayed. Local ID-01 foundation is delivered;
external sender, hosted certificate/key/backup operations, provider/legal acceptance and mobile
service adoption remain separate gates. The whole development objective remains active.

## D-273 target currencies and source-qualified holiday calendars

Deliver LOC-07/08 as one app batch. The working Western Europe scope is DE, AT, BE, FR, LI,
LU, MC, NL, CH, GB and IE, plus US, CA and AU; the broader geographic boundary question
remains open. Existing EUR, CHF, GBP, USD, CAD and AUD already have ISO minor digits of two.
No stored amount, account currency or language is changed by selecting a country. Country
registration is additive, independent of the interface language. Existing Iran dates remain
compatible; an Iran release variant is future work.

Offline holiday sources and regular dates are reviewed for Gregorian 2026-2027. Settings,
onboarding and Plan display scope, authority, reviewed years and an unverified-year warning.
Switzerland has only August 1 federal coverage; Canada uses federal nominal statutory dates;
the US uses standard Monday-Friday observed federal dates. UK and Australia contain only dates
shared by all constituent regions. Regional, workplace, extraordinary announced closures and
bank-business-day guarantees are excluded explicitly. Legacy Germany/Iran calculations remain
compatible outside the reviewed window; new country datasets do not invent future dates.
Saved Plan country/weekend masks are retained when device regional preferences change.

All 3,569 app tests pass, including 43 new cases; final Windows and signed Android Release builds
have zero warnings/errors. The original regional view passed 18 hidden own-window layout checks
(en/fa/de, light/dark, 360/412/1280 px, Debug text scale 2), complete captions and 44 px picker
height, with unchanged financial rows. Eighteen actual native selections passed in the three
English light contexts. Hidden RTL popup automation failed and is not counted as selection
acceptance. Actual signed emulator Settings selections for Canada, US and Australia, live
Persian/German/English captions, light/dark app choices and return to no holiday calendar were
checked separately through native XML. No financial entry was posted. Emulator Home remains
0.00 EUR; English, Gregorian and automatic digits/week start were restored; theme ends at
Use device setting. This is not physical-device, spoken screen-reader or iOS acceptance.

Detailed official sources, exact scope and observed verification failures are recorded in
quality/target-holiday-calendars.md. No new SDK, network request, permission, schema, paid resource
or commercial enforcement was introduced. The full development objective remains active.

## D-274 operation-scoped encrypted quick-resource reads

Measured protected Release startup after D-273: system first frames 1664/1401/1462 ms,
while loaded Home is observed later (accessibility polling upper bounds, not exact paint).
The original quick-resource operation reopened native keyed connections between EF queries.
A single DbContext alone did not improve its measured warm median (~989 versus ~999 ms).
The complete operation now captures one original context and explicitly keeps its non-pooled
connection open through both resource choices, complete input read and every original validation.
Standalone resource-choice reads also keep their own operation's connection open. No key format,
KDF, schema, guard, commercial activation, global pool or cross-operation credential cache changes.
Later explicit actions still read and validate fresh original data through a new context.

The same independently encrypted fictional fixture and actual production Data assemblies show
warm complete-read median 999.38 ms before, 82.42 ms after. First read 1220.60/565.23 ms includes
process/JIT effects and is kept separate. Wrong-key refusal, 32 migrations, negative balance and
portable read-back pass. Two actual-SQLite cases prove one captured context, unchanged rows/no
default settings or resource-choice writes, fresh action validation and drained ownership.

A Windows review launch exposed stale ApplicationId metadata despite an independent build property.
Its exact process was stopped; that attempt is not independent-installation acceptance. Touch/rebuild
and compiled identity checks precede the corrected review. Debug Home/holiday review routes now
reject a main identity before covered storage startup, without changing Release behavior. Do not
inspect or restore an unrelated installation to turn that failed attempt into evidence.

Final affected tests/build/native results and artifact identity are recorded in
quality/quick-resource-read-performance.md. Earlier full 3,571-test pass precedes the explicit-open
refinement; final Data/App tests are repeated because their production dependency changed, while
unchanged successful test projects retain that baseline. QA-06, full loaded-Home timing, large-data
native publication, physical devices and iOS remain open. The development goal stays active.

## D-275 complete Home loading and retry

Original Home now uses the existing snapshot load state from its first frame through complete
read/publication. Empty initial defaults and dashboard actions remain unavailable until ready;
an actually empty completed snapshot retains its existing next step. Six-language loading,
Home-specific failure/retry, original serialized reloads and retirement guards preserve data.
No schema, permission, SDK, key, stored preference, commercial activation or money formula changes.

All 3,572 app tests pass (zero failures/skips), with the new AT-275 final-retirement publication
case and existing initial/reload failure/retry/cancellation coverage. The final caption refinement
passes 242 localization tests without repeating unchanged successful projects. Strict Windows and
Android builds have zero warnings/errors. Eighteen native language/theme/width contexts, 36 focused
loading/failure captures, 18 actual native retries and unchanged complete financial fingerprint
pass in an independent protected installation. The first partial review exposed a wrong reused
transaction retry caption and was superseded by the corrected complete matrix.

Complete signed Android Release cold launch on the owned emulator shows preparation, actual Home
loading, then ready balance/actions in the same process; no false empty-account card appears.
APK signature/package/payload and exact artifact are recorded in quality/home-loading-publication.md.
This closes the observed publication defect, not the two-second performance objective, Android
fault injection, physical device, spoken screen-reader, OS scaling, iOS or release acceptance.
The development goal remains active; independent owner/provider gates remain unchosen.

## D-276 complete protected work-read performance batch

Complete Plan/Goal reads and fresh validation, budget choice/form reads and validation,
quick-resource validation and bounded budget-alert inputs keep their own non-pooled connection
open through the original operation. Budget form choices and inputs capture the same original
context/file. Every existing rights/stale/current-file check and writer/financial formula stays;
there is no global key-bearing pool, KDF reduction, schema or commercial activation change.

Actual production-assembly native comparison on the same encrypted fixture measures complete
read plus fresh validation: Plan 251.36 to 140.24 ms, Goal 624.61 to 180.53 ms, budget form
1298.93 to 155.51 ms, budget alerts 2281.44 to 653.78 ms, quick resources 788.64 to 146.29 ms
(three warmed medians; first use separate). Negative balance, 32 migrations, portable read-back
and wrong-key refusal pass. These are operation timings, not full loaded Home or device acceptance.
All 3,573 app tests pass with one new actual-SQLite budget context/lifetime case; strict builds,
original native Home and signed Release evidence are in quality/work-snapshot-read-performance.md.
The whole development objective and physical/iOS/reference-workload gates remain open.

## D-277 existing-user upgrade and current test-build policy

ENT-05's current policy is already implemented: the default commercial source is inactive,
with no fake Plus/Pro right or permanent/portable test entitlement. The actual first-schema
upgrade scenario writes four accounts above the approved Free quota and an expense, upgrades
through original migrations, retains identities/balance, accepts an explicit fifth test account,
and restores the real portable source into a separate fresh provider. Both original and restored
providers remain unenforced with null paid context; no resource selections are invented.

One focused AT-277 real-SQLite upgrade/restore scenario passes under strict compilation. It adds
one behavior to D-276's 3,573-test baseline (3,574 covered behaviors, not a new full-suite run).
No app/runtime/schema/dependency/UI change; no unrelated build/native matrix or new APK. The
complete signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.
Exact evidence and scope: quality/test-build-upgrade-policy.md.

Customer activation and announcement remain explicit owner/BIL gates. Do not turn disabled test
enforcement into permanent paid facts or announce future limits as already enabled. LOC-09's protection choice was subsequently approved on 2026-10-15 (D-281);
local personal sync was separately approved by the owner on 2026-10-15 and remains to be implemented;
sharing, purchases and hosting do not follow from that approval. The full development goal remains active.


## D-278 protected reference and tenfold workload evidence

An independent Windows executable now measures the actual protected generation/bootstrap and
production store/validation/calculation paths on deterministic 10,000-entry / 20-account / 100-plan
and tenfold workloads. Complete original schema/header/integrity/typed rows match the authenticated
pre-migration encrypted rollback after measurements, and both plaintext originals are removed.
Warm medians: all-entry reads 158.617 / 1191.737 ms; Plan read/validation 131.983 / 138.041 ms;
quick resources 127.675 / 126.937 ms; original balances/totals 1.487 / 19.066 ms.

Fresh tenfold protected preparation is 27,561.853 ms. Reference 439.797 ms is explicitly a resumed
revalidation, not fresh migration time: the original run terminated on an invalid raw export-byte
comparison, and the corrected complete logical verifier resumed the same generation/keys. The
terminal corrected run passed both workloads; strict Release compilation has no errors/warnings.
No app code/UI/schema change, unrelated test replay or new APK. Application coverage remains
3,574 behaviors (D-276 full 3,573 plus D-277 focused one), not a new complete suite run.

See quality/protected-workload-performance.md and the executable README for samples, failures,
resume ownership and exact scope. QA-06 remains partial: actual loaded Home/search/save, cold/ANR,
reference phone, Android/iOS and the two-second objective are not established by store timings.
The current signed installable APK remains artifacts/android-work-read-final/pro.vafadar.zanance-Signed.apk.


## D-279 hosted Windows native qualification

Native cipher candidates now includes a Windows lane using the existing exact compiler qualification,
two fresh native builds with matching DLL bytes, original schema/tamper checks, immutable package
entries, actual production-assembly create/cold reopening and protected reference/tenfold workloads.
Only allowlisted public logs/proofs/measurements are uploaded; databases and DPAPI/key material stay
private. No candidate cache, weaker package/compiler policy, new native dependency or app change.

Actual YAML and all five Windows PowerShell bodies parse. Hosted success is not inferred from syntax
or the existing local D-278 workloads; check the final pushed commit once after the normal delay.
Existing native warnings and the separate unprivileged Windows OpenSSL-suite scope remain explicit.
No unrelated successful app/UI/full-suite/APK replay: application coverage remains 3,574 behaviors
(3,573 full plus one focused), signed APK remains D-276. See quality/windows-native-ci.md for the
exact path, artifact policy and remaining platform/security/device/release gates. Full goal remains open.


## D-280 current protected search and validated-save workloads

The independent protected executable now exercises original complete search/group and validated entry
Save on fresh reference/tenfold data. Focused ledger mode omits unchanged D-278 read/calculation cohorts;
default hosted runs include all operations. Four searches per shape check unique transfer/missing/numeric/
complete results; four actual saves per shape verify stored fields and delete exactly their own row.
Complete schema/header/integrity/typed rows still match the pre-migration encrypted rollback afterward.

Actual warmed unique search/group medians are 9.084 / 17.202 ms; validated saves 68.810 / 69.033 ms.
Search timers include result checks and exclude initial read/native rendering. Both workloads finish,
all eight temporary rows are read back/removed and both original plaintext files are absent. Strict
Release builds have zero warnings/errors. See quality/protected-ledger-performance.md for all samples,
result counts, preservation and timing limits. No unrelated app/UI/APK/full-test replay or new app
change: 3,574 covered behaviors retain their qualified baseline. Native phone/Home/search/Save, cold/
ANR, iOS, the approximate two-second objective and full goal acceptance remain open.

## D-283 complete coherent protected Home read

Initial Home and reload now use one captured context/keyed connection/deferred SQLite reader for
all original sections, then revalidate the original reviews through a fresh operation before uncovering
actions. Reuse existing quick/plan/goal guards, exact monthly SQL selection and Core calculations;
no key-bearing global pool, lower KDF, dropped history, default-row write or schema/UI policy change.
All native opening/materialization and final validation run off the UI thread. Goal Home projection
uses the same full packet and preserves every original financial row and activity choice.

On the same protected reference/tenfold fixtures, complete read/validation warm medians changed
798.06 -> 223.96 ms and 1925.59 -> 1355.62 ms, including full packet equality checks. Full logical data
comparison passes. Ten real-SQLite cases and three original application-source cases are added;
affected Data 805 / App 1186 pass. Coverage 3,604 means previous 3,591 plus thirteen new, not a full-suite
rerun. Strict builds, 18 protected Windows native contexts, same-process Release emulator Home/entry/
Cancel and a complete signed APK pass. Exact evidence and retained negative observations:
[coherent-home-read.md](../../quality/coherent-home-read.md). No two-second/phone/iOS/release acceptance;
QA-06 and the full development goal remain open.

## D-287 current summary reconciliation

Current ADR/native/privacy/spec summaries now agree with delivered Android/Windows protection,
local copies, P2-18 loan estimates and local entitlement/identity foundations. Initial inventories
are explicitly historical; owner/external/native-warning gates are not marked complete. No code
or runtime verification repeated. Evidence: ../../quality/current-status-alignment.md.
## D-288 hosted Windows installer repair

Allow the exact Visual Studio 18.10.12224.181 metadata from runner image 20261007.264.1;
retain compiler SHA256/signature, MSVC/SDK and immutable package checks. Sixteen focused
local toolchain cases pass; hosted execution remains pending at commit time. No app/native
binary/UI/APK or unrelated successful test rerun. See the D-288 decision for failure evidence
and the versioned inventory. Native-warning safety review remains open.
## D-289 Windows native diagnostic drift gate

Bind the existing 704 upstream warning occurrences/689 diagnostic keys to 224 official-source
files and their hashes. Refuse diagnostic/source drift at native build completion and before
packaging; preserve exact resumed recipes and all original occurrences. Cache immutable packages
rather than incomplete candidate fragments. Export proposals into fresh ignored evidence only;
never suppress warnings or silently promote a new baseline. Twenty-six focused cases, real
producer/source checks and unchanged immutable package entries pass locally. No unrelated app,
UI/APK or upstream suites repeated; hosted result is pending at commit time. Existing warning
safety, platform/device and release gates remain open. See ../../quality/windows-warning-drift.md.
## D-290 one selected-generation verification at covered startup

The all-profile pass opened/discarded the selected generation before the actual session reopened
it. Prepare all other profiles first, then verify and retain the selected key through that same
real session before publication. Require paired captured current-original/publisher authority and
an actual verified profile/installation owner afterward. Keep complete key/KDF/integrity/history,
migration/rollback and observer retry behavior; default bootstrap callers are unchanged.

Nineteen affected strict infrastructure tests, four application startup tests and the final added
no-op publisher case pass (four new covered behaviors; 3,614 coverage, not a full-suite rerun).
Independent Windows/Release AOT Android creation/cold/key-failure cases, same-key observer retry,
retained restored-session cold read, strict Windows build, complete signed APK and original
same-process Android Home/expense/Cancel pass. Cold key reads fall 4 -> 2 (one profile) and
6 -> 4 (two); final guarded small Windows two-profile sample is 338.27ms versus 496.62ms before.
No whole-app/two-second/phone/iOS/native-warning/release acceptance follows. See
../../quality/selected-startup-verification.md for retained evidence and precise measurement boundaries.

## D-291 original protected Release/AOT Android Home

An independent x64 caller compiles the original Home/App/data sources and measures two actual native
pages in en/fa/de on protected reference (10,000 entries/20 accounts/100 schedules) and tenfold
(100,000/200/1,000) data. The complete twelve-page result and both original schema/header/integrity/
typed-row export comparisons pass. Reference publication is 1104.51-1786.76ms; tenfold is
6586.67-7225.87ms, with largest dispatcher gap 363.15ms. Tenfold latency remains an observed concern.
This excludes page construction, cold startup, fixture preparation and generation verification;
frame callbacks are not GPU/phone/two-second/ANR acceptance.

Caller-only manifest/property/registration/path-alias/locale/native-digit expectation failures are
retained. Original Android locale handling and original NativeDigits presentation are preserved;
no ledger discrepancy or financial Save occurred. Already successful English samples were retained,
and the same prepared financial files/keys continued without reseeding or observation-timeout restart.
Final Release/AOT build has zero warnings/errors; independent signed package/native hashes verify.
Main source/APK remain D-290 and 3,614 covered behaviors remain unchanged, not a full-suite rerun.
No unrelated successful test, Windows/UI/native corpus or unchanged main APK build was repeated.

The owner separately approved complete local personal Pro sync on 2026-10-15: changes/deletions,
explicit conflicts without guessed amounts, idempotency, safe restore and device management.
ZCR-SYNC-01..03 is the next complete implementation batch; paid hosting, household sharing and
commercial activation remain separate gates. This decision is recorded, not implemented by D-291.
See ../../quality/android-loaded-home.md for exact evidence, boundaries and retained failures.

Unpublished personal-sync implementation checkpoint: original optional application/lifetime source
and real pinned-HTTPS/PostgreSQL/two-SQLite-client protocol now have focused evidence, including lost
commit response, explicit competing money, tombstones/receipts/revocation, duplicate full settlement
and stale related-payment review. AT-292 remains partial; complete conflict UI, first-run recovery,
initialization takeover, remaining domain/native/platform cases and final delivery are unfinished.
See ../../quality/local-personal-sync.md. Do not interpret the staged protocol tests as a finished
application feature or change the last delivered full-suite/APK baseline.

D-294 lifecycle is in progress: focused actual service, native process-loss and real old-PostgreSQL-snapshot continuation passed with permanent access revocation, original intent/key/data preservation and exact-room retirement. Audit signature/frozen-prefix and authenticated expired-request finishing also passed. Main lifecycle UI has18 new native Windows Active/no-deletion status cases; destructive confirmation and other states remain unaccepted; mobile, complete lifecycle/identity erasure, operational latest-marker restore composition and scale/history gates remain. No previous successful unrelated suite was rerun. The complete-suite baseline is3702; no final D-294 APK or release is claimed. See11-household-delivery.md for exact fixtures and negative evidence.
D-294 full operational privacy restoration now passed on independent source/target Docker stacks, including actual complete snapshots before/after deletion, live-source refusal, latest-marker merge before old snapshot startup, permanent deleted-room receipt/retirement and unchanged independent-room ciphertext decryption. A concrete configuration omission was fixed: local-household opt-in now survives backup/restart/restore without enabling it publicly. No successful old D-293 hosting stage was rerun.

A separate real retained-owner counterexample showed that the original deletion workspace depended on forbidden financial refresh. The same original protected files/keys and31-day retained service state now complete owner-only opaque privacy checkpoint review, same-device fresh-session possession, original consent and retirement without fetching financial data or epoch grants. Actual financial/key resources and normal local writes stayed refused; every local financial column was preserved. The existing historical trusted root signs only this exact privacy request; newer opaque checkpoint metadata never grants roster/epoch/financial authority, and a missing/different root fails closed. Main confirmation refetches the exact reviewed checkpoint before persisting consent. No schema, dependency or resource changed, so no model regeneration or unchanged resource/layout matrix was repeated. New strict main Windows/Android builds passed; whole D-294 and native destructive-dialog/mobile acceptance remain unfinished. See11-household-delivery.md for current fixtures and exact scope.

Current SHR-01..04 local completion and exact gates supersede the earlier chronological D-294 in-progress notes. See11-household-delivery.md. The next ready complete batch is BIL-01..05 local implementation and future-web prerequisites; actual store sandbox, signing and production activation remain evidence/owner gates.

## Current billing delivery boundary (D-295)

The local Android/service end-to-end implementation is retained as one substantial delivery, including private
cache/recovery, actual native catalog/intent/replacement code, original comparison/identity UI and canonical
PostgreSQL/HTTP authority. Details and immutable evidence are in12-billing-delivery.md. This does not mark the
whole cross-platform commercial wave Done. Native StoreKit2 must be implemented and tested on the actual Mac;
genuine store products/credentials/sandbox/public callbacks and commercial activation remain external gates.
Continue ready approved German accountant-export work while those environments are unavailable; preserve the
Mac/store work in this current backlog. No new owner scope decision, Windows commercial phase or paid resource
is implied. Do not repeat delivered household/cipher/full-suite checks as a substitute for remaining implementation.


### D-296 accountant-export delivery

TAX-02 Level1 is locally qualified end-to-end, with23 new source-flow cases, relevant policy/resource checks,
18 native Windows contexts and main signed Release Android confirmation/Cancel/native share handoff. It does
not implement TAX-03 or tax filing. See [13-accountant-export-delivery.md](13-accountant-export-delivery.md)
for exact sources, source/privacy/size guards, failures, package hash and acceptance limits. Full-suite3753 remains
historical, not a fresh full run. Next ready work is BANK-01 original-format research/import, then approved
AI/FX prerequisites. Retain all current BIL/platform/provider/release gates and future Windows/web exclusions.

## D-300 measured Android startup compilation

The original observed process-startup failures are retained, and actual bootstrap/worker/Home phases are
measured separately. Supplement Android SDK AOT profiles with the real cold-start path, preserving the original
protected fixture, privacy order, cover and financial behavior. Exact native/package evidence is recorded in
17-startup-responsiveness-delivery.md. General performance, two-second/reference-phone, iOS, native upstream
warnings and release gates remain open. No unrelated full-suite/cipher/sync/provider/layout replay.

## D-301 native maintenance locally delivered; full platform/security gates remain

The approved official Community maintenance batch now has a reduced shared Windows/Android storage profile,
two fresh reproducible outputs on each local target, actual50-migration assembly/native checks, retained old
DPAPI data reopening, official configured OpenSSL/SQLCipher suites and closed producer identity policy.
The remaining508 Windows diagnostics retain no per-site safety waiver. Real hosted producer success, exact
servicing bytes, immutable consumer adoption and final original main upgrade/navigation are recorded; do not close
SEC-01/02 or infer device/Mac/physical/release acceptance. Follow18-native-storage-profile-delivery.md and do
not replay successful unrelated app/UI/provider/sync/financial tests.

## D-304 bounded backend qualifier repair

The D-303 hosted purchase-retirement failure is a shared null TestServer peer exhausting the unchanged durable
billing request window. The exact retained Linux continuation and a new cross-worker rate-boundary scenario
pass after fixture-only peer isolation. Production code and APK remain D-303; hosted completion of the repair
is still a separate gate. See [21-billing-http-isolation.md](21-billing-http-isolation.md).


## D-305 complete Home materialization

The bounded complete reader, original model conversions and fail-closed mapping check are locally qualified
with full native/financial preservation, same-process tenfold source-read evidence and refreshed startup
compilation. QA-06 retains its explicit remaining composition/memory/cold/ANR/device/iOS acceptance rather than
claiming a two-second result. See [delivery](22-home-materialization-delivery.md). D-304 backend/CI/CodeQL are
actually successful after its isolated-peer repair; the one-use follow-up is deleted.


## D-306 reviewed-input maintenance and rejected performance trial

The actual in-memory plan/goal review defect and its GoalDetail lifecycle caller are corrected together;
all database/choice/file/rights checks remain. New regressions, affected source flows, native goal commands,
protected Android original Home/data and final main package are qualified locally. This is maintenance of
QA-03/ENT-02/03 consistency with test limits inactive; it does not close their external release gates.
The bounded recurrence-cache candidate is removed after full native and isolated worker evidence fails to
justify its complexity. Do not repeat that trial without a new cause or add its discarded cases to the
shipping count. QA-06 remains In progress. D-305's one-use actual hosted check passes all four workflows;
its timer is deleted. [Evidence](23-reviewed-input-integrity-delivery.md).

## D-307 target-market bank discovery follow-through

First source-based discovery now covers all fourteen current target countries, with separate consumer,
business, document-only and unresolved export evidence plus a live-provider comparison. Postbank and
later-market AIB/KBC specimen priorities are explicit; no guessed schema, new preset or connection is
shipped. BANK-01 and IMP-01 remain In progress for exact format/fixture and separately accepted provider
work. No app or test count changes; D-306 package/native results are retained without repetition.
[Country matrix and original references](24-target-bank-research.md).

## D-308 complete saved-filter names and native actions

A reproduced Transactions saved-name overflow is corrected with growing bounded rows and the unchanged
original query/argument behind complete native buttons. Eighteen Windows 200% process-local contexts pass
216 target observations and 54 actual queries without stored writes. Other compact choices pass and remain
unchanged. [Native/package scope and negative evidence](../../quality/saved-filter-actions-readable.md).
Other A11Y-03 controls and actual OS/readers/phone/iOS acceptance remain open; no new unit-test count.

## D-309 protected schema connection follow-through

The actual covered-startup upgrade callback now retains one supplied unpooled keyed connection during
history/pending-migration review, then restores its caller's original state. Closed current/old/unknown
history reproduces two/three/two opens before correction and one afterward; already-open ownership,
cancellation and unknown-prefix refusal remain. Eight new cases plus two existing cases qualify ten
distinct related behaviors across retained runs. The existing old-schema fact also preserves original
account/entry/settings and balance through real migration; its final changed assertions alone are rerun.
No new complete-suite count is claimed.

Original-source Release/AOT Android reference/tenfold measurement on the same retained ciphertext and
keys observes two opens becoming one, with complete raw schema/typed rows unchanged. Strict test and
Windows builds pass. This bounded callback improvement is not whole cold-start, two-second, ANR, phone,
iOS or release acceptance. QA-06 stays In progress. Native/package receipts and exact limits:
[protected schema connection lifetime](../../quality/protected-schema-connection-lifetime.md).

## D-310 verified purchase validity and renewal presentation

The original plan screen now exposes original owned-purchase lifecycle alongside offers: paid/grace ends,
last verification, observed auto-renewal, Lifetime and future scheduled changes. Historical device and
identity packets remain separate from current effective rights; cached/outage facts explicitly require
refresh. No next debit, price, early scheduled access or cancellation is inferred. No raw purchase/account/
receipt identifiers are shown. Seven new focused cases cover all six languages and original financial
preservation, including live retranslation without store reads. Native/build/package evidence and retained
failures: [verified purchase lifecycle](../../quality/verified-purchase-lifecycle.md). Genuine store, Mac/iOS, phone and release gates stay open.

## D-311 readable report and forecast tables

The original trend/history and forecast comparison/parts templates now separate identity/date from whole
monetary packets, reusing the existing native horizontal AmountReadout and overflow hint. Native original
German month/amount overflow is reproduced at 360 width. Eighteen Windows en/fa/de, light/dark, 360/412/1280
contexts at process-local 200% stress and six original-source Release/AOT Android contexts pass with original
stored data unchanged. Charts/PDF/calculations and schema are unchanged. No new unit-test count or unrelated
successful suite replay is claimed. Exact scope, retained failures and package:
[financial table readouts](../../quality/financial-table-readouts.md). A11Y-03, spoken readers, actual OS/device/iOS and release gates remain open.

### D-312 actual-device pre-restore safety discovery

Physical Samsung qualification observed an inaccessible retained safety file after automatic-copy restore.
The original Backup page now exposes current-profile/install device-only candidates through bounded
header-only discovery; original authenticated validation and explicit consent still own replacement.
Seven focused tests and 18 native Windows layout contexts cover the changed boundary. Exact actual phone
continuation, signed candidate and external limits: [physical Android safety recovery](../../quality/physical-android-safety-recovery.md).


### D-313 approved tunnel path

No-port production origin, exact TLS/client metadata boundary and separate owner-credential connector are locally qualified, including gateway-preserving operations. No unrelated successful mobile or financial scenario was repeated. See [outbound tunnel origin](../../quality/outbound-tunnel-origin.md).


### D-314 actual physical workload and remaining latency

The complete independent original-source ARM64 batch qualifies Home/search/validated writes and preservation on the approved Samsung. Five successful groups remain retained; only the incomplete layout-observation group continued. Initial Home/ledger still misses its objective; investigate the actual remaining managed-read/projection/validation/layout work without speculative caching or weaker integrity/KDF scope. See [physical protected workload](../../quality/physical-protected-workload.md).


## D-315 QA-06 measured ledger improvement

34 related tests pass with independent EF expectations, optional inclusive date bounds, complete values/order and cancellation. Strict Windows/Android builds and complete signed main upgrade pass. On the same retained Samsung ciphertext/keys, 100,000-entry public read goes 7,773 -> 4,313 ms and original native load/layout 10,446 -> 6,964 ms, with complete preservation. Exact/empty/wide search and original main 979 EUR history remain correct; successful unchanged financial writes were not replayed. No full-suite rerun/count is claimed. QA-06 remains In progress for remaining latency, cold/ANR/low-memory/device/platform acceptance. See [physical ledger read](../../quality/physical-ledger-read.md).


## D-316 QA-01 planned-work device batch

The unchanged signed D-315 APK now has actual Samsung main-app evidence for original setup, optional budget and balance goal, manual 4/6 partial/final settlement, future-plan/forecast exclusion/reset, process restart and return to the original Main profile. The two actual expense rows total 10, balance is 990, budget left 40; future 20 gives forecast 970 without a third posting. Native displayed-state acceptance is distinct from full database equality. All data is fictitious and retained; no system setting, reinstall or personal data change. No unchanged builds, unit suites or APK qualification were repeated. Broader recurrence/notification/platform/performance/release gates remain open. See [physical planned work](../../quality/physical-planned-work.md).


## D-317 QA-01 native CSV consumer

Actual Samsung original file-picker cancellation/selection, own-format two-row preview/import, zero-new/two-known duplicate preview with disabled action and persisted Undo after process restart pass in the retained D-316 fictitious profile. Exact note/tag/literal formula-like text is retained; financial balance 990 -> 980 -> 990, original two payments, 40 budget left and 970 forecast are verified. Return to Main at 979 and hash-matched exact-file cleanup are complete. Unchanged app/APK and successful builds/tests remain D-315. No producer-export roundtrip, real-bank/aggregate/hostile-file/full raw equality or complete device/platform/release claim. See [physical CSV and Undo](../../quality/physical-csv-undo.md).

## D-318 QA-01 observed first-plan fix

The actual first-plan blank-list defect is fixed without persistence/financial changes. Independent original native Save on retained protected data and 18 final en/fa/de light/dark 360/412/1280 large-text layouts pass; standard Windows and complete signed Android builds have zero warnings/errors. Main upgrade without reset displays the retained unpaid plan before any segment toggle, handles empty Upcoming/return and preserves the displayed 979 EUR balance. No successful unrelated unit/device/crypto scenario is replayed or unit count increased. Broader device/platform/performance/release gates remain open. See [first plan publication](../../quality/first-plan-publication.md).

## D-319 QA-01 physical debt/lent correction

Both original physical debt/lent lifecycles now have native evidence: positive setup signs, complete 11- and 6-row estimates, cancellation without posting, principal transfers versus 10 expense / 5 income, explicit final repayments, six retained rows and cash 1,495 after signed cold upgrade. The observed zero-balance missing-terms hint is corrected; 38 focused native Windows cases and zero-warning Windows/complete Android builds pass. Main returns 979. Unit counts and successful unrelated checks are retained; broader device/platform/performance/release gates remain open. See [physical loan lifecycle](../../quality/physical-loan-lifecycle.md).

## D-320 QA-06 native compilation qualification

The independent phone caller now matches production assembly/profile inputs. A captured extra CoreLib
AOT-method selection reproduces native failures under load; removing only those additional roots while
preserving SDK defaults completes all 12 en/fa/de reference/tenfold Home cases and full schema/typed-row
preservation on the same retained ciphertext/keys. Align restored runtime/compiler/task servicing versions,
fix matching-configuration restore, keep only the explicit review window awake and retain scoped failures.
Windows and complete signed Android builds pass without warnings/errors; the main data-preserving upgrade
shows 979 EUR and actual expense/Cancel returns unchanged. No financial C# algorithm, schema, KDF/native
cipher or full-suite count changes. Current loaded observations remain 1.96-2.65s reference/8.83-10.29s tenfold;
this is stability qualification, not a general speed improvement, full cold/ANR matrix or release approval. See [native evidence](../../quality/physical-home-compilation.md).


## D-321 QA-06 complete current-row read improvement

D-321 reduces the measured complete 100,000-row reader from 4231 to 3344 ms and complete Home input read from 4653 to 3811 ms on retained Samsung ciphertext/keys. All 12 final en/fa/de native Home cases preserve complete financial presentation and raw schema/typed rows; loaded Home still takes 1.83-2.73s reference/ 8.47-9.58s tenfold. Related validation is 42 passing cases plus one final interceptor-reader case (nine added behaviors), not a full-suite rerun. Final strict Windows/complete signed offline Android builds and data-preserving main Home/All-history/return pass. QA-06, cold/ANR statistics and external platform/release gates remain open. See [evidence](../../quality/physical-home-row-reading.md).


## D-322 QA-06 trusted complete-row metadata

D-322 reduces the measured tenfold complete Home input read from 3811 to 3306 ms using exact-query null metadata, retaining all original 32 fields/conversions and safe interceptor fallback. All 12 final en/fa/de phone Home cases retain full financial presentation and raw schema/typed rows; loaded Home remains 1.92-2.83s reference / 7.23-9.51s tenfold. 45 related cases pass (two added behaviors), strict Windows/complete signed offline Android builds and main Home/All-history/return upgrade pass. A recurrence candidate without reliable native gain and temporary worker probes are fully withdrawn. No whole-suite rerun, two-second/cold/ANR/platform or release acceptance is claimed. See [evidence](../../quality/physical-home-null-bitmap.md).


## D-323 bounded physical report acceptance

The unchanged D-322 signed offline main APK passes an original Samsung report/filter/PDF handoff batch:
October income 1,000/spending 21/surplus 979 and five-row drill-down; empty September scope replaced by a
saved October expense filter (three rows/net -21); actual PDF chooser offered and cancelled without a
recipient; filter retained after cold PID 24409 -> 27876 and then explicitly deleted; original unpaid
plan 3/headroom 976 and cash-account zero income/spending retain transfer semantics. Final report/ledger
and Home return 979 without financial Save/reset. Eleven saved-device assertions pass; no new managed
test count, build, APK or unrelated successful checks are repeated. This is English/light displayed-state
acceptance, not raw main-store equality, inspected phone PDF pages, all reports/readers/iOS or release.
See [physical evidence](../../quality/physical-report-filter-pdf.md).


## D-324 bounded physical digital-receipt acceptance

The unchanged signed D-322 main APK passes six original Samsung digital-PDF receipt flows, one per
en/fa/de/es/fr/it source language: distinct total phrases and Persian digits yield the reviewed 24.50 EUR,
not cash 50/change 25.50/order 999999. Every original editor is explicitly cancelled/discarded without Save;
the same PID 27876 retains five rows/net 979 and final Home 979. The incomplete picker observation alone
continues, and only six exact hash-matched newly owned input files are removed. No managed test count,
app build/install or unrelated successful check is repeated. English/light presentation, digital text and
displayed preservation are the scope; evaluation notices limit merchant accuracy, and real corpus,
image OCR/camera, raw main-store equality, other native/platform and release acceptance remain open.
See [physical evidence](../../quality/physical-digital-receipts.md).


## D-325 physical OCR correction and next observed defect

Actual Samsung image OCR exposed French À -> Å in NET À PAYER while the number/unit remained correct.
D-325 folds Latin accents only for whole keyword/exclusion comparisons, preserving original source,
non-Latin marks and all numeric/currency/refusal rules. Ten added behaviors reproduce the failure and
retain safety; all 118 related cases pass, followed by test cleanup and zero-warning Windows/complete
signed offline Android builds. Same-file French recognition now gives 24.50 with original Å source;
seven original raster cases include German EXIF and blank missing-total/currency-conflict drafts.
Three pre-fix successes are retained; four final cases and upgrade preserve five rows/Home 979 without
Save/reset. Exact owned inputs and diagnostic package are removed. Real corpus/camera/scanned/iOS and
release gates remain open; a separate dangling transfer-summary caption was observed and is resolved by D-326.
See [physical evidence](../../quality/physical-receipt-ocr-accents.md).


## D-326 observed summary defect resolved

D-326 fixes the observed transfer-only transaction summary: show the existing translated count-only
caption when the actual result-currency dictionary is empty. Retain a real zero result, separate currency
packets, original matching rows and unchanged income/spending/transfer semantics. No new string, schema,
financial write, dependency or native cipher change is introduced. All 18 focused application-source cases
pass across six languages, followed by test cleanup; the Windows and complete signed offline Android
builds have zero warnings/errors. The independent actual Windows page passes 18 en/fa/de light/dark
360/412/1280 contexts with complete scaled captions and unchanged stored inputs. Six successful English
contexts are retained; only the remaining contexts continue after correcting the diagnostic's Persian-digit
comparison and hidden-root measurement order. The original Samsung upgrade shows transfer count 1 with
no dangling total, expenses 3/net -21, income 1/net 1000 and final Home 979 without Save/reset. Phone
preservation is displayed-state evidence, not raw main-store equality. Remaining receipt corpus/camera,
performance/cold/ANR, iOS/provider/Store and release acceptance remain separate.
See [qualified evidence](../../quality/transaction-summary-result.md).


## D-327 complete automatic-copy device batch

D-327 qualifies the original physical automatic-copy lifecycle on SM-A176B and fixes an observed
maintenance overlap. A synchronous onboarding-readiness read raced the legitimate exclusive safety-file
discovery; the native type/source stack identifies DatabaseAccessGate.EnterRead through GetSettings.
Use cancellable GetSettingsAsync readiness, recheck the captured profile set and preserve the original
publication/encryption/interval guards. A successful not-due check clears stale transient feedback without
replacing its file/timestamp. Seven focused cases pass (six together, one final scope case alone; four
added cases), test outputs are cleaned once and strict Windows/final signed offline Android builds pass
without warnings/errors. Twenty-three saved-native observation assertions cover Off/no copy, Daily
creation, one-entry preview, explicit 36-to-37 recovery, separate two-entry safety preview, cold persistence,
final Weekly/Daily without the former error, original-copy preservation and retirement of only the new
fictitious profile. Original successful creation/restore is retained, not repeated after the readiness fix.
Final Main is 979 with its three original profiles/copies and original Daily/password choices; the phone
is released. No native crypto/schema/SDK/permission change or full-suite rerun. Raw main-store/key equality,
power loss, key loss, full hardware retention stress, other devices/iOS/OAuth/Store and release remain open.
See [qualified evidence](../../quality/automatic-copy-readiness.md).


## D-328 TAX-02 physical packet continuation

D-328 completes a scoped physical Germany accountant export on SM-A176B using the unchanged signed offline
D-327 app. The original native chooser delivers two readable ZIPs only to a separately installed offline
receiver: both accounts/five rows, then cash only/the same one transfer plus its unselected Main identity.
The host verifier checks actual CSV/manifest bytes, six member digests, default redactions, blank tax fields,
refund/fee references and separate recorded-kind totals. Empty selection, confirmation Cancel and stale-review
invalidation are exercised. Twelve saved-observation/receipt assertions pass; the receiver strict build has
zero warnings/errors. The new receiver is removed, final Home remains 979 and the phone turn is released.
No production code/APK, schema, permission or managed test count changes; successful unrelated tests/builds
are not repeated. Physical documents/notes-enabled/all-layout, iOS, tax specialist/calculation/filing and
release acceptance remain open.


## D-329 QA-01 physical PDF contents

D-329 fixes an actual anonymous balance row on page2 of the original Samsung report PDF. Repeat each
table's account/section title, exact scope and column headings as one grid header, retaining all original
values, RTL mapping, row membership and footer exclusion. Three new en/fa/de continuation cases reproduce
the original failure; all eight related Reports cases pass. Strict Windows and the complete signed offline
Android build have no warnings/errors. Data-preserving upgrade and six pages from three original native
English/Persian/German PDF exports are checked; final Home979 and original display choices remain. The
receiver is removed and phone turn released. Seven saved-native/receipt assertions and an original-byte
host verifier retain actual context/figures/visible footer evidence. A suspected footer defect is disproved
by pixels; its experimental source/build are withdrawn and not installed. Initial stale uncompressed
Samsung chooser coordinates opened an unrelated app; no further interaction occurred there, and temporary
fictitious-file grant absence is not proven. Final selection uses a fresh compressed hierarchy and exact
local receiver. No financial/schema/native-crypto change or full-suite/performance replay. Wider device,
reader/iOS/provider/Store and release gates remain open.
See [qualified output](../../quality/physical-report-pagination.md).


## D-330 QA-01 own CSV continuation

D-330 qualifies the original Samsung CSV producer/consumer roundtrip on the unchanged D-329 offline APK:
five original rows, all27 exported fields/stable IDs equal after re-export, retained refund/transfer/fee
relationships, disabled five-ID duplicate import, cold original batch and explicit Undo to zero. Only the
new matching-account profile is retired; Main979 and three previous profiles remain. Twenty-three saved
observation assertions pass; only the independent offline receiver is rebuilt with zero warnings/errors.
No maintained unit count, main source/APK or unrelated successful suite/build changes. Bank/aggregate/
malformed-file/full-device/iOS/release gates remain separate.
IMP-01 still requires original external bank specimens; own-format acceptance does not qualify those revisions. See [evidence](../../quality/physical-csv-roundtrip.md).

## D-331 IO-09 row interpretation

Locally implemented: actual own-format/generic preview cards, bounded traversal including invalid/known rows and
changed-mapping refusal are implemented. The19 new behaviors,51 related application,29 bank and23 resource
cases pass; signed offline Android build/upgrade and18 native Windows contexts pass. Phone rendering
remains pending in [evidence](../../quality/interpreted-csv-preview.md). QA-01 remains in progress; no
arbitrary-bank, wider-device, iOS, performance or release gate closes.

## D-332 IO-09 Android page arrival

Locally verified: actual Android pages now arrive at the review heading after Next/Previous, with
three-page traversal, final invalid/disabled boundary and stale generic mapping/refreshed income.
One focused regression,18 native Windows contexts and the complete offline candidate pass; no financial
import or broad suite is repeated. Eleven saved Android assertions retain the English x64/API36/412x800
scope. QA-06 now has two reproduced background ScheduledAlarmReceiver startup-timeout ANRs; their
cause/fix remains open. Samsung/other device/iOS and release gates stay open. [Evidence](../../quality/android-import-preview-arrival.md).

## D-333 notification-only protected ownership

The newly reproduced cold-snooze queue loss is fixed through the original storage host, with focused
managed refusal/disposal coverage and original native action/separate cold metadata persistence on the
x64/API36 emulator. Strict builds, complete signed offline main upgrade and original Home0 pass.
QA-07 gains this scoped local reminder boundary; broader phone/iOS and interrupted-action acceptance
remain. QA-06 retains repeated startup ANRs, now also observed in an independent provider-only start;
neither their cause/fix nor two-second/low-memory acceptance is established. Reuse existing successful
financial/bank/cipher/backend/layout evidence. [Details](../../quality/cold-protected-reminder-startup.md).


## D-334 QA-06 packaging result

Both finite packaging alternatives are rejected: disabling assembly compression fails before
managed startup with a missing compressed_assembly_count native symbol; direct native-library loading
records a real binding ANR and takes24.422s through OnCreate versus the17.755s control. Single samples,
native rebuild differences and public memory counters do not establish the original root cause.
All temporary source/manifest changes are removed; ordinary application/APK remain D-333. No unchanged
successful suite is repeated or new test count claimed. QA-06 and wider device/iOS/release gates remain open.
Continue the unresolved binding path; do not repeat these variants without changed build/runtime
evidence. The full development goal remains active. [Evidence](../../quality/android-startup-packaging.md).


## D-335 A11Y-03 complete menu captions

The More menu now grows complete titles and explanations at native font scale instead of
inheriting compact title ellipsis and a two-line explanation limit. The original360/200% native case
reproduces14 trimmed captions. Both offline and personal-sync editions pass18 en/fa/de, Light/Dark,
360/412/1280 contexts each:1224 native caption observations and612 original command targets. Commands,
routes, fonts, semantic names/hints, palette and financial data flow are unchanged. No unrelated successful
suite is repeated or new unit count claimed. Broader A11Y-03, real OS scaling/readers, phone/iOS, startup
ANR and release gates remain open.
The D-77 blank-editor Back finding is not reproduced with the keyboard closed on D-333 x64/API36;
no navigation code or financial Save is changed. Keep broader platform acceptance separate.
[Evidence](../../quality/more-menu-readable.md).


## D-336 QA-06 packaging continuation

The observed pre-managed assembly-store abort comes from a cached CoreLib header retaining
index193 while the current descriptor table assigns CoreLib192 and Java.Interop193. Validate the
SDK's current compression-item metadata before its incremental timestamp check and regenerate only
incompatible owned cache entries. Seven focused behavior cases preserve valid cache timestamps,
invalidate stale index/size/magic/truncation, accept missing files and refuse outside destinations.
Keep the original SDK compression, serviced runtime, AOT profile, key/data and permissions. No unrelated
successful financial, OCR, backend or More-layout suite is repeated or new managed count claimed.
QA-06 startup ANR, two-second loaded Home, broader Android, Mac/iOS and owner release gates remain open.
[Evidence, failures and limits](../../quality/android-compression-cache.md).


## D-337 QA-06 scoped native timing

Exact-PID, frame-pointer native captures retain the actual background startup ANR. Monotonic
phase filtering places75.34% of888 main-thread OnCreate samples in the clock-reading path; an
isolated native executable reproduces expensive precise clocks without application code in both
original and fresh same-image emulators. Broken callchains and single-sample limits remain explicit.
No clock substitution, kernel restriction, runtime/licence change or speculative app workaround is
shipped. The original AVD/data remain; ordinary D-336 signed APK and same-PID Home are restored.
App code, APK and managed counts remain D-336; no unchanged successful suite/build is repeated.
QA-06/phone/iOS/release remain open. Continue ready release preparation independently of those gates.
[Evidence and limitations](../../quality/android-startup-clock-cost.md).

## D-338 REL-03/04 draft assets

Eight current native Windows draft screenshots in each existing language (en/fa/de/es/fr/it)
are delivered with exact RGB/source hashes and independent fictitious package/storage/non-input
desktop isolation. Five actual native pre-startup refusals, strict Windows/Android Debug builds and
48 visual/pixel checks passed. Only the rejected English budget loading frame was recaptured from
the same fictitious data; 47 successful frames were retained. Shipping UI/APK and managed counts
remain D-336. These are development drafts, not mobile or Store acceptance/publication. QA-06,
broader physical Android, Mac/iOS, provider/accounts and owner release gates remain open.

[Evidence and limits](../../quality/store-drafts.md).

## D-339 report legend completion boundary

Category/composition report names, shares and monetary packets occupy independent growing rows.
Names retain native scaling and complete wrapping; oversized amounts use the existing horizontal
readout, outside the original category navigation button. The 18 final Windows contexts (en/fa/de,
Light/Dark, 360/412/wide at process-local 200%) and six original Release/AOT Android contexts pass
native glyph/viewport/scroll/target checks. Fictitious presentation inputs are explicitly separate
from report calculations; no financial commands execute. Strict builds and the full offline signed
APK pass. The Android caller's initial startup ANR is retained: Wait continued the same original PID,
without restarting its scenario. QA-06/ANR, broader A11Y/OS readers, physical Android, iOS and release
gates remain open. Managed test counts are unchanged; unrelated passed suites were not repeated.

[Evidence and limits](../../quality/report-legends-readable.md).

## D-340 complete default row captions

The remaining inherited RowTitle ellipsis truncates the original Non-monthly bills caption at
200% Windows process-local scale/360 width. The shared default now wraps with its original 15-unit
font and semantic color. All 26 other current consumers already explicitly wrap, so their effective
behavior is unchanged. Eighteen final Windows contexts/36 native captions and six Release/AOT
Android contexts/12 captions pass; original account/entry/budget inputs stay unchanged and no method
is saved. Strict builds and a complete signed offline APK pass. The independent Android startup
ANR remains an actual failure; Wait continued the same PID 1753 to its completed caption result.
Actual OS text scale/TalkBack, broader device/iOS/performance and release gates remain open. No
system setting, financial algorithm, schema, translation or managed unit count changes.

[Evidence and limits](../../quality/budget-group-titles-readable.md).

## D-341 QA-06 experiment not adopted

QA-06 remains In progress. Reject shared-connection Home adoption after actual Android page
qualification, preserving all negative receipts and restoring production source. Repair the
standalone Data-test compilation dependency; do not count removed prototype tests as delivery.
The application and complete signed APK remain D-340.

[Evidence and limits](../../quality/home-publication-session.md).

## D-342 A11Y-03 optional Home category legend

Complete names/shares and independent monetary viewports replace constrained columns and the
chart-hole total. Focused 18-context Windows and six-context Android native checks pass, preserving
original inputs/actions and complete visible source. Hidden rows detach. No unit count increases.
A11Y-03 remains In progress, as do QA-06/ANR and platform/external acceptance.

[Evidence](../../quality/home-category-legends-readable.md).

## D-343 A11Y-03 shared plan-row values

Correct the actual split monetary packet in both original Home upcoming and Plans hosts. Native
18-context Windows and six-context Android checks pass; stored inputs and original navigation stay
intact. No unit count increases. A11Y-03 and QA-06 remain In progress; external gates stay open.

[Evidence and limits](../../quality/plan-row-amounts-readable.md).

## D-344 complete ledger/account monetary surfaces

All six original ledger/account value surfaces are corrected together, preserving actions/selection
and colors.18 retained Windows contexts and six native Android contexts pass component checks.
A11Y-03 remains In progress; QA-07 retains a separate unresolved Windows Canvas root-change AV.
The ineffective application teardown candidate is withdrawn; no vendor/native bytes change.

[Evidence and limitations](../../quality/ledger-account-amounts-readable.md).

## D-345 real OS accessibility qualification

Actual emulator OS2.0/412dp passes36 original ledger/account monetary checks across six language/theme
cases with stored inputs unchanged. Exact captured settings restore. TalkBack service binding and
low-confidence audio/prompt attempts do not pass spoken navigation. A11Y-01 and A11Y-03 remain
In progress; other controls/widths/devices, ANR and external gates remain. No app/APK or test-count
change. [Evidence](../../quality/android-os-accessibility-qualification.md).

## D-346 real OS evidence and native runtime boundary

Actual emulator OS2.0/412dp completes report/forecast tables, report legends and Flex captions
with unchanged stored inputs. Retain successful groups across actual process failures. Exact native
symbol/source evidence locates the observed Mono bridge crash; the serial-major GC trial still ANRs
and is rejected for production. Native TTS Play is audible control evidence, but renewed TalkBack
remains silent/not ready. A11Y-01/03 and QA-06 remain In progress; app/APK stay D-344, no unit-count
increase or unrelated successful-suite rerun.
[Evidence and limits](../../quality/android-os-accessibility-qualification.md).

## D-347 foreground-only native review

Reject geometry-only observations made under an Android ANR dialog. The independent observer
requires actual window focus and checks that retained receipts prove it. The guarded trial passes
the table/report-legend groups but remains unaccepted as a complete sequence after a real mid-budget
ANR. Remove the ineffective precise-Tarjan runtime experiment; Main/APK stay D-344. A11Y-01/03,
QA-06/07 and platform/external gates remain open; no unrelated passed suite is repeated.
[Evidence and final ordinary qualification](../../quality/android-os-accessibility-qualification.md).

D-347 final ordinary follow-through passes24 Home-category/total and original Home/Plans plan-row
amount viewports across12 en/fa/de xLight/Dark contexts at actual OS200%/412dp with real Activity
focus and unchanged stored inputs. These two original runs have no observed ANR through completion;
this is not a general runtime/stress fix. Captured settings restore and MainPID7654 is preserved.

## D-348 reader observation correction

Installed stock UI automation suppresses other accessibility services. The independent bounded
observer preserves TalkBack and serializes its single native client. Actual retained Main gestures
produce Recorded balance accessibility focus and captured audio; offline transcripts are unreliable,
so spoken-reader acceptance remains open. Settings restore exactly, notification flags and MainPID
are preserved, and the owned remote JAR is removed. Main/APK/count remain D-344. No unrelated
successful suites/builds repeat. A11Y-01/03, native ANR, remaining device/iOS and release gates stay open.

## D-349 actual reader input boundary

The retained Main assessment opens an unedited expense draft directly with ordinary adb injection,
not proven TalkBack activation. Further activation is refused without expected focus; original
Cancel returns to Home0.00EUR on the same PID. Console commands do not prove delivered reader input;
denied kernel input is not escalated. Captured settings/notification flags restore and exact remote
files are removed. No app/APK/count change or unrelated successful verification repeats. Stop these
unchanged injection trials; A11Y/native/device/iOS and external release gates remain open.

## D-350 complete original Home comparison

Fifteen new independent-EF date-storage cases pass on the restored original getter. The correct but
inconsistently slower decoder is rejected after24 original native Home observations across candidate
and control; complete data/presentation preservation passes. Measured pages now require actual window
focus and reject older unguarded receipts. Real startup/Main ANRs remain recorded; Main/APK stay D-344.
No full suite or successful unrelated verification is replayed. QA-06/device/iOS/release remain open.
[Evidence and limits](../../quality/home-date-materialization.md).

## D-355 scoped Android Settings text

Correct observed single-line native picker clipping at actual OS200% text, preserving native
selection and original font scale. Retain scoped original-page checks and exact settings
restoration. A11Y-01/03, QA-06/ANR and platform/external acceptance remain open.
[Evidence](../../quality/android-settings-os-text.md).

## D-356 complete original editor caption batch

One six-state editor matrix exposes and corrects chip stroke/text allocation, optional debt
disclosures and the decorative default glyph. Keep original commands/names, scalable readable
text, complete financial drafts and stored inputs. Reuse passed unchanged native surfaces;
A11Y-01/03, QA-06/ANR and remaining platform/external gates stay open.
[Evidence and exact scope](../../quality/android-editor-os-text.md).

## D-357 shared recent CI failure

Fix the compression test process exit after its expected outside-cache rejection. Preserve all
assertions and the actual production target. Runner-equivalent baseline/correction and retained
failure propagation are checked locally; hosted confirmation follows the push. The earlier linked
startup dependency is already fixed and both Linux tests/Android build pass at D-356. App/APK,
managed counts, QA-06 and all external acceptance gates remain unchanged.
[Log evidence and bounded verification](../../quality/android-compression-cache.md).

## D-358 complete measured choice captions

Correct the actual fractional Flex shrink in original goal choices. The accepted36 actual
OS200%/412dp work-editor contexts retain four unchanged settlement passes and recheck all
changed goal/budget consumers. Eighteen Windows goal-choice contexts, preserved drafts/data,
exact settings restoration and signed main upgrade pass. No managed count increase; the
observed finalizer fault, broader A11Y/QA-06/physical/iOS/release gates remain open.
[Evidence and bounded coverage](../../quality/work-editor-os-text.md).

## D-359 incremental Android manifest and retained runtime continuation

Correct the actual ignored optional overlay in an incremental signed Android build. Ordered
file/content fingerprints invalidate only a changed merger; seven focused process cases and real
on/off binary-manifest checks preserve native engine bytes and default offline boundaries.
The main source remains D-358. This does not resolve QA-06: original GoalEditor/Shell retirement
stalls after12 completed input-preserving groups and records actual ANRs. Bounded exact-symbol
GC/JNI samples do not establish the earlier managed finalizer root cause. Temporary diagnostic
phases/forced collections are removed; exact emulator settings restore. D-358's complete signed
APK also upgrades Samsung without reset, permits original Simple-goal cancellation and returns
Home979. This narrow physical evidence is not glyph-fit/OS200%/reader/full-device acceptance.
[Evidence and remaining boundaries](../../quality/android-manifest-overlay-incremental.md).

## D-360 Android retired-root ownership

Release only the retiring renderer's native item fragments, including pending initial adds with
reordered transactions. Eight final original root groups and six translated native Back actions
preserve stored inputs and preferences. The discarded subscription-only candidate retained roots;
the first native candidate's actual missing-container crash is retained and corrected. General
QA-06/finalizer/ANR, physical-device, spoken-reader and iOS/release gates stay open. No managed
count inflation or unrelated successful suite replay. [Evidence](../../quality/android-retired-navigation.md).

The retained original D-359 36-group GoalEditor/theme/language sequence now completes with 4860
Labels retired, all six stored collections unchanged, no Save and exact OS settings restoration.
This closes that bounded reproduction, without asserting every ANR/finalizer path is corrected.

## D-361 original physical display navigation and goal draft

The unchanged D-360 signed APK upgrades Samsung without reset. Original en/fa/de xLight/Dark
Settings/Back paths, advanced goal types/contribution methods and Keep editing/Discard pass
without Save, preserving empty goals and exact Home979. One native root remains after the
transitions. English/Light/Simple and all four captured OS settings restore; the phone turn is
released. Fifty-one assertions over stored observations are not new unit cases or complete
database/device/ANR/reader/iOS/release acceptance; no successful unrelated suite/build repeats.
[Exact evidence and observation corrections](../../quality/physical-navigation-and-goal-drafts.md).

## D-362 original managed startup and rejected model overlap

Application construction is 443ms in the retained Home sample; the original first settings call
is 777ms after native preparation. This is not yet a factory-versus-query causal profile.
Immutable compiled-model overlap completes in 46ms but does not improve application-entry-to-ready
(2747ms original,2779ms candidate); reject it and remove all temporary code. Same zero-balance
Home, original package/source and exact settings restore. App/APK remain D-360; D-361 physical
evidence is retained. No managed count, full-suite repeat or general ANR/loaded-Home acceptance.
[Exact stages, fixture distinction and next boundary](../../quality/android-managed-startup-phases.md).

## D-363 complete first-settings query delivery

Replace measured cold LINQ preparation with the complete guarded settings row, preserving the
original EF command/protection/maintenance and first-row creation contracts. Eight new cases and
two existing concurrency regressions pass selectively; Release/AOT protected reference/tenfold
reads preserve all25 settings fields and complete stored schema/rows, and refuse retired key owners.
Four process-cold original/candidate samples show222-297ms total managed entry-to-Home-publication
reduction, including EF cost shifted into Home. This is a limited improvement, not QA-06 closure.
Continue ready native startup/ANR, reader and remaining Android qualification without replaying
unchanged successful matrices. External/Mac/store/company gates remain separately open.
[Evidence and explicit limits](../../quality/settings-startup-reader.md).

## D-364 minimum-width original editor actions

Fix observed German icon/pattern clipping and wizard footer word clipping with existing growing
native-command surfaces. Retained OS200%/360dp cases distinguish editable viewport scrolling
from static text loss; no financial Save or full-suite replay. Android focused continuations and
18-context Windows action checks are qualified; final footer checks additionally qualify54
original steps in18 contexts. A11Y-03 and QA-06/QA-07 remain In progress, not accepted by a narrow text matrix.
[Exact evidence and remaining gates](../../quality/narrow-editor-actions.md).

The minimum-width review additionally exposes a real unchanged-theme constructor event racing
modal publication. Correct that cause and add three regression cases; four selected cases pass.
Retain and reject nine old step2 Home images despite their successful footer assertions. Their
explicit continuation proves the original modal owner and actual original wizard render. Final
strict builds and the complete signed offline APK pass; Main Home is preserved without reset.

## D-365 original changed-editor physical qualification

Reuse D-364's complete signed package for 18 normal-scale Samsung cases covering original account,
expanded debt-details and monthly-rule disclosures in en/fa/de Light/Dark. Preserve original
names/44dp targets, observed drafts and actual rule-panel behavior; discard without financial
Save. Restore app preferences and exact OS settings with original visible account/plan/Home data.
No app/APK, managed-count or build change. Broader QA-01/A11Y-03/QA-06 and external gates remain.
[Exact phone evidence](../../quality/physical-editor-actions.md).

## D-366 retained minimum-width original surfaces

The original financial/Settings readers qualify seven groups in 42 en/fa/de Light/Dark cases
at real OS200%/360dp, retaining exact fictitious inputs and original command/Cancel/selection
semantics. Source/input/configuration-bound case receipts prevent replay of successful contexts;
the prior 412dp/editor cohorts are not repeated. Restore exact OS settings and original Main Home.
Only independent qualification sources/package change. A11Y-03 remaining controls, spoken-reader,
broader device/QA-06/platform/provider/release gates remain.
[Complete scope](../../quality/narrow-original-surfaces.md).

## D-367 original Android reader navigation

Fix the actual private glyph focus defect in the global decorative icon style. Qualify original
expense/Cancel, More, About/Back and Home with actual TalkBack focus/public gestures across six
en/fa/de Light/Dark contexts, retaining successful scenes across observer failures. Strict builds,
signed offline APK, exact authorized emulator restoration and helper removal pass. A11Y-01 remains
partial: correct speech and remaining controls/physical/platform acceptance are not inferred.
[Exact evidence](../../quality/android-reader-glyphs.md).

## D-368 physical original glyph preservation

D-367's actual signed bytes upgrade Samsung without reset. Nine private glyph nodes disappear,
30 meaningful Home names/roles/geometry remain and six original expense/history/menu actions
return to the same979EUR/profile in one process. System settings remain exact and the phone
turn is released. No code/APK/build/managed-count change. A11Y-01 speech/phone-reader, broader
QA-01/QA-06, iOS/provider/release gates remain.
[Exact physical scope](../../quality/physical-reader-glyphs.md).

## D-369 original Android destination-fee completion and observed lifetime defect

Complete AT-106's original Android stored-fee scope through 12 en/fa/de/theme/mode contexts and 36
native Saves, preserving nine original successes and executing only 27 remaining operations. Exact
fee identities/net effects and all unrelated serialized rows/settings match; original Cancel and
creation policy remain. A terminal SfComboBox finalizer accesses a disposed native group; retire
only the application's owned chevron child on Android unload and restore on load under unchanged
ownership. QA-06's general GC/finalizer/ANR and performance/device/reader/iOS gates remain open.
Six original main contexts complete 24 currency-popup pairs and restore unchanged EUR/Home/OS
state with matching installed bytes. No new managed tests or repeated unchanged successful suite;
strict Windows/full offline Android pass.
[Failure, continuation and actual main evidence](../../quality/destination-fee-android-lifecycle.md).

## D-370 original physical currency lifetime continuation

Qualify the unchanged signed D-369 APK on Samsung without reset: six translated/theme contexts,
24 original owned popup open/close pairs and Settings child returns. Preserve EUR, profile 979 EUR,
original English/Light/Simple Home and exact system state in the same process. Keep initial
focus/hierarchy and host-coordinate failures, retain successful cases and release the phone turn.
No new managed tests/app/APK or repeated unchanged build/suite. QA-06 remains open for general
GC/ANR/performance, physical fee Saves, spoken readers, other devices/iOS and external release.
[Physical evidence](../../quality/physical-currency-lifecycle.md).

## D-371 bounded original physical stored-fee completion

Close AT-106's original editor/store hardware gap in 12 Samsung contexts/36 native Saves with exact
fee identities/net effects and unrelated serialized fields. Preserve original Cancel/creation
policy, signed ARM64/source/PID evidence and completed cases across two observation deadlines.
The fixture is standalone SQLite; main encrypted-adapter/full-device acceptance remains distinct.
Independent strict build passes; main source/APK remain D-369. Original main PID 11372/Home/profile 979 EUR,
exact system state and installed bytes restore; terminal independent caller stops and turn releases.
No new managed tests or repeated unchanged successful main/full-suite verification. QA-06 general
GC/ANR/low-memory/latency, readers/iOS and external release gates remain open.
[Original physical proof](../../quality/physical-destination-fee.md).

## D-372 original Home transport experiment withdrawn

Do not add the independently tested native current-row bridge to the application. All 32 fields,
provider dates, original EF/ownership boundaries and protected typed rows match; 57 native protocol
cases are not new managed tests. Original paired Home is mixed and tenfold median gain only 3.3%.
Preserve the rejected historical presentation comparison and fresh exact control, unchanged
fixtures/keys, terminal-process restoration and ordinary Data output. Main source/APK remain D-369;
no unchanged successful main/full-suite repeat. QA-06's actual latency/ANR requirement stays open.
[Evidence and explicit withdrawal](../../quality/home-native-row-transport.md).

## D-373 original unreviewed scope correctness/performance batch

Deliver one local account-scope index with stable endpoint merging and checked cross-entry totals.
Fourteen new/48 related Core cases and two original AOT overflow refusals on each platform pass.
Use a distinct protected all-unreviewed corpus; the old all-confirmed workload is insufficient.
Retain/resume only the exact pre-encryption reference failure through the original write-function
context, without replacing triggers/keys or recreating completed data. Complete original Home and
raw schema/typed rows match for both shapes/languages on emulator/Samsung. Tenfold descriptive
medians improve about 13.7%; reference and two-second/full-startup acceptance do not improve.
Preserve old/new native GC failures and all completed cases across terminal continuations.
Main strict builds/full offline signed APK and no-reset original Home/profile/system restoration
pass. Phone turn released; QA-06 general native stability, readers/iOS/provider/release remain open.
[Exact result](../../quality/unreviewed-account-scope.md).

## D-374 original Home review-count defect

Done locally/native: count an unreviewed transaction once across currency groups in both Home
notices. Seven new/36 related Core cases,18 Windows, six emulator and six Samsung original contexts
pass with complete fixture-field preservation. Retain the two native-observer failures, correct
only the expected Persian display digits, and preserve already completed English cases. The new
affected protected Home run preserves full raw data and original presentation without replaying
the old baseline; no performance gain is accepted. Complete signed offline APK/no-reset upgrades
and native Home0/979/exact OS restoration pass, phone turn released. General QA-06/reader/iOS/
provider/Store/release scope remains unfinished.
[Evidence and remaining gates](../../quality/home-review-count.md).

## D-375 Home space and title action

Done locally/native: a 48 px title action removes Home's bottom band, preserving its original command,
loading/failure guards and full bottom scroll reach. Eighteen Windows and six native OS 200%/360 dp
contexts pass with complete fixture-field preservation. Main emulator/Samsung upgrades and native
Add/Cancel pass without reset or Save; physical viewport gains 87.82 dp and Home 979 returns. Exact
emulator settings restored and phone turn released. No new unit count or unrelated suite/benchmark
replay; broader QA-06/reader/iOS/provider/Store/release remains open.
[Evidence](../../quality/home-add-header.md).

## D-376 observed Home guidance correctness

Done locally/native: unrelated Income/Transfer/Refund, Once plans and Week/TwoWeeks budgets no longer
complete the named first-expense/regular-payment/monthly-budget steps or hide the whole guidance card.
Use the captured snapshot on the existing worker, retaining dismissal/account/default behavior.
Fourteen new source-policy tests, 18 final Windows and six real OS 200%/360 dp Android contexts pass with
complete fixture preservation. Strict builds/full offline signed APK and no-reset emulator restoration
pass. No unchanged suite/benchmark replay, physical-phone update or broader gate completion.
[Evidence](../../quality/home-getting-started.md).

## D-377 original complete Home empty tags

Bounded delivery: keep every ledger field and original conversion/error/interceptor contract while
avoiding repeated JSON parsing for exact standard empty lists. Nine new tests and all 78 related
Data cases pass; only the final nine affected cases repeat after the binary-collation guard. Current
original native x64 and Samsung comparisons preserve full schema/typed rows and presentation.
Tenfold medians improve descriptively 3025.61 to 2664.23 ms and 6279.32 to 5158.13 ms. Preserve the
actual ARM64 native crash and four retained successful cases; do not close QA-06 or runtime gates.
[Qualification](../../quality/home-empty-tag-materialization.md).

## D-378 exact original native fault investigation

Locate the actual ARM64 fault through verified official symbols, not nearest exports. Reject the
raw-memory-emitting canary mode before installation; remset-only validation completes six original
Home cases/full stored-data and presentation proofs on the same retained keys and financial corpus.
This bounded non-reproduction does not close QA-06/runtime stability. Ordinary caller and unchanged
Main D-377/Home979/OS restore exactly; no new managed count or unrelated suite/build repetition.
[Exact result and limits](../../quality/android-home-bridge-fault.md).

## D-379 original constructor-list experiment withdrawn

QA-06 retains D-377: a candidate removes one explicit empty-tag-list replacement but complete native
x64 Home is slower in five of six corresponding samples per shape. Related78 cases and builds pass;
this does not establish a product benefit. Restore exact source, default independent package/Main0/
OS and clean outputs. No ARM/phone experiment, new managed count or Main APK rebuild. Wider runtime,
performance, reader/iOS/provider/release work remains open.
[Evidence](../../quality/home-tag-list-candidate.md).


## D-380 bounded physical accountant disclosure

Original Samsung UI produces two accountant packets from one newly created fictitious profile: explicit
notes/known-PDF inclusion, then review invalidation and redaction. Actual packet digests, financial-field
equality and byte-exact PDF linkage pass; strict independent receiver build has no warnings/errors.
Retire only that profile and preserve original three profiles/Main979/samePID/OS/Main APK. Receiver and
hash-matched owned input are removed. No new unit count or production/APK change; D-377 remains current.
See [bounded evidence](../../quality/physical-accountant-document-disclosure.md).
Expert/legal, arbitrary documents, full-device stability, iOS and publication are not accepted here.


## D-381 physical interpreted CSV continuation

Close the D-331/D-332 physical gap using only original picker/native controls, retained known synthetic
own-format/generic sources and a fresh profile. Four page arrivals, invalid last-row refusal and stale
sign interpretation until explicit Refresh pass. No Import/Undo; all-history remains empty. Retire only
the new profile/owned inputs and preserve original Main979/three profiles/OS/APK. No production/APK
change, new managed count, successful suite or build replay. Other QA/native/reader/iOS/provider/release
work remains open. [Full scope](../../quality/physical-import-preview.md).


## D-382 required identity characterization and withdrawal

Eighteen new required-identity representation cases retain independent EF values or exact refusal types.
All96 related candidate cases pass; all18 new cases pass after exact source restoration. Six original
protected native Home cases preserve complete schema/typed rows/presentation, but the candidate is
withdrawn for lack of whole-page benefit. Restore ordinary independent package/Main/OS, clean owned
outputs and retain the negative receipts. No Main APK, phone trial, broader suite or performance claim.
[Evidence and remaining boundaries](../../quality/home-guid-storage-candidate.md). QA-06, native GC/ANR,
reader/full-device/iOS/provider/Store and release remain open; continue ready work from measured causes.


## D-385 - Complete bounded CSV input

IMP-01 gains technical complete-read limits, off-window analysis and non-destructive replacement,
including malformed/empty/header-only inputs and late mapping changes. Forty-six new managed cases
pass selectively; native error surfaces/build/package and actual-picker scope are recorded in
[qualification](../../quality/csv-input-safety.md). Original bank specimens and broader device/iOS/
provider/release gates remain open. D-386's development-first order remains authoritative; this is
not a new generic ANR/crash or accessibility experiment.


## D-387 - Preserve CSV financial meaning at quote boundaries

IMP-01/IO-06/IO-08 refuse malformed quoted/unquoted digit concatenation before review publication.
Twenty-two new cases, affected valid exports/large inputs/bank syntax and original native error pages
qualify the correction. No guessed bank preset or unrelated runtime/accessibility/full-suite work.
[Evidence and remaining native/provider/platform gates](../../quality/csv-quotation-integrity.md).


## D-388 - Decision-ready ECB source and usage conditions

FX-01/OD-14 now have a concrete no-upstream-account/zero-data-fee source proposal, validated public
feed and all30 ordered target-currency calculations. Five are direct and25 derived; date-only source
precision and paid-context free-original disclosure require complete compatible integration before
activation. [Proposal and exact boundaries](25-ecb-reference-source-decision.md). Owner source/terms
choice remains open; no provider/SDK/API, default availability, app/APK or new managed-count change.
Bank/AI/metal providers, external hosting/store/iOS and D-386's later qualification tracks are separate.


## D-389 - Owner-approved daily ECB integration

The owner selected the complete D-388 proposal and its usage conditions on2026-10-19. Implement the
fixed daily adapter, all30 target pairs, same-feed coordination, explicit date-only/derived provenance,
version-one compatibility and six-language per-access/pre-payment disclosures. Manual financial input
and original explicit consent remain independent. Forty-five new managed cases and one explicit actual
source case pass selectively; source-only signed household transfer and portable/cold history preserve
evidence. No source schema migration, new SDK/permission or automatic financial update.

See ../../quality/ecb-reference-integration.md for original native/package proof and retained failures.
OD-14 FX source choice is settled for ECB; metal/item feeds, other providers, real public hosting, store
and iOS acceptance remain separate. Continue remaining approved development before D-386 stability/
accessibility tracks. Do not repeat completed source discovery or successful unrelated suites.

## D-390 - QA-06 supported new bridge candidate rejected

All six original protected Home cases pass correctness in one process; dispatcher gaps worsen in
all twelve historical corresponding samples, reaching2465.7064ms reference. No unchanged baseline
replay or phone extension. Ordinary caller and exact unchanged Main/Home/OS are restored; cleanup
passes. QA-06 remains In progress, with native crash/ANR and target acceptance open. Production/APK
and managed counts remain D-389. [Evidence](../../quality/android-new-bridge-trial.md).

## D-391 - Original reader speech-request boundary

Nine actual en/fa/de action/amount/cancellation text/locale/role packets and six real original reader
open/cancel activations match native focus/return evidence. Exact native/secure reader preferences,
app choices/Home/OS/APK are restored on the same Main15560; independent helper removed. No production
change, managed-count increase or unrelated successful verification replay. A11Y-01 remains In progress
for actual audible output and wider paths/platforms; QA-06 and release gates remain open.
[Qualification](../../quality/android-reader-speech-requests.md).


## D-392 - Physical aggregate/Undo gap and actual corrective delivery

QA-03 financial qualification now includes original explicit Link and replace/Keep both choices,
disabled incomplete review, dependent seed refusal and cold detail Undo with restored metadata on
Samsung. Actual ordinary edited Undo deleted a row; immutable journals now cover every new ledger import,
with conservative legacy/attachment/extra-row refusal. Existing Simple aggregate ranges are visible.
39 related managed cases (ten new),36 Windows/12 Release Android forms, strict builds/complete signed
APK and final same-profile failed-case refusal pass. Only the failed one-row input repeats after the
fix. Original Main979/five public packets/three profiles and exact OS/owned cleanup are restored.
This bounded gap is closed; QA-03 broader device paths, QA-06, A11Y and external release gates remain
In progress under D-386. [Evidence](../../quality/import-undo-and-retained-aggregates.md).

## D-393 - Physical bulk correction gap closed

Original Samsung selection/search/Home/cancellation and financial tag/review/category/mixed-kind guards
pass on one new six-row/two-account fixture. One positive Delete/Undo respects the original deadline;
day net973-to984-to973 and planned cold metadata preserve all six original rows, intended corrections,
refund and transfer-fee relationships. Restore Main979/five public packets/three profiles/OS/D-392 APK,
remove only the new profile and matched input and release the phone. No production change, new managed
count or successful unrelated build/suite replay. Broader QA-03, QA-06/A11Y and external gates stay open
under D-386. [Qualification](../../quality/physical-bulk-corrections.md).

## D-394 - Physical asset consent and retained account correction

AT-81/QA-04 gains bounded original Samsung new/edit cancellation, fresh explicit accepted saves,
internal transfer and original cold persistence evidence. The observed stale account tab is corrected
through its covered loader with preserved reconciliation drafts; six native Windows contexts and
same-fixture signed phone upgrade qualify the changed return path without replaying accepted writes.
The actual D-393 CI failure is narrowed to its independent fixture source boundary; one existing
managed case and two negative controls qualify the correction, without a count increase or full suite.
General QA-03/device, dedicated QA-06/A11Y under D-386, iOS and external gates remain open.
[Evidence and retained failures](../../quality/physical-asset-consent-and-account-refresh.md).

## D-395 - Owner-requested Plans UI consistency

Plans now follows D-375 Home with a48px real Add action beside the growing title, no lower action
band and full remaining list/empty height. Same command, translated name, filters/warnings and
pushed Back remain. Changed-page native evidence is [D-395](../../quality/plans-add-header.md);
no financial Save or successful unrelated test replay. Broader QA/A11Y/runtime/external gates stay open.

## D-396 - Original reconciliation review and atomic correction

Continue the ready AT-11/ACC-08 financial gap. Retire stale amount/date consent and save only the
unchanged actual-file review, with adjustment and current account metadata in one existing writer.
Twenty focused application/SQLite cases and thirty related ledger regressions pass; six original
Windows consent contexts and eighteen corrected visual observations are retained without replaying
accepted money. Complete signed offline Android package and strict Windows Release build pass.
Original Android native amount edit/reason/Cancel/Accept and one planned cold process preserve the
exact adjustment and account metadata. Original Main/profiles/OS are restored without financial
replay; test output is cleaned. Documentation and final commit accompany this batch; hosted success
remains subject to the exact final CI result. Broader physical-phone/iOS, stability/accessibility and
external release gates remain. [Evidence](../../quality/reconciliation-review.md).

## D-397 - Retain the original committed advance-bill outcome

F2-CON-04/AT-133 exposes an ordinary failure after actual commit, with no consumed form before return.
Retain accepted money across subscriber/navigation errors and let the original action retry only Back.
Sixteen original application/SQLite cases and55 related retained-policy regressions pass. Six original
Windows consent-free Save/Back cases and18 inspected images preserve original sources and never replay
accepted money. A separate Home stale-quick-resource refresh error remains recorded; the whole Windows
runner is not green. Six original Android contexts finish with retained accepted money and no replay;
strict builds, complete signed offline APK, original Main/OS restoration and test-output cleanup
complete. Documentation and push accompany this correction; hosted CI remains an exact-head gate.
[Evidence and retained failures](../../quality/settlement-commit-lifetime.md).

## D-398 - Covered original Home source refresh

The retained D-397 failure and incomplete rename-only case are corrected across original account,
plan, goal, template, filter, category and settings source changes. Original mutable packets,
different files, retired access and arbitrary errors still refuse; rereads stay covered and bounded.
Seventeen focused application behaviors,96 related reader contracts and21 template-action cases
pass. Preserve six native contexts per platform and qualify only one additional account-reference
case. Final strict builds, signed offline APK, original emulator Main/OS restoration and test cleanup
complete locally; documentation and push accompany delivery. Hosted CI and broader/external gates
remain separate under D-386. [Evidence](../../quality/home-refresh-supersession.md).

## D-399 - Original occurrence-command commit lifetime

AT-66/AT-138 preserves accepted foreground payments/corrections through ledger/plan notification
failure and retires the original form before native follow-through.50 focused application cases
(21 new,29 retained),70 related policy cases and eight actual native commands per platform pass.
18 Windows native dialog images and six Android message views avoid financial replay; Android17
exact answers and final Main0/OS restoration pass. Strict builds, full signed offline APK, cleanup,
docs and push complete locally. Latest hosted CI and wider/external acceptance remain separate.
General runtime/A11Y priority stays under D-386. [Evidence](../../quality/occurrence-commit-lifetime.md).

## D-400 - Original occurrence state conflicts

AT-66/AT-138 observed retained forms overwriting newer accepted state. The atomic writer now refuses
these conflicting corrections before link SQL/state mutation. Five new cases,55 app/70 policy cases,
four original Windows/Android native refusals each, six message views per platform, strict builds,
signed offline candidate, Main/OS preservation and documentation are locally complete. Hosted CI and
external/device/iOS gates remain separate; no unrelated successful financial or suite replay.
[Qualification](../../quality/occurrence-state-conflicts.md).

## D-401 - Original occurrence refresh and physical correction lifecycle

The Samsung stale suggestion defect is corrected by shared covered retained-tab reads that preserve
raw drafts, refuse financial retargeting and offer translated Retry.13 existing snapshot/21 resource
cases, six native contexts per Windows/Release AOT Android,42 inspected Windows frames and18 real
Android tab inputs pass. The same physical fixture qualifies metadata-only return, original override/
skip/unskip, cancel/accept manual link/keep and generated delete-on-reopen with one cold read.
Strict builds/full signed offline APK, Main979/five known rows/three original profiles/OS restoration,
retirement of only the new inactive profile and phone release are locally complete. No accepted
money or unrelated successful suites/matrices were replayed. QA-01 overall, exact hosted CI, D-386
runtime/A11Y, iOS/provider/store and release remain open.
[Qualification](../../quality/physical-occurrence-corrections.md).

## D-402 - Original physical prior-period/cross-account refund lifecycle

The bounded Samsung AT-13/14/15 continuation passes on the unchanged D-401 candidate: cancelled
original refund draft, old purchase100, current refunds25/35 into two EUR accounts, refused41 with40
remaining, same-draft correction40 and one cold original relationship/period/allocation readback.
Main979/five known public rows/three original profiles/OS match after only the new inactive profile
is retired and phone released. No app/APK, unit-count or build changes; no successful unrelated
matrix or accepted money is replayed. QA-01 overall, chart pixels, other currencies/iOS, D-386 runtime/
A11Y, exact hosted CI and external release gates remain open.
[Qualification](../../quality/physical-refund-lifecycle.md).

## D-403 - Remaining original portable fresh-install device boundary

AT-57 same-phone independent full original Release uninstall/identical reinstall and original native
first-run restore/cancel/accept/cold are qualified with one account, expense, skipped plan and Monday.
Original re-export matches complete51-table schema/74 typed rows except original onboarding's settings
timestamp; no duplicate account. Strict isolated builds/package/identity and targeted verifier refusals
pass. Main979/PID10005/three profiles/APK/OS match after owned cleanup and phone release. Product source,
APK, managed count and successful unrelated checks remain D-401. Broader QA-01, protected-password/new-
device/iOS, D-386 runtime/A11Y and external gates remain. Evidence: ../../quality/physical-portable-fresh-install.md.

## D-404 - Bounded original card payment and archive continuation

AT-07/AT-10 gain original Samsung card expense/payment/positive credit, dirty archive cancel/accept,
both dependent active plans ended, default-cleared notice, historical report preservation and original
unarchive/cold without resuming plans or choosing a default. Main979/five public packets/three profiles/
OS/APK are restored and only the new inactive profile retired. App/APK and managed counts remain D-401;
no unchanged successful suite/build replay. Broader QA-01, D-386 runtime/A11Y and external gates remain.
See ../../quality/physical-credit-card-archive.md.
Continue remaining ready work without repeating these accepted writes or successful unrelated checks.

## D-405 - Stale ordinary CSV review refused before money

Original own/mapped CSV now uses the existing coherent original-file financial packet in its atomic
writer. Changed opening/history facts and departed-form tokens need explicit Refresh; raw source and
choices remain. Eight new cases, retained related checks and12 original native financial contexts per
Windows/Release AOT Android pass, with strict builds/full offline signed APK and no-reset Main/Home0/
OS restoration. No full-suite or accepted-money replay. Broader QA/import, D-386 and external gates
remain separate. See ../../quality/original-csv-financial-review.md.

Continue remaining ready work, retaining completed cases and existing successful unrelated baselines.

## D-406 - Original physical opening-boundary CSV and retained-tab review

Samsung's original picker/preview/import stores one older12.34 expense and opening-day20 income in
one new fictitious Cash900 profile. Home920 excludes the older row; explicitly moving only opening
October20 to18 gives907.66. One planned cold preserves both original public details/dates/notes and
the durable two-row batch. Original Undo restores Home900/empty ledger while opening18 remains.
A real retained More-tab departure did not call OnDisappearing and left Import enabled. The original
Shell now retires that review on uncancelled tab navigation after existing leave guards. Final full
signed offline Release upgrades without reset and physically proves disabled Import, retained source
and explicit Refresh without financial replay. Strict Windows and complete Android builds have no
warnings/errors. Managed algorithms/tests are unchanged; retained D-405 successes are not rerun or
counted as new cases. Main979/five public packets/three profiles/OS are restored; only the new inactive profile
and exact hash-matched public file are removed, and the coordinated phone turn is released. No schema/model, resource, permission or provider change.
Broader device/import/report, D-386 runtime/A11Y, iOS/store/release and exact-head CI remain separate.
See ../../quality/physical-opening-csv.md for original receipts, preserved failures and completion status.


## D-407 - Original split/join financial state and physical lifecycle

Two real SQLite regressions first prove that retained split/join could overwrite a newer original
note. The coherent actual-file/group/account/category/refund/attachment review is now rechecked in
the existing atomic writer before EntryActions mutates copies. Changed sources/pending input refuse
before money and retain raw shares; known committed notification failures leave the original form
terminal/Close-only. Original writer rights, exact total and attachment ownership remain preserved.
Sixteen new Data and3 actual app-session cases pass selectively, with retained two-case native
Windows/Release AOT Android finance,36 Windows observations across en/fa/de light/dark360/412/1280
and12 Android observations. Observer failures continue the original stored refusal and pending
acceptance without reseeding or replaying successful money. Strict builds and complete signed offline
Main APK pass. Samsung's original new Cash900 profile records one100 expense, splits60/40, reallocates
70/30, preserves both public metadata packets and Home800 through one cold read, then combines once
to one100 Food row/Home800. Main979/five known public packets/three profiles/OS are restored and only
the new inactive fixture is retired before releasing the phone. No full-suite/unchanged successful
matrix rerun, model/schema/permission/provider change or release claim. Broader QA-01, D-386 runtime/
A11Y, iOS/store/external and exact-head CI remain separate. See [original evidence and limits](../../quality/original-split-financial-review.md).


## D-408 - Original holding event review and retained acceptance

Two real SQLite regressions first reproduce a stale correction replacing newer source data and an
ordinary exception after a purchase already committed. The original event/type/history/locations/
accounts/group/refund/attachment/valuation packet is now rechecked in the existing atomic quantity/
payment/fee/price writer. Copies preserve raw drafts and immutable reviewed facts. Foreign money or
unreviewed event identity, changed sources/access and pending input refuse before writes. Known
accepted notification/native-return failures retire the original form with Close-only navigation.
One explicit new location can be adopted only while all original financial facts stay unchanged.
An observed native empty-query redelivery now preserves the loaded review instead of losing it.
Twenty new Data and3 actual app-session cases pass selectively, plus46 original related cases once.
Two original financial cases per independent Windows/Release AOT Android preserve exact quantity,
fee/group/derived price, Cash874/929 and income0/expense1.36 Windows observations cover en/fa/de,
both themes and360/412/1280;12 Android observations cover the new native feedback and retained state.
Original Windows pre-command data is continued without reseeding or accepted-money replay. Strict
builds and complete signed offline Main pass; the independent emulator restores Home0/My account,
exact OS values/size/density and final Main hash through one no-reset upgrade. No physical phone,
private installed key/database, schema/model/permission/provider change or full-suite replay. Wider
holding relationships/device/iOS, D-386 runtime/A11Y and external/release/CI gates remain separate.
See [original receipts and limits](../../quality/original-holding-event-review.md).


## D-409 - Corrected source and retained refund boundaries

Three original real SQLite failures reproduce a corrected holding fee smaller than its retained
refund, a corrected holding source moved to an incompatible account currency and the same ordinary
expense-source currency gap. One shared REF-04 source validator now checks final linked refunds in
the existing atomic ordinary/holding writers before mutation. Explicit valid deletion/unlink and
same-batch source/refund correction remain supported. The actual editor shows amount/account errors
beside their fields, preserves raw input and accepts only an explicit valid correction of that same
original form. Known acceptance remains terminal; no conversion or guessed relinking is introduced.

Thirteen distinct new cases (8 Data, 5 Core) and17 affected existing cases pass in retained selective
cohorts, not a full-suite rerun. Two original native correction cases per independent Windows and
Release/AOT Android preserve exact refund identity/link/amount/currency/metadata, quantity/basis/price,
Cash875/874.75, income0 and net expense0/0.25. Four live observations prove refused Save remains enabled
and accepted Save is disabled. Afterwards36 Windows and12 Android read-only rejected-draft caption
observations cover en/fa/de, both themes and Windows360/412/1280; they rehydrate original refusal
presentation with retired authority and do not replay finance. Strict builds and the complete signed
offline Main pass. One no-reset emulator upgrade restores Home0/My account and exact OS/size/density.
No phone input, Main private database/key read, schema/model/permission/provider change or release
claim. Malformed optional fee text remains a source-inspected next concern, not fixed or reproduced
here. Broader financial/device, D-386 runtime/A11Y, iOS/external and exact-head CI gates remain separate.
See [original evidence and limits](../../quality/holding-fee-refund-limits.md).


## D-410 - Original optional holding-fee input

Three actual editor/SQLite regressions first prove malformed, negative and out-of-range nonblank
fees committed instead of refusing. The original editor now distinguishes invalid input from an
explicit blank/zero removal before building financial entries. It preserves raw fee/note, original
review and enabled correction with existing field-local feedback. Preview omits guessed zero-fee
or wrapped account changes. The actual non-UI editor is linked through its original native-effect
ports; native query delivery remains in its unchanged MAUI adapter. Seventeen distinct new actual
app-flow cases pass in retained selective cohorts, without replaying successful D-409 cases.

Two original native malformed/negative cases per independent Windows and Release/AOT Android each
refuse with complete rows unchanged, then correct that same original form once to fee1EUR. Both
preserve refund0.75EUR with complete original metadata/link, Cash874.75, quantity/basis/derived price,
income0/net expense0.25 and terminal accepted state. Four original live assertions precede accepted
receipts. Windows retains4 live plus6 English captures, then continues only12 pending fa/de captions;
Android continues only read-only presentation after its original observer failure, with6 final native
caption observations. The expectation now respects native Persian digits rather than raw Latin
resource examples; production text was complete. Original failures and accepted files are retained.

Strict builds, complete signed offline Main and no-reset independent emulator Home/OS restoration
pass. No phone input, private Main data/key read, full-suite rerun, schema/model/resource/permission/
provider change or ANR/crash resolution claim. Wider holding/device/iOS, D-386 runtime/A11Y and
external/release/exact-head CI gates remain separate.
See [original evidence and limits](../../quality/holding-fee-input.md).

## D-411 - Refund action for holding fees and original physical lifecycle

An original Samsung purchase exposed an eligible fee refund hidden by the managed-source actions
container. The existing CanRefund row now has its own card; purchase/sale principal edit/delete stay
in holding history. Four original accepted financial commands (purchase, linked refund, same-form
fee correction, sale) produce Cash900→848→849.50→850→879.50EUR. Out-of-range and below-refund fees
refuse with raw drafts retained. One terminal cold preserves five known public packets, one unit,
25EUR/unit original basis/price and displayed realized5EUR before fees, without a tax claim.

Twenty-six inspected Windows captures/native observations and24 final read-only Release/AOT Android
action contexts cover en/fa/de and both themes, plus Windows360/412/1280. Original Android semantic
observer and independent modal/route failures are retained; independent Android modal Cancel is not
accepted. Original seeded rows are reused read-only and accepted money is never replayed. Windows
native refund open/Cancel and the actual Samsung refund Save qualify the original action separately.
Strict builds, complete signed offline ARM64/x64 APK and no-reset emulator restoration pass. Samsung
Main979/five known public packets/original three profiles and exact OS values are restored; only the
new inactive fixture is retired and the coordinated phone turn released. No new unit-test count,
full-suite replay, schema/model/resource/permission/provider or Main private-data/key change. QA-01
overall, other holding variants, D-386 runtime/A11Y, iOS/external/release and hosted exact-head CI remain.

See [original evidence and limits](../../quality/physical-holding-fee-lifecycle.md).

## D-412 - Original holding quantity range input and field feedback

Three original parser and two actual editor/SQLite regressions reproduce Decimal base-unit conversion
and checked quantity-preview overflows. Check the original number against Int64/factor before
multiplication, distinguish range refusal from ordinary invalid quantities, omit an unrepresentable
preview and refuse the original proposed range before financial-entry construction. Keep raw drafts,
original review and explicit same-form correction; preserve the existing checked domain/atomic writer.
The new six-language feedback appears beside Quantity without a duplicate form message.

Sixteen distinct new cases and21 existing resource checks pass in retained selective cohorts; no
unchanged full suite or D-408..411 accepted money/matrix replay. Two original native refusal/correction
cases per independent Windows and Release/AOT Android preserve Cash874.75, source money/refund/price
and basis25 while each valid nonfinancial correction adds2units to the original1. Four original live
observations per platform precede only read-only presentation. After field placement changes, final
Windows18 and Android6 caption contexts reuse the exact original accepted receipts/files with zero
new commands; original Cases retain their original PID and Live is empty. Strict builds, complete
signed offline APK and no-reset emulator Home0/My account, OS and hash restoration pass. No Samsung
input, Main private database/key, schema/model/storage scale or provider change. Broader finance/
holding variants, D-386 general runtime/A11Y, physical/iOS/external/release and exact-head CI remain.

See [original evidence and limits](../../quality/holding-quantity-input.md).

## D-413 - Original manual holding-price Save/delete and accepted result

Bind manual prices and confirmed deletion to a coherent original type/history/price/location packet,
revalidated in the existing actual-file writer with fresh rights and current draft/confirmation.
Keep raw price/date/mode on changed facts, retire departed authority and offer explicit Refresh.
Known accepted Save/delete survives notification/readback failure without another write; an old-row
deletion preserves a separate new-price draft. An ordinary empty return may refresh history; raw or
failed drafts require explicit review. Keep field-local feedback, scalable native actions and six
translations. Fifteen new selective actual app/SQLite cases,74 affected policy checks and21 resource
checks pass; original native receipts and read-only continuation keep successful financial operations
from being replayed. Full runtime evidence and limits: [original manual price review](../../quality/holding-price-review.md).

## D-414 - Original holding history deletion and file-bound short Undo

Complete the original holding delete/Undo cycle: coherent displayed-row/group confirmation, fresh
actual-file writer rights/source checks, exact committed deletion receipt and accepted restoration.
Preserve explicit fee-refund links and orphan receipt ownership/content; reject changed/foreign/
mutated/consumed Undo before money and recheck the restored original packet. Keep uncommitted retry,
retire departed actions and distinguish known acceptance from failed notification/readback. Twenty
new selective actual app/SQLite cases, four affected prior cases and21 resource checks pass. Original
native commands and retained read-only captions are qualified separately, without replaying accepted
work. Short Undo remains process-local; no schema/model, new permission/provider or release claim.
See [original evidence and remaining gates](../../quality/holding-history-undo.md).


## D-415 - Original holding archive confirmation and accepted result

The detail archive no longer saves a retained whole-type copy. Revalidate the original coherent
actual-file type/history/prices/locations packet with fresh Corrections rights and current confirmation,
then change only the tracked type's archive flag. Refuse stale/foreign/mutated/consumed or departed
review, preserve financial history/refund/receipt ownership and raw price drafts, and retain known
acceptance before fallible notification/return. Explicit Refresh reads only; absent/failed reads never
rearm a stale or accepted archive. Thirteen new actual app/SQLite cases pass in selective retained
cohorts, without repeating accepted flows;23 resource checks also pass. Original Windows/Android
Cancel/refusal/archive and read-only captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass. No schema/model, new permission/provider or release claim. Evidence and remaining platform gates: [original archive review](../../quality/holding-archive.md).


## D-416 - Original type editor creation, edit and accepted identity

The actual type form now owns one coherent actual-file original type/history/price review, or one
stable absent new identity. Recheck fresh rights, immutable source and exact pending raw draft in
the existing writer; copy before mutation and preserve noneditable metadata and dimension rules.
A missing original row never becomes a new form on empty query redelivery. Keep invalid/raw input
for correction, retire stale/departed authority, and require close/reopen after changed source.
Known creation/edit acceptance consumes the original form before notification or navigation; a failed
next step cannot create a duplicate or repeat Save. Nineteen new actual app/SQLite cases pass in two
selective cohorts, without replaying accepted cases;23 resource checks also pass. Original Windows/
Android refusal/edit/create and retired captions preserve data. Strict builds, complete signed offline
APK and no-reset Main/OS restoration pass; no schema/model, new permission/provider or release claim. See
[original editor evidence and remaining gates](../../quality/holding-type-editor.md).

## D-417 - Original physical holding lifecycle

The existing D-416 signed offline candidate passes one original Samsung type creation/edit,
quantity refusal/correction, manual price/delete, immediate purchase-group Undo and archive/cold
lifecycle. Cash849.50, original refund link, type name/note and history persist; Main979/five public
packets/exact three profiles/OS are restored and the phone turn released. Only the new inactive
fixture is retired. No new app/APK/tests/builds or wider D-386/iOS/external/release acceptance is
claimed. The observed refund category is Uncategorized, without a Fees-selection claim. See
[original evidence, observer continuations and remaining gates](../../quality/physical-holding-type-price-history.md).

## D-418 - One original coherent holding overview

Replace separate initializing/profile-following overview reads with one actual-file worker transaction
and a complete prepared row/value packet. Recheck original source/rights, bound same-file supersession,
cover loading/failure with explicit Retry, and retire departed publication/native targets. Open/Add
check the actual file even under inactive test policy; an empty read creates no settings. Nineteen
distinct new actual app/SQLite cases and23 resource checks pass in selective cohorts, without a full
suite or accepted financial replay. Original Windows and Release/AOT Android loading/error/Retry,
ready/empty, retirement and native targets preserve all typed fixture rows. Only changed six-language
loading/Retry captions are rechecked after visual correction. Strict builds, complete signed offline
APK and no-reset emulator Main/Home0/OS restoration pass; Samsung is untouched. No schema/model or
privacy boundary changes, speed/general ANR/crash/A11Y/device/iOS/store/release acceptance is claimed.
See [original evidence, observer continuation and limits](../../quality/holding-overview.md).

## D-419 - Complete original holding detail publication and routed identity

Read one actual-file detail/settings packet on a worker without initializing settings, prepare all
quantity/value/history/price fields and publish under an original loading/error/Retry cover. Retire
departed/retained-tab work and recheck original native navigation/file targets. Preserve raw price
input for explicit Refresh, existing accepted editor outcomes and theme-only color rebinding. A new
routed type receives its own editors; old drafts/short Undo/native targets never move to it. Twenty
distinct new actual app/SQLite cases pass in selective cohorts; no full suite or unchanged resource/
financial replay. Retained original fixtures qualify57 Windows views plus only two new query views
and23 Release/AOT Android observations, with no seed/UI financial command. Strict builds, complete
signed offline APK and no-reset emulator Main/Home0/exact OS restoration pass; Samsung is untouched.
A final known-commit/departure test and one retained-source AOT callback control close automatic
readback to a departed view, without replaying completed native matrices. No schema/model/privacy boundary change or general runtime/A11Y/device/iOS/release acceptance.
See [original failures, continuations and limits](../../quality/holding-detail-publication.md).

## D-420 - Original holding-place creation and known metadata outcome

Create a named place only under the original actual-file holding review and pending form owner,
with fresh rights and a stable absent identity. Prompt ownership closes financial Save before the
name dialog. Recheck source and owner in the same transaction, consume the old review on metadata
acceptance, and retain that known identity before notification/readback. A failed following step
shows saved-place feedback and an explicit read-only choice refresh; it never creates the place
again or resets raw financial inputs. Transfer keeps its original source and selects the new destination.
A departed/replaced form cannot adopt old metadata or recover financial authority.

Eighteen distinct new actual app/SQLite cases and 23 resource checks pass in selective cohorts.
The three original baselines fail before repair; later cohorts run only new boundaries/outcomes.
Original Windows UIA and independent Release/AOT Android native controls each create one place and
recover its choices read-only, with zero financial Save, then retain captions across the required
language/theme/width contexts. All typed rows outside that one location addition remain unchanged.
Strict builds, complete signed offline APK, no-reset Main/Home0 and exact emulator OS restoration
pass. No schema/model, new data/permission/SDK/provider or general ANR/crash/A11Y/phone/iOS/release
acceptance is claimed. D-386 and remaining external gates stand.

See [original failures, focused evidence and limits](../../quality/holding-location-creation.md).

## D-421 - Complete original holding-event form publication and routed draft ownership

Keep the event form covered until its original coherent read, actual-file revalidation and complete
field publication succeed. Failed reads offer explicit original-file Retry and Cancel; they do not
initialize display settings or leak an exception out of query delivery. Validate the original event/
type before missing-default scaffolding. Required default creation retains Corrections policy and
one known metadata receipt through readback failure, without recreating a deleted accepted default.
A published raw draft gets read-only Retry; unavailable kind/account/place is never replaced silently.

A new routed event/type owns a blank draft and separate financial session. Empty redelivery retains
the existing form. Original pending/departed reads and accepted financial callbacks cannot publish,
close or activate another view. Keep archived historical payment/place identities and rebind a
retained transfer destination by identity. 22 distinct actual app/SQLite cases pass in selective
cohorts, followed only by affected checks. Final strict builds, 56 actual Windows views and 20
independent Release/AOT Android observations pass with original typed rows unchanged. Complete signed
offline APK and no-reset Main/Home0/exact emulator restoration pass. No unchanged full suite or
resource matrix replay; no schema/model, new permission/provider or general ANR/crash/A11Y/phone/iOS/
release acceptance. D-386 and remaining external gates stand.

See [original failures, native observations and limits](../../quality/holding-event-publication.md).

## D-422 - Original physical holding-place move and dirty discard

The existing D-421 signed offline APK passes one original Samsung opening3/basis30EUR and move1:
Home2 plus D422_Storage1, total3 and average basis10 remain, with Cash900 and an empty current-month
ledger. One new place is adopted by the original move draft without losing quantity/date/note or
changing its source. Keep editing retains raw2; Discard keeps the saved move1. One terminal cold
retains original history and quantities. Restore Main979/exact five public packets/three profiles/
OS/hash, retire only the new inactive fixture and release the coordinated phone turn.
Twenty-four retained-observation assertions are not new unit tests. No new app/APK/build or successful
suite/accepted financial sequence replay. A separate unsaved USD-type draft exposes unexpected
native currency-search text and is discarded; USD selection/new event and its root cause are not
qualified. Next ready work diagnoses only this original input/list boundary. D-386 general runtime/
A11Y, broader QA-01, Mac/iOS, external/expert/store and release gates remain separate.
See [original sequence, failed observations and limits](../../quality/physical-holding-location-move.md).

## D-423 - Original native currency selection, without a product workaround

The independent Release/AOT Android caller opens the actual unchanged AccountFormView currency
control once. After actual native realization, the original visible 44-unit GBP button selects GBP
in both the model and control and closes the popup. Stored accounts and entries compare unchanged;
there is no Save surface. The early active-window dump and raw ContentDescription were incomplete
observers, not proof of a blank product list. Preserve failed observations; remove all speculative
production viewport changes. Main code/APK remain D-421, Main resumes with its original PID/Home0,
and no phone input or system setting changes occur. The final independent strict build has zero
warnings/errors. No unchanged Main build/full suite or accepted financial sequence is repeated;
no new unit-test count, physical USD-input, spoken accessibility or general runtime acceptance is
claimed. Continue remaining ready feature/device work with D-386 and external gates separate.

## D-424 - Original entry-detail attachment owner and complete file names

Bind detail attachment reads, native picking and confirmed removal to the original displayed entry,
actual file, metadata and view owner; recheck cached rights and original bytes under the writer.
A changed/departed picker cannot target another query/profile. Consume known commits before
notification/readback; show the saved/reopen outcome without resubmission. Give the complete file
name its own row, separate from reading/removal actions.13 focused actual-source cases pass; no full
suite replay or schema/model/permission/provider change. Retain54 original Windows caption/Cancel
observations and18 changed-row continuations over the same original file. The latter completes its
report and then exits with native0xC0000005: general runtime acceptance is not claimed. Final strict
builds and complete signed offline APK pass. The normal installed Android emulator exercises one
original PDF pick/add, removal Cancel, original ledger Delete/Undo within8s, cold without the input,
and document-only removal with unchanged expense/balance. Restore Main0/original profile set/OS and
retire only the fixture. These are not physical-device, OCR/viewer, general ANR/A11Y, iOS or release
acceptance. Continue remaining ready original entry-editor/detail work under D-386.

## D-425 - Original editor Save and pending receipt

The original editor financial writer records acceptance before notification and captures its exact
saved attachment destination. Submitted financial rows/document bytes are detached; late profile,
query, draft or view changes cannot adopt the pending write. Document failure stays separate from
saved money and never rearms Save.12 distinct focused cases pass in cohorts10+2; no full-suite replay.
One real Windows editor/native Save preserves expense1234/document bytes through two deliberate
postcommit notification failures; original fixture/account seed is reused after failed observations.
See [evidence](../../quality/entry-editor-save.md). Final Android evidence and wider gates are recorded
there. No schema, compiled model, portable format, permission or provider change. Continue ready
original entry/detail work under D-386; general ANR/crash/A11Y and external release gates remain open.

Final installed Android qualification (D-425): upgrade the complete signed offline APK without reset,
then one original Home/PDF receipt and Save in a separate fictitious profile. The stored24.50EUR/
17October/Food/D425_Cash and13KB document survive one cold after the exact input is removed.
Restore Main0/original public profile set/current-month history/OS and retire only the inactive
fixture.12 focused cases, final strict Windows/Android builds and one actual Windows Save pass;
prior negative observers remain recorded. No physical phone, merchant-corpus, general ANR/A11Y,
iOS/provider/store or release acceptance; original edit/overlap/Undo/detail gates remain separate.

## D-426 - Complete original transaction detail publication and actions

Prepare original entry/reference/group/refund/holding/document/rule facts through one captured
actual-file worker transaction, without creating settings for display. Validate the complete packet
before covered synchronous publication. Keep loading, failed read-only Retry and missing states
separate. A routed identity owns its own session and command instances; old commands, confirmations,
template prompts, OCR/viewer work and departed reads cannot adopt a different publication.

Review updates only the original flag; explicit rule creation uses original category/match and
refuses an already equivalent rule. Delete retains the original exact group/refund/document Undo
contract and deadline. Consume known commits before notification; recovery after saved work only
rereads. A refused viewer request removes only its newly created cache file, while a possibly accepted
native request retains the existing startup cleanup policy.

35 distinct new actual-source behaviors and selective affected/resource checks pass; no unchanged
full suite is replayed. Original Windows evidence includes18 language/theme/width ready contexts,
covered failure/native Retry, query/missing/retired-owner cases and two one-time native metadata
commands through deliberate postcommit notification failures. Preserve the first pre-action report
serialization refusal and continue only its exact original file without reseeding. Final Android,
APK and delivery evidence is recorded in [the detailed qualification record](../../quality/entry-detail-publication.md). No schema/model, portable format,
permission/provider or broader ANR/crash/A11Y/physical-device/Mac/iOS/store/release acceptance is
inferred. Continue ready original financial workflow work under D-386 and the existing external gates.

## D-427 - Complete original transaction editor publication

The editor prepares its original references, group, refunds, tags and rules through one actual-file
worker read transaction. Complete fields stay covered until publication; initial failure offers
Retry and an unavailable explicit source never becomes a new blank entry. A different query owns
fresh raw input and commands. Read-only refresh preserves the original draft; source changes refuse
an unsafe Save. Original detail/template/receipt handoffs and the writer validate exact file/rights
and original financial facts, including later refunds of sibling group entries. Both duplicate fees
keep their values with fresh identities. Pending writer waiting and known acceptance never rearm
saved money. Derived currency flags and optional icon reset finish before the form is uncovered.

33 distinct focused new source behaviors, the related core tag case and23 resource cases pass;
there is no full-suite replay. Original Windows evidence totals96 observations across retained
processes, including18 language/theme/width contexts and two one-time native Save invocations.
The independent Android sequence totals36 original observations, with two native Saves. Final
strict builds, complete signed offline APK, no-reset Main upgrade and protected original form
open/Cancel pass. Native observer failures and general Windows exits remain documented. No schema,
compiled model, portable format, permission or provider change. D-428 remains the next separate
aggregate-overlap/choice/writer/Undo batch. D-386 general runtime/A11Y priorities and external gates
stand.

[Original evidence and acceptance limits](../../quality/entry-editor-publication.md).

### D-428 - Complete original nested account and aggregate workflow

Explicit nested account/default return retains original raw/receipt input and adopts only its known
reference packet. Each new aggregate coverage edge has an independent review, actual-file writer
revalidation and durable consumption record. Original short Undo restores aggregates and keeps the
detail; Home/Transactions use truthful transient notices.22 focused new behaviors,23 resource cases,
retained Windows matrices/actions and23 original Android observations are recorded with failures,
final package and scope in [workflow qualification](../../quality/entry-workflow-ownership.md).
Accepted account/seed/financial commands were not replayed. D-427 source/publication evidence remains
the baseline; no full-suite replay or wider runtime/platform/provider acceptance is inferred.

### D-429 - Complete original plan/debt form ownership

Complete original-file worker publication covers accounts/categories/settings/plan/overrides/payments
and cached readiness without initializing settings. Missing sources cannot become new plans. Fresh
query commands/raw fields, original transaction handoffs, nested account return and reminder/discard
lifetimes prevent late work from borrowing another owner. Explicit readiness refresh preserves raw
input; its temporary failure leaves complete fields editable with Save disabled. Missing debt payment
accounts retain direction, unknown principal and currency. Original new/edit/split writers revalidate
full inputs and read back saved semantic rows atomically. Open overrides count as history; exact
future owner moves preserve recurrence and recorded money. Acceptance precedes observers and cannot
rearm.27 focused new source behaviors, affected selective continuations and resource/native/build
qualification are recorded in [original plan evidence](../../quality/plan-editor-ownership.md).
No successful unrelated suite or accepted native action is replayed. D-386 and external gates stand.

### D-430 - Original plan monetary context

Implemented immutable source/destination parse culture, currency and explicit display-unit capture.
Display changes preserve raw text and original labels/examples; changed units/currencies require
complete reentry. Original writer checks retain context identity and edit revision.22 new focused
cases and selective related checks pass, with preserved accepted native Save and incomplete-only
observation continuation. [Native/build evidence and limits](../../quality/plan-money-context.md).
No schema/model/format, permission/provider/SDK change. D-386 and external gates remain.

### D-431 - Original goal/budget monetary meaning

Each target/contribution and total/category raw amount retains its independent original currency,
display unit and parse culture. Unit changes require complete clearing/reentry; partial edits cannot
renew meaning. Dirty context meaning survives reload, including identical digits in a new unit.
Hidden limits expose actionable review; share-of-income ignores irrelevant fixed amounts and physical
quantities retain their units. Original raw revision/native owner is checked under paired writers.
Known goal/budget/cut acceptance precedes notification and cannot rearm.25 new and75 affected prior
source cases pass selectively, without a full-suite replay. [Native/build evidence and retained failures](../../quality/goal-budget-money-context.md).
No schema/model/portable-format, permission/provider/SDK change. D-386 and external gates stand.

### D-432 - Original transaction and split monetary context

Each original transaction amount, destination amount, source/destination fee, ISO purchase amount and
reimbursable amount retains its own currency, applicable display unit and parse culture. Unit/currency
changes require complete clearing/reentry; partial edits never renew meaning. Original labels and
format examples remain visible. Receipt/template adoption starts from authoritative minor units;
account Undo and read-only/nested return retain original context. Repeating-plan handoff validates only
its supported primary/destination amounts, without borrowing unused fee meaning. Split shares retain
independent contexts; Join uses the original stored total. Raw/context revisions are rechecked under
original writers; known acceptance remains final. 27 new focused source cases and 51 related prior cases
pass across selective executions, with no full-suite or unrelated successful-check replay.
[Native/build evidence and retained failures](../../quality/entry-money-context.md). No schema/model/format, permission/provider/SDK change.

### D-433 - Shared CI recovery (Android native follow-up pending)

The D-422 and D-431 hosted test logs identify the same two failures: the mode-policy scanner treats
review-only source fixtures as shipping readers, and a shared-membership Undo fixture supplies a
fabricated group without the original deletion receipt required by D-414. D-431 Windows additionally
refuses installer 18.11.12306.390 before compiling, matching official image 20261018.275.1 inventory.

The scanner now names the 13 exact additional fixtures and validates their complete outer review-only
build gates; it does not exempt the Diagnostics directory or ordinary Release readers. Seven new
positive/negative gate cases prevent unsupported conditions, outer fallbacks and escaped source.
The Undo policy fixture first obtains one real committed deletion receipt, then revokes membership.
A new case proves refusal leaves both deleted rows and that original receipt intact, followed by
successful original restoration when membership returns. No production commercial or provenance
guard changes. Eight focused Core and 11 Data cases pass after the original failures are reproduced.

The official installer metadata is added without changing either approved compiler hash/file-version
pair, pinned MSVC/SDK, signature validation or immutable package contents.17 local toolchain cases
pass on the existing 18.10.12201.205 installation and compiler 19.51.36257.0; other installer versions
are metadata-only checks. Hosted bd6e9484 CI and native Windows pass; its Android package refusal is preserved below, followed by final hosted recovery.
Application sources, runtime behavior and the complete D-432 APK remain unchanged. Successful app,
Android, backend, native financial and device checks are not replayed locally. D-386 and external
gates remain unchanged.

[Failure evidence and hosted gate](../../quality/ci-common-recovery.md).

### Original account monetary-context scope (D-434, completed below)

Before D-434, AccountFormModel parsed opening/installment raw money through current culture/display
units; AccountDetailViewModel did the same for observed balance. The batch reproduced these boundaries,
then retain original currency/unit/culture and explicit debt direction through display/currency changes,
account save/reload and reconciliation confirmation. Preserve actual-file rights, unknown principal,
original write/notification ownership and recorded ledger invariants. Holding monetary inputs remain a
separate following boundary. Do not replay accepted native actions or successful unrelated suites.
D-386 stability/accessibility order and all external acceptance gates stand.


### D-433 hosted follow-up - Original Linux host snapshot

Commit bd6e9484 passes the complete CI workflow, CodeQL and the native Windows reproducibility/package/
persistence job. The Android native job refuses a changed ca-certificates package before compilation.
The Linux recipe restores the original signed Ubuntu snapshot with an isolated exact-release
certificate bootstrap, strict complete-index retrieval and unchanged final tool/NDK/compiler locks.
A fresh local image passes the original host verifier. Final hosted clean candidates and stock-suite
recovery pass on 05dbecf0; no native binary or accepted package is replaced. [Evidence](../../quality/ci-common-recovery.md).


### D-433 final hosted recovery

Commit `05dbecf089dabc5f433b12dbede82f4e97b10d2b` passes all three workflows:
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906007),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906028) and
[Native cipher candidates](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906072).
The actual Windows job passes exact compiler qualification, two clean candidates, immutable package,
drift refusal and protected persistence/workloads. Android passes host restoration, two clean
candidates, malformed-slice refusals and the official stock suite with reviewed static crypto.
The single scheduled check is complete and deleted; no successful job is rerun manually. The earlier
bd6e9484 Android package refusal remains historical failure evidence. This establishes hosted CI
recovery for that exact commit, not app/device/provider/store/release acceptance.


## D-434 - Original account monetary contexts

Opening balance, optional installment and observed balance retain independent original numeric
culture/currency/display-unit contexts. Display changes preserve raw input, original captions and
format examples; currency/unit changes require complete clearing/reentry. Preserve unknown principal,
historical loan/lent signs and decimal-rate meaning. Original file/presentation/edit revision guards
survive writer waits and reconciliation consent. Reject stored-currency changes before comparison,
including the match path. Postcommit default selection remains bound to the original file and native
owner; a failed follow-up never rearms the accepted account Save.

23 new distinct actual-source cases and 49 prior affected cases pass selectively. Original running
matrices complete 110 Windows/38 independent Release/AOT Android observations; two additional default
scenes and four read-only currency views per platform pass: totals116 Windows/44 Android. Final
strict builds and complete signed offline APK qualification are recorded in the linked evidence. No successful financial action or unrelated full suite is replayed.
See the account-money evidence document for exact identities, failures and limits. D-386 priorities
and physical-device/iOS/provider/store/release gates remain open. Holding monetary inputs follow as
the next separate development batch.

[Account monetary evidence](../../quality/account-money-context.md).


## D-435 - Original holding monetary contexts

Payment, fee, price-currency amount and opening/gift basis retain independent original currency,
display unit and numeric culture. Currency/unit changes require complete clearing/reentry. Quantity
keeps its original numeric culture because per-unit money depends on it; examples remain numeric.
Hidden fields do not reinterpret another event kind. Native account/unit/mode/kind choice and raw
edit revisions remain bound under original writers, including change-and-return. Manual-price
Refresh retains original raw meaning and known acceptance; old confirmations cannot rearm it.
Only a blank optional second amount requests unknown value; malformed nonblank input is refused.

24 distinct new source cases and 46 prior affected cases pass selectively; eleven original failures
are reproduced. Strict Windows, Android Debug and independent Release/AOT builds and the complete
signed offline APK pass without warnings/errors. 147 distinct Windows and 51 independent Android
observations complete across retained original scenes and final continuations, without accepted
purchase/opening replay. All 50 stored table sets match the original final checkpoint; actual Manual
and automatic Purchase valuations are verified separately. Main upgrades without reset with its
original account/Home0.00EUR and unchanged OS settings. No full-suite baseline is invented. D-386 stability/accessibility order and physical-device/iOS/provider/store/
publication gates remain unchanged.

[Original holding evidence](../../quality/holding-money-context.md).

### Next complete secondary monetary-input batch (D-436)

Audit and reproduce the remaining secondary money-entry boundaries in goal contributions/assumptions,
forecast assumptions, display-unit definition publication, occurrence/settlement input and shared-cost editing.
Inspect actual submitted raw values and pending native prompts rather than inferring safety from an
ambient culture variable. Retain original currency/unit/culture through awaits, explicit review and
original atomic writers. Estimates never become ledger entries, and known acceptance never rearms.
Complete the whole affected batch, including actual native paths and original source preservation;
do not replay the completed D-435 matrix or unrelated successful backend/security checks. Physical
holding-type and plain decimal-rate inputs remain separate following numeric boundaries. D-386's
deferred stability/accessibility order and external provider/account/device/release gates stand.

### D-436 in progress - Original goal and settlement source boundaries

Six original failures are reproduced. Original contribution/assumption and settlement parsers,
explicit reentry, invalid nonblank assumption refusal and original goal-writer/accepted-result guards
now pass22 distinct new and69 affected prior cases selectively. Strict Windows compilation passes;
native review, remaining secondary inputs, Android/APK and final delivery are unfinished. Preserve
the original failures and do not count source checks as platform acceptance. See the
[in-progress evidence and remaining batch requirements](../../quality/secondary-money-context.md).

The source continuation now includes the actual forecast prompt's fixed original unit/format and
essential-estimate draft/selective original-file writer, with accepted outcome and profile/restore
guards. Occurrence payment/override parser and field captions are integrated at this checkpoint; subsequent
writer and native-process evidence follows below. This checkpoint totals62 new and135 affected prior source cases,
with no new full-suite baseline. Source successes do not close running-app or release gates.

The next source/runtime continuation passes87 distinct new and149 affected prior managed cases.
Original occurrence file/currency/target and pending-owner checks are integrated. Original household
amount/share inputs preserve their own meanings while explicit currency selection still clears them.
Submitted shared-cost proposals are frozen through native consent and accepted forms retire before
fallible follow-up. Four distinct boundaries pass over the retained actual protected household Editor,
including genuinely changed-row rollback and accepted-result recovery without financial replay.
Windows and the independent protected caller compile without warnings/errors. Visual native matrices,
display-unit publication audit, final Android/APK, canonical closure and push remain unfinished;
source/native-process evidence does not close rendered UI, device or release gates.

The actual display-unit definition audit reproduces one further original SQLite-failure/global-publication
defect. The original selective file/settings writer now publishes only known durable acceptance; its
actual page retains consent/native lifetime and post-commit typing, and explicit readback applies the
saved units without resave. Twelve new distinct cases pass, bringing D-436 source evidence to99 new
and149 affected prior cases. No editable monetary calibration is invented. Final native interface
qualification, Android/APK, canonical complete documentation and push remain open for this batch.

Windows unit-definition qualification now preserves91 distinct observations across18 contexts, including
18 caption-only views of the original accepted outcome. One native unit definition is saved; failed
writes/readback and18 native Toman suggestions preserve original financial state. An actual clipped
Toman caption is fixed using the existing growing action, then checked in every final context. All50
source table sets retain original facts except the explicit unit property/UpdatedAt; no ledger/account
rows exist. Earlier success and accepted action are retained through exact continuations. Windows and
Android Debug compile without warnings/errors. The independent Android unit caller is prepared, not
runtime-qualified. Goal/estimate/forecast/occurrence/settlement/shared-cost native presentation, final
Android/APK and full documented delivery remain open; no new full-suite/device/release claim is made.

Goal contribution/assumption, occurrence payment/override and final-bill native qualification now
preserves149 distinct Windows observations over18 contexts. Five declared actions are accepted once;
ledger amounts234/400/1000 and earmark1234 match actual storage, with all50 table sets preserved after
its final checkpoint. A completed bill's Task-identity observer failure is resolved by exact unchanged
source readback, never another Save; its missing original post-Save frame is not invented. One new
field-local bill source case passes without a product change, giving100 new/149 prior selective cases.
Current independent Windows compilation is warning-free. Settings/forecast/shared-cost native review,
Release/AOT Android, final signed Offline APK and complete documented commit/push remain unfinished.


D-436 native presentation checkpoint: all four Windows groups are complete (91 unit definition,
149 goal/price/occurrence/bill,73 Settings/actual native forecast prompt and54 original protected
household observations;367 distinct native observations, not managed cases). One estimate metadata
Save and36 prompt outcomes are checked without accepted-action replay; all49 non-Settings tables
remain unchanged. Original protected household membership and all50 table sets remain unchanged,
with no accepted cost/repayment Save or HTTP. The native observer currency/count errors are retained
and corrected without altering financial fixtures. Managed counts stay100 new/149 affected prior.
Strict Android Debug and independent complete Offline Release/AOT builds pass without warnings;
actual Android qualification is in progress after correcting startup observation order. Final native
Android, complete Main APK/upgrade and whole-batch documented commit/push are still open. See
quality/secondary-money-context.md. D-386 and all external acceptance gates remain unchanged.


D-436 follow-up: image review finds one real missing main-amount unit-error publication in shared
cost/repayment forms. One actual-source case first fails then passes;101 new/149 affected prior
cases now pass,17 source defects reproduced overall. Only36 affected Windows states are repeated
on the same protected source, all50 table sets unchanged;18 original format observations are kept.
Final applicable Windows observations are367, with36 superseded frames retained as history.
Independent Release/AOT Android units31, secondary fields53 and Settings/native prompts25 complete
with original accepted actions once each;109 observations, not managed cases. The original complete
unit report corrects the mistaken blank-window/wait diagnosis; a later source guard refuses replay.
Main stays the same D-435 APK/PID10762 and Home0.00EUR; OS baseline is unchanged and Samsung untouched.
Remaining: actual protected household Android presentation, final signed Offline Main APK/upgrade,
canonical whole-batch docs and verified commit/push/one CI check. This batch is not yet delivered.

## D-436 - Complete secondary monetary source/native batch

Goal contribution/assumed price, essential estimate, actual forecast prompt, occurrence payment/
override, settlement bill, shared cost/repayment/shares and durable display-unit publication retain
original raw meaning and actual writer ownership. Known acceptance cannot be replayed through a
failed observer/readback.101 new distinct actual-source cases and149 affected prior cases pass;
17 source defects are reproduced.367 applicable Windows and127 independent Release/AOT Android
observations complete with accepted actions preserved, no unrelated suite/financial replay.
The final18 household observations use real new native signed membership; all old revoked devices
remain refused, their exact original APK is restored, and all50 protected source table sets remain
unchanged. Independent fictional setup has no cash ledger entries. See quality/secondary-money-context.md.

Final strict platform/independent builds and complete signed Offline APK are warning/error free.
Main upgrades without reset; final native readback is recorded in the linked evidence. Commit/push
and the one CI check follow the complete final review. No full-suite baseline or device/release
acceptance is invented from these observations.

Next separate ready development: physical holding-type quantity and plain decimal-rate input
contexts, including pending native interaction and original stored writer boundaries. Do not replay
D-436 monetary matrices or completed backend/security checks. Dedicated Android ANR/crash and
accessibility completion remain after ready feature development under D-386; Windows commercial
product delivery remains outside this work list. Owner/company/provider/Mac/Store/publication gates
remain explicit external acceptance, not grounds to idle or reopen settled product choices.

### D-437 - Physical quantity and plain manual-rate contexts delivered locally

Complete one numeric-input batch: independent original culture for asset-type free purity and
per-item gram weight, and manual exchange-rate raw input/pair/date/estimate. Preserve partial raw
edits and display-only refresh, give field-local original-format feedback, and retain actual type
writer revision guards. Manual rates use original file/review/writer ownership through asynchronous
load, save and delete consent; accepted publication/readback never rearms a save. Currency-pair
changes require an explicit new rate input, not silent reinterpretation. Manual rate calculations
remain plain ratios, independent of display-unit factors; no prior ledger amount is converted.

Actual-source defects were reproduced and the complete affected forms/writers were implemented.
Only focused new/affected cases and original native continuations were run. Strict Windows/Android
and complete signed Offline APK/upgrade are verified; final docs and commit/push close this batch. Do not replay D-436's
101 new/149 prior cases or367/127 native observations. Its pushed commit is
e89c403580db42ae620e3baecbd1dd02caab9809; its one CI response confirms all four workflows successful and the follow-up is deleted. No dedicated
ANR/crash/accessibility track, provider/company/account/Mac/Store or publication choice is reopened.

D-437 selective evidence: 42 new actual-source cases and 19 affected prior cases pass selectively, with
eight original source failures reproduced. 203 applicable Windows observations are complete;
the corrected108-note runner saved every observation before terminal0xC0000005, which remains a
real unresolved failure. The separate58-outcome runner succeeds without replaying accepted writes.
All 71 applicable independent Release/AOT Android observations complete on the original source:
48 inputs, 22 original outcomes and 1 final restored-guide scene. The outcome report-name collision
is repaired by source-verified read-only receipt recovery, not repeated actions. Main upgrades
without reset; actual rates/read/return retain My account/0.00 EUR and OS settings. Final reviewed
commit/push and its one CI follow-up follow this complete batch; see
[physical numeric evidence](../../quality/physical-numeric-context.md).

Ready approved local feature development is now complete through this numeric boundary. Continue
with the dedicated Android ANR/crash qualification track (QA-06), followed by remaining Android
accessibility acceptance (A11Y-01/A11Y-03), as ordered in D-386. Start with retained real failures;
do not replay successful monetary/backend/native matrices. Original bank specimens, actual providers,
company/store accounts, public tunnel/mail, Mac/iOS and publication remain explicit external gates.
They do not reopen settled owner choices or authorize guessed specimens, accounts or paid sources.

### D-438 - Retained failure classification and original numeric OS qualification

Complete the original-failure classification before another speculative runtime trial. Exact Windows
public symbols identify fatal exception handling/WinRT finalization, not an Android shared cause.
One new isolated API35 clock control leaves precise-call cost present; no runtime/GC/clock change is
adopted and rejected trials are not replayed. Actual OS200% original physical/rate inputs now have96
completed360/412dp native observations/360 readings, both themes and en/fa/de, unchanged original
50-table checkpoint/base and zero accepted commands. Original startup ANR is retained; Wait preserves
its process. Restore all recorded OS values and original Main17310/My account/0.00 EUR. Strict
independent Release/AOT build is warning-free; Main/code/APK and source-test baseline remain D-437.
See [scoped evidence](../../quality/retained-native-fault-classification.md).

QA-06 remains open for a supported, demonstrated stability remedy and cold/low-memory/performance
acceptance. A11Y-03 gains this numeric scope, not all-control acceptance; A11Y-01 audible clarity and
remaining workflows stay open. Do not restart accepted monetary or prior native suites. Continue
only unresolved qualification with new evidence; company/accounts, real OAuth/providers, public
Tunnel/mail, bank specimens, Mac/iOS and publishing remain external gates.

### D-439 - Original monetary field geometry at real OS200%

Eight original-source scopes complete288 observations/2340 native readings: account72, holding96,
entry72 and goal/budget48 over360/412dp, en/fa/de and Light/Dark. Actual original source/base rows are
unchanged and accepted commands zero; do not repeat prior saved financial actions or D-438's96
physical/rate OS scenes. Strict independent build has no warnings/errors. Original Main17310/account/
Home0.00 EUR and all recorded OS values restore exactly; shipping code/APK and managed baseline remain
D-437. D-438's actual pushed3af93373 CI/CodeQL both succeed; later commits require their own one check.
See [original monetary OS evidence](../../quality/original-money-os-qualification.md).

Next ready qualification section: only the changed secondary/Settings/household and new header/rate
outcome layouts not covered by these completed scopes. Preserve their original accepted checkpoints,
revocations and existing actual reader-gesture evidence. Do not replay waveform/transcription or
ordinary-tap attempts as audible acceptance. General ANR/crash, cold/low-memory/performance, remaining
spoken/platform/device and external release gates remain open; Windows commercial delivery is deferred.

### D-440 - Correct real native decimal-input filtering

The new actual InputConnection boundary reveals three comma-loss failures, now corrected in ordinary
Android numeric entries while retaining numeric keyboard hints, secure masking and maximum length.
20 focused native input observations and four adapter guards pass with unchanged original source/base
and zero accepted Saves. Prior successful source, financial, OS-layout and backend scopes are not
replayed. Strict builds and complete signed Offline APK pass; managed case count does not change.
Continue only uncovered secondary/header/rate-outcome OS layouts and supported stability work;
remaining spoken/device/iOS/provider/company/store/publication gates remain explicit.
See [actual native input evidence](../../quality/android-numeric-input.md).

### D-441 - Secondary monetary system-scale qualification complete

204 observations/900 native readings qualify original secondary fields, Settings/native prompts and
unit definitions at OS200%,360/412dp,en/fa/de and Light/Dark. Complete source/base equality, zero
accepted writes and exact OS/Main3522 restoration pass. Shipping code/APK and managed count stay
D-440; previous successful financial/platform/backend scopes are retained without replay.
Next ready section: only uncovered household-authority, moved Home/Plans header and rate-outcome
layouts. Preserve revocations and accepted metadata; do not resave or reseed for geometry. QA-06,
audible reader clarity, physical-device/iOS and genuine external release gates remain open.
See [evidence](../../quality/secondary-money-os-qualification.md).

### D-442 - Current header system-scale qualification complete

Home and empty/filled Plans complete36 observations/60 header states/120 native readings and24
real Add/Cancel sequences at OS200%,360/412dp,en/fa/de,Light/Dark. Full source/base data and exact
OS/Main3522/Home restore; no accepted writer or successful older scope is replayed. The existing
Undo reservation remains. Shipping code/APK and managed baseline stay D-440.
Next ready geometry section: only uncovered rate outcomes and protected household authority.
Preserve accepted metadata/revocations; no reseeding, resaving or reauthorizing for screenshots.
General stability, audible reader, physical/iOS and genuine external release gates remain open.
See [evidence](../../quality/header-os-qualification.md).

A further36 read-only native-coordinate observations verify the action is physically right of the
complete title (minimum gap8px), without replaying any Add/Cancel. Total D-442 evidence is72
observations/96 header states/192 native text readings; accepted writes remain zero.

### D-443 - Retained rate outcome system-scale geometry complete

36 observations/144 readings qualify historical rate outcomes, complete precision/date and estimate
badge at OS200%,360/412dp,en/fa/de,Light/Dark. Actual disabled template uses original accepted row/
native caption evidence; no seed, writer review reconstruction, accepted command or restoration of
the deleted rate. Complete source/base equality and exact OS/Main3522 restore pass; shipping app/APK
and managed count remain D-440. This is not a new current-rate acceptance frame.
Next ready geometry section: protected household with actual original signed authority. Preserve
revocations and original sources; no re-admission/revocation for visual qualification. General
stability, audible reader, physical/iOS and genuine external release gates remain open.
See [evidence](../../quality/rate-outcomes-os-qualification.md).

### D-444 - Protected household system-scale geometry complete

36 observations/312 readings qualify original protected cost/share/unsaved-repayment layouts at
OS200%,360/412dp,en/fa/de,Light/Dark. Current signed test authority is actually protected/writable;
original one cost/two contacts/zero repayments remain. No enrollment/refresh, new source or accepted
financial write occurs. Exact old fixture APK and OS/Main restore; app/APK and managed count stay
D-440. D-442 and D-443 exact CI/CodeQL both succeed; D-444 requires its own one check.
Next: audit canonical remaining QA-06,cold/low-memory/performance and audible/remaining platform
acceptance. Preserve completed D-438 through D-444 geometry and all rejected runtime/clock/bridge
experiments; repeat only for new evidence or actual changes. Genuine external release gates stand.
See [evidence](../../quality/household-os-qualification.md).

### D-445 - Diagnose original currency query input ownership

Two original emulator scopes produce ten native observations: arrow-only list opening retains the
name's modal-root focus; text sent there appends USD to the name. Explicit actual currency focus,
filtered native USD selection and Cancel/Discard succeed with complete source/base equality and
zero accepted writes. Activity.CurrentFocus alone is insufficient for modal ownership. A separate
signed offline arm64 empty-source prerequisite builds without warnings; phone input waits for the
requested exclusive coordination reply. Historical D-422 phone cause remains unproved. Shipping
app/APK and managed baseline remain D-440; no successful financial action or geometry is replayed.

See [currency focus evidence](../../quality/currency-input-focus-boundary.md).

Next: direct physical USD focus/query only after the exclusive phone turn; preserve the completed
emulator scenes and D-422 opening/move. Other supported stability/audible/platform/external gates
remain separate; no duplicate financial acceptance or rejected runtime experiment.

### D-446 - Move original Home goal progress off the window thread

The unchanged pure goal evaluator now runs on a worker; original metadata/formatting and covered,
current-owner publication remain on the caller. Five new and three affected prior cases pass
selectively, including actual task/thread identity and complete original goal output/source equality.
Three native Home contexts on two retained active goals preserve source/base rows and window-thread
publication; no accepted seed or payment is replayed. Strict Windows/Android/independent builds and
complete signed Offline Main upgrade pass without warnings. Original Main Home139 nodes and OS
remain; no general ANR/crash/two-second/low-memory or phone/iOS/release acceptance is inferred.

See [Home goal worker evidence](../../quality/home-goal-worker.md).

Next: direct D-445 physical USD qualification after coordinated phone availability; supported
remaining stability/performance/audible/platform and genuine external release gates stay open.
Do not replay completed goal, monetary, header, rate or protected-household observations.

### D-447 - Original complete goal-list delivery verified locally

The goal list now reads one captured original file/transaction, keeps its existing financial
calculation on a worker and publishes active/history/funding rows under one translated loading/error
cover. Exact row/command/page lifetime guards refuse stale input; explicit retry and detail return
preserve the original source. No financial algorithm, schema, permission or runtime policy changes.
Fourteen new actual-source cases, three affected prior cases and23 actual resource checks pass
selectively. Eighteen Windows contexts produce84 native observations, six Android contexts produce
36 observations (34new/2retained); original source/base rows remain identical with zero new writes.
Complete signed Offline Main upgrade and actual empty-list Add/Cancel pass without reset; Home139
nodes match the old upgrade except focus, and final return differs only in one drawing-order value.
Strict builds and test-output cleanup pass without warnings. Failed native target/capture-path
observers are preserved and corrected against original receipts without repeating completed scenes.
No general ANR/crash, two-second, low-memory, spoken, physical-phone, iOS or release acceptance is inferred.

See [original goal-list evidence](../../quality/goal-list-publication.md).

Next: retained original goal-detail file/command ownership review, without replaying accepted
financial writes; direct D-445 Samsung USD qualification waits for an explicitly coordinated free
turn. Dedicated remaining stability/performance/spoken/platform and genuine external gates remain.

### D-448 - Original goal-detail delivery locally qualified

Original goal details now use one complete captured file/transaction and worker financial
calculation under a local loading/error/retry cover. Page/identity revisions retire old commands
and pending reads while retaining original dirty money inputs. State/delete writers check the
original action inside their transaction before changes and commit, and preserve known accepted
results across observer failure. Three original regressions reproduce identity, pending-delete and
file-adoption defects;23 new focused cases and58 affected prior goal cases pass in selective runs,
plus19 original monetary-context and23 resource cases. Native Windows146 and Android50 observations
(31new/19retained Android) preserve retained fixture rows with no new fixture writes or replayed fixture
actions. Strict builds, signed Offline Main upgrade/Home139-node/OS checks and test cleanup pass.
Observer/compile failures are retained and repaired without replaying completed native actions.
General stability/performance, spoken/device/iOS, all protected-Main goal variants, native
destructive confirmations and genuine external gates are not accepted by this finite batch.

See [original goal-detail evidence](../../quality/goal-detail-publication.md).

Next: remaining supported final stability/performance and Android accessibility qualification after
ready feature delivery, as ordered by D-386; direct D-445 physical currency input waits for an
explicitly coordinated free turn. Preserve completed feature/scenario evidence. Mac/iOS, original
bank/receipt fixtures and owner accounts/providers/publication remain their concrete external gates.

Final source regressions also cover failed/busy/cancelled Edit validation and retained-stack theme
refresh. Only2 extra native observations per platform qualify the new Edit refusal/retry path;
completed cohorts remain. One new production-app test profile/account-opening/goal Save and one
cold read qualify positive protected-Main account-balance details100/150/50EUR and Edit/Cancel,
with All-history empty. Only that new profile is removed via full two-question UI cleanup. Main's
profile-list/Home139 nodes and0.00EUR return unchanged; English/Dark/Simple and OS are read back.
These are controlled new metadata saves, not repeated fixture payments or full protected-goal,
cryptographic, stability, spoken, physical/iOS or release acceptance.

### D-449 - Original goal-page OS text and native toolbar height

The original Persian title needs58px but received48px at actual Android OS200% text. The owned
native toolbar grows for its measured final line without reducing text size or replacing navigation.
Observers retire with the toolbar and growth requests are deduplicated within that lifetime.
Forty-eight immutable contexts contain216 native observations across360/412dp, en/fa/de and both
themes; completed contexts and native Retry are retained rather than replayed. Original fixture/base
rows remain unchanged with zero financial writes. Two unresponsive candidate sequences and a lost
log-chunk report are preserved; compact read-only durable receipt verification completes the report
without replay. Strict Windows/Android builds, complete signed Offline APK384 AOT entries, same-data
Main upgrade, original Goals read and Home139-node return pass. Exact emulator OS settings return.
No managed test baseline increase or general ANR/crash, speed, spoken/physical/iOS, provider/store
or release acceptance is claimed. See [native goal-page evidence](../../quality/goal-page-os-text.md).

This finite goal-page batch is complete. Dedicated general stability/performance and remaining
spoken/Android accessibility acceptance stay open. Direct physical currency input still requires
its explicitly coordinated free turn. Mac/iOS and original provider/account/publication gates remain
external dependencies; no successful unrelated feature or fixture payment is reopened.

### D-450 - Retire native toolbar listeners on the current Android tree

Three actual native roots reproduce a cached dead observer while the current observer is alive and
different. An exact separate native listener now removes from the current live tree and detaches
its weak owner before releasing the toolbar. Six focused root observations at actual OS200%/360
and412dp retain one active listener and retire four previous owners; two original Persian title
checks retain34px text,58px complete height and66px toolbar. Independent base rows and financial
writes remain unchanged/zero. The direct-tracker candidate's real native activation crash and two
caller compile repairs are preserved. Strict Windows/Android builds and complete signed Offline
APK384 AOT entries, no-reset Main upgrade, Goals/Home139-node return in the same process and exact
OS restoration pass. The transient null tree is continued read-only without replayed input/restart.
MAUI source references are corrected to the actual unchanged10.0.110 pin. No new managed-test count,
replayed D-449216-scene scope or general GC/ANR/speed/spoken/physical/iOS/release acceptance is claimed.
See [native toolbar lifetime evidence](../../quality/native-toolbar-observer-lifetime.md).

Next: remaining supported general Android stability/performance and audible accessibility evidence,
using the existing signed D-450 package where relevant. Keep rejected GC/bridge/clock candidates
closed without new primary evidence. Physical currency input still awaits an explicitly coordinated
free turn; Mac/iOS and real provider/account/store/publication dependencies remain external gates.

### D-451 - Complete one original read-only navigation pulse sequence

After D-450, one Release/AOT process completes18 original detail/Back visits with en/fa/de and both
themes, one current native root/subscription per cycle, no financial commands and exact source/base
equality. A500ms at-most-one UI probe records maximum callback age524ms and no ages over2000ms;
read/publication/Back/Home intervals are812-1436ms, median942ms on the small original source, not
whole Home/cold/tenfold or two-second acceptance. The exact PID's event buffer retains its start
with zero recorded ANR/crash events. Strict independent Android/Windows builds and signed Offline
caller pass. Main remains D-450: same PID15971, Home139 nodes, display and all original OS values.
No old216-scene matrix, accepted financial action, full suite or unchanged shipping APK is repeated;
no managed-test baseline is added. General stability/performance/spoken/physical/iOS/release gates
remain open. See [original navigation evidence](../../quality/original-navigation-pulse.md).

Next: genuinely uncovered cold/low-memory conditions and remaining supported stability/performance
work, without replaying this completed sequence. Explicit physical-turn coordination is refreshed;
no phone input occurs before acknowledgement. Owner accounts/providers/Mac and release gates stay
external. Spoken intelligibility/pronunciation is not inferred from earlier TTS request metadata.

### D-452 - Physical currency input continuation (completed by D-457)

An explicitly coordinated Samsung turn uses the exact complete signed ARM64 D-445 empty-source
prerequisite, without updating Main or changing phone settings. Two actual scenes pass: EUR/name
before the arrow and the opened popup retaining name focus. The following name-input expectation
fails before its actual changed text is recorded; no historical root cause or product defect is
inferred. Preserve those two scenes, failure and the newly installed/stopped probe data. Original
Main7361/Home141/979/public APK hash/OS and all prior packages are restored unchanged; only the
owned stopped probe remains for failure continuation. The phone turn is explicitly released.
The caller will log native/model text, selection and composing spans before retiring a mismatch.
Continuation requires its exact original failure, reconstructs only the retired draft precondition
and does not recertify/recount the two passed observations. No accepted financial action is replayed.
Phone input awaits a new coordinated turn; shipping code/APK and managed baseline remain unchanged.

D-452 actual continuation identifies the controlled-input mismatch: Samsung marks the entire
21-character name as composing; SetSelection21/21 leaves that span0..21, and CommitText USD
replaces it with USD while the currency remainsEUR. FinishComposingText before explicit selection
retains the exact original suffix expectation; corrected physical input produces the24-character
name and independent focused USD query with the original name preserved. Four physical scenes
are now retained (two original/two new), not four new managed tests. Native USD-row selection and
Discard remain incomplete because the current strict owned-panel/visibility observer cannot find
the row. Preserve its typed wait failure; inspect actual popup types/bounds/IME geometry before
changing any product code or action guard. A continuation-path refusal before input is retained
and corrected against the frozen source's actual root-local failure location. Main7361/Home141/
979/OS return unchanged and the stopped owned probe/data remain; the phone turn is released again.

D-457 completes the previously missing native USD selection and actual Cancel/Discard. The prior
four scenes are retained; the one new selection observation and source/base equality pass without
any accepted financial write. A one-physical-pixel popup-height rounding mismatch explains the
observer refusal; production selection code remains unchanged. Main7361/Home141/979/APK/OS and
the original two-package inventory match after removing only the newly owned probe. Both phone
turns are released. Wider device/stability/accessibility/iOS/external acceptance remains open.

### D-453 - Original Main cold start and retained-process background return

The unchanged complete signed D-450 Offline APK passes one genuinely cold Main launch on the
owned emulator: Android COLD/Activity2908ms, new PID17030 and exact original139-node public Home.
The package-scoped background-kill request leaves that same PID alive (LAST/previous); its later
HOT89ms return also preserves the complete public tree. This does not qualify process reclaim,
natural low-memory, whole-Home latency or private database equality. The exact PID's event buffer
retains its start and zero recorded ANR/crash events in this finite scope. APK/OS remain exact;
no financial commands, managed tests, unchanged builds or successful old sequences are repeated.
Only three exact hash-matched remote captures are removed. General stability/performance/spoken,
physical continuation, Mac/iOS and external release gates remain open. See [cold lifecycle evidence](../../quality/main-cold-lifecycle.md).

### D-454 - Recover the original Main task after confirmed cached-process death

A phase-free existing owned Activity supplies the missing cache precondition; preserve its first
LAST result, then require actual CAC/cached=true/adjustment900 before package-scoped kill. PID17030
is confirmed absent and its native intentional-kill event retained before recovery as17431. Android
reports COLD/Activity1334ms; the original139-node public Main, APK and OS values match. No review
route, financial command, build or old successful sequence runs. The quiet owned process is stopped
and two hash-matched remote captures removed. Natural low-memory, private full-data equality,
whole-Home timing and broader stability/platform/release gates remain open. See [cached-process evidence](../../quality/main-cold-lifecycle.md).

### D-455 - Bound the native scroll-readiness candidate without an unsupported product change

The pinned MAUI scroll wait supplies a concrete candidate, not an application-cause proof. The
original16-request control clears its native flag after two of twelve samples; no scroll is armed
and original source/base rows match. A separate hidden-root condition refuses readiness twice;
three actual snapshots establish attached/focused but already-laid-out412x640 geometry. Those
refusals are preserved, not relabelled as a scroll hang or passing regression. Three strict signed
Offline builds pass; experimental source is removed and the exact D-451 independent APK restored.
Main17431/139 nodes/D-450 APK/OS match; no financial command, managed-test increase or successful old
scope is replayed. Stability/performance and external gates stay open. See [native scroll evidence](../../quality/native-scroll-layout-investigation.md).

### D-456 - Resolve the remaining original ECB test-capture race

Failed-job logs from delayed older CI reports match D-433's already-corrected mode-policy,
holding Undo fixture and Windows installer boundaries. The unchanged ECB cancellation test has
one separate race: handler-entry readiness precedes capture of its actual request. Await that
captured request explicitly before cancellation/response construction; retain every original
transport, one-request and cross-pair assertion. Production app/service/APK remain unchanged.
Strict affected API/test compilation has zero warnings/errors; exactly one changed test passes,
no new managed cases and no unrelated local suite/build/device replay. Exact correction-hosted
completion remains pending at commit creation. Preserve unfinished D-452 and all external/runtime
release gates. See [failed-job audit and verification](../../quality/ci-common-recovery.md).

### D-457 - Complete retained physical USD selection and native draft discard

Three actual owned-window observations explain the failed observer: the USD native button is
124px high while its popup exposes123px. Preserve the full-width/owned semantic native target and
44dp minimum within that one-physical-pixel rounding allowance; production controls are unchanged.
The final ARM64 Release/AOT caller passes without warnings/errors,192 AOT entries, offline manifest
and v2/v3 signatures. Actual Samsung selection and Cancel/Discard succeed; four prior scenes are
retained, one new selection observation is added, accepted financial writes stay zero and complete
original source/base rows match. Remove only the new probe and two hash-matched remote captures;
Main7361/Home141/979, original APK/OS and prior packages match. Both coordinated phone turns are
released. Retain the typed wait, initial build-flag and host-verifier/decoder failures separately;
no managed-test increase or successful old suite/financial/matrix replay. D-452 is complete for
this controlled input scope; historical D-422 cause, general stability/spoken/platform/provider and
release acceptance remain open. See [physical continuation evidence](../../quality/currency-input-focus-boundary.md).

### D-458 - Continuous owner-reserved phone and original physical goal lifecycle

The owner reserves Samsung for this project until further notice; cross-project coordination is
no longer required, while data/settings/foreground/authentication limits remain. Reuse the exact
complete signed Offline D-450 APK and upgrade without reset. One new fictitious profile/account
opening100 and goal150 produce the actual100/150/50 list/detail and positive pinned Home card.
Dirty native Discard, pause/resume, confirmed completion/reactivation, archive, one cold read,
declined/accepted native goal deletion and empty All-history pass. Only the new profile is removed
through both native confirmations; original three profiles/Main979/141 post-upgrade nodes/OS and
APK hash match.34 assertions qualify stored observations, not new managed tests. Retain native
null-tree/tab/encoding/host-precondition failures; do not replay accepted commands or old suites.
General stability/performance/spoken/other goal/platform/provider/release gates stay open.
See [original physical goal evidence](../../quality/physical-goal-lifecycle.md).

### D-459 - Original physical earmark and holding-quantity goals

Continue the two remaining goal kinds on the owner-reserved Samsung using unchanged signed
Offline D-450. One new fictitious profile opens with cash900. Earmark150 starts at0, accepts
set-aside25, refuses release26 while preserving that draft, and accepts corrected release10:
progress15/150/135, unassigned885 and original+25/-10 history. One countable holding records
already-owned3 with original basis30, without a cash movement. Quantity goal5 shows3/5/2;
assumed price12 changes only the estimate, not quantity or holding value. Both pinned Home
cards and cash900 agree. One cold launch preserves both original goals, allocation history and
assumption; All/All-types ledger history is empty. Remove only the new profile through both
native confirmations. Original three profiles/Main979/all141 public nodes/OS/APK hash match.
33 behavioral observation assertions and113 capture hashes pass; these are not new unit tests.
Retain navigation/publication/null-tree and host-verifier failures; repair observation against
stored originals without repeating accepted saves or cold launch. Code/APK remain D-450;
no successful old suite/build/emulator scene is replayed. Protected variants, general stability,
spoken accessibility, other physical workflows, iOS/provider/store/release gates remain open.
See [physical goal-kind evidence](../../quality/physical-goal-variants.md).

### D-460 - Original physical zero, exact and exceeded budgets

On the owner-reserved Samsung, one new original profile opens with900EUR and no budget.
Explicit overall/category zero limits show the no-spending hint. One Food expense10 exceeds
both zero limits by10. Editing the same budget to overall10/Food5 gives100% overall and5
category excess. One linked refund4 initially has Uncategorized after an observed mistaken
category toggle: overall6/Food10 is correct for that source. Correct only that refund's category;
overall/category net6, overall left4/60%, Food excess1 and cash894 then agree. One cold read
retains the complete packet. All-empty limits are refused; same-draft category-only5 succeeds
without an overall line. Cancel then accept original budget deletion; it preserves both10/4
ledger rows and894 cash. Remove only the new profile through both confirmations; original
three profiles/Main979/all141 public nodes/OS/APK hash match.35 behavioral assertions and135
capture hashes validate original observations, not new managed tests. Retain native transition,
invalid host-input and observation-verifier failures; do not replay accepted financial creates.
Program/APK remain D-450; no successful old test/build/emulator matrix is repeated. Broader
period/method/rollover, physical workflow, stability/accessibility/iOS/provider/release gates remain.
See [physical budget boundary evidence](../../quality/physical-budget-boundaries.md).

### D-461 - Original budget action lifetime and committed outcomes

Six original SQLite regressions reproduce retained Edit/Delete/Copy commands acting on a newly
published period and misreported accepted writes after Changed throws. Commands now retain their
original publication revision; native disappearance retires commands/consent and hidden Retry.
Pending reads cannot revive a retired page. Copy/replace/delete recheck current ownership in the
actual writer before commit; acceptance is recorded before notification, so explicit Retry reads
without repeating the write.15 new cases and26 existing action cases are verified distinctly. Four cases repeat after the hidden
Retry guard; two later callback/read regressions fail first, then only seven affected cases repeat and pass. Strict Windows and final Offline Android
builds have no warnings/errors. Retain failed native Windows attempts, including the first terminal
UI crash and later target-readiness failures. Three native metadata actions passed their financial
checks before the later observer failure; never replay them. The same source supplies six final
read-only en/fa/de Light/Dark Windows captures with original base/nonbudget rows unchanged.
Complete signed Offline APK has384 AOT libraries; Samsung upgrades without reset and original
copy-next/Edit dirty Discard/delete-cancel/delete/copy-previous/tab/period reads pass with cash900
and empty All-history. Remove only the new profile; Main979/three profiles/all141 public nodes/OS
and intermediate candidate hash match (26 assertions/78 captures). After the last callback/read
guards, upgrade the final signed candidate without reset; existing D316 Edit/Cancel preserves
its budget50/Home990 without any write. Return Main979/three profiles/all141 nodes/exact OS
and final APK hash in the same PID12903 (9 assertions/34 additional captures). These are
original observations, not unit cases; accepted financial/native writes are not replayed.
No schema/provider/permission or financial formula changes. General stability/accessibility,
complete budget financial snapshots, other platform/workflow and release gates remain open.
See [budget action lifetime evidence](../../quality/budget-action-lifetime.md).

### D-462 - Coherent original budget financial publication

The original Budget page now captures its complete review, accounts/categories/preferences, ledger,
plans/states and goal earmarks on one original-file lease and one deferred SQLite transaction.
SQLite materialization and the unchanged financial calculators run on workers. Complete rollover
history, envelope cash to today, refund netting, confirmed-only display, short-period limits and
monthly flex/planned arithmetic retain their original meaning. The covered page publishes the
complete calculated rows synchronously after original review validation; hidden/error/Retry state
never exposes a partial financial packet. No entity/schema, permission, export or provider changes.
Fourteen new actual-source SQLite/financial-worker cases and seven affected prior command cases
pass selectively; no complete suite or accepted financial commands repeat. Eighteen independent
Windows method/language/theme observations retain all fictional source and base rows; own-window
images and exact separate desktop restoration are verified. Android retains10 completed contexts
across a real ANR/timeout and runs only the8 missing contexts on its original source, yielding18
complete contexts; the exact original public report is recovered without replay or invented fields.
Final strict builds and a complete signed Offline APK with384 AOT libraries pass. Samsung upgrades
without reset and reads its retained50/10/40/planned30 budget, then returns Main18990/979/141 nodes,
three profiles/exact OS/final APK hash; no financial writes or profile creates/deletes occur.
The final order correction repeats only its one failing source case and final-source read/builds.
ANR remains open. Complete scope, artifact and retained-failure evidence is recorded in
[budget financial read evidence](../../quality/budget-financial-read.md). General ANR/crash,
speed/low-memory, full accessibility, other original budget workflows, iOS/provider/Store/release
acceptance remains separate and open.

### D-463 - Original budget periods and physical rollover

The actual Samsung weekly/two-week workflow exposed shared captions that still described months.
Use period-neutral editor/limit/empty/copy/delete/planned/rollover wording in all six languages,
show the exact original financial date range in the growing editor header, and omit monthly
calendar annotations for short periods. Keep financial rules, raw draft money and all stored
amounts unchanged; monthly methods and genuinely monthly navigation retain their own wording.
Four new actual-source date-range cases (calendar month, shifted financial month, week, fortnight)
and six affected live-caption cases pass; six resource contracts pass. No complete suite repeats.
Eighteen independent Windows caption contexts produce54 own-window images at360/412/1280 widths;
six Release/AOT Android contexts qualify original captions/date headers without financial saves.
The first Windows observer incorrectly required44px from the existing40px MoveButton; retain its
failure and first image, continue the original source, and qualify only the missing observations.
Samsung uses one fictitious profile, opening900 and one Food expense10, with ten budget metadata
writes: monthly positive carry90/30, Start fresh50/20, surplus-only50/20 and deficit carry45/15;
weekly next30 with carry10 and fortnight next50 with carry20. Final signed Offline APK upgrades
without reset; one cold source read retains Home890, one ledger expense and all three final
period results. Edit/Cancel and delete-confirmation Cancel verify corrected native text without
replaying accepted writes. Delete only the new profile with both native confirmations and retain
Main979/all141 public Home nodes/the original three profiles/exact OS in final PID26903.
Strict Windows/Android builds pass without warnings; complete APK has384 AOT libraries and local
v2/v3 signature. These observations are not additional unit tests. General ANR/crash, speed,
low-memory, full accessibility/TalkBack, iOS/provider/Store and release acceptance remain open.

Evidence: [original budget period and rollover qualification](../../quality/budget-period-rollover.md).

### D-464 - Original physical envelope and Flex budgets

Continue QA-01 on owner-reserved Samsung with the unchanged signed Offline D-463 APK. One new
fictitious profile opens900, posts one Food expense10, earmarks25 for a goal and saves two unpaid
plans: monthly Housing30 and yearly Insurance120, both reminder/autopost off. Original envelope
budget200/Food100 shows cash890, goal25, unspent90 and unassigned775. Correct only Food to5:
overspent5 assigns nothing extra, so unassigned865; spent cash is not deducted twice. Change the
same budget to Flex50: variable10/50/40, fixed0 paid/30 expected, nonmonthly share10/0 paid, month
plan90. Actual due expenses150/two items remain separate from the comparison-only monthly share10.
One original cold start retains Home890/Flex40 left, goal25, both unpaid plans and exactly one
ledger expense. Delete only the new profile through both native confirmations; Main979/all141
public nodes/original three profiles/exact OS/unchanged APK match in final PID3591. Twenty-eight
assertions validate retained observations, not new managed tests. No code/APK change, rebuild,
old suite, accepted financial replay or emulator matrix is needed. General stability, complete
device/workflow acceptance, scaling/TalkBack, iOS/providers/Store and release gates remain open.

Evidence: [original physical method qualification](../../quality/physical-budget-methods.md).

### D-465 - Original budget account and currency boundaries

The original Simple editor now explains its retained account restriction, including archived or
missing references and dirty scope, without changing raw amounts or financial writes. Six new
actual-source SQLite cases and six resource contracts pass; eighteen Windows contexts/images and
six independent Release/AOT Android contexts qualify only the changed note. Old financial/period
matrices and accepted commands are reused. Strict Windows/full signed Offline Android builds have
zero warnings/errors. One retained Samsung profile verifies EUR30 across both accounts, secondary20,
primary10, independent USD30/50 and secondary EUR0 after MonthStartDay15; Persian display and USD
valuation preserve original budget currencies/calendars. Final cold PID19994 retains the corrected
note, EUR0/100, USD30/50/20 and balances890/80EUR and670USD. Delete only the new profile with both
confirmations; Main979/all141 public nodes/original three profiles/exact OS/final APK match.
Twenty-three observation assertions are not unit tests. General stability, full device/workflow,
spoken accessibility, iOS/provider/Store and release acceptance remain open.

Evidence: [original account/currency/pay-cycle qualification](../../quality/budget-account-scope.md).

### D-466 - Original physical incomplete forecast and saved scenario

Reuse the unchanged signed Offline D-465 APK on owner-reserved Samsung. One new fictitious profile
opens100EUR and saves three Once plans: expense150 on23 October, unknown expense on24 October,
income200 on30 October, all reminder/autopost off. Original end150/minimum-50 on23 and one unknown
become end130/minimum-70 on24 only after explicit temporary assumption20. Save one original named
scenario; its accepted Save disables, original detail retains the assumption and correctly defers
future actual comparison until tomorrow. Reset restores150/-50/unknown without changing the saved
scenario. One actual cold launch retains current original forecast and independent saved-70 path.
Cancel then accept native snapshot deletion; original three plans, cash100 and empty All-history
persist. Delete only the new profile with both confirmations; Main979/all141 public nodes/original
three profiles/exact OS/unchanged APK match in PID27525.33 assertions over retained observations
are not unit tests. No code/APK/build/suite or accepted-action replay. Later-date comparison,
missing-rate/other scenario variants, full device/stability/spoken/iOS/provider/Store/release gates
remain open.

Evidence: [original physical forecast qualification](../../quality/physical-forecast-snapshot.md).

### D-467 - Original purchase currencies and valid amount examples

Original Samsung purchases expose an invalid two-decimal error example for JPY. Format only the
original-purchase error from its actual ISO currency and retained numeric culture, independently
of display units. Seven new distinct actual-source cases and selected existing/resource contracts
pass across retained runs.54 Windows error contexts use53 retained valid images plus one bounded
visual recapture;18 independent Release/AOT Android contexts change no financial rows. Preserve
review-timeout, fixture, image-index and native-input failures instead of replaying successful work.
Strict final Windows/complete signed Offline Android builds have zero warnings/errors. Upgrade
without reset; actual COLD PID15323 retains the three original EUR10/20/30 expenses with12.34USD,
123JPY and1.234KWD metadata, Home940, and the corrected JPY rejection/Cancel. Delete only the new
profile with both confirmations; Main979/all141 public nodes/original three profiles/exact OS and
final APK match.23 observation assertions are not unit cases. No accepted financial Save repeats.
Full financial/device/stability/spoken-accessibility/iOS/provider/Store/release gates remain open.

Evidence: [original purchase currency qualification](../../quality/physical-original-purchase-amounts.md).

### D-468 - Serviced native UI framework and original lifecycle qualification

Adopt stable MAUI Controls/Essentials 10.0.120; eight existing modules advance with no new SDK or
permission. Official signatures/licences pass; Preferences/FileSystem sources, ten runtime/cipher
engines and 25 branding assets remain unchanged. Strict Windows/full signed Offline Android
builds have zero warnings/errors. Eighteen independent Release/AOT navigation cycles preserve
complete original rows and one native root/subscription per cycle. Eighteen Windows contexts
produce 36 inspected images with unchanged ledger entries. Correct the observed null expected
count in the report driver and validate its same completed run without native replay.
Samsung and emulator upgrade without reset. Six physical language/theme contexts cancel six
expense drafts and six plan drafts, including six dirty-discard confirmations, with no financial
saves. Delete only the new profile through both confirmations. Phone PID21905 retains Main979,
141 public nodes, three original profiles and exact OS; emulator PID26264 retains Home0/139 nodes
and exact OS. Twenty-seven observation assertions are not new managed tests. Finite event buffers
record each start and no ANR/crash; general stability is not accepted. The ScrollToAsync retry-loop
fix is absent and Mono/.NET10.0.13 unchanged. General ANR/GC/crash, low-memory, complete/spoken
accessibility, two-second Home, iOS/provider/Store and release gates remain open.

Evidence: [bounded native servicing qualification](../../quality/maui-servicing-qualification.md).

### D-469 - Original physical cached-process Main recovery

Use the unchanged signed Offline D-468 APK on owner-reserved Samsung. A phase-free existing owned
Activity supplies the missing cached-process precondition without running a review route. Preserve
the initial adjustment700/cached=false refusal; require actual cached=true/state16/adjustment900
for exact Main21905 before one package-scoped kill. Confirm death before retained-task recovery
as9423/COLD/Activity1392ms. Preserve the initial cover and later equal141-node Home979 in the same
PID, five known original public history rows and three original profiles. Restore This month and
Home, exact OS/both APK hashes; stop only the quiet owned process and remove17 hash-matched XMLs.
No financial command, new profile, app change, build, managed case or successful old suite replay.
Finite native events contain the intentional kill/new start and no recorded ANR/crash; natural
low-memory, private full-data/key equality, general stability, whole-Home speed, spoken/platform/
provider/Store and release acceptance remain open.

Evidence: [physical cached-process recovery](../../quality/physical-cached-main-recovery.md).

### D-470 - Serviced original loaded Home and exact recent-row oracle

Remove only the accidental U+00C2 before the original U+00B7 separator in the independent recent
subtitle assertion; exact production-source extraction proves the old ordinal predicate rejects
the valid original packet. Production source/APK remain D-468. Strict signed Offline x64/ARM64
callers qualify the unchanged original protected10,000/100,000-row corpora after MAUI10.0.120.
Twelve native publications per target complete in one process each, with full schema/typed-row
equality, original balance/recent packets and no financial Save or old raw-reader/suite replay.
Emulator medians1040.70/2680.07ms; Samsung1739.94/4933.12ms. Largest dispatcher gaps487.90/405.60ms
and794.66/1094.35ms respectively. One reference phone sample is2011.86ms; two-second acceptance
remains open. Fresh current presentation baselines are not historical framework speedup proof.
Restore original independent APKs/stopped state; same emulator26264/Home139 and Samsung9423/
Home141/979, shipping APK and OS match. Four hash-matched XMLs removed. Native event scopes have
starts and no recorded ANR/crash; no new managed cases, general stability or cold/low-memory/
spoken/iOS/provider/Store/release acceptance. Next actual ready defect: two corrupted separators
in original household-cost captions, kept outside this frozen-source measurement.

Evidence: [serviced original loaded Home](../../quality/serviced-loaded-home.md).

### D-471 - Original household caption separators

Remove two corrupted separator prefixes from the existing refund-source and destination-value
captions without changing money, parsing or authority.36 final inspected Windows images reuse
36 successful original popup receipts;12 Release/AOT Android selected/popup checks preserve real
protected source/base rows and membership, with no financial Save. Destination-only packets are
explicitly transient formatter/control evidence, not stored cash-linkage acceptance. Preserve
first incomplete Windows captures and Android's terminal observer failure; continue only missing
visibility/readiness checks. Strict builds are warning-free. Complete signed Offline APK384 AOT
upgrades without reset; emulator28047/Home139 and Samsung17769/Home141/979 retain exact public UI
and OS. Restore the original independent APK/stopped state. No new managed cases or unrelated
successful-suite replay. Mac unavailable per owner22 October; independent Android work continues.
General stability, full financial/device/accessibility, iOS/provider/Store/release gates stay open.

Evidence: [bounded original caption qualification](../../quality/household-caption-separators.md).

### D-472 - Original physical account reconciliation

Close the missing Samsung AT-11 boundary on unchanged signed Offline D-471. One new fictitious
profile/account starts100EUR. Native112.34 comparison, change to212.34 invalidation, required
reason and Cancel preserve the draft. One accepted adjustment+112.34 gives212.34 and reconciled
10/22/2026; original row/reason/account/date and metadata persist after one real COLD17769-to21187
restart. Activity1457ms is not full Home. No accepted command is replayed. Delete only the new
profile with both original confirmations; restore Main979/all141 public Home tuples, five known
public history rows, three profiles, period and exact OS/APK hashes.32 observation assertions are
not new managed tests;106 hash-matched XMLs removed. App/APK remain D-471; no build or old suite
replay. Retain observer/viewport/modal-ownership refusals and same-process continuations. General
stability, full financial/device/accessibility, iOS/provider/Store/release gates remain open.

Evidence: [original physical reconciliation](../../quality/physical-account-reconciliation.md).

### D-473 - Original physical receipt Save and stored reread

Close the missing Samsung receipt Save/attachment/cold-reread boundary on unchanged D-471.
One new profile/account opens100EUR on16 October; retained fictitious17 October PDF gives24.50.
One original Save yields Food expense24.50,Home75.50 and one13KB PDF. Hash-delete only the original
input before one COLD21187-to26415 restart. Stored Scan receipt reads the original saved attachment
and opens Edit transaction with the same total; Cancel without Save preserves every public detail
tuple,one expense and75.50. Delete only the new profile with both questions; restore Main979/all141
public nodes,three original profiles,exact OS/APK.27 observation assertions are not new unit tests;
78 hash-matched XMLs removed. No new app code, APK, build or successful-suite replay. Trial watermark/
merchant,real corpus,camera,image OCR,all variants,stability/accessibility/iOS/provider/Store/release
acceptance remain explicitly outside this bounded result.

Evidence: [physical original receipt lifecycle](../../quality/physical-receipt-save-reread.md).

### D-474 - Original physical cold notification action

Close the missing Samsung ARM64 D-333 boundary with a complete signed Offline independent current-
source package and real permission/notification action. One fictitious account/Once plan is
prepared in29556; actual death precedes one In1hour tap. Native broadcast receiver starts30999
without Activity; completed status has protected storage,windows0,one saved/native pending snooze
and equal intentionally populated financial fields/zero ledger. A separate provider31245 preserves
one snooze without storage preparation. Preserve both early in-flight reads and observer refusals;
no accepted action/seed or old successful suite repeats. Independent strict build is warning-free.
Remove only the new fixture package; Main26415/Home979/all141 public tuples,OS and original APKs
remain unchanged.19 observation assertions are not new managed tests. General stability,OS timed
notification restrictions,interrupted-action/low-memory,full device/accessibility/iOS/provider/
Store/release gates remain open. Main code/APK stay D-471.

Evidence: [physical original cold action](../../quality/physical-cold-reminder-action.md).

### D-475 - Original physical timed alarm and notification route

Add scheduled-only mode to the existing independent caller; original application code is unchanged.
After native permission, actual Schedule time/day offset supplies a real future AlarmManager
request, with no immediate Show copy. One original alarm/PendingIntent survives preparationPID1388
death and read-only waiting. Native ScheduledAlarmReceiver starts3318 and posts original184973019
155216ms after requested time under the existing inexact/tolerance policy. No clock change/rearm.
Actual content tap opens the original upcoming12.34EUR/23October due page in the same process;
Back posts nothing. Populated fixture fields and zero ledger match; Home1000 remains. Remove only
the new package; Main26415/Home979/all141 public tuples,OS/APKs and charging-awake15 remain unchanged.
Independent strict signed Offline ARM64 build passes;26 observation assertions are not new managed
cases. Old successful Snooze/financial/Windows suites are not replayed. This awake/charging scope
is not exact timing,Doze,reboot,full lock/device/stability/accessibility/iOS/provider/Store/release
acceptance. Shipping Main/APK stay D-471.

Evidence: [physical original scheduled reminder](../../quality/physical-scheduled-reminder.md).

### D-476 - Exact native-review build boundaries

Repair the two current causes confirmed in actual D-471/D-474 hosted failures: three independent
fixtures now share their dependency's Windows Debug/independent-build gate, and their exact paths
join the source-policy list only after full outer-gate validation. The affected strict Android
Debug billing build and eight distinct existing boundary cases pass; no new tests or shipping
APK/visible behavior changes. Keep successful Release/native/financial evidence without replay.
Hosted completion remains pending at commit creation; external/stability/accessibility gates stand.

Evidence: [original hosted failures and focused recovery](../../quality/ci-common-recovery.md).

### D-477 - Hosted Windows installer metadata

Actual D-473 Windows logs expose a separate new hosted installer18.11.12315.225. Match the dated
official image inventory and add only that exact metadata version; preserve executable hashes,
Microsoft signature, pinned MSVC/SDK and immutable package checks. Eighteen focused qualification
cases pass against the existing signed local compiler. The new hosted compiler/package must still
pass on the exact pushed SHA; no app build/native scene replay or shipping APK change.

Evidence: [actual installer refusal and retained qualification](../../quality/ci-common-recovery.md).

### D-478 - Immutable native producers for serviced hosted Windows

D-476's actual hosted Android build and complete5097-case run now pass with25 skips/zero failures.
The remaining Windows failure is a different deterministic native output under the old candidate.8
identity, despite the same approved compiler. Preserve that refusal and old package identities;
qualify a separate candidate.9, compare every original archive entry, and select the explicit
hosted producer before cache/build use. Twenty-six local script cases,three actual central-version
evaluations and the real local-host/environment boundary checks pass; no new managed cases or
Android shipping APK change. Ordinary consumer and final exact-head CI remain distinct gates.

Evidence: [original drift, producer and consumer qualification](../../quality/ci-common-recovery.md).

D-478 producer follow-up: run39857667608 on9823a734 passes the original producer gates; all five
actual candidate.9 archive entries match the distinct inventory. Normal exact-head main consumer
CI remains pending at adoption commit creation; this is not platform/product/release acceptance.

### D-479 - Bounded physical original-widget continuation

QA-07 gains actual Samsung provider/RemoteViews/PendingIntent evidence through an independent
native host and real single-widget consent, without changing the owner's launcher. Four original
actions, three Cancel returns and actual cold Expense origin26415-to16655 pass; no financial Save.
Original141 public Home tuples/979EUR,OS and shipping D-471 APK match. Remove owned widget2/host47901,
confirm owned0, uninstall only the new helper and remove25 exact hash-matched XMLs. Strict signed
ARM64 helper build and two identity/configuration refusals pass; no new managed tests, Main build
or old financial/emulator cohort replay. Actual launcher placement, all widget/lock variants,
general stability, OS200%/spoken, other devices/API24/16KiB, iOS and release remain open.

D-478 final main109743f5: all five workflows completed successfully;5097 total,5072 succeeded,
25 skipped,zero failed, with actual hosted Windows immutable-package/cold persistence/workloads
and Android/Windows consumer builds passing. iOS is intentionally skipped, not accepted. Retain
historical failures and pending-at-commit records; do not repeat the consumed CI check.

Evidence: [physical widget scope](../../quality/physical-widget-pending-intents.md).

### D-480 - Original four-form Android validation listener lifetime

Actual native observer retirement reproduces three callbacks after the old event-wrapper cleanup.
All four original transaction/plan/occurrence/settlement waits now use the same explicit native
listener, current-live-tree removal and neutralized callback, preserving the existing one-second
bound and form/version guards. Three fixed native checks pass: zero retired callbacks, attached
completion and bounded unattached timeout. Strict Windows/Offline Main/helper builds and helper
identity guards pass. Complete signed384-AOT APK upgrades without reset; original invalid Expense
Save reveals its field/error, then Cancel/Discard preserves979EUR/all141 public Home tuples and OS.
Zero accepted financial Saves; no new managed cases or successful unrelated-suite replay. Remove
only the independent fixture and20 exact hash-matched XMLs. Preserve the real helper JNI failure
and IME/input observer failures. This is not a general ANR/GC/crash fix, full form matrix, speed,
spoken/device/iOS/provider/Store or release acceptance. Exact-head hosted CI follows the push.

Evidence: [native lifetime and final package](../../quality/android-validation-layout-lifetime.md).

### D-481 - Remaining native validation callers

Original plan/occurrence/settlement invalid actions pass on the retained independent x64 editor
source, with unchanged named source/base groups and zero accepted financial Saves. Keep the first
two receipts while correcting settlement's stale diagnostic constructor/public loader. Recover
all3 complete receipts with NativeActionsThisPass0; no successful case, old matrix, financial seed,
managed/resource/business suite or unchanged Main/Windows build is repeated. Strict complete
signed Offline192-AOT caller build passes. Restore exact old independent APK and stopped state,
Main28047/all139 public nodes/OS/APK; remove only2 hash-matched remote XMLs. Samsung is untouched.
No new managed cases or shipping behavior. Actual shipping head0e8c6fa6 has successful CI/CodeQL;
its one-shot monitor is consumed/deleted. General ANR/GC/crash, whole-Home speed, full language/
OS-scale/spoken/device/iOS/provider/Store and release acceptance remain open.

Evidence: [native callers and exact preservation](../../quality/validation-layout-callers.md).

### D-485 - Follow the original Android toolbar between native roots

The actual native caller reproduces outgoing-root callbacks and missing callbacks after a toolbar
moves to a second window. Its separate weak-owner production observer now removes the exact old
registration at detachment and registers on the new root; the initial floating-tree merge is not
duplicated. Two focused native cases pass, for registration before/after attachment, with both
roots alive and zero detached/retired callbacks. Keep prior financial/title/scale/managed suites;
these are not new managed tests or general ANR/GC/performance/accessibility acceptance.
Strict Windows and complete signed Offline384-AOT APK builds pass; no-reset Samsung upgrade and
original Accounts title/Back preserve all141 public Home tuples/979EUR and OS. Remove only the
new helper and two emulator/twelve phone hash-matched XMLs; emulator Main28047 stays unchanged.

Evidence: [original toolbar transition qualification](../../quality/toolbar-observer-transitions.md).

### D-486 - Incomplete original foreign-transfer forecasts

Reproduce four false one-to-one destination results with eight new Core cases. Missing original
cross-currency destination amounts now keep null monetary effects; known source/same-currency/
explicit destination effects remain known. Two new actual SQLite/workspace cases retain that
warning through atomic currency snapshot Save.49 selected cases pass, including ten new cases.
Six original Release/AOT en/fa/de Light/Dark pages expose EUR90/USD50/incomplete1 on one retained
legacy source with all typed rows unchanged. Strict builds and the complete signed Offline384-AOT
APK pass; Samsung no-reset upgrade and read-only original forecast preserve Home979/all141 public
tuples/OS. Restore the exact old independent APK/stopped state and Main28047/all139 tuples.
No source financial rewrite, schema, resource, permission or unrelated successful cohort replay.
Priorb3fa1a0d CI/CodeQL succeed and its one-shot schedule is consumed/deleted. New-head CI and full
physical/platform/security/stability/accessibility/release acceptance remain separate.

Evidence: [original missing-destination conversion](../../quality/forecast-missing-destination.md).

### D-487 - Original saved forecast difference categories

Correct the reproduced cash-sign classification across all eight original ledger kinds. Refunds
reduce unplanned spending, income reversals reduce unplanned income, and transfers/capital trades/
corrections use a separate neutral account-movement row. The residual caption now says other
differences because earlier data edits as well as changed plans can contribute. Original scope,
saved paths, actual daily balances, recorded-later boundaries and source rows remain unchanged.
Fourteen new distinct Core cases and two affected regressions pass;23 resource cases pass,39
selected successes overall. Eighteen Windows contexts yield36 inspected images; six x64/412dp
and six physical ARM64/384dp Release/AOT contexts qualify156 original captions and reversal color
with full typed fixture/base equality. Historical timestamps are seeded once per independent
platform; this is not real elapsed-day comparison or financial form/Save acceptance.
Strict Windows and complete signed Offline384-AOT APK builds pass. Samsung no-reset upgrade
8764-to13827 retains Home979/all141 public tuples/OS; emulator28047/all139 tuples stays unchanged.
Restore both exact independent APKs/stopped states; remove five hash-matched owned XMLs. Prior
e310eadb has four successful workflows and its single-use schedule is consumed/deleted. Final
new-head CI, real elapsed-day/remaining physical variants, security/stability/accessibility/iOS/
provider/Store and release gates remain open. No unrelated successful cohort is replayed.

Evidence: [original saved comparison qualification](../../quality/snapshot-difference-kinds.md).

### D-488 - Physical original goal-reminder queue lifecycle

Qualify eleven original ReminderService/native scheduler states on one fresh independent Samsung
installation: default off, enable, pause/resume, complete/archive, derived reached/unreached,
detail consent, disable and generic restoration. Pending counts are0/1/0/1/0/0/0/1/1/0/1; every
queued request keeps the original scoped goal/date/stable id and24October2026 local09:00. Account
opening100EUR and contribution25/Once date remain unchanged, with zero entries/allocations/plans.
The reached case explicitly changes only the fictitious target150-to100 and restores150; no
money moves. Actual native AlarmManager/PendingIntent uses ScheduledAlarmReceiver with the
existing inexact one-hour window, not an immediate display. Eleven observations are not new
managed tests or financial-form/timed-delivery/lock acceptance. Strict complete signed Offline
ARM64 caller192 AOT passes; Main identity is refused. Retain the original live15501 after the
first observation timeout; no successful state/seed repeats. Remove only this new fixture and
its pending alarm/notification; Main13827/Home979/all141 public tuples/APK/OS match. Five owned
XMLs are hash-checked/removed. Main shipping code/APK remain D-487. Real09:00 delivery, doze/reboot,
notification tap through lock, remaining calendar/device/security/stability/accessibility/iOS/
provider/Store and release gates remain open.

Evidence: [physical goal queue qualification](../../quality/physical-goal-reminder-queue.md).

### D-489 - Physical original period-review reminder queues

Qualify22 original protected ReminderService/native queue states on Samsung in one retained20233:
empty/off and empty/enabled sources, source/off, six states in each Gregorian/Persian/Hijri
calendar, and final restored/off. Financial month starts24/2/13 schedule24October2026 local09:00;
the next two native requests follow their actual calendar, retaining09:00 across the stored DST
offset change. Partial review keeps three pending; finishing only the due period removes that
one and retains two future requests. Details, disable and missed-time cases pass. Compare every
settings field before/after publication; no review step is completed by a reminder. Original
account100EUR stays equal, with zero entries/goals/allocations/plans. These22 observations are
not new managed cases, actual timed delivery or financial-form acceptance. Strict signed Offline
ARM64 caller192 AOT/unchanged five runtime engines passes. Preserve initial caller compile error,
two live observation deadlines and premature cleanup refusal; continue only the same live PID.
The temporary owner-authorized cross-project phone handoff is respected; after a short exclusive
turn, remove only the fresh fixture and five hash-matched XMLs, retain Main13827/Home979/all141
public tuples/APK/OS, and explicitly release the phone for the other active project. Main shipping
code/APK remain D-487. Real09:00 delivery, lock/tap/profile-switch/restore/Doze/reboot, broader
security/stability/accessibility/iOS/provider/Store and release gates remain open.

Evidence: [physical period queue qualification](../../quality/physical-period-reminder-queue.md).

### D-490 - Preserve original imported-group dependencies on Undo

Reproduce unsafe Undo after a later fee is added to an imported transfer. Capture external ledger
and holding group context under the original import writer in internal ImportLinks version2;
refuse later changes before mutation, retaining valid Undo for unchanged pre-existing groups.
Ungrouped journals remain version1; legacy closed groups are supported and unverifiable external
groups require review without invented backfill. No EF model/schema/public CSV/permission change.
Ten new and five selected prior managed cases pass; strict Windows/complete signed Offline384-AOT
APK builds have zero warnings/errors. Six original native Undo cases pass on isolated fictional
Samsung sources with full typed-row/base equality. Preserve the first fixed-dock ScrollToAsync
observer failure and its accepted first outcome; resume only five unexecuted commands and read
that first result without replay. Consent is fictional, not native dialog/picker acceptance.
Restore the old independent APK/stopped state, then no-reset Main upgrade retains979/all141 public
Home tuples/OS in PID31885; remove three owned hash-matched XMLs. Prior ecde9366 CI/CodeQL succeed
once and its monitor is consumed/deleted. New-head CI and broader relationship/device/security/
stability/accessibility/iOS/provider/Store/release gates remain open. No unchanged successful
financial cohort or full suite is repeated.

Evidence: [original grouped Undo](../../quality/import-group-undo-dependencies.md).

### D-491 - Physical original goal calendars and unavailable sources

Twelve original protected/native queue observations qualify saved Gregorian/Persian/Hijri monthly
rules independently of display calendar, two-goal grouped routes, account/type archive/restore,
missed Once and final disabled state. First24October09:00 and second24November Gregorian /
23November Persian/Hijri stay local09:00 across DST offsets. Full original publication packets
remain equal; opening100/targets150EUR and5units stay, with zero ledger/allocations/holding events.
Preserve two actual coalesced-wait observer cancellations and two accepted states; final caller
follows the replaced handle after yielding and continues only ten missing stages in4354.
Strict complete signed Offline ARM64/192-AOT caller passes. Remove only the new fixture with
pending0 and2 Main/3 scoped owned XMLs; original Main31885/Home979/all141 public tuples/APK/OS
remain. Main code/APK stay D-490, no new managed count or successful unrelated suite/build replay.
Real timed/lock/profile/Doze/reboot, security/stability/accessibility/iOS/provider/Store and release
gates remain open. This is native queue/API evidence, not financial-form or whole-device acceptance.

Evidence: [physical calendar/source queues](../../quality/physical-goal-calendar-sources.md).
