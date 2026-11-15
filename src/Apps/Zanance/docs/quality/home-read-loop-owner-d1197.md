# Home read-loop page ownership (D1197)

## Two observed owner failures

The original `HomePublicationReadLoop` disposes superseded operations, but did not
check the owning Home publication before starting or repeating a full read.
The outer refresh and cover owners prevent later publication; they did not
prevent the loop from beginning a second real store read after its page retired.

Two new selected host cases use independently migrated empty fixtures and the
actual operation source/access gate, refresh session and snapshot cover. One
retires the owner when the first read returns with a changed period; the other
retires it during projection before that period retry. Both originally fail:
expected one Begin, actual two. There is no account or ledger seed and no accepted
financial operation to replay.

## Correction and focused verification

The Home view model now passes its existing `RequireCurrentPublication` guard
into the loop. Ownership is checked before beginning a read, after its completion
before inspecting its snapshot, around original file/period inspection and after
projection before a retry or successful return. An already running operation
finishes under its original store rules and is disposed by `await using` on
refusal. No cancellation-token source, mid-SQL abort, parallel read, key cache,
financial policy, schema or snapshot-comparison relaxation is introduced.

The two new host cases pass after correction. Three directly affected existing
empty-fixture cases pass on that same corrected host binary: both period retry
shapes and original-file refusal. The existing source-edit case that commits an
account is not invoked. This is2new+3affected selected host cases, not a full suite
or five new tests.

The same two new tracked methods are source-linked into an independent Android
SDK package with the actual Home loop, refresh session, cover and Data library.
They pass once in PID3879/UID10216 on API36/x64/4096, runtime10.0.15. The independent
fixture migrates empty databases, then requires accounts/entries/schedules empty
before disposal. The driver invokes only those two methods; it does not invoke
the other compiled host methods or their financial setup. Six actual compiler
PDB documents bind exact selected source inputs and the own AOT module equals
the package member. This is native component execution, not a genuine MAUI page
retirement gesture, whole source equality, cold-Home timing or speed acceptance.

The helper APK is32,553,107bytes, SHA256
`3b925ba95d0a1cc8ab7707b099b37a97d499ddbe0a87103d2d72c0cbf948f0b6`,
92AOT/103ELF/401static16KiB LOADs. Offline/nondebuggable policy, v2/v3, ZIP CRC and
alignment pass; stock x64 Mono15 remains unchanged.

The initial helper compile called the schedule getter on the wrong service;
that real CS1061 failure is retained and only the fixture getter was corrected
to the actual PlanStore. No successful method had executed. Early transport
observations precede the same emulator's111222ms boot completion; they are not
app failures. The original launcher12072/QEMU44904 remain through qualification,
with no second boot or relaunch.

## Product preparation

Strict Windows and complete default-offline Android builds finish with zero
warnings/errors. The full APK is99,684,255bytes, SHA256
`e59b5746aa79360e5b56c238e16e3b371fe7837d0f6f9bc89aab9dc6ac883e1f`,
384AOT/408ELF/1608static16KiB LOADs. v2/v3, ZIP CRC/alignment and offline/nondebuggable
policy pass. Five actual product compiler documents bind the selected Home/
startup/cover inputs and both own product AOT modules equal the package. Private
Core version5 and stock Mono15 are unchanged; producer/source/API preparation
and successful dependency scans are not repeated. The complete product is
prepared, not installed on a canonical emulator package, phone or Main. No
first-run/layout/authentication or financial acceptance is inferred.

The final evidence observer initially required a literal build-summary line in
the publish log, which this successful publish does not emit. That observer
refusal is retained; final validation uses the recorded publish exit0 and absence
of compiler/MSBuild warning/error diagnostics. Neither build nor tests are rerun.

## Exact retirement and limits

The fresh SDK package is removed after its two terminal cases. Five OS values,
locale value/key presence, geometry, reverse list and boot remain exact. No UI
capture is created. Launcher12072/QEMU44904 handles signal following normal owned
shutdown; the same ADB5037/PID59860 creation identity remains. Official AVD
retirement removes24files/10,567,431,619logical bytes; six files/7732bytes remain.
Four standard Cleans remove15,656files/1,569,237,215logical bytes;3,166files/509,948,828bytes remain
in their owned build roots. No supplemental deletion is attempted.

Used signed packages, selected PDB/source/AOT, raw failures, host counts and native
results remain. There is no supplemental/frozen deletion, phone/Main or protected
C7/Q02 input, consumed register reclassification, owner authentication or financial
write. Original in-flight source safety is preserved; stopping already running
SQL sooner is neither implemented nor claimed.

[The current CI guard correction](android-untrimmed-core-rids-d1198.md) was handled
as a separate observed failure before finishing this batch. No successful CI job
log, old layout/font cohort, accepted financial operation or rejected performance
candidate is replayed. The next commit's sole delayed CI response is not inferred.
Original native-fault cause/remedy and whole performance/security/accessibility/
platform/provider/release gates remain open; canonical parent States are unchanged
and the full delivery goal stays active.
