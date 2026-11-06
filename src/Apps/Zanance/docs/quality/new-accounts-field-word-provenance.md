# New accounts fault: static reference-element provenance (D-908)

## Question and result

The admitted original D831 fault at relative PC `0x2f3f92` reads the object header
through a value loaded from an array reference-element slot. It is not the direct
gray-stack entry load. This closes a static operand-provenance question, not the
actual pointer classification, earlier writer, object validity, fault cause or repair.
No runtime, GC or product code changes follow from this evidence alone.

The historical event remains PID17704/TID17731/UID10234, reason5/status0/importance100.
It is not relabelled status11. No device, saved register, map, heap, private DB/key,
new crash record or tombstone is read. D838 new-PC authority remains pending;
D841 remains undeployed and D809's old-PC result is not reinterpreted.

## Exact public inputs

The original x64 runtime10.0.14 Mono module has SHA256
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`,
BuildId `b9db63cc66e53a97dc8483280f7aab8a31672169`. The matching public debug file has
SHA256 `8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0`.
Both hashes are freshly bound before this static inspection. Package repository
metadata identifies immutable VMR commit `0427b9abbead3fa64c6655eb4290c537397131fa`.

The already admitted positive STT_FUNC bounds identify `drain_gray_stack_concurrent`
at `[0x2f3420,0x2f4786)`:4966bytes. All1280 decoded instructions are contiguous and
match the exact executable PT_LOAD bytes. A stripped nearest-export label is not
used as the function identity. This does not use the frozen D700 tool directory.

The exact-PC debug lookup names `major_copy_or_mark_object_concurrent_no_evacuation`,
physical source line137 of `sgen-marksweep-drain-gray-stack.h`, under the vector
scan at physical line59 of `sgen-scan-object.h`. Raw immutable source bytes, rather
than a rendered viewer's blank-line numbering, confirm that line137 reads
`mword vtable_word = *(mword*)obj;`. No source-line mismatch is established.

## Machine path and source correspondence

The descriptor low-three-bit dispatch subtracts one and bounds its index to0..6.
Its sole indirect jump at `0x2f34e3` uses seven signed relative table entries at
`0x78124`. Those28 public bytes hash to
`426d0a3d99e72e397e2ad1a7efd78914dde6bb2c09516b838de284e192c68d30`.
Every resolved target is a verified instruction boundary. Descriptor type5 reaches
`0x2f397b`; subtype bits `0x4000` reach the reference-element loop. This agrees with
`DESC_TYPE_VECTOR` and `DESC_TYPE_V_REFS`, including arrays of structs containing
only references; it does not identify a particular managed class or array instance.

| Relative PC | Original operation | Static meaning |
| --- | --- | --- |
| `0x2f39c5` | Load saved scanned-object base from stack | Reuse the current object being scanned |
| `0x2f39c9` | `lea 0x20(%rdx),%r12` | Begin array data |
| `0x2f39cf`..`0x2f39da` | Element size times array length plus data base | Establish loop end in RBX |
| `0x2f3f40` | Add8 to R12 | Advance reference slot |
| `0x2f3f51` | `movq (%r12),%r13` | Load the reference value from the slot |
| `0x2f3f72`..`0x2f3f87` | Null/nursery comparisons | Conditional path to object handling |
| `0x2f3f8d` | Prefetch through R13 | Same reference operand |
| `0x2f3f92` | `movq (%r13),%rax` | Admitted faulting eight-byte object-header read |
| `0x2f3f99` | Mask loaded header copy in RDI with-8 | Later vtable untagging, not R13 alignment |

A complete intra-function normal-return CFG resolves every direct branch and the
one indirect jump table. Removing each of `0x2f39c9`, `0x2f3f51`, `0x2f3f72` or
`0x2f3f84` makes the admitted fault unreachable from function entry. The slot-load
entry has only initial-jump and loop-comparison predecessors; the fault has only
the prefetch predecessor. There is no R13 write between slot load and fault.
This is a static CFG statement, not evidence about exceptional control transfer,
actual values, a valid descriptor, prior corruption or nursery membership.

The pinned [vector macro](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-descriptor.h#L273)
walks data slots and calls HANDLE_PTR for each. The
[scan contract](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-scan-object.h#L19)
distinguishes a pointer to a reference from the reference itself. The
[concurrent handler](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-marksweep-drain-gray-stack.h#L257)
loads that reference before calling the header reader. These independent source
and exact-binary observations agree; they do not prove source identity for every
runtime file or actual managed execution history.

## Preserved frontier

Public source receipts, original hashes, exact-PC lookup, verified instructions,
bounded jump-table proof and CFG result remain locally preserved.
The initial local-link checker omitted bare filenames because its prefix required
one character; that checker failure is retained and its pattern corrected. All five
new local links then resolve; no product verification is replayed for that correction.
No app build,
successful test, private candidate benchmark, financial operation, authentication,
phone/emulator action, cleanup or frozen scope is repeated. Main17183/16b7b889 and
Q0215609 are historical D907/D906 observations, not fresh liveness claims here.
Original fault remedy, pending classification, whole performance/security,
accessibility/platform/provider/release gates and the overall goal remain open.
