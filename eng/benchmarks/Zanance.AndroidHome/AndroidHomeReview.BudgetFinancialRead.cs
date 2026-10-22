using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Reads the original three budget methods through actual Release/AOT pages on independent sources.</summary>
    private static async Task<object> ReviewBudgetFinancialReadAsync(App app, IServiceProvider services, string root, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var themes = services.GetRequiredService<ThemeService>(); var local = services.GetRequiredService<ILocalizationService>();
        var oldTheme = themes.Choice; var oldLanguage = local.CurrentLanguage.CultureName;
        static bool Visible(VisualElement value)
        {
            for (Element? parent = value; parent is not null; parent = parent.Parent)
                if (parent is VisualElement { IsVisible: false }) return false;
            return true;
        }
        async Task Context(string language, ThemeChoice theme)
        {
            if (themes.Choice != theme)
            {
                var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                app.ShellRebuilt += Rebuilt;
                try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
                finally { app.ShellRebuilt -= Rebuilt; }
            }
            await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
        }
        try
        {
            var result = await BudgetFinancialNativeReview.RunAsync(services, root, Context, async (page, key) =>
            {
                // The budget is a real modal window: its native child owns focus, not the covered Activity decor.
                Android.Util.Log.Info("ZananceHomeStage", "budget-financial|" + key);
                var vm = (Features.Budget.BudgetViewModel)page.BindingContext;
                await PlanEditorNativeReview.WaitAsync(() => PlanEditorNativeReview.Views(page).OfType<Label>().Any(value =>
                    value.Text == vm.TotalLines[0].StatusText && Visible(value)
                    && value.Handler?.PlatformView is Android.Views.View { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true }), ct)
                    .WaitAsync(TimeSpan.FromSeconds(20), ct);
                var label = PlanEditorNativeReview.Views(page).OfType<Label>().First(value => value.Text == vm.TotalLines[0].StatusText && Visible(value));
                var viewport = PlanEditorNativeReview.Views(page).OfType<ScrollView>().First(value => value.Orientation != ScrollOrientation.Horizontal);
                await viewport.ScrollToAsync(label, ScrollToPosition.Center, false); await Task.Delay(100, ct);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true } native
                    || native.Layout is not { } layout || layout.Height > native.Height - native.TotalPaddingTop - native.TotalPaddingBottom + 1)
                    throw new ReviewCheckException("Original budget monetary row is not visible in the owned native window.");
                for (var line = 0; line < layout.LineCount; line++)
                    if (layout.GetEllipsisCount(line) != 0 || layout.GetLineRight(line) > native.Width - native.TotalPaddingRight + 1 || layout.GetLineLeft(line) < -1)
                        throw new ReviewCheckException("Original budget monetary status is truncated.");
            }, ct);
            // Keep the original completed source observations; only the public launch binding is added here.
            var report = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(result))!;
            report["Comparison"] = JsonSerializer.SerializeToElement(Platform.CurrentActivity?.Intent?.GetStringExtra("review_pass"));
            return report;
        }
        finally { await Context(oldLanguage, oldTheme); }
    }
}
