# D-520 - Original key protection on minimum-API and16KiB runtimes

## Original source, separate authority

Continue the outstanding API24/16KiB key-protection subgate after D-519 unlocks the isolated SDK.
Use the existing `eng/tests/AndroidKeyProtection` project and actual AndroidDatabaseKeyProtector/
DatabaseKeyBinding sources unchanged. Override only the independent Release build's ABI to
android-x64 and SupportedOSPlatformVersion to24.0; its default physical ARM64/API31 configuration
and D-483 phone evidence remain intact. Main never references this review package.

The first command-line value24 generates an invalid platform-version attribute (CA1418), followed
by platform analyzer errors. Preserve that failed build, correct the actual version literal to
24.0 and publish only the incomplete x64 candidate. No analyzer is disabled and no production
source or successful phone/build cohort is repeated.

Signed package `pro.vafadar.zanance.keyreview`,20919497bytes,
SHA256 `e9775a26c75b3956a8fc2225d7dcafac3d615008ba283c6ef98cab17ecc2baf9`,
has minimum API24, ten x86_64 native libraries, no permissions, non-debuggable status, valid ZIP CRC
and successful v2/v3 signatures. A v2 signature is actually present for the API24-minimum build;
do not substitute the older API31 v3-only receipt. Installed APK bytes match this same candidate
in both new AVD UIDs before retirement.

No database, financial scenario, network, system/account/authentication change or Main key access
is involved. Each new owned UID keeps private fictitious expectations/envelopes internally; only
public phase facts are read. No key, envelope, alias or private expected digest leaves the app.

## One retained run per actual platform

Reuse the project-local AVDs, preserving their OS settings and existing ADB5038/PID76812. Their
Main app fixtures were already removed in D-519. Assert the independent key package absent before
installation. Generate one nonsecret run identity per new UID and retain it for all phases.

| Actual platform | Create PID | Cold PID | Retirement PID | Reported key levels |
| --- | --- | --- | --- | --- |
| Android7 / API24 / x86_64 /4096-byte kernel pages |2570|2654|2712| null/null in all phases |
| Android15 / API35 / x86_64 /16384-byte pages |3239|3352|3410|0/0 in all phases |

Create validates original generation and recovery unwraps and non-exportable native wrapping keys.
Before cold, force-stop only the exact new key-review package and prove its package PID absent.
Cold requires a different PID and original expectations; twelve altered profile/installation/
generation/ciphertext/header/truncation cases refuse per platform. Never recreate accepted keys.
Before retirement, again prove the prior helper process dead and launch only the unconsumed phase
with the same run. Two removals without durable authority preserve the original keys; two explicit
private durable receipts authorize removal. Both aliases remain absent before and after later
unwrap refusals; lost keys are not recreated. Six actual native phase receipts,24 binding/tamper
refusals and four authority refusals are not additions to the managed test count.

The original adapter intentionally returns null below API31; this is unavailable security-level
metadata, not a failure of the observed API24 protection operations or an invented hardware level.
[KeyInfo.getSecurityLevel](https://developer.android.com/reference/android/security/keystore/KeyInfo#getSecurityLevel())
is introduced in API31. The actual API35 value0 means software enforcement according to
[KeyProperties](https://developer.android.com/reference/android/security/keystore/KeyProperties#SECURITY_LEVEL_SOFTWARE).
It is not Trusted Environment, StrongBox, remote attestation or proof about Main's existing keys.
The separate D-483 Samsung level1 result is retained as its own physical observation.

## Evidence and cleanup

Activity launch/phase markers, original per-phase receipts, exact prior-PID death proofs, APK
verification, public runtime/package metadata and terminal audit are retained locally. Private
fixture files are never pulled. The first API24 installed-APK transfer gives a digest mismatch;
a separate bounded read obtains the complete20919497bytes and matching original digest. That
observer failure remains preserved; no accepted key phase is repeated. A native sha256sum command
is unavailable on API24 and is recorded as such, not a product failure or substitute digest proof.

After both original retirement receipts and installed package equality, uninstall only the two
new AVD key packages and verify absence. Stop both owned emulators; preserve SDK/AVDs for further
approved work. No remote XML is created. Standard strict build-output cleanup passes without
warnings/errors; remove only the redundant unsigned published APK and retain the actual signed
candidate and evidence. Existing ADB server76812 and physical phone remain untouched.

Production adapter/data schema/permissions/dependencies and shipping APK stay unchanged at the
D-516 candidate adopted before these independent reviews. This qualifies only original adapter
creation, cold binding/tamper and authorized alias retirement on these two x86_64 systems. It does
not qualify all encrypted-database/migration/backup/recovery paths, authentication-bound keys,
biometric enrollment, hardware attestation, power loss, OEM transfer, ARM64/page-size variants,
Main private-key equality, general stability/performance/accessibility, iOS or external release.
No unrelated suite, accepted financial command or successful physical source setup is repeated.
New-head hosted CI remains pending until its own one-shot check.
