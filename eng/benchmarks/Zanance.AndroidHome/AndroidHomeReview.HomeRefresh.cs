using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original covered Home refresh in six actual Release/AOT contexts without Main writes.</summary>
    private static async Task<object> ReviewHomeRefreshAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct, bool referencesOnly = false)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var cases = new List<object>(); await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell();
        foreach (var choice in (referencesOnly ? new[] { ThemeChoice.Light } : new[] { ThemeChoice.Light, ThemeChoice.Dark }))
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            foreach (var language in (referencesOnly ? new[] { "en" } : new[] { "en", "fa", "de" }))
            {
                await SetLanguageAsync(app, local, language, ct); await AwaitReimbursementFocusAsync(ct);
                var context = language + "-" + choice + (referencesOnly ? "-references" : string.Empty);
                var result = await HomeRefreshNativeCase.RunAsync(services, Path.Combine(root, context + "-home-refresh.db"),
                    async page =>
                    {
                        page.FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                        await Shell.Current.Navigation.PushAsync(page, false);
                    },
                    async (page, home) =>
                    {
                        await AwaitReimbursementFocusAsync(ct);
                        var viewport = page.FindByName<ScrollView>("ContentViewport");
                        if (!viewport.IsVisible || viewport.Handler?.PlatformView is not Android.Views.View { IsShown: true })
                            throw new ReviewCheckException("Original Android Home is still covered after current source publication.");
                    }, async page =>
                    {
                        if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Only the original owned Home may return.");
                        await Shell.Current.Navigation.PopAsync(false);
                    }, ct, addAccount: referencesOnly);
                cases.Add(new { Context = context, Result = result });
            }
        }
        return new { Comparison = comparison, Cases = cases, MainPackageTouched = false, FinancialCommandsInvoked = false };
    }
}
