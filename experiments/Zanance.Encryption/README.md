# Zanance encryption feasibility — SEC-01

Independent fictitious-only proof for [proposed ADR 0010](../../docs/adr/0010-database-encryption.md).
Never referenced by Zanance or the main solution. The deprecated bundle is a feasibility comparator, not an approved
production dependency. Production source remains all rights reserved; dependency licences apply to their components.

## Windows

```powershell
New-Item -ItemType Directory -Force artifacts/encryption-proof
$env:ZANANCE_PROOF_RESULT = (Join-Path (Get-Location) 'artifacts/encryption-proof/windows.json')
dotnet test --project experiments/Zanance.Encryption/Windows.Tests/Windows.Tests.csproj -p:ContinuousIntegrationBuild=true
dotnet clean experiments/Zanance.Encryption/Windows.Tests/Windows.Tests.csproj
```

Four AT-75 tests cover native encrypted files/WAL/SHM, missing/wrong keys, integrity, tampering, rotation, export,
EF Core entity round-trip, DPAPI and PBKDF2 authenticated key envelopes. DPAPI test is Windows-only. Use the separate
project explicitly; do not add the legacy bundle to a production project. No secret or connection string is output.

## Android

```powershell
$env:ContinuousIntegrationBuild = 'true'
./eng/scripts/Build-AndroidApk.ps1 -Project experiments/Zanance.Encryption/Android/Android.csproj -Output artifacts/encryption-proof/android
./eng/scripts/Get-SigningInfo.ps1 -Path artifacts/encryption-proof/android/pro.vafadar.zanance.encryptionproof-Signed.apk
# Use a running emulator serial explicitly; never install over the finance application's package.
adb -s emulator-5570 install -r artifacts/encryption-proof/android/pro.vafadar.zanance.encryptionproof-Signed.apk
adb -s emulator-5570 shell am start -n pro.vafadar.zanance.encryptionproof/pro.vafadar.zanance.encryptionproof.MainActivity
adb -s emulator-5570 shell run-as pro.vafadar.zanance.encryptionproof cat files/fictitious-proof/result.json
adb -s emulator-5570 shell run-as pro.vafadar.zanance.encryptionproof cat files/fictitious-proof/key-wrapping.txt
# Repeat after stopping this harness only, to verify device-wrapped profile reopening in another process.
adb -s emulator-5570 shell am force-stop pro.vafadar.zanance.encryptionproof
adb -s emulator-5570 shell am start -n pro.vafadar.zanance.encryptionproof/pro.vafadar.zanance.encryptionproof.MainActivity
```

The fixture writes only under its package files directory and its own Keystore alias. No INTERNET permission or OS
backup. The Release build uses trimming and managed AOT, includes assemblies, and retains debug access solely for
retrieving fixture reports. It is not a production security configuration. Windows EF integration is tested; the
Android probe exercises native/ADO.NET encryption and Keystore, not the full Zanance EF compiled model or UI.

Verified on 2026-10-08 with the existing installed emulator and Google APIs API 36 x86_64 image revision 7. A new
isolated AVD lives under artifacts/encryption-proof/avd, with separate Android user/emulator homes under
artifacts/encryption-proof/android-user; existing AVDs and the main Zanance package were not used. Both first-run
creation and recovery of the same device-wrapped fictitious profile in a new process passed. Database/envelope
hashes stayed unchanged; tampered envelopes were rejected. Stale reports are removed before each run so a
previous PASS cannot be mistaken for a new result. Always select the intended isolated emulator serial explicitly.

Installing an emulator/system image requires owner approval under AGENTS.md. The harness does not download an image,
change device settings, read another package's files or call network/cloud services. Uninstall only this fixture
package to remove its files/Keystore alias. Fictitious Windows run directories live under the dedicated
Path.GetTempPath()/zanance-encryption-proof root; do not delete unrelated temp directories.

Raw evidence is generated under git-ignored artifacts/encryption-proof; reviewed measured results live in ADR 0010.
APK signature is a local test certificate; supply the actual signed APK path if asking for a phone test. Library
notices must be reviewed before distributing a test binary outside this local feasibility environment.
