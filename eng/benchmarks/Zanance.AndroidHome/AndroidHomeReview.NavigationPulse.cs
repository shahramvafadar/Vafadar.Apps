using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Distinguishes an unresponsive native window from a pending original publication in a bounded read-only sequence.</summary>
    private static async Task<object> ReviewNavigationPulseAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct);
        var source = Path.Combine(root, "original-secondary-fields");
        using var original = await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, source, ct);
        var before = await original.AllMoneyRowsAsync(ct); var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var languageBefore = local.CurrentLanguage.CultureName;
        var themes = services.GetRequiredService<ThemeService>(); var themeBefore = themes.Choice;
        var currentStage = "starting"; Task? pending = null; long uiTick = Environment.TickCount64;
        long maxAge = 0; long lastReport = 0; var queued = 0; var observations = new List<object>();
        void Stage(string name, Task? task = null)
        {
            Volatile.Write(ref currentStage, name); Volatile.Write(ref pending, task);
            LogReport("navigation-pulse-stage", comparison, new { Stage = name, TaskStatus = task?.Status.ToString() });
        }
        using var timer = new System.Threading.Timer(_ =>
        {
            var now = Environment.TickCount64; var age = now - Volatile.Read(ref uiTick);
            if (age > Interlocked.Read(ref maxAge)) Interlocked.Exchange(ref maxAge, age);
            if (age > 2000 && now - Interlocked.Read(ref lastReport) >= 1000)
            {
                Interlocked.Exchange(ref lastReport, now);
                LogReport("navigation-pulse-stall", comparison, new { Stage = Volatile.Read(ref currentStage),
                    TaskStatus = Volatile.Read(ref pending)?.Status.ToString(), UiHeartbeatAgeMs = age, PendingUiPulse = Volatile.Read(ref queued) });
            }
            // Keep at most one queued probe; an unresponsive window must not accumulate diagnostic callbacks.
            if (Interlocked.CompareExchange(ref queued, 1, 0) == 0)
                app.Dispatcher.Dispatch(() => { Interlocked.Exchange(ref uiTick, Environment.TickCount64); Volatile.Write(ref queued, 0); });
        }, null, 500, 500);
        async Task Theme(ThemeChoice choice)
        {
            if (themes.Choice == choice) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(choice); await rebuilt.Task.WaitAsync(ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            await AwaitRootHomeAsync(ct);
        }
        async Task Back(Page page)
        {
            await AwaitOriginalMoneyWindowAsync(ct);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page) || Platform.CurrentActivity is not AndroidX.Activity.ComponentActivity activity)
                throw new ReviewCheckException("Original current page required for native Back.");
            activity.OnBackPressedDispatcher.OnBackPressed(); await Task.Yield();
        }
        app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        try
        {
            for (var index = 0; index < 18; index++)
            {
                var language = new[] { "en", "fa", "de" }[index % 3]; var theme = index % 2 == 0 ? ThemeChoice.Light : ThemeChoice.Dark;
                Stage(index + "-display"); await Theme(theme); await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                var start = Environment.TickCount64;
                await original.ReadGoalNavigationAsync(index.ToString(), index % 2 != 0, Back, Stage, ct);
                var packet = new { Cycle = index, Language = language, Theme = theme.ToString(), Quantity = index % 2 != 0,
                    ObservedReadBackMs = Environment.TickCount64 - start, MaxUiHeartbeatAgeMs = Interlocked.Read(ref maxAge),
                    ToolbarSubscriptions = CountToolbarSubscriptions(), NativeRootFragments = CountNativeRootFragments(), FinancialWrites = 0 };
                observations.Add(packet); LogReport("navigation-pulse-cycle", comparison, packet);
            }
            Stage("source-check");
            if (before != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new ReviewCheckException("Original navigation sequence changed source/base rows.");
            Stage("complete");
            return new { Comparison = comparison, Scope = "navigation-pulse", Observations = observations,
                MaxUiHeartbeatAgeMs = Interlocked.Read(ref maxAge), OriginalSourceAndBaseUnchanged = true, FinancialWrites = 0, Device = DeviceEvidence() };
        }
        finally
        {
            Stage("restore-display"); await Theme(themeBefore); await SetLanguageAsync(app, local, languageBefore, ct); await AwaitRootHomeAsync(ct);
        }
    }
}
