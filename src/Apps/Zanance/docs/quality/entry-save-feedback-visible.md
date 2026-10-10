# Visible transaction Save failures - D-130 / AT-132

## Observed problem and scope

D-126's installed purchase correction rejected an amount below retained refunds, but its complete SaveError could
only be read after manually scrolling the long Advanced form. See ledger-write-policy.md and its retained actual
native rejection proof. The existing label now occupies the fixed growing footer before the original effect/Save
surface, outside the body viewport. Full wrapping text, native scaling and a complete spoken name are retained.
No view-model continuation, monetary parser, ledger validation, receipt-unit guard or draft semantics change.
Field errors retain D-100's field-adjacent/reveal behavior; another explicit Save clears stale general feedback.

## Windows native checks

The Debug-only entry-save-feedback route opens the actual sample purchase modal and invokes its real WinUI Save
through the Invoke pattern. It rejects a correction below the retained refund, rejects an explicit retry, and
reports an actual SQLite update failure. The exact sample Guid is the only trigger target; the trigger is removed
in finally. Reviewed editor-field JSON and complete values from every persisted table, including auditing and receipt bytes,
match before/after. A subsequent invalid amount clears the obsolete SaveError and exposes its existing field error.

All 18 normal contexts pass: en/fa/de, light/dark, 360x800, 412x892 and 1280x820. The additional German dark 360x800
process-local 200% font stress also passes. At both actual native body scroll ends, the full realized error glyphs
fit their allocation; native text is scalable/untrimmed, its spoken name is complete, the original Save is enabled
and at least 44 px, and the body retains a usable viewport. This is 57 rejected native Save invocations plus 19
subsequent field-validation invocations across 19 contexts. Original development database/sidecar/marker hashes
are restored after each run. Only the hidden application's own rendered root is captured; no desktop input/focus,
full-screen capture or OS-setting change. Real OS font conversion and screen-reader/device acceptance remain open.

Evidence: artifacts/entry-save-feedback-windows-*/{en,fa,de}-entry-save-feedback-proof.json, corresponding own-window
captures, and entry-save-feedback-windows-proof-summary.py. Layout evidence is separate from financial unit tests.

## Installed full Release checks

All six en/fa/de light/dark contexts use the actual normal signed Release on the owned emulator-5570. Each invokes
ledger rejection and explicit retry, verifies immediate complete footer text without a manual search, scrolls the
body while retaining feedback, invokes an actual exact-row SQLite failure, and checks old-feedback clearing during
input validation. Actual Cancel/discard returns to the retained detail. Native amount drafts remain intact;
complete values of all 24 persisted tables and receipt bytes/type/ownership match the prepared fixture exactly.
There are 18 rejected Saves plus six subsequent field-validation Saves. Fictitious trigger/data preparation uses
only the exact independently owned sample database. An old Debug APK is installed unlaunched solely for run-as
copying; no production fault switch or FLAG_SECURE change is introduced. Exact original database/sidecar bytes and
original English/System display choices are restored; the complete Release is restored and stopped on Home.

Evidence: artifacts/entry-save-feedback-release-native/{en,fa,de}-{light,dark}/proof.json and retained hierarchies;
restored-display-proof.json; entry-save-feedback-final-proof.json. No physical device is driven.

## Verification, artifact and limitations

Main 1,972 / App.Tests 318 pass with zero failed/skipped; no duplicated application algorithm or control fake.
The native acceptance route adds real UI coverage; the unit-suite count is unchanged. Test cleanup and final strict
Windows/Android builds pass with zero warnings/errors. Canonical Build-AndroidApk.ps1 completes the full Release.

Phone-test artifact: artifacts/android/zanance-d130-release.apk, 81,020,843 bytes. SHA-256:
`4d1e44c1a89c218dbc98532d308725472ebf46097d476ecfe2e93c7282371eb8`.
ZIP integrity, full ARM64/x86_64 assembly stores/app AOT, non-debuggable pro.vafadar.zanance, min 24/target 36,
unchanged cloud permissions and v2/v3 signature pass. Certificate SHA-256:
`92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` (local test signing, not production signing).
Evidence: entry-save-feedback-apk-proof.json and entry-save-feedback-{all-tests,tests-clean,windows-final-build,
android-build,android-release}.log.

Negative evidence is retained: initial Debug helper compilation required correcting a nonexistent start-scroll
helper and trigger DDL construction; final strict builds pass. An overlapping Windows/Android restore caused
NETSDK1005 before a native launch; the remaining matrix ran sequentially after Release completed. That failed
context is not counted. The native runner's initial off-screen Edit assumption, merged accessible amount text and
retained scrolled-detail title assumptions were corrected against existing hierarchies, resuming the same fixture
without repeating financial Saves. The Persian runner also replaces the visual-end/backspace assumption with
native Select All after checking the actual merged amount caption; an incorrect draft stops before Save. A transient adb-daemon failure during Persian fixture preparation recovered
with one read-only device inventory, exact prepared-byte verification and continuation; no server/device reset.

No schema/migration/compiled-model, SDK, permission, financial algorithm, portable preference or commercial
activation change. This delivery resolves the observed visible ledger Save feedback follow-up, not remaining
ENT-02/03/04, billing, encryption, OAuth, physical-phone/iOS or product-release acceptance. CI is a separate later
single read-only check of the pushed commit, not evidence of device or publication readiness.
