using Microsoft.EntityFrameworkCore;
using Vafadar.Data.Conventions;

namespace Vafadar.Data;

/// <summary>
/// Base class for an app's on-device SQLite database.
/// </summary>
/// <remarks>
/// <para>Applies conventions that SQLite needs:</para>
/// <list type="bullet">
///   <item><see cref="DateTimeOffset"/> is stored as UTC ticks, so it can be compared and sorted in SQL.</item>
/// </list>
/// <para>
/// SQLite has no decimal type: EF Core stores <see cref="decimal"/> as text and cannot sum or compare it in SQL.
/// Store money as <see cref="long"/> minor units (e.g. cents) instead.
/// </para>
/// </remarks>
public abstract class LocalDbContext : DbContext
{
    private IDisposable? accessLease;

    /// <summary>Transfers exactly one factory lifetime lease to this context.</summary>
    internal void AttachAccessLease(IDisposable lease)
    {
        if (accessLease is not null) throw new InvalidOperationException("Context already owns database access.");
        accessLease = lease;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        try { base.Dispose(); }
        finally { Interlocked.Exchange(ref accessLease, null)?.Dispose(); }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        try { await base.DisposeAsync().ConfigureAwait(false); }
        finally { Interlocked.Exchange(ref accessLease, null)?.Dispose(); }
    }
    /// <summary>Creates the context.</summary>
    protected LocalDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksDateTimeOffsetConverter>();
    }
}
