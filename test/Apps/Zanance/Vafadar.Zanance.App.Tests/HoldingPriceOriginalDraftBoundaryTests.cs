using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-644: original inline manual-price drafts and known metadata receipts retain their actual owner.</summary>
public sealed class HoldingPriceOriginalDraftBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(HoldingStore Store, AssetType Type, HoldingPriceEditor Editor)> OriginalAsync(FlowFixture f)
    {
        var store = f.Services.GetRequiredService<HoldingStore>(); var type = new AssetType { Name = "D644 original manual price", PriceCurrencyCode = "EUR",
            Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count };
        await store.SaveTypeAsync(type, Ct);
        var editor = new HoldingPriceEditor(store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await editor.LoadAsync(type.Id); editor.Raw = "12.34"; editor.Date = new(2026, 10, 9); return (store, type, editor);
    }
    /// <summary>Acceptance cannot clear later raw price, date or mode delivered by a following notification.</summary>
    [Theory]
    [InlineData("raw", false)] [InlineData("raw", true)]
    [InlineData("date", false)] [InlineData("date", true)]
    [InlineData("mode", false)] [InlineData("mode", true)]
    public async Task Accepted_original_price_keeps_the_later_independent_draft(string changed, bool notificationFails)
    {
        using var f = new FlowFixture(); var (store, type, editor) = await OriginalAsync(f); var originalDate = editor.Date;
        store.Changed += (_, _) =>
        {
            if (changed == "raw") editor.Raw = "99.99";
            if (changed == "date") editor.Date = originalDate.AddDays(1);
            if (changed == "mode") editor.ModeIndex = 1;
            if (notificationFails) throw new IOException("D644 original accepted price notification refusal.");
        };
        await editor.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await store.GetValuationsAsync(type.Id, Ct));
        Assert.Equal(editor.AcceptedId, saved.Id); Assert.Equal(1234000, saved.PricePerUnitMilli); Assert.Equal(originalDate, saved.Date);
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(changed == "raw" ? "99.99" : "12.34", editor.Raw);
        if (changed == "date") Assert.Equal(originalDate.AddDays(1), editor.Date);
        if (changed == "mode") Assert.Equal(1, editor.ModeIndex);
    }
    /// <summary>The original detail must expose its inline price as an unsaved form before native Back can discard it.</summary>
    [Fact]
    public async Task Original_detail_exposes_its_actual_inline_price_as_unsaved()
    {
        using var f = new FlowFixture(); var (store, type, _) = await OriginalAsync(f); var vm = HoldingDetailFlowTests.Create(f, store);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync();
        vm.PriceEditor.Raw = "12.34";
        Assert.True(Assert.IsAssignableFrom<IUnsavedChanges>(vm).IsDirty);
    }
    /// <summary>A departed actual detail cannot keep an enabled original refresh that belongs to its old frame.</summary>
    [Fact]
    public async Task Retired_original_detail_disables_inline_price_refresh()
    {
        using var f = new FlowFixture(); var (store, type, _) = await OriginalAsync(f); var vm = HoldingDetailFlowTests.Create(f, store);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync();
        vm.PriceEditor.Raw = "12.34"; vm.Retire(); Assert.False(vm.PriceEditor.CanRefresh); Assert.False(vm.PriceEditor.CanSubmit);
    }

    /// <summary>A native consent belongs to one original draft and frame, even if values later return.</summary>
    [Theory]
    [InlineData("draft")] [InlineData("departure")] [InlineData("root")]
    public async Task Pending_original_Back_cannot_discard_a_different_owner(string change)
    {
        using var f = new FlowFixture(); var (store, type, _) = await OriginalAsync(f); var vm = HoldingDetailFlowTests.Create(f, store);
        var ownsRoot = true; vm.BindRootOwner(() => ownsRoot);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync(); vm.PriceEditor.Raw = "12.34";
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task;
        var leaving = vm.BackCommand.ExecuteAsync(null);
        Assert.False(vm.CanLeave); Assert.False(vm.PriceEditor.CanSubmit); Assert.False(vm.PriceEditor.CanRefresh);
        await vm.BackCommand.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs);
        if (change == "draft") { vm.PriceEditor.Date = vm.PriceEditor.Date.AddDays(1); vm.PriceEditor.Date = vm.PriceEditor.Date.AddDays(-1); }
        if (change == "departure") vm.Retire();
        if (change == "root") ownsRoot = false;
        pending.SetResult(true); await leaving;
        Assert.Empty(f.Platform.Routes); Assert.Equal("12.34", vm.PriceEditor.Raw);
        Assert.Empty(await store.GetValuationsAsync(type.Id, Ct));
    }
    /// <summary>Cancellation keeps the original price; an explicit current discard leaves exactly once.</summary>
    [Fact]
    public async Task Original_Back_keeps_then_discards_only_the_current_inline_draft()
    {
        using var f = new FlowFixture(); var (store, type, _) = await OriginalAsync(f); var vm = HoldingDetailFlowTests.Create(f, store);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync(); vm.PriceEditor.Raw = "12.34";
        f.Platform.Confirmations.Enqueue(false); await vm.BackCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        f.Platform.Confirmations.Enqueue(true); await vm.BackCommand.ExecuteAsync(null);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route); Assert.Equal("12.34", vm.PriceEditor.Raw);
        Assert.Empty(await store.GetValuationsAsync(type.Id, Ct));
    }
    /// <summary>The real commit receipt exists before arbitrary fallible Changed subscribers execute.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Actual_price_receipt_precedes_the_original_notification(bool notificationFails)
    {
        using var f = new FlowFixture(); var (store, type, editor) = await OriginalAsync(f);
        Guid? observed = null;
        store.Changed += (_, _) => { observed = editor.AcceptedId; if (notificationFails) throw new IOException("D644 receipt notification"); };
        await editor.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await store.GetValuationsAsync(type.Id, Ct));
        Assert.Equal(saved.Id, observed); Assert.Equal(saved.Id, editor.AcceptedId); Assert.Empty(editor.Raw);
        Assert.False(editor.IsDirty); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Live translated choices retain numeric input, date and selected original price mode.</summary>
    [Theory]
    [InlineData("de")] [InlineData("fa")]
    public async Task Original_price_display_changes_keep_the_actual_draft(string language)
    {
        using var f = new FlowFixture(); var (_, _, editor) = await OriginalAsync(f); editor.ModeIndex = 1;
        var date = editor.Date; editor.SetMoneyPresentationActive(true);
        try
        {
            f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
            Assert.Equal(1, editor.ModeIndex); Assert.Equal("12.34", editor.Raw); Assert.Equal(date, editor.Date);
            Assert.Equal(f.Translator["Holding_PriceTotal"], editor.ModeNames[1]);
        }
        finally { editor.SetMoneyPresentationActive(false); }
    }

    /// <summary>A terminal archive must review the original separate price draft before either dialog can leave.</summary>
    [Theory]
    [InlineData("keep")] [InlineData("draft")] [InlineData("departure")]
    public async Task Original_archive_cannot_silently_discard_the_inline_price(string change)
    {
        using var f = new FlowFixture(); var (store, type, _) = await OriginalAsync(f); var vm = HoldingDetailFlowTests.Create(f, store);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync(); vm.PriceEditor.Raw = "12.34";
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task; var archiving = vm.ArchiveCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Common_DiscardTitle"], Assert.Single(f.Platform.ConfirmationDialogs).Title);
        if (change == "draft") vm.PriceEditor.Raw = "99.99";
        if (change == "departure") vm.Retire();
        pending.SetResult(change != "keep"); await archiving;
        Assert.Empty(f.Platform.Routes); Assert.False((await store.GetTypesAsync(Ct)).Single(item => item.Id == type.Id).IsArchived);
        Assert.Empty(await store.GetValuationsAsync(type.Id, Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>A historical accepted receipt cannot relabel a later refused write or failed read as saved.</summary>
    [Fact]
    public async Task Historical_price_receipt_does_not_label_a_later_refusal_as_saved()
    {
        using var f = new FlowFixture(); var (store, type, editor) = await OriginalAsync(f);
        await editor.SaveCommand.ExecuteAsync(null); var accepted = editor.AcceptedId; Assert.NotNull(accepted);
        editor.Raw = "55.00"; type.Name = "D644 actual concurrent type change"; await store.SaveTypeAsync(type, Ct);
        await editor.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, editor.AcceptedId);
        Assert.Equal(f.Translator["Holding_PriceSourceChanged"], editor.Error);
        editor.ReloadView = () => throw new IOException("D644 later original read refusal.");
        await editor.RefreshCommand.ExecuteAsync(null);
        Assert.NotEqual(f.Translator["Holding_PriceSavedRefresh"], editor.Error);
        Assert.Equal("55.00", editor.Raw); Assert.Equal(accepted, editor.AcceptedId);
        Assert.Single(await store.GetValuationsAsync(type.Id, Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
