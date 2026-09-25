# Vafadar.Data

On-device SQLite databases with EF Core. Design: [docs/architecture/data-and-backup.md](../../../docs/architecture/data-and-backup.md).

| Type | Purpose |
|---|---|
| `LocalDbContext` | Base `DbContext` with SQLite conventions (`DateTimeOffset` as UTC ticks) |
| `Auditing.AuditingSaveChangesInterceptor` | Sets `CreatedAt` / `UpdatedAt` of `IAuditableEntity` using `TimeProvider` |
| `SqliteDatabaseBackupSource<TContext>` | Includes the database in backups (online backup API, integrity check, migrate after restore) |
| `DataServiceCollectionExtensions` | `AddLocalDatabase<TContext>(path)`, `MigrateLocalDatabase<TContext>()` |

```csharp
public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : LocalDbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
}

services.AddLocalDatabase<FinanceDbContext>(Path.Combine(FileSystem.AppDataDirectory, "finance.db"));

// startup (synchronous, safe on the UI thread)
serviceProvider.MigrateLocalDatabase<FinanceDbContext>();

// usage
await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
```

Store money as `long` minor units: SQLite cannot sum or compare `decimal` values in SQL.
