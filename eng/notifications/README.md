# Reviewed Android notification Core (D-580)

Android notification broadcasts must not inherit ActivityFlags.SingleTop. Its numeric
bit means FLAG_RECEIVER_REPLACE_PENDING to Android's broadcast queue and can cancel
distinct simultaneous reminders whose Intent filters match. Keep SingleTop only in
the original Activity branch. Preserve request codes, extras, scheduling/privacy and
the original IncludeStoppedPackages flag.

`notification-core.inputs.json` pins the official MIT source archive, every original
C# file, license and reviewed `AndroidUtils.cs` replacement by SHA256. The preparation
script downloads only that bounded official archive into project-owned
`artifacts/notification-core`, refuses modified/incomplete/extra inputs and preserves
them for diagnosis. The PowerShell wrapper holds an exclusive file lock while
preparing. Python3 and PowerShell7 are build prerequisites; CI supplies Python3.13.

The source project retains public assembly identity1.1.2.0 and records file revision
1.1.2.580. The Android targets exclude the corresponding package assets and reference
this project when an app references Plugin.LocalNotification. Other platforms remain
on their original package. This is an explicitly modified source build, not an
official signed replacement NuGet package. The original upstream MIT copyright and
license remain in `LICENSE.upstream` and the existing bundled notices. Zanance's own
source remains all rights reserved.

Only the three named independent diagnosis projects may set the baseline opt-out.
They preserve original failing outcomes and must never be used as shipping packages.
Do not add a global feed, change source mapping or weaken digest checks to build.

Run `python eng/notifications/test_source_preparation.py` after relevant source-input
changes. The seven tests execute actual preparation against disposable owned inputs;
they do not post notifications, prepare financial data or establish native delivery.
See [the actual comparison and qualification limits](../../src/Apps/Zanance/docs/quality/reboot-paired-delivery-diagnosis.md).

D-581 uses explicit input rejection rather than Python `assert`: `-O` and
`PYTHONOPTIMIZE` must never disable source/license/archive/cache admission. Two
subprocess regressions run actual preparation with `-O` and changed replacement or
cached source; both must refuse without replacing the failed inputs. This changes
build admission only; every reviewed SDK compilation byte remains D-580.
