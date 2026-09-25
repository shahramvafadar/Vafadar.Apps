using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Vafadar.Core.Domain;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

internal sealed class Note : Entity, IAuditableEntity
{
    public required string Text { get; set; }

    public DateTimeOffset? RemindAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : LocalDbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // The test model has no migrations; Migrate() (used by restore) must not treat that as an error.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
}

/// <summary>A file based test database registered exactly like an app database.</summary>
internal sealed class TestDatabase : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;

    public TestDatabase()
    {
        _services = new ServiceCollection()
            .AddSingleton<TimeProvider>(Time)
            .AddLocalDatabase<TestDbContext>(_directory.Combine("data/test.db"))
            .BuildServiceProvider();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 9, 25, 8, 0, 0, TimeSpan.Zero));

    public IServiceProvider Services => _services;

    public TestDbContext CreateContext() => _services.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContext();

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }
}
