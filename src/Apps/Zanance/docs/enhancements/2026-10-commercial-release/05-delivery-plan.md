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
