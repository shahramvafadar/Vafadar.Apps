# Original generation-scoped worker exclusion - D1176

[D1169](concurrent-mark-forwarding-consumer-d1169.md) binds an actual conditional
forwarding consumer, without identifying the original object's header or collector
phase. This continuation resolves a different prerequisite: whether managed
stop-the-world or a nursery-worker join establishes that every old-generation
collector worker is quiescent. It does not repeat the consumed D1151 inspection
or the denied D1168 restoration report, backlink or message.

## Original source and complete native-code admission

Five executable-source inputs match the original D1155 archive manifest exactly:
sgen-gc.c, sgen-workers.c, sgen-thread-pool.c, metadata/sgen-stw.c and sgen-gc.h.
Their mapped component is a3788b71495457354b64ca8040b6ded5ff6ca4f9. The modified
private Tarjan file is not an input to this question.

Original public module/debug hashes and BuildId
`b1881505cd1c881ba16fec2997743cbc7033e60d` bind nine complete native functions:
collect_nursery, sgen_workers_join, sgen_workers_stop_all_workers,
sgen_thread_pool_idle_wait, sgen_perform_collection, sgen_client_stop_world,
unified_suspend_stop_world, sgen_thread_pool_is_thread_pool_thread and
sgen_client_thread_register_worker. Their 7,417 original executable bytes and 1,712
decoded instructions reconstruct completely. Fifteen explicit call/argument/branch
boundaries pass, not fifteen product tests. No module is loaded or executed.

Four initially named source functions have no separate exact symbol in this
module. Their absence is retained rather than treated as missing executable code;
the original collection dispatcher and unified suspension caller provide the
relevant compiled bodies. No address is inferred from a neighboring symbol.

## What the original exclusion actually covers

The original header enumerates nursery as 0 and old generation as 1. Before the
parallel nursery path reaches finish_gray_stack, its compiled call sequence has
`xor edi,edi` at 0x2dba75 and sgen_workers_join at 0x2dba77. The subsequent
finish_gray_stack call at 0x2dbaa5 also receives generation 0. This establishes the
named nursery-context join, not an all-generation join.

The join's original native body multiplies the selected generation by 168 at
0x313f57, then reads that context's thread-pool identifier for wait-for-all-jobs
at 0x313f7c and idle-wait at 0x313f90. Original source additionally requires no
finish callback and empty selected-context gray queues before clearing its
started flag. The thread-pool idle wait invokes its supplied context callback;
it is not a global worker suspension API.

Managed world suspension also has a separate boundary. Original
sgen_client_stop_world calls unified_suspend_stop_world at 0x2c4849. In the
compiled suspension enumeration, 0x2c4b04 tests thread-pool membership,
0x2c4b09 tests its result and 0x2c4b0b jumps to the next-thread loop at 0x2c4ad0
when membership is nonzero. The actual begin-suspend call at 0x2c4b21 is bypassed
on that branch. The source explicitly excludes these collector workers.

Concurrent finishing does provide a distinct old-worker stop: the original
dispatcher passes 1 at 0x2d7dc1 to sgen_workers_stop_all_workers at 0x2d7dc6 before
major_finish_collection. However, its separate nursery-with-concurrent-major
branch updates the card-table unions and calls collect_nursery at 0x2d7ec2;
that source branch has no corresponding old-context stop. Worker scheduling is
generation/context scoped: started, active-worker bounds and working/enqueued
state control eligibility, while higher-priority work may interrupt idle work.
Priority selection is not evidence that the old context was joined globally.

Consequently, managed-world suspension plus a successful nursery join is
insufficient evidence for universal old-worker quiescence. Conversely, this
static permitted path does not establish that an old worker actually ran during
the historical bridge operation. Actual collector generation, concurrent-phase
state, worker participation, object/header values and earlier writer remain
unobserved. No race, corruption cause, required runtime patch or crash repair is
accepted from this result.

## Preservation and remaining work

Local original-source admission, exact symbol metadata, complete decoder outputs
and worker-exclusion-boundary-proof.json remain under
artifacts/d1176-collector-worker-exclusion. No original SDK record, stream,
register, address value, process memory, new crash or signal is read or produced.
There is no build, benchmark, successful-test replay, runtime/GC/configuration
change, device command, financial action or cleanup. Phone/Main, retained C7/Q02,
accepted cohorts and every frozen scope remain untouched.

The next causal claim still needs actual phase/object evidence within its existing
authority; this source distinction does not authorize a new diagnostic or generic
stress workload. Rejected startup candidates remain rejected. Original fault
remedy, whole performance/security/accessibility, platform/provider/release gates
and the full delivery goal remain open.
