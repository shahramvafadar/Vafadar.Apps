# Independent original portable restore qualification

This project compiles the original production application and resources under the fixed isolated
Android identity `pro.vafadar.zanance.portablerestorereview`. It has its own build output, application
storage and device keys. The application never references it. It adds no diagnostic activity,
authentication bypass, synthetic financial writer or private-storage observation endpoint.

Build the complete signed offline Release application:

```powershell
./eng/scripts/Build-AndroidApk.ps1 -Offline -Project eng/tests/AndroidPortableRestore/Vafadar.Zanance.AndroidPortableRestore.csproj -Output artifacts/android-portable-restore
```

Verify package identity, signature, complete assemblies and absence of INTERNET/debuggable before
installation. Coordinate the physical test turn; require both fixture identities to be absent before
installing them. Preserve the installed `pro.vafadar.zanance` candidate, data, profiles and OS settings.

Use only newly created fictitious data through original onboarding/forms. For this no-secret fixture,
choose an unprotected portable export after reading its disclosure. Never enter a backup password or
inspect device keys. The independent export receiver's `PortableBackupActivity` accepts only a native
read grant from this fixed fixture's FileProvider, a bounded original `.vbak` name and a plain bounded
ZIP containing its manifest/data entries. It does not read application-private storage or forward files.
Read only that receiver's own packet/receipt; verify original length/hash and manifest checksums.

Retain the exact exported packet at an absent owned Downloads filename before uninstalling **only**
the independently identified source fixture. Reinstall the identical signed APK. Use the original
first-run restore button, native file picker, preview and confirmation; an empty new profile alone is
not an uninstall/reinstall test. Compare all known original public data and portable preferences and
confirm that no extra account is forced. Preserve accepted actions through an observation timeout.

After the planned terminal cold read, use the same original exporter once on the restored fixture.
Compare those two explicitly received original files without extracting a database onto disk:

```powershell
python eng/tests/AndroidPortableRestore/verify_roundtrip.py <owned-source.vbak> <owned-restored.vbak>
```

This verifier accepts only the D-403 known account37/expense2/Once9-skipped/Monday fixture. It checks
every manifest checksum, SQLite integrity, the complete exported schema and typed rows in every
table, including empty tables, original financial identities and the single original account.
The sole permitted row-field difference is a nondecreasing existing Settings.UpdatedAt value from
the original first-run completion. All other settings and portable display preferences must match.
It never reads the installed application's private store, device keys or an unrelated backup.

At completion, delete only hash-matched fixture exports and the two fresh fixture packages. Restore
the original Main task and verify its candidate, public balance/profiles and OS values. A same-phone
independent reinstall is not another physical device, iOS, a lost-key recovery test, protected portable
password acceptance, a power-loss test or full product release acceptance.
