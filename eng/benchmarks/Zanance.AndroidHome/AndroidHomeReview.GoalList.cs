using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Runs only original goal-list publication, read failure/Retry and detail-return qualification.</summary>
    private static async Task<object> ReviewGoalListAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var languageBefore = local.CurrentLanguage.CultureName; var themeBefore = themes.Choice;
        if (Math.Abs((Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 0) - 1f) > 0.01f)
            throw new ReviewCheckException("Original normal-scale list scope required.");
        var source = Path.Combine(root, "original-secondary-fields");
        if (!new[] { "goal", "price", "override", "partial", "bill" }.All(key => File.Exists(Path.Combine(source, "secondary-fields-" + key + "-accepted.json"))))
            throw new ReviewCheckException("Complete original secondary source required; never repeat accepted seeds or money.");
        using var original = await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, source, ct);
        var before = await original.AllMoneyRowsAsync(ct); var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var observations = new List<object>(); var contexts = new List<object>(); var retained = 0;
        var observed = Platform.CurrentActivity?.Intent?.GetStringExtra("review_goal_list_observed");
        if (observed is not null && !System.Text.RegularExpressions.Regex.IsMatch(observed, "^[0-9a-f]{32}$"))
            throw new ReviewCheckException("Original observation pass must be an exact owned identifier.");
        async Task Theme(ThemeChoice choice)
        {
            if (themes.Choice == choice) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            await AwaitRootHomeAsync(ct);
        }
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(target, ScrollToPosition.MakeVisible, false); break; }
            await Task.Delay(80, ct);
        }
        async Task Invoke(Page page, VisualElement target)
        {
            await AwaitOriginalMoneyWindowAsync(ct); await Scroll(target);
            await PlanEditorNativeReview.WaitAsync(() => target.Handler?.PlatformView is Android.Views.View { IsShown: true, HasWindowFocus: true, Enabled: true }, ct);
            var native = (Android.Views.View)target.Handler!.PlatformView!; using var visible = new Android.Graphics.Rect();
            var shown = native.GetGlobalVisibleRect(visible);
            var whole = shown && visible.Width() == native.Width && visible.Height() == native.Height;
            LogReport("goal-list-action-target", comparison, new { Native = native.GetType().FullName, native.Width, native.Height,
                VisibleWidth = visible.Width(), VisibleHeight = visible.Height(), native.IsShown, native.HasWindowFocus, native.Enabled,
                OwnPackage = native.Context?.PackageName == Package, WholeVisible = whole, Logical = target.Bounds });
            // Card overlays deliberately extend by their negative margin and are clipped by the card.
            // Require a visible native touch target rather than the clipped decorative perimeter.
            var density = native.Resources?.DisplayMetrics?.Density ?? 1f;
            var usable = shown && visible.Width() >= 44 * density && visible.Height() >= 44 * density;
            if (native.Context?.PackageName != Package || !usable)
                throw new ReviewCheckException("Original owned visible goal-list action required before invocation.");
            if (!native.PerformClick()) throw new ReviewCheckException("Original native goal-list invocation was not handled.");
            await Task.Delay(80, ct);
        }
        async Task Back(Page page)
        {
            await AwaitOriginalMoneyWindowAsync(ct);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page) || Platform.CurrentActivity is not AndroidX.Activity.ComponentActivity activity)
                throw new ReviewCheckException("Original actual detail page required before platform Back.");
            activity.OnBackPressedDispatcher.OnBackPressed(); await Task.Delay(80, ct);
        }
        async Task Observe(Page page, string stage)
        {
            if (observed is not null)
            {
                var previous = Path.Combine(root, "goal-list-" + observed + "-" + stage.Replace(comparison, observed, StringComparison.Ordinal) + ".json");
                if (File.Exists(previous))
                {
                    using var saved = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(previous, ct));
                    if (!saved.RootElement.GetProperty("WindowOwned").GetBoolean()
                        || saved.RootElement.GetProperty("Readings").EnumerateArray().Any(value => !value.GetProperty("CompleteNativeText").GetBoolean()))
                        throw new ReviewCheckException("Original retained goal-list observation is incomplete.");
                    observations.Add(saved.RootElement.Clone()); retained++; return;
                }
            }
            await AwaitOriginalMoneyWindowAsync(ct); await Task.Delay(100, ct);
            var targets = new List<VisualElement>();
            if (page.BindingContext is GoalsViewModel model)
            {
                if (model.Loading.IsLoading) targets.Add(page.FindByName<Label>("LoadingCaption"));
                else if (model.Loading.HasFailed)
                { targets.Add(page.FindByName<Label>("ErrorCaption")); targets.Add(page.FindByName<Button>("RetryLoadButton")); }
                else
                {
                    if (!model.Loading.IsReady || model.Active.Count == 0) throw new ReviewCheckException("Original complete active goal list required.");
                    targets.Add(PlanEditorNativeReview.Views(page).OfType<Label>().First(label => label.Text == model.Active[0].Name));
                }
            }
            else if (page.BindingContext is not GoalDetailViewModel { CanInteract: true, Summary: not null })
                throw new ReviewCheckException("Original completed detail state required.");
            var readings = new List<object>();
            foreach (var target in targets)
            {
                await Scroll(target);
                await PlanEditorNativeReview.WaitAsync(() => target.Handler?.PlatformView is Android.Widget.TextView { Layout: not null, IsShown: true }, ct);
                var text = (Android.Widget.TextView)target.Handler!.PlatformView!; var layout = text.Layout!;
                var value = target is Label label ? label.Text : ((Button)target).Text;
                using var visible = new Android.Graphics.Rect();
                if (text.Text != Vafadar.Localization.Formatting.NativeDigits.Apply(value) || !text.GetGlobalVisibleRect(visible)
                    || visible.Width() != text.Width || visible.Height() != text.Height
                    || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                    throw new ReviewCheckException("Original translated goal-list caption is clipped or differs.");
                MonetaryOsScope.RequireCompleteHeight(text, layout);
                readings.Add(new { Text = value, text.Width, text.Height, Lines = layout.LineCount, CompleteNativeText = true });
            }
            var packet = new { Stage = stage, ProcessId = Environment.ProcessId, Readings = readings, WindowOwned = true };
            var file = Path.Combine(root, "goal-list-" + comparison + "-" + stage + ".json");
            await using (var output = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await System.Text.Json.JsonSerializer.SerializeAsync(output, packet, cancellationToken: ct);
            observations.Add(packet); LogReport("goal-list-observation", comparison, packet);
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await Theme(theme); await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                var context = comparison + "-" + language + "-" + theme;
                await original.GoalListContextAsync(context, Invoke, Back, Observe, ct);
                contexts.Add(original.Observations[^1]);
            }
            if (before != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new ReviewCheckException("Original goal list qualification changed source/base rows.");
            return new { Comparison = comparison, Scope = "goal-list", Observations = observations, Contexts = contexts,
                AcceptedWrites = 0, OriginalSourceAndBaseUnchanged = true, RetainedObservations = retained,
                OriginalObservationPass = observed, Device = DeviceEvidence() };
        }
        finally { await original.LeaveAsync(); await Theme(themeBefore); await SetLanguageAsync(app, local, languageBefore, ct); await AwaitRootHomeAsync(ct); }
    }
}
