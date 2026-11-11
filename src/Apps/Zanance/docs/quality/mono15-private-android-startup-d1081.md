# Private serviced Android graph and original startup — D-1081

The [signed D1080 inputs](mono15-signed-servicing-inputs-d1080.md) now form one
actual strict, complete offline Android candidate. Central
`VafadarSecurityRuntimeVersion` remains10.0.14. Only this private command graph
overrides it to10.0.15; product source, installed workloads, global SDK and Main
installation do not change.

## Actual isolated build

Project-owned SDK10.0.404 executes on host10.0.15. Process-only
`DOTNETSDK_WORKLOAD_PACK_ROOTS` and `DOTNETSDK_WORKLOAD_MANIFEST_ROOTS` select
already installed read-only global workload inputs. Actual resolver evaluation
reports Android36.1.69 and MAUI10.0.110; central MAUI packages stay10.0.120.
No manifest is edited and no workload is installed. The earlier SDK source-path
HTTP404 remains; actual local evaluation supports the resolver observation.

CLI home, temporary files, new NuGet packages/caches, intermediate and publish
output stay under `artifacts/d1081-serviced-android-graph`. Existing unrelated
NuGet packages are a read-only fallback. All five restored archives match SHA256
and NuGetSHA512 of the already signature-qualified D1080 inputs, without repeating
signature checks. Original D320 targets select matching10.0.15 AOT compilers/task
for ARM64 and x64. Original notification/native dependency preparation is retained.

Initial full restore succeeds. First publish fails because the deliberately short
private PATH omits Python required by the original notification preparer. That
failure/receipt remains. Adding the existing Python directory only to that process
environment permits the unfinished publish to continue with the same restore and
output graph; successful restore is not repeated. Final strict publish exits0 with
no warnings. No global first-use/certificate action is requested by these commands.

## Complete candidate and one original native startup

Signed candidate99643295bytes/SHA256
`6118f4bf2dcf6b68208559f521d1db6c0e9fc57a5f40438975b4d2f1bf7dd332`
passes384AOT/408ELF/1608static16KiBLOAD, ZIPCRC/alignment, v2/v3 signature,
offline and nondebuggable checks. Both packaged Mono libraries equal the
restored signed10.0.15 runtime-pack files:

| ABI | `libmonosgen-2.0.so` SHA256 |
| --- | --- |
| ARM64 | `704f433de14a69f67e1eb098b4e2cdb1e35984741f8eeeb0975d6f8d2d6789a3` |
| x64 | `b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034` |

The reused verifier's local `final-shipping` filename/stage describes archive
checks only; it is not evidence or permission for Main adoption or release.

One fresh owned API36/x64/4096 emulator, actual320x640dp/density160/font1, admits
the previously absent original package. Exactly one installation and normal cold
launcher entry use the hash-matched APK. Original startup reaches PID4331/UID10216,
boot`e27f1ae3-d184-4a59-b717-8ff4ced95920`. The sole original XML has42owned nodes,
including actual `Welcome` and `I already have a backup` captions. The same PID
and five OS values, locale/key presence, geometry, boot and reverse state remain
exact. No app control is tapped; no account/ledger/plan/authentication/restore/photo
or financial preparation/command occurs. Normal startup may initialize private
storage; this cohort does not inspect or compare that storage.

Android's2342ms launch value describes this single startup, not whole-Home speed
or a comparison against the preceding runtime. One alive process/ready page does
not establish native-fault absence/remedy, all behavior, private source/key equality
or sustained stability. No successful financial or unrelated suite is replayed.

## Retirement and remaining qualification

The15215byte remote XML is hash-bound to its retained local original before removal.
Only the fresh package retires with exact captured OS state. Owned emulator stops;
official AVD deletion retires24files/4316869979logical bytes, with no AVD files
remaining. Three actual App/Core/Data PDBs are preserved before one isolated SDK
clean: exit0,13705files/800690185logical bytes removed,2181files/341323228bytes
remaining in the graph. Used signed APK, raw observations, verifier inputs and
working package cache remain for unfinished servicing qualification. There is no
supplemental deletion, policy refusal or attempt against frozen scopes.

Original ADB5037/PID59860/creationUTCticks639293902901445383 remains observed.
No physical-phone command occurs. The earlier owner-unlock reply corresponds to
the completed [D1041 lifecycle adoption](cumulative-lifecycle-main-adoption-d1041.md);
that successful installation/tests are not repeated. Main32015/fe4ee42e remains
only the dated [D1078 observation](current-authenticated-transport-main-d1078.md),
not fresh liveness. Protected untracked benchmark, retained5606/Q02, refused
scopes, terminal D607/D752/enrollment/financial checkpoints and canonical parent
States remain untouched.

The single consumed D1080 response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43083352744)
in_progress/no conclusion and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43083352898)
completed/success. Raw response/exclusive marker remain; no second query,
successful-job log or rerun follows. Work does not wait for CI.

Actual minimum-API/16KiB/ARM64 qualification, meaningful changed-runtime component
correctness and justified product pin/adoption remain. Original native-fault
cause/remedy, whole performance/security/accessibility, iOS/providers/Store/release
and complete delivery goal remain open.
