using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Resources;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// A timeline for the browser (ADR 0021): the views of <see cref="Timeline"/> with every word the drawing
/// shows already written by the server, in the language of the page. It is written into the page that shows
/// the same statements as a table – as data, which no browser runs – so the drawing and the table cannot say
/// different things, and nothing is asked for a second time.
/// </summary>
/// <param name="Views">The views, one for each axis something lies on.</param>
/// <param name="Words">The words of the axes.</param>
public sealed record TimelineData(IReadOnlyList<TimelineViewData> Views, TimelineWords Words)
{
    private static readonly JsonSerializerOptions Written = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Returns the data as the text a page holds.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, Written);

    /// <summary>Returns the name of a view of an order: of the telling or inside its world, and of which telling when the page says.</summary>
    /// <param name="view">The view.</param>
    /// <param name="text">The texts of the site.</param>
    public static string OrderTitle(TimeView view, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);
        return text[view.InWorld ? "Timeline_Axis_OrderInWorld" : "Timeline_Axis_Order"];
    }

    /// <summary>Returns the name of a view: the axis it counts on.</summary>
    /// <param name="view">The view.</param>
    /// <param name="visitor">The visitor.</param>
    /// <param name="text">The texts of the site.</param>
    public static string Title(TimeView view, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);
        return view.Axis switch
        {
            TimeAxis.Depth => string.Format(CultureInfo.CurrentCulture, text["Timeline_Axis_Depth"], TimeText.Year(Vafadar.Temporal.Era.CommonEra, view.Present!.Value, visitor, text)),
            TimeAxis.Order => OrderTitle(view, text),
            _ => text["Timeline_Axis_Calendar"],
        };
    }

    /// <summary>Makes the data of a timeline for a visitor.</summary>
    /// <param name="timeline">The timeline.</param>
    /// <param name="visitor">The visitor.</param>
    /// <param name="text">The texts of the site.</param>
    /// <param name="here">The address of the page, to which the rows of its table belong.</param>
    public static TimelineData Of(Timeline timeline, Visitor visitor, IStringLocalizer<Ui> text, string here)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        string Year(string era) => string.Format(CultureInfo.CurrentCulture, text["Year_Short"], "{0}", text[era]);
        string Ago(string unit) => string.Format(CultureInfo.CurrentCulture, text["Age_Ago"], text[unit].Value);
        return new(
            [.. timeline.Views.Select(view => new TimelineViewData(
                view.Axis,
                view.Axis switch
                {
                    TimeAxis.Depth => string.Format(CultureInfo.CurrentCulture, text["Timeline_Axis_Depth"], TimeText.Year(Vafadar.Temporal.Era.CommonEra, view.Present!.Value, visitor, text)),
                    TimeAxis.Order => TimelineData.OrderTitle(view, text),
                    _ => text["Timeline_Axis_Calendar"],
                },
                view.From,
                view.To,
                [.. view.Lanes.Select(lane => new TimelineLaneData(
                    lane.Key,
                    visitor.Text(lane.Label).Value,
                    [.. lane.Marks.Select(mark => new TimelineMarkData(
                        mark.Key,
                        visitor.Text(mark.Label).Value,
                        mark.Shape,
                        mark.From,
                        mark.To,
                        TimeText.Of(mark.Time, visitor, text),
                        $"{here}#item-{mark.Key}")
                    {
                        Start = mark.Shape == TimeShape.Lasting ? mark.Start : null,
                        End = mark.Shape == TimeShape.Lasting ? mark.End : null,
                        StartedBy = mark.StartedBy,
                        EndsFrom = mark.EndsFrom,
                        Loose = mark.IsLoose ? true : null,
                        Disputed = mark.IsDisputed ? true : null,

                        // At a step of a telling: what the step says about the name being there, in the words
                        // of the table below; and whether the name is only spoken of, which is drawn hollow.
                        Presence = mark.Presence is { } presence ? text[$"Presence_{presence}"].Value : null,
                        SpokenOf = mark.Presence is not (null or Presence.Acts or Presence.Involved or Presence.Setting or Presence.Object) ? true : null,
                    })]))]))],
            new(
                Year("Era_Short_CommonEra"),
                Year("Era_Short_BeforeCommonEra"),
                Ago("Age_Years"),
                Ago("Age_ThousandYears"),
                Ago("Age_MillionYears"),
                Ago("Age_BillionYears"),
                text["Timeline_Step"],
                visitor.Language.Tag == "fa"));
    }
}

/// <summary>One view: an axis and what lies on it.</summary>
/// <param name="Axis">The axis.</param>
/// <param name="Title">The name of the view.</param>
/// <param name="From">Where the view begins: a place of the axis of years, a number of years before the present, or a step.</param>
/// <param name="To">Where it ends.</param>
/// <param name="Lanes">The lanes.</param>
public sealed record TimelineViewData(TimeAxis Axis, string Title, decimal From, decimal To, IReadOnlyList<TimelineLaneData> Lanes);

/// <summary>A lane of a view.</summary>
/// <param name="Key">The identifier.</param>
/// <param name="Label">The subject.</param>
/// <param name="Marks">Its marks.</param>
public sealed record TimelineLaneData(string Key, string Label, IReadOnlyList<TimelineMarkData> Marks);

/// <summary>A mark: a moment, a window or something that lasted.</summary>
/// <param name="Key">The identifier of the statement.</param>
/// <param name="Label">What it is.</param>
/// <param name="Shape">What it says about its time.</param>
/// <param name="From">Where it begins on the axis.</param>
/// <param name="To">Where it ends.</param>
/// <param name="When">Its time in words, as the table of the page writes it.</param>
/// <param name="Address">The row of the table that says the same.</param>
public sealed record TimelineMarkData(string Key, string Label, TimeShape Shape, decimal From, decimal To, string When, string Address)
{
    /// <summary>Gets what is known of the beginning of something that lasted.</summary>
    public TimeEdge? Start { get; init; }

    /// <summary>Gets what is known of its end.</summary>
    public TimeEdge? End { get; init; }

    /// <summary>Gets where a beginning between two times is surely over.</summary>
    public decimal? StartedBy { get; init; }

    /// <summary>Gets where an end between two times may begin.</summary>
    public decimal? EndsFrom { get; init; }

    /// <summary>Gets whether the time is less than exact.</summary>
    public bool? Loose { get; init; }

    /// <summary>Gets whether the statement is disputed.</summary>
    public bool? Disputed { get; init; }

    /// <summary>Gets what a step of a telling says about the name being there, in words.</summary>
    public string? Presence { get; init; }

    /// <summary>Gets whether the name is only spoken of at the step – mentioned, reported, mourned – and not there.</summary>
    public bool? SpokenOf { get; init; }
}

/// <summary>The words of the axes; "{0}" stands for the number.</summary>
/// <param name="CommonEra">A year of the common era.</param>
/// <param name="BeforeCommonEra">A year before it.</param>
/// <param name="YearsAgo">So many years ago.</param>
/// <param name="ThousandYearsAgo">So many thousand years ago.</param>
/// <param name="MillionYearsAgo">So many million years ago.</param>
/// <param name="BillionYearsAgo">So many billion years ago.</param>
/// <param name="Step">A step of a telling.</param>
/// <param name="PersianDigits">Whether numbers are written in the digits of Persian.</param>
public sealed record TimelineWords(
    string CommonEra, string BeforeCommonEra, string YearsAgo, string ThousandYearsAgo, string MillionYearsAgo, string BillionYearsAgo, string Step, bool PersianDigits);
