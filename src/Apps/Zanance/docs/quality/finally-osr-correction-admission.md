# Original interpreter finally-OSR correction admission (D-771)

2 November 2026. Continue independent Android fault investigation; D752 remains held
for the unavailable owner. No authentication, financial command, phone or emulator
operation occurs. The original SGen reference provenance remains unresolved.

## Concrete official correction

[Runtime PR135625](https://github.com/dotnet/runtime/pull/135625), merged15October2026
as64804b2df303cea6a63112cf4516ff3f492763a9, repairs interpreter local-value corruption
when OSR occurs inside a finally handler. The correction preserves SSA-renamable IL
locals live across the handler's return; it does not replace a GC card-table scanner.
Its four changed files are transform-opt.c, transform.c, transform.h and handlerloop.cs.
Upstream standalone reproduction and validation are upstream evidence, not our Android
or original-fault acceptance. No copied workload or blanket runtime upgrade follows.

Inspect exact original VMR0427b9abbead3fa64c6655eb4290c537397131fa source, rather than
assuming the feature cannot exist because the public reproducer targets net11.0:

- Original transform.c already emits MINT_TIER_PATCHPOINT_DATA after MINT_CALL_HANDLER,
  admits empty-stack backward-branch patchpoints and excludes handler blocks from
  ordinary CFG links.
- Original insert_tiering_defs visits regular non-EH basic blocks and live-in locals.
  It lacks the new finally-continuation/live-out preservation pass and the new
  clause_has_patchpoint tracking. The source-level correction is absent.
- Original interp.c initializes tiering only when INTERP_OPT_TIERING is set; tiering.c
  then enables it. Presence of this code does not prove the original app executed it.

All seven exact downloaded UTF8 files are bounded to2MiB each and hashed without a
newline rewrite. The central original SHA256 values are:

| Source | SHA256 |
|---|---|
|tiering.c|26595d6a0ccd332d451705468563a0e881585bd31bba1a548bd7d60a1c2f47fa|
|interp.c|1f47513497020c60a404bfce7881d2847a98fad40c5b98660c55f1cd2a832578|
|transform-opt.c|dc067c25941a608ccb84fde9eb399a5c8cd42ef529a338bb271234ba84d2b89f|
|transform.c|17e98eaaf5f9dc5ad9cb6c28ac2a26b20714b09d9ba7c8c872c2ac74dbfc6632|
|transform.h|8a42c65eceecaf691a2e1d8be6c798fb8758af6782d4d408b2b38f72a972c42c|

## Current execution-mode admission

Read-only evaluation of the actual current app project for offline Release/android-x64
reports AndroidUseInterpreter=False, AotAssemblies=True, AndroidAotMode=Normal,
AndroidEnableProfiledAot=true and embedded assemblies. The installed SDK36.1.69 enables
its implicit UseInterpreter default only in Debug; its Release fallback is False.
An initial UseInterpreter-only evaluation returns empty, so inspect the actual effective
AndroidUseInterpreter property before interpreting it. No build target executes.

This is current evaluated project/SDK evidence, not a retrospective original D678
runtime flag dump, proof that no method can ever be interpreted or a complete APK
execution trace. The original bounded native frames do not establish an interpreted
finally loop, OSR transition or affected local. Therefore no matching original-fault
remedy is admitted yet. Do not enable another execution mode to manufacture applicability,
run the integer example on ordinary CoreCLR as Mono evidence, or label a successful
unrelated run as an Android crash fix. Preserve the possible source-level issue and
its missing execution-path input without claiming a causal exclusion.

## Separate reference-cold input and preservation

D772 subsequently binds the original used APK's declared native AOT mode to Normal,
without reading app images/environment tables. This improves original configuration
provenance but still does not establish every original method's execution path.
[Original package metadata and limits](original-apk-native-execution-mode.md).

The newly considered10k cold measurement does not begin: the identified retained
reference APK/transport roots are in D745's frozen rejection scopes. They are not read,
hashed, copied, recreated through an alternate export or used to build another caller.
No native reference/cold acceptance follows from this preflight. D766's one tenfold
boundary remains separate and is not repeated.

Only public sources, patch metadata, scoped SDK property evaluation and static receipts
are created locally. There is no compiler, binary, app/product test, source mutation,
runtime/GC change, financial replay or cleanup scope. All earlier frozen resources and
protected untracked files remain untouched. D749/D751/D607 stay terminal; D752 waits
for the owner. Original fault, performance and broader platform/release gates stay open.
