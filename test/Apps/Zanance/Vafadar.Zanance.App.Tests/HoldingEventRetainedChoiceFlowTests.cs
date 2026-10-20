using System.Text.Json;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-421: native choice rebinding preserves exact original historical money and transfer identities.</summary>
[Trait("AT", "AT-421")]
public sealed class HoldingEventRetainedChoiceFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_archived_original_payment_account_is_displayed_without_replacing_it_with_an_active_default()
    {
        using var f = new FlowFixture(); var (_, store, type, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var day = new DateOnly(2026, 10, 9); var original = new Account { Name = "Original archived payer", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = day };
        var next = new Account { Name = "Other active payer", CurrencyCode = "EUR", OpeningDate = day };
        await f.Store.SaveAccountAsync(original, Ct); await f.Store.SaveAccountAsync(next, Ct);
        var place = Assert.Single(await store.GetLocationsAsync(Ct));
        var purchase = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Kind = AssetEventKind.Purchase, Quantity = 1000, BasisAmount = 1000, Date = day };
        var payment = new LedgerEntry { AccountId = original.Id, Kind = EntryKind.AssetPurchase, Amount = 1000, Title = "Original payment", Date = day };
        Assert.True((await store.SaveEventAsync(purchase, [payment], false, Ct)).Succeeded);
        original.IsArchived = true; await f.Store.SaveAccountAsync(original, Ct);
        var before = JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var model = new AssetEventEditorViewModel(store, f.Store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = purchase.Id });
        Assert.True(model.Loading.IsReady); Assert.Equal(original.Id, model.Account?.Id); Assert.Contains(model.Accounts, a => a.Id == original.Id);
        Assert.Equal("10.00", model.AmountText); Assert.Equal(before, JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
    }

    [Fact]
    public async Task Same_original_transfer_Retry_rebinds_the_retained_destination_identity_to_its_current_native_choice()
    {
        using var f = new FlowFixture(); var (_, store, type, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var target = new AssetLocation { Name = "Original target" }; await store.SaveLocationAsync(target, Ct);
        var model = new AssetEventEditorViewModel(store, f.Store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "LocationTransfer" });
        var from = model.Location?.Id; model.ToLocation = model.Locations.Single(l => l.Id == target.Id); model.QuantityText = "1"; model.Note = "Original transfer draft";
        target.Name = "Changed original target caption"; await store.SaveLocationAsync(target, Ct); var before = JsonSerializer.Serialize(await store.GetLocationsAsync(Ct));
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "LocationTransfer" });
        Assert.True(model.Loading.IsReady); Assert.Equal(from, model.Location?.Id); Assert.Equal(target.Id, model.ToLocation?.Id); Assert.Equal(target.Name, model.ToLocation?.Name);
        Assert.Contains(model.ToLocation, model.Locations); Assert.Equal("1", model.QuantityText); Assert.Equal("Original transfer draft", model.Note);
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetLocationsAsync(Ct))); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }
}
