# SQLCipher native security validation

Status (2026-10-15, D-287): official Community encryption is active in the main Android/Windows
application (D-263/D-264). Native producers remain independently reproducible, but their initial
probe state is not the current app state. The current local engineering evidence is D-286; iOS,
physical-device/OEM/low-memory acceptance and product release remain unfinished.
Source/build identities are in [the Community guide](sqlcipher-community-build.md).

## Current scope and evidence

| Boundary | Qualified local evidence | Still required |
|---|---|---|
| Official source/tool provenance | Pinned official sources, reviewed tool identities, immutable baseline/servicing Windows packages and Android package | Advisory/support review on each update; exact required mobile matrix |
| Native determinism and CI | Matching clean producer outputs; earlier successful hosted Native cipher candidates run 38707538867 | Each changed recipe/consumer still requires its own hosted result; no current-head green claim here |
| Main data and lifecycle | Android/Windows protected startup, all-profile migration, original EF model, profile creation/switch/retirement and cold reopen | Physical device, iOS and broader OS/fault acceptance |
| Key loss and integrity | Native device-key storage, missing/wrong-key refusal, optional recovery and explicit Zanance restore | Hardware-backed phone assurance, iOS and real power-loss acceptance |
| Portable and local copies | Protected memory export/validated restore, encrypted safety and automatic local copies with independent retention (D-281) | Physical-device/low-memory/iOS acceptance; real OAuth backup clients |
| Official suites | Recorded unprivileged Windows OpenSSL baseline and Linux SQLCipher upstream suite; skip counts retained in each dated record | Never infer all APIs/ABIs or all upstream warnings are accepted |
| Main Home | D-286 original native reference/tenfold publication, complete typed-row preservation and focused cache/lifetime cases | Loaded phone/Android/iOS, complete cold startup and ANR acceptance |

The recorded current-profile Windows baseline reports 704 upstream occurrences (628 C4267, 72 C4244, two C4319 and
two C4133). They remain recorded, not suppressed or blanket-approved. Earlier 749-occurrence and
partial source-range reviews below concern their dated profiles; do not transfer them to every
current site. Main managed strict builds being warning-free does not resolve native warning review.

## Historical producer warning evidence (D-181)

### Original 749-occurrence inventory

The fresh Windows native producer logs each contain 749 occurrences, including compiler dependency
passes: C4267 670, C4244 73, C4133 2, C4319 2 and C4334 2. No blanket suppression or upstream patch
was applied. They remain a production-adoption concern; ordinary app strict-build results are separate.

The two C4133 occurrences originate at crypto/objects/obj_dat.c:238 and crypto/property/property.c:856.
Both use include/internal/tsan_assist.h's stock sizeof-based conditional selecting the 64-bit or
32-bit InterlockedExchangeAdd intrinsic. Their source arguments are int/uint32_t, so the 64-bit
branch is not selected by that constant condition. This explains the type-diagnostic origin;
it is not a comprehensive race/memory/security acceptance claim.

C4334 originates at include/internal/quic_vlint.h:99: the source mask limits the shift count to
0..3, with values 1,2,4,8 before widening to size_t. C4319 originates at crypto/bn/bn_gcd.c:658/670,
where the source uses a complemented unsigned-int mask combined with the low bit of a big-number
limb. Retain those findings and official big-number/QUIC suite results; do not classify every narrowing
warning as safe from this bounded inspection. The complete warning logs remain available.

## Update and activation discipline

An adopted update must deliberately change official pins, review published advisories/support and
format/API changes, rebuild all required slices, compare clean hashes and execute upstream plus
original-schema/key/integrity/backup/pool/crash tests. Never count skipped recipes as exercised,
build success as runtime evidence, or a dummy probe as full-app encryption. Keep candidate notices,
compiler/input/native hashes and negative evidence. Do not change the owner's OS or credentials.

## D-182 actual matching Windows result

`pwsh -NoProfile -File eng/native/sqlcipher/Test-UpstreamWindows.ps1 -RunName windows-d`
completed with exit 0 and stock Result: PASS. Root harness reports 347 files and 3,818 tests,
777 wallclock seconds for the test phase (building all test programs is additional). It reports
59 wholly skipped recipes and 114 skip markers; counts include configured skips, not only
exercised assertions. FIPS/shared/configuration/platform/dynamic-engine/fuzz exclusions remain
explicit. There is no FIPS claim, fuzz-corpus acceptance or mobile inference.

Native SHA-256 stays 3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
Records: artifacts/native-sqlcipher/build/windows-d/upstream-0861b2b605bf479985cccd49900be834.{log,json}
and artifacts/native-sqlcipher/openssl-suite-summary.json. Main 3,179 tests and D-179 APK are unchanged.

For subsequent Android work, independent Linux host restoration verified official NDK r30
30.0.16248370/Clang 21.0.0. Official HTTPS archive size 738,633,529 and published SHA1 matched;
computed SHA-256 is 753611f410d002cfcd3f3dc2ef49aad532089d3180b436c060a90bf0fcb64df2.
This is tool provenance, not an Android native build/runtime proof. Ubuntu image's existing UID
1000 blocked initial host creation; dedicated UID 10001 corrected it using cached inputs. No
installed SDK, OS settings or unrelated containers/services changed. Host/Android producer pins,
reproducibility and required native slices are next; the helper is not a shipped crypto dependency.
Official current input table: https://developer.android.com/ndk/downloads.

## D-183 independent Android scope

Official r30 native arm64/x64 outputs match across clean builds, with actual ELF/negative checks.
Native x64 API36/4 KiB emulator key/tamper/money/date proof and owned cleanup pass. Existing app
remains unchanged/plaintext; managed schema, arm64/API24/16 KiB/iOS/key/migration gates remain.
See [Android guide](sqlcipher-android-build.md).

## D-184 actual stock SQLCipher suite and initial hosted workflow

`pwsh -NoProfile -File eng/native/sqlcipher/Test-UpstreamSqlCipher.ps1 -RunName sqlcipher-upstream-c`
completed successfully with 226 tests/zero errors and upstream's reported no allocations leaked.
Fixture compiles official source with original codec/init/shutdown, temp-memory/FK/metadata/FTS
options plus SQLCIPHER_TEST hooks. Runtime identity is 4.19.0 community/OpenSSL 3.5.9; dependency
inventory includes Tcl/zlib/math/libc, with no system libcrypto/libssl fallback. This is Linux fixture
scope, not the full SQLite suite or final Android/iOS/managed/key/crash migration acceptance.

Initial sqlcipher-upstream-a passed the stock suite. An additional identity check in b failed before
a key existed; querying provider identity then is invalid. Corrected independent fictitious-key
initialization in c passes both native identity and the stock suite. Keep b's negative identity log.
Fixtures/evidence stay in owned artifacts/containers; no application database or key is opened.

The separate .github/workflows/native-cipher.yml initially covers Ubuntu24.04 restoration, two clean
Android builds/hash comparison, malformed ELF refusal and stock SQLCipher suite, with candidate/
evidence upload only. It contains no secret injection, package publication or product deployment.
YAML and job structure parse; maintained commands pass locally. Hosted execution is not yet verified,
and Windows/iOS native matrix plus same-candidate mobile runtime/packaging remains to complete.
The portable free-space preflight uses filesystem DriveInfo rather than assuming a Windows drive
letter. Eleven input-restoration tests pass; no unchanged app/test/build count is rerun or invented.

Records: artifacts/native-sqlcipher/{upstream-sqlcipher-a.log,upstream-sqlcipher-b.log,
upstream-sqlcipher-c.log,input-validation-d184.log}, sqlcipher-upstream-a/* and
build/sqlcipher-upstream-{b,c}/evidence/{identity.log,fixture-dependencies.txt,sqlcipher-test.log,
upstream-proof.json}. Main app and D-179 APK remain unchanged/plaintext, main suite baseline 3,179.

## D-185 actual managed Android original schema

Compatibility/SchemaCompatibility.cs holds the existing actual source/model/migration probe once.
Windows caller and Compatibility.Android invoke it; Android package is distinct
pro.vafadar.zanance.nativecompatibility, independent private files only, with no app references.

```powershell
pwsh -NoProfile -File eng/native/sqlcipher/Test-ManagedAndroid.ps1 -RunName android-d
```

Complete signed Release APK retains AOT/trimming and both exact native candidates; no default
SQLite bundle or INTERNET permission. Builds report zero warnings/errors. Initial probe's missing
actual AddZananceData registration caused a Mono compiled-model initialization deadlock, already
addressed by app D-44. Scoped stage and source evidence identify it; use original registration
before loading the model, without resolving its keyless factory or patching generated code.

Actual x64 API36/4 KiB run passes original compiled model/all 32 migrations, Unicode/identifiers/
money/EUR/date/settings, default foreign keys and wrong/missing-key/changed-page refusal. Shared
Windows caller passes after refactoring/composition. Maintained Android runner passes and stops
only the independent package; that package was uninstalled after evidence capture. No owner data,
main profile/security storage or main package was opened/changed. API24/16 KiB/arm64/iOS and final
whole-app keys/recovery/crash migration are not accepted by this probe.

Final independent APK: 29,392,143 bytes; SHA-256
f5886eadfc4fc3a718724d80cab4b4d8bd62d1d6cce6e6293f92749f84568cac.
Signature v2/v3, ZIP CRC, both native hashes and complete payload verified. This is a probe APK,
not a new user-facing Zanance phone-test handoff; main signed APK remains D-179/main suite 3,179.

Records: artifacts/native-sqlcipher/{android-managed-build.log,android-managed-diagnostic-build.log,
android-managed-composed-build.log,android-managed-own-process.log,android-managed-own-process-latest.log,
android-managed-composed-result.json,android-managed-final-apk-proof.json,windows-composed-shared-schema.log,
test-managed-android-d.log} and build/android-d/managed-android-proof.json. Initial negative runs/
scoped stage files are preserved as evidence; successful report does not erase them.

## Native CI failure diagnosis (D-190)

Actual job/artifact review identifies output ownership refusal before any native compilation.
Non-root host identity/fresh work binds correct this without weakening reviewed host/source/ELF
or suite gates. Six focused Docker workspace regressions pass; previous upstream suites are not
rerun locally. A complete corrected hosted build/suite result is still pending; local filesystem
contract success is not a substitute for hosted/native/mobile/encryption acceptance.

## Native parent traversal regression (D-191)

Old-image foreign UID fails actual work mount traversal; mode 750 is observed, not inferred.
Only immutable parent traversal changes to 0755; parent writes stay refused, work/output owner,
read-only sources/recipe and network isolation remain enforced. Seven cases and exact host review
pass. Actual hosted
[38363100746](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38363100746) completed successfully for 135d993 on 2026-10-13: both clean
ABI builds match reviewed bytes, all twelve ELF malformed cases are refused, official cipher
suite reports 226 tests/zero errors/no leaks. Artifact digest and actual matching libraries
independently checked. No main app activation or other native/device/recovery acceptance follows.

### D-221 — Review Windows upstream atomic-width warnings

The two C4133 diagnostics are in unchanged OpenSSL tsan_add calls for 32-bit new_nid and
global_seed. The MSVC macro type-checks both arms of a sizeof-based conditional; its unselected
64-bit arm causes the pointer diagnostic. Exact original obj_dat.obj disassembly uses lock xadd
dword ptr at new_nid. Exact property.obj uses lock add dword ptr at global_seed because its fetch
result is unused. Both original operations are 32-bit; no upstream source patch or warning suppression
was added. This is compiler/source evidence, not a claim that incompatible pointer warnings are
universally harmless. See the official [intrinsic signatures](https://learn.microsoft.com/en-us/cpp/intrinsics/interlockedexchangeadd-intrinsic-functions?view=msvc-170).

Test-WindowsAtomicWidth.ps1 verifies the exact header against the pinned official Git object, the
unchanged candidate DLL hash and pinned MSVC/SDK. A compiled upstream-macro probe exercises -1 + 1
with an adjacent sentinel (an incorrect 64-bit addition would carry into it), 64-bit values above
2^32 and 800,000 atomic additions across four threads. Probe disassembly separately verifies DWORD
and QWORD operations. The first verifier expected xadd for the unused global_seed result and failed;
inspection showed the valid lock add optimization. Corrected only that matching rule, then the related
probe passed. No unrelated native suite, main build/tests/UI or APK was repeated.

Static range review also bounds two C4334 diagnostics: the QUIC first-byte shift is 0..3 and its
result 1..8 fits 32 bits before widening. The two C4319 diagnostics complement a shifted unsigned
32-bit bit value, then intersect it with a final bit-0 mask; upper extension cannot affect that
condition. These are expression-level reviews, not independent acceptance of BN/QUIC algorithms.
The remaining 743 C4244/C4267 narrowing diagnostics still need review; the Windows native candidate
is not yet adopted or declared warning-free. Main encryption and the full recovery/platform scope
remain unfinished. Main tests (3,343) and signed APK are unchanged from D-220.

Proof: artifacts/native-sqlcipher/build/windows-d/atomic-width-4c3a49b4d702474aab6fc39a35172841/proof.json;
first verifier evidence retained in atomic-width-77cc4e16c9904d2ca91fda28fa00f259.
Full 749-occurrence inventory: artifacts/native-sqlcipher/d221-warning-inventory.json.
Native hash remains 3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
Review records source-header and both original-object hashes; the harness parser passes.


### Shared Windows qualification (D-270)

Run `./eng/native/sqlcipher/Test-WindowsToolchain.ps1` before native preparation. Build-Windows,
Test-UpstreamWindows and Test-WindowsAtomicWidth use WindowsToolchain.psm1 to qualify the same
installer allowlist, pinned MSVC/SDK and actual compiler SHA256. CI applies the check even on a
native dependency cache hit. Eleven policy cases and the current candidate atomic-width check
passed locally; the second installer patch was metadata-only, not installed-host evidence.
Unchanged upstream warnings remain open. App builds/UI/tests/APK were not repeated.
