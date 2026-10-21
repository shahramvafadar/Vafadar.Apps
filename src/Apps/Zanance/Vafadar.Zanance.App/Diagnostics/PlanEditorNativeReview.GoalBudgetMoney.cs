#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Seeds only a new independent original account; no financial UI command is replayed.</summary>
    internal async Task PrepareGoalBudgetMoneyAsync(CancellationToken ct)
    {
        await using var db = await Factory.CreateDbContextAsync(ct);
        if (await db.Accounts.AnyAsync(ct)) throw new IOException("Fresh goal/budget fixture required.");
        db.Accounts.Add(new Account { Name = "Original EUR account", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) });
        await db.SaveChangesAsync(ct);
        await File.WriteAllTextAsync(Path.Combine(_folder, "goal-budget-prepared.json"), await AllMoneyRowsAsync(ct), ct);
    }
    /// <summary>Continues only the last exact complete original source checkpoint, without repeating accepted commands.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueGoalBudgetMoneyAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = new PlanEditorNativeReview(host, folder);
        var name = File.Exists(Path.Combine(folder, "budget-native-accepted.json")) ? "budget-native-accepted.json"
            : File.Exists(Path.Combine(folder, "goal-native-accepted.json")) ? "goal-native-accepted.json" : "goal-budget-prepared.json";
        if (await File.ReadAllTextAsync(Path.Combine(folder, name), ct) != await result.AllMoneyRowsAsync(ct)) throw new IOException("Original goal/budget checkpoint differs.");
        return result;
    }
    private sealed class MoneyNotifications : IGoalEditorNotifications
    {
        /// <inheritdoc />
        public bool IsSupported => false;
        /// <inheritdoc />
        public Task<bool> EnableAsync() => throw new IOException("Monetary review requested a device permission.");
    }
    private sealed class MoneyInteraction : IAppInteraction
    {
        /// <inheritdoc />
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => route == ".." ? Task.CompletedTask : throw new IOException("Unexpected money navigation.");
        /// <inheritdoc />
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Actual monetary review failed.", exception);
        /// <inheritdoc />
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected money alert.");
        /// <inheritdoc />
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Unexpected money confirmation.");
        /// <inheritdoc />
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected money prompt.");
        /// <inheritdoc />
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected money choice.");
    }
    private async Task SetTabMoneyCultureAsync(string? culture, CancellationToken ct)
    {
        if (Localization.FormattingCultureName == culture) return;
        var app = (App)Application.Current!; var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
        app.ShellRebuilt += Rebuilt;
        try { Localization.SetFormattingCulture(culture); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
        finally { app.ShellRebuilt -= Rebuilt; }
    }
    private static async Task ReturnMoneyTabAsync(bool pendingDisplay, CancellationToken ct)
    {
        if (!pendingDisplay) { await Shell.Current.GoToAsync("//home", false); return; }
        // The production app deliberately rebuilds cached tab content after a display change in an open form.
        // Await that exact rebuild before routing another fixture page; a later rebuild must not retire its owner.
        var app = (App)Application.Current!; var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
        app.ShellRebuilt += Rebuilt;
        try { await Shell.Current.GoToAsync("//home", false); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
        finally { app.ShellRebuilt -= Rebuilt; }
    }
    /// <summary>Gets only the new monetary captions, avoiding unrelated native layout replay.</summary>
    internal static HashSet<string> GoalBudgetMoneyCaptions(Page page)
    {
        IEnumerable<string?> values = page.BindingContext switch
        {
            GoalEditorViewModel goal => [goal.TargetUnitName, goal.ContributionUnitName, goal.TargetFormatNote, goal.ContributionFormatNote, goal.AmountError, goal.ContributionError, goal.MoneyReviewText],
            BudgetEditorViewModel budget => new[] { budget.TotalUnitName, budget.TotalFormatNote, budget.TotalError, budget.MoneyReviewText }
                .Concat(budget.Limits.SelectMany(row => new[] { row.UnitName, row.FormatNote, row.Error })),
            _ => [],
        };
        return values.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Retains complete schema and typed source rows, not just the financial subset.</summary>
    internal async Task<string> AllMoneyRowsAsync(CancellationToken ct)
    {
        await using var db = await Factory.CreateDbContextAsync(ct); await db.Database.OpenConnectionAsync(ct);
        using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        using (var reader = await command.ExecuteReaderAsync(ct)) while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));
        var rows = new SortedDictionary<string, string[]>();
        foreach (var table in tables)
        {
            command.CommandText = "SELECT * FROM \"" + table.Replace("\"", "\"\"") + "\"";
            using var reader = await command.ExecuteReaderAsync(ct); var values = new List<string>();
            while (await reader.ReadAsync(ct))
                values.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? null : reader.GetValue(i))));
            rows[table] = [.. values.Order(StringComparer.Ordinal)];
        }
        return JsonSerializer.Serialize(rows);
    }
    /// <summary>Observes both actual original editors and invokes at most one accepted Save for each complete fixture.</summary>
    internal async Task GoalBudgetMoneyContextAsync(string context, Func<Page, System.Windows.Input.ICommand, Task> invoke,
        Func<Page, string, Task> observe, bool accept, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName; var before = await AllMoneyRowsAsync(ct);
        var dates = new DateFormatter(Localization); var goals = new GoalStore(Factory, new ReviewAccess()); var holdings = Services.GetRequiredService<HoldingStore>();
        var interaction = new MoneyInteraction(); string? route = null; var pendingDisplay = false;
        try
        {
            if (completed?.Invoke(context + "-goal-reentry") != true)
            {
            DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
            var goal = new GoalEditorViewModel(Store, goals, Translator, Localization, dates, Time, holdings,
                new HoldingText(Translator, Localization, dates), interaction, new MoneyNotifications());
            var page = new GoalEditorPage(goal, Localization); route = await ShowAsync(page);
            await WaitAsync(() => goal.CanInteract && page.Width > 0, ct); await Task.Delay(200, ct);
            goal.TypeIndex = 1; goal.Name = "Original monetary target"; goal.AmountText = "1,234.56"; goal.ContributionText = "12.34";
            Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; goal.RefreshDisplay(); await Task.Delay(150, ct);
            if (goal.TargetUnitName != "EUR" || goal.TargetFormatNote is null || !goal.CanSave) throw new IOException("Original goal monetary format differs.");
            await observe(page, context + "-goal-original");
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); goal.RefreshDisplay(); goal.AmountText += "0"; goal.ContributionText += "0";
            if (goal.CanSave || goal.AmountError is null || goal.ContributionError is null || goal.TargetUnitName != "EUR") throw new IOException("Partial goal edit acknowledged another unit.");
            await observe(page, context + "-goal-reentry");
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only goal view changed original rows.");
            if (accept && !File.Exists(Path.Combine(_folder, "goal-native-accepted.json")))
            {
                goal.AmountText = string.Empty; goal.AmountText = "12,3456"; goal.ContributionText = string.Empty; goal.ContributionText = "0,1234";
                await invoke(page, goal.SaveCommand); await WaitAsync(() => !goal.IsBusy && !goal.IsDirty, ct);
                var saved = (await goals.GetGoalsAsync(ct)).Single(); var plan = (await goals.GetContributionPlansAsync(ct)).Single();
                if (saved.TargetAmount != 123456 || plan.Amount != 1234 || goal.CanSave) throw new IOException("Native goal acceptance changed monetary meaning.");
                await File.WriteAllTextAsync(Path.Combine(_folder, "goal-native-accepted.json"), await AllMoneyRowsAsync(ct), ct);
                before = await AllMoneyRowsAsync(ct);
            }
            await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false; Routing.UnRegisterRoute(route); _routes.Remove(route); route = null;
            }
            if (completed?.Invoke(context + "-budget-reentry") == true) return;
            DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
            var budget = new BudgetEditorViewModel(Store, Translator, Localization, interaction); var budgetPage = new BudgetEditorPage(budget, Localization);
            route = await ShowAsync(budgetPage, new() { ["year"] = 2026, ["month"] = 10, ["currency"] = "EUR" });
            await Task.Delay(500, ct);
            await File.WriteAllTextAsync(Path.Combine(_folder, context + "-budget-publication.json"), JsonSerializer.Serialize(new
            { budget.IsReady, budget.IsLoading, budget.CanInteract, budget.IsBusy, budget.Error, budget.WorkText, budgetPage.Width, budgetPage.Height, Current = Shell.Current.CurrentPage.GetType().Name }), ct);
            await WaitAsync(() => budget.CanInteract && budgetPage.Width > 0, ct); await Task.Delay(200, ct);
            budget.TotalText = "1,234.56"; var row = budget.Limits.First(); row.Text = "12.34";
            Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; budget.RefreshDisplay(); await Task.Delay(150, ct);
            if (budget.TotalUnitName != "EUR" || budget.TotalFormatNote is null || row.FormatNote is null) throw new IOException("Original budget format differs.");
            await observe(budgetPage, context + "-budget-original");
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); budget.RefreshDisplay(); budget.TotalText += "0"; row.Text += "0";
            if (budget.CanSave || budget.TotalError is null || row.Error is null || row.UnitName != "EUR") throw new IOException("Partial budget edit acknowledged another unit.");
            await observe(budgetPage, context + "-budget-reentry");
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only budget view changed original rows.");
            if (accept && !File.Exists(Path.Combine(_folder, "budget-native-accepted.json")))
            {
                budget.TotalText = string.Empty; budget.TotalText = "12,3456"; row.Text = string.Empty; row.Text = "0,1234";
                await invoke(budgetPage, budget.SaveCommand); await WaitAsync(() => !budget.IsBusy && !budget.IsDirty, ct);
                var saved = (await Store.GetBudgetsAsync(ct)).Single();
                if (saved.TotalLimit != 123456 || saved.CategoryLimits.Single().Limit != 1234 || budget.CanSave) throw new IOException("Native budget acceptance changed monetary meaning.");
                await File.WriteAllTextAsync(Path.Combine(_folder, "budget-native-accepted.json"), await AllMoneyRowsAsync(ct), ct);
            }
            if ((await Store.GetEntriesAsync(cancellationToken: ct)).Count != 0) throw new IOException("Monetary configuration posted ledger money.");
        }
        finally
        {
            await ReturnMoneyTabAsync(pendingDisplay, ct);
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(units); await SetTabMoneyCultureAsync(culture, ct);
        }
    }
}
#endif
