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
| `VAFADAR_CAPTURE_WINDOW` | With `VAFADAR_START_ROUTE`: saves the whole window â€“ title, navigation and page â€“ as a PNG to this path, rendered by the app itself, so it also works while other windows cover the app; open dialogs are saved next to it (`<file>-popup1.png`, ...) |
| `VAFADAR_CAPTURE_DELAY` | Seconds before that capture (default 2), e.g. to open a dialog with UI Automation first |

### Installing on an Android phone without Visual Studio

Every Android phone-test handoff includes a freshly built complete signed APK, its actual file path and verified
package/signature (owner rule, D-66). Build Release with `eng/scripts/Build-AndroidApk.ps1`; set the process
environment property `ContinuousIntegrationBuild=true` for the same warning policy as CI. Do not substitute an
ordinary Debug/Fast Deployment artifact. APK creation and signature verification do not prove installation or
physical-device acceptance; the owner installs and tests the supplied file.

The APK in `bin/Debug/net10.0-android` is made for **Fast Deployment**: Visual Studio copies the app's assemblies to the
device separately, so that APK alone stops at start (logcat: *No assemblies found â€¦ Fast Deployment*). For a phone or
for testers, build a complete APK with `eng/scripts/Build-AndroidApk.ps1` (Release by default, signed with the local
debug key) and install `artifacts/android/pro.vafadar.zanance-Signed.apk`. Android blocks screenshots of the app on purpose
(`FLAG_SECURE`, D-23); use the accessibility tree (`adb shell uiautomator dump`) to check screens on an emulator.

**Visual Studio open at the same time:** it restores the solution in the background for all target frameworks, which can
overwrite the restore of a command-line build for one framework. The build then stops with `NETSDK1005` (*assets file
doesn't have a target for 'net10.0-android'*) or `APT2126` (*file not found*). Run the command again â€“ it restores
first â€“ or close the solution in Visual Studio while building from the command line.

## Target frameworks

* Plain libraries, tests and (future) web projects: `$(VafadarTargetFramework)` = `net10.0`.
* MAUI projects: `$(VafadarMauiTargetFrameworks)`, which depends on the build machine:

  | Build OS | MAUI targets |
  |---|---|
  | Windows | `net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0` |
  | macOS | `net10.0-android`, `net10.0-ios` |
  | Linux | `net10.0-android` |

  Passing `-p:VafadarMauiTargetFrameworks=...` on the command line overrides this for all projects â€“ useful for
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

* `codeql.yml` â€“ security analysis of C# and workflow files (weekly and on changes).
* `release-android.yml` â€“ manual: builds a signed `.aab` for Google Play (see [release guide](release-and-publishing.md)).
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

The Windows Debug transactions-saved-scope route compares real native named-filter invocation before and after
independent fictitious report scopes, retaining complete stored data. It prepares only its own missing unreviewed
fixture before the comparison; reviewed actions do not save money. Normal device acceptance remains separate.

The Windows Debug transactions-source route uses the actual bound transaction source to review equivalent-search
reuse, changed results and fresh data/display publication (D-110 / AT-115). It never saves money. Temporary timing
probes remain outside final app builds; preserve and restore the original development files.

The Windows Debug entry-repeat route invokes actual Repeat/Cancel for fictitious new expense/income/transfer drafts,
rejects invalid base input and compares complete original drafts/stored rows without Save (D-111 / AT-116).
The separate entry-repeat-presentation route checks the actual target inside its native Scroll viewport without
repeating financial handoffs. It uses bounded real fictitious onboarding; neither route saves its entry or plan.
Capture all translations/help, modes and layout contexts; preserve/restore original developer files on failure too.

The Windows Debug entry-categories route (D-113 / AT-117) substitutes only fictitious presentation choices,
checks actual native wrapping/glyph/viewport geometry and invokes original selection commands without Save.
It restores full drafts/choice state/rule hints and compares complete stored rows; snapshots never activate the
own native AppWindow. Native stored-name fixture review may prepare/restore only its exact independently owned
sample database, never profile or security storage. See [evidence](../../src/Apps/Zanance/docs/quality/entry-category-captions.md).

## Entry Save-feedback review (D-130)

The Debug-only entry-save-feedback snapshot route edits the walk-through's fictitious purchase below its refund,
invokes the actual native Save/retry and an exact sample update trigger, and verifies every stored table/receipt
value and the reviewed draft fields. The trigger is removed in finally. It measures full realized error glyphs/names
and the existing enabled Save target at both native scroll ends, then checks stale-feedback clearing. Use
Run-Snapshots.ps1 -Only entry-save-feedback with the existing language/theme/size/process-local font options.
Capture only the hidden application's own rendered root; no device/OS setting or desktop-input change.
[evidence](../../src/Apps/Zanance/docs/quality/entry-save-feedback-visible.md).

## Retained bill writer review (D-131)

Use Run-Snapshots.ps1 -Only settlement-writer with the existing language/theme/size/process-local font options.
The Debug route invokes actual native Save for an owned sample insert failure/retry and changed advance, restores
all complete sample rows, then runs successful extra/refund/zero-bill navigation. Temporary SQL objects are removed
in finally; only fictitious walk-through data is touched. Render the hidden app's own root; never change OS settings
or send desktop input. [Evidence](../../src/Apps/Zanance/docs/quality/selected-account-write-policy.md).

D-140 reminder verification compiles the actual ReminderService, IReminderScheduler and AppLinkRouter sources in
App.Tests, with isolated real SQLite/localization and explicit native-effect ports. No fake MAUI controls or
duplicated reminder algorithm. Normal signed Release native evidence and the complete APK are recorded in
src/Apps/Zanance/docs/quality/selected-plan-reminders.md. Builds/tests are separate from physical-phone/iOS acceptance.

## Durable-choice verification (D-141)

D-141 changes the database model: run AddResourceSelections and regenerate the compiled model with the standard
commands above. The full suite checks first-schema upgrade and current model; compare all 25 complete tables.
Current normal Release keeps commercial registration inactive. No enabled native commercial acceptance is implied.
See [the revision-specific evidence](../../src/Apps/Zanance/docs/quality/durable-resource-choices.md).

## Reviewed choice-form native checks (D-142)

Run-Snapshots.ps1 -Only active-items checks actual account/Plan row/kind buttons, native glyph allocations/targets/names,
capacity, paused state, unrestricted mode and covered retry against its independent artifact database, without Save.
-Only active-items-entry checks the normal registered Settings action, native help and Back with current inactive DI;
compare all 25 complete application-profile tables. Both support existing language/theme/width/process-local font
arguments. Preserve/restore development databases/sidecars before the canonical fictitious walk-through; hidden
own-root rendering and native UI Automation remain mandatory. Debug restores the original ui.theme preference too.
No OS setting, fake release tier, device security or desktop input change. [Evidence](../../src/Apps/Zanance/docs/quality/active-item-chooser.md).

## Goal choice review (D-144)

The existing active-items Debug route now checks three real choice groups, original goal measurement/state and
one-goal capacity. active-items-entry includes the normal registered Goals choice plus actual updated Help/Back.
Keep the same independent fixture/page-only bounded input, hidden own AppWindow, native Invoke/glyph/target checks
and exact original data/display restoration. [Current matrix/APK evidence](../../src/Apps/Zanance/docs/quality/goal-chooser.md).

## Goal-reminder delivery review (D-145)

Use the canonical complete signed Release APK for Android actions. Read actual owned native pending requests and deliver their unchanged single/group returning data through the real native Intent adapter; do not claim future 09:00 firing. Existing Debug goal-reminder preparation now validates the scoped original member/file identity. Keep independent fictitious files, original bytes/sidecars and display restoration. No desktop input, clock/system settings, physical device or runtime entitlement replacement.

[Source/native/APK evidence](../../src/Apps/Zanance/docs/quality/goal-work-reminders.md).

## Focused goal-view review (D-146)

Use VAFADAR_SNAPSHOT_ONLY=goal-work-views for actual list/detail/Home and goal-work-release-hint for the focused neutral explanation. Fixtures use independent artifact databases and page-local fictitious Free input; release registration stays inactive. The hidden own AppWindow, native glyph/target/Invoke checks and exact original data/display restoration remain required. Android acceptance uses the complete signed Release. An unlaunched complete Debug package reads/prepares/restores the exact owned sample file; a separate complete Debug run may diagnose an already failed owned action, without being counted as Release acceptance. Preserve failed helper attempts and never infer physical/OS/Store/startup acceptance.

[Evidence](../../src/Apps/Zanance/docs/quality/goal-work-views.md).

## Focused original goal-editor review (D-147)

The Debug-only `goal-editor-work` snapshot route uses an independently owned artifact database, actual Advanced/Simple
modals and real native Invoke targets. Set the usual own-window snapshot size/theme/languages and process-local font
stress; preserve/restore all original development database/sidecar bytes and display. It checks rejected stale Save,
reviewed reload, complete original rules/categories, glyphs/targets and native Close. Explicit review-effect answers
never replace release registration or MAUI controls. Android proof uses the normal complete signed Release in the
owned emulator and only its exact fictitious profile; one note-only Save preserves original financial rows.

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

Evidence: `src/Apps/Zanance/docs/quality/goal-editor-work.md`.

## Budget definition verification (D-148)

Final normal parallel main suite: 2,594 / App.Tests 537, followed by ordinary test output cleanup. Strict Windows
and Android and canonical Build-AndroidApk.ps1 complete. Artifacts: budget-choice-all-tests-final.log, matching
build/clean logs, budget-choice-native/final-proof.json and budget-choice-apk-proof.json. Signed full phone artifact:
artifacts/android/zanance-d148-release.apk. No UI source changed; completed layout matrices are retained. The normal
Release native Save/Delete does not activate or prove the later enabled chooser/view/alert integration.

## Budget chooser verification (D-149)

Main 2,607 / App.Tests 550 pass, followed by ordinary cleanup. Strict Windows/Android and canonical Release pass.
The active-budget-definitions Debug route operates actual native choices and real Save in an independent fixture,
restoring all 25 tables. Evidence covers 18 Windows chooser and 18 registered Settings/help cases plus six
normal inactive Release emulator cases.
Artifacts: budget-chooser build/test/clean logs, verified-size/theme proof folders, native/final-proof.json and
APK proof. Complete phone artifact: artifacts/android/zanance-d149-release.apk. Physical/iOS gates remain separate.

## Budget alert verification (D-150)

Main 2,632 / App.Tests 575 pass, followed by ordinary cleanup. Strict builds and canonical Release pass. Six
hidden own-window Windows budget renders and six normal signed Release emulator cases retain full original rows
and restore exact owned bytes/display. Retired native preparation is covered by explicit application ports;
commercial production registration remains inactive. Complete APK: artifacts/android/zanance-d150-release.apk.
See Zanance docs/quality/budget-alert-work.md for logs, proof paths and remaining acceptance boundaries.

## Original budget notification verification (D-151)

Main 2,646 / App.Tests 589 pass and normal cleanup completes. Strict Windows and canonical strict Android Release have no warnings/errors. The budget-reminder Debug route renders hidden original periods without Save; final native Monthly navigation is checked after a weekly destination. Six normal signed Release emulator cases check four exact original targets per case and restore all owned rows/bytes/display. Complete APK: artifacts/android/zanance-d151-release.apk. See Zanance docs/quality/budget-reminder-routing.md.

## Original budget editor verification (D-152)

The budget-editor-work Debug route uses an independent fictitious database, hidden native modal and real Invoke providers. Run en/fa/de in both themes at 360/412/wide; compare all 25 complete table/column/row packets after stale Save, explicit Reload/Keep edits and native Close. Retain failed route/convention evidence. Complete signed APK: artifacts/android/zanance-d152-release.apk. See Zanance docs/quality/budget-editor-drafts.md. Native tests use only the independently owned QA03 database and private emulator/server; no owner device settings.

D-153: the focused `budget-actions-work` Debug review uses the actual budget page over an independent fictitious
database, native Invoke targets and complete 25-table comparison. Failed captures are negative evidence.
Normal Release emulator review retains FLAG_SECURE and restores exact owned rows/sidecars/display.


D-154: the focused `active-quick-resources` Debug route uses the actual six-kind chooser and template page over
an independently owned SQLite fixture. It invokes native choice/Save/Delete patterns, captures current realized
caption/target/name geometry and restores all 25 tables. It waits at most one second for queued current native
kind layout and rejects retired pages. Failed captures are negative evidence. Normal complete Release Android
checks use only the existing owned sample profile/private emulator transport; restore exact bytes/display and
leave the final signed Release installed/stopped. This does not activate production commercial limits.

## Home template native review (D-155)

Windows Debug routes `home-template-work` and `home-template-destination` use an independent fictitious database, actual Home/chooser/entry controls and recorded original native-effect parameters. The continuation waits for the real Shell initial query before applying the recorded typed handoff, checks realized pinned feedback, restores complete rows and renders only the hidden app root. Earlier failed/racing captures are negative evidence. Canonical signed APK remains required for phone handoff; build/CI is not device or publication acceptance.

## Original profile choice native review (D-156)

The Windows Debug `profile-work` route uses an independent fictitious catalogue/database and actual Profiles
controls. It checks inactive/bounded original publication, explicit selected identity, stale Save, retained
drafts and native Cancel/Discard/Reload without financial Save. Hidden native AppWindow/root rendering retains
its own WinRT caption/Invoke read objects for the short review process; require normal process exit as well as
completed proof. Keep interrupted/finalizer-failed captures as negative evidence. The ordinary `profiles` screen
already belongs to the snapshot route list. No production provider replacement or OS/device setting change.
See the repository evidence at `src/Apps/Zanance/docs/quality/local-profile-work.md`.

## Actual file-work native review (D-157)

The Windows Debug `profile-file-work` route constructs the actual decorated DI over an independent fictitious
SQLite/catalogue. It renders the original goal list/detail: chosen basic balance-goal work, unchosen complete
financial history, native positive/release direction targets, retained release draft and rechosen original basic
work. A Free selection does not grant the retained paid earmark type. No native Save or authentication is invoked.
Complete 25-table data and original catalogue are compared/restored. Only the app's hidden own root is captured;
retained WinRT read objects survive through normal review-process exit. The original independent-profile review
remains `profile-work`. Failed prototype assertions are negative evidence. See
`src/Apps/Zanance/docs/quality/profile-file-work.md`.

## Original catalogue reminder native review (D-158)

Windows Debug `profile-reminder-work` uses actual decorated DI, Profiles controls and the real reminder watcher
over independent fictitious SQLite. A recording native-effect port avoids desktop notifications; actual native
Save withdraws/restores the original queue, while the same Free context does not grant period-review delivery.
Await PendingRefresh through its final guards, compare complete 25-table data and restore catalogue/owned files.
The form layout is unchanged; do not repeat the existing full layout matrix. Normal Release emulator SDK delivery
and original native returning-data checks remain distinct from fixture ports and commercial activation.
See `src/Apps/Zanance/docs/quality/profile-reminder-work.md`.

## Original reminder input native review (D-159)

Debug `reminder-input-work` renders the actual Goals page over independent fictitious SQLite/decorated DI.
A recording native-effect port changes original Plan/financial inputs during publication; the queue retires,
fresh reached-goal progress suppresses contribution work, and complete 25-table data restores. Capture only
the hidden own native window, preserve exact owned development files/sidecars and distinguish fixture ports
from actual SDK delivery. No form/layout change requires another unchanged full layout matrix.
See `src/Apps/Zanance/docs/quality/reminder-input-work.md`.

## Actual structured feedback native review (D-160)

Debug `commercial-feedback` uses original Transactions controls and actual atomic filter writer over separate
fictitious SQLite. Native menu/name effects are explicit ports; the changed reporter/Close and second bookmark
Invoke are actual native controls. Render the hidden own root and native popup separately. Compare full query
scope and all 25 tables; restore exact development bytes. No desktop input/focus or commercial activation.
See `src/Apps/Zanance/docs/quality/commercial-feedback.md`.

## Actual pre-Save readiness review (D-161)

Debug pre-save-readiness opens actual Goal/Budget editors over independent fictitious SQLite. Central Debug
mode configuration alone controls Simple/Advanced. Read realized reason/disabled Save controls and invoke
Cancel with explicit confirmation ports. Compare all 25 tables and complete raw drafts; restore exact owned
development bytes. No financial Save, desktop input/focus, OS setting change or production activation.
See `src/Apps/Zanance/docs/quality/pre-save-readiness.md`.

## Actual plan comparison native review (D-162)

Debug product-plans opens original comparison pages over independent fictitious SQLite with original native
navigation effects. Reacquire overlay chip targets, read complete realized identities/values and native 44-pixel
targets, and invoke actual help OK/Continue. Capture hidden own root and native popups separately; compare all
25 complete tables and restore exact owned development bytes. productplans is in the ordinary screen route list.
See `src/Apps/Zanance/docs/quality/product-plan-comparison.md`.

## Original editor writer native review (D-163)

Debug editor-writes uses actual Account/Plan pages and native Save/Close with original SQLite writers in
independent fictitious files. Return/consent refusals are explicit ports. Check pinned reasons and actual
input/Save enablement; remove only owned successful test rows and compare all 25 originals. Restore exact
development bytes. No owner desktop input/focus or original financial write.
See `src/Apps/Zanance/docs/quality/editor-write-feedback.md`.

## Original archive writer native review (D-164)

Debug account-archive invokes original Archive/Close and actual SQL rollback/accepted writers in separate
fictitious files. Consent/alert/return effects are explicit ports. Compare all 25 originals after declined
consent/SQL refusal, verify accepted original state/default without dirty financial fields, and restore exact
owned development bytes.
See `src/Apps/Zanance/docs/quality/account-archive.md`.

## Original editor readiness native review (D-165)

The focused Debug route is editor-readiness. It opens independent actual Account/Plan pages, reads
fictitious original cached access, invokes native explicit refresh/failure/retry and verifies complete
raw input plus all 25 original tables without Save. Main tests/cleanup/dependent builds remain sequential.
See the app quality/editor-readiness document for exact final native and APK evidence.

## Original data-file native review (D-166)

The focused Debug route is import-outcomes. It invokes actual native review/import/Undo against original
application/SQLite sources in an independently owned file. Picker/consent/failure-reporting and cached
access are explicit fictitious ports; actual money writers and all 25-row packet checks remain real.
WinUI must realize newly visible growing footer captions before measurement. Native failure is negative
evidence; fix only its cause and do not repeat unchanged successful main/Android builds. See the app
quality/import-outcomes document for final native and installable-package evidence.

## Restore outcome native review (D-167)

Debug route restore-outcomes uses the actual flow/original independent SQLite and safety/source services,
with explicit fictitious native completion effects and Debug-only discovery suppression. Ordinary final
Release Android separately checks actual DocumentsUI/consent/source/root. No owner desktop input, global
capture or device setting change. Exact owned fixtures restore; final signed Release remains stopped.
See [evidence](../../src/Apps/Zanance/docs/quality/restore-outcomes.md).

## Focused forecast review (D-169)

Windows Debug VAFADAR_SNAPSHOT_ONLY=forecast-work covers original native horizon/Refresh/Reset/Save
and retained saved data. forecast-currency-work adds actual two-currency Save and legacy comparison.
Fixtures use independent SQLite; rights/prompt/consent/return effects are explicit. Capture only the
hidden application's own root/window and restore complete owned rows/bytes/display. No device settings
or owner desktop input. Follow the usual signed Release package and strict-build handoff requirements.

## Focused receipt review (D-170)

Windows Debug VAFADAR_SNAPSHOT_ONLY=receipt-work invokes actual Home/attachment controls, native
translated feedback/Close and original typed editor. Picker/unlock/recognition are explicit fixture
ports over independent SQLite. Await real newly revealed caption geometry; preserve negative captures.
Only own hidden window/root is captured. Restore complete owned rows/bytes/display. Normal Release
digital-PDF verification and signed APK handoff remain separate from camera/OCR/device acceptance.

## Focused original loan review (D-171)

Windows Debug VAFADAR_SNAPSHOT_ONLY=loan-work uses independent SQLite, actual account/schedule targets,
original calculators/ledger writer and native confirmation Save. Cached rights and schedule navigation
capture alone are fictitious. Check Free balance/manual action/warning, retained reconciliation drafts,
full paid schedule/payment and exact 25-table restoration; render only the own hidden window/root.
Restore original development bytes/sidecars/display and separately verify normal signed Release APK/device flow.

## Focused original report review (D-172)

Windows Debug `VAFADAR_SNAPSHOT_ONLY=report-work` exercises native Free/Plus package selection and
detached actual PDF against one independently owned fictitious SQLite file. `report-pdf-work` adds
final original balance/currency PDF tables and an original contribution suggestion/native writer with
its named calendar and no ledger posting. Preserve successful contexts when repairing a probe; wait
for Shell reconstruction before changing languages. Keep native app-window capture and restore exact
owned rows/bytes. Android final signed Release checks the real share sheet without choosing recipients;
this is emulator/local test-certificate evidence, not phone, provider or production acceptance.

### D-222 â€” Cover the first window while preparing storage off its thread

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

## Android official native package prerequisite

The Android app restores Vafadar.Native.SqlCipher.Android 4.19.0-candidate.4 from the repository-local
feed. Run eng/native/sqlcipher/Ensure-AndroidPackage.ps1 to validate/reuse the exact qualified package
or build/package it from the pinned official inputs before restoration. Incomplete producers never
serve as package authority. CI and the manually dispatched Android release workflow share the qualified producer cache and run
this prerequisite before restore.
Use the canonical eng/scripts/Build-AndroidApk.ps1 for the complete signed Release phone artifact;
verify its signature, permanent package, both ABI native payloads and assembly stores. A successful
Debug/Fast Deployment build or independent probe cannot substitute for that artifact.

### Holiday catalog review (D-273)

The Windows Debug holiday-catalog snapshot route uses the actual regional preferences view with
memory-only preferences in the independent storageacceptance identity. It captures only its hidden
own window and compares financial rows. Hidden RTL popup peer selection is unavailable; layout
fixtures are not selection acceptance. Signed Android Settings XML checks cover actual selection
separately. Restore the normal ApplicationId and rebuild after independent Windows review.

### Original Home loading review (D-275)

The Debug home-loading snapshot route requires the compiled storageacceptance identity and the
independent protected fixture marker before startup. It forwards actual protected contexts, holds
one captured read, injects one controlled later I/O failure and invokes the real native retry.
It never substitutes queries or financial calculations. Own-window captures cover original loading
and failure at en/fa/de, both themes and 360/412/wide. A caption defect invalidates that partial
run; finish the affected corrected matrix before handoff. Restore the normal ApplicationId and
verify PackageName after review. Signed Release cold UI uses fresh emulator XML only, with one
confirmed live process; missing hierarchy is not stale-tree evidence or permission to restart.


## Protected workload benchmark (D-278)

The independent Windows executable and new/resume commands are documented in
eng/benchmarks/Zanance.ProtectedPerformance/README.md. It requires the reviewed Windows native
package and uses only marked fictitious artifacts roots. Never use app data. It measures store
operations, not native UI/device acceptance; no application rebuild is needed for this executable.

## Focused automatic-backup review (D-281)

Windows Debug `VAFADAR_SNAPSHOT_ONLY=automatic-backup` requires the compiled independent
`pro.vafadar.zanance.storageacceptance` identity before startup and its retained protected fixture
marker. It creates three actual native copies with a temporary two-copy retention bound, verifies
both retained packages and the unchanged pre-restore cohort, then checks the original backup page
in eighteen en/fa/de, light/dark, 360/412/wide contexts. Native row invocation previews the package;
cancellation clears selection, and the final complete financial fingerprint must match. The route
restores its retention/interval choices, avoids provider identity discovery, uses only the hidden own
AppWindow and optional process-local font stress, and never launches against the main identity.
Android qualification uses the final complete signed Release on the owned emulator with original
UI, private encrypted files and explicit restore. Do not repeat unrelated successful native reviews.

### Exact Windows native servicing variants (D-282)

The default local Windows producer uses reviewed MSVC 19.51.36257.0 and immutable native candidate.5.
Hosted windows-2025-vs2026 uses the separately reviewed 19.51.36260.0 producer with
VafadarWindowsCipherVariant=servicing and immutable candidate.6. Set this process-local environment
variable only when that exact signed compiler is installed; central package versions and native
packaging select the same variant. Unknown producers/variants and cross-producer packages are refused.
Neither setting installs or changes Visual Studio. Keep the actual producer evidence beside each raw
candidate and require the distinct entry policy plus production persistence checks. Existing local
baseline packages remain unchanged; do not overwrite one version with another compiler's output.

### Isolated PostgreSQL readiness in CI (D-282)

Wait on explicit TCP host/database, then initialize the fixture role on the same transport. The
pinned image's temporary initdb server listens only on Unix sockets; default pg_isready can report
that temporary process and race its shutdown. TCP readiness excludes that process without a fixed
startup delay. The CI fixture remains independent from the local backend and all other projects.

### Coherent Home comparison (D-283)

After building eng/benchmarks/Zanance.ProtectedPerformance, run its explicit home mode against an existing marked
protected reference/tenfold root, for example artifacts/protected-ledger-d280. It resumes the same keys/data,
compares only the original/current complete Home read operations and verifies complete typed financial preservation.
It refuses missing fixtures and writes a new home-measurements-<id>.json, preserving previous measurements.
This timer includes data equality/validation and excludes native UI. Do not infer a full startup/device claim.
The existing independent home-loading native route verifies the complete application binding/cover/retry matrix.

### Loaded native Windows Home (D-284)

Use a Windows Debug build with verified compiled ApplicationId pro.vafadar.zanance.storageacceptance,
VAFADAR_SNAPSHOT_ONLY=home-workload, VAFADAR_HOME_PERFORMANCE_ROOT pointing to the retained marked
protected reference/tenfold root, and a fresh VAFADAR_SNAPSHOTS output. Never use the main app identity.
The route refuses missing identities/history/keys and never seeds a replacement fixture. Requested
review windows remain hidden and capture only their own root. Standard Rebuild regenerates package
metadata when the build property changes; verify compiled metadata before launching and restore the
normal identity afterward. Native publication, dispatcher gaps and cold startup are distinct measures.

### Explicit personal-sync APK edition (D-292 in progress)

Use `./eng/scripts/Build-AndroidApk.ps1 -PersonalSync -Output artifacts/android-personal-sync` for the opt-in local
network edition. Use `-Offline` for a Release without connected providers or personal sync. The switches are
mutually exclusive; the script sets `ZanancePersonalSyncEnabled` explicitly so an ambient environment value cannot
enable network sync in an offline build. Neither edition activates commercial limits or real billing. Verify the
actual package/signature and final permission manifest before a phone handoff; a build result alone is insufficient.
The retained D-292 personal-sync APK is documented in the local-personal-sync qualification record; its existence
is not device, iOS, Store or public-service acceptance.

### Independent personal-sync native review (D-292)

Build Windows Debug with `-t:Rebuild -p:ApplicationId=pro.vafadar.zanance.syncreview
-p:ZanancePersonalSyncEnabled=true -p:ContinuousIntegrationBuild=true` and verify the generated PackageName metadata.
Use a fresh `VAFADAR_SNAPSHOTS` output with `VAFADAR_SNAPSHOT_ONLY=personal-sync`. This route refuses the main
installation before startup, and requires an empty independently named native data root before fictional seeding.
It renders the original first-run and version comparison pages in en/fa/de, light/dark and 360/412/1280 widths.
The existing native caption glyph checks and real UI Automation Invoke pattern inspect original controls, retaining
complete financial fingerprints. It never signs in, enters credentials, contacts a service or invokes a financial
choice. Own-window renders remain hidden; no desktop input or full-screen capture is used. Retain its marker,
financial fixture, negative evidence and results; do not reset a completed or failed installation to repeat successes.
Restore the normal compiled identity before the main artifact is built.

## Independent main household lifecycle review (D-294 in progress)

Windows Debug routes `household-lifecycle-flow` and `household-lifecycle-deleted-layout` require the compiled
identity `pro.vafadar.zanance.householdlifecycle` before covered storage starts. The project compares its owned
generated Windows manifest identity before the MAUI manifest target: changing only ApplicationId was otherwise
absent from the SDK incremental inputs and reused stale AppInfo metadata. It regenerates only that mismatched
generated manifest; no installation is reset. Verify generated manifest and AssemblyInfo identities before launch.

The completed original main lifecycle folder is `artifacts/household-main-lifecycle-ui-d294`. Preserve its public
progress and completion files, encrypted installation, keys and exact service deletion. Do not replay completed
flow stages. The separate deleted-state layout route requires that completed original fixture and resumes only
unfinished layout cases, without financial synchronization or another deletion. Its own-window root stays hidden.

For the independent Android Debug review only, use `Build-AndroidApk.ps1 -Configuration Debug -PersonalSync
-ReviewApplicationId pro.vafadar.zanance.householdlifecycle -Output artifacts/android-household-lifecycle-d294`.
The script rejects that override for Release; the normal product identity and complete signed Release handoff
remain unchanged. The explicit `household_lifecycle_review` activity extra can show only this package's original
main household page. Stage only the fictional service's public CA and exact public identity receipt in its own
files directory, and reverse the owned emulator's port14645 to localhost. Never alter device trust/settings or
FLAG_SECURE. Native confirmations remain real Android UI actions; the Debug observer records public status and
fictitious local ledger hashes, never credentials, device/root keys or tokens. This is not Release/AOT, phone,
all-financial-graph, iOS or whole-household acceptance.

The original main lifecycle and its six Android native confirmation cases are now completed; never replay or
reset them. The current retired room is0ec753d7-031b-4e3a-8c16-68599ef608ce, with original operation
8c52880c-f3f7-42cc-b014-7ea9ac8807e7. Main cold receipt and local account/entry preservation passed. The later
changed-state audit/deadline check used a new signed Debug binary and only a lifecycle observation, without
another deletion or receipt request. Its evidence is household-deleted-display-audit-android-final-proof.json
under artifacts. Final changed Windows layouts are in artifacts/household-deleted-final-layout-d294, separate
from the earlier unchanged cases. These are not final Release qualification or permission to reset test data.

The same independent package also accepts household_recovery_review with review_stage owner/export/restore/cold/original
for the main-page recovery qualification. It retains public fixture identifiers and exact financial hashes,
creates explicitly named independent protected profiles and refuses replacement after interrupted unpublished
creation. Consequential operations still require real native page buttons, confirmations and document pickers.
recovery_mode wrong/valid changes only compiled fictional in-memory password input when the original page is idle;
no password enters arguments, reports, preferences or service credentials. Preserve completed original stages and
use cold/original only to reopen their retained data; never clear or reseed an interrupted installation.


## Optional Android billing test edition (D-295)

Build the complete selected test APK with:

```powershell
./eng/scripts/Build-AndroidApk.ps1 -Billing -BillingSandbox -Output artifacts/android-billing-local-d295
./eng/scripts/Test-AndroidPrivacy.ps1 -Path artifacts/android-billing-local-d295/pro.vafadar.zanance-Signed.apk -Variant Billing
```

Billing is independent of PersonalSync. Offline cannot select either network feature; BillingSandbox requires
Billing. Explicit defaults override ambient build properties. The maintained official SDK restores only for the
selected Android billing graph. Debug/sandbox cannot issue production private rights; the selected local debug
signature is not store signing. The actual store catalog/service must be configured before an offer is available.
Do not bypass unavailable state with design prices, a client grant or an unverified transaction id.
Native StoreKit2 and iOS build/runtime qualification must run in the actual Mac clone; Windows common-code builds
are not iOS evidence. See src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/12-billing-delivery.md.


### Germany accountant preparation (D-296)

The original `accountantexport` route is included in the Debug walkthrough. Its dedicated `accountant-native`
Windows review requires the independently compiled id `pro.vafadar.zanance.accountantreview` (including
ApplicationIdGuid) and exact `artifacts/accountant-native-d296` output. It uses its own fictitious SQLite file,
checks native selection/review/Cancel and realized captions in en/fa/de, light/dark and360/412/1280 widths,
retains failed captures and records completed cases so successes are not replayed. It never shares externally.
Actual Release Android sharing/package checks remain distinct. Focused source-flow acceptance is AT-296 in
AccountantExportFlowTests. See the D-296 delivery record for final native/APK paths and failed-then-fixed evidence.


### Documented bank CSV (D-297)

Focused classes: BankCsvFlowTests and directly affected DataFileOutcomeFlowTests in Vafadar.Zanance.App.Tests;
resource completeness/usage in Vafadar.Localization.Tests. The independent Debug Windows identity
pro.vafadar.zanance.bankcsvreview with VAFADAR_SNAPSHOT_ONLY=bank-csv-native accepts only the exact owned
artifacts/bank-csv-native-d297 folder and resumes unfinished contexts. Native Invoke/Toggle/Cancel only,
fictitious independent sources, own-window rendering, unchanged financial fingerprint. Never reset its data
or replay complete contexts. Main Android Release uses Build-AndroidApk.ps1 -Billing -BillingSandbox with the
permanent identity; source-flow/native/APK evidence is in14-bank-csv-delivery.md.

### Optional analysis prerequisites (D-298)

Focused App.Tests classes: AiSnapshotTests, AiAnalysisFlowTests and AiResultBoundsTests. API qualifications have
Qualification=AiLocal and require an independently owned PostgreSQL fixture on127.0.0.1:55478, database
zanance_ai_d298 and ZANANCE_AI_QA_ROOT with exact fixture.marker text `Zanance fictional analysis d298`.
Run AiCreditPersistenceTests, then AiHttpContinuationTests, then AiConcurrentCreditTests. They retain original
stages/keys/ids and refuse completed replay. Never reset a completed fixture to inflate evidence. The identity-only
CI job excludes this trait; the dedicated analysis job owns its ephemeral database and uploads bounded test reports.
No real model, credentials, purchase or public hosted activation is involved.

Independent Windows native review: compile ApplicationId=pro.vafadar.zanance.aireview, set
VAFADAR_SNAPSHOT_ONLY=ai-native and exact VAFADAR_SNAPSHOTS=<repository>/artifacts/ai-native-d298. Native
Toggle/Invoke/Cancel and own-window rendering use original controls/fictitious source/effects only; completed cases
are not replayed. Keep failures and original fixture. Exact current native/private/APK checks:15-ai-local-delivery.md.

## Independent retained market-source qualification (D-299)

`dotnet run --project eng/sync/MarketHousehold -c Release -p:ContinuousIntegrationBuild=true` requires an explicitly
owned fictitious `ZANANCE_MARKET_QA_ROOT` containing its existing marker, protected service key ring and local
non-superuser PostgreSQL fixture. It refuses any other database/root and completed stages. Invocation1 seals
once; invocation2 reopens original keys/ciphertext and qualifies lost-response continuation plus Viewer
reception/refusal; invocation3 verifies the same protected clients cold. Never rerun completed stages or use
production data/credentials. Host provider selection remains independent. Native Windows market-native review
requires the separate pro.vafadar.zanance.marketreview identity and its exact artifacts/market-native-d299 root;
the independent Android caller uses ReviewMarketPrivate=true and its distinct package, never the main database.
Exact retained logs/package/native state: src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/16-market-reference-delivery.md.

## D-300 Android startup AOT input

Release Android builds supplement the installed SDK profiles with
src/Apps/Zanance/Vafadar.Zanance.App/Platforms/Android/Profiles/startup.aprof. Its adjacent README records method
metadata, digest, capture boundary and refresh rules. No profiler package, app-local profiler property or
forwarded port belongs in the production package. Do not use capture targets that change global device settings.
Qualification and actual package costs are recorded in the D-300 startup delivery document.

## Independent original currency layout review (D-302)

The Debug-only currency-text route requires pro.vafadar.zanance.currencyreview before startup and its owned
artifacts/currency-text-d302 root. It uses actual unsaved production forms, native Invoke patterns and retained
fictitious account data; never owner desktop input, focus, OS settings or the production database. The existing
process-only font override is layout stress, not OS-scale acceptance. A hidden Win2D canvas does not schedule
normal drawable layout: the review invokes the control's original IDrawable.Draw into its owned render target
before measuring the common button, never a shipping layout override or invented coordinates. Preserve failed
observations. The first row-selection fixture incorrectly requested an unrealized USD row; the corrected caller
invokes a realized GBP row and restores EUR without Save.


## Durable billing HTTP qualifier peers (D-304)

BillingClientContinuationTests gives each independent in-memory HTTP fixture a stable documentation-only
connection peer. Do not disable the service's RequestWindows accounting, raise its limits, delete its counters,
trust a forwarded header or wait for a minute boundary to make a test pass. Explicitly shared fixture peers
must retain the same real PostgreSQL budget across separate workers. Only the rate-boundary test freezes its
injected clock. Ordinary purchase HTTP retains its current server time. Linux Integration's unrelated default
HTTP installation key can be ephemeral; retaining the billing signing/private-client authority is distinct from
cold service-installation acceptance. Preserve failed roots and pending commands, then continue originals.
The D-304 evidence records the exact retained sequence and targeted continuation without replaying successful cases.


### Complete Home reader qualification (D-305)

The typed complete ledger reader uses the public EF relational command pipeline and original model-owned
converters. Refresh its explicit mapping contract when the persisted LedgerEntry mapping changes; do not
silently drop fields or bypass query filters. Its drift guard and full-value/order/cancellation tests keep
this boundary visible. The independent Android Home caller accepts a fresh review_pass plus original
review_baseline for same-data comparison without replaying stale completed measurements. Temporary timing
strategies are never shipping inputs. Refresh the real startup method profile after relevant startup changes;
retain SDK defaults, remove development recorder/environment/forward before ordinary APK qualification.
Current evidence: src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/22-home-materialization-delivery.md.

Development-only capture must select Android alone and override recording properties globally; a project-local
RunAOTCompilation value can be overwritten by later Release defaults. Use VafadarMauiTargetFrameworks=net10.0-android,
RunAOTCompilation=false and RuntimeIdentifier/RuntimeIdentifiers=android-x64 for that isolated recording build.
Confirm the recorder APK has no AOT libraries, reach actual Home in the original process, then collect names-only
metadata. Restore central package bytes and remove the exact owned forward before the ordinary unmodified
Build-AndroidApk.ps1 production build. Never apply those recording overrides to the final package.


### Reviewed input and goal lifecycle qualification (D-306)

The `goal-review-integrity` Windows snapshot route creates its own fresh SQLite fixture, invokes the actual
GoalDetail buttons/native confirmation and checks complete/reopen/pause/resume with retained amount and no
ledger/allocation writes. Use Run-Snapshots.ps1 -Languages 'en,fa,de' -Only goal-review-integrity -Theme light
-WindowSize 412x820, then a fresh dark output. Existing fixture paths are refused; keep the original failure
and continue only after its actual cause is understood. The review window stays hidden and uses only native
Invoke patterns. This is development/native evidence, not Windows commercial or phone/iOS acceptance.
The independent Android Home guard phase covers altered original inputs against retained protected datasets;
it is not another performance benchmark or new financial source. Scope/receipts:
src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/23-reviewed-input-integrity-delivery.md.

### Original saved-filter native checks (D-308)

A caller-owned Windows Debug review build can select `VAFADAR_SNAPSHOT_ONLY=filter-actions` with a fresh
VAFADAR_SNAPSHOTS directory. It creates only that directory's independent filter-actions.db and checks the
original page/actions; it does not use the generic historical seed. Set languages en,fa,de, light/dark and
360x820/412x820/1280x820. VAFADAR_SNAPSHOT_FONT_SCALE=2 is process-local layout stress, not an OS setting.
Keep the own window hidden; preserve its process, native observations and negative fixture evidence.
The independent Android Home caller's `filters` phase provides the original normal-scale counterpart;
see eng/benchmarks/Zanance.AndroidHome/README.md. This does not replace phone, spoken-reader or iOS acceptance.

### Original protected schema native check (D-309)

The independent Android Home caller's `schema` phase uses the original protected reference/tenfold run,
a fresh comparison id and the exact live PID. It invokes the actual protected-startup callback and counts
physical opens before comparing complete original ciphertext data against the existing rollback. Follow
eng/benchmarks/Zanance.AndroidHome/README.md; never reseed or replace keys for a comparison. Finish native
measurement before running another compiler. A callback timing is not whole Home/cold readiness.

### Owned purchase history layout (D-310)

A separate Windows Debug identity pro.vafadar.zanance.billinglayout with VAFADAR_SNAPSHOT_ONLY=billing-owned-layout
requires exactly artifacts/billing-owned-layout-d310. Its retained fixture/progress checks original growing
purchase captions in en/fa/de, light/dark and 360/412/1280 widths; process-local font stress is not an OS
setting. Completed cases are not repeated. Keep the own AppWindow hidden; no desktop activation/input.
The independent original-source Android caller has `billing-owned` for native text layout without store,
payment or financial effects. Actual store metadata/signature/provider lifecycle acceptance is separate.

### Original financial table readouts (D-311)

The Windows Debug `financial-tables` route requires independent ApplicationId pro.vafadar.zanance.financialtables
and an owned artifacts/financial-tables-d311* directory. It uses a separate SQLite fixture and original pages,
with in-memory canonical monetary/date presentation packets only. Default final review covers en/fa/de,
light/dark and 360/412/1280 at requested process-local text scale; VAFADAR_TABLE_BEFORE=1 is the narrowly scoped
original German/360 reproduction, not permission to replay it. Preserve failed fixtures. Template hashes
prevent carrying accepted native packets across a production-template change; completed contexts and individual
unchanged packets are skipped only for fixture corrections. Never activate or send desktop input.

The original-source independent Android Home caller's `tables` phase checks the same original reports/snapshot
surfaces, native text layout and real horizontal offsets in all three languages/themes. It uses a fresh
explicit run/comparison with original-process observation, no main data or system setting changes. See its
README. These native layout checks are not financial-calculation, statistical speed, phone or iOS acceptance.

### D-312 original safety-row layout qualification

The independent Windows Debug package pro.vafadar.zanance.safetycopies supports VAFADAR_SNAPSHOT_ONLY=safety-copies
with a fresh VAFADAR_SNAPSHOTS directory and process-local VAFADAR_SNAPSHOT_FONT_SCALE=2. The hidden original page
checks en/fa/de, both themes and 360/412/1280 widths using display-only candidates; it never invokes restore or
opens their files. The original full restore is qualified separately on the authorized phone, preserving its
existing fictitious data, safety files and keys across the signed upgrade. No system setting or credential entry.


### D-313 outbound tunnel qualification

eng/hosting/README.md documents the production origin and owner-maintained connector. The CI-only eng/hosting/qualify-tunnel.sh checks original Caddy TLS/forwarding in an independent no-port fixture plus focused operator tests; it never uses a Cloudflare credential or opens a tunnel. Local receipts are not public acceptance.


### D-314 approved physical ARM64 workload

eng/benchmarks/Zanance.AndroidHome/README.md documents the explicit ARM64 override, exact independent signed package, physical model/transport/PID guard and prepare/measure/ledger phases. Coordinate exclusive phone turns; never reset fixtures after an observation failure. The bounded frame observation extension retains the original success condition. Physical receipts are in the app quality/physical-protected-workload.md report; main APK remains D-312.


### D-315 same-data protected read qualification

The AndroidHome caller supports read-profile (complete public read packet + optional initial component diagnosis)
and ledger-read (original native search without repeating financial writes). See its README for baseline/pass
semantics and physical/PID guards. Build the main candidate with Build-AndroidApk.ps1 -Offline -Output
artifacts/android-sequential-ledger-d315-qualified. Actual final APK/signature, same-file phone evidence and
focused test scope are recorded in the app quality/physical-ledger-read.md; this is not full cold/ANR acceptance.

## Independent first-plan native regression (D-318)

See the AndroidHome caller README and [first-plan evidence](../../src/Apps/Zanance/docs/quality/first-plan-publication.md). Its plan-publication phase owns an initially empty independent installation, records intent before the one account/plan setup and observes actual editor Save/native row realization. plan-publication-fixed requires that exact retained unpaid fixture, exclusively preserves its definition before guarded retirement and refuses mismatched account/plan packets or financial history; never reset it or use main identity.

For Windows empty-layout qualification build the original app sequentially with ContinuousIntegrationBuild=true and ApplicationId=pro.vafadar.zanance.planpublication into an independent output. Launch only that verified executable hidden with VAFADAR_SNAPSHOTS pointing beneath artifacts/plan-publication-d318/, VAFADAR_SNAPSHOT_ONLY=plan-publication and VAFADAR_SNAPSHOT_FONT_SCALE=2. This early route bypasses the general seeded walk-through and owns plans-layout.db only. Preserve original progress/images when a changed template needs a new matrix; unchanged passed cases are not repeated. Never use the general snapshot wrapper to move existing development databases for this check.

## Original loan missing-term native checks (D-319)

Build the original Windows app sequentially with ContinuousIntegrationBuild=true, ApplicationId=pro.vafadar.zanance.loanterms and an independent output. Launch only that verified executable hidden, with VAFADAR_SNAPSHOTS beneath artifacts/loan-terms-d319/, VAFADAR_SNAPSHOT_ONLY=loan-terms and VAFADAR_SNAPSHOT_FONT_SCALE=2. The early guarded route owns loan-terms.db only, checks original AccountDetail publication/native caption visibility and preserves complete stored packets. It retains per-case progress and refuses completed review; continue failed/incomplete cases on the same fixture after diagnostic-only repairs. Use the actual vertical ScrollView rather than the horizontal monetary viewports. Never run the general snapshot wrapper against existing data for this check. See [physical loan evidence](../../src/Apps/Zanance/docs/quality/physical-loan-lifecycle.md).

## Serviced Android AOT toolchain and native load qualification (D-320)

The main Android host and runtime-pinned Home/sync callers import eng/AndroidAotRuntime.targets.
PackageDownload restores compiler packages for ARM64/x64 and the task matching VafadarSecurityRuntimeVersion;
selection validates their presence and overrides the workload task locally. Installed SDK/workloads remain
unchanged. The runtime, actual compiler --version and task product metadata must agree. Other hosts still
need their own qualification; current native runtime evidence is the approved ARM64 phone, not all ABIs.

Use matching restore/publish configurations. Build-AndroidApk.ps1 passes its Configuration to restore before
the unchanged complete publish; default Debug restore cannot prove Release tool downloads.
SDK default AOT profiles remain enabled. The captured startup profile omits only additional CoreLib method
roots after the retained-load failure comparison. A compatible .NET10 profile reader/writer can filter with
--filter-module='^(?!System[.]Private[.]CoreLib)' and --output; inspect actual method count/hash afterward.
A no-op filter is not acceptance. Keep original capture provenance/history, never rewrite tokens/identities,
and repeat only changed/failed/uncertain checks. See [native evidence](../../src/Apps/Zanance/docs/quality/physical-home-compilation.md)
and the [independent caller](../../eng/benchmarks/Zanance.AndroidHome/README.md).


### Independent filtered-summary review (D-326)

Build the original Windows Debug app with ContinuousIntegrationBuild=true,
ApplicationId=pro.vafadar.zanance.transactionsummary and an independent OutputPath. Launch its actual
verified executable hidden with VAFADAR_SNAPSHOT_ONLY=transaction-summary and a fresh
VAFADAR_SNAPSHOTS directory beneath artifacts/transaction-summary-d326/. Do not use the general
snapshot wrapper or move existing development databases. The guarded early route owns only its
independent transaction-summary.db, uses original page/view-model states and checks en/fa/de, both
themes and 360/412/1280 widths. Render the original hidden root before inspecting native glyphs;
compare rendered captions with the existing NativeDigits contract. Preserve the same fixture and
completed-contexts.json across a failed observation, never repeat completed contexts or accept a
partial matrix as complete. Phone filter interaction is a separate coordinated signed-main check.
See [qualified scope](../../src/Apps/Zanance/docs/quality/transaction-summary-result.md).


## Physical accountant export receiver

Use eng/tests/AndroidExportReceiver/README.md for the isolated native URI-grant receiver and original-packet host verifier. It is a local qualification fixture, not a production dependency. Coordinate the approved phone turn and never inspect main private storage or select a network recipient.


## Native continued report PDF qualification

The independent export receiver now accepts original report PDFs as well as accountant ZIPs. Use its README and verify_report_pages.py with local pypdf/Pillow and Poppler to inspect actual original bytes and rendered footer visibility. Samsung expanded chooser observations require the current compressed hierarchy; never reuse underlying-grid coordinates. Do not inspect main private storage or infer a valid export from a stale receipt.


## Physical original CSV producer/consumer

The isolated export receiver supports the bounded original ledger CSV format. Its README and verify_csv_roundtrip.py cover exact-byte native grant receipt, matching zero-account fixture preparation, all-column re-export comparison, same-file duplicate refusal, cold Undo and exact owned cleanup. Main code/APK need no rebuild when unchanged; this does not qualify arbitrary bank revisions.

## Independent interpreted import review (D-331)

Build Windows Debug with ApplicationId=pro.vafadar.zanance.importpreview; its pre-start guard refuses
the normal installation. Set VAFADAR_SNAPSHOT_ONLY=import-preview and an owned VAFADAR_SNAPSHOTS
folder. After protected startup, the original page reviews a separate fictitious SQLite store and CSV
through native Invoke/Scroll. It traverses en/fa/de, light/dark and360/412/1280 widths, retains completed
contexts and never invokes Import or Undo. Source/destination identities remain in the independent
fixture on continuation. Use sequential main Windows/Android restores; their shared assets file can race.
Restore the normal ApplicationId and build before handoff. Exact outcomes and package boundary:
src/Apps/Zanance/docs/quality/interpreted-csv-preview.md.

## Focused CSV page arrival (D-332)

The same independent importpreview Windows identity supports VAFADAR_SNAPSHOT_ONLY=import-page-arrival.
It invokes original Next/Previous and measures the actual review header before inspection scrolling,
without replaying900 prior caption checks. Requested18 contexts persist their own import-arrival-proof
records; no Import/Undo is invoked. Restore the normal identity and build sequentially after Android
publish. Native Android qualification uses the exact full offline candidate and owned inputs only; stage
all inputs before opening a picker, search the exact filename and verify the returned original filename.
A root/foreground observation failure does not authorize replaying the preceding native command.
Current startup ANR and package scope: src/Apps/Zanance/docs/quality/android-import-preview-arrival.md.

## D-333 independent cold reminder qualification

The original application can be built through eng/benchmarks/Zanance.AndroidReminders under its fixed
independent Android id. Build a full signed offline Release package with Build-AndroidApk.ps1 -Project
eng/benchmarks/Zanance.AndroidReminders/Vafadar.Zanance.AndroidReminders.csproj -Offline. Its README documents
explicit fixture initialization and read-only synthetic status; wrong identities fail before window/fixture effects.
Qualify the original notification action's actual new PID and zero-window state before another Activity.
A later metadata-only process intentionally does not prepare financial storage. See
[native boundaries](../../src/Apps/Zanance/docs/quality/cold-protected-reminder-startup.md).


## D-334 retained startup packaging

Keep the qualified default Android assembly compression and extraction configuration. A local complete
Release experiment with AndroidEnableAssemblyCompression=false crashes before Application.OnCreate
because libmonodroid.so requires missing compressed_assembly_count. Do not add custom symbols or
substitute runtime libraries. The alternative extractNativeLibs=false APK still has a real binding ANR
and nearly doubles the side-loaded package size. Neither is adopted.

Existing SDK lazy AOT/preload settings were already effective; do not repeat their setup or successful
unrelated tests. Observations must retain the exact actual process after a timeout. Restore all temporary
source/manifest bytes and the qualified ordinary APK after a rejected trial. Exact receipts and limits:
[src/Apps/Zanance/docs/quality/android-startup-packaging.md](../../src/Apps/Zanance/docs/quality/android-startup-packaging.md).


## D-335 independent More text review

The Debug-only VAFADAR_SNAPSHOT_ONLY=more-text route requires an independently compiled
ApplicationId=pro.vafadar.zanance.moretextreview before startup and output below
artifacts/more-text-d335. Set VAFADAR_SNAPSHOTS to a fresh owned subdirectory,
VAFADAR_SNAPSHOT_FONT_SCALE=2 and VAFADAR_WINDOW_SIZE=360x800. Launch only that verified build
with a hidden window. It loops en/fa/de, both themes and360/412/1280 widths and measures original
MorePage rows after actual native scrolling; it does not seed or invoke financial commands.
Qualify both ordinary and ZanancePersonalSyncEnabled=true builds sequentially. Retain original PIDs
through observation expiry; preserve partial/error receipts. Restore the permanent application id
and offline edition for final builds. No system text setting or desktop input is used.
[Evidence](../../src/Apps/Zanance/docs/quality/more-menu-readable.md).


## D-358 original work-editor text continuation

For the subsequent D-358 actual-OS work-editor continuation, restrict `editor-text` explicitly
through `review_editor_forms` and `review_editor_contexts`; new work forms use a marked separate
work-editor-text.db and compare goals/budgets too. `Run-StoreDrafts.ps1 -EditorTextReview
-EditorGoalChoicesOnly` checks only original Windows goal choices in a fresh isolated package;
it refuses combination with the debt-only mode and restores the new process environment flag.
Preserve completed matching source/input cases and verify actual native ancestry before proposing
layout fixes. [Bounded final evidence](../../src/Apps/Zanance/docs/quality/work-editor-os-text.md).

## D-357 expected negative native command results in CI

`eng/tests/AndroidCompression.Tests.ps1` deliberately invokes MSBuild with an unsafe destination.
After checking that refusal and preserving the outside file, it explicitly exits 0. Otherwise the
Windows runner's PowerShell epilogue forwards that expected native exit 1 even after PASS output.
Do not replace this with unconditional workflow success or remove any assertions. A runner-equivalent
child process reproduced exit 1 before the correction and exit 0 afterwards; an intentionally unmatched
refusal assertion still exited 1. The compression task and installed application remain unchanged.

## D-336 incremental Android assembly cache

The observed pre-managed assembly-store abort comes from a cached CoreLib header retaining
index193 while the current descriptor table assigns CoreLib192 and Java.Interop193. Validate the
SDK's current compression-item metadata before its incremental timestamp check and regenerate only
incompatible owned cache entries. Seven focused behavior cases preserve valid cache timestamps,
invalidate stale index/size/magic/truncation, accept missing files and refuse outside destinations.
Keep the original SDK compression, serviced runtime, AOT profile, key/data and permissions. No unrelated
successful financial, OCR, backend or More-layout suite is repeated or new managed count claimed.
QA-06 startup ANR, two-second loaded Home, broader Android, Mac/iOS and owner release gates remain open.
[Evidence, failures and limits](../../src/Apps/Zanance/docs/quality/android-compression-cache.md).


## D-337 bounded native startup profiling

Exact-PID, frame-pointer native captures retain the actual background startup ANR. Monotonic
phase filtering places75.34% of888 main-thread OnCreate samples in the clock-reading path; an
isolated native executable reproduces expensive precise clocks without application code in both
original and fresh same-image emulators. Broken callchains and single-sample limits remain explicit.
No clock substitution, kernel restriction, runtime/licence change or speculative app workaround is
shipped. The original AVD/data remain; ordinary D-336 signed APK and same-PID Home are restored.
App code, APK and managed counts remain D-336; no unchanged successful suite/build is repeated.
QA-06/phone/iOS/release remain open. Continue ready release preparation independently of those gates.
[Evidence and limitations](../../src/Apps/Zanance/docs/quality/android-startup-clock-cost.md).

## D-338 isolated store drafts

Use `eng/scripts/Run-StoreDrafts.ps1` for independent six-language native development drafts. It checks
the unique package before financial startup and renders on a non-input desktop without moving Main
data. Preserve live original processes after observation expiry. Export exact opaque RGB with
`eng/scripts/Export-StoreDrafts.ps1`; no upload occurs. Restore the ordinary Windows build afterwards.
See [commands, refusal cases and one-frame repair](../../src/Apps/Zanance/docs/quality/store-drafts.md).

## D-339 original report row stress

`eng/scripts/Run-StoreDrafts.ps1 -ReportRowsReview` reuses the fresh independent package/storage and
non-input desktop guard for 18 en/fa/de/theme/width contexts at process-local 200% text. These are
read-only UI fixtures, not the 48-image store export; the store encoder rejects this receipt shape.
Keep original processes and failures after observation expiry and restore the normal Windows build
afterward. The independent AndroidHome `legends` phase checks six current native report contexts.
See [evidence and current signed APK](../../src/Apps/Zanance/docs/quality/report-legends-readable.md).

## D-340 budget title review

`eng/scripts/Run-StoreDrafts.ps1 -BudgetTitlesReview` qualifies 18 independent Windows contexts at
process-local scale 2 using original translated Flex titles, without a saved method change. Do not
combine it with ReportRowsReview or export its receipt as store images. Restore the ordinary
Windows build identity afterward. AndroidHome phase budget-titles qualifies six native contexts;
retain its original live PID after observation expiry. See
[evidence and full signed APK](../../src/Apps/Zanance/docs/quality/budget-group-titles-readable.md).

## D-341 fresh linked-source test compilation

The Data-test project links the actual ReminderService and must also compile its original
Startup/IAppDatabaseStartup.cs dependency, as application-flow tests already do. A stale binary
cannot prove that fresh compilation succeeds. No replacement contract or copied startup behavior
belongs in the fixture. The Home-only check uses `--filter-class '*HomeReadSnapshotTests'` and an
explicit minimum expected count. Prototype checks are not current coverage after withdrawal. See
[the compilation repair and rejected native candidate](../../src/Apps/Zanance/docs/quality/home-publication-session.md).

## D-342 optional Home category review

Use `eng/scripts/Run-StoreDrafts.ps1 -HomeLegendsReview` for the guarded independent Windows
18-context matrix. `-HomeLegendsLanguages 'fa,de'` resumes only those language cases after an
English success; retain the original receipts/failures. Do not combine original review modes or
export their receipts as Store images. Process-local scale 2 never changes OS settings. Restore
ordinary Windows build identity afterward. AndroidHome phase `home-legends` uses a fresh comparison
on the existing independent financial-tables fixture; observe its exact original PID after timeout.

[Evidence and candidate](../../src/Apps/Zanance/docs/quality/home-category-legends-readable.md).

## D-343 shared plan-row review

`eng/scripts/Run-StoreDrafts.ps1 -PlanRowsReview` checks18 original Home/Plans contexts using
fresh isolated Windows storage/non-input desktop and process-local scale2. `-PlanRowsEdgeProbe`
checks only English Light360 for a concrete post-layout sign concern; do not replay the full passed
matrix for diagnostic repairs. Original native Scroll patterns, descriptions and stored inputs are
checked without financial commands. Restore the permanent Windows identity after review.
AndroidHome phase `plan-rows` reuses the retained financial-tables fixture with a fresh comparison;
observe the same original PID after timeout. [Evidence](../../src/Apps/Zanance/docs/quality/plan-row-amounts-readable.md).

## D-344 original ledger/account review

`eng/scripts/Run-StoreDrafts.ps1 -LedgerRowsReview` checks18 original Windows contexts at Debug
process-local scale2 using fresh isolated storage/non-input desktops. Explicit unique
`-LedgerRowsContexts 'de-Light-1280,de-Dark-360,de-Dark-412,de-Dark-1280'` can continue only missing
contexts, inside the selected `-LedgerRowsLanguages`. Retain earlier complete receipts and failures;
a completed component context does not clear an unrelated native renderer crash. Restore ordinary
Windows identity and remove all temporary native observers before handoff. AndroidHome phase
`ledger-rows` uses a fresh comparison on retained financial-tables data, without prepare/reset.
[Evidence and remaining gates](../../src/Apps/Zanance/docs/quality/ledger-account-amounts-readable.md).

## D-345 - Owner-scoped emulator accessibility qualification

Only on verified Zanance_Storage_Review/emulator-5570, the owner permits temporary accessibility
settings with exact prior-value restoration. Capture system font_scale and secure accessibility_enabled/
enabled_accessibility_services first, including whether a key is absent. Restore an absent key by
deleting only that exact key, not by writing the literal null. Verify readback and bound services.
Existing caller ledger-rows can qualify retained monetary fixtures at real OS2.0 without a rebuild.
Do not confuse its NoFinancialSaveOrSystemSettings result with the external authorized OS mutation.
The D-345 case passes36 viewports; TalkBack binding/audio remain unaccepted speech evidence.
No Samsung, Windows settings, account, authentication, microphone or desktop playback is covered.
See [evidence](../../src/Apps/Zanance/docs/quality/android-os-accessibility-qualification.md).

## D-346 bounded independent runtime diagnostics

AndroidHome font-scale receipts require actual OS2.0/412dp and bind exact Mono GC mode; do not
reuse another mode's passes. EnableCodeSampling=true is explicit independent-caller-only opt-in.
Bound simpleperf to exact PID/user code/fp/time/size; verify absence of raw memory/register/stack
fields. Official debug data requires both matching ELF BuildId and GNU debuglink CRC. Never infer
a production fix from stripped nearest symbols or another user's workaround. Serial-major GC is
not adopted after the D-346 actual ANR. Preserve live PID after observation expiry and restore
captured emulator accessibility settings.
[Evidence](../../src/Apps/Zanance/docs/quality/android-os-accessibility-qualification.md).

## D-347 foreground-only financial observations

AndroidHome requires Activity.HasWindowFocus before root/context construction and rendered-caption
checks. Its retained OS receipts must prove OriginalWindowFocusRequired as well as GC mode, actual
scale/width, cases and unchanged inputs. Keep old geometry-only evidence provisional. A real ANR
can coexist with realized controls and does not become a pass after later progress. The rejected
precise-Tarjan environment/property/marker are removed; do not reintroduce them as a production fix.
[Evidence](../../src/Apps/Zanance/docs/quality/android-os-accessibility-qualification.md).

## Reader-preserving Android observation (D-348)

Use eng/benchmarks/Zanance.AndroidAccessibility/README.md and its installed-tool Build.ps1 for the
independent shell-only observer. Do not run stock uiautomator dump/events while qualifying TalkBack.
Allow only one UiAutomation client, decode UTF-8, preserve failed observations, and remove only the
hash-matched owned JAR. Settings/input remain separately authorized and require exact restoration
and actual foreground checks. Audio energy or native names alone do not qualify spoken output.

## Original Home measured-window contract (D-350)

The independent AndroidHome measure/compare/home-stages caller requires actual window focus before
each measured page/read-factory and after native layout. Legacy case packets without the explicit
flag refuse; unmeasured shell setup and cold startup are outside this receipt. Use retained marked
ciphertext/keys and a fresh comparison for a changed getter; never infer full-page speed from an
isolated provider microbenchmark. Current production dates keep original provider parsing after the
rejected D-350 trial. New focused tests use the already built Data test DLL with -method filters;
no unchanged full suite, native matrix or production APK needs replay for a withdrawn prototype.

## D-351 focused reimbursement verification

Build the Core test project with ContinuousIntegrationBuild=true, then run its compiled DLL with
-class '*ReimbursementTests' -class '*ReimbursementBatchTests' -class '*ZexPhase4ReportTests'
-class '*ZexPhase4KpiTests'. A wildcard is valid only at the start/end of each filter, not in its middle.
The independent AndroidHome caller accepts review_phase=reimbursements with fresh hexadecimal run/pass
identities. Read only the launched PID's report through Read-ReviewReport.ps1; observation expiry is not
failure. This phase checks Core tuples and original page publication on fictitious inputs, not whole-Home,
large native lists, device encryption or production data. Do not replay successful unrelated suites.
[Evidence and limits](../../src/Apps/Zanance/docs/quality/reimbursement-batch.md).

## D-352 original reimbursement-page qualification

Run-StoreDrafts.ps1 -ReimbursementsReview uses a fresh private installation/desktop and qualifies
18 original en/fa/de xLight/Dark x360/412/1280 contexts. -ReimbursementsLanguages de selects only the
missing German contexts; -ReimbursementsVisibilityOnly and -ReimbursementsEmptyOnly select narrower
rendering checks instead of replaying completed failures/retry. The final combined mode checks both
visible boundaries, loading/failure/retry and the separate empty viewport's actual non-overlap.
Review its actual images; passing bounds alone did not detect the initial Header/EmptyView overlap.
AndroidHome's reimbursement-page-fixed phase reuses its completed fictitious baseline without
migration/reseed and retains immutable source/input-bound cases. Read only the original live PID;
observation expiry is not failure or permission to restart. Restore the ordinary Windows identity
with its strict build after private review. [Evidence](../../src/Apps/Zanance/docs/quality/reimbursement-page.md).

## D-353 original Home template reload

Run-StoreDrafts.ps1 -HomeTemplatesReview runs six independent private-desktop language/theme contexts
at412px with original controls and no financial commands. AndroidHome phase home-template-reload uses
a small independent fixture and the original Release/AOT Home source. Keep baseline failures and
observe the same PID after timeouts. [Evidence](../../src/Apps/Zanance/docs/quality/home-template-publication.md).

## D-356 independent original editor reviews

Run eng/scripts/Run-StoreDrafts.ps1 -EditorTextReview for original account-choice/default-icon/debt
components, or add -EditorDebtDetailsOnly for the isolated second disclosure continuation. Both
use a fresh owned package/private non-input desktop and18 en/fa/de xLight/Dark x360/412/1280
Windows layout-stress contexts. No owner desktop input or system scale change is permitted.
The Android editor-text phase uses actual authorized emulator font_scale2.0/412dp and the
original modal forms; the host owns exact prior-settings capture/restoration.
[Current checks and limits](../../src/Apps/Zanance/docs/quality/android-editor-os-text.md).

## Incremental Android manifest overlays (D-359)

AndroidManifestOverlays.targets tracks requested overlay paths, precedence and SHA256 content in
the current intermediate directory before the SDK merger. Switching a project-specific overlay
option on/off, editing with a preserved timestamp or removing the last overlay invalidates the
merge; unchanged inputs keep the output. A missing requested file fails the build. Never infer
variant fidelity only from evaluated MSBuild items: inspect the actual APK manifest.
Run eng/tests/AndroidManifestOverlays.Tests.ps1 for the seven isolated process regressions, without
an application build or device. The AndroidHome caller's existing EnableCodeSampling option is
independent-only; the ordinary app must remain non-profileable and satisfy its explicit privacy
variant. [Qualified packages and runtime limits](../../src/Apps/Zanance/docs/quality/android-manifest-overlay-incremental.md).

## Original Android root retirement (D-360)

The independent AndroidHome caller's root-retirement phase requires an existing nonredirected
owned root. review_assert_roots checks eight replaced original Shell instances, one active native
root and one translated toolbar subscription. review_back_names adds the actual About/native Back
operations for en/fa/de xLight/Dark, preserving the open detail through display changes. Derived
receipts bind comparison identity, PID and source MVID; explicit collections belong only to this
independent ownership check, never production. Capture and exactly restore emulator settings for
any separately authorized OS-scale continuation. Do not rerun completed matching native matrices
or infer general finalizer/ANR, physical-device, reader or release acceptance.
[Original failures and final evidence](../../src/Apps/Zanance/docs/quality/android-retired-navigation.md).

## Minimum-width editor continuation (D-364)

Run-StoreDrafts.ps1 -EditorTextReview -EditorNarrowActionsOnly reviews original changed-command
consumers on a separate non-input desktop. -EditorOnboardingFooterOnly checks all three wizard
steps without invoking Next/Finish. -EditorOnboardingContexts accepts a unique comma-separated
subset such as de-Light-360,de-Dark-360; retain earlier context receipts and the original live
process when observation expires. Footer progress.json identifies the last original operation;
it is not a completion receipt. Restore ordinary build identity after independent qualification.

The independent Android editor-text review accepts360 or412dp only with actual font_scale2.
Explicit review_editor_controls values entry-viewport,changed-actions,onboarding-footer restrict
continuation to affected native controls. Bind review_editor_baseline and its source MVID to
the preserved original private input receipt; do not create a replacement fixture. Caret/glyph
endpoint checks restore selection and never alter text, font or scroll policy. Restore the exact
captured emulator settings after the terminal report. [Evidence](../../src/Apps/Zanance/docs/quality/narrow-editor-actions.md).

Each store-draft run now uses the SDK artifacts output layout under its own build subdirectory,
including distinct dependency outputs and intermediates. Resolve exactly one original executable
and compiled manifest there. This preserves live prior review binaries/PRI resources and ordinary
development output; never reuse a shared OutDir for parallel or retained independent processes.

## Independent Android reader gestures (D-367)

The service-only eng/benchmarks/Zanance.ReaderGestures project is outside the shipping graph.
Use only the explicitly authorized emulator, fresh original focused windows and safe original
captions; preserve installed TalkBack. Public gesture completion requires actual reader focus/
page evidence. Capture and restore exact accessibility/TTS/font/display values and remove only
the helper after terminal evidence. Do not use ordinary adb input as reader-gesture acceptance,
stock uiautomator alongside TalkBack, root or system/security changes on a phone.
[Build/transport limits](../../eng/benchmarks/Zanance.ReaderGestures/README.md) and
[scoped original qualification](../../src/Apps/Zanance/docs/quality/android-reader-glyphs.md).

## Original Android fee/lifecycle continuation (D-369)

The independent Zanance.AndroidHome destination-fee phase qualifies original native stored-fee
Saves against complete fictitious ledger fields. Retained receipts bind actual caller/Data MVIDs,
original comparison/input and committed rows; missing original native-operation receipts refuse
acceptance. Do not replay successful cohorts, reset fixtures or restart on observation expiry.

For ComboChevron retirement, check the original signed main APK after no-reset upgrade. Original
Settings dropdowns can create non-focusable native PopupWindows absent from the active-window
uiautomator tree. Bind visible surface/parent/package/session-PID evidence to the original action;
close without currency selection and recheck after actual child navigation. This is not list-row
selection, TalkBack/speech or general GC/ANR acceptance. Preserve exact original app/OS state.
[Recorded failures, seven-migration continuation and packages](../../src/Apps/Zanance/docs/quality/destination-fee-android-lifecycle.md).

## Withdrawn current-row native experiment (D-372)

Native protocol or isolated read timing is not original Home acceptance. Require exact original
financial presentation, full protected schema/typed rows, actual window focus and paired complete
page publication/layout. An incompatible historical presentation baseline must refuse; retain it
and establish a fresh original control rather than weakening equality. Preserve exact live
processes across observation deadlines. After rejecting an artifact-only build override, restore
ordinary Data output and remove the experimental independent package bytes while preserving its
fixtures/main data. Never add a native build/runtime dependency for an unproven whole-page gain.
[Measured scope and restoration](../../src/Apps/Zanance/docs/quality/home-native-row-transport.md).

## Original unreviewed Home qualification (D-373)

Use the independent prepare-unreviewed phase on a new marked corpus; confirmed-only workloads
cannot qualify its account-scope path. Keep the original context's native write guard when setting
fictitious review state before first encryption. A narrowly validated failed reference can continue
without reseeding or replacing keys; existing protected datasets never become preparation targets.
Original context-bound in-memory expected data and full typed-row verification protect other fields.
Require native focus, original complete presentation and exact source/APK/comparison through any
terminal continuation. Preserve actual GC crashes; skip only completed same-source cases and never
call a retained completion a GC fix. Restore exact original main/profile/OS and release the phone.
[Build, failure and timing receipts](../../src/Apps/Zanance/docs/quality/unreviewed-account-scope.md).

## Original Home review-count qualification (D-374)

Run `./eng/scripts/Run-StoreDrafts.ps1 -HomeReviewCountReview` for18 original Home contexts in a fresh
guarded Windows identity/non-input desktop. It uses a distinct two-account/one-FX-transfer SQLite
fixture and checks both review captions without modifying main data. The independent Android Home
caller has `home-review-count`; `home-review-count-fixed` explicitly continues the preserved fixture
after terminal observer failure and retains only its qualified English packets. It uses the actual
device width and native display-digit mapper. The review constant is scoped to the independent
project; ordinary main Release does not include the diagnostic. Never restart after mere observation
expiry, reseed an existing fixture, change OS digits or treat geometry checks as spoken-reader proof.
[Exact runtime evidence](../../src/Apps/Zanance/docs/quality/home-review-count.md).

## D-375 original Home header review

`./eng/scripts/Run-StoreDrafts.ps1 -HomeAddHeaderReview` runs original en/fa/de Light/Dark Home layouts
at 360/412/1280 on the guarded independent non-input desktop. It checks original Add/viewport geometry,
full bottom scroll and complete fictitious field equality. The separate-desktop modal COM failure is
retained; this mode does not claim Windows modal acceptance. The distinct AndroidHome caller supports
`home-add-header` and retained-fixture `home-add-header-fixed`. Require original native controls' own
shown/focused windows and await async Cancel. Keep original draft confirmation, data and public
same-process reports; observation expiry alone never authorizes restarting the process or fixture.
Only the explicitly authorized independent emulator changes OS text/display settings, restored exactly.

## D-376 original getting-started review

`./eng/scripts/Run-StoreDrafts.ps1 -GettingStartedReview` runs the guarded fresh Windows fixture and
original Home at18 en/fa/de Light/Dark, 360/412/1280 contexts. The distinct AndroidHome caller accepts
`getting-started`; it checks original native action availability and absence of strikethrough at
actual OS 200%/360 dp. The new getting-started.db is separate from every retained count/performance
fixture and main profile. Compare all account/entry/settings/plan/budget fields before/after. Exact
emulator configuration restoration and same-process public-report rules remain mandatory. This mode
does not save financial data in the main application or repeat unrelated tests/benchmarks.

## D-377 original empty-tag comparison

Use retained independently owned protected workloads and `home-stages` on the current original page.
Record standard tag converter/reader type names outside the measured interval; preserve the exact
baseline and comparison identities plus complete schema/typed-data proofs. A command-line-only
artifact-local `CustomAfterMicrosoftCommonTargets` override can compile the two exact HEAD reader
sources for an ARM control without modifying tracked production files. Verify the original member
absence and subsequent ordinary candidate member presence; never persist the override or reuse an
ambiguous build output. Copy immutable signed packages before installing. An authoritative native
exit may continue only unfinished retained cases on the same package/data; a timeout never authorizes
a restart. [Exact D-377 evidence](../../src/Apps/Zanance/docs/quality/home-empty-tag-materialization.md).

## D-378 exact native code and safe local diagnostics

Resolve the actual packaged Mono ELF against official debug data using both GNU BuildId and
.gnu_debuglink CRC, then containing symbols and DWARF lines. A containing push_object frame may
point into inlined bridge_object_forward; nearest exports cannot identify that site. Reuse the
verified pyelftools wheel without installing it. Inspect runtime diagnostic implementation first:
nursery-canaries prints raw object/nearby heap bytes and is incompatible with code-only diagnostics.
A command-line artifact-only independent override may enable check-remset-consistency with a fixed
runtime mode receipt; never promote it or persist it in Main/global settings. Clean its generated
outputs and restore the exact ordinary independent APK/Main/OS after the bounded run. No arbitrary
environment logging, registers, raw stack, heap dump or private key/database export.
[Exact D-378 result](../../src/Apps/Zanance/docs/quality/android-home-bridge-fault.md).


## D-385 - Original retained CSV input reviews

Run `eng/scripts/Run-StoreDrafts.ps1 -CsvInputReview` for the original seven-refusal18-context matrix.
For a newly changed empty-only branch, add `-CsvInputEmptyOnly` instead of replaying successful refusal
checks. Each fresh private non-input desktop/application identity owns its fictitious data, build,
images and terminal receipt. Keep process-local font stress distinct from real OS/reader acceptance.
Independent Android phases `csv-input` and `csv-input-empty` run the original page/commands with only
selected-file effects supplied. Retained receipts bind comparison, input digest and source MVID;
never reuse a receipt across source changes. No financial Import/Undo or raw-file archive is authorized.
[Exact D-385 scope](../../src/Apps/Zanance/docs/quality/csv-input-safety.md).


## D-387 - Preserve CSV financial meaning at quote boundaries

Use `Run-StoreDrafts.ps1 -CsvInputReview -CsvQuotationOnly` for the changed misplaced-quote error
only; it is mutually exclusive with the empty-only continuation. Independent Android phase
`csv-quotation` uses the same original page/command and source-bound receipts. Neither mode repeats
the six unchanged resource refusals or invokes Import/Undo. [Evidence](../../src/Apps/Zanance/docs/quality/csv-quotation-integrity.md).

## Approved ECB reference source (D-389)

The service default remains UnconfiguredMarketProvider. Explicit Market:Source=ecb enables the
owner-approved daily adapter and authenticated HTTPS market routes; Market:LocalQualification remains
a separate fictitious-only switch. Unknown source values fail closed. Metadata, cache and history
never fetch. The feed is fixed, standard-TLS, no cookies/redirects/retries; at most262144 actual
uncompressed bytes,64 currency rows, depth4 and12 seconds for the original shared lookup.

The optional eng/hosting/compose.ecb-reference.yml overlay selects this source and gives only the API
outgoing edge connectivity, without a new listening port. Base deployment isolation is unchanged:
`docker compose -f eng/hosting/compose.yml -f eng/hosting/compose.ecb-reference.yml config --quiet`
validates configuration only. Public DNS, mail, tunnel sign-in and merchant activation remain separate.
No production deployment was performed for D-389. The actual public source probe is an explicit test,
excluded from ordinary CI; do not repeatedly call it or treat a test-server HTTPS scheme as real TLS.

## Retained account activation review (D-394)

Run eng/scripts/Run-StoreDrafts.ps1 -AccountTabRefreshReview for six en/fa/de light/dark original
Windows Shell/account contexts on the existing isolated non-input desktop. This review uses a
separate fictitious SQLite fixture with explicit edits; its completion requires that fixture's
identity/metadata and the unrelated base store to remain intact, not an unchanged financial fixture.
Use -AccountTabRefreshContinuation <exact32hexOriginalRun> only for the documented partial fixture:
retain two completed English images/receipts and the already accepted13EUR edit; qualify native
Persian digits without repeating that save, then finish remaining contexts. No restart of an
expired live process or general restart/reseed option is added. Exact package/fixture guards remain.

## Plans title action review (D-395)

eng/scripts/Run-StoreDrafts.ps1 -PlansAddHeaderReview uses the existing independent non-input Windows
desktop and original fictitious seeded data. It checks18 en/fa/de light/dark width contexts and saves
plans.png/empty.png plus geometry in each directory. Only view-model presentation is emptied; stored
schedules are not deleted. The independent AndroidHome caller's plans-add-header phase checks the
original native Add and unsaved Cancel, without touching the Main package or changing OS settings.

## Focused original reconciliation review (D-396)

Run `eng/scripts/Run-StoreDrafts.ps1 -ReconciliationReview` for original account commands/native
consent in six en/fa/de light/dark contexts, using a separate fictitious financial file and18 scoped
width images. It does not drive the owner input desktop. The optional exact32-hex
`-ReconciliationContinuation` is restricted to the retained terminal input-pattern observation;
never restart a live process or reseed its financial file. After all six financial cases are accepted,
`-ReconciliationVisualOnly` with that original continuation file refreshes only visible error images,
with no financial Save and complete accepted-input preservation. Incompatible/incomplete flags refuse
before build/desktop work. See the D-396 quality evidence for the original failures and exact limits.

## Retained original settlement outcome (D-397)

`eng/scripts/Run-StoreDrafts.ps1 -SettlementCommitReview` exercises original modal Save/Back in six
en/fa/de light/dark contexts with18 scoped width images and separate fictitious rows. Preserve
accepted receipts and any global error: the scoped completion receipt does not override an unrelated
Home read failure or make the whole runner green. Never repeat accepted money to repair observation.
The independent AndroidHome settlement-commit phases compile original Release/AOT sources and use
only their exact owned run. Fixed/observed continuations require the original terminal evidence and
actual saved-row checks; the already accepted first case is read back, never posted again. The
native owned-modal return port is scoped to the independent caller, not main-route acceptance.

## D-398 - Original Home refresh qualification

Run-StoreDrafts.ps1 -HomeRefreshReview checks six actual native Home contexts in its independent
installation/desktop. Preserve those completed cases; -HomeRefreshReview -HomeRefreshReferencesOnly
qualifies one additional account-reference change on final logic without repeating the matrix.
The independent AndroidHome phases home-refresh and home-refresh-references use original production
VM/page sources and isolated plain SQLite files. Preserve the exact run/report identities and
completed cases when upgrading the caller for the additional path. They do not qualify Main
encryption, phone/iOS, performance or general stability. Restore Main and OS and retain failures.

## D-399 - Original occurrence commit qualification

Run-StoreDrafts.ps1 -OccurrenceCommitReview uses a fresh independent Windows installation/desktop:
eight original commands, then18 read-only native message views at360/412/wide in en/fa/de light/dark.
Inspect the retained -popup images from CaptureWindowAsync, not the underlying root frames. Do not
replay accepted money to repair an image observer. AndroidHome occurrence-commit uses a fresh owned
run and actual original page/buttons/alerts in Release/AOT. Its narrow fictitious dialog-stage channel
identifies17 original answers; preserve the exact PID/fixture and one invocation intent per answer.
Native dialog focus may carry its title; match the original activity and exact owned UI content.
Store receipts as UTF-8, keep initial observer refusals and restore Main/OS after terminal proof.

## D-400 - Original state-conflict qualification

Run-StoreDrafts.ps1 -OccurrenceStateReview uses four separate original financial fixtures and six
unchanged en/fa/de light/dark412px native message views. Test-OccurrenceStateReview.ps1 validates an
already completed original receipt without process launch/input. Exactly four known deliberate
InvalidOperationException reports are accepted only with matching process/case/refusal/image proof;
all other failure lines remain rejected. Preserve original error.txt and original completion.
Three negative synthetic receipts prove unexpected-failure, changed-row and wrong-process refusal;
the positive shape is separately checked. These are tooling checks, not new app unit tests.
The independent Android phase occurrence-state uses four original commands and11 exact owned
native answers. Do not repeat accepted fixtures after a capture/parameter/observation failure.

## Original retained occurrence publication (D-401)

Run Run-StoreDrafts.ps1 -OccurrenceRefreshReview in a fresh independent Windows identity/desktop.
Six en/fa/de light/dark contexts capture42 original app-window frames:412px loaded and360/412/1280px
loading/error. The original real tab SelectionItem and native Retry/payment buttons operate only
owned fixtures. The independent Android phase occurrence-refresh requires a fresh run/comparison,
Release/AOT package and18 exact original translated-tab inputs, scoped to its PID/focused package.
Read-ReviewReport.ps1 observes only that same process and its public derived result. Shared reads,
covered failures, draft retention, target-change refusal and the pending writer/canonical accepted
reload are qualified once per platform. Preserve terminal receipts and accepted cases; no replay
after observation expiry. Main upgrades use only the complete signed offline package without reset.

## Original physical refund continuation (D-402)

Reuse a qualified complete signed candidate when source is unchanged. Journal each native action
and retain accepted money; a failed native observation is not a failed financial write. Before typing
a replacement, require the exact original focused field and verify its selected text is empty after
Ctrl+A/Delete; do not use clipboard, guessed caret/deletion success or device setting changes. Native
list edges may clip an otherwise realized target: scroll only the actual owned viewport until its
whole target is visible. Continue remaining reads on the same cold process; never replay completed
financial actions or restart after observation expiry. Final public Main/profile/OS/package readback
and explicit phone-turn release close the bounded workflow.

## D-403 - Complete independent original portable restore application

Use `eng/scripts/Build-AndroidApk.ps1 -Offline -Project eng/tests/AndroidPortableRestore/Vafadar.Zanance.AndroidPortableRestore.csproj -Output artifacts/android-portable-restore-d403`.
This fixed-identity Release-only caller compiles original production sources/XAML/resources/AOT inputs
in its own output without altering Main. It adds no diagnostic activity, private storage endpoint or
financial/authentication shortcut. Both complete caller and independent receiver strictly build and
verify signed/offline; permanent Main identity and Debug are refused before native effects. Do not
reset permanent Main for qualification. `eng/tests/AndroidPortableRestore/README.md` documents the
original grant/retained-file/uninstall/reinstall/first-run/cold/re-export sequence and fixed-fictitious
whole schema/typed-row verifier. Only original Settings.UpdatedAt integer ticks may advance. Keep
accepted money and late receiver receipts; repair only the failed observer. Exact native/package/
restoration limits: src/Apps/Zanance/docs/quality/physical-portable-fresh-install.md.

## Original CSV financial snapshot qualification (D-405)

`./eng/scripts/Run-StoreDrafts.ps1 -ImportFinancialReview` builds a fresh guarded Windows identity on
an independent desktop. `-ImportFinancialContinuation <original-run-id>` retains completed owned
fixtures/captures and finishes only a pending accepted batch/Undo without importing it again. A raw
source/command review is not a bank-provider or physical native-consent acceptance. The independent
AndroidHome caller's `import-financial-review` phase qualifies the same original sources under Release
AOT; read only its comparison-bound report from the original live PID. Keep full final Main APK and
privacy/signature verification separate from this caller; restore original Main and OS values.


## Original split/join review qualification (D-407)

`eng/scripts/Run-StoreDrafts.ps1 -SplitFinancialReview` uses a fresh separate store-preview identity,
performs only two original financial cases and retains them for36 read-only captures across en/fa/de,
both themes and360/412/1280 widths. It validates complete original source/dependency rows on refusal,
known committed notification outcomes, exact totals and attachment ownership. The original base store
and owner input desktop remain unchanged. Do not rerun accepted money to repair a capture/observer.

The independent AndroidHome caller supports `split-financial-review` with a fresh owned32-hex run
and comparison; use its comparison-bound public report. Its original modal owns native focus, so an
Activity-focus wait behind that modal is invalid. `review_resume_split_after_refusal=true` is strictly
a retained-original continuation: the exact baseline failure plus successful refusal receipt and
unchanged original one-row fixture must exist, with no acceptance/join fixture. It restores only the
refused presentation and continues pending acceptance; it never reseeds/repeats the recorded refusal.
This diagnostic path is excluded from shipped Main. See original-split-financial-review.md in the app
quality documentation for retained failures, signed package hashes and actual qualification limits.


## D-408 - Original holding forms

Run `eng/scripts/Run-StoreDrafts.ps1 -HoldingFinancialReview` with a fresh independent identity.
Two original financial cases precede36 read-only observations. The bounded optional
`-HoldingFinancialContinuation <original-run>` requires the exact original pre-command failure,
unchanged one-account/type/location fixture and no financial rows or progressed receipts. It reuses
the original financial file; the caller/base store remain independently owned. Never use it to
repeat accepted finance. The AndroidHome `holding-financial-review` phase uses the original modal's
own native focus and comparison-bound public report, with fresh owned fixtures and12 read-only
feedback observations. The final Main APK remains a separate complete signed offline Release build.


## D-409 - Original holding refund corrections

`eng/scripts/Run-StoreDrafts.ps1 -HoldingRefundReview` runs two original refused/corrected native forms
in independent owned SQLite, captures four original live states and36 read-only rejected-draft caption
states in en/fa/de light/dark360/412/1280. The latter rehydrate original refusal receipts with retired
authority; they do not repeat money or qualify an editable refused form. `holding-refund-review` in
AndroidHome uses fresh owned run/comparison and the original modal's own native focus, then exposes
two cases/four live/twelve read-only observations through the comparison-bound public report. Preserve
same-PID observation delays. See the app quality/holding-fee-refund-limits.md for original outcomes.


## D-410 - Original holding-fee input and retained caption continuation

Run StoreDrafts with `-HoldingFeeInputReview` for two original native malformed/negative refused and
corrected forms plus one caption matrix, not duplicate matrices for identical feedback. Its bounded
`-HoldingFeeInputContinuation <run>` requires the exact original terminal failure, four original live
captures, six completed English contexts and original accepted/refused financial files. It runs only
twelve pending read-only captions with no financial commands. Keep original failures and inspect all
successful own-window images across original/continued runs. Native expectations apply the same
existing digit conversion as rendering. Android uses a comparison-bound original-file continuation;
see its README and app quality/holding-fee-input.md. Never restart merely after observation timeout.

## D-411 - Independent holding-fee action review

`Run-StoreDrafts.ps1 -HoldingFeeActionReview` uses an isolated non-input Windows desktop and fictitious
root store. It captures26 original detail states (holding-fee18 across en/fa/de, light/dark and
360/412/1280; ordinary-expense6 at412; principal1; income1), checks one original native refund draft
open/Cancel without Save, and whole stored-row equality. It never inputs on the owner's desktop.

The independent Release/AOT Android `holding-fee-action` final caller qualifies24 read-only action
surfaces and actual accessibility-node names. Its precise failed-observer continuation is documented
in eng/benchmarks/Zanance.AndroidHome/README.md; retain original seeded rows and failure records.
Independent modal failures are not physical acceptance. Do not replay successful finance or unchanged
full suites; validate the complete signed offline candidate and restore original emulator/phone
Main, profiles, public rows and OS after the scoped original lifecycle.

## D-412 - Original quantity refusal and field-caption continuation

`Run-StoreDrafts.ps1 -HoldingQuantityInputReview` creates two fresh owned original SQLite scenarios,
invokes each refused/corrected holding Save once, preserves original money/refund/price and captures
four live plus18 read-only normal-scale field-caption contexts. The original D-412 preliminary run
5b014fc0293b47579399fb9d7d5e94cf completed before field placement changed. Only the new field contexts
continue with `-HoldingQuantityContinuation <original-completed-run>` in a fresh independent run;
the exact original completed proof and accepted/refused files are required. Its18 captures retain
original Cases/PID, Live empty and zero commands; never reseed or replay accepted quantities.

The independent Android phase is holding-quantity-input. Its completed-result continuation uses
review_resume_quantity=true, the same review_run, a fresh review_pass and original successful
review_baseline. Read exact original stores/receipts before/after, retire the raw presentation and
check named field/entry placement, complete native text/viewport and actual modal focus. A timeout
observes the same original process. Verify complete signed offline packaging and exact Main/OS/hash
restoration; physical-device and broad runtime gates remain separate.

### Original manual-price review (D-413)

Run `./eng/scripts/Run-StoreDrafts.ps1 -HoldingPriceReview` for one fresh independent original price
workflow and caption matrix. `-HoldingPriceContinuation <exact-completed-run>` validates the original
completed receipts and actual file, retaining original Cases and using zero financial commands for
read-only captions. It never reseeds or touches Main. Inspect every app-owned capture. The independent
Android caller's `holding-price` phase similarly uses a fresh run/pass; its narrowly guarded
`review_resume_price` requires the exact `review_baseline` pre-invocation viewport failure and retained
fixture with no command receipts. It does not replay a financial command or seed. Keep original
failure and continuation evidence separate; OS confirmation dialogs, phone and iOS remain gates.

### Original holding history delete/Undo (D-414)

`./eng/scripts/Run-StoreDrafts.ps1 -HoldingHistoryReview -TimeoutSeconds 1800` creates one independent
original native financial workflow and later retired captions. Inspect78 app-owned captures. A
`-HoldingHistoryContinuation <exact-completed-run>` qualifies only72 read-only caption scenes from
its original complete-financial receipt and exact complete-row digest. It never recreates short Undo
authority, reseeds data or repeats accepted writes. Original Cases/Live retain their original PID.
The independent Android `holding-history` phase similarly takes a fresh run/pass. Its optional
`--ez review_history_captions true` requires `review_baseline` equal to the exact original completed
financial invocation. An observation timeout keeps the original process alive; do not restart it.


## Original holding archive qualification (D-415)

Run `eng/scripts/Run-StoreDrafts.ps1 -HoldingArchiveReview` for one independently owned Windows
financial file, original native Cancel/refusal/archive and36 read-only caption contexts. After an
original financial receipt completes, `-HoldingArchiveContinuation <original-run>` requires its
exact completed file/digest and invokes zero financial commands. Preserve original PID/Cases and
accepted files. The independent Android `holding-archive` phase has the same original boundary;
observe its exact live PID and comparison through Read-ReviewReport.ps1. A caption continuation uses
`review_archive_captions=true` with the original baseline. Never replay accepted archive/seeds for
observer repair. The explicit confirmation port does not claim native OS-dialog acceptance.


## Original type form qualification (D-416)

`eng/scripts/Run-StoreDrafts.ps1 -HoldingTypeReview` uses one independent actual financial file for
original stale-source refusal, accepted edit and accepted creation, then54 permanently retired native
caption contexts. `-HoldingTypeContinuation <original-run>` requires its exact complete financial
receipt/file/digest and invokes zero financial commands. The independent Android `holding-type`
phase has the same original boundary; observe the exact original PID/pass. Its caption-only
continuation uses `review_type_captions=true` with the original baseline. Preserve accepted Cases,
PIDs, raw fields, failures and files; never replay original seeds/commands to repair an observer.

## Original holding overview qualification (D-418)

`eng/scripts/Run-StoreDrafts.ps1 -HoldingOverviewReview` owns one original independent overview and
empty file,18 en/fa/de/theme/width contexts and56 captures. After a terminal observer failure,
`-HoldingOverviewContinuation <original-run>` retains exact original files/Case PIDs and only finishes
pending contexts. Once completed, changed loading/Retry copy alone uses `-HoldingOverviewCaptionsOnly`
with both that original continuation id and `-HoldingOverviewCompletedRun <completed-run>`; this adds
36 caption captures without reseeding or replaying ready/navigation checks. Keep original files and
checkpoint hashes. Android phases `holding-overview` / `holding-overview-captions` retain one original
run and completed baseline; observe the exact live PID/pass using Read-ReviewReport.ps1. These are
independent native controls plus navigation effect ports, not physical-device/OS-route acceptance.

## Retained-source detail qualification (D-419)

`eng/scripts/Run-StoreDrafts.ps1 -HoldingDetailReview -HoldingDetailSourceRun <completed-overview-run>`
opens the exact retained original-overview.db without migrate/seed and captures57 Windows states.
Only a later query correction uses `-HoldingDetailQueryOnly -HoldingDetailCompletedRun <completed-detail-run>`
with the same source/review flags: two pending query captures, not the completed matrix again.
Preserve original reports/PIDs and files. Android phase `holding-detail` requires the retained
overview review_run and its completed original review_baseline, plus a new review_pass. Observe the
same live PID/pass through Read-ReviewReport.ps1; no final report yet means observation is pending,
not permission to restart. Actual native controls and navigation effect ports remain distinct.
The final `holding-detail-retired-callback` continuation requires the completed `holding-detail`
baseline and retained run; it checks only the new retired readback callback, without redoing its
23 accepted UI observations or financial commands.

## Original holding-place native qualification (D-420)

Run-StoreDrafts.ps1 -HoldingLocationReview creates one independent fictitious metadata fixture on a
non-input Windows desktop. Keep its original accepted receipt/rows and process; never replay its
create to repair an observer. The independent AndroidHome holding-location phase requires a fresh
run and explicit review_pass. It invokes the original Add/read-only refresh native buttons once;
the name dialog is an explicit effect port, not OS keyboard/dialog acceptance. Caption models copy
a known original outcome with retired authority. Observe only this package/PID, keep the live
process across startup observations, preserve system settings and restore Main after the terminal
report. No Main private store/key or unrelated archive is read. Build complete signed offline Main
with the canonical APK script, then check package/signature/native bytes. See
[original evidence](../../src/Apps/Zanance/docs/quality/holding-location-creation.md).

## Original holding-event covers and Retry (D-421)

Run-StoreDrafts.ps1 -HoldingEventPublicationReview qualifies the actual original form on a separate
non-input desktop. It seeds one independent source, then performs only original read/failed-read/
native Retry and explicit query controls. Preserve original files, PIDs and completed observations;
never seed again to fix an observer. The independent AndroidHome holding-event-publication phase
requires fresh review_run and review_pass. Native UIA/PerformClick invokes Retry; raw77 is an explicit
model draft control followed by actual native Entry rebinding, not keyboard or financial acceptance.
Compare every typed source row, base rows and exact original language/theme. Build complete signed
offline Main with the canonical script and verify package/signature/native bytes; restore the same
Main/Home/OS after the terminal caller. [Evidence](../../src/Apps/Zanance/docs/quality/holding-event-publication.md).

## Original native currency observation (D-423)

The independent AndroidHome currency-input phase requires fresh review_run/review_pass and a fresh
nonredirected report directory. It hosts the actual AccountFormView with no Save surface, observes
one original chevron pointer, waits for visible original native rows and invokes one visible native
GBP button. Read the action name from CreateAccessibilityNodeInfo: MAUI semantic delegates need not
populate raw ContentDescription. Inspect only this package's application panels; active-window-only
uiautomator dumps can omit a nonfocusable popup. Complete the original report before stopping the
caller, compare stored accounts/entries and resume the same Main PID. A delayed observation never
justifies replaying a pointer, seed, accepted command or install. This is not spoken-reader acceptance.

## Original entry-detail attachment review (D-424)

Run-StoreDrafts.ps1 -EntryAttachmentsReview uses one new isolated original source and54 native
caption/Cancel observations. For only the changed file-name rows, use -EntryAttachmentsReview
-EntryAttachmentsSourceRun <completed-run>: it requires the exact original54-case completion and
reads that same database without migrating, seeding or replaying accepted commands. Its18 original
row observations may finish before a later native process exit; retain both the report and exit,
never silently convert a failed host result to success. The independent non-input desktop and
app-only window/popup capture remain mandatory. Normal Android uses the canonical full offline
signed package and a new fictitious profile, one original picker/deletion/Undo sequence and terminal
cold. Preserve original deadlines, remove only hash-matched owned inputs and restore Main/OS/profile
set. See application quality/entry-attachment-ownership.md for exact reports and limits.

### Original editor Save review (D-425)

Run eng/scripts/Run-StoreDrafts.ps1 -EntrySaveReview for one original native editor Save, with a
fictitious receipt and deliberate postcommit notification failures, on its own non-input desktop.
-EntrySaveSourceRun accepts only a failed original source with no ledger entries; it reuses that
exact account/file without migration or reseeding. Completed accepted sequences cannot replay.
The page is constructed before its query, matching Shell; Save is a fixed footer, not a child of
the scrolling form. Retain target geometry, original source, failures and completion receipts.
No unchanged multilingual/layout matrix is required for this non-layout handoff correction.

### Original transaction detail review (D-426)

`./eng/scripts/Run-StoreDrafts.ps1 -EntryDetailReview` builds a unique independent Windows Debug
identity on its non-input desktop. It reads one isolated original fixture across the language/theme/
width matrix, checks loading/failure/native Retry and retained command ownership, then invokes only
two explicit metadata actions. It never performs a financial Save or opens a desktop document viewer.
Preserve the original process and receipts on observation expiry. `-EntryDetailSourceRun <id>` is
restricted to the exact terminal pre-action JSON observer failure and verifies its unchanged original
file; it never seeds a replacement. A completed metadata/financial action is never replayed to repair
an observer. The independent AndroidHome `entry-detail` phase compiles original sources/XAML in
Release/AOT and retains its separate fixture. See the detailed D-426 quality record for scope and
negative evidence; these commands are not general runtime, physical-device or release acceptance.

## D-427 - Complete original transaction editor publication

Run-StoreDrafts.ps1 -EntryFormReview uses a separate non-input Windows desktop and one original
fictitious source. -EntryFormSourceRun <id> continues only that retained failed sequence; completed
stages and accepted actions are not replayed. The independent AndroidHome entry-form phase compiles
actual sources/XAML in Release/AOT. Its continuation retains the same review_run, original manifest,
financial acceptance and native-action receipts. Native post-dialog observation awaits the original
focused field; caption geometry uses visible line extents rather than trailing whitespace. Keep
all negative evidence and original Case PIDs. These are focused app/source checks, not general
runtime, physical-device, spoken-accessibility or release acceptance. See the D-427 quality record.

### Original transaction workflow review (D-428)

Run-StoreDrafts.ps1 -EntryWorkflowReview exercises actual nested account/aggregate pages in the
independent non-input development host. Preserve its original32hex run. -EntryWorkflowSourceRun
continues only that accepted original file after its source process exits. -EntryWorkflowAggregateOnly
requires the original aggregates and no accepted financial Save. -EntryWorkflowCaptionsOnly reuses
accepted Save/Undo rows for readonly dialog contexts; -EntryWorkflowBlockedOnly continues only the
changed blocked dialog and presentation-only Home/Transactions notices. Never replay a known or
ambiguous metadata/financial command to repair an observer. Popup files accompany the page render.
The independent Android entry-workflow phase compiles production pages in Release/AOT and owns
fresh fictitious files. Read the original process report; retain accepted action checkpoints before
fallible observers. It never authorizes ordinary-installation financial writes or system changes.
See the D-428 quality record for exact original runs and final package evidence.

## D-429 - Original plan form qualification

Run-StoreDrafts.ps1 -PlanEditorReview uses a fresh independent installation on its non-input desktop.
Its actual production pages invoke one account Save, one new plan Save and one historical split Save;
fictional payment history is seeded once and exact typed rows are retained before fallible observers.
-PlanEditorSourceRun <original32hex> continues only an exited original process with actions-complete
and unchanged full typed rows. It never replays account/financial acceptance; completed contexts and
initial loading/Retry observations stay retained. The independent Android plan-form phase compiles
these same production pages in Release/AOT. Keep the exact run/pass/PID and public report receipts.
Timeout is an observation bound, never permission to restart/reseed/repeat accepted work.

-PlanEditorCaptionsOnly rereads only changed loading/Retry/missing captions against the exact original
completed-action file. -PlanEditorArchivedOnly checks one original archived reference after an explicit
fictitious metadata boundary. Neither replays native account/plan acceptance. Android explicit empty
continuation compares the original prepared rows and refuses an accepted account; completed-action
continuation compares its exact actions-complete rows, skips completed readonly contexts and never
replays their accepted writes. Preserve failed intentions and public reports. Wait for the actual
owned page focus across a system dialog; do not declare such a dialog a product raw-field mismatch.
D-429 packaging may use process-local UseArtifactsOutput/ArtifactsPath with the existing APK script
to place intermediates on a spacious task-owned drive. Restore those environment values afterwards.
Keep the final complete signed candidate in its recorded project-owned artifact folder.
## Original plan monetary-context review (D-430)

`Run-StoreDrafts.ps1 -PlanEditorReview -PlanMoneyReview` runs only the original monetary-context
matrix in a fresh independent Windows installation and non-input desktop. Source/destination
format, changed-unit refusal and explicit reentry are checked on the actual production page.
One native plan Save is consumed before fallible observations; no ledger payment is posted.
Retain the exact original source and complete observations when continuing. A live completed
source may be read-only evidence only after its verified money-Save checkpoint; no writer is replayed.
`-PlanMoneyDebtOnly` additionally requires that completed `-PlanEditorSourceRun` and observes a new
separate existing-debt fixture without Save. It is not a notification or physical-device test.
`-PlanMoneySubmissionOnly` uses the same completed source for two refused native submissions while
validation changes raw money, comparing every source row and never posting the newer amount.
Independent Android debt/submission reports use distinct immutable phase names. Report recovery
only rereads original rows and native observations; it does not seed or invoke a financial action.

When the repository volume is nearly full, set process-local `VAFADAR_REVIEW_BUILD_ROOT` to a fresh
task-owned directory on a volume with sufficient space, then restore its previous value in finally.
Never reuse a nonempty alternate build directory for another live review. Keep source fixture/output
ownership and negative observations; an expired wait never authorizes accepted-action replay.

### Original goal/budget monetary review (D-431)

Use `Run-StoreDrafts.ps1 -PlanEditorReview -GoalBudgetMoneyReview` for72 original monetary views across
18 language/theme/width contexts, with one native Save per editor and no ledger posts. Reuse only a
verified exact checkpoint with `-PlanEditorSourceRun` / `-PlanEditorObservationRun`, retaining completed
views. The outer receipt checks goal/budget-specific counters and unchanged base data. Recover a failed
receipt read-only instead of replaying UI or financial commands. Independent Android plan-form uses
`review_goal_budget_money=true`; `review_plan_resume_actions=true` requires its exact original checkpoint.
Wait for actual native ownership and deferred tab rebuilds. Normalize selected digit shapes only,
retaining native text/punctuation and complete line layout. Normal-scale checks are not OS/TalkBack
acceptance.

### Original transaction money review (D-432)

Run `eng/scripts/Run-StoreDrafts.ps1 -PlanEditorReview -EntryMoneyReview` only against its independent
fictitious installation, with an alternate-volume `VAFADAR_REVIEW_BUILD_ROOT` when needed. It qualifies
112 native observations across 18 contexts. `-PlanEditorSourceRun` retains original rows and accepted
financial receipts; live source continuation requires its complete financial-action checkpoint.
`-PlanEditorObservationRun` retains already captured native views. Never reseed or repeat an accepted
Save to repair a failed observer. The independent Release/AOT caller uses `review_entry_money` in its
existing `plan-form` phase, with the original run and explicit continuation. Ordinary Main qualification
still requires a complete signed `Build-AndroidApk.ps1 -Offline` candidate and no-reset upgrade.

### Hosted installer metadata versus compiler acceptance (D-433)

The reviewed installer allowlist includes the official 18.11.12306.390 hosted inventory. This does not
approve a new compiler: exact executable hash/file version, Microsoft signature and pinned MSVC/SDK
still gate every native producer/check. Local installation-version fixtures are metadata-only. A new
hosted executable or different immutable package must receive its own actual qualification; never
make a hash/version condition permissive merely to clear CI. Application/APK remain unchanged by
this delivery-check repair. Hosted success must be read from the latest commit's actual workflow.


### Original Linux native host packages (D-433 follow-up)

The native Docker recipe restores Android's unchanged host lock from Ubuntu snapshot 20261012T000000Z.
An isolated stage supplies only the immutable Noble release certificate bundle for HTTPS bootstrap;
the final stage restores all reviewed packages from explicit signed snapshot sources. Missing indexes
are fatal. Never accept a moving package revision or weaken TLS/signature checks to make CI pass.
Keep verify-host.py authoritative and distinguish local host, fresh native and actual hosted results.


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

### Original account monetary runtime checks (D-434)

Use `Run-StoreDrafts.ps1 -PlanEditorReview -AccountMoneyReview` for the actual Windows matrix.
The separate `-AccountDefaultOnly` and `-AccountCurrencyOnly` modes own fresh declared scenes, never
replay an accepted original matrix. Independent Android uses the same actual controls with
`review_account_money` and the corresponding `review_account_default_only` or
`review_account_currency_only` flags. Keep run/comparison/PID identities and original checkpoints;
an observation timeout is not permission to restart or reseed. Require strict report comparison,
base-store equality and actual native readings. Debug diagnostic changes require Android Debug
compilation as well as independent Release/AOT. See the D-434 quality document for preserved failures.

### Original holding monetary runtime review (D-435)

Use `Run-StoreDrafts.ps1 -PlanEditorReview -HoldingMoneyReview` for the actual Windows matrix.
Continuation supplies the exact `-PlanEditorSourceRun` and `-PlanEditorObservationRun`; use fresh
alternate build output while retaining original source/checkpoints and completed images. Independent
Release/AOT Android uses phase `plan-form` with `review_holding_money`, an original run/pass/PID and
the strict public report reader. Each platform accepts one purchase, one opening and one manual
price; an automatic purchase valuation cannot count as the manual action. Final verification checks
actual rows before reporting counts. Continue missing views/actions only; never reseed or repeat an
accepted financial command to repair a native observer. Debug diagnostics also need Android Debug
compilation. The D-435 quality document records actual reports and limitations.

### Secondary monetary runtime review (D-436)

`Run-StoreDrafts.ps1 -PlanEditorReview` accepts the mutually exclusive secondary units/fields/
Settings/household review switches. Retain source and observation run ids across incomplete
observations; never seed or repeat an accepted action to repair a reporter. Original native
forecast popup images are separate from their underlying app-window captures. The protected
household caller must use actual current membership and compare every original table set.

The independent Android Home caller exposes matching explicitly selected secondary groups.
Parse the complete packet for the exact original process; a blank hierarchy after completion is
not evidence that an action is still pending. `ReviewHouseholdMoney=true` confines an old-fixture
permission inspection to its original package. Preserve actual revoked-member refusals. Only
`ReviewFreshHouseholdMoney=true` compiles the new private TLS household fixture described in
`eng/sync/HouseholdMoneyHost/README.md`; never manufacture CanWrite or undo an old revocation.
Keep fixture APK/signature/data restoration distinct from the complete signed Offline Main APK.
Build alternate artifacts on a volume with space; Windows App SDK resources also require a short
absolute build root. Clean only the focused test artifacts after the selective run.

### Physical/rate native continuation (D-437)

Run-StoreDrafts.ps1 -PlanEditorReview -PhysicalRatesReview isolates the actual physical/rate pages.
ReviewWorkspaceRoot is restricted to its marked private temporary workspace, beneath the host Temp
root without redirected ancestors; source builds still run in the repository. Use a short alternate
VAFADAR_REVIEW_BUILD_ROOT on a volume with free space. Keep source and observation ids unchanged.
PhysicalRatesNotesOnly requires its exact prior baseline and repeats only affected note scenes.
PhysicalRatesOutcomesOnly requires the original untouched source; accepted action checkpoints are
saved before observers and cannot be reseeded/replayed. Restored feedback captions are read-only,
source-proven observations rather than another original action frame. The independent Android
caller selects matching review_physical_rates and review_physical_rate_outcomes options; review_pass
is the required32-digit comparison id. Parse complete packets for the actual original process.

PhysicalRatesGuideOnly checks only the later restored-guide correction against the completed source.
Android outcome and guide reports use distinct immutable phase names. A legacy name collision is
continued with review_physical_rate_receipt: require the original input report, terminal report IO
stack,22 original frames from one process and matching complete source checkpoints. It never invokes
an accepted command. Keep original process ids and distinguish recovered evidence from new frames.

### Actual Android numeric input (D-440)

The independent plan-form caller accepts review_numeric_ime=true only with an original completed
physical/rate fixture and review_plan_resume_actions=true. Use a fresh review_pass; never seed another
source or invoke Save. review_numeric_ime_followup=true runs only new incomplete/invalid/native-digit
and real handler keyboard/password/length cases after retaining the completed baseline.
review_numeric_ime_guards_only=true continues only the read-only/prediction/spelling boundary, preserving
completed source/base and earlier frames. Compare actual
InputConnection requested/native/model text and integer input flags; setting model raw text does not
prove IME acceptance. Empty password probes never enter/read credentials. See Zanance's
quality/android-numeric-input.md for declared evidence and remaining acceptance.

### Secondary monetary system-scale scopes (D-441)

Resume completed original secondary-fields/settings/units sources through review_money_os=true,
review_os_width360/412 and one original route. Require accepted checkpoints, conditional bill
recovery completion, actual font2/native width and focused independent ownership. Gate commands to
known invalid-unit refusals, explicit factory refusal and draft-only suggestions. Keep stage/report
identities separate from old success, verify complete source/base equality and restore exact OS/Main.
See [native evidence](../../src/Apps/Zanance/docs/quality/secondary-money-os-qualification.md).

### Current title actions at system scale (D-442)

headers-os requires the completed original secondary-field source, existing independent base account,
fresh review_pass and actual OS200% width360/412. Never seed or invoke a writer for geometry.
Check current title/Add/viewport bounds, not the obsolete historical Home row count. Native Add and
Cancel require attached focused visible targets. Persist scene receipts and verify complete source/
base rows; filled Plans is read-only. Restore exact OS/Main. See [evidence](../../src/Apps/Zanance/docs/quality/header-os-qualification.md).

A further36 read-only native-coordinate observations verify the action is physically right of the
complete title (minimum gap8px), without replaying any Add/Cancel. Total D-442 evidence is72
observations/96 header states/192 native text readings; accepted writes remain zero.

### Retained rate outcomes at system text scale (D-443)

rate-outcomes-os requires the original completed action/delete receipts and all18 native caption
frames, fresh review_pass and actual OS200% width360/412. Read the immutable later-version row and
its original native text into the actual disabled presentation collection; never restore a rate in
SQLite or manufacture ManualRatesReview authority. Require full text/height, disabled native Delete,
unchanged complete source/base rows and exact OS/Main restore. Historical geometry is not a new
active-rate lifecycle. See [evidence](../../src/Apps/Zanance/docs/quality/rate-outcomes-os-qualification.md).

### Protected household OS scopes (D-444)

Build only the retained ReviewFreshHouseholdMoney variant in its own artifact root. plan-form needs
review_secondary_household,review_plan_resume_actions,review_household_os and width360/412 on the
original admitted run. Require all original receipts first; stop foreground sync before startup wait
and never call fresh transport/admission. Current protected membership must genuinely allow writes.
Permit only the known changed-unit refusal; persist unique width scenes and verify complete source/
base/profile restoration. Restore exact prior public APK and OS/Main. Keep shipping Main Offline.
See [evidence](../../src/Apps/Zanance/docs/quality/household-os-qualification.md).

### Original goal-list native qualification (D-447)

Use `Run-StoreDrafts.ps1 -PlanEditorReview -SecondaryFieldsReview -GoalListReview
-PlanEditorSourceRun <original-secondary-fields-run>` only with a complete retained original source.
This dedicated scope never creates or repeats accepted financial fixture actions. Its84 own-window
captures use the Stage/goals.png receipt mapping. Android uses the independent `goal-list` phase
and requires all five accepted source receipts. Preserve native accepted-navigation markers before
observers; `review_goal_list_observed` may retain exact completed observations, not recreate money.
A terminal receipt/capture-path failure is repaired by validating existing artifacts, not rerunning
successful contexts. An observation timeout alone does not justify stopping or restarting a process.

### Original goal-detail native qualification (D-448)

Use `Run-StoreDrafts.ps1 -PlanEditorReview -SecondaryFieldsReview -GoalDetailReview
-PlanEditorSourceRun <original-secondary-fields-run>` for the changed original detail batch only.
It requires the exact complete original source and never repeats accepted fixture money. Its144
own-window captures use Stage/detail.png. Android's independent goal-detail phase records48
observations over12 contexts; an observation continuation retains completed sequential contexts.
A declared retained-Retry observer repair reconstructs only read-only controls and observes the
missing frame without redoing the original Retry. Keep source/base equality and raw Latin inputs
separate from display-digit captions. An observation timeout retains the same process; inspect its
later terminal complete receipt before validating captures, without restart or successful replay.

For the later Edit-only refusal, add `-GoalDetailEditFailureOnly`; it captures2 new Windows frames
without replaying the144 completed frames. Android uses review_goal_detail_edit_failure=1 on the
same independent phase/source. Retain the earlier48 frames and original source receipts. Native
readiness must verify an actual nonempty UI-dump success marker before pulling; a null root is not
a successful observation. Public production-app cleanup requires both profile-delete questions;
record cancellation and inspect current UI before continuing, never repeat a known accepted Save.

### Original budget action lifecycle review (D-461)

`eng/scripts/Run-StoreDrafts.ps1 -BudgetActionsLifetimeReview` builds a fresh independently identified
Windows Debug app on its non-input desktop. Its separate SQLite source exercises original native
replacement/deletion/previous-copy, known accepted notification failures and read-only Retry. It
then checks original en/fa/de Light/Dark targets and retirement after actual native disappearance.
Use `VAFADAR_REVIEW_BUILD_ROOT` for fresh output on a drive with sufficient space. Preserve the
original process on observation timeout; inspect its exact handle before any continuation.

`-BudgetActionsSourceRun <original-run-id>` accepts only a terminated original source process and
its exact fixed fictitious budget file. `-BudgetActionsObservationsOnly` requires that source and
qualifies the already-applied three-period configuration without any accepted command replay.
Its receipt explicitly reports zero new writes/retries; it cannot turn an earlier failed native
run into a completed one. Review every own-window image and retain original/cleanup failures.
The current native gate waits for the actual enabled44px child target, not only its parent layout.

### Original budget financial publication review (D-462)

`Run-StoreDrafts.ps1 -BudgetFinancialReadReview` builds a fresh independent Windows installation and
checks three original budget methods in en/fa/de Light/Dark at the existing412px review width.
The shared native review owns three separately migrated fictional SQLite sources. It never sends
Save/copy/delete commands; every view compares stored rows before and after. Each completed context
has its own receipt, and cleanup failure is rejected independently of financial observations.
The independent Android Home caller accepts `budget-financial-read` with original `review_run` and
`review_pass` identifiers. Its public report uses the same phase in `Read-ReviewReport.ps1`.
Continue retained sources/receipts after a failed observation; do not reseed or replay completed
contexts. A modal page owns its own focused native child, independently of the covered Activity
decor. Keep package/focus/secure-window guards and restore exact original package/OS/Main state.

The first successful financial-read public frame omitted Comparison. For that original frame only,
`Read-ReviewReport.ps1 -OriginalBudgetReport` requires the exact phase/run/live PID, no Comparison
argument,18 complete unique original contexts, unchanged rows and zero saves. It preserves original
JSON bytes without inventing a binding or rerunning the UI. Future frames include actual launch
Comparison and use the ordinary guarded observer.

### D-463 - Changed budget caption review

Run `eng/scripts/Run-StoreDrafts.ps1 -BudgetPeriodCaptionReview` for one independent, ledger-empty
original source and18 language/theme/width contexts (54 window images). The review only invokes
read-only period navigation and opens the original weekly editor; it never copies/deletes/saves
budgets. If the original process has terminated, `-BudgetPeriodSourceRun <original32hexrun>`
continues its exact fixture and retains an already captured image instead of reseeding it. Keep
the original failure and validate its cause before continuation. Android's independent Home caller
accepts `budget-period-captions` with exact `review_run`/`review_pass`; observe that live PID through
`Read-ReviewReport.ps1`, never restart merely because the observation timeout expired. Restore the
original independent APK and public Main/OS state after the complete report. This is a caption
review; MoveButton's existing40px target is not a new accessibility acceptance claim.

### D-465 - Changed budget account-note review

Use `eng/scripts/Run-StoreDrafts.ps1 -BudgetScopeCaptionReview` for18 original Simple-editor contexts
and18 own-window images, with its own `budget-scope-captions.db`. This scope-only branch does not
replay D-463 period scenes. `-BudgetPeriodSourceRun <original32hexrun>` can retain this exact source;
do not substitute the other branch's database. Two complete original account names include an
archived account; require clean draft, source/base equality and zero financial saves.
The independent Android caller accepts `budget-scope-captions` with original `review_run` and
`review_pass`; `Read-ReviewReport.ps1` validates the same live PID/Comparison. Preserve completed
reports instead of restarting/reseeding, then restore the exact original APK, Main and OS.
This changed-caption review does not establish full system scaling or spoken accessibility.

## Original purchase error-only review (D-467)

`Run-StoreDrafts.ps1 -OriginalAmountErrorReview` checks only the changed original purchase errors:
USD/JPY/KWD, en/fa/de, Light/Dark and360/412/1280. Original native Save refuses not-money; complete
source/base rows remain unchanged and FinancialSaves stays0. `-BudgetPeriodSourceRun <run>` reuses
the exact stopped original fixture and completed receipts. `-OriginalAmountErrorRecapture <key>`
requires that retained source and reobserves only the explicit missing visual context, such as
412-en-Dark-JPY. Keep rejected images and reports. Receipt observation directories identify the
actual host that owns each image; never infer a language subfolder or replay successful scenes
to repair a report. Require the error's actual native rectangle inside its scroll viewport.
Independent Android phase `original-amount-errors` checks only18 corresponding412dp contexts.
Final physical continuation and reporting limitations are in
[original purchase evidence](../../src/Apps/Zanance/docs/quality/physical-original-purchase-amounts.md).

### D-468 - Validate completed report captures without replay

Run-StoreDrafts.ps1 keeps its final mode-specific count in the same $expectedCount assignment.
A duplicate reimbursement guard previously terminated that expression before the final
PlanRowsEdgeProbe/HomeLegends/ReportRows fallback; ReportRowsReview returned null after completing
18 contexts/36 images. The original guard already runs before build. Remove its duplicate and
join the final elseif branch. Actual old/fixed AST and the retained receipt, package/run/PID,
unchanged ledger rows and original images are validated without replaying completed native UI.
Isolated outputs support strict MAUI10.0.120 builds without a global SDK/workload update.

## Original household caption-only review (D-471)

Run-StoreDrafts.ps1 -PlanEditorReview -SecondaryHouseholdReview -HouseholdCaptionsOnly
requires -PlanEditorSourceRun with an existing original navigation fixture and valid protected
household authority. It skips fresh monetary preparation and checks two original pickers across
18 contexts/36 selected-control images. -PlanEditorObservationRun reuses exact original popup
receipts when only missing viewport evidence needs continuation; images exclude native popups.
Independent plan-form uses review_secondary_household=true, review_plan_resume_actions=true,
review_household_captions=true, retained review_run and a fresh review_pass. The original worker
is stopped before startup. Never create enrollment or substitute authority for a caption check.
A source with no linked transfer uses an explicitly transient formatter/control-only destination
packet; no cash-linkage acceptance or Save. Require native text/layout/package/popup focus before
observation. Preserve failures, original source/base equality and original package/stopped state.
See [qualification boundaries](../../src/Apps/Zanance/docs/quality/household-caption-separators.md).

## Explicit hosted Windows native producers (D-478)

Local default remains baseline/candidate.7. The older reviewed serviced host uses
VafadarWindowsCipherVariant=servicing/candidate.8; installation18.11.12315.225 with its exact
reviewed compiler uses hosted20261021/candidate.9. Hosted lanes call
eng/native/sqlcipher/Select-HostedWindowsProducer.ps1 before cache/native use. Its optional
-EnvironmentFile requires an existing absolute file and persists only the qualified variant;
GITHUB_ENV is scoped to the current hosted job. No global SDK/compiler setup is changed.
Unknown hosts/compilers refuse, and fresh package output still must match every immutable entry.
Keep source/diagnostic/schema and actual binary-consumer gates; producer artifacts are review
evidence, never a replacement native binary feed or release approval.
See [bounded evidence](../../src/Apps/Zanance/docs/quality/ci-common-recovery.md).

## Independent original Android widget host (D-479)

`eng/tests/AndroidWidgetHost` is a native-only Release ARM64 helper, with no Main project reference
or shipping package inclusion. Its README gives the strict complete-package build. Verify actual
manifest, embedded native assembly store and signature; only BIND_APPWIDGET may be declared.
Use a fresh explicit32-hex review_run and the actual native single-widget consent, never a shell
grant or Always allow. Resume its admitted widget without allocating again. Native framework
delivery must invoke the original provider's own RemoteViews PendingIntents, not a constructed
Main intent. For cold origin confirm cached Main really exits before the first native action.
Cancel each draft without Save. Remove only the host owned by this independent package, confirm
owned0 and uninstall that newly installed helper. Preserve actual launcher arrangement and OS.
See [bounded physical evidence](../../src/Apps/Zanance/docs/quality/physical-widget-pending-intents.md).
No unchanged Main build, old financial action or successful emulator cohort needs repetition.

## Original Android validation-layout lifetime (D-480)

eng/tests/AndroidValidationLayout compiles the actual Android-only waiter without a Main reference
or fake MAUI controls. Its README specifies the strict native-only complete signed build, fixed
independent Release identity and explicit review_run. Verify no declared permission, package,
signature and native ABI before a newly owned install. Keep original event-wrapper reproduction
separate from fixed listener/current-tree/timeout checks; do not repeat completed native cases to
repair a build guard or report. Preserve real failed JNI registration and namespace/build evidence.
Before a native app-coordinate action, require owned foreground, actual target geometry and current
IME window visibility. Dismiss an overlapping keyboard before targeting Save; validation probes
must retain an explicit invalid-amount guard and never post money. See
[bounded evidence](../../src/Apps/Zanance/docs/quality/android-validation-layout-lifetime.md).

## Retained original validation callers (D-481)

Independent AndroidHome phase validation-layout requires the marked original work-editor source,
review_run and a fresh review_pass. Observe the exact live PID with Read-ReviewReport.ps1 -Phase
validation-layout -Comparison <pass>. An initially empty PID read is not terminal: follow the
actual native activity/process evidence rather than restart on an observation failure.

Require current post-appearance command identity, attached/shown/focused/enabled native ownership
and explicit raw # before invoking any original action. Fixed footer Save is outside the scroll
viewport; only true descendants may be ScrollTo targets. Settlement uses its current source-store
constructor and public LoadAsync. A failed stale loader is a setup refusal, not a product fix.
Completed receipts retain source hash and all native/invalid/no-write/data-equality facts. Skip
completed cases and recover full packets without their actions; NativeActionsThisPass0 makes that
reporting boundary explicit. Restore the exact initial independent APK without reset and leave it
stopped, preserving original Main/OS and hash-cleaning only owned captures. See
[actual continuation evidence](../../src/Apps/Zanance/docs/quality/validation-layout-callers.md).

## Detached native validation targets (D-482)

The independent eng/tests/AndroidValidationLayout fixture includes d482-detach (five cases) and
d482-remaining (four cases, preserving an accepted post-attachment result). Use a fresh owned
artifacts path and full Release dotnet publish for its package. The same original source is linked;
never replay a completed cohort for a missing report. The fixture README defines native collection
controls and restoration. An observed incremental n_onCreate failure is not a scenario result;
retain it and clean only that owned helper output before a full publish recovery. Main integration
uses the complete signed Offline APK and one invalid input/Cancel, with no financial write.

## Original saved forecast classification review (D-487)

Run-StoreDrafts.ps1 -SnapshotDifferenceReview selects one independent Windows Debug original
SnapshotPage review on a non-input desktop:18 contexts,36 images, complete source/base equality.
AndroidHome's snapshot-difference phase renders six original Release/AOT comparisons and returns
retained context receipts through Read-ReviewReport.ps1 -Phase snapshot-difference. Native width
comes from the actual owned window/density. Both use an independent fixture-only audit clock to
seed historical source facts, then compare through the real app clock; never change OS time.
FinancialSaves0 applies to presentation; setup seeds its own source once. Preserve receipts and
continue only incomplete observations. See [scope and package](../../src/Apps/Zanance/docs/quality/snapshot-difference-kinds.md).

## Original physical goal reminder queues (D-488)

The independent AndroidReminders Release/AOT caller accepts review_goal_lifecycle=true and one
32-lowercase-hex review_run; the actual platform notification prompt still requires an explicitly
owned UI action. Keep the fixed independent application id and an absent-before-install package.
Read content://pro.vafadar.zanance.androidreminderreview.status/goal-status only for already-produced
synthetic stage/count receipts. It never prepares storage or runs a step. Retain original live
PID/receipts through observation deadlines; do not rerun successful states. No clock change,
Main private-store read, actual owner authentication or password entry is authorized. Complete
original state/cleanup evidence and remaining timed-delivery gates:
[physical goal queue](../../src/Apps/Zanance/docs/quality/physical-goal-reminder-queue.md).

## Original physical period-review queues (D-489)

The fixed independent AndroidReminders Release/AOT caller accepts review_period_lifecycle=true
and one32-lowercase-hex review_run. Its native notification permission still requires a verified
owned UI action. Read content://pro.vafadar.zanance.androidreminderreview.status/period-status only
for synthetic persisted progress; do not recreate an Activity to repair an observation timeout.
Three initial source states, six states in each Gregorian/Persian/Hijri calendar and final restored/
off give22 observations. A completed review removes that boundary, not future financial months.
Verify original settings unchanged by publication and full source account equality; this changes
no OS settings or financial rows. Preserve explicit temporary phone ownership between projects.
Scope, final package and cleanup: [physical period queue](../../src/Apps/Zanance/docs/quality/physical-period-reminder-queue.md).

## Literal CSV native continuation (D-499)

Run-StoreDrafts.ps1 -ImportFinancialReview -ImportLiteralTextOnly qualifies the own ledger/holdings
pair with five captures. -ImportLiteralTextSourceRun <accepted-run> reuses that exact accepted
source read-only for a shifted named marker and performs only the previously unexecuted generic
case, with five further captures. Never replay accepted imports to repair observations.
The independent AndroidHome csv-literal-final phase requires review_csv_literal=true,
review_csv_literal_final=true and review_baseline=<original-accepted-comparison>, with the same
review_run and a fresh comparison. It validates the original completed receipt; ordinary fresh
review retains its fresh-root guard. Read the exact current PID/run/comparison report once complete.
Restore retained caller APK/data/stopped state, original Main/OS/public UI and remove only owned
hash-matched remote output. See [evidence](../../src/Apps/Zanance/docs/quality/csv-literal-identity.md).

## Current-day Home comparison (D-500)

Retain the original independent home-stages source run, protected generations/keys and raw rollback
oracles. A different-day complete presentation packet can refuse on date-sensitive captions; never
weaken equality or overwrite the old packet. Establish a fresh comparison=baseline identity with
the already verified original code on the same source, then compare the changed code against that
current-day baseline. Preserve failed/partial identities and continue only missing read-only scenes.
Never seed or repeat financial commands. Windows home-workload remains independent storageacceptance
with a marked retained source root and isolated non-input desktop. Record complete source equality,
restoration and exact owned output cleanup; small timing cohorts do not establish stable speedup.

## Fixed public receipt corpus (D-501)

The independent AndroidHome receipt-corpus phase requires a fresh32-hex review_run/comparison and
a local opt-in ReceiptCorpusRoot; ordinary builds contain no corpus assets. That root contains
ten original test-000.jpg through test-009.jpg, manifest.json, LICENSE-CC-BY and attribution.txt.
Manifest Source is naver-clova-ix/cord-v2, License CC-BY-4.0, Selection first-ten-test-rows, Cases
exactly0..9 with File/Sha256/AnnotatedTotal. Collect those first ten official test rows, retain
original response/image digests and licence, and never use expected amounts in recognition.
Set ReceiptCorpusRoot only in the current build process and build the independent project with
Build-AndroidApk.ps1 -Offline; do not add images to the product or repository. Validate packaged
image hashes/licence and the complete Offline signature/identity before install. Read the exact
original PID/run/comparison once complete. Reports retain public recognized text privately;
owner documents/credentials are excluded. Restore old caller/data/stopped state and Main/OS;
remove only owned hash-matched remote output. No financial commands or successful image replay.
See [scope and source](../../src/Apps/Zanance/docs/quality/public-receipt-corpus.md).

### D-504 receipt-source qualification

`ReviewReceiptSource=true` belongs only to the independent AndroidHome project/new disposable
receipt-source identity. Its documented receipt-source-help and portrait-only return paths never
belong in shipping behaviour. Use actual measured dialog widths and explicit same-run progress;
keep terminal source/UI evidence instead of replaying successful checks. D-504 uses separate
`--artifacts-path`/`ArtifactsPath` build roots on the spacious review drive; focused test outputs
are cleaned after the fifteen new cases. Existing Windows UI is unchanged; its strict build
passes and unrelated snapshots are not repeated. See the benchmark README and
`src/Apps/Zanance/docs/quality/android-receipt-source-help.md`.


## D-515 original holiday-caption review

Build Windows Debug with ContinuousIntegrationBuild=true and both ApplicationId and ApplicationIdGuid
set to pro.vafadar.zanance.holidayhintsreview, using an independent artifacts path. Verify the compiled
package metadata before launching only that executable hidden. Set VAFADAR_SNAPSHOTS to a fresh owned
folder, VAFADAR_SNAPSHOT_ONLY=plan-holiday-hint and VAFADAR_WINDOW_SIZE=360x892. The guarded route
reads the original PlanEditorPage/PlanEditorViewModel, measures actual360/412/1280 page widths and
native caption layout, then renders its own window root for six languages and both themes. It never
sends desktop input, takes a full-screen capture or saves a plan/ledger row. Its independent SQLite
setup and complete typed-row packet are retained beside per-context receipts.

Use VAFADAR_HOLIDAY_HINT_RESUME=1 only for the same incomplete fixture. Matching completed contexts
retain their original PNG/source receipts and are not replayed; a changed source/row packet refuses
continuation. An observation timeout does not authorize another launch or seed while the original
process is alive. Check final process exit, source-template equality, all36 receipts and stored-row
equality before reporting this scoped Windows result. Android still needs its own complete signed
candidate and actual native caption/control/Cancel evidence. D-517 permits a recorded temporary
screen-off timeout extension during authorized phone tests, with exact original restoration/readback
at completion/interruption/handoff; other OS/security settings remain protected.

## D-516 independent original SUPA review

Windows requires the exact pro.vafadar.zanance.supacsvreview compiled identity and Debug-only
supa-csv-native route. Use a fresh isolated folder for the36 original contexts. An interrupted
fixture requires explicit VAFADAR_SUPA_NATIVE_RESUME=1 and complete retained-source validation.
After only the new Help example changes, VAFADAR_SUPA_FINAL_HELP=1 with that retained fixture
checks12 narrow Help-only contexts; it must not replay picker/consent/Cancel or overwrite original
receipts. Use original owned native UI patterns and application-window captures only. Android
Home review ReviewSupaCsv=true has a separate SUPA-only identity and supa-csv phase; runtime
window inspection is API29+, independent of pending minimum-API qualification. External picker
and bank customer-file acceptance remain separate. No tool or fixture is referenced by shipping.

D-516 completion preserves both terminal Android observer failures and their immutable source
APKs before same-UID/no-reset continuation. Select captions within BankPresetReview, not any
identical common warning. Check actual CompoundButton checked/command state after one click;
its listener-handled return is not evidence of no action. Six contexts finish in the retained
source without financial writes; only that completed new caller is uninstalled. Static package
proof, actual native source evidence and original Main no-reset adoption have separate receipts.
Standard clean succeeds for all dedicated host/platform/native outputs; signed source/failure
packages and local receipts remain. Never replay a financial writer to repair an observer.

### D-519 - Scoped compatibility SDK and retained SUPA scale2 review

The owner-approved SDK root is `E:\Zanance.ReviewBuilds\AndroidCompatibility\sdk` with project-local
user/AVD directories. Existing official platform-tools may be copied there with identical hashes;
keep the general SDK and its licence marker unchanged. Select the isolated tools root through
process-local `AVDMANAGER_OPTS=-Dcom.android.sdkmanager.toolsdir=<isolated SDK>\cmdline-tools\latest`
when using the already installed command-line manager. Existing ADB5038 is reused; no server
restart or global environment change. Actual API/page-size output and complete signed-app runtime,
not package names or static alignment, establish only the bounded result tested.

For the retained fictitious Windows SUPA source, explicit `VAFADAR_SUPA_LARGE_TEXT=1` requires
`VAFADAR_SUPA_NATIVE_RESUME=1` and process-local `VAFADAR_SNAPSHOT_FONT_SCALE=2`. Use a new owned
copy of the original SQLite checkpoint, no accepted source/account/financial setup replay. The
DEBUG Windows review records actual native ratios and tall endpoints and skips unchanged Help/
financial confirmation dialogs. Never interpret this mode as real OS scale or platform acceptance.
See [D-519 evidence](../../src/Apps/Zanance/docs/quality/android-runtime-supa-large-text.md).

### D-521 - Prearmed independent migration observation

Use AndroidExportObservation.psm1 for the original fixed nativecompatibility identity. Require
an absent strict owned candidate path and fresh report/metadata. Arm before am start -W; a host
poll afterwards can already be after verification or selection. Pending keeps the same actual
process handle, and a retained report/PID must be revalidated rather than relaunching. Use shared
read access for the active redirect writer. Keep unavailable reattached exit metadata explicit;
native candidate/PID/STOPPED and independent package-absence proofs remain mandatory.

The helper's migration-inspect-interruption phase returns only retained journal/file booleans.
For a necessary unexecuted observation, migration-adopt-source accepts only an explicit other
owned run through migration_source_run, preserves exact plaintext-source bytes and current
migrations, and does not invoke the financial seed. Never adopt an arbitrary/Main path or replay
an accepted phase. PowerShell5.1 syntax-only checks are separate from actual7.6.5 execution;
leave an existing script-execution refusal/policy unchanged. Independent debuggable packages are
not product/phone APKs. [Full scope](../../src/Apps/Zanance/docs/quality/android-storage-runtime-compatibility.md).


### D-522 - Reuse the qualified package for profile runtime cohorts

Reuse D-521 signed441faf38 without rebuilding for nine previously unobserved original profile/
restore cohorts on each isolated API24/4096 and API35/16384 x86_64 runtime. Preserve each owned
source run across its cold phases, claim a fresh public receipt nonce before dispatch and prove
prior PID absence; never replay an accepted phase. Audit all36 records and installed public code
before uninstalling only new helpers and stopping owned emulators. Private fixtures stay in their
UID; retain existing signed code and public local evidence. No Main/phone/global SDK changes.
See [actual cohort and limits](../../src/Apps/Zanance/docs/quality/android-profile-runtime-compatibility.md).


### D-523 - Retain failed workload sources

Use the independent protected-workload README verify-retained mode to compare an already accepted
financial round trip without replaying it. Only an unprepared tenfold cohort may use tenfold mode;
an existing folder refuses. Actual backup policies project a fresh encrypted rollback copy, which
is removed after use; production key/factory/snapshot guards stay unchanged. Revision normalization
is memory-only and limited to the validated Home singleton; every other typed value remains exact.
Do not infer hosted failure cause from local reproduction when original hosted logs are gated.
[Focused evidence](../../src/Apps/Zanance/docs/quality/protected-workload-revision-preservation.md).


### D-524 - Original metadata-only native fault cohorts

Reuse signed441faf38 for identity staging, journal/history staging and uncommitted/committed journal
faults on the two isolated x86_64 runtimes. Claim fresh public nonces, retain each original run,
observe the original synchronized barrier before killing only the independent package and prove
death. Remove only an already-consumed observer marker before its next reuse. Audit phase order,
original facts and installed code before retiring new UIDs/owned AVDs. Twenty observations are ten
barriers plus ten terminal receipts, not twenty terminal results or new managed cases.
[Evidence and boundaries](../../src/Apps/Zanance/docs/quality/android-metadata-runtime-compatibility.md).
