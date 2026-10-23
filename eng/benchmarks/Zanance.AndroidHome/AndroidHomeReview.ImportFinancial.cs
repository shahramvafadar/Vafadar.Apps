using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original ordinary CSV financial guards in Release/AOT with actual native page buttons.</summary>
    private static async Task<object> ReviewImportFinancialAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var literal = Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_csv_literal", false) == true;
        var originalLanguage = local.CurrentLanguage; var originalTheme = theme.Choice; var results = new List<object>();
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
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                if (literal && (language != "en" || choice != ThemeChoice.Light)) continue;
                await SetTheme(choice); await SetLanguageAsync(app, local, language, ct);
                var context = language + "-" + choice;
                results.Add(await ImportFinancialNativeReview.RunAsync(services, Path.Combine(root, context), context,
                    async page => { await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(250, ct); },
                    async (page, command) =>
                    {
                        await AwaitReimbursementFocusAsync(ct);
                        var button = ReimbursementViews(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command) && Visible(item));
                        var scroll = ReimbursementViews(page).OfType<ScrollView>().Single(item => item.Orientation == ScrollOrientation.Vertical);
                        var inViewport = false;
                        for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                            if (ReferenceEquals(parent, scroll)) { inViewport = true; break; }
                        // The original Import/Refresh dock is a sibling of the scroll viewport.
                        if (inViewport) await scroll.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
                        await Task.Delay(150, ct);
                        if (button.Handler?.PlatformView is not Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true } native
                            || native.Context?.PackageName != Package || !native.PerformClick()) throw new ReviewCheckException("Original native import button unavailable.");
                        await Task.Yield();
                    },
                    async (state, page, caption) =>
                    {
                        await AwaitReimbursementFocusAsync(ct); await Task.Delay(200, ct);
                        var label = ReimbursementViews(page).OfType<Label>().Single(item => item.Text == caption && Visible(item));
                        if (literal)
                        {
                            var scroll = ReimbursementViews(page).OfType<ScrollView>().Single(item => item.Orientation == ScrollOrientation.Vertical);
                            for (Element? parent = label.Parent; parent is not null; parent = parent.Parent)
                                if (ReferenceEquals(parent, scroll)) { await scroll.ScrollToAsync(label, ScrollToPosition.MakeVisible, false); break; }
                            await Task.Delay(150, ct);
                        }
                        var expected = label.FlowDirection == FlowDirection.LeftToRight ? caption : Vafadar.Localization.Formatting.NativeDigits.Apply(caption);
                        if (label.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true } native
                            || native.Text != expected) throw new ReviewCheckException("Original financial review caption differs.");
                    },
                    async page =>
                    {
                        if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original import page changed.");
                        await Shell.Current.Navigation.PopAsync(false); await AwaitRootHomeAsync(ct);
                    }, ct, literalText: literal));
            }
        }
        finally
        {
            if (Shell.Current.CurrentPage is ImportExportPage) await Shell.Current.Navigation.PopAsync(false);
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, originalLanguage.CultureName, ct);
        }
        return new { Comparison = comparison, Cases = results, OriginalThemeAndLanguageRestored = true, NativeButtonsInvoked = true,
            FictitiousPickerAndUndoConsent = true, LiteralText = literal, MainPackageTouched = false, SystemSettingsChanged = false };
    }
}
