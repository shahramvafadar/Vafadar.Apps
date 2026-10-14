#if DEBUG && ANDROID
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Shows original sale-result help on an explicitly requested, independently packaged fictitious installation.</summary>
internal static class DebugSaleCopyAndroid
{
    /// <summary>Prepares only this independent package's synthetic holding; production identities cannot enter the review.</summary>
    internal static bool StartIfRequested(App app, IServiceProvider services)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.copyacceptance"
            || Environment.GetEnvironmentVariable("VAFADAR_SALE_COPY_REVIEW") != "1") return false;
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            try
            {
                await app.WaitForStartupAsync();
                var store = services.GetRequiredService<HoldingStore>();
                var type = (await store.GetTypesAsync()).SingleOrDefault(value => value.Name == "Synthetic sale help");
                if (type is null)
                {
                    type = new AssetType { Name = "Synthetic sale help", PriceCurrencyCode = "EUR" };
                    if (!await store.SaveTypeAsync(type)) throw new IOException("Synthetic type unavailable.");
                    var location = await store.EnsureDefaultLocationAsync("Synthetic location");
                    var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                    if (!(await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id,
                        Kind = AssetEventKind.Opening, Quantity = 1000000, BasisAmount = 10000, Date = day.AddDays(-1) }, [])).Succeeded)
                        throw new IOException("Synthetic opening unavailable.");
                    if (!(await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id,
                        Kind = AssetEventKind.Sale, Quantity = 100000, ProceedsAmount = 1200, Date = day }, [])).Succeeded)
                        throw new IOException("Synthetic sale unavailable.");
                }
                var vm = services.GetRequiredService<HoldingDetailViewModel>();
                vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id });
                await app.Windows[0].Page!.Navigation.PushModalAsync(new HoldingDetailPage(vm));
                await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "sale-copy-ready.txt"), "Original holding detail; independent fictitious sale only.");
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "sale-copy-error.txt"), error.GetType().Name);
            }
        });
        return true;
    }
}
#endif
