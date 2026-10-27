# Notification source admission under interpreter optimization (D-581)

## Reproduced defect

D-580's native broadcast correction and signed product remain verified. Its build
preparation used Python `assert` for input checks. Python removes those checks under
`-O` or `PYTHONOPTIMIZE`, so the build could admit a changed reviewed replacement or
changed retained source when the interpreter was optimized. This is a build-input
admission defect, not evidence that a previous accepted APK used changed inputs.

Two new regressions execute the actual preparation entry point in an optimized
subprocess against fresh disposable owned fixtures. A replacement with extra C# text
and a changed cached source each returned success on the original implementation;
both new tests failed before the correction. Original failure logs are retained.
There is no network request, device operation, financial source or account in either
fixture; the original archive is copied from the already verified owned cache.

## Correction and focused verification

All production source-admission assertions become explicit `require` checks raising
ValueError. Manifest version, license, bounded archive size/digest, reviewed replacement,
relative paths, every original file, retained cache bytes and extra compilation inputs
remain mandatory even with optimization. A refused input is preserved; the helper never
silently overwrites it or relaxes a digest. The PowerShell wrapper retains the same
exclusive cache lock and fails on a nonzero preparation exit.

The final selected run passes seven relevant tests: five existing admission cases
affected by the new explicit rejection, plus the two new optimized regressions.
The new managed/application test count is unchanged; these are two new build-source
tests, not seven new application cases. Existing financial/native suites are retained.

One strict source-Core build runs the real MSBuild/PowerShell preparation with
PYTHONOPTIMIZE=1 and succeeds with zero warnings/errors. The build uses an independent
owned output root. Every cached SDK C# byte still matches the exact D-580 manifest;
the used full signed APK still has SHA256
f7488d1da0bbed5b9910a7ace960ef5f883da008f31dd24e173e9421996ae9c7.
No production C#, SDK revision, replacement digest, payload, permissions or runtime
changes. Repeating the successful full Windows/Android package, native queue comparison
or no-reset phone installation would add no evidence for this build-only correction;
those retain their strict D-580 results. No new shipping APK is claimed.

## Boundaries

The physical phone remains in Dev Loyan1's exclusive turn until explicit return.
No phone/emulator command, installation, financial command, seed, clock or system-setting
change occurs. Disposable test fixtures are cleaned after their refusal assertions;
local logs and the original SDK/product evidence are retained. This does not close
general punctuality/Doze, stability, accessibility, hardware16KiB, iOS, identity,
provider, Store, public-hosting or release gates. The overall goal stays active.

Standard Clean removes all79 registered files/1,283,536 logical bytes in the exact
new Core output root after path-ownership verification. The live pinned source
cache, used signed APK and local evidence remain. No host free-space gain is claimed.
