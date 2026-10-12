# Reviewed atomic account archive - D-164 / AT-166

Account archive is now one reviewed SQLite transaction: original archive state, affected active/paused
plans in either account direction, exact default cleanup and original persisted activity choices commit
or roll back together. The reader captures complete original account/plan values and the local day without
creating settings. The writer reacquires/rechecks complete inputs under the actual file's writer and rejects
retired rows, day, cached access, another file or a different original notification owner.

Original Account/Plan feature/capacity decisions are shared, not duplicated. Archiving ends related plans
with established PlanActions; unarchive consumes the original account slot but never resumes ended plans.
Another concurrently chosen default remains untouched. Both original stores publish only after commit;
publication failures retain the accepted result and cannot cause a repeat. The UI blocks concurrent actions,
requires explicit consent before discarding a dirty draft, never silently saves those financial inputs,
and freezes an accepted result with Close after follow-up/return failure.

17 AT-166 cases pass. Final main: 2,963 passed, App.Tests 906, zero failures/skips, 2m 10.406s, followed by
cleanup. Strict Windows and complete signed Android Release have zero warnings/errors. 36 actual Windows
contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced with original native Archive/Close,
real SQL rollback and actual accepted account/plan/default writes over independent fictitious rows.
Explicit fictitious consent/alert/return effects are identified. Actual normal Android consent-decline
and exact owned restoration remain separate from typed fixture/physical-device acceptance. All four
changed production-source PDB checksums match. No EF property/schema, migration, permission, SDK,
credential, network or portable-format change. Production registration stays inactive; initial readiness
and other commercial paths remain open, not release completion.

## Actual policy and write boundary

ReadAccountArchiveAsync uses a deferred original SQLite reader and complete account/affected-plan packets,
not just audit times. It reads active and paused plans from both AccountId and ToAccountId and demands the
original history right. WriteAccountArchiveAsync verifies the exact configured file and notification store,
acquires one actual SQLite writer even when commercial registration is inactive, then rechecks original
packets, day and cached facts. Current account permission/capacity and original Plan whole-batch checks are
reused. Ended plans remain unchanged; ordinary unarchive changes no Plan lifecycle. Original default is
cleared only if it still names this account, with no guessed replacement or settings-row creation.
Account, Plans, exact default and stored choice edits use one SaveChanges/commit. A real refusal in any table
rolls back the whole operation and publishes neither store. Both stores are notified after success even if
the first subscriber fails; the typed exception exposes the accepted immutable result rather than hiding
failure or pretending that it did not save. No financial ledger, loan principal or estimated money is posted.

## Original UI and evidence

The original Archive/Unarchive button spans a single busy lifetime through read, consent and writer. A dirty
form requires explicit discard consent; declining retains every raw field and all rows. Archive uses stored
identity/financial fields, not the dirty form payload. Successful actions reload accepted stored fields,
freeze financial controls and expose Close. Late return/notification failures cannot resubmit an archive.
Current-state mismatch or stale review reports translated no-change feedback; quota rejection uses the
established structured reason. Permission/source errors never leak exceptions or original stored ids.

AT-166 covers six real resource languages, declined/accepted dirty forms, three real SQL trigger failures
with persisted original account/plan choices, complete same-audit-time edits/new related plans/day changes,
a concurrently selected other default, no settings creation, quota-blocked unarchive, accepted publication
failure to the first subscriber with both stores notified, and foreign-file/notification-owner/access guards.
Real SQLite and actual application sources are used; complete 25-table packets verify rollback.

Windows review invokes the original Archive/Close targets. Fictitious consent/alert and return refusal are
explicit native-effect ports. Each independent language file proves declined input retention, actual SQL
trigger rollback, original stored name/balance retained after accepted archive, both Plan states ended,
exact default cleared, and no duplicate write. Pinned complete reasons are measured in realized native
controls and own hidden windows. Exact original development bytes/sidecars restore; no owner desktop
input/focus, full-screen capture or device settings. Accepted writes belong only to independent fictitious
files. Native OS scaling/screen-reader and physical-device acceptance remain separate.
Evidence: artifacts/account-archive-all-tests.log, clean.log, windows-build.log; both matrix mode logs and
36 account-archive native proofs/captures. Representative own-window captions are visually inspected.

Android normal Release opens the original owned QA03 account/edit route, invokes the actual archive consent
and selects Keep editing. It cancels the unchanged editor and returns without money or status changes.
All original non-migration rows remain equal, with only existing D-141 migration/empty ResourceSelections.
Main/English/System and exact owned original QA03 bytes/sidecars restore. Old Debug is installed/stopped only
for run-as capture, never launched; final signed Release is installed/stopped. This proves the real native
consent/decline adapter, separately from fictitious accepted-writer failure effects on Windows.
Evidence: artifacts/account-archive-native/final-proof.json and actual native hierarchy captures.

## Preserved negative evidence

The first test preparation concatenated a fixed theory table into a raw trigger command. Strict analysis
rejected it; the final source uses explicit literal commands for the three owned tables without weakening
analysis. No tests ran in failed compilation. Other failed native preparations remain negative evidence,
with their same-process resumed observations; unchanged successful checks are not repeated.

## Signed installable APK

`artifacts/android/zanance-d164-release.apk`, 82126763 bytes, SHA-256 `8bec057122560fc58e27334413f594bfe892e797790bb58a60c419dae2653b1e`.
Non-debuggable pro.vafadar.zanance, API 24/36, full arm64-v8a/x86_64 stores/application AOT. v2/v3 verify with
the existing local test certificate 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
All four changed production-source PDB checksums match; original permission/cloud policy remains unchanged.

## Remaining scope

Next ready slice: original Account/Plan initial readiness before an attempted write, with full dirty drafts,
correction/history and actual file rights preserved. Other enforcement/import/restore/native coverage,
verified billing/provider facts and activation remain separate. Encryption/library/recovery decisions,
physical phone, iOS/Mac/signing, real OAuth and Store acceptance remain owner/external gates.
