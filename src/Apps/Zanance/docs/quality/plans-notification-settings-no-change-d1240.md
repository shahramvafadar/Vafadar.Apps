# Plans notification warning after returning without permission changes

This check covered the return without a grant, following the
[settings-return fix](plans-notification-settings-return-d1239.md). The
existing signed Android package (SHA-256
`b78fc6ad77d1a1007ff1e5fb40e89de5f4b81da58af45d64150143680f231592`) ran
on a fresh owned Android 16 x86_64 emulator with a 360 × 800 display at
density 160. The tested Zanance source was unchanged from `5fab5c16`
through the reviewed head `9f03a631`.

One synthetic account with zero opening balance and one one-time plan
prepared the case. The plan had an unknown amount and automatic posting
disabled. The emulator package permission was prepared as denied with
`USER_FIXED` to reach Android settings directly from the app's explanation.

1. Plans **Turn on** and **Open settings** opened the real Android
   notification settings for Zanance. **All Zanance notifications** was
   off. Android Back returned directly to Plans with the warning, the same
   due row and an enabled button. The process and denied permission
   were unchanged.
2. Pressing that button again reopened the app's explanation and the
   same real Android settings screen. After returning without changing the
   switch, Plans still showed the warning, same row and enabled button in
   the same process. Android still reported the identical denied permission
   and flags.
3. Opening the saved plan through **All plans** showed that its active
   reminder was still set to three days before the due date at 9:00 AM.
   Automatic posting remained off. No second plan save was used.

The check passed and required no product changes. Neither permission grants
nor financial transactions were performed. The prior grant and delivery tests
were not repeated. Neither physical phone was contacted, and the Samsung
positive transfer control remains open.

The initial boot observation timed out and an early onboarding UI dump could
not be parsed. The same emulator and installed process continued without a
relaunch or repeated input. Local evidence is retained in the D1240
artifact directory, including both Settings captures, both return receipts
and the retained-reminder capture.

The test package and its emulator were removed after verification. The
emulator settings matched the initial snapshot before shutdown, and the
shared ADB server was preserved.

## Android reminder channel disabled while app notifications are allowed

Android 16 can continue granting app notifications while its General reminder
channel is blocked. A fresh emulator ran the signed build from `f1888f03` with
one synthetic plan with an unknown amount, a three-day reminder and automatic
posting off. An actual reminder created the default channel. To reach that
event, the guest clock was temporarily advanced into the reminder window, then
the original clock and automatic-time setting were restored before changing
the channel.

Disabling only General set its importance to 0 with the app permission still
granted. The original Plans page returned to the same row with no warning.
The first candidate showed the warning but Turn on entered the Plans failure
state. The correction leaves the Android context's cached notification
manager available to the notification service.

The final signed offline APK
(`ee104d8e7021e96ebc9c080e723fa7f57523cdbf7886c5dc0c4e67f27241ff9f`)
was installed on the same UID without resetting the app. Plans showed the
warning and retained the original row. Turn on and Open settings opened the
actual General channel. Re-enabling it restored importance 3 while the app
permission remained granted. Back returned to the same Plans row and process,
with the warning gone. There was one plan save and no financial posting.
The guest's original display geometry was restored and the disposable test
installation and emulator were retired. Neither physical phone was used.

## The app withdraws its pending alarms when delivery is blocked

Previously, a refresh returned without reconciling already pending alarms
while Android blocked delivery. A new source-linked test reproduced the stale
queue after one plan's reminder was disabled. A refresh now withdraws pending
alarms while delivery is blocked. Once delivery is allowed and the app
refreshes, the queue is rebuilt from the current plan and profile settings.
No reminder rule or snooze is deleted by withdrawing the native queue. The
76 selected reminder and publication cases passed, including the current
source rebuild and the absence of a new snooze while blocked. The Windows
build passed without warnings or errors.

In an Android 16 x86-64 emulator, the original signed package
`045e3d29` scheduled a period-review alarm for December 1 at 09:00,
request ID 1219868079. After the Android package permission and the saved
review reminder were both turned off, the same pending native alarm remained.

The corrected signed Offline Release APK
`c0c41fb4fb981bedef355785c4b4a691d17227c9835654368f0d7cd4a0448223`
was installed without resetting UID 10216. After opening the app with
permission still denied, Android's active alarm list was empty for Zanance.
The first observer included historical cancellations in its count. Restricting
it to live scheduled alarms confirmed the cancellation on the existing app
process. After the permission was granted, the queue was still empty while
the user was in Android Settings and again after returning to Zanance. The
original review setting remained off and Transactions was empty.

The disposable emulator and package were retired after restoring display
geometry. The clock and physical phones were untouched. Evidence is under
`artifacts/android-disabled-reminder-queue`.

## Delivery becomes blocked during a reminder batch

Two source-linked tests reproduced a waiting occurrence alarm and a new snooze alarm
after delivery became blocked, both during preparation and during publication. The
reminder service now checks current delivery permission at both boundaries. A block
withdraws the waiting batch and returns before writing the new snooze or publishing
budget alerts. The original source validation still guards publication and cache
commit. All 78 selected reminder flow and publication tests passed, checking that the
saved financial rows are preserved. The tests model the timing at the scheduler
boundary. The earlier Android settings and delivery checks were not repeated.
Strict Windows and Android Release builds passed. The signed offline APK SHA256 is
f217d45fd8557a0de20deeaec6a4999301548e5640bb5d9ed6d530fb7bbaae92.
