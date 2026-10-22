

# Original Home goal progress on a worker (D-446)

## Concrete source boundary

Home previously calls GoalHomeProjection.Project synchronously after BuildSlicesAsync. With any
active or paused goal, GoalProgressService builds an AccountEntryIndex for the complete original
ledger, computes account balances/earmarks and evaluates contribution/holding progress. Those pure
financial calculations therefore run on the window thread even though earlier Home scans use a
worker. This is a concrete scheduling gap, not proof of the general startup ANR's cause.

GoalPresenter.EvaluateHomeAsync retains captured type metadata on its serialized caller, and runs
only the existing pure GoalProgressService.Evaluate on a worker. It neither reads another store nor
changes the financial algorithm or original source selection. GoalHomeProjection.ProjectAsync awaits
that result and formats original rows on the caller's context. Home awaits it and requires the
original live publication before publishing rows. Existing original-file/rights/snapshot validation
and the covered refresh remain. No schema, financial writer, source network, font, layout or runtime/
GC policy change occurs. The old synchronous projection remains the equivalence oracle.

## Focused actual-source checks

Seven selected cases pass in13.801s: three new en/fa/de output/source equivalence cases, one new
cancelled-projection refusal and three affected prior complete Home projection cases. A separate
new case passes in3.261s, observing the calculation task's public TaskStarted identity on a different
managed thread from its dedicated original caller, then comparing the original financial result.
Only task/thread IDs are observed; no stacks, memory or application values are captured by that
listener. The five new and three prior cases are eight distinct cases, not a full-suite baseline.

The initial thread-observer pattern does not compile; fix its local index declaration before any
execution. Its first exact MTP method filter selects zero cases and the minimum-test policy rejects
it. Use the actual wildcard selector and retain only its one executed pass. Do not call either
failed invocation a passed test, and do not repeat the seven already completed cases. Focused test
output cleanup passes without warnings.

## Original native source and complete Main package

The new independent home-goals-worker phase resumes only the exact D-436 secondary source
82d32312e5494c2a99fa68b8160d6490 and all five accepted receipts; no goal, account, seed, contribution,
bill or payment is repeated. All original typed source/base rows remain identical. Original PID7857
completes three English/Persian/German normal-scale observations with two active financial goals,
original goal output and actual Home publication on the window thread. This retained source has no
pinned cards; it qualifies active calculation and complete Home publication/empty goal group, not a
new positive pinned-card rendering matrix. Focused actual-source cases cover pinned-row equivalence.
No accepted write occurs, and the completed scope is stopped only after its terminal receipt.

Strict Windows build succeeds in55.48s without warnings/errors. The first default independent build
finds D-445's compile-time-false package branch as unreachable; use actual runtime package identity,
without weakening its owned-package/root guard. The corrected independent Release/AOT build passes
in1:22.94 without warnings/errors. This correction does not replay successful D-445 USD scenes.

The complete signed Offline Main APK is96,476,917 bytes, SHA256
7381cf24476d9cf9ac01bd8945c9c1dddb6cc917ba5adad1c27029be50d94d30.
Both arm64-v8a and x86_64 are packaged, with384 AOT modules and valid v2/v3 signatures. No INTERNET
permission is added. Both actual libsqlcipher.so engine files match D-440 byte-for-byte. An initial
host proof incorrectly includes rebuilt managed provider AOT modules among native engines; correct
only its filename predicate and inspect the same original APKs. No native scene/build is repeated
for this observer error.

Upgrade Main on the owned emulator without reset. Original My account/0.00 EUR and all139 prior
native Home nodes match except focus in the new expected PID7982. Installed package hash matches
the signed file. OS font1,size412x800,density160,accessibility0/services null stay unchanged. No
Samsung install/input or authentication occurs; the separate D-445 physical turn remains pending.
The prior Windows/Android successes and accepted financial commands are not replayed.

## Scope remaining open

This proves the original goal calculation's worker scheduling and preserved scoped output/data.
No latency comparison, two-second target, whole cold startup, low-memory/OOM, general ANR/crash,
audible accessibility, full physical-device/iOS/provider/store or release acceptance is claimed.
Continue only remaining evidence or a concrete new defect; retain rejected runtime/clock/bridge
experiments. The private original logs, reports, signed packages and exact restoration proof are
retained. Check this pushed commit's hosted workflows once separately.

The single exact-head response for82b18716b4e77483715598eeac7b1ed983220315 reports
[CI success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39693893024) and
[CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39693893018).
Its one-shot monitor is deleted. These results apply to the pushed D-446 source, not unfinished
D-447 edits or broader physical/stability/release acceptance.
