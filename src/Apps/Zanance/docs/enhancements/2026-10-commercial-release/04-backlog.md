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
| ZCR-GOV-02 | Refresh spec §31, AGENTS.md §12 and outdated statements | 0/2 | P2 | S | Proposed | GOV-01 |
| ZCR-SEC-01 | Threat model and encryption decision record + feasibility proof | 1 | P1 | M | In progress | OD-04 |
| ZCR-SEC-02 | Encrypted database (SQLite encryption) behind the existing data layer | 1 | P1 | L | Proposed | SEC-01 |
| ZCR-SEC-03 | Data key in Android Keystore / iOS Keychain / Windows DPAPI | 1 | P1 | M | Proposed | SEC-01 |
| ZCR-SEC-04 | Optional app password, biometrics changes, attempt limiting | 1 | P1 | M | Proposed | SEC-03, OD-04 |
| ZCR-SEC-05 | Crash-safe migration of existing plaintext databases (all profiles) | 1 | P1 | M | Proposed | SEC-02, SEC-03 |
| ZCR-SEC-06 | Recovery: recovery key / backup path, password change without data loss | 1 | P1 | M | Proposed | SEC-04, OD-04 |
| ZCR-SEC-07 | OS device/cloud backup rules (Android `dataExtractionRules`, iOS exclusion) | 1 | P1 | S | Proposed | OD-10 |
| ZCR-SEC-08 | Encrypted safety copies before restore | 1 | P2 | S | Proposed | SEC-03 |
| ZCR-SEC-10 | Owner-approved PIN, screenshot choice and ownership clarification (D-63) | Maintenance | P1 | M | Done | Owner request 2026-10-08 |
| ZCR-SEC-09 | Network/SDK review of online builds; privacy texts | 1/3 | P2 | S | Done (current baseline; repeat for future SDKs) | – |
| ZCR-LOC-01 | Release bug list from the owner's phone tests | 2 | P1 | M | Proposed | owner reports |
| ZCR-LOC-02 | Goal contribution reminders (ZEX-S0306) | 2 | P2 | M | Proposed | – |
| ZCR-LOC-03 | Optional period review reminder (ZEX-S0610) | 2 | P2 | S | Proposed | – |
| ZCR-LOC-04 | Aggregated entries: import overlap handling (ZEX-S0611) | 2 | P2 | M | Proposed | – |
| ZCR-LOC-05 | "Not a tax calculation" help on sale results (ZEX-S0404) | 2 | P3 | S | Proposed | owner translations |
| ZCR-LOC-06 | Recurring payments easier to find (repeat option in the entry form or a clearer entry point) | 2 | P2 | S | Proposed | OD-12 |
| ZCR-LOC-07 | Currencies of the target markets (ISO list, minor digits) | 2/8 | P3 | S | Proposed | target markets |
| ZCR-LOC-08 | More holiday regions with source, validity years and "uncertain" state | 8 | P3 | M | Proposed | target markets |
| ZCR-LOC-09 | Automatic local backup (interval exists, not wired) | 2 | P2 | S | Proposed | SEC-08 |
| ZCR-LOC-13 | Owner-approved plan and debt UX (D-65) | Maintenance | P1 | M | Done (local; AT-72 device gate remains) | owner approval 2026-10-08 |
| ZCR-LOC-12 | Owner-requested receipt total detection and review (D-64) | Maintenance | P1 | M | Done (local; device/corpus gate remains QA-05) | owner request 2026-10-08 |
| ZCR-LOC-11 | Owner-requested backup, onboarding and About corrections (D-62) | 2 | P2 | S | Done | owner approval 2026-10-08 |
| ZCR-LOC-10 | Copy follow-ups D-53/D-54 and listing texts naming three languages | 2 | P3 | S | Proposed | owner translations |
| ZCR-IMP-01 | CSV presets for other apps and bank exports, preview, duplicates, undo | 2 | P2 | M | Proposed | sample files |
| ZCR-IMP-02 | OFX/QIF/CAMT only on demand with real samples | 8 | P3 | M | Proposed | IMP-01 |
| ZCR-QA-01 | Device acceptance runs (AT-01…61, Q-02) on the owner's phone | 2 | P1 | M | Blocked (owner runs) | – |
| ZCR-QA-02 | Cloud backup with real OAuth clients (AT-59), restore on another device | 2 | P1 | M | Blocked (client ids) | – |
| ZCR-QA-03 | App test project: app lock, profiles, bulk operations, onboarding, widget, theme | 2 | P2 | M | Proposed | – |
| ZCR-QA-04 | Asset-account income/expense confirmation test (ZEX-S0408) | 2 | P2 | S | Proposed | QA-03 |
| ZCR-QA-05 | Receipt/PDF reading quality on devices, all six languages | 2 | P2 | M | In progress (D-64 local evidence verified; device/corpus pending) | sample receipts |
| ZCR-QA-06 | Performance: cold start, search, migration with the reference and a 10× data set | 2 | P2 | M | Proposed | – |
| ZCR-QA-07 | Platform parity Android/iOS/Windows (notifications, widget, file picker, lock, share, licences) | 2 | P2 | M | Blocked (iOS: Mac) | – |
| ZCR-A11Y-01 | TalkBack pass on Android | 2 | P1 | M | Proposed | – |
| ZCR-A11Y-02 | Narrator and keyboard pass on Windows | 2 | P2 | S | Proposed | – |
| ZCR-A11Y-03 | Font scaling to 200 %, 360/412 px, both themes, RTL | 2 | P1 | M | Proposed | – |
| ZCR-A11Y-04 | VoiceOver pass on iOS | 2 | P2 | S | Blocked (iOS) | QA-07 |
| ZCR-ENT-01 | Plan policy and quota model in Core (no UI, no store) | 3 | P1 | M | Proposed | OD-03 |
| ZCR-ENT-02 | Enforcement in services: create, import, restore, template, deep link, widget, OCR | 3 | P1 | L | Proposed | ENT-01 |
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

### ZCR-GOV-02 – Outdated statements
Value: documents match reality. Scope: spec §31 (date, test count, ZEX, device runs), AGENTS.md §12, PR-10, §2, §3,
privacy matrices (camera), ZEX README status lines. Out: requirement changes. Texts: none. Done: every statement in
[01-current-state.md §5](01-current-state.md#5-outdated-statements-found-to-correct-in-the-touched-documents) fixed
or confirmed by the owner (OD-11).

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

### ZCR-LOC-02 – Goal contribution reminders (from ZEX-S0306)
Scope: reminders on contribution dates using the reminder planner; generic lock-screen text. Texts: new strings
(owner). Done: planner tests, device notification check.

### ZCR-LOC-03 – Period review reminder (from ZEX-S0610)
Scope: optional reminder after a period ends. Done: planner tests; off by default.

### ZCR-LOC-04 – Aggregated entries and import (from ZEX-S0611)
Scope: when an import overlaps an aggregated entry, offer linking details to reduce the aggregate (same account,
kind, category, period) instead of only warning. Done: data tests for overlap, partial overlap, undo.

### ZCR-LOC-05 – Sale results help (from ZEX-S0404)
Scope: one help topic on holding sale results stating it is not a tax calculation. Texts: owner. Done: help shown.

### ZCR-LOC-06 – Recurring payments easier to find
Value: the owner could not find recurring payments. Scope per OD-12 (proposal: a "Repeat" switch in the new entry
form that opens the plan editor pre-filled, plus "Plans" mentioned in the + menu). Texts: owner. Done: snapshots in
six languages; no change to plan logic.

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
