using System.Text.Json.Nodes;
using System.Xml.Linq;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// A quantity as a statement about an entity (step 12.7, DOM-005): a number with its unit, as it is stated,
/// with or without a time. Test data.
/// </summary>
public sealed class QuantityTests
{
    private const string Work = "work-shahnameh";

    private static JsonObject Statement(string key, string? quantity, string? time = null)
    {
        var statement = new JsonObject
        {
            ["key"] = key, ["label"] = "Test data " + key, ["sourceIds"] = new JsonArray("src-ganjoor-index"),
            ["spoilerLevel"] = 0, ["reviewStatus"] = "proposed",
        };
        if (quantity is not null)
        {
            statement["quantity"] = JsonNode.Parse(quantity);
        }

        if (time is not null)
        {
            statement["time"] = JsonNode.Parse(time);
        }

        return statement;
    }

    private static Action<JsonObject> With(params JsonObject[] statements) =>
        sample => sample.Record("entities", Work)["dated"] = new JsonArray([.. statements]);

    private const string Year = """{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } }""";

    [Fact]
    public void A_quantity_is_read_as_it_is_stated_with_or_without_a_time()
    {
        var work = SampleFiles.Read(With(
            Statement("work-length", """{ "value": 50000, "unit": "verses", "approximate": true }"""),
            Statement("work-price", """{ "value": 60000.5, "unit": "money", "currency": "EUR" }""", Year),
            Statement("work-finished", null, Year))).Find(new EntityId(Work))!;

        Assert.Equal(new Quantity(50000m, "verses", IsApproximate: true), work.Dated[0].Quantity);
        Assert.Null(work.Dated[0].Time);
        Assert.Equal(new Quantity(60000.5m, "money", Currency: "EUR"), work.Dated[1].Quantity);
        Assert.IsType<Occurrence>(work.Dated[1].Time);
        Assert.Null(work.Dated[2].Quantity);
    }

    [Fact]
    public void Only_what_has_a_time_is_on_the_timeline()
    {
        var catalogue = SampleFiles.Read(With(Statement("work-length", """{ "value": 50000, "unit": "verses" }""")));
        var projection = Projection.Of(catalogue, ReadingLevel.Opening, Audience.LocalDemonstration)!;

        Assert.DoesNotContain(projection.Timeline().Subjects, subject => subject.Id.Value == Work);
        Assert.Single(projection.Find(new EntityId(Work))!.Dated);
    }

    [Fact]
    public void A_statement_says_when_or_how_much_and_a_quantity_has_a_unit_of_the_list()
    {
        IReadOnlyList<string> Of(string? quantity, string? time = null) => SampleFiles.Problems(With(Statement("work-x", quantity, time)));

        Assert.Contains("work-x: time is missing.", Of(null));
        Assert.Contains("work-x.quantity: unit has the unknown value \"couplets\".", Of("""{ "value": 1, "unit": "couplets" }"""));
        Assert.Contains("work-x.quantity: an amount of money names its currency with three capital letters, such as EUR.", Of("""{ "value": 1, "unit": "money" }"""));
        Assert.Contains("work-x.quantity: an amount of money names its currency with three capital letters, such as EUR.", Of("""{ "value": 1, "unit": "money", "currency": "euro" }"""));
        Assert.Contains("work-x.quantity: currency belongs to an amount of money only.", Of("""{ "value": 1, "unit": "verses", "currency": "EUR" }"""));
        Assert.Contains(Of("""{ "unit": "verses" }"""), problem => problem.Contains("value", StringComparison.Ordinal));
        Assert.Contains(Of("""{ "value": "many", "unit": "verses" }"""), problem => problem.Contains("value", StringComparison.Ordinal));
    }

    [Fact]
    public void A_quantity_of_a_later_stage_is_not_shown_before_it()
    {
        var late = Statement("work-secret", """{ "value": 3, "unit": "count" }""");
        late["spoilerLevel"] = 3;
        var catalogue = SampleFiles.Read(With(late));

        Assert.Empty(Projection.Of(catalogue, ReadingLevel.Opening, Audience.LocalDemonstration)!.Find(new EntityId(Work))!.Dated);
        Assert.Single(Projection.Of(catalogue, new ReadingLevel(3), Audience.LocalDemonstration)!.Find(new EntityId(Work))!.Dated);
    }

    [Theory]
    [InlineData("Ui.resx")]
    [InlineData("Ui.fa.resx")]
    public void Every_unit_has_its_words_in_the_language_of_a_page_and_its_meaning_for_writers(string file)
    {
        var texts = XDocument.Load(Path.Combine(SampleFiles.App, "Vafadar.Dastan.Web", "Resources", file)).Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string)data.Element("value")!, StringComparer.Ordinal);
        var listed = ContentVocabulary.Collection.Single(choices => choices.Field == "entities[].dated[].quantity.unit").Values;

        Assert.All(QuantityUnits.All, unit => Assert.Contains("{0}", texts["Unit_" + unit.Key], StringComparison.Ordinal));
        Assert.Contains("{1}", texts["Unit_money"], StringComparison.Ordinal);
        Assert.Equal(QuantityUnits.All.Length, listed.Length);
        Assert.All(listed, unit => Assert.False(string.IsNullOrWhiteSpace(unit.Note)));
        Assert.Equal(QuantityUnits.All.Length, QuantityUnits.All.Select(unit => unit.Key).Distinct(StringComparer.Ordinal).Count());
    }
}
