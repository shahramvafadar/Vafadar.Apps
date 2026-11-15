# Per-runtime untrimmed Android Core verification (D1198)

## Observed current CI failure

The sole D1196 status response reports [CI failure](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43694440019)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43694439998).
One subsequent job-metadata response identifies the Android Debug billing-sandbox
build as failed; library/tests and Windows jobs succeeded, and iOS was skipped.
Only that failed Android job log was retrieved. Successful job logs, status
polling and reruns were not requested.

The full Release build and private package preparation completed before the
Debug command failed at `VafadarVerifyPrivateCoreUntrimmed`: the old guard
required one Core assembly globally. Android's untrimmed `ProcessAssemblies`
contract deliberately retains one item per RID, including identical source paths.
The actual local Debug input contains the same prepared version5 Core twice,
with `android-arm64` and `android-x64` metadata. This is not a billing purchase,
financial-test failure, or evidence that a different framework binary is needed.
The hosted Android SDK is36.1.118; the local reproduction is36.1.69. The observed
per-RID inputs and original target contract, rather than SDK-version speculation,
explain the incorrect count.

## Correction and bounded controls

Untrimmed verification now requires a nonempty unique SDK-declared RID set and
exactly one Core input for each member. Empty, duplicate and undeclared input
RIDs are rejected. Every assembly item is hashed against the existing prepared
SHA256; paths are never deduplicated to hide missing coverage. Compiler package
provenance and trimmed-linker checks remain unchanged. Consumer-target changes
do not alter the source producer recipe, version5 package or successful source/API
witnesses, so no framework compilation or immutable package rebuild occurs.

Eight direct controls import the actual production target. Before correction,
two-RID valid input failed, while missing, unknown, empty and duplicate-declared
RID conditions were incorrectly accepted. After correction both valid shapes
(single and two RIDs) pass; missing, duplicate, unknown, empty, changed-hash and
duplicate-declared cases all refuse. Two additional controls with no declared RID
or no Core input also refuse. These are10 bounded build-guard controls, not new
product unit tests or a repeated application suite.

The first local observer wrote the two original per-RID inputs successfully but
then failed with MSB4012 while formatting an item count. That observer failure is
retained and is not called the production failure. Its count formatting alone was
corrected; the subsequent normal actual Debug billing-sandbox build completed
with zero warnings/errors and both expected Core hashes equal. No isolated
`Compile` target or resource-less app output was used. The working tree also
contains unfinished Home owner changes; this build qualifies the guard and is
not a claim that the separate Home batch or the eventual commit ran on Android.

## Retirement and remaining work

One standard Debug Clean removes16,103files/894,356,671logical bytes;159files/
5,714,311bytes remain. Guard sources, actual input metadata, hashes, original
failures, command logs and control outcomes remain in the owned D1198 evidence.
No supplemental deletion, frozen scope, phone/Main, protected C7/Q02, consumed
register interpretation, runtime/GC change, purchase, owner authentication or
financial write occurs. Previously successful Release/Windows/test jobs are not
repeated for this build-target-only correction.

The next pushed commit still needs its sole delayed CI observation; no hosted
success is inferred from the local build. Home owner qualification, original
native-fault cause/remedy and whole performance/security/accessibility/platform/
provider/release gates remain open. Canonical parent States remain unchanged and
the full delivery goal stays active.
