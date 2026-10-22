# Common hosted CI recovery (D-433)

## Authoritative failures

- [D-431 CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39551870040): Windows installer qualification stops before native production; the Linux test job reports 4784 total, 4757 successful, 25 skipped and exactly two failures. Android passes; iOS is skipped.
- [D-422 CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39453841405): the same mode-policy and original Undo-receipt failures occur earlier. This establishes a shared cause rather than attributing every recent failure to the newest UI change.
- [Official hosted image inventory](https://github.com/actions/runner-images/blob/win25-vs2026/20261018.275/images/windows/Windows2025-VS2026-Readme.md): installer 18.11.12306.390 and SDK 10.0.26100.0. Inventory metadata alone does not qualify executable bytes.

## Repair and focused evidence

The D-422 and D-431 hosted test logs identify the same two failures: the mode-policy scanner treats
review-only source fixtures as shipping readers, and a shared-membership Undo fixture supplies a
fabricated group without the original deletion receipt required by D-414. D-431 Windows additionally
refuses installer 18.11.12306.390 before compiling, matching official image 20261018.275.1 inventory.

The scanner now names the 13 exact additional fixtures and validates their complete outer review-only
build gates; it does not exempt the Diagnostics directory or ordinary Release readers. Seven new
positive/negative gate cases prevent unsupported conditions, outer fallbacks and escaped source.
The Undo policy fixture first obtains one real committed deletion receipt, then revokes membership.
A new case proves refusal leaves both deleted rows and that original receipt intact, followed by
successful original restoration when membership returns. No production commercial or provenance
guard changes. Eight focused Core and 11 Data cases pass after the original failures are reproduced.

The official installer metadata is added without changing either approved compiler hash/file-version
pair, pinned MSVC/SDK, signature validation or immutable package contents.17 local toolchain cases
pass on the existing 18.10.12201.205 installation and compiler 19.51.36257.0; other installer versions
are metadata-only checks. Hosted bd6e9484 CI and native Windows pass; its Android package refusal is preserved below, followed by final hosted recovery.
Application sources, runtime behavior and the complete D-432 APK remain unchanged. Successful app,
Android, backend, native financial and device checks are not replayed locally. D-386 and external
gates remain unchanged.

Both original managed failures reproduce locally. The first alternate-volume Core invocation fails
only because repository discovery starts above its binary; that is retained as an invocation failure,
not the intended policy baseline. The corrected private source-root fixture points to the actual
unchanged repository sources, and reproduces the same diagnostic reader collection as hosted CI.
All test output and large builds stay on the alternate volume; no unrelated file or archive is read.
Strict Core/Data compilation has zero warnings/errors. The affected policy cohort contains 19 cases,
including eight new behaviors; no complete-suite count increase is claimed from those focused runs.
The native policy retains refusal of unknown installers, tools/SDK changes, malformed/changed hashes,
wrong executable version/signature, duplicate policy and missing compiler. No fixture executes a
compiler/bootstrap, and no compiler acceptance is inferred from an installer version alone.

## Hosted acceptance

Commit `bd6e9484567157e18928ae8f2eb8128bdaca21a8` passes [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39565236657)
and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39565236611).
The [native workflow](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39565236511) passes exact Windows compiler qualification,
two fresh reproducible candidates, immutable package checks and protected persistence. Android job
`119105809799` stops before compilation: `host-proof.json` says `Host build package differs from review: ca-certificates`.
Its [original artifact](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39565236511/artifacts/12329586559) is retained.
The first one-shot review and its schedule are complete; no successful job is rerun manually.

### Restore the original Linux host closure

A digest-pinned base did not freeze subsequent APT installs. The moving archive now supplies
`20260816ubuntu0.24.04.1`, while the unchanged host lock requires `20260601~24.04.1`.
The final stage now restores the original signed Ubuntu archive snapshot `20261012T000000Z` using
only its explicit HTTPS URI, Noble release/updates/security and main/universe. Any missing index is
fatal, retrieval retries are bounded, and no moving-index fallback, unsigned package or disabled
TLS/signature check is permitted.

A separate stage installs immutable Noble release `ca-certificates=20240203` to bootstrap HTTPS trust.
Only its public certificate bundle enters the final base; none of that stage's moving dependencies do.
Snapshot installation replaces it with the exact reviewed final package. This uses the
[official Ubuntu snapshot service](https://snapshot.ubuntu.com/), retaining every host-package,
NDK archive/revision/compiler, source, ABI and immutable-package gate.

A fresh local image builds and the unchanged `verify-host.py` passes all locked package versions,
NDK `30.0.16248370` and compiler identity. Clang SHA256 remains
`ad535f0b6fc303edba958b51636169ff0291c6e36463236c99ff8abcaa34247c`.
Local image identity: `sha256:22ae1bc66640846a4f887ff887dbbe53e61069c096fbc7cca6e20a48df981357`.
The no-certificate probe and an incomplete remote-index attempt are retained failures; TLS was never
weakened. No native application binary or accepted package changes. Hosted Android clean builds and
the stock cipher suite still require the follow-up commit's actual result. Application sources/APK
in this CI-only step remain D-432.

Account monetary inputs remain the next complete feature batch (D-434), followed separately by holding
money contexts. Dedicated ANR/crash and Android accessibility work retain the owner's deferred order.


### D-433 final hosted recovery

Commit `05dbecf089dabc5f433b12dbede82f4e97b10d2b` passes all three workflows:
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906007),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906028) and
[Native cipher candidates](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39572906072).
The actual Windows job passes exact compiler qualification, two clean candidates, immutable package,
drift refusal and protected persistence/workloads. Android passes host restoration, two clean
candidates, malformed-slice refusals and the official stock suite with reviewed static crypto.
The single scheduled check is complete and deleted; no successful job is rerun manually. The earlier
bd6e9484 Android package refusal remains historical failure evidence. This establishes hosted CI
recovery for that exact commit, not app/device/provider/store/release acceptance.

## D-456 - Audit delayed failures against the current head

The delayed reports refer to older commits. Read only failed-job logs, preserve their original
results and compare the actual cause with current source before changing or rerunning anything.
The following runs share the already-corrected diagnostic mode-policy scan and, from D-415 onward,
the original holding Undo-receipt fixture: 39291571125, 39406434176, 39409094148, 39425064907,
39439259834, 39445968870, 39467791585, 39479168888 and 39521820617. The D-420 and D-428 Windows
jobs additionally refuse installer 18.11.12306.390; D-433 already reviews that installer metadata
while preserving exact executable/hash/SDK checks. These failures do not justify another local
suite, Android build, native workload or phone sequence. Current pre-change head 9215c29e has
successful [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39726959692), including
Libraries and tests, Windows and Android; iOS remains intentionally skipped.

One separate failure still has an unchanged source cause. Backend run
[39291571470](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39291571470), job
118193260297, fails Different_pairs_join_original_feed_after_first_caller_cancellation with
MarketSourceResponseRefused. The fixture signals Source.Called before invoking Respond. Another
thread can then cancel the first caller, join the second and construct a response before Respond
captures the original request. Passing runs do not eliminate this race. The corrected existing
test awaits a typed completion set by Respond with the actual request before cancellation and
response construction. It retains the original one-request, caller-cancellation, cross-pair value
and request-provenance assertions; no production adapter, transport refusal or cancellation rule
changes, no delay/retry and no assertion weakening.

Strict affected API/test compilation passes with zero warnings/errors. Exactly the one changed
test passes (one executed, zero failed/skipped); the test inventory does not increase. The first
local invocation uses an unsupported MTP filter on the actual in-process runner and executes no
tests; its diagnostic is retained, followed by the runner's supported -method selection and exact
one-result XML validation. No successful unrelated tests, app builds or device checks are replayed
locally. Hosted completion for this correction is pending at commit creation and must be checked
on its exact pushed SHA. D-452's unfinished phone continuation, all other schedules and external
or general stability/accessibility/release gates remain unchanged.

## D-476 - Restore exact native-review build boundaries

Actual failed-job logs from D-471 CI39839824109 and D-474 CI39846843688 show the same two causes,
still present at pre-change head9c05786c. Windows and ordinary Android Release builds succeed.
The Android Debug billing-sandbox step reports thirteen CS0103 references to
ImportFinancialNativeReview from BudgetFinancialNativeReview, BudgetPeriodCaptionReview and
OriginalAmountErrorCaptionReview. Those three callers allow ordinary Android DEBUG, while their
dependency and actual entry routes allow only DEBUG/WINDOWS or ZANANCE_INDEPENDENT_HOME_REVIEW.
Align the callers with that existing dependency gate; do not broaden the dependency into Main.

Libraries and tests separately fail No_page_reads_the_mode_itself on those exact three fixtures.
Add only their paths to the existing explicit fixture list. The existing complete outer-gate
validation remains mandatory before exclusion; ordinary shipping mode readers are still forbidden.
No blanket Diagnostics exception, skipped assertion, workflow retry or configuration weakening.

The exact affected strict Debug Android billing-sandbox build now succeeds with zero warnings
and errors. The corrected source-policy method passes once. Seven existing negative/positive
outer-gate cases pass in the earlier selected invocation; only its path-dependent source method
initially fails because external output has no repository ancestor. Continue that one method from
owned ignored repository-local output; do not rerun the seven successes. An initial unsupported
--filter-class invocation executes no tests and is retained as a command failure. Final selected
evidence is eight distinct existing cases, zero new tests; strict Core test compilation is clean.

No visible behavior, Release app code, monetary data, schema, permission, SDK or shipping APK changes.
Retain D-471's complete signed APK and original running-app evidence. Do not repeat successful
Release/Windows builds, financial writes or native scenes for these preprocessor/test-only changes.
The four overdue D-471 through D-474 exact-head checks are consumed once and their own schedules
deleted: CI failure/cancelled/failure/failure respectively, CodeQL success in each. Latest D-475
and this correction require their own exact pushed-head result; hosted success is not inferred
from the local correction. All unrelated schedules, goal and external/release gates remain open.

Failure sources: [D-471 CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39839824109),
[D-474 CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39846843688).

## D-477 - Review the new hosted Windows installer metadata

D-473 [CI39844124741](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39844124741)
has one additional distinct failure: Windows job120053507577 refuses installation18.11.12315.225
before any native production. Its actual image is windows-2025-vs2026/20261021.278.1. The original
[official inventory](https://github.com/actions/runner-images/blob/win25-vs2026/20261021.278/images/windows/Windows2025-VS2026-Readme.md)
lists that exact Enterprise installation and SDK10.0.26100.0. Add only this installation version
to windows.compiler.acceptance.json and update its dated inventory link.

Both approved compiler executable SHA256/file-version pairs, exact Microsoft Authenticode signer,
MSVC14.51.36231, SDK10.0.26100.0, immutable baseline/servicing package identities and all refusal
rules are unchanged. Metadata approval never approves a new compiler hash or native package.
The hosted producer must still prove its real executable and package entry hashes; any new bytes
remain a separate refusal. No runner/workflow bypass, installation or dependency update is made.

Existing Test-WindowsToolchain.ps1 passes18 cases on the actual local18.10.12201.205 installation,
compiler19.51.36257.0/SHA315a654ea116864516a1674858e587e535e3bc3045ff32ed2f2739a2c1ec5640.
The new installer version is exercised as metadata against those real signed bytes; unknown
installer, changed tools/SDK/hash/file version/signature policy, duplicate/malformed/empty policy,
missing compiler and unknown package-variant refusals remain. This is local metadata regression
evidence, not a claim to have installed the hosted version or proved its actual compiler bytes.
No successful app build, business suite, phone action or shipping APK is repeated/changed.
Hosted qualification on the exact follow-up SHA is pending at commit creation. D-476's two
source corrections remain included; external/stability/accessibility/release gates stay open.

## D-478 - Separate immutable producers across hosted image servicing

On8aeb8f02, CI39852707648 now passes Android Release/Debug billing and Libraries and tests:
5097 total,25 skipped,zero failed. CodeQL39852707614 succeeds. The actual complete test result,
not an assumed local fix, closes the two D-476 causes on this exact head. Native39852707414 also
passes official Android slices/stock cipher. Its Windows job120082812755 qualifies the real
18.11.12315.225 installation and original b0a1818e compiler, builds two fresh matching native
outputs, and passes original schema/key/tamper/portable compatibility before immutable packing
refuses a changed DLL under candidate.8. The ordinary Windows job refuses that same old identity.

Both original fresh manifests are equal, native SHA256
b02ca2545492b8584b2c15010ac24f6fc5a2b5e6e45a11c6c4ba825ccf277744,4108288bytes. Compiler SHA256
b0a1818ebae6da7f18230a84e76ce839212584ac4c2ab55b97a0d5268085da57/file19.51.36260.0 is unchanged.
Public artifact12458666625 is88047bytes, ZIP SHA256
efabfe48fa8075b4f6404dcb993bfa9af4f421a2afabbfff3492e99db0c969af. Verify that digest before
reading exact JSON members in memory. This establishes serviced-host output drift, not a proven
compiler, linker or SDK internal cause. Do not suppress the package refusal or overwrite candidate.8.

The existing producer-only branch flow proposes candidate.9 and exports only an unpublished
component after the original compiler/two-fresh/source/diagnostic/schema gates. Creating the branch
at the existing commit yields zero runs; its consumed one-shot check and schedule are closed.
Permanent native producer documentation commit9823a734 supplies the actual changed native path.
The prospective inventory is independently derived from the original matching manifests and
actual Pack-Windows template, first proving that the retained candidate.8 nuspec hash matches.
Adoption still requires equality with every actual candidate.9 archive entry, not that derivation.

Keep candidate.7/baseline and candidate.8/servicing unchanged. Add one separate hosted20261021
inventory/version9. Resolve the reviewed new installation and its exact compiler to that explicit
producer; old reviewed hosts retain their original baseline/servicing choice. Unknown hosts,
unknown hashes and the wrong already-reviewed compiler on the new host refuse selection. The
selector first validates actual executable SHA/file version/Microsoft Authenticode/MSVC/SDK,
then writes only the current job environment. Both hosted Windows lanes select before native use;
the app lane partitions its immutable-package cache by selected variant and all package inventories.
Pack-Windows includes all three existing identities when refusing explicit producer-version reuse.
No one version accepts alternative DLL/manifest entries and future drift still fails closed.

The affected qualification script passes26 cases on the actual existing signed local compiler:
the prior18 plus distinct new immutable identity, current identity-reuse refusal,three exact
producer selections and three wrong/unknown host/compiler refusals. Three real MSBuild evaluations
select versions7/8/9. The actual signed local host selects baseline into an owned environment file;
a relative environment target refuses. These are script/property checks, not new managed tests
or proof of a newly installed local host. Main Android code/APK remain D-471; no successful
financial, phone, Release application or unrelated suite is replayed locally. Source, crypto
profile, SDK pins, permissions, data/schema and Windows commercial deferral are unchanged.

Original failures: [native39852707414](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39852707414)
and [CI39852707648](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39852707648).
Producer archive adoption and ordinary exact-head consumer completion are separate gates below.
External/device/stability/accessibility/iOS and publication acceptance remain open.

### D-478 original producer accepted for consumer qualification

Producer-only [run39857667608](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39857667608)
on9823a734 succeeds: actual compiler, two fresh matching outputs, original schema/key/tamper/portable
compatibility, unpublished candidate.9 export and diagnostic drift refusals all pass. Android and
ordinary binary-consumer/performance jobs are explicitly skipped in this existing producer mode.
Artifact12460073284 is1807918bytes, SHA256
634d81028eba5401ab90ca6f590f2316fb28cbf806a7d9680d7861e5e725768d; validate that digest before reading.
The actual1721561byte nupkg has SHA256
bc5b553c0392c7afc806bf17564255a45a9ee175e11cac7f43fbad48818f1f72. Its exact five unique entries match
the independently derived inventory byte-for-byte, including nuspec1e9395b1,manifestf63f9327 and
nativeb02ca254. Both original new manifests also equal the retained first failed run's manifests.
Actual new host/approved compiler and x64 guarded PE protections match. No review binary is installed
or copied to the consumer feed; normal consumers still rebuild from original reviewed sources.

The prospective inventory is now backed by the original successful archive, without rerunning the
unchanged local selection/property/26-case evidence. The producer documentation commit is brought
to main without rewriting history; this adoption adds the distinct exact inventory and host choice.
Producer success is not ordinary consumer acceptance. Final main CI/CodeQL/native result is pending
at adoption commit creation and must be read on its exact pushed SHA. Do not infer general app,
Windows commercial, Android stability/accessibility, iOS, provider/Store or publication acceptance.

### D-478 final exact-head consumer outcome (recorded with D-479)

The consumed one-shot check for main109743f5b12f8d9c21c8e9a5c7b2a8f7a51e0984 reports all five
workflows completed successfully: [CI39860230484](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39860230484),
[CodeQL39860230560](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39860230560),
[native39860230662](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39860230662),
[identity39860230411](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39860230411) and
[service39860230477](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39860230477).

Actual CI has5097 total cases,5072 succeeded,25 skipped,zero failures; Windows Release and Android
Release/Debug billing sandbox build successfully. iOS is deliberately skipped. Native Windows
job120108738020 selects hosted20261021, produces two fresh matching b02ca254 DLLs and passes every
immutable package entry, original source/schema, diagnostic drift, production-assembly cold
persistence and protected reference/tenfold workload gate. Official stock Android native checks
also pass. This is the actual ordinary consumer result, separate from producer39857667608.

The exact-head monitor ci-windows-producer-selection-d478 was consumed and deleted. Recover the
retained final verification rather than repeating its runs-list request or successful suites.
The observed current CI causes are resolved; no claim of Android general stability/accessibility,
Windows commercial, iOS, provider/Store or release acceptance follows.
