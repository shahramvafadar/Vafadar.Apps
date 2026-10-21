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
are metadata-only checks. Hosted bd6e9484 CI and native Windows recovery pass; Android native host recovery remains pending.
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
