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
