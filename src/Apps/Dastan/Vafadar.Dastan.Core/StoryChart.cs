using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>
/// The part of the lanes of a story that a chart shows: a run of steps and the first lanes, cut out of what the
/// visitor may see. It holds nothing the <see cref="LanesView"/> does not hold – a chart can be closed at its
/// sides, never opened beyond the reading position.
/// </summary>
/// <param name="Steps">The steps shown, in the order of the telling.</param>
/// <param name="Lanes">The lanes shown, each with one cell per step shown.</param>
/// <param name="First">The place of the first step shown among the steps of the view, counted from 1; 0 when there is none.</param>
/// <param name="Last">The place of the last step shown.</param>
/// <param name="StepCount">The number of steps of the view.</param>
/// <param name="LaneCount">The number of lanes of the view.</param>
public sealed record StoryChart(
    ImmutableArray<StoryStep> Steps,
    ImmutableArray<Lane> Lanes,
    int First,
    int Last,
    int StepCount,
    int LaneCount);

/// <summary>Cuts the part a chart shows out of the lanes of a story.</summary>
public static class StoryCharts
{
    /// <summary>
    /// Returns the steps from one place to another and the first lanes of a view. What is asked for beyond the
    /// view is not there: the answer ends where the view ends. A lane stays in the chart when its name has no part
    /// in the steps shown – the rows do not change while the steps are opened and closed.
    /// </summary>
    /// <param name="view">The lanes the visitor may see.</param>
    /// <param name="first">The place of the first step to show, counted from 1; all steps from the beginning when not given.</param>
    /// <param name="last">The place of the last step to show; to the end of the view when not given.</param>
    /// <param name="lanes">The number of lanes to show, counted from the first; all when not given.</param>
    public static StoryChart Of(LanesView view, int? first = null, int? last = null, int? lanes = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (view.Steps.IsEmpty)
        {
            return new StoryChart([], [], 0, 0, 0, view.Lanes.Length);
        }

        var from = Math.Clamp(first ?? 1, 1, view.Steps.Length);
        var to = Math.Clamp(last ?? view.Steps.Length, from, view.Steps.Length);
        var count = Math.Clamp(lanes ?? view.Lanes.Length, Math.Min(1, view.Lanes.Length), view.Lanes.Length);
        return new StoryChart(
            view.Steps[(from - 1)..to],
            [.. view.Lanes.Take(count).Select(lane => lane with { Cells = lane.Cells[(from - 1)..to] })],
            from,
            to,
            view.Steps.Length,
            view.Lanes.Length);
    }
}
