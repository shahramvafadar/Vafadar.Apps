# Original occurrence command commit lifetime (D-399)

## Observed source boundary and complete batch

After an original payment commits, an ordinary Changed subscriber exception can escape the ledger
or plan store before OccurrenceCommands receives success. The command reports a rejected write and
never calls the original completion that retires the form. Partial payments are particularly exposed:
a later command creates another entry identity while the original reviewed occurrence stays open.
Do not replay accepted native money to demonstrate or repair this problem.

Deliver the whole foreground occurrence-command boundary: full/partial payments, linking, skipping,
date/amount/note changes, undo skip and reopening generated/manual settlements. Retain exact results
across post-commit notification failures, retire the original native command before fallible follow-up,
and keep pre-commit failures/consent unchanged. Complete focused managed and original native checks,
strict Windows/Android builds, full offline APK, original Main/OS restoration, docs and one push.
General ANR/crash/accessibility investigation remains later under D-386; automatic-posting behavior
and unrelated financial screens are outside this batch.

## Initial evidence

Actual application commands over real SQLite reproduce committed writes returned as false with no
completion. The initial ten-case run also includes missing confirmation in test setup for link/
reopen; those cancellations are not committed-defect evidence. Correct the consent setup and rerun
only this still-failing regression before changing production behavior. Existing successful tests,
Main data, Samsung and other accepted native financial cases are not replayed.

## Implemented correction and managed proof

The consent-corrected ten-case regression fails on original production behavior. Typed post-commit
publication now preserves SaveResult, void state-change acceptance and the exact generated/manual
reopening result across both data-store layers. The application calls completion even when only
notification failed, then reports saved feedback under the same busy gate. SQL/validation/consent
refusals are unchanged. Follow-through failure keeps its existing saved-refresh message; notification
failure after completed follow-through uses the new six-language saved-notification message.

Fifty original application cases pass:21 newly added cases and29 existing command cases. New coverage
includes all foreground action branches and both observer layers where applicable, manual unlink,
native-alert overlap/failure priority and post-commit cancellation with all six translations.70 related
SQLite occurrence-policy cases pass, including actual-file/access retirement, financial relationships,
generated deletion journals and complete rollback. Earlier passing subsets are not extra distinct
cases. No complete suite, money algorithm, entity/model/migration or backup-format change is claimed.

## Actual Windows native commands and dialog layers

Independent run29b990121bb94490920ab9be7a94d600/PID100512 builds with zero warnings/errors and completes
all eight original VM/page/command paths. A controlled original subscriber refuses after commit;
the old snapshot retires before saved feedback. Partial400 is shown with actual remaining600,
full1234 settles, link retains the original950 row, skip/change/unskip write no money, generated
reopen deletes its original payment, and manual reopen retains950 while unlinking it. Busy remains
through the actual native consent/saved alert. Each financial command is invoked once.

After those accepted actions,18 en/fa/de light/dark360/412/1280 native message views check only the
original alert adapter. CaptureWindowAsync retains root frames and separate popup layers; the18
actual popup1 dialog images are inspected through the retained image index/contact sheet. Root
frames alone are not message evidence. Base account/ledger/settings rows remain unchanged; only
the separate fictitious stores change. Preserve complete.json and all accepted/native receipts.

## Actual Release/AOT Android and observer continuation

Run42b4d0488c854dc9a672e77cd6a39de7, comparison09a945a883e947a7aa933c692f8a9702 uses the same live
PID5191 throughout. The first host observer accepts no input because native dialogs have title-named
windows; repair only its focused-activity/owned-widget check. A one-button native alert uses button2,
while a positive two-answer confirmation uses button1. Preserve the initial own-window hierarchy
and do not weaken the exact package/message/caption checks. The same first saved alert is continued.

Partial and full finish before a host CP1252 receipt write refuses a direction-marked link message.
Its zero-byte intent is retained as an unsubmitted encoding refusal; no third answer was sent.
Resume UTF-8 receipt handling from the two completed answers and continue that same pending consent.
No process, financial command, fixture or accepted answer is restarted.17 unique invocation intents/
native answers qualify eight original commands and six read-only translated message scenes.
The final original report contains eight retired accepted outcomes, preserved expected amounts/
relationships and no financial replay. This is an isolated plain-SQLite fixture, not Main encryption,
real-phone or genuine process-death idempotency acceptance. Initial observer failures remain retained.

## Complete candidate and original restoration

Final Windows Release and complete offline Android builds have zero warnings/errors. APK:
artifacts/android-occurrence-commit-d399/pro.vafadar.zanance-Signed.apk,
94981877 bytes, SHA256 8ecd2ac13f8ef622fb41b6851ee0c40503c8fcfb2dfc76f5e02608938d119ec9.
ARM64/x64 are present, INTERNET/debuggable absent, and existing development-certificate v2/v3
signatures verify. Only this complete candidate upgrades Main once without reset. Original Home0/
meaningful controls, four OS settings, size/density and installed hash match; the terminal independent
caller is stopped. MainPID7829 remains alive; no financial Main or system-setting write and no phone input occur.
Test output is cleaned with zero warnings/errors. Documentation and final commit/push accompany
this complete correction. Hosted exact-head CI, broader phone/iOS/financial histories, durable
crash/duplicate delivery, performance, general stability/A11Y and external release gates remain open.
