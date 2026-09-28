using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Testing;

namespace Vafadar.Zanance.Data.Tests;

public sealed class ZananceDataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;

    public ZananceDataTests()
    {
        _services = new ServiceCollection()
            .AddZananceData(_directory.Combine("zanance.db"))
            .BuildServiceProvider();
    }

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    public void Startup_migration_creates_the_database()
    {
        // This is exactly what the app runs at startup. It fails if the model has changes without a migration.
        _services.MigrateLocalDatabase<ZananceDbContext>();

        using var context = _services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContext();
        Assert.Empty(context.Database.GetPendingMigrations());
        Assert.True(File.Exists(_directory.Combine("zanance.db")));
    }

    [Fact]
    public void The_database_is_included_in_backups()
    {
        Assert.Single(_services.GetServices<IBackupSource>());
    }
}
