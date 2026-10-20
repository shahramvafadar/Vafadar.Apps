using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

public sealed partial class HoldingWritePolicyTests
{
    [Fact, Trait("AT", "AT-408")]
    public async Task A_committed_original_purchase_carries_its_accepted_outcome_when_notification_fails()
    {
        var f = await FixtureAsync(); var item = f.Event(); var payment = f.Payment();
        f.Holdings.Changed += (_, _) => throw new IOException("Deliberate original holding postcommit notification failure.");
        var exception = await Assert.ThrowsAsync<CommittedWriteNotificationException<HoldingSaveResult>>(
            () => f.Holdings.SaveEventAsync(item, [payment], true, Ct));
        Assert.True(exception.Result.Succeeded);
        Assert.Single(await f.Holdings.GetEventsAsync(cancellationToken: Ct), e => e.Id == item.Id);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Single(await f.Holdings.GetValuationsAsync(cancellationToken: Ct), v => v.Source == ValuationSource.Purchase);
    }

    [Fact, Trait("AT", "AT-408")]
    public async Task A_retained_original_holding_correction_cannot_replace_a_newer_note()
    {
        var f = await FixtureAsync();
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, f.Opening.Id, Ct);
        var old = HoldingEventReadReview.CopyEvent(review.Inputs.Events.Single(e => e.Id == f.Opening.Id));
        var current = (await f.Holdings.GetEventsAsync(cancellationToken: Ct)).Single(e => e.Id == f.Opening.Id);
        current.Note = "Newer original holding note";
        Assert.True((await f.Holdings.SaveEventAsync(current, [], Ct)).Succeeded);
        var before = await SnapshotAsync(f.Provider);
        old.Quantity += 1000;
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => f.Holdings.SaveReviewedEventAsync(review, old, [], false, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }
    [Theory, Trait("AT", "AT-408")]
    [InlineData("type")][InlineData("history")][InlineData("account")][InlineData("location")]
    [InlineData("price")][InlineData("payment")][InlineData("attachment")][InlineData("delete")]
    public async Task Changed_original_holding_inputs_refuse_before_quantity_money_or_price(string changed)
    {
        var f = await FixtureAsync(); var item = f.Event(); var payment = f.Payment();
        Assert.True((await f.Holdings.SaveEventAsync(item, [payment], true, Ct)).Succeeded);
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, item.Id, Ct);
        var draft = HoldingEventReadReview.CopyEvent(review.Inputs.Events.Single(e => e.Id == item.Id)); draft.Quantity += 1000;
        var entries = review.Inputs.Entries.Select(e => e.Copy()).ToList();
        switch (changed)
        {
            case "type": f.Type.Note = "New source type"; await f.Holdings.SaveTypeAsync(f.Type, Ct); break;
            case "history": f.Opening.Quantity += 1000; await f.Holdings.SaveEventAsync(f.Opening, [], Ct); break;
            case "account": f.Cash.OpeningBalance++; await f.Store.SaveAccountAsync(f.Cash, Ct); break;
            case "location": f.Location.Name = "New source place"; await f.Holdings.SaveLocationAsync(f.Location, Ct); break;
            case "price": f.Valuation.Note = "New source price"; await f.Holdings.SaveValuationAsync(f.Valuation, Ct); break;
            case "payment": payment.Note = "New source payment"; await f.Store.SaveEntryAsync(payment, Ct); break;
            case "attachment": await f.Store.AddAttachmentAsync(new EntryAttachment { EntryId = payment.Id, FileName = "fictitious.txt", ContentType = "text/plain", Data = [1, 2, 3] }, Ct); break;
            case "delete": await f.Holdings.DeleteEventAsync(item.Id, Ct); break;
        }
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => f.Holdings.SaveReviewedEventAsync(review, draft, entries, true, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact, Trait("AT", "AT-408")]
    public async Task Reviewed_purchase_and_correction_preserve_exact_payment_fee_price_and_owned_metadata()
    {
        var f = await FixtureAsync(); var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, ct: Ct);
        var item = f.Event(); var payment = f.Payment();
        var fee = new Vafadar.Zanance.Core.Ledger.LedgerEntry { AccountId = f.Cash.Id, Date = Day, Amount = 100,
            Kind = Vafadar.Zanance.Core.Ledger.EntryKind.Expense, CategoryId = await f.Holdings.FeesCategoryAsync(Ct) };
        Assert.True((await f.Holdings.SaveReviewedEventAsync(review, item, [payment, fee], true, Ct)).Succeeded);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => f.Holdings.SaveReviewedEventAsync(review, item, [payment, fee], true, Ct));
        var saved = (await f.Holdings.GetEventsAsync(cancellationToken: Ct)).Single(e => e.Id == item.Id);
        var correction = await f.Holdings.ReadEventReviewAsync(f.Type.Id, saved.Id, Ct);
        var draft = HoldingEventReadReview.CopyEvent(saved); draft.Quantity = 6000; draft.BasisAmount = 3000;
        var money = correction.Inputs.Entries.Select(e => e.Copy()).ToList();
        money.Single(e => e.Kind == Vafadar.Zanance.Core.Ledger.EntryKind.AssetPurchase).Amount = 3000;
        Assert.True((await f.Holdings.SaveReviewedEventAsync(correction, draft, money, true, Ct)).Succeeded);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(2, rows.Count); Assert.Equal(3100, rows.Sum(e => e.Amount));
        Assert.Equal(96900, Vafadar.Zanance.Core.Ledger.LedgerCalculator.Balance(f.Cash, rows, Day));
        var valuation = Assert.Single(await f.Holdings.GetValuationsAsync(cancellationToken: Ct), v => v.Source == ValuationSource.Purchase);
        Assert.Equal(500000, valuation.PricePerUnitMilli);
        Assert.Equal(5000, item.Quantity); Assert.Null(item.GroupId); // Reviewed draft was never mutated by the writer.
    }

    [Theory, Trait("AT", "AT-408")][InlineData(false)][InlineData(true)]
    public async Task New_location_adoption_keeps_original_sources_and_refuses_a_simultaneous_history_edit(bool changeHistory)
    {
        var f = await FixtureAsync(); var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, f.Opening.Id, Ct);
        var place = new AssetLocation { Name = "Explicit new place" }; await f.Holdings.SaveLocationAsync(place, Ct);
        if (changeHistory) { f.Opening.Note = "New original note"; await f.Holdings.SaveEventAsync(f.Opening, [], Ct); }
        var before = await SnapshotAsync(f.Provider);
        if (changeHistory) await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => f.Holdings.ReviewAddedLocationAsync(review, place.Id, Ct));
        else
        {
            var adopted = await f.Holdings.ReviewAddedLocationAsync(review, place.Id, Ct);
            Assert.Contains(adopted.Inputs.Locations, l => l.Id == place.Id);
            Assert.Equal(review.EventId, adopted.EventId);
        }
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

}
