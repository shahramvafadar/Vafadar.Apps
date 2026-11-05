# Retained transaction query recovery (D-855)

## Completed behavior

D854 safely covers a failed retained filter, but the filters themselves were bound
to Loading.IsReady. An invalid query therefore disabled its own correction. D855
allows only read-only filters while the same completed source remains owned by the
active page. Results, Add and financial actions retain the original readiness cover.
The original pending-task owner now covers retained presentation as well as reads;
presentation recovery supplies a completed no-read delegate. Current page, publication,
query, display, lifetime token and original resources are revalidated around presentation.
No query algorithm, Core money policy, model, migration, writer, runtime or GC changes.
Original feedback/accepted-operation receipts remain retained after successful correction.

Six new selected host cases pass on .NET10.0.14/X64: invalid-to-valid recovery without
another read, repeated failure, uninitialized refusal, pending original read refusal,
reentrant task coalescing and covered readiness notifications. The first test build
refuses an obsolete synchronous Assert.Throws task overload (CS0619/xUnit2014);
correct awaited assertions compile and pass. This is not six observed baseline product
failures or a full-suite run. D854 successful Core/host/native operations are not replayed.

## Original Android input and recovery

Use the retained original5606/API36/x64/kernel4096, with unchanged boot/public OS state.
Only a fresh private package pro.vafadar.zanance.transactionqueryrecoveryreview is installed
and normally launched once: UID10246/PID28558. Private copies add passive read/presentation
counters and exception-type observation; the actual TransactionsPage.BindingContext owns
the observation. One new GBP Checking opening0 account and three distinct fictitious
database rows are prepared once: Income MaxValue-10, Income18, Expense18. Only the two
Income titles contain probe. This is actual database preparation, not zero data writes;
no application financial Save/Import/Undo, authentication or photography occurs.

Full20 typed source SHA256 remains
DF66B9B6B33B067BDCE88E0675E798CB084B3A6B6E366B262E2FFC14C14B5677.
The original All read produces three rows and Readytrue. The first private query action
refuses before any input because its observer incorrectly requires the SearchView
container.Enabled flag. That false flag does not describe its actual editable child.
The consumed action is preserved and never retried or deleted. Owned native XML shows
the original search_src_text child enabled, focusable and visible. One normal tap focuses
that child; one input of probe and one actual enabled clear-query button tap complete
the remaining flow without a build, reset, source read or forced control enablement.

| Original state | Source reads | Presentations | Results / Add | Preserved evidence |
| --- | ---: | ---: | --- | --- |
| Initial All |1|1|Visible / enabled|Three original rows; completed source |
| Query probe |1|2|Covered / disabled|OverflowException; query retained; filters remain available |
| Clear query |1|3|Visible / enabled|Same group/row references and full source; feedback retained |

Failed error TextView has bounds and visible bounds [84,1170,996,1214], zero ellipsis,
and the original caption "The action could not be completed. Please try again."
Successful correction hides that error without clearing its model-owned feedback.
Private NativeEdits remains0 and FeedbackPreserved latch remainsfalse: those latches
belong to the refused private input path. Independent actual query/state comparison
proves the external native inputs and feedback equality; do not fabricate those latches
as passing. OriginalRootRestored is false; the ready fresh package is retired directly
after no pending presentation and full source equality. The private root-restore action
is not invoked because it depends on the unused private input counter.
Source reads1 means original Transactions VM reads; separate read-only typed-source
observer snapshots are not counted as VM reads or claimed to be no SQLite access.

## Packages, preservation and remaining gates

Strict selected-test, Windows, full offline shipping and private Android builds pass
without warnings/errors. Full shipping abf072cde0a43f44c577a3754709b13e66d26191b25c940472f9decae9a9e70b,
99286809bytes,384AOT/408ELF/1608staticLOAD is prepared, not installed on Main.
Private5006a20d943b4f0c9ae83dbca85892033d079bff286dc276c907f8c8b566b9cc,
56774428bytes,192AOT/204ELF/804staticLOAD is the actual used package. Both pass
v2/v3 signature, CRC, ZIP alignment, offline/nondebuggable and original Mono checks.
Nine actual own-artifact App/Core/Data AOT objects equal their APK members. Source
inputs/private source archive and signed used packages remain in local evidence.
No whole-PE identity, protected-screen glyph/pixel proof or broad platform acceptance.

Only freshUID10246 and three exact hash-matched remote XML180130bytes retire.
Local raw XML/evidence remain. Original emulator package UIDs, five OS settings,
locale-key presence, geometry, boot, reverse and shell UID equal preflight exactly.
Four standard Cleans pass; no supplemental deletion or frozen-scope access occurs.
No phone/R13/original fault record, original retained source/provider/history, runtime,
security-setting or other-project action. Main26327/655 is the dated D853 checkpoint,
not a new liveness observation. D854 failed cost matrix stays closed: no retuning,
successful-suite replay, resampling or new performance acceptance. D838 decision,
original fault, whole performance, security, accessibility and external/platform gates
remain separate; overall delivery remains active. CI is checked only once after push.
