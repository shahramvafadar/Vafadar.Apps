# 05 – Delivery plan

**Way of working:** one section at a time. The owner approves a section; it is built, tested, documented, committed
and pushed; then exactly one next section is proposed. The owner authorized continuous delivery of ready planned
work on 2026-10-08 (D-69), superseding the per-section wait for that scope. Unresolved owner decisions, licence
costs, provider identities and publishing still require explicit decisions. Item states live only in
[04-backlog.md](04-backlog.md).

The owner approved D-62 maintenance (ZCR-LOC-11) on 2026-10-08 before continuing the commercial waves.
That maintenance approval covered only backup, first run and About. The later D-69 continuation authorizes ready
planned sections; unresolved choices still require owner decisions.

## Waves (dependency order)

| Wave | Content | Exit criterion |
|---|---|---|
| 0 | Audit, plan matrix, architecture, backlog, delivery plan (ZCR-GOV-01); Tax and service-cost research start (TAX-01, OD-01/02) | Documents pushed; owner picks the first section |
| 1 | Local security: SEC-01 … SEC-09, highest risk first (database encryption, keys, app password, migration, recovery, OS backup rules) | Encrypted database on new and upgraded installs, recovery path tested, no plaintext safety copies |
| 2 | Release-critical local gaps: owner's bug list, ZEX leftovers (LOC-02…05), recurring entry point, import presets, device runs, accessibility, app tests, performance | All P1 of wave 2 Done or explicitly deferred by the owner |
| 3 | Plan policy and quotas (ENT-01…05), optional identity and backend foundation (ID-01); no real sale | Policy tests for every combination; enforcement switched off in release until wave 6 |
| 4 | Personal sync (SYNC-01…03) | Two-device conflict and idempotency tests pass |
| 5 | Shared space, roles, member lifecycle (SHR-01…04) | Authorization tests per request/resource pass |
| 6 | Billing in sandbox, restore/refund/upgrade, trial, offers, Lifetime, paywall, end of plan (BIL-01…05) | Sandbox matrix passes; production only by the owner |
| 7 | First approved Tax scope (TAX-02/03) or its own near milestone | Official examples pass; expert review recorded |
| 8 | Other approved release features; online rates/bank/AI only with approved scope and cost | Per item |
| 9 | **Last feature wave: at least 20 additional languages** | Display, calendars, numbers and receipts checked per language |
| 10 | Final QA, bug fixes, store metadata, staged rollout; no new features | Release checklist complete |

Deviations: Tax research and service-cost estimates start in wave 0 because they can change the plan; the
accessibility pass of the Phase 2 backlog is placed in wave 2 (release-critical), not after all features, because the
owner asked for it before release and it does not depend on the commercial waves. Existing-language copy fixes continue
in every wave.

## Release gates

1. Security gate (after wave 1): database encryption, recovery and migration tested; privacy matrix updated.
2. Local release gate (after wave 2): device acceptance runs, accessibility checklist, performance numbers.
3. Commercial gate (after wave 6): sandbox matrix, store review of texts, cost model, owner approval of prices.
4. Store gate (wave 10): release checklist `08-release-checklist.md`, privacy policy, Data safety, Financial features.

## Mandatory acceptance tests across the plan

* Free works without an online account, with the user's real data, without any service.
* Every combination of plan, purchase kind, Simple/Advanced, online/offline and personal/shared role is covered by
  policy tests.
* Quotas cannot be bypassed via UI, template, import, restore or concurrent actions; recovery and export never need a
  purchase.
* End of Plus/Pro, Lifetime with and after Pro, refund, revoked, pending, grace and duplicate notifications give the
  right result; turning off auto-renew does not end valid access early.
* Store/server/network errors never lose data or a form; changing the phone clock creates no fake subscription.
* Two devices settling one occurrence or recording one purchase never double-count.
* A Viewer or removed member cannot write; no report, file, search or API reveals an unshared account.
* An interrupted encryption migration, a wrong password, a biometric change and a restore on a new device never
  corrupt data.
* Plan expiry and theme/language changes while a form is open keep the input.
* "?" help, examples, short labels and screen reader hints stay connected; 200 % font, 360/412 px, both themes, RTL.
* Tax is tested with reference examples of the country/year; AI running out of credit or answering wrongly never
  changes data or amounts.

## Section template

Every section is written with: id and title · value and why now · prerequisites and decisions · current state and
source evidence · scope / out of scope · files and possible migrations · UI changes and translation keys · acceptance
tests and sample data · security/privacy/cost impact · relative size and uncertainty · rollback without data loss ·
Definition of Done and testable output · proposed next section.

## Proposed first section: ZCR-SEC-01 – Threat model, encryption decision and feasibility proof

* **Value and why now:** the local database (including attachments, WAL/SHM) is plaintext today, and Android's OS
  backup may copy it. Every later step of wave 1 depends on choosing a library and a key design that works on Android,
  iOS and Windows with .NET 10 MAUI.
* **Prerequisites:** none for the research; OD-04 (recovery) can be answered while it runs. Any paid licence needs the
  owner's approval before adoption.
* **Current state:** [01-current-state.md §2](01-current-state.md#2-security-and-data-protection).
* **Scope:** threat model; inventory of files with financial data; comparison of SQLCipher community build
  (`SQLitePCLRaw.bundle_e_sqlcipher`), commercial SQLCipher builds and the SQLite Encryption Extension (licence, cost,
  maintenance, Android/iOS/Windows, trimming/AOT, EF Core compatibility, performance); key design (random data key,
  keystore/keychain/DPAPI wrapping, optional password KDF, per profile); a feasibility test that creates an encrypted
  database, re-opens it, rejects a wrong key and checks WAL/SHM are not plaintext, run on Windows and in the Android
  emulator; ADR `docs/adr/0010-database-encryption.md`.
* **Out of scope:** any change to the app's database, migration of user data, UI, texts.
* **Files:** the ADR, the decision log entry, the backlog state; the feasibility test lives in a separate test project
  or is removed after the proof (decided in the section; nothing reaches the app).
* **UI/texts:** none.
* **Tests:** the feasibility test on Windows (dotnet test) and on the emulator (a small console/instrumented run);
  results written into the ADR with versions.
* **Security/privacy/cost:** no user data touched; possible licence cost flagged for approval.
* **Size:** M; uncertainty: Android native library packaging with .NET 10 and the EF Core provider combination.
* **Rollback:** documents only plus an unused test; nothing to roll back in user data.
* **Done:** ADR with chosen library, key flow, migration outline and measured feasibility; owner approves the ADR.
* **Next section after it:** ZCR-SEC-07 (OS backup rules, small and immediate risk reduction) or ZCR-SEC-02/03
  (encrypted database with keystore keys), depending on the ADR.

## Handoff

* 2026-10-07 – wave 0 done (ZCR-GOV-01): documents in this folder, D-61, MON-02/03 superseded. Waiting for the
  owner's approval of ZCR-SEC-01 and answers to [06-open-decisions.md](06-open-decisions.md) (OD-01, OD-04, OD-10 first).

Owner-approved maintenance on 2026-10-08 (D-63, ZCR-SEC-10) precedes commercial work: independent four-digit app PIN,
attempt limiting/recovery, screenshot preference and ownership clarification. The database encryption/key recovery
sequence is unchanged; approval is still required before beginning its next section.

Owner-approved receipt maintenance on 2026-10-08 (D-64, ZCR-LOC-12) is locally verified: total evidence, OCR geometry,
independent bounded image preparation and review in both entry paths. Device/corpus acceptance remains ZCR-QA-05.
This does not begin a commercial section; the next proposed section remains ZCR-SEC-01 and awaits owner approval.

The owner separately approved D-65 plan/debt UX maintenance (ZCR-LOC-13) on 2026-10-08. This does not authorize
commercial enforcement, another wave or unrelated entry/import work. AT-72 device acceptance remains separate.

Owner-approved D-67 maintenance (ZCR-LOC-14) on 2026-10-08 addresses cloud restore discovery and independent regional
formats before commercial work. AT-73/74 local verification is separate from real-provider and device acceptance.
The next proposed section remains ZCR-SEC-01; no commercial limits were enabled.


SEC-01 research/proof approved on 2026-10-08 (D-68). Technical delivery is complete: four Windows tests and two
Android API 36 x86_64 process runs pass, including recovery of the same Keystore-wrapped fictitious profile. The
existing installed emulator was used through an isolated AVD; no download was needed. ADR 0010 is ready for owner
review and remains proposed. No production connection or real-data migration is authorized. Exactly one next
section is proposed: SEC-07, subject to OD-10; do not start it without owner approval.

D-70 / LOC-02 proceeds under the D-69 continuation instruction while ADR/library and OD-10 decisions remain pending.
Goal reminders reuse the stored opt-in, recurrence/progress engines and platform notification service; no commercial
limits, production database encryption or OS-backup policy are introduced. The next independent ready local section
is LOC-03, the optional period review reminder.

D-71 / LOC-03 completes the optional period review reminder under the D-69 continuation instruction. Local tests,
translated running Settings/help and isolated native notification checks are distinct from physical-device acceptance.
The next independent ready section is LOC-04, aggregated-entry/import linking; pending ADR/library and OD-10 choices
continue to gate database encryption and OS-backup policy.

D-72 / LOC-04 completes explicit import overlap linking and persistent Undo under D-69. The 412 EUR sample and
native Release Undo are verified separately from phone/iOS acceptance. ADR/library, OD-10, OD-12, target-market,
provider and commercial decisions remain owner gates. The next independent ready section is QA-03: app-level tests
for existing lock/profile/bulk/onboarding/widget/theme behaviour, without enabling commercial limits.

D-73 / QA-03 completes the application flow project under D-69: actual linked sources, explicit native ports and
68 new AT-80 cases, including validation-safe bulk snapshots and secure startup/command time boundaries. Native
runtime evidence and owner phone/iOS/provider acceptance remain distinct. The next independent ready section is
QA-04, asset-account income/expense confirmation tests. ADR/library, OD-10, OD-12 and commercial decisions remain
owner gates; no production encryption, OS-backup policy or test-build limits are introduced.

D-74 / QA-04 completes the application/native coverage of existing valued-asset editor consent under D-69. AT-81
adds 37 cases; main suite 1,300. No data model, import restriction or commercial behavior change. The next independent
ready section is QA-06, startup/search/migration measurements with the reference and 10x data set. Product/licence,
provider/OS-backup decisions and physical-device/iOS release gates remain open.

D-75 / QA-06 proceeds under D-69: independent reference/tenfold fixture/migration/operation measurements expose
native input/loading risks; worker materialization and account-index reuse preserve financial semantics. Main suite
1,323; no new SDK, permission, schema, commercial restriction or production encryption. Report negative native
results and measurement limits in quality/performance-q02.md before claiming section or device acceptance.
The signed candidate installs and saves with full original-row preservation; controlled native searches pass both
shapes. Early-input result-row observation, the baseline ANR follow-up and loaded Android durations remain QA-06
work; Q-02 and physical/iOS acceptance are not closed by this verified optimization step.

D-76 follows the QA-06 early-input observation under D-69. Actual snapshot publication precedes input/native-row
exposure, simultaneous loads share one read, and errors retain coverage with retry. AT-83 adds 12 cases (main 1,335).
Record the new rendered and Release emulator results in Q-02 without erasing D-75 failures or inferring a native
rendering root cause. Loaded Android Home, ANR follow-up and physical/iOS gates remain open. No commercial limits,
production encryption, OS-backup policy, provider identity or other owner-gated choice is introduced.

D-77 starts A11Y-03 under D-69. Deliver verified Home/plan large-text layout repairs and a bounded Debug review path
without changing system settings or production security. Keep Windows stress, native Android conversion and real
OS/phone/iOS acceptance distinct. Continue the remaining A11Y-03 findings in quality/font-scaling-a11y03.md; do not
mark the section complete from the first slice or CI.

D-78 delivers the next A11Y-03 slice under D-69: seven persistent action docks, financial identity/amount separation
and Undo presentation. AT-85 and the same quality report record verified scope and the complete signed APK. Continue
ready fixed-control/currency findings, then other layouts; keep real OS/screen-reader/device acceptance and the early
Settings-selection finding explicit. No commercial restrictions or unresolved owner decisions are introduced.

D-79 follows the actual Settings selection/caption findings under D-69. Complete publication, covered retry and
translated choices preserve draft/selection values. AT-86 and the large-text report record separate unit, rendered,
native and APK evidence. Continue ready fixed-control/currency findings; unresolved owner decisions and physical/
OS/screen-reader/iOS acceptance remain open. No commercial restriction or production encryption is introduced.

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
