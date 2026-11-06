# Negative balance goal overshoot (D-909)

## Reproduced calculation defect

A supported positive target and negative recorded account balance could make
`GoalProgress.Overshoot` wrap into a positive number. For example, current
`long.MinValue` and target1 returned `long.MaxValue`, although the target was not
reached. The preview and original account-balance evaluation reproduce this defect.
Eight new preview cases and four original balance-goal cases run against unchanged
production code first:12total,7failures. Source JSON remains exact in every case.

The property now compares current with target before subtracting, then uses checked
subtraction only for a genuine positive overshoot. Negative balance/current, target,
remaining amount, progress clamp, notice, contribution/funding, ETA, currencies and
ledger behavior are unchanged. No model/migration, financial writer, runtime or GC
change is needed. The original presenter already checks IsReached before reading
Overshoot: an incorrect visible above-target caption is not demonstrated or claimed.

## Focused host and build evidence

The same12new cases pass after the correction on.NET10.0.14/X64. One affected
original active-balance overflow guard also passes; this does not weaken the original
checked per-account balance calculation. This is12new product cases, not24with native
execution and not a full-suite claim. Old unrelated successful tests are not replayed.
The retained runner output includes its existing `-xml` deprecation notice; that is
not a compiler warning, and successful cases are not repeated to change a report flag.

Strict test, Windows and full Android builds have zero compiler warnings/errors.
The full signed offline nondebuggable APK is99491743bytes, SHA256
`37518cfa86bbf8ca9df8af9a038a0ce15ce90b4a05d0d0e70ee9097f50e8ce54`.
Its384AOT/408ELF/1608staticLOAD, v2/v3 signatures, CRC and ZIP16KiB checks pass.
It is prepared, not installed on physical Main. Actual portable-PDB checks bind
GoalProgress in four builds and the new test source in host/native builds: six
checks, not whole-PE equivalence. Eight used Core DLL/PDB files are archived before Clean.

## One original Android page

The independent full-source native package uses192AOT/204ELF/804staticLOAD, exact
original x64 Mono, offline/nondebuggable/v2v3/CRC/ZIP checks. Its signed APK SHA256 is
`b6a7311ca8a443fb4a39cf8e85f9a78c1c645f637447ab79508b374cf3d7373f`.
Only its declared bootstrap service capture differs from original bootstrap text;
reversing that line gives the complete current original text exactly. Private
read-only qualification endpoints and test shims are absent from the shipping app.

Fresh API36/x64/4096 UID10269/PID20950 executes the12new cases once. Complete20part
source before/after these calculations is exact. The initial packet's Source20Exact
is false because the later page cohort has not yet been prepared; ScalarSourceExact
is true. This is not misreported as a failed or already completed page comparison.

One new fictitious Checking account with opening `long.MinValue` and one original
AccountBalance goal targeting0.01EUR are prepared once, with no ledger entry,
schedule, contribution, settlement or financial UI Save. Original progress preserves
the exact negative current amount, NegativeBalance notice and zero Overshoot.
Original GoalDetailPage is assigned as the fresh controlled root; this is page
integration evidence, not an end-to-end goal editor/navigation acceptance.

The first page packet is Loading and is not accepted as Ready. The same process
continues without relaunch/Retry/setup/test replay and reaches Ready/no failure:

| Original caption | Result |
| --- | --- |
| Balance/target |0.00EUR of0.01EUR|
| Remaining |0percent and0.01EUR left|
| Negative-balance warning |Exact−92,233,720,368,547,758.08EUR; progress counts as0percent|

All three actual native TextView strings equal their managed captions, all arranged
bounds equal global visible bounds, and every ellipsis count is0. One original UI
XML independently contains those captions; no extra frame, theme/width matrix,
screenshot bypass or glyph/contrast/speech acceptance is inferred. Full20part typed
source remains exact through page completion, SHA256
`F2E80A2BC032561C13C858ED8917ABFB44B929352E5C27C9B43A6960E0D7C16E`.

## Restoration and remaining work

Only fresh UID10269 and its single hash-bound16726byte remote XML retire. Local raw
XML, used signed APKs/current sources/compiler files/results remain. Original public
packages, five OS values, locale-key presence,1080x1920/420geometry, boot and reverse
state are exact. Retained Q02 returns normally in the same PID15609; no cold/source
reacceptance occurs. No phone/Main/private DB/key/authentication/photo or financial
operation replay occurs. Main17183/16b7b889 is only the last D907 observation here.

Four standard scoped Cleans remove28918files/2110147445logicalbytes.3680files/
588798922bytes remain; no supplemental deletion or frozen scope is attempted.
D907's sole response reports [CI42162637677 cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42162637677)
and [CodeQL42162637740 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42162637740).
Its exclusive marker/raw response/receipt are retained; no second request is allowed.
D908 has its own separate pending one-time check, without blocking this work.

Current12case ARM64 and prepared Main adoption remain separate missing conditions;
do not replay old host/x64/financial successes to fill them. Original native-fault
cause/repair, D838 pending authority/D841 undeployed reader, whole performance,
security/accessibility/platform/provider/release gates and overall goal remain open.
