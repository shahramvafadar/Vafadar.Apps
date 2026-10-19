# D-378 original ARM64 bridge fault and bounded local validation

## Exact code identity

Start from the D-377 actual native death PID18145/status11, not an observer timeout. The immutable
independent package SHA256 is
ba7c843d8f5067f19def7aa59a8b7d4377706284690a4ad1c3a3b47a28240f49.
Its ARM64 libmonosgen-2.0.so SHA256 is
6532983fcd509af0c6fac4d39c37764128f5b12feedf9a9b32079635f70fc3e2;
GNU BuildId f690337ef526804ac438ffffe96c594a02752b18, debuglink CRC99702995.

Official Microsoft symbol data matches both BuildId and CRC;16,455,624 bytes/SHA256
087d907988bfcfd88996603e5a0719ff00fad5d52deb802147a6e1bcaff552de.
The first attempted key uses the original debug filename and returns HTTP404; inspect the official
key generator and use the canonical _.debug/elf-buildid-sym-<id>/_.debug index. No guessed symbols.
Existing pyelftools0.33 wheel SHA256
f215ad5f47d3f1373a21496a6c9e0707c622840d0622f23ff7ce08678b020036 matches official package metadata;
zip-import only, no global installation. Symbols, CRC and full DWARF lines are independently checked.

| Native PC | Containing function | Source location |
| --- | --- | --- |
|0x29d438|push_object+16|sgen-tarjan-bridge.c:373:7; inlined bridge_object_forward loads o[0]|
|0x29c734|processing_stw_step+3320|sgen-scan-object.h:59:3|
|0x297d24|sgen_bridge_processing_stw_step+28|sgen-bridge.c:224:2|
|0x2b5ed8|finish_gray_stack+276|sgen-client-mono.h:239:2|
|0x2b5004|collect_nursery+1440|sgen-gc.c:1930:2|
|0x2b14ec|sgen_perform_collection+500|sgen-gc.c:2650:7|
|0x2b12d4|sgen_ensure_free_space+184|sgen-gc.c:2616:2|
|0x2adadc|sgen_alloc_obj_nolock+952|sgen-alloc.c:258:6|
|0x2a02c8|mono_gc_alloc_vector+132|sgen-mono.c:1119:20|

Exact official runtime source commit4099f83977f4d29604a9aeb971fb0a7bc1d5f699 has the inlined
object-header access at the reported line. This identifies the failing bridge path, not the
corrupted object, origin of corruption or all native failures. It resembles the upstream bridge
signature; reports of other applications do not prove an identical full cause here.

[Official ELF key format](https://github.com/dotnet/symstore/blob/main/src/Microsoft.SymbolStore/KeyGenerators/ELFFileKeyGenerator.cs),
[key construction](https://github.com/dotnet/symstore/blob/main/src/Microsoft.SymbolStore/KeyGenerators/KeyGenerator.cs),
[exact bridge source](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/metadata/sgen-tarjan-bridge.c),
[upstream investigation](https://github.com/dotnet/runtime/issues/123537).

## Runtime boundary and rejected diagnostic input

Official release metadata checked2026-10-19 still lists10.0.13 as the stable serviced .NET10 runtime.
Keep the existing compiler/task/runtime match. Do not downgrade, fork or adopt an experimental
runtime. Earlier serial-major and precise-Tarjan trials remain rejected; do not repeat them here.

Inspect maintainer-suggested local checks before execution. sgen_check_canary_for_object in the
exact sgen-gc.c writes the object's raw bytes and surrounding nursery on error. An initial local
independent build contains the combined remset/canary input but is never installed or executed;
retain that rejection and its package hash locally. Remove nursery-canaries and rebuild before any
device input. This is a source-derived safety decision, not evidence that a canary error occurred.

The remset-only path checks old-to-young references and may assert on missing unpinned references;
no binary-protocol output, nursery dumping, arbitrary environment logging, heap/register/raw-stack
capture or debugger is enabled. Inspect only the fixed runtime mode marker and public original
stage/completion receipts. The concurrent major GC and all native engine bytes remain unchanged.

[Stable release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json),
[exact diagnostic implementation](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/sgen/sgen-gc.c),
[exact remset checks](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/src/mono/mono/sgen/sgen-debug.c).

## Actual original Samsung cohort

Coordinate a fresh exclusive phone turn and require awake foreground original Main19291, profile
Zanance_Test_D312_Main and979.00 EUR before input. Main package is unchanged D-377. Use an
artifact-only CustomAfterMicrosoftCommonTargets override for the independent caller, never tracked
production/configuration. Its copied Activity requires exactly check-remset-consistency and emits
one fixed mode marker after original covered startup; no arbitrary environment contents are logged.

Strict Release/AOT/trim ARM64 build:0 warnings/errors,1:20.91. Immutable offline signed caller
SHA256275faf1c57a728a2b642b2b246f1a3da143345c5ce46f5008e9a263e715f344f,
53,256,576 bytes,192 AOT libraries, unchanged four engine libraries and certificate92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
Actual libxamarin-app.so contains remset-only input and the original major=marksweep-conc mode,
not nursery-canaries; no Internet/debuggable/profileable.

Retained protected unreviewed run d53542fe57d6446ab52b82a757ee7f6e, comparison
a04e414cc83a4d5a9c1685e55711170c, original baseline f3d82fe68b9c43e7905866945ce5ea86.
PID20385 proves the fixed runtime marker and completes all six reference/tenfold en/fa/de original
Home cases, two publications each, in one process. No completed cases are repeated through a
continuation. Complete raw schema/typed rows, balances, original full financial/native presentation,
layout and actual focus match. No fatal remset assertion occurs in this bounded cohort. Do not
claim the random original SIGSEGV is fixed or disproved. Diagnostic timings are not speed evidence.
No new prepared/reseeded files, replacement keys or financial Save.

## Restore and limits

Stop only the terminal independent caller, reinstall the exact ordinary D-377 independent APK,
verify its installed hash and keep it stopped. Return original Main19291: every meaningful native
text/description/bounds/state matches its pre-trial tree, including profile and979.00 EUR. Exact
font/accessibility/size/density values and unchanged installed Main APK hash match. Release the
exclusive phone turn. Clean temporary diagnostic build outputs with0 warnings/errors; ignored
source/evidence remains separate from tracked production. No device setting, personal data,
authentication or Main upgrade is changed.

Main app/code/APK/runtime remain D-377, SHA256
d349a8c19b7e3d76c546565a19d781de353415b481542765d70401c29cc7c621.
No new managed tests, full-suite replay, Windows matrix, Main build or unrelated successful check.
This evidence advances fault localization only. General runtime/GC/native stability, ANR/cold/low
memory, Q-02, complete-device/spoken-reader/iOS/provider/Store/company/release gates remain open.
Artifacts/home-native-d378 retains exact symbol/source identities, rejected unexecuted input,
immutable diagnostic package, mode/stage/public result, same-process restoration and cleanup.
