# Original saved allocation-to-collection path - D1177

[D1178](protected-context-options-retention-d1178.md) separately corrects an
observed protected-context options-cache retention defect. It neither repairs
nor establishes the cause of this original native collector fault.

[D1176](original-generation-worker-exclusion-d1176.md) distinguishes a
generation-scoped join from universal worker quiescence. This continuation binds
that distinction to the existing admitted original PID 5017 stack. It uses only
the previously exported module-relative frames, not another SDK inspection,
original report stream, register interpretation or crash execution.

## Same historical event and exact adjacent calls

The retained D1146 packet identifies PID 5017, UID 10216, crashing thread 5017,
OS timestamp 1794570836896, reason 5/status 11 and ten consecutively indexed module
frames. One non-module frame was discarded by the original bounded reader.
All ten exported frames name the exact original module BuildId
`b1881505cd1c881ba16fec2997743cbc7033e60d`. These are saved unwind observations,
not fresh process liveness or a new original-value disclosure.

Six adjacent caller frames occupy the final byte of an exact five-byte direct
call to the preceding frame's function. Each target is checked against its
actual complete symbol range and reconstructed executable bytes:

| Original caller ordinal | Caller | Saved relative PC | Direct-call target |
| --- | --- | --- | --- |
| 3 | finish_gray_stack |0x2dca36|sgen_bridge_processing_stw_step|
| 4 | collect_nursery |0x2dbaa9|finish_gray_stack|
| 5 | sgen_perform_collection |0x2d7ec6|collect_nursery|
| 6 | sgen_ensure_free_space |0x2d7cd2|sgen_perform_collection|
| 7 | sgen_alloc_obj_nolock |0x2d412f|sgen_ensure_free_space|
| 8 | sgen_alloc_obj |0x2d4597|sgen_alloc_obj_nolock|

The first three complete caller bodies already exist in retained decoded
evidence. No decoder is rerun for them. Three newly selected allocation functions
add 1,570 original executable bytes across 391 decoded rows, with complete byte
reconstruction. The two corresponding source files, sgen-gc.c and sgen-alloc.c,
match the original D1155 archive manifest. A nearby symbol or a generic source
call graph alone is not used to identify these saved call sites.

## What this resolves and leaves unknown

The actual saved collect_nursery frame maps to its finish_gray_stack call at
0x2dbaa5, after `xor edi,edi` at 0x2dbaa0. The original nursery enumeration is 0.
The admitted unwound path therefore identifies generation 0 for this bridge
invocation, rather than leaving the collection generation wholly unspecified.

The actual saved sgen_perform_collection frame maps specifically to the call at
0x2d7ec2. Its original dispatcher tests the requested generation and computed
finish-concurrent decision before branching to the nursery/non-finishing arm at
0x2d7e86. It is not the concurrent-finishing arm that stops old-generation
workers before major_finish_collection. The retained callers additionally bind
this invocation to the allocation/free-space path; this is not evidence of an
explicit managed GC.Collect caller at this point.

That nursery arm supports both absence of a concurrent major collection and an
already-running concurrent major collection that is not finishing. The saved
call PC does not distinguish those two cases. Nor does a nursery generation
identify the faulting object's generation, type, header tag, lifetime or earlier
writer. The crashing thread is the original main thread; participation or state
of any other collector worker remains unobserved. No generic worker race,
forwarding fault, runtime repair or missing-pointer guard follows from this result.

## Preservation and sole prior CI receipts

Input packet/decoder digests, original source hashes and six exact call-edge
checks remain under artifacts/d1177-original-allocation-caller-path. There is no
build, benchmark, product test, device command, runtime/GC/configuration change,
financial operation or cleanup. Phone/Main, retained C7/Q02, consumed one-shot
diagnostics, accepted cohorts, rejected performance candidates and all frozen
targets remain untouched. The original cause and remedy, whole performance and
broader security/accessibility/platform/provider/release gates stay open.

The sole D1175 feature response reports
[CI cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43628424530)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43628424413).
Its wording-only follow-up's sole response reports
[CI cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43628518014)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43628517966).
Exclusive markers preceded each request; raw responses and receipts are retained.
No actor, failing product test, later result or rerun is inferred. The active full
delivery goal remains open.
