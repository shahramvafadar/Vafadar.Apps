using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks retained original native fee actions and managed source restrictions without invoking a financial command.</summary>
    private static async Task<object> ReviewHoldingFeeActionAsync(App app, IServiceProvider services, string root, string comparison, bool retained, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var text = services.GetRequiredService<Translator>(); var originalLanguage = local.CurrentLanguage; var originalTheme = theme.Choice;
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var cases = retained ? await HoldingFeeActionNativeReview.ReadPreparedAsync(services, ct) : await HoldingFeeActionNativeReview.PrepareAsync(services, ct);
        var before = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var observations = new List<object>();
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        bool Visible(Element item)
        {
            for (Element? node = item; node is not null; node = node.Parent)
                if (node is VisualElement view && !view.IsVisible) return false;
            return true;
        }
        // MAUI publishes semantic descriptions through Android's accessibility delegate, not View.ContentDescription.
        static string? NativeName(Android.Widget.Button button)
        {
            using var node = button.CreateAccessibilityNodeInfo();
            return node?.ContentDescription;
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, language, ct);
                foreach (var item in cases)
                {
                    var model = services.GetRequiredService<EntryDetailViewModel>();
                    model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = item.EntryId }); await model.LoadAsync();
                    if (model.NotFound || model.IsHoldingMoney != item.ManagedHolding || model.CanRefund != item.RefundAvailable)
                        throw new ReviewCheckException("Original detail eligibility differs.");
                    var page = new EntryDetailPage(model); await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(200, ct);
                    var buttons = ReimbursementViews(page).OfType<Button>().Where(Visible).ToList();
                    var refund = buttons.Where(button => ReferenceEquals(button.Command, model.RefundCommand)).ToList();
                    if (refund.Count != (item.RefundAvailable ? 1 : 0)
                        || item.ManagedHolding && buttons.Any(button => ReferenceEquals(button.Command, model.DeleteCommand) || ReferenceEquals(button.Command, model.DuplicateCommand)))
                        throw new ReviewCheckException("Original managed source actions differ.");
                    if (item.RefundAvailable)
                    {
                        var viewport = ReimbursementViews(page).OfType<ScrollView>().Single(view => view.Orientation == ScrollOrientation.Vertical);
                        await viewport.ScrollToAsync(refund.Single(), ScrollToPosition.MakeVisible, false); await Task.Delay(200, ct);
                        var deadline = DateTime.UtcNow.AddSeconds(20); Android.Widget.Button? native;
                        while ((native = refund.Single().Handler?.PlatformView as Android.Widget.Button) is null || !native.Enabled
                            || !native.IsShown || !native.HasWindowFocus || native.Width < 44 || native.Height < 44
                            || NativeName(native) != NativeDigits.Apply(text["Entry_Refund"]))
                        { if (DateTime.UtcNow >= deadline) throw new ReviewCheckException($"Original refund native target: {language}/{choice}/{item.Name}; exists={native is not null}; enabled={native?.Enabled}; shown={native?.IsShown}; focus={native?.HasWindowFocus}; size={native?.Width}x{native?.Height}; name={(native is null ? null : NativeName(native))}; expected={NativeDigits.Apply(text["Entry_Refund"])}."); await Task.Delay(50, ct); }
                        using var visible = new Android.Graphics.Rect();
                        if (!native.GetGlobalVisibleRect(visible) || visible.Height() < native.Height - 1)
                            throw new ReviewCheckException("Original refund target is outside its visible viewport.");
                    }
                    observations.Add(new { Language = language, Theme = choice.ToString(), State = item.Name,
                        ManagedHolding = model.IsHoldingMoney, ExistingCanRefund = model.CanRefund, VisibleRefundTargets = refund.Count,
                        ManagedSourceActionsPreserved = true, ActualNativeActionChecked = true });
                    if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original detail navigation changed.");
                    await Shell.Current.Navigation.PopAsync(false); await AwaitRootHomeAsync(ct);
                }
            }
        }
        finally
        {
            if (Shell.Current.Navigation.ModalStack.LastOrDefault() is EntryEditorPage) await Shell.Current.Navigation.PopModalAsync(false);
            if (Shell.Current.CurrentPage is EntryDetailPage) await Shell.Current.Navigation.PopAsync(false);
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, originalLanguage.CultureName, ct);
        }
        if (before != await ImportFinancialNativeReview.StoredAsync(services, ct))
            throw new ReviewCheckException("Original native action review changed stored rows.");
        return new { Comparison = comparison, Observations = observations, RefundModalInvocationDeferredToPhysicalLifecycle = true,
            CompleteStoredRowsUnchangedAfterSeeding = true, FinancialSavesInvoked = 0, OriginalThemeAndLanguageRestored = true,
            MainPackageTouched = false, SystemSettingsChanged = false };
    }
}
