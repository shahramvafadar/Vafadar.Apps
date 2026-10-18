# Isolated six-language store drafts (D-338)

## Delivered scope

Eight current native Windows development screenshots in each of en/fa/de/es/fr/it cover Home,
transactions, new expense, plans, budget, reports, accounts and dark Home. All data is fictitious.
Daily account, Rent and Coffee are seeded user content, not untranslated interface resources.
Reports show the current period overview, replacing the earlier category-chart caption.

All 48 files are 596 x 1189 truecolor 24-bit RGB PNGs without alpha. The encoder copies every original
opaque RGB pixel without resizing, retouching, adding frames or changing values. A second decoder
compared every exported RGB byte against its native source; dimensions, alpha and all hashes matched.
The public `../store/screenshots/capture-manifest.json` records source/image/RGB SHA256 and independent
capture identity. These meet the basic Play screenshot envelope but not the recommended 1080 x 1920
featured portrait resolution. They are development drafts, not Android/iOS screenshots or submission approval.

## Isolation and reproduction

Use `eng/scripts/Run-StoreDrafts.ps1`, not the older general snapshot runner. A fresh GUID-based
ApplicationId isolates private development storage. The runner verifies the compiled manifest and
starts only its own executable on `WinSta0\ZananceStore_<run>`, never switching the input desktop.
Before composing shared services or SQLite, the Debug Windows app checks package, exact output,
actual thread desktop and empty private storage. It never enumerates, moves or resets Main data.
Fictitious seeding occurs behind onboarding before Home publication. Theme switches await ShellRebuilt;
page publication, including nested budget Actions, must finish without failed frames or native popups.

Observation expiry retains the original process and evidence. Inspect that same PID, never restart
solely because a wait expired. `-RunId <run> -UseExistingBuild` is only for a passed independent build
that has not launched a process; it refuses a run with an existing process receipt.

```powershell
./eng/scripts/Run-StoreDrafts.ps1
./eng/tests/StoreDraftSafety.Tests.ps1 -RunId <completed-run>
./eng/scripts/Export-StoreDrafts.ps1 -RunId <completed-run>
```

Review all frames and exact decoded RGB before copying only the 48 known image names and manifest
into the repository. Export creates a separate owned staging directory and refuses existing output.
No script uploads images. Restore the ordinary Windows development build identity afterwards, then
run `StoreDraftSafety.Tests.ps1 -RunId <completed-run> -MainIdentityOnly`.

## Actual evidence and retained failures

Original selected run `13b0e0648c0c40159fba817e8c199973`, package
`pro.vafadar.zanance.storepreview.13b0e0648c0c`, PID3224, completed 48 native frames on its verified
non-input desktop. The original empty-installation receipt and unchanged seeded financial rows remain
local. Four native cases refused identity/output/desktop/nonempty storage before MauiBuilder:
PIDs16784/83768/85692/14968. After ordinary Windows rebuild, Main PID63636 also refused draft identity
before shared services. All five exited themselves; no cleanup kill was used. Input desktop remained
Default before/after guard tests.

Visual inspection rejected the English budget loading frame: generic SnapshotLoadState did not cover
its nested Actions.IsLoading. The tool now awaits actual budget action publication. Only that frame
was recaptured from the same existing fictitious database, PID30296, with separate completion and
unchanged-row receipts. The original loading image/manifest remain local; 47 successful frames were
retained. The public manifest records the single SourceOverride. Explicit repair after rebuilding
the same independent identity uses:

```powershell
./eng/scripts/Repair-StoreBudgetDraft.ps1 -RunId <completed-run>
./eng/scripts/Export-StoreDrafts.ps1 -RunId <completed-run> -RepairEnglishBudget
```

Repair requires a completed independent run and terminal original process, never reseeds, retains
original receipts and refuses a second repair attempt. Continue the same live handle after timeout.
Earlier failed runs remain local: Home publication before seeding raced its snapshot guard; a later
theme switch inspected retired Home before ShellRebuilt. A complete run without separate desktop
was not selected. Encoder assembly-reference errors were corrected without rerendering passed scenes.
Generic Debug error output now appends instead of erasing earlier observed failures.

Strict Android Debug build passed without warnings/errors (65.50s); independent Windows repair build
(28.31s) and restored ordinary Windows build (23.05s) also passed without warnings/errors. Four
PowerShell scripts parsed. No unchanged financial/OCR/crypto/backend suite or Release APK was rebuilt.
Shipping code, signed Android handoff and managed test counts remain D-336.

## Remaining gates

These drafts do not qualify mobile rendering, actual OS scaling/readers, accounts, OAuth, purchases,
legal acceptance, featured resolution or publication. QA-06 startup ANR/performance remains open.
Physical Android qualification precedes Mac/iOS; Windows commercial delivery stays deferred.
No owner data, credentials, account access or external upload was used.

Sources: [Play preview assets](https://support.google.com/googleplay/android-developer/answer/9866151?hl=en-en),
[CreateDesktopW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createdesktopw),
[STARTUPINFO desktop](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-startupinfow).
