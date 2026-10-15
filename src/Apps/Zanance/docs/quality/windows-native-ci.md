# D-279 repeatable hosted Windows native qualification

## Gap and delivered path

Native cipher candidates previously had only the Linux job: two Android slices and the official
SQLCipher suite. The general Windows application build qualifies the compiler and restores exact
native package entries, but may use its reviewed candidate cache and does not execute the separate
actual-production-assembly create/cold consumer. Those scopes do not prove two fresh Windows outputs
or current reference/tenfold protected workloads on a hosted runner.

The dedicated Windows lane uses windows-2025-vs2026 and the original shared toolchain qualification:
approved installer metadata, pinned MSVC/SDK and actual compiler bytes. It performs no compiler-policy
relaxation, package hash update, global configuration change, app registration or new native suppression.
The current official image inventory is
https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-VS2026-Readme.md;
that inventory is orientation, not proof that actual runner compiler bytes will qualify.

1. Build two fresh independent native trees without a candidate cache; compare actual DLL SHA-256.
2. Run the existing original schema/key/tamper compatibility and exact immutable package-entry check
   through Ensure-WindowsPackage, tied to the second fresh candidate.
3. Restore and build the actual production-assembly host, check native/notices/provenance bytes and
   run create plus cold reopen in distinct processes with the same retained fixture. Cold opening
   cannot prepare replacement keys. The original 32 migrations, negative money, portable source and
   wrong-key refusal remain executable assertions.
4. Strictly build and run the independent protected reference/tenfold executable. Failed builds do
   not invoke stale output. No arbitrary latency threshold or two-second experience claim is made.
5. Always retain the exact public logs/manifests/proofs/measurement JSON for 14 days. No recursive
   fixture-root upload: databases, DPAPI envelopes, recovery material and native key files stay private.

The workflow has read-only repository permissions and the existing per-ref cancellation policy;
Windows is bounded to 60 minutes. Source and pinned portable build dependencies are restored by the
existing build recipe. No new package/tool/action is introduced. The benchmark/fixture paths now
trigger their qualification; unchanged app/UI tests and APK are not rerun locally for orchestration.

## Verification and honest limits

The actual YAML was parsed with the existing portable Perl YAML::Tiny. All five Windows PowerShell
run blocks parse with the PowerShell AST parser. The Python environments lacked a YAML module; no
download was made. Perl emitted a locale fallback warning; it parsed successfully and no global locale
was changed. Source inspection confirms the original script failure guards, no candidate cache and
the explicit log/proof upload allowlist. These checks qualify syntax/orchestration, not hosted success.

The successful D-278 actual local protected workloads and D-276 production binary proof remain local
behavior evidence; no duplicate producer/upstream/app/UI/APK runs are claimed here. Application
coverage remains 3,574 behaviors (3,573 full D-276 plus one focused D-277). Native upstream warnings
remain recorded/unapproved. Windows OpenSSL's full suite requires an unprivileged account; this job
does not bypass that guard or claim that suite ran. The existing Linux official SQLCipher suite is
a separate job, not Windows OpenSSL coverage. Physical/iOS, vulnerability assessment, all crash
points, main UI and release acceptance remain independent.

After push, inspect the final commit's hosted workflow once after the repository's normal delay.
Pending, cancellation, compiler/package mismatch and execution failure are not success. Preserve
the actual failed job/log before changing anything; never accept runner drift by deleting the
compiler or immutable-package checks. The full continuous-development goal stays open.

## D-282 CI platform contract and native provenance diagnosis

CI run 38698595330 on D-281 failed independently in Linux RSA exception assertions, Android
native packaging and Windows compiler qualification. Accept CryptographicException subtypes only
at native RSA refusal assertions; retain exact managed validation and fail-closed checks. Pin native
input-lock checkout endings to their reviewed CRLF bytes on every host: the prior platform-default
checkout changed both fingerprints under Linux, despite identical JSON and native source revisions.
Immutable package entries and observed ABI/runtime qualification remain required.

The Windows host compiler hash differs from the reviewed executable. Preserve refusal and report
only public SHA-256, file version and Authenticode provenance without executing unreviewed bytes.
Twenty focused recovery cases and all eleven existing toolchain cases pass locally. Hosted Windows
compiler review and final successful CI are still pending; this is diagnosis, not acceptance. No
application/UI/full-suite/APK repeat or production encryption/security/release claim is made.

### Reviewed Windows servicing producer and immutable package

Diagnostic run 38704482159 job 116320370636 observed compiler 19.51.36260.0, SHA-256
b0a1818ebae6da7f18230a84e76ce839212584ac4c2ab55b97a0d5268085da57, with valid Microsoft
Corporation Authenticode signature. Local reviewed 19.51.36257.0 remains pinned separately.
Policy schema 2 requires an exact hash/file-version pair AND valid Microsoft signature, plus the
existing installer/MSVC/SDK restrictions; there is no version wildcard or signature-only fallback.
Microsoft documents compiler servicing separately from folder/toolset versions:
https://learn.microsoft.com/en-us/cpp/overview/compiler-versions.

Windows package candidate.5 has a stable recipe manifest naming both exact reviewed producers.
The unchanged raw candidate manifest and compiler-host proof retain the actual observed producer;
neither is rewritten as another compiler. Every packaged entry, including the original native DLL
6455496953d402c7764693c2c2db60179ac9baf4ee3c5e6f5d6a405fdd17ec21, remains independently pinned.
Different native output is still refused. Two fresh hosted outputs and the production-assembly
create/cold/workload lane remain required before calling the servicing producer qualified.

A related local consumer failure exposed PreserveNewest retaining old provenance because deterministic
NuGet entries have fixed historical timestamps. Notices/provenance now copy Always. Candidate.4 and its
failed consumer evidence are retained locally; candidate.5 is a new immutable version, never an overwrite.
Thirteen toolchain cases pass. The actual production-assembly consumer builds with zero warnings/errors,
creates with 32 migrations and reopens the same encrypted fixture in another process with exact money,
portable-source and wrong-key checks. Only this changed consumer was repeated after the copy correction.
Hosted completion remains pending; app/UI/APK and unrelated successful tests were not rerun locally.

### Distinct immutable servicing output

Run 38705256020 produced two identical fresh serviced DLLs:
ac0939dbe00bec809fcfc4c77587b509b2798002456bb4c54ed3ca579a12f288. Original-schema,
32-migration, Unicode/money/date, foreign-key, wrong/missing-key/tamper, memory export and complete
staging checks passed before the old immutable package correctly refused the changed manifest.
Its public manifest digest c58665de95ee66aa9907051e273651513e42f01d41048de484cb982872a9a5c8
exactly matches the prior stable recipe manifest with only the observed native SHA-256 replaced;
size is still 5,641,216 bytes. This binds the full metadata to the hosted log without importing a binary.

Keep candidate.5 bound exclusively to the original compiler/DLL. Introduce candidate.6 exclusively for
the exact serviced compiler/DLL, with every entry pinned separately in windows.servicing.package.contents.json.
VafadarWindowsCipherVariant=servicing selects that explicit producer in both hosted Windows jobs and
central NuGet versions; default local baseline remains candidate.5. Unknown variants and cross-producer
packing fail closed. No one package version accepts alternative bytes and no native binary is downloaded.
The 15 toolchain/variant cases and real mismatched-producer refusal are focused checks; full hosted
consumer/workload/main-Windows-build completion remains pending at this commit. Already successful Linux
suite (3,566 passed / 25 skipped / zero failed), Android Release, stock cipher, CodeQL and local backend
on dfae08a2 are retained evidence, not replaced by new local repeats.

### Isolated PostgreSQL TCP readiness

Run 38707538871 failed before service publishing: pg_isready on the default Unix socket observed
"accepting connections" followed immediately by "rejecting connections". The pinned official image
uses a temporary bootstrap server with listen_addresses='' before starting the final server. CI now
waits on 127.0.0.1 TCP for the explicit database, then creates the fixture runtime role over that same
transport. No arbitrary extra delay or blind job retry is used. An isolated, network-none container
from the already present exact image digest verified the entrypoint behavior, final TCP readiness and
actual CREATE ROLE; only that explicitly labeled fixture was removed. Service/app code and prior
successful identity/restore scenarios were not rerun locally. Final hosted completion is reported by
GitHub after this commit; these entries record evidence available at each delivery push.
