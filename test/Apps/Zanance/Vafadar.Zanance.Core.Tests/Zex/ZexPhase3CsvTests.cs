using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>The holdings file and the format version of CSV files (ZEX-S0409, AT34).</summary>
public sealed class ZexPhase3CsvTests
{
    private static readonly DateOnly Day = new(2026, 10, 3);

    [Fact]
    public void Holdings_round_trip_keeps_quantities_units_locations_and_prices()
    {
        var (types, locations, events, valuations) = Sample();

        var text = HoldingsCsv.Write(types, locations, events, valuations, includeNotes: true);
        var content = HoldingsCsv.Read(Csv.Read(text, ','), [], []);

        Assert.Empty(content.Errors);
        var gold = Assert.Single(content.Types, t => t.Name == "18k gold");
        Assert.Equal(7500, gold.PurityPer10000);
        Assert.Equal(Metal.Gold, gold.Metal);
        var coin = Assert.Single(content.Types, t => t.Dimension == AssetDimension.Count);
        Assert.Equal(8_133, coin.UnitWeightMg);
        Assert.Equal("coin", coin.CountUnitName);
        Assert.Equal(2, content.Locations.Count);
        Assert.Equal(events.Select(e => (e.Id, e.Kind, e.Quantity, e.LocationId, e.ToLocationId, e.BasisAmount, e.ProceedsAmount, e.Date)),
            content.Events.Select(e => (e.Id, e.Kind, e.Quantity, e.LocationId, e.ToLocationId, e.BasisAmount, e.ProceedsAmount, e.Date)));
        var price = Assert.Single(content.Valuations);
        Assert.Equal(valuations[0].PricePerUnitMilli, price.PricePerUnitMilli);
        Assert.Equal(HoldingsLedger.Quantity(events, gold.Id, Day), HoldingsLedger.Quantity(content.Events, gold.Id, Day));
    }

    [Fact]
    public void A_holdings_file_is_recognised_by_its_header_and_carries_the_version()
    {
        var (types, locations, events, valuations) = Sample();
        var rows = Csv.Read(HoldingsCsv.Write(types, locations, events, valuations, includeNotes: false), ',');

        Assert.True(HoldingsCsv.IsHoldingsFormat(rows[0]));
        Assert.False(CsvImport.IsOwnFormat(rows[0]));
        Assert.All(rows.Skip(1), r => Assert.Equal(HoldingsCsv.FormatVersion, r[^1]));
    }

    [Fact]
    public void Rows_that_refer_to_unknown_types_or_bad_quantities_are_reported_not_guessed()
    {
        var header = string.Join(',', HoldingsCsv.Columns);
        var unknownType = $"event,{Guid.NewGuid()},{Guid.NewGuid()},,Opening,,,,,,,EUR,{Guid.NewGuid()},,2026-10-01,1.000";
        var content = HoldingsCsv.Read(Csv.Read(header + "\r\n" + unknownType + "\r\nsomething,else\r\n", ','), [], []);

        Assert.Equal(["Type", "Record"], content.Errors.Select(e => e.Error));
        Assert.Empty(content.Events);
    }

    [Fact]
    public void The_entries_export_carries_the_format_version_and_older_files_still_import()
    {
        var cash = new Account { Name = "Cash", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
        var entry = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Amount = 1_000_00, Date = Day, Title = "18k gold" };
        var rows = Csv.Read(CsvExport.Write([entry], new Dictionary<Guid, Account> { [cash.Id] = cash }, _ => string.Empty, includeNotes: true), ',');

        Assert.Equal("format_version", rows[0][^1]);
        Assert.Equal(HoldingsCsv.FormatVersion, rows[1][^1]);

        // An export of an earlier version has fewer columns and imports unchanged.
        var older = rows.Select(r => (IReadOnlyList<string>)r.Take(CsvExport.BaseColumns.Count).ToList()).ToList();
        var preview = CsvImport.PreviewOwn(older, [cash], [], _ => string.Empty, []);
        var imported = Assert.Single(preview).Entry!;
        Assert.Equal(EntryKind.AssetPurchase, imported.Kind);
        Assert.Equal(1_000_00, imported.Amount);
    }

    private static (List<AssetType> Types, List<AssetLocation> Locations, List<AssetEvent> Events, List<AssetValuation> Valuations) Sample()
    {
        var gold = new AssetType { Name = "18k gold", PriceCurrencyCode = "EUR", Metal = Metal.Gold, PurityPer10000 = 7500 };
        var coin = new AssetType { Name = "Bahar Azadi", Kind = AssetKind.CoinOrBar, Dimension = AssetDimension.Count, Metal = Metal.Gold, PurityPer10000 = 9000, UnitWeightMg = 8_133, CountUnitName = "coin", PriceCurrencyCode = "EUR" };
        var safe = new AssetLocation { Name = "Home safe" };
        var bank = new AssetLocation { Name = "=Bank box" };
        var events = new List<AssetEvent>
        {
            new() { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Opening, Quantity = 20_000, Date = Day.AddDays(-30) },
            new() { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Purchase, Quantity = 10_500, BasisAmount = 1_050_00, GroupId = Guid.NewGuid(), Date = Day.AddDays(-10) },
            new() { AssetTypeId = gold.Id, LocationId = safe.Id, ToLocationId = bank.Id, Kind = AssetEventKind.LocationTransfer, Quantity = 5_000, Date = Day.AddDays(-5) },
            new() { AssetTypeId = gold.Id, LocationId = bank.Id, Kind = AssetEventKind.Sale, Quantity = 2_000, ProceedsAmount = 240_00, Date = Day },
            new() { AssetTypeId = coin.Id, LocationId = safe.Id, Kind = AssetEventKind.GiftReceived, Quantity = 3_000, Date = Day, Note = "From grandma" },
        };
        var valuations = new List<AssetValuation> { new() { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = Day, PricePerUnitMilli = 123_456 } };
        return ([gold, coin], [safe, bank], events, valuations);
    }
}
