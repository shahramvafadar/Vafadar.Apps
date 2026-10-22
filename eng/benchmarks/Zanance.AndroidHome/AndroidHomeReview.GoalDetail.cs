using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Runs only original goal-detail publication, read failure/Retry and detail-return qualification.</summary>
    private static async Task<object> ReviewGoalDetailAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
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
        var editFailureOnly = Platform.CurrentActivity?.Intent?.GetStringExtra("review_goal_detail_edit_failure") == "1";
        var observations = new List<object>(); var contexts = new List<object>(); var retained = 0;
        var observed = Platform.CurrentActivity?.Intent?.GetStringExtra("review_goal_detail_observed");
        if (observed is not null && !System.Text.RegularExpressions.Regex.IsMatch(observed, "^[0-9a-f]{32}$"))
            throw new ReviewCheckException("Original observation pass must be an exact owned identifier.");
        var retryObserved = Platform.CurrentActivity?.Intent?.GetStringExtra("review_goal_detail_retry_observed");
        if (retryObserved is not null && (observed is null || !retryObserved.StartsWith(observed + "-", StringComparison.Ordinal)))
            throw new ReviewCheckException("Observed Retry must belong to the exact original observation pass.");
        var order = (from language in new[] { "en", "fa", "de" }
            from theme in new[] { ThemeChoice.Light, ThemeChoice.Dark } from quantity in new[] { false, true }
            select (Language: language, Theme: theme, Quantity: quantity)).ToArray();
        string OldContext((string Language, ThemeChoice Theme, bool Quantity) item) => observed + "-" + item.Language + "-" + item.Theme + (item.Quantity ? "-quantity" : "-earmark");
        string OldFile(string context, string stage) => Path.Combine(root, "goal-detail-" + observed + "-" + context + "-" + stage + ".json");
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
            LogReport("goal-detail-action-target", comparison, new { Native = native.GetType().FullName, native.Width, native.Height,
                VisibleWidth = visible.Width(), VisibleHeight = visible.Height(), native.IsShown, native.HasWindowFocus, native.Enabled,
                OwnPackage = native.Context?.PackageName == Package, WholeVisible = whole, Logical = target.Bounds });
            // Card overlays deliberately extend by their negative margin and are clipped by the card.
            // Require a visible native touch target rather than the clipped decorative perimeter.
            var density = native.Resources?.DisplayMetrics?.Density ?? 1f;
            var usable = shown && visible.Width() >= 44 * density && visible.Height() >= 44 * density;
            if (native.Context?.PackageName != Package || !usable)
                throw new ReviewCheckException("Original owned visible goal-detail action required before invocation.");
            if (!native.PerformClick()) throw new ReviewCheckException("Original native goal-detail invocation was not handled.");
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
                var previous = Path.Combine(root, "goal-detail-" + observed + "-" + stage.Replace(comparison, observed, StringComparison.Ordinal) + ".json");
                if (File.Exists(previous))
                {
                    using var saved = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(previous, ct));
                    if (!saved.RootElement.GetProperty("WindowOwned").GetBoolean()
                        || saved.RootElement.GetProperty("Readings").EnumerateArray().Any(value => !value.GetProperty("CompleteNativeText").GetBoolean()))
                        throw new ReviewCheckException("Original retained goal-detail observation is incomplete.");
                    observations.Add(saved.RootElement.Clone()); retained++; return;
                }
            }
            await AwaitOriginalMoneyWindowAsync(ct); await Task.Delay(100, ct);
            var targets = new List<VisualElement>();
            if (page.BindingContext is not GoalDetailViewModel model) throw new ReviewCheckException("Original detail model required.");
            if (model.IsLoading) targets.Add(page.FindByName<Label>("LoadingCaption"));
            else if (model.HasReadFailed)
            { targets.Add(page.FindByName<Label>("ErrorCaption")); targets.Add(page.FindByName<Button>("RetryLoadButton")); }
            else
            {
                if (!model.CanInteract || model.Summary is null) throw new ReviewCheckException("Original complete detail required.");
                static bool Visible(Element item)
                {
                    for (Element? parent = item; parent is not null; parent = parent.Parent)
                        if (parent is VisualElement visual && !visual.IsVisible) return false;
                    return true;
                }
                var captions = PlanEditorNativeReview.GoalDetailCaptions(model);
                targets.AddRange(PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(label => captions.Contains(label.Text ?? string.Empty)));
                if (captions.Any(caption => !targets.OfType<Label>().Any(label => label.Text == caption)))
                    throw new ReviewCheckException("An original financial detail caption is absent from its actual visible page.");
                var raw = new[] { model.AmountText, model.AssumedPriceText }.Where(value => !string.IsNullOrEmpty(value)).ToHashSet(StringComparer.Ordinal);
                targets.AddRange(PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(input => raw.Contains(input.Text ?? string.Empty)));
            }
            var readings = new List<object>();
            foreach (var target in targets)
            {
                await Scroll(target);
                await PlanEditorNativeReview.WaitAsync(() => target.Handler?.PlatformView is Android.Widget.TextView { Layout: not null, IsShown: true }, ct);
                var text = (Android.Widget.TextView)target.Handler!.PlatformView!; var layout = text.Layout!;
                var value = target switch { Label label => label.Text, Entry input => input.Text, Button button => button.Text, _ => null };
                // Inputs preserve original Latin text; only presentation captions receive native display digits.
                var expected = target is Entry ? value : Vafadar.Localization.Formatting.NativeDigits.Apply(value);
                using var visible = new Android.Graphics.Rect();
                if (text.Text != expected || !text.GetGlobalVisibleRect(visible)
                    || visible.Width() != text.Width || visible.Height() != text.Height
                    || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                {
                    LogReport("goal-detail-caption-failed", comparison, new { Stage = stage, Kind = target.GetType().Name,
                        Expected = expected, NativeText = text.Text, Logical = value, text.Width, text.Height,
                        VisibleWidth = visible.Width(), VisibleHeight = visible.Height() });
                    throw new ReviewCheckException("Original translated goal-detail caption is clipped or differs.");
                }
                MonetaryOsScope.RequireCompleteHeight(text, layout);
                readings.Add(new { Text = value, NativeText = text.Text, Kind = target.GetType().Name, text.Width, text.Height, Lines = layout.LineCount, CompleteNativeText = true });
            }
            var packet = new { Stage = stage, ProcessId = Environment.ProcessId, Readings = readings, WindowOwned = true };
            var file = Path.Combine(root, "goal-detail-" + comparison + "-" + stage + ".json");
            await using (var output = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await System.Text.Json.JsonSerializer.SerializeAsync(output, packet, cancellationToken: ct);
            observations.Add(packet); LogReport("goal-detail-observation", comparison, packet);
        }
        try
        {
            if (editFailureOnly)
            {
                await Theme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en", ct); await AwaitRootHomeAsync(ct);
                await original.GoalDetailEditFailureContextAsync(comparison + "-edit", Invoke, Back, Observe, ct);
                contexts.Add(original.Observations[^1]);
            }
            else
            for (var index = 0; index < order.Length; index++)
            {
                var item = order[index]; var oldContext = OldContext(item);
                var savedStages = new[] { "loading", "ready", "failed", "retried" };
                if (observed is not null && index + 1 < order.Length
                    && savedStages.All(stage => File.Exists(OldFile(oldContext, stage)))
                    && File.Exists(OldFile(OldContext(order[index + 1]), "loading")))
                {
                    // Sequential entry into the next original context proves the preceding guarded Back/source check returned.
                    foreach (var stage in savedStages)
                    {
                        using var saved = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(OldFile(oldContext, stage), ct));
                        if (!saved.RootElement.GetProperty("WindowOwned").GetBoolean()) throw new ReviewCheckException("Original retained detail window is not owned.");
                        observations.Add(saved.RootElement.Clone()); retained++;
                    }
                    contexts.Add(new { Context = oldContext, RetainedOriginalContext = true, NativeRetry = true, NativeBack = true, AcceptedWrites = 0,
                        Evidence = "All original stages and the next sequential context loading marker" });
                    continue;
                }
                await Theme(item.Theme); await SetLanguageAsync(app, local, item.Language, ct); await AwaitRootHomeAsync(ct);
                var context = comparison + "-" + item.Language + "-" + item.Theme + (item.Quantity ? "-quantity" : "-earmark");
                var repair = retryObserved == oldContext;
                if (repair && (!new[] { "loading", "ready", "failed" }.All(stage => File.Exists(OldFile(oldContext, stage)))
                    || File.Exists(OldFile(oldContext, "retried")))) throw new ReviewCheckException("Only the original unobserved Retry reading may be repaired.");
                var contextStart = observations.Count;
                await original.GoalDetailContextAsync(context, item.Quantity, Invoke, Back, Observe, ct, retryObserved: repair);
                if (repair)
                {
                    using var originalFailure = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(OldFile(oldContext, "failed"), ct));
                    if (!originalFailure.RootElement.GetProperty("WindowOwned").GetBoolean()) throw new ReviewCheckException("Original failed-read cover is not owned.");
                    observations.Insert(contextStart + 2, originalFailure.RootElement.Clone()); retained++;
                }
                contexts.Add(original.Observations[^1]);
            }
            if (before != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new ReviewCheckException("Original goal detail qualification changed source/base rows.");
            return new { Comparison = comparison, Scope = editFailureOnly ? "goal-detail-edit-failure" : "goal-detail", Observations = observations, Contexts = contexts,
                AcceptedWrites = 0, OriginalSourceAndBaseUnchanged = true, RetainedObservations = retained,
                OriginalObservationPass = observed, Device = DeviceEvidence() };
        }
        finally { await original.LeaveAsync(); await Theme(themeBefore); await SetLanguageAsync(app, local, languageBefore, ct); await AwaitRootHomeAsync(ct); }
    }
}
