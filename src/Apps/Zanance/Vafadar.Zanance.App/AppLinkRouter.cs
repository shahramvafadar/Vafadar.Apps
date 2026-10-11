using System.Globalization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

/// <summary>Opens only supported widget/reminder destinations after secure startup and unlocking, never posting money.</summary>
public sealed class AppLinkRouter(AppLockService appLock, IAppInteraction interaction, PlanStore plans)
{
    /// <summary>Prepares the first visible page after unlocking, then opens the startup link that waited for that page.</summary>
    public Task StartAsync(Func<Task<string?>> preparePage)
    {
        ArgumentNullException.ThrowIfNull(preparePage);
        return appLock.RunWhenUnlockedAsync(async () =>
        {
            // A startup PIN cover sits over a blank root, so navigation must wait for both access and the real Shell.
            var link = await preparePage();
            if (link is not null) { await OpenAsync(link); }
        });
    }

    /// <summary>Validates an external link before queuing its navigation behind the application access gate.</summary>
    public Task OpenAsync(string link)
    {
        if (PlanReminderLink.Parse(link) is { } reminder)
            return appLock.RunWhenUnlockedAsync(async () =>
            {
                // Resolve the active file after unlocking; a restored Guid is not proof of the same profile.
                var snapshot = await plans.GetWorkSnapshotAsync();
                if (snapshot.ReminderScope != reminder.Scope) return;
                var existing = reminder.Targets.Where(t => snapshot.Schedules.Any(s => s.Id == t.PlanId)).ToList();
                if (existing.Count == 0) return;
                await plans.ValidateWorkSnapshotAsync(snapshot);
                if (reminder.IsContract)
                    await interaction.NavigateAsync(AppRoutes.PlanDetailRoute, new Dictionary<string, object> { ["id"] = existing[0].PlanId });
                else if (reminder.Targets.Count == 1)
                    await interaction.NavigateAsync(AppRoutes.OccurrenceRoute,
                        new Dictionary<string, object> { ["plan"] = existing[0].PlanId, ["date"] = existing[0].OriginalDate!.Value });
                else await interaction.NavigateAsync("//plans");
            });

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

        return route is null ? Task.CompletedTask : appLock.RunWhenUnlockedAsync(() => interaction.NavigateAsync(route, parameters));
    }
}
