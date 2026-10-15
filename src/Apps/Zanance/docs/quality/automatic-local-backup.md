# Automatic device-encrypted local copies (D-281 / LOC-09 / AT-281)

Owner policy approved 2026-10-15: automatic local copies use device protection; portable exports
remain separate with optional passwords. No password is remembered. Android and Windows share the
original bounded package serialization, independent per-copy device keys, profile-bound encrypted
envelopes, complete authenticated reread and existing recovery/reference-aware native retirement.
Automatic success follows file and directory synchronization and retention, not upload alone.

Daily (default), weekly and off are device preferences. Startup/resume/onboarding completion/profile
entry coalesce one due attempt, not a catch-up loop or guaranteed background job. Success timestamps
are per profile and separate from portable export. Catalogue plus exact publication validation
reject profile changes, including switching away and returning to the same path. There is no
plaintext/cloud fallback. Automatic and pre-restore cohorts each keep ten by default; cross-cohort
and retained recovery/staged references still protect their keys.

The original backup page presents independent portable/automatic status, complete six-language
help and current-profile automatic rows without sharing. Selection uses the original preview,
confirmation, authentication, safety copy, protected replacement and completion flow. Cancellation
does not write money. Device/key loss still needs a separate portable export; existing protected
OS backup/transfer exclusions remain in force. No schema, native dependency, SDK or permission changes.
Automatic-copy configuration stays disabled on iOS until its native protected host is adopted
and verified on an available Mac.

## Verification

- Complete strict Vafadar.Tests.slnf suite (SQLite/mobile/library filter): **3,590 passed, zero failed/skipped**. The final publication-identity
  regression then passed alone under strict compilation: **3,591 covered behaviors**, not a second
  full-suite run. Sixteen new library/application cases cover real serialization/crypto/SQLite,
  concurrent due checks, independent manual/profile clocks, missed intervals/clock rollback,
  disabled/unavailable work, cancellation/cloud refusal, profile drift, failed qualification and
  explicit restoration. The additional Data case rejects switch-away-and-back to the same path.
- Independent protected Windows installation: three actual native copies, retention reduced to two
  only in the fictitious review, both retained packages authenticated, pre-restore cohort preserved,
  portable timestamp and complete financial rows unchanged. Final UI continuation retains the same
  installation/copies and adds only one copy for the final file/directory synchronization path.
- Final original Windows page: **18 contexts**, en/fa/de, light/dark, 360/412/1280 widths with process-local
  200-percent font stress. Actual native interval selections (off/weekly/original), stable caption
  publication and 18 native row preview/cancel operations pass. Hidden own AppWindow only. Final
  financial fingerprint `a1f00084323125f47bc4cf0f3288df5bddc8b0da8c162172070b04efae3564f9` matches; no financial Save or reseed.
- Final signed Release Android on the owned x64 API36/4KiB emulator: native first automatic creation,
  interval selection, retained state on cold restart, six en/fa/de/light/dark changed-surface/help
  contexts and explicit original restore succeed. A temporary second profile creates no automatic
  copy before onboarding, creates its own encrypted copy after completion on this final Release,
  and retirement removes exactly that copy while the main copy still authenticates in preview.
  Original encrypted financial file remains intact;
  the replacement publishes a new protected generation and Home retains the fictitious baseline.
  This is emulator evidence, not phone/arm64/API24/16KiB/iOS or power-loss acceptance.
- Final Windows Debug and Android Release builds have zero warnings/errors. Complete installable APK:
  `artifacts/android-automatic-backup-corrected/pro.vafadar.zanance-Signed.apk`, 88,320,326 bytes,
  SHA-256 `674cbc31e39b6241eb31e312d1672ff55cef1516092b326f925f0078772cf595`. Package `pro.vafadar.zanance`, v2/v3 signature verified with one signer; both reviewed native SQLCipher payloads and embedded AOT app assemblies are present.

## Observed failures and focused correction

The initial library assertion hit xUnit2031 and was corrected to the supported Single predicate.
Initial strict Windows compilation exposed namespace/static-instance errors; final builds pass.
Native path review identified a lock-order defect: automatic creation holds the stateful backup
gate, while native retention validates the completed portable bytes. That pure bounded validation
must not reacquire the same gate; stateful create/restore remain serialized and the reentrant reader
is checked by original-source and real native retention cases.

The first Android interval change exposed a real UI loop: status publication cleared/repopulated
the live picker while its native selection callback was completing. The app stayed alive and busy,
and UI observation returned null roots; this was not counted as acceptance. Captions now publish
once only when translations change, with nested write suppression and retained selection. The final
native continuation checks actual interval changes and stable caption identity. Only affected UI,
build and APK work was repeated; the successful full suite/native crypto corpus was not replayed.

The earlier D-280 hosted runs remain historical failures ([Native](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38657262681), [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/38657262663)): Native Windows compiler qualification,
CI Windows toolchain/Android native preparation and Linux Test failed. Their causes are not claimed
fixed by this feature or local success. Check the pushed D-281 commit once after the usual delay;
hosted/native-security, real device/iOS/OEM/OAuth/Store acceptance and the full backlog remain open.
