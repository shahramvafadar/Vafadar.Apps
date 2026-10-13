using Microsoft.EntityFrameworkCore;

namespace Vafadar.Data;

/// <summary>The original local factory with explicit context creation inside captured maintenance ownership.</summary>
public interface ILocalDbContextFactory<TContext> : IDbContextFactory<TContext> where TContext : LocalDbContext
{
    /// <summary>Creates an owned context while ordinary factory work remains excluded.</summary>
    TContext CreateDbContext(DatabaseAccessGate.ExclusiveLease owner);
    /// <summary>Creates an owned context with native credential construction off the calling thread.</summary>
    Task<TContext> CreateDbContextAsync(DatabaseAccessGate.ExclusiveLease owner, CancellationToken cancellationToken = default);
}
