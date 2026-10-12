# Independent SQLCipher Android producer (D-183)

Status: local native candidate; not app activation or product release. Main app remains plaintext
and unchanged at D-179 (3,179 main tests). Upstream Windows OpenSSL result remains separately D-182.

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
