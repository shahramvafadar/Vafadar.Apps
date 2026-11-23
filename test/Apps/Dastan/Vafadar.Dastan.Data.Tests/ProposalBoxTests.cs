using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The places visitors propose to the editors, in the database (D-085): what is kept of a proposal and what is
/// not, what is no proposal, a box that is full, the decision of an editor, and the table as the history of
/// the changes of form makes it. Test data.
/// </summary>
public sealed class ProposalBoxTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 11, 23);
    private static readonly string[] Kinds = ["university", "company"];
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-proposals-" + Guid.NewGuid().ToString("N"));
    private readonly string _database;

    public ProposalBoxTests() => _database = TestDatabase.For(Path.Combine(_folder, "proposals.db"));

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
    public void A_proposal_is_the_place_in_the_words_of_the_visitor_and_the_day_and_nothing_about_the_visitor()
    {
        var box = new ProposalBox(_database);
        var read = ProposalBox.Read(new ProposedPlace("  University\tof   the  Tests\u0007 ", "university", " Tehran ", " https://example.org/about ", "  Founded 1934.\r\nI studied there. "), Kinds)!;

        Assert.True(box.Take(read, "fa", Day));

        var proposal = Assert.Single(box.Waiting());
        Assert.Equal(("University of the Tests", "university", "Tehran", "https://example.org/about", "Founded 1934.\nI studied there.", "fa", Day),
            (proposal.Name, proposal.Kind, proposal.Where, proposal.Page!.AbsoluteUri, proposal.Note, proposal.Language, proposal.On));
        Assert.Equal(32, proposal.Id.Length);
        Assert.Null(proposal.SettledBy);
        Assert.Null(proposal.Outcome);

        // The table holds these columns and no other: nothing about who wrote it, and a day without a time.
        Assert.Equal(
            "Entry,Id,Kind,Language,Name,Note,On,Outcome,Page,SettledBy,SettledOn,Where",
            Ask<string>("select string_agg(column_name, ',' order by column_name) from information_schema.columns where table_schema = 'delivery' and table_name = 'Proposals'"));
        Assert.Equal("date", Ask<string>("select data_type from information_schema.columns where table_schema = 'delivery' and table_name = 'Proposals' and column_name = 'On'"));
    }

    [Fact]
    public void Only_the_name_and_the_kind_are_needed()
    {
        var read = ProposalBox.Read(new ProposedPlace("A company", "company", " ", null, string.Empty), Kinds)!;

        Assert.Equal(new ProposedPlace("A company", "company"), read);
    }

    [Theory]
    [InlineData("", "university", null, null)]
    [InlineData("   ", "university", null, null)]
    [InlineData("A place", "family", null, null)]
    [InlineData("A place", "", null, null)]
    [InlineData("A place", "university", null, "http://example.org")]
    [InlineData("A place", "university", null, "https://user:secret@example.org")]
    [InlineData("A place", "university", null, "javascript:alert(1)")]
    [InlineData("A place", "university", null, "https://localhost")]
    [InlineData("A place", "university", null, "example.org")]
    public void What_is_no_proposal_is_none(string name, string kind, string? where, string? page)
    {
        Assert.Null(ProposalBox.Read(new ProposedPlace(name, kind, where, page), Kinds));
    }

    [Fact]
    public void What_is_too_long_is_no_proposal()
    {
        Assert.NotNull(ProposalBox.Read(new ProposedPlace(new string('a', ProposalBox.LongestName), "company", new string('b', ProposalBox.LongestLine), null, new string('c', ProposalBox.LongestNote)), Kinds));
        Assert.Null(ProposalBox.Read(new ProposedPlace(new string('a', ProposalBox.LongestName + 1), "company"), Kinds));
        Assert.Null(ProposalBox.Read(new ProposedPlace("A", "company", new string('b', ProposalBox.LongestLine + 1)), Kinds));
        Assert.Null(ProposalBox.Read(new ProposedPlace("A", "company", null, null, new string('c', ProposalBox.LongestNote + 1)), Kinds));
    }

    [Fact]
    public void An_editor_decides_a_proposal_once_and_names_the_entry_of_a_place_that_was_added()
    {
        var box = new ProposalBox(_database);
        box.Take(new ProposedPlace("First", "university"), "en", Day);
        box.Take(new ProposedPlace("Second", "company"), "en", Day.AddDays(1));
        var (first, second) = (box.Waiting()[0], box.Waiting()[1]);
        Assert.Equal(("First", "Second"), (first.Name, second.Name));

        // An entry is a collection and an entity; a place that is declined has none.
        Assert.False(box.Settle(first.Id, "an editor", Day.AddDays(2), ProposalOutcome.Added, "no entry"));
        Assert.False(box.Settle(first.Id, "an editor", Day.AddDays(2), ProposalOutcome.Declined, "universities/university-first"));
        Assert.False(box.Settle("nothing", "an editor", Day.AddDays(2), ProposalOutcome.Added));
        Assert.Equal(2, box.Waiting().Count);

        Assert.True(box.Settle(first.Id, "an editor", Day.AddDays(2), ProposalOutcome.Added, " universities/university-first "));
        Assert.False(box.Settle(first.Id, "another editor", Day.AddDays(3), ProposalOutcome.Declined));
        Assert.True(box.Settle(second.Id, "an editor", Day.AddDays(3), ProposalOutcome.Declined));

        Assert.Empty(box.Waiting());
        var decided = box.Decided();
        Assert.Equal(["Second", "First"], decided.Select(proposal => proposal.Name));
        Assert.Equal((ProposalOutcome.Added, "universities/university-first", "an editor", Day.AddDays(2)), (decided[1].Outcome, decided[1].Entry, decided[1].SettledBy, decided[1].SettledOn));
        Assert.Equal((ProposalOutcome.Declined, null), (decided[0].Outcome, decided[0].Entry));

        // What was decided long ago is forgotten; what waits never is.
        box.Take(new ProposedPlace("Third", "company"), "en", Day);
        Assert.Equal(1, box.Forget(Day.AddDays(3)));
        Assert.Equal("Second", Assert.Single(box.Decided()).Name);
        Assert.Equal("Third", Assert.Single(box.Waiting()).Name);
    }

    [Fact]
    public void A_box_that_is_full_takes_no_more_until_an_editor_has_decided()
    {
        var box = new ProposalBox(_database);
        for (var index = 0; index < ProposalBox.MostWaiting; index++)
        {
            Assert.True(box.Take(new ProposedPlace($"Place {index}", "company"), "en", Day));
        }

        Assert.False(box.Take(new ProposedPlace("One more", "company"), "en", Day));
        Assert.True(box.Settle(box.Waiting()[0].Id, "an editor", Day, ProposalOutcome.Declined));
        Assert.True(box.Take(new ProposedPlace("One more", "company"), "en", Day));
    }

    [Theory]
    [InlineData("University of Tehran", "university  of TEHRAN.")]
    [InlineData("دانشگاه تهران", "دانشگاه‌ تهران")]
    [InlineData("كلية علي", "کلیه علی")]
    [InlineData("École 42", "ecole ۴۲")]
    public void The_same_place_written_another_way_stands_together(string one, string other)
    {
        Assert.Equal(ProposalBox.Folded(one), ProposalBox.Folded(other));
        Assert.NotEqual(ProposalBox.Folded(one), ProposalBox.Folded(one + " 2"));
    }

    [Fact]
    public void The_tables_are_what_the_history_of_their_changes_of_form_makes_them()
    {
        using var context = new DeliveryContext(new DbContextOptionsBuilder<DeliveryContext>().UseNpgsql(_database, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Delivery.Schema)).Options);

        // A change of the model that no change of form carries would be found here – also one written by hand.
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(context.Database.GetPendingMigrations());
        Assert.Null(Delivery.Missing(_database));
    }
}
