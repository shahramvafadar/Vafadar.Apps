# Occurrence command failures and retained drafts (D-136 / AT-138)

Full/partial payment, explicit linking, Skip/Unskip, date/amount/note changes and reopening use actual
OccurrenceCommands with the existing PlanStore writers. One gate spans native consent, writer failure feedback
and successful display publication. Rejection/cancellation never reloads, navigates or replaces the draft; ledger
validation stays inline. Unexpected failure uses the native interaction port without raw user-facing exceptions.
Retire the writable reviewed snapshot before post-commit navigation/refresh and publish its replacement only after
all load reads finish. A refresh failure explicitly says the change was saved and requires closing/reopening the
screen before another change. Explicit Reopen/Cancel captions exist in all six languages; French/Italian no longer
present two cancellation captions. No commercial activation, schema, SDK, permission, backup or device-setting change.
29 added actual application-flow cases (AT-138), App.Tests 347. Main 2,201 pass with an independently owned,
ignored Data.Tests output configuration limiting collection concurrency to one. Explicit writer-race tests retain
their concurrent operations. Two normal parallel full-suite attempts failed in unrelated Data.Tests SQLite cleanup/
migration (locked file and disposed native handle). Preserve negative logs; D-137 later completes file-scoped pool cleanup isolation.
An unsupported argument ran zero tests and a help invocation failed; neither is acceptance. Output-only test
configuration was removed; no product workaround or test execution policy was committed.
Strict Windows/Android builds and complete signed Release APK verified. Native normal Release emulator checks cover
en/fa/de, light/dark, actual error dialogs, retained drafts and all 24 complete stored tables. Six additional normal
Release cases commit an actual partial payment, deliberately break only the owned fixture read, display the saved-
but-not-refreshed message and reject replay from enabled native actions. No Windows native
command, real phone, iOS, provider or publication acceptance claim. Selected-plan generation/automation, choice
persistence/UI, commercial activation and existing external gates remain open.

## Android artifact

D:\_Projects\Vafadar.Apps\artifacts\android\zanance-d136-release.apk

- Bytes: 81508437
- SHA256: f18f7417a6d22db2ac864447477003eba165a07098b71da68b4740ef238a5a22
- Package pro.vafadar.zanance, minSdk 24, targetSdk 36, non-debuggable.
- Complete arm64-v8a/x86_64 stores/application AOT; ZIP integrity, v2/v3 signatures and existing local test certificate verified.
- Canonical Build-AndroidApk.ps1; no Fast Deployment or probe.

## Native evidence

[
  {
    "Language": "de",
    "Theme": "dark",
    "Mode": "published",
    "NormalSignedRelease": true,
    "ActualPaymentCommittedOnce": true,
    "SavedRefreshFailureNativeMessage": true,
    "WritableSnapshotRetiredBeforeFailedRead": true,
    "EnabledNativePaymentActionsCannotReplayOldDraft": true,
    "ExplicitReadFaultOnlyInOwnedFixture": true,
    "Other24CompleteRowsUnchanged": true
  },
  {
    "Language": "de",
    "Theme": "dark",
    "Mode": "settled",
    "ActualNativeFailedCommands": [
      "Occurrence_Undo"
    ],
    "ErrorShownAndDismissed": true,
    "DraftRetained": true,
    "All24RowsUnchanged": true,
    "NormalSignedRelease": true
  },
  {
    "Language": "de",
    "Theme": "light",
    "Mode": "open",
    "ActualNativeFailedCommands": [
      "Occurrence_Confirm",
      "Occurrence_PayPart",
      "Occurrence_Skip",
      "Common_Save"
    ],
    "ErrorShownAndDismissed": true,
    "DraftRetained": true,
    "All24RowsUnchanged": true,
    "NormalSignedRelease": true
  },
  {
    "Language": "de",
    "Theme": "light",
    "Mode": "published",
    "NormalSignedRelease": true,
    "ActualPaymentCommittedOnce": true,
    "SavedRefreshFailureNativeMessage": true,
    "WritableSnapshotRetiredBeforeFailedRead": true,
    "EnabledNativePaymentActionsCannotReplayOldDraft": true,
    "ExplicitReadFaultOnlyInOwnedFixture": true,
    "Other24CompleteRowsUnchanged": true
  },
  {
    "Language": "en",
    "Theme": "dark",
    "Mode": "published",
    "NormalSignedRelease": true,
    "ActualPaymentCommittedOnce": true,
    "SavedRefreshFailureNativeMessage": true,
    "WritableSnapshotRetiredBeforeFailedRead": true,
    "EnabledNativePaymentActionsCannotReplayOldDraft": true,
    "ExplicitReadFaultOnlyInOwnedFixture": true,
    "Other24CompleteRowsUnchanged": true
  },
  {
    "Language": "en",
    "Theme": "dark",
    "Mode": "settled",
    "ActualNativeFailedCommands": [
      "Occurrence_Undo"
    ],
    "ErrorShownAndDismissed": true,
    "DraftRetained": true,
    "All24RowsUnchanged": true,
    "NormalSignedRelease": true
  },
  {
    "Language": "en",
    "Theme": "light",
    "Mode": "open",
    "ActualNativeFailedCommands": [
      "Occurrence_Confirm",
      "Occurrence_PayPart",
      "Occurrence_Skip",
      "Common_Save",
      "Occurrence_Link"
    ],
    "ErrorShownAndDismissed": true,
    "DraftRetained": true,
    "All24RowsUnchanged": true,
    "NormalSignedRelease": true
  },
  {
    "Language": "en",
    "Theme": "light",
    "Mode": "published",
    "NormalSignedRelease": true,
    "ActualPaymentCommittedOnce": true,
    "SavedRefreshFailureNativeMessage": true,
    "WritableSnapshotRetiredBeforeFailedRead": true,
    "EnabledNativePaymentActionsCannotReplayOldDraft": true,
    "ExplicitReadFaultOnlyInOwnedFixture": true,
    "Other24CompleteRowsUnchanged": true
  },
  {
    "Language": "en",
    "Theme": "light",
    "Mode": "skipped",
    "ActualNativeFailedCommands": [
      "Occurrence_Undo"
    ],
    "ErrorShownAndDismissed": true,
    "DraftRetained": true,
    "All24RowsUnchanged": true,
    "NormalSignedRelease": true
  },
  {
    "Language": "fa",
    "Theme": "dark",
    "Mode": "published",
    "NormalSignedRelease": true,
    "ActualPaymentCommittedOnce": true,
    "SavedRefreshFailureNativeMessage": true,
    "WritableSnapshotRetiredBeforeFailedRead": true,
    "EnabledNativePaymentActionsCannotReplayOldDraft": true,
    "ExplicitReadFaultOnlyInOwnedFixture": true,
    "Other24CompleteRowsUnchanged": true
  },
  {
    "Language": "fa",
    "Theme": "dark",
    "Mode": "settled",
    "ActualNativeFailedCommands": [
      "Occurrence_Undo"
    ],
    "ErrorShownAndDismissed": true,
    "DraftRetained": true,
    "All24RowsUnchanged": true,
    "NormalSignedRelease": true
  },
  {
    "Language": "fa",
    "Theme": "light",
    "Mode": "open",
    "ActualNativeFailedCommands": [
      "Occurrence_Confirm",
      "Occurrence_PayPart",
      "Occurrence_Skip",
      "Common_Save"
    ],
    "ErrorShownAndDismissed": true,
    "DraftRetained": true,
    "All24RowsUnchanged": true,
    "NormalSignedRelease": true
  },
  {
    "Language": "fa",
    "Theme": "light",
    "Mode": "published",
    "NormalSignedRelease": true,
    "ActualPaymentCommittedOnce": true,
    "SavedRefreshFailureNativeMessage": true,
    "WritableSnapshotRetiredBeforeFailedRead": true,
    "EnabledNativePaymentActionsCannotReplayOldDraft": true,
    "ExplicitReadFaultOnlyInOwnedFixture": true,
    "Other24CompleteRowsUnchanged": true
  }
]

Original database bytes/sidecars restored; all 24 tables checked after English/original-theme restoration. Normal signed Release installed and stopped. Only emulator-5570 on dedicated local ADB 5038/TCP 5571; no physical device, shared-server, system setting, desktop input or secure-flag change.

## Local evidence files

- occurrence-feedback-all-tests-owned-config.log: full suite passed, real exit zero.
- occurrence-feedback-tests.log: two initial translated caption ambiguities, corrected and retained.
- occurrence-feedback-all-tests-verified.log / occurrence-feedback-all-tests-final.log: negative parallel SQLite cleanup/migration.
- occurrence-feedback-all-tests-serial.log: unsupported argument, zero tests; not acceptance.
- occurrence-feedback-windows-build-final.log / occurrence-feedback-android-build-final.log / occurrence-feedback-release.log.
- occurrence-feedback-apk-proof.json / occurrence-feedback-native/final-proof.json.

Local engineering checks do not accept real phone, iOS, provider connections, commercial activation or publication.

Native capture recovery: 46 transient unavailable hierarchy captures retained. Bounded reacquisition uses fresh native trees; unavailable captures are not acceptance and no successful financial write is repeated.
