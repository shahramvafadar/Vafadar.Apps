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
