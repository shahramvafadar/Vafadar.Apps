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
| ZCR-SEC-01 | Threat model and encryption decision record + feasibility proof | 1 | P1 | M | In progress | OD-04 |
| ZCR-SEC-02 | Encrypted database (SQLite encryption) behind the existing data layer | 1 | P1 | L | Proposed | SEC-01 |
| ZCR-SEC-03 | Data key in Android Keystore / iOS Keychain / Windows DPAPI | 1 | P1 | M | Proposed | SEC-01 |
| ZCR-SEC-04 | Optional app password, biometrics changes, attempt limiting | 1 | P1 | M | Proposed | SEC-03, OD-04 |
| ZCR-SEC-05 | Crash-safe migration of existing plaintext databases (all profiles) | 1 | P1 | M | Proposed | SEC-02, SEC-03 |
| ZCR-SEC-06 | Recovery: recovery key / backup path, password change without data loss | 1 | P1 | M | Proposed | SEC-04, OD-04 |
| ZCR-SEC-07 | Final OS device/cloud backup rules (Android `dataExtractionRules`, iOS exclusion) | 1 | P1 | S | Final policy open; interim exclusion declined (D-114) | OD-10 |
| ZCR-SEC-08 | Encrypted safety copies before restore | 1 | P2 | S | Proposed | SEC-03 |
| ZCR-SEC-10 | Owner-approved PIN, screenshot choice and ownership clarification (D-63) | Maintenance | P1 | M | Done | Owner request 2026-10-08 |
| ZCR-SEC-09 | Network/SDK review of online builds; privacy texts | 1/3 | P2 | S | Done (current baseline; repeat for future SDKs) | – |
| ZCR-LOC-01 | Release bug list from the owner's phone tests | 2 | P1 | M | Proposed | owner reports |
| ZCR-LOC-02 | Goal contribution reminders (ZEX-S0306) | 2 | P2 | M | Done (local/emulator; physical-device gate remains) | – |
| ZCR-LOC-03 | Optional period review reminder (ZEX-S0610) | 2 | P2 | S | Done (local/emulator; physical-device gate remains) | – |
| ZCR-LOC-04 | Aggregated entries: import overlap handling (ZEX-S0611) | 2 | P2 | M | Done (local/emulator, D-72; phone/iOS acceptance open) | – |
| ZCR-LOC-05 | "Not a tax calculation" help on sale results (ZEX-S0404) | 2 | P3 | S | Proposed | owner translations |
| ZCR-LOC-06 | Recurring payments easier to find (repeat option in the entry form or a clearer entry point) | 2 | P2 | S | Done (local/native, D-111; phone/iOS acceptance open) | – |
| ZCR-LOC-07 | Currencies of the target markets (ISO list, minor digits) | 2/8 | P3 | S | Proposed | target markets |
| ZCR-LOC-08 | More holiday regions with source, validity years and "uncertain" state | 8 | P3 | M | Proposed | target markets |
| ZCR-LOC-09 | Automatic local backup (interval exists, not wired) | 2 | P2 | S | Proposed | SEC-08 |
| ZCR-LOC-13 | Owner-approved plan and debt UX (D-65) | Maintenance | P1 | M | Done (local; AT-72 device gate remains) | owner approval 2026-10-08 |
| ZCR-LOC-15 | Independent saved filters after report navigation (D-109 / AT-114) | Maintenance | P1 | S | Done (local/native, D-109; device/iOS acceptance remains open) | D-69 ready-defect delivery |
| ZCR-LOC-12 | Owner-requested receipt total detection and review (D-64) | Maintenance | P1 | M | Done (local; device/corpus gate remains QA-05) | owner request 2026-10-08 |
| ZCR-LOC-11 | Owner-requested backup, onboarding and About corrections (D-62) | 2 | P2 | S | Done | owner approval 2026-10-08 |
| ZCR-LOC-10 | Copy follow-ups D-53/D-54 and listing texts naming three languages | 2 | P3 | S | Proposed | owner translations |
| ZCR-IMP-01 | CSV presets for other apps and bank exports, preview, duplicates, undo | 2 | P2 | M | Proposed | sample files |
| ZCR-IMP-02 | OFX/QIF/CAMT only on demand with real samples | 8 | P3 | M | Proposed | IMP-01 |
| ZCR-QA-01 | Device acceptance runs (AT-01…61, Q-02) on the owner's phone | 2 | P1 | M | Blocked (owner runs) | – |
| ZCR-QA-02 | Cloud backup with real OAuth clients (AT-59), restore on another device | 2 | P1 | M | Blocked (client ids) | – |
| ZCR-QA-03 | App test project: app lock, profiles, bulk operations, onboarding, widget, theme | 2 | P2 | M | Done (local/emulator, D-73; physical-device/iOS acceptance open) | – |
| ZCR-QA-04 | Asset-account income/expense confirmation test (ZEX-S0408) | 2 | P2 | S | Done (local/emulator, D-74; phone/iOS acceptance open) | QA-03 |
| ZCR-QA-05 | Receipt/PDF reading quality on devices, all six languages | 2 | P2 | M | In progress (D-64 local evidence verified; device/corpus pending) | sample receipts |
| ZCR-QA-06 | Performance: cold start, search, migration with the reference and a 10× data set | 2 | P2 | M | In progress (D-75 measurements, D-76 loading gate, D-92 Home reload optimization, D-95 hidden native account view removal and D-104 repeated-filter row reuse and D-105 complete bulk-selection reload and D-106 exact Settings suggestion history and D-110 unchanged native-source reuse delivered; cold duration/ANR, native publication and device gates open) | – |
| ZCR-QA-07 | Platform parity Android/iOS/Windows (notifications, widget, file picker, lock, share, licences) | 2 | P2 | M | Blocked (iOS: Mac) | – |
| ZCR-A11Y-01 | TalkBack pass on Android | 2 | P1 | M | Proposed | – |
| ZCR-A11Y-02 | Narrator and keyboard pass on Windows | 2 | P2 | S | Proposed | – |
| ZCR-A11Y-03 | Font scaling to 200 %, 360/412 px, both themes, RTL | 2 | P1 | M | In progress (D-77 through D-91 Home/rows/action docks, Settings/reopened languages/drafts, actions/dates/amounts, growing child headers/live Back visible Insights destinations and complete budget figures/signed formatting and visible period choices and complete selectable tags and state-matched detail actions and complete onboarding restore alternatives; D-94 complete Home customization names/targets; D-96 complete account type/status descriptions; D-97 complete debt entry action; D-98 single readable modal headers; D-99 visible complete plan validation; D-100 complete transaction-field feedback; D-101 retained visible existing transfer fees in Simple; D-102 complete settlement fields/action; D-103 complete occurrence actions/field feedback; D-107 complete growing native selected/popup picker captions; D-108 complete report-scope captions/Clear targets and reachable result viewport locally delivered; other controls and platform acceptance open) | – |
| ZCR-A11Y-04 | VoiceOver pass on iOS | 2 | P2 | S | Blocked (iOS) | QA-07 |
| ZCR-ENT-01 | Plan policy and quota model in Core (no UI, no store) | 3 | P1 | M | Implemented (D-117 / approved OD-03; 76 policy cases; no app enforcement) | OD-03 approved |
| ZCR-ENT-02 | Enforcement in services: create, import, restore, template, deep link, widget, OCR | 3 | P1 | L | In progress (D-118..125: actual-file resource/rule writes plus database recovery and retained import/Undo boundaries; selected read-only/future activation and remaining operations/resources/import/restore/native paths open; current enforcement inactive) | ENT-01 |
| ZCR-ENT-03 | Downgrade flow: choose active items, read-only rest | 3 | P1 | M | Proposed | ENT-02 |
| ZCR-ENT-04 | Plan screen, limit messages that keep the form, "continue with Free" | 3/6 | P1 | M | Proposed | ENT-02, translations |
| ZCR-ENT-05 | Existing users and test builds: migration to plans (no limits before approval) | 3 | P1 | S | Proposed | OD-03 |
| ZCR-BIL-01 | Store adapters (Play Billing, StoreKit 2; Windows decision) and catalog | 6 | P1 | L | Proposed | ENT-01, OD-08 |
| ZCR-BIL-02 | Verification service, notifications, idempotency, lifecycle states | 6 | P1 | L | Proposed | BIL-01, ID-01 |
| ZCR-BIL-03 | Secure entitlement cache, offline window, clock changes | 6 | P1 | M | Proposed | BIL-02 |
| ZCR-BIL-04 | Plus Lifetime, Lifetime-owner Pro discount, campaigns, trial | 6 | P1 | M | Proposed | BIL-02, OD-06, OD-07 |
| ZCR-BIL-05 | Sandbox test matrix; production activation (owner only) | 6 | P1 | M | Proposed | BIL-01..04 |
| ZCR-ID-01 | Optional identity, backend foundation, operations | 3 | P1 | L | Proposed | OD-02 |
| ZCR-SYNC-01 | Change log, outbox, versions, tombstones | 4 | P1 | L | Proposed | ID-01 |
| ZCR-SYNC-02 | Conflicts, idempotent posting, restore into a synced profile | 4 | P1 | L | Proposed | SYNC-01 |
| ZCR-SYNC-03 | Sync status UI and device management | 4 | P2 | M | Proposed | SYNC-01 |
| ZCR-SHR-01 | Shared space, invitations, roles, server-side authorization | 5 | P1 | L | Proposed | SYNC-02 |
| ZCR-SHR-02 | Cost sharing between people (F2-SHARE-05), household totals | 5 | P2 | M | Proposed | SHR-01 |
| ZCR-SHR-03 | Audit trail, revocation, ownership transfer, end of subscription | 5 | P1 | M | Proposed | SHR-01, OD-05 |
| ZCR-SHR-04 | End-to-end encryption decision and, if chosen, implementation | 5 | P2 | L | Proposed | OD-09 |
| ZCR-TAX-01 | Tax research: country, year, scope, expert, official examples | 0→7 | P1 | M | Proposed | OD-01 |
| ZCR-TAX-02 | Level 1: data and document export for an accountant (Plus) | 7 | P1 | M | Proposed | TAX-01 |
| ZCR-TAX-03 | Level 2: versioned rule set and report for the first country/year | 7 | P2 | L | Proposed | TAX-01, expert |
| ZCR-AI-01 | AI credit ledger, provider interface, consent, minimal data | 8 | P3 | L | Proposed | OD-13 |
| ZCR-BANK-01 | Read-only bank connection evaluation and decision | 8 | P3 | M | Proposed | OD-14 |
| ZCR-FX-01 | Online rates/prices: source, rights, caching, manual fallback | 8 | P3 | M | Proposed | OD-14 |
| ZCR-LANG-01 | At least 20 additional languages (last feature wave) | 9 | P2 | L | Proposed | stable texts |
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
of biometric enrolment changes, attempt limiting with growing delay, lock on return (existing 30 s). The current "no
device lock → unlock" path is re-decided. Texts: new strings (owner translations). Done: tests for all transitions;
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
Scope: one help topic on holding sale results stating it is not a tax calculation. Texts: owner. Done: help shown.

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
Scope: wire the existing interval to an app-start/resume check that writes an encrypted local backup when due
(password stored in the keystore only if the user agrees), with retention. Done: tests and setting text (owner).

### ZCR-LOC-11 – Backup, first run and About (D-62)

Approved by the owner on 2026-10-08 before further commercial development. Scope: optional cloud backup password,
theme/experience in onboarding, restore before creating the first account, and a compact notices link on About.
No password persistence, new schema or SDK; no commercial enforcement. Local verification covers protected and
unprotected restores, preserved profile settings and no duplicate account, all tests, both builds, and Windows
layouts in en/fa/de, light/dark, 360/412 px and wide. Provider sign-in with real OAuth clients and physical-device
gates remain open. Evidence and policy: D-62, spec §31, acceptance plan and architecture/data-and-backup.

### ZCR-LOC-10 – Copy follow-ups
Scope: D-53 "Sign-in complete", D-54 "as an expense of {2}", listing texts that name three languages. Texts: owner.

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
Research starts in wave 0 (OD-01): country, tax year, user type, expert, official examples. Level 1 (accountant
export) belongs to Plus; levels 2–3 are an add-on per country/year. A first valid Tax scope is a near milestone
before major user growth; if it cannot be in the first release, it gets its own milestone with owner, dependencies
and readiness criteria.

### ZCR-AI-01, ZCR-BANK-01, ZCR-FX-01
Each only after its own scope and cost approval (OD-13, OD-14); otherwise a later milestone, never an artificial
release blocker.

### ZCR-LANG-01
At least 20 languages in addition to the six existing ones, as the last feature wave, after the release texts are
stable: owner-provided translations with cultural review, never machine-filled files; each language with its
calendar defaults, number formats, receipts terms and display checks as for es/fr/it.

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
