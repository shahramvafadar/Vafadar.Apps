# Selected accounts and retained settlement write policy (D-131 / AT-133)

## Scope and actual boundaries

Approved OD-03 choices are modeled for accounts/goals/recurring plans/templates/filters/device-local profiles.
Original identities and pause/archive/end state remain unchanged; pauses consume slots. Stale/oversized choices
and missing choices above finite capacity require explicit review; unlimited upgrades restore availability. Budget
definitions/hosting keep separate policies. Choice is not a capability or membership grant.

The existing actual-file ledger writer now checks new money's source and transfer destination against that cached
immutable choice, rechecking facts before commit. Retained edits, reconciliation, refunds, fees and Delete/Undo keep
their original rights. Explicit reviewed overdue payments are classified from actual stored rule/slice/state,
matching kind/accounts and previous complete payments, never from a caller flag alone. Future/moved-future,
unreviewed/skipped/completed/outside-rule payments cannot acquire this retained right. Partial/final past payments
remain possible after downgrade/host expiry; exact shared membership remains required.

Native bill Save now rereads the actual expense plan and complete stored advances/refund totals in that writer,
compares reviewed plan/advance metadata and paid total, and privately creates only the existing algorithm's
difference. No caller-supplied ledger marker grants this correction. Failure preserves the explicit-Save draft and
the existing translated generic error. Matching bills write nothing. Existing complete ledger validation, paid
updates, attachment movement, writer rollback and Changed boundary remain shared.

## Automated evidence

24 Core and 55 actual isolated SQLite cases added; complete main suite 2,051 pass, App.Tests 318, no failed/skipped.
Compare all 24 complete tables/columns, including auditing and receipt bytes, for rejection/rollback. Tests cover
active/paused/history choices, source/destination accounts, retained fees/Undo, scope/member/host separation,
verified overdue full/partial payment, forged/future markers, reviewed extra/refund bills, stale basis, no-advance/
even bills, SQL failure and retired cached choice after SQL with no Changed and explicit successful retry.
Initial four isolated enabled over-capacity baseline cases failed because the old writer accepted new money;
the final guard passes. Test/setup compile and exact EF-wrapped SQL-exception expectation corrections are retained
as negative evidence, not acceptance. Main final log: artifacts/selected-account-all-tests-final.log.

## Native and package evidence

Windows: en/fa/de in light/dark at 412 px, plus German 360 px/process-local 2x and German wide: eight contexts
with 96 actual native Save invocations. After narrowing generic feedback to writer failures only, a focused final
English 412 px review repeats 12 native Saves. Nine Windows contexts/108 invocations total; three rejected bill
Saves per context compare complete 24-table data/drafts, and the existing valid helper checks extra/refund/zero-bill
navigation. Existing scalable translated failure text and the original enabled Save target remain readable.
Own-window images were reviewed; development database files restore with matching hashes.

Installed normal signed Release candidate: en/fa/de, light/dark, six successful bill Saves against four retained
accounts, with the precise extra payment or linked refund and every original column retained. The final APK's
focused English review adds an ordinary explicit expense with four accounts/no choice (confirming current
unrestricted testing), then the owned bill difference; a real sample SQLite insert failure and explicit retry keep
the native amount and existing translated error/Save, with no complete-row change. Native Cancel/Discard returns
normally. Exact original sample database/sidecars and English/System display choices restore; full final Release
is stopped on Home. Only emulator-5570 and its exact independently owned sample database are operated.

The candidate failure fixture stopped before app startup after a transient adb-daemon connection failure, with no
financial Save; it is negative evidence, not acceptance. One read-only inventory recovered connectivity. Original
bytes/display were restored before final-package checks. Bounded unavailable hierarchy reads reacquire the same
actual native navigation/result without repeating a financial Save. No daemon/device reset or security flag change.

Final main 2,051/App.Tests 318 pass; cleanup and strict Windows/Android have zero warnings/errors. The canonical
Build-AndroidApk.ps1 produces the complete non-debuggable arm64-v8a/x86_64 Release, with both assembly stores/App
AOT, package pro.vafadar.zanance, min 24/target 36, archive integrity and v2/v3 signature verified. Local test
certificate: 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b; no production certificate claim.
APK: artifacts/android/zanance-d131-release.apk; 81,033,131 bytes;
SHA-256: 6d513770e089132bac326979ea5cac219dbbb1cdfb134083d1d7f38fb891feb2.
Local proof: selected-account-apk-proof.json, selected-account-windows-proof.json, final native proof directories,
selected-account-{all-tests-last,tests-clean-last,windows-final-build,android-final-build,android-final-release}.log.

Writer rejection feedback is separate from navigation after commit; a later navigation exception is not relabeled
as failed persistence. Successful native navigation is checked; no injected post-commit navigation failure is claimed.

## Limits

Current app registration remains inactive; no test-build restriction or fabricated paid right is introduced.
Native flows therefore prove retained bill delivery in the actual unrestricted app; enabled selection checks are
real SQLite service tests. Selection UI/persistence, other resource bindings, budget-definition choice, automation,
limit messages and ENT-02/03/04 completion remain open. No schema/migration/compiled-model, new SDK, permission,
portable preference or backup format change. Physical-phone, iOS, OAuth, billing, encryption, CI and product release
acceptance remain separate. Native fixture data is fictitious and independently owned; preserve original files.
Manual debt-closing classification is delivered separately by D-132
([evidence](retained-debt-closing.md)); remaining occurrence-state/automation boundaries stay unfinished.
Inactive registration prevents a current test-build restriction.
