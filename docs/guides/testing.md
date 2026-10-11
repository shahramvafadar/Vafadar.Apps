# Testing

## Running tests

| Task | Command |
|---|---|
| All tests | `dotnet test --solution Vafadar.Apps.slnx` |
| Without building MAUI projects | `dotnet test --solution Vafadar.Tests.slnf` |
| One project | `dotnet test --project test/Libraries/Vafadar.Backup.Tests` |
| Filter by test name | `dotnet test --project test/Libraries/Vafadar.Backup.Tests --filter-method "*Encrypted*"` |
| Code coverage | `dotnet test --solution Vafadar.Tests.slnf --coverage` |
| TRX report | `dotnet test --solution Vafadar.Tests.slnf --report-trx` |
| Debug a test project directly | `dotnet run --project test/Libraries/Vafadar.Backup.Tests` |

Tests use **xUnit v3** on **Microsoft.Testing.Platform** (enabled in `global.json`). Visual Studio's Test Explorer
discovers and runs them as usual.

## Structure

* One test project per production project: `test/` mirrors `src/` (`Vafadar.Backup` → `Vafadar.Backup.Tests`).
* Every production assembly exposes its `internal` members to its test project (`Directory.Build.targets`).
* Shared helpers live in `test/Shared/Vafadar.Testing`:
  `TemporaryDirectory`, `FakeHttpMessageHandler`, `StaticAccessTokenProvider`, `RepositoryPaths`.
* Time-dependent code gets a `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`).

## Conventions

* Test names are sentences with underscores: `Wrong_password_is_rejected`.
* Arrange / act / assert, separated by blank lines; one behavior per test.
* Pass `TestContext.Current.CancellationToken` to async APIs (xUnit analyzers require it).
* Use real SQLite files (in a `TemporaryDirectory`) for data tests rather than mocks – SQLite is fast and behaves
  like production.
* Tests that change process-wide state (cultures) disable parallelization for their assembly
  (see `Vafadar.Localization.Tests/AssemblyInfo.cs`).
* External services (Google Drive, Microsoft Graph) are tested against `FakeHttpMessageHandler`; real accounts are
  never used in automated tests.

## Repository-wide checks

Some tests protect the repository as a whole:

* `ResourceCompletenessTests` – every translated `.resx` in `src/` has the same keys and placeholders as the neutral one.
* `ZananceAppTests` – the Zanance app id in code matches `ApplicationId` in the app project (the id is permanent).
* `ZananceDataTests.Startup_migration_creates_the_database` – the app's startup migration works, which also fails
  when the model changed without adding a migration.

## What is not unit-tested

MAUI pages and platform code. Keep logic out of code-behind and view models thin, so that almost everything worth
testing lives in `Core`, `Data` or a library. UI automation can be added later if needed.

## Application flow project (D-73 / QA-03)

Vafadar.Zanance.App.Tests is in Vafadar.Tests.slnf. It links actual platform-independent application source files and
resource files, with explicit dialog/window/theme/profile ports and real SQLite, commands and PIN verification.
It has no MAUI project reference and never creates fake Shell/Application/Page classes. FluentIcons.Common supplies
the same managed icon enum as the app. See test/Apps/Zanance/Vafadar.Zanance.App.Tests/README.md for the 68-case map.

Tests isolate fictitious profiles/verifiers/preferences and injected time; assembly parallelization is disabled for
process-wide culture/digits. Native adapters, rendering, Intent delivery, OS credentials and SecureStorage remain
running-app/device checks. Preserve the linked-source approach when extending existing flows; never duplicate an
algorithm merely to test a copy. Run the CI-policy test filter and clean outputs after verification.

D-74 / QA-04 adds 37 AT-81 cases to App.Tests (105 total), using the actual asset confirmation gate and real SQLite
save continuations. Native editor invocation remains a running-app check, with no duplicate editor algorithm.
Run the main test filter with CI warning policy; it now has 1,300 cases. Do not run cleanup concurrently with builds.

## Performance workload and thread/profile regression checks (D-75 / AT-82)

The normal test filter has 1,323 cases. AT-82 adds 12 account-index cases and 11 Data cases covering fixture ownership,
legacy-field preservation, literal paths, deterministic legacy rows, manual plans, cancellation and queued profile
capture. The Data tests link the actual fictitious fixture source; financial calculations remain in production Core.
Run the independent Release workload after builds/tests finish to avoid deliberate measurement contention:

```powershell
dotnet run --project eng/benchmarks/Zanance.Performance -c Release -- artifacts/performance-new-run
```

Use a fresh output leaf, record raw samples and separate store/calculation measurements from native loading,
debounce/rendering, ANR, system first-frame and loaded-content observations. See its README and the Q-02 report.
Do not run cleanup while a build or Windows app uses the same output. Device acceptance is separate from timing
regression tests and the isolated emulator.

## Snapshot publication boundary (D-76 / AT-83)

App.Tests explicitly links the production SnapshotLoadState. Twelve cases cover initial coverage, shared pending
reads, publication-before-ready, synchronous repeats, read/presentation failures, retry without transient readiness, cancelled reads and retry, argument
validation and real SQLite transfer preservation. App.Tests has 117 cases; the main filter has 1,335 passing cases.
The helper's readiness state is not proof that a native adapter has drawn its rows. Repeat native loading/input and
no-match/unique-result transitions with a complete signed Release APK, keeping failed observations in Q-02.

D-77 large-text checks use the actual running Windows layout, not fake MAUI controls or source-text mirrors. Each
Home capture asserts four native untrimmed quick-action names, horizontal containment and at least 44 px measured
button targets. Native Android experiments retain system settings and screenshot protection; failed configuration
prototypes are removed and are not platform acceptance. The final normal Release is checked separately. See the
[A11Y-03 evidence](../../src/Apps/Zanance/docs/quality/font-scaling-a11y03.md) for matrix coverage, negative observations and limits;
these runtime checks do not replace the main regression suite, real OS or screen-reader/device acceptance.

D-78 / AT-85 checks the actual realized native financial title for trimming, useful identity width and separation
from the amount; action geometry requires a positive viewport, at least 44 px action height and no viewport overlap.
Hidden parent chains are excluded. These runtime checks and own-window captures are not new unit cases and do not
prove every glyph, offscreen row, keyboard, screen reader or actual OS scaling. Undo preview is layout evidence only.
Android hierarchy checks require the actual viewport rather than the outer Shell container, the expected action
semantic name and both 44 dp dimensions at the unchanged density. Never infer the active page from a tab tap: a
retained detail stack may still be visible. Read back only the owned fictitious data, never an owner profile.

## Settings reads and translated choices (D-79 / AT-86)

App.Tests links actual SettingsSnapshot/SettingsChoiceLabels and reuses SnapshotLoadState. Ten cases use real
isolated SQLite/localization, delayed availability callbacks and failures; no fake MAUI controls. They prove complete
reads, covered retry, account eligibility/order, unchanged preferences/transfer data and six-language captions.
Main suite: 1,345 passing, App.Tests: 127. Real SettingsViewModel, native selection callbacks and rendered bindings
are verified through the settings-display running-app route and the complete Release emulator separately. Retain
physical OS scaling, keyboard/screen-reader, phone/iOS and provider acceptance as independent gates.

D-80 / AT-87 is actual running-app coverage of WrappingAction, not copied/fake MAUI controls or new xUnit cases.
Native caption/target/name and command/argument checks run in the snapshot matrix. Real Select all/Cancel Invoke
proves the binding reaches the existing selection logic; full entry JSON is unchanged. Check disabled/no-selection
and enabled/selected states. Main suite remains 1,345; real OS, screen-reader, keyboard and device checks stay open.

D-81 / AT-88 adds running-app checks for actual DateField/AmountReadout, not fake controls or a new unit count.
Main suite remains 1,345 passing (App.Tests 127). Check every realized date input's full digit count against actual
native width, valid/partial date binding and three calendars without Save. Check the original monetary packet and
spoken name, single-line realized glyph bounds, and both native Scroll-pattern ends. A detached bundled-font probe
can differ from the realized caption, so retain negative evidence and use actual rendered boundaries. Keep actual
external ValuePattern typing distinct from programmatic draft checks and Windows stress distinct from device QA.


D-82 / AT-89 adds a real reopened-Settings regression with native selection/back patterns and strongly retained old
Shells. It checks retired title detachment, live choice captions, preserved open drafts and complete stored
settings/accounts/entries, including UpdatedAt. No new xUnit count or fake native controls. Full Release emulator
checks repeat languages without restarting between choices; see the large-text report for final evidence/limits.

## Occurrence command feedback (D-136 / AT-138)

Full/partial payment, explicit linking, Skip/Unskip, date/amount/note changes and reopening use actual
OccurrenceCommands with the existing PlanStore writers. One gate spans native consent, writer failure feedback
and successful display publication. Rejection/cancellation never reloads, navigates or replaces the draft; ledger
validation stays inline. Unexpected failure uses the native interaction port without raw user-facing exceptions.
Retire the writable reviewed snapshot before post-commit navigation/refresh and publish its replacement only after
all load reads finish. A refresh failure explicitly says the change was saved and requires closing/reopening the
screen before another change. Explicit Reopen/Cancel captions exist in all six languages; French/Italian no longer
present two cancellation captions. No commercial activation, schema, SDK, permission, backup or device-setting change.
29 added actual application-flow cases (AT-138), App.Tests 347. Main 2,201 pass with an independently owned,
ignored Data.Tests output configuration limiting collection concurrency to one. Explicit writer-race tests retain
their concurrent operations. Two normal parallel full-suite attempts failed in unrelated Data.Tests SQLite cleanup/
migration (locked file and disposed native handle). Preserve negative logs; pool-cleanup isolation remains open.
An unsupported argument ran zero tests and a help invocation failed; neither is acceptance. Output-only test
configuration was removed; no product workaround or test execution policy was committed.
Strict Windows/Android builds and complete signed Release APK verified. Native normal Release emulator checks cover
en/fa/de, light/dark, actual error dialogs, retained drafts and all 24 complete stored tables. Six additional normal
Release cases commit an actual partial payment, deliberately break only the owned fixture read, display the saved-
but-not-refreshed message and reject replay from enabled native actions. No Windows native
command, real phone, iOS, provider or publication acceptance claim. Selected-plan generation/automation, choice
persistence/UI, commercial activation and existing external gates remain open.
[Evidence](../../src/Apps/Zanance/docs/quality/occurrence-command-feedback.md).

## Independent SQLite cleanup (D-137 / AT-139)

Dispose owned contexts/providers before SqliteTestPools.Clear(TemporaryDirectory), then dispose the directory.
The helper calls actual LocalDatabasePools for existing *.db files inside that unique directory, including nested
fixtures/profiles. Custom strings require exact owner-scoped ClearPool. Never use ClearAllPools from a test: it can
reclaim another parallel test's native handle during migration. Real TEMP-table session markers prove isolation.
The normal strict full command now passes 2,210 cases without output runner configuration/collection serialization;
explicit concurrent writer scenarios remain concurrent. D-136's historical failures and zero-test attempts remain
negative evidence; output-only serialization was removed. Clean Vafadar.Tests.slnf after runs as before.
