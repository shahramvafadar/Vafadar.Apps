using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-413: actual manual price source, confirmation, draft and accepted-result boundaries.</summary>
[Trait("AT", "AT-413")]
public sealed class HoldingPriceFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(HoldingStore Store, AssetType Type, AssetLocation Place, HoldingPriceEditor Editor)> PrepareAsync(FlowFixture f)
    {
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original manual valuation", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(type, Ct); var place = await store.EnsureDefaultLocationAsync("Original place", Ct);
        Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
            Date = new(2026, 10, 1), Kind = AssetEventKind.Opening, Quantity = 2000, BasisAmount = 5000,
            Note = "Retained original basis" }, [], Ct)).Succeeded);
        var editor = new HoldingPriceEditor(store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await editor.LoadAsync(type.Id); editor.ModeIndex = 1; editor.Raw = "60"; editor.Date = new(2026, 10, 9);
        return (store, type, place, editor);
    }
    [Theory]
    [InlineData("quantity")]
    [InlineData("currency")]
    [InlineData("price")]
    public async Task Changed_original_facts_refuse_then_explicit_refresh_keeps_raw_draft(string change)
    {
        using var f = new FlowFixture(); var (store, type, place, editor) = await PrepareAsync(f);
        if (change == "quantity") Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id,
            LocationId = place.Id, Kind = AssetEventKind.Opening, Date = editor.Date, Quantity = 1000 }, [], Ct)).Succeeded);
        if (change == "currency") { type.PriceCurrencyCode = "USD"; await store.SaveTypeAsync(type, Ct); }
        if (change == "price") await store.SaveValuationAsync(new AssetValuation { AssetTypeId = type.Id,
            Date = editor.Date.AddDays(-1), CurrencyCode = "EUR", PricePerUnitMilli = 2400000, Note = "Concurrent original price" }, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(editor.NeedsRefresh); Assert.False(editor.CanSubmit); Assert.Equal("60", editor.Raw);
        Assert.Equal(f.Translator["Holding_PriceSourceChanged"], editor.Error);
        await editor.RefreshCommand.ExecuteAsync(null);
        Assert.True(editor.CanSubmit); Assert.Equal("60", editor.Raw); Assert.Null(editor.Error);
        await editor.SaveCommand.ExecuteAsync(null);
        var accepted = (await store.GetValuationsAsync(type.Id, Ct)).Single(v => v.Id == editor.AcceptedId);
        Assert.Equal(change == "quantity" ? 2000000 : 3000000, accepted.PricePerUnitMilli);
        Assert.Equal(type.PriceCurrencyCode, accepted.CurrencyCode); Assert.Equal(ValuationSource.Manual, accepted.Source);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(editor.Raw);
        Assert.True(editor.CanSubmit); Assert.False(editor.NeedsRefresh);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Known_commit_survives_notification_failure_without_replaying_save_or_delete(bool delete)
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        var price = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = editor.Date,
            PricePerUnitMilli = 2500000, Note = "Original manual price" };
        if (delete) { await store.SaveValuationAsync(price, Ct); await editor.RefreshCommand.ExecuteAsync(null); f.Platform.Confirmations.Enqueue(true); }
        void Fail(object? sender, EventArgs args) => throw new IOException("Original post-commit notification");
        store.Changed += Fail;
        if (delete) await editor.DeleteAsync(price.Id, "Original confirmed price"); else await editor.SaveCommand.ExecuteAsync(null);
        store.Changed -= Fail;
        Assert.NotNull(editor.AcceptedId); Assert.Equal(delete, editor.LastWasDeletion);
        Assert.True(editor.NeedsRefresh); Assert.False(editor.CanSubmit);
        Assert.Equal(f.Translator["Holding_PriceSavedRefresh"], editor.Error);
        Assert.Equal(delete ? "60" : "", editor.Raw);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.SaveCommand.ExecuteAsync(null); await editor.DeleteAsync(price.Id, "Repeated command");
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await editor.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(delete ? 0 : 1, (await store.GetValuationsAsync(type.Id, Ct)).Count);
        Assert.True(editor.CanSubmit); Assert.False(editor.NeedsRefresh); Assert.Null(editor.Error);
    }
    [Theory]
    [InlineData("row")]
    [InlineData("departure")]
    [InlineData("draft")]
    public async Task Pending_original_delete_confirmation_cannot_delete_changed_or_departed_facts(string change)
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        var price = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = editor.Date,
            PricePerUnitMilli = 2500000, Note = "Original confirmation metadata" };
        await store.SaveValuationAsync(price, Ct); await editor.RefreshCommand.ExecuteAsync(null);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task;
        var deleting = editor.DeleteAsync(price.Id, "Original displayed row");
        if (change == "row") { price.Note = "Updated after original confirmation"; await store.SaveValuationAsync(price, Ct); }
        if (change == "departure") editor.Retire();
        if (change == "draft") editor.Raw = "70";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(true); await deleting;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Null(editor.AcceptedId); Assert.True(editor.NeedsRefresh); Assert.False(editor.CanSubmit);
        Assert.Equal(change == "draft" ? "70" : "60", editor.Raw);
        Assert.Equal(f.Translator["Holding_PriceSourceChanged"], editor.Error);
    }
    [Fact]
    public async Task Successful_save_retires_before_failed_readback_and_retry_refresh_only_reads()
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        editor.ReloadView = () => throw new IOException("Original readback failed");
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Holding_PriceSavedRefresh"], editor.Error); Assert.False(editor.CanSubmit);
        Assert.Empty(editor.Raw); var accepted = Assert.Single(await store.GetValuationsAsync(type.Id, Ct));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.SaveCommand.ExecuteAsync(null);
        editor.ReloadView = async () => { await editor.LoadAsync(type.Id); };
        await editor.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(accepted.Id, editor.AcceptedId); Assert.True(editor.CanSubmit); Assert.Null(editor.Error);
    }
    [Fact]
    public async Task Cancelled_price_delete_keeps_entire_store_and_original_raw_draft()
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        var price = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = editor.Date, PricePerUnitMilli = 2500000 };
        await store.SaveValuationAsync(price, Ct); await editor.RefreshCommand.ExecuteAsync(null);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Platform.Confirmations.Enqueue(false);
        await editor.DeleteAsync(price.Id, "Original price");
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal("60", editor.Raw);
        Assert.True(editor.CanSubmit); Assert.Null(editor.AcceptedId);
    }
    [Theory]
    [InlineData("invalid")]
    [InlineData("92233720368547.76")]
    public async Task Unrepresentable_or_invalid_price_refuses_without_rows_and_keeps_same_form_correction(string raw)
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        editor.ModeIndex = 0; editor.Raw = raw;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(raw, editor.Raw);
        Assert.Equal(f.Translator["Amount_Invalid"], editor.Error); Assert.True(editor.CanSubmit);
        editor.Raw = "30"; await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(3000000, Assert.Single(await store.GetValuationsAsync(type.Id, Ct)).PricePerUnitMilli);
    }
    [Fact]
    public async Task Empty_departed_form_reads_updated_history_but_raw_departed_form_requires_explicit_refresh()
    {
        using var f = new FlowFixture(); var (store, type, place, editor) = await PrepareAsync(f);
        editor.Raw = ""; editor.Leave();
        Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
            Date = editor.Date, Kind = AssetEventKind.Opening, Quantity = 1000 }, [], Ct)).Succeeded);
        var current = await editor.LoadAsync(type.Id); Assert.Equal(3000, current.Events.Sum(e => e.Quantity));
        Assert.True(editor.CanSubmit); Assert.False(editor.NeedsRefresh);
        editor.Raw = "60"; editor.Leave();
        Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
            Date = editor.Date, Kind = AssetEventKind.Opening, Quantity = 1000 }, [], Ct)).Succeeded);
        var retained = await editor.LoadAsync(type.Id); Assert.Equal(3000, retained.Events.Sum(e => e.Quantity));
        Assert.False(editor.CanSubmit); Assert.True(editor.NeedsRefresh); Assert.Equal("60", editor.Raw);
        await editor.RefreshCommand.ExecuteAsync(null); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(1500000, Assert.Single(await store.GetValuationsAsync(type.Id, Ct)).PricePerUnitMilli);
    }
    [Fact]
    public async Task Pending_explicit_refresh_keeps_save_disabled_until_the_original_read_finishes()
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        editor.Retire(); var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editor.ReloadView = async () => { await pending.Task; await editor.LoadAsync(type.Id); };
        var refreshing = editor.RefreshCommand.ExecuteAsync(null);
        Assert.True(editor.Busy); Assert.False(editor.CanSubmit); Assert.False(editor.CanRefresh);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        pending.SetResult(); await refreshing;
        Assert.False(editor.Busy); Assert.True(editor.CanSubmit); Assert.Equal("60", editor.Raw);
        Assert.Empty(await store.GetValuationsAsync(type.Id, Ct));
    }
    [Fact]
    public async Task Reviewed_price_identity_and_consumed_source_cannot_be_reused()
    {
        using var f = new FlowFixture(); var (store, type, _, editor) = await PrepareAsync(f);
        var review = await store.ReadPriceReviewAsync(type.Id, Ct);
        var price = new AssetValuation { AssetTypeId = Guid.NewGuid(), CurrencyCode = "EUR", Date = editor.Date, PricePerUnitMilli = 3000000 };
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.SaveReviewedValuationAsync(review, price, () => true, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); price.AssetTypeId = type.Id;
        await store.SaveReviewedValuationAsync(review, price, () => true, Ct);
        before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.DeleteReviewedValuationAsync(review, price.Id, () => true, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
