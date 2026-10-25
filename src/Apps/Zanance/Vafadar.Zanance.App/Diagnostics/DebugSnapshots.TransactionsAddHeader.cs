#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks the changed Transactions header in eighteen actual language/theme/width contexts.</summary>
    private static async Task ReviewTransactionsAddHeaderAsync(App app, IServiceProvider services, string folder, bool largeText = false)
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
            await Shell.Current.GoToAsync("//transactions");
            var page = Shell.Current.CurrentPage as TransactionsPage ?? throw new IOException("Original Transactions required.");
            var observation = await TransactionsAddHeaderReview.CheckAsync(page, CancellationToken.None);
            var scaledTitle = largeText ? CheckTransactionsHeaderLargeText(page) : null;
            var model = (TransactionsViewModel)page.BindingContext;
            var populated = model.Days;
            if (populated.Count == 0) throw new IOException("Original seeded transactions must remain available.");
            var context = language + "-" + theme + "-" + width;
            var output = Path.Combine(folder, context); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "transactions.png"));
            // Empty is a presentation-only transition; never delete a stored transaction to test geometry.
            model.Days = [];
            var empty = await TransactionsAddHeaderReview.CheckAsync(page, CancellationToken.None);
            var emptyScaledTitle = largeText ? CheckTransactionsHeaderLargeText(page) : null;
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "empty.png"));
            model.Days = populated;
            model.StartSelectingCommand.Execute(null);
            var bulk = await TransactionsAddHeaderReview.CheckAsync(page, CancellationToken.None);
            var bulkScaledTitle = largeText ? CheckTransactionsHeaderLargeText(page) : null;
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "bulk.png"));
            model.StopSelectingCommand.Execute(null);
            await File.WriteAllTextAsync(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(new
                { observation, empty, bulk, scaledTitle, emptyScaledTitle, bulkScaledTitle }));
            captures.Add(new { Language = context, File = "transactions.png" });
        }
        if (before != await Stored()) throw new IOException("Transactions header review changed stored financial inputs.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = true, NewManagedTests = 0,
            RequestedFontScale = largeText ? 2 : 1, RetainedSourceWithoutSeed = largeText,
            FinancialCommands = 0 }));
    }

    /// <summary>Checks the actual scaled native title, including wrapping and allocated height, without changing OS settings.</summary>
    private static object CheckTransactionsHeaderLargeText(TransactionsPage page)
    {
        var header = page.FindByName<Grid>("TransactionsHeader");
        var title = header.Children.OfType<Label>().Single();
        if (title.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
            throw new IOException("Original native Transactions title required.");
        var systemScale = new Windows.UI.ViewManagement.UISettings().TextScaleFactor;
        var ratio = text.FontSize * systemScale / title.FontSize;
        if (Math.Abs(ratio - 2) > 0.02 || text.IsTextTrimmed || text.ActualWidth <= 0
            || text.ActualHeight + 1 < text.DesiredSize.Height || text.Text != title.Text)
            throw new IOException("The original double-size Transactions title is clipped or unscaled.");
        return new { NativeFontSize = text.FontSize, OriginalFontSize = title.FontSize,
            SystemScale = systemScale, EffectiveRatio = ratio, text.ActualWidth, text.ActualHeight,
            DesiredHeight = text.DesiredSize.Height, CompleteTitle = text.Text, text.IsTextTrimmed };
    }
}
#endif
