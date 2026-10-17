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
    /// <summary>Composes a device adapter with the existing singleton verified source; no source is replaced with guessed paid facts.</summary>
    public static IServiceCollection DecorateZananceCommercialAccess(this IServiceCollection services,
        Func<IServiceProvider, ICommercialWriteAccessSource, ICommercialWriteAccessSource> decorator)
    {
        ArgumentNullException.ThrowIfNull(services); ArgumentNullException.ThrowIfNull(decorator);
        var original = services.LastOrDefault(item => item.ServiceType == typeof(ICommercialWriteAccessSource))
            ?? throw new InvalidOperationException("Register the verified commercial source before its adapter.");
        if (original.Lifetime != ServiceLifetime.Singleton || original.IsKeyedService)
            throw new InvalidOperationException("The commercial adapter requires the existing unkeyed singleton source.");
        services.Remove(original);
        // A private DI key retains the original ownership/disposal semantics without resolving the adapter
        // recursively or making an externally supplied instance container-owned.
        var key = new object();
        if (original.ImplementationInstance is ICommercialWriteAccessSource instance)
            services.AddKeyedSingleton<ICommercialWriteAccessSource>(key, instance);
        else if (original.ImplementationFactory is { } factory)
            services.AddKeyedSingleton<ICommercialWriteAccessSource>(key, (provider, _) => (ICommercialWriteAccessSource)factory(provider));
        else services.Add(ServiceDescriptor.KeyedSingleton(typeof(ICommercialWriteAccessSource), key, original.ImplementationType!));
        services.AddSingleton<ICommercialWriteAccessSource>(provider => decorator(provider,
            provider.GetRequiredKeyedService<ICommercialWriteAccessSource>(key)));
        return services;
    }

    /// <summary>
    /// Adds the Zanance database at <paramref name="databasePath"/> (also registered as a backup source), the stores
    /// and the automatic posting processor.
    /// </summary>
    /// <param name="protectionRequired">Require a protected generation before any store or backup can construct a context.</param>
    public static IServiceCollection AddZananceData(this IServiceCollection services, string databasePath, bool protectionRequired = false)
    {
        // The compiled model (CompiledModel/) builds itself on a second thread and waits for it inside its static
        // constructor; under Mono (Android, iOS) that thread waits for the same static constructor and the app hangs at
        // startup. The model is small, so it is built on the calling thread.
        AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);

        // D-118: no commercial limits in current test builds; this is not a customer entitlement.
        services.TryAddSingleton<ICommercialWriteAccessSource, InactiveCommercialWriteAccessSource>();

        services.AddLocalDatabase<ZananceDbContext>(databasePath, protectionRequired);
        // D-124: replace only this database source; preserve display/settings and every other registered source.
        var databaseSource = services.Single(d => d.ServiceType == typeof(IBackupSource)
            && d.ImplementationType == typeof(SqliteDatabaseBackupSource<ZananceDbContext>));
        services.Remove(databaseSource);

        return services.AddSingleton<IBackupSource, ZananceDatabaseBackupSource>()
            .AddSingleton<ZananceStore>()
            .AddSingleton<PlanStore>()
            .AddSingleton<ResourceChoiceStore>()
            .AddSingleton<CommercialAccessStore>()
            .AddSingleton<ForecastWorkStore>()
            .AddSingleton<ReceiptWorkStore>()
            .AddSingleton<LoanWorkStore>()
            .AddSingleton<ReportWorkStore>()
            .AddSingleton<Tax.AccountantExportStore>()
            .AddSingleton<Analysis.AiSnapshotStore>()
            .AddSingleton(provider =>
            {
                var references = new Rates.MarketReferenceStore(provider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<ZananceDbContext>>(),
                    provider.GetRequiredService<ICommercialWriteAccessSource>(), provider.GetRequiredService<TimeProvider>());
                references.Changed += (_, _) =>
                {
                    // Both existing subscriber sets observe the known commit; a failed observer never turns it into an uncommitted write.
                    var failures = new List<Exception>();
                    try { provider.GetRequiredService<ZananceStore>().NotifyReferenceChanged(); } catch (Exception error) when (error is not OutOfMemoryException) { failures.Add(error); }
                    try { provider.GetRequiredService<HoldingStore>().NotifyReferenceChanged(); } catch (Exception error) when (error is not OutOfMemoryException) { failures.Add(error); }
                    if (failures.Count != 0) throw new AggregateException(failures);
                };
                return references;
            })
            .AddSingleton<BudgetChoiceStore>()
            .AddSingleton<GoalStore>()
            .AddSingleton<HoldingStore>()
            .AddSingleton<AutoPostProcessor>()
            .AddSingleton<Sync.PersonalSyncStore>()
            .AddSingleton<Sync.PersonalSyncCoordinator>()
            .AddSingleton<Sharing.HouseholdProjectionStore>()
            .AddSingleton<Sharing.HouseholdJournal>()
            .AddSingleton<Sharing.HouseholdSyncCoordinator>()
            .AddSingleton<Sharing.HouseholdWorkspace>()
            .AddSingleton<Sharing.HouseholdCostStore>()
            .AddSingleton<IBackupSummaryProvider, ZananceBackupSummary>();
    }
}
