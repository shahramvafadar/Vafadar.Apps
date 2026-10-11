# Actual scoped Plan notification delivery - D-140 / AT-142

## Implemented boundary

Filter real Plan and contract reminder delivery through the actual opened file's work snapshot before planner and
group caps. Keep all original schedules/states for calculations. Contract delivery requires its independent paid
feature. Current release commercial registration stays inactive, without invented selections or customer grants.

Versioned native ReturningData contains a SHA256 identifier of the full actual database path and at most 30 unique
original plan/date members. This opaque local identity is not authorization and contains no raw path. Contract links
retain one plan identity. No new database schema, SDK, permission or portable setting.

Snooze/resume rebuild from current rule/slice, open state, reminder toggle, current explicit choice and privacy/
display settings. Original group members can shrink; no later guessed member is added. Resolve original dates even
when effective dates move beyond the range-window bound. Preserve snooze deadline and actual due dates; post no
money. Retire unscoped legacy caches because file/member identities cannot be established. Existing historical
navigation formats remain supported. After unlocking, new taps check the currently opened file and stored plans,
never switching profiles silently. Validate cached rights/file before and after native publication; retire stale
pending queues and membership-denied delivery. Reserve the snooze marker in hashed occurrence ids, preventing
the repeated notification from retaining the original deadline.

## Automated and build evidence

48 new cases: 16 Core / 32 actual application SQLite flows, AT-142. Compile actual ReminderService, scheduler port
and AppLinkRouter, with real isolated SQLite/localization; no duplicated reminder algorithm or fake MAUI controls.
Check exact complete rows in all 24 tables. Main normal parallel suite 2,330 / App.Tests 380 pass, zero failures/
skips (68.028 s). Strict final Windows 23.09 s / Android 38.75 s: zero warnings/errors. Test cleanup succeeds.

Initial fixture attempts used the plan's existing three-day reminder default, so a tomorrow-once fixture had no
future notification; initial settings publication was also missing from the no-write baseline. Explicit fixture
defaults and established initial settings now precede comparisons. Later fixture SQL used lowercase Guid literals
and an invented rule column; actual tracked model mutations replace those setup errors. Keep these failures as
negative evidence. The first full suite failed one existing legacy-link expectation; its shared queue/goal/review
assertions remain, now parsing the final scoped route. No regression assertions were relaxed or removed.

## Retained local evidence

- plan-reminder-tests.log: first complete suite and legacy route expectation failure.
- plan-reminder-final-tests.log: earlier complete parallel success.
- plan-reminder-clean.log / plan-reminder-windows.log / plan-reminder-android.log: cleanup/strict builds.
- plan-reminder-delay-release.log / plan-reminder-apk-proof.json: six-case adapter APK and package/signature.
- plan-reminder-delay-windows.log / plan-reminder-delay-android.log: earlier strict adapter builds.
- plan-reminder-final-label-tests.log: earlier 2,329-case normal parallel suite.
- plan-reminder-retirement-full-tests.log / plan-reminder-retirement-clean.log: final 2,330-case suite and cleanup.
- plan-reminder-retirement-windows.log / plan-reminder-retirement-android.log: final strict builds.
- plan-reminder-native: normal signed Release native actions and independently owned fictitious profile data.

Choice UI/persistence, other selected resource bindings and commercial activation remain open. No enabled-commercial
native, Windows native layout, physical-phone, iOS, provider, performance or publication acceptance is inferred
from these tests/builds. Final normal Release native and APK observations are recorded below.

## Observed native caption defect and final adapter

Initial English native delivery/snooze succeeds. The first Persian case has Persian notification text but English
action captions; no Persian snooze is accepted. An attempted cache inspection after that failed action finds no
snooze, which is negative evidence. Resume that same case after the source change rather than accepting English
captions as a workaround. Keep the original first APK/proof and failed hierarchies as local evidence.

Pinned package 14.1.2 repository commit 0f2d140560da30b81c16a436a2c5b54c98db0335 confirms Android appends registered
categories and uses the first matching instance: [upstream implementation](https://github.com/thudugala/Plugin.LocalNotification/blob/0f2d140560da30b81c16a436a2c5b54c98db0335/Source/Plugin.LocalNotification/Platforms/Android/NotificationServiceImpl.cs#L786).
Keep that original category instance and replace its complete action list on live display changes. Android
registers once; Apple retains its existing category refresh call. Receiver initialization registers current
translated captions without database work. Three new en/fa/de actual application cases check this initialization.
At this earlier caption stage, main 2,329 / App.Tests 379 pass, no failures/skips (64.074 s); strict Windows
21.17 s / Android 37.52 s have zero warnings/errors. These label logs supersede the earlier 2,326-case baseline.
The later retirement guard below supplies the final 2,330-case baseline.

## Inexact delivery rejection observed during the final native check

The own-emulator alarm observation places the native wakeup more than one minute after the fictitious 06:09
request. No notification appears, and no action is accepted. The pinned library defaults AllowedDelay to one
minute and rejects older deliveries in ScheduledAlarmReceiver. This is negative evidence, not a passed caption
check. Android permits normal inexact delivery within an hour without battery-saving restrictions; retain
InexactAllowWhileIdle and configure a bounded one-hour AllowedDelay. No exact-alarm permission, clock, system
setting or financial due date changes.

Sources: [pinned schedule options](https://github.com/thudugala/Plugin.LocalNotification/blob/0f2d140560da30b81c16a436a2c5b54c98db0335/Source/Plugin.LocalNotification.Core/Models/AndroidOption/AndroidScheduleOptions.cs#L25),
[pinned receiver](https://github.com/thudugala/Plugin.LocalNotification/blob/0f2d140560da30b81c16a436a2c5b54c98db0335/Source/Plugin.LocalNotification/Platforms/Android/ScheduledAlarmReceiver.cs#L94),
[Android inexact alarm timing](https://developer.android.com/develop/background-work/services/alarms#deliver-an-alarm-after-a-specific-time).

## Final normal Release observation: Persian live captions and late delivery

The final native inexact request is due at 06:25, with a platform delivery window ending near 06:27:48. Its
actual notification is visible at 06:27:57 after an English/Persian live round trip. Generic Persian body and both
Persian action captions are complete. Invoke the real one-hour action; the stored snooze contains exactly the
two original plan/date members and the actual file scope. Every column/value in all 24 prepared tables matches.
This exceeds the earlier one-minute rejection window and confirms the final adapter accepts the observed ordinary
late delivery. Remaining language/theme observations and final original-data restoration are recorded next.

Six-case adapter Release before the last retirement guard: artifacts/android/zanance-d140-release.apk, 81,098,667 bytes, SHA256
813e69f6388c4e10f4083c8ae2c15a88a1491d1da50c06aa8e9d0a8597aac2d5. Package pro.vafadar.zanance,
min API 24 / target 36, non-debuggable, complete ARM64/x64 assembly stores and application AOT. ZIP integrity and
v2/v3 signature pass with the existing local test certificate
92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. This is a phone-test package, not store publication.

The second independent group also delivers under the app dark theme with complete Persian body/actions; its real
one-hour action succeeds. The remaining fixed batch checks de/light, de/dark, en/light and en/dark, then verifies
all six scoped caches, complete financial rows and exact original-data/display restoration before delivery is closed.

## Known retirement before publication

A new actual SQLite flow first publishes normal work, then changes cached choices during the next read's
pre-publication validation. The old implementation fails Assert.Empty: its earlier pending queue remains.
Reuse one cancellation guard before and after native publication. The focused regression passes; all financial
rows remain complete and unchanged. The first focused invocation used an unsupported MTP option with this
assembly's in-process runner; retain that separate runner failure, then use its documented -method switch.
Final full normal parallel suite: 2,330 passed, none failed/skipped (68.028 s), App.Tests 380; test cleanup succeeds.

## Current final APK after the retirement regression

The complete final signed Release is artifacts/android/zanance-d140-final-release.apk, 81,537,109 bytes,
SHA256 09a1b06731eac114defa7fc73d26ac686fa24404a987e3b282fc7c862897ab17. The same package, min/target APIs,
full ARM64/x64 stores/AOT, ZIP integrity, v2/v3 signature and existing local test certificate are verified again
because the source changed. Canonical build log: plan-reminder-retirement-release.log; package proof:
plan-reminder-final-apk-proof.json. Its fresh normal Release native check follows after the earlier fixed batch
finishes and restores its original sample. Do not count the earlier APK as the final changed-code package.

## Completed six-case adapter batch

The fixed normal Release batch completes en/fa/de in both app themes. Every case delivers the generic body and
both current translated action captions, then invokes the real one-hour action. Six distinct saved ids retain
exactly their original two-member plan/date groups and actual file scope. After live language changes, every
cached body is the current generic English text and every title is Zanance. Complete 24-table values match the
prepared sample; 12 fictitious plans remain unrestricted in the inactive release above Free's five-plan allowance.
This checks app theme changes; the notification shade retains the emulator's existing OS appearance, with no
system theme or permission change. Own-QA data/profile/display restoration is checked separately next.

The six-case batch is terminal. Exact original owned sample files/sidecars, Main profile and English/System display
are restored through actual UI; all 24 original complete tables match. Its normal signed adapter Release remains
installed/stopped before the fresh final-guard package check. No physical device or system setting was changed.

## Current final APK native completion

After the six-case predecessor restores its original data, install the current final signed APK and prepare one
new independently owned two-member once-plan case. Its actual English notification/actions and native one-hour
snooze succeed, including exact current file/group metadata. All 24 complete tables match. The normal Release restarts
before final exact original files/sidecars and Main/en/System are restored through real UI again. The current
final normal signed Release remains installed/stopped. Only own qemu 5570/TCP 5571 and dedicated server 5038 are
used; that owned server is stopped afterward. No physical device, system setting or security preference changes.
The enabled before-publication retirement branch is proved by actual application SQLite flows, not by activating
commercial limits in this release. Final-guard native proof: plan-reminder-final-native/snooze-proof.json and
plan-reminder-final-native/final-proof.json. This completes the local D-140 step; remaining owner/provider/iOS/
physical-device and selection persistence/UI gates are unchanged.
