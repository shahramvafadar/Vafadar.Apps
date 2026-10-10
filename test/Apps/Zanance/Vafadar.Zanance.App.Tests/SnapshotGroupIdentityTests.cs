using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Native source reuse requires complete ordered object identity and unchanged displayed group captions.</summary>
public sealed class SnapshotGroupIdentityTests
{
    [Fact, Trait("AT", "AT-115")]
    public void Equivalent_complete_groups_retain_every_row_and_its_mutable_selection()
    {
        var rows = Enumerable.Range(0, 100_000).Select(i => new Row(i)).ToArray();
        var current = rows.Chunk(100).Select((items, i) => new Group($"Day {i}", $"Net {i}", items)).ToArray();
        var next = current.Select(group => group with { Rows = group.Rows.ToArray() }).ToArray();
        rows[^1].Selected = true;

        Assert.True(Matches(current, next));
        Assert.Equal(100_000, next.Sum(group => group.Rows.Count));
        Assert.True(next[^1].Rows[^1].Selected);
        Assert.Equal(rows.Select(row => row.Id), next.SelectMany(group => group.Rows).Select(row => row.Id));
    }

    [Theory, Trait("AT", "AT-115")]
    [InlineData("header")]
    [InlineData("net")]
    [InlineData("group-count")]
    [InlineData("group-order")]
    [InlineData("row-count")]
    [InlineData("row-order")]
    [InlineData("fresh-row")]
    public void Changed_caption_boundary_order_or_fresh_row_requires_publication(string change)
    {
        var current = new[] { new Group("Day 1", "−1 EUR", [new(1), new(2)]), new Group("Day 2", "+2 EUR", [new(3)]) };
        var next = current.Select(group => group with { Rows = group.Rows.ToArray() }).ToArray();
        next = change switch
        {
            "header" => [next[0] with { Header = "Translated day" }, next[1]],
            "net" => [next[0] with { NetText = "−1,00 €" }, next[1]],
            "group-count" => [next[0]],
            "group-order" => [next[1], next[0]],
            "row-count" => [next[0] with { Rows = [current[0].Rows[0]] }, next[1]],
            "row-order" => [next[0] with { Rows = current[0].Rows.Reverse().ToArray() }, next[1]],
            "fresh-row" => [next[0] with { Rows = [current[0].Rows[0], new(2)] }, next[1]],
            _ => throw new ArgumentException("Unknown fixture change", nameof(change))
        };

        Assert.False(Matches(current, next));
    }

    [Fact, Trait("AT", "AT-115")]
    public void Empty_results_match_but_a_new_empty_day_does_not()
    {
        Assert.True(Matches([], []));
        Assert.False(Matches([], [new("Day", "", [])]));
    }

    private static bool Matches(IReadOnlyList<Group> current, IReadOnlyList<Group> next) =>
        SnapshotGroupIdentity.Matches<Group, Row>(current, next, static group => group.Header,
            static group => group.NetText, static group => group.Rows);

    private sealed record Group(string Header, string NetText, IReadOnlyList<Row> Rows);

    private sealed class Row(int id)
    {
        public int Id { get; } = id;
        public bool Selected { get; set; }
    }
}
