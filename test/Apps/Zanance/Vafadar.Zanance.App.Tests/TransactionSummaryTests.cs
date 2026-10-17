using System.Globalization;
using System.Resources;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-50: filtered summaries distinguish no financial result from a real zero result.</summary>
[Trait("AT", "AT-50")]
public sealed class TransactionSummaryTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public void Non_result_movements_show_only_the_count_in_each_supported_language(string language)
    {
        var translator = Texts(language);
        var account = new Account { Name = "Fictitious EUR account", CurrencyCode = "EUR" };
        Dictionary<Guid, Account> accounts = new() { [account.Id] = account };
        foreach (var kind in new[] { EntryKind.Transfer, EntryKind.Adjustment, EntryKind.AssetPurchase, EntryKind.AssetSale })
        {
            LedgerEntry[] entries = [new() { AccountId = account.Id, Kind = kind, Amount = 10000 }];
            Assert.Empty(EntrySearch.NetByCurrency(entries, accounts));
            Assert.Equal(translator.Format("Report_TagCount", 1), TransactionSummary.Format(entries, accounts, translator, translator.Culture));
            Assert.Equal(10000, entries[0].Amount);
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public void Equal_income_and_expense_keep_the_real_zero_currency_total(string language)
    {
        var translator = Texts(language);
        var account = new Account { Name = "Fictitious EUR account", CurrencyCode = "EUR" };
        Dictionary<Guid, Account> accounts = new() { [account.Id] = account };
        LedgerEntry[] entries = [new() { AccountId = account.Id, Kind = EntryKind.Income, Amount = 2500 },
            new() { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 2500 }];
        Assert.Equal(0, EntrySearch.NetByCurrency(entries, accounts)["EUR"]);
        Assert.Equal(translator.Format("Tx_FilterTotal", 2, MoneyText.Format(0, "EUR", translator.Culture, showPlus: true)),
            TransactionSummary.Format(entries, accounts, translator, translator.Culture));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public void Mixed_movements_preserve_separate_currency_packets_and_the_full_count(string language)
    {
        var translator = Texts(language);
        var eur = new Account { Name = "Fictitious EUR account", CurrencyCode = "EUR" };
        var usd = new Account { Name = "Fictitious USD account", CurrencyCode = "USD" };
        Dictionary<Guid, Account> accounts = new() { [eur.Id] = eur, [usd.Id] = usd };
        LedgerEntry[] entries = [new() { AccountId = eur.Id, Kind = EntryKind.Transfer, Amount = 10000 },
            new() { AccountId = eur.Id, Kind = EntryKind.Expense, Amount = 2500 },
            new() { AccountId = eur.Id, Kind = EntryKind.Refund, Amount = 500 },
            new() { AccountId = usd.Id, Kind = EntryKind.Income, Amount = 3000 }];
        var packets = MoneyText.Format(-2000, "EUR", translator.Culture, showPlus: true) + "  "
            + MoneyText.Format(3000, "USD", translator.Culture, showPlus: true);
        Assert.Equal(translator.Format("Tx_FilterTotal", 4, packets),
            TransactionSummary.Format(entries, accounts, translator, translator.Culture));
    }

    private static Translator Texts(string language)
    {
        var translator = new Translator();
        translator.AddResources(new ResourceManager("Vafadar.Zanance.App.Tests.Resources.AppResources", typeof(TransactionSummaryTests).Assembly));
        translator.SetCulture(CultureInfo.GetCultureInfo(language));
        Assert.True(translator.TryGetString("Report_TagCount", out _));
        return translator;
    }
}
