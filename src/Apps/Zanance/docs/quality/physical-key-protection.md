# Physical original key protection (D-483)

## Scope and original adapter

Close the outstanding bounded physical key-protection observation independently of Main's
private data. The new permission-free Release ARM64 helper links the actual
AndroidDatabaseKeyProtector and DatabaseKeyBinding source, with no algorithm copy, Main reference,
database, network, account, system-setting or authentication effect. Its own UID owns only new
fictitious generation/recovered envelopes and two native wrapping aliases. Main's existing
aliases are neither enumerated nor opened. No key, envelope, private digest or alias is logged
or copied to the host; private expectations remain inside the helper until its removal.

The complete strict publish succeeds without warnings. APK20825319bytes, SHA256
8588bade9a6ac1b6e64973be94db6a1c1d063ff1c8ff05c346d6d692817351bc,
passes ZIP/package/ARM64/no-permission/non-debuggable checks and v3 signature verification.
The initial host verifier incorrectly demanded v2 as well and refuses before installation.
For this API31-minimum package the actual valid signer uses v3 and reports v2 false. Correct
the observer to the actual required signature, retain the refusal, and use the same already
successful build. No package/signing weakening, regeneration or Main build is performed. Explicit Debug and Main-identity
MSBuild guard evaluations both refuse before preparation.

## One original physical fixture, three completed phases

Samsung SM-A176B is available under D-458. The original baseline records Main27601 with141
public Home tuples,979EUR, APK5b6faeb0 and original font/size/density/accessibility settings.
The helper is absent before installation. One fixed fictitious run is retained throughout:

| Original phase | Actual PID | Verified result |
|---|---|---|
| Create |20062| Original generation and recovered keys unwrap to the original in-memory expectation; no plaintext key persistence/export |
| Cold |20266| Actual helper process death before launch; different PID reopens both original native keys and private encrypted envelopes |
| Retirement |20384| Two denied-authority removals preserve the original keys; two explicit durable-authority removals leave both aliases absent; later unwrap refuses without creating replacements |

Cold validates twelve refusals: changed profile, installation or generation, modified ciphertext,
changed purpose header and truncated envelope, separately for generation and recovered keys.
Original source catches native GCM authentication failure; no substitute crypto implementation
or guessed financial relationship is used. Cold results and original successful creation are
retained and never reseeded/replayed. A first retirement launch is delivered to the old top
Activity rather than OnCreate; its existing code has no OnNewIntent phase dispatcher. Retain
that delivery output and the absence of a retirement result, stop only the owned helper, prove
death and launch only the unexecuted retirement phase. No accepted phase is repeated.

Both aliases report KeyInfo security level1 during original/cold/retirement checks. According
to [Android's KeyInfo contract](https://developer.android.com/reference/android/security/keystore/KeyInfo),
this means Trusted Environment. [Android Keystore isolation](https://developer.android.com/privacy-and-security/keystore)
is per application; this does not claim inspection or hardware attestation of Main's existing
aliases. The original protector also rejects exportable native wrapping keys. These are actual
platform-reported metadata and observed non-exportable operations, not an independent physical
hardware audit, StrongBox claim or compromised-OS guarantee. Authentication-bound keys/biometric
changes and owner real authentication remain separate.

## Restoration, evidence and remaining gates

After original retirement passes, installed helper digest matches the verified APK and only that
new package is uninstalled. Main remains PID27601, all141 original public Home tuples/balance979,
APK and recorded OS settings match, and two owned XMLs are hash-matched and removed. No financial
Save, profile creation, Main installation/restart, owner secret access or unrelated test/build
occurs. This is bounded public restoration, not equality of all private databases/keys.

Three native phase receipts, twelve native refusals and two authority refusals are not new
managed-suite test counts. Actual protected-Main recovery/lifecycle evidence remains as previously
recorded; no repetition is needed for this independent adapter qualification. Production source,
shipping APK, schema, permissions and SDK dependencies stay D-482. Its hosted CI is separate.

This closes this phone's scoped original-adapter security-level/cold/binding/retirement cohort.
It does not close all SEC-03/platform acceptance, remote attestation, owner authentication,
physical power-loss, OEM transfer, API24/16KiB, iOS, broad ANR/crash/accessibility, provider/Store
or release gates. The continuous delivery goal remains active.

Private local receipts: baseline.json, apk-proof.json, accepted-create.json, accepted-cold.json,
accepted-retire.json, final-proof.json and capture-cleanup.json. No private key fixture is uploaded.
