#if DEBUG && WINDOWS
using System.Text.Json;
using System.Windows.Input;
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
    /// <summary>Reuses two accepted original financial cases across read-only language/theme/width presentation contexts.</summary>
    private static async Task ReviewHoldingRefundWindowsAsync(App app, IServiceProvider services, string folder, bool feeInput = false, bool quantityInput = false)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var originalLanguage = local.CurrentLanguage; var originalTheme = theme.Choice;
        var originalFeeRun = feeInput ? Environment.GetEnvironmentVariable("VAFADAR_HOLDING_FEE_CONTINUATION") : null;
        var originalQuantityRun = quantityInput ? Environment.GetEnvironmentVariable("VAFADAR_HOLDING_QUANTITY_CONTINUATION") : null;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await BaseRows(); var captures = new List<object>();
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        Task Show(AssetEventEditorPage page) => Shell.Current.Navigation.PushAsync(page, false);
        async Task Close(AssetEventEditorPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original holding page changed.");
            await Shell.Current.Navigation.PopAsync(false);
        }
        async Task Invoke(AssetEventEditorPage page, ICommand command)
        {
            await Task.Delay(200);
            var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command) && Visible(item));
            var scroll = FindScrollView(page) ?? throw new IOException("Original holding viewport missing.");
            var inViewport = false;
            for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                if (ReferenceEquals(parent, scroll)) { inViewport = true; break; }
            if (inViewport) await scroll.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original native holding Invoke unavailable.");
            action.Invoke(); await Task.Yield();
        }
        async Task<object> Observe(AssetEventEditorPage page, AssetEventEditorViewModel model, string stage, string? errorKey)
        {
            await Task.Delay(150);
            var save = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, model.SaveCommand) && Visible(item));
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button button || button.IsEnabled != model.CanSave)
            {
                if (DateTime.UtcNow >= deadline) throw new IOException("Original refunded-fee native Save state unavailable; preserve command receipts.");
                await Task.Delay(50);
            }
            if (errorKey is not null)
            {
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == HoldingRefundNativeReview.Feedback(model, errorKey!) && Visible(item));
                await (FindScrollView(page) ?? throw new IOException("Original refunded-fee viewport missing.")).ScrollToAsync(label, ScrollToPosition.MakeVisible, false);
                while (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } native
                    || native.Text != NativeDigits.Apply(services.GetRequiredService<Translator>()[errorKey]))
                {
                    if (DateTime.UtcNow >= deadline) throw new IOException("Original live refunded-fee error incomplete.");
                    await Task.Delay(50);
                }
            }
            var directory = Path.Combine(folder, "original-" + stage); Directory.CreateDirectory(directory);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "live.png"));
            captures.Add(new { Language = "original-" + stage, File = "live.png" });
            return new { Stage = stage, ActualNativeSaveEnabled = model.CanSave, OriginalLiveOutcome = true,
                CompleteFeedback = errorKey is not null, NoFinancialInvocationByObserver = true };
        }
        HoldingRefundNativeReview.Prepared? prepared = null;
        try
        {
            await SetTheme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en");
            app.Windows[0].Width = 412; app.Windows[0].Height = 820;
            prepared = !string.IsNullOrEmpty(originalQuantityRun) ? await ContinueOriginalQuantityCaptionsAsync(services, folder, originalQuantityRun)
                : quantityInput ? await HoldingQuantityNativeReview.PrepareAsync(services, Path.Combine(folder, "financial"), Show, Invoke, Close, Observe, CancellationToken.None)
                : !string.IsNullOrEmpty(originalFeeRun) ? await ContinueOriginalFeeCaptionsAsync(services, folder, originalFeeRun)
                : await HoldingRefundNativeReview.PrepareAsync(services, Path.Combine(folder, "financial"), Show, Invoke, Close, Observe, CancellationToken.None, feeInput);
            foreach (var language in string.IsNullOrEmpty(originalFeeRun) ? new[] { "en", "fa", "de" } : new[] { "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, language);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var context = language + "-" + choice + "-" + width; var directory = Path.Combine(folder, context); Directory.CreateDirectory(directory);
                foreach (var state in prepared.States)
                {
                    // These are exact original rejected drafts, rehydrated read-only after their live same-form corrections.
                    state.Model.RetireReview();
                    HoldingRefundNativeReview.SetFeedback(state.Model, state.ErrorKey, services.GetRequiredService<Translator>()[state.ErrorKey]);
                    var page = new AssetEventEditorPage(state.Model); await Show(page); await Task.Delay(150);
                    var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == HoldingRefundNativeReview.Feedback(state.Model, state.ErrorKey) && Visible(item));
                    if (quantityInput && (!ReferenceEquals(label, page.FindByName<Label>("QuantityRangeFeedback"))
                        || !ReferenceEquals(label.Parent, page.FindByName<Entry>("QuantityInput").Parent)
                        || state.Model.QuantityError != state.Model.Error || state.Model.FormError is not null))
                        throw new IOException("Original quantity error is not beside its input.");
                    var scroll = FindScrollView(page) ?? throw new IOException("Original holding viewport missing.");
                    await scroll.ScrollToAsync(label, ScrollToPosition.MakeVisible, false);
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } native
                        || native.Text != NativeDigits.Apply(HoldingRefundNativeReview.Feedback(state.Model, state.ErrorKey)))
                    {
                        if (DateTime.UtcNow >= deadline)
                        {
                            var observed = label.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBlock;
                            await File.WriteAllTextAsync(Path.Combine(directory, "pending-caption-evidence.json"), JsonSerializer.Serialize(new
                            { Expected = HoldingRefundNativeReview.Feedback(state.Model, state.ErrorKey), NativeText = observed?.Text,
                                Width = observed?.ActualWidth, Height = observed?.ActualHeight, Trimmed = observed?.IsTextTrimmed,
                                LabelWidth = label.Width, LabelHeight = label.Height, RawFee = state.Model.FeeText, NoFinancialInvocation = true }));
                            await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "pending-caption.png"));
                            throw new IOException("Original refunded-fee feedback is incomplete; retain original financial receipts.");
                        }
                        await Task.Delay(50);
                    }
                    if (state.Model.CanSave || state.Model.CloseText != services.GetRequiredService<Translator>()[state.Name == "accepted" ? "Common_Close" : "Common_Cancel"])
                        throw new IOException("Original retained outcome became financially actionable.");
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, state.Name + ".png"));
                    captures.Add(new { Language = context, File = state.Name + ".png" }); await Close(page);
                }
            }
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally
        {
            if (Shell.Current.CurrentPage is AssetEventEditorPage) await Shell.Current.Navigation.PopAsync(false);
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, originalLanguage.CultureName);
        }
        if (before != await BaseRows()) throw new IOException("Original independent base store changed.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Cases = prepared!.Cases, Live = prepared.Live, Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true, OriginalThemeAndLanguageRestored = true,
            ActualNativeInvoke = string.IsNullOrEmpty(originalQuantityRun), OriginalQuantityReceiptContinuation = !string.IsNullOrEmpty(originalQuantityRun),
            QuantityCommandsInvokedByContinuation = !string.IsNullOrEmpty(originalQuantityRun) ? 0 : (int?)null, ReadOnlyPresentationAfterTwoFinancialCases = !quantityInput, ReadOnlyPresentationAfterTwoQuantityCases = quantityInput, NoOwnerDesktopInput = true }));
    }
}
#endif
