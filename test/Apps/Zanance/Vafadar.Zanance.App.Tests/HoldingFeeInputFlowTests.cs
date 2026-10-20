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

/// <summary>AT-410: original optional fee input must not silently remove an existing fee or refund relationship.</summary>
[Trait("AT", "AT-410")]
public sealed class HoldingFeeInputFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed record Original(HoldingStore Holdings, AssetEventEditorViewModel Editor, AssetEvent Event,
        Account Account, LedgerEntry Fee, LedgerEntry Refund);

    private static async Task<Original> SeedAsync(FlowFixture f)
    {
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Advanced;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var account = new Account { Name = "Original fictitious fee cash", CurrencyCode = "EUR", OpeningBalance = 90000, OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var type = new AssetType { Name = "Original fictitious item", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await holdings.SaveTypeAsync(type, Ct); var place = await holdings.EnsureDefaultLocationAsync("Original fictitious place", Ct);
        var item = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = new(2026, 10, 9),
            Kind = AssetEventKind.Purchase, Quantity = 1000, BasisAmount = 2500, Note = "Original source note" };
        var payment = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = account.Id, Date = item.Date, Amount = 2500, Title = type.Name };
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = item.Date, Amount = 100,
            CategoryId = await holdings.FeesCategoryAsync(Ct), Title = "Original fee", Note = "Original fee metadata" };
        Assert.True((await holdings.SaveEventAsync(item, [payment, fee], true, Ct)).Succeeded);
        var refund = EntryActions.CreateRefund(fee, 75, account.Id, item.Date); refund.Note = "Retained original refund"; refund.Tags = ["original"];
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var editor = new AssetEventEditorViewModel(holdings, f.Store,
            new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = item.Id });
        return new(holdings, editor, item, account, fee, refund);
    }

    [Theory]
    [InlineData("not money")][InlineData("-1.00")][InlineData("9223372036854775808")]
    public async Task Invalid_nonblank_fee_refuses_without_deleting_original_fee_or_unlinking_refund(string raw)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var vm = s.Editor;
        vm.FeeText = raw; vm.Note = "Original retained raw correction";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(raw, vm.FeeText); Assert.Equal("Original retained raw correction", vm.Note);
        Assert.Equal(f.Translator["Amount_Invalid"], vm.FeeError); Assert.True(vm.CanSave); Assert.True(vm.CanEdit);
        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Failures); Assert.True(vm.IsDirty);
        vm.FeeText = "1.00"; Assert.Null(vm.FeeError); await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.CanSave); Assert.False(vm.CanEdit); Assert.False(vm.IsDirty);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(3, rows.Count); Assert.Equal(100, rows.Single(e => e.Id == s.Fee.Id).Amount);
        Assert.Equal(s.Fee.Id, rows.Single(e => e.Id == s.Refund.Id).RefundOfId);
        Assert.Equal(87475, LedgerCalculator.Balance(s.Account, rows, s.Event.Date));
    }

    [Theory][InlineData("")][InlineData("0")][InlineData("0.75")]
    public async Task Explicit_blank_zero_or_positive_fee_keeps_its_original_financial_meaning(string raw)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var vm = s.Editor;
        var originalRefund = (await f.Store.GetEntryAsync(s.Refund.Id, Ct))!;
        vm.FeeText = raw; await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.CanSave); Assert.Null(vm.FeeError); Assert.Single(f.Platform.Routes);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct); var kept = rows.Single(e => e.Id == s.Refund.Id);
        var amount = raw == "0.75" ? 75 : 0;
        if (amount == 0) { Assert.DoesNotContain(rows, e => e.Id == s.Fee.Id); Assert.Null(kept.RefundOfId); originalRefund.RefundOfId = null; }
        else { Assert.Equal(amount, rows.Single(e => e.Id == s.Fee.Id).Amount); Assert.Equal(s.Fee.Id, kept.RefundOfId); }
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(originalRefund), System.Text.Json.JsonSerializer.Serialize(kept));
        Assert.Equal(87575 - amount, LedgerCalculator.Balance(s.Account, rows, s.Event.Date));
        Assert.Single(await s.Holdings.GetEventsAsync(cancellationToken: Ct));
    }

    [Theory][InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Regional_fee_input_keeps_raw_negative_draft_and_accepts_its_explicit_original_correction(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var s = await SeedAsync(f); var vm = s.Editor;
        // Input formatting deliberately returns magnitude; the rejected draft must carry its own explicit sign.
        var raw = "-" + MoneyText.ForInput(125, "EUR", f.Localization.CurrentCulture);
        if (language == "fa") raw = raw.Replace('1', '۱').Replace('2', '۲').Replace('5', '۵');
        vm.FeeText = raw; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.DoesNotContain(s.Account.Name, vm.EffectText ?? string.Empty);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(raw, vm.FeeText); Assert.Equal(f.Translator["Amount_Invalid"], vm.FeeError);
        vm.FeeText = MoneyText.ForInput(125, "EUR", f.Localization.CurrentCulture);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Null(vm.FeeError); Assert.False(vm.CanSave);
        Assert.Equal(125, (await f.Store.GetEntryAsync(s.Fee.Id, Ct))!.Amount);
        Assert.Equal(s.Fee.Id, (await f.Store.GetEntryAsync(s.Refund.Id, Ct))!.RefundOfId);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Dirty_fee_cancel_uses_original_explicit_discard_choice_without_money(bool discard)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); s.Editor.FeeText = "not money";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Platform.Confirmations.Enqueue(discard);
        await s.Editor.CancelCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(discard ? 1 : 0, f.Platform.Routes.Count);
        Assert.Equal("not money", s.Editor.FeeText); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Nonfinancial_opening_ignores_hidden_fee_draft_without_creating_or_deleting_money()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var vm = s.Editor;
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = s.Event.AssetTypeId, ["kind"] = "Opening" });
        Assert.False(vm.HasMoney); vm.QuantityText = "1"; vm.FeeText = "not money";
        var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        await vm.SaveCommand.ExecuteAsync(null); Assert.False(vm.CanSave); Assert.Null(vm.FeeError);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
        Assert.Equal(2, (await s.Holdings.GetEventsAsync(cancellationToken: Ct)).Count);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Invalid_or_overflowing_fee_preview_never_shows_a_wrapped_account_change(bool overflow)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var vm = s.Editor;
        if (overflow) vm.AmountText = MoneyText.ForInput(long.MaxValue, "EUR", f.Localization.CurrentCulture);
        vm.FeeText = overflow ? "1.00" : "bad fee";
        Assert.DoesNotContain(s.Account.Name, vm.EffectText ?? string.Empty); Assert.True(vm.CanSave);
        Assert.Equal(100, (await f.Store.GetEntryAsync(s.Fee.Id, Ct))!.Amount); Assert.Empty(f.Platform.Routes);
    }
}
