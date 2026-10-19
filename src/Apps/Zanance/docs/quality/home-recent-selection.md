# Bounded original recent Home selection (D-383)

## Observed repeated window-thread work

GetHomeSnapshotAsync already returns the complete ledger in stable descending Date/CreatedAt order.
Home nevertheless filtered and ordered that same complete history again after RecentEntries.Clear,
on the window thread. Select the first three nonfuture records from the captured ordered snapshot on
Home's existing facts worker. Stop after the eligible prefix; an arbitrarily long future prefix still
requires its own traversal, on that worker. Preserve original object references and tie order. All
financial calculations continue using the complete unfiltered history. Original EntryPresenter,
date labels, account/category lookup, row commands and visible layout are unchanged. No schema,
compiled model, provider, permissions, security, runtime or GC policy change.

Ten actual application-source behavior cases pass, zero failures/errors/skips in0.185s: short histories,
stable ties, future prefix, bounded access to a100,000-row suffix, all-future input, original metadata,
and DateOnly extremes. The explicit App.Tests source link exercises production selection, not duplicated
logic or fake MAUI controls. Strict test compilation6.00s and Windows29.49s have0warnings/errors.
Unchanged complete Data/financial/backend/resource suites and prior successful visual matrices are not
replayed. Ten new cases is not a new full-suite total.

## Independent original page and timing boundaries

The old CapturePresentation baseline omitted RecentEntries. Add a separate original-order oracle and
compare exact recent identities, count/HasRecent, titles, MoneyText, review state and original subtitle
suffix. Keep the historical financial projection unchanged. Build expected formatting only after the
measured clock and heartbeat stop, avoiding measurement prewarming. Two initial successful x64 builds
precede that observer placement correction; neither initial binary is measured. Final independent
strict x64 build76.61s and ARM64 build148.96s have0warnings/errors. Packages have192AOT libraries each,
four byte-identical engine libraries, no network/debug/profile/native prototype or diagnostic GC mode.

- x64 SHA256:93af429ced01309cc943ccee9f1659504710bc9cb78811f429a601ef9c72bbf0.
- ARM64 SHA256:b1d5d3343ca54a002279c412b205fc9a21f70b7c181cd9d4bc4badf8c3206757.
- Core LedgerCalculator source remains77170d479a0daa655f9dd501c0295762d8053506580cbc6a186606e6f60f1cb4.

Reuse original protected x64 run25b9dab64bf849fcb24e39246d773215 and baseline539d2e32456d4109a480eb0d920b66b0.
Candidate comparisondd60d8b69e084038babaed41a7ba37db starts PID8625 and actually exits with native
SIGSEGV/status11. Preserve four completed cases. Only the unfinished tenfold fa/de cases continue in
PID8878 with identical APK/ciphertext/keys/comparison. A code-offset-only diagnostic has no matching
frames; do not assign this x64 fault the prior ARM64 bridge cause. No heap/register/raw-stack dump.

The actual failure and mixed-process timing justify one explicitly recorded current original-VM
control, rather than retrying to seek fast samples. Same caller oracle, original source, protected
run and baseline; fresh comparisondc68487021d8458ba2a1b25388bb2796, singlePID9102, six completed cases.
Control APK SHA256fdbc7f74bbf8f81a909303f2952d14e5e5acd21ba9e7bc98be4ef9b113ffca2f; strict build75.03s.
Restore candidate source exactly afterwards. Reference full-page medians971.08/992.84ms and tenfold
3065.15/3274.72ms (original/candidate) do not establish a whole-page speedup; candidate mixes processes.

The already captured public recent:Reset -> first recent:Add interval brackets the removed UI sort
plus original formatting/event overhead. It is not exact CPU profiling or a statistical benchmark.
Current x64 tenfold median24.74745->0.0594ms; reference0.8064->0.0973ms. All six paired intervals per
shape are shorter. Bounded indexed tests independently prove no older suffix traversal. This supports
removing redundant window-thread work, not the two-second full-Home objective.

## Actual Samsung continuation and retained failure

Phone run d53542fe57d6446ab52b82a757ee7f6e, original financial baselinef3d82fe68b9c43e7905866945ce5ea86,
comparison ea66c442bf5147d8b21f7f0257b04ab6. PID15678 actually exits with native SIGSEGV/status11 at
2026-10-19 13:19:31.372. Preserve original four completed cases and continue only tenfold fa/de in
PID16106 with the same package/ciphertext/keys. An observation timeout is followed on that same live
PID, without restarting. All six full original financial/native/recent packets and complete raw
schema/typed rows pass; no financial Save. Data MVID8d18afd6-3ae7-4649-a836-6c597c11ae9e;
callerf0af6dea-e492-4499-b706-4ac51a664bb1. Neither this failure nor D-377's ARM bridge fault is fixed.

Historical D-377 Samsung results are reused, not rerun. Both historical and candidate cohorts include
retained process continuations, so they cannot prove full-page performance. Their tenfold public recent
interval median66.0288->0.0863ms, reference4.7631->0.0873ms; all six per shape are shorter. Whole-page
reference1795.88->1896.18ms and tenfold5158.13->5171.24ms are expressly not an improvement claim.

## Complete production package and no-reset upgrades

Build-AndroidApk.ps1 -Offline produces the complete signed Release package,384AOT libraries across
ARM64/x64, minAPI24/target36, no INTERNET/debuggable/profileable declaration and eight byte-identical
native engine entries. Actual APK: artifacts/android-home-recent-d383/pro.vafadar.zanance-Signed.apk.
SHA256:60af0e7b02826f342efb23f0513b7015ce4c2268402cefd79694d8f8290dccf7; 94,838,517bytes.
Certificate92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
No temporary instrumentation enters production. Upgrade emulator and approved Samsung without reset;
original meaningful native Home nodes, My account/Home0 and Zanance_Test_D312_Main/Home979 respectively,
exact installed hashes and original system settings/size/density match. Ordinary accepted independent
packages remain stopped, preserving all fictitious corpora/keys. Release the coordinated phone turn.
Clean only owned build/test outputs after immutable package copies.

Artifacts/home-recent-selection-d383 preserve original sources, test XML, current control reason,
original/native-exit/retained-case receipts, stage summaries, immutable packages and upgrade proofs.
QA-06/native GC/ANR, cold/low-memory, two-second target, spoken reader, broader device/iOS/provider/Store
and release gates remain open. No full-suite rerun, generalized stability or publication acceptance.
