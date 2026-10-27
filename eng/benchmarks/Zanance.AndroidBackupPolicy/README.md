# Read-only Android backup eligibility (D-603)

This independent shell diagnostic asks the existing Android framework about its
current backup eligibility. It is not an app, backup agent, product dependency or
payload-transfer test. Never package it in Zanance or grant it a new permission.

## Admission and scope

`QueryBackupEligibility` requires shell UID2000, actual API36 and user0. Emulator
mode requires ranchu hardware. Phone mode requires SM-A176B and excludes the system
control package. Bind the permanent and retained independent package to their
original actual UIDs before any backup-service query. No arbitrary package, Binder
transaction number, method, user or credential is accepted.

Only `isBackupServiceActive`, `isBackupEnabledForUser` and
`isAppEligibleForBackupForUser` are invoked on BackupManager. PackageManager supplies
only the fixed package UID bindings. Exceptions exit3 with their type and an explicit
missing-result flag; a failed query is never converted to `ReportedEligible=false`.
No setter, backup/restore/request, transport selection, account/restore-token query,
private database/key read, APK install, app input or financial operation is called.

The observer uses the framework's hidden diagnostic interface from the already
authorized shell; it is not a supported API for an ordinary product application.
Do not change hidden-API policy, root authority or permissions if admission fails.
The framework may consult its existing transport's eligibility policy. This tool
does not request a backup payload or claim that the transport cannot use a network.

## Build and observe

Use the existing official Android36 SDK and local JDK. In a fresh evidence directory,
compile this source with `javac --release 17 -Xlint:all -Werror`, Android36's
`android.jar` on the classpath, then D8 with `--release --min-api 24 --lib android.jar`.
The min-api build setting does not qualify API24: runtime admission remains API36.
Retain source/JAR digests and strict compiler results; no signing/install step is used.

Select the existing project-owned ADB server and exact emulator/phone transport;
never start/reconnect a shared server to repair an observation. Record original
package APK/UID/CEDE/flags/stopped/PIDs, backup activation/enabled state and relevant
settings before and after. Push only the digest-bound JAR to a fresh exact owned
`/data/local/tmp/Zanance-D603-...jar` path. Run it with that CLASSPATH through
`app_process /system/bin QueryBackupEligibility MODE MAIN_UID CALLER_UID`.
Arguments are `emulator` or `phone`, then the two recorded original UIDs.

Emulator mode additionally queries the already installed Settings provider UID1000
as a positive control. It does not read that provider's data. Phone mode queries
only the two owned packages. Keep an absent service, disabled mechanism, unexpected
UID, exception and positive-control failure explicit. Do not enable backup or choose
a transport to manufacture a passing result. A result describes current framework
eligibility, not OEM/device-transfer behavior or successful data exclusion from a
real payload. After completion, remove only the new remote JAR after hash matching;
preserve local used build witnesses and original evidence.

[Actual results and preserved state](../../../src/Apps/Zanance/docs/quality/android-live-backup-eligibility.md).
