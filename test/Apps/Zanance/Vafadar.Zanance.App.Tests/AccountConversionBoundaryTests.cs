using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original legacy-account conversion must not invent a basis or lose its accepted result after notification.</summary>
public sealed class AccountConversionBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);

    private static async Task<Account> OriginalAssetAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original legacy asset", Type = AccountType.Asset, CurrencyCode = "EUR",
            OpeningBalance = 10000, OpeningDate = Day.AddMonths(-1) };
        await f.Store.SaveAccountAsync(account, Ct);
        return account;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999)]
    [InlineData(-1)]
    public async Task A_caller_authored_balance_cannot_replace_the_original_preview_basis(long supplied)
    {
        using var f = new FlowFixture(); var account = await OriginalAssetAsync(f);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => holdings.ConvertAccountAsync(account.Id, supplied, Day, "Original place", Ct));
        Assert.False(Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).IsArchived);
        Assert.Empty(await holdings.GetTypesAsync(Ct)); Assert.Empty(await holdings.GetLocationsAsync(Ct));
        Assert.Empty(await holdings.GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await holdings.GetValuationsAsync(cancellationToken: Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_notification_failure_retains_the_actual_committed_type_without_another_conversion()
    {
        using var f = new FlowFixture(); var account = await OriginalAssetAsync(f);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        holdings.Changed += (_, _) => throw new InvalidOperationException("Owned post-commit notification failure.");
        var completed = await Assert.ThrowsAsync<CommittedWriteNotificationException<AssetType>>(() =>
            holdings.ConvertAccountAsync(account.Id, 10000, Day, "Original place", Ct));
        var actual = Assert.Single(await holdings.GetTypesAsync(Ct)); Assert.Equal(actual.Id, completed.Result.Id);
        Assert.True(Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).IsArchived);
        Assert.Equal(10000, Assert.Single(await holdings.GetEventsAsync(cancellationToken: Ct)).BasisAmount);
        Assert.Single(await holdings.GetValuationsAsync(cancellationToken: Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
