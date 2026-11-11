# Project SDK/runtime servicing and current Main — D-1085

[Empty definition bindings D1086](definition-nullable-bindings-d1086.md) follows
with two independently reproduced application input failures; it preserves the
serviced SDK/runtime configuration and accepted financial checkpoints.

The [November servicing assessment](november-servicing-applicability-d1070.md)
now has an applied repository SDK/runtime update. Prepared signed inputs and
[actual x64 codec](mono15-original-codec-contracts-d1083.md),
[physical ARM64 codec](mono15-original-codec-arm64-d1084.md) and
[minimum/ps16k startup](mono15-minimum-ps16k-startup-d1082.md) qualification precede
this source change. Original financial behavior, schema/model, migrations,
constructor/source guards, startup profile and GC options do not change.

## Applied source and actual SDK selection

| Source setting | Before | After |
| --- | --- | --- |
| global.json SDK minimum |10.0.401|10.0.404|
| Central VafadarSecurityRuntimeVersion |10.0.14|10.0.15|

Existing latestFeature roll-forward, no-prerelease and Microsoft.Testing.Platform
selection remain. `sdk.paths` first considers the already qualified project-owned
`artifacts/d1071-local-serviced-sdk/sdk`, then `$host$`. The
[official SDK search-path contract](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json#paths)
supports repository-relative SDK roots and host fallback without global installation.
The prepared local SDK is now an active build input, not disposable trial output.

Actual invocation through `C:/Program Files/dotnet/dotnet.exe` reports SDK10.0.404;
evaluated MSBuildSDKsPath points at the prepared SDK and its actual managed host is
10.0.15. A separate controlled missing-optional-path case selects the qualified
10.0.404 host fallback. This does not simulate a GitHub runner or install a global
SDK. SDK paths affect SDK commands; they do not patch unrelated apphosts, direct-DLL
executions or the machine's older global runtime installations.

An additional ordinary-caller evaluation without workload-root environment
overrides selects404but leaves Android/MAUI SDK properties unavailable. Therefore
`eng/scripts/Invoke-ProjectDotNet.ps1` scopes the repository working directory,
project-owned CLI/temp/package state and existing installed workload discovery to
one SDK invocation. It forwards argument boundaries and restores every original
environment value/absence and caller location in finally; it does not install or
copy an SDK/workload. `Build-AndroidApk.ps1` now forwards its unchanged restore/
publish arguments through this entry point. For portable SDK commands, use:

```powershell
./eng/scripts/Invoke-ProjectDotNet.ps1 build src/Apps/Zanance/Vafadar.Zanance.App -f net10.0-android -p:ContinuousIntegrationBuild=true
```

Actual entry-point evaluation starts without DOTNET_ROOT or workload overrides,
from a different directory containing its own global.json. It selects404/host15/
Android36.1.69/MAUI10.0.110, preserves spaced/empty arguments, and exactly restores
the caller environment and location. A deliberate nonexistent MSBuild target
returns its expected exit1while preserving explicitly empty roots and the caller's
native-error preference. These are launcher controls, not failed product tests or
a repeated full build/financial suite. Initial multiple-command/link discovery and
absent-versus-empty restoration failures remain recorded; only the caller helper
changes. Both entry-point scripts pass syntax parsing. Installed-SDK/IDE setup is
not silently upgraded or claimed qualified by this portable CLI route.

Existing CI setup steps consume global.json, so their requested SDK baseline
updates without workflow edits. Actual CI results still require their own evidence.
Tracked service/container SDK404/ASP.NET15 sources and the
[already adopted retained runtime15 service](retained-runtime15-adoption-d1073.md)
are unchanged; no successful service/identity/container cohort is replayed.

## Strict builds and relevant changed-runtime verification

All new CLI homes, temporary/cache/output/proof roots are project-owned under
`artifacts/d1085-mono15-project-servicing`, with separate Windows/Android/host
intermediates. The active D1081 servicing package cache is reused; preexisting
unrelated packages are read-only fallbacks. Certificate generation, global-tool
PATH registration and telemetry remain disabled in the process environment.

Strict Windows build and complete offline Android publish both exit0 with0warnings/
0errors using the new default source pin, without a command-line runtime override.
Windows self-contained runtimeconfig includes Microsoft.NETCore.App10.0.15;
its CoreCLR and CoreLib files exactly equal the qualified official SDK15 runtime
payload. Windows product execution/delivery remains separately deferred; this is
not a claim that older system-wide installations were removed or patched.

Only the two existing pure HomeLedgerBlockCodecTests/HomeLedgerBlockBufferTests
classes run:26+13=39passed,0failed/0skipped. Result XML independently confirms
both exact class counts and actual64bit.NET10.0.15. These relevant changed-runtime
contracts cover memory transport/buffer behavior; no store/financial test class,
successful financial write, genuine authentication, enrollment or photo is replayed.
There are no new permanent tests or all-suite claims.

Twelve actual Windows/Android PDB document checks match App.xaml.cs, original
NativeAccessLifecycle/PersonalSyncLifetime, codec/buffer and Core LedgerEntry.
The original tracked Android startup.aprof bytes/hash remain exact; no profile is
retuned or recorded again. This is finite source/profile binding, not whole-PE
identity or qualification of every framework/optional edition.

Complete signed Android APK99643295bytes/SHA256
`78e21c2b43e221e460a163220fb013e3a2ef6218dfedbf39692ccf2cf362312d`
passes384AOT/408ELF/1608static16KiBLOAD, ZIPCRC/alignment, v2/v3 signature,
offline and nondebuggable checks. Both packaged Mono hashes equal the qualified
signed15inputs and actual original targets select matching15AOT compiler/task.
The candidate and the hash-matched existing Main rollback APK have the same public
signing certificate; no private key/password is inspected.

## One actual no-reset Main adoption

Current read-only Samsung SM-A176B/RZGL105RSDL admission confirms unlocked Main32015,
original boot`6bd9dccf-976f-4a2c-afa0-bfceba0af22e`, Home141owned/total nodes,
two selected Home nodes and zero editors/loading. One install-r replaces the
complete precedingfe4ee42e package, without reset, force-stop or data removal.
Only the intended Main package hash changes in captured public state. UID10380,
CE155299/DE154603, grants/flags, all other captured Zanance packages, five OS
values/key presence, locale, geometry, boot and reverse remain exact.

One normal launcher entry reaches Main11867. The sole settled final frame in that
same new process has141owned/total nodes, selected Home and no editor/loading.
Raw XML bytes and every ordered native attribute map equal the baseline without
normalization or another capture selected for a pass. Two remote XMLs/103864bytes
are individually hash-bound before removal; local originals remain. No explicit
financial/import/Undo/authentication/permission/photo operation, private Main
provider/database/key read or accepted-operation replay occurs. Ordinary original
startup/foreground behavior is not inspection or proof of the entire private source.

Main11867/78e21c2b is this dated current adoption observation, not ongoing liveness.
ADB5037/PID59860/creationUTCticks639293902901445383 remains observed. No server
reset/reconnect/pair, another device transport, retained emulator or reboot is used.

## Cleanup and exact refused output boundary

Four standard Cleans exit0, retiring15261files/1315026463logical bytes. Six actual
PDBs, current installed signed APK, old hash-bound rollback and small original
evidence remain. Global SDK/runtime installations and the active project SDK/cache
are intentionally retained; no unrelated resources are removed.

One supplemental removal of five newly completed output targets is rejected by
automatic review before process creation, with stated reason `blocked by policy`.
Zero files are removed by that command. The last measured2734files/559359100logical
bytes include2733residual build files/459878038bytes and the99481062byte unsigned
publish APK. These exact targets are now frozen; never inspect/retry/subset/
alternate/move/escalate/reuse them:

- `artifacts/d1085-mono15-project-servicing/windows-build`
- `artifacts/d1085-mono15-project-servicing/android-build`
- `artifacts/d1085-mono15-project-servicing/host-build`
- `artifacts/d1085-mono15-project-servicing/compiler-proof-build`
- `artifacts/d1085-mono15-project-servicing/android-publish/pro.vafadar.zanance.apk`

The refusal does not include the sibling signed delivery APK, preserved PDBs,
source, result records, active D1071 SDK or D1081 package cache. No later action
against any refused target occurs. Earlier frozen scopes, protected untracked
benchmark and retained5606/Q02/original fault artifacts remain untouched.

The consumed D1083 response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43091791287)
completed/cancelled and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43091791262)
completed/success. The sole D1084 response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43094169176) and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43094169032)
in_progress/no conclusion. Raw responses/exclusive markers are retained; no second
request, successful-job log or rerun is used, and development does not wait for CI.

The exclusive D1085 single response subsequently reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43100469915)
in_progress/no conclusion;
[Local identity backend](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43100469922),
[Self-hosted service qualification](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43100470027),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43100469893) and
[SDK update workflow](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43100662347)
are completed/success. The consumed marker, exact response and receipt remain;
none is queried again.

Terminal D607/D752/enrollment/financial checkpoints and canonical parent States
remain preserved. Operational repository/app servicing is applied, while separate
global-machine maintenance, full framework/edition/security/platform acceptance,
original SGen fault cause/remedy, complete performance/accessibility, iOS/providers/
Store/release and the full delivery goal remain open. The original14fault reader is
not rebound to the new runtime, and this security patch is not claimed as its fix.
