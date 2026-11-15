# Android configuration-metrics source package

This private preparation lane builds the two reviewed MAUI Core source changes in
`eng/patches/maui-10.0.120-android-configuration-metrics.patch`. It does not change
the default product graph, installed SDKs, environment, device state or runtime
configuration. Product activation and runtime qualification remain separate.

## Inputs and ownership

`configuration-metrics.inputs.json` pins the official source commit/archive,
original Core package, Android SDK/reference/manifest packages, JDK and public API
comparison tool by SHA256. The compiler is the repository SDK10.0.404 with its
runtime10.0.15, not the original upstream build host. The output is explicitly
versioned as a private modification; it is not an official upstream binary.

The preparation root must be under this repository's `artifacts` directory. An
existing Android36 platform and build-tools36.0.0 are prerequisites. Preparation
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

The Windows producer has been executed and qualified in D1184. Linux input hashes
are admitted and a Linux implementation is present, but no Linux producer run is
qualified yet. Do not activate this lane in Linux CI on the basis of Windows
evidence alone.

## Evidence and refusal behavior

The producer retains input hashes, source-archive inventory, exact patch
preconditions/results, command logs and native exit codes. Its compiled portable
PDB must bind all672 handwritten Core documents to the verified archive and two
allowed patch results, with706 total documents and the pinned compiler profile.
Private version stamps, unchanged AAR entry contents and a diagnostic-free strict
public API comparison are required before packaging. Public API equality does not
prove behavioral equality or application runtime acceptance.

Successful preparation phases are reused only when their command and recorded
witness hashes still match. Failed attempts remain retained. A completed package
must retain its recipe and package hashes; the producer refuses to silently
rebuild or overwrite the same immutable version. The wrapper serializes ownership
and writes `selection.props` only from that validated package receipt. No automatic
cleanup or deletion occurs.

## Explicit consumer qualification

`ConfigurationMetrics.Android.targets` is deliberately not imported by the
default repository build yet. A qualification build can import it through
`CustomAfterMicrosoftCommonTargets` and set `VafadarMauiCoreStateRoot` to the
prepared root. Its restore configuration must map `Vafadar.Maui.Core.Android` to
that root's `feed`, retaining the ordinary reviewed feeds for other dependencies.
The central package version is declared in `Directory.Packages.props`.

The consumer excludes the original Core compile/runtime assets, retains its
official dependency graph and resources, and adds the private Core package.
Before compilation and before trimmed/untrimmed Android linking, exactly one
actual Core assembly input must match the prepared SHA256. The compiler input
must also originate from the private package. An old cached original, missing
selection or mismatched assembly is an error, not a successful fallback.

See [D1184 evidence](../../src/Apps/Zanance/docs/quality/private-maui-reproducible-producer-d1184.md)
for actual results, failed attempts and remaining product adoption boundaries.
