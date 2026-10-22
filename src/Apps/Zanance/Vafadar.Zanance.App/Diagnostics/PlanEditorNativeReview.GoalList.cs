#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Exercises only original list reads, a native detail visit/back and local read-only Retry.</summary>
    internal async Task GoalListContextAsync(string context, Func<Page, VisualElement, Task> invoke,
        Func<Page, Task> back, Func<Page, string, Task> observe, CancellationToken ct, bool navigation = true)
    {
        if (navigation && File.Exists(Path.Combine(_folder, "goal-list-" + context + "-opened.json")))
            throw new IOException("Original native detail visit already accepted; do not replay it to repair an observer.");
        var before = await AllMoneyRowsAsync(ct);
        var goals = new GoalStore(Factory, new ReviewAccess()); var dates = new DateFormatter(Localization);
        var holdingText = new HoldingText(Translator, Localization, dates);
        var holdings = new HoldingStore(Factory, new ReviewAccess());
        var presenter = new GoalPresenter(Translator, dates, Localization, holdings, holdingText);
        GoalDetailViewModel? detail = null; GoalDetailPage? detailPage = null; string? detailRoute = null;
        var routeCount = 0;
        var effects = new GoalListEffects(async (target, args) =>
        {
            if (target != AppRoutes.GoalDetailRoute || args is null || !args.TryGetValue("id", out var value) || value is not Guid id)
                throw new IOException("Only original goal detail navigation belongs to this native scope.");
            detail = new GoalDetailViewModel(Store, goals, Plans, holdings, presenter, Translator, dates, Localization, Time, _host.GetRequiredService<IAppInteraction>());
            detail.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = id });
            detailPage = new GoalDetailPage(detail); detailRoute = await ShowAsync(detailPage, new() { ["id"] = id }); routeCount++;
        });
        var model = new GoalsViewModel(Store, goals, presenter, Translator, Time, effects);
        var page = new GoalsPage(model); string? route = null;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Factory.GateRequested = false; Factory.Gate = gate;
        try
        {
            route = await ShowAsync(page);
            await WaitAsync(() => Factory.GateRequested && model.Loading.IsLoading && page.IsLoaded, ct);
            if (model.AddCommand.CanExecute(null) || page.FindByName<Grid>("ReadySurface").IsVisible)
                throw new IOException("Original goal list was actionable before its complete reader.");
            await observe(page, context + "-loading"); gate.TrySetResult();
            await page.PendingPublication.WaitAsync(ct); await WaitAsync(() => model.Loading.IsReady, ct);
            await CheckRows(); await observe(page, context + "-ready");
            if (navigation)
            {
                var row = model.Active.First(); var command = model.OpenCommand;
                var open = Views(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, command) && ReferenceEquals(button.CommandParameter, row));
                await invoke(page, open);
                await WaitAsync(() => routeCount == 1 && detail is { CanInteract: true, NotFound: false } && detail.Summary?.Id == row.Id && detailPage is { IsLoaded: true }, ct);
                if (routeCount != 1 || command.CanExecute(row)) throw new IOException("Original goal list command lifetime differs after departure.");
                // Persist native acceptance before any fallible presentation observer. Never replay this visit to repair an image.
                var acceptance = Path.Combine(_folder, "goal-list-" + context + "-opened.json");
                await using (var file = new FileStream(acceptance, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    await System.Text.Json.JsonSerializer.SerializeAsync(file, new { ProcessId = Environment.ProcessId, Goal = row.Id, AcceptedNavigation = 1 }, cancellationToken: ct);
                await observe(detailPage!, context + "-detail");
                await back(detailPage!);
                await WaitAsync(() => ReferenceEquals(Shell.Current.CurrentPage, page) && model.Loading.IsReady, ct);
                await page.PendingPublication.WaitAsync(ct);
                await command.ExecuteAsync(row);
                if (routeCount != 1) throw new IOException("Retired original list command navigated after return.");
                if (detailRoute is not null) { Routing.UnRegisterRoute(detailRoute); _routes.Remove(detailRoute); detailRoute = null; }
                await CheckRows(); await observe(page, context + "-returned");
            }
            Factory.RefuseOnce = true;
            try { await model.LoadAsync(); throw new IOException("Declared original goal list read refusal was not observed."); }
            catch (IOException) when (model.Loading.HasFailed) { }
            if (model.AddCommand.CanExecute(null) || model.Active.Any(row => model.OpenCommand.CanExecute(row))
                || page.FindByName<Grid>("ReadySurface").IsVisible || !page.FindByName<VerticalStackLayout>("ErrorSurface").IsVisible)
                throw new IOException("Failed original goal list did not retain its local retry cover.");
            await observe(page, context + "-failed");
            await invoke(page, page.FindByName<Button>("RetryLoadButton"));
            await page.PendingPublication.WaitAsync(ct); await WaitAsync(() => model.Loading.IsReady, ct);
            await CheckRows(); await observe(page, context + "-retried");
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original goal list changed stored financial data.");
            Observations.Add(new { Context = context, ProcessId = Environment.ProcessId, Active = model.Active.Count,
                Finished = model.Finished.Count, Earmarks = model.Earmarks.Count, NativeDetailVisits = routeCount,
                NativeRetry = true, RetiredCommandRefused = navigation, CompleteOriginalRowsUnchanged = true });
        }
        finally
        {
            gate.TrySetResult(); Factory.Gate = null; Factory.RefuseOnce = false; model.Retire();
            if (route is not null) await LeaveAsync(route);
            if (detailRoute is not null) { Routing.UnRegisterRoute(detailRoute); _routes.Remove(detailRoute); }
        }

        async Task CheckRows()
        {
            var snapshot = await goals.ReadListSnapshotAsync(ct);
            var progress = (await presenter.EvaluateAsync(snapshot.Goals, snapshot.Accounts, snapshot.Entries,
                DateOnly.FromDateTime(Time.GetLocalNow().DateTime))).ToDictionary(item => item.Goal.Id);
            var rows = snapshot.Goals.Goals.Select(goal => presenter.Row(goal, progress.GetValueOrDefault(goal.Id), snapshot.Goals.Work)).ToArray();
            var active = snapshot.Goals.Goals.Where(goal => goal.State is GoalState.Active or GoalState.Paused).Select(goal => goal.Id).ToHashSet();
            if (active.Count == 0 || !model.Active.SequenceEqual(rows.Where(row => active.Contains(row.Id)))
                || !model.Finished.SequenceEqual(rows.Where(row => !active.Contains(row.Id))) || !model.HasActive || model.IsEmpty)
                throw new IOException("Original financial goal rows differ from the established presenter.");
            await goals.ValidateListSnapshotAsync(snapshot, ct);
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original goal source changed during read-only publication.");
        }
    }

    /// <summary>Only this declared original detail route is available; no dialogs or financial decisions are scripted.</summary>
    private sealed class GoalListEffects(Func<string, IDictionary<string, object>?, Task> navigate) : IAppInteraction
    {
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => navigate(route, parameters);
        public Task AlertAsync(string title, string message, string cancel) => throw Refused();
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw Refused();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw Refused();
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw Refused();
        public Task ShowFailureAsync(Exception exception) => throw Refused();
        private static IOException Refused() => new("No dialog or financial action belongs to original goal-list qualification.");
    }
}
#endif
