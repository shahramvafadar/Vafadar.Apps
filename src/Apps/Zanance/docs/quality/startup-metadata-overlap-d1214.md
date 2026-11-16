# Original startup metadata overlap: terminal rejection (D1214)

The owner requests the next independent startup/Home strategy after the rejected
[D1213 generated serializer](post-preparation-home-plan-stamp-d1213.md), with the
full delivery goal remaining paused and no unnecessary successful-test, timing or
financial replay. D1213 stays closed: no alternate generated mode or cold-only rescue.

## Independent hypothesis and fixed boundary

A storage-free sibling task prepares metadata through the exact original
`PlanWorkSnapshot.CaptureInputs` serializer, using one disposable Schedule and
OccurrenceState. It overlaps the unchanged original protected startup. The sibling
is drained before the post-preparation marker and before publication, including
original failure/cancellation. Original failure identity/priority is preserved;
ordinary metadata failure falls back to the original path, while metadata OOM
remains fatal when no original failure exists. Each callback executes once.
No key/connection reuse, integrity/KDF relaxation, generated serialization, financial
write, schema, runtime/GC or product configuration change occurs.

Eight new private ownership/failure controls and four new bounded peak-parser
controls pass on .NET10.0.15. These are12private controls, not product tests; the
second build adds the parser controls without rerunning the eight passed controls.
The parser emits only a positive aggregate VmHWM counter from `/proc/self/status`;
no memory/register/map contents are emitted.

One strict signed offline/nondebuggable x64 APK serves both roles, selected by
exclusive per-role markers. Seven inverse-order process-cold pairs are registered
before deployment. First-pair continuation requires both time ratios<=1.05,
allocation<=1.01 and additional peak<=65536KiB. Final medians require whole time
ratio<=1.00, post-preparation ratio<=0.95, all-thread allocation<=1.01 and additional
peak<=65536KiB. No sample exclusion, favorable rerun, rescue or threshold change.

Both roles use the same private fixed UTC2026-11-16T12:00:00Z; monotonic timing is
real and the OS clock is untouched. This new cohort is compared internally, not
claimed identical to the earlier D1212 date. Each cold launch follows actual cached
state/adj>=900 and two old-process absence observations; no force-stop is used.
A fixed45second observation-quiet period is outside the measured ready interval.

## Complete result

| Role | Metadata prepared | Whole ready ms | Post-preparation ms | All-thread allocated bytes | Peak KiB |
| --- | --- | ---: | ---: | ---: | ---: |
| 1 | No | 9798.4548 | 4715.9976 | 159472840 | 483340 |
| 2 | Yes | 9924.4638 | 4483.1913 | 159459016 | 483344 |
| 3 | Yes | 10301.8281 | 4737.0686 | 159443472 | 480004 |
| 4 | No | 9813.8343 | 4750.6411 | 159217552 | 479540 |
| 5 | No | 10000.6057 | 4913.4217 | 159506776 | 478916 |
| 6 | Yes | 13031.0850 | 4881.5444 | 159448744 | 480836 |
| 7 | Yes | 10524.9219 | 4703.1039 | 161337160 | 481540 |
| 8 | No | 22080.6035 | 15643.5351 | 161634968 | 478980 |
| 9 | No | 10127.5879 | 4864.4195 | 159173672 | 483408 |
| 10 | Yes | 9850.0215 | 4472.9576 | 159534680 | 480008 |
| 11 | Yes | 10513.0259 | 4798.9074 | 159571504 | 478592 |
| 12 | No | 9948.3661 | 4726.1648 | 159201856 | 483068 |
| 13 | No | 10061.4375 | 4789.7209 | 159445504 | 479008 |
| 14 | Yes | 9950.7660 | 4579.4182 | 159254432 | 479784 |

Whole medians10000.6057 -> 10301.8281ms give ratio1.0301204 and fail the no-regression
gate. Post-preparation medians4789.7209 -> 4703.1039ms give ratio0.9819161 and fail
the required5% reduction. All-thread allocation159445504 -> 159459016bytes gives
ratio1.0000847; median peak479540 -> 480008KiB adds468KiB. Those memory gates pass,
but both time failures close the candidate before product promotion. The slow
original role8 and candidate role6 remain in the complete sample set. No exclusive
metadata/IO/JIT/scheduler cause or universal regression is established.

The same retained independent100k-entry/200-account/1000-schedule source is reused,
never recreated. Every role has complete typed-source equality, all32entry fields,
original Home projection equality, zero financial writer calls, no dropped timing
markers/faults and a new PID under the same UID10216/boot. Full source digest:
`094FE4672762C9011770F00237E1BB26537B97F0A999ED0C5F480BF1FDCCB32E`.
The original serializer body remains byte-exact. No accepted setup, Save, Import,
Undo, settlement, authentication, camera or phone/Main action repeats.

## Package and retained evidence

Private native strict publish has no warnings/errors. Signed SHA256
`c19a53ee5e1c4d3f3c4edb93379bd6bf6d9e3daae9653c897944f568a4972d2b`
is56553244bytes,192AOT/204ELF/804static16KiBLOAD, v2/v3, ZIP CRC/alignment and
offline/nondebuggable. Stock Mono15 remains byte-identical. Ten actual compiler
source bindings and three own AOT-to-package comparisons are retained. The App
instrumentation reverses to the original source. There is no product edit, new
product test, unchanged Windows/full-shipping build or successful-suite repetition.

Raw role packets, exclusive consumed markers, qualification plan, source/PDB/AOT
proofs, host controls and the used signed APK remain under
`artifacts/d1214-startup-metadata-overlap`. No data/key extraction or userdata hash
is needed. Terminal PID10409/UID10216, owned focus and original public settings/locale/key
presence/geometry/boot/reverse are exact, using the last accepted complete-source
packet without another provider audit. Normal emulator shutdown returns exit0;
launcher6972 and its original conhost/QEMU children are absent, and shared
ADB5037/PID59860 creation identity is unchanged. Retain34AVDfiles/
12020766378logicalbytes, data, keys and every consumed role marker. The current
retention manifest points to the kept used APK after Clean. Two standard Cleans
remove13710files/822026764logicalbytes from only the new native/host graphs.
No supplemental/frozen deletion occurs.

The initial AOT observer incorrectly assumes the package's `libaot-` prefix is
also present in the build filename; only the unfinished three AOT comparisons
continue with the actual assembly filename. The terminal process observer initially
inherits PowerShell's absent-process error status; explicit successful absence
fixes that observer, with actual host names already absent. Both refusals remain;
no role, source audit, shutdown command or successful control is repeated.

The due D1213 single response records
[CI completed-success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43768354265)
and [CodeQL completed-success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43768354258).
Its exclusive marker/raw response remain; no second GET or successful-job rerun.
This is not CI acceptance of the new documentation commit. Overall goal stays
paused; whole startup/two-second performance, native-fault remedy, security,
accessibility, platform, provider, Store and release gates remain open.

The independent [D1215 complete Home plan-row reader](complete-home-plan-rows-d1215.md)
passes its actual host component gates but fails the first complete native pair;
this metadata-overlap rejection remains closed and unchanged.
