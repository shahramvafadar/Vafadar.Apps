using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The comparison of the quotes of a collection with the copies of its sources (backlog E4): a quote that
/// stands in its source, one that does not, one that differs only in how a page sets its letters, a source
/// without a copy, a record that does not exist. The quotes and the copies are test data.
/// </summary>
public sealed class QuoteCheckTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-quotes-" + Guid.NewGuid().ToString("N"));
    private readonly string _collection;
    private readonly string _source;
    private readonly string _relation;

    public QuoteCheckTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "sources"));
        _collection = Path.Combine(SampleFiles.Content, "samples", "shahnameh-pilot.json");
        var relation = JsonNode.Parse(File.ReadAllText(_collection))!["relations"]!.AsArray()[0]!;
        _relation = (string)relation["id"]!;
        _source = (string)relation["sourceIds"]!.AsArray()[0]!;
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    private string Sources => Path.Combine(_folder, "sources");

    private string Quotes(params (string Record, string Source, string Quote)[] quotes)
    {
        var file = Path.Combine(_folder, "quotes.json");
        File.WriteAllText(file, new JsonObject
        {
            ["formatVersion"] = QuoteCheck.FormatVersion,
            ["quotes"] = new JsonArray([.. quotes.Select(quote => (JsonNode)new JsonObject { ["record"] = quote.Record, ["sourceId"] = quote.Source, ["quote"] = quote.Quote })]),
        }.ToJsonString());
        return file;
    }

    private void Copy(string text) => File.WriteAllText(Path.Combine(Sources, _source + ".txt"), text);

    [Fact]
    public void A_quote_that_stands_in_its_source_is_found()
    {
        Copy("Test data. The hero rode out at dawn, and nobody saw him return.");
        var report = QuoteCheck.Compare(_collection, Quotes((_relation, _source, "rode out at dawn")), Sources);

        Assert.True(report.InOrder);
        Assert.Equal((1, 1), (report.Compared, report.Found));
        Assert.True(report.WithoutQuote > 0, "Every other statement of the collection has no quote.");
    }

    [Fact]
    public void A_quote_that_is_not_in_its_source_is_named_with_its_words()
    {
        Copy("Test data. The hero rode out at dawn.");
        var report = QuoteCheck.Compare(_collection, Quotes((_relation, _source, "rode out at dusk")), Sources);

        Assert.False(report.InOrder);
        Assert.Equal((1, 0), (report.Compared, report.Found));
        Assert.Equal($"quote 1 ({_relation}): not found in {_source}: \"rode out at dusk\"", Assert.Single(report.Problems));
    }

    [Theory]
    [InlineData("The  hero “rode   out” — at\r\n dawn", "the hero \"rode out\" - at dawn")]
    [InlineData("کتابِ شاهان، مي‌رود", "كتاب شاهان، میرود")]
    public void What_differs_only_in_how_a_page_sets_its_letters_is_the_same(string copy, string quote)
    {
        Copy("Test data. " + copy + ".");
        var report = QuoteCheck.Compare(_collection, Quotes((_relation, _source, quote)), Sources);

        Assert.True(report.InOrder, string.Join('\n', report.Problems));
    }

    [Fact]
    public void A_source_without_a_copy_is_named_and_its_quotes_are_not_compared()
    {
        var report = QuoteCheck.Compare(_collection, Quotes((_relation, _source, "anything")), Sources);

        Assert.False(report.InOrder);
        Assert.Equal(0, report.Compared);
        Assert.Equal([_source], report.WithoutCopy);
        Assert.Empty(report.Problems);
    }

    [Fact]
    public void A_record_or_a_source_the_collection_does_not_have_and_a_source_a_record_does_not_cite_are_problems()
    {
        Copy("Test data.");
        var other = JsonNode.Parse(File.ReadAllText(_collection))!["sources"]!.AsArray().Select(source => (string)source!["id"]!).First(id => id != _source);
        var report = QuoteCheck.Compare(_collection, Quotes(("relation-nobody", _source, "x"), (_relation, "src-nobody", "x"), (_relation, other, "x"), (_relation, _source, " ")), Sources);

        Assert.Equal(
            [
                "quote 1: the collection has no record \"relation-nobody\".",
                $"quote 2 ({_relation}): the collection has no source \"src-nobody\".",
                $"quote 3: {_relation} does not cite {other}.",
                "quote 4: record, sourceId and quote are all needed.",
            ],
            report.Problems);
    }

    [Fact]
    public void The_command_says_what_it_found_and_answers_with_its_exit_code()
    {
        Copy("Test data. The hero rode out at dawn.");
        using var good = new StringWriter();
        Assert.Equal(0, ContentCommands.Run(["quotes", _collection, "--quotes", Quotes((_relation, _source, "rode out at dawn")), "--sources", Sources], good, new DateOnly(2026, 11, 26)));
        Assert.Contains("1 of 1 quote(s) stand in their source.", good.ToString(), StringComparison.Ordinal);

        using var bad = new StringWriter();
        Assert.Equal(1, ContentCommands.Run(["quotes", _collection, "--quotes", Quotes((_relation, _source, "rode out at dusk")), "--sources", Sources, "--json"], bad, new DateOnly(2026, 11, 26)));
        var answer = JsonNode.Parse(bad.ToString())!;
        Assert.False((bool)answer["inOrder"]!);
        Assert.Single(answer["problems"]!.AsArray());

        using var nowhere = new StringWriter();
        Assert.Equal(1, ContentCommands.Run(["quotes", _collection, "--quotes", Quotes(), "--sources", Path.Combine(_folder, "nothing")], nowhere, new DateOnly(2026, 11, 26)));
    }
}
