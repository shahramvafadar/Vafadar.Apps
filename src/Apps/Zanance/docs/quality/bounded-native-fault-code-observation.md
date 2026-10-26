# Bounded native fault code observation (D-563)

## Purpose and acceptance boundary

D-561's actual ARM64 runtime14 SGen worker SIGSEGV has no accepted native tombstone.
Do not infer its instruction from EXIT_SELF/status255 or retry previously rejected GC settings.
This batch adds an opt-in observer only to the independent Android Home package. The shipping
application, runtime, GC configuration, financial algorithms, database model and permissions do
not change. An observed SIGSEGV may be recovered by the runtime; a record alone is never proof
of a fatal crash or its cause.

Before installing the observer into a managed application, qualify original signal chaining,
concurrent bounded recording and the actual record reader on both available architectures.
Then run one finite retained-source native cohort with the changed instrumentation, without
replaying preparation, keys, seed, accepted financial commands or old unrelated tests.

## Record and privacy contract

`native/fault_pc.c` resolves executable PT_LOAD ranges before installing its handler. Production
diagnostic module IDs are limited to `libmonosgen-2.0.so`, `libmonodroid.so` and `libsqlcipher.so`.
A separate controlled-origin module exists only behind `ZANANCE_FAULT_PC_SELFTEST` and is absent
from the packaged observer. Hardware signals outside those executable ranges, and generated
signals, pass through without consuming record capacity.

The fixed little-endian record is72 bytes: protocol/version, architecture, signal/module IDs,
owned UID/PID/TID, signal code, sequence, module-relative PC, monotonic timestamp and a16-byte
run token. Only the faulting instruction pointer is read transiently to derive the relative
offset; no absolute pointer, fault address, registers, raw stack, heap or database content is
written. The handler does not unwind, allocate, format, look up modules or enter managed code.
An atomic counter admits at most32 records; each invocation uses its own packet and one append
write. A short/failed write is not silently accepted as complete evidence by the reader.

Installation requires the actual current UID and a new, empty, owned0600 regular append file.
The native path wrapper uses exclusive/no-follow creation and retains its own close-on-exec
descriptor. Unsupported ignored/reset/nodefer dispositions are refused. The original runtime
handler receives the same signal context; an original default hardware disposition is restored
before refaulting the same instruction. No global signal, security or system setting changes.

`NativeFaultPcRecords` validates exact process/run/ABI, bounded complete records, unique sequences,
timestamps and externally supplied original executable ranges/build IDs. It returns code metadata
only and always requires independent fatal evidence. A guarded inspection Activity binds the
module catalog to the actual installed APK hash, reads only the named private record and reports
own OS exit metadata separately. It neither starts a data review nor claims an OS tombstone.
There is no network, crash SDK, upload, analytics or automatic shipping crash collection.

## Toolchain and component controls

Review official Windows NDK r30/30.0.16248370 metadata and terms; extract only the required local
clang21/sysroot/notices into the dedicated review directory. No global SDK/environment change
or Docker image is needed. The redundant official727980788-byte archive is hash-checked and
removed after extraction. Build the own native sources strictly for API24 ARM64/x64 with
16KiB static alignment. These build properties do not qualify API24 or hardware16KiB runtime.

Fifteen distinct actual-reader cases pass selected runtime14 executions: fourteen initial cases
and one corrected real shipping-module-name case. Do not count these as product financial cases.
New standalone component controls pass once on API36/x64/4096 and physical API36/ARM64/4096:

| Control | Actual outcome on each architecture |
| --- | --- |
| Admission | Wrong UID, pipe, nonempty record and unsupported reset disposition refused; exit0 |
| Recovery | Generated and unmatched signals preserve original handling; two matched records, four original handler calls; exit0 |
| Fatal | One matched controlled record; actual exit139 and process absence |
| Concurrent recovery | Eight threads, forty recovered signals, thirty-two complete bounded records; original handling preserved |

The actual managed reader validates those real records against the compiled controlled functions
and original build IDs. These shell-only controls are not Mono, AOT app, financial or stability
acceptance. All ten owned emulator and seven phone remote files are hash-matched and removed;
their roots and four processes on each device are gone. Phone Main7028/8fd5, both original APKs,
five settings and the actual owned window are preserved. No phone app installation/UI input,
financial operation, private Main key access or new all141-node acceptance occurs.

## Actual managed application cohort

Preserve initial CA1416 and local-name compile failures; both subsequent independent strict
publishes succeed without warnings. Reject the first successful APK before installation because
its Core/Data PE bytes differ through CodeView output paths. Reproduce the original output
context and embedded revision explicitly, without modifying financial source or weakening the
byte guard. Do not interpret that metadata reproduction as a claim that all current sources are
the historical revision.

The accepted x64 APK is `ebd910c2ec484884db93014a2df83b4858c65319564dedd79a2bb08542ce88be`:
59195377 bytes,192 own AOT modules,205 ELF files,807 statically aligned LOAD segments, valid ZIP,
offline/nondebug/nonprofile and v2/v3 signatures. Its decoded Core, Data and compression PEs and
actual Mono/Brotli/cipher engines equal shipping8fd5. The own observer bytes equal their reviewed
input; controlled test code is absent. No all-linked-BCL identity claim is made.

The first normal `install -r` is terminally refused for insufficient emulator storage. Preserve
that receipt. Remove only the stopped independent package's code with KEEP_DATA, then install
once; UID10221, CE/DE inodes, permissions and retained sources survive. This is code replacement
after a real storage refusal, not continuous upgrade acceptance. Do not resize the AVD or remove
retained financial sources/keys to make room.

Run `home-current-stages` once with original retained run25b9dab64bf849fcb24e39246d773215 and new
comparison7c5314bc13a34385a571c77fc9d2a35d. In PID4976 the actual native arm returns0 and a caught
managed null-reference exception remains recoverable; no claim is made that it used an OS signal.
The original default concurrent GC is unchanged. All six reference/tenfold en/fa/de contexts,
twelve actual native publications, complete source/schema/typed-row and presentation guards pass.
Only new encrypted comparison copies receive the known additive M1/M2/M3 suffix. Original and
adopted measurement source byte guards pass against this batch's fresh baseline. Historical
D-558 reference byte equality is not reinstated. No seed, new key, preparation or financial
command occurs. The final separate read-only recovery audit was not requested; its false flag
is not reinterpreted as an additional audit.

The named APK-bound inspection observes zero record bytes and no original exit metadata while
PID4976 is alive. Thus this finite cohort has no captured matched engine fault. Do not loop the
same successful cohort or call the random SGen crash fixed. Timings under diagnostic instrumentation
are not a speedup comparison, phone target, full cold-start metric or general two-second gate.

## Safe handback and remaining work

The attempted original-code handback first invokes the existing empty-independent-Main retirement
guard. PID5129 refuses because its ordinary Main has accepted financial entries. This occurs
before derived-schema retirement. Preserve the failure, do not relax the guard, delete those
entries or downgrade an incompatible source. Keep the new compatible APK installed and stopped,
with original UID/CE/DE/permissions and the original334ca restoration APK retained. Do not claim
successful original-code restoration or fresh private-key/full-database reopening.

Ordinary emulator Main3035/50f5 resumes through its normal launcher; original APK, UID/storage,
permissions, actual owned window and five OS settings match the fresh baseline. It is not
upgraded during this cohort. No remote XML is created. The physical Main remains untouched by
this emulator cohort. Three standard owned-output cleans succeed and remove23170 registered
files/1284380862 logical bytes. Three hash-inventoried unused rejected-package outputs remove
another118976099 logical bytes; retain the used signed package, native controls and evidence.
These are logical file counts, not measured host free-space gains. Publish the finished batch
and schedule one exact-head CI check; its success remains a later gate.

QA-06 still needs actual fatal-code correlation and an evidence-supported correction, general
ANR/crash qualification and the two-second phone target. Independent ordinary-emulator adoption
of D-562's changed shipping package is a separate ready migration gate. Accessibility, remaining
platform/security/device requirements and owner-dependent iOS/auth/provider/Store/public-release
acceptance remain open. This bounded observation advances evidence, not overall completion.

Evidence pointer: `Zanance-D563-current.txt`; strict native/managed builds,15 reader cases, real
both-ABI records/reader proofs and cleanup, frozen source manifests, rejected and accepted APK
identity proofs, terminal storage refusal, KEEP_DATA lineage, original cohort and zero-record
inspection, guarded retirement refusal and actual safe handback receipts are retained locally.
