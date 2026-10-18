# 04 – Canonical backlog (commercial release)

**This file is the only place where the state of remaining Zanance work is tracked from 2026-10-07 on.**
`05-phase-2-backlog.md` keeps the finished Phase 2A history and links here; the ZEX backlog keeps the finished ZEX
stories, and its open stories continue here under the ids given below. Waves and sections:
[05-delivery-plan.md](05-delivery-plan.md); owner decisions: [06-open-decisions.md](06-open-decisions.md).

States: Proposed · Ready (decisions made, may start after owner approval of the section) · In progress · Done
(implemented, tested, documented, pushed) · Blocked (by an owner action) · Deferred by owner. Priority: P1 (blocks
the release), P2 (in the release), P3 (later milestone). Size: S/M/L.

## Overview

| Id | Title | Wave | Prio | Size | State | Depends on |
|---|---|---|---|---|---|---|
| ZCR-GOV-01 | Audit, plan matrix, architecture, backlog and delivery plan | 0 | P1 | M | Done | – |
| ZCR-GOV-02 | Refresh spec §31, AGENTS.md §12 and outdated statements | 0/2 | P2 | S | Done (D-88; external confirmation/acceptance still open) | GOV-01 |
| ZCR-SEC-01 | Threat model and encryption decision record + feasibility proof | 1 | P1 | M | In progress (D-264 Android/Windows official Community integration locally verified; iOS/external gates remain) | OD-04 approved |
| ZCR-SEC-02 | Encrypted database (SQLite encryption) behind the existing data layer | 1 | P1 | L | In progress (D-264 Android/Windows main encryption locally verified; iOS adoption and device gates remain) | SEC-01 |
| ZCR-SEC-03 | Data key in Android Keystore / iOS Keychain / Windows DPAPI | 1 | P1 | M | In progress (D-264 Android Keystore/Windows DPAPI per-generation keys locally verified; iOS/device gates remain) | SEC-01 |
| ZCR-SEC-04 | Optional app password, biometrics changes, attempt limiting | 1 | P1 | M | In progress (D-266 success-only device unlock; biometric/platform acceptance remains) | SEC-03, OD-04 |
| ZCR-SEC-05 | Crash-safe migration of existing plaintext databases (all profiles) | 1 | P1 | M | In progress (D-263 Android all-profile migration/lifecycle and native interruption cases delivered; broader fault/platform acceptance remains) | SEC-02, SEC-03 |
| ZCR-SEC-06 | Recovery: recovery key / backup path, password change without data loss | 1 | P1 | M | In progress (D-264 Android/Windows optional recovery and explicit portable recovery locally verified; iOS/device acceptance remain) | SEC-04, OD-04 approved |
| ZCR-SEC-07 | Final OS device/cloud backup rules (Android `dataExtractionRules`, iOS exclusion) | 1 | P1 | S | In progress (D-263 final coupled Android policy delivered; iOS exclusion/OEM acceptance remain) | OD-10 approved |
| ZCR-SEC-08 | Encrypted safety copies before restore | 1 | P2 | S | In progress (D-264 local Android/Windows; D-312 actual Samsung safety recovery and profile isolation verified; broader device/iOS gates remain) | SEC-03 |
| ZCR-SEC-10 | Owner-approved PIN, screenshot choice and ownership clarification (D-63) | Maintenance | P1 | M | Done | Owner request 2026-10-08 |
| ZCR-SEC-09 | Network/SDK review of online builds; privacy texts | 1/3 | P2 | S | Done (current baseline; repeat for future SDKs) | – |
| ZCR-LOC-01 | Release bug list from the owner's phone tests | 2 | P1 | M | Proposed | owner reports |
| ZCR-LOC-02 | Goal contribution reminders (ZEX-S0306) | 2 | P2 | M | Done (local/emulator; physical-device gate remains) | – |
| ZCR-LOC-03 | Optional period review reminder (ZEX-S0610) | 2 | P2 | S | Done (local/emulator; physical-device gate remains) | – |
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
| ZCR-IMP-01 | CSV presets for other apps and bank exports, preview, duplicates, undo | 2 | P2 | M | In progress: D-297 documented StarMoney ordinary-cashflow preset; other bank/relationship variants require original fixtures | original export specifications/samples |
| ZCR-IMP-02 | OFX/QIF/CAMT only on demand with real samples | 8 | P3 | M | Proposed | IMP-01 |
| ZCR-QA-01 | Device acceptance runs (AT-01…61, Q-02) on the owner's phone | 2 | P1 | M | In progress (D-312 recovery/profile; D-314/315 protected workload and main upgrade; D-316 planned-work lifecycle; D-317 native CSV fixed-ID/duplicate/cold Undo; D-318 first-plan native publication fixed; D-319 debt/lent and six-row cold-upgrade lifecycle; D-323 report/saved-filter/cold/PDF chooser; D-326 transfer summary correction; D-327 automatic-copy lifecycle/readiness; D-328 accountant ZIP receiving/content; D-329 original en/fa/de PDF contents and continued-table fix; D-330 own-format producer/consumer/refund/transfer/cold Undo; broader phone acceptance remains) | – |
| ZCR-QA-02 | Cloud backup with real OAuth clients (AT-59), restore on another device | 2 | P1 | M | Blocked (client ids) | – |
| ZCR-QA-03 | App test project: app lock, profiles, bulk operations, onboarding, widget, theme | 2 | P2 | M | Done (local/emulator, D-73; physical-device/iOS acceptance open) | – |
| ZCR-QA-04 | Asset-account income/expense confirmation test (ZEX-S0408) | 2 | P2 | S | Done (local/emulator, D-74; phone/iOS acceptance open) | QA-03 |
| ZCR-QA-05 | Receipt/PDF reading quality on devices, all six languages | 2 | P2 | M | In progress (D-324 six source-language digital PDFs; D-325 physical Latin image OCR/EXIF/refusal and French accent fix; real corpus/camera/scanned/iOS remain) | sample receipts |
| ZCR-QA-06 | Performance: cold start, search, migration with the reference and a 10× data set | 2 | P2 | M | In progress (D-322 retained physical workloads; D-350 rejects unproven date decoder; D-351 indexes shared reimbursement aggregation with original financial semantics; actual Main/startup ANRs, two-second objective, cold/low-memory and broader device/iOS gates remain) | – |
| ZCR-QA-07 | Android/iOS parity (notifications, widget, file picker, lock, share, licences); existing Windows development checks | 2 | P2 | M | Mac readiness owner-reported; verification and iOS runs pending in that environment. Windows commercial delivery is outside current scope; D-344 native Canvas root-change AV remains a separate development limitation | platform access/signing |
| ZCR-A11Y-01 | TalkBack pass on Android | 2 | P1 | M | In progress (D-174/175 local names; D-348 service-preserving observer captures audio/focus; D-349 rejects injected navigation as gesture acceptance; correct spoken navigation and remaining platform/device acceptance open) | – |
| ZCR-A11Y-02 | Narrator and keyboard pass on Windows | 2 | P2 | S | Proposed | – |
| ZCR-A11Y-03 | Font scaling to 200 %, 360/412 px, both themes, RTL | 2 | P1 | M | In progress (D-77 through D-91 Home/rows/action docks, Settings/reopened languages/drafts, actions/dates/amounts, growing child headers/live Back visible Insights destinations and complete budget figures/signed formatting and visible period choices and complete selectable tags and state-matched detail actions and complete onboarding restore alternatives; D-94 complete Home customization names/targets; D-96 complete account type/status descriptions; D-97 complete debt entry action; D-98 single readable modal headers; D-99 visible complete plan validation; D-100 complete transaction-field feedback; D-101 retained visible existing transfer fees in Simple; D-102 complete settlement fields/action; D-103 complete occurrence actions/field feedback; D-107 complete growing native selected/popup picker captions; D-108 complete report-scope captions/Clear targets and reachable result viewport locally delivered; D-302 complete searchable currency controls and constrained transaction/goal/rate fields; D-308 complete native saved-filter names/actions; D-311 full monetary packets in report/forecast tables; D-335 complete More titles/explanations in offline/sync editions; D-339 complete category/composition report names and amount viewports; D-340 complete default RowTitle and Flex bill-group captions; D-342 complete optional Home category legends and total; D-343 complete shared plan-row values; D-344 complete ledger/account monetary rows and group headers; D-345 real emulator OS200%/412dp on six monetary surfaces; D-346 real OS report/forecast/legend geometry and Flex captions, with actual Mono crash/ANR retained; D-347 guarded ordinary foreground Home-category/plan evidence at OS200%/412dp; other controls and platform acceptance open) | – |
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
| ZCR-TAX-02 | Level 1: Germany data/document export for an accountant (Plus) | 7 | P1 | M | Locally delivered D-296; D-328 physical default/no-document and selected-account ZIP contents qualified; wider device/iOS/expert acceptance separate | TAX-01 |
| ZCR-TAX-03 | Level 2: versioned rule set and report for the first country/year | 7 | P2 | L | Proposed | TAX-01, expert |
| ZCR-AI-01 | Local AI credit ledger, provider interface, consent, minimal data | 8 | P3 | L | In progress: D-298 local aggregate/native-consent/private-client/service-credit prerequisites qualified; live provider/pricing/credit sales remain gated; see15-ai-local-delivery.md | OD-13 |
| ZCR-BANK-01 | German-first bank/file/provider evaluation, then USA and other targets | 8 | P3 | M | In progress: D-307 first discovery pass across fourteen targets and provider comparison delivered; exact consumer specimens and later live-provider acceptance remain | OD-14, IMP-01 |
| ZCR-FX-01 | Rates/prices provider boundary, caching, provenance, manual fallback | 8 | P3 | M | In progress: D-299 app/data/service and protected personal/shared source prerequisites; current native/package evidence in16-market-reference-delivery.md; actual source/licence/cost remain gated | OD-14 |
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
editing on Android/physical/iOS and other owner/platform/release gates remain open.
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
