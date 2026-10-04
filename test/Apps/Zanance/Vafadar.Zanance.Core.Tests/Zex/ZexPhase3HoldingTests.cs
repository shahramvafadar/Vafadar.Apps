using System.Globalization;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>Quantity holdings of enhancement ZEX phase 3 (golden examples G08–G11; ZEX-AT09..AT17, AT38, AT39).</summary>
public sealed class ZexPhase3HoldingTests
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");
    private static readonly DateOnly Day = new(2026, 10, 3);
    private readonly Guid _safe = Guid.CreateVersion7();
    private readonly Guid _bank = Guid.CreateVersion7();

    [Fact]
    public void Kilograms_and_grams_are_exact_milligrams()
    {
        var gold = Gold18();

        Assert.True(Quantities.TryParse("0.030", QuantityUnit.Kilogram, gold, En, out var kg));
        Assert.Equal(30_000, kg);
        Assert.True(Quantities.TryParse("20", QuantityUnit.Gram, gold, En, out var g));
        Assert.Equal("50.000 g", Quantities.Format(kg + g, gold, En));
        Assert.False(Quantities.TryParse("1.0001", QuantityUnit.Gram, gold, En, out _));
        Assert.False(Quantities.TryParse("-2", QuantityUnit.Gram, gold, En, out _));
    }

    [Fact]
    public void An_indivisible_coin_refuses_fractions()
    {
        var coin = Coin();

        Assert.False(Quantities.TryParse("1.5", QuantityUnit.Piece, coin, En, out _));
        Assert.True(Quantities.TryParse("3", QuantityUnit.Piece, coin, En, out var three));
        Assert.Equal("3 coins", Quantities.Format(three, coin, En, countName: "coins"));
    }

    [Theory]
    [InlineData("24", 9999)]
    [InlineData("22k", 9167)]
    [InlineData("21 k", 8750)]
    [InlineData("18", 7500)]
    [InlineData("750", 7500)]
    [InlineData("999.9", 9999)]
    public void Purity_is_read_as_karat_or_fineness(string text, int expected)
    {
        Assert.True(Purity.TryParse(text, En, out var purity));
        Assert.Equal(expected, purity);
    }

    [Fact]
    public void G08_quantities_of_one_type_add_up()
    {
        var gold = Gold18();
        var events = new[] { Opening(gold, 20_000), Purchase(gold, 30_000, 3_000_00) };

        Assert.Equal(50_000, HoldingsLedger.Quantity(events, gold.Id, Day));
    }

    [Fact]
    public void G09_different_purities_stay_two_lines_with_one_fine_gold_equivalent()
    {
        var gold18 = Gold18();
        var gold24 = new AssetType { Name = "24k gold", PriceCurrencyCode = "EUR", Metal = Metal.Gold, PurityPer10000 = 9999 };
        var events = new[] { Opening(gold18, 20_000), Opening(gold24, 20_000) };

        var values = AssetValuationService.Values([gold18, gold24], events, [], Day);
        var fine = Assert.Single(AssetValuationService.FineMetals(values));

        Assert.Equal(2, values.Count);
        Assert.Equal((Metal.Gold, 34_998L, 2), (fine.Metal, fine.FineMassMg, fine.TypeCount));
    }

    [Fact]
    public void G10_coins_with_a_unit_weight_derive_their_weight_once()
    {
        var coin = Coin();
        coin.UnitWeightMg = 10_000;

        Assert.Equal(30_000, Quantities.MassMg(3_000, coin));
        var events = new[] { Opening(coin, 3_000) };
        var value = Assert.Single(AssetValuationService.Values([coin], events, [Price(coin, 400_00 * 1_000)], Day));
        Assert.Equal(1_200_00, value.Value);
    }

    [Fact]
    public void G11_a_purchase_is_capital_and_only_the_fee_is_spending()
    {
        var ledger = new LedgerBuilder();
        var cash = ledger.Account("Cash", 2_000m, Accounts.AccountType.Cash);
        var group = Guid.CreateVersion7();
        ledger.Entries.Add(new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Amount = 1_000_00, Date = LedgerBuilder.Day1, GroupId = group });
        ledger.Entries.Add(new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Amount = 20_00, Date = LedgerBuilder.Day1, GroupId = group });

        Assert.Equal(980_00, ledger.Balance(cash));
        var totals = ledger.Totals();
        Assert.Equal((0L, 20_00L), (totals.NetIncome, totals.NetExpense));
        Assert.Equal(EntryClass.Capital, EntryClassification.Of(EntryKind.AssetPurchase));
        Assert.Equal(EntryClass.Capital, EntryClassification.Of(EntryKind.AssetSale));
    }

    [Fact]
    public void Sale_proceeds_are_never_income()
    {
        var ledger = new LedgerBuilder();
        var cash = ledger.Account("Cash", 0m, Accounts.AccountType.Cash);
        ledger.Entries.Add(new LedgerEntry { Kind = EntryKind.AssetSale, AccountId = cash.Id, Amount = 600_00, Date = LedgerBuilder.Day1 });

        Assert.Equal(600_00, ledger.Balance(cash));
        Assert.Equal(0, ledger.Totals().NetIncome);
    }

    [Fact]
    public void A_back_dated_sale_beyond_the_holding_of_that_day_is_refused()
    {
        var gold = Gold18();
        var events = new List<AssetEvent>
        {
            Opening(gold, 20_000, new DateOnly(2026, 8, 1)),
            Purchase(gold, 30_000, 3_000_00, new DateOnly(2026, 9, 15)),
            Sale(gold, 30_000, 3_300_00, new DateOnly(2026, 9, 1)),
        };

        var conflict = HoldingsLedger.FindConflict(events);

        Assert.NotNull(conflict);
        Assert.Equal((new DateOnly(2026, 9, 1), 20_000L, _safe), (conflict.Date, conflict.Available, conflict.LocationId));
        events[2].Date = new DateOnly(2026, 9, 20);
        Assert.Null(HoldingsLedger.FindConflict(events));
    }

    [Fact]
    public void A_location_transfer_keeps_the_total_and_moves_the_quantity()
    {
        var gold = Gold18();
        var events = new[]
        {
            Opening(gold, 20_000),
            new AssetEvent { AssetTypeId = gold.Id, Kind = AssetEventKind.LocationTransfer, LocationId = _safe, ToLocationId = _bank, Quantity = 5_000, Date = Day },
        };

        Assert.Equal(20_000, HoldingsLedger.Quantity(events, gold.Id, Day));
        var positions = HoldingsLedger.Positions(events, Day);
        Assert.Equal(15_000, positions.Single(p => p.LocationId == _safe).Quantity);
        Assert.Equal(5_000, positions.Single(p => p.LocationId == _bank).Quantity);
        Assert.Equal(0, HoldingsLedger.Basis(events, gold.Id).Sales.Count);
    }

    [Fact]
    public void Average_cost_with_an_unknown_opening_shows_no_result_until_the_price_is_entered()
    {
        var gold = Gold18();
        var opening = Opening(gold, 10_000, new DateOnly(2026, 8, 1));
        var events = new[] { opening, Purchase(gold, 10_000, 1_000_00, new DateOnly(2026, 9, 1)), Sale(gold, 5_000, 600_00, Day) };

        var unknown = HoldingsLedger.Basis(events, gold.Id);
        Assert.Null(Assert.Single(unknown.Sales).Result);
        Assert.Null(unknown.Basis);

        opening.BasisAmount = 900_00;
        var known = HoldingsLedger.Basis(events, gold.Id);
        Assert.Equal(95_00 * 1_000L, 1_900_00 * 1_000L * 1_000 / 20_000);
        Assert.Equal((475_00L, 125_00L), (known.Sales.Single().RemovedBasis!.Value, known.Sales.Single().Result!.Value));
        Assert.Equal(1_425_00, known.Basis);
    }

    [Fact]
    public void A_new_price_changes_the_value_today_but_never_the_past()
    {
        var gold = Gold18();
        var events = new[] { Opening(gold, 10_000, new DateOnly(2026, 8, 1)) };
        var valuations = new[] { Price(gold, 100_00 * 1_000, new DateOnly(2026, 9, 1)), Price(gold, 110_00 * 1_000, Day) };

        Assert.Equal(1_100_00, AssetValuationService.Values([gold], events, valuations, Day).Single().Value);
        Assert.Equal(1_000_00, AssetValuationService.Values([gold], events, valuations, Day.AddDays(-1)).Single().Value);
        Assert.Null(AssetValuationService.Values([gold], events, valuations, new DateOnly(2026, 8, 15)).Single().Value);
        Assert.Equal(10_000, HoldingsLedger.Quantity(events, gold.Id, Day));
    }

    [Fact]
    public void A_total_value_becomes_a_price_per_gram()
    {
        Assert.Equal(55_00 * 1_000, AssetValuationService.PricePerUnitMilliOf(1_100_00, 20_000));
        Assert.Equal(1_100_00, AssetValuationService.ValueOf(20_000, 55_00 * 1_000));
    }

    private static AssetType Gold18() => new() { Name = "18k gold", PriceCurrencyCode = "EUR", Metal = Metal.Gold, PurityPer10000 = 7500 };

    private static AssetType Coin() => new() { Name = "Coin", PriceCurrencyCode = "EUR", Kind = AssetKind.CoinOrBar, Dimension = AssetDimension.Count, Metal = Metal.Gold };

    private AssetEvent Opening(AssetType type, long quantity, DateOnly? date = null) =>
        new() { AssetTypeId = type.Id, LocationId = _safe, Kind = AssetEventKind.Opening, Quantity = quantity, Date = date ?? Day };

    private AssetEvent Purchase(AssetType type, long quantity, long basis, DateOnly? date = null) =>
        new() { AssetTypeId = type.Id, LocationId = _safe, Kind = AssetEventKind.Purchase, Quantity = quantity, BasisAmount = basis, Date = date ?? Day };

    private AssetEvent Sale(AssetType type, long quantity, long proceeds, DateOnly date) =>
        new() { AssetTypeId = type.Id, LocationId = _safe, Kind = AssetEventKind.Sale, Quantity = quantity, ProceedsAmount = proceeds, Date = date };

    private static AssetValuation Price(AssetType type, long perUnitMilli, DateOnly? date = null) =>
        new() { AssetTypeId = type.Id, PricePerUnitMilli = perUnitMilli, CurrencyCode = type.PriceCurrencyCode, Date = date ?? Day };
}
