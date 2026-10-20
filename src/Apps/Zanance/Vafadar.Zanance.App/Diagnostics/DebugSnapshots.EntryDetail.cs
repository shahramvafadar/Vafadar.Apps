#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks complete original transaction detail on an independent non-input desktop.</summary>
    private static async Task ReviewEntryDetailWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_DETAIL_SOURCE_RUN");
        var source = folder;
        if (!string.IsNullOrEmpty(sourceRun))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(sourceRun, "^[0-9a-f]{32}$")) throw new IOException("Exact original detail source required.");
            source = Path.Combine(Path.GetDirectoryName(folder)!, sourceRun);
            if (!File.ReadAllText(Path.Combine(source, "error.txt")).StartsWith("System.Text.Json.JsonException:", StringComparison.Ordinal)
                || File.Exists(Path.Combine(source, "complete.json"))) throw new IOException("Only the original pre-action observer refusal may continue.");
        }
        var original = string.IsNullOrEmpty(sourceRun)
            ? await EntryDetailNativeReview.PrepareAsync(Path.Combine(source, "original"), CancellationToken.None)
            : await EntryDetailNativeReview.ContinueAsync(Path.Combine(source, "original"), CancellationToken.None);
        var baseRows = await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None);
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
        Task Show(EntryDetailPage page) => Shell.Current.Navigation.PushAsync(page, false);
        async Task Close(EntryDetailPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original transaction route changed.");
            await Shell.Current.Navigation.PopAsync(false); await Task.Delay(100);
        }
        async Task Invoke(EntryDetailPage page, string action)
        {
            var model = (EntryDetailViewModel)page.BindingContext;
            var target = action == "retry" ? page.FindByName<Button>("RetryLoadButton")
                : VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command,
                    action == "review" ? model.MarkReviewedCommand : model.SaveRuleCommand));
            if (!Visible(target)) throw new IOException("Covered original detail target refused.");
            if (action != "retry") await ScrollToViewIfNeededAsync(page.FindByName<ScrollView>("ContentViewport"), target);
            await Task.Delay(150);
            if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original detail native Invoke unavailable.");
            invoke.Invoke(); await Task.Yield();
        }
        async Task<object> Observe(EntryDetailPage page, string stage)
        {
            await Task.Delay(150); var model = (EntryDetailViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadySurface")) != model.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("LoadingSurface")) != model.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("ErrorSurface")) != model.Loading.HasFailed
                || Visible(page.FindByName<Label>("MissingSurface")) != (model.Loading.IsReady && model.NotFound))
                throw new IOException("Original transaction native cover differs.");
            var reads = new List<object>();
            if (!model.Loading.IsReady)
            {
                if (model.CanInteract) throw new IOException("Covered detail retained action authority.");
                var label = page.FindByName<Label>(model.Loading.HasFailed ? "ErrorCaption" : "LoadingCaption");
                CheckCaption(label); reads.Add(new { label.Text, label.Width, label.Height });
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock caption || caption.Text != NativeDigits.Apply(label.Text))
                    throw new IOException("Original transaction cover text differs.");
            }
            else if (!model.NotFound)
            {
                var scroll = page.FindByName<ScrollView>("ContentViewport");
                foreach (var expected in new[] { model.Heading, model.AmountText }.Concat(model.Lines.Select(line => line.Value)))
                {
                    var labels = VisualDescendants(page).OfType<Label>().Where(label => Visible(label) && label.Text == expected).ToArray();
                    if (labels.Length == 0) throw new IOException("Original complete transaction field missing.");
                    foreach (var label in labels)
                    {
                        await ScrollToViewIfNeededAsync(scroll, label); await Task.Delay(30); CheckCaption(label); reads.Add(new { label.Text, label.Width, label.Height });
                        if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock caption || caption.Text != NativeDigits.Apply(expected))
                            throw new IOException("Original transaction field text differs.");
                    }
                }
                await scroll.ScrollToAsync(0, 0, false);
            }
            else CheckCaption(page.FindByName<Label>("MissingSurface"));
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "detail.png"));
            var observation = new { Stage = stage, model.Loading.IsLoading, model.Loading.HasFailed, model.Loading.IsReady,
                model.NotFound, model.Heading, model.AmountText, Fields = model.Lines.ToArray(), NativeReadings = reads,
                Attachments = model.Attachments.Select(row => new { row.Id, row.Name, row.Details }).ToArray(), model.HasRefunds,
                ProcessId = Environment.ProcessId, File = "detail.png" };
            captures.Add(new { Language = stage, File = "detail.png" });
            await File.WriteAllTextAsync(Path.Combine(output, "observation.json"), JsonSerializer.Serialize(observation)); return observation;
        }
        try
        {
            var first = true;
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name); app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await EntryDetailNativeReview.ContextAsync(services, original, name + "-" + value + "-" + width, first, Show, Close, Invoke, Observe, CancellationToken.None);
                first = false;
            }
            // A denied viewer dispatch creates and removes only its own cache file, without opening another desktop app.
            var cache = Path.Combine(FileSystem.CacheDirectory, "attachments");
            var before = Directory.Exists(cache) ? Directory.GetFiles(cache).Order().ToArray() : [];
            await AttachmentFiles.OpenAsync(new() { FileName = "Original refused.pdf", ContentType = "application/pdf", Data = [1, 2, 3] }, () => Task.FromResult(false));
            var after = Directory.Exists(cache) ? Directory.GetFiles(cache).Order().ToArray() : [];
            if (!before.SequenceEqual(after)) throw new IOException("Refused original viewer retained a new cache file.");
            await EntryDetailNativeReview.MetadataAsync(services, original, Show, Close, Invoke, Observe, CancellationToken.None);
            if (baseRows != await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None))
                throw new IOException("Original base installation rows changed.");
        }
        finally
        {
            await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName); await original.Fixture.DisposeAsync();
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Captures = captures,
            Cases = original.Cases, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true, FinancialCommands = 0,
            OriginalSourceRun = sourceRun, MetadataCommands = 2, NativeInvoke = true, RefusedViewerOwnedCacheRemoved = true, OriginalLanguageAndThemeRestored = true }));
    }
}
#endif
