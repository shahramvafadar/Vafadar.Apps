using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
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
    public void Compiled_model_matches_the_model_in_code()
    {
        // The app loads the compiled model in CompiledModel/ instead of building it at every start (D-44). After a model
        // change regenerate it with `dotnet ef dbcontext optimize` (docs/architecture/data-and-backup.md).
        using var context = _services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContext();
        Assert.IsType<CompiledModel.ZananceDbContextModel>(context.Model);

        // The read-optimized model keeps only what queries and saving need, so both are compared on that: tables,
        // columns with their store types, keys, foreign keys and indexes.
        var fromCode = context.GetService<IDesignTimeModel>().Model;
        Assert.Equal(Describe(fromCode), Describe(context.Model));
    }

    private static string Describe(IModel model) => string.Join(
        Environment.NewLine,
        model.GetEntityTypes().OrderBy(entity => entity.Name, StringComparer.Ordinal).Select(entity => string.Join(
            Environment.NewLine,
            new[] { $"{entity.Name} -> {entity.GetTableName()}" }
                .Concat(entity.GetProperties().Select(p =>
                    $"  {p.Name} {p.ClrType.Name} {p.GetColumnName()} {p.GetRelationalTypeMapping().StoreType}{(p.IsNullable ? " null" : string.Empty)}"))
                .Concat(entity.GetKeys().Select(k => $"  key {string.Join(',', k.Properties.Select(p => p.Name))}").Order(StringComparer.Ordinal))
                .Concat(entity.GetForeignKeys().Select(f =>
                    $"  fk {string.Join(',', f.Properties.Select(p => p.Name))} -> {f.PrincipalEntityType.Name} {f.DeleteBehavior}").Order(StringComparer.Ordinal))
                .Concat(entity.GetIndexes().Select(i =>
                    $"  index {string.Join(',', i.Properties.Select(p => p.Name))}{(i.IsUnique ? " unique" : string.Empty)}").Order(StringComparer.Ordinal)))));

    [Fact]
    public void The_database_is_included_in_backups()
    {
        Assert.Single(_services.GetServices<IBackupSource>());
    }
}
