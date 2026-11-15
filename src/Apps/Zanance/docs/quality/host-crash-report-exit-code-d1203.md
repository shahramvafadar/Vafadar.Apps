# Host crash-report termination classification (D1203)

## What the saved exit actually establishes

[D1202](home-payload-stamp-preparation-d1202.md) records the retained independent
emulator host ending with 4294930433 (`0xffff7001`). The official immutable
[Crashpad termination enum](https://chromium.googlesource.com/crashpad/crashpad/+/c4cdec3d72a2d2abad84a98bc0bcf525333b3280/util/win/termination_codes.h)
names this `kTerminationCodeCrashNoDump`: the crash handler did not respond and
the client self-terminated. The public Windows
[exception-handler implementation](https://chromium.googlesource.com/crashpad/crashpad/+/refs/heads/main/client/crashpad_client_win.cc)
also demonstrates the no-dump self-termination path after its bounded wait.
The installed 37.1.11/build 15917651 QEMU image contains the corresponding literal
`crash server did not respond` once and the failed-launch literal twice. These
selected bindings support the termination classification, not whole installed
Crashpad source/binary identity or recovery of the original exception.

The exact D1202 crash database has zero report files, no attachment entries and
zero-byte metadata. Only directory/file metadata is inspected; no dump, memory,
thread context, register or guest data is parsed. The first attempt's named report
directory likewise has no report in the incident window. A bounded Application
1000/1001/1002 check has no matching host event. Lack of those records cannot
identify the original fault. The earlier all-thread suspension is not proof of
an external actor or a recoverable paused VM: crash reporting/debugging can itself
suspend threads. No further raw process-resume operation is issued.

## New host-only diagnostic preparation

Current official `emulator-check.exe accel` returns 0 and states WHPX 10.0.26300
is installed and usable. This is current accelerator availability, not proof of
an entire guest boot or a cause/remedy. No OS resource-exhaustion event 2004 is
found in the selected incident System-log window; this is not blanket memory
acceptance. Current free RAM does not reconstruct incident resource state.

A new observer uses Windows debug events only. It emits bounded exception codes,
first/second-chance labels, module basenames and in-module relative code offsets;
raw addresses remain ephemeral. Public on-disk PE headers supply image bounds.
It never calls process-memory, thread-context, dump or forced-termination APIs.
Native x64 structure-size checks and source/API review pass without a host launch.
Debug-event continuation preserves original exception handling; debugger-exit
termination is disabled and a timeout detaches actual owned processes, without
restarting or killing them. The official installed help exposes the process-local
`-crash-report-mode disabled` option; the prepared command uses it with
`-no-metrics` to avoid dump/report upload. No global setting is changed.

The initial help-output capture fails on default-codepage decoding; only that
failed capture is corrected to raw bytes/UTF-8. It is not a VM or application run.

## Unissued execution and actual blocker

D has 686661632 free bytes at the preparation observation, below the 3 GiB guard.
The host diagnosis is not launched; the owner is asked to free space while data
and evidence remain retained. No original exception/module location is invented.
The prepared observer targets only the existing independent D1201 AVD, with no
new input, app install/Activity/provider/financial operation or phone/Main command.
Protected C7/Q02, consumed classifiers, frozen outputs and successful checks are
not retried. The original host fault remains unresolved pending that bounded
execution; `0xffff7001` is now classified, not treated as the original fault code.
The broad delivery goal remains paused as requested; only this owner-requested
host-diagnosis task is in progress.

[Executed host control D1208](host-crash-reporting-control-d1208.md) later consumes the one prepared observer, reproduces the normal reporting-path failure and qualifies one normal reporting-disabled startup/shutdown. The original initiating exception remains unknown and no app workload is replayed.
