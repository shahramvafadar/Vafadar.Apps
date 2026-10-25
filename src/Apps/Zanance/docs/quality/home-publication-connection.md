# D-541 - Qualified Home publication connection lifetime

## Objective and current scope

D-540 retains complete financial history and qualifies a transport improvement, but its
100000-row Home publication still exceeds the two-second requirement. This batch measures the
remaining original publication and qualifies one bounded connection lifetime, adopted without
reset on Samsung. It is not a full-suite result or a stability/release acceptance.

The initial Samsung Main baseline is the complete signed D-540 candidate, SHA256
`e0d3d1844e5896a0081ff5c6d9dc1c3fae823e26d3ebea40a6ee52c5d78fbcd5`, PID27774,
Home979, all141 public states and the original three profiles. No private Main data/key is read.
The retained independent workload run is `d53542fe57d6446ab52b82a757ee7f6e`.

## Original bounded observations

Independent-only EF diagnostic subscriptions collect at most512 numeric monotonic events from
owned context identities. They never format/retain event payloads, SQL, values, paths or keys;
foreign contexts are excluded. No interceptor is added to candidate transport. Subscriptions
detach outside their observer lock to avoid lock inversion with the diagnostic listener.

The first diagnostic APK has SHA256
`fed338513b1ff998de6a4110d484049ea2721a5633adf919e1103340e9366c2c`.
Original scalar controls complete in5901, with proven death before measurement7677 on the same
APK. Complete source/schema/typed rows, financial/native/recent/presentation controls match;
no seed, new key or accepted financial command repeats. Measurement7677 completes3393.2449ms,
maximum dispatcher gap475.5627ms, two contexts,114 events and zero drops. The second context's
observed span is408.6021ms; its opening-to-opened interval is170.7235ms. The first context's
last observed command to the second context's first event spans941.873ms. These intervals do
not identify internal KDF/query/materialization/GC causes. Native raw-transaction disposal need
not emit EF connection-close events; absence is not evidence that a connection stayed open.
The first derived summary mislabeled the408.6021ms second-context span as an inter-context
window; retain that receipt and the authoritative raw packet, without repeating publication.
Both diagnostic processes finish and are proven stopped before the next candidate.

## Candidate lifetime and original safeguards

`HomeReadSession` owns the original factory context only through one covered publication.
The complete snapshot uses the unchanged original SQLite read transaction. That transaction
ends before projection. Final validation starts a separate fresh transaction on the same
connection and performs the unchanged original file, access, in-memory input, financial revision
and definition checks. There is no global connection/key cache, pooling policy change, weaker
KDF, model/schema change or shortcut validation. The old public read/validate APIs still close
their own contexts, using the same extracted original implementations.

An exact source-body comparison with521cfd verifies both original worker bodies and the complete
financial projection/final-publication bodies remain unchanged. This is source-retention evidence,
separate from the actual SQLite transaction/lifetime/authority and native acceptance receipts.

The actual Home read loop disposes each operation before period/source retry, including file
guard, projection and validation failures. This avoids holding an old factory lease while a
pending exclusive maintenance writer blocks the next read. The original day/culture and
covered refresh/file ownership remain unchanged. The outer bounded source-supersession policy
is retained. Disposal serializes with validation and releases the original context once.

## Focused host evidence so far

Strict data, application-flow and Windows builds finish with zero warnings/errors. Eight new
real SQLite session cases verify one context, maintenance drain/idempotent disposal, separate
committed financial/definition edits, original input mutation, cancellation, retired authority
and failed-begin/reader cleanup. Four actual non-UI application cases verify both period-change
boundaries, file refusal and a single accepted metadata write followed by bounded reread while
covered. Three directly affected original file/authority/plan cases pass. One earlier observer
case proves context isolation and unchanged actual block eligibility. These are16 distinct
selected cases, not16 new tests or a full-suite claim.

Preserve the initial runner option refusal (no test execution), and the two initial new-case
serialization failures before their writer/assertion: `NameCulture.Parent` is a cyclic culture
graph and the packet also contains authority references; it is not a JSON export. Compare the
actual account/entry sources instead. Only those two
failed cases continue; four successful cases are not rerun. Both continuation cases pass.

The compiled actual source-identity predicate now binds validation mode in addition to the
existing source/day/module/transport scope. A changed/missing/unsupported mode refuses reuse.

## Completed native comparison

Complete offline/non-debuggable/non-profileable independent ARM64 candidate SHA256
`d494e0c69bda046294d5afdaf8dce1b39ff215d3a6818b547d85123187fb1c0a` contains192
actual packaged AOT images equal to built images,204ELF/804LOAD static16KiB checks, valid ZIP
alignment/CRC and v2/v3 signatures. These are structural evidence, not runtime16KiB acceptance.

One independent-only adapter calls the unchanged two-context APIs; the production adapter uses
the retained operation. Both run the same actual Home projection and ledger transport in this
same APK. Mode is immutable in the premeasurement fingerprint. Each measurement has its own
completed/dead scalar-control process, original complete source proof and actual context/path
observation. The first separate-mode preparation starts11921 with comparison
`257405b862c64245b43e7d2165e0ed77` and completes with the same original control digest.
After proven preparation death,12511 measures separate validation3518.1483ms with two actual
contexts and all source/financial/native/presentation guards. New retained-mode controls12735
complete with the same digest and are proven dead before13036 measures3310.2394ms with one
actual context and the same complete guards. Both measurements finish and are proven stopped.
The first ordered pair improves5.9096115988%, above the declared5% threshold; that permits only
the second opposite-order pair, not adoption or a two-second claim. Reverse-order retained
preparation13213 completes with comparison `f7143a94f2794c7cb0e8efff2441d856` and the same
original control digest. After its proven death,13758 measures retained3261.4481ms. New separate
controls14558 complete and are proven dead before14924 measures3443.1894ms. All four actual
publications retain the complete original source/financial/native/presentation guards, actual
one-versus-two context count and immutable same-APK mode. Both final measurements are proven
stopped. The reverse pair improves5.2782835588%, also above the declared5% threshold. This
qualifies the bounded repair, not the two-second objective or broad stability. No financial
seed/key/accepted operation is repeated.

An initial fresh connection inventory finds the historical5038 server absent; the ADB client
automatically creates48028. Record this drift rather than claim the old server76812 remained.
Existing5037/44944 is preserved. Samsung is rediscovered at192.168.178.31:45851 and connected;
read-only verification confirms Main27774/owned foreground, then full original public/OS/APK
proof matches before the one new independent no-reset install. No system setting is changed.

## Package adoption and retained failures

Read only the four completed native packets to classify the remaining publication intervals.
Retained navigation-to-first-control is1690.6731/1770.1699ms, first-control-to-final-model is
1412.9245/1279.1501ms, and final-model-to-native-ready is206.2121/211.6267ms. These combine source
materialization, calculations, validation, scheduling and native binding; none is an exclusive
CPU/KDF/GC/query cost. Static inspection of the actual packaged Data/Core AOT images exposes no
ordinary static function symbols, so it cannot prove missing AOT methods or justify a profile
change. The codec already decodes bounded spans directly; no speculative replacement or successful
native/financial replay is made merely to fill the foreground wait.

Independent retirement15232 completes: only normal empty Main/UID10381 reverses the exact M1/M2
internal migration suffix to its old50-migration prefix. Full memory/source proof preserves
financial rows and keys, with no shipping/workload touch, then15232 is proven stopped. The one
original-APK restore attempt waits at Play Protect. After the owner's personal decision, that
same live install returns successfully; there is no reinstall. Original b1d5/UID10381 normal
startup16026 reaches original Step1/Welcome/Next, then is proven stopped. Its exact original
CE/DE identities, grants and user flags match. Main27774 remains owned foreground afterward.

The first full dual-ABI shipping build succeeds but an additional exact Data/Core byte check
refuses equivalence to the independently qualified build. Preserve the refusal; do not install
that output. Inspection finds generated private Regex type-name hashes and PE/PDB identities
depend on the different artifact path. Metadata tables/constant blobs match, but attempted PE
identity normalization still leaves differences and is not exact-image/code proof. Rebuild only the complete package using the same
dependency artifact root, retaining immutable copies of the qualified native dependencies.
The final package verifies exact Data/Core equality against those immutable copies, with every
packaged AOT image equal to its actual build output. It has SHA256
`a2d98e2d7425aee77ae58e35f349fc565502d13c1d293b99189b2a0b49b23e70`,97071007bytes,
384AOT/408ELF/1608LOAD static16KiB checks, ZIP/v2v3 and original offline permission boundary.
No successful host/native/financial cohort repeats for this build.

One complete signed no-reset Main install succeeds. The post-install foreground guard refuses
`de.loyan.business` before any input or launch command. Preserve that refusal; Main17997 is alive
and public APK bytes match a2d98e exactly. Ask the owner only to return Zanance to foreground;
never reinstall/restart or send input to the unrelated window. At that checkpoint final readback
is pending; its later actual acceptance follows below. Completed test/diagnostic/Windows and first unqualified shipping build outputs are
cleaned through scoped targets. After inventory, remove only the three unneeded first publish
outputs,194711332bytes; preserve the qualified APK and all original sources/required backups.

Later, actual `pidof` confirms17997 absent. Android exit-info records device-local
2026-10-25 16:33:09.673, UID10380, reason10 `USER REQUESTED`, subreason22 `REMOVE TASK`, status0.
This proves task removal, not the identity of whoever requested it, a crash, low-memory recovery
or an observed product failure. Preserve the prior successful byte proof and this terminal
lineage. A separately authorized limited Business Settings turn returns Zanance through normal
Resume to29566; fresh own-app foreground/UID/APK guards bind that actual new PID, never the same
17997 continuation.

Main a2d98e/PID29566 preserves Home979/all141 public states, all three original profiles and
exact OS settings after the one signed no-reset upgrade and authorized normal Resume. Original
independent caller bytes/UID/storage/grants/stopped state match; ten new owned hash-matched XMLs
are removed. The earlier foreground refusal and17997 external task removal remain recorded.

## Independent nonempty compatibility continuation

Two newly owned independent x64 compatibility fixtures qualify nonempty100000-row block reads
and actual original Home publication on API24/4096-byte pages and API35/simulated16384-byte
userspace pages over a4096-byte kernel. Each fixture is seeded once with110000 reference/tenfold
rows; scalar controls finish in a separate proven-dead PID before the one native publication.
Complete original schema/typed-row/financial/native guards pass with one context, one block read,
zero scalar reads and no post-setup financial command/key/seed replay. Both initially absent
packages are removed and only the two owned AVDs are stopped, preserving their settings.
This is not hardware16KiB, whole-platform, startup, stability or two-second acceptance.

The complete signed x64 independent APK has SHA256
`4e07ec7a2ae5218ad90f58ad21c1de8826bafc2cc0f3fff97eec1b7c6a8d63f9`,58957730bytes,
192AOT/204ELF/804LOAD static16KiB, ZIP/v2/v3/offline guards. Actual Data/Core images exactly match
the immutable qualified shipping dependencies. Original50-migration sources are created using
the retained signed x64 caller, then only the exact M1/M2 suffix is adopted without reset.
API24 setup2685 finishes/dead; current scalar controls2828 finish/dead; native2926 completes
2654.2889ms. API35 setup3479 finishes/dead; scalar controls3746 finish/dead; native3842 completes
2266.5356ms. These single-case elapsed times are not comparative performance evidence. Kernel
mapping fallback confirms API24 page4096; API35 reports userspace16384/kernel4096 explicitly.
No physical-phone command is used in this independent cohort. Initial metadata-parser refusals
are preserved separately from successful native operations; no accepted cohort is replayed.

The bounded batch is qualified and adopted. The main goal and wider two-second performance,
stability/accessibility/platform/provider/Store gates remain open.
