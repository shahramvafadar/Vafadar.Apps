using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-408: actual holding session preserves original sources, drafts and known accepted results.</summary>
[Trait("AT", "AT-408")]
public sealed class HoldingEventEditorFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(HoldingStore Store, AssetType Type, AssetEvent Event, LedgerEntry Payment)> SeedAsync(FlowFixture f)
    {
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var store = f.Services.GetRequiredService<HoldingStore>();
        var account = new Account { Name = "Fictitious holding cash", CurrencyCode = "EUR", OpeningBalance = 100000, OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var type = new AssetType { Name = "Fictitious holding", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(type, Ct); var location = await store.EnsureDefaultLocationAsync("Fictitious place", Ct);
        return (store, type, new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Purchase,
            Date = new(2026, 10, 20), Quantity = 1000, BasisAmount = 2500, Note = "Original holding note" },
            new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 20), Kind = EntryKind.AssetPurchase, Amount = 2500, Title = type.Name });
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Changed_source_or_pending_draft_refuses_without_mutating_original_objects(bool source)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var session = new HoldingEventEditorSession(seed.Store);
        await session.LoadAsync(seed.Type.Id, null);
        if (source) { seed.Type.Note = "Newer original type"; await seed.Store.SaveTypeAsync(seed.Type, Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => session.SaveAsync(seed.Event, [seed.Payment], () => source));
        Assert.False(session.Committed); Assert.Null(seed.Event.GroupId); Assert.Null(seed.Payment.GroupId);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        session.Retire(); Assert.Null(await session.SaveAsync(seed.Event, [seed.Payment], () => true));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_known_committed_purchase_cannot_become_a_second_event_after_notification_failure()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var session = new HoldingEventEditorSession(seed.Store);
        await session.LoadAsync(seed.Type.Id, null);
        seed.Store.Changed += (_, _) => throw new IOException("Deliberate original holding notification failure.");
        await Assert.ThrowsAsync<CommittedWriteNotificationException<HoldingSaveResult>>(() => session.SaveAsync(seed.Event, [seed.Payment], () => true));
        Assert.True(session.Committed); Assert.Null(session.Review);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Null(await session.SaveAsync(new AssetEvent { AssetTypeId = seed.Type.Id }, [], () => true));
        Assert.Null(await session.LoadAsync(seed.Type.Id, null));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Single(await seed.Store.GetEventsAsync(cancellationToken: Ct));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Single(await seed.Store.GetValuationsAsync(cancellationToken: Ct));
    }
}
