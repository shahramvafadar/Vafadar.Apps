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
