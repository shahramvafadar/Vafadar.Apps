# Protected native connection lifetime authority - D1179

## Four actual stale-authority failures

After the [separate options-cache retention fix](protected-context-options-retention-d1178.md),
a distinct ownership question remained: does a caller retaining a native factory
connection still pass ProtectedSqliteAccess.Require after disposing its context?
The original callback checked current publication and an optional live
maintenance owner, but not the exact context lease that had already drained.

Four new permanent contracts failed on the published baseline: synchronous and
asynchronous normal and maintenance contexts all accepted the retained native
connection after context disposal. Each first proved that access was accepted
while the context was alive. The maintenance cases keep the same original owner
active; the normal cases acquire the next exclusive owner before checking the
old connection. No publication, key generation or source replacement is needed
to demonstrate this defect. No connection was opened and no database/file or
financial row was created. The observed failure is authorization at this explicit
captured-native API, not proof of a real data disclosure or arbitrary native-code
sandboxing.

## Same original lease, no additional lifetime

The factory now includes the actual existing DatabaseAccessGate.ContextLease in
the captured protection callback. Its active-state check precedes the existing
publication and maintenance-owner checks. Context disposal retires that same
lease and drains its original gate exactly once. There is no replacement lease,
new public API, new owner, extra dictionary or separate registration lifetime.
The original connection/command interceptors and private-material/export check
share this callback, so a retained connection does not inherit access from a
disposed context merely because its publication or maintenance turn survives.

All four new contracts pass after correction. Eight directly affected existing
factory-return/constructor/publication/owner contracts also pass. That focused
repeat is required because the factory-to-lease handoff changed; no unrelated
successful suite or financial operation was replayed. These are twelve selected
host contracts, not a full suite. The existing key-free service graph from D1178,
context disposal, same-path publication checks, protected connection ownership,
model/schema, writers, financial formulas, key derivation, runtime and collection
policy are unchanged. Concurrent use of an already disposed context is not made
valid by this guard.

## Verification and preservation

Strict Windows and complete default-offline Android builds pass without warnings
or errors. The signed complete APK is
12483eed9a6d3ed6fd995adc481d1d3aaa0a81c5e51fdfb8f7a95e272bb47a89,
99,659,679 bytes, with384 AOT/408 ELF/1,608 static16KiB LOAD segments. Its v2/v3
signature, ZIP CRC/alignment, offline and nondebuggable policy pass. Original
stock Mono10.0.15 inputs are unchanged. Nineteen actual PDB document bindings
and six own-build AOT witnesses qualify the used inputs; they do not prove whole
managed-PE identity or change the original runtime fault evidence.

Only the four new exact tracked conditions run in the independent Android SDK
package, SHA256
9ef96cda7eb15090936d1800dbc910f6a5400382c33fe9599e7bbf3b22c4494b.
It has91 AOT/102 ELF/397 static LOAD segments, not the complete shipping graph.
All four pass in one final process4984/UID10216, API36/X64/4096, runtime10.0.15,
boot4e2162c5-e9ae-460f-8bee-3bb9ceb5876e. The linked generic test model and finite
native assertion driver are explicit; this is not a new original-Zanance model,
cryptographic operation, storage-open or financial trial. No collectibility
contract from D1178 or accepted native condition is replayed.

The identical complete shipping APK is then installed once into the originally
absent canonical package on that same fresh emulator. One ordinary launch gives
PID5124/UID10217. Its first two read-only captures show original Preparing
Zanance; the next requested XML is absent and its read fails. The original
process remains alive. An initial continuation incorrectly retains the fresh
package-absence guard and refuses before installation, launch or input; it is
not a second product attempt. The correctly bound same-PID/UID/boot observation
then reaches original Onb_WelcomeTitle, Welcome, with55 owned native nodes.
The three original XML files and observer refusals remain; no fresh dump is
issued for the absent failed capture. This proves original first-run startup,
not startup timing, whole-private-source equality, every migration/restore or
glyph acceptance. No account/profile form, Save, authentication or other input
is issued.

The five original OS values, locale/value/key absence, geometry, boot and reverse
list remain exact. Both fresh packages are removed. Three remote XML files/
31,104 bytes are removed only after local/remote SHA256 and length equality;
local raw evidence remains. Normal owned-emulator shutdown retires the original
launcher25740/QEMU40412 and closes the owned console; the original shared ADB
process/creation is preserved. Official AVD deletion removes26 files/
4,427,691,788 logical bytes, leaving six userhome files/7,732 bytes. Four standard
Clean operations retire15,513 files/1,486,620,753 logical bytes;3,117 files/
508,897,284 bytes remain in their build roots. These are logical counts, not
physical reclamation guarantees. Used signed/baseline/corrected binaries,
compiler/PDB/source/AOT evidence are retained. No supplementary or frozen-scope
deletion is attempted.

The original four failures, exact used baseline binaries, corrected results and
new native/package/compiler/restoration receipts are kept separately under
artifacts/d1179-native-connection-authority. Early owned-emulator transport/boot
observations refused before installation; the same original emulator processes
continued without restart, server reset, reconnect or pair.

Phone/Main, retained C7/Q02, consumed one-shot classifications, accepted financial
actions and frozen targets remain outside this work. This ownership correction
does not identify or repair the original SGen native fault, establish whole
performance, immediate key zeroization, broad security/accessibility, platform,
genuine provider/Store or release acceptance. The full delivery goal remains open.
