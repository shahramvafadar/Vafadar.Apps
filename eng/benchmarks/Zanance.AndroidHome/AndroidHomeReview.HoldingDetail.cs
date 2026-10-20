using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Continues only the new closed readback boundary after the original native detail report is complete.</summary>
    private static async Task<object> ReviewHoldingDetailRetiredCallbackAsync(App app, IServiceProvider services, string folder,
        string comparison, string baseline, CancellationToken ct)
    {
        using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "holding-detail-" + baseline + "-result.json"), ct));
        if (prior.RootElement.GetProperty("Comparison").GetString() != baseline
            || !prior.RootElement.GetProperty("OriginalTypedRowsUnchanged").GetBoolean())
            throw new ReviewCheckException("Original completed detail evidence required.");
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var original = await HoldingDetailNativeReview.OpenAsync(services, Path.Combine(folder, "original"), ct);
        var result = await HoldingDetailNativeReview.RetiredCallbackAsync(services, original, ct);
        await original.Fixture.DisposeAsync();
        return new { Comparison = comparison, OriginalComparison = baseline, ClosedCallbackOnly = true, Result = result,
            OriginalNativeMatrixNotReplayed = true, FinancialCommands = 0, SeedsReplayed = 0, ProcessId = Environment.ProcessId };
    }
    /// <summary>Checks actual detail covers, original native targets and raw Refresh over retained independent source files.</summary>
    private static async Task<object> ReviewHoldingDetailAsync(App app, IServiceProvider services, string folder, string comparison, string baseline, CancellationToken ct)
    {
        using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "holding-overview-" + baseline + "-result.json"), ct));
        if (prior.RootElement.GetProperty("Comparison").GetString() != baseline
            || !prior.RootElement.GetProperty("OriginalOverviewAndEmptyRowsUnchanged").GetBoolean())
            throw new ReviewCheckException("Original completed overview source report required.");
        var original = await HoldingDetailNativeReview.OpenAsync(services, Path.Combine(folder, "original"), ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var observations = new List<object>();
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement view && !view.IsVisible) return false;
            return true;
        }
        async Task SetTheme(ThemeChoice value)
        {
            if (theme.Choice == value) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(value); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Show(HoldingDetailPage page) { await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(200, ct); }
        async Task Close(HoldingDetailPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original detail page changed.");
            await Shell.Current.Navigation.PopAsync(false); await AwaitRootHomeAsync(ct);
        }
        async Task Invoke(HoldingDetailPage page, string action)
        {
            var vm = (HoldingDetailViewModel)page.BindingContext;
            var button = action == "retry" ? page.FindByName<Button>("RetryLoadButton")
                : ReimbursementViews(page).OfType<Button>().Single(b => action switch
                {
                    "record" => ReferenceEquals(b.Command, vm.RecordCommand) && Equals(b.CommandParameter, "Purchase"),
                    "edit" => ReferenceEquals(b.Command, vm.EditTypeCommand),
                    "refresh" => ReferenceEquals(b.Command, vm.PriceEditor.RefreshCommand),
                    _ => false,
                });
            if (!Visible(button)) throw new ReviewCheckException("Covered original detail action cannot run.");
            if (action != "retry") await ReimbursementViews(page).OfType<ScrollView>().Single(v => v.Orientation == ScrollOrientation.Vertical).ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            var deadline = DateTime.UtcNow.AddSeconds(30); Android.Widget.Button? native;
            while ((native = button.Handler?.PlatformView as Android.Widget.Button) is null || !native.IsShown || !native.HasWindowFocus
                || !native.Enabled || native.Context?.PackageName != Package)
            { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original detail native action unavailable."); await Task.Delay(50, ct); }
            if (!native.PerformClick()) throw new ReviewCheckException("Original detail native action refused."); await Task.Yield();
        }
        async Task<object> Observe(HoldingDetailPage page, string stage)
        {
            await Task.Delay(200, ct); var vm = (HoldingDetailViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadySurface")) != vm.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("LoadingSurface")) != vm.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("ErrorSurface")) != vm.Loading.HasFailed
                || Visible(page.FindByName<Label>("MissingSurface")) != (vm.Loading.IsReady && vm.NotFound))
                throw new ReviewCheckException("Original native detail cover/absence differs.");
            if (!vm.Loading.IsReady)
            {
                if (vm.PriceEditor.CanSubmit || vm.HistoryEditor.CanOpen || vm.ArchiveEditor.CanArchive)
                    throw new ReviewCheckException("Covered original detail retained authority.");
                var caption = page.FindByName<Label>(vm.Loading.HasFailed ? "ErrorCaption" : "LoadingCaption");
                if (caption.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true, Layout: { LineCount: > 0 } } native
                    || native.Text != NativeDigits.Apply(caption.Text)) throw new ReviewCheckException("Original detail native caption missing.");
                for (var line = 0; line < native.Layout.LineCount; line++)
                    if (native.Layout.GetEllipsisCount(line) != 0 || native.Layout.GetLineWidth(line) > native.Width - native.TotalPaddingLeft - native.TotalPaddingRight + 1)
                        throw new ReviewCheckException("Original detail native caption incomplete.");
                if (vm.Loading.HasFailed && (page.FindByName<Button>("RetryLoadButton").Handler?.PlatformView is not Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true } retry
                    || retry.Height / (double)(retry.Resources?.DisplayMetrics?.Density ?? 1) < 43.5))
                    throw new ReviewCheckException("Original native detail Retry unavailable.");
            }
            else if (!vm.NotFound)
            {
                if (stage == "original-query-switch")
                { if (!vm.ValueUnknown || vm.ValueText is not null || vm.PriceEditor.Raw.Length != 0) throw new ReviewCheckException("New native query retained old value/draft."); }
                else HoldingDetailNativeReview.RequireReady(vm);
                if (!ReimbursementViews(page).OfType<Label>().Where(Visible).Any(label => label.Text == vm.Name
                    && label.Handler?.PlatformView is Android.Widget.TextView { IsShown: true, HasWindowFocus: true }))
                    throw new ReviewCheckException("Original native detail identity missing.");
                if (vm.Events[0].QuantityColor != Palette.IncomeText) throw new ReviewCheckException("Original history semantic color changed.");
                if (stage.StartsWith("original-query-", StringComparison.Ordinal)
                    && (page.FindByName<Entry>("ManualPriceInput").Handler?.PlatformView is not Android.Widget.EditText entry || entry.Text != vm.PriceEditor.Raw))
                    throw new ReviewCheckException("New native query price input did not rebind.");
            }
            var observation = new { Stage = stage, vm.Loading.IsLoading, vm.Loading.HasFailed, vm.Loading.IsReady, vm.NotFound, vm.Name, vm.QuantityText,
                vm.ValueText, Raw = vm.PriceEditor.Raw, vm.PriceEditor.CanSubmit, NativeCoverIdentityAndCommandsChecked = true, ProcessId = Environment.ProcessId };
            observations.Add(observation); return observation;
        }
        try
        {
            var first = true;
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                await HoldingDetailNativeReview.ContextAsync(services, original, name + "-" + value, first, Show, Close, Invoke, Observe, ct); first = false;
            }
            await HoldingDetailNativeReview.MissingAsync(services, original, Show, Close, Observe, ct);
            await HoldingDetailNativeReview.QueryAsync(services, original, Show, Close, Observe, ct);
        }
        finally
        {
            if (Shell.Current.CurrentPage is HoldingDetailPage page) await Close(page);
            await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct);
        }
        await HoldingDetailNativeReview.RequireUnchangedAsync(original, ct); await original.Fixture.DisposeAsync();
        return new { Comparison = comparison, OriginalComparison = baseline, Cases = original.Cases, Observations = observations,
            OriginalTypedRowsUnchanged = true, FinancialCommands = 0, SeedsReplayed = 0, OriginalThemeAndLanguageRestored = true,
            NativeNavigationUsesExplicitPort = true, ProcessId = Environment.ProcessId };
    }
}
