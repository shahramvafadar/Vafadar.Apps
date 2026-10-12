# Restore outcomes - D-167 / AT-169

Status: implemented, locally and natively verified. A completed original all-source restore is now
recorded before display, onboarding, app-lock, automatic posting, reminder or root-opening follow-through.
The accepted in-memory package/password/preview is retired immediately. A translated completed result
and explicit Open restored data action remain available when follow-through fails. Retrying that action
does not create another safety copy or replace the database again; original automatic-post idempotency
remains authoritative.

A source failure during replacement can occur after another source changed data, including a later
BackupException. This is reported as an uncertain restore, not as a pre-write validation refusal or
rollback. A new explicit inspection and replacement consent are required before trying again. Original
source recovery and safety-copy algorithms are unchanged. Consent/authentication decline and pre-primary
safety-copy failure retain the reviewed input. An incomplete new file read keeps the preceding review.

BackupRestoreSession is the actual non-UI application flow, compiled in App.Tests. IBackupRestoreRuntime
isolates native consent, authentication and completion effects. MauiBackupRestoreRuntime retains the
original backup service, exclusive automatic-post gate, localization, onboarding and app-lock operations.
Busy guards span the entire operation; accepted input is frozen. Page/native Shell Back/tab guards retain
the covered completion flow. Pinned review/result/error/actions grow outside the scrolling body. Opening
the form no longer writes the constructor's backup-protection choice. No password is persisted.

No EF model/schema/migration, SDK, permission, network, credentials, paid activation or portable-format
change. Optional protection, original backup/recovery access and device-security policy stay unchanged.
Safety-copy retention is unchanged; this is not encrypted safety-copy delivery or an OS-backup policy.

## Verification

35 AT-169 cases pass using actual application sources, original BackupService and isolated SQLite:
six languages across display/onboarding/lock/financial-work/open failures; exact accepted rows and retired
payload; explicit completion retry without repeat safety/primary restore; a real later-source exception
after database replacement; actual safety-directory failure; consent/authentication decline and busy
ownership; interrupted new stream retaining review; actual automatic posting before a follow-up failure,
then no duplicate ledger posting on retry.

Ordinary main: 3,045 passed / App.Tests 988, zero failures/skips, 2m 50.619s. Cleanup succeeded. Strict
Windows and canonical complete signed Android Release have zero warnings/errors. All seven changed
production-source PDB checksums match. Dependent tests, cleanup and builds ran sequentially.

Thirty-six Windows contexts cover en/fa/de, light/dark, 360/412/wide and Simple/Advanced. Original native
Restore and completion actions use actual independent SQLite/source/safety operations. An explicitly
fictitious lock failure preserves the accepted result; native completion retry invokes no second primary
restore/safety copy. Captions, targets, input freeze and CanLeave affordance are checked. This is not an
actual native Back invocation test. Consent/authentication/display/lock/open effects are explicit fixture
ports; provider discovery is skipped only on this Debug review route. Complete 25-table rows and exact
owned development bytes/sidecars are restored. Persian light and German dark captures were inspected.

Normal final signed Release Android uses actual DocumentsUI, original preview, native replacement
consent, original database source and restored-profile opening, once. All 25 prepared tables match the
observed restored database, including the fictitious account identity. The fixture only targets the
independently owned QA03 database. Original bytes/sidecars and Main/English/System are restored, only the
new owned uploaded package is removed, and final signed Release is installed/stopped. Existing D-141
migration is the only baseline schema difference. No extra account or ledger money is created.
This does not assert that the mandatory original safety-copy files disappeared or that all app files
were unchanged. Physical phone, iOS, real OAuth, billing and Store acceptance remain separate.

Evidence: artifacts/restore-outcomes-complete-tests.log, restore-outcomes-all-tests.log,
restore-outcomes-clean.log, restore-outcomes-windows-build-final.log, restore-outcomes-android-build.log;
both Windows matrix modes and 36 native proofs/captures; restore-outcomes-native/final-proof.json and
native hierarchies; restore-outcomes-apk-proof.json and restore-outcomes-release-source-proof.log.

## Negative preparation evidence

Initial strict Windows compilation rejected IDE0044 on a constructor-only protection guard. It was made
readonly; unchanged non-UI main verification was not repeated. A bounded Android hierarchy read failed
before action. The first package-identity assertion found the filename below the viewport and stopped
before Restore. A continuation helper had an indentation error before action. Fresh hierarchy and native
scrolling in the same live process exposed the original identity/manifest; consent was accepted once.
These preparations are retained as negative evidence, not acceptance or reasons to repeat replacement.

## Installable APK

artifacts/android/zanance-d167-release.apk, 82647125 bytes, SHA-256
871b46cdc1c3240cf4a5a343e0fb56089470a54d61f13dd038b952d23481dfe0.
Non-debuggable pro.vafadar.zanance, API 24/36, full arm64-v8a/x86_64 stores/application AOT. v2/v3 verify
with the existing local test certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.

## Remaining scope

ENT-02/03/04 remain in progress until a finite current entry-point inventory establishes actual remaining
gaps. Do not repeatedly treat historical partial-slice notes as current missing implementations. The
previously proposed extra generic mapped-CSV gate is unnecessary: the approved matrix lists it in Free,
Core PlanPolicy already allows CsvImport, and original import/restore deliberately preserves owned data
above quota. No second paid or orphan quota gate is added. LOC-05/10 explicitly depend on owner texts.
Encryption/library/recovery, provider registration, physical phone, iOS/Mac/signing, billing and release
decisions remain separate. This slice does not complete the full commercial release or continuous goal.
