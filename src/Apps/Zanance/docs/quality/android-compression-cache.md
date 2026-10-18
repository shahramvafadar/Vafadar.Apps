# Android compression cache consistency (D-336)

## Observed native failure

An ordinary incremental rebuild of the temporary Release profiling candidate installs successfully,
but processes30581 and30713 abort before managed entry. The runtime reports an assembly-store
uncompressed size2494464 exceeding its descriptor limit171008. This is a separate failure from the
previous application-binding ANRs. No user database, key or authentication state was read to diagnose it.

The generated x64 descriptor source assigns System.Private.CoreLib to192 with2494464 bytes and
Java.Interop to193 with171008 bytes. The cached CoreLib XALZ blob retains index193 and size2494464.
ARM64 similarly retains193 for the current192 slot. Thus timestamp-only reuse preserves a stale
descriptor index even though the assembly bytes themselves have not grown. The native abort is
consistent with the exact wrong slot and size; it is not evidence that profileable itself causes ANR.

## Focused build correction

The shared serviced Android build imports AndroidAssemblyCompression.targets. After the SDK collects
the current compression items and before its incremental compression check, inspect each existing
12-byte XALZ header against the SDK's actual DescriptorIndex and source byte count. Invalidate only
incompatible files inside this invocation's own intermediate compression directory. Missing entries
are left for the SDK; valid cache files remain untouched. The original SDK performs compression and
packaging; no header rewriting, native fork, format/KDF change, uncompressed APK or broad clean is used.
Invalid descriptor metadata or an outside destination fails closed before deletion.

eng/tests/AndroidCompression.Tests.ps1 exercises the actual imported target without application
restore or device state: valid cache preservation including its timestamp, changed index, changed
size, wrong magic, truncated header, missing entry and outside-path refusal. CI runs this focused
check alongside the existing Windows-hosted tooling checks. It adds no managed financial test count.

## Earlier finite startup experiments

Removing only the supplemental application AOT profile while retaining all three MAUI defaults shrinks
aggregate native AOT bytes80754864 to68193024, but the actual original process29777 records a binding
ANR and33885ms through OnCreate. The application AOT library itself does not shrink. Reject the trial;
retain the original profile and do not repeat large financial or OCR suites on a candidate that fails
its initial startup gate.

An independent trial removes automatic ML Kit provider initialization and uses its documented public
manual initialization on first OCR use. Original process30334 still records ANR and28806ms through
OnCreate. Reject and restore it; do not ship a speculative shared-document lifecycle change or repeat
unchanged successful OCR cases. Licensing, telemetry, crypto and runtime versions remain unchanged.

Exact-PID main-thread frames from Android's retained ANR traces show nativePollOnce/epoll at capture
for30018 and30334. These late idle snapshots do not establish the earlier blocker or prove deadlock.
Retain only the own-process stack addresses/symbols and scoped public CPU counters, never other process
traces, heap content, credentials or private files. Kernel-heavy CPU is evidence, not an environmental
root-cause verdict.

The first profileable native capture attaches to30713 but receives zero samples before the verified
assembly-store abort. It is a failed startup capture, not a timing baseline. No root, global profiling
settings, raw stack or heap dump is requested. Profiling uses frame-pointer callchains only.

## Qualification boundary

The corrected diagnostic candidate starts original process30880 through the existing package-replaced
ScheduledAlarmReceiver. Its first managed marker is299838888 and OnCreate completes299842824;
the original process start is299835377. No new ANR is recorded for that retained process. This one
sample proves passage through the previous crash site, not a statistical startup improvement. A local
sampler observer initially mishandles pidof's expected no-process exit; the later real PID is retained,
not restarted to recover observation. Do not profile an already completed startup as if it were cold.

All temporary MainApplication markers and the profileable manifest entry are restored byte-for-byte
and touched. Windows strict build succeeds with zero warnings/errors; the ordinary full offline
Android Release build succeeds with no reported warning/error. The signed final APK is
artifacts/android-compression-cache-d336/pro.vafadar.zanance-Signed.apk,94785269 bytes,
SHA256806fe9a68f498252c81b1833490de78663dbb239b4c80eeb628002e155b99182.
It retains the original certificate SHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b,
minAPI24/target36, ARM64/x64,384 AOT libraries, no INTERNET/debuggable/profileable flag, and identical
SQLCipher/Mono runtime bytes for both architectures. It is installed with-r without a data reset.
The final ordinary process31043 starts through the original receiver and remains alive through the
Activity request. The first own native hierarchy shows covered preparation; a later read of that same
PID shows My account and Recorded balance0.00 EUR with normal Home actions. No financial Save,
preferences write, private database/key inspection or process reset is used. No new own ANR/native
abort is recorded for that process during this observation. This is native crash-fix qualification,
not a cold-Home timing or full database comparison.

Unused old compression files are not an inventory of the current package. An extra exploratory
all-cache comparison is rejected: the generated common descriptor capacity can exceed a particular
ABI's source size, and unused ML Kit entries from an earlier trial may retain old indices. The shipping
target uses only the SDK's selected current items and their per-item source lengths; it never deletes
unselected files based on this exploratory scan.

QA-06 application-binding ANR, loaded-Home two-second performance, physical-phone, low-memory,
ARM64/API24/16KiB, Mac/iOS and owner/provider/release gates remain separate. A repaired assembly cache
must not be reported as resolving the earlier ANR. Unchanged financial, crypto, backend, OCR and More
layout suites are not repeated.

Primary sources: the SDK's [compression task metadata](https://github.com/dotnet/android/blob/release/10.0.1xx/src/Xamarin.Android.Build.Tasks/Tasks/CompressAssemblies.cs)
and [XALZ header writer](https://github.com/dotnet/android/blob/release/10.0.1xx/src/Xamarin.Android.Build.Tasks/Utilities/AssemblyCompression.cs).
The related [upstream incremental-store report](https://github.com/dotnet/android/issues/12162)
concerns .NET11/CoreCLR; it supports investigation but does not establish this Mono application's
runtime result. Local evidence is retained under artifacts/startup-native-sampling-d336,
artifacts/startup-footprint-d336, artifacts/startup-ocr-d336 and artifacts/compression-header-tests.
