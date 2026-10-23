# Original Android key protection on a physical device

This independent permission-free Release ARM64 package links the actual AndroidDatabaseKeyProtector
and DatabaseKeyBinding sources. Main never references it. The fixed identity is
`pro.vafadar.zanance.keyreview`; identity/Debug changes refuse. API31 is required for reported
KeyInfo security metadata. No real credentials, Main keys, database files, authentication,
network, accounts or OS settings are accessed. There is no key material in log output.

```powershell
dotnet publish eng/tests/AndroidKeyProtection/AndroidKeyProtection.csproj -c Release -p:ContinuousIntegrationBuild=true -p:UseArtifactsOutput=true -p:ArtifactsPath=E:/Zanance.ReviewBuilds/key-native -o E:/Zanance.ReviewBuilds/key-native-package
```

Verify the full APK's ZIP, exact identity, ARM64 slice, signature and absence of permissions before
installing only a newly owned package. The API31-minimum helper is validly v3 signed; do not require
the optional v2 scheme when apksigner reports v3 success and v2 false. Preserve actual signer evidence.

Capture original foreground Main/PID/public Home/APK/OS and assert this helper is absent first.
Create one random nonsecret `review_run` GUID in N format and retain it for all three phases.
Launch `.KeyActivity` with that extra and `review_phase=create`, then read the exact helper PID's
ZananceKeyReview record. It creates only random fictitious data keys in memory and its own
app-scoped wrapping aliases, persisting encrypted envelopes and a private digest expectation.
No plaintext key is written to storage or exported. Preserve the completed create receipt.

For `review_phase=cold`, stop only this exact helper package, prove its old PID absent, and launch
with the same run. Require a different PID, original unwrapped expectations, twelve refusal cases
and reported OS security levels. For `review_phase=retire`, again stop only this helper and prove
absence before launching the same run. This Activity does not dispatch phases from OnNewIntent;
delivery to an existing top instance is not phase execution. Never recreate the fixture or repeat
an accepted phase to repair a launch/observation. Each phase has a private CreateNew claim;
partial failure requires inspection, not replay or deleting the claim.

Retirement first proves that missing durable authority preserves both aliases, then uses each
explicit private durable receipt with the original removal adapter. Require absence before and
after lost-key unwrap refusals. It never enumerates another app's aliases or constructs Main
identities. No app database/financial scenario is seeded. Only after completed retirement verify
the installed helper digest, uninstall that newly installed package and restore original Main.
Compare exact recorded public tuples/APK/OS/PID; delete only owned hash-matched native XML captures.

Reported level1 is Trusted Environment; level2 is StrongBox. A reported level is OS metadata,
not remote key attestation, a compromised-OS guarantee or proof about Main's existing aliases.
See [Android KeyInfo](https://developer.android.com/reference/android/security/keystore/KeyInfo)
and [per-app Keystore isolation](https://developer.android.com/privacy-and-security/keystore).
No authentication-bound biometric, power-loss, OEM transfer, API24/16KiB or iOS acceptance follows.

The D-483 physical run completes creation, cold binding/tamper checks and original retirement
with source identities retained, Main unchanged and helper removed. These native scenarios are
not a new managed test count. See
[bounded physical evidence](../../../src/Apps/Zanance/docs/quality/physical-key-protection.md).
