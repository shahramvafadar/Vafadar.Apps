# Nursery pin-check phase incompatibility (D-761)

2 November 2026. This is static public-source and exact installed-code analysis following
[D760](sparse-native-unwind-metadata.md), not another diagnostic run or product fix.
The newly located assertion cannot independently establish a corrupt nursery object.
The collector clears nursery pin tags before asking the later checker to require them.
Do not remove that assertion or disable a production setting as a claimed crash repair.

## Exact inputs and ordered operations

Runtime revision is `0427b9abbead3fa64c6655eb4290c537397131fa`. The already admitted
original public Mono module SHA256 is revalidated as
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`, and matching
public debug SHA256 as `8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0`.
No record, memory, object header, register or private data is newly read. The selected
public module and sources are outside every frozen cleanup scope.

1. [major_finish_collection](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.c#L2362)
   calls sgen_build_nursery_fragments before finishing the pin queue and checking marks.
   The exact installed code calls fragment creation at0x2dd637, pin-queue finishing at
   0x2dd67c, and sgen_check_heap_marked at0x2dd69f, returning at D760's observed0x2dd6a3.
2. [Fragment construction](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-nursery-allocator.c#L748)
   explicitly SGEN_UNPIN_OBJECTs each admitted pin entry before preserving its occupied
   range and publishing the surrounding free fragments. Exact installed instructions
   at0x304e88 load that word,0x304e8c AND it with-8, and0x304e90 store it back.
3. [SGEN_UNPIN_OBJECT](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-gc.h#L284)
   clears pin/cement/forward tag bits; it does not free or invalidate the object merely
   because the pin bit clears. The pin queue is then cleared by sgen_finish_pinning;
   that function has no re-pinning operation.
4. [The later checker](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-debug.c#L570)
   scans valid remaining nursery objects and asserts their pin bit when its flag is
   true. The concurrent-in-progress flag is reset only near the end of major finish
   at sgen-gc.c:2440, after this check at2383. The installed code reloads the flag
   immediately before the checker call. D760 actually took the flag-true assertion.
5. The [simple-nursery hooks](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-simple-nursery.c#L46)
   return no exclusion list and have an empty build_fragments_finish; they do not
   restore the cleared pin bit. This is a source-defined path, not proof of every
   minor-collector variant or the identity of D760's particular failing object.

Thus a valid nursery survivor retained by the pin queue can have its tag deliberately
cleared by fragment construction before the checker requires that tag again. The
phase-order incompatibility is supported by both exact public source and original
installed instructions. It is not proof that the failing object was one specific pin
entry: no address/type/header was collected. Other invalid-reference paths remain
possible. A known inconsistent diagnostic precondition must not be presented as an
independent demonstration of product heap corruption.

## Consequence for the investigation

D760's native termination, source coordinates and fourteen code frames remain valid.
Its assertion interpretation is narrowed: this check-mark-bits termination has a
source-supported diagnostic phase problem. It does not establish the earlier writer,
original D678 ordinary SIGSEGV cause, historical D743 assertion cause or a repair.
Do not replay the same diagnostic with the check suppressed, alter production GC,
pin every object or turn an altered diagnostic's survival into product acceptance.

The next useful missing input is evidence about the original ordinary reference-load
failure, independent of this incompatible post-fragment pin check. Any new observation
must first have a concrete reference-origin boundary, preserve typed source and accepted
checkpoints, and avoid raw memory/register/object disclosure. An unchanged generic
stress run or another Home timing pair supplies none of that missing evidence.

## Scope and completion

Only four public source files were newly retrieved; the already retained collector/
checker source and two bounded installed-code disassemblies complete the comparison.
No native/emulator/phone command, build, test suite, seed or financial write occurs.
There is no new product test or runtime patch. No new build output needs cleanup;
small source/binding/disassembly evidence is retained. Every older frozen scope,
including D760's rejected log and twenty separate targets, is untouched.

D752 genuine owner readiness remains pending; do not release its held SDK request on
elapsed time or interpret a prior terminal cancellation as readiness for a new cohort.
D749/D751/D607 stay settled, broader delivery stays active, and later iOS/TalkBack order
is unchanged. This source finding does not qualify general stability, security,
performance, accessibility, provider, Store or release acceptance.
