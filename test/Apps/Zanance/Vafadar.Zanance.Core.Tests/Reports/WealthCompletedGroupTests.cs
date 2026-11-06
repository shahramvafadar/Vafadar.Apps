using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Complete wealth groups retain exact signed cancellation without weakening public money bounds.</summary>
public sealed class WealthCompletedGroupTests
{
    private static readonly DateOnly Day = new(2026, 10, 9);
    private static readonly int[][] Orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];

    /// <summary>Account enumeration changes the parts' order, never a supported completed currency group.</summary>
    [Theory]
    [InlineData(false, 0)] [InlineData(false, 1)] [InlineData(false, 2)]
    [InlineData(false, 3)] [InlineData(false, 4)] [InlineData(false, 5)]
    [InlineData(true, 0)] [InlineData(true, 1)] [InlineData(true, 2)]
    [InlineData(true, 3)] [InlineData(true, 4)] [InlineData(true, 5)]
    public void Complete_group_is_exact_in_every_original_order(bool negative, int permutation)
    {
        long[] values = negative ? [long.MinValue, -long.MaxValue, long.MaxValue] : [long.MaxValue, long.MaxValue, -long.MaxValue];
        var accounts = Orders[permutation].Select(i => Account(i, values[i])).ToArray();
        var original = Capture(accounts);
        var result = NetWorthCalculator.Compute(accounts, [], [], [], [], Day);
        var total = Assert.Single(result.Totals);
        Assert.Equal(negative ? long.MinValue : long.MaxValue, total.Money);
        Assert.Equal(total.Money, total.Total);
        Assert.Equal(accounts.Select(a => a.Id).ToArray(), result.Parts.Select(p => p.SourceId).ToArray());
        Assert.All(result.Parts, p => Assert.Equal(WealthGroup.Money, p.Group));
        if (negative)
        {
            // Composition deliberately counts positive assets: this negative net still has a supported gross slice.
            var composition = NetWorthCalculator.Compose(result, new RateTable([]), "EUR");
            Assert.Equal(long.MaxValue, composition.Known);
            Assert.Equal(0, composition.Debts);
            Assert.Equal(long.MaxValue, Assert.Single(composition.Slices).Value);
            Assert.Empty(composition.MissingRates);
        }
        else
        {
            // An exact net does not make two MaxValue positive assets a supported public gross composition.
            Assert.Throws<OverflowException>(() => NetWorthCalculator.Compose(result, new RateTable([]), "EUR"));
        }
        Assert.Equal(original, Capture(accounts));
    }

    /// <summary>Another group's cancellation cannot legitimize an out-of-range public Money component.</summary>
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void Unsupported_completed_group_still_refuses(bool negative, bool otherGroup)
    {
        var accounts = new List<Account> { Account(0, negative ? long.MinValue : long.MaxValue), Account(1, negative ? -1 : 1) };
        if (otherGroup) accounts.Add(Account(2, negative ? 1 : -1, negative ? AccountType.Lent : AccountType.Loan));
        var original = Capture([.. accounts]);
        Assert.Throws<OverflowException>(() => NetWorthCalculator.Compute(accounts, [], [], [], [], Day));
        Assert.Equal(original, Capture([.. accounts]));
    }

    /// <summary>Original currency case, future-opening exclusion and archived-zero filtering remain independent.</summary>
    [Fact]
    public void Original_currency_and_date_scope_remain()
    {
        var first = Account(0, long.MaxValue); first.CurrencyCode = "eur";
        var second = Account(1, -long.MaxValue);
        var dollars = Account(2, 1); dollars.CurrencyCode = "USD";
        var future = Account(3, long.MaxValue); future.OpeningDate = Day.AddDays(1);
        var archived = Account(4, 0); archived.IsArchived = true;
        Account[] accounts = [first, second, dollars, future, archived];
        var original = Capture(accounts);
        var result = NetWorthCalculator.Compute(accounts, [], [], [], [], Day);
        Assert.Equal(new[] { "USD", "eur" }, result.Totals.Select(t => t.CurrencyCode).ToArray());
        Assert.Equal(0, result.Totals.Single(t => t.CurrencyCode == "eur").Money);
        Assert.Equal(1, result.Totals.Single(t => t.CurrencyCode == "USD").Money);
        Assert.Equal(new[] { first.Id, second.Id, dollars.Id }, result.Parts.Select(p => p.SourceId).ToArray());
        Assert.Equal(original, Capture(accounts));
    }

    /// <summary>Archival never removes a nonzero balance to manufacture a representable group.</summary>
    [Fact]
    public void Archived_nonzero_member_remains_in_completed_group()
    {
        Account[] accounts = [Account(0, long.MaxValue), Account(1, long.MaxValue), Account(2, -long.MaxValue)];
        accounts[0].IsArchived = true;
        var original = Capture(accounts);
        var result = NetWorthCalculator.Compute(accounts, [], [], [], [], Day);
        Assert.Equal(long.MaxValue, Assert.Single(result.Totals).Money);
        Assert.Equal(3, result.Parts.Count);
        Assert.Equal(original, Capture(accounts));
    }

    private static Account Account(int index, long opening, AccountType type = AccountType.Checking) => new(new Guid(index + 1, 10, 9, [1, 2, 3, 4, 5, 6, 7, 8]))
    { Name = "Original group " + index, CurrencyCode = "EUR", Type = type, OpeningDate = Day, OpeningBalance = opening };
    private static string Capture(Account[] accounts) => JsonSerializer.Serialize(accounts, WealthGroupInputsJson.Default.AccountArray);
}

/// <summary>Original account properties are compared without reflection-only metadata in native callers.</summary>
[JsonSerializable(typeof(Account[]))]
internal sealed partial class WealthGroupInputsJson : JsonSerializerContext;
