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

### D-239 — Retire captured per-profile recovery mappings before native key removal

ProtectedProfileRetirementHost accepts an explicitly configured recovery catalogue. While Authorized,
it authenticates each target recovery mapping with the actual retained native DEK and records a bounded
immutable recovery-id/count/SHA-256 reference fingerprint in the existing retirement journal. This
contains no private material or plaintext key. Strict journal validation permits only the exact new
optional schema; unknown tables, malformed/missing captures and another recovery identity fail.
Retries requalify an existing Authorized capture before publishing catalogue removal. Missing recovery
capture cannot be inferred as empty after publication, and a host lacking recorded recovery effects
cannot continue a recovery-bearing request.

After actual catalogue persistence, the native host removes only the captured target profile mappings
before deleting its captured device envelopes/files. The recovery transaction rechecks target bytes,
refuses new/changed target generations, preserves every other profile's complete ciphertext/confirmation
rows, synchronizes and reopens actual removal. A delete visible after failed flush can requalify under
the same retained request without replacing keys or touching other profiles. Empty captures remain
explicit authority. Artifact and identity retirement both require actual recovery target absence and
the original recovery identity; the shared encrypted private vault is retained for other profiles.

Five actual SQLite/recovery cases cover exact target removal with other-profile recovery, failure
after delete followed by native requalification, added target generation refusal, wrong native key
capture refusal, and missing/wrong-phase/wrong-request cleanup refusal. One actual ProfileService
flow uses native Windows DPAPI/files and proves target recovery removal before identity retirement.
Existing four profile-retirement application cases still pass. Full/native/build/APK evidence follows.
No financial model or EF migration changed. Main registration remains plaintext; final recovery UX,
native re-enrollment/rotation, broader retained-reference/retention policy and coupled OS/platform
adoption remain in the full scope. Unchanged upstream native suites/UI are not repeated.

D-239 terminal test/native evidence: all 3445 solution tests pass, zero failures/skips
(2m48.472s). The separate strict Android Release AOT/trim build has zero warnings/errors
(55.62s). Actual ProfileService/native settings interruption followed by cold retirement succeeds
on the same fictitious run d367dc82b37c4c1299e987a33e379cfb. Target recovery mappings and identity
are retired, main balance remains 250, and the retained shared encrypted vault still recovers
main's exact key. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-5316e6f94cdc45a8b1af58468e1dd032.json.
This confirms actual Android catalogue/key/file/recovery composition, not product recovery UI,
physical phone/iOS or main encryption activation. Final main build/package evidence follows below.

D-239 main strict builds: Windows zero warnings/errors in 18.87s, Android zero warnings/errors
in 39.16s. No changed screen or upstream native producer required another layout/producer run.

D-239 complete signed Release APK: artifacts/android-d239-delivery/pro.vafadar.zanance-Signed.apk
(83,639,002 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. Local test
signing and independent recovery retirement proof do not activate main encryption or constitute
physical phone/iOS, recovery UX or store/release acceptance.

### D-240 — Explicit native re-enrollment of verified recovered keys

IDatabaseGenerationKeyRecovery requires an owned 32-byte recovered DEK and an explicit host callback
that verifies the same existing financial generation before any key entry is published. The host
remains responsible for app/device authorization and drained access. Native callers exercise actual
SqlCipherNewProfile/current-generation verification with the exact owned candidate key, not an
invented schema or a permissive financial check.

Windows stores CurrentUser DPAPI recovery material in a separate immutable .key.recovered entry;
Android stores a version-2 recovery envelope under a fresh non-exportable Keystore alias, with the
version/purpose/wrapping identity and existing DatabaseKeyBinding context authenticated by native
AES-GCM. Old aliases and original envelopes remain untouched, including unavailable originals.
The separate entry is staged, authenticated against the verified DEK, synchronized, renamed without
replacement and reopened before acceptance. An existing entry must match, with no replacement; a
staged entry can only resume through explicit recovery. Owned key copies are cleared. Reads choose
an existing recovery entry, fail on its corruption and never silently fall back or invoke recovery.
Ordinary preparation cannot regenerate a binding with a pending recovered stage.

Four native Windows contract cases verify original-envelope preservation and cold read, no writes
before failed finance verification, refusal of a different replacement key, and damaged recovery
entry refusal without fallback. The actual independent original-model caller runs create/key-loss,
verified import and a separate native cold read that does not use the recovery secret, preserving
signed finance and complete ciphertext. Android uses the same financial verification with actual
Keystore/fsync effects; terminal evidence follows below.

This is native re-enrollment, not a complete product recovery UI or authorization controller. Final
all-profile recovery orchestration, interrupted malformed-stage reconciliation, wrapping-alias
lifecycle for future new generations after invalidation, recovered-entry reference/retirement and
retention integration remain open. Strict reference discovery still refuses unqualified recovered
namespace artifacts before catalogue removal, rather than guessing ownership of an unavailable
original. Main registration stays plaintext; no temporary OS-backup policy, new permission or SDK.
Unchanged UI/native producer suites are not repeated. OD-04/OD-10 remain approved.

D-240 terminal native/test evidence: all 3449 solution tests pass, zero failures/skips
(2m54.750s). Final independent Android strict Release AOT/trim build has zero warnings/errors
(62.01s). Three native Windows processes create, import and reopen without using the recovery
secret; signed money and complete ciphertext remain unchanged. Three actual Android processes
pass the same flow using the fresh version-2 Keystore recovery envelope on fictitious run
7c24641d9fac425381bce1af3621ce25. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-2817166d0a484a5bab73f02ef924cca1.json.
Four Windows native contract cases also pass. The initial strict test compile exposed platform
annotations across assertion lambdas; the assertions now call the platform-neutral ports after
native Windows construction is explicitly guarded, without disabling diagnostics. Main Windows
strict build is zero warnings/errors (17.70s); final main Android/package evidence follows.
No full app recovery UI, alias invalidation matrix, hardware power-loss/device/iOS or main
registration acceptance is claimed by these native controlled tests.

D-240 main Android strict build completes with zero warnings/errors in 39.62s.

D-240 complete signed Release APK: artifacts/android-d240-delivery/pro.vafadar.zanance-Signed.apk
(83,216,944 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs were cleaned after all terminal checks. The APK
uses local test signing and does not activate main encryption/recovery UI or establish phone/iOS/
hardware power-loss/store acceptance. Recovered-entry reference/lifecycle integration remains open.

### D-241 — Authenticated native recovered-key references and retirement

KeyRecoveryReceipt records exact original/recovered native file identity, root identity, length and
SHA-256 under standard DEK-keyed HMAC-SHA256 and the existing profile/install/generation context.
It contains no plaintext key. The original object may explicitly be absent; missing receipt is never
inferred as empty authority. Same-content file/root replacement and changed bytes invalidate the
confirmation. A staged recovered object is normalized only to its final bound filename, retaining
its actual inode/file identity through no-replace rename.

Native import now publishes/resumes an immutable bounded receipt after actual finance verification
and recovered-envelope authentication, before final recovered-key publication. Native private file
creation and file/directory synchronization qualify the receipt. Existing and cold recovered reads
require exact authenticated receipt/current objects, without fallback or manufactured confirmation.
Explicit re-import can requalify the existing exact verified objects; ordinary reads never create
authority. Windows uses real owner-only native files; Android uses actual app-private no-follow
capture/fsync and the existing fresh wrapping alias. Original unavailable envelopes remain immutable.

ProfileRetirementReferenceDiscovery recognizes only exact recovered entry/receipt names, verifies
receipt HMAC and exact native originals/recovered objects with the actual DEK, then captures each
file for the existing native removal algorithm. Unknown or uncommitted staged recovery namespaces
still fail before catalogue publication. After catalogue removal the existing host removes captured
public recovery mappings, all original/recovered/receipt objects and finally the exact identity.
Other profiles and their shared private vault remain. RecoveryGenerationKeyStore forwards explicit
native import only after the same finance verification, then requalifies recovery mapping before
native publication; no public recovery write follows rejected finance verification.

Two new native Windows cases prove same-content original replacement refusal and missing receipt
refusal without fallback. Actual ProfileService source additionally discovers/removes original,
recovered entry and receipt alongside target recovery mappings. Existing native recovery/profile
cases pass. Independent actual Android finance/import/cold retirement evidence follows below.
No financial model/migration or visible UI changed. Main registration stays plaintext; final recovery
UX/all-profile orchestration, alias invalidation/future generation lifecycle, malformed-stage repair,
retention/OS/provider/platform acceptance remain in the full scope. No temporary OS policy or new SDK.

D-241 terminal verification: all 3452 solution tests pass, zero failures/skips (2m54.985s).
Six native Windows key/receipt cases and six actual app retirement cases pass. Final independent
Android strict Release AOT/trim build has zero warnings/errors (70.21s). The actual original
ProfileService/native recovery decorator imports the work profile's DEK after existing encrypted
finance verification, retains original/recovered/receipt objects during injected native preference
failure, and completes their captured cold retirement on the same fictitious run
40526ade78094cf3bcda86a7983ad21e. Main balance 250, its native identity and actual recovery through
the shared private vault remain. Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-e36f0b4cd43349f4b243acfeca472b6a.json.
The first compile exposed a local name collision; final source uses a distinct original-key reference.
Unchanged producer suites/UI were not repeated. Main/recovery UX/all-profile/alias lifecycle/
retention/OS/platform gates remain open. Final main build/package evidence follows below.

D-241 strict main builds complete with zero warnings/errors: Windows 17.76s, Android 39.04s.

D-241 complete signed Release APK: artifacts/android-d241-delivery/pro.vafadar.zanance-Signed.apk
(83,671,770 bytes). Signature, pro.vafadar.zanance package, API 24/36 and both arm64-v8a/x86_64
embedded assembly stores verified. Test outputs cleaned after all terminal checks. The local
signed APK does not activate main encryption/recovery UI or establish phone/iOS/store acceptance.

### D-242 — Preflight complete captured-profile recovery before native writes

ProtectedProfileRecovery captures the actual original profile catalogue independently of commercial
selection and requires the unpublished covered startup endpoint plus its real drained database owner.
Stable native identities must match every captured legacy profile and the existing recovery
installation. All canonical recovery bindings are read from strict bounded metadata; unknown profiles
and a missing original-profile mapping fail without inferring a fresh database or empty authority.

An explicit native unlock port opens the existing private vault with the caller-owned recovery key.
Each DEK is unwrapped/confirmed and passed to the host's complete actual reference verifier before
the first native import. A failed profile preflight therefore writes no native recovered entry.
All retained key buffers are bounded by the existing 4096-binding inventory and cleared in finally;
private material is disposed. Before/within each import and after the batch, catalogue, identities,
bindings and unpublished/drained access are rechecked. Later legitimate profile edits are preserved,
not overwritten. Native imports remain immutable and verify their owned copies against existing
references. Recovery does not publish a session, modify profiles/preferences or complete financial
operations; only normal protected startup runs after the verified native batch.

Four actual application-source cases with real isolated SQLite/RSA metadata prove full-profile
preflight before first write, second-profile failure with zero imports, missing mapping refusal before
unlock, and catalogue mutation refusal without name overwrite. Native unlock/reference/import effects
are explicit ports in these cases, not fake MAUI controls or copied product algorithms.

The independent Android caller uses actual native preferences/persistence, two encrypted original
32-migration profiles, automatic/public recovery capture and actual ProtectedProfileRecovery. It
removes only its fictitious native envelope files, verifies/imports all original profile keys and then
runs actual ProtectedDatabaseStartup before real switching/financial reads. Terminal evidence follows.
Initial strict fixture compilation exposed startup-call ordering before local declarations; the
final caller invokes startup only after the owned recovery lease is released.

Main registration remains plaintext. This composes captured recovery and startup prerequisites but
does not finish product recovery/authentication UI, real alias invalidation/hardware acceptance,
complete concrete retained safety/preference/abandoned-reference verification, retained-file policy,
OS exclusion or platform/iOS/provider acceptance. The final host must verify every supplied referenced
binding; the fixture has current initial generations only and does not prove a full retention corpus.
No schema/model/permission/export/new SDK changes; unchanged UI/native producer suites are not repeated.

D-242 terminal evidence: all 3456 solution tests pass, zero failures/skips (2m55.483s), including
four actual application recovery cases. Final independent Android strict Release AOT/trim build
has zero warnings/errors (81.99s). The same fictitious two-profile fixture
4535123ccf8f4f2c8db30f79f4ea1f87 removes both native envelope files and, in a cold process, runs
actual ProtectedProfileRecovery/native vault/import plus actual ProtectedDatabaseStartup and
real switching. Original main/work balances remain 250/880 and no extra profile is created.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-b977a43500c74666b1e93a0e3afe068b.json.
The fixture proves current initial native generations; full retained safety/preferences/abandoned
reference and real alias-loss corpus remain separate unfinished host acceptance. Final Windows
strict build has zero warnings/errors (19.42s). Main encryption/recovery UI remain inactive;
final Android/package evidence follows below. Unchanged native producer/UI checks were not repeated.

D-242 main strict Android build has zero warnings/errors (38.31s).

D-242 delivery APK: artifacts/android-d242-delivery/pro.vafadar.zanance-Signed.apk, 83,257,904 bytes. Signature verification passed; package pro.vafadar.zanance, API 24/36, arm64-v8a/x86_64 and both complete assembly stores were checked. Local test signing is not store or physical-device acceptance. Test output cleanup passed with zero warnings/errors. Main protection remains inactive.

### D-243 — Recover after actual independent Android wrapping-alias loss

The independent Android native caller now has a WrappingKeyLoss scenario. It removes only the
Keystore alias for its freshly created fictitious installation in the distinct compatibility
package. It preserves both original encrypted key envelopes and requires actual native reads
of both bound DEKs to fail. It changes no global/device settings or product security data.

A cold process then runs the actual ProtectedProfileRecovery and protected startup compositions
from D-242, including native recovered-envelope/receipt publication and real profile switching.
Both original envelope SHA-256 values remain unchanged, the unavailable installation alias is
not recreated, no extra profile appears, and main/work balances remain 250/880. This proves actual
alias absence rather than merely deleted envelope files. It does not establish hardware-backed
invalidation, biometric change behavior, future-generation alias lifecycle or physical-device acceptance.

Verified independent strict Release AOT/trim build: zero warnings/errors, 55.90s. Actual two-phase
runtime proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-679ead1e4b7f46e699024d045d8a8a6d.json,
run c12bdc1cb2d04abf88b7f9cf93593c31. Script parsing and diff checks passed. No application source,
provider registration, UI, permission, financial schema or APK changed. Therefore unrelated solution
suites, main builds, UI matrices and native producer suites were not repeated. The complete signed
D-242 delivery APK remains the current product package; main storage remains plaintext. Final
recovery/authentication UI, complete retained-reference verification, lifecycle/retention, coupled
OS backup exclusion and platform/provider/release acceptance remain open.

### D-244 — Independent Android wrapping aliases for new generations

New native generation preparation no longer recreates the installation wrapping alias. Every new
DEK is wrapped by a fresh non-exportable Android Keystore AES-256-GCM alias. The existing version-2
header authenticates purpose 0 for ordinary generations and purpose 1 for explicit receipt-bound
recovery, along with the complete profile/installation/generation context. Reads accept immutable
legacy version-1 entries and new purpose-0 entries; recovered reads still require purpose 1 and their
native receipt. Ordinary and recovered entries cannot substitute for one another. Bounded native
reads accept only exact authenticated formats up to 82 bytes, not a truncated legacy packet.

The independent caller deliberately prepares legacy installation-bound envelopes for both fictitious
profiles, removes their actual installation alias, verifies refused original reads, recovers through
the actual application/native startup, then creates a new independent generation without recreating
the old alias. A third cold process reads that generation using existing native storage only and
reopens a SQLCipher packet containing Int64.MinValue; recovery-purpose confusion is refused.
This is lifecycle creation/reopen evidence, not final reference-aware alias retirement or hardware acceptance.

The initial fixture compile exposed local-name collisions; final strict native Release AOT/trim build
passed with zero warnings/errors (64.51s). The first runtime observation of the third process expired.
The same process subsequently returned successful completion, read without restart or regeneration:
artifacts/native-sqlcipher/d244-same-process-future-proof.json. Negative observation evidence remains
in d244-alias-runtime.log. Earlier phases passed the runner's actual assertions before its third
launch; no synthesized Created/Reopened report is supplied. The runner now makes one final read of
the same completion target before a non-barrier deadline failure; it never restarts to hide expiry.
Script parsing passed. All 3456 solution tests passed, zero failures/skips (2m53.266s).

Main protection registration remains inactive. No UI/schema/account/portable-password/permission
change. Complete retained-reference validation, recovery/authentication UI, malformed staging,
reference-aware key/alias retention, final provider/OS adoption and physical platform/iOS/provider
acceptance remain open. Unchanged UI and upstream native producer suites were not repeated.

D-244 strict main builds passed with zero warnings/errors: Windows 17.10s and Android 38.62s.

D-244 complete signed Release APK: artifacts/android-d244-delivery/pro.vafadar.zanance-Signed.apk, 83,696,346 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both embedded assembly stores verified. Solution test outputs cleaned successfully after terminal checks. Local test signing is not phone/iOS/store acceptance; main protection registration stays inactive.

### D-245 — Authenticate retained native references before recovery import

ProfileRecoveryReferenceVerifier supplies the concrete read-only reference check for captured native
recovery. It reads actual stable identities and strict restore ancestry, rejects unknown database
namespace entries and missing selected candidates, and requires selected restored journals plus
retained before-restore/preference references. Existing candidate, initial rollback, before-restore
and preference databases are reopened with the owned recovered DEK, reviewed cipher version,
cipher_integrity_check and full integrity_check. Preference capsules also pass their actual typed,
bound allowlisted ReadAsync validation; no settings replay occurs during key verification.

Matching device safety references require complete AES-GCM authentication, not only an untrusted
header. Foreign headers never cause foreign native key access. Safety-only generations can prove
authenticated retained reference without pretending to be financial database generations. Keys
with no authenticated reference fail. All decoded package/key buffers are cleared; native file/root
identities, lengths, fingerprints, SQLite sidecars, namespace listings, identities and ancestry are
rechecked after validation. Caller-owned drained access and actual private-root effects remain mandatory.

Five native Windows/private-file cases pass: genuine safety-only reference, tampered content refusal,
foreign reference refusal, unknown namespace refusal and missing selected generation refusal. These
cases use actual SQLite metadata and native file capture, not a copied verification algorithm.

The independent signed Release Android caller now uses this verifier in actual ProtectedProfileRecovery.
It creates two original encrypted profiles, two initial encrypted rollbacks, two real typed encrypted
preference capsules and two authenticated safety envelopes with separate captured recovery keys. All
four fixture device envelopes are removed. A cold process verifies every binding before native
imports and then runs actual covered startup/switching. Main/work balances remain 250/880; no extra
profile is created. Native file inventory confirms both capsules/rollbacks and safety references.
Proof: artifacts/native-sqlcipher/build/android-d/android-migration-proof-6196395354e94738857ce12275c2ac91.json,
run 66f9c0a865d041b68b450403a753785b; inventory: artifacts/native-sqlcipher/d245-native-reference-inventory.log.
The safety fixture proves authenticated codec references, not ZIP manifest/restore acceptance. Its
preference capsules exercise the existing capsule format on initial candidates; a full multi-restore
history/pending-restoration corpus and staged/orphan reconciliation remain separate acceptance work.

All 3461 solution tests pass with zero failures/skips (2m55.599s). Final independent Android strict
Release AOT/trim build has zero warnings/errors (67.48s); Windows strict main build is clean (17.17s).
No UI/schema/permission or portable backup behavior changed. Main registration remains plaintext.
Final recovery UI/authentication, complete restore/crash corpus, reference-aware generation/alias
retention, final provider/adoption/OS policy and platform/provider/device gates remain open. Unchanged
UI matrices and upstream native producer suites were not repeated.

D-245 strict main Android build passed with zero warnings/errors (39.69s).

D-245 complete signed Release APK: artifacts/android-d245-delivery/pro.vafadar.zanance-Signed.apk, 83,262,000 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned successfully after terminal checks. Local signing is not store/phone/iOS acceptance; main protection remains inactive.

### D-246 — Require committed recovery candidates before history mirroring

Review of the real journal/history interruption boundary found a concrete omission in D-245:
required candidate presence followed only the history Selected flag. Verification/selection can
already have committed in the restore journal when history mirroring has not run. The verifier now
requires an actual candidate whenever the journal reached CandidateVerified or Selected/Completed,
independently of that history flag. An authenticated safety reference cannot replace the required
financial candidate. Existing selected rollback/preference requirements remain unchanged.

Two additional actual SQLite/native-file cases exercise CandidateVerified and Selected while history
remains unselected. Each requires the exact missing-candidate refusal before other reference reads,
without inventing data or changing the journal/history. The Selected fixture's intentionally unread
placeholder files isolate this ordering gate; they are not evidence of ciphertext/financial validation.
Seven focused cases pass. All 3463 solution tests pass, zero failures/skips (2m56.718s).

The independent signed Release AOT/trim Android caller executes the same original verifier with
real native private roots and real strict metadata for both interrupted phases. Both refuse missing
candidates, preserve journal/history and create no replacement data/key. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-363f2a58a6bb4a6a99ee8e25041a3305.json,
run 61aa8a42f371446692208249ebb47aa9. Final strict native build has zero warnings/errors (58.82s).
The independent entry point explicitly selects the reviewed native provider before metadata access.
Script parsing and diff checks pass. Unchanged positive financial/UI/producer matrices were not repeated.

This fixes a real missing-reference boundary; it does not complete the multi-restored financial corpus,
malformed/staged/orphan reconciliation, final recovery UI, reference-aware retention or main provider/
OS adoption. No schema/model/permission/UI change. Main storage registration remains plaintext.
Physical devices, iOS, external providers and release acceptance remain separate gates.

D-246 strict main builds have zero warnings/errors: Windows 17.06s and Android 38.94s.

D-246 complete signed Release APK: artifacts/android-d246-delivery/pro.vafadar.zanance-Signed.apk, 83,704,538 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not store/phone/iOS acceptance; main protection remains inactive.

### D-247 — Cold recovery of a real interrupted two-restore chain

The independent Android caller now creates the actual 32-migration model, writes initial account
balance 250, and restores two complete portable packages through the actual BackupService and
SqlCipherPackageRestoreReceiver. The source databases are in memory; the original exporter creates
bounded portable snapshots without plaintext disk staging. The first package restores balance 730.
The second restores balance 990 and deliberately fails after actual native preference persistence,
while its journal is Selected and history remains pending. All three captured device envelopes are
removed; original/candidate/rollback/preference files and private/public recovery material remain.

A cold process uses actual ProtectedProfileRecovery and ProfileRecoveryReferenceVerifier to preflight
and re-enroll every retained key before opening current storage. Actual OpenCurrentAsync resumes the
selected second restore without the portable package or old current connection, persists/replays the
native preference value and completes the same three-row ancestry. Actual compiled-model reads
confirm candidate balances 250/730/990 and before-restore balances 250/730. All retained ciphertext,
rollback and typed preference capsules pass native authentication/integrity/reference verification.
The source-only backup and unused profile-transition ports do not duplicate application algorithms.
This probe does not implement or bypass production UI/device authentication.

Initial cold recovery correctly refused stale independent-package profile metadata: fixture setup
removed profiles.catalogue rather than the real profiles.work.catalogue. Setup now clears the actual
fictitious catalogue. An explicit recorded-fixture continuation verifies the one-profile identity and
three-row history, refuses non-fictitious extra profiles, and repairs only that independent package's
stale catalogue before actual recovery. Existing generations/keys were not rebuilt. Negative runtime
and continuation evidence remain in d247-native-runtime.log and d247-native-continuation.log.
Continuation proof has Created=null, not invented creation evidence:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-39cbf89ea73f4a21b71d998352fecd12.json,
run 5e99e9f6a10944f0a286301b37d86733.

After the fixture correction, one fresh complete two-process scenario passes. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-89a5cc941e1842debb53350fb37f12d9.json.
Final independent strict Release AOT/trim build has zero warnings/errors (54.69s). Script parsing and
diff checks pass. Product source, financial model, UI, permissions, native producer binaries and main
APK are unchanged from D-246. Therefore unchanged 3463-test baseline, main builds/UI matrices and
producer suites were not repeated. The complete verified signed D-246 product APK remains current.

This verifies the real multi-restore/pending-replay recovery corpus. It does not complete malformed
staging/orphan reconciliation, generation/key/alias retention, final recovery/authentication UI,
main provider/OS adoption, physical-device/iOS/provider acceptance or publication. Main remains plaintext.

### D-248 — Match Android key reads to native file ownership requirements

AndroidDatabaseKeyEnvelopeStore now rejects non-regular, multiply linked and foreign-UID file objects
through the actual opened descriptor's fstat before reading/unwrapping. This matches the existing
native retirement/reference boundary instead of accepting a key object that cannot safely qualify for
later cleanup. No key format, crypto algorithm, profile data, native producer binary or UI changed.

The independent original key-store caller verifies creation, cold encrypted reopen and malformed/
missing/directory/symlink/no-overwrite/staged-file cases. Its additional hard-link case reports whether
the read-refusal branch was actually exercised. On this emulator, native link returned EACCES; the
initial negative phase stopped. The fixture now recognizes only EPERM/EACCES as explicit OS link
creation restrictions, not successful coverage of the link-count guard. Its first pattern form used
nonconstant Android errno properties and failed compilation; ordinary equality fixed that harness
compile issue. Both negative runtime/compile logs are retained.

The same fictitious key run fcf6548964f64553a8a8ba41af5a4979 resumes only the negative phase with the
existing key/encrypted fixture; no fresh DEK or creation/reopen replay. Proof:
artifacts/native-sqlcipher/build/android-d/android-key-proof-fcf6548964f64553a8a8ba41af5a4979.json.
HardLinkReadRefusalExercised=false is explicit. The original positive phases passed before the
OS restriction; resumed proof contains the negative phase only and does not invent those results.
Foreign-UID and hard-link read rejection still need an environment capable of creating such objects.
Final independent strict Release AOT/trim build is clean (57.56s). Harness parsing and diff checks pass.
All 3463 solution tests pass, zero failures/skips (2m54.568s); strict Windows main build is clean (23.24s).

Main registration remains plaintext. This read-boundary alignment does not finish malformed-stage/
orphan cleanup, reference-aware file/key/alias retention, final recovery UI/provider/OS adoption or
physical-device/iOS/provider/release acceptance. No unchanged UI or upstream producer suite repeated.

D-248 strict main Android build has zero warnings/errors (40.62s).

D-248 complete signed Release APK: artifacts/android-d248-delivery/pro.vafadar.zanance-Signed.apk, 83,266,096 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-249 — Continue verified recovery past an interrupted receipt stage

Explicit native recovery verifies the owned DEK against retained finance before receipt preparation.
When an existing uncommitted receipt stage has different bounded bytes, KeyRecoveryReceiptStore now
preserves it and creates a separate unique no-replace receipt stage. The new stage is read back,
synchronized and renamed without replacing final authority. Published receipts still require the
same exact native object/MAC and are never overwritten. Correct legacy stages still resume normally.
This does not infer ownership of or delete malformed objects, repair oversized/unsafe files or replace
a published receipt. Cipher-key staging itself remains under its existing stricter continuation.

One new actual Windows DPAPI/private-file case preserves torn stage bytes, publishes final authority,
cold-reads the same DEK and leaves no new unique receipt stage. Seven focused cases pass. The first
assertion used a Windows wildcard that also matched the retained legacy stage; an exact ordinal
predicate fixed the test, then the required collection assertion was aligned with the strict analyzer.
Original negative test/compile logs are retained; no cryptographic/provider code was changed for them.
All 3464 solution tests pass, zero failures/skips (2m48.082s).

The independent signed Release AOT/trim Android caller puts a three-byte interrupted receipt stage
beside each of its four removed fixture envelopes. Actual all-profile recovery authenticates native
references, imports the same keys, publishes valid receipts and opens original main/work balances
250/880 through covered startup and real switching. All four old stages remain with the SHA-256 of
bytes 1/2/3, verified from the same native private root after success. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-d60c0cd29ac9426d9008a60df6307368.json,
run cf79bce63f8546939f2686cebd92a45a; retained stages: artifacts/native-sqlcipher/d249-retained-stage-proof.log.
Independent strict native build is clean (59.44s); strict Windows main build is clean (21.99s).

Preserved rejected staging still needs reference-aware reconciliation before retirement; this is
functional explicit recovery continuation, not finished cleanup/retention. No schema/model/UI/permission
change. Main protection remains inactive; final recovery UI/provider/OS adoption and platform/device/
provider/release acceptance stay open. Unchanged UI and upstream producer suites were not repeated.

D-249 strict main Android build is clean (39.64s), zero warnings/errors.

D-249 complete signed Release APK: artifacts/android-d249-delivery/pro.vafadar.zanance-Signed.apk, 83,704,538 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.

### D-250 — Retire bounded temporary receipts under authenticated whole-profile authority

Native profile reference discovery recognizes only the exact legacy receipt-stage name and the
canonical lowercase nonempty GUID attempt suffix introduced in D-249. These are bounded temporary
receipt metadata, never key or selector authority. A reviewed whole-profile retirement may capture
them only after the actual final recovered key and its original/recovered native-object receipt pass
authentication. Oversized stages, invalid names, missing final recovered authority and staged key
envelopes remain refused. No age-based guess or directory-wide deletion is introduced.

Each permitted stage enters the existing immutable native-object inventory with its root/file ids,
length and full fingerprint. The existing durable catalogue-removal barrier and all-object preflight
still precede exact native deletion. Replaced/changed stages cannot be silently removed. This allows
receipt remnants preserved by successful explicit recovery to leave with their reviewed profile,
without giving those remnants permission to open financial data or authorizing automatic key cleanup.

Three actual application-source/native Windows cases cover removal of bounded legacy/unique stages,
refusal without a published recovered entry and refusal of oversized metadata. Nine focused cases
pass; all 3467 solution tests pass, zero failures/skips (2m53.771s).
The independent signed Release AOT/trim Android caller creates both receipt-stage forms, performs
actual finance-verified key recovery, captures real profile retirement and fails after actual native
catalogue persistence. Cold actual host continuation removes only the captured target artifacts and
recovery/identity mappings. Main balance 250 and its recovery material remain. Proof:
artifacts/native-sqlcipher/build/android-d/android-migration-proof-150ac54a41354b7093e18671608572ad.json,
run 21bea656d24e4c618f8fc38f0dc53772. Strict native build is clean (66.92s).

This closes reviewed profile retirement of bounded receipt-only remnants. It does not classify or
remove malformed key stages, unknown artifacts, historic pre-intent orphans or files still referenced
by recovery/safety/history. Final automatic safety/generation/key/alias retention and main recovery/UI/
provider/OS adoption remain open, as do physical-device/iOS/provider/release gates. No schema/model/
permission/UI change; main registration remains plaintext. Unchanged UI/producer suites not repeated.

D-250 strict main builds have zero warnings/errors: Windows 17.78s and Android 91.82s.

D-250 complete signed Release APK: artifacts/android-d250-delivery/pro.vafadar.zanance-Signed.apk, 83,266,096 bytes. Signature, pro.vafadar.zanance package, API 24/36, arm64-v8a/x86_64 and both complete embedded assembly stores verified. Test outputs cleaned after terminal checks. Local signing is not phone/iOS/store acceptance; main protection remains inactive.
