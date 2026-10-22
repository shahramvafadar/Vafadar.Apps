using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Home;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Inspects only native observer identity and lifetime on original owned Shell roots.</summary>
    private static async Task<object> ReviewToolbarObserverAsync(App app, IServiceProvider services, string comparison, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct);
        var before = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var observations = new List<object>();
        var previous = new List<object>();
        for (var index = 0; index < 3; index++)
        {
            app.OpenReviewShell(); await AwaitRootHomeAsync(ct); await AwaitOriginalMoneyWindowAsync(ct);
            var field = typeof(Translator).GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new ReviewCheckException("Original publisher event unavailable.");
            var targets = (field.GetValue(Translator.Instance) as Delegate)?.GetInvocationList()
                .Select(handler => handler.Target).Where(target => target?.GetType().FullName?.EndsWith("AccessibleShellRenderer+AccessibleToolbarTracker", StringComparison.Ordinal) == true).ToArray() ?? [];
            var retired = previous.Select(target => new
            {
                Retired = (bool?)target.GetType().GetField("_retired", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target),
                ListenerRegistered = (bool?)target.GetType().GetField("_layoutListenerRegistered", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target),
                ToolbarReleased = target.GetType().GetField("_nativeToolbar", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) is null
            }).ToArray();
            if (retired.Any(state => state.Retired != true || state.ListenerRegistered != false || !state.ToolbarReleased))
                throw new ReviewCheckException("Original retired toolbar retained its listener or native owner.");
            var states = new List<object>();
            foreach (var target in targets)
            {
                var type = target!.GetType();
                var cached = type.GetField("_layoutObserver", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as Android.Views.ViewTreeObserver;
                var toolbar = type.GetField("_nativeToolbar", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as AndroidX.AppCompat.Widget.Toolbar;
                var current = toolbar?.ViewTreeObserver;
                states.Add(new { CachedPresent = cached is not null, CachedAlive = cached?.IsAlive, CurrentAlive = current?.IsAlive,
                    SameNativeObserver = cached is not null && current is not null && cached.Equals(current),
                    Retired = type.GetField("_retired", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as bool?,
                    NativeListener = type.GetField("_layoutListener", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) is Android.Views.ViewTreeObserver.IOnGlobalLayoutListener,
                    ListenerRegistered = type.GetField("_layoutListenerRegistered", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as bool? });
                if (current?.IsAlive != true || type.GetField("_layoutListener", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) is not Android.Views.ViewTreeObserver.IOnGlobalLayoutListener
                    || type.GetField("_layoutListenerRegistered", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) is not true)
                    throw new ReviewCheckException("Original current toolbar listener is not registered on a live native tree.");
            }
            var packet = new { Root = index, ActiveTrackers = targets.Length, Observers = states, Retired = retired };
            previous.Clear(); previous.AddRange(targets.Cast<object>());
            observations.Add(packet); LogReport("toolbar-observer-state", comparison, packet);
        }
        object? titleEvidence = null;
        if (Android.App.Application.Context.Resources?.Configuration?.FontScale == 2f)
        {
            var local = services.GetRequiredService<ILocalizationService>(); var language = local.CurrentLanguage.CultureName;
            try
            {
                await SetLanguageAsync(app, local, "fa", ct); await AwaitRootHomeAsync(ct);
                await Shell.Current.GoToAsync(AppShell.GoalsRoute, false);
                await PlanEditorNativeReview.WaitAsync(() => Shell.Current.CurrentPage is GoalsPage, ct);
                var page = (GoalsPage)Shell.Current.CurrentPage; await page.PendingPublication.WaitAsync(ct);
                await AwaitOriginalMoneyWindowAsync(ct);
                var toolbar = NavigationRootNativeViews(Platform.CurrentActivity!.Window!.DecorView)
                    .OfType<AndroidX.AppCompat.Widget.Toolbar>().Single(item => item.IsShown && item.Title == page.Title);
                var title = NavigationRootNativeViews(toolbar).OfType<Android.Widget.TextView>().Single(item => item.IsShown && item.Text == page.Title);
                await PlanEditorNativeReview.WaitAsync(() => title.Layout is not null, ct);
                using var rect = new Android.Graphics.Rect(); var layout = title.Layout!;
                if (!title.GetGlobalVisibleRect(rect) || rect.Width() != title.Width || rect.Height() != title.Height
                    || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                    throw new ReviewCheckException("Original native listener lost complete Persian title geometry.");
                MonetaryOsScope.RequireCompleteHeight(title, layout);
                var metrics = Android.App.Application.Context.Resources!.DisplayMetrics!;
                titleEvidence = new { Text = title.Text, title.Width, title.Height, LastLineBottom = layout.GetLineBottom(layout.LineCount - 1),
                    ToolbarHeight = toolbar.Height, NativeTextPixels = title.TextSize, WidthDp = metrics.WidthPixels / metrics.Density, FontScale = 2f };
            }
            finally
            {
                await Shell.Current.GoToAsync("//home", false);
                if (Shell.Current.CurrentPage is HomePage home) await home.PendingPublication.WaitAsync(ct);
                await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
            }
        }
        if (before != await ImportFinancialNativeReview.StoredAsync(services, ct))
            throw new ReviewCheckException("Observer inspection changed original independent rows.");
        return new { Comparison = comparison, Scope = "toolbar-observer", Observations = observations,
            OriginalBaseUnchanged = true, FinancialWrites = 0, Title = titleEvidence, Device = DeviceEvidence() };
    }
}
