#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks the complete original editor on a separate non-input desktop with retained source checkpoints.</summary>
    private static async Task ReviewEntryFormWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_FORM_SOURCE_RUN");
        if (!string.IsNullOrEmpty(sourceRun) && !System.Text.RegularExpressions.Regex.IsMatch(sourceRun, "^[0-9a-f]{32}$"))
            throw new IOException("Exact original editor source required.");
        var source = string.IsNullOrEmpty(sourceRun) ? folder : Path.Combine(Path.GetDirectoryName(folder)!, sourceRun);
        if (source != folder && (!File.Exists(Path.Combine(source, "error.txt")) || File.Exists(Path.Combine(source, "complete.json"))))
            throw new IOException("Only a retained failed original form sequence may continue.");
        var original = await EntryFormNativeReview.PrepareAsync(Path.Combine(source, "original"), CancellationToken.None);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None);
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
        Task Show(EntryEditorPage page, Dictionary<string, object> query) => EntryFormNativeReview.ShowRoutedAsync(page, query);
        async Task Close(EntryEditorPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original editor route changed.");
            await Shell.Current.GoToAsync("..", false); await Task.Delay(100);
        }
        async Task Invoke(EntryEditorPage page, string action)
        {
            var model = (EntryEditorViewModel)page.BindingContext;
            var retry = model.ReloadFormCommand;
            if (action == "amount")
            {
                // Prepare only the fictitious raw draft, as in the original Save qualification.
                // The native readings prove model-to-control binding; only Save uses native Invoke here.
                model.AmountText = MoneyText.ForInput(2100, "EUR", local.CurrentCulture); await Task.Yield(); return;
            }
            var target = action == "retry" ? page.FindByName<Button>("FormRetryButton")
                : VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command,
                    action == "save" ? model.SaveCommand : model.CancelCommand));
            if (!Visible(target)) throw new IOException("Covered editor target refused.");
            await Task.Delay(150);
            if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original form native Invoke unavailable.");
            invoke.Invoke(); await Task.Yield();
            if (action == "retry")
            {
                var until = DateTime.UtcNow.AddSeconds(30);
                while (retry.ExecutionTask is null)
                { if (DateTime.UtcNow > until) throw new IOException("Original Retry never started."); await Task.Delay(20); }
                await retry.ExecutionTask.WaitAsync(TimeSpan.FromSeconds(45));
            }
        }
        async Task RetireConfirmation(EntryEditorPage page, Func<Task> replace)
        {
            var model = (EntryEditorViewModel)page.BindingContext; var cancel = model.CancelCommand;
            await Invoke(page, "cancel");
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                throw new IOException("Original discard native root unavailable.");
            Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null; var until = DateTime.UtcNow.AddSeconds(15);
            while (dialog is null)
            {
                dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(p => p.Child)
                    .OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault();
                if (DateTime.UtcNow > until) throw new IOException("Original discard prompt unavailable.");
                if (dialog is null) await Task.Delay(50);
            }
            await replace(); await Task.Delay(100);
            var discard = FindNativeButton(dialog, Translator.Instance["Common_Discard"])
                ?? throw new IOException("Original discard target unavailable.");
            if (FrameworkElementAutomationPeer.CreatePeerForElement(discard)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider answer)
                throw new IOException("Original discard Invoke unavailable.");
            answer.Invoke(); if (cancel.ExecutionTask is { } pending) await pending.WaitAsync(TimeSpan.FromSeconds(15));
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Retired discard closed the replacement form.");
        }
        async Task<object> Observe(EntryEditorPage page, string stage)
        {
            await Task.Delay(150); var model = (EntryEditorViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadyFormSurface")) != model.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("FormLoadingSurface")) != model.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("FormErrorSurface")) != model.Loading.HasFailed)
                throw new IOException("Original editor cover differs.");
            var readings = new List<object>();
            if (!model.Loading.IsReady)
            {
                if (model.CanSave || model.IsFormEditable) throw new IOException("Covered editor retained Save authority.");
                var label = page.FindByName<Label>(model.Loading.HasFailed ? "FormErrorCaption" : "FormLoadingCaption");
                CheckCaption(label);
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text || text.Text != NativeDigits.Apply(label.Text))
                    throw new IOException("Original editor cover text differs.");
                readings.Add(new { label.Text, label.Width, label.Height });
            }
            else
            {
                var scroll = page.FindByName<ScrollView>("ValidationViewport");
                foreach (var field in new[] { "AmountEntry", "ToAmountInput", "FeeInput", "DestinationFeeInput" })
                {
                    var entry = page.FindByName<Entry>(field); if (!Visible(entry)) continue;
                    await ScrollToViewIfNeededAsync(scroll, entry); await Task.Delay(40);
                    if (entry.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox { IsEnabled: true } native
                        || native.Text != entry.Text || native.ActualWidth < 44)
                        throw new IOException("Original native editor input differs: " + field);
                    readings.Add(new { Field = field, entry.Text, native.ActualWidth, native.ActualHeight });
                }
                foreach (var picker in new[] { "AccountInput", "ToAccountInput" })
                {
                    var control = page.FindByName<Picker>(picker); if (!Visible(control)) continue;
                    await ScrollToViewIfNeededAsync(scroll, control);
                    if (control.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ComboBox native
                        || (control.SelectedItem is null) != (native.SelectedItem is null))
                        throw new IOException("Original native account selection differs.");
                    // Several active accounts without an explicit/default source deliberately require a choice.
                    if (picker == "AccountInput" && (model.Account is null) != (control.SelectedItem is null))
                        throw new IOException("Original account-choice publication differs.");
                    readings.Add(new { Field = picker, Selected = control.SelectedItem?.ToString(), NativeSelected = native.SelectedItem?.ToString() });
                }
                await scroll.ScrollToAsync(0, 0, false);
            }
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "editor.png"));
            var observation = new { Stage = stage, model.Loading.IsLoading, model.Loading.HasFailed, model.Loading.IsReady,
                model.CanSave, model.IsFormEditable, model.CanReloadForm, model.HasNoAccounts, model.AmountText, model.ToAmountText,
                model.FeeText, model.DestinationFeeText, model.Note, model.IsDirty, model.HasPublishedForm, model.LoadError,
                NativeReadings = readings, ProcessId = Environment.ProcessId, File = "editor.png" };
            captures.Add(new { Language = stage, File = "editor.png" });
            await File.WriteAllTextAsync(Path.Combine(output, "observation.json"), JsonSerializer.Serialize(observation)); return observation;
        }
        try
        {
            var first = !original.CompletedStages.Contains("en-Light-360-missing");
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                if (original.CompletedStages.Contains(name + "-" + value + "-" + width + "-missing")) continue;
                await SetTheme(value); await SetLanguageAsync(app, local, name); app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await EntryFormNativeReview.ContextAsync(services, original, name + "-" + value + "-" + width, first,
                    Show, Close, Invoke, Observe, RetireConfirmation, CancellationToken.None); first = false;
            }
            await EntryFormNativeReview.FinancialAsync(services, original, Show, Invoke, Observe, CancellationToken.None);
            await EntryFormNativeReview.FreshDefaultsAsync(services, original, Show, Close, Observe, CancellationToken.None);
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None))
                throw new IOException("Original base installation rows changed.");
        }
        catch (Exception error)
        { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally { await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName); await original.Fixture.DisposeAsync(); }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Captures = captures,
            Cases = original.Cases, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, OriginalSourceRun = sourceRun,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true, NativeFinancialSaves = 2,
            KnownAcceptanceBeforeNotification = true, NoAcceptedCommandReplayed = true, OriginalLanguageAndThemeRestored = true }));
    }
}
#endif
