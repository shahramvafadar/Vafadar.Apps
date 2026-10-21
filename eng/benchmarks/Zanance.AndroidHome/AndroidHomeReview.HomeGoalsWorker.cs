using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies changed Home goal evaluation on the original complete source, without another accepted write.</summary>
    private static async Task<object> ReviewHomeGoalsWorkerAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        if (Math.Abs((Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 0) - 1f) > 0.01f)
            throw new ReviewCheckException("Original normal-scale Home worker qualification required.");
        var folder = Path.Combine(root, "original-secondary-fields");
        if (!new[] { "goal", "price", "override", "partial", "bill" }
            .All(key => File.Exists(Path.Combine(folder, "secondary-fields-" + key + "-accepted.json"))))
            throw new ReviewCheckException("Completed original secondary source required; never seed another goal or payment.");
        using var original = await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, folder, ct);
        var before = await original.AllMoneyRowsAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var oldLanguage = local.CurrentLanguage.CultureName;
        var observations = new List<object>();
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            {
                var receipt = Path.Combine(root, "home-goals-worker-" + comparison + "-" + language + ".json");
                if (File.Exists(receipt)) throw new ReviewCheckException("Completed original Home worker scene must not be replayed.");
                await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var captured = await original.Store.GetHomeSnapshotAsync(today, 0, ct);
                var active = captured.Goals.Goals.Count(goal => goal.State is GoalState.Active or GoalState.Paused);
                if (active == 0) throw new ReviewCheckException("Original active financial goal calculation required.");
                var presenter = services.GetRequiredService<GoalPresenter>(); var translator = services.GetRequiredService<Translator>();
                var expected = GoalHomeProjection.Project(captured, presenter, translator, today);
                var model = ActivatorUtilities.CreateInstance<HomeViewModel>(services, original.Store);
                var publishedOnWindow = false; var publications = 0;
                void Published() { publications++; publishedOnWindow = MainThread.IsMainThread; }
                model.PresentingSnapshot += Published;
                var page = new HomePage(model); string? route = null;
                try
                {
                    route = await original.ShowAsync(page);
                    await PlanEditorNativeReview.WaitAsync(() => page.IsLoaded && model.Loading.IsReady && publications > 0, ct);
                    await page.PendingPublication.WaitAsync(ct); await AwaitOriginalMoneyWindowAsync(ct);
                    if (!publishedOnWindow || !model.PinnedGoals.SequenceEqual(expected.Pinned) || model.GoalAttentionText != expected.Attention
                        || model.HasPinnedGoals != (expected.Pinned.Count != 0))
                        throw new ReviewCheckException("Original complete Home goal publication differs.");
                    await original.Store.ValidateHomeSnapshotAsync(captured, ct);
                    if (before != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                        throw new ReviewCheckException("Home worker qualification changed original source/base rows.");
                    var packet = new { Language = language, ProcessId = Environment.ProcessId, ActiveGoals = active,
                        PinnedRows = model.PinnedGoals.ToArray(), model.GoalAttentionText, PublishedOnWindowThread = publishedOnWindow,
                        OriginalGoalPacketsMatch = true, CompleteSourceAndBaseUnchanged = true, AcceptedWrites = 0 };
                    await File.WriteAllTextAsync(receipt, System.Text.Json.JsonSerializer.Serialize(packet), ct);
                    observations.Add(packet); LogReport("home-goals-worker-observation", comparison, packet);
                }
                finally
                {
                    model.PresentingSnapshot -= Published; page.RetirePublication();
                    if (route is not null) await original.LeaveAsync(route);
                }
            }
            return new { Comparison = comparison, Scope = "home-goals-worker", Observations = observations,
                AcceptedWrites = 0, OriginalSourceAndBaseUnchanged = true, Device = DeviceEvidence(), GeneralAnrOrSpeedAcceptance = false };
        }
        finally { await original.LeaveAsync(); await SetLanguageAsync(app, local, oldLanguage, ct); await AwaitRootHomeAsync(ct); }
    }
}
