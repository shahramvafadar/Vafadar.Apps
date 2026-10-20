using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-415: original holding archive confirmation and accepted outcome.</summary>
[Trait("AT", "AT-415")]
public sealed class HoldingArchiveFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(HoldingStore Store, AssetType Type, AssetLocation Place, HoldingArchiveEditor Editor)> PrepareAsync(FlowFixture f)
    {
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original", PriceCurrencyCode = "EUR", Dimension = AssetDimension.Count,
            Kind = AssetKind.CountableItem, Note = "Original metadata", Icon = "Diamond", SortOrder = 17 };
        await store.SaveTypeAsync(type, Ct);
        var place = await store.EnsureDefaultLocationAsync("Original place", Ct);
        Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
            Date = new(2026, 10, 1), Kind = AssetEventKind.Opening, Quantity = 2000, BasisAmount = 5000 }, [], Ct)).Succeeded);
        var editor = new HoldingArchiveEditor(store, f.Translator, f.Platform);
        editor.PublishDisplayed(await store.ReadPriceReviewAsync(type.Id, Ct));
        editor.RefreshView = async () => await store.ReadPriceReviewAsync(type.Id, Ct);
        return (store, type, place, editor);
    }
    [Theory]
    [InlineData("name")]
    [InlineData("currency")]
    [InlineData("history")]
    [InlineData("price")]
    public async Task Original_archive_must_not_overwrite_concurrent_facts(string change)
    {
        using var f = new FlowFixture(); var (store, type, place, editor) = await PrepareAsync(f);
        f.Platform.PendingConfirmation = async () =>
        {
            if (change == "name") { type.Name = "Concurrent correction"; await store.SaveTypeAsync(type, Ct); }
            if (change == "currency") { type.PriceCurrencyCode = "USD"; await store.SaveTypeAsync(type, Ct); }
            if (change == "history") await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
                Date = new(2026, 10, 2), Kind = AssetEventKind.Opening, Quantity = 1000 }, [], Ct);
            if (change == "price") await store.SaveValuationAsync(new AssetValuation { AssetTypeId = type.Id,
                Date = new(2026, 10, 2), PricePerUnitMilli = 2000000, CurrencyCode = "EUR" }, Ct);
            return true;
        };
        await editor.ArchiveCommand.ExecuteAsync(null);
        Assert.False((await store.GetTypesAsync(Ct)).Single().IsArchived); Assert.False(editor.Committed);
        Assert.True(editor.NeedsRefresh); Assert.False(editor.CanArchive);
        Assert.Equal(f.Translator["Holding_ArchiveSourceChanged"], editor.Error);
        if (change == "name") Assert.Equal("Concurrent correction", (await store.GetTypesAsync(Ct)).Single().Name);
        var original = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.ArchiveCommand.ExecuteAsync(null); Assert.Equal(original, await GoalChooserFlowTests.SnapshotAsync(f));
        f.Platform.PendingConfirmation = null; f.Platform.Confirmations.Enqueue(true);
        await editor.RefreshCommand.ExecuteAsync(null); Assert.True(editor.CanArchive);
        await editor.ArchiveCommand.ExecuteAsync(null);
        Assert.True(editor.Committed); Assert.False(editor.CanArchive); Assert.True((await store.GetTypesAsync(Ct)).Single().IsArchived);
        Assert.Equal(change == "history" ? 2 : 1, (await store.GetEventsAsync(type.Id, Ct)).Count);
    }
    [Fact]
    public async Task Cancellation_preserves_the_original_packet_without_archive()
    {
        using var f = new FlowFixture(); var (_, _, _, editor) = await PrepareAsync(f);
        var original = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.ArchiveCommand.ExecuteAsync(null);
        Assert.Equal(original, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(editor.CanArchive);
        Assert.False(editor.Committed); Assert.Empty(f.Platform.Routes);
    }
    [Fact]
    public async Task Departed_pending_confirmation_cannot_archive()
    {
        using var f = new FlowFixture(); var (_, _, _, editor) = await PrepareAsync(f);
        var pending = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => pending.Task;
        var original = await GoalChooserFlowTests.SnapshotAsync(f); var command = editor.ArchiveCommand.ExecuteAsync(null);
        editor.Retire(); pending.SetResult(true); await command;
        Assert.Equal(original, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.Committed); Assert.True(editor.NeedsRefresh);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Known_archive_survives_notification_or_navigation_failure_without_resubmission(bool notification)
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        var original = (await store.GetTypesAsync(Ct)).Single();
        var events = await store.GetEventsAsync(type.Id, Ct);
        void Fail(object? sender, EventArgs args) => throw new IOException("Original archive notification");
        if (notification) store.Changed += Fail;
        else f.Platform.PendingNavigation = () => throw new IOException("Original archive return");
        f.Platform.Confirmations.Enqueue(true); await editor.ArchiveCommand.ExecuteAsync(null);
        if (notification) store.Changed -= Fail;
        Assert.True(editor.Committed); Assert.False(editor.CanArchive);
        Assert.Equal(f.Translator["Holding_ArchiveSavedRefresh"], editor.Error);
        var current = (await store.GetTypesAsync(Ct)).Single();
        Assert.True(current.IsArchived); Assert.Equal(original.Name, current.Name); Assert.Equal(original.Note, current.Note);
        Assert.Equal(original.PriceCurrencyCode, current.PriceCurrencyCode); Assert.Equal(original.SortOrder, current.SortOrder);
        Assert.Equal(events.Select(e => e.Id), (await store.GetEventsAsync(type.Id, Ct)).Select(e => e.Id));
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.ArchiveCommand.ExecuteAsync(null); await editor.RefreshCommand.ExecuteAsync(null);
        await editor.ArchiveCommand.ExecuteAsync(null);
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.CanArchive);
        Assert.Single(f.Platform.ConfirmationDialogs);
    }
    [Fact]
    public async Task Foreign_consumed_or_mutated_original_review_has_no_archive_authority()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        var (store, type, _, _) = await PrepareAsync(f); var foreign = other.Services.GetRequiredService<HoldingStore>();
        var review = await store.ReadPriceReviewAsync(type.Id, Ct);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => foreign.ArchiveReviewedTypeAsync(review, () => true, Ct));
        Assert.False((await store.GetTypesAsync(Ct)).Single().IsArchived);
        review.Inputs.Type.Name = "Mutated review";
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.ArchiveReviewedTypeAsync(review, () => true, Ct));
        review = await store.ReadPriceReviewAsync(type.Id, Ct);
        await store.ArchiveReviewedTypeAsync(review, () => true, Ct);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.ArchiveReviewedTypeAsync(review, () => true, Ct));
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Retirement_after_sql_save_rolls_back_and_explicit_refresh_allows_retry()
    {
        using var f = new FlowFixture(); var (store, _, _, editor) = await PrepareAsync(f);
        var original = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.Confirmations.Enqueue(true); f.Time.BeforeRead = editor.Retire;
        await editor.ArchiveCommand.ExecuteAsync(null); f.Time.BeforeRead = null;
        Assert.Equal(original, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.Committed);
        await editor.RefreshCommand.ExecuteAsync(null); f.Platform.Confirmations.Enqueue(true);
        await editor.ArchiveCommand.ExecuteAsync(null); Assert.True(editor.Committed);
        Assert.True((await store.GetTypesAsync(Ct)).Single().IsArchived);
    }
    [Fact]
    public async Task Failed_original_readback_does_not_rearm_a_known_archive()
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        f.Platform.PendingNavigation = () => throw new IOException("Original return");
        f.Platform.Confirmations.Enqueue(true); await editor.ArchiveCommand.ExecuteAsync(null);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        editor.RefreshView = () => throw new IOException("Original readback"); await editor.RefreshCommand.ExecuteAsync(null);
        Assert.True(editor.Committed); Assert.True(editor.NeedsRefresh); Assert.False(editor.CanArchive);
        editor.RefreshView = async () => await store.ReadPriceReviewAsync(type.Id, Ct); await editor.RefreshCommand.ExecuteAsync(null);
        Assert.False(editor.CanArchive); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Missing_refresh_packet_cannot_rearm_a_stale_archive()
    {
        using var f = new FlowFixture(); var (_, _, _, editor) = await PrepareAsync(f);
        editor.Retire(); editor.RefreshView = () => Task.FromResult<HoldingPriceReview?>(null);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.RefreshCommand.ExecuteAsync(null);
        Assert.True(editor.NeedsRefresh); Assert.False(editor.CanArchive); Assert.NotNull(editor.Error);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Sql_failure_keeps_original_state_and_explicit_refreshed_retry_can_archive()
    {
        using var f = new FlowFixture(); var (_, _, _, editor) = await PrepareAsync(f);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RefuseArchive BEFORE UPDATE ON AssetTypes WHEN NEW.IsArchived = 1 BEGIN SELECT RAISE(ABORT, 'Original archive persistence refusal'); END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Platform.Confirmations.Enqueue(true);
        await editor.ArchiveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.Committed); Assert.True(editor.NeedsRefresh);
        await using (var db = await factory.CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RefuseArchive;", Ct);
        await editor.RefreshCommand.ExecuteAsync(null); f.Platform.Confirmations.Enqueue(true);
        await editor.ArchiveCommand.ExecuteAsync(null); Assert.True(editor.Committed); Assert.False(editor.CanArchive);
    }
}
