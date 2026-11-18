using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Threads the editors lay through a collection (step 14.9, R-EXP-04): what a thread is in the file, how far a
/// visitor follows it, and that a stop that is not reached is neither named nor counted. The threads are test
/// data: the collection of the repository has none.
/// </summary>
public sealed class ThreadsTests
{
    private const string Hero = "character-rostam";
    private static readonly Catalogue Pilot = PilotSample.Load(SampleFiles.PilotPath);

    // An event of the opening, an entity of a later part and its stage, and another name of the opening.
    private static readonly string Opening = Pilot.Events.First(storyEvent => storyEvent.Level == ReadingLevel.Opening).Id.Value;
    private static readonly Entity Later = Pilot.Entities.First(entity => entity.Level.CompareTo(ReadingLevel.Opening) > 0);
    private static readonly string Beside = Pilot.Entities.First(entity => entity.Level == ReadingLevel.Opening && entity.Id.Value != Hero && entity.Type.Nature == EntityNature.Being).Id.Value;

    private static JsonObject Stop(string field, string id, string note) => new() { [field] = id, ["note"] = note };

    private static JsonObject Thread(string id = "thread-father", int level = 0, params JsonObject[] stops) => new()
    {
        ["id"] = id, ["label"] = "A father and a son", ["summary"] = "From the hero to the one he does not know.", ["spoilerLevel"] = level,
        ["stops"] = new JsonArray([.. stops.Length > 0 ? stops : Stops()]),
    };

    private static JsonObject[] Stops() =>
    [
        Stop("entityId", Hero, "Start with the hero."),
        Stop("eventId", Opening, "Then what sets him on his way."),
        Stop("entityId", Later.Id.Value, "Then somebody of a later part."),
        Stop("entityId", Beside, "And back to somebody of the opening."),
    ];

    private static Action<JsonObject> With(params JsonObject[] threads) => sample => sample["threads"] = new JsonArray([.. threads]);

    private static Projection At(Catalogue catalogue, ReadingLevel level) => Projection.Of(catalogue, level, Audience.LocalDemonstration)!;

    [Fact]
    public void The_collection_of_the_repository_has_no_thread()
    {
        Assert.Empty(Pilot.Threads);
        Assert.Empty(At(Pilot, ReadingLevel.Opening).Threads);
        Assert.Empty(At(Pilot, ReadingLevel.Opening).Onward(new EntityId(Hero)));
    }

    [Fact]
    public void A_thread_is_read_with_its_stops_in_order_each_with_its_reason()
    {
        var thread = Assert.Single(SampleFiles.Read(With(Thread())).Threads);

        Assert.Equal(("thread-father", ReadingLevel.Opening, 4), (thread.Id, thread.Level, thread.Stops.Length));
        Assert.Equal("A father and a son", thread.Label.Resolve("en").Value);
        Assert.Equal([Hero, null, Later.Id.Value, Beside], thread.Stops.Select(stop => stop.Entity?.Value));
        Assert.Equal(Opening, thread.Stops[1].Event!.Value.Value);
        Assert.Equal("Then what sets him on his way.", thread.Stops[1].Note.Resolve("en").Value);
    }

    [Fact]
    public void A_thread_is_followed_as_far_as_the_visitor_has_read_and_says_only_that_it_goes_on()
    {
        var catalogue = SampleFiles.Read(With(Thread()));

        var opening = Assert.Single(At(catalogue, ReadingLevel.Opening).Threads);
        Assert.Equal([1, 2], opening.Steps.Select(step => step.Number));
        Assert.True(opening.Continues);
        Assert.Equal(Hero, opening.Steps[0].Entity!.Id.Value);
        Assert.Equal(Opening, opening.Steps[1].Step!.Event.Id.Value);

        // The stop after the one that is not reached is not shown either, although it could be read: the
        // thread is an order, and what follows a gap would say that there is one and where.
        Assert.DoesNotContain(opening.Steps, step => step.Entity?.Id.Value == Beside);

        var reached = Assert.Single(At(catalogue, Later.Level).Threads);
        Assert.Equal(4, reached.Steps.Length);
        Assert.False(reached.Continues);
    }

    [Fact]
    public void A_thread_of_a_later_part_is_offered_from_that_part_on()
    {
        var catalogue = SampleFiles.Read(With(Thread(level: Later.Level.Value)));

        Assert.Empty(At(catalogue, ReadingLevel.Opening).Threads);
        Assert.Null(At(catalogue, ReadingLevel.Opening).Thread("thread-father"));
        Assert.NotNull(At(catalogue, Later.Level).Thread("thread-father"));
    }

    [Fact]
    public void From_a_name_a_thread_leads_on_to_its_next_stop_with_the_reason()
    {
        var projection = At(SampleFiles.Read(With(Thread())), ReadingLevel.Opening);

        var onward = Assert.Single(projection.Onward(new EntityId(Hero)));
        Assert.Equal(("thread-father", 1, 2), (onward.Thread.Id, onward.Here.Number, onward.Next!.Number));
        Assert.Equal("Then what sets him on his way.", onward.Next.Stop.Note.Resolve("en").Value);

        // A name the thread does not pass leads nowhere, and neither does one behind what is not reached.
        Assert.Empty(projection.Onward(new EntityId(Beside)));
        Assert.Empty(projection.Onward(Later.Id));

        // At the last stop that is reached, the thread has nothing to offer yet.
        var last = At(SampleFiles.Read(With(Thread(stops: [Stop("eventId", Opening, "First."), Stop("entityId", Hero, "Then."), Stop("entityId", Later.Id.Value, "Later.")]))), ReadingLevel.Opening);
        Assert.Null(Assert.Single(last.Onward(new EntityId(Hero))).Next);
    }

    [Fact]
    public void A_thread_that_is_no_thread_is_refused_with_what_is_wrong()
    {
        IReadOnlyList<string> Of(params JsonObject[] threads) => SampleFiles.Problems(With(threads));

        Assert.Contains("thread-father: a thread has at least two stops.", Of(Thread(stops: [Stop("entityId", Hero, "Alone.")])));
        Assert.Contains("thread-father, stop 2: nobody-here is not in the collection.", Of(Thread(stops: [Stop("entityId", Hero, "One."), Stop("entityId", "nobody-here", "Two.")])));
        Assert.Contains("thread-father: a thread cannot be offered before its first stop can be read.", Of(Thread(stops: [Stop("entityId", Later.Id.Value, "One."), Stop("entityId", Hero, "Two.")])));
        Assert.Contains($"thread-father: {Hero} is a stop twice.", Of(Thread(stops: [Stop("entityId", Hero, "One."), Stop("entityId", Hero, "Two.")])));
        Assert.Contains("thread-father: two threads have this identifier.", Of(Thread(), Thread()));

        var two = Stop("entityId", Hero, "Both.");
        two["eventId"] = Opening;
        Assert.Contains("thread-father, stop 1: a stop names one thing: entityId, eventId or mediaId.", Of(Thread(stops: [two, Stop("entityId", Beside, "Two.")])));
        Assert.Contains("thread-father, stop 1: a stop names one thing: entityId, eventId or mediaId.", Of(Thread(stops: [new JsonObject { ["note"] = "Nothing." }, Stop("entityId", Beside, "Two.")])));

        var noNote = new JsonObject { ["entityId"] = Hero };
        Assert.Contains(Of(Thread(stops: [noNote, Stop("entityId", Beside, "Two.")])), problem => problem.Contains("note", StringComparison.Ordinal));
        var unknownField = Thread();
        unknownField["colour"] = "red";
        Assert.Contains(Of(unknownField), problem => problem.Contains("colour", StringComparison.Ordinal));
    }

    private static string Translation(JsonObject threads) => new JsonObject
    {
        ["formatVersion"] = PilotSample.TranslationVersion,
        ["datasetId"] = (string)SampleFiles.Pilot()["datasetId"]!,
        ["language"] = "fa",
        ["review"] = "reviewed",
        ["threads"] = threads,
    }.ToJsonString();

    [Fact]
    public void The_texts_of_a_thread_are_translated_the_note_of_a_stop_under_what_the_stop_names()
    {
        var translation = Translation(new JsonObject
        {
            ["thread-father"] = new JsonObject
            {
                ["label"] = "پدر و پسر",
                ["notes"] = new JsonObject { [Hero] = "با پهلوان آغاز کنید.", [Opening] = "سپس آنچه او را به راه می‌اندازد." },
            },
        });

        var thread = Assert.Single(SampleFiles.Read(With(Thread()), translation).Threads);

        Assert.Equal("پدر و پسر", thread.Label.Resolve("fa", "en").Value);
        Assert.Equal("با پهلوان آغاز کنید.", thread.Stops[0].Note.Resolve("fa", "en").Value);
        Assert.Equal("سپس آنچه او را به راه می‌اندازد.", thread.Stops[1].Note.Resolve("fa", "en").Value);

        // What has no translation is read in the language of the collection.
        Assert.True(thread.Summary.Resolve("fa", "en").IsFallback);
        Assert.True(thread.Stops[3].Note.Resolve("fa", "en").IsFallback);
    }

    [Fact]
    public void A_translation_of_a_thread_or_a_stop_that_is_not_there_is_refused()
    {
        var noThread = SampleFiles.Problems(With(Thread()), Translation(new JsonObject { ["thread-other"] = new JsonObject { ["label"] = "رشته" } }));
        var noStop = SampleFiles.Problems(With(Thread()), Translation(new JsonObject { ["thread-father"] = new JsonObject { ["notes"] = new JsonObject { ["nobody-here"] = "یادداشت" } } }));

        Assert.Contains("translation fa: threads/thread-other/label translates nothing the sample has.", noThread);
        Assert.Contains("translation fa: threads/thread-father/notes/nobody-here translates nothing the sample has.", noStop);
    }
}
