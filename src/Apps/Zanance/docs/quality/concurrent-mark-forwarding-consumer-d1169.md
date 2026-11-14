# Original concurrent mark forwarding consumers - D1169

[D1123](tarjan-concurrent-source-admission-d1123.md) establishes the generation-aware
predicate premise. This independent follow-up distinguishes actual concurrent-start
reference handling from evacuation/finishing handling, then follows slow object-size
calculation. It is not a replacement for the denied D1168 phase report or a new
original-record/register classification, crash trial, runtime patch or product test.

## Exact finite inputs

Seven original public source inputs match the D1155 mapped archive manifest at runtime
commit a3788b71495457354b64ca8040b6ded5ff6ca4f9. They contain the marksweep include
variants/canonical wrappers, shared copy/scan code, tag macros, size helper and
public descriptor constants. No patched source is substituted for the original.

The actual original Mono 15 module SHA remains
b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034,
BuildId b1881505cd1c881ba16fec2997743cbc7033e60d. Previously bound public symbols
select five unique canonical functions. Existing signed decoding is applied only
to their new executable ranges, 3,379 bytes / 988 instructions total, with complete
original-byte reconstruction. The three requested inline helper names have no
separate function symbols; the actual canonical bodies are used, not guessed sizes.
There is no new decoder refusal, native build or reread of old successful decodes.

| Complete function | Module-relative start | Bytes | Instructions |
| --- | --- | ---: | ---: |
| major_copy_or_mark_object_canonical |0x2eb6e0|853|248|
| major_copy_or_mark_object_concurrent_canonical |0x2f0570|352|109|
| major_copy_or_mark_object_concurrent_par_canonical |0x2f65c0|418|123|
| major_copy_or_mark_object_concurrent_finish_canonical |0x2f4790|853|248|
| major_copy_or_mark_object_concurrent_par_finish_canonical |0x2fa7a0|903|260|

No module data-section value, active callback/mode, process memory, object descriptor,
register value, SDK record/trace or application assembly is read. All code locations
are static module-relative locations, not an actual runtime pointer or call history.

## Distinct reference-write boundaries

The source instantiates concurrent-start helpers with COPY_OR_MARK_CONCURRENT or
COPY_OR_MARK_CONCURRENT_WITH_EVACUATION. These are distinct from the ordinary
COPY_OR_MARK_WITH_EVACUATION guard containing the old-generation forwarded-reference
update. Finishing uses the ordinary evacuation helper again; macro names do not
establish that the same branch executes in both phases.

In actual concurrent serial/parallel entries, the original reference-slot argument
is read at0x2f0575/0x2f65c5 and discarded at0x2f05a1/0x2f65f0. Before that discard,
its only uses are the exact slot read and overwrite: no call, store or argument alias
carries it to later code. The source likewise has no direct reference update in this
concurrent-start branch. Internal mark/gray-queue writes still occur; a later store
using the same register name is not a write through the discarded original argument.
This is not a claim of no memory writes or no possible corrupted-input aliasing.

Ordinary/finishing evacuation bodies instead contain forwarded-mask tests and direct
reference-slot writes at0x2eb76f, 0x2f481f and 0x2fa82f. Their existence must not be
reported as proof that an active concurrent-start worker rewrote the original field.
The actual original collector phase and active function remain unknown.

## Slow sizing still consumes the forwarded flag

The concurrent-start source explicitly untags the header before reading its descriptor,
including a comment about bridge tagging. That alone does not describe every later
use: sgen_safe_object_is_small calls the generic size helper for descriptor types
above DESC_TYPE_MAX_SMALL_OBJ (3). That helper also tests the forwarded flag.

The compiled serial body loads the header at0x2f059e, masks the outer vtable at
0x2f05a4and reads the descriptor at0x2f05a8. Its slow-size branch tests forwarded
mask1(bit0) at0x2f05b2, conditionally reads the first word of the masked base at
0x2f05bb, masks it again at0x2f05be and calls sgen_client_par_object_get_size at
0x2f05c5. The parallel equivalent is 0x2f65ed/0x2f65f3/0x2f65f7, followed by
0x2f6608/0x2f6611/0x2f6614and the same size target at0x2f661b.

For symbolic aligned nonzero vtable base V and header H = V | tag, let U clear the low three
bits and M represent a symbolic word read. The outer descriptor uses V. Under the
slow-size condition, tag 0 supplies V to the size callback; tag 7 takes the forwarded
branch and supplies U(M[V]), with the original object as the second argument.
No real M value is read and equality/inequality of these symbolic parameters is not
asserted for a real object. No additional source-format examples are counted as
product tests, and no actual tag 7 or descriptor classification is inferred.

This binds a distinct conditional interpretation path; it does not prove a reference
write, an actual wrong size, heap corruption or the original fault. The original
faulting object, its tag/type, participating worker/phase and earlier writer remain
unobserved. In particular, the unchanged consumed classification result is not
replaced by this symbolic case, and no generic forwarded-flag or sizing patch follows.

## Retention and delivery limits

Local authority is artifacts/d1169-concurrent-forwarding-consumer:
source-consumer-admission.json, descriptor-constant-admission.json,
consumer-symbol-metadata.json, consumer-complete-public-code.json and
consumer-boundary-proof.json. All five original decoder outputs and exact byte,
branch/target and argument-use boundaries remain available. The inspected runtime module is never loaded.

Phone/Main, retained C7/Q02 data and keys, accepted matrix/host/financial work,
original single-use diagnostics and every frozen scope are untouched. No device,
benchmark, product test, runtime/GC/configuration, account, authentication, financial
operation or cleanup occurs. The denied D1168 report/backlink/message targets are
neither read, rewritten nor retried by this separate consumer investigation.

The next causal claim requires evidence for the actual source object and collector
phase; broad forwarding, callback or tag assumptions are insufficient. Original fault
remedy, whole performance/security/platform/external requirements and the active full
delivery goal remain open.
