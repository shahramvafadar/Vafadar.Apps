# Sync-departure ownership on minimum Android and ps16k — D-1049

The eight [D1047 departure conditions](personal-sync-departure-feedback-d1047.md)
now have separate API24 and API35 ps16k qualification. This checks that changed
code on two missing platform boundaries; accepted API36/host cases, financial/
identity operations and existing Main delivery do not replay. No product, dependency,
runtime/GC, permanent test or build change occurs.

## One immutable package and two actual runtimes

The used D1047 SDK package is reused byte-exact, not rebuilt:56733602bytes, SHA256
`19d806b1164706ea26f78f89f2b0da3b3c3ee44a719261ae9b044537ca2ea25c`.
Its original192AOT/204ELF/804static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/
alignment proof and nine actual compiler-source bindings remain preserved. Those
nine current source files still match the used PDB. Installed bytes are independently
hashed against that package proof; there is no new package qualification or host suite.

| Boundary | Actual runtime and process | Page-size observation |
| --- | --- | --- |
| API24/x64 | .NET10.0.14, UID10062/PID2867, boot`b26d2a52-7c0f-4129-a6ad-35ee578d143f` | Filtered kernel/MMU4096; no missing getconf/pm-U tool probe |
| API35 ps16k/x64 | .NET10.0.14, UID10207/PID2926, boot`c3c0bec6-ff26-4d82-a962-e1edafe6acd4` | getconf16384; filtered kernel/MMU4096 |

Both use separate fresh2048MiB/two-core AVDs. Read-only inspection observes the same
original startup until ready and proves the phase has not started. Each original
phase is issued once, and a later read-only inspection retrieves the exact retained
terminal packet. No install, launch, action, source preparation or accepted callback
is repeated. ps16k is userspace compatibility evidence, not hardware16KiB or managed
SystemPageSize acceptance.

## Eight scoped conditions per platform

Each platform passes six actual original model conditions for request/preview
changing/changed feedback and reentrant old-field repopulation. Original fake frame
bytes are zeroed, request text empty, preview null, Close unaccepted and covered
read/Back available. The controlled model caller is explicit; it is not a genuine
credential exchange or native-decoded document.

One further held-read case cancels/ends its one original attempt once, retains covered
recovery and invokes only an explicit unaccepted-Close retry without another read.
The eighth condition uses the actual original first-run page/native Back: failed
departure leaves the modal with translated error metadata, one original native Reload
restores readiness/nonsecret device draft, then one native Back returns to the exact
original root. It is native-control invocation, not physical OS touch or a layout/
glyph/contrast/speech matrix. No provider/authentication/financial operation occurs.

Complete sqlite_master schema/all54tables/56typed rows stay equal before/after each
phase, with account/entry/plan/allocation/authentication and transport counts zero:

- API24 digest:`CCB12B05C45DB3D31486B7636D2972658E37F81459A4EB2DAE82C3C2261C2B9E`.
- ps16k digest:`C35631AE18F203D5C1B1E41DCD878C7C6FFDA33A4330DAFD563358C44683AB32`.

Fresh source timestamps/digests need not match between platforms. Each cohort keeps
its own PID/boot/OS/locale/key-presence/geometry/reverse state exact. These are16native
platform observations of eight conditions, not16new product unit tests.

## Exact retirement and continuing scope

Only the two fresh UIDs retire. Eight original emulator handles become signalled;
existing ADB5037/PID59860creation ticks639293902901445383 remain exact without reset/
restart/reconnect/pair. Official deletion retires16API24 files/9483592301logical bytes
and24ps16k files/4067607235logical bytes. No build output or remote XML was created,
so no old standard Clean or supplemental/frozen delete is repeated. Used source/
signed bytes and raw inspection/results remain preserved. API24userhome4files5285bytes,
ps16kuserhome6files7732bytes and2host-temp files40bytes per cohort remain. Logical sizes
are not measured host disk savings.

There is no phone input in this compatibility step. [D1048 Main21772](current-departure-main-d1048.md)
is only the last dated observation, not fresh liveness/private source equality.
Canonical parent States, retained5606/Q02, terminal D607/D752 and accepted enrollment/
financial checkpoints remain unchanged. Original native-fault cause/remedy, whole
security/performance/accessibility/private key/data, genuine providers/iOS/Store/
release and full delivery stay open. One post-push CI check does not delay ready work.
