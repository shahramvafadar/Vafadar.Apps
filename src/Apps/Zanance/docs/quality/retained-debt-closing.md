# Retained manual debt closure (D-132 / AT-134)

## Boundary

The approved downgrade matrix always permits closing an existing loan. D-131's ordinary selected-account
transfer check did not recognize this retained operation: four actual enabled SQLite baseline cases rejected
complete Loan/Lent payments without an account choice or after shared-host expiry.

The existing writer now recognizes explicit manual, reviewed principal closure from actual stored accounts and
ledger rows. Loan receives money against a known negative balance; Lent pays money out of a known positive balance.
The complete submitted payments and real Fees-category expenses must exactly extinguish that baseline, and the
complete resulting batch must leave zero current principal. Confirmed/all-row balances must agree; a backdated
payment must see the same baseline. Widen effect totals before addition/negation, including Int64.MinValue.

No caller marker, repayment estimate or draft grants this right. Partial/overpaid/future/unreviewed/imported/
plan-marked drafts, uncertain balances and wrong directions remain ordinary new work. Editing/deleting old rows,
adding adjustments or borrowing and repaying in the same batch cannot manufacture a retained baseline. Multiple
actual payments and explicit cross-currency destination amounts are supported; no exchange rate is guessed.

New closing groups contain one transfer and at most one actual Fees expense per endpoint on the same date.
Existing/holding groups and unrelated group rows do not acquire this right. Cash/checking/savings endpoints may
fund retained closure; credit cards, assets or newly borrowed/lent counterpart funds still require their own new-work
rights and selected account. An explicitly submitted Lent-to-Loan transfer may close both actual principals.

Membership, exact-file cached access, complete ledger validation, paid updates, attachments, rollback and Changed
remain in the same writer. No automatic archival, financial relationship, income/spending reclassification,
schema, portable entitlement, SDK or permission change. Current application enforcement stays inactive.

## Evidence

51 actual SQLite acceptance cases cover Loan/Lent, host expiry, selections, actual borrowing history, partial and
multiple payments, foreign currencies, both fees, unrelated groups, uncertain/stale baselines, Int64 boundaries,
complete-batch rejection, SQL failure, retired cached access, explicit retry and independent competing writers.
Rejected/failed writes compare all 24 complete tables, with no Changed. The independent writers can close the
principal only once. Initial test-fixture compile/category-seeding failures are negative setup evidence.

The complete suite has 2,102 passing cases, including 318 application cases. Final execution, cleanup, strict
Windows/Android build, signed APK and installed normal manual-repayment evidence are recorded in the local
retained-debt proof/log files. Native checks exercise the real unrestricted app; enabled commercial rejection is
proved separately by actual SQLite tests. Commercial selection UI/persistence, other bindings, occurrence-state
automation and ENT-02/03/04 completion remain unfinished. Physical phone, iOS, OAuth, billing, encryption,
CI and product release acceptance are separate.

## Final native/package record

Normal signed Release on emulator-5570: English/Persian/German in both light/dark, six actual native Saves.
Each language closes a Loan in light and a Lent in dark through the existing account-detail action and ordinary
transfer editor. Verify zero principal, exactly one confirmed manual transfer, no automatic archival, no income/
spending row and every original column/receipt across all 24 tables. Restore exact original sample database/
sidecars and English/System display preferences; final full Release stays installed and stopped.

The first premature hierarchy read occurred while fixture installation was unfinished; no Save was invoked.
A Persian review selector initially included an inverted/clipped native row rectangle and did not navigate.
Check valid actual target bounds, reacquire the existing Accounts view and continue before its first Save;
never repeat successful financial Saves. These helper failures are negative evidence, not product acceptance.
Initial cleanup overlapped still-running tests and reported locked files. The ordered final full-suite execution
and cleanup succeeded; after tightening SQL/access exception assertions, all 51 focused cases passed again and
cleanup again finished without warnings/errors. No production source changed after the strict builds.

Main log: artifacts/retained-debt-all-tests-final.log; exact assertion checks: retained-debt-focused-final.log;
cleanup: retained-debt-clean-last.log; strict builds: retained-debt-windows-build.log and retained-debt-android-build.log;
canonical signed Release: retained-debt-release.log. Final native proof: retained-debt-native/final-proof.json.
No Windows layout change is introduced; this record does not claim a Windows native repayment invocation.

APK: artifacts/android/zanance-d132-release.apk; 81,483,861 bytes;
SHA-256: a8a49d7e39723667ac59582bee6660df8d25e556f25231395417f4a4395880e5.
Package pro.vafadar.zanance, min 24/target 36, non-debuggable complete arm64-v8a/x86_64 assembly stores and App AOT;
archive integrity and v2/v3 signature verified. Local test certificate: 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b;
no production-signing or physical-device acceptance claim. Package proof: artifacts/retained-debt-apk-proof.json.
