# D-306 reviewed plan/goal input integrity

## Actual problem and complete correction

A reviewed plan/goal snapshot retained a complete original input stamp and validated the current database
against it, but did not revalidate its public in-memory model lists. Six actual Home regressions reproduce
acceptance after changing plan amount/rule/state or goal target/contribution/allocation inputs only in memory;
all six fail the required rejection assertion before the fix. This is an original-input consistency defect,
not evidence of a remote exploit, bypassed authentication or an unauthorized ledger posting.

PlanStore and GoalStore now compare the complete reviewed in-memory input packet against its original stamp,
after original file/rights checks and before continuing the existing database/choice validation. This follows
the existing quick-resource boundary. They still re-read the actual original database and validate its complete
stamp and current rights; the new comparison does not replace, weaken or cache those checks. Model fields,
schema/migrations, time, financial arithmetic, keys, backup/privacy, entitlements and activation stay unchanged.

The stronger original-input check reveals the actual GoalDetail lifecycle caller changing its reviewed Goal
object before Save. Completion, reopening and resume fail three original application cases until that caller
is corrected. WriteStateAsync now clones the complete goal, changes only the detached draft and submits it
with the untouched original review. The same helper covers completion/reopening, pause/resume, archive and
restore. No partial rollback tuple or early mutation of the reviewed object remains. The original native
confirmation, busy guard, saved-state reload, retained amount draft and error handling remain in use.

## Relevant verification

- Six before-fix Home cases fail specifically because no rejection occurs. After the data correction, all 63
  related Home/plan/goal/review-reminder cases pass, including those six.
- The new actual goal allocation-writer case refuses an altered review inside the real SQLite writer and
  preserves complete original ledger rows, target and empty allocation table. It passes together with 115
  separate related goal selection/plan/contribution policy cases. These are 179 distinct Data cases, not a
  new whole-Data or whole-suite run. An initial mixed class/method filter selects zero cases; that log is
  retained and counts as no verification. Separate correct filters supply the actual 1 + 115 results.
- Initial related original-source application flows: 169 cases, 166 pass and the three lifecycle defects above
  fail. After the detached-draft fix, only the affected three methods' nine variants are rerun; all nine pass.
  Thus all 169 distinct related application cases are qualified across those runs; it is not 178 distinct or
  a new full-suite run. The final implementation adds seven regression cases. Discarded prototype cases below
  are not added to the shipping count.
- Strict relevant test/native builds have zero warnings/errors. A new native diagnostic initially misses its
  formatting namespace; that compilation and secondary markup warning are retained, then the corrected build
  has no warnings/errors. No application logic is weakened to pass a test.
- Actual original GoalDetail page, buttons, confirmation and SQLite writer run in a new independently owned
  Windows fixture in English/Persian/German and light/dark at 412 units. Twenty-four real native actions finish:
  complete, reopen, pause and resume per context. Actual native Invoke patterns answer the original confirmation;
  the application window stays hidden and only its own root/popups are captured. Stored states match, the
  unsaved amount remains, the original opening balance stays 10000 and no ledger entry/allocation is posted.
  The confirmation and states are visually inspected from own-window artifacts. There is no new caption/layout,
  OS-scale, spoken-screen-reader, Windows commercial or physical-device claim.

Evidence: artifacts/home-recurrence-d306, including review-before.xml, review-related.xml,
review-writer-case.xml, review-goal-writers.xml, app-related.xml, app-final-failed-boundaries.xml and
windows-light/windows-dark/goal-review-integrity-proof.json. The retained folder name identifies the original
investigation; it does not mean that a recurrence cache was shipped.

## Rejected performance experiment, retained to prevent repetition

The earlier D-303 cumulative stage callbacks included dispatch/formatting/publication, so their plan duration
was not proof that pure recurrence expansion dominated. A bounded operation-local date cache was built and
qualified experimentally; its 73 distinct related cases and complete native financial/raw-data checks pass.
Twelve original Release/AOT Home publications retain the exact canonical presentation. However, the candidate
reference median is 1833.88 ms versus D-305 2134.22, while the tenfold median is 6929.95 versus 6436.50 ms: no
large-data improvement is demonstrated. Sample variation and memory pressure are retained rather than hidden.

A narrower same-process original HomeViewModel experiment records only actual plan/forecast worker clocks,
with no copied application algorithm and no native-layout claim. Off/on/on/off samples in milliseconds are
plans 272.37/190.65/45.47/61.67 and forecast 544.63/67.18/67.92/86.92; complete original financial presentations
match. First-use costs vary substantially. The later warm samples save only about 35.19 ms combined, which
does not justify the added cache/API/metadata complexity, particularly without improved full tenfold timing.
The prototype, all optional Core parameters, generated cache context, its 22 cases and temporary measurement
hooks are removed from product/tests; original Core/calculation and Home sources are restored. Original
prototype files, receipts, failed helper compilation and exact source backups remain only in ignored artifacts.
Do not repeat this rejected experiment without a new cause. It is not a performance delivery or a shipping
new test count. QA-06 retains its actual remaining work and platform/phone/cold/ANR acceptance.

## Protected Android/runtime/package scope

The independent protected Android Home caller has an explicit guard phase: one original English native Home
publication per unchanged reference/tenfold fixture; changed in-memory plan/goal inputs must be refused, then
that same original review must validate after exact in-memory restoration. Complete original raw database
schema/header/integrity/typed rows are compared after the checks. No data/keys are reseeded and no DB Save
belongs to this phase. It reuses the original canonical presentation, not old completed timings. These are
functional guard checks, not new performance statistics. Actual outcome and final main package follow below.

The existing D-305 startup profile already includes the same validation/stamp methods. No new runtime method
signature, startup dependency or method-profile selection is introduced by the shipping guard/draft changes;
the complete changed Data/application AOT libraries are rebuilt. No redundant recorder capture, compiled-model
regeneration, migration, upstream cipher qualification, unrelated backend/sync/billing/UI matrix or broad
successful suite is replayed. Physical phone, iOS, platform acceptance and product release stay separate.


## Actual protected outcome and complete main package

Native guard process and exact comparison are recorded in android-guard-identity.json. Both protected
reference (10000/20/100) and tenfold (100000/200/1000) cases publish original native Home with exact original
financial presentation. Both altered plan and goal packets are refused, and the same restored review is
accepted afterward. Complete raw schema/header/integrity/typed rows still match their original authenticated
rollback; no DB Save or key recreation occurs. android-guard.json retains all outcomes and module identities.
Its one-sample functional durations are not a statistical performance claim; unrelated raw-stepping work
is skipped. The earlier actual D-305 hosted result remains successful in all four workflows.

Complete main APK: artifacts/android-review-integrity-d306-qualified/pro.vafadar.zanance-Signed.apk.
95350517 bytes; SHA256 6234ab5a130c8ce060debb992d3dcfaacf1b8267232fea6fd1f1dd47d19dfbab.
Identity pro.vafadar.zanance, min24/target36, embedded arm64-v8a+x86_64 App/Data AOT. v2/v3 verification succeeds
with unchanged local test certificate 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b;
this is not store signing. Both source-built cipher hashes equal the qualified candidate5 pins. Recorder
library/dependency/environment are absent. This configured connected build is not an offline APK.

The original main installation is upgraded without reset. Same actual process 10371 reaches its unchanged
My account / 0.00 EUR Home, observed in 135 own-package native nodes. No new account/profile, financial Save,
settings change or security-flag removal belongs to that upgrade observation. Final test output cleanup is
complete. Receipts: main-package-proof.json, main-signature.txt, main-badging.txt, main-native-proof.json and
final-tests-clean.log. Strict Windows/native caller/ordinary Android builds have zero warnings/errors.
The source/runtime/behavior batch is locally complete; its own hosted result requires the single post-push
check. Phone, iOS, provider/Store/activation and product release remain separate unverified gates.
