# Source-admitted growing-list runtime input (D-711)

## Distinct question, not a repeated application test

D-679 locates the original D-678 fault in evacuation pointer scanning, reached from
reference-array card scanning during major completion. It does not identify the
managed array or the writer of its bad reference. D-700's non-array correction is
already present; D-708's fixed-array element-write input did not reproduce the fault.
Do not replay their application/financial cases or infer that another similar
upstream issue proves this app's cause.

Current original Home readers supply a different concrete operation family:
`HomeLedgerReadBlocks.TryReadAsync` and `CompleteHomeLedgerReader.ReadAsync` each
start an empty List<LedgerEntry> and append materialized rows. At current product
head0312ce87, that entails reference-list capacity growth rather than D-708's fixed
retained array. The two original source-file identities are recorded locally.
This is an admitted source-level hypothesis, not proof that either list was the
original D-678 failing array or that a particular copy caused corruption.

## Same-version original library path

Read original raw bytes at the actual runtime package revision
0427b9abbead3fa64c6655eb4290c537397131fa before choosing an input:

- [List.Add/AddWithResize/Grow and Capacity](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L114)
  reallocate backing storage and copy prior elements; its ToArray also copies them.
- [Mono Array.CopyImpl](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/System.Private.CoreLib/src/System/Array.Mono.cs#L127)
  tries the original FastCopy path before its managed fallback.
- [Native FastCopy](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/metadata/icall.c#L829)
  distinguishes value-type and reference-array copies, with a reference-copy helper
  on the latter branch. That branch is not evidence of a missing barrier or of the
  exact instruction selected by the new AOT caller.

The saved raw source hashes are List1346574b/Arrayd6dda39d/icall6d969a3f. Raw-byte
line receipts, not web-extracted blank-line numbering, own those locations. No
applicable new runtime correction is established by this source review. No source
from an unrelated issue or third-party reproducer is adopted.

## Frozen independent input

One fresh offline/non-debuggable Release/AOT runtime-only component contains no
product Data assembly, SQLCipher, original app datastore, application UI workflow
or financial fixture. A shellUID2000/ranchu/exact-package/unique-run guard and
exclusive native marker admit one batch. Every provider observation first requires
the actual original live PID; a missing process cannot be restarted by a query.

Four worker tasks are requested for one120second interval. Each has independent
local lists and immutable generated node ownership. A cohort starts an empty
List<Node>, adds100000nodes through the ordinary library Add, then uses ordinary
ToArray as an additional identity oracle. There is no pre-sized replacement,
unsafe write, forced collection, shared mutable slot, collector/runtime override or
configuration rescue. The started cohort finishes after the time limit rather
than truncating its verification. ToArray is the diagnostic oracle, not a claim
that the original Home readers explicitly call it.

Each cohort requires actual Count100000/Capacity131072. Every copied position must
be identical to the original list position and retain expected owner/cycle/slot,
text length/end characters, byte-array length/end bytes, child-reference identities
and null marker. These are bounded generated-field checks, not full byte/heap
validation. Actual capacity-change count and prior list Count at those changes
are accumulated. The latter is a logical measure, not native-copy instruction or
write-barrier telemetry. Copied-array generation and collection counters are also
recorded. Per-worker throughput and actual scanner-instruction coverage are absent.

## Actual bounded result

API36/x64/page4096, fresh UID10216/PID3974, boot
ba200531-5f95-4f69-ac7f-efba870ea97a completes the single admitted batch:

| Actual observation | Result |
|---|---:|
| Elapsed work |120157.3947ms|
| Complete cohorts |1467|
| Nodes added / copied-reference identity checks |146700000 /146700000|
| Actual capacity changes |23472|
| Logical prior elements at growth |192276756|
| Copied-array minimum/maximum generation |1 /1, actual maximum generation1|
| Generation0 counter before/after |0 /13683|
| Generation1 counter before/after |0 /1502|
| Observed generated-field failure or native exit |None in this bounded input|

The host requires actual complete phase, original PID/UID/run, elapsed at least120s,
matching nodes/checks, cohorts*100000, growths*16 and prior-count sum*131068, generation
and collection-counter observations. Intermediate running packets with partial
cohorts remain intermediate; their counters are not forced into final ratios.
The caller's fixed OriginalFailureReproductionNotEstablished flag and storage/forced-
collection labels are not independent outcome proof. The implementation requests
no collection; increased collection counters show collections occurred nevertheless.
Four requested tasks do not establish four-core concurrency or per-worker coverage.

This shows only that the admitted ordinary growing-list/copy input completes here.
It does not reproduce, refute or repair the original crash, identify its reference
origin, establish every native copy/scanner path, or accept whole-app stability.
Do not repeat/reverse this input or iterate GC settings after its result. Another
action needs an original object/writer/native-context input or a verified correction
which addresses the actual original fault. No speculative app patch follows.

## Build, observation refusals and retirement

One strict component publish succeeds with zero warnings/errors. Its APK SHA256 is
`eca32ad4c502b959047cb4de1a334f4625f7c53c1d21da48dad3343b4d81836b`,
17935931bytes,89ownAOT/99ELF/386static16KiBLOAD segments; offline/non-debuggable,
v2/v3 signature, CRC and ZIP16KiB guards pass. Its actual packaged SGen hash is
edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4,
exactly the original failure's module. Actual environment reports default
MONO_GC_PARAMS=major=marksweep-conc and absent MONO_GC_DEBUG. All BCL byte identity
or physical16KiB acceptance is not inferred.

The first package guard still expects the old component assembly name and refuses
before installation. Only that observer name is corrected to actual ListGrowthNative;
the original-SGen/AOT/permission guards stay. Boot-not-ready continues on the same
live host; the initially unissued install cannot read an absent proof and exits
before its issue marker. There is one actual installation/start/batch, no restart,
financial setup, original UI replay, dependency/runtime change or new product test.

Original OS/boot/locale/1080x1920/density420 and all captured settings remain exact.
Only fresh UID10216 and two owned hash-matched XMLs/3374bytes are removed. Actual
host82904/qemu49976 and console5572 close before official fresh-AVD retirement:
25files/5012206599logical bytes. One standard scoped Clean passes with469 registered
deletion lines. These are logical counts, not measured host free-space recovery.

One supplementary four-target removal is rejected before process creation with
blocked by policy:379files/47941614logical bytes remain frozen. Its scopes are the
new private source/output root, residual owned-AVD root and two unused publication
files. No retry/subset/alternate/move/escalation/reuse follows. Used named sources,
signed component, public source receipts and aggregate observations remain outside;
earlier refused scopes are untouched.

Product sources are unchanged, so D-710's accepted strict Windows/full shipping
09140d86 and focused tests are retained without repetition. Its sole exact-head CI
check at actual16.2029minutes reports
[CI in progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41252210492)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41252210489).
That schedule is deleted with no second request or assumed later result. D-711 gets
one own exact-head check after the complete documentation push. Phone/D-607's three
deadlines, consumed reboot, unread source and Main adoption condition remain intact;
no phone command occurs. Wider DB/key/security, ANR/crash, two-second Home,
accessibility, iOS/provider/Store/release and the overall delivery remain open.
