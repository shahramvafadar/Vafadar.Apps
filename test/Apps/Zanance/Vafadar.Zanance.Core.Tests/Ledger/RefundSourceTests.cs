using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>AT-409: exact final-source limits for retained linked refunds.</summary>
[Trait("AT", "AT-409")]
public sealed class RefundSourceTests
{
    [Theory]
    [InlineData("valid")][InlineData("amount")][InlineData("currency")][InlineData("kind")][InlineData("overflow")]
    public void Corrected_source_keeps_exact_refund_amount_type_and_receiving_currency(string scenario)
    {
        var sourceAccount = new Account { Name = "Original", CurrencyCode = "EUR" };
        var receiving = new Account { Name = "Receiving", CurrencyCode = scenario == "currency" ? "USD" : "eur" };
        var accounts = new Dictionary<Guid, Account> { [sourceAccount.Id] = sourceAccount, [receiving.Id] = receiving };
        var source = new LedgerEntry { Kind = scenario == "kind" ? EntryKind.Income : EntryKind.Expense,
            AccountId = sourceAccount.Id, Amount = scenario == "overflow" ? long.MaxValue : 100 };
        var refunds = new[]
        {
            new LedgerEntry { Kind = EntryKind.Refund, RefundOfId = source.Id, AccountId = receiving.Id, Amount = scenario == "overflow" ? long.MaxValue : 60 },
            new LedgerEntry { Kind = EntryKind.Refund, RefundOfId = source.Id, AccountId = receiving.Id, Amount = scenario == "overflow" ? 1 : scenario == "amount" ? 50 : 40 },
        };
        var errors = LedgerValidator.ValidateRefundSource(source, refunds, accounts);
        if (scenario == "valid") Assert.Empty(errors);
        else Assert.Contains(scenario switch { "currency" => LedgerError.RefundCurrencyMismatch,
            "kind" => LedgerError.RefundOriginalNotExpense, _ => LedgerError.RefundExceedsPurchase }, errors);
    }
}
