# Original Budget AOT exception prefixes and explicit body extents (D-796)

## Finite original question and identity

2 November2026. D789 proves original non-generic Budget AOT entries, but deliberately
leaves method-body size unknown. Before interpreting bounded native code or possible
finally/OSR routes, determine whether the actual original AOT metadata supplies an
explicit extent. Do not guess from the next method/symbol, run the original failed
UI case, force another engine or export private/live data.

The retained used D678 APK is SHA256
adc8a82af3a626af3cdc8e10995721fb39190b94718a53352d8d4bf5dc785937.
Its exact x64 libaot-Vafadar.Zanance.App.dll.so is20220976bytes/SHA256
0c27998ed41d763461cdb0251f653e920c0a93d873e8ef7de812f1a7990b1f28,
paired managed/AOT MVID ee40a98c-4e5a-4df8-bd32-b05758403482. Reuse D789's ten
admitted non-generic code identities and actual method-table entries; verify original
APK/module/format/MVID again before reading selected native exception prefixes.
No managed PE is decoded or persisted in this batch. The native image has no standard
.eh_frame section; no fictitious FDE/symbol substitute is introduced.

## Exact source and bounded decoder

Use retained raw original VMR0427b9abbead3fa64c6655eb4290c537397131fa sources:

- [MonoAotFileInfo](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.h#L113)
  declares blob/ex_info_offsets and format187. Header SHA256 remains
  e1fa07b52b7ab423897ad8c8c6572e687f27ff59d5219fbf81b5df7cbbd16d9b.
- [Original compressed values and offset tables](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.c#L347)
  define1/2/4/5byte signed-value handling and selected group delta accumulation.
- [Original exception-prefix decoder](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.c#L3131)
  reads flags/unwind/hole count and, only with bit128, an explicit code_len that
  replaces the padded interval supplied by the method-address map. Runtime C SHA256
  remains6d5a2a5328bf78d94911df94c2c83249c49740b21f84f7f49ea01398e1f30bfb.

The actual original exception offset table has31667entries/group size10/3167groups/
4byte indexes. Require bounded declared counts/groups/index width, one allocated file
section, exact original relative relocations and x64/native format. Decode only the
selected group through that selected method; do not inspect whole blob/environment
or catch/type payloads. Each original prefix consumes4to10bytes, at most32 permitted.
Unknown/LLVM flags, malformed/truncated groups, invalid indices/lengths and extents
outside the original executable section refuse. The abstract native-entry sentinel
is refused before any exception lookup.

25 new private decoder controls cover compact encodings, signed negative deltas,
truncation, two/four-byte indexes, out-of-range indices and resetting at a new group.
These are component checks, not25 application tests, native launches or crash samples.
The source contract and original identity guards are not weakened for an expected result.

## Actual original explicit code extents

Every selected prefix has original HAS_ARCH_EH_JIT_INFO, so its length below is explicit,
not the distance to a neighboring entry. Code locations are module-relative only.

| Original code identity | RID | Relative entry | Explicit bytes | Exception flags | Native clauses |
|---|---|---|---|---|---|
| App..ctor | 269 | 0x58960 | 1816 | 138 | 0 |
| MauiProgram.CreateMauiApp | 319 | 0x5c970 | 5803 | 138 | 0 |
| BudgetEditorViewModel..ctor | 13295 | 0x7d90f0 | 1417 | 138 | 0 |
| BudgetEditorViewModel.ApplyQueryAsync | 13348 | 0x7db600 | 234 | 138 | 0 |
| BudgetEditorViewModel.EnsureLoadedAsync | 13349 | 0x7db6f0 | 192 | 138 | 0 |
| BudgetEditorViewModel.LoadAsync | 13350 | 0x7db7b0 | 287 | 138 | 0 |
| BudgetEditorViewModel.LoadCoreAsync | 13351 | 0x7db8d0 | 195 | 138 | 0 |
| BudgetEditorViewModel.ReloadAsync | 13367 | 0x7df2e0 | 192 | 138 | 0 |
| <LoadCoreAsync>d__147.MoveNext | 29322 | 0xd35100 | 16989 | 174 | 4 |
| <LoadCoreAsync>d__147.SetStateMachine | 29323 | 0xd39360 | 80 | 138 | 0 |

Original Budget LoadCoreAsync.MoveNext is exactly16989bytes, from relative0xd35100 to
exclusive0xd3935d, with four native exception clauses and try-hole metadata. The other
admitted SetStateMachine entry is0xd39360, three bytes beyond that explicit end. This
shows why simply subtracting that entry would overstate the exact body by three bytes;
no claim is made about uninspected methods in the gap.

All selected prefixes report the GC-map flag absent, including both application
controls. That optional flag is not proof that GC roots, maps or barriers are missing
or broken; no such cause is established. Only clause presence/count is decoded:
catch/filter/finally kinds, handler payloads and execution are not inferred.

## Result and remaining boundary

The exact old Budget body can now be bounded without a symbol/next-method guess.
This removes a code-reading prerequisite; it does not prove which engine actually
executed at historical PID10110, a finally/OSR transition, the earlier bad-reference
writer/array identity, a runtime remedy or crash repair. No App/native code execution,
managed IL operand/user-string/field/resource/config/environment/key or live memory/
register/map/dump read occurs. No image reconstruction/persistence, build, old test,
financial/SDK/device action, cleanup or frozen scope occurs. Private reader and bounded
original identity/prefix/extent proofs remain local; no temporary product code exists.

D792/D793 original CSV/local-backup cancellations remain accepted and retired; PDF
still needs a legitimate visible original input, with no prompt pending. Historical
D789 owner-wait wording describes that earlier stage. Original native fault, whole-
Home speed and broader security/accessibility/iOS/provider/Store/release remain open;
overall goal stays active.

Related: [D789 actual original entries](original-budget-aot-method-entries.md),
[original fault symbols](exact-original-native-fault-symbols.md),
[current sensitive-route frontier](physical-sensitive-cancellation-completion.md).

## Subsequent D797 clause-decoder refusal

[D797](original-clause-decoding-review-refusal.md) locates the exact original public
clause enum but both preparation/download and read-only decoding executions are
refused before process creation. No actual clause-kind table or new control result
exists; the four clauses above must not be relabelled as finally handlers. The
explicit body extent remains the completed D796 result, not execution/cause proof.
