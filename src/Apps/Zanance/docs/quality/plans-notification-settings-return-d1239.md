# Plans notification permission after returning from Android settings

The [earlier denial check](plans-denied-notification-banner-d1238.md) established
that Plans shows a warning when a saved reminder is active and notifications
are denied. This regression exercised the warning's **Turn on** action on a
fresh owned Android 16 x86_64 emulator at 360 × 800 and density 160.

The fixture had one synthetic account with a zero opening balance and one
one-time plan with its reminder enabled, an unknown amount and automatic
posting disabled. The package permission was prepared as denied with
`USER_FIXED` to reach the settings fallback without replaying the earlier
permission-dialog test. From the original Plans page, **Turn on** showed the
app's explanation and **Open settings** opened Android's notification settings
for Zanance.

On the original signed APK, enabling **All Zanance notifications** in that
screen made Android report `POST_NOTIFICATIONS: granted=true`. After Android
Back, the same Plans process and due row remained, but the warning and its
button were still present.

Opening settings previously finished the action before Android returned to
Zanance. The Android settings action now awaits the original activity's
resumption. It releases the wait if the activity is destroyed. After resumption,
the existing permission helper reads the actual scheduled-notification
permission and requests a reminder refresh if it is enabled. Plans publishes
the result through its existing current-page and source checks.

The corrected signed APK (SHA-256
`b78fc6ad77d1a1007ff1e5fb40e89de5f4b81da58af45d64150143680f231592`)
replaced the original package on the same emulator without resetting app
data or changing the package UID. Only its notification permission was reset
to prepare the failed case again. After enabling the app's notifications
through Android Settings and pressing Back, the same corrected process
returned to Plans with the same due row, no warning and no Turn on button.
Android still reported the permission granted. The saved plan details showed
its reminder three days before the due date at 9:00 AM and automatic posting
off. The initial metadata save was the only plan save. No transaction or
prior successful notification-delivery case was replayed.

Strict Windows build and Android publish passed without warnings or errors.
The offline Android package passed dual-ABI, v2/v3 signature, ZIP CRC and
static 16 KiB ELF and ZIP alignment checks. The first launch's UI observer
failed while the original process was active. The same launch was inspected
and continued without reinstalling or relaunching it. The Android runtime
permission dialog, other entry points and physical OEM transfer were outside
this regression. Neither phone was contacted and the Samsung positive
control remains open.

The retained D1239 local evidence includes `baseline-result.json`,
`corrected-return-result.json`, `corrected-retained-reminder.xml`,
`native-terminal.json` and the two build receipts.

The test package and its isolated emulator were removed after verification.
The emulator's settings matched the initial snapshot before shutdown. The
shared ADB server was preserved, and standard Windows and Android build
cleanup passed.

## Android 7 App info and external notification changes

On API 24, Android exposed Zanance's notification control through App info.
After the system block changed outside the app, the original Plans page
continued to show the previous state. Leaving and re-entering Plans made
the same saved reminder show the warning. The Android foreground return now
reads the permission for the still-current Plans view, without loading its
plan list again. A pending explicit Turn on action retains its own result.

The first corrected package displayed the warning on return from the system
block. Its Turn on action still dismissed the warning without opening App
info. Android reported the package's importance as NONE. Since a request
can report success when no Android runtime permission applies, reminder
permission now comes from a fresh status read after the request.

The final signed Offline Release APK `44bfa6bb` was installed over the same
Android 7 x86-64 fixture with the package UID, data path and first-install
timestamp preserved. With the system still blocked, Plans displayed Turn on.
Turn on and Open settings reached Zanance's App info. Android's own notification
screen showed Block all enabled. Turning that setting off and pressing Back
returned directly to the same Plans page with its warning cleared.

The original plan still had its November 27 due date and the reminder for
November 24 at 09:00. Its pending alarm still used request code `1127156057`
and UID `10062`; Android reported a 1 ms difference from the first timestamp.
The observer initially expected a separate App info screen after Back and
continued from the returned Plans view. The one synthetic plan save was
preserved through both upgrades. Transactions remained empty.

Two focused permission cases passed with the system state and request result
disagreeing in either direction. Strict Windows and signed Android Release
builds passed. The disposable package and emulator were retired after
restoring display geometry and deleting the hash-checked capture. Evidence is
under `artifacts/api24-plans-settings-return`.

### Notification status on the open Settings form

On a separate Android 7 emulator, the original Settings form stayed open
after Android's App info blocked notifications. Android reported importance
NONE, but the form did not show Turn on. Foreground return now reads and
updates only the original form's notification status. Reloading the settings
snapshot and concurrent preference changes keep their existing ownership.

Signed Offline Release APK `8cfc0aeb` upgraded the same synthetic fixture
with its package UID, data directory and install timestamp preserved. After
an external block and Back, Turn on appeared in the same open Settings form.
The original button reopened Android App info. Clearing Block all and
returning removed the warning. The Show names and amounts setting retained
its original value throughout. The observer initially expected a short
Settings caption on the More tab and continued using the actual combined
caption. Strict Windows and signed Android Release builds passed. Display
geometry was restored and the disposable installation and emulator were
retired. Evidence is under `artifacts/api24-settings-notification-resume`.
