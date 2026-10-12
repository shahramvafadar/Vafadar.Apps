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
Sources for the compatibility build are extracted from those Git objects without local newline
conversion or source edits. This is HTTPS/commit/checksum provenance, not a claimed signature audit.

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

Next concrete implementation: production source/recipe restoration and Windows native packaging
with an actual EF/schema probe, followed by Android native Release proof. Key/recovery/migration
integration follows validated binaries. No app enforcement or temporary OS-backup policy is added.

Records: artifacts/native-security-build/windows-x64/{msys-negative.log,tool-restore-final.log,
amalgamate.log,build-native-perl.log,link-sqlcipher.log,capi-proof.json,dotnet-library-name-negative.log,
dotnet-proof-final.log,dotnet-backup-proof.log,dotnet-export-proof.log}.
