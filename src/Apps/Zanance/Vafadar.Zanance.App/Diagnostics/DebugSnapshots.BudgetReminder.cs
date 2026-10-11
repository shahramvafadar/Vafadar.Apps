#if DEBUG
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Actual original notification navigation in the hidden application; no Save or selected rights activation.</summary>
internal static partial class DebugSnapshots
{
    private static async Task ReviewBudgetReminderAsync(App app, IServiceProvider services, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
            Budgets = await store.GetBudgetsAsync(), Plans = await services.GetRequiredService<PlanStore>().GetSchedulesAsync() });
        var before = await StoredAsync();
        var review = await services.GetRequiredService<BudgetChoiceStore>().ReadAsync();
        var observations = new List<object>();
        foreach (var original in review.Items)
        {
            await services.GetRequiredService<AppLinkRouter>().OpenAsync(BudgetReminderLink.Format(review.ReminderScope, original.Id));
            await Task.Delay(350);
            if (Shell.Current.CurrentPage is not Features.Budget.BudgetPage page)
                throw new InvalidOperationException("The actual budget reminder did not navigate to the budget page.");
            var vm = (Features.Budget.BudgetViewModel)page.BindingContext;
            // The production navigation adapter must finish the destination; a diagnostic reload would mask a stale root tab.
            await Task.Delay(700);
            if (!vm.HasBudget || vm.SelectedCurrency != original.CurrencyCode || vm.PeriodIndex != (int)original.Period)
                throw new InvalidOperationException("The notification lost the original budget period/currency.");
            var expectedCalendar = Vafadar.Localization.Translator.Instance.Format("Budget_Currency", original.CurrencyCode,
                Vafadar.Localization.Translator.Instance[Presentation.Calendars.NameKey(original.Calendar)]);
            if (vm.CurrencyText != expectedCalendar) throw new InvalidOperationException("The original calendar changed on budget reload.");
#if WINDOWS
            if (original.Period == BudgetPeriod.Week)
            {
                var choices = page.FindByName<Vafadar.Maui.Controls.ChoiceChips>("BudgetPeriods");
                var chips = VisualDescendants(choices).OfType<Grid>().Where(chip => chip.Children.OfType<Button>().Any()).ToArray();
                InvokeChoiceReview(chips[0].Children.OfType<Button>().Single());
                await Task.Delay(150); await vm.LoadAsync(); await Task.Delay(700);
                if (vm.PeriodIndex != 0 || string.IsNullOrWhiteSpace(vm.PeriodText) || vm.CurrencyText != expectedCalendar)
                    throw new InvalidOperationException("An original weekly reminder lost its valid monthly navigation coordinates.");
                await CaptureAsync(app, folder, language + "-budget-reminder-month-navigation");
                await services.GetRequiredService<AppLinkRouter>().OpenAsync(BudgetReminderLink.Format(review.ReminderScope, original.Id));
                await Task.Delay(700);
                if (!vm.HasBudget || vm.PeriodIndex != 1 || vm.CurrencyText != expectedCalendar)
                    throw new InvalidOperationException("The retained budget tab did not reopen the original weekly reminder.");
            }
#endif
            await CaptureAsync(app, folder, language + "-budget-reminder-" + original.Id.ToString("N"));
            observations.Add(new { original.Id, original.Calendar, original.Period, vm.PeriodText, vm.CurrencyText,
                vm.SelectedCurrency, vm.HasBudget, OriginalLimit = original.TotalLimit, ActualTotalLines = vm.TotalLines.ToArray() });
        }
        if (before != await StoredAsync()) throw new InvalidOperationException("Opening budget notifications changed stored data.");
        await File.WriteAllTextAsync(Path.Combine(folder, language + "-budget-reminder-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { Observations = observations, ExactStoredRowsUnchanged = true }));
    }
}
#endif
