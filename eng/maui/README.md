# Android configuration-metrics source package

This private preparation lane builds the two reviewed MAUI Core source changes in
`eng/patches/maui-10.0.120-android-configuration-metrics.patch`. It does not change
installed SDKs, persistent environment, device state or runtime configuration.
MAUI Android consumers use the prepared package through the repository targets;
other platforms retain their original Core. Runtime qualification remains separate.

## Inputs and ownership

`configuration-metrics.inputs.json` pins the official source commit/archive,
original Core package, Android SDK/reference/manifest packages, JDK and public API
comparison tool by SHA256. The compiler is the repository SDK10.0.404 with its
runtime10.0.15, not the original upstream build host. The output is explicitly
versioned as a private modification; it is not an official upstream binary.

The preparation root must be under this repository's `artifacts` directory. An
existing Android36 platform, build-tools36.0.0 and platform-tools are prerequisites. Preparation
never installs an SDK or accepts terms. It uses anonymous official feeds and an
allowlisted child environment with owned CLI/Java/package/cache/temp directories.
Keep each platform's producer and NuGet package cache separate: equivalent private
source builds do not imply byte-identical Windows/Linux packages.

```powershell
./eng/maui/Ensure-ConfigurationMetrics.ps1 `
    -WorkRoot artifacts/maui-core `
    -DotNet <existing-dotnet-executable> `
    -AndroidSdk <existing-android-sdk>
```

`-InputSeeds` accepts a JSON object mapping input names to already verified
archives under repository-owned artifacts. `-NuGetFallback` accepts existing
package folders. Neither option bypasses the pinned input hashes. Do not point
either option at a protected or retired diagnostic scope.

Version3 preserves executable source-script modes and installs missing execute
permissions on actual Linux SDK host executables; no tool/script bytes change.
Both Windows and an isolated Linux producer have completed the pinned source and
strict public API checks. Linux generates LF instead of CRLF in exactly two AAR
text entries. Only that difference is admitted; all other entry bytes must match.
The package includes the complete original official AAR bytes on both hosts,
and retains the built AARs and their exact difference receipt. This does not
claim byte-identical assemblies or Android runtime acceptance on Linux.

Version4 pins the direct CLI SDK inside the verified source directory to
`10.0.404`, with roll-forward disabled and the selected installation's explicit
SDK search path. Upstream `tools` and `msbuild-sdks` declarations remain intact.
Checking the repository directory alone is insufficient: the upstream source
has its own `global.json`, whose `tools.dotnet` value is not a CLI SDK pin.
Preparation first refuses an executable installation missing the pinned SDK,
before any archive download, extraction or build. It then verifies actual SDK
selection from the source directory and retains original/prepared configuration
hashes. An existing policy whose bytes drifted is refused without implicit repair.
Version4 reuses unchanged successful source-build/API witnesses; its new package
identity does not imply another framework compilation or runtime qualification.
See [SDK selection evidence](../../src/Apps/Zanance/docs/quality/source-sdk-selection-d1191.md).

## Evidence and refusal behavior

The producer retains input hashes, source-archive inventory, exact patch
preconditions/results, command logs and native exit codes. Its compiled portable
PDB must bind all672 handwritten Core documents to the verified archive and two
allowed patch results, with706 total documents and the pinned compiler profile.
Private version stamps, the bounded AAR comparison and a diagnostic-free strict
public API comparison are required before packaging. Public API equality does not
prove behavioral equality or application runtime acceptance.

Successful preparation phases are reused only when their command and recorded
witness hashes still match. Failed attempts remain retained. A completed package
must retain its recipe and package hashes; the producer refuses to silently
rebuild or overwrite the same immutable version. The wrapper serializes ownership
and writes version-specific `selection.<version>.props` only from that validated
`package-proof.<version>.json` receipt. Earlier packages and selection files are
preserved; a version change does not rerun an unchanged successful source-build
phase whose command and witness hashes still match. No automatic
cleanup or deletion occurs.

## Explicit consumer qualification

`Directory.Build.targets` imports `ConfigurationMetrics.Android.targets` for
normal builds. Run preparation before a MAUI Android restore; do not add a
second explicit `CustomAfterMicrosoftCommonTargets` import. `NuGet.config` maps
`Vafadar.Maui.Core.Android` to the owned `artifacts/maui-core/feed`. The central
package version is declared in `Directory.Packages.props`.

`Build-AndroidApk.ps1` prepares or verifies that package before restore. CI and
release use the plain `AndroidDependencies.csproj` to install the existing SDK
prerequisites first, without a private Core dependency, then prepare Core before
restoring the actual app. This breaks the cold-feed bootstrap cycle. The
existing hosted SDK licence-acceptance policy is unchanged; the Core producer
itself never accepts terms. CI caches only complete pinned public input archives,
not successful phase logs, partial downloads or compiled Core output.

The consumer excludes the original Core compile/runtime assets, retains its
official dependency graph and resources, and adds the private Core package.
Before compilation and before trimmed/untrimmed Android linking, exactly one
actual Core assembly input must match the prepared SHA256. The compiler input
must also originate from the private package. An old cached original, missing
selection or mismatched assembly is an error, not a successful fallback.

See [D1184 evidence](../../src/Apps/Zanance/docs/quality/private-maui-reproducible-producer-d1184.md)
and [D1187 default integration evidence](../../src/Apps/Zanance/docs/quality/default-android-core-linux-producer-d1187.md)
for actual results, failed attempts and remaining product adoption boundaries.
