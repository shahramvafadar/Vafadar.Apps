# D-352 complete reimbursement-page loading and virtualization

## Final behavior

The original page read/materialized its ledger on the caller, notified once per row and created a
BindableLayout control tree for every open expense. D-351 fixed the shared aggregation; this delivery
addresses the separate page costs. Read complete accounts/categories/entries and calculate open
reimbursements on a worker. Prepare current translated rows and every currency total before publishing
one SnapshotCollection Reset through the existing SnapshotLoadState. A failed read retains the prior
snapshot under the cover; concurrent requests share the pending task. Loading, failure and native retry
reuse existing six-language captions. Covered and retired row objects cannot navigate.

Use one bounded CollectionView for results. Its introduction and totals scroll with the list. Growing
original identity/date and the unchanged real navigation target occupy one area; the full original
MoneyText has a separate AmountReadout with existing scalable RowMoneyFigure/RefundText styling.
The navigation surface does not cover horizontal scrolling. Empty content uses a mutually exclusive
sibling ScrollView with its own introduction/card, under the same readiness gate. Never nest the list
inside a ScrollView. No financial rule/field, provider/key/recovery, schema/compiled model, dependency,
permission or commercial-policy change is made. No fake MAUI controls or copied financial algorithms
are used. The original single-entry/Core authority supplies the financial oracle.

## Why the empty flow changed

The initial CollectionView.Header plus EmptyView prototype passes bounds/visibility assertions but
its actual Windows images show overlapping introduction and empty text. Those receipts are not visual
acceptance. Preserve them and withdraw the prototype. Put the empty card and introduction in one
ordinary vertical flow in a separate viewport; require their actual native coordinates not to overlap.
The ready parent now covers both alternatives, so effective visibility must include all ancestors.
This actual source change justifies one final combined matrix; unrelated successful suites do not rerun.

## Windows final-source evidence

Run b6c6d975ae0e416c83f9b4a49903a4ff completes18 combined en/fa/de xLight/Dark x360/412/1280 contexts
at process-local200% font stress. Each checks4,096 presentation rows with one Reset, original first/last
native captions/targets, both independent monetary scroll ends, effective loading cover, covered/retired
command refusal, explicit synthetic read failure and real native Retry. Empty presentation then checks
the real separate viewport, complete caption, actual vertical visibility and non-overlapping intro/text.
Accounts/entries/settings remain identical. The36 loaded/empty own-window images are visually reviewed.
These are independent private-desktop development checks, not Windows product, OS scale or Narrator
acceptance. No owner-desktop focus, input or full-screen capture is taken.

Earlier evidence is retained, not silently promoted: run509b7a4c3212471f8b6873d19d5b8ad8 completes12
English/Persian contexts but its first German360 last-row request does not realize the intended target.
Use the original native Scroll pattern and reacquire variable-height virtualized boundaries. German-only
run305e15c9991f45eaa3dac2bdacf5ad92 completes six page contexts, then an unrelated Home reload reports
COM0x8000FFFF through BindableLayout/DisconnectHandlers. Retain that root failure; it is not an overall
successful run. The final page receipt no longer requests an unrelated Home reload after completion.

Image review also reveals that horizontal bounds do not prove vertical visibility. Run93c36a1bdd0140fd99d3f720124c1db6
checks18 actual visible positive contexts; run511900509749414a97e2a54ea39d3122 checks18 empty bounds,
but the latter images expose the prototype overlap. These earlier checks are superseded by the final
combined matrix above, which explicitly refuses that overlap. Synthetic empty presentation is rendering
coverage, not genuine empty-store initialization. No financial UI command is activated.

## Android evidence and retained inputs

Use the independent offline Release/AOT/trimmed x64/API36 caller and fictitious run
dcae101b3bcd464583a9cfab808aa085. Initial comparisond803adf2b60448f38262b4507a618d46 onPID23698 creates
4,096 reimbursable expenses and4,096 partial refunds, then qualifies original ordering, coalesced loads,
covered/retired command refusal and native retry after real context-factory failure. A factory wrapper
refuses presentation-thread construction:60 worker attempts include six deliberate refusals (54 actual
contexts). Only5-8 readouts realize at either boundary in six en/fa/de xLight/Dark contexts. Complete
stored account/entry/settings objects remain equal. Preparation is not application latency.

Comparison28f88d30e942416c92ff1a0e0649d887 onPID24253 reuses that fixture without migration/reseed and
checks only missing vertical visibility, native accessible titles, monetary scroll ends and injected
empty rendering in six contexts (18 worker reads). Its old EmptyView assertions are not proof against
the Windows overlap subsequently found in images. Comparison40ae443707cb40308d0b9222af7d82fe adds
explicit actual VM title/amount checks for all4,096 rows and the32,768.00EUR total before any visual
stress overrides. Its first attempt onPID24519 refuses before cases when the old Main ANR dialog owns
focus. Retain that real refusal. Selecting the observed own Wait keeps MainPID23366, and its authorized
non-clearing APK upgrade retires the old process/dialog. The already terminal independent attempt is
then continued onPID24844 with the same fixture/comparison; all six remaining value contexts pass.
No failed or passed financial inputs are reseeded.

The final reimbursement-page-fixed mode reuses only that completed fixture. It combines original
values, actual visible rectangles, real worker/failure/retry checks and the separate empty viewport's
non-overlapping native geometry. While a real dialog covers it, preserve the same process and await
actual focus; never accept covered controls. Successful case receipts are CreateNew and bound to the
actual assembly MVID and complete fictitious input hash so a later continuation does not replay them.
A timeout is observation uncertainty, not authority to reset the process or data.

## Build failures and limits

The initial compile rejects unsupported FontAttributes on AmountReadout; use its existing CaptionStyle.
A native caller local-name collision is corrected after its failed compile. A broad APK engine selector
also counts the managed provider AOT images; restrict byte comparison to the two exact libsqlcipher.so
files. Do not count failed checks as passes. The intermediate installed APK/old empty prototype is
superseded by the corrected final candidate. Native engine bytes and security policy are unchanged.

No unchanged Core, crypto, OCR, backup, OS-font, reader or full unit suite is replayed. Managed counts
are unchanged; these new assertions execute the original app sources and native controls/factory.
Whole-Home/two-second latency, cold/low-memory/ANR, physical Android, OS readers, iOS and external release
acceptance remain open. Bounded virtualization/worker observations do not establish general runtime
stability or every monetary/device scenario.

## Final Android combined result and corrected package

Comparison a8bac2807d5b4f41b8db97a425c30b57 completes onPID25129, with actual caller MVID
677bd136-c3ff-4f53-ae46-45a767e60e26 and all six cases completed in that process. All original4,096
row titles/amounts/order and the32,768.00EUR total match the fixture/shared Core authority before
presentation stress. Each case passes first/last real native visibility, complete accessible targets,
independent monetary endpoints, covered/coalesced reads, actual worker factory failure/native retry,
retired refusal and the corrected separate empty flow's actual non-overlap. Realized readouts are
6/8 for English/German and5/7 for Persian. All stored account/entry/settings objects remain identical.
The60 worker attempts include54 successful contexts and six intentional refusals. RetainedCases=0:
no case replay/reuse branch is claimed as exercised by this final run. Source/input-bound immutable
receipts exist for future continuation; they are independent evidence, not financial journals.
Startup-wait17:20:22.024 becomes startup-ready17:21:40.058. Preserve that observed startup cost;
this is not a cold-start or ANR fix. The corrected final Windows ordinary identity build also passes
with zero warnings/errors in24.65s; the combined private review build passes in46.58s.

Corrected complete offline signed APK:
artifacts/android-reimbursement-d352-fixed/pro.vafadar.zanance-Signed.apk,94,822,133bytes,
SHA256d9831dafb2b555ff8e0d984739c7d633f1ece5b2320a95341b6135fe2d9436e5.
Signature verification passes with the existing development certificateSHA256
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
Permanent packagepro.vafadar.zanance retains ARM64/x64,384 AOT images, min24/target36 and no INTERNET
permission or debuggable flag. Both exact libsqlcipher.so files equal D-351 byte-for-byte. Main's
strict complete Release build has no warnings/errors. This replaces the intermediate overlapping
prototype; development signing does not establish Store signing or product release.

The corrected Main APK upgrades the existing emulator installation with install-r, without clear,
uninstall or reset. The immediate first PID observation is empty; preserve that launched process,
not another startup. Actual MainPID25356 subsequently presents its original English Home, My account,
Recorded balance0.00EUR and original Expense/Income/Transfer/Receipt/navigation in the focused native
window. No financial Save occurs. The owned temporary XML is deleted only after its SHA256 matches
the saved capture. TalkBack is observed absent before the ordinary tree capture; no system setting
or notification permission is changed. After its completed receipt, only the independent caller is
stopped. No unrelated package, device, account, credential or owner data is changed.
