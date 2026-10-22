#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Visits one original complete goal without invoking financial, edit or retry commands.</summary>
    internal async Task ReadGoalNavigationAsync(string context, bool quantity, Func<Page, Task> back,
        Action<string, Task?> stage, CancellationToken ct)
    {
        var goals = SecondaryGoals; var holdings = SecondaryHoldings;
        var saved = (await goals.GetGoalsAsync(ct)).Single(goal => goal.Type == (quantity ? GoalType.HoldingQuantity : GoalType.Earmark));
        var dates = new DateFormatter(Localization);
        var presenter = new GoalPresenter(Translator, dates, Localization, holdings, new HoldingText(Translator, Localization, dates));
        var model = new GoalDetailViewModel(Store, goals, Plans, holdings, presenter, Translator, dates, Localization, Time, new MoneyInteraction());
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = saved.Id });
        var page = new GoalDetailPage(model); string? route = null;
        try
        {
            var showing = ShowAsync(page); stage(context + "-show", showing); route = await showing.WaitAsync(ct);
            var publication = page.PendingPublication; stage(context + "-publication", publication); await publication.WaitAsync(ct);
            await WaitAsync(() => page.IsLoaded && model.CanInteract, ct);
            var edit = model.EditCommand;
            var leaving = back(page); stage(context + "-native-back", leaving); await leaving.WaitAsync(ct);
            await WaitAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page), ct);
            if (model.CanInteract || edit.CanExecute(null)) throw new IOException("Original departed detail remained actionable.");
            var home = LeaveAsync(route); stage(context + "-home", home); await home.WaitAsync(ct); route = null;
        }
        finally
        {
            model.Retire();
            if (route is not null) { var leave = LeaveAsync(route); stage(context + "-cleanup-home", leave); await leave.WaitAsync(ct); }
        }
    }
}
#endif
