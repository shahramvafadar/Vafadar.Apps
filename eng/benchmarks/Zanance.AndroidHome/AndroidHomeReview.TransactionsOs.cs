using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies only the uncovered actual OS-scaled title/context rows on retained independent data.</summary>
    private static async Task<object> ReviewTransactionsHeaderOsAsync(App app, IServiceProvider services, string root,
        string comparison, CancellationToken ct)
    {
        var width = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetIntExtra("review_os_width", 0) ?? 0;
        var resources = Android.App.Application.Context.Resources;
        if (Package != "pro.vafadar.zanance.androidhomereview" || AppInfo.Current.PackageName != Package
            || Android.OS.Build.Model != "sdk_gphone64_x86_64" || width is not (360 or 412)
            || Math.Abs((resources?.Configuration?.FontScale ?? 0) - 2f) > 0.01f
            || resources?.DisplayMetrics is not { Density: > 0 } metrics || Math.Abs(metrics.WidthPixels / metrics.Density - width) > 1)
            throw new ReviewCheckException("Retained independent emulator and actual OS200% Transactions width required.");
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        var accounts = await store.GetAccountsAsync(cancellationToken: ct);
        var entries = await store.GetEntriesAsync(cancellationToken: ct);
        if (accounts.Count == 0 || entries.Count == 0)
            throw new ReviewCheckException("Original nonempty source required; never prepare or save financial data for geometry.");
        async Task<string> Stored() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct), Plans = await plans.GetSchedulesAsync(ct) });
        var before = await Stored();
        var sourceReceipt = Path.Combine(root, "transactions-os-original-source.json");
        var originalDigest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(before)));
        if (File.Exists(sourceReceipt))
        {
            using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(sourceReceipt, ct));
            if (prior.RootElement.GetProperty("OriginalDigest").GetString() != originalDigest)
                throw new ReviewCheckException("Retained original Transactions source differs; never replace its receipt.");
        }
        else await File.WriteAllTextAsync(sourceReceipt, JsonSerializer.Serialize(new { OriginalDigest = originalDigest,
            Accounts = accounts.Count, Entries = entries.Count, FinancialCommands = 0, NoSeedOrKeyCreation = true }), ct);
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var originalLanguage = local.CurrentLanguage.CultureName; var originalTheme = themes.Choice;
        var observations = new List<JsonElement>();
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        async Task Until(Func<bool> ready)
        {
            for (var attempt = 0; attempt < 300; attempt++) { if (ready()) return; await Task.Delay(50, ct); }
            throw new ReviewCheckException("Original Transactions OS publication or native layout did not finish.");
        }
        async Task<object> Native(TransactionsPage page)
        {
            var header = page.FindByName<Grid>("TransactionsHeader"); var title = header.Children.OfType<Label>().Single();
            await Until(() => title.Handler?.PlatformView is Android.Widget.TextView { Layout.LineCount: > 0, IsShown: true, HasWindowFocus: true });
            var text = (Android.Widget.TextView)title.Handler!.PlatformView!; var layout = text.Layout!;
            using var visible = new Android.Graphics.Rect();
            if (text.Text != title.Text || !title.FontAutoScalingEnabled
                || text.TextSize <= title.FontSize * metrics.Density * 1.1
                || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0)
                || !text.GetGlobalVisibleRect(visible) || visible.Width() != text.Width || visible.Height() != text.Height)
                throw new ReviewCheckException("Actual OS-scaled Transactions title is clipped, unscaled or unfocused.");
            MonetaryOsScope.RequireCompleteHeight(text, layout);
            var model = (TransactionsViewModel)page.BindingContext; var add = page.FindByName<Button>("AddAction");
            if (!model.IsSelecting)
            {
                await Until(() => add.Handler?.PlatformView is Android.Views.View { IsShown: true, HasWindowFocus: true });
                var button = (Android.Views.View)add.Handler!.PlatformView!; using var target = new Android.Graphics.Rect();
                if (!button.GetGlobalVisibleRect(target) || target.Width() != button.Width || target.Height() != button.Height
                    || target.Left < visible.Right || target.Width() / metrics.Density < 44 || target.Height() / metrics.Density < 44)
                    throw new ReviewCheckException("Actual original Transactions Add target is incomplete or not physically right of title.");
            }
            return new { FullTitle = text.Text, Lines = layout.LineCount, NativeFontPixels = text.TextSize,
                OriginalFontSize = title.FontSize, ActualFontScale = resources!.Configuration!.FontScale,
                WidthDp = metrics.WidthPixels / metrics.Density, NativeTitleLeft = visible.Left,
                NativeTitleRight = visible.Right, NativeTitleTop = visible.Top, NativeTitleBottom = visible.Bottom,
                AndroidNonlinearFontScalingNotExactRatioClaim = true, OriginalAccessibleAddCommand = true };
        }
        try
        {
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var language in new[] { "en", "fa", "de" })
            {
                var states = new[] { "populated", "empty", "selection" };
                var files = states.Select(state => Path.Combine(root, $"transactions-os-{width}-{language}-{theme}-{state}.json")).ToArray();
                if (files.All(File.Exists))
                {
                    foreach (var existing in files) observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(existing, ct)));
                    continue;
                }
                await Shell.Current.GoToAsync("//home");
                if (themes.Choice != theme)
                {
                    var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                    app.ShellRebuilt += Rebuilt;
                    try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
                    finally { app.ShellRebuilt -= Rebuilt; }
                }
                await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                await Shell.Current.GoToAsync("//transactions");
                await Until(() => Shell.Current.CurrentPage is TransactionsPage { IsLoaded: true });
                var page = (TransactionsPage)Shell.Current.CurrentPage;
                var model = (TransactionsViewModel)page.BindingContext;
                // D-546: appearance can publish its Task after Shell navigation; await the actual original ready state.
                await Until(() => model.Loading.IsReady || model.Loading.HasFailed);
                await page.PendingPublication.WaitAsync(ct);
                if (!model.Loading.IsReady) throw new ReviewCheckException("Original Transactions publication failed before geometry.");
                // Retained historical rows need the original read-only All filter, not fresh entries or a changed clock.
                model.ClearFiltersCommand.Execute(null);
                await Until(() => model.Days.Count > 0 || model.Loading.HasFailed);
                var populated = model.Days; var wasEmpty = model.IsEmpty;
                if (populated.Count == 0 || wasEmpty) throw new ReviewCheckException("Original populated transaction scene required without financial preparation.");
                try
                {
                    for (var index = 0; index < states.Length; index++)
                    {
                        if (File.Exists(files[index])) { observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(files[index], ct))); continue; }
                        model.StopSelectingCommand.Execute(null); model.Days = populated; model.IsEmpty = wasEmpty;
                        if (states[index] == "empty") { model.Days = []; model.IsEmpty = true; }
                        if (states[index] == "selection") model.StartSelectingCommand.Execute(null);
                        var geometry = await Diagnostics.TransactionsAddHeaderReview.CheckAsync(page, ct);
                        var native = await Native(page);
                        if (before != await Stored()) throw new ReviewCheckException("Transactions OS observation changed original financial inputs.");
                        var observation = JsonSerializer.SerializeToElement(new { Comparison = comparison, Width = width,
                            Language = language, Theme = theme.ToString(), State = states[index], geometry, native,
                            OriginalDigest = originalDigest, FinancialCommands = 0, ViewOnlyEmptyOrSelection = true,
                            ReadOnlyOriginalAllFilter = model.PeriodIndex == 3, OriginalDisplayedPeriod = model.PeriodNames[model.PeriodIndex],
                            OriginalStoredInputsUnchanged = true, NativeAddCancelNotReplayed = true });
                        await File.WriteAllTextAsync(files[index], observation.GetRawText(), ct); observations.Add(observation);
                    }
                }
                finally { model.StopSelectingCommand.Execute(null); model.Days = populated; model.IsEmpty = wasEmpty; }
            }
        }
        finally
        {
            await Shell.Current.GoToAsync("//home"); themes.Set(originalTheme); await SetLanguageAsync(app, local, originalLanguage, ct);
        }
        if (before != await Stored()) throw new ReviewCheckException("Original source changed after Transactions OS restoration.");
        return new { Comparison = comparison, Width = width, Contexts = observations, OriginalDigest = originalDigest,
            FinancialCommands = 0, OriginalThemeAndLanguageRestored = true, MainPackageTouched = false,
            ActualOSFontScale = resources!.Configuration!.FontScale, Device = DeviceEvidence() };
    }
}
