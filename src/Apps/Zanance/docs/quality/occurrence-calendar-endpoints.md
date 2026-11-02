# Calendar endpoints in original occurrence search (D-791)

## Actual current failure and bounded correction

2 November2026. Ten new original Core cases expose nine actual
ArgumentOutOfRangeException failures: three next-open searches whose growing end
passes DateOnly.MaxValue, two exhausted/settled searches at that end, and four
Before/After weekend shifts at or one day inside the representable endpoints.
The offset62 next-open control already passes in the baseline. These are deterministic
managed date exceptions, not the original uninduced SGen reference-scan failure.

Clamp only the expanding search end to MaxValue and stop once that inclusive final
window is exhausted. Keep62/248/992/3968 horizons, number/status/order/identity/state
and current recurrence padding unchanged. ApplyWeekend stops at the representable
boundary when its next step would leave DateOnly, retaining that last date; this
is not a claim that the boundary is a working day. Its original31-step guard and
normal holiday/weekend behavior are retained. No calendar, due amount, automatic
posting policy, schema/model, clock, runtime/GC or permission changes occur.

The four open controls use the last date from0/30/62/100days before it; two settled
controls return null without changing their original state; four implicit shifts
preserve original rule inputs and stop at the lower/upper edge. Ten final cases pass,
plus six affected existing pure Core weekend cases. This is16 selected/10new, not a
full suite or a new financial-native cohort. Initial SDK-host/noSDK and incorrect
in-process test-option refusals execute no tests and are retained separately.

## Final build, original Android and retirement

Final Windows single-TFM Release and both Android Release publications pass strict
verification without warning/error diagnostics. The first Windows command propagates
win-x64 globally and fails to find a library PRI; preserve it separately from the
successful original single-TFM command without that extra property. The fresh
emulator's early offline/incomplete boot observations do not cause restart/recreation.

The complete signed shipping APK is99,458,975bytes, SHA256
`37d4f6552cfe28ebb1a1be0aba2e3d149cc0a8b64976cfb2bddfcd545cd29798`,
384ownAOT/408ELF/1608staticLOAD. The independent original-app APK is56,848,290bytes,
SHA256 `3881a0d923f70ff9d633cf3fa41e7473cc2d866a47a4eef1eac3c82982d0257d`,
192ownAOT/204ELF/804staticLOAD. CRC, v2v3 signature, ZIP16KiB alignment, offline/no
INTERNET and nondebuggable checks pass for each. Actual Mono digests remain the
previous x64edfa02c3/ARM64f03c6356; no runtime/GC change. Static alignment is not
hardware16KiB runtime acceptance or proof of complete linked-PE byte identity.

One fresh API36/x64/ranchu/4096 emulator, UID10216/PID4539, original OnboardingPage,
boot a48c98e5-4b3a-4bc5-b27e-66dd45a1d594, executes those ten actual production Core
calculations once through an emulator/run/package/shell-bound private port. Its
complete20typed-source serialization is equal before/after and source account/entry/
schedule collections are empty. No account, plan, PIN, ledger, Save, payment, picker,
notification, authentication or financial setup occurs. This qualifies original
calculator behavior on Android; it is not a new calendar-editor/native-input matrix.
No UI source/control/layout changes, capture or visual acceptance claim is introduced.

Five OS values/boot/geometry/reverse and the same process remain exact, with no setting
write. Only the fresh package retires. Original owned host68060/QEMU78604 exit and
port5594 closes; official AVD deletion removes26files/4537136745logicalbytes and shared
ADB5038/PID66176 stays alive. Four standardClean operations remove28916 inventoried
files/1941526062logicalbytes;4815 inventoried files/1013366792logicalbytes remain.
No supplementary removal/frozen action occurs. Used signed APKs, four private source
files and local receipts stay. No physical-phone action is performed: Main remains
D790's adopted D787/d5a59bff, while the new complete D791 candidate is not installed.

D752 remains owner-readiness gated with its original frame interruption preserved.
D790 physical Main and every historical frozen scope remain untouched. This correction
is not general native-crash, whole-Home speed, private Main DB/key, complete security/
accessibility, iOS/provider/Store/release acceptance.
