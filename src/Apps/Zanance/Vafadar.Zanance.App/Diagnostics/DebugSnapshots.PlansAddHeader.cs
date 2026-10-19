#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks only the changed Plans header in eighteen actual language/theme/width contexts.</summary>
    private static async Task ReviewPlansAddHeaderAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        async Task<string> Stored() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(), Plans = await plans.GetSchedulesAsync() });
        var before = await Stored();
        var local = services.GetRequiredService<ILocalizationService>();
        var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            await Shell.Current.GoToAsync("//home"); app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            await Shell.Current.GoToAsync("//plans");
            var page = Shell.Current.CurrentPage as PlansPage ?? throw new IOException("Original Plans required.");
            var observation = await PlansAddHeaderReview.CheckAsync(page, CancellationToken.None);
            var model = (PlansViewModel)page.BindingContext;
            var populated = model.Rows;
            if (populated.Count == 0) throw new IOException("Original seeded plans must remain available.");
            var context = language + "-" + theme + "-" + width;
            var output = Path.Combine(folder, context); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "plans.png"));
            // Empty is a presentation-only transition; never delete a stored schedule to test geometry.
            model.Rows = []; model.IsEmpty = true; model.HasNoPlans = true;
            var empty = await PlansAddHeaderReview.CheckAsync(page, CancellationToken.None);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "empty.png"));
            model.Rows = populated; model.IsEmpty = false; model.HasNoPlans = false;
            await File.WriteAllTextAsync(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(new { observation, empty }));
            captures.Add(new { Language = context, File = "plans.png" });
        }
        if (before != await Stored()) throw new IOException("Plans header review changed stored financial inputs.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = true, NewManagedTests = 0 }));
    }
}
#endif
