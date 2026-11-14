# Private Android configuration-metrics component qualification (D-1182)

The live-density defect recorded in [D-820](public-density-conversion-transition.md)
now has a compiled source correction and a finite original/corrected Android
component comparison. This does **not** adopt a private MAUI dependency in the
product, qualify all layouts, or change Main. The remaining delivery boundary is
reproducible product dependency adoption and affected running-product qualification.

## Bound source and correction

The current Microsoft.Maui.Core10.0.120 package names official source commit
`6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0`. Its complete official source archive is
728653584bytes, SHA256
`87ad2013288a6a51e10bd33cd538a575ab3450bca987986ed361393eb1097067`.
All26699 regular input files were extracted, including long-path image fixtures.
The initial ordinary Win32 extraction and a later ordinary-path inventory falsely
reported long-path failures/missing files. Extended-path reads confirm preservation;
these observer failures remain evidence, not deleted source or build acceptance.

The [reviewable source patch](../../../../../eng/patches/maui-10.0.120-android-configuration-metrics.patch)
adds an internal metrics refresh to ContextExtensions and invokes it from actual
MauiAppCompatActivity.OnConfigurationChanged after native base handling and before
the existing managed lifecycle callbacks. It refreshes the cached conversion density
and invalidates the three configuration-derived pixel-height caches. Existing hot
conversion getters are unchanged; there is no extra native read per conversion,
public API addition, reflection-based field reset or process-restart workaround.
Single active-activity configuration is the qualified scope, not multiple displays.

The original archive-file inventory differs only in those two executable source
files and isolated NuGet.config. An additional local Directory.Packages.props
sentinel prevents the enclosing product's central-package policy from changing
the upstream explicit-version build. Both source configurations are retained;
the product dependency/configuration remains unchanged. Each original and corrected
Core PDB independently binds both relevant source documents by exact SHA256.
This is source binding, not whole-PE identity or proof of every executed method.

## Strict source-build prerequisites and failures

Original feed source mapping and inherited central-package policy initially refused
evaluation/restore. Clear them only in the isolated source build, retain the original
configuration, and use the original anonymous official feeds without authentication.
The source declares Android SDK36.1.115 at dependency commit
`ee72c6ff89ce0b2609b21fc5b0897741cae8dc5c` and Java17.0.12. The installed SDK36.1.69
and Java21 were initially different. Download the declared official SDK/reference
pack/manifest and checksum-verified Microsoft OpenJDK archive into this task's
repository-owned directory only. No global install, setting or license-acceptance
operation occurs. Android source packs are MIT; the unmodified private Java build
tool retains its GPLv2/ClassPath and assembly notices. No Java runtime is linked or
redistributed in the application.

The first Core build failed with16CS8669 errors and three Java warnings. Matching
SDK/Java removed the Java warnings but did not remove CS8669; a fresh output root
also retained the errors. The enclosing product editorconfig classifies obj files
as generated, whereas the upstream editorconfig has its own root. External output
escaped that upstream boundary. Moving fresh output inside the upstream root
without changing generated source or warning policy gave the unmodified Core a
zero-warning/zero-error build. The two-file corrected Core then also built with
zero warnings/errors. No generated-file edit or diagnostic suppression hid failures.

## Actual callback comparison

Two fresh SDK-only callers use the original and corrected compiled Core respectively.
Each subclasses the actual MauiAppCompatActivity and retains a native editable draft.
These are not product shipping packages or a replay of the old D-820 caller.
The consumers use the complete installed SDK36.1.69 and established serviced
Mono/runtime/AOT10.0.15 selection; the library was compiled with its declared
SDK36.1.115. No generator or individual SDK tool was transplanted between versions.
This explicit consumer/library boundary is not a full product toolchain acceptance.

| Caller and phase | Native density | Public density | ToPixels(100) | FromPixels(100) |
| --- | ---: | ---: | ---: | ---: |
| Original before | 2.625 | 2.625 | 263 | 38.095238095238095 |
| Original changed | 1 | 2.625 | 263 | 38.095238095238095 |
| Original restored | 2.625 | 2.625 | 263 | 38.095238095238095 |
| Corrected before | 2.625 | 2.625 | 263 | 38.095238095238095 |
| Corrected changed | 1 | 1 | 100 | 100 |
| Corrected restored | 2.625 | 2.625 | 263 | 38.095238095238095 |

API36/x64/4096 originalPID4814/UID10216 and correctedPID5121/UID10217 each retain
one Activity creation, two actual native configuration callbacks and raw123.4500
through420dpi→160dpi→original absent-override420dpi. There is no relaunch/recreation
for the transition. These are six named observations, not six new product tests.
No account, database, financial Save, authentication, photograph or sharing occurs.

Each final signed offline nondebuggable caller has81AOT/91ELF/354static16KiBLOAD,
ZIPCRC/alignment and v2/v3 signatures. Original SHA256:
`5caedc28883b1829dea1f3b1b32a918e7c379160d65bdc00627e5c0c2852dd0d`;
corrected:
`bce2c5b5c537cd9e5c720cacaee580d605a0d88e31a6a1401ce9f55c0f673c30`.
Each is14887290bytes. Actual Mono SHA256 is the established serviced
`b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034`.

The initial caller omitted AppCompat compile dependencies through propagated asset
exclusion. Explicit exact dependency versions corrected it. Default Android globs
then admitted earlier output AARs as new binding inputs, producing real binding
errors; explicit Android items and nonbinding precompiled AAR inputs corrected the
caller. An unnecessary explicit ILLink task reference caused a warning and was
removed. Pre-install runtime admission also refused an unexpected Mono hash;
importing the existing serviced runtime/AOT selector corrected the omission before
either package was installed. No warning-bearing or runtime-refused build is accepted.
The first transport and early boot observations failed; the same emulator continued
without restart. Successful phase inputs were not repeated.

## Preservation and remaining boundary

Both fresh packages were removed only after their captured UIDs matched. Original
OS values, locale-key absence, size/density, boot and reverse are exact afterward;
there are no XML captures. The official fresh AVD retirement removed25files and
4196546081logicalbytes, leaving six7732byte user-home files. Used signed packages,
original/corrected compiled Core, source patch, source/compiler proofs, failures and
raw phase packets remain under artifacts/d1182-live-density-source-admission.
Seven standard Cleans remove24438files/630274723logicalbytes, leaving780files/
58976606bytes in the scoped build-output roots. Signed used copies and compiler
evidence were retained before Clean. Initial patch dry-run/application observers
failed; host Git autocrlf rewrote disposable C# outputs to CRLF while the original
archive and compiled proposal use LF. A byte-preserving diff applied with
command-local core.autocrlf=false and core.eol=lf reconstructs both compiled source
files exactly.
Its narrow Git attribute preserves the reviewed LF patch/context; no global Git
configuration is changed.
No supplementary deletion or frozen scope is attempted.

Phone/Main, retained C7/Q02 and consumed one-shot diagnostics remain untouched.
No product Windows/Android build, successful managed suite or financial operation
is repeated for this private component. Original fault cause/remedy, whole-app
performance/security/accessibility and external/platform/release gates remain open;
the full delivery goal remains active. This report does not claim the dependency
is an official released fix or that the product's live-density gap is closed.

The sole D-1181 CI response reports completed-success for CI, Local identity
backend, Self-hosted service qualification and CodeQL; no polling/job-log replay.
See [the CI run](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43645436552).

Official inputs: [bound MAUI source](https://github.com/dotnet/maui/tree/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0),
[Android runtime configuration handling](https://developer.android.com/guide/topics/resources/runtime-changes),
[Microsoft Java archive](https://learn.microsoft.com/en-us/java/openjdk/older-releases).
