using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.Data.Commerce;

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
    public static IServiceCollection AddZananceData(this IServiceCollection services, string databasePath)
    {
        // The compiled model (CompiledModel/) builds itself on a second thread and waits for it inside its static
        // constructor; under Mono (Android, iOS) that thread waits for the same static constructor and the app hangs at
        // startup. The model is small, so it is built on the calling thread.
        AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);

        // D-118: no commercial limits in current test builds; this is not a customer entitlement.
        services.TryAddSingleton<ICommercialWriteAccessSource, InactiveCommercialWriteAccessSource>();

        return services.AddLocalDatabase<ZananceDbContext>(databasePath)
            .AddSingleton<ZananceStore>()
            .AddSingleton<PlanStore>()
            .AddSingleton<GoalStore>()
            .AddSingleton<HoldingStore>()
            .AddSingleton<AutoPostProcessor>()
            .AddSingleton<IBackupSummaryProvider, ZananceBackupSummary>();
    }
}
