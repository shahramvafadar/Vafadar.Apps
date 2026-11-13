# Exact current Tarjan predicate and fault ordering - D1153

[D1123](tarjan-concurrent-source-admission-d1123.md) proves public source
correspondence, explicitly leaving compiled instructions unproved. This separate
static review closes that finite binary premise for the actual D1146module. It
does not reopen [D1151](current-bridge-header-operation-d1147.md)'s consumed
register question, issue a matrix case or change a runtime/collector.

## Exact public input and complete code binding

The actual crashed PID5017/UID10216 module remains SHA256
`b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034`,
BuildId `b1881505cd1c881ba16fec2997743cbc7033e60d`. Its previously admitted
official public symbols remain SHA256
`ad8f10412be8b8efdd4d5f3b6bd1e4904c9acd23aaf46321104ed31ea2aab259`,
with matching BuildId and GNU debuglink CRC24fa95dc. No download or SDK exit/trace
read occurs. Exact function symbols and executable PT_LOAD bounds admit only:

| Function | Relative start | Bytes | Instructions | Code SHA256 |
| --- | --- | ---: | ---: | --- |
| push_object |0x2c3b10|386|95|`cd8d9672730616706c54ecdc6dafa691a7121b1af2ced3fed64053a4f21c5364`|
| sgen_object_is_live |0x2d83d0|258|78|`757499d0cfa8a1c7a45a9cc51a2567100af9e84abc61f0ae7e8a084b06a76ecb`|

The already installed signed static decoder disassembles these644public code
bytes without loading them. Each direct branch uses its original opcode length
and signed displacement; all other emitted bytes must match the original. The
complete reconstructed byte sequences now match both functions, extending D1147's
limited49byte prefix proof without relabelling that older evidence.

Initial whole-function comparison refuses at push_object offset119: the decoder
canonicalizes a nine-byte padding NOP to five bytes. The original refusal remains.
The saved first decode is reused; only that exact nine-byte original NOP plus its
exact expected decoded instruction is specially admitted. No arbitrary mismatch
or branch relaxation is allowed. The previously unissued helper is then decoded.
These are new static reader checks, not product tests or execution of native code.

## Actual call and generation-zero result

At relative0x2c3b73, the original direct call resolves exactly to the symbol
sgen_object_is_live at0x2d83d0. Its result is tested at0x2c3b78; nonzero branches
to the common return at0x2c3c5a. This is the old helper, rather than an inferred
inlined generation-aware replacement from the proposed public change.

Inside that helper, the comparison at0x2d8431 resolves through its static
RIP-relative operand to the exact public symbol
sgen_current_collection_generation. Generation zero branches at0x2d8438 to
0x2d8491: xor eax,eax; pop rbx; ret. The earlier nursery-membership operands bind
to public sgen_nursery_bits/sgen_nursery_start symbols. The admitted original
header declares GENERATION_NURSERY first in its enum and its generation-aware
replacement returns true, instead, for a non-nursery object during that collection.
Only public symbol identities/code are read; no value from a data section,
actual collection generation, nursery membership, object or live memory is read.

## Earlier fault is not directly protected by the later guard

The original fault stays at0x2c3b3d, the header-derived gc_bits test from D1147.
The complete direct-control-flow graph reaches the old helper call only after
that faulting operation: removing that operation makes the call unreachable from
normal entry. The guard therefore cannot directly protect that same invocation's
earlier access, even if its later liveness predicate is corrected. This does not
rule out a change to an earlier object's processing or a concurrent interleaving;
the proposal is not disproved as an indirect remedy, nor qualified as one.

No actual header tag, object validity, earlier writer, generation, dynamic call
path or original cause is established. The public proposal remains the previously
observed open/unmerged source premise; its status is not queried again merely for
this review. A new runtime trial, collector switch or successful-case replay does
not follow from this code proof.

## Preserved continuation

Artifacts under artifacts/d1153-compiled-tarjan-predicate retain original identity
proofs, both decoder outputs, the padding refusal and complete reconstruction/
control-flow receipts. No build output, cleanup, device/Main/C7/Q02 action,
register/address/memory export, runtime configuration, financial write or passing
suite is repeated. All25qualified contexts and11incomplete/unissued contexts stay
as recorded by D1151. The full delivery goal remains active; original cause/remedy,
whole performance and broader delivery gates remain open.
