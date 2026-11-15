# Private MAUI Core product-asset admission (D-1183)

[D-1182](private-maui-density-configuration-d1182.md) qualified a private compiled
configuration-metrics correction in actual Android component callbacks. This step
determines how that library can enter the real product graph without silent
official-assembly fallback. It does not persistently change product dependencies,
publish an APK, install on a device, or close the product live-density boundary.

## Real graph failures and corrections

An explicit repository-owned local package, Vafadar.Maui.Core.Android, carries the
modified public framework library and its original MIT license/third-party notices.
Its notice identifies the modification and says it is not an official upstream
binary. An isolated extra target/config adds the package only to Android graphs;
canonical projects, central versions and NuGet configuration remain unchanged.
There is no vendor-package overwrite, global installation or credential input.

The first package omitted the DLL's Android AAR sidecars. Actual product-library
resource linking failed with APT2260/2061 for MauiAppBarLayout. A new immutable
package version includes Microsoft.Maui.aar and maui.aar next to the DLL. All17
and5 respective internal entry hashes match the official10.0.120 AARs; this is
entry-content equality, not whole ZIP-byte identity. Resource resolution then
passes in the real App/library graph.

The initial privately compiled library retains Arcade's development FileVersion
42.42.42.42424. Its public assembly identity already matches the official library:
Microsoft.Maui, Version10.0.0.0, neutral culture, null public-key token. An ordinary
FileVersion property did not replace the sentinel because the upstream version
target writes it later. A private after-initialization target explicitly sets
FileVersion10.0.120.1183 and informational version
10.0.120-vafadar.configurationmetrics.1+6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0.
It does not pretend to be an official build. Strict Core compilation has zero
warnings/errors. An initial external-output build retained16CS8669 errors despite
a copied editorconfig; the known source-root output boundary is required and is
used without editing generated C# or suppressing diagnostics.

A read-only metadata program compares the D-1182 accepted private Core with this
version-stamped Core:6407method definitions/1026types,5551method bodies including
local-signature/stack/exception metadata, and all public user-string mappings in
the64268byte heap agree. No library is executed and no IL/string contents are
exported. The final Core SHA256 is
`9cac5cb4409b52d06c92e08d6f908e7754b92cd641279360fbfc60000215d2d3`.
Both changed source documents also match the final PDB checksums. This comparison
supports only the version-stamping boundary; it does not prove full equivalence
to the vendor release, every runtime dependency or execution in the product.
The successful D-1182 native transitions are not repeated for this metadata change.

Lowering the file version exposed a real integration defect: ordinary conflict
resolution chose the official Core in all five Android compiler inputs, even with
PreferredPackages configured. That zero-exit product compile is explicitly refused
as private-Core acceptance. Do not rely on a high development version to win.

The final immutable admission package0.1.0-d1183.4 explicitly carries the official
Core direct dependency graph and both sidecars. The isolated Android target excludes
only the original Core compile/runtime assets while retaining its build assets;
the private library remains explicitly present. The package SHA256 is
`174e7629c67b763c2e2fa68aaae6193b07db167598f4b8ade221b46eda586860`.
Actual compiler reference paths in App, Vafadar.Maui, Documents.Maui,
Authentication.Maui and the existing notification source project each select the
single exact private DLL. Restored App targets net10.0-android, android-arm64 and
android-x64 expose only placeholders for the original Core compile/runtime assets
and the private DLL for both asset kinds. Strict actual product C#/source-generated
XAML compilation passes without warnings/errors against those inputs.

An earlier observer wrongly required a MAUI reference in non-Android projects and
in Android ReferenceCopyLocalPaths, where it is absent at that stage. Preserve the
refusal; use actual compiler references and restored per-target runtime assets.
Restored runtime selection is still not a packaged/linked/AOT runtime observation.
No product test suite, successful financial operation or native transition is replayed.

## First-use mistake and bounded restoration

One manual metadata-program command omitted the isolated environment and used the
wrong executable path. It did not run the comparison. SDK first-use output claimed
an HTTPS development certificate installation. The actual CurrentUser/My inventory
contains only the preexisting July2026 localhost development certificate, with no
fresh development certificate observed; no certificate/trust/key deletion or change
is performed. Three new zero-byte SDK sentinel files have exact creation times in
the command's23:28:54UTC second. After absolute-path/name/size/exact UTC-tick guards,
only those three files are removed. The first string/date guard and a diagnostic
syntax error refuse before mutation; corrected UTC-tick checks preserve ownership.
The dotnet tools path is already in both the parent process and persistent user
PATH; neither full PATH nor credentials are exported and no PATH write is performed.
No whole-registry or historical certificate-store equality is claimed.

The actual metadata comparison then runs with explicit isolated CLI/temp/cache
environment and first-use/certificate generation disabled. Its counts and digest
receipt, the failed invocation and bounded restoration metadata remain retained.

## Preserved scope and next delivery boundary

Used private Core/PDB/AARs, four immutable local packages, source/version target,
compiler references, full logs and failed admissions remain under
artifacts/d1183-maui-core-product-asset-admission. Four standard Cleans remove
19243files/415947440logicalbytes, leaving279files/26679108bytes in the four recorded
build-output roots. No supplementary/frozen cleanup or device/emulator action occurs.
Phone/Main, C7/Q02, original native-fault evidence and consumed one-shot diagnostics
remain untouched. The unrelated untracked benchmark file is not read or staged.

Before persistent adoption, retain a reproducible source/package preparation path
for the actual Windows/Linux build lanes, admit the broader private-versus-official
Core source/build boundary, add fail-closed compile/runtime selection guards, and
qualify the complete linked/AOT product and its affected live configuration flow.
Do not call this a shipped correction or substitute these compile receipts for APK,
layout, multi-display, stability, performance, security or release acceptance.
All broader parent gates and the full delivery goal remain open and active.

The sole D-1182 CI response records CI and CodeQL in_progress at response time;
their eventual conclusions are unknown here. No second request/job-log/rerun occurs.

Related: [original public conversion failure](public-density-conversion-transition.md).

[D1184](private-maui-reproducible-producer-d1184.md) adds the pinned source producer,
strict public-source/API boundary, actual compiler/linker digest refusal and a
complete isolated product APK with a bounded original configuration flow. Default
integration and broader density qualification remain open.
