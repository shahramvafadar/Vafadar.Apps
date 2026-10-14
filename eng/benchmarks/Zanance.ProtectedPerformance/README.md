# Protected reference and tenfold workload

Windows-only independent executable using the actual Data/Core assemblies, official source-built
SQLCipher Windows package, DPAPI key storage and protected generation/bootstrap implementation.
It links the existing deterministic PerformanceFixture; it is not shipped with the application.
Restore/build the reviewed native package using the repository native-build guide first.

Run from the repository root with a new output leaf under artifacts:

```powershell
dotnet build eng/benchmarks/Zanance.ProtectedPerformance -c Release -p:ContinuousIntegrationBuild=true
dotnet run --project eng/benchmarks/Zanance.ProtectedPerformance -c Release --no-build -- artifacts/protected-new-run
```

The directory must not exist. Redirected ancestors and paths outside artifacts are refused.
Only this executable's marked fictitious root can be resumed explicitly after an actual failure:

```powershell
dotnet run --project eng/benchmarks/Zanance.ProtectedPerformance -c Release --no-build -- artifacts/protected-new-run resume
```

Resume keeps existing generation identities, keys and data. ExistingFixtureResumed distinguishes
revalidation/publication time from fresh protection time. It does not reconstruct a missing fixture.
Keep all fixture/key files private; never point this executable at application data or a real backup.

Reference is 10,000 entries / 20 accounts / 100 manual schedules; tenfold is 100,000 / 200 / 1,000.
Both use seed 42 and the existing 2026-10-09 fixture date. The real first-schema upgrade runs before
protection; fixture generation is outside ProtectedPreparationMs. No automatic financial posting,
reminder, real credential, app provider registration or production data is involved.

Each production read/validation or ledger calculation records its first sample and three warmed
samples separately. The original portable source is compared against the authenticated retained
pre-migration encrypted rollback using the original complete schema/header/integrity/typed-row
verifier. Raw file equality is inappropriate because portable export rebuilds SQLite page layout.
measurements.json records every sample, shape, preservation result and original-file removal.

There is a ten-minute deadline. A failed observation must not cause regeneration of an existing
fixture or replacement keys. D-278's first reference run failed its former raw-byte comparison;
the corrected run resumed that same committed reference and created tenfold for the first time.
Its reference preparation time is therefore reopening, not fresh migration performance.

These Windows native store/calculation measurements do not measure MAUI rendering, Home startup,
search, saving, physical hardware, Android/iOS or Q-02's two-second objective. This independent
Windows benchmark is not part of the Linux-only Native cipher candidates workflow. See
[the evidence report](../../../src/Apps/Zanance/docs/quality/protected-workload-performance.md).
