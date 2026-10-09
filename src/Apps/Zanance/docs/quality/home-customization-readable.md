# Complete Home customization identities and targets (D-94 / AT-99)

## Corrected behavior

Customize Home used a single trailing-control row and the truncating RowTitle style. Narrow 200% renders in
D-92 showed incomplete section names. Wrapping in the original narrow title column still broke words into small
fragments. Each complete identity now occupies a full-width growing Auto row, with the unchanged reorder/visibility
controls below. All eight sections, order, defaults, translated names, RTL and profile preference semantics remain.
The Reset caption uses the existing growing semantic action and its original command. Arrow and switch targets have
local 44 px minimums, retaining palette resources and native text scaling. No financial calculation, schema,
portable preference allowlist, security, SDK, permission or provider behavior changes.

## Actual verification

- The first real row check rejected the existing 40 px reorder buttons. Increasing local minimums resolves that
  finding. The first one-row wrapping candidate was refined after its actual narrow German render still split
  names poorly. Earlier/intermediate cohorts are not counted as final acceptance.
- Final coherent Windows en/fa/de, light/dark, 360x800/412x892/1280x820 at process-local 200%, plus en/fa/de
  360x800/light at 100%: 399 owned-window renders. Real native glyph/caret geometry verifies all eight complete
  full-width identities, growing rows, 44 px controls and full Reset names. Twenty-one AT-99 proof files record
  84 native Invoke/Toggle operations: move down/up with exact persisted order, change the exact section visibility,
  then Reset to every default section. Complete Accounts/Entries/Budgets and every other preference remain equal.
  Writes occur only in the walk-through's fictitious profile; original development database/WAL/SHM files restore
  with matching hashes. This is process-local layout stress, not real OS text-scale or screen-reader acceptance.
- All 1,384 main tests pass, zero failures/skips, output cleaned; no new unit count is invented for XAML geometry.
  Final strict Windows and canonical complete Android Debug/Release builds have zero warnings/errors. The new
  Windows-only helper and caller share their platform boundary; Android Debug compilation verifies it (D-93).
- Normal signed Release runs on the independently owned API 36 x86_64 emulator. Native English hierarchy/scroll
  review finds all eight complete section identities above their switches, usable reorder targets, the complete
  Reset target and native Back to Home without changing customization. No screenshot/security flag or OS setting
  is changed. Readback compares all 24 original tables/columns/rows of the exact 10,001-entry, 100,001-entry and
  prior three-entry samples; SQLite integrity passes. The temporary Debug readback package is never launched;
  complete Release is reinstalled. Final English Home/three Transactions, System/Advanced, translated Back,
  secure window, unchanged density 420/font scale 1.0 and Home/stop pass. Cloud permission boundary is unchanged.
- APK: `artifacts/android/zanance-d94-release.apk`, 80,750,507 bytes; SHA-256
  `b7109d01315d9e2148bec2b73d55c880a4c59cacc628dadbf443444928c8bc87`. Package pro.vafadar.zanance 0.1.0/code 1, min 24/target 36, full ARM64/x86_64 assembly stores/app
  AOT, non-debuggable, ZIP integrity and v2/v3 signatures pass. Existing local debug certificate
  `92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b` provides test signing; this is no production/Store signing claim.

Independent full-field read experiments found no consistent advantage sufficient to justify replacing EF
materialization or manually maintaining its complete field/legacy semantics. Constructor identity allocation alone
is small in the Windows sample. Direct provider reading and per-read GUID/date caching were slower in the measured
warm Windows cohort; full projections varied across cohorts. Those readers, caches and probes are not app
dependencies or production changes. There is no Android cold-start improvement claim from these experiments.

A11Y-03 remains partial: remaining modal/custom controls, real OS 200% text scaling, Narrator/TalkBack/VoiceOver,
ARM64 phone and iOS acceptance remain. QA-06 cold-start/materialization/initial native-row creation/ANR and Q-02
remain open. Encryption/licence, provider, commercial and owner/Store decisions remain separate gates.
