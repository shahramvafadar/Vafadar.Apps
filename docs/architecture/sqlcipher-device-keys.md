# Device-protected database keys (D-192)

OD-04 and OD-10 are approved: automatic local key, optional separate recovery key and Zanance
backup recovery; permanent Android/iOS sensitive-data backup/transfer exclusion coupled with
completed encryption. Portable backup encryption stays optional. No interim policy is introduced.

DatabaseKeyBinding authenticates required nonempty profile/installation/generation IDs in a
canonical version-1 context. It is neither secret nor a KDF. WindowsDatabaseKeyProtector uses
[official CurrentUser DPAPI](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Security.Cryptography.ProtectedData/src/PACKAGE.md)
through already pinned ProtectedData 10.0.12. Only 32-byte DEKs and envelopes up to 4096 bytes are
accepted. Wrong context/damaged blobs/native plaintext with wrong length are refused, with no
replacement key or plaintext fallback. Owned input copies are cleared; the successful unwrapped
key belongs to its caller and must be cleared. Native/managed strings cannot be completely erased.

CurrentUser is not hardware/device-only protection and does not resist same-user code. Platform
storage adapters must add durable envelope files with proper ACLs and captured trusted binding;
this primitive neither opens existing credential stores nor chooses a recovery policy.

Eight native Windows tests plus four portable context tests pass (focused 69/full 3,232 on Windows).
DPAPI tests explicitly skip on non-Windows. Test-WindowsKeys.ps1 -RunName windows-d uses actual
source code, reviewed native manifest/hash/versions and only a new fictitious folder. A second
independent Windows process unseals its key and reads its encrypted native signed-money packet.
Records: artifacts/native-sqlcipher/d192-{data-key-tests,full-tests,key-process-proof}.log.

No app provider/key store/UI/schema/OS backup changes activated. Native Android/iOS adapters,
persistence/crash coordinator, optional recovery UX and final encryption/backup coupling remain.

D-192 strict main Windows and Android builds pass with zero errors/warnings. No visible screen
changed. Evidence: artifacts/native-sqlcipher/d192-{windows-build,android-build}.log.

The separate Android compatibility project compiles the same changed infrastructure/managed
package under ContinuousIntegrationBuild with zero warnings/errors (d192-android-probe-build.log).
Its unchanged schema/native runtime checks were not repeated for unused Windows key code.

## Android adapter and process proof (D-193)

AndroidDatabaseKeyProtector uses [official Android Keystore](https://developer.android.com/privacy-and-security/keystore)
and [KeyGenParameterSpec](https://developer.android.com/reference/android/security/keystore/KeyGenParameterSpec):
AES-256, encrypt/decrypt purposes, GCM/no padding, randomized encryption and automatic access
without per-use authentication. An application-scoped installation alias is explicitly prepared;
unwrapping only requires an existing non-exportable key. Execute off the UI thread under the
captured access/migration gate. The application PIN is separate. Envelope version 1 is exactly
65 bytes: ZKEY/version, 12-byte nonce, 32-byte encrypted DEK and 16-byte authentication tag.
D-192 context is authenticated; wrong context, damaged/malformed data and missing keys refuse.
Returned successful DEKs belong to callers; owned temporary DEK copies are cleared. This does
not promise complete native/managed memory erasure or protection against code inside the app.

Run ./eng/native/sqlcipher/Test-AndroidKeys.ps1 -RunName android-d using the existing private
emulator. It checks reviewed slices, builds only the independent signed Release AOT/trimmed
package and uses one fresh private fixture identity. Three force-stopped/new processes prove
creation, encrypted native reopen and fifteen behaviors (fourteen refusals plus nonce variation).
Evidence: artifacts/native-sqlcipher/build/android-d/android-key-proof-9c76f5ab82ee41fb9c24aa4ef33b9e4c.json,
android-key-build.log and artifacts/native-sqlcipher/d193-key-runtime.log. Both embedded ABI
hashes match reviewed candidates; ZIP integrity and v2/v3 signature checks pass. The independent
package is uninstalled after inspection, removing its fictitious files/Keystore entries only.

Actual OS-reported security level is 0 (software) on API36 x64/4 KiB emulator. Do not infer
hardware, arm64/API24/16 KiB OS, iOS or phone acceptance. Main strict Windows/Android builds pass
without errors/warnings. Portable managed sources are unchanged; 3,232 is retained D-192 test
baseline, not rerun. No visible change requires a screen matrix. Main composition still never
calls this adapter: no main key/provider/OS exclusion, durable files, recovery or migration
activation. Database remains plaintext until these remaining final effects are implemented.

## Android immutable envelope storage (D-194)

AndroidDatabaseKeyEnvelopeStore accepts an existing trusted application-private absolute directory;
caller must own its ancestors and hold the exclusive captured profile/access gate. It is not a
sandbox for arbitrary attacker-controlled directory paths. The final filename contains only bound
profile/generation GUIDs. Authenticate the envelope before creating its file. Actual hard-link publication returned EACCES
in the separate Android app-private Release run.
Use exclusive final creation (CreateNew), 0600, Flush(true), then
[native directory fsync](https://developer.android.com/reference/android/system/Os). This does not
publish the entire envelope atomically: interruption can leave a truncated final file, which fails
authentication/readiness and is never replaced with a fresh DEK. No automatic partial-file cleanup.
Future migration reconciliation must resolve interrupted state while retaining the original selector.

Read uses O_NOFOLLOW/O_NONBLOCK, fstat regular-file check, exactly 65 bounded bytes and authenticated
unwrap. No missing-key regeneration. Duplicate publication fails; resumption must read/reconcile
actual stored binding rather than retry with a new key. Failure after writing can leave the
complete or incomplete original envelope present; future coordinator must reopen it before recording readiness.
No failed flush certifies durable completion. These filesystem operations are not a hardware
power-loss guarantee, and same-UID malicious code is outside this adapter's protection boundary.

Run Test-AndroidKeys.ps1 -RunName android-d -Store. This changes only the separate proof's file
storage route, retaining the unchanged protector suite as D-193 baseline. Three new processes
create/reopen/failure-check original production storage source. Eight cases include refusing
replacement/corruption/malformed/missing/nonregular/symlink and ignoring a fictitious partial staged
orphan. No actual mid-publication kill or device power-loss injected. Main composition unchanged;
no automatic OS exclusion or live migration until the remaining protocol/recovery is implemented.

D-194 final evidence: build/android-d/android-key-proof-61b09ff1ad0044f28d0dc251c8f732aa.json
under artifacts/native-sqlcipher, plus d194-key-store-release-build.log. Signed APK v2/v3 and
ZIP integrity pass; original reviewed arm64/x64 native hashes match. Separate package removed.
Strict main Windows/Android build logs d194-main-{windows,android}.log have zero errors/warnings.

## Android final session ports (D-196)

Original envelope store now detects any existing entry before preparation, so damaged entries
are read/refused rather than replaced. AndroidGenerationKeyStore implements actual explicit
prepare/reopen and existing-only reads; mutable returned keys are cleared by the captured session.
StatFs and no-follow file/directory fsync provide concrete private-root durability effects. These
ports are exercised in independent original-model complete migration/process interruption proof;
main key creation and provider remain inactive. See generation guide for exact remaining scope.

### D-238 — Standard optional recovery wrapping and encrypted private vault

DatabaseRecoveryMaterial uses platform RSA-3072 with standard OAEP-SHA256. Each payload contains
its recovery id and the existing canonical DatabaseKeyBinding context plus the exact 32-byte DEK.
Public identity can wrap new generations after private material is disposed; private material is
needed only for explicit recovery. No application cipher, KDF or source fork is introduced.
Stored ciphertext has standard DEK-keyed HMAC-SHA256 confirmation over recovery id/context/ciphertext,
so the current native DEK can requalify an immutable existing mapping without the recovery secret.
Unwrap verifies both context and confirmation before returning an owned key. Inputs/exports/returned
keys have explicit ownership; owned private/key payload buffers are cleared and private RSA owners
are disposed. Managed connection-password strings cannot be completely erased.

SqlCipherRecoveryVault stores private PKCS#8 only inside official native SQLCipher keyed by a
caller-owned random 256-bit recovery key. It requires the reviewed cipher version, exact schema/
installation/recovery/public identity, bounded typed rows, keyed schema access, cipher integrity
and SQLite integrity. A staged no-replace metadata publication synchronizes and reopens exact
private material before final acceptance. Wrong/missing keys, plaintext, foreign or damaged vaults
fail without fallback or replacement. The recovery key itself is never persisted or logged.

SqliteRecoveryEnvelopeCatalogue contains only public identity, RSA ciphertext and HMAC confirmation,
with exact typed schema/identity and bounded immutable per-profile/generation mappings. Existing
capture authenticates ciphertext using the actual native DEK and requalifies native synchronization
after a visible commit/flush error. It refuses changed keys/ciphertext rather than overwriting them.
RecoveryGenerationKeyStore wraps every explicitly prepared native key before caller readiness
advances, while ordinary reads still require device keys. It never silently invokes recovery.
This applies to future database/safety generations when the final host supplies the decorator.

Thirteen focused cases verify standard encryption randomness, wrong profile/install/generation/
private identity, corruption/truncation/disposal, weak/noncanonical public keys, immutable SQLite
mapping/ciphertext/HMAC, failed synchronization without a replacement native key, absent authority
and missing/invalid vault keys. The full solution run passes 3438 behaviors before the final added
confirmation-tamper case; final affected suite passes 13 for 3439 covered behaviors. No financial
EF schema changed; operational recovery metadata does not alter the compiled model/migrations.

Independent native callers use the actual 32-migration model, native device envelope store and
SQLCipher current-generation owner. They preserve a negative fictitious opening amount through
cold recovery after deleting only the independently owned device envelope. A fixed synthetic key
exists only in these independent callers, never the product, a file or output. This tests recovery
material access, not a finished product recovery UI or durable device-key re-enrollment.

Native Windows actual recovery succeeds with unchanged financial ciphertext; wrong recovery key
and damaged private-vault copy fail authentication. Expected native HMAC failure diagnostics contain
page metadata, not keys. Android initially returned ENOENT after the intended fake-envelope removal;
the shared check failed to recognize that platform exception. Its explicit Android callback now
accepts only native ENOENT, not arbitrary exceptions. The original fixture is retained and resumed;
no replacement creation/key/schema is used to hide the failure. Terminal Android/build/APK evidence
follows below. An intermediate namespace qualification build error is retained separately.

Main storage remains plaintext. Final optional recovery enable/confirm UI, durable public/private
registration, native re-enrollment after actual wrapping-key loss, complete retained-reference/retirement
integration, key rotation, coupled OS exclusion and platform/device acceptance remain unfinished.
The vault/protocol is not registered by default; no PIN/device-authentication recovery is bypassed.
Zanance backup recovery and optional portable passwords remain unchanged. Owner OD-04/OD-10 stay
approved and do not require another decision. Unchanged UI and native producer suites are not repeated.

Primary API references: https://www.zetetic.net/sqlcipher/sqlcipher-api/ and
https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsaencryptionpadding.oaepsha256?view=net-10.0.

D-238 terminal evidence: independent strict Windows/Android callers have zero warnings/errors.
Main strict Windows completes in 19.94s, Android in 65.11s, both zero warnings/errors. Full
3438-test run has zero failures/skips (3m31.146s); final 13 affected tests pass (3.295s), including
the added confirmation-tamper scenario, for 3439 covered behaviors. The Android corrected caller
build completes in 62.11s with zero warnings/errors; the original namespace/error recognition
failures are retained, not counted as acceptance.

Windows cold reopen reuses the originally prepared fixture with its missing device envelope and
verifies both wrong recovery key and damaged private-vault copy refusal, actual signed money and
unchanged financial ciphertext. Android resumes the exact failed ENOENT fixture
b8c6121b68604d0692ba13a6a75afb5c without creation or key preparation. Its proof intentionally records
Created=null, not an invented successful initial phase. Cold existing-vault recovery succeeds,
wrong key/damaged vault fail, actual 32-migration finance and -321 opening balance remain, full
financial ciphertext hash is unchanged and no replacement device envelope is created. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-cfd01fd1aab141d9a6b3668efe90ed75.json.
Original expected-native-missing negative stack: d238-android-missing-native-error.log. Windows
final negative/positive log: d238-native-windows-final-reopen.log. Main encryption/recovery UX,
native re-enrollment after wrapping-key loss and full retention/retirement/OS adoption remain open.

D-238 complete signed Release APK: artifacts/android-d238-delivery/pro.vafadar.zanance-Signed.apk
(83,167,792 bytes). Signature verification passes; pro.vafadar.zanance package, API 24/36 and both
arm64-v8a/x86_64 embedded assembly stores are verified. Test output was cleaned after all terminal
checks. This APK does not activate the recovery host or main database encryption and uses local
test signing, not store release acceptance.
