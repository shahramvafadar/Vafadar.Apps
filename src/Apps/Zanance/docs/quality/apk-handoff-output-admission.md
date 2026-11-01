# Reject unusable APK handoffs (D-729)

## Actual original entry-point failures

The original Build-AndroidApk.ps1 trusts publish exit0 and then takes the newest matching file.
Exercise that actual script with an isolated build-command fixture, not a real SDK build. Each
case executes its original restore/publish command boundary once, returning0 while using a new
public file fixture. All three original failures are recorded: missing signed APK still prints
APK with an empty path/0MB and installation advice; an empty signed file receives the same advice;
two distinct matching signed filenames are arbitrarily reduced to one. No actual build, signing,
secret read, device command or financial operation occurs. Preserve those three failing results
and their tiny local failure inputs; they are explicitly not Android packages.

## Narrow output admission

After the existing warning/error guard, require exactly one matching **file**, then require its
length above0 before any handoff output. Missing/empty/ambiguous output throws with a concrete
error; mixed applications need separate output directories. Do not choose the newest arbitrary
APK, delete existing output, alter signing/edition/restore/publish arguments or touch frozen scopes.

Four focused PowerShell cases exercise the actual entry point: the three regressions now refuse,
and one unique nonempty artifact is selected. That positive file is plainly a tiny non-APK fixture:
this tests filename/count/length admission, not ZIP, embedded assemblies, package identity,
cryptographic signature or current-build provenance. Those existing full handoff gates still apply.
The fixture replaces only restore/publish commands inside its script scope, counts exactly8 calls,
restores working directory/process exit state and removes only its new resolved temporary root.
No toolchain installation or real app build is hidden behind this result.

Register that focused script in the existing Windows CI job before workload installation. There
is no new job, permission, secret or external service. Actual remote status belongs to the later
one-shot exact-head response. No successful unrelated suite, Windows/Android app build, native
scene, financial command or standard Clean without generated build output is repeated. These
are4 build-script checks,0new application unit tests.

## Delivery and protected scope

Product source/APK is unchanged from D-728's full signed0308b242 candidate. Its no-reset Main
adoption still waits for actual D-607 completion/cleanup. No physical command is issued here;
all three original deadlines, consumed reboot and unread post-reboot source stay protected.
The saved timed continuation is stricter than the observer's early code guard: it authorizes no
device command before1November08:05UTC. Read that saved restriction and honor it; do not run at
the observer's earlier five-minute pre-deadline boundary. Continue independent work meanwhile.

This admission guard does not establish installation, whole APK integrity/provenance, general
security/stability/performance/A11Y, physical/iOS/provider/Store/release acceptance. Android
completion precedes iOS; the overall delivery goal remains active.
