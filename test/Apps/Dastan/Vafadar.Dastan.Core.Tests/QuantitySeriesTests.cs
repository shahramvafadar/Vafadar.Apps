using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The same quantity stated for several times (step 14.3): which statements are one series, and where its
/// numbers lie on a drawing. Test data.
/// </summary>
public sealed class QuantitySeriesTests
{
    private static DatedStatement Stated(string key, string label, decimal value, int? year, string unit = "people", string? currency = null, bool about = false) => new()
    {
        Key = key,
        Label = SmallStory.Text(label),
        Quantity = new Quantity(value, unit, about, currency),
        Time = year is { } stated ? new Occurrence(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, stated)) : null,
        Sources = [new SourceId("passage-0")],
        Review = ReviewStatus.Proposed,
        Level = ReadingLevel.Opening,
    };

    private static Entity City(params DatedStatement[] dated) => new()
    {
        Id = new EntityId("city"), Type = EntityTypes.Place, Label = SmallStory.Text("A city"), Summary = SmallStory.Text("Test data."),
        Sources = [new SourceId("passage-0")], Level = ReadingLevel.Opening, Dated = [.. dated],
    };

    [Fact]
    public void Statements_called_the_same_in_the_same_unit_are_one_series_in_the_order_of_their_times()
    {
        var city = City(
            Stated("people-1100", "People", 2000, 1100),
            Stated("people-1000", "People", 1000, 1000, about: true),
            Stated("area-1000", "Area", 12.5m, 1000, "square_kilometres"),
            Stated("houses-1000", "Houses", 300, 1000, "pieces"),
            Stated("houses-1100", "Houses", 500, 1100, "pieces"));

        var series = QuantitySeries.Of(city, "en");

        Assert.Equal(["people", "pieces"], series.Select(one => one.Unit));
        Assert.Equal(["people-1000", "people-1100"], series[0].Points.Select(point => point.Statement.Key));
        Assert.Equal([1000, 1100], series[0].Points.Select(point => point.Year.Number));
        Assert.Equal(1000.5m, series[0].Points[0].At);
    }

    [Fact]
    public void One_number_alone_and_numbers_without_a_time_are_no_series()
    {
        Assert.Empty(QuantitySeries.Of(City(Stated("people-1000", "People", 1000, 1000)), "en"));
        Assert.Empty(QuantitySeries.Of(City(Stated("verses", "Verses", 50000, null), Stated("verses-too", "Verses", 60000, null)), "en"));
        Assert.Empty(QuantitySeries.Of(City(), "en"));
    }

    [Fact]
    public void Another_unit_another_currency_and_another_name_are_other_series()
    {
        var city = City(
            Stated("tax-1000", "Tax", 5000, 1000, "money", "EUR"),
            Stated("tax-1100", "Tax", 7000, 1100, "money", "USD"),
            Stated("people-1000", "People", 1000, 1000),
            Stated("guests-1100", "Guests", 2000, 1100));

        Assert.Empty(QuantitySeries.Of(city, "en"));
    }

    [Fact]
    public void The_drawing_runs_from_zero_with_time_to_the_right_and_more_upwards()
    {
        var series = QuantitySeries.Of(City(Stated("a", "People", 40000, 1000), Stated("b", "People", 120000, 1100), Stated("c", "People", 90000, 1250)), "en").Single();

        var chart = SeriesChart.Of(series);

        Assert.Equal([0m, 50000m, 100000m, 150000m], chart.Values.Select(line => line.Value));
        Assert.Equal(SeriesChart.Up, chart.Values[0].At, 6);
        Assert.Equal(0, chart.Values[^1].At, 6);
        Assert.True(chart.Marks[0].X < chart.Marks[1].X && chart.Marks[1].X < chart.Marks[2].X);
        Assert.True(chart.Marks[1].Y < chart.Marks[2].Y && chart.Marks[2].Y < chart.Marks[0].Y);
        Assert.All(chart.Marks, mark => Assert.True(mark.X > 0 && mark.X < chart.Width && mark.Y >= 0 && mark.Y <= chart.Height));

        // The years of the scale are whole steps inside the drawing.
        Assert.Equal([1000, 1050, 1100, 1150, 1200, 1250], chart.Years.Select(line => line.Year.Number));
        Assert.All(chart.Years, line => Assert.InRange(line.At, 0, chart.Width));
    }

    [Fact]
    public void Small_numbers_numbers_below_zero_and_equal_numbers_have_a_scale()
    {
        var small = SeriesChart.Of(QuantitySeries.Of(City(Stated("a", "Share", 0.2m, 1000, "percent"), Stated("b", "Share", 0.7m, 1001, "percent")), "en").Single());
        var below = SeriesChart.Of(QuantitySeries.Of(City(Stated("a", "Warmth", -12, 1000, "degrees_celsius"), Stated("b", "Warmth", 8, 1001, "degrees_celsius")), "en").Single());
        var equal = SeriesChart.Of(QuantitySeries.Of(City(Stated("a", "People", 0, 1000), Stated("b", "People", 0, 1001)), "en").Single());

        Assert.Equal([0m, 0.2m, 0.4m, 0.6m, 0.8m], small.Values.Select(line => line.Value));
        Assert.Equal([-15m, -10m, -5m, 0m, 5m, 10m], below.Values.Select(line => line.Value));
        Assert.Contains(equal.Values, line => line.Value == 0);
        Assert.All(equal.Marks, mark => Assert.InRange(mark.Y, 0, equal.Height));
        Assert.Equal([1000, 1001, 1002], small.Years.Select(line => line.Year.Number));
    }
}
