using System.Globalization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Opens only supported widget/reminder destinations after secure startup and unlocking, never posting money.</summary>
public sealed class AppLinkRouter(AppLockService appLock, IAppInteraction interaction, PlanStore plans, GoalStore goals, ZananceStore store)
{
    /// <summary>Prepares the first visible page after unlocking, then admits the links that waited for it.</summary>
    public Task StartAsync(Func<Task<IReadOnlyList<string>>> preparePage)
    {
        ArgumentNullException.ThrowIfNull(preparePage);
        return appLock.RunWhenUnlockedAsync(async () =>
        {
            // A startup PIN cover sits over a blank root, so navigation must wait for both access and the real Shell.
            await OpenBatchAsync(await preparePage());
        });
    }

    /// <summary>Validates an external link before queuing its navigation behind the application access gate.</summary>
    public Task OpenAsync(string link) => Resolve(link) is { } action
        ? appLock.RunWhenUnlockedAsync(action) : Task.CompletedTask;

    /// <summary>Queues all recognized original startup links together so later input cannot overtake them.</summary>
    internal Task OpenBatchAsync(IReadOnlyList<string> links)
    {
        var actions = links.Select(Resolve).OfType<Func<Task>>().ToArray();
        return actions.Length == 0 ? Task.CompletedTask
            : appLock.RunWhenUnlockedAsync(actions[0], new ArraySegment<Func<Task>>(actions, 1, actions.Length - 1));
    }

    private Func<Task>? Resolve(string link)
    {
        if (BudgetReminderLink.Parse(link) is { } budgetReminder)
        {
            return ResolveAsync;
            async Task ResolveAsync()
            {
                // An alert opens its original period without selecting a new Home currency or changing account data.
                if (await store.ResolveBudgetReminderAsync(budgetReminder) is null) return;
                if (appLock.RetainCurrentDestinationIfLocked(ResolveAsync)) return;
                await interaction.NavigateAsync(AppRoutes.BudgetRoute,
                    new Dictionary<string, object> { ["budgetReminder"] = budgetReminder });
            }
        }

        if (GoalReminderLink.Parse(link) is { } goalReminder)
        {
            return ResolveAsync;
            async Task ResolveAsync()
            {
                // Resolve after unlocking; a restored goal Guid alone cannot identify the original notification file.
                var snapshot = await goals.GetWorkSnapshotAsync();
                if (snapshot.ReminderScope != goalReminder.Scope) return;
                var existing = goalReminder.Targets.Where(target => snapshot.Goals.Any(goal => goal.Id == target.GoalId)).ToList();
                if (existing.Count == 0) return;
                await goals.ValidateWorkSnapshotAsync(snapshot);
                if (appLock.RetainCurrentDestinationIfLocked(ResolveAsync)) return;
                if (goalReminder.Targets.Count == 1)
                    await interaction.NavigateAsync(AppRoutes.GoalDetailRoute, new Dictionary<string, object> { ["id"] = existing[0].GoalId });
                else await interaction.NavigateAsync(AppRoutes.GoalsRoute);
            }
        }

        if (PlanReminderLink.Parse(link) is { } reminder)
        {
            return ResolveAsync;
            async Task ResolveAsync()
            {
                // Resolve the active file after unlocking; a restored Guid is not proof of the same profile.
                var snapshot = await plans.GetWorkSnapshotAsync();
                if (snapshot.ReminderScope != reminder.Scope) return;
                var existing = reminder.Targets.Where(t => snapshot.Schedules.Any(s => s.Id == t.PlanId)).ToList();
                if (existing.Count == 0) return;
                await plans.ValidateWorkSnapshotAsync(snapshot);
                if (appLock.RetainCurrentDestinationIfLocked(ResolveAsync)) return;
                if (reminder.IsContract)
                    await interaction.NavigateAsync(AppRoutes.PlanDetailRoute, new Dictionary<string, object> { ["id"] = existing[0].PlanId });
                else if (reminder.Targets.Count == 1)
                    await interaction.NavigateAsync(AppRoutes.OccurrenceRoute,
                        new Dictionary<string, object> { ["plan"] = existing[0].PlanId, ["date"] = existing[0].OriginalDate!.Value });
                else await interaction.NavigateAsync("//plans");
            }
        }

        var parts = link.Split('|');
        string? route = null;
        Dictionary<string, object>? parameters = null;
        if (parts is ["occurrence", var plan, var date] && Guid.TryParse(plan, out var planId) && planId != Guid.Empty
            && DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var original))
        {
            route = AppRoutes.OccurrenceRoute; parameters = new() { ["plan"] = planId, ["date"] = original };
        }
        else if (parts is ["plan", var id] && Guid.TryParse(id, out var scheduleId) && scheduleId != Guid.Empty)
        {
            route = AppRoutes.PlanDetailRoute; parameters = new() { ["id"] = scheduleId };
        }
        else if (parts is ["goal", var goal] && Guid.TryParse(goal, out var goalId) && goalId != Guid.Empty)
        {
            route = AppRoutes.GoalDetailRoute; parameters = new() { ["id"] = goalId };
        }
        else if (parts is ["entry", "Expense" or "Income" or "Transfer"])
        {
            route = AppRoutes.EntryEditorRoute; parameters = new() { ["kind"] = parts[1] };
        }
        else if (parts is [var destination])
        {
            route = destination switch
            {
                "plans" => "//plans",
                "goals" => AppRoutes.GoalsRoute,
                "budget" => AppRoutes.BudgetRoute,
                // A delayed tap opens the current review; it never completes an older period.
                "review" => AppRoutes.ReviewRoute,
                _ => null,
            };
        }

        return route is null ? null : () => interaction.NavigateAsync(route, parameters);
    }
}
