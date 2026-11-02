# D-775 - Frame publication and Volatile intrinsic source admission

2 November2026. Continue independent original native-fault investigation while D752
waits for owner authentication. Examine two distinct official correction candidates
without changing the runtime, enabling interpreter/GC options, injecting a fault or
replaying an already accepted workload. Finite searches return leads, not proof that
all upstream GC or interpreter defects have been reviewed.

## Frame publication candidate belongs to another runtime

Official [PR129795](https://github.com/dotnet/runtime/pull/129795), merged as
[99eb16b5420fe88e333e2d95600b8617a86d54ff](https://github.com/dotnet/runtime/commit/99eb16b5420fe88e333e2d95600b8617a86d54ff)
on26June2026, changes exactly src/coreclr/vm/interpexec.cpp. It initializes a new
interpreter child's instruction pointer before publishing its frame, with compiler
ordering for same-thread asynchronous interruption. Its reported failure is a
CoreCLR Windows/ARM64 stack-overflow unwinder assertion.

The actual original fault is Android/x64 in SGen's reference-object word load from
libmonosgen-2.0, not that CoreCLR frame implementation. The generic title and shared
interpreter terminology do not admit applying this patch or running its stack-overflow
reproducer here. No claim about all similar Mono risks follows from a different file.

Exact original Mono interp.c at VMR0427b9abbead3fa64c6655eb4290c537397131fa already
initializes its context and executes a compiler barrier before set_context publication;
it also has stack-pointer ordering at interpreter entries. These are source facts for
Mono's own structures, not proof that every original frame or GC reference was valid,
that the CoreCLR/Mono implementations are equivalent, or that our cause is repaired.

## Original Mono source already includes the Volatile correction

Official [PR130303](https://github.com/dotnet/runtime/pull/130303), release/10.0 backport
of124538, merged as
[319ee4785c9e5cd21df2289854dc43788a7d9242](https://github.com/dotnet/runtime/commit/319ee4785c9e5cd21df2289854dc43788a7d9242)
on6August2026. It adds recognition of the parameterless Volatile.ReadBarrier and
WriteBarrier APIs in Mono's interpreter transform, emitting the existing full-memory
barrier opcode. The reported issue is failure when those APIs are invoked under the
interpreter, rather than evidence of our particular SGen scan's bad reference.

Revalidate exact retained original-source UTF-8 bytes, without newline rewriting:

| Original source | SHA256 |
| --- | --- |
| interp.c |1f47513497020c60a404bfce7881d2847a98fad40c5b98660c55f1cd2a832578|
| transform.c |17e98eaaf5f9dc5ad9cb6c28ac2a26b20714b09d9ba7c8c872c2ac74dbfc6632|

Extract only added lines from the correction's exact transform.c patch and require
that complete added source block to occur in the original transform.c. It matches.
Also verify the original MINT_MONO_MEMORY_BARRIER dispatch calls mono_memory_barrier.
The claimed source omission therefore does not exist in this original revision.
Do not repeat upstream tests as a purported application reproducer or upgrade the
runtime to obtain a correction already present in its bound original source.

This is source admission, not disassembly of a dynamically transformed original call.
D772's actual original package mode is Normal; actual interpreter/finally/API execution
in the old fault is still unproven. Presence of interpreter code is not execution,
and source inclusion does not independently establish every packaged machine-code
branch. Preserve those limits instead of asserting interpretation is impossible.

## Outcome and preservation

Neither correction is admitted as the original SGen fault remedy: one changes another
runtime, and the other's added source block already exists. Original bad-reference
origin, categorical fault observation and repair remain open. The next useful input
must address an actual original writer/reference or a genuinely new qualifying event;
changing GC/execution mode or replaying arbitrary stress would not establish causality.

Only immutable public correction metadata and bounded local source receipts are
created. Original product/runtime/GC/model/tests remain unchanged; no build, device,
authentication, financial write, successful suite or historical workload repeats.
No cleanup scope is created or retried, and all frozen paths and protected untracked
owner files remain untouched. D752 awaits the owner, D749/D751/D607 remain terminal,
and the overall Android/crash/performance goal stays active.
