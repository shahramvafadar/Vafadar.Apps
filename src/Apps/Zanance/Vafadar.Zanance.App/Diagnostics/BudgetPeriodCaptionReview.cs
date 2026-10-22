#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Changed original budget captions/date header only; never repeats accepted financial qualification.</summary>
internal static class BudgetPeriodCaptionReview
{
    /// <summary>Renders one retained original monthly view and weekly editor on a separate, ledger-empty source.</summary>
    internal static async Task<object> RunAsync(IServiceProvider services, string root, IReadOnlyList<int> widths,
        Func<string, ThemeChoice, int, Task> context, Func<Button, Task> invoke,
        Func<ContentPage, string, IReadOnlyList<Label>, Task> observe, CancellationToken ct)
    {
        var source = Environment.GetEnvironmentVariable("VAFADAR_BUDGET_PERIOD_SOURCE");
        var path = Path.Combine(string.IsNullOrEmpty(source) ? root : source, "budget-period-captions.db"); var existed = File.Exists(path);
        if (!string.IsNullOrEmpty(source) && !existed) throw new IOException("Retained original caption source missing.");
        await using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        if (!existed)
        {
            await using var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
            var account = new Account { Name = "Original period caption account", CurrencyCode = "EUR", OpeningDate = today };
            db.Accounts.Add(account); db.Settings.Add(new ZananceSettings { Mode = ExperienceMode.Advanced, DefaultAccountId = account.Id });
            db.Budgets.AddRange(new Core.Budgets.Budget { Year = today.Year, Month = today.Month, CurrencyCode = "EUR", TotalLimit = 5000, AlertsEnabled = false },
                new Core.Budgets.Budget { Period = BudgetPeriod.Week, PeriodStart = BudgetPeriods.StartOf(BudgetPeriod.Week, today, DayOfWeek.Sunday),
                    CurrencyCode = "EUR", TotalLimit = 2000, AlertsEnabled = false });
            await db.SaveChangesAsync(ct);
        }
        var weekly = (await store.GetBudgetsAsync(ct)).Single(budget => budget.Period == BudgetPeriod.Week);
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        var baseRows = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var translator = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var dates = services.GetRequiredService<Vafadar.Localization.Formatting.IDateFormatter>(); var observations = new List<object>();
        Label Caption(ContentPage page, string text) => PlanEditorNativeReview.Views(page).OfType<Label>().First(label => label.Text == text && Visible(label));
        static bool Visible(VisualElement value)
        {
            for (Element? parent = value; parent is not null; parent = parent.Parent) if (parent is VisualElement { IsVisible: false }) return false;
            return true;
        }
        foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark }) foreach (var width in widths)
        {
            var key = width + "-" + language + "-" + theme; var receipt = Path.Combine(root, key + "-complete.json");
            if (File.Exists(receipt)) { observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt, ct))); continue; }
            await context(language, theme, width);
            var model = new BudgetViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(), translator,
                dates, local, TimeProvider.System, services.GetRequiredService<IAppInteraction>());
            var page = new BudgetPage(model).WithFlowDirection(local); await Shell.Current.Navigation.PushModalAsync(page);
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => page.IsLoaded && model.Actions.CanInteract, ct);
                await observe(page, key + "-view", [Caption(page, translator["Budget_CopyNext"])]);
                var next = PlanEditorNativeReview.Views(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, model.NextCommand));
                await invoke(next);
                await PlanEditorNativeReview.WaitAsync(() => model.NextCommand.ExecutionTask is not null, ct);
                await model.NextCommand.ExecutionTask!.WaitAsync(ct);
                if (model.HasBudget || !model.CanCopyPrevious) throw new IOException("Original next-period empty/previous alternative missing.");
                await observe(page, key + "-missing", [Caption(page, translator["Budget_None"]), Caption(page, translator["Budget_CopyPrevious"])]);
            }
            finally { if (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page)) await Shell.Current.Navigation.PopModalAsync(); }

            var editor = new BudgetEditorViewModel(store, translator, local, services.GetRequiredService<IAppInteraction>());
            await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = weekly.Id });
            var editorPage = new BudgetEditorPage(editor, local).WithFlowDirection(local); await Shell.Current.Navigation.PushModalAsync(editorPage);
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => editorPage.IsLoaded && editor.IsReady, ct);
                var first = dates.Format(weekly.PeriodStart, Vafadar.Localization.Formatting.DateFormatStyle.Short);
                var last = dates.Format(weekly.PeriodStart.AddDays(6), Vafadar.Localization.Formatting.DateFormatStyle.Short);
                if (editor.PeriodText != first + " – " + last || editor.IsDirty) throw new IOException("Original weekly date/clean draft identity changed.");
                await observe(editorPage, key + "-editor", [Caption(editorPage, translator["Budget_EditTitle"]), Caption(editorPage, editor.PeriodText),
                    Caption(editorPage, translator["Budget_TotalLimit"]), Caption(editorPage, translator["Rollover_Hint"])]);
            }
            finally { if (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), editorPage)) await Shell.Current.Navigation.PopModalAsync(); }
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Caption review changed original rows.");
            var result = new { Language = language, Theme = theme.ToString(), Width = width, ExactOriginalWeekDates = editor.PeriodText,
                CompleteChangedCaptions = 7, RawDraftUnchanged = true, OriginalRowsUnchanged = true, FinancialSaves = 0 };
            await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result), ct); observations.Add(result);
        }
        if (baseRows != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Independent base rows changed.");
        return new { Scope = "budget-period-captions", Observations = observations, OriginalBaseUnchanged = true, FinancialSaves = 0 };
    }
}
#endif
