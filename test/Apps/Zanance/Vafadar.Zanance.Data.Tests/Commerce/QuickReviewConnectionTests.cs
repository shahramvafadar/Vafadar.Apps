using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>Complete original quick-resource reads retain one factory lifetime and never create default settings.</summary>
[Trait("AT", "AT-274")]
public sealed class QuickReviewConnectionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Theory, InlineData(false), InlineData(true)]
    public async Task Complete_review_uses_one_captured_context_and_preserves_original_rows(bool includeSource)
    {
        using var directory = new TemporaryDirectory();
        await using var services = new ServiceCollection().AddZananceData(directory.Combine("review.db")).BuildServiceProvider();
        await services.MigrateLocalDatabaseAsync<ZananceDbContext>(Ct);
        var original = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var account = new Account { Name = "Fictitious original", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 15), OpeningBalance = -123 };
        var entry = new LedgerEntry { AccountId = account.Id, Date = account.OpeningDate, Amount = 456, Kind = EntryKind.Income, Title = "Original source" };
        await using (var db = await original.CreateDbContextAsync(Ct))
        {
            db.Accounts.Add(account); db.Entries.Add(entry); await db.SaveChangesAsync(Ct);
        }
        var counted = new CountedFactory(original);
        var store = new ZananceStore(counted, TimeProvider.System, services.GetRequiredService<ICommercialWriteAccessSource>());
        var review = await store.GetQuickResourceReviewAsync(includeSource ? entry.Id : null, TestContext.Current.CancellationToken);
        Assert.Equal(1, counted.Created);
        Assert.Equal(account.Id, Assert.Single(review.Accounts).Id);
        Assert.Null(review.Settings);
        Assert.Equal(includeSource ? entry.Id : (Guid?)null, review.SourceEntry?.Id);
        await store.ValidateQuickResourceReviewAsync(review, TestContext.Current.CancellationToken);
        Assert.Equal(2, counted.Created); // Explicit later action still rechecks through a fresh original context.
        await using (var db = await original.CreateDbContextAsync(Ct))
        {
            Assert.Equal(-123, (await db.Accounts.SingleAsync(Ct)).OpeningBalance);
            Assert.Equal(456, (await db.Entries.SingleAsync(Ct)).Amount);
            Assert.False(await db.Settings.AnyAsync(Ct));
            Assert.False(await db.ResourceSelections.AnyAsync(Ct));
        }
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var owner = await location.Access.AcquireExclusiveAsync(deadline.Token);
        location.Access.RequireDrainedOwner(owner);
        // The real plain-SQLite fixture owns this exact idle pool; do not clear unrelated test pools.
        LocalDatabasePools.ClearFile(location.Path);
    }

    /// <summary>Counts actual factory construction without supplying queries, policy or simulated data.</summary>
    private sealed class CountedFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        /// <summary>Number of actual contexts borrowed by the original operation.</summary>
        public int Created { get; private set; }
        /// <inheritdoc />
        public ZananceDbContext CreateDbContext() { Created++; return inner.CreateDbContext(); }
        /// <inheritdoc />
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        { Created++; return inner.CreateDbContextAsync(ct); }
    }
}
