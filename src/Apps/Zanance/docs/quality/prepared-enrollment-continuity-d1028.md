# Prepared owner-controlled enrollment continuity — D-1028

Status: prepared only. No installation, native phase, system enrollment change,
authentication, Main command or owner action has occurred in this step.
[D1025](private-analysis-keystore-loss-arm64-d1025.md) and
[D1026](private-keystore-loss-api24-ps16k-d1026.md) already close their named
alias-loss conditions; they are not replayed by this preparation.

## Concrete remaining boundary and owner decision

The remaining question is continuity of the original non-authentication-bound
wrapping key and private value across a personally performed enrollment change.
It is not the previous intentional own-alias deletion, app-unlock callback acceptance,
a claim that every biometric key should survive enrollment, or complete security.

D-597 in AGENTS.md requires an owner decision for high-risk physical-device actions;
real authentication remains owner-only. The [Android enrollment-key contract](https://developer.android.com/reference/android/security/keystore/KeyGenParameterSpec.Builder#setInvalidatedByBiometricEnrollment(boolean))
permits irreversible invalidation of some biometric-authenticated keys after new
enrollment. A separate test device is preferable. No primary-phone enrollment
change, existing template removal or security-setting input is authorized merely
by preparing this package. Other apps may use different key policies.

## Bounded prepared implementation

One standalone Samsung SM-A176B/ARM64-bound offline SDK links the unchanged original
AndroidDatabaseKeyProtector and SecureAiStorage. A different dedicated device needs
its own admission and appropriately bound package first. The three one-shot phases
are prepare, after-add and after-remove, each guarded by fresh-package/run/PID/
caller/ABI/model checks and an exclusive marker.

After a future explicit owner decision and fresh device/public-state admission,
prepare may create only the new disposable UID's two native aliases. Original
ProtectGeneration wraps a declared fictitious32-byte sample; original private
storage writes a fictitious string. The synthetic sample, envelope and marker
remain private to that fresh package. Outputs contain only phase/PID/UID/runtime,
exactness booleans and own alias count, never key/value/ciphertext. Original native
non-exportability checks remain intact; no cryptographic policy changes.

Only after that baseline succeeds may the owner personally add an extra fingerprint,
keeping the existing PIN, face and all original templates. Explicit owner confirmation
precedes one fresh-package stop/normal relaunch. The after-add phase reads existing
keys/value and verifies exact retained envelope/marker/two-alias state without a
new key-preparation or replacement path. Only the owner may remove that newly added
fingerprint if agreed; another confirmed fresh-package restart precedes after-remove.
Failures remain terminal observations, without repair/rekey/retry. Retire only the
fresh package and return to preserved Main. No original financial database/profile/
account/ledger, remote service or genuine login callback is involved. There are no
system-setting writes by the observer.

The external enrollment transition would be owner-attested; the package does not
enumerate templates or independently prove that enrollment changed. This is not yet
any runtime acceptance. Whole app-lock/enrollment/key-lifecycle/security and other
platform/provider/release gates remain separate.

## Verified preparation and cleanup

One strict private ARM64 build passes. Prepared package is17475032bytes, SHA256
`966a83cd32bd12c60475fbed1c4159bf0003144aee473c192d19263ea77d84bb`,
104AOT/114ELF/446static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/alignment pass.
Original ARM64 Mono remains exact. Three actual compiler documents bind the final
observer and both original classes. This is finite compiler-source evidence, not
whole-PE, app/device runtime or hardware16KiB acceptance. Product code/model/runtime/
GC/test count are unchanged; no Windows/full shipping or old suite is rerun.

Two standard own-output Cleans retire888registered files/118381557logical bytes;
438registered files and nine host-tmp files1485213bytes remain. All151logged delete
targets stay inside current own bin/obj roots. Prepared signed APK/source/compiler
and decision-proposal evidence remain. No supplemental/frozen or retained5606/Q02
action occurs. Main24881/6e8f5b61 remains only its dated D1025handback, not fresh
liveness/private data equality. Canonical parent States and the full delivery goal
remain open; this preparation grants no enrollment or authentication authority.

The sole D1026 response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42826610993)
completed/cancelled and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42826610806)
in_progress with no conclusion. No second query, job-log request, rerun or cause
inference follows that saved response.

The single D1027 response separately reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42828725582) and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42828725436)
in_progress with no conclusion. No polling or second request follows.
