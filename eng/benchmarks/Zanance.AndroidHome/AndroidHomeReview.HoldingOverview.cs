using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Diagnostics;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks actual original overview cover/retry/read-only navigation in the isolated native desktop.</summary>
    private static async Task<object> ReviewHoldingOverviewAsync(App app, IServiceProvider services, string folder, string comparison, string? baseline, bool captionsOnly, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var originalTheme = theme.Choice; var captures = new List<object>();
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(),
            Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var before = await BaseRows();
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        bool Visible(Element item)
        {
            for (Element? current = item; current is not null; current = current.Parent)
                if (current is VisualElement view && !view.IsVisible) return false;
            return true;
        }
        ScrollView? FindScrollView(Element page) => ReimbursementViews(page).OfType<ScrollView>().SingleOrDefault(view => view.Orientation == ScrollOrientation.Vertical);
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Show(HoldingsPage page) { await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(250, ct); }
        async Task Close(HoldingsPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original overview page changed.");
            await Shell.Current.Navigation.PopAsync(false); await AwaitRootHomeAsync(ct);
        }
        async Task Invoke(HoldingsPage page, string action, object? argument)
        {
            await Task.Delay(150); var vm = (HoldingsViewModel)page.BindingContext;
            var button = action == "retry" ? page.FindByName<Button>("RetryLoadButton")
                : action == "add" ? page.FindByName<Button>("AddAction")
                : ReimbursementViews(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, vm.OpenCommand) && ReferenceEquals(b.CommandParameter, argument));
            if (!Visible(button)) throw new IOException("Covered original overview action cannot be invoked.");
            if (action == "open" && FindScrollView(page) is { } viewport) await viewport.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            var deadline = DateTime.UtcNow.AddSeconds(30); Android.Widget.Button? native;
            while ((native = button.Handler?.PlatformView as Android.Widget.Button) is null || !native.IsShown
                || !native.HasWindowFocus || !native.Enabled || native.Context?.PackageName != Package)
            { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original overview native action unavailable."); await Task.Delay(50, ct); }
            if (!native.PerformClick()) throw new ReviewCheckException("Original overview native action refused.");
            await Task.Yield();
        }
        async Task<object> Observe(HoldingsPage page, string stage)
        {
            await Task.Delay(200); var vm = (HoldingsViewModel)page.BindingContext;
            var loading = page.FindByName<VerticalStackLayout>("LoadingSurface"); var error = page.FindByName<VerticalStackLayout>("ErrorSurface");
            var ready = page.FindByName<Grid>("ReadySurface"); var empty = page.FindByName<Border>("EmptySurface");
            if (Visible(loading) != vm.Loading.IsLoading || Visible(error) != vm.Loading.HasFailed || Visible(ready) != vm.Loading.IsReady
                || Visible(empty) != (vm.Loading.IsReady && vm.IsEmpty)) throw new IOException("Original native overview cover/empty visibility differs.");
            if (!vm.Loading.IsReady && Visible(page.FindByName<Button>("AddAction"))) throw new IOException("Original overview Add escaped its cover.");
            if (!vm.Loading.IsReady)
            {
                var label = page.FindByName<Label>(vm.Loading.IsLoading ? "LoadingCaption" : "ErrorCaption");
                var deadline = DateTime.UtcNow.AddSeconds(15);
                Android.Widget.TextView? block;
                while ((block = label.Handler?.PlatformView as Android.Widget.TextView) is null || !block.IsShown || !block.HasWindowFocus
                    || block.Text != NativeDigits.Apply(label.Text) || block.Layout is not { LineCount: > 0 })
                { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original overview complete cover caption missing."); await Task.Delay(50, ct); }
                for (var line = 0; line < block.Layout.LineCount; line++)
                    if (block.Layout.GetEllipsisCount(line) != 0 || block.Layout.GetLineWidth(line) > block.Width - block.TotalPaddingLeft - block.TotalPaddingRight + 1)
                        throw new ReviewCheckException("Original overview native cover caption incomplete.");
                if (vm.Loading.HasFailed)
                {
                    var retry = page.FindByName<Button>("RetryLoadButton").Handler?.PlatformView as Android.Widget.Button;
                    if (retry is null || !retry.Enabled || !retry.IsShown || !retry.HasWindowFocus
                        || retry.Text != NativeDigits.Apply(page.FindByName<Button>("RetryLoadButton").Text)
                        || retry.Height / (double)(retry.Resources?.DisplayMetrics?.Density ?? 1) < 43.5)
                        throw new ReviewCheckException("Original overview Retry target unavailable.");
                }
            }
            else if (!vm.IsEmpty)
            {
                HoldingOverviewNativeReview.RequireReady(vm);
                foreach (var row in vm.Rows)
                {
                    var button = ReimbursementViews(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, vm.OpenCommand) && ReferenceEquals(b.CommandParameter, row));
                    if (button.Handler?.PlatformView is not Android.Views.View native)
                        throw new ReviewCheckException("Original overview row handler unavailable.");
                    using var node = native.CreateAccessibilityNodeInfo();
                    if (SemanticProperties.GetDescription(button) != row.Description || node?.ContentDescription?.ToString() != row.Description)
                        throw new ReviewCheckException("Original overview row binding/name differs.");
                }
            }
            var observation = new { Language = stage, Stage = stage, vm.Loading.IsLoading, vm.Loading.HasFailed, vm.Loading.IsReady, vm.IsEmpty,
                vm.IsAdvanced, vm.KnownValueText, vm.IncompleteText, Rows = vm.Rows.Select(r => new { r.Name, r.QuantityText, r.ValueText }).ToArray(),
                NativeCoverAndCaptionChecked = true, ProcessId = Environment.ProcessId };
            captures.Add(observation); return observation;
        }
        HoldingOverviewNativeReview.Prepared? prepared = null;
        try
        {
            prepared = captionsOnly
                ? await HoldingOverviewNativeReview.ContinueNativeAsync(services, Path.Combine(folder, "original"),
                    Path.Combine(folder, "holding-overview-" + baseline + "-result.json"), baseline ?? throw new ReviewCheckException("Original overview comparison required."),
                    Path.Combine(folder, "overview-captions-" + comparison + "-source.json"), ct)
                : await HoldingOverviewNativeReview.PrepareAsync(services, Path.Combine(folder, "original"), ct);
            var original = !captionsOnly;
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, name, ct);
                if (captionsOnly)
                    await HoldingOverviewNativeReview.RunCoverCaptionsAsync(services, prepared, name + "-" + choice, Show, Close, Observe, ct);
                else
                    await HoldingOverviewNativeReview.RunContextAsync(services, prepared, name + "-" + choice,
                        Path.Combine(folder, "original", "receipts"), original, Show, Close, Invoke, Observe, ct);
                original = false;
            }
            if (!captionsOnly) await HoldingOverviewNativeReview.RunEmptyAsync(services, prepared, Show, Close, Observe, ct);
        }
        finally
        {
            if (Shell.Current.CurrentPage is HoldingsPage page) await Close(page);
            if (prepared is not null) { await prepared.Fixture.DisposeAsync(); await prepared.Empty.DisposeAsync(); }
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, language.CultureName, ct);
        }
        if (before != await BaseRows()) throw new IOException("Independent base store changed during overview review.");
        return new { Comparison = comparison, Cases = prepared!.Cases, Observations = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OriginalBaseStoreUnchanged = true, OriginalOverviewAndEmptyRowsUnchanged = true, FinancialCommands = 0,
            OriginalThemeAndLanguageRestored = true, NativeNavigationUsesExplicitPort = true, ChangedCaptionsOnly = captionsOnly, OriginalComparison = baseline };
    }
}
