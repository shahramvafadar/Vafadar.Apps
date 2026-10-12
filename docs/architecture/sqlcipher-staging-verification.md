# Complete SQLite staging verification (D-187)

SqliteSnapshotVerifier compares already open captured expected/actual connections through read
transactions. Caller owns exclusive generation/key coordination and final acceptance; this class
never chooses a profile, changes a pointer or offers a recovery policy. The actual connection's
integrity_check/foreign_key_check/cipher_integrity_check must report no damage. On plain SQLite the
unknown cipher pragma is empty; native cipher identity remains the separate provider gate.

Logical schema entries compare exact type/name/table/SQL, not physical page positions. All table
columns (including generated values, excluding hidden virtual inputs) are projected by SQLite
storage class and value, ordered by those same classes/values with binary collation. No guessed
primary key or row hash can discard duplicate multiplicity. Exact Int64, double bits and raw
UTF-8/BLOB spans compare directly without logging values or allocating full managed BLOB arrays.
Native statements finalize on cancellation/error; both caller connections remain caller-owned.

The first maintained driver GetBytes path on expression columns fatally accessed native column
metadata without a table origin (0xC0000005). Its stack/log remain negative evidence. Direct
sqlite3_prepare/step/column spans avoid that metadata path and compare the original bytes. No
source fork or weakened text-only/hash comparison replaces the required behavior.

Twelve added behavior cases and full 3,197/no failures/skips pass; strict main Windows/Android
builds zero errors/warnings. Original 32-migration/header/encrypted staging proof passes on Windows
and the separate Release AOT/trimmed Android x64 probe. Data.Tests now 34 cases. No UI or live
provider/backup/root selection changes; main databases still plaintext. This is not durable
copy/verify/commit/restart/key/recovery acceptance or arm64/API24/16 KiB/iOS qualification.

Records: artifacts/native-sqlcipher/{windows-full-staging-verify.log (fatal initial path),
windows-native-staging-verify.log,windows-integrity-staging-verify.log,staging-verifier-tests.log,
staging-integrity-tests.log,full-staging-tests.log,main-staging-windows-build.log,
main-staging-android-build.log,android-full-staging-run.log}, build/android-d/managed-android-proof.json.
