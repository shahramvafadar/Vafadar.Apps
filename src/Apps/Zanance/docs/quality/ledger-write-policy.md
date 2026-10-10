# Ledger Save rights and final refund totals - D-126 / AT-128

ENT-02 remains in progress. All three SaveEntriesAsync overloads and SaveEntryAsync reach one guarded actual-file
service. Registration stays inactive without a paid grant; test builds remain unrestricted. No schema/model,
migration/compiled model, SDK, permission, visible layout/string or portable entitlement changes.

## Complete groups and retained financial corrections

Acquire the actual SQLite writer before accounts, categories, existing entries, group and refund reads, including
inactive deployment. Capture cached access before waiting; keep the same actual file when the profile provider
moves. No network or purchase work enters the transaction. Reject before draft cleanup/audit.

Enabled new category splits and additional/relocated parts require SplitTransactions. Include untouched stored
siblings when saving one part, preventing incremental split creation from bypassing the feature. Retained parts,
corrections and combining preserve Corrections rights, including host expiry with exact accepted membership.
Personal Pro cannot replace membership. Holding payment/fee groups are distinct: retained edits use Corrections,
new fees require ManageHoldings. Transfer fees belong to a retained transfer correction, not category splitting.
Ordinary new entries use Transactions; adjustment, refund and income reversal retain corrective rights.

## Four reproduced refund defects

The previous service validates each refund against the old database, so two 7,000-minor-unit refunds on one 10,000
purchase are both accepted in one Save. It also accepts reducing that purchase below its stored refunds or
changing it away from Expense. Finally, a purchase of Int64.MaxValue, an existing refund of one and another refund
of Int64.MaxValue wrap the old addition and pass. Four focused real SQLite tests fail before repair.

Validate the final batch instead: untouched stored refunds plus incoming refunds, excluding replaced/deleted rows.
Resolve originals from incoming entries first, independent of batch order; missing targets retain the existing
translated validation error. Recheck incoming purchases against retained final refunds and kind. Widen exact minor
units before totaling/comparing; stored money stays Int64, with currency, parsing and rounding unchanged.
Valid original/refund ordering, simultaneous purchase/refund reductions, replacement refunds and the exact Int64
boundary remain accepted. This slice does not replace the distinct aggregate-import algorithm.

Entries, derived occurrence paid totals and receipt attachment moves stay in one writer/transaction. Cached access
is rechecked after SQL before commit. SQL failure or retired facts restores complete stored rows without Changed;
a retry succeeds. Commercial ledger delete/Undo, selected read-only resources and automation remain unfinished.

## Tests and preserved negative evidence

45 added AT-128 cases pass; main suite passes 1,907 with zero failures/skips, App.Tests 299 unchanged. Cases cover
inactive split/join metadata; Free rejection before mutation; three paid/shared contexts; retained correction/join;
extra/relocated/incremental parts; transfer and holding groups; four missing-membership overloads; corrective
movements versus new work; wrong/initial-file binding; post-SQL access retirement; attachment-move failure/retry;
invalid batches; independent same-id and refund writers; four refund regressions; valid final-batch edits/order/
replacement, missing targets, Int64 boundaries and derived occurrence paid-total rollback/retry.
Compare complete columns of all 24 tables, excluding only the exact accepted changed table where appropriate.
Use actual production DI/SQLite, with independent cached fixture facts and native-connection triggers.

Preserve the first failing Free-split test and passing repair: artifacts/ledger-policy-before-tests.log and
ledger-policy-after-rejection-tests.log. The initial 30 cases pass; adding the four amount regressions yields
30 passing/four failing in ledger-policy-refund-before-tests.log. All 34 pass after repair, then the final 45:
ledger-policy-focused-refund-fixed-tests.log and ledger-policy-focused-final-tests.log. Full suite and cleanup:
ledger-policy-all-tests.log, ledger-policy-tests-clean.log. Policy tests do not claim enabled native error-dialog
acceptance; translated commercial feedback remains ENT-04.

## Platforms and normal installed Release

Strict Windows and Android builds pass with zero warnings/errors; test cleanup passes too. Canonical
Build-AndroidApk.ps1 completes full signed Release with warning/error guards. Both production source hashes stay
unchanged during final checks. Logs: artifacts/ledger-policy-windows-final-build.log,
ledger-policy-android-final-build.log, ledger-policy-android-final-release.log and ledger-policy-final-source-hash.json.
No changed control/string/layout requires repeating completed language/theme/width matrices.

Only independently owned emulator-5570 and its exact sample database are used. Preserve original database/sidecar
bytes; seed one independently owned fictitious purchase SplitPolicyNative, retaining full note/payee/tags.
Normal signed Release opens Transactions and the actual purchase details, then Split across categories. Enter
6 and 4, invoke Save, and read back two parts totaling 10 with one group and complete metadata. All unrelated
columns of the other tables and original entries remain equal. Restart full Release, open Edit split and invoke
Combine into one transaction. Read back one ungrouped purchase of 10 with complete retained metadata.

Restart Release again, invoke the actual Record refund flow and save 3. Read back the exact linked Refund. Edit
the original purchase to 2 and invoke Save. The existing translated exceeded-refunds validation appears and the
draft stays open. Read back all 24 tables: every column equals the pre-rejection refund state. Restore exact
original database/sidecar bytes, reinstall full Release, return to Home and stop. No profile/security storage,
desktop input, screenshots, system settings or physical device is touched. Evidence:
artifacts/ledger-policy-release-native/proof.json, split/joined/refund/rejected proofs and fresh native hierarchies.
Enabled rejection/rollback is SQLite test evidence, separate from normal inactive native flow evidence.

Preserve negative native observations: one attempted Save is interrupted before invocation by an unavailable
ADB daemon; a later fresh hierarchy and actual Save supply the acceptance evidence. The existing SaveError is at
the end of the long Advanced form and requires manual scrolling. Its complete visible native text and the open
two-EUR draft are checked there, but immediate field-adjacent feedback remains a real UX follow-up, not accepted
by this data-only slice. Evidence: native-rejection-feedback.json and the retained hierarchies.

Phone-test artifact: artifacts/android/zanance-d126-release.apk, 81,012,651 bytes. SHA-256:
`da29d6756c264e4b2ede418936452adad7085018cc43e84599e695749eb8df0e`.
ZIP integrity, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permissions and v2/v3 signature pass. Local test signing is not production signing.
Evidence: artifacts/ledger-policy-apk-proof.json. Emulator/build evidence is not physical-phone, iOS, OAuth,
store or product-release acceptance. Remaining ENT-02/03/04, billing, encryption and external gates remain open.
