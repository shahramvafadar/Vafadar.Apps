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

For a focused search/save qualification after changing those benchmark operations, use a new
root and `ledger`; it omits previously qualified resource-read/calculation timing cohorts:

```powershell
dotnet run --project eng/benchmarks/Zanance.ProtectedPerformance -c Release --no-build -- artifacts/protected-ledger-new ledger
```

The default/full run includes these ledger operations too. Four original search/group samples
cover a known unique transfer, no match, a numeric query and the whole fixture; complete result
and no-duplicate checks are inside that timer. Validated SaveEntryAsync has four separate timed
transactions, each followed by actual stored-row read-back and deletion of exactly that row.
Read-back/cleanup are outside the save timer. The final complete verifier must still match every
original schema/header/typed value against the retained pre-migration encrypted rollback.

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
Windows benchmark runs in the dedicated Windows lane of Native cipher candidates (D-279). Its
fresh runner uses the same reviewed toolchain and immutable package checks; it does not run MAUI.
Only measurement JSON is uploaded, never DPAPI envelopes or financial fixture files. See
[the evidence report](../../../src/Apps/Zanance/docs/quality/protected-workload-performance.md).

## Focused complete Home comparison (D-283)

Use `dotnet run --project eng/benchmarks/Zanance.ProtectedPerformance -c Release --no-build -- artifacts/protected-ledger-d280 home`
after building. Both existing marked protected workload folders are required; missing data or keys never authorize
regeneration. Four original/new operation samples include original validation and full packet comparison; valuation
rate reads follow the actual enabled/multi-currency boundary. No search/save/unchanged timing cohort is repeated.
Complete final logical preservation is still verified. Each run writes a separate home-measurements-<id>.json.
Normal full hosted runs include the current complete Home read/validation cohort; ledger mode remains unchanged.
The comparison is not MAUI rendering, full cold launch or a physical-device two-second measurement.

## D-523 - Preserve financial data across deliberate round-trip writes

HomeFinancialRevision is an opaque local concurrency token. A successful Save followed by Delete must
change it even when all money and rows return to their original values. The benchmark comparator checks
the original singleton integer identity and complete lowercase hexadecimal token, requires a changed
token only after accepted writes, and normalizes exactly that field in a disposable memory copy.
The unchanged production SqliteSnapshotVerifier still compares complete schema, headers, triggers and
every other typed value. Original snapshots, financial values and production restore validation remain
strict. Missing/extra/malformed revision rows, financial edits, schema/header changes and an incorrect
accepted-write boundary must refuse.

After a failed terminal comparison, preserve its existing fictitious generation and original rollback:

```powershell
dotnet run --project eng/benchmarks/Zanance.ProtectedPerformance -c Release --no-build -- artifacts/protected-failed-run verify-retained
```

This performs no financial commands or timing samples and does not create a missing workload. It uses
the actual backup projection on a fresh encrypted copy of the original rollback, retains original-file
digest equality, deletes only that copy, and compares with the current original generation. The actual
protected factory's ReadWrite requirement is preserved; a ReadOnly creator is correctly refused.
No private key or plaintext snapshot is exported. Each result has its own readonly-verification file.

If reference completed its commands but tenfold was never prepared, explicitly continue only tenfold:

```powershell
dotnet run --project eng/benchmarks/Zanance.ProtectedPerformance -c Release --no-build -- artifacts/protected-failed-run tenfold
```

This skips reference and refuses an already existing tenfold folder. It cannot authorize re-running
accepted writes after a later tenfold failure; use verification-only mode for that source. The original
failure, old timings and new continuation receipts remain distinct. Hosted failure attribution requires
its actual logs; a locally reproduced snapshot failure alone is not that attribution.
