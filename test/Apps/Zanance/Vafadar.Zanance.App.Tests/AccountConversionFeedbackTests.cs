using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Current original conversion feedback follows display language without replaying any accepted write or dialog.</summary>
public sealed class AccountConversionFeedbackTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(Account Account, HoldingStore Store, AccountConversionSession Session)> CreateAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original conversion feedback", Type = AccountType.Asset, CurrencyCode = "EUR",
            OpeningBalance = 10000, OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var store = f.Services.GetRequiredService<HoldingStore>();
        var session = new AccountConversionSession(store, f.Translator, f.Localization, f.Time, f.Platform);
        session.SetOwner(account.Id, true); return (account, store, session);
    }

    [Fact]
    public async Task The_same_accepted_receipt_retranslates_without_another_confirmation_route_or_write()
    {
        using var f = new FlowFixture(); var (account, store, session) = await CreateAsync(f);
        f.Platform.PendingNavigation = () => Task.FromException(new IOException("Owned post-commit route failure."));
        f.Platform.Confirmations.Enqueue(true);
        var id = await session.ConvertAsync(account.Id, () => true); Assert.NotNull(id);
        var receipt = session.AcceptedOutcome;
        foreach (var language in new[] { "de", "fa", "en" })
        {
            f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
            session.RefreshFeedback();
            Assert.Equal(f.Translator["Account_Converted"], session.Result);
            Assert.Equal(f.Translator["Account_ConvertedFollowUpFailed"], session.Error);
            Assert.Same(receipt, session.AcceptedOutcome);
        }
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Single(f.Platform.Routes);
        Assert.Equal(id, Assert.Single(await store.GetTypesAsync(Ct)).Id);
        Assert.Single(await store.GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_prewrite_failure_retranslates_without_reopening_the_original_consent()
    {
        using var f = new FlowFixture(); var (account, store, session) = await CreateAsync(f);
        f.Platform.PendingConfirmation = () => Task.FromException<bool>(new IOException("Owned confirmation adapter failure."));
        Assert.Null(await session.ConvertAsync(account.Id, () => true));
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "fa"));
        session.RefreshFeedback(); Assert.Equal(f.Translator["Account_ConversionReviewAgain"], session.Error);
        Assert.Null(session.Result); Assert.Null(session.AcceptedOutcome); Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Empty(f.Platform.Routes); Assert.Empty(await store.GetTypesAsync(Ct));
        Assert.False(Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).IsArchived);
    }
}
