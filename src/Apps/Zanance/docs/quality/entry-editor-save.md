# Original editor Save and receipt handoff (D-425)

Date: 2026-10-20. This is one bounded editor/receipt delivery under D-386. General Android
ANR/crash/accessibility, whole stale-edit/aggregate-overlap/Undo/detail publication and external
platform/provider/release acceptance remain separate.

## Corrected boundary

Previously SaveEntriesAsync could commit and throw from Changed before the editor set saved=true.
The following unreviewed AddAttachmentAsync used the then-current context and the mutable editor id.
Now EntryEditorSaveSession owns a single original quick-reference/file/rights review. The editor
captures its raw draft/revision and original entry before financial confirmation. New queries or
departure retire pending callbacks; a later callback cannot navigate or publish into the new form.
Submitted entries, removed identities and document bytes are detached before persistence awaits.

SaveEditorEntriesAsync refuses another configured file before financial queries and validates the
original quick references under the existing ledger writer. It reuses ledger validation, account
activity and correction rules. Before commit it captures the exact saved entry and attachment
metadata. Immediately after commit it consumes the original acceptance, before notifying Changed.
The separate reviewed document writer rechecks source, file, content, current correction rights and
pending owner. Invalid/unavailable document storage preserves accepted money; no Save resubmission.
A document commit with failed notification stays accepted. An aggregate removed by the confirmed
replacement gets no guessed destination. Existing attachment-failure wording remains unchanged.
No model/schema/property/migration/compiled-model, permission, SDK, portable-format or layout change.

## Focused actual-source cases

EntryEditorSaveFlowTests compiles the production EntryEditorSaveSession and uses real isolated
SQLite.10 original cases and2 additional boundary cases pass, with strict builds0 warnings/errors.
Twelve distinct cases, not twelve complete suites; successful D-424 and unrelated suites are not
replayed. Cases cover detached inputs/bytes, consumed duplicate submission, financial and document
postcommit observer failures, departed owner and changed saved source, invalid document, actual
save-time retirement/rollback and uncommitted retry, invalid ledger/no attachment, another file
with the same imported ids before or after commit, commercial-policy activation after commit and
a second pending Save. Test output is cleaned with the exact App.Tests project after completion.

## Original Windows form

Use an independent non-input desktop and distinct application identity. Initial source
3e99db7ce37841bd91a2dce9a126f0d8 seeds one D425 fictitious cash account in original-editor.db.
Initial observation fails before Save because the form contains multiple scroll surfaces. Later
observations retain the original file/account and make no ledger Save: the fixed footer is outside
the scrolling form, and publishing a query before constructing a Picker page does not match routed
construction. The retained pre-page construction has a null account/disabled48px Save; its native
account-item observation also fails. These are recorded negative observers, not successful commands.
No account seed or accepted financial sequence is repeated to repair them. Their failed native
process shutdowns are preserved; general Windows stability is not claimed.

Final41f401fb267f48c4a6ea6dcb0c60381a follows actual routed construction: create/push the page, then
await its real query. The original account and receipt values publish, with enabled native Save
365.33x48. UI Automation invokes it once. One expense1234 and exact four-byte metadata-only document
persist despite deliberate Changed failures after both commits. Save stays consumed, navigation
returns, base-store accounts/entries are unchanged, and the host exits0. The app-only image is
visually reviewed. Those four bytes are not a valid PDF or PDF/OCR acceptance. Input desktop remains
Default before/after. No owner desktop input or full-screen capture.

## Android package

The complete signed offline Release APK is artifacts/android-entry-save-d425/pro.vafadar.zanance-Signed.apk,
96083871 bytes, SHA2566e3051b6861f70e7359d31f6281c5d7c2908d13759149fa483e9564ba5794470.
Permanent package pro.vafadar.zanance, nondebuggable, no INTERNET, ZIP integrity and v2/v3 signature
verified. Native cipher hashes remain equal to D-424/D-421. This is the local test signature, not
store signing or release acceptance.

Private original evidence: artifacts/entry-save-d425 and the exact store-drafts runs above.

## Normal installed Android sequence

Dedicated Zanance_Storage_Review/emulator-5570, no physical-phone input or system-setting changes.
Upgrade the complete signed APK once without reset,15210->18105. An early pidof observation returns
no process before startup settles; continue the already launched/focused18105, without another
install or launch. Original Main0/My account and the one-profile public set are retained.

Create only D425_ReceiptSave and its D425_Cash account, then use the actual Home receipt action and
one native document picker. Select the exact new owned Download folder and D425-original.pdf,
13881 bytes, SHA2566f6dcffa10e283897f71d7892733bb2632986810346153aa78f728b2d86a6919.
The source is the retained fictitious D-324 PDF. The original pending recognition completes without
restarting or reopening the picker. The real expense form publishes24.50EUR,17October2026 and
D425_Cash, with the explicit pending-receipt notice. One Food tap does not expose a selected semantic
flag; accept its category only from the later stored public transaction detail, never retap to repair
an observer. Invoke the original Save exactly once. The public row/detail show expense24.50,Food,
D425_Cash,17October2026 and the complete D425-original.pdf/13KB attachment. The source fixture's
library trial watermark is retained as payee: this is not merchant-recognition or real-corpus acceptance.

Home remains0. This is consistent with the receipt's historical17October date preceding the
setup account's default opening day; LedgerCalculator intentionally excludes pre-opening
transactions from its balance. The native opening-date field was not separately qualified here. Preserve the original accepted date/amount instead of editing/replaying money to change
that readout. Remove only the exact hash-matched temporary input and its newly owned empty folder.
One terminal cold18105->21193 preserves the single public transaction and13KB attachment without the
input file. An early pidof observer is corrected read-only on the same already launched process;
no second cold, Save, picker, seed, document delete or ledger Undo is performed. Public metadata is
not private database/blob equality evidence. No OS-cache or URI-grant revocation claim.

Restore original Main/My account/0.00EUR and its empty current-month history. Verify Main is current,
then retire only the inactive D425_ReceiptSave profile through its two original native confirmations.
Continue the old two-row pending list read-only until the original one-profile set is published;
never repeat deletion. FinalPID21193, original size/density/font/accessibility values and installed
APK hash all match. android-final-restoration.json records RestorationPending=false. No test fixture
remains active; no physical phone, owner settings, credentials, private Main database or keys touched.

These observations qualify the changed original editor/document handoff and one cold local receipt
lifecycle. They do not qualify native injected navigation/metadata failures on Android, every query/
edit/overlap/Undo variant, real receipt merchants/camera/OCR corpus, physical hardware, general
ANR/crash/accessibility, Mac/iOS, real providers/store accounts or release. Hosted exact-head CI is
checked separately once after push. Continue the next ready original entry/detail boundary without
replaying these accepted sequences or unrelated successful tests.

## Physical saved-attachment continuation (D-473)

The missing Samsung Save/cold/stored-reread boundary now passes on unchanged D-471. It uses the
retained original D-324 English PDF once in a fresh profile, with opening before the receipt date,
then one Save and cold stored attachment read after input deletion. The reread is cancelled,
never resaved. [Bounded original evidence](physical-receipt-save-reread.md) does not accept real
merchant/corpus/camera/all variants or broader platform/release gates. Old successful cases stay retained.
