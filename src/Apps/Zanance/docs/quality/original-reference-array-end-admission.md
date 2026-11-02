# Original reference-array end admission (D-757)

## Question and result

D712 proves the original D678 faulting referenced-object word load, but not the
reference's origin. A separate bounded question is whether the original card scan
necessarily interprets the multidimensional-array bounds trailer or rounded object
padding as reference slots. Inspect the exact original source and native code before
changing that loop or adding another workload.

For valid, intact x64 reference-array metadata and corresponding card indices, the
original loop already excludes that trailer and stops at the actual element end.
This excludes the proposed ordinary end-rounding explanation under those stated
preconditions. It does not establish that those preconditions held in the crashed
process, exclude metadata/card corruption, identify an earlier writer or repair QA06.

## Exact source and executable evidence

Use revision `0427b9abbead3fa64c6655eb4290c537397131fa`, not a current-branch
substitute. The original
[size helper](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/metadata/sgen-client-mono.h#L93)
sets `size_without_bounds = header + element_size * length`, adds alignment/bounds
only to the separate total size, then returns their difference through `bounds_size`.
The original
[scanner](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/metadata/sgen-mono.c#L979)
computes `obj_end = obj + obj_size - bounds_size`, clamps each card end to it,
and selects the reference-slot branch with eight-byte increments.

Raw UTF8 source SHA256:

| File | SHA256 |
|---|---|
|sgen-client-mono.h|c8ff48b51418a81686fcb5dcb8ba331a13013fc60962602ba405fb4dbfa8ff8c|
|sgen-mono.c|0400cded9cd7175318c5f4942e72df0ed770fd3e9b9795a0b02a0653be9610c4|

The latter is byte-equal to the retained D700 source. The initial local text write
adds one final LF and fails that exact comparison before arithmetic work. Preserve
the failed local representations and refusal; correct only that serialization and
continue. An attempted nonexistent `sgen-client-mono-internal.h` lookup is also kept
as a failed lookup, not a missing runtime implementation. No original source changes.

Revalidate the original public package Mono module,3290280bytes/SHA256
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`.
Within its executable PT_LOAD, the original client function starts at0x2c6450 and
contains810bytes/SHA256
`4beee135914c163dbb221d7650ddbee97b7a493f6d55cfd78f4de21189e730e2`.
Every retained D700 decoded instruction is consecutive and byte-equal to that current
original file range. Reuse only the preserved textual decode, not its frozen decoder
directory. No new decoder, symbol download, executable execution or native build occurs.

| Original relative location | Static observation |
|---|---|
|0x2c64d8..0x2c64e1|Load array length, multiply by metadata element size, then form header32 plus element bytes in RBP.|
|0x2c64eb /0x2c64f4|Copy the size into separate R14, then add object base to RBP for the element end.|
|0x2c6762..0x2c6775|The bounds-present branch rounds/adds bounds into R14 and rejoins0x2c64f4; it does not replace the element-end size in RBP.|
|0x2c6533|Use separate R14 total size for card-count calculation.|
|0x2c6605 /0x2c6608|Compare card end with element end and conditionally clamp it.|
|0x2c6640 /0x2c6644|Compute first element from its index/stride and actual vector base.|
|0x2c6657..0x2c665a|Skip the reference loop if its first element is already at or beyond the clamped end.|
|0x2c6679 /0x2c667c /0x2c6680..0x2c6683|Original scanner call, eight-byte advance and unsigned below-end continuation.|

These are static instruction-register names, not captured process register values.
The original caller frame0x2c667b remains inside its call instruction. D712's separate
fault PC0x2edd7d and its ordinal are unchanged.

## Conditional arithmetic admission

Let V be the valid vector start and E = V + 8N its element end. Both are eight-byte
aligned. The512-byte card boundaries are also eight-byte aligned. The loop starts at
V for a preceding card, otherwise V plus the exact nonnegative eight-byte element
index. Its clamped end is min(card end,E), likewise aligned. Therefore an admitted
slot S below that clamped end satisfies S >= V and S + 8 <= E. Extra cards covering
only the excluded trailer have no admitted slot. This argument requires valid
metadata, nonoverflowing address/size arithmetic and a valid corresponding card
index; it is not a proof for a corrupt header, descriptor, length or card table.

A finite local integer check exercises all64 eight-byte object residues modulo512
and519 lengths per residue:0..512 plus1023/1024/1025/16383/16384/0x7fefffff. It checks
202129 card cases and10710784 slots. For the largest length only boundary cards are
enumerated; intermediate coverage comes from the conditional divisibility argument,
not an invented exhaustive execution. A deliberate invalid four-byte reference
stride produces an over-end eight-byte read in the model, confirming that the valid
metadata precondition is essential. It is not evidence of that corruption in D678.

These are static arithmetic illustrations, not new product tests, real managed
arrays, a crash reproducer, hardware acceptance or a performance measurement.

## Next boundary and preservation

Do not add a bounds workaround, suppress the object-header read, switch collectors
or build a replacement runtime on this evidence. Original reference origin/type,
actual metadata validity and earlier writes remain unknown. The next diagnostic
must supply genuinely new permitted context or an applicable verified correction;
unchanged D708/D711/D743 workloads and closed performance candidates stay closed.

No product/runtime/GC/model/configuration change, build, managed test, phone/emulator
command, financial operation or private memory/register/database read occurs. Current
D734 product acceptance and D756 rejection remain unchanged. No temporary build or
device output is created; the small local source/code/arithmetic receipts are retained
as evidence. Existing frozen cleanup scopes are untouched. D752 still awaits owner
readiness before its first actual sensitive SDK request; D749/D751/D607 terminal
acceptances are not replayed. Broader stability, performance, security, accessibility,
iOS/provider/Store/release and the overall goal remain open.
