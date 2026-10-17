# Original protected Android Home review

This independent Release/AOT/trimmed caller (x64 emulator by default, explicit arm64 phone override) compiles the original application sources/XAML
and supplied production native package. The application never references it. Its distinct package
is `pro.vafadar.zanance.androidhomereview`; it must never run under the main application identity.
It keeps foreground screenshot protection, has no Internet permission and does not enable debugging.

Build with the existing Android workload and native package:

```powershell
dotnet build eng/benchmarks/Zanance.AndroidHome/Vafadar.Zanance.AndroidHome.csproj -c Release -f net10.0-android -p:ContinuousIntegrationBuild=true
```

By default use only the repository-owned `Zanance_Storage_Review` emulator (`emulator-5570`, adb port 5038).
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


The D-306 `guard` phase uses the same run and explicit review_baseline with a fresh review_pass. It checks
one original native English Home publication per shape, rejects changed in-memory plan/goal reviews and
accepts that same review after exact restoration. It compares the complete raw original database afterward.
No DB Save, new preparation/key, repeated native-stepping profile or statistical performance claim belongs
to this phase. Observe its exact process with Read-ReviewReport.ps1 -Phase guard -Comparison <original-pass>.

## Original native saved-filter actions

The `filters` phase qualifies the original TransactionsPage/TransactionsViewModel and unchanged saved-query
commands using a separate fictitious `filter-actions.db` under its explicit review run. It does not prepare
or measure the protected Home reference/tenfold workloads. Pass an explicit run/comparison to MainActivity,
then use Read-ReviewReport.ps1 with the same `-Phase filters`, `-Run`, `-Comparison` and actual `-ProcessId`.

The native original Shell must attach before theme/language root changes; logical publication alone does
not prove Android fragment attachment. Real accessibility nodes, visible native rectangles, caption layouts
and native button clicks are checked in en/fa/de and light/dark. Complete original fixture packets must be
unchanged after setup. No main data, system setting, security flag or financial Save belongs to selection.

After an actual failed caller, preserve its packet, original run directory and exact financial inputs. A
corrected caller can continue that same fixture after explicit value validation, with a fresh comparison
identity preserving the immutable failure packet. Completed comparisons refuse repetition. Observation
failure does not prove process death: verify the original authoritative process rather than restarting it.

## Original protected schema callback (D-309)

The `schema` phase takes the already prepared protected reference/tenfold run and a fresh `review_pass`.
It refuses absent original ciphertext, identities or keys and calls the actual production
ProtectedProfileStorage.UpgradeAsync on the original closed unpooled keyed connection under drained ownership.
It reports physical Open state transitions and callback elapsed time, then compares complete raw schema,
headers, integrity and typed rows against the existing encrypted rollback. It never prepares a replacement
fixture, saves money or changes the KDF. Observe its exact live PID with `-Phase schema -Comparison <pass>`.

For a production change, build a new caller and use a fresh comparison against the same retained run. Keep
before/after process/module identities and failure packets. A timeout is not process death. This phase is
not whole Home startup, rendered-frame, statistical timing, phone, iOS or two-second acceptance.

## Original verified purchase lifecycle (D-310)

The `billing-owned` phase uses a fresh explicit run/comparison and an independent SQLite fixture. Its
fictitious signed-metadata effect port never calls a store, payment, restore or management. The actual
production ProductPlansPage/view model/formatter render paid-through, grace, scheduled change, Lifetime and
cached-verification facts in en/fa/de and light/dark. Check original Android text layouts/ellipsis/geometry
and original stored settings/accounts/entries; never weaken flags, change device settings or use main data.
Observe the same exact live PID with `-Phase billing-owned -Comparison <pass>`. Preserve failure packets
and continue the same fixture after a caller correction; completed comparisons cannot be repeated. This
is native presentation qualification, not genuine signed-provider/store or physical/iOS acceptance.

## Original financial tables (D-311)

The `tables` phase uses a fresh explicit run/comparison and independent fictitious SQLite file. It renders
actual ReportsPage trend/history and SnapshotPage comparison/decomposition templates with canonical
MoneyText packets and original date formatting. Check native full text allocations, horizontal sign/unit
scroll ends and original settings/accounts/entries in en/fa/de and light/dark. In-memory stress packets do
not replace financial algorithms or persisted snapshots. No financial command or real source/provider call
belongs to the phase. Observe the exact live PID with `-Phase tables -Comparison <pass>`; timeouts retain it.
Never reset main data, recreate a failed fixture or change system settings/security flags. Completed
comparisons refuse repetition. This is not current phone, OS scale, reader, iOS or product acceptance.


## Owner-approved physical Android workload (D-314)

The owner authorized Samsung SM-A176B for fictitious-data qualification. Coordinate an exclusive phone
turn with other projects before installing, launching or driving this package, and release it afterward.
Build the complete independent Release package with the additional -p:RuntimeIdentifier=android-arm64.
Verify its exact independent identity, local signature, AOT, absent INTERNET/debugging and reviewed ARM64
SQLCipher bytes before a data-preserving install. Never clear/reset/uninstall the main application.
No system setting, screenshot-security exception, owner credential or personal data belongs to this run.

Read-ReviewReport.ps1 retains its original emulator guard. Explicit -PhysicalDevice -DeviceTransportId N
selects only the observed approved Samsung model and refuses an emulator; transport metadata is runtime
input, never a committed hardware identifier. Observe the exact PID and public result only. Lost observation
or a timeout never authorizes restarting a live phase or regenerating its ciphertext/keys.

A fresh prepare creates the same independent reference/tenfold workloads. The existing measure phase then
qualifies original loaded Home publication and full raw preservation in en/fa/de; its hardware/runtime
metadata distinguishes physical ARM64 evidence from the earlier x64 baseline. These timings exclude the
initial empty-install startup and preparation, not proof of complete loaded cold startup or the two-second target.

The new ledger phase uses that same retained run and a fresh review_pass. Original TransactionsPage and
native SearchView deliver exact/empty/wide queries through their real debounce, grouping and native layout;
the exact row must be realized. Four original validated store saves per shape are timed separately, read
back, then only their exact owned rows are removed. This is the production data write, not a timed native
Save-button/form interaction. Complete portable schema/typed rows before/after must match. Private sync
journal evolution is deliberately distinct from the unchanged financial portable projection. Before Save,
the exact fictitious pending intent is retained privately. If interrupted, preserve it for explicit same-fixture
inspection rather than guessing cleanup or repeating Save. Completed shapes are retained and skipped.

Largest dispatcher gaps cover the actual ledger phase and caller assertions; they are diagnostic samples,
not a statistical Android ANR or low-memory acceptance. Full cold startup, OS pressure, genuine provider,
spoken accessibility, other API/page-size matrices and iOS remain separate requirements.


## Same-data read diagnosis and read-only native ledger (D-315)

read-profile requires the retained protected run and a fresh review_pass. Set review_baseline to that
same pass for the initial actual-model timestamp/raw-CLR diagnosis. A new after pass uses the original
baseline identity and skips those already qualified component loops. Both actual public reads always
compare complete ordered entries and portable schema/typed rows before/after. Stream JSON digests to
a null sink, never a plaintext financial file. Compare original and final digests on the host.

ledger-read uses a fresh pass on that same run and original native TransactionsPage/SearchView/grouping.
It retains the ledger phase's exact/empty/wide native and portable-preservation checks, but deliberately
executes zero successful financial writes again. Per-shape progress validates the expected 0/4 write
count before continuation. Observe exact live PIDs with -Phase read-profile or ledger-read, same Run and
Comparison, explicit PhysicalDevice/DeviceTransportId. No reseeding, reset or new keys; observation timeout
is not process death. Finished comparisons refuse repetition. Actual receipts: app quality/physical-ledger-read.md.
