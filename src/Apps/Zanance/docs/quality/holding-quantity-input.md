# Original holding quantity input and same-form correction (D-412)

## Reproduced defects and complete change

A parseable Decimal maximum (`79228162514264337593543950335`) overflowed during conversion to stored
base units before Quantities.TryParse could return false. Three original Core cases reproduce it for
grams, kilograms and pieces. Two actual editor/SQLite regressions reproduce the same parser exception
and a separate Int64 sum overflow while typing a representable quantity (`9223372036854775` whole
pieces) above an original one-unit holding. Original baseline failures are preserved; a fixture's
initial missing required currency was a test compile failure, not product evidence.

Check the original value against Int64/base-unit factor before multiplying. The compatible existing
TryParse entry point remains; its explicit range-reason overload lets the actual editor distinguish
range refusal from ordinary sign/precision/indivisibility failures. Exact representable maxima stay
accepted. Existing false-with-converted-fraction output semantics remain unchanged; consumers use
quantity only after success. The editor omits an unrepresentable preview and checks the original
proposed quantity/history for range feedback before constructing financial entries. Raw quantity,
note, original review and correction authority remain available for an explicit smaller quantity.
The existing atomic writer still revalidates sources/rights and checked history. No clamping, guessed
quantity, financial relationship, schema/model, storage scale or write-authority bypass is introduced.

The new feedback has real en/fa/de/es/fr/it translations. QuantityError/FormError route the same
original validation value to one field-adjacent label without a duplicate form message; the existing
command, raw draft and other original form errors retain their semantics. This is a bounded input-validation change,
not a dedicated D-386 general ANR/crash investigation or broad stability acceptance.

## Selective actual-source tests

Sixteen distinct new cases pass in retained cohorts:3 original parser regressions;8 exact unit/max
boundaries;3 ordinary precision/indivisibility/sign cases;2 actual editor/SQLite refusal and same-form
opening corrections. The first boundary cohort had10 passes and one assertion expecting a zero out
value after an indivisible fraction refusal; the old implementation deliberately returns its parsed
1500 while returning false. Only the changed three-case ordinary-refusal method was rerun with that
original contract preserved. This was a test expectation correction, not a financial product defect.
Twenty-one existing resource completeness/placeholder checks pass for the changed resource set.
No unchanged full suite or D-408..411 accepted financial case/matrix was rerun. Test outputs were
cleaned only after all test/build processes finished. These counts do not claim a new full-suite run.

## Original native refusal/correction cases

Windows run `5b014fc0293b47579399fb9d7d5e94cf`, PID93764, completes normally on its separate non-input
desktop; original input desktop is Default before/after. Android run
`89cff8e9c0884252b9b2940348e0bcf2`, comparison`a4dd310bd6eb49178fde3fed8701f633`, PID21289, completes
on the independent Release/AOT x64 caller in Zanance_Storage_Review/emulator-5570.

Each platform uses two separately owned original SQLite fixtures. Original purchase1unit/25EUR,
fee1EUR and linked refund0.75EUR leave Cash874.75. The actual new quantity-correction form receives
the original raw-range or aggregate-range draft with explicit reason/note. One native Save refuses
with complete stored rows unchanged, complete translated feedback, original raw draft/review,
enabled correction and no navigation. That same original form changes explicitly to2units and saves
once. Each case adds one nonfinancial correction event, leaving quantity3units and total basis25EUR,
while every original financial/refund row, original source event/metadata and price remain unchanged.
Cash stays874.75 and no money row is created by the correction. Known acceptance retires that form.

Four live refusal/acceptance observations per platform verify the native Save enabled/disabled state.
Afterwards only the exact original rejected presentation is rehydrated with retired write authority:
18 read-only Windows contexts (en/fa/de, light/dark,360/412/1280), with22 total captures including the
four original live observations;6 read-only Android contexts (en/fa/de, both themes) check actual
native text, complete line/viewport geometry and the modal's own window focus. All22 initial Windows images
were inspected. These first caption contexts used the original global error location, before the
final field-adjacent presentation correction below. The retained original model identity/draft labels are not general live locale-switch
acceptance. No accepted write or fixture seed is replayed to produce caption evidence. Android's
first public-report observation expired; the same live PID later returned its terminal success,
without restarting the process or regenerating files.

## Final field-adjacent presentation without accepted-write replay

The image review identified that the new range message belonged immediately after the quantity
entry, following the repository form-error rule. QuantityError/FormError derived presentation and
one named field label now route that same Error value beside the input. Validation/math and the
original accepted commands did not change. Other original form errors keep their existing surface.

Windows continuation `e8ab09bec3dc47e2abf1e033128c76c3`, PID10264, reads only the original completed
proof and original two SQLite fixtures. Cases retain PID93764 and match their exact original accepted
receipts; Live is empty and QuantityCommandsInvokedByContinuation=0. Its18 final en/fa/de light/dark
360/412/1280 captures assert the exact named field label/entry parent, no duplicate form error,
complete actual native text and original rows unchanged. All18 final images were inspected; the
input desktop remains Default. The former18 global-caption images remain preliminary evidence.

Android final comparison `79f2d2b9647a4708ac9593ddcc2fa7b3`, PID21877, continues the same original
run/files and requires the exact original completed comparison. Cases retain PID21289, Live is empty,
NativeButtonsInvoked=false and QuantityCommandsInvokedByContinuation=0. Six final native caption
contexts verify the named field location, complete actual text/line/viewport geometry, modal focus,
original stored rows and exact raw refused draft, with original theme/language restored. No writer,
seed or accepted quantity/money operation is invoked to qualify the changed placement. These final
field contexts replace the preliminary global-caption placement claim; original live financial/
holding assertions remain historical evidence rather than new current-process invocations.

## Builds, package and restoration

Strict Windows build and independent Windows build complete without warnings/errors. Main Release
Android packaging and the independent Release/AOT build pass; the complete signed offline Main is
95,600,543 bytes, SHA256
`02a7177e1a53210f9c97b4befd6bff0b5cd4ca8410bcb8f6456f154773b77294`.
It contains ARM64/x64 runtime assets, the existing development certificate, no debuggable flag and
no INTERNET permission. Final independent field caller:55,930,786 bytes, SHA256
`1545e19478801ed0e1da2ec5268cdf3d10352e854929fbb385b490f1ac074e40`.
Its x64 engine matches final Main:
`20b009b7ce0fc50bf31fe6e321216085a2c9ee3f3e14e0eb6757597380c2c568`.

The independent caller stops only after terminal success. Two source-change no-reset Main upgrades20853→21608→22114 (arithmetic, then field placement)
restore original public Home0/My account, final signed candidate hash, exact four recorded OS values,
size412x800 and density160. Original independent fixtures remain preserved. No installed Main private
database/key/internal ID is extracted, no system setting is changed and no Samsung input is made.
Local evidence:artifacts/holding-quantity-d412; original Windows captures:artifacts/store-drafts/
5b014fc0293b47579399fb9d7d5e94cf and e8ab09bec3dc47e2abf1e033128c76c3; complete final candidate:artifacts/android-holding-quantity-d412-field/
pro.vafadar.zanance-Signed.apk. Private data, fixtures, keys and complete logs are not published.

The authoritative scope is raw/base-unit and checked proposed-quantity input in this actual editor.
It is not all possible basis/value arithmetic, import/sync corruption, every holding relationship,
physical-device/iOS, general runtime/A11Y, provider/store/expert/tax or release acceptance. Hosted
exact-head CI is checked once separately. Remaining canonical-backlog and external acceptance gates stay open.
