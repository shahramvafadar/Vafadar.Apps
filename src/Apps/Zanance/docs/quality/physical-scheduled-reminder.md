# Physical original timed alarm and notification route (D-475)

## Missing scope and independent producer change

D-474 qualifies a native action on an expedited displayed notification; it does not qualify
original AlarmManager timed delivery. Add an explicit review_scheduled_only boolean only to the
existing independent AndroidReminders caller. Main never references this project/provider.
No production app, planner, scheduler, SDK, permission, financial schema or native engine changes.

Native permission completes before choosing the real deadline, avoiding response time consuming
it. Four-minute lookahead is rounded to the minute. The fixture's actual Schedule.ReminderTime
and days-before offset produce that deadline through original ReminderService, ReminderPlanner
and LocalNotificationScheduler. Its financial due date remains tomorrow23 October2026, unpaid
12.34EUR/Once/auto-post=false, with Checking opening1000EUR and zero ledger entries. The new marker
adds one roundtrip local timestamp; original four-line/default snooze behavior remains separate.
Validate original financial identities/fields plus reminder time/day offset. This mode returns
after validating the actual scheduled request, never invoking the old immediate Show copy.

The synthetic read-only provider keeps its first eight columns and appends pendingPlans,
scheduledAt and plannedId. These are synthetic counts/deadline/id only, not credentials, financial
rows or notification payloads. Starting/error rows have the same eleven-column shape. Old successful
snooze, application/SQLite and other business suites are retained, not replayed. No new managed cases.

## Complete current-source native package

Strict independent ARM64 Release/AOT/Offline build completes with zero warnings/errors. Package
pro.vafadar.zanance.androidreminderreview is absent before one fresh installation. APK:
E:/Zanance.ReviewBuilds/D475-reminder-scheduled/apk/pro.vafadar.zanance.androidreminderreview-Signed.apk.
Bytes53125468; SHA256d8e073e4a502790f1c8cc139901a37cae2c49ce50cb46691c0defec2ae8fc831.
It has192 AOT files, valid v2/v3 local signatures, no INTERNET/debuggable declaration and five
runtime/cipher engines equal D-471 Main. No Windows/Main rebuild, global installation or SDK change.
D-474's earlier immutable output APK is retained; only its isolated compilation cache is reused.

Original run39871b74124f4bf39b75908ae58b09a0 starts preparationPID1388. Native app-labelled Allow
belongs only to the new package. The initial provider read is still unprepared, not a failure;
same1388 later reports protected=true,financialMatch=true,pendingPlans1 and original id184973019.
The original schedule is22 October2026,22:26:00+02:00. There are no posted fixture keys before
that deadline. Original Back closes its Activity; package-scoped am kill confirms1388 absent.
No fixture is reseeded, reopened or rearmed to repair an observation deadline.

## Actual original alarm lifetime

Capture active RTC_WAKEUP Alarm a1115fc and PendingIntentRecord d0b942b, owned by the independent
package and plugin.LocalNotification.ScheduledAlarmReceiver. origWhen1792700759999 is one millisecond
below the recorded minute's epoch boundary; preserve the native conversion, without clock edits.
The original InexactAllowWhileIdle policy has window153458ms and the existing bounded one-hour
AllowedDelay. Current queue reads at20:25:09Z,20:26:05Z,20:27:04Z and20:28:09Z retain the same actual
alarm while the app PID is absent and no notification is posted. Waiting follows that live native
handle; no new alarm or provider-created process is substituted. An initial retained dump is
trimmed to its exact owned record, excluding the following unrelated alarm metadata.

The device is already charging/awake: existing stay_on_while_plugged_in15,mStayOn=true. Read only;
never change it. This is awake/charging delivery, not idle/doze or unplugged acceptance.

At22:28:33.507, native events start3318 from the real ScheduledAlarmReceiver broadcast. Capture
that origin before querying its provider. Its actual notification184973019 has creation epoch
1792700915215,155216ms after origWhen. The process-start event is about50ms after the recorded latest
alarm window; notification creation follows it by about1.7s. These events do not isolate the
dispatch instant or initialization cost, and do not establish an exact delivery guarantee. The observed
late post is accepted under the existing one-hour SDK tolerance, not the old one-minute default.

The original native queue is consumed. Receiver3318 status has windows0,protected=false,
snoozes0,pendingPlans0 and the original marker/deadline. financialMatch=false means financial
rows were deliberately unread in this no-window process, not changed. No Activity is requested
to manufacture receiver startup; no Snooze action or old successful action cohort is repeated.

## Actual notification tap and financial preservation

Bind the one native record to exact package/UID10369,id184973019, title/body and current native
row. A first shade-opening observation refuses while focus is transitional; continue that same
already-open shade. One actual content tap opens the original Due item page in the same3318.
An early destination-focus read refuses; continue the same Activity rather than retapping.

The original page shows Cold reminder fixture plan,Due Friday23 October2026,Upcoming and actual
amount12.34EUR. Its unsaved actual date defaults to today22 October; the financial due date remains
23 October. No completed/partial payment or financial editor command is invoked. Same3318 status
now has windows1,protected=true,financialMatch=true and zero snoozes/ledger entries. Original Back
returns Home1000EUR. No accepted fixture or financial write is repeated. This qualifies ordinary
unlocked notification routing only, not PIN/device-authentication gates or every reminder type.

## Restoration and limits

Uninstall only the newly installed independent fixture after completed delivery/route/financial
readback. Its notification is absent. Main stays26415 with all141 original public Home tuples,
recorded979EUR and original Main/other-independent APK hashes equal. Exact initial font/display/
accessibility values and existing charging-awake setting15 match. No Main profile creation,
financial command, private storage/key extraction, owner authentication, clock/system-setting
change, unrelated-package mutation or reset.

Twenty-six assertions validate these observations; they are not new unit cases. Four successful
scoped native captures have their remote trees hash-checked/removed immediately. Seven additional
owned Main/independent XMLs are removed after exact local/remote hash checks; local originals stay.
Finite exact-PID buffers record no ANR/crash. Shipping Main/App/APK remain D-471. General runtime/
GC/ANR/crash, precise timing, doze/battery restrictions, reboot, interrupted action, natural
low-memory, all reminder/lock/platform/spoken/iOS/provider/Store and release gates remain open.
