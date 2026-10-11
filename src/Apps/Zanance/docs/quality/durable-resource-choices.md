# Durable scoped account and Plan choices - D-141 / AT-143

## Delivered boundary

ResourceChoiceStore reads complete original account/Plan identity, name and state lists without writing. Bounded
explicit Save binds the exact opened file, verified financial scope, immutable cached rights, original list and
stored revision to the actual SQLite writer. Reject stale reviews, profile changes, lost rights, changed original
states/names, excessive/nonexistent/historical selections and ABA. Canonical duplicate ordering is harmless;
explicit empty choices are different from absent choices. A no-op Save keeps the row revision and emits no Changed.

AddResourceSelections creates only the profile-local composite-key ResourceSelections table: kind, financial scope
kind/id, sorted original selected ids and a random change revision. Regenerate the compiled model. This row is part
of the normal financial database backup; it contains no entitlement, membership proof, credential or device security.
Restored foreign scopes are never guessed or retagged. Current inactive registration reads no choice rows and creates
none. Unlimited access uses all original eligible resources without rewriting a retained limited choice.

Persisted rows override the cached fallback selection only. Real ledger writers, automatic payments and future Plan
work use the stored choice inside their existing actual-file boundaries. Creation/unarchive joins remaining selected
account capacity atomically; account archival releases only its own identity. Plan creation/end and a validated
selected predecessor/continuation split update the same choice under the Plan writer. Paused plans keep their slots.
Keep every original account, plan, state and ledger row for history/calculations; choices themselves never post money.

Malformed choice input grants no bounded new work but does not block retained history corrections. The explicit
reviewed chooser Save can repair that exact row. Do not automatically fix corrupt choices during unrelated corrections.

PlanWorkSnapshot captures the stored choice revision and validates it before/after native publication. Choice changes
refresh the existing reminder coordinator. Real application flows prove grouped snoozes retain only their original
currently selected members, and a choice changed during native await cancels the retired queue.

## Automated evidence

44 added AT-143 cases: 42 real Data SQLite / two actual ReminderService application flows. Complete comparisons cover
all 25 current tables; successful selection-only saves preserve every complete non-choice row. First-schema migration
and regenerated compiled model checks remain in the full suite. Two independent reviewed writers cannot both overwrite
the same revision. SQLite failure and post-SQL retired rights roll back without publishing Changed. Also test actual
money/correction binding, capacity joining/release/splits, unlimited/inactive retention and exact shared membership.

Normal parallel full suite: 2,374 pass, zero failures/skips, 67.349 s. App.Tests: 382, including two added flows.
Test cleanup succeeds. Strict Windows: 25.23 s, zero warnings/errors. Strict Android: 51.74 s, zero warnings/errors.

Initial focused 35-case run and initial full 2,370-case run pass. Four later malformed-row regressions reproduce the
initial implementation blocking retained correction in writer construction; the final implementation separates
correction rights from usable choice input. Preserve all four failures; final complete suite passes the unchanged
regressions. Earlier local PowerShell brace-path parsing and stale guessed read paths perform no product writes and
are not acceptance evidence.

Local records (ignored artifacts): resource-choice-focused.log, resource-choice-full-tests.log,
resource-choice-malformed-negative.log, resource-choice-final-tests.log, resource-choice-clean.log,
resource-choice-windows.log and resource-choice-android.log.

## Remaining delivery

The chooser UI is the next ready slice, with covered loading, explicit Save and preserved draft/error feedback.
Other selected resources/import/restore classification and remaining native commercial bindings are still open.
Commercial enforcement stays inactive; these tests do not activate a tier or verify store/provider purchase facts.
No visible layout, resource string, SDK, permission or native scheduler-adapter change in this slice. Physical-phone,
iOS, real provider, performance and publication gates remain open. Release package and actual normal migration evidence follow below.

## Normal Release package and native migration

Canonical complete Release APK: `artifacts/android/zanance-d141-release.apk`, 81,123,243 bytes.
SHA256: `d62ed8f5c3af4fbbbca3ec67e8c77679d839063c1e6e0b726b73000cb8fa3469`.
Package `pro.vafadar.zanance`, min/target API 24/36, non-debuggable, intact ZIP, embedded ARM64/x64 assembly stores
and app AOT, v2/v3 signature. Existing local test certificate SHA256:
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b`.
Unchanged cloud-build permission boundary; no probe/performance package included. This is an installable local-test
APK, not store publication/signing acceptance. Logs: resource-choice-release.log / resource-choice-apk-proof.json.

The actual installed normal Release opens the independently owned QA03 Native profile through real UI. Six
fictitious monthly plans remain available above the Free allowance, with no financial Save and current commercial
registration inactive. Its real startup migration creates ResourceSelections empty: all 23 complete preexisting
non-migration tables remain equal to the prepared fixture, and old migration history gains only AddResourceSelections.
All 25 current tables pass SQLite integrity inspection. Native records: resource-choice-native/ui-proof.json and
migration-proof.json. Only the owned qemu emulator 5570 / TCP 5571 through local ADB 5038 is addressed.

No visible layout change requires a new Windows width/theme matrix here. This native check proves the normal
inactive migration and retained unrestricted work, not enabled limited-choice UI or production entitlement/native
acceptance. The four malformed-row negative cases are engineering evidence from actual isolated SQLite.

The initial restoration attempt loses the local ADB 5038 connection while the existing Main-profile action dialog
is open. Preserve resource-choice-native-finish.log as negative restoration evidence. Reacquire the current native
hierarchy after reconnecting the same owned transport and invoke the existing Open action; do not repeat successful
migration/plan review. The final complete migrated data remains equal before restoration. Restore only the owned
QA03 original binary and every original sidecar, comparing exact captured bytes. Reopen the original Main profile
with unchanged English/System display, install the complete final normal Release and force-stop it. Final records:
resource-choice-native-restoration-resumed.log / resource-choice-native/final-proof.json. No physical-device or
system-setting changes. The dedicated local ADB 5038 server is stopped after this completed restoration.
