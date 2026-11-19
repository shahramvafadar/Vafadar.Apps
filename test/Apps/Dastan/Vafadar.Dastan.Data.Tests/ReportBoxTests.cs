using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The reports of visitors in the database (steps 14.8 and 19.3): what is kept of a report, what is no report,
/// a box that is full – also when many write at the same moment – and the note that an editor has looked.
/// Test data.
/// </summary>
public sealed class ReportBoxTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 11, 18);
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-reports-" + Guid.NewGuid().ToString("N"));
    private readonly string _database;

    public ReportBoxTests() => _database = TestDatabase.For(Path.Combine(_folder, "reports.db"));

    private T Ask<T>(string sql)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)command.ExecuteScalar()!;
    }

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
        var box = new ReportBox(_database);

        Assert.True(box.Take("story-x", 3, "fa", "evidence/relation/relation-02", "  The passage names\r\nanother man.\u0007 ", Day));

        var report = Assert.Single(box.Waiting());
        Assert.Equal(("story-x", 3, "fa", "evidence/relation/relation-02", "The passage names\nanother man.", Day), (report.Collection, report.Revision, report.Language, report.Statement, report.Text, report.On));
        Assert.Equal(32, report.Id.Length);
        Assert.Null(report.SettledBy);

        // The table holds these columns and no other: nothing about who wrote it, and a day without a time.
        Assert.Equal(
            "Collection,Id,Language,On,Revision,SettledBy,SettledOn,Statement,Text",
            Ask<string>("select string_agg(column_name, ',' order by column_name) from information_schema.columns where table_schema = 'delivery' and table_name = 'Reports'"));
        Assert.Equal("date", Ask<string>("select data_type from information_schema.columns where table_schema = 'delivery' and table_name = 'Reports' and column_name = 'On'"));
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
        var box = new ReportBox(_database);

        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "en", "evidence/entity/x", "   ", Day));
        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "en", "evidence/entity/x", new string('a', ReportBox.LongestText + 1), Day));
        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "en", "somewhere/else", "Words.", Day));
        Assert.Throws<ArgumentException>(() => box.Take("../x", null, "en", "evidence/entity/x", "Words.", Day));
        Assert.Throws<ArgumentException>(() => box.Take("story-x", null, "e n", "evidence/entity/x", "Words.", Day));

        Assert.Empty(box.Waiting());
        Assert.Equal(0L, Ask<long>("""select count(*) from delivery."Reports" """));
    }

    [Fact]
    public void A_report_an_editor_has_looked_at_waits_no_longer_and_keeps_who_looked()
    {
        var box = new ReportBox(_database);
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
    public void Reports_wait_in_the_order_they_came()
    {
        var box = new ReportBox(_database);
        box.Take("story-x", null, "en", "evidence/entity/later", "Later.", Day.AddDays(1));
        box.Take("story-x", null, "en", "evidence/entity/earlier", "Earlier.", Day);

        Assert.Equal(["Earlier.", "Later."], box.Waiting().Select(report => report.Text));
    }

    [Fact]
    public async Task A_box_that_is_full_takes_no_more_until_somebody_has_looked()
    {
        var box = new ReportBox(_database);
        for (var number = 0; number < ReportBox.MostWaiting; number++)
        {
            Assert.True(box.Take("story-x", null, "en", "evidence/entity/x", $"Report {number}.", Day));
        }

        Assert.False(box.Take("story-x", null, "en", "evidence/entity/x", "One too many.", Day));
        Assert.Equal(ReportBox.MostWaiting, box.Waiting().Count);

        Assert.True(box.Settle(box.Waiting()[0].Id, "a reviewer", Day));
        Assert.True(box.Take("story-x", null, "en", "evidence/entity/x", "Room again.", Day));

        // One place is left again after another look; of twelve who write at the same moment one gets it.
        Assert.True(box.Settle(box.Waiting()[0].Id, "a reviewer", Day));
        using var start = new ManualResetEventSlim();
        var writing = Enumerable.Range(0, 12).Select(number => Task.Run(() =>
        {
            start.Wait();
            return new ReportBox(_database).Take("story-x", null, "en", "evidence/entity/x", $"At the same moment {number}.", Day);
        })).ToArray();
        start.Set();
        Assert.Single(await Task.WhenAll(writing), taken => taken);
        Assert.Equal(ReportBox.MostWaiting, box.Waiting().Count);
    }

    [Fact]
    public void A_box_nobody_wrote_into_holds_no_reports()
    {
        var box = new ReportBox(_database);

        Assert.Empty(box.Waiting());
        Assert.Empty(box.Looked());
        Assert.False(box.Settle(new string('0', 32), "a reviewer", Day));
    }

    [Fact]
    public void Reports_that_were_looked_at_long_ago_are_deleted_and_those_that_wait_never()
    {
        var box = new ReportBox(_database);
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
        var box = new ReportBox(_database);
        box.Take("story-x", null, "en", "evidence/entity/a", "One.", Day);
        box.Settle(box.Waiting()[0].Id, "a reviewer", Day);
        using var output = new StringWriter();

        var done = Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _database, "--looked-at-before", "2026-12-01"], output, Day);
        var noDay = Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _database, "--looked-at-before", "soon"], output, Day);
        var noFolder = Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", TestDatabase.Empty(), "--looked-at-before", "2026-12-01"], output, Day);

        Assert.Equal((0, 2, 1), (done, noDay, noFolder));
        Assert.Contains("Deleted 1 report(s)", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(box.Looked());
    }

    [Fact]
    public void Without_a_day_the_command_keeps_what_was_looked_at_in_the_last_ninety_days()
    {
        var box = new ReportBox(_database);
        box.Take("story-x", null, "en", "evidence/entity/a", "Looked at long ago.", Day.AddDays(-200));
        box.Take("story-x", null, "en", "evidence/entity/b", "Looked at ninety days ago.", Day.AddDays(-200));
        box.Take("story-x", null, "en", "evidence/entity/c", "Looked at lately.", Day.AddDays(-200));
        box.Take("story-x", null, "en", "evidence/entity/d", "Old, and nobody looked.", Day.AddDays(-200));
        box.Settle(box.Waiting().Single(report => report.Text == "Looked at long ago.").Id, "a reviewer", Day.AddDays(-91));
        box.Settle(box.Waiting().Single(report => report.Text == "Looked at ninety days ago.").Id, "a reviewer", Day.AddDays(-90));
        box.Settle(box.Waiting().Single(report => report.Text == "Looked at lately.").Id, "a reviewer", Day.AddDays(-5));
        using var output = new StringWriter();

        Assert.Equal(0, Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _database], output, Day));

        Assert.Equal(["Looked at lately.", "Looked at ninety days ago."], box.Looked().Select(report => report.Text).Order());
        Assert.Equal("Old, and nobody looked.", Assert.Single(box.Waiting()).Text);

        // Whoever runs the site may name another number of days; what is no number is not understood.
        Assert.Equal(0, Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _database, "--kept-days", "30"], output, Day));
        Assert.Equal("Looked at lately.", Assert.Single(box.Looked()).Text);
        Assert.Equal(2, Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _database, "--kept-days", "-1"], output, Day));
        Assert.Equal(2, Vafadar.Dastan.Content.ContentCommands.Run(["reports", "forget", _database, "--kept-days", "many"], output, Day));
        Assert.Single(box.Looked());
    }
}
