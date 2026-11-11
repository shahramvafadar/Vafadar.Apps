# Signed Android Mono15 servicing inputs (D-1080)

[D1081 private Android startup](mono15-private-android-startup-d1081.md) uses these
signed inputs in an isolated strict complete candidate and one fresh original
startup; product runtime pin and physical Main remain unchanged.

## Concrete prepared input and remaining boundary

The existing project-local official SDK10.0.404 manifest10.0.115 names Android
runtime/AOT pack version10.0.15. Five official NuGet version indexes independently
return HTTP200 and contain10.0.15. Five published nuspecs identify MIT licensing,
Microsoft authorship and the same dotnet/runtime source commit
`8e0f8ed2312954727f8919d7d1c2f78b483b4ee7`.
Primary metadata includes the [ARM64 version index](https://api.nuget.org/v3-flatcontainer/microsoft.netcore.app.runtime.mono.android-arm64/index.json)
and [versioned runtime nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.netcore.app.runtime.mono.android-arm64/10.0.15/microsoft.netcore.app.runtime.mono.android-arm64.nuspec).
A primary web-page viewer was unavailable; no claim relies on that failed view.
Actual HTTP status/retrieval times/bytes/digests and all five raw index/nuspec inputs
remain under `artifacts/d1080-mono15-servicing-admission`.

The exact five signed archives are prepared, not installed or adopted. Product
VafadarSecurityRuntimeVersion stays10.0.14, global.json/default SDK remains unchanged,
and no global workload/runtime installation or removal occurs. The existing
D320 matching runtime/compiler/task override remains the intended integration
boundary. Actual serviced-host/workload graph resolution, private Android builds,
relevant runtime/source correctness and ABI/platform execution must pass before
any product pin or Main adoption. Package existence/static compatibility is not
execution, crash remediation, throughput, key lifecycle or release acceptance.

## Archive and signature admission

Five HTTPS package downloads total140316232bytes. Each has SHA256/SHA512/HTTP200
receipts, bounded512MiB streaming, guarded archive names/no traversal/duplicates/
symlinks, complete ZIP CRC and a signature entry. Version/id/MIT/source-commit
metadata inside every signed archive matches its independently fetched nuspec.
The existing local SDK10.0.404 NuGet verifier completes `--all` with exit0 for all
five, using a public-only explicit config and no trust bypass. Both author and
repository signatures are present:

- Microsoft Corporation author SHA256: `9A1B131BEE0605433056A4EA3815478A8E177961A968C6C0027C1093D1FEB630`.
- NuGet.org Repository by Microsoft SHA256: `1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D`.

| Package10.0.15 | Bytes | Archive SHA256 |
|---|---:|---|
| microsoft.netcore.app.runtime.mono.android-arm64 | 26577915 | 7c2b50333e751d7f26829f0f99907b5b649215979a8ab7f4ca1888827180a459 |
| microsoft.netcore.app.runtime.mono.android-x64 | 26488674 | 4c4d96c93b274d57db24daa80d3743323cca75daee8934dded1d3147414c7e65 |
| microsoft.netcore.app.runtime.aot.win-x64.cross.android-arm64 | 43535691 | dbd4e1dad927eb1d7524964d20f4f2694a6ce0ea2686adc703a0be870cc88101 |
| microsoft.netcore.app.runtime.aot.win-x64.cross.android-x64 | 43600099 | da03bf50a3798c49689ffa553e4b22f18c19796704520a3920d8dd8c78c34245 |
| microsoft.net.runtime.monoaotcompiler.task | 113853 | c419138ea25d0eaa0ab30f7e6a54ecbc5a15dc8b9b1fa0202c965184319caaea |

Static archive inspection admits18public native ELF libraries, ARM64/x64 machine
identities and54PT_LOAD segments with16KiB alignment/congruence. Two selected
Windows-x64 cross-compilers have PE machine0x8664; the selected net10.0 AOT task is
hash-bound. Public license/third-party-notice members are inventoried by name/size/
digest for later distribution review. No downloaded compiler/runtime is launched or installed; static inspection reads
archive members in memory. CRC/static parsing and signature verification do not prove
actual compilation, runtime/BCL behavior, exact final APK composition or real
16KiB hardware support. No original fault record or runtime classifier is rebound.

## Process boundary and corrected first-use side effect

One initial SDK help command was mistakenly launched without the isolated CLI
home. It printed first-use setup and a generic development-certificate installation
message. Public certificate metadata finds no newly dated matching localhost
development certificate in the observed execution window. No private key is read; cleanup imports, removes or trusts no
certificate. The message alone
does not prove creation, and no whole pre-existing certificate-store/PATH equality
is claimed.

Three precisely identified zero-byte10.0.404 first-use/certificate/toolpath sentinel
files are newly created in the user's `.dotnet` directory. Their exact names,
zero lengths and creation times tie them to this help invocation. A first cleanup
guard refuses before deletion because implicit JSON date conversion loses timezone
context. Explicit date strings retain the same UTC ownership interval; only those
three new files are then removed. Terminal observation confirms their absence and
no newly dated matching development certificate. Existing SDK-version sentinels,
credentials, certificates and unrelated configuration are not deliberately changed.

Every subsequent verification uses project-owned CLI home/temp/package/HTTP/plugin
cache paths, telemetry opt-out, explicit public-only NuGet config, disabled global
tool PATH registration and `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`. Fixed values
and actual verifier exit codes are recorded; no owner credential, private key or
secret configuration is read. This explicit process boundary is mandatory for the
remaining local SDK work.

The first package-id helper guard rejects legitimate hyphens before any package
GET; it is corrected to an exact five-id allowlist. A static-inspector syntax
refusal also occurs before execution. Those private preflight failures remain;
no successful signature check is repeated to conceal them. No product test,
financial Save, emulator/phone action, new native classifier, global workload
change, code/runtime/GC edit or frozen-scope action occurs. The signed140MB input
set is intentionally retained for the still-required private qualification; it is
not an adopted Android patch or redundant finished build output.

D1079's single consumed response reports CIin_progress/null and CodeQLsuccess;
no second query follows. Canonical parent States, protected untracked benchmark,
retained5606/Q02, all refused scopes and terminal D607/D752/enrollment/financial
checkpoints remain unchanged. Main32015/fe4ee42e is only the preceding dated D1078
observation. Original native-fault cause/remedy, complete performance/security/
accessibility, iOS/providers/Store/release and the full delivery goal remain open.
