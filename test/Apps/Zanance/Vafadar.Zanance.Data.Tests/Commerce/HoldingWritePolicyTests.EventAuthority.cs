using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

public sealed partial class HoldingWritePolicyTests
{
    [Theory, Trait("AT", "AT-408")]
    [InlineData(ProductPlan.Free, false, false)]
    [InlineData(ProductPlan.Plus, false, true)]
    [InlineData(ProductPlan.Free, true, true)]
    public async Task Reviewed_holding_write_keeps_original_actual_file_capability(ProductPlan plan, bool shared, bool allowed)
    {
        var f = await FixtureAsync(); f.Enable(plan, shared);
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, ct: Ct); var item = f.Event(); var payment = f.Payment();
        var before = await SnapshotAsync(f.Provider);
        if (allowed)
        {
            Assert.True((await f.Holdings.SaveReviewedEventAsync(review, item, [payment], true, Ct)).Succeeded);
            Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        else
        {
            var failure = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Holdings.SaveReviewedEventAsync(review, item, [payment], true, Ct));
            Assert.Equal(CommercialFeature.ManageHoldings, failure.Feature);
            Assert.Equal(before, await SnapshotAsync(f.Provider));
        }
    }

    [Theory, Trait("AT", "AT-408")][InlineData("memory")][InlineData("payment")][InlineData("pending")]
    public async Task Reviewed_holding_rejects_changed_memory_foreign_money_or_pending_input(string changed)
    {
        var f = await FixtureAsync(); var foreign = f.Payment();
        Assert.True((await f.Store.SaveEntryAsync(foreign, Ct)).Succeeded);
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, ct: Ct); var item = f.Event();
        var payment = changed == "payment" ? foreign : f.Payment();
        var before = await SnapshotAsync(f.Provider);
        if (changed == "memory") review.Inputs.Type.Note = "Mutated original memory";
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => f.Holdings.SaveReviewedEventAsync(review, item, [payment], true, Ct, () => changed != "pending"));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Null(item.GroupId);
    }
    [Fact, Trait("AT", "AT-408")]
    public async Task Reviewed_new_event_cannot_retarget_an_existing_event_from_an_unreviewed_type()
    {
        var f = await FixtureAsync();
        var otherType = new Vafadar.Zanance.Core.Holdings.AssetType { Name = "Unreviewed other holding", PriceCurrencyCode = "EUR" };
        await f.Holdings.SaveTypeAsync(otherType, Ct);
        var other = new Vafadar.Zanance.Core.Holdings.AssetEvent { AssetTypeId = otherType.Id, LocationId = f.Location.Id,
            Date = Day, Kind = Vafadar.Zanance.Core.Holdings.AssetEventKind.Opening, Quantity = 1000 };
        Assert.True((await f.Holdings.SaveEventAsync(other, [], Ct)).Succeeded);
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, ct: Ct);
        var changed = HoldingEventReadReview.CopyEvent(other); changed.AssetTypeId = f.Type.Id;
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => f.Holdings.SaveReviewedEventAsync(review, changed, [], false, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

}
