# Original Tarjan reference input and managed control limits - D1170

[The original operation evidence](current-bridge-header-operation-d1147.md) binds
the faulting opaque-object header read. This continuation follows the immediate
caller input using only the previously admitted bounded frame metadata and retained
exact executable decoding. It does not reopen the consumed header classification.

## Original caller, not a new record

The historical original module remains SHA
b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034,
BuildId b1881505cd1c881ba16fec2997743cbc7033e60d. Only original frame0 relative
PC0x2c3b3d and frame1 relative PC0x2c2cf6 are selected from the retained public
module/source binding receipt. No SDK record/trace stream, register or process
memory is reopened, and no timestamp guard or alternative record is selected; only the retained relative PCs
are used for this source/code interpretation.

The existing exact processing_stw_step decode contains the frame1 PC in the last
byte of the unique call at0x2c2cf2, bytes e8190e0000, ending exclusively0x2c2cf7.
Its original signed displacement targets push_object at0x2c3b10. This uses the
containing instruction interval, not a guessed caller-PC adjustment or nearest
symbol. Reconstructing the retained rows still matches that function's complete
4421byte code hash; no decoder executes again.

## Interpreted element-bitmap source path

Four original mapped sources match the D1155archive manifest: unmodified Tarjan,
generic scan, descriptor macros and the Mono array-data helper. The Tarjan
push_all HANDLE_PTR reads a non-null child reference and passes it to push_object.
The generic scan selects OBJ_VECTOR_FOREACH_PTR; the bound code matches its
bitmap-element loop, rather than the all-reference or run-length element loops.

| Bound location | Exact static role |
| --- | --- |
|0x2c2c79/0x2c2c7e|Load interpreted parent and form payload base at offset32.|
|0x2c2c85/0x2c2c88|Read interpreted array length and multiply by encoded element stride.|
|0x2c2cb0|Advance to the next element by that stride.|
|0x2c2ca0|Derive element bitmap by descriptor shift16.|
|0x2c2cd3/0x2c2cd6|Shift the current bitmap and advance one8byte reference slot.|
|0x2c2ce3|Test the current bitmap bit.|
|0x2c2ce9/0x2c2ced|Load that selected reference slot and test it for null.|
|0x2c2cf2|Pass the unchanged non-null value to push_object.|

No call or argument write intervenes between that slot load, null test and call.
The immediate argument therefore comes from the interpreted element-bitmap payload
path. It is not established as a direct read of the parent's header/lock word.
This identifies scanner interpretation, not a valid array class or an actual element
stride, bitmap, index, source-slot address or child value. A corrupt descriptor,
parent or input value is not ruled out, and no original managed type is named.

The existing push_object code may then resolve an ordinary forwarded input before
its gc_bits access; its all-three-low-bits marker branch is separate. No new leaf
decode is run. Binding the caller argument does not prove it is still the same
resolved object at the fault, or replace the unknown actual header value with a
symbolic tagged case. Earlier writer/reference origin and fault remedy remain open.

## Reject an invalid managed-only background experiment before building it

The retained actual Android graph selects runtime/AOT inputs10.0.15. Its runtime
nuspec points to VMR8e0f8ed2312954727f8919d7d1c2f78b483b4ee7. One bounded download
of that immutable VMR's GC.Mono.cs is byte-exact with mapped runtime component
source, SHA be62fe40cb2086c70655e53e33f2a5d334566b41e0493cb4f8fd12f3bb2ea0f3.
Native collection wrappers also match the original archive manifest.

In this declared Mono source implementation, the managed Collect overload validates
mode but sends only generation to InternalCollect: blocking and compacting are not forwarded.
The native chain reaches sgen_gc_collect, whose requested collection passes
forced_serial TRUE to sgen_perform_collection. Calling a major collection with
blocking:false is therefore not an admitted request to start a background major
collection. This does not prove absence of an already-running concurrent phase.
GetGCMemoryInfo(GCKind) separately throws PlatformNotSupportedException; the
parameterless method alone does not establish a concurrent phase interval. This
is source-level admission: managed PE/IL is not inspected and no phase is observed.

No generic stress fixture, profiler, new AVD, allocation/GC-policy tuning, private
runtime variant or managed-only background experiment is prepared or executed.
A future legitimate original-path reproduction would need actual phase admission
and applicable authority, not CoreCLR assumptions or a clean synthetic stress result.

## Evidence and delivery limits

Authority remains under artifacts/d1170-original-reference-input:
reference-input-proof.json, the bounded public GC source/fetch receipt and
managed-reproducer-control-preflight.json. No application build/product test,
financial action, phone/Main/C7/Q02 command, cleanup or frozen target is touched.
No denied phase report/backlink/message is retried or replaced by this distinct
original-input/control review. All accepted cohorts and consumed one-shot questions
remain unchanged. This is new causal/input evidence, not a native fault repair,
whole performance/security/platform acceptance or completion of the active full goal.
