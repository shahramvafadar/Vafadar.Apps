# Original PIN clock and protected format on API24 and simulated 16KiB (D-723)

## Named remaining assertion

D-722 introduced Android's registered boot/elapsed clock and optional protected retry deadline
after the earlier compatibility cohorts. Qualify that new boundary on the minimum API and the
approved userspace page-size simulator. This is not another D-722 manual-clock, cold-process,
financial or general security run. No production source, dependency, manifest or build changes.

Reuse exactly the previously used signed independent D-722 package, SHA256
`f06ddc63636b053f0de5371f1030c47db0780bea2a4b2423979ef99e00bc7a92`,
56,889,250 bytes, with its original SecurePinStorage, source-generated JSON, PinLock and
registered AndroidPinAttemptClock. Its previously verified offline/nondebug/signature/CRC/ZIP,
192 own AOT/204 ELF/804 static LOAD evidence remains attached to that binary. Current package
metadata confirms minimum API24, target36 and x86_64. No successful build, old test suite or
threshold from an earlier fixture is repeated. Production shipping remains D-722's separate
`9a9ac821` candidate, unadopted on Main while D-607 is protected.

## Actual platforms and results

| Original observation | Fresh API24 | Fresh API35 userspace16KiB |
| --- | --- | --- |
| Device model | Android SDK built for x86_64 | sdk_gphone16k_x86_64 |
| Kernel page size | 4096 bytes | 4096 bytes |
| Userspace page size | 4096 bytes | 16384 bytes |
| Actual app PID / UID | 2804 / 10062 | 4890 / 10207 |
| Boot | 73e11b8e-a20f-422d-8076-011682c38ebb | cf238577-7b68-4720-ae05-6b859b139cf5 |
| Registered clock available before PIN setup | true | true |
| Actual neutral six-field source-generated JSON roundtrip | true | true |
| Original anchored protected state readable | true | true |
| Final protected metadata and complete22typed source unchanged | true | true |

Both fresh configurations use320x640/density160 and Europe/Berlin. Each has one install and one
normal launch of that same APK. One new fictitious app PIN and the original five wrong guesses
are created only once per new platform UID, establishing the newly introduced persisted anchor.
The terminal read preserves that exact state, original required marker and full typed source.
Only bounded nonsecret metadata/booleans leave the package; no PIN, verifier, salt, key, secure
value or private database is exported. No account, opening, plan, ledger entry or payment is
created. There is no clock mutation, manual expiry, cold launch, owner authentication or Main
command. Native observations are not new unit-test cases; the managed count remains D-722's24.

API35 has **simulated userspace16KiB with a4096-byte kernel**, not hardware16KiB. API24 proves
the actual public boot/elapsed provider works on Android7 in this environment, not on every
OEM or with unavailable/restricted clock metadata. Existing fail-closed/legacy/new-boot rules
remain as specified in [D-722](pin-retry-elapsed-clock.md); cross-reboot tamper resistance is
not claimed by these same-boot format observations.

## Preserved preparation and observer failures

The general AVD manager ignores the separate SDK environment when listing installed images and
refuses the approved E-drive API24 image before creating an AVD. Preserve that initial failure.
The next preparation creates a new configuration from only the approved configuration template
and points the approved E-drive emulator at its already installed official image. No retained
userdata, key, snapshot, external download or global SDK/ADB setting is copied or changed.
Both template hashes remain exact. Emulator37.2.12.0/build16428233 is actually used.

The first API24 identity guard expects a newer boot property absent on Android7 and refuses
before installation or PIN setup. Exact emulator-console AVD identity plus actual API and boot
readiness replaces only that observer assumption. No app input is replayed. Fresh-user metadata
warnings and normal boot delay remain recorded; no emulator is restarted to hide them.

## Retirement and residual cleanup

Final original protected/source packets, boot, size, density, timezone and seven captured system
values match on each platform, including API24's absent font-scale value. Uninstall only each
new package and prove its active own pending/posted state absent. Stop only its owned port5572
instance after completion and use official AVD deletion for the exact fresh name. Both actual
AVD data directories and packages are absent, port5572 is closed, and other AVDs/ADB5038/phone
remain untouched. Inventory before official deletion is13files/11,055,735,297 logical bytes on
API24 and23files/4,362,125,871 on API35. These are logical file sizes, not measured host free space.

One supplementary cleanup request targeted only the two retired configuration/user roots.
Its execution result was not recoverable after truncated output, and no success receipt exists;
read-only observation still finds both roots. Retain the prior inventory of10files/13,017 logical
bytes (4/5,285 and6/7,732) as residual, not reclaimed. Freeze these exact roots without another
deletion, subset, alternate method, move, escalation or build-input reuse. No XML was created.
Used signed packages and local evidence remain outside those residual roots.

## Exact-head hosted results consumed during this batch

Every row below comes from its own single saved response after the actual sixteen-minute due
time, with an exclusive consumed marker before GET. Each own schedule was deleted. No second
request, job rerun, cancellation-cause inference or later-result assumption is made.

| Batch | Actual workflow result and link |
| --- | --- |
| D-718 | [CI success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41268866845), [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41268866794), [identity success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41268866810), [service success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41268866774) |
| D-720 | [CI cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41270082880), [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41270082883) |
| D-721 | [CI success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41270814820), [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41270814819) |
| D-722 | [CI success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41274236946), [CodeQL success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41274236953), [identity success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41274236940), [service success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41274236958) |

The fresh D-723 commit has its own later exact-head check. These results do not accept physical
adoption, all database/key/recovery paths, genuine authentication, punctual reminders, general
security/stability/performance/accessibility, hardware16KiB, providers, Store or release. All three
D-607 deadlines, consumed reboot and unread post-reboot source remain protected. iOS follows
Android completion under D-714; the overall delivery goal stays active.
