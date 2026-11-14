# Factory creation cancellation handoff - D1181

## Four actual cancellation failures

A supplied creation token was observed only while entering the gate or scheduling
the background worker. Once that worker was running, a closed native creator or
context constructor could synchronously cancel the same token and the factory
would still return a successful context. Four new contracts reproduce this on
the published baseline: creator/constructor callbacks in normal and maintenance
creation all return without a cancellation exception. Used baseline binaries and
the original four failures are retained, not replaced by a successful result.

The callbacks perform no database work. A closed connection subclass refuses and
counts every Open/OpenAsync attempt; fictitious public creator labels never become
cryptographic keys. No app/profile/account/ledger setup or financial operation
is needed to expose the handoff defect.

## Creation-only checks and original cleanup

The original creation token now follows the existing private construction path.
It is checked at entry, after the native creator returns and at the original
post-constructor completion boundary. A cancelled creator result is disposed
before another context is constructed. A constructor-cancelled context is
disposed before return; existing catch/finally paths release its captured native
connection and the same original gate lease. Publication/owner checks and the
[D1179 captured native lifetime guard](protected-connection-lifetime-authority-d1179.md)
remain in place. No second lifetime, task, credential publication or public API
is introduced. Synchronous creation uses the original non-cancelled default.

The token is not retained in the native connection/command validation callback.
Two additional new contracts first accept a context, then cancel its creation
token: the original publication/owner/lease still authorizes that accepted
context. Cancellation of future work remains the caller's responsibility; this
is not a global atomic cancellation deadline or revocation of a previously
accepted context, key, source or financial action.

All six new host contracts pass on one final binary: four early cancellation
refusals and two after-handoff controls. Counts prove zero construction after
creator cancellation, one context already disposed before test cleanup after
constructor cancellation, exact original exception token, native connection
disposal, drained gate and zero storage opens/files. Eight directly affected
existing constructor/publication/owner return contracts also pass after this
shared completion-boundary change. These are fourteen selected host contracts,
not a successful full-suite replay. Models/schema, writers, financial formulas,
cryptographic settings, runtime and product collection policy are unchanged.

## Verification and terminal ownership

Strict Windows and complete default-offline Android builds pass without warnings
or errors. The complete signed APK has SHA256
fdea74ef8f80af3965ff26db05246d5f061628036a99c3ac4004f9477847396f,
99,659,679bytes,384 AOT/408 ELF/1,608 static16KiB LOAD segments, with v2/v3
signatures, ZIP CRC/alignment, offline and nondebuggable policy qualified.
Original stock runtime10.0.15 remains unchanged. Twelve actual source-document
bindings and six own-build AOT witnesses connect the used Windows/complete
Android/SDK inputs; whole managed-PE identity is not inferred.

The same six exact new tracked conditions pass in one final independent native
process4957/UID10216, API36/X64/4096, boot951f9f6c-d02d-40a5-9496-622141de5a3e.
Its SHA256 is
10b0a554a3775265f7db076918de006101e1c1ecf384afd43b9227e1d506dc00,
with91 AOT/102 ELF/397 static16KiB LOAD segments and the same signature/offline
checks. It is not a complete MAUI package. All four before-handoff refusals and
both after-acceptance controls share this final source/binary; no old successful
cancellation cohort or financial operation is replayed. The independent driver
uses only closed guarded test connections and performs zero storage opens/files.

The identical complete APK is installed once into the originally absent canonical
package on that same fresh emulator. One ordinary launch gives PID5089/UID10217
and reaches the original Onb_WelcomeTitle text, Welcome, in55 owned native nodes.
Two read-only XML captures follow that same process; no input, profile/account
Save or authentication occurs. This is actual first-run startup, not full private
database/source equality, every migration/restore, speed or glyph acceptance.

The original five OS values, locale/value/key absence, geometry, reverse list and
boot remain exact. Both fresh packages retire; two remote XML files/25,538bytes
are removed only after exact local/remote size and SHA256 equality, with local raw
evidence retained. Normal emulator shutdown retires original41060/QEMU93800 and
closes the owned console; shared ADB5037/PID59860 creation is unchanged. Official
AVD deletion removes25files/4,443,616,860logical bytes, leaving6userhome files/
7,732bytes. Four standard Clean operations remove15,513files/1,486,723,509logical
bytes;3,117files/508,894,513bytes remain in those build roots. Logical counts are
not physical reclamation guarantees.

Used signed/baseline/corrected binaries, source/PDB/AOT and raw proof are retained
outside publish roots. A separate single cleanup of this step's two redundant
publish roots is rejected before process creation with blocked by policy. The
pre-refusal inventory is6files/264,924,540logical bytes. Both exact roots are frozen:
no post-refusal enumeration, rehash, retry, subset, alternative, move, escalation
or reuse. The original used signed APKs had already been hash-matched into
used-evidence/shipping.apk and used-evidence/native.apk before that refusal.
Owner-removable absolute paths are recorded in
artifacts/d1181-factory-cancellation-handoff/confirmed-redundant-publish-paths.md;
no containing evidence directory or older denied scope is proposed for deletion.

Versioned baseline, corrected host and actual package/native/compiler/restoration
receipts remain under artifacts/d1181-factory-cancellation-handoff. Phone/Main,
retained C7/Q02, consumed one-shot classifications, accepted financial operations,
rejected performance candidates and frozen targets remain protected. This
correction is not proof of the original native fault cause/remedy, whole
performance/security/accessibility/platform/provider/Store/release acceptance.
The full delivery goal remains active.
