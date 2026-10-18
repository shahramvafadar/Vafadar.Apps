# 08 – Release checklist (first public release)

## Product and data

- [ ] Every Android phone-test handoff includes a complete signed Release APK with package/signature verified
  and the actual file path supplied (D-66); installation and physical-device acceptance are checked separately.
- [ ] All phase-1 acceptance scenarios pass on the **release** build (07), except explicitly unshipped cloud destinations
- [x] Golden data AT-62 exact in en/fa/de (calculation and display tests: separators per language, Persian digits)
- [ ] Upgrade from every earlier test build keeps data (AT-60) – integration test for the first schema passes; re-check with real test builds
- [ ] Encrypted backup → uninstall → reinstall → restore gives identical balances (AT-57) – fresh-install integration test passes; device check pending
- [ ] No known critical bug in balances, conversion, double counting, restore or data exposure (Q-01)
- [ ] Performance measured on the reference device with 10,000 entries (Q-02) and recorded – calculation budget test with 10,000 entries, 20 accounts and 100 plans passes
- [ ] Deviations in spec Section 31.3 accepted by the owner or resolved

- [ ] D-62 device acceptance: fresh install → choose theme/experience or restore an existing backup; cancel and return
  without losing the draft; restore protected and unprotected files without an extra account. For each released cloud
  provider, upload/restore with protection on and off, with a real OAuth client. Windows checks do not close this gate.

- [ ] D-64 receipt acceptance (AT-71, ZCR-QA-05): physical Android/iOS camera and file selection, EXIF, skew,
  missing/conflicting totals, currency/unit review, stored-image reread, cancel/error and explicit-save checks on
  representative receipts. Windows synthetic-image/UI checks and C# regressions pass; they do not close this gate.

- [ ] D-65 device acceptance (AT-72): new/existing plans, monthly anchors in Gregorian/Persian/Hijri, short months,
  count/end date, custom rules, debt/receivable creation, actual repayment versus estimate and reminder permission;
  draft cancellation must not post or alter balances. Windows fixture checks do not close this gate.

## Privacy and security

- [ ] Privacy matrix (06) reviewed against the release APK/AAB: package list, merged manifest permissions, network traffic
- [ ] Decide the release variant (D-35, D-50): **offline** (no OAuth client ids) or **with cloud backup** (`MICROSOFT_ENTRA_CLIENT_ID` and the Google client of the platform – `GOOGLE_OAUTH_CLIENT_ID_ANDROID`, `…_IOS`, `…_WINDOWS` – set). Client ids only: no client secret exists for any of them
- [ ] Offline variant: release manifest still without INTERNET and ACCESS_NETWORK_STATE after package updates (ML Kit asks for both; D-31)
- [ ] Cloud variant: merged manifest has INTERNET and the MSAL redirect activity with `msal{client-id}`, no ACCESS_NETWORK_STATE; connect, back up, list, restore and disconnect verified on a device for each provider; privacy policy, Data safety and store texts switched to the cloud variant
- [ ] Cloud variant, iOS: the built Info.plist lists the URL schemes `msauth.pro.vafadar.zanance` and the reversed Google iOS client id (and none in an offline build); the signed app has `keychain-access-groups` = `<Team ID>.pro.vafadar.zanance`; Microsoft and Google connect, back up, restore, disconnect (and reconnect after an app restart) verified on an iPhone; App Privacy answers re-checked
- [ ] Cloud variant, Windows: Google opens the default browser, the page "Sign-in complete" appears after consent, and the app shows the account; the files `%LOCALAPPDATA%\…\google\google.token` and `msal\msal.cache` are DPAPI-protected and removed or emptied on disconnect; a firewall prompt does not appear for the `127.0.0.1` listener
- [x] `INTERNET` permission removed if no online feature ships (D-20) – the offline build's merged manifest declares only notifications, boot and biometric (USE_BIOMETRIC / USE_FINGERPRINT from AndroidX Biometric); re-checked 2026-09-29 with the cloud libraries referenced
- [ ] Privacy policy published at a stable URL on vafadar.pro, reachable in the app and in Play Console (PRI-03)
- [ ] Android Auto Backup disclosed (D-16); decision re-checked
- [x] No financial data, notes, tokens or passwords in logs (SEC-04) – code review 2026-09-29: only `Debug.WriteLine` (removed from release builds) and the debug logger in Debug builds
- [x] Recent-apps preview and notifications hide financial data by default (SEC-02, REM-05) – notifications generic by default; preview always protected (Android API 33+ recents exclusion and secure background flags, iOS cover; D-63). Foreground screenshots may be allowed on Android. Device check pending

- [ ] D-63 device acceptance: set/change/remove PIN, failed-attempt delay across restart, notification/backup/export gates, recovery success/cancel/unavailable, secure-storage failures; Android screenshot toggle and recents on supported Android versions; iOS keychain/reinstall and privacy cover

## Build secrets

- [ ] GitHub repository secret `SYNCFUSION_LICENSE_KEY` set; `Vafadar.SyncfusionLicense.Tests` passes in CI (the release workflow requires it)
- [ ] Android signing secrets in the `production` environment
- [ ] Cloud variant only: OAuth clients registered (Entra redirect with the signature hashes of upload and Play app signing key, iOS/macOS platform with bundle ID `pro.vafadar.zanance` → `msauth.pro.vafadar.zanance://auth`, `http://localhost`; Google Android clients for both SHA-1, a Google iOS client for bundle ID `pro.vafadar.zanance`, a Google Desktop app client whose secret is not used), Google consent screen verified
- [ ] iOS: App ID `pro.vafadar.zanance` registered in the Apple Developer account, signing certificate and provisioning profiles (development, App Store) for it; built and signed on a Mac with Xcode

## Store

- [ ] Data safety form completed from the matrix (PRI-04)
- [ ] Financial features declaration completed per current Play guidance (REL-01)
- [ ] Target API level, signing, content rating, target audience per current Play requirements (REL-02)
- [x] Final app name: **Zanance** (owner decision 2026-09-26)
- [x] Icon, splash and notification icon from the approved brand master (D-26)
- [x] Draft store texts and eight fictitious native Windows screenshots in each of en/fa/de/es/fr/it (REL-03/04, D-338), with exact RGB provenance. Mobile screenshots, featured resolution, Store acceptance and publication remain open; see `docs/quality/store-drafts.md`.
- [x] Third-party licences listed in the app (Settings → About)
- [ ] Internal → closed testing → production track

### D-67 device gate

- [ ] On the signed APK, connect each configured provider, see automatic/empty/error feedback, create a protected
  and unprotected backup, refresh, preview, restore on a fresh installation and delete only the chosen file.
- [ ] Verify Google package/certificate registration matches the installed APK; a changed debug certificate can
  require owner console configuration, independently of the phone model.
- [ ] English UI + German formats + Gregorian + German holidays + Latin digits; retain choices after restart and
  restore, with state-holiday coverage explicitly excluded. Repeat Persian RTL and native pickers.

Local automated/rendered checks and CI do not close these physical-device or provider gates.


SEC-01 / D-68 gate: proposed ADR 0010 and independent Windows/Android emulator proof do not clear the encryption
release gate. Android API 36 x86_64 runtime and Keystore process-restart proof pass for fictitious data only. ARM64
phone, iOS, exact maintained native binaries, recovery, key loss, migration fault injection, OS backup and
no-plaintext-safety-copy checks remain. Never ship the deprecated feasibility provider through app dependencies.


D-69 / SEC-09: run the complete-APK permission guard for the selected Offline/Cloud Release variant; keep the SDK
review and store declarations matched to that binary. Native traffic, real OAuth and signed iOS SDK privacy-manifest
review remain gates; repeat before billing/sync SDKs. Never claim no SDK diagnostics in a cloud-enabled Android build.

D-70 / LOC-02 device gate: verify contribution reminder opt-in/refusal, local 09:00 scheduling with the saved calendar,
generic/details text, pause/reach/resume/disable cancellation and notification taps through the app lock on a signed
APK. Android emulator fixture delivery does not close ARM64 phone, Doze/timezone/reboot or iOS device acceptance.

D-71 / LOC-03 device gate: verify review-reminder opt-in/refusal, selected-calendar/pay-cycle boundaries and local
09:00 delivery, generic/details text, tap through app lock, finishing/opt-out cancellation, profile switch/restore,
Doze and reboot. Local tests and isolated API 36 x86_64 native delivery/tap do not close ARM64 phone or iOS acceptance.

D-72 / LOC-04 device gate: use the complete signed APK to preview/link/keep overlapping aggregates, confirm totals,
restart and Undo; exercise partial files, stale previews and dependent edits/imports with disposable data. Native
API 36 x86_64 import and Release Undo are verified; ARM64 phone and iOS runtime remain open. Historical metadata in
ImportLinks travels with database backups and is retained until its import is undone or profile/data deletion.

D-73 / QA-03: AT-80 application tests cover actual non-UI flows, not native platform acceptance. On the owner's phone
verify secure startup, widget drafts after unlocking, PIN/device confirmations, short/long background returns,
profiles, bulk validation/delete/Undo and theme changes with unsaved input using the complete signed APK. No result
from isolated SQLite, a native port double, the emulator or CI substitutes for those physical-device release checks.

D-74 / QA-04: repeat manual Income/Expense on a valued asset on the physical phone. Cancel must keep the draft and
asset value; Record anyway must save only once after validation. Transfers/adjustments must keep their meanings.
AT-81 local/emulator results and the complete signed APK do not close physical-device, iOS or product acceptance.

D-75 / Q-02: select an actual physical reference device and record process-cold Home, search and save with the
10,000/20/100 workload and explicit tenfold stress shape. Include native rendering and input response, not only
calculation/store timing or system first frame. Verify there is no ANR and that every matched result row is visible
and accessible after empty/nonempty filtering. Desktop/emulator timing and AT-82 do not close these device gates.

D-76 / AT-83: verify loading feedback, disabled search/filter/Add/bulk actions until publication, no false empty
state, failure/retry, retained filters on return and actual matched result buttons on the physical phone. The
publication helper tests, Windows captures and emulator observations do not close native rendering, performance,
ANR, iOS or device acceptance gates.

D-77 / A11Y-03: on the physical phone exercise 200% system text, complete quick-action names/targets, plan identity,
amount/date/status, both themes and RTL/language changes. Review remaining financial rows, currency tokens, fixed
actions, tabs, calendars, pickers, charts and dialogs. Process-local Windows stress and an emulator activity-only
configuration do not close real OS scaling, screen readers, ARM64 phone or iOS acceptance. Use the complete signed
Release APK; it has no font-scale review override or native text collector.

D-78 / A11Y-03: verify persistent Add/bulk/Undo actions remain outside scroll content, financial names/subtitles
wrap above amounts and Undo text/action remain readable. Include en/fa/de, both themes, 360/412/wide widths and
real OS 200% text. Local normal-scale API 36 review and Windows stress do not close physical ARM64/iOS/screen-reader
acceptance. Investigate early Settings selections that are ignored, fixed debt/bulk buttons and currency grouping.
AT-85 records the delivered test APK; require its actual signature/package check before phone handoff.

D-79 / AT-86: on the phone verify that Settings is covered until ready and offers retry after failure. Change
en/de/fa on the open form, confirm theme/mode and other choice captions update, selected values and unsaved inputs
remain, and language persists after restart. Use the complete signed D-79 APK recorded in AT-86's evidence report.
Windows/native emulator checks do not close real OS 200%, physical ARM64, screen-reader or iOS acceptance.

D-80 / AT-87: verify large Settings/account/debt captions and bulk actions with physical system 200% text, both
themes and en/fa/de. Confirm full spoken names, keyboard/touch targets, disabled selection state and real Select
all/Cancel. Account/debt Save and security/destructive actions retain their existing confirmation boundaries.
Use the full signed APK in the quality report; Windows stress and normal emulator runs do not close phone/iOS QA.

D-81 / AT-88: use the full signed APK in the quality report to check complete years/day/month inputs, valid and
partial date drafts, calendar redraw and the original monetary sign/decimal/currency. At physical system 200%,
reach both ends of any oversized value with a real swipe and verify the full spoken packet. Account movement rows
and Home's account heading remain usable. No Save is needed for draft review. Windows stress/normal emulator
checks do not close actual OS, screen-reader, ARM64 phone or iOS acceptance; preserve existing security settings.


D-82 / AT-89: install the complete signed APK from the quality report. Select Deutsch in Settings, return, reopen
and select فارسی, then repeat with English. Captions, selected language and returned navigation must agree without
a restart between choices; an open draft stays intact. Confirm cold persistence separately. Local native/Windows
evidence does not close physical-device, real OS large text, screen-reader, provider or iOS acceptance.

D-83 / AT-90: use the complete signed APK in the quality report. After real de/fa/en language choices on the open
Settings form and after Back/reopen, verify the native arrow has the current translated spoken name. Check actual
OS large text separately. Windows child titles must show all words above the body with a usable Back target at
360/412/wide, both themes and RTL. Emulator/Windows evidence does not close screen-reader/device/iOS acceptance.

D-84 / AT-91: use the complete signed APK in quality/settings-estimate-draft.md. Enter an unsaved estimate/period,
open a child/modal and return; text, period and currency must remain until explicit Save or leaving the form. A clean
field can refresh persisted changes. No unfinished input is promised across restart. Local native/Windows checks
remain separate from physical-device, real OS large-text, screen-reader and iOS acceptance.

D-85 / AT-92 evidence in quality/insights-navigation.md covers complete visible Windows root destinations and
actual native invocation/resize/data equality. Real OS text scale, screen readers, physical ARM64/iOS and other
control findings remain independent release gates; the growing navigation does not establish those outcomes.

D-86 / AT-93: verify complete budget figures and signed-boundary formatting using quality/budget-readouts.md.
Keep native runtime data-equality/fixture-restoration checks, strict suites/builds and complete signed APK separate
from physical OS scaling, screen readers, ARM64/iOS, OAuth, Store signing and owner product acceptance.

D-87 / AT-94: quality/budget-periods.md records the hidden-choice failure, actual visible caption/target geometry,
real selection/data-retention checks and signed Release handoff. Keep OS 200%, keyboard/screen readers, physical
ARM64/iOS, real OAuth and Store/product acceptance separate.

D-89 continues A11Y-03 with complete growing tag suggestions and original-value native selection. AT-95 checks
84 actual native invocations and 21 exact draft/stored-data restorations across the three-language/theme/width
matrix; normal signed Android Release confirms selection, Keep editing and Discard without financial writes.
Main suite remains 1,369 (App.Tests 140); strict Windows and equivalent-command Android Release have no warnings/errors.
PowerShell startup blocks the canonical APK/privacy scripts locally; the complete signed package and matching binary
policy are independently verified. Evidence and remaining tooling/OS/screen-reader/phone/iOS/owner gates:
[quality/tag-suggestions.md](quality/tag-suggestions.md).

D-90 continues A11Y-03 with state-matched transaction detail actions and retained unsaved fields. AT-96 checks
48 actual native Invoke operations and 24 complete draft/stored-row restorations, including Simple/Advanced initial
visibility. Main suite remains 1,369 (App.Tests 140); strict Windows and canonical Android Release have zero warnings/
errors. Normal signed Release passes hide/show, retained payee/tag/note values and Keep editing/Discard in en/fa/de;
complete fictitious financial rows remain unchanged. Canonical APK/privacy scripts and full package/signature pass.
Evidence and independent OS/screen-reader/phone/iOS/provider/owner gates:
[quality/entry-details-disclosure.md](quality/entry-details-disclosure.md).

D-91 / AT-97 keeps both first-run restore alternatives fully readable and retains their existing navigation/busy
bindings. The final Windows matrix and signed Android Release pass; 1,369 tests (App.Tests 140), zero-warning strict
builds and full installable APK/privacy checks. See [quality/onboarding-restore-actions.md](quality/onboarding-restore-actions.md)
for native draft/stored-data preservation and independent platform/physical-device/provider/owner gates.

D-92 / AT-98 removes measured redundant Home row/goal work while retaining complete values and financial rules.
All 1,384 tests pass (App.Tests 148), strict builds and complete signed Release APK/privacy/native checks pass.
Controlled tenfold Windows warm reload median: 2,727.70 -> 1,228.28 ms. QA-06 remains partial: entry materialization,
cold start/ANR and physical-device Q-02 are open. See [quality/performance-home-snapshots.md](quality/performance-home-snapshots.md).

D-93 fixes the Windows-only Home diagnostic call that broke Android Debug, with no Release/UI/financial behavior
change. Final Windows/Android Debug/Release builds and all 1,384 tests (App.Tests 148) pass. Temporary native stage
measurements identify entry materialization/initial account creation; QA-06 and physical/platform acceptance remain
open. Evidence: [quality/home-debug-platform-and-native-stages.md](quality/home-debug-platform-and-native-stages.md).

D-94 completes the local Customize Home caption/target follow-up: all eight section identities grow at full
width above the original controls, with 44 px targets and a complete Reset action. AT-99 final en/fa/de theme/width/
200% review and 84 native operations, 1,384 main tests, strict Windows/Android Debug/Release builds and signed Release
owned-sample readbacks pass. A11Y-03, QA-06 and real device/platform acceptance remain partial/open.
Evidence: [quality/home-customization-readable.md](quality/home-customization-readable.md).

D-95 / AT-100 removes measured eager native row construction for hidden Home Accounts while retaining the
complete snapshot and every shown account/action. Native tenfold publication median: 5,339.89 -> 10.69 ms, with no
total cold-start/Q-02 acceptance claim. Final language/theme/width/200% native visibility/detail/list checks, 1,384
main tests, strict Windows/Android Debug/Release builds, signed Release and complete owned-sample readbacks pass.
QA-06 full entry materialization, visible creation, cold duration/ANR and real device/platform acceptance remain open.
Evidence: [quality/home-hidden-account-views.md](quality/home-hidden-account-views.md).

D-96 / AT-101 completes account type/default/excluded/incomplete captions in the shared account row at large
native text, retaining complete text, flags, balances and row actions. Final en/fa/de themes/widths/200% and normal
text checks cover all eight flag combinations; 1,384 main tests, strict Windows/Android Debug/Release builds, signed
Release navigation and exact 24-table original-sample readbacks pass. A11Y-03 other controls/modals and actual OS,
screen-reader, phone/iOS and release acceptance remain open.
Evidence: [quality/account-descriptions-readable.md](quality/account-descriptions-readable.md).

D-97 / AT-102 completes the existing Accounts debt/receivable action caption with a growing real button,
unchanged text/scaling/command and native open/cancel verification of the same unsaved Loan form. Complete stored
accounts/entries/settings/budgets/schedules remain unchanged. Final en/fa/de themes/widths/200% and normal text,
1,384 tests, strict Windows/Android Debug/Release, signed Release navigation and exact 24-table readbacks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/debt-entry-action-readable.md](quality/debt-entry-action-readable.md).

D-98 / AT-103 removes duplicated Windows modal titles and reflows all eleven existing modal title/Cancel
rows without changing forms or financial behavior. Actual glyph/name/target/no-overlap and native Cancel/parent
Back pass across all Windows language/theme/width/text contexts; all nine complete stored data sources remain.
Normal Android debt/expense/plan forms pass in three languages and both themes; original three-profile tables
remain identical. All 1,384 tests, strict Windows/Android Debug/Release and complete signed APK checks pass.
A11Y-03 other controls and actual OS/readers/phone/iOS/release acceptance remain open.
Evidence: [quality/modal-headers-readable.md](quality/modal-headers-readable.md).

D-99 / AT-104 collects all applicable plan-field problems in one invalid Save attempt, displays destination
feedback beside its input and reveals the first problem using fresh native layout. Drafts and complete stored
Accounts/Entries/Settings/Schedules remain; existing money/recurrence/unknown-amount rules are unchanged.
Twenty-four new behaviour cases bring the main suite to 1,408 (App.Tests 172). Full Windows language/theme/width
review, strict Windows/Android Debug/Release, normal Android invalid Save/cancel, signed APK and original 24-table
readbacks pass. Other A11Y-03 controls and OS/readers/phone/iOS/owner/release gates remain open.
Evidence: [quality/plan-validation-visible.md](quality/plan-validation-visible.md).

D-100 / AT-105 collects independent transaction monetary problems before mutation, shows each beside its
input, reveals the next affected field and reopens invalid collapsed details without replacing entered values.
Existing parsers, consent/receipt protections and successful ledger/fee/overlap/attachment ordering remain.
Forty-eight new cases bring the main suite to 1,456 (App.Tests 220). Complete Windows matrix, strict builds,
normal Android invalid Save/cancel, signed APK and original 24-table readbacks pass. Simple destination-fee
retention is a separate pending runtime concern; other controls/platform/owner/release gates remain open.
Evidence: [quality/entry-validation-visible.md](quality/entry-validation-visible.md).

D-100 final callback review: native Windows callback failures rejoin the awaited action guard; timed-out queued
requests expire. Eight final German invalid Saves/61 renders, strict rebuilt Windows/Android Debug/Release,
one final-APK English Android invalid Save, repeated original 24-table readbacks and final native handoff pass.
The main validation tests are unchanged. Original full-matrix counts and the pre-guard Android candidate remain
distinct from these final checks; evidence: quality/entry-validation-visible.md.

D-101 / AT-106 closes the reproduced destination-fee loss in successful Simple edits. Existing fees stay
visible/editable in both modes; new destination-fee creation remains Advanced-only. Native retention, explicit
edit/removal and reopened/new Cancel/Discard preserve financial fields/ids and unrelated rows. The final
Windows matrix has 24 contexts/72 valid Saves; main tests remain 1,456 (App.Tests 220), strict builds and signed
APK pass. Normal Android creation-policy checks and original financial/preference values and expected settings audit pass; successful stored-fee
editing on Android/physical/iOS and other owner/platform/release gates remain open.
Evidence: [quality/destination-fee-retention.md](quality/destination-fee-retention.md).

D-102 / AT-107 delivers complete settlement period/bill feedback and a growing Record the difference
action. Invalid Save reveals the first affected input without changing the draft; original advance/refund logic
retains zero bills and no-op exact bills. Fourteen new cases bring the main suite to 1,470 (App.Tests 234).
The final Windows matrix has 24 contexts/144 invalid and 72 valid native Saves; six normal-scale Release Android
contexts/18 invalid Saves, strict builds, signed APK and original financial/preference-value checks pass.
Normal Settings audit updates are separate; physical/iOS/readers, valid Android settlement acceptance and other
owner/platform/release gates remain open. Evidence: [quality/settlement-feedback.md](quality/settlement-feedback.md).

D-103 / AT-108 completes due-item action captions and correctly placed payment/override feedback, with independent
corrections and native revelation of the actual affected field. Fourteen new cases bring the main suite to 1,484
(App.Tests 248). Windows: 24 contexts/96 invalid and 96 valid native Saves, plus a separate 24-context/96-invalid
final contextual-message review with no additional financial Saves; normal-scale Release Android:
six contexts/18 invalid Saves. Actual metadata changes never post entries; partial payment and completion retain
the intended entries and unique settlement. Original financial rows/states are preserved/restored; strict builds
and the complete signed phone-test APK pass. Phone/iOS/readers, valid Android occurrence acceptance and other
owner/platform/release gates remain open. Evidence: [quality/occurrence-feedback.md](quality/occurrence-feedback.md).

D-104 / AT-109 delivers complete transaction row reuse for repeated filters, scoped to one data/display snapshot.
Four new tests pass (main 1,488). Final running-app/build/APK checks and measured performance limits are recorded in
[quality/transaction-row-reuse.md](quality/transaction-row-reuse.md). QA-06 and real device/platform/release gates remain open.

D-105 / AT-110 removes repeated full-ledger membership scans from complete bulk-selection reload. Final tests/runtime/build/APK evidence: [quality/bulk-selection-reload.md](quality/bulk-selection-reload.md). Cold/device/ANR/native-publication and owner acceptance gates remain open.

D-106 / AT-111 removes unrelated ledger materialization from the unchanged Settings suggestion. Main suite: 1,512 passed, App.Tests 276. Final runtime/build/APK evidence: [quality/settings-suggestion-history.md](quality/settings-suggestion-history.md). Cold/device/ANR/platform and owner acceptance remain independent.

D-107 / AT-112 continues A11Y-03 with complete growing Windows selected/popup picker captions, inherited native
typography and 44-unit popup targets. Native reviews retain choices/drafts/full stored values without Save;
main suite remains 1,512 passed (App.Tests 276). Final native matrix, strict Windows/complete Android Debug+Release, exact original-data readbacks and signed APK pass: [quality/native-picker-captions.md](quality/native-picker-captions.md).
Other controls and actual OS/readers/device/iOS/release acceptance remain open. Continue ready work under D-69.

D-108 / AT-113 addresses actual clipped report-scope captions/off-page Clear actions and a zero-height transaction
result at large text. Final 21 Windows native contexts, main 1,512 tests (App.Tests 276), strict builds, actual
normal Release report/navigation/Clear, exact original-data readbacks and complete signed D-108 APK pass: [transaction-scope-readable.md](quality/transaction-scope-readable.md). A11Y-03 and independent
platform/OS/readers/device/owner/release gates remain open; continue ready work under D-69.

D-109 / AT-114 fixes observed inheritance of unrelated report scope when applying a named transaction filter.
Preserve the saved combination and complete financial data. Final six native contexts/18 independent report
restrictions, main 1,512 tests, strict builds, normal Release, exact original-data readbacks and signed APK pass:
[quality/saved-filter-report-scope.md](quality/saved-filter-report-scope.md). Provider/device/iOS and owner gates remain open.

D-110 / AT-115 avoids native rebuilding of completely unchanged transaction results while retaining every
ordered row, caption and boundary. Fresh data/display snapshots still publish. Main 1,521/App.Tests 285;
six native Windows contexts, strict builds, normal Release, original-data readbacks and signed APK pass:
[quality/unchanged-transaction-source.md](quality/unchanged-transaction-source.md).
Cold/ANR/physical-device/iOS/provider and owner/release gates remain open.

D-111 / approved OD-12 implements Repeat from a new transaction into an unsaved monthly Plan without posting
money. Preserve original complete drafts, named calendar dates, currency identities and safe defaults. Main 1,535/
App.Tests 299, final financial/presentation matrices, strict builds, normal signed Release Plan Save/cleanup,
exact original-data readbacks and complete signed APK pass:
[quality/entry-repeat-draft.md](quality/entry-repeat-draft.md). Provider/physical/iOS/owner/release gates remain open.

D-113 / AT-117 delivers complete growing transaction category choices, retaining original raw selection identity
without Save. Main 1,535/App.Tests 299, Windows 33-context/99-selection review, strict builds, normal signed
Release selection/discard and exact 24-table original-sample readbacks pass. Complete signed D-113 APK is ready:
[quality evidence](quality/entry-category-captions.md). Other A11Y-03/device/iOS/OS-reader/provider/owner gates stay open.

D-131 engineering evidence adds explicit Core resource choice and actual-file selected-account new money with
retained overdue/advance settlements. This is partial ENT-02/03, not activation or physical-device/iOS/provider/
security/billing/release acceptance. Current test builds remain unrestricted; see [evidence](quality/selected-account-write-policy.md).

D-133 proves atomic explicit Link and actual-file retained occurrence corrections with 39 added SQLite cases,
main 2,141 passing tests, strict platforms and six normal signed Release native confirmations. Current test limits
stay inactive. Other commercial writer/automation/feedback paths and all provider/device/iOS/security/billing/
publication gates remain open: [evidence](quality/occurrence-correction-writer.md).

D-134: 16 new SQLite cases/main 2,157 pass, strict Windows/Android, complete signed Release and six native normal
full/partial/final flows. No test-build commercial activation. Generated-entry reopening/automation/choice UI/
exception feedback and all external provider/device/iOS/security/billing/publication gates remain open.
[Evidence](quality/atomic-occurrence-payments.md).

D-135 continues ENT-02/03 with the reviewed settlement identity and actual entry relationship bound to the
same writer as generated deletion or manual unlinking. Reuse complete deletion/Undo; no second committed-state
write. Preserve partial money, overrides, refund/receipt ownership and suppression. 15 new cases/main 2,172 pass,
App.Tests 318; six normal Release native confirmations. Current registration stays inactive. Other selected-plan
generation/automation, choice persistence/UI and command exception feedback remain unfinished.
Strict Windows/Android and complete signed APK verified; external gates remain open. [Evidence](quality/atomic-occurrence-reopening.md).

D-136 completes exception feedback and shared consent/writer/publication gates for occurrence commands. 29 added application-flow cases; App.Tests 347 and main 2,201 pass with owned output-only Data.Tests serialization. Normal Release native failures preserve drafts and all complete rows. Negative parallel evidence is retained; D-137 later completes file-scoped cleanup isolation. Commercial registration remains inactive; selected-plan generation/automation, choice persistence/UI and external gates remain unfinished.
[Evidence](quality/occurrence-command-feedback.md).

D-137 closes the D-136 global SQLite cleanup follow-up: nine actual session/service cases; normal parallel full suite 2,210 passes, App.Tests 348. Strict Windows/Android, complete signed APK and normal Release native profile/restore/deletion verified. No commercial activation or external acceptance. [Evidence](quality/local-sqlite-pools.md).

D-138 continues ENT-02/03 with fresh automatic payment eligibility/values and explicit cached plan choice under the actual ledger writer. 31 new cases; normal parallel main 2,241 / App.Tests 348 pass. Strict builds, six normal Release native flows and complete signed APK verified. Enforcement remains inactive; generation/reminders, choice persistence/UI and other selected bindings/external gates remain open. [Evidence](quality/automatic-occurrence-writer.md).

D-139 continues ENT-02/03 with selected future occurrences in Home/Plans/detail and actual generic/full/partial
payment writers; history/overdue rights and original calculation inputs remain complete. 41 new cases; normal
parallel main 2,282 / App.Tests 348 pass. Strict builds, six normal Release native flows with all 24 complete rows
unchanged/restored and the complete signed APK are verified. Registration remains inactive. Actual reminder/snooze
delivery, choice persistence/UI, other bindings and external gates remain open. [Evidence](quality/selected-plan-occurrences.md).

D-140 binds actual Plan/contract notification delivery and snooze rebuilding to the opened file and current
explicit plan choice. Original group members, recurrence identities and effective dates survive snooze; current
privacy/language/text is rebuilt from stored rows. Unprovable legacy snooze caches are discarded while normal
reminders are rebuilt; legacy navigation remains readable. Notification taps resolve the current file after
unlocking, without profile guessing or money writes. Reserve the snooze id marker so a hashed occurrence id cannot
keep the original deadline. Registration remains inactive. Choice UI/persistence, other selected resources and
external acceptance remain open. [Evidence](quality/selected-plan-reminders.md).

## D-141 delivery boundary

Durable account/Plan selections and their actual writer/reminder bindings do not activate commercial limits.
Chooser UI and other resource bindings remain required before ENT-03 acceptance. Verify [current evidence](quality/durable-resource-choices.md)
and retain provider, physical-device, iOS and publication gates; a signed local APK or CI is not those approvals.

## D-142 revision-specific local evidence

Reviewed account/Plan form and registered Settings/Help/Back each have actual English/Persian/German, light/dark,
360/412/wide Windows evidence with process-local 1.5 layout stress. Six normal signed Release own-emulator cases
retain unrestricted access and complete financial rows; original owned files/sidecars and Main English/System are
restored. Canonical signed installable APK: artifacts/android/zanance-d142-release.apk. Main 2,399 / App.Tests 407
pass; strict Windows/Android zero warnings/errors. [Proof and APK digest](quality/active-item-chooser.md). This does
not activate commercial limits or satisfy physical-phone, iOS, provider, store-signing/publication or native OS
accessibility acceptance.

## D-143 revision-specific local evidence

Canonical installable signed APK: artifacts/android/zanance-d143-release.apk. Current commercial registration
remains inactive. Goal UI/projection/reminders and physical/iOS/provider/publication gates remain open.


31 new AT-145 real SQLite cases; normal parallel main 2,430 / App.Tests 407 pass, zero failures/skips (69.359 s).
Strict Windows/Android have zero warnings/errors. Normal signed Release actual goal/contribution/earmark Save and
exact owned-file restoration are verified on the owned emulator. No visible layout change. Complete signed APK
verified. [Evidence](quality/goal-resource-choices.md).

## D-144 revision-specific local evidence

Canonical installable signed APK: artifacts/android/zanance-d144-release.apk. Current commercial registration
remains inactive. Effective goal/contribution/reminder projection and physical/iOS/provider/publication gates remain open.


18 new AT-146 actual application cases; normal parallel main 2,448 / App.Tests 425 pass (71.008 s), no failures/skips.
Strict Windows/Android zero warnings/errors. Actual form and registered Settings/Help/Goals/Back each pass the
18-case English/Persian/German, light/dark, 360/412/wide matrix with process-local font stress. Six normal signed
Release own-emulator cases, exact original byte/display restoration and complete signed APK verified.
[Evidence](quality/goal-chooser.md).

## D-145 revision-specific local evidence

Canonical complete signed APK: artifacts/android/zanance-d145-release.apk.

36 new AT-147 actual application/real SQLite cases; focused 36 pass (9.147 s). Normal parallel main 2,484 /
App.Tests 461 pass (71.478 s), no failures/skips; test output cleaned. Strict Windows 15.49 s / Android 35.35 s,
zero warnings/errors. Six normal signed Release owned-emulator cases cover English/Persian/German, light/dark,
actual native stored single/group Intent routing, original destinations/Back, and rejection in another real profile.
The real pending queue retains all three original goals, bounded members, generic privacy and saved dates at 09:00.
No financial Save; complete original rows and exact owned bytes/sidecars plus Main/English/System restored.
This verifies stored native requests and actual Android Intent delivery, not future alarm firing at 09:00.
Complete signed installable APK verified. No layout/copy change; D-144 geometry evidence remains revision-specific.
Physical-phone, iOS, provider, OS accessibility, Store signing/publication and commercial activation remain unaccepted.

[Evidence](quality/goal-work-reminders.md). ENT-02/03 remain in progress.

## D-146 goal-view delivery

Original goal views, reviewed detail writes and historical negative releases are delivered under continuous authorization. Main 2,520 / App.Tests 497 pass; strict builds, Windows native matrices, six normal Release owned-emulator cases and complete signed APK verified. Phase 1 history remains implemented; commercial ENT-02/03 and external release gates remain open.

[Evidence](quality/goal-work-views.md).

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

## D-148 - Budget choice foundation

Main 2,594 / App.Tests 537, strict Windows/Android, native unrestricted budget Save/Delete and complete signed APK
verified. Reviewed budget chooser and effective view/alert integration are unfinished; current enforcement stays
inactive. Native emulator evidence is not physical-device/iOS/Store or commercial-release acceptance.
See [evidence](quality/budget-definition-choice.md).

## D-149 - Native budget chooser

Main 2,607 / App.Tests 550, strict builds, 36 actual Windows native cases, six normal signed Release emulator
cases and complete APK verified; exact owned data/display restored. Enforcement remains inactive and budget
view/alert integration remains unfinished. Emulator checks do not prove physical-device/iOS/Store acceptance.
See [evidence](quality/budget-definition-chooser.md).

## D-150 - Actual budget alert delivery

Main 2,632 / App.Tests 575, strict builds, six hidden Windows budget renders, six signed Release emulator cases
and complete installable APK verified; exact owned data/display restored. Registration remains inactive; scoped
budget returning-data and view/editor integration are unfinished. No physical/iOS/Store acceptance implied.
See [evidence](quality/budget-alert-work.md).

## D-151 - Original budget notification routing

Budget alerts now carry the opaque actual-file identity and original saved period id. Resolve after unlock and
again in the real budget page; reject another file, deleted identities and failed reads without guessing a
replacement. Retain original currency/calendar/period and monthly navigation coordinates without changing
Home currency or financial rows. Historical/unselected paid methods remain readable without granting new work.
14 new AT-153 cases; main 2,646 / App.Tests 589 pass. Strict Windows and complete strict Android Release pass;
six hidden Windows language/theme cases plus targeted native month navigation, and six normal signed Release
Android cases each tap four exact original currency/calendar/period destinations; owned data/display restored.
Commercial activation stays inactive. Budget view/editor commercial affordances and other ENT-02/03 paths remain
open. See [evidence](quality/budget-reminder-routing.md).

Complete signed phone artifact: artifacts/android/zanance-d151-release.apk. Physical-device/iOS/provider and publication gates stay open.

## D-152 - Original budget editor acceptance

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

See [evidence](quality/budget-editor-drafts.md).

### D-153 - Original budget action evidence

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

See [evidence](quality/budget-view-actions.md).


### D-154 - Original quick-resource evidence

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

See [evidence](quality/quick-resources.md).

### D-155 - Original Home template destination evidence

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

See [evidence](quality/home-template-destinations.md).

## D-156 - Original device profile review

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

See [evidence](quality/local-profile-work.md).

## D-157 - Actual-file original profile activity

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
[the evidence document](quality/profile-file-work.md). Commercial activation, physical-device, iOS, OAuth,
Store signing/publication and the remaining original-operation bindings remain open.

## D-158 - Original catalogue and period-review reminders

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
[evidence](quality/profile-reminder-work.md). Commercial activation, physical-device, iOS, OAuth and publication
remain separate. Original Plan/Goal financial-input retirement and other operation/native bindings remain open.

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

See [evidence](quality/reminder-input-work.md).

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

See [evidence](quality/commercial-feedback.md).

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

See [evidence](quality/pre-save-readiness.md).

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

See [evidence](quality/product-plan-comparison.md).

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

See [evidence](quality/editor-write-feedback.md).

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

See [evidence](quality/account-archive.md).

## D-165 - Account/Plan pre-attempt readiness

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

Remaining commercial bindings, activation and external/device acceptance stay open. See
[focused evidence](quality/editor-readiness.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) prove native disabled Save, complete reasons/refresh, retained input/read-refusal/retry
and all 25 unchanged original tables. Normal final signed Release Android verifies actual refresh and
Cancel with retained native fields, unchanged original rows, existing D-141 migration only and exact
owned byte/sidecar restoration. Final Release is installed/stopped. Canonical full Android build has
zero warnings/errors; all six changed production-source PDB checksums match. The installable APK is
artifacts/android/zanance-d165-release.apk (82589781 bytes); signature/structure evidence is recorded
in the focused quality document. No physical-phone, iOS, OAuth, billing or release acceptance is inferred.

## D-166 - Retained original import and Undo outcomes

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

Other commercial bindings and external acceptance remain open; see [evidence](quality/import-outcomes.md).

Actual native/package follow-through: 36 Windows contexts (en/fa/de, both themes, 360/412/wide,
Simple/Advanced) verify original native review/import/Undo, retained refusal and cancellation, complete
pinned reasons/results/targets, accepted subscriber-failure semantics and all 25 original tables restored.
Normal final Release Android verifies DocumentsUI picking, actual preview refresh, picker cancellation,
Import and consented Undo, unchanged original rows, existing D-141 migration only, exact owned bytes/
sidecars and Main/English/System restored. The new owned CSV is removed; final Release is installed/
stopped. APK: artifacts/android/zanance-d166-release.apk (82175915 bytes), signature/structure and all
seven production-source checksums verified. Physical-phone/iOS/OAuth/billing/activation stay separate.

D-167 local/native recovery and complete signed APK checks pass; this is not release acceptance.
See [restore-outcomes](quality/restore-outcomes.md) for 35 cases, main 3,045, strict builds, 36 Windows
contexts, actual normal Release Android source recovery, original fixture restoration and signature.
Physical-device/iOS/OAuth/billing/production certificate and encryption decisions remain open.

## Forecast/snapshot follow-through (D-169)

The original computation/atomic Save/history/Delete group is verified locally; retained Free data and
currency-safe legacy comparison are preserved. No commercial activation or external release acceptance.
Main 3,074 tests, strict builds, native Windows/Android evidence and complete signed installable APK:
artifacts/android/zanance-d169-release.apk. See [evidence](quality/forecast-work.md).
Next finite ready group: receipt recognition, followed by loan/report/PDF.

## Shared receipt follow-through (D-170)

Original Home/attachment recognition and typed editor source validation are delivered locally, with
original no-Save review and manual attachment policy intact. Main 3,104 tests, strict builds, 36 native
Windows contexts and both actual digital-PDF Release Android callers pass. Complete signed APK:
artifacts/android/zanance-d170-release.apk. [Evidence](quality/receipt-work.md).
Phone/iOS/camera/OCR corpus/provider/activation/release gates remain open. Next: loan/report/PDF.

## Original loan follow-through (D-171)

Loan analysis/confirmed payment writer are locally verified: main 3,141, strict clean builds, 36 native
Windows contexts and actual signed Release Android schedule/consent/ledger readback. Original balances,
manual repayment and essential warnings stay Free. Full signed APK: artifacts/android/zanance-d171-release.apk.
[Evidence](quality/loan-work.md). Next ready operation: reports/advanced PDF. Full release/external acceptance remains open.

## Original report/PDF local package (D-172)

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
Complete local test APK: artifacts/android/zanance-d172-release.apk. Signature and full assembly/AOT
structure checked with the existing local test certificate. The confirmed computation groups are
delivered locally, but bounded ENT-02/03 review and owner/external/full-release gates remain open.
[Evidence](quality/report-work.md).

## D-174 local Android widget evidence

Twelve native widget cases and original/cold intents checked; full signed installable APK verified.
TalkBack is installed but inactive; no physical-phone, system-font, iOS, OAuth, Store signing or release
acceptance. A11Y-01 remains open. [Evidence](quality/android-widget-accessibility.md).

## D-175 date naming evidence

Local native names/draft/calendar/no-write checks and full signed installable APK verified. Spoken
TalkBack/Narrator, system scaling, physical phone/iOS/provider and release acceptance remain open.
[Evidence](quality/date-context-accessibility.md).

## D-193 Android key evidence

Independent signed Release process creation/encrypted reopen/fifteen behaviors and native APK
payload/signature checks pass; software security level 0. Strict main builds pass. Main database
remains plaintext. Durable storage/recovery/crash migration/iOS and physical-device gates remain.
No whole-app encryption, OS backup or release acceptance inferred. Managed baseline not rerun.

## D-194 private envelope evidence

Independent stored-key process reopen/eight file behaviors exercise actual Android storage.
No injected publication crash, hardware power-loss, iOS or phone acceptance. Main remains plaintext;
final selector/journal/recovery/all-profile activation and OS policy coupling remain release gates.

## D-195 native journal restart evidence

Original transactional selector survives actual native process kills as expected on Windows and
independent signed Release Android; hot rollback header and committed selection checked. No
full-session crash/disk-pressure/hardware-power-loss, arm64/API24/16 KiB OS, phone/iOS or release
acceptance. Main provider remains plaintext and final multi-resource migration/recovery is open.

## D-196 original-model native migration evidence

Three real process-interruption/key-loss scenarios pass in independent signed Release Android,
using actual compiled model/32 migrations and typed financial content. Encrypted rollback/exact
plaintext cleanup verified; original main profiles untouched/plaintext. All-profile lifecycle,
provider packaging/activation, backup/attachments/safety/recovery/OS exclusion, Windows/iOS and
broader physical/power/disk crash gates remain. No whole-product security/release acceptance.

## D-197 protected EF access evidence

Original factory performs keyed native real-model read/write and stale/unkeyed/legacy-backup
refusal in separate signed Release Android package. Main provider/lifecycle/all-profile/portable
backup/recovery/OS exclusion adoption still open; no physical/iOS/whole-product acceptance.
Permanent legacy format guard is not a temporary restriction on current main backups.

## D-198 portable writer evidence

Protected actual-source portable/live typed comparison and first-chunk retirement pass independent
signed Release Android. Fixed output chunk, no plaintext temporary file, optional password retained.
Main/profile/provider/restore/recovery/all-profile/OS policy and device/iOS/full-product acceptance
remain. Do not publish partial output as a complete backup or claim current app encryption.

## D-199 old-backup staging proof

Original first schema imports under fresh local key and upgrades through all 32 migrations in
independent signed Release Android, preserving financial/profile packets and source bytes. No
live selection, safety/package rollback or main activation. Receiver dispatch/lifecycle/provider/
all-profile/recovery/OS policy, physical/iOS/wider crash acceptance remain release gates.

## D-200 restored selected restart

Actual encrypted rollback/distinct restored financial packets survive external termination and
same-run selected resume in independent signed Release Android. Timeout did not re-create input;
D-44 resume omission diagnosed/fixed. No package Completed/source rollback/retention/all-profile/
provider/main recovery/OS policy or physical/iOS/full-crash release acceptance.

D-201 whole-package receiver dispatch is available but unregistered in the app. Do not treat this
as durable all-source restore or encrypted database activation; native package journal/recovery,
portable-preference publication and application lease acceptance remain mandatory.

D-202 receiver/capsule does not activate main encryption. Require covered exclusive startup/profile
lease, durable catalogue/key reference publication, unfinished staging reconciliation, capsule replay
before unlock and final rollback retention/recovery acceptance before registration/release.

D-203 actual factory leases support maintenance exclusion. Still require covered startup/profile
coordination, background cancellation/reconciliation, trusted direct-native work ownership and exact
provider acceptance before activation. The gate alone is not encrypted database or device acceptance.

D-204 connects actual profile switch/migrations/settings to maintenance ownership. Native fixture
accept/refuse ports are not biometric/device acceptance; still require protected catalogue/provider
and covered startup integration, key/recovery/retention and physical/iOS acceptance.

D-205 real inactive deletion now owns maintenance. Do not treat this as final protected generation/
key/capsule/rollback cleanup or filesystem transaction acceptance. Current/main refusal and original
local file scope remain; protected deletion/retention lifecycle is still mandatory before release.

D-206 identity catalogue is private/non-portable and not registered in main startup. Initial ids
are not live selection authority. Require actual journal/key/file reopen, early partial reconciliation,
all-profile bootstrap/live references, native provider/security and recovery/retention before activation.

D-207 initial composition/retained key ownership does not activate main provider/startup. Require
live restored-generation resolution, all-profile registration, covered startup/access lifetime,
key/recovery/retention/OS-policy and platform acceptance before release.

D-208 retained restore history must be supplied by final main integration and consulted before initial
bootstrap fallback. Selected metadata is not package/preference durability or key/file authorization.
Main live resolver/provider/startup, replay/later edits and protected retention/recovery remain open.

D-209 callback/marker must be paired with a qualified real platform persistence adapter. A Task
placeholder, Set/Get readback or metadata flag is insufficient. Main adapter/startup/provider and
protected recovery/retention remain open; v1 independent capsules are not silently reset or migrated.

D-224 signed phone artifact: artifacts/android-d224-delivery/pro.vafadar.zanance-Signed.apk
(79.5 MiB), existing local test certificate, apksigner verification succeeds, package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; independent protected startup proof is not activation, device
acceptance, recovery completion or product release. No new screen layout was introduced.

D-225 phone artifact: artifacts/android-d225-delivery/pro.vafadar.zanance-Signed.apk (79.1 MiB),
existing local test certificate, apksigner verification succeeds. Actual package pro.vafadar.zanance,
API 24/36 and complete arm64-v8a/x86_64 assembly stores verified. Main storage remains plaintext;
this artifact does not establish encrypted-main, phone, iOS, recovery or release acceptance.

D-226 phone artifact: artifacts/android-d226-delivery/pro.vafadar.zanance-Signed.apk (79.5 MiB),
existing local test certificate; apksigner verification succeeds. Package pro.vafadar.zanance,
API 24/36 and complete arm64-v8a/x86_64 assembly stores verified. Main storage remains plaintext;
this is not encrypted-main, phone, iOS, recovery or release acceptance.

D-227 complete signed phone artifact: artifacts/android-d227-delivery/pro.vafadar.zanance-Signed.apk
(79.1 MiB), existing local test certificate; apksigner verification succeeds. Actual package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this is not encrypted-main, device, iOS or release acceptance.

D-228 complete signed phone artifact: artifacts/android-d228-delivery/pro.vafadar.zanance-Signed.apk
(79.5 MiB), existing local test certificate; apksigner verification succeeds. Actual package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this is not encrypted-main, device, iOS or release acceptance.

D-229 signed complete phone artifact: artifacts/android-d229-delivery/pro.vafadar.zanance-Signed.apk
(79.1 MiB), existing local test certificate; apksigner verification succeeds. Package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this does not establish encrypted-main, device/iOS or release.

D-230 signed complete phone artifact: artifacts/android-d230-delivery/pro.vafadar.zanance-Signed.apk
(79.5 MiB), existing local test certificate; apksigner verification succeeds. Actual package
pro.vafadar.zanance, API 24/36 and complete arm64-v8a/x86_64 assembly stores verified.
Main storage remains plaintext; this is not encrypted-main, device, iOS or release acceptance.

### D-263 — Android encrypted application delivery

- [x] Corrected 3509-test suite, zero failures/skips; test output cleaned.
- [x] Strict Windows and canonical complete Android Release builds without errors/warnings.
- [x] Signature v2/v3, permanent package, API24/36, non-debuggable and native engine exclusivity.
- [x] Patched core runtime13 native payloads and Xml/Pkcs13 package selection.
- [x] Original main encrypted startup/lifecycle/recovery/restore/safety and native continuation evidence.
- [x] Corrected 18 native language/theme/width contexts, growing captions and actual picker cancellation.
- [x] Exact fixture-key return and unchanged financial hashes; no review financial writes.
- [x] Packaged final Android OS cloud/device-transfer exclusions.
- [x] Canonical documentation and complete signed APK handoff prepared.
- [ ] Fresh hosted CI result for the final commit.
- [ ] Phone/arm64/API24/16KiB, spoken accessibility and OEM transfer acceptance.
- [ ] Windows/iOS encrypted main adoption and iOS signing/device acceptance.
- [ ] Real OAuth, Store signing and publication.

[Artifact and evidence](quality/android-protected-storage.md). Local signing/build and emulator checks
are not product publication; the full backlog and continuous-development objective remain open.

## D-264 Windows protected storage status

Windows main protected startup, DPAPI recovery enrollment/key-loss/cold reopen, isolated profile creation/switch/retirement, encrypted safety retention and explicit portable replacement have native application evidence on an independent fictional installation. Shared Android effects remain platform adapters to the same application algorithms. Final local verification: 3521 tests passed with zero failures/skips, strict Windows/Android builds without warnings/errors, complete signed APK and scoped emulator upgrade/cold reopening passed; see [Windows quality record](quality/windows-protected-storage.md). No iOS, physical-device, OAuth, Store or hosted fresh compiler acceptance is implied. The continuous-development objective remains open.

## D-265 holding accessibility and retired Home reads

Eighteen native contexts for three changed pages and eighteen distinct bottom contexts cover fa/en/de, both themes and 360/412/wide at process-local 200% typography; Cancel and original command/row identities retain financial data. The actual original Home read waits behind SQLite maintenance, retires with its Shell and completes without publishing into the old collection; replacement Home and complete stored rows verify. 3521 tests pass (zero failures/skips); native rendering is separately verified. [Bounded evidence](quality/holdings-readable.md). Final artifact/build details accompany delivery; physical-device, actual OS scaling/spoken screen readers and iOS remain open.


## D-266 device-only authentication requires success

The owner approved keeping the original lock cover when system authentication is unavailable on 2026-10-14. Only AuthenticationOutcome.Success authorizes device-only entry. Failed, unavailable, unknown and throwing responses preserve the original cover and queued destinations. The existing translated unavailable-device guidance explains restoring a device screen lock. PIN verification/recovery and the previously authenticated return grace remain separate and unchanged. No new stored data, permission, SDK or portable preference is introduced.

Actual Windows LockPage/native Invoke checks covered 18 language/theme/width contexts with an explicit authentication response port; a separate feedback pass checked the translated guidance. The independent Android package used the actual native adapter: NotAvailable after automatic prompting and Unlock, with Back unable to dismiss the cover and zero queued destinations opened. Neither review changes system credentials or native PIN storage. These checks do not accept configured biometrics, Windows Hello, physical devices or iOS.

An unrelated Home collection rendering COM exception remains observed during language/theme review. The lock assertions and unchanged financial fingerprint completed, but this is not a clean whole-application rendering result. SEC-04 remains in progress for its remaining platform and biometric transitions.

Final D-266 signed Release upgrade/cold Home passed on the owned emulator; test outputs cleaned. APK signature/payload and the open Home renderer finding are recorded in docs/quality/device-unlock-policy.md. This does not complete SEC-04 platform acceptance.


## D-267 complete Home balance publication

Observed Windows Home refresh failed in BindableLayout's ContentView handler disconnection while Balances.Clear published an empty intermediate list. Currency totals now use the existing SnapshotCollection complete replacement, preserving existing amount rows during ordinary and covered reloads. Currency calculations, inclusion policy, formatting and current-page retirement guards remain unchanged. The native regression checks original Home and original amount-handler identities, including a real modal cover; financial storage is not edited.


## D-268 sale-result help and truthful connection/account copy

The planned LOC-05 help sits beside an available realised holding result and explains recorded proceeds minus recorded average cost, unknown inputs and the absence of tax rules or official tax documents. The six existing languages include a concrete example. No tax calculation, financial posting, schema or new language is added.

LOC-10 corrects the browser-return message: it describes completion of the browser step and directs the user back to Zanance for the connection outcome, without claiming token exchange succeeded. English loan pay/receive confirmations identify the selected account explicitly, matching the original session argument and the other five translations.

Store drafts now name all six existing languages and describe optional password protection for local/connected backups, readable unprotected files, Android device-protected database keys and final system-backup/device-transfer exclusion. Real OAuth, physical-device, iOS and publication acceptance are explicitly separate; no store text was published.

## D-269 costed OD-02 backend proposal

Official provider prices and the 2026-10-15 PostgreSQL availability change inform three bounded USD scenarios and a complete first backend delivery/restore boundary. Arithmetic/overage checks pass; see docs/enhancements/2026-10-commercial-release/07-backend-cost-and-decision.md. Provider, region and test-environment budget remain owner choices. No backend, account, mail, hosted resource, SDK, app code or publication changes; ZCR-ID-01 and the full development objective remain incomplete. Application tests/build/native/APK evidence is unchanged D-268 and was not repeated.

## D-281 automatic device-encrypted local copies

LOC-09 is implemented locally under the owner's final device-key policy. Daily/weekly/off foreground
coordination, independent per-profile timestamps and automatic/pre-restore retention, six-language
status/help/discovery and explicit original protected restore are complete. No password storage,
readable automatic file, cloud fallback, sharing action, new crypto/provider/schema or permission.

Full strict suite: 3,590 passed; final focused Data publication case: one passed (3,591 covered
behaviors, no second full-suite claim). Strict Windows Debug and Android Release builds, 18 final Windows
contexts, native encryption/reference-aware retention, six Android changed-surface/help contexts,
original explicit restore and complete signed APK are recorded in quality/automatic-local-backup.md.
The initial Android live-picker publication loop and native validation lock issue were corrected;
only related checks were repeated. Local success is not hosted CI, physical-device/iOS/OEM,
power-loss, OAuth, store-signing or product-publication acceptance. The full goal remains open.

### D-293 service activation gate

- [x] Isolated current-machine non-root service, TLS role boundaries, full private-file restore and certificate renewal locally qualified.
- [x] Fictional production-promotion refused; public client uses system TLS and local client keeps explicit pins.
- [ ] Fresh production project, DNS/443 routing, real confirmation/recovery mail and external service acceptance.
- [ ] Actual hosted CI result and phone/iOS acceptance; local Windows/emulator evidence does not close these gates.
- [ ] Off-machine private service disaster recovery and reviewed compromise/trust-root migration procedure before live operation.

### D-294 local household engineering delivery

- [x] Original encrypted multi-client household, costs/conflicts, selective adoption, device/recovery/transfer flows locally qualified.
- [x] Main native Windows/Android privacy lifecycle, audit/revocation, full service snapshot/restore and final immutable image qualified.
- [x] Bounded original history, compact protected exports, actual Android main export and separate Release AOT/trimmed native allocation qualified.
- [x] Original transfer dialogs:18 language/theme/width cases plus only six affected RTL prompts; no financial/protocol mutation.
- [x]3753 existing cases pass by original full-run plus exact failed-case union; unrelated successes were not repeated.
- [x] Current complete Release APK signature/permanent id/arm64+x64 verified: artifacts/android-household-final-d294/pro.vafadar.zanance-Signed.apk.
- [ ] Real hosted CI result, public/commercial activation, external security review, physical-device/iOS/provider/store-signing and publication acceptance.

Exact scope, negative evidence and final artifacts: enhancements/2026-10-commercial-release/11-household-delivery.md.
Identity erasure, ciphertext recall,8GiB saturation and a performance threshold are not claimed.


## D-295 billing release gates (still unchecked)

- [ ] Implement/build/qualify the native StoreKit2 path in the verified Mac clone; iOS currently exposes no checkout.
- [ ] Configure genuine products,14-day trial, first-annual-year Lifetime discount and supported replacements in each store.
- [ ] Owner personally supplies provider/account/signing credentials and accepts store agreements; no credentials enter app binaries or repository.
- [ ] Complete the actual sandbox purchase/renewal/restore/refund/revocation/pending/hold/grace/cancel/expiry/recovery matrix.
- [ ] Qualify public notification authentication/routing, stable HTTPS origin, issuer/protection-key restoration and service operations.
- [ ] Qualify native SDK traffic/privacy, Data safety, physical devices and final signed store variants.
- [ ] Obtain separate commercial activation/publication acceptance. Local fixtures never enable quotas or a web checkout.

Current local Android/service evidence and complete test APK are in enhancements/2026-10-commercial-release/12-billing-delivery.md.


## D-296 Germany accountant-export qualification

- [x] Explicit country/year/account disclosure, opted-in stored copies, coherent original-source hashes and no-write acceptance covered locally.
- [x] Windows native en/fa/de, light/dark,360/412/1280 selection/review/Cancel:18 contexts, unchanged financial fingerprint.
- [x] Complete Release APK: permanent id, arm64+x64, ZIP integrity and local test v2/v3 signature verified.
- [ ] Physical-device recipient handoff and actual iOS build/share; native layout is not either acceptance.
- [ ] Expert-reviewed official country/year calculations before any Level 2 release. Level 1 makes no tax, GoBD, DATEV or filing acceptance claim.

Current source, safety bounds, readable-cache lifetime, actual Android observation and APK hash are in
commercial-release/13-accountant-export-delivery.md. Recipient copies and missing originals remain outside the app's control.


## D-297 bank CSV qualification

Keep the exact supported StarMoney Giro ordinary-cashflow variant, original structural-source evidence and
fictional fixtures distinct from customer bank acceptance.29 focused new cases,32 affected prior import cases
and23 resource checks are local evidence. Native contexts/package and negative evidence: enhancements/2026-10-commercial-release/14-bank-csv-delivery.md.
Do not list all German banks as supported or count CAMT/MT940/OFX/QIF/live connections as implemented. Physical
phone/iOS, current customer-export corpus and release acceptance remain open.

## D-298 Optional analysis gate

The local provider-neutral boundary does not select or enable a live model. Keep actual provider/privacy/retention,
licence/costs, unit packages/prices, trusted consumable verification, original provider idempotence/status contract,
public routing and genuine device/Mac checks open. No API-key workflow or subscription-implied credits. Default
service/provider remain closed and current commercial limits inactive. Manual native sharing is readable disclosure,
not integrated AI or a result guarantee. Local/native/package evidence is in15-ai-local-delivery.md.

## D-299 reference-source release gates

Keep a live provider, explicit source/coverage/redistribution licence, cost/entitlement policy and public route
activation gated. Local/native evidence cannot authorize them. New source format2 encrypted household content
requires updated readers; legacy format1 and financial schema hashes remain unchanged, not forward compatible
acceptance. Device-private cache is not a portable recovery promise; source history remains profile data.
Exact package/native/main-app evidence and open gates:16-market-reference-delivery.md. Physical devices, actual
arm64/API24/16KiB, iOS, store signing and release acceptance remain separate; quotas stay inactive.

## D-300 Startup evidence boundary

The measured Android cold-start profile is a build-only method input, with no shipping recorder or changed privacy/cryptography/access policy. Keep physical arm64/API24/16KiB, genuine phone timing, iOS, two-second target, general ANR freedom and release gates open.

Current exact evidence: enhancements/2026-10-commercial-release/17-startup-responsiveness-delivery.md.

## D-301 native maintenance gate

Use only the exact new source-built package entries after actual producer/compiler/profile qualification.
Required primitive/format/key behavior is preserved;508 Windows diagnostic occurrences still require their own
safety review. A reduced profile and passing stock suite are not blanket security acceptance. Retain physical
arm64/API24/16KiB, actual Mac/iOS, phone, genuine store signing and release gates. Exact consumer/native/main APK
and hosted status are in enhancements/2026-10-commercial-release/18-native-storage-profile-delivery.md.

## Currency control qualification (D-302)

Retain the local original-field/list matrix, strict builds and complete signed original Android upgrade/search/
selection/Cancel evidence in enhancements/2026-10-commercial-release/19-currency-accessibility-delivery.md.
Do not mark the general font-scaling, spoken-reader, physical-device, iOS or publication gates complete from it.

## D-303 complete ledger read without discarded identities

Home reads the entire ledger in physical row order, then applies the original stable newest-first date/time
order in memory. This avoids repeated encrypted-page visits through the date index without dropping history,
changing the operation-local cache, using SQLite temporary sort files or weakening encryption. The supported
compiled-model extension binds the existing-id LedgerEntry constructor, so materialization no longer creates
and discards a fresh identifier. New entries keep their original identifier generation. The compiled model is
regenerated; schema, financial rules, keys, backup and privacy policy are unchanged.

All 881 Data tests pass, including two new AT-303 complete-value/order cases. Actual Release/AOT Android
reference/tenfold Home in English/Persian/German retains every canonical financial presentation and complete
raw schema/header/integrity/typed rows on the same ciphertext and keys. Tenfold publication median decreases
from 14,828.37 to 7,418.54 ms; reference median from 3,066.82 to 2,252.00 ms. These are controlled loaded-emulator
observations, not two-second cold/phone/ANR/iOS acceptance. Windows native publication and complete portable
preservation also pass. Full-suite historical counts are not a new full run. Exact evidence and package:
[20-home-read-delivery.md](enhancements/2026-10-commercial-release/20-home-read-delivery.md).

## D-304 isolated billing HTTP fixture peers

The D-303 hosted backend qualifier failed because separate TestServer hosts shared a null remote peer and
consumed the same durable 30-request billing window. The same limited Linux sequence reproduces HTTP429;
standalone Windows/Linux cases pass. A test-only startup filter supplies a stable documentation peer per
fixture, without trusting headers or changing the production limiter. The previously failed original client
continues its retained pending command/authority. Separate Integration HTTP workers do not recreate a missing
ephemeral installation key. The exact failed retirement case and a new 30/31-request, independent-peer,
second-worker and next-window regression pass on the retained Linux fixture. Strict test builds pass;
production app/API code and the D-303 APK are unchanged. Its one-use hosted repair check later completed successfully for backend, CI and CodeQL.
Evidence: [21-billing-http-isolation.md](enhancements/2026-10-commercial-release/21-billing-http-isolation.md).


## D-305 complete typed Home materialization

Home retains complete 32-field ledger history and original order, transaction, conversion, cancellation and
file/rights boundaries while replacing per-row EF materialization with a guarded typed reader through EF's
original relational command pipeline. A mapping drift fails explicitly. Same-process Release/AOT tenfold
source-read medians are 2707.56 ms (original EF) and 2211.90 ms (typed), with complete materialized equality;
no stable reference improvement is claimed. Separate original Android Home publications in en/fa/de retain
all canonical financial presentation and full raw database preservation. Four new AT-305 boundary/value cases
and 24 distinct related Home/model cases are qualified across retained runs; no new whole-suite run is claimed.
Original hidden Windows Home and complete portable preservation also pass. The actual loaded main Android
startup method profile is refreshed, with development-only recording excluded from the ordinary candidate.
Evidence, package and remaining limits: [22-home-materialization-delivery.md](enhancements/2026-10-commercial-release/22-home-materialization-delivery.md).

QA-06 remains In progress for remaining composition, memory/cold/ANR, physical-phone and iOS acceptance.
The D-304 one-use hosted repair result is now completed/success for backend, CI and CodeQL; its timer is deleted.


## D-306 original reviewed inputs and detached goal lifecycle drafts

Original plan/goal reviews now reject changed in-memory model packets as well as changed database rows,
while retaining original file, choice and rights validation. Six Home mutations reproduce the missing
rejection before the fix; the goal allocation writer also refuses an altered source without money changes.
GoalDetail lifecycle actions now write a complete detached clone, preserving the original review through
confirmation and Save. Seven new regression cases and 179 distinct related Data / 169 original-source app
cases are qualified across retained runs; only the three affected failing app methods are repeated after
that caller correction. No new full-suite run or discarded prototype test count is claimed.

Twenty-four actual native goal actions/confirmations pass across en/fa/de and light/dark in a fresh Windows
fixture, preserving the unsaved amount and original ledger. The protected Release/AOT Android reference and
tenfold cases reject altered reviews, accept the same review after restoration, retain original native Home
presentation and complete raw data. Strict builds and the complete signed main APK pass; the original main
installation reaches unchanged Home after upgrade. A recurrence-cache trial is rejected for insufficient
benefit and completely removed; QA-06 and physical/iOS/release acceptance remain open. Exact evidence:
[23-reviewed-input-integrity-delivery.md](enhancements/2026-10-commercial-release/23-reviewed-input-integrity-delivery.md).

## D-307 target bank research boundary

- [x] First discovery pass covers all fourteen current target countries with primary references and explicit gaps.
- [x] Consumer/business/statement formats, failed retrievals and provider candidates remain distinguished.
- [ ] Exact revision/specimen and original importer acceptance for each additional bank preset.
- [ ] Separately accepted live-provider coverage, merchant role, consent, terms/costs and native/device acceptance.

This is documentation only. App, tests and signed APK remain D-306; no release/support claim follows from
[the research matrix](enhancements/2026-10-commercial-release/24-target-bank-research.md).

## D-308 complete saved-filter actions

- [x] Actual original normal-scale overflow reproduced in a fresh independently owned fixture.
- [x] Eighteen Windows large-text contexts qualify native targets, complete captions and unchanged query/data.
- [x] Six original-source Android contexts qualify 18 native saved-query clicks on the retained fixture.
- [x] Complete signed main APK, original upgrade/Home and real Transactions/filter-menu navigation pass.
- [ ] Actual OS-scale, spoken reader, physical phone and iOS acceptance remain separate.

Current Android/package outcome is recorded in [the native evidence](quality/saved-filter-actions-readable.md);
no product-release, commercial Windows or all-controls accessibility completion is inferred.

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
[protected schema connection lifetime](quality/protected-schema-connection-lifetime.md).

## D-310 verified purchase validity and renewal presentation

The original plan screen now exposes original owned-purchase lifecycle alongside offers: paid/grace ends,
last verification, observed auto-renewal, Lifetime and future scheduled changes. Historical device and
identity packets remain separate from current effective rights; cached/outage facts explicitly require
refresh. No next debit, price, early scheduled access or cancellation is inferred. No raw purchase/account/
receipt identifiers are shown. Seven new focused cases cover all six languages and original financial
preservation, including live retranslation without store reads. Native/build/package evidence and retained
failures: [verified purchase lifecycle](quality/verified-purchase-lifecycle.md). Genuine store, Mac/iOS, phone and release gates stay open.

## D-311 readable report and forecast tables

The original trend/history and forecast comparison/parts templates now separate identity/date from whole
monetary packets, reusing the existing native horizontal AmountReadout and overflow hint. Native original
German month/amount overflow is reproduced at 360 width. Eighteen Windows en/fa/de, light/dark, 360/412/1280
contexts at process-local 200% stress and six original-source Release/AOT Android contexts pass with original
stored data unchanged. Charts/PDF/calculations and schema are unchanged. No new unit-test count or unrelated
successful suite replay is claimed. Exact scope, retained failures and package:
[financial table readouts](quality/financial-table-readouts.md). A11Y-03, spoken readers, actual OS/device/iOS and release gates remain open.

### D-312 selectable encrypted safety copies

Retained pre-restore safety candidates are now selectable only within the current profile/installation,
separately from automatic/portable files. Listing reads no key/plaintext; original full authentication and
explicit replacement remain mandatory. Exact focused/native/physical receipts and limits:
[physical Android safety recovery](quality/physical-android-safety-recovery.md). This is not overall release acceptance.


## D-313 outbound gateway remains externally gated

Do not expose fictional deployments. Owner-created tunnel/DNS, real mail and processor/public acceptance are required before activation. APK remains D-312. See [outbound tunnel origin](quality/outbound-tunnel-origin.md).


## D-314 device performance is partial

Loaded original-source ARM64 Home/search/store-write and data-preservation receipts exist on the approved Samsung. Several reference Home samples exceed two seconds and tenfold Home/ledger is much slower. QA-06 and full cold/ANR/low-memory/platform acceptance remain open. No new main APK or release signature. See [physical protected workload](quality/physical-protected-workload.md).


## D-315 main physical upgrade and remaining performance gates

34 related tests pass with independent EF expectations, optional inclusive date bounds, complete values/order and cancellation. Strict Windows/Android builds and complete signed main upgrade pass. On the same retained Samsung ciphertext/keys, 100,000-entry public read goes 7,773 -> 4,313 ms and original native load/layout 10,446 -> 6,964 ms, with complete preservation. Exact/empty/wide search and original main 979 EUR history remain correct; successful unchanged financial writes were not replayed. No full-suite rerun/count is claimed. QA-06 remains In progress for remaining latency, cold/ANR/low-memory/device/platform acceptance. See [physical ledger read](quality/physical-ledger-read.md).


## D-316 bounded device acceptance

The unchanged signed D-315 APK now has actual Samsung main-app evidence for original setup, optional budget and balance goal, manual 4/6 partial/final settlement, future-plan/forecast exclusion/reset, process restart and return to the original Main profile. The two actual expense rows total 10, balance is 990, budget left 40; future 20 gives forecast 970 without a third posting. Native displayed-state acceptance is distinct from full database equality. All data is fictitious and retained; no system setting, reinstall or personal data change. No unchanged builds, unit suites or APK qualification were repeated. Broader recurrence/notification/platform/performance/release gates remain open. See [physical planned work](quality/physical-planned-work.md).


## D-317 bounded native file/import acceptance

Actual Samsung original file-picker cancellation/selection, own-format two-row preview/import, zero-new/two-known duplicate preview with disabled action and persisted Undo after process restart pass in the retained D-316 fictitious profile. Exact note/tag/literal formula-like text is retained; financial balance 990 -> 980 -> 990, original two payments, 40 budget left and 970 forecast are verified. Return to Main at 979 and hash-matched exact-file cleanup are complete. Unchanged app/APK and successful builds/tests remain D-315. No producer-export roundtrip, real-bank/aggregate/hostile-file/full raw equality or complete device/platform/release claim. See [physical CSV and Undo](quality/physical-csv-undo.md).

## D-318 first-plan candidate

The actual first-plan blank-list defect is fixed without persistence/financial changes. Independent original native Save on retained protected data and 18 final en/fa/de light/dark 360/412/1280 large-text layouts pass; standard Windows and complete signed Android builds have zero warnings/errors. Main upgrade without reset displays the retained unpaid plan before any segment toggle, handles empty Upcoming/return and preserves the displayed 979 EUR balance. No successful unrelated unit/device/crypto scenario is replayed or unit count increased. Broader device/platform/performance/release gates remain open. See [first plan publication](quality/first-plan-publication.md).

## D-319 actual loan lifecycle candidate

Both original physical debt/lent lifecycles now have native evidence: positive setup signs, complete 11- and 6-row estimates, cancellation without posting, principal transfers versus 10 expense / 5 income, explicit final repayments, six retained rows and cash 1,495 after signed cold upgrade. The observed zero-balance missing-terms hint is corrected; 38 focused native Windows cases and zero-warning Windows/complete Android builds pass. Main returns 979. Unit counts and successful unrelated checks are retained; broader device/platform/performance/release gates remain open. See [physical loan lifecycle](quality/physical-loan-lifecycle.md).

## D-320 same-data native compilation candidate

The independent phone caller now matches production assembly/profile inputs. A captured extra CoreLib
AOT-method selection reproduces native failures under load; removing only those additional roots while
preserving SDK defaults completes all 12 en/fa/de reference/tenfold Home cases and full schema/typed-row
preservation on the same retained ciphertext/keys. Align restored runtime/compiler/task servicing versions,
fix matching-configuration restore, keep only the explicit review window awake and retain scoped failures.
Windows and complete signed Android builds pass without warnings/errors; the main data-preserving upgrade
shows 979 EUR and actual expense/Cancel returns unchanged. No financial C# algorithm, schema, KDF/native
cipher or full-suite count changes. Current loaded observations remain 1.96-2.65s reference/8.83-10.29s tenfold;
this is stability qualification, not a general speed improvement, full cold/ANR matrix or release approval. See [native evidence](quality/physical-home-compilation.md).


## D-321 measured reader and main offline upgrade

D-321 reduces the measured complete 100,000-row reader from 4231 to 3344 ms and complete Home input read from 4653 to 3811 ms on retained Samsung ciphertext/keys. All 12 final en/fa/de native Home cases preserve complete financial presentation and raw schema/typed rows; loaded Home still takes 1.83-2.73s reference/ 8.47-9.58s tenfold. Related validation is 42 passing cases plus one final interceptor-reader case (nine added behaviors), not a full-suite rerun. Final strict Windows/complete signed offline Android builds and data-preserving main Home/All-history/return pass. QA-06, cold/ANR statistics and external platform/release gates remain open. See [evidence](quality/physical-home-row-reading.md).


## D-322 complete-read and signed offline upgrade

D-322 reduces the measured tenfold complete Home input read from 3811 to 3306 ms using exact-query null metadata, retaining all original 32 fields/conversions and safe interceptor fallback. All 12 final en/fa/de phone Home cases retain full financial presentation and raw schema/typed rows; loaded Home remains 1.92-2.83s reference / 7.23-9.51s tenfold. 45 related cases pass (two added behaviors), strict Windows/complete signed offline Android builds and main Home/All-history/return upgrade pass. A recurrence candidate without reliable native gain and temporary worker probes are fully withdrawn. No whole-suite rerun, two-second/cold/ANR/platform or release acceptance is claimed. See [evidence](quality/physical-home-null-bitmap.md).


## D-328 qualification boundary

Physical accountant default/no-document all-account and cash-only ZIP receiving/content checks pass on the approved Samsung. Main remains the D-327 candidate. This does not close tax specialist, notes/documents, full device, iOS, Store or release gates; see quality/physical-accountant-export.md.


## D-329 physical report PDF

Original en/fa/de exports and repeated financial context are qualified on Samsung; raw main-store equality, full device/reader, iOS/provider/Store and release acceptance remain open. See quality/physical-report-pagination.md.


## D-330 physical CSV boundary

Original own-format export/import/re-export/duplicate/cold Undo is qualified for the five-row Samsung fixture; real bank specimens, aggregate/unsafe Undo, broader device and iOS/Store/release acceptance remain open. Main APK stays D-329; see quality/physical-csv-roundtrip.md.

## D-331 interpreted preview boundary

The new interpreted-row UI and stale-mapping gate have focused managed and independent native review.
The complete offline candidate upgrades without reset; the new page's Samsung acceptance remains pending
while its adb transport is unavailable. This does not repeat or broaden D-330's original financial cycle.
See quality/interpreted-csv-preview.md for exact scope and retained failures; no publication gate closes.

## D-332 native arrival and remaining startup gate

The original Android preview and corrected Next/Previous are qualified on the x64/API36 emulator in
English at412x800, with no Import/financial Save and final Home0. The new offline signed candidate and
18 Windows arrival contexts pass. Samsung is absent; two actual ScheduledAlarmReceiver startup ANRs
remain unresolved and are not release acceptance. [Evidence](quality/android-import-preview-arrival.md).

## D-333 cold reminder boundary

Focused refusal/disposal and original process-cold Android action/persisted metadata are qualified locally.
Main offline APK is artifacts/android-cold-reminder-main-d333/pro.vafadar.zanance-Signed.apk, with
unchanged native crypto/runtime hashes and original emulator Home0 after upgrade. This does not close
startup ANR, interrupted-action/power-loss, phone, iOS, OAuth/provider/Store or release gates. See [evidence](quality/cold-protected-reminder-startup.md).


## D-334 rejected startup variants

The experimental APKs are not handoff candidates. Retain the qualified ordinary D-333 APK; assembly
compression-off crashes before managed entry, and direct native-library loading still produces a
startup ANR. No publication or performance gate closes. Temporary source/manifest changes are removed.
[Actual evidence and binary scope](quality/android-startup-packaging.md).


## D-335 menu text

Growing More title/explanation captions pass36 independent native Windows contexts across offline
and sync editions. The complete signed offline Android candidate is
artifacts/android-more-text-d335/pro.vafadar.zanance-Signed.apk. Actual package/navigation receipts
and remaining reader/phone/iOS/ANR/release gates are separate in [evidence](quality/more-menu-readable.md).


## D-336 package consistency gate

The observed pre-managed assembly-store abort comes from a cached CoreLib header retaining
index193 while the current descriptor table assigns CoreLib192 and Java.Interop193. Validate the
SDK's current compression-item metadata before its incremental timestamp check and regenerate only
incompatible owned cache entries. Seven focused behavior cases preserve valid cache timestamps,
invalidate stale index/size/magic/truncation, accept missing files and refuse outside destinations.
Keep the original SDK compression, serviced runtime, AOT profile, key/data and permissions. No unrelated
successful financial, OCR, backend or More-layout suite is repeated or new managed count claimed.
QA-06 startup ANR, two-second loaded Home, broader Android, Mac/iOS and owner release gates remain open.
[Evidence, failures and limits](quality/android-compression-cache.md).

## D-339 scoped report readability qualification

Category/composition report names, shares and monetary packets occupy independent growing rows.
Names retain native scaling and complete wrapping; oversized amounts use the existing horizontal
readout, outside the original category navigation button. The 18 final Windows contexts (en/fa/de,
Light/Dark, 360/412/wide at process-local 200%) and six original Release/AOT Android contexts pass
native glyph/viewport/scroll/target checks. Fictitious presentation inputs are explicitly separate
from report calculations; no financial commands execute. Strict builds and the full offline signed
APK pass. The Android caller's initial startup ANR is retained: Wait continued the same original PID,
without restarting its scenario. QA-06/ANR, broader A11Y/OS readers, physical Android, iOS and release
gates remain open. Managed test counts are unchanged; unrelated passed suites were not repeated.

[Evidence and limits](quality/report-legends-readable.md).

## D-340 scoped budget title qualification

The remaining inherited RowTitle ellipsis truncates the original Non-monthly bills caption at
200% Windows process-local scale/360 width. The shared default now wraps with its original 15-unit
font and semantic color. All 26 other current consumers already explicitly wrap, so their effective
behavior is unchanged. Eighteen final Windows contexts/36 native captions and six Release/AOT
Android contexts/12 captions pass; original account/entry/budget inputs stay unchanged and no method
is saved. Strict builds and a complete signed offline APK pass. The independent Android startup
ANR remains an actual failure; Wait continued the same PID 1753 to its completed caption result.
Actual OS text scale/TalkBack, broader device/iOS/performance and release gates remain open. No
system setting, financial algorithm, schema, translation or managed unit count changes.

[Evidence and limits](quality/budget-group-titles-readable.md).

## D-341 no performance promotion

The shared-connection prototype is not a release candidate or speed acceptance. Production and
the full signed APK stay D-340; the Data-test project receives its missing original startup source.
Physical Android, OS accessibility, cold/ANR/low-memory, Mac/iOS and owner release gates remain open.

[Evidence and limits](quality/home-publication-session.md).

## D-342 optional Home category candidate

Complete category identities and original amounts pass focused native qualification; hidden rows
avoid unnecessary native construction. Do not promote these geometry checks into OS/TalkBack,
phone, performance, iOS, Store or publication acceptance. No privacy data, permission, SDK, key,
financial schema or portable format changes. Existing owner/external gates remain open.

[Evidence and complete candidate](quality/home-category-legends-readable.md).

## D-343 scoped plan-row candidate

Shared plan-row monetary packets and navigation separation pass focused native qualification.
The complete offline candidate still needs the existing OS-reader, phone, performance/ANR, iOS,
provider/Store and publication gates. No privacy data, permission, SDK, key, schema or backup format changes.

[Evidence and limits](quality/plan-row-amounts-readable.md).

## D-344 scoped monetary-row candidate

Complete ledger/account packet viewports pass focused component qualification, with original actions,
selection and colors preserved. Windows Canvas root-change stability, Android startup/ANR, physical
devices, OS/spoken readers, iOS, providers, Store and publication remain unchecked/open. No native
observer, rejected root teardown, SDK downgrade/fork or security-policy change enters the candidate.

[Evidence and limitations](quality/ledger-account-amounts-readable.md).

## D-345 - Accessibility acceptance remains partial

Real emulator OS200% qualifies six original ledger/account surfaces at412dp,36 checks, with exact
captured settings restored. TalkBack speech/navigation is unaccepted; notification permission stays
denied and uncaptured flags are not claimed restored. Broader A11Y-03, phone, iOS, ANR and external
release gates remain open. App/APK unchanged D-344. [Evidence](quality/android-os-accessibility-qualification.md).

## D-346 native and spoken-reader gates remain open

Real OS scaling adds focused report/forecast/legend/budget evidence, with original data unchanged.
Observed Mono bridge SIGSEGV, serial-major-GC trial ANR and TalkBack silent client path remain
unaccepted. No runtime policy or experimental SDK enters Main. App/APK remain D-344; no phone,
iOS, spoken-reader, two-second startup, Windows Canvas stability or release claim.
[Evidence](quality/android-os-accessibility-qualification.md).

## D-347 observed-window fidelity and remaining runtime gate

Native geometry alone cannot pass accessibility when Android's ANR dialog covers the application.
Require real window focus and stronger receipt provenance. The precise-Tarjan candidate remains
ineffective after an actual mid-budget ANR and is removed; Main and its complete signed APK remain
D-344. Startup/ANR, broader accessibility, physical Android, iOS, providers and release stay open.
[Evidence](quality/android-os-accessibility-qualification.md).

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
[Evidence and limits](quality/home-date-materialization.md).

## D-351 scoped reimbursement correction

The shared Core batch removes repeated history scans while preserving original financial and overflow
semantics. All39 affected selected tests pass (15 new), strict Windows build passes, and the original
Release/AOT Android reimbursement page and three bounded comparison sizes pass on independent fictitious
inputs. No full-suite total, large-list rendering, whole-Home/two-second, cold/ANR, phone, TalkBack, iOS or
release acceptance is inferred. [Exact handoff and limits](quality/reimbursement-batch.md).

## D-352 scoped reimbursement-page candidate

The original page uses worker snapshot reads, one complete publication, bounded result rows and a
separate non-overlapping empty flow. Keep complete amount packets and original navigation independent.
Use actual original-source/native checks and visually reviewed own-window images; geometry without
actual vertical visibility or overlap refusal is insufficient. Managed/full-suite counts do not grow
from these native assertions. Root Home/ANR, two-second timing, physical/OS-reader, iOS, provider and
release acceptance remain open. [Current exact evidence and APK](quality/reimbursement-page.md).

## D-353 scoped Home template candidate

Complete template publication prevents unnecessary native teardown during Home reload. Native lifecycle
evidence is scoped to its actual environments; all broader GC/ANR, cold, phone/iOS, reader and release
gates remain open. [Current evidence](quality/home-template-publication.md).

## D-354 rejected performance candidate

Do not release the row-packet prototype: tenfold native reads regress despite complete data equality.
Production/App/APK remain D-353; retain nine restored-reader regressions and exact negative evidence.
No new performance/platform acceptance is claimed. [Evidence](quality/complete-history-row-packet.md).

## D-355 Android picker selected text

Grow complete native selected captions without changing selection, OS scale or keyboard authority.
Keep actual startup ANR and helper failures separate from successful geometry checks. Broader
Android/reader/iOS/release gates remain open. [Evidence](quality/android-settings-os-text.md).

## D-356 scoped original-editor caption candidate

Keep complete selected choices and debt disclosure captions without changing financial behavior.
Native metadata qualification is not spoken-reader acceptance. Preserve exact emulator restoration,
failed helper checks and original successful cases; keep ANR, wider device/iOS and release gates open.
[Qualification and signed candidate](quality/android-editor-os-text.md).
