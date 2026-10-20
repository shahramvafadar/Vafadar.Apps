using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

public sealed partial class LedgerWritePolicyTests
{
    [Theory, Trait("AT", "AT-407")]
    [InlineData(false)][InlineData(true)]
    public async Task A_retained_split_or_join_cannot_replace_a_newer_original_note(bool join)
    {
        var f = await FixtureAsync();
        if (join)
        {
            var (seed, removed) = EntryActions.Split([f.Original.Copy()], [(null, 6000), (null, 4000)]);
            Assert.True((await f.Store.SaveEntriesAsync(seed, removed, seed[0].Id, Ct)).Succeeded);
        }
        var entry = (await f.Store.GetEntryAsync(f.Original.Id, Ct))!;
        var review = await f.Store.ReadSplitReviewAsync(entry.Id, Ct);
        var current = entry.Copy(); current.Note = "Newer original note must survive";
        Assert.True((await f.Store.SaveEntryAsync(current, Ct)).Succeeded);
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<SplitReviewChangedException>(() => join
            ? f.Store.JoinReviewedSplitAsync(review, Ct)
            : f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }
    [Theory, Trait("AT", "AT-407")]
    [InlineData(false, "account")][InlineData(true, "account")]
    [InlineData(false, "refund")][InlineData(true, "refund")]
    [InlineData(false, "attachment")][InlineData(true, "attachment")]
    [InlineData(false, "delete")][InlineData(true, "delete")]
    public async Task Changed_original_dependencies_refuse_the_whole_split_or_join(bool join, string change)
    {
        var f = await FixtureAsync();
        if (join)
        {
            var (seed, removed) = EntryActions.Split([f.Original.Copy()], [(null, 6000), (null, 4000)]);
            Assert.True((await f.Store.SaveEntriesAsync(seed, removed, seed[0].Id, Ct)).Succeeded);
        }
        var review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct);
        if (change == "account") { f.Account.OpeningBalance++; await f.Store.SaveAccountAsync(f.Account, Ct); }
        else if (change == "refund") Assert.True((await f.Store.SaveEntryAsync(new LedgerEntry
            { Kind = EntryKind.Refund, RefundOfId = f.Original.Id, AccountId = f.Account.Id, Date = Day, Amount = 100, Title = "New dependent refund" }, Ct)).Succeeded);
        else if (change == "attachment") await f.Store.AddAttachmentAsync(new EntryAttachment
            { EntryId = f.Original.Id, FileName = "fictitious.txt", ContentType = "text/plain", Data = [1, 2, 3] }, Ct);
        else await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<SplitReviewChangedException>(() => join
            ? f.Store.JoinReviewedSplitAsync(review, Ct)
            : f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact, Trait("AT", "AT-407")]
    public async Task Reviewed_split_reallocation_and_join_preserve_total_metadata_and_current_attachment_owner()
    {
        var f = await FixtureAsync();
        var review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct);
        // An unrelated account is not part of this original split's financial source.
        f.Second.Name = "Unrelated account renamed"; await f.Store.SaveAccountAsync(f.Second, Ct);
        Assert.True((await f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct)).Succeeded);
        var afterSplit = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<SplitReviewChangedException>(() => f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct));
        Assert.Equal(afterSplit, await SnapshotAsync(f.Provider));
        var group = (await f.Store.GetEntryAsync(f.Original.Id, Ct))!.GroupId!.Value;
        var parts = await f.Store.GetGroupAsync(group, Ct); Assert.Equal(2, parts.Count); Assert.Equal(10000, parts.Sum(p => p.Amount));
        Assert.All(parts, p => { Assert.Equal("Original payee", p.Payee); Assert.Equal("Original note", p.Note); Assert.Equal(f.Original.Tags, p.Tags); });
        var second = parts.Single(p => p.Id != f.Original.Id);
        var attachment = new EntryAttachment { EntryId = second.Id, FileName = "fictitious.txt", ContentType = "text/plain", Data = [1, 2, 3] };
        await f.Store.AddAttachmentAsync(attachment, Ct);
        review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct);
        Assert.True((await f.Store.SaveReviewedSplitAsync(review, [(null, 7000), (null, 3000)], Ct)).Succeeded);
        review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct);
        Assert.True((await f.Store.JoinReviewedSplitAsync(review, Ct)).Succeeded);
        var joined = (await f.Store.GetEntryAsync(f.Original.Id, Ct))!;
        Assert.Null(joined.GroupId); Assert.Equal(10000, joined.Amount); Assert.Equal("Original note", joined.Note);
        Assert.Null(await f.Store.GetEntryAsync(second.Id, Ct));
        var moved = (await f.Store.GetAttachmentAsync(attachment.Id, Ct))!;
        Assert.Equal(joined.Id, moved.EntryId); Assert.Equal(new byte[] { 1, 2, 3 }, moved.Data);
    }

    [Fact, Trait("AT", "AT-407")]
    public async Task A_mutated_review_or_another_actual_file_cannot_supply_split_authority()
    {
        var f = await FixtureAsync(); var other = await FixtureAsync();
        var review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct); var before = await SnapshotAsync(f.Provider);
        review.Inputs.Parts[0].Note = "Mutated original";
        await Assert.ThrowsAsync<SplitReviewChangedException>(() => f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct); var otherBefore = await SnapshotAsync(other.Provider);
        await Assert.ThrowsAsync<SplitReviewChangedException>(() => other.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(otherBefore, await SnapshotAsync(other.Provider));
    }

    [Theory, Trait("AT", "AT-407")]
    [InlineData(ProductPlan.Free, false)][InlineData(ProductPlan.Plus, false)][InlineData(ProductPlan.Pro, true)]
    public async Task Reviewed_split_uses_actual_writer_rights_without_granting_access_from_its_read(ProductPlan plan, bool shared)
    {
        var f = await FixtureAsync(); f.Enable(plan, shared);
        var review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct); var before = await SnapshotAsync(f.Provider);
        if (plan == ProductPlan.Free)
        {
            var refused = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct));
            Assert.Equal(CommercialFeature.SplitTransactions, refused.Feature); Assert.Equal(before, await SnapshotAsync(f.Provider));
        }
        else Assert.True((await f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct)).Succeeded);
    }

    [Fact, Trait("AT", "AT-407")]
    public async Task Existing_split_can_join_under_Free_but_a_changed_access_snapshot_cannot_reuse_original_review()
    {
        var f = await FixtureAsync();
        var review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct);
        Assert.True((await f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct)).Succeeded);
        f.Enable(ProductPlan.Free); review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct);
        Assert.True((await f.Store.JoinReviewedSplitAsync(review, Ct)).Succeeded);
        review = await f.Store.ReadSplitReviewAsync(f.Original.Id, Ct); f.Enable(ProductPlan.Plus);
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveReviewedSplitAsync(review, [(null, 6000), (null, 4000)], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

}
