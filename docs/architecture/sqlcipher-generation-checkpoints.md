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
