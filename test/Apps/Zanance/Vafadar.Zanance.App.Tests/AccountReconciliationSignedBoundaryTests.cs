using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>The original observed-balance session retains the full signed range without relaxing positive amounts.</summary>
public sealed class AccountReconciliationSignedBoundaryTests
{
    [Theory]
    [InlineData("en-US", "EUR", "-", false)]
    [InlineData("de-DE", "EUR", "−", false)]
    [InlineData("fa-IR", "EUR", "−", false)]
    [InlineData("en-US", "JPY", "-", false)]
    [InlineData("en-US", "KWD", "-", false)]
    [InlineData("en-US", "EUR", "−", true)]
    public async Task Lowest_observed_balance_keeps_original_unit_and_review_without_writing(string cultureName, string currency, string sign, bool useUnit)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set(useUnit ? [new(currency, "Fictitious hundreds", 2)] : []);
            using var fixture = new FlowFixture();
            var day = new DateOnly(2026, 10, 1);
            var account = new Account { Name = "Fictitious signed comparison", CurrencyCode = currency,
                OpeningBalance = long.MinValue + 1, OpeningDate = day, OpeningBalanceKnown = true };
            Assert.True(await fixture.Store.SaveAccountAsync(account, TestContext.Current.CancellationToken));
            var context = MoneyInputContext.Capture(currency, CultureInfo.GetCultureInfo(cultureName));
            var raw = sign + context.ForInput(long.MinValue);
            if (cultureName == "fa-IR") raw = string.Concat(raw.Select(c => c is >= '0' and <= '9' ? (char)('۰' + c - '0') : c));
            var before = JsonSerializer.Serialize(await fixture.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken));
            var session = new AccountReconciliationSession(fixture.Store);
            session.ObservedMoney.ChangeText(raw, currency, context.Culture);
            fixture.Localization.SetFormattingCulture(cultureName == "de-DE" ? "en-US" : "de-DE");

            var review = await session.CompareObservedAsync(account.Id, currency, day);

            Assert.NotNull(review);
            Assert.Equal(long.MinValue, review.Result.Observed);
            Assert.Equal(long.MinValue + 1, review.Result.Recorded);
            Assert.Equal(-1, review.Result.Difference);
            Assert.Equal(raw, session.ObservedMoney.Text);
            Assert.False(session.Committed);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(before, JsonSerializer.Serialize(await fixture.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken)));
        }
        finally { DisplayUnits.Set(units); }
    }
}
