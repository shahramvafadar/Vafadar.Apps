using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
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
    /// the auditing interceptor, and the database as an <see cref="IBackupSource"/>. The file can be moved later through
    /// <see cref="LocalDatabaseLocation{TContext}"/> (local profiles).
    /// </summary>
    /// <remarks>Use the factory (short-lived contexts) in apps; there are no request scopes in a mobile app.</remarks>
    public static IServiceCollection AddLocalDatabase<TContext>(this IServiceCollection services, string databasePath)
        where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AuditingSaveChangesInterceptor>();
        services.AddSingleton(new LocalDatabaseLocation<TContext>(databasePath));
        services.AddSingleton<IDbContextFactory<TContext>, LocalDbContextFactory<TContext>>();
        services.AddSingleton<IBackupSource, SqliteDatabaseBackupSource<TContext>>();

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations synchronously. Use this from synchronous startup code (e.g. where the app
    /// creates its first window); blocking on the async overload can deadlock on a UI thread.
    /// </summary>
    public static void MigrateLocalDatabase<TContext>(this IServiceProvider services)
        where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        using var context = services.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext();

        // Most starts find the database up to date. Reading the migration history is cheap; Migrate() itself also takes
        // a lock and compares the whole model with the migrations, which costs seconds on a phone.
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }

    /// <summary>Applies pending EF Core migrations.</summary>
    public static async Task MigrateLocalDatabaseAsync<TContext>(this IServiceProvider services, CancellationToken cancellationToken = default)
        where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var factory = services.GetRequiredService<IDbContextFactory<TContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
