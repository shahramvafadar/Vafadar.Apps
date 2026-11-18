namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The folder of the reports of visitors (step 14.8): what is kept of a report, what is no report, a box that
/// is full, and the note that an editor has looked. Test data.
/// </summary>
public sealed class ReportBoxTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 11, 18);
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-reports-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void A_report_is_the_statement_the_words_and_the_day_and_nothing_else()
    {
        var box = new ReportBox(_folder);

        Assert.True(box.Take("story-x", 3, "fa", "evidence/relation/relation-02", "  The passage names\r\nanother man.\u0007 ", Day));

        var report = Assert.Single(box.Waiting());
        Assert.Equal(("story-x", 3, "fa", "evidence/relation/relation-02", "The passage names\nanother man.", Day), (report.Collection, report.Revision, report.Language, report.Statement, report.Text, report.On));
        Assert.Equal(32, report.Id.Length);
        Assert.Null(report.SettledBy);

        // The file holds these fields and no other: nothing about who wrote it.
        var file = File.ReadAllText(Assert.Single(Directory.GetFiles(_folder)));
        Assert.Equal(["collection", "id", "language", "on", "revision", "settledBy", "settledOn", "statement", "text"],
            System.Text.Json.Nodes.JsonNode.Parse(file)!.AsObject().Select(field => field.Key).Order(StringComparer.Ordinal));
        Assert.Contains("\"on\": \"2026-11-18\"", file, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("evidence/entity/character-rostam")]
    [InlineData("evidence/relation/relation-02")]
    [InlineData("evidence/media/media-1")]
    [InlineData("evidence/event/event-01")]
    [InlineData("evidence/event/event-01/entity/character-rostam")]
    public void The_address_of_the_sources_of_a_statement_names_it(string statement)
    {
        Assert.True(ReportBox.IsStatement(statement));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("entity/character-rostam")]
    [InlineData("evidence/entity/")]
    [InlineData("evidence/entity/a/b")]
    [InlineData("evidence/entity/../../etc")]
    [InlineData("evidence/person/x")]
    [InlineData("evidence/relation/x/entity/y")]
    [InlineData("evidence/entity/a b")]
    [InlineData("/evidence/entity/x")]
    [InlineData("evidence/entity/x?from=y")]
    public void What_is_no_such_address_names_no_statement(string? statement)
    {
        Assert.False(ReportBox.IsStatement(statement));
    }

    [Fact]
    public void What_is_no_report_is_refused_and_leaves_nothing()
    {
        var box = new ReportBox(_folder);

        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "en", "evidence/entity/x", "   ", Day));
        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "en", "evidence/entity/x", new string('a', ReportBox.LongestText + 1), Day));
        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "en", "somewhere/else", "Words.", Day));
        Assert.Throws<ArgumentException>(() => box.Take("../x", null, "en", "evidence/entity/x", "Words.", Day));
        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "e n", "evidence/entity/x", "Words.", Day));

        Assert.Empty(box.Waiting());
        Assert.False(Directory.Exists(_folder) && Directory.EnumerateFileSystemEntries(_folder).Any());
    }

    [Fact]
    public void A_report_an_editor_has_looked_at_waits_no_longer_and_keeps_who_looked()
    {
        var box = new ReportBox(_folder);
        box.Take("story-x", null, "en", "evidence/entity/a", "The first.", Day);
        box.Take("story-x", null, "en", "evidence/entity/b", "The second.", Day.AddDays(1));
        var first = box.Waiting()[0];

        Assert.True(box.Settle(first.Id, "a reviewer", Day.AddDays(2)));

        Assert.Equal("The second.", Assert.Single(box.Waiting()).Text);
        var looked = Assert.Single(box.Looked());
        Assert.Equal((first.Id, "The first.", "a reviewer", Day.AddDays(2)), (looked.Id, looked.Text, looked.SettledBy, looked.SettledOn));

        // Once is enough, and a name that is none settles nothing.
        Assert.False(box.Settle(first.Id, "a reviewer", Day));
        Assert.False(box.Settle("../settled/" + first.Id, "a reviewer", Day));
        Assert.False(box.Settle(new string('0', 32), "a reviewer", Day));
        Assert.Single(box.Waiting());
    }

    [Fact]
    public void Reports_wait_in_the_order_they_came_and_a_file_that_is_none_is_passed_over()
    {
        var box = new ReportBox(_folder);
        box.Take("story-x", null, "en", "evidence/entity/later", "Later.", Day.AddDays(1));
        box.Take("story-x", null, "en", "evidence/entity/earlier", "Earlier.", Day);
        File.WriteAllText(Path.Combine(_folder, "broken.json"), "{ not a report");
        File.WriteAllText(Path.Combine(_folder, new string('a', 32) + ".json"), """{ "id": "other", "on": "2026-11-18", "collection": "x", "language": "en", "statement": "evidence/entity/x", "text": "A name that is not its file." }""");
        File.WriteAllText(Path.Combine(_folder, "notes.txt"), "Not a report.");

        Assert.Equal(["Earlier.", "Later."], box.Waiting().Select(report => report.Text));
    }

    [Fact]
    public void A_box_that_is_full_takes_no_more_until_somebody_has_looked()
    {
        var box = new ReportBox(_folder);
        for (var number = 0; number < ReportBox.MostWaiting; number++)
        {
            Assert.True(box.Take("story-x", null, "en", "evidence/entity/x", $"Report {number}.", Day));
        }

        Assert.False(box.Take("story-x", null, "en", "evidence/entity/x", "One too many.", Day));
        Assert.Equal(ReportBox.MostWaiting, box.Waiting().Count);

        Assert.True(box.Settle(box.Waiting()[0].Id, "a reviewer", Day));
        Assert.True(box.Take("story-x", null, "en", "evidence/entity/x", "Room again.", Day));
    }

    [Fact]
    public void A_folder_that_is_not_there_holds_no_reports()
    {
        var box = new ReportBox(_folder);

        Assert.Empty(box.Waiting());
        Assert.Empty(box.Looked());
        Assert.False(box.Settle(new string('0', 32), "a reviewer", Day));
        Assert.False(Directory.Exists(_folder));
    }
    [Fact]
    public void Reports_that_were_looked_at_long_ago_are_deleted_and_those_that_wait_never()
    {
        var box = new ReportBox(_folder);
        box.Take("story-x", null, "en", "evidence/entity/a", "Looked at early.", Day);
        box.Take("story-x", null, "en", "evidence/entity/b", "Looked at late.", Day);
        box.Take("story-x", null, "en", "evidence/entity/c", "Still waiting, and old.", Day.AddDays(-400));
        Assert.True(box.Settle(box.Waiting().Single(report => report.Text == "Looked at early.").Id, "a reviewer", Day.AddDays(1)));
        Assert.True(box.Settle(box.Waiting().Single(report => report.Text == "Looked at late.").Id, "a reviewer", Day.AddDays(30)));

        Assert.Equal(1, box.Forget(Day.AddDays(10)));

        Assert.Equal("Looked at late.", Assert.Single(box.Looked()).Text);
        Assert.Equal("Still waiting, and old.", Assert.Single(box.Waiting()).Text);
        Assert.Equal(0, box.Forget(Day.AddDays(10)));
    }

    [Fact]
    public void The_command_deletes_what_was_looked_at_before_a_day_and_says_how_many()
    {
        var box = new ReportBox(_folder);
        box.Take("story-x", null, "en", "evidence/entity/a", "One.", Day);
        box.Settle(box.Waiting()[0].Id, "a reviewer", Day);
        using var output = new StringWriter();

        var done = Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _folder, "--looked-at-before", "2026-12-01"], output, Day);
        var noDay = Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _folder, "--looked-at-before", "soon"], output, Day);
        var noFolder = Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _folder + "-none", "--looked-at-before", "2026-12-01"], output, Day);

        Assert.Equal((0, 2, 1), (done, noDay, noFolder));
        Assert.Contains("Deleted 1 report(s)", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(box.Looked());
    }
}
