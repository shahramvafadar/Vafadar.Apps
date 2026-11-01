# Local reminder clock changes (D-717)

## Original observed defect

REM-07/08 and AT-37 require calendar reminders to follow device-local wall time without moving
financial calendar dates. The original SDK stores DateTimeOffset. Its pinned Android receiver
handles boot/package replacement, while the original merged manifest contains no time-zone/clock
receiver. Source absence alone was not treated as a running-app failure.

One fresh API36/x64/4096 component links the actual original LocalNotificationScheduler. It prepares
only notification7170900 for3November2026 at09:00 Europe/Berlin:09:00+01:00 and native RTC_WAKEUP
epoch1793692800000. A real OS change to America/New_York preserves that request and handle,
corresponding to03:00-05:00 rather than09:00. Java and managed zones both report New York.
The original date/id/content remain unchanged. Its original cached-death precondition is refused;
no kill occurs for that refusal and this original failure is explicitly live-process evidence.

An initial unpublished fix passes ordinary cold-receiver calendar/hour/tomorrow cases. Final review
then identifies a publication gap: SDK acceptance can precede the source snooze preference commit,
so inferring native clock kind from that mutable cache can use an old choice. That gap is found by
source review, not presented as an observed released failure. The initial simple cohort and its
package remain historical evidence; they are not substituted for final-source acceptance.

## Final product behavior and mutation boundary

The enabled, non-exported Android receiver handles protected TIMEZONE_CHANGED and TIME_SET actions.
GoAsync/background work opens neither a window nor financial storage, database/key provider or
source preparation. All native queue mutations share the scheduler's process gate.

Calendar requests preserve their wall components, id, routing, title/body, category and remaining
request properties. Resolve platform-reported nonexistent times to the first valid instant,
including non-hour gaps; select only the earlier repeated hour. Recompute the existing original
local-day allowance. Remove only an expired exact pending id without clearing notification history.

New one-hour snoozes carry their actual elapsed DateTimeOffset through original financial-source
validation. Tomorrow remains local-calendar based. The existing source preference cache optionally
retains the actual choice/instant; historical unknown choices are not invented. A normal source
refresh keeps their existing legacy local-time behavior, then marks their new native request fixed
until another normal source refresh. Complete historical-choice recovery is not claimed.

Crucially, broadcasts do not classify pending requests from those source preferences:

1. Store a derived private descriptor keyed by native notification id: exact wall ticks, exact offset
   ticks and local/elapsed binding only. No title, route, account, amount, credential or financial row
   is added. Equal UTC instants expressed with different wall/offset identity do not match.
2. Durably invalidate descriptors with a synchronous private SharedPreferences commit before SDK
   mutation. After all actual requests are accepted, atomically publish only their exact descriptors.
   Single Show/Remove uses the same invalidate-before/accept-before-publish rule under the gate.
3. A clock receiver first captures matching current descriptors, invalidates before its own native
   mutations, and publishes the accepted updated set. On interruption/refusal, absent or stale
   metadata cannot authorize reinterpretation. Normal covered source refresh can rebuild it.
4. Requests without matching metadata, including previously installed unbound requests, keep their
   native instant until ordinary source refresh. A broadcast never guesses their choice. Posted
   requests are not replayed as a fresh queue.

No EF model/migration, source amount/date, permission, network/SDK version, exact-alarm policy,
system clock or battery exception changes. The small device-local derived clock file is not a new
financial source or portable export. No global security or authentication exception is introduced.

## Selected managed verification

Twenty-eight new cases and42 affected existing cases pass. The latest App run is64/64; six unchanged
Core snooze/cache controls were already successful after their relevant source change and are retained.
The App cases are rerun only after the publication fix changes their original service/scheduler path.

Coverage includes actual Berlin/New York/Tokyo/Lord Howe offsets, supported hour/half-hour gaps, an
admitted custom twelve-hour gap, the earlier repeated hour, original local-component semantics,
normal/hour/tomorrow/unknown/duplicate classification, original real-SQLite snooze travel and repeated
hour flows with captured rows unchanged, and ten exact native-binding format/identity/refusal cases.
No accepted old financial UI command or unrelated suite is replayed.

The first run is52pass/1failed expectation: this Windows Samoa2011 zone data does not expose the
assumed skipped date (IsInvalidTime is false and round-trip retains09:00-10:00). Keep the failed
XML/log, remove that unsupported admission and test actual supported boundaries instead. This is
not a passed historical-zone qualification or an established product regression.

Preserve initial diagnostics separately: missing original ReminderPublication link in the first
private compile; wrong Data-assembly expectation in a database-free package observer; and terminal
observer assertions following an accepted uninstall. Continue from the accepted uninstall checkpoint,
refusing duplicate uninstall and observing pending/active state read-only. Historical Android Zen
uninstall logs are retained, not interpreted as active posted notifications. No cause beyond the
actual recorded observer evidence is invented.

## Final native stale-cache cohort

The final source requires a distinct fresh API36/x64/4096 cohort after the earlier completed fixture
is retired. Its source comes only from a digest-matched archive outside refused cleanup scopes;
no refused root/output is reused. This necessary source-change check is not financial replay.

Bootdf786802-7ac3-4e50-bd89-087e0b8452d9 / UID10216 / initialPID3618 prepares one normal future09:00
request and exactly two distinct original scheduler requests: elapsed hour7170901 and local tomorrow
7170902. There is no Data/SQLite library or financial database, Save/Import/payment, real action-tap
or old successful setup. The original real-SQLite service paths are covered separately above.

The diagnostic fixture deliberately swaps the two source preference choices and removes the source
instant, while keeping all three actual accepted SDK requests unchanged. This holds the stale cache
that must never classify native clock behavior. Close only the observed owned root task8 using the
supported stack command; verify cached/adj905 and use one scoped am kill. Death is proved without
force-stop, reset or rearm. A real Asia/Tokyo zone change follows.

Before any Activity/provider observation, OS am_proc_start records receiver-created PID4926 for the
actual product ReminderClockReceiver. The new process reads committed exact native descriptors:

- Normal7170900 stays3November09:00, now+09:00, expected epoch1793664000000.
- Tomorrow7170902 stays2November03:01:08.1506231, now+09:00.
- Every field of elapsed7170901 stays unchanged at04:01:08.1506231+01:00.
- The deliberately stale opposite source choices remain untouched, and no early notification is posted.
- SDK pending ids correlate through actual PendingIntentRecord/requestCode identity to exactly three
  RTC_WAKEUP handles, one per original id, with no extra pending request.

Restore only the original synthetic cache and original Europe/Berlin zone. All three request fields
and original source date/cache match the accepted pre-change state; their native handles remain unique.
No original preparation, accepted snooze command or financial operation is repeated.

Native epoch sampling remains explicit: final Tokyo0/0/+1ms and restored Berlin0/-1/0ms from exact
persisted deadlines. The original pinned SDK subtracts DateTimeOffset.UtcNow and adds separately
sampled Java currentTimeMillis. Keep actual differences; no millisecond precision or punctual future
delivery acceptance is claimed. No manual TIME_SET, every historical zone/DST dispatch, physical
travel, reboot/Doze or broad reminder acceptance follows from this finite queue check.

## Final packages and source boundary

Strict Windows and complete offline dual-ABI Release builds have zero warnings/errors. Final product:

`E:\Zanance.ReviewBuilds\D717-clock-publication-shipping-package\pro.vafadar.zanance-Signed.apk`

- SHA256:eb17f5d833fd94563b390b398eca04d6ae47fbde114a883c5c75e3c6407da852.
-99,381,151bytes;384 own AOT /408 ELF /1608 static16KiB LOAD entries; ZIP/CRC/v2/v3 pass.
- Actual permissions equal D716; protected clock receiver is present and private review provider absent.
- This is the latest prepared package, not physical Main adoption.

Final used native281bcd86 is19,464,515bytes with97AOT/107ELF/418staticLOAD, valid offline/nondebuggable/
signature/ZIP/CRC guards. Original74534083 and preliminaryebb54f2c remain limited earlier-source
observations. Source linkage is verified; whole linked-PE identity between component and full product
is not claimed. Runtime/GC policy is unchanged; no iOS or physical acceptance is repeated.

## Restoration and cleanup

Final original boot/SDK/page size/1080x1920/420density/locale/zone and five captured settings match.
Only each completed fresh fixture is uninstalled once; exact accepted checkpoints prevent replay.
No own pending/active posted notification or PendingIntent remains. Original historical Android
uninstall logs stay intact. Both owned emulator processes/consoles exit; official scoped deletion
retires50files/10,182,027,329logical bytes across two separate AVDs, not measured host free space.
No XML capture is created. Nine standard Cleans pass with7,040 registered deletion lines, not distinct files.

Two separate supplementary removal requests are rejected before process creation with `blocked by policy`:

- Initial ten exact scopes: inventory itself never runs, so file/byte counts remain unknown.
- Final seven different scopes: saved pre-request inventory is549files/159,728,901logical bytes.

Freeze both exact sets without retry/subset/alternate/move/escalation/reuse. Used signed APKs, receipts,
prior source archives outside refused scopes and all failure evidence remain. No old refusal is retried.

D716's sole response reports CI in_progress and identity/service/CodeQL success; its schedule is deleted
with no second GET. D607, its three deadlines, consumed reboot, unread post-reboot source and Main
adoption boundary remain untouched. Android completion precedes iOS. Wider financial/key/security,
stability, phone speed, provider/account/Store and deferred accessibility/release gates remain open;
the overall delivery goal is active.

## Primary source references

- [Android implicit broadcast exceptions](https://developer.android.com/develop/background-work/background-tasks/broadcasts/broadcast-exceptions).
- [Protected time-zone intent](https://developer.android.com/reference/android/content/Intent#ACTION_TIMEZONE_CHANGED).
- [Pinned original SDK receiver](https://github.com/thudugala/Plugin.LocalNotification/blob/0f2d140560da30b81c16a436a2c5b54c98db0335/Source/Plugin.LocalNotification/Platforms/Android/ScheduledAlarmReceiver.cs).
- [Pinned original SDK scheduling](https://github.com/thudugala/Plugin.LocalNotification/blob/0f2d140560da30b81c16a436a2c5b54c98db0335/Source/Plugin.LocalNotification/Platforms/Android/NotificationServiceImpl.cs).
