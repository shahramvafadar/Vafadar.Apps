# D-488 physical original goal-reminder queue lifecycle

## Missing boundary and independent caller

Existing D-70/Debug evidence covers local/emulator goal reminders; D-474/475 physical evidence is
specifically for original plan actions and timed plan delivery. Do not treat those as physical
goal-reminder lifecycle acceptance or replay their successful Snooze/timed/financial cohorts.

Add review_goal_lifecycle=true to the existing independent AndroidReminders foreground selector.
Reject simultaneous scheduled-only and goal modes before preparing either fixture. Main never
references this caller. NativeGoalReminderReview requires the exact independent package before
all effects/reads and owns one new directory/identity marker. The original read-only provider
adds /goal-status, returning only already-produced synthetic stage/count/goal metadata receipts.
It never runs a transition, prepares storage, creates a window or reads Main private files/keys.
All original plan/status behavior is retained. There is no production app/planner/scheduler,
permission, schema, resource, SDK or financial-writer change.

This new batch exercises the actual ReminderService, GoalStore, protected original data layer,
GoalReminderPlanner and LocalNotificationScheduler on ARM64, not copied application algorithms.
Set up one fictional Savings opening100EUR, one account-balance goal150EUR and a fixed25EUR/Once
contribution date tomorrow24October2026. Default reminder/details are off. Native permission is
granted only through the actual labelled Zanance reminder review prompt; ownership is tied to
its launching UID/package. No clock/system setting, Main permission, account or financial data
changes. A portable-password fresh-install step was considered separately but is not attempted:
owner-entered secrets remain a genuine boundary, not grounds to replay unprotected D-403.

## Original eleven-state evidence

Run cff3ebffa33b4eefb65915f072ef42d5, original process15501, completes without restart:

| Original source state | Native SDK pending goals |
|---|---:|
| Default reminder off | 0 |
| Explicit enabled | 1 |
| Paused | 0 |
| Resumed | 1 |
| Completed | 0 |
| Archived | 0 |
| Active but target reached | 0 |
| Active and unreached again | 1 |
| Explicit details enabled | 1 |
| Reminder disabled | 0 |
| Generic reminder restored | 1 |

For reached/unreached, deliberately change only the independent target150-to100-to150 while the
account stays100; this is not a contribution, repayment, ledger entry or form acceptance. Metadata
transitions use original stores, not synthetic notification requests. Every transition drains the
current original coalesced batch and performs original RefreshAsync before reading the native SDK
pending list. Retained per-stage receipts skip accepted states on continuation. Completed reports
are retained without replay. Interrupted setup retains ownership and refuses silent reseeding.

Every pending request has the original stable goal id, actual-file scoped link and sole original
contribution date, local09:00. The generic packet equals the original app-name/generic resources
and reveals no goal identity; explicit details use the original goal title and non-generic body.
All non-goal pending requests remain absent. Exact original serialized account stays unchanged;
goal identity/name/note/account/type/currency and plan amount/frequency/date remain equal. Entries,
allocations and ordinary plans stay empty. Goal state/target, reminder enabled/details and their
original update timestamps are the deliberately changed metadata, not full-database equality.
Eleven native observations do not increase the managed-test count.

The OS has the actual RTC_WAKEUP Alarm8320924 and PendingIntentRecord250b7a1 for the independent
ScheduledAlarmReceiver. origWhen1792825199999 corresponds to24October08:59:59.999 local; retain
that one-millisecond conversion rather than changing time. The original inexact window is one
hour, flags0x20. This is an actual queued native alarm, not proof of delivery at09:00. No expedited
Show copy or notification content tap is invoked. Preserve all existing timing/idle restrictions.

## Build, observation and preservation

Fresh complete strict Release/AOT/Offline ARM64 publish succeeds with zero warnings/errors.
Package pro.vafadar.zanance.androidreminderreview is proven absent before installation. APK:
E:/Zanance.ReviewBuilds/D488-reminders-package/pro.vafadar.zanance.androidreminderreview-Signed.apk,
53,592,582 bytes,192 AOT libraries, valid v2/v3 signatures, no INTERNET/debuggable; five original
runtime/cipher engines equal shipping D-487. SHA256:
309dcb86d862e9c0bac47a7e770db5d448c0f78126bba9ef8b940b37f40924ba.
The original build target refuses permanent Main identity before native effects. Main/Windows
builds, old financial suites, plan-action/timed cohorts and prior successful resource tests are
unchanged and are not repeated. No new managed tests are claimed.

Actual independent Activity COLD1720ms is not whole-Home/target-speed evidence. First UI observation
shows startup coverage; wait for the same process to reach its original permission prompt. The
45-second stage observer expires after seven completed states; the same live15501 subsequently
returns all eleven receipts. No state, seed or permission grant is replayed. Initial absence
observation incorrectly assumed pm path returns success for an absent package; exact package-list
absence resolves that observer without touching installation/data. Trim the retained native alarm
excerpt at the original owned Alarm boundaries, excluding both adjacent package records; these
were observer-boundary mistakes, not a planner or delivery failure.

After successful verification, uninstall only the fresh independent fixture and confirm its future
alarm/notification absent. Original Main13827 remains live. All141 public Home tuples including
979EUR, original APK SHA122e33f63af8024de1561021e466ebe625f67c00efb9056ff8d794827fe3c2ca and exact
font1/1080x2340/density450/accessibility0/empty services match baseline. No Main profile or financial
write, private-store/key extraction, screen-capture bypass or owner authentication occurs. Two
Main XMLs plus three scoped native app/permission XMLs are exact-hash checked and removed; retain
local private originals, stage receipts, package/build/identity logs and final proof.

## Open acceptance

This closes the missing physical original goal-reminder queue/eligibility/privacy cohort. Actual
09:00 delivery, missed dates and rule-calendar variants, account/holding-unavailable variants,
notification tap through app lock, Doze/reboot/timezone/device/OEM behavior, interrupted action,
all security/stability/ANR/crash/GC/performance/accessibility/iOS/provider/Store and publication
acceptance remain separate. Shipping Main/code/APK remain D-487; exact-head CI follows the push.
The broader goal stays active.

## Prior exact-head single check

The one delayed GET for D-487 head9c4c0812 returns CI39941872181 still in_progress; local identity
backend39941872196, self-hosted qualification39941872184 and CodeQL39941872180 are completed
success. ci-snapshot-difference-d487 is consumed/deleted in every result path; no second request
or all-green claim is made. Historical successes and the in-progress-at-check record stay intact.
The new pushed head gets its own delayed single check; newer-push cancellation is expected.
