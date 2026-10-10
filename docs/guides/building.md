# Building

## Common commands

| Task | Command |
|---|---|
| Build everything | `dotnet build Vafadar.Apps.slnx` |
| Build one product | `dotnet build Vafadar.Zanance.slnf` |
| Build without MAUI (no workloads needed) | `dotnet build Vafadar.Tests.slnf` |
| MAUI projects for one platform only | `dotnet build Vafadar.Apps.slnx -p:VafadarMauiTargetFrameworks=net10.0-android` |
| Release build of an app for Android | `dotnet build src/Apps/Zanance/Vafadar.Zanance.App -c Release -f net10.0-android` |
| Build as CI does (warnings are errors) | add `-p:ContinuousIntegrationBuild=true` |
| Restore local tools | `dotnet tool restore` |

## Helper scripts

| Script | Purpose |
|---|---|
| `eng/scripts/Add-Strings.ps1 -JsonPath strings.json` | Adds or updates UI strings in all languages (en, fa, de, es, fr, it) of a `.resx` set in one step |
| `eng/scripts/Run-Snapshots.ps1 -Languages fa [-Theme dark] [-WindowSize 1280x820] [-Empty] [-Only budget,report] [-Mode simple] [-Help]` | Builds the Windows Debug app, starts it with an empty development database (the existing one is moved to `Data-before-snapshots-*`, never deleted) and saves screenshots of every screen to `artifacts/snapshots`; `-WindowSize` checks wide windows, `-Empty` the empty states of a new user, `-Only` shoots the screens whose names start with the given prefixes, `-Mode simple` walks through Simple mode, `-Help` opens every "?" help dialog and saves it (`<language>-help-<topic>-popup1.png`) instead of the screens |
| `eng/scripts/Build-AndroidApk.ps1 [-Configuration Debug]` | Builds a complete Android APK for installing on a phone directly (`artifacts/android`); see below |
| `eng/scripts/Get-SigningInfo.ps1 [-Path <apk/aab>] [-Keystore <file> -Alias <alias>]` | Prints package name, version, SHA-1/SHA-256 fingerprints and the Entra signature hash for Google Cloud, Entra and Play Console |

The Windows Debug build also reads these environment variables (Debug builds only):

| Variable | Effect |
|---|---|
| `VAFADAR_START_ROUTE` | Opens a screen after the start, e.g. `//insights/budget` or `//plans;plan` (steps separated by `;`); the app stays open |
| `VAFADAR_WINDOW_SIZE` | The window size for the start route or the snapshots, e.g. `1280x820` (default `412x892`, a phone) |
| `VAFADAR_CAPTURE_WINDOW` | With `VAFADAR_START_ROUTE`: saves the whole window – title, navigation and page – as a PNG to this path, rendered by the app itself, so it also works while other windows cover the app; open dialogs are saved next to it (`<file>-popup1.png`, ...) |
| `VAFADAR_CAPTURE_DELAY` | Seconds before that capture (default 2), e.g. to open a dialog with UI Automation first |

### Installing on an Android phone without Visual Studio

Every Android phone-test handoff includes a freshly built complete signed APK, its actual file path and verified
package/signature (owner rule, D-66). Build Release with `eng/scripts/Build-AndroidApk.ps1`; set the process
environment property `ContinuousIntegrationBuild=true` for the same warning policy as CI. Do not substitute an
ordinary Debug/Fast Deployment artifact. APK creation and signature verification do not prove installation or
physical-device acceptance; the owner installs and tests the supplied file.

The APK in `bin/Debug/net10.0-android` is made for **Fast Deployment**: Visual Studio copies the app's assemblies to the
device separately, so that APK alone stops at start (logcat: *No assemblies found … Fast Deployment*). For a phone or
for testers, build a complete APK with `eng/scripts/Build-AndroidApk.ps1` (Release by default, signed with the local
debug key) and install `artifacts/android/pro.vafadar.zanance-Signed.apk`. Android blocks screenshots of the app on purpose
(`FLAG_SECURE`, D-23); use the accessibility tree (`adb shell uiautomator dump`) to check screens on an emulator.

**Visual Studio open at the same time:** it restores the solution in the background for all target frameworks, which can
overwrite the restore of a command-line build for one framework. The build then stops with `NETSDK1005` (*assets file
doesn't have a target for 'net10.0-android'*) or `APT2126` (*file not found*). Run the command again – it restores
first – or close the solution in Visual Studio while building from the command line.

## Target frameworks

* Plain libraries, tests and (future) web projects: `$(VafadarTargetFramework)` = `net10.0`.
* MAUI projects: `$(VafadarMauiTargetFrameworks)`, which depends on the build machine:

  | Build OS | MAUI targets |
  |---|---|
  | Windows | `net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0` |
  | macOS | `net10.0-android`, `net10.0-ios` |
  | Linux | `net10.0-android` |

  Passing `-p:VafadarMauiTargetFrameworks=...` on the command line overrides this for all projects – useful for
  faster local builds and used by CI.

## Warnings

The code builds without warnings. Locally warnings are shown; in CI (`ContinuousIntegrationBuild=true`, set
automatically on GitHub Actions) they are errors. Code style rules marked `warning` in `.editorconfig` are enforced
during the build.

## Continuous integration

`.github/workflows/ci.yml` runs on every push to `main` and on pull requests:

| Job | Runner | What it does |
|---|---|---|
| Libraries and tests | Ubuntu | Builds `Vafadar.Tests.slnf` (Release) and runs all tests with coverage and TRX results |
| Android build | Ubuntu | Installs the Android workload and builds the whole solution for Android (Release, trimmed) |
| Windows build | Windows | Builds the whole solution for Windows (Release) |
| iOS build | macOS | Only on manual runs with "ios" checked; simulator build, no signing |

Other workflows:

* `codeql.yml` – security analysis of C# and workflow files (weekly and on changes).
* `release-android.yml` – manual: builds a signed `.aab` for Google Play (see [release guide](release-and-publishing.md)).
* Dependabot (`.github/dependabot.yml`) opens weekly update pull requests for NuGet packages, the SDK and actions.

### First-run snapshot coverage (D-62)

The normal snapshot walk-through captures all three onboarding steps in each requested language, including the
ends of longer forms, and the restore page before an account exists. Back returns to the same draft, then the
walk-through completes onboarding and continues to the requested routes. Sample data is fictitious throughout.

D-63: `Run-Snapshots.ps1 -Only settings,about,notices` additionally captures the security card, PIN setup/validation/
change/unlock and the two privacy help dialogs. It creates only a fictitious PIN and removes it in finally; it refuses
to modify a pre-existing development PIN. Run en/fa/de in both themes at 360/412 px and wide. Snapshots do not prove
Android secure flags, device-authenticated recovery, iOS keychain or physical-device acceptance.

D-64: `Run-Snapshots.ps1 -Only receipt` captures found, conflicting, damaged, missing and currency-conflicting totals,
plus a reread without a new amount. It checks fictitious draft state, manual choice without saving, preservation of
the old amount and blocking a changed display unit before Save. Run en/fa/de, both themes, 360/412 px and wide. These
layout/draft checks do not replace real file-picker/image recognition or physical Android/iOS camera acceptance.

### Plan/debt review fixtures (D-65)

Run-Snapshots accepts `plan-debt-reminder`, `debt-new` and `receivable-new` alongside `plan-new`, `plan-edit`,
`loan-edit`, `loan-detail` and `accounts`. Plan/debt fixtures capture schedule/count, calendar/short-month and optional
term states and check unsaved draft invariants. Run Simple and Advanced, en/fa/de, light/dark at 360/412/wide.
Only fictitious data is seeded; the script preserves a non-fixture development database before the run.
Background app launches use hidden windows and the app captures its own rendered window.

### Regional and cloud-list review (D-67)

`Run-Snapshots.ps1 -Languages 'en,fa,de' -Calendar Gregorian -Only regional-settings,backup` captures the independent
German formatting/holiday choices and fictitious connected-cloud empty/error/files states, plus onboarding. Repeat
light/dark at 360x800, 412x892 and 1280x820. Destination fixtures use the same file-to-row application path as live
listing and assert discovered rows become visible. They never sign in, upload, restore or delete cloud files.
Prior portable choices and backup protection preference are restored after the run. The launcher requires a strict
Windows build and stops on failure; it never launches a stale executable. These checks do not validate real OAuth
clients or signing-certificate registration.


## Independent encryption feasibility

Use [experiments/Zanance.Encryption](../../experiments/Zanance.Encryption/README.md) explicitly for SEC-01 proof runs.
It is outside the main solution, references no app and writes fictitious data only. Never add its deprecated native
bundle to production references. Android fixture id: pro.vafadar.zanance.encryptionproof. Emulator/tool downloads
need owner approval; never use the main finance package as a fixture or infer runtime success from APK compilation.


## Offline Android variant and permission review

`Build-AndroidApk.ps1 -Offline -Output artifacts/android-offline` explicitly overrides all cloud client properties
with empty global values, preserving the local secrets file and license registration. Release only: Debug requires
debugger network access. The APK script enforces CI warnings-as-errors even without an environment variable.
Run `eng/tests/AndroidPrivacy.Tests.ps1`, then `eng/scripts/Test-AndroidPrivacy.ps1 -Path <complete.apk> -Variant Offline`
or `-Variant Cloud` for a genuinely cloud-configured Release build. The guard inspects the APK's declared permissions,
rejects Debug and unknown capabilities, and prints no secrets. See [SDK review](../privacy/zanance-sdk-review.md).

## Android goal notification fixture (D-70 / AT-77)

On a fresh isolated emulator installation only, a complete Debug APK with an AndroidEnvironment item containing
VAFADAR_GOAL_REMINDER_PROOF=1 runs the opt-in DebugGoalReminders fixture. It refuses accounts/goals outside its
own marked identities or any ledger data. It creates one fictitious balance goal, asks through the system permission
dialog, and verifies native pending local 09:00 requests before displaying one immediately for a notification-tap
check. No system clock/settings changes. Read only its files/goal-reminder-proof.json with adb run-as on that isolated
Debug package; use uiautomator for the screen because FLAG_SECURE stays enabled. UI pause/resume/edit opt-out plus
force-stop/restart checks pending cancellation and persistence. The fixture and snapshot types are compiled out of
Release; inspect Release metadata, then build the complete signed handoff APK without the fixture environment item.
This proves the real scheduler/delivery/tap path, not exact-time, Doze, reboot or physical-device/iOS acceptance.

## Android review notification fixture (D-71 / AT-78)

VAFADAR_REVIEW_REMINDER_PROOF=1 in an AndroidEnvironment item on a complete Debug APK enables DebugReviewReminders.
Use an isolated installation only. It refuses data outside its own marker or the earlier goal-reminder fixture,
and refuses any ledger data. One fictitious account has an earlier opening date and a financial month start matching
today (clamped to 28); the profile reminder is enabled once. It checks native pending 09:00 requests and unchanged
review/ledger state, then displays one immediately for a tap check without changing system time or settings.
Read only files/review-reminder-proof.json with adb run-as for that fixture. UI opt-out and process restart must leave
zero pending review requests. Subsequent starts preserve the UI choice. The diagnostic is absent from Release;
remove the fixture environment import before building and inspecting the complete signed handoff APK.

## Android aggregate import fixture (D-72 / AT-79)

VAFADAR_IMPORT_LINK_PROOF=1 in an AndroidEnvironment item on a complete Debug APK enables DebugImportLinks. Use only
the isolated marked reminder/import installation. It rejects other account/ledger/goal/holding/plan data, seeds its
own category catalogue if onboarding was bypassed, and prepares an unsaved six-row CSV preview against a 412 EUR
September aggregate. Subsequent starts reuse stable IDs and the saved aggregate; they never recreate it. Read only
files/import-link-proof.json for this fixture. Use uiautomator and native taps for the real choice/import actions;
FLAG_SECURE remains intact. Restart must skip the six imported IDs and show durable history. Build the complete
Release APK without the fixture environment item, install over the fixture and confirm Undo via UI. If database
inspection is needed, reinstall the known Debug package without launching it and read only the fictitious database.
The import fixture, preview helper and snapshot code are absent from Release. The Windows import-overlap snapshot
route shows pending/link/keep-both and help without saving the import. Preserve development databases before it and
verify restoration hashes afterward. These checks do not close physical ARM64/iOS or production acceptance.

D-74 adds entry-asset-income and entry-asset-expense to the Debug snapshot route list. Use
Run-Snapshots.ps1 -Languages 'en,fa,de' -Only entry-asset -Theme dark -WindowSize 360x800 to render the actual warning.
The fixture creates only a fictitious value account, invokes the actual Save command, captures the app window/dialog,
then cancels with the native UI Automation Invoke pattern. It asserts unchanged draft/ledger/value and never sends
desktop clicks, keys or focus requests. Native Android checks retain FLAG_SECURE and use accessibility XML.

## Performance inspection (D-75)

eng/benchmarks/Zanance.Performance is an independent .NET executable using actual Core/Data projects. It is not a
MAUI dependency or production diagnostic. Build it with the CI warning policy in Release and supply a new output
directory under artifacts. See its README for generation, migration and operation timer boundaries. Native checks
use separately created fictitious profiles, preserve Windows development files with matching hashes, explicitly
select only the emulator serial, retain screenshot protection and report failed observations/ANR honestly.
The complete signed APK requirement remains in force before physical phone testing.

D-76 extends the existing transactions Debug snapshot route with loading, read failure and successful reload states.
Run-Snapshots.ps1 -Languages 'en,fa,de' -Only transactions -Theme dark -WindowSize 360x800 captures these along with
the existing list/bulk/filter states. The read failure is fictitious, handled in the walkthrough and never writes
ledger data. On Windows the actual named retry button's native Invoke pattern triggers the page handler; readiness
must follow within the diagnostic deadline. Preserve/restore development database files with matching hashes. The fixture is absent from Release;
Release loading checks use the separately owned reference/tenfold emulator profiles through native accessibility.

## Large-text layout inspection (D-77)

Run-Snapshots.ps1 -Languages 'en,fa,de' -Only home,plans -FontScale 2 -Theme dark -WindowSize 360x800 uses an explicit
Debug process-local Windows font-manager replacement. Repeat at 412x892/1280x820 and both themes, and compare a
-FontScale 1 baseline. font-scale.json records the requested/unchanged real OS factor; *-quick-actions.json records
actual native text/target geometry. No global Windows setting is changed, so this is not native OS scaling acceptance.
Preserve/restore owned development database files and verify original hashes around the walkthrough.

Android review must use only the explicitly selected emulator and owned fictitious data, retain FLAG_SECURE and
avoid global settings changes. D-77's temporary activity-context prototype recorded 200% native text conversion for
fixed English/German content, but also pinned locale. Its font-only-delta trial left startup covered with no native
text, so the prototypes were removed from source before delivery. They do not establish live-language/RTL/native OS
acceptance and are not a supported app startup path. The normal complete Release must start and remain usable;
physical 200% system-text testing and native scaled-layout completion remain separate gates.

## Persistent-action layout review (D-78)

Use the existing snapshot route list: -Only home,transactions,accounts,plans,holdings,goals,categories with the
D-77 font/size/theme options. *-layout-checks.json measures actual action/viewport and realized financial-row
geometry. Visible parent chains are required: hidden Home sections can retain unmeasured native handlers. The
own-window *-window.png includes navigation and persistent actions, supplementing the existing page rendering.
Transactions also captures *-undo-preview without deleting a fixture entry or invoking Undo; visibility is restored
in finally. This proves layout, not the deletion/recovery workflow. Geometry collectors and own-window rendering
are Windows Debug only; the preview is Debug only. Preserve/restore original development data files and verify hashes.

## Settings loading/display review (D-79)

Use Run-Snapshots.ps1 -Languages 'en,fa,de' -Only settings-display -FontScale 2 -Theme dark -WindowSize 360x800;
repeat at 412x892/1280x820, both themes and a -FontScale 1 baseline. This separate route avoids the existing settings
security/PIN walkthrough. Hold the read, inject a handled failure, invoke the actual RetrySettingsButton through its
native UI Automation pattern and exercise the actual view-model's live language choices. Assert the editable form
is covered until publication, indices/draft input survive, and stored preference/entry JSON is unchanged. Supplemental
*-window.png renders the own window; *-settings-display-proof.json records the checks. Preserve/restore original
development files and verify hashes. The fixture is absent from Release; native checks use only the owned emulator,
unchanged system settings, screenshot protection and a complete signed APK. Never substitute this for phone/OS QA.

## Growing-action layout review (D-80)

Use -Only settings-display,loan-detail,account-detail,transactions with the existing D-77 font/theme/window options.
settings-display avoids the PIN walkthrough. *-layout-checks.json records actual growing captions, native sizes,
spoken names and unchanged command/argument bindings. Transactions captures no-selection/selected states and
invokes the real native Select all/Cancel without writing entries. Account detail adds own-window rendering.
Preserve original development files before starting and verify matching-hash restoration in finally. Wait for a
native layout pass after changing selection; capturing immediately can misclassify stale dock geometry as overlap.
The fixture/collector is Debug only. Keep system settings and FLAG_SECURE; use the full signed Release for native QA.

Use -Only loan-actions,settings-actions for intermediate command captures: each currently visible growing action
is scrolled into view and rendered without executing its command. This is separate from transaction All/Cancel
and the Settings retry fixture. Both routes use only existing fictitious snapshot profiles.

## Date/amount layout review (D-81)

The date-inputs Debug route opens the existing fictitious account reconciliation draft. It checks full digit
geometry, a valid date, a partial year, Gregorian/Persian/Hijri redraw and unchanged entry JSON without Save.
Programmatic draft input is recorded separately from actual external UI Automation ValuePattern input.
The other affected account/Home/plan/report/asset routes exercise AmountReadout. *-amount-scroll-proof.json records
actual native Scroll-pattern offsets at both ends; *-layout-checks.json records actual native glyph boundaries and
viewport geometry. Failed layout checks retain an own-window image and failure JSON, then still fail the run.
Use the existing three-language, both-theme, 360/412/wide and process-local 200%/100% options. Preserve original
development files first and verify their hashes after restoration. No system settings, FLAG_SECURE or stored money
are changed; Release excludes these collectors. Complete signed APK and physical-device acceptance stay separate.


## Reopened Settings review (D-82)

Use -Only settings-reopened with the existing en/fa/de, light/dark, 360/412/wide and process-local scale options.
The Debug-only route selects the actual realized native language Picker through SelectionItem, invokes the real
header Back, waits for the normal deferred rebuild and reopens Settings. It keeps retired shells strongly alive,
checks frozen old titles and live new captions, and preserves fictitious unsaved draft/choice values.
*-settings-reopened-proof.json records complete stored preference/account/entry equality; a failure reports changed
preference field names without values. Wait for replacement rows to lay out before measuring their geometry.
Preserve/restore original development files with matching hashes. No financial Save, PIN or permission operation.
Full Release emulator checks and the complete signed phone-test APK remain separate from this Windows route.

## Growing header review (D-83)

Use -Only headers,settings-reopened,notices,loan-schedule plus other non-modal child routes with existing three-
language/theme/width/font-scale options. The headers Debug route resizes only the app's own window, retains the
actual Settings body/bindings/draft during resize, checks idempotent attachment after a nested return and invokes
native Back. *-headers-proof.json compares complete stored data; *-layout-checks.json contains realized title glyph
geometry and native Back peer names. Modal editor headers and Insights tabs retain separate review boundaries.
Preserve/restore original development files and hashes. Android checks use normal full signed Release, the owned
emulator, native hierarchy/real Back and no screenshot/security/system-setting override.

## Settings estimate draft regression (D-84)

Use -Only headers with the existing language/theme/width/font-scale options. The actual Settings page keeps its
unsaved estimate through native own-window resizing and a nested Categories return; *-headers-proof.json adds
RetainedDraftAfterNestedReturn alongside complete stored-data equality. This replaces D-83's restored-before-return
fixture boundary. Preserve original development files/hashes. Emulator review may open and immediately close an
existing modal without entering credentials or invoking Save; full signed Release and device acceptance stay separate.

The `insights-tabs` Debug route (D-85 / AT-92) invokes the four actual Windows native targets, measures complete
caption/selection geometry, checks idempotent body retention and resizes only the owned window. Compare full
settings/accounts/entries JSON without financial Save. Centered WinUI character rectangles are checked against
LayoutInformation.GetLayoutSlot and the actual caption viewport, alongside trim and minimum-target checks.

The `budget-readouts` Debug route (D-86 / AT-93) retains the actual Budget page, substitutes only in-memory
presentation collections for full signed-boundary/default/compact typography and restores them afterward.
It checks actual native glyph geometry, horizontal Scroll endpoints and complete stored accounts/entries/settings/
budgets/plans equality. Use en/fa/de, light/dark and 360/412/wide process-local font stress, preserving original
development database/sidecar/marker files. Retain negative route/layout evidence; no Save or OS/security change.
See `src/Apps/Zanance/docs/quality/budget-readouts.md` for the independent formatter cases and final package proof.

Requested Windows snapshot runs hide their own native AppWindow and render its root directly: PowerShell
WindowStyle.Hidden alone does not hide WinUI. Never activate the review window or send desktop input.
Retired pages fail explicitly; do not wait on their stale native scroll controls or count interrupted captures.

The `budget-periods` Debug route (D-87 / AT-94) invokes all three actual native budget choices and joins their
existing serialized reloads. Check every full caption/target/selected name inside the growing group, restore
the original selection and compare complete stored accounts/entries/settings/budgets/plans without Save.
Use the same three-language/light-dark/360-412-wide process-local font review; keep negative failures and
original development files. Evidence: `src/Apps/Zanance/docs/quality/budget-periods.md`.

The `entry-tags` Debug route (D-89 / AT-95) checks four admissible fictitious tag captions on the actual editor.
Invoke real native peers, pass the unchanged raw value, retain full direction-safe spoken captions, minimum 44 px
targets and complete caption height within the bounded growing group. Reacquire controls after regeneration;
capture the checked caption before Invoke. Restore the exact original draft/suggestions/details/dirty state and
compare full stored Accounts/Entries/Settings without Save. Include loan/settings routes when reviewing the shared
Suggestion appearance. Evidence and the dated local PowerShell-host startup limitation are recorded in
`src/Apps/Zanance/docs/quality/tag-suggestions.md`; the normal APK script remains the standard build path.

The `entry-details` Debug route (D-90 / AT-96) checks existing Simple/Advanced initial visibility and both actual
native hide/show actions. Read the editor's actual draft fingerprint; retain typed fictitious values, complete
captions/spoken names, one visible action and bounded >=44 px targets. Restore exact original draft/dirty state,
suggestions and visibility and compare complete stored rows without Save. Include narrow baseline/Simple cohorts.
Canonical scripts completed with the existing installed PowerShell engine for this slice; no system/security setting
change was necessary. Evidence and independent platform gates: `src/Apps/Zanance/docs/quality/entry-details-disclosure.md`.

`Run-Snapshots.ps1 -Only onboarding-actions` (D-91 / AT-97) measures both actual restore alternatives, invokes native
Restore/Back and compares the existing account draft fingerprint, selected wizard choices and full stored rows.
The focused review returns before general snapshot account creation/financial seeding. Keep the outer development
data guard and en/fa/de theme/width/text-scale matrix; a helper's script-header failure is not final acceptance.
Evidence: `src/Apps/Zanance/docs/quality/onboarding-restore-actions.md`.

The named Home Debug snapshot route (D-92 / AT-98) checks the current page BindingContext, existing native account
row identities, fresh exact bindings, one complete Reset and full stored rows on reload. It writes a metadata-only
Home snapshot proof. Keep the development-file guard and final language/theme/width/scale matrix. Temporary timing
instrumentation is removed before final builds; a different transient VM is not evidence of actual UI retention.

After changing Debug diagnostics, also build the complete Android Debug package with
`./eng/scripts/Build-AndroidApk.ps1 -Configuration Debug`. A successful Release build excludes Debug source and cannot
prove that target compiles. Windows-only helpers and their callers must share the same WINDOWS boundary (D-93).

Customize Home review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only home-layout -FontScale 2 -Theme dark -WindowSize 360x800`.
AT-99 checks real full-width native names/44 px targets and invokes Down, Up, visibility Toggle and Reset against only
the walk-through's fictitious profile. Restore original development files after review; process-local Windows text
scaling is not evidence of real OS text scaling or Android/iOS screen readers.

Home visibility review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only home -FontScale 2 -Theme dark -WindowSize 360x800`.
AT-100 invokes real native customization switches, account detail/list and Back on fictitious data, restores the
original layout and compares complete data. AT-98 now records visible native retention separately from a hidden
complete snapshot with zero views. Preserve development files and wait for real arrangement before captures.

Account description review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only home,accounts -FontScale 2 -Theme dark -WindowSize 360x800`.
AT-101 varies all eight existing status-flag combinations on fictitious presentation values, checks actual native
glyphs against native and MAUI bounds, retains native rows and restores exact complete data without Save. Preserve
development files; a native text slot or IsTextTrimmed alone does not detect a shorter enclosing MAUI allocation.

Debt entry review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only accounts -FontScale 2 -Theme light -WindowSize 360x800`.
AT-102 checks the real debt command caption/target, UIA opens/cancels the existing unsaved Loan draft and compares
full stored data including schedules. Wait for actual modal date input arrangement and native-scroll the reference
date before capture. Keep all digit-width assertions and preserve original development files; do not Save.

Modal header review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only 'accounts,modal-headers' -FontScale 2 -Theme light -WindowSize 360x800`.
AT-103 opens thirteen actual modal cases with their real query inputs, measures complete actual native glyphs,
peer name and title/close geometry, invokes native Close and ordinary parent Back, and compares all nine full
stored data sources. Preserve the original development files; use no input or Save. Named-route openings are
distinct from AT-102 native debt-button opening. Negative fixtures never count as acceptance.

Plan validation review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only plan-validation -FontScale 2 -Theme light -WindowSize 360x800`.
AT-104 invokes native Save four times only with invalid blocking input, verifies initial full error glyphs and
automatic current input visibility after corrections, restores the original unsaved choices, and compares complete
stored accounts/entries/settings/schedules. Keep negative cohorts distinct and preserve original development files.

Transaction validation review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only entry-validation -FontScale 2 -Theme light -WindowSize 360x800`.
AT-105 invokes actual native Save eight times only with invalid blocking input, checks all error glyphs/current
input visibility and corrected/collapsed details, retains full drafts/stored rows and restores original presentation
choices. Preserve original development files and separate failed candidate cohorts from final native evidence.

Existing transfer-fee review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only entry-fee-retention -Mode simple -FontScale 2 -Theme light -WindowSize 360x800`.
AT-106 prepares fees only in the fictitious snapshot database and invokes three actual valid Saves: retain,
edit and explicit removal, then reopened/new Cancel/Discard. Preserve and restore original development files;
never run successful-save fixtures over personal data. The Advanced regression uses `-Mode advanced`.

Settlement feedback review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only settlement-feedback -Mode advanced -FontScale 2 -Theme light -WindowSize 360x800`.
AT-107 invokes six invalid/no-change and three valid actual native Saves in the fictitious snapshot database.
Extra payment, refund and zero-bill rows are compared with the original financial state, then only those identified
scenario rows are removed between cases. Preserve/restore original development files; never run valid-Save fixtures
over personal data. Simple uses the same settlement fields and can be reviewed with `-Mode simple`.

Occurrence feedback review: `./eng/scripts/Run-Snapshots.ps1 -Languages 'en,fa,de' -Only occurrence-feedback -Mode advanced -FontScale 2 -Theme light -WindowSize 360x800`.
AT-108 uses four invalid native actions and four valid metadata/partial/completion Saves per context, only in the
walk-through's fictitious database. Original fields/ids/states are compared; remove only the exact identified new
scenario rows/state between cases. Preserve/restore original development files. Never use valid-Save fixtures over
personal data. Simple uses the same fields and can be reviewed with `-Mode simple`.
For an isolated follow-up review of unchanged continuations, set VAFADAR_SNAPSHOT_OCCURRENCE_MESSAGES_ONLY=1:
the route invokes four invalid native Saves and independent corrections, compares complete original stored values,
writes a distinct message-only proof and performs no valid Save. Clear the process-local setting after review.

The Debug `transactions` snapshot route checks AT-109 complete row identity across filters, fresh reload and
process-local display-unit invalidation against actual EntryPresenter fields without Save. Run the existing snapshot
script with `-Only transactions`; restore original development data and plain preferences after owned reviews.
Temporary QA-06 timing probes are removed before final builds. Evidence:
`src/Apps/Zanance/docs/quality/transaction-row-reuse.md`.

The transactions Debug review also invokes actual native Select all, reloads the complete bound snapshot, checks
every retained selection, then invokes Cancel without financial writes (D-105 / AT-110). Preserve development files.
Temporary full-workload timings end at LoadAsync and exclude subsequent native painting; remove probes before final
Windows and complete Android Debug/Release builds. Evidence: src/Apps/Zanance/docs/quality/bulk-selection-reload.md.

The settings-suggestion Debug route compares the actual bound caption with the unchanged calculator over complete
fictitious history and reloads an unsaved estimate without Save (D-106 / AT-111). Settings display/reopened routes
retain native retry/choice/return checks; their deliberately covered failure captures are expected fixture states,
not failed review files. Preserve development files and remove temporary timers before final platform builds.
Evidence: src/Apps/Zanance/docs/quality/settings-suggestion-history.md.

The Windows Debug `settings-pickers` route reviews selected text and actual expanded popup rows through native
UIA ExpandCollapse/SelectionItem patterns (D-107 / AT-112). It reselects existing choices, retains the raw estimate
draft and compares complete Settings/Accounts/Entries without Save. General layout checks also measure selected
picker glyphs on the other captured pages. Use the snapshot script with `-Only settings-pickers` for the focused
route; preserve original development files and distinguish process-local text stress from OS/device acceptance.
Evidence: src/Apps/Zanance/docs/quality/native-picker-captions.md.

The snapshot script fails explicitly when its app times out, exits unsuccessfully or writes error.txt. Partial/failed
captures are retained for diagnosis and never count as a successful review, even when some proofs were written.
Native Back review uses the real control's associated automation peer and retains actual timeout state without
extending the bound or substituting programmatic navigation (D-107).

The Windows Debug `transactions-filters` route natively expands the existing optional controls and verifies targets,
result identity and stored rows without Save. `transactions-scope` passes a fictitious report query through the real
route, reviews native scope/date/category glyphs and Clear targets using native Scroll, exercises result scrolling
and native Clear, then restores the complete query and stored/result values (D-108 / AT-113). Preserve original
development files and distinguish process-local stress from real OS/device acceptance. Final evidence is recorded
in src/Apps/Zanance/docs/quality/transaction-scope-readable.md.
