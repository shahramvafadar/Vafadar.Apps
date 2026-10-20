using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Uses original Release/AOT holding purchase/sale commands once, then retained read-only native outcome forms.</summary>
    private static async Task<object> ReviewHoldingFinancialAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var originalLanguage = local.CurrentLanguage; var originalTheme = theme.Choice; var observations = new List<object>();
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
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
            for (Element? current = item; current is not null; current = current.Parent)
                if (current is VisualElement view && !view.IsVisible) return false;
            return true;
        }
        async Task Show(AssetEventEditorPage page) { await Shell.Current.Navigation.PushModalAsync(page, false); await Task.Delay(250, ct); }
        async Task Close(AssetEventEditorPage page)
        {
            if (!ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page)) throw new ReviewCheckException("Original holding modal changed.");
            await Shell.Current.Navigation.PopModalAsync(false); await AwaitRootHomeAsync(ct);
        }
        async Task Invoke(AssetEventEditorPage page, ICommand command)
        {
            // The original modal owns focus; waiting for Activity focus here waits behind that same modal.
            var button = ReimbursementViews(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command) && Visible(item));
            var scroll = ReimbursementViews(page).OfType<ScrollView>().Single(item => item.Orientation == ScrollOrientation.Vertical);
            var inViewport = false;
            for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                if (ReferenceEquals(parent, scroll)) { inViewport = true; break; }
            if (inViewport) await scroll.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150, ct);
            var deadline = DateTime.UtcNow.AddSeconds(30);
            Android.Widget.Button? native;
            while ((native = button.Handler?.PlatformView as Android.Widget.Button) is null
                || !native.IsShown || !native.HasWindowFocus || !native.Enabled || native.Context?.PackageName != Package)
            {
                if (DateTime.UtcNow >= deadline) throw new ReviewCheckException("Original native split button unavailable.");
                await Task.Delay(50, ct);
            }
            // Invoke only once after the original modal's own native focus arrives.
            if (!native.PerformClick()) throw new ReviewCheckException("Original native split button unavailable.");
            await Task.Yield();
        }
        HoldingEventNativeReview.Prepared? prepared = null;
        try
        {
            await SetTheme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en", ct);
            prepared = await HoldingEventNativeReview.PrepareAsync(services, Path.Combine(root, "financial"), Show, Invoke, Close, ct);
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, language, ct);
                foreach (var state in prepared.States)
                {
                    state.Model.RetireReview();
                    state.Model.Error = services.GetRequiredService<Translator>()[state.ErrorKey];
                    var page = new AssetEventEditorPage(state.Model); await Show(page);
                    var label = ReimbursementViews(page).OfType<Label>().Single(item => item.Text == state.Model.Error && Visible(item));
                    var scroll = ReimbursementViews(page).OfType<ScrollView>().Single(item => item.Orientation == ScrollOrientation.Vertical);
                    await scroll.ScrollToAsync(label, ScrollToPosition.MakeVisible, false); await Task.Delay(200, ct);
                    var deadline = DateTime.UtcNow.AddSeconds(20);
                    Android.Widget.TextView? native;
                    while ((native = label.Handler?.PlatformView as Android.Widget.TextView) is null
                        || !native.IsShown || !native.HasWindowFocus || native.Context?.PackageName != Package
                        || native.Text != state.Model.Error || native.Layout is not { LineCount: > 0 })
                    {
                        if (DateTime.UtcNow >= deadline) throw new ReviewCheckException("Original modal feedback did not finish native publication.");
                        await Task.Delay(50, ct);
                    }
                    var layout = native.Layout!;
                    if (layout.GetLineBottom(layout.LineCount - 1) > native.Height - native.CompoundPaddingTop - native.CompoundPaddingBottom)
                        throw new ReviewCheckException("Original holding feedback is vertically clipped.");
                    using var visible = new Android.Graphics.Rect();
                    if (!native.GetGlobalVisibleRect(visible) || visible.Height() < native.Height - 1)
                        throw new ReviewCheckException("Original holding feedback is outside its visible viewport.");
                    for (var line = 0; line < layout.LineCount; line++)
                        if (layout.GetEllipsisCount(line) != 0 || layout.GetLineWidth(line) > native.Width - native.TotalPaddingLeft - native.TotalPaddingRight + 1)
                            throw new ReviewCheckException("Original holding feedback line is incomplete.");
                    if (state.Model.CanSave || state.Model.CloseText != services.GetRequiredService<Translator>()[state.Name == "accepted" ? "Common_Close" : "Common_Cancel"])
                        throw new ReviewCheckException("Original retained outcome became financially actionable.");
                    observations.Add(new { Language = language, Theme = choice.ToString(), State = state.Name, OriginalFeedback = native.Text,
                        Lines = layout.LineCount, Width = native.Width, Height = native.Height, ActualWindowFocus = true,
                        ReadOnlyOriginalOutcome = true, NoFinancialReplay = true });
                    await Close(page);
                }
            }
        }
        finally
        {
            if (Shell.Current.Navigation.ModalStack.LastOrDefault() is AssetEventEditorPage) await Shell.Current.Navigation.PopModalAsync(false);
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, originalLanguage.CultureName, ct);
        }
        return new { Comparison = comparison, Cases = prepared!.Cases, Observations = observations, OriginalThemeAndLanguageRestored = true,
            NativeButtonsInvoked = true,
            ReadOnlyPresentationAfterTwoFinancialCases = true, MainPackageTouched = false, SystemSettingsChanged = false };
    }
}
