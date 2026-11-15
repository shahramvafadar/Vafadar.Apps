# Independent Windows emulator crash-reporting control (D1203/D1207/D1208)

Later outcome: [D1209](home-payload-stamp-cpu-completion-d1209.md) reuses the
qualified local workaround for the retained D1202 measurement, with normal host
shutdown/exit0 afterward. Its completed source equality supersedes the historical
retention authority below; the original diagnostic pair is not repeated.

## Exact question and one-shot observer

The owner asks to continue the host diagnosis after safe cleanup, keeping the
broad delivery goal paused. D now has more than 49 GiB free through another
project's reported cleanup, independently checked before launching. The preserved
independent D1201 AVD is reused, never protected C7/Q02 or phone/Main.

[D1203](host-crash-report-exit-code-d1203.md) already classifies `0xffff7001` as
Crashpad's no-dump self-termination code, not the original exception. Its prepared
bounded Windows observer is now issued exactly once: original retained host,
no app install/Activity/provider/input producer/financial action, 32 exception-row
bound, module basenames/relative code locations only. No process memory, register
context, raw address or dump is read/output. The debugger is configured not to kill
on exit; at 300.0579 seconds it detaches the three live owned processes successfully.
There is no second debugger run or consumed Android classifier interpretation.

6 netsimd.exe descendants exit with unhandled C++ code `0xe06d7363`, reported at
KernelBase.dll+0xc408a. No QEMU exception is observed during the bounded window.
The normal installed helpers' separate `--help` invocations both return 0. These
facts do not identify the C++ type/message, establish the original host exception,
or show that netsimd caused QEMU's historical termination. The instrumented run
uses process-local reporting disabled and eventually reaches actual Android boot 1.
Original ready/cold Home or old successful app tests are not executed.

## Normal no-debugger comparison

After normal console shutdown ends original launcher 5080/QEMU 75820, D1207 launches
once without a debugger, with metrics off and crash-report-mode never (reporting
installed, no upload consent). It reuses the same independent AVD/SDK, scoped
original environment and original settings. Actual QEMU 92036 becomes wholly
suspended, CPU stops advancing, the reporting database has zero-byte metadata and
no report files, and no new matching application/security event supplies the
missing original exception. Process-query-only metadata says no debugger port.

The official scoped console shutdown request waits. One exact-owned-process
resume is issued solely to allow that pending normal shutdown; no force-stop,
TerminateProcess, signal, reset or new launch is used. The host then ends with
`0xffff7001`; a zero client exit is not relabelled as a clean zero host exit.
The no-dump failure and missing original event remain explicit. No frozen process
is silently restarted on an observation timeout.

D1208 is one distinct normal no-debugger control. The only changed emulator option
from D1207 is `-crash-report-mode never` to `disabled`; the scoped environment,
TEMP directory, AVD, SDK, memory/CPU/graphics/no-snapshot settings and metrics-off
flag are retained. The actual installed host confirms reporting is disabled.
Android boot completes in 67798 ms and `sys.boot_completed` is 1. Normal owned console
shutdown succeeds and launcher 91500/QEMU 20796 end with host exit 0; no raw resume or
forced termination is needed in the successful control.

## Supported conclusion and preservation

For this retained environment the reporting-enabled path reproduces suspension
and no-dump termination; the process-local reporting-disabled path supplies one
verified usable startup/workaround without a debugger. This does not prove an
exclusive SDK root cause from one pair, recover the original initiating exception,
fix upstream code or qualify product-native crash/stability/performance. Emulator
boot time is not application startup speed or the two-second acceptance gate.
A future owned launch may use reporting-disabled/metrics-off locally without
repeating this diagnostic pair; no global SDK/system/security setting changes.

Four public reporting sources are retained from immutable official qemu commit
`ae9d18d2b6261179fbd57fffec720a04f7bfb053`, including CrashSytemInit.cpp's disabled
branch/upload-consent controls. They explain the mechanism, not whole source
identity for installed 37.1.11/build 15917651. No private report attachment or archive
is opened, and no telemetry/crash upload is requested.

The independent AVD retains 32 files/11330868544 logical bytes; no userdata hash or key
is extracted. Package/data are not reset, reseeded, uninstalled or upgraded. The
last full-source digest remains the historical D1201 receipt, not a fresh equality
claim. Both successful-control host handles end; shared ADB 5037/PID 59860 creation
stays unchanged, with no server restart/reconnect/pair. Phone/Main, protected
cohorts and terminal financial/authentication/classifier records remain untouched.
The ready D1202 payload/stamp observer remains unissued. Whole delivery stays
paused and its remaining gates are not marked complete.
