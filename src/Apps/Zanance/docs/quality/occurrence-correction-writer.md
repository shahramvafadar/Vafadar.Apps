# Retained occurrence corrections and atomic linking (D-133 / AT-135)

## Boundary

Seven baseline real SQLite regressions reproduced six retained writes bypassing membership and explicit Link
committing entry markers before a failed occurrence-state insert. Bind Skip/Unskip/change, repair and history-free
deletion to the actual file's cached rights and SQLite writer. Check deletion history in that same writer.
Retained corrections need no selected account or paid host, but require exact shared membership.

Explicit Link rereads actual plan rule/slice and payment kind/accounts/transfer destination/partial/link markers
under the same writer. A changed relationship or another occurrence's payment is rejected, without overwriting
it. Entry markers and state commit together; original actual amount/date/audit metadata/payee/note/tags/receipt
bytes remain unchanged. Existing matching links are idempotent; no second ledger entry or changed partial total.
SQL failure or cached facts retired after entry/state/deletion SQL roll back all complete rows, with no Changed.

39 added cases: six membership rejections, two state insert/update failures, twelve Free/expired-host retained
operations, full metadata/idempotence, nine changed relationships, transfer destination, five post-SQL retirement/
retry paths, idempotent repair/history deletion, wrong file and two independent providers competing for one entry.
All rejected paths compare the complete 24 tables. Main suite: 2,141 passed, zero failures/skips; App.Tests 318.
Strict Windows/Android and canonical complete Release have zero errors/warnings. Test cleanup is ordered after
actual completion. Initial literal-SQL/xUnit analyzer rejections were corrected before test execution;
the failing baseline is retained separately.

## Installed normal app evidence

Normal signed Release, emulator-5570 only: six actual explicit Link confirmations, English/Persian/German in both
light and dark. Independently owned sample fixtures contain a 1,000-minor-unit planned bill and a 950-minor-unit
actual manual payment with retained tags, note, payee and fictitious receipt. Real occurrence suggestion opens the
existing native confirmation. After explicit approval, verify that exact existing entry's markers, one settled
state, zero partial paid total, unchanged complete actual financial metadata and every other column across 24
tables/receipt bytes. No second entry. Native checks exercise unrestricted app delivery; enabled policy rejection
and SQL/access rollback are separate actual SQLite evidence, not claimed native failure-feedback acceptance.

Shared-port ADB failures interrupted Persian light/dark setup before Link confirmation. The first case resumed
after Start-server; the second required a dedicated local server on port 5038, excluding all USB devices and
connecting only to the owned emulator's verified local TCP 5571/qemu transport. Never stop the shared server.
Resume the already prepared case from its actual current Settings hierarchy; no successful Link is repeated.
Retain both interruptions and the initial dedicated-server discovery miss as negative infrastructure evidence,
not app acceptance or a product defect. Only the independently created server was stopped after final restoration.

Restore exact original owned sample files/sidecars and English/System preferences; final full Release stays
installed and stopped. No physical phone, device settings, desktop input, security-flag removal or full-screen
capture. No Windows layout change or native Windows Link claim.

## Package and unfinished paths

APK: artifacts/android/zanance-d133-release.apk; 81,045,419 bytes;
SHA-256: 71c225ea664b1059b865563f3239b2562c11727a5c252fc950420a163a7761dc.
Package pro.vafadar.zanance, min 24/target 36, complete arm64-v8a/x86_64 assembly stores/App AOT, non-debuggable,
archive integrity and v2/v3 signature verified. Local test certificate: 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b.
No production signing/release acceptance claim. Proof: artifacts/occurrence-writer-apk-proof.json;
native: artifacts/occurrence-writer-native/final-proof.json. Logs: occurrence-writer-all-tests.log,
occurrence-writer-clean.log, occurrence-writer-windows-build.log, occurrence-writer-android-build.log and
occurrence-writer-release.log. Baseline: occurrence-writer-before-tests.log. The initial analyzer rejection
is retained in the review history; it is not passing runtime evidence.

Current commercial registration remains inactive. New/full/partial settlement still has entry-first recovery;
generated-entry reopening, selected-plan generation/automation, choice persistence/UI and existing occurrence
command failure feedback remain unfinished. Other ENT-02/03/04, physical device/iOS/OAuth/encryption/billing and
owner publication gates remain open. No schema, SDK, permission, portable paid fact or caption/layout change.
