# Compile-only journal delegate feasibility - D1165 first gate

[D1164](original-journal-query-compilation-d1164.md) observes a645.0645ms read-query
compilation event span and confirms that merely constructing a delegate does not
remove first-use compilation. This private first gate asks whether the supported
delegate can actually be initialized without opening a database or enumerating
its result. It does not change product code or establish a speed improvement.

## Registered contract and actual original types

The original ZananceDbContext, ImportLinkBatch and unmodified Data/Core projects
are referenced directly. The expression remains the complete original entity query:
db.ImportLinks.AsNoTracking, with all four mapped properties. No generated interceptor,
internal EF API, narrowed StateJson-only projection, fake model, connection/key cache,
runtime/GC setting or new package version is introduced. The host's existing SQLite
bundle is an explicit private dependency; this is not native Android/SQLCipher or
an encrypted-publication qualification.

A private SqliteConnection subclass rejects synchronous/asynchronous Open and the
guarded DbCommand creation path before invoking their base implementations. Its
source is only :memory:, with pooling disabled. The real context constructor's
closed-connection function registration remains; no function is invoked, schema
created, source/key acquired, query enumerated or financial operation performed.

The first original-type metadata context receives the delegate, then is disposed.
A second newly constructed context must have actual ReferenceEquals model identity,
reuse that delegate without enumeration, and be disposed. Original EF different-model
checks are not suppressed or replaced. This is model identity within this host process,
not a claim that a future real protected factory or Android context has been admitted.

Fixed DiagnosticListener compiler-start/planned names are the only observed labels.
Event Values, expressions, context, SQL and configuration are never read or retained.
The first invocation must emit211/212; the second must not compile again. Records and
subscriptions are bounded at8and16. No forced collection, timing loop, warm benchmark,
real database setup, old suite or previous successful operation is used.

## One actual host process

The initial strict build refuses CS0108 because a private observation property hides
Component.Disposed. Renaming that property corrects only the helper; its first
execution is still unissued. The next build reuses the successful restore and exits0
with zero warnings/errors. An exclusive consumed marker binds the executable/source
hashes before exactly one .NET10.0.15/X64 process. Its actual terminal packet is:

- Passedtrue, no error type, terminal stage;
- actual same-model referencetrue;
- first and second metadata connections both disposed;
- guarded Open attempts0, command attempts0 and query enumerations0;
- exactly first-invocation labels211/212, with no second-invocation compilation;
- diagnostic faults0.

Eight actual portable-PDB documents bind the private host and original context,
entity, generated model/attribute/mapping, base context and timestamp converter.
This is selected compiler-source evidence, not whole-PE identity, materialization,
factory/lease/key safety, all journal refusal behavior or a completed optimization.
No original SDK record, register, memory, C7/phone/Main or frozen scope is accessed.

Standard Clean exits0 and retires172files/87679332logical bytes. Used host DLL,
PDB/source/graph inputs, initial build refusal, consumed marker and actual packet
remain under artifacts/d1165-compile-only-journal-warmup. No supplementary deletion,
new native build, financial action or successful test replay occurs. D1164 remains
the latest stopped C7 authority; all36matrix contexts remain carried and unrequested.

## Remaining correctness and complete-startup gates

The first metadata-only premise is now feasible, rather than assumed. Do not rerun
this consumed first gate or promote it to real-source acceptance. Before any timing,
the actual candidate must demonstrate reads after compiler-context disposal through
the original captured protected factory/file/key/connection/command interception,
all entity fields/order/converters and JSON/version/digest failures, cancellation,
cross-file/retired-owner/model boundaries and full protected-source preservation.
No model mismatch or source guard may be normalized away to obtain a pass.

Only a later registered complete-startup comparison may test overlapping compile-only
work after window creation with the unchanged protected worker. It must include the
warmup's time/allocation/working set and every original source, ownership, latency and
memory gate. Warmed query-only timing cannot qualify cold startup, and private metadata
context construction is not authority to bypass real context creation or early-open
source before protection completes. Original fault cause/supported-runtime remedy,
whole performance/security/platform/external requirements and full delivery remain open.
