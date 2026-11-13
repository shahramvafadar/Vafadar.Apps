# Current bridge header operation and prepared guard (D1147/D1148)

The [actual D1146 exit](retained-footer-native-lifecycle-and-exit-d1146.md) now
has an exact instruction/source operation. No original record, process or matrix
case is read or executed again. The25qualified/11remaining checkpoint is unchanged.

## Actual public code and source line

The original PID5017/UID10216 ordinal0 remains relativePC0x2c3b3d in the actual
Mono15module with BuildId `b1881505cd1c881ba16fec2997743cbc7033e60d`.
The already admitted public ELF/debug file identity is reused. An existing signed
static decoder processes only that public function's386bytes, without loading it.
Its final byte binding is deliberately limited to the exact49byte entry prefix:
18instructions, including the fault. No complete386byte binding is claimed.

The fault's four bytes `f6413708` decode as `test byte ptr [rcx + 55], 8`.
On both decoded normal entry paths, the last object-header load precedes this
read, RAX is copied to RCX, and RCX is masked with-8. RAX is not overwritten
between that copy and the fault. This is static register usage, not an actual
register value, object/type identification or recorded execution-path trace.
Another alignment classification of that masked base would add no information.

An initial full-function binding incorrectly treated assembler fixup placeholders
as literal encoding bytes; its admission fails. A subsequent complete canonical
re-encoding comparison also fails. Those failures and a local driver preflight
error remain explicit. The same successful decoder stdout is reused to bind only
the exact prefix, including its original branch displacement. No native input or
successful decoder execution is repeated to hide these failures.

A bounded reader subsequently parses the matching public debug file's complete
DWARF line section:277units/886,357program steps. Each of the10original relative
PCs has exactly one containing line interval. The fault's interval is precisely
0x2c3b3d through exclusive0x2c3b41, at
[sgen-tarjan-bridge.c:594](https://github.com/dotnet/dotnet/blob/8e0f8ed2312954727f8919d7d1c2f78b483b4ee7/src/runtime/src/mono/mono/metadata/sgen-tarjan-bridge.c#L594),
the inlined opaque-object vtable gc_bits test. Complete source bytes match the
already preserved [D1123 serviced source](tarjan-concurrent-source-admission-d1123.md).
No nearest-line extrapolation, managed image, private source or memory is used.

That test precedes find_data, the current object's liveness predicate and
create_data in the original function. The public proposal changes that later
predicate; a previous/other-object effect remains a hypothesis, not a proved
repair of this earlier read. Actual header contents, prior writer and cause remain
unknown. Runtime/GC, product code and original financial operations are unchanged.

## Separately prepared classification, not actual data

A new one-shot proposal asks whether the original saved RAX header word had one
of eight low-three-bit patterns. It would emit only a fixed header-low-000 through
header-low-111 label after exact original OS/package/UID/PID/TID/BuildId/PC/ordinal
admission. A111pattern is compatible with the original Tarjan marker, not proof
of a ScanData, valid object, corruption origin or repair. This is a new register/
site interpretation; previous RSI/R13 permissions are not extended by implication.
The owner decision is pending; no original word is read and nothing is deployed.

D1148 independently prepares only the private fixed-site reader and a host
synthetic harness. The public [AOSP declaration](https://android.googlesource.com/platform/system/core/+/refs/heads/main/debuggerd/proto/tombstone.proto)
confirms Thread.registers field3 and Register name1/u64value2. An initial uncompiled
field1assumption is corrected before any run. Proto3 can omit a zero numeric value;
a named RAX entry remains mandatory, while its omitted u64 correctly means zero.
This does not turn a missing RAX entry into an ordinary result.

One strict host build and44new synthetic cases pass on .NET10.0.15/X64/PID20116.
They cover all8labels under two different upper-word inputs, explicit/omitted zero,
wrong original identities/reason/code, missing/duplicate/oversized/misordered
register fields, original ordinal retention, frame/register/8MiB limits and
truncation. Unselected register values are discarded as raw bytes; an unrelated
malformed thread remains unparsed. Wrong OS metadata refuses before reading the
stream. Only the fixed label leaves the API; the transient word buffer is cleared.
No claim is made about eliminating every possible runtime/JIT copy in host memory.

The final revision corrects only an inherited XML summary that no longer described
the transient buffer. Reversing that one comment reconstructs the complete executed
source exactly. Its necessary strict rebuild and two actual portable-PDB bindings
pass; the44successful cases are not replayed, nor claimed as executions of that
final comment-only binary. Both versioned used binaries/PDBs and source remain.
These are new private guard checks, not44product tests, Android deployment or actual
classification. Two standard Cleans retire30file instances/605,822logical bytes.
No supplementary/frozen deletion,
device action, authentication, financial input, old reader suite or crash injection
occurs. Artifacts remain under `artifacts/d1147-current-bridge-fault-operation` and
`artifacts/d1148-prepared-original-header-guard`.

## One-time CI receipts

After the owner resumes the goal, the previously deferred sole D1144GET returns
[CI success43405508763](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43405508763)
and [CodeQL success43405508800](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43405508800).
The sole delayed D1146GET likewise returns
[CI success43424980214](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43424980214)
and [CodeQL success43424980253](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43424980253).
Each exclusive consumed marker precedes its request. No polling, successful job
log, rerun, repeat test or broader acceptance follows. Native-fault remedy and all
remaining full-delivery gates stay open.
