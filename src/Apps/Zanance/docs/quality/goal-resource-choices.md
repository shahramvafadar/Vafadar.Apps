# Durable goal choices and actual writer (D-143 / AT-145)

## Delivered boundary

Extend the existing ResourceSelections boundary to Goals without a schema/entity change. Reviewed original names,
measurement kinds, currency and active/paused/completed/archived states are rechecked with actual file, financial
scope, verified rights, stored revision and capacity. Free's one goal counts a pause; explicit empty is retained.
Invalid/stale choices need review, and choosing an advanced retained goal never grants its paid tools.

The goal writer reads stored choice under the actual SQLite writer; it overrides only cached choice, never
entitlement/membership. Explicit new or reopened goals join a free chosen slot atomically, rather than counting
unselected retained history again. Completion/archive/delete releases only that identity; pause keeps it.
Unselected paused goals cannot resume. Retained notes/state corrections, negative earmark releases and deletion
remain available. Positive earmarks independently require AdvancedGoals and usable goal/account choices; no ledger
or balance is written. Full editor/contribution failures roll back goal, pins, plan and choice together.

Current release registration remains inactive and unrestricted. Settings still exposes Accounts/Plans. Goal
chooser UI, effective goal/contribution projection and native reminder delivery follow in subsequent ENT-02/03
slices. Other selected resources, activation and owner/provider/device/iOS/publication gates remain open.

## Verification

- 31 new AT-145 real SQLite cases with complete 25-table equality for rejected/read-only operations.
- Focused 31 pass (11.139 s). Initial compilation caught a wrong test enum and unassigned fixture callback warning;
  corrected before execution. Normal parallel main 2,430 / App.Tests 407 pass (69.359 s), zero failures/skips.
- Standard test output cleaned. Strict Windows 17.68 s / Android 36.63 s, zero warnings/errors.
- Normal signed Release actual UI created OwnedGoalD143 (EUR target 5,000 minor), saved contribution 1,000 minor,
  and explicitly set aside 2,500 minor in exact owned QA03, selected through real UI. Unrelated original complete
  tables/columns/rows remain equal. Choices are empty, enforcement inactive. Only the already delivered D-141
  migration is added to this old restored fixture. This slice introduces no model/schema/migration change.
- Exact original sample bytes/every sidecar restored; Main/English/System retained, final Release installed/stopped.
  No shared ADB server, physical device, owner desktop input or system-setting change. No visible layout change.
- Initial native helper attempts excluded the bottom More tab and looked for Goals on More rather than Insights.
  Both fail before Save; preserve their XML/logs as negative evidence. Reacquire the actual hierarchy, use the real
  visible bottom target and registered Insights destination, then complete only the unfinished flow. No app change.

Records: goal-resource-choice-focused.log, goal-resource-choice-all-tests.log, goal-resource-choice-clean.log,
goal-resource-choice-{windows,android,release}.log, goal-resource-choice-apk-proof.json and
goal-resource-choice-native/{financial-proof,final-proof}.json plus actual native UI XML.

## Complete phone APK

Canonical Build-AndroidApk.ps1 produced artifacts/android/zanance-d143-release.apk. Package pro.vafadar.zanance,
nondebuggable Release, intact ZIP, complete ARM64/x64 assembly stores and app AOT, signature v2/v3 with existing
local test certificate. No probe/performance package or permission-policy change.

Bytes: 81676373. SHA-256: `20734e28db35595093f2aa7ef7d425110f49544d5113ff55d5e90ea3a35943e7`.


Local signing/package and owned-emulator evidence do not establish Store signing, physical phone, iOS, provider,
native OS accessibility or publication acceptance. Goal chooser/projection/reminder delivery, other resource
bindings and future approved activation remain open. Do not activate limits or claim ENT-03 complete.
