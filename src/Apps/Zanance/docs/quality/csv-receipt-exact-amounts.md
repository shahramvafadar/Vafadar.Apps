# Exact submitted CSV and recognized receipt amounts (D-852)

## Two complete independent reader corrections

Continue ready Android work without owner/provider/CI waits. The D851 holdings reader
is already corrected and is not retested. Inspect the remaining general signed CSV
amount reader and the recognized-number receipt reader with new original input cases.
No Import/Save, financial setup, camera/OCR image, authentication, old successful
native cohort or runtime/GC change is needed.

CsvImport.TryAmount checks precision only after decimal parsing. Submitted values such
as1.00000000000000000000000000001 or0.010000000000000000000000000001 can therefore
become an accepted currency amount with a nonzero digit discarded. Nine of14 new
CSV cases fail on original code, including leading/trailing/parenthesized negatives,
comma decimal/grouping and EUR/JPY/KWD. Five controls preserve exact redundant-zero
inputs, original sign/group grammar and currency-specific minor units.

ReceiptParser.TryAmount has a separate exactness defect: a syntactically complete
recognized number such as7922816251426433759354395033.51 is accepted as a rounded
decimal. Four of7 new low-level cases fail originally. Three exact trailing-zero/
integer controls remain accepted by that reader while the existing full-receipt
100-billion total cap independently keeps all three oversized totals in Review with
no amount/candidates and original source text. Do not claim an originally accepted
financial suggestion from these huge low-level numbers; the higher cap already refused.

CSV now also checks the submitted fraction after insignificant trailing zeroes are
removed, against the original currency precision. Its existing parsed-value precision,
long-range, signs, separator/group, whitespace/native digits and fallback-currency
rules remain unchanged. Receipt parsing compares submitted significant fractional
length with the decimal parser's retained scale; it refuses discarded nonzero digits
and returns zero on refusal. Exact redundant zeroes remain valid. Total-size/currency/
merchant/date/exclusion policies are unchanged; there is no rounding-policy expansion,
new financial writer, format/model/migration, confidence or price inference.

## Focused verification and actual Android

Strict test compilation passes; final14 CSV+7 receipt new cases and11 directly affected
previous cases pass:32 selected, not a full suite. Only original chosen-separator/
ambiguity and complete thousands-group controls are reused after the relevant change.
Strict Windows build passes without warnings/errors. No D851 success or financial
operation is replayed.

Original retained emulator5606/API36/x64/kernel4096 remains at boot
6663f98e-6546-4809-a0bc-285f11a96674. Fresh package
pro.vafadar.zanance.csvreceiptexactreview/UID10244 is installed and normally launched
once. The immediate PID observer is too early and refuses before the check marker;
the original launch continues as26710, without restart/reinstall/relaunch. A read-only
root-readiness response confirms actual original OnboardingPage before the one
exclusive package/run/PID/shell-UID/ranchu-bound original-reader action.

That action performs14 CSV+7 receipt checks on fresh memory-only inputs. There is no
account/entry/schedule/type/location/goal/rate setup, no file import, no financial
command and no OCR/camera/input replay. All20 declared typed source components compare
equal before/after, with accounts/entries/schedules0 and source SHA256
1469478F7C20315684D98DC7CF35FF2C6F7EA7DACBE5595335B21E438D2BDCD1.
This qualifies the actual original readers, not picker/preview/import/Undo, live OCR,
all languages/corpus or complete schema/cipher/key/security acceptance.

## Artifacts, retirement and exact scope

| Artifact | SHA256 | Bytes | AOT / ELF / static16KiB LOAD |
| --- | --- | ---: | --- |
| Full shipping |65523bd63f43e88939f2dd78e4b486e88ebffc386348bdd823d81b4b40dbdbce|99278617|384 /408 /1608|
| Used private |7e411e3880543232ba0ec93ecb62159aadb508e6746b4aeebb59e5bfc6faa2ad|56729372|192 /204 /804|

Strict packages pass offline/nondebuggable/original Mono, v2/v3, CRC/ZIP and static
alignment checks. Nine actual App/Core/Data AOT ZIP members equal their own build
artifacts (six shipping/three private), not whole cross-build managed PE identity or
hardware16KiB acceptance. Used signed APKs, four input hashes/source copies and the
private source archive remain under local Zanance-D852-current.txt evidence.

Only freshUID10244 is uninstalled after samePID26710/source equality. Original package
UIDs/five OS values/locale-key presence/geometry/boot/reverse/shell are exact. No XML
capture/removal, OS write, original fault/source/provider/history/R13 read or physical
phone command occurs. Four standard Clean profiles pass; no supplementary/frozen
scope, retry/subset/alternative cleanup is used. Original D809/D833 and pending D838/
unexecuted D841 remain protected. Prepared65523bd6 is not installed on Main;20144/904
is only the dated D849 checkpoint, not new liveness.

The sole historical D850 CI response records [CI cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41993994565)
and [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41993994697).
No second query, successful job log/rerun or waiting is used. This new step still needs
its own single later CI receipt. D607 is terminal; original fault cause/remedy, whole
performance/security/A11Y/platform/provider/Store/release remain open. The overall
continuous goal stays active; these two bounded reader corrections are complete.
