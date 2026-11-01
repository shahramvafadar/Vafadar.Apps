# Reminder test-source projection (D-719)

## Actual failed CI and local reproduction

The sole D717 exact-head response for233d2182 reports CI failure while identity, service qualification
and CodeQL succeed. Its schedule is deleted; the run-list endpoint is never queried again. Actual
failed-job metadata and logs identify the same missing-source dependency in all three build jobs:

| Failed job | Actual job id | Failure |
|---|---|---|
| Windows build |124724118176|CS0103 ReminderLocalTime missing in Data.Tests' linked ReminderService, line322|
| Libraries and tests |124724118276|Same CS0103 in the Ubuntu test build|
| Android build |124724118479|Same CS0103 while building the solution|

[Actual failed CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41264415066).
[Windows job](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41264415066/job/124724118176),
[test job](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41264415066/job/124724118276),
[Android job](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41264415066/job/124724118479).
The relevant exact error excerpts are retained locally. A large full-log transfer exceeds a64000-byte
execution limit and is not written or split into a workaround; unrelated/sensitive log material is
not needed to establish this error. The actual job logs remain at their original source.

The App test projection already includes ReminderLocalTime. Data.Tests also compiles the original
ReminderService/IReminderScheduler against real stores, but omits that newly referenced pure helper.
A strict local Data.Tests build on the unchanged published source reproduces the exact CS0103;
no product failure or new test is fabricated for reproduction.

## Minimal correction and relevant checks

Add only the explicit original ReminderLocalTime.cs compile item to Data.Tests, matching its existing
coordinator source projection. The helper uses Core types and system time-zone APIs, without MAUI.
No original application, financial model/writer, permission, SDK, runtime, workflow or package changes.
Both known original-coordinator projections now include their current helper dependency.

- Compile the entire Vafadar.Tests.slnf graph in Release/ContinuousIntegrationBuild mode:0warnings/0errors.
- Run only the affected existing original GoalReminderDeliveryTests and ReviewReminderDeliveryTests:
  13pass,0failure,0skipped. These use their existing isolated SQLite inputs and original source.
- New unit test count is0. Do not rerun D717's successful App/Core/native suites, unchanged Windows/
  Android application builds, financially accepted device cohorts or successful external jobs locally.
- The D717 prepared shippingeb17f5d8 remains the same source-bound application candidate; this is a
  compile-projection correction, not a new APK or physical adoption.

A new push's ordinary CI can now reach its previously skipped tests. Future remote success is not
claimed by the local compiler result. Only a new exact-head one-shot check after16minutes is planned;
old failed jobs are not rerun against their unchanged failing commit.

## Cleanup and independent work

Standard test-solution Clean succeeds with2288registered deletion lines, not distinct files or host
free-space proof. The one supplementary output-root removal is rejected before process creation
with `blocked by policy`; its measured residue273files/10,876,008logical bytes stays frozen without
retry/subset/alternate/move/escalation/reuse. No earlier refused scope is touched.

D718's distinct manual-clock cohort remains in progress. A real backward20minute clock change
moves the one-hour RTC request's monotonic target by+1,200,031ms while preserving calendar dates,
three native handles and source cache. Actual clock and seven settings are restored; the accepted
fixture is preserved without preparation replay while this build failure is fixed. No actual future
notification delivery or monotonic repair is claimed here. See the
[manual-clock continuation](manual-clock-snooze-interval.md).

Samsung/D607's three deadlines, consumed reboot, unread source, Main adoption boundary, iOS-after-
Android execution order and the overall active goal remain unchanged.


## Actual exact-head CI receipt (1 November 2026)

The single consumed check for `0bea44b2a7007012969a14f57500afc77f5dd519` returned
[CI success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41266484028) and
[CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41266484020).
Its own follow-up was deleted; no second query or old-job rerun occurred. This supersedes the
future-CI status above only for that exact head. D-718 later completes its distinct repair and
retires its own emulator; its new source requires its own one-shot CI, without reopening D-607.
