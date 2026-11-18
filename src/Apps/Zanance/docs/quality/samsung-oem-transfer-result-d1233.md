# Actual Samsung two-helper transfer result (D-1233)

## Result: applications arrived; data exclusion is inconclusive

The owner completed one Smart Switch transfer from Samsung SM-A176B/Android16/API36 to the personal S24 Ultra SM-S928B/Android17/API37 after confirming only the two independent helper applications were selected. The owner reported the completion display as **2 items / 26 MB**. This is the owner's observed transfer summary, not an independently measured payload size or proof of application-data transfer.

Both destination APK hashes exactly match the reviewed source APKs. Read-only fixed synthetic-result providers show **all nine payload domains missing in both the candidate and the positive control**. Neither target package has a source-preparation marker. Therefore the two applications arrived, but the positive control did not establish data-transfer capability for any domain. **Zero of nine domains qualifies as accepted rule-enforced exclusion from this trial.** No candidate payload was observed. This is inconclusive evidence, not a detected Main data leak and not a general security pass.

## Package and fixture identity

| Role | Source UID | Destination UID | Exact transferred APK SHA256 |
| --- | --- | --- | --- |
| Candidate |10459|11030|`531aa116b3c7982a9d99702cda26533c4d851035c223923afa6a029ec0555ad7`|
| Positive control |10460|11031|`ad318a7766038ddac28b5f1c94008546ef3d50820cc8b1291c6ad7777d2cea5a`|

The candidate package is `pro.vafadar.zanance.backupexclusionreview`; the control is `pro.vafadar.zanance.backupcontrolreview`. Both were absent on each phone before source preparation, and both source packages held all nine expected synthetic values before transfer. Target preparation was never invoked, and neither a destination APK installation nor a replacement transfer was used to manufacture acceptance.

The run identity is `cdd07a5e54a14ef0b7d630c51b4254aa`, and the known synthetic payload SHA256 is `ec24838a25a82c1a409f8aad96ddeedc1ae0629faa9cbe2d56c509d7982176ac`. The providers emit bounded synthetic presence/equality states only, without Main storage, keys or raw paths.

| Domain | Source candidate/control before transfer | Target candidate | Target control | Qualification |
| --- | --- | --- | --- | --- |
|root|Both exact|Missing|Missing|Inconclusive|
|file|Both exact|Missing|Missing|Inconclusive|
|database|Both exact|Missing|Missing|Inconclusive|
|sharedpref|Both exact|Missing|Missing|Inconclusive|
|external|Both exact|Missing|Missing|Inconclusive|
|device_root|Both exact|Missing|Missing|Inconclusive|
|device_file|Both exact|Missing|Missing|Inconclusive|
|device_database|Both exact|Missing|Missing|Inconclusive|
|device_sharedpref|Both exact|Missing|Missing|Inconclusive|

The app arrival and missing synthetic payloads do not identify which OEM mechanism or eligibility condition prevented the control data from arriving. No exclusive cause is asserted. The control's cloud exclusion and local D2D inclusion are retained as reviewed inputs, not silently changed for a second attempt.

## Main and device preservation

Before/after public Main APK SHA256, UID, CE/DE metadata, versions and recorded permissions match exactly on **both** phones. The recorded settings/key presence, locale, geometry, boot, hardware identity and ADB reverse state also match exactly. The older S24 Main remains older; no Main upgrade was needed for this synthetic transfer.

Neither Main database nor key was read. Public identity equality is not proof of every private financial row or key byte. No Main installation, reset, uninstall, restore, genuine authentication, account preparation, financial Save or successful old test was performed by this operation. The owner operated the real Smart Switch selection/transfer boundary; no other Android user or private container was accessed.

## Terminal handling and retained evidence

The fixed target observations and comparison were saved before removal of fresh helpers. All four exact reviewed helper installations on owner user0 were removed successfully and their absence verified: sourceUID10459/10460 and targetUID11030/11031. Both original Main public identities were then compared again and remained exact. No Main or unrelated app was removed, launched or altered during retirement. Local signed packages, source, compiler output, source-preparation results, target public/hash/results, the owner's completion receipt and preservation comparison remain under `artifacts/d1233-oem-transfer-preparation`.

This one transfer is terminal; it must not be replayed as if it were an unattempted check. No fresh transfer, rescue variant or destination preparation is part of this result. The broad OEM exclusion gate remains unqualified where a positive transfer control is required. See [prepared comparison](prepared-samsung-oem-transfer-d1233.md) for the original policy, independent packages and finite acceptance rule. Canonical parent states and retained crash/financial checkpoints remain unchanged.

## Follow-up diagnosis

[Independent control-route diagnosis](transfer-control-diagnosis-d1234.md) resolves the older-Main hypothesis for the helper payloads and establishes a nine-domain positive restore control on a fresh Android D2D framework. It does not replay or relabel this OEM transfer as accepted.

## Positive-control route review on 2026-11-18

A read-only follow-up verified the installed Main SHA-256 `5699adea9718e5f6a3cac3701e6e05667ceba4de6500a95f672835f5f32cfad6` on both Samsung devices. Smart Switch Mobile reported version 3.7.74.7 on both. The retained main/system logs were filtered on the devices to the two exact helper package names and backup, restore and Smart Switch tags. A17 returned two `restoreAtInstall` entries (`restoreSet=0` for each helper). S24 returned no matching entries. The A17 entries record installation-time restore checks. The available logs establish no payload backup or restore result for the OEM session, and log retention limits the conclusion.

The available Android backup transports on both phones were Android LocalTransport, Google Play services D2D and cloud transports, and the Google restore service. The selected transport was the existing Google cloud transport on both. Google documents a D2D simulation and a two-device test that moves an archive through the test host. These would exercise Android backup paths rather than the Samsung Smart Switch session. The nine-domain Android positive control is already recorded in [D1234](transfer-control-diagnosis-d1234.md). [Android backup test guide](https://developer.android.com/identity/data/testingbackup) · [Android two-device test guide](https://developer.android.com/studio/preview/backup-restore-testing#test-transfers-between-devices).

Samsung documents private app storage among Smart Switch's backup exclusions. No supported Smart Switch route was identified that can carry the private payload of the independent positive control on this pair. A shared file or APK arriving establishes transport for that type of object only. [Samsung supported content](https://www.samsung.com/us/support/answer/ANS10001412/).

The remaining acceptance dependency is a Samsung workflow that demonstrates restoration of a known third-party private payload using synthetic data. If a supported transfer requires initial device setup, use a spare destination dedicated to testing. Predeclare the claimed domains, record the same OEM transfer session for both helpers, and verify the positive payload on the destination before accepting the absence of the exclusion probe's payload for each domain. This review performed only package metadata, transport inventory and scoped historical log reads. Samsung OEM exclusion remains inconclusive. The existing transfer and the Android positive control were preserved as terminal evidence.
