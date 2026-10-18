# D-353 complete Home template publication

## Observed defect and correction

The retained D-352 Windows run305e15c9991f45eaa3dac2bdacf5ad92 reports COM0x8000FFFF from
BindableLayout.ClearChildren / DisconnectHandlers during HomeViewModel.QuickTemplates.Clear after
returning from a child page. Root retirement protection already exists; adding another retirement
check would not address this live-page reload. The original list clears and recreates even unchanged
native controls. A new independent original-source regression run158dd775ef43475388258f16e4f53c1d
confirms two rows before/after but RetainedOriginalNativeRows=false. This deterministic failure does
not claim every reload reproduces the intermittent COM exception.

Materialize all fresh QuickTemplate records first, then publish with the existing SnapshotCollection
ReplaceAll. Preserve source order/identity, icon/category/theme values, translated complete description,
reviewed enablement and original command parameters. Equal records still rebind fresh objects.
Do not catch/suppress native errors, fork MAUI, skip retirement checks or change financial navigation.
There is no XAML/layout, schema, dependency, permission, financial rule or security change.

The pinned [MAUI10.0.110 BindableLayout source](https://github.com/dotnet/maui/blob/10.0.110/src/Controls/src/Core/BindableLayout/BindableLayout.cs)
reuses same-template rows for complete Reset publication; clearing forces teardown. Real count reduction
still needs legitimate removal, so qualification includes shrinking, empty and restored lists.

## Windows evidence

Final-source runf6adc4d6344242f9b726ef3b72825fb5 completes six en/fa/de xLight/Dark contexts at412px
on an independent private desktop. Each real Home reload retains original MAUI rows and native platform
views. Presentation-only transitions2/1/0/2/0/2 rebind fresh reordered records, captions, full semantic
names, original command references/parameters and disabled targets. Original store reload restores its
real definitions. Accounts, entries, settings and templates remain equal within each context; no
financial command is invoked. Six own-window images are visually reviewed. Strict build0warnings/errors.
An initial diagnostic filename collision causes a compile refusal; the pre-existing review is restored
unchanged and the new driver has a distinct filename. No successful old checks are replayed for that error.

## Scope

This eliminates unnecessary native teardown on template reload; it is not proof that all WinUI COM,
Android GC/ANR, cold-start or low-memory defects are fixed. Windows is a development check, not the
deferred Windows product. Samsung, OS reader/font acceptance, iOS and public release remain separate.
Unchanged D-351 Core39-case and D-352 page matrices/full unit suite are not repeated. Unit counts stay
unchanged; these new checks exercise actual original native controls rather than duplicating algorithms.

## Retained Android startup failure

The first caller process25941 is killed before its review phase begins. Android's authoritative exit
record reports reason6 ANR, failed to complete startup, at2026-10-18 18:00:13.768. There is no successful
case receipt to replay and no evidence that this template correction fixes startup. Keep its exact
exit-info and empty process-scoped stage log. Continue the same run/comparison only after confirming
the process is dead; do not interpret observer PID refusal as an app result or recreate fixture data.

## Final build and package

Ordinary main Windows strict build succeeds with zero warnings/errors in22.05s. The complete main
Release offline APK is artifacts/android-home-templates-d353/pro.vafadar.zanance-Signed.apk,
95,264,671 bytes, SHA2563302aa8f1e5c8a6d44cdf8839395fc2531ceb829747fa84af26b211abd03cd01.
The verified certificate SHA256 is92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
Package identity remains pro.vafadar.zanance, minSDK24/target36, ARM64+x64 with384 AOT images, no
Internet permission or debuggable flag. Both raw SQLCipher libraries remain byte-equal to D-352.
The independent x64 caller is separately signed and verified, with192 AOT images and unchanged
SQLCipher bytes; it is not the product handoff.

## Android component evidence

Original run9168a68dd8594b3d93432c43cb566629 / comparison31bf11fc1cb14861a612712126d24631
continues in process26174 after the confirmed startup death; no fixture data is reset or reseeded.
The original Release/AOT caller completes all six en/fa/de xLight/Dark cases. Same-count store reload
retains actual native row objects. Every2/1/0/2/0/2 transition checks fresh reordered caption text on
Android TextView, exact original command/parameter, disabled target and complete semantic description.
The original final reload restores two definitions; all accounts, entries, settings and templates
remain equal. Actual Activity focus is required. No financial command or OS setting is changed.

The first result omits Comparison: the strict reader correctly refuses it. Retain that reader refusal
and the original unmodified packet in artifacts/home-templates-d353-android-original-result.json,
decoded from complete nonconflicting chunks bound to exact PID26174, phase and run. It contains all
six successful cases. Add explicit Comparison to future reports and refuse both earlier unsuffixed
and current suffixed completed packets, so no successful native cases are replayed for a report-field
correction. A separate strict caller compile checks that small reporting-only change. Production
source and native-case outcomes are unchanged. This is not a second completed native run.

The reporting-only caller build succeeds with zero warnings/errors in99.65s. No native UI cases are
replayed. Installed independent APK bytes are read back from its public package path and match
edb990f6d602f9117552b0034e2b9691a2deacbe631522049b76a9e9744471ef exactly.

The verified main APK upgrades the existing owned emulator installation without clearing data.
Main process26612 reaches original English Home/My account with the complete original zero-EUR
MoneyText packet and normal actions. An initial null native tree is retained as observation uncertainty;
the same live process later supplies Home. The first exact ASCII amount assertion is corrected for
existing Unicode isolation marks/NBSP using the already captured tree, not another scenario. No Save
or navigation input is issued. Only the owned XML matching its captured SHA256 is removed.
