# Plans notification banner after Android permission denial

The [earlier minimum-platform review](minimum-lifetime-and-current-main-d1232.md)
observed denied permission without an established active reminder plan. This
check exercised the original Plans page with that prerequisite in place. The
[separate Doze delivery result](contract-notification-doze-d1237.md) did not
cover this page.

The fresh owned emulator ran Android API 36, x86_64, with 4,096-byte pages and a
360 × 800 display at density 160. The installed signed APK was the retained
D1230 package, SHA-256
`5699adea9718e5f6a3cac3701e6e05667ceba4de6500a95f672835f5f32cfad6`.
The Plans and Reminders source directories were identical from its source
revision `1c80815a` through the reviewed head `067c45db`. The check qualifies
this banner on the retained build.

1. Empty Plans showed “No plans yet” without the warning or the “Turn on” button.
2. The original editor created a synthetic one-time plan with an unknown amount.
   Automatic posting was disabled. Enabling its Reminder switch opened the
   actual Android permission dialog. Choosing “Don’t allow” left the Reminder
   switch checked. The package manager reported `POST_NOTIFICATIONS` with
   `granted=false` and `USER_SET`.
3. After one plan save, the native Due & overdue view displayed
   “Notifications are off. Due items are still listed under Plans.” The warning
   and the enabled “Turn on” button were fully within the viewport and their
   native bounds did not overlap. The same plan remained visible below the banner
   as due today with an unknown amount. Android still reported the permission
   denied for the same package.

The checked denial path passed. No product change was needed. The recovery
button was observed without activating it. No financial transaction was
entered or posted, and prior successful app checks were not replayed. No
commands targeted either physical phone. The Samsung transfer positive control
remains open.

The fresh installation was removed and the owned AVD was retired. Emulator
settings matched the initial snapshot before shutdown and the original ADB
server remained running. Local evidence in the D1238 artifact directory includes
`plans-empty-control.xml`, `android-permission-prompt.xml`,
`editor-after-denial.xml`, `plans-eligible-denied.xml`, `native-terminal.json`
and `public-reader-retirement-proof.json`.
