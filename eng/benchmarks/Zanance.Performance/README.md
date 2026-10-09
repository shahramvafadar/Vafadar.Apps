# Zanance performance workload

Independent .NET executable for Q-02 / ZCR-QA-06 (D-75). References the actual Core/Data projects, not the MAUI app.
It is not shipped, does not find app data, and creates only new fictitious directories. No SDK or package was added.

```powershell
dotnet build eng/benchmarks/Zanance.Performance -c Release -p:ContinuousIntegrationBuild=true
dotnet run --project eng/benchmarks/Zanance.Performance -c Release --no-build -- artifacts/performance-new-run
```

The output leaf must not exist and its parent must exist. Existing paths and redirected ancestors are rejected.
The database and ownership marker use exclusive creation. SQLite paths are built as connection-string values,
including literal semicolons. Do not point the executable at a real profile or reuse an earlier output directory.

## Workloads and proof

| Workload | Entries | Accounts | Active manual schedules |
|---|---:|---:|---:|
| Reference | 10,000 | 20 | 100 |
| Tenfold | 100,000 | 200 | 1,000 |

EUR checking accounts, fixed date 2026-10-09, deterministic seed 42 and reproducible legacy ids. Entries span the
preceding 1,000 days: 10% income, 10% unlinked refunds, 10% same-currency transfers and 70% expenses. Transfers
retain one identity and a distinct destination; source/destination amounts agree. Positive magnitudes are minor
units. There are no attachments, budgets, holdings, goals, automatic posting, enabled reminders or credentials.
The workload is explicit, not representative of every user's mix or device.

Three fresh child processes per workload create the actual first migration schema and populate legacy columns
with prepared SQL in one transaction. Generation is outside the migration timer. The production pending-migration
path upgrades it. Before/after SHA-256 fingerprints cover every unchanged legacy account/entry field and retained
settings fields, in id order; the retired WeekStart column is intentionally excluded. Any mismatch aborts the run.
Standalone legacy.db is exported before upgrade. Schedules are added after upgrade because the first schema had
no schedule table; legacy migration samples therefore contain the named account/entry counts, not later plans.

Seven warmed samples measure real pending checks, all/month entry materialization, actual EntrySearch plus day
grouping (unique/missing/numeric/broad queries), Home's balance/totals/forecast calculation paths, and validated
SaveEntryAsync. First validated save is separate. Save read-back, cleanup and whole-fixture preservation checks
are outside that timer. Context creation for queued reads captures the original profile. No calculation or
financial validator is copied into the executable. FirstReadAfterWorker is a descriptive first-read observation
in the parent process, not a fresh UI process measurement for each workload.

measurements.json includes raw milliseconds, environment, shapes and preservation proof. Per-run folders contain
standalone current/legacy databases. Database footprint and process working set are observations, not memory limits.
There is no hard two-second assertion here; the calculation regression tests remain separate.

## Native measurements

Console timings do not measure rendering, typing debounce, native navigation, process launch or device acceptance.
Use only separately created benchmark profiles on an isolated emulator and a preserved Windows development data
directory. Never inspect SecureStorage, replace another profile, use an attached phone implicitly, remove
FLAG_SECURE, change system settings or capture the owner's desktop. Restore development files with matching hashes.

Record process-cold samples with warm OS/file caches separately for Windows Debug and Android Release. On Android,
`am start -W` TotalTime is the system first-frame metric; it is not loaded Home. Accessibility hierarchy generation
can be unavailable during loading and adds seconds of observation overhead. Record unavailable observations and
the upper bound at which the native fixture account, balance and attention content appear. Search must verify a
native result row, not only text echoed in the input or a result counter. Saving must verify the new financial row
and updated balance, not merely a button press. Preserve failures and ANR evidence; do not hide them in averages.

See [the Q-02 evidence report](../../../src/Apps/Zanance/docs/quality/performance-q02.md) for measured results and
remaining device/observation limits. The source-linked AT-82 tests run in the normal Core/Data test projects.
