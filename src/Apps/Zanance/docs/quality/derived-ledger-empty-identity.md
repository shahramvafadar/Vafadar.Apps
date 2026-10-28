# Complete-source fallback for an empty derived identity (D-611)

## Reproduced defect and exact product change

An otherwise valid optional ledger block can contain an all-zero entry GUID while
its compressed envelope, row count and recomputed SHA256 remain valid. The original
decoder passes that GUID to `LedgerEntry`, whose unchanged domain constructor throws
`ArgumentException`. The optional reader declines `InvalidDataException`, so this
domain exception escapes instead of selecting the original complete SQLite reader.
Even a valid prefix row must never become a partial Home publication.

Validate only the decoded entry GUID immediately before that constructor. An empty
derived GUID now throws `InvalidDataException`; the existing reader's refusal and
complete-source fallback handle it. Do not catch all argument exceptions, normalize
an original identity or change the domain constructor. All32 fields, original row
ordering, source/schema/revision guards, writer conflict policy, compression/digest
checks, format version and scalar behavior remain unchanged. No schema/model or
released migration is changed, and no localization string or UI control is added.

## Focused managed and package evidence

Six fresh cases fail against the original decoder, then pass after the exact guard:
four codec cases cover first/second row and buffered/unbuffered decoding; two actual
SQLite cases cover first/second damaged row. The SQLite cases recompress the changed
identity and update only its derived payload/digest, then prove complete original
field equality, unchanged cache tokens and exact damaged payload/digest retention.
No getter repairs the cache or publishes a valid prefix.

Three affected existing cases also pass: two complete-field round trips and the
digest-valid incomplete-compression fallback. Exact result XML proves baseline6/6
failures and corrected9/9 passes, including six new distinct managed cases. Preserve
the first harness build's cancellation-token analyzer refusal; it is corrected before
the baseline runs. No complete old suite or accepted financial/device cohort repeats.

Strict Windows passes with zero warnings/errors after explicitly scoping restoration
to Windows. Preserve the initial cross-framework NU1201 restore refusal. Complete
signed Offline Android also passes; preserve the initial process-local emulator-SDK
`aapt2.exe` location refusal and correct only the build process's SDK selection.

Product artifact:
`E:\Zanance.ReviewBuilds\D611-shipping-package\pro.vafadar.zanance-Signed.apk`,
SHA256 `67c7cda8090d4cc1a14a1ab7f85317d4fc664f55bb9a8a52b703746201ca7c05`,
97062815bytes,384 own AOT images/408 ELF/1608 statically16KiB-aligned LOAD segments.
ZIP CRC, actual16KiB ZIP alignment, ABI machine identities, LOAD offset congruence,
offline/nondebuggable declaration, original-owner certificate and v2/v3 checks pass.
Its actual x64 Data image contains the new exact refusal marker.
Static alignment is not hardware16KiB acceptance.

## One original native fallback, with accepted setup preserved

The retained API36 AVD has only about700MiB free. Observe its original package/settings
metadata and stop it without installing a package, opening its source or changing its
byte-exact configuration. Do not weaken the original staging-space guard. Create one
fresh `Zanance_D611_Identity` AVD using the already installed API36/x64 image, scoped
SDK/user/AVD directories, port5572 and the existing ADB5038 server. Configure only
this fresh data partition to8G. The first avdmanager Java-path batch refusal occurs
before creation; use the same installed Java through its process-local short path.
No SDK package download, global setting or phone command occurs.

The fresh independent package is `pro.vafadar.zanance.ledgeridentityreview`, UID10216,
run `68bd1566652d4b33b8e6e7c03b514d18`. Original covered startup creates its own protected
source. One original transaction accepts two fictitious rows: income100EUR and
expense10EUR against a zero-opening account. A durable pre-write plan binds the IDs;
no pre-existing source is replaced and no accepted setup is replayed.

PID4247 accepts that setup, then records `SqliteException` before its first terminal
snapshot receipt. Preserve the failure: its exclusive original throw line was not
recorded. The helper attempted a raw keyed-source-to-plain-memory backup; replace
that diagnostic adapter with the existing bounded `SqlCipherPortableSnapshot` and
`SqliteMemorySnapshot` APIs, not a new encryption implementation or weaker comparison.
An unused intermediate observer build's nested conditional is corrected before
installation; no native action is replayed to repair compilation/observation.

App-only observer upgrades7c7a96ce to8d59248e to2e0afa7d retain the same UID/CEDE/
permissions and actual Core/Data/Brotli/native engines. The first used APK and used
inspector APK are retained privately. PID5233 reads the same accepted IDs, both original
EF/store rows, all five expected ledger/history tables and53 applied/known migrations.
The complete original-row SHA256 is
`9f997f6a5680cd18d1cd592daacd8d9e7e4116e9645558150a1376efc95ef97e`.
This full baseline is captured after observer correction, not retrospectively before
the first error. Inspection logs only fixed stages, schema names/counts, digests and
numeric SQLite codes; no raw exception/SQL/path/key/payload export is introduced.

An explicit continuation requires that receipt and proves the matching-digest empty
identity damage is already present. It never repeats the accepted cache update or
financial setup. PID5464 then captures the full source through the original32MiB-bounded
memory exporter and publishes the original HomePage/HomeViewModel. Observe exactly
one derived-block command followed by one complete scalar command. The original
native formatted90EUR balance and104 owned public nodes are observed. The original
`SqliteSnapshotVerifier` compares all schema/header/typed tables after publication,
including the still-damaged cache; no fields or cookies are normalized and no repair
occurs. Plain memory buffers are cleared, no plaintext file or key is exported.

This is one native functional boundary, not six-language layout, cold/phone/whole-device
or performance acceptance. Native Core/Data PE bytes differ from the product x64 images;
do not claim shipping-equivalent financial-image or speed measurements. Both actual
packages contain the new guard, while original-source and finite native controls above
define this correctness evidence.

## Retirement, explicit refusals and pending physical adoption

Remove only terminal fresh UID10216 and prove pm-u absence. Five fresh settings,
boot/display and shell state match. One owned XML/40051logical bytes is hash-bound,
removed and proved absent. Stop only the fresh AVD, prove its actual host/serial/port
absent, then delete it and its registration with avdmanager:24 files/5174014792 logical
bytes retire. The retained AVD configuration remains exact and both original ADB server
PIDs remain44944/74652. No physical phone or private Main database/key is touched.

Standard Clean removes all2806 registered outputs/903120142 logical bytes under the
three fresh D-611 artifact roots. Further removal of those roots is rejected before
process creation with `blocked by policy`;1513 files/250041737bytes remain. Separate
removal of the fresh isolated SDK user home is also rejected before process creation;
six files/7732bytes remain, including its generated temporary authentication material,
whose contents are never read or hashed. No subset, alternate cleanup or escalation
follows either refusal, and no earlier rejected scope is retried. Keep used signed
packages and evidence. These are logical sizes, not attributed host free-space gains.

This batch does not touch or upgrade Samsung; its last verified Main baseline remains
recorded as D-606 while D-607's pending physical-reminder cohort and owner reboot decision
are preserved. No new phone observation is claimed. Do not upgrade it to exercise permission
or disturb that lineage. The new complete product APK is ready for later guarded
no-reset adoption; that physical gate stays open. Document/push this locally verified
fix and schedule one exact-head CI check after16minutes. The overall goal, broad
stability/security/accessibility, iOS/provider/Store/publication gates remain open;
TalkBack stays last under D-594. Evidence:`%TEMP%\Zanance-D611-current.txt`.
