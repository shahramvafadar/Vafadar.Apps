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

D-88 / GOV-02 completes the original documentation-alignment list under D-69. Brand, profiles, sign-in source,
optional backup protection/portable preferences and the D-61 plan model are current; original ZEX/ZCR review-only
statements are explicitly historical. No app code or gate is changed. OD-11 external setup/acceptance and the
unanswered OD-10/OD-12 product choices remain open; see the audited corrections in 01-current-state.md Section 5.

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

D-85 continues A11Y-03 with all four visible Windows Insights destinations in a growing navigation row above
the retained body. Narrow windows reflow into two columns; wide readable content shows four. AT-92 invokes the
actual native routes and checks complete caption/selection/target geometry and unchanged stored data. Final
evidence/APK and independent platform/other-control limits are in quality/insights-navigation.md.

D-86 continues A11Y-03 with complete wrapped budget identities and separate full-width spending/limit and
envelope readouts, retaining compact native typography. A separately reproduced MoneyText signed-boundary crash
is repaired by taking the magnitude after decimal conversion; input range and financial calculations stay unchanged.
AT-93 adds 11 independent Core cases and actual native scroll/geometry/data-equality checks. Final evidence and
remaining physical OS/screen-reader/device/iOS gates are in quality/budget-readouts.md. No schema or new strings.

D-87 continues A11Y-03 by keeping all three budget period decisions visible in the existing wrapping choice group.
An actual pre-change native check fails for the hidden third option. AT-94 measures full native captions/targets,
invokes the existing three choices, restores selection and compares complete stored data without Save. No shared
control, financial calculation, schema or new string change. Final evidence and independent OS/screen-reader/
physical-device/iOS gates are in quality/budget-periods.md.

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

D-104 delivers the ready QA-06 repeated-transaction-filter optimization, preserving complete results and fresh
data/display semantics. Evidence and final checks: [transaction-row-reuse.md](../../quality/transaction-row-reuse.md).
Continue ready work under D-69; native publication, cold/device performance and unresolved owner gates remain open.

D-105 delivers ready QA-06 complete bulk-selection reload, retaining current financial semantics. Actual evidence and final checks: [bulk-selection-reload.md](../../quality/bulk-selection-reload.md). Continue ready work under D-69; native publication, cold/device/ANR and unresolved owner gates remain open.

D-106 delivers ready QA-06 exact Settings suggestion history with unchanged financial and publication rules. Actual evidence/final checks: [settings-suggestion-history.md](../../quality/settings-suggestion-history.md). Continue ready work under D-69; cold/ANR/native-publication/device/platform and unresolved owner decisions remain open.

D-107 / AT-112 continues A11Y-03 with complete growing Windows selected/popup picker captions, inherited native
typography and 44-unit popup targets. Native reviews retain choices/drafts/full stored values without Save;
main suite remains 1,512 passed (App.Tests 276). Final native matrix, strict Windows/complete Android Debug+Release, exact original-data readbacks and signed APK pass: [../../quality/native-picker-captions.md](../../quality/native-picker-captions.md).
Other controls and actual OS/readers/device/iOS/release acceptance remain open. Continue ready work under D-69.

D-108 / AT-113 addresses actual clipped report-scope captions/off-page Clear actions and a zero-height transaction
result at large text. Final 21 Windows native contexts, main 1,512 tests (App.Tests 276), strict builds, actual
normal Release report/navigation/Clear, exact original-data readbacks and complete signed D-108 APK pass: [transaction-scope-readable.md](../../quality/transaction-scope-readable.md). A11Y-03 and independent
platform/OS/readers/device/owner/release gates remain open; continue ready work under D-69.

D-109 / AT-114 fixes observed inheritance of unrelated report scope when applying a named transaction filter.
Preserve the saved combination and complete financial data. Final six native contexts/18 independent report
restrictions, main 1,512 tests, strict builds, normal Release, exact original-data readbacks and signed APK pass:
[quality/saved-filter-report-scope.md](../../quality/saved-filter-report-scope.md). Provider/device/iOS and owner gates remain open.

D-110 delivers ready QA-06 unchanged native-source reuse under D-69. Preserve every result/group and require fresh
publication after data/display changes. Six native Windows contexts/186 own renders, main 1,521 tests, strict
builds, normal Release, exact original-data readbacks and signed APK pass:
[unchanged-transaction-source.md](../../quality/unchanged-transaction-source.md). Continue approved ready work;
OD-12 was separately approved by the owner on 2026-10-10 for a prefilled Plan draft without transaction posting.

D-111 implements explicitly approved OD-12 / LOC-06 after D-110 delivery. Repeat opens a detached editable monthly
Plan without saving a transaction, with complete original draft retention and no change to plan rules. Fourteen
cases, final financial/presentation matrices, strict builds, normal signed Release Plan Save/cleanup, exact
original-data readbacks and complete signed APK pass:
[entry-repeat-draft.md](../../quality/entry-repeat-draft.md). Continue ready work under D-69 after verified delivery.

D-112 closes a bounded QA-06 enumeration experiment without adopting an unreliable candidate. Complete values
and stored rows match; paired timing/allocation do not establish a dependable gain. Retain D-111's verified app
baseline and avoid repeated optimization trials without a new cause. SEC-07 still requires the explicit OD-10
OS-backup policy decision; the remaining library/recovery/provider/market/commercial/platform gates remain open.
Evidence: [entry-read-enumeration-comparison.md](../../quality/entry-read-enumeration-comparison.md).

D-113 / AT-117 continues ready A11Y-03 work with complete growing transaction category choices. Actual native
glyph/viewport/command checks pass in 33 contexts/99 selections, with full draft/store/developer-byte restoration.
Main 1,535/App.Tests 299, strict Windows/complete Android builds, normal Release selection/discard, exact original
data readbacks and complete signed APK pass.
See [entry-category-captions.md](../../quality/entry-category-captions.md). Other A11Y-03 and owner/external gates stay open.

D-115 closes the specific empty-entry gesture hypothesis using the actual IME window and an identical native
gesture after Back, with no typing or Save. No production candidate is adopted. Keep completed category/financial
checks retained; last-position and broader platform acceptance stay open. Permanent ENT-01 policy still depends
on the owner's OD-03 decision; test-build limits remain disabled. The owner declined interim OD-10 exclusions
(D-114): deliver permanent planned behavior,
keep existing runtime policy and determine final backup/key/recovery handling with completed encryption.

D-116 removes observed duplicate goal warning surfaces, preserving the complete warning packet, its scalable
presentation and unchanged card action. AT-118 checks both native templates without Save; full delivery evidence:
[goal-warning-packets.md](../../quality/goal-warning-packets.md). The owner approved OD-03 on 2026-10-10;
after completing this verified step, ENT-01 is the next ready permanent section. Test-build enforcement remains
separately gated, with no limits activated by the Core policy section.

D-117 completes the approved ENT-01 model with 76 policy cases and 1,611 passing main tests (App.Tests 299
unchanged). No visible or enforced commercial restriction is introduced. The next ready section is ENT-02:
service-side checks for all creation paths with atomic quota decisions, disabled pending activation. Do not replace
permanent delivery with interim product policies. Scope, validation and APK: [evidence](../../quality/entitlement-policy.md).

D-118 continues ENT-02 with reusable actual-file commercial write transactions and account create/unarchive/
correction checks. Current enforcement stays inactive. 15 added SQLite cases/main 1,626; independent-provider
last-slot contention and failed-write rollback pass. Continue directly with the remaining ENT-02 resource/store
paths; do not mark the section complete or activate test-build limits. [Evidence](../../quality/account-write-policy.md).

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

D-137 completes exact-file pool retirement for profiles/restore and 39 fixture cleanup sites. Normal parallel full suite 2,210 passes without the D-136 output workaround; App.Tests 348. Native Release profile/restore/deletion and complete signed APK verified. Continue ready ENT-02/03 resource/automation bindings; choice UI/persistence, activation and external gates stay open. [Evidence](../../quality/local-sqlite-pools.md).

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

## D-141 completed boundary and next ready work

D-141 delivers scoped account/Plan selection persistence and actual new-money/work/reminder bindings, including
atomic explicit new-slot changes. 44 new cases / main 2,374 / App.Tests 382 pass. No commercial activation.
Next: the reviewed account/Plan chooser UI, covered loading, explicit Save and preserved draft/error feedback;
then remaining selected resources/operations. Continuous delivery remains authorized; owner/external gates stay open.
[Evidence](../../quality/durable-resource-choices.md).

## D-142 completed boundary and next ready work

The next D-141 slice, actual reviewed account/Plan chooser UI, is delivered and verified. 25 new AT-144 flows;
main 2,399 / App.Tests 407 pass, strict builds and complete signed APK/native evidence recorded. Current registration
remains inactive. Continue ready planned selected-resource choices and their actual service/UI bindings; activation,
purchases and existing owner/external gates remain separate. [Evidence](../../quality/active-item-chooser.md).

## D-143 continuous-delivery result

31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](../../quality/goal-resource-choices.md).

Continue goal chooser/effective contribution/reminder bindings under D-69; keep activation and external gates separate.

## D-144 continuous-delivery result

18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](../../quality/goal-chooser.md).

Continue effective goal/contribution/reminder bindings under D-69; keep commercial activation and external gates separate.

## D-145 continuous-delivery result

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

Continue goal/Home/detail contribution affordances under D-69, with complete original progress/history and retained corrections; commercial activation and external gates remain separate.

## D-146 continuous-delivery result

36 new AT-148 actual-source/real SQLite cases pass; final normal parallel main 2,520 / App.Tests 497 pass,
zero failures/skips, test output cleaned. Strict Windows and Android builds have zero warnings/errors. Actual list,
detail and Home pass 18 English/Persian/German, light/dark, 360/412/wide cases with process-local 1.5 font stress;
18 additional focused cases check the corrected neutral earmark/release explanation. Native glyphs, original tones,
44-pixel action targets, original historical Release/account, dirty price/currency feedback and actual retry/Back are
checked. All 25 complete fixture tables remain equal; original development files/sidecars and display are restored.

Six normal signed Release owned-emulator cases verify original Home/list/detail, completed goal and archived funding,
initially selected Release, enabled Save and native Back. One explicit negative earmark release is saved; complete
original goals, accounts, ledger and other rows remain equal. Only the pre-existing D-141 migration and that one
expected owned allocation differ. Original owned bytes/sidecars plus Main/English/System are restored; the final
normal signed Release is installed/stopped. No physical-device, iOS, OS accessibility, provider, startup performance,
Store signing/publication or commercial-activation acceptance is implied.

[Evidence](../../quality/goal-work-views.md). Continue the actual goal editor/contribution draft under D-69; commercial activation and owner/external gates remain separate.

## D-147 goal-editor delivery

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

Canonical Build-AndroidApk.ps1 produces artifacts/android/zanance-d147-release.apk, package pro.vafadar.zanance,
nondebuggable Release, intact ZIP, full ARM64/x64 assembly stores and application AOT, verified v2/v3 signature with
the existing local test certificate. Bytes: 81491883. SHA-256: `d3aa44afc6f55aae1ea3b4a0fe82700c5120ad196343afcd7cdcfbdf0d6879aa`.
No schema/model/migration, portable preference, security policy, credential, SDK, paid grant or permission change.

ENT-02/03 remain in progress. Continue remaining actual budget/new-work bindings, then other resources/operations,
import/restore/native paths and commercial messages under D-69. Commercial activation and owner/external gates stay
separate; this editor slice is not completion of the commercial release.

Evidence: `docs/quality/goal-editor-work.md`.

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

## D-152 - Original budget editor drafts

The original budget editor publishes complete references and defaults behind a loading cover. Exact original
id/file handoff retains archived/missing account/category limits and explicit scopes in both experience modes.
Raw amounts keep their input culture through live display changes. Save rechecks the complete review under the
actual SQLite writer; stale failures retain input and require explicit reload/review. Dirty fields alone survive
reload, unchanged fields adopt current saved values, and a committed form retires before native return.
Current new-tool rights are separate from retained corrections and future-period capacity. Registration remains
inactive. Budget view copy/delete/other work affordances and the remaining ENT-02/03 paths are still open.

35 new AT-154 actual application/isolated SQLite cases pass. Final ordinary main 2,681 / App.Tests 624 pass,
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass
without warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, native
validation/stale Save/Reload/Keep edits/Close and complete 25-table restoration. Six normal Release Android
language/theme cases verify original corrections/archived references; one explicit original-total Save preserves
all other metadata and ledger rows. Final normal Release Create/Close and a targeted weekly Create/Save verify
explicit absent identity and canonical zero stored month coordinates; complete owned data/bytes/display restored.
Physical device, iOS, paid activation and product publication are not claimed.

See [evidence](../../quality/budget-editor-drafts.md).

### D-153 - Reviewed original budget view actions

Original budget view actions now use the complete displayed BudgetEditorReview for copy, confirmed replacement
and deletion, rechecked before/after confirmation and under the existing actual-file writer. Period lookup uses
original calendar/currency/explicit weekly dates and rejects ambiguity. Viewing, original correction and deletion
remain separate from paid new tools and net current-definition capacity; future periods do not consume today's
slot. Busy spans native confirmation through refresh. A committed operation whose refresh fails requires a
read-only Retry, without repeating Copy/Delete. Read-only opening no longer creates Settings; financial history
and all original calculation inputs remain visible. Current commercial registration remains inactive.

26 new AT-155 actual application/isolated SQLite cases pass. Final ordinary main 2,707 / App.Tests 650 pass
without failures/skips; outputs cleaned. Strict Windows and canonical complete strict Android Release pass with
no warnings/errors. Eighteen hidden Windows cases cover en/fa/de, light/dark, 360/412/wide, full native action
names/targets, retained paid history/correction, changed-confirmation rejection and read-only Retry. Six normal
Release Android language/theme cases verify actual original copy/delete confirmations; exactly one owned copy
and deletion of that copy preserve all original 25-table data. Original owned bytes/sidecars/Main/English/System
are restored, final signed Release installed/stopped. ENT-02/03 and remaining resource/operation/import/restore/
native paths stay open. Physical-device, iOS, provider, paid activation and publication acceptance are not claimed.

See [evidence](../../quality/budget-view-actions.md).


### D-154 - Reviewed quick templates and saved queries

Durable explicit quick-template/saved-filter choices retain every original packet, recheck revision/value and
same-timestamp changes, and use the existing actual-file scoped writer. Chosen new slots and explicit filter
replacement are atomic; retained correction/delete/history stay separate. Actual template create/apply/delete
and saved-query save/delete commands use complete reviews and one busy dialog/write/refresh guard. Queries are
captured before prompts; committed refresh failure never resubmits. Template names wrap, publication is covered
and explicit Reload is available. Six growing chooser destinations and help explain retained query history.
Current Release registration stays inactive; no schema, network, SDK, permission or portable grant is added.

29 new AT-156 cases/main 2,736 behaviours (App.Tests 679) pass after repairing one stale four-kind expectation;
only that failed case reran. Strict Windows/complete Android Release have zero warnings/errors. Eighteen hidden
Windows locale/theme/width cases and six normal Release emulator cases restore complete owned rows/bytes/display.
Physical-device, iOS, OAuth, commercial activation and publication gates remain open. ENT-02/03 are still in progress.

See [evidence](../../quality/quick-resources.md).

### D-155 - Original Home template destinations

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

## Finite remaining operation groups (D-168)

Continue the three confirmed ready groups in [the current inventory](07-current-entry-point-inventory.md):
forecast/snapshots, receipt recognition, then loan/reports/PDF. Retain the original calculators and guards;
prove each real new boundary once. Do not repeat delivered resource/recovery integrations or unchanged
successful verification. External decisions and provider-dependent purchase UI remain separate.

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

## Original report/PDF continuation (D-172)

The last confirmed D-168 operation group is locally delivered, with original financial calculations,
Free facts/status, detached PDF and reviewed native sharing/contribution Save. Complete package and
local evidence are in [report-work](../../quality/report-work.md). Next is the bounded ENT-02/03
completion audit. Activation, billing/provider/encryption/owner/device/iOS/release gates stay separate.

## Bounded local commercial review closed (D-173)

The finite current ENT-02/03 review is closed; delivered original operations are not rebuilt. Commercial
activation, verified billing/provider facts and actual shared-server lifecycle remain separate work.
No unchanged tests/builds/native matrix or APK regeneration for this source-only review. Next ready
engineering: A11Y-01 Android accessibility; record emulator limits and observed defects explicitly.
[Evidence](../../quality/commercial-completion-review.md).

## Android quick widget native continuation (D-174)

Fix observed native roles, target and stale-language defects; retain original intents, data and device
settings. Twelve actual widget cases/cold receiver/three-language draft routes and the complete signed
APK are verified. The whole TalkBack pass remains open; continue remaining actual accessibility work.
[Evidence](../../quality/android-widget-accessibility.md).

## Date naming continuation (D-175)

Original date-purpose names and calendar actions are locally delivered with retained raw input and
unchanged values/calendars. Eighteen Windows and nine normal Release Android Plan/debt contexts;
main 3,169 and full signed APK. Continue actual remaining accessibility findings; spoken/platform
acceptance remains separate. [Evidence](../../quality/date-context-accessibility.md).

## Startup preparation follow-up baseline (D-176)

Current bounded measurements and synchronous source boundary are recorded. Implement protected
original preparation, then native before/after and failure/retry checks; retain original security,
financial/legacy-unit and link behavior. No unrelated test/build/APK repetition for measurement-only
work. [Evidence](../../quality/startup-current-baseline.md).

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

## Windows Community producer (D-181)

Deliver official source/tool restoration and Windows build/package with original 32-migration
compiled-model compatibility before native Android/security-suite work. Changed recipes cannot
resume stale objects. Preserve negative determinism/link findings and exact clean/package results
in the build guide. Main app stays on D-179 baseline until native platforms, keys and crash-safe
migration are verified. No temporary OS-backup policy, recovery decision or publication is implied.

## Official crypto suite (D-182)

Stock Windows OpenSSL suite passes on matching candidate; record exercised tests and all skips
separately. Native hash unchanged. Official r30/Linux host preparation enables the next Android
source-build step; no mobile binary/runtime or app activation is yet accepted. Main app/APK remain
D-179. Continue native matrix/SQLCipher suite, key/recovery and crash-safe migration work.

## Android native slices (D-183)

Canonical source restoration and official r30 API24 arm64/x64 build/ELF/package/native x64 proof
are delivered independently. Keep main app and D-179 APK unchanged until the final backend is
accepted. Next: SQLCipher upstream suite and native CI/iOS matrix, followed by actual managed
platform schema, per-profile keys and crash-safe migration. Physical arm64/16 KiB/minimum-API
and owner recovery/OS-backup gates remain; a candidate package is not whole-app activation.

## Stock cipher suite and initial Native CI (D-184)

Maintained stock SQLCipher runner passes 226 tests and verifies actual provider/no-fallback/no-leak
evidence. Initial separate Linux Native workflow is configured and parsed; hosted execution is
still unaccepted. Continue native CI/iOS/managed-platform schema and key/crash-migration work;
physical/runtime and owner recovery/OS-backup gates remain. App and D-179 APK are unchanged.

## Managed Android schema (D-185)

Separate Release original-source/schema probe and shared Windows caller pass; preserve actual
D-44 composition and negative diagnostics. Main application stays on D-179/plaintext. Continue
native hosted/iOS matrix and final keyed storage/crash migration work; owner recovery/OS-backup
and physical/minimum-API/page-size gates remain. No interim app policy or full-release claim.

## Memory portable snapshot boundary (D-186)

Ship reusable infrastructure with actual schema/header/history/encrypted staging proof, not a
plaintext temporary-file workaround. Full 3,185/strict builds and independent Release runtime pass.
Keep live source/provider/backup unchanged until per-profile keys, authorization and crash-safe
verification/commit/recovery are accepted. Continue those final boundaries and remaining native matrix.

## Complete staging verifier (D-187)

Verify all original typed content and metadata before future generation acceptance; direct native
span comparison resolves the observed driver crash. Full 3,197/strict builds/independent runtime
pass. Continue final key/durable generation/recovery integration and remaining platform gates;
no app provider/pointer migration is activated by a verification primitive.

## Explicit generation checkpoints (D-188)

Final protocol model rejects skipped/repeated/foreign-identity transitions and retains original
selection until explicit commit. Eight cases/full 3,205 pass. Continue actual durable key/generation
coordination and restart fault injection; the model alone is not crash-safe migration acceptance.
Keep OD-04/OD-10 gates and live database/provider/backup composition unchanged until accepted.

## Resumable coordination (D-189)

Ordered native-effect coordination and readback checks pass fifteen new/full 3,220 cases. Do not
substitute faultable-port tests for real native durability. Continue actual captured key/selector/
cleanup adapters and process-restart proof after required policy choices; app encryption and final
migration remain incomplete. Do not repeat unchanged UI/native matrices.

## Native CI ownership failure correction (D-190)

Fix the observed hosted Linux output refusal with shared non-root owner/work mounts and focused
runtime regression. Keep all original native versions/hash/security checks. Push and check the
new hosted run once; do not repeat application tests/builds/UI or unchanged upstream suites locally.

## Native parent traversal correction (D-191)

Fix the actual remaining mode-750 parent refusal and reproduce it on the old image with foreign UID.
Seven focused runtime checks/exact host identity pass after scoped 0755 traversal. The same hosted
workflow [38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) completed successfully; ABI hash comparison, all ELF
refusals and official 226-case suite pass. No unrelated application checks or native rerun required.

## Approved recovery/OS policy and Windows keys (D-192)

OD-04/OD-10 are settled; continue native key persistence/adapters, durable generation migration,
optional recovery UX and final coupled OS exclusion without another policy question. Windows
DPAPI/context primitives pass twelve new/full 3,232 cases and actual two-process encrypted reopen.
Do not substitute this primitive for durable storage or full app activation. Existing UI/native
build/suite acceptance is not repeated without an actual change/concern.

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

### D-263 — Complete original Android protected storage batch

Delivered the original Android provider, covered all-profile startup/lifecycle, legacy continuation,
automatic native keys, optional acknowledged recovery, independent PIN authorization, retained portable
recovery, encrypted safety and reference-aware retirement. Final OS exclusion is coupled to adoption.
The final corrected APK and 3509-test suite, strict builds and 18 native language/theme/width cases pass.
Observed long-caption and October cryptography-package failures were fixed; only affected checks were
repeated. Exact owned envelopes/financial hashes and user data were preserved. No temporary review
hooks or unrelated verification remain. [Full evidence](../../quality/android-protected-storage.md).

Next ready large batch is explicit Windows main protected-runtime adoption; iOS requires verified
Mac/Xcode/signing. Other approved ready canonical work continues afterward. Physical/provider/billing/
Store gates remain owner/external acceptance. Hosted CI is checked once for the final pushed commit;
no green local check accepts physical power loss, full crash points, real devices or publication.
The full continuous-development objective stays open.


## Next complete delivery: approved local backend (D-271)

Deliver ZCR-ID-01 as one end-to-end batch, then check and commit its final state. Implement the
server in Zanance's own API project with its own PostgreSQL schema, independent of local financial
SQLite/profile storage. Keep maintained ASP.NET Core identity infrastructure and explicit server
ownership. Do not expose financial sync/sharing or billing routes under this foundation approval.

1. Establish current supported authentication/database packages, threat boundaries and a separate
   local deployment. Reuse an already installed PostgreSQL image in a new repository-owned
   container/volume; never reuse another project's database, credentials or buckets. Additional
   non-build downloads require approval under AGENTS.md. No paid hosting or actual mail sender.
2. Complete identity verification/recovery, bounded device sessions, revocation, server ownership,
   transactional one-use challenges and durable abuse limits. A fake client identifier is not an
   ownership proof. Local fictional mail capture must be isolated from production configuration.
3. Complete private object authorization and durable storage plus repeatable database migrations,
   persisted protection keys, background outbox/leases, health/readiness and graceful shutdown.
   No public object URLs or request-body/token/financial logging. Document operational limits.
4. Check the whole delivered service against real isolated PostgreSQL: concurrent replay, expiry,
   recovery, revocation, cross-user denial, invalid inputs and storage failures. Perform a real
   backup/restore to a distinct fictional database/object destination, compare relationships and
   object bytes, and measure restoration. Do not substitute mocked storage for this acceptance.
5. Update architecture/privacy/runbooks and backlog, build server/tests strictly, clean their owned
   output and push. Mobile UI/build/APK checks are needed only if this batch actually changes the
   mobile app; unchanged D-268 evidence is retained. Hosted/device/store acceptance stays separate.

The following regional batch implements LOC-07/08 for Germany, the other Western European markets,
USA, Canada and Australia. Define an explicit country list and official jurisdiction/year sources;
keep federal, provincial/state and national holidays distinct, bound supported years and show
uncertain/unavailable data truthfully. Use an extensible country catalogue separate from language.
Iran is a later separate variant, not implicitly enabled in this batch. New languages remain last.


## Approved next complete batch: LOC-09 (D-281, 2026-10-15)

1. Reuse device-envelope encryption and original bounded package validation. Separate automatic
   timestamps, names and retention from manual portable and pre-restore copies; no password storage.
2. Complete start/resume/onboarding-finish scheduling, daily/weekly/off device preferences and
   coalesced attempts. Capture/recheck profile and generation inside native key/maintenance ownership.
3. Complete translated status/help and current-profile recovery-point discovery. Route selection
   through the original preview/authentication/confirmation/restore; never share a device-only file.
4. Verify original library/application sources, concurrency/failure/profile changes, actual independent
   Windows/Android encryption, exact data preservation, independent retention and explicit restoration.
   Review changed surfaces in fa/en/de, light/dark, 360/412/wide and build strict targets/signed APK.
5. Update privacy/spec/acceptance/decision records and commit/push the complete verified batch. Keep
   failed observations and avoid unrelated successful verification. The complete local/native D-281 batch is delivered; see ../../quality/automatic-local-backup.md. Personal-sync product approval and other owner/external gates remain open.

## Complete coherent Home read batch (D-283)

1. Capture all original Home inputs under one actual factory lease/keyed connection/deferred SQLite reader;
   reuse the original quick/plan/goal guard methods and exact original budget/carry/valuation scopes.
2. Connect every original Home section to that packet, keeping covered publication, fresh original validation,
   explicit later actions, pinned goal/history availability and retired-context refusal.
3. Prove real concurrent SQLite snapshot behavior, absent default writes, foreign/stale access refusal and
   original goal projection. Compare original/new complete operations on the same protected reference/tenfold data.
4. Complete native Home first-read/failure/retry in en/fa/de, both themes and 360/412/wide, strict builds and
   same-process signed Release emulator open/Cancel/return. Preserve complete original rows and any negative observations.
5. Document and push the verified whole batch; do not infer a two-second experience or physical/iOS acceptance.

This local engineering batch is delivered; see ../../quality/coherent-home-read.md. QA-06 still retains its
loaded native/phone/cold/ANR/platform gates. Personal financial sync, sharing, purchases and other separately
recorded product/provider decisions are not implied by backend foundation approval. The full goal remains active.

## Loaded native Home batch (D-284)

Resume only the marked protected reference/tenfold datasets; run original native Home publication twice
per en/fa/de context, measure native layout and dispatcher delays separately from cold startup, verify
complete totals and final typed-row preservation, then document the observed limits. Delivered locally;
see ../../quality/loaded-native-home.md. No unrelated successful cohort, full suite or APK was rebuilt.
The synchronous appearance layout read and largest native gap remain concrete follow-up concerns.
