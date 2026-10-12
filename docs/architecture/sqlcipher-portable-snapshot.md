# Memory-only portable SQLite boundary (D-186)

Status: reusable final infrastructure, not activated for the main application. SQLCipher Community
build/probe identity remains documented in the native guides; real databases remain plaintext.

SqlCipherPortableSnapshot.WriteAsync receives the already open, captured keyed connection and
caller stream/cap. It refuses an existing transaction, attaches a private plaintext memory alias,
uses one read transaction for schema/rows/header copying through official sqlcipher_export, then
serializes the memory alias. user_version/application_id are explicit because export does not
carry them automatically. No plaintext temporary database path is created. Output protection stays
in the existing portable package layer, preserving owner D-62 optional password semantics. This
class does not remember keys or decide profile authorization; current backups still use their old path.

SqliteMemorySnapshot.OpenAsync reads a capped plaintext portable image, checks header/integrity and
creates an owned read-only native memory allocation. WAL-mode header bytes are normalized only in
that owned copy, following the pinned SQLite sqlite3.h deserialize contract; input bytes stay intact.
Owned managed/native buffers are cleared on normal disposal/failure and freed with SQLite's allocator.
Plaintext necessarily exists in memory; SQLite/provider/managed strings/page-cache memory is not
claimed universally erased. The explicit caller cap is infrastructure, not a new product quota.

ExportEncryptedAsync reserves one exact new caller-owned staging path (CreateNew), refusing existing
files/sidecars, binds the key as a parameter and uses official export. Interrupted candidates are
not committed or silently promoted; their coordinator owns explicit cleanup. Existing profile
files are not replaced here. Full financial/schema/header/cipher integrity verification, durable
key envelopes, exclusive profile operations, pointer commit, restart/rollback and cleanup protocol
remain to integrate. Do not treat a new encrypted candidate as completed whole-app migration.

The first plaintext memory BackupDatabase-to-cipher attempt returned SQLite error 1, unsupported
format conversion. Negative evidence remains. Official export succeeds instead, with preserved
original Unicode/schema/EF migration history/user_version/application_id and keyed readback.

Six automated behavior cases pass (Data.Tests 22 total), full repository 3,185/no failures/skips,
main strict Windows/Android builds zero errors/warnings. Shared original-source/schema probe passes
on Windows and a separate signed Release AOT/trimmed Android x64 package with memory export/header/
history/encrypted restore. Only the independent package was removed after evidence capture. No UI,
main profile/security storage, live backup/provider or OS policy was changed. No unchanged screen
matrix is repeated and no physical/API24/16 KiB/arm64/iOS/recovery/crash acceptance is inferred.

Records: artifacts/native-sqlcipher/{windows-portable-snapshot.log (failed generic conversion),
windows-portable-export-restore.log,memory-data-tests.log,full-memory-tests.log,
main-memory-windows-build.log,main-memory-android-build.log,android-portable-memory-run.log,
memory-tests-clean.log} and build/android-d/managed-android-proof.json.
