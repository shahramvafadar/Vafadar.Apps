# Official SQLCipher Community build and compatibility (D-180)

Status: owner direction accepted; independent Windows C/.NET feasibility verified. Production native
build/packaging/update/security pipeline and app integration remain in progress. The app is unchanged
and its databases remain plaintext. Current complete APK/test/build baseline is D-179.

## Owner constraints

On 2026-10-12 the owner selected Community Edition from current official source. No custom
cryptography or unnecessary fork; review security/maintenance before using native or managed
dependencies. First prove architecture, platforms and long-term maintenance, then integrate final
security, migration and CI. Recovery OD-04 and final OS-backup OD-10 remain separate decisions;
no interim app policy or deprecated probe binary is introduced.

## Source and host-tool provenance

`eng/native/sqlcipher/inputs.lock.json` records exact official tags/commits, reviewed date and the
portable native Windows Perl URL/size/SHA-256. SQLCipher 4.19.0 commit c4b275a47932888216bade83aff2bbc73df0ff85
and OpenSSL 3.5.9 LTS commit 45e844fa2a14ec92d146bd8f5778ac130b6625fb match official checkouts.
Current source restoration extracts verified Git objects with explicit scoped LF conversion settings
and no source edits. D-183 found earlier Windows archives inherited global CRLF conversion; see
the Android build guide for the correction and preserved evidence. This is HTTPS/commit/checksum provenance, not a claimed signature audit.

[SQLCipher 4.19.0](https://github.com/sqlcipher/sqlcipher/releases/tag/v4.19.0) fixes export-alias and
hexkey issues. The 5.0 beta changes format/API and is not production input.
[OpenSSL's current release table](https://openssl-library.org/source/) identifies 3.5.9 LTS and
support through 2030-04-08. Upstream advisory review must recur before every adopted update.

`Restore-WindowsBuildTools.ps1` restores the officially published portable native Perl 5.42.3.1,
304,765,269 bytes, with locked SHA-256. It checks origin, size/hash, extraction paths/symlinks,
completed cache/executable identity and native interpreter version. Cache remains in owned artifacts;
there is no installer, global PATH or OS change. C locale is process-scoped and restored.
Fresh restoration and verified cache reuse passed in PowerShell 7. Windows PowerShell 5.1
syntax parses; actual execution is blocked by the existing host execution policy, which is unchanged. An initial PowerShell continuation parse error was
corrected before download; initial native identity emitted an MSYS locale warning, corrected by scoped C.

Git's MSYS Perl failed OpenSSL source-path discovery and is not the documented VC host. Native
Windows Perl follows upstream NOTES-PERL; no CPAN module substitution or source patch was used.
The local compatibility compiler is existing Visual Studio 2026 BuildTools 18.10.0, Windows x64.

## Actual independent observations

The stock source produced an amalgamation and a DLL with original codec/extra-init/shutdown hooks,
OpenSSL provider, threading, memory temp store, column metadata and FTS5. This first compatibility
build uses no-asm/no-tests OpenSSL options; it is not the final hardened/reproducible shipping recipe
and does not claim upstream OpenSSL/SQLCipher security-suite acceptance.

Native C API reports SQLCipher 4.19.0 community / SQLite 3.53.4. Correct-key reopen preserves
Int64.MinValue, ISO date and fictitious text. Wrong keys, unkeyed native access, plain SQLite access
and a modified page are rejected. The fictitious sentinel is absent from database/WAL files.
Expected HMAC/decryption errors from negative cases are retained, not failed success cases.
DLL SHA-256: 51970a13c7568c954b1ad7351e0f75706fcdf8b3f27dc3923ebd528b1b4fe615.

Actual Microsoft.Data.Sqlite.Core 10.0.12 and the maintained managed-only provider.sqlcipher 3.0.5
report OpenSSL 3.5.9 and preserve original Int64/currency/DateOnly values across close/reopen; missing
and wrong keys are rejected. The first config.e_sqlite3 binding fails Microsoft's library-name
encryption check; the supported named SQLCipher provider passes without patching either library.
Provider review uses the [official source](https://github.com/ericsink/SQLitePCL.raw/tree/v3.0.5),
commit ed046114d5a30534e13294d94d78eb73de896ad4. It supplies no native crypto binary.

| Actual Microsoft BackupDatabase target | Observed result | Integration implication |
|---|---|---|
| Same key | Original value copied | Encrypted snapshot path is feasible |
| Different key | Original value copied | Separate encrypted safety key is feasible |
| Plaintext | Rejected, SQLite error 1 | Current unkeyed portable snapshot path cannot simply be reused |
| sqlcipher_export to a plaintext memory alias | Original value copied | Evaluate serialization/streaming without a plaintext temporary database |

These are bounded dummy-record checks, not full-schema, crash, pool, EF compiled-model, mobile,
biometric/key, recovery, performance or product-release acceptance. No app database, credential,
security storage or owner archive was touched. Main 3,179 tests and D-179 strict builds/APK remain
the existing app baseline; no new main-suite count is invented for independent probes.

## Architecture changes required before activation

| Current source boundary | Required final change |
|---|---|
| Vafadar.Data.csproj bundles default native SQLite transitively | Separate EF/ADO Core dependencies from explicit app-specific native composition; prevent duplicate/fallback native libraries |
| LocalDbContextFactory caches keyless options by file path | Introduce a keyed connection boundary, actual profile/key identity and pool retirement; no key in diagnostics or portable preferences |
| SqliteDatabaseBackupSource opens unkeyed snapshots and restores | Cipher-aware encrypted safety copies and verified portable export/import; preserve complete schema/data and optional backup protection |
| LocalDatabasePools clears default file strings | Retire every relevant keyed pool before replacement/rekey/profile switch |
| App.CreateWindow migration and lock-covered startup | Resolve provider/key/migration under the existing startup cover; no database work during app construction |
| Main/profile path and catalogue selection | Inventory all original profiles, durable per-profile key envelopes and copy/verify/switch recovery; no guessed recreation after missing keys |

## Final native delivery process

1. Pin official sources, crypto provider and supported toolchains; retain licences/notices, source
   identities, package hashes and build flags. Do not use an unofficial old encryption bundle.
2. Build Windows x64 and Android arm64/x64; separately build iOS device/simulator slices on a verified
   Mac/CI host with a supported official crypto provider. Define ABI, minimum OS and Android page alignment.
3. Package only exact validated native slices and managed provider metadata; verify dependency closure,
   exports, architecture, hardening and runtime cipher/provider versions with no plaintext fallback.
4. Compare clean builds for reproducibility; run upstream encryption/crypto tests plus original EF
   schema/history, cross-key backup/export, Unicode/money/date, pool, tamper and wrong/missing-key cases.
5. Exercise key persistence/loss, migration interruption/disk failure, all profiles and encrypted safety
   copies on actual required platforms before main-app activation. Preserve the optional portable
   protection choice, existing access gate and offline permission policy.
6. CI restores locked inputs, builds/tests/package-verifies the native matrix and retains provenance.
   An update reviews upstream advisories/support/format changes, changes pins deliberately, rebuilds
   every slice and repeats compatibility/security/migration tests before adoption. Store publication
   and physical-device/iOS/provider acceptance remain external gates.

Next concrete implementation: Android native source-built slices and upstream security-suite/CI
work, followed by final key and crash-migration integration. Key/recovery/migration
integration follows validated binaries. No app enforcement or temporary OS-backup policy is added.

Records: artifacts/native-security-build/windows-x64/{msys-negative.log,tool-restore-final.log,
amalgamate.log,build-native-perl.log,link-sqlcipher.log,capi-proof.json,dotnet-library-name-negative.log,
dotnet-proof-final.log,dotnet-backup-proof.log,dotnet-export-proof.log}.

## Windows candidate producer (D-181)

The checked-in recipe builds official source with the locked existing MSVC 14.51.36231 / SDK
10.0.26100.0 and portable native Perl plus NASM 3.02. NASM official HTTPS archive size/hash are
reviewed; this is checksum provenance, not an asserted upstream signature. Assembly stays enabled.
OpenSSL uses static libcrypto, no module/config autoload and upstream no-filenames; original source
is not patched. SQLCipher retains its original codec/init/shutdown, memory temp store, foreign keys,
threading, metadata and FTS5. PE linking enables CFG, ASLR, NX, high entropy and CET compatibility.

Run from the repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File eng/native/sqlcipher/Test-NativeInputs.ps1
pwsh -NoProfile -File eng/native/sqlcipher/Build-Windows.ps1 -RunName windows-proof
pwsh -NoProfile -File eng/native/sqlcipher/Test-Windows.ps1 -RunName windows-proof
pwsh -NoProfile -File eng/native/sqlcipher/Pack-Windows.ps1 -RunName windows-proof
```

Use distinct run names for clean comparisons. Resume only an interrupted identical recipe with
`-Resume`; changes require fresh directories. Do not clean/reconfigure an old tree and infer a
clean build. Initial missing /LD caused a link failure; corrected before any candidate packaging.
Earlier incremental no-filenames changes left old OpenSSL folder strings and unequal hashes;
those intermediate artifacts remain negative evidence. No binary byte patching is used.

Eleven actual hostile origin/digest/archive traversal/ADS/symlink/size checks pass. The standalone
net10 harness compiles original Vafadar.Data and Zanance.Data sources with the real compiled model,
EF Core Sqlite.Core 10.0.12 and maintained managed-only named provider 3.0.5. It starts with first
migration/raw fictitious financial rows, runs all 32 original migrations and reopens original
identifiers, Unicode, EUR amount/date/settings. Missing/wrong key and altered page refuse reads;
plaintext header/marker are absent and original default foreign keys remain enabled. Expected
negative HMAC errors are retained. Only independently owned fictitious artifacts are opened.

Packaging requires a matching manifest, original-schema proof and native dependency inventory;
required original key/rekey/backup/serialize/deserialize exports are checked. It produces an
unpublished candidate NuGet with exact managed-provider dependency and required component notices,
stable entry order/time, native/input/compiler hashes and architecture/hardening checks. Owner
source rights stay separate from component licences. This package is not yet referenced by the app.

The main application and full D-179 signed APK remain unchanged; main 3,179 tests/builds retain their
existing evidence. Native Android/iOS, upstream crypto/encryption suites, key wrapping/loss/recovery,
all-profile crash-safe migration and CI matrix are still required before activation. The Windows
candidate and fictitious original-schema test do not encrypt any existing app database.

### Final clean Windows observations

Fresh windows-c/windows-d builds produce byte-identical DLLs and local NuGet packages. Both
original-source harness runs pass all 32 migrations and the bounded security/financial cases.
Export inventory and dependency closure pass: only KERNEL32, ADVAPI32, CRYPT32, WS2_32 and USER32.
Package CRC and embedded native hash verify. Native SHA-256:
3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.
NuGet SHA-256: c5cce68ef5904bb52dc81164493c7d8bf46306d6a51bcc136dedd8ffe3dfb6e3.

Each upstream native log contains 749 warning occurrences (including dependency-generation
passes): C4267 670, C4244 73, C4133 2, C4319 2, C4334 2. They are not suppressed, patched or
called a warning-free production build. These findings require upstream/security review before
adoption. The managed compatibility build passes ContinuousIntegrationBuild; main application
Windows/Android strict results remain the unchanged D-179 baseline. Upstream suites are still open.

Records: artifacts/native-sqlcipher/{input-validation-reviewed.log,clean-comparison.json,
build-windows-c.log,build-windows-d.log,test-windows-c.log,test-windows-d.log,pack-windows-c.log,
pack-windows-d.log}, plus build/windows-{c,d}/{build.log,exports.txt,dependents.txt,
candidate/manifest.json,compatibility-proof.json,package-proof.json}. Earlier windows-a/b unequal
incremental outputs and the failed relative inventory-command invocation remain negative evidence.

## D-182 upstream suite update

The matching Windows candidate passes the stock OpenSSL suite: 347 files/3,818 reported tests,
with 59 whole recipe skips and 114 skip markers retained. Native hash unchanged; this does not
accept every warning, SQLCipher/mobile/key/crash migration or app encryption.
See [security validation](sqlcipher-security-validation.md) and Test-UpstreamWindows.ps1.

## D-183 Android producer

Independent official-source Android arm64/x64 build, canonical archives, ELF/negative/package
checks and native x64 emulator proof pass. App remains plaintext/unchanged.
See [Android build guide](sqlcipher-android-build.md) for exact scope and remaining gates.

## D-184 stock-suite / CI update

Maintained official SQLCipher-specific fixture passes 226 tests with actual reviewed provider
identity and reported allocation cleanup. Initial separate Linux Native CI is configured but its
hosted run is not yet accepted. See [validation](sqlcipher-security-validation.md) for exact scope.

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

### D-258 — Bound Windows secure-heap list-index narrowing

The current unsuppressed Windows inventory contains749 OpenSSL occurrences, not direct SQLCipher
warnings.20 C4244/C4267 sites in official crypto/mem_sec.c narrow only list/slist indices into int
(or the sh_getlist return into int). Under valid allocator state their interval is0..59 on the
reviewed Windows x64 ABI; this is expression/range review, not whole allocator security acceptance.

SH_LIST has two x64 pointers, so effective power-of-two minsize is at least16. Accepted arena_size
is a representable positive power of two, at most2^63; minsize cannot exceed it, and sh_init rejects
bittables smaller than one byte. For arena=2^a and min=2^m, ratio=2^(a-m), bittable=2*ratio and
freelist_size=(a-m)+1. Thus k=a-m<=59. sh_malloc starts list at k, only decreases it, checks negative
list/slist before helper calls and splits back toward the bounded requested list. sh_getlist starts
at k and shifts an initial ratio<=bit<2*ratio until a valid allocated ancestor, returning0..k.
sh_free coalesces toward the root; its root buddy is bit0, never set by valid positive-node setbit
calls, so no valid coalescing step decrements below0. sh_actual_size consumes the same bounded return.
This reasoning assumes intact allocator metadata and valid pointers; it does not establish safety
for arbitrary corrupted pointers, pointer arithmetic generally or all OpenSSL entry points.

Review-WindowsSecureHeapRanges.py pins the exact official source SHA256, Windows/OpenSSL commit,
matching native DLL and all20 original occurrence lines/codes, then checks1711 admitted power-of-two
parameter classes. It refuses incomplete coverage rather than writing acceptance. It modifies no
source/binary/compiler flag or suppression and never opens product storage. Positive proof:
artifacts/native-sqlcipher/d258-accepted-secure-heap-ranges.json. Incomplete inventory is refused with
no output acceptance file. The earlier conservative-class artifact is retained; the final calculation
excludes tiny rejected bittables. Source hash:
2406f86ebc633aa7cdf27ab9617a8a49b6ec238e6d9dd3792b749cf7257e5d5e.
Native hash remains3c842f12049684bc854d6e89bdda207f80b768b43ccdf27f7760ce8f303eedbf.

This bounds20 narrowing occurrences only:723 C4244/C4267 remain unreviewed in addition to the6
previously reviewed atomic/shift occurrences. All original749 compiler diagnostics remain present;
no warning-free native build, full Windows adoption or main encryption is claimed. No code/UI/APK,
main suite or unchanged native producer suite was repeated. Main remains D-256/plaintext with its
3482-test baseline. Full security/OS/platform/device/release and original goal scope remain open.

### D-259 — Triage legacy AES conversion sites without blanket approval

All16 C4267 occurrences in official OpenSSL crypto/evp/e_aes.c belong to GCM/TLS, CCM/TLS,
AES Wrap or OCB handlers (lines2960,3007,3164,3565,3573,3606,3612,3631,3641,3721,3731,3738,
4025,4041,4086,4093). The exact SQLCipher OpenSSL source chooses EVP_aes_256_cbc and calls
EVP_CipherInit_ex/Update/Final; these warning lines are not its CBC handler. That direct binding
observation is not proof that all compiled OpenSSL entry points are unreachable or safe.

Do not approve these conversions merely because EVP update inputs use int. The legacy padded-Wrap
output-length expression at3731 rounds input then adds8. At input2147483640 (itself within signed
int range), ((inlen+7)/8)*8+8 is2147483648, outside Windows int. This is a source-expression
counterexample to a blanket range proof, not a reproduced public-API vulnerability or exploit:
actual provider dispatch, output-query rules, error propagation and contextual bounds still matter.
EVP's ordinary block-update overflow guard is after the custom-cipher branch, so that later guard
cannot by itself qualify all custom handlers. OCB partial-buffer additions have local block-size
bounds, but this slice does not separately close them or infer whole handler safety.

Immutable source hashes and all16 original occurrences are captured in
artifacts/native-sqlcipher/d259-aes-warning-triage.json. e_aes.c hash:
02b2fdbd8b105c55ac78805806c027f60fe5d42603a706fc5741d177a8f3ae40;
SQLCipher provider hash:a429d5466fa3fe7bb7608c3f79e8e19ff5706b78f6251b44530171dc30e53f69.
No upstream patch, warning suppression, binary rebuild, large-buffer allocation or user-data access
was made. All16 remain unapproved inside the723 remaining narrowing occurrences. Further adoption
must qualify actual reachable/provider input constraints or an officially supported reduced build
profile, not equate unused-at-the-direct-call-site with full native acceptance. Windows/main encryption
remain unactivated. Main code/APK and3482-test baseline remain D-256; unchanged producer, UI, main
build/test/APK checks were not repeated. Full goal/security/platform/release scope remains open.
