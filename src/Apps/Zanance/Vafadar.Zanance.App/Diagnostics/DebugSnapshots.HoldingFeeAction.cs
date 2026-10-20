#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies actual fee/ordinary refund actions and keeps managed principal edit/delete restrictions.</summary>
    private static async Task ReviewHoldingFeeActionWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var text = services.GetRequiredService<Translator>(); var originalLanguage = local.CurrentLanguage; var originalTheme = theme.Choice;
        var cases = await HoldingFeeActionNativeReview.PrepareAsync(services, CancellationToken.None);
        var before = await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None);
        var captures = new List<object>(); var observations = new List<object>(); var originalRefundOpenedAndCancelled = false;
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Invoke(Button button)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: > 0, ActualHeight: >= 44 })
            { if (DateTime.UtcNow >= deadline) throw new IOException("Original detail native action not ready."); await Task.Delay(50); }
            var native = (Microsoft.UI.Xaml.Controls.Button)button.Handler.PlatformView!;
            if (FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original detail native Invoke unavailable.");
            action.Invoke(); await Task.Delay(200);
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, language);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var context = language + "-" + choice + "-" + width;
                foreach (var item in cases)
                {
                    if (item.Name == "ordinary-expense" && width != 412) continue;
                    if (item.Name is "holding-principal" or "ordinary-income" && (language != "en" || choice != ThemeChoice.Light || width != 412)) continue;
                    var model = services.GetRequiredService<EntryDetailViewModel>();
                    model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = item.EntryId }); await model.LoadAsync();
                    if (model.NotFound || model.IsHoldingMoney != item.ManagedHolding || model.CanRefund != item.RefundAvailable)
                        throw new IOException("Original detail eligibility differs.");
                    var page = new EntryDetailPage(model); await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(200);
                    var buttons = VisualDescendants(page).OfType<Button>().Where(Visible).ToList();
                    var refund = buttons.Where(button => ReferenceEquals(button.Command, model.RefundCommand)).ToList();
                    if (refund.Count != (item.RefundAvailable ? 1 : 0)
                        || item.ManagedHolding && buttons.Any(button => ReferenceEquals(button.Command, model.DeleteCommand) || ReferenceEquals(button.Command, model.DuplicateCommand)))
                        throw new IOException("Original managed detail actions changed.");
                    var directory = Path.Combine(folder, context); Directory.CreateDirectory(directory);
                    if (item.RefundAvailable)
                    {
                        var viewport = VisualDescendants(page).OfType<ScrollView>().Single(view => view.Orientation == ScrollOrientation.Vertical);
                        await viewport.ScrollToAsync(refund.Single(), ScrollToPosition.MakeVisible, false); await Task.Delay(200);
                        var native = refund.Single().Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button;
                        if (native is null || !native.IsEnabled || native.ActualWidth < 44 || native.ActualHeight < 44
                            || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) != NativeDigits.Apply(text["Entry_Refund"]))
                            throw new IOException("Original refund action name or target incomplete.");
                    }
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, item.Name + ".png"));
                    captures.Add(new { Language = context, File = item.Name + ".png" });
                    observations.Add(new { Language = language, Theme = choice.ToString(), Width = width, State = item.Name,
                        ManagedHolding = model.IsHoldingMoney, ExistingCanRefund = model.CanRefund, VisibleRefundTargets = refund.Count,
                        ManagedSourceActionsPreserved = true, ReadOnlyNativeContext = true });
                    if (!originalRefundOpenedAndCancelled && item.Name == "holding-fee" && language == "en" && choice == ThemeChoice.Light && width == 412)
                    {
                        await Invoke(refund.Single()); var deadline = DateTime.UtcNow.AddSeconds(20); EntryEditorPage? editor = null;
                        while ((editor = Shell.Current.Navigation.ModalStack.LastOrDefault() as EntryEditorPage) is null
                            || editor.BindingContext is not EntryEditorViewModel vm || vm.Kind != EntryKind.Refund || vm.IsDirty
                            || !MoneyText.TryParse(vm.AmountText, "EUR", local.CurrentCulture, out var amount) || amount != 200)
                        { if (DateTime.UtcNow >= deadline) throw new IOException("Original fee refund draft did not publish."); await Task.Delay(50); }
                        var refundModel = (EntryEditorViewModel)editor.BindingContext;
                        if (refundModel.EntryTitle != "D411 original refundable fee" || refundModel.RefundInfo is null)
                            throw new IOException("Original fee refund source differs.");
                        var cancel = VisualDescendants(editor).OfType<Button>().Single(button => ReferenceEquals(button.Command, refundModel.CancelCommand) && Visible(button));
                        await Invoke(cancel);
                        while (Shell.Current.Navigation.ModalStack.Any())
                        { if (DateTime.UtcNow >= deadline) throw new IOException("Original unchanged refund draft did not cancel."); await Task.Delay(50); }
                        originalRefundOpenedAndCancelled = true;
                    }
                    if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original detail page changed.");
                    await Shell.Current.Navigation.PopAsync(false);
                }
            }
        }
        finally
        {
            if (Shell.Current.Navigation.ModalStack.LastOrDefault() is EntryEditorPage) await Shell.Current.Navigation.PopModalAsync(false);
            if (Shell.Current.CurrentPage is EntryDetailPage) await Shell.Current.Navigation.PopAsync(false);
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, originalLanguage.CultureName);
        }
        if (!originalRefundOpenedAndCancelled || before != await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None))
            throw new IOException("Original detail action qualification changed stored financial rows.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Captures = captures, Observations = observations, OriginalFeeRefundOpenedAndCancelled = true, FinancialRowsUnchangedAfterSeeding = true,
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, NoOwnerDesktopInput = true, OriginalThemeAndLanguageRestored = true }));
    }
}
#endif
