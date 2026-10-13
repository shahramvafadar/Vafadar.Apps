# Database generation checkpoints (D-188)

DatabaseGenerationCheckpoint is the immutable non-secret protocol model for one profile and two
explicit generation identities. Its constructor rejects empty or identical generations. Advance
requires the original profile/candidate and exactly the next phase; completed states cannot advance.
Original remains selected through KeyPrepared and CandidateVerified. Selected and Completed name
only the candidate. A missing candidate key must never imply a plaintext fallback.

The coordinator, not this model, must persist and reopen each external milestone. KeyPrepared
requires a durable platform key binding, CandidateVerified requires the complete staging verifier,
Selected requires the durable selector, and Completed requires exact obsolete plaintext cleanup.
The model cannot certify those effects, open files, authorize profiles or choose recovery policy.
No durable serializer, filesystem selector, key adapter or startup composition is activated here.

Eight automated behavior cases cover ordered selection, immutable original state, skipped/repeated
phases, profile/candidate drift, invalid identities and terminal-state refusal. Full test/build
results are recorded after verification in the decision and acceptance records. This foundation
has no UI change; live app databases remain plaintext. OD-04 recovery and OD-10 final OS-backup
policy remain owner gates. Restart/fault injection requires the future actual durable coordinator.

Strict main Windows and Android builds pass with zero errors and warnings.
Evidence: artifacts/native-sqlcipher/d188-{full-tests,windows-build,android-build}.log.

## Resumable coordinator (D-189)

DatabaseGenerationMigration.ResumeAsync holds a caller-owned exclusive session across the whole
transition. It reads actual persisted state, rejects foreign generations/regression, then prepares
and reopens the key, creates/verifies the candidate and reopens it before selecting. Each checkpoint
write is compare-and-commit followed by exact readback. A returned write without matching readback
stops before subsequent effects. Retired context and cancellation are rechecked after awaits.

Selected restart always reopens the candidate with its key before exact obsolete plaintext cleanup.
Missing keys fail closed; there is no original/plaintext fallback. Interrupted native operations
must be idempotent in the adapter: a committed selection whose return was interrupted is discovered
by the next read. Completed restart only reopens the candidate; it does not repeat cleanup. Writers
must remain gated until the caller receives success and validates its final selected connection.

IDatabaseGenerationMigrationSession is the native-effect boundary. Its implementation must own
real durable key binding, coherent verified snapshots, filesystem selection/readback and exact
cleanup, with an exclusive profile writer lease and cached authorization/context validation.
No implementation or startup/provider composition is activated in this slice. The faultable unit
port does not prove filesystem, power-loss, SecureStorage or native restart durability. Those
remaining adapter tests and OD-04/OD-10 decisions are still required before app migration.

Fifteen behavior cases exercise interruption after eight effects/commits, cleanup ordering,
missing selected key, false successful persistence, retired await context, identity/regression,
completed restart and cancellation. Focused Data.Tests: 57 passed. Full suite/build results are
recorded in AT-188 after execution; existing native/UI matrices are not repeated without changes.

Native selector storage must be chosen from tested platform primitives, not assumed from a managed
rename alone. SQLite's [atomic commit](https://www.sqlite.org/atomiccommit.html) and
[synchronous settings](https://www.sqlite.org/pragma.html#pragma_synchronous) describe why VFS,
file/directory sync and actual hardware behavior matter. These references do not authorize a new
metadata database/schema or certify an unimplemented selector adapter.

D-189 strict main Windows and Android builds pass with zero errors and warnings.
Evidence: artifacts/native-sqlcipher/d189-{coordinator-tests,full-tests,windows-build,android-build}.log.

## Current policy after D-192

OD-04 and OD-10 are now approved, superseding their earlier pending references above. Continue
native storage/migration/recovery engineering without asking these policies again. Windows key
protection is implemented separately; durable session adapters and app activation remain incomplete.

## Transactional selector storage (D-195)

SqliteDatabaseGenerationJournal is the actual non-secret durable protocol file for a captured
private path and bound profile/original/candidate tuple. It does not contain keys, financial data,
paths, labels or portable preferences. SQLite/VFS transactions are chosen over a custom binary
log or unsupported Android hard-link publication. Application ID 0x565a474a (VZGJ), user_version 1,
one exact generation_state schema/singleton and phase 0..4 are validated. This protocol format is
independent of the unchanged EF financial schema; no compiled-model or EF migration change.

CreateNewAsync reserves an exact new path, then commits original state. Existing/interrupted files
are never replaced. Read/commit use ReadWrite (not ReadWriteCreate), allowing SQLite hot-journal
recovery while refusing missing state. They validate original format/context before any journal
mode adjustment. DELETE and synchronous EXTRA are read back. According to
[SQLite's synchronous semantics](https://www.sqlite.org/pragma.html#pragma_synchronous), EXTRA adds
containing-directory synchronization after DELETE journal unlink. Physical storage must still
honor SQLite/VFS sync assumptions; this is not hardware power-loss certification.

CompareAndCommitAsync validates exactly one bound next phase, obtains an immediate transaction,
rechecks actual state, updates one row and commits. Subsequent coordinator readback/keyed reopen
remains required. Unknown, missing, damaged and foreign states fail closed rather than selecting
Original. The caller owns trusted path ancestors and an exclusive access/profile lease. Same-UID
malicious modification is outside this storage boundary; no claim of metadata cryptographic
integrity. A journal row never proves the candidate/key/content actually exists or authorizes
cleanup. No main provider/startup or OS backup policy is activated yet.

Thirteen behavior cases cover real isolated SQLite, all ordered reopen phases, stale commit,
no missing-state recreation, explicit no-replace initialization, three foreign identity cases,
unknown version/unexpected schema/missing row/invalid phase, cancellation and skipped advance.
Focused Data.Tests 82 pass. Test-GenerationJournal.ps1 -RunName windows-d and
Test-AndroidGenerationJournal.ps1 -RunName android-d run the same original code against reviewed
native candidates in five distinct processes each. Official sqlite3_db_cacheflush is used only
by the independent fixture to force an uncommitted page to disk; a bounded actual rollback-header
read proves the hot journal before the external process kill. Restart recovers CandidateVerified.
A separate actual store commit followed by external kill/restart retains Selected. The Windows
observer must allow SQLite's existing write handle (ReadWrite/Delete share); the failed first
observation is retained, not counted as crash acceptance. Android is x64/API36/4 KiB, not physical
arm64/API24/16 KiB, iOS, disk-pressure, envelope/candidate/cleanup or hardware-power-loss acceptance.

Windows proof: artifacts/native-sqlcipher/build/windows-d/journal-proof-af2cbb35a32548d3b9297abddc16c406/journal-process-proof.json.
Android proof: artifacts/native-sqlcipher/build/android-d/android-journal-proof-87dc7d500fab448794e70c30e9cd1e28.json.
Independent Release APK v2/v3 signature, ZIP and both native payload hashes match reviewed slices;
only its distinct package was uninstalled after proof. No main app files/credentials were touched.

D-195 final managed evidence: all 3,245 tests pass with zero failures/skips; test output cleaned.
Main strict Windows/Android builds pass with zero warnings/errors. Records under
artifacts/native-sqlcipher: d195-{journal-tests,full-tests,test-clean,main-windows,main-android}.log.
Independent native strict builds also pass; no visible main screen changes or phone-test handoff.
