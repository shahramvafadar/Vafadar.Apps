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
