using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>Translations of a sample: the Persian text of the repository, and what the reader refuses.</summary>
public sealed class TranslationTests
{
    private static readonly Lazy<Catalogue> Loaded = new(() => PilotSample.Load(SampleFiles.PilotPath));

    private static Catalogue Catalogue => Loaded.Value;

    // --- The Persian text of the pilot sample ---

    [Fact]
    public void Every_text_of_the_sample_has_a_Persian_text()
    {
        var texts = AllTexts(Catalogue).ToList();

        Assert.Equal(5 + (14 * 2) + 20 + (8 * 2) + 30 + (5 * 2) + 3, texts.Count);
        Assert.All(texts, text => Assert.False(text.Text.Resolve("fa", "en").IsFallback, text.Name));
    }

    [Fact]
    public void The_Persian_text_is_a_draft_until_somebody_reviews_it()
    {
        Assert.All(AllTexts(Catalogue), text => Assert.Equal(TextReview.Draft, text.Text.Resolve("fa").Review));
        Assert.All(AllTexts(Catalogue), text => Assert.Equal(TextReview.Reviewed, text.Text.Resolve("en").Review));
    }

    [Fact]
    public void The_Persian_text_is_written_in_the_Persian_alphabet()
    {
        // No Arabic yeh or kaf and no Latin letters: both would be signs of text that was pasted, not written.
        foreach (var (name, text) in AllTexts(Catalogue))
        {
            var persian = text.Resolve("fa").Value;
            Assert.False(persian.Contains('ي', StringComparison.Ordinal) || persian.Contains('ك', StringComparison.Ordinal), name);
            Assert.DoesNotContain(persian, character => character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'));
        }
    }

    [Fact]
    public void A_translated_text_keeps_the_English_text_next_to_it()
    {
        var lineage = Catalogue.Find(new EventId("event-03"))!;

        Assert.Equal("Sohrab learns his lineage", lineage.Label.Resolve("en").Value);
        Assert.Equal("سهراب تبار خود را می‌شناسد", lineage.Label.Resolve("fa").Value);
        Assert.Equal("دیدار در سمنگان", Catalogue.Info.ReadingStages[1].Label.Resolve("fa").Value);
    }

    [Fact]
    public void A_role_is_translated_for_its_entity_in_its_event()
    {
        var lineage = Catalogue.Find(new EventId("event-03"))!;
        var rostam = lineage.Associations.Single(association => association.Entity == new EntityId("character-rostam"));

        Assert.Equal("پدری که در این گفت‌وگو از او نام برده می‌شود", rostam.Role.Resolve("fa").Value);
        Assert.Equal(Presence.MentionedOnly, rostam.Presence);
    }

    // --- What a translation can and cannot do ---

    [Fact]
    public void A_reviewed_translation_makes_its_texts_and_the_names_of_its_language_reviewed()
    {
        var catalogue = SampleFiles.Read(null, Translation(translation => translation["review"] = "reviewed"));
        var rostam = catalogue.Find(new EntityId("character-rostam"))!;

        Assert.Equal(TextReview.Reviewed, rostam.Summary.Resolve("fa").Review);
        Assert.Equal("خلاصه", rostam.Summary.Resolve("fa").Value);
        Assert.Equal(TextReview.Reviewed, rostam.Label.Resolve("fa").Review);
        Assert.Equal("رستم", rostam.Label.Resolve("fa").Value);
    }

    [Fact]
    public void A_text_without_a_translation_stays_a_fallback()
    {
        var catalogue = SampleFiles.Read(null, Translation());

        Assert.False(catalogue.Find(new EntityId("character-rostam"))!.Summary.Resolve("fa", "en").IsFallback);
        Assert.True(catalogue.Find(new EntityId("character-zal"))!.Summary.Resolve("fa", "en").IsFallback);
    }

    [Fact]
    public void A_second_language_stands_next_to_the_first()
    {
        var german = Translation(translation =>
        {
            translation["language"] = "de";
            translation["entities"]!["character-rostam"]!["summary"] = "Zusammenfassung";
            translation["entities"]!["character-rostam"]!["label"] = "Rostam";
        });

        var rostam = SampleFiles.Read(null, Translation(), german).Find(new EntityId("character-rostam"))!;

        Assert.Equal("Zusammenfassung", rostam.Summary.Resolve("de").Value);
        Assert.Equal("خلاصه", rostam.Summary.Resolve("fa").Value);
        Assert.Equal(["de", "en", "fa"], rostam.Label.Languages.Order());
    }

    [Fact]
    public void A_translation_of_something_the_sample_does_not_have_is_refused()
    {
        var problems = SampleFiles.Problems(_ => { }, Translation(translation =>
        {
            translation["entities"]!["character-gordafarid"] = new JsonObject { ["summary"] = "خلاصه" };
            translation["events"] = new JsonObject
            {
                ["event-03"] = new JsonObject { ["roles"] = new JsonObject { ["character-zal"] = "نقش" } },
            };
            translation["media"] = new JsonObject
            {
                ["media-umma-1963-1-46"] = new JsonObject { ["altText"] = "شرح" },
            };
        }));

        Assert.Equal(
            [
                "translation fa: entities/character-gordafarid/summary translates nothing the sample has.",
                "translation fa: events/event-03/roles/character-zal translates nothing the sample has.",
                "translation fa: media/media-umma-1963-1-46/altText translates nothing the sample has.",
            ],
            problems);
    }

    [Fact]
    public void A_Persian_name_is_given_once()
    {
        var problems = SampleFiles.Problems(_ => { }, Translation(translation =>
            translation["entities"]!["character-rostam"]!["label"] = "رستم دستان"));

        Assert.Equal(["character-rostam: the Persian name is given twice, as labelFa and by the translation."], problems);
    }

    [Theory]
    [InlineData("formatVersion", "dastan-translation-9", "translation 1: the format dastan-translation-9 is not understood; this reader reads dastan-translation-0.1.")]
    [InlineData("datasetId", "another-sample", "translation fa: it belongs to another-sample, not to this sample.")]
    [InlineData("language", "en", "translation en: it translates into the language the sample is written in.")]
    [InlineData("language", "Persian", "translation Persian: language is not a language tag such as fa or de.")]
    [InlineData("review", "done", "translation fa: review has the unknown value \"done\".")]
    public void A_translation_states_what_it_is(string field, string value, string expected)
    {
        var problems = SampleFiles.Problems(_ => { }, Translation(translation => translation[field] = value));

        Assert.Contains(expected, problems);
    }

    [Fact]
    public void Two_translations_into_one_language_are_refused()
    {
        var problems = SampleFiles.Problems(_ => { }, Translation(), Translation());

        Assert.Contains("translation fa: the sample has two translations into this language.", problems);
    }

    [Fact]
    public void A_field_of_a_translation_the_reader_does_not_know_is_reported()
    {
        var problems = SampleFiles.Problems(_ => { }, Translation(translation =>
        {
            translation["sources"] = new JsonObject();
            translation["entities"]!["character-rostam"]!["nickname"] = "تهمتن";
        }));

        Assert.Equal(
            [
                "translation fa.entities.character-rostam: the field nickname is not known to this reader.",
                "translation fa: the field sources is not known to this reader.",
            ],
            problems);
    }

    [Fact]
    public void A_translation_that_is_not_JSON_is_refused()
    {
        var problems = SampleFiles.Problems(_ => { }, "{ \"formatVersion\": ");

        Assert.StartsWith("translation 1: it is not valid JSON", Assert.Single(problems), StringComparison.Ordinal);
    }

    /// <summary>A small translation of the pilot sample: one summary, as a draft.</summary>
    private static string Translation(Action<JsonObject>? change = null)
    {
        var translation = new JsonObject
        {
            ["formatVersion"] = PilotSample.TranslationVersion,
            ["datasetId"] = "dastan-shahnameh-rostam-sohrab-pilot-v0.5",
            ["language"] = "fa",
            ["review"] = "draft",
            ["entities"] = new JsonObject
            {
                ["character-rostam"] = new JsonObject { ["summary"] = "خلاصه" },
            },
        };
        change?.Invoke(translation);
        return translation.ToJsonString();
    }

    /// <summary>Every text of a collection that a visitor can read, with a name for the failure message.</summary>
    private static IEnumerable<(string Name, LocalizedText Text)> AllTexts(Catalogue catalogue)
    {
        foreach (var stage in catalogue.Info.ReadingStages)
        {
            yield return ($"stage {stage.Level}", stage.Label);
        }

        foreach (var entity in catalogue.Entities)
        {
            yield return ($"{entity.Id} label", entity.Label);
            yield return ($"{entity.Id} summary", entity.Summary);
        }

        foreach (var relation in catalogue.Relations)
        {
            yield return ($"{relation.Id} label", relation.Label);
        }

        foreach (var storyEvent in catalogue.Events)
        {
            yield return ($"{storyEvent.Id} label", storyEvent.Label);
            yield return ($"{storyEvent.Id} summary", storyEvent.Summary);
            foreach (var association in storyEvent.Associations)
            {
                yield return ($"{storyEvent.Id} role of {association.Entity}", association.Role);
            }
        }

        foreach (var item in catalogue.Media)
        {
            yield return ($"{item.Id} label", item.Label);
            yield return ($"{item.Id} caption", item.Caption);
            if (item.AltText is not null)
            {
                yield return ($"{item.Id} text alternative", item.AltText);
            }
        }
    }
}
