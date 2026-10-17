# Physical own-format CSV and durable Undo (D-317)

## Installation, authority and retained fixture

The approved physical Samsung SM-A176B remains on the complete offline signed D-315 main APK, source
commit d025f66437226b1ff40ad8cf872181a79f1e0ada, APK SHA-256
f6602c3cb75fc7b7d1fd929981d7d1959888b99098242ff410e134bba18fcab3. No build, reinstall, reset,
new dependency, OS setting, credential or personal record change belongs to this batch. The other project
explicitly reserved its input until Zanance released the phone. Only observed original app/native picker
controls were driven; screenshot security was retained.

Reuse D316_Fictitious_Work and D316_Fictitious_Account, not a replacement scenario. Baseline is its actual
two earlier Food plan payments of 4.00 and 6.00, balance 990.00, budget spent 10.00/left 40.00, goal balance
990.00 and future forecast 970.00. Main remains separate at 979.00.

A 663-byte UTF-8 BOM CSV was created locally using the documented current own-format columns and fixed
fictitious entry IDs. This is an own-format consumer fixture, not an export produced by this app or a real
bank specimen. Fixture SHA-256: d9b950f3818613835671eb88863de606028cdc7a6b3e90934cc63d9eff740e19. One row is Expense 12.34, Food, with two exact note lines
and D317 tag. The other is Income 2.34 with a protected formula-like title. Both use ISO October 17, 2026,
EUR and the existing named account. Retain the host file/IDs under artifacts/phone-import-d317; never
regenerate them merely to retry an observation.

## Actual original native flow

1. Open Import & export in the existing fictitious profile. Choose CSV file opens the actual Android
   DocumentsUI picker. Cancel returns without an accepted file or import; no financial command is sent.
2. Place only the uniquely named project-owned CSV in Downloads. The first Recent search does not see
   the ADB-created file before its own media metadata is indexed. Scan only that exact file, then select
   its observed exact native row. No unrelated document names/content are retained on the host: picker
   observations redact unknown text and keep only control metadata and the exact fictitious name.
3. The original own-format preview shows New: 2, already imported: 0, invalid: 0, and explicit Import
   transactions (2). Scroll to the actual preview before invoking that original action once. Outcome is
   Imported: 2, Skipped: 0, with one past-import batch for two transactions.
4. Home changes 990.00 -> 980.00. Original ledger shows -12.34 expense, +2.34 income and the original
   plan payments, with day total -20.00. The formula-like title is displayed literally as
   =D317_Fictitious_Income. Open the original expense detail: its two note lines remain one multiline
   value, original Food/account/date/amount remain correct, and D317 tag is present. No edit is submitted.
5. Select exactly the same byte-identical file through the original picker again. Preview now shows
   New: 0, already imported (skipped): 2, invalid: 0. The original Import transactions (0) native button
   is explicitly disabled. No second import command or extra financial write is invoked.
6. Force-stop only Zanance after the completed import, and reopen the same installation/files. Actual
   PID changes 29131 -> 3027. Home still shows 980.00. Open original Import & export; Past imports
   retains the two-transaction batch and its Undo import action even though the in-memory preview is gone.
7. Invoke Undo import and inspect its explicit confirmation: earlier entries stay, this batch's entries
   are removed and later edits/dependencies require review. Confirm once through the original button.
   The original success message is shown. Its generic mention of restored aggregate amounts is not
   evidence that an aggregate was present in this particular fixture.
8. Home returns to 990.00. Ledger contains exactly the original two D316_Fictitious_Plan payment targets,
   amounts -6.00/-4.00 and day total -10.00; neither imported title remains. Budget shows original
   spent 10.00/limit 50.00/left 40.00, goal balance 990.00 and future forecast 970.00. No plan payment,
   allocation or extra account is created by the undo or duplicate preview.
9. Return through ordinary profile actions to Main. Home again shows 979.00 and Zanance_Test_D312_Main.
   Preserve both profiles and their existing keys. Verify the exact external CSV still matches its
   original SHA-256 before removing only that owned file and the owned temporary native hierarchy files.
   Host fixture and derived receipts remain. Release the exclusive phone turn after terminal assertions.

## Observer continuity and exact limits

The first picker observation ran before DocumentsUI became foreground; input was refused. The same
picker was observed again, not relaunched. One preview-caption wait and one return-form wait expired
because their retained scroll viewport did not expose the requested caption. Exact foreground/target
checks refused follow-on input; the same process/file/preview was scrolled into view. No financial Save
was repeated or data reset. These are host observation limits, not accepted product failures or fixes.

Local derived receipt: artifacts/phone-import-d317/receipt.json, with original CSV/hash, before/after cold
PIDs, native counts/disabled state, same-profile financial packets and exact-owned-file cleanup assertion.
No actual key or owner record is exported. Raw picker hierarchy text is not retained on the host; unrelated
application UI, document names and hardware identifiers are absent from committed evidence.

This proves bounded physical consumer evidence for own-format ID deduplication (AT-52), native file-picker
cancellation (bounded AT-54), multiline/literal formula-like import text (bounded AT-55) and durable ordinary
batch Undo. It does not prove a production-export/own-import roundtrip, real bank presets, generic ambiguous
mapping, transfer/refund/attachment CSV, aggregate overlap/link decisions or unsafe-Undo conflict matrices.
AT-79 aggregate phone acceptance remains distinct. Native displayed-state checks are not byte-for-byte
raw database equality, store/OAuth/provider, actual authentication, notifications, TalkBack/OS scaling,
malformed/large-file limits, full cold/ANR timing, other Android API/page-size matrices, Mac/iOS or release.

Production source/APK and their successful strict build/tests remain D-315; no unrelated successful suite,
crypto/backend/build or package qualification is replayed and no new whole-suite count is claimed. The
canonical remaining-work backlog and development goal stay open.
