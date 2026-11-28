using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// A hypothetical placement in time (ADR 0028): a dated statement marked with <c>placement</c> – what it rests on,
/// how sure it is and why. The placements here are test data put into a copy of the sample in memory.
/// </summary>
public sealed class HypotheticalPlacementReaderTests
{
    private const string Rostam = "character-rostam";
    private const string Work = "work-shahnameh";

    private static JsonObject Placement(string key = "placement-rostam-birth", string basis = "editorialHypothesis", string confidence = "veryLow") => JsonNode.Parse(
        $$"""
        {
          "key": "{{key}}",
          "label": "Hypothetical birth",
          "time": {
            "kind": "occurrenceWindow",
            "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 1500 },
            "latest": { "frame": "calendar", "era": "beforeCommonEra", "year": 1100 }
          },
          "placement": { "basis": "{{basis}}", "confidence": "{{confidence}}", "explanation": "Test data: about 1300 BCE." },
          "sourceIds": ["src-ganjoor-index"],
          "spoilerLevel": 0,
          "reviewStatus": "proposed"
        }
        """)!.AsObject();

    private static JsonObject Told() => JsonNode.Parse(
        """
        {
          "key": "rostam-told",
          "label": "First told",
          "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
          "sourceIds": ["src-ganjoor-index"],
          "spoilerLevel": 0,
          "reviewStatus": "proposed"
        }
        """)!.AsObject();

    private static Action<JsonObject> On(string entity, params JsonObject[] dated) =>
        sample => sample.Record("entities", entity)["dated"] = new JsonArray([.. dated]);

    [Fact]
    public void A_placement_is_read_with_its_basis_its_confidence_and_its_explanation()
    {
        var rostam = SampleFiles.Read(On(Rostam, Placement(), Placement("placement-rostam-birth-synchronism", "synchronism", "low"))).Find(new EntityId(Rostam))!;

        var (first, second) = (rostam.Placements[0], rostam.Placements[1]);
        Assert.Empty(rostam.Stated);
        Assert.Equal((PlacementBasis.EditorialHypothesis, PlacementConfidence.VeryLow), (first.Placement!.Basis, first.Placement.Confidence));
        Assert.Equal((PlacementBasis.Synchronism, PlacementConfidence.Low), (second.Placement!.Basis, second.Placement.Confidence));
        Assert.Equal("Test data: about 1300 BCE.", first.Placement.Explanation!.Resolve("en").Value);
        var window = Assert.IsType<OccurrenceWindow>(first.Time);
        Assert.Equal((Era.BeforeCommonEra, 1500), (((CalendarPosition)window.Earliest).Era, ((CalendarPosition)window.Earliest).Year));
    }

    [Fact]
    public void A_translation_gives_the_explanation_of_a_placement_by_its_key()
    {
        const string Persian = """
            {
              "formatVersion": "dastan-translation-0.1",
              "datasetId": "dastan-shahnameh-rostam-sohrab-pilot-v0.5",
              "language": "fa",
              "review": "draft",
              "dated": { "placement-rostam-birth": { "label": "تولد فرضی", "explanation": "دادهٔ آزمایشی." } }
            }
            """;

        var placement = SampleFiles.Read(On(Rostam, Placement()), Persian).Find(new EntityId(Rostam))!.Placements.Single();

        Assert.Equal("تولد فرضی", placement.Label.Resolve("fa").Value);
        Assert.Equal("دادهٔ آزمایشی.", placement.Placement!.Explanation!.Resolve("fa").Value);
    }

    [Fact]
    public void A_character_of_a_story_takes_a_dated_statement_only_as_a_placement()
    {
        Assert.Contains(
            "rostam-told: a character of a story has no date of history; a time given to character-rostam is a hypothetical placement and says so with placement.",
            SampleFiles.Problems(On(Rostam, Told())));

        // Any other entity may take both.
        var work = Told();
        work["key"] = "work-told";
        var read = SampleFiles.Read(On(Work, work, Placement("placement-work"))).Find(new EntityId(Work))!;
        Assert.Equal(["work-told"], read.Stated.Select(dated => dated.Key));
        Assert.Equal(["placement-work"], read.Placements.Select(dated => dated.Key));
    }

    [Theory]
    [InlineData("basis", "\"tradition\"", "placement-rostam-birth.placement: basis has the unknown value")]
    [InlineData("confidence", "\"high\"", "placement-rostam-birth.placement: confidence has the unknown value")]
    [InlineData("century", "\"14th\"", "placement-rostam-birth.placement: the field century is not known to this reader.")]
    public void What_is_wrong_with_a_placement_is_named_with_its_key(string field, string value, string problem)
    {
        var statement = Placement();
        statement["placement"]![field] = JsonNode.Parse(value);

        Assert.Contains(SampleFiles.Problems(On(Rostam, statement)), found => found.StartsWith(problem, StringComparison.Ordinal));
    }

    [Fact]
    public void A_placement_places_at_a_time_and_states_no_quantity_alone()
    {
        var timeless = Placement();
        timeless.Remove("time");
        var counted = Placement();
        counted.Remove("time");
        counted["quantity"] = JsonNode.Parse("""{ "value": 3, "unit": "years" }""");

        Assert.Contains("placement-rostam-birth: time is missing.", SampleFiles.Problems(On(Rostam, timeless)));
        Assert.Contains("placement-rostam-birth: a hypothetical placement states the time it places at; time is missing.", SampleFiles.Problems(On(Work, counted)));
    }

    [Fact]
    public void No_view_of_time_holds_a_placement()
    {
        var collection = SampleFiles.Read(On(Rostam, Placement()));
        var projection = Projection.Of(collection, ReadingLevel.Opening, Audience.LocalDemonstration)!;

        Assert.Single(projection.Find(new EntityId(Rostam))!.Placements);
        Assert.DoesNotContain(projection.Timeline().Subjects, subject => subject.Id.Value == Rostam);
        Assert.Null(projection.Years());
        Assert.Empty(projection.At(new YearOfEra(Era.BeforeCommonEra, 1300)).Stated);
    }

    [Fact]
    public void The_vocabulary_names_the_basis_and_the_confidence_of_a_placement()
    {
        var basis = ContentVocabulary.Collection.Single(choices => choices.Field == "entities[].dated[].placement.basis");
        var confidence = ContentVocabulary.Collection.Single(choices => choices.Field == "entities[].dated[].placement.confidence");

        Assert.Equal(["editorialHypothesis", "narrative", "synchronism"], basis.Values.Select(value => value.Name));
        Assert.All(basis.Values, value => Assert.False(string.IsNullOrEmpty(value.Note)));
        Assert.Equal(["low", "medium", "veryLow"], confidence.Values.Select(value => value.Name));
        Assert.Contains(ContentVocabulary.Translated, texts => texts.Group == "dated" && texts.Fields.Contains("explanation"));
    }
}
