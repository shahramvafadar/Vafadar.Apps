# Bound recovery and retained import rights - D-124 / AT-126

ENT-02 remains in progress. Backup/restore and aggregate import/Undo share the actual-file cached-access boundary.
Current registration remains inactive without a paid grant; test builds stay unrestricted. No model/schema,
migration/compiled model, SDK, permission, visible string/layout or portable entitlement change.

## Reproduced recovery defect and stable destination

The previous generic SQLite restore consumed its input before creating the destination context and created another
context for migration. A changing profile provider could therefore select a different database during either await.
The first runnable regression restores a real SQLite snapshot while its stream changes that provider. It fails:
the second profile contains the restored account instead of its original account. Preserve that negative evidence:
artifacts/recovery-policy-before-tests.log. This is a shared-source defect reproduction, not evidence of a concurrent
profile change through the app's exclusive restore UI.

Create the actual context before consuming input; retain it for native replacement and migration. The optional
generic callback checks that actual context before input/native copying and backup output, without a commercial
dependency in Vafadar.Data or a writer transaction around SQLite's native backup API. Existing interface overloads
remain available. Integrity checks and temporary-file cleanup remain. The same regression then passes:
artifacts/recovery-policy-after-destination-tests.log. A first-schema snapshot also migrates in the initial file
after a provider change, preserving all second-profile columns.

This does not make the whole portable package atomic. Native replacement followed by a failed migration still
requires the existing safety-copy recovery. Multi-source/profile-catalog coordination remains separate work.

## Actual-file rights and complete import transaction

CommercialFileAccess captures synchronous cached facts for the actual full database path once and rejects mismatched
or retired snapshots. It is shared by writer transactions and ZananceDatabaseBackupSource. Production DI replaces
only Zanance's generic database source, preserving other registered backup sources and the existing entry name.
No paid facts are accepted from portable financial preferences or backed-up rows; no network check runs here.

Backup/restore and owned import preserve data above quotas and after shared-host expiry, with exact accepted
membership still required. Personal Pro never substitutes for membership. Import uses BackupRestore; durable Undo
uses Corrections. Both check before stored rows/journal reads under their existing actual SQLite writer, even with
current enforcement inactive. Recheck before writes and after SQL before commit. SQL/access failure rolls back
the whole aggregate/detail/journal operation; Changed follows a successful commit once. Preserve D-72's explicit
link/keep-both choices, metadata, attachment ownership, stale-preview and dependent-import Undo rules.

This boundary retains owned data. Explicit read-only classification of newly imported/restored work, selected
active resources, profile creation and automation are not implemented by this step.

## Real SQLite acceptance

28 new AT-126 cases pass; the main suite passes 1,834 with zero failures/skips. App.Tests remains 299. Cases cover
the reproduced destination change; inactive/Free/expired-host imports and restarted Undo above quota; four missing
membership boundaries without stream consumption/output/writes; Free/Plus/Pro/expired-host recovery of above-quota
accounts/templates/advanced goals and preferences; two access changes during restore input; initial-file facts
after profile movement; SQL-trigger access retirement after journal INSERT/DELETE; journal SQL failures and complete
rollback; independent enabled/inactive duplicate imports; four mismatched-file operations; corrupt and cancelled
input; cached access retirement before native backup copying/output; DI preservation of another backup source;
and first-schema migration in the original file. Compare complete columns of all 24 tables where applicable.

Triggers use the production factory's actual native SQLite connection. Independent writers recheck real stored
identities; no duplicate details or inferred financial relationships. Focused and full logs:
artifacts/recovery-policy-focused-tests.log, recovery-policy-focused-final-tests.log and recovery-policy-all-tests.log.
Test cleanup passes with zero warnings/errors: artifacts/recovery-policy-tests-clean.log.

## Strict platforms and installed Release

Strict Windows and Android builds pass with zero warnings/errors. Canonical Build-AndroidApk.ps1 completes a full
signed Release with its warning/error guards. All six production source hashes remain stable during final builds.
Logs: artifacts/recovery-policy-windows-final-build.log, recovery-policy-android-final-build.log and
recovery-policy-android-final-release.log. No visible layout/control changes require a new language/theme/width
matrix; retain completed reviews. Commercial limit/error-dialog feedback remains ENT-04 work.

Use only independently owned emulator-5570 and its exact sample database. Preserve the original database/sidecar
bytes and prepare one fictitious 10 EUR aggregate with its complete title/note/tags. In normal signed Release,
open Import & export and select the independently created CSV through Android's real DocumentsUI Downloads picker.
The actual preview shows one new, zero skipped and zero invalid rows. Import is disabled until an explicit overlap
choice. Select Link and replace: the preview shows 7.50 EUR remaining. Invoke Import transactions (1); the actual
result reports one imported and zero skipped. Read back all columns of all 24 tables: only that aggregate, the
2.50 EUR detail and its journal change. The original aggregate metadata and total 10 EUR are preserved; accounts,
settings and all unrelated rows match.

Restart the complete Release, reopen Import & export, invoke the retained Undo import and confirm its actual
dialog. The result reports that the linked aggregate amounts were restored. Read back all 24 tables again:
all prepared columns match except the expected aggregate UpdatedAt audit timestamp; detail and journal are gone.
Restore exact original database/sidecar bytes, delete only the owned CSV, reinstall complete Release, return to Home
and stop it. No profile/security storage, desktop input, screenshots, system settings or physical device is touched.
Evidence: artifacts/recovery-policy-release-native/proof.json and fresh picker/app native hierarchies.

The first local scroll helper expected a scrollable container before the selected preview made the page grow;
fresh hierarchy inspection instead found Choose CSV file already visible. A transient picker hierarchy file was
not available on its first dump; a fresh dump of the same picker succeeded. Initial local command quoting errors
did not start builds or change production source. These helper failures are negative evidence, not app acceptance;
the completed real picker/import/reopened Undo and stored-data checks above supply acceptance.

Phone-test artifact: artifacts/android/zanance-d124-release.apk, 81,008,555 bytes. SHA-256:
`464089972edabb351bed95bd5cbcd173f911052bcd47bb9f98f7378b61c2643b`.
ZIP integrity, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min SDK 24/target 36,
unchanged cloud permissions and v2/v3 signature pass. Existing local test signing is not production signing.
Evidence: artifacts/recovery-policy-apk-proof.json. Emulator/build evidence is not physical-phone, iOS, OAuth,
store or product-release acceptance. No native profile-switch fault injection or new limit-dialog review is claimed.

Remaining ENT-02/03/04 includes explicit selected read-only resources and imported/restored new-work classification,
future budget activation, profile creation, occurrence/contribution automation, other native entry points and
translated limit feedback. Billing, encrypted storage and owner/external release decisions remain unfinished.
