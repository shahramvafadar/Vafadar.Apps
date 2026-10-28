using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual conversion audit, consumption, concurrent writers and access retirement retain the original financial source.</summary>
public sealed class AccountConversionWriterTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);

    private static async Task<Account> AssetAsync(FlowFixture f)
    {
        var item = new Account { Name = "Original valued item", Type = AccountType.Asset, CurrencyCode = "EUR",
            OpeningDate = Day.AddMonths(-1), OpeningBalance = 10000, CountryCode = "DE" };
        await f.Store.SaveAccountAsync(item, Ct); return item;
    }

    [Fact]
    public async Task The_proved_own_archive_audit_may_advance_without_ignoring_any_other_source_field()
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var store = f.Services.GetRequiredService<HoldingStore>();
        var review = await store.ReadAccountConversionAsync(account.Id, Day, Ct); Assert.NotNull(review);
        var originalAudit = review.Inputs.Account.UpdatedAt;
        f.Time.Now = f.Time.Now.AddMinutes(17);
        await store.ConvertAccountAsync(review, "Original place", () => true, Ct);
        var actual = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.True(actual.IsArchived); Assert.Equal(f.Time.Now, actual.UpdatedAt); Assert.NotEqual(originalAudit, actual.UpdatedAt);
        Assert.Equal(originalAudit, review.Inputs.Account.UpdatedAt); Assert.False(review.Inputs.Account.IsArchived);
        Assert.Equal(account.Name, actual.Name); Assert.Equal(account.CountryCode, actual.CountryCode); Assert.Equal(account.OpeningBalance, actual.OpeningBalance);
    }

    [Fact]
    public async Task A_consumed_original_review_never_creates_another_type_event_or_price()
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var store = f.Services.GetRequiredService<HoldingStore>();
        var review = await store.ReadAccountConversionAsync(account.Id, Day, Ct); Assert.NotNull(review);
        var accepted = await store.ConvertAccountAsync(review, "Original place", () => true, Ct);
        await Assert.ThrowsAsync<AccountConversionChangedException>(() => store.ConvertAccountAsync(review, "Another place", () => true, Ct));
        Assert.Equal(accepted.Id, Assert.Single(await store.GetTypesAsync(Ct)).Id);
        Assert.Single(await store.GetLocationsAsync(Ct)); Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
        Assert.Single(await store.GetValuationsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Independent_original_reviews_compete_for_one_real_sqlite_archive_transition()
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var store = f.Services.GetRequiredService<HoldingStore>();
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var second = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var firstReview = await store.ReadAccountConversionAsync(account.Id, Day, Ct);
        var secondReview = await second.ReadAccountConversionAsync(account.Id, Day, Ct);
        Assert.NotNull(firstReview); Assert.NotNull(secondReview);
        async Task<bool> Convert(HoldingStore writer, AccountConversionReview review)
        {
            try { await writer.ConvertAccountAsync(review, "Original place", () => true, Ct); return true; }
            catch (AccountConversionChangedException) { return false; }
        }
        var results = await Task.WhenAll(Convert(store, firstReview), Convert(second, secondReview));
        Assert.Single(results, value => value); Assert.Single(results, value => !value);
        Assert.Single(await store.GetTypesAsync(Ct)); Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
        Assert.Single(await store.GetValuationsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_changed_actual_file_access_snapshot_retires_the_preview_before_any_mutation()
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var source = new AccessSource();
        var store = new HoldingStore(factory, source);
        var review = await store.ReadAccountConversionAsync(account.Id, Day, Ct); Assert.NotNull(review);
        source.Current = new(System.IO.Path.GetFullPath(f.Directory.Combine("zanance.db")),
            new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid()), null));
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => store.ConvertAccountAsync(review, "Original place", () => true, Ct));
        Assert.False(Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).IsArchived);
        Assert.Empty(await store.GetTypesAsync(Ct)); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    /// <summary>Only the actual cached access identity changes; the real factory, reader and writer remain original.</summary>
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
}
