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
