# Independent same-day Android notification delivery

This notification-only package compiles the original production request builder, delivery-window
policy, publication guard and existing notification SDK. It has no financial database, account,
profile, authentication or network service. The production app never references this project.

Build a complete signed Offline Release APK with `eng/scripts/Build-AndroidApk.ps1 -Offline
-Project eng/benchmarks/Zanance.AndroidDelivery/Vafadar.Zanance.AndroidDelivery.csproj
-Output <owned-output>`. The fixed identity is `pro.vafadar.zanance.reminderdeliveryreview`.

Launch its original activity with one explicit32-character lowercase hexadecimal `review_run`.
Use its real button once and the ordinary native notification permission dialog for this new
package only. The real local time must already be more than one hour after09:00. No clock or
system setting is changed. Two original request-builder notifications retain09:00 as their
deliberately past deadline; observe actual AlarmManager receiver dispatch and notification
creation records before accepting same-day delivery. Yesterday's third request must be refused.
The native SDK also validates the last same-day tick and the first next-day tick without changing
the device clock. This is a controlled late-delivery regression, not future punctuality or Doze,
reboot, cold-start, whole-device or full-app financial-flow acceptance.

An exclusive started receipt and per-request accepted receipts prevent command replay. Read
`content://pro.vafadar.zanance.reminderdeliveryreview.status/status` only after the original window
has started. The provider returns only these synthetic receipts and never publishes a request.
An observation failure permits receipt/notification rereads, not another successful publication.
After retaining evidence, remove only the new package and verify no pending, blocked or posted
work; preserve historical native records and restore the original Main/APK/OS. The D-507 result
and original failed D-494 criterion are documented in
`src/Apps/Zanance/docs/quality/physical-timed-goal-period-reminders.md`.
