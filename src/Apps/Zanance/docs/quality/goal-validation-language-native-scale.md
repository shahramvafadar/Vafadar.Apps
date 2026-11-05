# Retained goal validation and native large text (D-848)

## Original failure and correction

Existing GoalEditorViewModel field errors store their rendered caption. RefreshDisplay
updates ordinary captions and the new percent-format note, but previously retained
NameError/AmountError/ContributionError/PercentError/RuleError in the old language.
A user had to submit again to see current-language validation. This also prevents
qualifying the changed percent-note/error cluster across languages without Save replay.

Refresh only nonnull existing field errors to their current captions. Keep numeric
unit-review reasons distinct through the existing target/contribution flags before
UpdateInputUnits applies current ownership. Do not validate a new draft, create or
clear an error, renew a parser, write, or replay Save. Dynamic SaveError/receipt/
commercial failure behavior is unchanged and is not newly accepted by this batch.

Twelve new six-language/fixed-or-share cases expose12 original failures at the first
retained name-caption assertion. After correction all12 pass with current name/amount/
rule and contribution-or-percent captions, retained raw/date drafts and unchanged
original snapshot/empty ledger. One original rejected validation per case, no second
Save on language refresh. The initial setup enum Fixed does not exist; retain CS0117
and correct only the test fixture to FixedAmount before the actual baseline run.
Two directly affected existing target/contribution unit-review cases pass. Total
12new+2existing selected tests, not a full suite or36new native unit tests.

## Separate owned native environment

Create only fresh Zanance_D848_GoalValidation, serial emulator-5612, Android36/x64,
actual4096-byte pages, boot a12d35f8-2d5f-4441-9e7e-280e14f2d2bf. The retained D809
emulator-5606/boot6663f98e is never configured, launched, reset or queried for source/
R13. Its complete public baseline compares exactly after this work. Shared ADB5038/
PID50972 survives; no server/reset/reconnect/pair or phone command occurs.

Set only the fresh device's OS font2.0 before first application launch. Keep density420
(native2.625); widths945/1082/1785px give actual native360/412/680dp. No stale-density
workaround or global SDK/configuration change is made. The original App sources/XAML
are directly linked into an offline private package with one DI capture. Fresh UID10216
belongs only to5612, not either retained UID10216/10234 on5606. PID4577 stays throughout.

Prepare one earmark goal/unpaid share-plan metadata with target1000EURminor/rate1.25,
no account/entry/schedule or financial posting. One original native EditText invalid
100+1e-27 and exactly one visible enabled original native Save produce the field error.
Change only this private app's formatting to de-DE while retaining its English dot
parser. All later contexts keep that raw input, same generated error and SaveClicks1.

## Thirty-six distinct contexts and explicit oracle failures

Six languages en/fa/de/es/fr/it, two effective light/dark themes and three actual
widths produce36 accepted contexts/72 native caption observations at FontScale2.0.
Every accepted note/error has exact actual native text, complete visible bounds,
zero ellipsis, no mutual overlap and native text sizes65.625/68.25px. Persian native
layout direction is RTL; its displayed digits differ from the unchanged Latin input.
These are runtime caption observations, not36 unit tests, all-form or glyph/speech/
contrast/whole accessibility acceptance.

The first direct dark UserAppTheme request is reset to Unspecified by the original
ThemeService System policy while OS night mode remains no; disqualify it. Continue
only that unfinished context by changing the fresh system's actual night mode, using
the product's supported system-following path. The effective theme is explicit
UserAppTheme when retained, or inferred from actual OS night mode and the original
System policy when Unspecified. This is not pixel/palette/contrast proof or acceptance
of every explicit persisted theme-choice path. No product theme policy is changed,
and no Save, language/context setup or accepted context is repeated to rescue it.

The first Persian audit incorrectly equates bound Latin digits with the native label.
Original VafadarMauiAppBuilderExtensions maps label display with NativeDigits.Apply.
Reaudit that saved packet using only the exact ASCII-to-Persian digit mapping, keeping
all text, separators and bidi marks. Raw input stays Latin; no context/input/Save replay.
All other comparisons and complete-bounds requirements remain unchanged.

Every context and terminal comparison retain all20 declared typed source components,
SHA256`36EE9E1F1E0E1A493E34A39F85CB6A7C2E03F54D26EA764E5AE065EA00140EB9`.
This is not all schema/cipher/WAL/key/private Main equality. Restore original private
language/format/theme and return to Onboarding; the departed editor is correctly not
Ready. Only the fresh package retires. No financial Save/Import/Undo, authentication,
photograph, TalkBack or accepted prior corpus/test is replayed.

## Artifacts, restoration and retirement

Strict Windows, complete offline Android and private native builds pass without
warnings/errors. Nine actual App/Core/Data AOT members match their build artifacts;
no cross-build PE identity is inferred. Original Mono, offline/nondebuggable, v2/v3,
ZIP CRC/alignment and static16KiB LOAD pass, not actual16KiB hardware qualification.

| Artifact | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Used private native |fc7d6b8abd8921754f1634065ad8fbe5ecfdb13c2010eaa20001b56efb949124|56758044|192/204/804|
| Complete shipping |904c1f91057b7fff713b49e5899b8c36bcc7c03171dddb878cec6550e9264b94|99483551|384/408/1608|

Fresh font1.0, physical-only1080x1920, original night no/secure1, five OS values,
original absent locale key, all prior package UIDs, boot/reverse/shell restore exactly.
An added en-US system-locale key is observed and explicitly restored to absence;
its exclusive writer/cause is unknown. The original retained emulator is independently
exact before/after; no inherited Main/source acceptance is inferred.

The launch script's inherited stdin __file__ puts five fresh local launch receipts/
logs in the repository root, although its official AVD root is correct. Their creation/
owned-host binding is verified; retain hashes/copies and retire only those five files,
16584logicalbytes, after host exit. No product/unrelated file is removed. The first
8-second stop observation is not terminal; verify the same host56784/conhost52928/
QEMU77080 has ended, without another kill/restart, before the official AVD deletion.
Only that fresh26file/4783879769logicalbyte AVD retires; no frozen scope is touched.
Four standard Clean profiles pass: tests384/windows1172/shipping1289/native1283
file-deletion lines, not unique physical-byte totals. Used signed packages, exact
sources, all raw packets and failed observers remain; no device XML was created.

D847 sole CI response records Self-hosted service qualification41982937300, Local
identity backend41982937264 and CodeQL41982937319 success; CI41982937293 in_progress.
No second request or successful job log is read. Shipping904c1f91 is prepared, not
installed on Main. D84615641/868f06c0 remains only the last dated physical observation.
Pending D838/D841 actual new-PC authority, terminal old R13 and D607 evidence stay
protected. Original fault, whole performance/security/platform/provider/release gates
and the overall goal remain open/active.

Related: [original percent precision](exact-goal-percent-input.md),
[physical normal-text checkpoint](physical-goal-percent-main-adoption.md),
[execution frontier](remaining-delivery-evidence.md).

Later [D849 physical adoption](current-physical-main-adoption-d849.md) installs the
existing904c1f91 once without reset as Main20144, preserving first141raw Home nodes
and original public identity/settings. The prepared-only statement above is historical.
