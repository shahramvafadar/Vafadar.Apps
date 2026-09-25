using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vafadar.Backup;
using Vafadar.Data.Auditing;

namespace Vafadar.Data;

/// <summary>
/// Registers on-device SQLite databases.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDbContextFactory{TContext}"/> for a SQLite database at <paramref name="databasePath"/>,
    /// the auditing interceptor, and the database as an <see cref="IBackupSource"/>.
    /// </summary>
    /// <remarks>Use the factory (short-lived contexts) in apps; there are no request scopes in a mobile app.</remarks>
    public static IServiceCollection AddLocalDatabase<TContext>(this IServiceCollection services, string databasePath)
        where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AuditingSaveChangesInterceptor>();
        services.AddDbContextFactory<TContext>((sp, options) =>
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            options
                .UseSqlite(connectionString)
                .AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
        });
        services.AddSingleton<IBackupSource, SqliteDatabaseBackupSource<TContext>>();

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations synchronously. Use this from synchronous startup code
    /// (e.g. an <c>IMauiInitializeService</c>); blocking on the async overload can deadlock on a UI thread.
    /// </summary>
    public static void MigrateLocalDatabase<TContext>(this IServiceProvider services)
        where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        using var context = services.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext();
        context.Database.Migrate();
    }

    /// <summary>Applies pending EF Core migrations.</summary>
    public static async Task MigrateLocalDatabaseAsync<TContext>(this IServiceProvider services, CancellationToken cancellationToken = default)
        where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var factory = services.GetRequiredService<IDbContextFactory<TContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
