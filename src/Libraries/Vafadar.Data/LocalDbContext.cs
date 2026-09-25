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
