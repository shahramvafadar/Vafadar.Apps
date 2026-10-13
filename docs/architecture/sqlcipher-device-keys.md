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
