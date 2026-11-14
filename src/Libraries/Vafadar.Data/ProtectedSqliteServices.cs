using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Data;

/// <summary>Shares SQLite services without storing key-bearing context options in the framework's global provider cache.</summary>
public static class ProtectedSqliteServices
{
    private static class ContextServices<TContext> where TContext : DbContext
    {
        // Different context types may configure different singleton warnings/model policies.
        // Retain their original separation without using any connection or options as a cache key.
        /// <summary>Owns only fixed infrastructure registrations for this context type, never context options.</summary>
        internal static readonly Lazy<ServiceProvider> Instance = new(
            () => new ServiceCollection().AddEntityFrameworkSqlite().BuildServiceProvider());
    }

    /// <summary>Uses key-free shared infrastructure while retaining this context's original connection, model and interceptors.</summary>
    /// <remarks>
    /// The default framework cache retains its first complete options key, including an externally supplied
    /// SQLite connection. A closed connection can still hold its connection options and registered callbacks.
    /// Supply only the fixed SQLite service graph here; never place a context, connection or profile in it.
    /// Per-context publication/command/transaction guards continue to come from the original options.
    /// </remarks>
    public static DbContextOptionsBuilder<TContext> UseProtectedSqliteServices<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseInternalServiceProvider(ContextServices<TContext>.Instance.Value);
    }
}
