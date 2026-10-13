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
    private Endpoint _endpoint;
    private bool _protectionRequired;

    /// <summary>Creates the location with its first file.</summary>
    public LocalDatabaseLocation(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _endpoint = new(System.IO.Path.GetFullPath(path), null, null);
    }

    /// <summary>Raised after the database moved to another file.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the full path of the database file in use.</summary>
    public string Path { get { lock (_gate) return _endpoint.Path; } }

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
            if (_protectionRequired) throw new InvalidOperationException("Protected databases require a bound protected publication.");
            if (string.Equals(full, _endpoint.Path, StringComparison.Ordinal))
            {
                return;
            }

            LocalDatabasePools.ClearFile(_endpoint.Path);
            _endpoint = new(full, null, null);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Atomically publishes a verified encrypted generation and its captured key/access connection creator.</summary>
    /// <remarks>Caller verifies the generation first and owns key lifetime/access validation. The creator returns a new
    /// closed, unpooled ReadWrite connection to this exact existing file, never a plaintext fallback or Create mode.</remarks>
    public void MoveToProtected(string path, Func<SqliteConnection> createConnection, string cipherVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); ArgumentNullException.ThrowIfNull(createConnection);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        var full = System.IO.Path.GetFullPath(path);
        lock (_gate)
        {
            LocalDatabasePools.ClearFile(_endpoint.Path);
            _protectionRequired = true;
            _endpoint = new(full, createConnection, cipherVersion);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Captures one atomic path/credential publication before context construction.</summary>
    internal Endpoint Capture() { lock (_gate) return _endpoint; }
    /// <summary>Rejects retired publications, including same-path credential replacement.</summary>
    internal void RequireCurrent(Endpoint captured)
    {
        lock (_gate)
            if (!ReferenceEquals(captured, _endpoint)) throw new InvalidOperationException("Captured protected database access retired.");
    }

    /// <summary>One immutable publication; its object identity also retires old same-path key generations.</summary>
    internal sealed record Endpoint(string Path, Func<SqliteConnection>? CreateConnection, string? CipherVersion);
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

    /// <inheritdoc />
    public TContext CreateDbContext() => CreateCaptured(location.Capture());

    /// <inheritdoc />
    public Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var captured = location.Capture();
        return captured.CreateConnection is null ? Task.FromResult(CreateCaptured(captured))
            : Task.Run(() => CreateCaptured(captured), cancellationToken);
    }

    private TContext CreateCaptured(LocalDatabaseLocation<TContext>.Endpoint captured)
    {
        if (captured.CreateConnection is null) return (TContext)Create(services, [_options.GetOrAdd(captured.Path, Build)]);
        location.RequireCurrent(captured);
        var connection = captured.CreateConnection();
        try
        {
            location.RequireCurrent(captured);
            var options = new SqliteConnectionStringBuilder(connection.ConnectionString);
            if (connection.State != System.Data.ConnectionState.Closed || options.Mode != SqliteOpenMode.ReadWrite || options.Pooling ||
                string.IsNullOrEmpty(options.Password) || System.IO.Path.GetFullPath(options.DataSource) != captured.Path)
                throw new InvalidOperationException("Protected creator returned an unsafe or foreign connection.");
            Action validate = () => location.RequireCurrent(captured);
            ProtectedSqliteAccess.Register(connection, validate);
            // D-197: never cache a key-bearing options/connection string by path across key/profile publications.
            var configured = new DbContextOptionsBuilder<TContext>().UseSqlite(connection, contextOwnsConnection: true)
                .AddInterceptors(interceptor, new ProtectedSqliteConnectionInterceptor(captured.Path, captured.CipherVersion!, validate),
                    new ProtectedSqliteCommandInterceptor(validate)).Options;
            return (TContext)Create(services, [configured]);
        }
        catch { connection.Dispose(); throw; }
    }

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
