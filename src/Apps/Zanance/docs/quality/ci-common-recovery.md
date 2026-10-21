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
are metadata-only checks. Actual hosted compiler bytes and latest-commit recovery remain pending.
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

Pending the new commit's actual workflow results. Do not call hosted CI fixed from local tests,
metadata inventory or a previously successful job. Preserve exact hashes and package identity if the
host supplies different bytes; qualify those separately rather than loosening the policy. Re-run only
failed work when its code/configuration changes. Current app/APK and runtime acceptance stay D-432.

Account monetary inputs remain the next complete feature batch (D-434), followed separately by holding
money contexts. Dedicated ANR/crash and Android accessibility work retain the owner's deferred order.
