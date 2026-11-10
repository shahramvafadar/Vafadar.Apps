# Current public CoreLib managed/AOT identity (D-786)

[D1013 subsequently compares69other SDK System-framework pairs per ABI](public-system-aot-identity-d1013.md)
in a later current APK. CoreLib is outside that new allowlist; the CoreLib results
below remain specific to this original dated D786package and are not rerun.

## Concrete artifact-mismatch admission

Continue independent Android fault investigation while only D752 awaits the owner's
presence. Do not reproduce accepted UI, financial, collector or performance cohorts.
The original D678 SGen reference-array fault remains unresolved; no runtime change,
blanket resource lock or removal of original Home validation follows from this review.

The original application imports `eng/Maui.props`, which explicitly selects
`MauiXamlInflator=SourceGen`. Absence of this setting cannot dismiss the official
[MAUI38281 report](https://github.com/dotnet/maui/issues/38281), transferred from
runtime132454. That report concerns an inconsistent deployed assembly/AOT pair and
debug-session-dependent launches. It does not identify the origin of our SGen fault.
Its reported workaround is not adopted without applicable original evidence.

Inspect the already used complete D783 signed APK, rather than rebuilding it:
SHA256 `8c7dfe7904ad323777519aaf6184906892a704566750acb5e732f48a09e501a6`,
99458975bytes. D785 used these exact bytes on its fresh owned emulator. Neither that
UI cohort nor its package integrity checks need another run. No physical Main or
private database/key is inspected. Decode only the public System.Private.CoreLib
module; no application assembly, configuration, licence value or credential is decoded.

## Original runtime contract and actual native metadata

The original runtime source identifier remains
0427b9abbead3fa64c6655eb4290c537397131fa. Its
[AOT format declaration](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.h#L113)
declares format187 and an assembly_guid pointer. Its
[actual comparison](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/mini/aot-runtime.c#L1637)
compares the loaded image GUID against that field before accepting the AOT module.
This is the assembly MVID, not the unrelated AOT compilation identifier.

Retain exact raw official inputs: header11912bytes/SHA256
e1fa07b52b7ab423897ad8c8c6572e687f27ff59d5219fbf81b5df7cbbd16d9b,
runtime202644bytes/SHA256
6d5a2a5328bf78d94911df94c2c83249c49740b21f84f7f49ea01398e1f30bfb.
Count original pointer declarations, including method_flags_table and runtime_version;
the64-bit assembly_guid/assembly_name offsets are192/264. No arbitrary UUID scan
substitutes for the original metadata fields.

For each ABI, admit one ELF64/little-endian defined global object symbol named
mono_aot_file_info. Bound its complete272byte prefix in exactly one allocated file
section, require version187 and one symbol-free architecture-appropriate RELATIVE
relocation per selected pointer. Resolve only the bounded public name/GUID strings.
Require the original assembly name and a complete valid GUID.

The initial observer incorrectly requires a positive ELF symbol size. The actual
defined global object has st_size0; preserve that refusal. Correct the observer to
use the complete source-declared prefix and allocated-section bounds, not an invented
object size or an unbounded search. No native code or previous workload executes.

## Independently decoded managed identity

The installed SDK's original assembly-store format3 is already documented by D739.
Use its exact ABI/version flags and packed index/descriptor/name layout. Resolve
only System.Private.CoreLib.dll by name. Both stores have206entries, descriptor73,
mapping73 and compression-descriptor192; these identities are separate fields,
not interchangeable assumptions. The SDK XALZ block declares the bounded PE size.

Read the Module row's MVID from the decoded PE metadata. Independently decode the
same two public blocks with the installed SDK LZ4 implementation and read their
MVIDs with the host's System.Reflection.Metadata. Both independent complete PE
digests, output sizes and GUIDs agree with the bounded static reader.

| ABI | Public PE bytes / SHA256 | Managed MVID and actual AOT assembly GUID |
|---|---|---|
|arm64-v8a|2489344 / `2a10cbecf5f5c8a00bc8c4a1dbea544c764d887a6e5f28437e4da44b4cde0751`|`f73e3452-b766-46cf-9b99-ff62e0a0213b`, equal|
|x86_64|2495488 / `b5e4b6f0b884ef7cf204bcfc27e499993a5f560a01d00b080df40039a8f5dcee`|`8c8b28c6-cf81-4e8c-ade4-71361fde1435`, equal|

Actual CoreLib AOT modules are2785088bytes/SHA256
c033d356b83f784fc0bd0f171d57707116cc9698e3ee853d880b079590cc62d5
on ARM64 and2802520bytes/SHA256
a5da37dafd60744e44058037b95a06d1e69e3281595cbc8c52272a3a3f642649
on x64. No Android assembly or ELF is loaded/executed on the host.

Nine new private reader controls cover refused ELF32/big-endian/truncated input,
missing named symbol, absent relative relocation, truncated compression header,
invalid LZ4 back-reference, invalid PE signature and a changed GUID that must not
compare equal. These are bounded diagnostic controls, not nine product tests,
real crash classifications or additional native qualifications.

## Exact conclusion and preservation

The current retained CoreLib pair has no assembly-GUID mismatch on either ABI.
This closes only that specific current-package premise. It does not prove that all
206assemblies/dependency identities, original D678 bytes, generator paths or historical
deployments were inspected, or that SourceGen cannot contribute indirectly to another
fault. It does not repair or explain the original bad managed reference in SGen.

No product source, build setting, runtime/GC, dependency, database schema, financial
behavior or product-test count changes. No new build/test-suite/device/authentication/
financial command, install, cleanup or frozen-scope access occurs. The exact used APK
is rehashed unchanged at completion; public compressed controls and bounded evidence
remain. D752 stays pending only for owner readiness. Original fault origin, whole-Home
performance/two-second target, broader security/accessibility, iOS/provider/Store and
release gates remain open; the overall delivery goal stays active.
