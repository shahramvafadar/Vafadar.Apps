using Vafadar.Localization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Complete original pinned rows and financial attention, with new-work availability kept separate.</summary>
/// <param name="Pinned">The existing two pinned original active goals, never a guessed selected subset.</param>
/// <param name="Attention">The original overdue warning when no goal is pinned.</param>
public sealed record GoalHomeView(IReadOnlyList<GoalRow> Pinned, string? Attention);

/// <summary>Actual Home goal projection shared with application flow verification; never posts financial data.</summary>
public static class GoalHomeProjection
{
    /// <summary>Reads and validates the actual file before publishing original Home rows and financial warnings.</summary>
    public static async Task<GoalHomeView> ReadAsync(GoalStore goals, GoalPresenter presenter, Translator translator,
        IReadOnlyCollection<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly today)
    {
        var snapshot = await goals.GetWorkSnapshotAsync();
        var progress = await presenter.EvaluateAsync(snapshot, accounts, entries, today);
        await goals.ValidateWorkSnapshotAsync(snapshot);
        return Project(snapshot, presenter, translator, progress);
    }

    /// <summary>Projects the original complete Home packet; its caller validates the packet before uncovering actions.</summary>
    public static GoalHomeView Project(HomeReadSnapshot snapshot, GoalPresenter presenter, Translator translator, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return Project(snapshot.Goals, presenter, translator, presenter.Evaluate(snapshot, today));
    }

    /// <summary>Computes original financial progress off the window thread, then formats the same rows on its caller.</summary>
    public static async Task<GoalHomeView> ProjectAsync(HomeReadSnapshot snapshot, GoalPresenter presenter,
        Translator translator, DateOnly today, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var progress = await presenter.EvaluateHomeAsync(snapshot, today, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return Project(snapshot.Goals, presenter, translator, progress);
    }

    private static GoalHomeView Project(GoalWorkSnapshot snapshot, GoalPresenter presenter, Translator translator,
        IReadOnlyList<GoalProgress> progress)
    {
        var pinned = progress.Where(p => p.Goal.State == GoalState.Active && p.Goal.HomePin is not null)
            .OrderBy(p => p.Goal.HomePin).Take(2).Select(p => presenter.Row(p.Goal, p, snapshot.Work)).ToArray();
        var overdue = progress.FirstOrDefault(p => p.Goal.State == GoalState.Active && p.IsOverdue);
        return new(pinned, pinned.Length == 0 && overdue is not null
            ? translator.Format("Home_GoalOverdue", overdue.Goal.Name) : null);
    }
}
