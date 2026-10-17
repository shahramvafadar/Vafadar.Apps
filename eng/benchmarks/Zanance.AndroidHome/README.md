# Original protected Android Home review

This independent Release/AOT/trimmed x64 caller compiles the original application sources/XAML
and supplied production native package. The application never references it. Its distinct package
is `pro.vafadar.zanance.androidhomereview`; it must never run under the main application identity.
It keeps foreground screenshot protection, has no Internet permission and does not enable debugging.

Build with the existing Android workload and native package:

```powershell
dotnet build eng/benchmarks/Zanance.AndroidHome/Vafadar.Zanance.AndroidHome.csproj -c Release -f net10.0-android -p:ContinuousIntegrationBuild=true
```

Only use the repository-owned `Zanance_Storage_Review` emulator (`emulator-5570`, adb port 5038).
Check its AVD name first. Verify the signed APK's package, signature, embedded AOT and qualified
native-library hash before installing that exact independent package. Resolve its actual activity
with `shell cmd package resolve-activity --brief`; never guess the generated Java activity name.

Launch once with `--es review_phase prepare --es review_run <new-32-hex-guid>`. This creates fresh
private, marked reference (10,000 entries, 20 accounts, 100 schedules) and tenfold (100,000, 200, 1,000)
fixtures through original migrations, then protects them with the actual Android generation/key
implementation. Preparation resolves the Android OS app-data alias before unchanged no-redirect
fixture guards. Existing roots are refused. No main package data is read or moved.

Record the exact PID and observe its public report, without clearing logcat or reading private files:

```powershell
./eng/benchmarks/Zanance.AndroidHome/Read-ReviewReport.ps1 -Run <guid> -Phase prepare -ProcessId <pid> -OutputPath <fresh-artifacts-file.json>
```

The observer only reads the exact live PID/tag/run's bounded public report. Each message holds an
indexed Base64 fragment of derived JSON, not key material or exception messages. The decoder refuses
conflicting/missing chunks and overwriting output. Observation timeout does not restart a process.

After successful preparation, force-stop only the independent package and launch its `measure`
phase with the same run id and a fresh `--es review_pass <32-hex-comparison>` identity. Record the new exact PID
and observe with `-Phase measure -Comparison <comparison>`. Measurement
requires existing ciphertext/identities/keys, never reseeds data or creates replacement keys. Failed
attempt reports and private failure files remain retained. Fix the observed cause, then continue
the same prepared fixtures; do not rerun preparation or reset app data. Completed language/shape
cases in private progress are retained and skipped; full final preservation still runs for both
shapes. Never reuse completed metrics after changing production sources; that requires a separately
identified comparison against the same retained financial data. `measure` captures the canonical financial
presentation; after an intended production change, `compare` uses a fresh `review_pass` and an explicit
`review_baseline` naming the original presentation packets. Omitting `review_baseline` retains the original
same-id behavior. Separate comparison identities prevent stale completed cases from becoming new timings.
Per-comparison progress and immutable final report names preserve every prior
result. Each final result records the loaded Data/caller module identities; do not reuse timings after a rebuild.

The original Home page runs twice in English, Persian and German for each shape. Navigation,
read/projection/publication and actual Android layout/frame callbacks are timed; complete native
balance text and actual covered publication are checked. It also measures dispatcher gaps and
compares the actual protected database directly to its existing authenticated encrypted rollback after timing,
including complete schema/header/integrity/typed rows. No in-memory backup image is held during measurement.
The raw read profile separates physical stepping from entity materialization, and reports the actual compiled
ledger constructor parameters without exposing keys or rows. No financial Save occurs. Stop the exact independent package
after reading the completed result. Preserve its fixtures for later same-data comparisons.

This is a loaded emulator baseline, not cold startup, hardware-rendered/GPU frame completion,
physical phone speed/ANR, arm64/API24/16 KiB OS, iOS or two-second product acceptance. Do not run
compilers or intrusive UI observers while measuring. No unchanged production APK/full test suite
needs repetition for this independent caller alone.
