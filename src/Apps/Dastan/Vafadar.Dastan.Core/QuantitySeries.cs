using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>One number of a series: a statement of a quantity with the place of its time among the years.</summary>
/// <param name="Statement">The statement.</param>
/// <param name="At">The middle of its time on the axis of years.</param>
/// <param name="Year">The year that middle lies in.</param>
public sealed record SeriesPoint(DatedStatement Statement, decimal At, YearOfEra Year)
{
    /// <summary>Gets the quantity; a point of a series always has one.</summary>
    public Quantity Quantity => Statement.Quantity!;
}

/// <summary>
/// The same quantity of one entity stated for several times: the people of a city in one year and in another.
/// Statements belong to one series when they are called the same and counted in the same unit.
/// </summary>
/// <param name="Entity">The entity.</param>
/// <param name="Label">What the statements are called.</param>
/// <param name="Unit">The unit they are counted in.</param>
/// <param name="Currency">The currency, for amounts of money.</param>
/// <param name="Points">The numbers, in the order of their times; at least two.</param>
public sealed record QuantitySeries(Entity Entity, LocalizedText Label, string Unit, string? Currency, ImmutableArray<SeriesPoint> Points)
{
    /// <summary>
    /// Returns the series among the statements of an entity: the statements of a quantity with a time that can
    /// be placed among the years, grouped by what they are called in <paramref name="language"/> and by their
    /// unit. One number alone is no series.
    /// </summary>
    public static ImmutableArray<QuantitySeries> Of(Entity entity, string language)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return
        [
            .. entity.Stated
                .Where(statement => statement.Quantity is not null)
                .Select(statement => (Statement: statement, Reach: YearCover.Reach(statement.Time)))
                .Where(placed => placed.Reach is not null)
                .GroupBy(placed => (Name: placed.Statement.Label.Resolve(language).Value, placed.Statement.Quantity!.Unit, placed.Statement.Quantity.Currency))
                .Where(group => group.Count() > 1)
                .Select(group => new QuantitySeries(
                    entity,
                    group.First().Statement.Label,
                    group.Key.Unit,
                    group.Key.Currency,
                    [.. group.Select(placed => new SeriesPoint(placed.Statement, placed.Reach!.Value.Middle, YearOfEra.At(placed.Reach.Value.Middle))).OrderBy(point => point.At)])),
        ];
    }
}

/// <summary>A number of a series on its drawing.</summary>
/// <param name="Point">The number.</param>
/// <param name="X">Its distance from the left side of the drawing.</param>
/// <param name="Y">Its distance from the upper side of the drawing.</param>
public sealed record SeriesMark(SeriesPoint Point, double X, double Y);

/// <summary>A line of a drawing that stands for a value.</summary>
/// <param name="Value">The value.</param>
/// <param name="At">Its distance from the upper side of the drawing.</param>
public sealed record ValueLine(decimal Value, double At);

/// <summary>A line of a drawing that stands for the beginning of a year.</summary>
/// <param name="Year">The year.</param>
/// <param name="At">Its distance from the left side of the drawing.</param>
public sealed record YearLine(YearOfEra Year, double At);

/// <summary>
/// The drawing of a series: time from left to right, the value upwards. The values are drawn from zero – or
/// from the least value, when one is below zero – so that a difference looks as large as it is.
/// </summary>
/// <param name="Width">The width of the drawing.</param>
/// <param name="Height">The height of the drawing.</param>
/// <param name="Marks">The numbers, in the order of their times.</param>
/// <param name="Values">The lines of values, from the bottom.</param>
/// <param name="Years">The lines of years, from the left.</param>
public sealed record SeriesChart(double Width, double Height, ImmutableArray<SeriesMark> Marks, ImmutableArray<ValueLine> Values, ImmutableArray<YearLine> Years)
{
    /// <summary>The width of a drawing.</summary>
    public const double Across = 30;

    /// <summary>The height of a drawing.</summary>
    public const double Up = 14;

    // The steps a scale can have, each times a power of ten.
    private static readonly decimal[] Steps = [1, 2, 5];

    /// <summary>Returns the drawing of a series.</summary>
    public static SeriesChart Of(QuantitySeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
        var values = series.Points.Select(point => point.Quantity.Value).ToList();
        var (bottom, top, step) = Scale(Math.Min(0, values.Min()), Math.Max(0, values.Max()));

        // Time: from the first to the last number, with a twentieth of room at both sides; one year at least.
        var (first, last) = (series.Points[0].At, series.Points[^1].At);
        var room = Math.Max(0.5m, (last - first) / 20);
        var (from, to) = (first - room, last + room);
        var yearStep = Step((to - from) / 6, whole: true);

        double X(decimal at) => (double)((at - from) / (to - from)) * Across;
        double Y(decimal value) => (double)((top - value) / (top - bottom)) * Up;

        return new(
            Across,
            Up,
            [.. series.Points.Select(point => new SeriesMark(point, X(point.At), Y(point.Quantity.Value)))],
            [.. Lines(bottom, top, step).Select(value => new ValueLine(value, Y(value)))],
            [.. Lines(from, to, yearStep).Select(place => new YearLine(YearOfEra.At(place), X(place)))]);
    }

    // A scale that holds both ends in a few whole steps.
    private static (decimal Bottom, decimal Top, decimal Step) Scale(decimal least, decimal most)
    {
        if (least == most)
        {
            most = least + 1;
        }

        var step = Step((most - least) / 4, whole: false);
        return (Math.Floor(least / step) * step, Math.Ceiling(most / step) * step, step);
    }

    // The least step of the kind 1, 2, 5 times a power of ten that is not less than what is asked for.
    private static decimal Step(decimal atLeast, bool whole)
    {
        var power = 1m;
        while (power * 5 < atLeast)
        {
            power *= 10;
        }

        while (!whole && power > atLeast)
        {
            power /= 10;
        }

        // Ten times the power stands after five times: between five tenths and one, one is the step.
        return Steps.Append(10).Select(step => step * power).First(step => step >= atLeast);
    }

    private static IEnumerable<decimal> Lines(decimal from, decimal to, decimal step)
    {
        for (var line = Math.Ceiling(from / step) * step; line <= to; line += step)
        {
            yield return line;
        }
    }
}
