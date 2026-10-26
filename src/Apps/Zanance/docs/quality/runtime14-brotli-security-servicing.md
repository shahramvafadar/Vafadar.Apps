# D-554 - Official10.0.14 Brotli security servicing

## Newly applicable official dependency correction

Microsoft publishes .NET10.0.14 on25October2026 as an out-of-band security release. Its Brotli
decoder correction, CVE-2026-111497, applies to the System.IO.Compression APIs used by the original
Home ledger block codec. This identifies a relevant component correction, not evidence of a
Zanance exploit or the origin of any historical Mono/GC fault. The source-corruption cause remains
unidentified; runtime123537 and Android12765 remain open at the26October review.

[Official release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.14/10.0.14.md),
[official release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json),
[concurrent scan investigation](https://github.com/dotnet/runtime/issues/123537),
[distinct JNI report](https://github.com/dotnet/android/issues/12765).

Change only VafadarSecurityRuntimeVersion from10.0.13 to10.0.14. Existing project targets select
matching Android runtime/compiler/task and the self-contained Windows runtime. SDK10.0.401,
MAUI10.0.120, financial sources/algorithms/schema, all unrelated package versions, GC mode,
permissions and account/provider settings remain unchanged. No global SDK/workload/environment
installation or service/container deployment occurs. All five official Mono runtime/cross-compiler/
task packages retain MIT and source revision0427b9abbead3fa64c6655eb4290c537397131fa. Both actual
cross-compilers report that exact version/revision. No experimental binary or runtime workaround.

The Windows test runtime is an isolated official archive, verified against its release-metadata
SHA512 before scoped extraction. Its actual executable runs the affected tests on10.0.14; it is
not enough to build against a property while running an older installed test host. Existing
redistribution notices remain applicable; this is not a new application SDK or data collector.

## Focused host and complete-package evidence

31 selected existing cases pass on actual x64 .NET10.0.14:22 complete codec cases and nine
read-buffer/cancellation/malformed/fallback cases. Zero failures/skips. They repeat only because
the underlying decoder changes; unrelated financial suites and accepted user operations do not
repeat. No new managed case or increase in the current-product test count. Strict Windows,
complete dual-architecture Android and independent codec builds pass without warnings/errors.

| Complete signed Offline candidate | Bytes | SHA256 |
| --- | --- | --- |
| Shipping ARM64+x64 |97071007|50f5d58b7848e5456941f89eedb7a19e2b50f67599afaac72a301daa49696895|
| Independent x64 codec |59064226|8aa52623d439626df1c0bc37721c3c6c12e654ada6eae5d7c55368407b670905|

Shipping has384 own-build matching AOT images/408 ELF/1608 statically16KiB-aligned LOAD segments;
independent has192/204/804. Each own witness is captured before another compilation can overwrite
mutable output. Both pass ZIP CRC/alignment/v2v3 and no INTERNET/debuggable/profileable checks.
Original concurrent major mode remains on both shipping ABIs.

Actual packaged ARM64/x64 Mono and Brotli native libraries match official10.0.14 runtime packs
byte-for-byte. Windows self-contained runtime config selects14 and its actual Brotli native DLL
matches the official14 archive. Independent and shipping actual Core/Data and managed/native
Brotli images match exactly; both use the same actual Mono native image. This binds the changed
runtime/component, not just restore metadata or an assumed installed SDK version.

## Preserved input-observer refusals

The first NuGet reader assumes one XML namespace and refuses before producing provenance;
read actual namespace-independent metadata without rebuilding or changing package authority.
An added all-BCL byte-equality assertion also refuses before native install: independent and
shipping Private.CoreLib/System.Runtime differ after their distinct application linking. Do not
claim all BCL images equal. Decode each actual assembly-store image, match its own linked input
exactly and verify both actual linked products identify14/source0427b9. The actual financial and
Brotli images remain equal and official native engine equality passes. Retain the first refusal.
This is not a relaxed same-image performance comparison: no performance comparison is performed.

## Changed-runtime original native compatibility

| Owned new source | Actual API/ABI | Native and managed pages | Original source run/PID |
| --- | --- | --- | --- |
| Minimum API |24/x64 |4096 |8e2fd8b22eeb40d19f4dd949e909f5af/2650|
| Simulated16KiB |35/x64 |16384 |0b5f9c56cec84eab959be18e155c5e4e/3835|

Each fresh independent UID creates exactly1031 fictitious original rows once. This is2062
fixture rows, not zero database writes or unit tests. Original complete typed values/order,
nonempty block reading, digest-valid malformed-compression scalar fallback, exact original cache
recovery and original native Home/Core balance all pass. These finite changed-runtime observations
are needed for the changed native component; old key/migration/financial cohorts are not replayed.
No owner Main database/key is read or replaced. Hardware16KiB and whole-platform acceptance are
not established by simulated page sizes or static ELF alignment.

After preserving both original reports, remove only the two new packages/fixtures. Prior package
paths/five settings/AVD configs are exact; both owned AVDs actually stop. No clock/system setting
changes, financial UI command replay or physical-phone command occurs during this batch.

## Cleanup, shipping selection and remaining gates

Four supported scoped Clean configurations remove all3425 known registered files/1157275260
logical bytes while preserving both used complete APKs exactly. Delete only the redundant,
SHA512-verified official download ZIP after extraction/use; retain the scoped14 test runtime for
future affected tests. No recursive folder deletion, total-cache cleanup or causal host-free-space
claim. Original used/restore APKs and local source/control/failure evidence remain.

D551's immutable earlier before packet still records historical896e. An explicit final selection
instead chooses50f5 after these security/runtime/package gates; only ignored host installer/
observer selection logic changes. Avoid an intermediate adoption of the now-superseded13 runtime.
No physical Main upgrade has yet occurred: Main4090/D542/Home979/all141 and restored timeout300000
remain under the separately pending original caller Play Protect installation. Never retry that
same installation from timeout. After owner action, complete original caller restoration first,
then the one signed no-reset Main upgrade with actual owned-foreground/data/identity/OS guards.

This component correction does not close general ANR/crash, two-second/cold/low-memory/spoken-
accessibility, real authentication, hardware16KiB, iOS/provider/Store/service-deployment or release
gates. Existing service/container runtimes and real physical adoption remain separately unverified.
