using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Forecasts;

/// <summary>Prevents legacy incomplete transfers from inventing a one-to-one currency conversion.</summary>
public sealed class ForecastMissingRateTests
{
    private static readonly DateOnly Today = new(2026, 10, 23);

    [Fact, Trait("AT", "AT-43")]
    public void A_missing_destination_amount_marks_only_the_other_currency_incomplete()
    {
        var (source, destination, plan) = Transfer();
        var result = Compute(source, destination, plan);
        var outgoing = Assert.Single(result, item => item.CurrencyCode == "EUR");
        Assert.Equal(9000, outgoing.EndBalance);
        Assert.False(outgoing.IsIncomplete);
        var incoming = Assert.Single(result, item => item.CurrencyCode == "USD");
        Assert.Equal(5000, incoming.EndBalance);
        Assert.True(incoming.IsIncomplete);
        Assert.Equal(1, incoming.UnknownCount);
        Assert.Null(Assert.Single(incoming.Items).Effect);
        Assert.Null(plan.ToAmount);
        Assert.Equal(1000, plan.Amount);
    }

    [Fact, Trait("AT", "AT-43")]
    public void An_out_of_scope_source_cannot_supply_a_guessed_destination_currency_amount()
    {
        var (source, destination, plan) = Transfer(); source.IncludeInTotals = false;
        var incoming = Assert.Single(Compute(source, destination, plan));
        Assert.Equal("USD", incoming.CurrencyCode);
        Assert.Equal(5000, incoming.EndBalance);
        Assert.Null(Assert.Single(incoming.Items).Effect);
        Assert.True(incoming.IsIncomplete);
    }

    [Fact, Trait("AT", "AT-43")]
    public void A_same_currency_transfer_without_a_second_amount_still_cancels_inside_the_scope()
    {
        var (source, destination, plan) = Transfer(); destination.CurrencyCode = "EUR";
        var result = Assert.Single(Compute(source, destination, plan));
        Assert.Equal(15000, result.EndBalance);
        Assert.Empty(result.Items);
        Assert.False(result.IsIncomplete);
    }

    [Fact, Trait("AT", "AT-43")]
    public void A_same_currency_incoming_transfer_keeps_its_known_amount_when_the_source_is_outside_scope()
    {
        var (source, destination, plan) = Transfer(); destination.CurrencyCode = "EUR"; source.IncludeInTotals = false;
        var incoming = Assert.Single(Compute(source, destination, plan));
        Assert.Equal(6000, incoming.EndBalance);
        Assert.Equal(1000, Assert.Single(incoming.Items).Effect);
        Assert.False(incoming.IsIncomplete);
    }

    [Fact, Trait("AT", "AT-43")]
    public void An_explicit_cross_currency_destination_amount_remains_known()
    {
        var (source, destination, plan) = Transfer(); source.IncludeInTotals = false; plan.ToAmount = 1234;
        var incoming = Assert.Single(Compute(source, destination, plan));
        Assert.Equal(6234, incoming.EndBalance);
        Assert.Equal(1234, Assert.Single(incoming.Items).Effect);
        Assert.False(incoming.IsIncomplete);
    }

    [Fact, Trait("AT", "AT-43")]
    public void Assuming_a_source_amount_does_not_invent_the_missing_transfer_rate()
    {
        var (source, destination, plan) = Transfer(); var scenario = new ForecastScenario();
        scenario.SetAmount(plan.Id, plan.Rule.Start, 2000);
        var result = Compute(source, destination, plan, scenario);
        Assert.Equal(8000, Assert.Single(result, item => item.CurrencyCode == "EUR").EndBalance);
        var incoming = Assert.Single(result, item => item.CurrencyCode == "USD");
        Assert.Equal(5000, incoming.EndBalance);
        Assert.Null(Assert.Single(incoming.Items).Effect);
        var saved = ForecastSnapshot.From("Incomplete destination", incoming, [destination.Id], Today, null, "0.1.0");
        Assert.Equal(1, saved.UnknownCount);
        Assert.All(saved.Points(), point => Assert.Equal(5000, point.Balance));
        Assert.Null(plan.ToAmount);
    }

    [Fact, Trait("AT", "AT-43")]
    public void Explicitly_excluding_the_incomplete_transfer_removes_its_warning_without_posting_money()
    {
        var (source, destination, plan) = Transfer(); var scenario = new ForecastScenario();
        scenario.SetExcluded(plan.Id, plan.Rule.Start, true);
        var result = Compute(source, destination, plan, scenario);
        Assert.All(result, item => { Assert.False(item.IsIncomplete); Assert.True(Assert.Single(item.Items).IsExcluded); });
        Assert.Equal(10000, Assert.Single(result, item => item.CurrencyCode == "EUR").EndBalance);
        Assert.Equal(5000, Assert.Single(result, item => item.CurrencyCode == "USD").EndBalance);
        Assert.Null(plan.ToAmount);
    }

    [Fact, Trait("AT", "AT-43")]
    public void A_future_legacy_entry_with_no_destination_amount_is_not_a_known_one_to_one_transfer()
    {
        var (source, destination, _) = Transfer();
        var entry = new LedgerEntry { AccountId=source.Id, ToAccountId=destination.Id, Kind=EntryKind.Transfer,
            Amount=1000, ToAmount=null, Date=Today.AddDays(1), Title="Legacy incomplete transfer" };
        var result = ForecastCalculator.Compute([source, destination], [entry], [], [], Today, Today.AddDays(2));
        var incoming = Assert.Single(result, item => item.CurrencyCode == "USD");
        Assert.Equal(5000, incoming.EndBalance);
        Assert.True(incoming.IsIncomplete);
        Assert.Equal(ForecastSource.FutureEntry, Assert.Single(incoming.Items).Source);
        Assert.Null(incoming.Items[0].Effect);
        Assert.Equal(9000, Assert.Single(result, item => item.CurrencyCode == "EUR").EndBalance);
    }

    private static (Account Source, Account Destination, Schedule Plan) Transfer()
    {
        var source = new Account { Name="Original EUR", CurrencyCode="EUR", OpeningBalance=10000, OpeningDate=Today };
        var destination = new Account { Name="Original USD", CurrencyCode="USD", OpeningBalance=5000, OpeningDate=Today };
        var plan = new Schedule { Name="Legacy missing destination", AccountId=source.Id, ToAccountId=destination.Id,
            Kind=EntryKind.Transfer, Amount=1000, ToAmount=null, AmountMode=AmountMode.Fixed,
            Rule=new RecurrenceRule { Frequency=Frequency.Once, Start=Today.AddDays(1) } };
        return (source, destination, plan);
    }

    private static IReadOnlyList<CurrencyForecast> Compute(Account source, Account destination, Schedule plan, ForecastScenario? scenario=null) =>
        ForecastCalculator.Compute([source, destination], [], [plan], [], Today, Today.AddDays(2), scenario:scenario);
}
