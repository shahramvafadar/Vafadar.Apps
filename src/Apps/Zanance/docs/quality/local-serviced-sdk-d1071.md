# Project-local serviced SDK preparation - D-1071

## Completed independent prerequisite

The exact official Windows x64 SDK10.0.404 is prepared and executed in project-owned
storage for subsequent serviced host/API work. It is not a global installation,
workload installation, product-runtime switch or application acceptance. The
[November applicability review](november-servicing-applicability-d1070.md) identified
this new servicing requirement; unchanged product/financial/authentication tests
are not rerun to prepare a toolchain.

The [official release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.15/10.0.15.md)
identify SDK10.0.404 with runtime10.0.15. The already saved primary release index
binds the Windows x64 ZIP to SHA512
`330ddd2665b43cd2e428216a0d8ea1ac7bb19b5434fdc530e62010dc3b52823a620bf95489d628e1ed6f5122401793ee4887cca4a019122361a1231497f632ee`.
One official HTTPS download contains303618098bytes and matches that complete hash.
ZIP CRC and all extraction paths are checked before extraction; absolute,
drive-qualified, escaping and symbolic-link entries are refused. The original
index receipt is unchanged. The supplied license permits development/test use;
license and third-party notices are reviewed and retained without modification.
No account, installer, elevation or global environment change is involved.

## Actual selected components

Using only the explicit owned dotnet executable from the repository root selects
SDK10.0.404 under the unchanged global.json rollForward rule. Its --info base path
and --list-runtimes bind NETCore/AspNetCore/WindowsDesktop10.0.15 to that same local
root. The bundled VSTest dependency graph contains the fixed
18.7.1-hotfix-26529-119 TestPlatform components specified by the
[official SDK advisory](https://github.com/dotnet/announcements/issues/517).
The actual bundled CLI --help returns successfully and reports18.7.1; no test
assembly, forged execution artifact or untrusted PDB is executed.

One new isolated framework-only console probe compiles using the actual new SDK
with ContinuousIntegrationBuild/TreatWarningsAsErrors, zero warnings/errors and
no external package sources. It excludes inherited repository Directory.Build
props/targets and shared compiler use. One actual invocation reports.NET10.0.15,
X64 and both CoreLib/ASP.NET HttpContext assembly locations inside the local root.
This is a six-command toolchain qualification, not six product tests, full-suite
acceptance, application compatibility, a vulnerability exploit or a native fix.

The new runtime's native DiaSymReader file has PE product/file version14.50.35740.0;
the SDK's managed dotnet-format/watch DiaSymReader wrappers have2.2.15. These PE
version namespaces are not the advisory's NuGet18.9.1 version namespace; a numeric
comparison between them would be invalid. Exact file SHA256/length and official
checksum-bound SDK/runtime provenance are retained. No claim that a malformed-PDB
regression was executed follows from provenance or a version resource.

Every child process uses local DOTNET_ROOT/CLI_HOME/TEMP/TMP/NuGet storage, telemetry
and workload-update notification opt-outs, no global-tools PATH addition and a
short process-only PATH. No installed workload is copied, modified or registered.
A final ordinary host invocation still selects10.0.401 and lists the same global
NETCore8.0.21/10.0.11/12, AspNetCore10.0.11/12 and WindowsDesktop10.0.11/12 as before.
Those global installations remain outstanding maintenance boundaries; preparing
the local copy does not patch or remove them.

## Retention and restoration

The reusable local executable is
`D:/_Projects/Vafadar.Apps/artifacts/d1071-local-serviced-sdk/sdk/dotnet.exe`.
The verified SDK retains5578files/812144112logical bytes for subsequent approved
host/API servicing. It is deliberately retained, not an abandoned build output.
The original license/notices, component hashes, qualification logs, original probe
sources and hash-matched used probe outputs remain with the bounded evidence.
The successfully executed probe binaries are not run again after preservation.

One standard Clean of only that new probe succeeds and removes234217logical bytes.
The exact redundant verified ZIP is then removed nonrecursively after a second
SHA512/size/path check:303618098logical bytes. These are logical file totals, not
measured physical space savings. No recursive supplemental cleanup, existing
artifact root or previously refused/frozen scope is attempted. The SDK remains
needed; no unrelated global package/cache/runtime is deleted.

## Remaining boundary

Default global selection and all tracked SDK/runtime/package pins remain unchanged.
Subsequent host/API qualification must explicitly use the serviced executable and
prove the selected runtime for the actual process. Merely placing this folder
beside the repository does not change existing tools, running services or images.
Android workload availability/Mono runtime/compiler/task matching and qualified
profiles remain their own requirements; this host preparation does not authorize
a silent Android toolchain fallback or claim the original SGen fault is fixed.
The current Linux SDK10.0.403/ASP.NET10.0.14 container pins and actual fresh-image
qualification still require their separate completed servicing step.

No product source/schema/palette/translation/test-count change, host product suite,
financial/authentication/enrollment/photo replay, phone/emulator/ADB input,
service/container mutation or original-fault interpretation occurs. Existing Main
is only its last dated D1068 observation, not a fresh liveness claim. Protected
Brotli source, retained5606/Q02, all frozen scopes, canonical parent States and
terminal D607/D752/financial checkpoints are preserved. Original fault cause/remedy,
whole-performance/security/accessibility, genuine providers, iOS/Store/release and
the full delivery goal remain open. One post-push CI read must not delay this work.

[Service runtime15 D1072](service-runtime15-fresh-qualification-d1072.md) updates
immutable image sources and qualifies one fresh original deployment plus exact
public-software rebase; retained deployment adoption remains separate.
