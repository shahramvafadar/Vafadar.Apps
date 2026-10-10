# Vafadar.Data

On-device SQLite databases with EF Core. Design: [docs/architecture/data-and-backup.md](../../../docs/architecture/data-and-backup.md).

| Type | Purpose |
|---|---|
| `LocalDbContext` | Base `DbContext` with SQLite conventions (`DateTimeOffset` as UTC ticks) |
| `Auditing.AuditingSaveChangesInterceptor` | Sets `CreatedAt` / `UpdatedAt` of `IAuditableEntity` using `TimeProvider` |
| `SqliteDatabaseBackupSource<TContext>` | Includes the database in backups (online backup API, integrity check, migrate after restore) |
| `DataServiceCollectionExtensions` | `AddLocalDatabase<TContext>(path)`, `MigrateLocalDatabase<TContext>()` |
| `LocalDatabaseLocation<TContext>` | The database file in use; `MoveTo(path)` switches it at run time (local profiles), contexts created afterwards use the new file |

```csharp
public sealed class ZananceDbContext(DbContextOptions<ZananceDbContext> options) : LocalDbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
}

services.AddLocalDatabase<ZananceDbContext>(Path.Combine(FileSystem.AppDataDirectory, "zanance.db"));

// startup (synchronous, safe on the UI thread)
serviceProvider.MigrateLocalDatabase<ZananceDbContext>();

// usage
await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
```

Store money as `long` minor units: SQLite cannot sum or compare `decimal` values in SQL.

## Actual destination and cached backup checks (D-124)

SqliteDatabaseBackupSource captures the actual context before restore stream input, then copies and migrates that
same file even if LocalDatabaseLocation moves. Existing IBackupSource calls keep their signatures. Optional overloads
accept Action<TContext> cached authorization callbacks before input and native copying/output; callbacks must not
move the database, do network work or start an external SQLite writer transaction. The library has no commercial
policy dependency. Integrity checking and temporary snapshot cleanup remain intact. A later migration failure may
follow a completed native replacement; callers still provide a safety copy before restore.
