# D-290 verify the selected startup generation once

## Cause and complete change

Covered startup previously verified every captured profile with a temporary generation owner,
then immediately resolved the selected profile again for its retained session. Both operations
ran the same existing-key/KDF/integrity path. Cold one-profile preparation read keys four times;
two profiles read six times. The duplicate selected owner was immediately discarded.

Bootstrap can now place the exact captured current original last and use its actual session
publisher for that final verification. All other profiles still complete their full original
migration/history/key/integrity checks before the selected session can publish. The selected
session still performs its original complete verification and retains that exact mutable key.
The callback and selected original must be paired, absolute, captured and equal to the unpublished
current original. A successful callback must leave the exact profile/installation's actual verified
SqlCipherProfileGeneration as endpoint owner; a no-op callback is refused. Default bootstrap
callers retain their original all-profile verification behavior.

No KDF, crypto parameter, key format, integrity check, profile scope, schema, migration checkpoint,
recovery or rollback policy changed. Preparation remains under drained maintenance in the covered
worker. The existing observer-failure path retains its exact live owner and only retries notification;
a wrong/missing selected or inactive key cannot publish an endpoint or prepare a replacement key.

## Focused verification

- The affected bootstrap class passes 19 strict cases including three new pair/foreign-original
  refusals. The final additional no-op publisher regression passes alone. Four actual application
  startup cases pass. This adds four covered behaviors to D-286's 3,610: 3,614 covered, not a new
  complete-suite run. No unrelated successful suite is repeated.
- Actual Windows original-schema/DPAPI caller: old fresh/cold preparation and two-profile
  preparation captured in windows-startup-proof-792b57ad652b40bdb00d2a23dab51023.json. The same
  retained files/identities/keys reopened with the changed source. Cold key reads reduce 4 -> 2
  and 6 -> 4. Initial small-fixture samples change 349.00 -> 198.53 ms and 496.62 -> 342.77 ms.
  These are bounded preparation samples, not whole-app/loaded/phone/cold-OS timing acceptance.
- Windows native final observer failure, changed-catalogue refusal and retry reuse the same key,
  preserve the 321 opening balance, reject missing identity and ordinary duplicate starts.
- Android signed Release AOT/trim independent caller builds without warnings/errors. Two-profile
  initial/cold startup retains the selected 880 entry/1000 opening balance; cold reads are four.
  Actual key-read refusal for each selected/inactive profile keeps the endpoint unpublished.
- Same Android binary also checks fresh schema in memory with no plaintext original, cold
  retained-key observer failure/retry, missing-identity refusal and the 321 opening balance.
  Cold reads are two. Observer-result timers measure the subsequent completed no-op call and
  are deliberately excluded from performance comparisons.

Evidence: artifacts/startup-d290-baseline.log, startup-d290-data-tests.log,
startup-d290-final-guard-test.log, startup-d290-app-tests.log, startup-d290-two-profile-final.json,
startup-d290-observer-final.json, startup-d290-two-profile-guarded.json, startup-d290-restored-cold.json, startup-d290-windows-build.log;
artifacts/native-sqlcipher/build/windows-storage-d260-ec/windows-startup-proof-792b57ad652b40bdb00d2a23dab51023.json;
artifacts/native-sqlcipher/build/android-d/android-migration-proof-5e688f6c4e3e49f9a520c70b4565b50d.json
and android-migration-proof-b3e621d805a7413a938b43e8003f23b9.json.

The first new Windows negative fixture omitted protectionRequired and was correctly refused by
ProtectedProfileStorage construction. Only that independent fixture was corrected; the same
retained cold data/keys then passed both key-failure cases. No successful unrelated checks were replayed.

## Main application handoff

Strict Windows build passes with zero warnings/errors (25.54 s). Final guarded two-profile
Windows cold preparation is 338.27 ms with four key reads; both missing-key refusals pass.
The retained D-229 restored fixture also cold-reopens with existing keys only, preserved later
French preferences and balance 321; no replay/old publication/old upgrade callback runs.

Complete strict signed Release APK: artifacts/android-startup-d290/pro.vafadar.zanance-Signed.apk,
88,353,094 bytes, SHA256 f5e8271f21feeaa0f75a3d1f1c7c271588d3642b8375ac92032f1c2b803fe1ab.
Package pro.vafadar.zanance, min API24/target36, v2/v3 signature verified. Both arm64-v8a/x86_64
native library hashes match qualified inputs and both AOT application payloads are embedded.
Only Zanance_Storage_Review emulator-5570 on adb port5038 was upgraded. Exact main PID24749
cold-launches through the loading cover to ready Home, opens actual New expense and returns
through Cancel with unchanged 0.00 EUR and enabled action. No Save or key/profile change.
The first entry dump returned a transient null root; the same process was observed again,
without restart or another tap. The 2201ms first-frame timer is not ready-Home timing.
Main was normally stopped. Proof: artifacts/startup-d290-apk-proof.json,
startup-d290-signature.txt, startup-d290-main-runtime.json and main ready/entry/return XML.
Release test-output cleanup passes with zero warnings/errors (0.98s).
Native cipher bytes are unchanged. No changed UI text/layout requires another translation/size/theme
matrix. Physical-device, iOS, full cold-start/low-memory/ANR, native-warning safety and release gates
remain open; this is not a two-second startup or hardware-keystore acceptance claim.