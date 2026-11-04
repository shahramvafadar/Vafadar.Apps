# Original concurrent descriptor correction admission (D-811)

4 November 2026. Continue the original native-reference investigation without another
app launch, failure injection, register interpretation or financial/UI replay. D809's
actual topPC2f0f66 belongs to concurrent object scanning; a similar upstream function
name is only a lead, not a common-cause diagnosis.

## Exact correction, not a suggested workaround

[Runtime63293](https://github.com/dotnet/runtime/pull/63293) and its
[original Mono21384](https://github.com/mono/mono/pull/21384) fix a concurrent descriptor
read racing with Tarjan bridge tagging. The merged runtime commit is
`34101393a62ce483ee16c34047358d4813aab9c5`, dated3January2022. Its actual patch replaces
three unsafe descriptor reads with sgen_obj_get_descriptor_safe: two card-table call
sites and one small-object scan in scan_card_table_for_block. No bridge/collector
switch, tag-mask speculation or arbitrary current-branch patch follows from the report.

The actual source VMR0427b9abbead3fa64c6655eb4290c537397131fa already contains all three
exact added statements: sgen-cardtable.c626/628 and sgen-marksweep.c2633. The original
sgen-gc.h safe helper uses SGEN_LOAD_VTABLE and pointer untagging, unlike the unsafe
helper. The retained card-table source and header have their previous exact hashes;
only the new marksweep source and immutable upstream patch need retrieval. The
previously refused D700 disassembler directory is not read or reused.

[Original card-table source](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-cardtable.c#L626),
[original marksweep source](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-marksweep.c#L2633),
[original safe helper](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.h#L799).

## Actual bounded native-code evidence

Read only the preserved public original Mono ELF, SHA256
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`,
and matching previously retained debug metadata, SHA256
`8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0`.
No managed App image, user string, environment/configuration, secret, actual register,
map, stack/heap or new system record is inspected.

The exact positive-size scan_card_table_for_block is923bytes at relative301d40.
Decode its complete257instructions with the existing reviewed NDK utility, without
loading/executing the ELF. Every instruction is contiguous, ends at exclusive3020db
and matches bytes in the original executable PT_LOAD. A local parser initially drops
the last byte of a ten-byte instruction with no padding before its tab separator;
preserve the refusal and correct the text-column parser against those same saved
bytes. No product/component test or native observation is repeated.

At302058, bytes4883e0f8 clear the three low bits before the descriptor read at30205c,
bytes488b7008, and the indirect scan callback at302064. No direct branch in this
complete function enters the descriptor read/callback after that mask. Reuse the
already retained D700 card-table instruction packet, without rerunning its decoder:
2d5938/4883e1f8 masks before2d593c/488b7108; both bytes match the same original ELF.
These are two actual compiled mask blocks corresponding to three source call sites,
not every runtime function, every architecture or proof those sites ran in our failure.
Ignore nearest exported-symbol annotations in disassembly branch labels.

## Result and preservation

The premise that this exact old descriptor correction is missing from the original
source/compiled sites is not supported. Do not add it again or replace the runtime on
that basis. D809's actual2f0f66 fault and older D6782edd7d remain different sites;
neither their earlier writer/reference origin nor a common cause is established.
D809's approved real R13 observation remains unknown and is not repeated or broadened.
All original runtime/GC/product/device state, retained AVD/data/record, frozen scopes,
terminal D607/D752 and accepted successors remain untouched. There is no build, old
suite replay, new product test, download/install of a tool, cleanup or financial action.
The finite source/binary admission closes this particular missing-fix premise only;
overall goal and original fault/performance/security/platform/release gates remain open.

Related: [current actual native exit](current-large-budget-native-exit.md),
[earlier distinct card-alignment admission](original-sgen-card-scan-correction-admission.md),
[remaining delivery frontier](remaining-delivery-evidence.md).
