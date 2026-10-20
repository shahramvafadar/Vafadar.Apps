#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original detail controls/covers against retained independent data on the non-input desktop.</summary>
    private static async Task ReviewHoldingDetailWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_DETAIL_SOURCE");
        if (sourceRun is null || !System.Text.RegularExpressions.Regex.IsMatch(sourceRun, "^[0-9a-f]{32}$"))
            throw new IOException("Original completed overview source required.");
        var source = Path.Combine(Directory.GetParent(folder)!.FullName, sourceRun, "original");
        var queryOnly = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_DETAIL_QUERY_ONLY") == "1";
        var completedRun = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_DETAIL_COMPLETED");
        if (queryOnly)
        {
            if (completedRun is null || !System.Text.RegularExpressions.Regex.IsMatch(completedRun, "^[0-9a-f]{32}$"))
                throw new IOException("Exact completed detail matrix required.");
            using var completed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Directory.GetParent(folder)!.FullName, completedRun, "complete.json")));
            if (completed.RootElement.GetProperty("OriginalSourceRun").GetString() != sourceRun
                || !completed.RootElement.GetProperty("OriginalTypedRowsUnchanged").GetBoolean() || completed.RootElement.GetProperty("Captures").GetArrayLength() != 57)
                throw new IOException("Original completed detail matrix differs.");
        }
        var original = await HoldingDetailNativeReview.OpenAsync(services, source, CancellationToken.None);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var captures = new List<object>();
        bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
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
        Task Show(HoldingDetailPage page) => Shell.Current.Navigation.PushAsync(page, false);
        async Task Close(HoldingDetailPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original detail route changed.");
            await Shell.Current.Navigation.PopAsync(false); await Task.Delay(100);
        }
        async Task Invoke(HoldingDetailPage page, string action)
        {
            var vm = (HoldingDetailViewModel)page.BindingContext;
            var button = action == "retry" ? page.FindByName<Button>("RetryLoadButton")
                : VisualDescendants(page).OfType<Button>().Single(b => action switch
                {
                    "record" => ReferenceEquals(b.Command, vm.RecordCommand) && Equals(b.CommandParameter, "Purchase"),
                    "edit" => ReferenceEquals(b.Command, vm.EditTypeCommand),
                    "refresh" => ReferenceEquals(b.Command, vm.PriceEditor.RefreshCommand),
                    _ => false,
                });
            if (!Visible(button)) throw new IOException("Covered original detail action cannot run.");
            if (action != "retry") await VisualDescendants(page).OfType<ScrollView>().Single(v => v.Orientation == ScrollOrientation.Vertical).ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original detail native Invoke unavailable.");
            invoke.Invoke(); await Task.Yield();
        }
        async Task<object> Observe(HoldingDetailPage page, string stage)
        {
            await Task.Delay(200); var vm = (HoldingDetailViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadySurface")) != vm.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("LoadingSurface")) != vm.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("ErrorSurface")) != vm.Loading.HasFailed
                || Visible(page.FindByName<Label>("MissingSurface")) != (vm.Loading.IsReady && vm.NotFound))
                throw new IOException("Original native detail cover/absence differs.");
            if (!vm.Loading.IsReady)
            {
                if (vm.PriceEditor.CanSubmit || vm.HistoryEditor.CanOpen || vm.ArchiveEditor.CanArchive)
                    throw new IOException("Covered native detail retained authority.");
                var caption = page.FindByName<Label>(vm.Loading.HasFailed ? "ErrorCaption" : "LoadingCaption");
                if (caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } text
                    || text.Text != NativeDigits.Apply(caption.Text)) throw new IOException("Original detail native cover caption incomplete.");
                if (vm.Loading.HasFailed && page.FindByName<Button>("RetryLoadButton").Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualHeight: >= 44 })
                    throw new IOException("Original detail Retry target unavailable.");
            }
            else if (!vm.NotFound)
            {
                if (stage == "original-query-switch")
                { if (!vm.ValueUnknown || vm.ValueText is not null || vm.PriceEditor.Raw.Length != 0) throw new IOException("New native query retained old value/draft."); }
                else HoldingDetailNativeReview.RequireReady(vm);
                var labels = VisualDescendants(page).OfType<Label>().Where(Visible).ToArray();
                if (!labels.Any(label => label.Text == vm.Name && label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock { IsTextTrimmed: false }))
                    throw new IOException("Original detail native identity missing.");
                if (vm.Events[0].QuantityColor != Palette.IncomeText) throw new IOException("Original history semantic color changed.");
                if (stage.StartsWith("original-query-", StringComparison.Ordinal)
                    && (page.FindByName<Entry>("ManualPriceInput").Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox entry || entry.Text != vm.PriceEditor.Raw))
                    throw new IOException("New native query price input did not rebind.");
            }
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "detail.png"));
            var observation = new { Language = stage, Stage = stage, vm.Loading.IsLoading, vm.Loading.HasFailed, vm.Loading.IsReady, vm.NotFound,
                vm.Name, vm.QuantityText, vm.ValueText, Raw = vm.PriceEditor.Raw, vm.PriceEditor.CanSubmit,
                NativeCoverIdentityAndCommandsChecked = true, ProcessId = Environment.ProcessId, File = "detail.png" };
            captures.Add(observation); await File.WriteAllTextAsync(Path.Combine(output, "observation.json"), JsonSerializer.Serialize(observation)); return observation;
        }
        try
        {
            var first = true;
            if (!queryOnly)
            {
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await HoldingDetailNativeReview.ContextAsync(services, original, name + "-" + value + "-" + width, first, Show, Close, Invoke, Observe, CancellationToken.None);
                first = false;
            }
            await HoldingDetailNativeReview.MissingAsync(services, original, Show, Close, Observe, CancellationToken.None);
            }
            if (queryOnly)
            {
                app.Windows[0].Width = 412; app.Windows[0].Height = 820;
                await HoldingDetailNativeReview.QueryAsync(services, original, Show, Close, Observe, CancellationToken.None);
            }
        }
        finally
        {
            if (Shell.Current.CurrentPage is HoldingDetailPage page) await Close(page);
            await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName);
        }
        await HoldingDetailNativeReview.RequireUnchangedAsync(original, CancellationToken.None); await original.Fixture.DisposeAsync();
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Cases = original.Cases, Captures = captures,
            Package = AppInfo.Current.PackageName, FinancialRowsUnchangedAfterSeeding = true,
            ProcessId = Environment.ProcessId, OriginalSourceRun = sourceRun, OriginalTypedRowsUnchanged = true, FinancialCommands = 0, SeedsReplayed = 0,
            QueryIdentityOnly = queryOnly, OriginalCompletedRun = completedRun,
            OriginalThemeAndLanguageRestored = true, NativeNavigationUsesExplicitPort = true }));
    }
}
#endif
