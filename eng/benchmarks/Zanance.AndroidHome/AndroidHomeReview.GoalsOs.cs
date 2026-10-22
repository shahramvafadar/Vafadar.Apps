using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Reads the durable final receipt without replaying native actions after lost log chunks.</summary>
    private static async Task<object> ReadGoalsOsReportAsync(string root, string comparison, CancellationToken ct)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(comparison, "^[0-9a-f]{32}$"))
            throw new ReviewCheckException("Original report identity required.");
        var path = Path.Combine(root, "goals-os-" + comparison + "-result.json");
        var bytes = await File.ReadAllBytesAsync(path, ct);
        using var report = JsonDocument.Parse(bytes); var saved = report.RootElement;
        if (saved.GetProperty("Scope").GetString() != "goals-os" || saved.GetProperty("Comparison").GetString() != comparison
            || saved.GetProperty("AcceptedWrites").GetInt32() != 0 || !saved.GetProperty("OriginalSourceAndBaseUnchanged").GetBoolean())
            throw new ReviewCheckException("Complete original final goal report required.");
        var width = saved.GetProperty("Width").GetInt32(); var frames = 0; var contexts = 0;
        foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { "Light", "Dark" })
            foreach (var kind in new[] { "empty", "filled", "earmark", "quantity" })
            {
                var receipt = Path.Combine(root, "goals-os-" + width + "-" + language + "-" + theme + "-" + kind + ".complete.json");
                using var context = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct)); var item = context.RootElement;
                if (item.GetProperty("Width").GetInt32() != width || item.GetProperty("FontScale").GetSingle() != 2
                    || item.GetProperty("AcceptedWrites").GetInt32() != 0 || !item.GetProperty("SourceAndBaseUnchanged").GetBoolean())
                    throw new ReviewCheckException("Original complete goal context receipt required.");
                frames += item.GetProperty("Observations").GetArrayLength(); contexts++;
            }
        return new { Comparison = comparison, Scope = "goals-os-receipt", Width = width, Contexts = contexts,
            ContextObservations = frames, NewFinalPassObservations = saved.GetProperty("Observations").GetArrayLength(),
            AcceptedWrites = 0, OriginalSourceAndBaseUnchanged = true, DurableFinalReport = true,
            ReportSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) };
    }

    /// <summary>Qualifies only newly changed goal-page publication surfaces at actual OS scale on retained original data.</summary>
    private static async Task<object> ReviewGoalsOsAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var width = Platform.CurrentActivity?.Intent?.GetIntExtra("review_os_width", 0) ?? 0;
        var resources = Android.App.Application.Context.Resources;
        if (AppInfo.Current.PackageName != Package || width is not (360 or 412)
            || Math.Abs((resources?.Configuration?.FontScale ?? 0) - 2f) > 0.01f
            || resources?.DisplayMetrics is not { Density: > 0 } metrics || Math.Abs(metrics.WidthPixels / metrics.Density - width) > 1)
            throw new ReviewCheckException("Actual owned OS200% goal width required.");
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var source = Path.Combine(root, "original-secondary-fields");
        if (!new[] { "goal", "price", "override", "partial", "bill" }.All(key => File.Exists(Path.Combine(source, "secondary-fields-" + key + "-accepted.json"))))
            throw new ReviewCheckException("Complete original goal-page source receipts required; never reseed.");
        using var original = await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, source, ct);
        var before = await original.AllMoneyRowsAsync(ct); var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        if ((await services.GetRequiredService<GoalStore>().GetGoalsAsync(ct)).Count != 0)
            throw new ReviewCheckException("The retained independent base must already have no goals; never clear it for qualification.");
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var languageBefore = local.CurrentLanguage.CultureName; var themeBefore = themes.Choice;
        var observations = new List<object>(); var contexts = new List<object>();
        var prefix = "goals-os-" + width + "-"; var currentContext = string.Empty;
        async Task Theme(ThemeChoice choice)
        {
            if (themes.Choice == choice) return;
            var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => changed.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(choice); await changed.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            await AwaitRootHomeAsync(ct);
        }
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(target, ScrollToPosition.MakeVisible, false); break; }
            await Task.Delay(100, ct);
        }
        async Task Invoke(Page page, VisualElement target)
        {
            await AwaitOriginalMoneyWindowAsync(ct); await Scroll(target);
            await PlanEditorNativeReview.WaitAsync(() => target.Handler?.PlatformView is Android.Views.View
                { IsShown: true, HasWindowFocus: true, Enabled: true }, ct);
            var native = (Android.Views.View)target.Handler!.PlatformView!; using var rect = new Android.Graphics.Rect();
            if (!ReferenceEquals(Shell.Current.CurrentPage, page) || native.Context?.PackageName != Package
                || !native.GetGlobalVisibleRect(rect) || rect.Width() / metrics.Density < 44 || rect.Height() / metrics.Density < 44)
                throw new ReviewCheckException("Original visible owned44dp goal action required.");
            if (!native.PerformClick()) throw new ReviewCheckException("Original goal action did not handle its native click.");
            await Task.Yield();
        }
        async Task Back(Page page)
        {
            await AwaitOriginalMoneyWindowAsync(ct);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page) || Platform.CurrentActivity is not AndroidX.Activity.ComponentActivity activity)
                throw new ReviewCheckException("Original current goal page required for native Back.");
            activity.OnBackPressedDispatcher.OnBackPressed(); await Task.Yield();
        }
        static bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        static IEnumerable<Android.Views.View> NativeViews(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++)
                    if (group.GetChildAt(i) is { } child) foreach (var nested in NativeViews(child)) yield return nested;
        }
        static bool InToolbar(Android.Views.View view)
        {
            for (var parent = view.Parent; parent is Android.Views.View ancestor; parent = ancestor.Parent)
                if (ancestor is AndroidX.AppCompat.Widget.Toolbar) return true;
            return false;
        }
        async Task Observe(Page page, string stage)
        {
            await AwaitOriginalMoneyWindowAsync(ct);
            var targets = new List<VisualElement>(); var navOnly = stage.EndsWith("-detail", StringComparison.Ordinal);
            if (page.BindingContext is GoalsViewModel list)
            {
                if (list.Loading.IsLoading) targets.Add(page.FindByName<Label>("LoadingCaption"));
                else if (list.Loading.HasFailed)
                { targets.Add(page.FindByName<Label>("ErrorCaption")); targets.Add(page.FindByName<Button>("RetryLoadButton")); }
                else
                {
                    if (!list.Loading.IsReady) throw new ReviewCheckException("Complete current original list required.");
                    var captions = list.Active.Concat(list.Finished).SelectMany(row => new[] { row.Name, row.FundedText,
                        row.RemainingText, row.PlanText, row.DateText, row.SuggestionText, row.WarningText, row.StateText, row.WorkText })
                        .Concat(list.Earmarks.Select(item => item.Text)).Where(text => !string.IsNullOrEmpty(text)).Cast<string>().ToHashSet(StringComparer.Ordinal);
                    if (list.IsEmpty)
                    {
                        var translator = services.GetRequiredService<Translator>();
                        captions.Add(translator["Goals_EmptyTitle"]); captions.Add(translator["Goals_Intro"]);
                    }
                    targets.AddRange(PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(label => captions.Contains(label.Text ?? string.Empty)));
                    if (captions.Any(caption => !targets.OfType<Label>().Any(label => label.Text == caption)))
                        throw new ReviewCheckException("An original goal-list caption is absent.");
                    targets.AddRange(PlanEditorNativeReview.Views(page).OfType<Button>().Where(Visible).Where(button => ReferenceEquals(button.Command, list.AddCommand)));
                }
            }
            else if (page.BindingContext is GoalDetailViewModel detail)
            {
                if (detail.IsLoading) targets.Add(page.FindByName<Label>("LoadingCaption"));
                else if (detail.HasReadFailed)
                { targets.Add(page.FindByName<Label>("ErrorCaption")); targets.Add(page.FindByName<Button>("RetryLoadButton")); }
                else
                {
                    if (!detail.CanInteract || detail.Summary is null) throw new ReviewCheckException("Original complete goal detail required.");
                    if (!navOnly)
                    {
                        var captions = PlanEditorNativeReview.GoalDetailCaptions(detail);
                        captions.UnionWith(detail.PerAccount); captions.UnionWith(detail.TrendPeriods);
                        captions.UnionWith(detail.History.SelectMany(row => new[] { row.Text, row.DateText, row.AmountText }));
                        targets.AddRange(PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(label => captions.Contains(label.Text ?? string.Empty)));
                        if (captions.Any(caption => !targets.OfType<Label>().Any(label => label.Text == caption)))
                            throw new ReviewCheckException("An original goal-detail caption is absent.");
                        var raw = new[] { detail.AmountText, detail.AssumedPriceText }.Where(value => !string.IsNullOrEmpty(value)).ToHashSet(StringComparer.Ordinal);
                        targets.AddRange(PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(input => raw.Contains(input.Text ?? string.Empty)));
                    }
                }
            }
            else throw new ReviewCheckException("Original goal page model required.");
            var readings = new List<object>();
            foreach (var target in targets)
            {
                await Scroll(target);
                await PlanEditorNativeReview.WaitAsync(() => target.Handler?.PlatformView is Android.Widget.TextView { Layout: not null, IsShown: true }, ct);
                var native = (Android.Widget.TextView)target.Handler!.PlatformView!; var layout = native.Layout!;
                var text = target switch { Label label => label.Text, Entry input => input.Text, Button button => button.Text, _ => null };
                var expected = target is Entry ? text : NativeDigits.Apply(text); using var rect = new Android.Graphics.Rect();
                var shown = native.GetGlobalVisibleRect(rect);
                if (native.Context?.PackageName != Package || native.Text != expected || !shown
                    || rect.Width() != native.Width || rect.Height() != native.Height
                    || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                {
                    LogReport("goals-os-caption-failed", comparison, new { Stage = stage, Kind = target.GetType().Name,
                        Expected = expected, NativeText = native.Text, native.Width, native.Height,
                        VisibleWidth = rect.Width(), VisibleHeight = rect.Height(), Lines = layout.LineCount });
                    throw new ReviewCheckException("Original OS-scaled goal text is clipped or differs.");
                }
                try { MonetaryOsScope.RequireCompleteHeight(native, layout); }
                catch (IOException)
                {
                    LogReport("goals-os-caption-failed", comparison, new { Stage = stage, Kind = target.GetType().Name,
                        Expected = expected, NativeText = native.Text, native.Width, native.Height,
                        LastLineBottom = layout.GetLineBottom(layout.LineCount - 1),
                        native.CompoundPaddingTop, native.CompoundPaddingBottom, Lines = layout.LineCount });
                    throw;
                }
                if (target is Button && (rect.Width() / metrics.Density < 44 || rect.Height() / metrics.Density < 44
                    || !native.Enabled)) throw new ReviewCheckException("Original current goal target is smaller than44dp or disabled.");
                readings.Add(new { Text = text, NativeText = native.Text, Kind = target.GetType().Name, native.Width, native.Height,
                    NativeTextPixels = native.TextSize, Lines = layout.LineCount, CompleteNativeText = true });
            }
            Android.Widget.TextView? title = null;
            await PlanEditorNativeReview.WaitAsync(() =>
            {
                title = Platform.CurrentActivity?.Window?.DecorView is { } decor
                    ? NativeViews(decor).OfType<Android.Widget.TextView>().SingleOrDefault(text => text.IsShown && InToolbar(text) && text.Text == page.Title) : null;
                return title?.Layout is not null;
            }, ct);
            using var titleRect = new Android.Graphics.Rect();
            if (!title!.GetGlobalVisibleRect(titleRect) || titleRect.Width() != title.Width || titleRect.Height() != title.Height
                || Enumerable.Range(0, title.Layout!.LineCount).Any(line => title.Layout.GetEllipsisCount(line) != 0))
                throw new ReviewCheckException("Original OS-scaled goal toolbar title is incomplete.");
            try { MonetaryOsScope.RequireCompleteHeight(title, title.Layout!); }
            catch (IOException)
            {
                LogReport("goals-os-caption-failed", comparison, new { Stage = stage, Kind = "ToolbarTitle",
                    Expected = page.Title, NativeText = title.Text, title.Width, title.Height,
                    LastLineBottom = title.Layout!.GetLineBottom(title.Layout.LineCount - 1),
                    title.CompoundPaddingTop, title.CompoundPaddingBottom, Lines = title.Layout.LineCount });
                throw;
            }
            var packet = new { Stage = stage, Width = width, FontScale = resources!.Configuration!.FontScale,
                ProcessId = Environment.ProcessId, Readings = readings, Title = title.Text, TitleWidth = title.Width,
                TitleHeight = title.Height, NavigationReadinessOnly = navOnly, WindowOwned = true };
            var file = Path.Combine(root, prefix + stage + ".json");
            await using (var output = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await JsonSerializer.SerializeAsync(output, packet, cancellationToken: ct);
            observations.Add(packet); LogReport("goals-os-observation", comparison, packet);
        }
        async Task Empty(string context)
        {
            var factory = new PlanEditorNativeReview.ReviewFactory(services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var goals = new GoalStore(factory, services.GetRequiredService<ICommercialWriteAccessSource>());
            var dates = new DateFormatter(local); var translator = services.GetRequiredService<Translator>();
            var holdings = services.GetRequiredService<HoldingStore>();
            var presenter = new GoalPresenter(translator, dates, local, holdings, new HoldingText(translator, local, dates));
            var model = new GoalsViewModel(services.GetRequiredService<ZananceStore>(), goals, presenter, translator,
                services.GetRequiredService<TimeProvider>(), services.GetRequiredService<IAppInteraction>());
            var page = new GoalsPage(model); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            factory.Gate = gate; string? route = null;
            try
            {
                route = await original.ShowAsync(page);
                await PlanEditorNativeReview.WaitAsync(() => factory.GateRequested && model.Loading.IsLoading && page.IsLoaded, ct);
                await Observe(page, context + "-loading"); gate.TrySetResult(); await page.PendingPublication.WaitAsync(ct);
                if (!model.Loading.IsReady || !model.IsEmpty || model.Active.Count != 0 || model.Finished.Count != 0)
                    throw new ReviewCheckException("Original empty base goal list changed.");
                await Observe(page, context + "-ready"); var old = model.AddCommand;
                factory.RefuseOnce = true;
                try { await model.LoadAsync(); throw new ReviewCheckException("Declared empty-list read refusal was not observed."); }
                catch (IOException) when (model.Loading.HasFailed) { }
                await Observe(page, context + "-failed"); await Invoke(page, page.FindByName<Button>("RetryLoadButton"));
                await page.PendingPublication.WaitAsync(ct); await PlanEditorNativeReview.WaitAsync(() => model.Loading.IsReady, ct);
                if (!model.IsEmpty) throw new ReviewCheckException("Original empty list Retry created goals.");
                await Observe(page, context + "-retried"); await Back(page);
                await PlanEditorNativeReview.WaitAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page), ct);
                if (old.CanExecute(null)) throw new ReviewCheckException("Original empty-list action survived departure.");
                contexts.Add(new { Context = context, Kind = "empty", NativeRetry = true, NativeBack = true, AcceptedWrites = 0 });
            }
            finally { gate.TrySetResult(); factory.Gate = null; factory.RefuseOnce = false; model.Retire(); if (route is not null) await original.LeaveAsync(route); }
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                foreach (var kind in new[] { "empty", "filled", "earmark", "quantity" })
                {
                    currentContext = comparison + "-" + language + "-" + theme + "-" + kind;
                    var receipt = Path.Combine(root, prefix + language + "-" + theme + "-" + kind + ".complete.json");
                    if (File.Exists(receipt))
                    {
                        using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                        if (saved.RootElement.GetProperty("Width").GetInt32() != width || saved.RootElement.GetProperty("FontScale").GetSingle() != 2)
                            throw new ReviewCheckException("Original complete goal OS receipt differs.");
                        contexts.Add(saved.RootElement.Clone()); continue;
                    }
                    await Theme(theme); await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                    var first = observations.Count;
                    if (kind == "empty") await Empty(currentContext);
                    else if (kind == "filled")
                    { await original.GoalListContextAsync(currentContext, Invoke, Back, Observe, ct); contexts.Add(original.Observations[^1]); }
                    else
                    {
                        // Preserve a completed native Retry when only departure/receipt publication was interrupted.
                        var retainedRetry = Directory.EnumerateFiles(root, prefix + "*-" + language + "-" + theme + "-" + kind + "-retried.json").Any();
                        await original.GoalDetailContextAsync(currentContext, kind == "quantity", Invoke, Back, Observe, ct, retryObserved: retainedRetry);
                        contexts.Add(original.Observations[^1]);
                    }
                    if (before != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                        throw new ReviewCheckException("Original OS goal context changed source/base rows.");
                    var complete = new { Context = currentContext, Kind = kind, Width = width, FontScale = 2f,
                        Observations = observations.Skip(first).ToArray(), AcceptedWrites = 0, SourceAndBaseUnchanged = true };
                    await using var file = new FileStream(receipt, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    await JsonSerializer.SerializeAsync(file, complete, cancellationToken: ct);
                }
            }
            return new { Comparison = comparison, Scope = "goals-os", Width = width, FontScale = 2f,
                Contexts = contexts, Observations = observations, AcceptedWrites = 0, OriginalSourceAndBaseUnchanged = true, Device = DeviceEvidence() };
        }
        finally { await original.LeaveAsync(); await Theme(themeBefore); await SetLanguageAsync(app, local, languageBefore, ct); await AwaitRootHomeAsync(ct); }
    }
}
