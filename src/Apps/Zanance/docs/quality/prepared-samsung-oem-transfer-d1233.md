# Prepared bounded Samsung OEM transfer comparison (D-1233)

## Ready scope, not transfer acceptance

On2026-11-17 two independent offline SDK applications were prepared for a finite Smart Switch comparison from the original Samsung SM-A176B/Android16/API36 to the owner's personal S24 Ultra SM-S928B/Android17/API37. Both Main installations are intentionally preserved; the S24 Main is an older installation and is not an input to these synthetic probes. No reset, uninstall or update of either Main is part of the transfer plan.

Both devices had neither helper installed before the operation. Both original Main public APK/UID/CE/DE/permission identities, OS settings/key presence/locale/geometry/boot/reverse were retained before source installation. Owner user0 is explicit; unrelated Android users/private containers are not queried. The earlier default all-user package command reported a user150 SecurityException; it did not authorize access to that user. Subsequent queries deliberately restrict the operation to the ordinary owner user0.

## Exact prepared pair

| Role | Package | Signed APK SHA256 | Backup policy |
| --- | --- | --- | --- |
| Candidate | `pro.vafadar.zanance.backupexclusionreview` | `531aa116b3c7982a9d99702cda26533c4d851035c223923afa6a029ec0555ad7` | allowBackupfalse; original nine-domain exclusion |
| Positive control | `pro.vafadar.zanance.backupcontrolreview` | `ad318a7766038ddac28b5f1c94008546ef3d50820cc8b1291c6ad7777d2cea5a` | allowBackuptrue; local D2D nine-domain inclusion |

Each is ARM64/minAPI31, non-debuggable and contains17AOT/27ELF/98statically checked16KiBLOAD segments. Signaturev3, ZIPCRC/alignment and absence of declared device/network permissions were verified. This is not production shipping identity, hardware page-size acceptance or a financial application build.

The candidate links the unchanged production `data_extraction_rules.xml` and `backup_rules.xml`. Their original SHA256 values are `faad705268d12e93aa951812df02bab7f982e4d5b9a4c2cfeb39b291ce4c954a` and `41d5d4188ea24f083dc652b0b4e48e55d217e42e79fe1207817a3d74948c4b75`. Compiled extraction domain sets were independently checked: root/file/database/sharedpref/external/device_root/device_file/device_database/device_sharedpref are all excluded in both candidate sections. The control includes those nine only for device transfer and excludes all nine from cloud backup.

The initial control preparation excluded only the cloud root domain. Before any installation it was tightened to all nine cloud domains, then only that changed control was republished. The prior uninstalled package is retained separately. Initial verification used the wrong badging label and expected numeric allowBackup output; its refusal is retained. Existing utility outputs were reused to repair the observer; no installed package or successful native operation was replayed.

## Source preparation, no production records

Each fresh helper was installed exactly once on the source, UID10459(candidate)/10460(control). The fixed read-only result provider first showed nine missing payloads. Exactly one source-preparation invocation per package then produced all nine expected synthetic values, with SHA256 `ec24838a25a82c1a409f8aad96ddeedc1ae0629faa9cbe2d56c509d7982176ac` and source no-backup guard present. No financial account, ledger entry, password, device key, authentication or production database is part of either helper.

The ordinary launcher/provider only inspects. Explicit source preparation is constrained to the compiled run identity and source model. Its exclusive no-backup guard precedes payload writes and refuses reseeding. Result JSON contains only package/run/UID/API/model and bounded synthetic presence/equality per domain; no raw paths or private Main data. The target must never be prepared, and its source-preparation guard must remain absent.

Shared source, compiled DLL/PDB, exact signed APKs, verification logs and original policy hashes are retained under local `artifacts/d1233-oem-transfer-preparation`. Two standard cleans removed272files/54,932,369logical bytes. No supplementary/frozen deletion, old test suite, successful financial operation or consumed crash interpretation was repeated.

## Required real transfer and decision boundary

The concrete local review plan is `artifacts/d1233-oem-transfer-preparation/owner-review-proposal.md`. It names only **Zanance backup exclusion review** and **Zanance backup transfer control**. Smart Switch is already installed on both devices. The owner must use Custom selection, deselect all unrelated categories and apps, and verify only these two helpers before the final Transfer action. Existing Zanance, other apps/accounts, media, settings, contacts/messages and Secure Folder remain unselected. Authentication and final personal-device transfer selections remain with the owner under D-597.

If Smart Switch cannot isolate exactly these helpers, requires broader changes or omits a helper, record that outcome and stop; no full-device migration/reset/account setup is an alternative. Do not install a missing target helper afterwards to pretend it arrived by OEM transfer.

A domain is accepted only if the positive control actually arrives with its exact expected payload and the candidate payload is absent, with admitted package/signature/run identity and no target preparation marker. A missing control is inconclusive. A missing candidate package is OEM package omission, not automatic proof for all nine domains. Any candidate payload presence is a failure. No result of the actual OEM transfer exists at this preparation point.

Samsung documents selective transfer and also transfer limitations; source policy alone is insufficient to establish this device pair's behavior. See [Samsung transfer instructions](https://www.samsung.com/us/support/answer/ANS10001345/), [Samsung supported content](https://www.samsung.com/us/support/answer/ANS10001412/) and [Android backup/extraction rules](https://developer.android.com/identity/data/autobackup).

Actual owner-controlled transfer, final target/provider observations and retirement of only fresh helpers remain outstanding. Both Main versions/data, retained evidence and canonical parent states remain protected. This preparation does not close the general OEM/cloud/security/release gates.

## Subsequent terminal observation

The owner subsequently completed the one reviewed transfer. Both APKs arrived but neither positive-control nor candidate synthetic payloads arrived. See [the actual transfer result](samsung-oem-transfer-result-d1233.md); preparation is complete and this transfer is terminal, while rule-enforced domain exclusion remains inconclusive.
