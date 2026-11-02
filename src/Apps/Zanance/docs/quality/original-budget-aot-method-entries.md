# Original Budget loader AOT method entries (D-789)

## Original context and finite question

2 November2026. Continue the unresolved D678 Budget/de/Light/680dp native failure,
historical PID10110/UID10216. D772 proves the actual old APK's Normal AOT configuration,
but not individual method availability. D771's separate interpreter finally-OSR
correction lacks original executed-path evidence. Check whether the exact original
Budget loader and its async state machine have native entries before guessing that
an absent AOT body forces another engine. No original UI, financial, crash, runtime
configuration or successful cohort is replayed.

The retained used signed APK is56815522bytes/SHA256
adc8a82af3a626af3cdc8e10995721fb39190b94718a53352d8d4bf5dc785937.
It is outside frozen supplementary scopes. Read the named x64 application assembly
store and native AOT image statically; never load or execute Android code.

## Preserved symbol refusal and bounded metadata alternative

The original libaot-Vafadar.Zanance.App.dll.so has no .symtab. Preserve the initial
symbol-only reader refusal; missing static names cannot prove missing compiled methods.
No duplicate symbol cache, frozen build output or reconstructed APK substitutes for it.

The alternative explicitly expands the first symbol-only scope to original code
identity metadata. Decode only the named managed App image transiently in memory with
the existing SDK decoder; do not write a managed image or inspect user-string heaps,
IL operands, field data, custom-attribute values, embedded resources, configuration,
environment tables, keys or live/private data. Read only original assembly/module
identity and the whitelisted type/method definitions. Dispose the metadata reader and
clear the image buffer. This is not a claim that no managed image was decoded.

Original x64 store format is0x80030003 with206entries/412packed indexes. The App has
descriptor195 and compression-descriptor118, decoded size7677952bytes. An initial
PowerShell signed-hex comparison incorrectly refuses that unsigned format. Preserve
the failure and independently confirm2147680259; correct the numeric representation,
not the original version/count/bounds requirement.

Resolve BudgetEditorViewModel's original nested LoadCoreAsync state-machine ownership,
not a matching name in another type. The selected types and methods have zero generic
parameters, so the original MethodDef-to-AOT index rule applies without a generic
sharing assumption. Two existing application code entries and one abstract interface
declaration provide independent positive/sentinel controls.

## Exact source contract and paired identity

Use the original VMR0427b9abbead3fa64c6655eb4290c537397131fa sources, not current
headers or guessed native offsets:

- [MonoAotFileInfo](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.h#L113)
  declares version187 and the selected pointer/scalar layout.
- [Original MethodDef index](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.c#L5221)
  is RID minus one. Bound it by nmethods minus nextra_methods.
- [Original method-table initialization](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.c#L2305)
  treats a call target equal to the table base as an absent native entry.
- [Actual AMD64 call decoder](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/tramp-amd64.c#L846)
  accepts opcode E8 and its signed relative displacement. The initial mini-amd64
  source-name lookup does not contain this implementation; preserve that limitation.

The selected native image is20220976bytes/SHA256
0c27998ed41d763461cdb0251f653e920c0a93d873e8ef7de812f1a7990b1f28.
Its declared assembly GUID equals the managed MVID
ee40a98c-4e5a-4df8-bd32-b05758403482. The native image has no GNU BuildID note;
preserve the failed positive-BuildID assumption and leave it absent, never invented.
Static identity uses the exact original APK/module hashes and paired MVID. This App
image is context evidence, not a new member of the original SGen crash-frame list.

Derived original layout offsets: method_addresses64, method_flags_table176,
assembly_guid192, assembly_name264, nmethods340, nextra_methods344, flags348,
call_table_entry_size392. Actual values are31667methods,0extras,flags32 and entry size5.
Separate-data, code-only and LLVM-only layouts are refused. Each selected pointer
has one original relative relocation; each five-byte slot is inside bounded file
sections. A non-sentinel target must be in the declared native code range and one
executable section. Export only module-relative code entries, never data pointers.

## Actual original entries and controls

| Original managed method | RID | Native table outcome | Relative code entry |
|---|---:|---|---|
|App constructor|269|Available|0x58960|
|MauiProgram.CreateMauiApp|319|Available|0x5c970|
|BudgetEditorViewModel constructor|13295|Available|0x7d90f0|
|BudgetEditorViewModel.ApplyQueryAsync|13348|Available|0x7db600|
|BudgetEditorViewModel.EnsureLoadedAsync|13349|Available|0x7db6f0|
|BudgetEditorViewModel.LoadAsync|13350|Available|0x7db7b0|
|BudgetEditorViewModel.LoadCoreAsync|13351|Available|0x7db8d0|
|BudgetEditorViewModel.ReloadAsync|13367|Available|0x7df2e0|
|Original nested LoadCoreAsync state machine MoveNext|29322|Available|0xd35100|
|Original nested LoadCoreAsync state machine SetStateMachine|29323|Available|0xd39360|
|IAppDatabaseStartup.PrepareAsync, abstract control|787|Original absent-entry sentinel|None|

All ten available entries lack INTERP_ENTRY_ONLY. The exact original state-machine
name is BudgetEditorViewModel+<LoadCoreAsync>d__147. No next-symbol guess supplies a
function size, and no machine instruction body or executed call stack is inferred
from these entry points. Eleven mappings are not eleven application tests/launches.

Raw exact-source SHA256 values remain e1fa07b52b7ab423897ad8c8c6572e687f27ff59d5219fbf81b5df7cbbd16d9b
for the11912byte header and6d5a2a5328bf78d94911df94c2c83249c49740b21f84f7f49ea01398e1f30bfb
for202644byte aot-runtime.c. The actual54132byte tramp-amd64.c SHA256 is
944e229a3dfc4769010053f1e120341532a7697d13432a186d526978f29b47f8.

## Exact conclusion and preservation

Native AOT entries are available for the selected original Budget loading methods,
including its async MoveNext. The explanation that these specific original bodies
were simply absent from AOT is unsupported. Availability is still not proof of the
actual executed engine, finally/OSR transition, earlier reference writer, managed
array identity or original native fault cause. Do not upgrade a runtime or force
interpreter mode from this result; D771's remedy admission remains unresolved.

Only bounded metadata/evidence is created. No product source, runtime/GC, build/test,
SDK installation, device/authentication/financial command, cleanup or frozen-scope
action occurs. Original APK/module, raw official source and refusal receipts stay;
no managed image is persisted. PID10110 is historical, not current liveness.
D752 alone waits for owner readiness. Whole-Home performance, original fault origin,
broader security/stability/accessibility, iOS/provider/Store/release remain open and
the overall delivery goal stays active.


## Subsequent D796 explicit extent evidence

[D796 original exception prefixes](original-budget-aot-exception-extents.md) now reads
the exact old per-method native metadata: MoveNext has an explicit16989byte extent
and four native clauses, independent of the three-byte gap to the other admitted
entry. This adds a bounded code-reading prerequisite, not historical executed-engine,
finally/OSR, reference-writer or crash-repair proof. D792/D793 subsequently qualify
owner-cancelled CSV/local backup; PDF input remains open, with no current SDK prompt.
