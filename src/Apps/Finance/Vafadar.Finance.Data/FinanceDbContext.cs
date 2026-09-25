using Microsoft.EntityFrameworkCore;
using Vafadar.Data;

namespace Vafadar.Finance.Data;

/// <summary>
/// The Finance app's on-device database.
/// </summary>
/// <remarks>
/// Entity sets and configurations are added together with the domain model. Every schema change needs a migration:
/// <c>dotnet ef migrations add &lt;Name&gt; --project src/Apps/Finance/Vafadar.Finance.Data</c>.
/// </remarks>
public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : LocalDbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
    }
}
