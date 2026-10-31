# Bounded reference-array runtime-origin screen (D-708)

## Question and original evidence

D679 locates the original uninduced x64 fault in major_scan_ptr_field_with_evacuation,
called from the reference-array branch of sgen_client_cardtable_scan_object during
large-object card scanning and major completion. It does not identify the origin of
an invalid reference. D700 already proves the separate non-array LOS alignment fix
is present in the exact original module. Do not reapply it or replay successful
layout/financial cohorts as a supposed stability correction.

A distinct bounded question is whether ordinary pure-managed old-to-young writes in
a retained large reference array, without application/native navigation context, are
already sufficient to reproduce an inconsistent generated graph or native exit with
the exact original serviced SGen module. This is a diagnostic input, not an app fix,
new product stress requirement or acceptance of the original failure.

## Original upstream hypothesis screening

The [original reporter's correction](https://github.com/dotnet/runtime/issues/124941#issuecomment-3976734652)
withdraws the issue title's missing-Delegate-write-barrier interpretation.
The [maintainer's scope](https://github.com/dotnet/runtime/issues/124941#issuecomment-3991217009)
limits that correction to non-array objects larger than8KiB. That is the already
excluded D700 path, not a new reason to change Delegate handling in this app.

[Issue123537](https://github.com/dotnet/runtime/issues/123537) remains open in the
inspected response. Its original ARM64 concurrent/Tarjan observations are not an
identity match for D679's x64 evacuation/reference-card path. A
[maintainer's suggestions](https://github.com/dotnet/runtime/issues/123537#issuecomment-3833473665)
are investigation options, not an applicable released correction. Reported success
with another collector, a larger nursery or a newer runtime is not our product
acceptance. No collector/runtime/GC parameter is changed, no experimental runtime is
installed, and another report is not called the exclusive cause of this app's fault.

## Frozen independent input and exact runtime

One fresh independent non-debuggable/offline Release/AOT component contains no product
Data assembly, SQLCipher, financial fixture, original app storage or original UI
workflow. Native screen/provider access is bound to shellUID2000/ranchu/exactpackage/
unique run. The only local files are this fixture's one-shot marker and aggregate
result. No raw heap, register, private database or authentication data is exported.

Its packaged libmonosgen-2.0.so SHA256 is exactly
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`,
the original D678/D679 module. This does not claim every BCL/framework byte matches.
Generated build environment and actual native state both show
`MONO_GC_PARAMS=major=marksweep-conc`; MONO_GC_DEBUG is absent. There is no nursery,
bridge, canary or consistency-debug override.

Before work starts, populate one16384-slot reference array and perform one explicit
full collection to promote it. The retained array is generation1, the actual maximum.
Four workers own disjoint4096-slot partitions for one120second interval. Each reads
its own previous node, checks owner/slot/sequence, text length/first character and
identity of the node's text/byte-array/self references, then performs an ordinary
managed reference-array store of a new immutable node. No cross-worker slot race or
unsafe write is introduced. No explicit collection is requested during that interval.
The checks are not complete heap/byte-content validation and do not prove every
original native scanner instruction was executed.

## Actual outcome, without a false repair claim

Fresh API36/x64/page4096, package
`pro.vafadar.zanance.referencearrayruntimereview`, UID10216/PID3853, boot
`92618cc2-d325-4fc9-9cdd-9647aa469407` completes the one issued batch:

| Actual observation | Value |
|---|---|
| Elapsed work interval |120009.4714ms|
| Generated reference writes / bounded consistency checks |53891361 /53891361|
| Generation0 collection counter before/after |4 /11295|
| Generation1 collection counter before/after |1 /2258|
| Process |Same3853 throughout recorded liveness/packets; no restart|
| Observed generated-graph exception or native exit |None during this bounded input|

The work phase requests no GC.Collect; increased counters establish collections during
that phase, not an exclusive mechanism or scanner-coverage proof. Each provider read
first checks the actual original PID, so an absent process cannot be silently started
by querying the provider. One prematurely named terminal-observation packet actually
says running; it remains an intermediate observation. The later completion packet,
matching writes/checks, elapsed interval, actual process and collection counters own
the terminal outcome.

The caller's hardcoded OriginalFailureReproduced=false/Conclusion text is not itself
independent proof. The host outcome above uses actual phase/counters/liveness. Its
NoFinancialOrAppStorage label means no original product datastore; the fixture's
own marker/result files do exist. Neither label proves whole-app security or stability.

This simple generated array workload is not a reproducer of the original fault.
Do not repeat it with reverse/configuration rescue, call the crash fixed, discard
D678's actual failure or infer a device-only cause. Further diagnosis needs a distinct
original reference-origin/context input or an applicable verified correction; the
original application/native-resource conditions remain unresolved.

## Compilation, failures and retirement

Private compilation initially refuses ambiguous Activity, then Environment names.
Both caller-only names are qualified before the sole install/run. A host command
saving public upstream comments exceeds Windows' command-length limit before process
creation; only that unissued local write continues in bounded chunks. The same fresh
emulator boot's offline/not-ready observations continue on its live host, without
restart. No old accepted app test or financial input is replayed.

Strict final component89ownAOT/99ELF/386staticLOAD passes offline/non-debuggable/v2v3/
CRC/ZIP16KiB and exact original SGen checks. Used APK
`34aab8068494b282e504512da7cbdf5995dd5757084cc64d656edc72276dcf98`,
17935935bytes, is a runtime-only diagnostic, not the full shipping application.
Product code/GC/runtime dependencies and the prepared full D705 shipping89e83738 stay
unchanged. No new unit-test count or Windows/product build is needed for this input.

Original OS/boot/1080x1920/density420/font/timeout/accessibility/TTS values compare
exactly. Two owned XMLs3414bytes are hash-matched and removed; freshUID10216 is removed.
Host38304/qemu18096 exit and port5572 closes before official scoped AVD retirement:
25files4992742513logicalbytes. One standard Release Clean succeeds with469 registered
deletion lines, not a unique-file or host-free-space total.

Automatic approval review rejects the sole four-target supplementary deletion before
process creation with blocked by policy:379files47933103logicalbytes remain frozen.
Scopes are the new private source/output root, residual AVD root and two unused
publication files. Used signed APK/source/aggregate evidence stay outside. No retry,
subset, alternate method, move, escalation or reuse follows; earlier refused scopes
are untouched.

Phone/D607's three original deadlines, consumed reboot and unread post-reboot source
remain protected; Main adoption still waits for actual completion/cleanup. General
stability, two-second Home, broader DB/key/security/accessibility/iOS/provider/Store/
release acceptance remains open. The overall delivery goal stays active.

## Sole prior CI responses

D705 after17.26805minutes: [CI completed/cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41245153223),
[CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41245153257).
D706 actually21.86405minutes after scheduling: [CI completed/cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41245413794),
[CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41245413854).
D707 after16.38952minutes: [CI in_progress/no conclusion](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41246188345),
[CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41246188390).
Each consumed marker/raw response/receipt remains; each own schedule is deleted.
No second request, later inferred main-CI pass or cancellation-cause claim occurs.
