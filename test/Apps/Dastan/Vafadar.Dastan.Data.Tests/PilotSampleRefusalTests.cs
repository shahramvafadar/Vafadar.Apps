using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Input the reader has to refuse. Each test changes one thing in a copy of the pilot sample. The first group are
/// the invalid copies the validator of the handoff pack checks itself with; the others are what a strict reader adds.
/// </summary>
public sealed class PilotSampleRefusalTests
{
    // --- The negative cases of the pack's validator that concern the story ---

    [Fact]
    public void A_relation_to_an_entity_that_does_not_exist_is_refused()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("relations", "relation-01")["to"] = "missing-entity");

        Assert.Contains("relation-01: the endpoint missing-entity does not exist.", problems);
    }

    [Fact]
    public void A_parentage_of_a_later_stage_cannot_be_moved_to_the_opening()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("relations", "relation-04")["spoilerLevel"] = 0);

        Assert.Contains(problems, problem => problem.StartsWith("relation-04: its endpoint character-sohrab belongs to a later reading level", StringComparison.Ordinal));
    }

    [Fact]
    public void A_story_event_cannot_be_given_a_year()
    {
        var problems = SampleFiles.Problems(sample => sample["events"]![0]!["temporalExpression"]!["year"] = 1000);

        Assert.Equal(["event-01.temporalExpression: year would turn a place in the story into a date."], problems);
    }

    [Fact]
    public void A_museum_image_without_permission_cannot_be_made_showable()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-umma-1963-1-46")["demoRenderEligibility"]!["allowed"] = true);

        Assert.Contains("media-umma-1963-1-46: an item without established permission has no file and is not shown.", problems);
        Assert.Contains("media-umma-1963-1-46: only original project content is shown in the demonstration.", problems);
    }

    [Fact]
    public void A_document_for_reviewers_cannot_be_the_public_page_of_an_image()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-dastan-family-illustration")["sourceUrl"] = "../../docs/product/17-flagship-content-and-source-plan.md");

        Assert.Equal(["media-dastan-family-illustration: sourceUrl is the public page of an item; a local document cannot be one."], problems);
    }

    [Fact]
    public void A_working_document_of_the_editors_cannot_be_the_transcript_of_a_video()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-dastan-parentage-video")["transcriptUrl"] = "../media/dastan-parentage-transcript.md");

        Assert.Contains(
            "media-dastan-parentage-video: the transcript for visitors is a plain text file (text/plain, .txt); a working document of the editors is not one.",
            problems);
    }

    [Fact]
    public void An_era_cannot_stand_where_a_calendar_belongs()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-met-452419")["creationDate"]!["calendar"] = "CE");

        Assert.Equal(["media-met-452419.creationDate: calendar is CE, which is an era and not a calendar."], problems);
    }

    // --- The format ---

    [Fact]
    public void A_format_the_reader_does_not_know_is_refused_before_anything_else_is_read()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            sample["schemaVersion"] = "dastan-pilot-0.3";
            sample.Record("relations", "relation-01")["to"] = "missing-entity";
        });

        Assert.Equal(["sample: the format dastan-pilot-0.3 is not understood; this reader reads dastan-pilot-0.2."], problems);
    }

    [Fact]
    public void A_sample_without_a_version_is_refused()
    {
        var problems = SampleFiles.Problems(sample => sample.Remove("schemaVersion"));

        Assert.Equal(["sample: schemaVersion is missing."], problems);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[]")]
    [InlineData("""{ "schemaVersion": "dastan-pilot-0.2", "schemaVersion": "dastan-pilot-0.2" }""")]
    public void Text_that_is_not_one_JSON_object_without_repeated_fields_is_refused(string text)
    {
        var problems = Assert.Throws<SampleFormatException>(() => PilotSample.Read(text, SampleFiles.Options())).Problems;

        Assert.StartsWith("sample: it is not", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Fact]
    public void A_field_the_reader_does_not_know_is_reported_and_not_ignored()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            sample["audience"] = "everyone";
            sample.Record("entities", "character-rostam")["nickname"] = "Tahamtan";
            sample.Record("media", "media-met-452419")["rights"]!["waiver"] = true;
        });

        Assert.Equal(
            [
                "character-rostam: the field nickname is not known to this reader.",
                "media-met-452419.rights: the field waiver is not known to this reader.",
                "sample: the field audience is not known to this reader.",
            ],
            problems);
    }

    [Fact]
    public void A_record_names_its_problem_by_its_identifier_and_all_problems_are_reported()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            sample.Record("entities", "character-zal").Remove("summary");
            sample.Record("sources", "src-ganjoor-s02")["accessedAt"] = "15.11.2026";
            sample.Record("events", "event-05")["order"] = 0;
        });

        Assert.Equal(
            [
                "src-ganjoor-s02: accessedAt is not a day written as year-month-day.",
                "character-zal: summary is missing.",
                "event-05: order is not a positive whole number.",
            ],
            problems);
    }

    [Fact]
    public void A_record_without_an_identifier_is_named_by_its_place()
    {
        var problems = SampleFiles.Problems(sample => sample["entities"]![3]!.AsObject().Remove("id"));

        Assert.Contains("sample.entities[3]: id is missing.", problems);
    }

    // --- Publication ---

    [Fact]
    public void An_editorial_sample_cannot_claim_approval_for_the_public()
    {
        var problems = SampleFiles.Problems(sample => sample["publication"]!["publicReleaseApproved"] = true);

        Assert.Equal(["sample.publication: an editorial sample cannot be approved for public release."], problems);
    }

    [Theory]
    [InlineData("rawDatasetPubliclyServable", "sample.inputContract: the sample itself is editorial input and can never be served to visitors.")]
    [InlineData("reviewerFilesPubliclyServable", "sample.inputContract: the documents for reviewers can never be served to visitors.")]
    public void Editorial_input_cannot_be_declared_public(string field, string expected)
    {
        var problems = SampleFiles.Problems(sample => sample["inputContract"]![field] = true);

        Assert.Equal([expected], problems);
    }

    // --- Told time is not calendar time ---

    [Fact]
    public void The_story_cannot_be_mapped_to_a_calendar()
    {
        var problems = SampleFiles.Problems(sample => sample["editionContext"]!["calendarMapping"] = "Gregorian");

        Assert.StartsWith("sample.editionContext: calendarMapping would give the story a calendar", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Fact]
    public void A_relation_of_the_story_cannot_be_given_a_calendar_period()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("relations", "relation-03")["scope"]!["calendarPeriod"] = new JsonObject { ["startYear"] = 900 });

        Assert.StartsWith("relation-03.scope: calendarPeriod would date a relation of the story", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("calendarMapping", "Gregorian")]
    [InlineData("duration", "three days")]
    [InlineData("date", "1010-03-08")]
    [InlineData("start", "spring")]
    [InlineData("end", "autumn")]
    public void A_place_in_the_story_gains_neither_a_date_nor_a_length(string field, string value)
    {
        var problems = SampleFiles.Problems(sample => sample.Record("events", "event-02")["temporalExpression"]![field] = value);

        Assert.StartsWith($"event-02.temporalExpression: {field} would ", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Fact]
    public void The_time_of_a_story_event_is_its_place_in_the_story()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("events", "event-02")["temporalExpression"]!["kind"] = "calendar_date");

        Assert.Equal(["event-02.temporalExpression: the time of a story event is its place in the story (narrative_position), not calendar_date."], problems);
    }

    [Fact]
    public void The_order_of_the_events_is_the_order_of_the_telling()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("events", "event-02")["orderBasis"] = "chronological");

        Assert.Equal(["event-02: orderBasis is chronological; the order of a story event is the order of the telling (narrative)."], problems);
    }

    [Fact]
    public void An_artwork_cannot_date_what_it_shows()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-met-452419")["depictedContext"]!["calendarDate"] = "1450");

        Assert.StartsWith("media-met-452419.depictedContext: calendarDate would date the story by an artwork", Assert.Single(problems), StringComparison.Ordinal);
    }

    // --- Events ---

    [Fact]
    public void A_bare_list_of_participants_is_refused()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("events", "event-03")["participantIds"] = new JsonArray("character-rostam"));

        Assert.Equal(["event-03: participantIds does not say what each entity has to do with the event; entityAssociations does."], problems);
    }

    [Fact]
    public void A_way_of_taking_part_the_reader_does_not_know_is_refused()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("events", "event-03")["entityAssociations"]![2]!["presence"] = "present");

        Assert.Equal(["event-03.entityAssociations[2]: presence has the unknown value \"present\"."], problems);
    }

    [Fact]
    public void A_place_that_acts_is_refused()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            var samangan = sample.Record("events", "event-01")["entityAssociations"]!.AsArray()
                .Single(association => (string?)association!["entityId"] == "place-samangan");
            samangan!["presence"] = "acts_in_selected_scenes";
        });

        Assert.Equal(["event-01: the place place-samangan is associated as Acts, not as the setting."], problems);
    }

    [Fact]
    public void Two_events_at_the_same_place_in_the_story_are_refused()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("events", "event-05")["order"] = 4);

        Assert.Equal(["event-04: event-05 has the same place in the story order."], problems);
    }

    // --- Entities and relations ---

    [Theory]
    [InlineData("entities", "character-rostam", "type", "Hero", "character-rostam: type has the unknown value \"Hero\".")]
    [InlineData("relations", "relation-06", "type", "owns", "relation-06: type has the unknown value \"owns\".")]
    [InlineData("relations", "relation-06", "reviewStatus", "final", "relation-06: reviewStatus has the unknown value \"final\".")]
    [InlineData("sources", "src-met-452419", "kind", "blog", "src-met-452419: kind has the unknown value \"blog\".")]
    public void A_value_outside_the_managed_lists_is_refused(string group, string id, string field, string value, string expected)
    {
        var problems = SampleFiles.Problems(sample => sample.Record(group, id)[field] = value);

        Assert.Contains(expected, problems);
    }

    [Fact]
    public void A_marriage_cannot_be_declared_one_sided()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("relations", "relation-03")["directionality"] = "directed");

        Assert.Equal(["relation-03: directionality contradicts the relation type spouseOf, which is symmetric."], problems);
    }

    [Fact]
    public void A_sample_tells_exactly_one_story_arc()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("entities", "character-zal")["type"] = "NarrativeArc");

        Assert.Contains("sample: a sample tells one story arc; this one has 2.", problems);
    }

    [Fact]
    public void A_reading_level_is_not_negative()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("entities", "character-zal")["spoilerLevel"] = -1);

        Assert.Contains("character-zal: spoilerLevel is negative.", problems);
    }

    [Fact]
    public void A_record_without_evidence_is_refused()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("entities", "character-zal")["sourceIds"] = new JsonArray());

        Assert.Equal(["character-zal: no source supports the record."], problems);
    }

    // --- Addresses and files ---

    [Theory]
    [InlineData("http://ganjoor.net/ferdousi/shahname/sohrab/sh2")]
    [InlineData("https://reader:secret@ganjoor.net/ferdousi/shahname/sohrab/sh2")]
    [InlineData("ftp://ganjoor.net/sohrab")]
    [InlineData("ganjoor.net/ferdousi")]
    public void A_source_is_an_https_address_without_credentials(string address)
    {
        var problems = SampleFiles.Problems(sample => sample.Record("sources", "src-ganjoor-s02")["url"] = address);

        Assert.Equal(["src-ganjoor-s02: url is not an https address without credentials."], problems);
    }

    [Theory]
    [InlineData("../../../../README.md", "media-dastan-family-illustration: url leaves the folder of the collection.")]
    [InlineData("../../README.md", "media-dastan-family-illustration: url leaves the folder of the collection.")]
    [InlineData("../media/missing.png", "media-dastan-family-illustration: url names the file ../media/missing.png, which does not exist.")]
    [InlineData("..\\media\\dastan-family-connections.png", "media-dastan-family-illustration: url is not a path relative to the sample, written with forward slashes.")]
    [InlineData("/etc/passwd", "media-dastan-family-illustration: url is not a path relative to the sample, written with forward slashes.")]
    public void A_file_of_an_item_is_inside_the_folder_of_the_collection_and_exists(string path, string expected)
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", "media-dastan-family-illustration")["url"] = path);

        Assert.Contains(expected, problems);
    }

    [Fact]
    public void A_missing_file_is_accepted_only_when_files_are_not_checked()
    {
        var sample = SampleFiles.Pilot();
        sample.Record("media", "media-dastan-parentage-video")["posterUrl"] = "../media/poster-to-come.png";

        var catalogue = PilotSample.Read(sample.ToJsonString(), SampleFiles.Options(checkFiles: false));

        Assert.Equal("media/poster-to-come.png", catalogue.Find(new Core.MediaId("media-dastan-parentage-video"))!.Poster);
    }

    [Fact]
    public void A_document_for_reviewers_needs_its_audience_and_has_to_exist()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            sample.Record("media", "media-dastan-parentage-video").Remove("reviewerProvenanceAudience");
            sample.Record("media", "media-dastan-family-illustration")["reviewerProvenancePath"] = "../../docs/product/no-such-document.md";
        });

        Assert.Equal(
            [
                "media-dastan-parentage-video: a document for reviewers needs the audience reviewer_only.",
                "media-dastan-family-illustration: reviewerProvenancePath names the file ../../docs/product/no-such-document.md, which does not exist.",
            ],
            problems);
    }

    [Fact]
    public void A_document_for_reviewers_stays_inside_the_app()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-dastan-family-illustration")["reviewerProvenancePath"] = "../../../../../README.md");

        Assert.Equal(["media-dastan-family-illustration: reviewerProvenancePath leaves the folder of the editors' documents."], problems);
    }

    // --- Media ---

    [Fact]
    public void An_item_says_whether_it_may_be_shown()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-met-452419")["demoRenderEligibility"]!.AsObject().Remove("allowed"));

        Assert.Equal(["media-met-452419.demoRenderEligibility: allowed is missing."], problems);
    }

    [Fact]
    public void An_image_with_an_unchecked_description_cannot_be_shown()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            var diagram = sample.Record("media", "media-dastan-family-illustration");
            diagram["altTextStatus"] = "draft_pending_visual_review";
        });

        Assert.Equal(["media-dastan-family-illustration: an image that is shown needs a checked text alternative."], problems);
    }

    [Fact]
    public void A_video_without_captions_cannot_be_shown()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", "media-dastan-parentage-video").Remove("captions"));

        Assert.Equal(["media-dastan-parentage-video: a video that is shown needs its playing time, captions and a transcript."], problems);
    }

    [Fact]
    public void An_item_shows_the_world_of_its_sample()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-met-452419")["depictedContext"]!["worldId"] = "world-history");

        Assert.Equal(["media-met-452419.depictedContext: the item shows the world world-history, not the world of the sample."], problems);
    }

    [Theory]
    [InlineData("uploadDate")]
    [InlineData("publicationDate")]
    [InlineData("sharedAt")]
    public void A_date_the_reader_would_have_to_guess_the_meaning_of_is_refused(string field)
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", "media-met-452419")[field] = "2026-11-15");

        Assert.Equal([$"media-met-452419: {field} is not read by this reader; of an item only the creation date is."], problems);
    }

    // --- When an item was made ---

    [Fact]
    public void A_date_known_to_the_year_has_no_month()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", "media-met-452419")["creationDate"]!["month"] = 3);

        Assert.Equal(["media-met-452419.creationDate: a date known to the year has no month."], problems);
    }

    [Fact]
    public void No_era_has_a_year_zero()
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", "media-met-452419")["creationDate"]!["year"] = 0);

        Assert.StartsWith("media-met-452419.creationDate: A year of an era is between 1 and", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Fact]
    public void A_date_known_to_the_day_names_its_calendar()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-dastan-family-illustration")["creationDate"]!["calendar"] = null);

        Assert.Equal(["media-dastan-family-illustration.creationDate: a date known to the day names its calendar."], problems);
    }

    [Fact]
    public void A_day_that_does_not_exist_is_refused()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-dastan-family-illustration")["creationDate"]!["date"] = "2026-02-30");

        Assert.Equal(["media-dastan-family-illustration.creationDate: date is not a day written as year-month-day."], problems);
    }

    [Theory]
    [InlineData("precision", "decade", "media-met-452419.creationDate: precision has the unknown value \"decade\".")]
    [InlineData("calendar", "Mayan", "media-met-452419.creationDate: calendar has the unknown value \"Mayan\".")]
    [InlineData("era", "AH", "media-met-452419.creationDate: era has the unknown value \"AH\".")]
    public void A_way_of_dating_the_reader_does_not_know_is_refused(string field, string value, string expected)
    {
        var problems = SampleFiles.Problems(sample => sample.Record("media", "media-met-452419")["creationDate"]![field] = value);

        Assert.Equal([expected], problems);
    }

    [Fact]
    public void Bounds_of_an_uncertain_date_are_not_read_silently()
    {
        var problems = SampleFiles.Problems(sample =>
            sample.Record("media", "media-met-452419")["creationDate"]!["uncertaintyBounds"] = new JsonObject { ["from"] = 1440, ["to"] = 1460 });

        Assert.StartsWith("media-met-452419.creationDate: uncertaintyBounds is not read by this reader", Assert.Single(problems), StringComparison.Ordinal);
    }
}
