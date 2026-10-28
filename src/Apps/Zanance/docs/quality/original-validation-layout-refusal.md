# Original validation helper refuses the held unlaid target (D-623)

## Original call-site question and bounded admission

D-622 observes stock ScrollToAsync pending under an attached, unlaid native child.
Before changing any product behavior, inspect the actual EntryEditor, PlanEditor,
Occurrence and Settlement validation paths. Each already awaits the original
AndroidValidationLayout helper and checks its original visible/version/handler
lifetime afterward. Import-preview and personal-sync scrolling have different
paths; no acceptance of those pages is inferred from an editor helper.

The helper listens for a native global layout, requests layout, waits at most one
second and checks its original target's peer/attachment before returning. The new
question is whether that exact existing helper falsely accepts a target that is
still unlaid under a suppressed native parent. Do not copy its algorithm, weaken
its deadline or immediately add a global scroll timeout from a static concern.

Freeze one original helper call before any candidate. The independent project
links the exact production AndroidValidationLayout.cs; SHA256
4140ced267b1408b91194d6c17cfd223aea34a4ec2505bd7123588ab257aa12c
is unchanged before/after and equals HEAD. No production source, financial model,
runtime policy, schema, permissions or application behavior is modified.

## Actual original native observation

Use one fresh Zanance_D623_ValidationLayout AVD, API36/x64/4096,1080x1920/density420,
boot681ebf50-435f-4cce-a5d3-77a1e754f293. The package
pro.vafadar.zanance.validationlayoutreview is absent including retained data before
one new install/normal launcher. Actual PID3994 executes only the helper experiment;
Core/Data and financial bootstrap are not linked or loaded.

The strict independent Release build has zero errors/warnings. Its complete APK
SHA256c2d270a8deaede0e9eb3c3409e8bed8cc7bdb7cf66beb5fc66047a3d05220d2b,
17940122bytes,90 own AOT modules,100ELFs/390 statically16KiB-aligned LOAD segments,
CRC/offline/nondebuggable and v2/v3 verification pass. It uses the independent SDK
Android Debug certificate, not a claimed shipping owner identity. Static alignment
is not hardware16KiB, physical or shipping-package acceptance; no fresh installed-
base hash comparison is claimed beyond the exact submitted APK/install/UID chain.

Before the one helper call and at its return, the real native control is attached,
visible,window-focused,0x0,IsLaidOut=false and IsLayoutRequested=true. The original
helper returns false. Releasing the temporary native parent's suppression afterward
produces actual1080x1920 layout with IsLaidOut=true/IsLayoutRequested=false. The full
helper plus100ms release-observation interval is1116.0815ms; this is not an app
performance or ANR metric. The unarmed candidate-only positive-control flag stays
false; its result is null and no second helper call occurs.

The proposed false-positive is not reproduced by this input. Another window's
global-layout callback was not instrumented or proved during the held interval;
do not claim that broader antecedent was satisfied, that no possible false-positive
exists, or that a historical ANR is fixed. Do not repeat this passed held-target
refusal, arm a rescue input or patch shipping behavior to justify the hypothesis.
An actual original application callback/lifetime failure remains required before a
product correction. The older D-455 setup failures and D-622 stock-task outcomes
remain exactly as recorded.

## Terminal proof and exact retirement

A separate retained-result verifier checks the actual before/held states, false
helper outcome, correct later native layout, no stock scroll request, empty
financial-assembly condition and exact original source bytes. One terminal native
XML shows only the original navigation_layout and original page label; the
fixture's temporary host/content is gone. All five original settings and native
size/density match with no restoration writes. No new camera, reader, account,
seed/key, financial Save/Import/Undo/payment or prior successful cohort runs.

Hash-match and remove the849byte own result and5371byte terminal XML, preserving
local originals. Uninstall only the new UID including retained data, verify absence,
stop only this fresh AVD and prove serial/ports absent. Official AVD retirement
removes24files/5085606786 logical bytes. Standard Clean removes465 registered files/
79590720 logical bytes. The older retained AVD configuration and phone/D-607 are
untouched; existing ADB servers remain.

Supplementary deletion of the new source/build/AVD residual roots and two unused
publish files is rejected before CreateProcess with blocked by policy. The entire
combined383file/47902933byte scope remains; no subset, alternative method, escalation
or repeat deletion follows. The actually used signed APK/source/failure/result
witnesses remain. Counts are logical sizes, not host free-space measurements.

## Delivery boundary

This is one original native helper observation, not a new managed product test,
complete editor/native regression, framework fix or general stability acceptance.
No candidate, Windows/product APK build or unchanged successful test suite is run.
The shipping baseline remains D-621; all product sources stay exact. Preserve the
owner-authorized temporary Loyan phone turn until explicit return and all three
future D-607 deadlines, consumed reboot and unread source. Continue ready independent
work; TalkBack stays last and the overall delivery goal remains active.

Publish the finished evidence and perform one exact-head CI follow-up after16minutes,
consume its result and delete only that monitor on every outcome. Original private
receipts: %TEMP%/Zanance-D623-current.txt.
