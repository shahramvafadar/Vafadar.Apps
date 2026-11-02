# Original fault precedes the heap-membership split (D-770)

2 November 2026. Continue independent Android fault investigation while the owner is
unavailable for D752. This is one new static control-flow question about the retained
original D678 fault, not a new workload, diagnostic assertion, runtime change or repair.

## Revalidated original input

Use the original public Mono module SHA256
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`
and matching official debug SHA256
`8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0`.
Retained D712 metadata binds BuildID b9db63cc66e53a97dc8483280f7aab8a31672169,
function major_scan_ptr_field_with_evacuation at0x2edd40/1001bytes and original
PID10110/UID10216/fault0x2edd7d. Revalidate both public-file hashes, the actual
executable PT_LOAD containment/file mapping and all retained decoded instruction bytes.
All283 instructions cover exactly1001 original bytes, with function SHA256
`725296ba878e02c317e26fd4b035ca5d5452ca1f452a08cb3bec58aa6afc85fa`.
No removed COFF wrapper or frozen decoder directory is recreated/read/reused.

## Exact normal-entry control flow

| Original module-relative code location | Static operation |
|---|---|
|0x2edd4b|Load the field value into RBX from the slot through RSI.|
|0x2edd4e /0x2edd51|Test RBX for zero; the zero branch goes to function-relative0x282, outside this prefix.|
|0x2edd70|Mask a copy in RAX; this does not modify the incoming RBX value.|
|0x2edd7d|Original fault: eight-byte word load through RBX into RDI.|
|0x2edd80 /0x2edd83|Only now compare mask/base values and branch to function-relative0x89.|
|0x2edd85 /0x2edd8f|Test header pin/forwarding flag bits after the word load.|
|0x2edd99 /0x2eddcb|Mask low header bits in RDI on the two later paths.|

Decode both relative conditional branch displacements independently from their bytes.
Inspect every direct jump in the complete function: none targets the prefix before
function-relative0x45. There is no call or unconditional jump before that comparison,
and RBX has no intervening write after its field-value load. These are static register
names/instructions, not actual process register values or live addresses. Wrapper-relative
branch coordinates remain explicitly function-relative, never treated as module addresses.

The matched original source
[copy/mark function](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-marksweep-drain-gray-stack.h)
places header-word work in nursery and non-nursery paths. Its exact retained source
SHA25650ab37ab96e90bffa48373ab18865a887b228219b29ab370296e4787cc3c26f6 is revalidated.
The optimized native code shares the first word load before the membership split;
an inner DWARF line0 still does not become a unique original C branch/statement.

## What this changes, and what remains unknown

The original faulting PC does not prove nursery membership: the load faults before
the membership comparison/branch executes. Do not infer a nursery-only object from
that PC or transfer D760's separate pin-check assertion to this original failure.
The later header-flag tests/masks have not executed at that load; changing only those
later operations is not an evidence-supported remedy for this earlier read address.
Earlier forwarding/pinning/writer activity may still affect reference provenance;
this observation does not exclude it or any runtime/application cause.

The expected object pointer is still the field value on normal entry. Its actual
value, generation, mapping, type, owner and earlier writer remain unknown. No raw
register, heap, header, fault address, tombstone or private source is newly read or
exported. Arbitrary corrupt control flow is not ruled out by a direct-branch audit.
The owner-approved categorical reader remains available only for a separately admitted
actual original native exit; this static result does not manufacture one.

Only source/control-flow receipts are created locally. There is no binary, SDK,
compiler invocation, product test, Windows/Android build, device command, financial
operation or new cleanup scope. All frozen scopes and protected untracked files stay
untouched. D752 waits for the owner; D749/D751/D607 remain terminal. Original fault
origin, physical performance and wider security/A11Y/platform/release gates remain open.
