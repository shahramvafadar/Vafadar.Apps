# Actual-file goal and plan write policy - D-120 / AT-122

ENT-02 remains in progress. Goal Save and whole Plan batch/split Save now use the D-118 actual-file cached-access
writer gate. No visible UI, schema/migration, SDK/permission, portable paid fact or active/read-only selection is
added. The registered provider stays inactive without a paid grant; current test builds remain unrestricted.

## Enabled write contract

Active and paused goals/plans count. Completed/archived goals and explicitly ended schedule history do not. Goal
creation/reopening checks capacity before protection normalization or another goal's Home pin can change. Existing
counted edits/pause/end and historical metadata corrections remain possible above quota. New earmark/quantity use
requires AdvancedGoals; reactivating it is new work, while correcting completed historical classification is not.

Plan Save materializes the batch once and compares actual stored affected identities/states against requested
states under the writer. Only net growth checks capacity; reject the whole batch before any edited/new rows are
saved. Ending one and creating one in the same batch is slot-neutral, independent of input order. New paid-tool use
is checked separately: nth/last weekday, second monthly day, weekend/holiday shift, contract metadata or auto-post.
Basic dates, calendars, reminder settings and presentation mode are not commercial rights. Retained tools and
historical metadata remain correctable; enabling new advanced automation requires its capability.

SaveSplitAsync keeps its existing owned transaction, checks a distinct linked continuation against the stored
predecessor (not the supplied already-Ended object) and transfers its slot. Earlier entries/states stay on the old
slice; recorded later links move to the continuation. Existing conflicting-date behavior returns false without
writes. Recheck cached access after moves and before commit; failures roll back the complete operation and do not
raise Changed. Expired-host retained corrections/splits do not request new capacity; missing membership is still
rejected even with personal Pro. No network, purchase verification or startup database work runs inside the gate.

## SQLite acceptance

45 new cases are tagged AT-122. Full main suite: 1,695 passing, no failures/skips; App.Tests 299 unchanged. Cases
cover inactive above-Free creation; counted pauses; retained over-quota edits/end; historical reopening and freed
slots; pin/protection preservation before rejection; paid/exact shared unlimited capacity; whole-batch rejection
with no partial correction/Changed; input-order-independent net slots; missing membership and expired-host data
rights; new advanced goal kinds and eight plan tools, retained-tool corrections/splits and new auto-post rejection.

Split/resume cases run at five and seven counted plans. Real expense entries and settled occurrence states before
and after the cut keep complete metadata, including EntryId/audit; only the later ScheduleId changes. Required-
column failure leaves a slot available. A SQLite trigger rejects the later entry relocation after occurrence states
already moved: every complete stored row remains identical, no Changed fires, and retry succeeds after dropping
only the owned test trigger. Conflicting new dates still return false with complete no-write comparison. Independent
providers/context factories compete under actual SQLite with both initial snapshots captured before acquisition;
exactly one final-slot write commits and Changed fires once. Counts are not mocked or serialized by a store semaphore.

The initial focused test compilation tried assigning the immutable entity id; fix only the fictitious EF seed
mapping, preserving the production identity contract. The focused 43-case suite passes. Final two historical-
correction cases and complete early/late settled-state metadata run in the full final suite; do not confuse the
earlier focused count with all 45 cases. Evidence: artifacts/goal-plan-policy-focused-tests.log and
artifacts/goal-plan-policy-all-tests-final.log. Strengthening fixture kinds/settled links changes tests only; the
verified production source/APK remains unchanged. Test output is cleaned after the final suite.

## Platform and actual normal Release

Strict Windows and Android builds and canonical complete signed Release pass with no warnings/errors. Source
hashing confirms GoalStore.cs/PlanStore.cs did not change during builds. Logs:
artifacts/goal-plan-policy-windows-final-build.log, goal-plan-policy-android-final-build.log and
goal-plan-policy-android-final-release.log.

Normal signed Release saves a sixth plan (2,500 minor-unit expense, Once, no auto-post) through its actual editor
and displays the due item. It saves a second account-balance goal (5,000 minor-unit target) and displays the card.
The goal editor retains its existing behavior of creating an empty, reminder-off contribution-date row; this is
not newly activated contribution-plan enforcement. Prepare only five fictitious plans and one earmark goal in the
exact independently owned emulator-5570 sample database. Compare every column/row in all 24 tables: only the exact
new Plan, Goal and its empty contribution row differ. All seed metadata, original rows, ledger and settings match.
No profile/security storage is touched. Restore exact original database/sidecar bytes, verify them and return full
Release to normal stopped Home. Evidence: artifacts/goal-plan-policy-release-native/proof.json and native captures.

The first fresh hierarchy is unavailable immediately after startup. Retain the two negative dump results; current
resumed-activity evidence and a later fresh hierarchy confirm the same live Home. Continue without restarting,
re-preparing or changing production code. No visible controls changed; earlier language/theme/width layout matrices
are retained instead of repeated. Emulator evidence is not physical-device, iOS or store/release acceptance.

Phone-test artifact: artifacts/android/zanance-d120-release.apk (80,996,267 bytes), SHA-256
`28204402525011af16f4f665eeb4579956dc649abcd923f40cf733625593affd`. ZIP integrity, full ARM64/x86_64 assembly
stores/app AOT, non-debuggable pro.vafadar.zanance, unchanged cloud permissions and v2/v3 signature pass. The existing
local test certificate is not production signing. Evidence: artifacts/goal-plan-policy-apk-proof.json.

Remaining ENT-02/03: contributions/allocations and occurrence settlement/automation; budgets/profiles/holdings;
import/restore above-quota classification and explicit read-only selection; automatic/deep-link/widget/OCR paths;
approved activation and final translated limit UI. Server roles, purchases, encryption and all release gates remain open.
