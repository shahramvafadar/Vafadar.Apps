using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;

namespace Vafadar.Finance.Data;

/// <summary>
/// Registers the Finance data layer.
/// </summary>
public static class FinanceDataServiceCollectionExtensions
{
    /// <summary>Adds the Finance database at <paramref name="databasePath"/> (also registered as a backup source) and the store.</summary>
    public static IServiceCollection AddFinanceData(this IServiceCollection services, string databasePath) =>
        services.AddLocalDatabase<FinanceDbContext>(databasePath).AddSingleton<FinanceStore>();
}
