using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests.Commerce;

public sealed partial class HoldingWritePolicyTests
{
    [Theory, Trait("AT", "AT-409")]
    [InlineData("holding-amount")][InlineData("holding-currency")][InlineData("ordinary-currency")]
    public async Task Correcting_a_refunded_holding_fee_preserves_its_original_amount_and_currency_boundary(string operation)
    {
        var f = await FixtureAsync(); var item = f.Event(); var payment = f.Payment();
        var fee = new LedgerEntry { AccountId = f.Cash.Id, Date = Day, Kind = EntryKind.Expense, Amount = 100,
            CategoryId = await f.Holdings.FeesCategoryAsync(Ct), Title = "Original holding fee", Note = "Complete fee note", Tags = ["retained"] };
        Assert.True((await f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct)).Succeeded);
        var usd = new Account { Name = "Fictitious USD cash", CurrencyCode = "USD", Type = AccountType.Cash, OpeningDate = Day.AddMonths(-1) };
        await f.Store.SaveAccountAsync(usd, Ct);
        var refund = EntryActions.CreateRefund(fee, 75, f.Cash.Id, Day); refund.Note = "Original linked fee refund";
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, item.Id, Ct);
        var draft = HoldingEventReadReview.CopyEvent(review.Inputs.Events.Single(e => e.Id == item.Id));
        var entries = review.Inputs.Entries.Select(e => e.Copy()).ToList();
        var editedFee = entries.Single(e => e.Id == fee.Id);
        if (operation == "holding-amount") editedFee.Amount = 50;
        else foreach (var entry in entries) entry.AccountId = usd.Id;
        var before = await SnapshotAsync(f.Provider);
        var result = operation.StartsWith("holding-", StringComparison.Ordinal)
            ? (await f.Holdings.SaveReviewedEventAsync(review, draft, entries, true, Ct)).Errors
            : (await f.Store.SaveEntryAsync(editedFee, Ct)).Errors;
        Assert.Contains(operation == "holding-amount" ? LedgerError.RefundExceedsPurchase : LedgerError.RefundCurrencyMismatch, result);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }
    [Theory, Trait("AT", "AT-409")][InlineData(false)][InlineData(true)]
    public async Task A_refused_fee_correction_keeps_its_review_and_accepts_only_the_corrected_original_draft(bool currency)
    {
        var f = await FixtureAsync(); var item = f.Event(); var payment = f.Payment();
        var fee = new LedgerEntry { AccountId = f.Cash.Id, Date = Day, Kind = EntryKind.Expense, Amount = 100,
            CategoryId = await f.Holdings.FeesCategoryAsync(Ct), Title = "Original holding fee", Note = "Original metadata" };
        Assert.True((await f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct)).Succeeded);
        var usd = new Account { Name = "Fictitious USD cash", CurrencyCode = "USD", OpeningDate = Day };
        await f.Store.SaveAccountAsync(usd, Ct);
        var refund = EntryActions.CreateRefund(fee, 75, f.Cash.Id, Day); await f.Store.SaveEntryAsync(refund, Ct);
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, item.Id, Ct);
        var original = HoldingEventReadReview.CopyEvent(review.Inputs.Events.Single(e => e.Id == item.Id));
        var entries = review.Inputs.Entries.Select(e => e.Copy()).ToList(); var edited = entries.Single(e => e.Id == fee.Id);
        if (currency) foreach (var entry in entries) entry.AccountId = usd.Id; else edited.Amount = 50;
        var before = await SnapshotAsync(f.Provider);
        Assert.False((await f.Holdings.SaveReviewedEventAsync(review, original, entries, true, Ct)).Succeeded);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        foreach (var entry in entries) entry.AccountId = f.Cash.Id;
        edited.Amount = 75; original.Note = "Explicit corrected original event";
        Assert.True((await f.Holdings.SaveReviewedEventAsync(review, original, entries, true, Ct)).Succeeded);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        var kept = rows.Single(e => e.Id == refund.Id); Assert.Equal(fee.Id, kept.RefundOfId); Assert.Equal(75, kept.Amount);
        Assert.Equal(75, rows.Single(e => e.Id == fee.Id).Amount); Assert.Equal(97500, LedgerCalculator.Balance(f.Cash, rows, Day));
        Assert.Equal(3, rows.Count);
    }

    [Fact, Trait("AT", "AT-409")]
    public async Task The_original_atomic_batch_validates_final_refund_currency_when_both_accounts_are_explicitly_corrected()
    {
        var f = await FixtureAsync();
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = f.Cash.Id, Date = Day, Amount = 100 };
        Assert.True((await f.Store.SaveEntryAsync(fee, Ct)).Succeeded);
        var refund = EntryActions.CreateRefund(fee, 75, f.Cash.Id, Day); Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var usd = new Account { Name = "Fictitious corrected USD", CurrencyCode = "USD", OpeningDate = Day }; await f.Store.SaveAccountAsync(usd, Ct);
        fee.AccountId = usd.Id; refund.AccountId = usd.Id;
        Assert.True((await f.Store.SaveEntriesAsync([fee, refund], [], fee.Id, Ct)).Succeeded);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal(usd.Id, row.AccountId)); Assert.Equal(fee.Id, rows.Single(e => e.Id == refund.Id).RefundOfId);
    }

    [Fact, Trait("AT", "AT-409")]
    public async Task Explicit_fee_removal_keeps_the_existing_unlinked_refund_contract_and_complete_refund_metadata()
    {
        var f = await FixtureAsync(); var item = f.Event(); var payment = f.Payment();
        var fee = new LedgerEntry { AccountId = f.Cash.Id, Date = Day, Kind = EntryKind.Expense, Amount = 100,
            CategoryId = await f.Holdings.FeesCategoryAsync(Ct), Title = "Explicitly removed fee" };
        await f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct);
        var refund = EntryActions.CreateRefund(fee, 75, f.Cash.Id, Day); refund.Note = "Original retained refund"; refund.Tags = ["keep"];
        await f.Store.SaveEntryAsync(refund, Ct);
        var expected = (await f.Store.GetEntryAsync(refund.Id, Ct))!.Copy(); expected.RefundOfId = null;
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, item.Id, Ct);
        var original = HoldingEventReadReview.CopyEvent(review.Inputs.Events.Single(e => e.Id == item.Id));
        var remaining = review.Inputs.Entries.Where(e => e.Kind == EntryKind.AssetPurchase).Select(e => e.Copy()).ToList();
        Assert.True((await f.Holdings.SaveReviewedEventAsync(review, original, remaining, true, Ct)).Succeeded);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, rows.Count); Assert.DoesNotContain(rows, e => e.Id == fee.Id);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(expected), System.Text.Json.JsonSerializer.Serialize(rows.Single(e => e.Id == refund.Id)));
        Assert.Equal(97575, LedgerCalculator.Balance(f.Cash, rows, Day));
    }

    [Fact, Trait("AT", "AT-409")]
    public async Task Both_invalid_fee_fields_report_together_and_partial_correction_cannot_post_money()
    {
        var f = await FixtureAsync(); var item = f.Event(); var payment = f.Payment();
        var fee = new LedgerEntry { AccountId = f.Cash.Id, Date = Day, Kind = EntryKind.Expense, Amount = 100,
            CategoryId = await f.Holdings.FeesCategoryAsync(Ct) };
        await f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct);
        var refund = EntryActions.CreateRefund(fee, 75, f.Cash.Id, Day); await f.Store.SaveEntryAsync(refund, Ct);
        var usd = new Account { Name = "Fictitious USD correction", CurrencyCode = "USD", OpeningDate = Day }; await f.Store.SaveAccountAsync(usd, Ct);
        var review = await f.Holdings.ReadEventReviewAsync(f.Type.Id, item.Id, Ct);
        var original = HoldingEventReadReview.CopyEvent(review.Inputs.Events.Single(e => e.Id == item.Id));
        var entries = review.Inputs.Entries.Select(e => e.Copy()).ToList(); foreach (var entry in entries) entry.AccountId = usd.Id;
        var editedFee = entries.Single(e => e.Id == fee.Id); editedFee.Amount = 50;
        var before = await SnapshotAsync(f.Provider);
        var both = await f.Holdings.SaveReviewedEventAsync(review, original, entries, true, Ct);
        Assert.Contains(LedgerError.RefundExceedsPurchase, both.Errors); Assert.Contains(LedgerError.RefundCurrencyMismatch, both.Errors);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        editedFee.Amount = 75;
        var partial = await f.Holdings.SaveReviewedEventAsync(review, original, entries, true, Ct);
        Assert.Contains(LedgerError.RefundCurrencyMismatch, partial.Errors); Assert.DoesNotContain(LedgerError.RefundExceedsPurchase, partial.Errors);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        foreach (var entry in entries) entry.AccountId = f.Cash.Id;
        Assert.True((await f.Holdings.SaveReviewedEventAsync(review, original, entries, true, Ct)).Succeeded);
        Assert.Equal(97500, LedgerCalculator.Balance(f.Cash, await f.Store.GetEntriesAsync(cancellationToken: Ct), Day));
        Assert.Equal(fee.Id, (await f.Store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);
    }

}
