# Isolated physical accountant export receiver

This independent .NET Android fixture verifies the original native sharing grant and received fictitious
accountant ZIP. It is not referenced by the application and must never become a production dependency.
The intentionally debuggable receiver has no declared network permissions or backup, removes inherited
AppSecret items and writes only its own private test storage. Minimum API33, ARM64, no MAUI dependency.

## Build and package

Use the repository's installed official .NET Android SDK and existing local test signing setup:

```powershell
dotnet build eng/tests/AndroidExportReceiver/AndroidExportReceiver.csproj -c Release -t:SignAndroidPackage -p:ContinuousIntegrationBuild=true
```

The complete signed local fixture is under bin/Release/net10.0-android/android-arm64. Verify its application
ID pro.vafadar.zanance.exportreview, signature and absence of network permissions before installing it.
Never confuse it with the full signed production candidate. Build/restore can consume existing local
configuration; do not inspect secret files or generated secret values. No global installation is required.

## Approved physical turn

Coordinate exclusive use of the authorized test phone. Confirm that this exact fixture package is absent
before a fresh installation; never clear an existing unknown fixture. Preserve main application data,
system settings and authentication. Select only fictitious profiles. Never inspect the main private
store, key files or FileProvider files through a debugging bypass.

First launch this fixture explicitly with ACTION_SEND/application/zip but no URI grant. Its private
files/status.json must report Received=false/InvalidOperationException; an immediate missing status may
mean startup has not finished. Observe the same invocation rather than relaunching it after a timeout.

In the original app, choose Germany/year2026 and the D-312 fictitious accounts. Keep notes/payees and
stored documents off. Check empty selection refusal and confirmation Cancel. Explicitly confirm the
reviewed export and choose only Zanance export review in Android's native share sheet (More if necessary).
Capture the generated accountant filename from that same chooser. The receiver finishes automatically;
read only its own files/status.json through adb exec-out run-as pro.vafadar.zanance.exportreview.
Require Received=true, ReadGrant=true and the exact observed filename. Copy the corresponding
files/received-<32-hex-token>.zip from that same owned fixture with binary-safe exec-out, verifying its
reported byte length and SHA256. Do not obtain files from the main package. No contacts or network
recipient should receive either packet.

Repeat only the distinct cash-only disclosure: changing selection invalidates the review; review and
confirm the one transfer/one additional linked identity. Match the second chooser filename to a fresh
receiver receipt. The receiver refuses overwriting a packet token and caps compressed/expanded data at
2 MiB. It accepts exactly seven known non-document members; it does not extract, execute or forward them.
This is deliberately a default/no-document test receiver, not a general ZIP or document viewer.

## Original-packet verification

```powershell
python eng/tests/AndroidExportReceiver/verify_packets.py <all-accounts.zip> <cash-only.zip>
```

The standard-library verifier checks the original D-312 fixture (five known amounts, two account names,
refund reference, grouped fee and one transfer), manifest digests, disclosure flags, account/category
references, blank tax classifications and cash-only exact-row equality. It does not implement the exporter,
open the database or calculate tax. It is fixture-specific; using it on other financial data is unsupported.

Return to the original Home, confirm the original displayed balance and uninstall only this fresh receiver.
Remove only the review's own temporary UI hierarchy files. Release the coordinated device turn. Keep local
fictitious evidence outside Git; never upload private artifacts or change main offered-cache expiry.
See src/Apps/Zanance/docs/quality/physical-accountant-export.md for D-328 results and remaining boundaries.
