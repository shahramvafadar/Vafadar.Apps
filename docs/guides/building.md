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
