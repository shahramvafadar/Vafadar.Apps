# Cold notification snoozes with protected storage (D-333)

## Reproduced defect and resulting behavior

A notification action can start Android without creating an application window. The protected database
endpoint is unpublished until its native startup host runs. ReminderService previously entered its ordinary
financial read immediately; that read rejected the unpublished endpoint and retired the pending notification
queue. A focused application-source/real-SQLite regression reproduces four valid notifications becoming an
empty queue. This is a managed reproduction, not a pre-fix physical-device observation.

Before a cold snooze reads financial data, prepare an unpublished required-protection endpoint through the
same IAppDatabaseStartup host used by covered foreground startup. Run this on a worker, after the notification
action, without creating a window or doing key/migration work in Application.onCreate. An already published
endpoint keeps its existing path. Missing startup, native refusal or return without publication preserves the
original queue and does not save a snooze. Disposal during preparation also prevents subsequent financial
reads/publication. Once prepared, the original profile, source, commercial and privacy guards still govern
notification replacement and retirement. Due dates, amounts, financial posting and app-lock navigation do not change.

The new optional DI dependencies construct only the existing no-I/O host/location. Registration and
WatchSnoozes still do not prepare storage. No financial schema, key format, SDK, production permission or
UI string changes. Native action lifetime under arbitrary process loss remains a separate acceptance boundary.

## Focused managed evidence

- The original one-case regression fails with four pending notifications replaced by zero.
- The candidate passes 100 related application cases: 97 existing and three new refusal/nonpublication/missing-host cases.
- A further disposal-during-preparation regression fails against that candidate and passes after rechecking
  disposal before the first financial read. The final changed-state run passes all four new cases, with actual
  original SQLite rows/preferences and notification preservation. The unchanged 97 successes are retained.
- There are 101 distinct covered cases across these focused runs, not a final whole-suite execution or a new
  whole-repository test count. Strict focused compilation has zero warnings/errors; test outputs are cleaned.

The native preparation port in the disposal test publishes no usable cipher connection: its creator throws if
opened. That case proves disposal/ownership ordering only. Actual native encryption and action delivery are
checked separately below.

Initial strict setup rejects Assert.Single-after-Where and a nullable MatrixCursor row; both are corrected.
Unsupported standalone runner options execute no tests and are not counted. Only the actual -class/-method
execution summaries support the totals above; the initial failures remain in the local evidence.

## Original Android action and separate cold read

Independent package: pro.vafadar.zanance.androidreminderreview, x64/API36 emulator at412x800. It compiles the
original production application, native key/storage host, scheduler, resources and Release/AOT profile. Run
abff683b488044ab87281416c64907cd retains its original account/plan identities and native keys. The fixture
creates one checking account with1000.00 EUR opening balance, one unpaid12.34 EUR plan due2026-10-19,
Once/09:00/no automatic posting, and zero ledger entries. These are explicit fixture-definition writes;
the actual snooze does not mutate those populated financial fields or post money.

The first read-only provider start, PID25930, hits a real application-startup ANR and Android ends it. The
next process25992 is retained through ordinary covered preparation, seeding and the native notification
permission dialog. An initially incomplete accessibility root is retried as a read on the same process.
The original native notification shows the fixture plan,12.34 EUR/due date and In1hour/Tomorrow actions.
Compressed current-window hierarchy and focus guards select only that fixture notification.

Android retains25992 after the first background kill request. No action is sent until actual PID absence
is verified. A single native In1hour tap starts PID26475 directly from
plugin.LocalNotification.NotificationActionReceiver. No Activity/window is created. The first status read
observes the in-flight batch; the same live process subsequently reports:

| Observation | Result |
|---|---|
| Original run | abff683b488044ab87281416c64907cd |
| Process/window count | 26475 /0 |
| Protected publication | true |
| Saved snoozes / actual native pending snoozes | 1 /1 |
| Populated account/plan fields and zero ledger | unchanged |
| Matching startup ANR/crash in this action process | none observed |

No action, fixture or keys are recreated after the early incomplete result. After deliberately ending the
completed process, a separate provider-only process26586 reports the same run, zero windows and one saved/
native pending snooze. Its protected endpoint remains unpublished: the status read does not prepare storage.
Its financialMatch=false means financial data was not read in that state, not that a difference was found.
This qualifies normal process-cold action and subsequent persisted metadata, not a kill between action
receipt and completion, power loss, scheduled-time delivery, phone hardware, iOS or all background restrictions.

The maintained independent caller rejects a changed application id at build time and before window creation
or synthetic status reads. The expected wrong-id build refusal is recorded separately. A further native
identity preflight times out publishing providers after an early Context.PackageName check is added to
OnCreate. That early check is withdrawn; provider initialization stays constant and context/identity checks
remain in Query and before Activity window creation. The failed preflight/cleanup is retained, not a passing
result. The corrected guard receives its own compile/provider preflight; the unchanged successful snooze is
not replayed. That preflight publishes its starting status successfully, with zero windows and no financial
preparation. This verifies the provider entry, not completed application binding. Its empty fixture package
is removed separately. The provider
returns only synthetic state/counts, never keys, database bytes or notification/preference payloads. Main
contains neither that provider nor its authority. Only independent fixture packages and owned temporary
hierarchies are removed. No Samsung input, OS setting, authentication bypass or screenshot-policy change occurs.

## Original startup ANR remains open

D-331/D-332 main startup failures and temporary D-333 PID24802 fail application binding; the independent
provider-start ANR also shows this is not unique to ScheduledAlarmReceiver logic. Temporary managed phases
measure19.270s before the first managed marker and14.213s in shared host/fonts in one failed start. Reminder
initialization210ms and resources185ms do not support moving their work. Other differently instrumented starts
vary materially: one licensing path takes4.886s, including13ms in telemetry disable, while a later path takes617ms.
These are not controlled statistical comparisons or attribution to disk, compression, licensing or the emulator.

The final native timing probe observes one explicitly preloaded app assembly in48ms and native initialization
around1.717s. Preloading is already disabled; existing captured metadata includes registration and telemetry
methods. No profile recording, package update, deferred licence/telemetry ordering, weakened crypto or timeout
workaround is delivered. All temporary source/logging environments are removed before the normal APK.
The profile README's stale original-capture digest/counts are corrected to the unchanged D-320 qualified input.
QA-06 startup/ANR, low-memory and the two-second target remain open. The snooze correction is not an ANR fix.

References: [Android16 application-binding timeout](https://github.com/aosp-mirror/platform_frameworks_base/blob/android16-release/services/core/java/com/android/server/am/ActivityManagerService.java),
[MAUI10.0.110 Application.onCreate](https://github.com/dotnet/maui/blob/10.0.110/src/Core/src/Platform/Android/MauiApplication.cs),
[runtime timing guidance](https://github.com/dotnet/android/blob/main/Documentation/guides/profiling.md),
[installed plugin action receiver](https://github.com/thudugala/Plugin.LocalNotification/blob/0f2d140560da30b81c16a436a2c5b54c98db0335/Source/Plugin.LocalNotification/Platforms/Android/NotificationActionReceiver.cs).

## Final application package

Strict Windows development build and complete ordinary offline Android Release build: zero warnings/errors.
Main APK: artifacts/android-cold-reminder-main-d333/pro.vafadar.zanance-Signed.apk,95223711 bytes,
SHA2564bf9c3fc21976698c175ede0d3b26f92ffaeeb8f58ddd603d07605585d9056c2.
Verified certificate SHA25692cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b;
permanent package pro.vafadar.zanance, min24/target36, ARM64+x64,384 AOT libraries, no INTERNET/debuggable
flag or independent provider. Original native SQLCipher/Mono hashes match D-332. Both packaged application
environment libraries contain none of the temporary diagnostic properties.

Data-preserving upgrade starts main PID26787 from ScheduledAlarmReceiver. The explicit Activity request is
warm3597ms, not loaded-Home timing. An initial observation shows overview loading; the same process later
shows original My account/Recorded balance0.00 EUR. No new matching ANR is observed in that process.
Successful unchanged financial/bank/cipher/backend/layout/full-suite checks are not repeated. An unintended
root-level test-help discovery also attempted unrelated builds and exposed an iOS Metal namespace/type
collision; that is not iOS qualification and remains for the actual Mac phase. The focused runner is the
already built application-test DLL with its documented -class/-method options.

Local evidence: artifacts/startup-binding-d333; native observation result native-qualified-result.json,
original action/persistence status and process events; final main native hierarchy under
artifacts/import-preview-d331/android/d333-final-home-ready.xml. Broader phone, iOS, OAuth/provider,
Store, interrupted-action and release gates remain open. The full development goal remains active.
