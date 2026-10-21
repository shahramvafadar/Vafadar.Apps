using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-435: actual holding money retains original culture, currency and display-unit meaning.</summary>
[Trait("AT", "AT-435")]
[Collection("Entry validation units")]
public sealed class HoldingMoneyContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(HoldingStore Store, AssetType Type, AssetEventEditorViewModel Editor)> PrepareAsync(FlowFixture f, string field)
    {
        f.Localization.SetFormattingCulture("en-US");
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Advanced;
        await f.Store.SaveSettingsAsync(settings, Ct);
        await f.Store.SaveAccountAsync(new Account { Name = "Original holding money", CurrencyCode = field == "second" ? "USD" : "EUR",
            OpeningBalance = 10000000, OpeningDate = new(2026, 10, 1) }, Ct);
        if (field == "currency") await f.Store.SaveAccountAsync(new Account { Name = "Original USD destination", CurrencyCode = "USD",
            OpeningBalance = 10000000, OpeningDate = new(2026, 10, 1) }, Ct);
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original holding amount", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(type, Ct); await store.EnsureDefaultLocationAsync("Original place", Ct);
        var editor = new AssetEventEditorViewModel(store, f.Store,
            new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id,
            ["kind"] = field == "basis" ? "Opening" : "Purchase" });
        Assert.True(editor.CanSave); editor.QuantityText = "1"; editor.AmountText = "10";
        return (store, type, editor);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("fee")]
    [InlineData("second")]
    [InlineData("basis")]
    public async Task Each_original_holding_amount_keeps_its_parser_after_display_change(string field)
    {
        using var f = new FlowFixture(); var (store, type, vm) = await PrepareAsync(f, field);
        if (field == "amount") vm.AmountText = "1,234";
        if (field == "fee") vm.FeeText = "1,234";
        if (field == "second") vm.SecondAmountText = "1,234";
        if (field == "basis") vm.BasisText = "1,234";
        f.Localization.SetFormattingCulture("de-DE");
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.CanSave); Assert.Empty(f.Platform.Failures);
        var item = Assert.Single(await store.GetEventsAsync(type.Id, Ct));
        if (field is "second" or "basis") Assert.Equal(123400, item.BasisAmount);
        else
        {
            var kind = field == "fee" ? EntryKind.Expense : EntryKind.AssetPurchase;
            Assert.Equal(123400, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Single(e => e.Kind == kind).Amount);
        }
    }

    [Fact]
    public async Task Original_manual_price_keeps_its_parser_after_display_change()
    {
        using var f = new FlowFixture(); var (store, type, _) = await PrepareAsync(f, "basis");
        var editor = new HoldingPriceEditor(store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await editor.LoadAsync(type.Id); editor.Raw = "1,234";
        f.Localization.SetFormattingCulture("de-DE"); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(123400000, Assert.Single(await store.GetValuationsAsync(type.Id, Ct)).PricePerUnitMilli);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("fee")]
    [InlineData("second")]
    [InlineData("basis")]
    public async Task Changed_display_unit_refuses_each_nonempty_original_money_field(string field)
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]);
            using var f = new FlowFixture(); var (_, _, vm) = await PrepareAsync(f, field);
            if (field == "fee") vm.FeeText = "1";
            if (field == "second") vm.SecondAmountText = "1";
            if (field == "basis") vm.BasisText = "1";
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            await vm.SaveCommand.ExecuteAsync(null);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.False(vm.CanSave); Assert.Empty(f.Platform.Routes);
        }
        finally { DisplayUnits.Set(original); }
    }

    [Fact]
    public async Task Manual_price_unit_change_cannot_silently_multiply_the_original_raw_price()
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]);
            using var f = new FlowFixture(); var (store, type, _) = await PrepareAsync(f, "basis");
            var editor = new HoldingPriceEditor(store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
                f.Translator, f.Localization, f.Time, f.Platform);
            await editor.LoadAsync(type.Id); editor.Raw = "12.34";
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            await editor.SaveCommand.ExecuteAsync(null);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal("EUR", editor.InputUnitName); Assert.NotNull(editor.InputContextError);
            editor.Raw += "0"; Assert.NotNull(editor.InputContextError);
            editor.Raw = string.Empty; editor.Raw = "12.34";
            Assert.Null(editor.InputContextError); await editor.SaveCommand.ExecuteAsync(null);
            Assert.Equal(123400000, Assert.Single(await store.GetValuationsAsync(type.Id, Ct)).PricePerUnitMilli);
        }
        finally { DisplayUnits.Set(original); }
    }

    [Fact]
    public async Task Manual_price_delete_cannot_rearm_after_raw_edit_and_return()
    {
        using var f = new FlowFixture(); var (store, type, _) = await PrepareAsync(f, "basis");
        var price = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = new(2026, 10, 1), PricePerUnitMilli = 100000 };
        await store.SaveValuationAsync(price, Ct);
        var editor = new HoldingPriceEditor(store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await editor.LoadAsync(type.Id); editor.Raw = "2";
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task;
        var deleting = editor.DeleteAsync(price.Id, "Original displayed price");
        editor.Raw = "3"; editor.Raw = "2";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(true); await deleting;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(editor.NeedsRefresh); Assert.Null(editor.AcceptedId);
    }

    [Fact]
    public async Task Per_unit_payment_keeps_the_original_quantity_parser_after_display_change()
    {
        using var f = new FlowFixture(); var (store, type, vm) = await PrepareAsync(f, "amount");
        type.Divisible = true; await store.SaveTypeAsync(type, Ct);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Purchase" });
        vm.QuantityText = "1,234"; vm.PriceModeIndex = 1; vm.AmountText = "1";
        f.Localization.SetFormattingCulture("de-DE");
        Assert.NotNull(vm.QuantityFormatNote); Assert.DoesNotContain("EUR", vm.QuantityFormatNote);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(1234000, Assert.Single(await store.GetEventsAsync(type.Id, Ct)).Quantity);
        Assert.Equal(123400, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Single(e => e.Kind == EntryKind.AssetPurchase).Amount);
    }

    [Fact]
    public async Task Manual_price_delete_cannot_rearm_after_mode_edit_and_return()
    {
        using var f = new FlowFixture(); var (store, type, _) = await PrepareAsync(f, "basis");
        var price = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = new(2026, 10, 1), PricePerUnitMilli = 100000 };
        await store.SaveValuationAsync(price, Ct);
        var editor = new HoldingPriceEditor(store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await editor.LoadAsync(type.Id); editor.Raw = "2";
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task;
        var deleting = editor.DeleteAsync(price.Id, "Original displayed price");
        editor.ModeIndex = 1; editor.ModeIndex = 0;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(true); await deleting;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(editor.NeedsRefresh); Assert.Null(editor.AcceptedId);
    }

    [Fact]
    public async Task Payment_and_fee_require_independent_full_reentry_after_account_currency_change()
    {
        using var f = new FlowFixture(); var (_, _, vm) = await PrepareAsync(f, "currency");
        vm.AmountText = "25"; vm.FeeText = "1";
        vm.Account = vm.Accounts.Single(a => a.CurrencyCode == "USD");
        Assert.False(vm.CanSave); Assert.NotNull(vm.AmountContextError); Assert.NotNull(vm.FeeContextError);
        vm.AmountText = string.Empty; vm.AmountText = "25";
        Assert.Null(vm.AmountContextError); Assert.NotNull(vm.FeeContextError); Assert.False(vm.CanSave);
        vm.FeeText += "0"; Assert.Equal("EUR", vm.FeeUnitName); Assert.False(vm.CanSave);
        vm.FeeText = string.Empty; vm.FeeText = "1";
        Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(2500, rows.Single(e => e.Kind == EntryKind.AssetPurchase).Amount);
        Assert.Equal(100, rows.Single(e => e.Kind == EntryKind.Expense).Amount);
        Assert.All(rows, row => Assert.Equal(vm.Account.Id, row.AccountId));
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("fee")]
    [InlineData("quantity")]
    [InlineData("mode")]
    [InlineData("unit")]
    [InlineData("departure")]
    public async Task Pending_holding_money_writer_rechecks_original_monetary_edits_and_owner(string change)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var (_, _, vm) = await PrepareAsync(f, "amount");
            vm.AmountText = "25"; vm.FeeText = "1";
            var before = await GoalChooserFlowTests.SnapshotAsync(f); var reached = false;
            f.Time.BeforeRead = () =>
            {
                f.Time.BeforeRead = null; reached = true;
                if (change == "amount") { vm.AmountText = "26"; vm.AmountText = "25"; }
                else if (change == "fee") { vm.FeeText = "2"; vm.FeeText = "1"; }
                else if (change == "quantity") { vm.QuantityText = "2"; vm.QuantityText = "1"; }
                else if (change == "mode") { vm.PriceModeIndex = 1; vm.PriceModeIndex = 0; }
                else if (change == "unit") DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
                else vm.RetireReview();
            };
            await vm.SaveCommand.ExecuteAsync(null);
            Assert.True(reached); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Empty(f.Platform.Routes); Assert.Equal("25", vm.AmountText); Assert.Equal("1", vm.FeeText);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Theory]
    [InlineData("not money")]
    [InlineData("-1")]
    [InlineData("9223372036854775808")]
    public async Task Nonblank_invalid_price_currency_amount_is_not_silently_saved_as_unknown(string raw)
    {
        using var f = new FlowFixture(); var (_, _, vm) = await PrepareAsync(f, "second");
        vm.SecondAmountText = raw;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(vm.CanEdit); Assert.Equal(raw, vm.SecondAmountText); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Pending_holding_kind_change_and_return_cannot_revive_the_original_money_writer()
    {
        using var f = new FlowFixture(); var (_, _, vm) = await PrepareAsync(f, "amount");
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var reached = false;
        f.Time.BeforeRead = () =>
        {
            f.Time.BeforeRead = null; reached = true;
            vm.KindIndex = vm.KindNames.Count - 1; vm.KindIndex = 0;
        };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(reached); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Routes);
    }
}
