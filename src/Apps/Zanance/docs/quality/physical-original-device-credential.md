# Original physical device-credential authentication (D-737)

## Actual prerequisite and owner action

Samsung SM-A176B/RZGL105RSDL, API36/ARM64/4096, original boot
82ccfbab-7840-402e-9a76-3f49bd423b3d. The current unchanged original
DeviceAuthenticator initially reports unavailable. The owner reports screen lock
None, then explicitly chooses to configure a PIN personally and reports completion.
The same live independent process 16954 subsequently reports available. No password,
PIN, pattern, biometric template, native credential hierarchy/image or replacement
Success is collected. The owner alone performs real credential input.

The owner's device PIN is separate from Zanance's four-digit app PIN. App PIN stays
unset throughout. Preserve the owner's new system credential; do not restore the
historical None setting. No global security, battery, clock, permission, accessibility,
TTS, font, display or timeout command is issued by this cohort.

## Original application and original SDK

Use one fresh originally absent package
`pro.vafadar.zanance.deviceownerreview`, UID10408, run
`d437fec0a6824e5f9e92091b13dd1144`. Complete original App/Core/Data/XAML and the
original Settings/AppLockService/AndroidX adapter run in an independent signed
55,075,334-byte offline/non-debuggable ARM64 APK:
`6c16539a1fce137af04ed6584010c61c19414046a4f8ef332e99cb3799a87481`.
192 own AOT libraries/204 ELF/804 statically 16KiB LOADs, v2/v3, CRC and ZIP alignment
pass before its single install. No production source, shipped permission, SDK,
financial algorithm or Main installation changes.

The diagnostic authenticator delegates availability and AuthenticateAsync directly
to a new production DeviceAuthenticator and returns its actual result. It records
only bounded request/completion counts, outcome, exception type and elapsed time.
The read-only shell/package/UID/PID/run-bound observer has no prepare/authenticate
command. It refuses financial source reads while the original AppLockService is
locked, returning null/unread rather than a claimed source mismatch.

Original onboarding is completed once with one fictitious Checking account
D737_AuthWallet/EUR/opening0. The original reminder rationale chooses Not now,
without a system permission grant. Ledger, schedules and goals remain empty.
The observer additionally enforces the exact one-account name/currency/zero opening,
no configured app PIN and the stated empty financial collections. No entry Save,
Import, Undo, settlement, plan or previously accepted financial fixture is replayed.

## Three distinct real requests

| Actual original action | SDK callback and persisted result |
|---|---|
| Enable Lock the app in original Settings | Request1 returns Success after the owner's PIN; runtime and persisted AppLockEnabled become true and the native switch publishes true. |
| Ordinary return after actual background interval | Same PID16954; device boot-clock lower bound 84.88 seconds, independent host interval retained. While background-covered, source is unread and request count remains1. Ordinary no-extra return starts request2; the owner enters the PIN and SDK returns Success. Cover clears; all 22 typed source collections match the complete post-enable SHA256 exactly. |
| Distinct original disable command, cancelled by owner | Request3 returns Failed through the original SDK error/cancel callback. Runtime/persisted lock and native switch return/remain true; the same complete 22-source digest remains exact. No disabling commit or financial write occurs. |

The originally requested return-prompt cancellation was not performed: the owner
reports entering the PIN instead. Preserve that actual successful return; do not
replay it or label it cancellation. The different disable-cancellation branch is
observed separately. Physical cancellation of an unlock cover is not accepted here.

Final trace is requested3/completed3/succeeded2, outcomes
Success/Success/Failed with no exception types. The elapsed values include the
owner's manual response delays; they are not authentication-performance measurements.
The actual full typed source SHA256 after enable, after return and after cancellation
is `43c6085fb9098a718d8ceb523380a05331ccc66010b5796a701be4e14270c8aa`.
This is equality of the defined 22-collection packet, not every private database
object or a key-attestation/full-database claim. Enabling intentionally changes its
setting before that baseline; it is not called equality with the initial empty or
pre-enable packet.

## Original Main handback and actual low-memory exit

Foreground original Main once through its normal launcher, with no extras, write,
profile switch, reset or install. Initial 16-node Preparing is refused as a ready
comparison. Continue read-only observation of the same actual new PID32068, without
another launch: Home979 and all 141 original public node attributes equal the retained
D736 final-current baseline exactly. Current original package remains dc774120;
UID10380/CE155299/DE154603, caller b1d5/UID10381/CE134622/DE135779/grants/Stoppedfalse,
boot, size1080x2340/density450 and all five prior OS values are exact after retirement.
No private Main database/key or original profile contents are read again.

The earlier Main29013 did not survive: ApplicationExitInfo records
1November2026 15:02:08.140local, reason3 LOW_MEMORY, subreason0/status0,
importance400/RSS80MB and no trace. This OS reason is not relabelled as an app/native
exception or as a fixed general stability issue. The original ordinary return reports
COLD1680ms; that Activity metric is not fully loaded Home performance. D737's own
review PID16954 remains the same across its actual authentication observations.

## Fresh-only retirement and output limits

After all three real requests complete, uninstall only the fresh exact owned package
once. Package including retained-data registration and its process are absent.
No own alarm/PendingIntent/active blocked or posted notification remains. The initial
blanket no-mention observer correctly refuses two notification State Changes rows:
Android's uninstall removeAutomaticZenRules history, including its no-changes record.
Classify those exact historical events separately; leave history untouched. No
uninstall, authentication or accepted setup is repeated to repair that observer.

17 newly owned XML files / 656,194 bytes are matched by local/remote SHA256 and removed.
No system credential XML or screenshot was created. One standard Clean succeeds with
zero errors/warnings; no unrecorded removed-file total or host-free-space gain is
invented. Used signed APK, three source-archive files and original success/refusal
receipts remain. Preserve the initial private unused-namespace compile refusal,
launch-helper keyword refusal before am start, one missing host observer-path refusal,
and first 16-node Preparing snapshot; continue only the unsubmitted/unfinished phases.

Supplementary deletion of exactly these three new scopes is rejected before process
creation by automatic review with `blocked by policy`:

- `artifacts/d737-device-owner-source`
- `E:/Zanance.ReviewBuilds/D737-device-owner-package/pro.vafadar.zanance.deviceownerreview.apk`
- `E:/Zanance.ReviewBuilds/D737-device-owner-package/_Microsoft.Android.Resource.Designer.dll`

831 files / 180,972,361 logical bytes remain across them. Freeze all three without retry,
subset, alternate method, move, escalation or reuse as build inputs. The actually
used Signed.apk and used source archive are outside those exact refused targets.
This supplementary cleanup is incomplete, not reported as disk space reclaimed.

## Acceptance boundary

This completes the finite original Samsung device-credential enable/real longer-return/
cancelled-disable cohort. It does not accept fingerprint/face/enrolment changes,
cold credential/notification/export/recovery routes, every device/security/DB/key case,
private Main data equality, general ANR/crash/performance, accessibility, hardware16KiB,
iOS/provider/Store/release. No new app-unit case or unchanged successful build/suite
is claimed. D607 remains terminal with its original reboot consumed and fixture absent;
the overall goal remains active and Android precedes iOS.
