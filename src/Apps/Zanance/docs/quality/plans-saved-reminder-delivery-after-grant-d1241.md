# Android delivery of a saved Plans reminder after permission grant

This check follows the [Plans settings-return correction](plans-notification-settings-return-d1239.md)
and the [return without a grant](plans-notification-settings-no-change-d1240.md).
It exercises delivery from a plan saved in the original app. It does not repeat
the prior contract-reminder Doze cohort or count the grant as a delivery.

A fresh owned Android 16 x86_64 emulator ran the existing signed package
`b78fc6ad77d1a1007ff1e5fb40e89de5f4b81da58af45d64150143680f231592`.
The installed package digest matched the archived build. The fixture saved
one synthetic one-time plan with an unknown amount and automatic posting
disabled. The original editor set **0 days before** and **3:11 PM** for the
November 18 due date. The emulator clock and idle settings were left alone.

While the package permission was denied, the plan was already visible in Plans.
Android had no active Zanance notification and no scheduled Zanance reminder
alarm. The original **Turn on** action opened Android's notification settings.
After enabling notifications there and pressing Android Back, the same app
process returned to the same Plan row. Android reported
`POST_NOTIFICATIONS: granted=true`.

Android then had one `RTC_WAKEUP` for Zanance's ScheduledAlarmReceiver, with
an original trigger of 15:10:59.999 local time. The native inexact window was
2 minutes 25.971 seconds. The alarm's actual PendingIntent record reported
request code `562908779` under package UID `10216`.

The last empty sample was at 15:13:24. A separate native sample already saw
the active key at 15:13:26, and the main observer captured it at 15:13:38.
Android's active NotificationRecord had the same package, owner and ID as
the alarm. Its creation time was 15:13:26.077, 146.077 seconds after the
editor's 15:11:00 setting and close to the end of the recorded inexact
window. The private notification contained the expected generic
“A financial item needs your attention.” message. This verifies actual
delivery with Android's inexact scheduling on this emulator.

No product change was needed. The reminder was saved and enabled once.
There was no manual notification, clock change, notification tap, financial
write, or replay of the successful delivery cases. Both physical phones
were untouched and the Samsung positive transfer control remains open.
The local D1241 evidence includes the stored time and plan row,
`before-enable-android.json`, `owned-reminder-alarm.json`,
`owned-alarm-intent.json`, `near-window-end.json` and the exact
notification record in `native-terminal.json`.

The test installation and its named emulator were removed. The original
emulator settings matched at shutdown, and the shared ADB server was
preserved.
