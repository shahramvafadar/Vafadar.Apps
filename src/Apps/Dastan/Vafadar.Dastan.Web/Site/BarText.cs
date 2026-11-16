namespace Vafadar.Dastan.Web.Site;

/// <summary>The words a chart writes on a bar and says about it; the page chooses them, the chart only draws.</summary>
/// <param name="Start">The year the period begins in, or <see langword="null"/> when no single year is stated for it.</param>
/// <param name="End">The year it ends in, or <see langword="null"/> when no single year is stated for it.</param>
/// <param name="Title">The period in full: its name and its time as the table states it.</param>
public sealed record BarText(string? Start, string? End, string Title);
