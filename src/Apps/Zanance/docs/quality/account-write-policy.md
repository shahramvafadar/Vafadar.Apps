# Actual-file account write policy - D-118 / AT-120

This is the first verified write boundary of ENT-02, not completion of the whole section. It extends the final
OD-03 Core model with a reusable Data transaction gate and the real SaveAccountAsync operation. There is no visible
UI change, schema/migration, SDK/permission, paid fact in backups, active-item selection or destructive downgrade.
The current provider is explicitly inactive; test builds retain unrestricted behavior with no fabricated Plus grant.

## Enabled write contract

Capture an already-resolved synchronous access snapshot for the actual SQLite connection DataSource, not a later
current-profile preference. Enabled facts must match the exact normalized file spelling. Acquire the writer before
existing account/count reads; check commercial permission, count active accounts only for creation/unarchive with a
finite maximum, save and commit before Changed. The same store serves onboarding, account and debt forms. Unlimited
contexts avoid count reads. Retired/mismatched access is rejected; failed owned transactions roll back. This gate
owns only its own transaction and never commits an outer caller's transaction. No network/provider work runs here.

Existing corrections and archival stay possible above quota. Active accepted matching membership is still required;
a personal Pro right cannot bypass it. New archived history does not consume a slot; unarchive rechecks capacity.
The helper does not choose active accounts, archive/delete data or implement still-open shared roles/retention.
Read-only choices and ledger work on unselected resources remain later ENT-02/03 integration, not an account claim.

## SQLite acceptance

15 cases pass, tagged AT-120. Full main suite: 1,626 passing, no failures/skips; App.Tests 299 unchanged. Current
inactive registration permits ten active accounts with no paid context. Enabled Free rejects the fourth before
writes/Changed and preserves complete account metadata and entries. Over-quota corrections/archive, unarchive and
freed slots, Plus/Pro/exact active shared capacity, expired-host and missing-membership checks, wrong-file/case-
variant and changed snapshots, required-column failure/rollback/retry and original currency lock all pass.

The contention test uses two independent DI providers/context factories, actual SQLite, two worker tasks and a
barrier that confirms both initial captures precede writer acquisition. Exactly one final-slot write commits and
raises Changed; the other receives the structured quota failure with observed count three. This is database-writer
evidence, not reliance on a store-instance semaphore or a fake count/read implementation. Snapshot paths are checked.
Evidence: artifacts/account-policy-all-tests-final.log. The earlier 14-case suite and initial 12 focused cases are
retained; final case-sensitive binding corrects the cross-platform file boundary and adds the fifteenth case.

## Platform and normal Release verification

Final strict Windows and Android builds pass with zero warnings/errors. The canonical Build-AndroidApk.ps1 completes
Release; final source hashing confirms the guard did not change during these builds. Logs:
artifacts/account-policy-windows-final-build.log, account-policy-android-final-build.log and
account-policy-android-final-release.log. Test output is cleaned with zero warnings/errors afterwards.

The full signed Release uses the actual native account form to save a fourth active account on the independently
owned emulator-5570, confirming current deployment is unrestricted. Prepare only one fictitious account beside the
actual two-account sample baseline; select the real Name/opening-balance fields, type the fictitious name and zero,
and invoke Save once. Complete all 24 tables match the prepared data except exactly this new account; all original
rows and fixture metadata match, with no ledger additions or preference/security changes. Exact original database
and sidecar bytes are restored; full Release returns to normal Home and is stopped. Evidence:
artifacts/account-policy-release-native-verified/proof.json. The first helper stops before Save because it expects
raw text without Android's semantic field prefix. Keep that negative capture; the corrected expectation checks the
actual Name caption plus entered value. No production or form workaround was added. No visible control changed,
so completed earlier layout matrices are retained instead of repeated.

Phone-test artifact: artifacts/android/zanance-d118-release.apk (80,988,075 bytes), SHA-256
`e18f91a3ed0c068002a2d7bfb6fb6ee7746f6416a8071a3f3298c7bff0da1df0`. ZIP integrity, full ARM64/x86_64 assembly
stores/app AOT, non-debuggable pro.vafadar.zanance package, unchanged cloud-build permissions and v2/v3 signature pass.
The certificate is the existing local test certificate, not production signing. Evidence: artifacts/account-policy-apk-proof.json.

Remaining ENT-02 work: goals/plans/budgets/templates/filters/profiles/holdings; import/restore above-quota data and
read-only selection; automatic work/deep links/widget/OCR; approved activation and final translated limit UX.
Physical-device, iOS, purchases/server/role, encryption and release acceptance remain separate gates.
