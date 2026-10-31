# Source-built native cipher producers

Source, compiler, diagnostic, compatibility and immutable package checks are independent gates.

`Observe-WindowsToolchain.ps1` records only public installed compiler metadata before
the existing qualification in CI. It neither executes nor admits an unreviewed compiler.
The31October host18.11.12328.437 remains unqualified until its actual signed executable
and native producer evidence are reviewed; local baseline evidence is not hosted proof.
[Current observed drift and unchanged gates](../../../src/Apps/Zanance/docs/quality/retired-billing-review-and-hosted-compiler-drift.md).

## Pending 31 October hosted producer qualification (D-683)

Original native run41185441600/job124491318973 on e1cbb1be observes installer
18.11.12328.437 with actual compiler19.51.36260.0/SHA256
`b0a1818ebae6da7f18230a84e76ce839212584ac4c2ab55b97a0d5268085da57`, valid Microsoft
signature, pinned tools14.51.36231 and SDK10.0.26100.0 present. These executable bytes
already belong to the reviewed compiler policy; this is actual hosted evidence, not
a substituted local installer fixture. The exact official runner inventory is linked
from the policy. Neither compiler hashes nor signature/tools/SDK predicates change.

The separate `shahram/native-producer-4.19.0-candidate.10` branch admits only this
observed installer metadata to the existing producer-only lane. Candidate.10 is an
unpublished export identity; it is not a consumer or Main adoption. Require two fresh
equal native outputs, original source/diagnostic/schema gates and actual archive-entry
review before deciding whether this host can reuse an already identical consumer or
requires a new immutable package version. Never merge the installer-only policy as
completed consumer recovery, change existing package-entry pins or rerun Android here.

The first producer run41186798703 actually receives the older image20261021.278.1/
installer18.11.12315.225. Its two fresh native outputs both equal the existing candidate9
DLL, but they do not qualify the requested newer host. Original schema execution then
stops at a real current source-closure compile error: both independent compatibility
projects omit `BackupRestorePublication`, now required by the original protected receiver.
Link that actual production helper rather than removing its publication checks or
introducing a stub. Strict Windows compile-only closure passes with the unchanged,
exact reviewed local candidate7 DLL; no original native-schema run is claimed from that build.
The complete Android compatibility build also passes with the two unchanged reviewed
candidate5 native slices. An initial invocation omitted their required directory and
failed packaging after C# compilation; the corrected build supplies both exact slices.
Both projects now refuse missing native inputs during `PrepareForBuild`, before compiling
their compatibility callers. These local checks do not execute schema or device acceptance.

Producer-only requests now bind the actual observed image/compiler to
`windows.producer.request.json` before fresh native compilation. Seventeen public-metadata
fixture cases preserve wrong-image/signature/compiler/SDK/candidate refusals; fixtures
are not actual hosted output acceptance. An older rolling image must stop before
repeating its already successful native work. The ordinary reviewed compiler and package
gates remain separate. Future producer artifacts also retain the exact fresh DLLs and
their required notices, so a later failed step does not erase completed native evidence.
These artifacts are diagnostic outputs, not consumer-admitted packages or release approval.
An approved compiler executable is insufficient to reuse an existing native package identity.
`Pack-Windows.ps1` refuses changed DLL or package-entry bytes under a pinned consumer version.
Keep baseline and serviced producers separate; never allow alternative bytes for one version.

## Hosted Windows drift observed on 2026-10-22 (D-478)

Native run39852707414 on commit8aeb8f02 qualifies the actual Microsoft-signed compiler
19.51.36260.0/SHA256b0a1818ebae6da7f18230a84e76ce839212584ac4c2ab55b97a0d5268085da57.
Installation18.11.12315.225 is listed in official windows-2025-vs2026 image20261021.278.1.
Two fresh outputs both have native SHA256
b02ca2545492b8584b2c15010ac24f6fc5a2b5e6e45a11c6c4ba825ccf277744.
Original schema/key/tamper/portable checks pass before the immutable consumer refuses those bytes:
candidate.8 remains exclusively45d430bcabce5daf3f8513fdc27ec25ff48590c064fe9ce55c7bbadd26ee6521.
The public evidence artifact12458666625 has ZIP SHA256
efabfe48fa8075b4f6404dcb993bfa9af4f421a2afabbfff3492e99db0c969af.
These observations establish host-output drift with the same reviewed compiler; they do not
identify an internal compiler, linker or SDK cause. Android native qualification already succeeds.

The existing native workflow supports producer-only branches named
`shahram/native-producer-<stable-source>-candidate.<number>`. That mode keeps actual compiler,
two-fresh-output, source, diagnostics and original-schema checks, exports an unpublished candidate
package, and skips unrelated Android/consumer workloads. It does not accept that package for Main.
For this drift, candidate.9 is prospective until the original producer result and exact archive
entries are inspected. Never infer qualification from branch creation, a local metadata fixture
or an elapsed wait; do not overwrite candidate.8 or weaken its guard to make the job pass.

Consumer adoption must preserve old identities, bind a new qualified host to its own immutable
version, and then pass ordinary main/PR package, production-assembly persistence and workload gates.
This engineering qualification does not accept the app, Android/iOS, private-key recovery or release.

## D-478 producer archive qualification

Producer-only run39857667608 on9823a734 succeeds and exports candidate.9. Original artifact12460073284
has SHA256634d81028eba5401ab90ca6f590f2316fb28cbf806a7d9680d7861e5e725768d; actual nupkg SHA256 is
bc5b553c0392c7afc806bf17564255a45a9ee175e11cac7f43fbad48818f1f72. Every one of its five entries
matches windows.hosted20261021.package.contents.json, including the original matching b02ca254 DLL.
Keep candidate.7/baseline and candidate.8/servicing unchanged. Select-HostedWindowsProducer.ps1
qualifies the actual signed host, resolves the exact variant and persists it only to the current
job environment before native/consumer work. Installation18.11.12315.225 with reviewed b0a1818e
compiler selects hosted20261021/candidate.9. Unknown or wrong host/compiler pairs refuse selection.
Ordinary main/PR consumers retain their full source-build, archive, persistence and workload gates;
the downloaded producer package is evidence, never a replacement source-built consumer feed.

## D-521 - Actual API24/16KiB storage and controller evidence

The immutable current Android native slices are reused, not rebuilt or repackaged. The independent
103AOT x86_64 compatibility APK qualifies actual original model/migrations/snapshots, controlled
candidate/selection process interruption, missing-key refusal and protected recovery on actual
API24/4096 and API35/16384. A late host poll is not an export barrier: use the shared guarded
AndroidExportObservation module to arm before launch and validate original KeyPrepared afterward.
Read-only checkpoint diagnosis and explicit retained-source adoption preserve failed observations
and avoid another financial seed. Both old late observations remain failed; successful source,
phone and financial cohorts are not repeated. The intentionally debuggable helper and private
fixtures remain outside Main; only public phase facts leave its own UID.
[Native evidence and cleanup](../../../src/Apps/Zanance/docs/quality/android-storage-runtime-compatibility.md).


### D-522 - Reuse the qualified package for profile runtime cohorts

Reuse D-521 signed441faf38 without rebuilding for nine previously unobserved original profile/
restore cohorts on each isolated API24/4096 and API35/16384 x86_64 runtime. Preserve each owned
source run across its cold phases, claim a fresh public receipt nonce before dispatch and prove
prior PID absence; never replay an accepted phase. Audit all36 records and installed public code
before uninstalling only new helpers and stopping owned emulators. Private fixtures stay in their
UID; retain existing signed code and public local evidence. No Main/phone/global SDK changes.
See [actual cohort and limits](../../../src/Apps/Zanance/docs/quality/android-profile-runtime-compatibility.md).
