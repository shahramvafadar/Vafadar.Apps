#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-159 / AT-161: original packets change only in this disposable database, never in device profiles.
    private static async Task ReviewReminderInputsAsync(App app, IServiceProvider services, IServiceProvider fixture,
        ZananceStore store, PlanStore plans, GoalStore goals, IDbContextFactory<ZananceDbContext> factory,
        ReminderService reminders, ProfileReminderReviewScheduler scheduler, Account account, Schedule plan, Goal goal,
        Func<Task<string>> stored, string before, string folder, string[] languages)
    {
        var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var holdings = fixture.GetRequiredService<HoldingStore>();
        async Task RestoreAsync()
        {
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Schedules SET ReminderEnabled=1 WHERE Id={plan.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Accounts SET OpeningBalance={account.OpeningBalance} WHERE Id={account.Id}");
            if (await stored() != before) throw new InvalidOperationException("The complete original reminder fixture was not restored.");
        }
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var dates = services.GetRequiredService<IDateFormatter>();
            var presenter = new GoalPresenter(translator, dates, localization, holdings, new HoldingText(translator, localization, dates));
            var vm = new GoalsViewModel(store, goals, presenter, translator, TimeProvider.System, services.GetRequiredService<IAppInteraction>());
            var page = new GoalsPage(vm); ProfileNativeReads.Add(page);
            await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync(); await Task.Delay(250);
            var originalText = vm.Active.Single(row => row.Id == goal.Id).FundedText;
            try
            {
                await reminders.RefreshAsync();
                if (!scheduler.Pending.Any(item => GoalReminderLink.Parse(item.Link) is not null))
                    throw new InvalidOperationException("Original below-target contribution reminder was not prepared.");
                scheduler.AfterPublication = async () =>
                {
                    scheduler.AfterPublication = null;
                    await using var db = await factory.CreateDbContextAsync();
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Schedules SET ReminderEnabled=0 WHERE Id={plan.Id}");
                };
                await reminders.RefreshAsync();
                if (scheduler.Pending.Count != 0) throw new InvalidOperationException("The changed original Plan packet survived native publication.");
                await RestoreAsync();
                scheduler.AfterPublication = async () =>
                {
                    scheduler.AfterPublication = null;
                    await using var db = await factory.CreateDbContextAsync();
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Accounts SET OpeningBalance={goal.TargetAmount} WHERE Id={account.Id}");
                };
                await reminders.RefreshAsync();
                if (scheduler.Pending.Count != 0) throw new InvalidOperationException("The changed original financial packet survived native publication.");
                var changed = await stored();
                await reminders.RefreshAsync(); await vm.LoadAsync(); await Task.Delay(250);
                var reachedText = vm.Active.Single(row => row.Id == goal.Id).FundedText;
                if (reachedText == originalText || scheduler.Pending.Any(item => GoalReminderLink.Parse(item.Link) is not null) || await stored() != changed)
                    throw new InvalidOperationException("Fresh original goal progress did not suppress the reached-goal reminder without writes.");
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label, ProfileNativeReads);
                await CaptureAsync(app, folder, language + "-reminder-input-work-reached");
                await RestoreAsync(); await reminders.RefreshAsync(); await vm.LoadAsync(); await Task.Delay(250);
                if (vm.Active.Single(row => row.Id == goal.Id).FundedText != originalText
                    || !scheduler.Pending.Any(item => GoalReminderLink.Parse(item.Link) is not null) || await stored() != before)
                    throw new InvalidOperationException("Restored original financial rows failed the fresh read-only retry.");
                await CaptureAsync(app, folder, language + "-reminder-input-work-restored");
                await File.WriteAllTextAsync(Path.Combine(folder, language + "-reminder-input-work-proof.json"), JsonSerializer.Serialize(new
                { OriginalRuleRetired = true, OriginalFinancialPacketRetired = true, ActualGoalsPageProgressChanged = true,
                    ReachedGoalReminderSuppressed = true, Complete25TablesRestored = true, FreshReadOnlyRetry = true,
                    NoFinancialSaveOrDesktopNotification = true, ProductionRegistrationInactive = true, originalText, reachedText }));
            }
            finally { scheduler.AfterPublication = null; await RestoreAsync(); InvokeSnapshotBack(page); await Task.Delay(150); }
        }
    }
}
#endif
