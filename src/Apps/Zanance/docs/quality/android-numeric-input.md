# Preserve original numeric text at the Android input boundary (D-440)

## Reproduced defect and shipping repair

Actual production purity, per-item weight and manual-rate controls receive text through their real
Android InputConnection. With de-DE formatting, the original numeric DigitsKeyListener silently
changes18,5 to185,1,234 to1234 and1,25 to125 before the model receives TextChanged. All three failures
are reproduced in the original process21895. An original signed en-US opening input-12.34 is intact.
No Save is invoked and all original50-table source/base rows remain unchanged. The original startup
ANR and same-process Wait continuation are retained; this is not a general stability repair.

Ordinary shared Android numeric Entries retain their numeric input-type flags but use TextKeyListener
without automatic replacement. SetRawInputType preserves the numeric keyboard hint without restoring
a character-filtering listener. Application validators retain responsibility for the original culture,
unit, sign, incomplete input and invalid text. The adapter never sanitizes a draft into another amount.
Password entries and native numeric-password variations are excluded; original length filters remain.
Mappings follow the real Controls static initialization and run after Keyboard/IsPassword/IsReadOnly
and prediction/spelling changes,
including an initial mapping. There are no retained per-view event handlers or platform changes outside
Android. Windows/iOS behavior, financial parsers, stored money and schema remain unchanged.

This follows the documented distinction between [DigitsKeyListener character filtering](https://developer.android.com/reference/android/text/method/DigitsKeyListener)
and [TextView input type versus raw input type](https://developer.android.com/reference/android/widget/TextView#setRawInputType(int)).
Original numeric flags12290 and later536578 (spelling disabled) are recorded as integers, rather than
misinterpreting overlapping text enum names.
No locale hint or system keyboard/locale setting is changed; this is not acceptance of every vendor's
soft-keyboard key layout, physical keyboard, composition sequence or accessibility input path.

## Focused actual native evidence

The original completed physical/rate fixture4a5de01df1a94c618bd0059b594417c5 is resumed; it is never
seeded again. Candidate process22255 preserves all four original requested/native/model inputs.
Followup-only process22492 adds13 distinct input observations: incomplete comma, German grouping,
Persian digits, negative weight, invalid suffix, duplicate comma, surrounding spaces, partial sign,
invalid opening, ordinary text and actual changes Text-to-Numeric/Password-to-Numeric/MaxLength.
All17 retained native input observations preserve exact requested text. Two corresponding checks retain
empty numeric-password masking/filtering and refusal beyond MaxLength4. No PIN/password is entered,
read or logged; the empty independent control uses the real shipping handler without a security service.
These are native regression observations, not new managed tests. A text-only draft need not be a
valid financial value: invalid input remains visible for the existing validator to refuse.

Complete original source and independent base rows remain unchanged. There are zero accepted Saves,
new ledger entries, setup seeds or repeated accepted financial actions. Successful D-438/D-439 monetary
geometry and prior source/backend suites are retained. Followup observations do not replay the four
completed candidate cases. The harness and real-app fixture are unavailable in ordinary Release.

Strict final Windows and independent complete Release/AOT builds have zero warnings/errors. The
complete signed Offline Main APK is96,911,263 bytes, SHA256
0cf40bede588dadf7b152c7045e907f898ee9dddc83e652bae9bbc305b11a1b0.
Signature v2/v3, permanent identity, no INTERNET, nondebuggable output and both exact libsqlcipher.so
engine hashes match the declared policy/previous engine bytes. Managed provider AOT output is not
mistaken for the two native engine binaries. No new SDK, dependency, permission, export or data is added.

Initial Main qualification upgrades without clearing data: original PID17310 becomes22703. The actual shipping expense
field retains raw1,25 entered through native input; Cancel/Discard removes only this unsaved draft,
without Save. That retained Home preserves My account/0.00 EUR. All139 native nodes/attributes match the prior
Home except ordinary focus. Font1.0,412x800 without override,density160,accessibility0 and absent
services remain unchanged. Two fresh hierarchy attempts return null during startup/navigation;
only those observations continue on the same22703 process, without consuming a stale tree or restart.
No Main private database/key is read. Windows's final source build has zero warnings/errors.

The later guard-only scope retains those completed four/13 inputs and password/length checks.
Original processes23076/23469 show model IsReadOnly=true with native Focusable=true and
FocusableInTouchMode=false; the first static failure lacks a precise check identity, corrected by
safe independent static-check reporting. This establishes a native focus-state mismatch, not a
financial write or proof of a read-only editing exploit. The Android adapter now restores MAUI's
read-only focus/cursor state after numeric mappings. The exact
[MAUI10.0.110 implementation](https://github.com/dotnet/maui/blob/10.0.110/src/Core/src/Platform/Android/EditTextExtensions.cs)
shows which keyboard/read-only/prediction/spelling updates can reinstall the native input type.

Final guard process3616 confirms both initial and changed read-only states are not focusable;
read-only-off, prediction-off and spelling-off preserve three additional native/model inputs.
There are20 distinct applicable input observations and four password/length/read-only guards across
retained completed scopes. No previous successful scope is rerun to obtain a cleaner report.
Original source/base equality also succeeds after the emulator's external stop/cold startup.
This adapter-only guard scope uses320dp after cold-launch defaults; it is not an OS layout matrix.
The original physical412x800/density160 launch geometry is restored before final Main qualification.
The earlier23076 startup ANR/Wait remains real even though last-ANR metadata reports none.

The owned AVD initially refuses startup for insufficient disk space. Standard default test clean
succeeds without recovering space. Seven old generated Windows binary-output folders are archived
intact on C, with mutable data/package/key files and redirected paths refused; existing observations,
source fixtures, accepted receipts and unrelated files are untouched. The original AVD/userdata is
never replaced, wiped or reseeded. This is neither a performance comparison nor an ANR remedy.

Final complete signed Offline Main APK, including read-only focus restoration, upgrades the original
installation without reset. On restored physical412x800/density160/font1.0, final Main3522 preserves
raw2,50 in the actual Expense field; Cancel/Discard retires the draft without Save. My account/0.00 EUR
returns, and all139 original Home native nodes/attributes match except ordinary focus. Accessibility0
and absent enabled services also match exactly. The final signed package retains192 AOT modules per
ABI and both original cipher-engine hashes. No Main private data/key or physical-phone input is used.
The full original checkpoints and the later focused source-preserving guards are retained separately;
only incomplete guard work and changed final package/input are verified again.

## Reproduction and remaining acceptance

Use the original plan-form review_run, fresh review_pass and review_plan_resume_actions=true with
review_numeric_ime=true. The original failing/fixed baseline is separate from
review_numeric_ime_followup=true, which adds only the later edge/adapter cases.
review_numeric_ime_guards_only=true continues only the later read-only/prediction/spelling scope. Require the actual
unlocked independent focused Activity and complete original fixture receipt before any form work.
Compare requested, native and bound-model text through the real InputConnection, including native
integer input flags. Never infer input acceptance from a model-to-native geometry observation.

D-439's sole exact-commit response confirms CI39663889948 and CodeQL39663889883 both successful;
its own one-shot followup is deleted. D-440 needs its own pushed-revision check. General Android
ANR/crash/cold/low-memory/performance, remaining OS/spoken/device/iOS acceptance and external
provider/company/Store/publication gates remain open. Samsung receives no input in this batch.
