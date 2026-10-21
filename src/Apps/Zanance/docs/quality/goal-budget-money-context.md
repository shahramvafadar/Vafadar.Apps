# Original goal and budget monetary contexts (D-431 / AT-431)

## Reproduced defect and resulting behavior

Four original actual-source SQLite cases fail before repair: goal target, contribution amount,
budget total and category limit. Retaining only a parse culture or ISO currency is insufficient:
ambient display-unit lookup can multiply an already typed amount. The category regression stores
123400 instead of the original1234 minor units.

Each monetary field now retains its own immutable MoneyInputContext, using the central explicit-unit
parser introduced by D-430. Display changes keep raw text, its original unit and a format example.
Currency/unit changes require complete clearing and reentry; appending a digit cannot acknowledge a
new unit. Actual editing with unchanged currency/unit may adopt the current numeric culture. An
explicit authoritative budget suggestion starts from stored minor units and captures a fresh context.
Read-only reload merges dirty contexts with raw text. Identical digits entered explicitly in another
unit are a semantic edit, rather than an untouched saved value. Untouched saved fields can renew their
format only through explicit reload, without rewriting their original financial amounts.

Target and contribution are independent. A share-of-income method ignores its hidden fixed amount;
physical quantities retain their gram/count rules independently of price-currency display units.
Changed hidden budget limits expose their complete review controls and a reload reason. No automatic
currency conversion, ledger posting, schema/migration/model, portable format, permission, SDK or
provider change is introduced.

Save captures the original draft, edit revision and native frame before publishing validation feedback.
Paired actual-file writers check that owner before work and after SaveChanges under the transaction,
before commit. Edit-and-return-to-identical-text does not revive a pending submission. Spending-cut
confirmation keeps the same original input and reviewed budget. Accepted goal/budget/cut work is
consumed immediately after commit, before fallible change notification; a saved form cannot rearm.

## Focused source verification

25 new actual-source application cases pass. The final affected cohort has97 passing cases (22 new
and75 prior goal/budget cases), followed by only the three new hidden-field/quantity boundaries.
This is100 distinct related cases across those retained executions, not a full-suite rerun or a
new complete-suite baseline. The original four failing cases, writer retirements, accepted-notification
failures and validation-observer mutations remain recorded. The additional boundaries verify visible
review for hidden saved limits, an irrelevant hidden share amount and unchanged physical quantities.
Strict test compilation has no warnings/errors. Existing money-parser, unrelated app/backend suites
and previously accepted D-430 native actions are not replayed.

## Native evidence

Windows:72 valid observations across18 en/fa/de, Light/Dark and360/412/1280 contexts cover
original-format and required-reentry views in both actual editors. Native scaled captions/glyph
bounds, raw values and command targets pass. Original50 source tables and typed rows are retained;
one native goal Save and one budget Save preserve123456 targets/totals and1234 contribution/category
minor units, with zero ledger posts and unchanged base installation. Three source processes retain
2+4+66 observations. Sampled application-window images and native measurements agree; the owner's
input desktop stays Default.

A diagnostic constructor initially omitted the commercial-access port; the failed build and secondary
XAML warning remain before strict0/0 builds. Two original budget observations expired when a queued
tab rebuild retired the next fixture page. The helper now awaits that exact deferred rebuild, retaining
accepted work. All72 observations completed before the outer driver rejected their financial-receipt
shape. Its validation now uses goal/budget-specific evidence. Read-only recovery validates the same
66 final captures, package/PID/count and unchanged input desktop; no native action is replayed.

Android:24 valid original Release/AOT observations across en/fa/de and Light/Dark use the independent
x64/API36/412px emulator. Eight English observations and both original accepted Saves are retained;
only16 incomplete Persian/German observations continue on the exact source checkpoint. Native labels
keep complete lines without ellipsis and raw Entry text remains unchanged. Base rows remain unchanged;
source acceptance is one goal and one budget Save, not another Save in the continuation. No ledger
money is posted.

The first Persian caption check compared selected digit shapes literally and refused a complete
rendered label. Its instrumented continuation confirms expected text with Persian digits, two lines
and zero ellipses. Comparison now normalizes only digit shapes and retains native text. Both failed
observations remain. An early PID read expired before the same process appeared; its original reports
recover the session without restarting a financial action. Accepted source rows and completed views
are never replayed for observer repair.

The independent review uses actual production pages, bindings, native controls and original stores.
Its own helper waits for the production app's deferred tab rebuild after an in-form display change;
it never relaxes the editor's native-owner retirement. Native label comparison accepts only the
selected digit-shape conversion; all punctuation, unit/source text, complete line layout and raw
monetary input remain checked. Operating-system text scaling, speech and general stability are not
inferred from normal-scale measurements.

## Final candidate and ordinary installation

The complete signed offline Release APK is96112373bytes, SHA256
`b616c859a3e3ff7373628e78d24dd77f4af40c7e40b4e47cb5964daf4ee86bab`.
The stable candidate is under Documents/Zanance.Builds/D431-20261021-bc0c31cd.
ZIP integrity, permanent pro.vafadar.zanance identity, no INTERNET permission, non-debuggable manifest
and v2/v3 signatures pass. Native cipher hashes remain:
arm64 `48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c`;
x64 `20b009b7ce0fc50bf31fe6e321216085a2c9ee3f3e14e0eb6757597380c2c568`.
An earlier candidate inherited provider network permission; it was never installed. The explicit
Offline candidate replaces it. Strict Windows and independent Android builds complete without
warnings/errors. Large outputs use fresh task-owned alternate-volume directories.

The final candidate upgrades the ordinary emulator installation without reset. PID5298
shows My account /0.00EUR after startup; installed APK hash matches the candidate. Original412x800/
density160/font1.0/accessibility0/servicesnull settings compare exactly. The independent review is
stopped and its private observer is removed only after a matching remote hash. No Main financial
input, profile switch or Samsung action occurs. This is not a complete Main database audit.

## Remaining boundaries

No Samsung input, physical-key/notification/authentication acceptance, OS text scaling/TalkBack,
iOS, general ANR/crash fix, public hosting/provider, real purchase/store signing or product release
is inferred. D-386 keeps runtime and Android accessibility after ready feature development.
Next D-432 addresses original transaction primary/destination/fee/split monetary inputs: those actual
sources still consult current formatting culture/display units. Keep original ledger, receipt,
aggregate/Undo and writer ownership from D-425 through D-428; no prior accepted Save is replayed.
Account and holding money contexts are not covered by this goal/budget delivery.
