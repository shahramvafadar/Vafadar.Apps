# Scoped Android startup clock evidence (D-337 / QA-06)

## Measured boundary

The D-336 compression-index crash is fixed. The separate binding ANR still reproduces on the current
x64/API36 emulator. Do not repeat the rejected compression/extraction, SDK-profile-only or manual
ML Kit variants without new evidence. This investigation changes no application algorithm, runtime,
license/telemetry order, time source, crypto, financial data, permission or product policy.

The existing corrected diagnostic APK adds only profileable and two Application.onCreate markers.
The ordinary D-336 signed APK is restored afterwards without clearing data. Diagnostic APKs are not
owner handoff candidates. Samsung is absent; these observations are not physical-device acceptance.

## Original process captures

| Original process | Actual trigger and result | Captured evidence |
| --- | --- | --- |
| 31288 | Package-replaced ScheduledAlarmReceiver; starts300407.884, enters OnCreate300416.648, completes300432.080; startup ANR300427.877 and actual process death300439.712 |20s cpu-clock:u capture,37 samples, zero lost;19 broken callchains. ANR counters show1.1% user/28% kernel. The callback completing later does not undo ANR. |
| 31449 | Explicit cold Activity only after31288 is actually dead; starts300500.873, enters OnCreate300502.130, exits300504.838; Activity reports6845ms |20s cpu-clock capture including kernel,7355 samples, zero lost;4307 broken callchains. Kernel symbol addresses remain restricted. |

Use monotonic nanosecond filter files on the already recorded31449 data; do not rerun the app to
separate phases. Main-thread samples before managed entry are172, of which145/84.30% are in vDSO
clock_gettime; ART class resolution and Mono timing are callers. Inside the exact OnCreate interval,
888 main-thread samples include669/75.34% in the same vDSO offset, with74.44% accumulated through
mono_100ns_ticks. Linker/mmap/mprotect work is also visible. The aggregate20s report includes later
rendering and worker threads, so its percentages are not startup-only measurements.

The first user-only capture is too small to blame its apparent LZ4/ICU percentages for the delay.
Frame-pointer unwinding is incomplete, so do not invent a missing managed caller. The samples locate
a substantial clock-reading cost; they do not prove a specific hypervisor defect or the sole cause
of every ANR, nor statistical improvement between these different background/foreground launches.

## Independent native control

Compile a tiny isolated executable with the already reviewed official NDK r30/API24 x64 clang,
-O2 -Wall -Wextra -Werror. Its source performs10000 calls per clock/mode and measures their complete
loop with two explicit monotonic syscalls. It uses no application, account, database, credential,
network, framework or MAUI code. The executable SHA256 is
c278eb9f1fb707620241c0dec4cf434c5d8e60f1a813e872c06db29e50f6cd8f.
The pinned existing native-host image verifies its original tool lock and runs without network,
capabilities or a writable container root. It writes only the owned diagnostic directory.

|10000 calls | Existing review AVD, milliseconds | Fresh independent AVD, milliseconds |
| --- | ---: | ---: |
| libc monotonic |408.1563 |159.1783 |
| libc boottime |275.6175 |182.6009 |
| libc realtime |330.1070 |164.5344 |
| libc raw |203.4310 |200.1636 |
| libc coarse |0.0461 |0.0442 |
| direct syscall monotonic |179.6302 |215.1239 |
| direct syscall boottime |239.7916 |214.6103 |
| direct syscall realtime |300.3942 |218.4865 |
| direct syscall raw |336.5428 |171.8967 |
| direct syscall coarse |7.7488 |0.5133 |

External host wall times are2457.8ms and1582.1ms, consistent with bounded complete-loop costs plus
transport overhead. These are single diagnostic samples, not a latency distribution. A coarse clock
is a diagnostic contrast, never a proposed replacement for precise application/runtime clocks.

The fresh project-owned Zanance_Clock_Control_D337 AVD uses the existing same API36 Google image,
matching fingerprint BE2A.250530.026.F3/13894323 and kernel6.6.66-android15-8-gd0c43a640eab-ab13812146.
It configures four CPUs/2048MiB and starts without snapshots, audio or a desktop window. Host free
physical memory exceeds51GiB before launch. Emulator37.1.11 logs actual WHPX initialization. The
original AVD is neither stopped nor reset. The independent result contradicts an explanation based
only on the original AVD's age; it does not identify its private clocksource or prove all environments
behave this way. The current stable release notes establish no specifically applicable clock fix.

## Safety, cleanup and ordinary restoration

Both perf files contain sample_type487 and no RAW, REGS_USER or STACK_USER fields. Only exact own
PIDs and frame-pointer callchain addresses are requested; no DWARF/raw-stack/heap capture, root,
global perf setting or system clock change is used. Do not follow the profiler's suggestion to alter
kptr_restrict. A denied clocksource read is retained as a limitation, not retried with privilege.

Match hashes before removing only the two owned remote profiling files and native control executables.
Remove the three owned filter files. Verify the fresh AVD's exact console name before shutting down
only emulator-5580; retain its isolated local configuration/evidence. Transport-id selection does not
identify an emulator console when multiple emulators exist: use the verified explicit serial for emu.
The command-line-tools19 batch wrapper fails on the installed Java path's parentheses; direct invocation
of its original AvdManagerCli/classpath/toolsdir property creates the control without editing installed
tools or global configuration. Preserve the failed invocations and the successful original control.

Reinstall the unchanged ordinary D-336 APK, SHA256
806fe9a68f498252c81b1833490de78663dbb239b4c80eeb628002e155b99182.
Its original receiver process31643 again records startup ANR and actually dies. A later explicit cold
Activity starts31704 and reports4249ms; its initial hierarchy remains covered. Retain that same PID
through storage preparation rather than restarting after observation expiry. Its later native hierarchy
shows the original My account and recorded0.00 EUR Home with normal actions in the same31704 process.
No financial Save or settings write occurs. No successful financial,
OCR, UI matrix, managed suite or build is repeated: application code/APK remain D-336.

QA-06 binding/cold/low-memory statistics, loaded-Home two-second performance, real device, iOS and
release acceptance remain open. Do not ship a coarse clock, native fork, weaker protection, changed
license order or another speculative packaging variant to make this emulator sample pass. Continue
ready release work independently of unavailable physical/Mac/account gates.

Local evidence: artifacts/startup-sampling-d337, including original PID captures/format checks, exact
phase filters/reports, clock source/build receipts, original/fresh CSVs, isolated AVD process/configuration
and scoped restoration hierarchies under artifacts/import-preview-d331/android/d337-*.

Primary references: [official application profiling](https://android.googlesource.com/platform/system/extras/+/android16-release/simpleperf/doc/android_application_profiling.md),
[timestamp filter format](https://android.googlesource.com/platform/system/extras/+/refs/tags/android-15.0.0_r36/simpleperf/doc/sample_filter.md)
and [current Emulator release notes](https://developer.android.com/studio/releases/emulator?hl=en).
