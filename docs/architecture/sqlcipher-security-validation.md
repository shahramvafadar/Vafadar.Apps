# SQLCipher native security validation

Status: independent candidate validation only. No original app database is encrypted or migrated.
Source and build identities are in [the Community guide](sqlcipher-community-build.md).

## Scope and evidence

| Boundary | Current authoritative evidence | Still required |
|---|---|---|
| Official source/tool provenance | Pinned official SQLCipher/OpenSSL commits, portable host checksums and tool identities | Advisory/support review on each update; mobile toolchain inputs |
| Windows determinism | Two fresh DLL and NuGet hashes match; negative stale incremental evidence retained | Required mobile matrix and CI comparison |
| Original application schema | Actual compiled model, 32 migrations, original dummy identifiers/Unicode/EUR/money/date/settings reopen | Full populated profiles, relationships, durable pools, cross-platform migration |
| Key and integrity refusal | Missing/wrong key and altered page reject reads; plaintext header/marker absent | Corrupt WAL, crash, disk failure, loss/recovery, encrypted safety copies |
| Portable backup boundary | Encrypted snapshots work; plaintext BackupDatabase rejects; memory export feasibility passes | Final verified streaming/export/import without plaintext disk artifacts |
| Official OpenSSL suite | Test-UpstreamWindows.ps1 runs stock nmake test under an unprivileged account | SQLCipher upstream suite; mobile runtime suite |
| App activation | None: original app and D-179 signed APK unchanged | Final platform-native composition, keys and crash-safe copy/verify/switch |

## Preserved upstream warnings

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
