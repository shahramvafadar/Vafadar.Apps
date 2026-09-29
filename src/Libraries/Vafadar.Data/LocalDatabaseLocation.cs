using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data.Auditing;

namespace Vafadar.Data;

/// <summary>
/// The file of an on-device database. Apps with several local profiles move it at run time: contexts created after
/// <see cref="MoveTo"/> use the new file, so every store and backup source follows without being rebuilt.
/// </summary>
/// <typeparam name="TContext">The database the location belongs to.</typeparam>
public sealed class LocalDatabaseLocation<TContext>
    where TContext : LocalDbContext
{
    private readonly Lock _gate = new();

    /// <summary>Creates the location with its first file.</summary>
    public LocalDatabaseLocation(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = System.IO.Path.GetFullPath(path);
    }

    /// <summary>Raised after the database moved to another file.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the full path of the database file in use.</summary>
    public string Path { get; private set; }

    /// <summary>
    /// Uses the database at <paramref name="path"/> from now on. Pooled connections to the old file are closed; apply
    /// migrations to the new file before reading it.
    /// </summary>
    public void MoveTo(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = System.IO.Path.GetFullPath(path);
        lock (_gate)
        {
            if (string.Equals(full, Path, StringComparison.Ordinal))
            {
                return;
            }

            SqliteConnection.ClearAllPools();
            Path = full;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Creates contexts for the file the <see cref="LocalDatabaseLocation{TContext}"/> points to.</summary>
internal sealed class LocalDbContextFactory<TContext>(
    IServiceProvider services,
    LocalDatabaseLocation<TContext> location,
    AuditingSaveChangesInterceptor interceptor) : IDbContextFactory<TContext>
    where TContext : LocalDbContext
{
    private static readonly ObjectFactory Create = ActivatorUtilities.CreateFactory(typeof(TContext), [typeof(DbContextOptions<TContext>)]);
    private readonly ConcurrentDictionary<string, DbContextOptions<TContext>> _options = new(StringComparer.Ordinal);

    public TContext CreateDbContext() => (TContext)Create(services, [_options.GetOrAdd(location.Path, Build)]);

    private DbContextOptions<TContext> Build(string path)
    {
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        return new DbContextOptionsBuilder<TContext>().UseSqlite(connectionString).AddInterceptors(interceptor).Options;
    }
}
