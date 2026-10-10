# Categorization rule rights and serialized patterns - D-125 / AT-127

ENT-02 remains in progress. Both the rule form and the transaction detail's save-rule action use the guarded
ZananceStore service. Current registration remains inactive without a paid grant; test builds stay unrestricted.
No model/schema, migration/compiled model, SDK, permission, visible string/layout or portable entitlement change.

## New automation and retained corrections

New text/kind configures categorization automation and requires CategorizationRules when checks are enabled.
Changing the category of a stored same-text/kind pattern uses Corrections, including the form/detail path that
supplies a new rule id for same-pattern replacement. Delete uses DeleteData. Free/expired-host data rights remain
available, with exact accepted membership still required; personal Pro cannot replace membership. An existing-id
change to a new text/kind is not a retained correction. Another kind with the same text remains a distinct pattern.
Suggestion delivery and explicit selected read-only data remain separate unfinished work.

Preserve existing trim/case matching, new-id replacement semantics and category suggestion behavior. An existing-id
update retains its stored CreatedAt. Reject before trimming the draft, removing old rules or auditing rows. Rules
never reclassify saved transactions; failed or refused operations leave ledger, accounts and settings unchanged.

## One actual SQLite writer before matching

Previously, same-pattern matching preceded Save's implicit transaction, permitting independent providers to read
an absent pattern before either insert. Acquire the actual-file writer before matching/replacement, including
inactive operations. Capture cached access before the writer wait; never read a subsequently selected profile or
do purchase/network work in the transaction. Enabled Delete uses the same actual-file boundary. Recheck after SQL
before commit; SQL failure or retired facts rolls back complete rules and replacements. Changed follows commit.
No process semaphore or fake count substitutes for SQLite serialization.

## Real SQLite tests and negative evidence

28 new AT-127 cases pass; main suite passes 1,862 with zero failures/skips. App.Tests remains 299. Cover inactive
unrestricted rules; Free new-rule refusal before trim/audit; three personal/shared paid creation contexts; retained
correction/deletion and same-pattern new-id replacement after downgrade/host expiry; two existing-id new-pattern/
kind refusals; distinct kind identity; four missing-membership paths; two mismatched-file paths; four trigger-driven
post-SQL access retirement rollbacks; two inactive/enabled replacement SQL failures and retries; two independent
same-pattern writer races; initial-file binding after a profile provider moves; and short-text validation before
writer acquisition. Compare complete columns of all 24 stored tables, excluding only accepted rule changes where
appropriate. Triggers use actual production-factory native connections; cached rights are independent fixture facts.

The initial focused test compilation reports an unassigned nullable rendezvous field under warnings-as-errors;
give that fixture field an explicit initial null. The first runnable policy regression then fails because no
CommercialWriteRejectedException is thrown: the previous service saves a new rule in an enabled Free context.
After the service change that regression passes, followed by all 28 and the main suite. Negative and passing logs:
artifacts/rule-policy-before-tests.log, rule-policy-before-tests-corrected.log,
rule-policy-after-rejection-tests.log, rule-policy-focused-tests.log and rule-policy-all-tests.log.
Test cleanup: artifacts/rule-policy-tests-clean.log.

## Platforms and actual installed Release

Strict Windows and Android builds pass with zero warnings/errors. Test cleanup also passes without warnings/errors.
Canonical Build-AndroidApk.ps1 completes full signed Release with its warning/error guards; the production store
hash remains unchanged during all final checks. Logs: artifacts/rule-policy-windows-final-build.log,
rule-policy-android-final-build.log and rule-policy-android-final-release.log. There is no visible control/string/
layout change requiring a fresh language/theme/width matrix; retain completed reviews. Policy test failures do
not claim native commercial error-dialog acceptance; translated limit feedback remains ENT-04 work.

Use only independently owned emulator-5570 and its exact sample database, preserving original database/sidecar
bytes without additional financial fixture seeding. In normal signed Release, More / Categorization rules opens
the real form. Enter RulePolicyNative with the default Expenses / Housing and invoke Add. The real row appears;
read back all columns of all 24 tables. Only that new CategoryRules row differs; ledger, accounts, categories,
settings and every unrelated table match. Restart complete Release and reopen the form; the saved row remains.
Enter the same pattern, choose Expenses / Food from the real native picker and invoke Add. The row is replaced
with Food; read back exactly one rule with the new id/category, preserving the established replacement contract.
All unrelated columns still match the original database.

Restart again and reopen: one saved RulePolicyNative / Food row remains. Invoke Delete; its actual confirmation
identifies that exact pattern/category. Confirm Delete; the row disappears. Read back all 24 tables: all original
columns and rows now match, including audit values. Restore exact original database/sidecar bytes, reinstall full
Release, return to Home and stop it. No profile/security storage, desktop input, screenshots, system settings or
physical device is touched. Evidence: artifacts/rule-policy-release-native/proof.json, created/replaced/deleted
proofs and fresh native hierarchies. The unchanged transaction-detail save-rule caller was source-checked to use
this same store; it was not separately invoked in this native review. Enabled rejection/rollback is real SQLite
test evidence, distinct from the current inactive deployment's normal form evidence.

Phone-test artifact: artifacts/android/zanance-d125-release.apk, 81,446,997 bytes. SHA-256:
`241ec7ebc920539c8099a7e0d26371e5fc2cdcf0c70ed9a9ae8450d17067cca9`.
ZIP integrity, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permissions and v2/v3 signature pass. Existing local test signing is not production signing.
Evidence: artifacts/rule-policy-apk-proof.json. Emulator/build evidence is not physical-phone, iOS, OAuth,
store or product-release acceptance.

Remaining ENT-02/03/04 includes rule suggestion delivery, explicit selected read-only resources and imported/
restored new-work classification, future budget activation, profiles, occurrence/contribution automation, other
native entry points and translated commercial feedback. Billing, encrypted storage and owner/external release
decisions remain unfinished.
