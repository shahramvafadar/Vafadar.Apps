# Actual-file template/filter write policy - D-119 / AT-121

ENT-02 remains in progress. SaveTemplateAsync and SaveSavedFilterAsync now share the D-118 actual-file cached-access
transaction gate. There is no visible UI change, schema/migration, SDK/permission, portable purchase fact or active
selection. The registered provider remains inactive without a paid grant; test builds stay unrestricted.

## Enabled write contract

Acquire the actual opened SQLite file's writer before existing-id/count reads. New templates count stored templates;
new uniquely named filters count stored filters. Existing ids reuse a slot, as do explicitly confirmed trimmed,
case-insensitive same-name filter replacements with a new id. Retained over-quota edits and confirmed duplicate-name
consolidation stay possible. The current UI already asks for replacement confirmation; no guessed merge is added.
Check capacity before sort assignment/removals/audit, preserve original creation/order rules, recheck synchronous
cached access, Save and commit before Changed. Failed owned transactions roll back all stored changes. Corrections
still require matching membership; an expired host retains existing-data rights but cannot create new resources.
Unlimited contexts skip count queries. No network, startup migration or purchase verification runs in this gate.

## Real SQLite acceptance

24 new cases pass, tagged AT-121. Main suite: 1,650 passing, no failures/skips; App.Tests 299 unchanged. Cases cover
inactive above-Free behavior; Free rejection before sort/audit/events and complete stored metadata preservation;
existing-id edits above quota; deletion freeing capacity; Plus/Pro/exact active shared unlimited capacity; complete
same-name replacement and same-id/name collision; expired-host corrections and missing-membership rejection.

An isolated database BEFORE INSERT trigger rejects the actual new template/replacement filter SaveChanges. Complete
original rows, including the removed filter/query metadata, remain identical; no Changed occurs. Removing only this
owned test trigger permits retry. Two independent DI providers/context factories and worker tasks compete for the
same actual database writer, with both initial captures synchronized before acquisition. One final-slot write
commits, one receives the structured capacity failure, sort order is correct and Changed fires once. There is no
fake count implementation or store-instance semaphore. This does not prove server roles or physical-device behavior.
Evidence: artifacts/template-filter-policy-focused-tests.log and template-filter-policy-all-tests.log.

## Platform and normal Release checks

Final strict Windows and Android builds pass with zero warnings/errors. The canonical Build-AndroidApk.ps1 completes
Release; source hashing confirms ZananceStore.cs did not change during builds. Logs:
artifacts/template-filter-policy-windows-final-build.log, template-filter-policy-android-final-build.log,
template-filter-policy-android-final-release.log. Test output is cleaned afterwards without warnings/errors.

The normal signed Release saves the fourth template from actual entry detail, retains the chosen 1,234 minor-unit
amount and displays its success dialog. Transactions saves the second filter and renders both actual filter buttons.
Prepare only three fictitious templates and one fictitious filter in the exact independently owned emulator-5570
sample database. Compare complete columns/rows of all 24 tables: only the exact new template/filter rows differ
from prepared data; all original rows, fixture metadata, ledger and preferences remain unchanged. No profile or
security storage is touched. Restore exact original database/sidecar bytes and return full Release to stopped Home.
Evidence: artifacts/template-filter-policy-release-native/proof.json and retained native hierarchy captures.

The first isolated fixture preparation fails on an unquoted SQL From column before placement/native Save; SQLite
rolls back that local fixture. Retain the negative evidence, quote fixture column names and recheck original owned
bytes before preparing again. No production change or speculative workaround is made. No visible control changed;
completed earlier three-language/theme/width layout matrices remain applicable and were not repeated.

Phone-test artifact: artifacts/android/zanance-d119-release.apk (81,430,613 bytes), SHA-256
`ce368a96b383cc0648834711ede92a36e925e3b7e6fc1c61a9cb07b6a484e938`. ZIP integrity, full ARM64/x86_64 assembly
stores/app AOT, non-debuggable pro.vafadar.zanance package, unchanged cloud-build permissions and v2/v3 signature pass.
The existing local test certificate is not production signing. Evidence: artifacts/template-filter-policy-apk-proof.json.

Remaining ENT-02/03: goals/plans/budgets/profiles/holdings; import/restore above-quota classification and explicit
read-only selection; automatic work/deep links/widget/OCR; approved deployment activation and final translated
limit UX. Physical-device, iOS, purchases/server/role, encryption and release acceptance remain separate gates.
