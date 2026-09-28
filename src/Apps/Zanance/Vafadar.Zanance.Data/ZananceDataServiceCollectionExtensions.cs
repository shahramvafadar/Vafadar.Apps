using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;

namespace Vafadar.Zanance.Data;

/// <summary>
/// Registers the Zanance data layer.
/// </summary>
public static class ZananceDataServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Zanance database at <paramref name="databasePath"/> (also registered as a backup source), the stores
    /// and the automatic posting processor.
    /// </summary>
    public static IServiceCollection AddZananceData(this IServiceCollection services, string databasePath) =>
        services.AddLocalDatabase<ZananceDbContext>(databasePath)
            .AddSingleton<ZananceStore>()
            .AddSingleton<PlanStore>()
            .AddSingleton<GoalStore>()
            .AddSingleton<AutoPostProcessor>()
            .AddSingleton<IBackupSummaryProvider, ZananceBackupSummary>();
}
