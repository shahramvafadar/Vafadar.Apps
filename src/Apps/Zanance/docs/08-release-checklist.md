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
- [x] Store texts and screenshots in en/fa/de with fictitious data (REL-03/04); Spanish, French and Italian store texts are in the listing, their store screenshots are still to be taken – `docs/store/listing.md`, `docs/store/screenshots/<language>/` (from the Windows development build at phone size; replace with device screenshots if Play asks for a higher resolution)
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
