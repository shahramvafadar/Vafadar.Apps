using System.Text.Json;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Versioned daily precision and immutable original evidence, without a provider or financial writer.</summary>
[Trait("AT", "AT-299")]
public sealed class EcbReferenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 13, 0, 0, TimeSpan.Zero);
    private static MarketQuote Quote()
    {
        var symbol = EcbReference.Catalog.Symbols.Single(s => s.Id == "USD-EUR"); var digest = new string('a', 64);
        return new(2, new("ecb", EcbReference.Revision, symbol.Id, new(2026, 10, 19)), EcbReference.Catalog with { Symbols = [symbol] }, symbol,
            new(EcbReference.QuoteId(new(2026, 10, 16), symbol, digest), new(2026, 10, 16), null, 0.8m)
            { Daily = new(1, digest, Now, Now.AddMinutes(-5), 1.25m, 1m, EcbReference.Division) }, Now, Now.AddMinutes(5), Now.AddDays(7));
    }
    [Theory]
    [InlineData("value")][InlineData("leg")][InlineData("date")][InlineData("time")][InlineData("fetched")]
    [InlineData("formula")][InlineData("hash")][InlineData("version")][InlineData("policy")]
    public void Daily_packet_refuses_changed_evidence_or_a_fabricated_fixing_timestamp(string change)
    {
        var quote = Quote(); var observation = quote.Observation; var daily = observation.Daily!;
        var changed = change switch
        {
            "value" => quote with { Observation = observation with { Value = 0.81m } },
            "leg" => quote with { Observation = observation with { Daily = daily with { BasePerEuro = 2m } } },
            "date" => quote with { Observation = observation with { Date = new(2026, 10, 15) } },
            "time" => quote with { Observation = observation with { ObservedAt = Now } },
            "fetched" => quote with { Observation = observation with { Daily = daily with { FetchedAt = Now.AddDays(1) } } },
            "formula" => quote with { Observation = observation with { Daily = daily with { Calculation = EcbReference.Direct } } },
            "hash" => quote with { Observation = observation with { Daily = daily with { SourceSha256 = new string('z', 64) } } },
            "version" => quote with { Version = 1 },
            _ => quote with { Catalog = quote.Catalog with { Attribution = "Incorrect replacement" } },
        };
        MarketReference.Validate(quote, Now); Assert.Throws<ArgumentException>(() => MarketReference.Validate(changed, Now));
    }

    [Fact]
    public void Original_version_one_observation_bytes_have_no_added_field_and_keep_original_date_semantics()
    {
        var original = new MarketObservation("original", new(2026, 10, 16), new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero), 1.25m);
        const string expected = "{\"QuoteId\":\"original\",\"Date\":\"2026-10-16\",\"ObservedAt\":\"2026-10-16T12:00:00+00:00\",\"Value\":1.25}";
        Assert.Equal(expected, JsonSerializer.Serialize(original, MarketJson.Default.MarketObservation));
        Assert.Equal(original, JsonSerializer.Deserialize(expected, MarketJson.Default.MarketObservation));
    }
}
