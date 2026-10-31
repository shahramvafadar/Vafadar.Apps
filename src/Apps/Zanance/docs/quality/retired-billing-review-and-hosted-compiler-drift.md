# Retired billing review and current hosted compiler drift

## D-681 actual CI evidence

The original [CI run41180020523](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41180020523)
on93173535 fails Libraries/tests and Windows; Android succeeds. The stored original
job logs are inspected without repeating the consumed exact-head workflow query or
rerunning its successful jobs. Seven selected tests reproduce locally before repair:
six language cases and one retired native-confirmation case. The local host is actual
.NET10.0.12; this evidence is not relabelled as runtime14 or Linux acceptance.

## Distinct test-contract and production defects

The six-language replacement-terms test changes its private store presentation between
observations but calls appearance `LoadAsync` again. D-663 deliberately coalesces an
already-visible frame; explicit original Reload is required for changed source facts.
The test now uses that actual command, preserving every timing/date/price, Lifetime,
changed-offer, native Cancel and full financial snapshot assertion. Production is not
changed to implicitly retry or reread on appearance.

The separate actual production defect is retained review ownership: `DeactivateBilling`
retires the original frame but leaves its pending native-review flag set. A new actual
frame cannot initialize its billing presentation until the old dialog returns. Frame
retirement now releases that old review reservation; both Buy and Restore completion
paths clear review feedback only when their original billing epoch is still current.
All original source, disclosure, native-consent and writer predicates remain in place.

The32 distinct cases in the two affected test classes pass. Two additional focused
cases prove that either acceptance or rejection of an old dialog cannot clear the
new frame's still-pending review; the new review's Cancel restores readiness, with
zero restoration/provider calls and the original financial snapshot unchanged.
These are34 distinct affected cases, not a repeated whole-suite baseline.
The actual original page/native dialog is also checked in PID19111/UID10216. One visible
Restore opens the original native confirmation. The precise failed lifecycle is invoked
privately on that same actual view model while its original dialog remains pending:
the new frame completes its second original source read and becomes ready. Accepting
the same old native dialog then produces zero restoration calls; readiness and the full
21-source snapshot remain intact. Original visible Back returns normally. This is not
acceptance of a user-navigation route during a modal dialog or of an actual store,
payment, provider restoration or physical phone. No successful dialog is repeated.
The necessary independent192AOT/204ELF/804 static LOAD caller has SHA256
`f76636f539c11e039932f3a0137bd7965d90253aed654aed6efbf84a82474460`.
Its first private compilation refuses a missing fully qualified localization type;
that helper-only failure is preserved before the corrected strict build succeeds.
The final shipping/Windows builds are strict and warning-free. The used native source
and APK remain archived; the shared fresh UID/AVD, exact OS restoration and bounded
cleanup complete with D-680. No phone or protected D-607 command occurs.

## Separate hosted Windows gate

Windows stops before native preparation/application compilation because the installed
Visual Studio version18.11.12328.437 is outside the reviewed installer list. The
[official exact runner inventory](https://github.com/actions/runner-images/blob/win25-vs2026/20261030.294/images/windows/Windows2025-VS2026-Readme.md)
identifies that serviced host. It does not prove the compiler executable hash, native
output identity or compatibility with an existing immutable package version.

A read-only public compiler observer now runs before the unchanged qualification in
both affected workflows. It exports only the installer/tools/SDK versions, compiler
presence/hash/file version and Authenticode status/public signer. It never executes
the compiler, prints private paths/environment/credentials or grants admission.
The current local compiler observation is validly Microsoft-signed and matches the
existing baseline;26 existing toolchain refusal/identity cases pass.

The hosted compiler is still unqualified. Preserve its next actual observation, then
review exact executable bytes and, if necessary, two fresh outputs under a new immutable
producer identity. Do not add an unobserved hash, weaken policy, overwrite a published
package or infer hosted equality from the local compiler. Windows CI is not yet green.
The delivery goal and D-607 remain active and untouched.
