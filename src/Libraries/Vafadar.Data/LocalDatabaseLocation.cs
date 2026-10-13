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

    /// <summary>Shared by every context from this location, including all local profile publications.</summary>
    public DatabaseAccessGate Access { get; } = new();

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

    /// <summary>Captures the complete current publication for rollback under this exact maintenance owner.</summary>
    /// <remarks>The host retains underlying key owners until the transition commits or rolls back.</remarks>
    public Publication CapturePublication(DatabaseAccessGate.ExclusiveLease owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        lock (_gate) { Access.RequireOwner(owner); return new(this, _endpoint, owner); }
    }

    /// <summary>One-use rollback of a captured path and bound connection creator; no credentials are exposed.</summary>
    public sealed class Publication
    {
        private readonly LocalDatabaseLocation<TContext> location;
        private readonly Endpoint endpoint;
        private readonly DatabaseAccessGate.ExclusiveLease capturedOwner;
        private bool restored;

        /// <summary>Captures the existing endpoint and the sole maintenance owner allowed to restore it.</summary>
        internal Publication(LocalDatabaseLocation<TContext> location, Endpoint endpoint, DatabaseAccessGate.ExclusiveLease owner)
        { this.location = location; this.endpoint = endpoint; capturedOwner = owner; }

        /// <summary>Restores the captured endpoint without reviving retired context identity or permitting plaintext fallback.</summary>
        public void Restore(DatabaseAccessGate.ExclusiveLease owner)
        {
            lock (location._gate)
            {
                if (!ReferenceEquals(owner, capturedOwner)) throw new InvalidOperationException("Publication rollback belongs to another maintenance owner.");
                location.Access.RequireOwner(owner);
                if (restored) throw new InvalidOperationException("Publication rollback was already consumed.");
                if (location._protectionRequired && endpoint.CreateConnection is null)
                    throw new InvalidOperationException("Protected publication cannot roll back to plaintext.");
                LocalDatabasePools.ClearFile(location._endpoint.Path);
                // A new identity keeps old protected contexts retired even when the same path/key is restored.
                location._endpoint = new(endpoint.Path, endpoint.CreateConnection, endpoint.CipherVersion);
                restored = true;
            }
            location.Changed?.Invoke(location, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Uses the database at <paramref name="path"/> from now on. Pooled connections to the old file are closed; apply
    /// migrations to the new file before reading it.
    /// </summary>
    public void MoveTo(string path, DatabaseAccessGate.ExclusiveLease? owner = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = System.IO.Path.GetFullPath(path);
        lock (_gate)
        {
            Access.RequirePublication(owner);
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
    public void MoveToProtected(string path, Func<SqliteConnection> createConnection, string cipherVersion,
        DatabaseAccessGate.ExclusiveLease? owner = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); ArgumentNullException.ThrowIfNull(createConnection);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        var full = System.IO.Path.GetFullPath(path);
        lock (_gate)
        {
            Access.RequirePublication(owner);
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
    AuditingSaveChangesInterceptor interceptor) : ILocalDbContextFactory<TContext>
    where TContext : LocalDbContext
{
    private static readonly ObjectFactory Create = ActivatorUtilities.CreateFactory(typeof(TContext), [typeof(DbContextOptions<TContext>)]);
    private readonly ConcurrentDictionary<string, DbContextOptions<TContext>> _options = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public TContext CreateDbContext()
    {
        var lease = location.Access.EnterRead(); return CreateLeased(location.Capture(), lease, null);
    }

    /// <inheritdoc />
    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var lease = await location.Access.EnterReadAsync(cancellationToken).ConfigureAwait(false);
        var captured = location.Capture();
        try
        {
            return captured.CreateConnection is null ? CreateLeased(captured, lease, null)
                : await Task.Run(() => CreateLeased(captured, lease, null), cancellationToken).ConfigureAwait(false);
        }
        catch { lease.Dispose(); throw; }
    }

    /// <inheritdoc />
    public TContext CreateDbContext(DatabaseAccessGate.ExclusiveLease owner)
    {
        var lease = location.Access.EnterOwnedContext(owner); return CreateLeased(location.Capture(), lease, owner);
    }

    /// <inheritdoc />
    public async Task<TContext> CreateDbContextAsync(DatabaseAccessGate.ExclusiveLease owner, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lease = location.Access.EnterOwnedContext(owner); var captured = location.Capture();
        try { return await Task.Run(() => CreateLeased(captured, lease, owner), cancellationToken).ConfigureAwait(false); }
        catch { lease.Dispose(); throw; }
    }

    private TContext CreateLeased(LocalDatabaseLocation<TContext>.Endpoint captured, IDisposable lease,
        DatabaseAccessGate.ExclusiveLease? owner)
    {
        try
        {
            var context = CreateCaptured(captured, owner); context.AttachAccessLease(lease); return context;
        }
        catch { lease.Dispose(); throw; }
    }

    private TContext CreateCaptured(LocalDatabaseLocation<TContext>.Endpoint captured, DatabaseAccessGate.ExclusiveLease? owner)
    {
        if (owner is not null) location.Access.RequireOwner(owner);
        if (captured.CreateConnection is null)
        {
            var options = _options.GetOrAdd(captured.Path, Build);
            if (owner is not null) options = new DbContextOptionsBuilder<TContext>(options)
                .AddInterceptors(new ProtectedSqliteCommandInterceptor(() =>
                { location.RequireCurrent(captured); location.Access.RequireOwner(owner); })).Options;
            return (TContext)Create(services, [options]);
        }
        location.RequireCurrent(captured);
        var connection = captured.CreateConnection();
        try
        {
            location.RequireCurrent(captured);
            var options = new SqliteConnectionStringBuilder(connection.ConnectionString);
            if (connection.State != System.Data.ConnectionState.Closed || options.Mode != SqliteOpenMode.ReadWrite || options.Pooling ||
                string.IsNullOrEmpty(options.Password) || System.IO.Path.GetFullPath(options.DataSource) != captured.Path)
                throw new InvalidOperationException("Protected creator returned an unsafe or foreign connection.");
            Action validate = () => { location.RequireCurrent(captured); if (owner is not null) location.Access.RequireOwner(owner); };
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
