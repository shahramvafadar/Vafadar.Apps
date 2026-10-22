using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks only changed original-purchase errors on actual owned Release/AOT editors.</summary>
    private static async Task<object> ReviewOriginalAmountErrorsAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var oldLanguage = local.CurrentLanguage.CultureName; var oldTheme = themes.Choice;
        async Task Context(string language, ThemeChoice theme, int width)
        {
            if (Platform.CurrentActivity?.Resources?.Configuration?.ScreenWidthDp != width) throw new ReviewCheckException("Original412dp window required; never change OS settings here.");
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
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(target, ScrollToPosition.Center, false); break; }
            await Task.Delay(100, ct);
        }
        try
        {
            var result = await OriginalAmountErrorCaptionReview.RunAsync(services, root, [412], Context, async button =>
            {
                await Scroll(button);
                await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Android.Views.View
                    { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true, Enabled: true }, ct);
                var native = (Android.Views.View)button.Handler!.PlatformView!;
                if (native.Context?.PackageName != Package || !native.PerformClick()) throw new ReviewCheckException("Original native validation command did not run.");
            }, async (page, key, labels) =>
            {
                Android.Util.Log.Info("ZananceHomeStage", "original-amount-errors|" + key);
                foreach (var label in labels)
                {
                    await Scroll(label);
                    await PlanEditorNativeReview.WaitAsync(() => label.Handler?.PlatformView is Android.Widget.TextView
                        { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true }, ct).WaitAsync(TimeSpan.FromSeconds(20), ct);
                    if (label.Handler?.PlatformView is not Android.Widget.TextView native || native.Context?.PackageName != Package
                        || native.Layout is not { } layout || layout.Height > native.Height - native.TotalPaddingTop - native.TotalPaddingBottom + 1)
                        throw new ReviewCheckException("Original purchase error has no complete native allocation.");
                    for (var line = 0; line < layout.LineCount; line++)
                        if (layout.GetEllipsisCount(line) != 0 || layout.GetLineRight(line) > native.Width - native.TotalPaddingRight + 1 || layout.GetLineLeft(line) < -1)
                            throw new ReviewCheckException("Original purchase error is truncated.");
                }
            }, ct);
            var report = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(result))!;
            report["Comparison"] = JsonSerializer.SerializeToElement(comparison); return report;
        }
        finally { await Context(oldLanguage, oldTheme, 412); }
    }
}
