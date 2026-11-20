# Reject unusable APK handoffs (D-729)

## Actual original entry-point failures

The original Build-AndroidApk.ps1 trusts publish exit0 and then takes the newest matching file.
Exercise that actual script with an isolated build-command fixture, not a real SDK build. Each
case executes its original restore/publish command boundary once, returning0 while using a new
public file fixture. All three original failures are recorded: missing signed APK still prints
APK with an empty path/0MB and installation advice; an empty signed file receives the same advice;
two distinct matching signed filenames are arbitrarily reduced to one. No actual build, signing,
secret read, device command or financial operation occurs. Preserve those three failing results
and their tiny local failure inputs; they are explicitly not Android packages.

## Narrow output admission

After the existing warning/error guard, require exactly one matching **file**, then require its
length above0 before any handoff output. Missing/empty/ambiguous output throws with a concrete
error; mixed applications need separate output directories. Do not choose the newest arbitrary
APK, delete existing output, alter signing/edition/restore/publish arguments or touch frozen scopes.

Four focused PowerShell cases exercise the actual entry point: the three regressions now refuse,
and one unique nonempty artifact is selected. That positive file is plainly a tiny non-APK fixture:
this tests filename/count/length admission, not ZIP, embedded assemblies, package identity,
cryptographic signature or current-build provenance. Those existing full handoff gates still apply.
The fixture replaces only restore/publish commands inside its script scope, counts exactly8 calls,
restores working directory/process exit state and removes only its new resolved temporary root.
No toolchain installation or real app build is hidden behind this result.

Register that focused script in the existing Windows CI job before workload installation. There
is no new job, permission, secret or external service. Actual remote status belongs to the later
one-shot exact-head response. No successful unrelated suite, Windows/Android app build, native
scene, financial command or standard Clean without generated build output is repeated. These
are4 build-script checks,0new application unit tests.

## Delivery and protected scope

Product source/APK is unchanged from D-728's full signed0308b242 candidate. Its no-reset Main
adoption still waits for actual D-607 completion/cleanup. No physical command is issued here;
all three original deadlines, consumed reboot and unread post-reboot source stay protected.
The saved timed continuation is stricter than the observer's early code guard: it authorizes no
device command before1November08:05UTC. Read that saved restriction and honor it; do not run at
the observer's earlier five-minute pre-deadline boundary. Continue independent work meanwhile.

This admission guard does not establish installation, whole APK integrity/provenance, general
security/stability/performance/A11Y, physical/iOS/provider/Store/release acceptance. Android
completion precedes iOS; the overall delivery goal remains active.

## APK verification before handoff

The entry point now sends the selected file and original build properties to
`Assert-AndroidApk.ps1` before emitting installation advice. The SDK verifies the
signature and package identity. Release rejects a debuggable APK, and the offline
flag rejects either network permission. A successful handoff reports the verified
package ID and SHA-256 with the full file path.

The existing signed x64 offline Release APK was accepted. Its unsigned counterpart,
a signed fixture with a different package ID, the debuggable fixture with its own
expected ID, and a network-enabled APK presented as offline were refused. The
entry-point fixture confirms that a verifier refusal emits no path or installation
advice. No app was rebuilt or installed for these checks.

## Fresh publish provenance

Each attempt has an empty staging directory, including the automatic retry after a
restore race. Selection and signature verification take place there. A successful
verification promotes the signed APK to the existing handoff path. A successful
publish exit that emits no file leaves a prior handoff intact and reports failure.
The retry likewise cannot hand off a file produced by its failed first attempt.
An existing differently named signed package still requires a separate output
directory. The script retires only its own staging directories.

On 2026-11-20, the real entry point built and verified a strict offline Release
APK from `f8d91857`. The output started with a copy of an older connected APK.
An initial single-ABI attempt injected the runtime identifier into plain library
projects and hit NETSDK1112. It preserved the prior file and retired its staging
directory. The standard project build then replaced only that copied handoff.

The resulting `pro.vafadar.zanance` APK has 100,052,895 bytes and SHA-256
`9f1c9df40e85763054eb3568be2fa2821c55e719cdcd28e744d0a3a7c7ed8bde`.
The entry point verified its signature and offline manifest and reported that same
digest. Each of ARM64 and x86_64 has 192 AOT and 204 ELF files. All native load
segments passed the static 16 KiB check. The temporary publish directory was
removed and a standard isolated build clean passed. The APK and a receipt remain
under `artifacts/zhb`. No device installation was performed.

## Managed payload admission

A derivative of the reviewed dual-ABI offline APK omitted only the ARM64 managed
assembly store from its application payload. It was signed with a new disposable
test key. The original verifier accepted its package, signature and offline
manifest. The verifier now uses the producing SDK's effective assembly packaging
mode. In assembly-store mode it requires exactly one nonempty primary DEX and one
nonempty assembly store for every ABI present in the archive. The SDK-resolved
loose-assembly Debug mode keeps its existing verification path.

The same incomplete fixture now fails specifically on the missing ARM64 store.
The original complete dual-ABI Release APK passes the new checks with its exact
previous digest. No app was rebuilt or installed. This checks required ZIP
members, without validating every assembly inside each store. The before and
after receipts and the signed negative fixture remain in `artifacts/zhc`.
