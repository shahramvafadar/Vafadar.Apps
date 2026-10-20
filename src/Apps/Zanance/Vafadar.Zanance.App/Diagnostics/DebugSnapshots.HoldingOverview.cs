#if DEBUG && WINDOWS
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks actual original overview cover/retry/read-only navigation in the isolated native desktop.</summary>
    private static async Task ReviewHoldingOverviewWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var originalTheme = theme.Choice; var captures = new List<object>();
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(),
            Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var before = await BaseRows();
        var originalRun = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_OVERVIEW_CONTINUATION");
        var retainedFrames = new List<string>();
        var captionsOnly = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_OVERVIEW_CAPTIONS") == "1";
        var completedRun = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_OVERVIEW_COMPLETED");
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        Task Show(HoldingsPage page) => Shell.Current.Navigation.PushAsync(page, false);
        async Task Close(HoldingsPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original overview page changed.");
            await Shell.Current.Navigation.PopAsync(false);
        }
        async Task Invoke(HoldingsPage page, string action, object? argument)
        {
            await Task.Delay(150); var vm = (HoldingsViewModel)page.BindingContext;
            var button = action == "retry" ? page.FindByName<Button>("RetryLoadButton")
                : action == "add" ? page.FindByName<Button>("AddAction")
                : VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, vm.OpenCommand) && ReferenceEquals(b.CommandParameter, argument));
            if (!Visible(button)) throw new IOException("Covered original overview action cannot be invoked.");
            if (action == "open" && FindScrollView(page) is { } viewport) await viewport.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original overview native Invoke unavailable.");
            invoke.Invoke(); await Task.Yield();
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
                while (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } block
                    || block.Text != NativeDigits.Apply(label.Text))
                { if (DateTime.UtcNow > deadline) throw new IOException("Original overview complete cover caption missing."); await Task.Delay(50); }
                if (vm.Loading.HasFailed)
                {
                    var retry = page.FindByName<Button>("RetryLoadButton");
                    if (retry.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualHeight: >= 44 } target
                        || FrameworkElementAutomationPeer.CreatePeerForElement(target)?.GetName() != NativeDigits.Apply(retry.Text))
                        throw new IOException("Original overview Retry target or complete caption unavailable.");
                }
            }
            else if (!vm.IsEmpty)
            {
                HoldingOverviewNativeReview.RequireReady(vm);
                foreach (var row in vm.Rows)
                {
                    var button = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, vm.OpenCommand) && ReferenceEquals(b.CommandParameter, row));
                    var native = button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button;
                    var spoken = native is null ? null : FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetName();
                    var bound = SemanticProperties.GetDescription(button);
                    await File.WriteAllTextAsync(Path.Combine(folder, stage + "-row-" + Array.IndexOf(vm.Rows.ToArray(), row) + ".json"),
                        JsonSerializer.Serialize(new { row.Name, OriginalDescription = row.Description, BoundDescription = bound,
                            NativeName = spoken, DisplayConvertedDescription = NativeDigits.Apply(row.Description), ProcessId = Environment.ProcessId }));
                    if (native is null || bound != row.Description || spoken != bound)
                        throw new IOException("Original overview row binding/name differs.");
                }
            }
            var directory = Path.Combine(folder, stage); Directory.CreateDirectory(directory);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "overview.png"));
            var observation = new { Language = stage, Stage = stage, vm.Loading.IsLoading, vm.Loading.HasFailed, vm.Loading.IsReady, vm.IsEmpty,
                vm.IsAdvanced, vm.KnownValueText, vm.IncompleteText, Rows = vm.Rows.Select(r => new { r.Name, r.QuantityText, r.ValueText }).ToArray(),
                NativeCoverAndCaptionChecked = true, File = "overview.png", ProcessId = Environment.ProcessId };
            captures.Add(observation); return observation;
        }
        HoldingOverviewNativeReview.Prepared? prepared = null;
        try
        {
            prepared = string.IsNullOrEmpty(originalRun)
                ? await HoldingOverviewNativeReview.PrepareAsync(services, Path.Combine(folder, "original"), CancellationToken.None)
                : await HoldingOverviewNativeReview.ContinueAsync(services, Path.Combine(Directory.GetParent(folder)!.FullName, originalRun, "original"),
                    Path.Combine(folder, "original"), CancellationToken.None);
            if (captionsOnly)
            {
                if (string.IsNullOrEmpty(completedRun) || string.IsNullOrEmpty(originalRun))
                    throw new IOException("Original completed overview and changed-caption scope required.");
                var completedRoot = Path.Combine(Directory.GetParent(folder)!.FullName, completedRun);
                using var completed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(completedRoot, "complete.json")));
                using var checkpoint = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(completedRoot, "original", "retained-input-checkpoint.json")));
                var prior = completed.RootElement;
                if (prior.GetProperty("OriginalContinuation").GetString() != originalRun
                    || !prior.GetProperty("OriginalOverviewAndEmptyRowsUnchanged").GetBoolean()
                    || !prior.GetProperty("OriginalBaseStoreUnchanged").GetBoolean() || prior.GetProperty("FinancialCommands").GetInt32() != 0
                    || Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(prepared.Rows))) != checkpoint.RootElement.GetProperty("SourceDigest").GetString()
                    || Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(prepared.EmptyRows))) != checkpoint.RootElement.GetProperty("EmptyDigest").GetString())
                    throw new IOException("Original completed overview source checkpoint differs.");
                prepared.Cases.Clear(); prepared.Cases.AddRange(prior.GetProperty("Cases").EnumerateArray().Select(item => (object)item.Clone()));
            }
            var original = string.IsNullOrEmpty(originalRun);
            if (!original)
            {
                var departure = Path.Combine(Directory.GetParent(prepared.SourceFolder)!.FullName, "en-Light-360-departure-loading", "overview.png");
                if (!File.Exists(departure)) throw new IOException("Original completed departure observation missing.");
                retainedFrames.Add(departure);
            }
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var stage = name + "-" + choice + "-" + width;
                if (captionsOnly)
                {
                    await HoldingOverviewNativeReview.RunCoverCaptionsAsync(services, prepared, stage, Show, Close, Observe, CancellationToken.None);
                    continue;
                }
                if (!string.IsNullOrEmpty(originalRun))
                {
                    var root = Directory.GetParent(prepared.SourceFolder)!.FullName;
                    var frames = new[] { "loading", "error", "ready" }.Select(state => Path.Combine(root, stage + "-" + state, "overview.png")).ToArray();
                    retainedFrames.AddRange(frames.Where(File.Exists));
                    if (frames.All(File.Exists)) continue;
                    var readyReceipt = Path.Combine(prepared.SourceFolder, "receipts", stage + "-ready.json");
                    if (File.Exists(readyReceipt))
                    {
                        if (!File.Exists(frames[0]) || !File.Exists(frames[1]) || File.Exists(frames[2]))
                            throw new IOException("Exact original pending overview caption required.");
                        await HoldingOverviewNativeReview.RunReadyCaptionAsync(services, prepared, stage, Show, Close, Observe, CancellationToken.None);
                        continue;
                    }
                    if (frames.Any(File.Exists)) throw new IOException("Do not replay a partially observed original overview sequence.");
                }
                await HoldingOverviewNativeReview.RunContextAsync(services, prepared, stage,
                    Path.Combine(folder, "original", "receipts"), original, Show, Close, Invoke, Observe, CancellationToken.None);
                original = false;
            }
            if (!captionsOnly) await HoldingOverviewNativeReview.RunEmptyAsync(services, prepared, Show, Close, Observe, CancellationToken.None);
        }
        finally
        {
            if (Shell.Current.CurrentPage is HoldingsPage page) await Close(page);
            if (prepared is not null) { await prepared.Fixture.DisposeAsync(); await prepared.Empty.DisposeAsync(); }
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, language.CultureName);
        }
        if (before != await BaseRows()) throw new IOException("Independent base store changed during overview review.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Cases = prepared!.Cases, Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OriginalBaseStoreUnchanged = true, OriginalOverviewAndEmptyRowsUnchanged = true, FinancialRowsUnchangedAfterSeeding = true, FinancialCommands = 0,
            OriginalThemeAndLanguageRestored = true, NativeNavigationUsesExplicitPort = true,
            OriginalContinuation = originalRun, RetainedFrames = retainedFrames, OriginalSeedsReplayedByContinuation = 0, ChangedCaptionsOnly = captionsOnly, OriginalCompletedRun = completedRun }));
    }
}
#endif
