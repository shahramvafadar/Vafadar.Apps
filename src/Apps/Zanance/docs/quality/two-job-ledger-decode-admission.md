# D-779 - Two-job block decoding rejected at host allocation admission

## Distinct finite premise and fixed requirements

2 November2026. Inspect current original HomeLedgerReadBlocks before implementing a
new private decoder experiment. Current production decodes in physical order with one
fresh HomeLedgerBlockBuffer. The rejected D545 projection-overlap and D777 text-reuse
candidates are not reused, rescued or incorporated.

The new private decoder owns two independent original expansion buffers, with at most
two Task.Run jobs pending. Each slot's previous task must end before that buffer can
be reused. Append results in original block order regardless of completion order.
Settle all pending jobs before disposing either original plaintext-buffer owner.
The host uses already materialized immutable-intent encoded payloads; it never shares
an EF context/SQLite reader or changes production SQL, query count, revision/schema/
digest guards, scalar fallback, financial rows, codec/compression, runtime or GC.

Before build/run require at least10% median latency reduction and at most1% all-thread
allocation growth for both10000/100000 repeated and unique text shapes. Seven fixed
alternating pairs follow fixed warmups. Retained managed-output growth may be at most
8MiB, and observed codec concurrency at most2. A second process requires every first-
process gate. Any later native admission retains at least5% whole-Home gain and64MiB
terminal PSS/RSS growth ceilings; host results alone cannot promote the candidate.
No retune, looser threshold, shape restriction or rescue follows a failed gate.

## Actual source and process

One strict isolated Release build passes with zero warnings/errors. It directly links
actual unchanged original HomeLedgerBlockCodec.cs/HomeLedgerBlockBuffer.cs and current
Core. The reference uses DecodeWithBuffer and one read-owned reusable buffer, matching
the current decoder scope rather than comparing against the higher-allocation standalone
Decode path. The candidate calls that same original DecodeWithBuffer with separate
owners; it does not include the rejected first-text cache.

Actual process68648 runs the already installed official.NET10.0.14/X64 host. New
fictitious in-memory codec inputs reuse only a safe retained measurement/row harness,
not frozen files or a successful financial workload. Complete32-field row JSON/order,
source digest before/after and independent domain rows/tag lists across decodes match.
The payloads are read-only by this implementation; no independent before/after payload-
byte digest is claimed. Mixed kinds/optional fields/Unicode/embedded nulls/ordered
repeated tags are finite controls, not exhaustive malformed/cancellation coverage.

Use GC.GetTotalAllocatedBytes(precise:true) around each complete invocation so worker
allocation is included. A calling-thread-only counter would omit relevant worker work.
Retained managed heap is separately measured with the result alive and full collections
before/after, not inferred from allocation or RSS. It is not native/process peak or a
low-memory/leak result. The aggregate peak counter is retained only as an observation,
not mislabeled as exclusive candidate memory. Both workers actually execute concurrently
(maximum2) and return original ordered complete rows.

| Rows / shape | Original ms | Candidate ms | Original all-thread bytes | Candidate all-thread bytes | Original retained bytes | Candidate retained bytes | Gate |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
|10000 repeated-short-text|10.4912500|8.2285375|8555585|8824048|7554728|7554872|**Reject**|
|10000 unique-short-text|13.6075375|9.7113250|9515615|9782983|8511224|8511032|**Reject**|
|100000 repeated-short-text|114.9242000|90.0959500|81946840|82256824|76594328|76569688|Pass|
|100000 unique-short-text|120.1049000|97.5957000|91546952|91856672|86312912|86295776|Pass|

Both10000 shapes fail: all-thread allocations rise **3.137868%** and
**2.809782%**, above the fixed1% limit. All four latency gates, retained-heap
ceilings and observed concurrency bounds pass;100000 allocation gates also pass.
These improvements do not override the failed reference-shape allocation requirements.
Reject before second process, new native package/run, SQL integration, targeted failure
suite, production edit or permanent product test. No task-batching retune or100k-only
specialization is attempted.

## Explicit integration limits and preservation

This immutable-payload decoder host does not qualify original SQLite IO/fault ordering
or cancellation. Prefetching a later block could observe a later IO failure before an
earlier malformed block would trigger scalar fallback. That original semantic boundary
would need a concrete preserved-order design and focused proof before any integration;
it is not silently relaxed by a faster host decoder. Failure cleanup/clearing is read
from the unchanged owner implementation and source structure, not reported as newly
executed cancellation or error-path tests. The private nonreentrant counters are not a
new product concurrency policy. No production behavior changes.

Used source/output and receipts remain archived outside cleanup scopes. Standard project
Clean passes with0 warnings/errors. One supplementary deletion is rejected before process
creation with blocked by policy. Freeze both exact scopes without retry, subset,
alternate method, move, escalation or reuse:

| Frozen exact scope | Remaining files | Logical bytes |
| --- | ---: | ---: |
| D:\_Projects\Vafadar.Apps\artifacts\d779-two-job-decode |3|10932|
| E:\Zanance.ReviewBuilds\D779-two-job-build |28|406773|

No old frozen scope, protected owner file, physical/virtual device, Main database/key,
authentication, financial seed/write, shipping build or old successful suite is touched.
D752 waits for owner presence, D749/D751/D607 remain terminal, and original native-fault
origin, two-second/performance, security/accessibility and later platform/provider/
Store/release gates stay open. The overall independent goal remains active.
