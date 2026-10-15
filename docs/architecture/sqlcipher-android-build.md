# Independent SQLCipher Android producer (D-183)

Status (2026-10-15, D-287): this source-built Android runtime is used by the main app (D-263).
D-286 is the current locally verified app/APK baseline. ARM64 phone/API 24/16 KiB OS, low-memory,
OEM transfer, iOS and product release remain separate gates. D-183 and later dated producer/probe
records below retain their historical scope; they do not override main-app integration.

## Inputs and repeatable commands

Original source commits come from inputs.lock.json; android.inputs.lock.json pins official NDK r30
30.0.16248370, archive SHA-256/size and reviewed Linux host versions. The build image is independent,
non-elevated for compilation, and sees only public source archives/recipe/owned outputs. Downloads
are build-tool restoration inside the image, without installed SDK/global machine changes. Ubuntu
base digest and NDK archive identity are fixed; exact runtime package/compiler checks refuse host
version drift, so update pins deliberately. This is not an assertion that rolling apt restoration
will always resolve the same dependencies indefinitely; long-term CI restoration remains to verify.

Run from the repository root in PowerShell 7 with the existing container engine:

```powershell
pwsh -NoProfile -File eng/native/sqlcipher/Build-Android.ps1 -RunName android-proof
pwsh -NoProfile -File eng/native/sqlcipher/Test-Android.ps1 -RunName android-proof
pwsh -NoProfile -File eng/native/sqlcipher/Pack-Android.ps1 -RunName android-proof
```

Device default is the existing private x64 emulator/ADB port; the runtime script reads its ABI,
uses only a fresh UUID path below /data/local/tmp/vafadar-sqlcipher-, verifies transferred bytes,
and removes that exact independently owned directory. No app data/settings/keys or system settings
are opened or changed. It does not install an app or establish managed MAUI/provider composition.
No phone test is requested without a complete signed APK. Main APK remains D-179.

Canonical archive restoration uses process-scoped core.autocrlf=false/core.eol=lf. Earlier Windows
archives inherited global CRLF conversion; Linux Configure failed on the resulting shebang. The
original SQLCipher/OpenSSL Configure files now exactly match their Git blobs. Initial shell CRLF,
local preparation TypeError and API macro-redefinition findings are retained. No source patch or
binary byte normalization is used. Explicit -U__ANDROID_API__ before the requested definition
avoids Clang 21's built-in alias redefinition; compiler target remains Android API24.

Assembly-enabled static OpenSSL and original codec/init/shutdown/foreign-key/temp-memory/threading/
metadata/FTS5 options produce libsqlcipher.so for arm64-v8a/x86_64. Hardening includes stack protection,
RELRO/NOW, non-executable stack, hidden static crypto, system-only dependencies and 16 KiB segments.
Actual ELF checks and required key/rekey/backup/serialize/deserialize exports are validated; six
malformed candidates per ABI are refused without loading/executing their bytes. NDK compiler-runtime
component notices accompany SQLCipher/OpenSSL notices; owner-written source stays all rights reserved.

## Actual observations

Two clean native runs (android-c and final producer android-d) have identical bytes per ABI:

| ABI | Bytes | SHA-256 |
|---|---:|---|
| arm64-v8a | 7,212,472 | abc2c51b7b284672d8f6e86249847b962997d245720b50273531a1f85dbf1f56 |
| x86_64 | 7,536,888 | 9579006cdaca361daba6b08cf5228319d605a60c84bfc33e6a1287a06e702644 |

The actual x64 emulator is API36 with 4,096-byte pages. Official native cipher/provider versions,
foreign keys, correct-key reopen/Int64.MinValue/EUR/date, wrong/missing key, changed page and missing
plaintext header checks pass. Both manual independent run and the maintained runner pass; actual
deployed library hash matches, exact owned fixtures removed. This is not arm64, API24 or 16 KiB OS
runtime acceptance. C/Clang compilation reports no warnings; each llvm-ar log retains its expected
new-archive creation notification. Nothing is blanket-suppressed.

Repeated package generations from identical final candidate inputs match SHA-256
6d22d00abe2fbd38201d244a2d231d6d990622d767b97ac1bbe2b1332a0f0733.
Stable ZIP order/epoch, CRC and both embedded native hashes verify. This is identical-input package
determinism, not a cross-host NuGet/runtime acceptance claim. Packages remain unpublished and are
not referenced by the app. Twelve hostile ELF cases and eleven input-restoration cases are separate
from unchanged main 3,179 tests; do not add them to that suite's count.

Records: artifacts/native-sqlcipher/{android-a.log,android-a-final.log,android-b.log,android-c.log,
build-android-d.log,test-android-d.log,android-clean-comparison.json,elf-negative-x64.json,
elf-negative-arm64.json,elf-negative-final-x64.json,input-validation-d183.log}, plus
build/android-d/{host-proof.json,runtime-proof.json,candidate/manifest.json,candidate/*/elf-proof.json}.

Next: SQLCipher upstream suite, native CI/iOS restoration and actual managed platform schema proof,
then final platform keys/recovery/crash-safe migration. Physical/mobile minimum/API/page-size gates,
owner recovery and OS-backup decisions remain. Existing app data is not yet encrypted.

The package uses the standard NuGet ID.version.nupkg filename under the owned run
packages/<PackageName> directory; final standard-name generation matches the same package hash.

## D-184 hosted build preparation

Separate native-cipher workflow restores the locked Linux build image and exercises clean Android
outputs, negative ELF cases and stock SQLCipher tests. Local commands/stock suite are verified;
hosted execution and same-candidate runtime/packaging plus Windows/iOS matrix remain open.

## D-185 managed runtime scope

Separate signed Release AOT/trimmed Android probe passes the shared actual original-schema
32-migration/compiled-model/key/tamper proof on x64. Main app unchanged/plaintext; see
[security validation](sqlcipher-security-validation.md) for exact artifacts and remaining gates.

## Hosted Linux bind ownership (D-190)

Run 38318994276 failed before compilation: /output/host-proof.json was not writable by fixed
container UID 10001. Its host-proof confirms reviewed versions; do not replace source/tool pins.
Get-NativeContainerRunArguments now shares host non-root UID/GID on Linux and binds fresh owned
work at /home/nativebuild/work (Android) or /home/nativebuild/upstream (stock suite). Docker Desktop
keeps its existing nativebuild user. Sources/recipe remain read-only, network disabled; no chmod
777 or root compilation. Failed build tails are printed while original files remain artifacts.

Test-ContainerWorkspace.ps1 uses one freshly named, exactly removed Linux fixture volume for
actual negative/positive permission checks, plus both original argument builders and ownership
boundary refusals. All six pass locally; it now runs in hosted CI before compilation. No native
version/flags or app/API/UI/APK change. Full corrected hosted native result remains pending.

## Private build-home traversal (D-191)

The D-190 hosted regression exposed a second ownership boundary: /home/nativebuild is 750/10001.
The matched runner UID cannot traverse it to the otherwise owned work mount. Docker Desktop's
default image UID had hidden this issue locally. A foreign-UID actual-mount case now reproduces
old-image failure; Dockerfile grants only 0755 parent traversal and preserves foreign write refusal.
All seven focused checks and exact reviewed host identity pass. Full hosted
[38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) succeeded on 2026-10-13. Both ABI files from clean builds
a/b are byte-identical and match the original reviewed native hashes above. Twelve ELF negative
cases and 226 official cipher tests pass. Artifact 11795943134 SHA256:
267c33e4d6a3ff211ada4f0472c4239cf0306c21c3699cff8b1da06abc5caec2.
Local copy: artifacts/native-sqlcipher/d191-hosted-success.zip. This closes hosted Linux native
build/suite evidence, not app key/recovery/migration or other platform/device acceptance.

### D-257 — Qualify source-built Android package consumption

Android candidate packaging now includes framework-specific buildTransitive assets for bundled
component notices and source provenance. Native slices remain standard runtimes/android-arm64/native
and runtimes/android-x64/native assets; the Android SDK imports these itself. The actual production-
assembly caller now references Vafadar.Native.SqlCipher.Android rather than loose build-directory
AndroidNativeLibrary items. Package version4.19.0-candidate.3 is centrally pinned; managed bridge3.0.5
and official source-built native bytes remain unchanged. Candidate status is not changed to release
acceptance. No package is published to a remote feed.

The repeatable caller packs into a fresh owned output, populates an immutable local feed only when
its version/hash matches, and restores with explicitly named native-package.nuget.config. Exact
source mapping confines this package name to the local feed; other dependencies use the established
public source. The config's explicit name avoids changing implicit restore configuration for other
native/repository projects. Binary builds additionally require TreatWarningsAsErrors; APK inspection
requires both original ABI hashes, packaged notices/provenance and no default e_sqlite3 leakage.

The first edit damaged a LF project's closing XML while removing CRLF-counted text; restore refused
before native execution. The exact project was repaired, retaining d257-package-consumer.log. Candidate.2
then restored and ran correctly but explicit AndroidNativeLibrary items duplicated SDK runtime assets,
producing two XA4301 warnings. Original runtime/build evidence is retained, not clean-build acceptance.
Those duplicate items were removed from package targets and immutable version bumped to candidate.3;
no warning was suppressed or package cache replaced.

Final strict signed Release AOT/trim caller build has zero warnings/errors (50.72s). Only the cold
phase of the same fictitious run331ce8fb80634664ab4e32ae4e716d84 continues through the new package;
creation/database/native keys were not reset. Actual production persistence assemblies, 32 migrations,
negative money, binary portable snapshot and wrong-key refusal pass. Proof:
artifacts/native-sqlcipher/build/android-d/binary-runtime-proof-b1c72b1eee9241c7bc79453ed129afae.json.
Earlier creation/runtime proof remains binary-runtime-proof-8999f5611cc04175a5e8ef4d3892ab5f.json.

Two packages of the final version are byte-identical, hash
a31d841fe87e14ed93d678b8d0b94baa72dffabfd5dae17949192416427aa1d8; both native slices exactly match
reviewed candidate files. Explicit renamed-config restore and script parsers pass. The actual main
asset graph contains the standard bundle and no Vafadar native cipher package. Main source/UI/engine/
APK remain D-256, with its3482-test baseline; no unrelated tests/main builds/APK or unchanged producer
suite was repeated. This qualifies package consumption on observed Android x64/API36/4KiB only;
arm64/API24/16KiB OS/iOS, native warning review, main registration/recovery/retention/OS/device/release
acceptance remain open. The full goal and SEC scope are not complete.
