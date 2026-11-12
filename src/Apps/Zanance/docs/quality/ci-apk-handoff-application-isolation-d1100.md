# D1100 - APK handoff regression uses an isolated SDK application

The single retained D1099 CI response reports CodeQL success and
[CI failure](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43141650241).
Only the failed Windows job metadata/log is retrieved for diagnosis:
[job 130929911719](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43141650241/job/130929911719).
Android build and Libraries and tests succeeded; iOS was skipped. The Windows
build itself was never reached. Its Android APK handoff regression failed first.
No successful job log, rerun or second scheduled run-list query is needed.

## Original failure and current cause

The original handoff regression replaces `dotnet` with a PowerShell function.
`Invoke-ProjectDotNet.ps1` intentionally resolves `Get-Command dotnet
-CommandType Application`, so that function does not intercept the SDK. The
fixture accidentally starts a real restore before the native package preparation
step. The retained log reports NU1301 because the fresh runner's
`artifacts/native-sqlcipher/feed` does not exist. This is an actual test-isolation
defect still present in current source, not evidence of a broken Android package
or a Windows compiler failure. Creating an empty feed or moving native builds
ahead of a supposedly inert fixture would conceal the wrong boundary.

## Correction and focused validation

The fixture now places an inert `dotnet.cmd` application first on its temporary
process PATH on Windows; the portable branch uses an executable shell file.
It verifies Application resolution before invoking the unchanged original APK
entry point and unchanged project SDK wrapper. The fake application records only
the first argument and exits zero. All four original artifact-selection cases
(missing, empty, ambiguous and unique) pass on Windows. The exact command sequence
is four restore/publish pairs. No actual SDK restore, compilation, signing or
device command occurs. This is four existing changed-boundary conditions, not
four new product tests or APK signature/content acceptance. The portable branch
has not been executed on Linux in this batch.

PATH, the temporary command-log variable and LASTEXITCODE are restored in finally.
Each new uniquely named fixture lives inside repository artifacts and is removed
only after the existing resolved-parent/name cleanup checks. No historical or
frozen scope is enumerated, inspected or reused. Application code, native package
policy and the production build scripts are unchanged, so successful application
builds, managed suites and financial operations are not repeated.

Original metadata/log evidence remains in
`artifacts/d1100-ci-current-failure/original-job-evidence.json`.
The earlier sole D1098 response reports CodeQL success and CI cancelled, not
failure. Main7200/232137dc remains the dated D1099 observation, not fresh liveness.
Canonical parent States, retained cohorts and the full delivery goal remain open;
this CI repair does not qualify original native-fault cause, whole performance,
security, accessibility, platform, provider, Store or release acceptance.
