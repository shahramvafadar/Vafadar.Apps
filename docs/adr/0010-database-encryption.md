# 0010. Profile database encryption and device-bound key envelopes

- Status: **Proposed for owner review; not accepted or wired into Zanance**
- Date: 2026-10-08
- Section: ZCR-SEC-01; decision log D-68
- Source baseline: 081603c596c8b2331990112212798c9604a7b477
- Scope: research, fictitious independent feasibility harness and migration design only.

## Recommendation

Use the SQLCipher format with a random 256-bit data-encryption key (DEK) per profile and a separate versioned device
key envelope. Prefer maintained official Zetetic platform builds for production, conditional on the owner's licence
budget approval and repeat verification with the exact purchased binaries. Do not ship the deprecated
SQLitePCLRaw.bundle_e_sqlcipher 2.1.11 build. If the owner declines recurring licence cost, evaluate reproducible
self-built current SQLCipher Community binaries in a separate section, including native build/update ownership,
crypto dependencies, licences, platform packaging and iOS. SEE is a paid alternative, not an adopted dependency.

This selects an architectural direction, not a production binary. Neither an unofficial successful probe nor a
vendor's platform support statement establishes production readiness. No paid purchase, trial account, production
provider switch, migration, password rule or recovery-policy approval occurs in this section.

## Verified current storage inventory

Inventory is based on source, not opening the owner's data. Paths below are logical locations; no real financial
file, credential, token cache or secure value was read to perform this review.

| Asset / source | Current storage and protection | Required future treatment |
|---|---|---|
| Main and profile databases: MauiProgram.DatabasePath, ProfileService.PathOf, LocalDatabaseLocation.Build | AppData/zanance.db and profile files; connection has DataSource only, no key. OS sandbox/disk protection is the current boundary | Encrypt all profiles, including inactive ones; fail closed if a key is missing |
| WAL, SHM, rollback journal | SQLite sidecars; plaintext page data in WAL/journal with current SQLite. SHM is a wal-index, not data pages | Encrypt page-bearing sidecars; inspect leakage while files exist; do not describe SHM as encrypted |
| EntryAttachment.Data | Receipt/document bytes, names and financial metadata inside database BLOBs | Covered by page encryption, including indices/schema |
| SqliteDatabaseBackupSource | Online backup into a new unkeyed temporary sqlite file under Path.GetTempPath; best-effort cleanup of sidecars | Replace this path in SEC-08/09; no new plaintext disk staging for encrypted backups |
| BackupService.CreateSafetyCopyAsync | Unprotected ZIP package in AppData/backups-safety, even when normal backup protection is on | Device-encrypted safety envelope with explicit key retention; do not prune a referenced recovery copy |
| Normal local/cloud .vbak | Optional password: AES-256-GCM, PBKDF2-SHA256 600,000; otherwise readable ZIP. D-62 deliberately permits no password | Preserve portable optional-password semantics; local DB encryption must not imply cloud end-to-end protection |
| AttachmentFiles / receipts / PDF rendering | Cache/attachments external-viewer copies; camera/picker and PDF-rendering temporary copies; startup/best-effort cleanup | Bound lifetime, exclude OS backup; encrypted storage cannot encrypt a file while an external viewer needs plaintext |
| CSV exports / PDF reports | Plaintext cache files, then user-controlled sharing destinations | Explicit user-controlled export; clear cache where safe; no claim of control over recipients' copies |
| Profile list and display Preferences | Names/profile ids and non-secret choices outside DB; portable display allowlist added in D-67 | Minimize exposed financial labels; never add DEKs, PIN state or tokens to portable allowlists |
| Four-digit PIN | Salted verifier/attempt state in SecureStorage; independent UI gate, not database encryption | Keep PIN as convenience gate, never use 10,000 possible PINs as offline DEK derivation |
| OAuth caches | Platform-protected stores (DPAPI on Windows, platform secure storage/cache elsewhere) | Keep independent; not part of finance database or its portable key envelope |
| OS backup | Android allowBackup=true; D-63 rules exclude only device-bound SecureStorage preference ciphertext. iOS no DB exclusion found | SEC-07: exclude database, envelopes, safety copies and sensitive staging from cloud backup and transfer; no change here |

The historical 2026-10-07 audit's claim that Android has no extraction rules is superseded by D-63: rules exist,
but still do not exclude financial databases. Debug snapshots can create fictitious files in the production app
namespace; this harness never uses those routes or that namespace.

## Threat model

Assets: financial ledger, attachments, profile labels, exports, database keys, recovery material and backup passwords.
Trust boundaries: OS sandbox/user session, native SQLite, application process, platform key service, user-controlled
backup/export destinations. Local-first Free remains usable without an account, payment or network.

| Adversary / event | Expected benefit / limitation |
|---|---|
| Copied app-data files, accidental disk/OS-backup disclosure | Ciphertext DB and device-bound envelopes reduce offline exposure; portable unprotected backups remain readable by design |
| Lost locked phone | OS file protection remains essential; an additional per-app envelope narrows copied-file exposure; no claim of bypass-proof hardware |
| Lost unlocked phone / app already unlocked | PIN/lock reduces casual UI access. A decrypted running process can still read its data; encryption alone cannot solve this |
| Shared Windows PC | CurrentUser DPAPI helps separate users and offline copies; malware executing as that same logged-in user may invoke DPAPI |
| Root, debugger, administrator, injected code, compromised OS | Out of scope for a confidentiality guarantee; can extract process keys or request decryption. No anti-forensics claim |
| Wrong key, missing secure storage, biometric/key invalidation | Explicit blocked/recovery state; never recreate the DB or silently generate a replacement key for existing data |
| Power loss, full disk, profile switch, interrupted migration | Copy/verify/durable-switch protocol below; no mixed plaintext/encrypted profile state |
| Modified ciphertext | SQLCipher page authentication rejects altered pages; this does not prevent valid old-snapshot rollback or all metadata tampering |
| Screenshots, notifications, exported files | Existing D-63 policy and generic notifications remain separate. Allowed screenshots and shared copies escape the DB boundary |
| Length/timing/path/WAL-index observations | File sizes, paths, access patterns and SHM metadata can remain observable; not hidden by page encryption |
| Forgotten password / lost device | Requires a separately approved recovery channel; no service can magically reconstruct an unknown random DEK |

## Library and deployment comparison (primary sources checked 2026-10-08)

| Candidate | Licence / cost | Platform, maintenance and EF/AOT implications | Decision |
|---|---|---|---|
| SQLCipher Community through bundle_e_sqlcipher 2.1.11 | Community BSD-style SQLCipher; wrapper Apache-2.0; underlying crypto notices also apply. No binary fee | NuGet marks bundle deprecated/unmaintained. Actual Windows probe loads SQLCipher 4.5.2, SQLite 3.39.2. EF 10's documented supported SQLite baseline is 3.46.1+, so a simple ORM pass does not establish supported compatibility. Native mobile ABI/page-size and crypto updates are concerns | Reproducible feasibility comparator only; never production |
| Current self-built SQLCipher Community | No SQLCipher binary licence fee; engineering, build, patch and compliance cost | Own Android/iOS/Windows native builds, crypto/toolchain updates, ABI alignment and provider initialization. Need pinned source, SBOM, signatures, reproducibility and update SLA | Fallback only if owner declines paid builds; not built in this section |
| Zetetic official Commercial / Enterprise SQLCipher | Commercial starts at USD 999 per application/year for a selected platform; taxes, multi-platform quote/terms require verification before purchase; Enterprise quote | Maintained native packages, official platform instructions and support. Managed SQLitePCLRaw glue still required. Android/iOS packages belong at application level; iOS uses internal provider. No commercial binary downloaded or tested here | Preferred production procurement route, subject to owner approval and exact-binary gates |
| SQLite Encryption Extension (SEE) | USD 2,000 perpetual source licence; listed USD 3,500 with one year support. Standard licence page requires compiled static linkage; clarify .NET dynamic native packaging/redistribution with vendor | Official SQLite extension; several cipher modes with different authentication properties. Requires native builds or separately quoted maintained build service. Provider/password semantics and format differ from SQLCipher; no licensed binary tested | Viable paid alternative, but more build/licence-integration uncertainty for this app |
| Encrypt only selected fields / whole file on close | Custom code and cryptographic maintenance | Field encryption leaves schema/index/query leakage and weakens reports; decrypt-to-temp or close-only encryption creates plaintext working files and crash risks | Reject for the stated at-rest database boundary |

Production integration must use Microsoft.EntityFrameworkCore.Sqlite.Core / Microsoft.Data.Sqlite.Core with exactly
one native provider; remove the default unencrypted bundle from the executable dependency graph. An encrypted native
provider is process-wide, not a per-profile toggle. Require a non-empty cipher_version and expected version at start;
unknown/ordinary SQLite must stop before any write. A Password connection-string value on ordinary SQLite is not
proof of encryption. Do not log a key-bearing connection string, SQL key pragma or parameter payload.

## Key design and choices

| Choice | Benefit | Limitation / proposed use |
|---|---|---|
| Random 32-byte DEK per profile | No human password entropy in page encryption; compartmentalizes profile files | Recommended; cryptographic RNG only, versioned key id/generation |
| One app-wide DEK | Simpler | Larger compromise/rotation blast radius; reject |
| Four-digit PIN as DEK password | Familiar | Offline enumeration is trivial; reject. Current PIN stays a UI gate |
| Device-only envelope | No extra remembered secret; fits existing UX | Cannot recover from device/key loss by itself; recommended default with explicit backup/recovery choices |
| Strong password-derived KEK | Portable/recoverable envelope option | User memory burden and offline guessing; requires versioned KDF and a product decision |
| High-entropy recovery secret | Can rewrap after loss/forgotten password | User must preserve it; shown once and verified by a future user flow. OD-04 remains proposed |

Flow: generate the profile DEK; encrypt database pages with it; seal the DEK under an OS-protected wrapping key;
persist only a versioned authenticated envelope. Authenticate profile id, installation id, key generation and format
version as envelope context. No DEK in Preferences, cloud account, portable display source, telemetry or logs.

* Android: non-exportable Android Keystore AES-GCM wrapping key. Hardware-backed status is measured, never assumed;
  emulator success is not hardware evidence. An authentication-bound alternative changes background posting and must
  be tested for lock, reboot and biometric enrollment/invalidation. Defaults must preserve after-unlock operation.
* iOS: use explicit Keychain accessibility/access-control settings, e.g. device-only when-unlocked storage for a
  wrapping secret, with synchronization disabled. Do not rely on MAUI SecureStorage defaults for a device-only
  assurance; its documented sync/reinstall behaviour differs. No Mac/iOS runtime proof was available here.
* Windows: CurrentUser DPAPI wrapping with profile/generation context; ACLs on envelope files. Avoid LocalMachine
  scope. Protects copies/other users, not arbitrary code running under the same user.
* Strong app password (future OD-04/SEC-04): derive a KEK and AES-GCM-wrap the DEK, then device-seal that envelope.
  If biometric unlock is enabled, a distinct authentication-bound DEK envelope is an explicitly approved alternate
  unlock path. Do not leave an automatically readable device envelope that silently bypasses a required password.
* KDF proposal: Argon2id (OWASP minimum 19 MiB, t=2, p=1) when a maintained cross-platform/AOT dependency is verified;
  otherwise PBKDF2-HMAC-SHA256 at least 600,000 iterations with random 16-byte salt, calibrated on target phones,
  bounded validated versioned parameters. The sample exercises PBKDF2/AES-GCM, not Argon2 or a product password UI.
* Recovery: an approved random recovery secret may add a recovery envelope. Restoring a portable backup creates a
  fresh local DEK/envelope; never copy device credentials. Password protection for portable backups stays optional
  under D-62, including cloud backups. Do not force encryption retroactively in this architectural proposal.

Rewrapping a DEK after password/KEK changes does not rewrite the database. Suspected DEK compromise requires data
re-encryption and copy/verify/switch; direct in-place rekey success in the harness is not a crash-safe rotation proof.
Dispose native connections and clear key-specific pools at lock/profile change. Zero mutable buffers best-effort;
managed strings passed through the SQLite Password API cannot be reliably zeroed, and plaintext exists in memory
while unlocked. Key loss never falls back to plaintext, automatic database deletion or default account creation.

## Independent feasibility evidence

Harness: experiments/Zanance.Encryption; outside application/solution references. Windows tests use generated
temporary directories. Android package is pro.vafadar.zanance.encryptionproof, uses only its own private files and
has OS backup disabled, no network permission, and explicit debug access for retrieving fictitious reports. This
harness is not a production app or a protection claim; no main Zanance package is installed, started or changed.
Keys are randomly generated at runtime and never printed. Only wrapped fictitious Android key material is persisted.

Windows evidence: four AT-75 tests pass on .NET 10.0.12, Windows 10.0.26300, x64. Actual SQLCipher 4.5.2 Community,
SQLite 3.39.2, bundle 2.1.11, Microsoft.Data.Sqlite.Core / EF Sqlite.Core 10.0.12. Tests cover create/write/read/reopen,
wrong/missing key, changed ciphertext, live WAL/SHM inspection, integrity, direct key rotation, plaintext-to-encrypted
sqlcipher_export, an EF entity SaveChanges/LINQ read, CurrentUser DPAPI and authenticated PBKDF2 key envelopes.

| Metric: 500 fictitious rows, each with 2 KiB attachment-like BLOB | Windows sample | Android second-process sample |
|---|---:|---:|
| Plain create/write transaction | 39 ms | 430 ms |
| Encrypted create/write, verification, live sidecar scan and checkpoint | 57 ms | 161 ms |
| Keyed reopen plus row/text/amount/BLOB checks | 22 ms | 33 ms |
| Encrypted DB bytes | 2,056,192 | 2,056,192 |
| Live WAL / SHM bytes | 2,076,512 / 32,768 | 2,076,512 / 32,768 |

These single-run operations are not equivalent benchmarking workloads; cold/warm order and scanning affect timings.
They prove feasibility, not an overhead ratio or startup SLA. Plaintext control contains the marker; encrypted DB,
WAL and SHM do not contain UTF-8/UTF-16 marker or schema text. SHM remains unencrypted structural metadata.
No absence-of-marker scan is a cryptographic audit. Integrity and row/amount checks supplement it. File-based SQL
temporary storage is explicitly disabled with temp_store=MEMORY; native compile-time defaults still need release
qualification. Real ledger migrations, compiled Zanance model, large-data stress and interrupted-migration recovery
are not inferred from these tests.

Android evidence: **passed on Android API 36, x86_64, Google APIs system image revision 7**, using the existing
installed SDK/emulator and a new isolated AVD under artifacts/encryption-proof. No download was needed and existing
AVDs were not used. The signed independent Release AOT/trimmed APK ran on .NET 10.0.11 with SQLCipher 4.5.2 Community
and SQLite 3.39.2. Both fresh runs passed the common encrypted-file, wrong/missing-key, integrity, tamper, rekey and
export checks. The first process created a fictitious profile with its random DEK wrapped by Android Keystore
AES-GCM. After force-stopping only the fixture, a different process recovered the envelope and reopened that same
profile; wrapped-envelope and database SHA-256 hashes stayed unchanged. Altered envelope ciphertext was rejected
on both runs. Stale reports are removed before each run; the PASS label was also read through Android uiautomator.
The main Zanance package was absent from this isolated AVD. No real profile or financial file was opened.

Signature/package, embedded assembly stores, ZIP integrity and absence of INTERNET permission are verified.
Both arm64-v8a and x86_64 SQLCipher ELF load segments are aligned to 16 KiB; that structural check is not a
16 KiB-page runtime test. Only x86_64 execution is verified; ARM64 phones, hardware-backed/authentication-bound
keys, reboot/key invalidation and iOS remain later gates. Android exercises native/ADO.NET and Keystore, not the
full Zanance EF compiled model. Fixture APK SHA-256: c948531a50d53bdedec2e7f904a8c1ecb3bccbd6277d926647b1c6f9171908a6.
Raw first/second-process reports and Windows result are retained locally under artifacts/encryption-proof.
Main Windows/Android builds pass the CI warning policy with zero warnings/errors; all 1,134 main tests pass,
and test output was cleaned. Production sources and restore graph remain free of the experiment provider.

## Safe migration outline (SEC-05 design only)

1. **Preflight / quiesce:** enumerate every profile using validated ids. Pause auto-post, reminders, profile switching,
   backup/restore, reads and writes; show a covered progress state. Verify source integrity and enough free space for
   encrypted candidate plus encrypted rollback/safety data and worst-case WAL growth. No static startup migration.
2. **Prepare keys:** persist and re-open a versioned wrapped DEK first. Verify recovery policy/options before an
   irreversible switch. Missing/corrupt envelope for an existing encrypted profile is a blocked state, not a new DB.
3. **Create candidate:** keep source immutable; checkpoint WAL under an exclusive coordinator, close pools. Export
   consistently through SQLCipher into a same-volume encrypted candidate with explicit key/settings. Do not copy
   only the main DB while WAL is active. sqlcipher_export does not replace explicit user_version/application_id and
   EF migration-history checks. Do not reuse the current unkeyed backup snapshot path.
4. **Verify candidate:** reopen using recovered envelope DEK in a fresh connection; integrity plus cipher integrity
   check where supported; schema/migration version, ledger row counts, signed minor-unit/currency totals, settlement
   uniqueness, attachment sizes/content hashes and profile settings agree. Old backup schemas migrate in staging.
5. **Durable prepare:** flush candidate/envelope and directory metadata with platform-appropriate primitives.
   Persist a versioned migration manifest (profile, old/new generation, state, non-secret checksums) atomically.
   Filesystem durability across OS/key storage is a multi-resource protocol; a rename alone is not proof of it.
6. **Commit pointer:** atomically select the validated encrypted generation through a small versioned manifest or
   tested same-volume rename sequence. Startup reads this state before any context. Retain an encrypted verified
   rollback generation; never announce completion while a profile still points to plaintext. Refuse old plaintext-only
   app downgrades rather than silently decrypting the new generation.
7. **Cleanup:** after reopen/read verification, remove obsolete plaintext originals and sidecars. Delete only files
   named by the owned migration manifest; never broad directory cleanup. Flash/SSD secure erasure cannot be promised.
   Record cleanup-pending state for retry; explain residual old plaintext copies/OS backups rather than claiming all
   previous copies have become encrypted. Resume writers only after the selected generation/key pair is valid.

| Crash point | Restart behaviour / rollback |
|---|---|
| Before candidate verification | Original profile remains authoritative; incomplete candidate can be discarded after ownership checks |
| Key persisted but no candidate | Retain/delete only the unreferenced candidate key according to manifest; original untouched |
| Verified candidate before pointer commit | Reverify envelope/candidate; complete switch or stay on original, never merge sidecars |
| Pointer committed, cleanup incomplete | Open only committed encrypted generation; retry plaintext cleanup; retain encrypted rollback |
| Envelope missing / corrupted after switch | Stop, offer approved recovery/backup path; never revert to a plaintext fallback |
| Low disk / export failure | Leave source/pointer intact; remove owned incomplete candidate; keep diagnostics free of financial data |
| Restore on another device | Validate portable package, import into a new encrypted staging DB with a new local DEK, verify, then switch |

Rollback before switch is the unchanged source. After switch it is a verified encrypted generation and matching key;
rollback to an old plaintext-only executable is not a supported data recovery strategy. Existing unprotected safety
copies must be reconciled in SEC-08 before any claim that the local finance boundary is encrypted. A multi-profile
upgrade requires per-profile manifests plus an application-format gate so inactive profiles cannot bypass migration.

## Gates and owner review

ZCR-SEC-01 delivers this proposal and measured independent evidence. Acceptance of ADR 0010, paid build procurement,
OD-04 recovery semantics and OD-10 OS-backup policy remain owner decisions. Production adoption requires exact
maintained-binary Windows/Android/iOS tests, native 16 KiB page-size qualification, EF compiled model/migration tests,
crash/disk-pressure migration injection, real device/biometric/key-loss runs, provider fallback rejection, package
notices and startup/performance budgets. Encryption is not a subscription or online-account requirement.

Proposed next section: **ZCR-SEC-07**, excluding sensitive OS backup/transfer data as immediate risk reduction, after
the owner reviews this ADR and chooses OD-10. This document does not authorize that implementation.

## Primary sources

Checked on 2026-10-08; prices are planning estimates, not purchase approval.

- [Microsoft.Data.Sqlite encryption](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/encryption)
- [EF SQLite supported engine baseline](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/)
- [Legacy bundle status](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlcipher)
- [SQLitePCLRaw maintainer's current encryption/build policy](https://github.com/ericsink/SQLitePCL.raw#encryption)
- [SQLCipher security design](https://www.zetetic.net/sqlcipher/design/)
- [SQLCipher Community licence](https://www.zetetic.net/sqlcipher/license/)
- [Zetetic Commercial pricing](https://www.zetetic.net/sqlcipher/commercial/)
- [Official .NET platform/provider integration](https://www.zetetic.net/sqlcipher/sqlcipher-for-dotnet/)
- [SEE licence and cost](https://www.sqlite.org/purchase/see)
- [SEE modes and packaging](https://www.sqlite.org/see/doc/trunk/www/readme.wiki)
- [Android Keystore](https://developer.android.com/privacy-and-security/keystore)
- [MAUI SecureStorage platform behaviour](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage?view=net-maui-10.0)
- [Windows DPAPI user scope](https://learn.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection)
- [Apple Keychain accessibility](https://developer.apple.com/documentation/security/restricting-keychain-item-accessibility)
- [OWASP KDF guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
