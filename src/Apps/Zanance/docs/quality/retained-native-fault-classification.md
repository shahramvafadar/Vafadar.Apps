# Retained native failure classification and numeric OS qualification (D-438)

## Original failure identities

D-437 is delivered at403b8e9e8ef3b4caa3420b0edd9e24262b74af38. Its completed monetary
source/native cohorts and accepted metadata commands are retained, not repeated for stability.
The108 corrected Windows note observations completed before PID82900 terminated with0xC0000005;
the58-outcome process succeeded separately. Android's actual ANR dialogs remain failures even when
Wait preserves the same process and its later work completes. Observation expiry is not a crash.

The exact Windows Application Error1000 identifies coreclr.dll10.0.1326.50504 at RVA0x3590bf.
Its module SHA256 is18eed8c40d05fc455be95d12cc77eacf379265063b15ca84bd7a11cf29078a22.
The public PDB identity BFA5339556F349D7B500232511B5203D1 matches the original module's CodeView
GUID/age and downloaded PDB stream. Offline symbolic lookup resolves ProcessCLRException+0x13f,
exceptionhandling.cpp:621 in official commit4099f83977f4d29604a9aeb971fb0a7bc1d5f699.
That line deliberately handles a fatal corrupted-state exception; it does not identify the original
memory-corrupting component. The same process's earlier Runtime1026 event supplies the symbolic
WinRT.IObjectReference.Finalize and System.GC.RunFinalizers boundary. This is insufficient to assert
a WinRT double free, a CoreCLR defect, or a shared cause with Android's separately symbolized Mono
bridge failure. No process attach, heap, raw stack-memory or register capture is used.

Android's retained independent exit metadata classifies17030 and17556 as package-updated exits,
and18000 as the explicit terminal force-stop after completed work. Their retained trace fields are
null. They do not supply an original ANR thread trace or prove that the actual dialogs did not occur.
The empty last-ANR service response is also not evidence that those observed dialogs were absent.

## Distinct environment control

One fresh, project-owned API35 control uses the already installed official Google image, revision9,
fingerprint google/sdk_gphone64_x86_64/emu64xa:15/AE3A.240806.043/12960925:userdebug/dev-keys,
kernel6.6.50-android15-8-g8adecb593e9b-ab12525588. Emulator37.1.11 initializes WHPX with four CPUs,
2048MiB, no snapshot, audio or desktop window. Its AVD registry/user paths are isolated temporary
directories; the existing5570 AVD and Samsung are untouched. No image/tool download is needed.

Reuse the exact reviewed D-337 native executable, SHA256
c278eb9f1fb707620241c0dec4cf434c5d8e60f1a813e872c06db29e50f6cd8f, without rebuilding it or
running any application/financial workload. A first controller launch refuses the unsupported
adb-port argument before boot; the final invocation uses the process-local Android ADB port.

|10000 calls | libc milliseconds | direct syscall milliseconds |
| --- | ---: | ---: |
| monotonic |156.5352|109.7483|
| boottime |152.0832|111.6830|
| realtime |114.6400|103.0243|
| raw |125.3499|100.4194|
| coarse |0.0402|1.1550|

This single bounded control does not establish a distribution or an application speedup. Precise
clock cost persists with a different installed image/kernel; changing only the original API36 image
is not an established cure. Coarse clocks remain a diagnostic contrast, never a product replacement.
After boot completion and exact remote hash confirmation, remove only the owned remote executable,
verify the exact Zanance_Clock_API35_D438 console name and stop only5582. Original Main17310 stays
alive. No owner desktop, global hypervisor/clock/kernel setting or original AVD is changed.

The serviced stable .NET10 runtime remains10.0.13, matching AOT compiler/task/runtime. Upstream
issue123537 is still open. Do not repeat rejected serial-major, precise-Tarjan,64MiB or new-bridge
experiments, adopt an experimental runtime, weaken protection or infer a general fix from a finite
successful run. Further runtime remediation and native/ANR acceptance remain open.

## New numeric OS scope

Actual OS200% review of the new physical/rate fields uses the original D-437 independent source
and its final complete50-table checkpoint. It never repeats accepted metadata/financial commands.
The caller requires resumed independent identity, actual font scale2 and an explicit360/412 scope;
separate immutable reports cannot collide with original input/outcome receipts. Native labels must
retain their complete text, no ellipsis and enough height for their actual final layout line.
The96 distinct completed observations contain360 actual native readings:48 at360dp in PID18659
and48 at412dp in PID18958, en/fa/de and Light/Dark. Complete original50-table final checkpoint and
independent base rows remain unchanged; accepted type/rate/delete/ledger commands are all zero.
The360dp process records an actual startup ANR before its first startup-wait marker. Its actual
owned dialog's Wait target preserves18659; all48 observations later complete in that same process.
This is not a fix or a statistically meaningful comparison with the separate412dp launch.

A later hierarchy dump cannot reach idle and leaves an old remote file despite its controller exit
code. That stale hierarchy is not used for another input or accepted as a new observation. Final
capture uses a unique owned filename and requires the actual dump-success marker before pulling.
The completed scope reports, not a timeout or stale hierarchy, authorize stopping each terminal
independent process. A local packet-summary reader's missing UTF-8 selection is corrected against
the existing saved reports without rerunning a scene or command.

Strict independent Release/AOT compilation completes with zero warnings/errors and192 AOT modules.
The signed offline nondebuggable review APK SHA256 is
7f6a0a8acaa9b40af2becd0dd9c529d8d0fe3fe14914434631ff5c6e3b50dc12.
Only this independent package upgrades. The complete shipping APK remains the exact D-437 package;
no Windows/Main APK, previous managed/resource cohort or financial command is repeated.

Restore font1.0, physical412x800 with no override, density160, accessibility0/null. All six original
OS/PID values match exactly; original Main17310 is foreground with My account/0.00 EUR. No Main
private database/key read, account reset, phone input or system setting outside this owned emulator.
The independent caller is stopped after its completed source-preservation receipt. Local immutable
reports preserve both scopes, packet reconstruction and restoration evidence.

Local evidence is retained under the isolated D-438 qualification directory: original event/module/
PDB identities, symbolic classification, control source identity/configuration/CSV and scoped cleanup.
No new managed-test count, full-suite, phone, spoken-reader, iOS, provider or release acceptance is
inferred from this work. Main shipping code/APK remain D-437 unless a separately verified defect
requires a change.

Primary references: [exact exception handling source](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/coreclr/vm/exceptionhandling.cpp),
[official public symbol file](https://msdl.microsoft.com/download/symbols/coreclr.pdb/BFA5339556F349D7B500232511B5203D1/coreclr.pdb),
[stable serviced releases](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json),
[upstream bridge issue](https://github.com/dotnet/runtime/issues/123537).

## Exact pushed-commit CI

The single check for3af93373e87837fe201ca4ef75a5a8da7689798f reports both actual workflows
completed successfully: [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39656387001)
and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39656387122).
Its one-shot follow-up is deleted and its request consumed. These exact-revision results do not
establish a native runtime fix, all-control/spoken acceptance or publication readiness.
